#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Play-mode stills. Launch without -batchmode and without -nographics.
    public static class Build88BannerStills
    {
        public static void Capture()
        {
            AudioListener.volume = 0f;
            AudioListener.pause = true;
            Directory.CreateDirectory("/Users/zfxgames/wkspaces/wingout");
            Build79Stills.SetGameView(1179, 2556);
            File.WriteAllText("/tmp/flock-five-b88-banner", "1");
            EditorApplication.isPlaying = true;
        }
    }
}
#endif
