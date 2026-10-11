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
            if (string.IsNullOrEmpty(line)) return true;
            Bind(style, line, font, false);
            return _scratch.CalcSize(_content).x <= maxW + 0.5f;
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

        // Garden bee lesson, without a glyph. Paint inserts the bee only when the
        // coach font actually has it.
        public const string HiveGardenLine =
            "You found a bee!\nBees earn cards for your collection.\nTap the hive to see them.";

        public const string HiveGardenEmojiLine =
            "You found a bee! \U0001F41D\nBees earn cards for your collection.\nTap the hive to see them.";

        // Inspect back: honey sentence stacked with the flip hints, one plate.
        public const string AlbumHoneyBlock =
            "Honeycombs are honey.\n2 normal \u00b7 3 foil \u00b7 5 inverse.\nTap to flip!\nTap outside to put back";

        // Plate pad. Unscaled minimums, and larger on a tall phone.
        public const float PadXMin = 28f;
        public const float PadYMin = 18f;

        public static Vector2 BoxPad(float screenH)
        {
            float s = Mathf.Max(screenH / 720f, 1f);
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
        // does not restart the fade.
        public static string LessonShown(string text, Font font)
        {
            if (text == HiveGardenLine && DrawsEmoji(font)) return HiveGardenEmojiLine;
            return text ?? "";
        }

        public struct CaptionFit
        {
            public string Shown;
            public int Font;
            public float TextH;
        }

        // Shrink from hi down to floor. Authored newlines stay. At the floor the
        // fitter wraps a piece onto more lines instead of going smaller. The
        // caller grows the plate when TextH is taller than the seat.
        public static CaptionFit FitFloor(GUIStyle style, string text, float maxW, float maxH, int floor, int hi)
        {
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
