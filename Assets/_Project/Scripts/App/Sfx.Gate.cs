using UnityEngine;

namespace FlockFive
{
    public static partial class Sfx
    {
        static AudioClip _gate;

        public static void GateGo()
        {
            Ensure();
            if (_gate == null) _gate = MakeGate();
            Shot(_gate, 1f, 0.86f, MixLayer.Lead, MixDesk.DuckWhoosh);
            if (MixDesk.Live != null) MixDesk.Live.MarkLead(2.7f, MixDesk.DuckWhoosh);
            Rumble();
        }

        static AudioClip MakeGate()
        {
            // Chevron lock + watery vortex blast. Original synthesis, not a
            // sample of anyone's wormhole. Warm, band-limited, ~3s.
            const float dur = 3.05f;
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n * 2];
            float lpL = 0f, lpR = 0f, bpL = 0f, bpR = 0f;
            float airL = 0f, airR = 0f;
            float aAir = 1f - Mathf.Exp(-2f * Mathf.PI * 900f / Rate);
            float aBp = 1f - Mathf.Exp(-2f * Mathf.PI * 420f / Rate);
            float aOut = 1f - Mathf.Exp(-2f * Mathf.PI * 1800f / Rate);
            int h = 90211;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                h = (h * 1103515245 + 12345) & 0x7fffffff;
                float nz = (h / 1073741824f) - 1f;
                airL += aAir * (nz - airL);
                h = (h * 1103515245 + 12345) & 0x7fffffff;
                float nzR = (h / 1073741824f) - 1f;
                airR += aAir * (nzR - airR);

                float l = 0f, r = 0f;

                if (t < 0.24f)
                {
                    float hit = t < 0.004f ? t / 0.004f : 1f;
                    float thud = Mathf.Sin(2f * Mathf.PI * 52f * t) * Mathf.Exp(-t * 26f) * hit;
                    float ring = Mathf.Sin(2f * Mathf.PI * 178f * t) * Mathf.Exp(-t * 16f);
                    ring += 0.42f * Mathf.Sin(2f * Mathf.PI * 268f * t) * Mathf.Exp(-t * 20f);
                    float click = airL * Mathf.Exp(-t * 48f) * (t < 0.035f ? 1f : 0f);
                    l += thud * 0.72f + ring * 0.48f + click * 0.22f;
                    r += thud * 0.68f + ring * 0.52f + airR * Mathf.Exp(-t * 48f) * 0.18f;
                }

                if (t > 0.08f && t < 0.32f)
                {
                    float u = (t - 0.08f) / 0.24f;
                    float env = (u < 0.12f ? u / 0.12f : 1f) * Mathf.Exp(-u * 4.2f);
                    float f = 312f * (1f - 0.08f * u);
                    float chirp = Mathf.Sin(2f * Mathf.PI * f * t);
                    chirp += 0.18f * Mathf.Sin(6f * Mathf.PI * f * t);
                    l += chirp * env * 0.22f;
                    r += chirp * env * 0.20f;
                }

                if (t > 0.18f && t < 0.96f)
                {
                    float u = (t - 0.18f) / 0.78f;
                    float amp = Mathf.Sin(Mathf.PI * Mathf.Pow(Mathf.Clamp01(u), 0.82f)) * Mathf.Lerp(0.18f, 0.72f, u);
                    float fc = Mathf.Lerp(72f, 250f, Mathf.Pow(u, 0.65f));
                    float swirl = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(1.4f, 7.2f, u) * t);
                    bpL += aBp * (airL - bpL);
                    bpR += aBp * (airR - bpR);
                    float tone = Mathf.Sin(2f * Mathf.PI * fc * t) * 0.35f;
                    float sub = Mathf.Sin(2f * Mathf.PI * 36f * (1f + 0.12f * u) * t) * 0.32f;
                    l += (bpL * 0.85f + tone + sub) * amp * (0.62f + 0.38f * swirl);
                    r += (bpR * 0.85f + tone + sub) * amp * (0.62f - 0.38f * swirl);
                }

                if (t > 0.82f)
                {
                    float tk = t - 0.82f;
                    float blast = tk < 0.014f ? tk / 0.014f : Mathf.Exp(-(tk - 0.014f) * 1.85f);
                    float fc = Mathf.Lerp(360f, 64f, Mathf.Clamp01(tk / 1.15f));
                    float water = 0.58f + 0.42f * Mathf.Sin(2f * Mathf.PI * 21f * t) * Mathf.Exp(-tk * 0.7f);
                    bpL += aBp * (airL - bpL);
                    bpR += aBp * (airR - bpR);
                    float whoosh = (bpL * 0.55f + Mathf.Sin(2f * Mathf.PI * fc * t) * 0.28f) * blast * water;
                    float rumble = Mathf.Sin(2f * Mathf.PI * 40f * t) * Mathf.Exp(-tk * 1.35f) * blast * 0.58f;
                    float width = Mathf.Clamp01(tk / 0.12f) * Mathf.Exp(-tk * 0.65f);
                    float spin = Mathf.Sin(2f * Mathf.PI * 2.8f * tk);
                    l += rumble + whoosh * (0.78f + 0.45f * width * spin);
                    r += rumble + (bpR * 0.55f + Mathf.Sin(2f * Mathf.PI * fc * t + 0.7f) * 0.28f) * blast * water * (0.78f - 0.45f * width * spin);
                }

                lpL += aOut * (l - lpL);
                lpR += aOut * (r - lpR);
                data[i * 2] = lpL;
                data[i * 2 + 1] = lpR;
            }

            float p = 1e-6f;
            for (int i = 0; i < data.Length; i++)
            {
                float v = Mathf.Abs(data[i]);
                if (v > p) p = v;
            }
            float g = 0.86f / p;
            for (int i = 0; i < data.Length; i++)
                data[i] = Mathf.Clamp(data[i] * g, -0.95f, 0.95f);

            var clip = AudioClip.Create("gate-go", n, 2, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
