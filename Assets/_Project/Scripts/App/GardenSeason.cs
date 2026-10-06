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
    //   25-31  winter
    //   32-39  spring (31 is a honey badger garden, so spring waits one)
    //   40+    summer, fall, winter, spring, ... every 8 gardens. A change that would land
    //          on a honey badger garden slides +1 (repeat until clean), and the next
    //          8 counts from where it actually landed.
    // Brandon's rule: a backdrop change never lands on a honey badger garden.
    public static class GardenSeason
    {
        public const int DesertFrom = 15;
        public const int WinterFrom = 25;
        public const int SpringFrom = 32;
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

        // Rotation change gardens, grown on demand. _changes[k] wears Rotation[k % 4].
        static readonly List<int> _changes = new List<int> { RotateFrom };

        public static GardenScene ForLevel(int level)
        {
            if (level < DesertFrom) return GardenScene.Summer;
            if (level < WinterFrom) return GardenScene.Desert;
            if (level < SpringFrom) return GardenScene.Winter;
            if (level < RotateFrom) return GardenScene.Spring;
            Grow(level);
            int k = 0;
            for (int i = _changes.Count - 1; i >= 0; i--)
            {
                if (_changes[i] > level) continue;
                k = i;
                break;
            }
            return Rotation[k % Rotation.Length];
        }

        // Honey badger garden: the contest is flagged on clearing 15, 20, 25, ... so it
        // plays as gardens 16, 21, 26, ... start (every garden ending in 6 or 1 from 16).
        // Reads BadgerSchedule.Due (not Enabled) so a schedule change moves the seasons too.
        public static bool IsBadgerLevel(int level)
        {
            return BadgerSchedule.Due(level - 1);
        }

        // Next rotation change after `from`: +8, then +1 while it lands on a badger garden.
        public static int NextChange(int from)
        {
            int at = from + RotateEvery;
            int guard = 0;
            while (IsBadgerLevel(at) && guard++ < RotateEvery)
                at++;
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
            Grow(maxLevel);
            for (int i = 0; i < _changes.Count && _changes[i] <= maxLevel; i++)
                list.Add(_changes[i]);
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
                if (IsBadgerLevel(list[i])) return list[i];
            }
            return 0;
        }

        static void Grow(int level)
        {
            while (_changes[_changes.Count - 1] <= level)
                _changes.Add(NextChange(_changes[_changes.Count - 1]));
        }
    }
}
