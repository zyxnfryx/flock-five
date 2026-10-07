using UnityEngine;

namespace FlockFive
{
    // One juicy honeycomb. Card backs, badger tiles, and the badger meter all draw through here.
    // The hex mask, amber fill, gloss, and drips are baked once. A frame only stretches them.
    public static class HoneyArt
    {
        const int W = 96;
        const int H = 111;
        static Texture2D _mask;
        static Texture2D _fill;
        static Texture2D _gloss;
        static Texture2D _dripA;
        static Texture2D _dripB;
        static bool _warm;

        public static void Warm()
        {
            if (_warm) return;
            _warm = true;
            _mask = BakeMask();
            _fill = BakeFill();
            _gloss = BakeGloss();
            _dripA = BakeDrip(0);
            _dripB = BakeDrip(1);
        }

        public static Texture2D Mask()
        {
            Warm();
            return _mask;
        }

        public static Texture2D Fill()
        {
            Warm();
            return _fill;
        }

        // `hex` is the whole cell, drips included. `t` is seconds (inspect drip loop).
        // `s` is the screen scale (height/720) so the gloss stays a few pixels on a small phone.
        // juicy false is the spent / empty comb: the mask tinted by rim, no fill and no drip.
        // drips false keeps a tight grid (badger tiles, meter) from hanging into the next cell.
        public static void DrawJuicyCell(Rect hex, Color rim, float t, float s, int index = 0,
            bool juicy = true, bool animate = false, bool drips = true)
        {
            if (hex.width < 3f || hex.height < 3f) return;
            Warm();
            if (_mask == null) return;
            if (!juicy)
            {
                GUI.color = rim;
                GUI.DrawTexture(hex, _mask, ScaleMode.StretchToFill, true);
                GUI.color = Color.white;
                return;
            }

            float bodyH = drips ? hex.height * 0.78f : hex.height;
            var body = new Rect(hex.x, hex.y, hex.width, bodyH);
            float lift = body.height * 0.045f;
            GUI.color = new Color(0f, 0f, 0f, 0.30f * rim.a);
            GUI.DrawTexture(new Rect(body.x + lift * 0.35f, body.y + lift, body.width, body.height), _mask, ScaleMode.StretchToFill, true);
            GUI.color = rim;
            GUI.DrawTexture(body, _mask, ScaleMode.StretchToFill, true);

            float inset = Mathf.Min(body.width, body.height) * 0.065f;
            var inner = new Rect(body.x + inset, body.y + inset, body.width - inset * 2f, body.height - inset * 2f);
            GUI.color = new Color(1f, 1f, 1f, rim.a);
            if (_fill != null) GUI.DrawTexture(inner, _fill, ScaleMode.StretchToFill, true);
            if (_gloss != null)
            {
                float pad = Mathf.Max(0f, s) * 0.15f;
                var shine = new Rect(inner.x + pad, inner.y + pad, inner.width, inner.height);
                GUI.DrawTexture(shine, _gloss, ScaleMode.StretchToFill, true);
            }

            if (drips && bodyH < hex.height - 1f)
                DrawDrips(hex, body, t, index, rim.a, animate);
            GUI.color = Color.white;
        }

        static void DrawDrips(Rect hex, Rect body, float t, int index, float alpha, bool animate)
        {
            int kind = index % 3;
            if (kind == 2) return;
            int count = kind == 0 ? 2 : 1;
            float stretch = 1f;
            if (animate)
                stretch = 0.82f + 0.28f * (0.5f + 0.5f * Mathf.Sin(t * (Mathf.PI * 2f / 3f) + index * 0.7f));
            float room = hex.yMax - (body.yMax - body.height * 0.08f);
            if (room < 3f) return;
            for (int d = 0; d < count; d++)
            {
                var tex = d == 0 ? _dripA : _dripB;
                if (tex == null) continue;
                float w = body.width * (d == 0 ? 0.22f : 0.15f);
                float h = body.height * (d == 0 ? 0.34f : 0.26f) * stretch;
                if (h > room) h = room;
                if (h < 2f || w < 2f) continue;
                float nudge = ((index * 13 + d * 5) % 7 - 3) * body.width * 0.018f;
                float side = d == 0 ? -body.width * 0.10f : body.width * 0.14f;
                float x = body.center.x + side + nudge - w * 0.5f;
                float y = body.yMax - h * 0.22f;
                if (y + h > hex.yMax) y = hex.yMax - h;
                if (x < hex.x) x = hex.x;
                if (x + w > hex.xMax) x = hex.xMax - w;
                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.DrawTexture(new Rect(x, y, w, h), tex, ScaleMode.StretchToFill, true);
            }
        }

        static Texture2D BakeMask()
        {
            var px = new Color32[W * H];
            float a = W * 0.5f;
            float r = H * 0.5f;
            float k = r / (2f * a);
            float norm = Mathf.Sqrt(1f + k * k);
            for (int iy = 0; iy < H; iy++)
            {
                for (int ix = 0; ix < W; ix++)
                {
                    float x = Mathf.Abs(ix + 0.5f - a);
                    float y = Mathf.Abs(iy + 0.5f - r);
                    float d = Mathf.Min(a - x, (r - k * x - y) / norm);
                    float al = Mathf.Clamp01(d + 0.5f);
                    px[iy * W + ix] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(al * 255f));
                }
            }
            return Finish(px, W, H);
        }

        static Texture2D BakeFill()
        {
            var px = new Color32[W * H];
            float a = W * 0.5f;
            float r = H * 0.5f;
            float k = r / (2f * a);
            float norm = Mathf.Sqrt(1f + k * k);
            var deep = new Color(0.62f, 0.24f, 0.04f, 1f);
            var gold = new Color(1f, 0.84f, 0.28f, 1f);
            var glow = new Color(1f, 0.95f, 0.62f, 1f);
            for (int iy = 0; iy < H; iy++)
            {
                for (int ix = 0; ix < W; ix++)
                {
                    float x = Mathf.Abs(ix + 0.5f - a);
                    float y = Mathf.Abs(iy + 0.5f - r);
                    float d = Mathf.Min(a - x, (r - k * x - y) / norm);
                    float al = Mathf.Clamp01((d + 0.5f) / 1.4f);
                    if (al <= 0f)
                    {
                        px[iy * W + ix] = new Color32(0, 0, 0, 0);
                        continue;
                    }
                    // Texture y grows downward. GUI draws it top-down, so row 0 is the top (golden).
                    float up = 1f - iy / (float)(H - 1);
                    var c = Color.Lerp(deep, gold, Mathf.Clamp01(up * 1.05f));
                    float nx = (ix + 0.5f) / W - 0.5f;
                    float ny = (iy + 0.5f) / H - 0.42f;
                    float rad = Mathf.Sqrt(nx * nx + ny * ny);
                    c = Color.Lerp(c, glow, Mathf.Clamp01(0.45f - rad) * 0.55f);
                    c.a = al;
                    px[iy * W + ix] = (Color32)c;
                }
            }
            return Finish(px, W, H);
        }

        static Texture2D BakeGloss()
        {
            var px = new Color32[W * H];
            for (int iy = 0; iy < H; iy++)
            {
                for (int ix = 0; ix < W; ix++)
                {
                    float u = ix / (float)(W - 1);
                    float v = iy / (float)(H - 1);
                    float streak = Mathf.Exp(-Mathf.Pow((u - 0.30f) / 0.16f, 2f) - Mathf.Pow((v - 0.24f) / 0.07f, 2f));
                    float curve = Mathf.Exp(-Mathf.Pow((u - 0.22f - v * 0.15f) / 0.10f, 2f) - Mathf.Pow((v - 0.20f) / 0.05f, 2f));
                    float dot = Mathf.Exp(-Mathf.Pow((u - 0.38f) / 0.035f, 2f) - Mathf.Pow((v - 0.16f) / 0.030f, 2f));
                    float a = Mathf.Clamp01(streak * 0.55f + curve * 0.40f + dot * 0.85f);
                    byte al = (byte)Mathf.RoundToInt(a * 255f);
                    px[iy * W + ix] = new Color32(255, 255, 255, al);
                }
            }
            return Finish(px, W, H);
        }

        static Texture2D BakeDrip(int which)
        {
            const int DW = 40;
            const int DH = 64;
            var px = new Color32[DW * DH];
            float cx = DW * (which == 0 ? 0.50f : 0.46f);
            float bulge = which == 0 ? 0.34f : 0.26f;
            var deep = new Color(0.72f, 0.32f, 0.05f, 1f);
            var gold = new Color(1f, 0.86f, 0.34f, 1f);
            for (int iy = 0; iy < DH; iy++)
            {
                float v = iy / (float)(DH - 1);
                // Neck at the top, teardrop swelling toward the bottom.
                float neck = Mathf.Lerp(0.10f, bulge, Mathf.SmoothStep(0f, 1f, v));
                float rad = neck * (1f - 0.35f * Mathf.Pow(Mathf.Clamp01((v - 0.72f) / 0.28f), 2f));
                for (int ix = 0; ix < DW; ix++)
                {
                    float dx = (ix + 0.5f - cx) / DW;
                    float edge = rad - Mathf.Abs(dx);
                    float al = Mathf.Clamp01(edge * 10f);
                    if (al <= 0f) continue;
                    float up = 1f - v;
                    var c = Color.Lerp(deep, gold, up * 0.85f);
                    float hi = Mathf.Exp(-Mathf.Pow((dx + 0.04f) / 0.045f, 2f) - Mathf.Pow((v - 0.28f) / 0.08f, 2f));
                    c = Color.Lerp(c, Color.white, hi * 0.75f);
                    c.a = al;
                    px[iy * DW + ix] = (Color32)c;
                }
            }
            return Finish(px, DW, DH);
        }

        static Texture2D Finish(Color32[] px, int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }
    }
}
