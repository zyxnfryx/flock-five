using UnityEngine;

namespace FlockFive
{
    public static partial class Sfx
    {
        // Hawk pest cry — deeper / longer raptor kee than SparrowYell, not hum_sel chirps.
        // Warm phone-friendly body (~180–420 Hz) with soft rasp; no piercing whistle/melody/gong.
        static AudioClip MakeHawkCry(int kind)
        {
            int k = ((kind % 8) + 8) % 8;
            float[] durs =
            {
                0.38f, 0.44f, 0.36f, 0.50f, 0.42f, 0.48f, 0.40f, 0.54f
            };
            // Body fundamentals in dove/hawk range — well under sparrow yell (~470–680).
            float[] fund =
            {
                220f, 260f, 190f, 310f, 240f, 280f, 205f, 340f
            };
            // Soft upper partial ceiling so kee never turns whistle.
            float[] raspHz =
            {
                78f, 92f, 70f, 105f, 84f, 96f, 75f, 110f
            };
            float[] peaks =
            {
                0.58f, 0.55f, 0.60f, 0.54f, 0.57f, 0.56f, 0.59f, 0.53f
            };
            float dur = durs[k];
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            int seed = 67019 + k * 557;
            float f0 = fund[k];
            float lp = 0f;
            float gritLp = 0f;
            float bodyLp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float u = Mathf.Clamp01(t / dur);
                // Soft attack, sustained kee body, gentle fade — longer than sparrow squawk.
                float hit = u < 0.06f ? u / 0.06f : 1f;
                float env = hit * Mathf.Pow(1f - u, 0.42f) * (0.78f + 0.22f * Mathf.Sin(Mathf.PI * u));
                // Light ascending-then-fall "kee-eee" contour — raptor-ish, not melody.
                float rise = u < 0.28f ? Mathf.Lerp(0.92f, 1.12f, u / 0.28f) : Mathf.Lerp(1.12f, 0.78f, (u - 0.28f) / 0.72f);
                float wob = 1f + 0.035f * Mathf.Sin(2f * Mathf.PI * (11f + k * 1.7f) * t);
                float f = f0 * rise * wob;
                // Warm scream body: odd-leaning partials kept soft + AM rasp.
                float scream = Mathf.Sin(2f * Mathf.PI * f * t);
                scream += 0.34f * Mathf.Sin(2f * Mathf.PI * f * 1.68f * t);
                scream += 0.14f * Mathf.Sin(2f * Mathf.PI * f * 2.35f * t) * Mathf.Exp(-u * 1.4f);
                float rasp = 0.62f + 0.38f * Mathf.Sin(2f * Mathf.PI * raspHz[k] * t);
                float chest = Mathf.Sin(2f * Mathf.PI * (f * 0.52f) * t);
                float grit = Soft(ref gritLp, seed, i, 0.14f);
                float air = Soft(ref lp, seed + 23, i, 0.08f);
                float body = Soft(ref bodyLp, seed + 41, i, 0.06f);
                // Occasional short second kee for variety (still one cry, not chatter).
                float echo = 0f;
                if ((k % 3) == 1)
                {
                    float e0 = 0.16f + 0.012f * k;
                    float de = t - e0;
                    if (de >= 0f && de < 0.11f)
                    {
                        float ee = (de < 0.012f ? de / 0.012f : 1f) * Mathf.Exp(-de / 0.055f);
                        echo = Mathf.Sin(2f * Mathf.PI * (f0 * 1.05f) * de) * ee * 0.42f;
                    }
                }
                data[i] = (scream * rasp * 0.68f + chest * 0.18f + grit * 0.08f + air * 0.05f + body * 0.04f + echo) * env;
            }

            // Gentle lowpass — phone-friendly, no raw open noise edge.
            float alp = 1f - Mathf.Exp(-2f * Mathf.PI * 1850f / Rate);
            float lo = 0f;
            for (int i = 0; i < n; i++)
            {
                lo += alp * (data[i] - lo);
                data[i] = lo;
            }

            float peak = 1e-6f;
            for (int i = 0; i < n; i++)
            {
                float v = Mathf.Abs(data[i]);
                if (v > peak) peak = v;
            }
            float g = peaks[k] / peak;
            int fadeOut = Mathf.Min(Mathf.RoundToInt(0.028f * Rate), n / 5);
            for (int i = 0; i < n; i++)
            {
                float w = 1f;
                int back = n - 1 - i;
                if (back < fadeOut) w = back / (float)fadeOut;
                data[i] = Mathf.Clamp(data[i] * g * w, -0.95f, 0.95f);
            }
            var c = AudioClip.Create("hawkCry" + k, n, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }
    }
}
