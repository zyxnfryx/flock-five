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
                    s.Bob = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 2f)) * 0.12f;
                    s.Tilt = -18f;
                    s.Lean = -10f;
                    s.Beak = 0.15f;
                    break;
                case OstrichAct.Honk:
                    s.Beak = Mathf.Sin(Mathf.Clamp01(u) * Mathf.PI);
                    s.Bob = s.Beak * 0.06f;
                    s.Tilt = -6f * s.Beak;
                    s.Ruffle = 0.25f * s.Beak;
                    break;
                case OstrichAct.Shake:
                    s.Shake = Mathf.Sin(u * Mathf.PI * 4f) * 28f;
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

    // Painterly ostrich. Big dark body, white wing and tail plumes, a bare
    // pinkish-grey neck and a small head. Frames are baked once and reused.
    // The neck itself is a line ribbon in OstrichMark so a bend stays one tube.
    public static class OstrichPaint
    {
        public const int BodyW = 360;
        public const int BodyH = 520;
        public const int HeadW = 168;
        public const int HeadH = 128;
        public const float BodyPpu = 100f;
        public const float HeadPpu = 190f / 1.2f; // review: head ~20% bigger, same chin pivot
        // Neck root, and the chin where the ribbon meets the head. y up.
        public static readonly Vector2 BodyPivot = new Vector2(210f / BodyW, 430f / BodyH);
        public static readonly Vector2 HeadPivot = new Vector2(48f / HeadW, 34f / HeadH);

        static readonly Color Skin = new Color(0.90f, 0.56f, 0.60f, 1f); // pinker neck
        static readonly Color SkinHi = new Color(1.00f, 0.80f, 0.78f, 1f);
        static readonly Color SkinSh = new Color(0.60f, 0.30f, 0.36f, 1f);
        static readonly Color BodyC = new Color(0.12f, 0.10f, 0.13f, 1f); // darker plumes
        static readonly Color BodyHi = new Color(0.36f, 0.32f, 0.38f, 1f);
        static readonly Color BodyLo = new Color(0.035f, 0.025f, 0.045f, 1f);
        static readonly Color PlumeC = new Color(0.97f, 0.95f, 0.91f, 1f);
        static readonly Color PlumeSh = new Color(0.68f, 0.66f, 0.62f, 1f);
        static readonly Color Horn = new Color(0.86f, 0.52f, 0.48f, 1f);
        static readonly Color HornDk = new Color(0.50f, 0.32f, 0.30f, 1f);

        struct Pix
        {
            public float R, G, B, A;
        }

        static readonly Dictionary<int, Sprite> _sprites = new Dictionary<int, Sprite>();
        static Sprite _neck;
        static Texture2D _neckTex;

        public static Sprite Body(OstrichAct act, int frame) => Bake(0, act, frame, BodyW, BodyH, BodyPpu, BodyPivot);

        // Pivot is the chin, where the neck meets the head.
        public static Sprite Head(OstrichAct act, int frame) => Bake(1, act, frame, HeadW, HeadH, HeadPpu, HeadPivot);

        // Soft tube cross-section. U runs along the neck, V runs across it.
        public static Texture2D NeckTexture()
        {
            if (_neckTex != null) return _neckTex;
            const int w = 8;
            const int h = 32;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "ostrich-neck-tex",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var raw = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float v = (y + 0.5f) / h;
                float side = Mathf.Abs(v - 0.5f) * 2f;
                float shade = 0.58f + 0.50f * (1f - side * side);
                float edge = side > 0.72f ? (1f - side) / 0.28f : 1f;
                edge = edge * edge * (3f - 2f * edge);
                var col = Color.Lerp(SkinSh, SkinHi, Mathf.Clamp01(shade));
                byte a = ToByte(edge);
                byte r = ToByte(col.r * shade);
                byte g = ToByte(col.g * shade);
                byte b = ToByte(col.b * shade * 0.98f);
                for (int x = 0; x < w; x++)
                    raw[y * w + x] = new Color32(r, g, b, a);
            }
            tex.SetPixels32(raw);
            tex.Apply(false, false);
            _neckTex = tex;
            return tex;
        }

        public static Sprite Neck()
        {
            if (_neck != null) return _neck;
            var tex = NeckTexture();
            _neck = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 32f, 0, SpriteMeshType.FullRect);
            _neck.name = "ostrich-neck";
            _neck.hideFlags = HideFlags.HideAndDontSave;
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
            float ruf = s.Ruffle;
            float spr = s.Spread;
            int dmg = s.Damage;
            float step = s.Step;
            float flee = s.Flee;
            float pivotX = BodyPivot.x * w;
            float pivotY = BodyPivot.y * h;
            float cx = pivotX - 34f;
            float cy = pivotY - 102f;
            float rx = 116f;
            float ry = 104f;

            for (int i = 0; i < 6; i++)
            {
                if (PlumeGap(i, dmg, true)) continue;
                float ang = 214f - i * 16f - spr * 6f + ruf * 5f;
                Plume(px, w, h, cx - 72f, cy - 16f, ang, 74f + ruf * 10f, 14f, PlumeC, 0.95f);
            }

            DrawLeg(px, w, h, cx - 6f, cy - 72f, step, false, flee);
            DrawLeg(px, w, h, cx + 44f, cy - 80f, step, true, flee);

            Stamp(px, w, h, cx + 8f, cy - 6f, rx * 1.02f, ry * 1.02f, BodyLo, 0f, 0.95f, 0.16f, 0);
            Stamp(px, w, h, cx, cy + 4f, rx, ry, BodyC, 0f, 1f, 0.2f, 0);
            Stamp(px, w, h, cx - 34f, cy + 40f, 68f, 50f, BodyHi, 0f, 0.4f, 0.45f, 0);
            Stamp(px, w, h, cx + 38f, cy - 26f, 62f, 48f, BodyLo, 0f, 0.5f, 0.4f, 0);

            // Soft plumage. A few broad dabs, not a cobblestone grid.
            for (int row = 0; row < 6; row++)
            {
                float trow = row / 5f;
                float y = cy + ry * 0.62f - trow * ry * 1.25f;
                float ty = (y - cy) / ry;
                if (ty * ty > 0.92f) continue;
                float half = rx * Mathf.Sqrt(Mathf.Max(0f, 1f - ty * ty));
                for (int k = 0; k < 4; k++)
                {
                    float u = (k + (row % 2) * 0.5f) / 4f;
                    float x = cx - half * 0.72f + u * half * 1.44f;
                    if (dmg >= 1 && ((row + k) % 6) == 0) y -= 5f;
                    float ang = -108f + (k - 1.5f) * 10f + ruf * 8f;
                    var col = Color.Lerp(BodyC, BodyHi, 0.18f + 0.35f * (1f - trow));
                    Stamp(px, w, h, x, y + Mathf.Sin(row * 1.7f + k) * ruf * 3f, 24f + ruf * 4f, 9f, col, ang, 0.42f, 0.48f, 1);
                    Stamp(px, w, h, x - 2f, y + 3f, 14f, 2.4f, BodyHi, ang, 0.28f, 0.55f, 1);
                }
            }

            for (int i = 0; i < 8; i++)
            {
                if (dmg >= 3 && (i % 2) == 0) continue;
                float u = i / 7f;
                float ang = -78f + (u - 0.5f) * 64f;
                float x = cx - 48f + u * 100f;
                float y = cy - ry * 0.7f;
                float length = 26f + ruf * 12f;
                float rad = ang * Mathf.Deg2Rad;
                Stamp(px, w, h, x + Mathf.Cos(rad) * length * 0.45f, y + Mathf.Sin(rad) * length * 0.45f,
                    length * 0.5f, 8f, Color.Lerp(BodyLo, BodyC, 0.4f + 0.2f * (i % 3)), ang, 0.8f, 0.4f, 1);
            }

            for (int i = 0; i < 7; i++)
            {
                if (PlumeGap(i, dmg, false)) continue;
                float ang = 168f - i * 12f + spr * 46f + ruf * 7f;
                Plume(px, w, h, cx - 16f, cy + 22f - i * 5f, ang, 76f + spr * 20f + ruf * 8f, 15f - i * 0.3f, PlumeC, 1f);
            }

            Stamp(px, w, h, cx - 2f, cy - 86f, 36f, 26f, BodyC, 0f, 1f, 0.28f, 0);
            Stamp(px, w, h, cx + 40f, cy - 94f, 32f, 24f, BodyLo, 0f, 1f, 0.28f, 0);

            // Plumage crowds the neck root. The ribbon sinks into this mound.
            Stamp(px, w, h, pivotX, pivotY - 8f, 58f, 42f, BodyC, 0f, 1f, 0.22f, 0);
            Stamp(px, w, h, pivotX - 10f, pivotY + 6f, 22f, 16f, BodyLo, 0f, 0.65f, 0.35f, 0);
            Stamp(px, w, h, pivotX, pivotY + 2f, 28f, 22f, SkinSh, 0f, 1f, 0.26f, 0);
            Stamp(px, w, h, pivotX, pivotY + 8f, 20f, 18f, Skin, 0f, 1f, 0.28f, 0);
            Stamp(px, w, h, pivotX - 6f, pivotY + 10f, 8f, 10f, SkinHi, 0f, 0.45f, 0.4f, 0);
        }

        static bool PlumeGap(int i, int dmg, bool tail)
        {
            if (!tail && dmg >= 2 && (i == 1 || i == 4 || i == 6)) return true;
            if (tail && dmg >= 2 && (i == 0 || i == 3 || i == 5)) return true;
            if (dmg >= 3 && (i == 2 || i == 5)) return true;
            return false;
        }

        static void DrawLeg(Pix[] px, int w, int h, float x, float y, float step, bool back, float flee)
        {
            float swing = Mathf.Sin((step + (back ? 0.55f : 0f)) * Mathf.PI * 2f);
            float kick = swing * (18f + flee * 30f);
            var col = back ? Skin * 0.82f : Skin;
            col.a = 1f;
            float kneeX = x + kick * 0.10f;
            float kneeY = y - 62f;
            float ankleX = x - 6f + kick * 0.48f;
            float ankleY = y - 156f;
            float toeX = ankleX + 38f + kick * 0.15f;
            float toeY = ankleY - 6f;
            Bone(px, w, h, x, y + 8f, kneeX, kneeY, back ? 14f : 18f, col);
            Bone(px, w, h, kneeX, kneeY, ankleX, ankleY, back ? 10f : 13f, col);
            Stamp(px, w, h, ankleX, ankleY, 11f, 9f, col, 0f, 1f, 0.3f, 0);
            Bone(px, w, h, ankleX, ankleY, toeX, toeY, 7f, col);
            Stamp(px, w, h, toeX - 4f, toeY, 20f, 6f, SkinSh, -6f, 1f, 0.32f, 2);
            Stamp(px, w, h, toeX + 14f, toeY + 1f, 8f, 3.2f, HornDk, -8f, 1f, 0.35f, 1);
            Stamp(px, w, h, ankleX - 14f, ankleY + 2f, 12f, 3.6f, SkinSh, 28f, 1f, 0.35f, 2);
        }

        static void DrawHead(Pix[] px, int w, int h, OstrichSample s, OstrichAct act)
        {
            float blink = s.Blink;
            float beak = s.Beak;
            int dmg = s.Damage;
            bool daze = act == OstrichAct.Daze || dmg >= 3;
            bool dizzy = dmg >= 2 && !daze;
            float cx = HeadPivot.x * w;
            float cy = HeadPivot.y * h;
            Stamp(px, w, h, cx + 6f, cy + 8f, 20f, 22f, SkinSh, 0f, 1f, 0.22f, 0);
            Stamp(px, w, h, cx + 10f, cy + 18f, 22f, 26f, Skin, 0f, 1f, 0.24f, 0);
            Stamp(px, w, h, cx + 4f, cy + 20f, 8f, 14f, SkinHi, 0f, 0.4f, 0.4f, 0);
            Stamp(px, w, h, cx + 34f, cy + 40f, 36f, 30f, SkinSh, 0f, 1f, 0.2f, 0);
            Stamp(px, w, h, cx + 32f, cy + 42f, 34f, 28f, Skin, 0f, 1f, 0.24f, 0);
            Stamp(px, w, h, cx + 18f, cy + 52f, 16f, 12f, SkinHi, 0f, 0.45f, 0.4f, 0);
            Stamp(px, w, h, cx + 28f, cy + 64f, 18f, 7f, new Color(0.36f, 0.33f, 0.34f, 1f), -8f, 0.8f, 0.4f, 1);
            float by = cy + 36f;
            Stamp(px, w, h, cx + 64f, by, 30f, 8.5f, Horn, -2f, 1f, 0.3f, 2);
            Stamp(px, w, h, cx + 96f, by - 1f, 12f, 7.5f, Horn, -2f, 1f, 0.28f, 0);
            float ly = by - 7f - beak * 18f;
            Stamp(px, w, h, cx + 62f, ly, 26f, 5.5f, HornDk, -4f - beak * 14f, 1f, 0.3f, 2);
            Stamp(px, w, h, cx + 90f, ly - beak * 2f, 10f, 5f, HornDk, -6f, 1f, 0.3f, 0);
            if (beak > 0.25f)
                Stamp(px, w, h, cx + 70f, by - 6f - beak * 8f, 16f, 5f + beak * 8f, new Color(0.36f, 0.12f, 0.14f, 1f), 0f, 0.95f, 0.35f, 1);
            Stamp(px, w, h, cx + 52f, by + 3f, 4.5f, 1.6f, HornDk, -8f, 0.8f, 0.4f, 1);
            float ex = cx + 36f;
            float ey = cy + 50f;
            if (blink > 0.8f)
            {
                Stamp(px, w, h, ex, ey, 14f, 2.6f, new Color(0.36f, 0.26f, 0.26f, 1f), -8f, 1f, 0.35f, 1);
                Stamp(px, w, h, ex, ey + 2.5f, 12f, 2.2f, Skin, -8f, 1f, 0.4f, 1);
            }
            else if (daze)
            {
                Stamp(px, w, h, ex, ey, 15f, 14f, new Color(0.97f, 0.95f, 0.90f, 1f), 0f, 1f, 0.22f, 0);
                Stamp(px, w, h, ex, ey, 13f, 2.4f, new Color(0.06f, 0.04f, 0.04f, 1f), 32f, 1f, 0.25f, 1);
                Stamp(px, w, h, ex, ey, 13f, 2.4f, new Color(0.06f, 0.04f, 0.04f, 1f), -32f, 1f, 0.25f, 1);
            }
            else if (dizzy)
            {
                Stamp(px, w, h, ex, ey, 15f, 13f, new Color(0.97f, 0.95f, 0.90f, 1f), 0f, 1f, 0.22f, 0);
                Stamp(px, w, h, ex + 1f, ey, 6f, 6f, new Color(0.42f, 0.24f, 0.12f, 1f), 0f, 1f, 0.25f, 0);
                Stamp(px, w, h, ex + 3f, ey + 1f, 2.4f, 2.4f, new Color(0.05f, 0.04f, 0.03f, 1f), 0f, 1f, 0.2f, 0);
                Stamp(px, w, h, ex - 2f, ey - 2f, 2f, 2f, new Color(0.05f, 0.04f, 0.03f, 1f), 0f, 0.8f, 0.3f, 0);
            }
            else
            {
                float squ = 1f - blink * 0.45f;
                Stamp(px, w, h, ex, ey, 16f, 15f * squ, new Color(0.98f, 0.96f, 0.92f, 1f), 0f, 1f, 0.2f, 0);
                Stamp(px, w, h, ex + 1f, ey - 1f, 9.5f, 9.2f * squ, new Color(0.45f, 0.26f, 0.14f, 1f), 0f, 1f, 0.22f, 0);
                Stamp(px, w, h, ex + 2f, ey - 1f, 5.2f, 5.2f * squ, new Color(0.05f, 0.035f, 0.03f, 1f), 0f, 1f, 0.18f, 0);
                Stamp(px, w, h, ex + 5f, ey + 3f, 2.6f, 2.6f, Color.white, 0f, 0.95f, 0.3f, 1);
                Stamp(px, w, h, ex - 2f, ey + 1f, 1.3f, 1.3f, Color.white, 0f, 0.75f, 0.4f, 1);
            }
            Stamp(px, w, h, ex + 2f, ey + 14f, 14f, 2.6f, new Color(0.42f, 0.30f, 0.30f, 1f), -16f, 0.9f, 0.4f, 1);
            Stamp(px, w, h, cx + 52f, cy + 32f, 9f, 4.5f, new Color(0.86f, 0.55f, 0.52f, 1f), 0f, 0.28f, 0.5f, 1);
        }

        static void Plume(Pix[] px, int w, int h, float x, float y, float ang, float length, float width, Color rgb, float alpha)
        {
            float rad = ang * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad);
            float s = Mathf.Sin(rad);
            Stamp(px, w, h, x + c * length * 0.42f, y + s * length * 0.42f, length * 0.50f, width * 0.50f, PlumeSh, ang, alpha * 0.95f, 0.42f, 1);
            Stamp(px, w, h, x + c * length * 0.55f, y + s * length * 0.55f, length * 0.40f, width * 0.32f, rgb, ang, alpha * 0.9f, 0.4f, 1);
            Stamp(px, w, h, x + c * length * 0.62f, y + s * length * 0.62f, length * 0.28f, Mathf.Max(1.3f, width * 0.12f), PlumeC, ang, 0.55f * alpha, 0.5f, 1);
        }

        static void Bone(Pix[] px, int w, int h, float x0, float y0, float x1, float y1, float thick, Color c)
        {
            float dx = x1 - x0;
            float dy = y1 - y0;
            float len = Mathf.Sqrt(dx * dx + dy * dy);
            if (len < 1f) return;
            float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            Stamp(px, w, h, (x0 + x1) * 0.5f, (y0 + y1) * 0.5f, len * 0.52f, thick, c, ang, 1f, 0.42f, 2);
        }

        // mode 0 round volume, 1 flat painted dab, 2 tube (opaque along its length).
        static void Stamp(Pix[] px, int w, int h, float cx, float cy, float rx, float ry, Color c, float rotDeg, float alpha, float soft, int mode)
        {
            if (alpha <= 0.01f || rx < 0.35f || ry < 0.35f) return;
            float rot = rotDeg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rot);
            float sin = Mathf.Sin(rot);
            float reach = Mathf.Sqrt(rx * rx + ry * ry) + 3f;
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - reach - 1f));
            int x1 = Mathf.Min(w - 1, Mathf.CeilToInt(cx + reach + 1f));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - reach - 1f));
            int y1 = Mathf.Min(h - 1, Mathf.CeilToInt(cy + reach + 1f));
            float softK = soft < 0.05f ? 0.05f : soft;
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x + 0.5f - cx;
                    float dy = y + 0.5f - cy;
                    float lx = dx * cos + dy * sin;
                    float ly = -dx * sin + dy * cos;
                    float ax = Mathf.Abs(lx) / rx;
                    float sy = Mathf.Abs(ly) / ry;
                    float a;
                    float shade;
                    if (mode == 2)
                    {
                        if (ax > 1f || sy > 1f) continue;
                        float side = sy;
                        float t = Mathf.Clamp01((1f - side) / softK);
                        a = t * t * (3f - 2f * t);
                        float end = Mathf.Clamp01((1f - ax) / 0.06f);
                        end = end * end * (3f - 2f * end);
                        a *= end;
                        shade = 0.52f + 0.48f * (1f - side * side);
                    }
                    else
                    {
                        float e = ax * ax + sy * sy;
                        if (e >= 1f) continue;
                        float edge = 1f - Mathf.Sqrt(e);
                        float t = Mathf.Clamp01(edge / softK);
                        a = t * t * (3f - 2f * t);
                        float nx = lx / rx;
                        float ny = ly / ry;
                        float nz = Mathf.Sqrt(Mathf.Clamp01(1f - 0.55f * nx * nx - 0.55f * ny * ny));
                        float nd = Mathf.Clamp01(-0.58f * nx + 0.66f * ny + 0.45f * nz);
                        shade = mode == 1 ? 0.62f + 0.45f * nd : 0.38f + 0.78f * nd;
                    }
                    a *= alpha * c.a;
                    if (a <= 0.004f) continue;
                    float nse = Hash01(x, y);
                    shade *= 0.94f + 0.08f * nse;
                    float warm = Mathf.Clamp01(shade);
                    float rr = c.r * shade * Mathf.Lerp(0.92f, 1.05f, warm);
                    float gg = c.g * shade * Mathf.Lerp(0.94f, 1.00f, warm);
                    float bb = c.b * shade * Mathf.Lerp(1.03f, 0.94f, warm);
                    Over(ref px[y * w + x], rr, gg, bb, a);
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
                Grade(p.R * inv, p.G * inv, p.B * inv, out float gr, out float gg, out float gb);
                raw[i] = new Color32(ToByte(gr), ToByte(gg), ToByte(gb), ToByte(a));
            }
            tex.SetPixels32(raw);
            tex.Apply(false, false);
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), pivot, ppu, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        // Review: the painterly pass washed the bird out. Every baked frame gets more
        // saturation and contrast; the brush texture (per-pixel variation) is kept.
        public const float GradeSat = 1.35f, GradeContrast = 1.18f;
        static void Grade(float r, float g, float b, out float or, out float og, out float ob)
        {
            float l = r * 0.299f + g * 0.587f + b * 0.114f;
            r = l + (r - l) * GradeSat; g = l + (g - l) * GradeSat; b = l + (b - l) * GradeSat;
            or = (r - 0.5f) * GradeContrast + 0.5f;
            og = (g - 0.5f) * GradeContrast + 0.5f;
            ob = (b - 0.5f) * GradeContrast + 0.5f;
        }

        static byte ToByte(float v)
        {
            if (v <= 0f) return 0;
            if (v >= 1f) return 255;
            return (byte)(v * 255f + 0.5f);
        }
    }
}
