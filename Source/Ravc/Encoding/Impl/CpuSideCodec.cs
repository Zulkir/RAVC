#region License
/*
Copyright (c) 2014 RAVC Project - Daniil Rodin

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
#endregion

using System;
using System.Runtime.InteropServices;
using Ravc.Encoding.Transforms;
using Ravc.Pcl;
using Ravc.Utility;

namespace Ravc.Encoding.Impl
{
    public unsafe class CpuSideCodec : ICpuSideCodec
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct CompressedFrameInfo
        {
            public int OriginalWidth;
            public int OriginalHeight;
            public FrameType Type;
            public int MostDetailedMip;
            public int ColorDiffThreshold;
            public float Timestamp;
            public short MouseX;
            public short MouseY;

            public const int Size = 5 * sizeof(int) + sizeof(float) + 2 * sizeof(ushort);
        }

        private struct PartInfo
        {
            public byte* Source;
            public byte* Auxiliary;
            public byte* Result;
            public int UncompressedSize;
        }

        private const int PartCount = 1;
        private const int LastPartIndex = PartCount - 1;
        private const int PartOffsetsTableSize = PartCount * sizeof(int);
        private const int HeaderSize = CompressedFrameInfo.Size + PartOffsetsTableSize;

        private readonly IPclWorkarounds pclWorkarounds;
        private readonly ByteArrayPool byteArrayPool;
        private readonly Pool<PartInfo[]> partInfoArrayPool;
        private readonly byte[] dottedEncodingReadLut;

        public CpuSideCodec(IPclWorkarounds pclWorkarounds, ByteArrayPool byteArrayPool)
        {
            this.pclWorkarounds = pclWorkarounds;
            this.byteArrayPool = byteArrayPool;

            partInfoArrayPool = new Pool<PartInfo[]>(() => new PartInfo[PartCount]);

            dottedEncodingReadLut = new byte[DottedEncoding.ReadLutSize];
            fixed (byte* pReadLut = dottedEncodingReadLut)
                DottedEncoding.BuildReadLut(pReadLut);
        }

        private volatile int cc;

        #region Compression
        public CompressedFrame Compress(UncompressedFrame frame)
        {
            int auxiliaryBufferSize = frame.Info.UncompressedSize * 3 / 2;
            int resultBufferSize = HeaderSize + auxiliaryBufferSize;

            var resultBuffer = byteArrayPool.Extract(resultBufferSize);
            int resultSize;

            var partInfoBuffer = partInfoArrayPool.Extract();
            var auxiliaryBuffer = byteArrayPool.Extract(auxiliaryBufferSize);
            var partSizesBuffer = byteArrayPool.Extract(PartCount * sizeof(int));
            fixed (byte* source = frame.DataPooled.Item)
            fixed (byte* auxiliary = auxiliaryBuffer.Item)
            fixed (byte* partSizes = partSizesBuffer.Item)
            fixed (byte* result = resultBuffer.Item)
            {
                if (frame.Info.OriginalWidth == 1280 && frame.Info.OriginalHeight == 720)
                {
                    cc++;
                }
                if (frame.Info.OriginalWidth == 1280 && frame.Info.OriginalHeight == 720 && cc > 20)
                {
                    cc = 0;
                }

                WriteFrameInfo(frame, result);
                var partInfos = partInfoBuffer.Item;
                FillPartInfosForCompression(partInfos, frame.Info, source, auxiliary, result);
                var compressedPartSizesTable = (int*)partSizes;
                for (int i = 0; i < PartCount; i++)
                //Parallel.For(0, PartCount, i =>
                    CompressPart(i, partInfos, compressedPartSizesTable);//);
                var partOffsetsTable = (int*)(result + CompressedFrameInfo.Size);
                FillPartOffsetsTable(partOffsetsTable, compressedPartSizesTable);
                PackCompressedParts(partInfos, result, partOffsetsTable, compressedPartSizesTable);
                resultSize = HeaderSize + partOffsetsTable[LastPartIndex] + compressedPartSizesTable[LastPartIndex];
            }
            partSizesBuffer.Release();
            auxiliaryBuffer.Release();
            partInfoBuffer.Release();

            return new CompressedFrame(resultBuffer, resultSize);
        }

        private static void WriteFrameInfo(UncompressedFrame frame, byte* result)
        {
            var frameInfo = (CompressedFrameInfo*)result;
            frameInfo->OriginalWidth = frame.Info.OriginalWidth;
            frameInfo->OriginalHeight = frame.Info.OriginalHeight;
            frameInfo->Type = frame.Info.Type;
            frameInfo->MostDetailedMip = frame.Info.MostDetailedMip;
            frameInfo->ColorDiffThreshold = frame.Info.ColorDiffThreshold;
            frameInfo->Timestamp = frame.Info.Timestamp;
            frameInfo->MouseX = (short)frame.Info.MouseX;
            frameInfo->MouseY = (short)frame.Info.MouseY;
        }

        private static void FillPartInfosForCompression(PartInfo[] partInfos, FrameInfo frameInfo, byte* source, byte* auxiliary, byte* result)
        {
            int typicalPartSizeInPixelTuples = frameInfo.UncompressedSize / 16 / PartCount;

            for (int i = 0; i < PartCount; i++)
                partInfos[i] = new PartInfo
                {
                    Source = source + i * typicalPartSizeInPixelTuples * 16,
                    Auxiliary = auxiliary + i * typicalPartSizeInPixelTuples * 24,
                    Result = result + HeaderSize + i * typicalPartSizeInPixelTuples * 24,
                    UncompressedSize = typicalPartSizeInPixelTuples * 16
                };
            partInfos[LastPartIndex].UncompressedSize = frameInfo.UncompressedSize - (PartCount - 1) * typicalPartSizeInPixelTuples * 16;
        }

        private static void CompressPart(int i, PartInfo[] partInfos, int* partSizesTable)
        {
            var part = partInfos[i];
            int sizeInPixels = part.UncompressedSize / 4;
            partSizesTable[i] = DottedEncoding.Apply(part.Auxiliary, part.Source, sizeInPixels);
        }

        private static void FillPartOffsetsTable(int* partOffsetsTable, int* partSizesTable)
        {
            int offset = 0;
            for (int i = 0; i < PartCount; i++)
            {
                partOffsetsTable[i] = offset;
                offset += partSizesTable[i];
            }
        }

        private void PackCompressedParts(PartInfo[] partInfos, byte* result, int* partOffsetsTable, int* compressedPartSizesTable)
        {
            for (int i = 0; i < PartCount; i++)
            {
                var part = partInfos[i];
                pclWorkarounds.CopyBulk((IntPtr)result + HeaderSize + partOffsetsTable[i], (IntPtr)part.Auxiliary, compressedPartSizesTable[i]);
            }
        }
        #endregion

        #region Decompression
        public UncompressedFrame Decompress(CompressedFrame compressedFrame)
        {
            FrameInfo frameInfo;
            IPooled<byte[]> resultBuffer;

            fixed (byte* source = compressedFrame.DataPooled.Item)
            {
                var compressedFrameInfo = *(CompressedFrameInfo*)source;
                frameInfo = new FrameInfo(compressedFrameInfo.Type, compressedFrameInfo.Timestamp, compressedFrameInfo.MostDetailedMip, 
                    compressedFrameInfo.ColorDiffThreshold, compressedFrameInfo.OriginalWidth, compressedFrameInfo.OriginalHeight,
                    compressedFrameInfo.MouseX, compressedFrameInfo.MouseY);
            
                resultBuffer = byteArrayPool.Extract(frameInfo.UncompressedSize);
            
                var partInfoBuffer = partInfoArrayPool.Extract();
                var auxiliaryBuffer = byteArrayPool.Extract(frameInfo.UncompressedSize);
                fixed (byte* auxiliary = auxiliaryBuffer.Item)
                fixed (byte* lut = dottedEncodingReadLut)
                fixed (byte* result = resultBuffer.Item)
                {
                    var lutLocal = lut;
                    var partInfos = partInfoBuffer.Item;
                    var partOffsetsTable = (int*)(source + CompressedFrameInfo.Size);
                    FillPartInfosForDecompression(partInfos, frameInfo, source, auxiliary, result, partOffsetsTable);
                    for (int i = 0; i < PartCount; i++)
                    //Parallel.For(0, PartCount, i => 
                        DecompressPart(i, partInfos, lutLocal);//);
                }
                auxiliaryBuffer.Release();
                partInfoBuffer.Release();
            }
            
            return new UncompressedFrame(frameInfo, resultBuffer);
        }

        private static void FillPartInfosForDecompression(PartInfo[] partInfos, FrameInfo frameInfo, byte* source, byte* auxiliary, byte* result, int* partOffsetsTable)
        {
            int typicalPartSizeInPixelTuples = frameInfo.UncompressedSize / 16 / PartCount;
            int typicalSizeInBytes = typicalPartSizeInPixelTuples * 16;

            for (int i = 0; i < PartCount; i++)
                partInfos[i] = new PartInfo
                {
                    Source = source + HeaderSize + partOffsetsTable[i],
                    Auxiliary = auxiliary + i * typicalSizeInBytes,
                    Result = result + i * typicalSizeInBytes,
                    UncompressedSize = typicalSizeInBytes
                };
            partInfos[LastPartIndex].UncompressedSize = frameInfo.UncompressedSize - (PartCount - 1) * typicalSizeInBytes;
        }

        private static void DecompressPart(int index, PartInfo[] partInfos, byte* lut)
        {
            var part = partInfos[index];
            int sizeInPixels = part.UncompressedSize / 4;
            DottedEncoding.Revert(part.Result, part.Source, lut, sizeInPixels);
        }
        #endregion
    }
}