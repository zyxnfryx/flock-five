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

                // Style table is unchanged: the two coach spots still resolve through coach.line.
                Check("place line text", CaptionLayout.CoachPlaceLine == "Tap a branch with the same color, or an empty one.", "");
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
