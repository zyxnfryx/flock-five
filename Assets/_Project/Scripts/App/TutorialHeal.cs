using System;

namespace FlockFive
{
    // Everything a tutorial step can leave behind when the app is backgrounded or closed
    // half-way: a lesson flag with no screen to live on, a pause nobody will release,
    // an ad stage with no coroutine behind it, a tap gate that never runs out.
    // Planner only (no Unity calls), so an editor test can drive it. FlockFiveApp builds
    // the snapshot, asks for a plan, and applies it in HealInterruptedTutorials.
    public struct TutorSnapshot
    {
        public bool Splash;
        // Gift card face is up (GiftFace.Card) / the ad stage face is up (GiftFace.Movie).
        public bool GiftCard;
        public bool GiftMovie;
        // The WatchGift coroutine is alive.
        public bool WatchRunning;
        // The Watch-ad glove lesson flag.
        public bool AdHand;
        // GamePause is paused / an ad is on screen / this class holds its own pause.
        public bool PauseHeld;
        public bool AdShowing;
        public bool TutorPause;
        // A sparrow cue is live in a garden: the only lesson that may hold the pause.
        public bool SparrowCueLive;
        // A home lesson (hive, poker, daily) is flagged live.
        public bool HomeLessonLive;
        // Seconds still to run on the resume swallow, tap swallow and gift lockout.
        public float ResumeGateLeft;
        public float TapGateLeft;
        public float GiftGateLeft;
        // Seconds a step tap gate (FlockFiveApp.StepTapGate) has been holding taps with no
        // glove posed on its target. A healthy gated step poses within a frame or two.
        public float StepGateBlind;
    }

    [Flags]
    public enum TutorFix
    {
        None = 0,
        // Watch-ad lesson flag cleared (its card is gone, or the lesson is on the wrong face).
        DropAdHand = 1,
        // Tutorial-held GamePause released.
        ReleaseTutorPause = 2,
        // Ad stage with no coroutine: gift closed, _busy released.
        ClearWatchStage = 4,
        // A tap gate far longer than any real one: clamped to its normal length.
        ClampGates = 8,
        // Home lessons flagged live off the home screen: dropped.
        DropHomeLessons = 16,
        // GamePause held with no owner left (no ad, no tutorial pause): reset.
        ResetLeakedPause = 32,
        // A step tap gate up with no glove on its target: the step is finished, taps freed.
        FinishGatedStep = 64
    }

    // One pointer event, as the step tap gate sees it.
    public enum GatePointer { Down, Drag, Up }

    public static class TutorialHeal
    {
        // The longest real gate is the 1 s gift lockout. Anything past this is stuck.
        public const float MaxGateSeconds = 2.5f;

        public static TutorFix Plan(TutorSnapshot s)
        {
            var fix = TutorFix.None;
            if (s.AdHand && (s.Splash || !s.GiftCard)) fix |= TutorFix.DropAdHand;
            if (s.TutorPause && (s.Splash || !s.SparrowCueLive)) fix |= TutorFix.ReleaseTutorPause;
            if (s.GiftMovie && !s.WatchRunning) fix |= TutorFix.ClearWatchStage;
            if (s.ResumeGateLeft > MaxGateSeconds || s.TapGateLeft > MaxGateSeconds || s.GiftGateLeft > MaxGateSeconds)
                fix |= TutorFix.ClampGates;
            if (s.HomeLessonLive && !s.Splash) fix |= TutorFix.DropHomeLessons;
            if (s.StepGateBlind > MaxGateSeconds) fix |= TutorFix.FinishGatedStep;
            // After the tutorial pause is accounted for, a pause with no owner is a leak.
            bool tutorHolds = s.TutorPause && (fix & TutorFix.ReleaseTutorPause) == 0;
            if (s.PauseHeld && !s.AdShowing && !tutorHolds) fix |= TutorFix.ResetLeakedPause;
            return fix;
        }

        // The step tap gate's rule, pure so an editor test can drive it. While a step that
        // waits on one control is up, may this pointer event reach the page's controls?
        // The glove's target always may. A press may also land in the step's swipe area so a
        // swipe can start there, and a release outside the target counts only as the end of
        // a real swipe. Drags pass: they only move a control that already took the press.
        // An any-tap step: the first left press anywhere finishes it. Drags and the release only
        // get eaten (GateLets is never asked), so the finishing tap reaches nothing beneath.
        public static bool GateAdvances(GatePointer kind, bool anyTap, int button)
        {
            return anyTap && kind == GatePointer.Down && button == 0;
        }

        public static bool GateLets(GatePointer kind, bool inTarget, bool inSwipeArea, bool swipeTaken)
        {
            if (inTarget) return true;
            switch (kind)
            {
                case GatePointer.Down: return inSwipeArea;
                case GatePointer.Drag: return true;
                default: return swipeTaken;
            }
        }
    }
}
