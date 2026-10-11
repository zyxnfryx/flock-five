using UnityEngine;

namespace FlockFive
{
    public static partial class Sfx
    {
        static AudioClip _thunk;

        // Wooden press. Lead, one shot, short enough that the release sparkle
        // does not stack on it. Warm body under 400 Hz, no noise floor.
        public static void ButtonThunk()
        {
            Ensure();
            if (_thunk == null) _thunk = MakeThunk();
            Shot(_thunk, 1f, 0.62f, MixLayer.Lead, MixDesk.DuckWhoosh);
        }

        static AudioClip MakeThunk()
        {
            const float dur = 0.09f;
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n * 2];
            float lpL = 0f, lpR = 0f;
            float a = 1f - Mathf.Exp(-2f * Mathf.PI * 900f / Rate);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env = t < 0.004f ? t / 0.004f : Mathf.Exp(-(t - 0.004f) * 38f);
                float body = Mathf.Sin(2f * Mathf.PI * 118f * t);
                float wood = Mathf.Sin(2f * Mathf.PI * 186f * t) * Mathf.Exp(-t * 46f);
                float knock = Mathf.Sin(2f * Mathf.PI * 92f * t) * Mathf.Exp(-t * 28f);
                float s = (body * 0.55f + wood * 0.30f + knock * 0.40f) * env;
                lpL += a * (s - lpL);
                lpR += a * (s * 0.94f - lpR);
                data[i * 2] = lpL;
                data[i * 2 + 1] = lpR;
            }
            float p = 1e-6f;
            for (int i = 0; i < data.Length; i++)
            {
                float v = Mathf.Abs(data[i]);
                if (v > p) p = v;
            }
            float g = 0.72f / p;
            for (int i = 0; i < data.Length; i++)
                data[i] = Mathf.Clamp(data[i] * g, -0.9f, 0.9f);
            var clip = AudioClip.Create("button-thunk", n, 2, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
