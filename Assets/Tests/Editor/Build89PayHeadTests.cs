#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 89: pay table header bands (PayHeadLayout). Per device, from the header band
    // DrawPokerPayTable reserves: title over bet over rule, inside the band, the title
    // band the larger, the bet capped under the title. Rows start at the same y as before
    // (headH unchanged), so they are never squeezed. Pure math: no GUI.skin needed.
    [InitializeOnLoad]
    static class Build89PayHeadTests
    {
        const string Cmd = "/tmp/flock-five-b89-payhead-tests";
        const string Out = "/tmp/flock-five-b89-payhead-tests.txt";

        static Build89PayHeadTests() { EditorApplication.update += Tick; }

        static void Tick()
        {
            if (!File.Exists(Cmd) || EditorApplication.isCompiling) return;
            try { File.Delete(Cmd); } catch { return; }
            Run();
        }

        [MenuItem("Flock Five/Build 89 Pay Head Tests")]
        public static void Run()
        {
            var sb = new StringBuilder();
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string info = "")
            {
                if (ok) pass++; else fail++;
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + (info.Length > 0 ? "  (" + info + ")" : ""));
            }

            // iPhone, iPad, Android, each with its own paper width and header band.
            var devs = new[]
            {
                new { tag = "iphone", w = 1179f, h = 2556f },
                new { tag = "ipad", w = 1536f, h = 2048f },
                new { tag = "android", w = 1080f, h = 2400f },
            };
            foreach (var d in devs)
            {
                float s = DeviceFit.Ui(Mathf.RoundToInt(d.w), Mathf.RoundToInt(d.h));
                float panelW = d.w * 0.94f;
                var paper = new Rect(d.w * 0.03f + 7f * s, 600f, panelW - 14f * s, 1200f);
                foreach (float headH in new[] { 34f * s, 52f * s, 78f * s })
                {
                    string k = d.tag + " headH " + headH.ToString("0");
                    var b = PayHeadLayout.Layout(paper, headH, 2f * s);
                    Check(k + " title in band", b.Title.yMin >= paper.y && b.Title.xMin >= paper.x && b.Title.xMax <= paper.xMax);
                    Check(k + " title over bet", b.Title.yMax <= b.Bet.yMin + 0.01f);
                    Check(k + " bet above rule", b.Bet.yMax <= b.RuleY + 0.01f);
                    Check(k + " rule at header end", Mathf.Abs(b.RuleY - (paper.y + headH - 2f * s)) < 0.01f);
                    Check(k + " title band larger", b.Title.height > b.Bet.height);
                    Check(k + " fit boxes inside bands", b.TitleMaxH <= b.Title.height && b.BetMaxH <= b.Bet.height
                        && b.TitleMaxW <= b.Title.width && b.BetMaxW <= b.Bet.width);
                    Check(k + " header uses most of band", b.Title.height + b.Bet.height >= headH - 4f * s - 0.01f);
                }
            }
            foreach (int t in new[] { 24, 40, 64, 120 })
                Check("bet cap under title " + t, PayHeadLayout.BetCap(t) < t && PayHeadLayout.BetCap(t) >= PayHeadLayout.BetMinPx,
                    "cap " + PayHeadLayout.BetCap(t));

            sb.Insert(0, "Build89PayHeadTests pass " + pass + " fail " + fail + "\n");
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[b89-payhead-tests] pass " + pass + " fail " + fail);
        }
    }
}
#endif
