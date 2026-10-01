using UnityEngine;

namespace FlockFive
{
    public sealed class SkyCycle : MonoBehaviour
    {
        // Dawn to last-light inside one garden. Load() rebuilds the sky at dawn.
        public const float Duration = 66f;
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
        bool _easing;
        float _easeFrom;
        float _easeTo;
        float _easeDur;
        float _easeT;
        float _savedDusk;
        bool _haveSaved;
        Color _veilCol;
        Color _camCol;
        bool _veilOn;
        bool _camSet;
        bool _starsOff;

        public static float Dusk
        {
            get
            {
                if (Instance == null) return 0f;
                if (Instance._easing)
                {
                    float eu = Mathf.Clamp01(Instance._easeT / Mathf.Max(0.01f, Instance._easeDur));
                    return Mathf.Lerp(Instance._easeFrom, Instance._easeTo, Mathf.SmoothStep(0f, 1f, eu));
                }
                if (Instance._heldNight) return 1f;
                if (Instance._rushing)
                {
                    float ru = Mathf.Clamp01(Instance._rushT / Mathf.Max(0.01f, Instance._rushDur));
                    return Mathf.Lerp(Instance._rushFrom, 1f, Mathf.SmoothStep(0f, 1f, ru));
                }
                float u = (PlayClock.Now - Instance._t0) / Duration;
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
            Instance._easing = false;
            Instance._welcomed = true;
        }

        float ClockDusk()
        {
            float u = (PlayClock.Now - _t0) / Duration;
            float v = Mathf.Clamp01((u - 0.12f) / 0.88f);
            return Mathf.SmoothStep(0f, 1f, v);
        }

        // Instant night for the last combo. The clock dusk is remembered so the
        // finale can ease back without a pop.
        public static void SnapNight()
        {
            if (Instance == null) return;
            var s = Instance;
            if (!s._haveSaved)
            {
                s._savedDusk = s.ClockDusk();
                s._haveSaved = true;
            }
            s._rushing = false;
            s._easing = false;
            s._heldNight = true;
            s._welcomed = true;
        }

        public static void EaseToSaved(float seconds)
        {
            if (Instance == null) return;
            var s = Instance;
            s._easeFrom = Dusk;
            s._easeTo = s._haveSaved ? s._savedDusk : s.ClockDusk();
            s._easeDur = Mathf.Max(0.25f, seconds);
            s._easeT = 0f;
            s._easing = true;
            s._heldNight = false;
            s._rushing = false;
            s._haveSaved = false;
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
            _t0 = PlayClock.Now;
            Courtesy = null;
            _welcomed = false;
            _rushing = false;
            _heldNight = false;
            _easing = false;
            _haveSaved = false;
        }

        void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        void Build()
        {
            var veilGo = WorldBuilder.Sprite("Veil", SpriteCatalog.Glow, new Vector3(0f, 0.35f, 7.6f), 1f, -18, transform);
            veilGo.transform.localScale = new Vector3(22f, 28f * PortraitLock.TallFactor(), 1f);
            _veil = veilGo.GetComponent<SpriteRenderer>();
            _veil.color = new Color(0.18f, 0.12f, 0.28f, 0f);
            _veil.enabled = false;

            var haloGo = WorldBuilder.Sprite("MoonHalo", SpriteCatalog.Glow, new Vector3(0.45f, 2.1f, 7.2f), 1f, -16, transform);
            haloGo.transform.localScale = new Vector3(2.8f, 2.8f, 1f);
            _halo = haloGo.GetComponent<SpriteRenderer>();
            _halo.color = new Color(1f, 0.92f, 0.72f, 0f);
            _halo.enabled = false;

            var moonGo = WorldBuilder.Sprite("Moon", SpriteCatalog.Moon, new Vector3(0.45f, 2.1f, 7.1f), 0.46f, -14, transform);
            _moon = moonGo.GetComponent<SpriteRenderer>();
            _moon.color = new Color(1f, 1f, 1f, 0f);
            _moon.enabled = false;

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
                _stars[i].enabled = false;
                _starPhase[i] = (float)rng.NextDouble() * 40f;
            }
        }

        void LateUpdate()
        {
            if (GamePause.Paused) return;
            if (_easing)
            {
                _easeT += PlayClock.Delta;
                if (_easeT >= _easeDur)
                {
                    _easing = false;
                    float v = Mathf.Clamp01(_easeTo);
                    float u = 0.12f + v * 0.88f;
                    _t0 = PlayClock.Now - u * Duration;
                }
            }
            if (_rushing)
            {
                _rushT += PlayClock.Delta;
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
            float bolt = GardenStorm.SkyFlash;
            float wet = GardenStorm.Wet;
            bool moonOn = moonIn > 0.004f;
            if (_moon != null)
            {
                if (_moon.enabled != moonOn) _moon.enabled = moonOn;
                if (moonOn)
                {
                    _moon.transform.position = moonPos;
                    _moon.transform.localScale = Vector3.one * Mathf.Lerp(0.34f, 0.42f, moonIn);
                    _moon.color = new Color(1f, 0.98f, 0.92f, moonIn);
                }
            }
            if (_halo != null)
            {
                if (_halo.enabled != moonOn) _halo.enabled = moonOn;
                if (moonOn)
                {
                    _halo.transform.position = moonPos;
                    float hs = Mathf.Lerp(1.9f, 2.55f, moonIn);
                    _halo.transform.localScale = new Vector3(hs, hs, 1f);
                    _halo.color = new Color(1f, 0.9f, 0.7f, moonIn * 0.32f);
                }
            }
            if (_veil != null)
            {
                var duskCol = Color.Lerp(new Color(0.42f, 0.18f, 0.16f, 0f), new Color(0.14f, 0.12f, 0.34f, 0.36f), d);
                duskCol.a = Mathf.Lerp(0f, 0.36f, d);
                duskCol = Color.Lerp(duskCol, new Color(0.10f, 0.12f, 0.18f, Mathf.Max(duskCol.a, 0.34f)), wet * 0.55f);
                if (bolt > 0.004f)
                    duskCol = Color.Lerp(duskCol, new Color(0.70f, 0.78f, 0.92f, Mathf.Max(duskCol.a, 0.28f)), bolt * 0.8f);
                bool veilOn = duskCol.a > 0.004f;
                if (veilOn)
                {
                    if (!_veil.enabled) _veil.enabled = true;
                    if (!_veilOn || !SameByte(_veilCol, duskCol))
                    {
                        _veilOn = true;
                        _veilCol = duskCol;
                        _veil.color = duskCol;
                    }
                }
                else if (_veil.enabled)
                {
                    _veil.enabled = false;
                    _veilOn = false;
                    _veilCol = duskCol;
                }
            }
            if (_stars != null)
            {
                float starA = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.38f, 0.85f, d));
                if (starA < 0.004f)
                {
                    if (!_starsOff)
                    {
                        _starsOff = true;
                        for (int i = 0; i < _stars.Length; i++)
                            if (_stars[i] != null) _stars[i].enabled = false;
                    }
                }
                else
                {
                    _starsOff = false;
                    for (int i = 0; i < _stars.Length; i++)
                    {
                        if (_stars[i] == null) continue;
                        if (!_stars[i].enabled) _stars[i].enabled = true;
                        float tw = 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(Time.time * 1.4f + _starPhase[i]));
                        var c = _stars[i].color;
                        c.a = starA * tw * 0.85f;
                        _stars[i].color = c;
                    }
                }
            }
            if (_cam != null)
            {
                var bg = Color.Lerp(new Color(0.07f, 0.12f, 0.08f), new Color(0.05f, 0.06f, 0.14f), d);
                bg = Color.Lerp(bg, new Color(0.05f, 0.07f, 0.10f), wet * 0.45f);
                if (bolt > 0.004f)
                    bg = Color.Lerp(bg, new Color(0.55f, 0.66f, 0.82f), bolt * 0.85f);
                if (!_camSet || !SameByte(_camCol, bg))
                {
                    _camSet = true;
                    _camCol = bg;
                    _cam.backgroundColor = bg;
                }
            }

            if (!_welcomed && moonIn > 0.55f)
            {
                _welcomed = true;
                Courtesy = "The moon is visiting. Stay as long as you like.";
                Sfx.Moonrise(); // MixDesk night swell, not a Lead arpeggio.
            }

        }

        static bool SameByte(Color a, Color b)
        {
            return Byte(a.r) == Byte(b.r) && Byte(a.g) == Byte(b.g)
                && Byte(a.b) == Byte(b.b) && Byte(a.a) == Byte(b.a);
        }

        static int Byte(float u)
        {
            int v = (int)(Mathf.Clamp01(u) * 255f + 0.5f);
            return v > 255 ? 255 : v;
        }
    }
}
