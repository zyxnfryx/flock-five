using UnityEngine;
using UnityEngine.InputSystem;

namespace FlockFive
{
    public enum TiltSource { None, Attitude, Accel, Drag }

    // Tilt for the live inspect card. Ladder: AttitudeSensor (fused gyro) -> gravity/accelerometer -> finger drag.
    // The project uses the new Input System only (activeInputHandler 1), so legacy Input.gyro would throw; sensors come from UnityEngine.InputSystem.
    // Value is -1..1 on each axis (+x = card's right edge toward the viewer, +y = top edge toward the viewer).
    public static class CardTilt
    {
        public const float MaxDeg = 20f;          // sensor clamp
        const float LowPass = 0.12f;              // seconds
        const float DragFullCards = 0.35f;        // finger travel (in card widths) for full tilt
        const float SpringHz = 10f;               // critically damped return after release
        const float SwayAfter = 1.5f;             // idle seconds before the slow sway
        const float TapSlop = 14f;                // px of travel that turns a tap into a drag

        // Axis signs differ between iOS and Android sensor frames; flip here after the device pass.
        public static float SignAttX = 1f, SignAttY = -1f, SignAccX = -1f, SignAccY = 1f;

        public static Vector2 Value;
        public static TiltSource Source { get; private set; }
        public static bool Open { get; private set; }
        // True from a drag that travelled past the slop until the press ends: the card tap must not flip.
        public static bool DragAte { get; private set; }

        static Vector2 _sensor, _sensorRaw, _drag, _dragVel;
        static Quaternion _att0 = Quaternion.identity;
        static Vector3 _g0;
        static bool _haveRef;
        static float _idle, _swayT, _swayAmp, _travel;
        static bool _pressed;
        static Vector2 _lastPos;
        static AttitudeSensor _att;
        static GravitySensor _grav;
        static Accelerometer _acc;

        // Inspect opened: turn the sensors on (they cost battery, so only while the card is up).
        public static void Begin()
        {
            if (Open) return;
            Open = true;
            Value = _sensor = _sensorRaw = _drag = _dragVel = Vector2.zero;
            _haveRef = false;
            _idle = 0f; _swayT = 0f; _swayAmp = 0f; _travel = 0f; _pressed = false; DragAte = false;
            Source = TiltSource.Drag;
            if (CardQuality.Tier == CardTier.Static) return;
            try
            {
                _att = AttitudeSensor.current;
                _grav = GravitySensor.current;
                _acc = Accelerometer.current;
                if (_att != null) { InputSystem.EnableDevice(_att); Source = TiltSource.Attitude; }
                else if (_grav != null) { InputSystem.EnableDevice(_grav); Source = TiltSource.Accel; }
                else if (_acc != null) { InputSystem.EnableDevice(_acc); Source = TiltSource.Accel; }
            }
            catch (System.Exception)
            {
                Source = TiltSource.Drag;
            }
        }

        public static void End()
        {
            if (!Open) return;
            Open = false;
            try
            {
                if (_att != null) InputSystem.DisableDevice(_att);
                if (_grav != null) InputSystem.DisableDevice(_grav);
                if (_acc != null) InputSystem.DisableDevice(_acc);
            }
            catch (System.Exception) { }
            _att = null; _grav = null; _acc = null;
            Value = Vector2.zero;
            Source = TiltSource.None;
        }

        // True while the game is actually in play. A static card (ad up, app unfocused, reduce motion, Static tier) never animates.
        public static bool Playing => Open && !Ads.IsShowing && Application.isFocused && CardQuality.Tier != CardTier.Static;

        // Call once per Repaint tick while the inspect is open. `card` is the on-screen card rect (for drag scale).
        public static void Tick(float dt, Rect card)
        {
            if (!Open) return;
            if (!Playing || dt <= 0f)
            {
                Value = Vector2.Lerp(Value, Vector2.zero, 0.25f);
                return;
            }
            dt = Mathf.Min(dt, 0.1f);
            ReadSensor(dt);
            ReadDrag(dt, card);
            bool touched = _pressed || _drag.sqrMagnitude > 0.0004f;
            bool sensorMoving = Source != TiltSource.Drag && (_sensorRaw - _sensor).sqrMagnitude > 1e-6f;
            _idle = touched ? 0f : _idle + dt;
            // Slow Lissajous sway only when nothing else is steering (no sensor reading change, no finger), after a short rest.
            bool sway = _idle > SwayAfter && !sensorMoving && Source == TiltSource.Drag;
            _swayAmp = Mathf.MoveTowards(_swayAmp, sway ? 0.38f : 0f, dt * 0.5f);
            _swayT += dt;
            var swayV = new Vector2(Mathf.Sin(_swayT * 0.9f), Mathf.Sin(_swayT * 0.63f + 1.1f)) * _swayAmp;
            var v = _sensor + _drag + swayV;
            Value = new Vector2(Mathf.Clamp(v.x, -1f, 1f), Mathf.Clamp(v.y, -1f, 1f));
        }

        static void ReadSensor(float dt)
        {
            if (Source == TiltSource.Drag) return;
            Vector2 raw = _sensorRaw;
            try
            {
                if (Source == TiltSource.Attitude && _att != null && _att.enabled)
                {
                    var q = _att.attitude.ReadValue();
                    if (!_haveRef) { _att0 = q; _haveRef = true; }
                    var rel = Quaternion.Inverse(_att0) * q;
                    var e = rel.eulerAngles;
                    raw = new Vector2(Wrap(e.y) * SignAttX, Wrap(e.x) * SignAttY) / MaxDeg;
                }
                else if (Source == TiltSource.Accel)
                {
                    Vector3 g = _grav != null && _grav.enabled ? _grav.gravity.ReadValue()
                              : _acc != null && _acc.enabled ? _acc.acceleration.ReadValue() : Vector3.zero;
                    if (g.sqrMagnitude > 1e-4f)
                    {
                        g.Normalize();
                        if (!_haveRef) { _g0 = g; _haveRef = true; }
                        float ax = Mathf.Asin(Mathf.Clamp(g.x, -1f, 1f)) - Mathf.Asin(Mathf.Clamp(_g0.x, -1f, 1f));
                        float ay = Mathf.Asin(Mathf.Clamp(g.y, -1f, 1f)) - Mathf.Asin(Mathf.Clamp(_g0.y, -1f, 1f));
                        raw = new Vector2(ax * Mathf.Rad2Deg * SignAccX, ay * Mathf.Rad2Deg * SignAccY) / MaxDeg;
                    }
                }
            }
            catch (System.Exception)
            {
                Source = TiltSource.Drag;   // a sensor that throws is a sensor we do not have
                return;
            }
            raw = new Vector2(Mathf.Clamp(raw.x, -1f, 1f), Mathf.Clamp(raw.y, -1f, 1f));
            _sensorRaw = raw;
            float k = 1f - Mathf.Exp(-dt / LowPass);
            _sensor = Vector2.Lerp(_sensor, raw, k);
        }

        static float Wrap(float deg) => deg > 180f ? deg - 360f : deg;

        // Feed every OnGUI pass (all event types, not just Repaint): a press, drag or release is not a Repaint event.
        // Uses rawType so a control that already consumed the event (the card's own tap handler) does not hide it,
        // and position deltas so a second call on the same event adds nothing.
        public static void Feed(Rect card)
        {
            if (!Open) return;
            var e = Event.current;
            if (e == null) return;
            switch (e.rawType)
            {
                case EventType.MouseDown:
                    if (card.Contains(e.mousePosition)) { _pressed = true; _travel = 0f; DragAte = false; _lastPos = e.mousePosition; _dragVel = Vector2.zero; }
                    break;
                case EventType.MouseDrag:
                    if (_pressed)
                    {
                        var d = e.mousePosition - _lastPos;
                        _lastPos = e.mousePosition;
                        _travel += d.magnitude;
                        if (_travel > TapSlop) DragAte = true;
                        float s = 1f / Mathf.Max(40f, card.width * DragFullCards);
                        // Screen y grows downward; finger dragging up tips the top edge away.
                        _drag += new Vector2(d.x * s, -d.y * s);
                        _drag = new Vector2(Mathf.Clamp(_drag.x, -1f, 1f), Mathf.Clamp(_drag.y, -1f, 1f));
                    }
                    break;
                case EventType.MouseUp:
                    _pressed = false;
                    break;
            }
        }

        static void ReadDrag(float dt, Rect card)
        {
            if (_pressed) return;
            // Released: critically damped spring back to 0.
            float w = SpringHz;
            var acc = -w * w * _drag - 2f * w * _dragVel;
            _dragVel += acc * dt;
            _drag += _dragVel * dt;
            if (_drag.sqrMagnitude < 1e-6f && _dragVel.sqrMagnitude < 1e-6f) { _drag = Vector2.zero; _dragVel = Vector2.zero; }
        }

        // The next press starts fresh (call from the place that consumed DragAte).
        public static void ClearDragAte() => DragAte = false;
    }
}
