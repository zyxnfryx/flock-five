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
