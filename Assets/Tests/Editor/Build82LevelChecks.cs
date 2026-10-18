using System;
using System.Collections.Generic;

namespace FlockFive
{
    // Build 82 level checks. Pure board code (no UnityEditor) so the same body
    // runs in the EditMode menu test and in an offline harness.
    public static class Build82LevelChecks
    {
        public const int NodeCap = 250000;

        // One recorded hidden-info trap per garden 16-29, found offline with the
        // Build 82 rules. Path and move are (from, to) branch indices on the
        // garden exactly as LevelData.Open(index) returns it. Alt is a
        // permutation of the shrouded birds at the trap state (color*3 + kit, in
        // branch order then trunk to tip) under which the SAME move stays winnable.
        public struct Proof
        {
            public int Level;
            public string Path;
            public int From, To;
            public int[] Alt;
            public Proof(int level, string path, int from, int to, int[] alt)
            {
                Level = level; Path = path; From = from; To = to; Alt = alt;
            }
        }

        public static readonly Proof[] Proofs =
        {
            new Proof(16, "3>14 12>6 1>13", 12, 1, new[] { 13, 0, 13, 10, 10 }),
            new Proof(17, "1>12 3>12 1>3 9>1 4>9", 4, 1, new[] { 10, 13, 13, 10 }),
            new Proof(18, "9>7 2>0 9>2 11>9 11>2 11>13 9>11 6>9", 2, 6, new[] { 3, 2, 7, 7, 9, 9 }),
            new Proof(19, "0>4 7>10 2>6 7>5 1>2", 1, 3, new[] { 5, 0 }),
            new Proof(20, "5>4 3>0 8>3 10>7 13>3 9>5 9>13 12>11 2>7", 2, 12, new[] { 13, 8, 11 }),
            new Proof(21, "1>4 12>10 1>11 12>1", 7, 3, new[] { 1, 9, 6, 1, 2 }),
            new Proof(22, "7>6", 7, 13, new[] { 12, 11, 9, 13 }),
            new Proof(23, "", 12, 13, new[] { 13, 11, 11, 14 }),
            new Proof(24, "9>12 11>9", 11, 13, new[] { 7, 9, 6, 10, 7, 4 }),
            new Proof(25, "6>5 2>10 1>9 1>0 6>12 4>8", 1, 4, new[] { 12, 10, 4, 12, 0, 10, 10, 12, 4, 6, 6 }),
            new Proof(26, "", 1, 2, new[] { 2, 2, 0, 0 }),
            new Proof(28, "0>2 2>3 2>1 3>2", 0, 3, new[] { 6, 7, 6, 7 }),
            new Proof(29, "0>1", 0, 2, new[] { 3, 3, 4, 5, 5 }),
            new Proof(30, "1>2 1>0", 2, 1, new[] { 11, 9, 11 }),
            // Build 111: generated gardens 33-37, proofs from TrapProbe.FindPlayed.
            new Proof(33, "10>11 1>6 13>8 5>3 11>10 10>13 3>4 5>6 11>8 2>5 2>0", 11, 7, new[] { 14, 10 }),
            new Proof(34, "12>13 7>9 5>6 10>11 0>2 4>3 4>5 5>4", 3, 0, new[] { 7, 6, 9, 12, 3, 12 }),
            new Proof(35, "13>10 11>9 1>2 5>1 6>3 4>2 7>9 1>6 12>13 0>2 10>11 13>10 12>13 11>12 1>0 5>1", 3, 5, new[] { 0, 8, 5, 0, 10 }),
            new Proof(36, "10>13 2>6 12>10 5>12 9>13 8>5 3>6", 3, 12, new[] { 5, 1, 10, 7, 14, 4 }),
            new Proof(37, "3>5 1>3 2>1 10>12 8>10 8>9", 9, 8, new[] { 11, 3, 8, 12, 3, 8, 8 }),
        };

        static int IndexOf(int number)
        {
            for (int i = 0; i < LevelData.Count; i++)
                if (LevelData.Peek(i).Number == number) return i;
            return -1;
        }

        static Board Apply(Board s, int from, int to)
        {
            var b = s.Clone();
            if (!b.TryMove(from, to, out _)) return null;
            GardenSolve.Drain(b);
            return b;
        }

        static bool Wins(Board b) =>
            b != null && (b.Won || GardenSolve.Search(b, NodeCap).Outlook == GardenSolve.Outlook.Winnable);

        static Board Unlock(Board b)
        {
            var n = b.Clone();
            for (int i = 0; i < n.Branches.Count; i++) n.Branches[i].AdLocked = false;
            return n;
        }

        public static void Run(Action<string, bool, string> Check)
        {
            CheckIdentity(Check);
            CheckValidator(Check);
            CheckAssign(Check);
            CheckLadder(Check);
            CheckSolo(Check);
            CheckTraps(Check);
        }

        // Kit is identity: hops, runs, fives and bee lifts all go through SameFlock.
        static void CheckIdentity(Action<string, bool, string> Check)
        {
            var crown = new Bird(BirdColor.Ruby, BirdKit.Crown);
            var bow = new Bird(BirdColor.Ruby, BirdKit.Bow);
            Check("same-flock-kit", crown.SameFlock(crown) && !crown.SameFlock(bow), "crown vs bow");

            var mixed = new BranchState();
            for (int i = 0; i < 4; i++) { mixed.Birds.Add(crown); mixed.Shrouded.Add(false); }
            mixed.Birds.Add(bow); mixed.Shrouded.Add(false);
            Check("five-needs-one-kit", !mixed.IsFullMatch(out _), "4 crown + 1 bow is not a five");
            Check("tip-run-stops-at-kit", mixed.TipRun() == 1, "run " + mixed.TipRun());

            var b = new Board();
            var a = new BranchState(); a.Birds.Add(crown); a.Shrouded.Add(false);
            var d = new BranchState(); d.Birds.Add(bow); d.Shrouded.Add(false);
            b.Branches.Add(a); b.Branches.Add(d);
            Check("no-hop-across-kits", !b.CanMove(0, 1, out _), "crown onto bow");

            var bees = new BranchState();
            bees.Birds.Add(bow); bees.Birds.Add(crown); bees.Birds.Add(crown);
            bees.Shrouded.Add(true); bees.Shrouded.Add(true); bees.Shrouded.Add(true);
            int lifted = bees.RevealExposed();
            Check("reveal-same-flock-only", lifted == 2 && bees.IsShrouded(0), "lifted " + lifted);
        }

        static Board Sets(BirdColor c, params BirdKit[] kits)
        {
            var b = new Board();
            foreach (var k in kits)
            {
                var br = new BranchState();
                for (int i = 0; i < BranchState.Cap; i++) { br.Birds.Add(new Bird(c, k)); br.Shrouded.Add(false); }
                b.Branches.Add(br);
            }
            return b;
        }

        static void CheckValidator(Action<string, bool, string> Check)
        {
            var three = Sets(BirdColor.Gold, BirdKit.Crown, BirdKit.Bow, BirdKit.Plain);
            Check("kitsets-three-ok", BoardValidator.CheckKitSets(three).Ok, BoardValidator.CheckKitSets(three).Message);
            var four = Sets(BirdColor.Gold, BirdKit.Crown, BirdKit.Bow, BirdKit.Plain, BirdKit.Plain);
            var r4 = BoardValidator.CheckKitSets(four);
            Check("kitsets-four-fails", !r4.Ok && r4.Message.Contains("4 sets"), r4.Message);
            var dup = Sets(BirdColor.Teal, BirdKit.Bow, BirdKit.Bow);
            Check("kitsets-ten-one-kit-fails", !BoardValidator.CheckKitSets(dup).Ok, BoardValidator.CheckKitSets(dup).Message);
        }

        // Assign without a map: whole sets, Crown/Bow/Plain only, deterministic.
        static void CheckAssign(Action<string, bool, string> Check)
        {
            var b = new Board();
            var colors = new[] { BirdColor.Ruby, BirdColor.Gold }; // 15 + 15 birds
            for (int r = 0; r < 6; r++)
            {
                var br = new BranchState();
                for (int i = 0; i < 5; i++) { br.Birds.Add(new Bird(colors[(r + i) % colors.Length])); br.Shrouded.Add(false); }
                b.Branches.Add(br);
            }
            var c = b.Clone();
            FlockKit.Assign(b, 7);
            FlockKit.Assign(c, 7);
            bool same = true;
            for (int i = 0; i < b.Branches.Count; i++)
                for (int k = 0; k < b.Branches[i].Count; k++)
                    if (b.Branches[i].Birds[k].Kit != c.Branches[i].Birds[k].Kit) same = false;
            Check("assign-deterministic", same, "two runs match");
            Check("assign-whole-sets", BoardValidator.CheckKitSets(b).Ok, BoardValidator.CheckKitSets(b).Message);
        }

        static void CheckLadder(Action<string, bool, string> Check)
        {
            string bad = null, lost = null, nomix = null;
            int mixing = 0, multi = 0;
            for (int i = 0; i < LevelData.Count; i++)
            {
                var lv = LevelData.Peek(i);
                var b = LevelData.Open(i);
                var r = BoardValidator.CheckKitSets(b);
                if (bad == null && !r.Ok) bad = "L" + lv.Number + " " + r.Message;
                if (lost == null && !Wins(b)) lost = "L" + lv.Number + " not winnable without the gift";
                if (lost == null && !Wins(Unlock(b))) lost = "L" + lv.Number + " not winnable with the gift";
                var n = BoardValidator.FlockCounts(b);
                bool anyMulti = false, anyMix = false;
                for (int col = 0; col < Palette.Max; col++)
                {
                    int kits = 0;
                    for (int k = 0; k < Palette.Kits; k++) if (n[col * Palette.Kits + k] > 0) kits++;
                    if (kits >= 2) anyMix = true;
                    int total = 0;
                    for (int k = 0; k < Palette.Kits; k++) total += n[col * Palette.Kits + k];
                    if (total >= 2 * BranchState.Cap) anyMulti = true;
                }
                if (anyMulti) multi++;
                if (anyMix) mixing++;
                if (anyMulti && !anyMix && nomix == null) nomix = "L" + lv.Number;
            }
            Check("ladder-count", LevelData.Count == 38, "count " + LevelData.Count);
            Check("ladder-kit-sets", bad == null, bad ?? "every garden: each color <= 3 sets, one set per kit");
            Check("ladder-winnable", lost == null, lost ?? "all gardens Winnable with and without the gift");
            Check("ladder-kits-mix", nomix == null && mixing == LevelData.Count, "gardens mixing kits within a color " + mixing + "/" + LevelData.Count);
        }

        static void CheckSolo(Action<string, bool, string> Check)
        {
            var want = new[] { BirdColor.Ruby, BirdColor.Teal, BirdColor.Gold, BirdColor.Violet };
            for (int s = 0; s < 4; s++)
            {
                int num = s == 0 ? 26 : 27 + s; // b83: ostrich debut at 27 shifts solo 27-29 to 28-30
                int idx = IndexOf(num);
                var lv = idx >= 0 ? LevelData.Peek(idx) : null;
                if (lv == null) { Check("solo-" + num, false, "missing"); continue; }
                var b = LevelData.Open(idx);
                var n = BoardValidator.Counts(b);
                bool one = true;
                for (int c = 0; c < Palette.Max; c++)
                    if (c != (int)want[s] && n[c] != 0) one = false;
                int limbs = 0, gifts = 0;
                foreach (var br in b.Branches) { if (br.AdLocked) gifts++; else limbs++; }
                Check("solo-" + num,
                    lv.Solo && lv.Id == "garden-" + num && lv.Title == "Garden " + num && one
                    && n[(int)want[s]] == 3 * BranchState.Cap && limbs == 4 && gifts == 2
                    && BoardValidator.CheckKitSets(b).Ok,
                    want[s] + " birds " + n[(int)want[s]] + " limbs " + limbs + " gifts " + gifts);
            }
        }

        // Every garden 16+ keeps a hidden-info trap: from a winnable state, a legal
        // move is proven Tangled, yet the same move wins under another arrangement
        // of the birds the player cannot see. The ad gift rescues it.
        static void CheckTraps(Action<string, bool, string> Check)
        {
            for (int p = 0; p < Proofs.Length; p++)
            {
                var pr = Proofs[p];
                // Build 83: 18-25 are the eased maps Level Design proved in its harness; their b82 proof paths no longer apply.
                if (pr.Level >= 18 && pr.Level <= 25) continue;
                string name = "trap-" + pr.Level;
                int idx = IndexOf(pr.Level);
                if (idx < 0) { Check(name, false, "missing level"); continue; }
                var s = LevelData.Open(idx);
                GardenSolve.Drain(s);
                bool ok = true; string why = "ok";
                foreach (var tok in pr.Path.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var ft = tok.Split('>');
                    s = Apply(s, int.Parse(ft[0]), int.Parse(ft[1]));
                    if (s == null) { ok = false; why = "path move " + tok + " illegal"; break; }
                }
                if (ok && !Wins(s)) { ok = false; why = "trap state not winnable"; }
                Board post = ok ? Apply(s, pr.From, pr.To) : null;
                if (ok && post == null) { ok = false; why = "trap move illegal"; }
                if (ok && (post.Won || GardenSolve.Search(post, NodeCap).Outlook != GardenSolve.Outlook.Tangled)) { ok = false; why = "trap move not proven Tangled"; }
                if (ok)
                {
                    var alt = s.Clone();
                    int j = 0;
                    for (int i = 0; i < alt.Branches.Count && ok; i++)
                    {
                        var br = alt.Branches[i];
                        if (br.Broken) continue;
                        for (int k = 0; k < br.Count; k++)
                        {
                            if (!br.IsShrouded(k)) continue;
                            if (j >= pr.Alt.Length) { ok = false; why = "alt too short"; break; }
                            br.Birds[k] = new Bird((BirdColor)(pr.Alt[j] / 3), (BirdKit)(pr.Alt[j] % 3));
                            j++;
                        }
                    }
                    if (ok && j != pr.Alt.Length) { ok = false; why = "alt length " + pr.Alt.Length + " vs shrouds " + j; }
                    if (ok && !Wins(Apply(alt, pr.From, pr.To))) { ok = false; why = "move not hidden-caused"; }
                }
                if (ok)
                {
                    var gift = Apply(Unlock(s), pr.From, pr.To);
                    if (!Wins(gift)) { ok = false; why = "gift does not rescue"; }
                }
                Check(name, ok, why + " move " + pr.From + ">" + pr.To);
            }
            var covered = new HashSet<int>();
            foreach (var pr in Proofs) covered.Add(pr.Level);
            bool all = true;
            for (int n = 16; n <= 37; n++) if ((n < 18 || n > 25) && n != 27 && n != 31 && n != 32 && !covered.Contains(n)) all = false;
            Check("trap-coverage-16-29", all, covered.Count + " gardens");
        }
    }
}
