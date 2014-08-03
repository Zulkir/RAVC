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
using System.Runtime.InteropServices;
using Beholder;
using Beholder.Math;
using Beholder.Platform;
using Ravc.Utility.DataStructures;
using Win32;

namespace Ravc.WinHost
{
    public class MainLoop : IPipelinedProvider<GpuRawFrame>, IDisposable
    {
        private readonly IDevice device;
        private readonly IScreenCapturer screenCapturer;
        private IPipelinedConsumer<GpuRawFrame> nextStage;
        private IntSize oldSize;

        public IPipelinedConsumer<GpuRawFrame> NextStage { set { nextStage = value; } }

        public MainLoop(IDevice device, IScreenCapturer screenCapturer)
        {
            this.device = device;
            this.screenCapturer = screenCapturer;
        }

        public void Dispose()
        {
            screenCapturer.Dispose();
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
                }

                var context = device.ImmediateContext;

                context.ClearRenderTargetView(swapChain.GetCurrentColorBuffer(), Color4.CornflowerBlue);

                var frameTexturePooled = screenCapturer.Capture(context, beholderRect);
                var frameInfo = new FrameInfo(FrameType.Relative, realTime.TotalRealTime, size.Width, size.Height);
                var gpuRawFrame = new GpuRawFrame(frameInfo, frameTexturePooled);
                nextStage.Consume(gpuRawFrame);

                swapChain.EndScene();
                swapChain.Present();
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