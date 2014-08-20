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

using Beholder;
using Beholder.Core;
using Beholder.Platform;
using Beholder.Resources;
using Beholder.Shaders;
using System.Linq;
using Ravc.Encoding;
using Ravc.Utility;

namespace Ravc.WinHost
{
    public class GpuSpatialDiffCalculator
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
static int3 White = int3(256, 256, 256);
static int3 Grey = int3(128, 128, 128);
static int2 Two = int2(2, 2);

float MaxComponent(float3 v)
{
    return max(max(v.x , v.y), v.z);
}

uint BuildCompressionInfo(int3 decodedDiff)
{
    float3 floatDiff = float3(decodedDiff);
    float outOfAC8 = MaxComponent(step(float3(2.5, 3.5, 2.5), floatDiff) + step(floatDiff, float3(-3.5, -3.5, -3.5)));
    //float outOfAC16 = MaxComponent(step(float3(15.5, 31.5, 15.5), floatDiff) + step(floatDiff, float3(-16.5, -32.5, -16.5)));
    return uint(outOfAC8/* + outOfAC16*/);
    //return 1;
}

%code_main
    int2 pixelCoord = INPUT(ThreadId).xy;
    int3 diff = Source[pixelCoord].rgb - Source.mips[1][pixelCoord / Two].rgb;
    uint3 encodedDiff = uint3((White + diff) % White);
    int3 decodedDiff = (encodedDiff + Grey) % White - Grey;
    uint compressionInfo = BuildCompressionInfo(decodedDiff);
    Output[pixelCoord] = uint4(encodedDiff.bgr, compressionInfo);
";

        public GpuSpatialDiffCalculator(IDevice device)
        {
            computeShader = device.Create.ComputeShader(ShaderParser.Parse(ComputeShaderText));
            formatId = device.Adapter.GetSupportedFormats(FormatSupport.Texture2D).First(x => x.ExplicitFormat == ExplicitFormat.R8G8B8A8_UINT).ID;
        }

        public void CalculateDiff(IDeviceContext context, ITexture2D target, ITexture2D source)
        {
            context.CopySubresourceRegion(target, EncodingConstants.SmallestMip, 0, 0, 0, source, EncodingConstants.SmallestMip, null);

            for (int i = EncodingConstants.SmallestMip; i >= 0; i--)
            {
                var srv = source.ViewAsShaderResource(formatId, i, i < EncodingConstants.SmallestMip ? 2 : 1);
                var uav = target.ViewAsUnorderedAccessResource(formatId, i);

                context.ShaderForDispatching = computeShader;

                context.ComputeStage.ShaderResources[0] = srv;
                context.ComputeStage.UnorderedAccessResources[0] = uav;

                context.Dispatch(RavcMath.DivideAndCeil(target.Width >> i, 16), RavcMath.DivideAndCeil(target.Height >> i, 16), 1);
            }
        }
    }
}