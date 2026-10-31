using UnityEngine;

namespace FlockFive
{
    // Honey badger contest, Phase 3-5: contest screen (IMGUI). Phase 4 (the opening hive
    // swipe and bee fill, the leap, the sitter) lives in FlockFiveApp.BadgerShow.cs.
    // Phase 5: rewards, first-fight lesson, lose/retry, Enabled on. Reached only through
    // OpenBadgerFight, which obeys the BadgerSchedule.Enabled kill switch. After the opening,
    // a short PostOpen pause (and the one-time lesson on appearance 1), then the badger picks
    // first every turn. No bee-swap screen and no FIGHT gate: auto-fill still uses
    // BadgerLoadout.Preload. Mild and cartoonish: a knock is a honey splat and a claw swipe.
    //
    // Shared pieces: BadgerLoadout (auto-fill), BadgerFight (rules; boss always leads),
    // BadgerSchedule (targets, prices, boss mix, Enabled), BadgerCopy / BadgerPay (lines +
    // settle plan), BadgerDuel (lunge / recoil / leap frames), BadgerFighter + BadgerSwipe
    // (the one actor and the one hive swipe), BadgerArenaRects (five arena boxes),
    // BadgerFighterPlace (bird left, badger right), DrawAvatarBird (the hummingbird,
    // facing the badger), BadgerMeter (honeycomb stacks), BadgerRoundAct (swipe or peck),
    // BadgerOutro (the end beat before the result line), BadgerShakeStart (the flinch),
    // PopupMotion + PushBadgerZoom (the Don't Care camera, around the boss eye),
    // DontCareSlamRects (word band), CoachLineStyle (meter numbers),
    // Purse.ClearRewardFor + Credit, Hive.TakeBossVisitor / TakeVisitor / HoneyOfFinish,
    // CardText.DrawHoneyDigit, AlbumWood/AlbumFace, DrawBadgerTile, BadgerButton,
    // FitCaptionBox + DrawSplashIntroLine (one caption box), CoachGloveAt (first-fight lesson),
    // TutorialHeal, Sfx.CardBump/Deny, SparkleFx, DrawWaxSplat.
    public sealed partial class FlockFiveApp
    {
        enum BadgerLook { Face, Down, Spent }

        const float BadgerBossWait = 0.75f;
        const float BadgerSlide = 0.35f;
        const float BadgerRevealHold = 0.80f;
        const float BadgerVerdictHold = 1.45f;
        const float BadgerDotStep = 0.11f;
        const float BadgerShake = 0.35f;
        const int BadgerPowerKey = 100;
        const int BadgerHiveKey = 80;
        const int BadgerBodyKey = 81;

        BadgerStage _bgStage;
        BadgerLoadout _bgLoadout;
        BadgerFight _bgFight;
        int _bgAppearance;
        int _bgSeed;
        float _bgT;
        float _bgDotT;
        Vector2 _bgFxFoot;
        Vector2 _bgFxBird;
        Vector2 _bgFxHive;
        float _bgFxSpan = 48f;
        float _bgWindAcc;
        float _bgClawAcc;
        float _bgLeapEmitT = -1f;
        float _bgHopPhase = -1f;
        int _bgSwipePhase = -1;
        float _bgBossAt;
        float _bgPlayerAt;
        int _bgBossIx = -1;
        int _bgPlayerIx = -1;
        BadgerPower _bgArmed;
        BadgerRound _bgRound;
        bool _bgResolved;
        int _bgShownPlayer;
        int _bgShownBoss;
        int _bgShakeKey = -1;
        float _bgShakeT;
        int _bgPulseWho;
        float _bgPulseAge;
        int _bgLeftL = -1;
        int _bgLeftR = -1;
        float _bgLeftAtL = -1f;
        float _bgLeftAtR = -1f;
        string _bgLine = "";
        // Final blow. Flash, one beat of silence, then the win fanfare.
        bool _bgBlow;
        float _bgBlowT;
        bool _bgBlowSung;
        readonly string[] _bgPrice = new string[5];
        int _bgFlagDisplay;
        int _bgRoundN;
        bool _bgLessonLive;
        // This contest ran the first-fight lesson: BadgerFight forces the move-3 slam.
        bool _bgCoachFight;
        // Eye of the one duel badger, in layout space before the page zoom.
        // The slam glint maps it through the same pivot and zoom.
        Vector2 _bgDuelEye;
        bool _bgDuelEyeOn;
        // Coach fight, moves 1-3: the guided tile's rect this frame (glove aim + tap rect).
        Rect _bgGuideAim;
        // b123: the guided comb's padded hit rect (glove tap rect + gate target, one rect so the gate never blinds).
        Rect _bgGuideHit;
        // b123: round-4 power step (x2 button, then the scripted comb). On only for the first-use x2 after move 3.
        bool _bgPowerGuideOn;
        // b127: Freeze demo break-free step (CoachStep.BadgerBreakFree). Derived-safe: BadgerBreakFreeLive also needs BossFrozenNow.
        bool _bgBreakFreeOn;
        int _bgBreakTile = -1;
        readonly BadgerFrozenUi _bgFrozenUi = new BadgerFrozenUi();
        readonly BadgerFrozenUi _bgFrozenUiFreeze = new BadgerFrozenUi(BadgerPower.FreezeSpray);
        BadgerPower _bgGuidePower;      // the power the round's forced step is about
        BadgerPower _bgBlockBeat;       // Hot Sauce / Freeze Spray played this round (any fight): drives the fly beat
        BadgerPower _bgGuideRoundPower; // set when that power's tile is played; drives the post-power arrows
        BadgerHex _bgYouHex;            // the page's own comb grid, stored per frame: glove, gate, hit pad and paint share it
        bool _bgYouHexOk;
        Rect _bgYouSlotNow, _bgBossSlotNow; // arena tiles as painted this frame (arrow targets)
        int _bgPowerTile = -1;
        Rect _bgPowerBtn;
        // b123: lesson taps that are eaten without advancing (back, power row, combs) and the step's start time.
        readonly Rect[] _bgLessonIgnore = new Rect[12];
        int _bgLessonIgnoreN;
        float _bgLessonStepAt;
        int _bgLessonStep;
        Rect _bgLessonAim;
        Rect _bgLessonCap;
        static GUIStyle _bgStyle;
#if UNITY_EDITOR
        bool _bgSwitchWas;
        bool _bgSwitchHeld;
#endif

        static readonly Color BadgerWax = new Color(1f, 0.86f, 0.40f, 1f);
        static readonly Color BadgerHide = new Color(0.52f, 0.46f, 0.43f, 1f);
        static readonly Color BadgerCream = new Color(1f, 0.95f, 0.78f, 1f);
        static readonly Color BadgerGold = new Color(0.98f, 0.74f, 0.18f, 1f);

        // ---- entry and exit ----

        // False while the kill switch is off. Phase 4/5 will call this from the flower/flag flow.
        bool OpenBadgerFight(int cleared)
        {
            if (!BadgerSchedule.Enabled) return false;
            // A season scene always finishes before any badger action starts.
            SeasonCrossfade.FinishLive();
            int n = BadgerSchedule.Appearance(cleared);
            if (n < 1) n = 1;
            _bgAppearance = n;
            _bgFlagDisplay = cleared > 0 ? cleared : 0;
            _bgSeed = Random.Range(1, int.MaxValue);
            _bgLoadout = BadgerLoadout.Preload(BadgerSchedule.Tiles);
            _bgFight = null;
            _bgStage = BadgerStage.Opening;
            _bgT = 0f;
            _bgDotT = 0f;
            _bgBossIx = -1;
            _bgPlayerIx = -1;
            _bgArmed = BadgerPower.None;
            _bgResolved = false;
            _bgShownPlayer = 0;
            _bgShownBoss = 0;
            _bgLeftL = -1;
            _bgLeftR = -1;
            _bgLeftAtL = -1f;
            _bgLeftAtR = -1f;
            _bgShakeKey = -1;
            _bgShakeT = 0f;
            _bgPulseWho = 0;
            _bgPulseAge = 0f;
            _bgRoundN = 0;
            _bgLessonLive = false;
            _bgLessonStep = 0;
            _bgLessonAim = default;
            _bgLessonCap = default;
            _bgCoachFight = false;
            _bgYouHexOk = false;
            _bgPowerGuideOn = false;
            _bgBreakFreeOn = false;
            _bgBreakTile = -1;
            _bgFrozenUi.Reset();
            _bgFrozenUiFreeze.Reset();
            _bgGuidePower = BadgerPower.None;
            _bgGuideRoundPower = BadgerPower.None;
            _bgPowerTile = -1;
            _bgGuideHit = default;
            _bgSlam = default;
            _bgSlamT = 0f;
            _bgSlamWord = -1;
            _bgTellT = 0f;
            _bgTellOn = false;
            _bgBlow = false;
            _bgBlowT = 0f;
            _bgBlowSung = false;
            _bgLine = "";
            RefreshBadgerPrices();
            _splash = true;
            _home = HomeFace.Badger;
            // Phase 4: hive swipe + bee fill, PostOpen pause, then the badger leads.
            BeginBadgerOpening();
            // Battle bed: a no-op when the leap's entrance cue already owns the seat.
            // A faster-riff visit starts the loop at the pitch cap. Smug otherwise stays at 1.
            if (MixDesk.Live != null)
            {
                MixDesk.Live.SetBadgerPitch(BadgerPolish.LoopPitch(BadgerMood.Smug, BadgerPolish.TwistOf(_bgAppearance)));
                MixDesk.Live.BadgerBattle();
            }
            return true;
        }

        // Quit mid-fight or Continue after Over. Win pays; lose / quit leaves the flag.
        void LeaveBadger()
        {
            BadgerIceAnim.Reset(); _bgIceWas = false; _bgThawRound = -1;
            _bgBreakFreeOn = false;
            _bgBreakTile = -1;
            _bgFrozenUi.Reset();
            _bgFrozenUiFreeze.Reset();
            EndBadgerLesson();
            // 0.3 s fade, then the splash bed comes back. A result stinger rings out.
            if (MixDesk.Live != null) MixDesk.Live.SetBadger(false);
            bool finished = (_bgStage == BadgerStage.Over || _bgStage == BadgerStage.Outro)
                && _bgFight != null
                && _bgFight.Result != BadgerResult.Playing;
            BadgerResult result = finished ? _bgFight.Result : BadgerResult.BadgerWon;
            int flag = _bgFlagDisplay > 0 ? _bgFlagDisplay : BadgerSave.Pending;
            var plan = BadgerPay.Plan(result, flag);
            BadgerPay.Apply(plan);
            _home = HomeFace.Splash;
            _bgFight = null;
            _bgLoadout = null;
            _bgStage = BadgerStage.Opening;
            _bgOpenT = 0f;
            _bgWashOut = 0f;
            _bgFlagDisplay = 0;
            _bgSlam = default;
            _bgSlamT = 0f;
            _bgSlamWord = -1;
            _bgTellT = 0f;
            _bgTellOn = false;
            _bgBlow = false;
            _bgBlowT = 0f;
            _bgBlowSung = false;
            if (_bgLeap.Live) _bgLeap.Release();
#if UNITY_EDITOR
            if (_bgSwitchHeld)
            {
                BadgerSchedule.Enabled = _bgSwitchWas;
                _bgSwitchHeld = false;
            }
#endif
        }

#if UNITY_EDITOR
        // Drop a file on /tmp/flock-five-badger-fight (optional cleared level inside, default 15).
        // The switch is held on only until the screen is left.
        void EditorOpenBadgerFight()
        {
            const string path = "/tmp/flock-five-badger-fight";
            int level = 15;
            try { int.TryParse(System.IO.File.ReadAllText(path).Trim(), out level); }
            catch { }
            try { System.IO.File.Delete(path); }
            catch { }
            if (level < BadgerSchedule.FirstAfter) level = BadgerSchedule.FirstAfter;
            if (!_bgSwitchHeld)
            {
                _bgSwitchWas = BadgerSchedule.Enabled;
                _bgSwitchHeld = true;
            }
            BadgerSchedule.Enabled = true;
            OpenBadgerFight(level);
        }
#endif

        // ---- round flow ----

        void BeginBadgerFight()
        {
            _bgFight = new BadgerFight(_bgAppearance, _bgSeed, _bgLoadout.Honeys(),
                BadgerPolish.BossTiles(_bgAppearance, _bgSeed), _bgCoachFight);
            SyncBadgerBed();
            _bgShownPlayer = 0;
            _bgShownBoss = 0;
            _bgLeftL = -1;
            _bgLeftR = -1;
            _bgLeftAtL = -1f;
            _bgLeftAtR = -1f;
            NextBadgerRound();
            Sfx.CardBump();
        }

        void NextBadgerRound()
        {
            _bgBossIx = -1;
            _bgPlayerIx = -1;
            _bgArmed = BadgerPower.None;
            _bgResolved = false;
            _bgT = 0f;
            _bgStage = BadgerStage.BossWait;
            bool newTaunt = BadgerPolish.HasTwist(_bgAppearance, BadgerTwist.NewTaunt);
            _bgLine = BadgerCopy.RoundLine(_bgRoundN, BadgerMoodNow(), newTaunt);
            // Coach fight: the round after move 3 announces the unlocked power-ups.
            _bgPowerGuideOn = false;
            _bgBreakFreeOn = false;
            _bgGuideRoundPower = BadgerPower.None;
            _bgBlockBeat = BadgerPower.None;
            if (_bgFight != null && _bgFight.CoachFight
                && _bgFight.RoundsResolved == BadgerTutorialScript.Moves && !_bgFight.PowersLocked)
            {
                _bgLine = BadgerCopy.PowersOpen;
                // b123: move 3 has resolved. Only now is the lesson done; a quit or kill before this restarts it.
                BadgerCopy.MarkCoach();
            }
            _bgRoundN++;
        }

        void TickBadger(float dt)
        {
            if (dt > 0.1f) dt = 0.1f;
            SparkleFx.Tick(dt);
            if (_bgShakeT > 0f) _bgShakeT = Mathf.Max(0f, _bgShakeT - dt);
            if (_bgPulseWho != 0)
            {
                _bgPulseAge += dt;
                if (_bgPulseAge > BadgerRoundAct.PulseSeconds) _bgPulseWho = 0;
            }
            if (_bgStage == BadgerStage.Opening)
            {
                StepBadgerOpening(dt);
                StepBadgerFx(dt);
                return;
            }
            _bgT += dt;
            if (_bgStage == BadgerStage.PostOpen)
            {
                if (_bgT >= BadgerOpening.PostSeconds)
                {
                    if (BadgerCopy.NeedsLesson(_bgAppearance)) BeginBadgerLesson();
                    else BeginBadgerFight();
                }
                return;
            }
            if (_bgStage == BadgerStage.Lesson)
            {
                // Pose + advance run from DrawBadgerPage / CoachPlace. Clock only holds the stage.
                return;
            }
            if (_bgFight == null) return;
            switch (_bgStage)
            {
                case BadgerStage.BossWait:
                    if (_bgT < BadgerBossWait) break;
                    // b123: a forced power round scripts the badger's tile (low for x2/x3, high for the blocks).
                    var stepPower = BadgerPowerGuideWanted() ? NextBadgerGuidePower() : BadgerPower.None;
                    int ix = stepPower != BadgerPower.None ? _bgFight.BossPickFor(stepPower) : _bgFight.BossPick();
                    if (ix < 0)
                    {
                        BeginBadgerOutro();
                        break;
                    }
                    _bgBossIx = ix;
                    _bgBossAt = AdPause.Unscaled;
                    _bgT = 0f;
                    _bgStage = BadgerStage.YourPick;
                    _bgLine = BadgerCopy.YourPick;
                    if (_bgFight.GuidedTile >= 0)
                    {
                        // Coach step: static caption box owns the line; the glove starts fresh
                        // at this step's tile (never slides over from the last one).
                        _bgLine = "";
                        _bgGuideAim = default;
                        ResetBadgerGlove();
                        // CoachPlace runs before OnGUI. Fill the tile now so the first pose
                        // already has this step's rect.
                        RefreshBadgerGuideAim();
                    }
                    else if (_bgFight.CoachFight && _bgFight.BossFrozenNow)
                    {
                        // Freeze demo beat 2: the badger is still iced; he struggles and shatters free when this round resolves.
                        // b127: a real gated step. The glove sits on the first open comb and only that comb takes the tap.
                        _bgBreakTile = FirstOpenPlayerTile();
                        if (_bgBreakTile >= 0)
                        {
                            _bgBreakFreeOn = true;
                            _bgLine = "";
                            _bgGuideAim = default;
                            ResetBadgerGlove();
                            RefreshBadgerGuideAim();
                        }
                        else _bgLine = BadgerCopy.FreezeBreakFreeWait; // no comb to point at: never gate (no softlock)
                    }
                    else if (BadgerPowerGuideWanted())
                    {
                        // b123: the first free use of each power (x2, x3, Hot Sauce, Freeze Spray) is a forced step.
                        // The intro line stays up (it used to be overwritten by "Your pick").
                        _bgGuidePower = NextBadgerGuidePower();
                        _bgPowerGuideOn = true;
                        _bgPowerTile = _bgFight.PowerStepPlayerTile(_bgGuidePower);
                        _bgLine = "";
                        _bgGuideAim = default;
                        ResetBadgerGlove();
                    }
                    Sfx.CardBump();
                    break;
                case BadgerStage.Reveal:
                    if (_bgT < BadgerSlide + BadgerRevealHold + BadgerPowerFly.RevealExtra(_bgBlockBeat)) break;
                    // Both picks are in: roll the Don't Care slam (spec 2b) before the compare.
                    if (_bgFight.TryDontCare(out var slam))
                    {
                        PlaySlamTell(slam);
                        break;
                    }
                    ResolveBadgerRound();
                    break;
                case BadgerStage.SlamTell:
                    StepSlamTell(dt);
                    break;
                case BadgerStage.DontCare:
                    StepDontCareSlam(dt);
                    if (!BadgerSlam.Done(_bgSlamT)) break;
                    FinishDontCareSlam();
                    break;
                case BadgerStage.Verdict:
                    StepBadgerDots(dt);
                    if (_bgResolved
                        && BadgerRoundAct.Of(_bgRound.PlayerGained, _bgRound.BossGained) == BadgerRoundAct.Kind.Peck
                        && BadgerRoundAct.PeckHits(_bgT - dt, _bgT))
                        BadgerShakeStart(BadgerBodyKey);
                    if (_bgResolved
                        && BadgerRoundAct.Of(_bgRound.PlayerGained, _bgRound.BossGained) == BadgerRoundAct.Kind.Swipe
                        && BadgerSwipe.PhaseAt(_bgT - dt) != BadgerSwipe.Phase.Hold
                        && BadgerSwipe.PhaseAt(_bgT) == BadgerSwipe.Phase.Hold)
                    {
                        SparkleFx.HoneySplash(_bgFxBird, 12);
                        SparkleFx.StreakSparks(_bgFxBird, 6);
                    }
                    if (_bgT < BadgerVerdictHold || BadgerDotsBusy()) break;
                    if (_bgFight.Result != BadgerResult.Playing) BeginBadgerOutro();
                    else NextBadgerRound();
                    break;
                case BadgerStage.Outro:
                    if (_bgBlow)
                    {
                        float before = _bgBlowT;
                        _bgBlowT += dt;
                        if (!_bgBlowSung && BadgerPolish.FanfareDue(before, _bgBlowT))
                        {
                            _bgBlowSung = true;
                            var at = _bgFxBird.sqrMagnitude > 4f ? _bgFxBird : _bgFxFoot;
                            SparkleFx.Celebrate(at);
                            bool stung = MixDesk.Live != null && MixDesk.Live.BadgerResult(true);
                            if (!stung) SfxLibrary.Play("fanfare", 0.40f);
                        }
                        if (_bgBlowT < BadgerPolish.BlowDuration) break;
                    }
                    if (_bgT < BadgerOutro.Duration) break;
                    ShowBadgerResult();
                    break;
            }
            StepBadgerFx(dt);
        }

        // Dust, claw sparks, and the special-attack charge. Positions are the last drawn marks.
        void StepBadgerFx(float dt)
        {
            float swipeT = -1f;
            bool swipe = false;
            if (_bgStage == BadgerStage.Opening && _bgWashOut <= 0f)
            {
                swipeT = _bgOpenT;
                swipe = BadgerSwipe.Active(swipeT);
            }
            else if (_bgResolved && _bgStage == BadgerStage.Verdict
                && BadgerRoundAct.Of(_bgRound.PlayerGained, _bgRound.BossGained) == BadgerRoundAct.Kind.Swipe
                && BadgerSwipe.Active(_bgT))
            {
                swipeT = _bgT;
                swipe = true;
            }

            int phase = swipe ? (int)BadgerSwipe.PhaseAt(swipeT) : -1;
            if (phase == (int)BadgerSwipe.Phase.Windup)
            {
                _bgWindAcc += dt;
                float radius = _bgFxSpan * 0.55f;
                while (_bgWindAcc >= 0.05f)
                {
                    _bgWindAcc -= 0.05f;
                    SparkleFx.ChargeSwirl(_bgFxFoot, radius);
                }
            }
            else _bgWindAcc = 0f;

            if (phase == (int)BadgerSwipe.Phase.Strike)
            {
                _bgClawAcc += dt;
                var aim = _bgStage == BadgerStage.Opening ? _bgFxHive : _bgFxBird;
                while (_bgClawAcc >= 0.045f)
                {
                    _bgClawAcc -= 0.045f;
                    var mid = Vector2.Lerp(_bgFxFoot, aim, 0.72f);
                    SparkleFx.StreakSparks(mid, 3);
                }
            }
            else _bgClawAcc = 0f;

            if (_bgSwipePhase == (int)BadgerSwipe.Phase.Strike && phase == (int)BadgerSwipe.Phase.Hold)
                SparkleFx.DustPuff(_bgFxFoot, 8);
            _bgSwipePhase = phase;

            if (_bgStage == BadgerStage.YourPick && _bgBossIx >= 0)
            {
                float age = AdPause.Unscaled - _bgBossAt;
                float phaseHop = Mathf.Repeat(age < 0f ? 0f : age, 0.28f) / 0.28f;
                if (_bgHopPhase >= 0f && _bgHopPhase < 0.55f && phaseHop >= 0.55f && age < BadgerDuel.LungeSeconds)
                    SparkleFx.DustPuff(_bgFxFoot, 7);
                _bgHopPhase = phaseHop;
            }
            else _bgHopPhase = -1f;
        }

        // The compare. A slam hit this round already stripped the power-up or set the halve.
        void ResolveBadgerRound()
        {
            if (!_bgFight.Resolve(out _bgRound))
            {
                NextBadgerRound();
                return;
            }
            _bgResolved = true;
            _bgT = 0f;
            _bgDotT = 0f;
            _bgStage = BadgerStage.Verdict;
            _bgLine = BadgerCopy.Spoken(BadgerVerdict(_bgRound), BadgerMoodNow());
            // b123: the forced block round says the block worked.
            if (_bgGuideRoundPower != BadgerPower.None && BadgerFight.IsBlock(_bgGuideRoundPower) && _bgRound.Power == _bgGuideRoundPower)
                _bgLine = BadgerCopy.BlockWorkedLine(_bgGuideRoundPower);
            if (_bgRound.PlayerGained > 0 || _bgRound.BossGained > 0 || BadgerFight.IsBlock(_bgRound.Power))
                Sfx.CardBump();
            if (_bgFight.Result == BadgerResult.Playing) SyncBadgerBed();
        }

        // Bed tempo follows the arc and this visit's twist. Smug with no faster riff stays at 1.
        void SyncBadgerBed()
        {
            if (MixDesk.Live == null || _bgFight == null) return;
            var mood = BadgerPolish.Mood(_bgFight.PlayerScore, _bgFight.PlayerTarget, false);
            var twist = BadgerPolish.TwistOf(_bgFight.Appearance);
            MixDesk.Live.SetBadgerPitch(BadgerPolish.LoopPitch(mood, twist));
        }

        BadgerMood BadgerMoodNow()
        {
            if (_bgFight == null) return BadgerMood.Smug;
            bool give = _bgBlow || (_bgFight.Result == BadgerResult.PlayerWon
                && (_bgStage == BadgerStage.Outro || _bgStage == BadgerStage.Over));
            return BadgerPolish.Mood(_bgFight.PlayerScore, _bgFight.PlayerTarget, give);
        }

        BadgerWear BadgerWearNow()
        {
            if (_bgFight == null) return BadgerWear.Fresh;
            return BadgerPolish.WearOf(_bgFight.PlayerScore, _bgFight.PlayerTarget);
        }

        bool BadgerDotsBusy()
        {
            return _bgShownPlayer < Mathf.Min(_bgFight.PlayerScore, _bgFight.PlayerTarget)
                || _bgShownBoss < Mathf.Min(_bgFight.BossScore, _bgFight.BadgerTarget);
        }

        void StepBadgerDots(float dt)
        {
            if (!BadgerDotsBusy()) return;
            _bgDotT += dt;
            if (_bgDotT < BadgerDotStep) return;
            _bgDotT = 0f;
            if (_bgShownPlayer < Mathf.Min(_bgFight.PlayerScore, _bgFight.PlayerTarget))
            {
                _bgShownPlayer++;
                _bgPulseWho = 1;
                _bgPulseAge = 0f;
            }
            else
            {
                _bgShownBoss++;
                _bgPulseWho = 2;
                _bgPulseAge = 0f;
            }
            SfxLibrary.Play("tick", 0.30f);
        }

        // End beat first. The result line waits until BadgerOutro.PopUp.
        // Bed fades 0.3 s and the badger-win / badger-lose stinger plays once, with the beat.
        // The old one-shots stay as the fallback when the clips are missing.
        void BeginBadgerOutro()
        {
            bool won = _bgFight != null && _bgFight.Result == BadgerResult.PlayerWon;
            _bgStage = BadgerStage.Outro;
            _bgT = 0f;
            _bgLine = "";
            _bgShownPlayer = _bgFight != null ? Mathf.Min(_bgFight.PlayerScore, _bgFight.PlayerTarget) : 0;
            _bgShownBoss = _bgFight != null ? Mathf.Min(_bgFight.BossScore, _bgFight.BadgerTarget) : 0;
            if (won)
            {
                // Final blow: cut the loop now, one sharp hit, white flash, one beat of
                // silence, then the win fanfare. "I give!" holds the caption through it.
                _bgBlow = true;
                _bgBlowT = 0f;
                _bgBlowSung = false;
                _bgLine = BadgerCopy.GiveUp;
                if (MixDesk.Live != null) MixDesk.Live.BadgerCut();
                SfxLibrary.SharpHit();
                return;
            }
            _bgBlow = false;
            bool stung = MixDesk.Live != null && MixDesk.Live.BadgerResult(false);
            if (!stung) Sfx.Deny();
        }

        void ShowBadgerResult()
        {
            bool won = _bgFight != null && _bgFight.Result == BadgerResult.PlayerWon;
            _bgStage = BadgerStage.Over;
            _bgT = 0f;
            _bgLine = BadgerCopy.EndLine(won ? BadgerResult.PlayerWon : BadgerResult.BadgerWon);
            // b123: say what happened and what it paid; a loss says how to try again.
            int flag = _bgFlagDisplay > 0 ? _bgFlagDisplay : BadgerSave.Pending;
            var plan = BadgerPay.Plan(won ? BadgerResult.PlayerWon : BadgerResult.BadgerWon, flag);
            _bgWinCoins = won ? plan.Credit : 0;
            _bgWinAt = AdPause.Unscaled;
            if (won && plan.Credit > 0) _bgLine += "\n+" + plan.Credit + " coins!";
            else if (!won) _bgLine += "\nTap the Level button to try again.";
        }

        static string BadgerVerdict(BadgerRound r) => BadgerCopy.VerdictLine(r);

        static string BadgerPowerName(BadgerPower p)
        {
            if (p == BadgerPower.X2) return "x2";
            if (p == BadgerPower.X3) return "x3";
            if (p == BadgerPower.HotSauce) return "Hot Sauce";
            if (p == BadgerPower.FreezeSpray) return "Freeze Spray";
            return "";
        }

        // Shared PriceFor / PriceLabel so FREE and the spend path stay in sync.
        void RefreshBadgerPrices()
        {
            int n = _bgAppearance < 1 ? 1 : _bgAppearance;
            for (int p = 1; p < _bgPrice.Length; p++)
            {
                var power = (BadgerPower)p;
                bool free = BadgerSave.IsTutorialFirstUse(power, n);
                _bgPrice[p] = BadgerSchedule.PriceLabel(power, n, free);
            }
        }

        string BadgerPowerPriceSub(BadgerPower p)
        {
            int n = _bgAppearance < 1 ? 1 : _bgAppearance;
            bool free = BadgerSave.IsTutorialFirstUse(p, n);
            return BadgerSchedule.PriceLabel(p, n, free);
        }

        // Shared contest shake. BadgerShakeNow also plays the deny sting; the hive hit
        // uses the start on its own so the swipe keeps Sfx.CardBump.
        void BadgerShakeStart(int key)
        {
            _bgShakeKey = key;
            _bgShakeT = BadgerShake;
        }

        void BadgerShakeNow(int key)
        {
            BadgerShakeStart(key);
            Sfx.Deny();
        }

        float BadgerShakeX(int key, float s)
        {
            if (_bgShakeT <= 0f || _bgShakeKey != key) return 0f;
            return Mathf.Sin(_bgShakeT * 60f) * 8f * s * (_bgShakeT / BadgerShake);
        }

        void ClickBadgerPower(BadgerPower p)
        {
            if (_bgStage != BadgerStage.YourPick || _bgFight == null) return;
            // b123: the round-4 step takes x2 only (arm / disarm). Other powers wait for their own turn.
            if (_bgPowerGuideOn && p != _bgGuidePower) return;
            if (BadgerBreakFreeLive() && p != BadgerPower.HotSauce && p != BadgerPower.FreezeSpray) return;
            if (!BadgerFight.IsBlock(p) && _bgArmed == p)
            {
                _bgArmed = BadgerPower.None;
                if (!_bgPowerGuideOn) _bgLine = BadgerCopy.YourPick;
                return;
            }
            // Hot Sauce and Freeze Spray are disabled while the badger is frozen (button is greyed); a tap does nothing but say why.
            // (Checked before PowerReady: a spent Freeze Spray still answers while he is frozen.)
            if (_bgFight.PowersLocked) return;
            if (BadgerFight.IsBlock(p))
            {
                var ui = p == BadgerPower.FreezeSpray ? _bgFrozenUiFreeze : _bgFrozenUi;
                var fl = ui.Tap(_bgFight, p, AdPause.Unscaled);
                if (fl != null)
                {
                    Sfx.Deny();
                    if (!BadgerBreakFreeLive()) _bgLine = fl;
                    return;
                }
            }
            if (!_bgFight.PowerReady(p)) return;
            // (Hot Sauce / Freeze Spray frozen taps are answered above.)
            // b127: shared deny sound only (no shake, squish or burst). The break-free step paints the line in its own
            // caption box for a moment; every other round puts it on the shared caption.
            if (!_bgFight.CanAfford(p))
            {
                BadgerShakeNow(BadgerPowerKey + (int)p);
                _bgLine = BadgerCopy.CoinsShort;
                return;
            }
            if (BadgerFight.IsBlock(p))
            {
                // b123: Hot Sauce / Freeze Spray assign themselves to the weakest open tile. No tile tap.
                int weakest = _bgFight.PowerStepPlayerTile(p);
                if (weakest < 0) return;
                _bgArmed = p;
                Sfx.CardTap();
                ClickBadgerTile(weakest);
                if (_bgStage != BadgerStage.Reveal) _bgArmed = BadgerPower.None; // the play was refused
                return;
            }
            _bgArmed = p;
            Sfx.CardTap();
            if (_bgPowerGuideOn) return; // the guide caption owns the line
            _bgLine = BadgerFight.IsBlock(p)
                ? BadgerPowerName(p) + " is ready. Now pick a tile."
                : BadgerPowerName(p) + " is ready. Now pick the tile to boost.";
        }

        void ClickBadgerTile(int i)
        {
            if (_bgStage != BadgerStage.YourPick || _bgFight == null) return;
            if (!_bgFight.PlayerOpen(i)) return;
            if (!_bgFight.PlayerPlay(i, _bgArmed))
            {
                BadgerShakeNow(i);
                _bgLine = BadgerCopy.CoinsShort;
                return;
            }
            _bgGuideRoundPower = _bgPowerGuideOn ? _bgGuidePower : BadgerPower.None;
            _bgBlockBeat = BadgerFight.IsBlock(_bgArmed) ? _bgArmed : BadgerPower.None;
            _bgPowerGuideOn = false;
            _bgBreakFreeOn = false;
            _bgPlayerIx = i;
            _bgPlayerAt = AdPause.Unscaled;
            _bgT = 0f;
            _bgStage = BadgerStage.Reveal;
            _bgLine = "";
            if (_bgArmed != BadgerPower.None) Sfx.Clink();
            Sfx.CardBump();
        }

        // ---- first-fight lesson (shared FitCaptionBox + glove) ----

        void BeginBadgerLesson()
        {
            _bgLessonLive = true;
            _bgCoachFight = true;
            _bgLessonStep = 0;
            _bgLessonStepAt = AdPause.Unscaled;
            _bgStage = BadgerStage.Lesson;
            _bgT = 0f;
            _bgLine = "";
            RefreshBadgerLessonAim();
            ResetBadgerGlove();
        }

        // Shared by the lesson steps and the guided picks: the hand starts fresh at the next aim.
        void ResetBadgerGlove()
        {
            _gloveReady = false;
            _gloveVis = false;
            _coachFade = 0f;
            GloveVeilReset();
        }

        // Coach fight, moves 1-3, while the player is to pick. Derived from the fight each
        // frame (no flag of its own), so nothing can leak and leave taps blocked: leaving the
        // page, the pick, or move 3 ends it on the spot.
        bool BadgerGuideLive()
        {
            return _splash && _home == HomeFace.Badger && _bgFight != null
                && _bgStage == BadgerStage.YourPick && (_bgFight.GuidedTile >= 0 || _bgPowerGuideOn || BadgerBreakFreeLive());
        }

        // b127: the Freeze demo's break-free step. Needs the badger still iced, so it can never outlive the freeze.
        bool BadgerBreakFreeLive()
            => _bgBreakFreeOn && _bgBreakTile >= 0 && _bgFight != null && _bgFight.CoachFight
               && _bgFight.BossFrozenNow && _bgFight.Result == BadgerResult.Playing;

        // The buttons that may run their handler during the break-free step: frozen Hot Sauce and frozen Freeze Spray
        // (each says why, never advances). b110 nit: one rect each, never their union, so nothing between or beside
        // them (x2 / x3) can slip through the gate. The gate tests both with BadgerFrozenUi.PressPasses.
        Rect BadgerGateToggleRect(int stage)
        {
            if (BadgerBreakFreeLive()) return BadgerPowerRect(BadgerLayout(), (int)BadgerPower.HotSauce - 1);
            return stage == 2 ? _bgPowerBtn : default;
        }

        Rect BadgerGateFreezeRect()
        {
            if (!BadgerBreakFreeLive()) return default;
            return BadgerPowerRect(BadgerLayout(), (int)BadgerPower.FreezeSpray - 1);
        }

        static readonly BadgerPower[] BadgerGuideOrder = { BadgerPower.X2, BadgerPower.X3, BadgerPower.HotSauce, BadgerPower.FreezeSpray };

        // b123: the next power whose free first use has not been taken (coach fight only), else None.
        BadgerPower NextBadgerGuidePower()
        {
            if (_bgFight == null || !_bgFight.CoachFight || _bgFight.PowersLocked) return BadgerPower.None;
            int n = _bgAppearance < 1 ? 1 : _bgAppearance;
            for (int i = 0; i < BadgerGuideOrder.Length; i++)
            {
                var p = BadgerGuideOrder[i];
                if (BadgerSave.IsTutorialFirstUse(p, n) && _bgFight.PowerReady(p) && !_bgFight.PowerFrozenOut(p) && _bgFight.CanAfford(p)) return p;
            }
            return BadgerPower.None;
        }

        // Round 4 on: a free first use is waiting, there is a comb to play, and the contest is still going.
        bool BadgerPowerGuideWanted()
        {
            if (_bgFight == null || !_bgFight.CoachFight || _bgFight.PowersLocked) return false;
            if (_bgFight.RoundsResolved < BadgerTutorialScript.Moves) return false;
            return NextBadgerGuidePower() != BadgerPower.None && FirstOpenPlayerTile() >= 0;
        }

        int FirstOpenPlayerTile()
        {
            if (_bgFight == null) return -1;
            for (int i = 0; i < BadgerSchedule.Tiles; i++)
                if (_bgFight.PlayerOpen(i)) return i;
            return -1;
        }

        // 1 = tap the x2 button, 2 = x2 is armed, tap the glowing comb.
        int BadgerPowerGuideStage() => !_bgPowerGuideOn ? 0 : (_bgArmed == _bgGuidePower ? 2 : 1);

        // Guided comb from the same hex fit the page paints. CoachPlace runs before
        // OnGUI, so the glove must not wait on the tile loop to store this rect.
        void RefreshBadgerGuideAim()
        {
            if (!BadgerGuideLive()) return;
            var L = BadgerLayout();
            int tile = BadgerBreakFreeLive() ? _bgBreakTile : _bgPowerGuideOn ? _bgPowerTile : _bgFight.GuidedTile;
            // b123: the page's own stored grid (same one HitPad and the paint use); a fresh fit only before the first paint.
            var hex = _bgYouHexOk ? _bgYouHex : BadgerHex.Fit(L.PlayerGrid, 4, 4);
            _bgGuideAim = hex.Draw(tile);
            _bgGuideHit = hex.Hit(tile);
            _bgPowerBtn = BadgerPowerRect(L, (int)_bgGuidePower - 1);
        }

        // Same slot maths as DrawBadgerPowers, so the glove, the gate and the button agree.
        static Rect BadgerPowerRect(BadgerRects L, int k)
        {
            float gap = 8f * L.S;
            float w = (L.Power.width - gap * 3f) / 4f;
            return new Rect(L.Power.x + k * (w + gap), L.Power.y, w, L.Power.height);
        }

        static Rect BadgerContinueRect(BadgerRects L)
        {
            float bw = Mathf.Min(L.Power.width * 0.72f, 420f * L.S);
            return new Rect(L.Power.center.x - bw * 0.5f, L.Power.y, bw, L.Power.height);
        }

        // Result screen on the first (coach) fight: the glove sits on Continue.
        bool BadgerResultLive()
            => _splash && _home == HomeFace.Badger && _bgStage == BadgerStage.Over && _bgFight != null && _bgFight.CoachFight;

        // Shared back-medal hold: the coach fight's locked-power stretch (and the round-4 step) cannot be quit.
        bool BadgerCoachHoldsBack()
            => _bgFight != null && _bgFight.CoachFight
               && (_bgFight.PowersLocked || _bgPowerGuideOn || BadgerGuidePending() || BadgerFreezeDemoHolds() || BadgerBreakFreeLive())
               && _bgStage != BadgerStage.Over && _bgStage != BadgerStage.Outro;

        // Coach fight: the Freeze demo's break-free beat. From the frozen verdict through the break-free round's
        // verdict, back stays held so the demo cannot be quit halfway. Ends on its own: BossFrozenNow clears at the
        // break-free resolve and _bgResolved clears when the next round starts (never a softlock).
        bool BadgerFreezeDemoHolds()
        {
            if (_bgFight == null || !_bgFight.CoachFight || _bgFight.Result != BadgerResult.Playing) return false;
            if (_bgFight.BossFrozenNow) return true;
            return _bgResolved && _bgRound.FreezeBeat == 2 && _bgStage == BadgerStage.Verdict;
        }

        // b123: between move 3 and the forced power step (round 4's BossWait, Reveal, Verdict), and between each
        // power's step, back stays held while a free first use is still waiting.
        bool BadgerGuidePending()
            => _bgFight != null && _bgFight.CoachFight && !_bgFight.PowersLocked
               && _bgFight.RoundsResolved >= BadgerTutorialScript.Moves
               && _bgFight.Result == BadgerResult.Playing
               && NextBadgerGuidePower() != BadgerPower.None && FirstOpenPlayerTile() >= 0;

        void EndBadgerLesson()
        {
            if (!_bgLessonLive && _bgStage != BadgerStage.Lesson) return;
            _bgLessonLive = false;
            _bgLessonStep = 0;
            _bgLessonAim = default;
            _bgLessonCap = default;
            _gloveVis = false;
            _gloveReady = false;
            _coachFade = 0f;
            GloveVeilReset();
        }

        void AdvanceBadgerLesson()
        {
            if (!_bgLessonLive) return;
            var step = CoachTap.BadgerLesson(_bgLessonStep);
            _bgLessonStep++;
            _bgLessonStepAt = AdPause.Unscaled;
            ResetBadgerGlove();
            if (_bgLessonStep < BadgerCopy.LessonCount)
            {
                RefreshBadgerLessonAim();
                NoteLessonDismiss(step);
                return;
            }
            // b123: the coach stamp moved to NextBadgerRound (after move 3 resolves), so quitting mid-tutorial restarts it.
            EndBadgerLesson();
            BeginBadgerFight();
            NoteLessonDismiss(step);
        }

        void RefreshBadgerLessonAim()
        {
            var L = BadgerLayout();
            if (_bgLessonStep <= 0) _bgLessonAim = L.PlayerGrid;
            else if (_bgLessonStep == 1) _bgLessonAim = L.Power;
            else _bgLessonAim = L.ColL;
        }

        // Called from CoachPlace while the lesson is live. Aims the shared glove at the
        // step's control; a valid tap fades the hand and advances the step.
        void PlaceBadgerLessonGlove(float dt, float s)
        {
            if (BadgerGuideLive())
            {
                // Guided pick / round-4 power step: same shared glove, aimed at the scripted control. The control's
                // own hit rect takes the tap; NoteGloveTap fades the hand only on that rect.
                RefreshBadgerGuideAim();
                var tile = BadgerPowerGuideStage() == 1 ? _bgPowerBtn : _bgGuideAim;
                var tap = BadgerPowerGuideStage() == 1 ? _bgPowerBtn : _bgGuideHit;
                if (tile.width < 2f || tile.height < 2f) return;
                _coachFade = Mathf.Min(1f, _coachFade + dt / 0.30f);
                CoachGloveAt(GloveTarget(tile), dt, s, float.NaN, false, float.NaN, tap);
                return;
            }
            if (BadgerResultLive())
            {
                // Result: the glove is on Continue (the only control).
                var cont = BadgerContinueRect(BadgerLayout());
                _coachFade = Mathf.Min(1f, _coachFade + dt / 0.30f);
                CoachGloveAt(GloveTarget(cont), dt, s, float.NaN, false, float.NaN, cont);
                return;
            }
            if (!_bgLessonLive || _bgStage != BadgerStage.Lesson) return;
            var aim = _bgLessonAim;
            if (aim.width < 2f || aim.height < 2f) return;
            // b123: lessons 1-3 are look-at steps. The pointer is an ARROW (CoachArrowAt, painted with the caption),
            // not a glove. A press on blank space or the glowed area continues; back, combs and the power row are eaten.
            var step = CoachTap.BadgerLesson(_bgLessonStep);
            if (CoachTap.AnyTap(step))
            {
                // SwallowOffTargetTap owns the press. This covers a press that arrived
                // only through the Input System, and the ate flag stops a second step.
                if (!CoachAteThisFrame() && Pressed(out var scr) && !BadgerLessonIgnores(new Vector2(scr.x, PhoneArea.H - scr.y)))
                {
                    MarkCoachAte();
                    AdvanceBadgerLesson();
                }
            }
            else if (BadgerLessonTapNow(aim))
                AdvanceBadgerLesson();
        }

        // True when a lesson press lands on something the step ignores (never advances, never toggles).
        bool BadgerLessonIgnores(Vector2 gui)
        {
            for (int i = 0; i < _bgLessonIgnoreN; i++)
                if (_bgLessonIgnore[i].Contains(gui)) return true;
            return false;
        }

        // One rule per lesson step (CoachTap.Rule): which zones are eaten.
        void RefreshBadgerLessonIgnore(BadgerRects L)
        {
            _bgLessonIgnoreN = FillBadgerLessonIgnore(L, _bgLessonStep, _bgLessonIgnore);
        }

        static int FillBadgerLessonIgnore(BadgerRects L, int step, Rect[] into)
        {
            int n = 0;
            into[n++] = L.Back;
            into[n++] = L.Coin;      // the coin cluster opens the shop
            into[n++] = L.BossGrid;  // nothing there is played
            if (step != 1)
                into[n++] = L.Power;          // L1, L3: the whole power row
            else
                for (int k = 0; k < 4; k++) into[n++] = BadgerPowerRect(L, k); // L2: the buttons themselves do nothing
            if (step != 0) into[n++] = L.PlayerGrid; // L2, L3: combs are not played
            return n;
        }

        // Test hook: would a press at p on lesson `step` be eaten (true) or continue the lesson (false)?
        public static bool BadgerLessonPressIgnored(int step, Rect back, Rect coin, Rect bossGrid, Rect power, Rect playerGrid, Vector2 p)
        {
            var L = new BadgerRects { S = 1f, Back = back, Coin = coin, BossGrid = bossGrid, Power = power, PlayerGrid = playerGrid };
            var into = new Rect[12];
            int n = FillBadgerLessonIgnore(L, step, into);
            for (int i = 0; i < n; i++) if (into[i].Contains(p)) return true;
            return false;
        }

        bool BadgerLessonTapNow(Rect aim)
        {
            if (!Pressed(out var screen)) return false;
            var gui = new Vector2(screen.x, PhoneArea.H - screen.y);
            return aim.Contains(gui);
        }

        void DrawBadgerLesson(BadgerRects L)
        {
            if (!_bgLessonLive || _bgStage != BadgerStage.Lesson) return;
            float s = L.S;
            string line = BadgerCopy.LessonAt(_bgLessonStep);
            if (string.IsNullOrEmpty(line)) return;
            // Cache the glove target for CoachPlace this frame.
            if (_bgLessonStep == 0) _bgLessonAim = L.PlayerGrid;
            else if (_bgLessonStep == 1) _bgLessonAim = L.Power;
            else _bgLessonAim = L.ColL;
            _bgLessonCap = DrawBadgerCoachLine(L, line);
            RefreshBadgerLessonIgnore(L);
            // b130 (a/b/g): small down-arrows through the one CoachArrow helper: two above the player grid, one over the
            // whole power row, then one over each score column at the same time (clear of both fighters).
            int nTips = BadgerCoachArrowTips(L, _bgLessonStep, BadgerSchedule.PlayerTarget(_bgAppearance), BadgerSchedule.BadgerTarget(_bgAppearance), _bgArrowTips);
            for (int i = 0; i < nTips; i++) DrawCoachArrow(_bgArrowTips[i], s, BadgerCoachArrowMul(_bgLessonStep));
            DrawTutorOverlay(s);
        }

        // Caption band shared by the badger plate and PreviewBadgerColumn.
        static void BadgerCaptionLimits(float screenW, float screenH, Rect safe, out float maxW, out float roomH, out float scale, out int hi)
        {
            scale = DeviceFit.UiH(screenH);
            float top = TopHudBox(screenH, safe, 0f);
            // b130 (f): the room is the shared caption seat inside the real layout's caption band.
            float sUi = DeviceFit.Ui(screenW, screenH);
            var L = BadgerLayoutFor(screenW, screenH, safe, sUi, top, BackMedalBox(sUi, top, safe));
            var seat = BadgerCaptionSeat(L.Caption, sUi);
            // The painted plate hangs CaptionLayout.BoxPad outside the text box, so the box gets the seat minus that hang.
            var hang = CaptionLayout.BoxPad(screenH > 2f ? screenH : 720f);
            seat = new Rect(seat.x + hang.x, seat.y + hang.y, Mathf.Max(8f, seat.width - hang.x * 2f), Mathf.Max(8f, seat.height - hang.y * 2f));
            maxW = seat.width;
            if (maxW > screenW * 0.86f) maxW = screenW * 0.86f;
            if (maxW < 8f) maxW = 8f;
            roomH = seat.height;
            if (roomH < 8f) roomH = 8f;
            hi = Mathf.Max(18, Mathf.RoundToInt(34f * scale));
        }

        // b123: does this badger caption fit its band (no bottom clip) at this screen, through the shared caption fit?
        public static bool BadgerCaptionFits(string line, float screenW, float screenH, Rect safe, out string detail)
        {
            BadgerCaptionLimits(screenW, screenH, safe, out float maxW, out float roomH, out float scale, out int hi);
            var fit = MeasureCaptionBox(line, maxW, hi, 14);
            CaptionTextRect(new Rect(0f, 0f, Mathf.Min(fit.width, maxW), Mathf.Min(fit.height, roomH)), scale, out var textR);
            int lo = CaptionPx(12);
            string shown = CaptionLayout.LockBreaks(CoachLineStyle(), line, textR.width, textR.height, lo, CaptionPx(hi), out int font);
            bool ok = fit.height <= roomH + 0.5f && fit.width <= maxW + 0.5f && font >= lo;
            detail = screenW + "x" + screenH + " box " + fit.width.ToString("0") + "x" + fit.height.ToString("0") + " room " + maxW.ToString("0") + "x" + roomH.ToString("0") + " font " + font;
            return ok;
        }

        // b127: the PAINTED result. DrawBadgerCoachLine clamps the box to the band and DrawSplashIntroLine refits the font
        // inside it, so a caption paints cleanly when the refit font is >= the floor and the authored rows (never re-split) fit the
        // band height at that font. Row width is not judged here: at the 14 px floor the long two-line lessons are wider than the inset text rect
        // by design of the shared fit, so a width rule would fail every shipped lesson. Test support; unlike BadgerCaptionFits it does not judge the unclamped preference.
        public static bool BadgerCaptionPaints(string line, float screenW, float screenH, Rect safe, out string detail)
        {
            BadgerCaptionLimits(screenW, screenH, safe, out float maxW, out float roomH, out float scale, out int hi);
            hi = BadgerCaptionHi(line, maxW, roomH, scale, hi);
            var fit = MeasureCaptionBox(line, maxW, hi, 14);
            var box = new Rect(0f, 0f, Mathf.Min(fit.width, maxW), Mathf.Min(fit.height, roomH));
            CaptionTextRect(box, scale, out var textR);
            int lo = CaptionPx(12);
            var st = CoachLineStyle();
            string shown = CaptionLayout.LockBreaks(st, line, textR.width, textR.height, lo, CaptionPx(hi), out int font);
            string painted = line.IndexOf('\n') > 0 ? line : BalanceTwoLines(line);
            st.fontSize = font;
            st.wordWrap = false;
            float rowsH = st.CalcSize(new GUIContent(painted)).y;
            st.wordWrap = true;
            bool heightOk = rowsH <= roomH + 0.5f;
            detail = screenW + "x" + screenH + " box " + box.width.ToString("0") + "x" + box.height.ToString("0") + " room " + maxW.ToString("0") + "x" + roomH.ToString("0")
                + " font " + font + " rows " + rowsH.ToString("0") + " [" + painted.Replace("\n", "/") + "]";
            return font >= lo && heightOk;
        }

        // Column lesson, as PaintCoachCaption will draw it at this screen. Shown keeps
        // the authored newline. TextW/TextH are the inner text rect.
        public static CoachCaptionPreview PreviewBadgerColumn(float screenW, float screenH, Rect safe)
        {
            BadgerCaptionLimits(screenW, screenH, safe, out float maxW, out float roomH, out float scale, out int hi);
            string line = BadgerCopy.LessonAt(2);
            var fit = MeasureCaptionBox(line, maxW, hi, 14);
            float seatW = fit.width < maxW ? fit.width : maxW;
            float seatH = fit.height < roomH ? fit.height : roomH;
            CaptionTextRect(new Rect(0f, 0f, seatW, seatH), scale, out var textR);
            int lo = CaptionPx(12);
            int hiScaled = CaptionPx(hi);
            string shown = CaptionLayout.LockBreaks(CoachLineStyle(), line, textR.width, textR.height, lo, hiScaled, out int font);
            return new CoachCaptionPreview
            {
                Shown = shown,
                Font = font,
                TextW = textR.width,
                TextH = textR.height,
            };
        }

        // Shared static caption box (FitCaptionBox + pinned intro line) for the lesson, the
        // guided picks, and the Don't Care line. The painted plate stays inside L.Caption
        // so it cannot sit on the cards, the fighters, or a second caption.
        Rect DrawBadgerCoachLine(BadgerRects L, string line)
        {
            float s = L.S;
            var hang = CaptionLayout.BoxPad(PhoneArea.H > 2f ? PhoneArea.H : 720f);
            float plateX = hang.x;
            float plateY = hang.y;
            BadgerCaptionLimits(PhoneArea.W, PhoneArea.H, PhoneArea.Safe, out float maxW, out float roomH, out float capScale, out int hi);
            hi = BadgerCaptionHi(line, maxW, roomH, capScale, hi); // b130 (f): authored rows fit the seat, never re-wrapped off the bottom
            var fit = FitCaptionBox(line, maxW, hi, 14);
            float boxW = fit.width;
            if (boxW > maxW) boxW = maxW;
            float boxH = fit.height;
            // b130 (f): the plate is as tall as the lesson paint really needs (floor font, wrapped rows), within the room.
            if (BadgerLessonCaptionRoom(line, PhoneArea.W, PhoneArea.H, PhoneArea.Safe, out float need, out _, out _) || need > boxH)
                boxH = Mathf.Max(boxH, need);
            if (boxH > roomH) boxH = roomH;
            var band = BadgerCaptionSeat(L.Caption, s); // b130 (f): the one caption-band helper
            band = new Rect(band.x + plateX, band.y + plateY, Mathf.Max(8f, band.width - plateX * 2f), Mathf.Max(8f, band.height - plateY * 2f));
            float x = band.center.x - boxW * 0.5f;
            float minX = band.x;
            float maxX = band.xMax - boxW;
            if (maxX < minX) maxX = minX;
            if (x < minX) x = minX;
            if (x > maxX) x = maxX;
            float y = band.y + (band.height - boxH) * 0.5f;
            if (y < band.y) y = band.y;
            var seat = new Rect(x, y, boxW, boxH);
            CapSpot("badger.lesson");
            DrawSplashIntroLine(line, seat, s, hi, pin: true);
            return seat;
        }

        // Coach fight, moves 1-3: this step's caption and the shared glove.
        // The comb bloom is painted with the grid (DrawBadgerGuideGlow) so it stays
        // under the arena, the powers, and this caption.
        void DrawBadgerGuide(BadgerRects L)
        {
            if (!BadgerGuideLive()) return;
            RefreshBadgerGuideAim();
            string line = BadgerBreakFreeLive()
                ? (_bgFrozenUi.Toast(AdPause.Unscaled) ?? _bgFrozenUiFreeze.Toast(AdPause.Unscaled) ?? BadgerCopy.FreezeBreakFreeWait)
                : _bgPowerGuideOn
                ? (BadgerPowerGuideStage() == 2 ? BadgerCopy.PowerArmedLine(_bgGuidePower) : BadgerCopy.PowerIntroLine(_bgGuidePower, _bgFight != null && _bgFight.RoundsResolved == BadgerTutorialScript.Moves))
                : BadgerCopy.GuideLine(_bgFight.TutorialMove);
            if (!string.IsNullOrEmpty(line)) DrawBadgerCoachLine(L, line);
            DrawTutorOverlay(L.S);
        }

        // Pulsing gold/cream GlowTex under the guided comb. Wider than DrawBadgerWinGlow
        // and breathed so the caption's "glowing" tile is the tap.
        void DrawBadgerGuideGlow(Rect r)
        {
            if (!GuiPaint() || r.width < 2f || r.height < 2f) return;
            float pulse = 0.5f + 0.5f * Mathf.Sin(AdPause.Unscaled * 6.5f);
            float pad = r.width * Mathf.Lerp(0.36f, 0.62f, pulse);
            var c = Color.Lerp(BadgerGold, BadgerCream, pulse);
            c.a = Mathf.Lerp(0.70f, 1f, pulse);
            GUI.color = c;
            GUI.DrawTexture(new Rect(r.x - pad, r.y - pad, r.width + pad * 2f, r.height + pad * 2f), GlowTex(), ScaleMode.StretchToFill, true);
            GUI.color = Color.white;
        }

        // ---- layout ----


        public struct BadgerRects
        {
            public float S;
            public Rect Back, Title, Need, Coin;
            public Rect BossGrid, Arena, PlayerGrid, Pick, Power, Caption;
            public Rect ColL, ColR;
        }

        // b130 (f): 0.18 -> 0.27 so every lesson (floor font, wrapped rows, paragraph gap) fits its plate; taken from the grids.
        const float BadgerCaptionFrac = 0.27f;
        const float BadgerPowerFrac = 0.11f;
        // b128: the 'Your pick.' band just above the power row (CaptionStyles "badger.pick"). Taken from the two grids
        // (0.045 each); the arena, the power row and the bottom caption band keep their size.
        const float BadgerPickFrac = 0.09f;

        // b110: the power row, then the caption band under it. Pure so a test can check the two never overlap.
        public static void BadgerBottomBands(float yPower, float xL, float xR, float room, float s, out Rect power, out Rect caption)
        {
            float gap = 6f * s;
            float powH = room * BadgerPowerFrac;
            float capH = room * BadgerCaptionFrac;
            power = new Rect(xL, yPower, xR - xL, powH);
            caption = new Rect(xL, yPower + powH + gap, xR - xL, capH);
        }

        BadgerRects BadgerLayout()
        {
            float s = DeviceFit.Ui();
            float top = TopHud();
            return BadgerLayoutFor(PhoneArea.W, PhoneArea.H, PhoneArea.Safe, s, top, BackMedalRect(s, top));
        }

        // #197 name kept as an alias of #198's public pure layout so both callers/tests resolve to one function.
        static BadgerRects BadgerLayoutAt(float screenW, float screenH, Rect safe, float s, float top, Rect back) => BadgerLayoutFor(screenW, screenH, safe, s, top, back);

        // b129 v4: the fight-page layout as a pure function of the screen (BadgerLayout feeds it the live values).
        public static BadgerRects BadgerLayoutFor(float screenW, float screenH, Rect safe, float s, float top, Rect back)
        {
            var L = new BadgerRects();
            L.S = s;
            L.Back = back;
            bool haveSafe = safe.width >= 2f && safe.height >= 2f;
            float xL = Mathf.Max(8f * s, haveSafe ? safe.xMin + 6f : 0f);
            float xR = Mathf.Min(screenW - 8f * s, haveSafe ? safe.xMax - 6f : screenW);
            // b130 (h): double-thick two-lane score columns.
            float colW = Mathf.Clamp(58f * Mathf.Clamp(s, 1f, 1.35f), 64f, 96f) * BadgerColumnWide;
            // b130 (e): the garden HUD coin balance size (CoinBalanceIcon), drawn through DrawCoinBalance.
            float icon = CoinBalanceIcon(screenW, s);
            L.Coin = new Rect(xR - icon, top + 4f * s, icon, icon);
            float tx = L.Back.xMax + 8f * s;
            float tw = Mathf.Max(80f, screenW * 0.62f - tx);
            // b130 (e): the title stops short of the coin pill (icon + amount, the amount up to BadgerCoinPillTextW).
            float pillLeft = L.Coin.x - 8f * s - BadgerCoinPillTextW(icon);
            if (tx + tw > pillLeft - 6f * s) tw = Mathf.Max(60f, pillLeft - 6f * s - tx);
            L.Title = new Rect(tx, top, tw, L.Back.height * 0.52f);
            L.Need = new Rect(tx, L.Title.yMax, tw, L.Back.height * 0.34f);

            float y0 = L.Back.yMax + 4f * s;
            // b130 (f): the caption band keeps a safe-area bottom margin (BadgerCaptionSafeMargin).
            float yEnd = screenH - BadgerCaptionSafeMargin(screenH, safe, s);
            float gap = 6f * s;
            // b130 (c): the shared PowerRowGap between the bottom comb row band and the power row comes out of the room.
            float rowGap = BadgerPowerRowGap(s);
            float room = Mathf.Max(100f, yEnd - y0 - gap * 4f - rowGap);
            float capH = room * BadgerCaptionFrac;
            float powH = room * BadgerPowerFrac;
            float pickH = room * BadgerPickFrac;
            // Middle stage band, between the badger tiles and the player tiles.
            // Height stays put so the opening hive and bee arcs keep their size.
            float arenaH = room * 0.15f;
            float gridH = (room - capH - powH - arenaH - pickH) * 0.5f;
            float gx = xL + colW + 10f * s;
            float gw = Mathf.Max(60f, xR - colW - 10f * s - gx);
            float y = y0;
            L.BossGrid = new Rect(gx, y, gw, gridH);
            y += gridH + gap;
            L.Arena = new Rect(gx, y, gw, arenaH);
            y += arenaH + gap;
            L.PlayerGrid = new Rect(gx, y, gw, gridH);
            y += gridH + gap;
            L.Pick = new Rect(xL, y, xR - xL, pickH);
            y += pickH + rowGap;
            BadgerBottomBands(y, xL, xR, room, s, out L.Power, out L.Caption);
            // Clamp the caption band to the safe bottom (never under the home indicator).
            if (L.Caption.yMax > yEnd) L.Caption.height = Mathf.Max(8f, yEnd - L.Caption.y);
            // b130 (e): the columns start under the coin pill row, so BADGER LEFT never meets the coin counter.
            float colTop = Mathf.Max(L.BossGrid.y, L.Coin.yMax + 4f * s);
            float colH = L.PlayerGrid.yMax - colTop;
            L.ColL = new Rect(xL, colTop, colW, colH);
            L.ColR = new Rect(xR - colW, colTop, colW, colH);
            return L;
        }

        // b128 test hook: the 'Your pick.' plate as DrawBadgerPickCaption paints it, and the rects it must stay clear of,
        // at one screen size. fits: the authored rows (never re-wrapped, at most the table's MaxLines) at >= 11 pt.
        public static Rect BadgerPickPlateAt(float screenW, float screenH, Rect safe, string line, out Rect bossGrid, out Rect arena,
            out Rect playerGrid, out Rect power, out bool fits, out string detail)
        {
            float s = DeviceFit.Ui(screenW, screenH);
            float top = TopHudBox(screenH, safe, 0f);
            var L = BadgerLayoutAt(screenW, screenH, safe, s, top, BackMedalBox(s, top, safe));
            bossGrid = L.BossGrid; arena = L.Arena; playerGrid = L.PlayerGrid; power = L.Power;
            var plate = BadgerPickPlate(L, "badger.pick", line, out _, out int font);
            int rows = string.IsNullOrEmpty(line) ? 0 : line.Split('\n').Length;
            int max = CaptionStyles.MaxLines("badger.pick");
            fits = font >= Mathf.RoundToInt(11f * s) && (max <= 0 || rows <= max);
            detail = screenW + "x" + screenH + " plate " + plate + " band " + L.Pick + " font " + font + " (" + (font / Mathf.Max(0.01f, s)).ToString("0.0") + " pt) rows " + rows;
            return plate;
        }

        // The plate for a CaptionStyles AbovePower spot: the authored rows (no re-wrap) at the table's TextPt, stepped
        // down until rows plus a slim even pad fit inside the pick band, centred in it. So it never touches a grid, a
        // fighter or the power row. text is the ink rect inside the plate.
        static Rect BadgerPickPlate(BadgerRects L, string spot, string line, out Rect text, out int font)
        {
            float s = L.S > 0.01f ? L.S : 1f;
            float inset = 2f * s;
            var band = new Rect(L.Pick.x + inset, L.Pick.y + inset, L.Pick.width - inset * 2f, Mathf.Max(4f, L.Pick.height - inset * 2f));
            var st = CoachLineStyle();
            int keep = st.fontSize; bool wrap = st.wordWrap;
            float pt = CaptionStyles.TextPt(spot) > 0f ? CaptionStyles.TextPt(spot) : 17f;
            font = Mathf.Max(8, Mathf.RoundToInt(pt * s));
            int lo = Mathf.Max(8, Mathf.RoundToInt(10f * s));
            Vector2 size = Vector2.zero; float padX = 0f, padY = 0f;
            var content = new GUIContent(line ?? "");
            try
            {
                st.wordWrap = false;
                for (; ; font--)
                {
                    st.fontSize = font;
                    size = st.CalcSize(content);
                    padY = Mathf.Max(3f * s, font * 0.30f);
                    padX = Mathf.Max(10f * s, font * 0.80f);
                    if ((size.y + padY * 2f <= band.height && size.x + padX * 2f <= band.width) || font <= lo) break;
                }
            }
            finally { st.fontSize = keep; st.wordWrap = wrap; }
            float w = Mathf.Min(band.width, size.x + padX * 2f);
            float h = Mathf.Min(band.height, size.y + padY * 2f);
            var plate = new Rect(band.center.x - w * 0.5f, band.center.y - h * 0.5f, w, h);
            text = new Rect(plate.x + padX, plate.y + padY, Mathf.Max(4f, plate.width - padX * 2f), Mathf.Max(4f, plate.height - padY * 2f));
            return plate;
        }

        // Pointy-top comb, odd rows shifted half a cell. Hit boxes tile with no overlap.
        struct BadgerHex
        {
            public float Cw, Ch, X0, Y0;
            public int Cols;

            public static BadgerHex Fit(Rect area, int cols, int rows)
            {
                float stack = 1f + (rows - 1) * 0.75f;
                float cw = area.width / (cols + 0.5f);
                float ch = cw * 1.1547f;
                if (ch * stack > area.height) ch = area.height / stack;
                cw = Mathf.Min(cw, ch * 0.95f);
                var hex = new BadgerHex { Cw = cw, Ch = ch, Cols = cols };
                hex.X0 = area.x + (area.width - (cols * cw + cw * 0.5f)) * 0.5f;
                hex.Y0 = area.y + (area.height - ch * stack) * 0.5f;
                return hex;
            }

            public Vector2 Center(int i)
            {
                int r = i / Cols;
                int c = i % Cols;
                float off = (r & 1) == 1 ? Cw * 0.5f : 0f;
                return new Vector2(X0 + off + Cw * (c + 0.5f), Y0 + Ch * 0.5f + r * Ch * 0.75f);
            }

            public Rect Draw(int i)
            {
                var c = Center(i);
                float w = Cw * 0.94f;
                float h = Ch * 0.94f;
                return new Rect(c.x - w * 0.5f, c.y - h * 0.5f, w, h);
            }

            public Rect Hit(int i)
            {
                var c = Center(i);
                return new Rect(c.x - Cw * 0.5f, c.y - Ch * 0.375f, Cw, Ch * 0.75f);
            }
        }

        // ---- shared drawing ----

        static GUIStyle BadgerStyle()
        {
            if (_bgStyle != null) return _bgStyle;
            _bgStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                clipping = TextClipping.Clip
            };
            return _bgStyle;
        }

        // The shared hex mask. HoneyArt bakes it once with the juicy fill.
        static Texture2D HoneycombTex() => HoneyArt.Mask();

        // The one honeycomb tile: grids, arena, and picker. Plate colors are the album's.
        // No pizza art, no foil sheen, no swarm. A foil or inverse bee gets a colored rim.
        static void DrawBadgerTile(Rect hex, BadgerLook look, int honey, Color tint, BeeFinish finish, float alpha, string label)
        {
            if (!GuiPaint() || alpha < 0.02f || hex.width < 4f) return;
            if (look == BadgerLook.Spent)
            {
                HoneyArt.DrawJuicyCell(hex, new Color(0.10f, 0.07f, 0.04f, 0.28f * alpha), 0f, 1f, 0, false, false, false);
                return;
            }
            Color rim = HoneyRim.For(finish);
            rim.a = alpha;
            // Tight grid: juicy fill, no hanging drips into the next tile.
            HoneyArt.DrawJuicyCell(hex, rim, 0f, 1f, honey, true, false, false);
            var inner = Inset(hex, 0.07f);
            bool named = !string.IsNullOrEmpty(label);
            var digit = named
                ? new Rect(inner.x, inner.y + inner.height * 0.08f, inner.width, inner.height * 0.56f)
                : new Rect(inner.x + inner.width * 0.10f, inner.y + inner.height * 0.12f, inner.width * 0.80f, inner.height * 0.76f);
            if (look == BadgerLook.Down) CardText.DrawHoneyDigit(digit, "?", alpha * 0.55f);
            else CardText.DrawHoneyDigit(digit, honey, alpha);
            if (!named) return;
            var st = BadgerStyle();
            var lr = new Rect(inner.x + inner.width * 0.12f, inner.y + inner.height * 0.66f, inner.width * 0.76f, inner.height * 0.20f);
            st.fontSize = FitFont(st, label, lr.width, lr.height, 8, 30);
            Paint(st, new Color(0.20f, 0.10f, 0.04f, alpha));
            GUI.Label(lr, label, st);
        }

        // The one button: Continue, power-ups. Hit pad is the whole rect;
        // `live` false skips the hit entirely so a modal in front owns the finger.
        bool BadgerButton(Rect hit, string title, string sub, bool gold, bool dim, bool lit, bool live, Color ink, float shakeX)
        {
            hit.x += shakeX;
            bool held = false;
            bool fire = live && HitPad(hit, out held);
            if (!GuiPaint()) return fire;
            if (lit)
            {
                float pad = hit.height * 0.18f;
                GUI.color = new Color(1f, 0.88f, 0.36f, 0.75f);
                GUI.DrawTexture(new Rect(hit.x - pad, hit.y - pad, hit.width + pad * 2f, hit.height + pad * 2f), GlowTex(), ScaleMode.StretchToFill, true);
                GUI.color = Color.white;
            }
            var lab = DrawPopupButton(hit, held, gold && !dim ? PopupTint.Gold : PopupTint.Wood);
            var st = BadgerStyle();
            float a = dim ? 0.55f : 1f;
            bool two = !string.IsNullOrEmpty(sub);
            var tr = two ? new Rect(lab.x, lab.y, lab.width, lab.height * 0.58f) : lab;
            if (!string.IsNullOrEmpty(title)) // b123: a picture button passes no title; the caller paints the picture
            {
                st.fontSize = FitFont(st, title, tr.width * 0.92f, tr.height * 0.92f, 10, 72);
                StampLight(tr, title, st, new Color(ink.r, ink.g, ink.b, a));
            }
            if (two)
            {
                var sr = new Rect(lab.x, lab.y + lab.height * 0.52f, lab.width, lab.height * 0.46f);
                st.fontSize = FitFont(st, sub, sr.width * 0.92f, sr.height * 0.92f, 9, 56);
                StampLight(sr, sub, st, new Color(BadgerCream.r, BadgerCream.g, BadgerCream.b, a));
            }
            return fire;
        }

        // Honeycomb stack for one side. Same comb height on both, so 10 is shorter than 18.
        // The number on the end of the bar is points LEFT, not points scored.
        // side 0 is YOU, side 1 is BADGER. `pulse` bumps the stack when a point lands.
        void DrawBadgerMeter(Rect col, int target, int filled, float unit, float pulse, int side)
        {
            if (!GuiPaint() || target < 1 || col.width < 4f) return;
            var keep = GUI.matrix;
            try
            {
                if (pulse > 1.001f)
                {
                    float top = col.yMax - BadgerMeter.Rows(target) * unit;
                    var pivot = new Vector2(col.center.x, (top + col.yMax) * 0.5f);
                    GUIUtility.ScaleAroundPivot(new Vector2(pulse, pulse), pivot);
                }
                int n = filled > target ? target : filled;
                if (n < 0) n = 0;
                for (int i = 0; i < target; i++)
                {
                    var seg = BadgerMeter.Comb2(col, i, unit);
                    if (i < n)
                        HoneyArt.DrawJuicyCell(seg, BadgerGold, 0f, 1f, i, true, false, false);
                    else
                        HoneyArt.DrawJuicyCell(seg, new Color(0.16f, 0.11f, 0.05f, 0.72f), 0f, 1f, i, false, false, false);
                }
            }
            finally
            {
                GUI.matrix = keep;
                GUI.color = Color.white;
            }
            int left = BadgerMeter.LeftToGo(target, filled);
            float now = AdPause.Unscaled;
            float tickAt = side == 0 ? _bgLeftAtL : _bgLeftAtR;
            int shown = side == 0 ? _bgLeftL : _bgLeftR;
            if (shown >= 0 && left < shown) tickAt = now;
            if (side == 0) { _bgLeftL = left; _bgLeftAtL = tickAt; }
            else { _bgLeftR = left; _bgLeftAtR = tickAt; }
            float tick = BadgerMeter.Tick(tickAt < 0f ? -1f : now - tickAt);
            string who = side == 0 ? "YOU" : "BADGER";
            var plate = BadgerMeter.EndPlate2(col, target, unit);
            float pop = 1f + 0.16f * tick;
            if (pop > 1.001f)
            {
                var c = plate.center;
                plate.width *= pop;
                plate.height *= pop;
                plate.x = c.x - plate.width * 0.5f;
                plate.y = c.y - plate.height * 0.5f;
            }
            DrawSolidRound(plate, Mathf.Min(16f, plate.height * 0.28f), 0, 0.97f);
            var st = CoachLineStyle();
            bool wrap = st.wordWrap;
            st.wordWrap = false;
            var whoR = new Rect(plate.x, plate.y + plate.height * 0.04f, plate.width, plate.height * 0.22f);
            st.fontSize = FitFont(st, who, whoR.width * 0.92f, whoR.height * 0.92f, CaptionPx(9), CaptionPx(22));
            StampOutlined(whoR, who, st, BadgerCream, 0, Mathf.Max(1, st.fontSize / 10));
            string num = left.ToString();
            var numR = new Rect(plate.x, plate.y + plate.height * 0.24f, plate.width, plate.height * 0.48f);
            st.fontSize = FitFont(st, num, numR.width * 0.86f, numR.height * 0.92f, CaptionPx(16), CaptionPx(48));
            StampOutlined(numR, num, st, BadgerGold, 1, Mathf.Max(2, st.fontSize / 12));
            var leftR = new Rect(plate.x, plate.y + plate.height * 0.72f, plate.width, plate.height * 0.24f);
            st.fontSize = FitFont(st, "LEFT", leftR.width * 0.90f, leftR.height * 0.90f, CaptionPx(9), CaptionPx(22));
            StampOutlined(leftR, "LEFT", st, BadgerCream, 0, Mathf.Max(1, st.fontSize / 10));
            st.wordWrap = wrap;
        }

        // x2 / x3 sit on the comb's top-right as the shared streak badge.
        // Other powers keep a short name; they are not multipliers.
        void DrawBadgerPowerBadge(Rect comb, BadgerPower power)
        {
            if (power == BadgerPower.X2 || power == BadgerPower.X3)
            {
                int mul = power == BadgerPower.X3 ? 3 : 2;
                float d = Mathf.Min(comb.width, comb.height) * 0.42f * DeviceFit.ControlK();
                var seat = StreakBadge.TopRight(comb, d);
                var st = BadgerStyle();
                string text = mul >= 3 ? "x3" : "x2";
                StreakBadge.NumeralFit(seat, out float fitW, out float fitH);
                st.fontSize = FitFont(st, text, fitW, fitH, 10, Mathf.Max(16, Mathf.RoundToInt(fitH)));
                StreakBadge.Draw(seat, text, st, StreakBadge.Colors(mul), 1f, 0.35f);
                return;
            }
            var badge = new Rect(comb.center.x - comb.width * 0.45f, comb.y - comb.height * 0.10f, comb.width * 0.9f, comb.height * 0.26f);
            DrawSolidRound(badge, badge.height * 0.5f, 0, 0.95f);
            var nameSt = BadgerStyle();
            string name = BadgerPowerName(power);
            nameSt.fontSize = FitFont(nameSt, name, badge.width * 0.86f, badge.height * 0.80f, 8, 40);
            StampLight(badge, name, nameSt, BadgerGold);
        }

        static Color BadgerTint(BadgerTile t)
        {
            if (t.Yard || (uint)t.Kind >= (uint)Hive.Kinds) return BadgerWax;
            return Hive.Roster[t.Kind].Tint;
        }

        // ---- the page ----

        // Don't Care is a camera move on the real page, pivoted on the boss fighter's eye.
        // The overlay, the speed lines, the glint, and the tilted words draw after the
        // zoom is restored, words last, so nothing paints over them.
        void DrawBadgerPage()
        {
            bool slam = DontCareSlamLive && _home == HomeFace.Badger;
            bool paint = GuiPaint();
            BadgerSlam.Shake(slam ? _bgSlamT : -1f, out float sx, out float sy);
            bool doShake = slam && paint && (Mathf.Abs(sx) > 0.2f || Mathf.Abs(sy) > 0.2f);
            var root = GUI.matrix;
            if (doShake)
                GUI.matrix = Matrix4x4.Translate(new Vector3(sx, sy, 0f)) * root;
            try
            {
                bool zoomDraw = slam && paint;
                var keep = GUI.matrix;
                Vector2 pivot = default;
                float zoom = 1f;
                if (zoomDraw)
                {
                    var L = BadgerLayout();
                    var spot = BadgerActorNow(L);
                    zoom = BadgerSlam.Zoom(_bgSlamT);
                    // Stay on the leap pupil so the shrug does not yank the camera.
                    pivot = BadgerSlamPivot(spot, zoom);
                    keep = PushBadgerZoom(zoom, pivot);
                }
                try
                {
                    DrawBadgerPageBody();
                }
                finally
                {
                    if (zoomDraw) GUI.matrix = keep;
                }
                if (slam && paint) DrawDontCareSlam(pivot, zoom);
                if (paint) DrawBadgerBlowFlash();
                if (paint && !_bgLeap.Live) SparkleFx.DrawBits();
            }
            finally
            {
                GUI.matrix = root;
            }
        }

        void DrawBadgerPageBody()
        {
            if (_bgLoadout == null)
            {
                _home = HomeFace.Splash;
                return;
            }
            if (Event.current.type == EventType.Layout) TickBadger(AdPause.UnscaledDelta);
            var L = BadgerLayout();
            float s = L.S;
            DrawHomeWash(0.58f);

            // Opening, PostOpen, and the lesson are not skippable via the medal.
            // b123: and the whole locked-power stretch of the coach fight (Reveal / Verdict / BossWait between moves).
            if (_bgStage == BadgerStage.Opening || _bgStage == BadgerStage.PostOpen || _bgStage == BadgerStage.Lesson
                || _bgStage == BadgerStage.DontCare || _bgStage == BadgerStage.SlamTell
                || BadgerGuideLive() || BadgerCoachHoldsBack() || BadgerResultLive())
                PaintBackMedal(L.Back, false);
            else if (DrawBackMedal(L.Back))
            {
                LeaveBadger();
                return;
            }

            if (GuiPaint())
            {
                var st = BadgerStyle();
                st.fontSize = FitFont(st, "HONEY BADGER", L.Title.width, L.Title.height, 12, 80);
                StampOutlined(L.Title, "HONEY BADGER", st, BadgerGold, 0, 2);
            }
            DrawCoinBalance(s, L.Coin.xMax, L.Coin.center.y); // b130 (e): same helper and size as the garden HUD

            int meterMax = BadgerSchedule.BadgerTargetFixed;
            float meterUnit = BadgerMeter.Unit(L.ColL, meterMax);
            float pulseYou = _bgPulseWho == 1 ? BadgerRoundAct.Pulse(_bgPulseAge) : 1f;
            float pulseBoss = _bgPulseWho == 2 ? BadgerRoundAct.Pulse(_bgPulseAge) : 1f;
            DrawBadgerMeter(L.ColL, BadgerSchedule.PlayerTarget(_bgAppearance), _bgShownPlayer, meterUnit, pulseYou, 0);
            DrawBadgerMeter(L.ColR, BadgerSchedule.BadgerTarget(_bgAppearance), _bgShownBoss, meterUnit, pulseBoss, 1);

            var bossHex = BadgerHex.Fit(L.BossGrid, 4, 4);
            var youHex = BadgerHex.Fit(L.PlayerGrid, 4, 4);
            _bgYouHex = youHex;
            _bgYouHexOk = true;
            bool tilesLive = _bgStage == BadgerStage.YourPick;

            for (int i = 0; i < BadgerSchedule.Tiles; i++)
            {
                bool open = _bgFight == null || _bgFight.BossOpen(i);
                DrawBadgerTile(bossHex.Draw(i), open ? BadgerLook.Down : BadgerLook.Spent, 0, BadgerHide, BeeFinish.Normal, 1f, null);
            }

            int tapped = -1;
            // Coach moves 1-3: only the glove's tile takes a tap; every other tile ignores it.
            int guide = _bgFight != null && tilesLive ? _bgFight.GuidedTile : -1;
            // b123: round-4 step: no comb takes a tap until x2 is armed, then only the scripted comb.
            if (guide < 0 && tilesLive && _bgPowerGuideOn) guide = _bgArmed == _bgGuidePower ? _bgPowerTile : -2;
            if (guide < 0 && tilesLive && BadgerBreakFreeLive()) guide = _bgBreakTile;
            Rect guidePaint = default;
            bool guideBloom = false;
            for (int i = 0; i < BadgerSchedule.Tiles; i++)
            {
                var t = _bgLoadout[i];
                bool open = _bgFight == null || _bgFight.PlayerOpen(i);
                var draw = youHex.Draw(i);
                if (i == guide) { _bgGuideAim = draw; _bgGuideHit = youHex.Hit(i); }
                bool held = false;
                bool live = tilesLive && open && (guide == -1 || i == guide);
                if (live && HitPad(youHex.Hit(i), out held)) tapped = i;
                if (held) draw = Inset(draw, 0.03f);
                draw.x += BadgerShakeX(i, s);
                // Defer the guided comb so its bloom sits above the other tiles and under this plate.
                if (i == guide && BadgerGuideLive())
                {
                    guidePaint = draw;
                    guideBloom = true;
                    continue;
                }
                BadgerTilePlate(draw, t, open ? BadgerLook.Face : BadgerLook.Spent, BadgerTileLand(i));
            }
            if (guideBloom)
            {
                DrawBadgerGuideGlow(guidePaint);
                var gt = _bgLoadout[guide];
                bool gOpen = _bgFight != null && _bgFight.PlayerOpen(guide);
                BadgerTilePlate(guidePaint, gt, gOpen ? BadgerLook.Face : BadgerLook.Spent, BadgerTileLand(guide));
            }

            DrawBadgerArena(L, bossHex, youHex);
            // Hive under the fighters, claws and bees in front. Both fighters draw on every
            // stage, including the slam, at full alpha. The hive cast fade does not apply.
            var hive = DrawBadgerOpeningHive(L);
            if (_bgWashOut > 0f) DrawBadgerWash(_bgWashOut / BadgerLeap.WashOutSeconds);
            DrawBadgerFighters(L);
            if (_bgStage == BadgerStage.Opening)
                DrawBadgerClaws(hive, _bgWashOut > 0f ? 0f : _bgOpenT);
            if (_bgStage == BadgerStage.DontCare && BadgerSlam.ShowNasty(_bgSlamT))
            {
                float a = 1f - Mathf.Clamp01((_bgSlamT - BadgerSlam.NastyEnd) / BadgerSlam.ZoomOutSeconds) * 0.4f;
                DrawBadgerColumnSplat(L, 0.92f * a);
            }
            DrawBadgerOpeningBees(L, youHex, hive.center);
            DrawBadgerPowers(L, _bgStage != BadgerStage.Lesson);
            if (_bgStage != BadgerStage.DontCare) DrawBadgerCaption(L);
            DrawBadgerLesson(L);
            DrawBadgerGuide(L);
            if (BadgerResultLive()) DrawTutorOverlay(s);
            if (tapped >= 0 && _bgStage != BadgerStage.Lesson) ClickBadgerTile(tapped);
        }

        // "+N" over the winner. Player N is the margin. Boss N is the full loss honey.
        void DrawBadgerGain(Rect tile, int gain, float age)
        {
            if (!GuiPaint()) return;
            string text = BadgerGainFloat.Text(gain);
            if (text.Length == 0) return;
            float a = BadgerGainFloat.Alpha(age);
            if (a < 0.04f) return;
            float rise = BadgerGainFloat.Rise(age) * tile.height * 0.55f;
            var r = new Rect(tile.x, tile.y - tile.height * 0.38f - rise, tile.width, tile.height * 0.34f);
            var st = BadgerStyle();
            st.fontSize = FitFont(st, text, r.width * 0.92f, r.height * 0.88f, 10, 64);
            var fill = BadgerGold;
            fill.a = a;
            StampOutlined(r, text, st, fill, 1, Mathf.Max(2, st.fontSize / 10));
        }

        // b129 v4 (Brandon): one shared gap between the badger and the honeycomb he picked. Measured from the
        // fighter rect (the ice block rect when frozen) to the pick-comb hex. Both rest rects are taken here the
        // way the page draws them (same unit, same right-HUD clamp); the comb steps left until the WIDER of the
        // two clears it by the gap, so it does not jump when he freezes. It never steps past the VS box's left edge.
        public struct BadgerGapRects
        {
            public Rect Comb, Fighter, Ice, ColR, You;
            public float Gap;
        }

        public static BadgerGapRects BadgerPickComb(Rect arena, Rect colR, float s)
        {
            var idle = SpriteCatalog.BadgerArt("badger_idle_b");
            var ice = SpriteCatalog.BadgerArt(Badger129.FrozenBlockArt);
            return BadgerPickComb(arena, colR, s,
                idle != null ? idle.rect.width : 480f, idle != null ? idle.rect.height : 640f,
                ice != null ? ice.rect.width : 471f, ice != null ? ice.rect.height : 688f);
        }

        public static BadgerGapRects BadgerPickComb(Rect arena, Rect colR, float s, float idleW, float idleH, float iceW, float iceH)
        {
            var g = new BadgerGapRects { ColR = colR, Gap = BadgerFighterPlace.CombGap(s) };
            var box = BadgerArenaRects.Split(arena);
            float right = colR.x - 2f;
            var mark = BadgerFighterPlace.Rest(box, idleW, idleH, right);
            float unit = mark.Unit;
            var foot = mark.Foot;
            foot.x = BadgerFighterPlace.ClampFootX(foot.x, idleW * unit, right);
            g.Fighter = SpriteAnim.Dest("badger_idle_b", idleW, idleH, foot, unit, 1f, 1f);
            float iceUnit = idleH * unit * Badger129.FrozenBlockHeight / Mathf.Max(1f, iceH);
            var iceFoot = mark.Foot;
            iceFoot.x = BadgerFighterPlace.ClampFootX(iceFoot.x, iceW * iceUnit, right);
            g.Ice = SpriteAnim.Dest(Badger129.FrozenBlockArt, iceW, iceH, iceFoot, iceUnit, 1f, 1f);
            g.Comb = BadgerFighterPlace.ClearComb(BadgerSlotTile(box.BossSlot), Mathf.Min(g.Fighter.xMin, g.Ice.xMin), g.Gap, box.Middle.x);
            // b130: the player's comb mirrors it, clear of the (bigger, inset) bird by the same gap. If the two combs would
            // meet in the middle, both shrink evenly (badger comb keeps its right edge, so its fighter gap holds).
            var you = BadgerSlotTile(box.YouSlot);
            float birdR = mark.BirdBox.xMax + g.Gap;
            if (you.x < birdR) you.x = birdR;
            if (you.xMax + g.Gap > g.Comb.x)
            {
                float avail = g.Comb.xMax - birdR;
                float w = Mathf.Max(8f, (avail - g.Gap) * 0.5f);
                float k = w / Mathf.Max(1f, g.Comb.width);
                float h = g.Comb.height * k;
                float cy = g.Comb.center.y;
                g.Comb = new Rect(g.Comb.xMax - w, cy - h * 0.5f, w, h);
                you = new Rect(birdR, cy - h * 0.5f, w, h);
            }
            g.You = you;
            return g;
        }

        static Rect BadgerSlotTile(Rect slot)
        {
            float h = slot.height * 0.90f;
            float w = h * 0.86f;
            if (w > slot.width * 0.94f)
            {
                w = slot.width * 0.94f;
                h = w / 0.86f;
            }
            if (h > slot.height) h = slot.height;
            return new Rect(slot.center.x - w * 0.5f, slot.center.y - h * 0.5f, w, h);
        }

        void DrawBadgerArena(BadgerRects L, BadgerHex bossHex, BadgerHex youHex)
        {
            float s = L.S;
            var boxes = BadgerArenaRects.Split(L.Arena);
            var pair = BadgerPickComb(L.Arena, L.ColR, s);
            var youSlot = pair.You;  // b130: mirrors the badger side, clear of the bigger bird
            var bossSlot = pair.Comb; // b129 v4: clear of the fighter / ice block by the shared gap
            bool tilesIn = _bgFight != null && (_bgBossIx >= 0 || _bgPlayerIx >= 0);
            bool tie = _bgResolved && _bgRound.PlayerGained <= 0 && _bgRound.BossGained <= 0 && !_bgRound.BossHeld
                && (_bgStage == BadgerStage.Verdict || _bgStage == BadgerStage.Over);

            if (GuiPaint() && (tie || (_bgStage != BadgerStage.Opening && !tilesIn && _bgStage != BadgerStage.Over)))
            {
                var st = BadgerStyle();
                string label = tie ? "TIE" : "VS";
                var mr = boxes.Middle;
                st.fontSize = FitFont(st, label, mr.width, mr.height, 8, 64);
                StampLight(mr, label, st, BadgerCream);
            }

            if (_bgFight != null && _bgBossIx >= 0)
            {
                float u = Mathf.SmoothStep(0f, 1f, (AdPause.Unscaled - _bgBossAt) / BadgerSlide);
                bool block = _bgResolved && BadgerFight.IsBlock(_bgRound.Power);
                int honey = _bgResolved ? _bgRound.BossFinal : _bgFight.BossHoney(_bgBossIx);
                var r = LerpRect(bossHex.Draw(_bgBossIx), bossSlot, u);
                _bgBossSlotNow = r;
                BadgerFreezeLook(ref r, s, out float ice, out float shatterT);
                if (block && _bgRound.Power != BadgerPower.FreezeSpray) r.x += Mathf.Sin(AdPause.Unscaled * 38f) * 5f * s * Mathf.Clamp01(1.2f - _bgT);
                bool win = _bgResolved && _bgRound.BossGained > 0;
                if (win) DrawBadgerWinGlow(r);
                DrawBadgerTile(r, BadgerLook.Face, honey, BadgerHide, BeeFinish.Normal, block ? 0.55f : 1f, null);
                if (win && _bgStage == BadgerStage.Verdict)
                    DrawBadgerGain(r, _bgRound.BossGained, _bgT);
                if (block) DrawBadgerSplat(r);
                if (GuiPaint())
                {
                    SparkleFx.DrawFrozenHex(r, ice, AdPause.Unscaled);
                    SparkleFx.DrawShatter(r, shatterT);
                }
            }
            if (_bgFight != null && _bgPlayerIx >= 0)
            {
                float u = Mathf.SmoothStep(0f, 1f, (AdPause.Unscaled - _bgPlayerAt) / BadgerSlide);
                var t = _bgLoadout[_bgPlayerIx];
                int honey = _bgResolved ? _bgRound.PlayerFinal : _bgFight.PlayerHoney(_bgPlayerIx);
                var r = LerpRect(youHex.Draw(_bgPlayerIx), youSlot, u);
                _bgYouSlotNow = r;
                if (_bgResolved && _bgRound.PlayerGained > 0) DrawBadgerWinGlow(r);
                DrawBadgerTile(r, BadgerLook.Face, honey, BadgerTint(t), t.Finish, 1f, null);
                if (_bgResolved && _bgStage == BadgerStage.Verdict && _bgRound.PlayerGained > 0)
                    DrawBadgerGain(r, _bgRound.PlayerGained, _bgT);
                if (_bgArmed != BadgerPower.None && GuiPaint())
                    DrawBadgerPowerBadge(r, _bgArmed);
                DrawBadgerBlockBeat(r, s);
                DrawBadgerLookArrows(r, s);
            }

        }

        // Freeze look for the badger's tile, shared with the tutorial Freeze demo (same SparkleFx helpers).
        // Turn 1 (and the wait before his break-free turn): iced, small shiver held. Turn 2: struggle, then shatter.
        void BadgerFreezeLook(ref Rect r, float s, out float ice, out float shatterT)
        {
            ice = 0f; shatterT = -1f;
            if (_bgFight == null) return;
            float now = AdPause.Unscaled;
            if (_bgResolved && _bgRound.FreezeBeat == 2)
            {
                const float Struggle = 0.8f, Break = 0.55f;
                if (_bgT < Struggle)
                {
                    var o = SparkleFx.Struggle(now, _bgT, 5f * s);
                    r.x += o.x; r.y += o.y;
                    ice = 1f;
                }
                else shatterT = (_bgT - Struggle) / Break;
            }
            else if (_bgResolved && _bgRound.FreezeBeat == 1) { ice = 1f; r.x += SparkleFx.Shiver(now, 2.2f * s); }
            else if (!_bgResolved && _bgFight.BossFrozenNow) { ice = 1f; r.x += SparkleFx.Shiver(now, 2.2f * s); }
        }

        // b123: Hot Sauce / Freeze demo on the forced round. The icon flies from its button to the weakest tile and
        // replaces the number; it stays through the verdict (the boss tile already dims and splats).
        void DrawBadgerBlockBeat(Rect tile, float s)
        {
            var p = _bgBlockBeat;
            if (_bgFight == null || p == BadgerPower.None || !BadgerFight.IsBlock(p) || !GuiPaint()) return;
            if (_bgStage != BadgerStage.Reveal && _bgStage != BadgerStage.Verdict && _bgStage != BadgerStage.DontCare) return;
            float t = _bgStage == BadgerStage.Reveal ? _bgT : 99f;
            float f = BadgerPowerFly.Progress(t);
            float side = Mathf.Min(tile.width, tile.height) * 0.62f;
            if (f >= BadgerPowerFly.SwapFrom)
            {
                // the number is gone: a disc of the tile's honey colour, then the icon
                float a = Mathf.Clamp01((f - BadgerPowerFly.SwapFrom) / (1f - BadgerPowerFly.SwapFrom));
                var disc = new Rect(tile.center.x - side * 0.55f, tile.center.y - side * 0.55f, side * 1.1f, side * 1.1f);
                GUI.color = new Color(0.98f, 0.78f, 0.28f, a);
                DrawSolidRound(disc, disc.height * 0.5f, 0, a);
                GUI.color = Color.white;
            }
            var L = BadgerLayout();
            var from = BadgerPowerRect(L, (int)p - 1);
            var pos = Vector2.Lerp(from.center, tile.center, f);
            pos.y -= Mathf.Sin(f * Mathf.PI) * tile.height * 0.8f; // a short arc
            float sz = Mathf.Lerp(from.height * 0.7f, side, f);
            DrawBadgerPowerIcon(new Rect(pos.x - sz * 0.5f, pos.y - sz * 0.5f, sz, sz), p, 1f);
        }

        // b128: the painted power icons (Resources/Sprites/fx_power_*), aspect-fit in r. frozen paints them greyed.
        static void DrawBadgerPowerIcon(Rect r, BadgerPower p, float a, bool frozen = false)
        {
            if (!GuiPaint() || r.width < 2f) return;
            var spr = SpriteCatalog.PowerIcon(p);
            if (spr == null || spr.texture == null) return;
            var prevC = GUI.color;
            GUI.color = frozen ? new Color(0.50f, 0.50f, 0.53f, 0.62f * a) : new Color(1f, 1f, 1f, a);
            DrawSprite(r, spr, true);
            GUI.color = prevC;
        }

        // b123 look-at arrows (the glove stays for taps): the power badge on the tile, the power's visible effect, and the
        // halved tile on move 3's verdict. Coach fight only; one arrow at a time.
        void DrawBadgerLookArrows(Rect playerTile, float s)
        {
            if (_bgFight == null || !_bgFight.CoachFight || !GuiPaint()) return;
            if (_bgStage == BadgerStage.Reveal && _bgGuideRoundPower != BadgerPower.None && _bgArmed != BadgerPower.None)
            {
                CoachArrowAt(playerTile, s); // look at the badge on the tile
                return;
            }
            if (_bgStage != BadgerStage.Verdict || !_bgResolved) return;
            if (_bgGuideRoundPower != BadgerPower.None)
            {
                // the effect: a boosted tile, or the badger's blocked tile
                CoachArrowAt(BadgerFight.IsBlock(_bgGuideRoundPower) && _bgBossSlotNow.width > 2f ? _bgBossSlotNow : playerTile, s);
                return;
            }
            if (_bgRound.Halved) CoachArrowAt(playerTile, s); // the halved tile
        }

        struct BadgerActorSpot
        {
            public string Frame;
            public string Under;
            public float FrameAlpha;
            public float UnderAlpha;
            public float Sx;
            public float Sy;
            public Vector2 Foot;
            public float Unit;
            public float Degrees;
            public Vector2 Eye;
            public Vector2 Bird;
            public float Icon;
            public bool FaceLeft;
            public float BirdDegrees;
            public bool BirdFolded;
            public bool BirdInFront;
            public Rect BirdBox;
            public float Alpha;
        }

        // Where the one badger and the hummingbird stand this frame. Rest marks come
        // from BadgerFighterPlace. The slam camera pivots on Eye.
        BadgerActorSpot BadgerActorNow(BadgerRects L)
        {
            var spot = new BadgerActorSpot();
            var box = BadgerArenaRects.Split(L.Arena);
            BadgerDuelPose(out var beat, out float age);
            float now = PlayClock.Now;
            var pose = BadgerAnim.Duel(beat, age, now);
            var wearNow = BadgerWearNow();
            // Wrecked idle drops into the crouch so the later hits read lower. The tell
            // stays on the idle face so the glint hits the same pupil every time.
            if (beat == BadgerDuelBeat.Idle && wearNow == BadgerWear.Wrecked
                && _bgStage != BadgerStage.Opening && _bgStage != BadgerStage.DontCare
                && _bgStage != BadgerStage.SlamTell)
            {
                pose.Frame = "badger_crouch";
                pose.Under = null;
                pose.UnderAlpha = 0f;
                pose.FrameAlpha = 1f;
            }
            spot.Alpha = BadgerFighter.Alpha(_bgStage);
            BadgerArtSize("badger_idle_b", 1f, out float aw, out float ah);
            var mark = BadgerFighterPlace.Rest(box, aw, ah, L.ColR.x - 2f);
            spot.Unit = mark.Unit; // b129 v4: battle badger at 1.0
            spot.FaceLeft = mark.FaceLeft;
            float bh = box.BossFighter.height;
            var foot = mark.Foot;
            var bird = mark.Bird;
            float icon = mark.Icon;
            float jolt = BadgerDuel.Jolt(age) * 8f * L.S;
            float travel = beat == BadgerDuelBeat.Lunge ? BadgerDuel.Travel(age) : 0f;
            float reach = (box.BossFighter.center.x - box.Middle.center.x) * 0.55f;
            if (reach < 4f) reach = box.BossFighter.width * 0.35f;

            var act = BadgerRoundAct.Kind.None;
            float hitLean = 0f;
            if (_bgResolved && _bgStage == BadgerStage.Verdict)
                act = BadgerRoundAct.Of(_bgRound.PlayerGained, _bgRound.BossGained);
            bool endBeat = (_bgStage == BadgerStage.Outro || _bgStage == BadgerStage.Over)
                && _bgFight != null && _bgFight.Result != BadgerResult.Playing;

            if (endBeat)
            {
                bool playerWon = _bgFight.Result == BadgerResult.PlayerWon;
                float oage = _bgStage == BadgerStage.Over ? BadgerOutro.Duration : _bgT;
                pose = BadgerAnim.Outro(playerWon, oage, now);
                float shift = BadgerOutro.BackOff(playerWon, oage) * box.BossFighter.width * 0.42f;
                foot.x += shift;
                bird.y -= BadgerOutro.Proud(playerWon, oage) * icon;
                spot.BirdDegrees = BadgerOutro.Droop(playerWon, oage);
                spot.BirdFolded = !playerWon;
            }
            else if (beat == BadgerDuelBeat.Lunge)
            {
                foot.x -= travel * reach;
                bird.x -= jolt;
                foot.y -= travel * bh * 0.08f;
            }
            else if (act == BadgerRoundAct.Kind.Swipe)
            {
                float gap = foot.x - bird.x;
                if (gap < 8f) gap = box.BossFighter.width;
                foot.x -= BadgerSwipe.Lunge(_bgT) * gap * 0.48f;
                foot.y -= BadgerSwipe.Lift(_bgT) * bh * 0.10f;
                float recoil = BadgerRoundAct.Recoil(_bgT);
                bird.x -= recoil * icon * 0.55f;
                bird.y += recoil * icon * 0.08f;
                spot.BirdDegrees = recoil * 14f;
            }
            else if (act == BadgerRoundAct.Kind.Peck)
            {
                float peck = BadgerRoundAct.Peck(_bgT);
                float gap = foot.x - bird.x;
                if (gap < 8f) gap = box.BossFighter.width;
                bird.x += peck * gap * 0.72f;
                bird.y -= Mathf.Sin(peck * Mathf.PI) * icon * 0.10f;
                spot.BirdDegrees = 12f * peck;
                spot.BirdInFront = peck > 0.08f;
                // The badger leans off the peck. Later wear leans further.
                // Applied after the pose copy so the pose degrees do not wipe it.
                hitLean = BadgerPolish.HitLean(wearNow, peck);
                foot.x += hitLean * 0.35f;
            }
            else if (beat == BadgerDuelBeat.Swipe)
            {
                float gap = foot.x - L.Arena.center.x;
                if (gap < 8f) gap = box.BossFighter.width;
                foot.x -= BadgerSwipe.Lunge(age) * gap * 0.62f;
                foot.y -= BadgerSwipe.Lift(age) * bh * 0.12f;
            }

            foot.x += BadgerShakeX(BadgerBodyKey, L.S);
            float minBird = L.ColL.xMax + (mark.Bird.x - mark.BirdBox.x) + 2f; // b130: the painted bird never sits on the YOU column
            if (bird.x < minBird) bird.x = minBird;

            spot.Frame = pose.Frame;
            spot.Under = pose.Under;
            spot.FrameAlpha = pose.FrameAlpha;
            spot.UnderAlpha = pose.UnderAlpha;
            spot.Sx = pose.ScaleX;
            spot.Sy = pose.ScaleY;
            spot.Degrees = pose.Degrees + hitLean;
            var moodNow = BadgerMoodNow();
            BadgerPolish.WearScale(wearNow, out float wx, out float wy);
            spot.Sx *= wx;
            spot.Sy *= wy;
            spot.Degrees += BadgerPolish.EarsBack(moodNow);
            if (_bgStage == BadgerStage.SlamTell)
                spot.Degrees += BossBeat.PawTwitch(_bgTellT);
            BadgerArtSize(spot.Frame, spot.Unit, out float w, out float h);
            // Stay left of ColR on every beat (SE wide crouch/leap/shrug frames).
            // Squash widens the sprite; keep that width inside the column too.
            float wide = w * (spot.Sx > 1f ? spot.Sx : 1f);
            float rightLimit = L.ColR.x - 2f;
            foot.x = BadgerFighterPlace.ClampFootX(foot.x, wide, rightLimit);

            spot.Foot = foot;
            spot.Bird = bird;
            spot.Icon = icon;
            spot.BirdBox = new Rect(mark.BirdBox.x + (bird.x - mark.Bird.x), mark.BirdBox.y + (bird.y - mark.Bird.y), mark.BirdBox.width, mark.BirdBox.height);
            BadgerSlam.EyeUv(spot.Frame, out float eu, out float ev);
            float ew = w * (spot.Sx > 0.05f ? spot.Sx : 1f);
            float eh = h * (spot.Sy > 0.05f ? spot.Sy : 1f);
            spot.Eye = new Vector2(
                foot.x - ew * 0.5f + ew * eu,
                foot.y - eh + eh * ev);
            return spot;
        }

        // Page-zoom pivot. The shrug has its own eye for the glint; the camera stays
        // on the leap_3 pupil for the whole zoom so the view does not lurch.
        static Vector2 BadgerSlamPivot(BadgerActorSpot spot, float zoom)
        {
            if (zoom <= 1.001f || spot.Frame == BadgerSlam.FaceFrame) return spot.Eye;
            BadgerArtSize(BadgerSlam.FaceFrame, spot.Unit, out float w, out float h);
            return new Vector2(
                spot.Foot.x - w * 0.5f + w * BadgerSlam.EyeU,
                spot.Foot.y - h + h * BadgerSlam.EyeV);
        }

        static void BadgerArtSize(string name, float unit, out float w, out float h)
        {
            var spr = SpriteCatalog.BadgerArt(name);
            if (spr == null || unit <= 0f)
            {
                w = 480f * Mathf.Max(0f, unit);
                h = 640f * Mathf.Max(0f, unit);
                return;
            }
            w = spr.rect.width * unit;
            h = spr.rect.height * unit;
        }

        // One badger and the player's hummingbird, every fight stage. The badger art faces
        // left, so he stands in the boss box. The bird art faces right, in the you box.
        // The page zoom, not a second scale here, is the Don't Care camera.
        void DrawBadgerFighters(BadgerRects L)
        {
            if (!BadgerFighter.BothVisible(_bgStage)) return;
            var a = L.Arena;
            if (a.height < 8f || a.width < 8f) return;
            var spot = BadgerActorNow(L);
            _bgDuelEye = spot.Eye;
            _bgDuelEyeOn = spot.Alpha > 0.01f;
            _bgFxFoot = spot.Foot;
            _bgFxBird = spot.Bird;
            if (spot.Icon > 4f) _bgFxSpan = spot.Icon;
            if (!GuiPaint() || spot.Alpha < 0.01f) return;

            float clock = BadgerBirdClock();
            if (!spot.BirdInFront) DrawBadgerBird(spot, clock);

            var keep = GUI.matrix;
            var drawn = new Rect();
            bool iced = false;
            try
            {
                if (Mathf.Abs(spot.Degrees) > 0.01f)
                    GUIUtility.RotateAroundPivot(spot.Degrees, spot.Foot);
                float fa = spot.FrameAlpha;
                float ua = spot.UnderAlpha;
                bool frameOk = !string.IsNullOrEmpty(spot.Frame) && SpriteCatalog.BadgerArt(spot.Frame) != null;
                bool underOk = !string.IsNullOrEmpty(spot.Under) && spot.Under != spot.Frame
                    && SpriteCatalog.BadgerArt(spot.Under) != null;
                SpriteAnim.Cover(ref fa, ref ua, frameOk, underOk);
                if (DrawBadgerIceBlock(spot, L, out drawn)) iced = drawn.width > 1f;
                else if (underOk && ua > 0.02f)
                    DrawBadgerArt(spot.Under, spot.Foot, spot.Unit, spot.Alpha * ua, 0f, spot.Sx, spot.Sy);
                if (drawn.width < 1f && frameOk && fa > 0.02f)
                    drawn = DrawBadgerArt(spot.Frame, spot.Foot, spot.Unit, spot.Alpha * fa, 0f, spot.Sx, spot.Sy);
                if (!iced && drawn.width > 1f)
                    DrawBadgerWear(drawn, spot.Frame, BadgerMoodNow(), BadgerWearNow(), spot.Alpha);
            }
            finally
            {
                GUI.matrix = keep;
            }
            if (drawn.width > 1f)
            {
                float eu, ev;
                if (_bgStage == BadgerStage.SlamTell)
                {
                    eu = BadgerPolish.TellEyeU;
                    ev = BadgerPolish.TellEyeV;
                }
                else BadgerSlam.EyeUv(spot.Frame, out eu, out ev);
                _bgDuelEye = new Vector2(
                    drawn.x + drawn.width * eu,
                    drawn.y + drawn.height * ev);
            }

            if (_bgStage == BadgerStage.SlamTell)
            {
                float g = BossBeat.Glint(_bgTellT);
                if (g > 0.02f) DrawBadgerGlint(_bgDuelEye, PhoneArea.H * 0.04f, g);
            }

            if (spot.BirdInFront) DrawBadgerBird(spot, clock);
            DrawBadgerRoundHit(spot);
        }

        // Wear-stage art through BadgerWearArt.PaintWear (same helper the stills use).
        // Mood still paints sweat and pinned ears on the unpadded body.
        void DrawBadgerWear(Rect body, string frame, BadgerMood mood, BadgerWear wear, float alpha)
        {
            if (!GuiPaint() || body.width < 4f || body.height < 4f) return;
            var prev = GUI.color;
            if (!PaintWear(body, frame, wear, alpha))
            {
                int n = BadgerPolish.BruiseCount(wear);
                for (int i = 0; i < n; i++)
                {
                    BadgerPolish.BruiseUv(i, out float u, out float v, out float rad);
                    float rx = body.width * rad;
                    float ry = body.height * rad * 0.75f;
                    var at = new Vector2(body.x + body.width * u, body.y + body.height * v);
                    GUI.color = new Color(0.45f, 0.32f, 0.55f, 0.72f);
                    GUI.DrawTexture(new Rect(at.x - rx, at.y - ry, rx * 2f, ry * 2f), Texture2D.whiteTexture);
                }
            }
            if (BadgerPolish.EarsBack(mood) > 0.5f)
            {
                GUI.color = new Color(0.22f, 0.14f, 0.10f, 0.80f);
                for (int i = 0; i < 2; i++)
                {
                    BadgerPolish.EarUv(i, out float u, out float v);
                    float rx = body.width * 0.035f;
                    float ry = body.height * 0.028f;
                    var at = new Vector2(body.x + body.width * u, body.y + body.height * v);
                    GUI.DrawTexture(new Rect(at.x - rx, at.y - ry, rx * 2f, ry * 2f), Texture2D.whiteTexture);
                }
            }
            if (BadgerPolish.Sweat(mood))
            {
                GUI.color = new Color(0.75f, 0.90f, 0.98f, 0.90f);
                for (int i = 0; i < 2; i++)
                {
                    BadgerPolish.SweatUv(i, out float u, out float v);
                    float drop = 1f + 0.15f * Mathf.Sin(PlayClock.Now * 3f + i);
                    float rx = body.width * 0.018f;
                    float ry = body.height * 0.032f * drop;
                    var at = new Vector2(body.x + body.width * u, body.y + body.height * v);
                    GUI.DrawTexture(new Rect(at.x - rx, at.y - ry * 0.2f, rx * 2f, ry), Texture2D.whiteTexture);
                }
            }
            GUI.color = prev;
        }

        // One blit of the v4_final stage PNG (halo already inside). Returns false when
        // the sprite is missing so the caller can fall back to the old bruise discs.
        static bool PaintWear(Rect body, string frame, BadgerWear wear, float alpha)
        {
            if (alpha < 0.01f) return false;
            var baseSpr = SpriteCatalog.BadgerArt(frame);
            float srcW = baseSpr != null ? baseSpr.rect.width : body.width;
            float srcH = baseSpr != null ? baseSpr.rect.height : body.height;
            var blit = BadgerWearArt.PaintWear(frame, body, srcW, srcH, wear);
            if (string.IsNullOrEmpty(blit.Sprite) || blit.Dest.width < 4f) return false;
            var spr = SpriteCatalog.BadgerArt(blit.Sprite);
            if (spr == null || spr.texture == null) return false;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            DrawSprite(blit.Dest, spr, false);
            return true;
        }

        // Full-screen white on the winning hit. BossBeat owns the fade.
        void DrawBadgerBlowFlash()
        {
            if (!_bgBlow || !GuiPaint()) return;
            float a = BadgerPolish.BlowFlash(_bgBlowT);
            if (a < 0.02f) return;
            var prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, a);
            GUI.DrawTexture(new Rect(0f, 0f, PhoneArea.W, PhoneArea.H), Texture2D.whiteTexture);
            GUI.color = prev;
        }

        // b129: while he is iced the fighter is the tall ice block (standing, paws up), feet on his mark, with the same
        // held shiver / break-free struggle the comb uses. False (draw the pose) when not frozen or the art is missing.
        bool DrawBadgerIceBlock(BadgerActorSpot spot, BadgerRects L, out Rect drawn)
        {
            drawn = default;
            if (_bgFight == null) { _bgIceWas = false; return false; }
            float now = PlayClock.Now;
            var probe = new Rect(0f, 0f, 10f, 10f);
            BadgerFreezeLook(ref probe, L.S, out float ice, out float shatterT);
            bool iced = ice >= 0.5f;
            // Rising edge: the spray just landed. First frame of his break-free turn: the thaw.
            if (iced && !_bgIceWas && !BadgerIceAnim.Thawing(now))
            {
                BadgerIceAnim.Freeze(now);
                if (!_bgFight.CoachFight && _bgStage == BadgerStage.YourPick) _bgLine = BadgerCopy.FreezeSprayFrozen;
            }
            _bgIceWas = iced;
            if (_bgResolved && _bgRound.FreezeBeat == 2 && _bgThawRound != _bgRoundN)
            {
                _bgThawRound = _bgRoundN;
                BadgerIceAnim.Thaw(now);
            }
            bool thawing = BadgerIceAnim.Thawing(now);
            if (!iced && !thawing) return false;
            var spr = SpriteCatalog.BadgerArt(Badger129.FrozenBlockArt);
            if (spr == null) return false;
            float idleH = BadgerArtHeight("badger_idle_b", spot.Unit);
            float unit = idleH * Badger129.FrozenBlockHeight / Mathf.Max(1f, spr.rect.height);
            var foot = spot.Foot;
            float w = spr.rect.width * unit;
            foot.x = BadgerFighterPlace.ClampFootX(foot.x, w, L.ColR.x - 2f);
            var block = SpriteAnim.Dest(Badger129.FrozenBlockArt, spr.rect.width, spr.rect.height, foot, unit, 1f, 1f);
            var keep = GUI.matrix;
            try
            {
                if (thawing) return DrawBadgerThaw(spot, L, spr, block, foot, unit, BadgerIceAnim.ThawAge(now), out drawn);
                float age = BadgerIceAnim.FreezeAge(now);
                if (age >= BadgerIceAnim.ShiverFrom) foot.x += SparkleFx.Shiver(now, 2.2f * L.S * (age < BadgerIceAnim.FreezeSeconds ? 1.6f : 1f));
                block.x = foot.x - block.width * 0.5f;
                if (age < BadgerIceAnim.FreezeSeconds)
                {
                    // snapped into the hands-up pose (no-ice art at 100%), then the block revealed bottom-up through a clip
                    float g = BadgerIceAnim.Grow(age);
                    if (GuiPaint())
                    {
                        var hands = SpriteCatalog.BadgerArt(BadgerIceAnim.HandsUpArt);
                        if (hands != null)
                        {
                            float hu = unit * BadgerIceAnim.HandsUpScale;
                            var hr = new Rect(block.x + BadgerIceAnim.HandsUpX * unit, block.y + BadgerIceAnim.HandsUpY * unit,
                                hands.rect.width * hu, hands.rect.height * hu);
                            DrawSpriteRect(hands, hr, spot.Alpha);
                        }
                        if (g > 0.001f)
                        {
                            var clip = new Rect(block.x - 4f, block.yMax - block.height * g, block.width + 8f, block.height * g + 4f);
                            GUI.BeginClip(clip);
                            DrawSpriteRect(spr, new Rect(block.x - clip.x, block.y - clip.y, block.width, block.height), spot.Alpha);
                            GUI.EndClip();
                            // frost line riding the top of the ice
                            SparkleFx.DrawGlow(new Vector2(block.center.x, clip.y), block.width * 0.9f, new Color(0.85f, 0.97f, 1f, 0.55f * (1f - g * 0.6f)));
                        }
                    }
                    float sp = BadgerIceAnim.SprayAlpha(age);
                    if (sp > 0.01f && GuiPaint())
                    {
                        var chest = new Vector2(block.center.x, block.y + block.height * 0.45f);
                        SparkleFx.DrawGlow(chest, block.width * (0.7f + 0.8f * (1f - sp)), new Color(0.45f, 0.75f, 1f, 0.75f * sp));
                        for (int k = 0; k < 4; k++)
                            SparkleFx.DrawAt(chest + new Vector2((k - 1.5f) * block.width * 0.18f, (k % 2 == 0 ? -1f : 1f) * block.height * 0.08f),
                                block.width * 0.14f, sp, SparkleFx.IceTint, false);
                    }
                    if (age >= BadgerIceAnim.GlintAt && GuiPaint())
                    {
                        float ga = Mathf.Sin(Mathf.Clamp01((age - BadgerIceAnim.GlintAt) / (BadgerIceAnim.FreezeSeconds - BadgerIceAnim.GlintAt)) * Mathf.PI);
                        SparkleFx.DrawAt(new Vector2(block.x + block.width * 0.78f, block.y + block.height * 0.16f), block.width * 0.42f, ga);
                    }
                    drawn = block;
                    return true;
                }
                drawn = DrawBadgerArt(Badger129.FrozenBlockArt, foot, unit, spot.Alpha);
                return true;
            }
            finally { GUI.matrix = keep; GUI.color = Color.white; }
        }
        bool _bgIceWas;

        static void DrawSpriteRect(Sprite sp, Rect r, float alpha)
        {
            var tr = sp.textureRect;
            float tw = sp.texture.width, th = sp.texture.height;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTextureWithTexCoords(r, sp.texture, new Rect(tr.x / tw, tr.y / th, tr.width / tw, tr.height / th), true);
            GUI.color = Color.white;
        }

        static bool BadgerFrozenLine(string line)
            => line == BadgerCopy.FreezeSprayFrozen || line == BadgerCopy.HotSauceFrozen;

        static GUIStyle _bandCapStyle;
        // Authored rows only ('/' or newline), centered in the band, font fit so the longest row and every row fit.
        public static string[] BandCaptionRows(string text)
        {
            var raw = (text ?? "").Replace(" / ", "\n").Replace("/", "\n").Split('\n');
            var rows = new System.Collections.Generic.List<string>();
            foreach (var r in raw) { var t = r.Trim(); if (t.Length > 0) rows.Add(t); }
            return rows.ToArray();
        }
        public static Rect BandCaptionLast;
        void PaintBadgerBandCaption(string text, Rect band, float s, float fade)
        {
            var rows = BandCaptionRows(text);
            if (rows.Length == 0) return;
            if (_bandCapStyle == null)
            {
                _bandCapStyle = new GUIStyle(CoachLineStyle()) { wordWrap = false, alignment = TextAnchor.MiddleCenter, richText = false };
                _bandCapStyle.padding = new RectOffset(0, 0, 0, 0); _bandCapStyle.margin = new RectOffset(0, 0, 0, 0);
            }
            var st = _bandCapStyle;
            float padX = 14f * s, padY = 6f * s;
            int px = Mathf.RoundToInt(32f * s);
            float rowH = 0f, maxW = 0f;
            var gc = new GUIContent();
            for (; px > 10; px--)
            {
                st.fontSize = px; maxW = 0f;
                foreach (var r in rows) { gc.text = r; maxW = Mathf.Max(maxW, st.CalcSize(gc).x); }
                rowH = st.lineHeight * 1.05f;
                if (maxW <= band.width - 2f * padX && rowH * rows.Length <= band.height - 2f * padY) break;
            }
            float h = rowH * rows.Length + 2f * padY;
            var plate = new Rect(band.x, band.center.y - h * 0.5f, band.width, h);
            BandCaptionLast = plate;
            var keep = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, CaptionStyles.BannerAlpha * fade);
            GUI.DrawTexture(plate, Texture2D.whiteTexture);
            GUI.color = Color.white;
            for (int i = 0; i < rows.Length; i++)
            {
                var rr = new Rect(plate.x, plate.y + padY + rowH * i, plate.width, rowH);
                gc.text = rows[i];
                st.normal.textColor = new Color(0f, 0f, 0f, 0.6f * fade);
                GUI.Label(new Rect(rr.x + 2f * s, rr.y + 2f * s, rr.width, rr.height), gc, st);
                st.normal.textColor = new Color(1f, 1f, 1f, fade);
                GUI.Label(rr, gc, st);
            }
            GUI.color = keep;
        }
        int _bgThawRound = -1;

        // Thaw: two wobbles while cracks spread, then the block shatters (chunks fall and fade) and he is back in his
        // normal stance with a shake-off and a small hop. Crack and shatter sounds from SfxLibrary.
        bool DrawBadgerThaw(BadgerActorSpot spot, BadgerRects L, Sprite spr, Rect block, Vector2 foot, float unit, float b, out Rect drawn)
        {
            drawn = default;
            if (b < BadgerIceAnim.WobbleSeconds)
            {
                if (b >= BadgerIceAnim.CrackFrom && !BadgerIceAnim.CrackPlayed) { BadgerIceAnim.CrackPlayed = true; SfxLibrary.IceCrack(); }
                float deg = BadgerIceAnim.Wobble(b);
                if (Mathf.Abs(deg) > 0.01f) GUIUtility.RotateAroundPivot(deg, foot);
                drawn = DrawBadgerArt(Badger129.FrozenBlockArt, foot, unit, spot.Alpha);
                float c = BadgerIceAnim.Crack(b);
                if (c > 0.01f && GuiPaint())
                {
                    var o = new Vector2(block.center.x, block.y + block.height * 0.42f);
                    for (int k = 0; k < 7; k++)
                    {
                        float ang = k * 0.9f + 0.4f;
                        var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) * 1.4f);
                        var mid = o + dir * block.width * 0.22f * c + new Vector2(dir.y, -dir.x) * block.width * 0.04f;
                        var end = o + dir * block.width * 0.46f * c;
                        var col = new Color(1f, 1f, 1f, 0.9f);
                        SparkleFx.Segment(o, mid, Mathf.Max(1.5f, 2.2f * L.S), col);
                        SparkleFx.Segment(mid, end, Mathf.Max(1.2f, 1.6f * L.S), col);
                    }
                }
                return true;
            }
            if (!BadgerIceAnim.ShatterPlayed) { BadgerIceAnim.ShatterPlayed = true; SfxLibrary.IceShatter(); }
            // back to his stance: the duel pose, a small hop and a shake-off
            var f2 = spot.Foot;
            f2.y -= BadgerIceAnim.Hop(b) * BadgerArtHeight("badger_idle_b", spot.Unit) * 0.10f;
            float shake = BadgerIceAnim.ShakeOff(b);
            if (Mathf.Abs(shake) > 0.01f) GUIUtility.RotateAroundPivot(shake, f2);
            string frame = SpriteCatalog.BadgerArt(spot.Frame) != null ? spot.Frame : "badger_idle_b";
            drawn = DrawBadgerArt(frame, f2, spot.Unit, spot.Alpha, 0f, spot.Sx, spot.Sy);
            GUI.matrix = Matrix4x4.identity * GUI.matrix;
            if (GuiPaint()) SparkleFx.DrawShatter(block, BadgerIceAnim.ShatterT(b), Badger129.ShatterIce);
            return true;
        }

        // One wingbeat across the peck or the recoil. AvatarFlapRate is 1.25 and a
        // six-pose cycle is 0.25 of that clock, so 0.20 of fight-time is one flap.
        float BadgerBirdClock()
        {
            if (_bgResolved && _bgStage == BadgerStage.Verdict)
            {
                var act = BadgerRoundAct.Of(_bgRound.PlayerGained, _bgRound.BossGained);
                if (act == BadgerRoundAct.Kind.Peck && BadgerRoundAct.PeckSeconds > 0.01f)
                    return (_bgT / BadgerRoundAct.PeckSeconds) * 0.20f;
                if (act == BadgerRoundAct.Kind.Swipe && BadgerRoundAct.RecoilSeconds > 0.01f)
                {
                    float age = _bgT - BadgerSwipe.HitAt;
                    if (age < 0f) age = 0f;
                    return (age / BadgerRoundAct.RecoilSeconds) * 0.20f;
                }
            }
            return PlayClock.Now;
        }

        // The player's hummingbird. faceLeft stays false so the art faces the badger.
        // Folded wings are the droop. A tilt is the recoil or the peck.
        void DrawBadgerBird(BadgerActorSpot spot, float now)
        {
            if (spot.Icon < 2f) return;
            var keep = GUI.matrix;
            try
            {
                GUI.color = Color.white;
                if (Mathf.Abs(spot.BirdDegrees) > 0.01f)
                    GUIUtility.RotateAroundPivot(spot.BirdDegrees, spot.Bird);
                DrawAvatarBird(SavedAvatar(), SavedAvatarKit(), spot.Bird, spot.Icon, spot.FaceLeft, spot.BirdFolded, now, false, BirdIdle.KitSlotBadger);
            }
            finally
            {
                GUI.matrix = keep;
                GUI.color = Color.white;
            }
        }

        // Badger score: the shared claw across the bird, and the theft splat on the bird.
        void DrawBadgerRoundHit(BadgerActorSpot spot)
        {
            if (!_bgResolved || _bgStage != BadgerStage.Verdict) return;
            if (BadgerRoundAct.Of(_bgRound.PlayerGained, _bgRound.BossGained) != BadgerRoundAct.Kind.Swipe) return;
            var claw = spot.BirdBox;
            claw.x -= claw.width * 0.12f;
            claw.width *= 1.24f;
            DrawBadgerClaws(claw, _bgT);
            if (!BadgerCopy.ShowTheftSplat(_bgRound)) return;
            float a = BadgerRoundAct.SplatAlpha(_bgT);
            if (a < 0.02f) return;
            float artW = 775f;
            var spr = SpriteCatalog.BadgerArt("honey_splat");
            if (spr != null && spr.rect.width > 1f) artW = spr.rect.width;
            float su = spot.Icon * 0.90f / artW;
            float sh = BadgerArtHeight("honey_splat", su);
            DrawBadgerArt("honey_splat", new Vector2(spot.Bird.x, spot.Bird.y + sh * 0.5f), su, a);
        }

        // Which pose the one badger plays this frame. Opening through the result.
        void BadgerDuelPose(out BadgerDuelBeat beat, out float age)
        {
            beat = BadgerDuelBeat.Idle;
            age = 0f;
            if (_bgStage == BadgerStage.Opening)
            {
                float t = _bgWashOut > 0f ? 0f : _bgOpenT;
                if (BadgerSwipe.Active(t))
                {
                    beat = BadgerDuelBeat.Swipe;
                    age = t;
                }
                return;
            }
            if (_bgStage == BadgerStage.DontCare)
            {
                float t = _bgSlamT;
                var b = BadgerSlam.BeatAt(t);
                if (b == BadgerSlamBeat.Nasty)
                    beat = BadgerDuelBeat.Shrug;
                else if (b == BadgerSlamBeat.ZoomIn || b == BadgerSlamBeat.ZoomOut)
                {
                    // Lock the close-up frame so the glint UV sits on this pupil.
                    beat = BadgerDuelBeat.Leap;
                    age = 0.99f;
                }
                else if (b == BadgerSlamBeat.Speed)
                {
                    beat = BadgerDuelBeat.Leap;
                    float u = BadgerSlam.SpeedSeconds <= 0f ? 1f : (t - BadgerSlam.WordsEnd) / BadgerSlam.SpeedSeconds;
                    age = Mathf.Clamp01(u) * 0.70f;
                }
                return;
            }
            if (_bgResolved && _bgStage == BadgerStage.Verdict
                && BadgerRoundAct.Of(_bgRound.PlayerGained, _bgRound.BossGained) == BadgerRoundAct.Kind.Swipe
                && BadgerSwipe.Active(_bgT))
            {
                beat = BadgerDuelBeat.Swipe;
                age = _bgT;
                return;
            }
            if (_bgBossIx >= 0 && _bgStage == BadgerStage.YourPick)
            {
                age = AdPause.Unscaled - _bgBossAt;
                if (age < BadgerDuel.LungeSeconds) beat = BadgerDuelBeat.Lunge;
            }
        }

        // Comic honey splat for the Don't Care zoom only. Fitted to the column so the
        // zoomed page scales it with the scene. A round's splat sits on the bird.
        static void DrawBadgerColumnSplat(BadgerRects L, float alpha)
        {
            var gutter = L.ColL;
            if (!GuiPaint() || alpha < 0.02f || gutter.width < 2f || gutter.height < 2f) return;
            var spr = SpriteCatalog.BadgerArt("honey_splat");
            float artW = spr != null ? spr.rect.width : 775f;
            if (artW < 1f) artW = 775f;
            float su = gutter.width * 0.92f / artW;
            float sh = BadgerArtHeight("honey_splat", su);
            float maxH = gutter.height * 0.55f;
            if (sh > maxH && sh > 0.01f)
            {
                su *= maxH / sh;
                sh = maxH;
            }
            var foot = new Vector2(gutter.center.x, gutter.y + gutter.height * 0.42f + sh * 0.5f);
            if (foot.y - sh < gutter.y) foot.y = gutter.y + sh;
            if (foot.y > gutter.yMax) foot.y = gutter.yMax;
            DrawBadgerArt("honey_splat", foot, su, alpha);
        }

        void DrawBadgerWinGlow(Rect r)
        {
            if (!GuiPaint()) return;
            float pad = r.width * 0.28f;
            GUI.color = new Color(1f, 0.86f, 0.30f, 0.65f);
            GUI.DrawTexture(new Rect(r.x - pad, r.y - pad, r.width + pad * 2f, r.height + pad * 2f), GlowTex(), ScaleMode.StretchToFill, true);
            GUI.color = Color.white;
        }

        // Honey splat over a knocked tile: a gold blob and three drips. Comic, not injury.
        static void DrawBadgerSplat(Rect r)
        {
            if (!GuiPaint()) return;
            var glow = GlowTex();
            GUI.color = new Color(1f, 0.74f, 0.10f, 0.92f);
            float big = r.width * 0.95f;
            GUI.DrawTexture(new Rect(r.center.x - big * 0.5f, r.center.y - big * 0.5f, big, big), glow, ScaleMode.StretchToFill, true);
            for (int i = 0; i < 3; i++)
            {
                float d = r.width * (0.26f - i * 0.04f);
                float dx = (i - 1) * r.width * 0.30f;
                float dy = r.height * (0.34f + 0.06f * i);
                GUI.DrawTexture(new Rect(r.center.x + dx - d * 0.5f, r.center.y + dy - d * 0.5f, d, d), glow, ScaleMode.StretchToFill, true);
            }
            GUI.color = Color.white;
        }

        void DrawBadgerPowers(BadgerRects L, bool live)
        {
            float s = L.S;
            // The end beat keeps the stage clear. Continue comes with the result line.
            if (_bgStage == BadgerStage.Outro) return;
            if (_bgStage == BadgerStage.Over)
            {
                var btn = BadgerContinueRect(L); // one rect: the button, the glove and the gate
                if (BadgerButton(btn, "Continue", null, true, false, false, true, Color.white, 0f))
                    LeaveBadger();
                return;
            }
            float gap = 8f * s;
            float w = (L.Power.width - gap * 3f) / 4f;
            for (int k = 0; k < 4; k++)
            {
                var p = (BadgerPower)(k + 1);
                var r = new Rect(L.Power.x + k * (w + gap), L.Power.y, w, L.Power.height);
                bool used = _bgFight != null && !_bgFight.PowerReady(p);
                bool locked = BadgerPowerLockedShown();
                bool inPick = _bgStage == BadgerStage.YourPick && !locked;
                bool armedNow = _bgArmed == p && (_bgStage == BadgerStage.YourPick || _bgStage == BadgerStage.Reveal
                    || _bgStage == BadgerStage.DontCare || _bgStage == BadgerStage.Verdict);
                bool lit = armedNow;
                // One state per button: LOCKED (moves 1-3, held through move 3's verdict), then FREE / price,
                // ARMED while it is the chosen power (through the reveal), then USED. Never FREE -> LOCKED -> FREE.
                string sub = armedNow ? BadgerCopy.Armed : used ? "USED" : locked ? BadgerCopy.Locked : BadgerPowerPriceSub(p);
                Color ink = p == BadgerPower.HotSauce ? new Color(1f, 0.55f, 0.35f)
                    : p == BadgerPower.FreezeSpray ? new Color(0.45f, 0.85f, 1f)
                    : Color.white;
                bool guideBlocks = _bgPowerGuideOn && p != _bgGuidePower;
                // Hot Sauce is greyed (same dim as USED / not-your-turn) while the badger is frozen.
                bool frozenOut = _bgFight != null && (!used || p == BadgerPower.FreezeSpray) && _bgFight.PowerFrozenOut(p);
                if (frozenOut && !armedNow) sub = BadgerCopy.HotSauceFrozenSub;
                guideBlocks |= frozenOut;
                // b127: break-free step: every power but the frozen Hot Sauce is inert.
                bool breakFree = BadgerBreakFreeLive();
                if (breakFree && p != BadgerPower.HotSauce && p != BadgerPower.FreezeSpray) guideBlocks = true;
                // Frozen Hot Sauce stays tappable (it says why); it is only drawn disabled.
                bool hsTap = frozenOut && BadgerFight.IsBlock(p) && inPick;
                var rr = r;
                if (BadgerFight.IsBlock(p))
                {
                    var fui = p == BadgerPower.FreezeSpray ? _bgFrozenUiFreeze : _bgFrozenUi;
                    fui.Observe(frozenOut, _bgFight != null && _bgFight.Result == BadgerResult.Playing);
                    if (used && !frozenOut) fui.CancelPop(); // spent Freeze Spray returns to USED, no pop
                    float pop = fui.PopScale(AdPause.Unscaled, inPick);
                    if (pop != 1f)
                    {
                        rr = new Rect(r.center.x - r.width * pop * 0.5f, r.center.y - r.height * pop * 0.5f, r.width * pop, r.height * pop);
                        DrawBadgerGuideGlow(r);
                    }
                }
                if (_bgPowerGuideOn && p == _bgGuidePower && BadgerPowerGuideStage() == 1) DrawBadgerGuideGlow(r);
                bool dim = used || !inPick || guideBlocks;
                bool fire = BadgerPowerTile(rr, p, sub, dim, frozenOut && !armedNow, lit,
                    live && inPick && (hsTap || (!used && !guideBlocks)), BadgerShakeX(BadgerPowerKey + (int)p, s));
                if (frozenOut) DrawBadgerIceBadge(rr);
                if (fire) ClickBadgerPower(p);
            }
        }

        // b128 (Brandon's flat rounded-square mock): the one helper for all four power buttons. Painted icon on top,
        // then the name, then the price or state (FREE / $price, ARMED, USED, LOCKED, FROZEN). frozen = greyed face and
        // icon; the caller adds the ice badge. The hit pad is the whole rect, as before.
        bool BadgerPowerTile(Rect hit, BadgerPower p, string sub, bool dim, bool frozen, bool lit, bool live, float shakeX)
        {
            hit.x += shakeX;
            bool held = false;
            bool fire = live && HitPad(hit, out held);
            if (!GuiPaint()) return fire;
            if (lit)
            {
                float pad = hit.height * 0.18f;
                GUI.color = new Color(1f, 0.88f, 0.36f, 0.75f);
                GUI.DrawTexture(new Rect(hit.x - pad, hit.y - pad, hit.width + pad * 2f, hit.height + pad * 2f), GlowTex(), ScaleMode.StretchToFill, true);
                GUI.color = Color.white;
            }
            var face = held ? Inset(hit, 0.03f) : hit;
            BadgerPowerTileColors(frozen, out var edge, out var fill, out var shine, out var line);
            float a = frozen ? 1f : dim ? 0.85f : 1f;
            float rad = Mathf.Min(face.width, face.height) * 0.16f;
            float bw = Mathf.Max(2f, face.height * 0.035f);
            var white = Texture2D.whiteTexture;
            var prev = GUI.color;
            GUI.color = Color.white;
            // soft drop shadow, dark rim, flat fill, lighter top half, thin cream inner line
            GUI.DrawTexture(new Rect(face.x, face.y + bw, face.width, face.height), white, ScaleMode.StretchToFill, true, 0f, new Color(0f, 0f, 0f, 0.28f * a), 0f, rad);
            GUI.DrawTexture(face, white, ScaleMode.StretchToFill, true, 0f, Fade(edge, a), 0f, rad);
            var inner = new Rect(face.x + bw, face.y + bw, face.width - bw * 2f, face.height - bw * 2f);
            float ir = Mathf.Max(1f, rad - bw);
            GUI.DrawTexture(inner, white, ScaleMode.StretchToFill, true, 0f, Fade(fill, a), 0f, ir);
            GUI.DrawTexture(new Rect(inner.x, inner.y, inner.width, inner.height * 0.5f), white, ScaleMode.StretchToFill, true, 0f, Fade(shine, a),
                Vector4.zero, new Vector4(ir, ir, 0f, 0f));
            var ln = new Rect(inner.x + bw * 0.9f, inner.y + bw * 0.9f, inner.width - bw * 1.8f, inner.height - bw * 1.8f);
            GUI.DrawTexture(ln, white, ScaleMode.StretchToFill, true, 0f, Fade(line, a), Mathf.Max(1f, bw * 0.45f), Mathf.Max(1f, ir - bw * 0.9f));
            GUI.color = prev;

            // icon on top, then the name, then the price or state
            var icon = new Rect(inner.x + inner.width * 0.12f, inner.y + inner.height * 0.07f, inner.width * 0.76f, inner.height * 0.50f);
            DrawBadgerPowerIcon(icon, p, frozen ? 1f : dim ? 0.6f : 1f, frozen);
            var st = BadgerStyle();
            string name = BadgerPowerTileName(p);
            float ta = dim && !frozen ? 0.6f : 1f;
            var nr = new Rect(inner.x + inner.width * 0.05f, inner.y + inner.height * 0.57f, inner.width * 0.90f, inner.height * 0.18f);
            st.fontSize = FitFont(st, name, nr.width, nr.height, 8, 48);
            StampLight(nr, name, st, Fade(frozen ? new Color(0.86f, 0.86f, 0.88f) : BadgerPowerInk(p), ta));
            if (!string.IsNullOrEmpty(sub))
            {
                var sr = new Rect(inner.x + inner.width * 0.05f, inner.y + inner.height * 0.75f, inner.width * 0.90f, inner.height * 0.20f);
                st.fontSize = FitFont(st, sub, sr.width, sr.height, 8, 56);
                StampLight(sr, sub, st, Fade(frozen ? new Color(0.88f, 0.88f, 0.90f) : Color.white, ta));
            }
            return fire;
        }

        static Color Fade(Color c, float a) { c.a *= a; return c; }

        // Mock colours: warm orange-brown tile; frozen is the cool grey-brown tile.
        static void BadgerPowerTileColors(bool frozen, out Color edge, out Color fill, out Color shine, out Color line)
        {
            if (frozen)
            {
                edge = new Color(0.24f, 0.20f, 0.18f, 1f);
                fill = new Color(0.40f, 0.34f, 0.30f, 1f);
                shine = new Color(0.47f, 0.41f, 0.37f, 1f);
                line = new Color(0.78f, 0.72f, 0.64f, 0.55f);
                return;
            }
            edge = new Color(0.30f, 0.15f, 0.06f, 1f);
            fill = new Color(0.66f, 0.36f, 0.14f, 1f);
            shine = new Color(0.78f, 0.46f, 0.21f, 1f);
            line = new Color(0.98f, 0.84f, 0.58f, 0.70f);
        }

        static Color BadgerPowerInk(BadgerPower p)
        {
            if (p == BadgerPower.HotSauce) return new Color(1f, 0.45f, 0.30f);
            if (p == BadgerPower.FreezeSpray) return new Color(0.45f, 0.85f, 1f);
            return new Color(1f, 0.90f, 0.40f);
        }

        // Button names (the mock): short so all four fit one row on an SE.
        public static string BadgerPowerTileName(BadgerPower p)
        {
            if (p == BadgerPower.X2) return "Double";
            if (p == BadgerPower.X3) return "Triple";
            if (p == BadgerPower.HotSauce) return "Hot Sauce";
            if (p == BadgerPower.FreezeSpray) return "Freeze";
            return "";
        }

        // b127/b128: ice badge (snowflake on an icy disc) at the top-right corner of a frozen-out button.
        void DrawBadgerIceBadge(Rect r)
        {
            if (!GuiPaint()) return;
            float d = Mathf.Min(r.width, r.height) * 0.30f;
            var disc = new Rect(r.xMax - d * 0.80f, r.y - d * 0.20f, d, d);
            var white = Texture2D.whiteTexture;
            var prev = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(disc, white, ScaleMode.StretchToFill, true, 0f, new Color(0.95f, 0.98f, 1f, 1f), 0f, d * 0.5f);
            float ring = Mathf.Max(1.5f, d * 0.09f);
            var core = new Rect(disc.x + ring, disc.y + ring, d - ring * 2f, d - ring * 2f);
            GUI.DrawTexture(core, white, ScaleMode.StretchToFill, true, 0f, new Color(0.36f, 0.72f, 0.97f, 1f), 0f, core.width * 0.5f);
            var old = GUI.matrix;
            float len = d * 0.28f, w = Mathf.Max(1.5f, d * 0.07f);
            for (int k = 0; k < 3; k++)
            {
                GUI.matrix = old;
                GUIUtility.RotateAroundPivot(k * 60f, disc.center);
                GUI.DrawTexture(new Rect(disc.center.x - len, disc.center.y - w * 0.5f, len * 2f, w), white);
            }
            GUI.matrix = old;
            GUI.color = prev;
        }

        // The coach fight's lock shows until move 3's verdict has played, so the labels never flip back and forth.
        bool BadgerPowerLockedShown()
        {
            if (_bgFight == null) return false;
            if (_bgFight.PowersLocked) return true;
            return _bgFight.CoachFight && _bgFight.RoundsResolved == BadgerTutorialScript.Moves
                && (_bgStage == BadgerStage.Reveal || _bgStage == BadgerStage.DontCare || _bgStage == BadgerStage.Verdict);
        }

        // b110: true for the first-fight lesson and the whole coach fight (every stage). Those keep the glove-driven fade.
        bool BadgerTutorialFight()
            => BadgerCaptionFade.IsTutorial(_bgLessonLive, _bgCoachFight, _bgFight != null && _bgFight.CoachFight,
                _bgStage == BadgerStage.Lesson, BadgerGuideLive(), BadgerResultLive());

        // b128: the caption spot for the current line. 'Your pick.' takes the table's AbovePower seat; the rest keep
        // the shared badger.caption banner.
        public static string BadgerCaptionSpot(string line) => line == BadgerCopy.YourPick ? "badger.pick" : "badger.caption";

        void DrawBadgerCaption(BadgerRects L)
        {
            if (!GuiPaint() || string.IsNullOrEmpty(_bgLine)) return;
            float s = L.S;
            // b130 (j): the win is a victory banner (shared reward banner helper), not a floating gold caption.
            if (BadgerWinBannerLive()) { DrawBadgerWinBanner(L); return; }
            // v2 (b116 bug): a frozen line paints in the BadgerBottomBands caption slot under the power row, hard-broken
            // on its authored '/' rows, so the banner never lands across the arena and covers the fighters.
            if (BadgerFrozenLine(_bgLine) && !BadgerTutorialFight())
            {
                PaintBadgerBandCaption(_bgLine, BadgerCaptionSeat(L.Caption, s), s, BadgerCaptionFade.For(false, _coachFade));
                return;
            }
            string spot = BadgerCaptionSpot(_bgLine);
            if (CaptionStyles.AbovePower(spot))
            {
                DrawBadgerPickCaption(L, spot);
                return;
            }
            var gold = GoldCaption(_bgLine, PhoneArea.W, PhoneArea.H);
            float w = gold.PlateW;
            float h = gold.PlateH;
            var band = BadgerCaptionSeat(L.Caption, s);
            if (w > band.width) w = band.width;
            if (h > band.height) h = band.height;
            var seat = new Rect(band.center.x - w * 0.5f, band.center.y - h * 0.5f, w, h);
            // b110: the shared caption paint multiplies its ink and plate by _coachFade, which only the tutorial glove
            // ramps. On a repeat (non-tutorial) fight it sat at 0, so the bottom dialog painted nothing. Use a full
            // fade there; every tutorial fight (lesson, coach fight, all its stages) keeps the glove's fade as before. Restored so the coach state is untouched.
            float keepFade = _coachFade;
            _coachFade = BadgerCaptionFade.For(BadgerTutorialFight(), _coachFade);
            try
            {
                CapSpot("badger.caption"); PaintCoachCaption(_bgLine, seat, s, 12, Mathf.RoundToInt(32f * s), default, false, StandardCaptionSlack);
            }
            finally { _coachFade = keepFade; }
        }

        // Snug plate in L.Pick through the shared pinned caption box (same paint as the lesson line).
        // Shared dark caption plate (DrawSolidRound kind 0) and the shared banner ink, at the table entry's size and seat.
        void DrawBadgerPickCaption(BadgerRects L, string spot)
        {
            var plate = BadgerPickPlate(L, spot, _bgLine, out var text, out int font);
            float fade = BadgerCaptionFade.For(BadgerTutorialFight(), _coachFade);
            if (fade < 0.02f) return;
            NoteTutorPlate(plate);
            DrawSolidRound(plate, Mathf.Min(plate.height * 0.30f, 14f * L.S), 0, fade * CaptionStyles.Ink(spot));
            var st = CoachLineStyle();
            int keep = st.fontSize; bool wrap = st.wordWrap; var align = st.alignment;
            try
            {
                st.fontSize = font;
                st.wordWrap = false;
                st.alignment = TextAnchor.MiddleCenter;
                var ink = text;
                ink.y -= CaptionLayout.BannerInkLift(font);
                StampBannerText(ink, _bgLine, st, fade, true);
            }
            finally { st.fontSize = keep; st.wordWrap = wrap; st.alignment = align; }
        }

    }
}
