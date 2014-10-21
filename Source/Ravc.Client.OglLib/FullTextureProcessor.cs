using System.Runtime.InteropServices;
using ObjectGL.Api;
using ObjectGL.Api.Objects;
using ObjectGL.Api.Objects.Resources;

namespace Ravc.Client.OglLib
{
    public class FullTextureProcessor
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

        private readonly IShaderProgram program;
        private readonly IVertexArray vertexArray;

        private const string DesktopHeader =
@"#version 150";
        private const string EsHeader =
@"#version 300 es

precision highp float;
precision highp sampler2D;";

        private const string VertexShaderText =
@"
in vec2 in_position;

void main()
{
    gl_Position = vec4(in_position.x, -in_position.y, 0.0f, 1.0f);
}
";

        public FullTextureProcessor(IClientSettings settings, IContext context, string fragmentShaderText, string[] samplerNames)
        {
            var header = settings.IsEs ? EsHeader : DesktopHeader;
            var vertexShader = context.Create.VertexShader(header + VertexShaderText);

            var decodeFragmentShader = context.Create.FragmentShader(header + fragmentShaderText);
            program = context.Create.Program(new ShaderProgramDescription
            {
                VertexShaders = new[] { vertexShader },
                FragmentShaders = new[] { decodeFragmentShader },
                VertexAttributeNames = new[] { "in_position" },
                SamplerNames = samplerNames
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
        }

        public void PrepareContext(IContext context)
        {
            var pipeline = context.Pipeline;

            pipeline.VertexArray = vertexArray;

            pipeline.Rasterizer.SetDefault();
            pipeline.Rasterizer.MultisampleEnable = false;

            pipeline.DepthStencil.SetDefault();
            pipeline.DepthStencil.DepthMask = false;

            pipeline.Blend.SetDefault(false);

            pipeline.Program = program;
        }
    }
}