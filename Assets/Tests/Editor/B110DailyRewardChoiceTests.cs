#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // b110: the DAILY Watch Ad 2X / Collect choice uses the same shared offer layout as the streak REWARD choice.
    public static class B110DailyRewardChoiceTests
    {
        const string Out = "/tmp/flock-five-b110-daily-choice-tests.txt";

        [MenuItem("Flock Five/B110 Daily Reward Choice Tests")]
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            int fail = 0;
            void Check(string name, bool ok)
            {
                if (!ok) fail++;
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[b110] " + (ok ? "PASS " : "FAIL ") + name);
            }
            float s = 1.5f;
            var two = FlockFiveApp.OfferButtons(0f, 0f, 300f, s, true);
            var one = FlockFiveApp.OfferButtons(0f, 0f, 300f, s, false);
            Check("daily stack with ad equals shared secondary bottom", Mathf.Abs(two.Secondary.yMax - FlockFiveApp.DailyOfferStackH(s, true)) < 0.01f);
            Check("daily stack without ad equals shared primary bottom", Mathf.Abs(one.Primary.yMax - FlockFiveApp.DailyOfferStackH(s, false)) < 0.01f);
            Check("ad stack is taller than lone Collect", FlockFiveApp.DailyOfferStackH(s, true) > FlockFiveApp.DailyOfferStackH(s, false));
            Check("daily card stays seated between chrome 2 and 3",
                Mathf.Abs(FlockFiveApp.DailyAuditCard(s, 2).y - FlockFiveApp.DailyAuditCard(s, 3).y) < 0.01f);
            Debug.Log("[b110] done fails=" + fail);
        }
    }
}
#endif
