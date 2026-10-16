#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b112: the studio card / loading cover swallows all input through the shared gate.
    public static class Build112Tests
    {
        const string Out = "/tmp/flock-five-b112-tests.txt";
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            void Check(string name, bool ok, string detail)
            {
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build112] " + (ok ? "PASS " : "FAIL ") + name + " " + detail);
            }
            string app = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs");
            string pre = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Preload.cs");
            Check("startup hold tied to the cover", pre.Contains("StartupInputHeld => WarmCoverLive"), "");
            int sw = app.IndexOf("static void SwallowResumePointer()", System.StringComparison.Ordinal);
            int pr = app.IndexOf("static bool Pressed(out Vector2 screen)", System.StringComparison.Ordinal);
            Check("pointer swallow honors the hold", sw > 0 && app.IndexOf("StartupInputHeld", sw, 400, System.StringComparison.Ordinal) > 0, "");
            Check("Pressed refuses while held", pr > 0 && app.IndexOf("if (StartupInputHeld) return false;", pr, 400, System.StringComparison.Ordinal) > 0, "");
            Check("hold is on while studio card up", FlockFiveApp.StudioCardHold ? FlockFiveApp.StartupInputHeld : true, "");
            string sp = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.SplashPress.cs");
            string st = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Settings.cs");
            int plb = st.IndexOf("bool PressLevelButton()", System.StringComparison.Ordinal);
            Check("PressLevelButton refuses while held", plb > 0 && st.IndexOf("if (StartupInputHeld) return false;", plb, 200, System.StringComparison.Ordinal) > 0, "");
            Check("home gates carry the hold", sp.Contains("_resumeInputUntil || StartupInputHeld"), "");
            int shp = sp.IndexOf("SplashPress.View StepHomePress", System.StringComparison.Ordinal);
            Check("StepHomePress denies while held", shp > 0 && sp.IndexOf("if (StartupInputHeld) allow = false;", shp, 500, System.StringComparison.Ordinal) > 0, "");
            Check("daily gate override keeps the hold", app.Contains("dailyGates.ResumeSwallow = StartupInputHeld;"), "");
            string imp = File.ReadAllText("Assets/_Project/Scripts/Editor/SpriteImport.cs");
            int sq = imp.IndexOf("\"/Audio/Squish/\"", System.StringComparison.Ordinal);
            Check("importer keeps squish PCM", sq > 0 && imp.IndexOf("AudioCompressionFormat.PCM", sq, 300, System.StringComparison.Ordinal) > 0, "");
            bool metas = true;
            foreach (var f in Directory.GetFiles("Assets/_Project/Art/Resources/Audio/Squish", "*.wav.meta"))
            {
                string m = File.ReadAllText(f);
                metas &= m.Contains("compressionFormat: 0") && m.Contains("loadType: 0") && m.Contains("forceToMono: 1") && m.Contains("normalize: 0");
            }
            Check("squish metas PCM decompress-on-load", metas, "");
        }
    }
}
#endif
