using System;

namespace FlockFive
{
    // Board reset vs a live tutorial. Pure (no Unity calls) so an editor test can
    // drive it. FlockFiveApp samples the live step, ticks this, and applies the verb.
    //
    // The bonus-branch caption is a countdown: full alpha, then a 0.5s fade-out.
    // Reset used to clear the drawn line and zero alpha, but it left the countdown
    // running. When the flock landed, the step wrote that leftover alpha back, so a
    // dim scrap of the plate flashed for a frame or a few. A fade-in or fade-out
    // that was already running did the same. This guard cuts the draw the frame
    // reset starts (no fade-out), holds it for the whole parade, and on landing
    // either resumes the step from a fresh fade-in or lets a step that had not
    // drawn yet fire at its next trigger. A second reset while the first is in
    // flight does not resume in the gap.
    public enum CoachFadePhase
    {
        None = 0,
        In = 1,
        Held = 2,
        Out = 3
    }

    [Flags]
    public enum CoachResetVerb
    {
        None = 0,
        // Caption and glove go to alpha 0 this frame. Do not play a fade-out.
        Cut = 1,
        // Nothing draws. Covers the parade and the landing frame.
        Hold = 2,
        // The step that was up starts its caption again from alpha 0.
        Resume = 4,
        // Nothing was up. A start that arrived during the parade may fire now.
        Rearm = 8,
        // A start arrived while the parade is in flight. Do not draw it yet.
        Wait = 16
    }

    public struct CoachResetGuard
    {
        public bool Flying;
        // A step was on screen (including mid fade-in or fade-out) when the parade began.
        public bool Suspended;
        // A step asked to start during the parade and had not drawn yet.
        public bool Deferred;

        // resetEdge: a reset began this call, including a second one while flying.
        // flying: the parade is in progress (birds out, or not home yet).
        // landedEdge: the formation finished landing this call.
        // showing: a tutorial step is up, or its caption/glove is mid-fade.
        // wantsStart: a step wants to become visible and is not already showing.
        public CoachResetVerb Tick(bool resetEdge, bool flying, bool landedEdge, bool showing, bool wantsStart, CoachFadePhase fade)
        {
            // The parade flag dropped without an explicit land. Finish once, so the
            // step cannot pop back on the stale alpha.
            if (!resetEdge && Flying && !flying)
                landedEdge = true;

            bool live = showing || fade != CoachFadePhase.None;
            if (resetEdge || (flying && !landedEdge))
            {
                Flying = true;
                if (live) Suspended = true;
                if (wantsStart && !live) Deferred = true;
                var verb = CoachResetVerb.Hold;
                if (live || Suspended) verb |= CoachResetVerb.Cut;
                if (Deferred || (wantsStart && !live)) verb |= CoachResetVerb.Wait;
                return verb;
            }

            if (landedEdge && Flying)
            {
                Flying = false;
                var verb = CoachResetVerb.Hold;
                if (Suspended) verb |= CoachResetVerb.Resume;
                else if (Deferred) verb |= CoachResetVerb.Rearm;
                Suspended = false;
                Deferred = false;
                return verb;
            }

            Flying = false;
            Suspended = false;
            Deferred = false;
            return CoachResetVerb.None;
        }

        public static bool BlocksDraw(CoachResetVerb verb) =>
            (verb & (CoachResetVerb.Cut | CoachResetVerb.Hold | CoachResetVerb.Wait)) != 0;

        // A start is refused while the parade holds the screen, and allowed on the
        // landing order (Resume / Rearm) so the next frame can fade in fresh.
        public static bool BlocksStart(CoachResetVerb verb) =>
            (verb & (CoachResetVerb.Hold | CoachResetVerb.Wait | CoachResetVerb.Cut)) != 0
            && (verb & (CoachResetVerb.Resume | CoachResetVerb.Rearm)) == 0;
    }
}
