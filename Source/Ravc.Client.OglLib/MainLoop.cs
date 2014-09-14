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

using System.Diagnostics;
using ObjectGL;
using ObjectGL.Api;
using Ravc.Client.OglLib.Pcl;

namespace Ravc.Client.OglLib
{
    public class MainLoop
    {
        private readonly IContext context;
        private readonly IRavcGameWindow gameWindow;
        private readonly IMainThreadBorderStage mainThreadBorderStage;
        private readonly IFinalFrameProvider finalFrameProvider;
        private readonly IClientStatistics statistics;
        private readonly TextureRenderer textureRenderer;
        private readonly IClientStatisticsRenderer statisticsRenderer;
        private readonly CursorRenderer cursorRenderer;
        private readonly Stopwatch stopwatch;

        public MainLoop(IPclWorkarounds pclWorkarounds, IClientStatistics statistics, IClientSettings settings, IContext context, IRavcGameWindow gameWindow, IMainThreadBorderStage mainThreadBorderStage, IFinalFrameProvider finalFrameProvider, IClientStatisticsRenderer statisticsRenderer, ITextureLoader textureLoader)
        {
            this.context = context;
            this.statistics = statistics;
            this.gameWindow = gameWindow;
            this.mainThreadBorderStage = mainThreadBorderStage;
            this.finalFrameProvider = finalFrameProvider;
            this.statisticsRenderer = statisticsRenderer;
            textureRenderer = new TextureRenderer(pclWorkarounds, settings, context);
            cursorRenderer = new CursorRenderer(settings, context, textureLoader);
            stopwatch = new Stopwatch();
        }

        public void OnNewFrame(double elapsedTime)
        {
            //context.ClearWindowColor(new Color4(0.4f, 0.6f, 0.9f, 1.0f));
            context.ClearWindowColor(new Color4(0.0f, 0.0f, 0.0f, 1.0f));

            mainThreadBorderStage.DoMainThreadProcessing();
            var frameToRender = finalFrameProvider.GetFrameToRender();
            textureRenderer.Render(context, gameWindow, frameToRender.MipChainPooled.Item[0]);

            //statisticsRenderer.Render(context);
            cursorRenderer.Draw(context, gameWindow, frameToRender.MipChainPooled.Item[0], frameToRender.Info.MouseX, frameToRender.Info.MouseY);

            statistics.OnFrameRendered();

            stopwatch.Restart();
            context.SwapBuffers();
            stopwatch.Stop();
            statistics.OnSwapChain(stopwatch.Elapsed.TotalMilliseconds);
        }
    }
}