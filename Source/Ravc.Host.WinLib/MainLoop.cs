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
using Beholder;
using Beholder.Math;
using Beholder.Platform;
using Ravc.Utility.DataStructures;
using Win32;

namespace Ravc.Host.WinLib
{
    public class MainLoop : IPipelinedProvider<GpuRawFrame>, IDisposable
    {
        //private static readonly int[] DetailSequence = new[] { 2, 1, 2, 1, 2, 1, 2, 0 };
        //private static readonly int[] DetailSequence = new[] { 1, 1, 1, 1, 1, 1, 1, 1 };
        //private static readonly int[] DetailSequence = new[] { 0 };
        private static readonly int[] DetailSequence = new[] { 0, 1, 0, 0, 0, 0, 0, 0 };

        private readonly IHostStatistics statistics;
        private readonly IDevice device;
        private readonly IScreenCaptor screenCaptor;
        private readonly Stopwatch stopwatch;
        private int frameIndex;
        private IPipelinedConsumer<GpuRawFrame> nextStage;
        private IntSize oldSize;

        public IPipelinedConsumer<GpuRawFrame> NextStage { set { nextStage = value; } }

        public MainLoop(IHostStatistics statistics, IDevice device, IScreenCaptor screenCaptor)
        {
            this.statistics = statistics;
            this.device = device;
            this.screenCaptor = screenCaptor;
            stopwatch = new Stopwatch();
        }

        public void Dispose()
        {
            screenCaptor.Dispose();
        }

        public void OnNewFrame(IRealTime realTime)
        {
            var swapChain = device.PrimarySwapChain;
            if (swapChain.BeginScene())
            {
                var focusedWindowHwnd = GetFocusedWindowHwnd();
                RECT windowRect;
                RECT clientRect;
                Functions.GetWindowRect(focusedWindowHwnd, out windowRect);
                Functions.GetClientRect(focusedWindowHwnd, out clientRect);
                var beholderRect = new IntRectangle(
                    Math.Max(windowRect.left, 0), 
                    Math.Max(windowRect.top, 0), 
                    Math.Max(clientRect.right - clientRect.left, 1), 
                    Math.Max(clientRect.bottom - clientRect.top, 1));
                var size = new IntSize(beholderRect.Width, beholderRect.Height);
                if (!size.Equals(oldSize) && size.Width > 0 && size.Height > 0)
                {
                    oldSize = size;
                    statistics.OnResize(size.Width, size.Height);
                }

                var context = device.ImmediateContext;

                context.ClearRenderTargetView(swapChain.GetCurrentColorBuffer(), Color4.CornflowerBlue);

                GpuRawFrame capturedFrame;
                if (screenCaptor.TryGetCaptured(context, beholderRect, FrameType.Relative, DetailSequence[frameIndex % DetailSequence.Length], out capturedFrame))
                {
                    stopwatch.Restart();
                    nextStage.Consume(capturedFrame);
                    stopwatch.Stop();
                    statistics.OnGpuCalls(stopwatch.Elapsed.TotalMilliseconds);
                }

                swapChain.EndScene();

                stopwatch.Restart();
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