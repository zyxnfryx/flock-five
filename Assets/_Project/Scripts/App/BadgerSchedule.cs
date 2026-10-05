using System;
using System.Collections.Generic;

namespace FlockFive
{
    public enum BadgerPower
    {
        None = 0,
        X2 = 1,
        X3 = 2,
        HotSauce = 3,
        FreezeSpray = 4,
    }

    // When the honey badger contest is due, and the numbers for that visit.
    // Pure: no purse, no album writes, no views.
    public static class BadgerSchedule
    {
        public const int FirstAfter = 15;
        public const int Interval = 5;
        public const int Side = 4;
        public const int Tiles = Side * Side;
        public const int YardHoney = 1;
        public const int PlayerTargetStart = 10;
        public const int PlayerTargetCap = 18;
        public const int BadgerTargetFixed = 18;

        // KILL SWITCH. Phases 3-5 (loadout, fight, rewards) are not built, so a set
        // flag would lock the player out of the next garden with no fight to play.
        // While false: no flag is written, BadgerSave.Pending reads 0, and the
        // inspect-back honey chip stays hidden. Flip to true when Phases 3-5 ship.
        // A property (not const) so editor tests can cover both states; only
        // tests use the setter.
        public static bool Enabled { get; set; } = false;

        // Counts of honey 2, 3, 4, 5 (fourteen tiles). Two 1s are added on top.
        // Every row rises: more 3s than 2s, more 4s than 3s, more 5s than 4s.
        // Each visit shifts one step hotter. The last row is the hottest mix
        // that still rises. Later visits stay on it. That ceiling is the
        // 16-tile grid, not a cap on which visit can show up.
        static readonly int[,] Rise =
        {
            { 2, 3, 4, 5 },
            { 1, 3, 4, 6 },
            { 0, 3, 4, 7 },
            { 0, 2, 4, 8 },
            { 0, 1, 4, 9 },
            { 0, 1, 3, 10 },
            { 0, 1, 2, 11 },
        };

        public static bool Due(int cleared)
        {
            if (cleared < FirstAfter) return false;
            return (cleared - FirstAfter) % Interval == 0;
        }

        // Hook rule: this clear writes the unpaid flag. The switch is a parameter so
        // tests can cover both states; the one-argument form reads Enabled.
        public static bool ShouldFlag(int cleared) => ShouldFlag(cleared, Enabled);

        public static bool ShouldFlag(int cleared, bool enabled)
        {
            return enabled && Due(cleared);
        }

        // (cleared - 15) / 5 + 1. Callers gate on Due. 16 still math-evaluates to 1.
        public static int Appearance(int cleared)
        {
            if (cleared < FirstAfter) return 0;
            return (cleared - FirstAfter) / Interval + 1;
        }

        public static int PlayerTarget(int appearance)
        {
            if (appearance >= PlayerTargetCap - PlayerTargetStart + 1) return PlayerTargetCap;
            int n = appearance < 1 ? 1 : appearance;
            return PlayerTargetStart + (n - 1);
        }

        // Always 18. Appearance is accepted so both meters are read the same way.
        public static int BadgerTarget(int appearance)
        {
            return BadgerTargetFixed;
        }

        public static int BasePrice(BadgerPower power)
        {
            if (power == BadgerPower.X2) return 100;
            if (power == BadgerPower.X3) return 200;
            if (power == BadgerPower.HotSauce) return 300;
            if (power == BadgerPower.FreezeSpray) return 600;
            return 0;
        }

        // base + 50 * (n - 1). No cap. A bad power is 0.
        public static int PowerUpPrice(BadgerPower power, int appearance)
        {
            int basePrice = BasePrice(power);
            if (basePrice <= 0) return 0;
            int n = appearance < 1 ? 1 : appearance;
            long price = basePrice + 50L * (n - 1);
            if (price > int.MaxValue) return int.MaxValue;
            return (int)price;
        }

        // Sixteen honey values. Same appearance and seed always return the same order.
        public static int[] BossTileMix(int appearance, int seed)
        {
            int row = appearance - 1;
            int last = Rise.GetLength(0) - 1;
            if (row < 0) row = 0;
            if (row > last) row = last;

            var tiles = new int[Tiles];
            int wrote = 0;
            tiles[wrote++] = 1;
            tiles[wrote++] = 1;
            for (int col = 0; col < 4; col++)
            {
                int face = col + 2;
                int count = Rise[row, col];
                for (int k = 0; k < count; k++)
                    tiles[wrote++] = face;
            }
            Shuffle(tiles, seed);
            return tiles;
        }

        // Best owned bees first (higher honey first). Pads with yard honey 1.
        // Does not remove album copies. BadgerLoadout is the one place that deals it.
        public static int[] PlayerLoadout(int slots)
        {
            return BadgerLoadout.Preload(slots).Honeys();
        }

        public static int[] PlayerLoadout()
        {
            return PlayerLoadout(Tiles);
        }

        // Pure sort-and-pad. `ownedHoney` is already in honey units.
        public static int[] ArrangeLoadout(int[] ownedHoney, int slots)
        {
            if (slots < 0) slots = 0;
            var tiles = new int[slots];
            int owned = ownedHoney == null ? 0 : ownedHoney.Length;
            var order = new int[owned];
            for (int i = 0; i < owned; i++) order[i] = ownedHoney[i];
            if (owned > 1) Array.Sort(order, (a, b) => b.CompareTo(a));
            int take = owned < slots ? owned : slots;
            for (int i = 0; i < take; i++) tiles[i] = order[i];
            for (int i = take; i < slots; i++) tiles[i] = YardHoney;
            return tiles;
        }

        static void Shuffle(int[] tiles, int seed)
        {
            var rng = new Random(seed);
            for (int i = tiles.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int swap = tiles[i];
                tiles[i] = tiles[j];
                tiles[j] = swap;
            }
        }
    }
}
