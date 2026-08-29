using UnityEngine;

namespace FlockFive
{
    public enum MixLayer { Bed, Mid, Lead }

    public sealed class MixDesk : MonoBehaviour
    {
        public static MixDesk Live;
        public const float DuckChirp = 0.18f;
        public const float DuckWhoosh = 0.15f;
        public const float DuckBreak = 0.08f;

        AudioSource[] _stems;
        float _leadUntil;
        float _leadDuck = 1f;
        float _moonLiftUntil;
        float _comboUntil = -99f;
        const int Rate = 44100;
        const float PlaceCap = 0.045f;
        const float PlaceMax = 0.06f;
        const float ComboCap = 0.04f;
        const float ComboWindow = 4f;
        const float ComboIn = 0.35f;

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

        public float BedDuck => LeadHot ? _leadDuck : 1f;

        public void MarkLead(float seconds, float duckRemain = DuckChirp)
        {
            bool wasHot = LeadHot;
            float until = Time.unscaledTime + Mathf.Max(0.05f, seconds);
            if (until > _leadUntil) _leadUntil = until;
            _leadDuck = wasHot ? Mathf.Min(_leadDuck, duckRemain) : duckRemain;
        }

        public void MoonLift(float seconds = 2.4f)
        {
            _moonLiftUntil = Time.unscaledTime + Mathf.Max(0.4f, seconds);
        }

        public void ComboWarm()
        {
            _comboUntil = Time.unscaledTime + ComboWindow;
        }

        void Build()
        {
            _stems = new AudioSource[4];
            _stems[0] = MakeLoop(MakePlace("place-day", 52f, 78f, 104f, 0.6f, 0.28f, 0.1f));
            _stems[1] = MakeLoop(MakePlace("place-dusk", 41f, 62f, 82f, 0.62f, 0.26f, 0.08f));
            _stems[2] = MakeLoop(MakeNight());
            _stems[3] = MakeLoop(MakeCombo());
        }

        AudioSource MakeLoop(AudioClip clip)
        {
            var a = gameObject.AddComponent<AudioSource>();
            a.playOnAwake = false;
            a.loop = true;
            a.spatialBlend = 0f;
            a.clip = clip;
            a.volume = 0f;
            a.Play();
            return a;
        }

        void LateUpdate()
        {
            if (!LeadHot) _leadDuck = 1f;
            if (_stems == null) return;

            float d = SkyCycle.Dusk;
            float day = 1f - Mathf.SmoothStep(0f, 0.42f, d);
            float night = Mathf.SmoothStep(0.38f, 1f, d);
            float dusk = Mathf.Clamp01(1f - Mathf.Abs(d - 0.5f) * 2.15f);
            bool moon = Time.unscaledTime < _moonLiftUntil;
            if (moon) night += 0.35f;
            float sum = day + dusk + night;
            if (sum < 0.001f) { day = 1f; sum = 1f; }
            day /= sum;
            dusk /= sum;
            night /= sum;

            float cap = moon ? PlaceMax : PlaceCap;
            float duck = BedDuck;
            SetStem(0, day * cap * duck);
            SetStem(1, dusk * cap * duck);
            SetStem(2, night * cap * duck);
            SetStem(3, ComboGain() * ComboCap * duck);
        }

        float ComboGain()
        {
            float age = Time.unscaledTime - (_comboUntil - ComboWindow);
            if (age < 0f || Time.unscaledTime >= _comboUntil) return 0f;
            if (age < ComboIn) return age / ComboIn;
            return 1f - (age - ComboIn) / (ComboWindow - ComboIn);
        }

        void SetStem(int i, float vol)
        {
            if (_stems[i] != null) _stems[i].volume = vol;
        }

        static AudioClip MakePlace(string name, float a, float b, float c, float wa, float wb, float wc)
        {
            const float dur = 6f;
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float s = Mathf.Sin(2f * Mathf.PI * a * t) * wa;
                s += Mathf.Sin(2f * Mathf.PI * b * t) * wb;
                s += Mathf.Sin(2f * Mathf.PI * c * t) * wc;
                float env = 0.92f + 0.08f * Mathf.Sin(t * 0.55f);
                data[i] = s * env * 0.09f;
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip MakeNight()
        {
            const float dur = 8f;
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float s = Mathf.Sin(2f * Mathf.PI * 36f * t) * 0.55f;
                s += Mathf.Sin(2f * Mathf.PI * 54f * t) * 0.32f;
                s += Mathf.Sin(2f * Mathf.PI * 72f * t) * 0.12f;
                float env = 0.9f + 0.1f * Mathf.Sin(t * 0.28f);
                data[i] = s * env * 0.08f;
            }
            var clip = AudioClip.Create("place-night", n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip MakeCombo()
        {
            const float dur = 8f;
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float s = Mathf.Sin(2f * Mathf.PI * 78f * t) * 0.55f;
                s += Mathf.Sin(2f * Mathf.PI * 117f * t) * 0.38f;
                float env = 0.92f + 0.08f * Mathf.Sin(t * 0.4f);
                data[i] = s * env * 0.08f;
            }
            var clip = AudioClip.Create("combo-fifth", n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
