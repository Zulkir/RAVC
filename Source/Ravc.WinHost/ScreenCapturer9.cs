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
using Beholder.Platform;
using Beholder.Resources;
using Ravc.Utility;
using SharpDX;
using SharpDX.Direct3D9;
using Usage = Beholder.Resources.Usage;

namespace Ravc.WinHost
{
    public class ScreenCapturer9 : IScreenCapturer
    {
        private readonly TexturePool texturePool;

        private readonly Form form;
        private readonly Direct3D direct3D;
        private readonly Device d3dDevice;
        private readonly Surface d3dSurface1;

        private readonly Stopwatch stopwatch;
        private long ms;
        private long max;
        private int c;
        private int bigCount;

        public ScreenCapturer9(IDevice device)
        {
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

            form.Show();
            stopwatch = new Stopwatch();
        }

        public void Dispose()
        {
            d3dSurface1.Dispose();
            d3dDevice.Dispose();
            direct3D.Dispose();
        }

        public unsafe IPooled<ITexture2D> Capture(IDeviceContext deviceContext, Beholder.Math.IntRectangle clientRectangle)
        {
            var result = texturePool.Extract(clientRectangle.Width, clientRectangle.Height);
            var resultTexture = result.Item;

            stopwatch.Restart();
            d3dDevice.GetFrontBufferData(0, d3dSurface1);
            var sdxRectangle = new Rectangle(clientRectangle.X, clientRectangle.Y, clientRectangle.X + clientRectangle.Width, clientRectangle.Y + clientRectangle.Height);

            var lockedRectangle = d3dSurface1.LockRectangle(sdxRectangle, LockFlags.ReadOnly);
            var mappedSubresource = deviceContext.Map(resultTexture, 0, MapType.WriteDiscard, MapFlags.None);
            {
                int commonRowPitch = Math.Min(mappedSubresource.RowPitch, lockedRectangle.Pitch);
                Parallel.For(0, clientRectangle.Height, i => 
                    Memory.CopyBulk(
                        (byte*)mappedSubresource.Data + i * mappedSubresource.RowPitch,
                        (byte*)lockedRectangle.DataPointer + i * lockedRectangle.Pitch,
                        commonRowPitch));
            }
            deviceContext.Unmap(resultTexture, 0);
            d3dSurface1.UnlockRectangle();
            stopwatch.Stop();

            var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            ms += elapsedMilliseconds;
            if (elapsedMilliseconds > 15)
                bigCount++;
            if (elapsedMilliseconds > max)
                max = elapsedMilliseconds;

            c++;

            if ((c & 0xf) == 0)
            {
                form.Text = string.Format("{0}---{1}---{2}", Math.Round((double)ms / 0x10, 2), max, bigCount);
                ms = 0;
                max = 0;
                c = 0;
            }

            return result;
        }
    }
}