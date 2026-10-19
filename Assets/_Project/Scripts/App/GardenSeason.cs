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
    //   31-39  spring (the one deliberate change on a honey badger garden, Brandon-approved)
    //   40+    summer, fall, winter, spring, ... on a FIXED grid: change k is 40 + 8k. A change
    //          that would land on a honey badger garden slides +1 only for that change; the
    //          grid itself does not shift (40, 48, 57, 64, 72, 80, 88, 97, ...).
    // Brandon's rule: from 40 on, a backdrop change never lands on a honey badger garden.
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

        // Rotation change number k (0 = RotateFrom): fixed grid RotateFrom + 8k, +1 when that
        // lands on a badger garden. Never accumulates.
        public static int ChangeAt(int k)
        {
            int at = RotateFrom + RotateEvery * k;
            return IsBadgerLevel(at) ? at + 1 : at;
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

        // Validator: no backdrop change (fixed ones and the 40+ rotation) may land on a
        // badger garden. Returns the first offender or 0. GardenSeasonTests calls this.
        public static int FirstBadgerClash(int maxLevel)
        {
            var list = ChangeLevels(maxLevel);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == SpringFrom) continue; // the one approved badger-garden change
                if (IsBadgerLevel(list[i])) return list[i];
            }
            return 0;
        }
    }
}
