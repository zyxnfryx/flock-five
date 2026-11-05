#if UNITY_EDITOR
using UnityEngine;

namespace FlockFive.Editor
{
    // b120 item 3: garden sideways gloves keep the start GloveLockPose seats (AngledLockStart).
    // #224 ran every angled start through AngledStartSnug, which applies GloveScaleTravel a second
    // time and pulls the hand toward its target. Only the poker-Back entry override is snugged now.
    // Garden travel is already short by design (b95 0.67x, b125 GloveTravelScale 0.5); this guards
    // that it is not shortened again, not that it is long.
    static class GardenGloveEntryTests
    {
        public static void Run()
        {
            int fail = 0, n = 0;
            void Check(bool ok, string what) { n++; if (!ok) { fail++; Debug.LogError("[garden-glove-entry] FAIL " + what); } }
            foreach (float s in new[] { 1f, 2f })
            {
                float dh = FlockFiveApp.GloveDh(s);
                var safe = new Rect(8f * s, 40f * s, 410f * s, 590f * s);
                var aim = new Vector2(100f * s, 160f * s);
                var away = new Vector2(1f, 0f);
                var rest = aim + away * 40f * s;
                var start = FlockFiveApp.AngledLockStart(rest, aim, away, s, safe, false, default);
                float garden = (start - aim).magnitude;
                var snug = FlockFiveApp.AngledStartSnug(start, aim, s, safe);
                float snugged = (snug - aim).magnitude;
                Check(start.y >= aim.y + 28f * s - 0.01f, s + " garden start not below the target line " + start);
                Check(garden >= FlockFiveApp.GloveMinStartHands * dh - 0.01f, s + " garden start inside min travel " + garden + " dh " + dh);
                Check(start.x > aim.x, s + " garden start lost its sideways (right) entry side " + start);
                Check(snugged < garden - 1f, s + " snug would not have shortened garden travel (fix is moot) snug=" + snugged + " garden=" + garden);
                var back = FlockFiveApp.GloveMiddleRightEntry(safe);
                var backStart = FlockFiveApp.AngledLockStart(rest, aim, away, s, safe, true, back);
                var backSnug = FlockFiveApp.AngledStartSnug(backStart, aim, s, safe);
                Check((backSnug - aim).magnitude <= dh * 1.7f + 1f, s + " poker-Back snug dist " + (backSnug - aim).magnitude);
            }
            Debug.Log("[garden-glove-entry] " + (fail == 0 ? "PASS " + n + "/" + n : "FAIL " + fail + "/" + n));
        }
    }
}
#endif
