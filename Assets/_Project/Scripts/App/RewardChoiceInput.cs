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

        // One line on the reward card. Not a dialog, and not a tap-to-continue.
        public const string AdUnavailableLine = "Ad not available";

        // How long a watch may sit with no ad on screen before it counts as a miss.
        public const float WatchIdleSec = 1.5f;
        public const float WatchMinSec = 2f;

        public struct WatchEnd
        {
            public bool Waiting;
            public bool PayDouble;
            public bool SkipInterstitial;
            public bool Stay;
            public bool Unavailable;
        }

        // The show finished. Earned pays 2x and may skip this clear's interstitial.
        // Anything else stays on the card. It does not collect the 1x and does not
        // count as a watched 2x.
        public static WatchEnd FinishWatch(bool earned)
        {
            if (earned)
                return new WatchEnd { PayDouble = true, SkipInterstitial = true };
            return new WatchEnd { Stay = true, Unavailable = true };
        }

        // Backup for a show that never calls back. Still showing, or still inside
        // the wait: keep holding. After that, the same earned / not-earned rule.
        public static WatchEnd Watchdog(bool adShowing, float idleSec, float sinceStart, bool earned)
        {
            if (adShowing || idleSec < WatchIdleSec || sinceStart < WatchMinSec)
                return new WatchEnd { Waiting = true };
            return FinishWatch(earned);
        }

        // Bottom band inside the card face, clear of the buttons that sit under it.
        public static Rect UnavailableLine(Rect face)
        {
            if (face.width < 8f || face.height < 8f) return default;
            float h = Mathf.Clamp(face.height * 0.16f, 18f, 34f);
            float insetX = face.width * 0.08f;
            float insetB = Mathf.Max(6f, face.height * 0.045f);
            return new Rect(face.x + insetX, face.yMax - insetB - h, Mathf.Max(8f, face.width - insetX * 2f), h);
        }
    }
}
