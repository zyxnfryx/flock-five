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

            BadgerCopy.MarkCoach();
            for (int app = 2; app <= 6; app++)
            {
                Check("a" + app + "-no-lesson", !BadgerCopy.NeedsLesson(app), "");
                Check("a" + app + "-opening-line", BadgerCopy.OpeningLine(app, false) == BadgerCopy.OpenLater, "");
                Check("a" + app + "-round-lines", BadgerCopy.RoundStartLine(0) == BadgerCopy.GoesFirst && BadgerCopy.RoundStartLine(1) == BadgerCopy.HowMany, "");
            }
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

            // Wiring: the shared helper is used, not gated on tutorial flags, and restores the coach fade.
            string badger = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Badger.cs");
            Check("wiring-shared-helper-fade", badger.Contains("BadgerCaptionFade.For(_bgLessonLive || BadgerGuideLive() || BadgerResultLive(), _coachFade)")
                && badger.Contains("finally { _coachFade = keepFade; }") && badger.Contains("CapSpot(\"badger.caption\"); PaintCoachCaption(_bgLine"), "");
            Check("wiring-caption-after-power-row", badger.IndexOf("DrawBadgerPowers(L, _bgStage != BadgerStage.Lesson);") < badger.IndexOf("if (_bgStage != BadgerStage.DontCare) DrawBadgerCaption(L);"), "");

            string end = fail == 0 ? "BUILD110_OK " + pass : "BUILD110_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            if (fail > 0) throw new System.Exception(end);
        }
    }
}
#endif
