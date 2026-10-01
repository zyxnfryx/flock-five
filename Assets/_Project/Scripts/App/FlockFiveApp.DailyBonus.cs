using UnityEngine;

namespace FlockFive
{
    public sealed partial class FlockFiveApp
    {
        // Armed until claimed or dismissed this launch. Opens only when the streak
        // toast, hive lesson, poker lesson, and VIP card are clear.
        bool _dailyArmed;
        bool _dailyOpen;
        bool _dailyDismissed;
#if UNITY_EDITOR
        bool _dailyShotQuiet;
#endif

        static GUIStyle _dailyTitle;
        static GUIStyle _dailyClaim;
        static GUIStyle _dailyTile;
        static GUIStyle _dailyLine;
        static int _dailyFitKey = int.MinValue;
        static int _dailyTitlePx = 22;
        static int _dailyClaimPx = 22;
        static int _dailyPayPx = 14;
        static int _dailyTilePx = 12;
        static int _dailyLinePx = 12;
        static int _dailyBonusPx = 12;
        static int _dailyDigitPx = 14;

        void ArmDailyBonus()
        {
#if UNITY_EDITOR
            if (DailyShotSentinel()) _dailyShotQuiet = true;
#endif
            DailyBonus.Boot();
            if (DailyBonus.RefreshDay()) _dailyDismissed = false;
            DailyReminder.Reschedule(DailyBonus.Streak, DailyBonus.ClaimedToday, DailyBonus.AtRisk);
#if UNITY_EDITOR
            if (_dailyShotQuiet || _pokerPlayrun)
            {
                _dailyArmed = false;
                _dailyOpen = false;
                return;
            }
#endif
            if (_dailyDismissed) return;
            _dailyArmed = DailyBonus.OfferReady;
        }

        void TickDailyBonus()
        {
            DailyReminder.Tick();
            if (DailyBonus.RefreshDay())
            {
                _dailyDismissed = false;
                _dailyArmed = DailyBonus.OfferReady;
            }
            if (_dailyDismissed)
            {
                _dailyOpen = false;
                return;
            }
#if UNITY_EDITOR
            if (_dailyShotQuiet || _pokerPlayrun || EditorShotLive)
            {
                _dailyOpen = false;
                return;
            }
#endif
            // Same queue as the poker lesson: streak sign, then hive, then poker.
            // VIP on screen holds the card; it does not consume the claim.
            bool wait = !_splash || _home != HomeFace.Splash
                || _streakSlide >= 0f
                || _hiveIntro || _hiveIntroLive
                || _pokerIntro || _pokerIntroLive
                || VipOffer.IsOpen;
            _dailyOpen = !wait && _dailyArmed && DailyBonus.OfferReady;
        }

        void ResumeDailyReminder()
        {
            DailyBonus.Boot();
            if (DailyBonus.RefreshDay())
            {
                _dailyDismissed = false;
                _dailyArmed = DailyBonus.OfferReady;
            }
            DailyReminder.Reschedule(DailyBonus.Streak, DailyBonus.ClaimedToday, DailyBonus.AtRisk);
        }

        void DrawDailyBonus(float s)
        {
            if (!_dailyOpen || !DailyBonus.OfferReady) return;
#if UNITY_EDITOR
            if (_dailyShotQuiet || EditorShotLive) return;
#endif
            var safe = Screen.safeArea;
            float top = TopHud();
            float xSz = Mathf.Max(48f * s, 44f);
            var xBtn = new Rect(
                Screen.width - Mathf.Max(14f, Screen.width - safe.xMax + 8f) - xSz,
                top, xSz, xSz);

            DailyLayout(s, out var card, out var flower, out var plate);
            var disc = FlowerDisc(flower, 0f);
            float hitGrow = 18f * s;
            var claimHit = new Rect(disc.x - hitGrow, disc.y - hitGrow * 0.65f, disc.width + hitGrow * 2f, disc.height + hitGrow * 1.3f);
            if (claimHit.height < 52f * s)
            {
                float extra = 52f * s - claimHit.height;
                claimHit.y -= extra * 0.5f;
                claimHit.height += extra;
            }

            float x0 = Mathf.Min(card.x, flower.x);
            float y0 = Mathf.Min(card.y, flower.y);
            float x1 = Mathf.Max(card.xMax, flower.xMax);
            float y1 = Mathf.Max(card.yMax, flower.yMax);
            var swallow = new Rect(x0 - 12f, y0 - 12f, (x1 - x0) + 24f, (y1 - y0) + 24f);

            bool xHit = HitPad(xBtn, out bool xHeld);
            bool claim = HitPad(claimHit, out bool claimHeld);
            HitPad(swallow, out _);
            bool outside = HitPad(new Rect(0f, 0f, Screen.width, Screen.height), out _);
            if (outside && swallow.Contains(Event.current.mousePosition)) outside = false;

            float t = Time.unscaledTime;
            float breathe = 0.5f + 0.5f * Mathf.Sin(t * 2.05f);
            var glow = GlowTex();
            GUI.color = new Color(0.04f, 0.03f, 0.02f, 0.72f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float aura = card.width * (0.08f + 0.03f * breathe);
            GUI.color = new Color(1f, 0.78f, 0.22f, 0.28f + 0.18f * breathe);
            GUI.DrawTexture(new Rect(card.x - aura, card.y - aura * 0.55f, card.width + aura * 2f, card.height + aura), glow, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;

            var wood = SpriteCatalog.Blanket != null ? SpriteCatalog.Blanket.texture : Texture2D.whiteTexture;
            GUI.color = new Color(0.10f, 0.06f, 0.03f, 0.45f);
            GUI.DrawTexture(new Rect(card.x + 6f, card.y + 12f, card.width, card.height), wood, ScaleMode.StretchToFill, true);
            GUI.color = new Color(0.45f, 0.26f, 0.10f, 1f);
            GUI.DrawTexture(card, wood, ScaleMode.StretchToFill, true);
            GUI.color = Color.white;
            var art = SpriteCatalog.AdCard != null ? SpriteCatalog.AdCard.texture : null;
            if (art != null)
            {
                float artH = card.width * (501f / 780f);
                if (artH > card.height) artH = card.height;
                GUI.DrawTexture(new Rect(card.x, card.y, card.width, artH), art, ScaleMode.ScaleToFit, true);
                QuietFrameFlowers(new Rect(card.x, card.y, card.width, artH));
            }
            GUI.color = new Color(1f, 0.78f, 0.22f, 0.90f);
            float rim = Mathf.Max(3f, 4f * s);
            GUI.DrawTexture(new Rect(card.x, card.y, card.width, rim), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(card.x, card.yMax - rim, card.width, rim), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(card.x, card.y, rim, card.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(card.xMax - rim, card.y, rim, card.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.color = new Color(0.04f, 0.02f, 0.01f, 0.92f);
            GUI.DrawTexture(plate, Texture2D.whiteTexture);
            GUI.color = new Color(1f, 0.78f, 0.22f, 0.42f + 0.28f * breathe);
            GUI.DrawTexture(new Rect(plate.x + 2f * s, plate.y + 2f * s, plate.width - 4f * s, 3f * s), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(plate.x + 2f * s, plate.yMax - 5f * s, plate.width - 4f * s, 3f * s), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(plate.x + 2f * s, plate.y + 2f * s, 3f * s, plate.height - 4f * s), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(plate.xMax - 5f * s, plate.y + 2f * s, 3f * s, plate.height - 4f * s), Texture2D.whiteTexture);
            GUI.color = Color.white;

            EnsureDailyStyles();
            EnsureDailyFit(s, plate, disc);
            DrawDailyCopy(plate, s, breathe, glow);
            DrawDailyTiles(plate, s, breathe, glow);
            DrawGiftMarquee(plate, s, t);
            DrawDailyBadge(card, s, glow);
            DrawDailyClaim(flower, claimHeld, s, t);
            DrawGiftCloseX(xBtn, xHeld, s);
            GUI.color = Color.white;

            if (claim)
            {
                ClaimDaily();
                return;
            }
            if (xHit || outside) DismissDaily();
        }

        void ClaimDaily()
        {
            if (!DailyBonus.TryClaim(out int coins, out bool first)) return;
            Sfx.CardTap();
            Sfx.Clink();
            Haptics.Play(Haptics.Tier.Medium);
            BurstDailyCoins(coins);
            _dailyOpen = false;
            _dailyArmed = false;
            _dailyDismissed = true;
            if (first) DailyReminder.AskAfterFirstClaim(DailyBonus.Streak, DailyBonus.ClaimedToday, DailyBonus.AtRisk);
            else DailyReminder.Reschedule(DailyBonus.Streak, DailyBonus.ClaimedToday, DailyBonus.AtRisk);
        }

        void DismissDaily()
        {
            if (!_dailyOpen && !_dailyArmed) return;
            _dailyOpen = false;
            _dailyArmed = false;
            _dailyDismissed = true;
            Sfx.CardTap();
        }

        void BurstDailyCoins(int coins)
        {
            int n = coins >= 75 ? 8 : coins >= 35 ? 6 : 4;
            float sc = Mathf.Max(Screen.height / 720f, 1f);
            var pig = PiggyRect(sc);
            var dest = new Vector2(pig.x + pig.width * 0.5f, pig.y + pig.height * 0.42f);
            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.58f;
            for (int i = 0; i < n; i++)
            {
                float ang = i * 6.2831853f / n;
                _flies.Add(new CoinFly
                {
                    A = new Vector2(cx + Mathf.Cos(ang) * 40f, cy + Mathf.Sin(ang) * 16f),
                    B = dest,
                    T = 0f,
                    Delay = i * 0.05f,
                    Spin = ((i & 1) == 0 ? 360f : -360f)
                });
            }
            _pigBurst = 1f;
            _pigJiggle = 1f;
        }

        static void DailyLayout(float s, out Rect card, out Rect flower, out Rect plate)
        {
            float cardW = Mathf.Min(Screen.width * 0.92f, 640f * s);
            float cardH = cardW * 0.86f;
            float flowerSz = Mathf.Min(Screen.width * 0.50f, 280f * s);
            float overlap = flowerSz * 0.36f;
            float stack = cardH + flowerSz - overlap;
            float top = TopHud() + 4f * s;
            float bot = Screen.height - Mathf.Max(8f, Screen.safeArea.yMin + 4f);
            float y = top + Mathf.Max(0f, (bot - top - stack) * 0.36f);
            if (y + stack > bot) y = Mathf.Max(top, bot - stack);
            card = new Rect((Screen.width - cardW) * 0.5f, y, cardW, cardH);
            flower = new Rect((Screen.width - flowerSz) * 0.5f, card.yMax - overlap, flowerSz, flowerSz);
            float plateTop = card.y + cardH * 0.13f;
            // Stop above the claim disc. The petals may cross the frame; the label must not.
            float plateBot = flower.y + flower.height * 0.08f;
            if (plateBot < plateTop + 72f * s) plateBot = plateTop + 72f * s;
            plate = new Rect(card.x + cardW * 0.08f, plateTop, cardW * 0.84f, plateBot - plateTop);
        }

        static void EnsureDailyStyles()
        {
            if (_dailyTitle != null) return;
            _dailyTitle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false
            };
            _dailyClaim = new GUIStyle(_dailyTitle);
            _dailyTile = new GUIStyle(_dailyTitle);
            _dailyLine = new GUIStyle(_dailyTitle);
        }

        static void EnsureDailyFit(float s, Rect plate, Rect disc)
        {
            int key = Screen.width * 73856093 ^ Screen.height * 19349663
                ^ DailyBonus.PayLine.Length * 83492791
                ^ DailyBonus.StreakLine.Length * 50331653
                ^ DailyBonus.BonusLine.Length * 2718281;
            if (key == _dailyFitKey) return;
            _dailyFitKey = key;
            float titleH = plate.height * 0.16f;
            var titleR = new Rect(plate.x, plate.y + plate.height * 0.04f, plate.width, titleH);
            _dailyTitlePx = FitFont(_dailyTitle, "Daily Bonus", titleR.width * 0.90f, titleR.height * 0.92f, 14, Mathf.Max(18, Mathf.RoundToInt(34f * s)));
            _dailyClaimPx = FitFont(_dailyClaim, "Claim", disc.width * 0.72f, disc.height * 0.42f, 12, Mathf.Max(16, Mathf.RoundToInt(32f * s)));
            _dailyPayPx = FitFont(_dailyClaim, DailyBonus.PayLine, disc.width * 0.70f, disc.height * 0.28f, 10, Mathf.Max(12, Mathf.RoundToInt(18f * s)));
            float gap = Mathf.Max(3f, 4f * s);
            float unit = (plate.width * 0.94f - gap * 6f) / 7.28f;
            _dailyTilePx = FitFont(_dailyTile, "$75", unit * 0.88f, plate.height * 0.22f, 8, Mathf.Max(10, Mathf.RoundToInt(16f * s)));
            float badge = Mathf.Clamp(58f * s, 46f, 76f);
            _dailyDigitPx = FitFont(_dailyLine, DailyBonus.StreakDigits, badge * 0.62f, badge * 0.42f, 10, Mathf.Max(12, Mathf.RoundToInt(20f * s)));
            _dailyLinePx = FitFont(_dailyLine, DailyBonus.StreakLine, badge * 1.7f, 22f * s, 9, Mathf.Max(11, Mathf.RoundToInt(15f * s)));
            string bonus = DailyBonus.BonusLine;
            if (bonus.Length > 0)
                _dailyBonusPx = FitFont(_dailyLine, bonus, plate.width * 0.80f, plate.height * 0.12f, 9, Mathf.Max(11, Mathf.RoundToInt(16f * s)));
        }

        static void DrawDailyCopy(Rect plate, float s, float breathe, Texture2D glow)
        {
            float titleH = plate.height * 0.16f;
            var titleR = new Rect(plate.x + plate.width * 0.06f, plate.y + plate.height * 0.035f, plate.width * 0.88f, titleH);
            GUI.color = new Color(1f, 0.72f, 0.16f, 0.36f + 0.18f * breathe);
            GUI.DrawTexture(new Rect(titleR.x, titleR.y - 3f * s, titleR.width, titleR.height + 6f * s), glow, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
            _dailyTitle.fontSize = _dailyTitlePx;
            int ink = Mathf.Max(2, Mathf.RoundToInt(_dailyTitlePx * 0.10f));
            StampOutlined(titleR, "Daily Bonus", _dailyTitle, new Color(1f, 0.96f, 0.72f), 1, ink);
            string bonus = DailyBonus.BonusLine;
            if (bonus.Length == 0) return;
            var bonusR = new Rect(titleR.x, titleR.yMax, titleR.width, plate.height * 0.09f);
            _dailyLine.fontSize = _dailyBonusPx;
            StampOutlined(bonusR, bonus, _dailyLine, new Color(1f, 0.86f, 0.42f), 1, 1);
        }

        static void DrawDailyTiles(Rect plate, float s, float breathe, Texture2D glow)
        {
            string bonus = DailyBonus.BonusLine;
            float top = plate.y + plate.height * (bonus.Length > 0 ? 0.30f : 0.24f);
            float bot = plate.yMax - plate.height * 0.06f;
            var row = new Rect(plate.x + plate.width * 0.03f, top, plate.width * 0.94f, Mathf.Max(24f, bot - top));
            float gap = Mathf.Max(3f, 4f * s);
            const float big = 1.28f;
            float unit = (row.width - gap * 6f) / (6f + big);
            if (unit < 4f) return;
            float x = row.x;
            int today = DailyBonus.Cycle;
            float baseH = row.height * 0.82f;
            var crown = SpriteCatalog.Crown;
            var crownTex = crown != null ? crown.texture : null;
            for (int i = 0; i < 7; i++)
            {
                bool day7 = i == 6;
                bool now = i == today;
                bool past = i < today;
                float w = day7 ? unit * big : unit;
                float h = day7 ? row.height : baseH;
                var tile = new Rect(x, row.yMax - h, w, h);
                DrawDailyTile(tile, i, now, past, day7, s, breathe, glow, crownTex);
                x += w + gap;
            }
        }

        static void DrawDailyTile(Rect tile, int day, bool now, bool past, bool day7, float s, float breathe, Texture2D glow, Texture crownTex)
        {
            if (now)
            {
                float pad = 5f * s + 2f * breathe;
                GUI.color = new Color(1f, 0.82f, 0.28f, 0.34f + 0.20f * breathe);
                GUI.DrawTexture(new Rect(tile.x - pad, tile.y - pad, tile.width + pad * 2f, tile.height + pad * 2f), glow, ScaleMode.ScaleToFit, true);
            }
            Color fill = now
                ? new Color(0.98f, 0.78f, 0.28f, 1f)
                : past
                    ? new Color(0.28f, 0.16f, 0.07f, 0.92f)
                    : new Color(0.14f, 0.08f, 0.04f, 0.88f);
            if (day7 && !now) fill = new Color(0.62f, 0.40f, 0.12f, 0.96f);
            GUI.color = fill;
            GUI.DrawTexture(tile, Texture2D.whiteTexture);
            GUI.color = new Color(1f, 0.86f, 0.42f, now || day7 ? 1f : 0.45f);
            float edge = Mathf.Max(2f, (now ? 3.5f : 2f) * s);
            GUI.DrawTexture(new Rect(tile.x, tile.y, tile.width, edge), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(tile.x, tile.yMax - edge, tile.width, edge), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(tile.x, tile.y, edge, tile.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(tile.xMax - edge, tile.y, edge, tile.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (day7 && crownTex != null)
            {
                float cs = tile.width * 0.46f;
                GUI.DrawTexture(new Rect(tile.center.x - cs * 0.5f, tile.y - cs * 0.55f, cs, cs * 0.72f), crownTex, ScaleMode.ScaleToFit, true);
            }
            var amt = new Rect(tile.x + 1f, tile.y + tile.height * (day7 ? 0.28f : 0.16f), tile.width - 2f, tile.height * 0.46f);
            _dailyTile.fontSize = _dailyTilePx;
            Color ink = now
                ? new Color(0.28f, 0.12f, 0.04f, 1f)
                : new Color(1f, 0.94f, 0.72f, past ? 0.55f : 0.78f);
            StampOutlined(amt, DailyBonus.TileLabel(day), _dailyTile, ink, 1, 1);
            if (!past) return;
            var mark = new Rect(tile.x + tile.width * 0.28f, tile.yMax - tile.height * 0.32f, tile.width * 0.44f, tile.height * 0.20f);
            DrawDailyCheck(mark, new Color(1f, 0.86f, 0.36f, 0.95f));
        }

        static void DrawDailyCheck(Rect r, Color c)
        {
            var prev = GUI.matrix;
            var p = new Vector2(r.x + r.width * 0.32f, r.center.y);
            float thick = Mathf.Max(2f, r.height * 0.42f);
            GUI.color = c;
            GUIUtility.RotateAroundPivot(42f, p);
            GUI.DrawTexture(new Rect(p.x - r.width * 0.22f, p.y - thick * 0.5f, r.width * 0.42f, thick), Texture2D.whiteTexture);
            GUI.matrix = prev;
            var q = new Vector2(r.x + r.width * 0.58f, r.y + r.height * 0.28f);
            GUIUtility.RotateAroundPivot(-48f, q);
            GUI.DrawTexture(new Rect(q.x - r.width * 0.06f, q.y - thick * 0.5f, r.width * 0.62f, thick), Texture2D.whiteTexture);
            GUI.matrix = prev;
            GUI.color = Color.white;
        }

        static void DrawDailyBadge(Rect card, float s, Texture2D glow)
        {
            float b = Mathf.Clamp(58f * s, 46f, 76f);
            var flame = new Rect(card.x + 6f * s, card.y - b * 0.28f, b, b);
            float minY = TopHud();
            if (flame.y < minY) flame.y = minY;
            GUI.color = new Color(1f, 0.55f, 0.12f, 0.40f);
            float pad = b * 0.18f;
            GUI.DrawTexture(new Rect(flame.x - pad, flame.y - pad * 0.2f, flame.width + pad * 2f, flame.height + pad * 2f), glow, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
            int frame = (int)(Time.unscaledTime * 8f) % 6;
            if (frame < 0) frame = 0;
            var spr = SpriteCatalog.Flame(frame);
            if (spr != null && spr.texture != null)
                GUI.DrawTexture(flame, spr.texture, ScaleMode.ScaleToFit, true);
            else
            {
                GUI.color = new Color(1f, 0.62f, 0.16f, 1f);
                GUI.DrawTexture(flame, glow, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
            }
            var num = new Rect(flame.x, flame.y + flame.height * 0.34f, flame.width, flame.height * 0.40f);
            _dailyLine.fontSize = _dailyDigitPx;
            StampOutlined(num, DailyBonus.StreakDigits, _dailyLine, new Color(1f, 0.97f, 0.82f), 1, 1);
            float lineW = Mathf.Max(b * 1.8f, 96f * s);
            var line = new Rect(flame.x - 4f * s, flame.yMax - 2f * s, lineW, Mathf.Max(16f, 18f * s));
            _dailyLine.fontSize = _dailyLinePx;
            StampOutlined(line, DailyBonus.StreakLine, _dailyLine, new Color(1f, 0.90f, 0.55f), 1, 1);
        }

        static void DrawDailyClaim(Rect flower, bool held, float s, float t)
        {
            var bloom = SpriteCatalog.PlayFlower;
            float sink = held ? flower.height * 0.028f : 0f;
            if (bloom != null && bloom.texture != null)
            {
                GUI.color = new Color(0.10f, 0.06f, 0.03f, 0.38f);
                GUI.DrawTexture(new Rect(flower.x + 5f, flower.y + 12f, flower.width, flower.height), bloom.texture, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
                if (!held) DrawFlowerHalo(flower, 0f);
                GUI.DrawTexture(flower, bloom.texture, ScaleMode.ScaleToFit, true);
                if (!held) DrawFlowerShimmer(flower, 0f);
                if (held) DrawDiscPress(flower, sink);
            }
            var disc = FlowerDisc(flower, sink);
            if (bloom == null || bloom.texture == null)
            {
                GUI.color = new Color(0.93f, 0.68f, 0.18f, 1f);
                GUI.DrawTexture(disc, Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            var payR = new Rect(disc.x, disc.y + disc.height * 0.08f, disc.width, disc.height * 0.30f);
            var labR = new Rect(disc.x, disc.y + disc.height * 0.36f, disc.width, disc.height * 0.46f);
            _dailyClaim.fontSize = _dailyPayPx;
            StampOutlined(payR, DailyBonus.PayLine, _dailyClaim, new Color(0.45f, 0.22f, 0.06f), 1, 1);
            _dailyClaim.fontSize = _dailyClaimPx;
            int ink = Mathf.Clamp(Mathf.RoundToInt(_dailyClaimPx * 0.12f), 2, 6);
            StampOutlined(labR, "Claim", _dailyClaim, new Color(0.28f, 0.12f, 0.04f), 2, ink);

            var spark = SpriteCatalog.Sparkle;
            var tex = spark != null && spark.texture != null ? spark.texture : null;
            if (tex == null) return;
            float pulse = 0.78f + 0.22f * Mathf.Sin(t * 5.4f);
            float sz = disc.width * 0.30f * pulse;
            var sr = new Rect(disc.xMax - sz * 0.78f, disc.y - sz * 0.18f, sz, sz);
            var prev = GUI.matrix;
            GUIUtility.RotateAroundPivot(t * 40f, sr.center);
            GUI.color = new Color(1f, 0.96f, 0.75f, 0.92f);
            GUI.DrawTexture(sr, tex, ScaleMode.ScaleToFit, true);
            GUI.matrix = prev;
            GUI.color = Color.white;
        }

#if UNITY_EDITOR
        static bool DailyShotSentinel()
        {
            return EditorShotLive
                || System.IO.File.Exists("/tmp/flock-five-shot")
                || System.IO.File.Exists("/tmp/flock-five-level6")
                || System.IO.File.Exists("/tmp/flock-five-streak-shot")
                || System.IO.File.Exists("/tmp/flock-five-consumer-tour")
                || System.IO.File.Exists("/tmp/flock-five-poker-run")
                || System.IO.File.Exists("/tmp/flock-five-poker-faces")
                || System.IO.File.Exists("/tmp/flock-five-poker-feel")
                || System.IO.File.Exists("/tmp/flock-five-poker-stamp")
                || System.IO.File.Exists("/tmp/flock-five-poker-punches")
                || System.IO.File.Exists("/tmp/flock-five-gift-shot")
                || System.IO.File.Exists("/tmp/flock-five-hand-qa")
                || System.IO.File.Exists("/tmp/flock-five-hive-inspect")
                || System.IO.File.Exists("/tmp/flock-five-birds-shot")
                || System.IO.File.Exists("/tmp/flock-five-leaves-shot")
                || System.IO.File.Exists("/tmp/flock-five-sparrow-shot")
                || System.IO.File.Exists("/tmp/flock-five-ice-shot")
                || System.IO.File.Exists("/tmp/flock-five-storm-shot")
                || System.IO.File.Exists("/tmp/flock-five-finale")
                || System.IO.File.Exists("/tmp/flock-five-oasis-shot");
        }
#endif
    }
}
