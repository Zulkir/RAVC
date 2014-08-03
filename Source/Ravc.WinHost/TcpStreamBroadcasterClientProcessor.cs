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
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Ravc.Encoding;
using Ravc.Infrastructure;
using Ravc.Streaming;

namespace Ravc.WinHost
{
    public class TcpStreamBroadcasterClientProcessor
    {
        private readonly TcpClient tcpClient;
        private readonly ConcurrentQueue<CompressedFrame> localQueue;
        private readonly Thread workerThread;
        private readonly ILogger logger;
        private readonly EndPoint endPoint;
        private volatile bool isRunning;

        public bool IsRunning { get { return isRunning; } }

        public TcpStreamBroadcasterClientProcessor(TcpClient tcpClient, ILogger logger)
        {
            this.tcpClient = tcpClient;
            this.logger = logger;

            tcpClient.NoDelay = true;

            localQueue = new ConcurrentQueue<CompressedFrame>();
            workerThread = new Thread(DoWork);
            endPoint = tcpClient.Client.RemoteEndPoint;
        }

        public void Start()
        {
            isRunning = true;
            workerThread.Start();
        }

        public void Stop()
        {
            isRunning = false;
            workerThread.Join(2000);
            if (workerThread.IsAlive)
                workerThread.Abort();
        }

        public void BroadcastFrame(CompressedFrame frame)
        {
            if (!isRunning)
                throw new InvalidOperationException("Trying to broadcast a frame to a stopped client processor");
            frame.DataPooled.IncRefCount();
            localQueue.Enqueue(frame);
        }

        private void DoWork()
        {
            CompressedFrame frame = null;

            try
            {
                var metadataBuffer = new byte[4];
                var stream = tcpClient.GetStream();
                stream.WriteTimeout = 5000;
                while (isRunning)
                {
                    if (localQueue.TryDequeue(out frame))
                    {
                        stream.WriteInt((int)StreamMessageType.NextFrame, metadataBuffer);
                        stream.WriteInt(frame.CompressedSize, metadataBuffer);
                        stream.Write(frame.DataPooled.Item, 0, frame.CompressedSize);
                        frame.DataPooled.Release();
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                }
                stream.WriteInt((int)StreamMessageType.Stop, metadataBuffer);
            }
            catch (Exception ex)
            {
                logger.Exception("Client processing thread was killed by an exception", ex);
                isRunning = false;
            }
            finally
            {
                try
                {
                    tcpClient.Close();
                }
                catch (Exception ex)
                {
                    logger.Exception("Failed to close a TCP client " + endPoint, ex);
                }

                if (frame != null)
                    frame.DataPooled.Release();

                while (localQueue.TryDequeue(out frame))
                    frame.DataPooled.Release();
                logger.Info(string.Format("Processing of client {0} processing stopped", endPoint));
            }
        }
    }
}