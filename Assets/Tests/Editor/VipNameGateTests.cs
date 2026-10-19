#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // The rail VIP button/badge stays hidden through the new-player intro (streak award, avatar
    // reveal, banner) and shows once the name picker is done. Players with a name see it as before.
    public static class VipNameGateTests
    {
        const string Out = "/tmp/flock-five-vip-name-gate-tests.txt";

        [MenuItem("Flock Five/VIP Name Gate Tests")]
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            int pass = 0, fail = 0;
            void Check(string name, bool ok)
            {
                if (ok) pass++; else fail++;
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[vipgate] " + (ok ? "PASS " : "FAIL ") + name);
            }
            float Goal(bool owned, int next, int flag, string name) =>
                FlockFiveApp.VipRailGoalFor(owned, next, !FlockFiveApp.NeedsNameFor(flag, name));

            // Fresh install, level 1 just cleared (streak award): no name, not adopted yet.
            Check("fresh-after-level1-hidden", Goal(false, 1, -1, "") == 0f);
            // Adoption in flight: avatar revealed, suggested name saved but named flag still 0.
            Check("adopt-suggested-name-hidden", Goal(false, 1, 0, "Goldie") == 0f);
            // Name picked (Done on the page).
            Check("named-shown", Goal(false, 1, 1, "Ruby") == 1f);
            // Older save: no flag, already has a name.
            Check("legacy-named-shown", Goal(false, 2, -1, "Pip") == 1f);
            // Before level 1 is cleared, named or not, it is still hidden (unchanged rule).
            Check("pre-level1-hidden", Goal(false, 0, 1, "Ruby") == 0f);
            // Owner with a name keeps the slot, even before level 1.
            Check("owner-named-shown", Goal(true, 0, 1, "Ruby") == 1f);
            // Owner still mid name picker waits for the name.
            Check("owner-unnamed-hidden", Goal(true, 1, 0, "Goldie") == 0f);
            Check("needs-name-flag1", !FlockFiveApp.NeedsNameFor(1, ""));
            Check("needs-name-flag0", FlockFiveApp.NeedsNameFor(0, "Ruby"));
            Check("needs-name-missing-empty", FlockFiveApp.NeedsNameFor(-1, null));

            sb.AppendLine("DONE pass=" + pass + " fail=" + fail);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[vipgate] DONE pass=" + pass + " fail=" + fail);
        }
    }
}
#endif
