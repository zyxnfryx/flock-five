namespace FlockFive
{
    // Even gaps in a tempo window. Four taps (three gaps) lock the pattern.
    public sealed class RhythmTap
    {
        readonly float[] _gaps = new float[8];
        int _n;
        float _last = -1f;

        public void Reset()
        {
            _n = 0;
            _last = -1f;
        }

        public bool Hear(float now)
        {
            if (_last < 0f)
            {
                _last = now;
                _n = 0;
                return false;
            }
            float gap = now - _last;
            _last = now;
            if (gap < 0.25f || gap > 0.9f)
            {
                _n = 0;
                return false;
            }
            if (_n < _gaps.Length) _gaps[_n++] = gap;
            else
            {
                for (int i = 1; i < _gaps.Length; i++) _gaps[i - 1] = _gaps[i];
                _gaps[_gaps.Length - 1] = gap;
            }
            if (_n < 3) return false;
            int use = _n < 6 ? _n : 6;
            int start = _n - use;
            float sum = 0f;
            for (int i = start; i < _n; i++) sum += _gaps[i];
            float mean = sum / use;
            if (mean < 0.001f) return false;
            for (int i = start; i < _n; i++)
            {
                if (UnityEngine.Mathf.Abs(_gaps[i] - mean) / mean > 0.25f) return false;
            }
            return true;
        }
    }
}
