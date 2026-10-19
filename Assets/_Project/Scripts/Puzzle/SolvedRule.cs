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

        // b87 (Brandon, iPad: reds left 4+1 after a sparrow, solve never completed): a board
        // whose every perched limb is one unshrouded colour, with nothing displaced, has no
        // decision left. Once the pest is gone it is merged automatically into fives.
        public static bool SplitOnly(Board board)
        {
            if (board == null || board.Displaced.Count > 0) return false;
            bool split = false;
            var seen = new System.Collections.Generic.Dictionary<Bird, int>();
            var branches = board.Branches;
            for (int i = 0; i < branches.Count; i++)
            {
                var b = branches[i];
                if (b == null || b.Broken || b.Count == 0) continue;
                var first = b.Birds[0];
                for (int k = 0; k < b.Count; k++)
                {
                    if (b.IsShrouded(k)) return false;
                    if (!b.Birds[k].SameFlock(first)) return false;
                }
                if (b.Count < BranchState.Cap)
                {
                    if (seen.ContainsKey(first)) split = true;
                    seen[first] = i;
                }
            }
            return split;
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

    // Safety net for a finished set on a live feeder that nobody picked up.
    // Collects start from KickCollects, which the player's hop and the tail of
    // the previous collect each call once. It refuses while another collect is
    // in flight, and when the first ready limb is locked it does not look at
    // the others. Nothing else ever asks again, so any path that skips the
    // tail (a lock, a coroutine that dies, a restart race) leaves the set full
    // on its limb, feeder in view, until the next hop. Polled from Update the
    // way TrySolvedFinish is, so a skipped kick runs the first idle frame.
    // Pure so the timing is testable without a scene.
    public static class CollectPoll
    {
        // Seconds the board must sit idle with a ready set before the poll kicks.
        // Longer than a frame gap between a hop landing and its own kick.
        public const float Hold = 0.6f;

        // True when a set is ready and nothing else owns the board.
        public static bool Ready(Board board, bool idle)
        {
            if (!idle || board == null) return false;
            if (board.Displaced.Count > 0) return false;
            return board.FindCollect() >= 0;
        }

        // since < 0 means not waiting. Returns true once, when Hold has passed.
        public static bool Step(bool ready, float now, ref float since)
        {
            if (!ready)
            {
                since = -1f;
                return false;
            }
            if (since < 0f)
            {
                since = now;
                return false;
            }
            if (now - since < Hold) return false;
            since = -1f;
            return true;
        }
    }
}
