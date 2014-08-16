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
using OpenTK;
using Ravc.Encoding;

namespace Ravc.WinformsOglClient
{
    public class GpuSpatialDiffCalculator
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct Vertex
        {
            public Vector2 Position;

            public Vertex(float px, float py)
            {
                Position.X = px;
                Position.Y = py;
            }

            public const int Size = 2 * sizeof(float);
        }

        private readonly IShaderProgram program;
        private readonly IVertexArray vertexArray;
        private readonly IFramebuffer framebuffer;
        private readonly IBuffer stepInfoBuffer;
        private readonly ISampler avgDiffsampler;
        private readonly ISampler localDiffsampler;

        private const string VertexShaderText =
@"#version 150

in vec2 in_position;

void main()
{
    gl_Position = vec4(in_position.x, -in_position.y, 0.0f, 1.0f);
}
";

        private const string FragmentShaderText =
@"#version 150

layout(std140) uniform StepInfoBuffer
{
    int MipLevel;
};

uniform sampler2D AverageDiffTexture;
uniform sampler2D LocalDiffTexture;

out vec4 out_color;

const ivec4 White = ivec4(256, 256, 256, 256);
const ivec4 HalfWhite = ivec4(128, 128, 128, 128);

uvec4 ToUint(vec4 v)
{
    return uvec4(v * 255.999);
}

vec4 ToFloat(uvec4 v)
{
    return vec4(v) / 255;
}

ivec4 DecodeDiff(uvec4 v)
{
    return ivec4((v + HalfWhite) % White - HalfWhite);
}

uvec4 EncodeDiff(ivec4 v)
{
    return uvec4((v + White) % White);
}

void main()
{
    ivec2 intCoord = ivec2(gl_FragCoord.xy);
    ivec4 avgDiff   = DecodeDiff(ToUint(texelFetch(AverageDiffTexture, intCoord / 2, MipLevel + 1)));
    ivec4 localDiff = DecodeDiff(ToUint(texelFetch(LocalDiffTexture, intCoord, MipLevel)));

    out_color = ToFloat(EncodeDiff(avgDiff + localDiff));

    //out_color = ToFloat(EncodeDiff(localDiff));
    //out_color = MipLevel <= 0 ? ToFloat(EncodeDiff(avgDiff)) : ToFloat(EncodeDiff(localDiff));
    //out_color = vec4(1, 0, 0, 1);
}
";

        public GpuSpatialDiffCalculator(IContext context)
        {
            var vertexShader = context.Create.VertexShader(VertexShaderText);
            var fragmentShader = context.Create.FragmentShader(FragmentShaderText);
            program = context.Create.Program(new ShaderProgramDescription
            {
                VertexShaders = new[] { vertexShader },
                FragmentShaders = new[] { fragmentShader },
                VertexAttributeNames = new[] { "in_position" },
                UniformBufferNames = new[] { "StepInfoBuffer" },
                SamplerNames = new[] { "AverageDiffTexture", "LocalDiffTexture" }
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
            //vertexArray.SetVertexAttributeF(1, vertexBuffer, VertexAttributeDimension.Two, VertexAttribPointerType.Float, false, Vertex.Size, 4 * sizeof(float));
            vertexArray.SetElementArrayBuffer(elementArrayBuffer);

            framebuffer = context.Create.Framebuffer();

            stepInfoBuffer = context.Create.Buffer(BufferTarget.UniformBuffer, 16, BufferUsageHint.StaticDraw);

            avgDiffsampler = context.Create.Sampler();
            localDiffsampler = context.Create.Sampler();
        }

        public unsafe void ApplyDiff(IContext context, ITexture2D target, ITexture2D spatialDiffTexture, ITexture2D workingTexture)
        {
            var pipeline = context.Pipeline;

            pipeline.Program = program;
            pipeline.VertexArray = vertexArray;
            
            pipeline.Rasterizer.SetDefault();
            pipeline.Rasterizer.MultisampleEnable = false;

            pipeline.Framebuffer = framebuffer;

            pipeline.UniformBuffers[0] = stepInfoBuffer;
            
            pipeline.Samplers[0] = avgDiffsampler;
            pipeline.Textures[1] = spatialDiffTexture;
            pipeline.Samplers[1] = localDiffsampler;

            pipeline.DepthStencil.SetDefault();
            pipeline.DepthStencil.DepthMask = false;

            pipeline.Blend.SetDefault(false);

            if (EncodingConstants.MipLevels % 2 == 0)
                Swap(ref target, ref workingTexture);

            for (int i = EncodingConstants.SmallestMip; i >= 0; i--)
            {
                Vector4 stepInfoBufferData;
                *(int*)&stepInfoBufferData = i;
                stepInfoBuffer.SetDataByMapping((IntPtr)(&stepInfoBufferData));
            
                framebuffer.AttachTextureImage(FramebufferAttachmentPoint.Color0, target, i);
                pipeline.Viewports[0].Set(spatialDiffTexture.Width >> i, spatialDiffTexture.Height >> i);
                pipeline.Textures[0] = workingTexture;
            
                context.DrawElements(BeginMode.Triangles, 6, DrawElementsType.UnsignedShort, 0);
            
                Swap(ref target, ref workingTexture);
            }

            framebuffer.DetachColorStartingFrom(0);
        }

        private static void Swap<T>(ref T a, ref T b)
        {
            var t = a;
            a = b;
            b = t;
        }
    }
}