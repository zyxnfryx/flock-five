#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 89 Settings row-order stills, one device per launch.
    // Launch without -batchmode and without -nographics.
    public static class Build89SettingsStills
    {
        public static void CaptureIphone() { Go("iphone", 1179, 2556); }
        public static void CaptureIpad() { Go("ipad", 2048, 2732); }
        public static void CaptureAndroid() { Go("android", 1440, 3200); }

        static void Go(string tag, int w, int h)
        {
            AudioListener.volume = 0f;
            AudioListener.pause = true;
            Directory.CreateDirectory("/Users/zfxgames/wkspaces/wingout");
            Build79Stills.SetGameView(w, h);
            File.WriteAllText("/tmp/flock-five-b89-settings-arg", tag + " " + w + " " + h);
            File.WriteAllText("/tmp/flock-five-b89-settings", "1");
            EditorApplication.isPlaying = true;
        }
    }
}
#endif
