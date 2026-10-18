using UnityEngine;

namespace FlockFive
{
    // One timeline for the ostrich's head strike. Pure math, no scene objects.
    // wind-up (head pulls back, away from the bird) -> strike (fast, accelerating)
    // -> contact hold -> recoil (eased, small overshoot back) -> settle.
    // Contact lands at 0.42s, the same moment the Peck frames open the beak (frame 5 at 12 fps).
    public static class OstrichStrike
    {
        public const float Wind = 0.30f;
        public const float Hit = 0.12f;
        public const float Hold = 0.10f;
        public const float Recoil = 0.32f;
        public const float Settle = 0.22f;
        public const float Contact = Wind + Hit;
        public const float Total = Wind + Hit + Hold + Recoil + Settle;
        // Head pulls back this far (world units) at the end of the wind-up.
        public const float PullBack = 0.55f;
        // Small undershoot past rest during recoil, as a fraction of the reach.
        public const float Undershoot = 0.10f;
        // Draw orders while striking: head over every bird near the target
        // (perched 12, hop 40), neck over the branch wood but under the perched flock.
        public const int HeadOrder = 41;
        public const int NeckOrder = 9;

        static float Ease(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

        // -1 = fully pulled back, 0 = rest, 1 = on the bird.
        public static float Reach(float t)
        {
            if (t <= 0f) return 0f;
            if (t < Wind) return -Ease(t / Wind);
            if (t < Contact)
            {
                float u = (t - Wind) / Hit;
                return Mathf.Lerp(-1f, 1f, u * u);
            }
            if (t < Contact + Hold) return 1f;
            float r0 = Contact + Hold;
            if (t < r0 + Recoil)
                return Mathf.Lerp(1f, -Undershoot, Ease((t - r0) / Recoil));
            float s0 = r0 + Recoil;
            if (t < Total) return Mathf.Lerp(-Undershoot, 0f, Ease((t - s0) / Settle));
            return 0f;
        }

        public static Vector3 HeadAt(Vector3 rest, Vector3 target, float t)
        {
            float r = Reach(t);
            if (r >= 0f) return Vector3.LerpUnclamped(rest, target, r);
            var away = rest - target;
            away.z = 0f;
            if (away.sqrMagnitude < 1e-6f) away = Vector3.up;
            return rest + away.normalized * (-r * PullBack);
        }

        // Which part of the beat t is in: 0 wind-up, 1 strike, 2 hold, 3 recoil, 4 settle, 5 done.
        public static int Phase(float t)
        {
            if (t < Wind) return 0;
            if (t < Contact) return 1;
            if (t < Contact + Hold) return 2;
            if (t < Contact + Hold + Recoil) return 3;
            if (t < Total) return 4;
            return 5;
        }
    }
}
