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

using System.Collections.Concurrent;
using System.Threading;
using Ravc.Encoding;
using Ravc.Utility.DataStructures;

namespace Ravc.WinHost
{
    public class CpuCompressionStage : IPipelineStage<UncompressedFrame, CompressedFrame>
    {
        private readonly ICpuSideCodec cpuSideCodec;
        private readonly IHostStatistics statistics;
        private readonly ConcurrentQueue<UncompressedFrame> queue;
        private IPipelinedConsumer<CompressedFrame> nextStage; 
        private Thread compressorThread;
        private bool isWorking;

        public IPipelinedConsumer<CompressedFrame> NextStage { set { nextStage = value; } }
        public bool IsOverloaded { get { return nextStage.IsOverloaded; } }

        public CpuCompressionStage(ICpuSideCodec cpuSideCodec, IHostStatistics statistics)
        {
            this.cpuSideCodec = cpuSideCodec;
            this.statistics = statistics;
            queue = new ConcurrentQueue<UncompressedFrame>();
        }

        private void DoWork()
        {
            while (isWorking)
            {
                UncompressedFrame frame;
                if (queue.TryDequeue(out frame))
                {
                    var compressedFrame = cpuSideCodec.Compress(frame);
                    statistics.OnNewFrame(frame.Info, compressedFrame.CompressedSize);
                    frame.DataPooled.Release();
                    nextStage.Consume(compressedFrame);
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
        }

        public void Consume(UncompressedFrame input)
        {
            if (isWorking)
                queue.Enqueue(input);
        }

        public void Start()
        {
            isWorking = true;
            compressorThread = new Thread(DoWork);
            compressorThread.Start();
        }

        public void Stop()
        {
            isWorking = false;
            compressorThread.Join();
            UncompressedFrame junk;
            while (queue.TryDequeue(out junk))
                junk.DataPooled.Release();
        }
    }
}