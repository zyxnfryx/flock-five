#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // Level button curtain. PressRainbow: burst, apex, fall, reveal. The wash is a
    // translucent veil, never an opaque glint sheet. About one second.
    // SparkleFx still carries the old tear helpers; the live path does not use them.
    public static class Build88CurtainTests
    {
        const string Out = "/tmp/flock-five-b88-curtain-tests.txt";
        const float W = 1179f;
        const float H = 2556f;

        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail)
            {
                string line = (ok ? "PASS  " : "FAIL  ") + name + "  " + detail;
                if (ok) pass++; else fail++;
                sb.AppendLine(line);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build88curtain] " + line);
            }
            try
            {
                var src = new Rect(390f, 1800f, 400f, 400f);
                var buf = new LevelCurtain.Glint[LevelCurtain.Count];
                Check("phases",
                    LevelCurtain.BeatName(0.05f) == "burst"
                    && LevelCurtain.BeatName(0.20f) == "burst"
                    && LevelCurtain.BeatName(LevelCurtain.Flood) == "apex"
                    && LevelCurtain.BeatName(0.55f) == "fall"
                    && LevelCurtain.BeatName(0.90f) == "reveal",
                    LevelCurtain.BeatName(0.05f) + "/" + LevelCurtain.BeatName(LevelCurtain.Flood) + "/" + LevelCurtain.BeatName(0.90f));
                bool order = LevelCurtain.BuildEnd < LevelCurtain.Flood
                    && LevelCurtain.Flood < LevelCurtain.WashEnd
                    && LevelCurtain.WashEnd < LevelCurtain.Duration;
                Check("phase-order", order,
                    "build " + LevelCurtain.BuildEnd.ToString("0.00")
                    + " flood " + LevelCurtain.Flood.ToString("0.00")
                    + " wash " + LevelCurtain.WashEnd.ToString("0.00")
                    + " dur " + LevelCurtain.Duration.ToString("0.00"));
                Check("about-one-second",
                    LevelCurtain.Duration >= 0.90f && LevelCurtain.Duration <= 1.10f,
                    "dur " + LevelCurtain.Duration.ToString("0.00"));
                // Duration still comes from SparkleFx. Beat names are the fountain's
                // (burst/apex/fall/reveal); CurtainPhase is the retired tear.
                Check("helper-owns-phase",
                    LevelCurtain.BeatName(LevelCurtain.Flood) == "apex"
                    && Mathf.Abs(LevelCurtain.Duration - SparkleFx.CurtainDuration) < 0.001f,
                    "shared duration, apex at flood");
                // Shots pin PreviewNow. Clock does not latch on captureDeltaTime.
                float keptPreview = LevelCurtain.PreviewNow;
                LevelCurtain.PreviewNow = 3f;
                float locked = LevelCurtain.Clock(9f);
                float again = LevelCurtain.Clock(0.2f);
                LevelCurtain.PreviewNow = -1f;
                float live = LevelCurtain.Clock(4f);
                LevelCurtain.PreviewNow = keptPreview;
                Check("capture-clock-holds-the-frame",
                    Mathf.Abs(locked - 3f) < 0.001f && Mathf.Abs(again - 3f) < 0.001f
                    && Mathf.Abs(live - 4f) < 0.001f,
                    "pinned " + locked.ToString("0.000") + "/" + again.ToString("0.000")
                    + " live " + live.ToString("0.000"));

                float shut = SparkleFx.CurtainOpen(LevelCurtain.WashEnd);
                float mid = SparkleFx.CurtainOpen(0.76f);
                float done = SparkleFx.CurtainOpen(LevelCurtain.Duration);
                Check("open-after-wash", shut < 0.001f && mid > 0.25f && mid < 0.75f && done > 0.98f,
                    "shut " + shut.ToString("0.00") + " mid " + mid.ToString("0.00") + " end " + done.ToString("0.00"));

                float left = SparkleFx.CurtainShiftX(0.20f, 0.60f);
                float right = SparkleFx.CurtainShiftX(0.80f, 0.60f);
                float same = SparkleFx.CurtainShiftX(0.15f, 0.60f);
                // Each half travels as one sheet, so two points on the left move together.
                Check("tear-opposite-sides",
                    left < -0.2f && right > 0.2f && Mathf.Abs(left + right) < 0.03f
                    && Mathf.Abs(same - left) < 0.03f,
                    "L " + left.ToString("0.00") + " R " + right.ToString("0.00"));
                Check("shift-zero-while-shut",
                    Mathf.Abs(SparkleFx.CurtainShiftX(0.2f, 0f)) < 0.001f
                    && Mathf.Abs(SparkleFx.CurtainShiftX(0.8f, SparkleFx.CurtainOpen(0.2f))) < 0.001f,
                    "no slide during build");

                float build = Cover(0.12f, src, buf);
                float wash = Cover(LevelCurtain.Flood, src, buf);
                // The early arc is a handful of glints, not a sheet. Zero coverage fails;
                // a quarter of the glass fails too.
                Check("build-not-yet-full", build > 0.015f && build < 0.25f, "cover " + build.ToString("0.00"));
                // The veil, not the glints, covers the glass. It is off at the apex hold
                // (Flood), peaks translucent, and the glint field stays sparse.
                float veil = LevelCurtain.WashLevel(LevelCurtain.WashPeak);
                Check("wash-covers-glass",
                    veil > 0.98f && LevelCurtain.WashBase * veil < 0.40f
                    && LevelCurtain.WashLevel(LevelCurtain.Flood) < 0.001f
                    && LevelCurtain.WashLevel(LevelCurtain.Duration) < 0.001f
                    && wash < 0.25f,
                    "veil " + veil.ToString("0.00") + " glint " + wash.ToString("0.00"));

                // Was part-is-center-out: the fountain does not tear a hole. The veil
                // peaks, then fades out through the reveal.
                float laterVeil = LevelCurtain.WashLevel(0.90f);
                Check("reveal-fades-veil",
                    veil > 0.98f && laterVeil < veil - 0.20f
                    && LevelCurtain.WashLevel(LevelCurtain.Duration) < 0.001f
                    && LevelCurtain.WashLevel(LevelCurtain.Flood) < 0.001f,
                    "peak " + veil.ToString("0.00") + " later " + laterVeil.ToString("0.00"));

                int end = LevelCurtain.Fill(LevelCurtain.Duration + 0.2f, W, H, src, buf);
                Check("part-clears", end < 8, "left " + end);

                float biggest = 0f;
                int wn = LevelCurtain.Fill(LevelCurtain.Flood, W, H, src, buf);
                for (int i = 0; i < wn; i++) if (buf[i].Size > biggest) biggest = buf[i].Size;
                Check("wash-is-sparkles", biggest < Mathf.Min(W, H) * 0.20f && wn > LevelCurtain.Count / 2,
                    "n " + wn + " max " + biggest.ToString("0"));

                LevelCurtain.Reset();
                const float t0 = 4f;
                LevelCurtain.Arm(t0);
                LevelCurtain.BeginLoad();
                float held = LevelCurtain.VisualAge(t0 + 3f);
                LevelCurtain.MarkReady();
                bool play = Mathf.Abs(LevelCurtain.VisualAge(t0 + 0.2f) - 0.2f) < 0.001f
                    && !LevelCurtain.Finished(t0 + 0.5f)
                    && LevelCurtain.Finished(t0 + LevelCurtain.Duration);
                LevelCurtain.Reset();
                Check("holds-on-wash-until-ready",
                    Mathf.Abs(held - LevelCurtain.Flood) < 0.001f && play,
                    "held " + held.ToString("0.00"));

                string curtain = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/LevelCurtain.cs"));
                string fx = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/SparkleFx.cs"));
                Check("draws-through-helper",
                    curtain.Contains("SparkleFx.DrawDepthGlint")
                    && curtain.Contains("SparkleFx.DepthArcAt")
                    && curtain.Contains("GUI.DrawTexture")
                    && !curtain.Contains("SparkleFx.DrawCurtainGlint")
                    && !curtain.Contains("SparkleFx.CurtainShiftX")
                    && fx.Contains("DrawDepthGlint"),
                    "depth arc and veil");
            }
            catch (System.Exception ex)
            {
                Check("curtain", false, ex.GetType().Name + " " + ex.Message);
            }
            string endLine = fail == 0
                ? "BUILD88CURTAIN_OK " + pass
                : "BUILD88CURTAIN_FAIL " + fail + " passed " + pass;
            sb.AppendLine(endLine);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[build88curtain] " + endLine);
            if (fail > 0) throw new System.Exception(endLine);
        }

        static float Cover(float age, Rect src, LevelCurtain.Glint[] buf)
        {
            const int GX = 12, GY = 24;
            int n = LevelCurtain.Fill(age, W, H, src, buf);
            int on = 0;
            for (int cy = 0; cy < GY; cy++)
            for (int cx = 0; cx < GX; cx++)
            {
                float px = (cx + 0.5f) * W / GX;
                float py = (cy + 0.5f) * H / GY;
                for (int i = 0; i < n; i++)
                {
                    float r = buf[i].Size * 0.5f;
                    if (Mathf.Abs(px - buf[i].X) <= r && Mathf.Abs(py - buf[i].Y) <= r)
                    {
                        on++;
                        break;
                    }
                }
            }
            return on / (float)(GX * GY);
        }

    }
}
#endif
