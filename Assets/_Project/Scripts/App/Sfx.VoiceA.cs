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
            float dur = Mathf.Lerp(0.10f, 0.16f, (Hash(seed) + 1f) * 0.5f);
            float thump = Mathf.Lerp(180f, 260f, (Hash(seed + 2) + 1f) * 0.5f);
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float u = t / dur;
                float env = Stroke(u, 0f, 0.55f);
                float body = Mathf.Sin(2f * Mathf.PI * thump * t);
                data[i] = body * env * 0.45f;
            }
            return ClipPunch("flap" + seed, data);
        }

        // Bird wing: irregular air whooshes + gated rustle. No rotor thump, no coo.
        static AudioClip MakeFlutter(int kind, int seed)
        {
            int beats = 3;
            float t0 = 0.010f;
            float dur = t0;
            var at = new float[beats];
            for (int b = 0; b < beats; b++)
            {
                at[b] = dur;
                float gap = 0.100f + 0.022f * ((Hash(seed + 11 + b) + 1f) * 0.5f);
                dur += gap;
            }
            dur += 0.08f;
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            for (int b = 0; b < beats; b++)
                PutWingBeat(data, at[b], 0.88f * (1f - b * 0.12f), seed + b * 19);
            return ClipFeather("flutter" + seed, data);
        }

        static void PutWingBeat(float[] data, float start, float amp, int seed)
        {
            const float len = 0.088f;
            int n = data.Length;
            float bpLo = 0f, bpHi = 0f, air = 0f;
            float aLo = 1f - Mathf.Exp(-2f * Mathf.PI * 280f / Rate);
            float aHi = 1f - Mathf.Exp(-2f * Mathf.PI * 1400f / Rate);
            int h = seed * 1103515245 + 12345;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate - start;
                if (t < 0f || t > len) continue;
                float u = t / len;
                float hit = u < 0.16f ? u / 0.16f : 1f;
                float env = hit * Mathf.Exp(-u * 10f) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(u / 0.92f));
                h = (h * 1103515245 + 12345) & 0x7fffffff;
                float nz = (h / 1073741824f) - 1f;
                float aAir = 1f - Mathf.Exp(-2f * Mathf.PI * Mathf.Lerp(780f, 260f, u) / Rate);
                bpLo += aLo * (nz - bpLo);
                bpHi += aHi * (nz - bpHi);
                air += aAir * (nz - air);
                float rustle = (bpHi - bpLo) * env * env;
                float whoosh = air * env;
                data[i] += (whoosh * 1.00f + rustle * 0.38f) * amp;
            }
        }

        static AudioClip ClipFeather(string name, float[] data)
        {
            float y = 0f;
            const float a = 0.30f;
            float p = 1e-6f;
            for (int i = 0; i < data.Length; i++)
            {
                y += a * (data[i] - y);
                data[i] = y;
                float v = Mathf.Abs(y);
                if (v > p) p = v;
            }
            float g = 0.58f / p;
            int fade = Mathf.Min(Mathf.RoundToInt(0.012f * Rate), data.Length / 10);
            for (int i = 0; i < data.Length; i++)
            {
                float w = 1f;
                if (i < fade) w = i / (float)fade;
                int back = data.Length - 1 - i;
                if (back < fade) w = Mathf.Min(w, back / (float)fade);
                data[i] = Mathf.Clamp(data[i] * g * w, -0.93f, 0.93f);
            }
            var c = AudioClip.Create(name, data.Length, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }

        static float Stroke(float u, float at, float span)
        {
            float x = (u - at) / span;
            if (x < 0f || x > 1f) return 0f;
            float hit = x < 0.18f ? x / 0.18f : 1f;
            return hit * Mathf.Exp(-x * 7.5f) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(x / 0.85f));
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

        static AudioClip MakePageTurn()
        {
            // Soft paper whoosh + brief rustle — Ultra Pro binder flip.
            float dur = 0.38f;
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float u = t / dur;
                float whoosh = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(420f, 180f, u) * t);
                whoosh *= Mathf.Sin(Mathf.PI * u) * 0.22f;
                float noise = Soft(ref lp, 9100 + i, i, 0.35f);
                float rustle = noise * (0.55f + 0.45f * Mathf.Sin(2f * Mathf.PI * 28f * t));
                float env = u < 0.15f ? (u / 0.15f) : Mathf.Pow(1f - (u - 0.15f) / 0.85f, 1.4f);
                rustle *= env * 0.55f;
                float flap = 0f;
                if (u > 0.08f && u < 0.22f)
                {
                    float v = (u - 0.08f) / 0.14f;
                    flap = Mathf.Sin(Mathf.PI * v) * 0.18f * Mathf.Sin(2f * Mathf.PI * 90f * t);
                }
                data[i] = whoosh + rustle + flap;
            }
            for (int i = 0; i < n; i++)
            {
                float x = data[i] * 1.35f;
                data[i] = x / (1f + Mathf.Abs(x));
            }
            return Clip("page-turn", data);
        }

        static AudioClip MakeBreak(int kind, int seed)
        {
            // Thick branch: three snappy splits, then a meaty pith CRUNCH.
            float dur = 0.38f + 0.03f * (kind % 3);
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            float t0 = 0.005f + 0.002f * (kind % 3);
            float t1 = t0 + 0.012f + 0.004f * ((kind + 1) % 3);
            float t2 = t1 + 0.014f + 0.005f * ((kind + 2) % 3);
            float f0 = Mathf.Lerp(78f, 110f, (Hash(seed + 2) + 1f) * 0.5f);
            float f1 = Mathf.Lerp(118f, 160f, (Hash(seed + 5) + 1f) * 0.5f);
            float f2 = Mathf.Lerp(165f, 210f, (Hash(seed + 8) + 1f) * 0.5f);
            float bodyF = Mathf.Lerp(52f, 74f, (Hash(seed + 11) + 1f) * 0.5f);
            float aLo = 1f - Mathf.Exp(-2f * Mathf.PI * 280f / Rate);
            float aHi = 1f - Mathf.Exp(-2f * Mathf.PI * 2200f / Rate);
            float pLoA = 1f - Mathf.Exp(-2f * Mathf.PI * 160f / Rate);
            float pHiA = 1f - Mathf.Exp(-2f * Mathf.PI * 780f / Rate);
            float bpLo = 0f, bpHi = 0f, pithLo = 0f, pithHi = 0f;
            int h = seed * 17 + 91;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                h = (h * 1103515245 + 12345) & 0x7fffffff;
                float nz = (h / 1073741824f) - 1f;
                bpLo += aLo * (nz - bpLo);
                bpHi += aHi * (nz - bpHi);
                pithLo += pLoA * (nz - pithLo);
                pithHi += pHiA * (nz - pithHi);
                float band = bpHi - bpLo;
                float pith = (pithHi - pithLo) * Mathf.Exp(-t * 7.2f);
                float body = Mathf.Sin(2f * Mathf.PI * bodyF * t * (1f - t * 1.8f)) * Mathf.Exp(-t * 8.5f);
                float s = body * 0.78f + pith * 0.52f;
                s += TwigSnap(t, t0, band, f0, 1.00f, 32f);
                s += TwigSnap(t, t1, band, f1, 0.78f, 38f);
                s += TwigSnap(t, t2, band, f2, 0.62f, 44f);
                data[i] = s;
            }
            return ClipTwig("break" + seed, data);
        }

        static float TwigSnap(float t, float at, float band, float woodF, float amp, float decay)
        {
            float d = t - at;
            if (d < 0f) return 0f;
            float hit = d < 0.0016f ? d / 0.0016f : 1f;
            float env = hit * Mathf.Exp(-d * decay);
            float knock = Mathf.Sin(2f * Mathf.PI * woodF * d) * Mathf.Exp(-d * 36f);
            return (band * 1.35f + knock * 0.48f) * env * amp;
        }

        static AudioClip ClipTwig(string name, float[] data)
        {
            float y = 0f;
            const float a = 0.36f;
            float p = 1e-6f;
            for (int i = 0; i < data.Length; i++)
            {
                y += a * (data[i] - y);
                data[i] = y;
                float v = Mathf.Abs(y);
                if (v > p) p = v;
            }
            float g = 0.92f / p;
            int fadeIn = Mathf.Min(Mathf.RoundToInt(0.0008f * Rate), 32);
            int fadeOut = Mathf.Min(Mathf.RoundToInt(0.028f * Rate), data.Length / 6);
            for (int i = 0; i < data.Length; i++)
            {
                float w = 1f;
                if (i < fadeIn) w = i / (float)fadeIn;
                int back = data.Length - 1 - i;
                if (back < fadeOut) w = Mathf.Min(w, back / (float)fadeOut);
                data[i] = Mathf.Clamp(data[i] * g * w, -0.94f, 0.94f);
            }
            var c = AudioClip.Create(name, data.Length, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }

        static AudioClip MakePop(int kind, int seed)
        {
            float dur = 0.11f;
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            float f = 205f + 24f * (kind % 5);
            float aLo = 1f - Mathf.Exp(-2f * Mathf.PI * 600f / Rate);
            float aHi = 1f - Mathf.Exp(-2f * Mathf.PI * 2200f / Rate);
            float bpLo = 0f, bpHi = 0f;
            int h = seed * 13 + 7;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float hit = t < 0.002f ? t / 0.002f : 1f;
                float env = hit * Mathf.Exp(-t * 28f);
                float body = Mathf.Sin(2f * Mathf.PI * f * t * (1f - t * 1.4f));
                body += 0.18f * Mathf.Sin(4f * Mathf.PI * f * t);
                h = (h * 1103515245 + 12345) & 0x7fffffff;
                float nz = (h / 1073741824f) - 1f;
                bpLo += aLo * (nz - bpLo);
                bpHi += aHi * (nz - bpHi);
                float click = (bpHi - bpLo) * Mathf.Exp(-t * 90f) * 0.28f;
                data[i] = (body * 0.82f + click) * env;
            }
            return ClipLp("pop" + seed, data, 0.34f);
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
