using System.Collections;
using UnityEngine;

namespace FlockFive
{
    public sealed partial class FlockFiveApp
    {
        // Stage-clear finale offer and the soft Go VIP ask. Both captions go
        // through PaintCoachCaption. The x2 chip is RewardStampRect (StreakBadge).
        public const string ClearDoubleLine = "Double your coins.";
        public const string ClearDoubleWatch = "Watch Ad: 2x coins";
        public const string ClearDoubleSkip = "Not now";
        public const string VipSoftLine = "Skip ads between stages.";
        public const string VipSoftGo = "Go VIP";
        public const string VipSoftSkip = "Not now";

        public struct ClearOfferLayout
        {
            public Rect Caption;
            public Rect Watch;
            public Rect Skip;
            public Rect Badge;
        }

        public struct VipSoftLayout
        {
            public Rect Caption;
            public Rect Go;
            public Rect Skip;
        }

        bool _clearOffer;
        int _clearOfferChoice;
        float _clearBadgeAt = -1f;
#if UNITY_EDITOR
        bool _shotClearDouble;
#endif
        static GUIStyle _offerWord;

        public static ClearOfferLayout LayoutClearOffer(float screenW, float screenH)
        {
            float s = screenH > 2f ? screenH / 720f : 1f;
            if (s < 1f) s = 1f;
            float safe = CaptionLayout.SafePlateWidth(screenW, screenH);
            float w = safe;
            if (w > screenW * 0.86f) w = screenW * 0.86f;
            float capH = Mathf.Max(128f * s, 108f);
            float x = (screenW - w) * 0.5f;
            float y = screenH * 0.36f;
            var caption = new Rect(x, y, w, capH);
            float bw = Mathf.Min(w, screenW * 0.78f);
            float bh = Mathf.Max(78f * s, 68f);
            var watch = new Rect((screenW - bw) * 0.5f, caption.yMax + 22f * s, bw, bh);
            float sw = bw * 0.62f;
            float sh = Mathf.Max(64f * s, 56f);
            var skip = new Rect((screenW - sw) * 0.5f, watch.yMax + 14f * s, sw, sh);
            var badge = RewardStampRect(watch, 2);
            return new ClearOfferLayout
            {
                Caption = caption,
                Watch = watch,
                Skip = skip,
                Badge = badge
            };
        }

        public static VipSoftLayout LayoutVipSoft(float screenW, float screenH)
        {
            float s = screenH > 2f ? screenH / 720f : 1f;
            if (s < 1f) s = 1f;
            float safe = CaptionLayout.SafePlateWidth(screenW, screenH);
            float w = safe;
            if (w > screenW * 0.86f) w = screenW * 0.86f;
            float capH = Mathf.Max(128f * s, 108f);
            float x = (screenW - w) * 0.5f;
            float y = screenH * 0.38f;
            var caption = new Rect(x, y, w, capH);
            float gap = 14f * s;
            float bh = Mathf.Max(72f * s, 64f);
            float bw = (w - gap) * 0.5f;
            float by = caption.yMax + 22f * s;
            return new VipSoftLayout
            {
                Caption = caption,
                Go = new Rect(x, by, bw, bh),
                Skip = new Rect(x + bw + gap, by, bw, bh)
            };
        }

        // Gold-standard plate at full alpha. The lesson fade clock is put back.
        void PaintOfferCaption(string line, Rect seat, float s)
        {
            float keepFade = _coachFade;
            bool plateOn = _tutorPlateOn;
            var plate = _tutorPlate;
            int frame = _tutorPlateFrame;
            _coachFade = 1f;
            StandardCaptionBox(s, out _, out _, out int lo, out int hi);
            PaintCoachCaption(line, seat, s, lo, hi, default, false, StandardCaptionSlack);
            _coachFade = keepFade;
            _tutorPlateOn = plateOn;
            _tutorPlate = plate;
            _tutorPlateFrame = frame;
        }

        static void EnsureOfferWord()
        {
            if (_offerWord != null) return;
            _offerWord = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false
            };
        }

        IEnumerator OfferClearDouble()
        {
            if (!Ads.ClearRewardOffered(Ads.Enabled, Ads.RewardedReady)) yield break;
            _clearOfferChoice = 0;
            _clearBadgeAt = Time.unscaledTime;
            _clearOffer = true;
            float guard = 0f;
            while (_clearOfferChoice == 0 && guard < 120f && !_restarting && !_splash)
            {
                guard += Time.unscaledDeltaTime;
                yield return null;
            }
            int choice = _clearOfferChoice;
            _clearOffer = false;
            _clearOfferChoice = 0;
            if (choice != 1 || _restarting || _splash) yield break;
            yield return Ads.Rewarded(Ads.PlacementClearDouble, Purse.PeekClearPay(), false);
            if (!RewardedGrant.Earned) yield break;
            Ads.MarkClearRewardWatched();
            RewardedGrant.Flush();
        }

        IEnumerator WaitVipSoft()
        {
            float guard = 0f;
            while (VipOffer.SoftUp && guard < 120f && !_restarting && !_splash)
            {
                guard += Time.unscaledDeltaTime;
                yield return null;
            }
            if (VipOffer.SoftUp) VipOffer.DismissSoft(false);
        }

        void DrawClearDoubleOffer(float s)
        {
#if UNITY_EDITOR
            bool show = _clearOffer || _shotClearDouble;
#else
            bool show = _clearOffer;
#endif
            if (!show) return;
            var lay = LayoutClearOffer(Screen.width, Screen.height);
            ModalBlock.Paint(ModalBlock.Id.ClearDouble, new Color(0.05f, 0.04f, 0.03f, 0.58f));

            bool watchHit = false;
            bool watchHeld = false;
            bool skipHit = false;
            bool skipHeld = false;
            bool outside = false;
            if (_clearOffer)
            {
                watchHit = HitPad(lay.Watch, out watchHeld);
                skipHit = HitPad(lay.Skip, out skipHeld);
                outside = HitPad(new Rect(0f, 0f, Screen.width, Screen.height), out _);
            }
            PaintOfferCaption(ClearDoubleLine, lay.Caption, s);
            EnsureOfferWord();
            DrawPopupButton(lay.Watch, watchHeld, PopupTint.Green);
            var word = lay.Watch;
            float wordMax = lay.Badge.xMin - 8f;
            if (wordMax > word.x + 40f) word.width = wordMax - word.x;
            DrawPopupWord(word, ClearDoubleWatch, _offerWord, s);
            if (_clearBadgeAt < 0f) _clearBadgeAt = Time.unscaledTime - 0.8f;
            DrawRewardStamp(lay.Badge, 2, 1f, Time.unscaledTime - _clearBadgeAt, 0.48f, false);
            DrawPopupButton(lay.Skip, skipHeld, PopupTint.Wood);
            DrawPopupWord(lay.Skip, ClearDoubleSkip, _offerWord, s);
            if (!_clearOffer) return;
            if (watchHit) _clearOfferChoice = 1;
            else if (skipHit || outside) _clearOfferChoice = 2;
        }

        bool ClearAdOfferUp()
        {
#if UNITY_EDITOR
            if (_shotClearDouble || VipOffer.ShotSoft) return true;
#endif
            return _clearOffer || VipOffer.SoftUp;
        }

#if UNITY_EDITOR
        IEnumerator ShotBuild82d()
        {
            EditorShotLive = true;
            UnityEditor.EditorApplication.isPaused = false;
            Application.runInBackground = true;
            const string dir = "/Users/zfxgames/wkspaces/birdshot/gb/b82out";
            System.IO.Directory.CreateDirectory(dir);
            Screen.SetResolution(1179, 2556, false);
            GamePause.Reset();
            AdConsent.DismissTransient();
            AdConsent.EditorHoldAtt = false;
            _coach = false;
            _cueHand = false;
            _cueLine = null;
            _gloveVis = false;
            _leafIntro = false;
            _hiveLevelLive = false;
            _coachFade = 0f;
            Load(0);
            _coach = false;
            _cueHand = false;
            _cueLine = null;
            _gloveVis = false;
            _leafIntro = false;
            _hiveLevelLive = false;
            _hiveLevelCue = false;
            _coachFade = 0f;
            _splash = false;
            float gardenWait = 0f;
            while (gardenWait < 4f && _board == null)
            {
                gardenWait += Time.unscaledDeltaTime;
                yield return null;
            }
            _shotClearDouble = true;
            VipOffer.ShotSoft = false;
            _clearBadgeAt = Time.unscaledTime - 0.9f;
            Ads.SetReadyOverride(1);
            yield return new WaitForSecondsRealtime(1.3f);
            yield return new WaitForEndOfFrame();
            SaveFrame(dir + "/b82-stage-clear-x2.png");

            _shotClearDouble = false;
            _clearOffer = false;
            VipOffer.ShotSoft = true;
            yield return new WaitForSecondsRealtime(0.45f);
            yield return new WaitForEndOfFrame();
            SaveFrame(dir + "/b82-vip-soft-offer.png");

            VipOffer.ShotSoft = false;
            _splash = true;
            _home = HomeFace.Splash;
            ShotDailyWheel = 4;
            Ads.SetReadyOverride(1);
            yield return new WaitForSecondsRealtime(0.7f);
            yield return new WaitForEndOfFrame();
            SaveFrame(dir + "/b82-wheel-x2-fallback.png");

            ShotDailyWheel = 0;
            Ads.SetReadyOverride(-1);
            DailyBonus.ClearWheelShot();
            _dailyOpen = false;
            _shotClearDouble = false;
            VipOffer.ShotSoft = false;
            EditorShotLive = false;
            Debug.Log("B82D_STILLS_DONE");
            var exit = System.Type.GetType("UnityEditor.EditorApplication, UnityEditor");
            var method = exit != null ? exit.GetMethod("Exit", new[] { typeof(int) }) : null;
            if (method != null) method.Invoke(null, new object[] { 0 });
        }
#endif
    }
}
