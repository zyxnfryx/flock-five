namespace FlockFive
{
    public enum RuleTip
    {
        None = 0,
        Kit = 1,
        Feeder = 2,
        Sleep = 3,
    }

    // b120: one short caption the first time a rule shows up on the board.
    // Pure so the order and the copy are testable in EditMode.
    public static class RuleTips
    {
        public const string KitKey = "flockfive.coach.rule.kit";
        public const string FeederKey = "flockfive.coach.rule.feeder";
        public const string SleepKey = "flockfive.coach.rule.sleep";

        // Two rows. "Bows with bows!" stays whole on the second row so a wrap
        // cannot split it into "bows with" / "bows!".
        public const string KitLine = "Crowns with crowns.\nBows with bows! Five a set.";
        public const string FeederLine = "Nice! That feeder wants\na new color. Check the top.";
        public const float ReadSeconds = 4f;
        public const string SleepLine = "These birds are napping.\nThey wake for their color.";

        // Longest a frozen tip may hold the garden before it clears itself.
        public const float HoldMax = 30f;

        // How long the move may wait for the first Zzz before it gives up.
        // The caption still requires the pose; this only stops the waiter.
        public const float NapWait = 1.5f;

        public struct Seen
        {
            public bool Kit, Feeder, Sleep;
        }

        public struct Board
        {
            public bool HasKit, FeederTurned, AnySleeping;
            // A bird on a sleeping stack has drawn Zzz. Board sleep alone is the
            // commit, which is still the hop.
            public bool NapBegun;
        }

        // First unseen rule that is on the board now. Feeder and sleep are what just
        // happened, so they win over the kit tip, which is always on the board.
        // Crowns are packed onto the birds (garden 1: Teal wears Crown). They are
        // not granted when Nice! dismisses. The crown caption is the next unseen
        // tip once Nice is gone, on every field size. A tall bottom bar can hide
        // the Teal crowns while Nice is up; the pill does not.
        // Sleep is on the board the instant the stack commits. The caption waits
        // until NapBegun, and until then it holds the slot so the kit tip cannot
        // pause the hop in its place.
        public static RuleTip Due(Seen seen, Board b)
        {
            if (b.FeederTurned && !seen.Feeder) return RuleTip.Feeder;
            if (b.AnySleeping && !seen.Sleep)
                return b.NapBegun ? RuleTip.Sleep : RuleTip.None;
            if (b.HasKit && !seen.Kit) return RuleTip.Kit;
            return RuleTip.None;
        }

        public static bool IsLine(string text)
        {
            return text == KitLine || text == FeederLine || text == SleepLine;
        }

        // The new feeder colour is on screen. Open the tip even if the collect
        // animation has not finished. blocked: splash, win, restart, or another lesson.
        public static bool FeederReady(bool turned, bool seen, bool tipUp, bool blocked)
        {
            return turned && !seen && !tipUp && !blocked;
        }

        // First nap in any garden. Not tied to a level name.
        // napBegun: a bird on that stack has drawn Zzz. The board can be asleep
        // while the hop is still in the air; the caption pauses time, so it waits.
        public static bool SleepReady(bool sleeping, bool napBegun, bool seen, bool tipUp, bool blocked)
        {
            return sleeping && napBegun && !seen && !tipUp && !blocked;
        }

        public static string Line(RuleTip t)
        {
            switch (t)
            {
                case RuleTip.Kit: return KitLine;
                case RuleTip.Feeder: return FeederLine;
                case RuleTip.Sleep: return SleepLine;
            }
            return null;
        }

        public static string Key(RuleTip t)
        {
            switch (t)
            {
                case RuleTip.Kit: return KitKey;
                case RuleTip.Feeder: return FeederKey;
                case RuleTip.Sleep: return SleepKey;
            }
            return null;
        }

        public static CoachStep Step(RuleTip t)
        {
            switch (t)
            {
                case RuleTip.Kit: return CoachStep.RuleKit;
                case RuleTip.Feeder: return CoachStep.RuleFeeder;
                default: return CoachStep.RuleSleep;
            }
        }

        // A slot that held a colour and now holds a different one (or none) turned.
        public static bool Turned(BirdColor?[] before, BirdColor?[] now)
        {
            if (before == null || now == null || before.Length != now.Length) return false;
            for (int i = 0; i < now.Length; i++)
                if (before[i] != null && before[i] != now[i]) return true;
            return false;
        }
    }
}
