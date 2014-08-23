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

using Ravc.Client.OglLib.Pcl;
using Ravc.Encoding;
using Ravc.Streaming;
using Ravc.Utility;

namespace Ravc.Client.OglLib
{
    public class TcpStreamReceiver : IStreamReceiver
    {
        private readonly IPclWorkarounds pclWorkarounds;
        private readonly ByteArrayPool byteArrayPool;
        private readonly IPclTcpClient tcpClient;
        private readonly byte[] metaDataBuffer;
        private readonly string hostname;
        private readonly int port;

        public TcpStreamReceiver(IPclWorkarounds pclWorkarounds, IClientSettings settings, ByteArrayPool byteArrayPool)
        {
            this.byteArrayPool = byteArrayPool;
            this.pclWorkarounds = pclWorkarounds;
            hostname = settings.TcpHostName;
            port = settings.TcpPort;
            tcpClient = pclWorkarounds.CreateTcpClient();
            metaDataBuffer = new byte[4];

            tcpClient.NoDelay = true;
        }

        public void Start()
        {
            tcpClient.Connect(hostname, port);
        }

        public StreamMessageType ReadMessageType()
        {
            var stream = tcpClient.GetStream();
            return (StreamMessageType)stream.ReadInt(metaDataBuffer);
        }

        public CompressedFrame ReadFrame()
        {
            var stream = tcpClient.GetStream();
            int dataSize = stream.ReadInt(metaDataBuffer);
            var pooledData = byteArrayPool.Extract(dataSize);
            stream.ReadCompletely(pooledData.Item, 0, dataSize);
            return new CompressedFrame(pooledData, dataSize);
        }

        public void Stop()
        {
            tcpClient.Close();
        }
    }
}