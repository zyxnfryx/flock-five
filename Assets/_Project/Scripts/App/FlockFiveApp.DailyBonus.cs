using System.Collections;
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
        // Coins are in flight and the welcome card has not opened yet.
        bool _welcomeQueued;
        float _welcomeAt = -1f;
        bool _welcomeGlove;
        float _welcomeGloveUntil;
        static int _welcomeFit = int.MinValue;
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
                _welcomeQueued = false;
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
                _welcomeQueued = false;
                return;
            }
#endif
            if (_splash && _home == HomeFace.Splash && !_dailyAskOpen
                && PlayerPrefs.GetInt(WelcomePendingKey, 0) != 0)
                OfferWelcome();
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
            if (!GatePopup(PopupKind.Daily, true)) return;
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
            var icon = DailyBonus.IconState;
            if (icon == DailyBonusIconState.Badge)
            {
                var glow = GlowTex();
                float breathe = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.6f);
                float bloom = plate.width * (0.05f + 0.025f * breathe);
                GUI.color = new Color(0.90f, 0.18f, 0.12f, 0.16f + 0.14f * breathe);
                GUI.DrawTexture(new Rect(plate.x - bloom, plate.y - bloom, plate.width + bloom * 2f, plate.height + bloom * 2f), glow, ScaleMode.ScaleToFit, true);
            }
            GUI.color = new Color(0.08f, 0.05f, 0.02f, 0.34f);
            GUI.DrawTexture(new Rect(plate.x + 3f, plate.y + 5f, plate.width, plate.height), tex, ScaleMode.ScaleToFit, true);
            GUI.color = held ? new Color(0.90f, 0.88f, 0.82f, 1f) : Color.white;
            GUI.DrawTexture(plate, tex, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;

            // One ornament. IconState already excludes the other, and this branch does too.
            if (icon == DailyBonusIconState.Flame)
                DrawDailyStreakFlame(plate);
            else if (icon == DailyBonusIconState.Badge)
            {
                float cx = plate.xMax - plate.width * 0.22f;
                float cy = plate.yMax - plate.width * 0.22f;
                DrawReadyBadge(cx, cy, plate.width);
            }
        }

        // Top-right of the round medal. Art is 289×672, so a square letterboxes the fire.
        // belly only hangs the graphic. The digit is BadgeBodyCenter (the belly, not the tip).
        void DrawDailyStreakFlame(Rect plate)
        {
            float fh = plate.width * 0.92f;
            float fw = fh * (289f / 672f);
            const float belly = 0.65f;
            float markX = plate.xMax - plate.width * 0.20f;
            float markY = plate.y + plate.width * 0.18f;
            float lift = plate.width * 0.06f;
            var flame = new Rect(markX - fw * 0.5f, markY - fh * belly - lift, fw, fh);
            int frame = (int)(Time.unscaledTime * 8f) % FlameFrames;
            if (frame < 0) frame = 0;
            var spr = SpriteCatalog.Flame(frame);
            var tex = spr != null ? spr.texture : null;
            if (tex != null)
                GUI.DrawTexture(flame, tex, ScaleMode.ScaleToFit, true);
            EnsureDailyStyles();
            float numH = fh * 0.32f;
            float numW = Mathf.Max(fw * 2.2f, plate.width * 0.55f);
            _dailyLine.fontSize = RailDigitPx(numW, numH);
            DrawBadgeNumber(FlameRestCenter(flame), DailyBonus.StreakDigits, _dailyLine, new Color(1f, 0.97f, 0.86f), numW, numH, 2);
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
            var claimHit = DailyClaimTapRect(s);

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
                // Claimed: the pedestal still takes taps, for the clunk and the rhythm only.
                if (!DailyBonus.OfferReady && DailyBonus.ClaimedToday) ClunkClaimed();
                else ClaimDaily();
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
            BeginRewardPay(coins);
            _dailyOpen = false;
            _dailyPopAt = -1f;
            DismissDailyIntro();
            DailyReminder.Reschedule(DailyBonus.Streak, DailyBonus.ClaimedToday, DailyBonus.AtRisk);
            StartCoroutine(FinishDailyClaim(firstEver));
        }

        // Ask card or welcome only after the claim coins, balance, and clink are done.
        IEnumerator FinishDailyClaim(bool firstEver)
        {
            yield return WaitRewardPay();
            if (_restarting) yield break;
            if (DailyReminder.PromptDue())
            {
                if (GatePopup(PopupKind.Ask, false))
                {
                    _dailyAskOpen = true;
                    _dailyAskAt = Time.unscaledTime;
                }
                NoteWelcomeClaim(firstEver);
                yield break;
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

        // True while the welcome card, its glove, its coin flight, or a deferred
        // grant owns the next home turn. Adoption and the streak board go first.
        bool WelcomeOwnsTurn()
        {
            if (_welcomeOpen || _welcomeGlove || _welcomeQueued) return true;
            if (RewardLessonHold() || AdoptHoldsQueue() || _dailyAskOpen) return false;
            if (PlayerPrefs.GetInt(WelcomeBonusKey, 0) != 0) return false;
            return PlayerPrefs.GetInt(WelcomePendingKey, 0) != 0;
        }

        void OfferWelcome()
        {
            if (PlayerPrefs.GetInt(WelcomeBonusKey, 0) != 0) return;
            if (_welcomeOpen || _welcomeQueued) return;
#if UNITY_EDITOR
            if (_dailyShotQuiet || _pokerPlayrun || EditorShotLive) return;
#endif
            if (!GatePopup(PopupKind.Welcome, false)) return;
            if (RewardLessonHold() || AdoptHoldsQueue() || _dailyAskOpen)
            {
                if (PlayerPrefs.GetInt(WelcomePendingKey, 0) == 0)
                {
                    PlayerPrefs.SetInt(WelcomePendingKey, 1);
                    PlayerPrefs.Save();
                }
                return;
            }
            PlayerPrefs.SetInt(WelcomeBonusKey, 1);
            PlayerPrefs.SetInt(WelcomePendingKey, 0);
            PlayerPrefs.Save();
            _welcomeQueued = true;
            Purse.Credit(WelcomeBonusCoins);
            Sfx.Clink();
            Haptics.Play(Haptics.Tier.Medium);
            BeginRewardPay(WelcomeBonusCoins);
            StartCoroutine(OpenWelcomeWhenPaid());
        }

        IEnumerator OpenWelcomeWhenPaid()
        {
            yield return WaitRewardPay();
            if (_restarting)
            {
                _welcomeQueued = false;
                yield break;
            }
#if UNITY_EDITOR
            if (_dailyShotQuiet || EditorShotLive)
            {
                _welcomeQueued = false;
                yield break;
            }
#endif
            _welcomeQueued = false;
            if (!GatePopup(PopupKind.Welcome, false)) yield break;
            _welcomeOpen = true;
            _welcomeAt = Time.unscaledTime;
            _welcomeGlove = false;
        }

        // Ask card remembered while a lesson owned the screen.
        void OpenAskPopup()
        {
            if (!GatePopup(PopupKind.Ask, false)) return;
            if (_dailyAskOpen || _welcomeOpen) return;
            _dailyAskOpen = true;
            _dailyAskAt = Time.unscaledTime;
        }

        // Welcome card remembered while a lesson owned the screen.
        // Coins are credited once, inside OfferWelcome, before the card opens.
        void OpenDeferredWelcome()
        {
            if (PlayerPrefs.GetInt(WelcomeBonusKey, 0) != 0)
            {
                if (!GatePopup(PopupKind.Welcome, false)) return;
                if (_welcomeOpen || _restarting) return;
#if UNITY_EDITOR
                if (_dailyShotQuiet || EditorShotLive) return;
#endif
                _welcomeQueued = false;
                _welcomeOpen = true;
                _welcomeAt = Time.unscaledTime;
                _welcomeGlove = false;
                return;
            }
            OfferWelcome();
        }

        void CloseWelcome()
        {
            _welcomeQueued = false;
            if (!_welcomeOpen) return;
            _welcomeOpen = false;
            _welcomeAt = -1f;
            Sfx.CardTap();
            // Build 51: no glove points at the VIP button after the welcome card. Players
            // find it themselves. The next splash lesson takes its turn right away.
            _welcomeGlove = false;
        }

        // Claimed-state tap: the one cowbell clunk every time, nothing else (no second
        // claim, no reward, no dismissal). Four steady or short-long-short taps fire the
        // shared RhythmTap: the guitar-and-cowbell snippet plus a visible button pulse.
        void ClunkClaimed()
        {
            SfxLibrary.Cowbell();
            _claimClunk = Time.unscaledTime;
            if (_cowbellRhythm == null) _cowbellRhythm = new RhythmTap();
            if (!_cowbellRhythm.Hear(Time.unscaledTime)) return;
            Debug.Log("[Easter] cowbell rhythm fired");
            _claimEaster = Time.unscaledTime;
            SfxLibrary.SeatGroove(true);
            SfxLibrary.Play("groove", 0.45f);
            Haptics.Play(Haptics.Tier.Medium);
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
            _claimEaster = -1f;
            // X and the dim close the card only. The lesson stays until Claim.
            Sfx.CardTap();
        }

        // Claim flower, same pad DrawDailyBonus hits. Not today's tile.
        static Rect DailyClaimTapRect(float s)
        {
            DailyLayout(s, out _, out var flower, out _, out _);
            return FlowerHit(flower, s);
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

        // Claim step. Fingertip is the center of the real Claim button (the pedestal
        // face, same FlowerDisc the Watch glove uses), never a reward box. Shared
        // rise. _gloveKeepOff holds the palm off the button face.
        void DailyClaimGlove(float s, out Vector2 aim, out float perchLift, out bool fromLeft)
        {
            DailyLayout(s, out var card, out var flower, out _, out _);
            float k = DailyPop(_dailyPopAt);
            var pivot = DailyPivot(card, flower);
            var disc = DailyScaleRect(FlowerDisc(flower, 0f), pivot, k);
            aim = GloveTarget(disc);
            perchLift = float.NaN;
            fromLeft = false;
            _gloveKeepOff = disc;
        }

        // Content-sized. Rows are reserved up front, including an empty bonus line,
        // so opening and a later week-bonus string cannot reflow the tiles.
        static void DailyLayout(float s, out Rect card, out Rect flower, out Rect board, out float band)
        {
            float cardW = StandardPopupWidth(s);
            band = Mathf.Clamp(cardW * 0.05f, 11f * s, 20f * s);
            // Brass outer edge is the card edge, so each bulb base sits on the border.
            float boardW = cardW - band * 2f;
            DailyBoardSize(s, boardW, out float boardH, out _, out _, out _, out _, out _, out _);
            float lip = band;
            float cardH = lip + boardH + lip;
            // Same square CTA as Watch / Retry. Caption stays under the face.
            float flowerSz = PopupButtonSize(s, cardW);
            // Pedestal art starts ~13% down the square (1024×811 letterboxed). A lip of
            // band*0.35 left the dim wash showing as a dark seam under the gold frame.
            // Cap at the brass plus the board's bottom pad so the day tiles stay clear.
            // PopupCtaOverlap closes the gap under the brass so the bulbs on the bottom
            // edge no longer show between the card and the pedestal. The pedestal
            // still sinks at most band + the board's bottom pad, so the tiles stay clear.
            float overlap = PopupCtaOverlap(flowerSz, band + 8f * s);
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

        // One title cache for every pop-up. The key is the full-open fit box, not
        // the lamp scale, so a ±2% breathe cannot swap the point size.
        const int PopupTitleSlots = 4;
        static readonly string[] _popTitleTx = new string[PopupTitleSlots];
        static readonly int[] _popTitlePx = new int[PopupTitleSlots];
        static readonly int[] _popTitleLo = new int[PopupTitleSlots];
        static readonly int[] _popTitleHi = new int[PopupTitleSlots];
        static readonly int[] _popTitleMark = new int[PopupTitleSlots];
        static readonly int[] _popTitleW = new int[PopupTitleSlots];
        static readonly int[] _popTitleH = new int[PopupTitleSlots];
        static readonly int[] _popTitleUse = new int[PopupTitleSlots];
        static int _popTitleSerial;

        static int PopupTitleSize(GUIStyle st, string text, float fitW, float fitH, int lo, int hi)
        {
            if (lo > hi) lo = hi;
            int w = Mathf.Max(1, Mathf.RoundToInt(fitW));
            int h = Mathf.Max(1, Mathf.RoundToInt(fitH));
            int mark = FitStamp(st, st != null && st.wordWrap);
            int oldest = 0;
            int oldestUse = int.MaxValue;
            for (int i = 0; i < PopupTitleSlots; i++)
            {
                if (_popTitleUse[i] < oldestUse)
                {
                    oldestUse = _popTitleUse[i];
                    oldest = i;
                }
                if (_popTitleTx[i] != text || _popTitleLo[i] != lo || _popTitleHi[i] != hi
                    || _popTitleMark[i] != mark || _popTitleW[i] != w || _popTitleH[i] != h)
                    continue;
                _popTitleSerial++;
                _popTitleUse[i] = _popTitleSerial;
                return _popTitlePx[i];
            }
            int px = FitFont(st, text, w, h, lo, hi);
            _popTitleTx[oldest] = text;
            _popTitleLo[oldest] = lo;
            _popTitleHi[oldest] = hi;
            _popTitleMark[oldest] = mark;
            _popTitleW[oldest] = w;
            _popTitleH[oldest] = h;
            _popTitlePx[oldest] = px;
            _popTitleSerial++;
            _popTitleUse[oldest] = _popTitleSerial;
            return px;
        }

        static Rect SnapPopupRect(Rect r)
        {
            float x = Mathf.Round(r.x);
            float y = Mathf.Round(r.y);
            float w = Mathf.Round(r.width);
            float h = Mathf.Round(r.height);
            if (w < 1f) w = 1f;
            if (h < 1f) h = 1f;
            return new Rect(x, y, w, h);
        }

        // Shared pop-up title. `rest` is the full-open seat (not the lamp scale).
        // The point size is cached from that seat. The seat snaps to whole pixels.
        // openK scales the snapped seat around pivot only while the card is opening
        // or tucking. openK == 1 leaves the matrix alone, so a later breathe cannot
        // walk the glyphs across a pixel. blackPx < 0 derives the outline from inkFrac.
        static void DrawPopupTitle(
            Rect rest, string text, GUIStyle st, Color fill,
            int lo, int hi, float fitWFrac, float fitHFrac,
            int whitePx, int blackPx, float inkFrac, float minA,
            float openK, Vector2 pivot)
        {
            if (st == null || string.IsNullOrEmpty(text)) return;
            if (fill.a < minA) return;
            if (rest.width < 4f || rest.height < 4f) return;
            var seat = SnapPopupRect(rest);
            if (fitWFrac <= 0f) fitWFrac = 0.92f;
            if (fitHFrac <= 0f) fitHFrac = 0.90f;
            int px = PopupTitleSize(st, text, seat.width * fitWFrac, seat.height * fitHFrac, lo, hi);
            st.fontSize = px;
            int dark;
            if (blackPx >= 0)
                dark = blackPx;
            else
            {
                float frac = inkFrac > 0f ? inkFrac : 0.16f;
                dark = Mathf.Max(2, Mathf.RoundToInt(px * frac));
                if (frac >= 0.15f && dark > 7) dark = 7;
            }
            bool move = openK < 0.9995f || openK > 1.0005f;
            var prev = GUI.matrix;
            if (move)
                GUIUtility.ScaleAroundPivot(new Vector2(openK, openK), pivot);
            StampOutlined(seat, text, st, fill, whitePx, dark, minA);
            if (move) GUI.matrix = prev;
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
            int titleHi = Mathf.Max(18, Mathf.RoundToInt(34f * s));
            DrawPopupTitle(titleR, "Daily Bonus", _dailyTitle, new Color(1f, 0.96f, 0.78f),
                14, titleHi, 0.90f, 0.90f, 1, -1, 0.10f, 0.04f, 1f, titleR.center);
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
            float x = row.x;
            int days = DailyBonus.CycleDays;
            for (int i = 0; i < days; i++)
            {
                bool now = i == today && DailyBonus.OfferReady;
                bool done = i < today || (i == today && claimed);
                var tile = new Rect(x, row.y, unit, row.height);
                DrawDailyTile(tile, i, now, done, s, breathe, glow);
                x += unit + gap;
            }
        }

        // The one box path for all five rewards ($10 to $100). Only the amount and the
        // now / done state differ. The amount fills the whole box and the style is
        // MiddleCenter, so every figure sits dead center both ways. No crown, no stars.
        static void DrawDailyTile(Rect tile, int day, bool now, bool done, float s, float breathe, Texture2D glow)
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
            GUI.color = fill;
            GUI.DrawTexture(tile, Texture2D.whiteTexture);
            GUI.color = new Color(1f, 0.86f, 0.42f, now ? 1f : done ? 0.55f : 0.40f);
            float edge = Mathf.Max(1.5f, (now ? 2.6f : 1.6f) * s);
            GUI.DrawTexture(new Rect(tile.x, tile.y, tile.width, edge), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(tile.x, tile.yMax - edge, tile.width, edge), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(tile.x, tile.y, edge, tile.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(tile.xMax - edge, tile.y, edge, tile.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var amt = new Rect(tile.x + 1f, tile.y, tile.width - 2f, tile.height);
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

        // Rhythm Easter egg reaction: a gold ring swells off the button and sparkles fly
        // out for about a second, so the player sees it fired. No-op when idle.
        static void DrawEasterPulse(Rect disc, float t)
        {
            if (_claimEaster < 0f) return;
            float age = t - _claimEaster;
            if (age < 0f || age > 1.1f) return;
            float u = age / 1.1f;
            float fade = 1f - u;
            var glow = GlowTex();
            if (glow != null)
            {
                float grow = 1f + 0.9f * u;
                float w = disc.width * 1.5f * grow;
                float h = disc.height * 1.5f * grow;
                GUI.color = new Color(1f, 0.82f, 0.30f, 0.70f * fade);
                GUI.DrawTexture(new Rect(disc.center.x - w * 0.5f, disc.center.y - h * 0.5f, w, h), glow, ScaleMode.ScaleToFit, true);
            }
            var spark = SpriteCatalog.Sparkle;
            var tex = spark != null && spark.texture != null ? spark.texture : null;
            if (tex != null)
            {
                var prev = GUI.matrix;
                for (int i = 0; i < 6; i++)
                {
                    float ang = (i / 6f) * Mathf.PI * 2f + 0.4f;
                    float reach = disc.width * (0.42f + 0.50f * u);
                    float sz = disc.width * 0.22f * (0.5f + fade);
                    var c = new Vector2(disc.center.x + Mathf.Cos(ang) * reach, disc.center.y + Mathf.Sin(ang) * reach * 0.8f);
                    var sr = new Rect(c.x - sz * 0.5f, c.y - sz * 0.5f, sz, sz);
                    GUIUtility.RotateAroundPivot(t * 180f + i * 30f, sr.center);
                    GUI.color = new Color(1f, 0.96f, 0.75f, 0.95f * fade);
                    GUI.DrawTexture(sr, tex, ScaleMode.ScaleToFit, true);
                    GUI.matrix = prev;
                }
            }
            GUI.color = Color.white;
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
            DrawEasterPulse(disc, t);
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
            float w = StandardPopupWidth(s);
            float band = Mathf.Max(8f, 10f * s);
            float pad = band + 12f * s;
            float innerW = w - pad * 2f;
            if (innerW < 40f) innerW = 40f;
            float btnH = PopupBarHeight(s, w);
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

        // Same wood frame as daily and welcome. band is the brass width.
        static void DrawAskFrame(Rect card, float s, float breathe, Texture2D glow)
        {
            float band = Mathf.Clamp(card.width * 0.045f, 10f * s, 18f * s);
            var board = new Rect(
                card.x + band,
                card.y + band,
                Mathf.Max(8f, card.width - band * 2f),
                Mathf.Max(8f, card.height - band * 2f));
            DrawDailyFrame(card, board, band, s, breathe, glow);
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
            int welcomeHi = Mathf.Max(22, Mathf.RoundToInt(30f * s));
            DrawPopupTitle(titleR, WelcomeTitle, _dailyAsk, new Color(1f, 0.94f, 0.62f),
                16, welcomeHi, 1f, 0.9f, 1, 2, 0f, 0.04f, 1f, titleR.center);
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
            float w = StandardPopupWidth(s);
            float h = Mathf.Max(248f * s, w * 0.50f);
            float capH = Mathf.Min(Screen.height * 0.58f, 360f * s);
            if (h > capH) h = capH;
            if (h < 220f * s && capH > 220f * s) h = 220f * s;
            card = PlacePopup(s, w, h, 0.42f);
            float band = Mathf.Max(10f, 12f * s);
            float pad = band + 16f * s;
            float btnH = PopupBarHeight(s, w);
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
