using System;

namespace FlockFive
{
    // Home covers that used to hide the settings gear. None of them do.
    // The garden never draws the gear. A forced glove step keeps it visible
    // and drops the tap so Settings cannot open over that step.
    [Flags]
    public enum HomeCover
    {
        None = 0,
        HiveIntro = 1 << 0,
        PokerIntro = 1 << 1,
        DailyIntro = 1 << 2,
        DailyClaim = 1 << 3,
        Welcome = 1 << 4,
        DailyAsk = 1 << 5,
        Adopt = 1 << 6,
        AvatarRename = 1 << 7,
        Streak = 1 << 8,
        KeepMultiplier = 1 << 9,
        Vip = 1 << 10,
        DailyCard = 1 << 11,
        Settings = 1 << 12
    }

    public struct HomeGearQuery
    {
        public bool Home;
        public HomeCover Cover;
        // Required glove step, or the Daily Claim step. Not an any-tap lesson.
        public bool ForcedTap;
    }

    public static class HomeGear
    {
        public static readonly HomeCover[] Lessons =
        {
            HomeCover.HiveIntro,
            HomeCover.PokerIntro,
            HomeCover.DailyIntro,
            HomeCover.DailyClaim,
            HomeCover.Welcome,
            HomeCover.DailyAsk,
            HomeCover.Adopt,
            HomeCover.AvatarRename,
            HomeCover.Streak,
            HomeCover.KeepMultiplier
        };

        // Always on the home face, including every lesson and both popups.
        public static bool Shown(HomeGearQuery q) => q.Home;

        // Painted after those dimmers. A cover bit cannot push it under them.
        public static bool AboveDimmer(HomeGearQuery q) => q.Home;

        // Forced-tap steps ignore the gear. Every other home state, including
        // an open popup, still accepts the tap.
        public static bool AcceptsTap(HomeGearQuery q) => q.Home && !q.ForcedTap;
    }
}
