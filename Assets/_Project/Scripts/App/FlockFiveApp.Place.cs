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
        // SeatTutorialCaption uses this for the gift line only.
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
            var box = new Rect(cx - inner * 0.5f, cy - inner * 0.5f, inner, inner);
            StampOutlined(box, number, _notifyDigits, Color.white, 0, 1);
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

        // Cream line with a thick dark edge. Sits in a caller-placed bottom band.
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
            int ink = Mathf.Max(3, Mathf.RoundToInt(_bottomStatus.fontSize * 0.16f));
            StampOutlined(line, text, _bottomStatus, new Color(1f, 0.97f, 0.88f, alpha), 0, ink);
        }
    }
}
