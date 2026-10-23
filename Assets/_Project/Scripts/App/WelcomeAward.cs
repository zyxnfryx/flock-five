namespace FlockFive
{
    // Welcome bonus ($1,000 card, coin blips, coin fly). It presents once, and only
    // when someone is looking: app focused, not paused, home actually on screen,
    // studio card gone. Anything else stays pending. No coin audio while it waits.
    public static class WelcomeAward
    {
        public enum Act { None, Defer, Grant }

        public static bool MayPresent(bool focused, bool appPaused, bool homeVisible, bool studioCardUp)
            => focused && !appPaused && homeVisible && !studioCardUp;

        // Freeze the coin fly (and therefore its landing blips) while nobody is watching.
        public static bool HoldCoinPlay(bool allowsCoinAudio, bool studioCardUp)
            => !allowsCoinAudio || studioCardUp;

        public static bool PlaysCoins(Act act) => act == Act.Grant;

        public static Act Decide(
            bool alreadyGranted, bool openOrQueued, bool editorShot,
            bool popupAllowed, bool lessonHolds,
            bool focused, bool appPaused, bool homeVisible, bool studioCardUp)
        {
            if (alreadyGranted || openOrQueued || editorShot) return Act.None;
            if (!popupAllowed) return Act.None;
            if (lessonHolds || !MayPresent(focused, appPaused, homeVisible, studioCardUp))
                return Act.Defer;
            return Act.Grant;
        }
    }
}
