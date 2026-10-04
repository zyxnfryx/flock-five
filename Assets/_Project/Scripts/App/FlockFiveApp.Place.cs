using System.Collections.Generic;
using UnityEngine;

namespace FlockFive
{
    // Static seats. Captions and pop-ups come from the safe area and the
    // reference scale only. They never read a bird, a glove, or a popup scale.
    public sealed partial class FlockFiveApp
    {
        // Cream used by the poker bet line and the other poker labels.
        static readonly Color PokerLabelGold = new Color(1f, 0.94f, 0.72f, 1f);
        // Extra gap under the five-card row, in reference pixels (720p), times s.
        const float PokerCardCaptionGap = 14f;

        // Top of the bet caption: under the card row, plus the shared extra gap.
        static float PokerCaptionTop(float s, float rowBottom) =>
            rowBottom + (2f + PokerCardCaptionGap) * s;
        // Top-left Back only. Finger points up-left so the hand enters from below-right.
        const float GloveBelowDeg = -45f;

        // Lowest legal caption top: just under the logo. Never the screen top.
        // The gift line's preferred seat starts here. PlaceCaption uses the
        // same floor when a line has to sit under the glove or the top row.
        static float CaptionFloorY(float s)
        {
            float title = TopHud() + (56f * 2f + 4f) * s;
            var halo = SplashTitleHalo();
            float below = title;
            if (halo.height > 2f && halo.yMax > below) below = halo.yMax;
            float y = below + 10f * s;
            if (y < 8f) y = 8f;
            return y;
        }

        // w/h are the text box. belowY is the thing it must sit under (logo, cards).
        // Static: never reads a bird or a glove, so a tap arc cannot re-seat the plate.
        static Rect PlaceCaption(float s, float w, float h, float belowY)
        {
            float min = CaptionFloorY(s);
            float y = belowY + 8f * s;
            if (y < min) y = min;
            float maxY = Screen.height - h - 8f;
            if (y > maxY) y = maxY > min ? maxY : min;
            if (y < min) y = min;
            if (w < 8f) w = 8f;
            if (h < 8f) h = 8f;
            float x = (Screen.width - w) * 0.5f;
            float left = Mathf.Max(8f, Screen.safeArea.xMin + 6f);
            float right = Screen.width - Mathf.Max(8f, Screen.width - Screen.safeArea.xMax + 6f) - w;
            if (right < left) right = left;
            if (x < left) x = left;
            if (x > right) x = right;
            return new Rect(x, y, w, h);
        }

        // One content width for every standard card: daily, gift, welcome, ask,
        // restart, freeze, VIP, and the ad stage. Insets, 88% of the glass,
        // cap 600 reference pixels, floor 240. Safe-area aware.
        static float StandardPopupWidth(float s)
        {
            float insetL = Mathf.Max(12f * s, Screen.safeArea.xMin + 8f);
            float insetR = Mathf.Max(12f * s, Screen.width - Screen.safeArea.xMax + 8f);
            float span = Screen.width - insetL - insetR;
            if (span < 8f) span = 8f;
            float cardW = Mathf.Min(span, Mathf.Min(Screen.width * 0.88f, 600f * s));
            float floor = 240f;
            if (floor > span) floor = span;
            if (cardW < floor) cardW = floor;
            if (cardW < 8f) cardW = 8f;
            return cardW;
        }

        // Square CTA (Watch, Claim, Retry, Buy). Same fraction of the shared card.
        static float PopupButtonSize(float s, float cardW)
        {
            if (s < 1f) s = 1f;
            float sz = cardW * 0.62f;
            float cap = Screen.width * 0.58f;
            if (sz > cap) sz = cap;
            float floor = 120f * s;
            if (sz < floor) sz = floor;
            float span = Screen.width - 24f;
            if (span < 8f) span = 8f;
            if (sz > span) sz = span;
            if (sz < 8f) sz = 8f;
            return sz;
        }

        // How far the square CTA's box rises into the card above it. The pedestal art
        // starts about 13% down its square, so 15% parks the pedestal on the card's
        // bottom edge. lipRoom > 0 is the card's own spare bottom (brass plus board pad)
        // the art may sink into: the pedestal then rises to 24% so no gap is left where
        // the marquee bulbs on the bottom edge show. 0 keeps the plain 15%.
        static float PopupCtaOverlap(float flowerSz, float lipRoom)
        {
            float plain = flowerSz * 0.15f;
            if (lipRoom <= 0f) return plain;
            float lifted = flowerSz * 0.24f;
            float most = flowerSz * 0.13f + lipRoom;
            float v = lifted < most ? lifted : most;
            return v > plain ? v : plain;
        }

        // Wide bar on the same card (Yes / No, Thanks, ask). Grows with PopupButtonSize.
        static float PopupBarHeight(float s, float cardW)
        {
            if (s < 1f) s = 1f;
            float h = PopupButtonSize(s, cardW) * 0.18f;
            float floor = 44f * s;
            if (h < floor) h = floor;
            float cap = 64f * s;
            if (h > cap) h = cap;
            return h;
        }

        // Tutorial line under a pop-up control. belowY is that control's bottom.
        // block is the rect the plate must clear. Centered, safe-area clamped.
        static Rect PlacePopupTutorCaption(float s, float w, float h, float belowY, Rect block)
        {
            float gap = 10f * s;
            float y = belowY + gap;
            if (block.height > 2f && y < block.yMax + gap)
                y = block.yMax + gap;
            if (w < 8f) w = 8f;
            if (h < 8f) h = 8f;
            return PlaceCaption(s, w, h, y - 8f * s);
        }

        // Card origin in the safe band. bias is 0 at the top of that band, 1 at the bottom.
        static Rect PlacePopup(float s, float w, float h, float bias)
        {
            float insetL = Mathf.Max(12f * s, Screen.safeArea.xMin + 8f);
            float insetR = Mathf.Max(12f * s, Screen.width - Screen.safeArea.xMax + 8f);
            float top = TopHud() + 4f * s;
            float bot = Screen.height - Mathf.Max(8f, Screen.safeArea.yMin + 4f);
            float span = Screen.width - insetL - insetR;
            if (span < 8f) span = 8f;
            if (w > span) w = span;
            if (w < 8f) w = 8f;
            if (h < 8f) h = 8f;
            float x = insetL + (span - w) * 0.5f;
            float room = bot - top - h;
            if (bias < 0f) bias = 0f;
            if (bias > 1f) bias = 1f;
            float y = top + (room > 0f ? room * bias : 0f);
            if (y + h > bot) y = bot - h;
            if (y < top) y = top;
            return new Rect(x, y, w, h);
        }

        // Right-edge anchor. w/h are the prop. top is the preferred top.
        // Safe-area inset only. Does not read a bird, a glove, or a popup scale.
        static Rect PlaceRight(float s, float w, float h, float top)
        {
            if (w < 8f) w = 8f;
            if (h < 8f) h = 8f;
            float inset = Mathf.Max(8f * s, Screen.width - Screen.safeArea.xMax + 6f);
            float x = Screen.width - inset - w;
            float left = Mathf.Max(8f, Screen.safeArea.xMin + 6f);
            if (x < left) x = left;
            float y = top;
            float bot = Screen.height - h - Mathf.Max(8f, Screen.safeArea.yMin + 4f);
            if (y > bot) y = bot;
            if (y < 8f) y = 8f;
            return new Rect(x, y, w, h);
        }

        // One home-bird size. Garden body times HomeAvatarMul. Not a layout clamp
        // and not the adoption px, so naming the bird cannot shrink it.
        static float HomeBirdIcon(float s) => HomeAvatarIcon(s);

        static float HomeAvatarIcon(float s)
        {
            float h = Screen.height > 2f ? Screen.height : s * 720f;
            float ppu = h / (WorldBuilder.CamOrtho * 2f);
            if (ppu < 1f) ppu = 1f;
            float icon = AvatarBirdWorld() * ppu * HomeAvatarMul;
            if (icon < 8f) icon = 8f;
            return icon;
        }

        static Texture2D _notifyBadge;
        static Texture2D _cautionMark;
        static GUIStyle _notifyDigits;

        // Hard antialiased disc: dark outer edge, cream ring, red face.
        static Texture2D NotifyBadgeTex()
        {
            if (_notifyBadge != null) return _notifyBadge;
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = "NotifyBadge"
            };
            var px = new Color32[n * n];
            float c = (n - 1) * 0.5f;
            float rad = c - 0.5f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = x + 0.5f - c;
                    float dy = y + 0.5f - c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / rad;
                    float a = Mathf.Clamp01((1f - d) * rad);
                    if (a <= 0f) continue;
                    Color col;
                    if (d > 0.90f) col = new Color(0.12f, 0.06f, 0.05f, 1f);
                    else if (d > 0.76f) col = new Color(1f, 0.96f, 0.88f, 1f);
                    else col = new Color(0.86f, 0.12f, 0.10f, 1f);
                    col.a *= a;
                    px[y * n + x] = col;
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _notifyBadge = tex;
            return tex;
        }

        // Shared ready-to-collect badge. The hive new-dot and the daily rail both call this.
        // Pulse and disc stay here so the two icons cannot drift.
        static void DrawReadyBadge(float cx, float cy, float plateWidth)
        {
            float breathe = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.6f);
            float dot = plateWidth * (0.16f + 0.02f * breathe);
            DrawNotifyBadge(cx, cy, dot, null);
        }

        // cx, cy is the center. number is optional and drawn in the face.
        static void DrawNotifyBadge(float cx, float cy, float diameter, string number)
        {
            var tex = NotifyBadgeTex();
            if (tex == null || diameter < 2f) return;
            float d = diameter;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(cx - d * 0.5f, cy - d * 0.5f, d, d), tex, ScaleMode.ScaleToFit, true);
            if (string.IsNullOrEmpty(number)) return;
            if (_notifyDigits == null)
                _notifyDigits = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = false
                };
            float inner = d * 0.62f;
            _notifyDigits.fontSize = Mathf.Max(8, Mathf.RoundToInt(inner * 0.72f));
            var slot = new Rect(cx - d * 0.5f, cy - d * 0.5f, d, d);
            DrawBadgeNumber(BadgeBodyCenter(slot, tex), number, _notifyDigits, Color.white, inner, inner, 1);
        }

        struct BadgeMark
        {
            public float U;
            public float V;
            public bool Ok;
        }

        static readonly Dictionary<int, BadgeMark> _badgeMark = new Dictionary<int, BadgeMark>();

        // ScaleToFit letterbox. The body sits in the drawn image, not the slot,
        // because flame frames are not all the same width as the medal slot.
        static Rect BadgeFit(Rect slot, Texture2D tex)
        {
            if (tex == null || tex.width < 1 || tex.height < 1 || slot.width < 1f || slot.height < 1f)
                return slot;
            float sa = slot.width / slot.height;
            float ta = tex.width / (float)tex.height;
            if (ta > sa)
            {
                float h = slot.width / ta;
                return new Rect(slot.x, slot.y + (slot.height - h) * 0.5f, slot.width, h);
            }
            float w = slot.height * ta;
            return new Rect(slot.x + (slot.width - w) * 0.5f, slot.y, w, slot.height);
        }

        // Belly of the opaque art, in GUI y-down. Rows narrower than the body
        // (the flame tip, the caps of a disc) are left out, so a flame's digit
        // sits on the bright belly and a round badge stays on the disc center.
        static Vector2 BadgeBodyCenter(Rect slot, Texture2D tex)
        {
            var fit = BadgeFit(slot, tex);
            if (!TryBadgeMark(tex, out float u, out float v)) return fit.center;
            return new Vector2(fit.x + u * fit.width, fit.y + v * fit.height);
        }

        static bool TryBadgeMark(Texture2D tex, out float u, out float v)
        {
            u = 0.5f;
            v = 0.5f;
            if (tex == null || !tex.isReadable || tex.width < 2 || tex.height < 2) return false;
            int id = tex.GetInstanceID();
            if (id != 0 && _badgeMark.TryGetValue(id, out var got))
            {
                if (!got.Ok) return false;
                u = got.U;
                v = got.V;
                return true;
            }
            bool ok = MeasureBadge(tex, out u, out v);
            if (id != 0) _badgeMark[id] = new BadgeMark { U = u, V = v, Ok = ok };
            return ok;
        }

        static bool MeasureBadge(Texture2D tex, out float u, out float v)
        {
            u = 0.5f;
            v = 0.5f;
            int w = tex.width;
            int h = tex.height;
            var px = tex.GetPixels32();
            if (px == null || px.Length < (long)w * h) return false;
            var left = new int[h];
            var right = new int[h];
            int widest = 0;
            for (int y = 0; y < h; y++)
            {
                int lo = -1;
                int hi = -1;
                int row = y * w;
                for (int x = 0; x < w; x++)
                {
                    if (px[row + x].a <= 40) continue;
                    if (lo < 0) lo = x;
                    hi = x;
                }
                left[y] = lo;
                right[y] = hi;
                if (lo < 0) continue;
                int wide = hi - lo + 1;
                if (wide > widest) widest = wide;
            }
            if (widest < 1) return false;
            float body = widest * 0.42f;
            double sumA = 0d, sumX = 0d, sumY = 0d;
            double allA = 0d, allX = 0d, allY = 0d;
            for (int y = 0; y < h; y++)
            {
                if (left[y] < 0) continue;
                bool belly = (right[y] - left[y] + 1) >= body;
                int row = y * w;
                for (int x = left[y]; x <= right[y]; x++)
                {
                    int a = px[row + x].a;
                    if (a <= 40) continue;
                    double ad = a;
                    allA += ad;
                    allX += (x + 0.5d) * ad;
                    allY += (y + 0.5d) * ad;
                    if (!belly) continue;
                    sumA += ad;
                    sumX += (x + 0.5d) * ad;
                    sumY += (y + 0.5d) * ad;
                }
            }
            double use = sumA > 0d ? sumA : allA;
            if (use <= 0d) return false;
            double cx = (sumA > 0d ? sumX : allX) / use;
            double cy = (sumA > 0d ? sumY : allY) / use;
            u = (float)(cx / w);
            v = (float)(1d - cy / h);
            return true;
        }

        // Ink center of the glyphs, relative to the em box MiddleCenter uses.
        // Positive x means the ink sits right of that box (a "1" does this).
        static void BadgeGlyphShift(string text, GUIStyle style, out float ox, out float oy)
        {
            ox = 0f;
            oy = 0f;
            if (string.IsNullOrEmpty(text) || style == null) return;
            var font = BadgeFont(style);
            if (font == null) return;
            // Real laid-out glyph quads first: the ink box is measured from the same
            // MiddleCenter layout the label uses, not from font metrics.
            if (BadgeInkShift(text, style, font, out ox, out oy)) return;
            ox = 0f;
            oy = 0f;
            int size = style.fontSize;
            if (size < 1) size = font.fontSize;
            if (size < 1) size = 16;
            var face = style.fontStyle;
            font.RequestCharactersInTexture(text, size, face);
            float pen = 0f;
            float minX = 0f, maxX = 0f, minY = 0f, maxY = 0f;
            bool any = false;
            for (int i = 0; i < text.Length; i++)
            {
                if (!font.GetCharacterInfo(text[i], out var info, size, face)) return;
                float x0 = pen + info.minX;
                float x1 = pen + info.maxX;
                if (!any || x0 < minX) minX = x0;
                if (!any || x1 > maxX) maxX = x1;
                if (!any || info.minY < minY) minY = info.minY;
                if (!any || info.maxY > maxY) maxY = info.maxY;
                any = true;
                pen += info.advance;
            }
            if (!any || pen <= 0f) return;
            ox = (minX + maxX) * 0.5f - pen * 0.5f;
            float ascent = font.ascent;
            float line = font.lineHeight;
            if (font.fontSize > 1)
            {
                float scale = size / (float)font.fontSize;
                ascent *= scale;
                line *= scale;
            }
            if (ascent < 1f) ascent = size * 0.92f;
            if (line < 1f) line = size;
            oy = (ascent - (minY + maxY) * 0.5f) - line * 0.5f;
        }

        static Font _badgeFont;
        static TextGenerator _badgeGen;
        static string _badgeInkText;
        static int _badgeInkSize = -1;
        static FontStyle _badgeInkFace;
        static int _badgeInkFont;
        static float _badgeInkX;
        static float _badgeInkY;
        static bool _badgeInkOk;

        // GUI.skin.font is null on the default skin, which used to leave every shift at
        // zero: the "1" then rode high and right of the flame. Fall back to the
        // built-in runtime font the labels actually draw with.
        static Font BadgeFont(GUIStyle style)
        {
            if (style.font != null) return style.font;
            if (GUI.skin != null && GUI.skin.font != null) return GUI.skin.font;
            if (_badgeFont == null) _badgeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _badgeFont;
        }

        // Ink center of the string as TextGenerator lays it out MiddleCenter around the
        // rect center. ox: + is right of center. oy: + is below center (GUI y down).
        static bool BadgeInkShift(string text, GUIStyle style, Font font, out float ox, out float oy)
        {
            ox = 0f;
            oy = 0f;
            int size = style.fontSize > 0 ? style.fontSize : 16;
            int fontId = font.GetInstanceID();
            if (_badgeInkSize == size && _badgeInkFace == style.fontStyle && _badgeInkFont == fontId
                && string.Equals(_badgeInkText, text))
            {
                ox = _badgeInkX;
                oy = _badgeInkY;
                return _badgeInkOk;
            }
            _badgeInkSize = size;
            _badgeInkFace = style.fontStyle;
            _badgeInkFont = fontId;
            _badgeInkText = text;
            _badgeInkOk = false;
            if (_badgeGen == null) _badgeGen = new TextGenerator();
            var settings = new TextGenerationSettings
            {
                font = font,
                color = Color.white,
                fontSize = size,
                fontStyle = style.fontStyle,
                lineSpacing = 1f,
                richText = false,
                scaleFactor = 1f,
                textAnchor = TextAnchor.MiddleCenter,
                alignByGeometry = false,
                resizeTextForBestFit = false,
                updateBounds = false,
                horizontalOverflow = HorizontalWrapMode.Overflow,
                verticalOverflow = VerticalWrapMode.Overflow,
                generateOutOfBounds = true,
                generationExtents = new Vector2(2000f, 2000f),
                pivot = new Vector2(0.5f, 0.5f)
            };
            _badgeGen.Populate(text, settings);
            var verts = _badgeGen.verts;
            int n = _badgeGen.vertexCount;
            if (verts == null || n < 4) return false;
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < n && i < verts.Count; i++)
            {
                var v = verts[i].position;
                if (v.x < minX) minX = v.x;
                if (v.x > maxX) maxX = v.x;
                if (v.y < minY) minY = v.y;
                if (v.y > maxY) maxY = v.y;
            }
            if (maxX <= minX || maxY <= minY) return false;
            _badgeInkX = (minX + maxX) * 0.5f;
            _badgeInkY = -(minY + maxY) * 0.5f;
            _badgeInkOk = true;
            ox = _badgeInkX;
            oy = _badgeInkY;
            return true;
        }

        static Vector2 BadgePadShift(GUIStyle style)
        {
            if (style == null) return Vector2.zero;
            var pad = style.padding;
            float x = style.contentOffset.x;
            float y = style.contentOffset.y;
            if (pad != null)
            {
                x += (pad.left - pad.right) * 0.5f;
                y += (pad.top - pad.bottom) * 0.5f;
            }
            return new Vector2(x, y);
        }

        // One number path for the streak flame and the red disc. The label's
        // em box is shifted so the glyph ink, not the advance, lands on body.
        static void DrawBadgeNumber(Vector2 body, string text, GUIStyle style, Color color, float boxW, float boxH, int blackPx)
        {
            if (string.IsNullOrEmpty(text) || style == null || boxW < 1f || boxH < 1f) return;
            BadgeGlyphShift(text, style, out float ox, out float oy);
            var pad = BadgePadShift(style);
            float cx = body.x - pad.x - ox;
            float cy = body.y - pad.y - oy;
            var num = new Rect(cx - boxW * 0.5f, cy - boxH * 0.5f, boxW, boxH);
            StampOutlined(num, text, style, color, 0, blackPx);
        }

        static Texture2D CautionMarkTex()
        {
            if (_cautionMark != null) return _cautionMark;
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = "CautionMark"
            };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            {
                float ny = 1f - (y + 0.5f) / n;
                for (int x = 0; x < n; x++)
                {
                    float nx = (x + 0.5f) / n;
                    float half = Mathf.Lerp(0.06f, 0.48f, ny);
                    float dx = Mathf.Abs(nx - 0.5f);
                    if (dx > half) continue;
                    float edge = (half - dx) * n;
                    float baseE = ny * n;
                    float a = Mathf.Clamp01(Mathf.Min(edge, baseE));
                    if (a <= 0f) continue;
                    float rim = Mathf.Clamp01(Mathf.Min(edge, baseE) / 3.2f);
                    Color col = rim < 1f
                        ? new Color(0.10f, 0.06f, 0.04f, 1f)
                        : new Color(1f, 0.78f, 0.18f, 1f);
                    if (rim > 1f && dx > half - 5.5f / n)
                        col = Color.Lerp(col, new Color(1f, 0.96f, 0.78f, 1f), 0.65f);
                    col.a *= a;
                    px[y * n + x] = col;
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _cautionMark = tex;
            return tex;
        }

        static void DrawCautionMark(Rect r)
        {
            var tex = CautionMarkTex();
            if (tex == null || r.width < 2f) return;
            GUI.color = Color.white;
            GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit, true);
        }

        // Two-stroke check. Shared by VIP benefit lines. No extra art.
        static void DrawCheckMark(Rect r)
        {
            if (r.width < 2f) return;
            var prev = GUI.matrix;
            float thick = Mathf.Max(2f, r.height * 0.16f);
            var p = new Vector2(r.x + r.width * 0.32f, r.center.y);
            GUI.color = new Color(0.55f, 0.85f, 0.42f, 1f);
            GUIUtility.RotateAroundPivot(42f, p);
            GUI.DrawTexture(new Rect(p.x - r.width * 0.18f, p.y - thick * 0.5f, r.width * 0.36f, thick), Texture2D.whiteTexture);
            GUI.matrix = prev;
            var q = new Vector2(r.x + r.width * 0.52f, r.y + r.height * 0.30f);
            GUIUtility.RotateAroundPivot(-48f, q);
            GUI.DrawTexture(new Rect(q.x - r.width * 0.02f, q.y - thick * 0.5f, r.width * 0.52f, thick), Texture2D.whiteTexture);
            GUI.matrix = prev;
            GUI.color = Color.white;
        }

        static GUIStyle _bottomStatus;

        // Shared banner type. Sits in a caller-placed bottom band.
        static void DrawBottomStatus(Rect line, string text, float alpha, float s)
        {
            if (string.IsNullOrEmpty(text) || alpha < 0.04f || line.width < 8f) return;
            if (_bottomStatus == null)
                _bottomStatus = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = false
                };
            int hi = Mathf.Max(14, Mathf.RoundToInt(18f * s));
            _bottomStatus.fontSize = FitFont(_bottomStatus, text, line.width * 0.96f, line.height * 0.88f, 12, hi);
            StampBannerText(line, text, _bottomStatus, alpha, true);
        }
    }
}
