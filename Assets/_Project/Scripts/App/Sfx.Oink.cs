using UnityEngine;

namespace FlockFive
{
    public static partial class Sfx
    {
        // Splash pig poke — cute soft snort-oink. Not Clink/Ching/gong/alarm/HawkCry/SparrowYell.
        // Low-mid body (~150–350 Hz) + soft nasal chiff; Soft grit; gentle lowpass.
        static AudioClip MakeOink(int kind)
        {
            int k = ((kind % 8) + 8) % 8;
            float[] durs =
            {
                0.14f, 0.18f, 0.12f, 0.22f, 0.16f, 0.26f, 0.15f, 0.24f
            };
            // Pig body fundamentals — warm phone-friendly mid, never piercing.
            float[] fund =
            {
                180f, 220f, 155f, 280f, 200f, 250f, 170f, 310f
            };
            // Soft nasal chiff (brief, damped) — cute oink color, not whistle.
            float[] nasal =
            {
                920f, 1080f, 860f, 1180f, 980f, 1120f, 900f, 1240f
            };
            // Snort vs tonal oink balance (higher = more Soft snort grit).
            float[] snort =
            {
                0.42f, 0.22f, 0.55f, 0.18f, 0.38f, 0.28f, 0.48f, 0.20f
            };
            float[] peaks =
            {
                0.54f, 0.52f, 0.56f, 0.50f, 0.53f, 0.51f, 0.55f, 0.52f
            };
            float dur = durs[k];
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            int seed = 88027 + k * 613;
            float f0 = fund[k];
            float sn = snort[k];
            float lp = 0f;
            float gritLp = 0f;
            float bodyLp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float u = Mathf.Clamp01(t / dur);
                // Soft plump attack, short cute hold, gentle fade.
                float hit = u < 0.08f ? u / 0.08f : 1f;
                float env = hit * Mathf.Pow(1f - u, 0.70f) * (0.82f + 0.18f * Mathf.Sin(Mathf.PI * u));

                // Pitch contour variety: little up-oink, down-snort, or flat cute.
                float contour;
                int shape = k % 4;
                if (shape == 0)
                    contour = Mathf.Lerp(0.94f, 1.10f, Mathf.SmoothStep(0f, 1f, u)); // soft rise
                else if (shape == 1)
                    contour = Mathf.Lerp(1.08f, 0.88f, Mathf.SmoothStep(0f, 1f, u)); // soft fall
                else if (shape == 2)
                    contour = u < 0.45f
                        ? Mathf.Lerp(0.96f, 1.12f, u / 0.45f)
                        : Mathf.Lerp(1.12f, 0.90f, (u - 0.45f) / 0.55f); // tiny bump
                else
                    contour = 1f + 0.04f * Mathf.Sin(2f * Mathf.PI * 1.2f * u); // gentle wobble

                float wob = 1f + 0.025f * Mathf.Sin(2f * Mathf.PI * (9f + k * 1.4f) * t);
                float f = f0 * contour * wob;

                // Warm oink body — odd-leaning soft partials, never brass ding.
                float oink = Mathf.Sin(2f * Mathf.PI * f * t);
                oink += 0.28f * Mathf.Sin(2f * Mathf.PI * f * 1.55f * t);
                oink += 0.10f * Mathf.Sin(2f * Mathf.PI * f * 2.15f * t) * Mathf.Exp(-u * 2.2f);

                // Soft nasal chiff — short front chiff only.
                float chiffEnv = Mathf.Exp(-t / 0.028f) * hit;
                float chiff = Mathf.Sin(2f * Mathf.PI * nasal[k] * t) * chiffEnv * 0.16f;
                chiff += 0.06f * Mathf.Sin(2f * Mathf.PI * nasal[k] * 1.35f * t) * Mathf.Exp(-t / 0.014f);

                // Soft snort grit (band-limited Soft, never open white noise).
                float grit = Soft(ref gritLp, seed, i, 0.16f);
                float air = Soft(ref lp, seed + 19, i, 0.09f);
                float chest = Soft(ref bodyLp, seed + 37, i, 0.05f);
                float snortBurst = (grit * 0.55f + air * 0.30f + chest * 0.15f)
                    * Mathf.Exp(-t / 0.045f) * (0.55f + 0.45f * hit);

                // Occasional tiny second snort tick for variety (still one poke).
                float tick = 0f;
                if ((k % 3) == 2)
                {
                    float t0 = 0.055f + 0.008f * k;
                    float dt = t - t0;
                    if (dt >= 0f && dt < 0.06f)
                    {
                        float te = (dt < 0.008f ? dt / 0.008f : 1f) * Mathf.Exp(-dt / 0.028f);
                        tick = Mathf.Sin(2f * Mathf.PI * (f0 * 1.08f) * dt) * te * 0.35f;
                        tick += Soft(ref gritLp, seed + 71, i, 0.10f) * te * 0.12f;
                    }
                }

                float tonal = 1f - sn * 0.55f;
                data[i] = (oink * 0.62f * tonal + chiff + snortBurst * sn + tick) * env;
            }

            // Gentle HP + lowpass — soft phone-friendly, no raw edge / coin plink.
            float ahp = 1f - Mathf.Exp(-2f * Mathf.PI * 80f / Rate);
            float alp = 1f - Mathf.Exp(-2f * Mathf.PI * 1750f / Rate);
            float hp = 0f, lo = 0f;
            for (int i = 0; i < n; i++)
            {
                hp += ahp * (data[i] - hp);
                float high = data[i] - hp;
                lo += alp * (high - lo);
                data[i] = lo;
            }

            float peak = 1e-6f;
            for (int i = 0; i < n; i++)
            {
                float v = Mathf.Abs(data[i]);
                if (v > peak) peak = v;
            }
            float g = peaks[k] / peak;
            int fadeOut = Mathf.Min(Mathf.RoundToInt(0.016f * Rate), n / 5);
            for (int i = 0; i < n; i++)
            {
                float w = 1f;
                int back = n - 1 - i;
                if (back < fadeOut) w = back / (float)fadeOut;
                data[i] = Mathf.Clamp(data[i] * g * w, -0.95f, 0.95f);
            }
            var c = AudioClip.Create("oink" + k, n, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }
    }
}
