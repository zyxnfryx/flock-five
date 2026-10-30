#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Batchmode stills for the honey-badger polish. CPU paint, so -nographics can write them.
    // Unity -executeMethod FlockFive.Editor.BadgerPolishStills.Save
    public static class BadgerPolishStills
    {
        const string OutDir = "/Users/zfxgames/wkspaces/ff-badger-run/stills";
        const string ArtDir = "Assets/_Project/Art/Resources/Sprites/Badger";

        struct Shot
        {
            public string Name;
            public string Frame;
            public BadgerMood Mood;
            public BadgerWear Wear;
            public float TellT;
            public float BlowT;
            public string Line;
        }

        public static void Save()
        {
            AudioListener.volume = 0f;
            AudioListener.pause = true;
            Directory.CreateDirectory(OutDir);
            float blowT = FlashAt(0.65f);
            var shots = new[]
            {
                new Shot { Name = "tell", Frame = "badger_idle_b", Mood = BadgerMood.Smug, Wear = BadgerWear.Fresh, TellT = 0.25f, BlowT = -1f, Line = "" },
                new Shot { Name = "arc-smug", Frame = "badger_idle_b", Mood = BadgerMood.Smug, Wear = BadgerWear.Fresh, TellT = -1f, BlowT = -1f, Line = BadgerCopy.GoesFirst },
                new Shot { Name = "arc-rattled", Frame = "badger_idle_b", Mood = BadgerMood.Rattled, Wear = BadgerWear.Scuffed, TellT = -1f, BlowT = -1f, Line = BadgerCopy.Spoken(BadgerCopy.GoesFirst, BadgerMood.Rattled) },
                new Shot { Name = "arc-desperate", Frame = "badger_crouch", Mood = BadgerMood.Desperate, Wear = BadgerWear.Wrecked, TellT = -1f, BlowT = -1f, Line = BadgerCopy.Spoken(BadgerCopy.HowMany, BadgerMood.Desperate) },
                new Shot { Name = "arc-i-give", Frame = "badger_shrug", Mood = BadgerMood.Surrender, Wear = BadgerWear.Wrecked, TellT = -1f, BlowT = -1f, Line = BadgerCopy.GiveUp },
                new Shot { Name = "final-blow", Frame = "badger_shrug", Mood = BadgerMood.Surrender, Wear = BadgerWear.Wrecked, TellT = -1f, BlowT = blowT, Line = BadgerCopy.GiveUp },
                new Shot { Name = "wear-fresh", Frame = "badger_idle_b", Mood = BadgerMood.Smug, Wear = BadgerWear.Fresh, TellT = -1f, BlowT = -1f, Line = "" },
                new Shot { Name = "wear-scuffed", Frame = "badger_idle_b", Mood = BadgerMood.Smug, Wear = BadgerWear.Scuffed, TellT = -1f, BlowT = -1f, Line = "" },
                new Shot { Name = "wear-bruised", Frame = "badger_idle_b", Mood = BadgerMood.Smug, Wear = BadgerWear.Bruised, TellT = -1f, BlowT = -1f, Line = "" },
                new Shot { Name = "wear-wrecked", Frame = "badger_crouch", Mood = BadgerMood.Smug, Wear = BadgerWear.Wrecked, TellT = -1f, BlowT = -1f, Line = "" },
            };
            var sizes = new[]
            {
                new Vector2Int(402, 874),
                new Vector2Int(820, 1180),
                new Vector2Int(412, 915),
            };
            int n = 0;
            for (int s = 0; s < sizes.Length; s++)
            {
                for (int i = 0; i < shots.Length; i++)
                {
                    string name = sizes[s].x + "x" + sizes[s].y + "-" + shots[i].Name + ".png";
                    var board = new Board(sizes[s].x, sizes[s].y);
                    Paint(board, shots[i]);
                    string path = Path.Combine(OutDir, name);
                    File.WriteAllBytes(path, board.Png());
                    Debug.Log("[badger-still] " + path);
                    n++;
                }
            }
            Debug.Log("BADGER_POLISH_STILLS_DONE " + n);
            EditorApplication.Exit(0);
        }

        // t inside the flash where BlowFlash is nearest `alpha`, so the badger still reads.
        static float FlashAt(float alpha)
        {
            float bestT = 0f;
            float best = 99f;
            int steps = 32;
            for (int i = 0; i < steps; i++)
            {
                float t = BossBeat.FlashSeconds * i / (float)steps;
                float d = Mathf.Abs(BadgerPolish.BlowFlash(t) - alpha);
                if (d < best) { best = d; bestT = t; }
            }
            return bestT;
        }

        static void Paint(Board b, Shot shot)
        {
            b.Clear(new Color(0.36f, 0.55f, 0.34f, 1f));
            var sky = new Rect(0f, 0f, b.W, b.H * 0.42f);
            b.Fill(sky, new Color(0.62f, 0.78f, 0.86f, 1f));
            b.Fill(new Rect(0f, b.H * 0.62f, b.W, b.H * 0.38f), new Color(0.42f, 0.58f, 0.28f, 1f));

            var tex = Load(shot.Frame);
            if (tex == null) tex = Load("badger_idle_b");
            float deg = BadgerPolish.EarsBack(shot.Mood);
            if (shot.TellT >= 0f) deg += BossBeat.PawTwitch(shot.TellT);
            BadgerPolish.WearScale(shot.Wear, out float sx, out float sy);
            float footX = b.W * 0.56f;
            float footY = b.H * 0.70f;
            float aspect = tex != null && tex.height > 0 ? tex.width / (float)tex.height : 0.7f;
            float baseH = b.H * 0.46f;
            var drawn = b.BlitFoot(tex, shot.Frame, footX, footY, baseH * aspect * sx, baseH * sy, deg, shot.Mood, shot.Wear);

            if (shot.TellT >= 0f)
            {
                float g = BossBeat.Glint(shot.TellT);
                if (g > 0.02f && drawn.width > 4f)
                {
                    var eye = EyeOn(drawn, BadgerPolish.TellEyeU, BadgerPolish.TellEyeV, deg, footX, footY);
                    int gs = Mathf.Max(6, Mathf.RoundToInt(b.H * 0.04f * g));
                    b.Oval((int)(eye.x - gs * 0.5f), (int)(eye.y - gs * 0.35f), gs, (int)(gs * 0.7f), new Color(1f, 0.98f, 0.82f, 0.95f));
                    b.Oval((int)(eye.x - gs * 0.18f), (int)(eye.y - gs * 0.12f), Mathf.Max(3, gs / 3), Mathf.Max(3, gs / 3), new Color(1f, 1f, 1f, 1f));
                }
            }

            if (!string.IsNullOrEmpty(shot.Line))
                Caption(b, shot.Line);

            if (shot.BlowT >= 0f)
            {
                float a = BadgerPolish.BlowFlash(shot.BlowT);
                if (a > 0.02f)
                    b.Fill(new Rect(0f, 0f, b.W, b.H), new Color(1f, 1f, 1f, a));
            }
        }

        static Vector2 EyeOn(Rect drawn, float u, float v, float deg, float footX, float footY)
        {
            float x = drawn.x + drawn.width * u;
            float y = drawn.y + drawn.height * v;
            return Rotate(x, y, footX, footY, deg);
        }

        static Vector2 Rotate(float x, float y, float ox, float oy, float deg)
        {
            float rad = deg * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad);
            float s = Mathf.Sin(rad);
            float dx = x - ox;
            float dy = y - oy;
            return new Vector2(ox + dx * c - dy * s, oy + dx * s + dy * c);
        }

        static void Caption(Board b, string line)
        {
            int w = Mathf.Max(80, b.W - 36);
            int h = Mathf.Clamp(Mathf.RoundToInt(b.H * 0.16f), 96, 220);
            int x = (b.W - w) / 2;
            int y = b.H - h - 28;
            b.Fill(new Rect(x, y, w, h), new Color(0.98f, 0.94f, 0.82f, 1f));
            b.Fill(new Rect(x + 4, y + 4, w - 8, h - 8), new Color(0.93f, 0.84f, 0.62f, 1f));
            var page = CardFaceBake.Build(new CardFaceBake.Request
            {
                Width = Mathf.Min(720, w - 16),
                Height = Mathf.Min(1024, h - 16),
                Title = line,
                Flavor = "",
                Finish = "",
                Full = false,
                Back = false,
                Honey = false,
                Ink = new Color(0.16f, 0.07f, 0.02f, 1f),
            });
            if (page.Tex != null)
                b.Blit(page.Tex, x + 8, y + 8, w - 16, h - 16);
        }

        static Texture2D Load(string name)
        {
            string path = Path.Combine(ArtDir, name + ".png");
            if (!File.Exists(path)) return null;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(File.ReadAllBytes(path))) return null;
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        sealed class Board
        {
            public readonly int W;
            public readonly int H;
            readonly Color32[] _px;

            public Board(int w, int h)
            {
                W = w;
                H = h;
                _px = new Color32[w * h];
            }

            public void Clear(Color c) => Fill(new Rect(0, 0, W, H), c);

            public void Fill(Rect r, Color c)
            {
                int x0 = Mathf.Clamp(Mathf.FloorToInt(r.x), 0, W);
                int y0 = Mathf.Clamp(Mathf.FloorToInt(r.y), 0, H);
                int x1 = Mathf.Clamp(Mathf.CeilToInt(r.xMax), 0, W);
                int y1 = Mathf.Clamp(Mathf.CeilToInt(r.yMax), 0, H);
                var src = (Color32)c;
                for (int y = y0; y < y1; y++)
                {
                    int row = (H - 1 - y) * W;
                    for (int x = x0; x < x1; x++)
                        _px[row + x] = src.a >= 250 ? src : Over(_px[row + x], src);
                }
            }

            public void Oval(int x, int y, int w, int h, Color c)
            {
                if (w < 2 || h < 2) return;
                float cx = x + w * 0.5f;
                float cy = y + h * 0.5f;
                var src = (Color32)c;
                int x0 = Mathf.Max(0, x);
                int y0 = Mathf.Max(0, y);
                int x1 = Mathf.Min(W, x + w);
                int y1 = Mathf.Min(H, y + h);
                for (int py = y0; py < y1; py++)
                {
                    int row = (H - 1 - py) * W;
                    for (int px = x0; px < x1; px++)
                    {
                        float nx = (px - cx) / (w * 0.5f);
                        float ny = (py - cy) / (h * 0.5f);
                        if (nx * nx + ny * ny > 1f) continue;
                        _px[row + px] = src.a >= 250 ? src : Over(_px[row + px], src);
                    }
                }
            }

            public void Plot(int x, int y, Color c)
            {
                if ((uint)x >= (uint)W || (uint)y >= (uint)H) return;
                int i = (H - 1 - y) * W + x;
                var src = (Color32)c;
                _px[i] = src.a >= 250 ? src : Over(_px[i], src);
            }

            // Plants the opaque foot. Wear marks are painted in sprite space first,
            // using the same shares as the live page, then the whole sprite leans.
            public Rect BlitFoot(Texture2D tex, string frame, float footX, float footY, float dw, float dh, float deg, BadgerMood mood, BadgerWear wear)
            {
                if (tex == null || dw < 2f || dh < 2f) return default;
                int sw = tex.width;
                int sh = tex.height;
                var src = tex.GetPixels32();
                PaintWear(src, sw, sh, mood, wear);
                FootOf(frame, out float cx, out float bottom);
                float destW = dw;
                float destH = dh;
                float left = footX - destW * cx;
                float top = footY - destH * bottom;
                int x0 = Mathf.FloorToInt(Mathf.Min(left, footX) - destW);
                int y0 = Mathf.FloorToInt(Mathf.Min(top, footY) - destH);
                int x1 = Mathf.CeilToInt(Mathf.Max(left + destW, footX) + destW);
                int y1 = Mathf.CeilToInt(Mathf.Max(top + destH, footY) + destH);
                x0 = Mathf.Clamp(x0, 0, W);
                y0 = Mathf.Clamp(y0, 0, H);
                x1 = Mathf.Clamp(x1, 0, W);
                y1 = Mathf.Clamp(y1, 0, H);
                float rad = -deg * Mathf.Deg2Rad;
                float c = Mathf.Cos(rad);
                float s = Mathf.Sin(rad);
                for (int y = y0; y < y1; y++)
                {
                    int row = (H - 1 - y) * W;
                    for (int x = x0; x < x1; x++)
                    {
                        float dx = x - footX;
                        float dy = y - footY;
                        float ux = footX + dx * c - dy * s;
                        float uy = footY + dx * s + dy * c;
                        float su = (ux - left) / destW;
                        float sv = (uy - top) / destH;
                        if (su < 0f || sv < 0f || su > 1f || sv > 1f) continue;
                        int sx = Mathf.Clamp(Mathf.RoundToInt(su * (sw - 1)), 0, sw - 1);
                        int sy = Mathf.Clamp(Mathf.RoundToInt(sv * (sh - 1)), 0, sh - 1);
                        var p = src[(sh - 1 - sy) * sw + sx];
                        if (p.a < 8) continue;
                        _px[row + x] = p.a >= 250 ? p : Over(_px[row + x], p);
                    }
                }
                return new Rect(left, top, destW, destH);
            }

            static void PaintWear(Color32[] src, int sw, int sh, BadgerMood mood, BadgerWear wear)
            {
                int n = BadgerPolish.BruiseCount(wear);
                var bruise = new Color(0.45f, 0.32f, 0.55f, 0.72f);
                for (int i = 0; i < n; i++)
                {
                    BadgerPolish.BruiseUv(i, out float u, out float v, out float rad);
                    Disc(src, sw, sh, u, v, rad, rad * 0.75f, bruise);
                }
                if (BadgerPolish.EarsBack(mood) > 0.5f)
                {
                    var pin = new Color(0.22f, 0.14f, 0.10f, 0.80f);
                    for (int i = 0; i < 2; i++)
                    {
                        BadgerPolish.EarUv(i, out float u, out float v);
                        Disc(src, sw, sh, u, v, 0.035f, 0.028f, pin);
                    }
                }
                if (BadgerPolish.Sweat(mood))
                {
                    var bead = new Color(0.75f, 0.90f, 0.98f, 0.90f);
                    for (int i = 0; i < 2; i++)
                    {
                        BadgerPolish.SweatUv(i, out float u, out float v);
                        Disc(src, sw, sh, u, v, 0.018f, 0.032f, bead);
                    }
                }
            }

            static void Disc(Color32[] src, int sw, int sh, float u, float v, float ru, float rv, Color c)
            {
                int cx = Mathf.RoundToInt(u * (sw - 1));
                int cy = Mathf.RoundToInt(v * (sh - 1));
                int rx = Mathf.Max(2, Mathf.RoundToInt(ru * sw));
                int ry = Mathf.Max(2, Mathf.RoundToInt(rv * sh));
                var ink = (Color32)c;
                for (int y = cy - ry; y <= cy + ry; y++)
                {
                    if ((uint)y >= (uint)sh) continue;
                    for (int x = cx - rx; x <= cx + rx; x++)
                    {
                        if ((uint)x >= (uint)sw) continue;
                        float nx = (x - cx) / (float)rx;
                        float ny = (y - cy) / (float)ry;
                        if (nx * nx + ny * ny > 1f) continue;
                        int i = (sh - 1 - y) * sw + x;
                        if (src[i].a < 8) continue;
                        src[i] = ink.a >= 250 ? ink : Over(src[i], ink);
                    }
                }
            }

            static void FootOf(string frame, out float cx, out float bottom)
            {
                if (frame == "badger_crouch") { cx = 345f / 803f; bottom = 598f / 604f; return; }
                if (frame == "badger_shrug") { cx = 399f / 618f; bottom = 648f / 654f; return; }
                cx = 352.5f / 438f;
                bottom = 634f / 640f;
            }

            public void Blit(Texture2D tex, int dx, int dy, int dw, int dh)
            {
                if (tex == null || dw < 2 || dh < 2) return;
                var src = tex.GetPixels32();
                int sw = tex.width;
                int sh = tex.height;
                for (int y = 0; y < dh; y++)
                {
                    int ty = dy + y;
                    if ((uint)ty >= (uint)H) continue;
                    int sy = Mathf.Clamp(Mathf.RoundToInt(y / (float)dh * (sh - 1)), 0, sh - 1);
                    int row = (H - 1 - ty) * W;
                    for (int x = 0; x < dw; x++)
                    {
                        int tx = dx + x;
                        if ((uint)tx >= (uint)W) continue;
                        int sx = Mathf.Clamp(Mathf.RoundToInt(x / (float)dw * (sw - 1)), 0, sw - 1);
                        var p = src[(sh - 1 - sy) * sw + sx];
                        if (p.a < 8) continue;
                        _px[row + tx] = p.a >= 250 ? p : Over(_px[row + tx], p);
                    }
                }
            }

            public byte[] Png()
            {
                var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
                tex.SetPixels32(_px);
                tex.Apply(false, false);
                var bytes = tex.EncodeToPNG();
                Object.DestroyImmediate(tex);
                return bytes;
            }

            static Color32 Over(Color32 dst, Color32 src)
            {
                float a = src.a / 255f;
                float ia = 1f - a;
                return new Color32(
                    (byte)Mathf.RoundToInt(src.r * a + dst.r * ia),
                    (byte)Mathf.RoundToInt(src.g * a + dst.g * ia),
                    (byte)Mathf.RoundToInt(src.b * a + dst.b * ia),
                    255);
            }
        }
    }
}
#endif
