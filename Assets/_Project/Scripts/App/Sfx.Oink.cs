using UnityEngine;

namespace FlockFive
{
    public static partial class Sfx
    {
        static AudioClip _oink;

        // Splash piggy poke oink. Procedural stand-in — MP remaster later (real pig sample / VO).
        public static void Oink()
        {
            Ensure();
            if (_oink == null) _oink = MakeOink();
            Shot(_oink, Random.Range(0.97f, 1.04f), 0.82f, MixLayer.Lead, MixDesk.DuckChirp);
            if (MixDesk.Live != null) MixDesk.Live.MarkLead(0.22f, MixDesk.DuckChirp);
        }

        static AudioClip MakeOink()
        {
            // Short nasal grunt: descending formant + soft snort grit. Not a chirp/coin.
            const float dur = 0.22f;
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            float lp = 0f;
            float gritLp = 0f;
            int seed = 91827;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float u = t / dur;
                float env = (u < 0.06f ? u / 0.06f : 1f) * Mathf.Pow(1f - u, 1.35f);
                // Body ~180→110 Hz with odd partials for piggy nasal.
                float f0 = Mathf.Lerp(210f, 105f, u * u);
                float body = Mathf.Sin(2f * Mathf.PI * f0 * t);
                body += 0.55f * Mathf.Sin(2f * Mathf.PI * f0 * 1.85f * t) * Mathf.Exp(-t / 0.09f);
                body += 0.22f * Mathf.Sin(2f * Mathf.PI * f0 * 2.7f * t) * Mathf.Exp(-t / 0.05f);
                // Soft snort / air grit up front.
                float grit = Soft(ref gritLp, seed, i, 0.20f);
                float snort = grit * Mathf.Exp(-t / 0.028f) * (u < 0.35f ? 1f : 0.25f);
                float air = Soft(ref lp, seed + 11, i, 0.10f) * env * 0.12f;
                data[i] = (body * 0.72f + snort * 0.38f + air) * env;
            }

            float ahp = 1f - Mathf.Exp(-2f * Mathf.PI * 70f / Rate);
            float alp = 1f - Mathf.Exp(-2f * Mathf.PI * 2200f / Rate);
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
            float g = 0.78f / peak;
            int fadeOut = Mathf.Min(Mathf.RoundToInt(0.020f * Rate), n / 4);
            for (int i = 0; i < n; i++)
            {
                float w = 1f;
                int back = n - 1 - i;
                if (back < fadeOut) w = back / (float)fadeOut;
                data[i] = Mathf.Clamp(data[i] * g * w, -0.95f, 0.95f);
            }
            var c = AudioClip.Create("oink", n, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }
    }
}
