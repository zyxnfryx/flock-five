#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Play-mode stills for the popup queue. Launch with graphics, not -batchmode.
    public static class Build89PopupStills
    {
        public static void Capture()
        {
            AudioListener.volume = 0f;
            AudioListener.pause = true;
            Build79Stills.SetGameView(1179, 2556);
            File.WriteAllText("/tmp/flock-five-b89-popup", "1");
            EditorApplication.isPlaying = true;
        }
    }
}
#endif
