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
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Beholder;
using Beholder.Core;
using Beholder.Math;
using Beholder.Platform;
using Beholder.Resources;
using Ravc.Host.WinLib;
using SharpDX.Direct3D9;
using Rectangle = SharpDX.Rectangle;
using Usage = Beholder.Resources.Usage;

namespace Ravc.Host.Win7
{
    public class ScreenCaptor9 : IScreenCaptor
    {
        private readonly IHostStatistics statistics;
        private readonly Form form;
        private readonly Direct3D direct3D;
        private readonly Device d3dDevice;
        private readonly Surface d3dSurface1;
        private readonly TexturePool texturePool;

        private readonly Stopwatch stopwatch;

        public ScreenCaptor9(IHostStatistics statistics, IDevice device)
        {
            this.statistics = statistics;
            var resultFormatId = device.Adapter.GetSupportedFormats(FormatSupport.Texture2D).First(x => x.ExplicitFormat == ExplicitFormat.B8G8R8A8_UNORM).ID;
            texturePool = new TexturePool(device, resultFormatId, Usage.Dynamic, BindFlags.ShaderResource, MiscFlags.None);

            direct3D = new Direct3D();
            form = new Form();
            d3dDevice = new Device(direct3D, 0, DeviceType.Hardware, form.Handle, CreateFlags.SoftwareVertexProcessing, new PresentParameters
            {
                SwapEffect = SwapEffect.Discard,
                Windowed = true
            });

            d3dSurface1 = Surface.CreateOffscreenPlain(d3dDevice, 4096, 1440, Format.A8R8G8B8, Pool.Scratch);

            stopwatch = new Stopwatch();
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public void Dispose()
        {
            d3dSurface1.Dispose();
            d3dDevice.Dispose();
            direct3D.Dispose();
        }

        public unsafe bool TryGetCaptured(IDeviceContext context, IntRectangle clientRectangle, FrameType frameType, int mostDetailedMip, out GpuRawFrame capturedFrame)
        {
            stopwatch.Restart();

            var result = texturePool.Extract(clientRectangle.Width, clientRectangle.Height);
            var resultTexture = result.Item;
            
            d3dDevice.GetFrontBufferData(0, d3dSurface1);
            var sdxRectangle = new Rectangle(clientRectangle.X, clientRectangle.Y, clientRectangle.X + clientRectangle.Width, clientRectangle.Y + clientRectangle.Height);
            
            var lockedRectangle = d3dSurface1.LockRectangle(sdxRectangle, LockFlags.ReadOnly);
            var mappedSubresource = context.Map(resultTexture, 0, MapType.WriteDiscard, MapFlags.None);
            {
                int commonRowPitch = Math.Min(mappedSubresource.RowPitch, lockedRectangle.Pitch);
                Parallel.For(0, clientRectangle.Height, i => 
                    Memory.CopyBulk(
                        (byte*)mappedSubresource.Data + i * mappedSubresource.RowPitch,
                        (byte*)lockedRectangle.DataPointer + i * lockedRectangle.Pitch,
                        commonRowPitch));
            }
            context.Unmap(resultTexture, 0);
            d3dSurface1.UnlockRectangle();

            var frameInfo = new FrameInfo(frameType, (float)Stopwatch.GetTimestamp() / Stopwatch.Frequency, mostDetailedMip, clientRectangle.Width, clientRectangle.Height);
            capturedFrame = new GpuRawFrame(frameInfo, result);

            stopwatch.Stop();
            statistics.OnCapture(stopwatch.Elapsed.TotalMilliseconds);

            return true;
        }
    }
}