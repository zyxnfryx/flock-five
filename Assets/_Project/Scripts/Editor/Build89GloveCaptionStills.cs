#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Play-mode stills. Launch without -batchmode and without -nographics.
    public static class Build89GloveCaptionStills
    {
        public static void Capture()
        {
            AudioListener.volume = 0f;
            AudioListener.pause = true;
            Build79Stills.SetGameView(1179, 2556);
            File.WriteAllText("/tmp/flock-five-b89-glove", "1");
            EditorApplication.isPlaying = true;
        }
    }
}
#endif
