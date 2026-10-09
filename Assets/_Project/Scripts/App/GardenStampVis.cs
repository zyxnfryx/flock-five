using System;

namespace FlockFive
{
    // Lessons that can be up while the garden multiplier chip is on screen.
    // None of these hide the chip. Splash, an ad, and a full-screen popup do.
    [Flags]
    public enum GardenLesson
    {
        None = 0,
        Sparrow = 1 << 0,
        Hawk = 1 << 1,
        Leaf = 1 << 2,
        Bee = 1 << 3,
        BadgerGuide = 1 << 4,
        BadgerLesson = 1 << 5,
        Coach = 1 << 6,
        CoachDim = 1 << 7,
        HiveGarden = 1 << 8,
        HiveHome = 1 << 9,
        HiveAlbum = 1 << 10,
        PokerHome = 1 << 11,
        PokerPage = 1 << 12,
        PokerDeal = 1 << 13,
        PokerBack = 1 << 14,
        Daily = 1 << 15,
        AdHand = 1 << 16,
        Welcome = 1 << 17,
        Adopt = 1 << 18,
        GiftCue = 1 << 19,
        Album = 1 << 20,
        Upgrade = 1 << 21,
        TutorPause = 1 << 22,
        CoachCaption = 1 << 23
    }

    // Inputs for the garden xN chip. Pure (no Unity calls) so an editor test
    // can turn each lesson on. FlockFiveApp fills this from the live HUD.
    public struct GardenStampQuery
    {
        // Home, hive, poker, and the badger page. No garden chip.
        public bool Splash;
        // Streak is at stake and the garden clock is armed.
        public bool ChipArmed;
        // A rewarded or interstitial ad covers the glass.
        public bool AdCover;
        // Gift card, restart ask, clear-reward board, or bee-card inspect.
        public bool FullPopup;
        // Finale logo smash. A world logo, not a HUD hide: the chip stays.
        public bool FinaleSmash;
        public GardenLesson Lesson;
    }

    public static class GardenStampVis
    {
        // True when the garden multiplier chip should paint at full alpha.
        // A lesson never hides it, including the sparrow freeze and the coach
        // dim (the chip is drawn after that veil). Splash, an ad, and a popup
        // that covers the whole screen do. The finale smash does not.
        public static bool Shown(GardenStampQuery q)
        {
            if (q.Splash || !q.ChipArmed || q.AdCover || q.FullPopup) return false;
            // Read so a lesson bit or the smash cannot be "optimized" into a hide.
            if (q.FinaleSmash && q.Lesson == GardenLesson.None) return true;
            return true;
        }
    }
}
