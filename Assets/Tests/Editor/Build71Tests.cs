#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 71. Hive overlay close, DEAL hidden while Drawn, GardenFit step cap.
    // The pest-timer pause lives in PestScheduleTests.
    [InitializeOnLoad]
    static class Build71Tests
    {
        const string Out = "/tmp/flock-five-build71-tests.txt";

        [MenuItem("Flock Five/Build 71 Tests")]
        public static void Run()
        {
            var sb = new StringBuilder();
            int pass = 0;
            int fail = 0;
            void Line(string t)
            {
                sb.AppendLine(t);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build71] " + t);
            }
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++;
                else fail++;
                Line((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            int inspect = 4;
            bool closing = true;
            float inspectT = 0.8f;
            int flip = 4;
            float flipT = 0.2f;
            bool album = true;
            HiveOverlay.Close(ref inspect, ref closing, ref inspectT, ref flip, ref flipT, ref album);
            Check("hive-close",
                inspect < 0 && !closing && inspectT == 0f && flip < 0 && flipT == 0f && !album,
                "inspect=" + inspect + " flip=" + flip + " album=" + album);

            Check("deal-hidden-drawn",
                !PokerDealGate.ActShown(BirdPoker.Phase.Drawn, false)
                && PokerDealGate.ActLabel(BirdPoker.Phase.Drawn, false) == null
                && PokerDealGate.ActLabel(BirdPoker.Phase.Drawn, true) == null,
                "Drawn stays hidden");
            Check("deal-shows-when-idle",
                PokerDealGate.ActShown(BirdPoker.Phase.Idle, true)
                && PokerDealGate.ActLabel(BirdPoker.Phase.Idle, true) == "DEAL",
                "Idle + CanDeal");
            Check("deal-hidden-broke",
                !PokerDealGate.ActShown(BirdPoker.Phase.Idle, false)
                && PokerDealGate.ActLabel(BirdPoker.Phase.Idle, false) == null,
                "CanDeal false");
            Check("draw-while-dealt",
                PokerDealGate.ActShown(BirdPoker.Phase.Dealt, false)
                && PokerDealGate.ActLabel(BirdPoker.Phase.Dealt, false) == "DRAW",
                "Dealt is DRAW");

            float cap = GardenFit.EaseStepCap;
            Check("fit-cap-hitch",
                Mathf.Abs(GardenFit.EaseDelta(0.5f) - cap) < 0.0001f
                && GardenFit.EaseDelta(0.5f) <= cap,
                "dt=0.5 -> " + GardenFit.EaseDelta(0.5f));
            Check("fit-cap-frame",
                Mathf.Abs(GardenFit.EaseDelta(0.01f) - 0.01f) < 0.0001f
                && GardenFit.EaseDelta(0f) == 0f
                && GardenFit.EaseDelta(-1f) == 0f,
                "small dt passes");
            float step = GardenFit.EaseDelta(0.5f) / 0.62f;
            float raw = 0.5f / 0.62f;
            Check("fit-cap-under-two-rows",
                step < 0.2f && step < raw * 0.5f && step * 3f < 1.5f,
                "uStep=" + step.ToString("0.000") + " raw=" + raw.ToString("0.000"));

            Purse.Boot();
            BirdPoker.Boot();
            int keep = Purse.Coins;
            try
            {
                SetCoins(400);
                BirdPoker.BeginVisit();
                bool drawn = RigSettledHand();
                var snap = new BirdPoker.Card[BirdPoker.HandSize];
                int holds = 0;
                for (int i = 0; i < BirdPoker.HandSize; i++)
                {
                    snap[i] = BirdPoker.Hand[i];
                    if (BirdPoker.Hold[i]) holds++;
                }
                int win = BirdPoker.LastWin;
                var rank = BirdPoker.LastRank;
                BirdPoker.Settle();
                bool same = drawn && holds > 0;
                int holdsLeft = 0;
                for (int i = 0; i < BirdPoker.HandSize; i++)
                {
                    var c = BirdPoker.Hand[i];
                    if (c.Wild != snap[i].Wild || c.Color != snap[i].Color || c.Sex != snap[i].Sex)
                        same = false;
                    if (BirdPoker.Hold[i]) holdsLeft++;
                }
                string label = PokerDealGate.ActLabel(BirdPoker.PhaseNow, BirdPoker.CanDeal());
                Check("settle-result-stays",
                    BirdPoker.CanDeal()
                    && BirdPoker.PhaseNow == BirdPoker.Phase.Idle
                    && same
                    && holdsLeft == 0
                    && BirdPoker.LastWin == win
                    && BirdPoker.LastRank == rank
                    && BirdPoker.ResultOnTable
                    && label == "DEAL",
                    "can=" + BirdPoker.CanDeal()
                    + " phase=" + BirdPoker.PhaseNow
                    + " same=" + same
                    + " holds=" + holds + "->" + holdsLeft
                    + " win=" + BirdPoker.LastWin
                    + " rank=" + BirdPoker.LastRank
                    + " onTable=" + BirdPoker.ResultOnTable
                    + " label=" + label);
                bool redealt = BirdPoker.Deal();
                Check("deal-clears-result",
                    redealt && !BirdPoker.ResultOnTable && BirdPoker.PhaseNow == BirdPoker.Phase.Dealt,
                    "dealt=" + redealt
                    + " onTable=" + BirdPoker.ResultOnTable
                    + " phase=" + BirdPoker.PhaseNow);
            }
            finally
            {
                SetCoins(keep);
                BirdPoker.BeginVisit();
            }

            Line(fail == 0
                ? "build71 tests DONE " + pass + "/" + (pass + fail)
                : "build71 tests FAIL " + fail + " failed, " + pass + " passed");
        }

        // One wild and four different colors: a pair, pay 0, no punch. Holds are set
        // so Settle has something to clear while the cards stay.
        static bool RigSettledHand()
        {
            if (BirdPoker.PhaseNow != BirdPoker.Phase.Idle) BirdPoker.ResetRound();
            if (!BirdPoker.Deal()) return false;
            for (int i = 0; i < BirdPoker.HandSize; i++)
                if (!BirdPoker.Hold[i]) BirdPoker.ToggleHold(i);
            BirdPoker.Hand[0] = BirdPoker.Card.MakeWild();
            BirdPoker.Hand[1] = BirdPoker.Card.Of(BirdColor.Ruby, BirdSex.Neutral);
            BirdPoker.Hand[2] = BirdPoker.Card.Of(BirdColor.Gold, BirdSex.Neutral);
            BirdPoker.Hand[3] = BirdPoker.Card.Of(BirdColor.Teal, BirdSex.Neutral);
            BirdPoker.Hand[4] = BirdPoker.Card.Of(BirdColor.Violet, BirdSex.Neutral);
            return BirdPoker.Draw();
        }

        static void SetCoins(int n)
        {
            n = Mathf.Max(0, n);
            int c = Purse.Coins;
            if (c < n) Purse.Credit(n - c);
            else if (c > n) Purse.TrySpend(c - n);
        }
    }
}
#endif
