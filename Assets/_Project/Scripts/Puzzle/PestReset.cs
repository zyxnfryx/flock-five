namespace FlockFive
{
    // Garden restart. One call for every pest: the ostrich fight returns to
    // its opening, and every visitor leaves with its perch, scrap, and fx.
    public static class PestReset
    {
        public static void Apply(Board board)
        {
            if (board != null)
            {
                if (board.Ostrich != null) board.Ostrich.Reset();
                board.Displaced.Clear();
            }
            SparrowView.DismissForReset();
            HawkView.DismissForReset();
            SparrowView.ClearFx();
            PestSchedule.ReleaseHold();
        }
    }
}
