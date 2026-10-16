using UnityEngine;

namespace FlockFive
{
    // Shared layout for the end-of-round "Cards collected this round" panel.
    // Few cards: static, side by side, fully inside the stage (no carousel).
    // More than fit: the carousel, with soft edge fades instead of a hard clip.
    public static class CollectedLayout
    {
        public const float Aspect = 1.35f;      // card h / w
        public const float HeroH = 0.94f;       // single card height share of the stage
        public const float MinShare = 0.48f;    // a static card is never below 48% of the hero width (inspect is a tap away)
        public const float GapK = 0.06f;        // gap between static cards, share of card width
        public const int MaxStatic = 3;
        public const float FadeK = 0.16f;       // edge fade width, share of the stage width

        public static float HeroW(Rect stage)
        {
            float w = stage.height * HeroH / Aspect;
            return Mathf.Min(w, stage.width * 0.62f);
        }

        // Best grid for n cards: the rows x cols that gives the biggest card.
        // Tall phone panels stack 2 and 3 cards (2 over 1); wide panels keep one row.
        public static void Grid(Rect stage, int n, out int rows, out int cols, out float w)
        {
            rows = 1; cols = Mathf.Max(1, n); w = HeroW(stage);
            if (n <= 1) return;
            float best = -1f;
            for (int r = 1; r <= n; r++)
            {
                int c = (n + r - 1) / r;
                float byW = stage.width / (c + (c + 1) * GapK);
                float byH = stage.height / (r * Aspect + (r + 1) * GapK);
                float cw = Mathf.Min(Mathf.Min(byW, byH), HeroW(stage));
                if (cw > best + 0.5f) { best = cw; rows = r; cols = c; }
            }
            w = best;
        }

        public static float RowW(Rect stage, int n)
        {
            Grid(stage, n, out _, out _, out float w);
            return w;
        }

        // How many cards show static. Computed from the stage size and aspect.
        public static int FitCount(Rect stage)
        {
            float hero = HeroW(stage);
            int fit = 1;
            for (int n = 2; n <= MaxStatic; n++)
                if (RowW(stage, n) >= hero * MinShare) fit = n;
            return fit;
        }

        public static bool Static(Rect stage, int count) => count >= 1 && count <= FitCount(stage);

        // Rect of card i of n, centered in the stage. A short last row is centered.
        // A single row gets a soft fan: outer cards sit a hair lower than the middle.
        public static Rect Slot(Rect stage, int i, int n)
        {
            Grid(stage, n, out int rows, out int cols, out float w);
            float h = w * Aspect;
            float gap = w * GapK;
            int row = i / cols;
            int inRow = row < rows - 1 ? cols : n - cols * (rows - 1);
            int col = i - row * cols;
            float rowW = inRow * w + (inRow - 1) * gap;
            float gridH = rows * h + (rows - 1) * gap;
            float x = stage.x + (stage.width - rowW) * 0.5f + col * (w + gap);
            float y = stage.y + (stage.height - gridH) * 0.5f + row * (h + gap);
            if (rows == 1 && inRow > 1)
            {
                float mid = (inRow - 1) * 0.5f;
                y += Mathf.Abs(col - mid) / Mathf.Max(0.5f, mid) * h * 0.025f;
            }
            return new Rect(x, y, w, h);
        }

        public static float FadeW(Rect stage) => stage.width * FadeK;

        // Alpha ramp, 0 at the left. Used to paint the panel color over the stage edges.
        static Texture2D _ramp;
        public static Texture2D Ramp()
        {
            if (_ramp != null) return _ramp;
            const int n = 64;
            _ramp = new Texture2D(n, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
            for (int x = 0; x < n; x++)
            {
                float u = x / (float)(n - 1);
                float a = 1f - Mathf.SmoothStep(0f, 1f, u); // opaque at the edge, clear inward
                _ramp.SetPixel(x, 0, new Color(1f, 1f, 1f, a));
            }
            _ramp.Apply(false, true);
            return _ramp;
        }

        // Soft fades at both edges of the clip, in the panel color. Cards slide under
        // them and fade instead of being cut at a hard line.
        public static void DrawEdgeFades(Rect clip, float fadeW, Color panel)
        {
            if (Event.current == null || Event.current.type != EventType.Repaint) return;
            var tex = Ramp();
            var keep = GUI.color;
            GUI.color = panel;
            GUI.DrawTexture(new Rect(clip.x, clip.y, fadeW, clip.height), tex);
            GUI.DrawTextureWithTexCoords(new Rect(clip.xMax - fadeW, clip.y, fadeW, clip.height), tex, new Rect(1f, 0f, -1f, 1f));
            GUI.color = keep;
        }

        public static void ResetStatics() { }
    }
}
