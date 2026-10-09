namespace FlockFive
{
    // The one solved-board rule. A bird counts as finished only when it sits in a
    // complete set: five of its colour on one branch (BranchState.IsFullMatch).
    // A partial perch, a mixed perch, a shroud, a locked gift that is not itself
    // a complete set, a bird still in the air, or a lone straggler is not solved.
    //
    // An empty board is solved. No bird is outside a complete set, a cleared
    // garden is the win, and pests must not arrive on it. A null board is not.
    //
    // A 4+1 split (the perch a pest uses so a scrap cannot auto-clear) is not
    // solved. The finale does not start, and the pest schedule stays open.
    public static class SolvedRule
    {
        public static bool IsSolved(Board board)
        {
            if (board == null) return false;
            if (board.Displaced.Count > 0) return false;
            var branches = board.Branches;
            for (int i = 0; i < branches.Count; i++)
            {
                var b = branches[i];
                if (b == null || b.Broken || b.Count == 0) continue;
                if (!b.IsFullMatch(out _)) return false;
            }
            return true;
        }

        // The finale is the cleared garden: solved, and no bird left perched.
        // Complete sets still waiting on a feeder are solved (no new pests) but
        // the show waits until those sets have left. A 4+1 park is neither.
        public static bool FinaleDue(Board board)
        {
            return board != null && IsSolved(board) && board.Won;
        }

        // Hive inspect delays the show. It does not make a partial board solved
        // and it does not cancel a clear that is already due.
        public static bool FinaleHeldByHive(bool hiveOpen) => hiveOpen;

        // A split is not solved, so nothing joins it and nothing clears it.
        // A finished set of five clears only through the feeder it matches.
        public static bool WouldAutoClear(Board board)
        {
            if (!IsSolved(board)) return false;
            if (board.FindCollect() >= 0) return true;
            return board.NextSolvedMerge(out _, out _);
        }

        // Board shape, plus a win sequence that has already started (the
        // auto-resolve merge, garden scoring, or the win latch). A 4+1 park
        // is not that sequence, so a pest still out keeps its schedule.
        public static bool BlocksNewPests(Board board, bool resolveBegun)
        {
            if (resolveBegun) return true;
            return IsSolved(board);
        }
    }
}
