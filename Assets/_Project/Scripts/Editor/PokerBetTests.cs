#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    [InitializeOnLoad]
    static class PokerBetTests
    {
        const string Cmd = "/tmp/flock-five-poker-bet-tests";
        const string Out = "/tmp/flock-five-poker-bet-tests.txt";

        static PokerBetTests()
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

        [MenuItem("Flock Five/Poker Bet Tests")]
        public static void Run()
        {
            var sb = new StringBuilder();
            int pass = 0;
            int fail = 0;
            int keep = 0;
            void Line(string t)
            {
                sb.AppendLine(t);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[poker-bet] " + t);
            }
            void Check(string name, bool ok, string detail)
            {
                if (ok) { pass++; Line("PASS  " + name + "  " + detail); }
                else { fail++; Line("FAIL  " + name + "  " + detail); }
            }

            Purse.Boot();
            BirdPoker.Boot();
            keep = Purse.Coins;
            try
            {
                Line("poker bet tests start coins=" + keep);

                SetCoins(1000);
                BirdPoker.BeginVisit();
                Check("min-1000", BirdPoker.MinBet == 5,
                    "min=" + BirdPoker.MinBet);
                Check("max-1000", BirdPoker.MaxBet == 100,
                    "max=" + BirdPoker.MaxBet + " (10% of 1000)");
                Check("rec-1000", BirdPoker.RecBet == 25,
                    "rec=" + BirdPoker.RecBet + " (2% of 1000 = 20, nearest 25)");
                Check("seed-rec", BirdPoker.Bet == BirdPoker.RecBet,
                    "bet=" + BirdPoker.Bet);
                Check("no-allin-1000", BirdPoker.MaxBet < 1000 && BirdPoker.MaxBet == 100,
                    "max=" + BirdPoker.MaxBet);
                Check("can-deal-1000", BirdPoker.CanDeal(),
                    "coins=" + Purse.Coins + " bet=" + BirdPoker.Bet);
                Check("nudge-down-to-min", NudgeUntilStuck(-1) == 5,
                    "landed=" + BirdPoker.Bet);
                Check("nudge-up-to-max", NudgeUntilStuck(1) == 100,
                    "landed=" + BirdPoker.Bet);

                SetCoins(100);
                BirdPoker.BeginVisit();
                Check("max-100", BirdPoker.MaxBet == 10,
                    "max=" + BirdPoker.MaxBet);
                Check("rec-100", BirdPoker.RecBet == 5,
                    "rec=" + BirdPoker.RecBet + " (2%=2 clamped to $5)");
                Check("min-100", BirdPoker.MinBet == 5, "min=" + BirdPoker.MinBet);

                SetCoins(51);
                BirdPoker.BeginVisit();
                Check("max-51-chip", BirdPoker.MaxBet == 10,
                    "max=" + BirdPoker.MaxBet + " (ceil 10% of 51 = 6, up to 10)");
                Check("min-51", BirdPoker.MinBet == 5, "min=" + BirdPoker.MinBet);

                SetCoins(47);
                BirdPoker.BeginVisit();
                Check("max-47", BirdPoker.MaxBet == 5,
                    "max=" + BirdPoker.MaxBet + " (ceil 10% of 47 = 5)");

                SetCoins(4);
                BirdPoker.BeginVisit();
                Check("broke-no-deal", !BirdPoker.CanDeal(),
                    "coins=" + Purse.Coins + " can=" + BirdPoker.CanDeal());
                Check("broke-no-nudge", !BirdPoker.CanNudge(1) && !BirdPoker.CanNudge(-1),
                    "plus=" + BirdPoker.CanNudge(1) + " minus=" + BirdPoker.CanNudge(-1));

                SetCoins(123);
                BirdPoker.BeginVisit();
                Check("max-123-chip", BirdPoker.MaxBet == 25 && BirdPoker.MaxBet != 123,
                    "max=" + BirdPoker.MaxBet + " (ceil 10% of 123 = 13, up to 25)");

                SetCoins(2320000);
                BirdPoker.BeginVisit();
                Check("max-2_32m", BirdPoker.MaxBet == 250000,
                    "max=" + BirdPoker.MaxBet + " (10% = 232K, up to 250K)");
                Check("rec-2_32m", BirdPoker.RecBet == 50000,
                    "rec=" + BirdPoker.RecBet + " (2% of 2.32M = 46400, nearest 50K)");
                Check("min-2_32m", BirdPoker.MinBet == 5, "min=" + BirdPoker.MinBet);

                SetCoins(2501);
                BirdPoker.BeginVisit();
                Check("max-2501-chip", BirdPoker.MaxBet == 500,
                    "max=" + BirdPoker.MaxBet + " (10% = 251, up to 500)");
                SetCoins(1001);
                BirdPoker.BeginVisit();
                Check("max-1001-chip", BirdPoker.MaxBet == 250,
                    "max=" + BirdPoker.MaxBet + " (10% = 101, up to 250)");

                // Session floor: max only rises while visiting.
                SetCoins(1000);
                BirdPoker.BeginVisit();
                int startMax = BirdPoker.MaxBet;
                SetCoins(2000);
                BirdPoker.RefreshBet();
                Check("session-rise", BirdPoker.MaxBet == 250 && startMax == 100,
                    "start=" + startMax + " after-credit=" + BirdPoker.MaxBet);
                SetCoins(500);
                BirdPoker.RefreshBet();
                Check("session-no-drop", BirdPoker.MaxBet == 250,
                    "max=" + BirdPoker.MaxBet + " (session floor 250, coins 500)");
                Check("session-cannot-over-coins", BirdPoker.MaxBet <= Purse.Coins,
                    "max=" + BirdPoker.MaxBet + " coins=" + Purse.Coins);

                SetCoins(50);
                BirdPoker.RefreshBet();
                Check("session-cap-by-purse", BirdPoker.MaxBet == 50,
                    "max=" + BirdPoker.MaxBet + " (session 200 but only $50 left)");

                // Revisit recalculates a lower max.
                BirdPoker.BeginVisit();
                Check("revisit-lowers", BirdPoker.MaxBet == 5,
                    "max=" + BirdPoker.MaxBet + " (10% of 50)");

                // Recalc after a paying hand (winnings inflate balance).
                SetCoins(1000);
                BirdPoker.BeginVisit();
                Check("pre-hand-max", BirdPoker.MaxBet == 100, "max=" + BirdPoker.MaxBet);
                int rec = BirdPoker.Bet;
                if (!BirdPoker.Deal())
                    Check("deal-for-pay", false, "deal failed coins=" + Purse.Coins + " bet=" + rec);
                else
                {
                    Check("after-deal-spend", Purse.Coins == 1000 - rec,
                        "coins=" + Purse.Coins + " bet=" + rec);
                    BirdPoker.RefreshBet();
                    Check("after-deal-session-holds", BirdPoker.SessionMax >= 100,
                        "session=" + BirdPoker.SessionMax);
                    BirdPoker.ResetRound();
                }
                SetCoins(2500);
                BirdPoker.RefreshBet();
                Check("after-win-max-rises", BirdPoker.MaxBet == 250,
                    "max=" + BirdPoker.MaxBet + " coins=" + Purse.Coins);

                // Stepper stays on the chip ladder between min and max.
                SetCoins(1000);
                BirdPoker.BeginVisit();
                bool ladderOk = true;
                int prev = -1;
                int steps = 0;
                BirdPoker.NudgeBet(-1);
                while (BirdPoker.CanNudge(-1) && steps < 40)
                {
                    BirdPoker.NudgeBet(-1);
                    steps++;
                }
                do
                {
                    int b = BirdPoker.Bet;
                    if (b < 5 || b > 100) ladderOk = false;
                    if (b <= prev) ladderOk = false;
                    prev = b;
                    steps++;
                } while (BirdPoker.NudgeBet(1) && steps < 80);
                Check("ladder-bounds", ladderOk && prev == 100,
                    "last=" + prev + " steps=" + steps + " ok=" + ladderOk);
            }
            finally
            {
                SetCoins(keep);
                BirdPoker.BeginVisit();
                BirdPoker.ResetRound();
            }

            Line(fail == 0
                ? "poker bet tests DONE " + pass + "/" + (pass + fail)
                : "poker bet tests FAIL " + fail + " failed, " + pass + " passed");
        }

        static int NudgeUntilStuck(int dir)
        {
            int guard = 0;
            while (BirdPoker.CanNudge(dir) && guard++ < 40)
                BirdPoker.NudgeBet(dir);
            return BirdPoker.Bet;
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

