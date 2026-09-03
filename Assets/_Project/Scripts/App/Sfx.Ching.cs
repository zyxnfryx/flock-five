using UnityEngine;

namespace FlockFive
{
    public static partial class Sfx
    {
        static AudioClip MakeChing(int kind)
        {
            // Garden till: wood body + muted brass + coin tap. Unique voicing per clip.
            float[] durs = { 0.188f, 0.201f, 0.214f, 0.226f, 0.239f, 0.251f, 0.263f, 0.275f, 0.287f, 0.299f, 0.311f, 0.318f };
            float[] peaks = { 0.47f, 0.50f, 0.46f, 0.53f, 0.49f, 0.51f, 0.48f, 0.52f, 0.54f, 0.45f, 0.55f, 0.50f };
            const float A3 = 220f, D4 = 293.66f, E4 = 329.63f, Fs4 = 369.99f, A4 = 440f;
            const float D5 = 587.33f, Fs5 = 739.99f;
            int k = ((kind % 12) + 12) % 12;
            float dur = durs[k];
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            int seed = 11011 + k * 1013;

            void Mix(float start, System.Action<int, float, float> add)
            {
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)Rate - start;
                    if (t < 0f) continue;
                    add(i, t, i / (float)Rate);
                }
            }

            float Env(float t, float atk, float tau)
            {
                float a = atk <= 0f ? 1f : Mathf.Clamp01(t / atk);
                return a * Mathf.Exp(-t / Mathf.Max(tau, 1e-5f));
            }

            void Wood(int i, float t, float f, float amp, float atk, float tau)
            {
                float e = Env(t, atk, tau);
                float ft = f * (1f - 0.12f * Mathf.Clamp01(t / 0.08f));
                float ph = 2f * Mathf.PI * ft * t;
                float s = Mathf.Sin(ph) + 0.18f * Mathf.Sin(2f * ph) + 0.06f * Mathf.Sin(3f * ph);
                data[i] += amp * e * s;
            }

            void Brass(int i, float t, float f, float amp, float atk, float tau, float bright)
            {
                float e = Env(t, atk, tau);
                float ft = f * (1f - 0.025f * Mathf.Clamp01(t / Mathf.Max(tau * 3f, 1e-3f)));
                float ph = 2f * Mathf.PI * ft * t;
                float s = Mathf.Sin(ph);
                s += bright * 0.55f * Mathf.Sin(2f * ph);
                s += bright * 0.12f * Mathf.Sin(3f * ph);
                s += bright * 0.04f * Mathf.Sin(2f * Mathf.PI * (f * 1.5f) * t);
                data[i] += amp * e * s;
            }

            void Coin(int i, float t, float f, float amp, float atk, float tau)
            {
                float e = Env(t, atk, tau);
                float s = 0f;
                float[] ratios = { 1f, 1.83f, 2.41f, 3.17f };
                float[] amps = { 1f, 0.38f, 0.16f, 0.07f };
                for (int m = 0; m < 4; m++)
                {
                    float ff = f * ratios[m];
                    if (ff > 3200f) continue;
                    float tauM = ratios[m] > 1.5f ? tau * 0.55f : tau;
                    s += amps[m] * Env(t, atk, tauM) * Mathf.Sin(2f * Mathf.PI * ff * t);
                }
                data[i] += amp * s * (0.65f + 0.35f * e);
            }

            void Click(float start, float amp, float lo, float hi, float cd)
            {
                float lp = 0f;
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)Rate - start;
                    if (t < 0f || t >= cd) continue;
                    float e = Mathf.Exp(-t / (cd * 0.35f)) * Mathf.Clamp01(t / Mathf.Min(0.0015f, cd * 0.2f));
                    float nz = Soft(ref lp, seed, i, 0.28f);
                    data[i] += amp * nz * e;
                }
            }

            switch (k)
            {
                case 0:
                    Mix(0f, (i, t, _) => Wood(i, t, D4, 0.72f, 0.0025f, 0.055f));
                    Click(0f, 0.38f, 650f, 1900f, 0.011f);
                    Mix(0.016f, (i, t, _) => Brass(i, t, A4, 0.55f, 0.003f, 0.062f, 0.16f));
                    Mix(0.042f, (i, t, _) => Coin(i, t, D5, 0.34f, 0.002f, 0.048f));
                    break;
                case 1:
                    Mix(0f, (i, t, _) => Brass(i, t, Fs4, 0.58f, 0.0018f, 0.022f, 0.28f));
                    Click(0.001f, 0.30f, 800f, 2100f, 0.009f);
                    Mix(0.018f, (i, t, _) => Wood(i, t, D4, 0.42f, 0.003f, 0.048f));
                    Mix(0.038f, (i, t, _) => Coin(i, t, D5, 0.48f, 0.0018f, 0.055f));
                    break;
                case 2:
                    Mix(0f, (i, t, _) => Wood(i, t, E4, 0.68f, 0.0022f, 0.05f));
                    Click(0f, 0.34f, 720f, 2000f, 0.01f);
                    Mix(0.012f, (i, t, _) => Brass(i, t, E4, 0.28f, 0.0025f, 0.04f, 0.14f));
                    Mix(0.034f, (i, t, _) => Coin(i, t, A4, 0.50f, 0.0022f, 0.058f));
                    break;
                case 3:
                    Mix(0f, (i, t, _) => Wood(i, t, D4, 0.80f, 0.0035f, 0.07f));
                    Mix(0f, (i, t, _) => Wood(i, t, A3, 0.28f, 0.004f, 0.08f));
                    Click(0f, 0.42f, 500f, 1600f, 0.014f);
                    Mix(0.02f, (i, t, _) => Brass(i, t, D4, 0.22f, 0.0018f, 0.022f, 0.28f));
                    Mix(0.052f, (i, t, _) => Coin(i, t, Fs4, 0.46f, 0.0024f, 0.06f));
                    break;
                case 4:
                    Mix(0f, (i, t, _) => Brass(i, t, A4, 0.40f, 0.0028f, 0.048f, 0.12f));
                    Click(0f, 0.22f, 900f, 2300f, 0.008f);
                    Mix(0.022f, (i, t, _) => Wood(i, t, D4, 0.50f, 0.003f, 0.055f));
                    Mix(0.058f, (i, t, _) => Coin(i, t, E4, 0.36f, 0.002f, 0.052f));
                    break;
                case 5:
                    Mix(0f, (i, t, _) => Coin(i, t, D5, 0.46f, 0.0016f, 0.045f));
                    Click(0.002f, 0.22f, 1000f, 2400f, 0.007f);
                    Mix(0.04f, (i, t, _) => Wood(i, t, A3, 0.55f, 0.0032f, 0.06f));
                    Mix(0.048f, (i, t, _) => Brass(i, t, A4, 0.42f, 0.0026f, 0.055f, 0.15f));
                    break;
                case 6:
                    Mix(0f, (i, t, _) => Brass(i, t, D4, 0.48f, 0.003f, 0.058f, 0.14f));
                    Mix(0f, (i, t, _) => Brass(i, t, A4, 0.36f, 0.0034f, 0.052f, 0.12f));
                    Click(0f, 0.32f, 600f, 1800f, 0.012f);
                    Mix(0f, (i, t, _) => Wood(i, t, D4, 0.38f, 0.003f, 0.05f));
                    Mix(0.05f, (i, t, _) => Coin(i, t, Fs5, 0.36f, 0.002f, 0.042f));
                    break;
                case 7:
                    Mix(0f, (i, t, _) => Wood(i, t, D4, 0.58f, 0.002f, 0.045f));
                    Click(0f, 0.30f, 750f, 2000f, 0.009f);
                    Mix(0.016f, (i, t, _) => Brass(i, t, E4, 0.40f, 0.0022f, 0.038f, 0.15f));
                    Mix(0.034f, (i, t, _) => Coin(i, t, Fs4, 0.42f, 0.0018f, 0.048f));
                    Mix(0.03f, (i, t, _) => Wood(i, t, A3, 0.18f, 0.004f, 0.055f));
                    break;
                case 8:
                    Mix(0f, (i, t, _) => Wood(i, t, D4, 0.78f, 0.004f, 0.075f));
                    Mix(0f, (i, t, _) => Wood(i, t, A3, 0.30f, 0.005f, 0.085f));
                    Click(0f, 0.44f, 480f, 1500f, 0.016f);
                    Mix(0.018f, (i, t, _) => Brass(i, t, E4, 0.20f, 0.0018f, 0.022f, 0.28f));
                    Mix(0.078f, (i, t, _) => Coin(i, t, A4, 0.48f, 0.0024f, 0.062f));
                    break;
                case 9:
                    Mix(0f, (i, t, _) => Brass(i, t, E4, 0.50f, 0.0026f, 0.05f, 0.16f));
                    Mix(0f, (i, t, _) => Wood(i, t, E4, 0.48f, 0.003f, 0.055f));
                    Click(0f, 0.33f, 680f, 1950f, 0.01f);
                    Mix(0.044f, (i, t, _) => Coin(i, t, D5, 0.46f, 0.002f, 0.052f));
                    break;
                case 10:
                    Mix(0f, (i, t, _) => Brass(i, t, Fs4, 0.56f, 0.0024f, 0.048f, 0.17f));
                    Click(0f, 0.26f, 820f, 2150f, 0.008f);
                    Mix(0.03f, (i, t, _) => Coin(i, t, A4, 0.44f, 0.002f, 0.05f));
                    Mix(0.082f, (i, t, _) => Wood(i, t, D4, 0.28f, 0.006f, 0.07f));
                    break;
                default:
                    Mix(0f, (i, t, _) => Wood(i, t, D4, 0.70f, 0.0028f, 0.058f));
                    Click(0f, 0.36f, 620f, 1850f, 0.012f);
                    Mix(0.014f, (i, t, _) => Brass(i, t, D4, 0.18f, 0.0018f, 0.022f, 0.28f));
                    Mix(0.042f, (i, t, _) => Brass(i, t, A4, 0.40f, 0.0028f, 0.055f, 0.14f));
                    Mix(0.044f, (i, t, _) => Coin(i, t, Fs4, 0.42f, 0.0022f, 0.05f));
                    break;
            }

            float hp = 0f, lp = 0f, mid = 0f;
            float ahp = 1f - Mathf.Exp(-2f * Mathf.PI * 180f / Rate);
            float alp = 1f - Mathf.Exp(-2f * Mathf.PI * 4600f / Rate);
            float amid = 1f - Mathf.Exp(-2f * Mathf.PI * 1000f / Rate);
            float notch = k == 4 ? 0.55f : 0.28f;
            for (int i = 0; i < n; i++)
            {
                hp += ahp * (data[i] - hp);
                float high = data[i] - hp;
                lp += alp * (high - lp);
                mid += amid * (lp - mid);
                data[i] = lp - notch * mid;
            }
            int fi = Mathf.Max(1, Mathf.RoundToInt(0.0015f * Rate));
            int fo = Mathf.Max(1, Mathf.RoundToInt(0.024f * Rate));
            for (int i = 0; i < fi; i++) data[i] *= i / (float)fi;
            for (int i = 0; i < fo; i++) data[n - 1 - i] *= i / (float)Mathf.Max(1, fo - 1);
            float peak = 1e-6f;
            for (int i = 0; i < n; i++)
            {
                float v = Mathf.Abs(data[i]);
                if (v > peak) peak = v;
            }
            float g = peaks[k] / peak;
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(data[i] * g, -0.95f, 0.95f);
            var c = AudioClip.Create("ching" + k, n, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }
    }
}
