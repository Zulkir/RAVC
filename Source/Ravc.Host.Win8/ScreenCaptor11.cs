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
using System.Threading;
using System.Windows.Forms;
using Beholder;
using Beholder.Core;
using Beholder.Libraries.SharpDX11;
using Beholder.Libraries.SharpDX11.Core;
using Beholder.Libraries.SharpDX11.Platform;
using Beholder.Libraries.SharpDX11.Resources;
using Beholder.Math;
using Beholder.Resources;
using Ravc.Host.WinLib;
using Ravc.Infrastructure;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using System.Linq;
using BindFlags = Beholder.Resources.BindFlags;
using FormatSupport = Beholder.Platform.FormatSupport;
using Resource = SharpDX.DXGI.Resource;
using Usage = Beholder.Resources.Usage;

namespace Ravc.Host.Win8
{
    public class ScreenCaptor11 : IScreenCaptor
    {
        private readonly IHostStatistics statistics;
        private readonly ILogger logger;
        private readonly ICDevice device;
        private readonly Output1 dxgiOutput;
        private readonly Thread workerThread;
        private readonly TexturePool texturePool;
        private readonly Stopwatch stopwatch;
        private OutputDuplication duplication;
        private Texture2D d3dTexture;
        private OutputDuplicateFrameInformation dxgiFrameInfo;
        private volatile bool ownsFrame;
        private volatile bool textureAwaitsProcessing;
        private volatile bool isWorking;

        public ScreenCaptor11(IHostStatistics statistics, ILogger logger, IDevice device)
        {
            this.statistics = statistics;
            this.logger = logger;
            if (!(device is ICDevice))
                throw new InvalidOperationException("ScreenCaptor11 can only work with SharpDX11 ICDevice");
            this.device = (ICDevice)device;
            dxgiOutput = ((COutput)device.Adapter.Outputs[0]).DXGIOutput.QueryInterface<Output1>();
            //workerThread = new Thread(DoWork);
            texturePool = new TexturePool(device, device.Adapter.GetSupportedFormats(FormatSupport.Texture2D).First(x => x.ExplicitFormat == ExplicitFormat.B8G8R8A8_UNORM).ID, Usage.Default, BindFlags.ShaderResource, MiscFlags.None);
            stopwatch = new Stopwatch();
        }

        public void Start()
        {
            isWorking = true;
            //workerThread.Start();
        }

        public void Stop()
        {
            isWorking = false;
            textureAwaitsProcessing = false;
            //workerThread.Join(2000);
        }

        public void Dispose()
        {
            if (d3dTexture != null)
                d3dTexture.Dispose();
            if (duplication != null)
                duplication.Dispose();
        }

        public bool TryGetCaptured(IDeviceContext context, IntRectangle clientRectangle, FrameType frameType, int colorDiffThreshold, int mostDetailedMip, out GpuRawFrame capturedFrame)
        {
            //var resultPooled2 = texturePool.Extract(clientRectangle.Width, clientRectangle.Height);
            //var frameInfo2 = new FrameInfo(frameType, (float)Stopwatch.GetTimestamp() / Stopwatch.Frequency, mostDetailedMip, clientRectangle.Width, clientRectangle.Height);
            //capturedFrame = new GpuRawFrame(frameInfo2, resultPooled2);
            //return true;

            if (duplication == null)
                duplication = dxgiOutput.DuplicateOutput(device.D3DDevice);

            stopwatch.Restart();

            if (ownsFrame)
                duplication.ReleaseFrame();

            Resource dxgiResource;
            duplication.AcquireNextFrame(100000, out dxgiFrameInfo, out dxgiResource);
            ownsFrame = true;

            stopwatch.Stop();
            statistics.OnCapture(stopwatch.Elapsed.Milliseconds);

            d3dTexture = dxgiResource.QueryInterface<Texture2D>();
            dxgiResource.Dispose();

            var resultPooled = texturePool.Extract(clientRectangle.Width, clientRectangle.Height);

            ((CDeviceContext)context).D3DDeviceContext.CopySubresourceRegion(d3dTexture, 0, 
                new ResourceRegion(
                    clientRectangle.X, 
                    clientRectangle.Y, 
                    0, 
                    Math.Min(clientRectangle.X + clientRectangle.Width, d3dTexture.Description.Width),
                    Math.Min(clientRectangle.Y + clientRectangle.Height, d3dTexture.Description.Height),
                    1
                    ),
                ((CTexture2D)resultPooled.Item).D3DTexture2D, 0);
            d3dTexture.Dispose();

            //var timestamp = (float)dxgiFrameInfo.LastPresentTime / Stopwatch.Frequency;
            var frameInfo = new FrameInfo(frameType, (float)Stopwatch.GetTimestamp() / Stopwatch.Frequency, 
                mostDetailedMip, colorDiffThreshold, clientRectangle.Width, clientRectangle.Height,
                //dxgiFrameInfo.PointerPosition.Position.X - clientRectangle.X, dxgiFrameInfo.PointerPosition.Position.Y - clientRectangle.Y);
                Cursor.Position.X - clientRectangle.X, Cursor.Position.Y - clientRectangle.Y);
            capturedFrame = new GpuRawFrame(frameInfo, resultPooled);

            return true;
        }
    }
}