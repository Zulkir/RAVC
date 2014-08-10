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
using Ravc.Encoding.Transforms;

namespace Ravc.Tests.Encoding
{
    [TestFixture]
    public unsafe class EntropyTransformTests
    {
        private Random random;

        [SetUp]
        public void SetUp()
        {
            //random = new Random();
            random = new Random(123123);
        }

        [Test]
        public void SeparateChannels()
        {
            const int width = 1920;
            var original = Enumerable.Range(0, width).Select(x => random.Next(0x00ffffff)).ToArray();
            var transformed = new int[width];
            var reverted = new int[width];
            transformed[width * 3 / 4] = 123123;

            fixed (int* pOriginal = original)
            fixed (int* pTransformed = transformed)
            fixed (int* pReverted = reverted)
            {
                SeparateChannelsTransform.Apply((byte*)pTransformed, (byte*)pOriginal, width);
                SeparateChannelsTransform.Revert((byte*)pReverted, (byte*)pTransformed, width);
            }

            Assert.That(reverted, Is.EqualTo(original));
            Assert.That(transformed[width * 3 / 4], Is.EqualTo(123123));
        }

        [Test]
        public void Delta()
        {
            const int length = 1920 * 3;
            var original = Enumerable.Range(0, length).Select(x => (byte)random.Next(0xff)).ToArray();
            var processed = new byte[length];
            Array.Copy(original, processed, length);

            fixed (byte* pProcessed = processed)
            {
                DeltaCoding.Apply(pProcessed, length);
                DeltaCoding.Revert(pProcessed, length);
            }

            Assert.That(processed, Is.EqualTo(original));
        }

        [Test]
        public void ZeroLength()
        {
            const int length = 1920 * 3;
            //var original = Enumerable.Range(0, length).Select(x => (byte)(int)(128 * Math.Sin(x / 100.0))).ToArray();
            var original = Enumerable.Range(0, length).Select(x => (byte)random.Next(0xff)).ToArray();
            var transformed = new byte[length * 2];
            var reverted = new byte[length];
            int compressedLength;

            fixed (byte* pOriginal = original)
            fixed (byte* pTransformed = transformed)
            fixed (byte* pReverted = reverted)
            {
                compressedLength = ZeroLengthEncoding.Apply(pTransformed, pOriginal, length);
                ZeroLengthEncoding.Revert(pReverted, pTransformed, length);
            }

            Assert.That(reverted, Is.EqualTo(original));
            Assert.That(compressedLength, Is.GreaterThanOrEqualTo(Enumerable.Range(0, length * 2).Reverse().First(x => transformed[x] != 0)));
            Console.WriteLine(compressedLength);
        }

        [Test]
        public void Asd()
        {
            Console.WriteLine(new DateTime(2011, 2, 6) - new DateTime(2011, 1, 27));
        }
    }
}