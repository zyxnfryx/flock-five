using UnityEngine;

namespace FlockFive
{
    // Painterly ostrich. The body stays centered in the background. The neck
    // reaches the feeder a match would have fed, and the head sits in front of
    // that feeder and behind the flock. Motion stays small while the player
    // is carrying birds.
    public sealed class OstrichMark : MonoBehaviour
    {
        // Build 110: the big body and the neck sit BEHIND the branch wood (order 2), the
        // feeders (8) and every bird (12). Only the head comes forward, over its feeder
        // and still under the flock. Background art (-20) and the life sheets (-12) stay below.
        public const int BodyOrder = -3;
        public const int NeckOrder = -2;
        public const int CollarOrder = -1;
        public const int HeadOrder = 10;
        public const int NeckParts = 8;
        // b115: at rest the head peeks out from BEHIND its feeder, beside the glass. The feeder
        // it peeks from is drawn one step above the head (FeederView.FrontOrder), so the ring and
        // glass occlude the neck; other feeders stay under the head. The head is offset toward the
        // way it faces so the face clears the glass instead of poking out of the bowl.
        public const float PeekX = 0.85f;
        public const float PeekY = 0.28f;
        public const float BeakReach = 1.45f * GrowFactor;
        public const float PeekSlotTol = 0.3f;
        // Strike: the head goes over the flock once it has dropped this far below its peek spot.
        public const float PeekDrop = 0.35f;
        // b115 "a little bigger": one tunable grows the whole bird (head, body, legs) about 15%.
        // The feet stay where they were (BodyDrop is derived below), so the ground under him
        // does not move and the extra size goes up into the body, not down toward the HUD.
        public const float GrowFactor = 1.15f;
        public const float BaseBodyScale = 1.70f;
        public const float BaseRigScale = 0.90f;
        public const float BaseBodyDrop = 3.20f;
        // Toe line below the body pivot (neck root), in body-sprite units at scale 1: hip drop plus
        // the leg sprite below its own pivot (ostrich_leg_back/front, 140 PPU, toes ~2px above the edge).
        public const float FootBelowPivot = 3.11f;
        // Whole bird: fits the screen width, legs included; the 160px master head stays near its native size.
        public const float RigScale = BaseRigScale * GrowFactor; // head
        // Build 110: the body is a large background figure (was 0.72, now 1.7 = 2.4x); b115 x GrowFactor.
        public const float BodyScale = BaseBodyScale * GrowFactor;
        // Big body sits below the feeder row, neck rises to the head. Pre-grow it was 3.20; growing
        // pulls the pivot up by the extra leg length so the toe line is unchanged.
        public const float BodyDrop = BaseBodyDrop - FootBelowPivot * (BodyScale - BaseBodyScale);
        // World y of his toe line for a feeder row at feederY (the head aims at feederY - 0.05).
        public static float GroundY(float feederY) => feederY - 0.05f + PortY - BodyDrop - FootBelowPivot * BodyScale;
        // World x of the middle of his planted feet for a body at bodyX facing side (+1/-1) with turn squash.
        public static float FootX(float bodyX, float side, float turn)
            => bodyX + (OstrichPaint.BackHip.x + OstrichPaint.FrontHip.x) * 0.5f * side * BodyScale * turn;
        // Half the visible width; the game camera when there is one, else the stills camera.
        public static float ViewHalfWidth()
        {
            var cam = Camera.main;
            if (cam != null && cam.orthographic) return cam.orthographicSize * cam.aspect;
            return 5.55f * 9f / 16f;
        }
        // Body width from the pivot (neck root) back to the tail, and in front of it, at RigScale.
        public static float BodyBack => 527.5f / OstrichPaint.BodyPpu * BodyScale;
        public static float BodyFront => (566f - 527.5f) / OstrichPaint.BodyPpu * BodyScale;
        // Feeder sprites pivot at the glass. The port is the flower on the tray.
        public const float PortX = 0.16f;
        public const float PortY = -0.88f;

        public int Feeders = 2;
        // Set by the garden builder. The ostrich raises the feeder it peeks from; null is fine.
        public FeederView[] FeederArt;
        public SpriteRenderer Head;
        // Slot of the feeder the head is hiding behind right now, or -1.
        public int PeekingSlot => _liftSlot;
        float _peek;
        int _liftSlot = -1;
        public bool Settled { get; private set; }

        public OstrichAct Act => _act;
        public int Frame => _frame;
        public bool Calm => _calm;
        public Transform Aim => _head != null ? _head : transform;

        Transform _body;
        Transform _head;
        Transform[] _neck;
        SpriteRenderer _bodySr;
        OstrichGround _ground;
        public OstrichGround Ground => _ground;
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
        // Smooth motion: the rig is driven every frame from eased, interpolated pose keys;
        // a new state blends in over BlendSec; the neck lags the body and the head lags the neck.
        public const float BlendSec = 0.2f;
        OstrichSample _cur, _from;
        OstrichAct _lastAct = OstrichAct.Idle;
        float _blend = 1f;
        float _dt;
        bool _snap = true;
        float _velX, _velY;
        Vector3 _headLag, _ctrlLag;
        float _tiltLag;
        float _faceS = 1f, _faceGoal = 1f, _faceVel;
        OstrichAct _driveAct = OstrichAct.Idle;
        public void Drive(OstrichAct act, float x, float y, float dt)
        {
            // Start each requested state once; its own chain (back to idle, daze -> flee) plays out.
            if (act != _driveAct) { _driveAct = act; if (act == OstrichAct.Daze) _fleeAfterDaze = false; Begin(act); }
            _headX = x; _headY = y; _placed = true;
            Tick(dt);
        }

        // Scripted strike (see OstrichStrike). The app starts one per landed peck: the head
        // winds up, lunges to the target bird's world position, holds, recoils and settles.
        bool _strikeOn;
        float _strikeT;
        Vector3 _strikeTarget;
        public bool Striking => _strikeOn;
        public float StrikeTime => _strikeT;

        public void BeginStrike(Vector3 target)
        {
            if (_gone) return;
            _strikeOn = true;
            _strikeT = 0f;
            _strikeTarget = target;
            _queued = OstrichAct.Idle;
            Begin(OstrichAct.Peck);
        }

        // The app plays this peck itself; Sync must not queue a second generic Peck for it.
        public void NotePecks(int peckIndex)
        {
            if (peckIndex > _shownPecks) _shownPecks = peckIndex;
        }

        void EndStrike()
        {
            _strikeOn = false;
            _strikeT = 0f;
            FlockSort.Apply(Head, HeadOrder);
            if (_line != null) _line.sortingOrder = NeckOrder;
        }

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
            FlockSort.Apply(_collarSr, CollarOrder);
            _body.localScale = new Vector3(BodyScale, BodyScale, 1f);
            _ground = OstrichGround.Attach(transform);

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
            // (legs are children of the body, which carries the turn squash)
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
                // The strike beat finishes before the ostrich takes its hit and leaves.
                if (_strikeOn) return;
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
            _frameT = 0f;
            _snap = true;
            _blend = 1f;
            _lastAct = act;
            _driveAct = act;
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
            _dt = dt;
            _blend = Mathf.Min(1f, _blend + dt / BlendSec);
            if (_strikeOn)
            {
                _strikeT += dt;
                if (_strikeT >= OstrichStrike.Total) EndStrike();
            }
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
            float rate = _calm && !_strikeOn ? 7f : 12f;
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
                var flee = SmoothAt(_act, _frame + _frameT, false);
                _drawX = Mathf.Lerp(_headX, _headX + 5.4f, flee.Flee);
                _drawY = Mathf.Lerp(_headY, _headY + 0.35f, flee.Flee);
            }
            else
            {
                // Eased reach (critically damped): starts soft, settles without a stop-pop.
                float st = _calm ? 0.34f : 0.16f;
                _drawX = Mathf.SmoothDamp(_drawX, _headX, ref _velX, st, speed, Mathf.Max(dt, 1e-4f));
                _drawY = Mathf.SmoothDamp(_drawY, _headY, ref _velY, st, speed, Mathf.Max(dt, 1e-4f));
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
            if (_act != _lastAct) { _from = _cur; _blend = 0f; _lastAct = _act; }
            var sample = SmoothAt(_act, _frame + _frameT, _calm);
            if (_blend < 1f && !_snap) sample = Mix(_from, sample, Ease(_blend));
            _cur = sample;
            bool flee = _act == OstrichAct.Flee;
            float headX = aimX + PortX;
            float headY = aimY + PortY + sample.Bob * 0.35f;
            headY += sample.Wind * 0.18f - sample.Strike * 0.12f;
            float sway = 0f;
            if (_act == OstrichAct.Strut) sway = Mathf.Sin(sample.Step * Mathf.PI * 2f) * 0.28f;
            if (_act == OstrichAct.Daze) sway += Mathf.Sin(sample.Step * Mathf.PI * 2f) * 0.36f;
            // Face the side the head reaches to; keep the whole body (tail end) on screen.
            float half = ViewHalfWidth();
            // Turning is eased, not snapped: face swings through 0 over ~0.25s (hysteresis on the side).
            if (flee || headX > -0.3f) _faceGoal = 1f;
            else if (headX < -0.5f) _faceGoal = -1f;
            if (_snap || _dt <= 0f) { _faceS = _faceGoal; _faceVel = 0f; }
            else _faceS = Mathf.SmoothDamp(_faceS, _faceGoal, ref _faceVel, 0.09f, 40f, _dt);
            float face = _faceS;
            float side = face < 0f ? -1f : 1f;
            float turn = Mathf.Max(Mathf.Abs(face), 0.18f);
            float bodyR = Mathf.Clamp(headX - 0.9f, -half + BodyBack + 0.12f, half - BodyFront - 0.12f);
            float bodyL = Mathf.Clamp(headX + 0.9f, -half + BodyFront + 0.12f, half - BodyBack - 0.12f);
            float bodyX;
            if (flee) bodyX = Mathf.Lerp(bodyL, headX - 0.55f, (face + 1f) * 0.5f);
            else bodyX = Mathf.Lerp(bodyL, bodyR, (face + 1f) * 0.5f) + sway;
            _body.localScale = new Vector3(BodyScale * turn, BodyScale, 1f);
            _head.localScale = new Vector3(RigScale * turn, RigScale, 1f);
            float bodyY = headY - BodyDrop + sample.Bob;
            headX += face * (sample.Strike * 0.50f - sample.Wind * 0.42f);

            _bodySr.sprite = OstrichPaint.Body(_act, _frame);
            _bodySr.flipX = side < 0f;
            if (_collarSr != null) { _collarSr.flipX = side < 0f; }
            _body.position = new Vector3(bodyX, bodyY, 0f);
            if (_ground != null) _ground.Place(FootX(bodyX, side, turn), GroundY(WorldBuilder.FeederY), turn);
            _body.localRotation = Quaternion.Euler(0f, 0f, sample.Lean * face);
            // Stride from the timeline: legs swing in opposition around the hips.
            float stride = Mathf.Sin(sample.Step * Mathf.PI * 2f);
            float swing = flee ? 26f : (_act == OstrichAct.Strut || _act == OstrichAct.Daze ? 14f : 3f);
            float brace = -sample.Lean * 0.5f; // feet stay planted while the body leans
            PoseLeg(_legBack, _legBackSr, OstrichPaint.BackHip, brace + stride * swing, side);
            PoseLeg(_legFront, _legFrontSr, OstrichPaint.FrontHip, brace - stride * swing, side);

            // b115: peek out from behind the feeder (not while fleeing or between feeders).
            int slot = flee ? -1 : PeekSlot(aimX, Feeders);
            float peekGoal = slot >= 0 ? 1f : 0f;
            if (_snap || _dt <= 0f) _peek = peekGoal;
            else _peek = Mathf.MoveTowards(_peek, peekGoal, _dt / 0.12f);
            headX += PeekShift(face, headX, half, _peek);
            headY += PeekY * _peek;
            var headPos = new Vector3(headX, headY, 0f);
            var restPos = headPos;
            Head.sprite = OstrichPaint.Head(_act, _frame);
            Head.flipX = side < 0f;
            // Follow-through: the head trails its target a touch, and its tilt trails the body.
            float tilt = (sample.Tilt + sample.Shake) * face;
            if (_snap || _dt <= 0f) { _headLag = headPos; _tiltLag = tilt; }
            else
            {
                _headLag = Vector3.Lerp(_headLag, headPos, 1f - Mathf.Exp(-_dt * 18f));
                _tiltLag = Mathf.LerpAngle(_tiltLag, tilt, 1f - Mathf.Exp(-_dt * 14f));
            }
            headPos = _headLag;
            if (_strikeOn)
            {
                // The strike owns the head: no lag, so the lunge reads as a lunge.
                headPos = OstrichStrike.HeadAt(restPos, _strikeTarget, _strikeT);
                _headLag = headPos;
            }
            // The feeder rises over the head only once the head has settled beside it, so the
            // order flip never happens while the face overlaps the glass.
            LiftFeeder(slot >= 0 && _peek >= 0.95f ? slot : -1);
            if (_strikeOn)
                FlockSort.Apply(Head, _liftSlot >= 0
                    ? RestHeadOrder(true, restPos.y - headPos.y) : OstrichStrike.HeadOrder);
            _head.position = headPos;
            _head.localRotation = Quaternion.Euler(0f, 0f, _tiltLag);

            // Start inside the plumage and end inside the chin, so the tube
            // reads as one neck in every pose.
            Vector3 neckA = _body.position + _body.rotation * new Vector3(-0.16f * face, -0.32f, 0f) * BodyScale;
            Vector3 neckB = headPos + _head.rotation * new Vector3(0f, 0.10f * RigScale, 0f);
            Vector3 ctrl = (neckA + neckB) * 0.5f;
            ctrl.x += (-sample.Wind * 0.90f + sample.Shake * 0.006f) * face * RigScale;
            ctrl.y += (sample.Wind * 0.42f - sample.Strike * 0.60f) * RigScale;
            if (_act == OstrichAct.Daze) ctrl.y -= 0.72f * RigScale;
            if (_act == OstrichAct.Honk) ctrl.y += 0.16f;
            if (_act == OstrichAct.Idle) ctrl.x += 0.10f * face;
            // The neck's bend lags the body: it catches up over ~0.1s.
            if (_snap || _dt <= 0f) _ctrlLag = ctrl;
            else _ctrlLag = Vector3.Lerp(_ctrlLag, ctrl, 1f - Mathf.Exp(-_dt * 11f));
            _snap = false;
            WriteNeck(neckA, _ctrlLag, neckB);
        }

        // Which feeder the head rests on: the one whose x matches the aim, else -1 (head between
        // feeders, as in a pair with no block slot: it stays in front like before).
        public static int PeekSlot(float aimX, int feeders)
        {
            int n = FeederRow.Clamp(feeders);
            int best = -1;
            float bd = PeekSlotTol;
            for (int i = 0; i < n; i++)
            {
                float d = Mathf.Abs(FeederRow.X(i, n) - aimX);
                if (d <= bd) { bd = d; best = i; }
            }
            return best;
        }

        // How far the peeking head sits sideways of its feeder (toward the facing), clamped so the
        // beak stays on screen. face is the eased facing (-1..1); half is the view half width.
        public static float PeekShift(float face, float headX, float half, float weight)
        {
            if (weight <= 0f) return 0f;
            float dir = face < 0f ? -1f : 1f;
            float room = dir > 0f ? half - 0.12f - BeakReach - headX : headX + half - 0.12f - BeakReach;
            float dx = Mathf.Clamp(PeekX, 0f, Mathf.Max(0f, room));
            return dir * dx * weight * Mathf.Min(1f, Mathf.Abs(face) * 2f);
        }

        // Head order while peeking. Behind the lifted feeder's order, so ring and glass cover it.
        public static int RestHeadOrder(bool striking, float dropBelowPeek)
        {
            return striking && dropBelowPeek > PeekDrop ? OstrichStrike.HeadOrder : HeadOrder;
        }

        void LiftFeeder(int slot)
        {
            _liftSlot = slot;
            if (FeederArt == null) return;
            for (int i = 0; i < FeederArt.Length; i++)
                if (FeederArt[i] != null) FeederArt[i].SetFront(i == slot);
        }

        static float Ease(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

        // Pose keys interpolated with an ease between frames (no stepped poses).
        public static OstrichSample SmoothAt(OstrichAct act, float f, bool calm)
        {
            int n = OstrichAnim.Count(act);
            if (f < 0f) f = 0f;
            int i = Mathf.Min((int)f, n - 1);
            int j = Mathf.Min(i + 1, n - 1);
            var a = OstrichAnim.At(act, i, calm);
            if (j == i) return a;
            return Mix(a, OstrichAnim.At(act, j, calm), Ease(f - i));
        }

        public static OstrichSample Mix(OstrichSample a, OstrichSample b, float t)
        {
            var s = new OstrichSample();
            s.Bob = Mathf.Lerp(a.Bob, b.Bob, t);
            s.Tilt = Mathf.Lerp(a.Tilt, b.Tilt, t);
            s.Lean = Mathf.Lerp(a.Lean, b.Lean, t);
            s.Shake = Mathf.Lerp(a.Shake, b.Shake, t);
            s.Wind = Mathf.Lerp(a.Wind, b.Wind, t);
            s.Strike = Mathf.Lerp(a.Strike, b.Strike, t);
            s.Blink = t < 0.5f ? a.Blink : b.Blink;
            s.Ruffle = Mathf.Lerp(a.Ruffle, b.Ruffle, t);
            s.Beak = Mathf.Lerp(a.Beak, b.Beak, t);
            s.Step = Mathf.Lerp(a.Step, b.Step, t);
            s.Spread = Mathf.Lerp(a.Spread, b.Spread, t);
            s.Damage = t < 0.5f ? a.Damage : b.Damage;
            s.Flee = Mathf.Lerp(a.Flee, b.Flee, t);
            s.Dip = Mathf.Lerp(a.Dip, b.Dip, t);
            return s;
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
                new Keyframe(0f, OstrichPaint.NeckWidth * 2.9f * BodyScale),
                new Keyframe(0.30f, OstrichPaint.NeckWidth * 2.45f * (BodyScale + RigScale) * 0.5f),
                new Keyframe(1f, OstrichPaint.NeckWidth * 2.30f * RigScale));
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
            _line.sortingOrder = _strikeOn && Mathf.Abs(OstrichStrike.Reach(_strikeT)) > 0.02f
                ? OstrichStrike.NeckOrder : NeckOrder;
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
            if (_ground != null) _ground.Show(true);
            if (Head != null) Head.enabled = true;
            if (_line != null) _line.enabled = true;
        }

        void Hide()
        {
            if (_liftSlot >= 0) LiftFeeder(-1);
            if (_bodySr != null) _bodySr.enabled = false;
            if (_legBackSr != null) { _legBackSr.enabled = false; _legFrontSr.enabled = false; }
            if (_collarSr != null) _collarSr.enabled = false;
            if (_ground != null) _ground.Show(false);
            if (Head != null) Head.enabled = false;
            if (_line != null) _line.enabled = false;
            if (_neckSr == null) return;
            for (int i = 0; i < _neckSr.Length; i++)
                if (_neckSr[i] != null) _neckSr[i].enabled = false;
        }

        public bool OnStage => !_gone && Head != null && Head.enabled;
    }
}
