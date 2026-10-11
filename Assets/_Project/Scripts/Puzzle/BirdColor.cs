namespace FlockFive
{
    public enum BirdColor
    {
        Ruby = 0,
        Gold = 1,
        Teal = 2,
        Violet = 3,
        Peach = 4
    }

    // Poker cards only (BirdPoker.Card, poker face art). Garden birds have no sex.
    public enum BirdSex
    {
        Neutral = 0,
        Female = 1,
        Male = 2
    }

    // Build 82: each color has exactly three kits. Plain is the bare head.
    // Kit is part of flock identity: a set is five birds of one color AND one kit.
    public enum BirdKit : byte
    {
        Plain = 0,
        Crown = 1,
        Bow = 2
    }

    public struct Bird
    {
        public BirdColor Color;
        public BirdKit Kit;

        public Bird(BirdColor color, BirdKit kit = BirdKit.Plain)
        {
            Color = color;
            Kit = kit;
        }

        // The one flock-identity rule. Every hop, run, five, bee and solver key uses this.
        public bool SameFlock(Bird other) => Color == other.Color && Kit == other.Kit;

        public static bool operator ==(Bird a, Bird b) => a.SameFlock(b);
        public static bool operator !=(Bird a, Bird b) => !(a == b);
        public override bool Equals(object o) => o is Bird b && this == b;
        public override int GetHashCode() => (int)Color * 3 + (int)Kit;
    }

    public static class Palette
    {
        public const int Max = 5;
        public const int Shipped = 5;
        public const int ComboMax = 16;
        public const int Kits = 3;
    }
}
