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
using System.Runtime.InteropServices;
using System.Threading;
using Beholder;
using Beholder.Libraries.SharpDX11.Platform;
using Beholder.Math;
using Beholder.Platform;
using Ravc.Utility.DataStructures;
using Win32;

namespace Ravc.Host.WinLib
{
    public class MainLoop : IPipelinedProvider<GpuRawFrame>, IDisposable
    {
        private static readonly int[] DetailSequence = { 1, 1, 1, 1, 1, 1, 1, 0 };
        //private static readonly int[] DetailSequence = { 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0 };
        //private static readonly int[] DetailSequence = { 2, 1, 2, 1, 2, 1, 2, 0 };
        //private static readonly int[] DetailSequence = { 1 };
        //private static readonly int[] DetailSequence = { 0 };
        //private static readonly int[] DetailSequence = new[] { 0, 1, 0, 0, 0, 0, 0, 0 };

        private static readonly int[] DiffThresholdSequence = { 5, 2, 5, 0 };

        private readonly IHostStatistics statistics;
        private readonly IDevice device;
        private readonly IScreenCaptor screenCaptor;
        private readonly Stopwatch stopwatch;
        private readonly bool isD3d11;
        private double previousTimestamp;
        private int frameIndex;
        private int processedFrameIndex;
        private IPipelinedConsumer<GpuRawFrame> nextStage;
        private IntSize oldSize;

        public IPipelinedConsumer<GpuRawFrame> NextStage { set { nextStage = value; } }

        public MainLoop(IHostStatistics statistics, IDevice device, IScreenCaptor screenCaptor)
        {
            this.statistics = statistics;
            this.device = device;
            this.screenCaptor = screenCaptor;
            stopwatch = new Stopwatch();
            isD3d11 = device.Adapter is CAdapter;
        }

        public void Dispose()
        {
            screenCaptor.Dispose();
        }

        public void OnNewFrame(IRealTime realTime)
        {
            var context = device.ImmediateContext;
            var swapChain = device.PrimarySwapChain;
            if (swapChain.BeginScene())
            {
                context.ClearRenderTargetView(swapChain.GetCurrentColorBuffer(), Color4.Black);

                var newTimestamp = (double)Stopwatch.GetTimestamp() / Stopwatch.Frequency;
                if (newTimestamp - previousTimestamp > 0.001)
                {
                    previousTimestamp = newTimestamp;

                    var focusedWindowHwnd = GetFocusedWindowHwnd();
                    RECT clientRect;
                    var clientPoint = new POINT(0, 0);
                    Functions.GetClientRect(focusedWindowHwnd, out clientRect);
                    Functions.ClientToScreen(focusedWindowHwnd, ref clientPoint);
                    var beholderRect = new IntRectangle(
                        Math.Max(clientPoint.X, 0),
                        Math.Max(clientPoint.Y, 0),
                        Math.Max(clientRect.right - clientRect.left, 1),
                        Math.Max(clientRect.bottom - clientRect.top, 1));
                    var size = new IntSize(beholderRect.Width, beholderRect.Height);
                    if (!size.Equals(oldSize) && size.Width > 0 && size.Height > 0)
                    {
                        oldSize = size;
                        statistics.OnResize(size.Width, size.Height);
                    }

                    if (!isD3d11)
                        context.ClearRenderTargetView(swapChain.GetCurrentColorBuffer(), Color4.CornflowerBlue);

                    GpuRawFrame capturedFrame;
                    if (screenCaptor.TryGetCaptured(context, beholderRect, FrameType.Relative,
                        DiffThresholdSequence[processedFrameIndex % DiffThresholdSequence.Length],
                        DetailSequence[processedFrameIndex % DetailSequence.Length], out capturedFrame))
                    {
                        if (frameIndex % 2 == 0)
                        {
                            stopwatch.Restart();
                            nextStage.Consume(capturedFrame);
                            stopwatch.Stop();
                            statistics.OnGpuCalls(stopwatch.Elapsed.TotalMilliseconds);
                            processedFrameIndex++;
                        }
                        else
                        {
                            capturedFrame.TexturePooled.Release();
                        }
                    }
                }
                else
                {
                    Thread.Sleep(1);
                }

                swapChain.EndScene();

                stopwatch.Restart();
                //swapChain.Present();
                //var output = context.Device.Adapter.Outputs[0];
                //if (output is COutput)
                //    ((COutput)output).DXGIOutput.WaitForVerticalBlank();
                //else
                //    swapChain.Present();
                swapChain.Present();
                stopwatch.Stop();
                statistics.OnPresent(realTime.ElapsedRealTime, stopwatch.Elapsed.TotalMilliseconds);

                frameIndex++;
            }
        }

        static readonly int sizeOfGUITHREADINFO = Marshal.SizeOf(typeof(GUITHREADINFO));

        static IntPtr GetFocusedWindowHwnd()
        {
            var gui = new GUITHREADINFO
            {
                cbSize = sizeOfGUITHREADINFO
            };
            Functions.GetGUIThreadInfo(0, out gui);
            return gui.hwndFocus;
        }
    }
}