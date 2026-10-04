namespace FlockFive
{
    // The ONE rhythm-tap detector. Four taps (three gaps) inside the tempo window
    // fire it. Forgiving: a steady beat (every gap within 30% of the mean) or a
    // syncopated short-long-short figure (first and last gap match, the middle one
    // is clearly longer). Every gap must sit in 0.2 to 1.0 s; a gap outside that
    // restarts the count with the late tap as the new first tap. A hit clears the
    // memory so the next four taps start fresh.
    public sealed class RhythmTap
    {
        public const float MinGap = 0.2f;
        public const float MaxGap = 1.0f;
        public const float Tolerance = 0.30f;

        readonly float[] _t = new float[4];
        int _n;

        public void Reset()
        {
            _n = 0;
        }

        public bool Hear(float now)
        {
            if (_n > 0)
            {
                float gap = now - _t[_n - 1];
                if (gap < 0f || gap > MaxGap) _n = 0;
                else if (gap < MinGap) return false;
            }
            if (_n == _t.Length)
            {
                for (int i = 1; i < _t.Length; i++) _t[i - 1] = _t[i];
                _n = _t.Length - 1;
            }
            _t[_n++] = now;
            if (_n < _t.Length) return false;
            float g0 = _t[1] - _t[0];
            float g1 = _t[2] - _t[1];
            float g2 = _t[3] - _t[2];
            if (!Fits(g0, g1, g2)) return false;
            _n = 0;
            return true;
        }

        static bool Near(float a, float b)
        {
            float m = (a + b) * 0.5f;
            if (m < 0.001f) return false;
            return UnityEngine.Mathf.Abs(a - b) / m <= Tolerance;
        }

        static bool Fits(float g0, float g1, float g2)
        {
            float mean = (g0 + g1 + g2) / 3f;
            if (mean < 0.001f) return false;
            bool steady = UnityEngine.Mathf.Abs(g0 - mean) / mean <= Tolerance
                && UnityEngine.Mathf.Abs(g1 - mean) / mean <= Tolerance
                && UnityEngine.Mathf.Abs(g2 - mean) / mean <= Tolerance;
            if (steady) return true;
            // Short-long-short.
            return Near(g0, g2) && g1 >= g0 * 1.4f && g1 <= g0 * 4f;
        }
    }
}
