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
            DrawAt(at, size, alpha, Color.white);
        }

        // tint colors the star. The core stays white so the point still glows.
        public static void DrawAt(Vector2 at, float size, float alpha, Color tint)
        {
            if (alpha < 0.02f || size < 1f) return;
            float core = size * 0.22f;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(new Rect(at.x - core * 0.5f, at.y - core * 0.5f, core, core), Texture2D.whiteTexture);
            var spr = SpriteCatalog.Sparkle;
            if (spr != null && spr.texture != null)
            {
                GUI.color = new Color(tint.r, tint.g, tint.b, alpha);
                GUI.DrawTexture(new Rect(at.x - size * 0.5f, at.y - size * 0.5f, size, size), spr.texture, ScaleMode.ScaleToFit, true);
            }
            GUI.color = Color.white;
        }

        // Soft disc. SpriteCatalog.Glow is built once. No per-frame texture.
        public static void DrawGlow(Vector2 at, float size, Color tint)
        {
            if (tint.a < 0.02f || size < 1f) return;
            var glow = SpriteCatalog.Glow;
            if (glow == null || glow.texture == null) return;
            GUI.color = tint;
            GUI.DrawTexture(new Rect(at.x - size * 0.5f, at.y - size * 0.5f, size, size), glow.texture, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
        }

        // Pooled bits for the badger fight. Textures are built once. Tick is idempotent
        // per frame so Layout and Repaint cannot step the pool twice. No per-frame allocs.
        public const int Spark = 0;
        public const int GlowBit = 1;
        public const int Dust = 2;
        public const int Honey = 3;
        public const int Snow = 4;

        const int Cap = 80;
        struct Bit
        {
            public byte On;
            public byte Kind;
            public float X, Y, Vx, Vy, Age, Life, Size, Grav, R, G, B;
        }

        static Bit[] _bits;
        static int _cursor;
        static int _tickFrame = -1;
        static int _drawFrame = -1;
        static int _snowFrame = -1;
        static float _spin;
        static Texture2D _dustTex;
        static Texture2D _honeyTex;
        static readonly float[] Ang =
        {
            0.15f, 0.7f, 1.25f, 1.9f, 2.5f, 3.15f, 3.8f, 4.45f, 5.1f, 5.7f, 0.4f, 2.15f
        };

        static void Ensure()
        {
            if (_bits != null) return;
            _bits = new Bit[Cap];
            _dustTex = Disc(32, 0.62f, 0.48f, 0.30f, 2);
            _honeyTex = Disc(24, 1f, 0.78f, 0.14f, 3);
        }

        static Texture2D Disc(int n, float r, float g, float b, int power)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[n * n];
            float m = (n - 1) * 0.5f;
            byte rb = (byte)Mathf.RoundToInt(r * 255f);
            byte gb = (byte)Mathf.RoundToInt(g * 255f);
            byte bb = (byte)Mathf.RoundToInt(b * 255f);
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x - m) / m;
                float dy = (y - m) / m;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - d);
                for (int p = 0; p < power; p++) a *= a;
                px[y * n + x] = new Color32(rb, gb, bb, (byte)Mathf.RoundToInt(a * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        public static void Tick(float dt)
        {
            int f = Time.frameCount;
            if (f == _tickFrame) return;
            _tickFrame = f;
            if (_bits == null) return;
            if (dt < 0f) dt = 0f;
            else if (dt > 0.05f) dt = 0.05f;
            for (int i = 0; i < Cap; i++)
            {
                if (_bits[i].On == 0) continue;
                _bits[i].Age += dt;
                if (_bits[i].Age >= _bits[i].Life)
                {
                    _bits[i].On = 0;
                    continue;
                }
                _bits[i].Vy += _bits[i].Grav * dt;
                _bits[i].X += _bits[i].Vx * dt;
                _bits[i].Y += _bits[i].Vy * dt;
            }
        }

        public static void DrawBits()
        {
            if (_bits == null) return;
            int f = Time.frameCount;
            if (_drawFrame == f) return;
            _drawFrame = f;
            var spark = SpriteCatalog.Sparkle;
            var glow = SpriteCatalog.Glow;
            Texture sparkT = spark != null ? spark.texture : null;
            Texture glowT = glow != null ? glow.texture : null;
            for (int i = 0; i < Cap; i++)
            {
                if (_bits[i].On == 0) continue;
                float life = _bits[i].Life;
                float u = life <= 0.001f ? 1f : _bits[i].Age / life;
                float a = (1f - u) * (1f - u * 0.35f);
                if (a < 0.03f) continue;
                float sz = _bits[i].Size;
                byte kind = _bits[i].Kind;
                // Snow is the freeze page only. DrawDrift paints it. A fight
                // pass must not pick the flakes up off the shared pool.
                if (kind == Snow) continue;
                if (kind == Dust) sz *= 1f + u * 0.85f;
                else if (kind == Honey) sz *= 1f - u * 0.22f;
                Texture tex = kind == Dust ? _dustTex : kind == Honey ? _honeyTex : kind == GlowBit ? glowT : sparkT;
                if (tex == null) continue;
                GUI.color = new Color(_bits[i].R, _bits[i].G, _bits[i].B, a);
                float h = sz * 0.5f;
                GUI.DrawTexture(new Rect(_bits[i].X - h, _bits[i].Y - h, sz, sz), tex, ScaleMode.ScaleToFit, true);
            }
            GUI.color = Color.white;
        }

        public static void DustPuff(Vector2 at, int count)
        {
            Burst(Dust, at, count, 78f, 22f, 0.40f, 240f, 0.72f, 0.58f, 0.36f, true);
        }

        public static void HoneySplash(Vector2 at, int count)
        {
            Burst(Honey, at, count, 130f, 16f, 0.48f, 320f, 1f, 0.78f, 0.16f, false);
        }

        public static void StreakSparks(Vector2 at, int count)
        {
            Burst(Spark, at, count, 160f, 18f, 0.28f, 40f, 1f, 0.94f, 0.62f, false);
        }

        // Charge-up: a soft glow plus a couple of sparks already moving around `at`.
        public static void ChargeSwirl(Vector2 at, float radius)
        {
            if (radius < 8f) radius = 18f;
            for (int k = 0; k < 2; k++)
            {
                float a = _spin + k * 2.15f;
                float c = Mathf.Cos(a);
                float s = Mathf.Sin(a);
                Emit(Spark, at.x + c * radius * 0.42f, at.y + s * radius * 0.42f,
                    -s * 86f, c * 86f, 15f, 0.30f, 0f, 1f, 0.95f, 0.68f);
            }
            _spin += 0.65f;
            if (_spin > 6.283f) _spin -= 6.283f;
            Emit(GlowBit, at.x, at.y, 0f, -6f, radius * 1.35f, 0.22f, 0f, 1f, 0.84f, 0.32f);
        }

        public static void SlamBurst(Vector2 at)
        {
            StreakSparks(at, 8);
            HoneySplash(at, 4);
            Emit(GlowBit, at.x, at.y, 0f, 0f, 54f, 0.18f, 0f, 1f, 0.92f, 0.55f);
        }

        public static void Celebrate(Vector2 at)
        {
            HoneySplash(at, 12);
            Burst(Spark, at, 10, 110f, 26f, 0.55f, 30f, 1f, 0.97f, 0.74f, false);
            Emit(GlowBit, at.x, at.y, 0f, -12f, 78f, 0.42f, 0f, 1f, 0.88f, 0.36f);
        }

        // A few slow flakes inside `area`. Tops the pool up to `want` once a frame.
        // Tick moves them. DrawDrift paints only these, so a garden page does not
        // dump fight dust. GUI y grows downward.
        public static void DriftSnow(Rect area, int want)
        {
            if (area.width < 8f || area.height < 8f) return;
            int f = Time.frameCount;
            if (f == _snowFrame) return;
            _snowFrame = f;
            if (want < 1) return;
            if (want > 8) want = 8;
            Ensure();
            int live = 0;
            for (int i = 0; i < Cap; i++)
            {
                if (_bits[i].On == 0 || _bits[i].Kind != Snow) continue;
                live++;
            }
            int need = want - live;
            if (need < 1) return;
            for (int k = 0; k < need; k++)
            {
                float a = Ang[(_cursor + k) % Ang.Length];
                float ux = Mathf.Repeat(0.07f + a * 0.11f + k * 0.17f, 1f);
                float uy = Mathf.Repeat(0.13f + a * 0.19f + k * 0.29f, 1f);
                float x = area.x + ux * area.width;
                float y = area.y + uy * area.height;
                float speed = area.height * (0.12f + 0.04f * (k % 3));
                float sway = (ux - 0.5f) * 36f;
                float size = 11f + (k % 3) * 4f;
                Emit(Snow, x, y, sway, speed, size, 6.4f, 6f, 0.90f, 0.97f, 1f);
            }
        }

        public static void DrawDrift(float alpha)
        {
            if (alpha < 0.02f || _bits == null) return;
            var spark = SpriteCatalog.Sparkle;
            Texture tex = spark != null ? spark.texture : null;
            if (tex == null) return;
            float t = Time.unscaledTime;
            for (int i = 0; i < Cap; i++)
            {
                if (_bits[i].On == 0 || _bits[i].Kind != Snow) continue;
                float life = _bits[i].Life;
                float u = life <= 0.001f ? 1f : _bits[i].Age / life;
                float a = (1f - u) * (0.55f + 0.45f * (0.5f + 0.5f * Mathf.Sin(t * 2.4f + i)));
                a *= alpha;
                if (a < 0.03f) continue;
                float sz = _bits[i].Size * (0.88f + 0.12f * Mathf.Sin(t * 1.7f + i * 0.6f));
                float h = sz * 0.5f;
                GUI.color = new Color(_bits[i].R, _bits[i].G, _bits[i].B, a);
                GUI.DrawTexture(new Rect(_bits[i].X - h, _bits[i].Y - h, sz, sz), tex, ScaleMode.ScaleToFit, true);
            }
            GUI.color = Color.white;
        }

        static void Burst(int kind, Vector2 at, int count, float speed, float size, float life, float gravity,
            float r, float g, float b, bool rise)
        {
            if (count < 1) return;
            if (count > 16) count = 16;
            for (int k = 0; k < count; k++)
            {
                float a = Ang[(_cursor + k) % Ang.Length];
                float sp = speed * (0.45f + 0.55f * ((k * 3) % 5) / 4f);
                float vx = Mathf.Cos(a) * sp;
                float vy = Mathf.Sin(a) * sp;
                if (rise) vy = -Mathf.Abs(vy);
                else if (kind == Honey) vy = Mathf.Abs(vy) * 0.65f + speed * 0.25f;
                Emit(kind, at.x, at.y, vx, vy, size * (0.75f + 0.25f * (k & 1)), life, gravity, r, g, b);
            }
        }

        static void Emit(int kind, float x, float y, float vx, float vy, float size, float life, float grav,
            float r, float g, float b)
        {
            Ensure();
            int i = _cursor;
            if ((uint)i >= (uint)Cap) i = 0;
            _cursor = i + 1;
            if (_cursor >= Cap) _cursor = 0;
            _bits[i].On = 1;
            _bits[i].Kind = (byte)kind;
            _bits[i].X = x;
            _bits[i].Y = y;
            _bits[i].Vx = vx;
            _bits[i].Vy = vy;
            _bits[i].Age = 0f;
            _bits[i].Life = life < 0.05f ? 0.05f : life;
            _bits[i].Size = size < 2f ? 2f : size;
            _bits[i].Grav = grav;
            _bits[i].R = r;
            _bits[i].G = g;
            _bits[i].B = b;
        }
    }
}
