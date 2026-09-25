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
        SpriteRenderer _glow;
        SpriteRenderer _kitGlow;
        float _glowA;
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
        // white border: a white, grown silhouette of the body (and kit) one step behind.
        SpriteRenderer MakeGlow(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.enabled = false;
            return r;
        }

        void PlaceGlow(bool selected)
        {
            _glowA = Mathf.MoveTowards(_glowA, selected ? 1f : 0f, 9f * Time.deltaTime);
            bool on = _glowA > 0.01f && _sr != null && _sr.enabled;
            if (!on)
            {
                if (_glow != null) _glow.enabled = false;
                if (_kitGlow != null) _kitGlow.enabled = false;
                return;
            }
            float a = _glowA * (0.9f + 0.1f * Mathf.Sin(Time.time * 6f + _phase));
            var col = new Color(1f, 1f, 1f, a);
            int bodyOrder = _sr.sortingOrder;
            if (_glow == null) _glow = MakeGlow(transform, "SelGlow");
            _glow.sprite = BirdGlow.For(_sr.sprite);
            _glow.enabled = _glow.sprite != null;
            _glow.flipX = _sr.flipX;
            _glow.color = col;
            _glow.sortingLayerID = _sr.sortingLayerID;
            _glow.sortingOrder = bodyOrder - 2; // behind kit bow (body-1) and body
            bool kitShown = _kit != null && _kit.enabled;
            if (kitShown && _kitGlow == null) _kitGlow = MakeGlow(_kit.transform, "SelGlow");
            if (_kitGlow != null)
            {
                float kpx = 1f;
                if (kitShown && _kit.sprite != null && _sr.sprite != null)
                {
                    // world size of one kit px vs one body px (kit is a scaled child)
                    float kitPx = _kit.transform.localScale.x / _kit.sprite.pixelsPerUnit;
                    float bodyPx = 1f / _sr.sprite.pixelsPerUnit;
                    kpx = bodyPx / Mathf.Max(1e-5f, kitPx);
                }
                _kitGlow.sprite = kitShown ? BirdGlow.For(_kit.sprite, kpx) : null;
                _kitGlow.enabled = kitShown && _kitGlow.sprite != null;
                _kitGlow.flipX = _kit != null && _kit.flipX;
                _kitGlow.color = col;
                _kitGlow.sortingLayerID = _sr.sortingLayerID;
                _kitGlow.sortingOrder = bodyOrder - 2;
            }
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
            { -0.11f, -0.11f, -0.11f, -0.11f, -0.11f, -0.11f }, // Ruby
            { -0.11f, -0.11f, -0.11f, -0.11f, -0.11f, -0.11f }, // Gold
            { -0.11f, -0.11f, -0.11f, -0.11f, -0.11f, -0.11f }, // Teal
            { -0.11f, -0.11f, -0.11f, -0.11f, -0.11f, -0.11f }, // Violet
            { -0.11f, -0.11f, -0.11f, -0.11f, -0.11f, -0.11f }, // Peach
        };
        static readonly float[,] BowLocalY = {
            { 1.055f, 0.895f, 0.895f, 1.055f, 1.055f, 1.055f }, // Ruby
            { 1.055f, 0.895f, 0.895f, 1.055f, 1.055f, 1.055f }, // Gold
            { 1.055f, 0.895f, 0.895f, 1.055f, 1.055f, 1.055f }, // Teal
            { 1.055f, 0.895f, 0.895f, 1.055f, 1.055f, 1.055f }, // Violet
            { 1.055f, 0.895f, 0.895f, 1.055f, 1.055f, 1.055f }, // Peach
        };

        // Per-frame male crown locals (facing-right), same row/col order as the bow.
        // _1/_2 are the spread-wing flap frames, eye-aligned to rest; their head
        // dome sits ~44px lower, so kit Y drops 0.15-0.16u on those columns.
// All five share the teal body: crown rests ON the head (drawn in front), tipped 26deg so the band
        // bottom follows the dome; every band-bottom point >=1px into feathers (no back gap).
        static readonly float[,] CrownLocalX = {
            { -0.03f, -0.03f, -0.03f, -0.03f, -0.03f, -0.03f }, // Ruby
            { -0.03f, -0.03f, -0.03f, -0.03f, -0.03f, -0.03f }, // Gold
            { -0.03f, -0.03f, -0.03f, -0.03f, -0.03f, -0.03f }, // Teal
            { -0.03f, -0.03f, -0.03f, -0.03f, -0.03f, -0.03f }, // Violet
            { -0.03f, -0.03f, -0.03f, -0.03f, -0.03f, -0.03f }, // Peach
        };
        static readonly float[,] CrownLocalY = {
            { 1.374f, 1.224f, 1.224f, 1.374f, 1.374f, 1.374f }, // Ruby
            { 1.374f, 1.224f, 1.224f, 1.374f, 1.374f, 1.374f }, // Gold
            { 1.374f, 1.224f, 1.224f, 1.374f, 1.374f, 1.374f }, // Teal
            { 1.374f, 1.224f, 1.224f, 1.374f, 1.374f, 1.374f }, // Violet
            { 1.374f, 1.224f, 1.224f, 1.374f, 1.374f, 1.374f }, // Peach
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
