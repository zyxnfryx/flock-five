using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlockFive
{
    // b120: first-time rule captions (crown/bow, feeder turn, sleep). Caption only,
    // no glove. Game time stops while one is up (HoldTutorPause); any tap continues.
    public sealed partial class FlockFiveApp
    {
        RuleTip _ruleTip;
        float _ruleHeld;
        // The press that closed a rule tip. Held until that pointer is up so it cannot select a bird.
        bool _ruleEatGesture;
        Board _ruleBoardRef;
        BirdColor?[] _ruleLive;
        bool _ruleTurned;
        // WebGL forceNap only. Zzz may draw, but the plate stays down until that
        // pose has been on an open stage. Normal play leaves this false.
        bool _devNapHold;

        static bool RuleSeen(RuleTip t) => PlayerPrefs.GetInt(RuleTips.Key(t), 0) != 0;

        void RuleTrackFeeders()
        {
            if (_board == null || _board.Live == null) return;
            if (!ReferenceEquals(_ruleBoardRef, _board) || _ruleLive == null || _ruleLive.Length != _board.Live.Length)
            {
                _ruleBoardRef = _board;
                _ruleLive = (BirdColor?[])_board.Live.Clone();
                _ruleTurned = false;
                return;
            }
            if (RuleTips.Turned(_ruleLive, _board.Live)) _ruleTurned = true;
            for (int i = 0; i < _ruleLive.Length; i++) _ruleLive[i] = _board.Live[i];
        }

        RuleTips.Board RuleBoardNow()
        {
            var b = new RuleTips.Board { FeederTurned = _ruleTurned };
            for (int i = 0; i < _board.Branches.Count; i++)
            {
                var br = _board.Branches[i];
                if (br == null || br.Broken) continue;
                bool asleep = _board.IsSleeping(i);
                if (asleep)
                {
                    b.AnySleeping = true;
                    if (!b.NapBegun && !_devNapHold && BranchNapBegun(i)) b.NapBegun = true;
                }
                if (!b.HasKit)
                    for (int k = 0; k < br.Count; k++)
                        if (br.Birds[k].Kit != BirdKit.Plain) { b.HasKit = true; break; }
            }
            return b;
        }

        // True once a seated bird on this limb has drawn Zzz for the current nap.
        bool BranchNapBegun(int branch)
        {
            var views = _garden.Branches;
            if (views == null || (uint)branch >= (uint)views.Length) return false;
            var view = views[branch];
            if (view == null) return false;
            var birds = view.Birds;
            for (int i = 0; i < birds.Length; i++)
            {
                var sr = birds[i];
                if (sr == null || !sr.gameObject.activeInHierarchy) continue;
                var idle = sr.GetComponent<BirdIdle>();
                if (idle != null && idle.Sleeping && idle.NapBegun) return true;
            }
            return false;
        }

        // Unpaused coach tick, after the first-garden coach. True while a tip owns the stage.
        bool RuleTipAdvance()
        {
            if (_ruleTip != RuleTip.None) return true;
            if (_holdRuleForLesson && _coach) return false;
            RuleTrackFeeders();
            if (_pestCue != 0 || _collecting || _busy || !PestStageFree()) return false;
            var seen = new RuleTips.Seen
            {
                Kit = RuleSeen(RuleTip.Kit),
                Feeder = RuleSeen(RuleTip.Feeder),
                Sleep = RuleSeen(RuleTip.Sleep),
            };
            if (seen.Kit && seen.Feeder && seen.Sleep) return false;
            var due = RuleTips.Due(seen, RuleBoardNow());
            if (due == RuleTip.None) return false;
            ShowRuleTip(due);
            return true;
        }

        // New feeder colour has landed. The collect may still be waking the next
        // row; the tip pauses here so 402 and 820 are not still in that beat
        // when the capture looks for the line.
        // A branch just became five of a colour no feeder wants. Not gated on the
        // coach being finished or on a pest line (the L5 bee plate was covering it).
        // The board is asleep at the commit. Zzz is drawn in LateUpdate, so the
        // move waits (RaiseSleepTipWhenNapping) instead of showing this on the tap.
        void RaiseRuleSleepTip()
        {
            if (_board == null) return;
            bool sleeping = false;
            bool napBegun = false;
            for (int i = 0; i < _board.Branches.Count; i++)
            {
                if (!_board.IsSleeping(i)) continue;
                sleeping = true;
                if (BranchNapBegun(i)) { napBegun = true; break; }
            }
            bool blocked = _splash || _won || _restarting || SeasonReveal.HoldsPlay;
            if (!RuleTips.SleepReady(sleeping, napBegun, RuleSeen(RuleTip.Sleep), _ruleTip != RuleTip.None, blocked))
                return;
            ShowRuleTip(RuleTip.Sleep);
        }

        // Hop has landed. Sync may have marked the row asleep in this same call,
        // before LateUpdate tucks the birds and draws Zzz. Wait for that frame,
        // then raise. The coach tick uses the same NapBegun gate, so the caption
        // cannot pause the hop on the commit.
        IEnumerator RaiseSleepTipWhenNapping(int branch, int gen)
        {
            float waited = 0f;
            while (gen == _motionGen && !BranchNapBegun(branch) && waited < RuleTips.NapWait)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            if (gen != _motionGen) yield break;
            if (BranchNapBegun(branch)) RaiseRuleSleepTip();
            CheckOver();
        }

        void RaiseRuleFeederTip()
        {
            RuleTrackFeeders();
            bool blocked = _splash || _won || _restarting || _board == null
                || _coach || _pestCue != 0 || _leafIntro || _hiveLevelLive
                || SeasonReveal.HoldsPlay;
            if (!RuleTips.FeederReady(_ruleTurned, RuleSeen(RuleTip.Feeder), _ruleTip != RuleTip.None, blocked))
                return;
            ShowRuleTip(RuleTip.Feeder);
        }

        void ShowRuleTip(RuleTip due)
        {
            _ruleTip = due;
            _ruleHeld = 0f;
            _cueHand = false;
            _cueForce = false;
            _cueFreeze = false;
            _cueGift = false;
            _cueBranch = -1;
            _gloveVis = false;
            _gloveTapRectOn = false;
            CoachHideGlow();
            CueLine(RuleTips.Line(due));
            // Full plate on the first paint. A fade from 0 left the backing clear
            // and the type dark for the whole time a device sat on the first frames.
            _coachFade = 1f;
            HoldTutorPause(true);
        }

        // Runs while GamePause holds the garden: fade in, watchdog, any tap continues.
        void RuleTipPausedTick()
        {
            if (_won || _restarting || _board == null)
            {
                DismissRuleTip();
                return;
            }
            _coachFade = Mathf.Min(1f, _coachFade + AdPause.UnscaledDelta / 0.35f);
            if (TickRuleHold()) return;
            if (Ads.IsShowing) return;
            if (Pressed(out var screen))
                TakeRuleTipPress(screen);
        }

        // Unscaled wall time. AdPause.UnscaledDelta is 0 during an ad, and the old
        // 0.1s clamp made a slow WebGL frame count as a tenth of a second.
        bool TickRuleHold()
        {
            if (_ruleTip == RuleTip.None) return false;
#if DEVELOPMENT_BUILD
            // DEV-ONLY inspect hook. Not a shipping-path proof.
            // ?forcePause=1 holds indefinitely. ?forcePause=N holds N seconds.
            // DevPauseFlip is the devConsole toggle. Release builds omit this.
            if (DevLevelSkip.CaptionHeld(Application.absoluteURL, _ruleHeld)) return false;
#endif
            _ruleHeld = AdvanceRuleHold(_ruleHeld, Time.unscaledDeltaTime);
            if (!RuleHoldDone(_ruleHeld) || Ads.IsShowing) return false;
            DismissRuleTip();
            return true;
        }

        void TakeRuleTipPress(Vector2 screen)
        {
            var drawn = OnScreenGlove();
            var fate = GardenPressFate(screen, PhoneArea.H, HomeHudRect(), RestartHudRect(), drawn, true, false);
            if (fate == PressFate.Home || fate == PressFate.Restart)
            {
                GardenHudAct(screen);
                DismissRuleTip();
                return;
            }
            RuleTipTap(screen);
        }

        Rect HomeHudRect()
        {
            HudShaken(out var restart, out _);
            return CornerAnchor.TopBarHome(restart, PhoneArea.W, PhoneArea.Safe);
        }

        Rect RestartHudRect()
        {
            HudShaken(out var restart, out _);
            return restart;
        }

        bool RuleTipTap(Vector2 screen)
        {
            if (_ruleTip == RuleTip.None) return false;
            // Any tap advances, including Restart and Home. The caller does not
            // eat the event, so those controls still receive it.
            DismissRuleTip();
            return false;
        }

        void DismissRuleTip()
        {
            if (_ruleTip == RuleTip.None) return;
            var t = _ruleTip;
            _ruleTip = RuleTip.None;
            if (t == RuleTip.Feeder) _ruleTurned = false;
            PlayerPrefs.SetInt(RuleTips.Key(t), 1);
            PlayerPrefs.Save();
            _cueLine = null;
            _cueSpoken = null;
            _cueBranch = -1;
            _tutorSeatOn = false;
            _ruleEatGesture = true;
            ReleaseTutorPause();
            NoteLessonDismiss(RuleTips.Step(t));
        }

        // Runs even while the garden is paused, before any control. The feeder tip's
        // first press dismisses and is eaten here, not one frame later on the board.
        void SwallowRuleTipPointer()
        {
            var e = Event.current;
            if (e == null) return;
            var t = e.type;
            if (t != EventType.MouseDown && t != EventType.MouseUp && t != EventType.MouseDrag) return;
            if (_ruleTip != RuleTip.None)
            {
                if (t == EventType.MouseDown && e.button == 0)
                {
                    var screen = new Vector2(e.mousePosition.x, PhoneArea.H - e.mousePosition.y);
                    TakeRuleTipPress(screen);
                }
                if (t == EventType.MouseUp) GUIUtility.hotControl = 0;
                return;
            }
            if (!_ruleEatGesture) return;
            if (t == EventType.MouseUp)
            {
                GUIUtility.hotControl = 0;
                if (!RulePointerHeld()) _ruleEatGesture = false;
            }
            e.Use();
        }

        static bool RulePointerHeld()
        {
            if (Mouse.current != null && Mouse.current.leftButton.isPressed) return true;
            var touch = Touchscreen.current;
            if (touch != null)
            {
                var primary = touch.primaryTouch;
                if (primary != null && primary.press.isPressed) return true;
            }
            return false;
        }
    }
}
