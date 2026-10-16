using System;
using UnityEngine;

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
        Ostrich = 38,
        // b123: badger power-up round (after move 3) and the result.
        BadgerPowerIntro = 39,   // round 4 unlock: the x2 button arms it
        BadgerPowerArmed = 40,   // x2 armed: the scripted comb resolves with x2
        BadgerResult = 41,       // result: Continue
    }

    // b123 (Brandon): every step names ONE explicit press-to-continue, distinct from toggle taps.
    // Advance = the taps that move the step. Toggle = taps that run their normal handler (arm / disarm a
    // power) and never advance. Ignore = everything else, eaten by the shared gate.
    [Flags]
    public enum TapZone
    {
        None = 0,
        Blank = 1,          // empty space (not a control)
        Focus = 2,          // the glowed / pointed-at area (grid, power row, YOU meter)
        Target = 4,         // the one control the glove is on (comb, power button, Continue)
        PowerButton = 8,    // a power button
        Back = 16,
        Comb = 32,          // a comb that is not the target
    }

    // Glove = tap this. Arrow = look at this. None = nothing is shown.
    public enum CoachPointer
    {
        None = 0,
        Glove = 1,
        Arrow = 2,
    }

    public struct TapRule
    {
        public TapZone Advance;
        public TapZone Toggle;
        public CoachPointer Pointer;
        // Ignore is the rest: ~(Advance | Toggle).
        public bool Advances(TapZone z) => (Advance & z) != 0;
        public bool Toggles(TapZone z) => (Toggle & z) != 0 && (Advance & z) == 0;
        public bool Ignores(TapZone z) => !Advances(z) && !Toggles(z);
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
                case CoachStep.Ostrich:
                case CoachStep.BadgerPowerIntro:
                case CoachStep.BadgerPowerArmed:
                case CoachStep.BadgerResult:
                    return CoachTapKind.Required;
            }
            throw new ArgumentOutOfRangeException(nameof(step), step, "Classify this lesson step");
        }

        // Required and the daily claim. Informational lessons are not forced.
        public static bool Forced(CoachStep step)
        {
            var kind = Kind(step);
            return kind == CoachTapKind.Required || kind == CoachTapKind.ClaimOnly;
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

        // The shared per-step tap rule. Badger steps are listed explicitly (spec: Advance vs toggle, Pointer type).
        // Every other step derives from its kind: an informational step advances on a tap anywhere (Blank + Focus +
        // Target), a required one only on its target, and the glove points at it.
        public static TapRule Rule(CoachStep step)
        {
            switch (step)
            {
                // L1-L3: look-at steps. Tap blank space or the glowed area to continue; back, combs, other buttons ignored.
                case CoachStep.BadgerPick:
                    return new TapRule { Advance = TapZone.Blank | TapZone.Focus, Pointer = CoachPointer.Arrow };
                case CoachStep.BadgerPowers:
                    return new TapRule { Advance = TapZone.Blank | TapZone.Focus, Toggle = TapZone.PowerButton, Pointer = CoachPointer.Arrow };
                case CoachStep.BadgerColumn:
                    return new TapRule { Advance = TapZone.Blank | TapZone.Focus, Pointer = CoachPointer.Arrow };
                // Moves 1-3: only the glowed comb.
                case CoachStep.BadgerComb1:
                case CoachStep.BadgerComb2:
                case CoachStep.BadgerComb3:
                    return new TapRule { Advance = TapZone.Target, Pointer = CoachPointer.Glove };
                // Round 4: the x2 button arms it; re-tap disarms and the caption stays.
                case CoachStep.BadgerPowerIntro:
                    return new TapRule { Advance = TapZone.Target, Toggle = TapZone.Target, Pointer = CoachPointer.Glove };
                case CoachStep.BadgerPowerArmed:
                    return new TapRule { Advance = TapZone.Target, Toggle = TapZone.PowerButton, Pointer = CoachPointer.Glove };
                case CoachStep.BadgerResult:
                    return new TapRule { Advance = TapZone.Target, Pointer = CoachPointer.Glove };
            }
            switch (Kind(step))
            {
                case CoachTapKind.Informational:
                    return new TapRule { Advance = TapZone.Blank | TapZone.Focus | TapZone.Target, Pointer = CoachPointer.Glove };
                default:
                    return new TapRule { Advance = TapZone.Target, Pointer = CoachPointer.Glove };
            }
        }

        public static CoachPointer Pointer(CoachStep step) => Rule(step).Pointer;

        // A toggle that is also the advance (x2 on the unlock round): it arms, the step moves on arming.
        public static bool TogglesOnly(CoachStep step, TapZone z) => Rule(step).Toggles(z);

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
                case CoachStep.CoachPlace: return CaptionLayout.CoachPlaceLine;
                case CoachStep.CoachSecondHop: return "Second guided hop (glove on the next branch).";
                case CoachStep.CoachNowhere: return CaptionLayout.CoachNowhereLine;
                case CoachStep.GiftStuck: return "Stuck? Tap the gift\nbranch for a bonus.";
                case CoachStep.Sparrow: return CaptionLayout.SparrowLine;
                case CoachStep.Hawk: return CaptionLayout.HawkLine;
                case CoachStep.Ostrich: return CaptionLayout.OstrichLine;
                case CoachStep.Bee: return CaptionLayout.BeePestLine;
                case CoachStep.Leaf: return CaptionLayout.LeafLine;
                case CoachStep.HiveGarden: return CaptionLayout.HiveGardenLine;
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
                case CoachStep.AlbumHoney: return CaptionLayout.AlbumHoneyBlock;
                case CoachStep.AlbumPage: return "Swipe to turn the page! Or tap a page number.";
                case CoachStep.AlbumUpgrade: return CaptionLayout.AlbumUpgradeLine;
                case CoachStep.AdHand: return "Tap to unlock your bonus!";
                case CoachStep.AdoptGreet: return CaptionLayout.AdoptGreetLine;
                case CoachStep.AdoptLook: return "Tap your bird for a closer look.";
                case CoachStep.VipWelcome: return "Welcome, VIP!";
                case CoachStep.VipPitch: return CaptionLayout.VipPitchLine;
                case CoachStep.RestartSure: return "Are you sure you want to restart?";
                case CoachStep.RestartKeep: return "Keep multiplier (Watch Ad / No thanks).";
                case CoachStep.BadgerPowerIntro: return BadgerCopy.PowersOpen;
                case CoachStep.BadgerPowerArmed: return BadgerCopy.X2Armed;
                case CoachStep.BadgerResult: return BadgerCopy.Win;
            }
            return step.ToString();
        }
    }

    // Home buttons a forced glove can point at. None is not a button.
    public enum HomeControl
    {
        None = 0,
        Poker = 1,
        Daily = 2,
        Vip = 3,
        Hive = 4,
        Gear = 5,
    }

    // One decision for a tap while a glove step owns the home screen.
    // AllowPress is the sink. Navigate is the button's action. Advance dismisses the glove.
    public struct GlovePress
    {
        public bool AllowPress;
        public bool Navigate;
        public bool Advance;
    }

    public static class GloveGate
    {
        public static GlovePress Resolve(bool forced, HomeControl target, HomeControl tapped)
        {
            if (!forced || tapped == HomeControl.None)
                return new GlovePress { AllowPress = true, Navigate = true, Advance = false };
            var screen = new Rect(0f, 0f, 200f, 40f);
            var hole = HomeSlot(target);
            var tap = HomeSlot(tapped);
            var hit = ModalBlock.ResolveLesson(true, screen, hole, tap.center, false);
            if (hit.Deliver && hit.Advance)
                return new GlovePress { AllowPress = true, Navigate = true, Advance = true };
            return default;
        }

        // Distinct holes so the shared cut-out can tell the buttons apart.
        static Rect HomeSlot(HomeControl control)
        {
            int i = (int)control;
            if (i <= 0) return default;
            return new Rect(i * 24f, 8f, 16f, 16f);
        }
    }

    // One decision for a coached home button. The app and the editor tap test
    // both use it, so a lesson-side gate cannot eat the glove's own control.
    public static class CoachPress
    {
        public struct Decision
        {
            public bool Open;
            public bool Dismiss;
            public bool Rearm;
            public bool Blocked;
        }

        public static bool OwnsTarget(bool coachUp, HomeControl target, HomeControl tapped, out GlovePress glove)
        {
            glove = GloveGate.Resolve(coachUp, target, tapped);
            return coachUp && glove.Navigate && glove.Advance
                && tapped == target && target != HomeControl.None;
        }

        public static Decision Resolve(bool coachUp, HomeControl target, HomeControl tapped, SplashPress.Gates gates, bool beganInside)
        {
            bool own = OwnsTarget(coachUp, target, tapped, out var glove);
            if (own) gates.LessonOwn = true;
            bool blocked = SplashPress.Blocks(gates);
            bool open = beganInside && !blocked && glove.AllowPress && glove.Navigate;
            bool dismiss = open && own;
            return new Decision
            {
                Open = open,
                Dismiss = dismiss,
                Rearm = coachUp && !dismiss,
                Blocked = blocked,
            };
        }

        // The faded hand returns only for the step that faded, and only when that
        // step is still the lesson. A dismiss during the fade stays down.
        public static int ReturnStep(int fadedStep, int liveStep, bool dismissed)
        {
            if (dismissed || fadedStep < 0 || liveStep != fadedStep) return -1;
            return liveStep;
        }
    }
}
