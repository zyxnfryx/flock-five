namespace FlockFive
{
    // One dismiss for every lesson. The tap that finishes an informational
    // caption is eaten, so the branch, gift, or button under the finger does
    // not also run. For GuardSeconds afterwards (caller passes unscaled time)
    // the board and the HUD ignore taps. A required step still delivers the
    // tap on its own target; the guard starts after that delivery.
    public static class CoachDismiss
    {
        public const float GuardSeconds = 0.25f;

        public struct Input
        {
            public bool LessonUp;
            public CoachStep Step;
            public bool OnTarget;
            // What the finger would operate if this tap were allowed through.
            public bool OverBranch;
            public bool OverGift;
            public bool OverHud;
            public float Now;
            public float GuardUntil;
        }

        public struct Result
        {
            public bool Dismissed;
            public bool Consumed;
            public bool DeliveredTarget;
            public bool BranchPick;
            public bool GiftPopup;
            public bool HudButton;
            public float GuardUntil;
        }

        public static bool GuardBlocks(float now, float guardUntil) => now < guardUntil;

        public static float Arm(float now, float guardUntil)
        {
            float until = now + GuardSeconds;
            return until > guardUntil ? until : guardUntil;
        }

        public static Result Resolve(Input tap)
        {
            if (!tap.LessonUp)
            {
                bool block = GuardBlocks(tap.Now, tap.GuardUntil);
                return new Result
                {
                    BranchPick = tap.OverBranch && !block,
                    GiftPopup = tap.OverGift && !block,
                    HudButton = tap.OverHud && !block,
                    GuardUntil = tap.GuardUntil,
                };
            }

            float until = Arm(tap.Now, tap.GuardUntil);
            if (!CoachTap.Advances(tap.Step, tap.OnTarget))
            {
                return new Result
                {
                    Consumed = true,
                    GuardUntil = tap.GuardUntil,
                };
            }

            if (CoachTap.Consumes(tap.Step))
            {
                return new Result
                {
                    Dismissed = true,
                    Consumed = true,
                    GuardUntil = until,
                };
            }

            bool deliver = tap.OnTarget;
            return new Result
            {
                Dismissed = true,
                DeliveredTarget = deliver,
                BranchPick = deliver && tap.OverBranch,
                GiftPopup = deliver && tap.OverGift,
                HudButton = deliver && tap.OverHud,
                GuardUntil = until,
            };
        }
    }
}
