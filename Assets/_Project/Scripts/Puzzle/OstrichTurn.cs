using System.Collections.Generic;
using UnityEngine;

namespace FlockFive
{
    // Pacing for the ostrich gardens (27, 32, 38, 43). The ostrich answers a player move; it never
    // runs ahead of it and never batches several. One move at a time goes through the gate:
    //   Idle -> PlayerMove (hop + collects resolve) -> OstrichTurn (pecks, one relocation at a time) -> Idle.
    // A tap that would start another move while the gate is not Idle is refused, so there is nothing
    // to queue: at most one ostrich turn is ever pending, and it belongs to the move that is resolving.
    // Plain C# on purpose: the edit-mode suite drives it without a scene.
    public sealed class OstrichTurnGate
    {
        public enum Phase { Idle, PlayerMove, OstrichTurn }

        Phase _phase;
        int _ticket;
        bool _turnPending;

        public Phase Current => _phase;
        public bool Busy => _phase != Phase.Idle;
        // 0 or 1. Never more: a second move cannot start while the first is resolving.
        public int PendingTurns => _turnPending ? 1 : 0;
        public int TurnsRun { get; private set; }

        // A move may start only from Idle. Returns a ticket (> 0) or 0 when refused.
        public int TryBeginMove()
        {
            if (_phase != Phase.Idle) return 0;
            _phase = Phase.PlayerMove;
            _turnPending = true;
            return ++_ticket;
        }

        // The move's animations are done. The ostrich may act only now, and only if it owes a turn.
        // animationsBusy: a hop, collect or other flight is still in the air.
        public bool TryBeginTurn(int ticket, bool animationsBusy)
        {
            if (ticket != _ticket || _phase != Phase.PlayerMove || !_turnPending || animationsBusy) return false;
            _turnPending = false;
            _phase = Phase.OstrichTurn;
            TurnsRun++;
            return true;
        }

        // Turn over (or the move ended without one). Stale tickets (restart) are ignored.
        public void End(int ticket)
        {
            if (ticket != _ticket) return;
            _phase = Phase.Idle;
            _turnPending = false;
        }

        // Restart / stage change: drop everything, invalidate outstanding tickets.
        public void Reset()
        {
            _ticket++;
            _phase = Phase.Idle;
            _turnPending = false;
        }
    }

    // Timing for how the ostrich's pecks play out and how each bird relocates.
    public static class OstrichTurn
    {
        // Beat between the player's move landing and the ostrich starting to act.
        public const float TurnDelaySec = 0.30f;
        // Beat between one relocation landing and the next wind-up.
        public const float PeckGapSec = 0.18f;
        // A relocation never plays faster than this, however it is tuned elsewhere.
        public const float MinFlightSec = 0.45f;

        // Hit-testing. A complete set is normally skipped (it is about to fly off), so a tap near it goes to the
        // limb above. With a living ostrich the set STAYS on its limb, so it must stay tappable: otherwise the
        // tap lands on the branch above and the set looks dead. Broken limbs are never tappable.
        public static bool Tappable(Board b, int branch)
        {
            if (b == null || (uint)branch >= (uint)b.Branches.Count) return false;
            var br = b.Branches[branch];
            if (br == null || br.Broken) return false;
            if (!br.IsFullMatch(out _)) return true;
            return b.Ostrich != null && b.Ostrich.Alive;
        }

        public static float FlightSec => Mathf.Max(MinFlightSec, FlockRedistribute.Duration(0));

        // Where the relocated bird is `t` seconds after the kick. Eased hop with an arc; the bird
        // is at `from` at t = 0 and at `to` only from the end of the flight, never before.
        public static Vector3 BirdAt(Vector3 from, Vector3 to, float t)
        {
            return FlockRedistribute.Sample(from, to, 0, Mathf.Clamp(t, 0f, FlightSec));
        }

        // Start times (seconds from the turn start) for n relocations: strictly one at a time.
        // Each begins after the previous one has landed plus the gap.
        public static List<float> Schedule(int n, float strikeSec)
        {
            var s = new List<float>(n);
            float t = TurnDelaySec;
            for (int i = 0; i < n; i++)
            {
                s.Add(t);
                t += strikeSec + FlightSec + PeckGapSec;
            }
            return s;
        }
    }

    // Timeline of one ostrich hit and of the exit. Pure math: the app plays it, the edit-mode suite checks it.
    // One hit: the birds of the cleared set fly up to the ostrich ONE AFTER ANOTHER (staggered launches),
    // each lands an impact, then they fly back and reseat on their branch (the branch does NOT break).
    // Three hits, then the ostrich exits (stagger, flustered run off screen with a feather puff), and only
    // then do the sets break and fly to their feeders.
    public static class OstrichHitTimeline
    {
        public const float LaunchStagger = 0.14f;   // gap between one bird leaving and the next
        public const float FlyUpSec = 0.40f;        // limb -> ostrich
        public const float HoldSec = 0.16f;         // after the last impact, before they head home
        public const float ReturnStagger = 0.08f;
        public const float ReturnSec = 0.50f;       // ostrich -> own seat
        public const float BetweenHitsSec = 0.25f;
        public const float ArcUp = 1.1f;            // hop height of the flight up / back (world units)

        // Exit: stagger in place, then run off screen. Puff at the start of the run and again mid-run.
        public const float ExitStaggerSec = 0.85f;
        public const float ExitRunSec = 1.30f;
        public const float ExitPuffAtRun = 0.0f;
        public const float ExitPuffMidRun = 0.5f;
        public const float ExitTotalSec = ExitStaggerSec + ExitRunSec;
        public const float BreakDelaySec = 0.20f;   // beat after he is gone, before the branches break

        public static float LaunchAt(int bird) => bird * LaunchStagger;
        public static float ArriveAt(int bird) => LaunchAt(bird) + FlyUpSec;
        public static float LastArrive(int birds) => birds <= 0 ? 0f : ArriveAt(birds - 1);
        public static float ReturnAt(int birds, int bird) => LastArrive(birds) + HoldSec + bird * ReturnStagger;
        public static float LandAt(int birds, int bird) => ReturnAt(birds, bird) + ReturnSec;
        public static float HitSec(int birds) => birds <= 0 ? 0f : LandAt(birds, birds - 1);
        public static float HitStride(int birds) => HitSec(birds) + BetweenHitsSec;

        // Bird position `t` seconds into one hit. home = its seat, target = the ostrich.
        // Before launch: home. Up: eased hop to the target. Hold: on the target. Back: eased hop home.
        public static Vector3 BirdAt(int birds, int bird, float t, Vector3 home, Vector3 target)
        {
            float a = LaunchAt(bird), arrive = ArriveAt(bird), ret = ReturnAt(birds, bird), land = LandAt(birds, bird);
            if (t <= a) return home;
            if (t < arrive) return Hop(home, target, (t - a) / FlyUpSec);
            if (t <= ret) return target;
            if (t < land) return Hop(target, home, (t - ret) / ReturnSec);
            return home;
        }

        static Vector3 Hop(Vector3 from, Vector3 to, float u)
        {
            u = Mathf.Clamp01(u);
            float e = u * u * (3f - 2f * u);
            var p = Vector3.Lerp(from, to, e);
            p.y += Mathf.Sin(u * Mathf.PI) * ArcUp;
            return p;
        }

        public struct Beat
        {
            public int Hit;      // 0-based
            public int Bird;
            public float Launch, Arrive, Return, Land;   // absolute seconds from the start of the sequence
        }

        // Every bird of every hit, back to back. hits sets of `birds` birds each.
        public static List<Beat> Plan(int hits, int birds, out float exitStart, out float breakAt)
        {
            var l = new List<Beat>();
            float t0 = 0f;
            for (int h = 0; h < hits; h++)
            {
                for (int i = 0; i < birds; i++)
                    l.Add(new Beat
                    {
                        Hit = h, Bird = i,
                        Launch = t0 + LaunchAt(i), Arrive = t0 + ArriveAt(i),
                        Return = t0 + ReturnAt(birds, i), Land = t0 + LandAt(birds, i)
                    });
                t0 += HitStride(birds);
            }
            exitStart = t0;
            breakAt = exitStart + ExitTotalSec + BreakDelaySec;
            return l;
        }
    }
}
