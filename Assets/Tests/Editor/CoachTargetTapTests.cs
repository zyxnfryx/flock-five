#if UNITY_EDITOR
using UnityEngine;

namespace FlockFive.Editor
{
    // b120 item 2: an empty-sky tap advanced the Level 1 tutorial once on a 412-wide Android viewport.
    // Only a press on the guided target (within one seat's reach) may advance a step.
    static class CoachTargetTapTests
    {
        public static void Run()
        {
            int fail = 0, n = 0;
            void Check(bool ok, string what) { n++; if (!ok) { fail++; Debug.LogError("[coach-target-tap] FAIL " + what); } }
            // ortho span ~ 20 world units over the phone height (Level 1 garden), UI scale 1
            foreach (var h in new[] { 874f, 1180f, 915f })
            {
                float wpp = 20f / h;
                float r = FlockFiveApp.CoachTargetReach(wpp, 1f);
                Check(r >= 1.05f && r <= 2f, h + " reach " + r);
                Check(FlockFiveApp.CoachTargetReachOk(0.5f * 0.5f, wpp, 1f), h + " seat press rejected");
                // the shared pick reaches 2.6-3.45 units: those sky presses must not advance a lesson
                Check(!FlockFiveApp.CoachTargetReachOk(2.6f * 2.6f, wpp, 1f), h + " 2.6u sky press advanced");
                Check(!FlockFiveApp.CoachTargetReachOk(3.25f * 3.25f, wpp, 1f), h + " 3.25u sky press advanced");
            }
            Check(FlockFiveApp.CoachTargetReach(0f, 1f) == 2f, "no camera falls back to 2u");
            Debug.Log("[coach-target-tap] " + (fail == 0 ? "PASS " + n + "/" + n : "FAIL " + fail + "/" + n));
        }
    }
}
#endif
