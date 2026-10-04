using UnityEngine;

namespace FlockFive
{
    // First garden: every hint shares one thick black edge, a soft glow under the
    // branch the step wants, and one gloved hand. The hand points sideways. A
    // target on the left half is a right hand from the right, pointing left. A
    // target on the right half mirrors the sprite into a left hand from the left,
    // pointing right. The cuff sits off to that side and above the target. The
    // tap lifts, arcs over the top, and the fingertip descends onto the target.
    // Any tap fades that hand out in place. The next step fades it in at the
    // start of the new arc. It is never dragged to a new target while visible.
    // SeatTutorialCaption places the plate once. PlaceTutorCaption sits a low
    // target's plate above the path; every other line keeps its seat. The plate
    // does not move when the glove animates. ClearGloveOfCaption still keeps
    // the perch off that plate. DrawTutorOverlay paints the glove after the
    // caption, so the hand is always on top.
    // The pose clock is PlayClock, so a suspension cannot skip the arc.
    // Other taps wait. One glove on screen at a time.
    public sealed partial class FlockFiveApp
    {
        const string CoachKey = "flockfive.coach.done";
        const string CoachGiftKey = "flockfive.coach.gift";
        const string CoachAdHandKey = "flockfive.coach.adhand";
        const string CoachHiveKey = "flockfive.coach.hive";
        const string CoachPokerKey = "flockfive.coach.poker";
        const string CoachDailyKey = "flockfive.coach.daily";
        const string CoachLeafKey = "flockfive.coach.leaf";
        const string CoachSparrowKey = "flockfive.coach.sparrow";
        const string CoachHawkKey = "flockfive.coach.hawk";
        const string AdHandLine = "Tap to watch\nand unlock a bonus spot.";
        const string GiftStuckLine = "Stuck? Tap the gift branch for a bonus spot.";
        // Old clear CoachLineY rest under the hud, in reference pixels. Not the logo floor.
        const float TutorCaptionGap = 8f;
        // Gap between a caption plate and the glove sweep, in reference pixels.
        const float CaptionGloveMargin = 12f;
        // GUI y is down. An aim below this fraction of the screen is a low target.
        const float TutorAimLow = 0.68f;
        // Gift line only. Reference pixels under the shared PlaceCaption seat.
        const float GiftCaptionDrop = 24f;
        const string HiveIntroLine = "You found a bee!\nFinding bees awards cards\nthat are stored in your collection.\nClick the hive to view them.";
        const string HiveHomeLine = "Tap the hive to see your bees.";
        // Two lines, snug. The break is the wrap; the plate is measured to these lines.
        const string PokerIntroLine = "You earned coins!\nTap poker to bet them.";
        // Break is the two-line wrap. The plate is measured to these lines.
        const string DailyIntroLine = "Tap Daily for\nyour bonus!";
        // A feeder collect calls Board.Breeze, which lifts the tip leaf.
        const string LeafIntroLine = "Leaves hide these birds.\nCollect at a feeder\nto blow them away.";
        // A tap does not scare a sparrow. One full match (five birds) into its feeder does.
        const string SparrowIntroLine = "A sparrow!\nIt blocks a feeder.\nCollect five matching birds there\nto chase it off.";
        // HitsNeeded is two collects on the blocked feeder. Five birds twice is ten.
        const string HawkIntroLine = "A hawk!\nIt needs two collects\non its feeder to clear.\nMatch five birds twice\nto drive it off.";
        const int PestCueSparrow = 1;
        const int PestCueHawk = 2;
        const float PestCueSeconds = 4.5f;
        // Hawk waits out the sparrow line, then this long, so the two never share a frame.
        const float PestCueGap = 1.05f;
        // One outline for every coach line. StampCoach is the only draw path.
        const int CoachOutlinePx = 6;
        // About 20% under the old 118px lesson hand.
        const float GlovePx = 94.4f;
        static float GloveDh(float s) => GlovePx * s;
        // How far above the target the wind-up perch sits, in GUI pixels (y down).
        static float GloveRise(float s) => Mathf.Max(28f * s, GloveDh(s) * 0.34f);
        // Extra height of the apex above that perch, so the path rises before it falls.
        static float GloveArch(float s) => Mathf.Max(18f * s, GloveDh(s) * 0.20f);

        // Top of a round control, slightly inside the disc so the tip is on the face
        // and the palm (which sits above the pivot) stays off the label.
        static Vector2 TopTouch(Rect r)
        {
            if (r.width < 2f || r.height < 2f) return r.center;
            float inset = Mathf.Min(10f, r.height * 0.18f);
            return new Vector2(r.center.x, r.y + inset);
        }

        // Body of a bird quad that was actually drawn. The sheet is empty above
        // the head, so TopTouch lands off the crown. The fingertip wants this center.
        static Vector2 BodyTouch(Rect drawn)
        {
            return drawn.center;
        }

        // Poke along the arc, soft curl, draw back, short pause. 0.56+0.26+0.22+0.16 = 1.20s.
        const float TapHover = 0.56f;
        const float TapPress = 0.26f;
        const float TapLift = 0.22f;
        const float TapPause = 0.16f;
        const float TapCycle = TapHover + TapPress + TapLift + TapPause;
        // Tap hides the hand. The next step brings it back at the arc start.
        const float GloveFadeOutDur = 0.16f;
        const float GloveFadeInDur = 0.18f;
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
        bool _dailyIntro;
        bool _dailyIntroLive;
        bool _dailyIntroSaw;
        // True once this session has seen NextPlay still on level 1. That unlock slides
        // the gift in. A save that already cleared level 1 leaves this false.
        bool _dailyRailLocked;
        bool _dailyGloveOnClaim;
        bool _leafIntro;
        float _leafSeen;
        int _leafWatch = -1;
        int _pestCue;
        int _tutorPause;
        bool _hiveLevelCue;
        bool _hiveLevelLive;
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
        // Survives CoachClearCue, which nulls _cueLine every coach frame and then sets it again.
        string _cueSpoken;
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
        float _gloveApproach = float.NaN;
        float _glovePerchLift = float.NaN;
        bool _gloveFromLeft;
        bool _glovePosing;
        // Set at the start of CoachPlace. A new sentence waits for this frame's
        // pose before it latches, so the seat is not taken against last step's hand.
        int _glovePoseFrame = -1;
        Vector2 _gloveShown;
        Vector2 _gloveRest;
        float _gloveRestAng;
        float _glovePhase;
        float _gloveDip;
        bool _tapSent;
        int _gloveBranch = -1;
        bool _gloveMirror;
        // One veil for every lesson glove, including the home avatar.
        enum TutorGloveAct { None, Reappear, Live, FadeOutOnTap, Hidden }
        TutorGloveAct _gloveAct;
        float _gloveAlpha;
        float _gloveFadeFrom;
        float _gloveActT;
        bool _gloveLockOn;
        bool _gloveHoldPose;
        float _gloveHoldDip;
        Vector2 _gloveLockAim;
        Vector2 _gloveStepAim;
        string _gloveStepLine;
        int _gloveStepBranch = -1;
        bool _gloveStepFromLeft;
        float _gloveStepApproach = float.NaN;
        int _gloveClaimFrame = -1;
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
        float _coachLineHoldX;
        bool _coachHoldXOn;
        string _coachLineFor;
        // Tutorial plate, stored on the first draw of a step. Not the splash nudge latch.
        bool _tutorSeatOn;
        float _tutorSeatX;
        float _tutorSeatY;
        string _tutorSeatFor;
        // Last painted tutorial plate (text plus the soft corner). Not a seat latch.
        Rect _tutorPlate;
        int _tutorPlateFrame = -1;
        bool _tutorPlateOn;
        // Lowest GUI y the arc apex may use. NaN keeps the full arch.
        float _gloveApexMinY = float.NaN;
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
            _cueSpoken = null;
            _cueBranch = -1;
            _gloveVis = false;
            GloveVeilReset();
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
            _gloveApproach = float.NaN;
            _gloveBranch = -1;
            _gloveMirror = false;
            _restVis = false;
            _restMirror = false;
            _coachGlowKick = 0f;
            _coachLineHeld = false;
            _cueSpoken = null;
            GloveVeilReset();
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
                _gloveWiggle = Mathf.Max(0f, _gloveWiggle - PlayClock.Delta / 0.28f);
            TickAdopt();
            TickHivePop();
            TickHiveIntro();
            TickPokerIntro();
            TickDailyIntro();
            TickLevelBee();
            if (_pestCue == PestCueSparrow)
            {
                if (_splash || _won || !PestCueAlive())
                    PestIntroHide();
                else if (!Ads.IsShowing)
                    HoldTutorPause(true);
            }
            else if (!PestStageFree())
                PestIntroHide();

            if (_splash || _board == null || _garden.Cam == null)
            {
                // Home lessons keep the tap cycle. A full release would restart it every frame.
                if ((_hiveIntro || _pokerIntro || _dailyIntro || _welcomeGlove || _adoptLive || _pokerPageOn || _pokerDealHint || _albumTutorOn) && _splash) return;
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
                    _cueLine = AdHandLine;
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
                CueLine(GiftStuckLine);
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

            _coachFade = Mathf.Min(1f, _coachFade + PlayClock.Delta / 0.35f);
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
                CueLine("Tap a branch to pick up its top birds.");
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
                CueLine("Those birds have nowhere to go yet. Try another branch.");
                CoachHideGlow();
                _gloveReady = false;
                return;
            }
            _coachFrom = _sel;
            _coachTo = dest;
            _cueHand = true;
            _cueForce = true;
            _cueBranch = dest;
            CueLine("Now tap a branch with the same color on top, or an empty one.");
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

        // Body center of the top bird. The seat transform sits under the sprite.
        static bool TipBody(BranchView view, int seat, out Vector3 world)
        {
            world = default;
            if (view == null || view.Birds == null) return false;
            if ((uint)seat >= (uint)view.Birds.Length) return false;
            var sr = view.Birds[seat];
            if (sr == null || !sr.enabled || sr.sprite == null) return false;
            world = sr.bounds.center;
            return true;
        }

        static Vector3 SeatOrWood(BranchView view, int seat, Vector3 wood)
        {
            if (view.Seats != null && (uint)seat < (uint)view.Seats.Length && view.Seats[seat] != null)
                return view.SeatWorld(seat);
            return wood + Vector3.up * 0.4f;
        }

        void CoachFocus(BranchView view, out Vector3 aim, out Vector3 glow, out float dx, out float dy)
        {
            Vector3 wood = view.transform.position;
            Vector3 focus = wood + Vector3.up * 0.4f;
            var st = _board.Branches[_cueBranch];
            int run = st.TipRun();
            if (_leafIntro && st.TipLocked && st.Count > 0)
            {
                int tip = st.Count - 1;
                if (!TipBody(view, tip, out focus))
                    focus = SeatOrWood(view, tip, wood);
            }
            else if (!_cueGift && st.Count > 0 && run > 0)
            {
                int tip = st.Count - 1;
                if (!TipBody(view, tip, out focus))
                    focus = SeatOrWood(view, tip, wood);
            }
            else if (_cueGift && view.Sign != null && view.Sign.gameObject.activeInHierarchy)
            {
                focus = view.Sign.position;
                var sign = view.Sign.GetComponent<SpriteRenderer>();
                if (sign != null && sign.sprite != null) focus = sign.bounds.center;
            }

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

        // A new sentence fades back in. The same sentence, repeated each frame, does not.
        // CoachAdvance clears _cueLine and then calls this again with the same text.
        void CueLine(string text)
        {
            _cueLine = text;
            if (_cueSpoken == text) return;
            _cueSpoken = text;
            _coachFade = 0f;
            _coachLineHeld = false;
            _tutorSeatOn = false;
        }

        void CoachPlace()
        {
            _glovePoseFrame = Time.frameCount;
            float dt = PlayClock.Delta;
            NoteGloveTap();
            TickGloveAct(dt);
            if (_coachGlowKick > 0f)
                _coachGlowKick = Mathf.Max(0f, _coachGlowKick - dt / 0.24f);
            CoachTickRipples(dt);
            if (TickAlbumTutor(dt)) return;
            if (_adoptLive && _splash)
            {
                float adoptS = Mathf.Max(Screen.height / 720f, 1f);
                AdoptPlaceGlove(dt, adoptS);
                return;
            }
            if (_welcomeGlove)
            {
                if (!_splash || _home != HomeFace.Splash || VipOffer.IsOpen || VipRailGoal() < 0.5f)
                {
                    _welcomeGlove = false;
                    _gloveVis = false;
                    return;
                }
                float welcomeS = Mathf.Max(Screen.height / 720f, 1f);
                var vipBox = SplashNoAdsRect();
                var vipSeat = SplashRailSeat(RailVip);
                Vector2 vipAim = vipBox.width > 12f ? TopTouch(vipBox) : TopTouch(vipSeat);
                _coachFade = Mathf.Min(1f, _coachFade + dt / 0.30f);
                CoachGloveAt(vipAim, dt, welcomeS);
                if (PlayClock.Now >= _welcomeGloveUntil)
                {
                    _welcomeGlove = false;
                    _gloveVis = false;
                    _gloveReady = false;
                }
                return;
            }
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
                float handS = Mathf.Max(Screen.height / 720f, 1f);
                _cueLine = AdHandLine;
                _coachFade = Mathf.Min(1f, _coachFade + dt / 0.30f);
                GiftCardLayout(handS, out _, out var watchFlower);
                var watchDisc = FlowerDisc(watchFlower, 0f);
                Vector2 watchAim = TopTouch(watchDisc.width > 2f ? watchDisc : watchFlower);
                CoachGloveAt(watchAim, dt, handS);
                return;
            }
            if (_hiveIntroLive || _hiveLevelLive)
            {
                float handS = Mathf.Max(Screen.height / 720f, 1f);
                if (_hiveIntroLive && _splash)
                {
                    // Rail is still sliding or the skep is still popping. Sampling
                    // now locks the previous perch or a moving top. A visible hand
                    // fades out where it is. A hidden one stays hidden.
                    var box = SplashHiveRect();
                    bool settled = _railInit && RailSettled(RailHive) && !_hivePopping
                        && box.width > 12f && box.height > 12f;
                    if (!settled)
                    {
                        if ((_gloveAct == TutorGloveAct.Live || _gloveAct == TutorGloveAct.Reappear)
                            && _gloveVis && _gloveAlpha > 0.03f)
                            GloveStartFadeOut();
                        else if (_gloveAct != TutorGloveAct.FadeOutOnTap)
                        {
                            GloveVeilReset();
                            _gloveVis = false;
                        }
                        _glovePoseFrame = -1;
                        return;
                    }
                    if ((_gloveAct == TutorGloveAct.None || _gloveAct == TutorGloveAct.Hidden)
                        && (!string.Equals(_gloveStepLine, HiveHomeLine) || !_gloveLockOn))
                    {
                        GloveVeilReset();
                        _tutorSeatOn = false;
                    }
                    _cueLine = HiveHomeLine;
                    CoachGloveAt(TopTouch(box), dt, handS);
                    return;
                }
                CoachGloveAt(LevelHiveAim(), dt, handS);
                return;
            }
            if (_pokerIntroLive)
            {
                float handS = Mathf.Max(Screen.height / 720f, 1f);
                var box = SplashPokerRect();
                var seat = SplashRailSeat(RailPoker);
                var target = box.width > 12f ? box : seat;
                Vector2 pokerAim = target.width > 2f ? target.center : box.center;
                CoachGloveAt(pokerAim, dt, handS);
                return;
            }
            if (_dailyIntroLive)
            {
                float handS = Mathf.Max(Screen.height / 720f, 1f);
                bool onClaim = _dailyOpen;
                if (onClaim != _dailyGloveOnClaim)
                {
                    _dailyGloveOnClaim = onClaim;
                    _glovePhase = 0f;
                    _gloveDip = 0f;
                    _tapSent = false;
                }
                Vector2 dailyAim;
                if (onClaim)
                {
                    DailyClaimGlove(handS, out dailyAim, out float perchLift, out bool fromLeft);
                    CoachGloveAt(dailyAim, dt, handS, perchLift, fromLeft);
                }
                else
                {
                    // Same rect DrawDailyRail paints and HitPad tests. The fingertip
                    // is the pivot, so the center is the contact. The tap still arcs
                    // over the top and arrives straight down.
                    var box = SplashDailyRect();
                    if (box.width < 2f) box = SplashRailSeat(RailDaily);
                    dailyAim = box.center;
                    CoachGloveAt(dailyAim, dt, handS);
                }
                return;
            }
            if (TickPokerPageTutor(dt)) return;
            if (TickPokerDealHint(dt)) return;
            if (TickPokerBackHint(dt)) return;
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
            float a = (0.40f + 0.28f * pulse + 0.62f * kick) * EaseOutCubic(_coachFade);
            if (a > 1f) a = 1f;
            _coachGlow.color = new Color(1f, 0.91f, 0.46f, a);

            float s = Mathf.Max(Screen.height / 720f, 1f);
            if (CoachGloveAt(_cueAimGui, dt, s))
            {
                _coachGlowKick = 1f;
                CoachSpawnRipple(_cueAimWorld);
                float flash = (0.40f + 0.28f * pulse + 0.62f) * EaseOutCubic(_coachFade);
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
            _coachFade = 0f;
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

        // Hard hide. A fade that was still running does not keep a frozen hand.
        void GloveVeilReset()
        {
            _gloveAct = TutorGloveAct.None;
            _gloveAlpha = 0f;
            _gloveFadeFrom = 0f;
            _gloveActT = 0f;
            _gloveLockOn = false;
            _gloveHoldPose = false;
            _gloveHoldDip = 0f;
            _gloveClaimFrame = -1;
            _gloveStepLine = null;
            _gloveStepBranch = -1;
            _gloveStepFromLeft = false;
            _gloveStepApproach = float.NaN;
            _gloveLockAim = Vector2.zero;
            _gloveStepAim = Vector2.zero;
        }

        // Freeze the drawn pose and drop alpha. A second tap does not restart it.
        void GloveStartFadeOut()
        {
            if (_gloveAct == TutorGloveAct.FadeOutOnTap || _gloveAct == TutorGloveAct.Hidden
                || _gloveAct == TutorGloveAct.None)
                return;
            if (_gloveAlpha <= 0.03f)
            {
                _gloveAlpha = 0f;
                _gloveAct = TutorGloveAct.Hidden;
                _gloveVis = false;
                _gloveHoldPose = false;
                return;
            }
            _gloveFadeFrom = _gloveAlpha;
            _gloveActT = 0f;
            _gloveAct = TutorGloveAct.FadeOutOnTap;
            _gloveHoldPose = true;
            _gloveHoldDip = _gloveDip;
            _gloveVis = true;
        }

        void NoteGloveTap()
        {
            if (_gloveAct != TutorGloveAct.Live && _gloveAct != TutorGloveAct.Reappear) return;
            if (!Pressed(out _)) return;
            GloveStartFadeOut();
        }

        void TickGloveAct(float dt)
        {
            if (dt < 0f) dt = 0f;
            // Lesson ended while the hand was already hidden. Drop the step so
            // the next one can fade in, and so a stale aim cannot move a caption.
            if (_gloveAct == TutorGloveAct.Hidden
                && _gloveClaimFrame >= 0
                && _gloveClaimFrame < Time.frameCount - 1)
            {
                _gloveAct = TutorGloveAct.None;
                _gloveLockOn = false;
                _gloveAlpha = 0f;
                _gloveVis = false;
                return;
            }
            if (_gloveAct == TutorGloveAct.FadeOutOnTap)
            {
                _gloveActT += dt;
                float u = GloveFadeOutDur > 0f ? _gloveActT / GloveFadeOutDur : 1f;
                if (u >= 1f)
                {
                    _gloveAlpha = 0f;
                    _gloveAct = TutorGloveAct.Hidden;
                    _gloveVis = false;
                    _gloveHoldPose = false;
                    _gloveReady = false;
                    return;
                }
                _gloveAlpha = _gloveFadeFrom * (1f - u);
                _gloveVis = true;
                return;
            }
            if (_gloveAct != TutorGloveAct.Reappear) return;
            _gloveActT += dt;
            float dur = GloveFadeInDur > 0f ? GloveFadeInDur : 0.18f;
            _gloveAlpha = Mathf.Clamp01(_gloveActT / dur);
            if (_gloveAlpha >= 1f)
            {
                _gloveAlpha = 1f;
                _gloveAct = TutorGloveAct.Live;
            }
        }

        // Same lesson target. A bobbing bird is not a new step.
        bool GloveSameStep(Vector2 aim, float s, bool fromLeft, float approachDeg)
        {
            if (!_gloveLockOn || _gloveAct == TutorGloveAct.None) return false;
            if (!string.Equals(_cueLine, _gloveStepLine)) return false;
            if (_cueBranch != _gloveStepBranch) return false;
            if (fromLeft != _gloveStepFromLeft) return false;
            bool angled = !float.IsNaN(approachDeg);
            bool was = !float.IsNaN(_gloveStepApproach);
            if (angled != was) return false;
            if (angled && Mathf.Abs(Mathf.DeltaAngle(approachDeg, _gloveStepApproach)) > 2f) return false;
            float lim = 64f * s;
            return (aim - _gloveStepAim).sqrMagnitude <= lim * lim;
        }

        // Arc start for this aim. Drawn once, then left alone while the hand is visible.
        void GloveLockPose(Vector2 aimGui, float s, float perchLift, bool fromLeft, float approachDeg)
        {
            _cueAimGui = aimGui;
            _gloveApproach = approachDeg;
            _glovePerchLift = perchLift;
            _gloveFromLeft = fromLeft;
            NaturalGlovePose(s, out var rest, out var away, out bool mirror, out float ang);
            _gloveMirror = mirror;
            _glovePosing = true;
            ClearGloveOfCaption(_cueAimGui, s, ref rest, ref away, ref _gloveMirror, ref ang, approachDeg);
            _glovePosing = false;
            bool angled = !float.IsNaN(approachDeg);
            _gloveRest = rest;
            _gloveRestAng = ang;
            _gloveAway = away;
            _gloveAng = ang;
            _gloveAngVel = 0f;
            _gloveVel = Vector2.zero;
            if (angled)
            {
                float dh = GloveDh(s);
                var safe = CoachSafeGui(8f * s);
                var start = rest + away * (dh * 0.9f);
                float pad = dh * 0.42f;
                float x0 = safe.xMin + pad;
                float x1 = safe.xMax - pad;
                float y0 = safe.yMin + pad;
                float y1 = safe.yMax - pad;
                if (x1 < x0) { x0 = safe.center.x; x1 = x0; }
                if (y1 < y0) { y0 = safe.center.y; y1 = y0; }
                start.x = Mathf.Clamp(start.x, x0, x1);
                start.y = Mathf.Clamp(start.y, y0, y1);
                float below = _cueAimGui.y + 28f * s;
                if (start.y < below) start.y = Mathf.Min(y1, below);
                _gloveTip = start;
            }
            else
                _gloveTip = rest;
            _glovePhase = 0f;
            _gloveDip = 0f;
            _tapSent = false;
            _gloveReady = true;
            _gloveBranch = _cueBranch;
            _gloveLockOn = true;
            _gloveHoldPose = false;
            _gloveLockAim = aimGui;
            _gloveStepAim = aimGui;
            _gloveStepLine = _cueLine;
            _gloveStepBranch = _cueBranch;
            _gloveStepFromLeft = fromLeft;
            _gloveStepApproach = approachDeg;
        }

        void GloveBeginReappear(Vector2 aimGui, float s, float perchLift, bool fromLeft, float approachDeg)
        {
            GloveLockPose(aimGui, s, perchLift, fromLeft, approachDeg);
            _gloveAct = TutorGloveAct.Reappear;
            _gloveActT = 0f;
            _gloveAlpha = 0f;
            _gloveFadeFrom = 0f;
        }

        // Shared pose for every glove: tap, hive, daily, gift, hawk, leaves, sparrow,
        // and the home avatar. aimGui is GUI space, y down. True on the frame the
        // fingertip lands. A tap fades the hand out where it is. The next step
        // fades in at the start of that step's arc. perchLift replaces the shared
        // rise. fromLeft keeps the hand on the left when a nudge would flip it.
        bool CoachGloveAt(Vector2 aimGui, float dt, float s, float perchLift = float.NaN, bool fromLeft = false, float approachDeg = float.NaN)
        {
            _gloveClaimFrame = Time.frameCount;
            if (_gloveAct == TutorGloveAct.FadeOutOnTap)
            {
                // Caption reads the new aim. The drawn hand stays on the locked pose.
                _cueAimGui = aimGui;
                _gloveApproach = approachDeg;
                _glovePerchLift = perchLift;
                _gloveFromLeft = fromLeft;
                _gloveVis = _gloveAlpha > 0.03f;
                return false;
            }
            bool same = GloveSameStep(aimGui, s, fromLeft, approachDeg);
            if (_gloveAct == TutorGloveAct.Hidden && same)
            {
                _cueAimGui = aimGui;
                _gloveVis = false;
                return false;
            }
            if (!same || !_gloveLockOn || _gloveAct == TutorGloveAct.None)
            {
                bool showing = (_gloveAct == TutorGloveAct.Live || _gloveAct == TutorGloveAct.Reappear)
                    && _gloveAlpha > 0.03f && _gloveVis;
                if (showing)
                {
                    _cueAimGui = aimGui;
                    _gloveApproach = approachDeg;
                    _glovePerchLift = perchLift;
                    _gloveFromLeft = fromLeft;
                    GloveStartFadeOut();
                    return false;
                }
                GloveBeginReappear(aimGui, s, perchLift, fromLeft, approachDeg);
            }
            _cueAimGui = aimGui;
            bool hold = _gloveAct == TutorGloveAct.Reappear;
            float pressAt = TapHover + TapPress;
            bool fire = false;
            float posePhase = 0f;
            float travel = 0f;
            if (hold)
            {
                _glovePhase = 0f;
                _gloveDip = 0f;
                _tapSent = false;
            }
            else
            {
                float prevPhase = _glovePhase;
                float nextPhase = prevPhase + dt;
                fire = _coachFade > 0.25f && !_tapSent && prevPhase < pressAt && nextPhase >= pressAt;
                if (nextPhase >= TapCycle)
                {
                    nextPhase -= TapCycle;
                    if (nextPhase < 0f) nextPhase = 0f;
                    _tapSent = false;
                }
                _glovePhase = nextPhase;
                if (fire) _tapSent = true;
                posePhase = fire ? pressAt : _glovePhase;
                _gloveDip = TapDip(posePhase);
                travel = TapTravel(posePhase);
            }
            bool angled = !float.IsNaN(_gloveApproach);
            Vector2 aim = _gloveLockOn ? _gloveLockAim : aimGui;
            Vector2 arcPos;
            if (angled)
                arcPos = Vector2.Lerp(_gloveTip, aim, Mathf.Clamp01(travel));
            else
                TapArc(_gloveTip, aim, _gloveAway, s, travel, out arcPos, out _, _gloveMirror, _gloveApexMinY);
            float wig = hold ? 0f : Mathf.Sin(Time.unscaledTime * 46f) * 7f * _gloveWiggle;
            var axis = _gloveAway.sqrMagnitude > 0.0001f ? _gloveAway : Vector2.right;
            _gloveShown = arcPos - axis.normalized * wig * (1f - travel);
            _gloveShownAng = _gloveRestAng;
            bool hand = _gloveMirror;
            SeatGlove(ref _gloveShown, ref _gloveShownAng, aim, s, ref hand, _gloveDip, _gloveApproach);
            // Seat may slide the cuff. At the tap the pivot is the aim, so the
            // drawn fingertip (bob is zero while dipped) is the target.
            if (travel > 0.84f)
            {
                float pin = Mathf.SmoothStep(0f, 1f, (travel - 0.84f) / 0.16f);
                _gloveShown = Vector2.Lerp(_gloveShown, arcPos, pin);
            }
            _gloveShownAng = angled ? _gloveApproach : ClampUpright(_gloveMirror);
            _gloveVis = true;
            return fire;
        }

        // The perch CoachGloveAt uses before ClearGloveOfCaption. Same aim, lift,
        // and approach the draw will keep when the plate already misses this pose.
        void NaturalGlovePose(float s, out Vector2 rest, out Vector2 away, out bool mirror, out float ang)
        {
            bool angled = !float.IsNaN(_gloveApproach);
            if (angled)
            {
                float rad = _gloveApproach * Mathf.Deg2Rad;
                var point = new Vector2(Mathf.Sin(rad), -Mathf.Cos(rad));
                away = -point;
                if (away.sqrMagnitude < 0.0001f) away = new Vector2(0.7071f, 0.7071f);
                away.Normalize();
                float dh = GloveDh(s);
                var safe = CoachSafeGui(12f * s);
                float gap = 64f * s;
                for (int i = 0; i < 5; i++)
                {
                    float tryGap = 48f * s + 16f * s * i;
                    var tryRest = _cueAimGui + away * tryGap;
                    if (GloveFits(tryRest, _gloveApproach, dh, 0f, false, safe))
                    {
                        gap = tryGap;
                        break;
                    }
                }
                rest = _cueAimGui + away * gap;
                ang = _gloveApproach;
                mirror = false;
                return;
            }
            CoachAimAway(_cueAimGui, s, out away, out float side, _glovePerchLift, _gloveFromLeft);
            float lift = float.IsNaN(_glovePerchLift) ? GloveRise(s) : _glovePerchLift;
            rest = _cueAimGui + away * side + new Vector2(0f, -lift);
            mirror = _gloveFromLeft || _cueAimGui.x >= Screen.width * 0.5f;
            ang = ClampUpright(mirror);
        }

        // Horizontal approach. Left half is met from the right (right hand, points
        // left). Right half is met from the left (mirrored left hand, points right).
        // The cuff is the far end of that line, toward the screen edge. Gap grows
        // until the glove clears the bird and still fits on screen.
        void CoachAimAway(Vector2 aim, float s, out Vector2 away, out float gap,
            float perchLift = float.NaN, bool fromLeft = false)
        {
            bool fromRight = !fromLeft && aim.x < Screen.width * 0.5f;
            away = new Vector2(fromRight ? 1f : -1f, 0f);
            bool mirror = !fromRight;
            float ang = ClampUpright(mirror);
            float floor = SideGap(s);
            var safe = CoachSafeGui(12f * s);
            float dh = GloveDh(s);
            float lift = float.IsNaN(perchLift) ? GloveRise(s) : perchLift;
            var rise = new Vector2(0f, -lift);
            gap = floor;
            float clear = -1f;
            for (int i = 0; i < 8; i++)
            {
                float tryGap = floor + 20f * s * i;
                // The wind-up perch only. The tap itself is supposed to touch the target.
                var rest = aim + away * tryGap + rise;
                bool hits = GloveHitsBirds(rest, ang, s, mirror) || GloveHitsPest(rest, ang, s, mirror)
                    || GloveHitsAdoptBird(rest, ang, s, mirror)
                    || StampOnGloveArc(rest, aim, away, s, mirror, ang);
                bool fits = GloveFits(rest, ang, dh, 0f, mirror, safe);
                if (!hits)
                {
                    clear = tryGap;
                    if (fits)
                    {
                        gap = tryGap;
                        return;
                    }
                }
                if (!fits) break;
            }
            // A clear pose whose cuff leaves the screen still beats one that covers the bird.
            if (clear > 0f) gap = clear;
        }

        // Branch and pest aims sit on a body. The fingertip starts outside that body
        // so the palm, which trails the index, does not cover it.
        float SideGap(float s)
        {
            float gap = 48f * s;
            bool atBird = _pestCue != 0
                || (_leafIntro && _cueBranch >= 0)
                || (_coach && _cueBranch >= 0 && !_cueGift);
            if (!atBird) return gap;
            var cam = _garden.Cam;
            if (cam == null || !cam.orthographic) return Mathf.Max(gap, 72f * s);
            float span = cam.orthographicSize * 2f;
            if (span < 0.01f) return Mathf.Max(gap, 72f * s);
            return Mathf.Max(gap, LiftHalfW * (Screen.height / span) + 16f * s);
        }

        bool GloveHitsBirds(Vector2 pivot, float ang, float s, bool mirror = false)
        {
            var tex = CoachGloveTex();
            if (tex == null) return false;
            float dh = GloveDh(s);
            var rect = GloveRect(pivot, dh, mirror);
            if (!RotAabb(rect.x, rect.y, rect.width, rect.height, pivot, ang, 8f * s, out var box)) return false;
            return CoachBirdsBlock(box, 0f);
        }

        // True when this glove sprite covers the garden multiplier chip.
        bool GloveCoversStamp(Vector2 pivot, float ang, float s, bool mirror)
        {
            if (!GardenStampLive()) return false;
            var stamp = GardenStampRect(s);
            float m = 8f * s;
            stamp.x -= m;
            stamp.y -= m;
            stamp.width += m * 2f;
            stamp.height += m * 2f;
            float dh = GloveDh(s);
            var rect = GloveRect(pivot, dh, mirror);
            if (!RotAabb(rect.x, rect.y, rect.width, rect.height, pivot, ang, 4f * s, out var box)) return false;
            return box.Overlaps(stamp);
        }

        // Perch, the bow, and the tap. A clear pose is one that misses the chip the whole way.
        bool StampOnGloveArc(Vector2 rest, Vector2 aim, Vector2 away, float s, bool mirror, float ang)
        {
            if (!GardenStampLive()) return false;
            if (GloveCoversStamp(rest, ang, s, mirror)) return true;
            bool angled = !float.IsNaN(_gloveApproach);
            for (int step = 1; step <= 4; step++)
            {
                float u = step / 4f;
                Vector2 pos;
                float poseAng;
                if (angled)
                {
                    pos = Vector2.Lerp(rest, aim, u);
                    poseAng = ang;
                }
                else
                    TapArc(rest, aim, away, s, u, out pos, out poseAng, mirror);
                if (GloveCoversStamp(pos, poseAng, s, mirror)) return true;
            }
            return false;
        }

        // Hawk and sparrow are not lifted birds. The same rect test keeps the palm off the body.
        bool GloveHitsPest(Vector2 pivot, float ang, float s, bool mirror)
        {
            if (_pestCue == 0) return false;
            Transform body = null;
            if (_pestCue == PestCueSparrow && SparrowView.Live != null)
                body = SparrowView.Live.transform;
            else if (_pestCue == PestCueHawk && HawkView.Live != null)
                body = HawkView.Live.transform;
            if (body == null) return false;
            var sr = body.GetComponent<SpriteRenderer>();
            if (sr == null || !sr.enabled || sr.sprite == null) return false;
            var cam = _garden.Cam;
            if (cam == null) return false;
            var b = sr.bounds;
            var a = cam.WorldToScreenPoint(new Vector3(b.min.x, b.max.y, 0f));
            var c = cam.WorldToScreenPoint(new Vector3(b.max.x, b.min.y, 0f));
            if (a.z < 0f && c.z < 0f) return false;
            float pad = 4f * s;
            float gx0 = (a.x < c.x ? a.x : c.x) - pad;
            float gx1 = (a.x > c.x ? a.x : c.x) + pad;
            float gy0 = Screen.height - (a.y > c.y ? a.y : c.y) - pad;
            float gy1 = Screen.height - (a.y < c.y ? a.y : c.y) + pad;
            float dh = GloveDh(s);
            var rect = GloveRect(pivot, dh, mirror);
            if (!RotAabb(rect.x, rect.y, rect.width, rect.height, pivot, ang, 6f * s, out var box)) return false;
            return box.xMax >= gx0 && box.xMin <= gx1 && box.yMax >= gy0 && box.yMin <= gy1;
        }

        // 0 through the poke, 1 at full reach. The curl and the return both ease.
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

        // 0 at the side hold, 1 at the end of the poke. One ease in, the same curve back out.
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

        // Over the top and down. t = 0 is the perch, t = 1 is exactly the aim
        // (the arc offset is zero on the tap). Arrival is straight down in GUI y.
        // apexMinY, when set, stops the bow at the tutorial plate. It never lifts the arc.
        static void TapArc(Vector2 rest, Vector2 aim, Vector2 away, float s, float t,
            out Vector2 pos, out float ang, bool mirror = false, float apexMinY = float.NaN)
        {
            t = Mathf.Clamp01(t);
            float high = Mathf.Min(rest.y, aim.y);
            float apexY = high - GloveArch(s);
            if (!float.IsNaN(apexMinY) && apexY < apexMinY)
                apexY = apexMinY < high ? apexMinY : high;
            var ctrl = new Vector2(aim.x, apexY);
            float u = 1f - t;
            pos = rest * (u * u) + ctrl * (2f * u * t) + aim * (t * t);
            ang = ClampUpright(mirror);
        }

        // How far the sprite reaches above the fingertip, in GUI pixels (y down).
        static float GloveAbove(float ang, float s, bool mirror, float dip)
        {
            GloveSpan(Vector2.zero, ang, GloveDh(s), dip, mirror, out _, out float top, out _, out _);
            float above = -top;
            return above > 0f ? above : 0f;
        }

        static float GloveIntersect(Vector2 pivot, float ang, float dh, float dip, bool mirror, Rect plate)
        {
            GloveSpan(pivot, ang, dh, dip, mirror, out float x0, out float y0, out float x1, out float y1);
            float ix0 = x0 > plate.xMin ? x0 : plate.xMin;
            float iy0 = y0 > plate.yMin ? y0 : plate.yMin;
            float ix1 = x1 < plate.xMax ? x1 : plate.xMax;
            float iy1 = y1 < plate.yMax ? y1 : plate.yMax;
            float w = ix1 - ix0;
            float h = iy1 - iy0;
            if (w <= 0f || h <= 0f) return 0f;
            return w * h;
        }

        // Hover, the glide in, the bow, and the tap. 0 means the whole pose misses the plate.
        float GlovePlateArea(Vector2 perch, Vector2 aim, Vector2 away, float s, bool mirror,
            float ang, bool angled, float approachDeg, Rect plate)
        {
            float dh = GloveDh(s);
            float area = 0f;
            Vector2 from = angled ? perch + away * (dh * 0.9f) : perch;
            for (int step = 0; step <= 4; step++)
            {
                float u = step / 4f;
                var glide = Vector2.Lerp(from, perch, u);
                area += GloveIntersect(glide, ang, dh, 0f, mirror, plate);
                Vector2 pos;
                float poseAng;
                if (angled)
                {
                    pos = Vector2.Lerp(perch, aim, u);
                    poseAng = ang;
                }
                else
                    TapArc(perch, aim, away, s, u, out pos, out poseAng, mirror, _gloveApexMinY);
                float dip = u > 0.8f ? 1f : 0f;
                area += GloveIntersect(pos, poseAng, dh, dip, mirror, plate);
                var seated = pos;
                float seatAng = poseAng;
                bool seatMirror = mirror;
                SeatGlove(ref seated, ref seatAng, aim, s, ref seatMirror, dip, approachDeg);
                area += GloveIntersect(seated, seatAng, dh, dip, seatMirror, plate);
            }
            return area;
        }

        static float ApexFloor(Rect plate, float ang, float s, bool mirror)
        {
            float above = GloveAbove(ang, s, mirror, 0f);
            float dipped = GloveAbove(ang, s, mirror, 1f);
            if (dipped > above) above = dipped;
            return plate.yMax + above + 2f;
        }

        // One avoidance for every lesson glove. Moves the perch (further out, then
        // down) or flips the approach side. A flip starts inboard of SideGap, with
        // the fingertip still inside the safe rect: the old far perch sat off screen,
        // GloveFits threw that pose out, and the inward cuff stayed on the plate.
        // The cuff may hang off the outer edge. SeatGlove leaves that hang. Pulling
        // it back walks the fingertip onto the plate. Does not move the caption.
        // The fingertip still comes down from above, and a flip is only the fallback.
        // TopTouch puts that fingertip on the face, so the palm stays above it.
        void ClearGloveOfCaption(Vector2 aim, float s, ref Vector2 rest, ref Vector2 away,
            ref bool mirror, ref float ang, float approachDeg)
        {
            _gloveApexMinY = float.NaN;
            if (!TutorCaptionPlate(out var plate)) return;
            bool angled = !float.IsNaN(approachDeg);
            float bestFloor = ApexFloor(plate, ang, s, mirror);
            _gloveApexMinY = bestFloor;
            float bestArea = GlovePlateArea(rest, aim, away, s, mirror, ang, angled, approachDeg, plate);
            bool onStamp = StampOnGloveArc(rest, aim, away, s, mirror, ang);
            if (bestArea <= 0f && !onStamp) return;
            Vector2 bestRest = rest;
            Vector2 bestAway = away;
            bool bestMirror = mirror;
            float bestAng = ang;
            bool birdsClear = !GloveHitsBirds(rest, ang, s, mirror) && !GloveHitsPest(rest, ang, s, mirror)
                && !GloveHitsAdoptBird(rest, ang, s, mirror);
            var safe = CoachSafeGui(12f * s);
            float dh = GloveDh(s);
            int passes = angled ? 1 : 2;
            for (int pass = 0; pass < passes; pass++)
            {
                bool flip = pass == 1;
                Vector2 dir = away;
                bool hand = mirror;
                float handAng = ang;
                float gap;
                float rise;
                if (flip)
                {
                    dir = new Vector2(-away.x, 0f);
                    if (dir.sqrMagnitude < 0.25f)
                        dir = new Vector2(aim.x < Screen.width * 0.5f ? -1f : 1f, 0f);
                    dir.Normalize();
                    hand = dir.x < 0f;
                    handAng = ClampUpright(hand);
                    // SideGap puts an edge-branch perch off the glass. Start close
                    // enough that the fingertip stays inside the safe rect, then step out.
                    float room = dir.x < 0f ? aim.x - safe.xMin : safe.xMax - aim.x;
                    if (room < 0f) room = 0f;
                    gap = 16f * s;
                    if (gap < 12f) gap = 12f;
                    if (gap > room) gap = room;
                    rise = GloveRise(s);
                }
                else if (angled)
                {
                    gap = Vector2.Distance(rest, aim);
                    if (gap < 8f * s) gap = 48f * s;
                    rise = 0f;
                }
                else
                {
                    gap = Mathf.Abs(rest.x - aim.x);
                    if (gap < 8f * s) gap = SideGap(s);
                    rise = aim.y - rest.y;
                    if (rise < 0f) rise = 0f;
                }
                float floor = ApexFloor(plate, handAng, s, hand);
                _gloveApexMinY = floor;
                int shoves = flip ? 14 : 8;
                for (int down = 0; down <= 12; down++)
                {
                    float drop = 8f * s * down;
                    if (!angled && rise - drop < 6f * s) break;
                    for (int shove = 0; shove <= shoves; shove++)
                    {
                        float reach = gap + 16f * s * shove;
                        Vector2 cand = angled
                            ? aim + dir * reach + new Vector2(0f, drop)
                            : aim + new Vector2(dir.x, 0f) * reach + new Vector2(0f, -(rise - drop));
                        if (!angled && cand.y > aim.y - 6f * s) continue;
                        bool onScreen = GloveFits(cand, handAng, dh, 0f, hand, safe);
                        if (!onScreen && !GloveHangOk(cand, handAng, dh, 0f, hand, safe, dir)) continue;
                        if (StampOnGloveArc(cand, aim, dir, s, hand, handAng)) continue;
                        if (birdsClear && (GloveHitsBirds(cand, handAng, s, hand) || GloveHitsPest(cand, handAng, s, hand)
                            || GloveHitsAdoptBird(cand, handAng, s, hand)))
                            continue;
                        float area = GlovePlateArea(cand, aim, dir, s, hand, handAng, angled, approachDeg, plate);
                        if (area <= 0f)
                        {
                            rest = cand;
                            away = dir;
                            mirror = hand;
                            ang = handAng;
                            _gloveApexMinY = floor;
                            return;
                        }
                        // A hanging cuff is only a win when it misses the plate.
                        // A partial overlap stays with a pose that fits.
                        if (!onScreen) continue;
                        if (area < bestArea)
                        {
                            bestArea = area;
                            bestRest = cand;
                            bestAway = dir;
                            bestMirror = hand;
                            bestAng = handAng;
                            bestFloor = floor;
                        }
                    }
                }
            }
            rest = bestRest;
            away = bestAway;
            mirror = bestMirror;
            ang = bestAng;
            _gloveApexMinY = bestFloor;
        }

        // Plate the glove must miss. Garden seats are known before the draw.
        // Anything painted through NoteTutorPlate is used on the next pose, and
        // on this pose when the paint already ran (the hive-card hint).
        bool TutorCaptionPlate(out Rect plate)
        {
            float s = Mathf.Max(Screen.height / 720f, 1f);
            if (_adHand && _gift == GiftFace.Card)
            {
                var pref = AdHandCaptionRect(s);
                var seat = SeatTutorialCaption(AdHandLine, s, pref.y, pref.width, pref.height, pref.y, pref.x);
                plate = PadPlate(CoachPanelRect(seat), s);
                return plate.width > 2f && plate.height > 2f;
            }
            if (_tutorPlateOn && _tutorPlateFrame == Time.frameCount)
            {
                plate = PadPlate(_tutorPlate, s);
                return true;
            }
            if (GardenTutorPlate(s, out plate) || SplashCaptionPlate(s, out plate))
            {
                plate = PadPlate(plate, s);
                return true;
            }
            if (_tutorPlateOn && _tutorPlateFrame >= 0 && Time.frameCount - _tutorPlateFrame <= 1)
            {
                plate = PadPlate(_tutorPlate, s);
                return true;
            }
            plate = default;
            return false;
        }

        static Rect PadPlate(Rect plate, float s)
        {
            float p = 6f * s;
            return new Rect(plate.x - p, plate.y - p, plate.width + p * 2f, plate.height + p * 2f);
        }

        void NoteTutorPlate(Rect plate)
        {
            if (plate.width < 2f || plate.height < 2f) return;
            _tutorPlate = plate;
            _tutorPlateOn = true;
            _tutorPlateFrame = Time.frameCount;
        }

        // Same box DrawCoach paints. Gift keeps the lower PlaceCaption seat.
        void GardenLineBox(float s, out float w, out float h)
        {
            w = Mathf.Min(Screen.width * 0.72f, 520f * s);
            if (_leafIntro)
                h = 176f * s;
            else if (_pestCue != 0 || _hiveLevelLive || _hiveIntroLive)
                h = 252f * s;
            else
                h = Mathf.Max(128f * s, 108f);
        }

        bool GardenTutorPlate(float s, out Rect plate)
        {
            plate = default;
            if (_adHand || _splash || _levelHive || string.IsNullOrEmpty(_cueLine)) return false;
            HudLayout(out _, out float top, out _, out _, out _);
            GardenLineBox(s, out float w, out float h);
            var seat = SeatTutorialCaption(_cueLine, s, top, w, h);
            plate = CoachPanelRect(seat);
            return plate.width > 2f && plate.height > 2f;
        }

        // GUI-free splash plates. Measured poker and daily lines arrive through NoteTutorPlate.
        bool SplashCaptionPlate(float s, out Rect plate)
        {
            plate = default;
            Rect seat;
            if (_adoptLive)
                seat = SeatedAdoptCaption(s);
            else if (_hiveIntroLive && _splash && _home == HomeFace.Splash)
            {
                var pref = HiveHomeCaption(s);
                seat = SeatTutorialCaption(HiveHomeLine, s, pref.y, pref.width, pref.height, pref.y, pref.x);
            }
            else
                return false;
            plate = CoachPanelRect(seat);
            return plate.width > 2f && plate.height > 2f;
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

        // False is the right hand pointing left. True is the mirrored left hand pointing right.
        // -90 points left, +90 points right. 0 would be finger-up and 180 finger-down;
        // neither is used.
        static float ClampUpright(bool mirror)
        {
            return mirror ? 90f : -90f;
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

        // Cuff may leave the screen on the side it approaches from. The fingertip
        // stays in the safe rect, and the sprite may not spill off the other three sides.
        static bool GloveHangOk(Vector2 pivot, float ang, float dh, float dip, bool mirror, Rect safe, Vector2 awayDir)
        {
            if (pivot.x < safe.xMin || pivot.x > safe.xMax || pivot.y < safe.yMin || pivot.y > safe.yMax)
                return false;
            GloveSpan(pivot, ang, dh, dip, mirror, out float minX, out float minY, out float maxX, out float maxY);
            if (minY < safe.yMin - 0.4f || maxY > safe.yMax + 0.4f) return false;
            if (minX < safe.xMin - 0.4f && awayDir.x >= -0.01f) return false;
            if (maxX > safe.xMax + 0.4f && awayDir.x <= 0.01f) return false;
            return true;
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

        // A pose that already fits is left alone, so the poke keeps its reach.
        // An overflow may slide perpendicular to the finger (the index stays level
        // with the target). It does not slide toward the target: that lays the palm
        // on the bird. The cuff may leave the screen on the edge side.
        static void SeatGlove(ref Vector2 pivot, ref float ang, Vector2 aim, float s, ref bool mirror, float dip, float approachDeg = float.NaN)
        {
            // The caller picked the hand from the screen half. Sliding to fit
            // must not retarget the finger, or a high branch turns it straight down.
            // A finite approach keeps that angle (top-edge Back comes up from below).
            bool angled = !float.IsNaN(approachDeg);
            if (angled)
            {
                ang = approachDeg;
                mirror = false;
            }
            else
                ang = ClampUpright(mirror);
            float dh = GloveDh(s);
            var safe = CoachSafeGui(12f * s);
            if (GloveFits(pivot, ang, dh, dip, mirror, safe)) return;
            // Outer cuff only. Pulling that edge back on glass walks the fingertip
            // across the caption. GloveHangOk is the same test the perch search uses.
            var cuff = angled
                ? new Vector2(-Mathf.Sin(approachDeg * Mathf.Deg2Rad), Mathf.Cos(approachDeg * Mathf.Deg2Rad))
                : (mirror ? Vector2.left : Vector2.right);
            if (GloveHangOk(pivot, ang, dh, dip, mirror, safe, cuff)) return;
            for (int n = 0; n < 4; n++)
            {
                GloveSpan(pivot, ang, dh, dip, mirror, out float x0, out float y0, out float x1, out float y1);
                if (!GlovePush(x0, y0, x1, y1, safe, out var push)) break;
                var toward = aim - pivot;
                float along = Vector2.Dot(push, toward);
                if (along > 0f && toward.sqrMagnitude > 1f)
                    push -= toward * (along / toward.sqrMagnitude);
                if (push.sqrMagnitude < 0.25f) break;
                pivot += push;
                ang = angled ? approachDeg : ClampUpright(mirror);
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
            GUI.color = new Color(0f, 0f, 0f, 0.34f * EaseOutCubic(_coachFade));
            GUI.DrawTexture(new Rect(c.x - half, c.y - half, side, side), tex, ScaleMode.StretchToFill, true);
            GUI.color = Color.white;
        }

        static GUIStyle _coachLine;
        static GUIContent _coachContent;
        static string _coachSizedFor;
        static string _coachShown;
        static float _coachSizedW;
        static float _coachSizedH;
        static int _coachSizedPx;
        static Texture2D _coachPanel;

        // Soft dark rounded plate behind every tutorial sentence.
        static Texture2D CoachPanelTex()
        {
            if (_coachPanel != null) return _coachPanel;
            const int n = 64;
            const float rad = 22f;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = "CoachPanel"
            };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float d = PanelSdf(x + 0.5f, y + 0.5f, n, n, rad);
                    float a = Mathf.Clamp01(0.55f - d * 0.45f);
                    a = a * a * (3f - 2f * a);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.Clamp(a * 255f + 0.5f, 0f, 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _coachPanel = tex;
            return tex;
        }

        static float PanelSdf(float x, float y, float w, float h, float rad)
        {
            float cx = Mathf.Abs(x - w * 0.5f) - (w * 0.5f - rad);
            float cy = Mathf.Abs(y - h * 0.5f) - (h * 0.5f - rad);
            float ox = Mathf.Max(cx, 0f);
            float oy = Mathf.Max(cy, 0f);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(cx, cy), 0f) - rad;
        }

        // Plate behind a tutorial sentence, including the soft corner pad.
        static Rect CoachPanelRect(Rect r)
        {
            const float padX = 18f;
            const float padY = 12f;
            return new Rect(r.x - padX, r.y - padY, r.width + padX * 2f, r.height + padY * 2f);
        }

        static void DrawCoachPanel(Rect r, float fade)
        {
            if (fade < 0.02f) return;
            var tex = CoachPanelTex();
            if (tex == null) return;
            var box = CoachPanelRect(r);
            var prev = GUI.color;
            GUI.color = new Color(0.07f, 0.05f, 0.03f, 0.90f * fade);
            const float corner = 22f;
            float u = corner / tex.width;
            float cw = Mathf.Min(corner, box.width * 0.5f);
            float ch = Mathf.Min(corner, box.height * 0.5f);
            var bl = new Rect(0f, 0f, u, u);
            var br = new Rect(1f - u, 0f, u, u);
            var tl = new Rect(0f, 1f - u, u, u);
            var tr = new Rect(1f - u, 1f - u, u, u);
            GUI.DrawTextureWithTexCoords(new Rect(box.x, box.yMax - ch, cw, ch), tex, bl);
            GUI.DrawTextureWithTexCoords(new Rect(box.xMax - cw, box.yMax - ch, cw, ch), tex, br);
            GUI.DrawTextureWithTexCoords(new Rect(box.x, box.y, cw, ch), tex, tl);
            GUI.DrawTextureWithTexCoords(new Rect(box.xMax - cw, box.y, cw, ch), tex, tr);
            var edgeH = new Rect(u, 0.40f, Mathf.Max(0.04f, 1f - 2f * u), 0.20f);
            var edgeV = new Rect(0.40f, u, 0.20f, Mathf.Max(0.04f, 1f - 2f * u));
            float midW = Mathf.Max(0f, box.width - cw * 2f);
            float midH = Mathf.Max(0f, box.height - ch * 2f);
            GUI.DrawTextureWithTexCoords(new Rect(box.x + cw, box.yMax - ch, midW, ch), tex, edgeH);
            GUI.DrawTextureWithTexCoords(new Rect(box.x + cw, box.y, midW, ch), tex, edgeH);
            GUI.DrawTextureWithTexCoords(new Rect(box.x, box.y + ch, cw, midH), tex, edgeV);
            GUI.DrawTextureWithTexCoords(new Rect(box.xMax - cw, box.y + ch, cw, midH), tex, edgeV);
            var mid = new Rect(u, u, Mathf.Max(0.04f, 1f - 2f * u), Mathf.Max(0.04f, 1f - 2f * u));
            GUI.DrawTextureWithTexCoords(new Rect(box.x + cw, box.y + ch, midW, midH), tex, mid);
            GUI.color = prev;
        }

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

        // Shared garden/tutorial line. SeatTutorialCaption latches the plate.
        // PaintCoachCaption draws. A caller may pin placeY; that pin is the
        // preferred seat, and the same latch still clears the glove.
        void DrawCoachLine(string text, float s, float top, float boxH = 0f, int fontHi = 0, float placeY = -1f, float placeW = 0f, float placeX = -1f)
        {
            float w = placeW > 1f ? placeW : Mathf.Min(Screen.width * 0.72f, 520f * s);
            float h = boxH > 1f ? boxH : Mathf.Max(128f * s, 108f);
            int hi = fontHi > 0 ? fontHi : Mathf.RoundToInt(32f * s);
            int lo = fontHi > 0 ? 20 : 18;
            if (lo > hi) lo = hi;
            var seat = SeatTutorialCaption(text, s, top, w, h, placeY, placeX);
            _coachLineFor = text;
            _coachLineHold = seat.y;
            _coachLineHoldX = seat.x;
            _coachHoldXOn = true;
            _coachLineHeld = true;
            PaintCoachCaption(text, seat, s, lo, hi);
        }

        // One x,y per tutorial sentence. The first call after this frame's pose
        // runs PlaceTutorCaption, then the latch never moves. placeY pins a
        // preferred seat (ad hand, splash lines). A low aim sits the plate above
        // the path; every other line keeps that preferred seat.
        Rect SeatTutorialCaption(string text, float s, float top, float w, float h, float placeY = -1f, float placeX = -1f)
        {
            if (_tutorSeatOn && _tutorSeatFor == text)
                return ClearCaptionOfStamp(new Rect(_tutorSeatX, _tutorSeatY, w, h), s);
            float restX;
            float restY;
            bool gift = false;
            if (placeY >= 0f)
            {
                restX = placeX >= 0f ? placeX : (Screen.width - w) * 0.5f;
                restY = placeY;
            }
            else
            {
                restX = (Screen.width - w) * 0.5f;
                restY = top + TutorCaptionGap * s;
                if (text == GiftStuckLine)
                {
                    gift = true;
                    float underLogo = CaptionFloorY(s);
                    if (underLogo < top) underLogo = top;
                    Rect low = PlaceCaption(s, w, h, underLogo);
                    restX = low.x;
                    restY = low.y + GiftCaptionDrop * s;
                    float limit = Screen.height - h - 8f;
                    if (restY > limit) restY = limit;
                }
            }
            var pref = new Rect(restX, restY, w, h);
            bool poseReady = _glovePosing || _glovePoseFrame == Time.frameCount;
            if (!poseReady)
                return ClearCaptionOfStamp(pref, s);
            var seat = PlaceTutorCaption(pref, s, gift);
            _tutorSeatFor = text;
            _tutorSeatX = seat.x;
            _tutorSeatY = seat.y;
            _tutorSeatOn = true;
            return ClearCaptionOfStamp(new Rect(seat.x, seat.y, w, h), s);
        }

        // Drops a tutorial plate under the garden multiplier chip. A plate that
        // already misses the chip stays put. Splash has no chip.
        Rect ClearCaptionOfStamp(Rect seat, float s)
        {
            if (!GardenStampLive()) return seat;
            var stamp = GardenStampRect(s);
            float m = 8f * s;
            var zone = new Rect(stamp.x - m, stamp.y - m, stamp.width + m * 2f, stamp.height + m * 2f);
            var panel = CoachPanelRect(seat);
            if (!panel.Overlaps(zone)) return seat;
            float padY = seat.y - panel.y;
            float want = zone.yMax + 4f + padY;
            float cap = Screen.height - seat.height - 8f;
            if (want > cap) want = cap;
            if (want < 8f) want = 8f;
            if (want <= seat.y) return seat;
            var dropped = seat;
            dropped.y = want;
            if (TutorTargetLow() && TutorCaptionBlocked(dropped, s)) return seat;
            seat.y = want;
            return seat;
        }

        // Low aim: plate above the target and the arc. Anything else keeps the
        // seat FitTutorSeat already used. Set once by SeatTutorialCaption.
        Rect PlaceTutorCaption(Rect preferred, float s, bool giftDrop)
        {
            if (!TutorTargetLow())
                return FitTutorSeat(preferred, s, giftDrop);
            if (CaptionAlreadyAbove(preferred, s))
                return preferred;
            return CaptionAboveTarget(preferred, s);
        }

        bool TutorTargetLow()
        {
            float h = Screen.height;
            if (h < 2f) return false;
            if (_gloveAct == TutorGloveAct.None && !_glovePosing && !_gloveLockOn) return false;
            if (_cueAimGui.x < -40f || _cueAimGui.x > Screen.width + 40f) return false;
            if (_cueAimGui.y < 0f || _cueAimGui.y > h + 40f) return false;
            return _cueAimGui.y > h * TutorAimLow;
        }

        // True when the plate already sits above the aim and the arc, and misses
        // the target, the path, the feeders, and the top birds.
        bool CaptionAlreadyAbove(Rect text, float s)
        {
            if (TutorCaptionBlocked(text, s)) return false;
            float gap = CaptionGloveMargin * s;
            float limit = _cueAimGui.y - gap;
            if (GloveArcRect(s, false, out var sweep) && sweep.yMin < limit)
                limit = sweep.yMin - gap;
            return text.yMax <= limit;
        }

        bool TutorCaptionBlocked(Rect text, float s)
        {
            if (text.width < 2f || text.height < 2f) return false;
            var panel = CoachPanelRect(text);
            float margin = CaptionGloveMargin * s;
            if (GloveArcRect(s, false, out var sweep))
            {
                var zone = new Rect(
                    sweep.xMin - margin,
                    sweep.yMin - margin,
                    sweep.width + margin * 2f,
                    sweep.height + margin * 2f);
                if (panel.Overlaps(zone)) return true;
            }
            float pad = 28f * s;
            var target = new Rect(_cueAimGui.x - pad, _cueAimGui.y - pad, pad * 2f, pad * 2f);
            if (panel.Overlaps(target)) return true;
            return TopGardenHits(panel, margin, out _);
        }

        // Just above the path, and under the feeders and the top birds.
        Rect CaptionAboveTarget(Rect preferred, float s)
        {
            float margin = CaptionGloveMargin * s;
            float gap = margin + 4f;
            float h = preferred.height;
            float block = _cueAimGui.y;
            if (GloveArcRect(s, false, out var sweep) && sweep.yMin < block)
                block = sweep.yMin;
            float aimPad = 32f * s;
            float aimTop = _cueAimGui.y - aimPad;
            if (aimTop < block) block = aimTop;

            float minY = CaptionFloorY(s);
            TopGardenHits(new Rect(-10000f, -10000f, 1f, 1f), margin, out float gardenBottom);
            if (gardenBottom > 8f)
            {
                float under = gardenBottom + gap;
                if (under > minY) minY = under;
            }
            float y = block - gap - h;
            if (y < minY) y = minY;
            if (y < 4f) y = 4f;
            float bot = Screen.height - h - 8f;
            if (y > bot) y = bot;

            var seat = preferred;
            seat.y = y;
            if (!TutorCaptionBlocked(seat, s)) return seat;

            float leftX = Mathf.Max(8f, Screen.safeArea.xMin + 6f);
            float rightX = Screen.width - preferred.width - Mathf.Max(8f, Screen.width - Screen.safeArea.xMax + 6f);
            if (rightX < leftX) rightX = leftX;
            var left = seat;
            left.x = leftX;
            var right = seat;
            right.x = rightX;
            bool aimRight = _cueAimGui.x >= Screen.width * 0.5f;
            if (aimRight && !TutorCaptionBlocked(left, s)) return left;
            if (!aimRight && !TutorCaptionBlocked(right, s)) return right;
            if (!TutorCaptionBlocked(left, s)) return left;
            if (!TutorCaptionBlocked(right, s)) return right;
            return seat;
        }

        // Preferred stays when its plate already misses the glove sweep and the
        // top garden. Otherwise PlaceCaption sits under that band. A side seat
        // keeps the preferred y. ParkOffGlove is the last glove dodge. The gift
        // drop is added only when this sentence is the gift line and the chosen
        // seat is lower than the preferred one.
        Rect FitTutorSeat(Rect preferred, float s, bool giftDrop)
        {
            float margin = CaptionGloveMargin * s;
            bool haveGlove = GloveArcRect(s, false, out var sweep);
            var zone = preferred;
            if (haveGlove)
                zone = new Rect(sweep.xMin - margin, sweep.yMin - margin, sweep.width + margin * 2f, sweep.height + margin * 2f);

            bool HitsGlove(Rect text)
            {
                if (!haveGlove) return false;
                return CoachPanelRect(text).Overlaps(zone);
            }

            bool HitsGarden(Rect text, out float low)
            {
                return TopGardenHits(CoachPanelRect(text), margin, out low);
            }

            // Daily claim lives in the petals, so the bloom is not a block there.
            // Other splash lines stay off the logo and the LEVEL flower.
            bool Blocked(Rect text)
            {
                if (_dailyOpen)
                {
                    float words = FlowerLevelTop() - 4f;
                    return text.yMax > words;
                }
                if (!_splash) return false;
                var panel = CoachPanelRect(text);
                var flower = FlowerPlayRect();
                if (flower.width > 2f && panel.Overlaps(flower)) return true;
                var halo = SplashTitleHalo();
                return halo.height > 2f && panel.Overlaps(halo);
            }

            Rect WithGiftDrop(Rect cand)
            {
                if (!giftDrop || cand.y <= preferred.y + 0.5f) return cand;
                float kept = cand.y;
                cand.y += GiftCaptionDrop * s;
                float cap = Screen.height - cand.height - 8f;
                if (cand.y > cap) cand.y = cap;
                if (cand.y < 8f) cand.y = 8f;
                if (HitsGlove(cand) || HitsGarden(cand, out _) || Blocked(cand))
                    cand.y = kept;
                return cand;
            }

            bool ClearOfBoth(Rect text)
            {
                return !HitsGlove(text) && !HitsGarden(text, out _) && !Blocked(text);
            }

            bool gardenHit = HitsGarden(preferred, out float gardenBottom);
            bool gloveHit = HitsGlove(preferred);
            if (!gloveHit && !gardenHit)
                return preferred;

            float band = -1f;
            if (gloveHit) band = zone.yMax;
            if (gardenHit)
            {
                float g = gardenBottom + margin;
                if (g > band) band = g;
            }
            const float padY = 12f;
            float belowY = band + padY - 8f * s;
            var under = WithGiftDrop(PlaceCaption(s, preferred.width, preferred.height, belowY));
            if (ClearOfBoth(under))
                return under;

            Rect gloveClear = preferred;
            bool foundGloveClear = false;
            if (!HitsGlove(under) && !Blocked(under))
            {
                gloveClear = under;
                foundGloveClear = true;
            }
            if (haveGlove)
            {
                float aboveY = zone.yMin - margin - padY - preferred.height;
                if (aboveY >= 8f)
                {
                    var above = preferred;
                    above.y = aboveY;
                    above = WithGiftDrop(above);
                    if (ClearOfBoth(above))
                        return above;
                    if (!foundGloveClear && !HitsGlove(above) && !Blocked(above))
                    {
                        gloveClear = above;
                        foundGloveClear = true;
                    }
                }
                float leftX = zone.xMin - margin - preferred.width;
                float rightX = zone.xMax + margin;
                var left = new Rect(leftX, preferred.y, preferred.width, preferred.height);
                var right = new Rect(rightX, preferred.y, preferred.width, preferred.height);
                bool leftOk = leftX >= 4f && ClearOfBoth(left);
                bool rightOk = right.xMax <= Screen.width - 4f && ClearOfBoth(right);
                if (leftOk || rightOk)
                {
                    bool wantLeft = _cueAimGui.x >= Screen.width * 0.5f;
                    if (wantLeft && leftOk) return left;
                    if (!wantLeft && rightOk) return right;
                    if (leftOk) return left;
                    return right;
                }
                if (!leftOk && leftX >= 4f && !HitsGlove(left) && !Blocked(left) && !foundGloveClear)
                {
                    gloveClear = left;
                    foundGloveClear = true;
                }
                if (!rightOk && right.xMax <= Screen.width - 4f && !HitsGlove(right) && !Blocked(right) && !foundGloveClear)
                {
                    gloveClear = right;
                    foundGloveClear = true;
                }
                var parked = WithGiftDrop(ParkOffGlove(preferred, s));
                if (ClearOfBoth(parked))
                    return parked;
                if (!foundGloveClear && !HitsGlove(parked) && !Blocked(parked))
                {
                    gloveClear = parked;
                    foundGloveClear = true;
                }
            }
            return foundGloveClear ? gloveClear : preferred;
        }

        // Feeders and the top row only, and only while the garden is the playfield.
        // bottom is the lowest GUI edge of that band. True when the plate touches it.
        bool TopGardenHits(Rect panel, float margin, out float bottom)
        {
            bottom = 0f;
            if (_splash || _home != HomeFace.Splash) return false;
            if (_levelHive || _hiveInspect >= 0 || _gift != GiftFace.None) return false;
            var cam = _garden.Cam;
            if (cam == null) return false;
            bool any = false;
            bool hit = false;
            float lowEdge = 0f;
            void Take(Bounds world)
            {
                if (!ProjectGui(cam, world, 0f, out var box)) return;
                if (!any || box.yMax > lowEdge) lowEdge = box.yMax;
                any = true;
                var grown = new Rect(box.xMin - margin, box.yMin - margin, box.width + margin * 2f, box.height + margin * 2f);
                if (panel.Overlaps(grown)) hit = true;
            }
            var feeders = _garden.Feeders;
            if (feeders != null)
            {
                for (int i = 0; i < feeders.Length; i++)
                {
                    var feeder = feeders[i];
                    if (feeder == null) continue;
                    var parts = feeder.GetComponentsInChildren<SpriteRenderer>();
                    for (int p = 0; p < parts.Length; p++)
                    {
                        var sr = parts[p];
                        if (sr == null || !sr.enabled || !sr.gameObject.activeInHierarchy) continue;
                        Take(sr.bounds);
                    }
                }
            }
            var branches = _garden.Branches;
            if (branches != null)
            {
                int n = branches.Length < 2 ? branches.Length : 2;
                for (int i = 0; i < n; i++)
                {
                    var view = branches[i];
                    if (view == null) continue;
                    var wood = view.Wood;
                    if (wood != null && wood.enabled && wood.gameObject.activeInHierarchy)
                        Take(wood.bounds);
                    var birds = view.Birds;
                    for (int b = 0; b < birds.Length; b++)
                    {
                        var sr = birds[b];
                        if (sr == null || !sr.enabled || !sr.gameObject.activeInHierarchy) continue;
                        var idle = sr.GetComponent<BirdIdle>();
                        if (idle != null && idle.Shrouded) continue;
                        Take(sr.bounds);
                    }
                }
            }
            bottom = lowEdge;
            return any && hit;
        }

        // Shared tutorial caption: even inset, balanced wrap, one outline weight.
        void PaintCoachCaption(string text, Rect r, float s, int lo, int hi)
        {
            NoteTutorPlate(CoachPanelRect(r));
            var st = CoachLineStyle();
            if (_coachContent == null) _coachContent = new GUIContent();
            float padX = Mathf.Clamp(r.width * 0.055f, 10f * s, 18f * s);
            float padY = Mathf.Clamp(r.height * 0.10f, 6f * s, 14f * s);
            var textR = new Rect(
                r.x + padX,
                r.y + padY,
                Mathf.Max(48f, r.width - padX * 2f),
                Mathf.Max(24f, r.height - padY * 2f));
            if (_coachSizedFor != text || Mathf.Abs(_coachSizedW - textR.width) > 1f || Mathf.Abs(_coachSizedH - textR.height) > 1f)
            {
                int px = FitFontWrapped(st, text, textR.width, textR.height, lo, hi);
                string shown = BalanceWrap(st, text, textR.width, px);
                st.wordWrap = true;
                st.fontSize = px;
                _coachContent.text = shown;
                int guard = 0;
                while (px > lo && guard < 24 && st.CalcHeight(_coachContent, textR.width) > textR.height + 1f)
                {
                    px--;
                    st.fontSize = px;
                    shown = BalanceWrap(st, text, textR.width, px);
                    _coachContent.text = shown;
                    guard++;
                }
                _coachSizedPx = px;
                _coachShown = shown;
                _coachSizedFor = text;
                _coachSizedW = textR.width;
                _coachSizedH = textR.height;
            }
            st.fontSize = _coachSizedPx;
            st.wordWrap = true;
            float fade = EaseOutCubic(_coachFade);
            DrawCoachPanel(r, fade);
            int black = Mathf.Clamp(Mathf.CeilToInt(CoachOutlinePx * s), CoachOutlinePx, 8);
            StampOutlined(textR, _coachShown ?? text, st, new Color(1f, 0.98f, 0.90f, fade), 0, black);
        }

        // Glove poke plus each lifted bird, once per line placement. The scan below only
        // tests this list, so it does not walk the flock on every candidate row.
        void CoachFillBlocks(float s)
        {
            if (_blocks == null || _blocks.Length < 48) _blocks = new ScreenBox[48];
            _blockN = 0;
            float pad = 12f * s;
            if (_gloveVis)
            {
                AddGlove(_gloveShown, _gloveShownAng, s, pad, _gloveMirror);
                AddArcPose(0f, s, pad);
                AddArcPose(0.35f, s, pad);
                AddArcPose(0.55f, s, pad);
                AddArcPose(1f, s, pad);
            }
            if (_restVis)
            {
                AddGlove(_restShown, _restAng, s, pad, _restMirror);
                AddRestPose(0.06f, s, s, pad);
                AddRestPose(_restTravelHi, s, s, pad);
            }
            AddPestBlock(pad);
            AddLiftedBirds(pad);
            AddGardenObstacles(pad);
            AddStampBlock(s, pad);
        }

        void AddStampBlock(float s, float pad)
        {
            if (!GardenStampLive() || _blocks == null || _blockN >= _blocks.Length) return;
            var stamp = GardenStampRect(s);
            _blocks[_blockN].X0 = stamp.xMin - pad;
            _blocks[_blockN].Y0 = stamp.yMin - pad;
            _blocks[_blockN].X1 = stamp.xMax + pad;
            _blocks[_blockN].Y1 = stamp.yMax + pad;
            _blockN++;
        }

        void AddRestPose(float t, float layoutS, float drawS, float pad)
        {
            if (_restAway.sqrMagnitude < 0.0001f) return;
            TapArc(_restTip, _restAim, _restAway, layoutS, t, out var pos, out float ang, _restMirror);
            bool restMirror = _restMirror;
            SeatGlove(ref pos, ref ang, _restAim, layoutS, ref restMirror, 0f);
            AddGlove(pos, ang, drawS, pad, _restMirror);
        }

        void AddArcPose(float t, float s, float pad)
        {
            if (_gloveAway.sqrMagnitude < 0.0001f) return;
            TapArc(_gloveTip, _cueAimGui, _gloveAway, s, t, out var pos, out float ang, _gloveMirror);
            bool arcMirror = _gloveMirror;
            SeatGlove(ref pos, ref ang, _cueAimGui, s, ref arcMirror, 0f);
            AddGlove(pos, ang, s, pad, _gloveMirror);
        }

        void AddGlove(Vector2 pivot, float ang, float s, float pad, bool mirror = false)
        {
            if (_blockN >= _blocks.Length) return;
            var tex = CoachGloveTex();
            if (tex == null) return;
            float dh = GloveDh(s);
            var rect = GloveRect(pivot, dh, mirror);
            if (!RotAabb(rect.x, rect.y, rect.width, rect.height, pivot, ang, pad, out var box)) return;
            _blocks[_blockN].X0 = box.xMin;
            _blocks[_blockN].Y0 = box.yMin;
            _blocks[_blockN].X1 = box.xMax;
            _blocks[_blockN].Y1 = box.yMax;
            _blockN++;
        }

        void AddGardenObstacles(float pad)
        {
            var cam = _garden.Cam;
            if (cam == null) return;
            var feeders = _garden.Feeders;
            if (feeders != null)
            {
                for (int i = 0; i < feeders.Length; i++)
                {
                    var f = feeders[i];
                    if (f == null) continue;
                    var sr = f.GetComponentInChildren<SpriteRenderer>();
                    if (sr != null && sr.enabled) AddBounds(cam, sr.bounds, pad);
                }
            }
            var branches = _garden.Branches;
            if (branches == null) return;
            for (int i = 0; i < branches.Length; i++)
            {
                var view = branches[i];
                if (view == null) continue;
                var sr = view.GetComponent<SpriteRenderer>();
                if (sr != null && sr.enabled) AddBounds(cam, sr.bounds, pad);
            }
        }

        void AddBounds(Camera cam, Bounds b, float pad)
        {
            if (_blockN >= _blocks.Length) return;
            if (!ProjectGui(cam, b, pad, out var box)) return;
            _blocks[_blockN].X0 = box.xMin;
            _blocks[_blockN].Y0 = box.yMin;
            _blocks[_blockN].X1 = box.xMax;
            _blocks[_blockN].Y1 = box.yMax;
            _blockN++;
        }

        // Sprite bounds in GUI space. Shared by the keepout list and the top-row test.
        static bool ProjectGui(Camera cam, Bounds b, float pad, out Rect box)
        {
            box = default;
            if (cam == null) return false;
            var a = cam.WorldToScreenPoint(new Vector3(b.min.x, b.max.y, 0f));
            var c = cam.WorldToScreenPoint(new Vector3(b.max.x, b.min.y, 0f));
            if (a.z < 0f && c.z < 0f) return false;
            float x0 = (a.x < c.x ? a.x : c.x) - pad;
            float x1 = (a.x > c.x ? a.x : c.x) + pad;
            float y0 = Screen.height - (a.y > c.y ? a.y : c.y) - pad;
            float y1 = Screen.height - (a.y < c.y ? a.y : c.y) + pad;
            box = new Rect(x0, y0, Mathf.Max(0f, x1 - x0), Mathf.Max(0f, y1 - y0));
            return box.width > 1f && box.height > 1f;
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
            bool fading = _gloveAct == TutorGloveAct.FadeOutOnTap;
            if (_gloveAlpha < 0.03f) return;
            if (!fading && !_gloveVis) return;
            float dip = _gloveHoldPose ? _gloveHoldDip : _gloveDip;
            DrawGloveAt(_gloveShown, _gloveShownAng, GloveDh(s), _gloveAlpha, dip, _gloveMirror);
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
            if (!_gloveHoldPose && float.IsNaN(_gloveApproach))
                ang = ClampUpright(mirror);
            // Small poke along the finger, not a vertical bob. Zero at full dip so the
            // tap frame's fingertip is the pivot. Held still while a tap fades the hand.
            // Unscaled so GamePause does not freeze it.
            float bob = _gloveHoldPose ? 0f : Mathf.Sin(Time.unscaledTime * 2.35f) * dh * 0.028f * (1f - dip);
            float rad = ang * Mathf.Deg2Rad;
            pivot += new Vector2(Mathf.Sin(rad), -Mathf.Cos(rad)) * bob;
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

        // One glove. The mirrored pair on the second reward sign is not posed.
        void PoseRestGlove()
        {
            _restVis = false;
            _restMirror = false;
        }

        void DrawRestGlove(float s)
        {
            if (!_restVis || _coachFade < 0.03f) return;
            DrawGloveAt(_restShown, _restAng, GloveDh(s), EaseOutCubic(_coachFade), TapDip(_glovePhase), _restMirror);
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
                if (!TipBody(view, tip, out focus))
                    focus = SeatOrWood(view, tip, view.transform.position);
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
            {
                focus = view.Sign.position;
                var sign = view.Sign.GetComponent<SpriteRenderer>();
                if (sign != null && sign.sprite != null) focus = sign.bounds.center;
            }
            var sp = cam.WorldToScreenPoint(focus);
            if (sp.z < 0f) return false;
            gui = new Vector2(sp.x, Screen.height - sp.y);
            return true;
        }

        void DrawCoach(float s, float top)
        {
            // The gift wash covers this pass. The Watch lesson is painted after it.
            if (_adHand) return;
            if (_levelHive)
            {
                _restVis = false;
                return;
            }
            PoseRestGlove();
            if (_cueLine != null)
            {
                GardenLineBox(s, out _, out float capH);
                int capHi = Mathf.RoundToInt(32f * s);
                if (_leafIntro || _pestCue != 0 || _hiveLevelLive || _hiveIntroLive)
                    DrawCoachLine(_cueLine, s, top, capH, capHi);
                else
                    DrawCoachLine(_cueLine, s, top, capH);
            }
            DrawTutorOverlay(s);
        }

        // Shared tutorial overlay. The caller paints the caption first.
        // The glove is last, over the plate and every other tutorial mark.
        void DrawTutorOverlay(float s)
        {
            if (_restVis)
                DrawRestGlove(s);
            DrawCoachGlove(s);
        }

        // Move a caption off a dialog. Above, then below, then a thin strip in the free gap.
        static Rect ClearOfDialog(Rect line, Rect dialog, float s)
        {
            if (dialog.width < 8f || dialog.height < 8f || !line.Overlaps(dialog)) return line;
            float gap = 10f * s;
            float above = dialog.y - gap - line.height;
            if (above >= 4f)
            {
                line.y = above;
                return line;
            }
            float below = dialog.yMax + gap;
            if (below + line.height <= Screen.height - 4f)
            {
                line.y = below;
                return line;
            }
            float room = dialog.y - gap - 4f;
            if (room > 28f * s)
            {
                line.y = 4f;
                line.height = Mathf.Max(28f * s, room - 4f);
                return line;
            }
            float roomB = Screen.height - 4f - below;
            if (roomB > 28f * s)
            {
                line.y = below;
                line.height = roomB;
            }
            return line;
        }

        // Play-flower keepouts cover the whole bloom. The LEVEL words sit lower.
        // A claim line under the button lives in the petals above those words.
        void LiftLevelWords()
        {
            var platform = FlowerPlayRect();
            if (platform.width < 2f || _blocks == null) return;
            const float plateY = 12f;
            float words = FlowerLevelTop() - plateY;
            float wide = platform.width * 0.75f;
            float low = Screen.height * 0.72f;
            for (int i = 0; i < _blockN; i++)
            {
                float w = _blocks[i].X1 - _blocks[i].X0;
                if (w < wide || _blocks[i].Y1 < low || _blocks[i].Y0 >= words) continue;
                _blocks[i].Y0 = words;
            }
        }

        // Held so a caption does not jump every frame. Once a sentence is held,
        // that x,y stays. The glove arc must not move it. Forbidden on the first
        // seat: the glove's drawn rect over its whole tap arc, the LEVEL flower,
        // both side rails, and the bird plus its name plate. holdWant keeps the
        // caller's seat when the only hit is the bloom above LEVEL.
        Rect NudgeCaption(string key, Rect want, float s, Rect obstacle = default, bool holdWant = false)
        {
            CoachFillBlocks(s);
            AddSplashKeepouts(s);
            AddAimBlock(s);
            AddDailyFrameKeepout(s);
            AddTutorKeepouts(s);
            // Claim line sits under the button, in the petals above the LEVEL words.
            // The full play-flower block has no room there and was throwing the bar to the top.
            if (holdWant)
                LiftLevelWords();
            if (obstacle.width > 2f && obstacle.height > 2f)
            {
                if (_blocks == null || _blockN >= _blocks.Length) GrowBlocks();
                if (_blocks != null && _blockN < _blocks.Length)
                {
                    // CoachPanelRect hangs 18×12 past the text. A smaller pad still covers the frame.
                    float px = 6f * s;
                    float py = 6f * s;
                    if (px < 18f) px = 18f;
                    if (py < 12f) py = 12f;
                    _blocks[_blockN].X0 = obstacle.xMin - px;
                    _blocks[_blockN].Y0 = obstacle.yMin - py;
                    _blocks[_blockN].X1 = obstacle.xMax + px;
                    _blocks[_blockN].Y1 = obstacle.yMax + py;
                    _blockN++;
                }
            }
            if (_coachLineFor != key)
            {
                _coachLineFor = key;
                _coachLineHeld = false;
                _coachHoldXOn = false;
            }
            if (_coachLineHeld)
            {
                var held = want;
                held.y = _coachLineHold;
                if (_coachHoldXOn) held.x = _coachLineHoldX;
                return held;
            }
            if (CaptionSeat(want, s, out var placed, holdWant))
            {
                _coachLineHold = want.y;
                _coachLineHoldX = want.x;
                _coachHoldXOn = true;
                _coachLineHeld = true;
                return placed;
            }
            // Stay under the claim button. Scanning from the top parks the bar on the logo.
            if (holdWant)
            {
                _coachLineHold = want.y;
                _coachLineHoldX = want.x;
                _coachHoldXOn = true;
                _coachLineHeld = true;
                return want;
            }
            if (SeekOpenCaption(want, want.x, s, out var found))
                return found;
            AvatarChannel(s, out float chL, out float chR);
            const float padX = 18f;
            float innerL = chL + 6f * s + padX;
            float innerR = chR - 6f * s - padX - want.width;
            if (innerL < 4f) innerL = 4f;
            if (innerR < 4f) innerR = 4f;
            if (innerR < innerL) innerR = innerL;
            // The hand sits on the aim's side. Try the open side first.
            bool gloveOnRight = _cueAimGui.x >= Screen.width * 0.5f;
            float prefer = gloveOnRight ? innerL : innerR;
            float other = gloveOnRight ? innerR : innerL;
            if (Mathf.Abs(prefer - want.x) > 1f && SeekOpenCaption(want, prefer, s, out found))
                return found;
            if (Mathf.Abs(other - want.x) > 1f && Mathf.Abs(other - prefer) > 1f
                && SeekOpenCaption(want, other, s, out found))
                return found;
            _coachLineHeld = false;
            _coachHoldXOn = false;
            if (!BlocksHit(want)) return want;
            return ParkOffGlove(want, s);
        }

        // Highest row at this x that misses every forbidden rect. Holds that seat.
        bool SeekOpenCaption(Rect want, float x, float s, out Rect placed)
        {
            placed = want;
            float minY = CaptionFloorY(s);
            float maxY = Screen.height - want.height - 4f;
            if (maxY < minY) return false;
            for (float y = minY; y <= maxY; y += 6f)
            {
                var probe = want;
                probe.x = x;
                probe.y = y;
                if (!CaptionSeat(probe, s, out placed)) continue;
                _coachLineHold = y;
                _coachLineHoldX = x;
                _coachHoldXOn = true;
                _coachLineHeld = true;
                return true;
            }
            return false;
        }

        // Last seat when every scanned row still touches a keepout. Push clear of the
        // glove arc even if a rail has to give, so the bar does not stay on the hand.
        Rect ParkOffGlove(Rect want, float s)
        {
            if (!GloveArcRect(s, true, out var sweep)) return want;
            float gap = 8f * s + 18f;
            var parked = want;
            float above = sweep.yMin - gap - want.height;
            float below = sweep.yMax + gap;
            if (above >= 4f) parked.y = above;
            else if (below + want.height <= Screen.height - 4f) parked.y = below;
            bool overX = parked.xMax > sweep.xMin - gap && parked.x < sweep.xMax + gap;
            bool overY = parked.yMax > sweep.yMin - gap && parked.y < sweep.yMax + gap;
            float leftOf = sweep.xMin - gap - want.width;
            if (overX && overY && leftOf >= 4f)
                parked.x = leftOf;
            if (parked.x < 4f) parked.x = 4f;
            float floorY = CaptionFloorY(s);
            if (parked.y < floorY) parked.y = floorY;
            return parked;
        }

        // LEVEL flower, both rails, the perched bird, and its name plate.
        // The glove's drawn rect, including the whole tap arc, is in the same list.
        void AddTutorKeepouts(float s)
        {
            float pad = 12f + 8f * s;
            if (pad < 18f) pad = 18f;
            AddKeepout(FlowerPlayRect(), pad);
            AddKeepout(PiggyRect(s), pad);
            AddKeepout(SplashHiveRect(), pad);
            AddKeepout(SplashPokerRect(), pad);
            AddKeepout(SplashDailyRect(), pad);
            var vip = SplashNoAdsRect();
            AddKeepout(vip, pad);
            if (vip.width > 2f) AddKeepout(SplashNoAdsRibbon(vip), pad);
            if (!GloveArcRect(s, true, out var sweep)) return;
            AddKeepout(sweep, pad);
        }

        // Union of the glove over this step's arc: the perch (or the angled
        // start), the bow, and the tap. A fade keeps the drawn hand out of this
        // rect so the next plate is seated on the new aim. livePose also takes
        // where the hand is this frame. Slack covers the fingertip bob.
        bool GloveArcRect(float s, bool livePose, out Rect box)
        {
            box = default;
            bool fading = _gloveAct == TutorGloveAct.FadeOutOnTap || _gloveAct == TutorGloveAct.Hidden;
            if (!_gloveVis && !_glovePosing && !fading) return false;
            if (fading && _cueAimGui.sqrMagnitude < 1f && !_glovePosing) return false;
            float dh = GloveDh(s);
            bool any = false;
            float x0 = 0f, y0 = 0f, x1 = 0f, y1 = 0f;
            if (livePose && _gloveVis && !fading)
                UnionGlove(ref any, ref x0, ref y0, ref x1, ref y1, _gloveShown, _gloveShownAng, dh, _gloveDip, _gloveMirror);
            Vector2 rest;
            Vector2 away;
            bool mirror;
            float ang;
            if (_glovePosing || !_gloveVis || fading)
                NaturalGlovePose(s, out rest, out away, out mirror, out ang);
            else
            {
                rest = _gloveRest;
                away = _gloveAway;
                mirror = _gloveMirror;
                ang = _gloveRestAng;
            }
            var aim = fading || _glovePosing || !_gloveLockOn ? _cueAimGui : _gloveLockAim;
            bool angled = !float.IsNaN(_gloveApproach);
            void Take(Vector2 pivot, float poseAng, bool poseMirror, float dip)
            {
                var p = pivot;
                float a = poseAng;
                bool m = poseMirror;
                SeatGlove(ref p, ref a, aim, s, ref m, dip, _gloveApproach);
                UnionGlove(ref any, ref x0, ref y0, ref x1, ref y1, p, a, dh, dip, m);
            }
            Vector2 start = angled ? rest + away * (dh * 0.9f) : rest;
            for (int step = 0; step <= 4; step++)
                Take(Vector2.Lerp(start, rest, step / 4f), ang, mirror, 0f);
            if (away.sqrMagnitude > 0.0001f || angled)
            {
                for (int step = 0; step <= 8; step++)
                {
                    float t = step / 8f;
                    Vector2 pos;
                    float poseAng;
                    if (angled)
                    {
                        pos = Vector2.Lerp(rest, aim, t);
                        poseAng = ang;
                    }
                    else
                        TapArc(rest, aim, away, s, t, out pos, out poseAng, mirror, _gloveApexMinY);
                    Take(pos, poseAng, mirror, 0f);
                    Take(pos, poseAng, mirror, 1f);
                }
            }
            if (!any) return false;
            float slack = dh * 0.028f + 8f;
            x0 -= slack;
            y0 -= slack;
            x1 += slack;
            y1 += slack;
            box = new Rect(x0, y0, Mathf.Max(0f, x1 - x0), Mathf.Max(0f, y1 - y0));
            return box.width > 2f && box.height > 2f;
        }

        static void UnionGlove(ref bool any, ref float x0, ref float y0, ref float x1, ref float y1,
            Vector2 pivot, float ang, float dh, float dip, bool mirror)
        {
            GloveSpan(pivot, ang, dh, dip, mirror, out float gx0, out float gy0, out float gx1, out float gy1);
            if (!any)
            {
                any = true;
                x0 = gx0;
                y0 = gy0;
                x1 = gx1;
                y1 = gy1;
                return;
            }
            if (gx0 < x0) x0 = gx0;
            if (gy0 < y0) y0 = gy0;
            if (gx1 > x1) x1 = gx1;
            if (gy1 > y1) y1 = gy1;
        }

        void GrowBlocks()
        {
            int n = _blocks == null ? 64 : _blocks.Length * 2;
            if (n < 64) n = 64;
            var next = new ScreenBox[n];
            if (_blocks != null)
            {
                int c = _blockN;
                if (c > _blocks.Length) c = _blocks.Length;
                for (int i = 0; i < c; i++) next[i] = _blocks[i];
            }
            _blocks = next;
        }

        // Open daily pop-up only. The frame rect is forbidden so a caption cannot cover
        // the heading. Pad matches CoachPanelRect, or the bar still overlaps the gold.
        void AddDailyFrameKeepout(float s)
        {
            if (!_dailyOpen || _blocks == null || _blockN >= _blocks.Length) return;
            DailyLayout(s, out var card, out _, out var board, out float band);
            var frame = DailyFrameOuter(card, board, band);
            const float px = 18f;
            const float py = 12f;
            _blocks[_blockN].X0 = frame.xMin - px;
            _blocks[_blockN].Y0 = frame.yMin - py;
            _blocks[_blockN].X1 = frame.xMax + px;
            _blocks[_blockN].Y1 = frame.yMax + py;
            _blockN++;
        }

        // Prefer the splash-band seat when it is still clear. Otherwise keep the candidate
        // that already missed every block, so the logo clamp cannot drop the bar on the frame.
        bool CaptionSeat(Rect candidate, float s, out Rect placed, bool pin = false)
        {
            placed = candidate;
            if (candidate.y < 2f || candidate.yMax > Screen.height - 2f) return false;
            if (BlocksHit(candidate)) return false;
            if (pin) return true;
            var seated = SeatSplashCaption(candidate, s);
            if (!BlocksHit(seated)) placed = seated;
            return true;
        }

        // Splash lessons only. Garden PlaceCaption does not call this.
        void AddSplashKeepouts(float s)
        {
            if (!_splash || _home != HomeFace.Splash) return;
            float pad = 12f + 8f * s;
            AddKeepout(FlowerPlayRect(), pad);
            AddKeepout(SplashTitleHalo(), pad);
            AddKeepout(PiggyRect(s), pad);
            AddKeepout(SplashHiveRect(), pad);
            AddKeepout(SplashPokerRect(), pad);
            AddKeepout(SplashDailyRect(), pad);
            var vip = SplashNoAdsRect();
            AddKeepout(vip, pad);
            if (vip.width > 2f) AddKeepout(SplashNoAdsRibbon(vip), pad);
        }

        void AddKeepout(Rect zone, float pad)
        {
            if (zone.width < 2f || zone.height < 2f) return;
            if (_blocks == null || _blockN >= _blocks.Length) GrowBlocks();
            if (_blocks == null || _blockN >= _blocks.Length) return;
            _blocks[_blockN].X0 = zone.xMin - pad;
            _blocks[_blockN].Y0 = zone.yMin - pad;
            _blocks[_blockN].X1 = zone.xMax + pad;
            _blocks[_blockN].Y1 = zone.yMax + pad;
            _blockN++;
        }

        // Panel pad is 12×18. The text rect has to sit inside the free band
        // or the bubble still covers LEVEL, a rail, or the logo.
        Rect SeatSplashCaption(Rect caption, float s)
        {
            if (!_splash || _home != HomeFace.Splash) return caption;
            const float padY = 12f;
            const float padX = 18f;
            float gap = 8f * s;
            float flowerTop = FlowerPlayRect().y;
            float limit = flowerTop - gap - padY;
            var logo = SplashTitleHalo();
            float logoClear = logo.yMax + gap + padY;
            if (caption.yMax + padY > flowerTop - gap)
                caption.y = limit - caption.height;
            if (caption.y - padY < logo.yMax + gap)
            {
                float room = limit - logoClear;
                float minH = 28f * s;
                if (room >= minH)
                {
                    if (caption.height > room) caption.height = room;
                    caption.y = logoClear;
                    if (caption.yMax > limit) caption.y = limit - caption.height;
                }
                else if (limit - minH > 4f)
                {
                    caption.height = Mathf.Max(minH, limit - (logo.yMax + 4f));
                    caption.y = limit - caption.height;
                }
            }
            if (caption.y < 4f) caption.y = 4f;
            AvatarChannel(s, out float chL, out float chR);
            float left = chL + 6f * s + padX;
            float right = chR - 6f * s - padX;
            if (right - left < 80f * s)
            {
                left = 8f;
                right = Screen.width - 8f;
            }
            float span = right - left;
            if (caption.width > span) caption.width = Mathf.Max(80f * s, span);
            if (caption.x < left) caption.x = left;
            if (caption.xMax > right) caption.x = right - caption.width;
            if (caption.x < 4f) caption.x = 4f;
            return caption;
        }

        void AddAimBlock(float s)
        {
            if (_blocks == null || _blockN >= _blocks.Length) return;
            float p = 22f * s;
            var a = _cueAimGui;
            _blocks[_blockN].X0 = a.x - p;
            _blocks[_blockN].Y0 = a.y - p;
            _blocks[_blockN].X1 = a.x + p;
            _blocks[_blockN].Y1 = a.y + p;
            _blockN++;
        }

        void TickHivePop()
        {
            if (!_hivePopping) return;
            _hivePop += PlayClock.Delta / 0.40f;
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

        // One splash lesson at a time. Pending flags stay set; only the turn that
        // wins this frame is live. Ads, the streak board, RewardGap, VIP, the ask,
        // and the welcome card all hold the queue. Order when several are owed:
        // streak board, adoption, a pending welcome, then daily, hive, poker.
        bool SplashLessonRoom()
        {
#if UNITY_EDITOR
            if (_dailyShotQuiet || _pokerPlayrun || EditorShotLive) return false;
#endif
            if (!_splash || _home != HomeFace.Splash || !_railInit) return false;
            if (RewardLessonHold() || _welcomeQueued) return false;
            if (VipOffer.IsOpen || _dailyAskOpen || _welcomeOpen || _welcomeGlove) return false;
            // A card the player opened early waits. The claim glove is the live lesson.
            if (_dailyOpen && !_dailyIntroLive) return false;
            if (Ads.IsBusy || GamePause.Paused) return false;
            return true;
        }

        // Glove and caption wait until the adopted bird is perched on the branch.
        bool SplashDailyTurn()
        {
            if (!SplashLessonRoom() || !_dailyIntro || LevelData.NextPlay < 1) return false;
            if (AdoptHoldsQueue() || !BirdSettledOnBranch() || WelcomeOwnsTurn()) return false;
            return true;
        }

        bool SplashHiveTurn()
        {
            if (!SplashLessonRoom() || !_hiveIntro || _dailyIntro || AdoptHoldsQueue()) return false;
            if (WelcomeOwnsTurn()) return false;
            return true;
        }

        bool SplashPokerTurn()
        {
            if (!SplashLessonRoom() || !_pokerIntro || LevelData.NextPlay < 1) return false;
            if (_dailyIntro || _hiveIntro || AdoptHoldsQueue() || WelcomeOwnsTurn()) return false;
            return true;
        }

        // Streak toast, an ad, or an earlier lesson owns the home screen. Hive waits its turn.
        void TickHiveIntro()
        {
            bool live = SplashHiveTurn();
            if (!live)
            {
                if (_hiveIntroLive && HoldLiveLesson()) return;
                bool was = _hiveIntroLive;
                _hiveIntroLive = false;
                if (was)
                {
                    _gloveVis = false;
                    if (string.Equals(_cueLine, HiveHomeLine)) _cueLine = null;
                }
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
            _coachFade = Mathf.Min(1f, _coachFade + PlayClock.Delta / 0.30f);
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
            if (Hive.LegacyAdopted)
            {
                MarkHiveCoach();
                return;
            }
            if (PlayerPrefs.GetInt(CoachHiveKey, 0) != 0) return;
            // Dawn Garden (level 1) is the first board with inner bees.
            // The home lesson waits until that clear, same gate as the poker intro.
            if (LevelData.NextPlay < 1) return;
            _hiveIntro = true;
            if (_dailyIntro) return;
            _gloveReady = false;
            _gloveVis = false;
            _coachFade = 0f;
            TickHiveIntro();
        }

        void DismissHiveIntro()
        {
            bool any = _hiveIntro || _hiveIntroLive || _hiveLevelLive || _hiveLevelCue;
            if (!any) return;
            // The home lesson stamps only while it is on the splash.
            // Opening the garden hive must not consume that one-time flag.
            if (_hiveIntroLive || (_hiveIntroSaw && !_hiveLevelLive)) MarkHiveCoach();
            bool dropLevel = _hiveLevelLive || _levelHive;
            _hiveIntro = false;
            _hiveIntroLive = false;
            _hiveIntroSaw = false;
            if (dropLevel)
            {
                _hiveLevelCue = false;
                _hiveLevelLive = false;
            }
            _cueHand = false;
            _cueLine = null;
            _gloveVis = false;
            _gloveReady = false;
            _coachFade = 0f;
        }

        void NoteHiveIntroLeft()
        {
            _hiveIntro = false;
            _hiveIntroLive = false;
            _hiveIntroSaw = false;
            _hiveLevelCue = false;
            _hiveLevelLive = false;
        }

        bool BoardHasBees()
        {
            if (_board == null) return false;
            var list = _board.Branches;
            for (int i = 0; i < list.Count; i++)
            {
                var b = list[i];
                int n = b.Count;
                for (int k = 0; k < n - 1; k++)
                    if (b.IsShrouded(k)) return true;
            }
            return false;
        }

        // First board that actually has inner-shroud bees. Queued behind the other
        // intros and never opened over an ad. Bee Thicket is the first authored
        // hive level; Dawn Garden also hides inner birds, so the check is the board.
        void ArmLevelBeeIntro()
        {
            _hiveLevelLive = false;
            if (PlayerPrefs.GetInt(CoachHiveKey, 0) != 0) return;
            if (Hive.LegacyAdopted) return;
            if (Ads.IsBusy) return;
            if (!BoardHasBees()) return;
            _hiveLevelCue = true;
        }

        void TickLevelBee()
        {
            if (!_hiveLevelCue) return;
            if (_hiveLevelLive)
            {
                _hiveIntro = true;
                _cueHand = true;
                CueLine(HiveIntroLine);
                _coachFade = Mathf.Min(1f, _coachFade + PlayClock.Delta / 0.30f);
                return;
            }
            if (HoldLiveLesson()) return;
            if (_splash || _won || Ads.IsBusy || Ads.IsShowing) return;
            if (_coach || _leafIntro || _pestCue != 0 || _adHand || _gift != GiftFace.None) return;
            _hiveLevelLive = true;
            _hiveIntro = true;
            _gloveReady = false;
            _glovePhase = 0f;
            _coachFade = 0f;
            CueLine(HiveIntroLine);
        }

        Vector2 LevelHiveAim()
        {
            HudLayout(out _, out _, out _, out _, out var hive);
            return TopTouch(hive);
        }

        bool TutorialGuideLive() =>
            _pestCue != 0 || _hiveLevelLive || _hiveIntroLive || _pokerIntroLive
            || _dailyIntroLive || _leafIntro || _adHand || _welcomeGlove || _adoptLive
            || _albumTutorOn || (_coach && _cueHand);

        // One arbiter for every non-tutorial card. A live lesson blocks every
        // kind except the card that lesson owns. Player taps are refused.
        // Automatic rewards stay on one mask until the lesson is gone.
        enum PopupKind { None = 0, Gift = 1, Daily = 2, Streak = 4, Welcome = 8, Vip = 16, Ask = 32 }

        int _popupDefer;

        bool OtherTutorialLive()
        {
            return _tutorPause != 0
                || _hiveTutorOn
                || _hiveIntroLive
                || _hiveLevelLive
                || _pokerIntroLive
                || _dailyIntroLive
                || _leafIntro
                || _pestCue != 0
                || _adoptLive
                || _welcomeGlove
                || _pokerPageOn
                || _pokerDealHint
                || _adHand
                || _cueGift
                || (_coach && _cueHand);
        }

        bool TutorialStepActive() => OtherTutorialLive() || _albumTutorOn;

        // Ads, pause, a modal card, or a lesson that is already up. A lesson that
        // is itself running does not count. New lessons call this before they arm.
        bool TutorModalUp()
        {
            if (HoldLiveLesson() || Ads.IsShowing) return true;
            if (RewardLessonHold() || _welcomeQueued) return true;
            if (VipOffer.IsOpen || _dailyOpen || _dailyAskOpen || _welcomeOpen || _welcomeGlove || _adoptLive)
                return true;
            if (_gift != GiftFace.None) return true;
            return false;
        }

        bool TutorialGateClear()
        {
#if UNITY_EDITOR
            if (_dailyShotQuiet || _pokerPlayrun || EditorShotLive) return false;
#endif
            if (TutorModalUp()) return false;
            if (OtherTutorialLive()) return false;
            return true;
        }

        PopupKind TutorialOwnPopup()
        {
            if (_adHand || _cueGift) return PopupKind.Gift;
            if (_dailyIntroLive) return PopupKind.Daily;
            if (_welcomeGlove) return PopupKind.Vip;
            return PopupKind.None;
        }

        bool PopupBlocked(PopupKind kind)
        {
            if (kind == PopupKind.None || !TutorialStepActive()) return false;
            return TutorialOwnPopup() != kind;
        }

        bool IsTutorialBlocking => TutorialStepActive() && TutorialOwnPopup() == PopupKind.None;

        // True when this open may proceed. A blocked automatic open is remembered once.
        bool GatePopup(PopupKind kind, bool fromPlayer)
        {
            if (!PopupBlocked(kind)) return true;
            if (!fromPlayer) _popupDefer |= (int)kind;
            return false;
        }

        void TickPopups()
        {
            if (_popupDefer == 0) return;
            int want = _popupDefer;
            _popupDefer = 0;
            if ((want & (int)PopupKind.Ask) != 0) OpenAskPopup();
            if ((want & (int)PopupKind.Streak) != 0) OpenStreakBoard();
            // A freeze card waits out the lesson. A bit with no freeze left is dropped.
            if ((want & (int)PopupKind.Gift) != 0 && (_frozen || _freezeOffer || _gift != GiftFace.None))
                RaiseGiftCard();
            if ((want & (int)PopupKind.Welcome) != 0) OpenDeferredWelcome();
        }

        // Pause, an ad, or the resume swallow. A live lesson stays on its step.
        bool HoldLiveLesson()
        {
            return GamePause.Paused || Ads.IsBusy || PlayClock.Now < _resumeInputUntil;
        }

        // Same aim, arc restarted. The lesson flags stay. A suspension is not a tap.
        void FreshTutorGlove()
        {
            if (!TutorialStepActive()) return;
            GloveVeilReset();
            _gloveVis = false;
            _gloveReady = false;
            _glovePhase = 0f;
            _gloveDip = 0f;
            _tapSent = false;
            _tutorSeatOn = false;
        }

        void HoldTutorPause(bool hold)
        {
            if (hold)
            {
                if (_tutorPause != 0 || Ads.IsShowing) return;
                GamePause.Push();
                _tutorPause = 1;
                return;
            }
            ReleaseTutorPause();
        }

        void ReleaseTutorPause()
        {
            if (_tutorPause == 0) return;
            GamePause.Pop();
            _tutorPause = 0;
        }

        // Glove, caption, and the completing tap stay alive while the garden is frozen.
        void TickPausedTutorial()
        {
            if (_pestCue == PestCueSparrow)
            {
                if (!PestCueAlive())
                {
                    PestIntroHide();
                    return;
                }
                ApplyPestCue();
            }
            if (Ads.IsShowing) return;
            if (!Pressed(out var screen)) return;
            if (_pestCue != 0)
            {
                PestIntroTap(screen);
                return;
            }
            if (_hiveLevelLive)
            {
                HudLayout(out _, out _, out _, out _, out var hive);
                var gui = new Vector2(screen.x, Screen.height - screen.y);
                if (hive.Contains(gui)) OpenLevelHive();
            }
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
            if (_hiveLevelLive || _hiveIntroLive) return true;
            if (_hiveIntro && !_hiveIntroLive) return false;
            if (PlayerPrefs.GetInt(CoachHiveKey, 0) != 0) return true;
            if (!_splash && Hive.Collected) return true;
            return false;
        }

        bool SplashHiveShown() => HiveReachable();

        // Clear middle of the splash: under the logo and the coin row, above the
        // play flower, inset from every rail button that shares that band.
        Rect SplashIntroBand(float s)
        {
            float flowerTop = FlowerPlayRect().y;
            float titleBottom = TopHud() + (56f * 2f + 4f) * s;
            float hudBottom = titleBottom;
            var pig = SplashRailSeat(RailPig);
            if (pig.height > 1f) hudBottom = Mathf.Max(hudBottom, pig.yMax);
            var vip = SplashRailSeat(RailVip);
            if (vip.height > 1f) hudBottom = Mathf.Max(hudBottom, SplashNoAdsRibbon(vip).yMax);

            float margin = 14f * s;
            float y0 = hudBottom + margin;
            float y1 = flowerTop - margin - 12f;
            if (y1 < y0 + 64f * s)
            {
                y0 = titleBottom + 8f * s;
                y1 = Mathf.Max(y0 + 72f * s, flowerTop - 8f * s);
            }
            float h = Mathf.Min(176f * s, Mathf.Max(96f * s, y1 - y0));
            float y = y0;
            if (y1 - y0 > h) y = y0 + (y1 - y0 - h) * 0.36f;
            if (y + h > y1) y = Mathf.Max(8f, y1 - h);

            float left = Mathf.Max(18f * s, Screen.safeArea.xMin + 12f);
            float right = Screen.width - Mathf.Max(18f * s, Screen.width - Screen.safeArea.xMax + 12f);
            float pad = 12f * s;
            void Eat(Rect obstacle, bool fromLeft)
            {
                if (obstacle.width < 2f || obstacle.height < 2f) return;
                if (obstacle.yMax < y - 2f || obstacle.y > y + h + 2f) return;
                if (fromLeft) left = Mathf.Max(left, obstacle.xMax + pad);
                else right = Mathf.Min(right, obstacle.x - pad);
            }
            Eat(vip, true);
            Eat(SplashNoAdsRect(), true);
            Eat(SplashRailSeat(RailDaily), true);
            Eat(SplashDailyRect(), true);
            Eat(pig, false);
            Eat(PiggyRect(s), false);
            Eat(SplashRailSeat(RailHive), false);
            Eat(SplashHiveRect(), false);
            Eat(SplashRailSeat(RailPoker), false);
            Eat(SplashPokerRect(), false);

            float span = right - left;
            float w = Mathf.Min(span, Screen.width * 0.58f);
            if (w < 96f * s)
            {
                w = Mathf.Min(Screen.width * 0.62f, Screen.width - 36f * s);
                left = (Screen.width - w) * 0.5f;
            }
            else left += (span - w) * 0.5f;
            return new Rect(left, y, w, h);
        }

        void DrawSplashIntroLine(string line, Rect r, float s)
        {
            int hi = Mathf.Max(18, Mathf.RoundToInt(34f * s));
            var seat = SeatTutorialCaption(line, s, r.y, r.width, r.height, r.y, r.x);
            PaintCoachCaption(line, seat, s, 12, hi);
        }

        static Rect HiveHomeCaption(float s)
        {
            float w = Mathf.Min(Screen.width * 0.72f, 420f * s);
            return PlaceCaption(s, w, 52f * s, CaptionFloorY(s));
        }

        // Two lines at the splash caption size. Same width and seat as the hive line.
        static Rect AdHandCaptionRect(float s)
        {
            float w = Mathf.Min(Screen.width * 0.72f, 420f * s);
            return PlaceCaption(s, w, 104f * s, CaptionFloorY(s));
        }

        void DrawHiveIntro(float s)
        {
            if (!_hiveIntroLive) return;
            DrawSplashIntroLine(HiveHomeLine, HiveHomeCaption(s), s);
            DrawTutorOverlay(s);
        }

        // Daily, then hive, then poker. The rail samples a hidden slot until this turn.
        void TickPokerIntro()
        {
            bool live = SplashPokerTurn();
            if (!live)
            {
                if (_pokerIntroLive && HoldLiveLesson()) return;
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
            _coachFade = Mathf.Min(1f, _coachFade + PlayClock.Delta / 0.30f);
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
            if (_dailyIntro || _hiveIntro) return;
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
            if (GuiPaint()) TickPokerWarm();
            DrawSplashIntroLine(PokerIntroLine, PokerIntroBox(s), s);
            DrawTutorOverlay(s);
        }

        // Two lines, width of the longer line, height of that wrap. No spare row.
        // Sits under the logo on the side away from the poker glove (right rail).
        Rect PokerIntroBox(float s)
        {
            AvatarChannel(s, out float chL, out float chR);
            const float padX = 18f;
            float left = chL + 6f * s + padX;
            float right = chR - 6f * s - padX;
            float maxW = right - left;
            if (maxW < 80f * s)
                maxW = Mathf.Min(Screen.width * 0.62f, Screen.width - 36f * s);
            if (maxW < 8f) maxW = 8f;
            var st = CoachLineStyle();
            int hi = Mathf.Max(18, Mathf.RoundToInt(34f * s));
            const int floor = 12;
            int br = PokerIntroLine.IndexOf('\n');
            string top = br > 0 ? PokerIntroLine.Substring(0, br) : PokerIntroLine;
            string bot = br > 0 ? PokerIntroLine.Substring(br + 1) : PokerIntroLine;
            float w = maxW;
            float h = 32f * s;
            for (int fs = hi; fs >= floor; fs--)
            {
                st.fontSize = fs;
                BindFit(st, top, false);
                _fitScratch.fontSize = fs;
                float wa = _fitScratch.CalcSize(_fitContent).x;
                BindFit(st, bot, false);
                _fitScratch.fontSize = fs;
                float wb = _fitScratch.CalcSize(_fitContent).x;
                float need = (wa > wb ? wa : wb) + 8f;
                if (need > maxW && fs > floor) continue;
                float useW = need > maxW ? maxW : need;
                if (useW < 8f) useW = 8f;
                BindFit(st, PokerIntroLine, true);
                _fitScratch.fontSize = fs;
                float th = _fitScratch.CalcHeight(_fitContent, useW);
                BindFit(st, top, false);
                _fitScratch.fontSize = fs;
                float two = _fitScratch.CalcSize(_fitContent).y * 2f;
                if (th > two + 1f) th = two;
                w = useW;
                h = th > 1f ? th : 1f;
                break;
            }
            float x = left;
            if (x + w > right) x = right - w;
            if (x < 4f) x = 4f;
            float gap = 8f * s + 12f;
            float y = CaptionFloorY(s);
            float limit = FlowerPlayRect().y - gap - h;
            if (limit > y && y + h > limit) y = limit;
            float capFloor = CaptionFloorY(s);
            if (y < capFloor) y = capFloor;
            return PlaceCaption(s, w, h, y - 8f * s);
        }

        // First in the splash queue. Hive and poker stay pending until this one ends.
        // The gift stays hidden on a fresh level-1 unlock until this lesson is the
        // one on screen; an older save already shows the button.
        void TickDailyIntro()
        {
            bool live = SplashDailyTurn();
            if (!live)
            {
                if (_dailyIntroLive && HoldLiveLesson()) return;
                bool was = _dailyIntroLive;
                _dailyIntroLive = false;
                if (was) _gloveVis = false;
                return;
            }
            if (!_dailyIntroLive)
            {
                _gloveReady = false;
                _coachFade = 0f;
                _dailyGloveOnClaim = false;
                _glovePhase = 0f;
                _tapSent = false;
            }
            _dailyIntroLive = true;
            _dailyIntroSaw = true;
            _coachFade = Mathf.Min(1f, _coachFade + PlayClock.Delta / 0.30f);
        }

        void MarkDailyCoach()
        {
            if (PlayerPrefs.GetInt(CoachDailyKey, 0) != 0) return;
            PlayerPrefs.SetInt(CoachDailyKey, 1);
            PlayerPrefs.Save();
        }

        void ArmDailyIntro()
        {
            _dailyIntro = false;
            _dailyIntroLive = false;
            _dailyIntroSaw = false;
            _dailyGloveOnClaim = false;
#if UNITY_EDITOR
            if (_dailyShotQuiet || _pokerPlayrun || EditorShotLive) return;
#endif
            if (LevelData.NextPlay < 1) return;
            if (PlayerPrefs.GetInt(CoachDailyKey, 0) != 0) return;
            _dailyIntro = true;
            _gloveReady = false;
            _gloveVis = false;
            _coachFade = 0f;
            TickDailyIntro();
        }

        void DismissDailyIntro()
        {
            if (!_dailyIntro) return;
            if (_dailyIntroSaw) MarkDailyCoach();
            _dailyIntro = false;
            _dailyIntroLive = false;
            _dailyIntroSaw = false;
            _dailyGloveOnClaim = false;
            _gloveVis = false;
            _gloveReady = false;
            _coachFade = 0f;
        }

        void NoteDailyIntroLeft()
        {
            if (_dailyIntroSaw) MarkDailyCoach();
            _dailyIntro = false;
            _dailyIntroLive = false;
            _dailyIntroSaw = false;
            _dailyGloveOnClaim = false;
            _dailyOpen = false;
        }

        void DrawDailyIntro(float s)
        {
            if (!_dailyIntroLive || _dailyOpen || _dailyAskOpen) return;
            DrawSplashIntroLine(DailyIntroLine, DailyIntroBox(s), s);
            DrawTutorOverlay(s);
        }

        // Two lines, width of the longer line, height of that wrap. No spare row under the words.
        Rect DailyIntroBox(float s)
        {
            var band = SplashIntroBand(s);
            var st = CoachLineStyle();
            int hi = Mathf.Max(18, Mathf.RoundToInt(34f * s));
            float maxW = band.width > 48f ? band.width : Mathf.Min(Screen.width * 0.72f, 420f * s);
            int br = DailyIntroLine.IndexOf('\n');
            string top = br > 0 ? DailyIntroLine.Substring(0, br) : DailyIntroLine;
            string bot = br > 0 ? DailyIntroLine.Substring(br + 1) : DailyIntroLine;
            float w = maxW;
            float h = 32f * s;
            for (int fs = hi; fs >= 16; fs--)
            {
                st.fontSize = fs;
                BindFit(st, top, false);
                _fitScratch.fontSize = fs;
                float wa = _fitScratch.CalcSize(_fitContent).x;
                BindFit(st, bot, false);
                _fitScratch.fontSize = fs;
                float wb = _fitScratch.CalcSize(_fitContent).x;
                float need = (wa > wb ? wa : wb) + 8f;
                if (need > maxW && fs > 16) continue;
                float useW = need > maxW ? maxW : need;
                if (useW < 8f) useW = 8f;
                BindFit(st, DailyIntroLine, true);
                _fitScratch.fontSize = fs;
                float th = _fitScratch.CalcHeight(_fitContent, useW);
                w = useW;
                h = th > 1f ? th : 1f;
                break;
            }
            float x = band.center.x - w * 0.5f;
            float y = band.center.y - h * 0.5f;
            if (x < 4f) x = 4f;
            return PlaceCaption(s, w, h, y - 8f * s);
        }

        // Claim sentence, painted after the card so the dim wash does not hide it.
        // Under the Claim face, outside the gold frame, above the LEVEL words.
        // NudgeCaption (holdWant) keeps that seat instead of jumping to the logo.
        void DrawDailyClaimLine(float s)
        {
            if (!_dailyIntroLive || !_dailyOpen || _dailyAskOpen) return;
            DailyLayout(s, out var card, out var flower, out var board, out float band);
            var frame = DailyFrameOuter(card, board, band);
            var face = FlowerDisc(flower, 0f);
            const string line = "Tap to claim!";
            const float plateY = 12f;
            const float plateX = 18f;
            float keep = 12f + 8f * s;
            if (keep < 18f) keep = 18f;
            // Bubble hangs plateY past the text. Stay under the face and off the words.
            float top = face.yMax + plateY + 4f;
            float words = FlowerLevelTop() - plateY - 4f;
            float h = 36f * s;
            float y = top;
            if (y + h > words)
            {
                float room = words - y;
                float minH = 22f * s;
                if (room >= minH) h = room;
                else
                {
                    h = minH;
                    y = words - h;
                    float frameClear = frame.yMax + plateY + 4f;
                    if (y < frameClear) y = frameClear;
                }
            }
            // First seat only. SeatTutorialCaption latches after this, and a later
            // frame must not slide the line along the glove.
            if ((!_tutorSeatOn || _tutorSeatFor != line) && GloveArcRect(s, false, out var sweep))
            {
                float under = sweep.yMax + 4f;
                if (under > y && under + h <= words)
                    y = under;
            }

            AvatarChannel(s, out float chL, out float chR);
            float left = chL + 6f * s + plateX;
            float right = chR - 6f * s - plateX;
            if (right - left < 72f * s)
            {
                left = 8f + plateX;
                right = Screen.width - 8f - plateX;
            }
            EatSide(_avatarDrawR);
            EatSide(_avatarPlateR);
            float span = right - left;
            float w = Mathf.Min(span, Mathf.Min(flower.width * 2.4f, 320f * s));
            if (w < 72f * s) w = Mathf.Max(72f * s, span);
            float x = flower.center.x - w * 0.5f;
            if (x < left) x = left;
            if (x + w > right) x = right - w;
            if (x < 4f) x = 4f;

            float x0 = Mathf.Min(frame.x, face.x);
            float y0 = Mathf.Min(frame.y, face.y);
            float x1 = Mathf.Max(frame.xMax, face.xMax);
            float y1 = Mathf.Max(frame.yMax, face.yMax);
            var dialog = new Rect(x0, y0, x1 - x0, y1 - y0);
            var r = new Rect(x, y, w, h);
            r = NudgeCaption(line, r, s, dialog, true);
            DrawSplashIntroLine(line, r, s);

            void EatSide(Rect block)
            {
                if (block.width < 2f || block.height < 2f) return;
                float yLo = block.y - keep;
                float yHi = block.yMax + keep;
                if (yHi < y || yLo > y + h) return;
                if (block.center.x >= flower.center.x) right = Mathf.Min(right, block.x - keep);
                else left = Mathf.Max(left, block.xMax + keep);
            }
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

        // Leaves pop on after the garden is built. Hold the line until a cover is actually up.
        bool LeafCoverReady(int b)
        {
            var branches = _garden.Branches;
            if (branches == null || (uint)b >= (uint)branches.Length || branches[b] == null || !branches[b].LeavesOn)
            {
                _leafSeen = 0f;
                return false;
            }
            _leafSeen += PlayClock.Delta;
            return _leafSeen >= 0.35f;
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
            _leafSeen = 0f;
            _leafWatch = -1;
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
            _leafSeen = 0f;
            _leafWatch = -1;
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
            if (b != _leafWatch)
            {
                _leafWatch = b;
                _leafSeen = 0f;
            }
            if (!LeafCoverReady(b))
            {
                _cueHand = false;
                _cueLine = null;
                _gloveVis = false;
                _cueBranch = b;
                return;
            }
            _cueHand = true;
            _cueForce = false;
            _cueFreeze = false;
            _cueGift = false;
            _cueBranch = b;
            CueLine(LeafIntroLine);
            _coachFade = Mathf.Min(1f, _coachFade + PlayClock.Delta / 0.35f);
        }

        // Level-1 coach, hive lesson, poker lesson, leaf lesson, gift, and ads keep the glove.
        bool PestStageFree()
        {
            if (_splash || _board == null || _garden.Cam == null) return false;
            if (_restarting || _won || _frozen || _gift != GiftFace.None) return false;
            if (_coach || _leafIntro || _adHand || _levelHive) return false;
            if (_hiveIntro || _hiveIntroLive || _pokerIntro || _pokerIntroLive) return false;
            if (_dailyIntro || _dailyIntroLive) return false;
            if (PlayClock.Now < _coachGiftUntil) return false;
            return true;
        }

        void PestIntroHide()
        {
            ReleaseTutorPause();
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
            if (_pestCue == PestCueSparrow)
            {
                if (!PestCueAlive())
                {
                    PestIntroHide();
                    return false;
                }
                ApplyPestCue();
                return true;
            }
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
            return s != null && s.IsBlocking && s.Settled && !s.InScrap;
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
            return h != null && h.IsBlocking && h.Settled && !h.InScrap;
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
            _pestCueUntil = kind == PestCueSparrow ? float.PositiveInfinity : PlayClock.Now + PestCueSeconds;
            if (kind == PestCueSparrow) HoldTutorPause(true);
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
            _cueHand = true;
            _cueForce = false;
            _cueFreeze = false;
            _cueGift = false;
            _cueBranch = -1;
            _cueHolePx = 0f;
            CueLine(_pestCue == PestCueHawk ? HawkIntroLine : SparrowIntroLine);
            _coachFade = Mathf.Min(1f, _coachFade + PlayClock.Delta / 0.30f);
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

        // True when this tap completed the lesson. The caller must not also
        // hand it to the board.
        bool PestIntroTap(Vector2 screen)
        {
            if (_pestCue == 0) return false;
            var cam = _garden.Cam != null ? _garden.Cam : Camera.main;
            if (cam == null) return false;
            var world = (Vector2)cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0f));
            if (_pestCue == PestCueSparrow && SparrowView.Live != null && PestHit(SparrowView.Live.transform, world))
            {
                DismissPestIntro();
                return true;
            }
            if (_pestCue == PestCueHawk && HawkView.Live != null && PestHit(HawkView.Live.transform, world))
            {
                DismissPestIntro();
                return true;
            }
            return false;
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
            {
                world = body.position;
                var sr = body.GetComponent<SpriteRenderer>();
                if (sr != null && sr.enabled && sr.sprite != null)
                {
                    var b = sr.bounds;
                    // Body center. The hand sits off to the side and points in, so the palm stays off the pest.
                    world = new Vector3(b.center.x, b.center.y, b.center.z);
                }
            }
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
            new CoachCap(68f, 38f, 152f, 38f, 38f, 38f),
            new CoachCap(88f, 66f, 138f, 104f, 42f, 42f),
            new CoachCap(116f, 94f, 116f, 94f, 40f, 40f),
            new CoachCap(78f, 100f, 26f, 158f, 26f, 27f, -7f, -1f),
            new CoachCap(176f, 104f, 204f, 164f, 17f, 14f, -2.4f, 1f),
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
            if (alpha <= 0f)
            {
                float sd = GloveField(x - 6f, y + 8f);
                if (sd >= -0.4f) return default;
                float sa = Mathf.Clamp01((-sd) / 14f);
                sa = sa * sa;
                if (sa < 0.04f) return default;
                return new Color32(48, 32, 22, GloveByte(sa * 78f));
            }
            float ow = Mathf.Lerp(5.6f, 4.0f, Mathf.SmoothStep(30f, 250f, y));
            float fillT = Mathf.Clamp01((-dist - ow) / aa + 0.5f);
            GloveFill(x, y, dist, out float fr, out float fg, out float fb);
            float rib = GloveRib(x, y, dist);
            if (rib > 0f)
            {
                fr = Mathf.Lerp(fr, 132f, rib);
                fg = Mathf.Lerp(fg, 86f, rib);
                fb = Mathf.Lerp(fb, 58f, rib);
            }
            return new Color32(
                GloveByte(Mathf.Lerp(90f, fr, fillT)),
                GloveByte(Mathf.Lerp(68f, fg, fillT)),
                GloveByte(Mathf.Lerp(52f, fb, fillT)),
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
            float soft = 1f - shade * 0.62f;
            float warm = Mathf.SmoothStep(36f, 220f, y);
            r = Mathf.Lerp(168f, 228f, soft);
            g = Mathf.Lerp(118f, 196f, soft);
            b = Mathf.Lerp(86f, 158f, soft);
            r = Mathf.Lerp(r * 0.92f, r, warm);
            g = Mathf.Lerp(g * 0.78f, g, warm);
            b = Mathf.Lerp(b * 0.62f, b, warm);
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
            r = Mathf.Lerp(r, 242f, hi * 0.42f);
            g = Mathf.Lerp(g, 220f, hi * 0.34f);
            b = Mathf.Lerp(b, 186f, hi * 0.22f);
        }

        static float GloveRib(float x, float y, float dist)
        {
            if (y > 68f || dist > -12f) return 0f;
            if (GloveCapDist(x, y, _coachCaps[0]) > -4f) return 0f;
            float best = 0f;
            float arc = (x - 110f) * (x - 110f) * 0.00085f;
            best = GloveBand(y, 30f - arc, 0.78f);
            float upper = GloveBand(y, 46f - arc, 0.62f);
            if (upper > best) best = upper;
            float lip = GloveBand(y, 64f - arc * 0.35f, 0.70f);
            if (lip > best) best = lip;
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
