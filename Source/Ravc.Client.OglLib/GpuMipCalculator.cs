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
using System.Runtime.InteropServices;
using ObjectGL.Api;
using ObjectGL.Api.Objects;
using ObjectGL.Api.Objects.Resources;
using Ravc.Client.OglLib.Pcl;
using Ravc.Encoding;

namespace Ravc.Client.OglLib
{
    public class GpuMipCalculator
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
        private readonly IShaderProgram shrinkProgram;
        private readonly IShaderProgram copyProgram;
        private readonly IVertexArray vertexArray;
        private readonly IFramebuffer framebuffer;
        private readonly IBuffer mipInfoBuffer;
        private readonly ISampler sampler;

        private const string DesktopHeader = 
@"#version 400";
        private const string EsHeader =
@"#version 300 es

precision highp float;
precision highp int;
precision highp sampler2D;";

        private const string VertexShaderText =
@"
in vec4 in_position;

void main()
{
    gl_Position = vec4(in_position.x, -in_position.y, 0.0f, 1.0f);
}
";

        private const string ShrinkFragmentShaderText =
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

        private const string CopyFragmentShaderText =
        @"
layout(std140) uniform MipInfoBuffer
{
    int MipLevel;
};

uniform sampler2D Source;

out vec4 out_color;

void main()
{
    out_color = texelFetch(Source, ivec2(gl_FragCoord.xy), MipLevel);
}
";

        public GpuMipCalculator(IPclWorkarounds pclWorkarounds, IClientSettings settings, IContext context)
        {
            this.pclWorkarounds = pclWorkarounds;
            var header = settings.IsEs ? EsHeader : DesktopHeader;
            var vertexShader = context.Create.VertexShader(header + VertexShaderText);
            var shrinkFragmentShader = context.Create.FragmentShader(header + ShrinkFragmentShaderText);
            shrinkProgram = context.Create.Program(new ShaderProgramDescription
            {
                VertexShaders = new[] { vertexShader },
                FragmentShaders = new[] { shrinkFragmentShader },
                VertexAttributeNames = new[] { "in_position" },
                SamplerNames = new[] { "Source" }
            });
            var copyFragmentShader = context.Create.FragmentShader(header + CopyFragmentShaderText);
            copyProgram = context.Create.Program(new ShaderProgramDescription
            {
                VertexShaders = new[] { vertexShader },
                FragmentShaders = new[] { copyFragmentShader },
                VertexAttributeNames = new[] { "in_position" },
                UniformBufferNames = new[] { "MipInfoBuffer" },
                SamplerNames = new[] { "Source" }
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
        }

        public unsafe void GenerateMips(IContext context, ManualMipChain target, int mostDetailedMip)
        {
            //if (mostDetailedMip == 0)
            //    return;

            var pipeline = context.Pipeline;
            pipeline.VertexArray = vertexArray;
            pipeline.Framebuffer = framebuffer;
            pipeline.Rasterizer.SetDefault();
            pipeline.Rasterizer.MultisampleEnable = false;
            pipeline.DepthStencil.SetDefault();
            pipeline.DepthStencil.DepthMask = false;
            pipeline.Blend.SetDefault(false);
            pipeline.Samplers[0] = sampler;
            pipeline.UniformBuffers[0] = mipInfoBuffer;

            //var currentTarget = working;
            //var currentWorking = target;

            for (int i = 1; i < EncodingConstants.MipLevels; i++)
            {
                pipeline.Viewports[0].Set(target.Width >> i, target.Height >> i);

                Vector4 mipInfoData;
                var mipInfoPtr = (int*)&mipInfoData;
                mipInfoPtr[0] = i;
                mipInfoBuffer.SetDataByMapping(pclWorkarounds, (IntPtr)mipInfoPtr);

                pipeline.Program = shrinkProgram;
                framebuffer.AttachTextureImage(FramebufferAttachmentPoint.Color0, target[i], 0);
                pipeline.Textures[0] = target[i - 1];
                
                context.DrawElements(BeginMode.Triangles, 6, DrawElementsType.UnsignedShort, 0);
                framebuffer.DetachColorStartingFrom(0);
            }
        } 
    }
}