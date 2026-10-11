#if UNITY_EDITOR
using System.Diagnostics;
using System.Reflection;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 83 item 8: cost of every splash preload slice, in order (edit mode, -batchmode -quit).
    public static class PreloadBench
    {
        public static void Run()
        {
            var sw = new Stopwatch();
            var sb = new System.Text.StringBuilder("FF_PRELOAD_BENCH");
            void T(string n, System.Action a)
            {
                sw.Restart();
                try { a(); } catch (System.Exception e) { sb.Append(" " + n + "=ERR(" + e.GetType().Name + ")"); return; }
                double ms = sw.Elapsed.TotalMilliseconds;
                sb.Append(" " + n + "=" + ms.ToString("0.0"));
            }
            int k = 0;
            while (!Sfx.WarmDone && k < 200) { T("Sfx" + k, Sfx.WarmStep); k++; }
            var glove = typeof(FlockFiveApp).GetMethod("GloveWarmRows", BindingFlags.Static | BindingFlags.NonPublic);
            k = 0;
            while (!FlockFiveApp.GloveWarmDone && k < 2000) { T("Glove" + k, () => glove.Invoke(null, new object[] { 2 })); k++; }
            k = 0;
            while (!SfxLibrary.WarmDone && k < 200) { T("Lib" + k, SfxLibrary.WarmStep); k++; }
            foreach (var n in new[] { "CoachDimTex", "CoachGlowSprite", "CoachRippleSprite", "HoneycombTex" })
            {
                var m = typeof(FlockFiveApp).GetMethod(n, BindingFlags.Static | BindingFlags.NonPublic);
                T(n, () => m.Invoke(null, null));
            }
            k = 0;
            while (!HoneyArt.WarmDone && k < 50) { T("Honey" + k, () => HoneyArt.WarmStep()); k++; }
            UnityEngine.Debug.Log(sb.ToString());
        }
    }
}
#endif
