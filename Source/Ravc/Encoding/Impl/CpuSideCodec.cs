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
using System.Threading.Tasks;
using Ravc.Encoding.Transforms;
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
            public float Timestamp;

            public const int Size = 3 * sizeof(int) + sizeof(float);
        }

        private struct PartInfo
        {
            public byte* Source;
            public byte* Auxiliary;
            public byte* Result;
            public int UncompressedSize;
        }

        private const int PartCount = 24;
        private const int LastPartIndex = PartCount - 1;
        private const int PartOffsetsTableSize = PartCount * sizeof(int);
        private const int HeaderSize = CompressedFrameInfo.Size + PartOffsetsTableSize;

        private readonly ByteArrayPool byteArrayPool;
        private readonly Action<IntPtr, IntPtr, int> cpblk;
        private readonly Pool<PartInfo[]> partInfoArrayPool;
        private readonly byte[] blockEncodingWriteLut;
        private readonly byte[] blockEncodingReadLut;

        public CpuSideCodec(ByteArrayPool byteArrayPool, Action<IntPtr, IntPtr, int> cpblk)
        {
            this.byteArrayPool = byteArrayPool;
            this.cpblk = cpblk;

            partInfoArrayPool = new Pool<PartInfo[]>(() => new PartInfo[PartCount]);

            blockEncodingWriteLut = new byte[TernaryBlockEncoding.WriteLutSize];
            fixed (byte* pWriteLut = blockEncodingWriteLut)
                TernaryBlockEncoding.BuildWriteLut(pWriteLut);

            blockEncodingReadLut = new byte[TernaryBlockEncoding.ReadLutSize];
            fixed (byte* pReadLut = blockEncodingReadLut)
                TernaryBlockEncoding.BuildReadLut(pReadLut);
        }

        private static int CalculateAuxiliaryRowSize(int width)
        {
            return width * 6;
        }

        private static void RemoveAlpha(byte* destination, byte* source, int width)
        {
            var iDest = (int*)destination;
            var iSource = (int*)source;
            while (width > 0)
            {
                //*iDest = *iSource;
                *iDest = *iSource & 0x00FFFFFF;
                iDest++;
                iSource++;
                width--;
            }
        }

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
            fixed (byte* lut = blockEncodingWriteLut)
            fixed (byte* result = resultBuffer.Item)
            {
                var lutLocal = lut;
                WriteFrameInfo(frame, result);
                var partInfos = partInfoBuffer.Item;
                FillPartInfosForCompression(partInfos, frame.Info, source, auxiliary, result);
                var compressedPartSizesTable = (int*)partSizes;
                //for (int i = 0; i < PartCount; i++)
                Parallel.For(0, PartCount, i =>
                    CompressPart(i, partInfos, lutLocal, compressedPartSizesTable));
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
            frameInfo->Timestamp = frame.Info.Timestamp;
        }

        private static void FillPartInfosForCompression(PartInfo[] partInfos, FrameInfo frameInfo, byte* source, byte* auxiliary, byte* result)
        {
            int typicalPartSizeInPixels = frameInfo.UncompressedSize / 4 / PartCount;

            for (int i = 0; i < PartCount; i++)
                partInfos[i] = new PartInfo
                {
                    Source = source + i * typicalPartSizeInPixels * 4,
                    Auxiliary = auxiliary + i * typicalPartSizeInPixels * 6,
                    Result = result + HeaderSize + i * typicalPartSizeInPixels * 6,
                    UncompressedSize = typicalPartSizeInPixels * 4
                };
            partInfos[LastPartIndex].UncompressedSize = frameInfo.UncompressedSize - (PartCount - 1) * typicalPartSizeInPixels * 4;
        }

        private static void CompressPart(int i, PartInfo[] partInfos, byte* lut, int* partSizesTable)
        {
            var part = partInfos[i];
            int sizeInPixels = part.UncompressedSize / 4;
            SeparateChannelsTransform.Apply(part.Result, part.Source, sizeInPixels);
            int separatedSize = sizeInPixels * 3;
            //DeltaCoding.Apply(part.Result, separatedSize);
            partSizesTable[i] = TernaryBlockEncoding.Apply(part.Auxiliary, part.Result, lut, separatedSize);
            
            //cpblk((IntPtr)part.Auxiliary, (IntPtr)part.Source, part.UncompressedSize);
            //partSizesTable[i] = part.UncompressedSize;

            //RemoveAlpha(part.Result, part.Source, sizeInPixels);
            //partSizesTable[i] = TernaryBlockEncoding.Apply(part.Auxiliary, part.Result, lut, part.UncompressedSize);
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
                cpblk((IntPtr)result + HeaderSize + partOffsetsTable[i], (IntPtr)part.Auxiliary, compressedPartSizesTable[i]);
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
                frameInfo = new FrameInfo(compressedFrameInfo.Type, compressedFrameInfo.Timestamp, compressedFrameInfo.OriginalWidth, compressedFrameInfo.OriginalHeight);

                resultBuffer = byteArrayPool.Extract(frameInfo.UncompressedSize);

                var partInfoBuffer = partInfoArrayPool.Extract();
                var auxiliaryBuffer = byteArrayPool.Extract(frameInfo.UncompressedSize);
                fixed (byte* auxiliary = auxiliaryBuffer.Item)
                fixed (byte* lut = blockEncodingReadLut)
                fixed (byte* result = resultBuffer.Item)
                {
                    var lutLocal = lut;
                    var partInfos = partInfoBuffer.Item;
                    var partOffsetsTable = (int*)(source + CompressedFrameInfo.Size);
                    FillPartInfosForDecompression(partInfos, frameInfo, source, auxiliary, result, partOffsetsTable);
                    //for (int i = 0; i < PartCount; i++)
                    Parallel.For(0, PartCount, i => 
                        DecompressPart(i, partInfos, lutLocal));
                }
                auxiliaryBuffer.Release();
                partInfoBuffer.Release();
            }

            return new UncompressedFrame(frameInfo, resultBuffer);
        }

        private static void FillPartInfosForDecompression(PartInfo[] partInfos, FrameInfo frameInfo, byte* source, byte* auxiliary, byte* result, int* partOffsetsTable)
        {
            int typicalPartSizeInPixels = frameInfo.UncompressedSize / 4 / PartCount;
            int typicalSizeInBytes = typicalPartSizeInPixels * 4;

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
            int separatedSize = sizeInPixels * 3;
            TernaryBlockEncoding.Revert(part.Auxiliary, part.Source, lut, separatedSize);
            //DeltaCoding.Revert(part.Auxiliary, separatedSize);
            SeparateChannelsTransform.Revert(part.Result, part.Auxiliary, sizeInPixels);

            //cpblk((IntPtr)part.Result, (IntPtr)part.Source, part.UncompressedSize);

            //TernaryBlockEncoding.Revert(part.Result, part.Source, lut, part.UncompressedSize);
        }
        #endregion
    }
}