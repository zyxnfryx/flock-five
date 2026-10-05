using UnityEngine;

namespace FlockFive
{
    // Shared GUI sparkle / twinkle. SpriteCatalog.Sparkle (celebration glint) only —
    // never the bird-selection glow. Clock is Time.unscaledTime so twinkles stay
    // smooth under pause / timeScale 0. Static offset tables; no per-frame allocs.
    public static class SparkleFx
    {
        // Normalized offsets around a rect. Same seats StreakBadge used to own.
        static readonly float[] Ox = { -0.40f, 0.38f, 0.02f, -0.22f, 0.34f };
        static readonly float[] Oy = { -0.22f, -0.36f, -0.46f, 0.40f, 0.28f };
        static readonly float[] Ph = { 0.2f, 1.4f, 2.5f, 3.6f, 4.7f };

        // A few more seats for larger frames (reward board). Appended, not replacing.
        static readonly float[] OxWide = { -0.40f, 0.38f, 0.02f, -0.22f, 0.34f, -0.52f, 0.50f };
        static readonly float[] OyWide = { -0.22f, -0.36f, -0.46f, 0.40f, 0.28f, 0.06f, -0.12f };
        static readonly float[] PhWide = { 0.2f, 1.4f, 2.5f, 3.6f, 4.7f, 5.6f, 0.85f };

        // Twinkles around / over a rect. gold = warm celebration tint.
        public static void DrawAround(Rect r, float alpha, bool gold = false, float sizeFrac = 0.12f, bool wide = false)
        {
            if (alpha < 0.02f || r.width < 4f || r.height < 4f) return;
            var spr = SpriteCatalog.Sparkle;
            var tex = spr != null ? spr.texture : null;
            if (tex == null) return;

            float[] ox = wide ? OxWide : Ox;
            float[] oy = wide ? OyWide : Oy;
            float[] ph = wide ? PhWide : Ph;
            float t = Time.unscaledTime;
            float cr = gold ? 1f : 1f;
            float cg = gold ? 0.97f : 0.98f;
            float cb = gold ? 0.82f : 0.94f;
            float baseSz = Mathf.Min(r.width, r.height) * sizeFrac;
            if (baseSz < 2f) return;

            for (int i = 0; i < ox.Length; i++)
            {
                float tw = 0.5f + 0.5f * Mathf.Sin(t * 2.2f + ph[i]);
                tw = tw * tw;
                if (tw < 0.12f) continue;
                float sz = baseSz * (1f + 0.35f * (i & 1));
                float x = r.center.x + ox[i] * r.width - sz * 0.5f;
                float y = r.center.y + oy[i] * r.height - sz * 0.5f;
                GUI.color = new Color(cr, cg, cb, tw * alpha);
                GUI.DrawTexture(new Rect(x, y, sz, sz), tex, ScaleMode.ScaleToFit, true);
            }
            GUI.color = Color.white;
        }

        // Single point glint (badger eye, stamp flash): white core + sparkle sprite.
        // Core alone still reads if the sparkle sprite is missing.
        public static void DrawAt(Vector2 at, float size, float alpha)
        {
            if (alpha < 0.02f || size < 1f) return;
            float core = size * 0.22f;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(new Rect(at.x - core * 0.5f, at.y - core * 0.5f, core, core), Texture2D.whiteTexture);
            var spr = SpriteCatalog.Sparkle;
            if (spr != null && spr.texture != null)
                GUI.DrawTexture(new Rect(at.x - size * 0.5f, at.y - size * 0.5f, size, size), spr.texture, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
        }
    }
}
