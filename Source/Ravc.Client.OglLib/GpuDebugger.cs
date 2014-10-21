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

namespace Ravc.Client.OglLib
{
    public class GpuDebugger
    {
        private readonly IPclWorkarounds pclWorkarounds;
        private readonly FullTextureProcessor fullTextureProcessor;
        private readonly IFramebuffer framebuffer;
        private readonly IBuffer mipInfoBuffer;
        private readonly ISampler sampler;

        private const string FragmentShaderText =
@"
layout(std140) uniform MipInfoBuffer
{
    int MipLevel;
    int CoordDivisor;
};

uniform sampler2D Texture0;
uniform sampler2D Texture1;

out vec4 out_color;

float MaxValue(vec3 v)
{
    return max(max(v.x, v.y), v.z);
}

float MinValue(vec3 v)
{
    return min(min(v.x, v.y), v.z);
}

float GetError(float diff)
{
    return diff > 1.5/255.0 && diff < 1.0 ? 1.0 : 0.0;
}

void main()
{
    ivec2 coord = ivec2(gl_FragCoord.xy);
    vec3 fetched = texelFetch(Texture0, coord, 0).rgb;
    //vec3 value = (fetched - vec3(171.0/255.0, 205.0/255.0, 239.0/255.0)) * 64.0;
    //vec3 value = vec3(GetError(fetched.x), GetError(fetched.y), GetError(fetched.z));
    vec3 value = fetched;
    out_color = vec4(value, 1.0);
}
";

        public GpuDebugger(IPclWorkarounds pclWorkarounds, IClientSettings settings, IContext context)
        {
            this.pclWorkarounds = pclWorkarounds;
            fullTextureProcessor = new FullTextureProcessor(settings, context, FragmentShaderText, new[] { "Texture0", "Texture1" });
            framebuffer = context.Create.Framebuffer();
            mipInfoBuffer = context.Create.Buffer(BufferTarget.UniformBuffer, 16, BufferUsageHint.DynamicDraw);
            sampler = context.Create.Sampler();
        }

        public unsafe void Process(IContext context, ITexture2D target, ITexture2D texture0, ITexture2D texture1)
        {
            var pipeline = context.Pipeline;
            fullTextureProcessor.PrepareContext(context);
            pipeline.Framebuffer = framebuffer;
            pipeline.Samplers[0] = sampler;
            pipeline.Samplers[1] = sampler;
            pipeline.UniformBuffers[0] = mipInfoBuffer;
            pipeline.Viewports[0].Set(target.Width, target.Height);

            Vector4 mipInfoData;
            var mipInfoPtr = (int*)&mipInfoData;
            mipInfoPtr[0] = 0;
            mipInfoPtr[1] = 1;
            mipInfoBuffer.SetDataByMapping(pclWorkarounds, (IntPtr)mipInfoPtr);

            framebuffer.AttachTextureImage(FramebufferAttachmentPoint.Color0, target, 0);
            pipeline.Textures[0] = texture0;
            pipeline.Textures[1] = texture1;

            context.DrawElements(BeginMode.Triangles, 6, DrawElementsType.UnsignedShort, 0);
            framebuffer.DetachColorStartingFrom(0);
        }
    }
}