#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // b130 badger-stack play-mode stills. Launch without -batchmode: -executeMethod FlockFive.Editor.BadgerStackStills.Capture
    public static class BadgerStackStills
    {
        public static void Capture()
        {
            AudioListener.volume = 0f;
            AudioListener.pause = true;
            Build79Stills.SetGameView(1170, 2532);
            if (!File.Exists("/tmp/ff-badger-stack-shot")) File.WriteAllText("/tmp/ff-badger-stack-shot", "1");
            EditorApplication.isPlaying = true;
        }
    }
}
#endif
