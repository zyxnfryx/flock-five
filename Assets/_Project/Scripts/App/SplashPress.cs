using System.Collections.Generic;
using UnityEngine;

namespace FlockFive
{
    // Home buttons (LEVEL, and the rail buttons that sink the same way) share one
    // hit and one gate. The press-in and the launch both read that hit, so a sink
    // that plays is the same gesture that starts the level.
    //
    // HitPad took a fresh control id on every call, and the call was skipped while
    // a gate was closed. The id moved between the press and the release, the sink
    // had already started, and the release no longer matched, so the level did not
    // start and the sink could stay down. IMGUI also folds every finger into one
    // mouse position, so a second finger made the release look outside.
    public static class SplashPress
    {
        public enum Id { Level = 0, Poker = 1, Hive = 2, Daily = 3, Vip = 4 }
        public const int Count = 5;

        // Mouse stands in when no touch is down. Real touch ids are never negative.
        public const int MouseId = -1;
        public const int NoTouch = int.MinValue;

        public enum Phase { Held, Began, Ended, Canceled, Tap }

        public struct Sample
        {
            public int TouchId;
            public Vector2 Pos;
            public Phase Phase;
        }

        public struct View
        {
            public bool Pressed;
            public bool Launch;
            // This call began or finished the tracked finger. The app eats that event.
            public bool Handled;
        }

        // Every reason a home button must ignore the finger. One value for the sink
        // and the launch: if this is true the sink does not start, and a sink that
        // already started springs back without launching.
        public struct Gates
        {
            public bool Paused;
            public bool ResumeSwallow;
            public bool ShowHolds;
            public bool CoachAte;
            public bool LessonGuard;
            public bool StepGate;
            public bool HardModal;
            public bool SoftModal;
            // Daily's own lesson still opens the card while a coin payout is ticking.
            public bool DailySoftOk;
            public bool AdoptQueue;
            public bool RailLesson;
            public bool HomeTaken;
            // GatePopup would refuse the open. The sink must not play either.
            public bool PopupGate;
            public bool Hidden;
            public bool WelcomeHold;
            public bool Inactive;
            // A consent or ATT card that is actually on screen and covers this control.
            // A flow that is only waiting, or a card that misses the control, stays false.
            public bool Consent;
        }

        public static bool Blocks(Gates g)
        {
            if (g.Paused || g.ResumeSwallow || g.ShowHolds || g.CoachAte || g.LessonGuard || g.StepGate)
                return true;
            if (g.Hidden || g.WelcomeHold || g.Inactive || g.PopupGate)
                return true;
            if (g.HardModal || g.Consent) return true;
            if (g.SoftModal && !g.DailySoftOk) return true;
            if (g.AdoptQueue || g.RailLesson || g.HomeTaken) return true;
            return false;
        }

        // MouseDown is the editor click. A touch that began this frame is the device
        // path: an IMGUI control that already Used the mouse event (the old consent
        // buttons did this) must not stop the finger from arming.
        public static bool CanBegin(EventType type, int button, IList<Sample> samples)
        {
            if (button == 0 && type == EventType.MouseDown) return true;
            if (samples == null) return false;
            for (int i = 0; i < samples.Count; i++)
            {
                var phase = samples[i].Phase;
                if (phase == Phase.Began || phase == Phase.Tap) return true;
            }
            return false;
        }

        // One object per button. Press and launch both return it.
        public sealed class Hit
        {
            public Rect Rect;
            public Rect Extra;
            public bool Disc;

            public bool Contains(Vector2 p)
            {
                if (Rect.width < 1f || Rect.height < 1f) return false;
                if (!Disc) return Rect.Contains(p);
                return DiscContains(Rect, Extra, p);
            }
        }

        static readonly Hit[] Hits = new Hit[Count];

        static SplashPress()
        {
            for (int i = 0; i < Count; i++)
                Hits[i] = new Hit();
        }

        public static Hit PressHit(Id id) => Hits[(int)id];
        public static Hit LaunchHit(Id id) => Hits[(int)id];

        public static void Layout(Id id, Rect rect)
        {
            var hit = Hits[(int)id];
            hit.Rect = rect;
            hit.Extra = default;
            hit.Disc = false;
        }

        public static void LayoutDisc(Id id, Rect medal, Rect ribbon)
        {
            var hit = Hits[(int)id];
            hit.Rect = medal;
            hit.Extra = ribbon;
            hit.Disc = true;
        }

        // Places the LEVEL square and returns the shared hit (press and launch).
        public static Hit LayoutLevel(float w, float h, Rect safe)
        {
            Layout(Id.Level, LevelRect(w, h, safe));
            return PressHit(Id.Level);
        }

        // The wood LEVEL flower. Same square the splash has always drawn and hit:
        // safe-area floor, then the smaller of 94% of the width and half the height.
        public static Rect LevelRect(float w, float h, Rect safe)
        {
            float s = h / 720f;
            if (s < 1f) s = 1f;
            float clear = safe.yMin + 2f;
            if (clear < 4f) clear = 4f;
            float old = safe.yMin + 8f;
            if (old < 14f) old = 14f;
            float pad = old - 28f * s;
            if (pad < clear) pad = clear;
            float size = w * 0.94f;
            float half = h * 0.50f;
            if (half < size) size = half;
            return new Rect((w - size) * 0.5f, h - pad - size, size, size);
        }

        // Medallion disc plus the ribbon under it. Square corners outside the rim are cold.
        public static bool DiscContains(Rect medal, Rect ribbon, Vector2 m)
        {
            float dx = m.x - medal.center.x;
            float dy = m.y - medal.center.y;
            float rad = medal.width * 0.5f;
            if (dx * dx + dy * dy <= rad * rad) return true;
            return ribbon.width > 1f && ribbon.height > 1f && ribbon.Contains(m);
        }

        static int _claimFrame = int.MinValue;
        static int _claimN;
        static readonly int[] _claim = new int[12];

        // Tests start from an empty board. A frame change clears it too.
        public static void ResetBoard()
        {
            _claimFrame = int.MinValue;
            _claimN = 0;
        }

        static void NoteFrame(int frame)
        {
            if (_claimFrame == frame) return;
            _claimFrame = frame;
            _claimN = 0;
        }

        static bool Claimed(int id)
        {
            for (int i = 0; i < _claimN; i++)
                if (_claim[i] == id) return true;
            return false;
        }

        static void Claim(int id)
        {
            if (Claimed(id) || _claimN >= _claim.Length) return;
            _claim[_claimN++] = id;
        }

        // One finger per button. A second finger cannot start another sink or a second launch.
        public sealed class Gesture
        {
            int _touch = NoTouch;
            bool _pressed;
            int _rejectFrame = int.MinValue;
            int _rejectN;
            readonly int[] _reject = new int[8];

            public bool Pressed => _pressed;
            public int TouchId => _touch;

            public View Apply(Hit hit, bool blocked, IList<Sample> samples, bool canBegin, bool pointerLive, int frame)
            {
                NoteFrame(frame);
                var view = default(View);
                if (hit == null)
                {
                    Clear();
                    return view;
                }
                if (blocked)
                {
                    if (canBegin) RejectInside(hit, samples, frame);
                    bool owned = _touch != NoTouch;
                    if (owned) Reject(frame, _touch);
                    Clear();
                    view.Handled = owned;
                    return view;
                }
                if (_touch != NoTouch)
                {
                    int idx = Find(samples, _touch);
                    if (idx < 0)
                    {
                        if (!pointerLive)
                        {
                            view.Pressed = _pressed;
                            return view;
                        }
                        Clear();
                        view.Handled = true;
                        return view;
                    }
                    var s = samples[idx];
                    bool inside = hit.Contains(s.Pos);
                    if (s.Phase == Phase.Ended || s.Phase == Phase.Canceled || s.Phase == Phase.Tap)
                    {
                        Claim(s.TouchId);
                        Clear();
                        view.Handled = true;
                        view.Launch = s.Phase != Phase.Canceled && inside;
                        return view;
                    }
                    _pressed = inside;
                    Claim(_touch);
                    view.Pressed = _pressed;
                    return view;
                }
                if (!canBegin || samples == null) return view;
                for (int i = 0; i < samples.Count; i++)
                {
                    var s = samples[i];
                    if (s.Phase != Phase.Began && s.Phase != Phase.Tap) continue;
                    if (Claimed(s.TouchId) || Rejected(frame, s.TouchId)) continue;
                    if (!hit.Contains(s.Pos)) continue;
                    Claim(s.TouchId);
                    if (s.Phase == Phase.Tap)
                    {
                        view.Launch = true;
                        view.Handled = true;
                        return view;
                    }
                    _touch = s.TouchId;
                    _pressed = true;
                    view.Pressed = true;
                    view.Handled = true;
                    return view;
                }
                return view;
            }

            void RejectInside(Hit hit, IList<Sample> samples, int frame)
            {
                if (samples == null) return;
                for (int i = 0; i < samples.Count; i++)
                {
                    var s = samples[i];
                    if (s.Phase != Phase.Began && s.Phase != Phase.Tap) continue;
                    if (hit.Contains(s.Pos)) Reject(frame, s.TouchId);
                }
            }

            void Reject(int frame, int id)
            {
                if (_rejectFrame != frame)
                {
                    _rejectFrame = frame;
                    _rejectN = 0;
                }
                for (int i = 0; i < _rejectN; i++)
                    if (_reject[i] == id) return;
                if (_rejectN >= _reject.Length) return;
                _reject[_rejectN++] = id;
            }

            bool Rejected(int frame, int id)
            {
                if (_rejectFrame != frame) return false;
                for (int i = 0; i < _rejectN; i++)
                    if (_reject[i] == id) return true;
                return false;
            }

            static int Find(IList<Sample> samples, int id)
            {
                if (samples == null) return -1;
                for (int i = 0; i < samples.Count; i++)
                    if (samples[i].TouchId == id) return i;
                return -1;
            }

            void Clear()
            {
                _touch = NoTouch;
                _pressed = false;
            }
        }
    }
}
