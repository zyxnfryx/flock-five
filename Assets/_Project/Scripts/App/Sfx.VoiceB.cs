using UnityEngine;

namespace FlockFive
{
    public static partial class Sfx
    {
        static AudioClip MakeSnooze(int kind, int seed)
        {
            float dur = kind == 3 ? 0.55f : Mathf.Lerp(0.22f, 0.42f, (Hash(seed) + 1f) * 0.5f);
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            float f0 = Mathf.Lerp(180f, 320f, (Hash(seed + 2) + 1f) * 0.5f);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float u = Mathf.Clamp01(t / dur);
                float env = Mathf.Sin(Mathf.PI * u);
                float air = Soft(ref lp, seed, i, 0.08f);
                float s = 0f;
                switch (kind % 10)
                {
                    case 0:
                        s = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(f0, f0 * 0.82f, u) * t);
                        s += 0.28f * Mathf.Sin(4f * Mathf.PI * f0 * 0.82f * t);
                        env *= u < 0.45f ? 1f : 0.55f + 0.45f * Mathf.Sin((u - 0.45f) / 0.55f * Mathf.PI);
                        break;
                    case 1:
                        s = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(f0 * 1.15f, f0 * 0.62f, u) * t);
                        s += 0.06f * air;
                        break;
                    case 2:
                        env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(u / 0.55f));
                        s = Mathf.Sin(2f * Mathf.PI * (f0 * 1.15f) * t);
                        s += 0.1f * Mathf.Sin(4f * Mathf.PI * f0 * 1.15f * t);
                        break;
                    case 3:
                    {
                        float f = 70f + 18f * Mathf.Sin(t * 6f);
                        s = Mathf.Sin(2f * Mathf.PI * f * t) * 0.82f;
                        s += air * 0.12f * (0.5f + 0.5f * Mathf.Sin(t * 8f));
                        env = Mathf.Pow(Mathf.Sin(Mathf.PI * u), 1.4f);
                        break;
                    }
                    case 4:
                        s = Mathf.Sin(2f * Mathf.PI * (f0 + 18f * Mathf.Sin(t * 11f)) * t);
                        break;
                    case 5:
                    {
                        float gate = u < 0.42f ? Mathf.Sin(u / 0.42f * Mathf.PI) : (u > 0.52f ? Mathf.Sin((u - 0.52f) / 0.48f * Mathf.PI) : 0f);
                        env = gate;
                        float ff = u < 0.45f ? f0 : f0 * 0.88f;
                        s = Mathf.Sin(2f * Mathf.PI * ff * t) + 0.22f * Mathf.Sin(4f * Mathf.PI * ff * t);
                        break;
                    }
                    case 6:
                        s = air * 0.22f + Mathf.Sin(2f * Mathf.PI * 90f * t) * 0.7f;
                        env = Mathf.Pow(Mathf.Sin(Mathf.PI * u), 1.6f);
                        break;
                    case 7:
                        s = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(f0 * 1.12f, f0 * 0.82f, u * u) * t);
                        break;
                    case 8:
                        s = Mathf.Sin(2f * Mathf.PI * (f0 * 0.55f) * t);
                        s += 0.4f * Mathf.Sin(2f * Mathf.PI * (f0 * 0.82f) * t);
                        break;
                    default:
                    {
                        float[] notes = { f0, f0 * 1.12f, f0 * 0.9f };
                        int ni = u < 0.33f ? 0 : (u < 0.66f ? 1 : 2);
                        float local = (u % 0.33f) / 0.33f;
                        env = Mathf.Sin(Mathf.PI * local) * 0.85f;
                        s = Mathf.Sin(2f * Mathf.PI * notes[ni] * t);
                        break;
                    }
                }
                data[i] = s * env * 0.2f;
            }
            return Clip("snooze" + kind, data);
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
                data[i] = s * env * 0.2f;
            }
            return Clip("hum" + kind, data);
        }

        static AudioClip MakeScatter(int kind, int seed)
        {
            float dur = Mathf.Lerp(0.22f, 0.38f, (Hash(seed) + 1f) * 0.5f);
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            float f0 = Mathf.Lerp(220f, 360f, (Hash(seed + 4) + 1f) * 0.5f);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float u = Mathf.Clamp01(t / dur);
                float env = Mathf.Pow(1f - u, 1.2f) * (u < 0.08f ? u / 0.08f : 1f);
                float noise = Soft(ref lp, seed * 7, i, 0.1f);
                float s;
                switch (kind % 8)
                {
                    case 0:
                        s = noise * 0.14f;
                        s += Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(f0 * 0.7f, f0 * 1.12f, u) * t) * 0.72f;
                        break;
                    case 1:
                        s = Mathf.Sin(2f * Mathf.PI * (f0 * 0.95f) * t) * Mathf.Abs(Mathf.Sin(t * 18f));
                        s += noise * 0.08f;
                        break;
                    case 2:
                        s = Mathf.Sin(2f * Mathf.PI * f0 * t) * 0.62f;
                        s += Mathf.Sin(2f * Mathf.PI * f0 * 1.25f * t) * 0.22f;
                        break;
                    case 3:
                        s = noise * 0.12f * (0.4f + 0.6f * u);
                        s += Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(f0 * 0.8f, f0 * 1.05f, u) * t) * 0.7f;
                        env = Mathf.Sin(Mathf.PI * u);
                        break;
                    case 4:
                        s = noise * 0.18f;
                        s += Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(f0 * 0.45f, f0 * 0.28f, u) * t) * 0.7f;
                        env = Mathf.Pow(1f - u, 1.1f) * (u < 0.08f ? u / 0.08f : 1f);
                        break;
                    case 5:
                        s = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(f0 * 0.75f, f0 * 1.1f, u * u) * t);
                        s += noise * 0.1f * (1f - u);
                        break;
                    case 6:
                        s = noise * 0.16f * (1f - u);
                        s += Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(f0 * 0.55f, f0 * 0.32f, u) * t) * 0.68f;
                        env = Mathf.Sin(Mathf.PI * Mathf.Pow(u, 0.65f)) * Mathf.Exp(-u * 1.4f);
                        break;
                    default:
                        s = Mathf.Sin(2f * Mathf.PI * (f0 * 0.4f + 8f * Mathf.Sin(t * 7f)) * t) * 0.7f;
                        s += noise * 0.14f;
                        env = Mathf.Pow(Mathf.Sin(Mathf.PI * u), 1.1f);
                        break;
                }
                data[i] = s * env * 0.24f;
            }
            return Clip("scatter" + kind, data);
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
