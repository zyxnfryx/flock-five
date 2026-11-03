#if UNITY_EDITOR
using UnityEngine;

namespace FlockFive.Editor
{
    // b120: the poker Back glove rode in from mid-right and crossed the whole screen. Angled starts stay snug.
    static class GloveSnugTests
    {
        public static void Run()
        {
            int fail = 0;
            foreach (var sz in new[] { new Vector2(402, 874), new Vector2(820, 1180), new Vector2(412, 915) })
            {
                var safe = new Rect(8, 50, sz.x - 16, sz.y - 90);
                float s = 1f;
                var back = new Vector2(40, 90);
                var raw = FlockFiveApp.GloveMiddleRightEntry(safe);
                var p = FlockFiveApp.AngledStartSnug(raw, back, s, safe);
                float cap = 1.6f * FlockFiveApp.GloveDh(s) + 1f;
                float d = Vector2.Distance(p, back);
                bool ok = d <= cap && d < Vector2.Distance(raw, back) * 0.5f;
                if (!ok) { fail++; Debug.LogError("[glove-snug] FAIL " + sz + " dist " + d + " cap " + cap); }
            }
            Debug.Log("[glove-snug] " + (fail == 0 ? "PASS 3/3" : "FAIL " + fail));
        }
    }
}
#endif
