using UnityEngine;

namespace FlockFive
{
    // Medallion opens this. Buy is the gold button only.
    // Copy is the real NoAds deal: stage-clear ads off, extra-branch
    // ads still optional, one-time purchase. Price comes from the store.
    public sealed partial class FlockFiveApp
    {
        static class VipOffer
        {
            const string Headline = "Go VIP: play without interruptions";
            const string BuyLabel = "Go VIP";
            const string RestoreLabel = "Already a VIP? Restore purchase";
            const float BuyScale = 1.08f;
            const float FullSpan = 2.05f;
            const float ShortSpan = 0.45f;

            static readonly string[] Benefits =
            {
                "No ads between stages",
                "Rewarded ads for a branch stay optional",
                "One-time purchase. No subscription."
            };

            static bool _open;
            static bool _fullPlayed;
            static bool _full;
            static bool _reduce;
            static bool _skipped;
            static bool _thud;
            static int _revealed;
            static float _showAt = -1f;
            static Vector2 _anchor;
            static bool _anchored;
            static int _noteSerial = -1;
            static float _noteUntil;
            static GUIStyle _title, _body, _buy, _link;

            public static bool IsOpen => _open;

            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
            static void Reset()
            {
                _open = false;
                _fullPlayed = false;
                _showAt = -1f;
                _noteUntil = 0f;
                _noteSerial = -1;
                _anchored = false;
            }

            public static void Anchor(Rect rail)
            {
                _anchor = rail.center;
                _anchored = rail.width > 4f;
            }

            public static void Show()
            {
                if (NoAds.Owned || _open) return;
                if (_app != null && !_app.GatePopup(PopupKind.Vip, true)) return;
                _open = true;
                _showAt = Time.unscaledTime;
                _skipped = false;
                _thud = false;
                _revealed = 0;
                _noteUntil = 0f;
                _reduce = PlayerPrefs.GetInt("flockfive.reduceMotion", 0) == 1;
                _full = !_fullPlayed && !_reduce;
                if (_full)
                {
                    _fullPlayed = true;
                    SfxLibrary.Play("fanfare", 0.34f, 0f);
                }
                else
                    Sfx.CardTap();
            }

            public static void Close() => _open = false;

            static void Dismiss()
            {
                if (!_open) return;
                _open = false;
                Sfx.CardTap();
            }

            static float Age => _showAt < 0f ? 99f : Time.unscaledTime - _showAt;

            static float Span => _reduce ? 0f : (_full ? FullSpan : ShortSpan);

            public static void Draw(float s)
            {
                if (!_open) return;
                SyncNote();
                string note = NoAds.RestoreNote;
                float noteLife = _noteUntil - Time.unscaledTime;
                bool showNote = !string.IsNullOrEmpty(note) && noteLife > 0f;
                if (NoAds.Owned && !showNote)
                {
                    _open = false;
                    return;
                }

                float age = Age;
                bool entering = !_reduce && !_skipped && age < Span;
                var safe = Screen.safeArea;
                float top = TopHud();
                float xSz = Mathf.Max(48f * s, 44f);
                var xBtn = new Rect(
                    Screen.width - Mathf.Max(14f, Screen.width - safe.xMax + 8f) - xSz,
                    top, xSz, xSz);

                GiftCardLayout(s, out var card, out var flower);
                flower = ScaledAbout(flower, BuyScale);
                float statusH = Mathf.Max(28f, 22f * s);
                float linkH = Mathf.Max(48f, 40f * s);
                float linkW = Mathf.Min(Screen.width * 0.86f, 420f * s);
                float bottom = Screen.height - Mathf.Max(6f, safe.yMin);
                var link = new Rect((Screen.width - linkW) * 0.5f, bottom - linkH - 4f * s, linkW, linkH);
                var status = new Rect(link.x, link.y - 4f * s - statusH, link.width, statusH);
                SeatCard(s, status, ref card, ref flower);

                var disc = FlowerDisc(flower, 0f);
                var discHit = GrowDisc(disc, s);
                bool xHit = HitPad(xBtn, out bool xHeld);
                bool buy = HitPad(discHit, out bool buyHeld);
                bool restore = HitPad(link, out bool linkHeld);
                var swallow = new Rect(
                    Mathf.Min(card.x, flower.x) - 12f,
                    Mathf.Min(card.y, flower.y) - 12f,
                    Mathf.Max(card.xMax, flower.xMax) - Mathf.Min(card.x, flower.x) + 24f,
                    Mathf.Max(card.yMax, flower.yMax) - Mathf.Min(card.y, flower.y) + 24f);
                bool onCard = HitPad(swallow, out _);
                bool outside = HitPad(new Rect(0f, 0f, Screen.width, Screen.height), out _);
                if (outside && (swallow.Contains(Event.current.mousePosition) || link.Contains(Event.current.mousePosition)))
                    outside = false;

                float t = Time.unscaledTime;
                float breathe = 0.5f + 0.5f * Mathf.Sin(t * 2.05f);
                var glow = GlowTex();
                GUI.color = new Color(0.04f, 0.03f, 0.02f, entering ? 0.78f : 0.72f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
                DrawArrivalGlow(glow, s, age);

                int reveal = RevealCount(age);
                NoteTicks(reveal);
                NoteThud(age);

                if (_full && !_reduce && !_skipped)
                    DrawCrownFlight(s, age, card);
                if (_full && !_reduce && age >= 0.90f && age < 1.45f)
                    DrawVipBurst(new Vector2(card.center.x, card.y + card.height * 0.34f), (age - 0.90f) / 0.55f, s);

                float k = PanelScale(age);
                var pivot = new Vector2(card.center.x, (card.y + flower.yMax) * 0.5f);
                var prev = GUI.matrix;
                if (k > 0.04f)
                {
                    if (k < 0.98f || k > 1.02f)
                        GUIUtility.ScaleAroundPivot(new Vector2(k, k), pivot);
                    DrawCard(card, s, breathe, glow, t, reveal);
                    float shineStart = _full && !_reduce ? 1.62f : ShortSpan;
                    float shineU = (age - shineStart) / 0.55f;
                    DrawBuy(flower, buyHeld, s, shineU, !entering);
                    GUI.matrix = prev;
                }

                DrawRestore(link, linkHeld, s);
                if (showNote)
                {
                    float alpha = noteLife > 0.8f ? 1f : noteLife / 0.8f;
                    DrawBottomStatus(status, note, alpha, s);
                }
                DrawGiftCloseX(xBtn, xHeld, s);
                GUI.color = Color.white;

                if (xHit)
                {
                    Dismiss();
                    return;
                }
                if (restore)
                {
                    Sfx.CardTap();
                    NoAds.Restore();
                    return;
                }
                bool panelIn = _reduce || _skipped || k > 0.92f;
                if (buy && panelIn)
                {
                    Sfx.CardTap();
                    NoAds.Buy();
                    if (NoAds.Owned) _open = false;
                    return;
                }
                if (entering && (buy || outside || onCard))
                {
                    _skipped = true;
                    _revealed = Benefits.Length;
                    return;
                }
                if (outside) Dismiss();
            }

            static void SyncNote()
            {
                if (NoAds.RestoreSerial == _noteSerial) return;
                _noteSerial = NoAds.RestoreSerial;
                _noteUntil = string.IsNullOrEmpty(NoAds.RestoreNote) ? 0f : Time.unscaledTime + 3.6f;
            }

            static int RevealCount(float age)
            {
                if (!_full || _reduce || _skipped) return Benefits.Length;
                if (age < 1.60f) return 0;
                if (age < 1.76f) return 1;
                if (age < 1.92f) return 2;
                return Benefits.Length;
            }

            static void NoteTicks(int reveal)
            {
                if (reveal <= _revealed) return;
                if (_full && !_reduce && !_skipped)
                    SfxLibrary.Play("tick", 0.15f, 0.03f);
                _revealed = reveal;
            }

            static void NoteThud(float age)
            {
                if (_thud || !_full || _reduce) return;
                if (_skipped || age < 0.90f) return;
                _thud = true;
                SfxLibrary.Play("thud", 0.40f, 0f);
                if (CamShake.Live != null)
                    CamShake.Live.Punch(0.14f, 0.05f, 1.1f, 0.03f);
            }

            static float PanelScale(float age)
            {
                if (_reduce || _skipped) return 1f;
                float start = _full ? 1.20f : 0f;
                float dur = _full ? 0.40f : ShortSpan;
                float u = Mathf.Clamp01((age - start) / Mathf.Max(0.05f, dur));
                return EaseOutBack(u);
            }

            static void SeatCard(float s, Rect status, ref Rect card, ref Rect flower)
            {
                float ceiling = status.y - 12f * s;
                float floor = SplashTitleHalo().yMax + 8f * s;
                if (flower.yMax > ceiling)
                {
                    float shift = flower.yMax - ceiling;
                    card.y -= shift;
                    flower.y -= shift;
                }
                if (card.y < floor)
                {
                    float push = floor - card.y;
                    float room = ceiling - flower.yMax;
                    if (room < 0f) room = 0f;
                    if (push > room) push = room;
                    card.y += push;
                    flower.y += push;
                }
                if (flower.yMax > ceiling && flower.height > 8f)
                {
                    float over = flower.yMax - ceiling;
                    float k = Mathf.Clamp(1f - over / flower.height, 0.62f, 1f);
                    flower = ScaledAbout(flower, k);
                    flower.y = ceiling - flower.height;
                }
            }

            static void DrawArrivalGlow(Texture2D glow, float s, float age)
            {
                if (!_full || _reduce || glow == null) return;
                float a = Mathf.Clamp01(1f - Mathf.Abs(age - 0.40f) / 0.55f);
                if (a < 0.04f) return;
                Vector2 from = _anchored ? _anchor : new Vector2(Screen.width * 0.82f, TopHud() + 40f * s);
                float d = Mathf.Lerp(80f * s, 280f * s, Mathf.Clamp01(age / 0.7f));
                GUI.color = new Color(1f, 0.78f, 0.28f, 0.55f * a);
                GUI.DrawTexture(new Rect(from.x - d * 0.5f, from.y - d * 0.5f, d, d), glow, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
            }

            static void DrawCrownFlight(float s, float age, Rect card)
            {
                float u = Mathf.Clamp01((age - 0.35f) / 0.55f);
                float fade = u < 1f ? 1f : Mathf.Clamp01(1f - (age - 0.90f) / 0.28f);
                if (fade < 0.04f) return;
                var tex = VipCrownTex();
                if (tex == null) return;
                Vector2 from = _anchored ? _anchor : new Vector2(Screen.width * 0.82f, TopHud() + 36f * s);
                Vector2 to = new Vector2(card.center.x, card.y + card.height * 0.30f);
                float e = 1f - (1f - u) * (1f - u) * (1f - u);
                Vector2 p = Vector2.Lerp(from, to, e);
                float sz = Mathf.Lerp(36f * s, 92f * s, e);
                GUI.color = new Color(1f, 0.95f, 0.72f, fade);
                GUI.DrawTexture(new Rect(p.x - sz * 0.5f, p.y - sz * 0.55f, sz, sz * 0.8f), tex, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
            }

            static void DrawVipBurst(Vector2 c, float u, float s)
            {
                u = Mathf.Clamp01(u);
                var spark = SpriteCatalog.Sparkle;
                var tex = spark != null && spark.texture != null ? spark.texture : Texture2D.whiteTexture;
                for (int i = 0; i < 12; i++)
                {
                    float ang = i * 0.5236f + 0.4f;
                    float reach = Mathf.Lerp(10f * s, 86f * s, u);
                    float sz = Mathf.Lerp(14f * s, 5f * s, u);
                    float a = (1f - u) * 0.85f;
                    var r = new Rect(c.x + Mathf.Cos(ang) * reach - sz * 0.5f, c.y + Mathf.Sin(ang) * reach - sz * 0.5f, sz, sz);
                    GUI.color = new Color(1f, 0.86f, 0.35f, a);
                    GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit, true);
                }
                GUI.color = Color.white;
            }

            static void DrawCard(Rect card, float s, float breathe, Texture2D glow, float t, int reveal)
            {
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
                    card.x + card.width * 0.10f,
                    card.y + card.height * 0.18f,
                    card.width * 0.80f,
                    card.height * 0.64f);
                GUI.color = new Color(0.04f, 0.02f, 0.01f, 0.92f);
                GUI.DrawTexture(plate, Texture2D.whiteTexture);
                GUI.color = new Color(1f, 0.78f, 0.22f, 0.42f + 0.28f * breathe);
                GUI.DrawTexture(new Rect(plate.x + 2f * s, plate.y + 2f * s, plate.width - 4f * s, 3f * s), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(plate.x + 2f * s, plate.yMax - 5f * s, plate.width - 4f * s, 3f * s), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(plate.x + 2f * s, plate.y + 2f * s, 3f * s, plate.height - 4f * s), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(plate.xMax - 5f * s, plate.y + 2f * s, 3f * s, plate.height - 4f * s), Texture2D.whiteTexture);
                GUI.color = Color.white;
                DrawCopy(plate, s, reveal);
                float bulbFrac = Mathf.Clamp(15f * s / Mathf.Max(1f, plate.width), 0.040f, 0.058f);
                DrawGiftMarquee(plate, s, t, 0.70f, -1f, bulbFrac, 12, 0f, true, 0.16f, 0f, card);
            }

            static void DrawCopy(Rect plate, float s, int reveal)
            {
                float insetX = Mathf.Max(12f * s, plate.width * 0.06f);
                float insetY = Mathf.Max(8f * s, plate.height * 0.06f);
                var block = new Rect(
                    plate.x + insetX,
                    plate.y + insetY,
                    plate.width - insetX * 2f,
                    Mathf.Max(48f, plate.height - insetY * 2f));
                float titleH = block.height * 0.34f;
                var titleR = new Rect(block.x, block.y, block.width, titleH);
                if (_title == null)
                    _title = new GUIStyle(GUI.skin.label)
                    {
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                        wordWrap = true
                    };
                int titleHi = Mathf.Max(28, Mathf.RoundToInt(60f * s));
                _title.fontSize = FitFontWrapped(_title, Headline, titleR.width * 0.96f, titleR.height * 0.92f, 16, titleHi);
                int titleInk = Mathf.Max(3, Mathf.RoundToInt(_title.fontSize * 0.14f));
                StampOutlined(titleR, Headline, _title, new Color(1f, 0.97f, 0.86f, 1f), 0, titleInk);

                if (_body == null)
                    _body = new GUIStyle(GUI.skin.label)
                    {
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleLeft,
                        wordWrap = true
                    };
                float rowH = (block.yMax - titleR.yMax) / Benefits.Length;
                int bodyHi = Mathf.Max(16, Mathf.RoundToInt(29f * s));
                var cream = new Color(1f, 0.97f, 0.88f, 1f);
                for (int i = 0; i < Benefits.Length; i++)
                {
                    if (i >= reveal) continue;
                    var row = new Rect(block.x, titleR.yMax + rowH * i, block.width, rowH);
                    float mark = Mathf.Min(rowH * 0.46f, 26f * s);
                    var mk = new Rect(row.x, row.center.y - mark * 0.5f, mark, mark);
                    DrawCheckMark(mk);
                    var lab = new Rect(mk.xMax + 8f * s, row.y, Mathf.Max(20f, row.xMax - mk.xMax - 8f * s), row.height);
                    _body.fontSize = FitFontWrapped(_body, Benefits[i], lab.width, lab.height * 0.88f, 13, bodyHi);
                    int ink = Mathf.Max(3, Mathf.RoundToInt(_body.fontSize * 0.16f));
                    StampOutlined(lab, Benefits[i], _body, cream, 0, ink);
                }
            }

            static void DrawBuy(Rect flower, bool held, float s, float shineU, bool sparkle)
            {
                float pulse = sparkle ? 1f + 0.035f * Mathf.Sin(Time.unscaledTime * 2.5f) : 1f;
                var sized = pulse > 1.001f ? ScaledAbout(flower, pulse) : flower;
                var disc = DrawPopupButton(sized, held, true);
                if (_buy == null)
                    _buy = new GUIStyle(GUI.skin.label)
                    {
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                        wordWrap = false
                    };
                string price = NoAds.PriceLabel();
                bool priced = !string.IsNullOrEmpty(price);
                var verb = priced
                    ? new Rect(disc.x, disc.y + disc.height * 0.22f, disc.width, disc.height * 0.30f)
                    : new Rect(disc.x, disc.y + disc.height * 0.22f, disc.width, disc.height * 0.52f);
                int hi = Mathf.Max(18, Mathf.RoundToInt(36f * s));
                _buy.fontSize = FitFont(_buy, BuyLabel, verb.width * 0.78f, verb.height * 0.92f, 12, hi);
                int ink = Mathf.Clamp(Mathf.RoundToInt(_buy.fontSize * 0.12f), 2, 6);
                StampOutlined(verb, BuyLabel, _buy, new Color(0.28f, 0.12f, 0.04f), 2, ink);
                if (priced)
                {
                    var priceR = new Rect(disc.x + disc.width * 0.08f, verb.yMax, disc.width * 0.84f, disc.height * 0.22f);
                    int phi = Mathf.Max(12, Mathf.RoundToInt(20f * s));
                    _buy.fontSize = FitFont(_buy, price, priceR.width, priceR.height * 0.92f, 10, phi);
                    StampOutlined(priceR, price, _buy, new Color(0.28f, 0.12f, 0.04f), 1, 2);
                }
                if (shineU >= 0f && shineU <= 1f) DrawVipShine(disc, shineU);
                if (!sparkle) return;
                var spark = SpriteCatalog.Sparkle;
                if (spark == null || spark.texture == null) return;
                float sz = disc.width * 0.22f;
                var sr = new Rect(disc.xMax - sz * 0.7f, disc.y - sz * 0.08f, sz, sz);
                var prev = GUI.matrix;
                GUIUtility.RotateAroundPivot(Time.unscaledTime * 40f, sr.center);
                GUI.color = new Color(1f, 0.96f, 0.75f, 0.9f);
                GUI.DrawTexture(sr, spark.texture, ScaleMode.ScaleToFit, true);
                GUI.matrix = prev;
                GUI.color = Color.white;
            }

            static void DrawVipShine(Rect disc, float u)
            {
                float x = Mathf.Lerp(disc.x, disc.xMax - disc.width * 0.12f, Mathf.Clamp01(u));
                var band = new Rect(x, disc.y + disc.height * 0.22f, disc.width * 0.10f, disc.height * 0.52f);
                var prev = GUI.matrix;
                GUIUtility.RotateAroundPivot(-18f, band.center);
                GUI.color = new Color(1f, 0.97f, 0.82f, 0.42f * Mathf.Sin(Mathf.Clamp01(u) * Mathf.PI));
                GUI.DrawTexture(band, Texture2D.whiteTexture);
                GUI.matrix = prev;
                GUI.color = Color.white;
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
                int hi = Mathf.Max(13, Mathf.RoundToInt(16f * s));
                _link.fontSize = FitFont(_link, RestoreLabel, link.width * 0.96f, link.height * 0.72f, 11, hi);
                int ink = Mathf.Max(3, Mathf.RoundToInt(_link.fontSize * 0.18f));
                var col = held ? new Color(1f, 1f, 0.94f, 1f) : new Color(1f, 0.97f, 0.90f, 1f);
                StampOutlined(link, RestoreLabel, _link, col, 0, ink);
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
