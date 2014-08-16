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

namespace Ravc.Encoding.Transforms
{
    public static unsafe class DottedEncoding
    {
        private enum BlockType
        {
            Zero4 = 0,
            Zero12 = 1,
            Zero20 = 2,
            Zero28 = 3,
            RedOnly3 = 4,
            GreenOnly3 = 5,
            BlueOnly3 = 6,
            AllColors8 = 7,
            AllColors16 = 8,
            AllColors24 = 9
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Pixel
        {
            public sbyte Red;
            public sbyte Green;
            public sbyte Blue;
            public sbyte Alpha;
        }

        public const int ReadLutSize = 256 * sizeof(int);

        public static void BuildReadLut(byte* destination)
        {
            for (int b = -3; b < 3; b++)
            for (int g = -3; g < 4; g++)
            for (int r = -3; r < 3; r++)
            {
                destination[0] = (byte)r;
                destination[1] = (byte)g;
                destination[2] = (byte)b;
                destination[3] = 0;
                destination += 4;
            }
        }

        public static int Apply(byte* destination, byte* source, int sizeInPixels)
        {
            if (sizeInPixels % 4 != 0)
                throw new NotSupportedException("Dotted encoding only supports parts that are divisible by 4");

            byte* destinationStart = destination;
            byte* border = source + 4 * sizeInPixels;

            while (source < border)
            {
                var pixelInt0 = ((int*)source)[0];
                var pixelInt1 = ((int*)source)[1];
                var pixelInt2 = ((int*)source)[2];
                var pixelInt3 = ((int*)source)[3];
                source += 4 * 4;

                if (pixelInt0 == 0 && pixelInt1 == 0 && pixelInt2 == 0 && pixelInt3 == 0)
                {
                    int zeroCountMinus4 = 0;
                    while (source < border && * (int*)source == 0)
                    {
                        zeroCountMinus4++;
                        source += 4;
                    }

                    if (zeroCountMinus4 < (1 << 4))
                    {
                        *destination = (byte)((int)BlockType.Zero4 | (zeroCountMinus4 << 4));
                        destination++;
                    }
                    else if (zeroCountMinus4 < (1 << 12))
                    {
                        *(ushort*)destination = (ushort)((int)BlockType.Zero12 | (zeroCountMinus4 << 4));
                        destination += 2;
                    }
                    else if (zeroCountMinus4 < (1 << 20))
                    {
                        *(int*)destination = (int)BlockType.Zero20 | (zeroCountMinus4 << 4);
                        destination += 3;
                    }
                    else // if (zeroCountMinus4 < (1 << 28))
                    {
                        *(int*)destination = (int)BlockType.Zero28 | (zeroCountMinus4 << 4);
                        destination += 4;
                    }
                }
                else
                {
                    var blockStart = destination;
                    destination++;
                    int zeroMask = 0;

                    const int redMask = 0x000000FF;
                    const int greenMask = 0x0000FF00;
                    const int blueMask = 0x00FF0000;
                    const int alphaMask = 0x7F000000;

                    //FillAlpha(ref pixelInt0);
                    //FillAlpha(ref pixelInt1);
                    //FillAlpha(ref pixelInt2);
                    //FillAlpha(ref pixelInt3);

                    int allAlphas = (pixelInt0 & alphaMask) | ((pixelInt1 & alphaMask) >> 8) | ((pixelInt2 & alphaMask) >> 16) | ((pixelInt3 & alphaMask) >> 24);

                    if (allAlphas == 0)
                    {
                        if (pixelInt0 != 0)
                        {
                            var pixel = *(Pixel*)&pixelInt0;
                            *destination = (byte)((pixel.Red + 3) + (pixel.Green + 3) * 6 + (pixel.Blue + 3) * 42);
                            destination++;
                            zeroMask |= 0x10;
                        }
                        if (pixelInt1 != 0)
                        {
                            var pixel = *(Pixel*)&pixelInt1;
                            *destination = (byte)((pixel.Red + 3) + (pixel.Green + 3) * 6 + (pixel.Blue + 3) * 42);
                            destination++;
                            zeroMask |= 0x20;
                        }
                        if (pixelInt2 != 0)
                        {
                            var pixel = *(Pixel*)&pixelInt2;
                            *destination = (byte)((pixel.Red + 3) + (pixel.Green + 3) * 6 + (pixel.Blue + 3) * 42);
                            destination++;
                            zeroMask |= 0x40;
                        }
                        if (pixelInt3 != 0)
                        {
                            var pixel = *(Pixel*)&pixelInt3;
                            *destination = (byte)((pixel.Red + 3) + (pixel.Green + 3) * 6 + (pixel.Blue + 3) * 42);
                            destination++;
                            zeroMask |= 0x80;
                        }

                        *blockStart = (byte)((int)BlockType.AllColors8 | zeroMask);
                    }
                    else
                    {
                        if (pixelInt0 != 0)
                        {
                            *(int*)destination = pixelInt0;
                            destination += 3;
                            zeroMask |= 0x10;
                        }
                        if (pixelInt1 != 0)
                        {
                            *(int*)destination = pixelInt1;
                            destination += 3;
                            zeroMask |= 0x20;
                        }
                        if (pixelInt2 != 0)
                        {
                            *(int*)destination = pixelInt2;
                            destination += 3;
                            zeroMask |= 0x40;
                        }
                        if (pixelInt3 != 0)
                        {
                            *(int*)destination = pixelInt3;
                            destination += 3;
                            zeroMask |= 0x80;
                        }

                        *blockStart = (byte)((int)BlockType.AllColors24 | zeroMask);
                    }
                }
            }

            return (int)(destination - destinationStart + 3);
        }

        private static void FillAlpha(ref int pixel)
        {
            var red = (sbyte)(pixel & 0xFF);
            var green = (sbyte)((pixel >> 8) & 0xFF);
            var blue = (sbyte)((pixel >> 16) & 0xFF);
            if (-3 > red || red > 2 || -3 > green || green > 3 || -3 > blue || blue > 2)
                pixel |= 0x01000000;
            else
                pixel &= 0x00FFFFFF;
        }

        public static void Revert(byte* destination, byte* source, byte* lut, int sizeInPixels)
        {
            var lutInt = (int*)lut;
            byte* border = destination + sizeInPixels * 4;

            while (destination < border)
            {
                var blockHeader = *(int*)source;
                var blockType = (BlockType)(blockHeader & 0x0F);

                switch (blockType)
                {
                    case BlockType.Zero4:
                    {
                        source++;
                        int zeroCount = ((blockHeader >> 4) & 0x0F) + 4;
                        byte* localBorder = destination + zeroCount * 4;
                        while (destination < localBorder)
                        {
                            *(int*)destination = 0;
                            destination += 4;
                        }
                        break;
                    }
                    case BlockType.Zero12:
                    {
                        source += 2;
                        int zeroCount = ((blockHeader >> 4) & 0x0FFF) + 4;
                        byte* localBorder = destination + zeroCount * 4;
                        while (destination < localBorder)
                        {
                            *(int*)destination = 0;
                            destination += 4;
                        }
                        break;
                    }
                    case BlockType.Zero20:
                    {
                        source += 3;
                        int zeroCount = ((blockHeader >> 4) & 0x0FFFFF) + 4;
                        byte* localBorder = destination + zeroCount * 4;
                        while (destination < localBorder)
                        {
                            *(int*)destination = 0;
                            destination += 4;
                        }
                        break;
                    }
                    case BlockType.Zero28:
                    {
                        source += 4;
                        int zeroCount = ((blockHeader >> 4) & 0x0FFFFFFF) + 4;
                        byte* localBorder = destination + zeroCount * 4;
                        while (destination < localBorder)
                        {
                            *(int*)destination = 0;
                            destination += 4;
                        }
                        break;
                    }
                    case BlockType.RedOnly3:
                        break;
                    case BlockType.GreenOnly3:
                        break;
                    case BlockType.BlueOnly3:
                        break;
                    case BlockType.AllColors8:
                    {
                        source++;
                        var destinationInt = (int*)destination;
                        if ((blockHeader & 0x10) != 0) { destinationInt[0] = lutInt[*source]; source++; } else { destinationInt[0] = 0; }
                        if ((blockHeader & 0x20) != 0) { destinationInt[1] = lutInt[*source]; source++; } else { destinationInt[1] = 0; }
                        if ((blockHeader & 0x40) != 0) { destinationInt[2] = lutInt[*source]; source++; } else { destinationInt[2] = 0; }
                        if ((blockHeader & 0x80) != 0) { destinationInt[3] = lutInt[*source]; source++; } else { destinationInt[3] = 0; }
                        destination += 16;
                        break;
                    }   
                    case BlockType.AllColors16:
                        break;
                    case BlockType.AllColors24:
                    {
                        source++;
                        var destinationInt = (int*)destination;
                        if ((blockHeader & 0x10) != 0) { destinationInt[0] = *(int*)source & 0x00FFFFFF; source += 3; } else { destinationInt[0] = 0; }
                        if ((blockHeader & 0x20) != 0) { destinationInt[1] = *(int*)source & 0x00FFFFFF; source += 3; } else { destinationInt[1] = 0; }
                        if ((blockHeader & 0x40) != 0) { destinationInt[2] = *(int*)source & 0x00FFFFFF; source += 3; } else { destinationInt[2] = 0; }
                        if ((blockHeader & 0x80) != 0) { destinationInt[3] = *(int*)source & 0x00FFFFFF; source += 3; } else { destinationInt[3] = 0; }
                        destination += 16;
                        break;
                    }
                }
            }
        }
    }
}