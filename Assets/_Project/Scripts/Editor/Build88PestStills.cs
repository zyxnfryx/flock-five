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
        static bool _watching;

        static Build88PestStills()
        {
            if (!Application.isBatchMode && File.Exists(Arm))
                ArmWatch();
        }

        public static void Capture()
        {
            AudioListener.volume = 0f;
            AudioListener.pause = true;
            Directory.CreateDirectory("/Users/zfxgames/wkspaces/wingout");
            Build79Stills.SetGameView(1179, 2556);
            File.WriteAllText(Arm, "1");
            ArmWatch();
        }

        static void ArmWatch()
        {
            if (_watching) return;
            _watching = true;
            EditorApplication.update += Watch;
        }

        // Keep asking until play mode is actually up. Drop the hook once the
        // arm file is gone so a normal editor session does not stat it forever.
        static void Watch()
        {
            if (Application.isBatchMode || !File.Exists(Arm))
            {
                EditorApplication.update -= Watch;
                _watching = false;
                return;
            }
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
