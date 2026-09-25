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
        float _glowA;
        static Material _silhouette;
        float _phase;
        float _liftShown;
        float _nextWing;
        float _nextRuffle;
        float _ruffle;
        float _flutterUntil;
        float _blinkUntil;
        float _nextBlink;

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

        void EnsureFace()
        {
            if (_face != null)
            {
                _face.sprite = BirdMood.Face(Color);
                return;
            }
            var mood = BirdMood.Of(Color);
            var go = WorldBuilder.Sprite("Mood", BirdMood.Face(Color), transform.position, mood.FaceScale, 13, transform);
            go.transform.localRotation = Quaternion.identity;
            _face = go.GetComponent<SpriteRenderer>();
            _face.sortingOrder = 13;
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
            bool fly = !Sleeping && !Shrouded && show && (Flapping || Lift > 0.05f || _ruffle > 0f);
            var mood = BirdMood.Of(Color);
            if (show)
            {
                _sr.flipX = FaceLeft;
                // Spread-wing flight frames (_1/_2) only while actually airborne.
                // A seated Flutter or ruffle keeps the rest frame so toes stay on bark.
                bool airborne = Frozen || _liftShown > 0.05f || (Flapping && _flutterUntil <= 0f);
                bool wings = fly && airborne;
                // BirdFrame steps poses at t*16, so t = Time.time * FlapRate gives
                // 16*FlapRate poses/sec (rest,_1,_2,_1 = 4 poses per wingbeat).
                // 1.25 = 20 poses/sec = 5 wingbeats/sec, frame-rate independent.
                _sr.sprite = SpriteCatalog.BirdFrame(Color, (Time.time + _phase) * (wings ? FlapRate : 0.06f), wings, Sex);
                if (!Frozen)
                {
                    _sr.color = Shrouded ? new Color(0.04f, 0.03f, 0.05f, 1f) : UnityEngine.Color.white;
                    _sr.sortingOrder = Shrouded ? 7 : (Lift > 0.05f ? 40 : 12);
                }
            }
            // Draw order (back→front): kit bow → body/flaps → face
            bool kitOn = show && !Shrouded && Sex != BirdSex.Neutral;
            if (kitOn) EnsureKit();
            PlaceKit(mood, kitOn);
            PlaceFace(mood, show && !Shrouded);
            PlaceGlow(show && !Shrouded && !Sleeping && !Frozen && Lift >= 1f);
            if (fly && !Frozen) BeatWings();
            else if (show && !Sleeping && !Shrouded && !Frozen) MaybeRuffle();

            if (Frozen) return;
            if (Shrouded)
            {
                transform.localPosition = RestLocal;
                transform.localRotation = Quaternion.identity;
                transform.localScale = RestScale;
                return;
            }
            float wantLift = Sleeping ? -0.10f : Lift;
            _liftShown = Mathf.MoveTowards(_liftShown, wantLift, 4.2f * Time.deltaTime);
            float scale = mood.Scale;
            if (Sleeping)
            {
                float snore = Mathf.Sin(Time.time * (Color == BirdColor.Violet ? 1.35f : 1.7f) + _phase);
                float droop = Color == BirdColor.Violet ? 0.04f : 0.025f;
                transform.localPosition = new Vector3(RestLocal.x, RestLocal.y + snore * droop + _liftShown, RestLocal.z);
                float z = (FaceLeft ? 8f : -8f) + snore * 2.5f + mood.Lean * 0.35f;
                transform.localRotation = Quaternion.Euler(0f, 0f, z);
                float breathe = 1f + snore * 0.03f;
                transform.localScale = new Vector3(RestScale.x * scale * breathe, RestScale.y * scale * (2f - breathe), 1f);
                return;
            }
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

        // Selected run (BranchView.SetReady lifts tip birds to Lift 1.15) gets a thick
        // white border. It is drawn from the SAME sprite as the body (and kit): a ring of
        // white-silhouette copies pushed outward, one sorting step behind. Same sprite,
        // pivot, transform and flip as the bird, so it can't drift off the bird.
        const int GlowRing = 16;           // copies per ring
        const float GlowSolidPx = 19f;     // inner ring radius, in body source px
        const float GlowSoftPx = 29f;      // outer (soft) ring radius, in body source px
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

        static void HideRing(SpriteRenderer[] ring)
        {
            if (ring == null) return;
            for (int i = 0; i < ring.Length; i++)
                if (ring[i] != null) ring[i].enabled = false;
        }

        // unitsPerBodyPx: this renderer's local units per body source pixel.
        static void ShowRing(SpriteRenderer[] ring, SpriteRenderer src, float unitsPerBodyPx,
            float a, int layer, int order)
        {
            if (ring == null || src == null || src.sprite == null) { HideRing(ring); return; }
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

        void PlaceGlow(bool selected)
        {
            _glowA = Mathf.MoveTowards(_glowA, selected ? 1f : 0f, 9f * Time.deltaTime);
            bool on = _glowA > 0.01f && _sr != null && _sr.enabled && _sr.sprite != null;
            if (!on)
            {
                HideRing(_glow);
                HideRing(_kitGlow);
                return;
            }
            float a = _glowA * (0.9f + 0.1f * Mathf.Sin(Time.time * 6f + _phase));
            int bodyOrder = _sr.sortingOrder;
            if (_glow == null) _glow = MakeGlowRing(transform);
            float bodyUnitsPerPx = 1f / _sr.sprite.pixelsPerUnit;
            ShowRing(_glow, _sr, bodyUnitsPerPx, a, _sr.sortingLayerID, bodyOrder - 2); // behind kit bow (body-1)
            bool kitShown = _kit != null && _kit.enabled && _kit.sprite != null;
            if (!kitShown) { HideRing(_kitGlow); return; }
            if (_kitGlow == null) _kitGlow = MakeGlowRing(_kit.transform);
            // Kit ring lives under the scaled kit transform: convert body px to kit-local units.
            float ks = Mathf.Max(1e-4f, Mathf.Abs(_kit.transform.localScale.x));
            ShowRing(_kitGlow, _kit, bodyUnitsPerPx / ks, a, _sr.sortingLayerID, bodyOrder - 2);
        }

        void PlaceFace(BirdMood.Pose mood, bool on)
        {
            if (_face == null) return;
            _face.enabled = on;
            if (!on) return;
            if (Time.time >= _nextBlink)
            {
                _blinkUntil = Time.time + 0.08f;
                _nextBlink = Time.time + mood.BlinkEvery + Random.Range(-0.4f, 0.8f);
            }
            bool blink = Time.time < _blinkUntil || Sleeping;
            float x = FaceLeft ? -mood.HeadX : mood.HeadX;
            float y = mood.HeadY + (Sleeping ? -0.02f : 0f);
            _face.transform.localPosition = new Vector3(x, y, 0f);
            _face.transform.localRotation = Quaternion.identity;
            _face.flipX = FaceLeft;
            // Face always in front of kit bow
            _face.sortingOrder = _sr != null ? _sr.sortingOrder + 2 : 14;
            float fs = mood.FaceScale * (blink ? 1f : 1f);
            _face.transform.localScale = new Vector3(fs, blink ? fs * 0.18f : fs, 1f);
            if (!Frozen) _face.color = UnityEngine.Color.white;
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
                _kit.sprite = spr;
                _kit.enabled = true;
                return;
            }
            var go = WorldBuilder.Sprite("Kit", spr, transform.position, 0.3f, 13, transform);
            go.transform.localRotation = Quaternion.identity;
            _kit = go.GetComponent<SpriteRenderer>();
            _kit.sortingOrder = 13;
        }

        // Per-frame female bow locals (facing-right). Rows = BirdColor enum order
        // Ruby,Gold,Teal,Violet,Peach. Cols = rest,_1,_2,_3,_4,_5. Measured so
        // bow loops embed crown (behind-head). Flip X when FaceLeft.
        static readonly float[,] BowLocalX = {
            { -0.11f, -0.270f, -0.259f, -0.11f, -0.11f, -0.11f }, // Ruby
            { -0.11f, -0.270f, -0.259f, -0.11f, -0.11f, -0.11f }, // Gold
            { -0.11f, -0.270f, -0.259f, -0.11f, -0.11f, -0.11f }, // Teal
            { -0.11f, -0.270f, -0.259f, -0.11f, -0.11f, -0.11f }, // Violet
            { -0.11f, -0.270f, -0.259f, -0.11f, -0.11f, -0.11f }, // Peach
        };
        static readonly float[,] BowLocalY = {
            { 1.055f, 0.831f, 0.843f, 1.055f, 1.055f, 1.055f }, // Ruby
            { 1.055f, 0.831f, 0.843f, 1.055f, 1.055f, 1.055f }, // Gold
            { 1.055f, 0.831f, 0.843f, 1.055f, 1.055f, 1.055f }, // Teal
            { 1.055f, 0.831f, 0.843f, 1.055f, 1.055f, 1.055f }, // Violet
            { 1.055f, 0.831f, 0.843f, 1.055f, 1.055f, 1.055f }, // Peach
        };

        // Per-frame male crown locals (facing-right), same row/col order as the bow.
        // _1/_2 are the spread-wing flap frames, eye-aligned to rest; their head
        // dome sits ~44px lower, so kit Y drops 0.15-0.16u on those columns.
        // All five share the teal body: crown rests ON the head (drawn in front), tipped 26deg so the band
        // bottom follows the dome; every band-bottom point >=1px into feathers (no back gap).
        static readonly float[,] CrownLocalX = {
            { -0.03f, -0.168f, -0.156f, -0.03f, -0.03f, -0.03f }, // Ruby
            { -0.03f, -0.168f, -0.156f, -0.03f, -0.03f, -0.03f }, // Gold
            { -0.03f, -0.168f, -0.156f, -0.03f, -0.03f, -0.03f }, // Teal
            { -0.03f, -0.168f, -0.156f, -0.03f, -0.03f, -0.03f }, // Violet
            { -0.03f, -0.168f, -0.156f, -0.03f, -0.03f, -0.03f }, // Peach
        };
        static readonly float[,] CrownLocalY = {
            { 1.374f, 1.252f, 1.264f, 1.374f, 1.374f, 1.374f }, // Ruby
            { 1.374f, 1.252f, 1.264f, 1.374f, 1.374f, 1.374f }, // Gold
            { 1.374f, 1.252f, 1.264f, 1.374f, 1.374f, 1.374f }, // Teal
            { 1.374f, 1.252f, 1.264f, 1.374f, 1.374f, 1.374f }, // Violet
            { 1.374f, 1.252f, 1.264f, 1.374f, 1.374f, 1.374f }, // Peach
        };

        static int KitFrameIndex(Sprite spr)
        {
            if (spr == null || string.IsNullOrEmpty(spr.name)) return 0;
            var n = spr.name;
            // bird_teal_f_5 / bird_teal_5 / bird_teal_f
            for (int i = 5; i >= 1; i--)
            {
                if (n.EndsWith("_" + i) || n.Contains("_f_" + i) || n.Contains("_m_" + i))
                    return i;
            }
            return 0;
        }

        void PlaceKit(BirdMood.Pose mood, bool on)
        {
            if (Sex == BirdSex.Neutral)
            {
                if (_kit != null) _kit.enabled = false;
                return;
            }
            if (_kit == null) return;
            _kit.enabled = on;
            if (!on) return;
            bool girl = Sex == BirdSex.Female;
            float headX = FaceLeft ? -mood.HeadX : mood.HeadX;
            float bowX;
            float bowY;
            float ks;
            float tiltZ;
            if (girl)
            {
                // Per-frame crown embed (not one global Y) — head redraws in-atlas.
                int fi = KitFrameIndex(_sr != null ? _sr.sprite : null);
                int ci = (int)Color;
                if (ci < 0 || ci >= BowLocalX.GetLength(0)) ci = 0;
                if (fi < 0 || fi > 5) fi = 0;
                float lx = BowLocalX[ci, fi];
                float ly = BowLocalY[ci, fi];
                bowX = FaceLeft ? -lx : lx;
                bowY = ly;
                ks = 0.42f;
                tiltZ = FaceLeft ? -12f : 12f;
            }
            else
            {
                // Male frames share the female body art; crown seated per color.
                int fi = KitFrameIndex(_sr != null ? _sr.sprite : null);
                int ci = (int)Color;
                if (ci < 0 || ci >= CrownLocalX.GetLength(0)) ci = 0;
                if (fi < 0 || fi > 5) fi = 0;
                float lx = CrownLocalX[ci, fi];
                bowX = FaceLeft ? -lx : lx;
                bowY = CrownLocalY[ci, fi];
                ks = 0.42f;
                tiltZ = FaceLeft ? -26f : 26f;
            }
            _kit.transform.localPosition = new Vector3(bowX, bowY, 0f);
            _kit.transform.localRotation = Quaternion.Euler(0f, 0f, tiltZ);
            _kit.flipX = FaceLeft;
            // Bow tucks behind body/head; crown rests in front of the head, below the face layer.
            int bodyOrder = _sr != null ? _sr.sortingOrder : 12;
            _kit.sortingOrder = girl ? bodyOrder - 1 : bodyOrder + 1;
            _kit.transform.localScale = new Vector3(ks, ks, 1f);
            if (!Frozen) _kit.color = UnityEngine.Color.white;
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
    }
}
