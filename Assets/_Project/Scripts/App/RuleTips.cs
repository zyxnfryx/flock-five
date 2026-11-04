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

        public const string KitLine = "Crowns go with crowns, bows with bows!\nA set is five matching birds.";
        public const string FeederLine = "Nice! That feeder now wants\na new colour. Check the top.";
        public const string SleepLine = "These birds are napping.\nThey wake when a feeder wants their colour.";

        // Longest a frozen tip may hold the garden before it clears itself.
        public const float HoldMax = 30f;

        public struct Seen
        {
            public bool Kit, Feeder, Sleep;
        }

        public struct Board
        {
            public bool HasKit, FeederTurned, AnySleeping;
        }

        // First unseen rule that is on the board now. Feeder and sleep are what just
        // happened, so they win over the kit tip, which is always on the board.
        public static RuleTip Due(Seen seen, Board b)
        {
            if (b.FeederTurned && !seen.Feeder) return RuleTip.Feeder;
            if (b.AnySleeping && !seen.Sleep) return RuleTip.Sleep;
            if (b.HasKit && !seen.Kit) return RuleTip.Kit;
            return RuleTip.None;
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
