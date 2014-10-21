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
    public class GpuMipCalculator
    {
        private readonly IPclWorkarounds pclWorkarounds;
        private readonly FullTextureProcessor fullTextureProcessor;
        private readonly IFramebuffer framebuffer;
        private readonly IBuffer mipInfoBuffer;
        private readonly ISampler sampler;

        private const string FragmentShaderText =
@"
uniform sampler2D Source;

out vec4 out_color;

ivec3 ToInt(vec3 v)
{
    return ivec3(v * 255.999);
}

vec3 ToFloat(ivec3 v)
{
    return vec3(v) / 255.0;
}

void main()
{
    ivec2 coord = ivec2(gl_FragCoord.xy) * 2;
    vec3 topLeft = texelFetch(Source, coord, 0).rgb;
    vec3 topRight = texelFetch(Source, ivec2(coord.x + 1, coord.y), 0).rgb;
    vec3 bottomLeft = texelFetch(Source, ivec2(coord.x, coord.y + 1), 0).rgb;
    vec3 bottomRight = texelFetch(Source, ivec2(coord.x + 1, coord.y + 1), 0).rgb;
    
    ivec3 sumInt = ToInt(topLeft) + ToInt(topRight) + ToInt(bottomLeft) + ToInt(bottomRight);
    vec3 averageColor = ToFloat((sumInt) / 4);

    out_color = vec4(averageColor, 1.0);
    //out_color = vec4(1.0, 0.0, 0.0, 1.0);
    //out_color = texelFetch(Source, coord, 0);
    //out_color = vec4(0.0, 0.0, 0.0, 1.0);
}
";

        public GpuMipCalculator(IPclWorkarounds pclWorkarounds, IClientSettings settings, IContext context)
        {
            this.pclWorkarounds = pclWorkarounds;
            fullTextureProcessor = new FullTextureProcessor(settings, context, FragmentShaderText, new[] { "Source" });
            framebuffer = context.Create.Framebuffer();
            mipInfoBuffer = context.Create.Buffer(BufferTarget.UniformBuffer, 16, BufferUsageHint.DynamicDraw);
            sampler = context.Create.Sampler();
        }

        public unsafe void GenerateMips(IContext context, ManualMipChain target)
        {
            var pipeline = context.Pipeline;
            fullTextureProcessor.PrepareContext(context);
            pipeline.Framebuffer = framebuffer;
            pipeline.Samplers[0] = sampler;
            pipeline.UniformBuffers[0] = mipInfoBuffer;

            for (int i = 1; i < EncodingConstants.MipLevels; i++)
            {
                pipeline.Viewports[0].Set(target.Width >> i, target.Height >> i);

                Vector4 mipInfoData;
                var mipInfoPtr = (int*)&mipInfoData;
                mipInfoPtr[0] = i;
                mipInfoBuffer.SetDataByMapping(pclWorkarounds, (IntPtr)mipInfoPtr);

                framebuffer.AttachTextureImage(FramebufferAttachmentPoint.Color0, target[i], 0);
                pipeline.Textures[0] = target[i - 1];
                
                context.DrawElements(BeginMode.Triangles, 6, DrawElementsType.UnsignedShort, 0);
                framebuffer.DetachColorStartingFrom(0);
            }
        } 
    }
}