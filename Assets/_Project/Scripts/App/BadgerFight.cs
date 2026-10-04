using System;

namespace FlockFive
{
    public enum BadgerResult
    {
        Playing = 0,
        PlayerWon = 1,
        BadgerWon = 2,
    }

    public struct BadgerRound
    {
        public int BossIndex;
        public int PlayerIndex;
        public int BossHoney;
        public int PlayerHoney;
        public int BossFinal;
        public int PlayerFinal;
        public int BossGained;
        public int PlayerGained;
        public BadgerPower Power;
        public BadgerResult Result;
    }

    // One honey-badger contest. Each side has its own grid (4x4 in a real visit).
    // The badger always picks first, at random from the tiles still open.
    // Higher final honey scores that honey. A tie scores nothing.
    // The only side effect is Purse.TrySpend when a power-up is bought.
    public sealed class BadgerFight
    {
        readonly int _appearance;
        readonly int[] _player;
        readonly int[] _boss;
        readonly bool[] _playerOpen;
        readonly bool[] _bossOpen;
        readonly bool[] _usedPower = new bool[5];
        readonly Random _rng;

        int _playerLeft;
        int _bossLeft;
        int _bossPick = -1;
        int _playerPick = -1;
        BadgerPower _armed = BadgerPower.None;

        public int Appearance => _appearance;
        public int PlayerScore { get; private set; }
        public int BossScore { get; private set; }
        public BadgerResult Result { get; private set; }
        public int PlayerTarget => BadgerSchedule.PlayerTarget(_appearance);
        public int BadgerTarget => BadgerSchedule.BadgerTarget(_appearance);
        public int PlayerCount => _player.Length;
        public int BossCount => _boss.Length;
        public int PlayerLeft => _playerLeft;
        public int BossLeft => _bossLeft;
        public BadgerPower Armed => _armed;

        // Null grids deal from the album and BadgerSchedule. Tests pass both.
        public BadgerFight(int appearance, int seed, int[] playerTiles, int[] bossTiles)
        {
            _appearance = appearance < 1 ? 1 : appearance;
            _rng = new Random(seed);
            if (playerTiles == null) playerTiles = BadgerSchedule.PlayerLoadout(BadgerSchedule.Tiles);
            if (bossTiles == null) bossTiles = BadgerSchedule.BossTileMix(_appearance, seed);
            _player = Copy(playerTiles);
            _boss = Copy(bossTiles);
            _playerOpen = AllOpen(_player.Length);
            _bossOpen = AllOpen(_boss.Length);
            _playerLeft = _player.Length;
            _bossLeft = _boss.Length;
            Result = Decide(0, 0, PlayerTarget, BadgerTarget, _playerLeft > 0 && _bossLeft > 0);
        }

        // Caller checks BadgerSchedule.Due first. Before level 15, Appearance is 0
        // and this still deals visit 1.
        public static BadgerFight Start(int cleared, int seed)
        {
            int n = BadgerSchedule.Appearance(cleared);
            if (n < 1) n = 1;
            return new BadgerFight(n, seed, null, null);
        }

        public bool PlayerOpen(int index)
        {
            return (uint)index < (uint)_playerOpen.Length && _playerOpen[index];
        }

        public bool BossOpen(int index)
        {
            return (uint)index < (uint)_bossOpen.Length && _bossOpen[index];
        }

        public int PlayerHoney(int index)
        {
            if ((uint)index >= (uint)_player.Length) return 0;
            return _player[index];
        }

        public int BossHoney(int index)
        {
            if ((uint)index >= (uint)_boss.Length) return 0;
            return _boss[index];
        }

        public bool PowerReady(BadgerPower power)
        {
            if (BadgerSchedule.BasePrice(power) <= 0) return false;
            return !Used(power);
        }

        // Random open badger tile. -1 if the contest is over, the badger already
        // picked this round, or that grid has nothing left.
        public int BossPick()
        {
            if (Result != BadgerResult.Playing) return -1;
            if (_bossPick >= 0) return -1;
            if (_bossLeft <= 0 || _playerLeft <= 0)
            {
                Result = Decide(PlayerScore, BossScore, PlayerTarget, BadgerTarget, false);
                return -1;
            }
            int ix = NextOpen(_bossOpen, _bossLeft);
            if (ix < 0) return -1;
            _bossOpen[ix] = false;
            _bossLeft--;
            _bossPick = ix;
            return ix;
        }

        // False if the badger has not picked yet, or the tile is closed. No change then.
        public bool PlayerPick(int index)
        {
            if (Result != BadgerResult.Playing) return false;
            if (_bossPick < 0) return false;
            if (_playerPick >= 0) return false;
            if ((uint)index >= (uint)_playerOpen.Length) return false;
            if (!_playerOpen[index]) return false;
            _playerOpen[index] = false;
            _playerLeft--;
            _playerPick = index;
            return true;
        }

        public int PriceOf(BadgerPower power)
        {
            return BadgerSchedule.PowerUpPrice(power, _appearance);
        }

        // Unused this contest and the purse covers this visit's price.
        public bool CanAfford(BadgerPower power)
        {
            if (!PowerReady(power)) return false;
            return Purse.Coins >= PriceOf(power);
        }

        // The player's whole turn in one call: buy the power-up (if any) and pick the tile.
        // A short purse, a used power, or a closed tile changes nothing, so the view can
        // shake the tile and let the player try again.
        public bool PlayerPlay(int index, BadgerPower power)
        {
            if (Result != BadgerResult.Playing) return false;
            if (_bossPick < 0 || _playerPick >= 0) return false;
            if ((uint)index >= (uint)_playerOpen.Length || !_playerOpen[index]) return false;
            if (power != BadgerPower.None && !CanAfford(power)) return false;
            if (!PlayerPick(index)) return false;
            if (power == BadgerPower.None) return true;
            if (TryPower(power)) return true;
            _playerOpen[index] = true;
            _playerLeft++;
            _playerPick = -1;
            return false;
        }

        // One power per round, one use per contest. A short purse returns false
        // and leaves coins, the armed power, and the used flags alone.
        public bool TryPower(BadgerPower power)
        {
            if (Result != BadgerResult.Playing) return false;
            if (_bossPick < 0 || _playerPick < 0) return false;
            if (_armed != BadgerPower.None) return false;
            if (Used(power)) return false;
            int price = BadgerSchedule.PowerUpPrice(power, _appearance);
            if (price <= 0) return false;
            if (!Purse.TrySpend(price)) return false;
            Mark(power);
            _armed = power;
            return true;
        }

        public bool Resolve(out BadgerRound round)
        {
            round = default;
            if (Result != BadgerResult.Playing) return false;
            if (_bossPick < 0 || _playerPick < 0) return false;

            int bossHoney = _boss[_bossPick];
            int playerHoney = _player[_playerPick];
            BadgerPower power = _armed;
            ApplyRound(playerHoney, bossHoney, power,
                out int playerFinal, out int bossFinal, out int playerGain, out int bossGain);

            int bossIndex = _bossPick;
            int playerIndex = _playerPick;
            _bossPick = -1;
            _playerPick = -1;
            _armed = BadgerPower.None;

            PlayerScore += playerGain;
            BossScore += bossGain;
            bool remain = _playerLeft > 0 && _bossLeft > 0;
            Result = Decide(PlayerScore, BossScore, PlayerTarget, BadgerTarget, remain);

            round.BossIndex = bossIndex;
            round.PlayerIndex = playerIndex;
            round.BossHoney = bossHoney;
            round.PlayerHoney = playerHoney;
            round.BossFinal = bossFinal;
            round.PlayerFinal = playerFinal;
            round.BossGained = bossGain;
            round.PlayerGained = playerGain;
            round.Power = power;
            round.Result = Result;
            return true;
        }

        // X2 and X3 scale the player's honey this round. Hot Sauce and Pepper
        // are the same comic knock-back: the badger's honey counts as 0, so
        // the badger scores 0 and the tile is already spent.
        public static void ApplyRound(int playerHoney, int bossHoney, BadgerPower power,
            out int playerFinal, out int bossFinal, out int playerGain, out int bossGain)
        {
            playerFinal = playerHoney * MultiplierOf(power);
            bossFinal = IsBlock(power) ? 0 : bossHoney;
            playerGain = 0;
            bossGain = 0;
            if (playerFinal > bossFinal) playerGain = playerFinal;
            else if (bossFinal > playerFinal) bossGain = bossFinal;
        }

        public static int MultiplierOf(BadgerPower power)
        {
            if (power == BadgerPower.X2) return 2;
            if (power == BadgerPower.X3) return 3;
            return 1;
        }

        public static bool IsBlock(BadgerPower power)
        {
            return power == BadgerPower.HotSauce || power == BadgerPower.Pepper;
        }

        // Target first. If the grids cannot deal another round, the higher
        // score wins. An exact tie goes to the badger.
        public static BadgerResult Decide(int playerScore, int bossScore, int playerTarget, int badgerTarget, bool tilesRemain)
        {
            if (playerScore >= playerTarget) return BadgerResult.PlayerWon;
            if (bossScore >= badgerTarget) return BadgerResult.BadgerWon;
            if (tilesRemain) return BadgerResult.Playing;
            if (playerScore > bossScore) return BadgerResult.PlayerWon;
            return BadgerResult.BadgerWon;
        }

        int NextOpen(bool[] open, int left)
        {
            int roll = _rng.Next(left);
            int seen = 0;
            for (int i = 0; i < open.Length; i++)
            {
                if (!open[i]) continue;
                if (seen == roll) return i;
                seen++;
            }
            return -1;
        }

        bool Used(BadgerPower power)
        {
            int ix = (int)power;
            if ((uint)ix >= (uint)_usedPower.Length) return true;
            return _usedPower[ix];
        }

        void Mark(BadgerPower power)
        {
            int ix = (int)power;
            if ((uint)ix >= (uint)_usedPower.Length) return;
            _usedPower[ix] = true;
        }

        static int[] Copy(int[] src)
        {
            if (src == null) src = new int[0];
            var dst = new int[src.Length];
            for (int i = 0; i < src.Length; i++) dst[i] = src[i];
            return dst;
        }

        static bool[] AllOpen(int n)
        {
            var open = new bool[n];
            for (int i = 0; i < n; i++) open[i] = true;
            return open;
        }
    }
}
