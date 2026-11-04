using UnityEngine;

namespace FlockFive
{
    // b120 R2 (Readability): one fixed shape mark per bird color, worn on the bird's chest and
    // painted on that color's feeder, so flocks and feeders still match in grayscale or for
    // color-blind players. The mark is dark ink on a cream disc: a luminance contrast, not a hue.
    // Kits (bow/crown) stay flock identity; the mark only ever follows Color.
    public static class ColorMark
    {
        public enum Shape { Dot = 0, Triangle = 1, Diamond = 2, Ring = 3, Bars = 4 }

        public static Shape Of(BirdColor c)
        {
            switch (c)
            {
                case BirdColor.Ruby: return Shape.Dot;
                case BirdColor.Gold: return Shape.Triangle;
                case BirdColor.Teal: return Shape.Diamond;
                case BirdColor.Violet: return Shape.Ring;
                default: return Shape.Bars;
            }
        }

        // Fraction of the smaller body/feeder side the mark spans. Subtle, but >= ~12 px on phones.
        public const float BirdShare = 0.26f;
        public const float FeederShare = 0.30f;
        public const int TexSize = 64;
        public static readonly Color Ink = new Color(0.16f, 0.12f, 0.10f, 1f);
        public static readonly Color Cream = new Color(1f, 0.96f, 0.86f, 1f);

        static Sprite[] _cache;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache() => _cache = null;

        public static Sprite SpriteFor(BirdColor c)
        {
            if (_cache == null) _cache = new Sprite[5];
            int i = Mathf.Clamp((int)c, 0, 4);
            if (_cache[i] != null) return _cache[i];
            var tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color32[TexSize * TexSize];
            var shape = Of(c);
            for (int y = 0; y < TexSize; y++)
                for (int x = 0; x < TexSize; x++)
                {
                    // -1..1 centered sample.
                    float u = (x + 0.5f) / TexSize * 2f - 1f, v = (y + 0.5f) / TexSize * 2f - 1f;
                    px[y * TexSize + x] = Sample(shape, u, v);
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _cache[i] = Sprite.Create(tex, new Rect(0, 0, TexSize, TexSize), new Vector2(0.5f, 0.5f), TexSize);
            _cache[i].name = "ColorMark_" + c;
            return _cache[i];
        }

        // Ink coverage 0..1 of a shape at (u, v) in -1..1. Pure; the tests read it.
        public static float InkAt(Shape shape, float u, float v)
        {
            const float aa = 0.06f;
            float d; // signed distance-ish, negative inside
            switch (shape)
            {
                case Shape.Dot: d = Mathf.Sqrt(u * u + v * v) - 0.42f; break;
                case Shape.Ring: { float r = Mathf.Sqrt(u * u + v * v); d = Mathf.Abs(r - 0.40f) - 0.13f; break; }
                case Shape.Diamond: d = (Mathf.Abs(u) + Mathf.Abs(v)) - 0.58f; break;
                case Shape.Triangle:
                {
                    // Upright triangle, apex up.
                    float vy = v + 0.12f;
                    d = Mathf.Max(-vy - 0.36f, Mathf.Abs(u) * 0.866f + vy * 0.5f - 0.30f);
                    break;
                }
                default:
                {
                    // Two horizontal bars.
                    float bv = Mathf.Abs(Mathf.Abs(v) - 0.22f) - 0.11f;
                    d = Mathf.Max(bv, Mathf.Abs(u) - 0.46f);
                    break;
                }
            }
            return Mathf.Clamp01(0.5f - d / aa);
        }

        static Color32 Sample(Shape shape, float u, float v)
        {
            float r = Mathf.Sqrt(u * u + v * v);
            float disc = Mathf.Clamp01((0.94f - r) / 0.06f);
            if (disc <= 0f) return new Color32(0, 0, 0, 0);
            float ink = InkAt(shape, u, v);
            // Thin ink rim so the cream disc reads on light feathers too.
            float rim = Mathf.Clamp01(1f - Mathf.Abs(r - 0.86f) / 0.05f);
            ink = Mathf.Max(ink, rim);
            var col = Color.Lerp(Cream, Ink, ink);
            col.a = disc;
            return col;
        }

        // Mark size in local units for a sprite of this bounds size.
        public static float Size(Vector2 spriteSize, float share) => Mathf.Min(spriteSize.x, spriteSize.y) * share;
    }
}
