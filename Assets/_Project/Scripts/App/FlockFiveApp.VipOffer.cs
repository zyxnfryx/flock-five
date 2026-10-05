using UnityEngine;

namespace FlockFive
{
    // Medallion opens this. Buy is the gold button only.
    // Copy is the real NoAds deal: stage-clear ads off, extra-branch
    // ads still optional, one-time purchase. Price comes from the store.
    // Build 58: one frame, two faces, plus a one-time welcome.
    //   Offer  (not owned): headline, perks, Buy flower, Restore link.
    //   Member (owned):     "You're a VIP!" thank-you, perks, Restore link, no Buy.
    //   Welcome:            crown pop + sparkles + "Welcome, VIP!" caption + fanfare,
    //                       once per install after purchase / restore / reinstall.
    // Shared: VipCardLayout + DrawAdCardFrame (frame and panel), PopupMotion (open/close
    // easing), DrawVipMedal (rail badge, member crest, welcome crown), DrawVipTwinkles
    // (sparkle), PaintCoachCaption (standard caption box), SfxLibrary "fanfare" (MixDesk Lead).
    public sealed partial class FlockFiveApp
    {
        static class VipOffer
        {
            const string Headline = "Go VIP: play without interruptions";
            const string MemberHeadline = "You're a VIP! Thank you";
            const string BuyLabel = "Go VIP";
            const string RestoreLabel = "Already a VIP? Restore purchase";
            const string MemberRestoreLabel = "New device? Restore purchase";
            const string WelcomeLine = "Welcome, VIP!";
            const string WelcomedKey = "flockfive.vip.welcomed";
            const float BuyScale = VipBuyScale;

            // Build 58 pacing (Brandon, build 51: the entrance felt hurried and the text moved
            // while the button glow played). One beat at a time; each starts after the last ends.
            // All ages are Time.unscaledTime deltas, so the pace is the same at any frame rate.
            //   First open (full):  dim + rail glow 0-0.45 | crown flies 0.45-0.95, fades to 1.03
            //                       | card eases in 1.03-1.48 (thud + burst as it lands)
            //                       | title 1.72-1.94 | rows 1.94 / 2.10 / 2.26 | Buy pops 2.42-2.72.
            //   Later opens:        dim + glow 0-0.22 | card 0.22-0.58 | title 0.64-0.80
            //                       | rows 0.80 / 0.90 / 1.00 | Buy pops 1.12-1.40.
            //   Member card:        dim + glow 0-0.22 | card 0.22-0.58 | crest pops 0.58-0.88
            //                       | title 0.90-1.06 | rows 1.06 / 1.16 / 1.26.
            //   Close:              Buy / crest and copy out 0-0.12 | card shrinks + fades
            //                       0.08-0.30 | dim out 0.06-0.30.
            const float FullGlow = 0.45f;
            const float ShortGlow = 0.22f;
            const float CrownStart = 0.45f;
            const float CrownDur = 0.50f;
            const float CrownFade = 0.08f;
            const float FullPanelStart = 1.03f;
            const float FullPanelDur = 0.45f;
            const float ShortPanelDur = 0.36f;
            const float FullThudAt = FullPanelStart + FullPanelDur * 0.37f;
            const float BurstDur = 0.40f;
            const float FullTitleStart = 1.72f;
            const float FullTitleFade = 0.22f;
            const float FullRowStep = 0.16f;
            const float FullRowFade = 0.16f;
            const float FullBuyStart = 2.42f;
            const float ShortTitleStart = 0.64f;
            const float ShortTitleFade = 0.16f;
            const float ShortRowStep = 0.10f;
            const float ShortRowFade = 0.12f;
            const float ShortBuyStart = 1.12f;
            const float BuyPopDur = 0.30f;
            const float CrestStart = 0.58f;
            const float CrestDur = 0.30f;
            const float MemberTitleStart = 0.90f;
            const float CloseDur = 0.30f;
            const float RowRise = 6f;

            // Welcome: dim 0-0.25 | crown pops 0.12-0.62 (fanfare) | burst 0.40-1.10
            // | caption 0.72-1.00 | hold to 3.0 (tap skips) | crown flies to the badge 0.6.
            const float WelPopStart = 0.12f;
            const float WelPopDur = 0.50f;
            const float WelBurstStart = 0.40f;
            const float WelBurstDur = 0.70f;
            const float WelCapStart = 0.72f;
            const float WelCapFade = 0.28f;
            const float WelHold = 3.0f;
            const float WelExit = 0.60f;
            const float WelSkipAfter = 0.45f;

            static readonly string[] Benefits =
            {
                "No ads between stages",
                "Rewarded ads for a branch stay optional",
                "One-time purchase. No subscription."
            };

            static readonly string[] MemberPerks =
            {
                "No ads between stages",
                "Rewarded ads for a branch stay optional",
                "Restore keeps VIP on every device"
            };

            static readonly float[] _rowA = new float[3];

            static bool _open;
            static bool _member;
            static bool _fullPlayed;
            static bool _full;
            static bool _reduce;
            static bool _skipped;
            static bool _thud;
            static int _revealed;
            static float _showAt = -1f;
            static float _closeAt = -1f;
            static Vector2 _anchor;
            static bool _anchored;
            static int _noteSerial = -1;
            static float _noteUntil;
            static GUIStyle _title, _body, _buy, _link;

            static bool _welcome;
            static float _welAt = -1f;
            static float _welExitAt = -1f;
            static bool _welFanfare;

            // True while a card or the welcome owns the screen (home treats it as modal).
            public static bool IsOpen => _open || _welcome;

            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
            static void Reset()
            {
                _open = false;
                _member = false;
                _fullPlayed = false;
                _showAt = -1f;
                _closeAt = -1f;
                _noteUntil = 0f;
                _noteSerial = -1;
                _anchored = false;
                _welcome = false;
                _welAt = -1f;
                _welExitAt = -1f;
            }

            public static void Anchor(Rect rail)
            {
                _anchor = rail.center;
                _anchored = rail.width > 4f;
            }

            // Offer when not owned, the member thank-you card when owned.
            public static void Show()
            {
                if (_open || _welcome) return;
                if (_app != null && !_app.GatePopup(PopupKind.Vip, true)) return;
                _member = NoAds.Owned;
                _open = true;
                _closeAt = -1f;
                _showAt = Time.unscaledTime;
                _skipped = false;
                _thud = false;
                _revealed = 0;
                _noteUntil = 0f;
                _noteSerial = NoAds.RestoreSerial;
                _reduce = PlayerPrefs.GetInt("flockfive.reduceMotion", 0) == 1;
                _full = !_member && !_fullPlayed && !_reduce;
                if (_full)
                {
                    _fullPlayed = true;
                    SfxLibrary.Play("fanfare", 0.34f, 0f);
                }
                else
                    Sfx.CardTap();
            }

            // Hard stop (left the home face). The welcome stays marked as shown.
            public static void Close()
            {
                _open = false;
                _closeAt = -1f;
                _welcome = false;
            }

            // Home face, every pass. Owned while the offer is up (Buy or Restore landed):
            // the offer closes. Owned and never welcomed on this install: the welcome runs
            // once the home is free (no card, lesson, reward pay or streak board).
            public static void Tick()
            {
                if (!NoAds.Owned) return;
                if (_open && !_member && !Closing) BeginClose(true);
                if (_open || _welcome) return;
                if (PlayerPrefs.GetInt(WelcomedKey, 0) != 0) return;
                if (_app == null || !WelcomeClear()) return;
                StartWelcome();
            }

            static bool WelcomeClear()
            {
                if (!_app.TutorialGateClear()) return false;
                if (_app.RewardPayBusy() || _app._streakSlide >= 0f) return false;
                return !Ads.IsBusy;
            }

            static void BeginClose(bool quiet)
            {
                if (!_open || Closing) return;
                if (!quiet) Sfx.CardTap();
                if (_reduce)
                {
                    _open = false;
                    return;
                }
                _closeAt = Time.unscaledTime;
            }

            static void Dismiss() => BeginClose(false);

            static bool Closing => _closeAt >= 0f;

            static float Age => _showAt < 0f ? 99f : Time.unscaledTime - _showAt;

            static float Span
            {
                get
                {
                    if (_reduce) return 0f;
                    if (_member) return MemberTitleStart + ShortTitleFade + ShortRowStep * 2f + ShortRowFade;
                    return (_full ? FullBuyStart : ShortBuyStart) + BuyPopDur;
                }
            }

            public static void Draw(float s)
            {
                if (_welcome)
                {
                    DrawWelcome(s);
                    return;
                }
                if (!_open) return;
                SyncNote();
                string note = NoAds.RestoreNote;
                float noteLife = _noteUntil - Time.unscaledTime;
                bool showNote = !string.IsNullOrEmpty(note) && noteLife > 0f;
                if (!_member && NoAds.Owned && !Closing) BeginClose(true);
                if (!_open) return;

                float now = Time.unscaledTime;
                float closeT = Closing ? now - _closeAt : 0f;
                if (Closing && closeT >= CloseDur)
                {
                    _open = false;
                    _closeAt = -1f;
                    return;
                }
                float age = Age;
                bool entering = !Closing && !_reduce && !_skipped && age < Span;
                var safe = Screen.safeArea;
                float top = TopHud();
                float xSz = Mathf.Max(48f * s, 44f);
                var xBtn = new Rect(
                    Screen.width - Mathf.Max(14f, Screen.width - safe.xMax + 8f) - xSz,
                    top, xSz, xSz);

                VipCardLayout(s, !_member, out var card, out var flower);
                if (!_member) flower = ScaledAbout(flower, BuyScale);
                VipBottomRects(s, out var link, out var status);
                SeatCard(s, status, ref card, ref flower);
                var crest = _member ? VipCrestRect(card) : default;

                // Same hit order in both faces so control ids stay stable.
                var discHit = _member ? default : GrowDisc(FlowerDisc(flower, 0f), s);
                bool xHit = HitPad(xBtn, out bool xHeld);
                bool buy = HitPad(discHit, out bool buyHeld);
                bool restore = HitPad(link, out bool linkHeld);
                var body = card;
                if (_member) body = Union(body, crest);
                else body = Union(body, flower);
                var swallow = new Rect(body.x - 12f, body.y - 12f, body.width + 24f, body.height + 24f);
                bool onCard = HitPad(swallow, out _);
                bool outside = HitPad(new Rect(0f, 0f, Screen.width, Screen.height), out _);
                if (outside && (swallow.Contains(Event.current.mousePosition) || link.Contains(Event.current.mousePosition)))
                    outside = false;

                float breathe = 0.5f + 0.5f * Mathf.Sin(now * 2.05f);
                var glow = GlowTex();

                // Close beats: contents first, then the frame, dim under both.
                float outCopy = Closing ? 1f - PopupMotion.Smooth(closeT / 0.12f) : 1f;
                float outPop = Closing ? 1f - EaseInCubic(closeT / 0.12f) : 1f;
                float outFrameU = Closing ? PopupMotion.Beat(closeT, 0.08f, CloseDur - 0.08f) : 0f;
                float outDim = Closing ? 1f - PopupMotion.Smooth(PopupMotion.Beat(closeT, 0.06f, CloseDur - 0.06f)) : 1f;

                float dimIn = (_reduce || _skipped) ? 1f : PopupMotion.Smooth(age / 0.25f);
                GUI.color = new Color(0.04f, 0.03f, 0.02f, (entering ? 0.78f : 0.72f) * dimIn * outDim);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
                if (!Closing) DrawArrivalGlow(glow, s, age);

                int reveal = RevealCount(age);
                NoteTicks(reveal);
                NoteThud(age);

                bool fullLive = _full && !_reduce && !_skipped && !Closing;
                if (fullLive)
                    DrawCrownFlight(s, age, card);
                if (fullLive && age >= FullThudAt && age < FullThudAt + BurstDur)
                    DrawVipBurst(new Vector2(card.center.x, card.y + card.height * 0.34f), (age - FullThudAt) / BurstDur, s);

                float k = Closing ? PopupMotion.ExitScale(outFrameU) : PanelScale(age);
                float frameA = Closing ? PopupMotion.ExitAlpha(outFrameU) : PanelAlpha(age);
                var pivot = card.center;
                var prev = GUI.matrix;
                if (frameA > 0.02f)
                {
                    // Scale right up to rest; at rest the matrix is untouched.
                    if (Mathf.Abs(k - 1f) > 0.0005f)
                        GUIUtility.ScaleAroundPivot(new Vector2(k, k), pivot);
                    var plate = DrawAdCardFrame(card, s, breathe, frameA, true);
                    float titleA = TitleAlpha(age) * outCopy * frameA;
                    RowAlphas(age, outCopy * frameA);
                    DrawCopy(plate, s, card.width, titleA, _member ? MemberHeadline : Headline, _member ? MemberPerks : Benefits);
                    if (!entering && !Closing)
                        DrawVipTwinkles(card, 0.55f, 0.055f);
                    GUI.matrix = prev;
                }

                // Buy flower (offer) or crest (member) pops as its own last beat, outside the
                // card matrix, and leaves first on close.
                float popK = PopScaleAt(age) * outPop;
                if (_member)
                {
                    if (popK > 0.03f)
                        DrawVipMedal(ScaledAbout(crest, popK), s, false, true, false);
                }
                else if (popK > 0.03f)
                {
                    float shineStart = (_full ? FullBuyStart : ShortBuyStart) + BuyPopDur + 0.04f;
                    float shineU = _reduce ? -1f : (age - shineStart) / 0.55f;
                    DrawBuy(popK < 0.999f ? ScaledAbout(flower, popK) : flower, buyHeld, s, shineU, !entering && !Closing);
                }

                float chromeA = dimIn * outDim;
                DrawRestore(link, linkHeld, s, _member ? MemberRestoreLabel : RestoreLabel, chromeA);
                if (showNote)
                {
                    float alpha = noteLife > 0.8f ? 1f : noteLife / 0.8f;
                    DrawBottomStatus(status, note, alpha * outDim, s);
                }
                if (!Closing) DrawGiftCloseX(xBtn, xHeld, s);
                GUI.color = Color.white;

                // Closing: taps are swallowed above and do nothing else.
                if (Closing) return;
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
                bool buyIn = _reduce || _skipped || popK > 0.92f;
                if (!_member && buy && buyIn)
                {
                    Sfx.CardTap();
                    NoAds.Buy();
                    if (NoAds.Owned) BeginClose(true);
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

            static Rect Union(Rect a, Rect b)
            {
                if (b.width < 1f || b.height < 1f) return a;
                float x0 = Mathf.Min(a.x, b.x);
                float y0 = Mathf.Min(a.y, b.y);
                float x1 = Mathf.Max(a.xMax, b.xMax);
                float y1 = Mathf.Max(a.yMax, b.yMax);
                return new Rect(x0, y0, x1 - x0, y1 - y0);
            }

            static void SyncNote()
            {
                if (NoAds.RestoreSerial == _noteSerial) return;
                _noteSerial = NoAds.RestoreSerial;
                _noteUntil = string.IsNullOrEmpty(NoAds.RestoreNote) ? 0f : Time.unscaledTime + 3.6f;
            }

            static float TitleStart => _member ? MemberTitleStart : (_full ? FullTitleStart : ShortTitleStart);
            static float TitleFade => _full ? FullTitleFade : ShortTitleFade;
            static float RowStep => _full ? FullRowStep : ShortRowStep;
            static float RowFade => _full ? FullRowFade : ShortRowFade;

            static float TitleAlpha(float age)
            {
                if (_reduce || _skipped) return 1f;
                return PopupMotion.Smooth(PopupMotion.Beat(age, TitleStart, TitleFade));
            }

            // Rows start once the title is fully in, one after another.
            static void RowAlphas(float age, float mul)
            {
                float r0 = TitleStart + TitleFade;
                for (int i = 0; i < _rowA.Length; i++)
                {
                    float a = (_reduce || _skipped) ? 1f : PopupMotion.Smooth(PopupMotion.Beat(age, r0 + RowStep * i, RowFade));
                    _rowA[i] = a * mul;
                }
            }

            static int RevealCount(float age)
            {
                if (!_full || _reduce || _skipped) return Benefits.Length;
                float r0 = FullTitleStart + FullTitleFade;
                int n = 0;
                for (int i = 0; i < Benefits.Length; i++)
                    if (age >= r0 + FullRowStep * i) n++;
                return n;
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
                if (_skipped || age < FullThudAt) return;
                _thud = true;
                SfxLibrary.Play("thud", 0.40f, 0f);
                if (CamShake.Live != null)
                    CamShake.Live.Punch(0.14f, 0.05f, 1.1f, 0.03f);
            }

            static float PanelStart => _full ? FullPanelStart : ShortGlow;
            static float PanelDur => _full ? FullPanelDur : ShortPanelDur;

            static float PanelScale(float age)
            {
                if (_reduce || _skipped) return 1f;
                return PopupMotion.EnterScale(PopupMotion.Beat(age, PanelStart, PanelDur));
            }

            static float PanelAlpha(float age)
            {
                if (_reduce || _skipped) return 1f;
                return PopupMotion.EnterAlpha(PopupMotion.Beat(age, PanelStart, PanelDur));
            }

            // Buy flower / member crest: 0 until its beat, overshoot pop, exactly 1 at rest.
            static float PopScaleAt(float age)
            {
                if (_reduce || _skipped) return 1f;
                float start = _member ? CrestStart : (_full ? FullBuyStart : ShortBuyStart);
                float dur = _member ? CrestDur : BuyPopDur;
                float u = PopupMotion.Beat(age, start, dur);
                return u <= 0f ? 0f : PopupMotion.PopScale(u);
            }

            static void SeatCard(float s, Rect status, ref Rect card, ref Rect flower)
            {
                float ceiling = status.y - 12f * s;
                // The member crest rides above the card, so it is what must clear the logo.
                float floor = SplashTitleHalo().yMax + 8f * s + (_member ? VipCrestOverhang(card.height) : 0f);
                float low = Mathf.Max(card.yMax, flower.yMax);
                if (low > ceiling)
                {
                    float shift = low - ceiling;
                    card.y -= shift;
                    flower.y -= shift;
                }
                if (card.y < floor)
                {
                    float push = floor - card.y;
                    float room = ceiling - Mathf.Max(card.yMax, flower.yMax);
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
                if (_reduce || _skipped || glow == null) return;
                // Rises and falls inside [0, dur] and is gone before the next beat starts.
                float dur = _full ? FullGlow : ShortGlow;
                if (age >= dur) return;
                float half = dur * 0.5f;
                float a = Mathf.Clamp01(1f - Mathf.Abs(age - half) / half);
                a = PopupMotion.Smooth(a);
                if (a < 0.04f) return;
                Vector2 from = _anchored ? _anchor : new Vector2(Screen.width * 0.82f, TopHud() + 40f * s);
                float d = Mathf.Lerp(80f * s, 280f * s, EaseOutCubic(age / dur));
                GUI.color = new Color(1f, 0.78f, 0.28f, 0.55f * a);
                GUI.DrawTexture(new Rect(from.x - d * 0.5f, from.y - d * 0.5f, d, d), glow, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
            }

            static void DrawCrownFlight(float s, float age, Rect card)
            {
                if (age < CrownStart) return;
                float u = Mathf.Clamp01((age - CrownStart) / CrownDur);
                float fade = u < 1f ? 1f : Mathf.Clamp01(1f - (age - (CrownStart + CrownDur)) / CrownFade);
                if (fade < 0.04f) return;
                var tex = VipCrownTex();
                if (tex == null) return;
                Vector2 from = _anchored ? _anchor : new Vector2(Screen.width * 0.82f, TopHud() + 36f * s);
                Vector2 to = new Vector2(card.center.x, card.y + card.height * 0.30f);
                // Ease in-out: lifts off gently, glides, and slows into the card spot.
                float e = PopupMotion.Smooth(u);
                Vector2 p = Vector2.Lerp(from, to, e);
                float sz = Mathf.Lerp(36f * s, 92f * s, e) * (card.width / Mathf.Max(1f, StandardPopupWidth(s)));
                GUI.color = new Color(1f, 0.95f, 0.72f, fade);
                GUI.DrawTexture(new Rect(p.x - sz * 0.5f, p.y - sz * 0.55f, sz, sz * 0.8f), tex, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
            }

            static void DrawVipBurst(Vector2 c, float u, float s)
            {
                u = Mathf.Clamp01(u);
                var spark = SpriteCatalog.Sparkle;
                var tex = spark != null && spark.texture != null ? spark.texture : Texture2D.whiteTexture;
                float eu = EaseOutCubic(u);
                for (int i = 0; i < 12; i++)
                {
                    float ang = i * 0.5236f + 0.4f;
                    float reach = Mathf.Lerp(10f * s, 86f * s, eu);
                    float sz = Mathf.Lerp(14f * s, 5f * s, u);
                    float a = (1f - u) * 0.85f;
                    var r = new Rect(c.x + Mathf.Cos(ang) * reach - sz * 0.5f, c.y + Mathf.Sin(ang) * reach - sz * 0.5f, sz, sz);
                    GUI.color = new Color(1f, 0.86f, 0.35f, a);
                    GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit, true);
                }
                GUI.color = Color.white;
            }

            // Title centred, three rows left-aligned on one check column. One bold face:
            // the title fits its own box and ALL rows share one size (the smallest any row
            // needs) and one outline weight. Caps grow with the card (cardW / standard
            // width) so type stays proportional to the bigger frame. Copy fades in after the
            // card has settled; rows rise a few pixels as they fade (they never scale).
            static void DrawCopy(Rect plate, float s, float cardW, float titleA, string headline, string[] rows)
            {
                float grow = cardW / Mathf.Max(1f, StandardPopupWidth(s));
                float insetX = Mathf.Max(12f * s, plate.width * 0.06f);
                float insetY = Mathf.Max(10f * s, plate.height * 0.07f);
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
                if (titleA >= 0.04f)
                {
                    int titleHi = Mathf.Max(28, Mathf.RoundToInt(60f * s * grow));
                    _title.fontSize = FitFontWrapped(_title, headline, titleR.width * 0.96f, titleR.height * 0.92f, 16, titleHi);
                    int titleInk = Mathf.Max(3, Mathf.RoundToInt(_title.fontSize * 0.14f));
                    StampOutlined(titleR, headline, _title, new Color(1f, 0.97f, 0.86f, titleA), 0, titleInk);
                }

                if (_body == null)
                    _body = new GUIStyle(GUI.skin.label)
                    {
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleLeft,
                        wordWrap = true
                    };
                float gap = 6f * s * grow;
                float rowsTop = titleR.yMax + gap;
                int n = Mathf.Min(rows.Length, _rowA.Length);
                float rowH = (block.yMax - rowsTop) / Mathf.Max(1, n);
                int bodyHi = Mathf.Max(16, Mathf.RoundToInt(29f * s * grow));
                float mark = Mathf.Min(rowH * 0.46f, 26f * s * grow);
                float labX = block.x + mark + 8f * s;
                float labW = Mathf.Max(20f, block.xMax - labX);
                int bodyPx = bodyHi;
                for (int i = 0; i < n; i++)
                {
                    int px = FitFontWrapped(_body, rows[i], labW, rowH * 0.88f, 13, bodyHi);
                    if (px < bodyPx) bodyPx = px;
                }
                _body.fontSize = bodyPx;
                int ink = Mathf.Max(3, Mathf.RoundToInt(bodyPx * 0.16f));
                for (int i = 0; i < n; i++)
                {
                    float a = _rowA[i];
                    if (a < 0.04f) continue;
                    float rise = (1f - a) * RowRise * s;
                    var row = new Rect(block.x, rowsTop + rowH * i + rise, block.width, rowH);
                    var mk = new Rect(block.x, row.center.y - mark * 0.5f, mark, mark);
                    DrawCheckMark(mk, new Color(0.55f, 0.85f, 0.42f, a));
                    var lab = new Rect(labX, row.y, labW, row.height);
                    StampOutlined(lab, rows[i], _body, new Color(1f, 0.97f, 0.88f, a), 0, ink);
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

            static void DrawRestore(Rect link, bool held, float s, string label, float alpha)
            {
                if (alpha < 0.04f) return;
                if (_link == null)
                    _link = new GUIStyle(GUI.skin.label)
                    {
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                        wordWrap = false
                    };
                int hi = Mathf.Max(13, Mathf.RoundToInt(16f * s));
                _link.fontSize = FitFont(_link, label, link.width * 0.96f, link.height * 0.72f, 11, hi);
                int ink = Mathf.Max(3, Mathf.RoundToInt(_link.fontSize * 0.18f));
                var col = held ? new Color(1f, 1f, 0.94f, alpha) : new Color(1f, 0.97f, 0.90f, alpha);
                StampOutlined(link, label, _link, col, 0, ink);
            }

            // ---- Welcome, VIP! (once per install) -------------------------------------

            static void StartWelcome()
            {
                PlayerPrefs.SetInt(WelcomedKey, 1);
                PlayerPrefs.Save();
                _welcome = true;
                _welAt = Time.unscaledTime;
                _welExitAt = -1f;
                _welFanfare = false;
                _reduce = PlayerPrefs.GetInt("flockfive.reduceMotion", 0) == 1;
            }

            // Crown medallion pops in centre stage with a burst and twinkles, the caption
            // settles under it in the standard caption box, then the medallion glides into
            // the rail badge it becomes. A tap after the pop skips to that glide.
            static void DrawWelcome(float s)
            {
                float now = Time.unscaledTime;
                float age = now - _welAt;
                if (_welExitAt < 0f && age >= WelHold) _welExitAt = now;
                float exitU = _welExitAt >= 0f ? PopupMotion.Beat(now - _welExitAt, 0f, _reduce ? 0.2f : WelExit) : 0f;
                if (exitU >= 1f)
                {
                    _welcome = false;
                    return;
                }
                bool tap = HitPad(new Rect(0f, 0f, Screen.width, Screen.height), out _);

                if (!_welFanfare && age >= WelPopStart)
                {
                    _welFanfare = true;
                    SfxLibrary.Play("fanfare", 0.34f, 0f);
                    Haptics.Play(Haptics.Tier.Medium);
                }

                float dimA = 0.66f * PopupMotion.Smooth(age / 0.25f) * (1f - PopupMotion.Smooth(PopupMotion.Beat(exitU, 0.35f, 0.65f)));
                GUI.color = new Color(0.04f, 0.03f, 0.02f, dimA);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;

                float d = Mathf.Min(Screen.width * 0.36f, Screen.height * 0.19f);
                float floorY = SplashTitleHalo().yMax + 12f * s;
                float cy = Mathf.Max(Screen.height * 0.38f, floorY + d * 0.5f);
                var rest = new Rect(Screen.width * 0.5f - d * 0.5f, cy - d * 0.5f, d, d);
                Rect home = _app != null ? _app.SplashNoAdsRect() : default;
                bool canFly = home.width > 4f;

                float pop = _reduce ? 1f : PopupMotion.PopScale(PopupMotion.Beat(age, WelPopStart, WelPopDur));
                if (age < WelPopStart && !_reduce) pop = 0f;
                float fly = PopupMotion.Smooth(exitU);
                Rect medal;
                if (_welExitAt >= 0f && canFly)
                {
                    medal = new Rect(
                        Mathf.Lerp(rest.x, home.x, fly),
                        Mathf.Lerp(rest.y, home.y, fly),
                        Mathf.Lerp(rest.width, home.width, fly),
                        Mathf.Lerp(rest.height, home.height, fly));
                }
                else if (_welExitAt >= 0f)
                    medal = ScaledAbout(rest, 1f - fly);
                else
                    medal = ScaledAbout(rest, pop);

                var glow = GlowTex();
                float haloA = PopupMotion.Smooth(PopupMotion.Beat(age, WelPopStart, 0.3f)) * (1f - fly);
                if (haloA > 0.02f && glow != null)
                {
                    float breathe = 0.5f + 0.5f * Mathf.Sin(now * 2.2f);
                    float h = d * (1.9f + 0.12f * breathe);
                    GUI.color = new Color(1f, 0.80f, 0.30f, 0.50f * haloA);
                    GUI.DrawTexture(new Rect(rest.center.x - h * 0.5f, rest.center.y - h * 0.5f, h, h), glow, ScaleMode.ScaleToFit, true);
                    GUI.color = Color.white;
                }
                if (!_reduce && age >= WelBurstStart && age < WelBurstStart + WelBurstDur)
                    DrawVipBurst(rest.center, (age - WelBurstStart) / WelBurstDur, s * (d / Mathf.Max(1f, 120f * s)));
                float twA = PopupMotion.Smooth(PopupMotion.Beat(age, WelPopStart + 0.2f, 0.4f)) * (1f - fly);
                if (twA > 0.02f)
                {
                    float g = d * 0.55f;
                    DrawVipTwinkles(new Rect(rest.x - g, rest.y - g * 0.7f, d + g * 2f, d + g * 1.9f), twA, 0.075f);
                }
                if (medal.width > 2f)
                    DrawVipMedal(medal, s, false, true, true);

                float capA = PopupMotion.Smooth(PopupMotion.Beat(age, WelCapStart, WelCapFade))
                    * (1f - PopupMotion.Smooth(PopupMotion.Beat(exitU, 0f, 0.35f)));
                if (capA > 0.02f && _app != null)
                {
                    float w = Mathf.Min(StandardPopupWidth(s) * 0.78f, Screen.width - 32f * s);
                    float h = Mathf.Max(56f, 60f * s);
                    float y = rest.yMax + d * 0.36f + 16f * s;
                    _app.PaintWelcomeCaption(WelcomeLine, new Rect((Screen.width - w) * 0.5f, y, w, h), s, capA);
                }

                if (tap && age >= WelSkipAfter && _welExitAt < 0f)
                    _welExitAt = now;
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

        // Standard caption box at an explicit alpha (the welcome fades it out as well as
        // in). Same PaintCoachCaption plate and type as the lessons; the lesson fade clock
        // is restored afterwards so a coach line is not left half-faded.
        // The plate hugs the line (FitCaptionBox) inside `slot`, centred, top at slot.y.
        void PaintWelcomeCaption(string line, Rect slot, float s, float alpha)
        {
            int hi = Mathf.Max(20, Mathf.RoundToInt(38f * s));
            float padX = 18f * s;
            float padY = 14f * s;
            var fit = FitCaptionBox(line, Mathf.Max(48f, slot.width - padX * 2f), hi, 12);
            float w = Mathf.Min(slot.width, fit.width + padX * 2f);
            float h = Mathf.Max(slot.height, fit.height + padY * 2f);
            var r = new Rect(slot.center.x - w * 0.5f, slot.y, w, h);
            float keep = _coachFade;
            _coachFade = Mathf.Clamp01(alpha);
            PaintCoachCaption(line, r, s, 12, hi);
            _coachFade = keep;
        }
    }
}
