namespace FlockFive
{
    // One visibility rule for a garden bird and everything it wears: the kit
    // (bow, crown, or a duplicate-set accessory), the face, the select aura,
    // cheer twinkles, Zzz and the "!".
    // BirdIdle reads these every LateUpdate, so nothing a bird wears can draw
    // while its body is hidden. Pure, so the console harness can test it.
    public static class BirdDress
    {
        // Lift above this means the bird is up off its seat (BirdIdle uses the same 0.05).
        public const float LiftedAt = 0.05f;

        // A stage-reset veil uses forceRenderingOff, which leaves enabled true.
        // Both count: a veiled body is a hidden body.
        public static bool BodyShown(bool enabled, bool forcedOff) => enabled && !forcedOff;

        // bare: nothing worn (a plain neutral). A sex kit or a set accessory draws.
        public static bool KitOn(bool bodyShown, bool shrouded, bool bare) =>
            bodyShown && !shrouded && !bare;

        public static bool FaceOn(bool bodyShown, bool shrouded) => bodyShown && !shrouded;

        // Zzz and the "!" belong to a seated bird. A frozen, lifted bird is a
        // restart or collect flight, so they stay off there.
        public static bool SeatFxOn(bool bodyShown, bool frozen, float lift) =>
            bodyShown && !(frozen && lift > LiftedAt);
    }
}
