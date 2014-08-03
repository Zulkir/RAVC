﻿#region License
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
using Ravc.Encoding;
using Ravc.Utility;
using Ravc.Utility.DataStructures;
using BufferTarget = ObjectGL.Api.Objects.Resources.BufferTarget;
using BufferUsageHint = ObjectGL.Api.Objects.Resources.BufferUsageHint;

namespace Ravc.WinformsOglClient
{
    public class GpuProcessingStage : IPipelineStage<UncompressedFrame, GpuSideFrame>
    {
        private readonly IClientStatistics statistics;
        private readonly IContext context;

        private readonly TexturePool texturePool;
        private readonly GpuSideDecoder gpuSideDecoder;
        private readonly ITextureInitializer textureInitializer;
        private readonly ITexture2D blackTex;
        private readonly Stopwatch stopwatch;

        private IPipelinedConsumer<GpuSideFrame> nextStage;
        private IPooled<ITexture2D> prevTexPooled;
        private IBuffer pixelUnpackBuffer;
        private int width;
        private int height;

        public IPipelinedConsumer<GpuSideFrame> NextStage { set { nextStage = value; } }
        public bool IsOverloaded { get { return nextStage.IsOverloaded; } }

        private ITexture2D entropyTex;

        public GpuProcessingStage(IClientStatistics statistics, IContext context, ITextureInitializer textureInitializer)
        {
            this.statistics = statistics;
            this.context = context;
            this.textureInitializer = textureInitializer;
            texturePool = new TexturePool(context, textureInitializer, false);
            gpuSideDecoder = new GpuSideDecoder(context);

            blackTex = context.Create.Texture2D(1, 1, 1, Format.Rgba8);
            stopwatch = new Stopwatch();
        }

        public void Consume(UncompressedFrame input)
        {
            var inputInfo = input.Info;
            if (inputInfo.AlignedWidth != width || inputInfo.AlignedHeight != height || inputInfo.Type == FrameType.Absolute)
            {
                width = inputInfo.AlignedWidth;
                height = inputInfo.AlignedHeight;

                if (entropyTex != null)
                    entropyTex.Dispose();
                entropyTex = context.Create.Texture2D(width, height, 1, Format.Rgba8);
                textureInitializer.InitializeTexture(entropyTex);

                if (prevTexPooled != null)
                    prevTexPooled.Release();
                prevTexPooled = null;

                if (pixelUnpackBuffer != null)
                    pixelUnpackBuffer.Dispose();
                pixelUnpackBuffer = context.Create.Buffer(BufferTarget.PixelUnpackBuffer, width * height * 4, BufferUsageHint.StreamDraw);
            }

            stopwatch.Restart();
            pixelUnpackBuffer.SetDataByMapping(input.DataPooled.Item);
            entropyTex.SetData(0, IntPtr.Zero, FormatColor.Bgra, FormatType.UnsignedByte, pixelUnpackBuffer);
            stopwatch.Stop();
            statistics.OnGpuUpload(stopwatch.Elapsed.Milliseconds);
            

            var resultPooled = texturePool.Extract(width, height);
            var resultTex = resultPooled.Item;
            var parentTex = prevTexPooled != null ? prevTexPooled.Item : blackTex;

            gpuSideDecoder.Decode(context, resultTex, entropyTex, parentTex, width, height);
            
            if (prevTexPooled != null)
                prevTexPooled.Release();
            prevTexPooled = resultPooled;
            prevTexPooled.IncRefCount();

            var gpuSideFrame = new GpuSideFrame(input.Info, resultPooled);
            nextStage.Consume(gpuSideFrame);
        }
    }
}