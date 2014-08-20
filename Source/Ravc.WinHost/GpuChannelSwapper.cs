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

namespace Ravc.WinHost
{
    public class GpuChannelSwapper
    {
        private readonly IComputeShader computeShader;
        private readonly int rgbaFormat;
        private readonly int bgraFormat;

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
Texture2D <float4> Input : slot = 0

%uavs
RWTexture2D <float4> Output : slot = 0

%code_main
    Output[INPUT(ThreadId).xy] = Input[INPUT(ThreadId).xy];
";

        public GpuChannelSwapper(IDevice device)
        {
            computeShader = device.Create.ComputeShader(ShaderParser.Parse(ComputeShaderText));
            rgbaFormat = device.Adapter.GetSupportedFormats(FormatSupport.Texture2D).First(x => x.ExplicitFormat == ExplicitFormat.R8G8B8A8_UNORM).ID;
            bgraFormat = device.Adapter.GetSupportedFormats(FormatSupport.Texture2D).First(x => x.ExplicitFormat == ExplicitFormat.B8G8R8A8_UNORM).ID;
        }

        public void SwapBgraToRgba(IDeviceContext context, ITexture2D target, ITexture2D texture)
        {
            var uav = target.ViewAsUnorderedAccessResource(rgbaFormat, 0);
            var srv = texture.ViewAsShaderResource(bgraFormat, 0, 1);

            context.ShaderForDispatching = computeShader;
            context.ComputeStage.ShaderResources[0] = srv;
            context.ComputeStage.UnorderedAccessResources[0] = uav;

            context.Dispatch(RavcMath.DivideAndCeil(target.Width, 16), RavcMath.DivideAndCeil(target.Height, 16), 1);
        } 
    }
}