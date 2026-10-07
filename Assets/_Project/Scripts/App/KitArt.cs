using UnityEngine;

namespace FlockFive
{
    // Painterly stand-ins for accessories that have no sprite yet.
    // Crown, bow, and bowtie stay the imported art. These match that
    // thick cream edge and soft fill so a second set reads at perch size.
    public static class KitArt
    {
        const int N = 256;
        const float Ppu = 200f;

        static Sprite _hat, _flower, _shades, _beanie;

        public static Sprite TopHat => _hat != null ? _hat : (_hat = Bake(DrawHat, "kit-tophat"));
        public static Sprite Flower => _flower != null ? _flower : (_flower = Bake(DrawFlower, "kit-flower"));
        public static Sprite Shades => _shades != null ? _shades : (_shades = Bake(DrawShades, "kit-shades"));
        public static Sprite Beanie => _beanie != null ? _beanie : (_beanie = Bake(DrawBeanie, "kit-beanie"));

        static Sprite Bake(System.Action<Color[]> draw, string name)
        {
            var pix = new Color[N * N];
            draw(pix);
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = name,
                hideFlags = HideFlags.HideAndDontSave
            };
            tex.SetPixels(pix);
            tex.Apply();
            var spr = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), Ppu);
            spr.name = name;
            spr.hideFlags = HideFlags.HideAndDontSave;
            return spr;
        }

        static void DrawHat(Color[] pix)
        {
            var edge = new Color(0.96f, 0.91f, 0.78f, 1f);
            var felt = new Color(0.18f, 0.16f, 0.22f, 1f);
            var feltHi = new Color(0.34f, 0.32f, 0.40f, 1f);
            var band = new Color(0.62f, 0.18f, 0.28f, 1f);
            var buckle = new Color(0.93f, 0.78f, 0.38f, 1f);
            Blob(pix, 128f, 78f, 108f, 22f, edge, felt);
            Round(pix, 128f, 148f, 52f, 78f, edge, felt);
            Blob(pix, 128f, 168f, 28f, 18f, edge, feltHi);
            Blob(pix, 128f, 108f, 56f, 14f, edge, band);
            Blob(pix, 128f, 108f, 10f, 10f, edge, buckle);
        }

        static void DrawBeanie(Color[] pix)
        {
            var edge = new Color(0.98f, 0.94f, 0.88f, 1f);
            var wool = new Color(0.45f, 0.62f, 0.78f, 1f);
            var woolHi = new Color(0.72f, 0.84f, 0.92f, 1f);
            var fold = new Color(0.30f, 0.46f, 0.64f, 1f);
            var pom = new Color(0.93f, 0.78f, 0.38f, 1f);
            Blob(pix, 128f, 132f, 78f, 70f, edge, wool);
            Blob(pix, 108f, 158f, 26f, 22f, edge, woolHi);
            Blob(pix, 128f, 86f, 84f, 26f, edge, fold);
            Blob(pix, 128f, 198f, 22f, 18f, edge, pom);
            Rib(pix, 128f, 112f, 70f, new Color(0.96f, 0.94f, 0.90f, 0.55f));
        }

        static void DrawFlower(Color[] pix)
        {
            var edge = new Color(0.99f, 0.95f, 0.90f, 1f);
            var petal = new Color(0.93f, 0.45f, 0.58f, 1f);
            var petalHi = new Color(0.99f, 0.72f, 0.78f, 1f);
            var heart = new Color(0.95f, 0.78f, 0.28f, 1f);
            var leaf = new Color(0.42f, 0.62f, 0.38f, 1f);
            Blob(pix, 168f, 96f, 28f, 16f, edge, leaf);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI * 2f / 6f - 0.4f;
                float x = 128f + Mathf.Cos(a) * 46f;
                float y = 128f + Mathf.Sin(a) * 46f;
                Blob(pix, x, y, 32f, 22f, edge, petal);
                Blob(pix, x + Mathf.Cos(a) * 8f, y + Mathf.Sin(a) * 8f, 12f, 8f, petalHi, petalHi);
            }
            Blob(pix, 128f, 128f, 24f, 24f, edge, heart);
            Dot(pix, 120f, 136f, 5f, new Color(1f, 0.96f, 0.82f, 0.9f));
        }

        static void DrawShades(Color[] pix)
        {
            var edge = new Color(0.96f, 0.91f, 0.78f, 1f);
            var lens = new Color(0.12f, 0.16f, 0.22f, 1f);
            var shine = new Color(0.78f, 0.86f, 0.92f, 0.85f);
            var arm = new Color(0.28f, 0.24f, 0.22f, 1f);
            Blob(pix, 46f, 128f, 34f, 8f, edge, arm);
            Blob(pix, 210f, 128f, 34f, 8f, edge, arm);
            Blob(pix, 86f, 120f, 42f, 26f, edge, lens);
            Blob(pix, 170f, 120f, 42f, 26f, edge, lens);
            Blob(pix, 128f, 128f, 16f, 7f, edge, arm);
            Blob(pix, 74f, 132f, 10f, 6f, shine, shine);
            Blob(pix, 158f, 132f, 10f, 6f, shine, shine);
        }

        static void Rib(Color[] pix, float cx, float cy, float rx, Color col)
        {
            for (int i = -2; i <= 2; i++)
                Stroke(pix, cx - rx * 0.55f, cy + i * 14f, cx + rx * 0.55f, cy + i * 14f + 10f, 2.2f, col);
        }

        static void Round(Color[] pix, float cx, float cy, float rx, float ry, Color edge, Color fill)
        {
            Blob(pix, cx, cy, rx, ry, edge, fill);
            // Flatten the underside so the crown of the hat meets the brim.
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - ry));
            int y1 = Mathf.Min(N - 1, Mathf.CeilToInt(cy - ry * 0.15f));
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - rx));
            int x1 = Mathf.Min(N - 1, Mathf.CeilToInt(cx + rx));
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float u = (x - cx) / rx;
                if (u * u > 1f) continue;
                pix[y * N + x] = fill;
            }
        }

        static void Blob(Color[] pix, float cx, float cy, float rx, float ry, Color edge, Color fill)
        {
            Paint(pix, cx, cy, rx * 1.16f, ry * 1.18f, edge);
            Paint(pix, cx, cy, rx, ry, fill);
        }

        static void Dot(Color[] pix, float cx, float cy, float r, Color col) =>
            Paint(pix, cx, cy, r, r, col);

        static void Paint(Color[] pix, float cx, float cy, float rx, float ry, Color col)
        {
            if (rx < 0.5f || ry < 0.5f) return;
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - rx - 1f));
            int x1 = Mathf.Min(N - 1, Mathf.CeilToInt(cx + rx + 1f));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - ry - 1f));
            int y1 = Mathf.Min(N - 1, Mathf.CeilToInt(cy + ry + 1f));
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float u = (x - cx) / rx;
                float v = (y - cy) / ry;
                float d = u * u + v * v;
                if (d > 1.08f) continue;
                float a = d > 0.86f ? Mathf.SmoothStep(1f, 0f, (d - 0.86f) / 0.22f) : 1f;
                Blend(pix, x, y, new Color(col.r, col.g, col.b, col.a * a));
            }
        }

        static void Stroke(Color[] pix, float x0, float y0, float x1, float y1, float thick, Color col)
        {
            int steps = Mathf.CeilToInt(Vector2.Distance(new Vector2(x0, y0), new Vector2(x1, y1)));
            for (int i = 0; i <= steps; i++)
            {
                float u = i / (float)Mathf.Max(1, steps);
                Paint(pix, Mathf.Lerp(x0, x1, u), Mathf.Lerp(y0, y1, u), thick, thick * 0.7f, col);
            }
        }

        static void Blend(Color[] pix, int x, int y, Color c)
        {
            int i = y * N + x;
            var d = pix[i];
            float a = c.a + d.a * (1f - c.a);
            if (a <= 0.001f) return;
            pix[i] = new Color(
                (c.r * c.a + d.r * d.a * (1f - c.a)) / a,
                (c.g * c.a + d.g * d.a * (1f - c.a)) / a,
                (c.b * c.a + d.b * d.a * (1f - c.a)) / a,
                a);
        }
    }
}
