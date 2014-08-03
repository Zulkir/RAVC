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
    public class ArithmeticCodingTests : CompressionTransformTestsBase
    {
        protected override unsafe void DoTest(byte[] original)
        {
            var length = original.Length;

            var probability = new ushort[ArithmeticCoding.ProbabilityModelBuildingSize / 2];
            var transformed = new byte[length * 2];
            var reverted = new byte[length];
            int compressedLength;

            fixed (byte* pOriginal = original)
            fixed (byte* pTransformed = transformed)
            fixed (byte* pReverted = reverted)
            fixed (ushort* pProbability = probability)
            {
                ArithmeticCoding.BuildProbabilityModel((byte*)pProbability, pOriginal, length);
                compressedLength = ArithmeticCoding.Apply(pTransformed, pOriginal, (byte*)pProbability, length);
                ArithmeticCoding.Revert(pReverted, pTransformed, (byte*)pProbability, length);
            }

            Assert.That(reverted, Is.EqualTo(original));
            Assert.That(compressedLength, Is.GreaterThanOrEqualTo(Enumerable.Range(0, length * 2).Reverse().FirstOrDefault(x => transformed[x] != 0)));
            Console.WriteLine(compressedLength);
        }
    }
}