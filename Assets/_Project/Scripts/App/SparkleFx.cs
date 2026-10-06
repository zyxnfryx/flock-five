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

        // Twinkles around / over a rect (seats fall inside it too). gold = warm celebration tint.
        // For a card or pop-up whose inside must stay clean, use DrawBorder.
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

        // Border seats: fraction of the way round the rect, clockwise from the top-left
        // corner. Spread so no two sides bunch up; phases keep neighbours out of step.
        static readonly float[] Rim = { 0.035f, 0.155f, 0.275f, 0.395f, 0.53f, 0.655f, 0.78f, 0.905f };
        static readonly float[] RimPh = { 0.2f, 1.4f, 2.5f, 3.6f, 4.7f, 5.6f, 0.85f, 3.1f };

        // Twinkles that live ONLY on a frame. Each sparkle is centred on the rect's outer
        // edge and its size is capped just under twice the frame thickness, so it may reach
        // out into the air but never past the frame onto the panel, copy or numerals inside.
        // border = frame thickness (outer edge to the content it must not touch).
        // round = seats on the ellipse inscribed in r (discs, chips, medals).
        public static void DrawBorder(Rect r, float border, float alpha, bool gold = false, float sizeFrac = 0.12f, bool round = false)
        {
            if (alpha < 0.02f || r.width < 4f || r.height < 4f || border < 0.5f) return;
            var spr = SpriteCatalog.Sparkle;
            var tex = spr != null ? spr.texture : null;
            if (tex == null) return;

            float t = Time.unscaledTime;
            float cg = gold ? 0.97f : 0.98f;
            float cb = gold ? 0.82f : 0.94f;
            float baseSz = Mathf.Min(r.width, r.height) * sizeFrac;
            float cap = border * 1.8f;

            for (int i = 0; i < Rim.Length; i++)
            {
                float tw = 0.5f + 0.5f * Mathf.Sin(t * 2.2f + RimPh[i]);
                tw = tw * tw;
                if (tw < 0.12f) continue;
                float sz = Mathf.Min(baseSz * (1f + 0.35f * (i & 1)), cap);
                if (sz < 2f) continue;
                var p = round ? OnEllipse(r, Rim[i]) : OnPerimeter(r, Rim[i]);
                GUI.color = new Color(1f, cg, cb, tw * alpha);
                GUI.DrawTexture(new Rect(p.x - sz * 0.5f, p.y - sz * 0.5f, sz, sz), tex, ScaleMode.ScaleToFit, true);
            }
            GUI.color = Color.white;
        }

        // Point on the rect's outline, u in [0,1) clockwise from the top-left corner.
        static Vector2 OnPerimeter(Rect r, float u)
        {
            float w = r.width;
            float h = r.height;
            float d = Mathf.Repeat(u, 1f) * 2f * (w + h);
            if (d < w) return new Vector2(r.x + d, r.y);
            d -= w;
            if (d < h) return new Vector2(r.xMax, r.y + d);
            d -= h;
            if (d < w) return new Vector2(r.xMax - d, r.yMax);
            d -= w;
            return new Vector2(r.x, r.yMax - d);
        }

        // Point on the inscribed ellipse, u in [0,1) clockwise from 12 o'clock.
        static Vector2 OnEllipse(Rect r, float u)
        {
            float a = Mathf.Repeat(u, 1f) * Mathf.PI * 2f;
            return new Vector2(r.center.x + Mathf.Sin(a) * r.width * 0.5f, r.center.y - Mathf.Cos(a) * r.height * 0.5f);
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
