using UnityEngine;

namespace FlockFive
{
    // Shared poker discard shrink/pop. PokerPose scales the tossed card; DrawPokerFlames
    // uses the same window constants for the burst. Scale reaches exactly 0 so nothing is
    // left behind when the pop finishes — the slot stays empty until the inbound deal.
    public static class PokerCardAnim
    {
        // Fraction of the toss window where shrink starts, and its length. Matches the
        // long-standing PokerPose curve (pop over the last 38% after a 0.62 hang).
        public const float PopStart = 0.62f;
        public const float PopSpan = 0.38f;

        // Face stays up until this pop amount, then flips to the back for the burst.
        public const float FaceUntil = 0.88f;

        // Smooth 0..1 pop amount. tossU is the smoothstepped toss progress
        // (after u = u * u * (3 - 2 * u) in PokerPose), not raw wall time.
        public static float DiscardPop(float tossU)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((tossU - PopStart) / PopSpan));
        }

        // Linear 0..1 for confetti that wants even spread (DrawPokerFlames).
        public static float DiscardPopLinear(float tossU)
        {
            return Mathf.Clamp01((tossU - PopStart) / PopSpan);
        }

        // Scale 1 → 0. Exactly 0 when the pop finishes.
        public static float DiscardScale(float tossU)
        {
            return 1f - DiscardPop(tossU);
        }

        public static bool DiscardVisible(float tossU)
        {
            return DiscardScale(tossU) > 0f;
        }

        public static bool DiscardShowFace(float tossU)
        {
            return DiscardPop(tossU) < FaceUntil;
        }
    }
}
