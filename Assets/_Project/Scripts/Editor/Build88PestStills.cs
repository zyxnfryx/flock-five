#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Play-mode still. Launch without -batchmode and without -nographics.
    // A define-symbol reload during boot drops play mode; the arm file
    // brings it back once the domain is quiet. Batch runs ignore the arm.
    [InitializeOnLoad]
    public static class Build88PestStills
    {
        const string Arm = "/tmp/flock-five-wt-b88-pest-arm";
        const string Trigger = "/tmp/flock-five-wt-b88-pest";

        static Build88PestStills()
        {
            EditorApplication.update += Watch;
        }

        public static void Capture()
        {
            AudioListener.volume = 0f;
            AudioListener.pause = true;
            Directory.CreateDirectory("/Users/zfxgames/wkspaces/wingout");
            Build79Stills.SetGameView(1179, 2556);
            File.WriteAllText(Arm, "1");
        }

        // Keep asking until play mode is actually up. A one-shot delayCall
        // was lost when a define-symbol reload cancelled the first entry.
        static void Watch()
        {
            if (Application.isBatchMode) return;
            if (!File.Exists(Arm)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            AudioListener.volume = 0f;
            AudioListener.pause = true;
            File.WriteAllText(Trigger, "1");
            EditorApplication.isPlaying = true;
        }
    }
}
#endif
