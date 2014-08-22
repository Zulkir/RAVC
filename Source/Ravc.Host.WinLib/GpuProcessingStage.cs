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
using Beholder.Libraries.SharpDX11.Core;
using Beholder.Platform;
using Beholder.Resources;
using Ravc.Utility;
using System.Linq;
using Ravc.Utility.DataStructures;

namespace Ravc.Host.WinLib
{
    public class GpuProcessingStage : IPipelineStage<GpuRawFrame, GpuEncodedFrame>, IDisposable
    {
        private readonly IDevice device;
        private readonly int formatRgbaTypelessId;
        private readonly TexturePool texturePool;
        private readonly GpuChannelSwapper gpuChannelSwapper;
        private readonly GpuTemporalDiffCalculator gpuTemporalDiffCalculator;
        private readonly GpuDiffMipGenerator gpuDiffMipGenerator;
        private readonly GpuSpatialDiffCalculator gpuSpatialDiffCalculator;
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
            formatRgbaTypelessId = device.Adapter.GetSupportedFormats(FormatSupport.Texture2D).First(x => x.ExplicitFormat == ExplicitFormat.R8G8B8A8_TYPELESS).ID;
            texturePool = new TexturePool(device, formatRgbaTypelessId, Usage.Default, BindFlags.ShaderResource | BindFlags.RenderTarget | BindFlags.UnorderedAccess, MiscFlags.GenerateMips);
            gpuChannelSwapper = new GpuChannelSwapper(device);
            gpuTemporalDiffCalculator = new GpuTemporalDiffCalculator(device);
            gpuDiffMipGenerator = new GpuDiffMipGenerator(device);
            gpuSpatialDiffCalculator = new GpuSpatialDiffCalculator(device);

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
            texturePool.Dispose();
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

            //context.PixelStage.ShaderResources[0] = null;
            //context.ConsumeDrawPipeline();

            var copyPooled = texturePool.Extract(width, height);
            var copyTex = copyPooled.Item;
            gpuChannelSwapper.SwapBgraToRgba(context, copyTex, capturedFrameTex);

            ((CDeviceContext)context).D3DDeviceContext.ComputeShader.SetUnorderedAccessView(0, null);
            ((CDeviceContext)context).D3DDeviceContext.ComputeShader.SetShaderResource(0, null);

            capturedFramePooled.Release();

            var temporalDiffPooled = texturePool.Extract(width, height);
            var temporalDiffTex = temporalDiffPooled.Item;
            gpuTemporalDiffCalculator.CalculateDiff(context, temporalDiffTex, copyTex, prevFrameTexPooled != null ? prevFrameTexPooled.Item : blackTex);

            ((CDeviceContext)context).D3DDeviceContext.ComputeShader.SetUnorderedAccessView(0, null);
            ((CDeviceContext)context).D3DDeviceContext.ComputeShader.SetShaderResource(0, null);
            ((CDeviceContext)context).D3DDeviceContext.ComputeShader.SetShaderResource(1, null);

            gpuDiffMipGenerator.GenerateMips(context, temporalDiffTex);

            var spatialDiffPooled = texturePool.Extract(width, height);
            var spatialDiffTex = spatialDiffPooled.Item;
            gpuSpatialDiffCalculator.CalculateDiff(context, spatialDiffTex, temporalDiffTex);

            temporalDiffPooled.Release();
            //spatialDiffPooled.Release();

            var encodedFrame = new GpuEncodedFrame(input.Info, spatialDiffPooled);
            nextStage.Consume(encodedFrame);

            if (prevFrameTexPooled != null)
            {
                prevFrameTexPooled.Release();
                prevFrameTexPooled = null;
            }   
            prevFrameTexPooled = copyPooled;
        }
    }
}