using UnityEngine;

namespace FlockFive
{
    // Medallion opens this. Buy is the gold button only.
    // Copy is the real NoAds deal: stage-clear ads off, extra-branch
    // ads still optional, one-time purchase. Price comes from the store.
    // Build 58: one frame, two faces, plus a one-time welcome.
    //   Offer  (not owned): headline, perks, Buy flower, Restore link.
    //   Member (owned):     "You're a VIP!" thank-you, perks, Restore link, no Buy.
    //   Welcome:            diamond badge pop + sparkles + "Welcome, VIP!" caption + fanfare,
    //                       once per install after purchase / restore / reinstall.
    // Shared: VipCardLayout + DrawAdCardFrame (frame and panel), PopupMotion (open/close
    // easing), DrawVipMedal (rail offer, owned diamond badge, member crest, welcome), DrawVipTwinkles
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
            //   First open (full):  dim + rail glow 0-0.45 | crown flies to the card centre
            //                       0.45-0.95 | burst at 0.95 (gold sparkles, light flash,
            //                       pop + thud), the crown pops into it. Build 66: the card
            //                       holds 0.32 so that sparkle pop reads, then grows out of
            //                       it 1.27-1.72 | title 2.02-2.24 | rows 2.24 / 2.40 / 2.56
            //                       | Buy pops 2.72-3.02.
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
            const float CrownFade = 0.12f;
            // The crown lands on the card centre and the burst fires there; the card's scale
            // pivot is that same centre. The card waits so the sparkle pop reads, then grows
            // out of that same centre.
            const float BurstAt = CrownStart + CrownDur;
            const float BurstDur = 0.55f;
            // Hold so the landing sparkle reads before the card grows over it.
            const float PanelAfterBurst = 0.32f;
            const float FullPanelStart = BurstAt + PanelAfterBurst;
            const float FullPanelDur = 0.45f;
            const float ShortPanelDur = 0.36f;
            const float FullTitleFade = 0.22f;
            const float FullRowStep = 0.16f;
            const float FullRowFade = 0.16f;
            // Title still waits 0.30s after the card settles; Buy still starts as the last row finishes.
            const float FullTitleStart = FullPanelStart + FullPanelDur + 0.30f;
            const float FullBuyStart = FullTitleStart + FullTitleFade + FullRowStep * 2f + FullRowFade;
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

            // Welcome: dim 0-0.25 | diamond badge pops 0.12-0.62 (fanfare) | burst 0.40-1.10
            // | caption 0.72-1.00 | hold to 3.0 (tap skips) | badge flies to the rail 0.6.
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

            // Purchase landed, or the welcome diamond is still flying home. The rail slot
            // stays packed (SplashNoAdsRect stays put) but does not draw or take taps until
            // the flying medal finishes, so the rail badge is the one that landed.
            public static bool VipWelcomeHoldsRail { get; private set; }

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
                VipWelcomeHoldsRail = false;
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
                // Left home. A welcome in flight will not resume (it is already marked shown),
                // so the owner's rail badge comes back. Returning welcomed owners never hold.
                VipWelcomeHoldsRail = false;
            }

            // Home face, every pass. Owned while the offer is up (Buy or Restore landed):
            // the offer closes. Owned and never welcomed on this install: the welcome runs
            // once the home is free (no card, lesson, reward pay or streak board).
            public static void Tick()
            {
                // Before the home rail draws: a finished fly hands the slot to the rail badge
                // on this same frame, at SplashNoAdsRect, with no second medal.
                SettleWelcome();
                if (!NoAds.Owned)
                {
                    VipWelcomeHoldsRail = false;
                    return;
                }
                if (_open && !_member && !Closing) BeginClose(true);
                if (_open || _welcome) return;
                if (PlayerPrefs.GetInt(WelcomedKey, 0) != 0)
                {
                    VipWelcomeHoldsRail = false;
                    return;
                }
                if (_app == null || !WelcomeClear()) return;
                StartWelcome();
            }

            // Age and the skip / reduce-motion exit share this. exitU >= 1 ends the hold.
            static void SettleWelcome()
            {
                if (!_welcome) return;
                float now = Time.unscaledTime;
                if (_welExitAt < 0f && now - _welAt >= WelHold) _welExitAt = now;
                if (_welExitAt < 0f) return;
                float exitU = PopupMotion.Beat(now - _welExitAt, 0f, _reduce ? 0.2f : WelExit);
                if (exitU < 1f) return;
                _welcome = false;
                VipWelcomeHoldsRail = false;
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
                // Buy / Restore just landed on the offer face. Hide the rail badge until the
                // welcome medal flies into that slot. A member card closing does not hold.
                if (NoAds.Owned && !_member) VipWelcomeHoldsRail = true;
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
                var xBtn = CornerCloseRect(s, TopHud());

                VipCardLayout(s, !_member, out var card, out var flower);
                if (!_member) flower = ScaledAbout(flower, BuyScale);
                VipBottomRects(s, out var link, out var status);
                SeatCard(s, status, ref card, ref flower);
                var crest = _member ? VipCrestRect(card) : default;

                // Same hit order in both faces so control ids stay stable.
                var discHit = _member ? default : GrowDisc(FlowerDisc(flower, 0f), s);
                bool xHit = HitPad(CloseTapRect(xBtn), out bool xHeld);
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
                // Build 63: the offer face dims a little deeper so the LEVEL flower and rails
                // under the bigger card recede (nothing behind it moves or resizes).
                float dimTo = _member ? (entering ? 0.78f : 0.72f) : (entering ? 0.84f : 0.80f);
                GUI.color = new Color(0.04f, 0.03f, 0.02f, dimTo * dimIn * outDim);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
                if (!Closing) DrawArrivalGlow(glow, s, age);

                int reveal = RevealCount(age);
                NoteTicks(reveal);
                NoteThud(age);

                bool fullLive = _full && !_reduce && !_skipped && !Closing;
                if (fullLive)
                    DrawCrownFlight(s, age, card);
                if (fullLive && age >= BurstAt && age < BurstAt + BurstDur)
                    DrawVipBurst(card.center, (age - BurstAt) / BurstDur, BurstScale(s, card), 1f);

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
                        DrawVipFrameTwinkles(card, plate, 0.55f, 0.055f);
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

            // The burst's sound: the shared card pop on the VIP thud, a medium haptic, and the
            // same small camera punch the landing had.
            static void NoteThud(float age)
            {
                if (_thud || !_full || _reduce) return;
                if (_skipped || age < BurstAt) return;
                _thud = true;
                Sfx.CardPop();
                SfxLibrary.Play("thud", 0.40f, 0f);
                Haptics.Play(Haptics.Tier.Medium);
                if (CamShake.Live != null)
                    CamShake.Live.Punch(0.14f, 0.05f, 1.1f, 0.03f);
            }

            // Burst reach follows the card (the old 86 px reach was sized for the standard card).
            static float BurstScale(float s, Rect card) =>
                s * Mathf.Max(1f, card.width / Mathf.Max(1f, StandardPopupWidth(s))) * 1.6f;

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

            // Opaque clay starts about 13% down the flower square (PopupCtaOverlap).
            const float VipPedestalFrac = 0.13f;

            static float FlowerArtTop(Rect flower) => flower.y + flower.height * VipPedestalFrac;

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
                    // Last resort, and never under today's 1.08 (flower is already at VipBuyScale).
                    float over = flower.yMax - ceiling;
                    float minK = 1.08f / Mathf.Max(0.01f, VipBuyScale);
                    float k = Mathf.Clamp(1f - over / flower.height, minK, 1f);
                    flower = ScaledAbout(flower, k);
                    flower.y = ceiling - flower.height;
                }
                // Offer only. The bigger flower must not cover the perks plate. Shift the card
                // up off the art first. Shrink only if the title (or the close X band) blocks
                // that shift, and never below today's size. The member crest does not come here.
                if (_member || flower.height < 8f) return;
                var plate = AdCardPlate(card);
                float cover = plate.yMax - FlowerArtTop(flower);
                if (cover <= 1f) return;
                float can = card.y - floor;
                if (can < 0f) can = 0f;
                float shiftUp = cover < can ? cover : can;
                card.y -= shiftUp;
                cover -= shiftUp;
                if (cover <= 1f) return;
                float minScale = 1.08f / Mathf.Max(0.01f, VipBuyScale);
                float center = flower.center.y;
                float kClear = (center - plate.yMax) / Mathf.Max(1f, flower.height * (0.5f - VipPedestalFrac));
                if (kClear < minScale) kClear = minScale;
                if (kClear < 0.999f)
                {
                    flower = ScaledAbout(flower, kClear);
                    if (flower.yMax > ceiling) flower.y = ceiling - flower.height;
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
                // To the card centre, where the burst fires and the card grows from.
                Vector2 to = card.center;
                // Ease in-out: lifts off gently, glides, and slows into the card spot.
                float e = PopupMotion.Smooth(u);
                Vector2 p = Vector2.Lerp(from, to, e);
                float sz = Mathf.Lerp(36f * s, 92f * s, e) * (card.width / Mathf.Max(1f, StandardPopupWidth(s)));
                // On arrival it pops into the burst: swells while it fades.
                if (u >= 1f) sz *= 1f + 0.4f * (1f - fade);
                GUI.color = new Color(1f, 0.95f, 0.72f, fade);
                GUI.DrawTexture(new Rect(p.x - sz * 0.5f, p.y - sz * 0.55f, sz, sz * 0.8f), tex, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
            }

            // THE VIP burst (offer crown landing, Welcome badge pop). flash > 0 adds a light
            // flash: a warm screen wash and a white-gold bloom off the centre that fade out
            // in the first part of the burst, plus the shared SparkleFx glint at the core.
            static void DrawVipBurst(Vector2 c, float u, float s, float flash = 0f)
            {
                u = Mathf.Clamp01(u);
                if (flash > 0f)
                {
                    float f = Mathf.Clamp01(1f - u / 0.45f);
                    f *= f;
                    if (f > 0.01f)
                    {
                        GUI.color = new Color(1f, 0.93f, 0.70f, 0.30f * f * flash);
                        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
                        var bloom = GlowTex();
                        if (bloom != null)
                        {
                            float d = Mathf.Lerp(60f * s, 260f * s, EaseOutCubic(u / 0.45f));
                            GUI.color = new Color(1f, 0.90f, 0.55f, 0.85f * f * flash);
                            GUI.DrawTexture(new Rect(c.x - d * 0.5f, c.y - d * 0.5f, d, d), bloom, ScaleMode.ScaleToFit, true);
                        }
                        GUI.color = Color.white;
                        SparkleFx.DrawAt(c, Mathf.Lerp(70f * s, 40f * s, u), f * flash);
                    }
                }
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

            // Title centred, three rows left-aligned on one check column. One bold face.
            // Build 63 (Brandon: love the list, make it big): type comes from the standard
            // caption (StandardCaptionBox's top size through CaptionPx), not from the card.
            // Rows try that size first; the title rides at CopyTitleK x the rows. Both step
            // down together until title + rows + gaps fit the panel, and no single word may be
            // wider than its column (the wrapped fit only checks height). Checks are the shared
            // DrawCheckMark at CopyMarkK x the row type, seated on each row's first line.
            // Left-over height opens the gaps. Fit is cached per panel size and headline, so
            // the open / close scale (GUI.matrix) never refits. Rows fade and rise as before.
            const float CopyTitleK = 1.3f;
            const float CopyMarkK = 1.1f;
            const float CopyGapTitle = 0.6f;
            const float CopyGapRow = 0.45f;

            struct CopyFit
            {
                public float W, H;
                public string Head;
                public int TitlePx, BodyPx;
                public float TitleH, Mark, LabW, Spare;
                public float[] RowH;
            }

            static CopyFit _fit;
            static GUIContent _fitContent;

            static void EnsureCopyStyles()
            {
                if (_title == null)
                    _title = new GUIStyle(GUI.skin.label)
                    {
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.UpperCenter,
                        wordWrap = true
                    };
                if (_body == null)
                    _body = new GUIStyle(GUI.skin.label)
                    {
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.UpperLeft,
                        wordWrap = true
                    };
                if (_fitContent == null) _fitContent = new GUIContent();
            }

            static bool WordsFit(GUIStyle st, string text, float w)
            {
                var words = text.Split(' ');
                for (int i = 0; i < words.Length; i++)
                {
                    _fitContent.text = words[i];
                    if (st.CalcSize(_fitContent).x > w) return false;
                }
                return true;
            }

            static float WrappedH(GUIStyle st, string text, float w)
            {
                _fitContent.text = text;
                return st.CalcHeight(_fitContent, w);
            }

            static CopyFit FitCopy(Rect block, float s, string headline, string[] rows, int n)
            {
                if (_fit.RowH != null && _fit.Head == headline
                    && Mathf.Abs(_fit.W - block.width) < 1f && Mathf.Abs(_fit.H - block.height) < 1f)
                    return _fit;
                StandardCaptionBox(s, out _, out _, out int lo, out int hi);
                lo = CaptionPx(lo);
                hi = CaptionPx(hi);
                int titleHi = Mathf.RoundToInt(hi * 1.10f);
                var fit = new CopyFit { W = block.width, H = block.height, Head = headline, RowH = new float[n] };
                bool done = false;
                for (int bp = hi; bp >= lo && !done; bp--)
                {
                    float mark = bp * CopyMarkK;
                    float labW = Mathf.Max(20f, block.width - mark - bp * 0.45f);
                    _body.fontSize = bp;
                    bool ok = true;
                    for (int i = 0; i < n && ok; i++)
                        ok = WordsFit(_body, rows[i], labW);
                    if (!ok && bp > lo) continue;
                    int tp = Mathf.Min(titleHi, Mathf.RoundToInt(bp * CopyTitleK));
                    _title.fontSize = tp;
                    while (tp > bp && !WordsFit(_title, headline, block.width))
                        _title.fontSize = --tp;
                    float th = WrappedH(_title, headline, block.width);
                    float need = th + bp * CopyGapTitle + bp * CopyGapRow * (n - 1);
                    for (int i = 0; i < n; i++)
                    {
                        fit.RowH[i] = WrappedH(_body, rows[i], labW);
                        need += fit.RowH[i];
                    }
                    if (need > block.height && bp > lo) continue;
                    fit.TitlePx = tp;
                    fit.BodyPx = bp;
                    fit.TitleH = th;
                    fit.Mark = mark;
                    fit.LabW = labW;
                    fit.Spare = Mathf.Max(0f, block.height - need);
                    done = true;
                }
                _fit = fit;
                return fit;
            }

            static void DrawCopy(Rect plate, float s, float cardW, float titleA, string headline, string[] rows)
            {
                float insetX = Mathf.Max(14f * s, plate.width * 0.05f);
                float insetY = Mathf.Max(12f * s, plate.height * 0.045f);
                var block = new Rect(
                    plate.x + insetX,
                    plate.y + insetY,
                    plate.width - insetX * 2f,
                    Mathf.Max(48f, plate.height - insetY * 2f));
                EnsureCopyStyles();
                int n = Mathf.Min(rows.Length, _rowA.Length);
                var fit = FitCopy(block, s, headline, rows, n);
                _title.fontSize = fit.TitlePx;
                _body.fontSize = fit.BodyPx;
                // Spare height: half a share above the title and below the last row, a full
                // share in each gap.
                float share = fit.Spare / Mathf.Max(1f, n + 1f);
                float y = block.y + share * 0.5f;
                if (titleA >= 0.04f)
                {
                    int titleInk = Mathf.Max(3, Mathf.RoundToInt(fit.TitlePx * 0.14f));
                    StampOutlined(new Rect(block.x, y, block.width, fit.TitleH), headline, _title, new Color(1f, 0.97f, 0.86f, titleA), 0, titleInk);
                }
                y += fit.TitleH + fit.BodyPx * CopyGapTitle + share;
                float labX = block.xMax - fit.LabW;
                float line = fit.BodyPx * 1.15f;
                int ink = Mathf.Max(3, Mathf.RoundToInt(fit.BodyPx * 0.16f));
                for (int i = 0; i < n; i++)
                {
                    float a = _rowA[i];
                    float rowH = fit.RowH[i];
                    if (a >= 0.04f)
                    {
                        float rise = (1f - a) * RowRise * s;
                        var mk = new Rect(block.x, y + rise + (line - fit.Mark) * 0.5f, fit.Mark, fit.Mark);
                        DrawCheckMark(mk, new Color(0.55f, 0.85f, 0.42f, a));
                        StampOutlined(new Rect(labX, y + rise, fit.LabW, rowH), rows[i], _body, new Color(1f, 0.97f, 0.88f, a), 0, ink);
                    }
                    y += rowH + fit.BodyPx * CopyGapRow + share;
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
                var cream = new Color(1f, 0.98f, 0.93f, 1f);
                var verb = priced
                    ? new Rect(disc.x, disc.y + disc.height * 0.14f, disc.width, disc.height * 0.32f)
                    : new Rect(disc.x, disc.y + disc.height * 0.22f, disc.width, disc.height * 0.52f);
                int hi = Mathf.Max(18, Mathf.RoundToInt(36f * s));
                _buy.fontSize = FitFont(_buy, BuyLabel, verb.width * 0.78f, verb.height * 0.92f, 12, hi);
                int ink = Mathf.Clamp(Mathf.RoundToInt(_buy.fontSize * 0.14f), 2, 7);
                StampOutlined(verb, BuyLabel, _buy, cream, 0, ink);
                if (priced)
                {
                    var priceR = new Rect(disc.x + disc.width * 0.06f, verb.yMax, disc.width * 0.88f, disc.height * 0.30f);
                    int phi = Mathf.Max(16, Mathf.RoundToInt(34f * s));
                    _buy.fontSize = FitFont(_buy, price, priceR.width * 0.96f, priceR.height * 0.92f, 12, phi);
                    int pink = Mathf.Clamp(Mathf.RoundToInt(_buy.fontSize * 0.16f), 2, 8);
                    StampOutlined(priceR, price, _buy, cream, 0, pink);
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
                VipWelcomeHoldsRail = true;
                _welAt = Time.unscaledTime;
                _welExitAt = -1f;
                _welFanfare = false;
                _reduce = PlayerPrefs.GetInt("flockfive.reduceMotion", 0) == 1;
            }

            // Owned diamond badge pops in centre stage with a burst and twinkles, the caption
            // settles under it in the standard caption box, then the badge glides into
            // the rail slot it becomes. A tap after the pop skips to that glide.
            static void DrawWelcome(float s)
            {
                float now = Time.unscaledTime;
                float age = now - _welAt;
                if (_welExitAt < 0f && age >= WelHold) _welExitAt = now;
                float exitU = _welExitAt >= 0f ? PopupMotion.Beat(now - _welExitAt, 0f, _reduce ? 0.2f : WelExit) : 0f;
                if (exitU >= 1f)
                {
                    // Tick settles this before the rail draw. If Draw runs first, end the same way.
                    _welcome = false;
                    VipWelcomeHoldsRail = false;
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
