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
using Ravc.Client.OglLib.Pcl;
using Ravc.Encoding;
using Ravc.Encoding.Impl;
using Ravc.Utility;
using Ravc.Utility.DataStructures;

namespace Ravc.Client.OglLib
{
    public class CpuDecompressionStage : IPipelineStage<CompressedFrame, UncompressedFrame>
    {
        private readonly IPclWorkarounds pclWorkarounds;
        private readonly IClientStatistics statistics;
        private readonly ICpuSideCodec cpuSideCodec;
        private readonly ConcurrentQueue<CompressedFrame> queue; 
        private readonly IPclThread workerThread;
        private readonly Stopwatch stopwatch;
        private IPipelinedConsumer<UncompressedFrame> nextStage;
        private volatile bool isRunning;

        public IPipelinedConsumer<UncompressedFrame> NextStage { set { nextStage = value; } }
        public bool IsOverloaded { get { return queue.Count > 10; } }

        public CpuDecompressionStage(IPclWorkarounds pclWorkarounds, IClientStatistics statistics, ByteArrayPool byteArrayPool)
        {
            this.pclWorkarounds = pclWorkarounds;
            this.statistics = statistics;
            cpuSideCodec = new CpuSideCodec(pclWorkarounds, byteArrayPool);
            queue = new ConcurrentQueue<CompressedFrame>();
            workerThread = pclWorkarounds.CreateThread(DoWork);
            stopwatch = new Stopwatch();
        }

        public void Consume(CompressedFrame input)
        {
            queue.Enqueue(input);
            statistics.OnCpuDecompressionQueue(queue.Count);
        }

        public void Start()
        {
            isRunning = true;
            workerThread.Start();
        }

        public void Stop()
        {
            isRunning = false;
            workerThread.Join(1000);
            if (workerThread.IsAlive)
                workerThread.Abort();
        }

        private void DoWork()
        {
            while (isRunning)
            {
                CompressedFrame compressedFrame;
                if (!nextStage.IsOverloaded && queue.TryDequeue(out compressedFrame))
                {
                    statistics.OnCpuDecompressionQueue(queue.Count);
                    stopwatch.Restart();
                    var decompressedFrame = cpuSideCodec.Decompress(compressedFrame);
                    stopwatch.Stop();
                    statistics.OnCpuDecoding(stopwatch.Elapsed.TotalMilliseconds);
                    compressedFrame.DataPooled.Release();
                    nextStage.Consume(decompressedFrame);
                }
                else
                {
                    pclWorkarounds.ThreadSleep(5);
                }
            }
        }
    }
}