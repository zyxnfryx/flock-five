using UnityEngine;

namespace FlockFive
{
    // b127: the Hot Sauce button while the badger is frozen. Disabled (shared dim + ice badge), a tap says why with
    // one plain line and the shared deny sound (no shake, squish or burst), and it pops back the moment he thaws.
    // All state is derived from the fight each frame except the pop / toast clocks, which Reset() clears with the fight.
    public sealed class BadgerFrozenUi
    {
        public const float ToastSeconds = 2.2f;
        public const float PopSeconds = 0.40f;

        bool _wasFrozen;
        bool _popPending;
        float _popAt = -1f;
        float _toastUntil = -1f;

        // Hot Sauce is the only frozen-out power; used buttons keep their USED look.
        public static bool Disabled(BadgerFight f, BadgerPower p, bool used)
            => f != null && !used && f.Result == BadgerResult.Playing && f.PowerFrozenOut(p);

        // Frozen turn and break-free turn, in the tutorial demo and every later fight.
        public static string TapLine(BadgerFight f, BadgerPower p)
            => Disabled(f, p, false) ? BadgerCopy.HotSauceFrozen : null;

        // The tap on a disabled button. Returns the line (null when the button is live) and arms the toast.
        public string Tap(BadgerFight f, BadgerPower p, float now)
        {
            var line = TapLine(f, p);
            if (line != null) _toastUntil = now + ToastSeconds;
            return line;
        }

        public bool ToastLive(float now) => now < _toastUntil;
        public string Toast(float now) => ToastLive(now) ? BadgerCopy.HotSauceFrozen : null;

        // Call every draw with the live disabled state. A thaw queues the pop; it plays once the buttons are active
        // again (a pop during a reveal would be missed). A fight that ended clears everything.
        public void Observe(bool disabledNow, bool fightLive)
        {
            if (!fightLive) { Reset(); return; }
            if (_wasFrozen && !disabledNow) _popPending = true;
            _wasFrozen = disabledNow;
        }

        // 1 = rest. Peaks at 1.18 mid-pop. active = the buttons are tappable right now.
        public float PopScale(float now, bool active)
        {
            if (_popPending && active) { _popPending = false; _popAt = now; }
            if (_popAt < 0f) return 1f;
            float age = now - _popAt;
            if (age >= PopSeconds) { _popAt = -1f; return 1f; }
            return 1f + 0.18f * Mathf.Sin(Mathf.PI * age / PopSeconds);
        }

        public bool Popping(float now) => _popAt >= 0f && now - _popAt < PopSeconds;
        public bool PopPending => _popPending;
        public bool WasFrozen => _wasFrozen;

        // Fight end, restart, leaving the page, backgrounding: nothing may stay disabled or mid-pop.
        public void Reset()
        {
            _wasFrozen = false;
            _popPending = false;
            _popAt = -1f;
            _toastUntil = -1f;
        }

        // Break-free step gate: the glowing comb advances; the frozen Hot Sauce button runs its handler (says why)
        // and never advances; Back, other powers, other combs and blank space are eaten.
        public static bool PressPasses(Vector2 p, Rect comb, Rect hotSauce)
            => (comb.width > 2f && comb.Contains(p)) || (hotSauce.width > 2f && hotSauce.Contains(p));

        // Same rule the shared glove uses (CoachAimAway): a target in the left half is met from the right and the
        // hand points left; the right half is met from the left and the hand points right.
        public static bool PointsLeft(float targetCenterX, float screenW) => targetCenterX < screenW * 0.5f;
    }
}
