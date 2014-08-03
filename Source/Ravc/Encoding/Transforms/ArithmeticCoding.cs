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
    public static unsafe class ArithmeticCoding
    {
        public const int ProbabilityModelBuildingSize = 256 * sizeof(int);

        public static void BuildProbabilityModel(byte* result, byte* data, int length)
        {
            var dataBorder = data + length;
            var resultAsInts = (int*)result;
            var resultAsUshorts = (ushort*)result;

            for (int i = 0; i < 256; i++)
                resultAsInts[i] = 0;

            while (data < dataBorder)
            {
                resultAsInts[*data]++;
                data++;
            }

            int probabilityBorder = 0;
            for (int i = 0; i < 256; i++)
            {
                var range = (int)((double)((long)resultAsInts[i] << 16) / (length));
                if (range == 0)
                    range = 1;
                int highBorder = probabilityBorder + range;
                if (highBorder + 254 - i > ushort.MaxValue)
                    highBorder = ushort.MaxValue - 254 + i;
                resultAsUshorts[i + 1] = (ushort)(highBorder);
                probabilityBorder += range;
            }
            resultAsUshorts[0] = 0;
            resultAsUshorts[256] = 0;
        }

        public static int Apply(byte* destination, byte* source, byte* probabilityModel, int length)
        {
            var destinationStart = destination;
            var border = source + length;
            var lowBorders = (ushort*)probabilityModel;
            var highBorders = lowBorders + 1;

            ushort low = 0;
            ushort highMinusOne = ushort.MaxValue;

            int output = 0;
            int outputBitOffset = 7;
            int underflow = 0;

            while (source < border)
            {
                var sourceValue = *source;
                source++;

                int range = highMinusOne + 1 - low;
                int highBorder = highBorders[sourceValue];
                int highBorderMultipliedByRange = highBorder == 0
                    ? range == (1 << 16)
                        ? (1 << 16)
                        : range
                    : (highBorder * range) >> 16;
                highMinusOne = (ushort)(low + highBorderMultipliedByRange - 1);
                low = (ushort)(low + ((lowBorders[sourceValue] * range) >> 16));
                if (highMinusOne < low)
                    highMinusOne = low;

                while (true)
                {
                    int msb;
                    if ((msb = (low >> 15)) == (highMinusOne >> 15))
                    {
                        output |= msb << outputBitOffset;

                        if (outputBitOffset != 0)
                            outputBitOffset--;
                        else
                        {
                            *destination = (byte)output;
                            destination++;
                            output = 0;
                            outputBitOffset = 7;
                        }

                        while (underflow != 0)
                        {
                            int bitToOutput = msb ^ 1;
                            output |= (bitToOutput << outputBitOffset);

                            if (outputBitOffset != 0)
                                outputBitOffset--;
                            else
                            {
                                *destination = (byte)output;
                                destination++;
                                output = 0;
                                outputBitOffset = 7;
                            }

                            underflow--;
                        }
                    }
                    else if ((low & (1 << 14)) != 0 && (highMinusOne & (1 << 14)) == 0)
                    {
                        underflow++;

                        low &= 0x3fff;
                        highMinusOne |= 0x4000;
                    }
                    else
                    {
                        break;
                    }

                    low <<= 1;
                    highMinusOne = (ushort)((highMinusOne << 1) | 1);
                }
            }

            output |= 1 << outputBitOffset;
            *destination = (byte)output;
            destination++;

            return (int)(destination - destinationStart);
        }

        public static void Revert(byte* destination, byte* source, byte* probabilityModel, int expectedLength)
        {
            var border = destination + expectedLength;
            var lowBorders = (ushort*)probabilityModel;
            var highBorders = lowBorders + 1;

            ushort low = 0;
            ushort highMinusOne = ushort.MaxValue;

            var sourceValue32 = (uint)(*source << 24);
            source++;
            sourceValue32 |= (uint)(*source << 16);
            source++;
            sourceValue32 |= (uint)(*source << 8);
            source++;
            sourceValue32 |= *source;
            source++;

            int sourceBitDebt = 0;

            while (destination < border)
            {
                int range = highMinusOne + 1 - low;
                var temp = (((sourceValue32 | 0xffff) - (uint)(low << 16)) / range);

                // todo optimize with LUT
                byte symbol = 0;
                while (highBorders[symbol] <= temp && symbol != 255)
                    symbol++;

                *destination = symbol;
                destination++;

                int highBorder = highBorders[symbol];
                int highBorderMultipliedByRange = highBorder == 0
                    ? range == (1 << 16)
                        ? (1 << 16)
                        : range
                    : (highBorder * range) >> 16;
                highMinusOne = (ushort)(low + highBorderMultipliedByRange - 1);
                low = (ushort)(low + ((lowBorders[symbol] * range) >> 16));
                if (highMinusOne < low)
                    highMinusOne = low;

                while (true)
                {
                    if ((low >> 15) == (highMinusOne >> 15))
                    {
                    }
                    else if ((low & (1 << 14)) != 0 && (highMinusOne & (1 << 14)) == 0)
                    {
                        low &= 0x3fff;
                        highMinusOne |= 0x4000;
                        sourceValue32 ^= 0x40000000;
                    }
                    else
                    {
                        break;
                    }

                    low <<= 1;
                    highMinusOne = (ushort)((highMinusOne << 1) | 1);
                    sourceValue32 <<= 1;

                    if (sourceBitDebt != 7)
                        sourceBitDebt++;
                    else
                    {
                        sourceValue32 |= *source;
                        source++;
                        sourceBitDebt = 0;
                    }
                }
            }
        }
    }
}