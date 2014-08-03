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

namespace Ravc.Encoding.Transforms
{
    public static unsafe class ZeroLengthEncoding
    {
        public static int Apply(byte* destination, byte* source, int length)
        {
            var border = source + length;
            var border16 = source + length - 1;
            var border32 = source + length - 3;
            var border64 = source + length - 7;
            var desztinationStart = destination;

            bool zeroBlock = true;

            while (source < border)
            {
                var destinationBlockStart = (ushort*)destination;
                var sourceBlockStart = source;
                destination += 2;

                if (zeroBlock)
                {
                    while (source < border64 && *(long*)source == 0)
                        source += sizeof(long);
                    while (source < border32 && *(int*)source == 0)
                        source += sizeof(int);
                    while (source < border16 && *(short*)source == 0)
                        source += sizeof(short);
                    while (source < border && *source == 0)
                        source++;
                }
                else
                {
                    while (source < border)
                    {
                        var sourceValue = *source;
                        if (sourceValue == 0 && *(int*)source == 0)
                            break;
                        *destination = sourceValue;
                        destination++;
                        source++;
                    }
                }

                *destinationBlockStart = (ushort)(source - sourceBlockStart);
                zeroBlock = !zeroBlock;
            }

            return (int)(destination - desztinationStart);
        }

        public static void Revert(byte* destination, byte* source, int expectedLength)
        {
            var border = destination + expectedLength;
            bool zeroBlock = true;

            while (destination < border)
            {
                ushort length = *(ushort*)source;
                var localBorder32 = destination + (length & -4);
                var localBorder16 = destination + (length & -2);
                var localBorder8 = destination + length;

                source += 2;

                if (zeroBlock)
                {
                    while (destination < localBorder32)
                    {
                        *(int*)destination = 0;
                        destination += sizeof(int);
                    }
                    while (destination < localBorder16)
                    {
                        *(short*)destination = 0;
                        destination += sizeof(short);
                    }
                    if (destination < localBorder8)
                    {
                        *destination = 0;
                        destination++;
                    }
                }
                else
                {
                    while (destination < localBorder32)
                    {
                        *(int*)destination = *(int*)source;
                        destination += sizeof(int);
                        source += sizeof(int);
                    }
                    while (destination < localBorder16)
                    {
                        *(short*)destination = *(short*)source;
                        destination += sizeof(short);
                        source += sizeof(short);
                    }
                    if (destination < localBorder8)
                    {
                        *destination = *source;
                        destination++;
                        source++;
                    }
                }

                zeroBlock = !zeroBlock;
            }
        }
    }
}