﻿#region License
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
using Beholder;
using Beholder.Core;
using Beholder.Libraries.SharpDX11;
using Beholder.Libraries.SharpDX11.Core;
using Beholder.Libraries.SharpDX11.Platform;
using Beholder.Libraries.SharpDX11.Resources;
using Beholder.Math;
using Beholder.Resources;
using Ravc.Infrastructure;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using FormatSupport = Beholder.Platform.FormatSupport;
using Resource = SharpDX.DXGI.Resource;
using System.Linq;
using BindFlags = Beholder.Resources.BindFlags;
using Usage = Beholder.Resources.Usage;

namespace Ravc.WinHost
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
            workerThread = new Thread(DoWork);
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

        private void DoWork()
        {
            while (isWorking)
            {
                DoCapture();
            }
        }

        private void DoCapture()
        {
            if (duplication == null)
                duplication = dxgiOutput.DuplicateOutput(device.D3DDevice);

            while (textureAwaitsProcessing)
                Thread.Sleep(1);
            
            stopwatch.Restart();

            if (ownsFrame)
                duplication.ReleaseFrame();

            Resource dxgiResource;
            duplication.AcquireNextFrame(10000, out dxgiFrameInfo, out dxgiResource);
            ownsFrame = true;

            stopwatch.Stop();
            statistics.OnCapture(stopwatch.Elapsed.Milliseconds);

            d3dTexture = dxgiResource.QueryInterface<Texture2D>();
            dxgiResource.Dispose();
            textureAwaitsProcessing = true;
        }

        public bool TryGetCaptured(IDeviceContext context, IntRectangle clientRectangle, FrameType frameType, float defaultTimestamp, out GpuRawFrame capturedFrame)
        {
            //if (!textureAwaitsProcessing)
            //{
            //    capturedFrame = null;
            //    return false;
            //}

            DoCapture();

            //var d3dDesc = d3dTexture.Description;
            //var desc = new Beholder.Resources.Texture2DDescription
            //{
            //    Width = d3dDesc.Width,
            //    Height = d3dDesc.Height,
            //    ArraySize = d3dDesc.ArraySize,
            //    MipLevels = d3dDesc.MipLevels,
            //    Sampling = new Sampling
            //    {
            //        Count = (ushort)d3dDesc.SampleDescription.Count,
            //        Quality = (ushort)d3dDesc.SampleDescription.Quality
            //    },
            //    FormatID = (int)d3dDesc.Format,
            //    BindFlags = (BindFlags)d3dDesc.BindFlags,
            //    Usage = (Usage)d3dDesc.Usage,
            //    MiscFlags = (MiscFlags)d3dDesc.MipLevels
            //};
            //var beholderTexture = new CTexture2D(device, d3dTexture, ref desc, x => { });

            var resultPooled = texturePool.Extract(clientRectangle.Width, clientRectangle.Height);
            //context.CopySubresourceRegion(resultPooled.Item, 0, 0, 0, 0, beholderTexture, 0, new Box
            //{
            //    Left = clientRectangle.X,
            //    Top = clientRectangle.Y,
            //    Front = 0,
            //    Right = Math.Min(clientRectangle.X + clientRectangle.Width, beholderTexture.Width),
            //    Bottom = Math.Min(clientRectangle.Y + clientRectangle.Height, beholderTexture.Height),
            //    Back = 1
            //});
            //beholderTexture.Dispose();

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
            textureAwaitsProcessing = false;

            var frameInfo = new FrameInfo(frameType, (float)dxgiFrameInfo.LastPresentTime / Stopwatch.Frequency, clientRectangle.Width, clientRectangle.Height);
            capturedFrame = new GpuRawFrame(frameInfo, resultPooled);

            return true;
        }
    }
}