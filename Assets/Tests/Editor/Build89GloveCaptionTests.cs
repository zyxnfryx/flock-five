#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b89: the glove paints after the caption. The shared caption helper records
    // its words, and DrawGloveAt stamps them again when the glove touches the plate.
    public static class Build89GloveCaptionTests
    {
        const string Out = "/tmp/flock-five-b89-glove-tests.txt";

        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail)
            {
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
                if (ok) pass++; else fail++;
                File.WriteAllText(Out, sb.ToString());
            }
            try
            {
                var band = new Rect(0f, 1200f, 1179f, 260f);
                Check("gap-cut-only-on-blank-rows",
                    CaptionLayout.GapCut(new GUIStyle(), "One.\nTwo.") == 0f && CaptionLayout.GapCut(new GUIStyle(), null) == 0f
                    && CaptionLayout.HeadlineGapKeep > 0f && CaptionLayout.HeadlineGapKeep < 1f,
                    CaptionLayout.HeadlineGapKeep.ToString());
                Check("glove over band", CaptionLayout.GloveHitsCaption(new Rect(700f, 1350f, 220f, 260f), band), "");
                Check("glove clear of band", !CaptionLayout.GloveHitsCaption(new Rect(700f, 1500f, 220f, 260f), band), "");
                Check("empty glove", !CaptionLayout.GloveHitsCaption(default, band), "");
                Check("empty caption", !CaptionLayout.GloveHitsCaption(new Rect(0f, 0f, 50f, 50f), default), "");

                string src = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Coach.cs");
                int paint = src.IndexOf("void PaintCoachCaption(");
                int end = src.IndexOf("static void DrawCaptionBee", paint);
                string body = paint >= 0 && end > paint ? src.Substring(paint, end - paint) : "";
                int stamps = Count(body, "StampBannerText(");
                int keeps = Count(body, "KeepCaptionOnTop(");
                Check("every caption paint is recorded", stamps > 0 && stamps == keeps, "stamps " + stamps + " keeps " + keeps);
                int glove = src.IndexOf("void DrawGloveAt(");
                int gEnd = src.IndexOf("public const float GloveScreenMargin", glove);
                string g = glove >= 0 && gEnd > glove ? src.Substring(glove, gEnd - glove) : "";
                Check("glove draw restamps caption", g.IndexOf("RepaintCaptionOver(") > g.IndexOf("GUI.DrawTexture("), "");

                // Padding: equal above and below, a fixed share of the font on every device.
                float phone = CaptionLayout.BannerBandHeight(100f, 40) - 100f;
                float pad = CaptionLayout.BannerBandHeight(100f, 60) - 100f;
                Check("band pad scales with font", Mathf.Abs(pad / 60f - phone / 40f) < 0.001f, phone + " " + pad);
                var plate = new Rect(0f, 400f, 1179f, 300f);
                var below = CaptionLayout.GloveKeepRect(plate, new Vector2(800f, 900f), 1179f, 2556f);
                Check("glove clip below plate", Mathf.Approximately(below.yMin, 700f) && Mathf.Approximately(below.yMax, 2556f), below.ToString());
                var above = CaptionLayout.GloveKeepRect(plate, new Vector2(800f, 200f), 1179f, 2556f);
                Check("glove clip above plate", Mathf.Approximately(above.yMax, 400f), above.ToString());
                Check("glove draw clips at plate", g.IndexOf("DrawGloveClipped(") >= 0 && g.IndexOf("DrawGloveClipped(") < g.IndexOf("GUI.DrawTexture("), "");
                // b92: the clip and the word restamp are switched off, so the glove is never under a caption.
                Check("b92 glove over captions", CaptionLayout.GloveOverCaptions, "");
                int clipFn = src.IndexOf("bool DrawGloveClipped(");
                Check("b92 clip gated off", clipFn > 0 && src.IndexOf("if (CaptionLayout.GloveOverCaptions) return false;", clipFn) > clipFn
                    && src.IndexOf("if (CaptionLayout.GloveOverCaptions) return;", src.IndexOf("void RepaintCaptionOver(")) > 0, "");
                int cnt = 0, at = 0; while ((at = src.IndexOf("DrawGloveAt(", at)) >= 0) { cnt++; at += 12; }
                Check("b92 one glove draw path", cnt >= 1 && src.IndexOf("GUI.DrawTexture(rect, tex", glove) > glove, "");
                var quad = new[] { new Vector3(0f, 600f, 0f), new Vector3(100f, 600f, 0f), new Vector3(100f, 800f, 0f), new Vector3(0f, 800f, 0f) };
                var uvs = new[] { new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(0f, 0f) };
                var cut = CaptionLayout.ClipQuadY(quad, uvs, 700f, 2556f);
                float minY = 9999f; foreach (var v in cut) minY = Mathf.Min(minY, v.y);
                Check("quad cut at plate edge", cut.Count == 4 && Mathf.Approximately(minY, 700f), cut.Count + " " + minY);
                Check("banner lifts over glove body", src.IndexOf("GloveBodyFresh();") > 0, "");

                // b92: every shared tutorial caption is at most two authored rows, and each row
                // is short enough (about 21 chars) to hold one line at iPhone and Android width.
                string[] shared = { CaptionLayout.HiveGardenLine, CaptionLayout.LeafLine, CaptionLayout.SparrowLine,
                    CaptionLayout.HawkLine, CaptionLayout.OstrichLine, CaptionLayout.BeePestLine,
                    CaptionLayout.CoachPlaceLine, CaptionLayout.CoachNowhereLine };
                foreach (var line in shared)
                {
                    var rows = line.Split('\n');
                    bool ok = rows.Length <= 2;
                    foreach (var r in rows) if (r.Length > 24 && rows.Length > 1) ok = false;
                    if (rows.Length == 1 && line.Length > 36) ok = false;
                    Check("b92 two lines: " + line.Replace("\n", " / "), ok, "rows " + rows.Length);
                }
                Check("b92 banner clears top row", src.IndexOf("BoardTopGui(out float rowTop)") > 0, "");

                // Style table is unchanged: the two coach spots still resolve through coach.line.
                Check("place line text", CaptionLayout.CoachPlaceLine == "Tap a matching or empty branch.", "");
            }
            catch (System.Exception ex)
            {
                Check("glove caption", false, ex.GetType().Name + " " + ex.Message);
            }
            sb.AppendLine("pass " + pass + " fail " + fail);
            File.WriteAllText(Out, sb.ToString());
        }

        static int Count(string s, string needle)
        {
            int n = 0, i = 0;
            while ((i = s.IndexOf(needle, i)) >= 0) { n++; i += needle.Length; }
            return n;
        }
    }
}
#endif
