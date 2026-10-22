using System.Text;
using UnityEngine;

namespace FlockFive
{
    // b126 (Brandon, mockup D): the tap-to-continue cue. A tiny fingertip that taps the caption box's
    // bottom-right corner, no words. One shared helper: FlockFiveApp.KeepCaptionOnTop (the hook every
    // caption paint path calls with its final plate) asks ForCaption and draws via DrawAt.
    // The cue never changes the box: CueRect only reads the plate, and the plate is measured before it.
    // Shown only on tap-to-continue captions: CoachTap.AnyTap steps (any tap anywhere advances).
    // Required steps (glove on a target) and the Daily claim are excluded.
    public static class TapCue
    {
        // Sizes are fractions of the caption font px.
        public const float WidthEm = 0.30f;     // fingertip width
        public const float HeightEm = 0.50f;    // fingertip height (tip + short finger stub)
        public const float OutlineEm = 0.07f;   // dark outline, slightly thicker than the D sketch
        public const int OutlineMinPx = 3;
        // Tip anchor: inside the right edge, hanging past the bottom edge (part in, part out).
        public const float InsetRightEm = 0.45f;
        public const float HangBelowEm = 0.18f;
        // Tap: a short press every Period seconds. Dips DipEm, a faint touch mark shows while down.
        public const float Period = 1.4f;
        public const float PressFrac = 0.22f;
        public const float DipEm = 0.08f;
        public const float MarkAlpha = 0.28f;
        public const float MarkEm = 0.22f;

        static string Norm(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length);
            foreach (char c in s) if (!char.IsWhiteSpace(c)) sb.Append(c);
            return sb.ToString();
        }

        // True when this caption text belongs to a tap-to-continue step. Wrapping and HeadlineGap ignored.
        public static bool ForCaption(string text)
        {
            string n = Norm(text);
            if (n.Length == 0) return false;
            foreach (CoachStep step in System.Enum.GetValues(typeof(CoachStep)))
            {
                if (!CoachTap.AnyTap(step)) continue;
                if (Norm(CoachTap.Caption(step)) == n) return true;
            }
            return false;
        }

        public static int OutlinePx(int px) => Mathf.Max(OutlineMinPx, Mathf.RoundToInt(px * OutlineEm));

        // 0..1 press depth at time t.
        public static float Press(float t)
        {
            float u = Mathf.Repeat(t, Period) / Period;
            if (u >= PressFrac) return 0f;
            return Mathf.Sin(u / PressFrac * Mathf.PI);
        }

        // GUI rect (y down) of the fingertip for a plate. Reads the plate only.
        public static Rect CueRect(Rect plate, int px, float t)
        {
            float w = Mathf.Max(6f, px * WidthEm), h = Mathf.Max(10f, px * HeightEm);
            float tipX = plate.xMax - px * InsetRightEm;
            float tipY = plate.yMax + px * HangBelowEm + Press(t) * px * DipEm;
            return new Rect(tipX - w * 0.5f, tipY - h, w, h);
        }

        public static Rect MarkRect(Rect cue, int px)
        {
            float m = px * MarkEm;
            return new Rect(cue.center.x - m * 0.5f, cue.yMax - m * 0.25f, m, m * 0.4f);
        }

        static Texture2D _tex, _dot;
        static int _texW, _texH, _texO;

        // Fingertip: rounded tip at the bottom, straight stub up, white fill, dark outline.
        public static Texture2D Tex(int w, int h, int outline)
        {
            if (_tex != null && _texW == w && _texH == h && _texO == outline) return _tex;
            if (_tex != null) Object.DestroyImmediate(_tex);
            _tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave, name = "TapCueTip" };
            var px = new Color32[w * h];
            float r = w * 0.5f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    // texture y=0 is the bottom (the tip)
                    float cx = x + 0.5f - r, cy = y + 0.5f;
                    float d = cy < r ? Mathf.Sqrt(cx * cx + (cy - r) * (cy - r)) : Mathf.Abs(cx);
                    float edge = r - d;
                    Color32 c = new Color32(0, 0, 0, 0);
                    if (edge > outline) c = new Color32(250, 250, 252, 255);
                    else if (edge > 0f) c = new Color32(28, 28, 36, 255);
                    px[y * w + x] = c;
                }
            _tex.SetPixels32(px);
            _tex.Apply(false, false);
            _texW = w; _texH = h; _texO = outline;
            return _tex;
        }

        static Texture2D Dot()
        {
            if (_dot != null) return _dot;
            const int n = 32;
            _dot = new Texture2D(n, n, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, name = "TapCueMark" };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            _dot.SetPixels32(px); _dot.Apply(false, false);
            return _dot;
        }

        public static void DrawAt(Rect plate, int px, float fade, float t)
        {
            if (fade < 0.04f || px < 1) return;
            var r = CueRect(plate, px, t);
            var prev = GUI.color;
            float press = Press(t);
            if (press > 0.01f)
            {
                GUI.color = new Color(1f, 1f, 1f, MarkAlpha * press * fade);
                GUI.DrawTexture(MarkRect(r, px), Dot());
            }
            GUI.color = new Color(1f, 1f, 1f, fade);
            GUI.DrawTexture(r, Tex(Mathf.RoundToInt(r.width), Mathf.RoundToInt(r.height), OutlinePx(px)));
            GUI.color = prev;
        }
    }
}
