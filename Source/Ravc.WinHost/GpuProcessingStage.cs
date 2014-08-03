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
using Beholder;
using Beholder.Core;
using Beholder.Platform;
using Beholder.Resources;
using Ravc.Utility;
using System.Linq;
using Ravc.Utility.DataStructures;

namespace Ravc.WinHost
{
    public class GpuProcessingStage : IPipelineStage<GpuRawFrame, GpuEncodedFrame>, IDisposable
    {
        private readonly IDevice device;
        private readonly TexturePool entropyTexturePool;
        private readonly TexturePool copyTexturePool;
        private readonly GpuDiffCalculator gpuDiffCalculator;
        private readonly GpuChannelSwapper gpuChannelSwapper;
        private readonly ITexture2D blackTex;
        private IPipelinedConsumer<GpuEncodedFrame> nextStage;
        private int width;
        private int height;
        private IPooled<ITexture2D> prevFrameTexPooled;

        public IPipelinedConsumer<GpuEncodedFrame> NextStage { set { nextStage = value; } }
        public bool IsOverloaded { get { return nextStage.IsOverloaded; } }

        public GpuProcessingStage(IDevice device)
        {
            this.device = device;
            var copyFormatId = device.Adapter.GetSupportedFormats(FormatSupport.Texture2D).First(x => x.ExplicitFormat == ExplicitFormat.R8G8B8A8_TYPELESS).ID;
            var entropyFormatId = device.Adapter.GetSupportedFormats(FormatSupport.Texture2D).First(x => x.ExplicitFormat == ExplicitFormat.R8G8B8A8_TYPELESS).ID;
            copyTexturePool = new TexturePool(device, copyFormatId, Usage.Default, BindFlags.ShaderResource | BindFlags.RenderTarget | BindFlags.UnorderedAccess, MiscFlags.GenerateMips);
            entropyTexturePool = new TexturePool(device, entropyFormatId, Usage.Default, BindFlags.ShaderResource | BindFlags.RenderTarget | BindFlags.UnorderedAccess, MiscFlags.GenerateMips);
            gpuDiffCalculator = new GpuDiffCalculator(device);
            gpuChannelSwapper = new GpuChannelSwapper(device);

            blackTex = device.Create.Texture2D(new Texture2DDescription
            {
                Width = 1,
                Height = 1,
                ArraySize = 1,
                MipLevels = 1,
                Sampling = Sampling.NoMultisampling,
                FormatID = device.Adapter.GetSupportedFormats(FormatSupport.Texture2D).First(x => x.ExplicitFormat == ExplicitFormat.R8G8B8A8_TYPELESS).ID,
                Usage = Usage.Immutable,
                BindFlags = BindFlags.ShaderResource
            }, new[] {new SubresourceData(new byte[4])});
        }

        public void Dispose()
        {
            blackTex.Dispose();
            copyTexturePool.Dispose();
            entropyTexturePool.Dispose();
        }

        public void Consume(GpuRawFrame input)
        {
            var context = device.ImmediateContext;
            var capturedFramePooled = input.TexturePooled;
            var capturedFrameTex = capturedFramePooled.Item;

            if (input.Info.AlignedWidth != width || input.Info.AlignedHeight != height)
            {
                width = input.Info.AlignedWidth;
                height = input.Info.AlignedHeight;

                if (prevFrameTexPooled != null)
                {
                    prevFrameTexPooled.Release();
                    prevFrameTexPooled = null;
                }
            }

            context.PixelStage.ShaderResources[0] = null;
            context.ConsumeDrawPipeline();

            var copyPooled = copyTexturePool.Extract(width, height);
            var copyTex = copyPooled.Item;
            gpuChannelSwapper.SwapBgraToRgba(context, copyTex, capturedFrameTex);

            capturedFramePooled.Release();

            var diffPooled = entropyTexturePool.Extract(width, height);
            var diffTex = diffPooled.Item;
            gpuDiffCalculator.CalculateDiff(context, diffTex, copyTex, prevFrameTexPooled != null ? prevFrameTexPooled.Item : blackTex);

            //var revertedPooled = entropyTexturePool.Extract(width, height);
            //var revertedTex = diffPooled.Item;
            //gpuDiffCalculator.RevertDiff(context, revertedTex, diffTex, copyTex);

            //diffPooled.Release();

            context.ComputeStage.ShaderResources[0] = null;
            context.ComputeStage.ShaderResources[1] = null;
            context.ComputeStage.UnorderedAccessResources[0] = null;
            context.ComputeStage.UnorderedAccessResources[1] = null;
            context.ConsumeDispatchPipeline();

            var encodedFrame = new GpuEncodedFrame(input.Info, diffPooled);
            nextStage.Consume(encodedFrame);

            if (prevFrameTexPooled != null)
                prevFrameTexPooled.Release();
            prevFrameTexPooled = copyPooled;
        }
    }
}