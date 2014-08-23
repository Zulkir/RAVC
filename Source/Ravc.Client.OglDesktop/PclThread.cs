using System.Threading;
using Ravc.Client.OglLib.Pcl;

namespace Ravc.Client.OglDesktop
{
    public class PclThread : IPclThread
    {
        private readonly Thread thread;

        public PclThread(Thread thread)
        {
            this.thread = thread;
        }

        public bool IsAlive { get { return thread.IsAlive; } }

        public void Start() { thread.Start(); }
        public void Abort() { thread.Abort(); }
        public void Join(int timeoutMilliseconds) { thread.Join(timeoutMilliseconds); }
    }
}