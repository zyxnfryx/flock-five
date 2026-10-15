#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 89. Garden 17 dead end from Brandon's build-88 photo: the only hop
    // left was one Ruby bird going back and forth. A take-back is not a move,
    // so the board ices and the gift glove points at the ad branch. A sparrow
    // snap may never leave a board the solver cannot win.
    public static class Build89DeadEndTests
    {
        const string Out = "/tmp/flock-five-b89-deadend-tests.txt";

        static Bird B(BirdColor c, BirdKit k) => new Bird(c, k);

        static BranchState Limb(bool shroudBase, params Bird[] birds)
        {
            var st = new BranchState();
            for (int i = 0; i < birds.Length; i++)
            {
                st.Birds.Add(birds[i]);
                st.Shrouded.Add(shroudBase && i == 0);
            }
            return st;
        }

        static BranchState Gone() => new BranchState { Broken = true };

        static BranchState Gift() => new BranchState { IsBonus = true, AdLocked = true };

        // DEADEND_b88.md section 1, packed limb order b0..b15. b1, b2, b5 and
        // b12 are gone (three collects and one sparrow snap).
        public static Board Garden17Dead(bool spareLimb = false)
        {
            const BirdColor R = BirdColor.Ruby, G = BirdColor.Gold, T = BirdColor.Teal,
                V = BirdColor.Violet, P = BirdColor.Peach;
            const BirdKit N = BirdKit.Plain, C = BirdKit.Crown, W = BirdKit.Bow;
            var b = new Board();
            b.Branches.Add(Limb(false, B(V, C), B(R, N), B(R, N), B(R, N), B(R, N))); // b0  L0
            b.Branches.Add(Gone());                                                    // b1
            b.Branches.Add(Gone());                                                    // b2
            b.Branches.Add(Limb(false, B(G, C), B(V, C), B(V, C), B(V, C)));           // b3  R0
            b.Branches.Add(Limb(true, B(V, C), B(G, C), B(G, C), B(G, C), B(G, N)));   // b4  L1
            b.Branches.Add(Gone());                                                    // b5
            b.Branches.Add(Limb(false, B(T, N), B(T, N), B(V, W), B(V, W), B(V, W)));  // b6  L2
            b.Branches.Add(Limb(true, B(P, C), B(G, W), B(G, W), B(G, W), B(G, W)));   // b7  R1
            b.Branches.Add(Limb(false, B(G, N), B(P, W), B(P, W), B(P, W)));           // b8  L3
            b.Branches.Add(Limb(false, B(G, W), B(P, C), B(G, N), B(G, N), B(G, C)));  // b9  R2
            b.Branches.Add(Limb(false, B(T, N), B(P, W), B(P, W), B(G, N), B(P, C)));  // b10 L4
            b.Branches.Add(Limb(true, B(P, C), B(T, N), B(T, N)));                     // b11 R3
            b.Branches.Add(Gone());                                                    // b12
            b.Branches.Add(Limb(false, B(V, W), B(V, W), B(P, C), B(R, N)));           // b13 R4
            if (spareLimb) b.Branches.Add(new BranchState());
            b.Branches.Add(Gift());                                                    // b14
            b.Branches.Add(Gift());                                                    // b15
            b.Live[0] = R;
            b.Live[1] = V;
            b.Queue.Add(G);
            return b;
        }

        static int Gift(Board b)
        {
            for (int i = 0; i < b.Branches.Count; i++)
                if (b.Branches[i].AdLocked) return i;
            return -1;
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
                Debug.Log("[build89deadend] " + line);
            }

            // ---- Garden 17 dead board ----
            var dead = Garden17Dead();
            var trail = new HopTrail();
            int legal = 0, onlyFrom = -1, onlyTo = -1, onlyRun = 0;
            for (int f = 0; f < dead.Branches.Count; f++)
                for (int t = 0; t < dead.Branches.Count; t++)
                    if (dead.CanMove(f, t, out int r)) { legal++; onlyFrom = f; onlyTo = t; onlyRun = r; }
            Check("g17-one-legal-hop", legal == 1 && onlyFrom == 0 && onlyTo == 13 && onlyRun == 1,
                "n " + legal + " " + onlyFrom + "->" + onlyTo + " x" + onlyRun);
            Check("g17-unwinnable-without-ad", GardenSolve.Look(dead, 20000) == GardenSolve.Outlook.Tangled,
                GardenSolve.Look(dead, 20000).ToString());
            Check("g17-has-gift-out", Gift(dead) >= 0, "gift " + Gift(dead));
            Check("g17-no-false-ice-before-hop", !trail.DeadEnd(dead) && !trail.Stuck(dead), "");

            dead.TryMove(0, 13, out int run1);
            trail.Note(0, 13, run1, dead.JustUnveiled);
            Check("g17-old-rule-missed-it", dead.HasHop(), "HasHop " + dead.HasHop());
            Check("g17-no-progress-hop", !dead.HasProgressHop(0, 13, run1), "");
            Check("g17-ices", trail.DeadEnd(dead), "");
            Check("g17-nudges", trail.Stuck(dead) && Gift(dead) >= 0, "");

            dead.TryMove(13, 0, out int run2);
            trail.Note(13, 0, run2, dead.JustUnveiled);
            Check("g17-ices-after-take-back", trail.DeadEnd(dead) && trail.Stuck(dead), "rev " + trail.Reversals);

            // Opening the ad branch is a real out: the stuck state ends.
            var opened = dead.Clone();
            int g = Gift(opened);
            opened.Branches[g].AdLocked = false;
            Check("g17-ad-branch-clears-dead-end", !trail.DeadEnd(opened)
                && GardenSolve.Look(opened, 20000) == GardenSolve.Outlook.Winnable,
                GardenSolve.Look(opened, 20000).ToString());

            // ---- Back-and-forth detection ----
            var t2 = new HopTrail();
            t2.Note(2, 5, 1, false);
            Check("bf-first-hop-not-reversal", t2.Reversals == 0 && !t2.BackAndForth, "rev " + t2.Reversals);
            t2.Note(5, 2, 1, false);
            Check("bf-one-reversal", t2.Reversals == 1 && !t2.BackAndForth, "rev " + t2.Reversals);
            t2.Note(2, 5, 1, false);
            Check("bf-two-reversals-nudge", t2.Reversals == 2 && t2.BackAndForth, "rev " + t2.Reversals);
            t2.Note(2, 7, 1, false);
            Check("bf-real-move-resets", t2.Reversals == 0 && !t2.BackAndForth, "rev " + t2.Reversals);
            t2.Note(7, 2, 2, false);
            Check("bf-different-run-not-undo", t2.Reversals == 0, "rev " + t2.Reversals);
            t2.Note(2, 7, 2, false);
            t2.Note(7, 2, 2, true);
            Check("bf-collect-or-reveal-resets", t2.Reversals == 0 && t2.LastRun == 0, "rev " + t2.Reversals);
            Check("bf-is-undo", Board.IsUndo(5, 2, 1, 2, 5, 1) && !Board.IsUndo(5, 2, 1, -1, -1, 0)
                && !Board.IsUndo(5, 3, 1, 2, 5, 1), "");

            // A take-back on a live board is not a dead end: Garden 17 from its start.
            int keepIndex = LevelData.Index;
            var live = LevelData.Open(16);
            LevelData.Open(keepIndex);
            int af = -1, at = -1;
            for (int f = 0; f < live.Branches.Count && af < 0; f++)
                for (int t = 0; t < live.Branches.Count; t++)
                    if (live.CanMove(f, t, out _)) { af = f; at = t; break; }
            var ft = new HopTrail();
            live.TryMove(af, at, out int fr);
            ft.Note(af, at, fr, live.JustUnveiled);
            Check("live-board-not-dead", af >= 0 && ft.HasProgressHop(live) && !ft.DeadEnd(live) && !ft.Stuck(live),
                af + "->" + at + " x" + fr);
            Check("no-last-hop-equals-hashop", live.HasProgressHop(-1, -1, 0) == live.HasHop()
                && Garden17Dead().HasProgressHop(-1, -1, 0) == Garden17Dead().HasHop(), "");

            // ---- Sparrow never bricks a board ----
            int tried = 0, broke = 0, bricked = 0, oldRuleBricks = 0;
            string firstBrick = "";
            void Sparrow(Board start, string tag)
            {
                if (GardenSolve.Look(start, PestPark.SolveCap) != GardenSolve.Outlook.Winnable) return;
                for (int i = 0; i < start.Branches.Count; i++)
                {
                    var st = start.Branches[i];
                    if (st.Broken || st.IsBonus || st.AdLocked || st.Count == 0) continue;
                    if (start.IsSleeping(i)) continue;
                    tried++;
                    var after = start.Clone();
                    var plan = PestPark.Apply(after, i, false);
                    if (!plan.Break) continue;
                    broke++;
                    if (GardenSolve.Look(after, PestPark.SolveCap) == GardenSolve.Outlook.Winnable) continue;
                    bricked++;
                    if (firstBrick.Length == 0) firstBrick = tag + " limb " + i;
                }
            }

            // The board Brandon would have had before the snap: the spare limb back.
            var spare = Garden17Dead(true);
            Check("g17-spare-limb-winnable", GardenSolve.Look(spare, 20000) == GardenSolve.Outlook.Winnable,
                GardenSolve.Look(spare, 20000).ToString());
            Sparrow(spare, "g17-spare");
            int spareTried = tried;

            // How often the old seat-count rule alone would have snapped a limb into a dead board.
            for (int i = 0; i < spare.Branches.Count; i++)
            {
                var st = spare.Branches[i];
                if (st.Broken || st.IsBonus || st.Count == 0) continue;
                var c = spare.Clone();
                var flock = new System.Collections.Generic.List<Bird>(c.Branches[i].Birds);
                c.Branches[i].Birds.Clear();
                c.Branches[i].Shrouded.Clear();
                c.Branches[i].Broken = true;
                int need = flock.Count + 1;
                if (PestPark.FreeSeats(c) < need) continue;
                if (GardenSolve.Look(c, PestPark.SolveCap) == GardenSolve.Outlook.Tangled) oldRuleBricks++;
            }

            int levels = LevelData.Count;
            int saveIndex = LevelData.Index;
            for (int lv = 0; lv < levels; lv++)
                Sparrow(LevelData.Open(lv), "garden " + (lv + 1));
            LevelData.Open(saveIndex);

            Check("sparrow-never-bricks", tried > spareTried && bricked == 0,
                "levels " + levels + " tried " + tried + " snapped " + broke + " bricked " + bricked
                + (firstBrick.Length > 0 ? " first " + firstBrick : "")
                + " (old seat rule could brick " + oldRuleBricks + " empty-limb snaps on g17)");

            string park = File.ReadAllText("Assets/_Project/Scripts/Puzzle/PestPark.cs");
            Check("pestpark-uses-solver", park.IndexOf("KeepsSolvable(b)") >= 0
                && park.IndexOf("GardenSolve.Look(after, SolveCap) == GardenSolve.Outlook.Winnable") >= 0, "");

            // ---- App wiring (source checks, same style as earlier builds) ----
            string app = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs");
            string coach = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Coach.cs");
            Check("checkover-uses-progress-hop",
                app.IndexOf("if (_hopTrail.HasProgressHop(_board) || _board.FindCollect() >= 0) return;") >= 0
                && app.IndexOf("if (_board.HasHop() || _board.FindCollect() >= 0) return;") < 0, "");
            Check("move-notes-trail", app.IndexOf("_hopTrail.Note(from, to, run, kicked > 0 || _board.JustUnveiled);") >= 0
                && app.IndexOf("DeadEndNudge();") >= 0, "");
            int nudgeAt = coach.IndexOf("void DeadEndNudge()");
            int nudgeEnd = nudgeAt >= 0 ? coach.IndexOf("int GiftBranch()", nudgeAt) : -1;
            string nudge = nudgeAt >= 0 && nudgeEnd > nudgeAt ? coach.Substring(nudgeAt, nudgeEnd - nudgeAt) : "";
            Check("nudge-reuses-gift-glove",
                nudge.IndexOf("_coachGiftUntil = PlayClock.Now + GiftHintSeconds;") >= 0
                && nudge.IndexOf("_coachResumeFade = true;") >= 0
                && nudge.IndexOf("GiftBranch() < 0") >= 0, "");
            Check("nudge-ignores-install-flag-and-never-plays-ad",
                nudge.Length > 0 && nudge.IndexOf("CoachGiftKey") < 0 && nudge.IndexOf("PlayerPrefs") < 0
                && nudge.IndexOf("Ads.") < 0 && nudge.IndexOf("ShowRewarded") < 0 && nudge.IndexOf("OpenBonus") < 0, "");

            Debug.Log(fail == 0
                ? "build89deadend tests DONE " + pass + "/" + (pass + fail)
                : "build89deadend tests FAIL " + fail + " failed, " + pass + " passed");
        }
    }
}
#endif
