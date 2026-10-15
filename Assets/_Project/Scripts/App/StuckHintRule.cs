namespace FlockFive
{
    // b104 (Brandon): one rule for when a "stuck" gift hint may arm or stay up. A solved board reads as
    // "no progress hop, nothing to collect", which looked stuck while a sparrow arrived on it.
    public static class StuckHintRule
    {
        public struct Live
        {
            public bool Won, Restarting, Frozen, Paused, Modal, Collecting, SolvedMerging, Fitting;
            public bool PestOnStage, PestResolving, PestFinishing, Scatter;
        }

        public static bool Allowed(Board board, Live s)
        {
            if (board == null) return false;
            if (s.Won || s.Restarting || s.Frozen || s.Paused || s.Modal) return false;
            if (SolvedRule.IsSolved(board) || SolvedRule.SplitOnly(board)) return false;
            if (s.Collecting || s.SolvedMerging || s.Fitting) return false;
            if (s.PestOnStage || s.PestResolving || s.PestFinishing || s.Scatter) return false;
            return true;
        }

        // The hint fires only when the rule allows it and the board is really stuck.
        public static bool ShouldArm(Board board, Live s, HopTrail trail) =>
            Allowed(board, s) && trail != null && trail.Stuck(board);
    }
}
