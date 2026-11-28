#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FlockFive.Editor
{
    // Every coach tip, on the four portrait classes, is one rounded pill:
    // class height, radius, bottom band, and text cap. Copy stays within two lines.
    public static class TipPillTests
    {
        public static void Run()
        {
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) Debug.Log("[tip-pill] PASS " + name);
                else
                {
                    fail++;
                    Debug.LogError("[tip-pill] FAIL " + name + " " + detail);
                }
            }

            Check("height-360", TipPill.Height(780f) == 104, TipPill.Height(780f).ToString());
            Check("height-402", TipPill.Height(874f) == 114, TipPill.Height(874f).ToString());
            Check("height-412", TipPill.Height(915f) == 120, TipPill.Height(915f).ToString());
            Check("height-820", TipPill.Height(1180f) == 160, TipPill.Height(1180f).ToString());
            Check("cap-360", TipPill.Cap(780f) == 22, TipPill.Cap(780f).ToString());
            Check("cap-402", TipPill.Cap(874f) == 24, TipPill.Cap(874f).ToString());
            Check("cap-412", TipPill.Cap(915f) == 26, TipPill.Cap(915f).ToString());
            Check("cap-820", TipPill.Cap(1180f) == 34, TipPill.Cap(1180f).ToString());
            Check("radius-402", Mathf.Abs(TipPill.Radius(874f) - 18f) < 0.05f, TipPill.Radius(874f).ToString("0.00"));
            Check("gift-uses-floor-band", TipPill.BandFor("ad.hand") == TipPill.Band.Floor
                && TipPill.BandFor("coach.line") == TipPill.Band.Floor, "");

            var tips = new List<string>();
            var seen = new HashSet<string>();
            var steps = CoachTap.All;
            for (int i = 0; i < steps.Length; i++)
            {
                string line = CoachTap.Caption(steps[i]);
                if (string.IsNullOrEmpty(line) || !seen.Add(line)) continue;
                tips.Add(line);
            }
            if (seen.Add(CaptionLayout.YoursLine)) tips.Add(CaptionLayout.YoursLine);
            if (seen.Add(FlockFiveApp.DailyAskCopy)) tips.Add(FlockFiveApp.DailyAskCopy);
            Check("tip-count", tips.Count >= 30, tips.Count.ToString());

            var style = FlockFiveApp.CoachCaptionStyle();
            var windows = new[]
            {
                new Vector2Int(360, 780),
                new Vector2Int(402, 874),
                new Vector2Int(412, 915),
                new Vector2Int(820, 1180),
            };
            var bad = new StringBuilder();
            for (int d = 0; d < windows.Length; d++)
            {
                int winW = windows[d].x;
                int winH = windows[d].y;
                PhoneArea.PlayPixels(winW, winH, out int fieldW, out int fieldH);
                int classH = TipPill.Height(fieldH);
                int classCap = TipPill.Cap(fieldH);
                float rad = TipPill.Radius(fieldH);
                float gap = TipPill.Gap(fieldH);
                // 820 is also checked with the 59/34 safe insets. Phones use a flush safe rect.
                int safes = winW == 820 ? 2 : 1;
                for (int s = 0; s < safes; s++)
                {
                    float safeBot = s == 0 ? 0f : 34f;
                    float safeTop = s == 0 ? 0f : 59f;
                    var safe = new Rect(0f, safeBot, fieldW, Mathf.Max(2f, fieldH - safeBot - safeTop));
                    var floor = TipPill.Place(fieldW, fieldH, safeBot, TipPill.Band.Floor);
                    var raised = TipPill.Place(fieldW, fieldH, safeBot, TipPill.Band.Raised);
                    var framed = FlockFiveApp.TipFrame(fieldW, fieldH, safe, "coach.line");
                    bool raisedOk = Mathf.Abs((floor.yMax - raised.yMax) - classH) < 1.5f;
                    bool frameOk = Mathf.Abs(framed.x - floor.x) < 0.5f && Mathf.Abs(framed.y - floor.y) < 0.5f
                        && Mathf.Abs(framed.width - floor.width) < 0.5f && Mathf.Abs(framed.height - floor.height) < 0.5f;
                    if (!raisedOk || !frameOk)
                        bad.Append(winW).Append(s == 0 ? " floor" : " safe").Append(" raised/frame; ");
                    for (int t = 0; t < tips.Count; t++)
                    {
                        string text = tips[t];
                        var pill = TipPill.Place(fieldW, fieldH, safeBot, TipPill.BandFor("coach.line"));
                        float inner = TipPill.InnerWidth(pill, classCap);
                        string shown = TipPill.Shown(style, text, inner, classCap);
                        int lines = TipPill.CountLines(shown);
                        bool height = Mathf.Abs(pill.height - classH) < 0.5f;
                        bool round = rad > 0.5f;
                        bool inset = pill.xMin >= 8f && pill.xMax <= fieldW - 8f;
                        float wantBottom = fieldH - safeBot - gap;
                        bool band = Mathf.Abs(pill.yMax - wantBottom) < 1.5f;
                        bool cap = classCap == TipPill.Cap(fieldH);
                        bool two = lines >= 1 && lines <= TipPill.MaxLines;
                        bool fits = CaptionLayout.ShownFits(style, shown, inner, classCap);
                        if (height && round && inset && band && cap && two && fits) continue;
                        bad.Append(winW).Append(s == 0 ? "" : "+safe").Append(" [")
                            .Append(text.Replace("\n", " / ")).Append("] h=").Append(pill.height.ToString("0"))
                            .Append(" r=").Append(rad.ToString("0.0"))
                            .Append(" yMax=").Append(pill.yMax.ToString("0"))
                            .Append("/").Append(wantBottom.ToString("0"))
                            .Append(" cap=").Append(classCap)
                            .Append(" lines=").Append(lines)
                            .Append(fits ? "" : " overflow")
                            .Append("; ");
                    }
                }
            }
            Check("every-tip", bad.Length == 0, bad.ToString());
            if (fail > 0) throw new System.Exception("tip-pill " + fail);
        }
    }
}
#endif
