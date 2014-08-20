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
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Beholder;
using Beholder.Core;
using Beholder.Platform;
using Beholder.Resources;
using Ravc.Encoding;
using Ravc.Utility;
using Ravc.Utility.DataStructures;

namespace Ravc.WinHost
{
    public class GpuReadBackStage : IPipelineStage<GpuEncodedFrame, UncompressedFrame>, IDisposable
    {
        private struct StagingFrame
        {
            public FrameInfo Info;
            public ITexture2D StagingTex;

            public bool HasTexture { get { return StagingTex != null; } }
        }

        private readonly IHostStatistics statistics;
        private readonly IDevice device;
        private readonly ByteArrayPool byteArrayPool;
        private readonly StagingFrame[] stagingTexChain;
        private readonly int formatId;
        private readonly Stopwatch stopwatch;
        private IPipelinedConsumer<UncompressedFrame> nextStage;
        private int backIndex;
        private int frontIndex;

        public IPipelinedConsumer<UncompressedFrame> NextStage { set { nextStage = value; } }
        public bool IsOverloaded { get { return nextStage.IsOverloaded; } }

        public GpuReadBackStage(IHostStatistics statistics, IDevice device, ByteArrayPool byteArrayPool, int chainLength)
        {
            this.statistics = statistics;
            this.device = device;
            this.byteArrayPool = byteArrayPool;
            stagingTexChain = new StagingFrame[chainLength];
            formatId = device.Adapter.GetSupportedFormats(FormatSupport.Texture2D).First(x => x.ExplicitFormat == ExplicitFormat.R8G8B8A8_UNORM).ID;
            stopwatch = new Stopwatch();
            frontIndex = 1 % chainLength;
        }

        public void Dispose()
        {
            foreach (var stagingFrame in stagingTexChain.Where(t => t.StagingTex != null))
                stagingFrame.StagingTex.Dispose();
        }

        public void Consume(GpuEncodedFrame input)
        {
            var context = device.ImmediateContext;

            if (stagingTexChain[frontIndex].HasTexture)
                PushToNextStage(stagingTexChain[frontIndex]);

            var diffPooled = input.DiffPooled;
            var diffTex = input.DiffPooled.Item;

            var backStagingFrame = stagingTexChain[backIndex];
            var backTex = backStagingFrame.StagingTex;
            if (backTex == null || backTex.Width != input.Info.AlignedWidth || backTex.Height != input.Info.AlignedHeight)
            {
                if (backTex != null)
                    backTex.Dispose();
                backTex = device.Create.Texture2D(new Texture2DDescription
                {
                    Width = input.Info.AlignedWidth,
                    Height = input.Info.AlignedHeight,
                    ArraySize = 1,
                    MipLevels = EncodingConstants.MipLevels,
                    Sampling = Sampling.NoMultisampling,
                    FormatID = formatId,
                    Usage = Usage.Staging,
                });
                stagingTexChain[backIndex].StagingTex = backTex;
            }
            stagingTexChain[backIndex].Info = input.Info;

            context.CopyResource(backTex, diffTex);

            diffPooled.Release();

            backIndex = frontIndex;
            frontIndex = (frontIndex + 1) % stagingTexChain.Length;
        }

        private unsafe void PushToNextStage(StagingFrame frontStagingFrame)
        {
            var context = device.ImmediateContext;

            stopwatch.Restart();

            var frontTex = frontStagingFrame.StagingTex;
            var dataPooled = byteArrayPool.Extract(frontStagingFrame.Info.UncompressedSize);

            fixed (byte* pData = dataPooled.Item)
            {
                var pDataMip = pData;
                int width = frontStagingFrame.Info.AlignedWidth;
                int height = frontStagingFrame.Info.AlignedHeight;

                for (int i = 0; i < EncodingConstants.MipLevels; i++)
                {
                    var pDataMipLocal = pDataMip;
                    var dataRowSize = width * 4;

                    var mapInfo = context.Map(frontTex, i, MapType.Read, MapFlags.None);
                    {
                        int rowSizeToCopy = Math.Min(dataRowSize, mapInfo.RowPitch);
                        //for (int r = 0; r < height; r++)
                        Parallel.For(0, height, r =>
                            Memory.CopyBulk(pDataMipLocal + r * dataRowSize, (byte*)mapInfo.Data + r * mapInfo.RowPitch, rowSizeToCopy));
                    }
                    context.Unmap(frontTex, i);

                    pDataMip += height * dataRowSize;
                    width /= 2;
                    height /= 2;
                }
            }

            stopwatch.Stop();
            statistics.OnReadback(stopwatch.Elapsed.TotalMilliseconds);

            var uncompressedFrame = new UncompressedFrame(frontStagingFrame.Info, dataPooled);
            nextStage.Consume(uncompressedFrame);
        }
    }
}