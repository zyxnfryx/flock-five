using UnityEngine;

namespace FlockFive
{
    // Gameplay weather. First storm after 70s, 75s of denser rain, 90s clear, repeat.
    // Rain is place air on MixDesk. Thunder is Mid, never Lead: a crack on the big
    // bolts, then one delayed roll from the same clip pool.
    public sealed class GardenStorm : MonoBehaviour
    {
        public const float FirstWait = 70f;
        public const float StormLen = 75f;
        public const float ClearLen = 90f;
        const float Fade = 2.1f;
        // One batched sheet. Phones use fewer quads; each is a little longer so the curtain stays full.
        const int DropsDesk = 156;
        const int DropsPhone = 120;
        const float StreakW = 0.125f; // RainStreak width at scale 1
        const int Segs = 36;
        const int PathCap = 10;
        const float BarW = 1.35f;
        const float LeanDt = 0.042f; // ~24 fps — fewer bolt pieces, wider strike gap
        // Past the bezel so a streak finishes off-screen before it wraps.
        const float Edge = 1.7f;

        public static GardenStorm Instance { get; private set; }
        public static float Wet { get; private set; }

        // Same curve the flash sprite uses, so sky tints do not wait on script order.
        public static float SkyFlash
        {
            get
            {
                float ft = PlayClock.Now - _skyAt;
                if (ft < 0f || ft > 0.5f) return 0f;
                return FlashShape(ft, _skyBig) * _skyPow;
            }
        }

        Mesh _mesh;
        MeshRenderer _sheet;
        Material _sheetMat;
        MaterialPropertyBlock _sheetBlock;
        Vector3[] _verts;
        Color32[] _cols;
        float[] _x;
        float[] _y;
        float[] _spd;
        float[] _phase;
        float[] _amp;
        float[] _cos;
        float[] _sin;
        float[] _hw;
        float[] _hh;
        int _colQ = -1;
        Bounds _sheetBounds;
        static readonly int SpritePropsId = Shader.PropertyToID("unity_SpriteProps");
        static readonly int SpriteColorId = Shader.PropertyToID("unity_SpriteColor");
        SpriteRenderer _veil;
        SpriteRenderer _flash;
        float _play;
        float _wet;
        float _nextBoom;
        int _rainSync;
        float _floor = -8.6f;
        bool _wasDry = true;
        bool _flashLit;
        float _veilA;
        int _frameW = -1;
        int _frameH;
        float _frameTall;
        float _frL, _frR, _frB, _frT;
        Transform[] _bolt;
        SpriteRenderer[] _boltSr;
        float[] _segMul;
        float[] _px;
        float[] _py;
        int _pn;
        int _boltN;
        int _boltHi;
        int _boltQ = -1;
        bool _boltOn;
        float _boltAt;
        float _boltLife = 0.3f;
        float _boltPow = 1f;
        float _step;
        float _rumbleIn = -1f;
        float _rumblePow;
        static float _skyAt = -100f;
        static float _skyPow;
        static bool _skyBig;
        Transform _cue;
        SpriteRenderer _track;
        SpriteRenderer _fill;
        Transform _fillT;
        int _barQ = -1;
        int _fadeQ = -1;
        bool _cueOn;
        int _cueFrame = int.MinValue;
        int _cueFrameH;
        float _cueTall;

        public static GardenStorm Attach(Transform root)
        {
            var go = new GameObject("Storm");
            go.transform.SetParent(root, false);
            return go.AddComponent<GardenStorm>();
        }

        void OnEnable()
        {
            Instance = this;
            _play = 0f;
            _rainSync = 0;
#if UNITY_EDITOR
            if (System.IO.File.Exists("/tmp/flock-five-storm-now"))
            {
                try { System.IO.File.Delete("/tmp/flock-five-storm-now"); } catch { }
                _play = FirstWait + 1.2f;
            }
#endif
            Wet = 0f;
            _wet = 0f;
            _nextBoom = 6.5f;
            _flashLit = false;
            _veilA = 0f;
            _colQ = -1;
            _frameW = -1;
            _boltOn = false;
            _boltQ = -1;
            _rumbleIn = -1f;
            _step = 0f;
            _skyAt = -100f;
            _skyPow = 0f;
            _skyBig = false;
            _barQ = -1;
            _fadeQ = -1;
            _cueOn = false;
            _cueFrame = int.MinValue;
            EndBolt();
            HideCue();
        }

        void OnDisable()
        {
            if (Instance == this) Instance = null;
            Wet = 0f;
            _skyAt = -100f;
            EndBolt();
            HideCue();
            if (_sheet != null) _sheet.enabled = false;
        }

        void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
            if (_sheetMat != null) Destroy(_sheetMat);
        }

        // Resume frame's unscaled step is the whole suspension. The app handler
        // drops that frame once. Rain still rebuilds from the foreground state.
        void OnApplicationPause(bool paused)
        {
            if (paused) FlockFiveApp.NoteAppBackground();
            else FlockFiveApp.NoteAppResume();
            if (!paused && _rainSync == 0) _rainSync = 1;
        }

        void OnApplicationFocus(bool focus)
        {
            if (focus) FlockFiveApp.NoteAppResume();
            else FlockFiveApp.NoteAppBackground();
            if (focus && _rainSync == 0) _rainSync = 1;
        }

        void Start() => StartCoroutine(Build());

        // Drops stay off until the first storm. One mesh, built on this frame — not 177 sprites.
        System.Collections.IEnumerator Build()
        {
            var veilGo = WorldBuilder.Sprite("StormVeil", SpriteCatalog.Glow, new Vector3(0f, 0.2f, 6.8f), 1f, 16, transform);
            veilGo.transform.localScale = new Vector3(24f, 30f * PortraitLock.TallFactor(), 1f);
            _veil = veilGo.GetComponent<SpriteRenderer>();
            _veil.color = new Color(0.07f, 0.09f, 0.13f, 0f);
            _veil.enabled = false;

            var flashGo = WorldBuilder.Sprite("Flash", SpriteCatalog.Glow, new Vector3(0f, 1.2f, 6.7f), 1f, 17, transform);
            flashGo.transform.localScale = new Vector3(22f, 28f, 1f);
            _flash = flashGo.GetComponent<SpriteRenderer>();
            _flash.color = new Color(0.82f, 0.88f, 1f, 0f);
            _flash.enabled = false;

            FullFrame(out float left, out float right, out float bottom, out float top);
            BuildSheet(left, right, bottom, top);
            yield return null;

            _bolt = new Transform[Segs];
            _boltSr = new SpriteRenderer[Segs];
            _segMul = new float[Segs];
            _px = new float[PathCap];
            _py = new float[PathCap];
            for (int i = 0; i < Segs; i++)
            {
                var go = WorldBuilder.Sprite("Bolt" + i, SpriteCatalog.RainStreak, new Vector3(0f, -20f, 0.22f), 1f, 18, transform);
                _bolt[i] = go.transform;
                _boltSr[i] = go.GetComponent<SpriteRenderer>();
                _boltSr[i].enabled = false;
                _boltSr[i].color = new Color(0.94f, 0.97f, 1f, 0f);
                if ((i & 7) == 7) yield return null;
            }
            BuildCue();
        }

        static int DropCount() => Application.isMobilePlatform ? DropsPhone : DropsDesk;

        // Streak quads share the sprite shader. unity_SpriteProps defaults to 0 on a mesh and flattens it.
        void BuildSheet(float left, float right, float bottom, float top)
        {
            int n = DropCount();
            bool phone = Application.isMobilePlatform;
            float wide = phone ? 0.98f : 0.86f;
            float lenMul = phone ? 1.18f : 1.06f;
            _x = new float[n];
            _y = new float[n];
            _spd = new float[n];
            _phase = new float[n];
            _amp = new float[n];
            _cos = new float[n];
            _sin = new float[n];
            _hw = new float[n];
            _hh = new float[n];
            _verts = new Vector3[n * 4];
            _cols = new Color32[n * 4];
            var tris = new int[n * 6];
            var uvs = new Vector2[n * 4];
            var norms = new Vector3[n * 4];
            var rng = new System.Random(29);
            float halfW = StreakW * wide * 0.5f;
            for (int i = 0; i < n; i++)
            {
                _x[i] = Mathf.Lerp(left - Edge, right + Edge, (float)rng.NextDouble());
                _y[i] = Mathf.Lerp(bottom - Edge, top + Edge, (float)rng.NextDouble());
                float len = Mathf.Lerp(0.70f, 1.45f, (float)rng.NextDouble()) * lenMul;
                _spd[i] = Mathf.Lerp(8.5f, 26.5f, (float)rng.NextDouble());
                _phase[i] = (float)rng.NextDouble() * 2.8f;
                _amp[i] = Mathf.Lerp(0.34f, 0.76f, (i % 11) / 10f);
                float ang = (9f + (float)rng.NextDouble() * 6f) * Mathf.Deg2Rad;
                _cos[i] = Mathf.Cos(ang);
                _sin[i] = Mathf.Sin(ang);
                _hw[i] = halfW;
                _hh[i] = len * 0.5f;
                int v = i * 4;
                int t = i * 6;
                tris[t] = v;
                tris[t + 1] = v + 2;
                tris[t + 2] = v + 1;
                tris[t + 3] = v + 2;
                tris[t + 4] = v + 3;
                tris[t + 5] = v + 1;
                uvs[v] = new Vector2(0f, 0f);
                uvs[v + 1] = new Vector2(1f, 0f);
                uvs[v + 2] = new Vector2(0f, 1f);
                uvs[v + 3] = new Vector2(1f, 1f);
                norms[v] = norms[v + 1] = norms[v + 2] = norms[v + 3] = new Vector3(0f, 0f, -1f);
                PlaceDrop(i, _x[i], _y[i], true);
            }

            var donor = WorldBuilder.Sprite("RainDonor", SpriteCatalog.RainStreak, new Vector3(0f, -40f, 0.4f), 1f, 15, transform);
            var donorSr = donor.GetComponent<SpriteRenderer>();
            if (donorSr.sharedMaterial != null)
                _sheetMat = new Material(donorSr.sharedMaterial);
            Destroy(donor);
            if (_sheetMat == null) return;
            _sheetMat.mainTexture = SpriteCatalog.RainStreak.texture;
            _sheetMat.SetVector(SpritePropsId, new Vector4(1f, 1f, -1f, 0f));
            _sheetMat.SetVector(SpriteColorId, new Vector4(1f, 1f, 1f, 1f));

            _mesh = new Mesh { name = "RainSheet" };
            _mesh.MarkDynamic();
            _mesh.vertices = _verts;
            _mesh.uv = uvs;
            _mesh.normals = norms;
            _mesh.colors32 = _cols;
            _mesh.triangles = tris;
            _sheetBounds = new Bounds(new Vector3(0f, WorldBuilder.CamRestY, 0f), new Vector3(80f, 80f, 2f));
            _mesh.bounds = _sheetBounds;

            var go = new GameObject("RainSheet");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, 0.4f);
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = _mesh;
            _sheet = go.AddComponent<MeshRenderer>();
            _sheet.sharedMaterial = _sheetMat;
            _sheet.sortingOrder = 15;
            _sheet.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _sheet.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            _sheet.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            _sheet.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            _sheetBlock = new MaterialPropertyBlock();
            _sheetBlock.SetVector(SpritePropsId, new Vector4(1f, 1f, -1f, 0f));
            _sheetBlock.SetVector(SpriteColorId, new Vector4(1f, 1f, 1f, 1f));
            _sheet.SetPropertyBlock(_sheetBlock);
            _sheet.enabled = false;
        }

        void PlaceDrop(int i, float cx, float cy, bool live)
        {
            int v = i * 4;
            if (!live)
            {
                _verts[v].x = _verts[v + 1].x = _verts[v + 2].x = _verts[v + 3].x = cx;
                _verts[v].y = _verts[v + 1].y = _verts[v + 2].y = _verts[v + 3].y = cy;
                _verts[v].z = _verts[v + 1].z = _verts[v + 2].z = _verts[v + 3].z = 0f;
                return;
            }
            float hw = _hw[i];
            float hh = _hh[i];
            float c = _cos[i];
            float s = _sin[i];
            Put(v, cx, cy, -hw, -hh, c, s);
            Put(v + 1, cx, cy, hw, -hh, c, s);
            Put(v + 2, cx, cy, -hw, hh, c, s);
            Put(v + 3, cx, cy, hw, hh, c, s);
        }

        void Put(int v, float cx, float cy, float x, float y, float c, float s)
        {
            _verts[v].x = cx + x * c - y * s;
            _verts[v].y = cy + x * s + y * c;
            _verts[v].z = 0f;
        }

        void WriteColors(float wet)
        {
            const byte r = 199, g = 219, b = 240;
            int n = _x.Length;
            for (int i = 0; i < n; i++)
            {
                int a = Mathf.RoundToInt(wet * _amp[i] * 255f);
                if (a < 0) a = 0;
                else if (a > 255) a = 255;
                var col = new Color32(r, g, b, (byte)a);
                int v = i * 4;
                _cols[v] = _cols[v + 1] = _cols[v + 2] = _cols[v + 3] = col;
            }
        }

        void CommitSheet()
        {
            if (_mesh == null) return;
            _mesh.SetVertices(_verts);
            _mesh.bounds = _sheetBounds;
        }

        // Bar that shrinks across StormLen. Built once. No bolt glyph: three
        // RainStreaks here sat under the notch for the whole storm (a stuck scar).
        // Strikes are the Bolt segments, and EndBolt clears those.
        void BuildCue()
        {
            var root = new GameObject("StormCue");
            root.transform.SetParent(transform, false);
            _cue = root.transform;
            _cue.localScale = new Vector3(1.4f, 1.4f, 1f);
            var trackGo = WorldBuilder.Sprite("CueTrack", SpriteCatalog.Glow, Vector3.zero, 1f, 19, _cue);
            trackGo.transform.localPosition = new Vector3(0f, -0.62f, 0f);
            trackGo.transform.localScale = new Vector3(BarW, 0.11f, 1f);
            _track = trackGo.GetComponent<SpriteRenderer>();
            _track.color = new Color(0.10f, 0.12f, 0.16f, 0f);
            _track.enabled = false;
            var fillGo = WorldBuilder.Sprite("CueFill", SpriteCatalog.Glow, Vector3.zero, 1f, 20, _cue);
            _fillT = fillGo.transform;
            _fillT.localPosition = new Vector3(0f, -0.62f, 0f);
            _fillT.localScale = new Vector3(BarW, 0.11f, 1f);
            _fill = fillGo.GetComponent<SpriteRenderer>();
            _fill.color = new Color(0.82f, 0.90f, 1f, 0f);
            _fill.enabled = false;
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
            if (GamePause.Paused) return;
            float dt = PlayClock.Delta;
            if (dt > 0.0001f)
                _step = _step <= 0f ? dt : _step + (dt - _step) * 0.2f;
            _play += dt;
            float want = WantStorm(_play) ? 1f : 0f;
            _wet = Mathf.MoveTowards(_wet, want, dt / Fade);
            Wet = _wet;

            if (_rumbleIn >= 0f)
            {
                _rumbleIn -= dt;
                if (_rumbleIn <= 0f)
                {
                    _rumbleIn = -1f;
                    Sfx.ThunderRoll(_rumblePow);
                }
            }
            if (_wet > 0.45f)
            {
                _nextBoom -= dt;
                if (_nextBoom <= 0f)
                {
                    // Visible strikes, not a chatter loop. A low frame stretches the gap.
                    bool big = Random.value < 0.28f;
                    float power = big ? Random.Range(0.90f, 1f) : Random.Range(0.62f, 0.86f);
                    float gap = big ? Random.Range(8.5f, 14f) : Random.Range(5.5f, 9.5f);
                    if (LeanFrame()) gap += 3.5f;
                    _nextBoom = gap;
                    Boom(power, big);
                }
            }

            // Steady rain keeps one veil alpha. A strike opens it so the sky pulse reads.
            float veilA = _wet > 0.001f ? 0.62f * _wet : 0f;
            float sky = SkyFlash;
            if (sky > 0f) veilA *= 1f - 0.7f * sky;
            if (_veil != null && veilA != _veilA)
            {
                _veilA = veilA;
                _veil.color = new Color(0.06f, 0.08f, 0.12f, veilA);
                bool veilOn = veilA > 0.001f;
                if (_veil.enabled != veilOn) _veil.enabled = veilOn;
            }

            // Clear stretches are invisible. A snap to dry (resume used to do this
            // in one spiked step) must still hide every streak, not leave the last paint.
            bool dry = _wet <= 0.001f && want <= 0f;
            if (_rainSync > 0)
            {
                if (dry)
                {
                    ClearDrops();
                    _rainSync = 0;
                }
                else if (_rainSync == 1)
                {
                    // One hidden frame so a frozen resume batch is dropped, then restart.
                    HoldDrops();
                    _rainSync = 2;
                }
                else
                {
                    RestartDrops();
                    _rainSync = 0;
                }
            }
            if (dry)
            {
                if (!_wasDry || DropsEnabled()) ClearDrops();
                _wasDry = true;
            }
            else if (_wasDry && _phase != null)
            {
                // Holds only stagger a wrap, not the moment rain becomes visible.
                for (int i = 0; i < _phase.Length; i++)
                    _phase[i] = 0f;
                _colQ = -1;
                _wasDry = false;
            }
            if (_x != null && _sheet != null && !dry)
            {
                // Full screen, including the letterbox past the home indicator.
                // Safe area is for HUD only — drops exit past every edge before recycle.
                Frame(out float left, out float right, out float bottom, out float top);
                _floor = bottom - Edge;
                float wet = _wet;
                if (dt > 0.0001f)
                {
                    for (int i = 0; i < _x.Length; i++)
                    {
                        // Per-drop hold so the curtain doesn't reset as one sheet.
                        if (_phase[i] > 0f)
                        {
                            _phase[i] -= dt;
                            PlaceDrop(i, _x[i], _y[i], false);
                            continue;
                        }
                        float fall = _spd[i] * dt * Mathf.Lerp(0.18f, 1f, wet);
                        float nx = _x[i] - (1.15f + 0.9f * ((_spd[i] - 8.5f) / 18f)) * dt * wet;
                        float ny = _y[i] - fall;
                        if (ny < _floor || nx < left - Edge || nx > right + Edge)
                        {
                            ny = Random.Range(top + 0.25f, top + Edge + 1.8f);
                            nx = Random.Range(left - 0.35f, right + Edge);
                            _phase[i] = Random.Range(0.05f, 1.35f);
                            _spd[i] = Random.Range(8.5f, 26.5f);
                        }
                        _x[i] = nx;
                        _y[i] = ny;
                        PlaceDrop(i, nx, ny, true);
                    }
                    CommitSheet();
                }
                int q = (int)(wet * 48f);
                if (q != _colQ)
                {
                    _colQ = q;
                    WriteColors(wet);
                    _mesh.SetColors(_cols);
                }
                // Hold frame (_rainSync == 2) stays hidden so a frozen resume batch is dropped.
                if (_rainSync != 2 && !_sheet.enabled) _sheet.enabled = true;
            }

            PaintFlash();
            if (_boltOn)
            {
                float age = PlayClock.Now - _boltAt;
                if (age >= _boltLife) EndBolt();
                else FadeBolt(age);
            }
            PaintCue();
        }

        bool DropsEnabled() => _sheet != null && _sheet.enabled;

        // Rain-off. Renderer off, so a stale sheet cannot keep needles up.
        void ClearDrops()
        {
            _wasDry = true;
            if (_sheet != null) _sheet.enabled = false;
        }

        void HoldDrops()
        {
            if (_sheet != null) _sheet.enabled = false;
        }

        void RestartDrops()
        {
            _wasDry = false;
            _colQ = -1;
            if (_sheet != null) _sheet.enabled = true;
        }

        void Boom(float power, bool big)
        {
            _skyAt = PlayClock.Now - 0.012f;
            _skyBig = big;
            _skyPow = big ? 1f : Mathf.Lerp(0.62f, 0.84f, Mathf.Clamp01(power));
            // Close bolts crack with the light. The roll arrives later, sooner when the hit is bigger.
            bool close = big || power >= 0.84f;
            if (close) Sfx.ThunderCrack(power);
            _rumblePow = power;
            _rumbleIn = close
                ? Mathf.Lerp(0.42f, 0.14f, Mathf.Clamp01(power))
                : Mathf.Lerp(1.15f, 0.50f, Mathf.Clamp01(power));
            CamShake.Bolt(big ? Mathf.Max(power, 0.92f) : power);
            if (big) Haptics.Play(Haptics.Tier.Strong);
            else if (power >= 0.78f) Haptics.Play(Haptics.Tier.Medium);
            SpawnBolt(power, big);
        }

        static float FlashShape(float ft, bool big)
        {
            float strike = Mathf.Exp(-(ft - 0.012f) * (ft - 0.012f) / 0.00020f);
            float echo = (big ? 0.94f : 0.68f) * Mathf.Exp(-(ft - 0.058f) * (ft - 0.058f) / 0.00032f);
            float s = strike + echo;
            return s > 1f ? 1f : s;
        }

        // Screen flash while the strike is live. One clear after the tail, then no further writes.
        // SkyFlash is the same curve, so the painted sky does not wait on script order.
        void PaintFlash()
        {
            if (_flash == null) return;
            float ft = PlayClock.Now - _skyAt;
            if (ft < 0f || ft > 0.45f)
            {
                if (_flashLit) DimFlash();
                return;
            }
            float shape = FlashShape(ft, _skyBig);
            float a = shape * _skyPow;
            if (a < 0.012f && ft > 0.14f)
            {
                if (_flashLit) DimFlash();
                return;
            }
            var warm = new Color(1f, 0.97f, 0.90f, a);
            var cool = new Color(0.75f, 0.86f, 1f, a);
            _flash.color = Color.Lerp(warm, cool, Mathf.Clamp01(ft * 8f));
            float sc = (28f + 7f * shape) * (_skyBig ? 1.08f : 1f);
            float tall = PortraitLock.TallFactor();
            _flash.transform.localScale = new Vector3(sc, sc * tall, 1f);
            if (!_flash.enabled) _flash.enabled = true;
            _flashLit = true;
        }

        void DimFlash()
        {
            _flash.color = new Color(0.78f, 0.86f, 1f, 0f);
            float rest = 22f;
            float tall = PortraitLock.TallFactor();
            _flash.transform.localScale = new Vector3(rest, rest * tall, 1f);
            _flash.enabled = false;
            _flashLit = false;
        }

        bool LeanFrame() => _step > LeanDt;

        void SpawnBolt(float power, bool big)
        {
            if (_bolt == null) return;
            EndBolt();
            _boltOn = true;
            _boltAt = PlayClock.Now;
            _boltLife = big ? 0.46f : 0.28f;
            _boltPow = big ? 1f : Mathf.Clamp01(power);
            Frame(out float left, out float right, out float bottom, out float top);
            float x0 = Random.Range(left * 0.62f, right * 0.62f);
            float y0 = top - 0.05f;
            float x1 = x0 + Random.Range(-1.8f, 1.8f);
            float y1 = Mathf.Lerp(bottom + 2.6f, bottom + 0.55f, Mathf.Clamp01(power));
            float thick = big ? 0.36f : 0.22f;
            Lay(x0, y0, x1, y1, big ? 8 : 6, big ? 0.9f : 0.55f, thick, true);
            int forks = big ? (LeanFrame() ? 2 : 4) : (LeanFrame() ? 1 : 2);
            for (int f = 0; f < forks; f++)
            {
                if (_pn < 3) break;
                int at = 1 + (f * 2) % (_pn - 1);
                float dir = ((f & 1) == 0) ? -1f : 1f;
                if (Random.value < 0.15f) dir = -dir;
                float len = Random.Range(0.9f, big ? 2.6f : 1.7f);
                Lay(_px[at], _py[at], _px[at] + dir * len, _py[at] - Random.Range(0.35f, 1.35f), big ? 4 : 3, 0.35f, thick * 0.48f, false);
            }
            if (big && !LeanFrame())
            {
                float side = Random.value < 0.5f ? -1f : 1f;
                float ox = x0 + side * Random.Range(1.1f, 2.0f);
                Lay(ox, y0 - 0.35f, ox + Random.Range(-0.8f, 0.8f), y1 + 1.5f, 6, 0.6f, thick * 0.62f, false);
            }
            FadeBolt(0f);
        }

        void Lay(float x0, float y0, float x1, float y1, int steps, float jag, float thick, bool record)
        {
            if (steps < 2) steps = 2;
            float px = x0;
            float py = y0;
            if (record)
            {
                _pn = 0;
                PushPath(px, py);
            }
            float dx = x1 - x0;
            float dy = y1 - y0;
            float inv = 1f / Mathf.Max(0.001f, Mathf.Sqrt(dx * dx + dy * dy));
            float nx = -dy * inv;
            float ny = dx * inv;
            for (int s = 1; s <= steps; s++)
            {
                float u = s / (float)steps;
                float x = Mathf.Lerp(x0, x1, u);
                float y = Mathf.Lerp(y0, y1, u);
                if (s != steps && jag > 0f)
                {
                    float kick = Random.Range(-jag, jag);
                    x += nx * kick;
                    y += ny * kick;
                }
                PlaceSeg(px, py, x, y, thick * Mathf.Lerp(1.2f, 0.5f, u));
                px = x;
                py = y;
                if (record) PushPath(px, py);
            }
        }

        void PushPath(float x, float y)
        {
            if (_px == null || _pn >= PathCap) return;
            _px[_pn] = x;
            _py[_pn] = y;
            _pn++;
        }

        void PlaceSeg(float x0, float y0, float x1, float y1, float thick)
        {
            if (_bolt == null || _boltN >= _bolt.Length) return;
            if (LeanFrame() && _boltN >= 14) return;
            float dx = x1 - x0;
            float dy = y1 - y0;
            float len = Mathf.Sqrt(dx * dx + dy * dy);
            if (len < 0.05f) return;
            int i = _boltN;
            var t = _bolt[i];
            var sr = _boltSr[i];
            if (t == null || sr == null) return;
            _boltN = i + 1;
            _boltHi = _boltN;
            t.position = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, 0.22f);
            t.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dy, dx) * Mathf.Rad2Deg - 90f);
            // RainStreak is 0.125 wide and 1 tall at scale 1.
            t.localScale = new Vector3(thick * 8f, len * 1.08f, 1f);
            sr.enabled = true;
            _segMul[i] = thick >= 0.2f ? 1f : 0.8f;
        }

        void FadeBolt(float age)
        {
            if (_boltSr == null) return;
            float tail = _boltLife > 0.16f ? _boltLife - 0.10f : 0.16f;
            float hold = age < 0.10f ? 1f : 1f - (age - 0.10f) / tail;
            if (hold < 0f) hold = 0f;
            // Same double peak as the screen flash, with a floor so the fork stays readable in the dip.
            float shape = FlashShape(age + 0.012f, _skyBig);
            float a = (age < 0.10f ? 0.34f + 0.66f * shape : hold) * _boltPow;
            int q = (int)(a * 32f);
            if (q == _boltQ) return;
            _boltQ = q;
            var core = new Color(0.95f, 0.97f, 1f, a);
            var limb = new Color(0.70f, 0.84f, 1f, a * 0.86f);
            int n = _boltHi;
            for (int i = 0; i < n; i++)
            {
                var sr = _boltSr[i];
                if (sr == null || !sr.enabled) continue;
                sr.color = _segMul[i] > 0.9f ? core : limb;
            }
        }

        void EndBolt()
        {
            _boltOn = false;
            _boltQ = -1;
            if (_boltSr == null) return;
            for (int i = 0; i < _boltHi; i++)
            {
                if (_boltSr[i] != null) _boltSr[i].enabled = false;
            }
            _boltHi = 0;
            _boltN = 0;
        }

        static float Remain(float play)
        {
            if (play < FirstWait) return 0f;
            float u = play - FirstWait;
            float cycle = StormLen + ClearLen;
            float into = u % cycle;
            if (into >= StormLen) return 0f;
            return StormLen - into;
        }

        void PaintCue()
        {
            if (_cue == null) return;
            float left = Remain(_play);
            bool show = left > 0.05f && _wet > 0.12f;
            if (!show)
            {
                if (_cueOn) HideCue();
                return;
            }
            if (!_cueOn) ShowCue();
            PlaceCue();
            float frac = left / StormLen;
            float fade = Mathf.Clamp01((_wet - 0.12f) / 0.4f);
            int q = (int)(frac * 48f);
            int fq = (int)(fade * 16f);
            if (q == _barQ && fq == _fadeQ) return;
            _barQ = q;
            _fadeQ = fq;
            if (_track != null) _track.color = new Color(0.10f, 0.12f, 0.16f, fade * 0.42f);
            if (_fill != null) _fill.color = new Color(0.82f, 0.90f, 1f, fade * 0.74f);
            float w = BarW * Mathf.Clamp01(frac);
            if (w < 0.04f) w = 0.04f;
            _fillT.localScale = new Vector3(w, 0.11f, 1f);
            _fillT.localPosition = new Vector3(-BarW * 0.5f + w * 0.5f, -0.62f, 0f);
        }

        void PlaceCue()
        {
            Frame(out _, out _, out _, out _);
            if (_cueFrame == _frameW && _cueFrameH == _frameH && _cueTall == _frameTall) return;
            float y = _frT - WorldBuilder.SafeTopWorld() - 0.95f;
            float x = (_frL + _frR) * 0.5f;
            _cue.position = new Vector3(x, y, 0.12f);
            _cueFrame = _frameW;
            _cueFrameH = _frameH;
            _cueTall = _frameTall;
        }

        void ShowCue()
        {
            _cueOn = true;
            if (_track != null) _track.enabled = true;
            if (_fill != null) _fill.enabled = true;
        }

        void HideCue()
        {
            _cueOn = false;
            _barQ = -1;
            _fadeQ = -1;
            if (_track != null) _track.enabled = false;
            if (_fill != null) _fill.enabled = false;
        }
    }

    // Foreground seconds. Time.unscaledTime and the resume frame's unscaledDeltaTime
    // include the whole iOS/Android suspension, so ambience countdowns must not read them.
    public static class PlayClock
    {
        const float Spike = 0.5f;

        static float _now;
        static float _delta;
        static int _frame = -1;
        static int _drop;

        public static float Now
        {
            get
            {
                Tick();
                return _now;
            }
        }

        public static float Delta
        {
            get
            {
                Tick();
                return _delta;
            }
        }

        // Pause and focus both bookend a suspend. The next frames' unscaled step is that gap.
        public static void DropResumeFrame()
        {
            _drop = 2;
        }

        public static System.Collections.IEnumerator Wait(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Delta;
                yield return null;
            }
        }

        static void Tick()
        {
            int frame = Time.frameCount;
            if (frame == _frame) return;
            _frame = frame;
            float dt = Time.unscaledDeltaTime;
            if (GamePause.Paused)
                dt = 0f;
            else if (_drop > 0)
            {
                _drop--;
                dt = 0f;
            }
            else if (dt > Spike)
                dt = 0f;
            _delta = dt;
            _now += dt;
        }
    }
}
