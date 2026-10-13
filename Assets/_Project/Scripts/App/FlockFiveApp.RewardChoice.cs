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
            float s = screenH > 2f ? screenH / 720f : 1f;
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
            DrawRewardChoice(Mathf.Max(Screen.height / 720f, 1f));
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

            if (GuiPaint()) PaintChoiceScrim(a);
            bool priHeld, secHeld = false;
            bool priHit = HitPad(lay.Primary, out priHeld);
            bool secHit = hasAd && HitPad(lay.Secondary, out secHeld);
            var keep = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, a);
            var face = DrawPopupButton(lay.Primary, priHeld, PopupTint.Green);
            if (hasAd)
            {
                float ic = face.height * 0.62f;
                DrawPlayAdIcon(new Rect(face.x + face.height * 0.22f, face.center.y - ic * 0.5f, ic, ic));
                DrawPopupWord(face, RewardDoubleWord, _choiceWord, s);
                var sf = DrawPopupButton(lay.Secondary, secHeld, PopupTint.Wood);
                DrawPopupWord(sf, RewardCollectWord, _choiceWord, s);
            }
            else DrawPopupWord(face, RewardCollectWord, _choiceWord, s);
            GUI.color = keep;
            if (a < 0.5f) return;
            if (hasAd && priHit) StartCoroutine(WatchRewardRepeat());
            else if ((!hasAd && priHit) || secHit) _rewardChoice = 2;
        }

        IEnumerator WatchRewardRepeat()
        {
            _rewardChoice = 1;
            _rewardAdT0 = AdPause.Unscaled;
            int amt = _rewardRepeatAmt;
            yield return Ads.Rewarded(Ads.PlacementRewardRepeat, amt, false);
            if (RewardedGrant.Earned) RewardedGrant.Flush();
            _rewardChoice = 2;
        }
    }
}
