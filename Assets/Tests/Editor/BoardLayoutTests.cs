#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Data-level checks for restart, pest scraps, and the two gift limbs.
    // Run from the menu, or drop a file on /tmp/flock-five-board-tests.
    [InitializeOnLoad]
    static class BoardLayoutTests
    {
        const string Cmd = "/tmp/flock-five-board-tests";
        const string Out = "/tmp/flock-five-board-tests.txt";

        static BoardLayoutTests()
        {
            EditorApplication.delayCall += Maybe;
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (File.Exists(Cmd)) Maybe();
        }

        static void Maybe()
        {
            if (!File.Exists(Cmd)) return;
            if (EditorApplication.isCompiling) return;
            try { File.Delete(Cmd); }
            catch { return; }
            Run();
        }

        [MenuItem("Flock Five/Board Layout Tests")]
        public static void Run()
        {
            var sb = new StringBuilder();
            int pass = 0;
            int fail = 0;
            void Line(string t)
            {
                sb.AppendLine(t);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[board-layout] " + t);
            }
            void Check(string name, bool ok, string detail)
            {
                if (ok) { pass++; Line("PASS  " + name + "  " + detail); }
                else { fail++; Line("FAIL  " + name + "  " + detail); }
            }

            CheckSeats(Check);
            CheckEndgame(Check);
            CheckHawkKeepsLimb(Check);
            CheckBonusRegrows(Check);
            CheckRestart(Check);
            CheckLayout(Check);
            CheckBonusCloses(Check);
            CheckLevels(Check);
            CheckRestore(Check);
            CheckBonusOpensEmpty(Check);
            CheckStageOne(Check);
            CheckKitSets(Check);
            CheckFreezeContinue(Check);

            Line(fail == 0 ? "ALL OK  " + pass : "FAILED  " + fail + "  passed " + pass);
        }

        static void CheckSeats(System.Action<string, bool, string> Check)
        {
            var x = WorldBuilder.SeatXPx;
            bool even = x != null && x.Length == BranchState.Cap;
            float minG = 999f, maxG = 0f;
            if (even)
            {
                for (int i = 1; i < x.Length; i++)
                {
                    float g = x[i] - x[i - 1];
                    if (g < minG) minG = g;
                    if (g > maxG) maxG = g;
                }
                even = minG >= 200f && maxG - minG <= 30f;
            }
            bool margin = even && x[0] >= 160f && x[x.Length - 1] <= 1160f && (1280f - x[x.Length - 1]) >= 100f;
            Check("seats-even", even, even ? "gaps " + minG.ToString("0") + "-" + maxG.ToString("0") + "px" : "uneven");
            Check("seats-tip-margin", margin, margin ? "tip at " + x[x.Length - 1].ToString("0") : "tip tight");
        }

        static void CheckEndgame(System.Action<string, bool, string> Check)
        {
            var b = MonoFlock();
            var expect = BoardValidator.Counts(b);
            int before = b.Branches.Count;
            var plan = PestPark.Apply(b, 0, hawk: false);
            var report = BoardValidator.Check(b, expect);
            Check("endgame-counts", report.Ok, report.Message);
            Check("endgame-kept-limb", !b.Branches[0].Broken, "broken=" + b.Branches[0].Broken);
            Check("endgame-has-room", PestPark.FreeSeats(b) >= 1, "free " + PestPark.FreeSeats(b));
            Check("endgame-no-full-match", b.FindCollect() < 0, "collect " + b.FindCollect());
            Check("endgame-spawned", b.Branches.Count > before && plan.Spawn >= 1, "branches " + b.Branches.Count);
            Check("endgame-all-seated", Seated(b), "birds " + b.RemainingBirds);
        }

        static void CheckHawkKeepsLimb(System.Action<string, bool, string> Check)
        {
            var b = MonoFlock();
            b.Branches.Add(new BranchState());
            var expect = BoardValidator.Counts(b);
            int limbs = Live(b);
            PestPark.Apply(b, 0, hawk: true);
            Check("hawk-counts", BoardValidator.Same(BoardValidator.Counts(b), expect), "birds " + b.RemainingBirds);
            Check("hawk-limbs", Live(b) >= limbs && !b.Branches[0].Broken, "live " + Live(b) + " broken0 " + b.Branches[0].Broken);
        }

        static void CheckBonusRegrows(System.Action<string, bool, string> Check)
        {
            var b = new Board();
            var gift = new BranchState { IsBonus = true };
            for (int i = 0; i < 5; i++)
                gift.Birds.Add(new Bird(BirdColor.Violet, LevelData.SexFor(BirdColor.Violet)));
            b.Branches.Add(gift);
            b.Live[0] = BirdColor.Violet;
            var expect = BoardValidator.Counts(b);
            PestPark.Apply(b, 0, hawk: false);
            Check("bonus-regrows", !b.Branches[0].Broken && b.Branches[0].IsBonus, "broken=" + b.Branches[0].Broken);
            Check("bonus-counts", BoardValidator.Same(BoardValidator.Counts(b), expect), "birds " + b.RemainingBirds);
        }

        static void CheckRestart(System.Action<string, bool, string> Check)
        {
            var seed = LevelData.Open(15);
            var expect = BoardValidator.Counts(seed);
            var play = seed.Clone();
            int snaps = 0;
            for (int n = 0; n < 4; n++)
            {
                int pick = -1;
                for (int i = 0; i < play.Branches.Count; i++)
                {
                    var st = play.Branches[i];
                    if (st.IsBonus || st.Broken || st.Count < 3) continue;
                    pick = i;
                    break;
                }
                if (pick < 0) break;
                PestPark.Apply(play, pick, hawk: false);
                snaps++;
            }
            for (int i = 0; i < play.Branches.Count; i++)
            {
                var st = play.Branches[i];
                if (st.IsBonus || st.Broken || st.Count < 1) continue;
                PestPark.Apply(play, i, hawk: true);
                break;
            }
            Check("sparrow-snaps", snaps >= 3, "snaps " + snaps);
            Check("play-conserves", BoardValidator.Same(BoardValidator.Counts(play), expect),
                "snaps " + snaps + " " + BoardValidator.Check(play, expect).Message);

            var claimed = new bool[WorldBuilder.GiftCount];
            claimed[0] = true;
            var restarted = seed.Clone();
            BonusBranches.ApplyClaims(restarted, claimed);
            Check("restart-count", restarted.Branches.Count == seed.Branches.Count,
                restarted.Branches.Count + " vs seed " + seed.Branches.Count);
            Check("restart-birds", BoardValidator.Same(BoardValidator.Counts(restarted), expect),
                BoardValidator.Check(restarted, expect).Message);
            Check("restart-one-claim", ClaimedOpen(restarted) == 1, "open gifts " + ClaimedOpen(restarted));
            Check("restart-no-broken", BrokenCount(restarted) == 0, "broken " + BrokenCount(restarted));
            Check("restart-no-gap", Packed(restarted), "columns");
            Check("restart-seated", Seated(restarted), "birds " + restarted.RemainingBirds);
        }

        static void CheckLayout(System.Action<string, bool, string> Check)
        {
            var b = LevelData.Open(0);
            int guard = 0;
            for (int i = 0; i < b.Branches.Count && guard < 3; i++)
            {
                if (b.Branches[i].IsBonus || b.Branches[i].Broken || b.Branches[i].Count == 0) continue;
                b.Branches[i].Broken = true;
                guard++;
            }
            var spots = new List<GardenFit.Spot>();
            GardenFit.Collect(b, null, spots);
            bool scale = true;
            for (int i = 0; i < spots.Count; i++)
                if (spots[i].Scale != GardenFit.LimbScale) scale = false;
            Check("layout-scale", scale, "spots " + spots.Count);
            Check("layout-left-packed", GardenFit.RowsPacked(spots, 0), "left");
            Check("layout-right-packed", GardenFit.RowsPacked(spots, 1), "right");
            Check("layout-two-gifts", BonusSpots(b, spots) == 2, "gifts " + BonusSpots(b, spots));

            float anchor = WorldBuilder.ColumnX();
            var full = LevelData.Open(0);
            var thin = ThinColumns();
            bool held = ColumnsAnchored(full, anchor) && ColumnsAnchored(b, anchor) && ColumnsAnchored(thin, anchor);
            Check("layout-x-anchor", held, "anchor " + anchor.ToString("0.00"));
        }

        // One plain limb each side, gifts left alone. X must match a full board.
        static Board ThinColumns()
        {
            var b = LevelData.Open(0);
            int seenL = 0, seenR = 0, plain = 0;
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var st = b.Branches[i];
                if (st == null || st.IsBonus) continue;
                bool rightSide = (plain & 1) == 1;
                plain++;
                if (rightSide)
                {
                    seenR++;
                    if (seenR > 1) st.Broken = true;
                }
                else
                {
                    seenL++;
                    if (seenL > 1) st.Broken = true;
                }
            }
            return b;
        }

        static bool ColumnsAnchored(Board b, float anchor)
        {
            var spots = new List<GardenFit.Spot>();
            GardenFit.Collect(b, null, spots);
            if (spots.Count == 0) return false;
            for (int i = 0; i < spots.Count; i++)
            {
                if (Mathf.Abs(Mathf.Abs(spots[i].Pos.x) - anchor) > 0.0001f) return false;
                if (Mathf.Abs(spots[i].Scale.x - 1f) > 0.0001f) return false;
            }
            return true;
        }

        // Two full sets of one color and sex must not share a crown. Set 0 keeps
        // the sex kit. Later sets differ. Hops still match by color unless the
        // level already split the flock by accessory.
        static void CheckKitSets(System.Action<string, bool, string> Check)
        {
            Board orange = null;
            int orangeAt = -1;
            string bad = null;
            for (int i = 0; i < LevelData.Count; i++)
            {
                var b = LevelData.Open(i);
                if (orange == null && PeachSets(b) >= 2)
                {
                    orange = b;
                    orangeAt = i;
                }
                if (bad == null && !SetsDiffer(b, out var why))
                    bad = "L" + (i + 1) + " " + why;
            }
            Check("kit-sets", bad == null, bad ?? "each extra set wears its own accessory");
            if (orange == null)
                Check("orange-two-sets", false, "no garden has two peach sets");
            else
            {
                bool ok = OrangeSets(orange, out var detail);
                Check("orange-two-sets", ok, "L" + (orangeAt + 1) + " " + detail);
            }

            var dawn = LevelData.Open(0);
            bool one = OnePeachSet(dawn, out var oneDetail);
            Check("orange-one-set", one, oneDetail);

            bool authored = AuthoredKitsBind(out var auth);
            Check("authored-kit-binds", authored, auth);

            bool kept = MoveKeepsKit(out var move);
            Check("kit-rides-hop", kept, move);
        }

        static int PeachSets(Board b)
        {
            int n = 0;
            bool male = true;
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var br = b.Branches[i];
                for (int k = 0; k < br.Birds.Count; k++)
                {
                    if (br.Birds[k].Color != BirdColor.Peach) continue;
                    n++;
                    if (br.Birds[k].Sex != BirdSex.Male) male = false;
                }
            }
            if (!male || n < BranchState.Cap * 2 || n % BranchState.Cap != 0) return 0;
            return n / BranchState.Cap;
        }

        static bool OrangeSets(Board b, out string detail)
        {
            int n = 0;
            var look = new int[8];
            var raw = new int[8];
            var rawSeen = new bool[8];
            bool male = true;
            bool binds = false;
            bool flock = true;
            bool rawMix = false;
            bool have = false;
            Bird first = default;
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var br = b.Branches[i];
                for (int k = 0; k < br.Birds.Count; k++)
                {
                    var bird = br.Birds[k];
                    if (bird.Color != BirdColor.Peach) continue;
                    int worn = (int)FlockKit.Worn(bird);
                    if ((uint)worn < (uint)look.Length) look[worn]++;
                    n++;
                    if (bird.Sex != BirdSex.Male) male = false;
                    if (bird.KitBinds) binds = true;
                    if ((uint)worn < (uint)raw.Length)
                    {
                        if (!rawSeen[worn]) { rawSeen[worn] = true; raw[worn] = (int)bird.Kit; }
                        else if (raw[worn] != (int)bird.Kit) rawMix = true;
                    }
                    if (!have) { first = bird; have = true; }
                    else if (!first.SameFlock(bird)) flock = false;
                }
            }
            int kinds = 0;
            bool fives = true;
            for (int i = 0; i < look.Length; i++)
            {
                if (look[i] == 0) continue;
                kinds++;
                if (look[i] != BranchState.Cap) fives = false;
            }
            int sets = n / BranchState.Cap;
            int crowns = look[(int)BirdKit.Crown];
            bool ok = male && !binds && flock && !rawMix && crowns == BranchState.Cap
                && n >= BranchState.Cap * 2 && n % BranchState.Cap == 0 && kinds == sets && fives;
            detail = "peach " + n + " kinds " + kinds + " crowns " + crowns
                + " male " + male + " binds " + binds + " flock " + flock + " mix " + rawMix;
            return ok;
        }

        static bool SetsDiffer(Board b, out string why)
        {
            why = "";
            var n = new int[Palette.Max * 3];
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var br = b.Branches[i];
                for (int k = 0; k < br.Birds.Count; k++)
                {
                    int key = (int)br.Birds[k].Color * 3 + (int)br.Birds[k].Sex;
                    if ((uint)key < (uint)n.Length) n[key]++;
                }
            }
            for (int key = 0; key < n.Length; key++)
            {
                if (n[key] < BranchState.Cap * 2 || n[key] % BranchState.Cap != 0) continue;
                var color = (BirdColor)(key / 3);
                var sex = (BirdSex)(key % 3);
                if (!GroupOk(b, color, sex, n[key], out why)) return false;
            }
            return true;
        }

        static bool GroupOk(Board b, BirdColor color, BirdSex sex, int count, out string why)
        {
            why = "";
            var look = new int[8];
            bool binds = false;
            bool flock = true;
            bool have = false;
            Bird first = default;
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var br = b.Branches[i];
                for (int k = 0; k < br.Birds.Count; k++)
                {
                    var bird = br.Birds[k];
                    if (bird.Color != color || bird.Sex != sex) continue;
                    int worn = (int)FlockKit.Worn(bird);
                    if ((uint)worn < (uint)look.Length) look[worn]++;
                    if (bird.KitBinds) binds = true;
                    if (!have) { first = bird; have = true; }
                    else if (!first.SameFlock(bird)) flock = false;
                }
            }
            int kinds = 0;
            bool fives = true;
            for (int i = 0; i < look.Length; i++)
            {
                if (look[i] == 0) continue;
                kinds++;
                if (look[i] != BranchState.Cap) fives = false;
            }
            int sets = count / BranchState.Cap;
            if (!binds && flock && fives && kinds == sets) return true;
            why = color + "/" + sex + " n" + count + " kinds " + kinds + " binds " + binds + " flock " + flock;
            return false;
        }

        static bool OnePeachSet(Board b, out string detail)
        {
            int n = 0;
            bool crowns = true;
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var br = b.Branches[i];
                for (int k = 0; k < br.Birds.Count; k++)
                {
                    var bird = br.Birds[k];
                    if (bird.Color != BirdColor.Peach) continue;
                    n++;
                    if (bird.KitBinds || bird.Kit != BirdKit.Plain || FlockKit.Worn(bird) != BirdKit.Crown)
                        crowns = false;
                }
            }
            detail = "dawn peach " + n + " crowns " + crowns;
            return n == BranchState.Cap && crowns;
        }

        static bool AuthoredKitsBind(out string detail)
        {
            var b = new Board();
            var crowns = new BranchState();
            var hats = new BranchState();
            for (int i = 0; i < BranchState.Cap; i++)
            {
                var crown = new Bird(BirdColor.Peach, BirdSex.Male);
                crown.Kit = BirdKit.Crown;
                crowns.Birds.Add(crown);
                var hat = new Bird(BirdColor.Peach, BirdSex.Male);
                hat.Kit = BirdKit.TopHat;
                hats.Birds.Add(hat);
            }
            b.Branches.Add(crowns);
            b.Branches.Add(hats);
            FlockKit.Assign(b);
            bool kept = true;
            for (int i = 0; i < BranchState.Cap; i++)
            {
                var c = b.Branches[0].Birds[i];
                var h = b.Branches[1].Birds[i];
                if (c.Kit != BirdKit.Crown || !c.KitBinds) kept = false;
                if (h.Kit != BirdKit.TopHat || !h.KitBinds) kept = false;
                if (!c.SameFlock(b.Branches[0].Birds[0])) kept = false;
                if (c.SameFlock(h)) kept = false;
            }
            detail = kept ? "crown and hat stay separate flocks" : "authored kits were overwritten";
            return kept;
        }

        static bool MoveKeepsKit(out string detail)
        {
            // Four, not five: a full color stack is not a hop. Two crowns under
            // two hats. Copying the tip would turn the whole run into hats.
            var b = new Board();
            var src = new BranchState();
            for (int i = 0; i < 4; i++)
            {
                var bird = new Bird(BirdColor.Peach, BirdSex.Male);
                bird.Kit = i < 2 ? BirdKit.Crown : BirdKit.TopHat;
                src.Birds.Add(bird);
            }
            b.Branches.Add(src);
            b.Branches.Add(new BranchState());
            bool moved = b.TryMove(0, 1, out int run);
            int crowns = 0;
            int hats = 0;
            var dst = b.Branches[1];
            for (int i = 0; i < dst.Birds.Count; i++)
            {
                if (dst.Birds[i].Kit == BirdKit.Crown) crowns++;
                else if (dst.Birds[i].Kit == BirdKit.TopHat) hats++;
            }
            detail = "run " + run + " crowns " + crowns + " hats " + hats;
            return moved && run == 4 && crowns == 2 && hats == 2 && src.Count == 0;
        }

        static void CheckLevels(System.Action<string, bool, string> Check)
        {
            var a = LevelData.Open(15);
            var c = LevelData.Open(16);
            Check("gifts-16", BonusBranches.CountOn(a) == WorldBuilder.GiftCount, "gifts " + BonusBranches.CountOn(a));
            Check("gifts-17", BonusBranches.CountOn(c) == WorldBuilder.GiftCount, "gifts " + BonusBranches.CountOn(c));
            Check("level16-empty", EmptyPlain(a) >= 1, "empty plains " + EmptyPlain(a));
            Check("level17-fives", Fives(c), Census(c));
            Check("level16-fives", Fives(a), Census(a));
        }

        static void CheckRestore(System.Action<string, bool, string> Check)
        {
            var b = MonoFlock();
            var expect = BoardValidator.Counts(b);
            b.Branches[0].Broken = true;
            var bad = BoardValidator.Check(b, expect);
            int put = BoardValidator.Restore(b, expect);
            var good = BoardValidator.Check(b, expect);
            Check("restore-short", !bad.Ok && good.Ok && put == 5, bad.Message + " -> " + good.Message + " put " + put);
        }

        // A short census used to park on the gift: it is the perch with the most free seats.
        static void CheckBonusOpensEmpty(System.Action<string, bool, string> Check)
        {
            var b = MonoFlock();
            b.Branches.Add(new BranchState { IsBonus = true, AdLocked = true });
            var expect = BoardValidator.Counts(b);
            b.Branches[0].Birds.Clear();
            b.Branches[0].Shrouded.Clear();
            b.Branches[0].Broken = true;
            BonusBranches.OpenEmpty(b.Branches[1]);
            BoardValidator.Restore(b, expect);
            Check("bonus-opens-empty", b.Branches[1].Empty && !b.Branches[1].AdLocked,
                "gift birds " + b.Branches[1].Count);

            var park = MonoFlock();
            park.Branches.Add(new BranchState { IsBonus = true });
            var before = BoardValidator.Counts(park);
            PestPark.Apply(park, 0, hawk: false);
            Check("bonus-not-a-park", park.Branches[1].Empty && park.Branches[1].IsBonus,
                "gift birds " + park.Branches[1].Count);
            Check("bonus-park-keeps", BoardValidator.Same(BoardValidator.Counts(park), before),
                "birds " + park.RemainingBirds);
        }

        // Stage 1 stays solvable with the two gifts. A locked gift is not a move.
        // An empty open gift is. The ice card uses HasHop, so this is the stuck rule.
        static void CheckStageOne(System.Action<string, bool, string> Check)
        {
            var dawn = LevelData.Open(0);
            var look = GardenSolve.Search(dawn, 30000);
            Check("stage1-winnable", look.Outlook == GardenSolve.Outlook.Winnable,
                look.Outlook + " moves " + look.Moves);
            Check("stage1-has-hop", dawn.HasHop(), "branches " + dawn.Branches.Count);

            int bonus = -1;
            for (int i = 0; i < dawn.Branches.Count; i++)
            {
                if (!dawn.Branches[i].IsBonus) continue;
                bonus = i;
                break;
            }
            bool locked = bonus >= 0 && dawn.Branches[bonus].AdLocked && dawn.Branches[bonus].Empty;
            bool ontoLocked = false;
            if (bonus >= 0)
            {
                for (int from = 0; from < dawn.Branches.Count; from++)
                    if (dawn.CanMove(from, bonus, out _)) ontoLocked = true;
            }
            Check("stage1-locked-gift", locked && !ontoLocked, "bonus " + bonus);

            var only = new Board();
            var src = new BranchState();
            src.Birds.Add(new Bird(BirdColor.Ruby, LevelData.SexFor(BirdColor.Ruby)));
            only.Branches.Add(src);
            only.Branches.Add(new BranchState { IsBonus = true, AdLocked = true });
            Check("locked-bonus-not-a-move", !only.HasHop() && !only.CanMove(0, 1, out _),
                "hops");
            BonusBranches.OpenEmpty(only.Branches[1]);
            Check("empty-bonus-is-a-move", only.Branches[1].Empty && only.HasHop() && only.CanMove(0, 1, out _),
                "gift birds " + only.Branches[1].Count);

            int onto = 0;
            GardenSolve.Outlook openedOutlook = GardenSolve.Outlook.Tangled;
            int openedMoves = 0;
            if (bonus >= 0)
            {
                BonusBranches.OpenEmpty(dawn.Branches[bonus]);
                for (int from = 0; from < dawn.Branches.Count; from++)
                    if (dawn.CanMove(from, bonus, out _)) onto++;
                var opened = GardenSolve.Search(dawn, 30000);
                openedOutlook = opened.Outlook;
                openedMoves = opened.Moves;
            }
            Check("stage1-open-bonus-dest", onto > 0 && dawn.HasHop(), "onto " + onto);
            Check("stage1-open-winnable", openedOutlook == GardenSolve.Outlook.Winnable,
                openedOutlook + " moves " + openedMoves);
        }

        // Continue-after-freeze opens the next locked gift through BonusBranches,
        // the same claim UnlockBonus uses. Cancel and a failed show pay nothing.
        // A full same-color stack is not a hop even onto an empty gift, so the
        // stuck board is two short different-color limbs.
        static void CheckFreezeContinue(System.Action<string, bool, string> Check)
        {
            var b = StuckPair();
            var claimed = new bool[WorldBuilder.GiftCount];
            bool stuck = !b.HasHop() && b.FindCollect() < 0 && b.Branches.Count == 4;
            int first = BonusBranches.ContinueIndex(b, claimed, 0);
            int grant = BonusBranches.Claim(b, claimed, first);
            int ord = BonusBranches.Ordinal(b, first);
            bool opened = stuck && grant == 1 && first == 2
                && b.Branches[first].IsBonus && b.Branches[first].Empty && !b.Branches[first].AdLocked
                && ord == 0 && claimed[ord]
                && b.Branches[3].AdLocked
                && b.HasHop() && b.Branches.Count == 4;
            Check("freeze-continue-branch", opened, "first " + first + " grant " + grant);

            int again = BonusBranches.Claim(b, claimed, first);
            Check("freeze-one-grant", again == 0 && b.Branches[first].Empty && b.Branches.Count == 4,
                "again " + again);

            int second = BonusBranches.ContinueIndex(b, claimed, first);
            int grant2 = BonusBranches.Claim(b, claimed, second);
            int ord2 = BonusBranches.Ordinal(b, second);
            bool next = second == 3 && grant2 == 1 && ord2 == 1 && claimed[ord2]
                && b.Branches[second].Empty && !b.Branches[second].AdLocked;
            Check("freeze-continue-next", next, "second " + second + " grant " + grant2);

            var tapped = StuckPair();
            var tapClaimed = new bool[WorldBuilder.GiftCount];
            int pick = BonusBranches.ContinueIndex(tapped, tapClaimed, 3);
            int tapGrant = BonusBranches.Claim(tapped, tapClaimed, pick);
            bool honored = pick == 3 && tapGrant == 1 && !tapped.Branches[3].AdLocked
                && tapped.Branches[2].AdLocked && tapped.HasHop();
            Check("freeze-tapped-gift", honored, "pick " + pick);

            var broken = StuckPair();
            broken.Branches[2].Broken = true;
            var brokenClaimed = new bool[WorldBuilder.GiftCount];
            int skipped = BonusBranches.ContinueIndex(broken, brokenClaimed, 2);
            int skipGrant = BonusBranches.Claim(broken, brokenClaimed, skipped);
            bool skips = skipped == 3 && skipGrant == 1
                && broken.Branches[2].Broken && broken.Branches[2].AdLocked
                && !broken.Branches[3].AdLocked;
            Check("freeze-skips-broken", skips, "picked " + skipped);

            var cancel = StuckPair();
            var none = new bool[WorldBuilder.GiftCount];
            int peek = BonusBranches.ContinueIndex(cancel, none, -1);
            bool untouched = peek == 2 && cancel.Branches[2].AdLocked && cancel.Branches[3].AdLocked
                && !cancel.HasHop() && !none[0] && !none[1];
            bool noPay = !BonusBranches.ContinuePays(false, false, true)
                && !BonusBranches.ContinuePays(false, true, true);
            bool pay = BonusBranches.ContinuePays(true, false, true)
                && BonusBranches.ContinuePays(true, true, true)
                && BonusBranches.ContinuePays(false, true, false);
            Check("freeze-cancel", untouched && noPay && pay, "peek " + peek);
        }

        static Board StuckPair()
        {
            var b = new Board();
            var ruby = new BranchState();
            var teal = new BranchState();
            for (int i = 0; i < 3; i++)
            {
                ruby.Birds.Add(new Bird(BirdColor.Ruby, LevelData.SexFor(BirdColor.Ruby)));
                teal.Birds.Add(new Bird(BirdColor.Teal, LevelData.SexFor(BirdColor.Teal)));
            }
            b.Branches.Add(ruby);
            b.Branches.Add(teal);
            b.Branches.Add(new BranchState { IsBonus = true, AdLocked = true });
            b.Branches.Add(new BranchState { IsBonus = true, AdLocked = true });
            return b;
        }

        static Board MonoFlock()
        {
            var b = new Board();
            var br = new BranchState();
            for (int i = 0; i < BranchState.Cap; i++)
                br.Birds.Add(new Bird(BirdColor.Ruby, LevelData.SexFor(BirdColor.Ruby)));
            b.Branches.Add(br);
            b.Live[0] = BirdColor.Ruby;
            return b;
        }

        static int Live(Board b)
        {
            int n = 0;
            for (int i = 0; i < b.Branches.Count; i++)
                if (!b.Branches[i].Broken) n++;
            return n;
        }

        static int BrokenCount(Board b)
        {
            int n = 0;
            for (int i = 0; i < b.Branches.Count; i++)
                if (b.Branches[i].Broken) n++;
            return n;
        }

        static int ClaimedOpen(Board b)
        {
            int n = 0;
            for (int i = 0; i < b.Branches.Count; i++)
                if (b.Branches[i].IsBonus && !b.Branches[i].AdLocked) n++;
            return n;
        }

        static int EmptyPlain(Board b)
        {
            int n = 0;
            for (int i = 0; i < b.Branches.Count; i++)
                if (!b.Branches[i].IsBonus && !b.Branches[i].Broken && b.Branches[i].Empty) n++;
            return n;
        }

        // A right-column clear packs that column, sign included. The other
        // column is still full, so its gift stays put.
        static void CheckBonusCloses(System.Action<string, bool, string> Check)
        {
            var b = LevelData.Open(0);
            var before = new List<GardenFit.Spot>();
            GardenFit.Collect(b, null, before);
            int broken = -1;
            int below = -1;
            int plain = 0;
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var st = b.Branches[i];
                if (st == null || st.IsBonus) continue;
                if ((plain & 1) == 1)
                {
                    if (broken < 0) broken = i;
                    else if (below < 0) below = i;
                }
                plain++;
            }
            int rightGift = -1;
            int leftGift = -1;
            int giftOrd = 0;
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var st = b.Branches[i];
                if (st == null || !st.IsBonus) continue;
                if ((giftOrd & 1) == 1) rightGift = i;
                else leftGift = i;
                giftOrd++;
            }
            float gift0 = SpotY(before, rightGift);
            float below0 = SpotY(before, below);
            float left0 = SpotY(before, leftGift);
            if (broken >= 0) b.Branches[broken].Broken = true;
            var after = new List<GardenFit.Spot>();
            GardenFit.Collect(b, null, after);
            float gift1 = SpotY(after, rightGift);
            float below1 = SpotY(after, below);
            float left1 = SpotY(after, leftGift);
            float rise = gift1 - gift0;
            float mate = below1 - below0;
            bool lowest = rightGift >= 0 && LowestInColumn(after, rightGift);
            bool ok = broken >= 0 && below >= 0 && rightGift >= 0 && leftGift >= 0
                && rise > 0.2f && Mathf.Abs(rise - mate) < 0.0001f
                && Mathf.Abs(left1 - left0) < 0.0001f && lowest;
            Check("bonus-closes", ok, "rise " + rise.ToString("0.00") + " mate " + mate.ToString("0.00"));
        }

        static float SpotY(List<GardenFit.Spot> spots, int index)
        {
            if (spots == null || index < 0) return float.NaN;
            for (int i = 0; i < spots.Count; i++)
                if (spots[i].Index == index) return spots[i].Pos.y;
            return float.NaN;
        }

        static bool LowestInColumn(List<GardenFit.Spot> spots, int index)
        {
            int col = -1;
            float y = 0f;
            bool found = false;
            for (int i = 0; i < spots.Count; i++)
            {
                if (spots[i].Index != index) continue;
                col = spots[i].Column;
                y = spots[i].Pos.y;
                found = true;
                break;
            }
            if (!found) return false;
            for (int i = 0; i < spots.Count; i++)
            {
                if (spots[i].Column != col) continue;
                if (spots[i].Pos.y < y - 0.0001f) return false;
            }
            return true;
        }

        static int BonusSpots(Board b, List<GardenFit.Spot> spots)
        {
            int n = 0;
            if (b == null || spots == null) return 0;
            for (int i = 0; i < spots.Count; i++)
            {
                int ix = spots[i].Index;
                if ((uint)ix >= (uint)b.Branches.Count) continue;
                var st = b.Branches[ix];
                if (st != null && st.IsBonus) n++;
            }
            return n;
        }

        static bool Packed(Board b)
        {
            var spots = new List<GardenFit.Spot>();
            GardenFit.Collect(b, null, spots);
            if (!GardenFit.RowsPacked(spots, 0) || !GardenFit.RowsPacked(spots, 1)) return false;
            for (int i = 0; i < spots.Count; i++)
                if (spots[i].Scale != GardenFit.LimbScale) return false;
            return true;
        }

        static bool Seated(Board b)
        {
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var br = b.Branches[i];
                if (br.Broken && br.Count > 0) return false;
                if (!br.Broken && br.Count > BranchState.Cap) return false;
            }
            return true;
        }

        static bool Fives(Board b)
        {
            var n = BoardValidator.Counts(b);
            for (int c = 0; c < Palette.Shipped && c < n.Length; c++)
                if (n[c] < BranchState.Cap || n[c] % BranchState.Cap != 0) return false;
            return true;
        }

        static string Census(Board b)
        {
            var n = BoardValidator.Counts(b);
            var sb = new StringBuilder();
            for (int c = 0; c < n.Length; c++)
            {
                if (c > 0) sb.Append(' ');
                sb.Append((BirdColor)c).Append(n[c]);
            }
            return sb.ToString();
        }
    }
}
#endif
