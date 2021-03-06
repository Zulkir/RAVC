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

using System;
using System.IO;
using ObjectGL.Api.Objects.Resources;
using Ravc.Client.OglLib.Pcl;

namespace Ravc.Client.OglLib
{
    public static class BefferExtensions
    {
        public static bool SetDataByMapping(this IBuffer buffer, IPclWorkarounds pclWorkarounds, IntPtr data)
        {
            var pMapped = buffer.Map(0, buffer.SizeInBytes, MapAccess.Write | MapAccess.InvalidateBuffer);
            pclWorkarounds.CopyBulk(pMapped, data, buffer.SizeInBytes);
            return buffer.Unmap();
        }

        public static unsafe bool SetDataByMapping(this IBuffer buffer, IPclWorkarounds pclWorkarounds, byte[] data)
        {
            bool result;
            fixed (byte* pData = data)
                result = SetDataByMapping(buffer, pclWorkarounds, (IntPtr)pData);
            return result;
        }

        public static unsafe void CheckData(this IBuffer buffer, byte[] data)
        {
            var pMapped = buffer.Map(0, buffer.SizeInBytes, MapAccess.Read);
            for (int i = 0; i < buffer.SizeInBytes; i++)
                if (data[i] != *(byte*)pMapped + i)
                    throw new InvalidDataException(string.Format("Data differs at position {0}: expected {1}, but was {2}", 
                        i, data[i], *(byte*)pMapped + i));
            buffer.Unmap();
        }

        public static void SetData(this IBuffer buffer, IntPtr data)
        {
            buffer.SetData(0, buffer.SizeInBytes, data);
        }

        public static unsafe void SetData(this IBuffer buffer, byte[] data)
        {
            fixed (byte* pData = data)
                SetData(buffer, (IntPtr)pData);
        }
    }
}