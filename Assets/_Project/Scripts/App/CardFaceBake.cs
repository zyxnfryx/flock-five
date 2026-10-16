using System.Text;
using UnityEngine;

namespace FlockFive
{
    // Card name, flavor, and finish tag baked into one texture. The flip stretches
    // that texture. Live IMGUI text would rewrap as the card width squashes.
    public static class CardFaceBake
    {
        public struct Request
        {
            public int Width;
            public int Height;
            public string Title;
            public string Flavor;
            public string Finish;
            public bool Full;
            public bool Back;
            public bool Honey;
            public Color Ink;
            public Color FinishInk;
            // Themed card: nameplate ribbon under the title and a rim around the lettering (matches the live path).
            public bool Themed;
            public Color InkOuter;
        }

        public struct Page
        {
            public Texture2D Tex;
            public string TitleLines;
            public string FlavorLines;
        }

        const int Cache = 6;
        static readonly Texture2D[] _tex = new Texture2D[Cache];
        static readonly string[] _key = new string[Cache];
        static readonly string[] _title = new string[Cache];
        static readonly string[] _flavor = new string[Cache];
        static readonly int[] _use = new int[Cache];
        static int _serial;
        static bool _fontFailed;

        // Same wrap Build stores. A narrower face wraps differently, which is why
        // the flip must stretch the wide bake instead of fitting the squashed rect.
        public static string FlavorJoined(Rect face, string title, string flavor, bool full, bool back, bool honey)
        {
            Layout(face, title, flavor, full, back, honey, out _, out _, out _, out _, out var flavorFit);
            return Join(flavorFit.Lines);
        }

        public static Page Build(Request req)
        {
            if (req.Width < 8) req.Width = 8;
            if (req.Height < 8) req.Height = 8;
            if (req.Width > 720) req.Width = 720;
            if (req.Height > 1024) req.Height = 1024;
            string key = req.Width + "x" + req.Height
                + (req.Full ? "F" : "M") + (req.Back ? "B" : "A") + (req.Honey ? "H" : "h")
                + "|" + req.Title + "|" + req.Flavor + "|" + req.Finish
                + "|" + ColorKey(req.Ink) + "|" + ColorKey(req.FinishInk)
                + (req.Themed ? "|T" + ColorKey(req.InkOuter) : "");
            for (int i = 0; i < Cache; i++)
            {
                if (_key[i] != key || _tex[i] == null) continue;
                _use[i] = ++_serial;
                return new Page { Tex = _tex[i], TitleLines = _title[i], FlavorLines = _flavor[i] };
            }

            var face = new Rect(0f, 0f, req.Width, req.Height);
            Layout(face, req.Title, req.Flavor, req.Full, req.Back, req.Honey,
                out var titleR, out var flavorR, out var finishR, out var titleFit, out var flavorFit);
            var finishFit = string.IsNullOrEmpty(req.Finish)
                ? default
                : FlockFiveApp.CardText.FitFinish(finishR, req.Finish, req.Full, 1f);

            var tex = new Texture2D(req.Width, req.Height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = "CardFace"
            };
            var px = new Color32[req.Width * req.Height];
            var ink = req.Ink.a <= 0f ? new Color(0.16f, 0.07f, 0.02f, 1f) : req.Ink;
            var finishInk = req.FinishInk.a <= 0f ? FinishInk.Tag(BeeFinish.InverseRainbow) : req.FinishInk;
            if (req.Themed)
            {
                float step = titleFit.LineStep > 1f ? titleFit.LineStep : titleFit.Size + 2f;
                int n = titleFit.Lines != null ? titleFit.Lines.Length : 0;
                if (n > 0)
                    CardArt.BlitRibbon(px, req.Width, req.Height,
                        CardArt.RibbonFor(titleR, titleR.width / 0.84f, n * step + titleFit.Outline * 2f));
                var rim = req.InkOuter.a <= 0f ? new Color(0.12f, 0.03f, 0.16f, 1f) : req.InkOuter;
                Rim(px, req.Width, req.Height, titleR, titleFit, rim);
                Rim(px, req.Width, req.Height, flavorR, flavorFit, rim);
            }
            Stamp(px, req.Width, req.Height, titleR, titleFit, ink);
            Stamp(px, req.Width, req.Height, flavorR, flavorFit, ink);
            if (!string.IsNullOrEmpty(req.Finish))
                Stamp(px, req.Width, req.Height, finishR, finishFit, finishInk);
            tex.SetPixels32(px);
            tex.Apply(false, false);

            int slot = 0;
            int oldest = int.MaxValue;
            for (int i = 0; i < Cache; i++)
            {
                if (_tex[i] == null) { slot = i; break; }
                if (_use[i] < oldest) { oldest = _use[i]; slot = i; }
            }
            if (_tex[slot] != null) Object.DestroyImmediate(_tex[slot]);
            _tex[slot] = tex;
            _key[slot] = key;
            _title[slot] = Join(titleFit.Lines);
            _flavor[slot] = Join(flavorFit.Lines);
            _use[slot] = ++_serial;
            return new Page { Tex = tex, TitleLines = _title[slot], FlavorLines = _flavor[slot] };
        }

        static void Layout(Rect face, string title, string flavor, bool full, bool back, bool honey,
            out Rect titleR, out Rect flavorR, out Rect finishR,
            out FlockFiveApp.CardText.Fitted titleFit, out FlockFiveApp.CardText.Fitted flavorFit)
        {
            FlockFiveApp.CardText.FaceBands(face, full, back, honey, out titleR, out flavorR);
            finishR = FlockFiveApp.CardText.FinishOf(face, full);
            FlockFiveApp.CardText.CopyArgs(full, back, out int titleFloor, out float titleLead, out int flavorFloor, out float flavorLead, out float flavorRatio);
            titleFit = FlockFiveApp.CardText.Fit(titleR, title ?? "", FlockFiveApp.CardText.FitKind.Title, titleFloor, titleLead, 0);
            int hi = titleFit.Size > 0 ? Mathf.Max(flavorFloor, Mathf.RoundToInt(titleFit.Size * flavorRatio)) : 0;
            flavorFit = FlockFiveApp.CardText.Fit(flavorR, flavor ?? "", FlockFiveApp.CardText.FitKind.Flavor, flavorFloor, flavorLead, hi);
        }

        // 8-way stroke under the fill. Lines use the same wrap and size as the live outlined label.
        static readonly float[] _rx = { 1f, 0.7071f, 0f, -0.7071f, -1f, -0.7071f, 0f, 0.7071f };
        static readonly float[] _ry = { 0f, 0.7071f, 1f, 0.7071f, 0f, -0.7071f, -1f, -0.7071f };

        static void Rim(Color32[] px, int w, int h, Rect box, FlockFiveApp.CardText.Fitted fit, Color rim)
        {
            if (fit.Lines == null || fit.Lines.Length == 0 || fit.Size < 1) return;
            float r = Mathf.Clamp(fit.Size * 0.075f, 1f, 5f);
            for (int ring = 1; ring <= 2; ring++)
                for (int k = 0; k < 8; k++)
                {
                    float rr = r * ring * 0.5f;
                    var b = new Rect(box.x + _rx[k] * rr, box.y + _ry[k] * rr, box.width, box.height);
                    Stamp(px, w, h, b, fit, rim);
                }
        }

        static void Stamp(Color32[] px, int w, int h, Rect box, FlockFiveApp.CardText.Fitted fit, Color ink)
        {
            if (fit.Lines == null || fit.Lines.Length == 0 || fit.Size < 1) return;
            float y = box.y + Mathf.Max(0f, fit.Outline);
            float step = fit.LineStep > 1f ? fit.LineStep : fit.Size + 2f;
            for (int i = 0; i < fit.Lines.Length; i++)
            {
                StampLine(px, w, h, box, y, step, fit.Lines[i], fit.Size, ink);
                y += step;
            }
        }

        static void StampLine(Color32[] px, int w, int h, Rect box, float guiY, float step, string line, int size, Color ink)
        {
            if (string.IsNullOrEmpty(line)) return;
            bool usedFont = false;
            if (!_fontFailed)
            {
                var font = FontOf();
                if (font != null)
                {
                    try { usedFont = StampFont(px, w, h, box, guiY, line, size, ink, font); }
                    catch (System.Exception) { _fontFailed = true; }
                }
            }
            if (!usedFont) StampBlocks(px, w, h, box, guiY, step, line, size, ink);
        }

        static bool StampFont(Color32[] px, int w, int h, Rect box, float guiY, string line, int size, Color ink, Font font)
        {
            font.RequestCharactersInTexture(line, size, FontStyle.Bold);
            var atlas = font.material != null ? font.material.mainTexture as Texture2D : null;
            if (atlas == null || !atlas.isReadable) return false;
            float width = 0f;
            for (int i = 0; i < line.Length; i++)
            {
                if (!font.GetCharacterInfo(line[i], out CharacterInfo info, size, FontStyle.Bold))
                    return false;
                width += info.advance;
            }
            float pen = box.x + Mathf.Max(0f, (box.width - width) * 0.5f);
            float baselineGui = guiY + size * 0.82f;
            int placed = 0;
            for (int i = 0; i < line.Length; i++)
            {
                if (!font.GetCharacterInfo(line[i], out CharacterInfo info, size, FontStyle.Bold))
                    continue;
                placed += BlitGlyph(px, w, h, atlas, info, pen, baselineGui, ink);
                pen += info.advance;
            }
            return placed > 0;
        }

        static int BlitGlyph(Color32[] px, int w, int h, Texture2D atlas, CharacterInfo info, float pen, float baselineGui, Color ink)
        {
            int gw = info.glyphWidth;
            int gh = info.glyphHeight;
            if (gw < 1 || gh < 1) return 0;
            int n = 0;
            for (int py = 0; py < gh; py++)
            {
                for (int pxX = 0; pxX < gw; pxX++)
                {
                    float u = Mathf.Lerp(info.uvTopLeft.x, info.uvTopRight.x, (pxX + 0.5f) / gw);
                    float v = Mathf.Lerp(info.uvTopLeft.y, info.uvBottomLeft.y, (py + 0.5f) / gh);
                    var sample = atlas.GetPixelBilinear(u, v);
                    float a = sample.a > 0.04f ? sample.a : sample.r;
                    if (a < 0.08f) continue;
                    int dx = Mathf.RoundToInt(pen) + info.minX + pxX;
                    int gui = Mathf.RoundToInt(baselineGui) - info.maxY + py;
                    Plot(px, w, h, dx, gui, ink, a);
                    n++;
                }
            }
            return n;
        }

        static void StampBlocks(Color32[] px, int w, int h, Rect box, float guiY, float step, string line, int size, Color ink)
        {
            int cell = Mathf.Clamp(size / 2, 3, 14);
            float width = line.Length * (cell + 1);
            float x = box.x + Mathf.Max(0f, (box.width - width) * 0.5f);
            int gh = Mathf.Clamp(Mathf.RoundToInt(Mathf.Min(step * 0.72f, size * 0.8f)), 4, 28);
            int gy = Mathf.RoundToInt(guiY + 1f);
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] != ' ')
                    BlockGlyph(px, w, h, Mathf.RoundToInt(x), gy, cell, gh, line[i], ink);
                x += cell + 1;
            }
        }

        static void BlockGlyph(Color32[] px, int w, int h, int x, int y, int cw, int ch, char c, Color ink)
        {
            int bits = c;
            for (int row = 0; row < ch; row++)
            {
                for (int col = 0; col < cw; col++)
                {
                    bool edge = row == 0 || col == 0 || row == ch - 1 || col == cw - 1;
                    bool stem = col == cw / 2 || row == ch / 2;
                    int bit = (bits >> (col % 4)) & 1;
                    if (!edge && !stem && bit == 0) continue;
                    Plot(px, w, h, x + col, y + row, ink, 1f);
                }
            }
        }

        static void Plot(Color32[] px, int w, int h, int x, int guiY, Color ink, float a)
        {
            if (a <= 0f || x < 0 || guiY < 0 || x >= w || guiY >= h) return;
            int i = (h - 1 - guiY) * w + x;
            var dst = px[i];
            float da = dst.a / 255f;
            float outA = a + da * (1f - a);
            if (outA < 0.004f) return;
            float k = a / outA;
            px[i] = new Color32(
                (byte)Mathf.RoundToInt(Mathf.Lerp(dst.r, ink.r * 255f, k)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(dst.g, ink.g * 255f, k)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(dst.b, ink.b * 255f, k)),
                (byte)Mathf.RoundToInt(outA * 255f));
        }

        static Font FontOf()
        {
            var font = FlockFiveApp.CardText.MeasureStyle().font;
            if (font != null) return font;
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        static string Join(string[] lines)
        {
            if (lines == null || lines.Length == 0) return "";
            var sb = new StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0) sb.Append('\n');
                sb.Append(lines[i] ?? "");
            }
            return sb.ToString();
        }

        static string ColorKey(Color c) =>
            Mathf.RoundToInt(c.r * 255f) + "," + Mathf.RoundToInt(c.g * 255f) + "," + Mathf.RoundToInt(c.b * 255f);
    }
}
