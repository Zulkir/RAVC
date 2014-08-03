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
using Beholder.Math;
using Beholder.Platform;
using Beholder.Resources;
using Beholder.Shaders;
using System.Linq;

namespace Ravc.WinHost
{
    public class GpuDiffCalculator
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
Texture2D <uint4> CurrentFrameTex : slot = 0
Texture2D <uint4> ParentFrameTex : slot = 1

%uavs
RWTexture2D <uint4> Output : slot = 0

%code_global
static int4 White = int4(256, 256, 256, 256);

%code_main
    int2 pixelCoord = INPUT(ThreadId).xy;
    int4 diff = (White + CurrentFrameTex[pixelCoord] - ParentFrameTex[pixelCoord]) % White;
    Output[pixelCoord] = diff.bgra;
";

        public GpuDiffCalculator(IDevice device)
        {
            computeShader = device.Create.ComputeShader(ShaderParser.Parse(ComputeShaderText));
            formatId = device.Adapter.GetSupportedFormats(FormatSupport.Texture2D).First(x => x.ExplicitFormat == ExplicitFormat.R8G8B8A8_UINT).ID;
        }

        public void CalculateDiff(IDeviceContext context, ITexture2D target, ITexture2D texture, ITexture2D parentTexture)
        {
            var uav = target.ViewAsUnorderedAccessResource(formatId, 0);
            context.ClearUnorderedAccessView(uav, new IntVector4(255, 255, 0, 255));
            
            context.ShaderForDispatching = computeShader;
            context.ComputeStage.ShaderResources[0] = texture.ViewAsShaderResource(formatId, 0, 1);
            context.ComputeStage.ShaderResources[1] = parentTexture.ViewAsShaderResource(formatId, 0, 1);
            context.ComputeStage.UnorderedAccessResources[0] = uav;

            context.Dispatch(target.Width / 16, target.Height / 16, 1);
        }
    }
}