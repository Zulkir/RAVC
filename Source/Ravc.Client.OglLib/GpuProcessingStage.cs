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
using ObjectGL.Api;
using ObjectGL.Api.Objects.Resources;
using Ravc.Client.OglLib.Pcl;
using Ravc.Encoding;
using Ravc.Utility;
using Ravc.Utility.DataStructures;

namespace Ravc.Client.OglLib
{
    public class GpuProcessingStage : IPipelineStage<UncompressedFrame, GpuSideFrame>
    {
        private readonly IPclWorkarounds pclWorkarounds;
        private readonly IClientStatistics statistics;
        private readonly IContext context;

        private readonly TexturePool texturePool;
        private readonly GpuFinalDecoder gpuFinalDecoder;
        private readonly GpuSpatialDiffCalculator gpuSpatialDiffCalculator;
        private readonly GpuMipCalculator gpuMipCalculator;
        private readonly GpuDebugger gpuDebugger;
        private readonly ITextureInitializer textureInitializer;
        private readonly TextureRenderer textureRenderer;

        private readonly Stopwatch stopwatch;

        private IPipelinedConsumer<GpuSideFrame> nextStage;
        private IPooled<ManualMipChain> prevTexPooled;
        private IBuffer pixelUnpackBuffer;
        private int width;
        private int height;

        public IPipelinedConsumer<GpuSideFrame> NextStage { set { nextStage = value; } }
        public bool IsOverloaded { get { return nextStage.IsOverloaded; } }

        private ManualMipChain spatialDiffTex;
        private ManualMipChain temporalDiffTex;
        private ManualMipChain workingTex;
        private ManualMipChain blackTex;
        private ManualMipChain debugTex;

        public GpuProcessingStage(IPclWorkarounds pclWorkarounds, IClientStatistics statistics, IClientSettings settings, IContext context, ITextureInitializer textureInitializer, TextureRenderer textureRenderer)
        {
            this.pclWorkarounds = pclWorkarounds;
            this.statistics = statistics;
            this.context = context;
            this.textureInitializer = textureInitializer;
            this.textureRenderer = textureRenderer;
            texturePool = new TexturePool(context, textureInitializer);
            gpuFinalDecoder = new GpuFinalDecoder(pclWorkarounds, settings, context);
            gpuSpatialDiffCalculator = new GpuSpatialDiffCalculator(pclWorkarounds, settings, context);
            gpuMipCalculator = new GpuMipCalculator(pclWorkarounds, settings, context);
            gpuDebugger = new GpuDebugger(pclWorkarounds, settings, context);
            stopwatch = new Stopwatch();
        }

        public unsafe void Consume(UncompressedFrame input)
        {
            var inputInfo = input.Info;
            if (inputInfo.AlignedWidth != width || inputInfo.AlignedHeight != height || inputInfo.Type == FrameType.Absolute)
            {
                width = inputInfo.AlignedWidth;
                height = inputInfo.AlignedHeight;

                if (spatialDiffTex != null)
                    spatialDiffTex.Dispose();
                spatialDiffTex = new ManualMipChain(context, width, height, Format.Rgba8);

                if (temporalDiffTex != null)
                    temporalDiffTex.Dispose();
                temporalDiffTex = new ManualMipChain(context, width, height, Format.Rgba8);

                if (workingTex != null)
                    workingTex.Dispose();
                workingTex = new ManualMipChain(context, width, height, Format.Rgba8);

                if (debugTex != null)
                    debugTex.Dispose();
                debugTex = new ManualMipChain(context, width, height, Format.Rgba8);

                if (blackTex != null)
                    blackTex.Dispose();
                blackTex = new ManualMipChain(context, width, height, Format.Rgba8);
                var blackData = new byte[width * height * 4];
                foreach (var level in blackTex.Levels)
                    level.SetData(0, blackData, FormatColor.Rgba, FormatType.UnsignedByte);

                if (prevTexPooled != null)
                    prevTexPooled.Release();
                prevTexPooled = null;

                if (pixelUnpackBuffer != null)
                    pixelUnpackBuffer.Dispose();
                pixelUnpackBuffer = context.Create.Buffer(BufferTarget.PixelUnpackBuffer, input.Info.UncompressedSize, BufferUsageHint.StreamDraw);
            }

            stopwatch.Restart();
            //pixelUnpackBuffer.SetDataByMapping(pclWorkarounds, input.DataPooled.Item);
            
            int offset = 0;
            fixed (byte* pData = input.DataPooled.Item)
            for (int i = input.Info.MostDetailedMip; i < EncodingConstants.MipLevels; i++)
            {
                //spatialDiffTex.SetData(i, (IntPtr)offset, FormatColor.Rgba, FormatType.UnsignedByte, pixelUnpackBuffer);
                spatialDiffTex.Levels[i].SetData(0, (IntPtr)pData + offset, FormatColor.Rgba, FormatType.UnsignedByte);
                offset += (width >> i) * (height >> i) * 4;
            }
            stopwatch.Stop();
            statistics.OnGpuUpload(stopwatch.Elapsed.Milliseconds);
            
            gpuSpatialDiffCalculator.ApplyDiff(context, temporalDiffTex, spatialDiffTex, blackTex[EncodingConstants.SmallestMip], input.Info.MostDetailedMip);

            var resultPooled = texturePool.Extract(width, height);
            var parentTex = prevTexPooled != null ? prevTexPooled.Item : blackTex;

            gpuFinalDecoder.Decode(context, resultPooled.Item, temporalDiffTex, parentTex, input.Info.MostDetailedMip);

            gpuMipCalculator.GenerateMips(context, resultPooled.Item, input.Info.MostDetailedMip);

            //var debugTexPooled = texturePool.Extract(width, height);
            //gpuDebugger.Process(context, debugTexPooled.Item[0], resultPooled.Item[1], temporalDiffTex[1]);

            if (prevTexPooled != null)
                prevTexPooled.Release();
            prevTexPooled = resultPooled;
            prevTexPooled.IncRefCount();

            var gpuSideFrame = new GpuSideFrame(input.Info, resultPooled);
            nextStage.Consume(gpuSideFrame);

            // remove from production
            //resultPooled.Release();
        }
    }
}