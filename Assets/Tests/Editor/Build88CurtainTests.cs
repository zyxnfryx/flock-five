#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // Level button curtain. Sparkles build, wash the glass, then part from the
    // center. About one second. The reveal is a tear, not a left-to-right wipe.
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
                    LevelCurtain.BeatName(0.05f) == "build"
                    && LevelCurtain.BeatName(0.20f) == "build"
                    && LevelCurtain.BeatName(LevelCurtain.Flood) == "wash"
                    && LevelCurtain.BeatName(LevelCurtain.WashEnd + 0.02f) == "part"
                    && LevelCurtain.BeatName(0.90f) == "part",
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
                Check("helper-owns-phase",
                    LevelCurtain.BeatName(0.10f) == SparkleFx.CurtainPhase(0.10f)
                    && Mathf.Abs(LevelCurtain.Duration - SparkleFx.CurtainDuration) < 0.001f,
                    "shared SparkleFx clock");

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
                Check("build-not-yet-full", build > 0.04f && build < 0.62f, "cover " + build.ToString("0.00"));
                Check("wash-covers-glass", wash > 0.85f, "cover " + wash.ToString("0.00"));

                float early = 0.66f;
                float later = 0.80f;
                bool earlyTear = Tear(early, src, buf, out float eMid, out float eWid, out bool eWipe, out string eWhy);
                bool laterTear = Tear(later, src, buf, out float lMid, out float lWid, out bool lWipe, out string lWhy);
                Check("part-is-center-out",
                    earlyTear && laterTear && !eWipe && !lWipe
                    && Mathf.Abs(eMid - 0.5f) < 0.08f && Mathf.Abs(lMid - 0.5f) < 0.08f
                    && lWid > eWid + 0.08f,
                    "early " + eWhy + " later " + lWhy);

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
                    curtain.Contains("SparkleFx.DrawCurtainGlint")
                    && curtain.Contains("SparkleFx.CurtainShiftX")
                    && fx.Contains("DrawCurtainGlint")
                    && !curtain.Contains("GUI.DrawTexture"),
                    "shared draw");
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

        // Clear band around the middle, widening. A wipe clears from one edge
        // and leaves the other edge covered.
        static bool Tear(float age, Rect src, LevelCurtain.Glint[] buf,
            out float mid, out float width, out bool wipe, out string why)
        {
            const int C = 24;
            int n = LevelCurtain.Fill(age, W, H, src, buf);
            var hit = new bool[C];
            for (int i = 0; i < n; i++)
            {
                float r = buf[i].Size * 0.5f;
                int a = Mathf.FloorToInt((buf[i].X - r) / W * C);
                int b = Mathf.FloorToInt((buf[i].X + r) / W * C);
                if (a < 0) a = 0;
                if (b >= C) b = C - 1;
                for (int c = a; c <= b; c++) hit[c] = true;
            }
            int clearL = -1, clearR = -1;
            for (int c = 0; c < C; c++)
            {
                if (hit[c]) continue;
                if (clearL < 0) clearL = c;
                clearR = c;
            }
            // The center hole is the clear run that contains the middle column,
            // not a clear margin past an off-screen half.
            int midC = C / 2;
            int holeL = midC, holeR = midC;
            if (!hit[midC])
            {
                while (holeL > 0 && !hit[holeL - 1]) holeL--;
                while (holeR < C - 1 && !hit[holeR + 1]) holeR++;
            }
            mid = (holeL + holeR + 1) * 0.5f / C;
            width = (holeR - holeL + 1) / (float)C;
            bool leftOn = hit[0] || hit[1];
            bool rightOn = hit[C - 1] || hit[C - 2];
            bool centerOff = !hit[midC] && !hit[midC - 1];
            wipe = (!hit[0] && !hit[1] && !hit[2] && hit[C - 1] && hit[C - 2])
                || (hit[0] && hit[1] && !hit[C - 1] && !hit[C - 2] && !hit[C - 3]);
            bool ok = centerOff && leftOn && rightOn && holeL > 0 && holeR < C - 1;
            why = "mid " + mid.ToString("0.00") + " wid " + width.ToString("0.00")
                + " L" + (leftOn ? 1 : 0) + " R" + (rightOn ? 1 : 0)
                + " hole " + holeL + "-" + holeR + " n " + n
                + " span " + clearL + "-" + clearR;
            return ok;
        }
    }
}
#endif
