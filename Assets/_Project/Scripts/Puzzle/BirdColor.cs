namespace FlockFive
{
    public enum BirdColor
    {
        Ruby = 0,
        Gold = 1,
        Teal = 2,
        Violet = 3
        // Fifth palette slot is reserved. Flock Five never ships more than Palette.Max colors.
    }

    public static class Palette
    {
        public const int Max = 5;
        public const int Shipped = 4;
    }
}
