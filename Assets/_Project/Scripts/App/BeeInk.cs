using UnityEngine;

namespace FlockFive
{
    // The album bee is a painted yellow sprite. GUI multiplies it by the kind tint.
    // A dark tint (Bass Note, Onyx, Ink, Shadow) crushes both the gold and the
    // black stripes into one blob. Mul lifts the hue until the brightest channel
    // can still carry the painting. The kind color stays in the wood and the plate.
    public static class BeeInk
    {
        public const float MinPeak = 0.78f;

        public static Color Mul(Color tint)
        {
            float peak = Mathf.Max(tint.r, Mathf.Max(tint.g, tint.b));
            float a = tint.a <= 0f ? 1f : tint.a;
            if (peak < 0.02f) return new Color(MinPeak, MinPeak, MinPeak, a);
            if (peak >= MinPeak) return new Color(tint.r, tint.g, tint.b, a);
            float k = MinPeak / peak;
            float r = Mathf.Min(1f, tint.r * k);
            float g = Mathf.Min(1f, tint.g * k);
            float b = Mathf.Min(1f, tint.b * k);
            // A flat lift leaves Ink Band, Coal Stripe, Onyx Shade, and the other
            // neutral darks too close to the cream plate (under the 2.4 cutoff).
            // Green is most of the painted bee's light, so easing it darkens the
            // body and keeps the black stripes cut. The peak stays on red or blue.
            var plate = CardPlate.Face(tint, true);
            const float floor = 2.50f;
            for (int i = 0; i < 14; i++)
            {
                if (Contrast(Body(new Color(r, g, b, 1f)), plate) >= floor) break;
                bool dropG = g > 0.22f && (r + 0.001f >= MinPeak || b + 0.001f >= MinPeak || g > MinPeak);
                bool dropR = r > 0.22f && (g + 0.001f >= MinPeak || b + 0.001f >= MinPeak || r > MinPeak);
                if (dropG)
                    g *= 0.96f;
                else if (dropR)
                    r *= 0.96f;
                else
                {
                    r *= 0.96f;
                    g *= 0.96f;
                    b = Mathf.Max(b, MinPeak);
                }
                if (Mathf.Max(r, Mathf.Max(g, b)) + 0.001f < MinPeak)
                    b = MinPeak;
            }
            return new Color(r, g, b, a);
        }

        // Approximate yellow of the painted bee under the multiply. Contrast checks
        // use this, not the raw tint, because that is the color the player sees.
        public static Color Body(Color mul) =>
            new Color(mul.r * 0.95f, mul.g * 0.72f, mul.b * 0.12f, 1f);

        public static float Luma(Color c)
        {
            float Lin(float u) => u <= 0.04045f ? u / 12.92f : Mathf.Pow((u + 0.055f) / 1.055f, 2.4f);
            return 0.2126f * Lin(c.r) + 0.7152f * Lin(c.g) + 0.0722f * Lin(c.b);
        }

        public static float Contrast(Color a, Color b)
        {
            float hi = Mathf.Max(Luma(a), Luma(b));
            float lo = Mathf.Min(Luma(a), Luma(b));
            return (hi + 0.05f) / (lo + 0.05f);
        }

        public static float Peak(Color c) => Mathf.Max(c.r, Mathf.Max(c.g, c.b));
    }
}
