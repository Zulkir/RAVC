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
using Beholder;
using Beholder.Core;
using Beholder.Math;
using Beholder.Resources;

namespace Ravc.Host.WinLib
{
    public class TextureRenderer : FullScreenQuadRendererBase
    {
        private readonly IBuffer dimensionsBuffer;
        private readonly ISamplerState samplerState;

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
    float3 val = sample(DiffuseTexture, INPUT(TexCoord)).rgb;

    //val = ((val + Half) % One) - Half;
    //val = abs(val);

    float norm = max(val.x, max(val.y, val.z));

    //if (norm < 2.5 / 256)
    //    val = float3(0, 0, 0);
    
    //if (val.g > 0.0 || val.b > 0.0)
    //    val = float3(0, 0, 0);

    //if (norm < 0.5 / 256)
    //    val = float3(0, 0, 0);
    //else if (norm < 2.5 / 256)
    //    val = float3(1, 0, 0);
    //else if (norm < 8.5 / 256)
    //    val = float3(0, 1, 0);
    //else
    //    val = float3(1, 1, 1);

    //val = val * 256;
    
    float a = sample(DiffuseTexture, INPUT(TexCoord)).a;
    val = norm < 0.5/256 ? float3(0,0,0) : a < 0.5/256 ? float3(1,0,0) : a < 1.5/256 ? float3(0,1,0) : a < 2.5/256 ? float3(0,0,1) : float3(1,1,1);

    //val = norm < 0.5/256 ? float3(0,0,0) : float3(1,1,1);    

    OUTPUT(Color) = float4(val, 1.0);
";

        public TextureRenderer(IDevice device)
            : base(device, PixelShaderText)
        {
            dimensionsBuffer = device.Create.Buffer(new BufferDescription
            {
                BindFlags = BindFlags.UniformBuffer,
                Usage = Usage.Dynamic,
                SizeInBytes = 4 * sizeof(float)
            });

            samplerState = device.Create.SamplerState(new SamplerDescription
            {
                Filter = Filter.MinMagMipPoint,
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
        }

        public unsafe void Render(IDeviceContext context, IRenderTargetView target, IShaderResourceView texture, int width, int height)
        {
            SetNonPixelStages(context, target, width, height);

            var dimensions = new Vector2(width, height);
            context.SetSubresourceData(dimensionsBuffer, 0, new SubresourceData((IntPtr)(&dimensions)));

            context.PixelStage.UniformBuffers[0] = dimensionsBuffer;
            context.PixelStage.ShaderResources[0] = texture;
            context.PixelStage.Samplers[0] = samplerState;

            Draw(context);
        }
    }
}