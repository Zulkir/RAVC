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
using System.Runtime.InteropServices;
using Beholder;
using Beholder.Core;
using Beholder.Platform;
using Beholder.Resources;
using Beholder.Shaders;
using Ravc.Utility;

namespace Ravc.Host.WinLib
{
    public class GpuLossyCalculator
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct MipInfo
        {
            public int MipLevel;
            public int CoordDivisor;
            private int Dummy0;
            private int Dummy1;

            public const int Size = 4 * sizeof(int);
        }

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
ubuffer MipInfo : slot = 0, slotGL3 = 0, slotDX9 = c0
	int MipLevel
    int CoordDivisor

%srvs
Texture2D <uint4> Previous : slot = 0
Texture2D <uint4> TemporalDiff : slot = 1

%uavs
RWTexture2D <uint4> Output : slot = 0

%code_global
static int3 White = int3(256, 256, 256);
static int3 Grey = int3(128, 128, 128);
static float3 Zero = float3(0.0, 0.0, 0.0);

int MaxComponent(int3 v)
{
    return max(max(v.x , v.y), v.z);
}

%code_main
    int2 pixelCoordDetailed = INPUT(ThreadId).xy;
    int2 pixelCoordMip = INPUT(ThreadId).xy / CoordDivisor;
    
    int3 previousValueDetailed = Previous[pixelCoordDetailed].rgb;
    int3 previousValueMip = Previous.mips[MipLevel][pixelCoordMip].rgb;
    int3 encodedDiff = TemporalDiff.mips[MipLevel][pixelCoordMip].rgb;    

    uint3 valueMip = (previousValueMip + encodedDiff) % White;
    int3 diffMip = valueMip - previousValueMip;
    
    int diffSize = MaxComponent(abs(diffMip));
    
    // todo: to step()
    uint3 lossyValue = diffSize < 32 ? clamp(previousValueDetailed + diffMip, int3(0,0,0), int3(255,255,255)) : valueMip;

    Output[pixelCoordDetailed] = uint4(lossyValue, 255);
";

        private readonly IComputeShader computeShader;
        private readonly IBuffer mipInfoBuffer;
        private readonly int formatId;

        public GpuLossyCalculator(IDevice device)
        {
            computeShader = device.Create.ComputeShader(ShaderParser.Parse(ComputeShaderText));
            mipInfoBuffer = device.Create.Buffer(new BufferDescription
            {
                BindFlags = BindFlags.UniformBuffer,
                Usage = Usage.Dynamic,
                SizeInBytes = MipInfo.Size
            });
            formatId = device.Adapter.GetSupportedFormats(FormatSupport.Texture2D).First(x => x.ExplicitFormat == ExplicitFormat.R8G8B8A8_UINT).ID;
        }

        public unsafe void CalculateLossy(IDeviceContext context, ITexture2D target, ITexture2D previous, ITexture2D temporalDiff, int mostDetailedMip)
        {
            var mipInfo = new MipInfo { MipLevel = mostDetailedMip, CoordDivisor = 1 << mostDetailedMip };

            var map = context.Map(mipInfoBuffer, 0, MapType.WriteDiscard, MapFlags.None);
            *(MipInfo*)map.Data = mipInfo;
            context.Unmap(mipInfoBuffer, 0);

            // todo: try doing everything using MostDetailedMip of SRV

            context.ShaderForDispatching = computeShader;
            context.ComputeStage.UniformBuffers[0] = mipInfoBuffer;
            context.ComputeStage.ShaderResources[0] = previous.ViewAsShaderResource(formatId, 0, previous.MipLevels);
            context.ComputeStage.ShaderResources[1] = temporalDiff.ViewAsShaderResource(formatId, 0, temporalDiff.MipLevels);
            context.ComputeStage.UnorderedAccessResources[0] = target.ViewAsUnorderedAccessResource(formatId, 0);

            context.Dispatch(RavcMath.DivideAndCeil(target.Width, 16), RavcMath.DivideAndCeil(target.Height, 16), 1);
        } 
    }
}