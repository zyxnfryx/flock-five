using System.Collections.Generic;
using UnityEngine;

namespace FlockFive
{
    // One timeline for the painterly ostrich. The view plays these frames.
    // Calm play keeps the bob and the blink and skips the ruffle flourish.
    public enum OstrichAct
    {
        Idle = 0,
        Peck = 1,
        Hit1 = 2,
        Hit2 = 3,
        Daze = 4,
        Flee = 5,
        Strut = 6,
        Honk = 7,
        Shake = 8,
    }

    public struct OstrichSample
    {
        public float Bob;
        public float Tilt;
        public float Lean;
        public float Shake;
        public float Wind;
        public float Strike;
        public float Blink;
        public float Ruffle;
        public float Beak;
        public float Step;
        public float Spread;
        public int Damage;
        public float Flee;
        public float Dip;
    }

    public static class OstrichAnim
    {
        public const int IdleFrames = 16;
        public const int PeckFrames = 12;
        public const int Hit1Frames = 8;
        public const int Hit2Frames = 8;
        public const int DazeFrames = 10;
        public const int FleeFrames = 14;
        public const int StrutFrames = 10;
        public const int HonkFrames = 8;
        public const int ShakeFrames = 10;

        public static int Count(OstrichAct act)
        {
            switch (act)
            {
                case OstrichAct.Peck: return PeckFrames;
                case OstrichAct.Hit1: return Hit1Frames;
                case OstrichAct.Hit2: return Hit2Frames;
                case OstrichAct.Daze: return DazeFrames;
                case OstrichAct.Flee: return FleeFrames;
                case OstrichAct.Strut: return StrutFrames;
                case OstrichAct.Honk: return HonkFrames;
                case OstrichAct.Shake: return ShakeFrames;
                default: return IdleFrames;
            }
        }

        public static int TotalFrames()
        {
            int n = 0;
            for (int i = 0; i <= (int)OstrichAct.Shake; i++)
                n += Count((OstrichAct)i);
            return n;
        }

        public static bool BlinkFrame(int frame) => frame == 7 || frame == 8;

        public static bool RuffleFrame(int frame) => frame >= 10 && frame <= 15;

        public static bool WindFrame(int frame) => frame <= 4;

        public static bool StrikeFrame(int frame) => frame >= 5 && frame <= 7;

        public static OstrichSample At(OstrichAct act, int frame, bool calm)
        {
            int n = Count(act);
            if (n < 1) n = 1;
            if (frame < 0) frame = 0;
            if (frame >= n) frame = n - 1;
            float u = frame / (float)(n - 1 > 0 ? n - 1 : 1);
            float wave = Mathf.Sin(u * Mathf.PI * 2f);
            var s = new OstrichSample();
            switch (act)
            {
                case OstrichAct.Peck:
                    s.Wind = frame <= 4 ? frame / 4f : Mathf.Max(0f, 1f - (frame - 4) / 4f);
                    s.Strike = frame >= 5 && frame <= 7 ? (frame - 4) / 3f : (frame > 7 ? Mathf.Max(0f, 1f - (frame - 7) / 4f) : 0f);
                    s.Beak = s.Strike;
                    s.Dip = s.Strike * 0.34f - s.Wind * 0.06f;
                    s.Tilt = -16f * s.Wind + 22f * s.Strike;
                    s.Lean = -8f * s.Wind + 6f * s.Strike;
                    break;
                case OstrichAct.Hit1:
                    s.Damage = 1;
                    s.Ruffle = 0.85f;
                    s.Lean = Mathf.Sin(u * Mathf.PI) * -14f;
                    s.Tilt = Mathf.Sin(u * Mathf.PI) * 12f;
                    s.Shake = Mathf.Sin(u * Mathf.PI * 3f) * 8f;
                    break;
                case OstrichAct.Hit2:
                    s.Damage = 2;
                    s.Ruffle = 1f;
                    s.Lean = Mathf.Sin(u * Mathf.PI) * -22f;
                    s.Tilt = Mathf.Sin(u * Mathf.PI) * 16f;
                    s.Shake = Mathf.Sin(u * Mathf.PI * 4f) * 11f;
                    s.Bob = Mathf.Sin(u * Mathf.PI) * -0.06f;
                    break;
                case OstrichAct.Daze:
                    s.Damage = 3;
                    s.Ruffle = 0.4f;
                    s.Lean = Mathf.Sin(u * Mathf.PI * 3f) * 16f;
                    s.Tilt = Mathf.Sin(u * Mathf.PI * 2f) * 14f;
                    s.Bob = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 3f)) * -0.05f;
                    s.Step = u;
                    break;
                case OstrichAct.Flee:
                    s.Damage = 3;
                    s.Spread = Mathf.Clamp01(u * 1.4f);
                    s.Flee = u;
                    s.Step = u * 3f;
                    s.Lean = 18f + 10f * u;
                    s.Tilt = -8f;
                    s.Bob = Mathf.Sin(u * Mathf.PI * 6f) * 0.06f;
                    break;
                case OstrichAct.Strut:
                    s.Step = u;
                    s.Bob = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 2f)) * 0.10f;
                    s.Tilt = -12f;
                    s.Lean = -6f;
                    s.Beak = 0.15f;
                    break;
                case OstrichAct.Honk:
                    s.Beak = Mathf.Sin(Mathf.Clamp01(u) * Mathf.PI);
                    s.Bob = s.Beak * 0.06f;
                    s.Tilt = -6f * s.Beak;
                    s.Ruffle = 0.25f * s.Beak;
                    break;
                case OstrichAct.Shake:
                    s.Shake = Mathf.Sin(u * Mathf.PI * 4f) * 20f;
                    s.Tilt = s.Shake * 0.35f;
                    break;
                default:
                    s.Bob = wave * 0.07f;
                    s.Tilt = Mathf.Sin(u * Mathf.PI * 2f + 0.8f) * 7f;
                    s.Lean = s.Tilt * 0.35f;
                    s.Blink = BlinkFrame(frame) ? 1f : (frame == 6 || frame == 9 ? 0.4f : 0f);
                    s.Ruffle = RuffleFrame(frame) ? Mathf.Sin((frame - 10) / 5f * Mathf.PI) : 0.04f;
                    s.Step = u * 0.15f;
                    break;
            }
            if (calm)
            {
                s.Bob *= 0.25f;
                s.Tilt *= 0.25f;
                s.Lean *= 0.25f;
                s.Shake *= 0.2f;
                s.Ruffle *= 0.15f;
            }
            return s;
        }
    }

    // Soft storybook ostrich in the sparrow palette: cream belly, chestnut
    // wing bars, a big glossy eye. Frames are baked once and reused.
    public static class OstrichPaint
    {
        public const int BodyW = 250;
        public const int BodyH = 280;
        public const int HeadW = 170;
        public const int HeadH = 136;
        public const float BodyPpu = 125f;
        public const float HeadPpu = 148f;

        struct Pix
        {
            public float R, G, B, A;
        }

        static readonly Dictionary<int, Sprite> _sprites = new Dictionary<int, Sprite>();
        static Sprite _neck;

        public static Sprite Body(OstrichAct act, int frame) => Bake(0, act, frame, BodyW, BodyH, BodyPpu, new Vector2(0.50f, 0.71f));

        // Pivot is the chin, where the neck meets the head.
        public static Sprite Head(OstrichAct act, int frame) => Bake(1, act, frame, HeadW, HeadH, HeadPpu, new Vector2(0.30f, 0.24f));

        public static Sprite Neck()
        {
            if (_neck != null) return _neck;
            const int w = 88;
            const int h = 36;
            var px = new Pix[w * h];
            Disc(px, w, h, 44f, 18f, 40f, 14f, new Color(0.94f, 0.82f, 0.68f, 1f), 0f);
            Disc(px, w, h, 44f, 21f, 28f, 6f, new Color(0.98f, 0.90f, 0.80f, 0.55f), 0f);
            _neck = Finish("ostrich-neck", px, w, h, 64f, new Vector2(0.5f, 0.5f));
            return _neck;
        }

        public static int OpaqueCount(Sprite sprite) => Count(sprite, false);

        public static int DarkCount(Sprite sprite) => Count(sprite, true);

        static int Count(Sprite sprite, bool dark)
        {
            if (sprite == null || sprite.texture == null) return 0;
            var px = sprite.texture.GetPixels32();
            int n = 0;
            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i];
                if (c.a < 48) continue;
                if (!dark) { n++; continue; }
                if (c.r < 55 && c.g < 55 && c.b < 55) n++;
            }
            return n;
        }

        static Sprite Bake(int part, OstrichAct act, int frame, int w, int h, float ppu, Vector2 pivot)
        {
            int n = OstrichAnim.Count(act);
            if (frame < 0) frame = 0;
            if (frame >= n) frame = n - 1;
            int key = part * 100000 + (int)act * 100 + frame;
            Sprite found;
            if (_sprites.TryGetValue(key, out found) && found != null) return found;
            var sample = OstrichAnim.At(act, frame, false);
            var px = new Pix[w * h];
            if (part == 0) DrawBody(px, w, h, sample);
            else DrawHead(px, w, h, sample, act);
            var sprite = Finish("ostrich-" + part + "-" + (int)act + "-" + frame, px, w, h, ppu, pivot);
            _sprites[key] = sprite;
            return sprite;
        }

        static void DrawBody(Pix[] px, int w, int h, OstrichSample s)
        {
            float fluff = s.Ruffle * 8f;
            float spread = s.Spread;
            // Tail fan grows out of the rump, behind the body.
            Blob(px, w, h, 78f, 132f, 22f, 16f, new Color(0.72f, 0.48f, 0.28f, 1f), 20f);
            for (int i = 0; i < 6; i++)
            {
                bool gap = s.Damage >= 2 && (i == 0 || i == 5);
                if (gap) continue;
                float ang = 128f - i * 14f;
                float len = 34f + spread * 10f;
                Disc(px, w, h, 70f, 140f, len, 7f, new Color(0.28f, 0.15f, 0.08f, 1f), ang);
                Disc(px, w, h, 74f, 142f, len * 0.62f, 4.2f, new Color(0.90f, 0.82f, 0.68f, 1f), ang);
            }
            // Chest, belly, back. The pivot sits on the neck root.
            Blob(px, w, h, 132f, 128f, 62f, 50f, new Color(0.58f, 0.34f, 0.18f, 1f), s.Lean * 0.08f);
            Blob(px, w, h, 138f, 116f, 48f, 36f, new Color(0.94f, 0.86f, 0.70f, 1f), 0f);
            Blob(px, w, h, 154f, 146f, 34f, 26f, new Color(0.76f, 0.48f, 0.26f, 1f), 12f);
            // Wing bars. Damage 2 and 3 leave gaps where feathers came out.
            int feathers = 7;
            for (int i = 0; i < feathers; i++)
            {
                bool gap = s.Damage >= 2 && (i == 1 || i == 3 || i == 5);
                if (s.Damage >= 3 && i == 2) gap = true;
                if (gap) continue;
                float y = 158f - i * 11f + fluff * Mathf.Sin(i * 1.3f);
                float ang = -24f - i * 5f - spread * 26f;
                float len = 48f + spread * 16f + fluff * 0.35f;
                Disc(px, w, h, 128f, y, len, 8.2f, new Color(0.26f, 0.14f, 0.08f, 1f), ang);
                Disc(px, w, h, 136f, y + 1f, len * 0.58f, 4.6f, new Color(0.93f, 0.86f, 0.72f, 1f), ang);
            }
            Blob(px, w, h, 158f, 92f, 24f, 16f, new Color(0.92f, 0.82f, 0.66f, 1f), 0f);
            DrawLeg(px, w, h, 112f, 88f, s.Step, false, s.Flee);
            DrawLeg(px, w, h, 158f, 84f, s.Step, true, s.Flee);
            // Neck root. The stretch of neck sprites meets this oval.
            Blob(px, w, h, 126f, 176f, 20f, 22f, new Color(0.93f, 0.80f, 0.66f, 1f), 80f);
            Blob(px, w, h, 125f, 198f, 14f, 16f, new Color(0.96f, 0.86f, 0.74f, 1f), 86f);
        }

        static void DrawLeg(Pix[] px, int w, int h, float x, float y, float step, bool back, float flee)
        {
            float swing = Mathf.Sin((step + (back ? 0.5f : 0f)) * Mathf.PI * 2f);
            float kick = swing * (10f + flee * 22f);
            float kneeX = x + kick * 0.28f;
            float kneeY = y - 36f;
            float footX = x + kick;
            float footY = 36f + Mathf.Max(0f, swing) * flee * 14f;
            var leg = new Color(0.62f, 0.44f, 0.28f, 1f);
            StampBone(px, w, h, x, y, kneeX, kneeY, 5.4f, leg);
            StampBone(px, w, h, kneeX, kneeY, footX, footY, 4.2f, leg);
            Disc(px, w, h, footX + 10f, footY, 14f, 3.6f, new Color(0.48f, 0.32f, 0.18f, 1f), -4f);
            Disc(px, w, h, footX + 20f, footY + 1.5f, 7f, 2.4f, new Color(0.36f, 0.22f, 0.12f, 1f), -8f);
        }

        static void StampBone(Pix[] px, int w, int h, float x0, float y0, float x1, float y1, float thick, Color c)
        {
            float dx = x1 - x0;
            float dy = y1 - y0;
            float len = Mathf.Sqrt(dx * dx + dy * dy);
            if (len < 1f) return;
            float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            Disc(px, w, h, (x0 + x1) * 0.5f, (y0 + y1) * 0.5f, len * 0.5f, thick, c, ang);
        }

        static void DrawHead(Pix[] px, int w, int h, OstrichSample s, OstrichAct act)
        {
            float open = s.Beak;
            // Chin pivot is (0.30, 0.24). Skull sits above it, beak points forward.
            Blob(px, w, h, 52f, 40f, 16f, 14f, new Color(0.93f, 0.80f, 0.66f, 1f), 70f);
            Blob(px, w, h, 86f, 74f, 34f, 28f, new Color(0.55f, 0.32f, 0.16f, 1f), 0f);
            Blob(px, w, h, 90f, 76f, 28f, 23f, new Color(0.96f, 0.88f, 0.74f, 1f), 0f);
            Disc(px, w, h, 70f, 96f, 10f, 7f, new Color(0.40f, 0.22f, 0.12f, 1f), -24f);
            Disc(px, w, h, 78f, 100f, 6f, 4f, new Color(0.28f, 0.16f, 0.09f, 1f), -10f);
            // Beak. Honk and the peck strike open the lower mandible.
            Disc(px, w, h, 124f, 66f, 32f, 7f, new Color(0.90f, 0.48f, 0.32f, 1f), -2f);
            Disc(px, w, h, 122f, 56f - open * 18f, 28f, 5.6f, new Color(0.70f, 0.30f, 0.22f, 1f), -4f - open * 22f);
            if (open > 0.25f)
                Disc(px, w, h, 118f, 60f - open * 8f, 16f, 4f + open * 7f, new Color(0.28f, 0.10f, 0.10f, 0.95f), 0f);
            Disc(px, w, h, 150f, 64f, 7f, 3.2f, new Color(0.42f, 0.16f, 0.12f, 1f), 0f);
            Disc(px, w, h, 108f, 70f, 2.2f, 1.6f, new Color(0.35f, 0.14f, 0.12f, 1f), 0f);
            // Eye. A blink replaces the glossy pupil with a thin lid.
            if (s.Blink > 0.8f)
            {
                Disc(px, w, h, 98f, 82f, 15f, 3.4f, new Color(0.18f, 0.09f, 0.06f, 1f), -6f);
                Disc(px, w, h, 98f, 85f, 13f, 3f, new Color(0.94f, 0.86f, 0.72f, 1f), -6f);
            }
            else if (act == OstrichAct.Daze || s.Damage >= 3)
            {
                Disc(px, w, h, 98f, 84f, 14f, 13f, new Color(0.97f, 0.95f, 0.90f, 1f), 0f);
                Disc(px, w, h, 98f, 84f, 12f, 3.4f, new Color(0.12f, 0.08f, 0.06f, 1f), 32f);
                Disc(px, w, h, 98f, 84f, 12f, 3.4f, new Color(0.12f, 0.08f, 0.06f, 1f), -32f);
                Disc(px, w, h, 103f, 89f, 2.4f, 2.4f, new Color(1f, 1f, 1f, 0.85f), 0f);
            }
            else
            {
                float squish = 1f - s.Blink * 0.45f;
                Disc(px, w, h, 98f, 84f, 15f, 14f * squish, new Color(0.98f, 0.96f, 0.92f, 1f), 0f);
                Disc(px, w, h, 100f, 82f, 8f, 8f * squish, new Color(0.08f, 0.05f, 0.04f, 1f), 0f);
                Disc(px, w, h, 104f, 87f, 2.8f, 2.8f, new Color(1f, 1f, 1f, 0.95f), 0f);
                Disc(px, w, h, 96f, 78f, 1.5f, 1.5f, new Color(1f, 1f, 1f, 0.7f), 0f);
            }
            // Brow and cheek. The brow sits low so the look is a little smug.
            Disc(px, w, h, 100f, 98f, 12f, 2.4f, new Color(0.32f, 0.16f, 0.09f, 1f), -12f);
            Disc(px, w, h, 112f, 66f, 8f, 4.5f, new Color(0.86f, 0.46f, 0.40f, 0.5f), 0f);
        }

        static void Blob(Pix[] px, int w, int h, float cx, float cy, float rx, float ry, Color c, float rotDeg)
        {
            var ink = new Color(0.28f, 0.15f, 0.08f, c.a);
            Disc(px, w, h, cx, cy, rx + 2.6f, ry + 2.4f, ink, rotDeg);
            Disc(px, w, h, cx, cy, rx, ry, c, rotDeg);
        }

        static void Disc(Pix[] px, int w, int h, float cx, float cy, float rx, float ry, Color c, float rotDeg)
        {
            if (c.a <= 0.01f || rx < 0.4f || ry < 0.4f) return;
            float rot = rotDeg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rot);
            float sin = Mathf.Sin(rot);
            float reach = rx + ry + 2f;
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - reach));
            int x1 = Mathf.Min(w - 1, Mathf.CeilToInt(cx + reach));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - reach));
            int y1 = Mathf.Min(h - 1, Mathf.CeilToInt(cy + reach));
            float irx = 1f / rx;
            float iry = 1f / ry;
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x + 0.5f - cx;
                    float dy = y + 0.5f - cy;
                    float lx = dx * cos + dy * sin;
                    float ly = -dx * sin + dy * cos;
                    float e = lx * lx * irx * irx + ly * ly * iry * iry;
                    if (e >= 1f) continue;
                    float edge = 1f - Mathf.Sqrt(e);
                    float a = edge * edge * (3f - 2f * edge);
                    if (edge > 0.22f) a = 0.55f + 0.45f * a;
                    a *= c.a;
                    float n = Hash01(x, y);
                    float shade = 0.90f + 0.14f * n;
                    Over(ref px[y * w + x], c.r * shade, c.g * shade, c.b * shade * (0.96f + 0.04f * n), a);
                }
            }
        }

        static void Over(ref Pix p, float r, float g, float b, float a)
        {
            float ia = 1f - a;
            p.R = r * a + p.R * ia;
            p.G = g * a + p.G * ia;
            p.B = b * a + p.B * ia;
            p.A = a + p.A * ia;
        }

        static float Hash01(int x, int y)
        {
            uint n = (uint)(x * 374761393 + y * 668265263);
            n = (n ^ (n >> 13)) * 1274126177u;
            return (n & 255) / 255f;
        }

        static Sprite Finish(string name, Pix[] px, int w, int h, float ppu, Vector2 pivot)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp
            };
            var raw = new Color32[px.Length];
            for (int i = 0; i < px.Length; i++)
            {
                var p = px[i];
                float a = p.A;
                if (a < 0.004f) { raw[i] = new Color32(0, 0, 0, 0); continue; }
                float inv = 1f / a;
                raw[i] = new Color32(
                    ToByte(p.R * inv),
                    ToByte(p.G * inv),
                    ToByte(p.B * inv),
                    ToByte(a));
            }
            tex.SetPixels32(raw);
            tex.Apply(false, false);
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), pivot, ppu, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        static byte ToByte(float v)
        {
            if (v <= 0f) return 0;
            if (v >= 1f) return 255;
            return (byte)(v * 255f + 0.5f);
        }
    }
}
