using UnityEngine;

namespace FlockFive
{
    // Shared pop-up open / close motion. Pure functions of an age in seconds that the
    // caller measures with Time.unscaledTime, so a card paces the same at 30, 60 or
    // 120 fps and keeps moving under timeScale 0. One beat at a time: the caller
    // chains beats with Beat(), and nothing here peeks at a frame count.
    public static class PopupMotion
    {
        // Frame open: starts a touch small, eases out, and settles through a soft
        // overshoot (~1.4% peak, crossing 1 at ~37% of the beat) instead of
        // the old scale-from-zero EaseOutBack punch.
        public const float EnterFrom = 0.86f;
        const float EnterBack = 1.70158f;
        // Close: shrinks a little while it fades, ease-in so it leaves gently.
        public const float ExitTo = 0.92f;

        // 0..1 progress of a beat that starts at `start` and lasts `dur`.
        public static float Beat(float age, float start, float dur)
        {
            if (dur <= 0.0001f) return age >= start ? 1f : 0f;
            return Mathf.Clamp01((age - start) / dur);
        }

        // Smooth 0..1 (smootherstep). Used for every fade so alphas never pop.
        public static float Smooth(float u)
        {
            u = Mathf.Clamp01(u);
            return u * u * u * (u * (u * 6f - 15f) + 10f);
        }

        // Frame scale while opening. 1 exactly at u >= 1 so a settled card never drifts.
        public static float EnterScale(float u)
        {
            u = Mathf.Clamp01(u);
            if (u >= 1f) return 1f;
            float v = u - 1f;
            float e = 1f + (EnterBack + 1f) * v * v * v + EnterBack * v * v;
            return Mathf.LerpUnclamped(EnterFrom, 1f, e);
        }

        // Frame alpha while opening: in over the first 40% of the scale beat.
        public static float EnterAlpha(float u) => Smooth(u / 0.40f);

        // Close beat: scale 1 -> ExitTo with an ease-in cubic.
        public static float ExitScale(float u)
        {
            u = Mathf.Clamp01(u);
            return Mathf.Lerp(1f, ExitTo, u * u * u);
        }

        // Close alpha: holds briefly, then fades to 0 by the end of the beat.
        public static float ExitAlpha(float u) => 1f - Smooth((u - 0.15f) / 0.85f);

        // Pop for a celebratory icon: bigger overshoot than a frame, still exact 1 at rest.
        public static float PopScale(float u)
        {
            u = Mathf.Clamp01(u);
            if (u >= 1f) return 1f;
            const float c1 = 1.9f;
            float v = u - 1f;
            return 1f + (c1 + 1f) * v * v * v + c1 * v * v;
        }
    }
}
