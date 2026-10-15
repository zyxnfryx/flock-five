using UnityEngine;

namespace FlockFive
{
    // b87: the ONE device-scaling helper. Every phone (9:16 or taller) maps to
    // exactly the old numbers. Wider windows (iPad 3:4, 4:5 foldables) widen the
    // play camera up to MaxPlayAspect so the garden fills the screen, and the UI
    // scale eases down so HUD, buttons, captions and popups keep phone proportions.
    public static class DeviceFit
    {
        public const float PhoneAspect = 9f / 16f;
        public const float MaxPlayAspect = 3f / 4f;
        // UI scale at full tablet width (3:4) relative to the phone height scale.
        public const float TabletUi = 0.80f;
        // Hard cap on any one control as a fraction of the screen width.
        public const float MaxControlFrac = 0.42f;

        public static float WindowAspect(float w, float h) => h < 1f ? PhoneAspect : w / h;
        // Batch tests run in an arbitrary editor window; they see a phone unless they set this.
        public static float? TestWindow;
        public static float WindowAspect()
        {
            if (TestWindow.HasValue) return TestWindow.Value;
            if (Application.isBatchMode) return PhoneAspect;
            return WindowAspect(Screen.width, Screen.height);
        }

        // Play camera aspect: phones stay 9:16 (tall phones letterbox as before).
        public static float PlayAspect(float window)
        {
            if (window <= PhoneAspect + 0.03f) return PhoneAspect;
            return Mathf.Min(window, MaxPlayAspect);
        }
        public static float PlayAspect() => PlayAspect(WindowAspect());

        // 0 on phones, 1 at 3:4 and wider.
        public static float Wide(float window) => Mathf.Clamp01((PlayAspect(window) - PhoneAspect) / (MaxPlayAspect - PhoneAspect));
        public static float Wide() => Wide(WindowAspect());
        public static bool Tablet => Wide() > 0.01f;

        // UI scale: the old Max(h / 720, 1) on phones, eased down on wide screens.
        public static float Ui(float w, float h)
        {
            float s = Mathf.Max(h / 720f, 1f);
            float win = TestWindow ?? (Application.isBatchMode ? PhoneAspect : WindowAspect(w, h));
            return s * Mathf.Lerp(1f, TabletUi, Wide(win));
        }
        public static float Ui() => Ui(Screen.width, Screen.height);
        // Same, for callers that only carry the height (uses the live width).
        public static float UiH(float h) => Ui(Screen.height > 1 ? Screen.width * (h / Screen.height) : h * PhoneAspect, h);

        // Size of a control in px, capped to a share of the screen width.
        // b103 (Brandon): one shared tablet cap for the big round controls (LEVEL disc, PAY TABLE tab, DEAL,
        // back medal). Phones are exactly 1.0. At full tablet width controls are this share of their
        // phone-proportioned size; it eases in with Wide() like Ui() does.
        public const float TabletControlK = 0.75f;
        public static float ControlK(float window) => Mathf.Lerp(1f, TabletControlK, Wide(window));
        public static float ControlK() => ControlK(WindowAspect());

        public static float Cap(float px, float frac = MaxControlFrac) => Mathf.Min(px, Screen.width * frac);
    }
}
