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
        float _bolt;
        float _boltDur;
        float _boltAmp;
        float _boltKick;
        float _boltTwist;
        float _boltSeed;
        Vector2 _boltDir;

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
            size = Mathf.Clamp(size, 1, Palette.ComboMax);
            float n = Mathf.Min(size - 1, 10);
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

        public static void Bolt(float power)
        {
            if (Live == null) return;
            power = Mathf.Clamp01(power);
            Live._boltDur = Mathf.Lerp(0.46f, 0.78f, power);
            Live._boltAmp = Mathf.Lerp(0.07f, 0.16f, power);
            Live._boltKick = Mathf.Lerp(0.022f, 0.052f, power);
            Live._boltTwist = Mathf.Lerp(0.85f, 2.1f, power);
            Live._bolt = Live._boltDur;
            float ang = Random.Range(0f, Mathf.PI * 2f);
            Live._boltDir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) * 0.52f);
            Live._boltSeed = Random.Range(0f, 40f);
            Live._t = 0f;
        }

        void LateUpdate()
        {
            if (_bolt > 0f)
            {
                ApplyBolt();
                return;
            }
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

        void ApplyBolt()
        {
            _bolt -= Time.unscaledDeltaTime;
            if (_bolt <= 0f)
            {
                Settle();
                return;
            }
            float t = _boltDur - _bolt;
            float s1 = Mathf.Exp(-(t - 0.018f) * (t - 0.018f) / 0.00048f);
            float s2 = 0.58f * Mathf.Exp(-(t - 0.096f) * (t - 0.096f) / 0.00105f);
            float roll = Mathf.Exp(-t * 3.1f) * Mathf.Sin(t * 19.5f + _boltSeed);
            float env = s1 + s2 + 0.24f * roll;
            transform.localPosition = _rest + new Vector3(_boltDir.x, _boltDir.y, 0f) * (_boltAmp * env);
            transform.localRotation = Quaternion.Euler(0f, 0f, (s1 * 0.75f + s2 * 0.4f + roll * 0.28f) * _boltTwist);
            if (_cam != null)
                _cam.orthographicSize = _restSize * (1f - _boltKick * (s1 + 0.32f * s2));
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
