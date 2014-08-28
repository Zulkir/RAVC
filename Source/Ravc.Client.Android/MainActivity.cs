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
using Android.App;
using Android.OS;
using Android.Content.PM;
using Android.Util;

namespace Ravc.Client.Android
{
    [Activity(Label = "Ravc.Client.Android",
        MainLauncher = true,
        Icon = "@drawable/icon",
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.KeyboardHidden,
        Theme = "@android:style/Theme.Black.NoTitleBar.Fullscreen"
#if __ANDROID_11__
		,HardwareAccelerated=false
#endif
)]
    public class MainActivity : Activity
    {
        GLView1 view;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            if (SupportedOpenGLVersion() < 3)
                throw new NotSupportedException("OpenGL ES 3.0 support is required");

            view = new GLView1(this);
            SetContentView(view);
        }

        /// <summary>
        /// Gets the supported OpenGL ES version of device.
        /// </summary>
        /// <returns>Hieghest supported version of OpenGL ES</returns>
        private long SupportedOpenGLVersion()
        {
            //from https://android.googlesource.com/platform/cts/+/master/tests/tests/graphics/src/android/opengl/cts/OpenGlEsVersionTest.java
            var featureInfos = PackageManager.GetSystemAvailableFeatures();
            if (featureInfos != null && featureInfos.Length > 0)
                foreach (FeatureInfo info in featureInfos)
                    if (IsGLVersionInfo(info))
                        return info.ReqGlEsVersion != FeatureInfo.GlEsVersionUndefined ? GetMajorVersion(info.ReqGlEsVersion) : 0L;
            return 0L;
        }

        private static bool IsGLVersionInfo(FeatureInfo info)
        {
            // Null feature name means this feature is the open gl es version feature.
            return info.Name == null;
        }

        private static long GetMajorVersion(long raw)
        {
            //from https://android.googlesource.com/platform/cts/+/master/tests/tests/graphics/src/android/opengl/cts/OpenGlEsVersionTest.java
            long cleaned = ((raw & 0xffff0000) >> 16);
            Log.Info("GLVersion", "OpenGL ES major version: " + cleaned);
            return cleaned;
        }

        protected override void OnPause()
        {
            base.OnPause();
            view.StopReceiving();
            view.Dispose();
        }

        protected override void OnResume()
        {
            base.OnResume();
            view = new GLView1(this);
            SetContentView(view);
        }
    }
}

