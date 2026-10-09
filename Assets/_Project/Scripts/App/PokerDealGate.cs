namespace FlockFive
{
    // What one tap on the poker DEAL control does. Every gate the button checks
    // lives here, so a settled hand cannot leave DEAL dead while bet +/- still work.
    //
    // Root cause (TF 68, a one-wild no-pay hand): the tap handler treated "an
    // interstitial is due" as its own branch and did nothing when the ad was not
    // ready. Ready required the splash shell to be down. The table is a face of
    // that shell, so once ten hands had been counted the ad could never start, the
    // counter never reset, and every later DEAL tap was discarded. A single wild
    // that misses a paying rank is an ordinary loss (pair or nothing, pay 0). It
    // does not own a win fanfare or a punch stamp. It was simply the hand that
    // crossed the cadence. Bet +/- never read this branch.
    public static class PokerDealGate
    {
        public const string BrokeHint = "Not enough coins";

        public enum Act { None, Ad, Draw, Deal, Broke }

        // One check for the floral control. DRAW only while a hand is dealt.
        // DEAL only when CanDeal is true. A Drawn result stays hidden: CanDeal
        // is false until the hand is back to Idle, so the label cannot say DEAL early.
        public static string ActLabel(BirdPoker.Phase phase, bool canDeal)
        {
            if (phase == BirdPoker.Phase.Dealt) return "DRAW";
            if (phase == BirdPoker.Phase.Drawn || !canDeal) return null;
            return "DEAL";
        }

        public static bool ActShown(BirdPoker.Phase phase, bool canDeal) =>
            ActLabel(phase, canDeal) != null;

        public struct Snap
        {
            public BirdPoker.Phase Phase;
            // Draw / deal motion still inside its duration.
            public bool MotionBusy;
            // Punch-card ceremony still up.
            public bool Stamp;
            // Pay table open or still covering the controls.
            public bool PayBlocked;
            // WIN word still in its punch / hold / whoosh.
            public bool WinFlying;
            // PokerHandAd coroutine still marked running.
            public bool AdRunning;
            // An interstitial is actually on screen. A running flag with no show is stale.
            public bool AdShowing;
            public bool AdDue;
            // PokerDealGate.AdCanShow. False must not eat the tap.
            public bool AdReady;
            public int Coins;
            // This hand paid, so a win fanfare or stamp may still be honest.
            // A no-pay hand (one wild, no winner) never owns those.
            public bool Ceremony;
        }

        public struct TapOut
        {
            public Act Act;
            public bool DropAdLock;
            public bool DropWin;
            public bool DropStamp;
        }

        // Coins under the $5 floor: the control cannot start a hand. Say so.
        // A dealt hand already paid for its draw, so the hint stays down until that draw lands.
        public static bool ShowBrokeHint(int coins, BirdPoker.Phase phase) =>
            coins < BirdPoker.FloorBet && phase != BirdPoker.Phase.Dealt;

        // True when the every-10-hands interstitial may take the table.
        // The splash shell is not an input. The poker page only exists while that
        // shell is up; folding it in made this permanently false.
        public static bool AdCanShow(
            bool onPoker,
            bool pageTutor,
            bool adsBusy,
            bool tutorialBlocking,
            bool welcomeOpen,
            bool dealt,
            bool motionBusy,
            bool stamp,
            bool payBlocking,
            bool winUnresolved)
        {
            if (!onPoker || pageTutor) return false;
            if (adsBusy || tutorialBlocking || welcomeOpen) return false;
            if (dealt || motionBusy || stamp || payBlocking || winUnresolved) return false;
            return true;
        }

        // Deal tap. A resolved hand (no longer the live deal, ceremony over or never
        // owed) drops a leftover ad / win / stamp lock, then acts. A lock that is
        // still honestly playing returns None and is left alone.
        public static TapOut Tap(Snap s)
        {
            var o = new TapOut();
            if (s.Phase != BirdPoker.Phase.Dealt && !s.Ceremony)
            {
                if (s.WinFlying)
                {
                    s.WinFlying = false;
                    o.DropWin = true;
                }
                if (s.Stamp)
                {
                    s.Stamp = false;
                    o.DropStamp = true;
                }
            }
            bool presenting = s.MotionBusy || s.Stamp || s.PayBlocked || s.WinFlying;
            if (!presenting && s.Phase != BirdPoker.Phase.Dealt && s.AdRunning && !s.AdShowing)
            {
                s.AdRunning = false;
                o.DropAdLock = true;
            }
            o.Act = Decide(s);
            return o;
        }

        static Act Decide(Snap s)
        {
            if (s.MotionBusy || s.Stamp || s.PayBlocked || s.WinFlying || s.AdRunning)
                return Act.None;
            if (s.Phase == BirdPoker.Phase.Dealt)
                return Act.Draw;
            // Due and actually showable: one ad, then the next tap deals.
            // Due but not showable used to swallow the tap and never reset the counter.
            if (s.AdDue && s.AdReady)
                return Act.Ad;
            if (s.Coins < BirdPoker.FloorBet)
                return Act.Broke;
            return Act.Deal;
        }
    }
}
