using System.Collections.Generic;
using UnityEngine;

namespace FlockFive
{
    /// <summary>
    /// Builds (and caches) a thick white "selected" border for a bird body or kit sprite.
    /// The glow is the sprite's silhouette grown outward: solid white for the first
    /// <see cref="SolidPx"/> source pixels, then fading to clear over <see cref="FadePx"/>.
    /// Built on a 4x-downsampled alpha mask so it's cheap, then drawn on a padded canvas at the same
    /// scale and pivot as the source sprite, one sorting step behind it.
    /// Needs the source texture to be Read/Write enabled (bird + kit sprites are).
    /// </summary>
    public static class BirdGlow
    {
        const int Down = 4;          // source px per glow px
        const float SolidPx = 30f;   // solid white border, in source px
        const float FadePx = 26f;    // soft falloff past the solid band, in source px

        static readonly Dictionary<(Sprite, int), Sprite> _cache = new Dictionary<(Sprite, int), Sprite>();

        /// <param name="srcPxPerBodyPx">How many of this sprite's pixels span one body-sprite
        /// pixel on screen (1 for the body; larger for the kit, whose pixels are drawn smaller),
        /// so the border is the same thickness on body and kit.</param>
        public static Sprite For(Sprite src, float srcPxPerBodyPx = 1f)
        {
            if (src == null) return null;
            var key = (src, Mathf.RoundToInt(srcPxPerBodyPx * 100f));
            if (_cache.TryGetValue(key, out var hit)) return hit;
            Sprite built = null;
            try { built = Build(src, Mathf.Max(0.05f, srcPxPerBodyPx)); }
            catch (UnityException) { built = null; } // texture not readable: no glow
            _cache[key] = built;
            return built;
        }

        static Sprite Build(Sprite src, float k)
        {
            var tex = src.texture;
            if (tex == null) return null;
            var r = src.textureRect;
            int sw = Mathf.RoundToInt(r.width), sh = Mathf.RoundToInt(r.height);
            var px = tex.GetPixels32(); // throws UnityException if not readable
            int tw = tex.width;
            int ox = Mathf.RoundToInt(r.x), oy = Mathf.RoundToInt(r.y);
            float solid = SolidPx * k / Down, fade = FadePx * k / Down; // in glow px
            int pad = Mathf.CeilToInt(solid + fade) + 2;                 // grow canvas so the rim never clips
            int iw = Mathf.Max(1, Mathf.CeilToInt(sw / (float)Down));
            int ih = Mathf.Max(1, Mathf.CeilToInt(sh / (float)Down));
            int w = iw + pad * 2, h = ih + pad * 2;

            // Downsample: a glow px is "inside" if any source px in its block is mostly opaque.
            const float Inf = 1e6f;
            var d = new float[w * h];
            for (int i = 0; i < d.Length; i++) d[i] = Inf;
            for (int y = 0; y < ih; y++)
            for (int x = 0; x < iw; x++)
            {
                bool inside = false;
                for (int by = 0; by < Down && !inside; by++)
                {
                    int sy = y * Down + by; if (sy >= sh) break;
                    int row = (oy + sy) * tw + ox;
                    for (int bx = 0; bx < Down; bx++)
                    {
                        int sx = x * Down + bx; if (sx >= sw) break;
                        if (px[row + sx].a > 110) { inside = true; break; }
                    }
                }
                if (inside) d[(y + pad) * w + (x + pad)] = 0f;
            }

            // Two-pass chamfer distance (in glow px).
            const float A = 1f, B = 1.41421356f;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x; float v = d[i];
                if (x > 0) v = Mathf.Min(v, d[i - 1] + A);
                if (y > 0)
                {
                    v = Mathf.Min(v, d[i - w] + A);
                    if (x > 0) v = Mathf.Min(v, d[i - w - 1] + B);
                    if (x < w - 1) v = Mathf.Min(v, d[i - w + 1] + B);
                }
                d[i] = v;
            }
            for (int y = h - 1; y >= 0; y--)
            for (int x = w - 1; x >= 0; x--)
            {
                int i = y * w + x; float v = d[i];
                if (x < w - 1) v = Mathf.Min(v, d[i + 1] + A);
                if (y < h - 1)
                {
                    v = Mathf.Min(v, d[i + w] + A);
                    if (x < w - 1) v = Mathf.Min(v, d[i + w + 1] + B);
                    if (x > 0) v = Mathf.Min(v, d[i + w - 1] + B);
                }
                d[i] = v;
            }

            var outPx = new Color32[w * h];
            for (int i = 0; i < outPx.Length; i++)
            {
                float t = d[i] <= solid ? 1f : 1f - Mathf.Clamp01((d[i] - solid) / fade);
                t = t * t * (3f - 2f * t);
                outPx[i] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(t * 255f));
            }

            var gt = new Texture2D(w, h, TextureFormat.RGBA32, false);
            gt.name = src.name + "_glow";
            gt.wrapMode = TextureWrapMode.Clamp;
            gt.filterMode = FilterMode.Bilinear;
            gt.SetPixels32(outPx);
            gt.Apply(false, true);

            float scale = iw / (float)sw;                        // glow px per source px
            float ppu = src.pixelsPerUnit * scale;
            var pivot = new Vector2((src.pivot.x * scale + pad) / w, (src.pivot.y * scale + pad) / h);
            var s = Sprite.Create(gt, new Rect(0, 0, w, h), pivot, ppu, 0, SpriteMeshType.FullRect);
            s.name = gt.name;
            return s;
        }
    }
}
