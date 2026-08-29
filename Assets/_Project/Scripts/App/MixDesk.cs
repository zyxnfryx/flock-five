using UnityEngine;

namespace FlockFive
{
    public enum MixLayer { Bed, Mid, Lead }

    public sealed class MixDesk : MonoBehaviour
    {
        public static MixDesk Live;
        AudioSource _air;
        float _leadUntil;
        const int Rate = 44100;

        public static void Boot(GameObject host)
        {
            if (Live != null) return;
            Live = host.GetComponent<MixDesk>();
            if (Live == null) Live = host.AddComponent<MixDesk>();
            Live.Build();
        }

        public bool LeadHot => Time.unscaledTime < _leadUntil;

        public bool AllowMid => Time.unscaledTime >= _leadUntil;

        public bool AllowBed => Time.unscaledTime >= _leadUntil + 1.6f;

        public float BedDuck => LeadHot ? 0.28f : 1f;

        public void MarkLead(float seconds)
        {
            float until = Time.unscaledTime + Mathf.Max(0.05f, seconds);
            if (until > _leadUntil) _leadUntil = until;
        }

        void Build()
        {
            _air = gameObject.AddComponent<AudioSource>();
            _air.playOnAwake = false;
            _air.loop = true;
            _air.spatialBlend = 0f;
            _air.clip = MakeAir();
            _air.volume = 0.045f;
            _air.Play();
        }

        void LateUpdate()
        {
            if (_air != null)
                _air.volume = 0.045f * BedDuck;
        }

        static AudioClip MakeAir()
        {
            const float dur = 6f;
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float s = Mathf.Sin(2f * Mathf.PI * 52f * t) * 0.6f;
                s += Mathf.Sin(2f * Mathf.PI * 78f * t) * 0.28f;
                s += Mathf.Sin(2f * Mathf.PI * 104f * t) * 0.1f;
                float env = 0.92f + 0.08f * Mathf.Sin(t * 0.55f);
                data[i] = s * env * 0.09f;
            }
            var c = AudioClip.Create("air", n, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }
    }
}
