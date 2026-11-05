#if UNITY_EDITOR
using UnityEngine;

namespace FlockFive.Editor
{
    // b120 item 1: Level 1 step 3's glove sat top-left (~66,130) on a stale aim, ~210px from left branch 2
    // (~100,345), and only bobbed. The locked glove is re-anchored every frame to the live target spot.
    static class GloveAnchorTests
    {
        public static void Run()
        {
            int fail = 0, n = 0;
            void Check(bool ok, string what) { n++; if (!ok) { fail++; Debug.LogError("[glove-anchor] FAIL " + what); } }
            // Guard: an unset / zero target never shows the glove; a nudge far off the target is dropped.
            Check(!FlockFiveApp.GloveTargetSet(Vector2.zero, 402, 874), "zero target accepted");
            Check(!FlockFiveApp.GloveTargetSet(new Vector2(float.NaN, 5), 402, 874), "NaN target accepted");
            Check(!FlockFiveApp.GloveTargetSet(new Vector2(-4, 300), 402, 874), "off-glass target accepted");
            Check(FlockFiveApp.GloveTargetSet(new Vector2(100, 345), 402, 874), "real target rejected");
            float dh = FlockFiveApp.GloveDh(1f);
            Check(!FlockFiveApp.GloveAimNearTarget(new Vector2(66, 130), new Vector2(100, 345), dh), "210px drift accepted");
            Check(FlockFiveApp.GloveAimNearTarget(new Vector2(100, 360), new Vector2(100, 345), dh), "small banner nudge rejected");
            foreach (var sz in new[] { new Vector2(402, 874), new Vector2(820, 1180), new Vector2(412, 915) })
            {
                float k = sz.x / 402f;
                var stale = new Vector2(66, 130) * k;
                var live = new Vector2(100, 345) * k;
                var lockAim = stale;
                var tip = stale + new Vector2(60f, 0f) * k; // the arc start sits beside the stale aim
                var travel0 = tip - lockAim;
                FlockFiveApp.GloveReanchor(ref lockAim, ref tip, live);
                Check(lockAim == live, sz + " lock aim " + lockAim + " != live " + live);
                // start, path and settled end: travel keeps its length, the end (travel = 1) is the target
                Check((tip - lockAim - travel0).sqrMagnitude < 0.01f, sz + " travel changed");
                Check(tip != lockAim, sz + " start collapsed onto the target (no travel)");
                var mid = Vector2.Lerp(tip, lockAim, 0.5f);
                var end = Vector2.Lerp(tip, lockAim, 1f);
                Check(Vector2.Distance(end, live) < 0.5f, sz + " settled end " + end);
                Check(Vector2.Distance(mid, live) < Vector2.Distance(tip, live), sz + " path does not approach");
                // a NaN live spot (unresolved) leaves the pose alone
                var a2 = lockAim; var t2 = tip;
                FlockFiveApp.GloveReanchor(ref a2, ref t2, new Vector2(float.NaN, 1f));
                Check(a2 == lockAim && t2 == tip, sz + " NaN moved the glove");
            }
            Debug.Log("[glove-anchor] " + (fail == 0 ? "PASS " + n + "/" + n : "FAIL " + fail + "/" + n));
        }
    }
}
#endif
