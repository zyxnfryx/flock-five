using System.Collections.Generic;

namespace FlockFive
{
    // Which painting sits behind a garden. Summer is the jungle garden.
    public enum GardenScene
    {
        Summer = 0,
        Desert = 1,
        Fall = 2,
        Winter = 3,
        Spring = 4,
    }

    // One schedule for the garden backdrop by garden number (LevelData.DisplayNumber).
    // Pure: no sprites, no prefs. SpriteCatalog.GardenBgFor turns the scene into art.
    //   1-14   summer (jungle)
    //   15-24  desert oasis
    //   25-30  winter
    //   31-39  spring
    //   40+    summer, fall, winter, spring, ... on a FIXED grid: change k is 40 + 8k
    //          (40, 48, 56, 64, 72, 80, 88, 96, ...). A change may land on a honey badger
    //          garden. It slides +1 (for that change only, the grid does not shift) when it
    //          would land on an ostrich garden.
    // Brandon's rule: a backdrop change never lands on an ostrich garden.
    // SeasonReveal plays that change once, the first time the garden is entered.
    public static class GardenSeason
    {
        public const int DesertFrom = 15;
        public const int WinterFrom = 25;
        public const int SpringFrom = 31;
        public const int RotateFrom = 40;
        public const int RotateEvery = 8;

        // Rotation order from RotateFrom on. RotateFrom itself is the first entry.
        static readonly GardenScene[] Rotation =
        {
            GardenScene.Summer,
            GardenScene.Fall,
            GardenScene.Winter,
            GardenScene.Spring,
        };

        public static GardenScene ForLevel(int level)
        {
            if (level < DesertFrom) return GardenScene.Summer;
            if (level < WinterFrom) return GardenScene.Desert;
            if (level < SpringFrom) return GardenScene.Winter;
            if (level < RotateFrom) return GardenScene.Spring;
            int k = 0;
            while (ChangeAt(k + 1) <= level) k++;
            return Rotation[k % Rotation.Length];
        }

        // Honey badger garden: the contest is flagged on clearing 15, 20, 25, ... so it
        // plays as gardens 16, 21, 26, ... start (every garden ending in 6 or 1 from 16).
        // Reads BadgerSchedule.Due (not Enabled) so a schedule change moves the seasons too.
        public static bool IsBadgerLevel(int level)
        {
            return BadgerSchedule.Due(level - 1);
        }

        // Ostrich garden: authored ostrich stages in the ladder (LevelData.Level.Ostrich).
        // Past the authored ladder there are none yet.
        public static bool IsOstrichLevel(int level)
        {
            return level >= 1 && level <= LevelData.Count && LevelData.Peek(level - 1).Ostrich;
        }

        // Rotation change number k (0 = RotateFrom): fixed grid RotateFrom + 8k, +1 (repeat)
        // only while that lands on an ostrich garden. Never accumulates.
        public static int ChangeAt(int k)
        {
            int at = RotateFrom + RotateEvery * k;
            while (IsOstrichLevel(at)) at++;
            return at;
        }

        // Every garden where the backdrop changes, ascending, up to and including `maxLevel`.
        public static List<int> ChangeLevels(int maxLevel)
        {
            var list = new List<int>();
            if (maxLevel >= DesertFrom) list.Add(DesertFrom);
            if (maxLevel >= WinterFrom) list.Add(WinterFrom);
            if (maxLevel >= SpringFrom) list.Add(SpringFrom);
            if (maxLevel < RotateFrom) return list;
            for (int k = 0; ChangeAt(k) <= maxLevel; k++)
                list.Add(ChangeAt(k));
            return list;
        }

        public static bool IsChangeLevel(int level)
        {
            if (level <= 1) return false;
            return ForLevel(level) != ForLevel(level - 1);
        }

        // Validator: no backdrop change (fixed ones and the 40+ rotation) may land on an
        // ostrich garden. Returns the first offender or 0. GardenSeasonTests calls this.
        public static int FirstOstrichClash(int maxLevel)
        {
            var list = ChangeLevels(maxLevel);
            for (int i = 0; i < list.Count; i++)
            {
                if (IsOstrichLevel(list[i])) return list[i];
            }
            return 0;
        }
    }
}
