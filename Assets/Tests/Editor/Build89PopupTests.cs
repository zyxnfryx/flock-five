#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 89: popups never stack. One queue orders the post-stage sequence.
    public static class Build89PopupTests
    {
        const string Out = "/tmp/flock-five-b89-popup-tests.txt";

        static string Plan(PopupQueue.Step[] p)
        {
            var parts = new string[p.Length];
            for (int i = 0; i < p.Length; i++) parts[i] = p[i].ToString();
            return string.Join(">", parts);
        }

        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail)
            {
                string line = (ok ? "PASS  " : "FAIL  ") + name + "  " + detail;
                if (ok) pass++; else fail++;
                sb.AppendLine(line);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build89popup] " + line);
            }

            string p = Plan(PopupQueue.PostStagePlan(true, false, true, true));
            Check("collect: reward, then ad, then vip", p == "Reward>Interstitial>Vip", p);
            p = Plan(PopupQueue.PostStagePlan(true, true, true, true));
            Check("double replaces the interstitial", p == "Reward", p);
            p = Plan(PopupQueue.PostStagePlan(true, true, false, false));
            Check("double, cadence off: reward only", p == "Reward", p);
            p = Plan(PopupQueue.PostStagePlan(true, false, false, true));
            Check("collect, cadence not due: no ad, no vip ask", p == "Reward", p);
            p = Plan(PopupQueue.PostStagePlan(true, false, true, false));
            Check("collect, vip not due: reward then ad", p == "Reward>Interstitial", p);
            p = Plan(PopupQueue.PostStagePlan(false, false, true, true));
            Check("no reward popup: ad then vip", p == "Interstitial>Vip", p);

            Check("free when nothing open", PopupQueue.ScreenFree(false, false, false, false), "");
            Check("modal holds", !PopupQueue.ScreenFree(true, false, false, false), "streak/daily/cards");
            Check("owed reward holds", !PopupQueue.ScreenFree(false, true, false, false), "");
            Check("coin fly-in holds", !PopupQueue.ScreenFree(false, false, true, false), "");
            Check("ad holds", !PopupQueue.ScreenFree(false, false, false, true), "");

            PopupQueue.Reset();
            PopupQueue.RequestVip();
            Check("vip waits while busy", !PopupQueue.TryReleaseVip(false) && PopupQueue.VipWanted, "");
            Check("vip opens once free", PopupQueue.TryReleaseVip(true), "");
            Check("vip opens only once", !PopupQueue.TryReleaseVip(true), "");
            PopupQueue.RequestVip();
            PopupQueue.DropVip();
            Check("owned drops the request", !PopupQueue.TryReleaseVip(true), "");

            SparkleFx.StreakSparks(new Vector2(100f, 100f), 6);
            SparkleFx.Celebrate(new Vector2(120f, 140f));
            int before = SparkleFx.LiveBits();
            SparkleFx.ClearBits();
            Check("transient fx cleared", before > 0 && SparkleFx.LiveBits() == 0, "before=" + before);

            sb.AppendLine("pass=" + pass + " fail=" + fail);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[build89popup] pass=" + pass + " fail=" + fail);
        }
    }
}
#endif
