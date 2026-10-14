#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Play-mode clip. Launch with graphics, never -batchmode and never -nographics.
    public static class LightningFireShot
    {
        public static void Capture()
        {
            AudioListener.volume = 0f;
            AudioListener.pause = true;
            Directory.CreateDirectory("/Users/zfxgames/wkspaces/post88");
            Build79Stills.SetGameView(1179, 2556);
            File.WriteAllText("/tmp/flock-five-lightning-fire", "1");
            EditorApplication.isPlaying = true;
        }
    }
}
#endif
