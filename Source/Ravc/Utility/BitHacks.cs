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
using System.Runtime.InteropServices;

namespace Ravc.Utility
{
    public static class BitHacks
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct Lut20
        {
            readonly uint data0; 
            readonly uint data1;
            readonly uint data2;
            readonly uint data3;
            readonly uint data4;
            
            public Lut20(uint d0, uint d1, uint d2, uint d3, uint d4)
            {
                data0 = d0; data1 = d1; data2 = d2; data3 = d3; data4 = d4;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int NextPowerOfTwo(int v)
        {
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v++;
            return v;
        }

        private static readonly Lut20 MsbLut = new Lut20(0xAAAAAAAA, 0xCCCCCCCC, 0xF0F0F0F0, 0xFF00FF00, 0xFFFF0000);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int ExponentOfPowerOfTwo(int v)
        {
            var lut = MsbLut;
            var b = (int*)&lut;
            int r = (v & b[0]) != 0 ? 1 : 0;
            r |= ToByte((v & b[4]) != 0) << 4;
            r |= ToByte((v & b[3]) != 0) << 3;
            r |= ToByte((v & b[2]) != 0) << 2;
            r |= ToByte((v & b[1]) != 0) << 1;
            return r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static byte ToByte(bool b)
        {
            return *(byte*)&b;
        }
    }
}