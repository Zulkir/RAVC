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

namespace Ravc.WinformsOglClient
{
    public class ClientStatistics : IClientStatistics
    {
        private readonly StatisticsForm form;
        private readonly StatCounter frameTime;
        private readonly StatCounter timeLag;
        private readonly StatCounter gpuUploadTime;
        private readonly StatCounter timeBufferingQueue;
        private readonly StatCounter mainThreadBarrierQueue;
        private readonly StatCounter cpuProcessingQueue;

        private int frames;
        private int timedFrames;
        private int idleFrames;
        private int skippedFrames;

        public ClientStatistics()
        {
            form = new StatisticsForm();

            frameTime = new StatCounter();
            timeLag = new StatCounter();
            gpuUploadTime = new StatCounter();
            timeBufferingQueue = new StatCounter();
            mainThreadBarrierQueue = new StatCounter();
            cpuProcessingQueue = new StatCounter();
        }

        public void ShowForm()
        {
            form.Show();
        }

        public void OnFrameRendered(double elapsedSeconds)
        {
            frameTime.AddValue(elapsedSeconds);

            if (timedFrames == 0)
                idleFrames++;
            else if (timedFrames > 1)
                skippedFrames += timedFrames - 1;

            frames++;
            if (frames % 16 == 0)
            {
                form.lbFps.Text = FormatValue(1.0 / frameTime.Average);
                form.lbIdleFrames.Text = FormatValue(idleFrames);
                form.lbSkippedFrames.Text = FormatValue(skippedFrames);
                form.lbTimeLag.Text = FormatMinMax(timeLag);
                form.lbGpuUploadTime.Text = FormatValue(gpuUploadTime.Average, 1);
                form.lbTimeBufferingQueue.Text = FormatMinMax(timeBufferingQueue);
                form.lbMainThreadQueue.Text = FormatMinMax(mainThreadBarrierQueue);
                form.lbCpuProcessingQueue.Text = FormatMinMax(cpuProcessingQueue);

                frameTime.Reset();
                timeLag.Reset();
                gpuUploadTime.Reset();
                timeBufferingQueue.Reset();
                mainThreadBarrierQueue.Reset();
                cpuProcessingQueue.Reset();

                frames = 0;
            }

            timedFrames = 0;
        }

        private static string FormatMinMax(StatCounter statCounter)
        {
            return string.Format("{0}--{1}", FormatValue(statCounter.Min), FormatValue(statCounter.Max));
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

        public void OnGpuUpload(double uploadTime)
        {
            gpuUploadTime.AddValue(uploadTime);
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