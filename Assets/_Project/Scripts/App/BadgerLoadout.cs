using System;
using System.Collections.Generic;

namespace FlockFive
{
    // One tile on the player's 4x4 grid. Kind -1 is a yard-honey tile (not an album bee).
    // Honey comes from Hive.HoneyOf (base finish + card upgrades), or the yard value.
    public struct BadgerTile
    {
        public int Kind;
        public BeeFinish Finish;
        public int Honey;

        public bool Yard => Kind < 0;

        public static BadgerTile Bee(int kind, BeeFinish finish)
        {
            return new BadgerTile { Kind = kind, Finish = finish, Honey = Hive.HoneyOf(kind, finish) };
        }

        public static BadgerTile YardTile()
        {
            return new BadgerTile { Kind = -1, Finish = BeeFinish.Normal, Honey = BadgerSchedule.YardHoney };
        }

        public bool Same(BadgerTile other)
        {
            return Kind == other.Kind && (Kind < 0 || Finish == other.Finish);
        }
    }

    // The player's grid before a fight: preloaded with the best owned bees, padded with
    // yard honey. Swap helpers remain for tests; the contest screen auto-fills and skips
    // the swap UI. Pure: reads counts through a delegate (Hive in the game, a fake album
    // in tests). Never writes the album.
    public sealed class BadgerLoadout
    {
        public delegate int CountOf(int kind, BeeFinish finish);

        readonly CountOf _count;
        readonly int _kinds;
        readonly BadgerTile[] _tiles;

        public int Length => _tiles.Length;

        BadgerLoadout(CountOf count, int kinds, int slots)
        {
            _count = count;
            _kinds = kinds < 0 ? 0 : kinds;
            _tiles = new BadgerTile[slots < 0 ? 0 : slots];
        }

        public BadgerTile this[int index]
        {
            get
            {
                if ((uint)index >= (uint)_tiles.Length) return BadgerTile.YardTile();
                return _tiles[index];
            }
        }

        public static BadgerLoadout Preload(int slots)
        {
            return Preload((k, f) => Hive.CountOf(k, f), Hive.Kinds, slots);
        }

        // Best owned first: higher honey, then lower kind, then lower finish. Each owned copy
        // can fill one tile. Fewer bees than slots pads with yard honey.
        public static BadgerLoadout Preload(CountOf count, int kinds, int slots)
        {
            var load = new BadgerLoadout(count, kinds, slots);
            var owned = new List<BadgerTile>();
            for (int kind = 0; kind < load._kinds; kind++)
            {
                for (int f = 0; f < Hive.Finishes; f++)
                {
                    var finish = (BeeFinish)f;
                    int copies = count == null ? 0 : count(kind, finish);
                    if (copies > load._tiles.Length) copies = load._tiles.Length;
                    for (int c = 0; c < copies; c++) owned.Add(BadgerTile.Bee(kind, finish));
                }
            }
            owned.Sort(Better);
            for (int i = 0; i < load._tiles.Length; i++)
                load._tiles[i] = i < owned.Count ? owned[i] : BadgerTile.YardTile();
            return load;
        }

        static int Better(BadgerTile a, BadgerTile b)
        {
            int byHoney = b.Honey.CompareTo(a.Honey);
            if (byHoney != 0) return byHoney;
            int byKind = a.Kind.CompareTo(b.Kind);
            if (byKind != 0) return byKind;
            return ((int)a.Finish).CompareTo((int)b.Finish);
        }

        public int BeeTiles
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _tiles.Length; i++)
                    if (!_tiles[i].Yard) n++;
                return n;
            }
        }

        // Owned copies of this bee not already sitting on the grid.
        public int Free(int kind, BeeFinish finish)
        {
            int owned = _count == null ? 0 : _count(kind, finish);
            for (int i = 0; i < _tiles.Length; i++)
            {
                if (_tiles[i].Yard) continue;
                if (_tiles[i].Kind == kind && _tiles[i].Finish == finish) owned--;
            }
            return owned < 0 ? 0 : owned;
        }

        // Yard honey is always allowed. A bee needs a free copy, or to already be that slot's bee.
        public bool CanSet(int slot, BadgerTile tile)
        {
            if ((uint)slot >= (uint)_tiles.Length) return false;
            if (tile.Yard) return true;
            if ((uint)tile.Kind >= (uint)_kinds) return false;
            if (_tiles[slot].Same(tile)) return true;
            return Free(tile.Kind, tile.Finish) > 0;
        }

        public bool Set(int slot, BadgerTile tile)
        {
            if (!CanSet(slot, tile)) return false;
            _tiles[slot] = tile.Yard ? BadgerTile.YardTile() : BadgerTile.Bee(tile.Kind, tile.Finish);
            return true;
        }

        // Every bee this slot could hold (one entry per distinct kind and finish), best honey first.
        public void Options(int slot, List<BadgerTile> into)
        {
            into.Clear();
            if ((uint)slot >= (uint)_tiles.Length) return;
            for (int kind = 0; kind < _kinds; kind++)
            {
                for (int f = 0; f < Hive.Finishes; f++)
                {
                    var tile = BadgerTile.Bee(kind, (BeeFinish)f);
                    if (CanSet(slot, tile) && (_count == null ? 0 : _count(kind, tile.Finish)) > 0)
                        into.Add(tile);
                }
            }
            into.Sort(Better);
        }

        public int[] Honeys()
        {
            var honey = new int[_tiles.Length];
            for (int i = 0; i < _tiles.Length; i++) honey[i] = _tiles[i].Honey;
            return honey;
        }
    }
}
