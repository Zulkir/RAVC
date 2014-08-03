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
using Beholder;
using Beholder.Core;
using Beholder.Math;
using Beholder.Resources;
using Beholder.Shaders;

namespace Ravc.WinHost
{
    public abstract class FullScreenQuadRendererBase
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct Vertex
        {
            public Vector2 Position;
            public Vector2 TexCoord;
        }

        private readonly IShaderCombination shaderCombination;
        private readonly IVertexLayout vertexLayout;
        private readonly IBuffer vertexBuffer;
        private readonly IBuffer indexBuffer;
        
        private readonly IRasterizerState rasterizerState;
        private readonly IDepthStencilState depthStencilState;
        private readonly IBlendState blendState;
        
        const string VertexShaderText = @"
%meta
Name = StarVS
ProfileDX9 = vs_2_0
ProfileDX10 = vs_4_0
ProfileGL3 = 150

%input
float2 Position    : SDX9 = POSITION,  SDX10 = %name, SGL3 = %name
float2 TexCoord    : SDX9 = TEXCOORD,  SDX10 = %name, SGL3 = %name

%output
float4 Position : SDX9 = POSITION0, SDX10 = SV_Position, SGL3 = gl_Position
float2 TexCoord : SDX9 = TEXCOORD,  SDX10 = %name, SGL3 = %name

%code_main
    OUTPUT(Position) = float4(INPUT(Position), 0.0, 1.0);
    OUTPUT(TexCoord) = INPUT(TexCoord);
";

        protected FullScreenQuadRendererBase(IDevice device, string pixelShaderText)
        {
            var vertexShader = device.Create.VertexShader(ShaderParser.Parse(VertexShaderText));
            var pixelShader = device.Create.PixelShader(ShaderParser.Parse(pixelShaderText));
            shaderCombination = device.Create.ShaderCombination(vertexShader, null, null, null, pixelShader);

            vertexLayout = device.Create.VertexLayout(vertexShader, new[]
            {
                new VertexLayoutElement(ExplicitFormat.R32G32_FLOAT, 0, 0),
                new VertexLayoutElement(ExplicitFormat.R32G32_FLOAT, 0, sizeof(float) * 2)
            });

            vertexBuffer = device.Create.Buffer(new BufferDescription
            {
                BindFlags = BindFlags.VertexBuffer,
                Usage = Usage.Immutable,
                SizeInBytes = 4 * sizeof(float) * 4
            }, new SubresourceData(new[]
            {
                new Vertex { Position = new Vector2(-1,  1), TexCoord = new Vector2(0, 0) },
                new Vertex { Position = new Vector2( 1,  1), TexCoord = new Vector2(1, 0) },
                new Vertex { Position = new Vector2( 1, -1), TexCoord = new Vector2(1, 1) },
                new Vertex { Position = new Vector2(-1, -1), TexCoord = new Vector2(0, 1) }
            }));

            indexBuffer = device.Create.Buffer(new BufferDescription
            {
                BindFlags = BindFlags.IndexBuffer,
                Usage = Usage.Immutable,
                SizeInBytes = 6 * sizeof(ushort),
                ExtraFlags = ExtraFlags.SixteenBitIndices
            }, new SubresourceData(new ushort[]
            {
                0, 1, 3, 1, 2, 3
            }));

            rasterizerState = device.Create.RasterizerState(new RasterizerDescription
            {
                AntialiasedLineEnable = false,
                CullMode = Cull.None,
                DepthBias = 0,
                DepthBiasClamp = 0,
                DepthClipEnable = false,
                FillMode = FillMode.Solid,
                FrontFaceWinding = Winding.Clockwise,
                MultisampleEnable = false,
                ScissorEnable = false,
                SlopeScaledDepthBias = 0
            });

            depthStencilState = device.Create.DepthStencilState(DepthStencilDescription.Default);
            blendState = device.Create.BlendState(BlendDescription.Default);
            
        }

        protected void SetNonPixelStages(IDeviceContext context, IRenderTargetView target, int width, int height)
        {
            context.ShadersForDrawing = shaderCombination;

            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.InputAssembler.VertexLayout = vertexLayout;
            context.InputAssembler.VertexSources[0] = new VertexSource(vertexBuffer, 0, 4 * sizeof(float));
            context.InputAssembler.IndexSource = new IndexSource(indexBuffer, 0, IndexFormat.SixteenBit);

            context.Rasterizer.Viewports.Set(new Viewport(0, 0, width, height));
            context.Rasterizer.State = rasterizerState;

            context.OutputMerger.RenderTargets.Set(target);
            context.OutputMerger.DepthStencilState = depthStencilState;
            context.OutputMerger.BlendState = blendState;
        }

        protected void Draw(IDeviceContext context)
        {
            context.DrawIndexed(6, 0, 0);
        }
    }
}