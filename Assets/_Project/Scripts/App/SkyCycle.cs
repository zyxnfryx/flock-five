using UnityEngine;

namespace FlockFive
{
    public sealed class SkyCycle : MonoBehaviour
    {
        // One day, chosen by level index. Six beats: dawn, morning, noon, dusk,
        // moonrise, night. The next garden after night is dawn again.
        // Load used to rebuild at dawn, so a night clear snapped back to day.
        public const float Duration = 66f;
        public const int Beats = 6;
        // Peak phase speed. Smoothstep peaks at 1.5x this. A beat (1/6) takes ~1.4s.
        public const float PhasePerSecond = 0.12f;
        public static SkyCycle Instance { get; private set; }
        public static string Courtesy;

        static bool _haveCarry;
        static float _carryPhase;
        static bool _parkNext;
        static bool _queued;
        static float _queuedTarget;

        // Darkness at the six beats, and the same dawn again at 1 so the wrap meets.
        static readonly float[] DarkKey = { 0.14f, 0.03f, 0.00f, 0.48f, 0.74f, 0.98f, 0.14f };

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
        bool _cycling;
        bool _gliding;
        float _parked;
        float _levelPhase;
        float _savedPhase;
        float _glideFrom;
        float _glideTo;
        float _glideDur;
        float _glideStart;
        Color _veilCol;
        Color _camCol;
        bool _veilOn;
        bool _camSet;
        bool _starsOff;

        // 0 dawn … 5 night. Index is LevelData.Index (0 = Dawn Garden).
        public static float PhaseForLevel(int index)
        {
            if (index < 0) index = 0;
            return (index % Beats) / (float)Beats;
        }

        public static float NightPhase => PhaseForLevel(Beats - 1);

        // Forward travel on the circle, in [0, 1). The same point stays put.
        public static float ForwardDelta(float from, float to)
        {
            float d = Mathf.Repeat(to - from, 1f);
            if (d > 0.9999f) d = 0f;
            return d;
        }

        // Shared path. u = 0 is `from`, u = 1 is `to`, and every step walks forward.
        public static float ForwardAt(float from, float to, float u)
        {
            u = Mathf.Clamp01(u);
            float s = u * u * (3f - 2f * u);
            return Mathf.Repeat(from + ForwardDelta(from, to) * s, 1f);
        }

        // How long the shared glide takes. Never faster than PhasePerSecond.
        public static float GlideSeconds(float from, float to)
        {
            float d = ForwardDelta(from, to);
            if (d < 0.0008f) return 0f;
            float t = d / PhasePerSecond;
            return t < 0.35f ? 0.35f : t;
        }

        // Veil, moon, and stars read this. Ends meet, so night can roll into dawn.
        public static float Darkness(float phase)
        {
            phase = Mathf.Repeat(phase, 1f);
            if (phase >= 1f) phase = 0f;
            float span = 1f / Beats;
            for (int i = 0; i < Beats; i++)
            {
                float a = i * span;
                float b = (i + 1) * span;
                if (phase > b && i < Beats - 1) continue;
                float u = b <= a ? 1f : (phase - a) / (b - a);
                if (u < 0f) u = 0f;
                else if (u > 1f) u = 1f;
                u = u * u * (3f - 2f * u);
                return Mathf.Lerp(DarkKey[i], DarkKey[i + 1], u);
            }
            return DarkKey[0];
        }

        // Clock dusk (0 day … 1 night) onto the cycle, for a finale that starts
        // before Present. Picks the closest phase so the darkness does not pop.
        public static float PhaseForDarkness(float darkness)
        {
            darkness = Mathf.Clamp01(darkness);
            float bestP = 0f;
            float best = 999f;
            for (int i = 0; i <= 128; i++)
            {
                float p = i / 128f;
                if (p >= 1f) p = 0f;
                float err = Mathf.Abs(Darkness(p) - darkness);
                if (err < best)
                {
                    best = err;
                    bestP = p;
                }
            }
            return bestP;
        }

        public static float Phase
        {
            get
            {
                if (Instance == null) return 0f;
                if (Instance._gliding || Instance._cycling) return Instance.PhaseNow;
                return PhaseForDarkness(Dusk);
            }
        }

        // Next Present parks on the level instead of gliding. The shot uses this
        // so the night garden is already night before the transition starts.
        public static void ParkNextPresent()
        {
            _parkNext = true;
        }

        public static void ResetForTest()
        {
            _haveCarry = false;
            _carryPhase = 0f;
            _parkNext = false;
            _queued = false;
            if (Instance == null) return;
            Instance._gliding = false;
            Instance._cycling = false;
            Instance._haveSaved = false;
            Instance._heldNight = false;
            Instance._easing = false;
            Instance._rushing = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _haveCarry = false;
            _carryPhase = 0f;
            _parkNext = false;
            _queued = false;
            Instance = null;
            Courtesy = null;
        }

        float PhaseNow
        {
            get
            {
                if (_gliding)
                {
                    float dur = _glideDur > 0.01f ? _glideDur : 0.01f;
                    float u = (PlayClock.Now - _glideStart) / dur;
                    if (u >= 1f) return _glideTo;
                    if (u < 0f) u = 0f;
                    return ForwardAt(_glideFrom, _glideTo, u);
                }
                return _parked;
            }
        }

        public static float Dusk
        {
            get
            {
                if (Instance == null) return 0f;
                if (Instance._gliding || Instance._cycling)
                    return Darkness(Instance.PhaseNow);
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

        // Night for the last combo, on the shared forward path. The level phase
        // is remembered so the finale can walk forward back to it without a pop.
        public static void SnapNight()
        {
            if (Instance == null) return;
            var s = Instance;
            if (!s._haveSaved)
            {
                s._savedPhase = (s._cycling || s._gliding) ? s._levelPhase : PhaseForDarkness(Dusk);
                s._savedDusk = s.ClockDusk();
                s._haveSaved = true;
            }
            float from = (s._cycling || s._gliding) ? s.PhaseNow : s._savedPhase;
            s._welcomed = true;
            s.Glide(from, NightPhase, GlideSeconds(from, NightPhase));
        }

        public static void EaseToSaved(float seconds)
        {
            if (Instance == null) return;
            var s = Instance;
            float to = s._haveSaved ? s._savedPhase : s.PhaseNow;
            float from = s._cycling || s._gliding ? s.PhaseNow : to;
            s._haveSaved = false;
            s._heldNight = false;
            s._rushing = false;
            float dur = GlideSeconds(from, to);
            if (seconds > dur) dur = seconds;
            s.Glide(from, to, dur);
        }

        // Level index picks the sky. A cold load parks there (no dawn flash).
        // A stage change glides forward from the phase the last sky left behind.
        public static void Present(int levelIndex)
        {
            float target = PhaseForLevel(levelIndex);
            _queued = false;
            if (Instance == null)
            {
                _queuedTarget = target;
                _queued = true;
                return;
            }
            Instance.PresentNow(target);
        }

        // The garden disable path also remembers. Tests call this before destroy
        // because an edit-mode object may not run OnDisable.
        public static void Remember()
        {
            if (Instance == null) return;
            if (!Instance._cycling && !Instance._gliding) return;
            _carryPhase = Instance.PhaseNow;
            _haveCarry = true;
        }

        void PresentNow(float target)
        {
            target = Mathf.Repeat(target, 1f);
            _levelPhase = target;
            bool park = _parkNext || !_haveCarry;
            float from = _haveCarry ? _carryPhase : target;
            _parkNext = false;
            _haveCarry = false;
            _haveSaved = false;
            if (park)
            {
                Park(target);
                return;
            }
            Glide(from, target, GlideSeconds(from, target));
        }

        void Glide(float from, float to, float seconds)
        {
            from = Mathf.Repeat(from, 1f);
            to = Mathf.Repeat(to, 1f);
            if (seconds <= 0.001f || ForwardDelta(from, to) < 0.0008f)
            {
                Park(to);
                return;
            }
            _glideFrom = from;
            _glideTo = to;
            _glideDur = seconds;
            _glideStart = PlayClock.Now;
            _gliding = true;
            _cycling = true;
            _rushing = false;
            _heldNight = false;
            _easing = false;
            ApplyLook();
        }

        void Park(float phase)
        {
            _parked = Mathf.Repeat(phase, 1f);
            _cycling = true;
            _gliding = false;
            _rushing = false;
            _heldNight = false;
            _easing = false;
            ApplyLook();
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
            _gliding = false;
            _cycling = false;
            if (!_queued) return;
            _queued = false;
            PresentNow(_queuedTarget);
        }

        void OnDisable()
        {
            // The garden is destroyed between stages (and on the way home).
            // Keep the phase so the next Present walks forward instead of popping.
            if (_cycling || _gliding)
                Remember();
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
            if (_gliding)
            {
                float dur = _glideDur > 0.01f ? _glideDur : 0.01f;
                if ((PlayClock.Now - _glideStart) / dur >= 1f)
                    Park(_glideTo);
            }
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
                _rushT += ShowClock.Delta;
                if (_rushT >= _rushDur)
                {
                    _rushing = false;
                    _heldNight = true;
                }
            }

            ApplyLook();

            if (!_welcomed && _moon != null && _moon.enabled && _moon.color.a > 0.55f)
            {
                _welcomed = true;
                Courtesy = "The moon is visiting. Stay as long as you like.";
                Sfx.Moonrise(); // MixDesk night swell, not a Lead arpeggio.
            }

        }

        // Veil, moon, stars, and the camera clear. Park and Glide call this too,
        // so the first frame is already this phase and not the brown camera clear.
        void ApplyLook()
        {
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
