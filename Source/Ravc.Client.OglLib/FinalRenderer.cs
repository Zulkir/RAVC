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
    public class FinalRenderer
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct Vertex
        {
            public Vector4 Position;
            public Vector4 TexCoord;

            public Vertex(float px, float py, float tx, float ty)
            {
                Position.X = px;
                Position.Y = py;
                Position.Z = 0f;
                Position.W = 0f;

                TexCoord.X = tx;
                TexCoord.Y = ty;
                TexCoord.Z = 0f;
                TexCoord.W = 0f;
            }

            public const int Size = 2 * 4 * sizeof(float);
        }

        private readonly IPclWorkarounds pclWorkarounds;
        private readonly IShaderProgram program;
        private readonly IVertexArray vertexArray;
        private readonly IBuffer dimensionsBuffer;

        private const string DesktopHeader =
@"#version 150";
        private const string EsHeader =
@"#version 300 es

precision highp float;
precision highp sampler2D;";

        private const string VertexShaderText =
@"
layout(std140) uniform DimensionsBuffer
{
    vec2 Dimensions;
};

in vec4 in_position;
in vec4 in_tex_coord;

out vec2 v_tex_coord;

void main()
{
    gl_Position = vec4(in_position.x * Dimensions.x, -in_position.y * Dimensions.y, 0.0f, 1.0f);
    v_tex_coord = in_tex_coord.xy;
}
";

        private readonly ISampler sampler;

        private const string FragmentShaderText =
@"
uniform sampler2D DiffuseTexture;

in vec2 v_tex_coord;

out vec4 out_color;

void main()
{
    vec2 correctTexCoord = vec2(v_tex_coord.x, 1.0 - v_tex_coord.y);
    out_color = texture(DiffuseTexture, correctTexCoord);
    //out_color = vec4(correctTexCoord, 0.0, 1.0);
}
";

        public FinalRenderer(IPclWorkarounds pclWorkarounds, IClientSettings settings, IContext context)
        {
            this.pclWorkarounds = pclWorkarounds;
            var header = settings.IsEs ? EsHeader : DesktopHeader;
            var vertexShader = context.Create.VertexShader(header + VertexShaderText);
            var fragmentShader = context.Create.FragmentShader(header + FragmentShaderText);
            program = context.Create.Program(new ShaderProgramDescription
            {
                VertexShaders = new[] { vertexShader },
                FragmentShaders = new[] { fragmentShader },
                VertexAttributeNames = new[] { "in_position", "in_tex_coord" },
                UniformBufferNames = new[] { "DimensionsBuffer" },
                SamplerNames = new[] { "DiffuseTexture" }
            });

            var vertexBuffer = context.Create.Buffer(BufferTarget.ArrayBuffer, 4 * Vertex.Size, BufferUsageHint.StaticDraw, new[]
            {
                new Vertex(-1f, 1f, 0f, 0f),
                new Vertex(1f, 1f, 1f, 0f),
                new Vertex(1f, -1f, 1f, 1f),
                new Vertex(-1f, -1f, 0f, 1f)
            });

            var elementArrayBuffer = context.Create.Buffer(BufferTarget.ElementArrayBuffer, 6 * sizeof(ushort), BufferUsageHint.StaticDraw, new ushort[]
            {
                0, 1, 2, 0, 2, 3
            });

            vertexArray = context.Create.VertexArray();
            vertexArray.SetVertexAttributeF(0, vertexBuffer, VertexAttributeDimension.Two, VertexAttribPointerType.Float, false, Vertex.Size, 0);
            vertexArray.SetVertexAttributeF(1, vertexBuffer, VertexAttributeDimension.Two, VertexAttribPointerType.Float, false, Vertex.Size, 4 * sizeof(float));
            vertexArray.SetElementArrayBuffer(elementArrayBuffer);

            dimensionsBuffer = context.Create.Buffer(BufferTarget.UniformBuffer, 4 * sizeof(float), BufferUsageHint.DynamicDraw);

            sampler = context.Create.Sampler();
            sampler.SetMinFilter(TextureMinFilter.Linear);
            sampler.SetMagFilter(TextureMagFilter.Linear);
			sampler.SetWrapR(TextureWrapMode.ClampToEdge);
			sampler.SetWrapS(TextureWrapMode.ClampToEdge);
			sampler.SetWrapT(TextureWrapMode.ClampToEdge);
        }

        public unsafe void Render(IContext context, IRavcGameWindow gameWindow, ITexture2D texture)
        {
            var pipeline = context.Pipeline;

            pipeline.Program = program;
            pipeline.VertexArray = vertexArray;

            pipeline.Viewports[0].Set(gameWindow.ClientWidth, gameWindow.ClientHeight);
            pipeline.Rasterizer.SetDefault();
            pipeline.Rasterizer.MultisampleEnable = false;

            pipeline.Framebuffer = null;

            Vector4 dimensions = CalculateDimensions(texture, gameWindow.ClientWidth, gameWindow.ClientHeight);
            dimensionsBuffer.SetDataByMapping(pclWorkarounds, (IntPtr)(&dimensions));
            pipeline.UniformBuffers[0] = dimensionsBuffer;

            pipeline.Textures[0] = texture;
            pipeline.Samplers[0] = sampler;

            pipeline.DepthStencil.SetDefault();
            pipeline.DepthStencil.DepthMask = false;

            pipeline.Blend.SetDefault(false);

            context.DrawElements(BeginMode.Triangles, 6, DrawElementsType.UnsignedShort, 0);
        }

        private static Vector4 CalculateDimensions(ITexture2D texture, int windowWidth, int windowHeight)
        {
            var windowAspectRatio = (float)windowWidth / Math.Max(windowHeight, 1);
            var textureAspectRatio = (float)texture.Width / Math.Max(texture.Height, 1);

            if (windowAspectRatio > textureAspectRatio)
                return new Vector4(textureAspectRatio / windowAspectRatio, 1.0f, 0.0f, 0.0f);
            if (windowAspectRatio < textureAspectRatio)
                return new Vector4(1.0f, windowAspectRatio / textureAspectRatio, 0.0f, 0.0f);
            return new Vector4(1.0f, 1.0f, 0.0f, 0.0f);
        }
    }
}