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

using System.Collections.Generic;
using System.Diagnostics;
using ObjectGL.Api;
using ObjectGL.Api.Objects.Resources;
using Ravc.Utility.DataStructures;

namespace Ravc.Client.OglLib
{
    public class TimeBufferingStage : IPipelinedConsumer<GpuSideFrame>, IFinalFrameProvider
    {
        private readonly IClientSettings settings;
        private readonly IClientStatistics statistics;
        private readonly Queue<GpuSideFrame> queue;
        private GpuSideFrame current;
        private double timeOffset;
        private bool initiationComplete;

        public bool IsOverloaded { get { return queue.Count >= settings.QueueCapacity; } }

        public TimeBufferingStage(IClientSettings settings, IClientStatistics statistics, IContext context)
        {
            this.settings = settings;
            this.statistics = statistics;
            queue = new Queue<GpuSideFrame>();
            current = new GpuSideFrame(new FrameInfo(FrameType.Absolute, 0.0f, 0, 64, 64), new TexturePool(context, new TextureInitializer(), false).Extract(64, 64));
        }

        public void Consume(GpuSideFrame input)
        {
            queue.Enqueue(input);
            statistics.OnTimeBufferQueue(queue.Count);
        }

        public ITexture2D GetTextureToRender()
        {
            var localTimestamp = (float)Stopwatch.GetTimestamp() / Stopwatch.Frequency;

            if (!initiationComplete)
            {
                if (queue.Count < settings.TimeBufferInitiationLength)
                    return current.TexturePooled.Item;
                timeOffset = localTimestamp - queue.Peek().Info.Timestamp + settings.TimeOffsetOffset;
                initiationComplete = true;
            }

            if (queue.Count > 0 && IsRunningBehind(localTimestamp, queue.Peek().Info.Timestamp, timeOffset))
            {
                while (queue.Count > 0 && CanMoveNext(localTimestamp, queue.Peek().Info.Timestamp, timeOffset))
                {
                    current.TexturePooled.Release();
                    current = queue.Dequeue();
                    statistics.OnTimeBufferQueue(queue.Count);
                    statistics.OnTimedFrameExtracted();
                }
            }
            else if (queue.Count > 0 && CanMoveNext(localTimestamp, queue.Peek().Info.Timestamp, timeOffset))
            {
                current.TexturePooled.Release();
                current = queue.Dequeue();
                statistics.OnTimeBufferQueue(queue.Count);
                statistics.OnTimedFrameExtracted();
            }

            statistics.OnTimeLag(CalculateTimeLag(localTimestamp, current.Info.Timestamp, timeOffset));

            return current.TexturePooled.Item;
        }

        private static double CalculateTimeLag(double localTimestamp, double frameTimestamp, double timeOffset)
        {
            return localTimestamp - frameTimestamp - timeOffset;
        }

        private static bool IsRunningBehind(double localTimestamp, double frameTimestamp, double timeOffset)
        {
            return CalculateTimeLag(localTimestamp, frameTimestamp, timeOffset) > 0.034;
        }

        private static bool CanMoveNext(double localTimestamp, double frameTimestamp, double timeOffset)
        {
            return CalculateTimeLag(localTimestamp, frameTimestamp, timeOffset) >= 0.0;
        }
    }
}