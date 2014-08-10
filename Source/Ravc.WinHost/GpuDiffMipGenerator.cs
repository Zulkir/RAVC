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

using System.Linq;
using Beholder;
using Beholder.Core;
using Beholder.Platform;
using Beholder.Resources;
using Beholder.Shaders;
using Ravc.Encoding;
using Ravc.Utility;

namespace Ravc.WinHost
{
    public class GpuDiffMipGenerator
    {
        private readonly IComputeShader computeShader;
        private readonly int formatId;

        const string ComputeShaderText = @"
%meta
Name = DiffCS
ProfileDX10 = cs_5_0
ThreadCountX = 16
ThreadCountY = 16
ThreadCountZ = 1

%input
int3 ThreadId : SDX10 = SV_DispatchThreadID

%srvs
Texture2D <uint4> Source : slot = 0

%uavs
RWTexture2D <uint4> Output : slot = 0

%code_global
static int4 White = int4(256, 256, 256, 256);
static int4 HalfWhite = int4(128, 128, 128, 128);
static int2 Two = int2(2, 2);
static float4 FloatWhite = float4(1.0, 1.0, 1.0, 1.0);
static float4 FloatDivisor = float4(255.0, 255.0, 255.0, 255.0);
static float4 FloatMultiplier = float4(255.999, 255.999, 255.999, 255.999);

int4 DecodeDiff(uint4 v)
{
    return ((v + HalfWhite) % White - HalfWhite);
}

uint4 EncodeDiff(int4 v)
{
    return (v + White) % White;
}

%code_main
    int2 pixelCoord = INPUT(ThreadId).xy;
    int2 topLeftCoord = pixelCoord * Two;

    int4 topLeft = DecodeDiff(Source[topLeftCoord]);
    int4 topRight = DecodeDiff(Source[int2(topLeftCoord.x + 1, topLeftCoord.y)]);
    int4 bottomLeft = DecodeDiff(Source[int2(topLeftCoord.x, topLeftCoord.y + 1)]);
    int4 bottomRight = DecodeDiff(Source[int2(topLeftCoord.x + 1, topLeftCoord.y + 1)]);

    int4 average = (topLeft + topRight + bottomLeft + bottomRight) / 4;
    Output[pixelCoord] = EncodeDiff(average);
";

        public GpuDiffMipGenerator(IDevice device)
        {
            computeShader = device.Create.ComputeShader(ShaderParser.Parse(ComputeShaderText));
            formatId = device.Adapter.GetSupportedFormats(FormatSupport.Texture2D).First(x => x.ExplicitFormat == ExplicitFormat.R8G8B8A8_UINT).ID;
        }

        public void GenerateMips(IDeviceContext context, ITexture2D diffTexture)
        {
            for (int i = 1; i < EncodingConstants.MipLevels; i++)
            {
                var srv = diffTexture.ViewAsShaderResource(formatId, i - 1, 1);
                var uav = diffTexture.ViewAsUnorderedAccessResource(formatId, i);
                context.ShaderForDispatching = computeShader;
                context.ComputeStage.ShaderResources[0] = srv;
                context.ComputeStage.UnorderedAccessResources[0] = uav;
                context.Dispatch(RavcMath.DivideAndCeil(diffTexture.Width >> i, 16), RavcMath.DivideAndCeil(diffTexture.Height >> i, 16), 1);
            }
        }
    }
}