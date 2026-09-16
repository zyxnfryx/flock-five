#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    [InitializeOnLoad]
    static class PokerFuzz
    {
        const string Cmd = "/tmp/flock-five-poker-fuzz";
        const string Out = "/tmp/flock-five-poker-seeds.txt";
        const string Log = "/tmp/flock-five-poker-fuzz.log";
        const int SeedCap = 120000;

        static PokerFuzz()
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

        [MenuItem("Flock Five/Fuzz Poker Five-of-a-Kind")]
        public static void Run()
        {
            var sb = new StringBuilder();
            void Line(string t)
            {
                sb.AppendLine(t);
                File.WriteAllText(Log, sb.ToString());
                Debug.Log("[poker-fuzz] " + t);
            }

            Purse.Boot();
            BirdPoker.Boot();
            if (Purse.Coins < 200000) Purse.Credit(200000 - Purse.Coins);

            var seeds = new int[BirdPoker.PunchKinds];
            var ranks = new string[BirdPoker.PunchKinds];
            int found = 0;
            Line("fuzz start kinds=" + BirdPoker.PunchKinds + " cap=" + SeedCap);

            for (int kind = 0; kind < BirdPoker.PunchKinds; kind++)
            {
                seeds[kind] = -1;
                string who = BirdPoker.IsWildKind(kind) ? "WILD" : "";
                if (!BirdPoker.IsWildKind(kind))
                {
                    BirdColor c;
                    BirdSex sx;
                    BirdPoker.KindParts(kind, out c, out sx);
                    who = c + "/" + sx;
                }
                for (int seed = 1; seed <= SeedCap; seed++)
                {
                    BirdPoker.ClearPunches();
                    BirdPoker.ResetRound();
                    BirdPoker.SeedRng(seed);
                    if (!BirdPoker.Deal()) continue;
                    BirdPoker.HoldForKind(kind);
                    BirdPoker.Draw();
                    if (!BirdPoker.LastPunchFresh || BirdPoker.LastPunchKind != kind) continue;
                    seeds[kind] = seed;
                    ranks[kind] = BirdPoker.RankLabel(BirdPoker.LastRank);
                    found++;
                    Line("kind " + kind + " " + who + " seed=" + seed + " " + ranks[kind] + " win=" + BirdPoker.LastWin);
                    break;
                }
                if (seeds[kind] < 0)
                    Line("FAIL kind " + kind + " " + who + " no seed in 1.." + SeedCap);
            }

            int bingoKind = 0;
            int bingoSeed = -1;
            for (int kind = 0; kind < BirdPoker.PunchKinds; kind++)
            {
                if (seeds[kind] < 0) continue;
                BirdPoker.FillPunchesExcept(kind);
                BirdPoker.ResetRound();
                BirdPoker.SeedRng(seeds[kind]);
                if (!BirdPoker.Deal()) continue;
                BirdPoker.HoldForKind(kind);
                BirdPoker.Draw();
                if (BirdPoker.LastPunchBingo && BirdPoker.LastPunchKind == kind)
                {
                    bingoKind = kind;
                    bingoSeed = seeds[kind];
                    Line("bingo kind " + kind + " seed=" + bingoSeed);
                    break;
                }
            }

            var file = new StringBuilder();
            for (int k = 0; k < seeds.Length; k++)
            {
                if (seeds[k] < 0) continue;
                file.Append(k).Append(' ').Append(seeds[k]).Append(' ').Append(ranks[k]).Append('\n');
            }
            if (bingoSeed >= 0)
                file.Append("bingo ").Append(bingoKind).Append(' ').Append(bingoSeed).Append('\n');
            File.WriteAllText(Out, file.ToString());
            Line("wrote " + Out + " found=" + found + "/" + BirdPoker.PunchKinds + " bingo=" + (bingoSeed >= 0));
            Line(found == BirdPoker.PunchKinds ? "poker fuzz DONE" : "poker fuzz PARTIAL");
        }
    }
}
#endif
