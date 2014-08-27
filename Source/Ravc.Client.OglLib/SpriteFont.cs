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

using System.Runtime.InteropServices;
using ObjectGL.Api;
using ObjectGL.Api.Objects;
using ObjectGL.Api.Objects.Resources;
using ObjectGL.Api.PipelineAspects;

namespace Ravc.Client.OglLib
{
    public class Spritefont
    {
        private const int MaxTextSize = 1024;

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
    float val = texture(DiffTexture, v_tex_coord).r;
    out_color = vec4(val, 0.75 * val, 0, max(val, 0.75));
}
";

        private readonly ITexture2D texture;
        private readonly IShaderProgram program;
        private readonly IBuffer vertexBuffer;
        private readonly IBuffer indexBuffer;
        private readonly IVertexArray vertexArray;
        private readonly ISampler sampler;

        public Spritefont(IClientSettings settings, IContext context, ITextureLoader textureLoader)
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

            vertexBuffer = context.Create.Buffer(BufferTarget.ArrayBuffer, Vertex.Size * 4 * MaxTextSize, BufferUsageHint.DynamicDraw);
            indexBuffer = context.Create.Buffer(BufferTarget.ElementArrayBuffer, sizeof(ushort) * 6 * MaxTextSize, BufferUsageHint.DynamicDraw);

            vertexArray = context.Create.VertexArray();
            vertexArray.SetVertexAttributeF(0, vertexBuffer, VertexAttributeDimension.Two, VertexAttribPointerType.Float, false, Vertex.Size, 0);
            vertexArray.SetVertexAttributeF(1, vertexBuffer, VertexAttributeDimension.Two, VertexAttribPointerType.Float, false, Vertex.Size, 2 * sizeof(float));
            vertexArray.SetElementArrayBuffer(indexBuffer);

            texture = textureLoader.LoadTexture(context, "DebugFont.png");
            sampler = context.Create.Sampler();
        }

        public unsafe void DrawString(IContext context, float x, float y, float size, string text)
        {
            var width = size * 0.33f;

            var pipeline = context.Pipeline;

            var pVertices = vertexBuffer.Map(0, vertexBuffer.SizeInBytes, MapAccess.Write | MapAccess.InvalidateBuffer);
            {
                foreach (var ch in text)
                {
                    int charIndex = (int)ch - ' ';
                    *(Vertex*)pVertices = new Vertex(x, y, (charIndex % 16) / 16.0f + 0.015f, (charIndex / 16) / 16.0f);
                    pVertices += Vertex.Size;
                    *(Vertex*)pVertices = new Vertex(x + width, y, ((charIndex % 16) + 1) / 16.0f - 0.015f, (charIndex / 16) / 16.0f);
                    pVertices += Vertex.Size;
                    *(Vertex*)pVertices = new Vertex(x + width, y - size, ((charIndex % 16) + 1) / 16.0f - 0.015f, ((charIndex / 16) + 1) / 16.0f);
                    pVertices += Vertex.Size;
                    *(Vertex*)pVertices = new Vertex(x, y - size, (charIndex % 16) / 16.0f + 0.015f, ((charIndex / 16) + 1) / 16.0f);
                    pVertices += Vertex.Size;
                    x += width;
                }
            }
            vertexBuffer.Unmap();

            var pIndices = indexBuffer.Map(0, indexBuffer.SizeInBytes, MapAccess.Write | MapAccess.InvalidateBuffer);
            for (int i = 0; i < text.Length; i++)
            {
                int baseIndex = i * 4;
                var p = (ushort*)pIndices;
                p[0] = (ushort)baseIndex;
                p[1] = (ushort)(baseIndex + 1);
                p[2] = (ushort)(baseIndex + 2);
                p[3] = (ushort)baseIndex;
                p[4] = (ushort)(baseIndex + 2);
                p[5] = (ushort)(baseIndex + 3);
                pIndices += 6 * sizeof(ushort);
            }
            indexBuffer.Unmap();

            pipeline.Program = program;
            pipeline.VertexArray = vertexArray;

            //pipeline.Viewports[0].Set(1024, 1024);
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

            context.DrawElements(BeginMode.Triangles, 6 * text.Length, DrawElementsType.UnsignedShort, 0);
        }
    }
}