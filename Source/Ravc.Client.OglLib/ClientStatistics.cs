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

using System.Diagnostics;
using System.Globalization;
using Ravc.Utility;

namespace Ravc.Client.OglLib
{
    public class ClientStatistics : IClientStatistics
    {
        private readonly IClientStatisticsRenderer renderer;
        private readonly StatCounter frameTime;
        private readonly StatCounter timeLag;
        private readonly StatCounter presentTime;
        private readonly StatCounter gpuUploadTime;
        private readonly StatCounter borderPassTime;
        private readonly StatCounter cpuDecodingTime;
        private readonly StatCounter timeBufferingQueue;
        private readonly StatCounter mainThreadBarrierQueue;
        private readonly StatCounter cpuProcessingQueue;

        private double previousTimeStamp;
        private int frames;
        private int timedFrames;
        private int idleFrames;
        private int skippedFrames;

        public ClientStatistics(IClientStatisticsRenderer renderer)
        {
            this.renderer = renderer;

            frameTime = new StatCounter();
            timeLag = new StatCounter();
            presentTime = new StatCounter();
            gpuUploadTime = new StatCounter();
            borderPassTime = new StatCounter();
            cpuDecodingTime = new StatCounter();
            timeBufferingQueue = new StatCounter();
            mainThreadBarrierQueue = new StatCounter();
            cpuProcessingQueue = new StatCounter();
        }

        public void OnFrameRendered()
        {
            var currentTimestamp = (float)Stopwatch.GetTimestamp() / Stopwatch.Frequency;
            frameTime.AddValue(currentTimestamp - previousTimeStamp);
            previousTimeStamp = currentTimestamp;

            if (timedFrames == 0)
                idleFrames++;
            else if (timedFrames > 1)
                skippedFrames += timedFrames - 1;

            frames++;
            if (frameTime.Accumulated > 0.25f)
            {
                renderer.Fps = FormatValue(1.0 / frameTime.Average);
                renderer.IdleFrames = FormatValue(idleFrames);
                renderer.SkippedFrames = FormatValue(skippedFrames);
                renderer.TimeLag = FormatMinMax(timeLag);
                renderer.PresentTime = FormatMinMax(presentTime);
                renderer.GpuUploadTime = FormatMinMax(gpuUploadTime);
                renderer.BorderPassTime = FormatMinMax(borderPassTime);
                renderer.CpuDecodingTime = FormatMinMax(cpuDecodingTime);
                renderer.TimeBufferingQueue = FormatQueue(timeBufferingQueue);
                renderer.MainThreadQueue = FormatQueue(mainThreadBarrierQueue);
                renderer.CpuProcessingQueue = FormatQueue(cpuProcessingQueue);

                frameTime.Reset();
                timeLag.Reset();
                presentTime.Reset();
                gpuUploadTime.Reset();
                borderPassTime.Reset();
                cpuDecodingTime.Reset();
                timeBufferingQueue.Reset();
                mainThreadBarrierQueue.Reset();
                cpuProcessingQueue.Reset();

                frames = 0;
            }

            timedFrames = 0;
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

        public void OnTimedFrameExtracted()
        {
            timedFrames++;
        }

        public void OnTimeLag(double lag)
        {
            timeLag.AddValue(lag * 1000);
        }

        public void OnSwapChain(double time)
        {
            presentTime.AddValue(time);
        }

        public void OnGpuUpload(double uploadTime)
        {
            gpuUploadTime.AddValue(uploadTime);
        }

        public void OnBorderPass(double time)
        {
            borderPassTime.AddValue(time);
        }

        public void OnCpuDecoding(double time)
        {
            cpuDecodingTime.AddValue(time);
        }

        public void OnTimeBufferQueue(int queueCount)
        {
            timeBufferingQueue.AddValue(queueCount);
        }

        public void OnMainThreadQueue(int queueCount)
        {
            mainThreadBarrierQueue.AddValue(queueCount);
        }

        public void OnCpuDecompressionQueue(int queueCount)
        {
            cpuProcessingQueue.AddValue(queueCount);
        }
    }
}