using UnityEngine;

namespace FlockFive
{
    // Settings card geometry. The privacy link and the Do Not Sell toggle share
    // one column: the link is the row above, the switch sits on the right of the
    // row under it. Pure, so editor tests can check they sit next to each other.
    public static class SettingsSheet
    {
        public const string PrivacyLabel = "Privacy Policy";
        public const string DoNotSellLabel = "Do Not Sell or Share My Personal Information";
        public const string PrivacyUrl = "https://zyxnfryx.github.io/zfx-legal/flock-five-privacy.html";
        public const float RowGap = 8f;

        public readonly struct Rows
        {
            public readonly Rect PrivacyLink;
            public readonly Rect Label;
            public readonly Rect Toggle;

            public Rows(Rect privacyLink, Rect label, Rect toggle)
            {
                PrivacyLink = privacyLink;
                Label = label;
                Toggle = toggle;
            }
        }

        public static Rows Place(Rect inner, float scale)
        {
            if (scale < 0.5f) scale = 0.5f;
            if (inner.width < 8f) inner.width = 8f;
            float gap = RowGap * scale;
            // Privacy and Do Not Sell share one row height and one left edge.
            float rowH = Mathf.Max(64f, 56f * scale);
            float linkH = rowH;
            float sw = Mathf.Max(44f, 44f * scale);
            if (sw > rowH) sw = rowH;
            if (sw > inner.width * 0.45f) sw = Mathf.Max(8f, inner.width * 0.45f);
            var privacy = new Rect(inner.x, inner.y, inner.width, linkH);
            float y = privacy.yMax + gap;
            var toggle = new Rect(inner.xMax - sw, y + (rowH - sw) * 0.5f, sw, sw);
            float labelW = toggle.xMin - gap - inner.x;
            if (labelW < 8f) labelW = 8f;
            var label = new Rect(inner.x, y, labelW, rowH);
            return new Rows(privacy, label, toggle);
        }
    }
}
