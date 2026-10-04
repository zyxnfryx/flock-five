using System.Collections;
using UnityEngine;

namespace FlockFive
{
    /// <summary>
    /// Idle bird. Female kit bow / male crown are sibling SpriteRenderers so draw order is
    /// kit (behind) → body/flaps → face — bow tucked behind the head silhouette, never baked into PNGs.
    /// </summary>
    public sealed class BirdIdle : MonoBehaviour
    {
        public Vector3 RestLocal;
        public Vector3 RestScale = new Vector3(0.42f, 0.42f, 1f);
        public float Lift;
        public bool Frozen;
        public bool Flapping;
        public float FlapMul = 1f;
        public bool Sleeping;
        public bool Shrouded;
        public bool FaceLeft;
        public BirdColor Color;
        public BirdSex Sex;
        SpriteRenderer _sr;
        SpriteRenderer _face;
        SpriteRenderer _kit;
        SpriteRenderer[] _glow;
        SpriteRenderer[] _kitGlow;
        bool _glowHidden = true;
        bool _kitGlowHidden = true;
        float _glowA;
        SpriteRenderer[] _aura;
        SpriteRenderer[] _twink;
        float _selectA;
        float _cheerA;
        static Material _silhouette;
        float _phase;
        float _liftShown;
        float _nextWing;
        float _nextRuffle;
        float _ruffle;
        float _flutterUntil;
        float _blinkUntil;
        float _nextBlink;
        float _cheerUntil;
        float _alertFrom;
        float _alertUntil;
        Transform[] _zzz;
        SpriteRenderer[] _zzzSr;
        Transform[] _zzzEdge;
        SpriteRenderer[] _zzzEdgeSr;
        Transform[] _zzzGlow;
        SpriteRenderer[] _zzzGlowSr;
        SpriteRenderer _bang;
        static Sprite _bangSpr;
        static readonly UnityEngine.Color ShroudTint = new UnityEngine.Color(0.04f, 0.03f, 0.05f, 1f);

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _phase = Random.Range(0f, 40f);
            _nextRuffle = Time.time + Random.Range(0.4f, 3.2f);
            _nextWing = Time.time + Random.Range(0f, 0.08f);
            _nextBlink = Time.time + Random.Range(0.6f, 2.4f);
        }

        public void Bind(BirdColor color, Vector3 restLocal) =>
            Bind(new Bird(color, BirdSex.Female), restLocal);

        public void Bind(Bird bird, Vector3 restLocal)
        {
            Color = bird.Color;
            Sex = bird.Sex;
            RestLocal = restLocal;
            Frozen = false;
            Flapping = false;
            _flutterUntil = 0f;
            _cheerUntil = 0f;
            _glowA = 0f;
            _liftShown = 0f; // re-bound birds (e.g. after Restart) start seated, no pop
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _sr.flipX = FaceLeft;
            EnsureFace();
            // Neutral = plain bird (finale). Female/Male get kit bow/crown.
            if (Sex == BirdSex.Neutral)
            {
                if (_kit != null) _kit.enabled = false;
            }
            else
            {
                EnsureKit();
            }
        }

        public void Flutter(float seconds)
        {
            Flapping = true;
            _flutterUntil = Time.time + Mathf.Max(0.12f, seconds);
            _nextWing = 0f;
        }

        // Short celebratory outline. Fades on its own so a score hop never sticks.
        public void Cheer(float seconds)
        {
            _cheerUntil = Time.time + Mathf.Max(0.05f, seconds);
        }

        // One seated bounce. Restores whatever Lift the perch already had.
        // Matching feeder arrived. Eyes open, one startled hop. The "!" is local.
        public void WakeAlert()
        {
            Sleeping = false;
            _blinkUntil = 0f;
            _nextBlink = Time.time + 0.9f;
            _alertFrom = Time.time;
            _alertUntil = Time.time + 0.46f;
            EnsureBang();
            if (_bang != null) _bang.enabled = true;
            HideZzz();
        }

        public void HopCheer(float delay, float hop)
        {
            if (!isActiveAndEnabled || Frozen || Shrouded || Sleeping) return;
            StartCoroutine(HopCheerCo(delay, hop));
        }

        IEnumerator HopCheerCo(float delay, float hop)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (Frozen || Shrouded || Sleeping) yield break;
            float keep = Lift;
            Cheer(0.32f);
            Lift = keep + hop;
            float t = 0f;
            while (t < 0.16f)
            {
                t += Time.deltaTime;
                if (Frozen || Shrouded)
                {
                    Lift = 0f;
                    yield break;
                }
                yield return null;
            }
            Lift = keep;
        }

        public void SetFade(float a)
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            FadeSprite(_sr, a);
            FadeSprite(_face, a);
            FadeSprite(_kit, a);
        }

        static void FadeSprite(SpriteRenderer sr, float a)
        {
            if (sr == null) return;
            var c = sr.color;
            c.a = a;
            sr.color = c;
        }

        void EnsureFace()
        {
            if (_face != null)
            {
                int faceHeld = _face.sortingOrder;
                _face.sprite = BirdMood.Face(Color);
                FlockSort.Apply(_face, faceHeld > 0 ? faceHeld : FlockSort.Perch + 1);
                return;
            }
            var mood = BirdMood.Of(Color);
            var go = WorldBuilder.Sprite("Mood", BirdMood.Face(Color), transform.position, mood.FaceScale, FlockSort.Perch + 1, transform);
            go.transform.localRotation = Quaternion.identity;
            _face = go.GetComponent<SpriteRenderer>();
            FlockSort.Apply(_face, FlockSort.Perch + 1);
        }

        // Shared pose rule. Off the perch, wing frames. On the perch, only a real airborne flap.
        public static bool UseFlyingPose(bool onPerch, bool airborneFlap)
        {
            if (!onPerch) return true;
            return airborneFlap;
        }

        // Feet on this bird's seat. Sleep droops into the bark. A lift, or a body
        // that flight has carried off RestLocal, is not perched.
        bool PerchHeld()
        {
            if (Sleeping) return true;
            if (Lift > 0.05f || _liftShown > 0.05f) return false;
            var d = transform.localPosition - RestLocal;
            return d.sqrMagnitude < 0.0256f;
        }

        void LateUpdate()
        {
            if (!isActiveAndEnabled) return;
            if (_flutterUntil > 0f && Time.time >= _flutterUntil)
            {
                _flutterUntil = 0f;
                if (Lift < 0.05f) Flapping = false;
            }
            if (_ruffle > 0f) _ruffle -= Time.deltaTime;
            bool show = _sr != null && _sr.enabled;
            // Shrouded birds stay a still silhouette on the perch. Off the perch,
            // including a frozen takeoff, the flying cycle is mandatory.
            bool onPerch = PerchHeld();
            bool fly = !Sleeping && show && (Flapping || Lift > 0.05f || _ruffle > 0f || !onPerch)
                && (!Shrouded || (Frozen && Flapping) || !onPerch);
            var mood = BirdMood.Of(Color);
            if (show)
            {
                if (_sr.flipX != FaceLeft) _sr.flipX = FaceLeft;
                // On the bark, a seated flutter or ruffle keeps the rest frame.
                // Anywhere else, wing frames. A still rest pose is only a perched bird.
                bool airborne = Frozen || _liftShown > 0.05f || (Flapping && _flutterUntil <= 0f);
                bool wings = UseFlyingPose(onPerch, fly && airborne);
                // BirdFrame steps poses at t*16, so t = Time.time * FlapRate gives
                // 16*FlapRate poses/sec (rest,_1,_2,_1 = 4 poses per wingbeat); with the _3/_4
                // in-betweens BirdFrame doubles that to 8 poses per beat at the same beat rate.
                // 1.25 = 20 poses/sec = 5 wingbeats/sec, frame-rate independent.
                float mul = FlapMul < 0.05f ? 1f : FlapMul;
                float flap = wings ? FlapRate * mul : 0.06f;
                var frame = SpriteCatalog.BirdFrame(Color, (Time.time + _phase) * flap, wings, Sex);
                // Read before the assign. A sprite swap must not stick the body at 0
                // (behind wood and leaves). Frozen flight keeps Fly; it does not drop to Perch.
                int heldOrder = _sr.sortingOrder;
                if (_sr.sprite != frame) _sr.sprite = frame;
                if (_sr.flipX != FaceLeft) _sr.flipX = FaceLeft;
                if (!Frozen)
                {
                    // Shroud darkens the body. The "!" hop does not tint it.
                    UnityEngine.Color tint = Shrouded ? ShroudTint : UnityEngine.Color.white;
                    if (_sr.color != tint) _sr.color = tint;
                    int order = Shrouded ? FlockSort.Shroud : (Lift > 0.05f ? FlockSort.Lift : FlockSort.Perch);
                    FlockSort.Apply(_sr, order);
                }
                else
                    FlockSort.Apply(_sr, heldOrder);
            }
            // Draw order (back→front): kit bow → body/flaps → face
            bool kitOn = show && !Shrouded && Sex != BirdSex.Neutral;
            if (kitOn) EnsureKit();
            PlaceKit(mood, kitOn);
            PlaceFace(mood, show && !Shrouded);
            // White silhouette only used to mean "selected". Selection is the colored
            // aura. A cheer (feeder clear, hawk wave, finale) is twinkles, including
            // while Frozen, and never that halo. Flight sets Lift with Frozen and stays dark.
            bool selected = show && !Shrouded && !Sleeping && !Frozen && Lift >= 1f;
            bool cheering = show && !Shrouded && Time.time < _cheerUntil;
            PlaceSelect(selected);
            PlaceCheerFx(cheering);
            if (fly && !Frozen) BeatWings();
            else if (show && !Sleeping && !Shrouded && !Frozen) MaybeRuffle();

            if (Frozen) return;
            if (Shrouded)
            {
                HideZzz();
                HideBang();
                transform.localPosition = RestLocal;
                transform.localRotation = Quaternion.identity;
                transform.localScale = RestScale;
                return;
            }
            if (Time.time < _alertUntil)
            {
                float u = Mathf.Clamp01((Time.time - _alertFrom) / 0.46f);
                float pulse = Mathf.Sin(u * Mathf.PI * 2f);
                pulse = pulse < 0f ? 0f : pulse;
                float hop = Mathf.Sin(Mathf.Clamp01(u / 0.42f) * Mathf.PI) * 0.20f;
                transform.localPosition = new Vector3(RestLocal.x, RestLocal.y + hop, RestLocal.z);
                float lean = (FaceLeft ? 12f : -12f) * (1f - u);
                transform.localRotation = Quaternion.Euler(0f, 0f, lean);
                float sc = mood.Scale * (1f + 0.05f * pulse);
                transform.localScale = new Vector3(RestScale.x * sc, RestScale.y * sc, 1f);
                PlaceBang(u);
                HideZzz();
                return;
            }
            HideBang();
            float wantLift = Sleeping ? -0.16f : Lift;
            _liftShown = Mathf.MoveTowards(_liftShown, wantLift, 4.2f * Time.deltaTime);
            float scale = mood.Scale;
            if (Sleeping)
            {
                float snore = Mathf.Sin(Time.time * (Color == BirdColor.Violet ? 1.15f : 1.45f) + _phase);
                float droop = Color == BirdColor.Violet ? 0.055f : 0.04f;
                transform.localPosition = new Vector3(RestLocal.x, RestLocal.y + snore * droop + _liftShown, RestLocal.z);
                float tuck = FaceLeft ? 16f : -16f;
                float z = tuck + snore * 3.2f + mood.Lean * 0.25f;
                transform.localRotation = Quaternion.Euler(0f, 0f, z);
                float breathe = 1f + snore * 0.045f;
                transform.localScale = new Vector3(RestScale.x * scale * breathe, RestScale.y * scale * (2f - breathe) * 0.98f, 1f);
                PlaceZzz(snore);
                return;
            }
            HideZzz();
            float look = Color == BirdColor.Teal ? Mathf.Sin(Time.time * 1.15f + _phase) * mood.Tilt : Mathf.Sin(Time.time * 5.1f + _phase) * mood.Tilt;
            float bob = Mathf.Sin(Time.time * mood.BobHz + _phase) * (fly ? mood.BobAmp * 3.2f : mood.BobAmp);
            float beat = Mathf.Sin(Time.time * 21f + _phase);
            transform.localPosition = new Vector3(RestLocal.x, RestLocal.y + bob + _liftShown, RestLocal.z);
            transform.localRotation = Quaternion.Euler(0f, 0f, look + mood.Lean);
            float squash = fly ? mood.Squash : mood.Squash * 0.7f;
            transform.localScale = new Vector3(
                RestScale.x * scale * (1f + beat * squash),
                RestScale.y * scale * (1f - beat * squash * 0.8f),
                1f);
        }

        const float FlapRate = 1.25f;

        // Selected run (BranchView.SetReady lifts tip birds to Lift 1.15) draws
        // PlaceSelect. The silhouette ring below is unused; celebrations do not call it.
        const int GlowRing = 16;           // copies per ring
        const float GlowSolidPx = 9.5f;    // inner ring radius, in body source px
        const float GlowSoftPx = 14.5f;     // outer (soft) ring radius, in body source px
        const float GlowSoftAlpha = 0.22f; // per-copy alpha of the soft ring

        static Material Silhouette()
        {
            if (_silhouette != null) return _silhouette;
            var sh = Resources.Load<Shader>("Shaders/SpriteSilhouette");
            if (sh == null) sh = Shader.Find("FlockFive/SpriteSilhouette");
            if (sh == null) sh = Shader.Find("GUI/Text Shader"); // alpha-only fallback
            if (sh == null) return null;
            _silhouette = new Material(sh) { name = "SelGlowSilhouette" };
            return _silhouette;
        }

        SpriteRenderer[] MakeGlowRing(Transform parent)
        {
            var mat = Silhouette();
            var arr = new SpriteRenderer[GlowRing * 2];
            for (int i = 0; i < arr.Length; i++)
            {
                var go = new GameObject("SelGlow");
                go.transform.SetParent(parent, false);
                var r = go.AddComponent<SpriteRenderer>();
                if (mat != null) r.sharedMaterial = mat;
                r.enabled = false;
                arr[i] = r;
            }
            return arr;
        }

        static void HideRing(SpriteRenderer[] ring, ref bool hidden)
        {
            if (hidden || ring == null) return;
            hidden = true;
            for (int i = 0; i < ring.Length; i++)
                if (ring[i] != null) ring[i].enabled = false;
        }

        static void RetireRing(ref SpriteRenderer[] ring, ref bool hidden)
        {
            HideRing(ring, ref hidden);
            if (ring == null) return;
            GlowPool.Give(ring);
            ring = null;
            hidden = true;
        }

        // unitsPerBodyPx: this renderer's local units per body source pixel.
        static void ShowRing(SpriteRenderer[] ring, SpriteRenderer src, float unitsPerBodyPx,
            float a, int layer, int order, ref bool hidden)
        {
            if (ring == null || src == null || src.sprite == null) { HideRing(ring, ref hidden); return; }
            hidden = false;
            float flip = src.flipX ? -1f : 1f;
            for (int i = 0; i < ring.Length; i++)
            {
                var r = ring[i];
                if (r == null) continue;
                bool soft = i >= GlowRing;
                float ang = (i % GlowRing) * (Mathf.PI * 2f / GlowRing) + (soft ? Mathf.PI / GlowRing : 0f);
                float rad = (soft ? GlowSoftPx : GlowSolidPx) * unitsPerBodyPx;
                r.sprite = src.sprite;
                r.flipX = false; // mirrored by scale instead, so any shader flips correctly
                r.transform.localPosition = new Vector3(Mathf.Cos(ang) * rad, Mathf.Sin(ang) * rad, 0f);
                r.transform.localRotation = Quaternion.identity;
                r.transform.localScale = new Vector3(flip, 1f, 1f);
                r.color = new Color(1f, 1f, 1f, soft ? a * GlowSoftAlpha : a);
                r.sortingLayerID = layer;
                r.sortingOrder = order;
                r.enabled = true;
            }
        }

        void HideZzz()
        {
            HideZGroup(_zzz);
            HideZGroup(_zzzEdge);
            HideZGroup(_zzzGlow);
        }

        static void HideZGroup(Transform[] group)
        {
            if (group == null) return;
            for (int i = 0; i < group.Length; i++)
                if (group[i] != null) group[i].gameObject.SetActive(false);
        }

        void EnsureZzz()
        {
            if (_zzz != null && _zzzEdge != null && _zzzGlow != null) return;
            var zee = SpriteCatalog.Zee;
            if (zee == null) return;
            if (_zzz == null)
            {
                _zzz = new Transform[2];
                _zzzSr = new SpriteRenderer[2];
                for (int i = 0; i < 2; i++)
                {
                    var go = WorldBuilder.Sprite("Z" + i, zee, transform.position, 0.24f, 14, transform);
                    go.SetActive(false);
                    _zzz[i] = go.transform;
                    _zzzSr[i] = go.GetComponent<SpriteRenderer>();
                }
            }
            if (_zzzEdge == null)
            {
                _zzzEdge = new Transform[2];
                _zzzEdgeSr = new SpriteRenderer[2];
                for (int i = 0; i < 2; i++)
                {
                    var go = WorldBuilder.Sprite("ZEdge" + i, zee, transform.position, 0.28f, 13, transform);
                    go.SetActive(false);
                    _zzzEdge[i] = go.transform;
                    _zzzEdgeSr[i] = go.GetComponent<SpriteRenderer>();
                }
            }
            if (_zzzGlow != null) return;
            var glow = SpriteCatalog.Glow;
            if (glow == null) return;
            _zzzGlow = new Transform[2];
            _zzzGlowSr = new SpriteRenderer[2];
            for (int i = 0; i < 2; i++)
            {
                var go = WorldBuilder.Sprite("ZGlow" + i, glow, transform.position, 0.50f, 12, transform);
                go.SetActive(false);
                _zzzGlow[i] = go.transform;
                _zzzGlowSr[i] = go.GetComponent<SpriteRenderer>();
            }
        }

        // Marks rise on one diagonal. Spacing stays at least one mark wide,
        // starts are staggered, and each mark grows as it climbs. Drift is
        // clamped to this bird's half of the perch gap.
        void PlaceZzz(float snore)
        {
            EnsureZzz();
            if (_zzz == null || _zzz[0] == null) return;
            int order = _sr != null ? _sr.sortingOrder + 3 : 15;
            float lane = SleepZLane(transform.localScale.x);
            int count = _zzz.Length;
            for (int i = 0; i < count; i++)
            {
                if (_zzz[i] == null) continue;
                _zzz[i].gameObject.SetActive(true);
                SleepZSlot(i, count, Time.time, _phase, FaceLeft, lane, snore, out var pos, out float pulse, out float a);
                _zzz[i].localPosition = pos;
                _zzz[i].localScale = Vector3.one * pulse;
                _zzz[i].localRotation = Quaternion.identity;
                if (_zzzSr[i] != null)
                {
                    _zzzSr[i].color = new Color(0.96f, 0.97f, 1f, a);
                    _zzzSr[i].sortingOrder = order;
                }
                if (_zzzEdge != null && i < _zzzEdge.Length && _zzzEdge[i] != null)
                {
                    _zzzEdge[i].gameObject.SetActive(true);
                    _zzzEdge[i].localPosition = pos;
                    _zzzEdge[i].localScale = Vector3.one * (pulse * 1.16f);
                    _zzzEdge[i].localRotation = Quaternion.identity;
                    if (_zzzEdgeSr[i] != null)
                    {
                        _zzzEdgeSr[i].color = new Color(0.05f, 0.04f, 0.07f, a * 0.92f);
                        _zzzEdgeSr[i].sortingOrder = order - 1;
                    }
                }
                if (_zzzGlow != null && i < _zzzGlow.Length && _zzzGlow[i] != null)
                {
                    _zzzGlow[i].gameObject.SetActive(true);
                    _zzzGlow[i].localPosition = pos;
                    _zzzGlow[i].localScale = Vector3.one * (pulse * 2.5f);
                    _zzzGlow[i].localRotation = Quaternion.identity;
                    if (_zzzGlowSr[i] != null)
                    {
                        _zzzGlowSr[i].color = new Color(0.85f, 0.90f, 1f, a * 0.28f);
                        _zzzGlowSr[i].sortingOrder = order - 2;
                    }
                }
            }
        }

        // Half the nearest seat step, in this bird's local units.
        static float SleepZLane(float scaleX)
        {
            float sc = scaleX < 0f ? -scaleX : scaleX;
            if (sc < 0.05f) sc = 0.42f;
            float step = (WorldBuilder.SeatXPx[1] - WorldBuilder.SeatXPx[0]) * WorldBuilder.WoodScaleX / 140f;
            float lane = step / sc * 0.5f;
            if (lane < 0.2f) lane = 0.2f;
            return lane;
        }

        // One mark on the shared diagonal. `along` differs by ZGap, so neighbors
        // stay at least one mark-width apart even when a mark wraps to the bottom.
        static void SleepZSlot(int i, int count, float time, float phase, bool faceLeft, float lane, float snore,
            out Vector3 pos, out float pulse, out float alpha)
        {
            const float ZGap = 0.42f;
            if (count < 1) count = 1;
            float span = count * ZGap + 0.22f;
            float along = Mathf.Repeat(time * 0.34f + phase * 0.15f + i * ZGap, span);
            float climb = along / span;
            float side = faceLeft ? -1f : 1f;
            float drift = Mathf.Sin(time * 0.9f + phase) * 0.04f;
            float x = side * (0.16f + along * 0.62f) + drift;
            float y = 0.58f + along * 1.20f;
            pulse = Mathf.Lerp(0.15f, 0.34f, climb) * (0.82f + 0.18f * i);
            float limit = lane - pulse;
            if (limit < 0.08f) limit = 0.08f;
            if (x > limit) x = limit;
            if (x < -limit) x = -limit;
            pos = new Vector3(x, y, 0f);
            float breathe = 0.88f + 0.12f * snore;
            alpha = (0.40f + 0.60f * (1f - climb)) * breathe;
        }

        void HideBang()
        {
            if (_bang != null) _bang.enabled = false;
        }

        void EnsureBang()
        {
            if (_bang != null) return;
            if (_bangSpr == null) _bangSpr = MakeBang();
            if (_bangSpr == null) return;
            var go = WorldBuilder.Sprite("Alert", _bangSpr, transform.position, 0.2f, 16, transform);
            _bang = go.GetComponent<SpriteRenderer>();
            _bang.enabled = false;
        }

        // Shared "!". Every alert marker uses this sprite (EnsureBang).
        // Fill is inset one texel so a 3px black ring stays inside the quad.
        // 3 texels is about one screen pixel at bird scale 0.42. PlaceBang's
        // red tint multiplies the texture, so the ring stays black.
        static Sprite MakeBang()
        {
            const int w = 24;
            const int h = 48;
            const float ring = 3f;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = "AlertBang"
            };
            var fill = new bool[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (x + 0.5f) / w - 0.5f;
                    float ny = (y + 0.5f) / h;
                    bool bar = ny > 0.30f && ny < 0.94f && Mathf.Abs(nx) < 0.16f;
                    bool dot = ny > 0.055f && ny < 0.18f
                        && nx * nx + (ny - 0.09f) * (ny - 0.09f) * 4f < 0.012f;
                    fill[y * w + x] = bar || dot;
                }
            }
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                int row = y * w;
                for (int x = 0; x < w; x++)
                {
                    int i = row + x;
                    if (fill[i])
                    {
                        px[i] = new Color32(255, 42, 36, 255);
                        continue;
                    }
                    float nearest = ring + 1f;
                    int y0 = y - 3;
                    int y1 = y + 3;
                    int x0 = x - 3;
                    int x1 = x + 3;
                    if (y0 < 0) y0 = 0;
                    if (x0 < 0) x0 = 0;
                    if (y1 >= h) y1 = h - 1;
                    if (x1 >= w) x1 = w - 1;
                    for (int yy = y0; yy <= y1; yy++)
                    {
                        int dy = yy - y;
                        int scan = yy * w;
                        for (int xx = x0; xx <= x1; xx++)
                        {
                            if (!fill[scan + xx]) continue;
                            int dx = xx - x;
                            float dist = Mathf.Sqrt(dx * dx + dy * dy);
                            if (dist < nearest) nearest = dist;
                        }
                    }
                    if (nearest > ring) continue;
                    px[i] = new Color32(0, 0, 0, 255);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 48f);
        }

        void PlaceBang(float u)
        {
            if (_bang == null) return;
            _bang.enabled = true;
            float pop = Mathf.Sin(Mathf.Clamp01(u / 0.28f) * Mathf.PI * 0.5f);
            float fade = u < 0.72f ? 1f : Mathf.Clamp01((1f - u) / 0.28f);
            int frame = SpriteCatalog.PoseIndex(_sr != null ? _sr.sprite : null, Color, Sex);
            float bird = transform.localScale.x;
            if (bird < 0f) bird = -bird;
            AlertAnchor(frame, FaceLeft, bird, out float ax, out float ay, out float fit);
            // Pivot is the bottom, so the pop grows up and the gap under it stays.
            float sc = fit * Mathf.Lerp(0.78f, 1f, pop);
            float y = ay + pop * 0.05f;
            var t = _bang.transform;
            t.localPosition = new Vector3(ax, y, 0f);
            t.localScale = new Vector3(sc, sc, 1f);
            t.localRotation = Quaternion.identity;
            _bang.color = new Color(1f, 0.16f, 0.12f, fade);
            _bang.sortingOrder = _sr != null ? _sr.sortingOrder + 4 : 16;
            float above = transform.TransformPoint(new Vector3(ax, ay, 0f)).y;
            FitBang(sc, above);
        }

        // Keep the mark inside the playfield and in the gap between limbs.
        // aboveHead is the world Y of the clearance, so a low ceiling shrinks
        // the mark instead of pulling it down over the bird.
        void FitBang(float sc, float aboveHead)
        {
            if (_bang == null || _bang.sprite == null) return;
            var t = _bang.transform;
            var box = _bang.bounds;
            if (box.size.y < 0.001f) return;
            float topLim = float.PositiveInfinity;
            float botLim = float.NegativeInfinity;
            float leftLim = float.NegativeInfinity;
            float rightLim = float.PositiveInfinity;
            var cam = Camera.main;
            if (cam != null && cam.orthographic)
            {
                var a = cam.ViewportToWorldPoint(new Vector3(0.02f, 0.02f, 10f));
                var b = cam.ViewportToWorldPoint(new Vector3(0.98f, 0.98f, 10f));
                leftLim = Mathf.Min(a.x, b.x);
                rightLim = Mathf.Max(a.x, b.x);
                botLim = Mathf.Min(a.y, b.y);
                topLim = Mathf.Max(a.y, b.y);
            }
            float ceiling = LimbEdge(box.center.x, true);
            float floor = LimbEdge(box.center.x, false);
            const float gap = 0.03f;
            if (ceiling < topLim) topLim = ceiling - gap;
            if (floor > botLim) botLim = floor + gap;
            if (aboveHead > botLim) botLim = aboveHead;
            float room = topLim - botLim;
            if (room > 0.08f && box.size.y > room)
            {
                float fit = sc * (room / box.size.y);
                if (fit < sc)
                {
                    t.localScale = Vector3.one * Mathf.Max(0.2f, fit);
                    box = _bang.bounds;
                }
            }
            Vector3 shift = Vector3.zero;
            if (box.max.y > topLim) shift.y -= box.max.y - topLim;
            float bottom = box.min.y + shift.y;
            // A tight limb gap must not drag the glyph back onto the head.
            if (bottom < aboveHead)
            {
                shift.y += aboveHead - bottom;
                bottom = aboveHead;
            }
            if (bottom < botLim)
            {
                float up = botLim - bottom;
                float spare = topLim - (box.max.y + shift.y);
                if (up > spare) up = Mathf.Max(0f, spare);
                shift.y += up;
            }
            if (box.max.x > rightLim) shift.x -= box.max.x - rightLim;
            float left = box.min.x + shift.x;
            if (left < leftLim)
            {
                float back = leftLim - left;
                float spare = rightLim - (box.max.x + shift.x);
                if (back > spare) back = Mathf.Max(0f, spare);
                shift.x += back;
            }
            if (shift.sqrMagnitude > 0.0000001f)
                t.position += shift;
        }

        // above: underside of the next limb. Otherwise the top of the perch under this bird.
        float LimbEdge(float worldX, bool above)
        {
            float best = above ? float.PositiveInfinity : float.NegativeInfinity;
            if (above)
            {
                var limb = transform.parent;
                var garden = limb != null ? limb.parent : null;
                if (garden == null) return best;
                float y0 = transform.position.y;
                int n = garden.childCount;
                for (int i = 0; i < n; i++)
                {
                    var ch = garden.GetChild(i);
                    if (ch == null || ch == limb || !ch.gameObject.activeInHierarchy) continue;
                    float dy = ch.position.y - y0;
                    if (dy < 0.25f || dy > 3.2f) continue;
                    var view = ch.GetComponent<BranchView>();
                    if (view == null || view.Breaking || view.Wood == null) continue;
                    if (!WoodSpan(view.Wood, worldX, true, out float y)) continue;
                    if (y < best) best = y;
                }
                return best;
            }
            var home = transform.parent != null ? transform.parent.GetComponent<BranchView>() : null;
            if (home == null || home.Wood == null) return best;
            if (WoodSpan(home.Wood, worldX, false, out float top))
                best = top;
            return best;
        }

        static bool WoodSpan(SpriteRenderer wood, float worldX, bool underside, out float worldY)
        {
            worldY = 0f;
            bool any = false;
            float edge = underside ? float.PositiveInfinity : float.NegativeInfinity;
            for (int s = -1; s <= 1; s++)
            {
                if (!WoodColumn(wood, worldX + s * 0.06f, underside, out float y)) continue;
                any = true;
                if (underside) { if (y < edge) edge = y; }
                else if (y > edge) edge = y;
            }
            if (!any) return false;
            worldY = edge;
            return true;
        }

        static bool WoodColumn(SpriteRenderer wood, float worldX, bool underside, out float worldY)
        {
            worldY = 0f;
            if (wood == null || !wood.enabled || !wood.gameObject.activeInHierarchy) return false;
            var spr = wood.sprite;
            if (spr == null || spr.texture == null || !spr.texture.isReadable) return false;
            var tex = spr.texture;
            float ppu = spr.pixelsPerUnit;
            var rect = spr.rect;
            if (ppu < 1f || rect.width < 2f || rect.height < 2f) return false;
            Vector3 local = wood.transform.InverseTransformPoint(new Vector3(worldX, wood.transform.position.y, wood.transform.position.z));
            float u = local.x * ppu / rect.width + 0.5f;
            if (wood.flipX) u = 1f - u;
            if (u < 0f || u >= 1f) return false;
            int px = (int)rect.x + Mathf.FloorToInt(u * rect.width);
            if (!RailRow(tex, px, underside, out int py)) return false;
            float v = (py + 0.5f) / rect.height;
            float localY = (v - 0.5f) * (rect.height / ppu);
            worldY = wood.transform.TransformPoint(new Vector3(local.x, localY, 0f)).y;
            return true;
        }

        struct RailCols
        {
            public int Id;
            public int[] Lo;
            public int[] Hi;
        }

        static RailCols _rail0, _rail1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetBangRails()
        {
            _rail0 = default;
            _rail1 = default;
        }

        static bool RailRow(Texture2D tex, int x, bool underside, out int row)
        {
            row = -1;
            if (tex == null || !EnsureRailCols(tex)) return false;
            int id = tex.GetInstanceID();
            int[] col = null;
            if (_rail0.Id == id) col = underside ? _rail0.Lo : _rail0.Hi;
            else if (_rail1.Id == id) col = underside ? _rail1.Lo : _rail1.Hi;
            if (col == null || (uint)x >= (uint)col.Length) return false;
            row = col[x];
            return row >= 0;
        }

        static bool EnsureRailCols(Texture2D tex)
        {
            int id = tex.GetInstanceID();
            if (_rail0.Id == id) return _rail0.Lo != null;
            if (_rail1.Id == id) return _rail1.Lo != null;
            Color32[] pix = null;
            try { pix = tex.GetPixels32(); }
            catch (System.Exception) { pix = null; }
            int w = tex.width;
            int h = tex.height;
            var map = new RailCols { Id = id };
            if (pix != null && w >= 2 && h >= 2 && pix.Length >= w * h)
            {
                var lo = new int[w];
                var hi = new int[w];
                for (int x = 0; x < w; x++)
                {
                    int bot = -1;
                    int top = -1;
                    int i = x;
                    for (int y = 0; y < h; y++)
                    {
                        if (pix[i].a > 32)
                        {
                            if (bot < 0) bot = y;
                            top = y;
                        }
                        i += w;
                    }
                    lo[x] = bot;
                    hi[x] = top;
                }
                map.Lo = lo;
                map.Hi = hi;
            }
            if (_rail0.Id == 0) _rail0 = map;
            else _rail1 = map;
            return map.Lo != null;
        }

        void PlaceSelect(bool on)
        {
            _selectA = Mathf.MoveTowards(_selectA, on ? 1f : 0f, 8f * Time.deltaTime);
            if (_selectA <= 0.01f)
            {
                HideFx(_aura);
                return;
            }
            EnsureAura();
            var tint = Wow.Of(Color);
            float pulse = 0.72f + 0.28f * Mathf.Sin(Time.time * 5.2f + _phase);
            float a = _selectA * pulse;
            int order = _sr != null ? _sr.sortingOrder : 12;
            int layer = _sr != null ? _sr.sortingLayerID : 0;
            var halo = _aura[0];
            halo.sprite = SpriteCatalog.Glow;
            halo.color = new Color(tint.r, tint.g, tint.b, 0.40f * a);
            halo.transform.localPosition = new Vector3(0f, 0.42f, 0f);
            halo.transform.localRotation = Quaternion.identity;
            halo.transform.localScale = Vector3.one * (3.15f + 0.28f * pulse);
            halo.sortingOrder = order - 3;
            halo.sortingLayerID = layer;
            halo.enabled = true;
            float spin = Time.time * 70f + _phase * 20f;
            for (int i = 0; i < 5; i++)
            {
                var dot = _aura[1 + i];
                float ang = (spin + i * 72f) * Mathf.Deg2Rad;
                dot.sprite = SpriteCatalog.Glow;
                dot.color = new Color(tint.r, tint.g, tint.b, 0.88f * a);
                dot.transform.localPosition = new Vector3(Mathf.Cos(ang) * 1.15f, 0.42f + Mathf.Sin(ang) * 1.15f, 0f);
                dot.transform.localRotation = Quaternion.identity;
                dot.transform.localScale = Vector3.one * 0.42f;
                dot.sortingOrder = order + 3;
                dot.sortingLayerID = layer;
                dot.enabled = true;
            }
            for (int i = 0; i < 3; i++)
            {
                var sp = _aura[6 + i];
                float ang = (-spin * 1.35f + i * 120f) * Mathf.Deg2Rad;
                float rad = 1.45f + 0.12f * Mathf.Sin(Time.time * 3f + i);
                float tw = 0.28f + 0.10f * Mathf.Sin(Time.time * 8f + i * 2f);
                sp.sprite = SpriteCatalog.Sparkle;
                sp.color = new Color(
                    Mathf.Lerp(tint.r, 1f, 0.35f),
                    Mathf.Lerp(tint.g, 1f, 0.35f),
                    Mathf.Lerp(tint.b, 1f, 0.35f),
                    0.92f * a);
                sp.transform.localPosition = new Vector3(Mathf.Cos(ang) * rad, 0.42f + Mathf.Sin(ang) * rad * 0.82f, 0f);
                sp.transform.localScale = Vector3.one * tw;
                sp.transform.localRotation = Quaternion.Euler(0f, 0f, Time.time * 120f + i * 40f);
                sp.sortingOrder = order + 4;
                sp.sortingLayerID = layer;
                sp.enabled = true;
            }
        }

        void PlaceCheerFx(bool on)
        {
            _cheerA = Mathf.MoveTowards(_cheerA, on ? 1f : 0f, 7f * Time.deltaTime);
            if (_cheerA <= 0.01f)
            {
                HideFx(_twink);
                return;
            }
            EnsureTwink();
            int order = _sr != null ? _sr.sortingOrder : 12;
            int layer = _sr != null ? _sr.sortingLayerID : 0;
            for (int i = 0; i < _twink.Length; i++)
            {
                var sp = _twink[i];
                float u = Mathf.Repeat(Time.time * 0.85f + _phase * 0.17f + i * 0.25f, 1f);
                float ang = (i * 90f + u * 40f) * Mathf.Deg2Rad;
                float rise = Mathf.Lerp(-0.2f, 1.55f, u);
                float rad = 0.35f + u * 0.7f;
                float hue = Mathf.Repeat(Time.time * 0.35f + i * 0.18f + _phase * 0.02f, 1f);
                var col = UnityEngine.Color.HSVToRGB(hue, 0.55f, 1f);
                col = UnityEngine.Color.Lerp(new Color(1f, 0.82f, 0.28f), col, 0.55f);
                float fade = _cheerA * Mathf.Sin(u * Mathf.PI);
                sp.sprite = (i & 1) == 0 ? SpriteCatalog.Sparkle : SpriteCatalog.Glow;
                sp.color = new Color(col.r, col.g, col.b, fade);
                sp.transform.localPosition = new Vector3(Mathf.Cos(ang) * rad, rise, 0f);
                float sc = ((i & 1) == 0 ? 0.34f : 0.55f) * (0.65f + 0.45f * Mathf.Sin(u * Mathf.PI));
                sp.transform.localScale = Vector3.one * sc;
                sp.transform.localRotation = Quaternion.Euler(0f, 0f, u * 180f + i * 30f);
                sp.sortingOrder = order + 5;
                sp.sortingLayerID = layer;
                sp.enabled = true;
            }
        }

        void EnsureAura()
        {
            if (_aura != null) return;
            _aura = new SpriteRenderer[9];
            for (int i = 0; i < _aura.Length; i++)
            {
                var spr = i >= 6 ? SpriteCatalog.Sparkle : SpriteCatalog.Glow;
                var go = WorldBuilder.Sprite(i == 0 ? "Aura" : "AuraBit", spr, transform.position, 0.2f, 11, transform);
                go.transform.localRotation = Quaternion.identity;
                var sr = go.GetComponent<SpriteRenderer>();
                sr.enabled = false;
                _aura[i] = sr;
            }
        }

        void EnsureTwink()
        {
            if (_twink != null) return;
            _twink = new SpriteRenderer[4];
            for (int i = 0; i < _twink.Length; i++)
            {
                var spr = (i & 1) == 0 ? SpriteCatalog.Sparkle : SpriteCatalog.Glow;
                var go = WorldBuilder.Sprite("Twink", spr, transform.position, 0.2f, 14, transform);
                go.transform.localRotation = Quaternion.identity;
                var sr = go.GetComponent<SpriteRenderer>();
                sr.enabled = false;
                _twink[i] = sr;
            }
        }

        static void HideFx(SpriteRenderer[] fx)
        {
            if (fx == null) return;
            for (int i = 0; i < fx.Length; i++)
                if (fx[i] != null) fx[i].enabled = false;
        }

        void PlaceGlow(float target)
        {
            _glowA = Mathf.MoveTowards(_glowA, Mathf.Clamp01(target), 9f * Time.deltaTime);
            bool on = _glowA > 0.01f && _sr != null && _sr.enabled && _sr.sprite != null;
            if (!on)
            {
                // Faded rings leave the bird so a bob does not drag disabled copies.
                RetireRing(ref _glow, ref _glowHidden);
                RetireRing(ref _kitGlow, ref _kitGlowHidden);
                return;
            }
            float a = _glowA * (0.9f + 0.1f * Mathf.Sin(Time.time * 6f + _phase));
            int bodyOrder = _sr.sortingOrder;
            if (_glow == null) _glow = RentRing(transform);
            float bodyUnitsPerPx = 1f / _sr.sprite.pixelsPerUnit;
            ShowRing(_glow, _sr, bodyUnitsPerPx, a, _sr.sortingLayerID, bodyOrder - 2, ref _glowHidden); // behind kit bow (body-1)
            bool kitShown = _kit != null && _kit.enabled && _kit.sprite != null;
            if (!kitShown)
            {
                RetireRing(ref _kitGlow, ref _kitGlowHidden);
                return;
            }
            if (_kitGlow == null) _kitGlow = RentRing(_kit.transform);
            // Kit ring lives under the scaled kit transform: convert body px to kit-local units.
            float ks = Mathf.Max(1e-4f, Mathf.Abs(_kit.transform.localScale.x));
            ShowRing(_kitGlow, _kit, bodyUnitsPerPx / ks, a, _sr.sortingLayerID, bodyOrder - 2, ref _kitGlowHidden);
        }

        SpriteRenderer[] RentRing(Transform parent)
        {
            var got = GlowPool.Take(parent);
            return got != null ? got : MakeGlowRing(parent);
        }

        void PlaceFace(BirdMood.Pose mood, bool on)
        {
            if (_face == null) return;
            if (_face.enabled != on) _face.enabled = on;
            if (!on) return;
            if (Time.time >= _nextBlink)
            {
                _blinkUntil = Time.time + 0.08f;
                _nextBlink = Time.time + mood.BlinkEvery + Random.Range(-0.4f, 0.8f);
            }
            bool blink = Time.time < _blinkUntil || Sleeping;
            float x = FaceLeft ? -mood.HeadX : mood.HeadX;
            float y = mood.HeadY + (Sleeping ? -0.07f : 0f);
            var facePos = new Vector3(x, y, 0f);
            if (_face.transform.localPosition != facePos) _face.transform.localPosition = facePos;
            if (_face.transform.localRotation != Quaternion.identity)
                _face.transform.localRotation = Quaternion.identity;
            if (_face.flipX != FaceLeft) _face.flipX = FaceLeft;
            // Face always in front of kit bow
            int faceOrder = _sr != null ? _sr.sortingOrder + 2 : 14;
            FlockSort.Apply(_face, faceOrder);
            float fs = mood.FaceScale;
            var faceScale = new Vector3(fs, blink ? fs * 0.18f : fs, 1f);
            if (_face.transform.localScale != faceScale) _face.transform.localScale = faceScale;
            if (!Frozen && _face.color != UnityEngine.Color.white) _face.color = UnityEngine.Color.white;
        }

        void EnsureKit()
        {
            if (Sex == BirdSex.Neutral)
            {
                if (_kit != null) _kit.enabled = false;
                return;
            }
            var spr = Sex == BirdSex.Female ? SpriteCatalog.Bow : SpriteCatalog.CrownFor(Color);
            if (_kit != null)
            {
                int kitHeld = _kit.sortingOrder;
                if (_kit.sprite != spr) _kit.sprite = spr;
                if (!_kit.enabled) _kit.enabled = true;
                FlockSort.Apply(_kit, kitHeld);
                return;
            }
            var go = WorldBuilder.Sprite("Kit", spr, transform.position, 0.3f, FlockSort.Perch + 1, transform);
            go.transform.localRotation = Quaternion.identity;
            _kit = go.GetComponent<SpriteRenderer>();
            FlockSort.Apply(_kit, FlockSort.Perch + 1);
        }

        // Per-frame female bow locals (facing-right). Rows = BirdColor enum order
        // Ruby,Gold,Teal,Violet,Peach. Cols = rest,_1,_2,_3,_4,_5. Measured so
        // bow loops embed crown (behind-head). Flip X when FaceLeft.
        static readonly float[,] BowLocalX = {
            { -0.11f, -0.270f, -0.259f, -0.270f, -0.270f, -0.11f }, // Ruby
            { -0.11f, -0.270f, -0.259f, -0.270f, -0.270f, -0.11f }, // Gold
            { -0.11f, -0.270f, -0.259f, -0.270f, -0.270f, -0.11f }, // Teal
            { -0.11f, -0.270f, -0.259f, -0.270f, -0.270f, -0.11f }, // Violet
            { -0.11f, -0.270f, -0.259f, -0.270f, -0.270f, -0.11f }, // Peach
        };
        static readonly float[,] BowLocalY = {
            { 1.055f, 0.831f, 0.843f, 0.831f, 0.831f, 1.055f }, // Ruby
            { 1.055f, 0.831f, 0.843f, 0.831f, 0.831f, 1.055f }, // Gold
            { 1.055f, 0.831f, 0.843f, 0.831f, 0.831f, 1.055f }, // Teal
            { 1.055f, 0.831f, 0.843f, 0.831f, 0.831f, 1.055f }, // Violet
            { 1.055f, 0.831f, 0.843f, 0.831f, 0.831f, 1.055f }, // Peach
        };

        // Per-frame male crown locals (facing-right), same row/col order as the bow.
        // _1/_2 are the spread-wing flap frames, eye-aligned to rest; their head
        // dome sits ~44px lower, so kit Y drops 0.15-0.16u on those columns.
        // All five share the teal body: crown rests ON the head (drawn in front), tipped 26deg so the band
        // bottom follows the dome; every band-bottom point >=1px into feathers (no back gap).
        static readonly float[,] CrownLocalX = {
            { -0.03f, -0.168f, -0.156f, -0.168f, -0.168f, -0.03f }, // Ruby
            { -0.03f, -0.168f, -0.156f, -0.168f, -0.168f, -0.03f }, // Gold
            { -0.03f, -0.168f, -0.156f, -0.168f, -0.168f, -0.03f }, // Teal
            { -0.03f, -0.168f, -0.156f, -0.168f, -0.168f, -0.03f }, // Violet
            { -0.03f, -0.168f, -0.156f, -0.168f, -0.168f, -0.03f }, // Peach
        };
        static readonly float[,] CrownLocalY = {
            { 1.374f, 1.252f, 1.264f, 1.252f, 1.252f, 1.374f }, // Ruby
            { 1.374f, 1.252f, 1.264f, 1.252f, 1.252f, 1.374f }, // Gold
            { 1.374f, 1.252f, 1.264f, 1.252f, 1.252f, 1.374f }, // Teal
            { 1.374f, 1.252f, 1.264f, 1.252f, 1.252f, 1.374f }, // Violet
            { 1.374f, 1.252f, 1.264f, 1.252f, 1.252f, 1.374f }, // Peach
        };

        // Garden accessory anchor. Locals are facing-right; faceLeft mirrors X only.
        // Splash DrawAvatarKit uses this so the dialog does not keep a second offset.
        // Bow and crown art is the mirror of the right-facing body. Build 39 flipped
        // the sprite only when the body did not, and that flip mirrored the tilt.
        // Build 40 copied the body's flip (flip when faceLeft) and negated tilt,
        // which turned the kit around on the dome. flip is applied first, then tilt
        // (SpriteRenderer order). The splash scales, then rotates.
        public static void KitAnchor(bool crown, int frame, bool faceLeft,
            out float x, out float y, out float scale, out float tilt, out bool flip,
            bool buttonSeat = false)
        {
            if (frame < 0 || frame > 5) frame = 0;
            float lx = crown ? CrownLocalX[0, frame] : BowLocalX[0, frame];
            float ly = crown ? CrownLocalY[0, frame] : BowLocalY[0, frame];
            x = faceLeft ? -lx : lx;
            y = ly;
            scale = crown ? 0.42f : SpriteCatalog.BowScale;
            float tip = crown ? 26f : 12f;
            flip = !faceLeft;
            tilt = flip ? -tip : tip;
            if (!buttonSeat) return;
            // Color-row buttons only. Garden birds and the home avatar leave this
            // false, so their seat stays on the locals above. The button quad is
            // the full sheet: the crown base still clears the dome, and the bow
            // knot sits a hair high. Nudge is in the same facing space as x.
            if (crown)
            {
                x += faceLeft ? -0.06f : 0.06f;
                y -= 0.15f;
            }
            else
            {
                x += faceLeft ? -0.02f : 0.02f;
                y -= 0.045f;
            }
        }

        // Bottom of the alert "!". Same local space as KitAnchor. Clears the
        // head, the bow, and the crown by a small gap. scale is the glyph
        // localScale for a child of the bird, so world size tracks birdScale.
        public static void AlertAnchor(int frame, bool faceLeft, float birdScale,
            out float x, out float y, out float scale)
        {
            if (frame < 0 || frame > 5) frame = 0;
            float top = KitTop(true, frame, faceLeft, out x);
            float bowTop = KitTop(false, frame, faceLeft, out _);
            if (bowTop > top) top = bowTop;
            const float gap = 0.10f;
            y = top + gap;
            float unit = birdScale < 0.05f ? 0.42f : birdScale;
            // Glyph is 1 local unit tall at scale 1. Parent scale is the bird,
            // so a local scale of ~0.58 grows and shrinks with it. Floor keeps
            // a tiny bird readable.
            float world = unit * 0.58f;
            if (world < 0.14f) world = 0.14f;
            scale = world / unit;
        }

        // Top of the tilted kit sprite, bird-local. x is the kit anchor.
        static float KitTop(bool crown, int frame, bool faceLeft, out float x)
        {
            KitAnchor(crown, frame, faceLeft, out x, out float ky, out float ks, out float tilt, out _);
            float halfW = 0.22f * ks;
            float halfH = 0.20f * ks;
            var spr = crown ? SpriteCatalog.Crown : SpriteCatalog.Bow;
            if (spr != null && spr.pixelsPerUnit > 1f)
            {
                halfW = spr.rect.width * 0.5f / spr.pixelsPerUnit * ks;
                halfH = spr.rect.height * 0.5f / spr.pixelsPerUnit * ks;
            }
            float rad = tilt * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad);
            float s = Mathf.Sin(rad);
            if (c < 0f) c = -c;
            if (s < 0f) s = -s;
            return ky + halfH * c + halfW * s;
        }

        void PlaceKit(BirdMood.Pose mood, bool on)
        {
            if (Sex == BirdSex.Neutral)
            {
                if (_kit != null && _kit.enabled) _kit.enabled = false;
                return;
            }
            if (_kit == null) return;
            if (_kit.enabled != on) _kit.enabled = on;
            if (!on) return;
            bool girl = Sex == BirdSex.Female;
            int fi = SpriteCatalog.PoseIndex(_sr != null ? _sr.sprite : null, Color, Sex);
            KitAnchor(!girl, fi, FaceLeft, out float bowX, out float bowY, out float ks, out float tiltZ, out bool kitFlip);
            var kitPos = new Vector3(bowX, bowY, 0f);
            if (_kit.transform.localPosition != kitPos) _kit.transform.localPosition = kitPos;
            var kitRot = Quaternion.Euler(0f, 0f, tiltZ);
            if (_kit.transform.localRotation != kitRot) _kit.transform.localRotation = kitRot;
            if (_kit.flipX != kitFlip) _kit.flipX = kitFlip;
            // Bow tucks behind body/head; crown rests in front of the head, below the face layer.
            int bodyOrder = _sr != null ? _sr.sortingOrder : FlockSort.Perch;
            int kitOrder = girl ? bodyOrder - 1 : bodyOrder + 1;
            FlockSort.Apply(_kit, kitOrder);
            var kitScale = new Vector3(ks, ks, 1f);
            if (_kit.transform.localScale != kitScale) _kit.transform.localScale = kitScale;
            if (!Frozen && _kit.color != UnityEngine.Color.white) _kit.color = UnityEngine.Color.white;
        }

        void BeatWings()
        {
            if (Time.time < _nextWing) return;
            _nextWing = Time.time + Random.Range(0.07f, 0.12f);
            if (_ruffle > 0f && !Flapping && Lift < 0.05f) Sfx.FlapSoft();
            else Sfx.Flap();
        }

        void MaybeRuffle()
        {
            if (Time.time < _nextRuffle) return;
            if (!Sfx.QuietMid)
            {
                _nextRuffle = Time.time + Random.Range(1.2f, 2.4f);
                return;
            }
            _nextRuffle = Time.time + Random.Range(5.5f, 10f);
            _ruffle = 0.22f;
            _nextWing = 0f;
            Sfx.FlapSoft();
        }

        // Rings checked out while a bird is lit, parked once the fade finishes.
        static class GlowPool
        {
            const int Max = 12;
            static readonly System.Collections.Generic.List<SpriteRenderer[]> Free =
                new System.Collections.Generic.List<SpriteRenderer[]>(Max);
            static Transform _bin;

            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
            static void Reset()
            {
                Free.Clear();
                _bin = null;
            }

            public static SpriteRenderer[] Take(Transform parent)
            {
                while (Free.Count > 0)
                {
                    int last = Free.Count - 1;
                    var ring = Free[last];
                    Free.RemoveAt(last);
                    if (!Alive(ring)) continue;
                    for (int i = 0; i < ring.Length; i++)
                    {
                        var sr = ring[i];
                        sr.transform.SetParent(parent, false);
                        sr.gameObject.SetActive(true);
                        sr.enabled = false;
                    }
                    return ring;
                }
                return null;
            }

            public static void Give(SpriteRenderer[] ring)
            {
                if (!Alive(ring))
                {
                    DestroyRing(ring);
                    return;
                }
                if (Free.Count >= Max)
                {
                    DestroyRing(ring);
                    return;
                }
                if (_bin == null)
                {
                    var go = new GameObject("SelGlowPool");
                    go.SetActive(false);
                    _bin = go.transform;
                }
                for (int i = 0; i < ring.Length; i++)
                {
                    var sr = ring[i];
                    sr.enabled = false;
                    sr.gameObject.SetActive(false);
                    sr.transform.SetParent(_bin, false);
                }
                Free.Add(ring);
            }

            static bool Alive(SpriteRenderer[] ring)
            {
                if (ring == null || ring.Length == 0) return false;
                for (int i = 0; i < ring.Length; i++)
                    if (ring[i] == null) return false;
                return true;
            }

            static void DestroyRing(SpriteRenderer[] ring)
            {
                if (ring == null) return;
                for (int i = 0; i < ring.Length; i++)
                    if (ring[i] != null) Object.Destroy(ring[i].gameObject);
            }
        }
    }
}
