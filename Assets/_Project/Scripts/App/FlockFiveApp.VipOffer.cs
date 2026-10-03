using UnityEngine;

namespace FlockFive
{
    // Medallion opens this. Buy is the gold button only.
    // Copy is the real NoAds deal: stage-clear ads off, extra-branch
    // ads still optional, non-consumable. NoAds has no price string.
    public sealed partial class FlockFiveApp
    {
        static class VipOffer
        {
            const string Title = "VIP";
            const string BuyLabel = "Go VIP";
            const string RestoreLabel = "Already a VIP? Restore purchase";
            const string Body =
                "No ads between stages.\nYou can still watch for a branch.\nOne-time purchase.";
            const float BuyScale = 0.85f;

            static bool _open;
            static GUIStyle _title, _body, _buy, _link;
            static string _note;
            static float _noteUntil;

            public static bool IsOpen => _open;

            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
            static void Reset() => _open = false;

            public static void Show()
            {
                if (NoAds.Owned) return;
                _open = true;
            }

            public static void Close() => _open = false;

            static void Dismiss()
            {
                if (!_open) return;
                _open = false;
                Sfx.CardTap();
            }

            public static void Draw(float s)
            {
                if (!_open) return;
                if (NoAds.Owned && (string.IsNullOrEmpty(_note) || Time.unscaledTime >= _noteUntil))
                {
                    _open = false;
                    _note = null;
                    return;
                }

                var safe = Screen.safeArea;
                float top = TopHud();
                float xSz = Mathf.Max(48f * s, 44f);
                var xBtn = new Rect(
                    Screen.width - Mathf.Max(14f, Screen.width - safe.xMax + 8f) - xSz,
                    top, xSz, xSz);
                bool xHit = HitPad(xBtn, out bool xHeld);

                GiftCardLayout(s, out var card, out var flower);
                var buyFlower = ScaledAbout(flower, BuyScale);
                var disc = FlowerDisc(buyFlower, 0f);
                var discHit = GrowDisc(disc, s);
                bool buy = HitPad(discHit, out bool buyHeld);

                float linkH = Mathf.Max(44f, 36f * s);
                float gap = 22f * s;
                float linkY = buyFlower.yMax + gap;
                float playFlower = Mathf.Min(Screen.width * 0.94f, Screen.height * 0.50f);
                float playTop = Screen.height - Mathf.Max(14f, safe.yMin + 8f) - playFlower;
                float maxY = playTop - linkH - 10f * s;
                if (linkY > maxY) linkY = Mathf.Max(disc.yMax + 12f * s, maxY);
                float linkW = Mathf.Min(card.width * 0.86f, 340f * s);
                var link = new Rect(flower.center.x - linkW * 0.5f, linkY, linkW, linkH);
                bool restore = HitPad(link, out bool linkHeld);

                float x0 = Mathf.Min(card.x, Mathf.Min(flower.x, link.x));
                float y0 = Mathf.Min(card.y, flower.y);
                float x1 = Mathf.Max(card.xMax, Mathf.Max(flower.xMax, link.xMax));
                float y1 = Mathf.Max(card.yMax, Mathf.Max(flower.yMax, link.yMax));
                var swallow = new Rect(x0 - 12f, y0 - 12f, (x1 - x0) + 24f, (y1 - y0) + 24f);
                HitPad(swallow, out _);
                bool outside = HitPad(new Rect(0f, 0f, Screen.width, Screen.height), out _);
                if (outside && swallow.Contains(Event.current.mousePosition)) outside = false;

                float t = Time.unscaledTime;
                float breathe = 0.5f + 0.5f * Mathf.Sin(t * 2.05f);
                var glow = GlowTex();
                GUI.color = new Color(0.04f, 0.03f, 0.02f, 0.72f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;

                float aura = card.width * (0.10f + 0.04f * breathe);
                GUI.color = new Color(1f, 0.78f, 0.22f, 0.30f + 0.22f * breathe);
                GUI.DrawTexture(new Rect(card.x - aura, card.y - aura * 0.6f, card.width + aura * 2f, card.height + aura * 1.2f), glow, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;

                var tex = SpriteCatalog.AdCard != null ? SpriteCatalog.AdCard.texture : null;
                if (tex != null)
                {
                    GUI.color = new Color(0.10f, 0.06f, 0.03f, 0.40f);
                    GUI.DrawTexture(new Rect(card.x + 6f, card.y + 14f, card.width, card.height), tex, ScaleMode.ScaleToFit, true);
                    GUI.color = Color.white;
                    GUI.DrawTexture(card, tex, ScaleMode.ScaleToFit, true);
                }
                DrawVipAccents(card);

                var plate = new Rect(
                    card.x + card.width * 0.13f,
                    card.y + card.height * 0.22f,
                    card.width * 0.74f,
                    card.height * 0.54f);
                GUI.color = new Color(0.04f, 0.02f, 0.01f, 0.92f);
                GUI.DrawTexture(plate, Texture2D.whiteTexture);
                GUI.color = new Color(1f, 0.78f, 0.22f, 0.42f + 0.28f * breathe);
                GUI.DrawTexture(new Rect(plate.x + 2f * s, plate.y + 2f * s, plate.width - 4f * s, 3f * s), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(plate.x + 2f * s, plate.yMax - 5f * s, plate.width - 4f * s, 3f * s), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(plate.x + 2f * s, plate.y + 2f * s, 3f * s, plate.height - 4f * s), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(plate.xMax - 5f * s, plate.y + 2f * s, 3f * s, plate.height - 4f * s), Texture2D.whiteTexture);
                GUI.color = Color.white;

                DrawCopy(plate, s, breathe, glow);
                float bulbFrac = Mathf.Clamp(15f * s / Mathf.Max(1f, plate.width), 0.040f, 0.058f);
                DrawGiftMarquee(plate, s, t, 0.70f, -1f, bulbFrac, 12, 0f, true, 0.16f, 0f, card);
                DrawBuy(buyFlower, buyHeld, s);
                DrawRestore(link, linkHeld, s);
                DrawRestoreNote(link, s);
                DrawGiftCloseX(xBtn, xHeld, s);
                GUI.color = Color.white;

                if (buy)
                {
                    Sfx.CardTap();
                    NoAds.Buy();
                    if (NoAds.Owned) _open = false;
                    return;
                }
                if (restore)
                {
                    Sfx.CardTap();
                    NoAds.Restore();
                    _note = NoAds.Owned ? "Restored" : "Nothing to restore";
                    _noteUntil = Time.unscaledTime + 1.6f;
                    return;
                }
                if (xHit || outside) Dismiss();
            }

            static void DrawCopy(Rect plate, float s, float breathe, Texture2D glow)
            {
                float insetX = Mathf.Max(12f * s, plate.width * 0.08f);
                float insetY = Mathf.Max(8f * s, plate.height * 0.07f);
                var block = new Rect(
                    plate.x + insetX,
                    plate.y + insetY,
                    plate.width - insetX * 2f,
                    Mathf.Max(48f, plate.height * 0.78f - insetY));
                float titleH = block.height * 0.30f;
                var titleR = new Rect(block.x, block.y, block.width, titleH);
                GUI.color = new Color(1f, 0.72f, 0.16f, 0.40f + 0.22f * breathe);
                GUI.DrawTexture(new Rect(titleR.x - 6f * s, titleR.y - 4f * s, titleR.width + 12f * s, titleR.height + 8f * s), glow, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;

                if (_title == null)
                    _title = new GUIStyle(GUI.skin.label)
                    {
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                        wordWrap = false
                    };
                int titleHi = Mathf.Max(26, Mathf.RoundToInt(40f * s));
                _title.fontSize = FitFont(_title, Title, titleR.width * 0.92f, titleR.height * 0.90f, 18, titleHi);
                int titleInk = Mathf.Max(2, Mathf.RoundToInt(_title.fontSize * 0.10f));
                StampOutlined(titleR, Title, _title, new Color(1f, 0.96f, 0.72f), 1, titleInk);

                if (_body == null)
                    _body = new GUIStyle(GUI.skin.label)
                    {
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                        wordWrap = true
                    };
                var bodyR = new Rect(block.x, titleR.yMax, block.width, Mathf.Max(24f, block.yMax - titleR.yMax));
                int bodyLo = Mathf.Max(12, Mathf.RoundToInt(13f * s));
                int bodyHi = Mathf.Max(bodyLo + 2, Mathf.RoundToInt(22f * s));
                _body.fontSize = FitFontWrapped(_body, Body, bodyR.width, bodyR.height * 0.96f, bodyLo, bodyHi);
                var bodyContent = new GUIContent(Body);
                int bodyGuard = 0;
                while (_body.fontSize > 10 && bodyGuard < 24
                    && _body.CalcHeight(bodyContent, bodyR.width) > bodyR.height)
                {
                    _body.fontSize -= 1;
                    bodyGuard++;
                }
                int bodyInk = Mathf.Max(1, Mathf.RoundToInt(_body.fontSize * 0.12f));
                StampOutlined(bodyR, Body, _body, new Color(1f, 0.96f, 0.78f), 1, bodyInk);
            }

            static void DrawBuy(Rect flower, bool held, float s)
            {
                var disc = DrawPopupButton(flower, held, true);
                if (_buy == null)
                    _buy = new GUIStyle(GUI.skin.label)
                    {
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                        wordWrap = false
                    };
                int hi = Mathf.Max(18, Mathf.RoundToInt(42f * s));
                _buy.fontSize = FitFont(_buy, BuyLabel, disc.width * 0.82f, disc.height * 0.62f, 12, hi);
                var content = new GUIContent(BuyLabel);
                int guard = 0;
                while (_buy.fontSize > 10 && guard < 20 && _buy.CalcSize(content).x > disc.width * 0.90f)
                {
                    _buy.fontSize -= 1;
                    guard++;
                }
                int ink = Mathf.Clamp(Mathf.RoundToInt(_buy.fontSize * 0.12f), 2, 6);
                StampOutlined(disc, BuyLabel, _buy, new Color(0.28f, 0.12f, 0.04f), 2, ink);
            }

            static void DrawRestore(Rect link, bool held, float s)
            {
                if (_link == null)
                    _link = new GUIStyle(GUI.skin.label)
                    {
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                        wordWrap = false
                    };
                var st = _link;
                int hi = Mathf.Max(11, Mathf.RoundToInt(15f * s));
                st.fontSize = FitFont(st, RestoreLabel, link.width * 0.96f, link.height * 0.62f, 10, hi);
                int ink = Mathf.Clamp(Mathf.RoundToInt(st.fontSize * 0.16f), 1, 3);
                var inkCol = held
                    ? new Color(0.72f, 0.58f, 0.40f, 1f)
                    : new Color(0.55f, 0.42f, 0.28f, 0.96f);
                StampOutlined(link, RestoreLabel, st, inkCol, 0, ink);
            }

            static void DrawRestoreNote(Rect link, float s)
            {
                if (string.IsNullOrEmpty(_note) || Time.unscaledTime >= _noteUntil) return;
                if (_link == null) return;
                var st = _link;
                float h = Mathf.Max(22f, 18f * s);
                var r = new Rect(link.x, link.yMax + 4f * s, link.width, h);
                int hi = Mathf.Max(11, Mathf.RoundToInt(14f * s));
                st.fontSize = FitFont(st, _note, r.width * 0.96f, r.height * 0.90f, 10, hi);
                StampOutlined(r, _note, st, new Color(0.93f, 0.86f, 0.70f, 0.95f), 0, 2);
            }

            // Baked orchids stay in the card texture. Wash the corners, then two
            // small petals at opposite top corners so the frame is not a cluster.
            static void DrawVipAccents(Rect card)
            {
                var glow = GlowTex();
                float w = card.width * 0.20f;
                float h = card.height * 0.24f;
                GUI.color = new Color(0.05f, 0.03f, 0.025f, 0.62f);
                GUI.DrawTexture(new Rect(card.x - w * 0.04f, card.y - h * 0.02f, w, h), glow, ScaleMode.ScaleToFit, true);
                GUI.DrawTexture(new Rect(card.xMax - w * 0.96f, card.y - h * 0.02f, w, h), glow, ScaleMode.ScaleToFit, true);
                float bw = card.width * 0.18f;
                float bh = card.height * 0.20f;
                GUI.DrawTexture(new Rect(card.x, card.yMax - bh, bw, bh), glow, ScaleMode.ScaleToFit, true);
                GUI.DrawTexture(new Rect(card.xMax - bw, card.yMax - bh, bw, bh), glow, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
                float sz = card.width * 0.10f;
                var pink = SpriteCatalog.PetalPink;
                var peach = SpriteCatalog.PetalPeach;
                if (pink != null && pink.texture != null)
                {
                    GUI.color = new Color(1f, 0.74f, 0.82f, 0.88f);
                    GUI.DrawTexture(new Rect(card.x + card.width * 0.03f, card.y + card.height * 0.03f, sz, sz), pink.texture, ScaleMode.ScaleToFit, true);
                }
                if (peach != null && peach.texture != null)
                {
                    GUI.color = new Color(1f, 0.80f, 0.58f, 0.88f);
                    float pz = sz * 0.86f;
                    GUI.DrawTexture(new Rect(card.xMax - pz - card.width * 0.04f, card.y + card.height * 0.045f, pz, pz), peach.texture, ScaleMode.ScaleToFit, true);
                }
                GUI.color = Color.white;
            }

            static Rect ScaledAbout(Rect r, float k)
            {
                float w = r.width * k;
                float h = r.height * k;
                return new Rect(r.center.x - w * 0.5f, r.center.y - h * 0.5f, w, h);
            }

            static Rect GrowDisc(Rect disc, float s)
            {
                float hitGrow = 18f * s;
                var hit = new Rect(disc.x - hitGrow, disc.y - hitGrow * 0.65f, disc.width + hitGrow * 2f, disc.height + hitGrow * 1.3f);
                float minH = 52f * s;
                if (hit.height < minH)
                {
                    float extra = minH - hit.height;
                    hit.y -= extra * 0.5f;
                    hit.height += extra;
                }
                return hit;
            }
        }
    }
}
