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

namespace Ravc.WinformsOglClient
{
    public class MainLoop
    {
        private readonly IContext context;
        private readonly MyGameWindow gameWindow;
        private readonly IMainThreadBorderStage mainThreadBorderStage;
        private readonly IFinalFrameProvider finalFrameProvider;
        private readonly IClientStatistics statistics;
        private readonly TextureRenderer textureRenderer;
        private readonly Stopwatch stopwatch;

        public MainLoop(IContext context, MyGameWindow gameWindow, IMainThreadBorderStage mainThreadBorderStage, IFinalFrameProvider finalFrameProvider, IClientStatistics statistics)
        {
            this.context = context;
            this.gameWindow = gameWindow;
            this.mainThreadBorderStage = mainThreadBorderStage;
            this.finalFrameProvider = finalFrameProvider;
            this.statistics = statistics;
            textureRenderer = new TextureRenderer(context);
            stopwatch = new Stopwatch();
        }

        public void OnNewFrame(double elapsedTime, double totalRealTime)
        {
            context.ClearWindowColor(new Color4(0.4f, 0.6f, 0.9f, 1.0f));

            mainThreadBorderStage.DoMainThreadProcessing();
            var textureToRender = finalFrameProvider.GetTextureToRender(totalRealTime);
            textureRenderer.Render(context, textureToRender, gameWindow.ClientSize.Width, gameWindow.ClientSize.Height);

            statistics.OnFrameRendered(elapsedTime);

            stopwatch.Restart();
            context.SwapBuffers();
            stopwatch.Stop();
            statistics.OnSwapChain(stopwatch.Elapsed.TotalMilliseconds);
        }
    }
}