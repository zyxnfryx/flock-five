using System.Collections.Generic;
using UnityEngine;

namespace FlockFive
{
    // Video-poker draw: 75 birds (5×15 looks) + 5 wilds. Jackpot = natural five-of-a-kind.
    public static class BirdPoker
    {
        public const int HandSize = 5;
        public const int CopiesEach = 5;
        public const int WildCount = 5;
        public const int DeckSize = Palette.Max * 3 * CopiesEach + WildCount; // 80

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

        public static Phase PhaseNow { get; private set; }
        public static int Bet { get; private set; } = 5;
        public static int LastWin { get; private set; }
        public static Rank LastRank { get; private set; }
        public static readonly Card[] Hand = new Card[HandSize];
        public static readonly bool[] Hold = new bool[HandSize];

        static readonly List<Card> _deck = new List<Card>(DeckSize);
        static readonly List<Card> _shoe = new List<Card>(DeckSize);
        static readonly int[] _bets = { 1, 5, 10, 25 };

        public static IReadOnlyList<int> Bets => _bets;

        public static void Boot()
        {
            if (_deck.Count == DeckSize) return;
            _deck.Clear();
            for (int c = 0; c < Palette.Max; c++)
                for (int s = 0; s < 3; s++)
                    for (int n = 0; n < CopiesEach; n++)
                        _deck.Add(Card.Of((BirdColor)c, (BirdSex)s));
            for (int w = 0; w < WildCount; w++)
                _deck.Add(Card.MakeWild());
            ResetRound();
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

        public static void CycleBet()
        {
            if (PhaseNow != Phase.Idle) return;
            int ix = 0;
            for (int i = 0; i < _bets.Length; i++)
                if (_bets[i] == Bet) { ix = i; break; }
            Bet = _bets[(ix + 1) % _bets.Length];
        }

        public static bool Deal()
        {
            if (PhaseNow != Phase.Idle) return false;
            if (!Purse.TrySpend(Bet)) return false;
            ShuffleShoe();
            for (int i = 0; i < HandSize; i++)
            {
                Hand[i] = DrawOne();
                Hold[i] = false;
            }
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
            PhaseNow = Phase.Drawn;
            return true;
        }

        public static void Collect()
        {
            if (PhaseNow != Phase.Drawn) return;
            ResetRound();
        }

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

        static int PayFor(Rank r, int bet)
        {
            int mult;
            switch (r)
            {
                case Rank.NaturalFive: mult = 250; break;
                case Rank.FiveWild: mult = 50; break;
                case Rank.Quads: mult = 25; break;
                case Rank.FullHouse: mult = 9; break;
                case Rank.Trips: mult = 3; break;
                case Rank.TwoPair: mult = 2; break;
                case Rank.Pair: mult = 1; break;
                default: return 0;
            }
            return bet * mult;
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
            int last = _shoe.Count - 1;
            var c = _shoe[last];
            _shoe.RemoveAt(last);
            return c;
        }
    }
}
