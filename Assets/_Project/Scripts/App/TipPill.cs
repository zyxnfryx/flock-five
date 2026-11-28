using UnityEngine;

namespace FlockFive
{
    // One rounded pill for every tutorial and coach message.
    // Size, radius, type, and the bottom band come from the field height, so a
    // short line (Nice!) and a long one share the plate. The four portrait
    // classes are knots on that curve: 104/22 at 780, 114/24 at 874, 120/26 at
    // 915, and 160/34 at 1180 (the 360, 402, 412, and 820 windows). A single
    // proportion of width or height cannot land on all four, so the curve is
    // the rule and no message picks its own numbers.
    //
    // The settings sheet is a menu (Sound, Privacy, Animate). It has no coach
    // sentence, so it does not paint a pill. Restart confirm does.
    public static class TipPill
    {
        public const int MaxLines = 2;
        public const float RefHeight = 114f;
        public const float RefRadius = 18f; // 402 x 874 class
        public const float RefGap = 50f;    // px above the bottom safe inset at that class

        // Floor: pill bottom sits Gap px above the bottom safe inset.
        // Raised: one pill higher. The single alternate when a tip must clear
        // a control (the gift Watch disc). The gift coach uses Floor; that
        // band sits under the disc. Raised is here so every such tip shares
        // one lift instead of a private offset.
        public enum Band
        {
            Floor = 0,
            Raised = 1,
        }

        static readonly float[] FieldH = { 780f, 874f, 915f, 1180f };
        static readonly float[] PillH = { 104f, 114f, 120f, 160f };
        static readonly float[] CapPx = { 22f, 24f, 26f, 34f };

        public static int Height(float fieldH) => Mathf.RoundToInt(Curve(fieldH, FieldH, PillH));

        public static int Cap(float fieldH) => Mathf.RoundToInt(Curve(fieldH, FieldH, CapPx));

        public static float Radius(float fieldH)
        {
            float h = Height(fieldH);
            float rad = RefRadius * h / RefHeight;
            if (rad < 2f) rad = 2f;
            return rad;
        }

        public static float Gap(float fieldH) => RefGap * Height(fieldH) / RefHeight;

        // Same black edge on every tip, including Nice!. Share of the class cap.
        public const float OutlineFrac = 0.16f;

        public static int Outline(int cap)
        {
            int px = Mathf.RoundToInt((cap < 1 ? 1 : cap) * OutlineFrac);
            if (px < 2) px = 2;
            return px;
        }

        public static float TextPad(int cap)
        {
            float p = (cap < 1 ? 1 : cap) * 0.28f;
            if (p < 6f) p = 6f;
            return p;
        }

        // In from each field edge. A few percent of the field, never a full-bleed
        // bar. On an iPad the field is the phone column (PhoneArea), not the window.
        // The corner radius is separate: the type sits in the straight middle of the pill.
        public static float Inset(float fieldW, float fieldH)
        {
            float inset = fieldW * 0.03f;
            if (inset < 10f) inset = 10f;
            float cap = fieldW * 0.12f;
            if (cap > 8f && inset > cap) inset = cap;
            return inset;
        }

        // Gift offer uses the same floor band as every other tip. Raised is the
        // one documented lift for a tip that has to clear a control.
        public static Band BandFor(string spot)
        {
            // Every current tip, including ad.hand and an unnamed spot, shares
            // the floor. Raised stays the one lift (see Band) for a control.
            if (string.IsNullOrEmpty(spot)) return Band.Floor;
            return Band.Floor;
        }

        // safeBottom is the bottom inset in GUI pixels (Screen.safeArea.yMin).
        public static float SafeBottom(float fieldH, Rect safe)
        {
            if (safe.width < 2f || safe.height < 2f) return 0f;
            float bot = safe.yMin;
            if (bot < 0f) bot = 0f;
            if (fieldH > 2f && bot > fieldH) bot = fieldH;
            return bot;
        }

        public static Rect Place(float fieldW, float fieldH, float safeBottom, Band band)
        {
            if (fieldW < 8f) fieldW = 8f;
            if (fieldH < 8f) fieldH = 8f;
            if (safeBottom < 0f) safeBottom = 0f;
            float h = Height(fieldH);
            float inset = Inset(fieldW, fieldH);
            float w = fieldW - inset * 2f;
            if (w < 48f)
            {
                inset = Mathf.Max(0f, (fieldW - 48f) * 0.5f);
                w = fieldW - inset * 2f;
                if (w < 8f) w = fieldW;
            }
            float bottom = fieldH - safeBottom - Gap(fieldH);
            if (band == Band.Raised) bottom -= h;
            float y = bottom - h;
            if (y < 0f) y = 0f;
            if (y + h > fieldH) y = Mathf.Max(0f, fieldH - h);
            float x = (fieldW - w) * 0.5f;
            return Snap(new Rect(x, y, w, h));
        }

        // One integer rect for every tip. Rounding the far edge on its own was
        // shaving a couple of pixels off some plates and not others.
        public static Rect Snap(Rect r)
        {
            float x = Mathf.Round(r.x);
            float y = Mathf.Round(r.y);
            float w = Mathf.Round(r.width);
            float h = Mathf.Round(r.height);
            if (w < 4f) w = 4f;
            if (h < 4f) h = 4f;
            return new Rect(x, y, w, h);
        }

        public static float InnerWidth(Rect pill, int cap)
        {
            float pad = TextPad(cap);
            float w = pill.width - pad * 2f;
            if (w < 48f) w = pill.width > 16f ? pill.width - 16f : Mathf.Max(8f, pill.width);
            return w;
        }

        // Fixed cap. At most two lines. Authored breaks stay when each row fits.
        public static string Shown(GUIStyle style, string text, float innerW, int cap)
        {
            if (string.IsNullOrEmpty(text)) return text ?? "";
            int size = cap < 1 ? 1 : cap;
            var fit = CaptionLayout.FitBanner(style, text, innerW, size, MaxLines);
            return string.IsNullOrEmpty(fit.Shown) ? text : fit.Shown;
        }

        public static int CountLines(string shown)
        {
            if (string.IsNullOrEmpty(shown)) return 0;
            int n = 1;
            for (int i = 0; i < shown.Length; i++)
                if (shown[i] == '\n') n++;
            return n;
        }

        static float Curve(float x, float[] xs, float[] ys)
        {
            int n = xs.Length;
            if (n < 2) return ys.Length > 0 ? ys[0] : 0f;
            if (x <= xs[0])
            {
                float span = xs[1] - xs[0];
                float t = span > 0.01f ? (x - xs[0]) / span : 0f;
                return ys[0] + t * (ys[1] - ys[0]);
            }
            if (x >= xs[n - 1])
            {
                float span = xs[n - 1] - xs[n - 2];
                float t = span > 0.01f ? (x - xs[n - 2]) / span : 1f;
                return ys[n - 2] + t * (ys[n - 1] - ys[n - 2]);
            }
            for (int i = 0; i < n - 1; i++)
            {
                if (x > xs[i + 1]) continue;
                float span = xs[i + 1] - xs[i];
                float t = span > 0.01f ? (x - xs[i]) / span : 0f;
                return ys[i] + t * (ys[i + 1] - ys[i]);
            }
            return ys[n - 1];
        }
    }
}
