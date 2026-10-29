#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b119 (Brandon): Five of a Kind trophy board — record, migration, chip gate, layout.
    // Run: Unity -batchmode -quit -projectPath . -executeMethod FlockFive.Editor.Build119FivesBoardTests.Run
    public static class Build119FivesBoardTests
    {
        const string Out = "/tmp/flock-five-b119-fives-tests.txt";

        static BirdPoker.Card C(BirdColor c, BirdSex s) => BirdPoker.Card.Of(c, s);
        static BirdPoker.Card W() => BirdPoker.Card.MakeWild();

        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail)
            {
                string line = (ok ? "PASS  " : "FAIL  ") + name + "  " + detail;
                if (ok) pass++; else fail++;
                sb.AppendLine(line);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build119-fives] " + line);
            }

            // 1. Slot map: 15 looks + FIVE WILDS, ids unique and in range.
            var seen = new bool[BirdPoker.PunchKinds];
            bool mapOk = BirdPoker.PunchKinds == 16 && BirdPoker.WildKind == 15;
            for (int c = 0; c < Palette.Max; c++)
                for (int x = 0; x < 3; x++)
                {
                    int k = BirdPoker.KindId((BirdColor)c, (BirdSex)x);
                    if ((uint)k >= 15u || seen[k]) mapOk = false; else seen[k] = true;
                    BirdPoker.KindParts(k, out BirdColor bc, out BirdSex bs);
                    if ((int)bc != c || (int)bs != x) mapOk = false;
                }
            Check("16 slots: 5 colors x 3 looks + FIVE WILDS", mapOk, "");

            // 2. Resolve: natural, wild-assisted, five wilds.
            int kind;
            var nat = new[] { C(BirdColor.Teal, BirdSex.Male), C(BirdColor.Teal, BirdSex.Male), C(BirdColor.Teal, BirdSex.Male), C(BirdColor.Teal, BirdSex.Male), C(BirdColor.Teal, BirdSex.Male) };
            bool natR = BirdPoker.ResolveFiveKindForTests(nat, out kind) && kind == BirdPoker.KindId(BirdColor.Teal, BirdSex.Male);
            BirdPoker.Evaluate(nat, out bool isNat);
            Check("natural five resolves to its slot and ranks natural", natR && isNat, "kind=" + kind);
            var wa = new[] { C(BirdColor.Gold, BirdSex.Female), W(), C(BirdColor.Gold, BirdSex.Female), W(), C(BirdColor.Gold, BirdSex.Female) };
            bool waR = BirdPoker.ResolveFiveKindForTests(wa, out kind) && kind == BirdPoker.KindId(BirdColor.Gold, BirdSex.Female);
            Check("wild-assisted five fills the same slot, rank FIVE OF A KIND", waR && BirdPoker.Evaluate(wa, out _) == BirdPoker.Rank.FiveWild, "kind=" + kind);
            var fw = new[] { W(), W(), W(), W(), W() };
            Check("five wilds -> slot 15", BirdPoker.ResolveFiveKindForTests(fw, out kind) && kind == BirdPoker.WildKind, "kind=" + kind);

            // 3. Record: first day sticks, count climbs, natural is sticky.
            var rec = new BirdPoker.FiveRecord[BirdPoker.PunchKinds];
            BirdPoker.ApplyFive(rec, 4, false, 20400);
            BirdPoker.ApplyFive(rec, 4, true, 20410);
            BirdPoker.ApplyFive(rec, 4, false, 20420);
            Check("repeat hits count up, first day kept, natural sticky",
                rec[4].Count == 3 && rec[4].FirstDay == 20400 && rec[4].Natural, rec[4].Count + "/" + rec[4].FirstDay);

            // 4. Encode/decode round trip, junk tolerant.
            BirdPoker.ApplyFive(rec, 15, false, 20421);
            string enc = BirdPoker.EncodeFives(rec);
            var dec = BirdPoker.DecodeFives(enc);
            Check("encode/decode round trip", enc == "4:3:20400:1;15:1:20421:0" && dec[4].Count == 3 && dec[4].Natural && dec[15].FirstDay == 20421, enc);
            var junk = BirdPoker.DecodeFives("x;99:1:1:0;3:0:5:0;2:2:-7:1;;");
            Check("decode drops junk, keeps valid, negative day = earlier",
                junk[2].Count == 2 && junk[2].FirstDay == BirdPoker.FiveDayEarlier && junk[3].Count == 0, BirdPoker.EncodeFives(junk));

            // 5. Migration: punch.v1 slots -> count 1, date earlier, nothing else.
            var punched = new bool[BirdPoker.PunchKinds];
            punched[0] = punched[7] = punched[15] = true;
            var mig = BirdPoker.MigrateFromPunches(punched);
            int lit = 0; bool migOk = true;
            for (int i = 0; i < mig.Length; i++)
            {
                if (mig[i].Count > 0) lit++;
                if (punched[i] && (mig[i].Count != 1 || mig[i].FirstDay != BirdPoker.FiveDayEarlier || mig[i].Natural)) migOk = false;
            }
            Check("migration: 3 punches -> 3 slots, count 1, 'earlier'", migOk && lit == 3 && BirdPoker.DayLabel(mig[0].FirstDay) == "earlier", "lit=" + lit);
            Check("day label", BirdPoker.DayLabel(0) == "Jan 1" && BirdPoker.DayLabel(20755) == "Oct 29", BirdPoker.DayLabel(20755));
            Check("caption", FlockFiveApp.FivesCaption(new BirdPoker.FiveRecord { Count = 5, FirstDay = 20755, Natural = true }) == "×5 · Oct 29 ★"
                && FlockFiveApp.FivesCaption(mig[0]) == "×1 · earlier", FlockFiveApp.FivesCaption(mig[0]));

            // 6. Live prefs migration (restores prefs afterward).
            string kp = "flockfive.poker.punch.v1", kf = BirdPoker.PrefFives;
            bool hadP = PlayerPrefs.HasKey(kp), hadF = PlayerPrefs.HasKey(kf);
            string oldP = PlayerPrefs.GetString(kp, ""), oldF = PlayerPrefs.GetString(kf, "");
            int oldPx = PlayerPrefs.GetInt(kp + ".x", 0), oldFx = PlayerPrefs.GetInt(kf + ".x", 0);
            bool hadPx = PlayerPrefs.HasKey(kp + ".x"), hadFx = PlayerPrefs.HasKey(kf + ".x");
            try
            {
                PlayerPrefs.DeleteKey(kf); PlayerPrefs.DeleteKey(kf + ".x");
                PlayerPrefs.DeleteKey(kp + ".x"); PlayerPrefs.SetString(kp, "1,9,15");
                BirdPoker.ReloadFivesForTests();
                bool ok = BirdPoker.FivesFound() == 3 && BirdPoker.Five(9).Count == 1 && BirdPoker.Five(9).FirstDay == BirdPoker.FiveDayEarlier
                    && PlayerPrefs.HasKey(kf) && BirdPoker.FivesHits() == 3;
                Check("first read migrates punch.v1 into fives.v1 and saves", ok, PlayerPrefs.GetString(kf, "?"));
                BirdPoker.ReloadFivesForTests();
                Check("second read uses fives.v1 (no double credit)", BirdPoker.FivesHits() == 3, "hits=" + BirdPoker.FivesHits());
            }
            finally
            {
                if (hadP) PlayerPrefs.SetString(kp, oldP); else PlayerPrefs.DeleteKey(kp);
                if (hadPx) PlayerPrefs.SetInt(kp + ".x", oldPx); else PlayerPrefs.DeleteKey(kp + ".x");
                if (hadF) PlayerPrefs.SetString(kf, oldF); else PlayerPrefs.DeleteKey(kf);
                if (hadFx) PlayerPrefs.SetInt(kf + ".x", oldFx); else PlayerPrefs.DeleteKey(kf + ".x");
                PlayerPrefs.Save();
                BirdPoker.ReloadFivesForTests();
            }

            // 7. Chip gate: tappable only with no hand in play.
            Check("chip tappable when idle", FlockFiveApp.FivesChipTappable(BirdPoker.Phase.Idle, false, false, false), "");
            Check("chip locked while dealt / drawn / animating / stamp / pay open",
                !FlockFiveApp.FivesChipTappable(BirdPoker.Phase.Dealt, false, false, false)
                && !FlockFiveApp.FivesChipTappable(BirdPoker.Phase.Drawn, false, false, false)
                && !FlockFiveApp.FivesChipTappable(BirdPoker.Phase.Idle, true, false, false)
                && !FlockFiveApp.FivesChipTappable(BirdPoker.Phase.Idle, false, true, false)
                && !FlockFiveApp.FivesChipTappable(BirdPoker.Phase.Idle, false, false, true), "");
            Check("chip label", FlockFiveApp.FivesChipLabel(6) == "★ FIVES 6/16", FlockFiveApp.FivesChipLabel(6));

            // 8. Chip sits left of PAY TABLE, same row; board fits each device.
            var tab = new Rect(300f, 120f, 86f, 24f);
            var chip = FlockFiveApp.PokerFivesChipBox(tab, 1f);
            Check("chip left of PAY TABLE, same row and size", chip.xMax < tab.x && Mathf.Approximately(chip.y, tab.y) && Mathf.Approximately(chip.width, tab.width), chip.ToString());
            var sizes = new[] { new Vector2(402f, 874f), new Vector2(820f, 1180f), new Vector2(360f, 780f) };
            bool fits = true; string fd = "";
            foreach (var z in sizes)
            {
                var g = FlockFiveApp.LayoutFivesBoard(z.x, z.y, new Rect(0f, 40f, z.x, z.y - 74f), 1f);
                float wildBottom = g.GridTop + Palette.Max * g.RowH + g.CardH;
                bool f = g.Panel.xMin >= 0f && g.Panel.xMax <= z.x && wildBottom <= g.Panel.yMax - 30f && g.Card > 30f && g.Card <= g.ColW;
                fd += z + (f ? " ok " : " BAD ") + g.Card.ToString("0") + "; ";
                fits &= f;
            }
            Check("board (6 rows) fits iPhone 18 Pro / iPad A16 / Samsung", fits, fd);

            // 9. Wiring: every five-of-a-kind records (not only fresh punches); FULLCARD never clears punch.v1.
            string bp = File.ReadAllText("Assets/_Project/Scripts/App/BirdPoker.cs");
            int settle = bp.IndexOf("static void SettleRound()");
            int rec5 = bp.IndexOf("RecordFive(fiveKind, LastRank == Rank.NaturalFive)");
            int tryP = bp.IndexOf("LastPunchFresh = TryPunch(Hand, out kind)");
            Check("SettleRound records every five before the punch check", settle > 0 && rec5 > settle && rec5 < tryP, "");
            string bpRuntime = bp.Substring(0, bp.IndexOf("#if UNITY_EDITOR\n        public static void SeedRng") > 0 ? bp.IndexOf("#if UNITY_EDITOR\n        public static void SeedRng") : bp.Length);
            Check("no runtime path clears punches (FULLCARD keeps the card)", !System.Text.RegularExpressions.Regex.IsMatch(bpRuntime, @"_punched\[[^\]]+\]\s*=\s*false"), "");
            string app = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs");
            string modal = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Modal.cs");
            Check("screen wiring: tick, chip, board, modal", app.Contains("TickPokerFives(PokerPayTabRect(below, s), s);")
                && app.Contains("DrawPokerFivesChip(PokerPayTabRect(below, s), s);") && app.Contains("DrawPokerFivesBoard(s);")
                && modal.Contains("ModalBlock.Id.PokerFives"), "");

            string end = fail == 0 ? "BUILD119_FIVES_OK " + pass : "BUILD119_FIVES_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            if (fail > 0) throw new System.Exception(end);
        }
    }
}
#endif
