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
            CheckLevels(Check);
            CheckRestore(Check);
            CheckBonusOpensEmpty(Check);
            CheckStageOne(Check);

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
            Check("layout-two-gifts", BonusSpots(spots) == 2, "gifts " + BonusSpots(spots));

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

        static int BonusSpots(List<GardenFit.Spot> spots)
        {
            int n = 0;
            for (int i = 0; i < spots.Count; i++)
                if (spots[i].Column == 2) n++;
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
