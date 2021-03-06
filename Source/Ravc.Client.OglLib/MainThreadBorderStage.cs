﻿#region License
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

using System.Collections.Concurrent;
using System.Diagnostics;
using Ravc.Encoding;
using Ravc.Utility.DataStructures;

namespace Ravc.Client.OglLib
{
    public class MainThreadBorderStage : IPipelineStage<UncompressedFrame, UncompressedFrame>, IMainThreadBorderStage
    {
        private readonly IClientStatistics statistics;
        private readonly ConcurrentQueue<UncompressedFrame> queue;
        private readonly Stopwatch stopwatch;
        private IPipelinedConsumer<UncompressedFrame> nextStage;

        public IPipelinedConsumer<UncompressedFrame> NextStage { set { nextStage = value; } }
        public bool IsOverloaded { get { return queue.Count > 7; } }

        public MainThreadBorderStage(IClientStatistics statistics)
        {
            this.statistics = statistics;
            queue = new ConcurrentQueue<UncompressedFrame>();
            stopwatch = new Stopwatch();
        }

        public void Consume(UncompressedFrame input)
        {
            queue.Enqueue(input);
            statistics.OnMainThreadQueue(queue.Count);
        }

        public void DoMainThreadProcessing()
        {
            if (nextStage.IsOverloaded)
                return;

            statistics.OnMainThreadQueue(queue.Count);
            stopwatch.Restart();
            for (int i = 0; i < 3; i++)
            {
                UncompressedFrame uncompressedFrame;
                if (!nextStage.IsOverloaded && queue.TryDequeue(out uncompressedFrame))
                    nextStage.Consume(uncompressedFrame);
            }
            stopwatch.Stop();
            statistics.OnBorderPass(stopwatch.Elapsed.TotalMilliseconds);
            statistics.OnMainThreadQueue(queue.Count);
        }
    }
}