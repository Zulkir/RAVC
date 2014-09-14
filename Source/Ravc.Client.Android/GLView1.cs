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
using Android.Content.Res;
using Android.Util;
using ObjectGL;
using ObjectGL.CachingImpl;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Platform.Android;
using Ravc.Client.OglLib;
using Ravc.Streaming;
using Ravc.Utility;
using Ravc.Utility.DataStructures;
using Context = Android.Content.Context;

namespace Ravc.Client.Android
{
    class GLView1 : AndroidGameView, IRavcGameWindow
    {
        private readonly IGL gl;
        private readonly INativeGraphicsContext nativeGraphicsContext;
        private readonly AssetManager assetManager;
        private ObjectGL.CachingImpl.Context context;
        private MainLoop mainLoop;
        private StreamReceivingStage streamReceivingStage;
        private CpuDecompressionStage cpuDecompressionStage;
        private double totalSeconds;

        public int ClientWidth { get { return Width; } }
        public int ClientHeight { get { return Height; } }

        public GLView1(Context context)
            : base(context)
        {
            gl = new AndroidGL();
            nativeGraphicsContext = new GameViewContext(this);
            assetManager = context.Assets;
            KeepScreenOn = true;
        }

        // Copied from GLTriangle20 example
        protected override void CreateFrameBuffer()
        {
            GLContextVersion = GLContextVersion.Gles3_0;

            // the default GraphicsMode that is set consists of (16, 16, 0, 0, 2, false)
            try
            {
                Log.Verbose("RAVC", "Loading with default settings");
                GraphicsMode = new AndroidGraphicsMode(0, 0, 0, 0, 0, false);
                base.CreateFrameBuffer();
                return;
            }
            catch (Exception ex)
            {
                Log.Verbose("RAVC", "{0}", ex);
            }

            try
            {
                Log.Verbose("RAVC", "Loading with custom Android settings (low mode)");
                GraphicsMode = new AndroidGraphicsMode(0, 0, 0, 0, 0, false);
                base.CreateFrameBuffer();
                return;
            }
            catch (Exception ex)
            {
                Log.Verbose("GLTriangle", "{0}", ex);
            }
            throw new Exception("Can't load egl, aborting");
        }

        // This gets called when the drawing surface is ready
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            Init();

            // Run the render loop
            Run();
        }

        private void Init()
        {
            context = new ObjectGL.CachingImpl.Context(gl, nativeGraphicsContext);
            var pclWorkarounds = new PclWorkarounds();
            var settings = new ClientSettings();
            var textureLoader = new TextureLoader(assetManager);
            var spritefont = new Spritefont(settings, context, textureLoader);
            var statisticsRenderer = new OnScreenClientStatisticsRenderer(spritefont);
            var statistics = new ClientStatistics(statisticsRenderer);
            var byteArrayPool = new ByteArrayPool();
            var streamReceiver = settings.FromFile
                ? (IStreamReceiver)new AssetStreamReceiver(assetManager, byteArrayPool, "stream.dat")
                : new TcpStreamReceiver(pclWorkarounds, settings, byteArrayPool);
            streamReceivingStage = new StreamReceivingStage(pclWorkarounds, streamReceiver);
            cpuDecompressionStage = new CpuDecompressionStage(pclWorkarounds, statistics, byteArrayPool);
            var mainThreadBorderStage = new MainThreadBorderStage(statistics);
            var textureInitializer = new TextureInitializer();
            var textureRenderer = new TextureRenderer(pclWorkarounds, settings, context);
            var gpuProcessingStage = new GpuProcessingStage(pclWorkarounds, statistics, settings, context, textureInitializer, textureRenderer);
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

        // This gets called on each frame render
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            totalSeconds += e.Time;
            mainLoop.OnNewFrame(e.Time);
        }

        public void StopReceiving()
        {
            if (streamReceivingStage != null)
            {
                streamReceivingStage.Stop();
                streamReceivingStage = null;
            }
            if (cpuDecompressionStage != null)
            {
                cpuDecompressionStage.Stop();
                cpuDecompressionStage = null;
            }
        }
    }
}
