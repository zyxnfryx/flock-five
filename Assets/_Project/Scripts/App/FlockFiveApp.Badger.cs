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
            int n = BadgerSchedule.Appearance(cleared);
            if (n < 1) n = 1;
            _bgAppearance = n;
            _bgFlagDisplay = cleared > 0 ? cleared : 0;
            _bgSeed = Random.Range(1, int.MaxValue);
            _bgLoadout = BadgerLoadout.Preload(BadgerSchedule.Tiles);
            // b123 fairness: a first visit with a thin album does not deal the player a grid of 1s.
            _bgLoadout.FairStart(n);
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
            _bgGuidePower = BadgerPower.None;
            _bgGuideRoundPower = BadgerPower.None;
            _bgPowerTile = -1;
            _bgGuideHit = default;
            _bgSlam = default;
            _bgSlamT = 0f;
            _bgSlamWord = -1;
            _bgLine = "";
            RefreshBadgerPrices();
            _splash = true;
            _home = HomeFace.Badger;
            // Phase 4: hive swipe + bee fill, PostOpen pause, then the badger leads.
            BeginBadgerOpening();
            // Battle bed: a no-op when the leap's entrance cue already owns the seat.
            if (MixDesk.Live != null) MixDesk.Live.BadgerBattle();
            return true;
        }

        // Quit mid-fight or Continue after Over. Win pays; lose / quit leaves the flag.
        void LeaveBadger()
        {
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
                BadgerSchedule.BossTileMix(_bgAppearance, _bgSeed), _bgCoachFight);
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
            _bgLine = BadgerCopy.RoundStartLine(_bgRoundN);
            // Coach fight: the round after move 3 announces the unlocked power-ups.
            _bgPowerGuideOn = false;
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
                        PlayDontCareSlam(slam);
                        break;
                    }
                    ResolveBadgerRound();
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
            _bgLine = BadgerVerdict(_bgRound);
            // b123: the forced block round says the block worked.
            if (_bgGuideRoundPower != BadgerPower.None && BadgerFight.IsBlock(_bgGuideRoundPower) && _bgRound.Power == _bgGuideRoundPower)
                _bgLine = BadgerCopy.BlockWorkedLine(_bgGuideRoundPower);
            if (_bgRound.PlayerGained > 0 || _bgRound.BossGained > 0 || BadgerFight.IsBlock(_bgRound.Power))
                Sfx.CardBump();
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
            bool stung = MixDesk.Live != null && MixDesk.Live.BadgerResult(won);
            if (won)
            {
                var at = _bgFxBird.sqrMagnitude > 4f ? _bgFxBird : _bgFxFoot;
                SparkleFx.Celebrate(at);
            }
            if (!stung)
            {
                if (won) SfxLibrary.Play("fanfare", 0.40f);
                else Sfx.Deny();
            }
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
            if (!BadgerFight.IsBlock(p) && _bgArmed == p)
            {
                _bgArmed = BadgerPower.None;
                if (!_bgPowerGuideOn) _bgLine = BadgerCopy.YourPick;
                return;
            }
            if (!_bgFight.PowerReady(p) || _bgFight.PowersLocked) return;
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
                && _bgStage == BadgerStage.YourPick && (_bgFight.GuidedTile >= 0 || _bgPowerGuideOn);
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
                if (BadgerSave.IsTutorialFirstUse(p, n) && _bgFight.PowerReady(p) && _bgFight.CanAfford(p)) return p;
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
            int tile = _bgPowerGuideOn ? _bgPowerTile : _bgFight.GuidedTile;
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
               && (_bgFight.PowersLocked || _bgPowerGuideOn || BadgerGuidePending())
               && _bgStage != BadgerStage.Over && _bgStage != BadgerStage.Outro;

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
                if (!CoachAteThisFrame() && Pressed(out var scr) && !BadgerLessonIgnores(new Vector2(scr.x, Screen.height - scr.y)))
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
            var gui = new Vector2(screen.x, Screen.height - screen.y);
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
            // Look-at arrow: the grid, the power row, then YOU / BADGER meters alternating on the last step.
            Rect look = _bgLessonAim;
            if (_bgLessonStep >= 2)
                look = Mathf.Repeat(AdPause.Unscaled - _bgLessonStepAt, 3.2f) < 1.6f ? L.ColL : L.ColR;
            CoachArrowAt(look, s);
            DrawTutorOverlay(s);
        }

        // Caption band shared by the badger plate and PreviewBadgerColumn.
        static void BadgerCaptionLimits(float screenW, float screenH, Rect safe, out float maxW, out float roomH, out float scale, out int hi)
        {
            scale = DeviceFit.UiH(screenH);
            float top = TopHudBox(screenH, safe, 0f);
            var back = BackMedalBox(scale, top, safe);
            bool have = safe.width >= 2f && safe.height >= 2f;
            float xL = Mathf.Max(8f * scale, have ? safe.xMin + 6f : 0f);
            float xR = Mathf.Min(screenW - 8f * scale, have ? safe.xMax - 6f : screenW);
            float y0 = back.yMax + 4f * scale;
            float yEnd = screenH - Mathf.Max(8f * scale, have ? safe.yMin + 4f : 0f);
            float gap = 6f * scale;
            float room = Mathf.Max(100f, yEnd - y0 - gap * 4f);
            float capH = room * 0.18f;
            float capW = xR - xL;
            const float plateX = 18f;
            const float plateY = 12f;
            maxW = capW - plateX * 2f;
            if (maxW > screenW * 0.86f) maxW = screenW * 0.86f;
            if (maxW < 8f) maxW = 8f;
            roomH = capH - plateY * 2f;
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
            var hang = CaptionLayout.BoxPad(Screen.height > 2f ? Screen.height : 720f);
            float plateX = hang.x;
            float plateY = hang.y;
            BadgerCaptionLimits(Screen.width, Screen.height, Screen.safeArea, out float maxW, out float roomH, out _, out int hi);
            var fit = FitCaptionBox(line, maxW, hi, 14);
            float boxW = fit.width;
            if (boxW > maxW) boxW = maxW;
            float boxH = fit.height;
            if (boxH > roomH) boxH = roomH;
            float x = L.Caption.center.x - boxW * 0.5f;
            float minX = L.Caption.x + plateX;
            float maxX = L.Caption.xMax - plateX - boxW;
            if (maxX < minX) maxX = minX;
            if (x < minX) x = minX;
            if (x > maxX) x = maxX;
            float yRoom = L.Caption.height - plateY * 2f;
            float y = L.Caption.y + plateY + (yRoom - boxH) * 0.5f;
            if (y < L.Caption.y + plateY) y = L.Caption.y + plateY;
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
            string line = _bgPowerGuideOn
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


        struct BadgerRects
        {
            public float S;
            public Rect Back, Title, Need, Coin;
            public Rect BossGrid, Arena, PlayerGrid, Power, Caption;
            public Rect ColL, ColR;
        }

        BadgerRects BadgerLayout()
        {
            var L = new BadgerRects();
            float s = DeviceFit.Ui();
            L.S = s;
            float top = TopHud();
            L.Back = BackMedalRect(s, top);
            var safe = Screen.safeArea;
            bool haveSafe = safe.width >= 2f && safe.height >= 2f;
            float xL = Mathf.Max(8f * s, haveSafe ? safe.xMin + 6f : 0f);
            float xR = Mathf.Min(Screen.width - 8f * s, haveSafe ? safe.xMax - 6f : Screen.width);
            float colW = Mathf.Clamp(58f * Mathf.Clamp(s, 1f, 1.35f), 64f, 96f);
            float icon = 40f * Mathf.Clamp(s, 1f, 1.35f);
            L.Coin = new Rect(xR - icon, top + 4f * s, icon, icon);
            float tx = L.Back.xMax + 8f * s;
            float tw = Mathf.Max(80f, Screen.width * 0.62f - tx);
            L.Title = new Rect(tx, top, tw, L.Back.height * 0.52f);
            L.Need = new Rect(tx, L.Title.yMax, tw, L.Back.height * 0.34f);

            float y0 = L.Back.yMax + 4f * s;
            float yEnd = Screen.height - Mathf.Max(8f * s, haveSafe ? safe.yMin + 4f : 0f);
            float gap = 6f * s;
            float room = Mathf.Max(100f, yEnd - y0 - gap * 4f);
            float capH = room * 0.18f;
            float powH = room * 0.11f;
            // Middle stage band, between the badger tiles and the player tiles.
            // Height stays put so the opening hive and bee arcs keep their size.
            float arenaH = room * 0.16f;
            float gridH = (room - capH - powH - arenaH) * 0.5f;
            float gx = xL + colW + 10f * s;
            float gw = Mathf.Max(60f, xR - colW - 10f * s - gx);
            float y = y0;
            L.BossGrid = new Rect(gx, y, gw, gridH);
            y += gridH + gap;
            L.Arena = new Rect(gx, y, gw, arenaH);
            y += arenaH + gap;
            L.PlayerGrid = new Rect(gx, y, gw, gridH);
            y += gridH + gap;
            L.Power = new Rect(xL, y, xR - xL, powH);
            y += powH + gap;
            L.Caption = new Rect(xL, y, xR - xL, capH);
            float colH = L.PlayerGrid.yMax - L.BossGrid.y;
            L.ColL = new Rect(xL, L.BossGrid.y, colW, colH);
            L.ColR = new Rect(xR - colW, L.BossGrid.y, colW, colH);
            return L;
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
                    float top = col.yMax - target * unit;
                    var pivot = new Vector2(col.center.x, (top + col.yMax) * 0.5f);
                    GUIUtility.ScaleAroundPivot(new Vector2(pulse, pulse), pivot);
                }
                int n = filled > target ? target : filled;
                if (n < 0) n = 0;
                for (int i = 0; i < target; i++)
                {
                    var seg = BadgerMeter.Comb(col, i, unit);
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
            var plate = BadgerMeter.EndPlate(col, target, unit);
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
                || _bgStage == BadgerStage.DontCare || BadgerGuideLive() || BadgerCoachHoldsBack() || BadgerResultLive())
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
            DrawCoinCluster(L.Coin, s, 0.86f);

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
            var youSlot = BadgerSlotTile(boxes.YouSlot);
            var bossSlot = BadgerSlotTile(boxes.BossSlot);
            bool tilesIn = _bgFight != null && (_bgBossIx >= 0 || _bgPlayerIx >= 0);
            bool tie = _bgResolved && _bgRound.PlayerGained <= 0 && _bgRound.BossGained <= 0
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
                if (block) r.x += Mathf.Sin(AdPause.Unscaled * 38f) * 5f * s * Mathf.Clamp01(1.2f - _bgT);
                bool win = _bgResolved && _bgRound.BossGained > 0;
                if (win) DrawBadgerWinGlow(r);
                DrawBadgerTile(r, BadgerLook.Face, honey, BadgerHide, BeeFinish.Normal, block ? 0.55f : 1f, null);
                if (win && _bgStage == BadgerStage.Verdict)
                    DrawBadgerGain(r, _bgRound.BossGained, _bgT);
                if (block) DrawBadgerSplat(r);
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

        // PLACEHOLDER art (drawn shapes, no sprites yet): a hot sauce bottle and a freeze spray can, mild cartoon.
        static void DrawBadgerPowerIcon(Rect r, BadgerPower p, float a)
        {
            if (!GuiPaint() || r.width < 2f) return;
            var prevC = GUI.color;
            var white = Texture2D.whiteTexture;
            Rect F(float x, float y, float w, float h) => new Rect(r.x + r.width * x, r.y + r.height * y, r.width * w, r.height * h);
            void Box(Rect q, Color c) { c.a *= a; GUI.color = c; GUI.DrawTexture(q, white); }
            void Round(Rect q, Color c) { c.a *= a; GUI.color = c; DrawSolidRound(q, q.height * 0.5f, 0, a); }
            var rim = new Color(0.12f, 0.06f, 0.03f, 1f);
            if (p == BadgerPower.HotSauce)
            {
                // bottle: dark outline, red body, narrow neck, black cap, cream label with a chili dot
                Round(F(0.17f, 0.36f, 0.66f, 0.62f), rim);
                Box(F(0.36f, 0.14f, 0.28f, 0.34f), rim);
                Round(F(0.21f, 0.40f, 0.58f, 0.54f), new Color(0.86f, 0.14f, 0.08f));
                Box(F(0.40f, 0.18f, 0.20f, 0.30f), new Color(0.86f, 0.14f, 0.08f));
                Box(F(0.38f, 0.04f, 0.24f, 0.16f), new Color(0.15f, 0.12f, 0.12f));
                Box(F(0.27f, 0.54f, 0.46f, 0.26f), new Color(1f, 0.93f, 0.72f));
                Round(F(0.43f, 0.60f, 0.14f, 0.14f), new Color(0.86f, 0.14f, 0.08f));
            }
            else
            {
                // spray can: dark outline, icy-blue body, silver shoulder, dark nozzle, little mist puffs
                Box(F(0.24f, 0.26f, 0.52f, 0.70f), rim);
                Box(F(0.28f, 0.30f, 0.44f, 0.62f), new Color(0.35f, 0.74f, 0.96f));
                Box(F(0.28f, 0.30f, 0.44f, 0.12f), new Color(0.82f, 0.86f, 0.90f));
                Box(F(0.40f, 0.14f, 0.20f, 0.14f), new Color(0.25f, 0.25f, 0.30f));
                Box(F(0.28f, 0.52f, 0.44f, 0.22f), new Color(0.95f, 0.98f, 1f));
                Round(F(0.62f, 0.08f, 0.16f, 0.16f), new Color(0.80f, 0.94f, 1f));
                Round(F(0.76f, 0.02f, 0.14f, 0.14f), new Color(0.80f, 0.94f, 1f));
                Round(F(0.80f, 0.20f, 0.12f, 0.12f), new Color(0.80f, 0.94f, 1f));
            }
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
            spot.Alpha = BadgerFighter.Alpha(_bgStage);
            BadgerArtSize("badger_idle_b", 1f, out float aw, out float ah);
            var mark = BadgerFighterPlace.Rest(box, aw, ah);
            spot.Unit = mark.Unit;
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
            }
            else if (beat == BadgerDuelBeat.Swipe)
            {
                float gap = foot.x - L.Arena.center.x;
                if (gap < 8f) gap = box.BossFighter.width;
                foot.x -= BadgerSwipe.Lunge(age) * gap * 0.62f;
                foot.y -= BadgerSwipe.Lift(age) * bh * 0.12f;
            }

            foot.x += BadgerShakeX(BadgerBodyKey, L.S);
            float minBird = L.ColL.x + icon * 0.45f;
            if (bird.x < minBird) bird.x = minBird;

            spot.Frame = pose.Frame;
            spot.Under = pose.Under;
            spot.FrameAlpha = pose.FrameAlpha;
            spot.UnderAlpha = pose.UnderAlpha;
            spot.Sx = pose.ScaleX;
            spot.Sy = pose.ScaleY;
            spot.Degrees = pose.Degrees;
            BadgerArtSize(spot.Frame, spot.Unit, out float w, out float h);
            // Stay left of ColR on every beat (SE wide crouch/leap/shrug frames).
            // Squash widens the sprite; keep that width inside the column too.
            float wide = w * (spot.Sx > 1f ? spot.Sx : 1f);
            float rightLimit = L.ColR.x - 2f;
            foot.x = BadgerFighterPlace.ClampFootX(foot.x, wide, rightLimit);

            spot.Foot = foot;
            spot.Bird = bird;
            spot.Icon = icon;
            spot.BirdBox = new Rect(bird.x - icon * 0.5f, bird.y - icon * 0.5f, icon, icon);
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
                if (underOk && ua > 0.02f)
                    DrawBadgerArt(spot.Under, spot.Foot, spot.Unit, spot.Alpha * ua, 0f, spot.Sx, spot.Sy);
                if (frameOk && fa > 0.02f)
                    drawn = DrawBadgerArt(spot.Frame, spot.Foot, spot.Unit, spot.Alpha * fa, 0f, spot.Sx, spot.Sy);
            }
            finally
            {
                GUI.matrix = keep;
            }
            if (drawn.width > 1f)
            {
                BadgerSlam.EyeUv(spot.Frame, out float eu, out float ev);
                _bgDuelEye = new Vector2(
                    drawn.x + drawn.width * eu,
                    drawn.y + drawn.height * ev);
            }

            if (spot.BirdInFront) DrawBadgerBird(spot, clock);
            DrawBadgerRoundHit(spot);
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
                if (_bgPowerGuideOn && p == _bgGuidePower && BadgerPowerGuideStage() == 1) DrawBadgerGuideGlow(r);
                bool picture = BadgerFight.IsBlock(p);
                bool fire = BadgerButton(r, picture ? "" : BadgerPowerName(p), sub, k < 2, used || !inPick || guideBlocks, lit,
                    live && inPick && !used && !guideBlocks, ink, BadgerShakeX(BadgerPowerKey + (int)p, s));
                if (picture && GuiPaint())
                {
                    float side = r.height * 0.58f;
                    DrawBadgerPowerIcon(new Rect(r.center.x - side * 0.5f + BadgerShakeX(BadgerPowerKey + (int)p, s), r.y + r.height * 0.05f, side, side), p, used || !inPick ? 0.55f : 1f);
                }
                if (fire) ClickBadgerPower(p);
            }
        }

        // The coach fight's lock shows until move 3's verdict has played, so the labels never flip back and forth.
        bool BadgerPowerLockedShown()
        {
            if (_bgFight == null) return false;
            if (_bgFight.PowersLocked) return true;
            return _bgFight.CoachFight && _bgFight.RoundsResolved == BadgerTutorialScript.Moves
                && (_bgStage == BadgerStage.Reveal || _bgStage == BadgerStage.DontCare || _bgStage == BadgerStage.Verdict);
        }

        void DrawBadgerCaption(BadgerRects L)
        {
            if (!GuiPaint() || string.IsNullOrEmpty(_bgLine)) return;
            float s = L.S;
            var gold = GoldCaption(_bgLine, Screen.width, Screen.height);
            float w = gold.PlateW;
            float h = gold.PlateH;
            var band = L.Caption;
            if (w > band.width) w = band.width;
            if (h > band.height) h = band.height;
            var seat = new Rect(band.center.x - w * 0.5f, band.center.y - h * 0.5f, w, h);
            CapSpot("badger.caption"); PaintCoachCaption(_bgLine, seat, s, 12, Mathf.RoundToInt(32f * s), default, false, StandardCaptionSlack);
        }

    }
}
