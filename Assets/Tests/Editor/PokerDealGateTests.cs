#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // DEAL must come back after a settled hand. The TF 68 report was a one-wild
    // no-pay hand (pair, pay 0) after which DEAL ignored taps while bet +/- still
    // moved. That hand does not own a win or a stamp. The lock was the due-ad
    // branch: ready could never pass on the splash shell, so the tap was discarded
    // and the hand counter never reset.
    // Run from the menu, or drop a file on /tmp/flock-five-deal-gate-tests.
    [InitializeOnLoad]
    static class PokerDealGateTests
    {
        const string Cmd = "/tmp/flock-five-deal-gate-tests";
        const string Out = "/tmp/flock-five-deal-gate-tests.txt";

        static PokerDealGateTests()
        {
            EditorApplication.delayCall += Maybe;
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (File.Exists(Cmd)) Maybe();
        }

        static void Maybe()
        {
            if (!File.Exists(Cmd)) return;
            if (EditorApplication.isCompiling) return;
            try { File.Delete(Cmd); }
            catch { return; }
            Run();
        }

        [MenuItem("Flock Five/Poker Deal Gate Tests")]
        public static void Run()
        {
            var sb = new StringBuilder();
            int pass = 0;
            int fail = 0;
            void Line(string t)
            {
                sb.AppendLine(t);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[poker-deal] " + t);
            }
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++;
                else fail++;
                Line((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            Purse.Boot();
            BirdPoker.Boot();
            int keep = Purse.Coins;
            try
            {
                Line("poker deal gate tests start coins=" + keep);

                // The shell is not a ready input. Blockers all clear, on the table: showable.
                Check("ad-can-show-on-table",
                    PokerDealGate.AdCanShow(true, false, false, false, false, false, false, false, false, false),
                    "ready");
                Check("ad-hidden-off-table",
                    !PokerDealGate.AdCanShow(false, false, false, false, false, false, false, false, false, false),
                    "off table");

                // Brandon: ~5000 coins, bet 500, one wild, no pay. Ad due, not ready,
                // ad-running stuck, nothing on screen. DEAL must still start the next hand.
                SetCoins(5000);
                BirdPoker.BeginVisit();
                Check("reach-bet-500", NudgeTo(500),
                    "bet=" + BirdPoker.Bet + " max=" + BirdPoker.MaxBet);
                int before = Purse.Coins;
                Check("wild-loss-dealt", RigOneWildLoss(),
                    "phase=" + BirdPoker.PhaseNow + " coins=" + Purse.Coins);
                int wilds = 0;
                for (int i = 0; i < BirdPoker.HandSize; i++)
                    if (BirdPoker.Hand[i].Wild) wilds++;
                Check("wild-loss-one-wild",
                    wilds == 1 && BirdPoker.LastWin == 0
                    && BirdPoker.Multiplier(BirdPoker.LastRank) == 0
                    && BirdPoker.PhaseNow == BirdPoker.Phase.Drawn,
                    "wilds=" + wilds + " win=" + BirdPoker.LastWin
                    + " rank=" + BirdPoker.RankLabel(BirdPoker.LastRank)
                    + " phase=" + BirdPoker.PhaseNow);
                Check("wild-loss-still-rich",
                    Purse.Coins > 4000 && Purse.Coins == before - 500,
                    "coins=" + Purse.Coins + " before=" + before);

                var stuck = Settled(BirdPoker.Phase.Drawn, Purse.Coins);
                stuck.AdDue = true;
                stuck.AdReady = false;
                stuck.AdRunning = true;
                stuck.AdShowing = false;
                stuck.Ceremony = BirdPoker.LastWin > 0;
                var stuckTap = PokerDealGate.Tap(stuck);
                Check("wild-loss-deal-tap",
                    stuckTap.Act == PokerDealGate.Act.Deal && stuckTap.DropAdLock
                    && !stuckTap.DropWin,
                    "act=" + stuckTap.Act + " dropAd=" + stuckTap.DropAdLock
                    + " dropWin=" + stuckTap.DropWin + " ceremony=" + stuck.Ceremony);

                // Same hand, draw still playing: the tap waits, and does not clear the lock early.
                var mid = stuck;
                mid.MotionBusy = true;
                var midTap = PokerDealGate.Tap(mid);
                Check("wild-loss-waits-for-draw",
                    midTap.Act == PokerDealGate.Act.None && !midTap.DropAdLock,
                    "act=" + midTap.Act + " dropAd=" + midTap.DropAdLock);

                if (BirdPoker.PhaseNow == BirdPoker.Phase.Drawn) BirdPoker.Collect();
                BirdPoker.SyncBet();
                Check("wild-loss-can-deal",
                    BirdPoker.CanDeal() && BirdPoker.Bet == 500 && Purse.Coins >= BirdPoker.Bet,
                    "can=" + BirdPoker.CanDeal() + " bet=" + BirdPoker.Bet + " coins=" + Purse.Coins);
                int charged = Purse.Coins;
                bool redealt = BirdPoker.Deal();
                Check("wild-loss-deal-works",
                    redealt && BirdPoker.PhaseNow == BirdPoker.Phase.Dealt
                    && Purse.Coins == charged - BirdPoker.Bet,
                    "dealt=" + redealt + " phase=" + BirdPoker.PhaseNow
                    + " coins=" + Purse.Coins + " bet=" + BirdPoker.Bet);
                BirdPoker.ResetRound();

                // Due and actually showable: the tap shows the ad, it does not discard the press.
                var ad = Settled(BirdPoker.Phase.Idle, 5000);
                ad.AdDue = true;
                ad.AdReady = true;
                Check("due-and-ready-shows-ad",
                    PokerDealGate.Tap(ad).Act == PokerDealGate.Act.Ad,
                    "act=" + PokerDealGate.Tap(ad).Act);

                // A real win still flying is not a leftover. The tap leaves it alone.
                var flying = Settled(BirdPoker.Phase.Drawn, 5000);
                flying.WinFlying = true;
                flying.Ceremony = true;
                var flyTap = PokerDealGate.Tap(flying);
                Check("win-flight-blocks",
                    flyTap.Act == PokerDealGate.Act.None && !flyTap.DropWin,
                    "act=" + flyTap.Act + " dropWin=" + flyTap.DropWin);

                // Bet 250, then the loss leaves fewer than 250 coins. Step to the top rung he can pay.
                SetCoins(3000);
                BirdPoker.BeginVisit();
                Check("reach-bet-250", NudgeTo(250),
                    "bet=" + BirdPoker.Bet + " max=" + BirdPoker.MaxBet);
                SetCoins(400);
                Check("loss-under-250", RigOneWildLoss() && Purse.Coins == 150 && Purse.Coins < 250,
                    "coins=" + Purse.Coins + " bet=" + BirdPoker.Bet + " max=" + BirdPoker.MaxBet);
                Check("step-down-250",
                    BirdPoker.Bet < 250 && BirdPoker.Bet <= Purse.Coins
                    && BirdPoker.Bet == BirdPoker.MaxBet && BirdPoker.Bet >= BirdPoker.FloorBet,
                    "bet=" + BirdPoker.Bet + " max=" + BirdPoker.MaxBet + " coins=" + Purse.Coins);
                if (BirdPoker.PhaseNow == BirdPoker.Phase.Drawn) BirdPoker.Collect();
                BirdPoker.SyncBet();
                Check("step-down-can-deal",
                    BirdPoker.CanDeal() && BirdPoker.Bet == BirdPoker.MaxBet,
                    "can=" + BirdPoker.CanDeal() + " bet=" + BirdPoker.Bet);
                BirdPoker.ResetRound();

                // Last coins went in on the 250. Under $5: no silent dead button.
                SetCoins(3000);
                BirdPoker.BeginVisit();
                Check("reach-bet-250-broke", NudgeTo(250),
                    "bet=" + BirdPoker.Bet);
                SetCoins(254);
                Check("spent-to-broke", RigOneWildLoss() && Purse.Coins == 4,
                    "coins=" + Purse.Coins + " phase=" + BirdPoker.PhaseNow);
                var broke = Settled(BirdPoker.PhaseNow, Purse.Coins);
                broke.Ceremony = BirdPoker.LastWin > 0;
                var brokeTap = PokerDealGate.Tap(broke);
                Check("broke-hint",
                    Purse.Coins < BirdPoker.FloorBet
                    && PokerDealGate.ShowBrokeHint(Purse.Coins, BirdPoker.PhaseNow)
                    && brokeTap.Act == PokerDealGate.Act.Broke
                    && PokerDealGate.BrokeHint == "Not enough coins"
                    && !BirdPoker.CanDeal(),
                    "coins=" + Purse.Coins + " act=" + brokeTap.Act
                    + " hint=" + PokerDealGate.ShowBrokeHint(Purse.Coins, BirdPoker.PhaseNow)
                    + " can=" + BirdPoker.CanDeal());
                Check("broke-hint-hidden-while-dealt",
                    !PokerDealGate.ShowBrokeHint(4, BirdPoker.Phase.Dealt),
                    "dealt hint");
                Check("floor-has-no-hint",
                    !PokerDealGate.ShowBrokeHint(BirdPoker.FloorBet, BirdPoker.Phase.Idle),
                    "coins=" + BirdPoker.FloorBet);
                BirdPoker.ResetRound();
            }
            finally
            {
                SetCoins(keep);
                BirdPoker.BeginVisit();
                BirdPoker.ResetRound();
            }

            Line(fail == 0
                ? "poker deal gate tests DONE " + pass + "/" + (pass + fail)
                : "poker deal gate tests FAIL " + fail + " failed, " + pass + " passed");
        }

        // Quiet settled table: the result is up, nothing is playing.
        static PokerDealGate.Snap Settled(BirdPoker.Phase phase, int coins)
        {
            return new PokerDealGate.Snap
            {
                Phase = phase,
                Coins = coins,
                Ceremony = false
            };
        }

        // Deal, keep every card, overwrite with one wild and four unmatched colors, draw.
        // Pair (the wild) pays 0. Not a flush.
        static bool RigOneWildLoss()
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

        static bool NudgeTo(int want)
        {
            int guard = 0;
            while (BirdPoker.Bet < want && BirdPoker.NudgeBet(1) && guard++ < 40) { }
            guard = 0;
            while (BirdPoker.Bet > want && BirdPoker.NudgeBet(-1) && guard++ < 40) { }
            return BirdPoker.Bet == want;
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
