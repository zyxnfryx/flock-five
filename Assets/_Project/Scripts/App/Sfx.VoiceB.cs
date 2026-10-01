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

        static AudioClip MakeFonzieEight()
        {
            // Combo 8 only. "Combo" then the vowel of eight held as Fonzie's ayyy.
            // One lead gag. Not a second bed, and not the wood sting.
            float dur = 1.42f;
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            float gphase = 0f;
            float ay1 = 0f, ay2 = 0f, by1 = 0f, by2 = 0f, ny1 = 0f, ny2 = 0f;
            float aA1 = 0f, aA2 = 0f, bA1 = 0f, bA2 = 0f, nA1 = 0f, nA2 = 0f;
            int rng = 17;
            float peak = 0.0001f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                FonzieFrame(t, out float f0, out float f1, out float f2, out float amp, out float breath, out float nose);
                gphase += f0 / Rate;
                float excite = 0f;
                if (amp > 0.01f && gphase >= 1f)
                {
                    gphase -= 1f;
                    excite = 1f;
                }
                rng = (rng * 1103515245 + 12345) & 0x7fffffff;
                float noise = (rng / 1073741824f) - 1f;
                excite += noise * breath * 0.25f;
                float vf = Vowel(ref ay1, ref ay2, ref aA1, ref aA2, f1, 80f, excite);
                float vb = Vowel(ref by1, ref by2, ref bA1, ref bA2, f2, 120f, excite);
                float vn = Vowel(ref ny1, ref ny2, ref nA1, ref nA2, 250f, 60f, excite);
                float s = (vf * 0.9f + vb * 0.45f) * amp + vn * nose * Mathf.Max(amp, 0.2f);
                s += noise * breath * 0.06f;
                data[i] = s;
                float a = Mathf.Abs(s);
                if (a > peak) peak = a;
            }
            float gain = 0.82f / peak;
            for (int i = 0; i < n; i++)
                data[i] = Mathf.Clamp(data[i] * gain, -0.95f, 0.95f);
            return ClipLp("fonzie-eight", data, 0.78f);
        }

        static void FonzieFrame(float t, out float f0, out float f1, out float f2, out float amp, out float breath, out float nose)
        {
            f0 = 118f;
            f1 = 700f;
            f2 = 1200f;
            amp = 0f;
            breath = 0f;
            nose = 0.05f;
            if (t < 0.04f)
            {
                breath = 0.35f * (t / 0.04f);
                return;
            }
            if (t < 0.15f)
            {
                float u = (t - 0.04f) / 0.11f;
                f0 = 112f;
                f1 = 730f;
                f2 = 1120f;
                amp = Mathf.Sin(Mathf.PI * u);
                breath = 0.04f;
                return;
            }
            if (t < 0.25f)
            {
                float u = (t - 0.15f) / 0.10f;
                f0 = 106f;
                f1 = 270f;
                f2 = 1050f;
                amp = 0.7f * Mathf.Sin(Mathf.PI * u);
                nose = 0.9f;
                breath = 0.02f;
                return;
            }
            if (t < 0.29f)
            {
                breath = 0.05f;
                return;
            }
            if (t < 0.48f)
            {
                float u = (t - 0.29f) / 0.19f;
                f0 = Mathf.Lerp(114f, 124f, u);
                f1 = Mathf.Lerp(500f, 400f, u);
                f2 = Mathf.Lerp(1000f, 800f, u);
                amp = 0.95f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(u));
                breath = 0.04f;
                return;
            }
            if (t < 1.24f)
            {
                float u = (t - 0.48f) / 0.76f;
                float slide = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u / 0.42f));
                float fall = u > 0.84f ? (u - 0.84f) / 0.16f : 0f;
                f0 = Mathf.Lerp(102f, 178f, slide) * (1f - 0.08f * fall);
                f0 *= 1f + 0.025f * Mathf.Sin(2f * Mathf.PI * 5.2f * (t - 0.48f));
                f1 = Mathf.Lerp(560f, 310f, slide);
                f2 = Mathf.Lerp(1600f, 2400f, slide);
                float env = u < 0.04f ? u / 0.04f : 1f;
                amp = env * (1f - 0.15f * fall);
                breath = 0.05f;
                nose = 0.35f;
                return;
            }
            if (t < 1.30f)
                breath = 0.45f;
        }

        static float Vowel(ref float y1, ref float y2, ref float a1, ref float a2, float freq, float bw, float x)
        {
            float r = Mathf.Exp(-Mathf.PI * bw / Rate);
            float na1 = 2f * r * Mathf.Cos(2f * Mathf.PI * freq / Rate);
            float na2 = -r * r;
            if (a1 == 0f) { a1 = na1; a2 = na2; }
            a1 += 0.35f * (na1 - a1);
            a2 += 0.35f * (na2 - a2);
            float y = 0.08f * x + a1 * y1 + a2 * y2;
            y2 = y1;
            y1 = y;
            return y;
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

        static AudioClip MakeCelebrate()
        {
            // Full-house+ cadence in D major. Resolves; not a gong or Zelda climb.
            float[] notes = { 293.66f, 369.99f, 440.00f, 587.33f, 440.00f, 293.66f };
            float[] at = { 0.00f, 0.08f, 0.16f, 0.28f, 0.40f, 0.52f };
            float dur = 0.92f;
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            for (int k = 0; k < notes.Length; k++)
            {
                float f = notes[k];
                float start = at[k];
                float decay = k == notes.Length - 1 ? 6.5f : 11f - 0.6f * k;
                float amp = k == notes.Length - 1 ? 0.50f : 0.36f;
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)Rate - start;
                    if (t < 0f) continue;
                    float hit = t < 0.003f ? t / 0.003f : 1f;
                    float env = hit * Mathf.Exp(-t * decay);
                    float s = Mathf.Sin(2f * Mathf.PI * f * t);
                    s += 0.13f * Mathf.Sin(4f * Mathf.PI * f * t);
                    data[i] += s * env * amp;
                }
            }
            return ClipPunch("celebrate", data);
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
            // 12-clip array: 0 close crack, 1 mid roll, 2 far growl — cycle families.
            int family = kind % 3;
            float dur = family == 0 ? 0.95f + 0.22f * (kind % 4)
                : family == 1 ? 1.65f + 0.35f * (kind % 4)
                : 2.35f + 0.45f * (kind % 4);
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            float f0 = Mathf.Lerp(34f, 62f, (Hash(seed) + 1f) * 0.5f);
            float f1 = f0 * Mathf.Lerp(1.22f, 1.55f, (Hash(seed + 2) + 1f) * 0.5f);
            float knock = Mathf.Lerp(78f, 168f, (Hash(seed + 4) + 1f) * 0.5f);
            float crackHi = Mathf.Lerp(220f, 480f, (Hash(seed + 6) + 1f) * 0.5f);
            float lp = 0f;
            float lp2 = 0f;
            int h = seed | 1;
            float crackAmt = family == 0 ? 0.72f : family == 1 ? 0.38f : 0.18f;
            float rollAmt = family == 0 ? 0.42f : family == 1 ? 0.78f : 0.92f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float u = t / dur;
                float roll = Mathf.Sin(u * Mathf.PI);
                roll *= roll;
                roll *= Mathf.Exp(-u * (family == 2 ? 0.72f : 1.15f));
                float hit = Mathf.Exp(-((t - 0.012f) * (t - 0.012f)) / 0.00038f);
                float hit2 = 0.62f * Mathf.Exp(-((t - 0.078f) * (t - 0.078f)) / 0.00095f);
                float hit3 = 0.35f * Mathf.Exp(-((t - 0.16f) * (t - 0.16f)) / 0.0024f);
                float body = Mathf.Sin(2f * Mathf.PI * f0 * t * (1f - u * 0.22f));
                body += 0.48f * Mathf.Sin(2f * Mathf.PI * f1 * t * (1f - u * 0.18f));
                float tap = Mathf.Sin(2f * Mathf.PI * knock * t) * (hit + hit2 + hit3 * (family == 0 ? 1f : 0.4f));
                float zap = Mathf.Sin(2f * Mathf.PI * crackHi * t) * hit * (family == 0 ? 1f : 0.35f);
                h = (h * 1103515245 + 12345) & 0x7fffffff;
                float nz = (h / 1073741824f) - 1f;
                lp += 0.055f * (nz - lp);
                lp2 += 0.018f * (nz - lp2);
                float rumble = body * 0.70f * roll * rollAmt;
                float noise = (lp * 0.14f + lp2 * 0.08f) * roll;
                data[i] = (rumble + tap * crackAmt * 0.42f + zap * crackAmt * 0.22f + noise) * 0.50f;
            }
            float soft = family == 2 ? 0.08f : 0.12f;
            return ClipLp("thunder" + seed, data, soft);
        }

        static AudioClip MakeRowAlert()
        {
            const float dur = 0.24f;
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float p0 = Mathf.Exp(-((t - 0.02f) * (t - 0.02f)) / 0.0009f);
                float p1 = Mathf.Exp(-((t - 0.12f) * (t - 0.12f)) / 0.0011f);
                float a = Mathf.Sin(2f * Mathf.PI * 486f * t);
                float b = Mathf.Sin(2f * Mathf.PI * 364f * t);
                a += 0.35f * Mathf.Sin(2f * Mathf.PI * 243f * t);
                b += 0.28f * Mathf.Sin(2f * Mathf.PI * 182f * t);
                data[i] = (a * p0 + b * p1 * 0.9f) * 0.55f;
            }
            return Clip("row-alert", data);
        }

        static AudioClip MakeFeederArrive()
        {
            const float dur = 0.16f;
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float u = Mathf.Clamp01(t / dur);
                float env = Mathf.Exp(-u * 7.5f) * Mathf.Sin(Mathf.Clamp01(u / 0.08f) * Mathf.PI * 0.5f);
                float s = Mathf.Sin(2f * Mathf.PI * 392f * t);
                s += 0.45f * Mathf.Sin(2f * Mathf.PI * 523f * t);
                s += 0.18f * Mathf.Sin(2f * Mathf.PI * 196f * t);
                data[i] = s * env * 0.5f;
            }
            return Clip("feeder-arrive", data);
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

