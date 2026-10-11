#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    [InitializeOnLoad]
    static class LevelSolve
    {
        const string Cmd = "/tmp/flock-five-solve";
        const string Out = "/tmp/flock-five-solve.txt";
        const int NodeCap = 250000;
        const int Trials = 48;
        const int ParkTrials = 64;

        static LevelSolve()
        {
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (!File.Exists(Cmd)) return;
            if (EditorApplication.isCompiling) return;
            try { File.Delete(Cmd); }
            catch { return; }
            Run();
        }

        [MenuItem("Flock Five/Solve Levels")]
        public static void Run()
        {
            var sb = new StringBuilder();
            bool all = true;
            for (int i = 0; i < LevelData.Count; i++)
            {
                var level = LevelData.Peek(i);
                var board = LevelData.Open(i);
                string flocks = FlockSummary(board);
                bool fakeHide = SameColorUnderBees(board);
                var result = GardenSolve.Search(board, NodeCap);
                int tangle = RandomTangles(i, Trials);
                int parkFail = FuzzPestPark(i, ParkTrials);
                var kitSets = BoardValidator.CheckKitSets(board);
                bool ok = result.Outlook == GardenSolve.Outlook.Winnable && kitSets.Ok && !fakeHide && parkFail == 0;
                if (!ok) all = false;
                sb.Append(level.Number).Append(' ').Append(level.Id);
                sb.Append(ok ? " OK" : " FAIL");
                sb.Append(" outlook=").Append(result.Outlook);
                sb.Append(" moves=").Append(result.Moves);
                sb.Append(" kit-sets=").Append(kitSets.Message);
                sb.Append(" fake-hide=").Append(fakeHide);
                sb.Append(" random-tangle=").Append(tangle).Append('/').Append(Trials);
                sb.Append(" pest-park-fail=").Append(parkFail);
                sb.Append(" pests=s").Append(LevelData.SparrowVisits).Append("/h").Append(LevelData.HawkVisits);
                sb.Append(" | ").Append(flocks);
                sb.Append(" sets=").Append(SetLine(board));
                sb.Append('\n');
                Debug.Log("Flock Five solve " + level.Number + " " + level.Id +
                    (ok ? " OK" : " FAIL") + " " + result.Outlook +
                    " tangle " + tangle + "/" + Trials +
                    " park " + parkFail);
            }
            sb.Append(all ? "ALL OK\n" : "SOME FAILED\n");
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("Flock Five solve wrote " + Out);
        }

        // How often a random hop order freezes the garden. Those rounds
        // must rewind, not lose.
        static int RandomTangles(int index, int trials)
        {
            int n = 0;
            for (int t = 0; t < trials; t++)
            {
                var b = LevelData.Open(index);
                GardenSolve.Drain(b);
                bool froze = false;
                for (int step = 0; step < 160; step++)
                {
                    if (b.Won) break;
                    if (!RandomHop(b))
                    {
                        if (!b.Won) froze = true;
                        break;
                    }
                    GardenSolve.Drain(b);
                }
                if (froze) n++;
            }
            return n;
        }

        // Pest scrap parks survivors onto remaining limbs. Must never fill a
        // same-color Cap (that auto-clears) or overflow a perch.
        static int FuzzPestPark(int index, int trials)
        {
            int fails = 0;
            for (int t = 0; t < trials; t++)
            {
                var b = LevelData.Open(index);
                GardenSolve.Drain(b);
                for (int step = 0; step < 100; step++)
                {
                    if (b.Won) break;
                    int c = b.FindCollect();
                    if (c >= 0)
                    {
                        var before = BoardValidator.Counts(b);
                        var live0 = b.Live[0];
                        var live1 = b.Live[1];
                        PestPark.Apply(b, c, hawk: false);
                        if (b.Live[0] != live0 || b.Live[1] != live1) fails++;
                        if (!BoardValidator.Same(BoardValidator.Counts(b), before)) fails++;
                        if (b.RemainingBirds > 0 && PestPark.FreeSeats(b) < 1) fails++;
                        var hawkBoard = b.Clone();
                        int hawkLimbs = LiveLimbs(hawkBoard);
                        int hawkCollect = hawkBoard.FindCollect();
                        if (hawkCollect >= 0)
                        {
                            var hawkCounts = BoardValidator.Counts(hawkBoard);
                            PestPark.Apply(hawkBoard, hawkCollect, hawk: true);
                            if (LiveLimbs(hawkBoard) < hawkLimbs) fails++;
                            if (!BoardValidator.Same(BoardValidator.Counts(hawkBoard), hawkCounts)) fails++;
                        }
                        continue;
                    }
                    if (!RandomHop(b)) break;
                    GardenSolve.Drain(b);
                }
            }
            return fails;
        }

        static int LiveLimbs(Board b)
        {
            if (b == null) return 0;
            int n = 0;
            for (int i = 0; i < b.Branches.Count; i++)
                if (b.Branches[i] != null && !b.Branches[i].Broken) n++;
            return n;
        }

        static bool RandomHop(Board b)
        {
            var hops = new List<(int from, int to)>(32);
            int n = b.Branches.Count;
            for (int from = 0; from < n; from++)
                for (int to = 0; to < n; to++)
                    if (b.CanMove(from, to, out int run) && run > 0)
                        hops.Add((from, to));
            if (hops.Count == 0) return false;
            var pick = hops[Random.Range(0, hops.Count)];
            return b.TryMove(pick.from, pick.to, out _);
        }

        static bool SameColorUnderBees(Board b)
        {
            if (b == null) return false;
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var br = b.Branches[i];
                for (int k = 0; k < br.Count - 1; k++)
                {
                    if (!br.IsShrouded(k)) continue;
                    for (int j = k + 1; j < br.Count; j++)
                    {
                        if (br.IsShrouded(j)) continue;
                        if (br.Birds[k].SameFlock(br.Birds[j])) return true;
                    }
                }
            }
            return false;
        }

        static string SetLine(Board b)
        {
            var sets = BoardValidator.SetsPerColor(b);
            string[] cn = { "R", "G", "T", "V", "P" };
            var sb = new StringBuilder();
            for (int c = 0; c < sets.Length && c < cn.Length; c++)
            {
                if (sets[c] == 0) continue;
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(cn[c]).Append(sets[c]);
            }
            return sb.ToString();
        }

        // One letter per kit that color wears: N bare, C crown, B bow.
        static string FlockSummary(Board b)
        {
            var n = new int[Palette.Max * Palette.Kits];
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var br = b.Branches[i];
                for (int k = 0; k < br.Birds.Count; k++)
                {
                    var bird = br.Birds[k];
                    int ix = (int)bird.Color * Palette.Kits + (int)bird.Kit;
                    if ((uint)ix < (uint)n.Length) n[ix]++;
                }
            }
            var sb = new StringBuilder();
            string[] cn = { "R", "G", "T", "V", "P" };
            string[] kn = { "N", "C", "B" };
            for (int c = 0; c < Palette.Max; c++)
            {
                int sets = 0;
                var letters = new StringBuilder();
                for (int k = 0; k < Palette.Kits; k++)
                {
                    if (n[c * Palette.Kits + k] == 0) continue;
                    sets++;
                    letters.Append(kn[k]);
                }
                if (sets == 0) continue;
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(cn[c]).Append(sets).Append('(').Append(letters).Append(')');
            }
            return sb.ToString();
        }
    }
}
#endif
