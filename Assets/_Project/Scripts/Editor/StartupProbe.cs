#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Batch play-mode measurement of the first seconds after launch.
    // Unity -batchmode -projectPath <wt> -executeMethod FlockFive.Editor.StartupProbe.Run -logFile <log>
    // Does not pass -quit: the probe exits itself once FF_STARTUP is on disk.
    [InitializeOnLoad]
    static class StartupProbe
    {
        const string Flag = "/tmp/ff-startup-probe";
        const string ExitFlag = "/tmp/ff-startup-exit";
        const string Result = "/tmp/ff-startup-result.txt";

        static bool _sawPlay;
        static double _playAt;
        static bool _stopSent;

        static StartupProbe()
        {
            if (File.Exists(ExitFlag))
            {
                try { File.Delete(ExitFlag); } catch { }
                try { File.Delete(Flag); } catch { }
                EditorApplication.delayCall += () => EditorApplication.Exit(0);
                return;
            }
            if (!File.Exists(Flag)) return;
            EditorApplication.update += Tick;
        }

        public static void Run()
        {
            try { if (File.Exists(Result)) File.Delete(Result); } catch { }
            try { if (File.Exists(ExitFlag)) File.Delete(ExitFlag); } catch { }
            ColdBench();
            StartupTrace.ResetWindow();
            File.WriteAllText(Flag, "1");
            EditorApplication.update += Tick;
            EditorApplication.isPaused = false;
            EditorApplication.isPlaying = true;
        }

        static void ColdBench()
        {
            var culture = CultureInfo.InvariantCulture;
            string line = "FF_STARTUP_COLD"
                + " GearIcon.Texture=" + Millis(() => GearIcon.Texture()).ToString("0.0", culture)
                + " StreakBadge.Disc=" + Millis(() => StreakBadge.SharedDisc()).ToString("0.0", culture)
                + " WheelDisc=" + Millis(() => InvokeBake("BakeWheelDisc", 512)).ToString("0.0", culture)
                + " WheelHi=" + Millis(() => InvokeBake("BakeWheelHighlight", 512)).ToString("0.0", culture)
                + " WheelPointer=" + Millis(() => FlockFiveApp.WheelPointerPixels(out _, out _)).ToString("0.0", culture);
            UnityEngine.Debug.Log(line);
        }

        static double Millis(Action act)
        {
            var sw = Stopwatch.StartNew();
            try { act(); }
            catch (Exception e) { UnityEngine.Debug.LogWarning("FF_STARTUP_COLD skipped: " + e.Message); }
            sw.Stop();
            return sw.Elapsed.TotalMilliseconds;
        }

        static void InvokeBake(string name, int n)
        {
            var mi = typeof(FlockFiveApp).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
            if (mi == null) throw new MissingMethodException(name);
            mi.Invoke(null, new object[] { n });
        }

        static void Tick()
        {
            if (EditorApplication.isCompiling) return;
            if (!File.Exists(Flag) && !File.Exists(Result)) return;
            if (!EditorApplication.isPlaying)
            {
                // Domain reload is off, so this same Tick has to quit after play stops.
                if (_stopSent)
                {
                    if (!EditorApplication.isPlayingOrWillChangePlaymode)
                        EditorApplication.Exit(File.Exists(Result) ? 0 : 1);
                    return;
                }
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    EditorApplication.isPlaying = true;
                return;
            }
            EditorApplication.isPaused = false;
            if (!_sawPlay)
            {
                _sawPlay = true;
                _playAt = EditorApplication.timeSinceStartup;
            }
            Pump();
            bool done = File.Exists(Result);
            bool timedOut = EditorApplication.timeSinceStartup - _playAt > 22.0;
            if (done)
            {
                EditorApplication.Exit(0);
                return;
            }
            if (!timedOut) return;
            if (!_stopSent)
                UnityEngine.Debug.LogWarning("FF_STARTUP probe timed out before the summary line");
            _stopSent = true;
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(1);
        }

        static void Pump()
        {
            var asm = typeof(EditorWindow).Assembly;
            var gvType = asm.GetType("UnityEditor.GameView");
            if (gvType == null) return;
            EditorWindow gv = null;
            var all = Resources.FindObjectsOfTypeAll(gvType);
            if (all != null && all.Length > 0) gv = all[0] as EditorWindow;
            if (gv == null) gv = EditorWindow.GetWindow(gvType, false, "Game", false);
            if (gv == null) return;
            gv.Repaint();
        }
    }
}
#endif
