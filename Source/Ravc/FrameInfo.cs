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

using Ravc.Encoding;

namespace Ravc
{
    public class FrameInfo
    {
        public FrameType Type { get; private set; }
        public float Timestamp { get; private set; }
        public int MostDetailedMip { get; private set; }
        public int ColorDiffThreshold { get; private set; }
        public int OriginalWidth { get; private set; }
        public int OriginalHeight { get; private set; }
        public int MouseX { get; private set; }
        public int MouseY { get; private set; }

        public int AlignedWidth { get; private set; }
        public int AlignedHeight { get; private set; }
        public int UncompressedSize { get; private set; }

        public FrameInfo(FrameType type, float timestamp, int mostDetailedMip, int colorDiffThreshold, int width, int height, int mouseX, int mouseY)
        {
            Type = type;
            Timestamp = timestamp;
            MostDetailedMip = mostDetailedMip;
            ColorDiffThreshold = colorDiffThreshold;
            OriginalWidth = width;
            OriginalHeight = height;
            MouseX = mouseX;
            MouseY = mouseY;

            AlignedWidth = AlignDimension(width);
            AlignedHeight = AlignDimension(height);
            UncompressedSize = CalculateUncompressedSize(AlignedWidth, AlignedHeight, mostDetailedMip);
        }

        private static int AlignDimension(int x)
        {
            int mod = x % EncodingConstants.DimensionAlignment;
            return mod == 0 ? x : x + EncodingConstants.DimensionAlignment - mod;
        }

        private static int CalculateUncompressedSize(int alignedWidth, int alignedHeight, int mostDetailedMip)
        {
            int result = 0;
            for (int i = mostDetailedMip; i < EncodingConstants.MipLevels; i++)
                result += (alignedWidth >> i) * (alignedHeight >> i) * 4;
            return result;
        }
    }
}