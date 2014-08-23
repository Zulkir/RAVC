using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Ravc.Client.OglLib.Pcl;

namespace Ravc.Client.OglDesktop
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