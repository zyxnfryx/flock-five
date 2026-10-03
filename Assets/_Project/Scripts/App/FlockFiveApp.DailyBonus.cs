using UnityEngine;

namespace FlockFive
{
    public sealed partial class FlockFiveApp
    {
        // The card opens only when the player taps the rail button. The lesson glove
        // keeps tapping that button and does not open it. Welcome does not open it either.
        // The ask card is the in-game reminder prompt after a claim.
        bool _dailyOpen;
        bool _dailyAskOpen;
        float _dailyPopAt = -1f;
        float _dailyAskAt = -1f;
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
        static int _dailyTilePx = 12;
        static int _dailyLinePx = 12;
        static int _dailyBonusPx = 12;
        static int _railDigitPx;
        static int _railDigitKey = int.MinValue;
        static GUIStyle _dailyAsk;
        static GUIStyle _dailyAskBtn;
        static int _dailyAskFit = int.MinValue;
        static int _dailyAskBodyPx = 18;
        static int _dailyAskYesPx = 16;
        static int _dailyAskNoPx = 16;
        static Texture2D _dailyMedal;

        const string DailyAskCopy = "Want a reminder so you never miss your streak?";
        const string DailyAskYes = "Yes please";
        const string DailyAskNo = "Not now";

        const string WelcomeBonusKey = "flockfive.welcome.bonus";
        const string WelcomePendingKey = "flockfive.welcome.pending";
        const int WelcomeBonusCoins = 1000;
        const string WelcomeTitle = "Welcome bonus!";
        static string WelcomeAmountText() => Money.Format(WelcomeBonusCoins);
        const string WelcomeLine = "Ready to remove ads and support the game? Tap the VIP button anytime.";
        const string WelcomeThanks = "Thanks!";

        // Screw lip of fx_ad_bulb: metal ends and the glass starts about 28% down the art.
        const float DailyBulbPin = 0.28f;
        // Shared marquee glass, about 27% smaller (inside the 25–30% shrink).
        const float DailyBulbShrink = 0.73f;

        bool _welcomeOpen;
        float _welcomeAt = -1f;
        bool _welcomeGlove;
        float _welcomeGloveUntil;
        static int _welcomeFit = int.MinValue;
        static int _welcomeTitlePx = 26;
        static int _welcomeAmtPx = 34;
        static int _welcomeLinePx = 16;
        static int _welcomeBtnPx = 18;

        void ArmDailyBonus()
        {
#if UNITY_EDITOR
            if (DailyShotSentinel()) _dailyShotQuiet = true;
#endif
            DailyBonus.Boot();
            DailyBonus.RefreshDay();
            DailyReminder.Reschedule(DailyBonus.Streak, DailyBonus.ClaimedToday, DailyBonus.AtRisk);
#if UNITY_EDITOR
            if (_dailyShotQuiet || _pokerPlayrun)
            {
                _dailyOpen = false;
                _dailyAskOpen = false;
                _welcomeOpen = false;
                _welcomeGlove = false;
                return;
            }
#endif
            if (!_dailyAskOpen && PlayerPrefs.GetInt(WelcomePendingKey, 0) != 0)
                OfferWelcome();
        }

        void TickDailyBonus()
        {
            DailyReminder.Tick();
            DailyBonus.RefreshDay();
#if UNITY_EDITOR
            if (_dailyShotQuiet || _pokerPlayrun || EditorShotLive)
            {
                _dailyOpen = false;
                _dailyAskOpen = false;
                _welcomeOpen = false;
                _welcomeGlove = false;
            }
#endif
        }

        void ResumeDailyReminder()
        {
            DailyBonus.Boot();
            DailyBonus.RefreshDay();
            DailyReminder.Reschedule(DailyBonus.Streak, DailyBonus.ClaimedToday, DailyBonus.AtRisk);
        }

        void OpenDailyCard()
        {
            if (_dailyOpen || _dailyAskOpen || _welcomeOpen) return;
#if UNITY_EDITOR
            if (_dailyShotQuiet || EditorShotLive) return;
#endif
            DailyBonus.Boot();
            _dailyOpen = true;
            _dailyPopAt = Time.unscaledTime;
            SfxLibrary.SeatGroove(true);
            Sfx.CardTap();
        }

        void DrawDailyRail(Rect r, float s, bool held)
        {
            if (r.width < 2f) return;
            DailyBonus.Boot();
            float sink = held ? r.height * 0.045f : 0f;
            var plate = new Rect(r.x, r.y + sink, r.width, r.height);
            var tex = DailyMedalTex();
            var glow = GlowTex();
            bool ready = DailyBonus.OfferReady;
            float breathe = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.6f);
            if (ready)
            {
                float bloom = plate.width * (0.05f + 0.025f * breathe);
                GUI.color = new Color(0.90f, 0.18f, 0.12f, 0.16f + 0.14f * breathe);
                GUI.DrawTexture(new Rect(plate.x - bloom, plate.y - bloom, plate.width + bloom * 2f, plate.height + bloom * 2f), glow, ScaleMode.ScaleToFit, true);
            }
            GUI.color = new Color(0.08f, 0.05f, 0.02f, 0.34f);
            GUI.DrawTexture(new Rect(plate.x + 3f, plate.y + 5f, plate.width, plate.height), tex, ScaleMode.ScaleToFit, true);
            GUI.color = held ? new Color(0.90f, 0.88f, 0.82f, 1f) : Color.white;
            GUI.DrawTexture(plate, tex, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;

            // Top-right of the round medal. Art is 289×672, so a square letterboxes the fire.
            // The digit sits on the flame's middle (the bright belly, not the thin tip).
            float fh = plate.width * 0.92f;
            float fw = fh * (289f / 672f);
            const float belly = 0.65f;
            float markX = plate.xMax - plate.width * 0.20f;
            float markY = plate.y + plate.width * 0.18f;
            float lift = plate.width * 0.06f;
            var flame = new Rect(markX - fw * 0.5f, markY - fh * belly - lift, fw, fh);
            int frame = (int)(Time.unscaledTime * 8f) % 6;
            if (frame < 0) frame = 0;
            if (DailyBonus.FlameLit)
            {
                var spr = SpriteCatalog.Flame(frame);
                if (spr != null && spr.texture != null)
                    GUI.DrawTexture(flame, spr.texture, ScaleMode.ScaleToFit, true);
            }
            if (DailyBonus.Streak >= 1)
            {
                EnsureDailyStyles();
                float numH = fh * 0.32f;
                float numW = Mathf.Max(fw * 2.2f, plate.width * 0.55f);
                float bodyX = flame.center.x;
                float bodyY = flame.center.y;
                var num = new Rect(bodyX - numW * 0.5f, bodyY - numH * 0.5f, numW, numH);
                _dailyLine.fontSize = RailDigitPx(num.width, num.height);
                StampOutlined(num, DailyBonus.StreakDigits, _dailyLine, new Color(1f, 0.97f, 0.86f), 0, 2);
            }
            if (!ready) return;
            float dot = plate.width * (0.15f + 0.02f * breathe);
            float cx = plate.xMax - plate.width * 0.22f;
            float cy = plate.yMax - plate.width * 0.22f;
            DrawNotifyBadge(cx, cy, dot, null);
        }

        static int RailDigitPx(float w, float h)
        {
            int key = Mathf.RoundToInt(w) * 397 ^ Mathf.RoundToInt(h) * 17 ^ DailyBonus.StreakDigits.Length * 13;
            if (key == _railDigitKey) return _railDigitPx;
            _railDigitKey = key;
            EnsureDailyStyles();
            _railDigitPx = FitFont(_dailyLine, DailyBonus.StreakDigits, w * 0.92f, h * 0.92f, 10, 36);
            return _railDigitPx;
        }

        static Texture2D DailyMedalTex()
        {
            if (_dailyMedal != null) return _dailyMedal;
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = "DailyMedal"
            };
            var px = new Color32[n * n];
            float c = (n - 1) * 0.5f;
            float rad = c - 1.2f;
            for (int y = 0; y < n; y++)
            {
                float ny = (y + 0.5f - c) / rad;
                for (int x = 0; x < n; x++)
                {
                    float nx = (x + 0.5f - c) / rad;
                    float dist = Mathf.Sqrt(nx * nx + ny * ny);
                    float edge = Mathf.Clamp01((1.02f - dist) * rad * 0.45f);
                    if (edge <= 0f) continue;
                    Color col = DailyMedalPixel(nx, ny, dist);
                    col.a *= edge;
                    px[y * n + x] = col;
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _dailyMedal = tex;
            return tex;
        }

        // Gold ring, cream gift, red ribbon. nx/ny are radius units, +y up.
        static Color DailyMedalPixel(float nx, float ny, float dist)
        {
            float up = Mathf.Clamp01(0.5f + ny * 0.5f);
            if (dist > 0.80f)
            {
                float k = Mathf.Clamp01((dist - 0.80f) / 0.20f);
                Color rim = Color.Lerp(new Color(1f, 0.90f, 0.55f, 1f), new Color(0.55f, 0.30f, 0.08f, 1f), k);
                rim = Color.Lerp(rim, new Color(1f, 0.96f, 0.78f, 1f), (1f - k) * up * 0.45f);
                return rim;
            }
            Color face = Color.Lerp(new Color(0.42f, 0.18f, 0.07f, 1f), new Color(0.28f, 0.12f, 0.05f, 1f), ny * 0.5f + 0.5f);
            bool box = nx > -0.36f && nx < 0.36f && ny > -0.42f && ny < 0.28f;
            if (box)
            {
                float shade = Mathf.Clamp01((0.28f - ny) / 0.70f);
                face = Color.Lerp(new Color(1f, 0.96f, 0.84f, 1f), new Color(0.90f, 0.78f, 0.52f, 1f), shade);
                bool ribbon = Mathf.Abs(nx) < 0.09f || Mathf.Abs(ny + 0.04f) < 0.075f;
                if (ribbon)
                    face = Color.Lerp(new Color(0.55f, 0.08f, 0.10f, 1f), new Color(0.86f, 0.18f, 0.16f, 1f), up);
            }
            float bowL = (nx + 0.16f) * (nx + 0.16f) + (ny - 0.40f) * (ny - 0.40f);
            float bowR = (nx - 0.16f) * (nx - 0.16f) + (ny - 0.40f) * (ny - 0.40f);
            float knot = nx * nx * 1.8f + (ny - 0.28f) * (ny - 0.28f);
            if (bowL < 0.018f || bowR < 0.018f || knot < 0.012f)
                face = new Color(0.78f, 0.12f, 0.14f, 1f);
            if ((bowL > 0.010f && bowL < 0.016f) || (bowR > 0.010f && bowR < 0.016f))
                face = Color.Lerp(face, new Color(1f, 0.55f, 0.52f, 1f), 0.65f);
            return face;
        }

        void DrawDailyBonus(float s)
        {
            if (!_dailyOpen) return;
#if UNITY_EDITOR
            if (_dailyShotQuiet || EditorShotLive) return;
#endif
            var safe = Screen.safeArea;
            float top = TopHud();
            float xSz = Mathf.Max(48f * s, 44f);
            var xBtn = new Rect(
                Screen.width - Mathf.Max(14f, Screen.width - safe.xMax + 8f) - xSz,
                top, xSz, xSz);

            DailyLayout(s, out var card, out var flower, out var board, out float band);
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

            // Hits stay on the resting layout. The picture eases in around the same pivot.
            float k = DailyPop(_dailyPopAt);
            var pivot = DailyPivot(card, flower);
            var prev = GUI.matrix;
            if (k < 0.999f)
                GUIUtility.ScaleAroundPivot(new Vector2(k, k), pivot);

            DrawDailyFrame(card, board, band, s, breathe, glow);
            EnsureDailyStyles();
            DailyRows(s, board, out var titleR, out var streakR, out var bonusR, out var row, out float gap, out float unit);
            EnsureDailyFit(s, band, titleR, streakR, bonusR, disc, unit, row.height);
            DrawDailyCopy(titleR, streakR, bonusR, s, breathe, glow);
            DrawDailyTiles(row, gap, unit, s, breathe, glow);
            GUI.matrix = prev;

            // Screw lip on the pop-up's outer edge. Glass hangs outside, smaller than the old ring.
            // The claim flower paints after so a bottom bulb cannot cover the word.
            DrawPopupBulbs(card, board, band, s, t, pivot, k, true);
            if (k < 0.999f)
                GUIUtility.ScaleAroundPivot(new Vector2(k, k), pivot);
            DrawDailyClaim(flower, claimHeld, s, t);
            GUI.matrix = prev;
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
            if (!DailyBonus.TryClaim(out int coins, out bool firstEver))
            {
                if (DailyBonus.ClaimedToday) ClunkClaimed();
                return;
            }
            StopClaimGroove();
            Sfx.CardTap();
            Sfx.Clink();
            Haptics.Play(Haptics.Tier.Medium);
            BurstDailyCoins(coins);
            _dailyOpen = false;
            _dailyPopAt = -1f;
            DismissDailyIntro();
            DailyReminder.Reschedule(DailyBonus.Streak, DailyBonus.ClaimedToday, DailyBonus.AtRisk);
            if (DailyReminder.PromptDue())
            {
                _dailyAskOpen = true;
                _dailyAskAt = Time.unscaledTime;
                NoteWelcomeClaim(firstEver);
                return;
            }
            NoteWelcomeClaim(firstEver);
        }

        // First claim only. The ask card, when it is due, is answered before this award.
        void NoteWelcomeClaim(bool firstEver)
        {
            if (!firstEver) return;
            if (PlayerPrefs.GetInt(WelcomeBonusKey, 0) != 0) return;
#if UNITY_EDITOR
            if (_dailyShotQuiet || _pokerPlayrun || EditorShotLive) return;
#endif
            if (_dailyAskOpen)
            {
                PlayerPrefs.SetInt(WelcomePendingKey, 1);
                PlayerPrefs.Save();
                return;
            }
            OfferWelcome();
        }

        void OfferWelcome()
        {
            if (PlayerPrefs.GetInt(WelcomeBonusKey, 0) != 0) return;
#if UNITY_EDITOR
            if (_dailyShotQuiet || _pokerPlayrun || EditorShotLive) return;
#endif
            PlayerPrefs.SetInt(WelcomeBonusKey, 1);
            PlayerPrefs.SetInt(WelcomePendingKey, 0);
            PlayerPrefs.Save();
            Purse.Credit(WelcomeBonusCoins);
            Sfx.Clink();
            Haptics.Play(Haptics.Tier.Medium);
            BurstDailyCoins(WelcomeBonusCoins);
            _welcomeOpen = true;
            _welcomeAt = Time.unscaledTime;
            _welcomeGlove = false;
        }

        void CloseWelcome()
        {
            if (!_welcomeOpen) return;
            _welcomeOpen = false;
            _welcomeAt = -1f;
            Sfx.CardTap();
            if (VipRailGoal() <= 0.5f) return;
            var box = SplashNoAdsRect();
            var seat = SplashRailSeat(RailVip);
            if (box.width < 12f && seat.width < 12f) return;
            _welcomeGlove = true;
            _welcomeGloveUntil = Time.unscaledTime + TapCycle;
            _gloveReady = false;
            _gloveVis = false;
            _glovePhase = 0f;
            _gloveDip = 0f;
            _tapSent = false;
            _coachFade = 0f;
        }

        void ClunkClaimed()
        {
            SfxLibrary.Play("cowbell", 0.22f, 0.04f);
            _claimClunk = Time.unscaledTime;
            if (_cowbellRhythm == null) _cowbellRhythm = new RhythmTap();
            if (_cowbellRhythm.Hear(Time.unscaledTime))
                SfxLibrary.Play("groove", 0.28f);
        }

        void StopClaimGroove()
        {
            SfxLibrary.SeatGroove(false);
            if (_cowbellRhythm != null) _cowbellRhythm.Reset();
        }

        void DismissDaily()
        {
            if (!_dailyOpen) return;
            StopClaimGroove();
            _dailyOpen = false;
            _dailyPopAt = -1f;
            DismissDailyIntro();
            Sfx.CardTap();
        }

        void BurstDailyCoins(int coins)
        {
            int n = coins >= 100 ? 8 : coins >= 25 ? 6 : 4;
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

        // Ease-out cubic, 0.88 → 1. Resting scale is 1, so an already-open card does not drift.
        static float DailyPop(float at)
        {
            if (at < 0f) return 1f;
            float u = (Time.unscaledTime - at) / 0.34f;
            if (u >= 1f) return 1f;
            if (u < 0f) u = 0f;
            float inv = 1f - u;
            return Mathf.Lerp(0.88f, 1f, 1f - inv * inv * inv);
        }

        static Vector2 DailyPivot(Rect card, Rect flower)
        {
            float x0 = Mathf.Min(card.x, flower.x);
            float y0 = Mathf.Min(card.y, flower.y);
            float x1 = Mathf.Max(card.xMax, flower.xMax);
            float y1 = Mathf.Max(card.yMax, flower.yMax);
            return new Vector2((x0 + x1) * 0.5f, (y0 + y1) * 0.5f);
        }

        static Rect DailyScaleRect(Rect r, Vector2 pivot, float k)
        {
            if (k >= 0.999f) return r;
            return new Rect(
                pivot.x + (r.x - pivot.x) * k,
                pivot.y + (r.y - pivot.y) * k,
                r.width * k,
                r.height * k);
        }

        // Outermost pixels of the pop-up frame. The wood card, unless the brass band sticks past it.
        static Rect DailyFrameOuter(Rect card, Rect board, float frameBand)
        {
            float x0 = Mathf.Min(card.x, board.x - frameBand);
            float y0 = Mathf.Min(card.y, board.y - frameBand);
            float x1 = Mathf.Max(card.xMax, board.xMax + frameBand);
            float y1 = Mathf.Max(card.yMax, board.yMax + frameBand);
            return new Rect(x0, y0, x1 - x0, y1 - y0);
        }

        // Where the avatar looks. Same contact the claim glove taps.
        Vector2 DailyClaimAim(float s)
        {
            DailyClaimGlove(s, out var aim, out _, out _);
            return aim;
        }

        // Claim step only. Hit is just left of the face center and a little below
        // it, on the top surface: not the rim, not the middle. The perch lift is
        // shorter than the shared rise, which parks the hand up on the reward row.
        // fromLeft keeps that side-on approach when the nudge crosses mid-screen.
        void DailyClaimGlove(float s, out Vector2 aim, out float perchLift, out bool fromLeft)
        {
            DailyLayout(s, out var card, out var flower, out _, out _);
            var disc = FlowerDisc(flower, 0f);
            float left = Mathf.Clamp(disc.width * 0.10f, 8f * s, 18f * s);
            float below = Mathf.Clamp(disc.height * 0.12f, 4f * s, 10f * s);
            var hit = new Vector2(disc.center.x - left, disc.center.y + below);
            float k = DailyPop(_dailyPopAt);
            var pivot = DailyPivot(card, flower);
            aim = new Vector2(pivot.x + (hit.x - pivot.x) * k, pivot.y + (hit.y - pivot.y) * k);
            float lift = disc.height * 0.20f;
            float minLift = 6f * s;
            if (lift < minLift) lift = minLift;
            float maxLift = GloveRise(s) * 0.45f;
            if (lift > maxLift) lift = maxLift;
            perchLift = lift;
            fromLeft = disc.center.x >= Screen.width * 0.5f;
        }

        // Content-sized. Rows are reserved up front, including an empty bonus line,
        // so opening and a later week-bonus string cannot reflow the tiles.
        static void DailyLayout(float s, out Rect card, out Rect flower, out Rect board, out float band)
        {
            float insetL = Mathf.Max(12f * s, Screen.safeArea.xMin + 8f);
            float insetR = Mathf.Max(12f * s, Screen.width - Screen.safeArea.xMax + 8f);
            float cardW = Mathf.Min(Screen.width - insetL - insetR, Mathf.Min(Screen.width * 0.72f, 420f * s));
            if (cardW < 200f) cardW = Mathf.Min(Screen.width - insetL - insetR, 200f);
            band = Mathf.Clamp(cardW * 0.05f, 11f * s, 20f * s);
            // Brass outer edge is the card edge, so each bulb base sits on the border.
            float boardW = cardW - band * 2f;
            DailyBoardSize(s, boardW, out float boardH, out _, out _, out _, out _, out _, out _);
            float lip = band;
            float cardH = lip + boardH + lip;
            // ~18% larger than the old 0.38 / 0.44 disc. Caption stays under the face.
            float flowerSz = Mathf.Min(Screen.width * 0.448f, cardW * 0.519f);
            // Pedestal art starts ~13% down the square (1024×811 letterboxed). A lip of
            // band*0.35 left the dim wash showing as a dark seam under the gold frame.
            // Cap at the brass plus the board's bottom pad so the day tiles stay clear.
            float overlap = flowerSz * 0.15f;
            float tileClear = band + 8f * s;
            if (overlap > tileClear) overlap = tileClear;
            float stack = cardH + flowerSz - overlap;
            var placed = PlacePopup(s, cardW, stack, 0.36f);
            card = new Rect(placed.x, placed.y, cardW, cardH);
            board = new Rect(card.x + (cardW - boardW) * 0.5f, card.y + lip, boardW, boardH);
            flower = new Rect(card.center.x - flowerSz * 0.5f, card.yMax - overlap, flowerSz, flowerSz);
        }

        static void DailyBoardSize(float s, float boardW, out float boardH, out float titleH, out float streakH, out float bonusH, out float tileH, out float gap, out float unit)
        {
            float pad = 8f * s;
            titleH = Mathf.Clamp(26f * s, 22f, 36f * s);
            streakH = Mathf.Clamp(16f * s, 14f, 22f * s);
            bonusH = Mathf.Clamp(14f * s, 12f, 18f * s);
            gap = Mathf.Max(6f, 7f * s);
            int days = DailyBonus.CycleDays;
            if (days < 1) days = 1;
            int seams = days - 1;
            float inner = boardW - pad * 2f;
            if (inner < 40f) inner = 40f;
            unit = (inner - gap * seams) / days;
            if (unit < 8f)
            {
                gap = 4f;
                unit = (inner - gap * seams) / days;
                if (unit < 4f) unit = 4f;
            }
            tileH = unit * 1.22f;
            boardH = pad + titleH + streakH + bonusH + 6f * s + tileH + pad;
        }

        static void DailyRows(float s, Rect board, out Rect title, out Rect streak, out Rect bonus, out Rect row, out float gap, out float unit)
        {
            DailyBoardSize(s, board.width, out _, out float titleH, out float streakH, out float bonusH, out float tileH, out gap, out unit);
            float pad = 8f * s;
            float x = board.x + pad;
            float w = board.width - pad * 2f;
            float y = board.y + pad;
            title = new Rect(x, y, w, titleH);
            y += titleH;
            streak = new Rect(x, y, w, streakH);
            y += streakH;
            bonus = new Rect(x, y, w, bonusH);
            y += bonusH + 6f * s;
            int days = DailyBonus.CycleDays;
            if (days < 1) days = 1;
            float rowW = unit * days + gap * (days - 1);
            row = new Rect(board.center.x - rowW * 0.5f, y, rowW, tileH);
        }

        // Shared pop-up frame. Bulbs are not drawn here. flatGold is a flat band
        // with no card slab. DrawPopupInset runs for every caller.
        static void DrawDailyFrame(Rect card, Rect board, float band, float s, float breathe, Texture2D glow, bool flatGold = false)
        {
            float aura = card.width * (0.055f + 0.02f * breathe);
            GUI.color = new Color(1f, 0.78f, 0.22f, 0.20f + 0.10f * breathe);
            GUI.DrawTexture(new Rect(card.x - aura, card.y - aura * 0.55f, card.width + aura * 2f, card.height + aura), glow, ScaleMode.ScaleToFit, true);

            var wood = SpriteCatalog.Blanket != null ? SpriteCatalog.Blanket.texture : Texture2D.whiteTexture;
            if (!flatGold)
            {
                float sh = 4f * s;
                GUI.color = new Color(0.05f, 0.03f, 0.02f, 0.42f);
                // Right only. Shifting down drew a dark band along the bottom of the gold frame.
                GUI.DrawTexture(new Rect(card.x + sh, card.y, card.width, card.height), wood, ScaleMode.StretchToFill, true);
            }
            GUI.color = new Color(0.52f, 0.30f, 0.11f, 1f);
            GUI.DrawTexture(card, wood, ScaleMode.StretchToFill, true);

            var outer = new Rect(board.x - band, board.y - band, board.width + band * 2f, board.height + band * 2f);
            if (flatGold)
            {
                GUI.color = new Color(0.74f, 0.52f, 0.16f, 1f);
                GUI.DrawTexture(outer, Texture2D.whiteTexture);
            }
            else
            {
                // Brass band. Bulb screw-lips sit on the outer edge of the pop-up, not this midline.
                GUI.color = new Color(0.30f, 0.16f, 0.05f, 1f);
                GUI.DrawTexture(outer, Texture2D.whiteTexture);
                GUI.color = new Color(0.74f, 0.52f, 0.16f, 1f);
                float face = 2f * s;
                GUI.DrawTexture(new Rect(outer.x + face, outer.y + face, outer.width - face * 2f, outer.height - face * 2f), Texture2D.whiteTexture);
                float edge = Mathf.Max(1f, 1.6f * s);
                GUI.color = new Color(1f, 0.88f, 0.50f, 0.90f);
                GUI.DrawTexture(new Rect(outer.x + face, outer.y + face, outer.width - face * 2f, edge), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(outer.x + face, outer.y + face, edge, outer.height - face * 2f), Texture2D.whiteTexture);
                GUI.color = new Color(0.18f, 0.09f, 0.03f, 0.92f);
                GUI.DrawTexture(new Rect(outer.x + face, outer.yMax - face - edge, outer.width - face * 2f, edge), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(outer.xMax - face - edge, outer.y + face, edge, outer.height - face * 2f), Texture2D.whiteTexture);
            }

            GUI.color = new Color(0.09f, 0.05f, 0.025f, 1f);
            GUI.DrawTexture(board, wood, ScaleMode.StretchToFill, true);
            DrawPopupInset(board, s, breathe);
            GUI.color = new Color(0f, 0f, 0f, 0.50f);
            float shade = 2f * s;
            GUI.DrawTexture(new Rect(board.x, board.y, board.width, shade), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(board.x, board.y, shade, board.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // Thin gold line just inside the band. Shared-frame pop-ups keep it.
        static void DrawPopupInset(Rect board, float s, float breathe)
        {
            GUI.color = new Color(1f, 0.78f, 0.22f, 0.50f + 0.16f * breathe);
            float rim = Mathf.Max(2f, 2.5f * s);
            float line = Mathf.Max(1.5f, 2f * s);
            GUI.DrawTexture(new Rect(board.x + rim, board.y + rim, board.width - rim * 2f, line), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(board.x + rim, board.yMax - rim - line, board.width - rim * 2f, line), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(board.x + rim, board.y + rim, line, board.height - rim * 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(board.xMax - rim - line, board.y + rim, line, board.height - rim * 2f), Texture2D.whiteTexture);
        }

        // Orange marquee around a pop-up. Off unless bulbs is set.
        // Daily Bonus is the only frame that sets it. Call outside the pop-scale
        // matrix so the pre-scaled shell keeps bulb spins round.
        static void DrawPopupBulbs(Rect card, Rect board, float band, float s, float t, Vector2 pivot, float pop, bool bulbs = false)
        {
            if (!bulbs) return;
            var shell = DailyFrameOuter(card, board, band);
            var shellDraw = DailyScaleRect(shell, pivot, pop);
            float bulbPx = Mathf.Max(30f * s, DailyScaleRect(board, pivot, pop).width * 0.085f) * DailyBulbShrink;
            DrawGiftMarquee(shellDraw, s, t, 1f, 0f, 0f, 16, 0f, true, DailyBulbPin, bulbPx);
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

        static void EnsureDailyFit(float s, float band, Rect titleR, Rect streakR, Rect bonusR, Rect disc, float unit, float tileH)
        {
            int key = Screen.width * 73856093 ^ Screen.height * 19349663
                ^ Mathf.RoundToInt(titleR.width) * 83492791
                ^ Mathf.RoundToInt(disc.width) * 50331653
                ^ Mathf.RoundToInt(unit) * 2718281
                ^ DailyBonus.StreakLine.Length * 97;
            if (key == _dailyFitKey) return;
            _dailyFitKey = key;
            _dailyTitlePx = FitFont(_dailyTitle, "Daily Bonus", titleR.width * 0.90f, titleR.height * 0.90f, 14, Mathf.Max(18, Mathf.RoundToInt(34f * s)));
            _dailyLinePx = FitFont(_dailyLine, DailyBonus.StreakLine, streakR.width * 0.88f, streakR.height * 0.90f, 10, Mathf.Max(12, Mathf.RoundToInt(18f * s)));
            _dailyBonusPx = FitFont(_dailyLine, "streak +$25", bonusR.width * 0.88f, bonusR.height * 0.90f, 9, Mathf.Max(11, Mathf.RoundToInt(16f * s)));
            _dailyTilePx = FitFont(_dailyTile, "$100", unit * 0.90f, tileH * 0.32f, 8, Mathf.Max(10, Mathf.RoundToInt(16f * s)));
            _dailyClaimPx = FitFont(_dailyClaim, "Claimed", disc.width * 0.72f, disc.height * 0.52f, 12, Mathf.Max(16, Mathf.RoundToInt(30f * s)));
        }

        static void DrawDailyCopy(Rect titleR, Rect streakR, Rect bonusR, float s, float breathe, Texture2D glow)
        {
            GUI.color = new Color(1f, 0.72f, 0.16f, 0.26f + 0.12f * breathe);
            GUI.DrawTexture(new Rect(titleR.x, titleR.y - 2f * s, titleR.width, titleR.height + 4f * s), glow, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
            _dailyTitle.fontSize = _dailyTitlePx;
            int ink = Mathf.Max(2, Mathf.RoundToInt(_dailyTitlePx * 0.10f));
            StampOutlined(titleR, "Daily Bonus", _dailyTitle, new Color(1f, 0.96f, 0.78f), 1, ink);
            _dailyLine.fontSize = _dailyLinePx;
            StampOutlined(streakR, DailyBonus.StreakLine, _dailyLine, new Color(1f, 0.90f, 0.55f), 1, 1);
            string bonus = DailyBonus.BonusLine;
            if (bonus.Length == 0) return;
            _dailyLine.fontSize = _dailyBonusPx;
            StampOutlined(bonusR, bonus, _dailyLine, new Color(1f, 0.86f, 0.42f), 1, 1);
        }

        static void DrawDailyTiles(Rect row, float gap, float unit, float s, float breathe, Texture2D glow)
        {
            if (unit < 4f) return;
            int today = DailyBonus.Cycle;
            bool claimed = DailyBonus.ClaimedToday;
            var crown = SpriteCatalog.Crown;
            var crownTex = crown != null ? crown.texture : null;
            var spark = SpriteCatalog.Sparkle;
            var sparkTex = spark != null ? spark.texture : null;
            float x = row.x;
            int days = DailyBonus.CycleDays;
            int crownDay = days - 1;
            for (int i = 0; i < days; i++)
            {
                bool now = i == today && DailyBonus.OfferReady;
                bool done = i < today || (i == today && claimed);
                var tile = new Rect(x, row.y, unit, row.height);
                DrawDailyTile(tile, i, now, done, i == crownDay, s, breathe, glow, crownTex, sparkTex);
                x += unit + gap;
            }
        }

        static void DrawDailyTile(Rect tile, int day, bool now, bool done, bool crown, float s, float breathe, Texture2D glow, Texture crownTex, Texture sparkTex)
        {
            if (now)
            {
                float pad = 4f * s + 1.5f * breathe;
                GUI.color = new Color(1f, 0.82f, 0.28f, 0.30f + 0.22f * breathe);
                GUI.DrawTexture(new Rect(tile.x - pad, tile.y - pad, tile.width + pad * 2f, tile.height + pad * 2f), glow, ScaleMode.ScaleToFit, true);
            }
            Color fill = now
                ? new Color(0.98f, 0.78f, 0.28f, 1f)
                : done
                    ? new Color(0.30f, 0.17f, 0.07f, 0.94f)
                    : new Color(0.16f, 0.09f, 0.04f, 0.90f);
            if (crown && !now && !done) fill = new Color(0.55f, 0.34f, 0.10f, 0.96f);
            if (crown && now) fill = new Color(1f, 0.84f, 0.34f, 1f);
            GUI.color = fill;
            GUI.DrawTexture(tile, Texture2D.whiteTexture);
            GUI.color = new Color(1f, 0.86f, 0.42f, now || crown ? 1f : done ? 0.55f : 0.40f);
            float edge = Mathf.Max(1.5f, (now ? 2.6f : 1.6f) * s);
            GUI.DrawTexture(new Rect(tile.x, tile.y, tile.width, edge), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(tile.x, tile.yMax - edge, tile.width, edge), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(tile.x, tile.y, edge, tile.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(tile.xMax - edge, tile.y, edge, tile.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float textTop = tile.y + tile.height * 0.14f;
            float textH = tile.height * 0.48f;
            if (crown && crownTex != null)
            {
                float cs = tile.width * 0.78f;
                float ch = cs * 0.58f;
                var crownR = new Rect(tile.center.x - cs * 0.5f, tile.y + tile.height * 0.04f, cs, ch);
                GUI.DrawTexture(crownR, crownTex, ScaleMode.ScaleToFit, true);
                if (sparkTex != null)
                {
                    float pulse = 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.1f));
                    float sz = tile.width * 0.30f;
                    var sr = new Rect(crownR.xMax - sz * 0.62f, crownR.y - sz * 0.04f, sz, sz);
                    GUI.color = new Color(1f, 0.96f, 0.75f, 0.22f + 0.48f * pulse);
                    GUI.DrawTexture(sr, sparkTex, ScaleMode.ScaleToFit, true);
                    GUI.color = Color.white;
                }
                textTop = crownR.yMax + 1f;
                float below = tile.yMax - textTop - (done ? tile.height * 0.22f : tile.height * 0.05f);
                textH = below > tile.height * 0.20f ? below : tile.height * 0.20f;
            }
            else if (done)
            {
                textTop = tile.y + tile.height * 0.08f;
                textH = tile.height * 0.46f;
            }
            var amt = new Rect(tile.x + 1f, textTop, tile.width - 2f, textH);
            _dailyTile.fontSize = _dailyTilePx;
            Color ink = now
                ? new Color(0.32f, 0.14f, 0.04f, 1f)
                : new Color(1f, 0.94f, 0.74f, done ? 0.72f : 0.92f);
            StampOutlined(amt, DailyBonus.TileLabel(day), _dailyTile, ink, 1, 1);
            if (!done) return;
            float mh = tile.height * 0.15f;
            var mark = new Rect(tile.x + tile.width * 0.27f, tile.yMax - mh - tile.height * 0.07f, tile.width * 0.46f, mh);
            DrawDailyCheck(mark, new Color(1f, 0.88f, 0.40f, 0.96f));
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

        static string DailyVerb()
        {
            if (DailyBonus.OfferReady) return "Claim";
            if (DailyBonus.ClaimedToday) return "Claimed";
            return "Later";
        }

        static void DrawDailyClaim(Rect flower, bool held, float s, float t)
        {
            bool ready = DailyBonus.OfferReady;
            float squish = 1f;
            if (_claimClunk >= 0f)
            {
                float age = Time.unscaledTime - _claimClunk;
                if (age >= 0f && age < 0.18f)
                {
                    float u = age / 0.18f;
                    squish = u < 0.35f
                        ? Mathf.Lerp(1f, 0.90f, u / 0.35f)
                        : Mathf.Lerp(0.90f, 1f, (u - 0.35f) / 0.65f);
                }
            }
            var pressed = GUI.matrix;
            if (squish < 0.995f)
                GUIUtility.ScaleAroundPivot(new Vector2(squish, squish * 0.94f), flower.center);
            var disc = DrawPopupButton(flower, held, ready);
            var labR = new Rect(disc.x, disc.y + disc.height * 0.22f, disc.width, disc.height * 0.56f);
            _dailyClaim.fontSize = _dailyClaimPx;
            int ink = Mathf.Clamp(Mathf.RoundToInt(_dailyClaimPx * 0.12f), 2, 6);
            StampOutlined(labR, DailyVerb(), _dailyClaim, new Color(0.28f, 0.12f, 0.04f), 2, ink);
            if (!ready)
            {
                GUI.matrix = pressed;
                return;
            }

            var spark = SpriteCatalog.Sparkle;
            var tex = spark != null && spark.texture != null ? spark.texture : null;
            if (tex == null)
            {
                GUI.matrix = pressed;
                return;
            }
            float pulse = 0.82f + 0.18f * Mathf.Sin(t * 5.4f);
            float sz = disc.width * 0.28f * pulse;
            var sr = new Rect(disc.xMax - sz * 0.72f, disc.y - sz * 0.12f, sz, sz);
            var prev = GUI.matrix;
            GUIUtility.RotateAroundPivot(t * 40f, sr.center);
            GUI.color = new Color(1f, 0.96f, 0.75f, 0.92f);
            GUI.DrawTexture(sr, tex, ScaleMode.ScaleToFit, true);
            GUI.matrix = prev;
            GUI.color = Color.white;
            GUI.matrix = pressed;
        }

        void DrawDailyAsk(float s)
        {
            if (!_dailyAskOpen) return;
#if UNITY_EDITOR
            if (_dailyShotQuiet || EditorShotLive) return;
#endif
            DailyAskLayout(s, out var card, out var body, out var yesR, out var noR);
            bool yes = HitPad(yesR, out bool yesHeld);
            bool no = HitPad(noR, out bool noHeld);
            HitPad(new Rect(0f, 0f, Screen.width, Screen.height), out _);

            float t = Time.unscaledTime;
            float breathe = 0.5f + 0.5f * Mathf.Sin(t * 2.05f);
            var glow = GlowTex();
            GUI.color = new Color(0.04f, 0.03f, 0.02f, 0.72f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float k = DailyPop(_dailyAskAt);
            var prev = GUI.matrix;
            if (k < 0.999f)
                GUIUtility.ScaleAroundPivot(new Vector2(k, k), card.center);
            DrawAskFrame(card, s, breathe, glow);
            EnsureAskStyles();
            EnsureAskFit(s, body, yesR);
            _dailyAsk.fontSize = _dailyAskBodyPx;
            int ink = Mathf.Max(2, Mathf.RoundToInt(_dailyAskBodyPx * 0.10f));
            StampOutlined(body, DailyAskCopy, _dailyAsk, new Color(1f, 0.96f, 0.78f), 1, ink);
            DrawAskButton(yesR, DailyAskYes, _dailyAskYesPx, true, yesHeld);
            DrawAskButton(noR, DailyAskNo, _dailyAskNoPx, false, noHeld);
            GUI.matrix = prev;
            GUI.color = Color.white;

            if (yes) CloseDailyAsk(true);
            else if (no) CloseDailyAsk(false);
        }

        void CloseDailyAsk(bool yes)
        {
            if (!_dailyAskOpen) return;
            _dailyAskOpen = false;
            _dailyAskAt = -1f;
            if (yes) DailyReminder.Accept();
            else DailyReminder.Decline();
            Sfx.CardTap();
            if (PlayerPrefs.GetInt(WelcomePendingKey, 0) != 0)
                OfferWelcome();
        }

        static void DailyAskLayout(float s, out Rect card, out Rect body, out Rect yesR, out Rect noR)
        {
            EnsureAskStyles();
            float side = Mathf.Max(16f * s, Screen.safeArea.xMin + 8f);
            float w = Mathf.Min(Screen.width - side * 2f, Mathf.Min(Screen.width * 0.78f, 420f * s));
            if (w < 180f) w = Mathf.Min(Screen.width - 12f, 180f);
            float band = Mathf.Max(8f, 10f * s);
            float pad = band + 12f * s;
            float innerW = w - pad * 2f;
            if (innerW < 40f) innerW = 40f;
            float btnH = Mathf.Clamp(42f * s, 38f, 50f * s);
            float gap = 10f * s;
            int hi = Mathf.Max(14, Mathf.RoundToInt(18f * s));
            int lo = 11;
            float textH = 28f * s;
            float maxH = Screen.height * 0.48f;
            float chrome = pad * 2f + 12f * s + btnH;
            for (int fs = hi; fs >= lo; fs--)
            {
                BindFit(_dailyAsk, DailyAskCopy, true);
                _fitScratch.fontSize = fs;
                float measured = _fitScratch.CalcHeight(_fitContent, innerW);
                textH = measured;
                if (chrome + measured + 8f * s <= maxH || fs == lo) break;
            }
            float h = chrome + textH + 8f * s;
            if (h > maxH) h = maxH;
            if (h < chrome + 28f * s) h = chrome + 28f * s;
            card = PlacePopup(s, w, h, 0.44f);
            float btnW = (innerW - gap) * 0.5f;
            if (btnW < 8f) btnW = 8f;
            float btnY = card.yMax - pad - btnH;
            if (btnY < card.y + pad) btnY = card.y + pad;
            yesR = new Rect(card.x + pad, btnY, btnW, btnH);
            noR = new Rect(yesR.xMax + gap, btnY, btnW, btnH);
            float bodyTop = card.y + pad;
            float bodyBot = btnY - 12f * s;
            if (bodyBot < bodyTop) bodyBot = bodyTop;
            body = new Rect(card.x + pad, bodyTop, innerW, bodyBot - bodyTop);
        }

        static void DrawAskFrame(Rect card, float s, float breathe, Texture2D glow)
        {
            float aura = card.width * 0.06f;
            GUI.color = new Color(1f, 0.78f, 0.22f, 0.16f + 0.08f * breathe);
            GUI.DrawTexture(new Rect(card.x - aura, card.y - aura * 0.4f, card.width + aura * 2f, card.height + aura), glow, ScaleMode.ScaleToFit, true);
            var wood = SpriteCatalog.Blanket != null ? SpriteCatalog.Blanket.texture : Texture2D.whiteTexture;
            float sh = 4f * s;
            GUI.color = new Color(0.05f, 0.03f, 0.02f, 0.42f);
            GUI.DrawTexture(new Rect(card.x + sh, card.y + sh * 1.4f, card.width, card.height), wood, ScaleMode.StretchToFill, true);
            GUI.color = new Color(0.46f, 0.26f, 0.10f, 1f);
            GUI.DrawTexture(card, wood, ScaleMode.StretchToFill, true);
            float band = Mathf.Max(8f, 10f * s);
            GUI.color = new Color(0.78f, 0.56f, 0.18f, 1f);
            GUI.DrawTexture(new Rect(card.x + 3f * s, card.y + 3f * s, card.width - 6f * s, card.height - 6f * s), Texture2D.whiteTexture);
            var inner = new Rect(card.x + band, card.y + band, card.width - band * 2f, card.height - band * 2f);
            GUI.color = new Color(0.18f, 0.10f, 0.05f, 1f);
            GUI.DrawTexture(inner, wood, ScaleMode.StretchToFill, true);
            GUI.color = Color.white;
        }

        static void EnsureAskStyles()
        {
            if (_dailyAsk != null) return;
            _dailyAsk = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            _dailyAskBtn = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false
            };
        }

        static void EnsureAskFit(float s, Rect body, Rect btn)
        {
            int key = Screen.width * 19349663 ^ Screen.height * 83492791
                ^ Mathf.RoundToInt(body.width) * 17
                ^ Mathf.RoundToInt(body.height)
                ^ Mathf.RoundToInt(btn.width);
            if (key == _dailyAskFit) return;
            _dailyAskFit = key;
            EnsureAskStyles();
            int hi = Mathf.Max(14, Mathf.RoundToInt(18f * s));
            _dailyAskBodyPx = FitFontWrapped(_dailyAsk, DailyAskCopy, body.width, body.height * 0.92f, 11, hi);
            int yes = FitFont(_dailyAskBtn, DailyAskYes, btn.width * 0.88f, btn.height * 0.62f, 12, Mathf.Max(16, Mathf.RoundToInt(22f * s)));
            int no = FitFont(_dailyAskBtn, DailyAskNo, btn.width * 0.88f, btn.height * 0.62f, 12, Mathf.Max(16, Mathf.RoundToInt(22f * s)));
            _dailyAskYesPx = yes < no ? yes : no;
            _dailyAskNoPx = _dailyAskYesPx;
        }

        static void DrawAskButton(Rect r, string label, int px, bool gold, bool held)
        {
            var face = DrawPopupButton(r, held, gold, 1f, false);
            _dailyAskBtn.fontSize = px;
            var ink = gold ? new Color(0.32f, 0.14f, 0.04f, 1f) : new Color(1f, 0.94f, 0.78f, 1f);
            StampOutlined(face, label, _dailyAskBtn, ink, 1, 1);
        }

        void DrawWelcome(float s)
        {
            if (!_welcomeOpen) return;
#if UNITY_EDITOR
            if (_dailyShotQuiet || EditorShotLive) return;
#endif
            WelcomeLayout(s, out var card, out var titleR, out var amtR, out var lineR, out var btn);
            bool thanks = HitPad(btn, out bool held);
            HitPad(new Rect(0f, 0f, Screen.width, Screen.height), out _);

            float t = Time.unscaledTime;
            float breathe = 0.5f + 0.5f * Mathf.Sin(t * 2.05f);
            var glow = GlowTex();
            GUI.color = new Color(0.04f, 0.03f, 0.02f, 0.72f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float k = DailyPop(_welcomeAt);
            var prev = GUI.matrix;
            if (k < 0.999f)
                GUIUtility.ScaleAroundPivot(new Vector2(k, k), card.center);
            float band = Mathf.Clamp(card.width * 0.045f, 12f * s, 22f * s);
            var board = new Rect(card.x + band, card.y + band, card.width - band * 2f, card.height - band * 2f);
            DrawDailyFrame(card, board, band, s, breathe, glow, true);
            EnsureAskStyles();
            EnsureWelcomeFit(s, titleR, amtR, lineR, btn);
            _dailyAsk.fontSize = _welcomeTitlePx;
            StampOutlined(titleR, WelcomeTitle, _dailyAsk, new Color(1f, 0.94f, 0.62f), 1, 2);
            _dailyAsk.fontSize = _welcomeAmtPx;
            StampOutlined(amtR, WelcomeAmountText(), _dailyAsk, new Color(1f, 0.86f, 0.28f), 1, 2);
            _dailyAsk.fontSize = _welcomeLinePx;
            int ink = Mathf.Max(2, Mathf.RoundToInt(_welcomeLinePx * 0.10f));
            StampOutlined(lineR, WelcomeLine, _dailyAsk, new Color(1f, 0.96f, 0.78f), 1, ink);
            DrawAskButton(btn, WelcomeThanks, _welcomeBtnPx, true, held);
            GUI.matrix = prev;
            GUI.color = Color.white;
            if (thanks) CloseWelcome();
        }

        static void WelcomeLayout(float s, out Rect card, out Rect title, out Rect amt, out Rect line, out Rect btn)
        {
            float insetL = Mathf.Max(14f * s, Screen.safeArea.xMin + 8f);
            float insetR = Mathf.Max(14f * s, Screen.width - Screen.safeArea.xMax + 8f);
            float maxW = Mathf.Max(160f * s, Screen.width - insetL - insetR);
            float w = Mathf.Min(maxW, Mathf.Min(Screen.width * 0.84f, 500f * s));
            float h = Mathf.Clamp(248f * s, 220f, 310f * s);
            if (h > Screen.height * 0.52f) h = Screen.height * 0.52f;
            card = PlacePopup(s, w, h, 0.42f);
            float band = Mathf.Max(10f, 12f * s);
            float pad = band + 16f * s;
            float btnH = Mathf.Clamp(48f * s, 44f, 58f * s);
            float innerW = w - pad * 2f;
            float y = card.y + pad;
            float titleH = 32f * s;
            float amtH = 40f * s;
            title = new Rect(card.x + pad, y, innerW, titleH);
            y += titleH;
            amt = new Rect(card.x + pad, y, innerW, amtH);
            y += amtH + 4f * s;
            float btnY = card.yMax - pad - btnH;
            line = new Rect(card.x + pad, y, innerW, Mathf.Max(36f, btnY - y - 8f * s));
            btn = new Rect(card.x + pad, btnY, innerW, btnH);
        }

        static void EnsureWelcomeFit(float s, Rect title, Rect amt, Rect line, Rect btn)
        {
            int key = Screen.width * 83492791 ^ Screen.height * 19349663
                ^ Mathf.RoundToInt(line.width) * 13
                ^ Mathf.RoundToInt(line.height)
                ^ Mathf.RoundToInt(btn.width);
            if (key == _welcomeFit) return;
            _welcomeFit = key;
            EnsureAskStyles();
            _welcomeTitlePx = FitFont(_dailyAsk, WelcomeTitle, title.width, title.height * 0.9f, 16, Mathf.Max(22, Mathf.RoundToInt(30f * s)));
            _welcomeAmtPx = FitFont(_dailyAsk, WelcomeAmountText(), amt.width, amt.height * 0.92f, 18, Mathf.Max(26, Mathf.RoundToInt(40f * s)));
            _welcomeLinePx = FitFontWrapped(_dailyAsk, WelcomeLine, line.width * 0.92f, line.height * 0.92f, 13, Mathf.Max(16, Mathf.RoundToInt(20f * s)));
            _welcomeBtnPx = FitFont(_dailyAskBtn, WelcomeThanks, btn.width * 0.8f, btn.height * 0.62f, 14, Mathf.Max(16, Mathf.RoundToInt(22f * s)));
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
