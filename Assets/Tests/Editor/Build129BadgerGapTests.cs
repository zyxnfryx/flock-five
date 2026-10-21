#if UNITY_EDITOR
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // b129 v4 (Brandon): the badger keeps one shared gap from the honeycomb he picked. At 375x667, 390x844,
    // 430x932 and iPad, normal and frozen: the fighter rect (ice block rect when frozen) clears the pick-comb hex
    // by at least the shared gap, and neither the fighter nor the ice block touches the right HUD column.
    // Rects come from the same layout + placement the fight page draws with (BadgerLayoutFor, BadgerPickComb).
    public static class Build129BadgerGapTests
    {
        const string Out = "/tmp/flock-five-b129-tests.txt";

        struct Dev { public string Name; public float W, H, Top, Bottom; }

        [MenuItem("Flock Five/Build 129 Badger Gap Tests")]
        public static void Run()
        {
            var sb = new StringBuilder();
            int pass = 0, fail = 0;
            var ci = CultureInfo.InvariantCulture;
            void Check(string name, bool ok, string detail)
            {
                string line = (ok ? "PASS  " : "FAIL  ") + name + "  " + detail;
                if (ok) pass++; else fail++;
                sb.AppendLine(line);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build129] " + line);
            }

            // Pixel screens with their safe-area insets (top, bottom), as the device reports them.
            var devs = new[]
            {
                new Dev { Name = "375x667", W = 750f, H = 1334f, Top = 40f, Bottom = 0f },
                new Dev { Name = "390x844", W = 1170f, H = 2532f, Top = 141f, Bottom = 102f },
                new Dev { Name = "430x932", W = 1290f, H = 2796f, Top = 177f, Bottom = 102f },
                new Dev { Name = "ipad1024x1366", W = 2048f, H = 2732f, Top = 48f, Bottom = 40f },
            };
            float? keepWin = DeviceFit.TestWindow;
            try
            {
                foreach (var d in devs)
                {
                    DeviceFit.TestWindow = d.W / d.H;
                    float s = DeviceFit.Ui(d.W, d.H);
                    var safe = new Rect(0f, d.Bottom, d.W, d.H - d.Top - d.Bottom);
                    float top = d.Top + 8f * s;
                    var back = new Rect(Mathf.Max(16f, safe.xMin + 10f), top, 101.2f * Mathf.Clamp(s, 1f, 1.35f), 101.2f * Mathf.Clamp(s, 1f, 1.35f));
                    var L = FlockFiveApp.BadgerLayoutFor(d.W, d.H, safe, s, top, back);
                    var g = FlockFiveApp.BadgerPickComb(L.Arena, L.ColR, s);
                    float pt = d.Name.StartsWith("ipad") ? 1024f / d.W : (d.Name == "375x667" ? 375f / d.W : (d.Name == "390x844" ? 390f / d.W : 430f / d.W));
                    foreach (bool frozen in new[] { false, true })
                    {
                        var f = frozen ? g.Ice : g.Fighter;
                        string tag = d.Name + (frozen ? ".frozen" : ".normal");
                        float gap = f.xMin - g.Comb.xMax;
                        bool vOverlap = f.yMin < g.Comb.yMax && f.yMax > g.Comb.yMin;
                        Check("comb-gap." + tag, gap >= g.Gap - 0.01f,
                            "gap " + gap.ToString("0.0", ci) + "px (" + (gap * pt).ToString("0.0", ci) + "pt) need " + g.Gap.ToString("0.0", ci)
                            + "px (" + (g.Gap * pt).ToString("0.0", ci) + "pt) fighter " + R(f) + " comb " + R(g.Comb) + " vOverlap " + vOverlap);
                        bool hit = f.xMax > L.ColR.xMin && f.xMin < L.ColR.xMax && f.yMax > L.ColR.yMin && f.yMin < L.ColR.yMax;
                        Check("hud-clear." + tag, !hit, "fighter.xMax " + f.xMax.ToString("0.0", ci) + " colR.x " + L.ColR.xMin.ToString("0.0", ci));
                        Check("comb-in-arena." + tag, g.Comb.xMin >= L.Arena.xMin - 0.5f, "comb " + R(g.Comb) + " arena " + R(L.Arena));
                    }
                    Check("one-shared-gap." + d.Name, Mathf.Abs(g.Gap - BadgerFighterPlace.CombGap(s)) < 0.001f, "gap " + g.Gap.ToString("0.00", ci));
                }
                DeviceFit.TestWindow = 1170f / 2532f;
                Check("gap-12px-at-390x844", Mathf.Abs(BadgerFighterPlace.CombGap(DeviceFit.Ui(1170f, 2532f)) * 390f / 1170f - 12f) < 0.05f,
                    "pt " + (BadgerFighterPlace.CombGap(DeviceFit.Ui(1170f, 2532f)) * 390f / 1170f).ToString("0.00", ci));
            }
            finally { DeviceFit.TestWindow = keepWin; }
            sb.AppendLine("pass " + pass + " fail " + fail);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[build129] pass " + pass + " fail " + fail);
        }

        static string R(Rect r) => string.Format(CultureInfo.InvariantCulture, "({0:0.0},{1:0.0},{2:0.0},{3:0.0})", r.x, r.y, r.width, r.height);
    }
}
#endif
