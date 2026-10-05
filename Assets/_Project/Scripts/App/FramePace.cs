using System;

namespace FlockFive
{
    // Frame pacing rules and frame-rate-independent easing. Pure (System only) so the
    // test harness can check them; FlockFiveApp.Preload.cs applies the target.
    public static class FramePace
    {
        // Every per-frame constant in the app was tuned at 60 fps.
        public const int Base = 60;
        // Lowest rate the default picker accepts before it prefers a higher even rate.
        public const int Floor = 59;
        // Top rate the high-refresh picker allows (ProMotion / 120 Hz Android).
        public const int HighCap = 120;

        // Opt-in: run at the display's top rate. Off by default for battery and heat.
        // iOS also needs Player Settings > Enable ProMotion for the panel to report 120.
        public static bool HighRefresh = false;

        // Frame rate to ask for on a panel refreshing at refreshHz.
        // The target is always the panel rate divided by a whole number, so every frame
        // is held for the same number of refreshes (no 2-1-2-1 judder).
        //   default: the lowest even rate that is still about 60 (60 Hz and 120 Hz -> 60,
        //            90 Hz -> 90, 144 Hz -> 72, 50 Hz -> 50).
        //   high:    the highest even rate up to 120 (120 Hz -> 120, 90 Hz -> 90,
        //            144 Hz -> 72, 60 Hz -> 60).
        // Unknown refresh (0, NaN) falls back to 60.
        public static int Target(double refreshHz, bool high)
        {
            if (double.IsNaN(refreshHz) || double.IsInfinity(refreshHz) || refreshHz < 20.0) return Base;
            if (high)
            {
                for (int n = 1; n <= 8; n++)
                {
                    double r = refreshHz / n;
                    if (r <= HighCap + 0.5) return (int)Math.Round(r);
                }
                return Base;
            }
            if (refreshHz < Floor) return (int)Math.Round(refreshHz);
            int best = (int)Math.Round(refreshHz);
            for (int n = 2; n <= 8; n++)
            {
                double r = refreshHz / n;
                if (r < Floor) break;
                best = (int)Math.Round(r);
            }
            return best;
        }

        // Per-frame multiply tuned at 60 fps (vel *= k each frame), made frame-rate
        // independent: the same decay per second at 30, 60, 90 or 120 fps.
        public static float Damp(float perFrameAt60, float dt)
        {
            if (dt <= 0f) return 1f;
            if (perFrameAt60 <= 0f) return 0f;
            return (float)Math.Pow(perFrameAt60, dt * Base);
        }

        // Per-frame lerp fraction tuned at 60 fps (x = Lerp(x, goal, t) each frame),
        // made frame-rate independent.
        public static float Follow(float perFrameAt60, float dt)
        {
            if (dt <= 0f) return 0f;
            if (perFrameAt60 >= 1f) return 1f;
            return 1f - (float)Math.Pow(1f - perFrameAt60, dt * Base);
        }
    }
}
