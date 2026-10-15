namespace FlockFive
{
    // Build 89. Remembers the last hop so the game can tell a real move from
    // the same flock hopping back and forth. A collect, a bee reveal, a pest
    // or a fresh board makes the next reverse a new move, so they reset it.
    public sealed class HopTrail
    {
        // Back, then forth again: the nudge shows on the second reversal.
        public const int NudgeAfter = 2;

        public int LastFrom { get; private set; } = -1;
        public int LastTo { get; private set; } = -1;
        public int LastRun { get; private set; }
        public int Reversals { get; private set; }

        public bool BackAndForth => Reversals >= NudgeAfter;

        public void Reset()
        {
            LastFrom = -1;
            LastTo = -1;
            LastRun = 0;
            Reversals = 0;
        }

        // fresh: this hop changed the board for good (collect, reveal).
        public void Note(int from, int to, int run, bool fresh)
        {
            if (fresh || run <= 0)
            {
                Reset();
                return;
            }
            if (Board.IsUndo(from, to, run, LastFrom, LastTo, LastRun)) Reversals++;
            else Reversals = 0;
            LastFrom = from;
            LastTo = to;
            LastRun = run;
        }

        public bool HasProgressHop(Board board) =>
            board != null && board.HasProgressHop(LastFrom, LastTo, LastRun);

        // CheckOver's rule: nothing real can move and nothing can collect.
        public bool DeadEnd(Board board) =>
            board != null && board.Displaced.Count == 0
            && board.FindCollect() < 0 && !HasProgressHop(board);

        // The gift nudge: a dead end, or the same flock hopping back and forth.
        public bool Stuck(Board board) =>
            board != null && board.FindCollect() < 0
            && (BackAndForth || !HasProgressHop(board));
    }
}
