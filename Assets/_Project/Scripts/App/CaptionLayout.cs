using UnityEngine;

namespace FlockFive
{
    // Shared caption fit. An authored newline is a hard break: the font shrinks
    // until each piece fits on one line. The fitter does not invent a wrap inside
    // a piece ("Honey / badger"). PaintCoachCaption and the badger preview both
    // come through here.
    public struct CoachCaptionPreview
    {
        public string Shown;
        public int Font;
        public float TextW;
        public float TextH;
    }

    public static class CaptionLayout
    {
        static GUIStyle _scratch;
        static GUIContent _content;

        static void Bind(GUIStyle proto, string text, int size, bool wrap)
        {
            if (_scratch == null) _scratch = new GUIStyle();
            if (proto != null)
            {
                _scratch.font = proto.font;
                _scratch.fontStyle = proto.fontStyle;
                _scratch.alignment = proto.alignment;
                _scratch.richText = proto.richText;
            }
            _scratch.wordWrap = wrap;
            _scratch.fontSize = Mathf.Max(1, size);
            if (proto != null && proto.padding != null)
            {
                var pad = proto.padding;
                _scratch.padding = new RectOffset(pad.left, pad.right, pad.top, pad.bottom);
            }
            else
                _scratch.padding = new RectOffset(0, 0, 0, 0);
            if (_content == null) _content = new GUIContent();
            _content.text = text ?? "";
        }

        // Longest authored line, plus a few pixels, at the largest size in
        // [floor, hi] that fits maxW. `line` may already contain newlines.
        public static Vector2 Measure(GUIStyle style, string line, float maxW, int hi, int floor)
        {
            if (hi < 1) hi = 1;
            if (floor < 1) floor = 1;
            if (floor > hi) floor = hi;
            if (string.IsNullOrEmpty(line)) return new Vector2(Mathf.Max(8f, maxW), 32f);
            string measured = line.IndexOf('\n') > 0 ? line : BalanceTwoLines(line);
            int br = measured.IndexOf('\n');
            string top = br > 0 ? measured.Substring(0, br) : measured;
            string bot = br > 0 ? measured.Substring(br + 1) : measured;
            float w = maxW;
            float h = 32f;
            for (int fs = hi; fs >= floor; fs--)
            {
                Bind(style, top, fs, false);
                float wa = _scratch.CalcSize(_content).x;
                Bind(style, bot, fs, false);
                float wb = _scratch.CalcSize(_content).x;
                float need = (wa > wb ? wa : wb) + 8f;
                if (need > maxW && fs > floor) continue;
                float useW = need > maxW ? maxW : need;
                if (useW < 8f) useW = 8f;
                Bind(style, line, fs, true);
                float th = _scratch.CalcHeight(_content, useW);
                Bind(style, top, fs, false);
                int rows = 1;
                for (int ci = 0; ci < measured.Length; ci++)
                    if (measured[ci] == '\n') rows++;
                float two = _scratch.CalcSize(_content).y * rows;
                if (th > two + 1f) th = two;
                w = useW;
                h = th > 1f ? th : 1f;
                break;
            }
            return new Vector2(w, h);
        }

        // Keep every authored line. Shrink until each piece's width fits maxW
        // and the block's height fits maxH. Never splits a piece to make it fit.
        public static string LockBreaks(GUIStyle style, string text, float maxW, float maxH, int lo, int hi, out int font)
        {
            font = 1;
            if (string.IsNullOrEmpty(text)) return text ?? "";
            if (hi < 1) hi = 1;
            if (lo < 1) lo = 1;
            if (lo > hi) lo = hi;
            var parts = text.Split('\n');
            int chosen = lo;
            for (int px = hi; px >= lo; px--)
            {
                if (!WidthsFit(style, parts, maxW, px)) continue;
                if (maxH > 1f && !HeightFits(style, text, maxW, maxH, px)) continue;
                chosen = px;
                break;
            }
            font = chosen;
            return text;
        }

        public static bool LineFits(GUIStyle style, string line, float maxW, int font)
        {
            return LineWidth(style, line, font) <= maxW + 0.5f;
        }

        // Same width CalcSize reports for one unwrapped row. Requests the glyphs
        // first so a dynamic font does not answer with a short guess.
        public static float LineWidth(GUIStyle style, string line, int font)
        {
            if (string.IsNullOrEmpty(line)) return 0f;
            int size = font < 1 ? 1 : font;
            if (style != null && style.font != null)
                style.font.RequestCharactersInTexture(line, size, style.fontStyle);
            Bind(style, line, size, false);
            // Bold dynamic fonts report a short width on the first CalcSize.
            float a = _scratch.CalcSize(_content).x;
            float b = _scratch.CalcSize(_content).x;
            return a > b ? a : b;
        }

        static bool WidthsFit(GUIStyle style, string[] parts, float maxW, int font)
        {
            if (maxW < 8f) return false;
            for (int i = 0; i < parts.Length; i++)
            {
                if (string.IsNullOrEmpty(parts[i])) continue;
                if (!LineFits(style, parts[i], maxW, font)) return false;
            }
            return true;
        }

        static bool HeightFits(GUIStyle style, string text, float maxW, float maxH, int font)
        {
            Bind(style, text, font, true);
            return _scratch.CalcHeight(_content, Mathf.Max(8f, maxW)) <= maxH + 1f;
        }

        // Garden bee lesson. Headline plus two short lines. Paint draws a bee
        // icon on the left of this plate. The emoji swap stays for fonts that have it.
        public const string HiveGardenLine =
            "You found a bee!\nBees earn album cards.\nTap the hive.";

        public const string HiveGardenEmojiLine =
            "You found a bee! \U0001F41D\nBees earn album cards.\nTap the hive.";

        // Inspect back. The value sentence is exact. The two tap lines stay with it.
        public const string AlbumHoneyBlock =
            "Honeycombs show the card's value based on rarity.\nTap to flip!\nTap outside to put back";

        public const string LeafLine =
            "Leaves hide these birds.\nCollect at a feeder to clear them.";

        public const string SparrowLine =
            "A sparrow blocks a feeder!\nMatch five birds there to chase it off.";

        public const string HawkLine =
            "A hawk blocks a feeder!\nMatch five there twice.";

        // First ostrich garden. HeadlineGap inserts the blank line at paint time.
        public const string OstrichLine =
            "An ostrich blocks feeders!\nMatch three sets to chase it off.";

        public const string BeePestLine =
            "Bees cover a branch!\nMove birds off to free a card.";

        public const string CoachPlaceLine =
            "Tap a branch with the same color, or an empty one.";

        public const string CoachNowhereLine =
            "Nowhere to go yet.\nTry another branch.";

        public const string VipPitchLine = "Tap VIP anytime to drop ads.";

        public const string AlbumUpgradeLine = "Upgrade adds honey for 1000 coins.";

        public const string AdoptGreetLine = "Look, a bird followed you home!";

        // The garden bee callout is a shared banner. The plate used to draw a bee
        // sprite on the left; that icon is gone so the words match the other banners.
        public static bool WantsBeeIcon(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return false;
        }

        public static float BeeIconSlot(float lineH)
        {
            if (lineH < 8f) lineH = 8f;
            return lineH * 0.85f + lineH * 0.22f;
        }

        // Plate pad. Unscaled minimums, and larger on a tall phone.
        public const float PadXMin = 28f;
        public const float PadYMin = 18f;

        public static Vector2 BoxPad(float screenH)
        {
            float s = DeviceFit.UiH(screenH);
            float x = Mathf.Max(PadXMin, 16f * s);
            float y = Mathf.Max(PadYMin, 11f * s);
            return new Vector2(x, y);
        }

        public struct SnugPlate
        {
            public int Font;
            public float TextW;
            public float TextH;
            public float LineH;
            public float PadX;
            public float PadY;
            public float PlateW;
            public float PlateH;
        }

        // One line of the coach font, used as the caption's padding unit.
        public static float LineHeight(GUIStyle style, int font)
        {
            if (font < 1) font = 1;
            Bind(style, "Mg", font, false);
            float h = _scratch.CalcSize(_content).y;
            if (h < font * 0.8f) h = font;
            return h;
        }

        // About one line across and 0.6 of a line down. The plate uses this
        // unless the shared BoxPad hang is already larger.
        public static Vector2 TextPad(float lineH)
        {
            if (lineH < 1f) lineH = 1f;
            return new Vector2(lineH * 1f, lineH * 0.6f);
        }

        // Plate that hugs `shown`. Pad is the larger of TextPad and the hang
        // CoachPanelRect already adds, so the hang is part of the padding
        // instead of extra empty plate.
        public static SnugPlate MeasureSnug(GUIStyle style, string shown, int font, float hangX, float hangY)
        {
            var plate = new SnugPlate();
            plate.Font = font < 1 ? 1 : font;
            string text = shown ?? "";
            float lineH = LineHeight(style, plate.Font);
            plate.LineH = lineH;
            float textW = 8f;
            float textH = lineH;
            if (text.Length > 0)
            {
                var parts = text.Split('\n');
                textW = 0f;
                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i].Length == 0) continue;
                    Bind(style, parts[i], plate.Font, false);
                    float w = _scratch.CalcSize(_content).x;
                    if (w > textW) textW = w;
                }
                if (textW < 8f) textW = 8f;
                Bind(style, text, plate.Font, true);
                textH = _scratch.CalcHeight(_content, textW);
                int rows = 1;
                for (int i = 0; i < text.Length; i++)
                    if (text[i] == '\n') rows++;
                float stack = lineH * rows;
                if (textH < stack * 0.85f) textH = stack;
                if (textH < lineH) textH = lineH;
            }
            var want = TextPad(lineH);
            float padX = want.x > hangX ? want.x : hangX;
            float padY = want.y > hangY ? want.y : hangY;
            if (hangX < 0f) hangX = 0f;
            if (hangY < 0f) hangY = 0f;
            plate.TextW = textW;
            plate.TextH = textH;
            plate.PadX = padX;
            plate.PadY = padY;
            plate.PlateW = textW + padX * 2f;
            plate.PlateH = textH + padY * 2f;
            return plate;
        }

        // True only when the font draws the bee (U+1F41D). LegacyRuntime does not.
        // Font.HasCharacter misses non-BMP, so this asks for the glyph and checks
        // that both UTF-16 units come back with a real advance.
        public static bool DrawsEmoji(Font font)
        {
            const string mark = "\U0001F41D";
            if (font == null) return false;
            font.RequestCharactersInTexture(mark, 32, FontStyle.Bold);
            if (mark.Length < 2) return false;
            if (!font.GetCharacterInfo(mark[0], out var hi, 32, FontStyle.Bold)) return false;
            if (!font.GetCharacterInfo(mark[1], out var lo, 32, FontStyle.Bold)) return false;
            float adv = hi.advance + lo.advance;
            int wide = hi.glyphWidth > lo.glyphWidth ? hi.glyphWidth : lo.glyphWidth;
            return adv > 8f && wide > 8;
        }

        // Paint-time swap. The cue string stays the plain sentence so a repeat
        // does not restart the fade. The blank line is applied here and again
        // in FitFloor; HeadlineGap is idempotent so the two agree.
        public static string LessonShown(string text, Font font)
        {
            if (text == HiveGardenLine && DrawsEmoji(font)) text = HiveGardenEmojiLine;
            return HeadlineGap(text);
        }

        // One empty line between the first sentence and the rest. A single
        // sentence, including one that already wraps, stays as authored.
        // Trailing marks on the headline (the bee emoji) stay on that line.
        public static string HeadlineGap(string text)
        {
            if (string.IsNullOrEmpty(text)) return text ?? "";
            int split = FirstSentenceEnd(text);
            if (split < 0) return text;
            int body = split + 1;
            while (body < text.Length && text[body] != '\n' && text[body] != '\r' && !char.IsLetterOrDigit(text[body]))
                body++;
            int rest = body;
            while (rest < text.Length && (text[rest] == ' ' || text[rest] == '\n' || text[rest] == '\r'))
                rest++;
            if (rest >= text.Length || !HasAlnum(text, rest)) return text;
            string between = text.Substring(body, rest - body);
            if (between.IndexOf("\n\n", System.StringComparison.Ordinal) >= 0) return text;
            string head = text.Substring(0, body).TrimEnd(' ', '\r');
            return head + "\n\n" + text.Substring(rest);
        }

        static int FirstSentenceEnd(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c != '.' && c != '!' && c != '?') continue;
                int j = i;
                while (j + 1 < text.Length && text[j + 1] == c) j++;
                if (j + 1 < text.Length && char.IsDigit(text[j + 1])) continue;
                return j;
            }
            return -1;
        }

        static bool HasAlnum(string text, int from)
        {
            for (int i = from; i < text.Length; i++)
                if (char.IsLetterOrDigit(text[i])) return true;
            return false;
        }

        // Glass inside the side margins. Plates stay inside this. Text stays
        // inside it minus the shared box pad, so a line cannot run off either edge.
        public static float SafeMargin(float screenW, float screenH)
        {
            float s = screenH > 2f ? DeviceFit.UiH(screenH) : 1f;
            if (s < 1f) s = 1f;
            float m = 12f * s;
            if (m < 18f) m = 18f;
            if (screenW > 2f && m > screenW * 0.2f) m = screenW * 0.2f;
            return m;
        }

        public static float SafePlateWidth(float screenW, float screenH)
        {
            float w = screenW - SafeMargin(screenW, screenH) * 2f;
            if (w < 48f) w = screenW > 48f ? 48f : Mathf.Max(8f, screenW);
            return w;
        }

        public static float SafeTextWidth(float screenW, float screenH)
        {
            float w = SafePlateWidth(screenW, screenH) - BoxPad(screenH).x * 2f;
            if (w < 48f) w = 48f;
            return w;
        }

        // Text width whose snug plate (the larger of TextPad and BoxPad) stays
        // inside the safe plate. SafeTextWidth uses only BoxPad, which is smaller
        // than TextPad at the lesson font, so a line that fills it spills the glass.
        public static float GoldTextWidth(GUIStyle style, float screenW, float screenH, int font)
        {
            if (font < 1) font = 1;
            float hang = BoxPad(screenH).x;
            float want = TextPad(LineHeight(style, font)).x;
            float pad = want > hang ? want : hang;
            float w = SafePlateWidth(screenW, screenH) - pad * 2f - 1f;
            if (w < 48f) w = 48f;
            return w;
        }

        public struct CaptionFit
        {
            public string Shown;
            public int Font;
            public float TextH;
        }

        // What PaintCoachCaption draws: floor font, wraps inside the safe text
        // width, snug plate. Overflow is a rendered line wider than that width,
        // or a plate wider than the safe plate.
        public struct GoldFit
        {
            public string Shown;
            public int Font;
            public float TextW;
            public float TextH;
            public float PlateW;
            public float PlateH;
            public int Lines;
            public bool Overflow;
        }

        public static GoldFit FitGold(GUIStyle style, string text, float screenW, float screenH, int floor, int hi)
        {
            var gold = new GoldFit { Shown = text ?? "", Font = 1, Lines = 1 };
            int size = hi > floor ? hi : floor;
            float maxW = GoldTextWidth(style, screenW, screenH, size);
            var fit = FitFloor(style, text, maxW, 4096f, floor, hi);
            var hang = BoxPad(screenH);
            var snug = MeasureSnug(style, fit.Shown, fit.Font, hang.x, hang.y);
            int lines = 1;
            bool overflow = false;
            string shown = fit.Shown ?? "";
            if (shown.Length > 0)
            {
                var parts = shown.Split('\n');
                lines = 0;
                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i].Length == 0) continue;
                    lines++;
                    if (!LineFits(style, parts[i], maxW, fit.Font)) overflow = true;
                }
                if (lines < 1) lines = 1;
            }
            if (snug.PlateW > SafePlateWidth(screenW, screenH) + 0.5f) overflow = true;
            gold.Shown = shown;
            gold.Font = fit.Font;
            gold.TextW = snug.TextW;
            gold.TextH = snug.TextH;
            gold.PlateW = snug.PlateW;
            gold.PlateH = snug.PlateH;
            gold.Lines = lines;
            gold.Overflow = overflow;
            return gold;
        }

        // Shrink from hi down to floor. Authored newlines stay. At the floor the
        // fitter wraps a piece onto more lines instead of going smaller. The
        // caller grows the plate when TextH is taller than the seat.
        public static CaptionFit FitFloor(GUIStyle style, string text, float maxW, float maxH, int floor, int hi)
        {
            text = HeadlineGap(text);
            var fit = new CaptionFit { Shown = text ?? "", Font = 1, TextH = 1f };
            if (string.IsNullOrEmpty(text)) return fit;
            if (hi < 1) hi = 1;
            if (floor < 1) floor = 1;
            if (floor > hi) floor = hi;
            if (maxW < 8f) maxW = 8f;
            int chosen = floor;
            string shown = text;
            float textH = 1f;
            for (int px = hi; px >= floor; px--)
            {
                shown = WrapAt(style, text, maxW, px);
                Bind(style, shown, px, true);
                textH = _scratch.CalcHeight(_content, maxW);
                if (textH < 1f) textH = 1f;
                chosen = px;
                bool wide = WidthsFit(style, shown.Split('\n'), maxW, px);
                bool tall = maxH <= 1f || textH <= maxH + 1f;
                if (wide && tall) break;
                if (px == floor) break;
            }
            fit.Shown = shown;
            fit.Font = chosen;
            fit.TextH = textH;
            return fit;
        }

        static string WrapAt(GUIStyle style, string text, float maxW, int px)
        {
            var parts = text.Split('\n');
            var sb = new System.Text.StringBuilder();
            for (int p = 0; p < parts.Length; p++)
            {
                if (p > 0) sb.Append('\n');
                sb.Append(WrapPiece(style, parts[p], maxW, px));
            }
            return sb.ToString();
        }

        static string WrapPiece(GUIStyle style, string piece, float maxW, int px)
        {
            if (string.IsNullOrEmpty(piece)) return "";
            if (LineFits(style, piece, maxW, px)) return piece;
            var words = piece.Split(' ');
            var sb = new System.Text.StringBuilder();
            string cur = "";
            for (int i = 0; i < words.Length; i++)
            {
                string w = words[i];
                if (w.Length == 0) continue;
                string trial = cur.Length == 0 ? w : cur + " " + w;
                if (cur.Length == 0 || LineFits(style, trial, maxW, px))
                    cur = trial;
                else
                {
                    if (sb.Length > 0) sb.Append('\n');
                    sb.Append(cur);
                    cur = w;
                }
            }
            if (cur.Length > 0)
            {
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(cur);
            }
            return sb.Length > 0 ? sb.ToString() : piece;
        }

        // Banner type. One size for every banner: the size the adopt greet already
        // uses (the old shrink loop's start). Banners do not auto-shrink. A long
        // line wraps into at most BannerMaxLines balanced rows and the plate grows.
        public const int BannerMaxLines = 3;

        public static int BannerFont(float screenW, float screenH)
        {
            float ui = DeviceFit.Ui(screenW, screenH);
            int px = Mathf.RoundToInt(44f * ui * 0.62f);
            if (px < 22) px = 22;
            return px;
        }

        // Preferred text column. A long row at the fixed size (the sparrow line on
        // a phone) does not fit here, so BannerMeasureWidth opens the column out
        // to the screen edge instead of shrinking the font.
        public const float BannerInsetFrac = 0.92f;

        public static float BannerMeasureWidth(float screenW, GUIStyle style, string text, int font)
        {
            float full = screenW < 8f ? 8f : screenW;
            float inset = full * BannerInsetFrac;
            if (inset < 8f) inset = 8f;
            if (inset > full) inset = full;
            if (string.IsNullOrEmpty(text)) return inset;
            // Keep the author's breaks. Widen the column before inventing a new wrap,
            // so "Bees earn album cards." stays one row instead of spilling into the next.
            if (KeepsAuthor(style, text, inset, font)) return inset;
            if (KeepsAuthor(style, text, full, font)) return full;
            var fit = FitBanner(style, text, inset, font);
            if (fit.Lines >= 1 && fit.Lines <= BannerMaxLines && ShownFits(style, fit.Shown, inset, font))
                return inset;
            return full;
        }

        static bool KeepsAuthor(GUIStyle style, string text, float maxW, int font)
        {
            if (string.IsNullOrEmpty(text)) return true;
            return AuthoredBanner(style, text, maxW, font) != null;
        }

        public static bool ShownFits(GUIStyle style, string shown, float maxW, int font)
        {
            if (string.IsNullOrEmpty(shown)) return true;
            int i = 0;
            while (i <= shown.Length)
            {
                int j = shown.IndexOf('\n', i);
                if (j < 0) j = shown.Length;
                if (j > i && !LineFits(style, shown.Substring(i, j - i), maxW, font)) return false;
                if (j >= shown.Length) break;
                i = j + 1;
            }
            return true;
        }

        public struct BannerMeasure
        {
            public string Shown;
            public int Font;
            public float TextH;
            public int Lines;
        }

        // Fixed `font`. No shrink. Authored rows stay when each one fits in maxW
        // and there are at most BannerMaxLines of them. Otherwise the words
        // reflow into the fewest balanced rows that fit, never more than the cap.
        public static BannerMeasure FitBanner(GUIStyle style, string text, float maxW, int font)
        {
            var measure = new BannerMeasure();
            measure.Font = font < 1 ? 1 : font;
            measure.Shown = text ?? "";
            measure.Lines = 1;
            measure.TextH = 1f;
            if (string.IsNullOrEmpty(text)) return measure;
            if (maxW < 8f) maxW = 8f;
            string shown = AuthoredBanner(style, text, maxW, measure.Font);
            if (shown == null) shown = BalancedBanner(style, text, maxW, measure.Font, BannerMaxLines);
            if (string.IsNullOrEmpty(shown)) shown = text;
            int lines = 1;
            for (int i = 0; i < shown.Length; i++)
                if (shown[i] == '\n') lines++;
            if (lines < 1) lines = 1;
            float lineH = LineHeight(style, measure.Font);
            float stack = lineH * lines;
            Bind(style, shown, measure.Font, true);
            float th = _scratch.CalcHeight(_content, maxW);
            if (th < stack * 0.85f) th = stack;
            if (th > stack * 1.25f) th = stack;
            if (th < 1f) th = 1f;
            measure.Shown = shown;
            measure.TextH = th;
            measure.Lines = lines;
            return measure;
        }

        // Authored newlines, when every piece fits on one row. A blank row is not
        // this path: the balancer drops it and refolds the words.
        static string AuthoredBanner(GUIStyle style, string text, float maxW, int font)
        {
            if (text.IndexOf("\n\n", System.StringComparison.Ordinal) >= 0) return null;
            if (text.IndexOf('\n') < 0)
                return LineFits(style, text, maxW, font) ? text : null;
            int lines = 0;
            int i = 0;
            while (i <= text.Length)
            {
                int j = text.IndexOf('\n', i);
                if (j < 0) j = text.Length;
                int len = j - i;
                if (len > 0)
                {
                    if (!LineFits(style, text.Substring(i, len), maxW, font)) return null;
                    lines++;
                    if (lines > BannerMaxLines) return null;
                }
                if (j >= text.Length) break;
                i = j + 1;
            }
            if (lines < 1) return null;
            return text;
        }

        static string[] _banWords = new string[48];
        static float[] _banWordW = new float[48];
        static float[,] _banCost = new float[4, 49];
        static int[,] _banPrev = new int[4, 49];
        static float[,] _spanW = new float[48, 49];

        static string BalancedBanner(GUIStyle style, string text, float maxW, int font, int maxLines)
        {
            if (maxLines < 1) maxLines = 1;
            if (maxLines > 3) maxLines = 3;
            int n = TakeWords(text);
            if (n <= 0) return text ?? "";
            if (n == 1) return _banWords[0];
            if (n > _banWordW.Length) return text ?? "";
            MeasureSpans(style, n, font);
            int use = 0;
            for (int k = 1; k <= maxLines && k <= n; k++)
            {
                PackLines(n, k);
                if (_banCost[k, n] <= maxW + 0.5f) { use = k; break; }
            }
            if (use < 1)
            {
                use = n < maxLines ? n : maxLines;
                PackLines(n, use);
            }
            return JoinPacked(n, use);
        }

        // Width of words [from, to) as one drawn row. Same CalcSize LineFits uses,
        // so a packed row cannot slip past the fit check on a short space guess.
        static void MeasureSpans(GUIStyle style, int n, int font)
        {
            int cap = _banWords.Length;
            if (_spanW.GetLength(0) < cap || _spanW.GetLength(1) < cap + 1)
                _spanW = new float[cap, cap + 1];
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < n; i++)
            {
                sb.Length = 0;
                for (int j = i; j < n; j++)
                {
                    if (sb.Length > 0) sb.Append(' ');
                    sb.Append(_banWords[j]);
                    _spanW[i, j + 1] = LineWidth(style, sb.ToString(), font);
                }
            }
        }

        static int TakeWords(string text)
        {
            int n = 0;
            int i = 0;
            int len = text.Length;
            while (i < len)
            {
                while (i < len)
                {
                    char c = text[i];
                    if (c != ' ' && c != '\n' && c != '\r' && c != '\t') break;
                    i++;
                }
                if (i >= len) break;
                int j = i + 1;
                while (j < len)
                {
                    char c = text[j];
                    if (c == ' ' || c == '\n' || c == '\r' || c == '\t') break;
                    j++;
                }
                if (n >= _banWords.Length)
                {
                    var grown = new string[_banWords.Length * 2];
                    for (int k = 0; k < _banWords.Length; k++) grown[k] = _banWords[k];
                    _banWords = grown;
                    _banWordW = new float[_banWords.Length];
                    int rows = _banCost.GetLength(0);
                    _banCost = new float[rows, _banWords.Length + 1];
                    _banPrev = new int[rows, _banWords.Length + 1];
                }
                _banWords[n++] = text.Substring(i, j - i);
                i = j;
            }
            return n;
        }

        // cost[k, i] is the narrowest "widest row" covering the first i words in k rows.
        static void PackLines(int n, int lines)
        {
            int cap = _banCost.GetLength(1);
            if (n + 1 > cap) return;
            for (int k = 0; k <= lines; k++)
                for (int i = 0; i <= n; i++)
                {
                    _banCost[k, i] = 1e9f;
                    _banPrev[k, i] = 0;
                }
            _banCost[0, 0] = 0f;
            for (int k = 1; k <= lines; k++)
            {
                for (int i = k; i <= n; i++)
                {
                    float best = 1e9f;
                    int at = 0;
                    int p0 = k == 1 ? 0 : k - 1;
                    int p1 = k == 1 ? 0 : i - 1;
                    for (int p = p0; p <= p1; p++)
                    {
                        float prev = k == 1 ? 0f : _banCost[k - 1, p];
                        if (prev >= 1e8f) continue;
                        float row = _spanW[p, i];
                        float mx = row > prev ? row : prev;
                        if (mx < best) { best = mx; at = p; }
                    }
                    _banCost[k, i] = best;
                    _banPrev[k, i] = at;
                }
            }
        }

        static string JoinPacked(int n, int lines)
        {
            var start = new int[lines];
            int i = n;
            for (int k = lines; k >= 1; k--)
            {
                int p = _banPrev[k, i];
                start[k - 1] = p;
                i = p;
            }
            var sb = new System.Text.StringBuilder();
            for (int k = 0; k < lines; k++)
            {
                int a = start[k];
                int b = k + 1 < lines ? start[k + 1] : n;
                if (k > 0) sb.Append('\n');
                for (int w = a; w < b; w++)
                {
                    if (w > a) sb.Append(' ');
                    sb.Append(_banWords[w]);
                }
            }
            return sb.ToString();
        }

        // Splits a long sentence at the space nearest its middle. Short or
        // break-free text comes back unchanged. Explicit newlines are not this path.
        public static string BalanceTwoLines(string line)
        {
            if (string.IsNullOrEmpty(line) || line.Length < 26) return line;
            if (line.IndexOf('\n') >= 0) return line;
            int mid = line.Length / 2;
            int best = -1;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] != ' ') continue;
                if (best < 0 || Mathf.Abs(i - mid) < Mathf.Abs(best - mid)) best = i;
            }
            if (best <= 0 || best >= line.Length - 1) return line;
            return line.Substring(0, best) + "\n" + line.Substring(best + 1);
        }
    }
}
