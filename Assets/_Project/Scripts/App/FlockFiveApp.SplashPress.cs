using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlockFive
{
    public sealed partial class FlockFiveApp
    {
        readonly SplashPress.Gesture[] _homePress =
        {
            new SplashPress.Gesture(),
            new SplashPress.Gesture(),
            new SplashPress.Gesture(),
            new SplashPress.Gesture(),
            new SplashPress.Gesture(),
        };

        static readonly List<SplashPress.Sample> _homeSamples = new List<SplashPress.Sample>(8);

        // Hard cards and the soft payout / streak sign. Pig and the rails share this.
        void HomeModals(out bool hard, out bool soft)
        {
            hard = VipOffer.IsOpen || _dailyOpen || _dailyAskOpen || _welcomeOpen || _adoptLive || _settingsOpen;
            soft = _streakSlide >= 0f || RewardPayBusy();
        }

        // Gates that apply to every home button. The caller adds the ones that are
        // its own (adopt queue, the bird, a popup that would refuse the open).
        SplashPress.Gates HomePressGates(int rail)
        {
            HomeModals(out bool hard, out bool soft);
            var tapped = RailControl(rail);
            bool own = CoachPress.OwnsTarget(ForcedGloveUp(), ForcedGloveTarget(), tapped, out var glove);
            return new SplashPress.Gates
            {
                Paused = GamePause.Paused,
                ResumeSwallow = GateNow < _resumeInputUntil || StartupInputHeld,
                ShowHolds = BadgerShowHoldsTaps(),
                CoachAte = CoachAteThisFrame(),
                LessonGuard = LessonGuardUp(),
                HardModal = hard,
                SoftModal = soft,
                RailLesson = !HomeTapAllowed(rail) || !glove.AllowPress,
                LessonOwn = own,
            };
        }

        static HomeControl RailControl(int rail)
        {
            if (rail == RailPoker) return HomeControl.Poker;
            if (rail == RailDaily) return HomeControl.Daily;
            if (rail == RailVip) return HomeControl.Vip;
            if (rail == RailHive) return HomeControl.Hive;
            return HomeControl.None;
        }

        // A glove step that owns a home button. Adopt look and the daily claim
        // glove have no button target, so every listed control stays blocked.
        bool ForcedGloveUp()
        {
            if (!_splash || _home != HomeFace.Splash) return false;
            if (_pokerIntroLive || _hiveIntroLive || _welcomeGlove) return true;
            if (_dailyIntroLive && !_dailyAskOpen) return true;
            if (_adoptLive && _adoptStep == AdoptStep.Look) return true;
            return false;
        }

        HomeControl ForcedGloveTarget()
        {
            if (!_splash || _home != HomeFace.Splash) return HomeControl.None;
            if (_pokerIntroLive) return HomeControl.Poker;
            if (_hiveIntroLive) return HomeControl.Hive;
            if (_dailyIntroLive && !_dailyOpen && !_dailyAskOpen) return HomeControl.Daily;
            if (_welcomeGlove && !_welcomeOpen) return HomeControl.Vip;
            return HomeControl.None;
        }

        // A step gate already ate this event, or it will. Any-tap eats the whole
        // screen. A required step eats everything that is not its own control.
        bool StepGateBlocks(Rect button)
        {
            if (!StepTapGate(out var gate)) return false;
            if (gate.Pass) return false;
            if (gate.AnyTap) return true;
            if (button.width < 2f || button.height < 2f) return true;
            return !gate.Target.Contains(button.center);
        }

        SplashPress.View StepHomePress(SplashPress.Id id, bool allow, bool ownTarget = false)
        {
            // b112: the studio card / loading cover owns input; no home button (LEVEL included) sees a press.
            if (StartupInputHeld) allow = false;
            ReadHomePointers(_homeSamples, out bool live);
            var e = Event.current;
            var type = e != null ? e.type : EventType.Ignore;
            int button = e != null ? e.button : -1;
            bool canBegin = SplashPress.CanBegin(type, button, _homeSamples);
            var view = _homePress[(int)id].Apply(
                SplashPress.PressHit(id), !allow, _homeSamples, canBegin, live, Time.frameCount, ownTarget);
            if (view.Handled) EatHomePointer();
            return view;
        }

        static void EatHomePointer()
        {
            var e = Event.current;
            if (e == null) return;
            var t = e.type;
            if (t != EventType.MouseDown && t != EventType.MouseUp && t != EventType.MouseDrag) return;
            if (t == EventType.MouseUp) GUIUtility.hotControl = 0;
            e.Use();
        }

        // One sample per finger, in GUI space (y down). The mouse is only the
        // stand-in when nothing is touching, so a second finger cannot move the
        // first finger's point. Ended and canceled contacts stay in the list for
        // this frame so the release is not mistaken for a vanished finger.
        static void ReadHomePointers(List<SplashPress.Sample> into, out bool devices)
        {
            into.Clear();
            float h = Screen.height;
            var ts = Touchscreen.current;
            if (ts != null)
            {
                var touches = ts.touches;
                for (int i = 0; i < touches.Count; i++)
                    AddTouch(into, touches[i], h);
                if (into.Count == 0 && ts.primaryTouch != null)
                    AddTouch(into, ts.primaryTouch, h);
            }
            bool touchBegan = false;
            for (int i = 0; i < into.Count; i++)
            {
                var ph = into[i].Phase;
                if (ph == SplashPress.Phase.Began || ph == SplashPress.Phase.Tap)
                    touchBegan = true;
            }
            var mouse = Mouse.current;
            devices = ts != null || mouse != null;
            // A finger that already began is the sample. A stale Held contact must
            // not hide a new click: Android keeps a ghost touch while IMGUI delivers
            // the real tap as a mouse event.
            bool mouseFresh = false;
            if (mouse != null)
            {
                var watch = mouse.leftButton;
                mouseFresh = watch.wasPressedThisFrame || watch.wasReleasedThisFrame;
            }
            var evn = Event.current;
            bool eventFresh = evn != null && evn.button == 0 &&
                (evn.type == EventType.MouseDown || evn.type == EventType.MouseUp || evn.type == EventType.MouseDrag);
            if (!SplashPress.MouseStandsIn(touchBegan, into.Count, mouseFresh, eventFresh)) return;
            if (mouse == null)
            {
                SynthesizeMouse(into);
                return;
            }
            var btn = mouse.leftButton;
            if (!btn.isPressed && !btn.wasReleasedThisFrame)
            {
                SynthesizeMouse(into);
                return;
            }
            var m = mouse.position.ReadValue();
            var pos = new Vector2(m.x, h - m.y);
            var ev = Event.current;
            if (ev != null && ev.button == 0 &&
                (ev.type == EventType.MouseDown || ev.type == EventType.MouseUp || ev.type == EventType.MouseDrag))
                pos = ev.mousePosition;
            SplashPress.Phase phase;
            if (btn.wasPressedThisFrame && btn.wasReleasedThisFrame) phase = SplashPress.Phase.Tap;
            else if (btn.wasPressedThisFrame) phase = SplashPress.Phase.Began;
            else if (btn.wasReleasedThisFrame) phase = SplashPress.Phase.Ended;
            else phase = SplashPress.Phase.Held;
            into.Add(SampleAt(SplashPress.MouseId, pos, phase));
        }

        static void AddTouch(List<SplashPress.Sample> into, UnityEngine.InputSystem.Controls.TouchControl t, float screenH)
        {
            if (t == null) return;
            var phase = t.phase.ReadValue();
            if (!SplashPress.ClassifyContact(
                phase == UnityEngine.InputSystem.TouchPhase.Canceled,
                phase == UnityEngine.InputSystem.TouchPhase.Ended,
                phase == UnityEngine.InputSystem.TouchPhase.Began,
                t.press.isPressed,
                t.press.wasPressedThisFrame,
                out var kind))
                return;
            var p = t.position.ReadValue();
            var pos = new Vector2(p.x, screenH - p.y);
            into.Add(SampleAt(t.touchId.ReadValue(), pos, kind));
        }

        static void SynthesizeMouse(List<SplashPress.Sample> into)
        {
            var e = Event.current;
            if (e == null || e.button != 0) return;
            if (e.type == EventType.MouseDown)
                into.Add(SampleAt(SplashPress.MouseId, e.mousePosition, SplashPress.Phase.Began));
            else if (e.type == EventType.MouseUp)
                into.Add(SampleAt(SplashPress.MouseId, e.mousePosition, SplashPress.Phase.Ended));
            else if (e.type == EventType.MouseDrag)
                into.Add(SampleAt(SplashPress.MouseId, e.mousePosition, SplashPress.Phase.Held));
        }

        static SplashPress.Sample SampleAt(int id, Vector2 pos, SplashPress.Phase phase)
        {
            return new SplashPress.Sample { TouchId = id, Pos = pos, Phase = phase };
        }
    }
}
