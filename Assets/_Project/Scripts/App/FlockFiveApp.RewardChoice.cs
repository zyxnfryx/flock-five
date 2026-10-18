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
            float W = Screen.width, H = Screen.height;
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
            var lay = LayoutRewardChoice(_rewardFace, Screen.width, Screen.height, hasAd);
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
            DrawOfferChoice(lay, hasAd, priHeld, secHeld, a, s, RewardCollectWord);
            GUI.color = keep;
            if (a < 0.5f) return;
            if (hasAd && priHit) StartCoroutine(WatchRewardRepeat());
            else if ((!hasAd && priHit) || secHit) _rewardChoice = 2;
        }

        // Shared Watch-Ad-2X / Collect choice painter. Used by the REWARD popup and the DAILY
        // popup so both have the same buttons, icon+label group, pulse, sheen and popup style.
        // Primary = green WATCH AD 2X (or lone green Collect when no ad); Secondary = wood Collect.
        // Caller owns hit testing and alpha (GUI.color); `a` only scales the sheen.
        void DrawOfferChoice(OfferButtonsLayout lay, bool hasAd, bool priHeld, bool secHeld, float a, float s, string collectWord)
        {
            if (_choiceWord == null)
                _choiceWord = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = false };
            // b87: the 2X button reads as "Watch Ad": video icon, "WATCH AD" over a big 2X,
            // a gentle breathe and the shared PushButton sheen at half speed.
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
                // b111 (Brandon): icon + words are one group centered on the button, inside the same
                // inner padding the Collect / Pay Double buttons use (DrawPopupWord).
                float ic = face.height * 0.58f;
                float gap = face.height * 0.08f;
                float padX = face.width * PopupWordPadX;
                float slotW = Mathf.Max(8f, face.width - 2f * padX - ic - gap);
                var slotLab = new Rect(0f, face.y + face.height * PopupWordPadY * 0.5f, slotW, face.height * (1f - PopupWordPadY));
                float textW = MeasureWatchAdWords(slotLab, s);
                float groupW = ic + gap + textW;
                float gx = face.center.x - groupW * 0.5f;
                var icR = new Rect(gx, face.center.y - ic * 0.5f, ic, ic);
                DrawPlayAdIcon(icR);
                var txt = new Rect(icR.xMax + gap, slotLab.y, textW, slotLab.height);
                DrawWatchAdWords(txt, s);
                PushButton.EndPulse(m0);
                var sf = DrawPopupButton(lay.Secondary, secHeld, PopupTint.Wood);
                DrawPopupWord(sf, collectWord, _choiceWord, s);
            }
            else DrawPopupWord(face, collectWord, _choiceWord, s);
        }

        public const string WatchAdWord = "WATCH AD";
        public const float WatchAdPulse = 0.03f;
        public const float WatchAdPulsePeriod = 1.6f;
        GUIStyle _watchSmall;

        // Fits both lines to the slot (same FitFont/outline rule as DrawPopupWord) and returns the widest line,
        // so the icon + words group can be centered on the button.
        float MeasureWatchAdWords(Rect slot, float s)
        {
            FitWatchAdWords(slot, s, out _, out _);
            var c1 = new GUIContent(WatchAdWord);
            var c2 = new GUIContent(RewardDoubleWord);
            float w1 = _watchSmall.CalcSize(c1).x;
            float w2 = _choiceWord.CalcSize(c2).x;
            return Mathf.Min(slot.width, Mathf.Max(w1, w2));
        }

        void FitWatchAdWords(Rect r, float s, out Rect top, out Rect big)
        {
            if (_watchSmall == null)
                _watchSmall = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = false };
            top = new Rect(r.x, r.y, r.width, r.height * 0.36f);
            big = new Rect(r.x, r.y + r.height * 0.34f, r.width, r.height * 0.66f);
            _watchSmall.wordWrap = false; _watchSmall.alignment = TextAnchor.MiddleCenter; _watchSmall.fontStyle = FontStyle.Bold;
            _watchSmall.fontSize = FitFont(_watchSmall, WatchAdWord, top.width, top.height, 9, Mathf.Max(12, Mathf.RoundToInt(18f * s)));
            _choiceWord.wordWrap = false; _choiceWord.alignment = TextAnchor.MiddleCenter; _choiceWord.fontStyle = FontStyle.Bold;
            _choiceWord.fontSize = FitFont(_choiceWord, RewardDoubleWord, big.width, big.height, 14, Mathf.Max(18, Mathf.RoundToInt(34f * s)));
        }

        // White fill, dark outline: same stamp as DrawPopupWord on Collect / Pay Double.
        void DrawWatchAdWords(Rect r, float s)
        {
            FitWatchAdWords(r, s, out var top, out var big);
            StampOutlined(top, WatchAdWord, _watchSmall, Color.white, 0, Mathf.Clamp(Mathf.RoundToInt(_watchSmall.fontSize * 0.16f), 2, 8));
            StampOutlined(big, RewardDoubleWord, _choiceWord, Color.white, 0, Mathf.Clamp(Mathf.RoundToInt(_choiceWord.fontSize * 0.16f), 2, 8));
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
