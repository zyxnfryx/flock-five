#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Brandon: garden 1 is always clear weather. No rain, no storm, no lightning, ever.
    public static class Build126ClearGardenOneTests
    {
        [MenuItem("Flock Five/Build 126 Clear Garden 1 Tests")]
        public static void Run()
        {
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++; else fail++;
                Debug.Log((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            Check("garden-1-no-storm", !GardenStorm.StormAllowed(1), "");
            Check("garden-2-storms", GardenStorm.StormAllowed(2), "");
            Check("garden-1-no-lightning", !LightningStrike.GardenEligible(0), "index 0");
            Check("stages", GardenStorm.StageFor(1) == GardenStorm.StormStage.Clear && GardenStorm.StageFor(2) == GardenStorm.StormStage.RainOnly
                && GardenStorm.StageFor(3) == GardenStorm.StormStage.FirstStrike && GardenStorm.StageFor(4) == GardenStorm.StormStage.Normal
                && GardenStorm.StageFor(40) == GardenStorm.StormStage.Normal, "1 clear, 2 rain, 3 first strike, 4+ normal");
            Check("garden-2-rain-no-strike", GardenStorm.StormAllowed(2) && !LightningStrike.GardenEligible(1), "");
            Check("garden-3-first-strike", LightningStrike.GardenEligible(2) && GardenStorm.FirstWaitFor(3) < GardenStorm.FirstWait
                && GardenStorm.WantStorm(GardenStorm.FirstStrikeWait + 0.1f, GardenStorm.FirstWaitFor(3)), "wait " + GardenStorm.FirstWaitFor(3) + "s");
            Check("garden-4-unchanged", GardenStorm.FirstWaitFor(4) == GardenStorm.FirstWait && LightningStrike.GardenEligible(3), "");
            // Brandon: the first strike and the sparrow's first visit never share a garden.
            // Behaviour (Brandon): simulate the ticks. A fast garden-3 clear never pushes the intro into garden 4.
            const float dt = 0.1f;
            bool struck = false, won3 = false;
            LightningStrike.Plan p3 = LightningStrike.Plan.None;
            for (float t = 0f; t < 30f && !won3; t += dt)
            {
                won3 = t >= 2.0f; // cleared before the intro delay (worst case)
                var pl = LightningStrike.Decide(2, struck, GardenStorm.WantStorm(t, GardenStorm.FirstWaitFor(3)), t, true, false, false, won3, 1f);
                if (pl != LightningStrike.Plan.None) { p3 = pl; struck = true; }
            }
            int g4Strikes = 0, g4Intro = 0;
            for (float t = 0f; t < 600f; t += dt)
            {
                bool lineUp = t > 20f && t < 30f; // sparrow lesson line up
                var pl = LightningStrike.Decide(3, struck, GardenStorm.WantStorm(t, GardenStorm.FirstWaitFor(4)), t, true, lineUp, false, false);
                if (pl == LightningStrike.Plan.Intro) g4Intro++;
                if (pl != LightningStrike.Plan.None) g4Strikes++;
            }
            Check("fast-garden-3-clear-no-strike-in-garden-4", p3 == LightningStrike.Plan.None && g4Strikes == 0 && g4Intro == 0,
                "g3 " + p3 + " g4 strikes " + g4Strikes);
            // Normal pace: replay garden 3's real wet curve (GardenStorm Fade 2.1 s toward want, capped by the garden-3
            // onset). The intro must land with rain on screen (Wet > 0.3, past StormGlass.WetOn so glass drops spawn)
            // and before a fastest realistic 9-move clear: ~0.7 s per move (pick + place taps plus the hop) = ~6.3 s.
            const float FastestNineMoveClear = 6.3f;
            float at = -1f, wetAt = 0f, wet = 0f;
            for (float t = 0f; t < 30f && at < 0f; t += 1f / 60f)
            {
                float want = GardenStorm.WantStorm(t, GardenStorm.FirstWaitFor(3)) ? 1f : 0f;
                wet = Mathf.MoveTowards(wet, want, (1f / 60f) / 2.1f);
                wet = Mathf.Min(wet, StormGlass.OnsetMul(t, GardenStorm.OnsetDelayFor(3)));
                if (LightningStrike.Decide(2, false, wet > 0.45f, t, true, false, false, false, wet) == LightningStrike.Plan.Intro) { at = t; wetAt = wet; }
            }
            Check("garden-3-intro-lands-in-visible-rain", at >= LightningStrike.IntroDelay - 0.01f && wetAt > LightningStrike.IntroWet
                && StormGlass.Storming(true, wetAt) && at < FastestNineMoveClear,
                "at " + at.ToString("0.00") + "s wet " + wetAt.ToString("0.00") + " (glass drops from " + StormGlass.WetOn + ")");
            Check("garden-3-dry-never-strikes", LightningStrike.Decide(2, false, false, 10f, true, false, false, false, 0f) == LightningStrike.Plan.None, "");
            Check("onset-short-on-3-only", GardenStorm.OnsetDelayFor(3) < StormGlass.StartDelay && GardenStorm.OnsetDelayFor(2) == StormGlass.StartDelay
                && GardenStorm.OnsetDelayFor(4) == StormGlass.StartDelay, "");
            // After the sparrow intro, garden 4 may roll normally (still never an owed intro).
            Check("garden-4-rolls-after-sparrow", LightningStrike.Decide(3, false, true, 60f, true, false, true, false) == LightningStrike.Plan.Roll
                && LightningStrike.Decide(3, false, true, 60f, true, true, true, false) == LightningStrike.Plan.None, "");
            Check("pest-line-blocks-any-garden", LightningStrike.Decide(9, true, true, 60f, true, true, true, false) == LightningStrike.Plan.None, "");

            int firstSparrow = 0;
            for (int g = 1; g <= 20 && firstSparrow == 0; g++) if (PestSchedule.CapFor(g) > 0) firstSparrow = g;
            Check("first-strike-not-first-sparrow", firstSparrow == 4 && firstSparrow != GardenStorm.FirstStrikeGarden,
                "strike " + GardenStorm.FirstStrikeGarden + " sparrow " + firstSparrow);

            // Run a garden-1 storm sheet well past FirstWait and through a full storm window.
            var root = new GameObject("clear-g1-root");
            try
            {
                var gs = GardenStorm.Attach(root.transform, GardenSeason.ForLevel(1), 1);
                gs.JumpToStorm();
                float peak = 0f;
                var late = typeof(GardenStorm).GetMethod("LateUpdate",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                float until = GardenStorm.FirstWait + GardenStorm.StormLen + 5f;
                for (float t = 0f; t < until; t += 1f / 30f)
                {
                    PlayClock.StepForTest(1f / 30f);
                    late.Invoke(gs, null);
                    peak = Mathf.Max(peak, GardenStorm.Wet, GardenStorm.SkyFlash);
                }
                Check("garden-1-stays-dry", gs.IsClear && !gs.IsSnow && peak <= 0f, "peak wet/flash " + peak);
            }
            finally { Object.DestroyImmediate(root); }

            Debug.Log(fail == 0 ? "build126 clear-garden-1 tests DONE " + pass + "/" + pass
                : "build126 clear-garden-1 tests FAIL " + fail + " failed, " + pass + " passed");
        }
    }
}
#endif
