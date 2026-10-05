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

        // Kill switch. Phase 5 (rewards, lesson, lose/retry) is wired, so the
        // contest is safe to play. While false: no flag is written, BadgerSave.Pending
        // reads 0, and the inspect-back honey chip stays hidden.
        // A property (not const) so editor tests can cover both states; only
        // tests use the setter.
        public static bool Enabled { get; set; } = true;

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

        // Shared price helper. Tutorial first-use is free (0); otherwise PowerUpPrice.
        // Callers pass isTutorialFirstUse from BadgerSave so button labels and TryPower agree.
        public static int PriceFor(BadgerPower power, int appearance, bool isTutorialFirstUse)
        {
            if (BasePrice(power) <= 0) return 0;
            if (isTutorialFirstUse) return 0;
            return PowerUpPrice(power, appearance);
        }

        // Button sub-label. "FREE" while the shared helper returns a tutorial free price.
        public static string PriceLabel(BadgerPower power, int appearance, bool isTutorialFirstUse)
        {
            if (BasePrice(power) <= 0) return "";
            if (isTutorialFirstUse) return "FREE";
            return Money.Format(PowerUpPrice(power, appearance));
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

    // First-fight (coach) script for moves 1-3 (spec 2b tutorial guidance). Pure: built from
    // the fight's own grids, so the album deal and the seeded boss mix stay as they are and
    // only the picks are scripted. BadgerFight consumes it (no separate fight path):
    //   move 1: the player's best tile vs a lower badger tile  (higher honey wins)
    //   move 2: the next best vs the badger's best tile        (the badger bites back)
    //   move 3: the next best vs a badger tile above half of it (the forced Don't Care halve)
    // Power-ups are locked for all three, so the compare is plain honey (move 3 halved).
    // If a deal would still let a column reach its target, the remaining moves fall back to
    // the player's lowest tile vs the badger's highest. BadgerFight also caps both columns
    // below target through move 3, so the guarantee never rests on the deal alone.
    public sealed class BadgerTutorialScript
    {
        public const int Moves = 3;

        readonly int[] _player = new int[Moves];
        readonly int[] _boss = new int[Moves];

        public int ExpectedPlayer { get; private set; }
        public int ExpectedBoss { get; private set; }
        public bool FellBack { get; private set; }

        // move is 1-based. -1 outside 1..Moves.
        public int PlayerPick(int move)
        {
            if (move < 1 || move > Moves) return -1;
            return _player[move - 1];
        }

        public int BossPick(int move)
        {
            if (move < 1 || move > Moves) return -1;
            return _boss[move - 1];
        }

        public static BadgerTutorialScript For(int[] player, int[] boss, int playerTarget, int badgerTarget)
        {
            var sc = new BadgerTutorialScript();
            int pn = player == null ? 0 : player.Length;
            int bn = boss == null ? 0 : boss.Length;
            var pOpen = new bool[pn];
            var bOpen = new bool[bn];
            for (int i = 0; i < pn; i++) pOpen[i] = true;
            for (int i = 0; i < bn; i++) bOpen[i] = true;
            int ps = 0, bs = 0;
            bool fall = false;
            for (int m = 0; m < Moves; m++)
            {
                bool halve = m == Moves - 1;
                int p, b;
                Choose(m, fall, player, boss, pOpen, bOpen, out p, out b);
                if (!fall && !Safe(player, boss, p, b, halve, ps, bs, playerTarget, badgerTarget))
                {
                    fall = true;
                    Choose(m, true, player, boss, pOpen, bOpen, out p, out b);
                }
                sc._player[m] = p;
                sc._boss[m] = b;
                if (p >= 0) pOpen[p] = false;
                if (b >= 0) bOpen[b] = false;
                Gains(player, boss, p, b, halve, out int pg, out int bg);
                ps += pg;
                bs += bg;
            }
            sc.ExpectedPlayer = ps;
            sc.ExpectedBoss = bs;
            sc.FellBack = fall;
            return sc;
        }

        static void Choose(int m, bool fall, int[] player, int[] boss, bool[] pOpen, bool[] bOpen, out int p, out int b)
        {
            if (fall)
            {
                p = Lowest(player, pOpen);
                b = Highest(boss, bOpen);
                return;
            }
            p = Highest(player, pOpen);
            int ph = p >= 0 ? player[p] : 0;
            if (m == 0)
            {
                b = LowestBelow(boss, bOpen, ph);
                if (b < 0) b = Lowest(boss, bOpen);
            }
            else if (m == 1)
            {
                b = Highest(boss, bOpen);
            }
            else
            {
                b = LowestAbove(boss, bOpen, BadgerFight.HalveHoney(ph));
                if (b < 0) b = Highest(boss, bOpen);
            }
        }

        static void Gains(int[] player, int[] boss, int p, int b, bool halve, out int pg, out int bg)
        {
            int ph = p >= 0 ? player[p] : 0;
            int bh = b >= 0 ? boss[b] : 0;
            BadgerFight.ApplyRound(ph, bh, BadgerPower.None, false, halve, out _, out _, out pg, out bg);
        }

        static bool Safe(int[] player, int[] boss, int p, int b, bool halve, int ps, int bs, int pt, int bt)
        {
            Gains(player, boss, p, b, halve, out int pg, out int bg);
            return ps + pg < pt && bs + bg < bt;
        }

        static int Highest(int[] v, bool[] open)
        {
            int best = -1;
            for (int i = 0; i < open.Length; i++)
                if (open[i] && (best < 0 || v[i] > v[best])) best = i;
            return best;
        }

        static int Lowest(int[] v, bool[] open)
        {
            int best = -1;
            for (int i = 0; i < open.Length; i++)
                if (open[i] && (best < 0 || v[i] < v[best])) best = i;
            return best;
        }

        // Lowest open tile strictly below `limit`.
        static int LowestBelow(int[] v, bool[] open, int limit)
        {
            int best = -1;
            for (int i = 0; i < open.Length; i++)
                if (open[i] && v[i] < limit && (best < 0 || v[i] < v[best])) best = i;
            return best;
        }

        // Lowest open tile strictly above `floor`.
        static int LowestAbove(int[] v, bool[] open, int floor)
        {
            int best = -1;
            for (int i = 0; i < open.Length; i++)
                if (open[i] && v[i] > floor && (best < 0 || v[i] < v[best])) best = i;
            return best;
        }
    }
}
