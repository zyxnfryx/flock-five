using UnityEngine;

namespace FlockFive
{
    public sealed class SkyCycle : MonoBehaviour
    {
        public const float Duration = 200f;
        public static SkyCycle Instance { get; private set; }
        public static string Courtesy;

        Camera _cam;
        SpriteRenderer _veil;
        SpriteRenderer _moon;
        SpriteRenderer _halo;
        SpriteRenderer[] _stars;
        float[] _starPhase;
        float _t0;
        bool _welcomed;
        bool _rushing;
        bool _heldNight;
        float _rushFrom;
        float _rushDur;
        float _rushT;

        public static float Dusk
        {
            get
            {
                if (Instance == null) return 0f;
                if (Instance._heldNight) return 1f;
                if (Instance._rushing)
                {
                    float ru = Mathf.Clamp01(Instance._rushT / Mathf.Max(0.01f, Instance._rushDur));
                    return Mathf.Lerp(Instance._rushFrom, 1f, Mathf.SmoothStep(0f, 1f, ru));
                }
                float u = (Time.unscaledTime - Instance._t0) / Duration;
                float v = Mathf.Clamp01((u - 0.12f) / 0.88f);
                return Mathf.SmoothStep(0f, 1f, v);
            }
        }

        public static void RushNight(float seconds)
        {
            if (Instance == null) return;
            Instance._rushFrom = Dusk;
            Instance._rushDur = Mathf.Max(0.25f, seconds);
            Instance._rushT = 0f;
            Instance._rushing = true;
            Instance._welcomed = true;
        }

        public static SkyCycle Attach(Transform root, Camera cam)
        {
            var go = new GameObject("Sky");
            go.transform.SetParent(root, false);
            var sky = go.AddComponent<SkyCycle>();
            sky._cam = cam;
            sky.Build();
            return sky;
        }

        void OnEnable()
        {
            Instance = this;
            _t0 = Time.unscaledTime;
            Courtesy = null;
            _welcomed = false;
            _rushing = false;
            _heldNight = false;
        }

        void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        void Build()
        {
            var veilGo = WorldBuilder.Sprite("Veil", SpriteCatalog.Glow, new Vector3(0f, 0.35f, 7.6f), 1f, -18, transform);
            veilGo.transform.localScale = new Vector3(22f, 28f, 1f);
            _veil = veilGo.GetComponent<SpriteRenderer>();
            _veil.color = new Color(0.18f, 0.12f, 0.28f, 0f);

            var haloGo = WorldBuilder.Sprite("MoonHalo", SpriteCatalog.Glow, new Vector3(0.45f, 2.1f, 7.2f), 1f, -16, transform);
            haloGo.transform.localScale = new Vector3(2.8f, 2.8f, 1f);
            _halo = haloGo.GetComponent<SpriteRenderer>();
            _halo.color = new Color(1f, 0.92f, 0.72f, 0f);

            var moonGo = WorldBuilder.Sprite("Moon", SpriteCatalog.Moon, new Vector3(0.45f, 2.1f, 7.1f), 0.46f, -14, transform);
            _moon = moonGo.GetComponent<SpriteRenderer>();
            _moon.color = new Color(1f, 1f, 1f, 0f);

            var rng = new System.Random(41);
            _stars = new SpriteRenderer[16];
            _starPhase = new float[16];
            for (int i = 0; i < 16; i++)
            {
                float x = Mathf.Lerp(-1.85f, 1.85f, (float)rng.NextDouble());
                float y = Mathf.Lerp(3.6f, 7.35f, (float)rng.NextDouble());
                var go = WorldBuilder.Sprite("Star" + i, SpriteCatalog.Glow, new Vector3(x, y, 7.3f),
                    Mathf.Lerp(0.08f, 0.16f, (float)rng.NextDouble()), -15, transform);
                _stars[i] = go.GetComponent<SpriteRenderer>();
                _stars[i].color = new Color(1f, 0.96f, 0.82f, 0f);
                _starPhase[i] = (float)rng.NextDouble() * 40f;
            }
        }

        void LateUpdate()
        {
            if (_rushing)
            {
                _rushT += Time.unscaledDeltaTime;
                if (_rushT >= _rushDur)
                {
                    _rushing = false;
                    _heldNight = true;
                }
            }

            float d = Dusk;
            float moonIn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.16f, 0.55f, d));
            float y = Mathf.Lerp(2.15f, 5.12f, moonIn);
            var moonPos = new Vector3(0.12f, y, 7.1f);
            if (_moon != null)
            {
                _moon.transform.position = moonPos;
                _moon.transform.localScale = Vector3.one * Mathf.Lerp(0.34f, 0.42f, moonIn);
                _moon.color = new Color(1f, 0.98f, 0.92f, moonIn);
            }
            if (_halo != null)
            {
                _halo.transform.position = moonPos;
                float hs = Mathf.Lerp(1.9f, 2.55f, moonIn);
                _halo.transform.localScale = new Vector3(hs, hs, 1f);
                _halo.color = new Color(1f, 0.9f, 0.7f, moonIn * 0.32f);
            }
            if (_veil != null)
            {
                var duskCol = Color.Lerp(new Color(0.42f, 0.18f, 0.16f, 0f), new Color(0.14f, 0.12f, 0.34f, 0.36f), d);
                duskCol.a = Mathf.Lerp(0f, 0.36f, d);
                _veil.color = duskCol;
            }
            if (_stars != null)
            {
                float starA = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.38f, 0.85f, d));
                for (int i = 0; i < _stars.Length; i++)
                {
                    if (_stars[i] == null) continue;
                    float tw = 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(Time.time * 1.4f + _starPhase[i]));
                    var c = _stars[i].color;
                    c.a = starA * tw * 0.85f;
                    _stars[i].color = c;
                }
            }
            if (_cam != null)
                _cam.backgroundColor = Color.Lerp(new Color(0.07f, 0.12f, 0.08f), new Color(0.05f, 0.06f, 0.14f), d);

            if (!_welcomed && moonIn > 0.55f)
            {
                _welcomed = true;
                Courtesy = "The moon is visiting. Stay as long as you like.";
                Sfx.Moonrise(); // MixDesk night swell, not a Lead arpeggio.
            }

        }
    }
}
