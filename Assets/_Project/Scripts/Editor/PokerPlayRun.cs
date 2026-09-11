#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    [InitializeOnLoad]
    static class PokerPlayRun
    {
        const string Cmd = "/tmp/flock-five-poker-run";
        const string Log = "/tmp/flock-five-poker-run.log";

        static PokerPlayRun()
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
            RunThreeHands();
        }

        [MenuItem("Flock Five/Poker Playrun x3")]
        public static void RunThreeHands()
        {
            var sb = new StringBuilder();
            void Line(string t)
            {
                sb.AppendLine(t);
                File.WriteAllText(Log, sb.ToString());
                Debug.Log("[poker-playrun] " + t);
            }
            Line("poker playrun start");
            Purse.Boot();
            BirdPoker.Boot();
            if (Purse.Coins < 40) Purse.Credit(40 - Purse.Coins);
            Line("coins " + Purse.Coins + " bet " + BirdPoker.Bet);
            int ok = 0;
            for (int round = 1; round <= 3; round++)
            {
                Line("--- hand " + round + " ---");
                if (BirdPoker.PhaseNow == BirdPoker.Phase.Drawn)
                    BirdPoker.Collect();
                if (BirdPoker.PhaseNow != BirdPoker.Phase.Idle)
                    BirdPoker.ResetRound();
                int before = Purse.Coins;
                if (!BirdPoker.Deal())
                {
                    Line("FAIL deal hand " + round + " coins " + Purse.Coins);
                    break;
                }
                Line("dealt " + Hand(BirdPoker.Hand) + " spent " + (before - Purse.Coins) + " phase " + BirdPoker.PhaseNow);
                if (BirdPoker.PhaseNow != BirdPoker.Phase.Dealt)
                {
                    Line("FAIL expected Dealt");
                    break;
                }
                BirdPoker.ToggleHold(0);
                BirdPoker.ToggleHold(2);
                if (!BirdPoker.Hold[0] || !BirdPoker.Hold[2] || BirdPoker.Hold[1])
                {
                    Line("FAIL hold flags " + BirdPoker.Hold[0] + BirdPoker.Hold[1] + BirdPoker.Hold[2]);
                    break;
                }
                var keep0 = BirdPoker.Hand[0];
                var keep2 = BirdPoker.Hand[2];
                if (!BirdPoker.Draw())
                {
                    Line("FAIL draw");
                    break;
                }
                bool kept = Same(keep0, BirdPoker.Hand[0]) && Same(keep2, BirdPoker.Hand[2]);
                Line("drawn " + Hand(BirdPoker.Hand) + " rank " + BirdPoker.RankLabel(BirdPoker.LastRank) + " win " + BirdPoker.LastWin + " coins " + Purse.Coins + " kept=" + kept);
                if (!kept)
                {
                    Line("FAIL held cards changed");
                    break;
                }
                if (BirdPoker.PhaseNow != BirdPoker.Phase.Drawn)
                {
                    Line("FAIL expected Drawn");
                    break;
                }
                BirdPoker.Collect();
                if (BirdPoker.PhaseNow != BirdPoker.Phase.Idle)
                {
                    Line("FAIL expected Idle after collect");
                    break;
                }
                Line("collect ok coins " + Purse.Coins);
                ok++;
            }
            Line(ok == 3 ? "poker playrun DONE 3/3" : "poker playrun FAIL " + ok + "/3");
        }

        static bool Same(BirdPoker.Card a, BirdPoker.Card b) =>
            a.Wild == b.Wild && a.Color == b.Color && a.Sex == b.Sex;

        static string Hand(BirdPoker.Card[] hand)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < hand.Length; i++)
            {
                if (i > 0) sb.Append(',');
                if (hand[i].Wild) sb.Append("WILD");
                else sb.Append(hand[i].Color).Append('/').Append(hand[i].Sex);
            }
            return sb.ToString();
        }
    }
}
#endif
