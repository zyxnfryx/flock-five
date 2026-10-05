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
        // "Honey Badger Don't Care" slam landed this round (see BadgerFight.TryDontCare).
        public bool DontCare;
        // Power-up the slam destroyed (coins stay spent, no effect). None if none.
        public BadgerPower Destroyed;
        // No power-up this round: the player's final honey was halved for the compare.
        public bool Halved;
    }

    // One TryDontCare call. Eligible false: first move or the round right after a hit
    // (no roll, chance unchanged). Roll is the 0..99 draw; Hit is Roll < ChanceBefore.
    public struct BadgerDontCare
    {
        public bool Eligible;
        public bool Hit;
        // Coach-fight move 3 (spec 2b tutorial force): hit without a roll.
        public bool Forced;
        public int Roll;
        public int ChanceBefore;
        public int ChanceAfter;
        public BadgerPower Destroyed;
        public bool Halved;
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
        // Separate stream for the slam so boss picks stay the same for a given seed.
        readonly Random _slamRng;

        int _playerLeft;
        int _bossLeft;
        int _bossPick = -1;
        int _playerPick = -1;
        BadgerPower _armed = BadgerPower.None;
        // Rounds left where the badger still scores 0 (from Freeze Spray leftover).
        int _bossSkipLeft;

        // "Honey Badger Don't Care" (spec 2b). This contest only; a retry is a new fight at 10%.
        public const int DontCareBaseChance = 10;
        public const int DontCareStep = 5;
        int _dontCareChance = DontCareBaseChance;
        bool _dontCareLastHit;
        int _roundsResolved;
        bool _dontCareRolled;
        bool _dontCareHalve;
        BadgerPower _dontCareDestroyed = BadgerPower.None;
        bool _dontCareHit;
        // First-fight tutorial force (spec 2b): the coach fight only, appearance 1.
        public const int DontCareForcedMove = 3;
        readonly bool _coachFight;
        bool _dontCareForceSpent;
        // Coach fight only: scripted picks for moves 1-3 (BadgerSchedule.cs).
        readonly BadgerTutorialScript _script;
        int _tutorialCaps;

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
        public int BossSkipLeft => _bossSkipLeft;
        public int DontCareChance => _dontCareChance;
        public bool DontCareLastHit => _dontCareLastHit;
        public int RoundsResolved => _roundsResolved;
        public bool CoachFight => _coachFight;
        public BadgerTutorialScript Script => _script;

        // 1..3 while the coach script guides this round's picks; 0 otherwise.
        public int TutorialMove =>
            _script != null && _roundsResolved < BadgerTutorialScript.Moves ? _roundsResolved + 1 : 0;

        // The glove's tile this round (-1 when unguided). Only this tile can be played.
        public int GuidedTile => TutorialMove > 0 ? _script.PlayerPick(TutorialMove) : -1;

        // Coach fight: no power-ups until move 3 has resolved (slam included).
        public bool PowersLocked =>
            _script != null && _roundsResolved < BadgerTutorialScript.Moves;

        // Times the move 1-3 column cap had to trim a gain (0 for any real deal; tests check).
        public int TutorialCaps => _tutorialCaps;

        // Null grids deal from the album and BadgerSchedule. Tests pass both.
        public BadgerFight(int appearance, int seed, int[] playerTiles, int[] bossTiles)
            : this(appearance, seed, playerTiles, bossTiles, false)
        {
        }

        // coachFight: this contest ran the first-fight lesson. Only counts on appearance 1;
        // it turns on the move-3 Don't Care force.
        public BadgerFight(int appearance, int seed, int[] playerTiles, int[] bossTiles, bool coachFight)
        {
            _appearance = appearance < 1 ? 1 : appearance;
            _coachFight = coachFight && _appearance <= 1;
            _rng = new Random(seed);
            _slamRng = new Random(unchecked(seed * 486187739 + 0x5EED));
            if (playerTiles == null) playerTiles = BadgerSchedule.PlayerLoadout(BadgerSchedule.Tiles);
            if (bossTiles == null) bossTiles = BadgerSchedule.BossTileMix(_appearance, seed);
            _player = Copy(playerTiles);
            _boss = Copy(bossTiles);
            _playerOpen = AllOpen(_player.Length);
            _bossOpen = AllOpen(_boss.Length);
            _playerLeft = _player.Length;
            _bossLeft = _boss.Length;
            if (_coachFight)
                _script = BadgerTutorialScript.For(_player, _boss, PlayerTarget, BadgerTarget);
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
            int ix = -1;
            int move = TutorialMove;
            if (move > 0)
            {
                int scripted = _script.BossPick(move);
                if (BossOpen(scripted)) ix = scripted;
            }
            if (ix < 0) ix = NextOpen(_bossOpen, _bossLeft);
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
            bool free = BadgerSave.IsTutorialFirstUse(power, _appearance);
            return BadgerSchedule.PriceFor(power, _appearance, free);
        }

        // Unused this contest and the purse covers this visit's price.
        public bool CanAfford(BadgerPower power)
        {
            if (PowersLocked) return false;
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
            // Coach moves 1-3: only the glove's tile, and no power-up.
            int guided = GuidedTile;
            if (guided >= 0 && index != guided) return false;
            if (power != BadgerPower.None && PowersLocked) return false;
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
            if (PowersLocked) return false;
            if (Used(power)) return false;
            if (BadgerSchedule.BasePrice(power) <= 0) return false;
            bool tutFree = BadgerSave.IsTutorialFirstUse(power, _appearance);
            int price = BadgerSchedule.PriceFor(power, _appearance, tutFree);
            if (price > 0)
            {
                if (!Purse.TrySpend(price)) return false;
            }
            else if (!tutFree)
            {
                return false;
            }
            if (tutFree) BadgerSave.ClaimTutorialFree(power);
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
            // A slam already cleared _armed if it destroyed the power-up.
            BadgerPower power = _armed;
            bool pendingSkip = _bossSkipLeft > 0;
            bool halve = _dontCareHalve;
            ApplyRound(playerHoney, bossHoney, power, pendingSkip, halve,
                out int playerFinal, out int bossFinal, out int playerGain, out int bossGain);
            int skips = SkipTurnsOf(power);
            if (skips > 0) _bossSkipLeft = skips - 1;
            else if (pendingSkip) _bossSkipLeft--;

            int bossIndex = _bossPick;
            int playerIndex = _playerPick;
            _bossPick = -1;
            _playerPick = -1;
            _armed = BadgerPower.None;
            bool slam = _dontCareHit;
            BadgerPower destroyed = _dontCareDestroyed;
            // Coach moves 1-3: neither column may reach its target (spec 2b guidance).
            if (TutorialMove > 0)
            {
                int pRoom = PlayerTarget - 1 - PlayerScore;
                int bRoom = BadgerTarget - 1 - BossScore;
                if (pRoom < 0) pRoom = 0;
                if (bRoom < 0) bRoom = 0;
                if (playerGain > pRoom) { playerGain = pRoom; _tutorialCaps++; }
                if (bossGain > bRoom) { bossGain = bRoom; _tutorialCaps++; }
            }
            _roundsResolved++;
            _dontCareLastHit = slam;
            _dontCareRolled = false;
            _dontCareHit = false;
            _dontCareHalve = false;
            _dontCareDestroyed = BadgerPower.None;

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
            round.DontCare = slam;
            round.Destroyed = destroyed;
            round.Halved = halve;
            return true;
        }

        // ---- "Honey Badger Don't Care" (spec 2b) ----

        // Roll + apply, once per round, after both picks and before Resolve. Uses the
        // fight's own slam stream. Returns true on a hit.
        public bool TryDontCare(out BadgerDontCare slam)
        {
            return TryDontCare(_slamRng.Next, out slam);
        }

        // `next(100)` must return 0..99 (System.Random.Next shape); tests inject it.
        // Not eligible (no roll, chance unchanged): first move of the fight, the round right
        // after a hit, before both picks, a second call this round, or a decided contest.
        // Miss: chance +5. Hit: chance back to 10, then for this round only either the armed
        // power-up is destroyed (coins stay spent, its used flag stays set, no multiply /
        // block / Freeze carry) or, with no power-up, the player's honey is halved for the
        // compare. Totals already scored are never touched.
        public bool TryDontCare(Func<int, int> next, out BadgerDontCare slam)
        {
            slam = default;
            slam.ChanceBefore = _dontCareChance;
            slam.ChanceAfter = _dontCareChance;
            if (Result != BadgerResult.Playing) return false;
            if (_bossPick < 0 || _playerPick < 0) return false;
            if (_dontCareRolled) return false;
            _dontCareRolled = true;
            if (!DontCareEligible(_roundsResolved, _dontCareLastHit)) return false;
            // Coach fight: no roll at all before the forced move (chance unchanged).
            if (DontCareCoachHold(_coachFight, _roundsResolved, _dontCareForceSpent)) return false;
            slam.Eligible = true;
            if (DontCareForced(_coachFight, _roundsResolved, _dontCareLastHit, _dontCareForceSpent))
            {
                // Tutorial force: no roll is drawn; the hit resets the chance like any hit.
                slam.Forced = true;
                slam.Hit = true;
            }
            else
            {
                int roll = next != null ? next(100) : 99;
                if (roll < 0) roll = 0;
                if (roll > 99) roll = 99;
                slam.Roll = roll;
                slam.Hit = roll < _dontCareChance;
            }
            _dontCareChance = DontCareChanceAfter(_dontCareChance, slam.Hit);
            slam.ChanceAfter = _dontCareChance;
            if (!slam.Hit) return false;
            // Any hit in the coach fight means the player has seen the slam: no force after it.
            _dontCareForceSpent = true;
            _dontCareHit = true;
            if (_armed != BadgerPower.None)
            {
                _dontCareDestroyed = _armed;
                _armed = BadgerPower.None;
                slam.Destroyed = _dontCareDestroyed;
            }
            else
            {
                _dontCareHalve = true;
                slam.Halved = true;
            }
            return true;
        }

        // Coach fight, moves before the forced one (1 and 2): no roll, chance unchanged.
        public static bool DontCareCoachHold(bool coachFight, int roundsResolved, bool forceSpent)
        {
            if (!coachFight || forceSpent) return false;
            return roundsResolved + 1 < DontCareForcedMove;
        }

        // Coach fight, move 3 (round 3, after both picks; roundsResolved == 2). Moves 1-2 never
        // roll in the coach fight, so this always lands; normal rolls start after it (move 4
        // is still skipped by never-twice).
        public static bool DontCareForced(bool coachFight, int roundsResolved, bool lastHit, bool forceSpent)
        {
            if (!coachFight || forceSpent || lastHit) return false;
            return roundsResolved + 1 == DontCareForcedMove;
        }

        // Never on the first move; never twice in a row.
        public static bool DontCareEligible(int roundsResolved, bool lastHit)
        {
            return roundsResolved >= 1 && !lastHit;
        }

        // Miss: +5 (capped at 100). Hit: back to the 10% base.
        public static int DontCareChanceAfter(int chance, bool hit)
        {
            if (hit) return DontCareBaseChance;
            int c = chance + DontCareStep;
            return c > 100 ? 100 : c;
        }

        // Integer halving for the compare, floor 0.
        public static int HalveHoney(int honey)
        {
            if (honey <= 0) return 0;
            return honey / 2;
        }

        // X2 and X3 scale the player's honey this round. Hot Sauce and Freeze
        // Spray share one knock-back path: the badger's honey counts as 0 so
        // it scores nothing and its tile is already spent. SkipTurnsOf says
        // how many boss turns that lasts (1 for sauce, 2 for freeze).
        public static void ApplyRound(int playerHoney, int bossHoney, BadgerPower power,
            out int playerFinal, out int bossFinal, out int playerGain, out int bossGain)
        {
            ApplyRound(playerHoney, bossHoney, power, false,
                out playerFinal, out bossFinal, out playerGain, out bossGain);
        }

        public static void ApplyRound(int playerHoney, int bossHoney, BadgerPower power, bool forceSkip,
            out int playerFinal, out int bossFinal, out int playerGain, out int bossGain)
        {
            ApplyRound(playerHoney, bossHoney, power, forceSkip, false,
                out playerFinal, out bossFinal, out playerGain, out bossGain);
        }

        // halvePlayer: the Don't Care debuff (no power-up that round) halves the player's
        // final honey for this compare only.
        public static void ApplyRound(int playerHoney, int bossHoney, BadgerPower power, bool forceSkip, bool halvePlayer,
            out int playerFinal, out int bossFinal, out int playerGain, out int bossGain)
        {
            playerFinal = playerHoney * MultiplierOf(power);
            if (halvePlayer) playerFinal = HalveHoney(playerFinal);
            bossFinal = (forceSkip || IsBlock(power)) ? 0 : bossHoney;
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

        // Boss turns skipped when this power is spent (current round counts as one).
        public static int SkipTurnsOf(BadgerPower power)
        {
            if (power == BadgerPower.HotSauce) return 1;
            if (power == BadgerPower.FreezeSpray) return 2;
            return 0;
        }

        public static bool IsBlock(BadgerPower power)
        {
            return SkipTurnsOf(power) > 0;
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
