#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // Wheel Collect / 2X sprinkle speed. 73a237f (b88) stepped the shared coin flight
    // once per frame instead of once per OnGUI event, which doubled its on-screen time.
    // The flight stays one step per frame; the timing lives in named constants.
    public static class Build89CoinSpeedTests
    {
        const string Out = "/tmp/flock-five-b89-coin-speed-tests.txt";

        // Mirrors DrawCoinFly's per-frame step for n coins at a fixed frame rate.
        public static float SimulateSpan(int coins, float fps)
        {
            int n = Mathf.Clamp(coins, 0, FlockFiveApp.CoinFlyMax);
            if (n <= 0) return 0f;
            var delay = new float[n];
            var t = new float[n];
            for (int i = 0; i < n; i++) delay[i] = i * FlockFiveApp.CoinFlyStaggerSec;
            float dt = Mathf.Min(1f / fps, FlockFiveApp.CoinFlyMaxStep);
            float clock = 0f;
            int left = n;
            while (left > 0 && clock < 20f)
            {
                clock += dt;
                for (int i = 0; i < n; i++)
                {
                    if (t[i] >= 1f) continue;
                    delay[i] -= dt;
                    if (delay[i] > 0f) continue;
                    float run = Mathf.Min(dt, -delay[i]);
                    delay[i] = 0f;
                    t[i] += run / FlockFiveApp.CoinFlyDurSec;
                    if (t[i] >= 1f) left--;
                }
            }
            return clock;
        }

        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            int pass = 0, fail = 0;
            void Check(string n, bool ok, string d)
            {
                string line = (ok ? "PASS  " : "FAIL  ") + n + "  " + d;
                if (ok) pass++; else fail++;
                sb.AppendLine(line);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build89coin] " + line);
            }
            try
            {
                float stag = FlockFiveApp.CoinFlyStaggerSec;
                float dur = FlockFiveApp.CoinFlyDurSec;
                float span = FlockFiveApp.CoinFlySpanSec(12);
                // b87 as played (two OnGUI steps a frame): 0.066 s stagger, 0.871 s flight, 1.60 s for 12.
                Check("stagger-snappy", stag > 0.03f && stag <= 0.066f, "stagger " + stag.ToString("0.000"));
                Check("flight-snappy", dur > 0.5f && dur <= 0.871f, "flight " + dur.ToString("0.000"));
                Check("span-12-at-or-under-b87", span <= 1.60f && span > 0.9f, "span12 " + span.ToString("0.000"));
                Check("span-1", Mathf.Abs(FlockFiveApp.CoinFlySpanSec(1) - dur) < 1e-4f, "span1");
                Check("span-clamps-to-max",
                    Mathf.Abs(FlockFiveApp.CoinFlySpanSec(500) - span) < 1e-4f && FlockFiveApp.CoinFlySpanSec(0) == 0f, "clamp");

                foreach (float fps in new[] { 30f, 60f, 120f })
                {
                    float sim = SimulateSpan(12, fps);
                    Check("frame-rate-even-" + fps, Mathf.Abs(sim - span) <= 1f / fps + 1e-3f,
                        "sim " + sim.ToString("0.000") + " vs " + span.ToString("0.000"));
                }
                // Hitch frame clamps instead of teleporting.
                float hitch = SimulateSpan(12, 5f);
                Check("hitch-clamped", hitch >= span - 0.06f, "5fps " + hitch.ToString("0.000"));

                float prev = -1f;
                bool mono = true;
                for (int i = 0; i <= 100; i++)
                {
                    float k = FlockFiveApp.CoinFlyEase(i / 100f);
                    if (k < prev - 1e-6f) mono = false;
                    prev = k;
                }
                float k0 = FlockFiveApp.CoinFlyEase(0f), k1 = FlockFiveApp.CoinFlyEase(1f);
                float launch = FlockFiveApp.CoinFlyEase(0.1f);
                float catchV = (1f - FlockFiveApp.CoinFlyEase(0.99f)) / 0.01f;
                Check("ease-ends", Mathf.Abs(k0) < 1e-5f && Mathf.Abs(k1 - 1f) < 1e-5f && mono, "mono " + mono);
                Check("ease-launches", launch > 0.07f, "k(0.1) " + launch.ToString("0.000") + " (smoothstep 0.028)");
                Check("ease-soft-catch", catchV < 0.1f, "end speed " + catchV.ToString("0.000"));

                string home = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/FlockFiveApp.cs"));
                Check("one-step-per-frame-kept",
                    home.IndexOf("bool step = _coinFlyFrame != Time.frameCount;") > 0, "guard");
                Check("no-inline-timings",
                    home.IndexOf("0.10f * RewardPace") < 0 && home.IndexOf("1.32f * RewardPace") < 0
                    && home.IndexOf("Delay = i * CoinFlyStaggerSec") > 0, "constants");
                string choice = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/FlockFiveApp.RewardChoice.cs"));
                Check("reward-choice-hold-intact", choice.IndexOf("int _rewardChoice;") > 0, "choice");
            }
            catch (System.Exception ex)
            {
                Check("exception", false, ex.ToString());
            }
            sb.AppendLine("TOTAL pass " + pass + " fail " + fail);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[build89coin] TOTAL pass " + pass + " fail " + fail);
        }
    }
}
#endif
