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
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Ravc.Client.OglLib.Pcl;

namespace Ravc.Client.Android
{
    public class PclWorkarounds : IPclWorkarounds
    {
        public void CopyBulk(IntPtr destination, IntPtr source, int numBytes)
        {
            Memory.CopyBulk(destination, source, numBytes);
        }

        public IPclThread CreateThread(Action threadProc)
        {
            return new PclThread(new Thread(() => threadProc()));
        }

        public IPclTcpClient CreateTcpClient()
        {
            return new PclTcpClient(new TcpClient());
        }

        public Stream FileOpenRead(string path)
        {
            return File.OpenRead(path);
        }

        public void ThreadSleep(int milliseconds)
        {
            Thread.Sleep(milliseconds);
        }
    }
}