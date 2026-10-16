using UnityEngine;

namespace FlockFive
{
    // Fuzzy moth for Moth Guest. Not the bee sprite. Wings are wider than the
    // body, and the edge alpha is noisy so the silhouette reads as fuzz.
    public static class MothArt
    {
        public const int Size = 160;
        static Texture2D _tex;

        public static Texture2D Texture()
        {
            if (_tex != null) return _tex;
            var px = new Color32[Size * Size];
            float c = (Size - 1) * 0.5f;
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float nx = (x - c) / c;
                    float ny = (y - c) / c;
                    float wing = Wing(nx, ny);
                    float body = Body(nx, ny);
                    float a = Mathf.Max(wing, body);
                    if (a < 0.02f) continue;
                    // Wings were cream-on-cream. A dusty membrane and a spot keep the moth
                    // readable on the album plate without changing the alpha silhouette.
                    Color rgb = body > wing
                        ? new Color(0.40f, 0.28f, 0.16f, 1f)
                        : new Color(0.58f, 0.44f, 0.30f, 1f);
                    float spot = Mathf.Max(WingSpot(nx + 0.40f, ny - 0.02f), WingSpot(nx - 0.40f, ny - 0.02f));
                    if (wing > body && spot > 0f)
                        rgb = Color.Lerp(rgb, new Color(0.22f, 0.13f, 0.07f), spot * 0.9f);
                    float eye = Eye(nx, ny);
                    if (eye > 0f)
                    {
                        rgb = Color.Lerp(rgb, new Color(0.12f, 0.08f, 0.06f), eye);
                        a = Mathf.Max(a, eye);
                    }
                    px[y * Size + x] = new Color32(
                        (byte)Mathf.RoundToInt(rgb.r * 255f),
                        (byte)Mathf.RoundToInt(rgb.g * 255f),
                        (byte)Mathf.RoundToInt(rgb.b * 255f),
                        (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
                }
            }
            Antenna(px, c);
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = "MothGuest"
            };
            tex.SetPixels32(px);
            tex.Apply(false, false);
            _tex = tex;
            return tex;
        }

        public static Color32 Pixel(int x, int y)
        {
            var tex = Texture();
            if (tex == null || x < 0 || y < 0 || x >= Size || y >= Size)
                return new Color32(0, 0, 0, 0);
            return tex.GetPixel(x, y);
        }

        static float Wing(float nx, float ny)
        {
            float side = nx < 0f ? -1f : 1f;
            float ox = nx - side * 0.38f;
            float oy = ny - 0.08f;
            float e = (ox * ox) / 0.22f + (oy * oy) / 0.10f;
            float core = Mathf.Clamp01(1f - e);
            if (core <= 0f) return 0f;
            float edge = Mathf.Clamp01((1f - core) * 1.4f);
            float fuzz = 0.55f + 0.45f * Hash01(Mathf.RoundToInt(nx * 80f), Mathf.RoundToInt(ny * 80f));
            float a = Mathf.Lerp(0.82f, 0.82f * fuzz, edge);
            return core * a;
        }

        static float Body(float nx, float ny)
        {
            float e = (nx * nx) / 0.045f + ((ny + 0.05f) * (ny + 0.05f)) / 0.22f;
            float core = Mathf.Clamp01(1f - e);
            if (core <= 0f) return 0f;
            float edge = Mathf.Clamp01((1f - core) * 1.6f);
            float fuzz = 0.35f + 0.65f * Hash01(Mathf.RoundToInt(nx * 120f) + 3, Mathf.RoundToInt(ny * 120f) + 9);
            return Mathf.Lerp(1f, fuzz, edge * 0.85f);
        }

        static float WingSpot(float dx, float dy)
        {
            float e = (dx * dx) / 0.012f + (dy * dy) / 0.007f;
            return Mathf.Clamp01(1f - e);
        }

        static float Eye(float nx, float ny)
        {
            float dy = ny - 0.28f;
            float lx = nx + 0.045f;
            float rx = nx - 0.045f;
            float l = 1f - (lx * lx + dy * dy) / 0.0045f;
            float r = 1f - (rx * rx + dy * dy) / 0.0045f;
            return Mathf.Clamp01(Mathf.Max(l, r));
        }

        static void Antenna(Color32[] px, float c)
        {
            for (int i = 0; i < 28; i++)
            {
                float u = i / 27f;
                float y = c + c * (0.32f + u * 0.28f);
                float spread = c * (0.06f + u * 0.22f);
                Plot(px, c - spread, y, 0.45f);
                Plot(px, c + spread, y, 0.45f);
            }
        }

        static void Plot(Color32[] px, float x, float y, float a)
        {
            int ix = Mathf.RoundToInt(x);
            int iy = Mathf.RoundToInt(y);
            if (ix < 1 || iy < 1 || ix >= Size - 1 || iy >= Size - 1) return;
            byte alpha = (byte)Mathf.RoundToInt(a * 255f);
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int p = (iy + dy) * Size + (ix + dx);
                    var cur = px[p];
                    byte na = dx == 0 && dy == 0 ? alpha : (byte)(alpha / 3);
                    if (na <= cur.a) continue;
                    px[p] = new Color32(70, 52, 36, na);
                }
            }
        }

        static float Hash01(int x, int y)
        {
            int n = x * 374761393 + y * 668265263;
            n = (n << 13) ^ n;
            int m = (n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff;
            return m / 2147483647f;
        }
    }
}
