using UnityEngine;

namespace FlockFive
{
    public static partial class Sfx
    {
        static AudioClip MakeSnooze(int kind, int seed)
        {
            // Cartoon snore: HONK in, then a comedic shoo/whee out. 12 unique.
            float inhale = 0.20f + 0.012f * (kind % 4);
            float exhale = 0.28f + 0.016f * ((kind + 2) % 5);
            float dur = inhale + 0.04f + exhale;
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            float honkF = Mathf.Lerp(72f, 118f, (Hash(seed + 2) + 1f) * 0.5f);
            float flut = Mathf.Lerp(7.5f, 13f, (Hash(seed + 5) + 1f) * 0.5f);
            float whee0 = Mathf.Lerp(260f, 360f, (Hash(seed + 8) + 1f) * 0.5f);
            bool whistle = kind % 3 != 1;
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float air = Soft(ref lp, seed, i, 0.10f);
                float s;
                if (t < inhale)
                {
                    float u = t / inhale;
                    float hit = u < 0.12f ? u / 0.12f : 1f;
                    float env = hit * Mathf.Sin(Mathf.PI * Mathf.Clamp01(u / 0.95f));
                    float f = honkF * (1f + 0.08f * u);
                    float buzz = Mathf.Sin(2f * Mathf.PI * f * t);
                    buzz += 0.28f * Mathf.Sin(4f * Mathf.PI * f * t);
                    float flutter = 0.62f + 0.38f * Mathf.Sin(2f * Mathf.PI * flut * t);
                    s = (buzz * flutter + air * 0.10f) * env;
                }
                else
                {
                    float te = t - inhale - 0.03f;
                    if (te < 0f) { data[i] = 0f; continue; }
                    float u = Mathf.Clamp01(te / exhale);
                    float env = (u < 0.10f ? u / 0.10f : 1f) * Mathf.Exp(-u * 3.4f);
                    if (whistle)
                    {
                        float f = Mathf.Lerp(whee0, whee0 * 0.42f, u * u);
                        s = Mathf.Sin(2f * Mathf.PI * f * te);
                        s += 0.12f * Mathf.Sin(4f * Mathf.PI * f * te);
                        s += air * 0.08f;
                    }
                    else
                    {
                        float f = honkF * 0.78f * (1f - 0.22f * u);
                        s = Mathf.Sin(2f * Mathf.PI * f * te) * 0.55f;
                        s += air * 0.42f;
                    }
                    s *= env;
                }
                data[i] = s * 0.72f;
            }
            return ClipPunch("snooze" + seed, data);
        }

        static AudioClip MakeHum(int kind, int seed)
        {
            float dur = Mathf.Lerp(0.16f, 0.28f, (Hash(seed) + 1f) * 0.5f);
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            float f0 = Mathf.Lerp(160f, 280f, (Hash(seed + 3) + 1f) * 0.5f);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float u = Mathf.Clamp01(t / dur);
                float env = Mathf.Pow(Mathf.Sin(Mathf.PI * u), 1.15f);
                float noise = Soft(ref lp, seed * 5, i, 0.1f);
                float s;
                switch (kind % 8)
                {
                    case 0:
                        s = Mathf.Sin(2f * Mathf.PI * (f0 * 0.55f + 12f * Mathf.Sin(t * 18f)) * t);
                        s += noise * 0.18f;
                        break;
                    case 1:
                        s = noise * (0.22f + 0.18f * Mathf.Sin(2f * Mathf.PI * 72f * t));
                        s += Mathf.Sin(2f * Mathf.PI * f0 * 0.4f * t) * 0.7f;
                        break;
                    case 2:
                        s = Mathf.Sin(2f * Mathf.PI * (f0 * 0.95f) * t);
                        s += 0.12f * Mathf.Sin(4f * Mathf.PI * f0 * 0.95f * t);
                        env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(u / 0.6f));
                        break;
                    case 3:
                        s = Mathf.Sin(2f * Mathf.PI * f0 * t) * 0.55f;
                        s += Mathf.Sin(2f * Mathf.PI * (f0 * 1.25f) * t) * 0.4f;
                        s += noise * 0.1f;
                        break;
                    case 4:
                        s = noise * 0.16f + Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(f0 * 0.85f, f0 * 1.08f, u) * t) * 0.7f;
                        break;
                    case 5:
                        s = noise * 0.12f * Mathf.Abs(Mathf.Sin(t * 22f));
                        s += Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.7f;
                        break;
                    case 6:
                        s = Mathf.Sin(2f * Mathf.PI * (190f + 20f * Mathf.Sin(t * 9f)) * t);
                        s += noise * 0.16f;
                        break;
                    default:
                        s = Mathf.Sin(2f * Mathf.PI * (f0 * 0.9f + 12f * Mathf.Sin(t * 10f)) * t);
                        s += noise * 0.08f;
                        break;
                }
                data[i] = s * env * 0.38f;
            }
            return ClipPunch("hum" + kind, data);
        }

        static AudioClip MakeScatter(int kind, int seed)
        {
            // Bee-leaving buzz. Wing AM, recedes. Audible, not a bird flap.
            float dur = 0.32f + 0.02f * (kind % 12);
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            float f0 = Mathf.Lerp(240f, 340f, (Hash(seed + 4) + 1f) * 0.5f);
            float am = Mathf.Lerp(155f, 230f, (Hash(seed + 7) + 1f) * 0.5f);
            float wob = Mathf.Lerp(8f, 16f, (Hash(seed + 9) + 1f) * 0.5f);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float u = Mathf.Clamp01(t / dur);
                float hit = u < 0.05f ? u / 0.05f : 1f;
                float env = hit * Mathf.Pow(1f - u, 0.85f);
                float recede = 1f - 0.14f * u;
                float buzz = Mathf.Sin(2f * Mathf.PI * f0 * recede * t);
                buzz += 0.28f * Mathf.Sin(4f * Mathf.PI * f0 * recede * t);
                float wings = 0.50f + 0.50f * Mathf.Sin(2f * Mathf.PI * am * t);
                float drift = 1f + 0.05f * Mathf.Sin(2f * Mathf.PI * wob * t);
                float grit = Soft(ref lp, seed * 7, i, 0.14f) * 0.12f * env;
                data[i] = (buzz * wings * drift + grit) * env * 0.78f;
            }
            return ClipPunch("scatter" + seed, data);
        }

        static AudioClip MakeBoom(int seed)
        {
            float dur = Mathf.Lerp(0.28f, 0.42f, (Hash(seed) + 1f) * 0.5f);
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            float thump = Mathf.Lerp(48f, 88f, (Hash(seed + 2) + 1f) * 0.5f);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float u = t / dur;
                float env = Mathf.Exp(-u * 7f);
                float crack = Soft(ref lp, seed * 11, i, 0.16f) * Mathf.Exp(-u * 11f);
                float body = Mathf.Sin(2f * Mathf.PI * thump * t * (1f - u * 0.4f)) * env;
                data[i] = (body * 0.82f + crack * 0.18f) * 0.36f;
            }
            return Clip("boom" + seed, data);
        }

        static AudioClip MakeComboJingle(int size)
        {
            // Garden D-major sting. Climbs in pitch and length with combo size.
            // Wood pluck, not a bell or Zelda arpeggio.
            int tier = Mathf.Clamp(size, 2, 8) - 2;
            float[][] phrases =
            {
                new[] { 220.00f, 329.63f },
                new[] { 293.66f, 369.99f, 440.00f },
                new[] { 369.99f, 440.00f, 493.88f, 587.33f },
                new[] { 440.00f, 493.88f, 587.33f, 659.25f, 739.99f },
                new[] { 493.88f, 587.33f, 659.25f, 739.99f, 659.25f, 739.99f },
                new[] { 440.00f, 493.88f, 587.33f, 659.25f, 739.99f, 659.25f, 739.99f },
                new[] { 493.88f, 587.33f, 659.25f, 739.99f, 587.33f, 659.25f, 739.99f }
            };
            var seq = phrases[tier];
            float step = 0.090f;
            float tail = 0.18f + 0.05f * tier;
            float dur = seq.Length * step + tail;
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            for (int k = 0; k < seq.Length; k++)
            {
                float f = seq[k];
                float start = k * step;
                float decay = 14f - 1.2f * k;
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)Rate - start;
                    if (t < 0f) continue;
                    float hit = t < 0.003f ? t / 0.003f : 1f;
                    float env = hit * Mathf.Exp(-t * decay);
                    float s = Mathf.Sin(2f * Mathf.PI * f * t);
                    s += 0.14f * Mathf.Sin(4f * Mathf.PI * f * t);
                    data[i] += s * env * 0.42f;
                }
            }
            return ClipPunch("combo" + size, data);
        }

        static AudioClip MakeMoonrise()
        {
            float dur = 1.35f;
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            float[] notes = { 392f, 523.25f, 659.25f };
            float[] at = { 0f, 0.28f, 0.58f };
            for (int k = 0; k < notes.Length; k++)
            {
                float f = notes[k];
                float start = at[k];
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)Rate - start;
                    if (t < 0f) continue;
                    float env = Mathf.Exp(-t * 1.8f) * (t < 0.02f ? t / 0.02f : 1f);
                    float s = Mathf.Sin(2f * Mathf.PI * f * t);
                    s += 0.12f * Mathf.Sin(4f * Mathf.PI * f * t);
                    data[i] += s * env * 0.14f;
                }
            }
            return Clip("moon", data);
        }

        static AudioClip MakeThunder(int kind, int seed)
        {
            float dur = 1.85f + 0.55f * kind;
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            float f0 = Mathf.Lerp(38f, 56f, (Hash(seed) + 1f) * 0.5f);
            float f1 = f0 * 1.38f;
            float knock = Mathf.Lerp(92f, 128f, (Hash(seed + 4) + 1f) * 0.5f);
            float lp = 0f;
            int h = seed | 1;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float u = t / dur;
                float roll = Mathf.Sin(u * Mathf.PI);
                roll *= roll;
                roll *= Mathf.Exp(-u * 1.05f);
                float hit = Mathf.Exp(-((t - 0.018f) * (t - 0.018f)) / 0.00055f);
                float hit2 = 0.55f * Mathf.Exp(-((t - 0.095f) * (t - 0.095f)) / 0.0011f);
                float body = Mathf.Sin(2f * Mathf.PI * f0 * t * (1f - u * 0.2f));
                body += 0.42f * Mathf.Sin(2f * Mathf.PI * f1 * t * (1f - u * 0.16f));
                float tap = Mathf.Sin(2f * Mathf.PI * knock * t) * (hit + hit2);
                h = (h * 1103515245 + 12345) & 0x7fffffff;
                float nz = (h / 1073741824f) - 1f;
                lp += 0.05f * (nz - lp);
                data[i] = (body * 0.72f * roll + tap * 0.38f + lp * 0.10f * roll) * 0.48f;
            }
            return ClipLp("thunder" + seed, data, 0.11f);
        }

        static AudioClip Clip(string name, float[] data) => ClipLp(name, data, 0.2f);

        static AudioClip ClipPunch(string name, float[] data) => ClipLp(name, data, 0.42f);

        static AudioClip ClipLp(string name, float[] data, float a)
        {
            float y = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                y += a * (data[i] - y);
                data[i] = Mathf.Clamp(y, -0.95f, 0.95f);
            }
            var c = AudioClip.Create(name, data.Length, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }
    }
}
