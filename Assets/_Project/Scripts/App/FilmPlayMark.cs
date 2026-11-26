using UnityEngine;

namespace FlockFive
{
    // Short piece of black film tape: sprocket holes along both edges, a white
    // play arrow in the frame. Baked once. The reward and daily 2X buttons draw it.
    public static class FilmPlayMark
    {
        public const int Width = 356;
        public const int Height = 200;
        public const int Sprockets = 4;
        public const float Aspect = 356f / 200f;

        const float Margin = 0.03f;
        const float Radius = 0.09f;
        const float HoleH = 0.105f;
        const float HoleW = 0.062f;
        const float HoleCorner = 0.035f;
        const float HoleTop = 0.145f;
        const float HoleBot = 0.855f;
        const float ArrowBase = 0.36f;
        const float ArrowTip = 0.66f;
        const float ArrowHalf = 0.20f;

        static readonly float[] HoleU = { 0.16f, 0.387f, 0.613f, 0.84f };

        static readonly Color32 Black = new Color32(10, 10, 12, 255);
        static readonly Color32 White = new Color32(255, 255, 255, 255);

        static Color32[] _px;
        static Texture2D _tex;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            if (_tex != null)
            {
                if (Application.isPlaying) Object.Destroy(_tex);
                else Object.DestroyImmediate(_tex);
            }
            _tex = null;
            _px = null;
        }

        public static Color32[] Pixels()
        {
            if (_px != null) return _px;
            var px = new Color32[Width * Height];
            const int k = 3;
            float step = 1f / k;
            for (int y = 0; y < Height; y++)
            {
                int row = y * Width;
                for (int x = 0; x < Width; x++)
                {
                    float r = 0f, g = 0f, b = 0f, a = 0f;
                    for (int sy = 0; sy < k; sy++)
                    {
                        float v = (y + (sy + 0.5f) * step) / Height;
                        for (int sx = 0; sx < k; sx++)
                        {
                            float u = (x + (sx + 0.5f) * step) / Width;
                            var c = Cover(u, v);
                            r += c.r;
                            g += c.g;
                            b += c.b;
                            a += c.a;
                        }
                    }
                    float n = k * k;
                    px[row + x] = new Color32(
                        (byte)Mathf.RoundToInt(r / n),
                        (byte)Mathf.RoundToInt(g / n),
                        (byte)Mathf.RoundToInt(b / n),
                        (byte)Mathf.RoundToInt(a / n));
                }
            }
            _px = px;
            return px;
        }

        public static Texture2D Texture()
        {
            if (_tex != null) return _tex;
            var px = Pixels();
            var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = "FilmPlay"
            };
            tex.SetPixels32(px);
            tex.Apply(false, false);
            _tex = tex;
            return tex;
        }

        // 0 outside the tape, 1 black tape, 2 white (hole or arrow).
        static int CoverKind(float u, float v)
        {
            if (!InBody(u, v)) return 0;
            if (InHole(u, v) || InArrow(u, v)) return 2;
            return 1;
        }

        static Color32 Cover(float u, float v)
        {
            int k = CoverKind(u, v);
            if (k == 2) return White;
            if (k == 1) return Black;
            return new Color32(0, 0, 0, 0);
        }

        static bool InBody(float u, float v)
        {
            float radU = Radius / Aspect;
            float x0 = Margin, x1 = 1f - Margin, y0 = Margin, y1 = 1f - Margin;
            float ix0 = x0 + radU, ix1 = x1 - radU, iy0 = y0 + Radius, iy1 = y1 - Radius;
            if (u >= ix0 && u <= ix1 && v >= y0 && v <= y1) return true;
            if (u >= x0 && u <= x1 && v >= iy0 && v <= iy1) return true;
            float cx = u < ix0 ? ix0 : ix1;
            float cy = v < iy0 ? iy0 : iy1;
            float dx = (u - cx) * Aspect;
            float dy = v - cy;
            return dx * dx + dy * dy <= Radius * Radius;
        }

        static bool InHole(float u, float v)
        {
            for (int i = 0; i < HoleU.Length; i++)
            {
                if (InRoundRect(u, v, HoleU[i], HoleTop, HoleW, HoleH, HoleCorner)) return true;
                if (InRoundRect(u, v, HoleU[i], HoleBot, HoleW, HoleH, HoleCorner)) return true;
            }
            return false;
        }

        static bool InRoundRect(float u, float v, float cu, float cv, float w, float h, float rad)
        {
            float du = Mathf.Abs(u - cu);
            float dv = Mathf.Abs(v - cv);
            if (du > w * 0.5f || dv > h * 0.5f) return false;
            float radU = rad / Aspect;
            if (du <= w * 0.5f - radU || dv <= h * 0.5f - rad) return true;
            float dx = (du - (w * 0.5f - radU)) * Aspect;
            float dy = dv - (h * 0.5f - rad);
            return dx * dx + dy * dy <= rad * rad;
        }

        static bool InArrow(float u, float v)
        {
            if (u < ArrowBase || u > ArrowTip) return false;
            float t = (u - ArrowBase) / (ArrowTip - ArrowBase);
            float half = ArrowHalf * (1f - t);
            return Mathf.Abs(v - 0.50f) <= half;
        }

        public static int SprocketRuns(bool bottom)
        {
            var px = Pixels();
            int y = Mathf.Clamp(Mathf.FloorToInt((bottom ? HoleBot : HoleTop) * Height), 0, Height - 1);
            int runs = 0;
            bool on = false;
            int row = y * Width;
            for (int x = 0; x < Width; x++)
            {
                bool white = IsWhite(px[row + x]);
                if (white && !on) runs++;
                on = white;
            }
            return runs;
        }

        // Tall base on the left, tip on the right, sitting in the black frame.
        public static bool ArrowPointsRight()
        {
            return IsWhite(At(0.40f, 0.50f))
                && IsWhite(At(0.60f, 0.50f))
                && IsWhite(At(0.40f, 0.34f))
                && !IsWhite(At(0.60f, 0.32f))
                && !IsWhite(At(0.28f, 0.50f))
                && IsBlack(At(0.28f, 0.50f));
        }

        public static bool TapeBlack() => IsBlack(At(0.28f, 0.50f));

        public static bool CornerClear() => At(0.01f, 0.01f).a < 20;

        public static bool HolesClearOfArrow()
        {
            // A sprocket is white, the frame under it is black, the arrow below that is white.
            return IsWhite(At(HoleU[1], HoleTop))
                && IsBlack(At(HoleU[1], 0.26f))
                && IsWhite(At(0.50f, 0.50f));
        }

        static Color32 At(float u, float v)
        {
            var px = Pixels();
            int x = Mathf.Clamp(Mathf.RoundToInt(u * (Width - 1)), 0, Width - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(v * (Height - 1)), 0, Height - 1);
            return px[y * Width + x];
        }

        static bool IsWhite(Color32 c) => c.a > 200 && c.r > 220;

        static bool IsBlack(Color32 c) => c.a > 200 && c.r < 40;
    }
}
