using UnityEngine;

namespace FlockFive
{
    // Shared punctuation for a boss special: the tell before it, the white flash
    // on a final blow, and one beat of silence after the hit. Pure clocks.
    // The honey badger calls these. An ostrich can call the same clocks later.
    // No audio and no views live here. Pitch stays at or under PitchCap.
    public static class BossBeat
    {
        public const float TellSeconds = 0.50f;
        // One beat of the honey-badger bed. MixDesk's bar is 1.6 s at 150 BPM, four beats.
        public const float BeatSeconds = 0.40f;
        public const float FlashSeconds = 0.16f;
        public const float SilenceSeconds = BeatSeconds;
        // Musical-producer cap. No clip plays back above this.
        public const float PitchCap = 1.04f;

        public static bool TellDone(float t) => t >= TellSeconds;

        public static bool FlashDone(float t) => t >= FlashSeconds;

        public static bool SilenceDone(float t) => t >= SilenceSeconds;

        // One glint, the same curve every call. Peaks at the middle of the tell.
        public static float Glint(float t)
        {
            if (t <= 0f || t >= TellSeconds) return 0f;
            float u = t / TellSeconds;
            return Mathf.Sin(u * Mathf.PI);
        }

        // One paw twitch, degrees, the same curve every call. Negative of the glint
        // so the paw dips and returns once. No seed, no variation.
        public static float PawTwitch(float t) => -7f * Glint(t);

        // White flash. Full on the hit frame, gone by FlashSeconds.
        public static float FlashAlpha(float t)
        {
            if (t < 0f || t >= FlashSeconds) return 0f;
            float u = t / FlashSeconds;
            return 1f - u * u;
        }

        public static float ClampPitch(float pitch)
        {
            if (pitch < 1f) return 1f;
            if (pitch > PitchCap) return PitchCap;
            return pitch;
        }
    }
}
