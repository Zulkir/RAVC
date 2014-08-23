using System.IO;
using System.Net.Sockets;
using Ravc.Client.OglLib.Pcl;

namespace Ravc.Client.OglDesktop
{
    public class PclTcpClient : IPclTcpClient
    {
        private readonly TcpClient tcpClient;

        public PclTcpClient(TcpClient tcpClient)
        {
            this.tcpClient = tcpClient;
        }

        public bool NoDelay { set { tcpClient.NoDelay = value; } }

        public void Close() { tcpClient.Close(); }
        public void Connect(string hostname, int port) { tcpClient.Connect(hostname, port); }
        public Stream GetStream() { return tcpClient.GetStream(); }
    }
}