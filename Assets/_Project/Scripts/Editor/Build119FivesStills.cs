#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // b119 Five of a Kind board stills. Launch without -batchmode:
    // -executeMethod FlockFive.Editor.Build119FivesStills.Capture  (output dir in /tmp/flock-five-b119-fives)
    public static class Build119FivesStills
    {
        public static void Capture()
        {
            AudioListener.volume = 0f;
            AudioListener.pause = true;
            Build79Stills.SetGameView(1206, 2622);
            if (!File.Exists("/tmp/flock-five-b119-fives")) File.WriteAllText("/tmp/flock-five-b119-fives", "/tmp/ff-b119-fives");
            EditorApplication.isPlaying = true;
        }
    }
}
#endif
