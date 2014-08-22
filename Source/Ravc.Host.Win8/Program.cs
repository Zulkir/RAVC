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
using System.Linq;
using System.Windows.Forms;
using Beholder.Core;
using Beholder.Platform;
using Beholder.Utility.ForImplementations.Platform;
using Ravc.Encoding.Impl;
using Ravc.Host.WinLib;
using Ravc.Streaming;
using Ravc.Utility;
using Ravc.Utility.DataStructures;

namespace Ravc.Host.Win8
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            AppDomain.CurrentDomain.UnhandledException += (s, a) =>
                MessageBox.Show(a.ExceptionObject.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            Run(args);
        }

        static void Run(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var statistics = new HostStatistics();

            var eye = new Beholder.Eyes.SharpDX11.Winforms.WinformsEye();
            var graphicsWindowHandle = eye.CreateNewWindow(200, 150, "Stream", true);
            var colorFormatInfo = eye.Adapters[0].GetSupportedWindowedDisplayFormats().First(x => x.ExplicitFormat == ExplicitFormat.R8G8B8A8_UNORM);
#if DEBUG
            var deviceFlags = DeviceInitializationFlags.Debug;
#else
            var deviceFlags = DeviceInitializationFlags.None;
#endif
            eye.Initialize(eye.Adapters[0], graphicsWindowHandle, new SwapChainDescription(2, colorFormatInfo.ID, false, 0, Sampling.NoMultisampling, true), deviceFlags, new StraightforwardFileSystem());
            var device = eye.Device;

            var byteArrayPool = new ByteArrayPool();

            var logger = new FileLogger();
            var settings = new HostSettings();

            var broadcaster = settings.FromFile 
                ? (IStreamBroadcaster)new FileStreamBroadcaster() 
                : new TcpStreamBroadcaster(settings, logger);
            var broadcastingStage = new BroadcastStage(broadcaster);
            var cpuSideCodec = new CpuSideCodec(byteArrayPool, Memory.CopyBulk);
            var cpuCompressionStage = new CpuCompressionStage(statistics, cpuSideCodec);
            var gpuReadBackStage = new GpuReadBackStage(statistics, device, byteArrayPool, 1);
            var debugStage = new DebugStage(device);
            var gpuProcessingStage = new GpuProcessingStage(device);
            //var screenCaptor = new ScreenCaptor9(statistics, device);
            var screenCaptor = new ScreenCaptor11(statistics, logger, device);
            var mainLoop = new MainLoop(statistics, device, screenCaptor);

            PipelineBuilder
                .BeginWith(mainLoop)
                .ContinueWith(gpuProcessingStage)
                //.ContinueWith(debugStage)
                .ContinueWith(gpuReadBackStage)
                .ContinueWith(cpuCompressionStage)
                .EndWith(broadcastingStage);

            broadcaster.Start();
            cpuCompressionStage.Start();
            screenCaptor.Start();

            eye.NewFrame += mainLoop.OnNewFrame;

            statistics.ShowForm();

            using (mainLoop)
            using (eye)
                eye.RunLoop(device.PrimarySwapChain.Window);

            cpuCompressionStage.Stop();
            broadcaster.Stop();
            screenCaptor.Stop();
        }
    }
}
