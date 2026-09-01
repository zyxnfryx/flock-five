using UnityEngine;

namespace FlockFive
{
    public static partial class Sfx
    {
        static AudioClip MakeChirp(int seed)
        {
            float f0 = Mathf.Lerp(380f, 620f, (Hash(seed) + 1f) * 0.5f);
            float f1 = f0 * Mathf.Lerp(1.06f, 1.18f, (Hash(seed + 3) + 1f) * 0.5f);
            float dur = Mathf.Lerp(0.12f, 0.2f, (Hash(seed + 5) + 1f) * 0.5f);
            float slide = Mathf.Lerp(0.03f, 0.08f, (Hash(seed + 7) + 1f) * 0.5f);
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float u = t / dur;
                float env = u < 0.12f ? u / 0.12f : Mathf.Pow(1f - (u - 0.12f) / 0.88f, 1.35f);
                float f = Mathf.Lerp(f0, f1, Mathf.SmoothStep(0f, 1f, u));
                f *= 1f + slide * Mathf.Sin(t * 12f);
                float s = Mathf.Sin(2f * Mathf.PI * f * t);
                s += 0.12f * Mathf.Sin(4f * Mathf.PI * f * t);
                data[i] = s * env * 0.26f;
            }
            return Clip("chirp" + seed, data);
        }

        static AudioClip MakeFlap(int kind, int seed)
        {
            float dur = Mathf.Lerp(0.07f, 0.16f, (Hash(seed) + 1f) * 0.5f);
            float thump = Mathf.Lerp(88f, 190f, (Hash(seed + 2) + 1f) * 0.5f);
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float u = t / dur;
                float noise = Soft(ref lp, seed * 17, i, 0.12f);
                float s;
                switch (kind % 4)
                {
                    case 0:
                    {
                        float e1 = Mathf.Exp(-u * 14f) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(u / 0.45f));
                        float e2 = Mathf.Exp(-(u - 0.38f) * 16f) * Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Clamp01((u - 0.32f) / 0.5f)));
                        float env = e1 * 0.7f + e2 * 0.55f;
                        s = noise * env * 0.22f + Mathf.Sin(2f * Mathf.PI * thump * t) * env * 0.72f;
                        break;
                    }
                    case 1:
                    {
                        float a = Mathf.Exp(-u * 16f) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(u / 0.32f));
                        float b = Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Clamp01((u - 0.36f) / 0.4f))) * Mathf.Exp(-(u - 0.36f) * 14f);
                        float env = a * 0.75f + b * 0.7f;
                        s = noise * env * 0.18f + Mathf.Sin(2f * Mathf.PI * thump * 1.15f * t) * env * 0.75f;
                        break;
                    }
                    case 2:
                    {
                        float env = Mathf.Pow(Mathf.Sin(Mathf.PI * u), 0.7f) * Mathf.Exp(-u * 6f);
                        s = noise * env * 0.28f + Mathf.Sin(2f * Mathf.PI * (thump * 0.55f) * t) * env * 0.55f;
                        break;
                    }
                    default:
                    {
                        float env = Mathf.Exp(-u * 22f) * (u < 0.05f ? u / 0.05f : 1f);
                        s = noise * env * 0.2f + Mathf.Sin(2f * Mathf.PI * (thump * 1.4f) * t) * env * 0.72f;
                        break;
                    }
                }
                data[i] = s * 0.34f;
            }
            return Clip("flap" + seed, data);
        }

        static float Pulse(float freq, float t, float duty)
        {
            float p = freq * t;
            p -= Mathf.Floor(p);
            return p < duty ? 1f : -1f;
        }

        static void PutSweep(float[] data, float start, float len, float f0, float f1, float amp, float duty)
        {
            int n = data.Length;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate - start;
                if (t < 0f || t > len) continue;
                float u = t / len;
                float env = (u < 0.06f ? u / 0.06f : 1f) * Mathf.Pow(1f - u, 1.25f);
                float f = f0 * Mathf.Pow(f1 / Mathf.Max(1f, f0), u);
                data[i] += Pulse(f, t, duty) * env * amp;
            }
        }

        static void PutThump(float[] data, float start, float f0, float amp)
        {
            int n = data.Length;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate - start;
                if (t < 0f || t > 0.12f) continue;
                float u = t / 0.12f;
                float env = (u < 0.04f ? u / 0.04f : 1f) * Mathf.Exp(-u * 9f);
                float f = f0 * (1f - 0.45f * u);
                data[i] += Pulse(f, t, 0.5f) * env * amp;
            }
        }

        static void PutClick(float[] data, float start, int seed, float amp)
        {
            int n = data.Length;
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate - start;
                if (t < 0f || t > 0.018f) continue;
                float env = 1f - t / 0.018f;
                float nz = Soft(ref lp, seed, i, 0.55f);
                data[i] += nz * env * amp;
            }
        }

        static AudioClip MakeCelebrate(int kind, int seed)
        {
            // 8-bit NES jump-hit: pulse sweep + body thump. Harder than a sine arpeggio.
            float dur = 0.42f;
            if (kind == 2 || kind == 6 || kind == 9) dur = 0.52f;
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            switch (kind % 12)
            {
                case 0:
                    PutClick(data, 0f, seed, 0.35f);
                    PutThump(data, 0f, 92f, 0.55f);
                    PutSweep(data, 0.012f, 0.22f, 196f, 523f, 0.48f, 0.25f);
                    break;
                case 1:
                    PutClick(data, 0f, seed + 3, 0.3f);
                    PutThump(data, 0f, 74f, 0.62f);
                    PutSweep(data, 0.01f, 0.26f, 147f, 392f, 0.5f, 0.25f);
                    break;
                case 2:
                    PutThump(data, 0f, 88f, 0.5f);
                    PutSweep(data, 0.01f, 0.16f, 175f, 440f, 0.42f, 0.25f);
                    PutClick(data, 0.18f, seed + 7, 0.28f);
                    PutThump(data, 0.18f, 100f, 0.42f);
                    PutSweep(data, 0.19f, 0.2f, 220f, 523f, 0.46f, 0.25f);
                    break;
                case 3:
                    PutClick(data, 0f, seed + 11, 0.4f);
                    PutThump(data, 0f, 82f, 0.58f);
                    PutSweep(data, 0.008f, 0.18f, 131f, 262f, 0.5f, 0.5f);
                    PutSweep(data, 0.05f, 0.16f, 262f, 523f, 0.32f, 0.25f);
                    break;
                case 4:
                    PutThump(data, 0f, 96f, 0.5f);
                    PutSweep(data, 0.00f, 0.07f, 196f, 196f, 0.42f, 0.25f);
                    PutSweep(data, 0.07f, 0.07f, 262f, 262f, 0.44f, 0.25f);
                    PutSweep(data, 0.14f, 0.16f, 330f, 330f, 0.46f, 0.25f);
                    break;
                case 5:
                    PutClick(data, 0f, seed + 17, 0.32f);
                    PutThump(data, 0f, 70f, 0.6f);
                    PutSweep(data, 0.01f, 0.12f, 165f, 440f, 0.48f, 0.125f);
                    PutSweep(data, 0.12f, 0.16f, 440f, 247f, 0.36f, 0.125f);
                    break;
                case 6:
                    PutThump(data, 0f, 85f, 0.45f);
                    PutSweep(data, 0.00f, 0.09f, 196f, 330f, 0.4f, 0.25f);
                    PutSweep(data, 0.10f, 0.09f, 220f, 370f, 0.42f, 0.25f);
                    PutSweep(data, 0.20f, 0.14f, 247f, 494f, 0.46f, 0.25f);
                    break;
                case 7:
                    PutClick(data, 0f, seed + 23, 0.45f);
                    PutThump(data, 0f, 58f, 0.72f);
                    PutThump(data, 0.02f, 110f, 0.4f);
                    PutSweep(data, 0.02f, 0.24f, 123f, 349f, 0.5f, 0.5f);
                    break;
                case 8:
                    PutThump(data, 0f, 90f, 0.52f);
                    PutSweep(data, 0.01f, 0.22f, 175f, 466f, 0.46f, 0.25f);
                    PutSweep(data, 0.04f, 0.18f, 220f, 523f, 0.22f, 0.125f);
                    break;
                case 9:
                    PutClick(data, 0f, seed + 29, 0.38f);
                    PutThump(data, 0f, 64f, 0.68f);
                    PutThump(data, 0.08f, 88f, 0.4f);
                    PutSweep(data, 0.1f, 0.24f, 147f, 392f, 0.5f, 0.25f);
                    break;
                case 10:
                    PutThump(data, 0f, 78f, 0.58f);
                    PutSweep(data, 0.01f, 0.3f, 110f, 349f, 0.5f, 0.25f);
                    break;
                default:
                    PutClick(data, 0f, seed + 31, 0.34f);
                    PutThump(data, 0f, 86f, 0.55f);
                    PutSweep(data, 0.01f, 0.2f, 196f, 494f, 0.4f, 0.5f);
                    PutSweep(data, 0.01f, 0.2f, 196f, 494f, 0.28f, 0.25f);
                    break;
            }

            for (int i = 0; i < n; i++)
            {
                float x = data[i] * 1.55f;
                data[i] = x / (1f + Mathf.Abs(x));
            }
            return ClipPunch("neshop" + kind, data);
        }

        static AudioClip MakeDeny()
        {
            int n = Mathf.CeilToInt(Rate * 0.16f);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float u = t / 0.16f;
                float f = Mathf.Lerp(220f, 110f, u);
                float env = (1f - u) * (1f - u);
                data[i] = Mathf.Sin(2f * Mathf.PI * f * t) * env * 0.28f;
            }
            return Clip("deny", data);
        }

        static AudioClip MakeBreak(int kind, int seed)
        {
            float dur = Mathf.Lerp(0.28f, 0.48f, (Hash(seed) + 1f) * 0.5f);
            if (kind % 4 == 2) dur += 0.12f;
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            float thunk = Mathf.Lerp(42f, 78f, (Hash(seed + 2) + 1f) * 0.5f);
            float snap = Mathf.Lerp(160f, 280f, (Hash(seed + 5) + 1f) * 0.5f);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float u = Mathf.Clamp01(t / dur);
                float air = Soft(ref lp, seed, i, 0.08f);
                float s;
                switch (kind % 6)
                {
                    case 0:
                    {
                        float body = Mathf.Sin(2f * Mathf.PI * thunk * t * (1f - u * 0.35f)) * Mathf.Exp(-u * 6.5f);
                        float crack = Mathf.Sin(2f * Mathf.PI * snap * t) * Mathf.Exp(-(u - 0.08f) * 18f) * (u > 0.06f ? 1f : 0f);
                        s = body * 0.85f + crack * 0.45f + air * 0.12f * Mathf.Exp(-u * 10f);
                        break;
                    }
                    case 1:
                    {
                        float a = Mathf.Sin(2f * Mathf.PI * snap * t) * Mathf.Exp(-u * 16f);
                        float b = Mathf.Sin(2f * Mathf.PI * (snap * 0.78f) * t) * Mathf.Exp(-(u - 0.09f) * 14f) * (u > 0.08f ? 1f : 0f);
                        float body = Mathf.Sin(2f * Mathf.PI * thunk * t) * Mathf.Exp(-u * 8f);
                        s = body * 0.7f + a * 0.5f + b * 0.4f + air * 0.1f * Mathf.Exp(-u * 9f);
                        break;
                    }
                    case 2:
                    {
                        float creak = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(140f, 70f, u) * t) * (u < 0.45f ? 0.45f : 0f);
                        float body = Mathf.Sin(2f * Mathf.PI * thunk * t) * Mathf.Exp(-(u - 0.38f) * 7f) * (u > 0.35f ? 1f : 0f);
                        float crack = Mathf.Sin(2f * Mathf.PI * snap * t) * Mathf.Exp(-(u - 0.4f) * 16f) * (u > 0.38f ? 1f : 0f);
                        s = creak + body * 0.8f + crack * 0.5f + air * 0.1f * Mathf.Exp(-u * 8f);
                        break;
                    }
                    case 3:
                    {
                        float h0 = Mathf.Exp(-u * 18f);
                        float h1 = Mathf.Exp(-(u - 0.07f) * 16f) * (u > 0.06f ? 1f : 0f);
                        float h2 = Mathf.Exp(-(u - 0.15f) * 12f) * (u > 0.14f ? 1f : 0f);
                        s = Mathf.Sin(2f * Mathf.PI * thunk * t) * (h0 * 0.7f + h1 * 0.55f + h2 * 0.4f);
                        s += Mathf.Sin(2f * Mathf.PI * snap * t) * h1 * 0.35f;
                        s += air * 0.1f * Mathf.Exp(-u * 9f);
                        break;
                    }
                    case 4:
                    {
                        float body = Mathf.Sin(2f * Mathf.PI * (thunk * 0.85f) * t * (1f - u * 0.5f)) * Mathf.Exp(-u * 5.5f);
                        float pop = Mathf.Sin(2f * Mathf.PI * (snap * 0.7f) * t) * Mathf.Exp(-u * 11f);
                        s = body * 0.9f + pop * 0.4f + air * 0.14f * Mathf.Exp(-u * 7f);
                        break;
                    }
                    default:
                    {
                        float body = Mathf.Sin(2f * Mathf.PI * thunk * t) * Mathf.Exp(-u * 7f);
                        float crack = Mathf.Sin(2f * Mathf.PI * snap * 1.1f * t) * Mathf.Abs(Mathf.Sin(t * 28f)) * Mathf.Exp(-u * 13f);
                        s = body * 0.75f + crack * 0.4f + air * 0.16f * Mathf.Exp(-u * 8f);
                        break;
                    }
                }
                data[i] = Mathf.Clamp(s * 0.48f, -0.95f, 0.95f);
            }
            return Clip("break" + seed, data);
        }

        static AudioClip MakeLift(int kind, int seed)
        {
            float dur = Mathf.Lerp(0.32f, 0.48f, (Hash(seed) + 1f) * 0.5f);
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            float f0 = Mathf.Lerp(140f, 220f, (Hash(seed + 2) + 1f) * 0.5f);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float u = Mathf.Clamp01(t / dur);
                float env = Mathf.Sin(Mathf.PI * Mathf.Pow(u, 0.7f)) * Mathf.Exp(-u * 1.8f);
                float air = Soft(ref lp, seed, i, 0.07f);
                float f = Mathf.Lerp(f0, f0 * 1.18f, u);
                float s;
                switch (kind % 4)
                {
                    case 0:
                        s = Mathf.Sin(2f * Mathf.PI * f * t) * 0.75f + air * 0.12f;
                        break;
                    case 1:
                        s = Mathf.Sin(2f * Mathf.PI * f * t) * 0.55f;
                        s += Mathf.Sin(2f * Mathf.PI * (f * 1.25f) * t) * 0.22f;
                        s += air * 0.1f;
                        break;
                    case 2:
                        s = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(f0 * 0.8f, f0 * 1.1f, u * u) * t);
                        s += air * 0.14f;
                        break;
                    default:
                        s = Mathf.Sin(2f * Mathf.PI * (f0 * 0.9f) * t) * (1f - u * 0.3f);
                        s += air * 0.1f * (1f - u);
                        break;
                }
                data[i] = s * env * 0.28f;
            }
            return Clip("lift" + seed, data);
        }
    }
}
