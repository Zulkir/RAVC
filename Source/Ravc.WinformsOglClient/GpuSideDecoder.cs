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
using OpenTK;
using BeginMode = ObjectGL.Api.BeginMode;
using BufferTarget = ObjectGL.Api.Objects.Resources.BufferTarget;
using BufferUsageHint = ObjectGL.Api.Objects.Resources.BufferUsageHint;
using DrawElementsType = ObjectGL.Api.DrawElementsType;
using TextureMagFilter = ObjectGL.Api.Objects.TextureMagFilter;
using TextureMinFilter = ObjectGL.Api.Objects.TextureMinFilter;
using TextureWrapMode = ObjectGL.Api.Objects.TextureWrapMode;
using VertexAttribPointerType = ObjectGL.Api.Objects.VertexAttribPointerType;

namespace Ravc.WinformsOglClient
{
    public class GpuSideDecoder
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

        private readonly IShaderProgram program;
        private readonly IVertexArray vertexArray;
        private readonly IFramebuffer framebuffer;
        private readonly ISampler sampler;

        private const string VertexShaderText =
@"#version 150

in vec4 in_position;
in vec4 in_tex_coord;

out vec2 v_tex_coord;

void main()
{
    gl_Position = vec4(in_position.x, -in_position.y, 0.0f, 1.0f);
    v_tex_coord = in_tex_coord.xy;
}
";

        private const string FragmentShaderText =
@"#version 150
uniform sampler2D DiffuseTexture;
uniform sampler2D ParentTexture;

in vec2 v_tex_coord;

out vec4 out_color;

vec3 AddWithOverflow(vec3 v1, vec3 v2)
{
    ivec3 i1 = ivec3(v1 * 255.999);
    ivec3 i2 = ivec3(v2 * 255.999);

    ivec3 iResult = (i1 + i2) % ivec3(256, 256, 256);
    return vec3(iResult) / 255;
}

void main()
{
    vec3 diff = texture(DiffuseTexture, v_tex_coord).xyz;
    vec3 prev = texture(ParentTexture, v_tex_coord).xyz;
    vec3 result = AddWithOverflow(diff, prev);
    out_color = vec4(result, 1.0);
    //out_color = vec4(diff, 1.0);
    //out_color = vec4(1.0, 0.0, 0.0, 1.0);
}
";

        public GpuSideDecoder(IContext context)
        {
            var vertexShader = context.Create.VertexShader(VertexShaderText);
            var fragmentShader = context.Create.FragmentShader(FragmentShaderText);
            program = context.Create.Program(new ShaderProgramDescription
            {
                VertexShaders = new[] { vertexShader },
                FragmentShaders = new[] { fragmentShader },
                VertexAttributeNames = new[] { "in_position", "in_tex_coord" },
                SamplerNames = new[] { "DiffuseTexture", "ParentTexture" }
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

            framebuffer = context.Create.Framebuffer();

            sampler = context.Create.Sampler();
            sampler.SetMinFilter(TextureMinFilter.Nearest);
            sampler.SetMagFilter(TextureMagFilter.Nearest);
            sampler.SetWrapR(TextureWrapMode.Clamp);
            sampler.SetWrapS(TextureWrapMode.Clamp);
            sampler.SetWrapT(TextureWrapMode.Clamp);
        }

        public void Decode(IContext context, ITexture2D target, ITexture2D texture, ITexture2D parentTexture, int width, int height)
        {
            var pipeline = context.Pipeline;

            pipeline.Program = program;
            pipeline.VertexArray = vertexArray;
            
            pipeline.Viewports[0].Set(width, height);
            pipeline.Rasterizer.SetDefault();
            pipeline.Rasterizer.MultisampleEnable = false;

            framebuffer.AttachTextureImage(FramebufferAttachmentPoint.Color0, target, 0);
            pipeline.Framebuffer = framebuffer;
            
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