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
