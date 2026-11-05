#if UNITY_EDITOR
using UnityEngine;

namespace FlockFive.Editor
{
    // b120 item 3: garden sideways gloves must keep a visible start-to-target run.
    // AngledStartSnug is for the poker-Back entry override only; applying it to every
    // angled start collapsed L1 steps 1–2 into a near-zero travel that looked like no glove.
    static class GardenGloveEntryTests
    {
        public static void Run()
        {
            int fail = 0;
            float s = 2f;
            float dh = FlockFiveApp.GloveDh(s);
            var safe = new Rect(40f, 40f, 740f, 1100f);
            var aim = new Vector2(200f, 320f);
            var rest = aim + new Vector2(1f, 0f) * 80f;
            var away = new Vector2(1f, 0f);
            var start = FlockFiveApp.GloveEntryPoint(rest, aim, away, dh);
            float gardenTravel = (start - aim).magnitude;
            var snug = FlockFiveApp.AngledStartSnug(start, aim, s, safe);
            float snugTravel = (snug - aim).magnitude;
            if (!(gardenTravel > dh * 0.8f))
            {
                fail++;
                Debug.LogError("[garden-glove-entry] FAIL garden travel " + gardenTravel + " dh " + dh);
            }
            if (!(snugTravel < gardenTravel * 0.75f))
            {
                fail++;
                Debug.LogError("[garden-glove-entry] FAIL snug did not collapse vs garden entry snug="
                    + snugTravel + " garden=" + gardenTravel);
            }
            var backStart = FlockFiveApp.GloveMiddleRightEntry(safe);
            var backSnug = FlockFiveApp.AngledStartSnug(backStart, aim, s, safe);
            if (!((backSnug - aim).magnitude <= dh * 1.7f + 1f))
            {
                fail++;
                Debug.LogError("[garden-glove-entry] FAIL poker-Back snug dist " + (backSnug - aim).magnitude);
            }
            Debug.Log("[garden-glove-entry] " + (fail == 0 ? "PASS 3/3" : "FAIL " + fail));
        }
    }
}
#endif
