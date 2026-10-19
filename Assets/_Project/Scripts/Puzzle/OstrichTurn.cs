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
}
