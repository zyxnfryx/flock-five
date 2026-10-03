using UnityEngine;

namespace FlockFive
{
    // One gate for sparrow and hawk. After any pest appears or is cleared,
    // neither kind may arrive until the cooldown and two player collects.
    // Visit() itself is not gated, so a scripted/editor visit still runs.
    public static class PestSchedule
    {
        public const float CooldownMin = 25f;
        public const float CooldownMax = 40f;
        public const int CollectsBetween = 2;

        // Matches SparrowVisits + HawkVisits so the hawk lesson slot stays.
        public const int CapQuiet = 0;   // levels 1-2
        public const int CapEarly = 1;   // 3-6
        public const int CapMid = 2;     // 7-10
        public const int CapHawk = 3;    // 11-13
        public const int CapLate = 4;    // 14+

        static int _cap;
        static int _spawned;
        static bool _held;
        static float _readyAt;
        static int _collectsLeft;

        public static bool StageFull => _cap <= 0 || _spawned >= _cap;

        public static bool Busy => _held || SparrowView.Live != null || HawkView.Live != null;

        public static void BeginStage(int displayLevel)
        {
            _spawned = 0;
            _held = false;
            _readyAt = 0f;
            _collectsLeft = 0;
            _cap = CapFor(displayLevel);
        }

        public static int CapFor(int displayLevel)
        {
            if (displayLevel <= 2) return CapQuiet;
            if (displayLevel <= 6) return CapEarly;
            if (displayLevel <= 10) return CapMid;
            if (displayLevel <= 13) return CapHawk;
            return CapLate;
        }

        // True only when this caller may spawn. Arms the shared cooldown immediately.
        public static bool TryReserve()
        {
            if (_cap <= 0 || _spawned >= _cap) return false;
            if (Busy) return false;
            if (PlayClock.Now < _readyAt) return false;
            if (_collectsLeft > 0) return false;
            _held = true;
            _spawned++;
            Arm();
            return true;
        }

        // Pest left the stage (or the visit ended). Fresh cooldown from the clear.
        public static void NoteGone()
        {
            _held = false;
            Arm();
        }

        public static void NoteCollect()
        {
            if (_collectsLeft > 0) _collectsLeft--;
        }

        static void Arm()
        {
            float at = PlayClock.Now + Random.Range(CooldownMin, CooldownMax);
            if (at > _readyAt) _readyAt = at;
            _collectsLeft = CollectsBetween;
        }
    }
}
