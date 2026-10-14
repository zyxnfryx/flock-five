#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Play-mode stills of the home name picker. Launch without -batchmode / -nographics.
    public static class NamePickerStills
    {
        public static void Capture()
        {
            AudioListener.volume = 0f;
            AudioListener.pause = true;
            Build79Stills.SetGameView(1080, 2400);
            File.WriteAllText("/tmp/flock-five-namepick", "1");
            EditorApplication.isPlaying = true;
        }
    }
}
#endif
