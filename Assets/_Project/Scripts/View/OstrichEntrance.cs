using UnityEngine;

namespace FlockFive
{
    // Build 121. The ostrich is a pest, not a boss, and he used to be standing in the
    // garden on the first frame. This is the arrival before he is in play: the stand
    // fades in, he honks from offscreen, then he strides in from the left (the painted
    // bird faces right, so the walk reads) and plants. Taps stay gated until Total.
    // Pure math so the edit-mode suite can play the whole beat without a scene.
    public static class OstrichEntrance
    {
        public const float LedgeIn = 0.42f;       // empty stand appears before he does
        public const float HonkAt = 0.20f;        // warning honk while he is still offstage
        public const float StrideStart = 0.50f;
        public const float StrideSec = 1.05f;
        public const float PlantSec = 0.42f;      // weight settles onto the ledge
        public const float Total = StrideStart + StrideSec + PlantSec;

        public static bool GatesTaps(float t) => t >= 0f && t < Total;
        public static bool BirdVisible(float t) => t >= StrideStart;
        public static bool InPlay(float t) => t >= Total;

        public static float LedgeWeight(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= LedgeIn) return 1f;
            float u = t / LedgeIn;
            return u * u * (3f - 2f * u);
        }

        public static float StrideU(float t)
        {
            if (t <= StrideStart) return 0f;
            if (t >= StrideStart + StrideSec) return 1f;
            float u = (t - StrideStart) / StrideSec;
            return u * u * (3f - 2f * u);
        }

        public static float PlantU(float t)
        {
            float p0 = StrideStart + StrideSec;
            if (t <= p0) return 0f;
            if (t >= Total) return 1f;
            return Mathf.Clamp01((t - p0) / PlantSec);
        }

        // Neck-root x. Starts fully off the left edge and eases to the planted body x.
        public static float BodyX(float t, float plantX, float half, float bodyFront)
        {
            float start = -half - bodyFront - 0.55f;
            return Mathf.Lerp(start, plantX, StrideU(t));
        }

        // Downward press during the plant, back to 0 when he is in play. Feet stay on
        // the ledge through the stride (this is 0 until the plant beat).
        public static float Stomp(float t)
        {
            float u = PlantU(t);
            if (u <= 0f || u >= 1f) return 0f;
            return -0.08f * Mathf.Sin(u * Mathf.PI);
        }

        public static int StrutFrame(float t)
        {
            int n = OstrichAnim.StrutFrames;
            if (n < 1) return 0;
            float u = StrideU(t);
            return Mathf.Clamp(Mathf.FloorToInt(u * (n * 2f - 1f)), 0, n - 1);
        }
    }
}
