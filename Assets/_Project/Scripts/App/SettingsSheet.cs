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
        public const string AnimateLabel = BackgroundMotion.Label;
        public const string PrivacyUrl = "https://zyxnfryx.github.io/zfx-legal/flock-five-privacy.html";
        public const float RowGap = 8f;
        // Inner buffer on each side of the wood, as a fraction of the card width.
        public const float SidePad = 0.07f;

        public readonly struct Rows
        {
            public readonly Rect PrivacyLink;
            public readonly Rect Label;
            public readonly Rect Toggle;
            public readonly Rect AnimateLabel;
            public readonly Rect AnimateToggle;

            public Rows(Rect privacyLink, Rect label, Rect toggle, Rect animateLabel, Rect animateToggle)
            {
                PrivacyLink = privacyLink;
                Label = label;
                Toggle = toggle;
                AnimateLabel = animateLabel;
                AnimateToggle = animateToggle;
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

            // Slide the whole card. Hits and paint share this, so a moving
            // panel keeps its controls on the wood. Zero returns the same seat.
            public Frame OffsetY(float dy)
            {
                if (dy > -0.01f && dy < 0.01f) return this;
                return new Frame(
                    MoveY(Card, dy),
                    MoveY(Content, dy),
                    MoveY(Title, dy),
                    MoveY(Close, dy),
                    new Rows(
                        MoveY(Body.PrivacyLink, dy),
                        MoveY(Body.Label, dy),
                        MoveY(Body.Toggle, dy),
                        MoveY(Body.AnimateLabel, dy),
                        MoveY(Body.AnimateToggle, dy)));
            }

            static Rect MoveY(Rect r, float dy)
            {
                return new Rect(r.x, r.y + dy, r.width, r.height);
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
            float bodyH = probe.AnimateLabel.yMax - probe.PrivacyLink.yMin;
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
            float y2 = label.yMax + gap;
            var animateToggle = new Rect(inner.xMax - sw, y2 + (rowH - sw) * 0.5f, sw, sw);
            var animateLabel = new Rect(inner.x, y2, labelW, rowH);
            return new Rows(privacy, label, toggle, animateLabel, animateToggle);
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

    public enum SettingsPhase
    {
        Closed = 0,
        Opening = 1,
        Open = 2,
        Closing = 3
    }

    // Settings open / close. Pure, so the editor suite can step the machine
    // without a frame. Curves come from PopupMotion: ease-out cubic on the
    // way in (0.4s), ease-in cubic on the way out (0.3s), Smooth on the dim.
    public static class SettingsMotion
    {
        public const float OpenSeconds = 0.40f;
        public const float CloseSeconds = 0.30f;
        public const float OverlayBlack = 0.60f;
        // Clockwise on the way open. A multiple of the 8-tooth step so the
        // last opening frame matches the unrotated cog. Inside 180–360.
        public const float SpinDegrees = 270f;

        public enum Hit
        {
            None = 0,
            Gear = 1,
            Overlay = 2,
            Close = 3,
            Sheet = 4
        }

        // Where the cog paints. AboveOverlay is after the settings dim.
        public enum GearPass
        {
            Hidden = 0,
            AboveDimmer = 1,
            AboveOverlay = 2
        }

        public static bool BlocksInput(SettingsPhase phase)
        {
            return phase == SettingsPhase.Opening || phase == SettingsPhase.Closing;
        }

        public static GearPass GearPassFor(bool home, SettingsPhase phase)
        {
            if (!home) return GearPass.Hidden;
            if (phase == SettingsPhase.Closed) return GearPass.AboveDimmer;
            return GearPass.AboveOverlay;
        }

        public static SettingsPhase AfterGearTap(SettingsPhase phase)
        {
            if (phase == SettingsPhase.Closed) return SettingsPhase.Opening;
            if (phase == SettingsPhase.Open) return SettingsPhase.Closing;
            return phase;
        }

        public static SettingsPhase AfterDismiss(SettingsPhase phase)
        {
            if (phase == SettingsPhase.Open) return SettingsPhase.Closing;
            return phase;
        }

        // Gear toggles. Overlay and the X dismiss, and only while Open.
        // Opening and Closing ignore every hit.
        public static SettingsPhase AfterHit(SettingsPhase phase, Hit hit)
        {
            if (BlocksInput(phase)) return phase;
            if (hit == Hit.Gear) return AfterGearTap(phase);
            if (phase != SettingsPhase.Open) return phase;
            if (hit == Hit.Overlay || hit == Hit.Close) return AfterDismiss(phase);
            return phase;
        }

        public static SettingsPhase Settled(SettingsPhase phase, float age)
        {
            if (phase == SettingsPhase.Opening && age >= OpenSeconds) return SettingsPhase.Open;
            if (phase == SettingsPhase.Closing && age >= CloseSeconds) return SettingsPhase.Closed;
            return phase;
        }

        public static float Age(float now, float started)
        {
            float age = now - started;
            if (age < 0f) return 0f;
            return age;
        }

        // Travel that parks the card's bottom `pad` pixels above the screen.
        public static float DropTravel(float cardBottom, float pad)
        {
            if (cardBottom < 0f) cardBottom = 0f;
            if (pad < 0f) pad = 0f;
            return cardBottom + pad;
        }

        // GUI y offset. Negative is above the seat. 0 is seated.
        public static float PanelOffset(SettingsPhase phase, float age, float travel)
        {
            if (travel < 0f) travel = 0f;
            if (phase == SettingsPhase.Opening)
            {
                float u = PopupMotion.Beat(age, 0f, OpenSeconds);
                return -travel * (1f - PopupMotion.EaseOutCubic(u));
            }
            if (phase == SettingsPhase.Closing)
            {
                float u = PopupMotion.Beat(age, 0f, CloseSeconds);
                return -travel * PopupMotion.EaseInCubic(u);
            }
            return 0f;
        }

        public static float OverlayAlpha(SettingsPhase phase, float age)
        {
            if (phase == SettingsPhase.Open) return OverlayBlack;
            if (phase == SettingsPhase.Opening)
            {
                float u = PopupMotion.Beat(age, 0f, OpenSeconds);
                return OverlayBlack * PopupMotion.Smooth(u);
            }
            if (phase == SettingsPhase.Closing)
            {
                float u = PopupMotion.Beat(age, 0f, CloseSeconds);
                return OverlayBlack * (1f - PopupMotion.Smooth(u));
            }
            return 0f;
        }

        // Positive degrees are clockwise (RotateAroundPivot, same as the wheel).
        // Open and Closed stay at 0 so the settled icon is not a filtered spin.
        // SpinDegrees is one tooth-step multiple, so that 0 matches the last frame.
        public static float GearDegrees(SettingsPhase phase, float age)
        {
            if (phase == SettingsPhase.Opening)
            {
                float u = PopupMotion.Beat(age, 0f, OpenSeconds);
                return SpinDegrees * PopupMotion.EaseOutCubic(u);
            }
            if (phase == SettingsPhase.Closing)
            {
                float u = PopupMotion.Beat(age, 0f, CloseSeconds);
                return SpinDegrees * (1f - PopupMotion.EaseInCubic(u));
            }
            return 0f;
        }
    }
}
