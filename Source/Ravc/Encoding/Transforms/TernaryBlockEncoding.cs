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
using Ravc.Utility;

namespace Ravc.Encoding.Transforms
{
    public static unsafe class TernaryBlockEncoding
    {
        private enum BlockType
        {
            Zeroes,
            ManyZeroes,
            Huffman,
            AsIs
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SymbolWritreInfo
        {
            public ushort BitLength;
            public ushort Code;

            public const int SizeInBytes = 2 * sizeof(ushort);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SymbolReadInfo
        {
            public byte BitLength;
            public byte Symbol;

            public const int SizeInBytes = 2 * sizeof(byte);
        }

        public const int WriteLutSize = 256 * SymbolWritreInfo.SizeInBytes;
        public const int ReadLutSize = 255 * SymbolReadInfo.SizeInBytes;

        public static void BuildWriteLut(byte* destination)
        {
            var destinationAsCodes = (SymbolWritreInfo*)destination;
            destinationAsCodes[0].Code = 1;
            destinationAsCodes[0].BitLength = 2;

            destinationAsCodes[256 - 1].Code = 0;
            destinationAsCodes[256 - 1].BitLength = 2;

            destinationAsCodes[1].Code = 2;
            destinationAsCodes[1].BitLength = 2;

            for (int i = 2; i < 7; i++)
            {
                var suffix = ((1 << i) - 1);
                var bitLength = (ushort)(i + 2);

                destinationAsCodes[256 - i].Code = (ushort)suffix;
                destinationAsCodes[256 - i].BitLength = bitLength;

                destinationAsCodes[i].Code = (ushort)((1 << (i + 1)) | suffix);
                destinationAsCodes[i].BitLength = bitLength;
            }

            destinationAsCodes[7].Code = 127;
            destinationAsCodes[7].BitLength = 8;

            for (int i = 8; i < 256 - 6; i++)
            {
                destinationAsCodes[i].Code = (ushort)((i << 8) | 255);
                destinationAsCodes[i].BitLength = 16;
            }
        }

        public static void BuildReadLut(byte* destination)
        {
            var destinationAsCodes = (SymbolReadInfo*)destination;

            for (int i = 0; i < 256; i += (1 << 2))
            {
                int index = i | 0x1;
                destinationAsCodes[index].Symbol = 0;
                destinationAsCodes[index].BitLength = 2;

                index = i;
                destinationAsCodes[index].Symbol = 256 - 1;
                destinationAsCodes[index].BitLength = 2;

                index = i | 0x2;
                destinationAsCodes[index].Symbol = 1;
                destinationAsCodes[index].BitLength = 2;
            }

            for (int i = 2; i < 7; i++)
            {
                var suffix = ((1 << i) - 1);
                var bitLength = (byte)(i + 2);
                var offset = 1 << bitLength;

                for (int j = 0; j < 256; j += offset)
                {
                    int index = j | suffix;
                    destinationAsCodes[index].Symbol = (byte)(256 - i);
                    destinationAsCodes[index].BitLength = bitLength;

                    index = j | (1 << (i + 1)) | suffix;
                    destinationAsCodes[index].Symbol = (byte)i;
                    destinationAsCodes[index].BitLength = bitLength;
                }
            }

            destinationAsCodes[127].Symbol = 7;
            destinationAsCodes[127].BitLength = 8;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ApplicationContext
        {
            public byte* Source;
            public SymbolWritreInfo* SymbolInfos;
            public byte* Border;
            public byte* NextZeroBlock5;
            public byte* NextZeroBlock17;
            public byte* DominationBorder;
            public BlockType CurrentBlockType;
        }

        private const int MaxBlockSize = (1 << 14) - 1;

        public static int Apply(byte* destination, byte* source, byte* writeLut, int length)
        {
            var context = new ApplicationContext
            {
                Source = source,
                SymbolInfos = (SymbolWritreInfo*)writeLut,
                Border = source + length
            };

            var destinationStart = destination;

            UpdateZeroBlockPredictions(&context);

            if (context.NextZeroBlock5 == context.Source)
                context.CurrentBlockType = BlockType.Zeroes;
            else if (HuffmanPaysOff(&context))
                context.CurrentBlockType = BlockType.Huffman;
            else
                context.CurrentBlockType = BlockType.AsIs;

            var bitWriter = new BitWriter();

            while (context.Source < context.Border)
            {
                var blockStart = (ushort*)destination;
                destination += sizeof(ushort);

                switch (context.CurrentBlockType)
                {
                    case BlockType.Zeroes:
                    {
                        int currentBlockElements = 0;
                        while (context.Border - context.Source >= sizeof(long) && *(long*)context.Source == 0)
                        {
                            context.Source += sizeof(long);
                            currentBlockElements += sizeof(long);
                        }
                        while (context.Border - context.Source >= sizeof(int) && *(int*)context.Source == 0)
                        {
                            context.Source += sizeof(int);
                            currentBlockElements += sizeof(int);
                        }
                        while (context.Border - context.Source >= sizeof(ushort) && *(ushort*)context.Source == 0)
                        {
                            context.Source += sizeof(ushort);
                            currentBlockElements += sizeof(ushort);
                        }
                        while (context.Border - context.Source >= sizeof(byte) && *context.Source == 0)
                        {
                            context.Source++;
                            currentBlockElements++;
                        }

                        if (currentBlockElements > MaxBlockSize)
                        {
                            var bigBlockStart = (uint*)blockStart;
                            destination += (sizeof(uint) - sizeof(ushort));
                            *bigBlockStart = (uint)((int)BlockType.ManyZeroes | (currentBlockElements << 2));
                        }
                        else
                        {
                            *blockStart = (ushort)((int)context.CurrentBlockType | (currentBlockElements << 2));
                        }

                        if (context.NextZeroBlock17 < context.Source)
                            UpdateZeroBlockPredictions(&context);
                        context.CurrentBlockType = HuffmanPaysOff(&context) ? BlockType.Huffman : BlockType.AsIs;
                        break;
                    }
                    case BlockType.Huffman:
                    {
                        bitWriter.Reset(destination);
                        int currentBlockElements = 0;
                        var symbolInfo = context.SymbolInfos[*context.Source];

                        while (true)
                        {
                            bitWriter.Write(symbolInfo.Code, symbolInfo.BitLength);

                            context.Source++;
                            currentBlockElements++;

                            if (context.NextZeroBlock17 < context.Source)
                                UpdateZeroBlockPredictions(&context);

                            if (context.Source == context.NextZeroBlock17)
                                break;

                            symbolInfo = context.SymbolInfos[*context.Source];

                            if (currentBlockElements == MaxBlockSize)
                            {
                                destination = bitWriter.Flush();
                                *blockStart = (ushort)((int)context.CurrentBlockType | (currentBlockElements << 2));
                                blockStart = (ushort*)destination;
                                destination += sizeof(ushort);
                                bitWriter.Reset(destination);
                                currentBlockElements = 0;
                            }

                            if (context.Source < context.DominationBorder)
                                continue;
                            if (symbolInfo.BitLength < 8)
                                continue;
                            if (!HuffmanPaysOff(&context))
                                break;
                        }
                        destination = bitWriter.Flush();
                        *blockStart = (ushort)((int)context.CurrentBlockType | (currentBlockElements << 2));
                        context.CurrentBlockType = context.Source == context.NextZeroBlock17 ? BlockType.Zeroes : BlockType.AsIs;
                        break;
                    }
                    case BlockType.AsIs:
                    {
                        int currentBlockElements = 0;
                        var sourceValue = *context.Source;

                        while (true)
                        {
                            *destination = sourceValue;

                            destination++;
                            context.Source++;
                            currentBlockElements++;

                            if (context.NextZeroBlock5 < context.Source)
                                UpdateZeroBlockPredictions(&context);

                            if (context.Source == context.NextZeroBlock5)
                                break;

                            sourceValue = *context.Source;

                            if (currentBlockElements == MaxBlockSize)
                            {
                                *blockStart = (ushort)((int)context.CurrentBlockType | (currentBlockElements << 2));
                                blockStart = (ushort*)destination;
                                destination += sizeof(ushort);
                                currentBlockElements = 0;
                            }

                            if (context.Source < context.DominationBorder)
                                continue;
                            if (context.SymbolInfos[sourceValue].BitLength >= 8)
                                continue;
                            if (HuffmanPaysOff(&context))
                                break;
                        }
                        *blockStart = (ushort)((int)context.CurrentBlockType | (currentBlockElements << 2));
                        context.CurrentBlockType = context.Source == context.NextZeroBlock5 ? BlockType.Zeroes : BlockType.Huffman;
                        break;
                    }
                }
            }

            return (int)(destination - destinationStart);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void UpdateZeroBlockPrediction5(ApplicationContext* context)
        {
            var blockStart = (byte*)0;
            int blockLength = 0;
            var source = context->Source;

            while (source < context->Border)
            {
                if (*source == 0)
                    switch (blockLength)
                    {
                        case 0:
                            blockStart = source;
                            blockLength = 1;
                            break;
                        case 4:
                            context->NextZeroBlock5 = blockStart;
                            return;
                        default:
                            blockLength++;
                            break;
                    }
                else
                    blockLength = 0;
                source++;
            }

            context->NextZeroBlock5 = context->Border;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void UpdateZeroBlockPredictions(ApplicationContext* context)
        {
            if (context->NextZeroBlock5 < context->Source)
                UpdateZeroBlockPrediction5(context);

            var blockStart = context->NextZeroBlock5;
            int blockLength = 5;
            var source = blockStart + 5;

            while (source < context->Border)
            {
                if (*source == 0)
                    switch (blockLength)
                    {
                        case 0:
                            blockStart = source;
                            blockLength = 1;
                            break;
                        case 16:
                            context->NextZeroBlock17 = blockStart;
                            return;
                        default:
                            blockLength++;
                            break;
                    }
                else
                    blockLength = 0;
                source++;
            }

            context->NextZeroBlock17 = context->Border;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HuffmanPaysOff(ApplicationContext* context)
        {
            var source = context->Source;
            var border = context->NextZeroBlock17 - context->Source > 16 ? context->Source + 16 : context->NextZeroBlock17;

            int huffmanCost = 32;
            int asisCost = context->CurrentBlockType == BlockType.AsIs ? 0 : 16;

            while (source < border)
            {
                huffmanCost += context->SymbolInfos[*source].BitLength;
                asisCost += 8;

                if (huffmanCost < asisCost)
                {
                    context->DominationBorder = source + 1;
                    return true;
                }

                source++;
            }

            context->DominationBorder = source;
            return false;
        }

        public static void Revert(byte* destination, byte* source, byte* readLut, int expectedLength)
        {
            byte* border = destination + expectedLength;
            var symbolInfos = (SymbolReadInfo*)readLut;
            var bitReader = new BitReader();

            while (destination < border)
            {
                var blockInfo = *(ushort*)source;
                var blockType = (BlockType)(blockInfo & 0x3);

                int remainingBlockSize;
                if (blockType == BlockType.ManyZeroes)
                {
                    var bigBlockInfo = *(int*)source;
                    remainingBlockSize = (bigBlockInfo >> 2) & ((1 << 30) - 1);
                    source += 4;
                }
                else
                {
                    remainingBlockSize = (blockInfo >> 2) & ((1 << 14) - 1);
                    source += 2;
                }
                
                switch (blockType)
                {
                    case BlockType.Zeroes:
                    case BlockType.ManyZeroes:
                        {
                            while (remainingBlockSize >= sizeof(int))
                            {
                                *(int*)destination = 0;
                                destination += sizeof(int);
                                remainingBlockSize -= sizeof(int);
                            }
                            while (remainingBlockSize > 0)
                            {
                                *destination = 0;
                                destination++;
                                remainingBlockSize--;
                            }
                            break;
                        }
                    case BlockType.Huffman:
                        {
                            bitReader.Reset(source);
                            while (remainingBlockSize > 0)
                            {
                                byte peekedByte = bitReader.PeekByte();
                                byte value;
                                if (peekedByte == byte.MaxValue)
                                {
                                    bitReader.SkipBits(8);
                                    value = bitReader.PeekByte();
                                    bitReader.SkipBits(8);
                                }
                                else
                                {
                                    var symbolInfo = symbolInfos[peekedByte];
                                    value = symbolInfo.Symbol;
                                    bitReader.SkipBits(symbolInfo.BitLength);
                                }
                                *destination = value;
                                destination++;
                                remainingBlockSize--;
                            }
                            source = bitReader.GetNextBytePointer();
                            break;
                        }
                    case BlockType.AsIs:
                        {
                            while (remainingBlockSize > sizeof(int))
                            {
                                *(int*)destination = *(int*)source;
                                source += sizeof(int);
                                destination += sizeof(int);
                                remainingBlockSize -= sizeof(int);
                            }
                            while (remainingBlockSize > sizeof(ushort))
                            {
                                *(ushort*)destination = *(ushort*)source;
                                source += sizeof(ushort);
                                destination += sizeof(ushort);
                                remainingBlockSize -= sizeof(ushort);
                            }
                            while (remainingBlockSize > 0)
                            {
                                *destination = *source;
                                source++;
                                destination++;
                                remainingBlockSize--;
                            }
                            break;
                        }
                }
            }
        }
    }
}