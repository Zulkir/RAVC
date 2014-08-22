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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Ravc.Encoding;
using Ravc.Infrastructure;
using Ravc.Streaming;
using Ravc.Utility;

namespace Ravc.Host.WinLib
{
    public class TcpStreamBroadcaster : IStreamBroadcaster
    {
        private readonly ConcurrentHashSet<TcpStreamBroadcasterClientProcessor> activeClients;
        private readonly ILogger logger;
        private readonly IPAddress localaddr;
        private readonly int port;
        private TcpListener tcpListener;
        private Thread listenerThread;
        private volatile bool isRunning;

        public TcpStreamBroadcaster(IHostSettings settings, ILogger logger)
        {
            this.logger = logger;
            localaddr = IPAddress.Parse(settings.TcpHostName);
            port = settings.TcpPort;
            activeClients = new ConcurrentHashSet<TcpStreamBroadcasterClientProcessor>();
        }

        public void Start()
        {
            tcpListener = new TcpListener(localaddr, port);
            tcpListener.Start();
            isRunning = true;
            listenerThread = new Thread(DoListen);
            listenerThread.Start();
        }

        public void Stop()
        {
            isRunning = false;
            listenerThread.Join(2000);
            if (listenerThread.IsAlive)
                listenerThread.Abort();
            try
            {
                tcpListener.Stop();
            }
            catch (Exception ex)
            {
                logger.Exception("Failed to stop the listener", ex);
            }

            foreach (var clientProcessor in activeClients)
                clientProcessor.Stop();
            activeClients.Clear();
        }

        public void BroadcastFrame(CompressedFrame frame)
        {
            var deathNote = new List<TcpStreamBroadcasterClientProcessor>();
            foreach (var clientProcessor in activeClients)
                if (clientProcessor.IsRunning)
                    clientProcessor.BroadcastFrame(frame);
                else
                    deathNote.Add(clientProcessor);
            foreach (var clientProcessor in deathNote)
                activeClients.TryRemove(clientProcessor);
            frame.DataPooled.Release();
        }

        private void DoListen()
        {
            try
            {
                logger.Info("Listener started");
                while (isRunning)
                {
                    if (tcpListener.Pending())
                        try
                        {
                            var client = tcpListener.AcceptTcpClient();
                            logger.Info("Accepted a client " + client.Client.RemoteEndPoint);
                            var clientProcessor = new TcpStreamBroadcasterClientProcessor(client, logger);
                            clientProcessor.Start();
                            if (!activeClients.TryAdd(clientProcessor))
                                clientProcessor.Stop();
                        }
                        catch (Exception ex)
                        {
                            logger.Exception("Error during accepting a client", ex);
                        }
                    else
                        Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                logger.Exception("Listener thread was killed by an exception", ex);
            }
            logger.Info("Listener stopped");
        }
    }
}