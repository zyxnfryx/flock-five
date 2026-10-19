#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 110 ostrich peck. Board math the staged fight depends on, and the strike timeline.
    // Not run on device. Registered in EnsurePlayable.
    public static class Build110OstrichPeckTests
    {
        const string Out = "/tmp/flock-five-b110-peck-tests.txt";

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
                Debug.Log("[build110-peck] " + line);
            }

            // --- strike timeline: wind-up back, strike, contact, recoil, settle, never a pop ---
            Check("strike-contact-before-total", OstrichStrike.Contact > OstrichStrike.Wind
                && OstrichStrike.Total > OstrichStrike.Contact + 0.3f, "contact " + OstrichStrike.Contact + " total " + OstrichStrike.Total);
            Check("strike-reach-keys",
                Mathf.Abs(OstrichStrike.Reach(0f)) < 1e-4f
                && OstrichStrike.Reach(OstrichStrike.Wind) < -0.99f
                && Mathf.Abs(OstrichStrike.Reach(OstrichStrike.Contact) - 1f) < 1e-3f
                && Mathf.Abs(OstrichStrike.Reach(OstrichStrike.Contact + OstrichStrike.Hold * 0.5f) - 1f) < 1e-4f
                && Mathf.Abs(OstrichStrike.Reach(OstrichStrike.Total)) < 1e-3f, "");
            float maxStep = 0f;
            float prev = OstrichStrike.Reach(0f);
            for (float t = 1f / 240f; t <= OstrichStrike.Total + 0.1f; t += 1f / 240f)
            {
                float r = OstrichStrike.Reach(t);
                maxStep = Mathf.Max(maxStep, Mathf.Abs(r - prev));
                prev = r;
            }
            Check("strike-continuous", maxStep < 0.25f, "max step per 1/240s " + maxStep.ToString("0.000"));
            var rest = new Vector3(0f, 4f, 0f);
            var target = new Vector3(2f, 0f, 0f);
            float dRest = Vector3.Distance(rest, target);
            float dWind = Vector3.Distance(OstrichStrike.HeadAt(rest, target, OstrichStrike.Wind), target);
            bool atBird = (OstrichStrike.HeadAt(rest, target, OstrichStrike.Contact) - target).sqrMagnitude < 1e-4f;
            bool back = (OstrichStrike.HeadAt(rest, target, OstrichStrike.Total) - rest).sqrMagnitude < 1e-4f;
            Check("strike-head-path", dWind > dRest + 0.3f && atBird && back,
                "wind " + dWind.ToString("0.00") + " rest " + dRest.ToString("0.00"));
            Check("strike-head-above-birds", OstrichStrike.HeadOrder > FlockSort.Perch && OstrichStrike.HeadOrder > FlockSort.Lift
                && OstrichStrike.NeckOrder > OstrichMark.NeckOrder, "");

            // --- pecks relocate a bird to a different perch; no auto-clear ---
            var b = PeckBoard(Mixed(), 7);
            var before = b.Clone();
            var log = new List<PeckHit>();
            GardenSolve.AfterMove(b, log, out int finale);
            bool diff = log.Count > 0;
            bool replay = true;
            var sim = before.Clone();
            for (int i = 0; i < log.Count; i++)
            {
                var h = log[i];
                diff &= h.From != h.To;
                if (sim.Branches[h.From].Count != h.FromCount || sim.Branches[h.To].Count != h.ToCount) { replay = false; break; }
                sim.Branches[h.From].Birds.RemoveAt(h.FromCount - 1);
                sim.Branches[h.To].Birds.Add(h.Bird);
            }
            Check("pecks-logged-different-perches", diff && finale == -1 && b.Ostrich.Alive, "pecks " + log.Count);
            bool same = replay;
            for (int i = 0; same && i < b.Branches.Count; i++)
                same = b.Branches[i].Count == sim.Branches[i].Count;
            Check("pecks-replay-matches-board", same, "");
            bool noClear = true;
            for (int i = 0; i < b.Branches.Count; i++) noClear &= !b.Branches[i].Broken;
            Check("pecks-no-autoclear", noClear && b.Ostrich.Alive && Total(b) == Total(before), "birds " + Total(b));

            int bad = 0;
            for (int seed = 0; seed < 400; seed++)
            {
                var f = PeckBoard(Mixed(), seed);
                var pre = f.Clone();
                var l = new List<PeckHit>();
                OstrichPest.AfterMove(f, l, out _);
                var s2 = pre.Clone();
                foreach (var h in l)
                {
                    if (h.From == h.To || s2.Branches[h.From].Count != h.FromCount || s2.Branches[h.To].Count != h.ToCount) { bad++; break; }
                    s2.Branches[h.From].Birds.RemoveAt(h.FromCount - 1);
                    s2.Branches[h.To].Birds.Add(h.Bird);
                }
                for (int i = 0; i < f.Branches.Count; i++)
                    if (f.Branches[i].Broken || f.Branches[i].IsFullMatch(out _) && f.Ostrich.Alive && f.Ostrich.Hits == 0) { bad++; break; }
            }
            Check("pecks-fuzz-replay-no-clear", bad == 0, "bad " + bad);

            // --- 2 cleared sets: the fight beat still plays before the ostrich leaves ---
            // Hits already 1, two sets complete in the same move: both bonk, third hit flees.
            var two = TwoSets(1);
            var l2 = new List<PeckHit>();
            GardenSolve.AfterMove(two, l2, out int fin2);
            Check("two-sets-flee-has-finale",
                !two.Ostrich.Alive && l2.Count == 0 && fin2 >= 0 && fin2 == two.Ostrich.LastBonkBranch
                && two.Branches[0].Broken && two.Branches[1].Broken,
                "finale " + fin2 + " pecks " + l2.Count);
            // Fresh ostrich, same two sets: it takes two hits, stays, and (b116) does not peck on that move. Sets are not cleared.
            var fresh = TwoSets(0);
            var l3 = new List<PeckHit>();
            GardenSolve.AfterMove(fresh, l3, out int fin3);
            Check("two-sets-fresh-stays-and-pecks",
                fresh.Ostrich.Alive && fresh.Ostrich.Hits == 2 && fin3 == -1 && l3.Count == 0 // b116: a completing move means no attack
                && !fresh.Branches[0].Broken && !fresh.Branches[1].Broken,
                "hits " + fresh.Ostrich.Hits + " pecks " + l3.Count);
            Check("finale-flag-only-without-peck", fin2 >= 0 && fin3 == -1, "");
            Check("old-api-unchanged",
                OstrichPeck.Scatter(PeckBoard(Mixed(), 7)) && Total(TwoSets(0)) == 14, "");

            // --- wiring (source checks, same style as the redistribute test) ---
            string root = Path.GetDirectoryName(Application.dataPath);
            string app = File.ReadAllText(Path.Combine(root, "Assets/_Project/Scripts/App/FlockFiveApp.cs"));
            string fight = File.ReadAllText(Path.Combine(root, "Assets/_Project/Scripts/App/FlockFiveApp.OstrichFight.cs"));
            string mark = File.ReadAllText(Path.Combine(root, "Assets/_Project/Scripts/View/OstrichMark.cs"));
            Check("move-plays-fight", app.Contains("yield return OstrichFight();")
                && app.Contains("!_ostrichFight) _garden.Ostrich.Sync"), "");
            Check("fight-uses-shared-flight", fight.Contains("PerchOne(") && fight.Contains("FlockRedistribute.Duration")
                && fight.Contains("OstrichStrike.Contact") && fight.Contains("Wow.Burst") && fight.Contains("Sfx.Chirp"), "");
            Check("mark-strike-gates-flee", mark.Contains("if (_strikeOn) return;") && mark.Contains("BeginStrike(Vector3"), "");

            Debug.Log(fail == 0 ? "build110-peck tests DONE " + pass + "/" + (pass + fail) : "build110-peck tests FAIL " + fail + " failed, " + pass + " passed");
        }

        static Board PeckBoard(Board b, int seed)
        {
            b.Ostrich = new OstrichState { PeckSeed = seed, PecksPerMove = 2 };
            return b;
        }

        static Board Mixed()
        {
            var b = new Board();
            b.SetFeeders(3);
            b.Live[0] = BirdColor.Ruby;
            b.Live[1] = BirdColor.Gold;
            b.Live[2] = BirdColor.Teal;
            b.Branches.Add(Run(BirdColor.Ruby, 3, BirdColor.Gold, 1));
            b.Branches.Add(Run(BirdColor.Gold, 3, BirdColor.Teal, 1));
            b.Branches.Add(Run(BirdColor.Teal, 2, BirdColor.Ruby, 2));
            b.Branches.Add(Run(BirdColor.Violet, 1, BirdColor.Violet, 0));
            b.Branches.Add(new BranchState());
            return b;
        }

        static Board TwoSets(int hits)
        {
            var b = new Board();
            b.SetFeeders(3);
            b.Live[0] = BirdColor.Ruby;
            b.Live[1] = BirdColor.Gold;
            b.Live[2] = BirdColor.Teal;
            b.Queue.Add(BirdColor.Violet);
            b.Branches.Add(Five(BirdColor.Ruby));
            b.Branches.Add(Five(BirdColor.Gold));
            b.Branches.Add(Run(BirdColor.Teal, 3, BirdColor.Violet, 1));
            b.Branches.Add(new BranchState());
            b.Ostrich = new OstrichState { PeckSeed = 4, PecksPerMove = 2, Hits = hits };
            return b;
        }

        static BranchState Run(BirdColor a, int na, BirdColor c, int nc)
        {
            var br = new BranchState();
            for (int i = 0; i < na; i++) { br.Birds.Add(new Bird(a)); br.Shrouded.Add(false); }
            for (int i = 0; i < nc; i++) { br.Birds.Add(new Bird(c)); br.Shrouded.Add(false); }
            return br;
        }

        static BranchState Five(BirdColor c) => Run(c, 5, c, 0);
        static BranchState Four(BirdColor c) => Run(c, 4, c, 0);

        static int Total(Board b)
        {
            int n = 0;
            for (int i = 0; i < b.Branches.Count; i++) n += b.Branches[i].Count;
            return n;
        }
    }
}
#endif
