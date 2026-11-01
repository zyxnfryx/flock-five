#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // PR 221 play-mode stills (spring garden, strike + flame tongues, frozen captions). Launch without -batchmode / -nographics.
    public static class Pr221Stills
    {
        public static void Capture()
        {
            AudioListener.volume = 0f;
            AudioListener.pause = true;
            Build79Stills.SetGameView(1170, 2532);
            File.WriteAllText("/tmp/flock-five-pr221", "1");
            EditorApplication.isPlaying = true;
        }
    }
}
#endif
