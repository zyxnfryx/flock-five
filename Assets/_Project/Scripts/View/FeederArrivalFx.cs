namespace FlockFive
{
    // Build 84 (Playtest FEEDER_ARRIVAL_b83.md, Brandon approved): the feeder rewards each
    // arriving bird and the finished set. One entry point for every clean-collect call site.
    //  - Per bird: squash-bounce, peak about 1.12x over 0.14s, with the existing Pop sound.
    //  - Set complete: bigger pop, about 1.22x over 0.30s (short spin, warm flash, 4 glints) and
    //    the existing Ching, finished before RetireFeeder pulls the feeder away (0.15s + 0.16s).
    // Motion lives in FeederView.LateUpdate on scaled time (follows the 1.44x auto-resolve),
    // and repeated calls refresh one impulse instead of stacking coroutines. No haptics here:
    // the collect already fired its combo/branch haptic. Pest scraps keep Wow.Burst and do not
    // get the set pop.
    public static class FeederArrivalFx
    {
        public const float BirdPeak = 1.12f, BirdSec = 0.14f;
        public const float SetPeak = 1.22f, SetSec = 0.30f;
        public const float SetLead = 0.15f; // Collect waits this, then RetireFeeder's own 0.16s

        // quiet: finale live or hive inspect open -> only the old pop + tick, no extra effects.
        public static void Play(FeederView feeder, int birdsIn, bool setComplete, bool quiet = false)
        {
            if (!setComplete)
            {
                Sfx.ScorePop(birdsIn - 1);
                if (feeder == null) return;
                feeder.ScoreTick();
                if (!quiet) feeder.Squash();
                return;
            }
            if (quiet || feeder == null) return;
            feeder.SetPop();
            Sfx.FeederDone();
        }
    }
}
