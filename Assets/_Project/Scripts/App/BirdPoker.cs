using System.Collections.Generic;
using UnityEngine;

namespace FlockFive
{
    // Video-poker draw: 75 birds (5 colors × 3 looks × 5 copies) + 5 wilds.
    // Pay table is a house game: pair does not pay. Greedy-hold RTP sits ~95%.
    public static class BirdPoker
    {
        public const int HandSize = 5;
        public const int CopiesEach = 5;
        public const int WildCount = 5;
        public const int DeckSize = Palette.Max * 3 * CopiesEach + WildCount; // 80
        public const int LookKinds = Palette.Max * 3; // 15 bird looks
        public const int WildKind = LookKinds; // 16th: five wilds
        public const int PunchKinds = LookKinds + 1; // 16
        public const int FullcardPrize = 1000000;
        const string PrefPunch = "flockfive.poker.punch.v1";
        const string PrefFull = "flockfive.poker.fullcard.v1";

        static bool[] _punched;
        static bool _punchReady;

        public enum Phase { Idle, Dealt, Drawn }

        public struct Card
        {
            public bool Wild;
            public BirdColor Color;
            public BirdSex Sex;

            public static Card Of(BirdColor c, BirdSex s) => new Card { Color = c, Sex = s };
            public static Card MakeWild() => new Card { Wild = true };
            public Bird AsBird() => new Bird(Color, Sex);
        }

        public enum Rank
        {
            Nothing = 0,
            Pair = 1,
            TwoPair = 2,
            Trips = 3,
            FullHouse = 4,
            Quads = 5,
            FiveWild = 6,
            NaturalFive = 7
        }

        public const int FloorBet = 5;

        public static Phase PhaseNow { get; private set; }
        public static int Bet { get; private set; } = FloorBet;
        public static int MinBet { get; private set; } = FloorBet;
        public static int MaxBet { get; private set; } = FloorBet;
        public static int RecBet { get; private set; } = FloorBet;
        public static int SessionMax => _sessionMax;
        public static int LastWin { get; private set; }
        public static Rank LastRank { get; private set; }
        public static bool LastPunchFresh { get; private set; }
        public static bool LastPunchBingo { get; private set; }
        public static int LastPunchKind { get; private set; } = -1;
        public static int LastFullcardPay { get; private set; }
        public static int FullcardCount { get; private set; }
        public static readonly Card[] Hand = new Card[HandSize];
        public static readonly bool[] Hold = new bool[HandSize];

        static readonly List<Card> _deck = new List<Card>(DeckSize);
        static readonly List<Card> _shoe = new List<Card>(DeckSize);
        static readonly List<int> _ladder = new List<int>(32);
        static int _ladderCoins = -1;
        static int _sessionMax;
        static bool _betPicked;

        public static void Boot()
        {
            WarmPunch();
            if (_deck.Count == DeckSize) return;
            _deck.Clear();
            for (int c = 0; c < Palette.Max; c++)
                for (int s = 0; s < 3; s++)
                    for (int n = 0; n < CopiesEach; n++)
                        _deck.Add(Card.Of((BirdColor)c, (BirdSex)s));
            for (int w = 0; w < WildCount; w++)
                _deck.Add(Card.MakeWild());
            ResetRound();
            SyncBet();
        }

        public static void ResetRound()
        {
            PhaseNow = Phase.Idle;
            LastWin = 0;
            LastRank = Rank.Nothing;
            for (int i = 0; i < HandSize; i++)
            {
                Hand[i] = default;
                Hold[i] = false;
            }
        }

        public static void BeginVisit()
        {
            _sessionMax = 0;
            _betPicked = false;
            _ladderCoins = -1;
            ResetRound();
            SyncBet();
        }

        public static void RefreshBet()
        {
            _ladderCoins = -1;
            SyncBet();
        }

        public static bool CanDeal()
        {
            return PhaseNow == Phase.Idle
                && Purse.Coins >= FloorBet
                && Bet >= FloorBet
                && Bet <= Purse.Coins;
        }

        public static void SyncBet()
        {
            RebuildLadder();
            if (Purse.Coins < 1)
            {
                Bet = _ladder.Count > 0 ? _ladder[0] : 1;
                return;
            }
            if (!_betPicked)
            {
                Bet = RecBet;
                _betPicked = true;
                return;
            }
            int cap = Mathf.Min(MaxBet, Mathf.Max(MinBet, Purse.Coins));
            if (Bet > cap || Bet < MinBet || !_ladder.Contains(Bet))
                Bet = NearestBet(Mathf.Clamp(Bet, MinBet, cap));
        }

        public static bool NudgeBet(int dir)
        {
            if (PhaseNow != Phase.Idle) return false;
            RebuildLadder();
            _betPicked = true;
            int ix = 0;
            for (int i = 0; i < _ladder.Count; i++)
                if (_ladder[i] == Bet) { ix = i; break; }
            int n = ix + (dir < 0 ? -1 : 1);
            if (n < 0 || n >= _ladder.Count) return false;
            Bet = _ladder[n];
            return true;
        }

        public static bool CanNudge(int dir)
        {
            if (PhaseNow != Phase.Idle) return false;
            RebuildLadder();
            int ix = 0;
            for (int i = 0; i < _ladder.Count; i++)
                if (_ladder[i] == Bet) { ix = i; break; }
            int n = ix + (dir < 0 ? -1 : 1);
            return n >= 0 && n < _ladder.Count;
        }

        static int CeilTenth(int coins)
        {
            if (coins <= 0) return FloorBet;
            return Mathf.Max(FloorBet, (coins + 9) / 10);
        }

        static int RecRaw(int coins)
        {
            if (coins <= 0) return FloorBet;
            return Mathf.Max(FloorBet, (coins * 2 + 50) / 100);
        }

        // 5, 10, 25, 50, 100, 250, 500, then 1K 5K 10K 25K 50K 100K 250K 500K, 1M, 5M...
        static readonly List<int> _chips = new List<int>(40);

        static void EnsureChips()
        {
            if (_chips.Count > 0) return;
            int[] low = { 5, 10, 25, 50, 100, 250, 500 };
            for (int i = 0; i < low.Length; i++) _chips.Add(low[i]);
            int[] hi = { 1, 5, 10, 25, 50, 100, 250, 500 };
            long unit = 1000;
            for (int g = 0; g < 4; g++)
            {
                for (int i = 0; i < hi.Length; i++)
                {
                    long v = hi[i] * unit;
                    if (v > int.MaxValue) return;
                    int iv = (int)v;
                    if (_chips[_chips.Count - 1] == iv) continue;
                    _chips.Add(iv);
                }
                unit *= 1000;
                if (unit > int.MaxValue) return;
            }
        }

        static int RoundUpChip(int n)
        {
            EnsureChips();
            if (n <= _chips[0]) return _chips[0];
            for (int i = 0; i < _chips.Count; i++)
                if (_chips[i] >= n) return _chips[i];
            return _chips[_chips.Count - 1];
        }

        static int RoundDownChip(int n)
        {
            EnsureChips();
            int best = _chips[0];
            for (int i = 0; i < _chips.Count; i++)
            {
                if (_chips[i] > n) break;
                best = _chips[i];
            }
            return best;
        }

        static void RebuildLadder()
        {
            EnsureChips();
            int coins = Mathf.Max(0, Purse.Coins);
            int rawMax = CeilTenth(coins);
            int chipMax = RoundUpChip(rawMax);
            if (chipMax > _sessionMax) _sessionMax = chipMax;
            if (coins == _ladderCoins && _ladder.Count > 0) return;
            _ladderCoins = coins;
            _ladder.Clear();

            if (coins < 1)
            {
                MinBet = 1;
                MaxBet = 1;
                RecBet = 1;
                _ladder.Add(1);
                return;
            }
            if (coins < FloorBet)
            {
                MinBet = Mathf.Max(1, coins);
                MaxBet = MinBet;
                RecBet = MinBet;
                _ladder.Add(MinBet);
                return;
            }

            MinBet = FloorBet;
            int afford = Mathf.Min(coins, _sessionMax);
            MaxBet = RoundDownChip(afford);
            if (MaxBet < MinBet) MaxBet = MinBet;
            for (int i = 0; i < _chips.Count; i++)
            {
                int v = _chips[i];
                if (v < MinBet) continue;
                if (v > MaxBet) break;
                _ladder.Add(v);
            }
            if (_ladder.Count == 0) _ladder.Add(MinBet);

            RecBet = NearestBet(RecRaw(coins));
            if (RecBet < MinBet) RecBet = MinBet;
            if (RecBet > MaxBet) RecBet = MaxBet;
        }

        static int NearestBet(int want)
        {
            if (_ladder.Count == 0) return Mathf.Max(1, want);
            int best = _ladder[0];
            int bestD = Mathf.Abs(best - want);
            for (int i = 1; i < _ladder.Count; i++)
            {
                int d = Mathf.Abs(_ladder[i] - want);
                if (d < bestD || (d == bestD && _ladder[i] <= want))
                {
                    best = _ladder[i];
                    bestD = d;
                }
            }
            return best;
        }

        public static bool Deal()
        {
            if (PhaseNow != Phase.Idle) return false;
            if (Purse.Coins < FloorBet || Bet < FloorBet) return false;
            if (!Purse.TrySpend(Bet)) return false;
            ShuffleShoe();
            for (int i = 0; i < HandSize; i++)
            {
                Hand[i] = DrawOne();
                Hold[i] = false;
            }
            SortShow();
            PhaseNow = Phase.Dealt;
            LastWin = 0;
            LastRank = Rank.Nothing;
            return true;
        }

        public static void ToggleHold(int i)
        {
            if (PhaseNow != Phase.Dealt) return;
            if (i < 0 || i >= HandSize) return;
            Hold[i] = !Hold[i];
        }

        public static bool Draw()
        {
            if (PhaseNow != Phase.Dealt) return false;
            for (int i = 0; i < HandSize; i++)
                if (!Hold[i]) Hand[i] = DrawOne();
            LastRank = Evaluate(Hand, out bool natural);
            LastWin = PayFor(LastRank, Bet);
            if (LastWin > 0) Purse.Credit(LastWin);
            RefreshBet();
            LastPunchFresh = false;
            LastPunchBingo = false;
            LastPunchKind = -1;
            if (LastRank == Rank.NaturalFive || LastRank == Rank.FiveWild)
            {
                int kind;
                LastPunchFresh = TryPunch(Hand, out kind);
                if (LastPunchFresh)
                {
                    LastPunchKind = kind;
                    LastPunchBingo = PunchFound() == PunchKinds;
                    if (LastPunchBingo)
                    {
                        LastFullcardPay = FullcardPrize;
                        Purse.Credit(FullcardPrize);
                        FullcardCount++;
                        SaveFullcard();
                    }
                    else LastFullcardPay = 0;
                }
            }
            PhaseNow = Phase.Drawn;
            return true;
        }

        public static void Collect()
        {
            if (PhaseNow != Phase.Drawn) return;
            int win = LastWin;
            var rank = LastRank;
            ResetRound();
            LastWin = win;
            LastRank = rank;
            RefreshBet();
        }

        // Paying ranks high→low — single source for UI pay table + PayFor.
        public static readonly Rank[] PayTableRows =
        {
            Rank.NaturalFive,
            Rank.FiveWild,
            Rank.Quads,
            Rank.FullHouse,
            Rank.Trips,
            Rank.TwoPair
        };

        public static string RankLabel(Rank r)
        {
            switch (r)
            {
                case Rank.NaturalFive: return "NATURAL FIVE";
                case Rank.FiveWild: return "FIVE OF A KIND";
                case Rank.Quads: return "FOUR OF A KIND";
                case Rank.FullHouse: return "FULL HOUSE";
                case Rank.Trips: return "THREE OF A KIND";
                case Rank.TwoPair: return "TWO PAIR";
                case Rank.Pair: return "PAIR";
                default: return "";
            }
        }

        // Bet multiplier for a rank (0 = no pay). Shared by PayFor and the pay-table UI.
        public static int Multiplier(Rank r)
        {
            switch (r)
            {
                case Rank.NaturalFive: return 150;
                case Rank.FiveWild: return 30;
                case Rank.Quads: return 10;
                case Rank.FullHouse: return 6;
                case Rank.Trips: return 1;
                case Rank.TwoPair: return 1;
                default: return 0;
            }
        }

        static int PayFor(Rank r, int bet) => bet * Multiplier(r);

        // Cluster matches for the fan: color then look, wilds on the right.
        public static void SortShow()
        {
            for (int i = 1; i < HandSize; i++)
            {
                var x = Hand[i];
                int j = i;
                while (j > 0 && ShowKey(Hand[j - 1]) > ShowKey(x))
                {
                    Hand[j] = Hand[j - 1];
                    j--;
                }
                Hand[j] = x;
            }
        }

        static int ShowKey(Card c)
        {
            if (c.Wild) return 1000;
            return (int)c.Color * 3 + (int)c.Sex;
        }

        public static int PunchFound()
        {
            WarmPunch();
            int n = 0;
            for (int i = 0; i < _punched.Length; i++)
                if (_punched[i]) n++;
            return n;
        }

        public static bool IsPunched(int kind)
        {
            WarmPunch();
            if ((uint)kind >= (uint)_punched.Length) return false;
            return _punched[kind];
        }

        public static int KindId(BirdColor c, BirdSex s) => (int)c * 3 + (int)s;

        public static bool IsWildKind(int kind) => kind == WildKind;

        public static void KindParts(int kind, out BirdColor c, out BirdSex s)
        {
            if (kind == WildKind)
            {
                c = BirdColor.Gold;
                s = BirdSex.Neutral;
                return;
            }
            c = (BirdColor)(kind / 3);
            s = (BirdSex)(kind % 3);
        }

        static void WarmPunch()
        {
            if (_punchReady && _punched != null && _punched.Length == PunchKinds) return;
            FullcardCount = Mathf.Max(0, PrefGuard.GetInt(PrefFull, 0));
            var prev = _punched;
            _punched = new bool[PunchKinds];
            if (prev != null)
            {
                int n = Mathf.Min(prev.Length, _punched.Length);
                for (int i = 0; i < n; i++) _punched[i] = prev[i];
                _punchReady = true;
                return;
            }
            string raw = PrefGuard.GetString(PrefPunch, "");
            if (!string.IsNullOrEmpty(raw))
            {
                var parts = raw.Split(',');
                for (int i = 0; i < parts.Length; i++)
                {
                    int k;
                    if (int.TryParse(parts[i], out k) && (uint)k < (uint)_punched.Length)
                        _punched[k] = true;
                }
            }
            _punchReady = true;
        }

        static void SavePunch()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < _punched.Length; i++)
            {
                if (!_punched[i]) continue;
                if (sb.Length > 0) sb.Append(',');
                sb.Append(i);
            }
            PrefGuard.SetString(PrefPunch, sb.ToString());
            PlayerPrefs.Save();
        }

        static void SaveFullcard()
        {
            PrefGuard.SetInt(PrefFull, FullcardCount);
            PlayerPrefs.Save();
        }

#if UNITY_EDITOR
        public static void RestoreFullcardCount(int n)
        {
            FullcardCount = Mathf.Max(0, n);
            SaveFullcard();
        }
#endif

        static bool TryPunch(Card[] hand, out int kind)
        {
            WarmPunch();
            kind = -1;
            if (!ResolveFiveKind(hand, out kind)) return false;
            if (_punched[kind]) return false;
            _punched[kind] = true;
            SavePunch();
            return true;
        }

        // Which bird look the five-of-a-kind landed on (wilds fill that look).
        static bool ResolveFiveKind(Card[] hand, out int kind)
        {
            kind = -1;
            int wilds = 0;
            for (int i = 0; i < hand.Length; i++)
                if (hand[i].Wild) wilds++;
            if (wilds >= HandSize)
            {
                kind = WildKind;
                return true;
            }
            var counts = new int[LookKinds];
            for (int i = 0; i < hand.Length; i++)
            {
                if (hand[i].Wild) continue;
                counts[KindId(hand[i].Color, hand[i].Sex)]++;
            }
            int best = 0;
            int bestId = -1;
            for (int i = 0; i < counts.Length; i++)
            {
                if (counts[i] > best)
                {
                    best = counts[i];
                    bestId = i;
                }
            }
            if (bestId < 0) return false;
            if (best + wilds < 5) return false;
            kind = bestId;
            return true;
        }

        public static Rank Evaluate(Card[] hand, out bool naturalFive)
        {
            naturalFive = false;
            int wilds = 0;
            var counts = new int[Palette.Max * 3];
            for (int i = 0; i < hand.Length; i++)
            {
                if (hand[i].Wild) { wilds++; continue; }
                int id = (int)hand[i].Color * 3 + (int)hand[i].Sex;
                counts[id]++;
            }
            int best = 0;
            int second = 0;
            int kinds = 0;
            for (int i = 0; i < counts.Length; i++)
            {
                int n = counts[i];
                if (n <= 0) continue;
                kinds++;
                if (n > best) { second = best; best = n; }
                else if (n > second) second = n;
            }
            int top = best + wilds;
            if (top >= 5)
            {
                if (wilds == 0 && best == 5)
                {
                    naturalFive = true;
                    return Rank.NaturalFive;
                }
                return Rank.FiveWild;
            }
            if (top >= 4) return Rank.Quads;
            // Full house: three + two, wilds fill the hole.
            if (best >= 3 && second >= 2) return Rank.FullHouse;
            if (best == 2 && second == 2 && wilds >= 1) return Rank.FullHouse;
            if (best == 3 && second == 1 && wilds >= 1) return Rank.FullHouse;
            if (best == 2 && second == 1 && wilds >= 2) return Rank.FullHouse;
            if (top >= 3) return Rank.Trips;
            if (best >= 2 && second >= 2) return Rank.TwoPair;
            if (best == 2 && wilds >= 1 && second >= 1) return Rank.TwoPair;
            if (top >= 2) return Rank.Pair;
            return Rank.Nothing;
        }

        static void ShuffleShoe()
        {
            _shoe.Clear();
            _shoe.AddRange(_deck);
            for (int i = _shoe.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var tmp = _shoe[i];
                _shoe[i] = _shoe[j];
                _shoe[j] = tmp;
            }
        }

        static Card DrawOne()
        {
            if (_shoe.Count == 0) ShuffleShoe();
            if (_shoe.Count == 0) return default;
            int last = _shoe.Count - 1;
            var c = _shoe[last];
            _shoe.RemoveAt(last);
            return c;
        }

#if UNITY_EDITOR
        public static void SeedRng(int seed) => Random.InitState(seed);

        public static void ClearPunches()
        {
            WarmPunch();
            for (int i = 0; i < _punched.Length; i++) _punched[i] = false;
            SavePunch();
        }

        public static void FillPunchesExcept(int kind)
        {
            WarmPunch();
            for (int i = 0; i < _punched.Length; i++) _punched[i] = i != kind;
            SavePunch();
        }

        public static void HoldForKind(int kind)
        {
            if (PhaseNow != Phase.Dealt) return;
            if (kind == WildKind)
            {
                for (int i = 0; i < HandSize; i++)
                    Hold[i] = Hand[i].Wild;
                return;
            }
            KindParts(kind, out BirdColor c, out BirdSex s);
            for (int i = 0; i < HandSize; i++)
            {
                if (Hand[i].Wild) { Hold[i] = true; continue; }
                Hold[i] = Hand[i].Color == c && Hand[i].Sex == s;
            }
        }

        public static void ForceFiveWilds()
        {
            if (PhaseNow == Phase.Idle)
            {
                if (!Purse.TrySpend(Mathf.Min(Bet, Mathf.Max(1, Purse.Coins)))) return;
            }
            for (int i = 0; i < HandSize; i++)
            {
                Hand[i] = Card.MakeWild();
                Hold[i] = true;
            }
            LastRank = Rank.FiveWild;
            LastWin = PayFor(LastRank, Bet);
            if (LastWin > 0) Purse.Credit(LastWin);
            RefreshBet();
            LastPunchFresh = TryPunch(Hand, out int kind);
            LastPunchKind = kind;
            LastPunchBingo = false;
            LastFullcardPay = 0;
            if (LastPunchFresh)
            {
                LastPunchBingo = PunchFound() == PunchKinds;
                if (LastPunchBingo)
                {
                    LastFullcardPay = FullcardPrize;
                    Purse.Credit(FullcardPrize);
                    FullcardCount++;
                    SaveFullcard();
                }
            }
            PhaseNow = Phase.Drawn;
        }
#endif
    }
}

