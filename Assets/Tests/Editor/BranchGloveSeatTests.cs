#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // Garden pickup/place glove sits on the branch hit rect, not in the column gap.
    // Home LEVEL 1 cue is once, until a garden is entered.
    public static class BranchGloveSeatTests
    {
        const string Out = "/tmp/flock-five-branch-glove-seat-tests.txt";

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
                Debug.Log("[branch-glove] " + line);
            }

            float? keep = DeviceFit.TestWindow;
            try
            {
                Check("home play cue shows on a fresh level 1 save",
                    FlockFiveApp.HomePlayCueDue(0, 0, 0), "seen 0 next 0 coach 0");
                Check("home play cue stays down after it has been seen",
                    !FlockFiveApp.HomePlayCueDue(1, 0, 0), "seen");
                Check("home play cue stays down after a garden has been cleared",
                    !FlockFiveApp.HomePlayCueDue(0, 1, 0), "next");
                Check("home play cue stays down after the garden coach has finished",
                    !FlockFiveApp.HomePlayCueDue(0, 0, 1), "coach");

                var faces = new[]
                {
                    new Vector2(1179f, 2556f), // tall phone
                    new Vector2(750f, 1334f),  // SE
                    new Vector2(1536f, 2048f), // iPad window
                    new Vector2(2048f, 2732f), // iPad pro
                    new Vector2(1152f, 2048f), // iPad pillar area, 9:16
                };
                string[] tags = { "iphone", "se", "ipad", "ipad-pro", "ipad-pillar" };
                for (int d = 0; d < faces.Length; d++)
                {
                    float w = faces[d].x, h = faces[d].y;
                    DeviceFit.TestWindow = w / h;
                    float s = DeviceFit.Ui(w, h);
                    float dh = FlockFiveApp.GloveAuditDh(s);
                    float col = WorldBuilder.ColumnX();
                    float y0 = WorldBuilder.RowY0For(1f);
                    // Level 1 step 1 is branch 0 (top-left). Step 2 places on branch 5 (right, row 2).
                    var left = WoodGui(w, h, -col, y0, false);
                    var rightTop = WoodGui(w, h, col, y0, true);
                    var right = WoodGui(w, h, col, y0 - 2f * WorldBuilder.RowGap, true);
                    float gapL = left.xMax;
                    float gapR = rightTop.xMin;
                    bool gap = gapL < w * 0.5f - 4f && gapR > w * 0.5f + 4f && gapL < gapR && !left.Overlaps(rightTop);
                    Check("column gap is open " + tags[d], gap,
                        "gap " + gapL.ToString("0") + ".." + gapR.ToString("0") + " mid " + (w * 0.5f).ToString("0"));

                    Seat(Check, tags[d] + " L0", left, rightTop, w, dh, gapL, gapR);
                    Seat(Check, tags[d] + " R2", right, left, w, dh, gapL, gapR);

                    var level = SplashPress.LevelRect(w, h, new Rect(0f, 0f, w, h));
                    var pivot = FlockFiveApp.SeatBranchGlove(level, w, dh);
                    float ang = FlockFiveApp.BranchGloveAng(level, w);
                    bool mirror = FlockFiveApp.BranchGloveMirror(level, w);
                    var box = FlockFiveApp.GloveBox(pivot, ang, dh, mirror);
                    bool on = level.Contains(pivot) && level.Contains(box.center);
                    Check("LEVEL 1 glove centered on the play puck " + tags[d], on && level.width > 8f,
                        "pivot " + pivot + " center " + box.center + " puck " + level);
                }
            }
            finally
            {
                DeviceFit.TestWindow = keep;
            }

            sb.AppendLine((fail == 0 ? "OK  " : "BAD ") + pass + " pass, " + fail + " fail");
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[branch-glove] " + (fail == 0 ? "OK" : "BAD") + " " + pass + "/" + (pass + fail));
        }

        static void Seat(System.Action<string, bool, string> check, string name, Rect hit, Rect other, float w, float dh, float gapL, float gapR)
        {
            var pivot = FlockFiveApp.SeatBranchGlove(hit, w, dh);
            float ang = FlockFiveApp.BranchGloveAng(hit, w);
            bool mirror = FlockFiveApp.BranchGloveMirror(hit, w);
            var box = FlockFiveApp.GloveBox(pivot, ang, dh, mirror);
            float onWood = Overlap(box.xMin, box.xMax, hit.xMin, hit.xMax);
            float inGap = Overlap(box.xMin, box.xMax, gapL, gapR);
            float onOther = Overlap(box.xMin, box.xMax, other.xMin, other.xMax);
            bool tip = hit.Contains(pivot);
            bool mid = hit.Contains(box.center);
            bool clear = onWood > box.width * 0.5f && onWood > inGap && onWood > onOther;
            check("glove on the focus branch, not the column gap " + name,
                tip && mid && clear,
                "tip " + tip + " mid " + mid + " on " + onWood.ToString("0") + " gap " + inGap.ToString("0")
                + " other " + onOther.ToString("0") + " box " + box.xMin.ToString("0") + ".." + box.xMax.ToString("0"));
        }

        static Rect WoodGui(float w, float h, float worldX, float worldY, bool fromRight)
        {
            var wood = BranchHit.WoodRect(new Vector2(worldX, worldY), fromRight, false);
            var a = CornerAnchor.WorldToGui(w, h, wood.xMin, wood.yMax);
            var b = CornerAnchor.WorldToGui(w, h, wood.xMax, wood.yMin);
            return Rect.MinMaxRect(
                Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y),
                Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        static float Overlap(float a0, float a1, float b0, float b1)
        {
            float x0 = a0 > b0 ? a0 : b0;
            float x1 = a1 < b1 ? a1 : b1;
            return x1 > x0 ? x1 - x0 : 0f;
        }
    }
}
#endif
