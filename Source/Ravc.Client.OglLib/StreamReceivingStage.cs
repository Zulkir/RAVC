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
using Ravc.Client.OglLib.Pcl;
using Ravc.Encoding;
using Ravc.Streaming;
using Ravc.Utility.DataStructures;

namespace Ravc.Client.OglLib
{
    public class StreamReceivingStage : IPipelinedProvider<CompressedFrame>
    {
        private readonly IPclWorkarounds pclWorkarounds;
        private readonly IStreamReceiver streamReceiver;
        private readonly IPclThread workerThread;
        private IPipelinedConsumer<CompressedFrame> nextStage;
        private volatile bool isRunning;

        public IPipelinedConsumer<CompressedFrame> NextStage { set { nextStage = value; } }

        public StreamReceivingStage(IPclWorkarounds pclWorkarounds, IStreamReceiver streamReceiver)
        {
            this.pclWorkarounds = pclWorkarounds;
            this.streamReceiver = streamReceiver;
            workerThread = pclWorkarounds.CreateThread(DoWork);
        }

        public void Start()
        {
            isRunning = true;
            streamReceiver.Start();
            workerThread.Start();
        }

        public void Stop()
        {
            isRunning = false;
            streamReceiver.Stop();
            workerThread.Join(1000);
            if (workerThread.IsAlive)
                workerThread.Abort();
        }

        private void DoWork()
        {
            while (isRunning)
            {
                if (nextStage.IsOverloaded)
                {
                    pclWorkarounds.ThreadSleep(5);
                    continue;
                }

                var type = streamReceiver.ReadMessageType();
                switch (type)
                {
                    case StreamMessageType.NextFrame:
                        var frame = streamReceiver.ReadFrame();
                        nextStage.Consume(frame);
                        break;
                    case StreamMessageType.Stop:
                        isRunning = false;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}