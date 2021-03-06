﻿#region License
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

using System.Globalization;
using System.IO;
using Ravc.Client.OglLib;

namespace Ravc.Client.OglDesktop
{
    public class ClientSettings : IClientSettings 
    {
        public bool IsEs { get { return false; } }
        public string PrimaryTcpHostName { get; private set; }
        public string SecondaryTcpHostName { get; private set; }
        public int TcpPort { get; private set; }
        public bool AreFromFile { get; private set; }
        public int QueueCapacity { get; private set; }
        public int TimeBufferInitiationLength { get; private set; }
        public float BufferingOffset { get; private set; }
        public bool ShowDebugInfo { get; private set; }

        public ClientSettings()
        {
            try
            {
                using (var reader = new StreamReader("settings.txt"))
                {
                    PrimaryTcpHostName = reader.ReadLine();
                    SecondaryTcpHostName = null;
                    TcpPort = int.Parse(reader.ReadLine());
                    AreFromFile = bool.Parse(reader.ReadLine());
                    QueueCapacity = int.Parse(reader.ReadLine());
                    TimeBufferInitiationLength = int.Parse(reader.ReadLine());
                    BufferingOffset = float.Parse(reader.ReadLine(), CultureInfo.InvariantCulture);
                    ShowDebugInfo = bool.Parse(reader.ReadLine());
                }
            }
            catch
            {
                PrimaryTcpHostName = "127.0.0.1";
                SecondaryTcpHostName = null;
                TcpPort = 7123;
                AreFromFile = false;
                QueueCapacity = 5;
                TimeBufferInitiationLength = 5;
                BufferingOffset = 1;
                ShowDebugInfo = true;
            }
        }
    }
}