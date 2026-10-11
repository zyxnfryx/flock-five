#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 83 pass c2. Sparkle fountain curtain (5/5b), glove never presses and labels stay on the cap (9).
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
            const int GX = 24, GY = 48;
            float worst = 0f, worstAge = 0f;
            float apexTop = 1f;
            bool fellLater = false;
            float minYApex = 9f, minYFall = 9f;
            for (int k = 0; k <= 24; k++)
            {
                float age = k * 0.05f;
                int n = LevelCurtain.Fill(age, W, H, src, buf);
                var cell = new bool[GX * GY];
                float meanY = 0f; int my = 0;
                for (int i = 0; i < n; i++)
                {
                    var g = buf[i];
                    float r = g.Size * 0.5f;
                    for (int cy = 0; cy < GY; cy++)
                    for (int cx = 0; cx < GX; cx++)
                    {
                        float px = (cx + 0.5f) * W / GX, py = (cy + 0.5f) * H / GY;
                        if (Mathf.Abs(px - g.X) <= r && Mathf.Abs(py - g.Y) <= r) cell[cy * GX + cx] = true;
                    }
                    meanY += g.Y / H; my++;
                }
                int on = 0; foreach (var c in cell) if (c) on++;
                float cover = on / (float)(GX * GY);
                if (cover > worst) { worst = cover; worstAge = age; }
                if (my > 0)
                {
                    meanY /= my;
                    string beat = LevelCurtain.BeatName(age);
                    if (beat == "apex" && meanY < minYApex) minYApex = meanY;
                    if (beat == "fall" && meanY < minYFall) minYFall = meanY;
                }
                if (age >= 0.9f && n > 0) fellLater = true;
            }
            Check("curtain-no-solid-fill", worst < 0.55f, "max grid cover " + worst.ToString("0.00") + " at " + worstAge.ToString("0.00") + "s");
            float oy = src.center.y / H;
            Check("curtain-fountain-rises", minYApex < oy - 0.2f, "apex mean y " + minYApex.ToString("0.00") + " source y " + oy.ToString("0.00"));
            Check("curtain-rain-falls", minYFall > minYApex, "fall mean y " + minYFall.ToString("0.00") + " > apex " + minYApex.ToString("0.00"));
            Check("curtain-beats", LevelCurtain.BeatName(0.1f) == "burst" && LevelCurtain.BeatName(0.3f) == "apex"
                && LevelCurtain.BeatName(0.6f) == "fall" && LevelCurtain.BeatName(0.9f) == "reveal", "burst/apex/fall/reveal");
            int end = LevelCurtain.Fill(1.6f, W, H, src, buf);
            Check("curtain-clears", end < LevelCurtain.Count / 6, "glints left at 1.6s " + end);
            Check("curtain-duration", LevelCurtain.Duration >= 0.8f && LevelCurtain.Duration <= 1.2f, "dur " + LevelCurtain.Duration);
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
