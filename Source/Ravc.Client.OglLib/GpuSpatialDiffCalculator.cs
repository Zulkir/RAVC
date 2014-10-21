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
using ObjectGL.Api;
using ObjectGL.Api.Objects;
using ObjectGL.Api.Objects.Resources;
using Ravc.Client.OglLib.Pcl;
using Ravc.Encoding;

namespace Ravc.Client.OglLib
{
    public class GpuSpatialDiffCalculator
    {
        private readonly IPclWorkarounds pclWorkarounds;
        private readonly FullTextureProcessor fullTextureProcessor;
        private readonly IFramebuffer framebuffer;
        private readonly IBuffer stepInfoBuffer;
        private readonly ISampler avgDiffsampler;
        private readonly ISampler localDiffsampler;

        private const string FragmentShaderText =
@"
uniform sampler2D AverageDiffTexture;
uniform sampler2D LocalDiffTexture;

out vec4 out_color;

const float AbsCoef = (255.0f/256.0f);
const float NormCoef = (256.0f/255.0f);
const vec4 One = vec4(1.0, 1.0, 1.0, 1.0);

void main()
{
    ivec2 intCoord = ivec2(gl_FragCoord.xy);
    vec4 nAvgDiff = texelFetch(AverageDiffTexture, intCoord / 2, 0);
    vec4 nLocalDiff = texelFetch(LocalDiffTexture, intCoord, 0);
    out_color = NormCoef * mod((AbsCoef * (nAvgDiff + nLocalDiff)), One);
    //out_color = vec4(1.0, 0.0, 0.0, 1.0);
}
";

        public GpuSpatialDiffCalculator(IPclWorkarounds pclWorkarounds, IClientSettings settings, IContext context)
        {
            this.pclWorkarounds = pclWorkarounds;
            fullTextureProcessor = new FullTextureProcessor(settings, context, FragmentShaderText, new[] { "AverageDiffTexture", "LocalDiffTexture" });
            framebuffer = context.Create.Framebuffer();
            stepInfoBuffer = context.Create.Buffer(BufferTarget.UniformBuffer, 16, BufferUsageHint.StaticDraw);
            avgDiffsampler = context.Create.Sampler();
            localDiffsampler = context.Create.Sampler();
        }

        public unsafe void ApplyDiff(IContext context, ManualMipChain target, ManualMipChain spatialDiff, ITexture2D black, int mostDetailedMip)
        {
            var pipeline = context.Pipeline;
            fullTextureProcessor.PrepareContext(context);

            pipeline.Framebuffer = framebuffer;
            pipeline.UniformBuffers[0] = stepInfoBuffer;
            pipeline.Samplers[0] = avgDiffsampler;
            pipeline.Samplers[1] = localDiffsampler;

            for (int i = EncodingConstants.SmallestMip; i >= mostDetailedMip; i--)
            {
                Vector4 stepInfoBufferData;
                *(int*)&stepInfoBufferData = i;
                stepInfoBuffer.SetDataByMapping(pclWorkarounds, (IntPtr)(&stepInfoBufferData));
            
                framebuffer.AttachTextureImage(FramebufferAttachmentPoint.Color0, target[i], 0);
                pipeline.Viewports[0].Set(spatialDiff.Width >> i, spatialDiff.Height >> i);
                pipeline.Textures[0] = i == EncodingConstants.SmallestMip ? black : target[i + 1];
                pipeline.Textures[1] = spatialDiff[i];

                context.DrawElements(BeginMode.Triangles, 6, DrawElementsType.UnsignedShort, 0);
            }

            framebuffer.DetachColorStartingFrom(0);
        }
    }
}