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

using System.Globalization;
using Ravc.Utility;

namespace Ravc.Host.WinLib
{
    public class HostStatistics : IHostStatistics
    {
        private readonly StatisticsForm form;
        private readonly StatCounter frameTime;
        private readonly StatCounter captureTime;
        private readonly StatCounter gpuCallsTime;
        private readonly StatCounter readbackTime;
        private readonly StatCounter presentTime;
        private readonly StatCounter cpuProcessingQueue;
        private readonly StatCounter compressionTime;

        private int width;
        private int height;
        private int compressedBytes;

        public HostStatistics()
        {
            form = new StatisticsForm();

            frameTime = new StatCounter();
            captureTime = new StatCounter();
            gpuCallsTime = new StatCounter();
            readbackTime = new StatCounter();
            presentTime = new StatCounter();
            cpuProcessingQueue = new StatCounter();
            compressionTime = new StatCounter();
        }

        public void ShowForm()
        {
            form.Show();
        }

        public void OnPresent(double frameSeconds, double presentMs)
        {
            frameTime.AddValue(frameSeconds);
            presentTime.AddValue(presentMs);

            if (frameTime.Accumulated > 1.0)
            {
                form.lbFps.Text = FormatValue(1.0 / frameTime.Average);
                form.lbSize.Text = string.Format("{0}x{1} ({2:0.##e+0})", width, height, width * height);
                form.lbCapture.Text = FormatMinMax(captureTime);
                form.lbGpuCallsTime.Text = FormatMinMax(gpuCallsTime);
                form.lbReadbackTime.Text = FormatMinMax(readbackTime);
                form.lbPresentTime.Text = FormatMinMax(presentTime);
                form.lbCpuProcessingQueue.Text = FormatQueue(cpuProcessingQueue);
                form.lbCompressionTime.Text = FormatMinMax(compressionTime);
                form.lbBitrate.Text = string.Format(CultureInfo.InvariantCulture, "{0:F3} Mbit/s", (double)compressedBytes * 8.0 / frameTime.Accumulated / 1024.0 / 1024);

                frameTime.Reset();
                captureTime.Reset();
                gpuCallsTime.Reset();
                readbackTime.Reset();
                presentTime.Reset();
                cpuProcessingQueue.Reset();
                compressionTime.Reset();

                compressedBytes = 0;
            }
        }

        private static string FormatMinMax(StatCounter statCounter)
        {
            return string.Format("{0:0.#}--{1:0.#}   ({2:0.##})", statCounter.Min, statCounter.Max, statCounter.Average);
        }

        private static string FormatQueue(StatCounter statCounter)
        {
            return string.Format("{0:0}--{1:0}   ({2:0.#})", statCounter.Min, statCounter.Max, statCounter.Average);
        }

        private static string FormatValue(double value, int decimals = 0)
        {
            return value.ToString("N" + decimals.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        }

        public void OnCapture(double ms)
        {
            captureTime.AddValue(ms);
        }

        public void OnGpuCalls(double ms)
        {
            gpuCallsTime.AddValue(ms);
        }

        public void OnReadback(double ms)
        {
            readbackTime.AddValue(ms);
        }

        public void OnCpuProcessingQueue(int queueCount)
        {
            cpuProcessingQueue.AddValue(queueCount);
        }

        public void OnFrameCompressed(int bytes, double ms)
        {
            compressionTime.AddValue(ms);
            compressedBytes += bytes;
        }

        public void OnResize(int newWidth, int newHeight)
        {
            width = newWidth;
            height = newHeight;
        }
    }
}