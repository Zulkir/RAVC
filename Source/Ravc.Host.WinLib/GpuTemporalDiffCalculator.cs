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
using Ravc.Encoding;
using Ravc.Utility;

namespace Ravc.Host.WinLib
{
    public class GpuTemporalDiffCalculator
    {
        private readonly IComputeShader computeShader;
        private readonly IBuffer frameInfoBuffer;
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

%ubuffers
ubuffer FrameInfo : slot = 0, slotGL3 = 0, slotDX9 = c0
	float ColorDiffThreshold

%srvs
Texture2D <uint4> CurrentFrameTex : slot = 0
Texture2D <uint4> ParentFrameTex : slot = 1

%uavs
RWTexture2D <uint4> Output : slot = 0

%code_global
static int4 White = int4(256, 256, 256, 256);

int MaxComponent(int3 value)
{
    return max(value.x, max(value.y, value.z));
}

%code_main
    int2 pixelCoord = INPUT(ThreadId).xy;
    int3 diff = CurrentFrameTex[pixelCoord].rgb - ParentFrameTex[pixelCoord].rgb;
    int steppingZero = step(ColorDiffThreshold, float(MaxComponent(abs(diff))));
    //int steppingZero = 1;
    uint3 endocedDiff = steppingZero * ((White + diff) % White);
    Output[pixelCoord] = uint4(endocedDiff, 255);
";

        public GpuTemporalDiffCalculator(IDevice device)
        {
            computeShader = device.Create.ComputeShader(ShaderParser.Parse(ComputeShaderText));
            frameInfoBuffer = device.Create.Buffer(new BufferDescription
            {
                BindFlags = BindFlags.UniformBuffer,
                Usage = Usage.Dynamic,
                SizeInBytes = 16
            });
            formatId = device.Adapter.GetSupportedFormats(FormatSupport.Texture2D).First(x => x.ExplicitFormat == ExplicitFormat.R8G8B8A8_UINT).ID;
        }

        public unsafe void CalculateDiff(IDeviceContext context, ITexture2D target, ITexture2D current, ITexture2D parentTexture, int colorDiffThreshold)
        {
            context.ShaderForDispatching = computeShader;

            var map = context.Map(frameInfoBuffer, 0, MapType.WriteDiscard, MapFlags.None);
            *(Vector4*)map.Data = new Vector4(colorDiffThreshold, 0, 0, 0);
            context.Unmap(frameInfoBuffer, 0);

            context.ComputeStage.UniformBuffers[0] = frameInfoBuffer;

            for (int i = 0; i < EncodingConstants.MipLevels; i++)
            {
                var uav = target.ViewAsUnorderedAccessResource(formatId, i);
                var currentSrv = current.ViewAsShaderResource(formatId, i, 1);
                var parentSrv = parentTexture.ViewAsShaderResource(formatId, i, 1);

                context.ComputeStage.UnorderedAccessResources[0] = uav;
                context.ComputeStage.ShaderResources[0] = currentSrv;
                context.ComputeStage.ShaderResources[1] = parentSrv;

                context.Dispatch(RavcMath.DivideAndCeil(target.Width >> i, 16), RavcMath.DivideAndCeil(target.Height >> i, 16), 1);
            }
        }
    }
}