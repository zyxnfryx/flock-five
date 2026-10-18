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

            // Real PlayerPrefs are saved and restored so the run leaves the editor's coach stamp alone.
            int keepCoach = PlayerPrefs.GetInt(BadgerCopy.CoachPref, 0);
            try {
            BadgerCopy.MarkCoach();
            for (int app = 2; app <= 6; app++)
            {
                Check("a" + app + "-no-lesson", !BadgerCopy.NeedsLesson(app), "");
                Check("a" + app + "-opening-line", BadgerCopy.OpeningLine(app, false) == BadgerCopy.OpenLater, "");
                Check("a" + app + "-round-lines", BadgerCopy.RoundStartLine(0) == BadgerCopy.GoesFirst && BadgerCopy.RoundStartLine(1) == BadgerCopy.HowMany, "");
            }
            } finally { PlayerPrefs.SetInt(BadgerCopy.CoachPref, keepCoach); PlayerPrefs.Save(); }
            Check("pick-line-exists", BadgerCopy.YourPick.Length > 0, "");

            // Every existing line a repeat fight can show paints inside the bottom band on small to large screens.
            var lines = new[] { BadgerCopy.OpenLater, BadgerCopy.GoesFirst, BadgerCopy.HowMany, BadgerCopy.YourPick, BadgerCopy.Tie,
                BadgerCopy.DontCare, BadgerCopy.MineNow, BadgerCopy.CoinsShort, BadgerCopy.Win, BadgerCopy.Lose,
                BadgerCopy.DontCareWasted, BadgerCopy.DontCareHalved, BadgerCopy.BreakFree, BadgerCopy.FrozenSolid, BadgerCopy.BlockedBoth };
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

            // Caption band never overlaps the power row, at iPhone sizes (same helper BadgerLayout uses).
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

            // Wiring (loose: names, not exact source lines): the shared helper is used, not gated on tutorial flags, and restores the coach fade.
            string badger = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Badger.cs");
            Check("wiring-shared-helper-fade", badger.Contains("BadgerCaptionFade.For(") && badger.Contains("BadgerTutorialFight()") && badger.Contains("PaintCoachCaption(_bgLine"), "");
            Check("wiring-caption-after-power-row", badger.IndexOf("DrawBadgerPowers(L,") < badger.IndexOf("DrawBadgerCaption(L);"), "");

            string end = fail == 0 ? "BUILD110_OK " + pass : "BUILD110_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            if (fail > 0) throw new System.Exception(end);
        }
    }
}
#endif
