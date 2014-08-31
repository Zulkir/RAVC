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
using System.Runtime.InteropServices;
using ObjectGL.Api;
using ObjectGL.Api.Objects;
using ObjectGL.Api.Objects.Resources;
using Ravc.Client.OglLib.Pcl;

namespace Ravc.Client.OglLib
{
    public class GpuSideDecoder
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct Vertex
        {
            public float PositionX;
            public float PositionY;

            public Vertex(float px, float py)
            {
                PositionX = px;
                PositionY = py;
            }

            public const int Size = 2 * sizeof(float);
        }

        private readonly IPclWorkarounds pclWorkarounds;
        private readonly IShaderProgram program;
        private readonly IVertexArray vertexArray;
        private readonly IFramebuffer framebuffer;
        private readonly IBuffer mipInfoBuffer;
        private readonly ISampler sampler;

        private const string DesktopHeader = 
@"#version 150";
        private const string EsHeader =
@"#version 300 es

precision highp float;
precision highp sampler2D;";

        private const string VertexShaderText =
@"
in vec4 in_position;

void main()
{
    gl_Position = vec4(in_position.x, -in_position.y, 0.0f, 1.0f);
}
";

        private const string FragmentShaderText =
@"
layout(std140) uniform MipInfoBuffer
{
    int MipLevel;
    int CoordDivisor;
};

uniform sampler2D DiffTexture;
uniform sampler2D PrevTexture;

out vec4 out_color;

const float AbsCoef = (255.0f/256.0f);
const float NormCoef = (256.0f/255.0f);
const vec3 One = vec3(1.0, 1.0, 1.0);

float MaxComponent(vec3 value)
{
    return max(max(value.x, value.y), value.z);
}

void main()
{
    //ivec2 intCoord = ivec2(gl_FragCoord.xy);
    //vec4 nDiff = texelFetch(DiffTexture, intCoord, 0);
    //vec4 nPrev = texelFetch(PrevTexture, intCoord, 0);
    //out_color = NormCoef * mod((AbsCoef * (nPrev + nDiff)), One);
    //out_color = vec4(1.0, 0.0, 0.0, 1.0);

    ivec2 pixelCoordDetailed = ivec2(gl_FragCoord.xy);
    ivec2 pixelCoordMip = pixelCoordDetailed / CoordDivisor;

    vec3 previousValueDetailed = texelFetch(PrevTexture, pixelCoordDetailed, 0).rgb;
    vec3 previousValueMip = texelFetch(PrevTexture, pixelCoordMip, MipLevel).rgb;
    vec3 encodedDiff = texelFetch(DiffTexture, pixelCoordMip, MipLevel).rgb;

    vec3 valueMip = NormCoef * mod((AbsCoef * (previousValueMip + encodedDiff)), One);
    vec3 diffMip = valueMip - previousValueMip;

    float diffSize = MaxComponent(abs(diffMip));

    vec3 lossyValue = diffSize < 32.0/256.0 ? clamp(previousValueDetailed + diffMip, vec3(0.0, 0.0, 0.0), vec3(1.0, 1.0, 1.0)) : valueMip;
    //vec3 lossyValue = valueMip;
    //vec3 lossyValue = encodedDiff;
    //vec3 lossyValue = texelFetch(DiffTexture, pixelCoordDetailed, 0).rgb;

    out_color = vec4(lossyValue, 1.0);
}
";

        public GpuSideDecoder(IPclWorkarounds pclWorkarounds, IClientSettings settings, IContext context)
        {
            this.pclWorkarounds = pclWorkarounds;
            var header = settings.IsEs ? EsHeader : DesktopHeader;
            var vertexShader = context.Create.VertexShader(header + VertexShaderText);
            var fragmentShader = context.Create.FragmentShader(header + FragmentShaderText);
            program = context.Create.Program(new ShaderProgramDescription
            {
                VertexShaders = new[] { vertexShader },
                FragmentShaders = new[] { fragmentShader },
                VertexAttributeNames = new[] { "in_position" },
                UniformBufferNames = new[] { "MipInfoBuffer" },
                SamplerNames = new[] { "DiffTexture", "PrevTexture" }
            });

            var vertexBuffer = context.Create.Buffer(BufferTarget.ArrayBuffer, 4 * Vertex.Size, BufferUsageHint.StaticDraw, new[]
            {
                new Vertex(-1f, 1f),
                new Vertex(1f, 1f),
                new Vertex(1f, -1f),
                new Vertex(-1f, -1f)
            });

            var elementArrayBuffer = context.Create.Buffer(BufferTarget.ElementArrayBuffer, 6 * sizeof(ushort), BufferUsageHint.StaticDraw, new ushort[]
            {
                0, 1, 2, 0, 2, 3
            });

            vertexArray = context.Create.VertexArray();
            vertexArray.SetVertexAttributeF(0, vertexBuffer, VertexAttributeDimension.Two, VertexAttribPointerType.Float, false, Vertex.Size, 0);
            vertexArray.SetElementArrayBuffer(elementArrayBuffer);

            framebuffer = context.Create.Framebuffer();

            mipInfoBuffer = context.Create.Buffer(BufferTarget.UniformBuffer, 16, BufferUsageHint.DynamicDraw);

            sampler = context.Create.Sampler();
            sampler.SetMinFilter(TextureMinFilter.Nearest);
            sampler.SetMagFilter(TextureMagFilter.Nearest);
            sampler.SetWrapR(TextureWrapMode.Clamp);
            sampler.SetWrapS(TextureWrapMode.Clamp);
            sampler.SetWrapT(TextureWrapMode.Clamp);
        }

        public unsafe void Decode(IContext context, ITexture2D target, ITexture2D texture, ITexture2D parentTexture, int mostDetailedMip)
        {
            var pipeline = context.Pipeline;

            pipeline.Program = program;
            pipeline.VertexArray = vertexArray;

            pipeline.Viewports[0].Set(target.Width, target.Height);
            pipeline.Rasterizer.SetDefault();
            pipeline.Rasterizer.MultisampleEnable = false;

            framebuffer.AttachTextureImage(FramebufferAttachmentPoint.Color0, target, 0);
            pipeline.Framebuffer = framebuffer;

            Vector4 mipInfoData;
            var mipInfoPtr = (int*)&mipInfoData;
            mipInfoPtr[0] = mostDetailedMip;
            mipInfoPtr[1] = 1 << mostDetailedMip;
            mipInfoBuffer.SetDataByMapping(pclWorkarounds, (IntPtr)mipInfoPtr);

            pipeline.UniformBuffers[0] = mipInfoBuffer;
            pipeline.Textures[0] = texture;
            pipeline.Samplers[0] = sampler;
            pipeline.Textures[1] = parentTexture;
            pipeline.Samplers[1] = sampler;

            pipeline.DepthStencil.SetDefault();
            pipeline.DepthStencil.DepthMask = false;

            pipeline.Blend.SetDefault(false);

            //framebuffer.ClearColor(0, new Color4(0, 1, 0, 1));
            context.DrawElements(BeginMode.Triangles, 6, DrawElementsType.UnsignedShort, 0);
            framebuffer.DetachColorStartingFrom(0);
        }
    }
}