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
using Ravc.Utility.DataStructures;

namespace Ravc.Host.WinLib
{
    public class DebugStage : IPipelineStage<GpuEncodedFrame, GpuEncodedFrame>
    {
        private readonly IDevice device;
        private readonly TextureRenderer textureRenderer;
        private readonly int formatId;
        private IPipelinedConsumer<GpuEncodedFrame> nextStage;

        public IPipelinedConsumer<GpuEncodedFrame> NextStage { set { nextStage = value; } }
        public bool IsOverloaded { get { return nextStage.IsOverloaded; } }

        public DebugStage(IDevice device)
        {
            this.device = device;
            textureRenderer= new TextureRenderer(device);
            formatId = device.Adapter.GetSupportedFormats(FormatSupport.Texture2D).First(x => x.ExplicitFormat == ExplicitFormat.R8G8B8A8_UNORM).ID;
        }

        public void Consume(GpuEncodedFrame input)
        {
            var context = device.ImmediateContext;
            var diffTex = input.DiffPooled.Item;
            textureRenderer.Render(context, context.Device.PrimarySwapChain.GetCurrentColorBuffer(), diffTex, input.Info.MostDetailedMip);
            nextStage.Consume(input);
        }
    }
}