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
using ObjectGL.Api.PipelineAspects;

namespace Ravc.Client.OglLib
{
    public class CursorRenderer
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct Vertex
        {
            public float PositionX;
            public float PositionY;
            public float TexCoordX;
            public float TexCoordY;

            public Vertex(float posX, float poxY, float texX, float texY)
            {
                PositionX = posX;
                PositionY = poxY;
                TexCoordX = texX;
                TexCoordY = texY;
            }

            public const int Size = 4 * sizeof(float);
        }

        private const string DesktopHeader =
@"#version 150";
        private const string EsHeader =
@"#version 300 es

precision highp float;
precision highp sampler2D;";

        private const string VertexShaderText =
@"
in vec2 in_position;
in vec2 in_tex_coord;

out vec2 v_tex_coord;

void main()
{
    gl_Position = vec4(in_position.xy, 0.0f, 1.0f);
    v_tex_coord = in_tex_coord;
}
";

        private const string FragmentShaderText =
@"
uniform sampler2D DiffTexture;

in vec2 v_tex_coord;

out vec4 out_color;

void main()
{
    vec3 val = texture(DiffTexture, v_tex_coord).rgb;
    float a = 1.0 - abs(val.r - val.g);
    float v = val.g;
    out_color = vec4(v, v, v, a);
}
";

        private readonly ITexture2D texture;
        private readonly IShaderProgram program;
        private readonly IVertexArray vertexArray;
        private readonly ISampler sampler;

        public CursorRenderer(IClientSettings settings, IContext context, ITextureLoader textureLoader)
        {
            var header = settings.IsEs ? EsHeader : DesktopHeader;
            var vertexShader = context.Create.VertexShader(header + VertexShaderText);
            var fragmentShader = context.Create.FragmentShader(header + FragmentShaderText);
            program = context.Create.Program(new ShaderProgramDescription
            {
                VertexShaders = new[] { vertexShader },
                FragmentShaders = new[] { fragmentShader },
                VertexAttributeNames = new[] { "in_position", "in_tex_coord" },
                SamplerNames = new[] { "DiffTexture" }
            });

            var vertexBuffer = context.Create.Buffer(BufferTarget.ArrayBuffer, 4 * Vertex.Size, BufferUsageHint.StaticDraw, new[]
            {
                new Vertex(-1f, 1f, 0f, 0f),
                new Vertex(1f, 1f, 1f, 0f),
                new Vertex(1f, -1f, 1f, 1f),
                new Vertex(-1f, -1f, 0f, 1f)
            });

            var indexBuffer = context.Create.Buffer(BufferTarget.ElementArrayBuffer, 6 * sizeof(ushort), BufferUsageHint.StaticDraw, new ushort[]
            {
                0, 1, 2, 0, 2, 3
            });

            vertexArray = context.Create.VertexArray();
            vertexArray.SetVertexAttributeF(0, vertexBuffer, VertexAttributeDimension.Two, VertexAttribPointerType.Float, false, Vertex.Size, 0);
            vertexArray.SetVertexAttributeF(1, vertexBuffer, VertexAttributeDimension.Two, VertexAttribPointerType.Float, false, Vertex.Size, 2 * sizeof(float));
            vertexArray.SetElementArrayBuffer(indexBuffer);

            texture = textureLoader.LoadTexture(context, "Cursor.png");
            sampler = context.Create.Sampler();
            sampler.SetMagFilter(TextureMagFilter.Nearest);
        }

        public unsafe void Draw(IContext context, IRavcGameWindow gameWindow, ITexture2D frameTexture, int x, int y)
        {
            var pipeline = context.Pipeline;

            pipeline.Program = program;
            pipeline.VertexArray = vertexArray;

            var windowAspectRatio = (float)gameWindow.ClientWidth / Math.Max(gameWindow.ClientHeight, 1);
            var textureAspectRatio = (float)frameTexture.Width / Math.Max(frameTexture.Height, 1);

            int adjustedTextureWidth = gameWindow.ClientWidth;
            int adjustedTextureHeight = gameWindow.ClientHeight;

            if (windowAspectRatio > textureAspectRatio)
                adjustedTextureWidth = (int)(gameWindow.ClientHeight * textureAspectRatio);
            if (windowAspectRatio < textureAspectRatio)
                adjustedTextureHeight = (int)(gameWindow.ClientWidth / textureAspectRatio);

            int aspectOffsetX = (gameWindow.ClientWidth - adjustedTextureWidth) / 2;
            int aspectOffsetY = (gameWindow.ClientHeight - adjustedTextureHeight) / 2;

            int adjustedSize = texture.Width * adjustedTextureWidth / frameTexture.Width;

            int viewportX = x * adjustedTextureWidth / frameTexture.Width + aspectOffsetX;
            int viewportY = gameWindow.ClientHeight - (y * adjustedTextureHeight / frameTexture.Height + aspectOffsetY) - adjustedSize;
            pipeline.Viewports[0].Set(viewportX, viewportY, adjustedSize, adjustedSize);
            pipeline.Rasterizer.SetDefault();
            pipeline.Rasterizer.MultisampleEnable = false;

            pipeline.Textures[0] = texture;
            pipeline.Samplers[0] = sampler;

            pipeline.DepthStencil.SetDefault();
            pipeline.DepthStencil.DepthMask = false;

            pipeline.Blend.SetDefault(false);
            pipeline.Blend.BlendEnable = true;
            pipeline.Blend.Targets[0].Color.SrcFactor = BlendFactor.SrcAlpha;
            pipeline.Blend.Targets[0].Color.DestFactor = BlendFactor.OneMinusSrcAlpha;
            pipeline.Blend.Targets[0].Alpha.SrcFactor = BlendFactor.SrcAlpha;
            pipeline.Blend.Targets[0].Alpha.SrcFactor = BlendFactor.OneMinusSrcAlpha;

            context.DrawElements(BeginMode.Triangles, 6, DrawElementsType.UnsignedShort, 0);
        }
    }
}