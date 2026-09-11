using UnityEngine;

namespace FlockFive
{
    public static partial class Sfx
    {
        // Pleasant garden feeder shake: soft glass/metal ring + wood bump.
        // Not coin ding, not Ching score payoff, not alarm/gong/Pavlov.
        static AudioClip MakeFeederRattle(int kind)
        {
            int k = ((kind % 20) + 20) % 20;
            float[] durs =
            {
                0.20f, 0.24f, 0.18f, 0.28f, 0.22f, 0.30f, 0.19f, 0.26f, 0.21f, 0.32f,
                0.23f, 0.27f, 0.18f, 0.29f, 0.25f, 0.31f, 0.20f, 0.26f, 0.22f, 0.28f
            };
            // Glass body mostly under ~1.2 kHz; brief soft tick may sit near ~2 kHz after LP.
            float[] glassF =
            {
                620f, 740f, 880f, 980f, 540f, 710f, 840f, 1020f, 590f, 760f,
                910f, 680f, 830f, 1080f, 560f, 790f, 940f, 650f, 870f, 990f
            };
            float[] woodF =
            {
                210f, 260f, 180f, 310f, 240f, 280f, 195f, 340f, 225f, 270f,
                190f, 300f, 250f, 320f, 205f, 285f, 235f, 355f, 215f, 295f
            };
            float[] peaks =
            {
                0.52f, 0.48f, 0.55f, 0.50f, 0.46f, 0.53f, 0.49f, 0.51f, 0.47f, 0.54f,
                0.50f, 0.48f, 0.52f, 0.49f, 0.55f, 0.47f, 0.51f, 0.53f, 0.48f, 0.50f
            };
            float dur = durs[k];
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            int seed = 44011 + k * 733;
            float glass = glassF[k];
            float wood = woodF[k];
            bool glassFirst = (k % 2) == 0;
            float glassAt = glassFirst ? 0.004f + 0.003f * (k % 3) : 0.055f + 0.012f * (k % 4);
            float woodAt = glassFirst ? 0.045f + 0.010f * ((k + 1) % 4) : 0.006f + 0.004f * (k % 3);
            // Extra light shake ticks for rattle feel (still short).
            float tick2 = glassAt + 0.035f + 0.008f * (k % 5);
            float tick3 = woodAt + 0.040f + 0.006f * ((k + 2) % 4);
            float lp = 0f;
            float shakeLp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float s = 0f;

                // Soft glass/metal ring — inharmonic partials, fast decay, no brass ding.
                float dg = t - glassAt;
                if (dg >= 0f)
                {
                    float hit = dg < 0.0018f ? dg / 0.0018f : 1f;
                    float env = hit * Mathf.Exp(-dg / 0.055f);
                    float ring = Mathf.Sin(2f * Mathf.PI * glass * dg);
                    ring += 0.28f * Mathf.Sin(2f * Mathf.PI * glass * 1.47f * dg) * Mathf.Exp(-dg / 0.028f);
                    ring += 0.12f * Mathf.Sin(2f * Mathf.PI * glass * 2.11f * dg) * Mathf.Exp(-dg / 0.014f);
                    // Soft ~2 kHz tick, heavily damped so it never reads as coin plink.
                    float tick = Mathf.Sin(2f * Mathf.PI * Mathf.Min(1880f, glass * 2.05f) * dg)
                        * Mathf.Exp(-dg / 0.008f) * 0.10f;
                    s += (ring * 0.62f + tick) * env;
                }

                // Soft wood knock / feeder pole bump — body under ~400 Hz.
                float dw = t - woodAt;
                if (dw >= 0f)
                {
                    float hit = dw < 0.0022f ? dw / 0.0022f : 1f;
                    float env = hit * Mathf.Exp(-dw / 0.048f);
                    float knock = Mathf.Sin(2f * Mathf.PI * wood * dw * (1f - 0.18f * dw));
                    knock += 0.22f * Mathf.Sin(2f * Mathf.PI * wood * 1.68f * dw) * Mathf.Exp(-dw / 0.022f);
                    float thump = Soft(ref lp, seed, i, 0.22f) * Mathf.Exp(-dw / 0.012f);
                    s += (knock * 0.55f + thump * 0.18f) * env;
                }

                // Light follow-up glass tick (shake cascade).
                float d2 = t - tick2;
                if (d2 >= 0f && tick2 < dur - 0.04f)
                {
                    float hit = d2 < 0.0014f ? d2 / 0.0014f : 1f;
                    float env = hit * Mathf.Exp(-d2 / 0.032f) * 0.42f;
                    s += Mathf.Sin(2f * Mathf.PI * (glass * 0.92f) * d2) * env;
                }

                float d3 = t - tick3;
                if (d3 >= 0f && tick3 < dur - 0.04f && (k % 3) != 0)
                {
                    float hit = d3 < 0.0016f ? d3 / 0.0016f : 1f;
                    float env = hit * Mathf.Exp(-d3 / 0.028f) * 0.28f;
                    s += Mathf.Sin(2f * Mathf.PI * (wood * 1.15f) * d3) * env;
                }

                // Band-limited shake grit — Soft, never open white noise.
                float grit = Soft(ref shakeLp, seed + 91, i, 0.12f);
                float u = t / dur;
                float shakeEnv = (u < 0.08f ? u / 0.08f : 1f) * Mathf.Pow(1f - u, 1.15f);
                s += grit * 0.10f * shakeEnv;

                data[i] = s;
            }

            // Soft lowpass ~2 kHz so glass tick stays gentle; HP to clear DC.
            float ahp = 1f - Mathf.Exp(-2f * Mathf.PI * 90f / Rate);
            float alp = 1f - Mathf.Exp(-2f * Mathf.PI * 2000f / Rate);
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
            int fadeOut = Mathf.Min(Mathf.RoundToInt(0.018f * Rate), n / 5);
            for (int i = 0; i < n; i++)
            {
                float w = 1f;
                int back = n - 1 - i;
                if (back < fadeOut) w = back / (float)fadeOut;
                data[i] = Mathf.Clamp(data[i] * g * w, -0.95f, 0.95f);
            }
            var c = AudioClip.Create("rattle" + k, n, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }

        // Sparrow collect-smack squawk — harsh short dove/yell family, not hum_sel chirp.
        static AudioClip MakeSparrowYell(int kind)
        {
            int k = ((kind % 6) + 6) % 6;
            float[] durs = { 0.28f, 0.32f, 0.26f, 0.36f, 0.30f, 0.38f };
            float[] fund =
            {
                520f, 610f, 470f, 680f, 550f, 640f
            };
            float[] peaks = { 0.62f, 0.58f, 0.66f, 0.60f, 0.64f, 0.59f };
            float dur = durs[k];
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            int seed = 55103 + k * 641;
            float f0 = fund[k];
            float lp = 0f;
            float gritLp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float u = Mathf.Clamp01(t / dur);
                // Fast attack, rough hold, then flee-tail fall.
                float hit = u < 0.04f ? u / 0.04f : 1f;
                float env = hit * Mathf.Pow(1f - u, 0.55f) * (0.72f + 0.28f * Mathf.Sin(Mathf.PI * u));
                // Descending alarmed contour — dove range, not piccolo.
                float glide = Mathf.Lerp(1.08f, 0.72f, u * u);
                float wob = 1f + 0.06f * Mathf.Sin(2f * Mathf.PI * (18f + k * 2.5f) * t);
                float f = f0 * glide * wob;
                // Harsh squawk: odd partials + AM rasp (not a clean whistle).
                float squawk = Mathf.Sin(2f * Mathf.PI * f * t);
                squawk += 0.42f * Mathf.Sin(2f * Mathf.PI * f * 1.92f * t);
                squawk += 0.18f * Mathf.Sin(2f * Mathf.PI * f * 2.85f * t);
                float rasp = 0.55f + 0.45f * Mathf.Sin(2f * Mathf.PI * (95f + 12f * k) * t);
                float form = Mathf.Sin(2f * Mathf.PI * (f * 0.48f) * t);
                float grit = Soft(ref gritLp, seed, i, 0.18f);
                float air = Soft(ref lp, seed + 17, i, 0.10f);
                // Two short yips on some variants for alarm chatter.
                float yip = 0f;
                if (k % 2 == 1)
                {
                    float y0 = 0.09f + 0.01f * k;
                    float dy = t - y0;
                    if (dy >= 0f && dy < 0.07f)
                    {
                        float ye = (dy < 0.008f ? dy / 0.008f : 1f) * Mathf.Exp(-dy / 0.035f);
                        yip = Mathf.Sin(2f * Mathf.PI * (f0 * 1.15f) * dy) * ye * 0.55f;
                    }
                }
                data[i] = (squawk * rasp * 0.70f + form * 0.16f + grit * 0.10f + air * 0.06f + yip) * env;
            }

            // Soft lowpass keeps it garden-safe (no piercing chirp edge).
            float alp = 1f - Mathf.Exp(-2f * Mathf.PI * 2400f / Rate);
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
            int fadeOut = Mathf.Min(Mathf.RoundToInt(0.022f * Rate), n / 5);
            for (int i = 0; i < n; i++)
            {
                float w = 1f;
                int back = n - 1 - i;
                if (back < fadeOut) w = back / (float)fadeOut;
                data[i] = Mathf.Clamp(data[i] * g * w, -0.95f, 0.95f);
            }
            var c = AudioClip.Create("sparrowYell" + k, n, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }
    }
}
