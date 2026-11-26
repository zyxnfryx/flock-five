using System.Collections.Generic;
using UnityEngine;

namespace FlockFive
{
    // Stage-clear REWARD buttons (green Watch Ad 2X, brown Collect).
    // Once those buttons are on screen, a leftover from the garden that just
    // cleared must not eat the first tap. An ostrich clear and a plain clear
    // share this rule. A resume swallow and the startup cover still hold:
    // those are a finger the app did not ask for, not a finished fight.
    public static class RewardChoiceInput
    {
        public enum Tap { None, Watch, Collect }

        // Garden state that can still be set when the home card appears.
        public struct Leftovers
        {
            public bool FightHold;
            public bool PestHold;
            public bool RuleEat;
            public bool LessonGuard;
        }

        public struct Press
        {
            public Tap Action;
            public bool PrimaryHeld;
            public bool SecondaryHeld;
        }

        // Buttons are up, and this is not a resume or cover hold.
        public static bool Live(bool cardUp, bool resumeHold, bool startupHold)
        {
            return cardUp && !resumeHold && !startupHold;
        }

        // The card is the modal. Fight, pest, rule-eat and the dismiss guard drop.
        // They stay while the buttons are not up yet (the intro is still playing).
        public static Leftovers Drop(bool cardUp, Leftovers held)
        {
            if (!cardUp) return held;
            return default;
        }

        // One press for the two buttons. Watch is the wide primary when an ad is
        // offered; otherwise that same rect is Collect. A blocked control that
        // overlaps the button (the LEVEL flower under the card) does not own the
        // finger: this gesture is the one that claims it.
        public static Press Resolve(
            bool cardUp,
            bool hasAd,
            bool resumeHold,
            bool startupHold,
            Rect primary,
            Rect secondary,
            SplashPress.Gesture watch,
            SplashPress.Gesture collect,
            IList<SplashPress.Sample> samples,
            bool canBegin,
            bool pointerLive,
            int frame)
        {
            var press = new Press();
            if (watch == null || collect == null) return press;
            if (!Live(cardUp, resumeHold, startupHold))
            {
                Block(watch, frame);
                Block(collect, frame);
                return press;
            }
            var watchHit = new SplashPress.Hit { Rect = hasAd ? primary : default };
            var collectHit = new SplashPress.Hit { Rect = hasAd ? secondary : primary };
            var w = watch.Apply(watchHit, !hasAd, samples, canBegin, pointerLive, frame);
            var c = collect.Apply(collectHit, false, samples, canBegin, pointerLive, frame);
            press.PrimaryHeld = hasAd ? w.Pressed : c.Pressed;
            press.SecondaryHeld = hasAd && c.Pressed;
            if (hasAd && w.Launch) press.Action = Tap.Watch;
            else if (c.Launch) press.Action = Tap.Collect;
            return press;
        }

        static void Block(SplashPress.Gesture g, int frame)
        {
            g.Apply(new SplashPress.Hit(), true, null, false, false, frame);
        }
    }
}
