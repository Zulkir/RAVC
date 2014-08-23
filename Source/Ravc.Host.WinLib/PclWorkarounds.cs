using System;
using Ravc.Pcl;

namespace Ravc.Host.WinLib
{
    public class PclWorkarounds : IPclWorkarounds
    {
        public void CopyBulk(IntPtr destination, IntPtr source, int numBytes)
        {
            Memory.CopyBulk(destination, source, numBytes);
        }
    }
}