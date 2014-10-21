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
    public class GpuFinalDecoder
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

uniform sampler2D DiffTexture;
uniform sampler2D PrevTextureDetailed;
uniform sampler2D PrevTextureMip;

out vec4 out_color;

const float AbsCoef = (255.0f/256.0f);
const float NormCoef = (256.0f/255.0f);
const vec3 One = vec3(1.0, 1.0, 1.0);

float MaxComponent(vec3 value)
{
    return max(max(value.x, value.y), value.z);
}

ivec3 ToInt(vec3 v)
{
    return ivec3(v * 255.999);
}

vec3 ToFloat(ivec3 v)
{
    return vec3(v / 255);
}

void main()
{
    ivec2 pixelCoordDetailed = ivec2(gl_FragCoord.xy);
    ivec2 pixelCoordMip = pixelCoordDetailed / CoordDivisor;
    //ivec2 pixelCoordMip = ivec2(gl_FragCoord.xy / CoordDivisor);

    vec3 previousValueDetailed = texelFetch(PrevTextureDetailed, pixelCoordDetailed, 0).rgb;
    vec3 previousValueMip = texelFetch(PrevTextureMip, pixelCoordMip, 0).rgb;
    vec3 encodedDiff = texelFetch(DiffTexture, pixelCoordMip, 0).rgb;

    vec3 valueMip = NormCoef * mod((AbsCoef * (previousValueMip + encodedDiff)), 1.0);
    vec3 diffMip = valueMip - previousValueMip;

    float diffSize = MaxComponent(abs(diffMip));

    vec3 lossyValue = diffSize < 31.5/255.0 ? clamp(previousValueDetailed + diffMip, vec3(0.0, 0.0, 0.0), vec3(1.0, 1.0, 1.0)) : valueMip;
    //vec3 lossyValue = valueMip;

    out_color = vec4(lossyValue, 1.0);
}
";

        public GpuFinalDecoder(IPclWorkarounds pclWorkarounds, IClientSettings settings, IContext context)
        {
            this.pclWorkarounds = pclWorkarounds;
            fullTextureProcessor = new FullTextureProcessor(settings, context, FragmentShaderText, new[] { "DiffTexture", "PrevTextureDetailed", "PrevTextureMip" });
            framebuffer = context.Create.Framebuffer();
            mipInfoBuffer = context.Create.Buffer(BufferTarget.UniformBuffer, 16, BufferUsageHint.DynamicDraw);
            sampler = context.Create.Sampler();
        }

        public unsafe void Decode(IContext context, ManualMipChain target, ManualMipChain temporalDiff, ManualMipChain previous, int mostDetailedMip)
        {
            var pipeline = context.Pipeline;
            fullTextureProcessor.PrepareContext(context);
            pipeline.Viewports[0].Set(target.Width, target.Height);
            framebuffer.AttachTextureImage(FramebufferAttachmentPoint.Color0, target[0], 0);
            pipeline.Framebuffer = framebuffer;

            Vector4 mipInfoData;
            var mipInfoPtr = (int*)&mipInfoData;
            mipInfoPtr[0] = mostDetailedMip;
            mipInfoPtr[1] = 1 << mostDetailedMip;
            mipInfoBuffer.SetDataByMapping(pclWorkarounds, (IntPtr)mipInfoPtr);

            pipeline.UniformBuffers[0] = mipInfoBuffer;
            pipeline.Textures[0] = temporalDiff[mostDetailedMip];
            pipeline.Samplers[0] = sampler;
            pipeline.Textures[1] = previous[0];
            pipeline.Samplers[1] = sampler;
            pipeline.Textures[2] = previous[mostDetailedMip];
            pipeline.Samplers[2] = sampler;

            context.DrawElements(BeginMode.Triangles, 6, DrawElementsType.UnsignedShort, 0);
            framebuffer.DetachColorStartingFrom(0);
        }
    }
}