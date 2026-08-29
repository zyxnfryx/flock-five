using UnityEngine;

namespace FlockFive
{
    public sealed class CamShake : MonoBehaviour
    {
        public static CamShake Live;
        Vector3 _rest;
        float _restSize;
        Camera _cam;
        float _t;
        float _dur;
        float _amp;
        float _twist;
        float _kick;
        float _seed;

        void Awake()
        {
            Live = this;
            _cam = GetComponent<Camera>();
            _rest = transform.localPosition;
            _restSize = _cam != null ? _cam.orthographicSize : 8.2f;
        }

        void OnEnable() => Live = this;

        void OnDisable()
        {
            if (Live == this) Live = null;
            Settle();
        }

        public static void Combo(int size)
        {
            if (Live == null) return;
            size = Mathf.Clamp(size, 1, Palette.Max);
            float n = size - 1;
            Live.Punch(0.16f + 0.07f * n, 0.09f + 0.07f * n, 1.5f + 1.2f * n, 0.10f + 0.05f * n);
        }

        public void Punch(float dur, float amp, float twist, float kick)
        {
            _dur = Mathf.Max(0.08f, dur);
            _amp = amp;
            _twist = twist;
            _kick = kick;
            _t = _dur;
            _seed = Random.Range(0f, 80f);
        }

        void LateUpdate()
        {
            if (_t <= 0f) return;
            _t -= Time.unscaledDeltaTime;
            if (_t <= 0f)
            {
                Settle();
                return;
            }
            float u = Mathf.Clamp01(_t / _dur);
            float decay = u * u;
            float w = Time.unscaledTime * 62f + _seed;
            transform.localPosition = _rest + new Vector3(
                Mathf.Sin(w) * _amp * decay,
                Mathf.Cos(w * 1.17f) * _amp * 0.72f * decay,
                0f);
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(w * 1.3f) * _twist * decay);
            if (_cam != null)
                _cam.orthographicSize = _restSize * (1f - _kick * Mathf.Sin((1f - u) * Mathf.PI) * decay);
        }

        void Settle()
        {
            _t = 0f;
            transform.localPosition = _rest;
            transform.localRotation = Quaternion.identity;
            if (_cam != null) _cam.orthographicSize = _restSize;
        }
    }
}
