using UnityEngine;

namespace FlockFive
{
    // b120: first-time rule captions (crown/bow, feeder turn, sleep). Caption only,
    // no glove. Game time stops while one is up (HoldTutorPause); any tap continues.
    public sealed partial class FlockFiveApp
    {
        RuleTip _ruleTip;
        float _ruleHeld;
        Board _ruleBoardRef;
        BirdColor?[] _ruleLive;
        bool _ruleTurned;

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
                if (!b.AnySleeping && _board.IsSleeping(i)) b.AnySleeping = true;
                if (!b.HasKit)
                    for (int k = 0; k < br.Count; k++)
                        if (br.Birds[k].Kit != BirdKit.Plain) { b.HasKit = true; break; }
            }
            return b;
        }

        // Unpaused coach tick, after the first-garden coach. True while a tip owns the stage.
        bool RuleTipAdvance()
        {
            if (_ruleTip != RuleTip.None) return true;
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
        void RaiseRuleSleepTip()
        {
            if (_board == null) return;
            bool sleeping = false;
            for (int i = 0; i < _board.Branches.Count; i++)
                if (_board.IsSleeping(i)) { sleeping = true; break; }
            bool blocked = _splash || _won || _restarting || SeasonReveal.HoldsPlay;
            if (!RuleTips.SleepReady(sleeping, RuleSeen(RuleTip.Sleep), _ruleTip != RuleTip.None, blocked))
                return;
            ShowRuleTip(RuleTip.Sleep);
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
            _ruleHeld += Mathf.Min(AdPause.UnscaledDelta, 0.1f);
            if (_ruleHeld > RuleTips.HoldMax && !Ads.IsShowing)
            {
                DismissRuleTip();
                return;
            }
            if (Ads.IsShowing) return;
            if (Pressed(out var screen)) RuleTipTap(screen);
        }

        bool RuleTipTap(Vector2 screen)
        {
            if (_ruleTip == RuleTip.None) return false;
            var gui = new Vector2(screen.x, PhoneArea.H - screen.y);
            if (RestartGui(gui)) return false;
            return FinishLessonTap(RuleTips.Step(_ruleTip), false, false, DismissRuleTip);
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
            ReleaseTutorPause();
            NoteLessonDismiss(RuleTips.Step(t));
        }
    }
}
