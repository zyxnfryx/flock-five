#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // b110: the bottom badger dialog shows on repeat fights (appearance 2+), outside the tutorial.
    public static class Build110BadgerDialogTests
    {
        const string Out = "/tmp/flock-five-b110-tests.txt";

        static int[] Fill(int n, int v) { var a = new int[n]; for (int i = 0; i < n; i++) a[i] = v; return a; }

        [MenuItem("Flock Five/Build 110 Badger Dialog Tests")]
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
                Debug.Log("[build110] " + line);
            }

            // The coach stamp is saved and restored through PrefGuard (shared checksum), never raw PlayerPrefs.
            int keepCoach = BadgerCopy.CoachStampForTest();
            try
            {
                BadgerCopy.ClearCoachForTest();
                Check("mark-coach-sets-stamp", !BadgerCopy.CoachDone() && BadgerCopy.NeedsLesson(1), "");
                BadgerCopy.MarkCoach();
                Check("mark-coach-done", BadgerCopy.CoachDone() && !BadgerCopy.NeedsLesson(1), "");
                BadgerCopy.MarkCoach(); // already set: no-op, still done, stamp still valid
                Check("mark-coach-noop-when-set", BadgerCopy.CoachDone() && BadgerCopy.CoachStampForTest() == 1, "");
                for (int app = 2; app <= 6; app++)
                {
                    Check("a" + app + "-no-lesson", !BadgerCopy.NeedsLesson(app), "");
                    Check("a" + app + "-opening-line", BadgerCopy.OpeningLine(app, false) == BadgerCopy.OpenLater, "");
                    Check("a" + app + "-round-lines", BadgerCopy.RoundStartLine(0) == BadgerCopy.GoesFirst && BadgerCopy.RoundStartLine(1) == BadgerCopy.HowMany, "");
                }
            }
            finally { BadgerCopy.SetCoachStampForTest(keepCoach); }
            Check("pick-line-exists", BadgerCopy.YourPick.Length > 0, "");

            // Every existing line a repeat fight can show paints inside the bottom band on small to large screens.
            var lines = new[] { BadgerCopy.OpenLater, BadgerCopy.GoesFirst, BadgerCopy.HowMany, BadgerCopy.YourPick, BadgerCopy.Tie,
                BadgerCopy.DontCare, BadgerCopy.MineNow, BadgerCopy.CoinsShort, BadgerCopy.Win, BadgerCopy.Lose,
                BadgerCopy.DontCareWasted, BadgerCopy.DontCareHalved, BadgerCopy.BreakFree, BadgerCopy.FrozenSolid, BadgerCopy.BlockedBoth, BadgerCopy.CancelTieLine(5, 5) };
            lines = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Concat(lines, BadgerCopy.WinLines));
            var sizes = new[] { new Vector2(375, 667), new Vector2(390, 844), new Vector2(430, 932) };
            int clipped = 0; string firstBad = "";
            foreach (var sz in sizes)
                foreach (var ln in lines)
                    if (!FlockFiveApp.BadgerCaptionPaints(ln, sz.x, sz.y, new Rect(0, 0, sz.x, sz.y), out string d))
                    { clipped++; if (firstBad == "") firstBad = ln.Replace("\n", "/") + " @ " + d; }
            Check("repeat-lines-paint-iphone", clipped == 0, firstBad);

            // The fade: non-tutorial caption is fully visible even though the glove fade is 0.
            Check("fade-repeat-fight-visible", BadgerCaptionFade.For(false, 0f) == 1f, "");
            Check("fade-tutorial-keeps-glove-fade", BadgerCaptionFade.For(true, 0.4f) == 0.4f && BadgerCaptionFade.For(true, 0f) == 0f, "");

            Check("fade-clamped", BadgerCaptionFade.For(true, -0.5f) == 0f && BadgerCaptionFade.For(true, 1.7f) == 1f, "");

            // Caption band never overlaps the power row, at iPhone sizes. BadgerLayout itself is an instance method that reads
            // DeviceFit/TopHud/Screen (Unity runtime state), so the test drives the static helper it delegates to.
            int overlaps = 0; string bad = "";
            foreach (var sz in sizes)
            {
                float sc = Mathf.Max(1f, sz.x / 375f);
                float yEnd = sz.y - 8f * sc;
                float y0 = 90f * sc;
                float room = Mathf.Max(100f, yEnd - y0 - 6f * sc * 4f);
                float yPower = yEnd - 6f * sc * 4f - room * 0.29f - 6f * sc; // any start works; the bands stack from it
                FlockFiveApp.BadgerBottomBands(yPower, 8f * sc, sz.x - 8f * sc, room, sc, out var pw, out var cp);
                if (pw.Overlaps(cp) || cp.yMin < pw.yMax) { overlaps++; bad = sz + " " + pw + " " + cp; }
            }
            Check("caption-band-clear-of-power-row", overlaps == 0, bad);

            // Fight type decides the fade: only a non-tutorial fight gets full fade.
            Check("tutorial-signals", BadgerCaptionFade.IsTutorial(true, false, false, false, false, false)
                && BadgerCaptionFade.IsTutorial(false, true, false, false, false, false)
                && BadgerCaptionFade.IsTutorial(false, false, true, false, false, false)
                && BadgerCaptionFade.IsTutorial(false, false, false, true, false, false)
                && BadgerCaptionFade.IsTutorial(false, false, false, false, true, false)
                && BadgerCaptionFade.IsTutorial(false, false, false, false, false, true), "");
            Check("repeat-fight-not-tutorial", !BadgerCaptionFade.IsTutorial(false, false, false, false, false, false), "");
            var repeat = new BadgerFight(2, 5, Fill(16, 2), Fill(16, 5), false);
            var coach = new BadgerFight(1, 5, Fill(16, 2), Fill(16, 5), true);
            Check("fight-flag", !repeat.CoachFight && coach.CoachFight, "");

            string end = fail == 0 ? "BUILD110_OK " + pass : "BUILD110_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            if (fail > 0) throw new System.Exception(end);
        }
    }
}
#endif
