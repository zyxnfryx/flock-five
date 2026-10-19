namespace FlockFive
{
    // A tap on a honeycomb is not a flip. It squishes and nothing is left behind on screen
    // (the honey smudges/fingerprints were removed: touching must not mark the screen).
    // What stays is the press latch, so a drag-then-release in one cell squishes once.
    public static class HoneyPoke
    {
        // Upper bound on honey cells on one card back; sizes the hit-rect scratch array.
        public const int Cap = 10;

        // The drag latch ("this press already poked cell N") lives here so Clear() (card put away, restart) drops it too.
        public static int LastCell = -1;
        public static void Clear() { LastCell = -1; }
        public static void PressStart() { LastCell = -1; }
        // True when a drag into cell i should poke (first entry of this press).
        public static bool DragInto(int i) { if (LastCell == i) return false; LastCell = i; return true; }
        // True when a release in cell i should poke. False when the drag already poked this cell. Always unlatches.
        public static bool Release(int i) { bool skip = LastCell == i; LastCell = -1; return !skip; }
    }
}
