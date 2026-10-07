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

    // What a bird wears. Plain draws the sex default: crown, bow, or nothing.
    // A level may set a real kit before the build pass. That pass leaves a
    // group alone when it already wears more than one kit.
    public enum BirdKit : byte
    {
        Plain = 0,
        Crown = 1,
        Bow = 2,
        TopHat = 3,
        Flower = 4,
        Shades = 5,
        Beanie = 6,
        Bowtie = 7
    }

    public struct Bird
    {
        public BirdColor Color;
        public BirdSex Sex;
        public BirdKit Kit;
        // True only when the level already split this color by accessory.
        // Cosmetic kits (the duplicate-set pass) leave this false.
        public bool KitBinds;

        public Bird(BirdColor color, BirdSex sex)
        {
            Color = color;
            Sex = sex;
            Kit = BirdKit.Plain;
            KitBinds = false;
        }

        public bool Female => Sex == BirdSex.Female;
        public bool Male => Sex == BirdSex.Male;
        public bool Bare => Sex == BirdSex.Neutral;

        // Hops match color and sex. An accessory counts only when the level
        // already used it to define the sets.
        public bool SameFlock(Bird other) =>
            Color == other.Color && Sex == other.Sex && KitsMatch(other);

        public bool KitsMatch(Bird other)
        {
            if (!KitBinds && !other.KitBinds) return true;
            return KitBinds && other.KitBinds && Kit == other.Kit;
        }

        public static bool operator ==(Bird a, Bird b) => a.SameFlock(b);
        public static bool operator !=(Bird a, Bird b) => !(a == b);
        public override bool Equals(object o) => o is Bird b && this == b;
        public override int GetHashCode()
        {
            int h = ((int)Color) * 3 + (int)Sex;
            if (KitBinds) h = h * 17 + (int)Kit + 1;
            return h;
        }
    }

    public static class Palette
    {
        public const int Max = 5;
        public const int Shipped = 5;
        public const int ComboMax = 16;
    }
}
