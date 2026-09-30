using UnityEngine;

namespace FlockFive
{
    // Gameplay weather. First storm after 70s, 75s of denser rain, 90s clear, repeat.
    // Rain is place air on MixDesk. Thunder is a distant Mid rumble, never Lead.
    public sealed class GardenStorm : MonoBehaviour
    {
        public const float FirstWait = 70f;
        public const float StormLen = 75f;
        public const float ClearLen = 90f;
        const float Fade = 2.1f;
        const int Drops = 177; // ~50% denser than 118
        // Past the bezel so a streak finishes off-screen before it wraps.
        const float Edge = 1.7f;

        public static GardenStorm Instance { get; private set; }
        public static float Wet { get; private set; }

        Transform[] _drop;
        SpriteRenderer[] _dropSr;
        float[] _spd;
        float[] _len;
        float[] _phase;
        SpriteRenderer _veil;
        SpriteRenderer _flash;
        float _t0;
        float _wet;
        float _nextBoom;
        float _flashT = 99f;
        float _flashPower;
        float _floor = -8.6f;
        bool _wasDry = true;
        bool _flashLit;
        float[] _dropA;
        float _veilA;
        int _frameW = -1;
        int _frameH;
        float _frameTall;
        float _frL, _frR, _frB, _frT;

        public static GardenStorm Attach(Transform root)
        {
            var go = new GameObject("Storm");
            go.transform.SetParent(root, false);
            return go.AddComponent<GardenStorm>();
        }

        void OnEnable()
        {
            Instance = this;
            _t0 = Time.unscaledTime;
#if UNITY_EDITOR
            if (System.IO.File.Exists("/tmp/flock-five-storm-now"))
            {
                try { System.IO.File.Delete("/tmp/flock-five-storm-now"); } catch { }
                _t0 = Time.unscaledTime - FirstWait - 1.2f;
            }
#endif
            Wet = 0f;
            _wet = 0f;
            _nextBoom = 6.5f;
            _flashT = 99f;
            _flashPower = 0f;
            _flashLit = false;
            _veilA = 0f;
            _frameW = -1;
        }

        void OnDisable()
        {
            if (Instance == this) Instance = null;
            Wet = 0f;
        }

        void Start() => StartCoroutine(Build());

        // Drops are invisible until the first storm. Spread the instantiate
        // so stage load does not hitch on one frame of 177 sprites.
        System.Collections.IEnumerator Build()
        {
            var veilGo = WorldBuilder.Sprite("StormVeil", SpriteCatalog.Glow, new Vector3(0f, 0.2f, 6.8f), 1f, 16, transform);
            veilGo.transform.localScale = new Vector3(24f, 30f * PortraitLock.TallFactor(), 1f);
            _veil = veilGo.GetComponent<SpriteRenderer>();
            _veil.color = new Color(0.07f, 0.09f, 0.13f, 0f);

            var flashGo = WorldBuilder.Sprite("Flash", SpriteCatalog.Glow, new Vector3(0f, 1.2f, 6.7f), 1f, 17, transform);
            flashGo.transform.localScale = new Vector3(22f, 28f, 1f);
            _flash = flashGo.GetComponent<SpriteRenderer>();
            _flash.color = new Color(0.82f, 0.88f, 1f, 0f);

            _drop = new Transform[Drops];
            _dropSr = new SpriteRenderer[Drops];
            _dropA = new float[Drops];
            _spd = new float[Drops];
            _len = new float[Drops];
            _phase = new float[Drops];
            var rng = new System.Random(29);
            FullFrame(out float left, out float right, out float bottom, out float top);
            for (int i = 0; i < Drops; i++)
            {
                float x = Mathf.Lerp(left - Edge, right + Edge, (float)rng.NextDouble());
                // Spread starts over the full column so sheets don't fall in lockstep.
                float y = Mathf.Lerp(bottom - Edge, top + Edge, (float)rng.NextDouble());
                _len[i] = Mathf.Lerp(0.70f, 1.45f, (float)rng.NextDouble());
                _spd[i] = Mathf.Lerp(8.5f, 26.5f, (float)rng.NextDouble());
                _phase[i] = (float)rng.NextDouble() * 2.8f;
                var go = WorldBuilder.Sprite("Drop" + i, SpriteCatalog.RainStreak, new Vector3(x, y, 0.4f), 1f, 15, transform);
                go.transform.localScale = new Vector3(0.82f, _len[i], 1f);
                go.transform.localRotation = Quaternion.Euler(0f, 0f, 9f + (float)rng.NextDouble() * 6f);
                _drop[i] = go.transform;
                _dropSr[i] = go.GetComponent<SpriteRenderer>();
                _dropSr[i].color = new Color(0.78f, 0.86f, 0.95f, 0f);
                if ((i & 11) == 11) yield return null;
            }
        }

        // Play-camera letterbox is not the screen. Bleed height × window aspect
        // is the glass, home indicator included. Never Screen.safeArea.
        static void FullFrame(out float left, out float right, out float bottom, out float top)
        {
            float halfH = WorldBuilder.CamOrtho * PortraitLock.TallFactor();
            float aspect = Screen.height > 1 ? (float)Screen.width / Screen.height : WorldBuilder.PortraitAspect;
            if (aspect < 0.2f) aspect = WorldBuilder.PortraitAspect;
            float halfW = halfH * aspect;
            left = -halfW;
            right = halfW;
            bottom = WorldBuilder.CamRestY - halfH;
            top = WorldBuilder.CamRestY + halfH;
        }

        void Paint(int i, float a)
        {
            if (_dropA == null || _dropSr == null || _dropSr[i] == null) return;
            if (_dropA[i] == a) return;
            _dropA[i] = a;
            _dropSr[i].color = new Color(0.78f, 0.86f, 0.94f, a);
        }

        // Screen size and letterbox height. Drops do not ask again while both hold.
        void Frame(out float left, out float right, out float bottom, out float top)
        {
            float tall = PortraitLock.TallFactor();
            int w = Screen.width;
            int h = Screen.height;
            if (w != _frameW || h != _frameH || tall != _frameTall)
            {
                FullFrame(out _frL, out _frR, out _frB, out _frT);
                _frameW = w;
                _frameH = h;
                _frameTall = tall;
            }
            left = _frL;
            right = _frR;
            bottom = _frB;
            top = _frT;
        }

        static bool WantStorm(float play)
        {
            if (play < FirstWait) return false;
            float u = play - FirstWait;
            float cycle = StormLen + ClearLen;
            return (u % cycle) < StormLen;
        }

        void LateUpdate()
        {
            float play = Time.unscaledTime - _t0;
            float want = WantStorm(play) ? 1f : 0f;
            _wet = Mathf.MoveTowards(_wet, want, Time.unscaledDeltaTime / Fade);
            Wet = _wet;

            // Steady rain keeps one veil alpha. Dry skips the write after the clear.
            float veilA = _wet > 0.001f ? 0.62f * _wet : 0f;
            if (_veil != null && veilA != _veilA)
            {
                _veilA = veilA;
                _veil.color = new Color(0.06f, 0.08f, 0.12f, veilA);
            }

            // Clear stretches are invisible. Frozen drops keep the last spread,
            // so the next fade-in is still a sheet, not a hitch every frame.
            bool dry = _wet <= 0.001f && want <= 0f;
            if (dry) _wasDry = true;
            else if (_wasDry && _phase != null)
            {
                // Holds only stagger a wrap, not the moment rain becomes visible.
                for (int i = 0; i < _phase.Length; i++) _phase[i] = 0f;
                _wasDry = false;
            }
            if (_drop != null && !dry)
            {
                // Full screen, including the letterbox past the home indicator.
                // Safe area is for HUD only — drops exit past every edge before recycle.
                Frame(out float left, out float right, out float bottom, out float top);
                _floor = bottom - Edge;
                float dt = Time.deltaTime;
                float wet = _wet;
                for (int i = 0; i < _drop.Length; i++)
                {
                    if (_drop[i] == null) continue;
                    // Per-drop hold so the curtain doesn't reset as one sheet.
                    if (_phase[i] > 0f)
                    {
                        _phase[i] -= dt;
                        Paint(i, 0f);
                        continue;
                    }
                    var p = _drop[i].position;
                    float fall = _spd[i] * dt * Mathf.Lerp(0.18f, 1f, wet);
                    float nx = p.x - (1.15f + 0.9f * ((_spd[i] - 8.5f) / 18f)) * dt * wet;
                    float ny = p.y - fall;
                    if (ny < _floor || nx < left - Edge || nx > right + Edge)
                    {
                        ny = Random.Range(top + 0.25f, top + Edge + 1.8f);
                        nx = Random.Range(left - 0.35f, right + Edge);
                        _phase[i] = Random.Range(0.05f, 1.35f);
                        _spd[i] = Random.Range(8.5f, 26.5f);
                    }
                    if (nx != p.x || ny != p.y)
                    {
                        p.x = nx;
                        p.y = ny;
                        _drop[i].position = p;
                    }
                    Paint(i, wet * Mathf.Lerp(0.34f, 0.76f, (i % 11) / 10f));
                }
            }

            // Same bolt curve. After the tail fades, stop rewriting the full-screen sprite.
            if (_flash != null && _flashT < 1.6f)
            {
                float ft = _flashT;
                float strike = Mathf.Exp(-(ft - 0.018f) * (ft - 0.018f) / 0.00042f);
                float echo = 0.62f * Mathf.Exp(-(ft - 0.098f) * (ft - 0.098f) / 0.00095f);
                float glow = 0.10f * Mathf.Exp(-ft * 4.4f);
                float a = (strike + echo + glow) * _flashPower;
                _flashT += Time.unscaledDeltaTime;
                if (a < 0.002f && ft > 0.2f)
                {
                    if (_flashLit)
                    {
                        _flash.color = new Color(0.78f, 0.86f, 1f, 0f);
                        float rest = 22f;
                        _flash.transform.localScale = new Vector3(rest, rest * 1.18f * PortraitLock.TallFactor(), 1f);
                        _flashLit = false;
                    }
                }
                else
                {
                    var warm = new Color(1f, 0.96f, 0.88f, a);
                    var cool = new Color(0.78f, 0.86f, 1f, a);
                    _flash.color = Color.Lerp(warm, cool, Mathf.Clamp01(ft * 6.5f));
                    float sc = 22f + 3.4f * strike + 1.6f * echo;
                    _flash.transform.localScale = new Vector3(sc, sc * 1.18f * PortraitLock.TallFactor(), 1f);
                    _flashLit = true;
                }
            }

            if (_wet > 0.45f)
            {
                _nextBoom -= Time.unscaledDeltaTime;
                if (_nextBoom <= 0f)
                {
                    // Fewer strikes — favor a solid roll over chatter.
                    float power = Random.value < 0.55f
                        ? Random.Range(0.82f, 1f)
                        : Random.Range(0.55f, 0.78f);
                    _nextBoom = power > 0.8f ? Random.Range(11f, 20f) : Random.Range(14f, 24f);
                    Boom(power);
                    // Rare echo only — not a rumble loop.
                    if (power > 0.9f && Random.value < 0.12f)
                        _nextBoom = Mathf.Min(_nextBoom, Random.Range(1.8f, 3.2f));
                }
            }
        }

        void Boom(float power)
        {
            _flashT = 0f;
            _flashPower = power;
            Sfx.Thunder();
            CamShake.Bolt(power);
            if (power > 0.78f) Sfx.Rumble();
        }
    }
}
