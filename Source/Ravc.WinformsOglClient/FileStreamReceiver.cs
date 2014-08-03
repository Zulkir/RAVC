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

using System.IO;
using Ravc.Encoding;
using Ravc.Streaming;
using Ravc.Utility;

namespace Ravc.WinformsOglClient
{
    public class FileStreamReceiver : IStreamReceiver
    {
        private readonly ByteArrayPool byteArrayPool;
        private readonly string fileName;
        private readonly byte[] metadataBuffer;
        private Stream stream;

        public FileStreamReceiver(ByteArrayPool byteArrayPool, string fileName)
        {
            this.byteArrayPool = byteArrayPool;
            this.fileName = fileName;
            metadataBuffer = new byte[4];
        }

        public void Start()
        {
            stream = File.OpenRead(fileName);
        }

        public StreamMessageType ReadMessageType()
        {
            return (StreamMessageType)stream.ReadInt(metadataBuffer);
        }

        public CompressedFrame ReadFrame()
        {
            int size = stream.ReadInt(metadataBuffer);
            var dataPooled = byteArrayPool.Extract(size);
            stream.ReadCompletely(dataPooled.Item, 0, size);
            return new CompressedFrame(dataPooled, size);
        }

        public void Stop()
        {
            stream.Close();
        }
    }
}