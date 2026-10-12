// GENERATED from /workspace/ff/b83/zfx-jingle/sparkle-sync.json (jingle v4, zfx-jingle.wav (v4)). Do not hand-edit:
// rerun the generator when the producer re-renders. Times are seconds after the audio starts
// (= the frame the studio card shows).
namespace FlockFive
{
    public static class StudioSync
    {
        public const float Fps = 60;
        public const float PopT = 2.000f;
        public const float Duration = 3.000f;
        public const float BurstGrow = 0.050f, BurstHoldUntil = 2.080f, BurstTau = 0.150f, BurstGone = 2.771f;
        public const float FadeStart = 2.700f, FadeEnd = 3.000f;
        public static readonly float[] KeyT = { 0.004f, 0.500f, 1.000f, 1.460f, 1.700f, 1.780f, 1.855f, 1.925f, 2.000f };
        public static readonly float[] KeyU = { 0.000f, 0.120f, 0.300f, 0.540f, 0.700f, 0.780f, 0.870f, 0.940f, 1.000f };
        public static readonly float[] GlintT = { 0.004f, 0.500f, 1.000f, 1.460f, 1.700f, 1.780f, 1.855f, 1.925f };
        public static readonly float[] GlintAdd = { 0.350f, 0.350f, 0.350f, 0.350f, 0.180f, 0.180f, 0.180f, 0.180f };
        public static readonly float[] GlintTau = { 0.120f, 0.120f, 0.120f, 0.120f, 0.120f, 0.120f, 0.120f, 0.120f };
        public static readonly bool[] GlintHook = { true, true, true, true, false, false, false, false };
        public static readonly float[] Brightness = { 0.000f, 0.363f, 0.396f, 0.437f, 0.481f, 0.510f, 0.520f, 0.515f, 0.483f, 0.431f, 0.410f, 0.389f, 0.364f, 0.325f, 0.293f, 0.274f, 0.249f, 0.221f, 0.181f, 0.176f, 0.172f, 0.170f, 0.167f, 0.165f, 0.163f, 0.161f, 0.160f, 0.159f, 0.157f, 0.169f, 0.732f, 0.778f, 0.769f, 0.682f, 0.603f, 0.555f, 0.538f, 0.496f, 0.454f, 0.417f, 0.398f, 0.371f, 0.372f, 0.380f, 0.400f, 0.402f, 0.386f, 0.354f, 0.315f, 0.294f, 0.285f, 0.283f, 0.257f, 0.228f, 0.190f, 0.167f, 0.160f, 0.158f, 0.157f, 0.384f, 0.908f, 0.955f, 0.963f, 0.954f, 0.928f, 0.885f, 0.827f, 0.788f, 0.769f, 0.746f, 0.724f, 0.688f, 0.653f, 0.619f, 0.613f, 0.609f, 0.605f, 0.596f, 0.573f, 0.553f, 0.531f, 0.528f, 0.530f, 0.530f, 0.522f, 0.513f, 0.497f, 0.688f, 1.114f, 1.119f, 1.067f, 1.003f, 0.956f, 0.896f, 0.847f, 0.789f, 0.746f, 0.725f, 0.716f, 0.703f, 0.677f, 0.661f, 0.840f, 0.814f, 0.797f, 0.794f, 0.790f, 0.968f, 0.943f, 0.930f, 0.904f, 0.879f, 1.025f, 0.986f, 0.983f, 0.966f, 1.126f, 1.069f, 1.032f, 1.053f, 1.121f, 0.874f, 0.660f, 0.486f, 0.360f, 0.268f, 0.199f, 0.145f, 0.107f, 0.079f, 0.060f, 0.044f, 0.033f, 0.025f, 0.019f, 0.015f, 0.011f, 0.008f, 0.006f, 0.005f, 0.003f, 0.003f, 0.002f, 0.001f, 0.001f, 0.001f, 0.001f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f };

        // Path fraction u at time t: piecewise-linear through the keyframes.
        public static float U(float t)
        {
            if (t <= KeyT[0]) return KeyU[0];
            for (int i = 1; i < KeyT.Length; i++)
                if (t <= KeyT[i]) return KeyU[i - 1] + (KeyU[i] - KeyU[i - 1]) * (t - KeyT[i - 1]) / (KeyT[i] - KeyT[i - 1]);
            return KeyU[KeyU.Length - 1];
        }

        // Per-frame brightness (60fps), linearly interpolated between frames.
        public static float Bright(float t)
        {
            float f = t * Fps;
            if (f <= 0f) return Brightness[0];
            int i = (int)f;
            if (i >= Brightness.Length - 1) return Brightness[Brightness.Length - 1];
            return Brightness[i] + (Brightness[i + 1] - Brightness[i]) * (f - i);
        }

        // Glint flash (0..1) from the most recent glint, for the star's size kick.
        public static float Flash(float t, out bool hook)
        {
            hook = false;
            float best = 0f;
            for (int i = 0; i < GlintT.Length; i++)
            {
                float dt = t - GlintT[i];
                if (dt < 0f) continue;
                float v = UnityEngine.Mathf.Exp(-dt / GlintTau[i]);
                if (v > best) { best = v; hook = GlintHook[i]; }
            }
            return best;
        }

        // Starburst intensity: ease-out cubic grow, hold, exponential decay; 0 when gone.
        public static float Burst(float t, out float grow)
        {
            grow = 0f;
            if (t < PopT || t >= BurstGone) return 0f;
            float g = UnityEngine.Mathf.Clamp01((t - PopT) / BurstGrow);
            grow = 1f - (1f - g) * (1f - g) * (1f - g);
            if (t <= BurstHoldUntil) return grow;
            return UnityEngine.Mathf.Exp(-(t - BurstHoldUntil) / BurstTau);
        }
    }
}
