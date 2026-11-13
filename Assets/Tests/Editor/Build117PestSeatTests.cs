#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 117 (Melchior, build 115, levels 7-13). (A) After a sparrow or hawk scrap the five
    // birds that flew at the feeder came home spread over three to five limbs, one per limb.
    // (B) A finished set on a live feeder sat there with no collect after a scrap. Pure board
    // logic and source wiring, no scene. Registered in EnsurePlayable.RunEditorTests.
    public static class Build117PestSeatTests
    {
        const string Out = "/tmp/flock-five-b117-pest-seat-tests.txt";
        const BirdColor R = BirdColor.Ruby, G = BirdColor.Gold, T = BirdColor.Teal;

        static BranchState Limb(params BirdColor[] cols)
        {
            var st = new BranchState();
            foreach (var c in cols)
            {
                st.Birds.Add(new Bird(c));
                st.Shrouded.Add(false);
            }
            return st;
        }

        // Limb 0 holds the five Rubies the sparrow sits over (Ruby feeder live). Limbs 1-3 are
        // mixed Gold/Teal with 1-2 seats free, limb 4 is empty. 15 birds, five of each colour.
        static Board Scrap()
        {
            var b = new Board();
            b.Branches.Add(Limb(R, R, R, R, R));
            b.Branches.Add(Limb(G, T, G, T));
            b.Branches.Add(Limb(T, G, T));
            b.Branches.Add(Limb(G, T, G));
            b.Branches.Add(new BranchState());
            b.Live[0] = R;
            b.Live[1] = G;
            return b;
        }

        static int Limbs(PestPark.Plan plan)
        {
            var seen = new HashSet<int>();
            if (plan.Homes != null)
                for (int i = 0; i < plan.Homes.Length; i++) seen.Add(plan.Homes[i].Branch);
            return seen.Count;
        }

        static int MaxOnOneLimb(Board b, BirdColor c)
        {
            int best = 0;
            for (int i = 0; i < b.Branches.Count; i++)
            {
                if (b.Branches[i].Broken) continue;
                int n = 0;
                for (int k = 0; k < b.Branches[i].Count; k++)
                    if (b.Branches[i].Birds[k].Color == c) n++;
                if (n > best) best = n;
            }
            return best;
        }

        public static void Run()
        {
            var sb = new StringBuilder();
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail)
            {
                string line = (ok ? "PASS  " : "FAIL  ") + name + "  " + detail;
                if (ok) pass++; else fail++;
                sb.AppendLine(line);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build117-seat] " + line);
            }

            // --- (A) the flock comes home together ---
            {
                var b = Scrap();
                var expect = BoardValidator.Counts(b);
                var plan = PestPark.Apply(b, 0, hawk: false);
                Check("sparrow-flock-two-limbs", Limbs(plan) <= 2, "limbs " + Limbs(plan));
                Check("sparrow-four-together", MaxOnOneLimb(b, R) == 4, "most on one limb " + MaxOnOneLimb(b, R));
                Check("sparrow-counts", BoardValidator.Check(b, expect).Ok, BoardValidator.Check(b, expect).Message);
                Check("sparrow-no-auto-clear", b.FindCollect() < 0 && !SolvedRule.IsSolved(b), "collect " + b.FindCollect());
                Check("sparrow-all-seated", b.Displaced.Count == 0 && b.RemainingBirds == 15, "birds " + b.RemainingBirds);
            }
            {
                var b = Scrap();
                var plan = PestPark.Apply(b, 0, hawk: true);
                Check("hawk-flock-two-limbs", Limbs(plan) <= 2 && !b.Branches[0].Broken, "limbs " + Limbs(plan));
                Check("hawk-no-auto-clear", b.FindCollect() < 0, "collect " + b.FindCollect());
            }
            {
                // The path the game takes: Hold at the start of the scrap, Resolve after it.
                var b = Scrap();
                var expect = BoardValidator.Counts(b);
                PestPark.Hold(b, 0);
                Check("hold-keeps-count", b.Displaced.Count == 5 && BoardValidator.Check(b, expect).Ok, "displaced " + b.Displaced.Count);
                var plan = PestPark.Resolve(b, 0, hawk: false);
                Check("resolve-flock-two-limbs", Limbs(plan) <= 2, "limbs " + Limbs(plan));
                Check("resolve-homes-match-board", HomesMatchBoard(b, plan), "");
                Check("resolve-clears-displaced", b.Displaced.Count == 0 && BoardValidator.Check(b, expect).Ok, "");
            }

            // --- never a five-match from the park ---
            {
                // Limb 1 already holds three Rubies and has two seats free. The flock may add one
                // there (four) but never the bird that would make five.
                var b = new Board();
                b.Branches.Add(Limb(R, R, R, R, R));
                b.Branches.Add(Limb(R, R, R));
                b.Branches.Add(Limb(G, G));
                b.Branches.Add(new BranchState());
                b.Live[0] = R;
                var expect = BoardValidator.Counts(b);
                var plan = PestPark.Apply(b, 0, hawk: false);
                Check("no-five-from-park", b.FindCollect() < 0 && MaxOnOneLimb(b, R) <= 4, "most " + MaxOnOneLimb(b, R));
                Check("no-five-counts", BoardValidator.Check(b, expect).Ok, BoardValidator.Check(b, expect).Message);
            }

            // --- many boards: whenever a limb has four or more free seats the flock uses at most two limbs ---
            {
                var rng = new System.Random(117);
                int runs = 0, bad = 0, lost = 0, collect = 0, wide = 0;
                for (int seed = 0; seed < 300; seed++)
                {
                    var b = new Board();
                    b.Live[0] = R;
                    b.Live[1] = G;
                    int nb = 5 + rng.Next(3);
                    for (int i = 0; i < nb; i++) b.Branches.Add(new BranchState());
                    b.Branches[0] = Limb(R, R, R, R, R);
                    var pool = new List<Bird>();
                    for (int c = 1; c < 3; c++)
                        for (int i = 0; i < 5; i++) pool.Add(new Bird((BirdColor)c));
                    for (int i = pool.Count - 1; i > 0; i--)
                    {
                        int j = rng.Next(i + 1);
                        var t = pool[i]; pool[i] = pool[j]; pool[j] = t;
                    }
                    foreach (var bird in pool)
                    {
                        int to;
                        do to = 1 + rng.Next(nb - 1); while (b.Branches[to].Count >= BranchState.Cap);
                        b.Branches[to].Birds.Add(bird);
                        b.Branches[to].Shrouded.Add(false);
                    }
                    bool full = false;
                    for (int i = 1; i < nb; i++) if (b.Branches[i].IsFullMatch(out _)) full = true;
                    if (full) continue;
                    bool roomy = false;
                    for (int i = 1; i < nb; i++) if (b.Branches[i].Free >= 4) roomy = true;
                    var expect = BoardValidator.Counts(b);
                    var plan = PestPark.Apply(b, 0, hawk: seed % 2 == 0);
                    runs++;
                    if (roomy && Limbs(plan) > 2) wide++;
                    if (!BoardValidator.Check(b, expect).Ok || b.Displaced.Count != 0) lost++;
                    if (b.FindCollect() >= 0 || MaxOnOneLimb(b, R) > 4) collect++;
                    if (!HomesMatchBoard(b, plan)) bad++;
                }
                Check("random-boards-ran", runs > 100, "runs " + runs);
                Check("random-roomy-two-limbs", wide == 0, "wide " + wide + " of " + runs);
                Check("random-conserved", lost == 0, "lost " + lost);
                Check("random-no-auto-clear", collect == 0, "collect " + collect);
                Check("random-homes-match", bad == 0, "bad " + bad);
            }

            // --- (B) a finished set on a live feeder is picked up by the poll ---
            {
                var b = new Board();
                b.Branches.Add(Limb(R, R, R, R, R));
                b.Branches.Add(Limb(G, T));
                b.Live[0] = R;
                b.Live[1] = G;
                Check("poll-ready-when-idle", CollectPoll.Ready(b, true), "");
                Check("poll-not-ready-when-busy", !CollectPoll.Ready(b, false), "");
                b.Displaced.Add(new Bird(G));
                Check("poll-not-ready-with-flock-in-air", !CollectPoll.Ready(b, true), "");
                b.Displaced.Clear();
                var asleep = new Board();
                asleep.Branches.Add(Limb(R, R, R, R, R));
                asleep.Live[0] = G;
                asleep.Live[1] = T;
                Check("poll-not-ready-for-sleeping-set", !CollectPoll.Ready(asleep, true) && asleep.IsSleeping(0), "");

                float since = -1f;
                bool early = CollectPoll.Step(true, 10f, ref since);
                bool mid = CollectPoll.Step(true, 10f + CollectPoll.Hold * 0.5f, ref since);
                bool due = CollectPoll.Step(true, 10f + CollectPoll.Hold + 0.01f, ref since);
                bool again = CollectPoll.Step(true, 10f + CollectPoll.Hold + 0.02f, ref since);
                Check("poll-waits-then-fires-once", !early && !mid && due && !again, "early " + early + " mid " + mid + " due " + due + " again " + again);
                CollectPoll.Step(true, 20f, ref since);
                CollectPoll.Step(false, 20.3f, ref since);
                bool reset = CollectPoll.Step(true, 20.4f, ref since);
                Check("poll-resets-when-not-ready", !reset && since >= 0f, "since " + since);

                // Collecting the set through the poll's own path empties the limb and advances the feeder.
                b.Queue.Add(T);
                int at = b.FindCollect();
                b.ApplyCollect(at);
                Check("poll-kick-collects", at == 0 && b.Branches[0].Broken && b.Live[0] == T, "at " + at);
            }

            // --- wiring ---
            {
                string app = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs");
                string park = File.ReadAllText("Assets/_Project/Scripts/Puzzle/PestPark.cs");
                int ts = app.IndexOf("            TrySolvedFinish();\n            TryKickIdle();", System.StringComparison.Ordinal);
                Check("update-polls-kick", ts >= 0, "");
                int ki = app.IndexOf("bool KickIdle()", System.StringComparison.Ordinal);
                int kiEnd = ki >= 0 ? app.IndexOf("void TryKickIdle()", ki, System.StringComparison.Ordinal) : -1;
                string idle = ki >= 0 && kiEnd > ki ? app.Substring(ki, kiEnd - ki) : "";
                Check("kick-idle-guards", idle.Contains("_collecting") && idle.Contains("_locked.Count > 0")
                    && idle.Contains("_pestResolving") && idle.Contains("_pestFinishing") && idle.Contains("GamePause.Paused")
                    && idle.Contains("_frozen") && idle.Contains("_restarting"), "");
                Check("place-seats-runs", park.Contains("RunFits(") && !park.Contains("int home = Find(b, flock[i], allowFull);"), "");
            }

            Debug.Log(fail == 0 ? "build117-seat DONE " + pass + "/" + (pass + fail) : "build117-seat FAIL " + fail + " failed, " + pass + " passed");
            if (fail > 0) throw new System.Exception("BUILD117_SEAT_FAIL " + fail);
        }

        // Every plan home names the limb and seat the bird really sits on.
        static bool HomesMatchBoard(Board b, PestPark.Plan plan)
        {
            if (plan.Homes == null) return true;
            for (int i = 0; i < plan.Homes.Length; i++)
            {
                var h = plan.Homes[i];
                if ((uint)h.Branch >= (uint)b.Branches.Count) return false;
                var st = b.Branches[h.Branch];
                if (h.Seat < 0 || h.Seat >= st.Count) return false;
                if (!st.Birds[h.Seat].SameFlock(h.Bird)) return false;
            }
            return true;
        }
    }
}
#endif
