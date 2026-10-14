#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // Three proposals for the first card the player sees. None of them is chosen.
    // CPU paint, so a batchmode run can write the PNGs without the editor window.
    public static class FirstCardStills
    {
        const int W = 750;
        const int H = 1334;

        public static void Save()
        {
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "stills");
            Directory.CreateDirectory(dir);
            var honey = Find("honey");
            var bass = Find("bass");
            int n = 0;
            n += Write(dir, "first-card-a-held.png", board => OptionHeld(board, honey));
            n += Write(dir, "first-card-b-peek.png", board => OptionPeek(board, honey));
            n += Write(dir, "first-card-c-value.png", board => OptionValue(board, honey));
            n += Write(dir, "bass-note.png", board => BassProof(board, bass));
            n += Write(dir, "moth-guest.png", MothProof);
            n += Write(dir, "warm-rims.png", WarmProof);
            n += Write(dir, "inverse-tag.png", InverseProof);
            Debug.Log("FIRST_CARD_STILLS_DONE " + n);
        }

        static int Write(string dir, string name, System.Action<Board> paint)
        {
            var board = new Board(W, H);
            board.Clear(new Color(0.16f, 0.10f, 0.06f, 1f));
            paint(board);
            string path = Path.Combine(dir, name);
            File.WriteAllBytes(path, board.Png());
            Debug.Log("[still] " + path);
            return 1;
        }

        static void OptionHeld(Board b, BeeKind kind)
        {
            Caption(b, "Option A. Opens on the card you just earned.");
            var card = new Rect(150, 220, 450, 630);
            PaintCard(b, card, kind, true, false);
            Stamp(b, new Rect(card.x + 36, card.y + card.height * 0.48f, card.width - 72, 180), kind.Name, CardCopy.Shown(kind), "");
            Dots(b, 3, 0, 980);
        }

        static void OptionPeek(Board b, BeeKind kind)
        {
            Caption(b, "Option B. Neighbors peek in. You swipe or tap. Nothing turns on its own.");
            PaintCard(b, new Rect(40, 340, 250, 350), kind, false, true);
            PaintCard(b, new Rect(460, 340, 250, 350), kind, false, true);
            var mid = new Rect(230, 250, 290, 406);
            PaintCard(b, mid, kind, true, false);
            Stamp(b, new Rect(mid.x + 16, mid.y + mid.height * 0.55f, mid.width - 32, 140), kind.Name, "", "");
            Dots(b, 5, 2, 980);
        }

        static void OptionValue(Board b, BeeKind kind)
        {
            Caption(b, "Option C. Name, line, and one comb are already on the front.");
            var card = new Rect(145, 200, 460, 644);
            PaintCard(b, card, kind, true, false);
            string flavor = CardCopy.Shown(kind);
            Stamp(b, card, kind.Name, flavor, "");
            int hx = (int)card.center.x;
            int hy = (int)(card.y + card.height * 0.72f);
            Hex(b, hx, hy, 54, 62, HoneyRim.For(BeeFinish.Normal));
        }

        static void BassProof(Board b, BeeKind kind)
        {
            Caption(b, "Bass Note. The painting stays visible on the cream plate.");
            var raw = kind.Tint;
            var lifted = BeeInk.Body(BeeInk.Mul(kind.Tint));
            LabelCard(b, new Rect(70, 280, 280, 392), "Before", raw);
            LabelCard(b, new Rect(400, 280, 280, 392), "After", lifted);
        }

        static void MothProof(Board b)
        {
            Caption(b, "Moth Guest. A fuzzy moth, and a rare kind.");
            var card = new Rect(175, 240, 400, 560);
            var kind = Find("moth");
            PaintCard(b, card, kind, true, false);
            var tex = MothArt.Texture();
            int side = 260;
            b.Blit(tex, (int)card.center.x - side / 2, (int)card.y + 150, side, side);
            Stamp(b, new Rect(card.x + 30, card.yMax - 160, card.width - 60, 120), "Moth Guest", "Wrong party.", "");
        }

        static void WarmProof(Board b)
        {
            Caption(b, "Comb wax stays warm for Normal, Holo, and Inverse Rainbow.");
            var finishes = new[] { BeeFinish.Normal, BeeFinish.Holo, BeeFinish.InverseRainbow };
            string[] names = { "Normal", "Holo", "Inverse" };
            for (int i = 0; i < 3; i++)
            {
                int cx = 160 + i * 210;
                Hex(b, cx, 560, 70, 80, HoneyRim.For(finishes[i]));
                Stamp(b, new Rect(cx - 90, 700, 180, 120), names[i], "", "");
            }
        }

        static void InverseProof(Board b)
        {
            Caption(b, "Inverse Rainbow, in ink that reads on the cream plate.");
            var card = new Rect(120, 280, 510, 420);
            var face = CardPlate.Face(new Color(0.72f, 0.68f, 0.55f), true);
            b.Fill(card, face);
            Stamp(b, new Rect(card.x + 24, card.y + 40, card.width - 48, 200), "Inverse Rainbow", "Readable on the plate.", "");
        }

        static void Caption(Board b, string text)
        {
            var bar = new Rect(36, 48, W - 72, 140);
            b.Fill(bar, new Color(0.96f, 0.90f, 0.72f, 1f));
            Stamp(b, new Rect(bar.x + 16, bar.y + 12, bar.width - 32, bar.height - 24), text, "", "");
        }

        static void PaintCard(Board b, Rect card, BeeKind kind, bool full, bool faded)
        {
            var wood = CardPlate.Wood(kind.Tint, true);
            var face = CardPlate.Face(kind.Tint, true);
            if (faded)
            {
                wood.a = 0.55f;
                face.a = 0.55f;
            }
            b.Fill(new Rect(card.x + 8, card.y + 10, card.width, card.height), new Color(0f, 0f, 0f, faded ? 0.2f : 0.35f));
            b.Fill(card, wood);
            float inset = card.width * 0.06f;
            b.Fill(new Rect(card.x + inset, card.y + inset, card.width - inset * 2f, card.height - inset * 2f), face);
            if (!full) return;
            var body = BeeInk.Body(BeeInk.Mul(kind.Tint));
            BeeBlob(b, (int)card.center.x, (int)(card.y + card.height * 0.34f), (int)(card.width * 0.22f), body);
        }

        static void LabelCard(Board b, Rect card, string label, Color bee)
        {
            b.Fill(card, CardPlate.Face(new Color(0.22f, 0.22f, 0.38f), true));
            Stamp(b, new Rect(card.x + 12, card.y + 16, card.width - 24, 80), label, "", "");
            BeeBlob(b, (int)card.center.x, (int)(card.y + card.height * 0.55f), (int)(card.width * 0.28f), bee);
        }

        static void BeeBlob(Board b, int cx, int cy, int rad, Color body)
        {
            var wing = new Color(0.95f, 0.92f, 0.84f, 0.9f);
            b.Oval(cx - rad, cy - rad / 3, rad, (int)(rad * 0.7f), wing);
            b.Oval(cx + rad / 5, cy - rad / 3, rad, (int)(rad * 0.7f), wing);
            b.Oval(cx - rad / 2, cy - rad, rad, (int)(rad * 1.5f), body);
            var stripe = new Color(0.12f, 0.08f, 0.05f, 1f);
            b.Fill(new Rect(cx - rad / 3, cy - rad / 5, (int)(rad * 0.7f), rad / 7), stripe);
            b.Fill(new Rect(cx - rad / 3, cy + rad / 6, (int)(rad * 0.7f), rad / 7), stripe);
        }

        static void Hex(Board b, int cx, int cy, int rx, int ry, Color wax)
        {
            var rim = new Color(wax.r * 0.55f, wax.g * 0.45f, wax.b * 0.3f, 1f);
            for (int y = cy - ry; y <= cy + ry; y++)
            {
                for (int x = cx - rx; x <= cx + rx; x++)
                {
                    float nx = (x - cx) / (float)rx;
                    float ny = (y - cy) / (float)ry;
                    float d = Mathf.Abs(nx) * 0.55f + Mathf.Abs(ny);
                    if (d > 1f) continue;
                    b.Plot(x, y, d > 0.82f ? rim : wax);
                }
            }
        }

        static void Dots(Board b, int count, int on, int y)
        {
            int dot = 22;
            int gap = 16;
            int row = count * dot + (count - 1) * gap;
            int x = (W - row) / 2;
            for (int i = 0; i < count; i++)
            {
                var c = i == on ? new Color(1f, 0.82f, 0.28f, 1f) : new Color(1f, 0.94f, 0.72f, 0.45f);
                b.Oval(x, y, dot, dot, c);
                x += dot + gap;
            }
        }

        static void Stamp(Board b, Rect area, string title, string flavor, string finish)
        {
            int w = Mathf.Max(8, Mathf.RoundToInt(area.width));
            int h = Mathf.Max(8, Mathf.RoundToInt(area.height));
            var page = CardFaceBake.Build(new CardFaceBake.Request
            {
                Width = Mathf.Min(720, w),
                Height = Mathf.Min(1024, h),
                Title = title ?? "",
                Flavor = flavor ?? "",
                Finish = finish ?? "",
                Full = false,
                Back = false,
                Honey = false,
                Ink = new Color(0.16f, 0.07f, 0.02f, 1f),
                FinishInk = FinishInk.Tag(BeeFinish.InverseRainbow),
            });
            if (page.Tex == null) return;
            b.BlitInk(page.Tex, (int)area.x, (int)area.y, (int)area.width, (int)area.height);
        }

        static BeeKind Find(string id)
        {
            var roster = Hive.Roster;
            for (int i = 0; i < roster.Length; i++)
                if (roster[i].Id == id) return roster[i];
            return roster[0];
        }

        sealed class Board
        {
            readonly Color32[] _px;
            readonly int _w;
            readonly int _h;

            public Board(int w, int h)
            {
                _w = w;
                _h = h;
                _px = new Color32[w * h];
            }

            public void Clear(Color c) => Fill(new Rect(0, 0, _w, _h), c);

            public void Fill(Rect r, Color c)
            {
                int x0 = Mathf.Clamp(Mathf.FloorToInt(r.x), 0, _w);
                int y0 = Mathf.Clamp(Mathf.FloorToInt(r.y), 0, _h);
                int x1 = Mathf.Clamp(Mathf.CeilToInt(r.xMax), 0, _w);
                int y1 = Mathf.Clamp(Mathf.CeilToInt(r.yMax), 0, _h);
                var src = (Color32)c;
                for (int y = y0; y < y1; y++)
                {
                    int row = (_h - 1 - y) * _w;
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
                int x1 = Mathf.Min(_w, x + w);
                int y1 = Mathf.Min(_h, y + h);
                for (int py = y0; py < y1; py++)
                {
                    int row = (_h - 1 - py) * _w;
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
                if ((uint)x >= (uint)_w || (uint)y >= (uint)_h) return;
                int i = (_h - 1 - y) * _w + x;
                var src = (Color32)c;
                _px[i] = src.a >= 250 ? src : Over(_px[i], src);
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
                    if ((uint)ty >= (uint)_h) continue;
                    int sy = Mathf.Clamp(Mathf.RoundToInt(y / (float)dh * (sh - 1)), 0, sh - 1);
                    int row = (_h - 1 - ty) * _w;
                    for (int x = 0; x < dw; x++)
                    {
                        int tx = dx + x;
                        if ((uint)tx >= (uint)_w) continue;
                        int sx = Mathf.Clamp(Mathf.RoundToInt(x / (float)dw * (sw - 1)), 0, sw - 1);
                        // Texture y 0 is the bottom. GUI y 0 is the top, and sy grows downward.
                        int sample = (sh - 1 - sy) * sw + sx;
                        var p = src[sample];
                        if (p.a < 8) continue;
                        _px[row + tx] = p.a >= 250 ? p : Over(_px[row + tx], p);
                    }
                }
            }

            public void BlitInk(Texture2D tex, int dx, int dy, int dw, int dh)
            {
                var src = tex.GetPixels32();
                int sw = tex.width;
                int sh = tex.height;
                int minX = sw;
                int minY = sh;
                int maxX = 0;
                int maxY = 0;
                bool any = false;
                for (int y = 0; y < sh; y++)
                {
                    for (int x = 0; x < sw; x++)
                    {
                        if (src[y * sw + x].a < 24) continue;
                        any = true;
                        if (x < minX) minX = x;
                        if (y < minY) minY = y;
                        if (x > maxX) maxX = x;
                        if (y > maxY) maxY = y;
                    }
                }
                if (!any) return;
                int bw = maxX - minX + 1;
                int bh = maxY - minY + 1;
                int ox = dx + Mathf.Max(0, (dw - bw) / 2);
                int oy = dy + Mathf.Max(0, (dh - bh) / 2);
                for (int y = minY; y <= maxY; y++)
                {
                    int ty = oy + (maxY - y);
                    if ((uint)ty >= (uint)_h) continue;
                    int row = (_h - 1 - ty) * _w;
                    for (int x = minX; x <= maxX; x++)
                    {
                        int tx = ox + (x - minX);
                        if ((uint)tx >= (uint)_w) continue;
                        var p = src[y * sw + x];
                        if (p.a < 24) continue;
                        _px[row + tx] = p.a >= 250 ? p : Over(_px[row + tx], p);
                    }
                }
            }

            public byte[] Png()
            {
                var tex = new Texture2D(_w, _h, TextureFormat.RGBA32, false);
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
