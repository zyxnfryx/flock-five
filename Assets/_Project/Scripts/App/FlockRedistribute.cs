using UnityEngine;

namespace FlockFive
{
    // Shared perch after a pest scrap (sparrow, hawk, and anything else that
    // calls PerchOnRemain). Every bird leaves at t = 0. Later birds travel
    // longer, so arrivals stagger without an idle gap. The sample is the
    // position: the last one is the seat, and nothing teleports onto it.
    public static class FlockRedistribute
    {
        public const float BaseDuration = 0.62f;
        public const float Stagger = 0.045f;
        public const float Lift = 1.15f;

        public static float Duration(int index)
        {
            if (index < 0) index = 0;
            return BaseDuration + index * Stagger;
        }

        // Small ease-out back. f(0) = 0, f(1) = 1, f'(1) = 0, f'(0) > 0.
        public static float Ease(float u)
        {
            if (u <= 0f) return 0f;
            if (u >= 1f) return 1f;
            const float c1 = 0.6f;
            const float c3 = c1 + 1f;
            float t = u - 1f;
            return 1f + c3 * t * t * t + c1 * t * t;
        }

        public static Vector3 Sample(Vector3 start, Vector3 dest, int index, float time)
        {
            float dur = Duration(index);
            if (dur < 0.05f) dur = 0.05f;
            if (time >= dur) return dest;
            if (time <= 0f) return start;
            float u = time / dur;
            float e = Ease(u);
            var p = Vector3.LerpUnclamped(start, dest, e);
            float arc = Mathf.Sin(u * Mathf.PI);
            p.y += arc * arc * Lift;
            return p;
        }

        // 0 in cruise and exactly at the landing. Peaks in the last fifth.
        public static float Sway(int index, float time)
        {
            float dur = Duration(index);
            if (time <= 0f || time >= dur) return 0f;
            float u = time / dur;
            if (u < 0.8f) return 0f;
            float w = (u - 0.8f) / 0.2f;
            return Mathf.Sin(w * Mathf.PI);
        }
    }
}
