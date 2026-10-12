using UnityEngine;

namespace FlockFive
{
    // Painterly ostrich. The body stays centered in the background. The neck
    // reaches the feeder a match would have fed, and the head sits in front of
    // that feeder and behind the flock. Motion stays small while the player
    // is carrying birds.
    public sealed class OstrichMark : MonoBehaviour
    {
        public const int BodyOrder = 1;
        public const int NeckOrder = 9;
        public const int HeadOrder = 10;
        public const int NeckParts = 8;
        public const float BodyDrop = 1.50f; // body high and close: the neck is not stretched
        // Whole bird at 72%: it fits the screen width, legs included, and the 160px master
        // head is drawn at or under its native size on device.
        public const float RigScale = 0.72f;
        // Half the visible width; the game camera when there is one, else the stills camera.
        public static float ViewHalfWidth()
        {
            var cam = Camera.main;
            if (cam != null && cam.orthographic) return cam.orthographicSize * cam.aspect;
            return 5.55f * 9f / 16f;
        }
        // Body width from the pivot (neck root) back to the tail, and in front of it, at RigScale.
        public static float BodyBack => 527.5f / OstrichPaint.BodyPpu * RigScale;
        public static float BodyFront => (566f - 527.5f) / OstrichPaint.BodyPpu * RigScale;
        // Feeder sprites pivot at the glass. The port is the flower on the tray.
        public const float PortX = 0.16f;
        public const float PortY = -0.88f;

        public int Feeders = 2;
        public SpriteRenderer Head;
        public bool Settled { get; private set; }

        public OstrichAct Act => _act;
        public int Frame => _frame;
        public bool Calm => _calm;
        public Transform Aim => _head != null ? _head : transform;

        Transform _body;
        Transform _head;
        Transform[] _neck;
        SpriteRenderer _bodySr;
        Transform _legBack, _legFront, _collar;
        SpriteRenderer _collarSr;
        SpriteRenderer _legBackSr, _legFrontSr;
        SpriteRenderer[] _neckSr;
        LineRenderer _line;
        const int NeckSamples = 26;
        OstrichAct _act = OstrichAct.Idle;
        OstrichAct _queued = OstrichAct.Idle;
        int _frame;
        float _frameT;
        float _settle;
        bool _calm;
        bool _returnChain;
        bool _fleeAfterDaze;
        bool _gone;
        bool _wasAlive = true;
        int _shownHits;
        int _shownPecks = -1;
        int _block = -2;
        float _headX;
        float _headY;
        float _drawX;
        float _drawY;
        bool _placed;

        public static OstrichMark Attach(Transform parent, int feeders)
        {
            var root = new GameObject("Ostrich").transform;
            root.SetParent(parent, false);
            var mark = root.gameObject.AddComponent<OstrichMark>();
            mark.Feeders = feeders < 1 ? 2 : feeders;
            mark.Build();
            mark.Preview(OstrichAct.Idle, 0, 0f, WorldBuilder.FeederY, false);
            return mark;
        }

        void Build()
        {
            _body = new GameObject("Body").transform;
            _body.SetParent(transform, false);
            _bodySr = _body.gameObject.AddComponent<SpriteRenderer>();
            FlockSort.Apply(_bodySr, BodyOrder);
            // Legs: same layer as the body, just behind it (z), hinged at the hip.
            _legBackSr = MakeLeg("LegBack", OstrichPaint.BackLeg(), out _legBack);
            _legFrontSr = MakeLeg("LegFront", OstrichPaint.FrontLeg(), out _legFront);
            var collarGo = new GameObject("Collar");
            _collar = collarGo.transform;
            _collar.SetParent(_body, false);
            _collar.localPosition = new Vector3(0f, 0f, 0.01f);
            _collarSr = collarGo.AddComponent<SpriteRenderer>();
            _collarSr.sprite = OstrichPaint.Collar();
            FlockSort.Apply(_collarSr, HeadOrder);
            _body.localScale = new Vector3(RigScale, RigScale, 1f);

            _neck = new Transform[NeckParts];
            _neckSr = new SpriteRenderer[NeckParts];
            var neckSprite = OstrichPaint.Neck();
            for (int i = 0; i < NeckParts; i++)
            {
                var go = new GameObject("Neck" + i);
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = neckSprite;
                sr.enabled = false;
                FlockSort.Apply(sr, NeckOrder);
                _neck[i] = go.transform;
                _neckSr[i] = sr;
            }
            EnsureLine();

            var headGo = new GameObject("Head");
            _head = headGo.transform;
            _head.SetParent(transform, false);
            _head.localScale = new Vector3(RigScale, RigScale, 1f);
            Head = headGo.AddComponent<SpriteRenderer>();
            FlockSort.Apply(Head, HeadOrder);
        }

        SpriteRenderer MakeLeg(string name, Sprite sprite, out Transform t)
        {
            var go = new GameObject(name);
            t = go.transform;
            t.SetParent(_body, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            FlockSort.Apply(sr, BodyOrder);
            return sr;
        }

        void PoseLeg(Transform leg, SpriteRenderer sr, Vector2 hip, float angle, float face)
        {
            if (leg == null) return;
            leg.localPosition = new Vector3(hip.x * face, hip.y, 0.02f);
            leg.localRotation = Quaternion.Euler(0f, 0f, angle * face);
            sr.flipX = face < 0f;
        }

        public void SetCalm(bool calm) => _calm = calm;

        public void Sync(Board b) => Sync(b, _calm);

        public void Sync(Board b, bool calm)
        {
            _calm = calm;
            if (b == null || b.Ostrich == null)
            {
                Hide();
                return;
            }
            var o = b.Ostrich;
            if (!o.Alive)
            {
                if (_wasAlive)
                {
                    _fleeAfterDaze = true;
                    Begin(OstrichAct.Daze);
                }
                _wasAlive = false;
                _shownHits = o.Hits;
                return;
            }
            bool reset = _gone || !_wasAlive || o.Hits < _shownHits
                || (_shownPecks >= 0 && o.PeckIndex < _shownPecks);
            if (reset)
            {
                _gone = false;
                Show();
                _wasAlive = true;
                _shownHits = 0;
                _shownPecks = o.PeckIndex;
                _returnChain = true;
                _fleeAfterDaze = false;
                _queued = OstrichAct.Idle;
                Begin(OstrichAct.Strut);
                NoteBlock(o.BlockSlot);
                PlaceHead(o.BlockSlot);
                return;
            }
            _wasAlive = true;
            if (o.Hits > _shownHits)
            {
                int hit = o.Hits;
                _shownHits = hit;
                if (hit >= OstrichState.HitsToFlee)
                {
                    _fleeAfterDaze = true;
                    Want(OstrichAct.Daze);
                }
                else if (hit >= 2)
                    Want(OstrichAct.Hit2);
                else
                    Want(OstrichAct.Hit1);
            }
            if (_shownPecks < 0) _shownPecks = o.PeckIndex;
            else if (o.PeckIndex > _shownPecks)
            {
                _shownPecks = o.PeckIndex;
                Want(OstrichAct.Peck);
            }
            if (o.BlockSlot != _block)
            {
                NoteBlock(o.BlockSlot);
                if (o.BlockSlot >= 0)
                    Want(OstrichAct.Shake);
            }
            PlaceHead(o.BlockSlot);
        }

        // Editor stills and the fight clip. The head rests on (x, y).
        public void Preview(OstrichAct act, int frame, float x, float y, bool calm)
        {
            Show();
            _gone = false;
            _act = act;
            _frame = frame;
            _calm = calm;
            _headX = x;
            _headY = y;
            _drawX = x;
            _drawY = y;
            _placed = true;
            Apply();
        }

        // Placeholder pose. Hits pick the damage frame. The point is the head.
        public void Pose(float x, float y, int hits, bool show)
        {
            if (!show)
            {
                Hide();
                return;
            }
            OstrichAct act = OstrichAct.Idle;
            int frame = 0;
            if (hits >= 3) { act = OstrichAct.Flee; frame = 8; }
            else if (hits == 2) { act = OstrichAct.Hit2; frame = 3; }
            else if (hits == 1) { act = OstrichAct.Hit1; frame = 3; }
            Preview(act, frame, x, y, false);
        }

        public void Tick(float dt)
        {
            if (dt < 0f) dt = 0f;
            if (_gone)
            {
                Hide();
                return;
            }
            if (!_placed)
            {
                _headY = WorldBuilder.FeederY;
                _drawX = _headX;
                _drawY = _headY;
                _placed = true;
            }
            if (!_calm && _queued != OstrichAct.Idle && _act == OstrichAct.Idle)
            {
                var next = _queued;
                _queued = OstrichAct.Idle;
                Begin(next);
            }
            float rate = _calm ? 7f : 12f;
            _frameT += dt * rate;
            int n = OstrichAnim.Count(_act);
            while (_frameT >= 1f)
            {
                _frameT -= 1f;
                _frame++;
                if (_frame >= n)
                {
                    _frame = 0;
                    Advance();
                    n = OstrichAnim.Count(_act);
                }
            }
            if (_calm && _act == OstrichAct.Idle && OstrichAnim.RuffleFrame(_frame))
                _frame = 2;
            if (!Settled)
            {
                _settle += dt;
                if (_settle > 0.35f) Settled = true;
            }
            float speed = _calm ? 1.4f : 6.5f;
            float step = speed * dt;
            if (_act == OstrichAct.Flee)
            {
                var flee = OstrichAnim.At(_act, _frame, false);
                _drawX = Mathf.Lerp(_headX, _headX + 5.4f, flee.Flee);
                _drawY = Mathf.Lerp(_headY, _headY + 0.35f, flee.Flee);
            }
            else
            {
                _drawX = Mathf.MoveTowards(_drawX, _headX, step);
                _drawY = Mathf.MoveTowards(_drawY, _headY, step);
            }
            ApplyAt(_drawX, _drawY);
        }

        void Update()
        {
            if (!Application.isPlaying) return;
            Tick(Time.deltaTime);
        }

        void PlaceHead(int slot)
        {
            int n = Feeders < 1 ? 2 : Feeders;
            float x = slot >= 0 ? FeederRow.X(slot, n) : 0f;
            _headX = x;
            _headY = WorldBuilder.FeederY - 0.05f;
            _placed = true;
        }

        void NoteBlock(int slot)
        {
            _block = slot;
        }

        void Want(OstrichAct act)
        {
            if (act == OstrichAct.Idle) return;
            if (_calm && act != OstrichAct.Daze && act != OstrichAct.Flee)
            {
                _queued = Ranked(_queued, act);
                return;
            }
            if (_act == OstrichAct.Idle)
                Begin(act);
            else
                _queued = Ranked(_queued, act);
        }

        static OstrichAct Ranked(OstrichAct a, OstrichAct b)
        {
            return Rank(b) >= Rank(a) ? b : a;
        }

        static int Rank(OstrichAct act)
        {
            switch (act)
            {
                case OstrichAct.Flee: return 6;
                case OstrichAct.Daze: return 5;
                case OstrichAct.Hit2: return 4;
                case OstrichAct.Hit1: return 3;
                case OstrichAct.Peck: return 2;
                case OstrichAct.Strut: return 1;
                case OstrichAct.Honk: return 1;
                case OstrichAct.Shake: return 1;
                default: return 0;
            }
        }

        void Begin(OstrichAct act)
        {
            _act = act;
            _frame = 0;
            _frameT = 0f;
            _gone = false;
            Show();
            if (act == OstrichAct.Honk && Application.isPlaying)
                SfxLibrary.Honk();
        }

        void Advance()
        {
            if (_returnChain)
            {
                if (_act == OstrichAct.Strut) { Begin(OstrichAct.Honk); return; }
                if (_act == OstrichAct.Honk) { Begin(OstrichAct.Shake); return; }
                _returnChain = false;
            }
            if (_fleeAfterDaze && _act == OstrichAct.Daze)
            {
                _fleeAfterDaze = false;
                Begin(OstrichAct.Flee);
                return;
            }
            if (_act == OstrichAct.Flee)
            {
                _gone = true;
                Hide();
                _act = OstrichAct.Idle;
                return;
            }
            if (_queued != OstrichAct.Idle && !_calm)
            {
                var next = _queued;
                _queued = OstrichAct.Idle;
                Begin(next);
                return;
            }
            _act = OstrichAct.Idle;
            _frame = 0;
        }

        void Apply()
        {
            ApplyAt(_headX, _headY);
        }

        void ApplyAt(float aimX, float aimY)
        {
            if (_body == null || _head == null) return;
            var sample = OstrichAnim.At(_act, _frame, _calm);
            bool flee = _act == OstrichAct.Flee;
            float headX = aimX + PortX;
            float headY = aimY + PortY + sample.Bob * 0.35f;
            headY += sample.Wind * 0.18f - sample.Strike * 0.12f;
            float sway = 0f;
            if (_act == OstrichAct.Strut) sway = Mathf.Sin(sample.Step * Mathf.PI * 2f) * 0.28f;
            if (_act == OstrichAct.Daze) sway += Mathf.Sin(sample.Step * Mathf.PI * 2f) * 0.36f;
            // Face the side the head reaches to; keep the whole body (tail end) on screen.
            float half = ViewHalfWidth();
            float face = flee ? 1f : (headX < -0.4f ? -1f : 1f);
            float bodyX;
            if (flee) bodyX = headX - 0.55f;
            else if (face > 0f) bodyX = Mathf.Clamp(headX - 0.9f, -half + BodyBack + 0.12f, half - BodyFront - 0.12f) + sway;
            else bodyX = Mathf.Clamp(headX + 0.9f, -half + BodyFront + 0.12f, half - BodyBack - 0.12f) + sway;
            float bodyY = headY - BodyDrop + sample.Bob;
            headX += face * (sample.Strike * 0.50f - sample.Wind * 0.42f);

            _bodySr.sprite = OstrichPaint.Body(_act, _frame);
            _bodySr.flipX = face < 0f;
            if (_collarSr != null) { _collarSr.flipX = face < 0f; }
            _body.position = new Vector3(bodyX, bodyY, 0f);
            _body.localRotation = Quaternion.Euler(0f, 0f, sample.Lean * face);
            // Stride from the timeline: legs swing in opposition around the hips.
            float stride = Mathf.Sin(sample.Step * Mathf.PI * 2f);
            float swing = flee ? 26f : (_act == OstrichAct.Strut || _act == OstrichAct.Daze ? 14f : 3f);
            float brace = -sample.Lean * 0.5f; // feet stay planted while the body leans
            PoseLeg(_legBack, _legBackSr, OstrichPaint.BackHip, brace + stride * swing, face);
            PoseLeg(_legFront, _legFrontSr, OstrichPaint.FrontHip, brace - stride * swing, face);

            var headPos = new Vector3(headX, headY, 0f);
            Head.sprite = OstrichPaint.Head(_act, _frame);
            Head.flipX = face < 0f;
            _head.position = headPos;
            _head.localRotation = Quaternion.Euler(0f, 0f, (sample.Tilt + sample.Shake) * face);

            // Start inside the plumage and end inside the chin, so the tube
            // reads as one neck in every pose.
            Vector3 neckA = _body.position + _body.rotation * new Vector3(-0.16f * face, -0.32f, 0f) * RigScale;
            Vector3 neckB = headPos + _head.rotation * new Vector3(0f, 0.10f * RigScale, 0f);
            Vector3 ctrl = (neckA + neckB) * 0.5f;
            ctrl.x += (-sample.Wind * 0.90f + sample.Shake * 0.006f) * face * RigScale;
            ctrl.y += (sample.Wind * 0.42f - sample.Strike * 0.60f) * RigScale;
            if (_act == OstrichAct.Daze) ctrl.y -= 0.72f * RigScale;
            if (_act == OstrichAct.Honk) ctrl.y += 0.16f;
            if (_act == OstrichAct.Idle) ctrl.x += 0.10f * face;
            WriteNeck(neckA, ctrl, neckB);
        }

        void EnsureLine()
        {
            if (_line != null) return;
            var go = new GameObject("NeckRibbon");
            go.transform.SetParent(transform, false);
            _line = go.AddComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.loop = false;
            _line.positionCount = NeckSamples;
            _line.numCapVertices = 1;
            _line.numCornerVertices = 5;
            _line.textureMode = LineTextureMode.Stretch;
            _line.alignment = LineAlignment.View;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.sortingOrder = NeckOrder;
            _line.widthMultiplier = 1f;
            var curve = new AnimationCurve(
                new Keyframe(0f, OstrichPaint.NeckWidth * 2.9f * RigScale),
                new Keyframe(0.30f, OstrichPaint.NeckWidth * 1.75f * RigScale),
                new Keyframe(1f, OstrichPaint.NeckWidth * 1.45f * RigScale));
            _line.widthCurve = curve;
            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                });
            _line.colorGradient = grad;
            var shader = Shader.Find("Unlit/Transparent");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            mat.mainTexture = OstrichPaint.NeckTexture();
            mat.color = Color.white;
            mat.hideFlags = HideFlags.HideAndDontSave;
            _line.material = mat;
        }

        void WriteNeck(Vector3 neckA, Vector3 ctrl, Vector3 neckB)
        {
            EnsureLine();
            _line.enabled = true;
            _line.sortingOrder = NeckOrder;
            for (int i = 0; i < NeckSamples; i++)
            {
                float t = i / (float)(NeckSamples - 1);
                _line.SetPosition(i, Bez(neckA, ctrl, neckB, t));
            }
        }

        static Vector3 Bez(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * b + t * t * c;
        }

        void Show()
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            if (_bodySr != null) _bodySr.enabled = true;
            if (_legBackSr != null) { _legBackSr.enabled = true; _legFrontSr.enabled = true; }
            if (_collarSr != null) _collarSr.enabled = true;
            if (Head != null) Head.enabled = true;
            if (_line != null) _line.enabled = true;
        }

        void Hide()
        {
            if (_bodySr != null) _bodySr.enabled = false;
            if (_legBackSr != null) { _legBackSr.enabled = false; _legFrontSr.enabled = false; }
            if (_collarSr != null) _collarSr.enabled = false;
            if (Head != null) Head.enabled = false;
            if (_line != null) _line.enabled = false;
            if (_neckSr == null) return;
            for (int i = 0; i < _neckSr.Length; i++)
                if (_neckSr[i] != null) _neckSr[i].enabled = false;
        }

        public bool OnStage => !_gone && Head != null && Head.enabled;
    }
}
