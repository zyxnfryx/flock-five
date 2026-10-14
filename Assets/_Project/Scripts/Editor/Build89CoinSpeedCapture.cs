#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Wheel Collect sprinkle timing clip. Launch without -batchmode / -nographics:
    // Unity -projectPath <wt> -executeMethod FlockFive.Editor.Build89CoinSpeedCapture.Capture
    // Output dir comes from /tmp/flock-five-b89-coinspeed-arg (frames, clip.mp4, timing.txt).
    [InitializeOnLoad]
    public static class Build89CoinSpeedCapture
    {
        const string PumpFlag = "/tmp/flock-five-b89-coinspeed-pump";
        static bool _started;

        static Build89CoinSpeedCapture() { EditorApplication.update += Pump; }

        public static void Capture()
        {
            AudioListener.volume = 0f;
            AudioListener.pause = true;
            Build79Stills.SetGameView(1179, 2556);
            EditorPrefs.SetInt("ScriptCompilationDuringPlay", 1);
            AssetDatabase.DisallowAutoRefresh();
            File.WriteAllText(PumpFlag, Application.dataPath);
            EditorApplication.isPlaying = true;
        }

        static void Pump()
        {
            // Armed per project, so another editor never picks up this capture.
            if (!File.Exists(PumpFlag) || File.ReadAllText(PumpFlag) != Application.dataPath) return;
            if (!EditorApplication.isPlaying) return;
            EditorApplication.QueuePlayerLoopUpdate();
            var gv = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (gv != null)
                foreach (var w in Resources.FindObjectsOfTypeAll(gv)) (w as EditorWindow)?.Repaint();
            if (_started) return;
            var app = Object.FindAnyObjectByType<FlockFive.FlockFiveApp>();
            if (app == null) return;
            _started = true;
            try { File.Delete(PumpFlag); } catch { }
            app.StartCoroutine(app.ShotWheelCoinSpeed());
        }
    }
}
#endif
