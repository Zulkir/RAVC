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

namespace Ravc.Client.OglLib
{
    public class GpuDebugger
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
            var header = settings.IsEs ? EsHeader : DesktopHeader;
            var vertexShader = context.Create.VertexShader(header + VertexShaderText);
            var fragmentShader = context.Create.FragmentShader(header + FragmentShaderText);
            program = context.Create.Program(new ShaderProgramDescription
            {
                VertexShaders = new[] { vertexShader },
                FragmentShaders = new[] { fragmentShader },
                VertexAttributeNames = new[] { "in_position" },
                //UniformBufferNames = new[] { "MipInfoBuffer" },
                SamplerNames = new[] { "Texture0", "Texture1" }
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

        public unsafe void Process(IContext context, ITexture2D target, ITexture2D texture0, ITexture2D texture1)
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
            pipeline.Samplers[1] = sampler;
            pipeline.UniformBuffers[0] = mipInfoBuffer;

            //var currentTarget = working;
            //var currentWorking = target;

            pipeline.Viewports[0].Set(target.Width, target.Height);

            Vector4 mipInfoData;
            var mipInfoPtr = (int*)&mipInfoData;
            mipInfoPtr[0] = 0;
            mipInfoPtr[1] = 1;
            mipInfoBuffer.SetDataByMapping(pclWorkarounds, (IntPtr)mipInfoPtr);

            pipeline.Program = program;
            framebuffer.AttachTextureImage(FramebufferAttachmentPoint.Color0, target, 0);
            pipeline.Textures[0] = texture0;
            pipeline.Textures[1] = texture1;

            context.DrawElements(BeginMode.Triangles, 6, DrawElementsType.UnsignedShort, 0);
            framebuffer.DetachColorStartingFrom(0);
        }
    }
}