#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Brandon: garden 1 is always clear weather. No rain, no storm, no lightning, ever.
    static class Build126ClearGardenOneTests
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
