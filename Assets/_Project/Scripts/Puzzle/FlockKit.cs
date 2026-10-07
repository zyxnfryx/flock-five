using System.Collections.Generic;

namespace FlockFive
{
    // One place that gives duplicate flocks different accessories.
    // Called once, after the sex stamp, on the packed garden.
    public static class FlockKit
    {
        public enum Seat
        {
            Head,
            Tuck,
            Face
        }

        struct Spot
        {
            public int Branch;
            public int Index;
        }

        // Later sets, in order. Set 0 keeps whatever the bird already wears
        // (plain crown / bow / bare, or one authored kit on the whole flock).
        static readonly BirdKit[] MaleAlt = { BirdKit.TopHat, BirdKit.Shades, BirdKit.Beanie, BirdKit.Flower };
        static readonly BirdKit[] FemaleAlt = { BirdKit.Flower, BirdKit.Bowtie, BirdKit.Shades, BirdKit.Beanie };
        static readonly BirdKit[] NeutralAlt = { BirdKit.Beanie, BirdKit.Shades, BirdKit.Bowtie, BirdKit.Flower };

        public static BirdKit Worn(Bird bird) => Worn(bird.Sex, bird.Kit);

        public static BirdKit Worn(BirdSex sex, BirdKit kit)
        {
            if (kit != BirdKit.Plain) return kit;
            if (sex == BirdSex.Female) return BirdKit.Bow;
            if (sex == BirdSex.Male) return BirdKit.Crown;
            return BirdKit.Plain;
        }

        public static bool BehindBody(BirdKit worn) =>
            worn == BirdKit.Bow || worn == BirdKit.Bowtie;

        public static bool OverFace(BirdKit worn) => worn == BirdKit.Shades;

        public static Seat SeatOf(BirdKit worn)
        {
            if (worn == BirdKit.Shades) return Seat.Face;
            if (BehindBody(worn)) return Seat.Tuck;
            return Seat.Head;
        }

        // Two or more full sets of the same color and sex each get their own
        // accessory. Five birds inside a set stay identical. A group that
        // already wears more than one kit is a level-authored split: leave
        // the kits, and let them count in the hop.
        public static void Assign(Board b)
        {
            if (b == null) return;
            var buckets = new List<Spot>[Palette.Max * 3];
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var br = b.Branches[i];
                if (br == null) continue;
                for (int k = 0; k < br.Birds.Count; k++)
                {
                    var bird = br.Birds[k];
                    int key = (int)bird.Color * 3 + (int)bird.Sex;
                    if ((uint)key >= (uint)buckets.Length) continue;
                    var list = buckets[key];
                    if (list == null)
                    {
                        list = new List<Spot>();
                        buckets[key] = list;
                    }
                    list.Add(new Spot { Branch = i, Index = k });
                }
            }
            for (int key = 0; key < buckets.Length; key++)
            {
                var list = buckets[key];
                if (list == null || list.Count == 0) continue;
                if (AlreadySplit(b, list))
                {
                    Bind(b, list);
                    continue;
                }
                int sets = list.Count / BranchState.Cap;
                if (sets < 2) continue;
                var sex = (BirdSex)(key % 3);
                var keep = At(b, list[0]).Kit;
                for (int i = 0; i < list.Count; i++)
                {
                    int set = i / BranchState.Cap;
                    if (set >= sets) set = 0;
                    var br = b.Branches[list[i].Branch];
                    var bird = br.Birds[list[i].Index];
                    bird.Kit = set == 0 ? keep : Look(sex, set, keep);
                    bird.KitBinds = false;
                    br.Birds[list[i].Index] = bird;
                }
            }
        }

        static bool AlreadySplit(Board b, List<Spot> list)
        {
            var worn = Worn(At(b, list[0]));
            for (int i = 1; i < list.Count; i++)
                if (Worn(At(b, list[i])) != worn) return true;
            return false;
        }

        static void Bind(Board b, List<Spot> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var br = b.Branches[list[i].Branch];
                var bird = br.Birds[list[i].Index];
                bird.KitBinds = true;
                br.Birds[list[i].Index] = bird;
            }
        }

        static Bird At(Board b, Spot spot) => b.Branches[spot.Branch].Birds[spot.Index];

        static BirdKit Look(BirdSex sex, int set, BirdKit keep)
        {
            var pool = sex == BirdSex.Female ? FemaleAlt : sex == BirdSex.Male ? MaleAlt : NeutralAlt;
            var wornKeep = Worn(sex, keep);
            int usable = 0;
            for (int i = 0; i < pool.Length; i++)
                if (pool[i] != keep && Worn(sex, pool[i]) != wornKeep) usable++;
            if (usable == 0 || set <= 0) return keep;
            int pick = (set - 1) % usable;
            int seen = 0;
            for (int i = 0; i < pool.Length; i++)
            {
                var kit = pool[i];
                if (kit == keep || Worn(sex, kit) == wornKeep) continue;
                if (seen == pick) return kit;
                seen++;
            }
            return keep;
        }
    }
}
