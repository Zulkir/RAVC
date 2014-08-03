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
    public abstract unsafe class CompressionTransformTestsBase
    {
        protected abstract void DoTest(byte[] original);

        [Test]
        public void Zeroes()
        {
            DoTest(Enumerable.Range(0, 1920 * 3 * 10).Select(x => byte.MinValue).ToArray());
        }

        [Test]
        public void Ones()
        {
            DoTest(Enumerable.Range(0, 1920 * 3 * 10).Select(x => byte.MaxValue).ToArray());
        }

        [Test]
        public void Constant127()
        {
            DoTest(Enumerable.Range(0, 1920 * 3 * 10).Select(x => (byte)127).ToArray());
        }

        [Test]
        public void Cycling()
        {
            DoTest(Enumerable.Range(0, 1920 * 3 * 10).Select(x => (byte)x).ToArray());
        }

        [Test]
        public void Steps()
        {
            DoTest(new byte[]
            {
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
                1, 1, 1, 1, 1, 1, 1, 
                9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
                9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 
                1, 1, 1, 1, 1, 1, 1, 1, 1,
            });
        }

        [Test]
        public void Sin()
        {
            const int length = 1920 * 3 * 10;
            var original = Enumerable.Range(0, length).Select(x => (byte)(int)(128 * Math.Sin(x / 100.0))).ToArray();
            DoTest(original);
        }

        [Test]
        public void SinDelta()
        {
            const int length = 1920 * 3 * 10;
            var original = Enumerable.Range(0, length).Select(x => (byte)(int)(128 * Math.Sin(x / 100.0))).ToArray();
            fixed (byte* pOriginal = original)
                DeltaCoding.Apply(pOriginal, length);
            DoTest(original);
        }

        [Test]
        public void Random()
        {
            var random = new Random();
            DoTest(Enumerable.Range(0, 1920 * 3 * 10).Select(x => (byte)random.Next(0, 256)).ToArray());
        }
    }
}