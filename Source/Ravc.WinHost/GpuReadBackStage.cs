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
        }

        private readonly IDevice device;
        private readonly ByteArrayPool byteArrayPool;
        private readonly StagingFrame[] stagingTexChain;
        private readonly int formatId;
        private IPipelinedConsumer<UncompressedFrame> nextStage;
        private int backIndex;
        private int frontIndex;

        public IPipelinedConsumer<UncompressedFrame> NextStage { set { nextStage = value; } }
        public bool IsOverloaded { get { return nextStage.IsOverloaded; } }

        public GpuReadBackStage(IDevice device, ByteArrayPool byteArrayPool, int chainLength)
        {
            this.device = device;
            this.byteArrayPool = byteArrayPool;
            stagingTexChain = new StagingFrame[chainLength];
            formatId = device.Adapter.GetSupportedFormats(FormatSupport.Texture2D).First(x => x.ExplicitFormat == ExplicitFormat.R8G8B8A8_UNORM).ID;
            frontIndex = 1 % chainLength;
        }

        public void Dispose()
        {
            foreach (var stagingFrame in stagingTexChain.Where(t => t.StagingTex != null))
                stagingFrame.StagingTex.Dispose();
        }

        public unsafe void Consume(GpuEncodedFrame input)
        {
            var context = device.ImmediateContext;

            var frontStagingFrame = stagingTexChain[frontIndex];
            var frontTex = frontStagingFrame.StagingTex;
            if (frontTex != null)
            {
                int frontWidth = frontTex.Width;
                int frontHeight = frontTex.Height;
                var dataRowSize = frontWidth * 4;
                var dataPooled = byteArrayPool.Extract(frontHeight * dataRowSize);

                var mapInfo = context.Map(frontTex, 0, MapType.Read, MapFlags.None);
                fixed (byte* pData = dataPooled.Item)
                {
                    var pDataLocal = pData;
                    int rowSizeToCopy = Math.Min(dataRowSize, mapInfo.RowPitch);
                    Parallel.For(0, frontHeight, i =>
                        Memory.CopyBulk(pDataLocal + i * dataRowSize, (byte*)mapInfo.Data + i * mapInfo.RowPitch, rowSizeToCopy));
                }
                context.Unmap(frontTex, 0);

                var uncompressedFrame = new UncompressedFrame(frontStagingFrame.Info, dataPooled);
                nextStage.Consume(uncompressedFrame);
            }

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
                    MipLevels = 1,
                    Sampling = Sampling.NoMultisampling,
                    FormatID = formatId,
                    Usage = Usage.Staging,
                });
                stagingTexChain[backIndex].StagingTex = backTex;
            }
            stagingTexChain[backIndex].Info = input.Info;

            context.CopySubresourceRegion(backTex, 0, 0, 0, 0, diffTex, 0, null);

            diffPooled.Release();

            backIndex = frontIndex;
            frontIndex = (frontIndex + 1) % stagingTexChain.Length;
        }
    }
}