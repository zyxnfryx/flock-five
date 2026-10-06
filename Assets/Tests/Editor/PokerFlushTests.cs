#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Poker Flush checks: all five birds one color (wilds count as any) lifts Trips / Two Pair
    // to Flush, never lowers Full House and up, and the pay table lists it between Full House
    // and Trips. Also counts every 5-card hand of the 80-card deck against the exact figures
    // from the RTP script, so the evaluator and the deck cannot drift apart unnoticed.
    // Build 59: FIVE WILDS (all five wild) is its own rank, 250x, top row; the one
    // all-wild deal moves out of FiveWild (3751 -> 3750 + 1).
    // Run from the menu, or drop a file on /tmp/flock-five-flush-tests.
    [InitializeOnLoad]
    static class PokerFlushTests
    {
        const string Cmd = "/tmp/flock-five-flush-tests";
        const string Out = "/tmp/flock-five-flush-tests.txt";

        static PokerFlushTests()
        {
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (!File.Exists(Cmd) || EditorApplication.isCompiling) return;
            try { File.Delete(Cmd); }
            catch { return; }
            Run();
        }

        static BirdPoker.Card B(BirdColor c, BirdSex s) => BirdPoker.Card.Of(c, s);
        static BirdPoker.Card Wd() => BirdPoker.Card.MakeWild();

        static BirdPoker.Card FromKind(int k)
        {
            if (k == BirdPoker.WildKind) return Wd();
            return B((BirdColor)(k / 3), (BirdSex)(k % 3));
        }

        static long Choose5(int n)
        {
            switch (n)
            {
                case 0: return 1;
                case 1: return 5;
                case 2: return 10;
                case 3: return 10;
                case 4: return 5;
                default: return 1;
            }
        }

        [MenuItem("Flock Five/Poker Flush Tests")]
        public static void Run()
        {
            var sb = new StringBuilder();
            int pass = 0;
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++; else fail++;
                string line = (ok ? "PASS  " : "FAIL  ") + name + "  " + detail;
                sb.AppendLine(line);
                Debug.Log("[poker-flush] " + line);
            }
            void Hand(string name, BirdPoker.Rank want, params BirdPoker.Card[] h)
            {
                var got = BirdPoker.Evaluate(h, out _);
                Check(name, got == want, "want " + want + "  got " + got);
            }

            const BirdColor R = BirdColor.Ruby;
            const BirdColor G = BirdColor.Gold;
            const BirdColor T = BirdColor.Teal;
            const BirdSex N = BirdSex.Neutral;
            const BirdSex F = BirdSex.Female;
            const BirdSex M = BirdSex.Male;

            Hand("same color, three of a look", BirdPoker.Rank.Flush, B(R, N), B(R, N), B(R, N), B(R, F), B(R, M));
            Hand("same color, two pair", BirdPoker.Rank.Flush, B(T, N), B(T, N), B(T, F), B(T, F), B(T, M));
            Hand("same color, pair + wild fills the third look", BirdPoker.Rank.Flush, B(G, N), B(G, N), B(G, F), B(G, M), Wd());
            Hand("same color, two wilds", BirdPoker.Rank.Flush, B(R, N), B(R, F), B(R, M), Wd(), Wd());
            Hand("two birds + three wilds is four of a kind, not flush", BirdPoker.Rank.Quads, B(R, N), B(R, F), Wd(), Wd(), Wd());
            Hand("mixed colors, two pair stays two pair", BirdPoker.Rank.TwoPair, B(R, N), B(R, N), B(R, F), B(R, F), B(G, M));
            Hand("mixed colors, trips stays trips", BirdPoker.Rank.Trips, B(R, N), B(R, N), B(R, N), B(G, F), B(T, M));
            Hand("wild does not make a flush out of two colors", BirdPoker.Rank.Trips, B(R, N), B(R, N), B(G, F), B(T, M), Wd());
            Hand("same color full house stays full house", BirdPoker.Rank.FullHouse, B(R, N), B(R, N), B(R, N), B(R, F), B(R, F));
            Hand("same color full house via wild stays full house", BirdPoker.Rank.FullHouse, B(R, N), B(R, N), B(R, F), B(R, F), Wd());
            Hand("same color quads stays quads", BirdPoker.Rank.Quads, B(R, N), B(R, N), B(R, N), B(R, N), B(R, F));
            Hand("three + wild stays quads", BirdPoker.Rank.Quads, B(R, N), B(R, N), B(R, N), Wd(), B(G, M));
            Hand("natural five stays natural five", BirdPoker.Rank.NaturalFive, B(R, N), B(R, N), B(R, N), B(R, N), B(R, N));
            Hand("five with a wild stays five of a kind", BirdPoker.Rank.FiveWild, B(R, N), B(R, N), B(R, N), B(R, N), Wd());
            Hand("five wilds is FIVE WILDS", BirdPoker.Rank.FiveWilds, Wd(), Wd(), Wd(), Wd(), Wd());
            Hand("four wilds + a bird stays five of a kind", BirdPoker.Rank.FiveWild, Wd(), Wd(), Wd(), Wd(), B(R, N));
            Hand("single pair, mixed", BirdPoker.Rank.Pair, B(R, N), B(R, N), B(G, F), B(T, M), B(BirdColor.Violet, N));
            Hand("nothing", BirdPoker.Rank.Nothing, B(R, N), B(R, F), B(G, M), B(T, N), B(BirdColor.Violet, F));

            Check("IsFlush: one color", BirdPoker.IsFlush(new[] { B(R, N), B(R, F), B(R, M), B(R, N), B(R, F) }), "");
            Check("IsFlush: wilds fill", BirdPoker.IsFlush(new[] { B(R, N), Wd(), Wd(), B(R, M), Wd() }), "");
            Check("IsFlush: two colors", !BirdPoker.IsFlush(new[] { B(R, N), B(G, N), B(R, M), B(R, N), B(R, F) }), "");
            Check("IsFlush: all wild is not a color flush", !BirdPoker.IsFlush(new[] { Wd(), Wd(), Wd(), Wd(), Wd() }), "");

            // Pay table: Flush sits between Full House and Trips in rank, multiplier and row order.
            int mF = BirdPoker.Multiplier(BirdPoker.Rank.Flush);
            Check("flush pays more than trips", mF > BirdPoker.Multiplier(BirdPoker.Rank.Trips), "x" + mF);
            Check("flush pays less than full house", mF < BirdPoker.Multiplier(BirdPoker.Rank.FullHouse), "x" + mF);
            Check("flush enum between trips and full house",
                BirdPoker.Rank.Flush > BirdPoker.Rank.Trips && BirdPoker.Rank.Flush < BirdPoker.Rank.FullHouse, "");
            Check("flush label", BirdPoker.RankLabel(BirdPoker.Rank.Flush) == "FLUSH", BirdPoker.RankLabel(BirdPoker.Rank.Flush));
            // FIVE WILDS: its own top rank, 250x, first pay-table row, punches, jackpot fanfare.
            Check("five wilds enum value 9, existing values kept",
                (int)BirdPoker.Rank.FiveWilds == 9 && (int)BirdPoker.Rank.NaturalFive == 8 && (int)BirdPoker.Rank.FiveWild == 7, "");
            Check("five wilds pays 250x", BirdPoker.Multiplier(BirdPoker.Rank.FiveWilds) == 250,
                "x" + BirdPoker.Multiplier(BirdPoker.Rank.FiveWilds));
            Check("natural five still 150x", BirdPoker.Multiplier(BirdPoker.Rank.NaturalFive) == 150,
                "x" + BirdPoker.Multiplier(BirdPoker.Rank.NaturalFive));
            Check("five wilds label", BirdPoker.RankLabel(BirdPoker.Rank.FiveWilds) == "FIVE WILDS", BirdPoker.RankLabel(BirdPoker.Rank.FiveWilds));
            Check("five wilds is the first pay-table row, natural five next",
                BirdPoker.PayTableRows.Length > 1 && BirdPoker.PayTableRows[0] == BirdPoker.Rank.FiveWilds
                && BirdPoker.PayTableRows[1] == BirdPoker.Rank.NaturalFive, "");
            Check("five wilds punches like the other fives", BirdPoker.IsFiveKind(BirdPoker.Rank.FiveWilds)
                && BirdPoker.IsFiveKind(BirdPoker.Rank.NaturalFive) && BirdPoker.IsFiveKind(BirdPoker.Rank.FiveWild)
                && !BirdPoker.IsFiveKind(BirdPoker.Rank.Quads), "");
            Check("five wilds celebrates at least as big as natural five",
                BirdPoker.IsJackpot(BirdPoker.Rank.FiveWilds)
                && BirdPoker.CelebrationLevel(BirdPoker.Rank.FiveWilds) >= BirdPoker.CelebrationLevel(BirdPoker.Rank.NaturalFive), "");

            var rows = BirdPoker.PayTableRows;
            int at = -1;
            bool desc = true;
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i] == BirdPoker.Rank.Flush) at = i;
                if (i > 0 && BirdPoker.Multiplier(rows[i]) > BirdPoker.Multiplier(rows[i - 1])) desc = false;
                if (string.IsNullOrEmpty(BirdPoker.RankLabel(rows[i]))) desc = false;
            }
            Check("pay table lists flush once, rows high to low, all labeled", at >= 0 && desc, "row " + at + " of " + rows.Length);
            Check("pay table fits the 8 cached row slots", rows.Length <= 8, "rows " + rows.Length);

            // Exhaustive: every 5-card hand of the real 80-card deck, grouped as multisets of the 16 kinds.
            var tally = new long[10];
            bool flushNeverLow = true;
            var hand = new BirdPoker.Card[5];
            var cnt = new int[16];
            for (int a = 0; a < 16; a++)
            for (int b = a; b < 16; b++)
            for (int c = b; c < 16; c++)
            for (int d = c; d < 16; d++)
            for (int e = d; e < 16; e++)
            {
                System.Array.Clear(cnt, 0, 16);
                cnt[a]++; cnt[b]++; cnt[c]++; cnt[d]++; cnt[e]++;
                long w = 1;
                for (int k = 0; k < 16; k++) w *= Choose5(cnt[k]);
                hand[0] = FromKind(a); hand[1] = FromKind(b); hand[2] = FromKind(c);
                hand[3] = FromKind(d); hand[4] = FromKind(e);
                var r = BirdPoker.Evaluate(hand, out _);
                tally[(int)r] += w;
                if (BirdPoker.IsFlush(hand) && r < BirdPoker.Rank.Flush) flushNeverLow = false;
            }
            Check("a same-color hand never ranks below flush", flushNeverLow, "");
            WantCount(Check, tally, BirdPoker.Rank.Nothing, 9384375L);
            WantCount(Check, tally, BirdPoker.Rank.Pair, 11090625L);
            WantCount(Check, tally, BirdPoker.Rank.TwoPair, 675000L);
            WantCount(Check, tally, BirdPoker.Rank.Trips, 2587500L);
            WantCount(Check, tally, BirdPoker.Rank.Flush, 36250L);
            WantCount(Check, tally, BirdPoker.Rank.FullHouse, 73500L);
            WantCount(Check, tally, BirdPoker.Rank.Quads, 189000L);
            WantCount(Check, tally, BirdPoker.Rank.FiveWild, 3750L);
            WantCount(Check, tally, BirdPoker.Rank.FiveWilds, 1L);
            WantCount(Check, tally, BirdPoker.Rank.NaturalFive, 15L);
            long total = 0;
            for (int i = 0; i < tally.Length; i++) total += tally[i];
            Check("all 24,040,016 deals counted", total == 24040016L, total.ToString());

            sb.AppendLine(fail == 0 ? "ALL OK  " + pass : "FAILED  " + fail + "  passed " + pass);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[poker-flush] " + (fail == 0 ? "ALL OK " + pass : "FAILED " + fail));
        }

        delegate void CheckFn(string name, bool ok, string detail);

        static void WantCount(CheckFn check, long[] tally, BirdPoker.Rank r, long want)
        {
            long got = tally[(int)r];
            check("deal count " + r, got == want, "want " + want + "  got " + got);
        }
    }
}
#endif
