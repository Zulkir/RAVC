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
using ObjectGL;
using ObjectGL.CachingImpl;
using ObjectGL.GL4;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using Ravc.Client.OglLib;
using Ravc.Streaming;
using Ravc.Utility;
using Ravc.Utility.DataStructures;

namespace Ravc.Client.OglDesktop
{
    public class MyGameWindow : GameWindow, IRavcGameWindow
    {
        private readonly IGL gl;
        private readonly INativeGraphicsContext nativeGraphicsContext;
        private readonly string fileName;
        private Context context;
        private MainLoop mainLoop;
        private StreamReceivingStage streamReceivingStage;
        private CpuDecompressionStage cpuDecompressionStage;
        private bool isFullscreen;

        public int ClientWidth { get { return ClientSize.Width; } }
        public int ClientHeight { get { return ClientSize.Height; } }

        public MyGameWindow(string fileName = null)
            : base(1280, 720, new GraphicsMode(new ColorFormat(32), 24, 8, 4), "Object.GL Tester",
            GameWindowFlags.Default, DisplayDevice.Default, 4, 2, GraphicsContextFlags.Default)
        {
            this.fileName = fileName;
            gl = new GL4();
            nativeGraphicsContext = new GL4NativeGraphicsContextWrapper(Context);
            VSync = VSyncMode.On;
            Context.SwapInterval = 1;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            context = new Context(gl, nativeGraphicsContext);
            var pclWorkarounds = new PclWorkarounds();
            var settings = new ClientSettings();
            var textureLoader = new TextureLoader();
            var spritefont = new Spritefont(settings, context, textureLoader);
            var statisticsRenderer = new OnScreenClientStatisticsRenderer(spritefont);
            //var statisticsRenderer = new FormClientStatisticsRenderer();
            var statistics = new ClientStatistics(statisticsRenderer);
            var byteArrayPool = new ByteArrayPool();
            var streamReceiver = settings.AreFromFile 
                ? (IStreamReceiver)new FileStreamReceiver(pclWorkarounds, byteArrayPool, fileName ?? "stream.dat")
                : new TcpStreamReceiver(pclWorkarounds, settings, byteArrayPool);
            streamReceivingStage = new StreamReceivingStage(pclWorkarounds, streamReceiver);
            cpuDecompressionStage = new CpuDecompressionStage(pclWorkarounds, statistics, byteArrayPool);
            var mainThreadBorderStage = new MainThreadBorderStage(statistics);
            var textureInitializer = new TextureInitializer();
            var gpuProcessingStage = new GpuProcessingStage(pclWorkarounds, statistics, settings, context, textureInitializer);
            var timedBufferingStage = new TimeBufferingStage(settings, statistics, context);
            mainLoop = new MainLoop(pclWorkarounds, statistics, settings, context, this, mainThreadBorderStage, timedBufferingStage, statisticsRenderer, textureLoader);

            //statisticsRenderer.ShowForm();

            PipelineBuilder
                .BeginWith(streamReceivingStage)
                .ContinueWith(cpuDecompressionStage)
                .ContinueWith(mainThreadBorderStage)
                .ContinueWith(gpuProcessingStage)
                .EndWith(timedBufferingStage);

            cpuDecompressionStage.Start();
            streamReceivingStage.Start();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            mainLoop.OnNewFrame(e.Time);
            base.OnRenderFrame(e);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            streamReceivingStage.Stop();
            cpuDecompressionStage.Stop();
            base.OnClosing(e);
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.F11)
            {
                if (!isFullscreen)
                {
                    WindowState = WindowState.Fullscreen;
                    isFullscreen = true;
                }
                else
                {
                    WindowState = WindowState.Normal;
                    isFullscreen = false;
                }
            }
        }
    }
}