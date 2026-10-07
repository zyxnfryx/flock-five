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
        int _bgLessonStep;
        Rect _bgLessonAim;
        Rect _bgLessonCap;
        static GUIStyle _bgStyle;
        static GUIStyle _bgWrap;
#if UNITY_EDITOR
        bool _bgSwitchWas;
        bool _bgSwitchHeld;
#endif

        static readonly Color BadgerWax = new Color(1f, 0.86f, 0.40f, 1f);
        static readonly Color BadgerHide = new Color(0.52f, 0.46f, 0.43f, 1f);
        static readonly Color BadgerFoilRim = new Color(0.15f, 0.48f, 0.92f, 1f);
        static readonly Color BadgerInverseRim = new Color(0.82f, 0.22f, 0.68f, 1f);
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
            if (_bgFight != null && _bgFight.CoachFight
                && _bgFight.RoundsResolved == BadgerTutorialScript.Moves && !_bgFight.PowersLocked)
                _bgLine = BadgerCopy.PowersOpen;
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
                    int ix = _bgFight.BossPick();
                    if (ix < 0)
                    {
                        BeginBadgerOutro();
                        break;
                    }
                    _bgBossIx = ix;
                    _bgBossAt = Time.unscaledTime;
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
                    Sfx.CardBump();
                    break;
                case BadgerStage.Reveal:
                    if (_bgT < BadgerSlide + BadgerRevealHold) break;
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
                float age = Time.unscaledTime - _bgBossAt;
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
            if (_bgArmed == p)
            {
                _bgArmed = BadgerPower.None;
                _bgLine = BadgerCopy.YourPick;
                return;
            }
            if (!_bgFight.PowerReady(p) || _bgFight.PowersLocked) return;
            if (!_bgFight.CanAfford(p))
            {
                BadgerShakeNow(BadgerPowerKey + (int)p);
                _bgLine = BadgerCopy.CoinsShort;
                return;
            }
            _bgArmed = p;
            Sfx.CardTap();
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
            _bgPlayerIx = i;
            _bgPlayerAt = Time.unscaledTime;
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
                && _bgStage == BadgerStage.YourPick && _bgFight.GuidedTile >= 0;
        }

        // Guided comb from the same hex fit the page paints. CoachPlace runs before
        // OnGUI, so the glove must not wait on the tile loop to store this rect.
        void RefreshBadgerGuideAim()
        {
            if (!BadgerGuideLive()) return;
            var L = BadgerLayout();
            _bgGuideAim = BadgerHex.Fit(L.PlayerGrid, 4, 4).Draw(_bgFight.GuidedTile);
        }

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
            _bgLessonStep++;
            ResetBadgerGlove();
            if (_bgLessonStep < BadgerCopy.LessonCount)
            {
                RefreshBadgerLessonAim();
                return;
            }
            BadgerCopy.MarkCoach();
            EndBadgerLesson();
            BeginBadgerFight();
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
                // Guided pick: same shared glove, aimed at the scripted tile. The tile's own
                // HitPad takes the tap; NoteGloveTap fades the hand only on that tile.
                RefreshBadgerGuideAim();
                var tile = _bgGuideAim;
                if (tile.width < 2f || tile.height < 2f) return;
                _coachFade = Mathf.Min(1f, _coachFade + dt / 0.30f);
                CoachGloveAt(GloveTarget(tile), dt, s, float.NaN, false, float.NaN, tile);
                return;
            }
            if (!_bgLessonLive || _bgStage != BadgerStage.Lesson) return;
            var aim = _bgLessonAim;
            if (aim.width < 2f || aim.height < 2f) return;
            _coachFade = Mathf.Min(1f, _coachFade + dt / 0.30f);
            // Pose the shared glove. Its return is the demo poke beat, not a player tap.
            CoachGloveAt(GloveTarget(aim), dt, s, float.NaN, false, float.NaN, aim);
            // Advance only on a real tap of the demonstrated control (glove fades via NoteGloveTap).
            if (BadgerLessonTapNow(aim))
                AdvanceBadgerLesson();
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
            DrawTutorOverlay(s);
        }

        // Shared static caption box (FitCaptionBox + pinned intro line) for the lesson, the
        // guided picks, and the Don't Care line. The painted plate stays inside L.Caption
        // so it cannot sit on the cards, the fighters, or a second caption.
        Rect DrawBadgerCoachLine(BadgerRects L, string line)
        {
            float s = L.S;
            const float plateX = 18f;
            const float plateY = 12f;
            float maxW = L.Caption.width - plateX * 2f;
            if (maxW > Screen.width * 0.86f) maxW = Screen.width * 0.86f;
            if (maxW < 8f) maxW = 8f;
            int hi = Mathf.Max(18, Mathf.RoundToInt(34f * s));
            var fit = FitCaptionBox(line, maxW, hi, 14);
            float boxW = fit.width;
            if (boxW > maxW) boxW = maxW;
            float roomH = L.Caption.height - plateY * 2f;
            if (roomH < 8f) roomH = 8f;
            float boxH = fit.height;
            if (boxH > roomH) boxH = roomH;
            float x = L.Caption.center.x - boxW * 0.5f;
            float minX = L.Caption.x + plateX;
            float maxX = L.Caption.xMax - plateX - boxW;
            if (maxX < minX) maxX = minX;
            if (x < minX) x = minX;
            if (x > maxX) x = maxX;
            float y = L.Caption.y + (L.Caption.height - boxH) * 0.5f;
            var seat = new Rect(x, y, boxW, boxH);
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
            string line = BadgerCopy.GuideLine(_bgFight.TutorialMove);
            if (!string.IsNullOrEmpty(line)) DrawBadgerCoachLine(L, line);
            DrawTutorOverlay(L.S);
        }

        // Pulsing gold/cream GlowTex under the guided comb. Wider than DrawBadgerWinGlow
        // and breathed so the caption's "glowing" tile is the tap.
        void DrawBadgerGuideGlow(Rect r)
        {
            if (!GuiPaint() || r.width < 2f || r.height < 2f) return;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6.5f);
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
            float s = Mathf.Max(Screen.height / 720f, 1f);
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
            float capH = room * 0.14f;
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

        static GUIStyle BadgerWrapStyle()
        {
            if (_bgWrap != null) return _bgWrap;
            _bgWrap = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                clipping = TextClipping.Clip
            };
            return _bgWrap;
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
            Color rim = finish == BeeFinish.Holo ? BadgerFoilRim
                : finish == BeeFinish.InverseRainbow ? BadgerInverseRim
                : AlbumWood(tint, true);
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
            var lab = DrawRoundedFace(hit, held, gold && !dim ? PopupTint.Gold : PopupTint.Wood, true);
            var st = BadgerStyle();
            float a = dim ? 0.55f : 1f;
            bool two = !string.IsNullOrEmpty(sub);
            var tr = two ? new Rect(lab.x, lab.y, lab.width, lab.height * 0.58f) : lab;
            st.fontSize = FitFont(st, title, tr.width * 0.92f, tr.height * 0.92f, 10, 72);
            StampLight(tr, title, st, new Color(ink.r, ink.g, ink.b, a));
            if (two)
            {
                var sr = new Rect(lab.x, lab.y + lab.height * 0.52f, lab.width, lab.height * 0.46f);
                st.fontSize = FitFont(st, sub, sr.width * 0.92f, sr.height * 0.92f, 9, 56);
                StampLight(sr, sub, st, new Color(BadgerCream.r, BadgerCream.g, BadgerCream.b, a));
            }
            return fire;
        }

        // Honeycomb stack for one side. Same comb height on both, so 10 is shorter than 18.
        // The count uses the caption font. `pulse` is 1 at rest and bumps when a point lands.
        void DrawBadgerMeter(Rect col, int target, int filled, float unit, float pulse)
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
            string num = filled.ToString();
            var nr = BadgerMeter.Number(col);
            var st = CoachLineStyle();
            bool wrap = st.wordWrap;
            st.wordWrap = false;
            st.fontSize = FitFont(st, num, nr.width * 0.92f, nr.height * 0.90f, CaptionPx(22), CaptionPx(72));
            StampOutlined(nr, num, st, BadgerCream, 1, Mathf.Max(2, st.fontSize / 12));
            st.wordWrap = wrap;
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
            if (Event.current.type == EventType.Layout) TickBadger(Time.unscaledDeltaTime);
            var L = BadgerLayout();
            float s = L.S;
            DrawHomeWash(0.58f);

            // Opening, PostOpen, and the lesson are not skippable via the medal.
            if (_bgStage == BadgerStage.Opening || _bgStage == BadgerStage.PostOpen || _bgStage == BadgerStage.Lesson
                || _bgStage == BadgerStage.DontCare || BadgerGuideLive())
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
            DrawBadgerMeter(L.ColL, BadgerSchedule.PlayerTarget(_bgAppearance), _bgShownPlayer, meterUnit, pulseYou);
            DrawBadgerMeter(L.ColR, BadgerSchedule.BadgerTarget(_bgAppearance), _bgShownBoss, meterUnit, pulseBoss);

            var bossHex = BadgerHex.Fit(L.BossGrid, 4, 4);
            var youHex = BadgerHex.Fit(L.PlayerGrid, 4, 4);
            bool tilesLive = _bgStage == BadgerStage.YourPick;

            for (int i = 0; i < BadgerSchedule.Tiles; i++)
            {
                bool open = _bgFight == null || _bgFight.BossOpen(i);
                DrawBadgerTile(bossHex.Draw(i), open ? BadgerLook.Down : BadgerLook.Spent, 0, BadgerHide, BeeFinish.Normal, 1f, null);
            }

            int tapped = -1;
            // Coach moves 1-3: only the glove's tile takes a tap; every other tile ignores it.
            int guide = _bgFight != null && tilesLive ? _bgFight.GuidedTile : -1;
            Rect guidePaint = default;
            bool guideBloom = false;
            for (int i = 0; i < BadgerSchedule.Tiles; i++)
            {
                var t = _bgLoadout[i];
                bool open = _bgFight == null || _bgFight.PlayerOpen(i);
                var draw = youHex.Draw(i);
                if (i == guide) _bgGuideAim = draw;
                bool held = false;
                bool live = tilesLive && open && (guide < 0 || i == guide);
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
                float u = Mathf.SmoothStep(0f, 1f, (Time.unscaledTime - _bgBossAt) / BadgerSlide);
                bool block = _bgResolved && BadgerFight.IsBlock(_bgRound.Power);
                int honey = _bgResolved ? _bgRound.BossFinal : _bgFight.BossHoney(_bgBossIx);
                var r = LerpRect(bossHex.Draw(_bgBossIx), bossSlot, u);
                if (block) r.x += Mathf.Sin(Time.unscaledTime * 38f) * 5f * s * Mathf.Clamp01(1.2f - _bgT);
                bool win = _bgResolved && _bgRound.BossGained > 0;
                if (win) DrawBadgerWinGlow(r);
                DrawBadgerTile(r, BadgerLook.Face, honey, BadgerHide, BeeFinish.Normal, block ? 0.55f : 1f, null);
                if (win && _bgStage == BadgerStage.Verdict)
                    DrawBadgerGain(r, _bgRound.BossGained, _bgT);
                if (block) DrawBadgerSplat(r);
            }
            if (_bgFight != null && _bgPlayerIx >= 0)
            {
                float u = Mathf.SmoothStep(0f, 1f, (Time.unscaledTime - _bgPlayerAt) / BadgerSlide);
                var t = _bgLoadout[_bgPlayerIx];
                int honey = _bgResolved ? _bgRound.PlayerFinal : _bgFight.PlayerHoney(_bgPlayerIx);
                var r = LerpRect(youHex.Draw(_bgPlayerIx), youSlot, u);
                if (_bgResolved && _bgRound.PlayerGained > 0) DrawBadgerWinGlow(r);
                DrawBadgerTile(r, BadgerLook.Face, honey, BadgerTint(t), t.Finish, 1f, null);
                if (_bgResolved && _bgStage == BadgerStage.Verdict && _bgRound.PlayerGained > 0)
                    DrawBadgerGain(r, _bgRound.PlayerGained, _bgT);
                if (_bgArmed != BadgerPower.None && GuiPaint())
                {
                    var badge = new Rect(r.center.x - r.width * 0.45f, r.y - r.height * 0.10f, r.width * 0.9f, r.height * 0.26f);
                    DrawSolidRound(badge, badge.height * 0.5f, 0, 0.95f);
                    var st = BadgerStyle();
                    string name = BadgerPowerName(_bgArmed);
                    st.fontSize = FitFont(st, name, badge.width * 0.86f, badge.height * 0.80f, 8, 40);
                    StampLight(badge, name, st, BadgerGold);
                }
            }

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
                if (!string.IsNullOrEmpty(spot.Under) && spot.UnderAlpha > 0.02f && spot.Under != spot.Frame)
                    DrawBadgerArt(spot.Under, spot.Foot, spot.Unit, spot.Alpha * spot.UnderAlpha, 0f, spot.Sx, spot.Sy);
                drawn = DrawBadgerArt(spot.Frame, spot.Foot, spot.Unit, spot.Alpha * spot.FrameAlpha, 0f, spot.Sx, spot.Sy);
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
                age = Time.unscaledTime - _bgBossAt;
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
                float bw = Mathf.Min(L.Power.width * 0.72f, 420f * s);
                var btn = new Rect(L.Power.center.x - bw * 0.5f, L.Power.y, bw, L.Power.height);
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
                // Coach fight: visibly locked (dim, no hit) until move 3 has resolved.
                bool locked = _bgFight != null && _bgFight.PowersLocked;
                bool inPick = _bgStage == BadgerStage.YourPick && !locked;
                bool lit = _bgArmed == p && (inPick || _bgStage == BadgerStage.Reveal || _bgStage == BadgerStage.Verdict);
                string sub = used ? "USED" : locked ? BadgerCopy.Locked : BadgerPowerPriceSub(p);
                Color ink = p == BadgerPower.HotSauce ? new Color(1f, 0.55f, 0.35f)
                    : p == BadgerPower.FreezeSpray ? new Color(0.45f, 0.85f, 1f)
                    : Color.white;
                bool fire = BadgerButton(r, BadgerPowerName(p), sub, k < 2, used || !inPick, lit,
                    live && inPick && !used, ink, BadgerShakeX(BadgerPowerKey + (int)p, s));
                if (fire) ClickBadgerPower(p);
            }
        }

        void DrawBadgerCaption(BadgerRects L)
        {
            if (!GuiPaint() || string.IsNullOrEmpty(_bgLine)) return;
            var panel = Inset(L.Caption, 0.03f);
            DrawSolidRound(panel, 18f, 0, 0.92f);
            var st = BadgerWrapStyle();
            var tr = new Rect(panel.x + panel.width * 0.04f, panel.y + panel.height * 0.08f, panel.width * 0.92f, panel.height * 0.84f);
            st.fontSize = FitFontWrapped(st, _bgLine, tr.width, tr.height, 10, 64);
            StampOutlined(tr, _bgLine, st, BadgerCream, 0, 2);
        }

    }
}
