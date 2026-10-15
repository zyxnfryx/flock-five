using System.Collections;
using UnityEngine;

namespace FlockFive
{
    public sealed partial class FlockFiveApp
    {
        // Set when a stage clears. The interstitial and Go VIP now run on home, after
        // the rewards popup is fully closed and its coins have landed (PopupQueue).
        bool _postStagePending;
        bool _postStageRunning;
        // The player picked Double on this stage's rewards popup: that ad replaces the interstitial.
        bool _rewardTookDouble;
        ModalBlock.Id _lastModal = ModalBlock.Id.None;

        // The rewards popup is owed or deferred but not up yet.
        bool RewardPopupPending()
        {
            return _streakHomeDue || _clearReward || (_popupDefer & (int)PopupKind.Streak) != 0;
        }

        bool PopupScreenFree()
        {
            return PopupQueue.ScreenFree(OpenModal() != ModalBlock.Id.None || _streakSlide >= 0f,
                RewardPopupPending(), RewardPayBusy(), Ads.IsShowing);
        }

        // Home, nothing open, no lesson holding the screen.
        bool VipMayOpen()
        {
            return PopupScreenFree() && !IsTutorialBlocking;
        }

        void TickPopupQueue()
        {
            var now = OpenModal();
            if (_lastModal != ModalBlock.Id.None && now == ModalBlock.Id.None) ClearTransientFx();
            _lastModal = now;
            if (!_postStagePending || _postStageRunning) return;
            if (_restarting) { _postStagePending = false; _rewardTookDouble = false; return; }
            if (!_splash || _home != HomeFace.Splash || _settingsOpen) return;
            if (!PopupScreenFree()) return;
            StartCoroutine(RunPostStage());
        }

        IEnumerator RunPostStage()
        {
            _postStageRunning = true;
            _postStagePending = false;
            // A short breath after the coins land.
            yield return new WaitForSecondsRealtime(0.35f);
            if (_rewardTookDouble) Ads.MarkClearRewardWatched();
            _rewardTookDouble = false;
            yield return Ads.Interstitial();
            if (Ads.TryConsumeVipSoft()) PopupQueue.RequestVip();
            _postStageRunning = false;
        }

        // Shared reset for leftover one-shot effects (reward sparks, burst bits). Runs on
        // every return to home and every popup close, so nothing drifts onto the next face.
        void ClearTransientFx()
        {
            SparkleFx.ClearBits();
        }
    }
}
