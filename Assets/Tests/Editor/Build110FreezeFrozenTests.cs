#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // b110: Freeze Spray is disabled while the badger is frozen (both beats), same pattern as Hot Sauce (b127):
    // engine refuses, greyed + ice badge, tap says why, nothing stuck after the fight.
    public static class Build110FreezeFrozenTests
    {
        const string Out = "/tmp/flock-five-b110-freeze-frozen-tests.txt";

        static int[] Fill(int n, int v) { var a = new int[n]; for (int i = 0; i < n; i++) a[i] = v; return a; }

        static bool Play(BadgerFight f, BadgerPower power, out BadgerRound r)
        {
            r = default;
            if (f.BossPick() < 0) return false;
            int ix = -1;
            for (int i = 0; i < f.PlayerCount; i++) if (f.PlayerOpen(i)) { ix = i; break; }
            if (ix < 0 || !f.PlayerPick(ix)) return false;
            if (power != BadgerPower.None && !f.TryPower(power)) return false;
            return f.Resolve(out r);
        }

        [MenuItem("Flock Five/Build 110 Freeze Frozen Tests")]
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
                Debug.Log("[build110ff] " + line);
            }

            int coins = Purse.Coins;
            if (coins < 5000) Purse.Credit(5000 - coins);

            // ---- engine: Freeze Spray refused on both frozen beats; x2 / x3 unaffected ----
            var f = new BadgerFight(1, 31, Fill(16, 2), Fill(16, 5), false);
            Check("fresh-freeze-not-frozen-out", !f.PowerFrozenOut(BadgerPower.FreezeSpray) && f.PowerReady(BadgerPower.FreezeSpray), "");
            bool b1 = f.BossPick() >= 0 && f.PlayerPick(0) && f.TryPower(BadgerPower.FreezeSpray)
                && f.PowerFrozenOut(BadgerPower.FreezeSpray) && !f.TryPower(BadgerPower.FreezeSpray)
                && !f.PowerFrozenOut(BadgerPower.X2) && !f.PowerFrozenOut(BadgerPower.X3)
                && f.Resolve(out var r1) && r1.FreezeBeat == 1;
            Check("beat1-frozen-turn-freeze-refused", b1, "");
            int before = Purse.Coins;
            bool b2 = f.BossFrozenNow && f.PowerFrozenOut(BadgerPower.FreezeSpray)
                && f.BossPick() >= 0 && f.PlayerPick(1)
                && !f.TryPower(BadgerPower.FreezeSpray) && Purse.Coins == before
                && f.Resolve(out var r2) && r2.FreezeBeat == 2 && r2.Power == BadgerPower.None;
            Check("beat2-break-free-turn-freeze-refused-no-spend", b2, "");
            Check("thaw-freeze-no-longer-frozen-out", !f.BossFrozenNow && !f.PowerFrozenOut(BadgerPower.FreezeSpray), "");

            // Refusal does not depend on the used flag: a fresh fight armed with Hot Sauce is not frozen, a frozen one is.
            var g = new BadgerFight(1, 41, Fill(16, 2), Fill(16, 5), false);
            bool gOk = g.BossPick() >= 0 && g.PlayerPick(0) && g.TryPower(BadgerPower.HotSauce) && !g.PowerFrozenOut(BadgerPower.FreezeSpray);
            Check("hot-sauce-does-not-freeze-out-freeze", gOk, "");

            // PlayerPlay with Freeze on the break-free turn is refused and leaves the tile open.
            var h = new BadgerFight(1, 51, Fill(16, 2), Fill(16, 5), false);
            bool h1 = Play(h, BadgerPower.FreezeSpray, out var hr1) && hr1.FreezeBeat == 1;
            bool h2 = h.BossPick() >= 0 && !h.PlayerPlay(1, BadgerPower.FreezeSpray) && h.PlayerOpen(1);
            Check("playerplay-freeze-refused-tile-stays-open", h1 && h2, "");

            // ---- UI state: disabled look beats the USED look for Freeze only while frozen ----
            var ui = new BadgerFrozenUi(BadgerPower.FreezeSpray);
            const float t0 = 100f;
            var d = new BadgerFight(1, 22, Fill(16, 2), Fill(16, 5), false);
            Check("before-freeze-not-disabled", !BadgerFrozenUi.Disabled(d, BadgerPower.FreezeSpray, false) && ui.Tap(d, BadgerPower.FreezeSpray, t0) == null && !ui.ToastLive(t0), "");
            d.BossPick(); d.PlayerPick(0);
            bool armed = d.TryPower(BadgerPower.FreezeSpray);
            Check("beat1-disabled-even-though-used", armed && BadgerFrozenUi.Disabled(d, BadgerPower.FreezeSpray, true), "");
            d.Resolve(out var dr1);
            Check("beat2-disabled-shows-frozen-look-over-used", dr1.FreezeBeat == 1 && d.BossFrozenNow && !d.PowerReady(BadgerPower.FreezeSpray)
                && BadgerFrozenUi.Disabled(d, BadgerPower.FreezeSpray, true), "");
            Check("used-hot-sauce-still-keeps-used-look", !BadgerFrozenUi.Disabled(d, BadgerPower.HotSauce, true), "");
            ui.Observe(true, true);
            string line = ui.Tap(d, BadgerPower.FreezeSpray, t0 + 5f);
            Check("beat2-tap-shows-freeze-line", line == BadgerCopy.FreezeSprayFrozen && ui.ToastLive(t0 + 6f) && ui.Toast(t0 + 6f) == BadgerCopy.FreezeSprayFrozen
                && !ui.ToastLive(t0 + 5f + BadgerFrozenUi.ToastSeconds + 0.01f), "");
            Check("hot-sauce-line-unchanged", BadgerFrozenUi.TapLine(d, BadgerPower.HotSauce) == BadgerCopy.HotSauceFrozen
                && new BadgerFrozenUi().Tap(d, BadgerPower.HotSauce, t0) == BadgerCopy.HotSauceFrozen, "");
            Check("x2-x3-not-disabled", !BadgerFrozenUi.Disabled(d, BadgerPower.X2, false) && !BadgerFrozenUi.Disabled(d, BadgerPower.X3, false), "");
            // Thaw: the pop queues, but a spent Freeze Spray returns to USED, so the pop is cancelled.
            Play(d, BadgerPower.None, out var dr2);
            bool disabledNow = BadgerFrozenUi.Disabled(d, BadgerPower.FreezeSpray, true);
            ui.Observe(disabledNow, true);
            bool queued = ui.PopPending;
            ui.CancelPop();
            Check("thaw-back-to-used-no-pop", dr2.FreezeBeat == 2 && !d.BossFrozenNow && !disabledNow && queued && !ui.PopPending && ui.PopScale(t0 + 9f, true) == 1f
                && !d.PowerReady(BadgerPower.FreezeSpray), "");
            // Fight end / restart: nothing stuck.
            var ui2 = new BadgerFrozenUi(BadgerPower.FreezeSpray);
            ui2.Observe(true, true); ui2.Tap(d, BadgerPower.FreezeSpray, t0); ui2.Observe(false, true); ui2.Reset();
            Check("reset-clears-everything", !ui2.PopPending && !ui2.WasFrozen && !ui2.ToastLive(t0) && ui2.PopScale(t0, true) == 1f, "");

            // ---- copy ----
            string cp = BadgerCopy.FreezeSprayFrozen;
            Check("copy-plain-two-lines-same-shape-as-hot-sauce", cp == "Frozen badger.\nFreeze Spray is on hold." && cp.Split('\n').Length == 2
                && cp.Replace("Freeze Spray", "Hot Sauce") == BadgerCopy.HotSauceFrozen, "");
            var sizes = new[] { new Vector2(320, 568), new Vector2(375, 667), new Vector2(390, 844), new Vector2(768, 1024), new Vector2(1024, 1366) };
            int clipped = 0; string firstBad = "";
            foreach (var sz in sizes)
                if (!FlockFiveApp.BadgerCaptionPaints(cp, sz.x, sz.y, new Rect(0, 0, sz.x, sz.y), out string dd))
                { clipped++; if (firstBad == "") firstBad = dd; }
            Check("copy-fits-se-to-ipad", clipped == 0, "clipped " + clipped + " " + firstBad);

            // ---- gate helper ----
            var comb = new Rect(200, 340, 60, 52); var hot = new Rect(130, 520, 80, 70); var fz = new Rect(250, 520, 80, 70); var x2 = new Rect(10, 520, 80, 70);
            Check("gate-freeze-handler-passes-x2-eaten", BadgerFrozenUi.PressPasses(fz.center, comb, hot, fz) && BadgerFrozenUi.PressPasses(hot.center, comb, hot, fz)
                && BadgerFrozenUi.PressPasses(comb.center, comb, hot, fz) && !BadgerFrozenUi.PressPasses(x2.center, comb, hot, fz), "");

            // ---- source wiring ----
            string badger = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Badger.cs");
            Check("wiring-freeze-ui", badger.Contains("_bgFrozenUiFreeze") && badger.Contains("new BadgerFrozenUi(BadgerPower.FreezeSpray)")
                && badger.Contains("p != BadgerPower.HotSauce && p != BadgerPower.FreezeSpray") && badger.Contains("if (frozenOut) DrawBadgerIceBadge(rr);")
                && badger.Contains("_bgFrozenUiFreeze.Reset();") && badger.Contains("_bgFrozenUiFreeze.Toast(AdPause.Unscaled)"), "");

            string end = fail == 0 ? "BUILD110FF_OK " + pass : "BUILD110FF_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            if (fail > 0) throw new System.Exception(end);
        }
    }
}
#endif
