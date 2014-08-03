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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Ravc.WinHost
{
    public class HostStatistics : IHostStatistics
    {
        private readonly Stopwatch stopwatch;
        private readonly List<int> frameSizes;
        private readonly object updateLock = new object();

        public int FramesPerSecond { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public long TotalBytes { get; private set; }
        public long AverageBitrate { get; private set; }

        public HostStatistics()
        {
            stopwatch = new Stopwatch();
            frameSizes = new List<int>();
        }

        public void Initialize()
        {
            stopwatch.Start();
        }

        public void OnNewFrame(FrameInfo frameInfo, int compressedSize)
        {
            lock (updateLock)
            {
                frameSizes.Add(compressedSize);
                TotalBytes += compressedSize;

                if (frameSizes.Count == 10)
                {
                    FramesPerSecond = (int)Math.Round(frameSizes.Count / stopwatch.Elapsed.TotalSeconds);
                    Width = frameInfo.AlignedWidth;
                    Height = frameInfo.AlignedHeight;

                    stopwatch.Restart();
                    long totalBytesPast30Frames = frameSizes.Select(x => (long)x).Sum();
                    AverageBitrate = totalBytesPast30Frames * FramesPerSecond * 8 / 10;
                    frameSizes.Clear();
                }
            }
        }

        public void Reset()
        {
            frameSizes.Clear();
            TotalBytes = 0;
            stopwatch.Restart();
        }
    }
}