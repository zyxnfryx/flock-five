#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 89: come-back notifications exist only while the app is closed.
    static class Build89NotifyTests
    {
        [MenuItem("Flock Five/Build 89 Notify Tests")]
        public static void Run()
        {
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++; else fail++;
                Debug.Log("[build89-notify] " + (ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }
            DateTime At(int d, int h, int m = 0) { return new DateTime(2026, 10, d, h, m, 0, DateTimeKind.Local); }

            var a = NotifyPlan.Compute(At(14, 12), 5, false, true);
            Check("unclaimed noon: ready 6pm today + keep 8pm today", a.Count == 2 && a[0].When == At(14, 18) && a[1].When == At(14, 20), a.Count.ToString());
            var b = NotifyPlan.Compute(At(14, 19), 5, false, true);
            Check("unclaimed 7pm: ready tomorrow, no keep", b.Count == 1 && b[0].When == At(15, 18), b.Count.ToString());
            var c = NotifyPlan.Compute(At(14, 12), 5, true, true);
            Check("claimed: tomorrow 6pm + 8pm", c.Count == 2 && c[0].When == At(15, 18) && c[1].When == At(15, 20), c.Count.ToString());
            var d = NotifyPlan.Compute(At(14, 12), 5, true, false);
            Check("claimed, not at risk: ready only", d.Count == 1, d.Count.ToString());
            var e = NotifyPlan.Compute(At(14, 17, 59), 1, false, true);
            Check("near-future (<60s lead is dropped to next day)", NotifyPlan.Compute(At(14, 17, 59).AddSeconds(30), 1, false, false)[0].When == At(15, 18) && e[0].When == At(14, 18), "");
            var f = NotifyPlan.Compute(At(14, 12), 1, false, true);
            Check("streak 1 never gets keep line", f.Count == 1, f.Count.ToString());
            var g = NotifyPlan.Filter(new System.Collections.Generic.List<NotifyPlan.Entry> {
                new NotifyPlan.Entry(NotifyPlan.Kind.Ready, At(14, 11), "x"),
                new NotifyPlan.Entry(NotifyPlan.Kind.Ready, At(14, 12).AddSeconds(10), "y"),
                new NotifyPlan.Entry(NotifyPlan.Kind.Ready, At(14, 13), "z") }, At(14, 12));
            Check("filter drops past and near-future", g.Count == 1 && g[0].Body == "z", g.Count.ToString());
            Check("all entries strictly future by min lead", AllFuture(), "");
            Check("no schedule while active", !NotifyPlan.MayScheduleOn(true) && NotifyPlan.MayScheduleOn(false), "");
            Check("keep body", NotifyPlan.KeepBody(7) == "Keep your 7 day streak!", NotifyPlan.KeepBody(7));

            bool AllFuture()
            {
                for (int h = 0; h < 24; h++)
                    foreach (bool cl in new[] { false, true })
                        foreach (var en in NotifyPlan.Compute(At(14, h, 59), 4, cl, true))
                            if ((en.When - At(14, h, 59)).TotalSeconds < NotifyPlan.MinLeadSeconds) return false;
                return true;
            }

            Debug.Log("[build89-notify] " + pass + " passed, " + fail + " failed");
            if (Application.isBatchMode) EditorApplication.Exit(fail == 0 ? 0 : 1);
        }
    }
}
#endif
