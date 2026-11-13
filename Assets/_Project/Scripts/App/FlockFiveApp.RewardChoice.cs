using System.Collections;
using UnityEngine;

namespace FlockFive
{
    public sealed partial class FlockFiveApp
    {
        // Build 83 items 10d-10i. The home REWARD popup plays, then holds until a tap:
        // big green 2X (rewarded ad, the same prize again) over a smaller Collect.
        // No ad available (not loaded, cooldown, grace): one green Collect only.
        public const string RewardDoubleWord = "2X";
        public const string RewardCollectWord = "Collect";
        // Stage-clear 2X button. Daily keeps the shorter RewardDoubleWord.
        public const string RewardCoinsDoubleWord = "2X COINS";

        // 0 waiting, 1 ad running, 2 chosen.
        int _rewardChoice;
        int _rewardRepeatAmt;
        int _rewardHasAd = -1; // b85: decided once, before the buttons show
        bool _rewardAtHold;
        Rect _rewardFace;
        float _rewardChoiceBorn = -1f;
        static GUIStyle _choiceWord;
#if UNITY_EDITOR
        // Captures and tests: -1 live, 0 force no-ad, 1 force ad.
        public static int ShotRewardAd = -1;
        public static bool RewardAutoCollect;
#endif

        public struct OfferButtonsLayout
        {
            public Rect Primary;
            public Rect Secondary;
            public bool HasSecondary;
        }

        // Shared primary/secondary sizing for every offer popup: the primary is the wide
        // button; the secondary is smaller and centered beneath it.
        public static OfferButtonsLayout OfferButtons(float centerX, float top, float width, float s, bool secondary)
        {
            float bh = Mathf.Max(78f * s, 68f);
            var primary = new Rect(centerX - width * 0.5f, top, width, bh);
            var lay = new OfferButtonsLayout { Primary = primary, HasSecondary = secondary };
            if (secondary)
            {
                float sw = width * 0.62f;
                float sh = Mathf.Max(bh * 0.80f, 54f);
                lay.Secondary = new Rect(centerX - sw * 0.5f, primary.yMax + 14f * s, sw, sh);
            }
            return lay;
        }

        public static OfferButtonsLayout LayoutRewardChoice(Rect face, float screenW, float screenH, bool hasAd)
        {
            float s = screenH > 2f ? DeviceFit.UiH(screenH) : 1f;
            if (s < 1f) s = 1f;
            float w = Mathf.Min(face.width * 0.86f, screenW * 0.78f);
            if (w < 160f) w = Mathf.Min(screenW * 0.78f, 160f);
            return OfferButtons(face.center.x, face.yMax + 26f * s, w, s, hasAd);
        }

        bool RewardAdNow()
        {
#if UNITY_EDITOR
            if (ShotRewardAd >= 0) return ShotRewardAd == 1;
#endif
            return Ads.RewardRepeatReady;
        }

        float _rewardAdT0 = -1f;
        float _rewardAdIdle = -1f;

        // RewardAdWatchdog: if the ad never comes back (app paused/killed mid-ad, SDK drops
        // the callback, the coroutine host goes away) the popup must not hang. Once no ad is
        // showing for 1.5s after it started, pay what was earned and treat it as Collect.
        void RewardAdWatchdog()
        {
            if (_rewardChoice != 1) { _rewardAdIdle = -1f; return; }
            float now = AdPause.Unscaled;
            if (Ads.IsShowing) { _rewardAdIdle = -1f; return; }
            if (_rewardAdIdle < 0f) _rewardAdIdle = now;
            if (now - _rewardAdIdle < 1.5f || now - _rewardAdT0 < 2f) return;
            if (RewardedGrant.Earned) RewardedGrant.Flush();
            _rewardChoice = 2;
        }

        bool RewardChoicePending()
        {
            RewardAdWatchdog();
#if UNITY_EDITOR
            if (RewardAutoCollect && _rewardChoice == 0) _rewardChoice = 2;
#endif
            return _rewardChoice != 2;
        }

        public bool RewardChoiceUp => _rewardAtHold && _streakSlide >= 0f && _rewardChoice == 0;

        // Drawn last, over the LEVEL flower and the badger, as a modal: the home dims
        // around the REWARD card and only the choice buttons take taps.
        void DrawRewardChoiceTop()
        {
            if (!_splash || _home != HomeFace.Splash) { _rewardAtHold = false; return; }
            DrawRewardChoice(DeviceFit.Ui());
        }

        void PaintChoiceScrim(float a)
        {
            var c = new Color(0.05f, 0.04f, 0.03f, 0.58f * a);
            var keep = GUI.color;
            GUI.color = c;
            var f = _rewardFace;
            float W = PhoneArea.W, H = PhoneArea.H;
            var tex = Texture2D.whiteTexture;
            GUI.DrawTexture(new Rect(0f, 0f, W, Mathf.Max(0f, f.y)), tex);
            GUI.DrawTexture(new Rect(0f, f.yMax, W, Mathf.Max(0f, H - f.yMax)), tex);
            GUI.DrawTexture(new Rect(0f, f.y, Mathf.Max(0f, f.x), f.height), tex);
            GUI.DrawTexture(new Rect(f.xMax, f.y, Mathf.Max(0f, W - f.xMax), f.height), tex);
            GUI.color = keep;
        }

        void DrawRewardChoice(float s)
        {
            _rewardAtHold = false;
            if (_clearReward || _streakSlide < 0f || _rewardChoice != 0) { _rewardChoiceBorn = -1f; return; }
            if (_rewardFace.width < 4f) return;
            int streak; int fromPay; int toPay;
            RewardPays(out streak, out fromPay, out toPay);
            var beat = RewardBeats(RewardMulOn(streak, fromPay, toPay));
            float t = _streakSlide * (beat.SeqDur > 0.05f ? beat.SeqDur : 0.05f);
            if (t < beat.ChoiceAt - 0.001f || RewardPayBusy()) { _rewardChoiceBorn = -1f; return; }
            _rewardAtHold = true;
            if (_rewardChoiceBorn < 0f) _rewardChoiceBorn = AdPause.Unscaled;
            float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((AdPause.Unscaled - _rewardChoiceBorn) / 0.22f));

            // Decide 2X vs Collect first (the owed pay is the 2X prize), then show, then wait.
            if (_rewardHasAd < 0)
            {
                if (_rewardRepeatAmt <= 0) _rewardRepeatAmt = Purse.OwedPay;
                _rewardHasAd = RewardAdNow() && _rewardRepeatAmt > 0 ? 1 : 0;
            }
            bool hasAd = _rewardHasAd == 1;
            var lay = LayoutRewardChoice(_rewardFace, PhoneArea.W, PhoneArea.H, hasAd);
            if (_choiceWord == null)
                _choiceWord = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = false };

            if (GuiPaint())
            {
                PaintChoiceScrim(a);
                // b86 (Brandon, iPad): the x2 badge sticks out past the REWARD card; the scrim
                // washed its outer half. Repaint the whole badge over the scrim, full color.
                if (_stampFrame == Time.frameCount)
                {
                    _stampRepaint = true;
                    DrawRewardStamp(_stampR, _stampMul, _stampA, _stampAge, _stampSpark, false);
                    _stampRepaint = false;
                }
            }
            bool priHeld, secHeld = false;
            bool priHit = HitPad(lay.Primary, out priHeld);
            bool secHit = hasAd && HitPad(lay.Secondary, out secHeld);
            var keep = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, a);
            DrawOfferChoice(lay, hasAd, priHeld, secHeld, a, s, RewardCollectWord, RewardCoinsDoubleWord);
            GUI.color = keep;
            if (a < 0.5f) return;
            if (hasAd && priHit) StartCoroutine(WatchRewardRepeat());
            else if ((!hasAd && priHit) || secHit) _rewardChoice = 2;
        }

        // Shared 2X / Collect choice painter. The REWARD popup and the DAILY popup both
        // use it, so the film mark, the single line, the pulse and the sheen stay matched.
        // Primary = green film-strip 2X (or lone green Collect when no ad); Secondary = wood Collect.
        // `doubleWord` is the big line. Reward passes "2X COINS"; daily leaves it null and gets "2X".
        // Caller owns hit testing and alpha (GUI.color); `a` only scales the sheen.
        void DrawOfferChoice(OfferButtonsLayout lay, bool hasAd, bool priHeld, bool secHeld, float a, float s, string collectWord, string doubleWord = null)
        {
            if (string.IsNullOrEmpty(doubleWord)) doubleWord = RewardDoubleWord;
            if (_choiceWord == null)
                _choiceWord = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = false };
            // Gentle breathe and the shared PushButton sheen at half speed.
            var priR = lay.Primary;
            float pk = 1f;
            if (hasAd && !priHeld)
            {
                pk = PushButton.PulseK(AdPause.Unscaled, WatchAdPulse, WatchAdPulsePeriod);
                priR = PushButton.Scaled(priR, pk);
            }
            var face = DrawPopupButton(priR, priHeld, PopupTint.Green);
            if (hasAd)
            {
                // b110: gloss follows the rounded plate, not an oval.
                PushButton.DrawRectSheen(LastPlateRect, PushButton.SheenU(AdPause.Unscaled * 0.5f), 0.55f * a, PopupCornerPx);
                // b110: label is laid out and fitted at rest size, then scaled with the plate by the same pulse value.
                var m0 = PushButton.BeginPulse(priR.center, pk);
                face = PushButton.ScaledAbout(face, priR.center, 1f / pk);
                // Film strip + one line, one group, inside the same padding as Collect / Pay Double.
                var full = LayoutFilmOffer(face, 0f);
                float textW = FitDoubleWord(full.Label, s, doubleWord);
                var mark = LayoutFilmOffer(face, textW);
                DrawFilmPlayMark(mark.Icon);
                int ink = Mathf.Clamp(Mathf.RoundToInt(_choiceWord.fontSize * 0.16f), 2, 8);
                StampOutlined(mark.Label, doubleWord, _choiceWord, Color.white, 0, ink);
                PushButton.EndPulse(m0);
                var sf = DrawPopupButton(lay.Secondary, secHeld, PopupTint.Wood);
                DrawPopupWord(sf, collectWord, _choiceWord, s);
            }
            else DrawPopupWord(face, collectWord, _choiceWord, s);
        }

        public const float WatchAdPulse = 0.03f;
        public const float WatchAdPulsePeriod = 1.6f;
        // Film height as a fraction of the button face, and film width as a fraction of the padded row.
        // The width cap is what keeps "2X COINS" readable on the 402 / 412 glass.
        public const float FilmOfferHeightFrac = 0.70f;
        public const float FilmOfferWidthFrac = 0.42f;

        public struct FilmOfferLayout
        {
            public Rect Icon;
            public Rect Label;
        }

        // `textW` <= 0 uses the whole remaining slot (the fit pass). A measured width
        // recenters the film + word as one group. The rect is the button face DrawPopupButton returns.
        public static FilmOfferLayout LayoutFilmOffer(Rect face, float textW)
        {
            float padX = face.width * PopupWordPadX;
            float innerW = Mathf.Max(8f, face.width - 2f * padX);
            float innerH = Mathf.Max(8f, face.height * (1f - 2f * PopupWordPadY));
            float gap = Mathf.Max(4f, face.height * 0.06f);
            float filmH = face.height * FilmOfferHeightFrac;
            float filmW = filmH * FilmPlayMark.Aspect;
            float capW = innerW * FilmOfferWidthFrac;
            if (filmW > capW)
            {
                filmW = capW;
                filmH = filmW / FilmPlayMark.Aspect;
            }
            float labelW = Mathf.Max(8f, innerW - filmW - gap);
            if (textW > 0f && textW < labelW) labelW = textW;
            float groupW = filmW + gap + labelW;
            float gx = face.center.x - groupW * 0.5f;
            var lay = new FilmOfferLayout();
            lay.Icon = new Rect(gx, face.center.y - filmH * 0.5f, filmW, filmH);
            lay.Label = new Rect(gx + filmW + gap, face.center.y - innerH * 0.5f, labelW, innerH);
            return lay;
        }

        // Fits the one line to the slot (same FitFont / outline rule as DrawPopupWord) and returns its width.
        float FitDoubleWord(Rect slot, float s, string word)
        {
            _choiceWord.wordWrap = false;
            _choiceWord.alignment = TextAnchor.MiddleCenter;
            _choiceWord.fontStyle = FontStyle.Bold;
            int cap = Mathf.Max(18, Mathf.RoundToInt(34f * s));
            _choiceWord.fontSize = FitFont(_choiceWord, word, slot.width, slot.height, 14, cap);
            float w = _choiceWord.CalcSize(new GUIContent(word)).x;
            return Mathf.Min(slot.width, Mathf.Max(8f, w));
        }

        static void DrawFilmPlayMark(Rect r)
        {
            if (r.width < 4f || r.height < 4f) return;
            var tex = FilmPlayMark.Texture();
            if (tex == null) return;
            GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit, true);
        }

        IEnumerator WatchRewardRepeat()
        {
            _rewardChoice = 1;
            // Chosen ad: it stands in for this stage's interstitial.
            _rewardTookDouble = true;
            _rewardAdT0 = AdPause.Unscaled;
            int amt = _rewardRepeatAmt;
            yield return Ads.Rewarded(Ads.PlacementRewardRepeat, amt, false);
            if (RewardedGrant.Earned) RewardedGrant.Flush();
            _rewardChoice = 2;
        }
    }
}
