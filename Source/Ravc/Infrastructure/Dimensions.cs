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

namespace Ravc.Infrastructure
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Dimensions : IEquatable<Dimensions>
    {
        public const int Size = 2 * sizeof(int);

        public int Width;
        public int Height;

        public Dimensions(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public bool Equals(Dimensions other)
        {
            return Width == other.Width && Height == other.Height;
        }

        public override bool Equals(object obj)
        {
            return obj is Dimensions && Equals((Dimensions)obj);
        }

        public override int GetHashCode()
        {
            return Width ^ Height;
        }

        public override string ToString()
        {
            return string.Format("{0}x{1}", Width, Height);
        }

        public static bool operator ==(Dimensions d1, Dimensions d2)
        {
            return d1.Width == d2.Width && d1.Height == d2.Height;
        }

        public static bool operator !=(Dimensions d1, Dimensions d2)
        {
            return d1.Width != d2.Width || d1.Height != d2.Height;
        }
    }
}