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
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Ravc.Encoding.Transforms;

namespace Ravc.Tests.Encoding
{
    public unsafe class DottedEncodingTests
    {
        Random random;

        [SetUp]
        public void SetUp()
        {
            var seed = Environment.TickCount;
            Console.WriteLine(seed);
            random = new Random(seed);
        }

        private static void FillAlpha(int[] original)
        {
            for (int i = 0; i < original.Length; i++)
            {
                int pixel = original[i];
                var red = (sbyte)(pixel & 0xFF);
                var green = (sbyte)((pixel >> 8) & 0xFF);
                var blue = (sbyte)((pixel >> 16) & 0xFF);
                if (-3 > red || red > 2 || -3 > green || green > 3 || -3 > blue || blue > 2)
                    original[i] = pixel | 0x01000000;
            }
        }

        private static void EraseAlpha(int[] original)
        {
            for (int i = 0; i < original.Length; i++)
            {
                original[i] &= 0x00FFFFFF;
            }
        }

        private static void DoTest(int[] original)
        {
            FillAlpha(original);

            var readLut = new byte[DottedEncoding.ReadLutSize];
            var transformed = new byte[original.Length * 8];
            var reversed = new int[original.Length];

            fixed (int* pOriginal = original)
            fixed (byte* pLut = readLut)
            fixed (byte* pTransformed = transformed)
            fixed (int* pReversed = reversed)
            {
                int compressedSIze = DottedEncoding.Apply(pTransformed, (byte*)pOriginal, original.Length);
                Console.WriteLine(compressedSIze);
                Console.WriteLine((double)compressedSIze / (original.Length * 3));
                DottedEncoding.BuildReadLut(pLut);
                DottedEncoding.Revert((byte*)pReversed, pTransformed, pLut, original.Length);
            }

            EraseAlpha(original);

            Assert.That(reversed, Is.EqualTo(original));
        }

        [Test]
        public void Zeroes()
        {
            DoTest(new int[1280 * 720]);
        }

        [Test]
        public void ConstantSmall()
        {
            DoTest(Enumerable.Range(0, 1280 * 720).Select(x => 0x00FD03FD).ToArray());
        }

        [Test]
        public void ConstantLarge()
        {
            DoTest(Enumerable.Range(0, 1280 * 720).Select(x => 0x00808080).ToArray());
        }

        [Test]
        public void Different()
        {
            var dataList = new List<int>();
            for (int i = 0; i < 123; i++)
            {
                int blockType = random.Next(0, 7);
                int blockLength = random.Next(0, 100);
                for (int j = 0; j < blockLength; j++)
                {
                    switch (blockType)
                    {
                        case 0: dataList.Add(0); break;
                        case 1: dataList.Add(random.Next(3) < 2 ? (byte)random.Next(-4, 3) : 0); break;
                        case 2: dataList.Add(random.Next(3) < 2 ? (byte)random.Next(-4, 3) << 8 : 0); break;
                        case 3: dataList.Add(random.Next(3) < 2 ? (byte)random.Next(-4, 3) << 16 : 0); break;
                        case 4: dataList.Add(random.Next(3) < 2 ? ((byte)random.Next(-3, 2) | (byte)random.Next(-3, 3) << 8 | (byte)random.Next(-3, 3) << 16) : 0); break;
                        case 5: dataList.Add(random.Next(3) < 2 ? ((byte)random.Next(-16, 15) | (byte)random.Next(-32, 31) << 8 | (byte)random.Next(-16, 15) << 16) : 0); break;
                        case 6: dataList.Add(random.Next(3) < 2 ? ((byte)random.Next(-128, 127) | (byte)random.Next(-128, 127) << 8 | (byte)random.Next(-128, 127) << 16) : 0); break;
                    }
                }
            }
            int mod = dataList.Count % 4;
            if (mod != 0)
                for (int i = 0; i < 4 - mod; i++)
                    dataList.Add(0);
            DoTest(dataList.ToArray());
        }
    }
}