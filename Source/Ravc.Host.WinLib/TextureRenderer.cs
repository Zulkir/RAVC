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
using System.Linq;
using System.Runtime.InteropServices;
using Beholder;
using Beholder.Core;
using Beholder.Libraries.SharpDX11.Core;
using Beholder.Math;
using Beholder.Platform;
using Beholder.Resources;
using Beholder.Shaders;

namespace Ravc.Host.WinLib
{
    public class TextureRenderer// : FullScreenQuadRendererBase
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

        private readonly IBuffer dimensionsBuffer;
        private readonly ISamplerState samplerState;
        private readonly int formatRgba8UnormId;

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

        const string PixelShaderText = @"
%meta
Name = StarPS
ProfileDX9 = ps_2_0
ProfileDX10 = ps_4_0
ProfileGL3 = 150

%ubuffers
ubuffer Dimensions : slot = 0, slotGL3 = 0, slotDX9 = c0
	float2 Size

%input
float4 Position : SDX9 = %unused,   SDX10 = SV_Position, SGL3 = %unused
float2 TexCoord : SDX9 = TEXCOORD0, SDX10 = %name,	     SGL3 = %name

%output
float4 Color    : SDX9 = COLOR, SDX10 = SV_Target, SGL3 = %name

%samplers
sampler TextureMapSampler : slot = 0

%srvs
Texture2D <float4> DiffuseTexture : slot = 0, slotGL3 = 0

%fixed_sampling
DiffuseTexture : TextureMapSampler

%code_global

static int3 White = int3(256, 256, 256);
static int3 HalfWhite = int3(128, 128, 128);
static int2 Two = int2(2, 2);
static float4 FloatWhite = float4(1.0, 1.0, 1.0, 1.0);
static float4 FloatDivisor = float4(255.0, 255.0, 255.0, 255.0);
static float4 FloatMultiplier = float4(255.999, 255.999, 255.999, 255.999);

int3 DecodeDiff(uint3 v)
{
    return ((v + HalfWhite) % White - HalfWhite);
}

uint3 EncodeDiff(int3 v)
{
    return (v + White) % White;
}


static float3 One = float3(1.0, 1.0, 1.0);
static float3 Half = float3(0.5, 0.5, 0.5);


%code_main
    float3 realval = sample(DiffuseTexture, INPUT(TexCoord)).rgb;

    float3 val = ((realval + Half) % One) - Half;
    val = abs(val);
    //val *= 64.0;

    float norm = max(val.x, max(val.y, val.z));
    
    //if (norm < 0.5 / 256)
    //    val = float3(0, 0, 0);
    //else if (norm < 1.5 / 256)
    //    val = float3(1, 0, 0);
    //else if (norm < 4.5 / 256)
    //    val = float3(0, 1, 0);
    //else if (norm < 8.5 / 256)
    //    val = float3(0, 0, 1);
    //else
    //    val = float3(1, 1, 1);

    //val = val * 256;
    
    float a = sample(DiffuseTexture, INPUT(TexCoord)).a;
    val = norm < 0.5/256 ? float3(0,0,0) : a < 0.5/256 ? float3(1,0,0) : a < 1.5/256 ? float3(0,1,0) : a < 2.5/256 ? float3(0,0,1) : float3(1,1,1);

    //val = norm < 0.5/256 ? float3(0,0,0) : float3(1,1,1);    

    //OUTPUT(Color) = float4(val, 1.0);
    OUTPUT(Color) = float4(realval, 1.0);
";

        public TextureRenderer(IDevice device)
        {
            var vertexShader = device.Create.VertexShader(ShaderParser.Parse(VertexShaderText));
            var pixelShader = device.Create.PixelShader(ShaderParser.Parse(PixelShaderText));
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

            dimensionsBuffer = device.Create.Buffer(new BufferDescription
            {
                BindFlags = BindFlags.UniformBuffer,
                Usage = Usage.Dynamic,
                SizeInBytes = 4 * sizeof(float)
            });

            samplerState = device.Create.SamplerState(new SamplerDescription
            {
                Filter = Filter.MinMagLinearMipPoint,
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                MipLodBias = 0.0f,
                MaximumAnisotropy = 16,
                ComparisonFunction = Comparison.Never,
                BorderColor = new Color4(),
                MaximumLod = 3.402823466e+38f,
                MinimumLod = -3.402823466e+38f
            });

            formatRgba8UnormId = device.Adapter.GetSupportedFormats(FormatSupport.Texture2D)
                                .First(x => x.ExplicitFormat == ExplicitFormat.R8G8B8A8_UNORM).ID;
        }

        public unsafe void Render(IDeviceContext context, IRenderTargetView target, ITexture2D texture, int mipLevel)
        {
            context.ShadersForDrawing = shaderCombination;

            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.InputAssembler.VertexLayout = vertexLayout;
            context.InputAssembler.VertexSources[0] = new VertexSource(vertexBuffer, 0, 4 * sizeof(float));
            context.InputAssembler.IndexSource = new IndexSource(indexBuffer, 0, IndexFormat.SixteenBit);

            context.Rasterizer.Viewports.Set(new Viewport(0, 0, target.Width, target.Height));
            context.Rasterizer.State = rasterizerState;

            context.OutputMerger.RenderTargets.Set(target);
            context.OutputMerger.DepthStencilState = depthStencilState;
            context.OutputMerger.BlendState = blendState;

            var dimensions = new Vector2(texture.Width, texture.Height);
            context.SetSubresourceData(dimensionsBuffer, 0, new SubresourceData((IntPtr)(&dimensions)));

            context.PixelStage.UniformBuffers[0] = dimensionsBuffer;
            context.PixelStage.ShaderResources[0] = texture.ViewAsShaderResource(formatRgba8UnormId, mipLevel, 1);
            context.PixelStage.Samplers[0] = samplerState;

            context.DrawIndexed(6, 0, 0);

            context.PixelStage.ShaderResources[0] = null;
            ((CDeviceContext)context).D3DDeviceContext.PixelShader.SetShaderResource(0, null);
        }
    }
}