using UnityEngine;

namespace FlockFive
{
    // First garden: every hint shares one thick black edge, a soft glow under the
    // branch the step wants, and one gloved hand on that branch. The two reward
    // branches are the only hint that draws a mirrored pair. Every hand comes in
    // from the side and above the target. Other taps wait.
    public sealed partial class FlockFiveApp
    {
        const string CoachKey = "flockfive.coach.done";
        const string CoachGiftKey = "flockfive.coach.gift";
        const string CoachAdHandKey = "flockfive.coach.adhand";
        const string CoachHiveKey = "flockfive.coach.hive";
        const string CoachPokerKey = "flockfive.coach.poker";
        const string CoachLeafKey = "flockfive.coach.leaf";
        const string CoachSparrowKey = "flockfive.coach.sparrow";
        const string CoachHawkKey = "flockfive.coach.hawk";
        const string AdHandLine = "Tap to watch and unlock a bonus spot.";
        const string HiveIntroLine = "You found a bee! Bees live in your hive. Tap the hive to see your collection.";
        const string PokerIntroLine = "You earned coins from that stage. Tap poker to bet them.";
        // A feeder collect calls Board.Breeze, which lifts the tip leaf.
        const string LeafIntroLine = "Leaves hide these birds. Collect at a feeder to blow them away.";
        // A tap does not scare a sparrow. One full match (five birds) into its feeder does.
        const string SparrowIntroLine = "A sparrow! It blocks a feeder. Collect five matching birds there to chase it off.";
        // HitsNeeded is two collects on the blocked feeder. Five birds twice is ten.
        const string HawkIntroLine = "A hawk! It needs two collects on its feeder to clear. Match five birds twice to drive it off.";
        const int PestCueSparrow = 1;
        const int PestCueHawk = 2;
        const float PestCueSeconds = 4.5f;
        // Hawk waits out the sparrow line, then this long, so the two never share a frame.
        const float PestCueGap = 1.05f;
        // One outline for every coach line. StampCoach is the only draw path.
        const int CoachOutlinePx = 6;
        // Sweep along the arc, soft dip, rise back, short pause. 0.56+0.26+0.22+0.16 = 1.20s.
        const float TapHover = 0.56f;
        const float TapPress = 0.26f;
        const float TapLift = 0.22f;
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
        bool _adHand;
        bool _hiveIntro;
        bool _hiveIntroLive;
        bool _hiveIntroSaw;
        bool _pokerIntro;
        bool _pokerIntroLive;
        bool _pokerIntroSaw;
        bool _leafIntro;
        int _pestCue;
        float _pestCueUntil;
        float _pestNext;
        int _pestSlot = -1;
        bool _hivePopping;
        float _hivePop;
        float _hivePopMul = 1f;
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
        int _gloveBranch = -1;
        bool _gloveMirror;
        bool _restVis;
        bool _restMirror;
        Vector2 _restShown;
        float _restAng;
        Vector2 _restTip;
        Vector2 _restAim;
        Vector2 _restAway;
        float _restTravelHi;
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
            _gloveBranch = -1;
            _gloveMirror = false;
            _restVis = false;
            _restMirror = false;
            _coachGlowKick = 0f;
            _coachLineHeld = false;
            _cueHand = false;
            _cueForce = false;
            _cueFreeze = false;
            _cueGift = false;
            _cueLine = null;
            _cueBranch = -1;
            _gloveVis = false;
            _adHand = false;
            _pestCue = 0;
            _pestCueUntil = 0f;
            _pestNext = 0f;
            _pestSlot = -1;
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
                _coachGiftUntil = PlayClock.Now + 4.5f;
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

        // The other locked gift. -1 when this hint has only one reward branch.
        int OtherGiftBranch(int branch)
        {
            if (_board == null || branch < 0) return -1;
            int other = -1;
            for (int i = 0; i < _board.Branches.Count; i++)
            {
                if (i == branch) continue;
                var st = _board.Branches[i];
                if (st == null || !st.IsBonus || st.Broken || !st.AdLocked) continue;
                other = i;
            }
            return other;
        }

        // The usable extra limb on the left (ordinal 0). A locked sign is a reward
        // branch, so this is never the two-sign pair.
        bool LeftOpenBonus(int branch)
        {
            if (_board == null || (uint)branch >= (uint)_board.Branches.Count) return false;
            var st = _board.Branches[branch];
            if (st == null || !st.IsBonus || st.Broken || st.AdLocked) return false;
            int ord = BonusBranches.Ordinal(_board, branch);
            return ord < 0 || (ord & 1) == 0;
        }

        bool CoachPick(out int from, out int to)
        {
            from = -1; to = -1;
            int fallFrom = -1, fallTo = -1;
            // First lesson: a single bird hops onto a matching bird.
            if (_coachMoves == 0)
                for (int a1 = 0; a1 < _board.Branches.Count; a1++)
                {
                    if (!_board.CanPick(a1) || Locked(a1) || GiftLocked(a1) || _board.Branches[a1].TipRun() != 1) continue;
                    for (int b1 = 0; b1 < _board.Branches.Count; b1++)
                    {
                        if (b1 == a1 || Locked(b1) || GiftLocked(b1) || _board.Branches[b1].Birds.Count != 1) continue;
                        if (!_board.CanMove(a1, b1, out _)) continue;
                        from = a1; to = b1; return true;
                    }
                }
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
            _gloveBranch = -1;
            _gloveMirror = false;
            _restVis = false;
            _restMirror = false;
            _coachGlowKick = 0f;
            _coachLineHeld = false;
            CoachClearCue();
            CoachHideGlow();
            CoachHideRipples();
            _pestCue = 0;
            _pestCueUntil = 0f;
            _pestSlot = -1;
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
            TickHivePop();
            TickHiveIntro();
            TickPokerIntro();
            if (!PestStageFree())
                PestIntroHide();

            if (_splash || _board == null || _garden.Cam == null)
            {
                // Home lessons keep the tap cycle. A full release would restart it every frame.
                if ((_hiveIntro || _pokerIntro) && _splash) return;
                CoachRelease();
                return;
            }
            if (_restarting || _won || _gift != GiftFace.None)
            {
                // Tutorial stays down on the gift card. The one-shot ad glove keeps its tap cycle.
                if (_adHand && _gift == GiftFace.Card && !_restarting && !_won)
                {
                    _cueHand = false;
                    _cueForce = false;
                    _cueFreeze = false;
                    _cueGift = false;
                    _cueLine = null;
                    _cueBranch = -1;
                    CoachHideGlow();
                    CoachHideRipples();
                    return;
                }
                _adHand = false;
                CoachHideNow();
                return;
            }
            if (!_coach && PlayClock.Now >= _coachGiftUntil)
            {
                if (_leafIntro)
                {
                    LeafIntroAdvance();
                    return;
                }
                if (PestStageFree() && PestIntroAdvance())
                    return;
                CoachRelease();
                return;
            }

            CoachClearCue();
            if (PlayClock.Now < _coachGiftUntil)
            {
                int g = GiftBranch();
                if (g < 0)
                {
                    CoachHideGlow();
                    _gloveReady = false;
                    _coachFade = 0f;
                    return;
                }
                float left = (_coachGiftUntil - PlayClock.Now) / 0.5f;
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
                int other = OtherGiftBranch(_cueBranch);
                int hit = HitGiftSign(world);
                bool sign = hit == _cueBranch || (other >= 0 && hit == other);
                int nearBranch = -1;
                if (!sign && NearGift(_cueBranch, world)) nearBranch = _cueBranch;
                else if (!sign && other >= 0 && NearGift(other, world)) nearBranch = other;
                if (!sign && nearBranch < 0)
                {
                    CoachNudge();
                    return true;
                }
                int open = nearBranch >= 0 ? nearBranch : hit;
                if (!CanOfferBonus(open)) return true;
                if (nearBranch >= 0) OpenBonus(nearBranch);
                CoachHideNow();
                return nearBranch >= 0;
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
            if (_leafIntro && st.TipLocked && st.Count > 0 && view.Seats != null)
            {
                Vector3 acc = Vector3.zero;
                int n = 0;
                int tip = st.Count - 1;
                for (int seat = 0; seat <= tip && seat < view.Seats.Length; seat++)
                {
                    if (view.Seats[seat] == null) continue;
                    acc += view.SeatWorld(seat);
                    n++;
                }
                if (n > 0) focus = acc / n + Vector3.up * 0.95f;
            }
            else if (!_cueGift && st.Count > 0 && run > 0)
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
            if (_adHand)
            {
                if (_gift != GiftFace.Card)
                {
                    _adHand = false;
                    _gloveVis = false;
                    CoachHideGlow();
                    return;
                }
                CoachHideGlow();
                CoachHideRipples();
                _coachFade = 1f;
                float handS = Mathf.Max(Screen.height / 720f, 1f);
                CoachGloveAt(GiftWatchAim(handS), dt, handS);
                return;
            }
            if (_hiveIntroLive)
            {
                float handS = Mathf.Max(Screen.height / 720f, 1f);
                CoachGloveAt(SplashHiveRect().center, dt, handS);
                return;
            }
            if (_pokerIntroLive)
            {
                float handS = Mathf.Max(Screen.height / 720f, 1f);
                var box = SplashPokerRect();
                var seat = SplashRailSeat(RailPoker);
                Vector2 pokerAim = box.width > 12f ? box.center : seat.center;
                CoachGloveAt(pokerAim, dt, handS);
                return;
            }
            if (_pestCue != 0)
            {
                PlacePestGlove(dt);
                return;
            }
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
            if (CoachGloveAt(_cueAimGui, dt, s))
            {
                _coachGlowKick = 1f;
                CoachSpawnRipple(_cueAimWorld);
                float flash = (0.40f + 0.28f * pulse + 0.62f) * _coachFade;
                if (flash > 1f) flash = 1f;
                _coachGlow.transform.localScale = new Vector3(dx * breathe * spread * 1.12f, dy * breathe * spread * 1.12f, 1f);
                _coachGlow.color = new Color(1f, 0.91f, 0.46f, flash);
            }
        }

        // First bonus-branch ad card only. The flag sticks even if they close without watching.
        void ArmAdHand()
        {
            if (PlayerPrefs.GetInt(CoachAdHandKey, 0) != 0) return;
            PlayerPrefs.SetInt(CoachAdHandKey, 1);
            PlayerPrefs.Save();
            _adHand = true;
            _coachFade = 1f;
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
            CoachHideGlow();
            CoachHideRipples();
        }

        void DismissAdHand()
        {
            if (!_adHand) return;
            _adHand = false;
            _gloveVis = false;
            _gloveReady = false;
        }

        // Same arc-point as the tutorial glove. aimGui is GUI space, y down.
        // True on the frame the fingertip lands.
        bool CoachGloveAt(Vector2 aimGui, float dt, float s)
        {
            _cueAimGui = aimGui;
            // Of the two reward gloves, the left one is the mirror, so the pair is
            // symmetrical and both backs face the camera. One branch is never mirrored.
            _gloveMirror = false;
            if (_cueGift && !LeftOpenBonus(_cueBranch))
            {
                int other = OtherGiftBranch(_cueBranch);
                if (other >= 0 && GiftPointAim(other, out var otherAim))
                    _gloveMirror = aimGui.x < otherAim.x;
            }
            float margin = 130f * s;
            CoachAimAway(_cueAimGui, s, margin, out var away, out float gap, _gloveMirror);
            // Right-rail buttons. The sentence sits on their left and the play flower
            // owns the space under them, so the hand comes from the right and above.
            if (_hiveIntroLive)
            {
                away = new Vector2(1f, -0.8f).normalized;
                gap = Mathf.Max(gap, 72f * s);
            }
            else if (_pokerIntroLive)
            {
                away = new Vector2(1f, -0.8f).normalized;
                gap = Mathf.Max(gap, 76f * s);
            }
            else if (LeftOpenBonus(_cueBranch))
            {
                // One glove. From the right, above the limb, fingertip toward the left.
                _gloveMirror = false;
                away = new Vector2(1f, -0.82f).normalized;
                gap = Mathf.Max(gap, 72f * s);
            }
            away = SideAbove(away);
            var rest = _cueAimGui + away * gap;
            float ang = Mathf.Atan2(-away.x, away.y) * Mathf.Rad2Deg;
            _gloveRest = rest;
            _gloveRestAng = ang;
            // Same branch keeps the glide. Crossing to the hop's other branch takes the
            // tap in place so the hand does not slide across the flock. The phase clock stays put.
            bool swap = _gloveReady && _coach && !_cueGift && !_adHand
                && _gloveBranch >= 0 && _cueBranch >= 0 && _gloveBranch != _cueBranch
                && (_gloveBranch == _coachFrom || _gloveBranch == _coachTo)
                && (_cueBranch == _coachFrom || _cueBranch == _coachTo);
            if (!_gloveReady || swap)
            {
                _gloveTip = rest;
                _gloveVel = Vector2.zero;
                _gloveAway = away;
                _gloveAng = ang;
                _gloveAngVel = 0f;
                if (!_gloveReady)
                {
                    _glovePhase = 0f;
                    _gloveDip = 0f;
                    _tapSent = false;
                }
                _gloveReady = true;
                _gloveBranch = _cueBranch;
            }
            else
            {
                _gloveTip = Vector2.SmoothDamp(_gloveTip, rest, ref _gloveVel, 0.36f, Mathf.Infinity, dt);
                _gloveAway = Vector2.Lerp(_gloveAway, away, 1f - Mathf.Exp(-dt / 0.28f));
                if (_gloveAway.sqrMagnitude > 0.0001f) _gloveAway.Normalize();
                _gloveAng = Mathf.SmoothDampAngle(_gloveAng, ang, ref _gloveAngVel, 0.36f, Mathf.Infinity, dt);
                if (_cueBranch >= 0) _gloveBranch = _cueBranch;
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
            if (fire) _tapSent = true;
            // The crossing frame draws the bottom of the arc, where the ripple is sent.
            float posePhase = fire ? pressAt : _glovePhase;
            _gloveDip = TapDip(posePhase);
            float travel = TapTravel(posePhase);
            TapArc(_gloveTip, _cueAimGui, _gloveAway, s, travel, out var arcPos, out float arcAng, _gloveMirror);
            float wig = Mathf.Sin(Time.unscaledTime * 46f) * 8f * _gloveWiggle;
            var side = CurlSide(_gloveAway, _gloveMirror);
            _gloveShown = arcPos + side * wig * (1f - travel);
            float baseAng = Mathf.Atan2(-_gloveAway.x, _gloveAway.y) * Mathf.Rad2Deg;
            // Press nods with the curl. Positive flexes the unmirrored hand; the mirror flips it.
            float nod = 11f * _gloveDip * (_gloveMirror ? -1f : 1f);
            _gloveShownAng = _gloveAng + Mathf.DeltaAngle(baseAng, arcAng)
                + Mathf.Sin(Time.unscaledTime * 46f) * 7f * _gloveWiggle + nod;
            SeatGlove(ref _gloveShown, ref _gloveShownAng, _cueAimGui, s, _gloveMirror, _gloveDip);
            _gloveVis = true;
            return fire;
        }

        // Hand sits off the lifted flock, always to one side and above the aim.
        // GUI y grows down, so a positive away.y would rise into the target from underneath.
        void CoachAimAway(Vector2 aim, float s, float margin, out Vector2 away, out float gap, bool mirror = false)
        {
            gap = 52f * s;
            // Left half of the screen is met from the right, right half from the left.
            float hx = aim.x < Screen.width * 0.5f ? 1f : -1f;
            float sx = hx;
            if (LiftedGuiCenter(out var flock))
            {
                float dx = aim.x - flock.x;
                float dy = aim.y - flock.y;
                if (dx * dx + dy * dy > 36f * 36f && Mathf.Abs(dx) > 24f)
                    sx = dx >= 0f ? 1f : -1f;
            }
            bool nearTop = aim.y < margin * 0.55f;
            float y0 = nearTop ? -0.4f : -0.78f;
            float y1 = nearTop ? -0.78f : -1.05f;
            float y2 = nearTop ? -1.05f : -0.4f;
            float bestHx = hx, bestHy = y0, bestGap = gap;
            bool found = false;
            float[] xs = { sx, hx, -hx };
            float[] ys = { y0, y1, y2 };
            for (int g = 0; g < 3 && !found; g++)
            {
                float tryGap = (52f + 28f * g) * s;
                for (int i = 0; i < xs.Length && !found; i++)
                {
                    for (int k = 0; k < ys.Length && !found; k++)
                    {
                        float cx = xs[i];
                        float cy = ys[k];
                        if (cy >= 0f) continue;
                        var dir = new Vector2(cx, cy).normalized;
                        float ang = Mathf.Atan2(-dir.x, dir.y) * Mathf.Rad2Deg;
                        var rest = aim + dir * tryGap;
                        // The whole glove has to sit in the safe area, not only the fingertip.
                        if (!GloveFits(rest, ang, 118f * s, 0f, mirror, CoachSafeGui(12f * s))) continue;
                        if (GloveHitsBirds(rest, ang, s, mirror) || GloveHitsBirds(aim, ang, s, mirror)) continue;
                        if (ArcHitsBirds(rest, aim, dir, s, mirror)) continue;
                        bestHx = cx;
                        bestHy = cy;
                        bestGap = tryGap;
                        found = true;
                    }
                }
            }
            gap = bestGap;
            away = SideAbove(new Vector2(bestHx, bestHy));
        }

        // Side component required, and the rest stays above the aim.
        static Vector2 SideAbove(Vector2 away)
        {
            if (away.sqrMagnitude < 0.0001f) return new Vector2(1f, -0.8f).normalized;
            float x = away.x;
            if (Mathf.Abs(x) < 0.35f) x = x >= 0f ? 0.9f : -0.9f;
            float y = away.y;
            if (y > -0.2f) y = -0.8f;
            return new Vector2(x, y).normalized;
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

        bool GloveHitsBirds(Vector2 pivot, float ang, float s, bool mirror = false)
        {
            var tex = CoachGloveTex();
            if (tex == null) return false;
            float dh = 118f * s;
            var rect = GloveRect(pivot, dh, mirror);
            if (!RotAabb(rect.x, rect.y, rect.width, rect.height, pivot, ang, 8f * s, out var box)) return false;
            return CoachBirdsBlock(box, 0f);
        }

        // Side hold and the widest part of the sweep. Rest and the aim are checked by the caller.
        bool ArcHitsBirds(Vector2 rest, Vector2 aim, Vector2 away, float s, bool mirror = false)
        {
            TapArc(rest, aim, away, s, 0f, out var sidePos, out float sideAng, mirror);
            if (GloveHitsBirds(sidePos, sideAng, s, mirror)) return true;
            TapArc(rest, aim, away, s, 0.22f, out var bowPos, out float bowAng, mirror);
            return GloveHitsBirds(bowPos, bowAng, s, mirror);
        }

        // 0 through the sweep, 1 at the bottom of the arc. The dip and the lift both ease.
        static float TapDip(float phase)
        {
            float pressAt = TapHover + TapPress;
            if (phase < TapHover) return 0f;
            if (phase < pressAt)
            {
                float u = (phase - TapHover) / TapPress;
                return u * u * (3f - 2f * u);
            }
            float liftEnd = pressAt + TapLift;
            if (phase < liftEnd)
            {
                float u = (phase - pressAt) / TapLift;
                float back = u * u * (3f - 2f * u);
                return 1f - back;
            }
            return 0f;
        }

        // 0 at the side pose, 1 on the target. One ease in, the same curve back out.
        static float TapTravel(float phase)
        {
            float pressAt = TapHover + TapPress;
            float liftEnd = pressAt + TapLift;
            if (phase < pressAt)
            {
                float u = phase / pressAt;
                return u * u * (3f - 2f * u);
            }
            if (phase < liftEnd)
            {
                float u = (phase - pressAt) / TapLift;
                float back = u * u * (3f - 2f * u);
                return 1f - back;
            }
            return 0f;
        }

        // Curl side of the finger. mirror flips the knuckles. A result that points
        // down the screen is turned back up, so the sweep curls in from above.
        static Vector2 CurlSide(Vector2 away, bool mirror)
        {
            var side = new Vector2(away.y, -away.x);
            if (mirror) side = -side;
            if (side.y > 0f) side = -side;
            return side;
        }

        // Curved path from the side, over the top, onto the aim. The bow flattens
        // at the end, so the last of the ease is along the finger. ang has no press nod.
        static void TapArc(Vector2 rest, Vector2 aim, Vector2 away, float s, float t,
            out Vector2 pos, out float ang, bool mirror = false)
        {
            float baseAng = Mathf.Atan2(-away.x, away.y) * Mathf.Rad2Deg;
            float gap = Vector2.Distance(rest, aim);
            if (gap < 1f || away.sqrMagnitude < 0.0001f)
            {
                pos = aim;
                ang = baseAng;
                return;
            }
            var side = CurlSide(away, mirror);
            const float bow = 5.5f;
            float sweep = Mathf.Min(gap * 0.46f, 36f * s);
            float peakT = (bow - 2f) / (3f * bow);
            float peakU = 1f - peakT;
            float peak = sweep * peakU * peakU * (1f + bow * peakT);
            float allow = FitAlong(rest, side, peak);
            if (peak > 0.001f && allow < peak) sweep *= allow / peak;
            float u = 1f - t;
            float lateral = sweep * u * u * (1f + bow * t);
            pos = aim + away * (gap * u) + side * lateral;
            float dLat = sweep * (-2f * u * (1f + bow * t) + u * u * bow);
            var travel = -away * gap + side * dLat;
            if (travel.sqrMagnitude < 0.0001f) travel = -away;
            travel.Normalize();
            float arcAng = Mathf.Atan2(travel.x, -travel.y) * Mathf.Rad2Deg;
            float delta = Mathf.Clamp(Mathf.DeltaAngle(baseAng, arcAng), -18f, 18f);
            ang = baseAng + delta * (1f - t);
        }

        // How far `dir` (unit) can go from origin before the point leaves the screen.
        static float FitAlong(Vector2 origin, Vector2 dir, float want)
        {
            if (want <= 0f) return 0f;
            const float m = 8f;
            float lim = want;
            if (dir.x > 0.02f) lim = Mathf.Min(lim, (Screen.width - m - origin.x) / dir.x);
            else if (dir.x < -0.02f) lim = Mathf.Min(lim, (origin.x - m) / -dir.x);
            if (dir.y > 0.02f) lim = Mathf.Min(lim, (Screen.height - m - origin.y) / dir.y);
            else if (dir.y < -0.02f) lim = Mathf.Min(lim, (origin.y - m) / -dir.y);
            return lim > 0f ? lim : 0f;
        }

        // Safe area in GUI space, inset so the outline does not sit on the notch.
        static Rect CoachSafeGui(float margin)
        {
            var a = Screen.safeArea;
            Rect safe = (a.width < 8f || a.height < 8f)
                ? new Rect(0f, 0f, Screen.width, Screen.height)
                : new Rect(a.x, Screen.height - a.yMax, a.width, a.height);
            float m = Mathf.Max(0f, margin);
            if (safe.width <= m * 2f + 8f || safe.height <= m * 2f + 8f)
                return safe;
            return new Rect(safe.x + m, safe.y + m, safe.width - m * 2f, safe.height - m * 2f);
        }

        // Finger angle: texture up aims from `from` toward `to` (GUI y grows downward).
        static float GlovePointAng(Vector2 from, Vector2 to)
        {
            float dx = to.x - from.x;
            float dy = to.y - from.y;
            if (dx * dx + dy * dy < 0.25f) return float.NaN;
            return Mathf.Atan2(dx, -dy) * Mathf.Rad2Deg;
        }

        // Drawn rect after the same scale-then-rotate DrawGloveAt applies. Mirror flips local X.
        static void GloveSpan(Vector2 pivot, float ang, float dh, float dip, bool mirror,
            out float minX, out float minY, out float maxX, out float maxY)
        {
            var tex = CoachGloveTex();
            float dw = tex == null ? dh * (220f / 300f) : dh * (tex.width / (float)Mathf.Max(1, tex.height));
            float left = pivot.x - _gloveTipU * dw;
            float top = pivot.y - (1f - _gloveTipV) * dh;
            float widen = 1f + 0.07f * dip;
            float squash = 1f - 0.12f * dip;
            float sx = mirror ? -widen : widen;
            float rad = ang * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad);
            float sn = Mathf.Sin(rad);
            minX = maxX = minY = maxY = 0f;
            for (int i = 0; i < 4; i++)
            {
                float x = (i & 1) == 0 ? left : left + dw;
                float y = (i & 2) == 0 ? top : top + dh;
                float dx = (x - pivot.x) * sx;
                float dy = (y - pivot.y) * squash;
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
                    if (rx > maxX) maxX = rx;
                    if (ry < minY) minY = ry;
                    if (ry > maxY) maxY = ry;
                }
            }
        }

        static bool GloveFits(Vector2 pivot, float ang, float dh, float dip, bool mirror, Rect safe)
        {
            GloveSpan(pivot, ang, dh, dip, mirror, out float x0, out float y0, out float x1, out float y1);
            return x0 >= safe.xMin - 0.4f && y0 >= safe.yMin - 0.4f
                && x1 <= safe.xMax + 0.4f && y1 <= safe.yMax + 0.4f;
        }

        static bool GlovePush(float minX, float minY, float maxX, float maxY, Rect safe, out Vector2 push)
        {
            float dx = 0f;
            float dy = 0f;
            float bw = maxX - minX;
            float bh = maxY - minY;
            if (bw >= safe.width) dx = safe.center.x - (minX + maxX) * 0.5f;
            else if (minX < safe.xMin) dx = safe.xMin - minX;
            else if (maxX > safe.xMax) dx = safe.xMax - maxX;
            if (bh >= safe.height) dy = safe.center.y - (minY + maxY) * 0.5f;
            else if (minY < safe.yMin) dy = safe.yMin - minY;
            else if (maxY > safe.yMax) dy = safe.yMax - maxY;
            push = new Vector2(dx, dy);
            return dx * dx + dy * dy > 0.25f;
        }

        // Keep the full glove rect inside the safe area. A pose that already fits is left
        // alone, so the tap arc keeps its reach. An overflowing pose shifts inward; the
        // finger stays aimed at the target, and the fingertip comes back in as far as the body allows.
        static void SeatGlove(ref Vector2 pivot, ref float ang, Vector2 aim, float s, bool mirror, float dip)
        {
            float dh = 118f * s;
            var safe = CoachSafeGui(12f * s);
            if (GloveFits(pivot, ang, dh, dip, mirror, safe)) return;
            float point = GlovePointAng(pivot, aim);
            float bias = float.IsNaN(point) ? 0f : Mathf.DeltaAngle(point, ang);
            for (int n = 0; n < 4; n++)
            {
                GloveSpan(pivot, ang, dh, dip, mirror, out float x0, out float y0, out float x1, out float y1);
                if (!GlovePush(x0, y0, x1, y1, safe, out var push)) break;
                pivot += push;
                float next = GlovePointAng(pivot, aim);
                if (float.IsNaN(next)) continue;
                float turn = float.IsNaN(point) ? 0f : Mathf.Abs(Mathf.DeltaAngle(point, next));
                if (turn > 80f) bias = 0f;
                ang = next + bias;
                point = next;
            }
            float dist = Vector2.Distance(pivot, aim);
            if (dist > 1f)
            {
                var dir = (aim - pivot) / dist;
                float lo = 0f;
                float hi = dist;
                var origin = pivot;
                for (int i = 0; i < 8; i++)
                {
                    float mid = (lo + hi) * 0.5f;
                    var tryP = origin + dir * mid;
                    float tryA = GlovePointAng(tryP, aim);
                    if (float.IsNaN(tryA))
                    {
                        hi = mid;
                        continue;
                    }
                    if (GloveFits(tryP, tryA + bias, dh, dip, mirror, safe)) lo = mid;
                    else hi = mid;
                }
                pivot = origin + dir * lo;
                float aimed = GlovePointAng(pivot, aim);
                if (!float.IsNaN(aimed)) ang = aimed + bias;
            }
            for (int n = 0; n < 3; n++)
            {
                GloveSpan(pivot, ang, dh, dip, mirror, out float x0, out float y0, out float x1, out float y1);
                if (!GlovePush(x0, y0, x1, y1, safe, out var push)) break;
                pivot += push;
            }
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

        void DrawCoachLine(string text, float s, float top, float boxH = 0f, int fontHi = 0, float placeY = -1f, float placeW = 0f, float placeX = -1f)
        {
            var st = CoachLineStyle();
            if (_coachContent == null) _coachContent = new GUIContent();
            float w = placeW > 1f ? placeW : Screen.width * 0.86f;
            float h = boxH > 1f ? boxH : 68f * s;
            int hi = fontHi > 0 ? fontHi : Mathf.RoundToInt(22f * s);
            int lo = fontHi > 0 ? 18 : 14;
            if (lo > hi) lo = hi;
            float y = placeY >= 0f ? placeY : CoachLineY(text, s, top, w, h);
            float x = placeX >= 0f ? placeX : (Screen.width - w) * 0.5f;
            var r = new Rect(x, y, w, h);
            if (_coachSizedFor != text || Mathf.Abs(_coachSizedW - r.width) > 1f || Mathf.Abs(_coachSizedH - r.height) > 1f)
            {
                _coachContent.text = text;
                _coachSizedPx = FitFontWrapped(st, text, r.width, r.height, lo, hi);
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
                float nod = 11f * (_gloveMirror ? -1f : 1f);
                AddGlove(_gloveShown, _gloveShownAng, s, pad, _gloveMirror);
                AddArcPose(0f, s, pad, 0f);
                AddArcPose(0.22f, s, pad, 0f);
                AddArcPose(0.55f, s, pad, 0f);
                AddArcPose(1f, s, pad, nod);
            }
            if (_restVis)
            {
                AddGlove(_restShown, _restAng, s, pad, _restMirror);
                AddRestPose(0.06f, s, s, pad);
                AddRestPose(_restTravelHi, s, s, pad);
            }
            AddPestBlock(pad);
            AddLiftedBirds(pad);
        }

        void AddRestPose(float t, float layoutS, float drawS, float pad)
        {
            if (_restAway.sqrMagnitude < 0.0001f) return;
            TapArc(_restTip, _restAim, _restAway, layoutS, t, out var pos, out float ang, _restMirror);
            SeatGlove(ref pos, ref ang, _restAim, layoutS, _restMirror, 0f);
            AddGlove(pos, ang, drawS, pad, _restMirror);
        }

        void AddArcPose(float t, float s, float pad, float extraAng)
        {
            if (_gloveAway.sqrMagnitude < 0.0001f) return;
            TapArc(_gloveTip, _cueAimGui, _gloveAway, s, t, out var pos, out float ang, _gloveMirror);
            float baseAng = Mathf.Atan2(-_gloveAway.x, _gloveAway.y) * Mathf.Rad2Deg;
            float shown = _gloveRestAng + Mathf.DeltaAngle(baseAng, ang) + extraAng;
            SeatGlove(ref pos, ref shown, _cueAimGui, s, _gloveMirror, 0f);
            AddGlove(pos, shown, s, pad, _gloveMirror);
        }

        void AddGlove(Vector2 pivot, float ang, float s, float pad, bool mirror = false)
        {
            if (_blockN >= _blocks.Length) return;
            var tex = CoachGloveTex();
            if (tex == null) return;
            float dh = 118f * s;
            var rect = GloveRect(pivot, dh, mirror);
            if (!RotAabb(rect.x, rect.y, rect.width, rect.height, pivot, ang, pad, out var box)) return;
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
            DrawGloveAt(_gloveShown, _gloveShownAng, 118f * s, _coachFade, _gloveDip, _gloveMirror);
        }

        // Unmirrored rect keeps the fingertip pixel on the pivot. mirror is for the
        // hit box only: the draw flips with a negative scale around that same pivot.
        Rect GloveRect(Vector2 pivot, float dh, bool mirror)
        {
            var tex = CoachGloveTex();
            float dw = tex == null ? dh : dh * (tex.width / (float)Mathf.Max(1, tex.height));
            float u = mirror ? 1f - _gloveTipU : _gloveTipU;
            return new Rect(pivot.x - u * dw, pivot.y - (1f - _gloveTipV) * dh, dw, dh);
        }

        void DrawGloveAt(Vector2 pivot, float ang, float dh, float alpha, float dip, bool mirror = false)
        {
            var tex = CoachGloveCurl(dip);
            if (tex == null) return;
            float dw = dh * (tex.width / (float)tex.height);
            var rect = new Rect(
                pivot.x - _gloveTipU * dw,
                pivot.y - (1f - _gloveTipV) * dh,
                dw, dh);
            // Scale in the glove's own up (finger) axis, then rotate, so the dip squashes the fingertip.
            // Negative X mirrors across the finger. The tip stays on the pivot, the back stays toward the camera.
            float widen = 1f + 0.07f * dip;
            float squash = 1f - 0.12f * dip;
            float sx = (mirror ? -widen : widen);
            var prev = GUI.matrix;
            var fro = Matrix4x4.TRS(new Vector3(-pivot.x, -pivot.y, 0f), Quaternion.identity, Vector3.one);
            var scale = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(sx, squash, 1f));
            var rot = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, ang), Vector3.one);
            var to = Matrix4x4.TRS(new Vector3(pivot.x, pivot.y, 0f), Quaternion.identity, Vector3.one);
            GUI.matrix = to * rot * scale * fro * prev;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit, true);
            GUI.matrix = prev;
            GUI.color = Color.white;
        }

        // Glove count. The mirrored pair is drawn only here, and only for the other
        // locked reward sign. The open left bonus, a hop, a leaf, a pest, the hive,
        // poker, and the ad card each point at one target and draw one hand.
        void PoseRestGlove(float s)
        {
            _restVis = false;
            if (LeftOpenBonus(_cueBranch)) return;
            if (!_gloveVis || !_cueGift || _won || !_cueHand || _coachFade < 0.03f) return;
            int other = OtherGiftBranch(_cueBranch);
            if (other < 0 || !GiftPointAim(other, out var aim)) return;
            _restMirror = aim.x < _cueAimGui.x;
            float margin = 130f * s;
            CoachAimAway(aim, s, margin, out var away, out float gap, _restMirror);
            away = SideAbove(away);
            var rest = aim + away * gap;
            float hi = 0.40f;
            TapArc(rest, aim, away, s, hi, out var farPos, out float farAng, _restMirror);
            if (GloveHitsBirds(farPos, farAng, s, _restMirror)) hi = 0.18f;
            float travel = Mathf.Lerp(0.06f, hi, TapTravel(_glovePhase));
            TapArc(rest, aim, away, s, travel, out _restShown, out _restAng, _restMirror);
            _restAng += 11f * TapDip(_glovePhase) * (_restMirror ? -1f : 1f);
            SeatGlove(ref _restShown, ref _restAng, aim, s, _restMirror, TapDip(_glovePhase));
            _restTip = rest;
            _restAim = aim;
            _restAway = away;
            _restTravelHi = hi;
            _restVis = true;
        }

        void DrawRestGlove(float s)
        {
            if (!_restVis || _coachFade < 0.03f) return;
            DrawGloveAt(_restShown, _restAng, 118f * s, _coachFade, TapDip(_glovePhase), _restMirror);
        }

        // Perch the tap hand uses, in GUI space. Matches the lesson focus, not the gift sign.
        bool CoachBranchAim(int branch, out Vector2 gui)
        {
            gui = default;
            var cam = _garden.Cam;
            var branches = _garden.Branches;
            if (cam == null || branches == null || _board == null) return false;
            if ((uint)branch >= (uint)branches.Length) return false;
            if ((uint)branch >= (uint)_board.Branches.Count) return false;
            var view = branches[branch];
            if (view == null) return false;
            Vector3 focus = view.transform.position + Vector3.up * 0.4f;
            var st = _board.Branches[branch];
            int run = st.TipRun();
            if (st.Count > 0 && run > 0)
            {
                int tip = st.Count - 1;
                int a = tip - run + 1;
                Vector3 acc = Vector3.zero;
                int n = 0;
                for (int i = a; i <= tip; i++)
                {
                    if ((uint)i >= (uint)view.Seats.Length || view.Seats[i] == null) continue;
                    acc += view.SeatWorld(i);
                    n++;
                }
                if (n > 0) focus = acc / n + Vector3.up * 0.42f;
            }
            var sp = cam.WorldToScreenPoint(focus);
            if (sp.z < 0f) return false;
            gui = new Vector2(sp.x, Screen.height - sp.y);
            return true;
        }

        // Gift sign when it is up, otherwise the limb. Matches the primary gift glove.
        bool GiftPointAim(int branch, out Vector2 gui)
        {
            gui = default;
            var cam = _garden.Cam;
            var branches = _garden.Branches;
            if (cam == null || branches == null || _board == null) return false;
            if ((uint)branch >= (uint)branches.Length) return false;
            if ((uint)branch >= (uint)_board.Branches.Count) return false;
            var view = branches[branch];
            if (view == null) return false;
            Vector3 focus = view.transform.position + Vector3.up * 0.4f;
            if (view.Sign != null && view.Sign.gameObject.activeInHierarchy)
                focus = view.Sign.position;
            var sp = cam.WorldToScreenPoint(focus);
            if (sp.z < 0f) return false;
            gui = new Vector2(sp.x, Screen.height - sp.y);
            return true;
        }

        void DrawCoach(float s, float top)
        {
            if (_levelHive)
            {
                _restVis = false;
                return;
            }
            PoseRestGlove(s);
            if (_cueLine != null)
            {
                if (_leafIntro)
                    DrawCoachLine(_cueLine, s, top, 100f * s, Mathf.RoundToInt(28f * s));
                else if (_pestCue != 0)
                    DrawCoachLine(_cueLine, s, top, 148f * s, Mathf.RoundToInt(34f * s));
                else
                    DrawCoachLine(_cueLine, s, top);
            }
            if (_restVis)
                DrawRestGlove(s);
            if (_cueHand)
                DrawCoachGlove(s);
        }

        // Drawn after the gift card so the wash does not cover the hand.
        // `top` is the close-button row. The line may sit higher, beside that button.
        void DrawAdHand(float s, float top)
        {
            if (!_adHand) return;
            _coachFade = 1f;
            AdHandLineRect(s, top, out var line);
            DrawCoachLine(AdHandLine, s, top, line.height, 0, line.y, line.width, line.x);
            DrawCoachGlove(s);
        }

        void TickHivePop()
        {
            if (!_hivePopping) return;
            _hivePop += Time.unscaledDeltaTime / 0.40f;
            if (_hivePop >= 1f)
            {
                _hivePop = 1f;
                _hivePopping = false;
                _hivePopMul = 1f;
                return;
            }
            float u = _hivePop;
            float settle = Mathf.SmoothStep(0.42f, 1f, u);
            float over = Mathf.Sin(u * Mathf.PI) * 0.12f * (1f - u);
            _hivePopMul = Mathf.Clamp(settle + over, 0.35f, 1.14f);
        }

        // Streak toast owns the middle of the home screen. The hive lesson waits until it tucks away.
        void TickHiveIntro()
        {
            bool live = _hiveIntro && _splash && _home == HomeFace.Splash && _streakSlide < 0f;
            if (!live)
            {
                _hiveIntroLive = false;
                if (_hiveIntro && _splash) _gloveVis = false;
                return;
            }
            if (!_hiveIntroLive)
            {
                _hivePopping = true;
                _hivePop = 0f;
                _hivePopMul = 0.35f;
                _gloveReady = false;
                _coachFade = 0f;
            }
            _hiveIntroLive = true;
            _hiveIntroSaw = true;
            _coachFade = Mathf.Min(1f, _coachFade + Time.unscaledDeltaTime / 0.30f);
        }

        void MarkHiveCoach()
        {
            if (PlayerPrefs.GetInt(CoachHiveKey, 0) != 0) return;
            PlayerPrefs.SetInt(CoachHiveKey, 1);
            PlayerPrefs.Save();
        }

        void ArmHiveIntro()
        {
            _hiveIntro = false;
            _hiveIntroLive = false;
            _hiveIntroSaw = false;
            if (!Hive.Collected) return;
            if (Hive.LegacyAdopted)
            {
                MarkHiveCoach();
                return;
            }
            if (PlayerPrefs.GetInt(CoachHiveKey, 0) != 0) return;
            _hiveIntro = true;
            _gloveReady = false;
            _gloveVis = false;
            _coachFade = 0f;
            TickHiveIntro();
        }

        void DismissHiveIntro()
        {
            if (!_hiveIntro) return;
            if (_hiveIntroSaw) MarkHiveCoach();
            _hiveIntro = false;
            _hiveIntroLive = false;
            _hiveIntroSaw = false;
            _gloveVis = false;
            _gloveReady = false;
            _coachFade = 0f;
        }

        void NoteHiveIntroLeft()
        {
            if (_hiveIntroSaw) MarkHiveCoach();
            _hiveIntro = false;
            _hiveIntroLive = false;
            _hiveIntroSaw = false;
        }

        // First real collect, lesson not stamped, not a pre-stamp album.
        bool HiveLessonOwed()
        {
            return Hive.Collected && !Hive.LegacyAdopted && PlayerPrefs.GetInt(CoachHiveKey, 0) == 0;
        }

        // Home rail, garden hive, and the album. Hidden until the lesson is on screen
        // or already stamped. A book that already has cards and is not waiting on that
        // lesson is shown and stamped, so a missed intro cannot hide it. The in-garden
        // skep still pops on the first collect so the visitor has a hive to fly to.
        bool HiveReachable()
        {
            if (PlayerPrefs.GetInt(CoachHiveKey, 0) == 0 && Hive.Found > 0 && !HiveLessonOwed())
                MarkHiveCoach();
            if (_hiveIntroLive) return true;
            if (_hiveIntro && !_hiveIntroLive) return false;
            if (PlayerPrefs.GetInt(CoachHiveKey, 0) != 0) return true;
            if (!_splash && Hive.Collected) return true;
            return false;
        }

        bool SplashHiveShown() => HiveReachable();

        void DrawHiveIntro(float s)
        {
            if (!_hiveIntroLive) return;
            // Seats, not the growing icons, so the line does not reflow during the slide.
            var hive = SplashRailSeat(RailHive);
            var pig = SplashRailSeat(RailPig);
            float botPad = Mathf.Max(14f, Screen.safeArea.yMin + 8f);
            float flower = Mathf.Min(Screen.width * 0.94f, Screen.height * 0.50f);
            float flowerTop = Screen.height - botPad - flower;
            float left = Mathf.Max(16f * s, Screen.safeArea.xMin + 8f);
            if (!NoAds.Owned && LevelData.NextPlay >= 1)
                left = Mathf.Max(left, SplashRailSeat(RailVip).xMax + 10f * s);
            float right = hive.x - 12f * s;
            float y = pig.yMax + 10f * s;
            float avail = flowerTop - 12f * s - y;
            float h = Mathf.Min(avail, 124f * s);
            float w = right - left;
            if (w < 150f * s || h < 64f * s)
            {
                left = (Screen.width - Screen.width * 0.86f) * 0.5f;
                w = Screen.width * 0.86f;
                y = TopHud() + 122f * s;
                h = Mathf.Max(72f * s, Mathf.Min(116f * s, hive.y - y - 8f * s));
            }
            var r = new Rect(left, y, w, h);
            var st = CoachLineStyle();
            if (_coachContent == null) _coachContent = new GUIContent();
            if (_coachSizedFor != HiveIntroLine || Mathf.Abs(_coachSizedW - r.width) > 1f || Mathf.Abs(_coachSizedH - r.height) > 1f)
            {
                _coachContent.text = HiveIntroLine;
                int hi = Mathf.Max(18, Mathf.RoundToInt(30f * s));
                _coachSizedPx = FitFontWrapped(st, HiveIntroLine, r.width, r.height, 18, hi);
                _coachSizedFor = HiveIntroLine;
                _coachSizedW = r.width;
                _coachSizedH = r.height;
            }
            st.fontSize = _coachSizedPx;
            int black = Mathf.Clamp(Mathf.CeilToInt(CoachOutlinePx * s), CoachOutlinePx, 8);
            StampOutlined(r, HiveIntroLine, st, new Color(1f, 0.96f, 0.82f, _coachFade), 0, black);
            DrawCoachGlove(s);
        }

        // Streak sign first, then the hive lesson. Poker waits so the two gloves never share a frame.
        void TickPokerIntro()
        {
            bool queued = _hiveIntro || _hiveIntroLive;
            // Wait until the rail has sampled a hidden poker slot, so the first
            // sighting eases in instead of popping on at full size.
            bool live = _pokerIntro && _railInit && _splash && _home == HomeFace.Splash
                && _streakSlide < 0f && !queued && LevelData.NextPlay >= 1;
            if (!live)
            {
                bool was = _pokerIntroLive;
                _pokerIntroLive = false;
                if (was) _gloveVis = false;
                return;
            }
            if (!_pokerIntroLive)
            {
                _gloveReady = false;
                _coachFade = 0f;
            }
            _pokerIntroLive = true;
            _pokerIntroSaw = true;
            _coachFade = Mathf.Min(1f, _coachFade + Time.unscaledDeltaTime / 0.30f);
        }

        void MarkPokerCoach()
        {
            if (PlayerPrefs.GetInt(CoachPokerKey, 0) != 0) return;
            PlayerPrefs.SetInt(CoachPokerKey, 1);
            PlayerPrefs.Save();
        }

        void ArmPokerIntro()
        {
            _pokerIntro = false;
            _pokerIntroLive = false;
            _pokerIntroSaw = false;
            if (LevelData.NextPlay < 1) return;
            if (PlayerPrefs.GetInt(CoachPokerKey, 0) != 0) return;
            _pokerIntro = true;
            if (_hiveIntro || _hiveIntroLive) return;
            _gloveReady = false;
            _gloveVis = false;
            _coachFade = 0f;
            TickPokerIntro();
        }

        void DismissPokerIntro()
        {
            if (!_pokerIntro) return;
            if (_pokerIntroSaw) MarkPokerCoach();
            _pokerIntro = false;
            _pokerIntroLive = false;
            _pokerIntroSaw = false;
            _gloveVis = false;
            _gloveReady = false;
            _coachFade = 0f;
        }

        void NotePokerIntroLeft()
        {
            if (_pokerIntroSaw) MarkPokerCoach();
            _pokerIntro = false;
            _pokerIntroLive = false;
            _pokerIntroSaw = false;
        }

        void DrawPokerIntro(float s)
        {
            if (!_pokerIntroLive) return;
            var poker = SplashRailSeat(RailPoker);
            var pig = SplashRailSeat(RailPig);
            float botPad = Mathf.Max(14f, Screen.safeArea.yMin + 8f);
            float flower = Mathf.Min(Screen.width * 0.94f, Screen.height * 0.50f);
            float flowerTop = Screen.height - botPad - flower;
            float left = Mathf.Max(16f * s, Screen.safeArea.xMin + 8f);
            if (!NoAds.Owned && LevelData.NextPlay >= 1)
                left = Mathf.Max(left, SplashRailSeat(RailVip).xMax + 10f * s);
            float right = poker.width > 1f ? poker.x - 12f * s : Screen.width - 24f * s;
            float y = pig.height > 1f ? pig.yMax + 10f * s : TopHud() + 122f * s;
            float avail = Mathf.Min(flowerTop, poker.y > 1f ? poker.y : flowerTop) - 12f * s - y;
            float h = Mathf.Min(Mathf.Max(0f, avail), 124f * s);
            float w = right - left;
            if (w < 150f * s || h < 64f * s)
            {
                left = (Screen.width - Screen.width * 0.86f) * 0.5f;
                w = Screen.width * 0.86f;
                y = TopHud() + 122f * s;
                float under = poker.y > 1f ? poker.y - y - 8f * s : 116f * s;
                h = Mathf.Max(72f * s, Mathf.Min(116f * s, under));
            }
            var r = new Rect(left, y, w, h);
            var st = CoachLineStyle();
            if (_coachContent == null) _coachContent = new GUIContent();
            if (_coachSizedFor != PokerIntroLine || Mathf.Abs(_coachSizedW - r.width) > 1f || Mathf.Abs(_coachSizedH - r.height) > 1f)
            {
                _coachContent.text = PokerIntroLine;
                int hi = Mathf.Max(18, Mathf.RoundToInt(30f * s));
                _coachSizedPx = FitFontWrapped(st, PokerIntroLine, r.width, r.height, 18, hi);
                _coachSizedFor = PokerIntroLine;
                _coachSizedW = r.width;
                _coachSizedH = r.height;
            }
            st.fontSize = _coachSizedPx;
            int black = Mathf.Clamp(Mathf.CeilToInt(CoachOutlinePx * s), CoachOutlinePx, 8);
            StampOutlined(r, PokerIntroLine, st, new Color(1f, 0.96f, 0.82f, _coachFade), 0, black);
            DrawCoachGlove(s);
        }

        int FirstLeaf()
        {
            if (_board == null) return -1;
            for (int i = 0; i < _board.Branches.Count; i++)
            {
                var br = _board.Branches[i];
                if (br.Broken || br.Count == 0 || !br.TipLocked) continue;
                return i;
            }
            return -1;
        }

        void ArmLeafIntro()
        {
            _leafIntro = false;
            if (_coach) return;
            if (PlayerPrefs.GetInt(CoachLeafKey, 0) != 0) return;
            if (FirstLeaf() < 0) return;
            PlayerPrefs.SetInt(CoachLeafKey, 1);
            PlayerPrefs.Save();
            _leafIntro = true;
            _coachFade = 0f;
            _gloveReady = false;
            _gloveWiggle = 0f;
            _glovePhase = 0f;
            _gloveDip = 0f;
            _tapSent = false;
            _cueForce = false;
            _cueFreeze = false;
        }

        void DismissLeafIntro()
        {
            if (!_leafIntro) return;
            _leafIntro = false;
            _cueHand = false;
            _cueForce = false;
            _cueFreeze = false;
            _cueGift = false;
            _cueLine = null;
            _cueBranch = -1;
            _gloveVis = false;
            _gloveReady = false;
            CoachHideGlow();
            CoachHideRipples();
        }

        void LeafIntroAdvance()
        {
            if (_won || _restarting || _gift != GiftFace.None || _levelHive)
            {
                _cueHand = false;
                _gloveVis = false;
                CoachHideGlow();
                return;
            }
            int b = FirstLeaf();
            if (b < 0)
            {
                DismissLeafIntro();
                return;
            }
            _coachFade = Mathf.Min(1f, _coachFade + Time.unscaledDeltaTime / 0.35f);
            _cueHand = true;
            _cueForce = false;
            _cueFreeze = false;
            _cueGift = false;
            _cueBranch = b;
            _cueLine = LeafIntroLine;
        }

        // Level-1 coach, hive lesson, poker lesson, leaf lesson, gift, and ads keep the glove.
        bool PestStageFree()
        {
            if (_splash || _board == null || _garden.Cam == null) return false;
            if (_restarting || _won || _frozen || _gift != GiftFace.None) return false;
            if (_coach || _leafIntro || _adHand || _levelHive) return false;
            if (_hiveIntro || _hiveIntroLive || _pokerIntro || _pokerIntroLive) return false;
            if (PlayClock.Now < _coachGiftUntil) return false;
            return true;
        }

        void PestIntroHide()
        {
            if (_pestCue == 0) return;
            _pestCue = 0;
            _pestCueUntil = 0f;
            _pestSlot = -1;
            _cueHand = false;
            _cueForce = false;
            _cueFreeze = false;
            _cueGift = false;
            _cueLine = null;
            _cueBranch = -1;
            _gloveVis = false;
            _gloveReady = false;
            _cueHolePx = 0f;
            CoachHideGlow();
            CoachHideRipples();
        }

        // True while a pest line is up. Caller skips CoachRelease so the tap ripples stay.
        bool PestIntroAdvance()
        {
            if (_pestCue != 0)
            {
                if (!PestCueAlive() || PlayClock.Now >= _pestCueUntil)
                {
                    DismissPestIntro();
                    return false;
                }
                ApplyPestCue();
                return true;
            }
            if (PlayClock.Now < _pestNext) return false;
            if (SparrowDue())
            {
                BeginPestCue(PestCueSparrow);
                return true;
            }
            if (HawkDue())
            {
                BeginPestCue(PestCueHawk);
                return true;
            }
            return false;
        }

        bool SparrowDue()
        {
            if (PlayerPrefs.GetInt(CoachSparrowKey, 0) != 0) return false;
            var s = SparrowView.Live;
            return s != null && s.IsBlocking && !s.InScrap;
        }

        // Hold the hawk while an unseen sparrow is still in the garden, and while
        // the sparrow line's gap has not elapsed. One line at a time.
        bool HawkDue()
        {
            if (PlayerPrefs.GetInt(CoachHawkKey, 0) != 0) return false;
            if (PlayClock.Now < _pestNext) return false;
            if (PlayerPrefs.GetInt(CoachSparrowKey, 0) == 0 && SparrowView.Live != null) return false;
            if (SparrowDue()) return false;
            var h = HawkView.Live;
            return h != null && h.IsBlocking && !h.InScrap;
        }

        bool PestCueAlive()
        {
            if (_pestCue == PestCueSparrow) return SparrowView.Live != null;
            if (_pestCue == PestCueHawk) return HawkView.Live != null;
            return false;
        }

        void BeginPestCue(int kind)
        {
            _pestCue = kind;
            _pestCueUntil = PlayClock.Now + PestCueSeconds;
            _coachFade = 0f;
            _gloveReady = false;
            _gloveWiggle = 0f;
            _glovePhase = 0f;
            _gloveDip = 0f;
            _tapSent = false;
            _gloveBranch = -1;
            _restVis = false;
            _coachGlowKick = 0f;
            _coachLineHeld = false;
            _pestSlot = -1;
            if (kind == PestCueSparrow && SparrowView.Live != null)
                _pestSlot = SparrowView.Live.BlockingSlot;
            else if (kind == PestCueHawk && HawkView.Live != null)
                _pestSlot = HawkView.Live.BlockingSlot;
            ApplyPestCue();
        }

        void ApplyPestCue()
        {
            _coachFade = Mathf.Min(1f, _coachFade + Time.unscaledDeltaTime / 0.30f);
            _cueHand = true;
            _cueForce = false;
            _cueFreeze = false;
            _cueGift = false;
            _cueBranch = -1;
            _cueHolePx = 0f;
            _cueLine = _pestCue == PestCueHawk ? HawkIntroLine : SparrowIntroLine;
        }

        void DismissPestIntro()
        {
            if (_pestCue == 0) return;
            if (_pestCue == PestCueSparrow) PlayerPrefs.SetInt(CoachSparrowKey, 1);
            else PlayerPrefs.SetInt(CoachHawkKey, 1);
            PlayerPrefs.Save();
            _pestNext = PlayClock.Now + PestCueGap;
            PestIntroHide();
        }

        // Tap on the pest dismisses its line and still reaches the garden.
        void PestIntroTap(Vector2 screen)
        {
            if (_pestCue == 0 || HitHud(screen)) return;
            var cam = _garden.Cam != null ? _garden.Cam : Camera.main;
            if (cam == null) return;
            var world = (Vector2)cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0f));
            if (_pestCue == PestCueSparrow && SparrowView.Live != null && PestHit(SparrowView.Live.transform, world))
                DismissPestIntro();
            else if (_pestCue == PestCueHawk && HawkView.Live != null && PestHit(HawkView.Live.transform, world))
                DismissPestIntro();
        }

        static bool PestHit(Transform t, Vector2 world)
        {
            if (t == null) return false;
            if (((Vector2)t.position - world).sqrMagnitude <= 1.35f * 1.35f) return true;
            var sr = t.GetComponent<SpriteRenderer>();
            if (sr == null || !sr.enabled || sr.sprite == null) return false;
            var b = sr.bounds;
            b.Expand(0.45f);
            return b.Contains(new Vector3(world.x, world.y, b.center.z));
        }

        void PlacePestGlove(float dt)
        {
            // Halo only. Hole stays under the dim threshold so the rest of the garden still takes taps.
            _cueHolePx = 0f;
            if (!PestAim(out var gui, out var world))
            {
                _gloveVis = false;
                CoachHideGlow();
                return;
            }
            CoachEnsureGlow();
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (Mathf.PI * 2f / 1.35f));
            float breathe = 0.94f + 0.08f * pulse;
            float a = (0.40f + 0.28f * pulse) * _coachFade;
            if (a > 1f) a = 1f;
            _coachGlow.enabled = true;
            _coachGlow.transform.position = world;
            _coachGlow.transform.localScale = new Vector3(2.3f * breathe, 2.3f * breathe, 1f);
            _coachGlow.color = new Color(1f, 0.91f, 0.46f, a);
            float handS = Mathf.Max(Screen.height / 720f, 1f);
            if (CoachGloveAt(gui, dt, handS))
            {
                _coachGlowKick = 1f;
                CoachSpawnRipple(world);
                _coachGlow.color = new Color(1f, 0.91f, 0.46f, Mathf.Min(1f, a + 0.35f * _coachFade));
            }
        }

        bool PestAim(out Vector2 gui, out Vector3 world)
        {
            gui = default;
            world = default;
            Transform body = null;
            int slot = _pestSlot;
            float perch = 0.35f;
            if (_pestCue == PestCueSparrow && SparrowView.Live != null)
            {
                body = SparrowView.Live.transform;
                if (SparrowView.Live.BlockingSlot >= 0) slot = SparrowView.Live.BlockingSlot;
            }
            else if (_pestCue == PestCueHawk && HawkView.Live != null)
            {
                body = HawkView.Live.transform;
                if (HawkView.Live.BlockingSlot >= 0) slot = HawkView.Live.BlockingSlot;
                perch = 0.42f;
            }
            if (body != null)
                world = body.position;
            else if (_garden.Feeders != null && (uint)slot < (uint)_garden.Feeders.Length && _garden.Feeders[slot] != null)
                world = _garden.Feeders[slot].Mouth + new Vector3(0f, perch, 0f);
            else
                return false;
            var cam = _garden.Cam;
            if (cam == null) return false;
            var sp = cam.WorldToScreenPoint(world);
            if (sp.z < 0f) return false;
            gui = new Vector2(sp.x, Screen.height - sp.y);
            return true;
        }

        void AddPestBlock(float pad)
        {
            if (_pestCue == 0 || _blockN >= _blocks.Length) return;
            Transform body = null;
            if (_pestCue == PestCueSparrow && SparrowView.Live != null) body = SparrowView.Live.transform;
            else if (_pestCue == PestCueHawk && HawkView.Live != null) body = HawkView.Live.transform;
            if (body == null) return;
            var sr = body.GetComponent<SpriteRenderer>();
            var cam = _garden.Cam;
            if (sr == null || cam == null || sr.sprite == null) return;
            var b = sr.bounds;
            var a = cam.WorldToScreenPoint(new Vector3(b.min.x, b.max.y, 0f));
            var c = cam.WorldToScreenPoint(new Vector3(b.max.x, b.min.y, 0f));
            if (a.z < 0f && c.z < 0f) return;
            float x0 = (a.x < c.x ? a.x : c.x) - pad;
            float x1 = (a.x > c.x ? a.x : c.x) + pad;
            float y0 = Screen.height - (a.y > c.y ? a.y : c.y) - pad;
            float y1 = Screen.height - (a.y < c.y ? a.y : c.y) + pad;
            _blocks[_blockN].X0 = x0;
            _blocks[_blockN].Y0 = y0;
            _blocks[_blockN].X1 = x1;
            _blocks[_blockN].Y1 = y1;
            _blockN++;
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

        const int GloveW = 220;
        const int GloveH = 300;
        // Rest curl, then three presses. The step is about one screen pixel, so the bend eases.
        const int GloveCurlSteps = 4;
        const float GloveIndexBowRest = -14f;
        const float GloveIndexBowPress = -20f;

        static Texture2D[] _gloveCurls;
        static float[] _gloveBodyDist;
        static Color32[] _gloveRestPx;
        static float _gloveIndexBow = GloveIndexBowRest;
        static Texture2D _coachDim;
        static Sprite _coachGlowSpr;
        static Sprite _coachRipple;
        static float _gloveTipU = 0.39f, _gloveTipV = 0.87f;

        // Outer silhouette. Ends and radii stay put, so the index tip (GloveTip reach 24),
        // the pivot, and the texture span stay put. Bow is the peak sagitta in pixels, to
        // the left of base→tip. The index bow is negative so it arches over the other
        // fingers, flat at the tip. A press deepens that bow. The thumb arches inward.
        // The pinky root overlaps the palm and sits against the ring.
        struct CoachCap
        {
            public float X0, Y0, X1, Y1, R0, R1, Bow, Curl;
            public CoachCap(float x0, float y0, float x1, float y1, float r0, float r1, float bow = 0f, float curl = 0f)
            {
                X0 = x0;
                Y0 = y0;
                X1 = x1;
                Y1 = y1;
                R0 = r0;
                R1 = r1;
                Bow = bow;
                Curl = curl;
            }
        }

        static readonly CoachCap[] _coachCaps =
        {
            new CoachCap(72f, 44f, 148f, 44f, 34f, 34f),
            new CoachCap(88f, 66f, 138f, 104f, 42f, 42f),
            new CoachCap(116f, 94f, 116f, 94f, 40f, 40f),
            new CoachCap(78f, 100f, 26f, 158f, 26f, 27f, -7f, -1f),
            new CoachCap(174f, 108f, 198f, 146f, 16f, 14f, -2.2f, 1f),
            new CoachCap(156f, 110f, 166f, 158f, 21f, 20f, -6f, 1f),
            new CoachCap(128f, 114f, 134f, 180f, 23f, 22f, -8f, 1f),
            new CoachCap(98f, 116f, 88f, 236f, 26f, 24f, -14f, 2f)
        };

        static Texture2D CoachGloveTex()
        {
            var rest = CoachGloveCurl(0f);
            if (_gloveCurls == null) return rest;
            for (int i = 1; i < GloveCurlSteps; i++)
            {
                if (_gloveCurls[i] == null)
                    CoachGloveCurl(i / (float)(GloveCurlSteps - 1));
            }
            return rest;
        }

        // dip 0 is the resting point. dip 1 curls the index a little further over the other fingers.
        static Texture2D CoachGloveCurl(float dip)
        {
            if (_gloveCurls == null) _gloveCurls = new Texture2D[GloveCurlSteps];
            int step = Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(dip) * (GloveCurlSteps - 1)), 0, GloveCurlSteps - 1);
            var cached = _gloveCurls[step];
            if (cached != null) return cached;
            if (step != 0 && _gloveRestPx == null)
            {
                CoachGloveCurl(0f);
                return CoachGloveCurl(dip);
            }
            EnsureGloveBody();
            _gloveIndexBow = Mathf.Lerp(GloveIndexBowRest, GloveIndexBowPress, step / (float)(GloveCurlSteps - 1));
            var tex = RasterCoachGlove(step == 0);
            _gloveCurls[step] = tex;
            GloveTip(98f, 116f, 88f, 236f, 13f, 11f, GloveW, GloveH);
            return tex;
        }

        static void EnsureGloveBody()
        {
            if (_gloveBodyDist != null) return;
            var body = new float[GloveW * GloveH];
            for (int y = 0; y < GloveH; y++)
            {
                int row = y * GloveW;
                float fy = y + 0.5f;
                for (int x = 0; x < GloveW; x++)
                    body[row + x] = GloveBodyRaw(x + 0.5f, fy);
            }
            _gloveBodyDist = body;
        }

        static Texture2D RasterCoachGlove(bool rest)
        {
            var tex = new Texture2D(GloveW, GloveH, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = rest ? "CoachGlove" : "CoachGloveCurl"
            };
            var px = new Color32[GloveW * GloveH];
            for (int y = 0; y < GloveH; y++)
            {
                int row = y * GloveW;
                float fy = y + 0.5f;
                for (int x = 0; x < GloveW; x++)
                {
                    if (!rest && GloveIndexFar(x + 0.5f, fy))
                        px[row + x] = _gloveRestPx[row + x];
                    else
                        px[row + x] = GlovePixel(x + 0.5f, fy);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            if (rest) _gloveRestPx = px;
            return tex;
        }

        // A pixel the index never reaches, at rest or at full press, keeps the resting color.
        static bool GloveIndexFar(float x, float y)
        {
            float saved = _gloveIndexBow;
            bool far = GloveIndexDist(x, y, GloveIndexBowRest) > 24f
                && GloveIndexDist(x, y, GloveIndexBowPress) > 24f;
            _gloveIndexBow = saved;
            return far;
        }

        static float GloveIndexDist(float x, float y, float bow)
        {
            _gloveIndexBow = bow;
            return GloveCapDist(x, y, _coachCaps[7]);
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

        static Color32 GlovePixel(float x, float y)
        {
            float dist = GloveField(x, y);
            const float aa = 1.25f;
            float alpha = Mathf.Clamp01(0.5f - dist / aa);
            if (alpha <= 0f) return default;
            float ow = Mathf.Lerp(9f, 6.4f, Mathf.SmoothStep(30f, 250f, y));
            float fillT = Mathf.Clamp01((-dist - ow) / aa + 0.5f);
            GloveFill(x, y, dist, out float fr, out float fg, out float fb);
            float rib = GloveRib(x, y, dist);
            if (rib > 0f)
            {
                fr = Mathf.Lerp(fr, 186f, rib);
                fg = Mathf.Lerp(fg, 164f, rib);
                fb = Mathf.Lerp(fb, 148f, rib);
            }
            return new Color32(
                GloveByte(Mathf.Lerp(58f, fr, fillT)),
                GloveByte(Mathf.Lerp(40f, fg, fillT)),
                GloveByte(Mathf.Lerp(32f, fb, fillT)),
                GloveByte(alpha * 255f));
        }

        static void GloveFill(float x, float y, float dist, out float r, out float g, out float b)
        {
            const float e = 1.35f;
            float nx = GloveField(x + e, y) - GloveField(x - e, y);
            float ny = GloveField(x, y + e) - GloveField(x, y - e);
            float mag = Mathf.Sqrt(nx * nx + ny * ny);
            if (mag < 1e-4f) mag = 1f;
            nx /= mag;
            ny /= mag;
            // Lower-right of each puff. The middle stays bright so it reads as round.
            float down = Mathf.Clamp01(-ny * 0.55f + nx * 0.85f);
            float depth = dist < 0f ? -dist : 0f;
            float rim = Mathf.Clamp01((18f - depth) / 13f);
            float puff = 0f;
            var caps = _coachCaps;
            for (int i = 3; i <= 7; i++)
            {
                GloveAxis(x, y, caps[i], out float cx, out float cy, out float rad);
                float rx = x - cx;
                float ry = y - cy;
                float side = Mathf.Sqrt(rx * rx + ry * ry);
                if (side > rad || side < 0.4f || rad < 0.5f) continue;
                float edge = side / rad;
                edge *= edge;
                float face = Mathf.Clamp01((-ry / side) * 0.70f + (rx / side) * 0.62f);
                float here = face * edge;
                if (here > puff) puff = here;
            }
            float shade = Mathf.Clamp01(down * rim * 0.85f + puff * 0.55f);
            float soft = 1f - shade * 0.50f;
            r = Mathf.Lerp(206f, 250f, soft);
            g = Mathf.Lerp(182f, 245f, soft);
            b = Mathf.Lerp(162f, 236f, soft);
            float hi = 0f;
            const float lx = -0.30f, ly = 0.95f;
            for (int i = 3; i <= 7; i++)
            {
                var tip = caps[i];
                float rad = tip.R1;
                float hx = tip.X1 + lx * rad * 0.26f;
                float hy = tip.Y1 + ly * rad * 0.20f;
                float dx = x - hx;
                float dy = y - hy;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float spot = Mathf.Clamp01(1f - d / (rad * 0.50f));
                float core = Mathf.Clamp01(1f - d / (rad * 0.26f));
                spot = spot * 0.55f + core * core * 0.95f;
                if (spot > hi) hi = spot;
            }
            float px = (x - 104f) / 16f;
            float py = (y - 114f) / 11f;
            float palm = Mathf.Sqrt(px * px + py * py);
            float pad = Mathf.Clamp01(1f - palm);
            pad = pad * 0.5f + pad * pad * 0.7f;
            if (pad > hi) hi = pad;
            hi = Mathf.Clamp01(hi);
            r = Mathf.Lerp(r, 255f, hi);
            g = Mathf.Lerp(g, 255f, hi);
            b = Mathf.Lerp(b, 255f, hi);
        }

        static float GloveRib(float x, float y, float dist)
        {
            if (y > 68f || dist > -12f) return 0f;
            if (GloveCapDist(x, y, _coachCaps[0]) > -4f) return 0f;
            float best = 0f;
            float arc = (x - 110f) * (x - 110f) * 0.00085f;
            best = GloveBand(y, 33.5f - arc, 0.46f);
            float upper = GloveBand(y, 49.5f - arc, 0.34f);
            if (upper > best) best = upper;
            return best;
        }

        static float GloveBand(float y, float ry, float amp)
        {
            float d = y - ry;
            if (d < 0f) d = -d;
            float band = Mathf.Clamp01(1f - d / 4.2f);
            band = band * band * (3f - 2f * band);
            return band * amp;
        }

        // Cuff, palm, and thumb blend together. Ring, middle, and pinky join that
        // mass; the pinky root overlaps it so the fill stays one piece. The index
        // joins last, and its bow is the press curl.
        static float GloveField(float x, float y)
        {
            return GloveSmin(SampleGloveBody(x, y), GloveCapDist(x, y, _coachCaps[7]), 11f);
        }

        static float GloveBodyRaw(float x, float y)
        {
            var c = _coachCaps;
            float body = GloveCapDist(x, y, c[0]);
            body = GloveSmin(body, GloveCapDist(x, y, c[1]), 16f);
            body = GloveSmin(body, GloveCapDist(x, y, c[2]), 14f);
            body = GloveSmin(body, GloveCapDist(x, y, c[3]), 13f);
            body = GloveSmin(body, GloveCapDist(x, y, c[4]), 11f);
            body = GloveSmin(body, GloveCapDist(x, y, c[5]), 11f);
            body = GloveSmin(body, GloveCapDist(x, y, c[6]), 11f);
            return body;
        }

        static float SampleGloveBody(float x, float y)
        {
            var body = _gloveBodyDist;
            if (body == null) return GloveBodyRaw(x, y);
            float fx = x - 0.5f;
            float fy = y - 0.5f;
            if (fx <= 0f || fy <= 0f || fx >= GloveW - 1f || fy >= GloveH - 1f)
                return GloveBodyRaw(x, y);
            int x0 = (int)fx;
            int y0 = (int)fy;
            float tx = fx - x0;
            float ty = fy - y0;
            int x1 = x0 + 1;
            int y1 = y0 + 1;
            float d00 = body[y0 * GloveW + x0];
            float d10 = body[y0 * GloveW + x1];
            float d01 = body[y1 * GloveW + x0];
            float d11 = body[y1 * GloveW + x1];
            return (d00 + (d10 - d00) * tx) * (1f - ty) + (d01 + (d11 - d01) * tx) * ty;
        }

        static float GloveSmin(float a, float b, float k)
        {
            float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / k);
            return Mathf.Lerp(b, a, h) - k * h * (1f - h);
        }

        static float GloveCapDist(float x, float y, CoachCap c)
        {
            GloveAxis(x, y, c, out float cx, out float cy, out float rad);
            float dx = x - cx;
            float dy = y - cy;
            return Mathf.Sqrt(dx * dx + dy * dy) - rad;
        }

        static void GloveAxis(float x, float y, CoachCap c, out float cx, out float cy, out float rad)
        {
            // Curl > 1.5 is the index. Its bow follows the press.
            if (c.Curl > 1.5f) c.Bow = _gloveIndexBow;
            float ax = c.X1 - c.X0;
            float ay = c.Y1 - c.Y0;
            float den = ax * ax + ay * ay;
            if (Mathf.Abs(c.Bow) < 0.01f || den < 0.0001f)
            {
                float t = den < 0.0001f ? 0f : ((x - c.X0) * ax + (y - c.Y0) * ay) / den;
                if (t < 0f) t = 0f;
                else if (t > 1f) t = 1f;
                rad = c.R0 + (c.R1 - c.R0) * t;
                cx = c.X0 + ax * t;
                cy = c.Y0 + ay * t;
                return;
            }
            float best = 0f;
            float bestD = 1e30f;
            for (int i = 0; i <= 8; i++)
            {
                float sample = i / 8f;
                GloveBowPoint(c, sample, out float sx, out float sy);
                float ddx = x - sx;
                float ddy = y - sy;
                float d = ddx * ddx + ddy * ddy;
                if (d < bestD)
                {
                    bestD = d;
                    best = sample;
                }
            }
            float t0 = best - 0.125f;
            float t1 = best + 0.125f;
            if (t0 < 0f) t0 = 0f;
            if (t1 > 1f) t1 = 1f;
            for (int i = 0; i < 8; i++)
            {
                float m0 = (2f * t0 + t1) / 3f;
                float m1 = (t0 + 2f * t1) / 3f;
                GloveBowPoint(c, m0, out float ax0, out float ay0);
                GloveBowPoint(c, m1, out float bx, out float by);
                float d0 = (x - ax0) * (x - ax0) + (y - ay0) * (y - ay0);
                float d1 = (x - bx) * (x - bx) + (y - by) * (y - by);
                if (d0 < d1) t1 = m1;
                else t0 = m0;
            }
            float tf = (t0 + t1) * 0.5f;
            GloveBowPoint(c, tf, out cx, out cy);
            rad = c.R0 + (c.R1 - c.R0) * tf;
        }

        // Curl > 1 arches over the top and is flat at the tip, so the fingertip keeps
        // aiming along the finger instead of kinking back. Curl > 0 hooks toward the
        // tip. Curl < 0 peaks mid-shaft and is flat at the tip.
        static void GloveBowPoint(CoachCap c, float t, out float x, out float y)
        {
            float ax = c.X1 - c.X0;
            float ay = c.Y1 - c.Y0;
            float len = Mathf.Sqrt(ax * ax + ay * ay);
            float px = 0f;
            float py = 0f;
            if (len > 0.001f)
            {
                px = -ay / len;
                py = ax / len;
            }
            float u = 1f - t;
            float amt;
            if (c.Curl > 1.5f)
            {
                // t^6 (1-t)^2 peaks at t=0.75 and has zero slope at the tip.
                float f = t * t * t * t * t * t * u * u;
                const float peak = 0.0111236572f;
                amt = f / peak;
            }
            else if (c.Curl > 0.5f) amt = 6.75f * t * t * u;
            else if (c.Curl < -0.5f) amt = 16f * t * t * u * u;
            else amt = 4f * t * u;
            float b = c.Bow * amt;
            x = c.X0 + ax * t + px * b;
            y = c.Y0 + ay * t + py * b;
        }

        static byte GloveByte(float v)
        {
            return (byte)Mathf.Clamp(v + 0.5f, 0f, 255f);
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
