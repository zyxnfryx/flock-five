#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 116 ostrich pacing. No stacked turns after rapid moves, relocations are animated (never instant),
    // the ostrich acts only after the move resolves. Edit-mode, no scene. Registered in EnsurePlayable.RunEditorTests.
    public static class Build116OstrichPaceTests
    {
        const string Out = "/tmp/flock-five-b116-ostrich-pace-tests.txt";

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
                Debug.Log("[build116-pace] " + line);
            }

            // --- no stacked turns after rapid moves ---
            var g = new OstrichTurnGate();
            int accepted = 0, refused = 0, maxPending = 0;
            int t1 = g.TryBeginMove();
            for (int i = 0; i < 8; i++)
            {
                int t = g.TryBeginMove(); // rapid taps while the first move resolves
                if (t != 0) accepted++; else refused++;
                maxPending = Mathf.Max(maxPending, g.PendingTurns);
            }
            Check("rapid-moves-refused", t1 > 0 && accepted == 0 && refused == 8 && maxPending <= 1, "accepted " + accepted + " refused " + refused);
            Check("one-pending-turn", g.PendingTurns == 1, "pending " + g.PendingTurns);

            // --- ostrich acts only after the move resolves ---
            Check("no-turn-while-animating", !g.TryBeginTurn(t1, true) && g.Current == OstrichTurnGate.Phase.PlayerMove && g.TurnsRun == 0,
                "phase " + g.Current);
            Check("no-turn-for-stale-ticket", !g.TryBeginTurn(t1 + 5, false), "");
            Check("turn-after-resolve", g.TryBeginTurn(t1, false) && g.Current == OstrichTurnGate.Phase.OstrichTurn && g.PendingTurns == 0, "");
            Check("no-second-turn", !g.TryBeginTurn(t1, false) && g.TurnsRun == 1, "runs " + g.TurnsRun);
            Check("no-move-during-turn", g.TryBeginMove() == 0 && g.Busy, "");
            g.End(t1);
            Check("idle-after-turn", !g.Busy && g.PendingTurns == 0, "");

            // many sequential move attempts: turns == accepted moves, never more
            var g2 = new OstrichTurnGate();
            int moves = 0;
            for (int i = 0; i < 20; i++)
            {
                int t = g2.TryBeginMove();
                if (t == 0) continue;
                moves++;
                if (i % 3 == 0) { int x = g2.TryBeginMove(); if (x != 0) moves++; } // a banked attempt
                g2.TryBeginTurn(t, false);
                g2.End(t);
            }
            Check("turns-equal-moves", g2.TurnsRun == moves, "turns " + g2.TurnsRun + " moves " + moves);

            // restart invalidates an in-flight move
            var g3 = new OstrichTurnGate();
            int old = g3.TryBeginMove();
            g3.Reset();
            int fresh = g3.TryBeginMove();
            g3.End(old); // stale finally from the stopped coroutine
            Check("reset-ignores-stale-ticket", fresh > 0 && g3.Busy && !g3.TryBeginTurn(old, false), "");

            // --- board side: one move owes at most PecksPerMove relocations ---
            var b = new Board();
            b.SetFeeders(3);
            b.Live[0] = BirdColor.Ruby; b.Live[1] = BirdColor.Gold; b.Live[2] = BirdColor.Teal;
            var l0 = new BranchState(); var l1 = new BranchState();
            for (int i = 0; i < 3; i++) { l0.Birds.Add(new Bird(BirdColor.Ruby)); l0.Shrouded.Add(false); }
            l0.Birds.Add(new Bird(BirdColor.Gold)); l0.Shrouded.Add(false);
            for (int i = 0; i < 2; i++) { l1.Birds.Add(new Bird(BirdColor.Teal)); l1.Shrouded.Add(false); }
            b.Branches.Add(l0); b.Branches.Add(l1); b.Branches.Add(new BranchState());
            b.Ostrich = new OstrichState { PeckSeed = 11, PecksPerMove = 2 };
            var log = new System.Collections.Generic.List<PeckHit>();
            GardenSolve.AfterMove(b, log, out _);
            Check("one-turn-at-most-pecks", log.Count <= 2 && b.Ostrich.PeckIndex == 2, "pecks " + log.Count + " idx " + b.Ostrich.PeckIndex);

            // --- set completion is always detected, collected, counted ---
            // 1) completed by the player's move, then the ostrich turn: counted as a hit, set stays (alive), pecks follow
            var sc = SetBoard(0, 5);
            var lg = new System.Collections.Generic.List<PeckHit>();
            GardenSolve.AfterMove(sc, lg, out _);
            Check("set-after-move-counts-hit", sc.Ostrich.Hits == 1 && (sc.Ostrich.Bonked & 1UL) != 0 && !HasPending(sc), "hits " + sc.Ostrich.Hits);
            // 2) the stale-bonk bug: set bonked, player breaks it, completes it again on the same limb -> counts again
            sc.Branches[0].Birds.RemoveAt(sc.Branches[0].Count - 1); sc.Branches[0].Shrouded.RemoveAt(sc.Branches[0].Shrouded.Count - 1);
            OstrichPest.Settle(sc); // broken set forgets its bonk
            Check("broken-set-forgets-bonk", (sc.Ostrich.Bonked & 1UL) == 0, "bonked " + sc.Ostrich.Bonked);
            sc.Branches[0].Birds.Add(new Bird(BirdColor.Gold)); sc.Branches[0].Shrouded.Add(false);
            Check("recompleted-set-is-pending", OstrichPest.HasPendingSet(sc), "");
            GardenSolve.AfterMove(sc, lg, out _);
            Check("recompleted-set-counts-hit", sc.Ostrich.Hits == 2 && !OstrichPest.HasPendingSet(sc), "hits " + sc.Ostrich.Hits);
            // 3) completion deferred during the turn, processed after: settle after a bare board mutation picks it up
            var df = SetBoard(0, 4);
            df.Ostrich.PecksPerMove = 0; // keep the 4 in place; the point is the late completion
            var fl = new System.Collections.Generic.List<PeckHit>();
            GardenSolve.AfterMove(df, fl, out _); // turn with no set
            df.Branches[0].Birds.Add(new Bird(BirdColor.Gold)); df.Branches[0].Shrouded.Add(false); // completes while the turn plays
            Check("deferred-set-pending", OstrichPest.HasPendingSet(df) && df.Ostrich.Hits == 0, "");
            OstrichPest.Settle(df);
            Check("deferred-set-processed", df.Ostrich.Hits == 1 && !OstrichPest.HasPendingSet(df), "hits " + df.Ostrich.Hits);
            // 4) the third hit collects the sets and the ostrich leaves
            var fin = SetBoard(2, 5);
            GardenSolve.AfterMove(fin, null, out _);
            Check("third-hit-collects", !fin.Ostrich.Alive && fin.Branches[0].Broken, "");

            // 5) precedence: a completing move hits and collects first; the ostrich does not attack that move
            var pr = SetBoard(0, 5);
            var plog = new System.Collections.Generic.List<PeckHit>();
            GardenSolve.AfterMove(pr, plog, out _);
            Check("completing-move-no-attack", pr.Ostrich.Hits == 1 && plog.Count == 0 && pr.Ostrich.PeckIndex == 0,
                "hits " + pr.Ostrich.Hits + " pecks " + plog.Count + " idx " + pr.Ostrich.PeckIndex);
            var pr3 = SetBoard(2, 5);
            GardenSolve.AfterMove(pr3, null, out _);
            Check("completing-move-collects-no-attack", !pr3.Ostrich.Alive && pr3.Branches[0].Broken && pr3.Ostrich.PeckIndex == 0, "");
            var np = SetBoard(0, 4); // no completion: he does attack
            var nlog = new System.Collections.Generic.List<PeckHit>();
            GardenSolve.AfterMove(np, nlog, out _);
            Check("non-completing-move-attacks", np.Ostrich.Hits == 0 && np.Ostrich.PeckIndex == 2, "idx " + np.Ostrich.PeckIndex);

            // 6) a completed set stays tappable in an ostrich garden (also after ostrich turns); normal gardens unchanged
            var tp = SetBoard(0, 5);
            Check("full-set-tappable-ostrich", OstrichTurn.Tappable(tp, 0), "");
            GardenSolve.AfterMove(tp, null, out _);
            GardenSolve.AfterMove(tp, null, out _); // further turns
            Check("full-set-tappable-after-turns", tp.Ostrich.Alive && !tp.Branches[0].Broken && OstrichTurn.Tappable(tp, 0), "");
            var plain = SetBoard(0, 5); plain.Ostrich = null;
            Check("full-set-not-tappable-plain", !OstrichTurn.Tappable(plain, 0) && OstrichTurn.Tappable(plain, 1), "");
            var gone = SetBoard(2, 5); GardenSolve.AfterMove(gone, null, out _);
            Check("collected-set-not-tappable", !OstrichTurn.Tappable(gone, 0), "");
            // 7) a bird relocated this turn is never relocated again in the same turn
            int dup = 0, turns = 0;
            for (int seed = 0; seed < 300; seed++)
            {
                var m = SetBoard(0, 3);
                m.Branches[2].Birds.Add(new Bird(BirdColor.Ruby)); m.Branches[2].Shrouded.Add(false);
                m.Ostrich.PeckSeed = seed; m.Ostrich.PecksPerMove = 3;
                var ml = new System.Collections.Generic.List<PeckHit>();
                OstrichPest.AfterMove(m, ml, out _);
                turns++;
                // replay: each hit's source seat must not be a seat an earlier hit landed on
                var landed = new System.Collections.Generic.List<KeyValuePair<int, int>>();
                foreach (var h in ml)
                {
                    if (landed.Contains(new KeyValuePair<int, int>(h.From, h.FromCount - 1))) dup++;
                    landed.Add(new KeyValuePair<int, int>(h.To, h.ToCount));
                }
            }
            Check("bird-not-moved-twice-per-turn", dup == 0 && turns == 300, "dups " + dup);
            // forced case: one movable bird only; second peck must find nothing rather than move it again
            var one = new Board();
            one.SetFeeders(3);
            one.Live[0] = BirdColor.Gold; one.Live[1] = BirdColor.Ruby; one.Live[2] = BirdColor.Teal;
            var o0 = new BranchState(); o0.Birds.Add(new Bird(BirdColor.Gold)); o0.Shrouded.Add(false);
            one.Branches.Add(o0); one.Branches.Add(new BranchState());
            one.Ostrich = new OstrichState { PeckSeed = 5, PecksPerMove = 3 };
            var olog = new System.Collections.Generic.List<PeckHit>();
            OstrichPest.AfterMove(one, olog, out _);
            Check("lone-bird-moves-once", olog.Count == 1, "pecks " + olog.Count);
            var movedList = new System.Collections.Generic.List<KeyValuePair<int, int>>();
            var again = new Board(); again.SetFeeders(3);
            again.Live[0] = BirdColor.Gold; again.Live[1] = BirdColor.Ruby; again.Live[2] = BirdColor.Teal;
            var a0 = new BranchState(); a0.Birds.Add(new Bird(BirdColor.Gold)); a0.Shrouded.Add(false);
            again.Branches.Add(a0); again.Branches.Add(new BranchState());
            again.Ostrich = new OstrichState { PeckSeed = 5, PecksPerMove = 3 };
            bool first = OstrichPeck.Scatter(again, out _, movedList);
            bool second = OstrichPeck.Scatter(again, out _, movedList);
            Check("scatter-excludes-moved", first && !second, "first " + first + " second " + second);

            // 8) hits are serial: birds fly up one after another, hit after hit, then the exit, then the branches break
            var plan = OstrichHitTimeline.Plan(3, 5, out float exitStart, out float breakAt);
            bool serialArrive = plan.Count == 15, serialLaunch = true, hitsOrdered = true;
            for (int i = 1; i < plan.Count; i++)
            {
                serialArrive &= plan[i].Arrive > plan[i - 1].Arrive + 0.05f;       // never two impacts at once
                serialLaunch &= plan[i].Launch > plan[i - 1].Launch;
                if (plan[i].Hit != plan[i - 1].Hit)
                    hitsOrdered &= plan[i].Launch >= plan[i - 1].Land && plan[i].Hit == plan[i - 1].Hit + 1;
            }
            Check("hit-birds-arrive-one-after-another", serialArrive && serialLaunch, "");
            Check("hits-sequential-not-overlapping", hitsOrdered, "");
            int hitCount = 0; foreach (var bt in plan) if (bt.Bird == 0) hitCount++;
            Check("three-hits", hitCount == 3, "hits " + hitCount);
            Check("exit-after-third-hit", exitStart >= plan[plan.Count - 1].Land, "exit " + exitStart + " last land " + plan[plan.Count - 1].Land);
            Check("branches-break-after-exit", breakAt >= exitStart + OstrichHitTimeline.ExitTotalSec + OstrichHitTimeline.BreakDelaySec - 1e-4f
                && OstrichHitTimeline.ExitTotalSec >= 1.5f, "break " + breakAt);
            Check("return-after-last-impact", plan[0].Return >= plan[4].Arrive, "ret " + plan[0].Return + " last arrive " + plan[4].Arrive);
            // positions: bird 0 is already up while bird 4 is still on its seat; continuous; seat -> ostrich -> seat
            var home = new Vector3(-3f, -2f, 0f); var head = new Vector3(0.5f, 4f, 0f);
            float tMid = OstrichHitTimeline.ArriveAt(0) - 0.05f;
            Check("birds-staggered-in-air", (OstrichHitTimeline.BirdAt(5, 0, tMid, home, head) - home).magnitude > 0.5f
                && (OstrichHitTimeline.BirdAt(5, 4, tMid, home, head) - home).sqrMagnitude < 1e-6f, "");
            Check("hit-flight-endpoints",
                (OstrichHitTimeline.BirdAt(5, 2, OstrichHitTimeline.ArriveAt(2), home, head) - head).sqrMagnitude < 1e-4f
                && (OstrichHitTimeline.BirdAt(5, 2, OstrichHitTimeline.LandAt(5, 2), home, head) - home).sqrMagnitude < 1e-4f, "");
            float hitMax = 0f; var hp = home; float hs = OstrichHitTimeline.HitSec(5);
            for (float t = 1f / 120f; t <= hs; t += 1f / 120f)
            {
                var p = OstrichHitTimeline.BirdAt(5, 3, t, home, head);
                hitMax = Mathf.Max(hitMax, (p - hp).magnitude); hp = p;
            }
            Check("hit-flight-no-teleport", hitMax < 0.45f, "max step " + hitMax.ToString("0.000"));
            // board: three completing moves = three hits, one per move, no pecks, branches stay until the third
            var tb = new Board();
            tb.SetFeeders(3);
            tb.Live[0] = BirdColor.Gold; tb.Live[1] = BirdColor.Ruby; tb.Live[2] = BirdColor.Teal;
            var cols = new[] { BirdColor.Gold, BirdColor.Ruby, BirdColor.Teal };
            for (int i = 0; i < 3; i++) tb.Branches.Add(new BranchState());
            tb.Branches.Add(new BranchState());
            tb.Ostrich = new OstrichState { PeckSeed = 9, PecksPerMove = 2 };
            bool oneEach = true, staysUntil3 = true, noPecks = true;
            for (int k = 0; k < 3; k++)
            {
                for (int i = 0; i < 5; i++) { tb.Branches[k].Birds.Add(new Bird(cols[k])); tb.Branches[k].Shrouded.Add(false); }
                var kl = new System.Collections.Generic.List<PeckHit>();
                GardenSolve.AfterMove(tb, kl, out _);
                oneEach &= tb.Ostrich.Hits == k + 1;
                noPecks &= kl.Count == 0;
                if (k < 2) for (int j = 0; j <= k; j++) staysUntil3 &= !tb.Branches[j].Broken && tb.Branches[j].Count == 5;
            }
            bool allBroken = tb.Branches[0].Broken && tb.Branches[1].Broken && tb.Branches[2].Broken;
            Check("three-completing-moves-three-hits", oneEach && noPecks, "hits " + tb.Ostrich.Hits);
            Check("branches-intact-until-ostrich-leaves", staysUntil3 && !tb.Ostrich.Alive && allBroken, "");

            // --- relocation is animated, not instant ---
            var a = new Vector3(-2f, 1f, 0f);
            var c = new Vector3(3f, -1f, 0f);
            float fl = OstrichTurn.FlightSec;
            Check("flight-duration-sensible", fl >= 0.45f && fl <= 1.2f, "flight " + fl);
            Check("starts-at-origin", (OstrichTurn.BirdAt(a, c, 0f) - a).sqrMagnitude < 1e-6f, "");
            Check("not-instant", (OstrichTurn.BirdAt(a, c, 0.02f) - c).magnitude > 4f
                && (OstrichTurn.BirdAt(a, c, fl * 0.5f) - c).magnitude > 0.5f, "");
            Check("lands-at-destination", (OstrichTurn.BirdAt(a, c, fl) - c).sqrMagnitude < 1e-6f, "");
            float maxStep = 0f; var prev = a;
            for (float t = 1f / 120f; t <= fl; t += 1f / 120f)
            {
                var p = OstrichTurn.BirdAt(a, c, t);
                maxStep = Mathf.Max(maxStep, (p - prev).magnitude);
                prev = p;
            }
            Check("no-teleport-step", maxStep < 0.6f, "max step per 1/120s " + maxStep.ToString("0.000"));
            var sch = OstrichTurn.Schedule(3, OstrichStrike.Total);
            bool serial = sch.Count == 3 && sch[0] >= OstrichTurn.TurnDelaySec;
            for (int i = 1; i < sch.Count; i++) serial &= sch[i] >= sch[i - 1] + fl + OstrichStrike.Total;
            Check("relocations-one-at-a-time", serial, "starts " + string.Join(",", sch));

            // --- wiring ---
            string root = Path.GetDirectoryName(Application.dataPath);
            string app = File.ReadAllText(Path.Combine(root, "Assets/_Project/Scripts/App/FlockFiveApp.cs"));
            string fight = File.ReadAllText(Path.Combine(root, "Assets/_Project/Scripts/App/FlockFiveApp.OstrichFight.cs"));
            Check("hit-test-uses-tappable", app.Split(new[] { "OstrichTurn.Tappable(" }, System.StringSplitOptions.None).Length - 1 >= 3, "");
            string mk = File.ReadAllText(Path.Combine(root, "Assets/_Project/Scripts/View/OstrichMark.cs"));
            int iHit = fight.IndexOf("yield return OstrichHit(", System.StringComparison.Ordinal);
            int iExit = fight.IndexOf("yield return OstrichExit(", System.StringComparison.Ordinal);
            int iBreak = fight.IndexOf("StartCoroutine(StartleThenFly(sr, Vector3.up", System.StringComparison.Ordinal);
            Check("fight-order-hits-exit-break", iHit > 0 && iExit > iHit && iBreak > iExit, "" + iHit + "," + iExit + "," + iBreak);
            Check("hit-feedback-wired", fight.Contains("Impact(tr.position, col)") && fight.Contains("mark.ShowHit(hitNo)")
                && fight.Contains("mark.HoldFlee = true") && mk.Contains("public void BeginExit()") && mk.Contains("HoldFlee) return;"), "");
            Check("tap-gated", app.Contains("if (_turnGate.Busy)") && app.Contains("TryBeginMove()"), "");
            Check("fight-after-resolve", app.Contains("while (MoveStillAnimating())") && app.Contains("_turnGate.TryBeginTurn(_turnTicket")
                && app.Contains("yield return OstrichFight();"), "");
            Check("pending-set-processed", app.Contains("OstrichPest.HasPendingSet(_board)") && app.Contains("GardenSolve.Drain(_board);"), "");
            Check("gate-reset-on-restart", app.Split(new[] { "_turnGate.Reset();" }, System.StringSplitOptions.None).Length - 1 >= 2, "");
            Check("fight-serial-flights", fight.Contains("OstrichTurn.PeckGapSec") && fight.Contains("OstrichTurn.FlightSec")
                && !fight.Contains("srcStack.Count != h.FromCount"), "");

            Debug.Log(fail == 0 ? "build116-pace tests DONE " + pass + "/" + (pass + fail) : "build116-pace tests FAIL " + fail + " failed, " + pass + " passed");
        }

        static bool HasPending(Board b) => OstrichPest.HasPendingSet(b);

        // Limb 0: n gold (5 = complete). Limb 1: teal pair. Limb 2: empty room to peck into.
        static Board SetBoard(int hits, int n)
        {
            var b = new Board();
            b.SetFeeders(3);
            b.Live[0] = BirdColor.Gold; b.Live[1] = BirdColor.Ruby; b.Live[2] = BirdColor.Teal;
            var l0 = new BranchState(); var l1 = new BranchState();
            for (int i = 0; i < n; i++) { l0.Birds.Add(new Bird(BirdColor.Gold)); l0.Shrouded.Add(false); }
            for (int i = 0; i < 2; i++) { l1.Birds.Add(new Bird(BirdColor.Teal)); l1.Shrouded.Add(false); }
            b.Branches.Add(l0); b.Branches.Add(l1); b.Branches.Add(new BranchState());
            b.Ostrich = new OstrichState { PeckSeed = 3, PecksPerMove = 2, Hits = hits };
            return b;
        }
    }
}
#endif
