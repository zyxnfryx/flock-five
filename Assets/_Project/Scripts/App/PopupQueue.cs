namespace FlockFive
{
    // Build 89. The one popup queue. Popups never stack: a card waits until the screen
    // is free (no modal, no coins in the air, no ad on screen), then opens alone.
    // Post-stage order: rewards popup (Double or Collect, coin fly-in finished), then the
    // interstitial only if they took Collect and the cadence is due, then Go VIP if due.
    // A watched Double is the stage's ad, so no interstitial follows it.
    public static class PopupQueue
    {
        public enum Step { Reward = 1, Interstitial = 2, Vip = 3 }

        public static Step[] PostStagePlan(bool rewardPopup, bool tookDouble, bool interstitialDue, bool vipDue)
        {
            var list = new System.Collections.Generic.List<Step>(3);
            if (rewardPopup) list.Add(Step.Reward);
            bool ad = interstitialDue && !tookDouble;
            if (ad) list.Add(Step.Interstitial);
            // The soft Go VIP ask rides on a shown interstitial close.
            if (vipDue && ad) list.Add(Step.Vip);
            return list.ToArray();
        }

        // Nothing owns the screen. Every queued card checks this before it opens.
        public static bool ScreenFree(bool modalOpen, bool rewardPending, bool coinsBusy, bool adShowing)
        {
            return !modalOpen && !rewardPending && !coinsBusy && !adShowing;
        }

        static bool _vipWanted;
        public static bool VipWanted => _vipWanted;

        public static void RequestVip() => _vipWanted = true;
        public static void DropVip() => _vipWanted = false;

        // True once, on the first free frame after a request.
        public static bool TryReleaseVip(bool screenFree)
        {
            if (!_vipWanted || !screenFree) return false;
            _vipWanted = false;
            return true;
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() => _vipWanted = false;
    }
}
