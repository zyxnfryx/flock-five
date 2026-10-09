using System;

namespace FlockFive
{
    // How a lesson step treats a tap. One table, one decision (CoachTap.Advances).
    // Informational: the glove only points to teach. Any tap advances, and that tap
    // is consumed so the control under the finger does not also fire.
    // Required: the glove is on something the player must use now (a guided comb,
    // a branch, Deal, Back). Only that target advances. Existing target gates stay.
    // ClaimOnly: the Daily bonus card. Only Claim advances. Not an any-tap step.
    public enum CoachTapKind
    {
        Informational = 0,
        Required = 1,
        ClaimOnly = 2,
    }

    // Every coached step the glove or a caption can hold. The app asks CoachTap
    // which kind is live. It does not decide any-tap versus target at the call site.
    public enum CoachStep
    {
        BadgerPick = 0,
        BadgerPowers = 1,
        BadgerColumn = 2,
        BadgerComb1 = 3,
        BadgerComb2 = 4,
        BadgerComb3 = 5,
        CoachPickup = 6,
        CoachPlace = 7,
        CoachSecondHop = 8,
        CoachNowhere = 9,
        GiftStuck = 10,
        Sparrow = 11,
        Hawk = 12,
        Bee = 13,
        Leaf = 14,
        HiveGarden = 15,
        HiveHome = 16,
        PokerIntro = 17,
        PokerBet = 18,
        PokerHold = 19,
        PokerDeal = 20,
        PokerBack = 21,
        PokerKeep = 22,
        DailyRail = 23,
        DailyClaim = 24,
        AlbumTap = 25,
        AlbumEmpty = 26,
        AlbumFlip = 27,
        AlbumHoney = 28,
        AlbumPage = 29,
        AlbumUpgrade = 30,
        AdHand = 31,
        AdoptGreet = 32,
        AdoptLook = 33,
        VipWelcome = 34,
        VipPitch = 35,
        RestartSure = 36,
        RestartKeep = 37,
    }

    public static class CoachTap
    {
        public static CoachTapKind Kind(CoachStep step)
        {
            switch (step)
            {
                case CoachStep.BadgerPick:
                case CoachStep.BadgerPowers:
                case CoachStep.BadgerColumn:
                case CoachStep.Sparrow:
                case CoachStep.Hawk:
                case CoachStep.Bee:
                case CoachStep.Leaf:
                case CoachStep.AlbumHoney:
                case CoachStep.AlbumUpgrade:
                case CoachStep.AdoptGreet:
                case CoachStep.VipWelcome:
                    return CoachTapKind.Informational;
                case CoachStep.DailyClaim:
                    return CoachTapKind.ClaimOnly;
                case CoachStep.BadgerComb1:
                case CoachStep.BadgerComb2:
                case CoachStep.BadgerComb3:
                case CoachStep.CoachPickup:
                case CoachStep.CoachPlace:
                case CoachStep.CoachSecondHop:
                case CoachStep.CoachNowhere:
                case CoachStep.GiftStuck:
                case CoachStep.HiveGarden:
                case CoachStep.HiveHome:
                case CoachStep.PokerIntro:
                case CoachStep.PokerBet:
                case CoachStep.PokerHold:
                case CoachStep.PokerDeal:
                case CoachStep.PokerBack:
                case CoachStep.PokerKeep:
                case CoachStep.DailyRail:
                case CoachStep.AlbumTap:
                case CoachStep.AlbumEmpty:
                case CoachStep.AlbumFlip:
                case CoachStep.AlbumPage:
                case CoachStep.AdHand:
                case CoachStep.AdoptLook:
                case CoachStep.VipPitch:
                case CoachStep.RestartSure:
                case CoachStep.RestartKeep:
                    return CoachTapKind.Required;
            }
            throw new ArgumentOutOfRangeException(nameof(step), step, "Classify this lesson step");
        }

        // True when a tap anywhere advances. Claim and required controls are not this.
        public static bool AnyTap(CoachStep step) => Kind(step) == CoachTapKind.Informational;

        // Informational taps are eaten. Required and Claim keep their own controls.
        public static bool Consumes(CoachStep step) => Kind(step) == CoachTapKind.Informational;

        // The one decision. onTarget is the glove's control, or Claim on the daily card.
        // Informational ignores onTarget. Required and ClaimOnly advance only on target.
        public static bool Advances(CoachStep step, bool onTarget)
        {
            if (AnyTap(step)) return true;
            return onTarget;
        }

        public static CoachStep BadgerLesson(int index)
        {
            if (index <= 0) return CoachStep.BadgerPick;
            if (index == 1) return CoachStep.BadgerPowers;
            if (index == 2) return CoachStep.BadgerColumn;
            throw new ArgumentOutOfRangeException(nameof(index), index, "badger lesson");
        }

        // move is 1-based, matching BadgerCopy.GuideLine.
        public static CoachStep BadgerGuide(int move)
        {
            if (move == 1) return CoachStep.BadgerComb1;
            if (move == 2) return CoachStep.BadgerComb2;
            if (move == 3) return CoachStep.BadgerComb3;
            throw new ArgumentOutOfRangeException(nameof(move), move, "badger guide");
        }

        public static readonly CoachStep[] All = (CoachStep[])Enum.GetValues(typeof(CoachStep));

        // Short caption for the audit log. Badger lines come from BadgerCopy.
        public static string Caption(CoachStep step)
        {
            switch (step)
            {
                case CoachStep.BadgerPick: return BadgerCopy.LessonAt(0);
                case CoachStep.BadgerPowers: return BadgerCopy.LessonAt(1);
                case CoachStep.BadgerColumn: return BadgerCopy.LessonAt(2);
                case CoachStep.BadgerComb1: return BadgerCopy.GuideLine(1);
                case CoachStep.BadgerComb2: return BadgerCopy.GuideLine(2);
                case CoachStep.BadgerComb3: return BadgerCopy.GuideLine(3);
                case CoachStep.CoachPickup: return "Tap a branch to pick up its top birds.";
                case CoachStep.CoachPlace: return "Now tap a branch with the same color on top, or an empty one.";
                case CoachStep.CoachSecondHop: return "Second guided hop (glove on the next branch).";
                case CoachStep.CoachNowhere: return "Those birds have nowhere to go yet. Try another branch.";
                case CoachStep.GiftStuck: return "Stuck? Tap the gift branch for a bonus spot.";
                case CoachStep.Sparrow: return "A sparrow blocks a feeder! Match five birds there to chase it off.";
                case CoachStep.Hawk: return "A hawk blocks a feeder! Match five there twice to drive it off.";
                case CoachStep.Bee: return "Bees cover birds on a branch! Move the birds off a bee to free it and win a bee card.";
                case CoachStep.Leaf: return "Leaves hide these birds. Collect at a feeder to blow them away.";
                case CoachStep.HiveGarden: return "You found a bee! Click the hive to view them.";
                case CoachStep.HiveHome: return "Tap the hive to see your bee collection.";
                case CoachStep.PokerIntro: return "You earned coins! Tap poker to bet them.";
                case CoachStep.PokerBet: return "Pick your bet, then tap Deal.";
                case CoachStep.PokerHold: return "Tap a card to hold it, then tap Draw.";
                case CoachStep.PokerDeal: return "Pick your bet, then tap Deal. (later visit)";
                case CoachStep.PokerBack: return "Tap the back button to return home.";
                case CoachStep.PokerKeep: return "TAP A CARD TO KEEP";
                case CoachStep.DailyRail: return "Tap Daily for your bonus!";
                case CoachStep.DailyClaim: return "Tap to claim!";
                case CoachStep.AlbumTap: return "Tap a card you have to enlarge it.";
                case CoachStep.AlbumEmpty: return "Tap a card to take a closer look!";
                case CoachStep.AlbumFlip: return "Tap again to flip.";
                case CoachStep.AlbumHoney: return "Honeycombs are honey. 2 normal · 3 foil · 5 inverse.";
                case CoachStep.AlbumPage: return "Swipe to turn the page! Or tap a page number.";
                case CoachStep.AlbumUpgrade: return "Upgrade adds honey for 1000 coins.";
                case CoachStep.AdHand: return "Tap to unlock your bonus!";
                case CoachStep.AdoptGreet: return "Look, a bird followed you home!";
                case CoachStep.AdoptLook: return "Tap your bird for a closer look.";
                case CoachStep.VipWelcome: return "Welcome, VIP!";
                case CoachStep.VipPitch: return "Ready to remove ads and support the game? Tap the VIP button anytime.";
                case CoachStep.RestartSure: return "Are you sure you want to restart?";
                case CoachStep.RestartKeep: return "Keep multiplier (Watch Ad / No thanks).";
            }
            return step.ToString();
        }
    }
}
