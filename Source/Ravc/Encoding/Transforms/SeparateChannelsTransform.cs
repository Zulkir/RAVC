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
    public static unsafe class SeparateChannelsTransform
    {
        public static void Apply(byte* destination, byte* source, int width)
        {
            byte* destinationBlue = destination;
            byte* destinationGreen = destination + width;
            byte* destinationRed = destination + 2 * width;

            for (int i = 0; i < width; i++)
            {
                *destinationBlue = source[0];
                *destinationGreen = source[1];
                *destinationRed = source[2];

                destinationBlue++;
                destinationGreen++;
                destinationRed++;
                source += 4;
            }
        }

        public static void Revert(byte* destination, byte* source, int width)
        {
            byte* sourceBlue = source;
            byte* sourceGreen = source + width;
            byte* sourceRed = source + 2 * width;

            for (int i = 0; i < width; i++)
            {
                destination[0] = *sourceBlue;
                destination[1] = *sourceGreen;
                destination[2] = *sourceRed;

                destination += 4;
                sourceBlue++;
                sourceGreen++;
                sourceRed++;
            }
        }
    }
}