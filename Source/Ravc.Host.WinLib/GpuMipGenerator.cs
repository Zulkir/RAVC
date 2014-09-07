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
using Ravc.Utility;

namespace Ravc.Host.WinLib
{
    public class GpuMipGenerator
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

%code_main
    int2 pixelCoord = INPUT(ThreadId).xy;
    int2 topLeftCoord = pixelCoord * 2;

    uint4 topLeft = Source[topLeftCoord];
    uint4 topRight = Source[int2(topLeftCoord.x + 1, topLeftCoord.y)];
    uint4 bottomLeft = Source[int2(topLeftCoord.x, topLeftCoord.y + 1)];
    uint4 bottomRight = Source[int2(topLeftCoord.x + 1, topLeftCoord.y + 1)];
    
    uint4 average = (topLeft + topRight + bottomLeft + bottomRight) / 4;
    Output[pixelCoord] = uint4(average.rgb, 255);
    //Output[pixelCoord] = Source[topLeftCoord];
    //Output[pixelCoord] = uint4(0, 0, 0, 255);
";

        public GpuMipGenerator(IDevice device)
        {
            computeShader = device.Create.ComputeShader(ShaderParser.Parse(ComputeShaderText));
            formatId = device.Adapter.GetSupportedFormats(FormatSupport.Texture2D).First(x => x.ExplicitFormat == ExplicitFormat.R8G8B8A8_UINT).ID;
        }

        public void GenerateMips(IDeviceContext context, ITexture2D target, int mipLevels)
        {
            for (int i = 1; i < mipLevels; i++)
            {
                var srv = target.ViewAsShaderResource(formatId, i - 1, 1);
                var uav = target.ViewAsUnorderedAccessResource(formatId, i);

                context.ShaderForDispatching = computeShader;

                context.ComputeStage.ShaderResources[0] = srv;
                context.ComputeStage.UnorderedAccessResources[0] = uav;

                context.Dispatch(RavcMath.DivideAndCeil(target.Width >> i, 16), RavcMath.DivideAndCeil(target.Height >> i, 16), 1);
            }
        }
    }
}