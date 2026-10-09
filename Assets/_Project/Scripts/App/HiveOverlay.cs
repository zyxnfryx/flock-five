namespace FlockFive
{
    // One close for the Honey Gold inspect. Load, the splash, a board restart,
    // and a season reveal that is about to show all use this, so a flipped card
    // cannot stay up over restart, the keep-multiplier card, or the next banner.
    // A solved board that is still waiting on the open pop-up does not call it:
    // the finale stays gated until the player puts the card away.
    public static class HiveOverlay
    {
        public static void Close(ref int inspect, ref bool closing, ref float inspectT,
            ref int flip, ref float flipT, ref bool albumTutor)
        {
            inspect = -1;
            closing = false;
            inspectT = 0f;
            flip = -1;
            flipT = 0f;
            albumTutor = false;
        }
    }
}
