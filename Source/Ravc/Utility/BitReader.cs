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

using System.Runtime.CompilerServices;

namespace Ravc.Utility
{
    public unsafe struct BitReader
    {
        private byte* internalPointer;
        private int buffer;
        private int bufferedBits;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset(byte* pointer)
        {
            buffer = 0;
            internalPointer = pointer;
            bufferedBits = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte* GetNextBytePointer()
        {
            return internalPointer - (bufferedBits >> 3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte PeekByte()
        {
            EnsureHasByte();
            return (byte)buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SkipBits(int bitCount)
        {
            while (bitCount > 8)
            {
                EnsureHasByte();
                buffer >>= 8;
                bufferedBits -= 8;
                bitCount -= 8;
            }

            if (bitCount != 0)
            {
                EnsureHasByte();
                buffer >>= bitCount;
                bufferedBits -= bitCount;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureHasByte()
        {
            if (bufferedBits < 8)
            {
                buffer |= (*internalPointer << bufferedBits);
                internalPointer++;
                bufferedBits += 8;
            }
        }
    }
}