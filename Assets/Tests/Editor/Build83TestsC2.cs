#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 83 pass c2. Level curtain is the b88 tear (build/wash/part), glove never presses and labels stay on the cap (9).
    // Kit seat on the skull (6) is covered by Build81Tests kit-on-dome via BirdIdle.KitTouch.
    static class Build83TestsC2
    {
        [MenuItem("Flock Five/Build 83c2 Tests")]
        public static void Run()
        {
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++; else fail++;
                Debug.Log("[build83c2] " + (ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }
            CheckCurtain(Check);
            CheckPress(Check);
            Debug.Log(fail == 0
                ? "build83c2 tests DONE " + pass + "/" + (pass + fail)
                : "build83c2 tests FAIL " + fail + " failed, " + pass + " passed");
        }

        static void CheckCurtain(System.Action<string, bool, string> Check)
        {
            const float W = 1179f, H = 2556f;
            var src = new Rect(390f, 1800f, 400f, 400f);
            var buf = new LevelCurtain.Glint[LevelCurtain.Count * 2];
            float build = Cover(0.12f, W, H, src, buf);
            float wash = Cover(LevelCurtain.Flood, W, H, src, buf);
            float left = SparkleFx.CurtainShiftX(0.22f, 0.55f);
            float right = SparkleFx.CurtainShiftX(0.78f, 0.55f);
            bool tear = left < -0.15f && right > 0.15f && Mathf.Abs(left + right) < 0.04f;
            Check("curtain-wash-covers", wash > 0.80f && wash > build + 0.25f,
                "wash " + wash.ToString("0.00") + " build " + build.ToString("0.00"));
            Check("curtain-tear-sides", tear, "L " + left.ToString("0.00") + " R " + right.ToString("0.00"));
            Check("curtain-beats", LevelCurtain.BeatName(0.1f) == "build" && LevelCurtain.BeatName(LevelCurtain.Flood) == "wash"
                && LevelCurtain.BeatName(0.75f) == "part", "build/wash/part");
            int end = LevelCurtain.Fill(1.6f, W, H, src, buf);
            Check("curtain-clears", end < LevelCurtain.Count / 6, "glints left at 1.6s " + end);
            Check("curtain-duration", LevelCurtain.Duration >= 0.8f && LevelCurtain.Duration <= 1.2f, "dur " + LevelCurtain.Duration);
            float biggest = 0f;
            int n = LevelCurtain.Fill(LevelCurtain.Flood, W, H, src, buf);
            for (int i = 0; i < n; i++) if (buf[i].Size > biggest) biggest = buf[i].Size;
            Check("curtain-no-fullscreen-quad", biggest < Mathf.Min(W, H) * 0.22f, "max glint " + biggest.ToString("0"));
        }

        static float Cover(float age, float w, float h, Rect src, LevelCurtain.Glint[] buf)
        {
            const int GX = 16, GY = 32;
            int n = LevelCurtain.Fill(age, w, h, src, buf);
            var cell = new bool[GX * GY];
            for (int i = 0; i < n; i++)
            {
                var g = buf[i];
                float r = g.Size * 0.5f;
                for (int cy = 0; cy < GY; cy++)
                for (int cx = 0; cx < GX; cx++)
                {
                    float px = (cx + 0.5f) * w / GX, py = (cy + 0.5f) * h / GY;
                    if (Mathf.Abs(px - g.X) <= r && Mathf.Abs(py - g.Y) <= r) cell[cy * GX + cx] = true;
                }
            }
            int on = 0;
            for (int i = 0; i < cell.Length; i++) if (cell[i]) on++;
            return on / (float)cell.Length;
        }

        static void CheckPress(System.Action<string, bool, string> Check)
        {
            Check("glove-never-presses", !PushButton.Finger(false, true), "glove only -> no press");
            Check("finger-presses", PushButton.Finger(true, false) && PushButton.Finger(true, true), "real pointer -> press");
            var rest = new Rect(300f, 1700f, 420f, 420f);
            var cap = PushButton.FlowerCap(rest);
            var label = new Rect(cap.center.x - 90f, cap.center.y - 30f, 180f, 60f);
            bool centered = true; string why = "ok";
            float[] ages = { 0.02f, 0.06f, 0.13f, 0.2f, 0.28f };
            foreach (var a in ages)
            {
                var pose = PushButton.At(false, a);
                var moved = PushButton.Visual(rest, pose);
                var mcap = PushButton.FlowerCap(moved);
                var f = PushButton.Follow(label, rest, moved, 2f);
                float d = Mathf.Abs(f.center.x - mcap.center.x);
                if (d > 1.5f) { centered = false; why = "age " + a + " dx " + d.ToString("0.0"); }
            }
            var held = PushButton.Visual(rest, PushButton.At(true, 0f));
            var fh = PushButton.Follow(label, rest, held, 2f);
            float dh = Mathf.Abs(fh.center.x - PushButton.FlowerCap(held).center.x);
            if (dh > 1.5f) { centered = false; why = "held dx " + dh.ToString("0.0"); }
            Check("label-centered-all-press-frames", centered, why);
        }
    }
}
#endif
