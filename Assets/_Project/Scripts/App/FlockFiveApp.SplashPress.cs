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
            hard = VipOffer.IsOpen || _dailyOpen || _dailyAskOpen || _welcomeOpen || _adoptLive;
            soft = _streakSlide >= 0f || RewardPayBusy();
        }

        // Gates that apply to every home button. The caller adds the ones that are
        // its own (adopt queue, the bird, a popup that would refuse the open).
        SplashPress.Gates HomePressGates(int rail)
        {
            HomeModals(out bool hard, out bool soft);
            return new SplashPress.Gates
            {
                Paused = GamePause.Paused,
                ResumeSwallow = PlayClock.Now < _resumeInputUntil,
                ShowHolds = BadgerShowHoldsTaps(),
                CoachAte = CoachAteThisFrame(),
                LessonGuard = LessonGuardUp(),
                HardModal = hard,
                SoftModal = soft,
                RailLesson = !HomeTapAllowed(rail),
            };
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

        SplashPress.View StepHomePress(SplashPress.Id id, bool allow)
        {
            ReadHomePointers(_homeSamples, out bool live);
            var e = Event.current;
            bool canBegin = e != null && e.button == 0 && e.type == EventType.MouseDown;
            var view = _homePress[(int)id].Apply(
                SplashPress.PressHit(id), !allow, _homeSamples, canBegin, live, Time.frameCount);
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
            bool touch = into.Count > 0;
            var mouse = Mouse.current;
            devices = ts != null || mouse != null;
            if (touch) return;
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
            if (phase == UnityEngine.InputSystem.TouchPhase.None) return;
            var p = t.position.ReadValue();
            var pos = new Vector2(p.x, screenH - p.y);
            SplashPress.Phase kind;
            if (phase == UnityEngine.InputSystem.TouchPhase.Canceled) kind = SplashPress.Phase.Canceled;
            else if (phase == UnityEngine.InputSystem.TouchPhase.Ended && t.press.wasPressedThisFrame) kind = SplashPress.Phase.Tap;
            else if (phase == UnityEngine.InputSystem.TouchPhase.Ended) kind = SplashPress.Phase.Ended;
            else if (phase == UnityEngine.InputSystem.TouchPhase.Began) kind = SplashPress.Phase.Began;
            else if (t.press.isPressed) kind = SplashPress.Phase.Held;
            else return;
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
