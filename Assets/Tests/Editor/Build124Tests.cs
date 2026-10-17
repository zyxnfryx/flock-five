#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // b124: power tiles score 0, two-turn Freeze, glove near start, poker first-hand banner, claimed Daily button.
    public static class Build124Tests
    {
        const string Out = "/tmp/flock-five-b124-tests.txt";

        static int[] Fill(int n, int v) { var a = new int[n]; for (int i = 0; i < n; i++) a[i] = v; return a; }

        static bool Play(BadgerFight f, int honey, BadgerPower power, out BadgerRound r)
        {
            r = default;
            if (f.BossPick() < 0) return false;
            int ix = -1;
            for (int i = 0; i < f.PlayerCount; i++) if (f.PlayerOpen(i) && f.PlayerHoney(i) == honey) { ix = i; break; }
            if (ix < 0 || !f.PlayerPick(ix)) return false;
            if (power != BadgerPower.None && !f.TryPower(power)) return false;
            return f.Resolve(out r);
        }

        [MenuItem("Flock Five/Build 124 Tests")]
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            void Check(string name, bool ok, string detail)
            {
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build124] " + (ok ? "PASS " : "FAIL ") + name + " " + detail);
            }

            // (1) power tile scores 0 for both.
            bool zero = true;
            foreach (var pw in new[] { BadgerPower.HotSauce, BadgerPower.FreezeSpray })
            {
                BadgerFight.ApplyRound(2, 5, pw, out int pf, out int bf, out int pg, out int bg);
                zero &= pf == 0 && bf == 0 && pg == 0 && bg == 0;
            }
            BadgerFight.ApplyRound(2, 5, BadgerPower.X2, out int xf, out _, out _, out _);
            Check("power-tile-scores-zero-badger-blocked", zero && xf == 4, "");
            var tut = BadgerCopy.PowerIntroLine(BadgerPower.HotSauce, false) + BadgerCopy.PowerIntroLine(BadgerPower.FreezeSpray, false);
            Check("tutorial-text-says-tile-scores-0", tut.Split(new[] { "scores 0" }, System.StringSplitOptions.None).Length == 3, tut);

            // (2) two-turn Freeze.
            SaveCoins(5000);
            var fight = new BadgerFight(1, 22, Fill(16, 2), Fill(16, 5));
            bool t1 = Play(fight, 2, BadgerPower.FreezeSpray, out var r1)
                && r1.FreezeBeat == 1 && r1.BossFinal == 0 && r1.BossGained == 0 && r1.PlayerFinal == 0 && r1.PlayerGained == 0
                && fight.BossFrozenNow && fight.BossScore == 0;
            Check("freeze-turn1-badger-frozen-no-action", t1, "");
            bool t2 = Play(fight, 2, BadgerPower.None, out var r2)
                && r2.FreezeBeat == 2 && r2.BossFinal == 0 && r2.BossGained == 0 && r2.PlayerGained == 2
                && !fight.BossFrozenNow && fight.BossScore == 0;
            Check("freeze-turn2-breaks-free-scores-0", t1 && t2, "");
            bool t3 = Play(fight, 2, BadgerPower.None, out var r3) && r3.FreezeBeat == 0 && r3.BossGained == 5 && fight.BossScore == 5;
            Check("freeze-turn3-acts-normally", t3, "");
            var sauce = new BadgerFight(1, 23, Fill(16, 2), Fill(16, 5));
            SaveCoins(5000);
            bool sOk = Play(sauce, 2, BadgerPower.HotSauce, out var s1) && s1.FreezeBeat == 0 && !sauce.BossFrozenNow
                && Play(sauce, 2, BadgerPower.None, out var s2) && s2.FreezeBeat == 0 && s2.BossGained == 5;
            Check("hot-sauce-is-not-a-freeze-beat", sOk, "");
            Check("freeze-verdict-lines", BadgerCopy.VerdictLine(r1) == BadgerCopy.FrozenSolid && BadgerCopy.VerdictLine(r2) == BadgerCopy.BreakFree, "");

            // PR 159 follow-up (1): Hot Sauce is disabled while the badger is frozen (both beats), allowed after.
            SaveCoins(5000);
            var ice = new BadgerFight(1, 31, Fill(16, 2), Fill(16, 5));
            bool beat1 = ice.BossPick() >= 0 && ice.PlayerPick(0) && !ice.PowerFrozenOut(BadgerPower.HotSauce)
                && ice.TryPower(BadgerPower.FreezeSpray)
                && ice.PowerFrozenOut(BadgerPower.HotSauce) && !ice.TryPower(BadgerPower.HotSauce)
                && !ice.PowerFrozenOut(BadgerPower.X2) && ice.Resolve(out var ib1) && ib1.FreezeBeat == 1;
            Check("hot-sauce-disabled-on-frozen-beat", beat1, "");
            int coinsBefore = Purse.Coins;
            bool beat2 = ice.BossFrozenNow && ice.PowerFrozenOut(BadgerPower.HotSauce)
                && ice.BossPick() >= 0 && ice.PlayerPick(1)
                && !ice.TryPower(BadgerPower.HotSauce) && Purse.Coins == coinsBefore && ice.PowerReady(BadgerPower.HotSauce)
                && ice.Resolve(out var ib2) && ib2.FreezeBeat == 2 && ib2.Power == BadgerPower.None;
            Check("hot-sauce-refused-on-break-free-beat", beat2, "");
            bool after = !ice.BossFrozenNow && !ice.PowerFrozenOut(BadgerPower.HotSauce)
                && ice.BossPick() >= 0 && ice.PlayerPick(2) && ice.TryPower(BadgerPower.HotSauce)
                && ice.Resolve(out var ib3) && ib3.Power == BadgerPower.HotSauce && ib3.FreezeBeat == 0;
            Check("hot-sauce-allowed-after-freeze", after, "");
            Check("freeze-still-two-turns-and-hot-sauce-one", t1 && t2 && t3 && sOk, "");
            Check("honey-values-2-3-5", Hive.HoneyOfFinish(BeeFinish.Normal) == 2 && Hive.HoneyOfFinish(BeeFinish.Holo) == 3
                && Hive.HoneyOfFinish(BeeFinish.InverseRainbow) == 5, "");

            // (2) Freeze demo reaches the break-free beat: caption, back hold, and the shared dim/click wiring.
            string badgerApp = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Badger.cs");
            Check("freeze-demo-break-free-caption-and-back-hold",
                r2.FreezeBeat == 2 && BadgerCopy.VerdictLine(r2) == BadgerCopy.BreakFree
                && BadgerCopy.FreezeBreakFreeWait.Contains(BadgerCopy.HotSauceFrozen.Replace("\n", " "))
                && badgerApp.Contains("BadgerCopy.FreezeBreakFreeWait")
                && badgerApp.Contains("BadgerFreezeDemoHolds()")
                && badgerApp.Contains("_bgFight.PowerFrozenOut(p)"), "");

            // (3) poker Back cue only while the hint is not live.
            Check("poker-back-cue-only-when-hint-not-live",
                FlockFiveApp.PokerBackCueShows(true, false) && !FlockFiveApp.PokerBackCueShows(true, true)
                && !FlockFiveApp.PokerBackCueShows(false, false) && !FlockFiveApp.PokerBackCueShows(false, true), "");

            // (4) glove near start, per device class.
            var devices = new[] { new Vector2(1170, 2532), new Vector2(2048, 2732), new Vector2(1080, 2400) };
            string[] names = { "iphone", "ipad", "android" };
            for (int d = 0; d < devices.Length; d++)
            {
                float w = devices[d].x, h = devices[d].y;
                float s = DeviceFit.Ui(w, h);
                float dh = FlockFiveApp.GloveAuditDh(s);
                var safe = new Rect(0f, 60f * s, w, h - 120f * s);
                var aim = new Vector2(w * 0.3f, h * 0.4f);
                var far = aim + new Vector2(dh * 6f, -dh * 2f);
                var p = FlockFiveApp.GloveNearStartPoint(far, aim, s, safe);
                float cap = 1.6f * dh;
                bool capped = (p - aim).magnitude <= cap + 0.5f;
                var near = aim + new Vector2(dh * 0.9f, 0f);
                bool keepsNear = FlockFiveApp.GloveNearStartPoint(near, aim, s, safe) == near;
                var edge = new Vector2(w - 2f, h * 0.5f);
                var pe = FlockFiveApp.GloveNearStartPoint(edge, new Vector2(w - dh, h * 0.5f), s, safe);
                bool onScreen = pe.x <= safe.xMax - dh * 0.42f + 0.01f && pe.x >= safe.xMin;
                Check("glove-near-start-" + names[d], capped && keepsNear && onScreen, "cap " + cap + " got " + (p - aim).magnitude);
                var entry = FlockFiveApp.GloveMiddleRightEntry(safe);
                Check("poker-back-glove-entry-middle-right-" + names[d], Mathf.Approximately(entry.x, safe.xMax) && Mathf.Approximately(entry.y, safe.center.y), "");
                // (5) banner covers the whole bet-control seat, full width.
                var seat = new Rect(w * 0.1f, h * 0.80f, w * 0.8f, h * 0.08f);
                var band = CaptionLayout.BandCovering(new Rect(0, h * 0.78f, w, 50f * s), seat, w, 6f * s);
                bool covers = band.x <= 0f && band.width >= w && band.y <= seat.y && band.yMax >= seat.yMax;
                Check("poker-back-banner-covers-bet-seat-" + names[d], covers, band.ToString());
            }

            // (5) source wiring.
            string app = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs");
            Check("poker-bar-held-while-first-hand-result",
                app.Contains("if (PokerFirstHandResultHoldsBetBar()) return true;")
                && app.Contains("_home == HomeFace.Poker && !PokerBackDone() && BirdPoker.PhaseNow == BirdPoker.Phase.Drawn"), "");
            Check("poker-back-uses-shared-banner-spot", CaptionStyles.IsBanner("poker.back") && app.Contains("\"poker.back\""), "");
            Check("dealer-button-held-with-bar", app.Contains("|| PokerFirstHandResultHoldsBetBar()) return false;"), "");

            // (6) claimed Daily: no burst, no squish; unclaimed still bursts.
            string daily = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.DailyBonus.cs");
            Check("daily-claimed-no-burst-unclaimed-bursts",
                !FlockFiveApp.DailyClaimBursts(false, true) && FlockFiveApp.DailyClaimBursts(true, false) && FlockFiveApp.DailyClaimBursts(false, false), "");
            Check("daily-claimed-keeps-cowbell-no-squish", !daily.Contains("_claimClunk") && !app.Contains("_claimClunk")
                && daily.Contains("SfxLibrary.Cowbell();") && app.Contains("bool burst = true"), "");
        }

        static void SaveCoins(int n)
        {
            n = Mathf.Max(0, n);
            int c = Purse.Coins;
            if (c < n) Purse.Credit(n - c);
            else if (c > n) Purse.TrySpend(c - n);
        }
    }
}
#endif
