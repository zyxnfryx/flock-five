using UnityEngine;

namespace FlockFive
{
    public static partial class Sfx
    {
        static AudioClip _bbTaunt;
        static AudioClip _bbTell;

        // One low croak. Lead, so it is heard after the drop. Not a tune.
        public static void BlackbirdTaunt()
        {
            if (_bbTaunt == null) _bbTaunt = MakeBlackbirdTone(0.42f, 420f, 280f, true);
            Shot(_bbTaunt, 1f, 0.62f, MixLayer.Lead, MixDesk.DuckWhoosh);
        }

        // The 0.5s tell's wing. Mid, so a hot lead (the hop) refuses it.
        public static void BlackbirdTell()
        {
            if (_bbTell == null) _bbTell = MakeBlackbirdTone(0.16f, 340f, 220f, false);
            Shot(_bbTell, 1f, 0.30f, MixLayer.Mid);
        }

        // Dove-range tone, gentle lowpass. No grit, no second pitch.
        static AudioClip MakeBlackbirdTone(float dur, float f0, float f1, bool croak)
        {
            int n = Mathf.CeilToInt(Rate * dur);
            if (n < 8) n = 8;
            var data = new float[n];
            float lp = 0f;
            float a = 1f - Mathf.Exp(-2f * Mathf.PI * 1800f / Rate);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float u = t / dur;
                float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(u));
                if (croak && u < 0.08f) env *= u / 0.08f;
                float f = Mathf.Lerp(f0, f1, u);
                float s = Mathf.Sin(2f * Mathf.PI * f * t);
                if (croak) s += 0.22f * Mathf.Sin(4f * Mathf.PI * f * t);
                lp += a * (s - lp);
                data[i] = lp * env * 0.7f;
            }
            var clip = AudioClip.Create(croak ? "blackbird-taunt" : "blackbird-tell", n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
