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
                bool mixed = SexKinds(board) >= 2;
                bool split = ColorSplit(board);
                var result = GardenSolve.Search(board, NodeCap);
                int tangle = RandomTangles(i, Trials);
                int parkFail = FuzzPestPark(i, ParkTrials);
                bool ok = result.Outlook == GardenSolve.Outlook.Winnable && mixed && !split && parkFail == 0;
                if (!ok) all = false;
                sb.Append(level.Number).Append(' ').Append(level.Id);
                sb.Append(ok ? " OK" : " FAIL");
                sb.Append(" outlook=").Append(result.Outlook);
                sb.Append(" moves=").Append(result.Moves);
                sb.Append(" mixed=").Append(mixed);
                sb.Append(" split=").Append(split);
                sb.Append(" random-tangle=").Append(tangle).Append('/').Append(Trials);
                sb.Append(" pest-park-fail=").Append(parkFail);
                sb.Append(" | ").Append(flocks);
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
                        var flock = b.Branches[c].Birds.ToArray();
                        b.ApplyCollect(c);
                        if (!ParkFlockSafe(b, flock, c)) fails++;
                        continue;
                    }
                    if (!RandomHop(b)) break;
                    GardenSolve.Drain(b);
                }
            }
            return fails;
        }

        static bool ParkFlockSafe(Board b, Bird[] flock, int broken)
        {
            if (flock == null) return true;
            var parkedOnto = new HashSet<int>();
            for (int i = 0; i < flock.Length; i++)
            {
                int home = FindPark(b, broken, flock[i]);
                if (home < 0) continue;
                var st = b.Branches[home];
                if (st.Free <= 0) return false;
                if (WouldFullMatchAfterAdd(st, flock[i])) return false;
                st.Birds.Add(flock[i]);
                st.AlignShroud();
                parkedOnto.Add(home);
                if (st.Count > BranchState.Cap) return false;
                if (st.IsFullMatch(out _)) return false;
            }
            int collect = b.FindCollect();
            if (collect >= 0 && parkedOnto.Contains(collect))
                return false;
            return true;
        }

        static int FindPark(Board b, int broken, Bird bird)
        {
            int best = -1;
            int bestFree = -1;
            for (int i = 0; i < b.Branches.Count; i++)
            {
                if (i == broken) continue;
                var st = b.Branches[i];
                if (st.Broken || st.AdLocked || st.Free <= 0) continue;
                if (b.IsSleeping(i)) continue;
                if (WouldFullMatchAfterAdd(st, bird)) continue;
                if (st.Free > bestFree)
                {
                    bestFree = st.Free;
                    best = i;
                }
            }
            return best;
        }

        static bool WouldFullMatchAfterAdd(BranchState st, Bird bird)
        {
            if (st == null || st.Broken) return false;
            if (st.Count + 1 != BranchState.Cap) return false;
            for (int i = 0; i < st.Count; i++)
            {
                if (st.IsShrouded(i)) return false;
                if (st.Birds[i].Color != bird.Color) return false;
            }
            return true;
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

        static int SexKinds(Board b)
        {
            bool f = false, m = false, n = false;
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var br = b.Branches[i];
                for (int k = 0; k < br.Birds.Count; k++)
                {
                    var s = br.Birds[k].Sex;
                    if (s == BirdSex.Female) f = true;
                    else if (s == BirdSex.Male) m = true;
                    else n = true;
                }
            }
            int kinds = 0;
            if (f) kinds++;
            if (m) kinds++;
            if (n) kinds++;
            return kinds;
        }

        static bool ColorSplit(Board b)
        {
            var sex = new int[Palette.Max];
            var have = new bool[Palette.Max];
            for (int i = 0; i < sex.Length; i++) sex[i] = -1;
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var br = b.Branches[i];
                for (int k = 0; k < br.Birds.Count; k++)
                {
                    int c = (int)br.Birds[k].Color;
                    if ((uint)c >= (uint)sex.Length) continue;
                    int s = (int)br.Birds[k].Sex;
                    if (!have[c]) { have[c] = true; sex[c] = s; }
                    else if (sex[c] != s) return true;
                }
            }
            return false;
        }

        static string FlockSummary(Board b)
        {
            var n = new int[Palette.Max * 3];
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var br = b.Branches[i];
                for (int k = 0; k < br.Birds.Count; k++)
                {
                    var bird = br.Birds[k];
                    n[(int)bird.Color * 3 + (int)bird.Sex]++;
                }
            }
            var sb = new StringBuilder();
            string[] cn = { "R", "G", "T", "V", "P" };
            string[] sn = { "N", "F", "M" };
            for (int c = 0; c < Palette.Max; c++)
            {
                for (int s = 0; s < 3; s++)
                {
                    int k = n[c * 3 + s];
                    if (k == 0) continue;
                    if (sb.Length > 0) sb.Append(' ');
                    sb.Append(cn[c]).Append(sn[s]).Append(k);
                }
            }
            return sb.ToString();
        }
    }
}
#endif
