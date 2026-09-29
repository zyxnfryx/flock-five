using UnityEngine;

namespace FlockFive
{
    // First garden: every hint shares one thick black edge, a soft glow under the
    // branch the step wants, and a gloved hand that taps it. Other taps wait.
    public sealed partial class FlockFiveApp
    {
        const string CoachKey = "flockfive.coach.done";
        const string CoachGiftKey = "flockfive.coach.gift";
        // One outline for every coach line. StampCoach is the only draw path.
        const int CoachOutlinePx = 6;
        // Hover, quick press, ease-out lift, short pause. 0.72+0.12+0.20+0.16 = 1.20s.
        const float TapHover = 0.72f;
        const float TapPress = 0.12f;
        const float TapLift = 0.20f;
        const float TapPause = 0.16f;
        const float TapCycle = TapHover + TapPress + TapLift + TapPause;
        const int RipplePool = 4;
        const float RippleLife = 0.46f;
        // Wing-up opaque extent of the 1536 flap sheet at 220 ppu, BirdScale 0.42,
        // mood scale ≤ 1.07, plus the selection rim. The hop span is added on top.
        const float LiftHead = 1.62f;
        const float LiftFoot = 0.95f;
        const float LiftHalfW = 1.28f;

        bool _coach;
        int _coachMoves;
        int _coachFrom = -1, _coachTo = -1;
        float _coachFade;
        float _coachGiftUntil = -1f;

        bool _cueHand;
        bool _cueForce;
        bool _cueFreeze;
        bool _cueGift;
        int _cueBranch = -1;
        string _cueLine;
        Vector2 _cueAimGui;
        Vector2 _cueHoleGui;
        float _cueHolePx;

        bool _gloveVis;
        bool _gloveReady;
        Vector2 _gloveTip;
        Vector2 _gloveVel;
        Vector2 _gloveAway;
        float _gloveAng;
        float _gloveAngVel;
        float _gloveWiggle;
        float _gloveShownAng;
        Vector2 _gloveShown;
        Vector2 _gloveRest;
        float _gloveRestAng;
        float _glovePhase;
        float _gloveDip;
        bool _tapSent;
        float _coachGlowKick;
        Vector3 _cueAimWorld;
        SpriteRenderer _coachGlow;
        SpriteRenderer[] _ripples;
        float[] _rippleAge;
        bool _coachLineHeld;
        float _coachLineHold;
        string _coachLineFor;
        ScreenBox[] _blocks;
        int _blockN;

        struct ScreenBox
        {
            public float X0, Y0, X1, Y1;
        }

        void CoachBegin(int index)
        {
            _coach = index == 0 && PlayerPrefs.GetInt(CoachKey, 0) == 0;
            _coachMoves = 0;
            _coachFade = 0f;
            _coachGiftUntil = -1f;
            _coachFrom = -1;
            _coachTo = -1;
            _gloveReady = false;
            _gloveWiggle = 0f;
            _glovePhase = 0f;
            _gloveDip = 0f;
            _tapSent = false;
            _coachGlowKick = 0f;
            _coachLineHeld = false;
            _cueHand = false;
            _cueForce = false;
            _cueFreeze = false;
            _cueGift = false;
            _cueLine = null;
            _cueBranch = -1;
            _gloveVis = false;
            if (!_coach) CoachRelease();
            else
            {
                CoachHideGlow();
                CoachHideRipples();
            }
        }

        // Called after every successful move.
        void CoachMoved(int from, int to, int collects)
        {
            if (!_coach) return;
            bool ownIdea = _coachFrom >= 0 && (from != _coachFrom || to != _coachTo);
            _coachMoves++;
            if (ownIdea || collects > 0 || _coachMoves >= 2) CoachDone();
        }

        void CoachDone()
        {
            if (!_coach) return;
            _coach = false;
            PlayerPrefs.SetInt(CoachKey, 1);
            if (PlayerPrefs.GetInt(CoachGiftKey, 0) == 0 && GiftBranch() >= 0)
            {
                _coachGiftUntil = Time.unscaledTime + 4.5f;
                PlayerPrefs.SetInt(CoachGiftKey, 1);
            }
            PlayerPrefs.Save();
        }

        int GiftBranch()
        {
            if (_board == null) return -1;
            for (int i = 0; i < _board.Branches.Count; i++)
                if (_board.Branches[i].AdLocked) return i;
            return -1;
        }

        bool CoachPick(out int from, out int to)
        {
            from = -1; to = -1;
            int fallFrom = -1, fallTo = -1;
            for (int a = 0; a < _board.Branches.Count; a++)
            {
                if (!_board.CanPick(a) || Locked(a) || GiftLocked(a)) continue;
                for (int b = 0; b < _board.Branches.Count; b++)
                {
                    if (b == a || Locked(b) || GiftLocked(b)) continue;
                    if (!_board.CanMove(a, b, out _)) continue;
                    // Prefer joining a matching color over dropping on an empty branch.
                    if (!_board.Branches[b].Empty) { from = a; to = b; return true; }
                    if (fallFrom < 0) { fallFrom = a; fallTo = b; }
                }
            }
            from = fallFrom; to = fallTo;
            return from >= 0;
        }

        void CoachClearCue()
        {
            _cueHand = false;
            _cueForce = false;
            _cueFreeze = false;
            _cueGift = false;
            _cueLine = null;
            _cueBranch = -1;
            _gloveVis = false;
        }

        // Same-frame hide. SnapRound and the splash set this before the glove draws.
        void CoachHideNow()
        {
            _coachFade = 0f;
            _gloveReady = false;
            _gloveWiggle = 0f;
            _glovePhase = 0f;
            _gloveDip = 0f;
            _tapSent = false;
            _coachGlowKick = 0f;
            _coachLineHeld = false;
            CoachClearCue();
            CoachHideGlow();
            CoachHideRipples();
        }

        void CoachRelease()
        {
            CoachHideNow();
            if (_coachGlow != null)
            {
                Destroy(_coachGlow.gameObject);
                _coachGlow = null;
            }
            CoachDestroyRipples();
        }

        void CoachHideGlow()
        {
            if (_coachGlow != null) _coachGlow.enabled = false;
        }

        void CoachNudge()
        {
            _gloveWiggle = 1f;
        }

        // Which branch this frame's hint is about. Does not move _coachFrom while a hop
        // is in flight — CoachMoved still has to recognize the move that just landed.
        void CoachAdvance()
        {
            if (_gloveWiggle > 0f)
                _gloveWiggle = Mathf.Max(0f, _gloveWiggle - Time.unscaledDeltaTime / 0.28f);

            if (_splash || _board == null || _garden.Cam == null)
            {
                CoachRelease();
                return;
            }
            if (_restarting || _won || _gift != GiftFace.None)
            {
                CoachHideNow();
                return;
            }
            if (!_coach && Time.unscaledTime >= _coachGiftUntil)
            {
                CoachRelease();
                return;
            }

            CoachClearCue();
            if (Time.unscaledTime < _coachGiftUntil)
            {
                int g = GiftBranch();
                if (g < 0)
                {
                    CoachHideGlow();
                    _gloveReady = false;
                    _coachFade = 0f;
                    return;
                }
                float left = (_coachGiftUntil - Time.unscaledTime) / 0.5f;
                _coachFade = Mathf.Clamp01(Mathf.Min(1f, left));
                _cueHand = true;
                _cueForce = true;
                _cueGift = true;
                _cueBranch = g;
                _cueLine = "Stuck? Tap the gift branch for a bonus spot.";
                return;
            }

            if (_locked.Count > 0)
            {
                // The second guided hop finishes the lesson. Keep the stage clear while it plays.
                if (_coachMoves >= 1)
                {
                    CoachHideGlow();
                    _gloveReady = false;
                    return;
                }
                if (CoachPick(out int next, out _))
                {
                    _cueHand = true;
                    _cueFreeze = true;
                    _cueBranch = next;
                }
                else CoachHideGlow();
                return;
            }

            _coachFade = Mathf.Min(1f, _coachFade + Time.unscaledDeltaTime / 0.35f);
            if (_sel < 0)
            {
                if (!CoachPick(out _coachFrom, out _coachTo))
                {
                    CoachHideGlow();
                    _gloveReady = false;
                    return;
                }
                _cueHand = true;
                _cueForce = true;
                _cueBranch = _coachFrom;
                _cueLine = "Tap a branch to pick up its top birds.";
                return;
            }

            int dest = -1;
            if (_sel == _coachFrom && _coachTo >= 0 && _board.CanMove(_sel, _coachTo, out _))
                dest = _coachTo;
            else
                for (int b = 0; b < _board.Branches.Count && dest < 0; b++)
                    if (b != _sel && !Locked(b) && !GiftLocked(b) && _board.CanMove(_sel, b, out _)) dest = b;
            if (dest < 0)
            {
                _cueLine = "Those birds have nowhere to go yet. Try another branch.";
                CoachHideGlow();
                _gloveReady = false;
                return;
            }
            _coachFrom = _sel;
            _coachTo = dest;
            _cueHand = true;
            _cueForce = true;
            _cueBranch = dest;
            _cueLine = "Now tap a branch with the same color on top, or an empty one.";
        }

        bool CoachReject(Vector2 world)
        {
            if (_cueFreeze)
            {
                CoachNudge();
                return true;
            }
            if (!_cueForce || _cueBranch < 0) return false;
            if (_cueGift)
            {
                bool sign = HitGiftSign(world) == _cueBranch;
                bool near = !sign && NearGift(_cueBranch, world);
                if (!sign && !near)
                {
                    CoachNudge();
                    return true;
                }
                if (!CanOfferBonus(_cueBranch)) return true;
                if (near) OpenBonus(_cueBranch);
                CoachHideNow();
                return near;
            }
            if (HitBranch(world) == _cueBranch) return false;
            CoachNudge();
            return true;
        }

        bool NearGift(int i, Vector2 world)
        {
            if (_garden.Branches == null || (uint)i >= (uint)_garden.Branches.Length) return false;
            var v = _garden.Branches[i];
            if (v == null) return false;
            float d = v.NearestPadSqr(world);
            if (v.Sign != null && v.Sign.gameObject.activeInHierarchy)
            {
                float ds = ((Vector2)v.Sign.position - world).sqrMagnitude;
                if (ds < d) d = ds;
            }
            if (d > 2.1f * 2.1f) return false;
            for (int b = 0; b < _garden.Branches.Length; b++)
            {
                if (b == i || _garden.Branches[b] == null) continue;
                if (_garden.Branches[b].NearestPadSqr(world) < d) return false;
            }
            return true;
        }

        bool CoachView(out BranchView view)
        {
            view = null;
            if (_garden.Branches == null || _board == null) return false;
            if ((uint)_cueBranch >= (uint)_garden.Branches.Length) return false;
            if ((uint)_cueBranch >= (uint)_board.Branches.Count) return false;
            view = _garden.Branches[_cueBranch];
            return view != null;
        }

        void CoachFocus(BranchView view, out Vector3 aim, out Vector3 glow, out float dx, out float dy)
        {
            Vector3 wood = view.transform.position;
            Vector3 focus = wood + Vector3.up * 0.4f;
            var st = _board.Branches[_cueBranch];
            int run = st.TipRun();
            if (!_cueGift && st.Count > 0 && run > 0)
            {
                int tip = st.Count - 1;
                int a = tip - run + 1;
                Vector3 acc = Vector3.zero;
                int n = 0;
                for (int s = a; s <= tip; s++)
                {
                    if (view.Seats[s] == null) continue;
                    acc += view.SeatWorld(s);
                    n++;
                }
                if (n > 0) focus = acc / n + Vector3.up * 0.42f;
            }
            else if (_cueGift && view.Sign != null && view.Sign.gameObject.activeInHierarchy)
                focus = view.Sign.position;

            float minX = Mathf.Min(wood.x, focus.x);
            float maxX = Mathf.Max(wood.x, focus.x);
            float minY = Mathf.Min(wood.y, focus.y);
            float maxY = Mathf.Max(wood.y, focus.y);
            if (_cueGift && view.Sign != null && view.Sign.gameObject.activeInHierarchy)
            {
                var sp = view.Sign.position;
                minX = Mathf.Min(minX, sp.x);
                maxX = Mathf.Max(maxX, sp.x);
                minY = Mathf.Min(minY, sp.y);
                maxY = Mathf.Max(maxY, sp.y);
            }
            dx = Mathf.Max(2.8f, (maxX - minX) + 2.2f);
            dy = Mathf.Max(2.3f, (maxY - minY) + 2.0f);
            aim = focus;
            glow = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
        }

        void CoachEnsureGlow()
        {
            if (_coachGlow != null) return;
            var go = new GameObject("CoachGlow");
            go.hideFlags = HideFlags.HideAndDontSave;
            go.transform.SetParent(transform, false);
            _coachGlow = go.AddComponent<SpriteRenderer>();
            _coachGlow.sprite = CoachGlowSprite();
            _coachGlow.sortingOrder = 0;
            _coachGlow.enabled = false;
        }

        void CoachPlace()
        {
            float dt = Time.unscaledDeltaTime;
            if (_coachGlowKick > 0f)
                _coachGlowKick = Mathf.Max(0f, _coachGlowKick - dt / 0.24f);
            CoachTickRipples(dt);
            if (_levelHive || !_cueHand || !CoachView(out var view) || _garden.Cam == null)
            {
                _gloveVis = false;
                CoachHideGlow();
                return;
            }

            CoachFocus(view, out var aim, out var glow, out float dx, out float dy);
            var cam = _garden.Cam;
            float span = cam.orthographicSize * 2f;
            float ppu = span > 0.01f ? Screen.height / span : 60f;
            _cueHolePx = 0.5f * Mathf.Sqrt(dx * dx + dy * dy) * ppu * 1.06f;
            _cueAimWorld = aim;
            var screen = cam.WorldToScreenPoint(aim);
            _cueAimGui = new Vector2(screen.x, Screen.height - screen.y);
            var holeScreen = cam.WorldToScreenPoint(glow);
            _cueHoleGui = new Vector2(holeScreen.x, Screen.height - holeScreen.y);

            CoachEnsureGlow();
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (Mathf.PI * 2f / 1.35f));
            float breathe = 0.94f + 0.08f * pulse;
            // Quad is larger than the limb so the soft falloff, not the hard edge, covers it.
            const float spread = 1.75f;
            float kick = _coachGlowKick;
            float pop = 1f + 0.12f * kick;
            _coachGlow.enabled = true;
            _coachGlow.transform.position = glow;
            _coachGlow.transform.localScale = new Vector3(dx * breathe * spread * pop, dy * breathe * spread * pop, 1f);
            float a = (0.40f + 0.28f * pulse + 0.62f * kick) * _coachFade;
            if (a > 1f) a = 1f;
            _coachGlow.color = new Color(1f, 0.91f, 0.46f, a);

            float s = Mathf.Max(Screen.height / 720f, 1f);
            float margin = 130f * s;
            CoachAimAway(s, margin, out var away, out float gap);
            var rest = _cueAimGui + away * gap;
            float ang = Mathf.Atan2(-away.x, away.y) * Mathf.Rad2Deg;
            _gloveRest = rest;
            _gloveRestAng = ang;
            if (!_gloveReady)
            {
                _gloveTip = rest;
                _gloveVel = Vector2.zero;
                _gloveAway = away;
                _gloveAng = ang;
                _gloveAngVel = 0f;
                _glovePhase = 0f;
                _gloveDip = 0f;
                _tapSent = false;
                _gloveReady = true;
            }
            else
            {
                _gloveTip = Vector2.SmoothDamp(_gloveTip, rest, ref _gloveVel, 0.36f, Mathf.Infinity, dt);
                _gloveAway = Vector2.Lerp(_gloveAway, away, 1f - Mathf.Exp(-dt / 0.28f));
                if (_gloveAway.sqrMagnitude > 0.0001f) _gloveAway.Normalize();
                _gloveAng = Mathf.SmoothDampAngle(_gloveAng, ang, ref _gloveAngVel, 0.36f, Mathf.Infinity, dt);
            }

            float pressAt = TapHover + TapPress;
            float prevPhase = _glovePhase;
            float nextPhase = prevPhase + dt;
            bool fire = _coachFade > 0.25f && !_tapSent && prevPhase < pressAt && nextPhase >= pressAt;
            if (nextPhase >= TapCycle)
            {
                nextPhase -= TapCycle;
                if (nextPhase < 0f) nextPhase = 0f;
                _tapSent = false;
            }
            _glovePhase = nextPhase;
            _gloveDip = TapDip(_glovePhase);
            if (fire)
            {
                _tapSent = true;
                _coachGlowKick = 1f;
                CoachSpawnRipple(_cueAimWorld);
                float flash = (0.40f + 0.28f * pulse + 0.62f) * _coachFade;
                if (flash > 1f) flash = 1f;
                _coachGlow.transform.localScale = new Vector3(dx * breathe * spread * 1.12f, dy * breathe * spread * 1.12f, 1f);
                _coachGlow.color = new Color(1f, 0.91f, 0.46f, flash);
            }

            // Bob only while hovering. The press itself is the dip toward the aim.
            float bob = _glovePhase < TapHover
                ? Mathf.Sin(Time.unscaledTime * (Mathf.PI * 2f / 1.15f)) * (8f * s)
                : 0f;
            float wig = Mathf.Sin(Time.unscaledTime * 46f) * 8f * _gloveWiggle;
            var side = new Vector2(-_gloveAway.y, _gloveAway.x);
            var hover = _gloveTip + _gloveAway * bob + side * wig;
            _gloveShown = Vector2.Lerp(hover, _cueAimGui, _gloveDip);
            float nod = 11f * _gloveDip * (_gloveAway.x >= 0f ? -1f : 1f);
            _gloveShownAng = _gloveAng + Mathf.Sin(Time.unscaledTime * 46f) * 7f * _gloveWiggle + nod;
            _gloveVis = true;
        }

        // Hand sits off the lifted flock so the glove and those birds do not share a spot.
        // Falls back to the side that stays on screen when nothing is lifted.
        void CoachAimAway(float s, float margin, out Vector2 away, out float gap)
        {
            gap = 46f * s;
            float hx = _cueAimGui.x < Screen.width * 0.5f ? 1f : -1f;
            float hy = 1.05f;
            if (_cueAimGui.y > Screen.height - margin) hy = -1.15f;
            else if (_cueAimGui.y < margin * 0.65f) hy = 1.15f;
            float sx = hx;
            float sy = hy;
            if (LiftedGuiCenter(out var flock))
            {
                float dx = _cueAimGui.x - flock.x;
                float dy = _cueAimGui.y - flock.y;
                if (dx * dx + dy * dy > 36f * 36f)
                {
                    if (Mathf.Abs(dx) > 24f) sx = dx >= 0f ? 1f : -1f;
                    if (Mathf.Abs(dy) > 24f) sy = dy >= 0f ? 1.15f : -1.15f;
                }
            }
            float bestHx = sx, bestHy = sy, bestGap = gap;
            bool found = false;
            for (int g = 0; g < 3 && !found; g++)
            {
                float tryGap = (46f + 28f * g) * s;
                for (int i = 0; i < 4 && !found; i++)
                {
                    float cx = i == 3 ? hx : sx;
                    float cy = i == 1 ? 1.15f : i == 2 ? -1.15f : i == 3 ? hy : sy;
                    if (_cueAimGui.y < margin * 0.65f && cy < 0f) continue;
                    if (_cueAimGui.y > Screen.height - margin && cy > 0f) continue;
                    var dir = new Vector2(cx, cy).normalized;
                    float ang = Mathf.Atan2(-dir.x, dir.y) * Mathf.Rad2Deg;
                    var rest = _cueAimGui + dir * tryGap;
                    if (rest.x < 8f || rest.y < 8f || rest.x > Screen.width - 8f || rest.y > Screen.height - 8f) continue;
                    if (GloveHitsBirds(rest, ang, s) || GloveHitsBirds(_cueAimGui, ang, s)) continue;
                    bestHx = cx;
                    bestHy = cy;
                    bestGap = tryGap;
                    found = true;
                }
            }
            if (!found)
            {
                if (_cueAimGui.y < margin * 0.65f && bestHy < 0f) bestHy = 1.15f;
                if (_cueAimGui.y > Screen.height - margin && bestHy > 0f) bestHy = -1.15f;
            }
            gap = bestGap;
            away = new Vector2(bestHx, bestHy).normalized;
        }

        bool LiftedGuiCenter(out Vector2 gui)
        {
            gui = default;
            var cam = _garden.Cam;
            var branches = _garden.Branches;
            if (cam == null || branches == null) return false;
            float sx = 0f, sy = 0f;
            int n = 0;
            for (int b = 0; b < branches.Length; b++)
            {
                var view = branches[b];
                if (view == null) continue;
                var birds = view.Birds;
                for (int i = 0; i < birds.Length; i++)
                {
                    var sr = birds[i];
                    if (sr == null || !sr.enabled || !sr.gameObject.activeInHierarchy) continue;
                    var idle = sr.GetComponent<BirdIdle>();
                    if (idle == null || idle.Shrouded || idle.Lift < 0.9f) continue;
                    var p = view.transform.TransformPoint(new Vector3(
                        idle.RestLocal.x, idle.RestLocal.y + idle.Lift, idle.RestLocal.z));
                    var sp = cam.WorldToScreenPoint(p);
                    if (sp.z < 0f) continue;
                    sx += sp.x;
                    sy += Screen.height - sp.y;
                    n++;
                }
            }
            if (n == 0) return false;
            gui = new Vector2(sx / n, sy / n);
            return true;
        }

        bool GloveHitsBirds(Vector2 pivot, float ang, float s)
        {
            var tex = CoachGloveTex();
            if (tex == null) return false;
            float dh = 118f * s;
            float dw = dh * (tex.width / (float)Mathf.Max(1, tex.height));
            float left = pivot.x - _gloveTipU * dw;
            float top = pivot.y - (1f - _gloveTipV) * dh;
            if (!RotAabb(left, top, dw, dh, pivot, ang, 8f * s, out var box)) return false;
            return CoachBirdsBlock(box, 0f);
        }

        // 0 at rest, 1 at the bottom of the press. The lift eases out (fast off the target, then settles).
        static float TapDip(float phase)
        {
            if (phase < TapHover) return 0f;
            if (phase < TapHover + TapPress)
            {
                float u = (phase - TapHover) / TapPress;
                return u * u;
            }
            if (phase < TapHover + TapPress + TapLift)
            {
                float u = (phase - TapHover - TapPress) / TapLift;
                float back = 1f - u;
                return back * back;
            }
            return 0f;
        }

        void CoachDim()
        {
            if (_levelHive || !_cueHand || _coachFade < 0.03f || _cueHolePx < 8f) return;
            var tex = CoachDimTex();
            if (tex == null) return;
            // One radial veil. Clear until 0.28 of the half-extent, solid by 0.62,
            // including the texture corners, so the garden outside the limb falls off
            // without a second rectangle to seam against.
            var c = _cueHoleGui;
            float sw = Screen.width;
            float sh = Screen.height;
            float reach = Mathf.Max(
                Mathf.Max(c.magnitude, Vector2.Distance(c, new Vector2(sw, 0f))),
                Mathf.Max(Vector2.Distance(c, new Vector2(0f, sh)), Vector2.Distance(c, new Vector2(sw, sh))));
            float half = Mathf.Max(_cueHolePx / 0.28f, reach / 0.62f);
            float side = half * 2f;
            GUI.color = new Color(0f, 0f, 0f, 0.34f * _coachFade);
            GUI.DrawTexture(new Rect(c.x - half, c.y - half, side, side), tex, ScaleMode.StretchToFill, true);
            GUI.color = Color.white;
        }

        static GUIStyle _coachLine;
        static GUIContent _coachContent;
        static string _coachSizedFor;
        static float _coachSizedW;
        static float _coachSizedH;
        static int _coachSizedPx;

        // Cream fill, thick black edge. Every step sets _cueLine and draws through here.
        static GUIStyle CoachLineStyle()
        {
            if (_coachLine != null) return _coachLine;
            _coachLine = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            return _coachLine;
        }

        void DrawCoachLine(string text, float s, float top)
        {
            var st = CoachLineStyle();
            if (_coachContent == null) _coachContent = new GUIContent();
            float w = Screen.width * 0.86f;
            float h = 68f * s;
            float y = CoachLineY(text, s, top, w, h);
            var r = new Rect((Screen.width - w) * 0.5f, y, w, h);
            if (_coachSizedFor != text || Mathf.Abs(_coachSizedW - r.width) > 1f || Mathf.Abs(_coachSizedH - r.height) > 1f)
            {
                _coachContent.text = text;
                _coachSizedPx = FitFontWrapped(st, text, r.width, r.height, 14, Mathf.RoundToInt(22f * s));
                _coachSizedFor = text;
                _coachSizedW = r.width;
                _coachSizedH = r.height;
            }
            st.fontSize = _coachSizedPx;
            int black = Mathf.Clamp(Mathf.CeilToInt(CoachOutlinePx * s), CoachOutlinePx, 8);
            StampOutlined(r, text, st, new Color(1f, 0.96f, 0.82f, _coachFade), 0, black);
        }

        // Highest band that clears the glove's tap sweep and the lifted birds' hop.
        // The usual spot is just under the top inset; when a high branch's birds rise
        // into it, the next open band (often below that hop, clear of the glove) is used.
        float CoachLineY(string text, float s, float hudTop, float w, float h)
        {
            float x = (Screen.width - w) * 0.5f;
            float minY = hudTop + 8f * s;
            HudLayout(out _, out _, out float bot, out _, out _);
            float maxY = Screen.height - bot - h - 36f * s;
            if (maxY < minY) return minY;
            CoachFillBlocks(s);
            if (_coachLineFor != text)
            {
                _coachLineFor = text;
                _coachLineHeld = false;
            }
            var probe = new Rect(x, minY, w, h);
            if (!BlocksHit(probe)) return minY;
            if (_coachLineHeld)
            {
                probe.y = _coachLineHold;
                if (probe.y >= minY && probe.y <= maxY && !BlocksHit(probe))
                    return _coachLineHold;
            }
            for (float y = minY; y <= maxY; y += 4f)
            {
                probe.y = y;
                if (BlocksHit(probe)) continue;
                _coachLineHold = y;
                _coachLineHeld = true;
                return y;
            }
            _coachLineHeld = false;
            return minY;
        }

        // Glove sweep plus each lifted bird, once per line placement. The scan below only
        // tests this list, so it does not walk the flock on every candidate row.
        void CoachFillBlocks(float s)
        {
            if (_blocks == null) _blocks = new ScreenBox[16];
            _blockN = 0;
            float pad = 12f * s;
            if (_gloveVis)
            {
                float nod = 11f * (_gloveAway.x >= 0f ? -1f : 1f);
                AddGlove(_gloveShown, _gloveShownAng, s, pad);
                AddGlove(_gloveRest, _gloveRestAng, s, pad);
                AddGlove(_cueAimGui, _gloveRestAng + nod, s, pad);
                AddGlove((_gloveShown + _gloveRest) * 0.5f, _gloveShownAng, s, pad);
            }
            AddLiftedBirds(pad);
        }

        void AddGlove(Vector2 pivot, float ang, float s, float pad)
        {
            if (_blockN >= _blocks.Length) return;
            var tex = CoachGloveTex();
            if (tex == null) return;
            float dh = 118f * s;
            float dw = dh * (tex.width / (float)Mathf.Max(1, tex.height));
            float left = pivot.x - _gloveTipU * dw;
            float top = pivot.y - (1f - _gloveTipV) * dh;
            if (!RotAabb(left, top, dw, dh, pivot, ang, pad, out var box)) return;
            _blocks[_blockN].X0 = box.xMin;
            _blocks[_blockN].Y0 = box.yMin;
            _blocks[_blockN].X1 = box.xMax;
            _blocks[_blockN].Y1 = box.yMax;
            _blockN++;
        }

        void AddLiftedBirds(float pad)
        {
            var cam = _garden.Cam;
            var branches = _garden.Branches;
            if (cam == null || branches == null) return;
            for (int b = 0; b < branches.Length && _blockN < _blocks.Length; b++)
            {
                var view = branches[b];
                if (view == null) continue;
                var birds = view.Birds;
                for (int i = 0; i < birds.Length && _blockN < _blocks.Length; i++)
                {
                    var sr = birds[i];
                    if (sr == null || !sr.enabled || !sr.gameObject.activeInHierarchy) continue;
                    var idle = sr.GetComponent<BirdIdle>();
                    if (idle == null || idle.Shrouded || idle.Lift < 0.9f) continue;
                    if (!LiftedGui(cam, view, idle, pad, out var box)) continue;
                    _blocks[_blockN].X0 = box.xMin;
                    _blocks[_blockN].Y0 = box.yMin;
                    _blocks[_blockN].X1 = box.xMax;
                    _blocks[_blockN].Y1 = box.yMax;
                    _blockN++;
                }
            }
        }

        bool BlocksHit(Rect line)
        {
            for (int i = 0; i < _blockN; i++)
            {
                float x0 = _blocks[i].X0;
                float y0 = _blocks[i].Y0;
                float x1 = _blocks[i].X1;
                float y1 = _blocks[i].Y1;
                if (x1 >= line.xMin && x0 <= line.xMax && y1 >= line.yMin && y0 <= line.yMax)
                    return true;
            }
            return false;
        }

        static bool RotAabb(float left, float top, float dw, float dh, Vector2 pivot, float ang, float pad, out Rect box)
        {
            float rad = ang * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad);
            float sn = Mathf.Sin(rad);
            float minX = 0f, minY = 0f, maxX = 0f, maxY = 0f;
            for (int i = 0; i < 4; i++)
            {
                float x = (i & 1) == 0 ? left : left + dw;
                float y = (i & 2) == 0 ? top : top + dh;
                float dx = x - pivot.x;
                float dy = y - pivot.y;
                float rx = pivot.x + dx * c - dy * sn;
                float ry = pivot.y + dx * sn + dy * c;
                if (i == 0)
                {
                    minX = maxX = rx;
                    minY = maxY = ry;
                }
                else
                {
                    if (rx < minX) minX = rx;
                    if (ry < minY) minY = ry;
                    if (rx > maxX) maxX = rx;
                    if (ry > maxY) maxY = ry;
                }
            }
            minX -= pad;
            minY -= pad;
            maxX += pad;
            maxY += pad;
            box = new Rect(minX, minY, Mathf.Max(0f, maxX - minX), Mathf.Max(0f, maxY - minY));
            return true;
        }

        bool CoachBirdsBlock(Rect line, float pad)
        {
            var cam = _garden.Cam;
            var branches = _garden.Branches;
            if (cam == null || branches == null) return false;
            for (int b = 0; b < branches.Length; b++)
            {
                var view = branches[b];
                if (view == null) continue;
                var birds = view.Birds;
                for (int i = 0; i < birds.Length; i++)
                {
                    var sr = birds[i];
                    if (sr == null || !sr.enabled || !sr.gameObject.activeInHierarchy) continue;
                    var idle = sr.GetComponent<BirdIdle>();
                    if (idle == null || idle.Shrouded || idle.Lift < 0.9f) continue;
                    if (!LiftedGui(cam, view, idle, pad, out var box)) continue;
                    if (box.xMax >= line.xMin && box.xMin <= line.xMax && box.yMax >= line.yMin && box.yMin <= line.yMax)
                        return true;
                }
            }
            return false;
        }

        // Rest through the full hop, so the reserved box is where the wings arrive, not where they are this frame.
        static bool LiftedGui(Camera cam, BranchView view, BirdIdle idle, float pad, out Rect box)
        {
            box = default;
            var up = view.transform.TransformPoint(new Vector3(idle.RestLocal.x, idle.RestLocal.y + idle.Lift, idle.RestLocal.z));
            var down = view.transform.TransformPoint(idle.RestLocal);
            float top = (up.y > down.y ? up.y : down.y) + LiftHead;
            float bot = (up.y < down.y ? up.y : down.y) - LiftFoot;
            float x = up.x;
            var a = cam.WorldToScreenPoint(new Vector3(x - LiftHalfW, top, 0f));
            var b = cam.WorldToScreenPoint(new Vector3(x + LiftHalfW, bot, 0f));
            if (a.z < 0f && b.z < 0f) return false;
            float gx0 = (a.x < b.x ? a.x : b.x) - pad;
            float gx1 = (a.x > b.x ? a.x : b.x) + pad;
            float gy0 = Screen.height - (a.y > b.y ? a.y : b.y) - pad;
            float gy1 = Screen.height - (a.y < b.y ? a.y : b.y) + pad;
            box = new Rect(gx0, gy0, Mathf.Max(0f, gx1 - gx0), Mathf.Max(0f, gy1 - gy0));
            return true;
        }

        void DrawCoachGlove(float s)
        {
            if (!_gloveVis || _coachFade < 0.03f) return;
            var tex = CoachGloveTex();
            if (tex == null) return;
            float dh = 118f * s;
            float dw = dh * (tex.width / (float)tex.height);
            var pivot = _gloveShown;
            var rect = new Rect(
                pivot.x - _gloveTipU * dw,
                pivot.y - (1f - _gloveTipV) * dh,
                dw, dh);
            // Scale in the glove's own up (finger) axis, then rotate, so the dip squashes the fingertip.
            float widen = 1f + 0.07f * _gloveDip;
            float squash = 1f - 0.12f * _gloveDip;
            var prev = GUI.matrix;
            var fro = Matrix4x4.TRS(new Vector3(-pivot.x, -pivot.y, 0f), Quaternion.identity, Vector3.one);
            var scale = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(widen, squash, 1f));
            var rot = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, _gloveShownAng), Vector3.one);
            var to = Matrix4x4.TRS(new Vector3(pivot.x, pivot.y, 0f), Quaternion.identity, Vector3.one);
            GUI.matrix = to * rot * scale * fro * prev;
            GUI.color = new Color(1f, 1f, 1f, _coachFade);
            GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit, true);
            GUI.matrix = prev;
            GUI.color = Color.white;
        }

        void DrawCoach(float s, float top)
        {
            if (_levelHive) return;
            if (_cueLine != null)
                DrawCoachLine(_cueLine, s, top);
            if (_cueHand)
                DrawCoachGlove(s);
        }

        void CoachEnsureRipples()
        {
            if (_ripples != null) return;
            _ripples = new SpriteRenderer[RipplePool];
            _rippleAge = new float[RipplePool];
            var spr = CoachRippleSprite();
            for (int i = 0; i < RipplePool; i++)
            {
                var go = new GameObject("CoachRipple");
                go.hideFlags = HideFlags.HideAndDontSave;
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = spr;
                sr.sortingOrder = 46;
                sr.enabled = false;
                _ripples[i] = sr;
            }
        }

        void CoachSpawnRipple(Vector3 world)
        {
            CoachEnsureRipples();
            int slot = -1;
            int oldest = 0;
            float oldestAge = -1f;
            for (int i = 0; i < _ripples.Length; i++)
            {
                var sr = _ripples[i];
                if (sr == null || !sr.enabled)
                {
                    slot = i;
                    break;
                }
                if (_rippleAge[i] > oldestAge)
                {
                    oldestAge = _rippleAge[i];
                    oldest = i;
                }
            }
            if (slot < 0) slot = oldest;
            var ripple = _ripples[slot];
            if (ripple == null) return;
            ripple.enabled = true;
            ripple.transform.position = new Vector3(world.x, world.y, 0f);
            ripple.transform.localScale = new Vector3(0.28f, 0.28f, 1f);
            ripple.color = new Color(1f, 0.95f, 0.75f, 0.75f * Mathf.Clamp01(_coachFade));
            _rippleAge[slot] = 0f;
        }

        void CoachTickRipples(float dt)
        {
            if (_ripples == null) return;
            float fade = Mathf.Clamp01(_coachFade);
            for (int i = 0; i < _ripples.Length; i++)
            {
                var sr = _ripples[i];
                if (sr == null || !sr.enabled) continue;
                _rippleAge[i] += dt;
                float u = _rippleAge[i] / RippleLife;
                if (u >= 1f)
                {
                    sr.enabled = false;
                    continue;
                }
                float grow = 1f - (1f - u) * (1f - u);
                float span = Mathf.Lerp(0.28f, 1.20f, grow);
                sr.transform.localScale = new Vector3(span, span, 1f);
                float a = (1f - u) * (1f - u) * fade * 0.75f;
                sr.color = new Color(1f, 0.95f, 0.75f, a);
            }
        }

        void CoachHideRipples()
        {
            if (_ripples == null) return;
            for (int i = 0; i < _ripples.Length; i++)
                if (_ripples[i] != null) _ripples[i].enabled = false;
        }

        void CoachDestroyRipples()
        {
            if (_ripples == null) return;
            for (int i = 0; i < _ripples.Length; i++)
            {
                if (_ripples[i] != null)
                    Destroy(_ripples[i].gameObject);
            }
            _ripples = null;
            _rippleAge = null;
        }

        static Texture2D _coachGlove;
        static Texture2D _coachDim;
        static Sprite _coachGlowSpr;
        static Sprite _coachRipple;
        static float _gloveTipU = 0.39f, _gloveTipV = 0.87f;

        static Texture2D CoachGloveTex()
        {
            if (_coachGlove != null) return _coachGlove;
            const int w = 220, h = 300;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = "CoachGlove"
            };
            var px = new Color32[w * h];
            // Four fingers (index long) plus a side thumb and a ribbed cuff. Not a three-finger glove.
            Blob(px, w, h, 72f, 44f, 148f, 44f, 22f, 12f, 255, 255, 255);
            Blob(px, w, h, 78f, 100f, 26f, 158f, 16f, 11f, 255, 255, 255);
            Blob(px, w, h, 88f, 66f, 138f, 104f, 30f, 12f, 255, 255, 255);
            Blob(px, w, h, 192f, 106f, 200f, 136f, 9f, 8f, 255, 255, 255);
            Blob(px, w, h, 156f, 110f, 166f, 158f, 11f, 9f, 255, 255, 255);
            Blob(px, w, h, 128f, 114f, 134f, 180f, 12f, 10f, 255, 255, 255);
            Blob(px, w, h, 98f, 116f, 88f, 236f, 13f, 11f, 255, 255, 255);
            GloveTip(98f, 116f, 88f, 236f, 13f, 11f, w, h);
            Blob(px, w, h, 90f, 42f, 130f, 42f, 4f, 0f, 12, 12, 14);
            Blob(px, w, h, 92f, 60f, 128f, 60f, 3f, 0f, 12, 12, 14);
            tex.SetPixels32(px);
            tex.Apply(false, false);
            _coachGlove = tex;
            return tex;
        }

        static void GloveTip(float x0, float y0, float x1, float y1, float rad, float outline, float w, float h)
        {
            float dx = x1 - x0;
            float dy = y1 - y0;
            float len = Mathf.Sqrt(dx * dx + dy * dy);
            if (len < 0.001f) return;
            float reach = rad + outline;
            _gloveTipU = (x1 + dx / len * reach) / w;
            _gloveTipV = (y1 + dy / len * reach) / h;
        }

        static void Blob(Color32[] px, int w, int h, float x0, float y0, float x1, float y1, float rad, float outline, byte r, byte g, byte b)
        {
            if (outline > 0f)
                PaintCapsule(px, w, h, x0, y0, x1, y1, rad + outline, 12, 12, 14);
            PaintCapsule(px, w, h, x0, y0, x1, y1, rad, r, g, b);
        }

        static void PaintCapsule(Color32[] px, int w, int h, float x0, float y0, float x1, float y1, float rad, byte r, byte g, byte b)
        {
            float pad = rad + 2f;
            int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(x0, x1) - pad));
            int maxX = Mathf.Min(w - 1, Mathf.CeilToInt(Mathf.Max(x0, x1) + pad));
            int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(y0, y1) - pad));
            int maxY = Mathf.Min(h - 1, Mathf.CeilToInt(Mathf.Max(y0, y1) + pad));
            float abx = x1 - x0;
            float aby = y1 - y0;
            float den = abx * abx + aby * aby;
            for (int y = minY; y <= maxY; y++)
            {
                int row = y * w;
                for (int x = minX; x <= maxX; x++)
                {
                    float px0 = x + 0.5f;
                    float py0 = y + 0.5f;
                    float t = den < 0.001f ? 0f : ((px0 - x0) * abx + (py0 - y0) * aby) / den;
                    if (t < 0f) t = 0f;
                    else if (t > 1f) t = 1f;
                    float dx = px0 - (x0 + abx * t);
                    float dy = py0 - (y0 + aby * t);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float cover = rad - dist + 0.5f;
                    if (cover <= 0f) continue;
                    if (cover > 1f) cover = 1f;
                    Over(px, row + x, r, g, b, cover);
                }
            }
        }

        static void Over(Color32[] px, int i, byte r, byte g, byte b, float cover)
        {
            int a = (int)(cover * 255f);
            if (a <= 0) return;
            if (a >= 255)
            {
                px[i] = new Color32(r, g, b, 255);
                return;
            }
            var dst = px[i];
            float af = a / 255f;
            float ia = 1f - af;
            px[i] = new Color32(
                (byte)(r * af + dst.r * ia),
                (byte)(g * af + dst.g * ia),
                (byte)(b * af + dst.b * ia),
                (byte)Mathf.Min(255f, a + dst.a * ia));
        }

        static Texture2D CoachDimTex()
        {
            if (_coachDim != null) return _coachDim;
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = "CoachDim"
            };
            var px = new Color32[n * n];
            float c = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = (x - c) / c;
                    float dy = (y - c) / c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.SmoothStep(0.28f, 0.62f, d);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            _coachDim = tex;
            return tex;
        }

        static Sprite CoachGlowSprite()
        {
            if (_coachGlowSpr != null) return _coachGlowSpr;
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = "CoachGlow"
            };
            var px = new Color32[n * n];
            float c = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = (x - c) / c;
                    float dy = (y - c) / c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = 1f - Mathf.SmoothStep(0.12f, 1f, d);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            _coachGlowSpr = Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), n);
            _coachGlowSpr.name = "CoachGlow";
            _coachGlowSpr.hideFlags = HideFlags.HideAndDontSave;
            return _coachGlowSpr;
        }

        static Sprite CoachRippleSprite()
        {
            if (_coachRipple != null) return _coachRipple;
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = "CoachRipple"
            };
            var px = new Color32[n * n];
            float c = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = (x - c) / c;
                    float dy = (y - c) / c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float ring = Mathf.SmoothStep(0.34f, 0.48f, d) * (1f - Mathf.SmoothStep(0.56f, 0.78f, d));
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(ring) * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            _coachRipple = Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), n);
            _coachRipple.name = "CoachRipple";
            _coachRipple.hideFlags = HideFlags.HideAndDontSave;
            return _coachRipple;
        }
    }
}
