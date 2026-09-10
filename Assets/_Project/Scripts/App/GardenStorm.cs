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
            if (System.IO.File.Exists("/tmp/flock-five-storm-now"))
            {
                try { System.IO.File.Delete("/tmp/flock-five-storm-now"); } catch { }
                _t0 = Time.unscaledTime - FirstWait - 1.2f;
            }
            Wet = 0f;
            _wet = 0f;
            _nextBoom = 6.5f;
            _flashT = 99f;
            _flashPower = 0f;
        }

        void OnDisable()
        {
            if (Instance == this) Instance = null;
            Wet = 0f;
        }

        void Start() => Build();

        void Build()
        {
            var veilGo = WorldBuilder.Sprite("StormVeil", SpriteCatalog.Glow, new Vector3(0f, 0.2f, 6.8f), 1f, 16, transform);
            veilGo.transform.localScale = new Vector3(24f, 30f, 1f);
            _veil = veilGo.GetComponent<SpriteRenderer>();
            _veil.color = new Color(0.07f, 0.09f, 0.13f, 0f);

            var flashGo = WorldBuilder.Sprite("Flash", SpriteCatalog.Glow, new Vector3(0f, 1.2f, 6.7f), 1f, 17, transform);
            flashGo.transform.localScale = new Vector3(22f, 28f, 1f);
            _flash = flashGo.GetComponent<SpriteRenderer>();
            _flash.color = new Color(0.82f, 0.88f, 1f, 0f);

            _drop = new Transform[Drops];
            _dropSr = new SpriteRenderer[Drops];
            _spd = new float[Drops];
            _len = new float[Drops];
            _phase = new float[Drops];
            var rng = new System.Random(29);
            for (int i = 0; i < Drops; i++)
            {
                float x = Mathf.Lerp(-5.4f, 5.4f, (float)rng.NextDouble());
                // Spread starts over a taller column so sheets don't fall in lockstep.
                float y = Mathf.Lerp(-8.4f, 14.5f, (float)rng.NextDouble());
                _len[i] = Mathf.Lerp(0.70f, 1.45f, (float)rng.NextDouble());
                _spd[i] = Mathf.Lerp(8.5f, 26.5f, (float)rng.NextDouble());
                _phase[i] = (float)rng.NextDouble() * 2.8f;
                var go = WorldBuilder.Sprite("Drop" + i, SpriteCatalog.RainStreak, new Vector3(x, y, 0.4f), 1f, 15, transform);
                go.transform.localScale = new Vector3(0.82f, _len[i], 1f);
                go.transform.localRotation = Quaternion.Euler(0f, 0f, 9f + (float)rng.NextDouble() * 6f);
                _drop[i] = go.transform;
                _dropSr[i] = go.GetComponent<SpriteRenderer>();
                _dropSr[i].color = new Color(0.78f, 0.86f, 0.95f, 0f);
            }
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

            if (_veil != null)
                _veil.color = new Color(0.06f, 0.08f, 0.12f, 0.62f * _wet);

            if (_drop != null)
            {
                float dt = Time.deltaTime;
                for (int i = 0; i < _drop.Length; i++)
                {
                    if (_drop[i] == null) continue;
                    // Per-drop hold so the curtain doesn't reset as one sheet.
                    if (_phase[i] > 0f)
                    {
                        _phase[i] -= dt;
                        if (_dropSr[i] != null)
                            _dropSr[i].color = new Color(0.78f, 0.86f, 0.94f, 0f);
                        continue;
                    }
                    var p = _drop[i].position;
                    float fall = _spd[i] * dt * Mathf.Lerp(0.18f, 1f, _wet);
                    p.y -= fall;
                    p.x -= (1.15f + 0.9f * ((_spd[i] - 8.5f) / 18f)) * dt * _wet;
                    if (p.y < -8.6f)
                    {
                        p.y = Random.Range(9.2f, 16.5f);
                        p.x = Random.Range(-5.4f, 5.4f);
                        _phase[i] = Random.Range(0.05f, 1.35f);
                        _spd[i] = Random.Range(8.5f, 26.5f);
                    }
                    _drop[i].position = p;
                    if (_dropSr[i] != null)
                    {
                        float a = _wet * Mathf.Lerp(0.34f, 0.76f, (i % 11) / 10f);
                        _dropSr[i].color = new Color(0.78f, 0.86f, 0.94f, a);
                    }
                }
            }

            if (_flash != null)
            {
                float ft = _flashT;
                float strike = Mathf.Exp(-(ft - 0.018f) * (ft - 0.018f) / 0.00042f);
                float echo = 0.62f * Mathf.Exp(-(ft - 0.098f) * (ft - 0.098f) / 0.00095f);
                float glow = 0.10f * Mathf.Exp(-ft * 4.4f);
                float a = (strike + echo + glow) * _flashPower;
                var warm = new Color(1f, 0.96f, 0.88f, a);
                var cool = new Color(0.78f, 0.86f, 1f, a);
                _flash.color = Color.Lerp(warm, cool, Mathf.Clamp01(ft * 6.5f));
                float sc = 22f + 3.4f * strike + 1.6f * echo;
                _flash.transform.localScale = new Vector3(sc, sc * 1.18f, 1f);
                _flashT += Time.unscaledDeltaTime;
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
