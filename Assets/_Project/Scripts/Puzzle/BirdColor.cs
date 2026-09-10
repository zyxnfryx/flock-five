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

    public enum BirdSex
    {
        Neutral = 0,
        Female = 1,
        Male = 2
    }

    public struct Bird
    {
        public BirdColor Color;
        public BirdSex Sex;

        public Bird(BirdColor color, BirdSex sex)
        {
            Color = color;
            Sex = sex;
        }

        public bool Female => Sex == BirdSex.Female;
        public bool Male => Sex == BirdSex.Male;
        public bool Bare => Sex == BirdSex.Neutral;

        public bool SameFlock(Bird other) => Color == other.Color && Sex == other.Sex;

        public static bool operator ==(Bird a, Bird b) => a.Color == b.Color && a.Sex == b.Sex;
        public static bool operator !=(Bird a, Bird b) => !(a == b);
        public override bool Equals(object o) => o is Bird b && this == b;
        public override int GetHashCode() => ((int)Color) * 3 + (int)Sex;
    }

    public static class Palette
    {
        public const int Max = 5;
        public const int Shipped = 5;
        public const int ComboMax = 16;
    }
}
