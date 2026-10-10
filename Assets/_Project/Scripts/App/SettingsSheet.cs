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
        // Inner buffer on each side of the wood, as a fraction of the card width.
        public const float SidePad = 0.07f;

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

        // Card, padded column, title, and close control in screen space.
        // The close control sits in its own corner, fully above the title.
        public readonly struct Frame
        {
            public readonly Rect Card;
            public readonly Rect Content;
            public readonly Rect Title;
            public readonly Rect Close;
            public readonly Rows Body;

            public Frame(Rect card, Rect content, Rect title, Rect close, Rows body)
            {
                Card = card;
                Content = content;
                Title = title;
                Close = close;
                Body = body;
            }
        }

        public static Frame Layout(float screenW, float screenH, Rect safe, float topHud)
        {
            float s = screenH / 720f;
            if (s < 0.5f) s = 0.5f;
            if (s < 1f) s = 1f;
            float cardW = CardWidth(screenW, safe, s);
            float chrome = Mathf.Max(10f, 12f * s);
            float side = Mathf.Max(cardW * SidePad, 18f * s);
            float vPad = Mathf.Max(cardW * 0.055f, 16f * s);
            float contentW = cardW - (chrome + side) * 2f;
            if (contentW < 8f) contentW = 8f;
            float gap = Mathf.Max(RowGap * s, cardW * 0.028f);
            float titleH = Mathf.Max(44f, 46f * s);
            var probe = Place(new Rect(0f, 0f, contentW, 8f), s, gap);
            float bodyH = probe.Label.yMax - probe.PrivacyLink.yMin;
            float xSz = Mathf.Max(44f, 42f * s);
            float xMargin = Mathf.Max(cardW * 0.035f, 12f * s);
            float titleGap = Mathf.Max(10f * s, 8f);
            float cardH = chrome + vPad + xSz + titleGap + titleH + gap + bodyH + vPad + chrome;
            var card = PlaceCard(screenW, screenH, safe, topHud, s, cardW, cardH, 0.34f);
            var close = new Rect(card.xMax - xMargin - xSz, card.y + xMargin, xSz, xSz);
            float contentX = card.x + chrome + side;
            float contentY = close.yMax + titleGap;
            var content = new Rect(contentX, contentY, contentW, titleH + gap + bodyH);
            var title = new Rect(content.x, content.y, content.width, titleH);
            var body = Place(new Rect(content.x, title.yMax + gap, content.width, bodyH), s, gap);
            return new Frame(card, content, title, close, body);
        }

        public static Rows Place(Rect inner, float scale)
        {
            return Place(inner, scale, -1f);
        }

        public static Rows Place(Rect inner, float scale, float gapOverride)
        {
            if (scale < 0.5f) scale = 0.5f;
            if (inner.width < 8f) inner.width = 8f;
            float gap = gapOverride >= 0f ? gapOverride : RowGap * scale;
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

        // Same width rule as the other standard cards (88% of the glass, cap 600s).
        static float CardWidth(float screenW, Rect safe, float s)
        {
            float insetL = Mathf.Max(12f * s, safe.xMin + 8f);
            float insetR = Mathf.Max(12f * s, screenW - safe.xMax + 8f);
            float span = screenW - insetL - insetR;
            if (span < 8f) span = 8f;
            float cardW = Mathf.Min(span, Mathf.Min(screenW * 0.88f, 600f * s));
            float floor = 240f;
            if (floor > span) floor = span;
            if (cardW < floor) cardW = floor;
            if (cardW < 8f) cardW = 8f;
            return cardW;
        }

        static Rect PlaceCard(float screenW, float screenH, Rect safe, float topHud, float s, float w, float h, float bias)
        {
            float insetL = Mathf.Max(12f * s, safe.xMin + 8f);
            float insetR = Mathf.Max(12f * s, screenW - safe.xMax + 8f);
            float top = topHud + 4f * s;
            float bot = screenH - Mathf.Max(8f, safe.yMin + 4f);
            float span = screenW - insetL - insetR;
            if (span < 8f) span = 8f;
            if (w > span) w = span;
            if (w < 8f) w = 8f;
            if (h < 8f) h = 8f;
            float x = insetL + (span - w) * 0.5f;
            float room = bot - top - h;
            if (bias < 0f) bias = 0f;
            if (bias > 1f) bias = 1f;
            float y = top + (room > 0f ? room * bias : 0f);
            if (y + h > bot) y = bot - h;
            if (y < top) y = top;
            return new Rect(x, y, w, h);
        }
    }
}
