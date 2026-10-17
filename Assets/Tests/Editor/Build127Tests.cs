#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // b127: the Freeze demo's break-free beat is a real gated step (CoachStep.BadgerBreakFree), and Hot Sauce reads as
    // disabled (not broken) while the badger is frozen: line on tap, pop on thaw, nothing stuck after the fight.
    public static class Build127Tests
    {
        const string Out = "/tmp/flock-five-b127-tests.txt";

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

        [MenuItem("Flock Five/Build 127 Tests")]
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
                Debug.Log("[build127] " + line);
            }

            // ---- shared table: explicit tap mode, pointer, caption ----
            var step = CoachStep.BadgerBreakFree;
            var rule = CoachTap.Rule(step);
            Check("table-kind-required-target-only", CoachTap.Kind(step) == CoachTapKind.Required && !CoachTap.AnyTap(step)
                && rule.Advances(TapZone.Target) && !rule.Advances(TapZone.Blank) && !rule.Advances(TapZone.Focus), "");
            Check("table-back-comb-blank-ignored", rule.Ignores(TapZone.Back) && rule.Ignores(TapZone.Comb) && rule.Ignores(TapZone.Blank), "");
            Check("table-hot-sauce-toggles-never-advances", rule.Toggles(TapZone.PowerButton) && !rule.Advances(TapZone.PowerButton), "");
            Check("table-pointer-glove", rule.Pointer == CoachPointer.Glove && CoachTap.Pointer(step) == CoachPointer.Glove, "");
            // Glove side rule: left half points left (enters from the right); right half points right (enters from the left).
            Check("pointer-side-rule", BadgerFrozenUi.PointsLeft(100f, 800f) && !BadgerFrozenUi.PointsLeft(700f, 800f), "");
            Check("caption-from-shared-table", CoachTap.Caption(step) == BadgerCopy.FreezeBreakFreeWait, "");
            var cap = BadgerCopy.FreezeBreakFreeWait;
            Check("caption-two-lines-max", cap.Split('\n').Length == 2 && BadgerCopy.HotSauceFrozen.Split('\n').Length == 2, cap.Replace("\n", "/"));
            var sizes = new[] { new Vector2(320, 568), new Vector2(375, 667), new Vector2(390, 844), new Vector2(768, 1024), new Vector2(1024, 1366) };
            int clipped = 0; string firstBad = "";
            foreach (var sz in sizes)
                foreach (var ln in new[] { cap, BadgerCopy.HotSauceFrozen })
                    if (!FlockFiveApp.BadgerCaptionPaints(ln, sz.x, sz.y, new Rect(0, 0, sz.x, sz.y), out string d))
                    { clipped++; if (firstBad == "") firstBad = ln.Replace("\n", "/") + " @ " + d; }
            Check("caption-fits-se-to-ipad", clipped == 0, "clipped " + clipped + " " + firstBad);
            Check("copy-plain-friendly", BadgerCopy.HotSauceFrozen == "Frozen badger.\nHot Sauce is on hold." && BadgerCopy.HotSauceFrozen.Replace("\n", " ") == "Frozen badger. Hot Sauce is on hold." && BadgerCopy.HotSauceFrozenSub == "FROZEN", "");

            // ---- gate: only the glowing comb (and the frozen Hot Sauce handler) takes a press ----
            var comb = new Rect(200, 340, 60, 52); var hot = new Rect(130, 520, 80, 70);
            var back = new Rect(10, 10, 60, 60); var x2 = new Rect(10, 520, 80, 70); var freeze = new Rect(250, 520, 80, 70);
            bool gate = BadgerFrozenUi.PressPasses(comb.center, comb, hot) && BadgerFrozenUi.PressPasses(hot.center, comb, hot)
                && !BadgerFrozenUi.PressPasses(back.center, comb, hot) && !BadgerFrozenUi.PressPasses(x2.center, comb, hot)
                && !BadgerFrozenUi.PressPasses(freeze.center, comb, hot) && !BadgerFrozenUi.PressPasses(new Vector2(400, 300), comb, hot)
                && !BadgerFrozenUi.PressPasses(new Vector2(300, 360), comb, hot);
            Check("gate-only-focus-comb-passes-others-ignored", gate, "");

            // ---- drive the demo: Freeze round (beat 1), then the break-free round (beat 2) ----
            int coins = Purse.Coins;
            if (coins < 5000) Purse.Credit(5000 - coins);
            var f = new BadgerFight(1, 22, Fill(16, 2), Fill(16, 5), false);
            var ui = new BadgerFrozenUi();
            const float t0 = 100f;
            Check("before-freeze-hot-sauce-live", !BadgerFrozenUi.Disabled(f, BadgerPower.HotSauce, false) && ui.Tap(f, BadgerPower.HotSauce, t0) == null && !ui.ToastLive(t0), "");

            // Beat 1: Freeze armed on the frozen turn.
            f.BossPick();
            int ix = -1; for (int i = 0; i < f.PlayerCount; i++) if (f.PlayerOpen(i)) { ix = i; break; }
            bool armed = f.PlayerPick(ix) && f.TryPower(BadgerPower.FreezeSpray);
            string l1 = ui.Tap(f, BadgerPower.HotSauce, t0);
            Check("beat1-frozen-turn-tap-shows-line", armed && l1 == BadgerCopy.HotSauceFrozen && ui.ToastLive(t0 + 1f) && ui.Toast(t0 + 1f) == l1, "");
            Check("beat1-other-powers-not-disabled", !BadgerFrozenUi.Disabled(f, BadgerPower.X2, false) && !BadgerFrozenUi.Disabled(f, BadgerPower.X3, false), "");
            bool r1ok = f.Resolve(out var r1) && r1.FreezeBeat == 1;
            ui.Observe(BadgerFrozenUi.Disabled(f, BadgerPower.HotSauce, false), true);

            // Beat 2: the break-free round. Hot Sauce is still disabled and says why; the step is live.
            Check("beat2-still-frozen", r1ok && f.BossFrozenNow && BadgerFrozenUi.Disabled(f, BadgerPower.HotSauce, false), "");
            string l2 = ui.Tap(f, BadgerPower.HotSauce, t0 + 5f);
            Check("beat2-break-free-tap-shows-line", l2 == BadgerCopy.HotSauceFrozen && ui.ToastLive(t0 + 6f) && !ui.ToastLive(t0 + 5f + BadgerFrozenUi.ToastSeconds + 0.01f), "");
            Check("beat2-no-pop-while-frozen", ui.PopScale(t0 + 5f, true) == 1f && !ui.PopPending, "");

            // Thaw: the break-free round resolves, Hot Sauce re-enables with a pop.
            bool r2ok = Play(f, BadgerPower.None, out var r2) && r2.FreezeBeat == 2;
            bool disabledNow = BadgerFrozenUi.Disabled(f, BadgerPower.HotSauce, false);
            ui.Observe(disabledNow, true);
            Check("thaw-hot-sauce-re-enabled", r2ok && !f.BossFrozenNow && !disabledNow && ui.Tap(f, BadgerPower.HotSauce, t0 + 9f) == null, "");
            Check("thaw-pop-waits-for-active-buttons", ui.PopPending && ui.PopScale(t0 + 9f, false) == 1f && ui.PopPending, "");
            float mid = ui.PopScale(t0 + 10f, true);
            float peak = ui.PopScale(t0 + 10f + BadgerFrozenUi.PopSeconds * 0.5f, true);
            float done = ui.PopScale(t0 + 10f + BadgerFrozenUi.PopSeconds + 0.01f, true);
            Check("thaw-visible-pop-then-rest", !ui.PopPending && peak > 1.1f && peak <= 1.2f && mid >= 1f && done == 1f && !ui.Popping(t0 + 12f), "peak " + peak);

            // ---- repeat fights: the same holds for a normal fight, and nothing is stuck ----
            var g = new BadgerFight(3, 31, Fill(16, 2), Fill(16, 5), false);
            var ui2 = new BadgerFrozenUi();
            bool g1 = Play(g, BadgerPower.FreezeSpray, out var gr1) && gr1.FreezeBeat == 1;
            bool tapG = ui2.Tap(g, BadgerPower.HotSauce, t0) == BadgerCopy.HotSauceFrozen;
            ui2.Observe(true, true);
            bool g2 = Play(g, BadgerPower.None, out var gr2) && gr2.FreezeBeat == 2;
            ui2.Observe(BadgerFrozenUi.Disabled(g, BadgerPower.HotSauce, false), true);
            Check("repeat-fight-same-line-and-thaw", g1 && tapG && g2 && ui2.PopPending && !BadgerFrozenUi.Disabled(g, BadgerPower.HotSauce, false), "");

            // Fight end / restart / leaving: state resets; a fresh fight starts enabled.
            var ui3 = new BadgerFrozenUi();
            ui3.Observe(true, true); ui3.Tap(f, BadgerPower.HotSauce, t0); ui3.Observe(false, true);
            ui3.Observe(true, false); // fight over
            Check("fight-end-resets-ui-state", !ui3.PopPending && !ui3.WasFrozen && !ui3.ToastLive(t0) && ui3.PopScale(t0, true) == 1f, "");
            var ui4 = new BadgerFrozenUi();
            ui4.Observe(true, true); ui4.Tap(f, BadgerPower.HotSauce, t0); ui4.Observe(false, true); ui4.Reset();
            Check("restart-or-background-reset-clears-everything", !ui4.PopPending && !ui4.WasFrozen && !ui4.ToastLive(t0) && ui4.PopScale(t0, true) == 1f, "");
            var fresh = new BadgerFight(1, 5, Fill(16, 2), Fill(16, 5), true);
            Check("fresh-fight-not-disabled", !BadgerFrozenUi.Disabled(fresh, BadgerPower.HotSauce, false) && !BadgerFrozenUi.Disabled(null, BadgerPower.HotSauce, false), "");
            Check("used-button-keeps-used-look", !BadgerFrozenUi.Disabled(f, BadgerPower.HotSauce, true), "");

            // ---- source wiring ----
            string badger = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Badger.cs");
            string coach = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Coach.cs");
            Check("wiring-break-free-in-shared-gate-and-back-hold",
                coach.Contains("_bgGateToggle = BadgerGateToggleRect(stage);") && badger.Contains("bool BadgerBreakFreeLive()")
                && badger.Contains("|| BadgerFreezeDemoHolds() || BadgerBreakFreeLive())")
                && badger.Contains("|| BadgerBreakFreeLive());") && badger.Contains("guide = _bgBreakTile;"), "");
            Check("wiring-hot-sauce-ui", badger.Contains("DrawBadgerIceBadge(rr)") && badger.Contains("_bgFrozenUi.Tap(_bgFight, p, AdPause.Unscaled)")
                && badger.Contains("Sfx.Deny();") && badger.Contains("_bgFrozenUi.Observe(frozenOut") && badger.Contains("_bgFrozenUi.Reset();"), "");

            string end = fail == 0 ? "BUILD127_OK " + pass : "BUILD127_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            if (fail > 0) throw new System.Exception(end);
        }
    }
}
#endif
