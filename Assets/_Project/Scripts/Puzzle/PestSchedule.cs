using System.Collections;
using UnityEngine;

namespace FlockFive
{
    // One gate for sparrow and hawk. After any pest appears or is cleared,
    // neither kind may arrive until the cooldown and two player collects.
    // A solved board (or a solve-resolve already underway) closes the gate
    // for both kinds. The cooldown does not gate Visit, so a scripted visit
    // still runs until the board is solved.
    public static class PestSchedule
    {
        public enum PestKind { Sparrow, Hawk }

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
        static Board _watch;
        static System.Func<bool> _resolveBegun;
        static bool _tutorialPause;
        static float _pauseAt;
        static float _pauseDebt;

        public static bool StageFull => _cap <= 0 || _spawned >= _cap;

        // Leaf lesson (and any other tutorial that asks). While this is set, no
        // time-driven pest may arrive. The arrive wait and the cooldown both
        // freeze, then the missed seconds are added back so nothing fires the
        // instant the lesson closes.
        public static bool TutorialPausesPests => _tutorialPause;

        // SolvedRule: every bird is in a complete set of five, or the win
        // sequence has already started. A 4+1 park is not solved, so the
        // schedule stays open while that pest is out.
        public static bool IsBoardSolved(Board board, bool resolveBegun) =>
            SolvedRule.BlocksNewPests(board, resolveBegun);

        // The live garden. BeginStage clears it; Load watches the new board.
        public static void Watch(Board board, System.Func<bool> resolveBegun)
        {
            _watch = board;
            _resolveBegun = resolveBegun;
        }

        public static bool ResolveBegunNow => _resolveBegun != null && _resolveBegun();

        public static bool SolvedNow => IsBoardSolved(_watch, ResolveBegunNow);

        // False once the board is solved. Kind is part of the call so sparrow and
        // hawk each ask, and a shared default cannot let one of them through.
        public static bool MaySpawn(PestKind kind, Board board, bool resolveBegun)
        {
            if (kind != PestKind.Sparrow && kind != PestKind.Hawk) return false;
            if (_tutorialPause) return false;
            return !IsBoardSolved(board, resolveBegun);
        }

        // Foreground wait that does not advance while a tutorial is holding pests.
        public static IEnumerator Wait(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            float t = 0f;
            while (t < seconds)
            {
                if (!_tutorialPause)
                    t += PlayClock.Delta;
                yield return null;
            }
        }

        public static void SetTutorialPause(bool on)
        {
            if (on == _tutorialPause) return;
            if (on)
            {
                _tutorialPause = true;
                _pauseAt = PlayClock.Now;
                _pauseDebt = 0f;
                return;
            }
            float held = PlayClock.Now - _pauseAt;
            if (held < 0f) held = 0f;
            held += _pauseDebt;
            _pauseDebt = 0f;
            _readyAt = ExtendedReady(_readyAt, PlayClock.Now, held);
            _tutorialPause = false;
        }

        // A cooldown that ran out while pests were paused does not open on the
        // next frame. An unarmed timer (readyAt <= 0) is left alone; the arrive
        // wait itself is frozen in Wait.
        public static float ExtendedReady(float readyAt, float now, float held)
        {
            if (held <= 0f || readyAt <= 0f) return readyAt;
            float at = readyAt + held;
            if (at < now) at = now + held;
            return at;
        }

        // Edit-mode suites have no ticking clock. Add seconds the unpause must give back.
        public static void HoldTutorial(float seconds)
        {
            if (!_tutorialPause || seconds <= 0f) return;
            _pauseDebt += seconds;
        }

        public static float SecondsUntilReady
        {
            get
            {
                float d = _readyAt - PlayClock.Now;
                return d > 0f ? d : 0f;
            }
        }

        // An arrive timer that elapses after the solve does not open a visit.
        public static bool TimerOpens(PestKind kind, bool fired, Board board, bool resolveBegun)
        {
            if (!fired) return false;
            return MaySpawn(kind, board, resolveBegun);
        }

        public static bool TimerOpens(PestKind kind, bool fired) =>
            TimerOpens(kind, fired, _watch, ResolveBegunNow);

        public static bool Busy => _held || SparrowView.Live != null || HawkView.Live != null;

        public static void BeginStage(int displayLevel)
        {
            _spawned = 0;
            _held = false;
            _readyAt = 0f;
            _collectsLeft = 0;
            _cap = CapFor(displayLevel);
            _watch = null;
            _resolveBegun = null;
            _tutorialPause = false;
            _pauseAt = 0f;
            _pauseDebt = 0f;
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
            if (_tutorialPause) return false;
            if (SolvedNow) return false;
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
