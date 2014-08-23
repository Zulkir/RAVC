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
using System.Linq;
using NUnit.Framework;
using Ravc.Encoding;
using Ravc.Encoding.Impl;
using Ravc.Pcl;
using Ravc.Utility;

namespace Ravc.Tests.Encoding
{
    [TestFixture]
    public class CpuSideCodecTests
    {
        private class PckWorkaroundsMock : IPclWorkarounds
        {
            public unsafe void CopyBulk(IntPtr dst, IntPtr src, int length)
            {
                while (length >= sizeof(long))
                {
                    *(long*)dst = *(long*)src;
                    dst += sizeof(long);
                    src += sizeof(long);
                    length -= sizeof(long);
                }
                while (length > 0)
                {
                    *(byte*)dst = *(byte*)src;
                    dst += 1;
                    src += 1;
                    length--;
                }
            }
        }

        private Random random;
        private ByteArrayPool byteArrayPool;
        private CpuSideCodec codec;

        [SetUp]
        public void SetUp()
        {
            byteArrayPool = new ByteArrayPool();
            var pclWorkarounds = new PckWorkaroundsMock();
            codec = new CpuSideCodec(pclWorkarounds, byteArrayPool);
            random = new Random(234234);
        }

        private uint GetRandomPixel()
        {
            return (uint)random.Next(int.MinValue, int.MaxValue) & 0x00FFFFFF;
        }

        private static void FillAlpha(uint[] original)
        {
            for (int i = 0; i < original.Length; i++)
            {
                uint pixel = original[i];
                var red = (sbyte)(pixel & 0xFF);
                var green = (sbyte)((pixel >> 8) & 0xFF);
                var blue = (sbyte)((pixel >> 16) & 0xFF);
                if (-3 > red || red > 2 || -3 > green || green > 3 || -3 > blue || blue > 2)
                    original[i] = pixel | 0x01000000;
            }
        }

        private static void EraseAlpha(uint[] original)
        {
            for (int i = 0; i < original.Length; i++)
            {
                original[i] &= 0x00FFFFFF;
            }
        }

        private unsafe void DoTest(int width, int height, Func<int, uint> getPixel)
        {
            var frameInfo = new FrameInfo(FrameType.Relative, 123456.789f, width, height);
            var uncompressedSizeInPixels = frameInfo.UncompressedSize / 4;
            var pixels = Enumerable.Range(0, uncompressedSizeInPixels).Select(getPixel).ToArray();
            FillAlpha(pixels);

            var frame = new UncompressedFrame(frameInfo, byteArrayPool.Extract(uncompressedSizeInPixels * 4));
            fixed (byte* frameData = frame.DataPooled.Item)
            {
                var frameDataUint = (uint*)frameData;
                for (int i = 0; i < uncompressedSizeInPixels; i++)
                    frameDataUint[i] = pixels[i];
            }
            var compressedFrame = codec.Compress(frame);
            Console.WriteLine(compressedFrame.CompressedSize);
            var decompressedFrame = codec.Decompress(compressedFrame);
            Assert.That(decompressedFrame.Info.AlignedWidth, Is.EqualTo(frame.Info.AlignedWidth));
            Assert.That(decompressedFrame.Info.AlignedHeight, Is.EqualTo(frame.Info.AlignedHeight));
            Assert.That(decompressedFrame.Info.Timestamp, Is.EqualTo(frame.Info.Timestamp));


            var resultPixels = new uint[uncompressedSizeInPixels];
            fixed (byte* resultData = decompressedFrame.DataPooled.Item)
            {
                var resultDataUint = (uint*)resultData;
                for (int i = 0; i < uncompressedSizeInPixels; i++)
                    resultPixels[i] = resultDataUint[i] & 0x00FFFFFF;
            }

            EraseAlpha(pixels);
            Assert.That(resultPixels, Is.EqualTo(pixels));
        }

        [TestCase(1, 1)]
        [TestCase(10, 10)]
        [TestCase(1, 60)]
        [TestCase(1920, 1)]
        [TestCase(1920, 1080)]
        public void TestZeroData(int width, int height)
        {
            DoTest(width, height, x => (uint)0);
        }

        [TestCase(1, 1)]
        [TestCase(10, 10)]
        [TestCase(1, 60)]
        [TestCase(1920, 1)]
        [TestCase(1920, 1080)]
        public void TestConstantData(int width, int height)
        {
            DoTest(width, height, x => (uint)0x007F7F7F);
        }

        [TestCase(1, 1)]
        [TestCase(10, 10)]
        [TestCase(1, 60)]
        [TestCase(1920, 1)]
        [TestCase(1920, 1080)]
        public void TestSequentialData(int width, int height)
        {
            DoTest(width, height, x => (uint)x);
        }

        [TestCase(1, 1)]
        [TestCase(10, 10)]
        [TestCase(1, 60)]
        [TestCase(1920, 1)]
        [TestCase(1920, 1080)]
        public void TestRandomData(int width, int height)
        {
            DoTest(width, height, x => GetRandomPixel());
        }
    }
}