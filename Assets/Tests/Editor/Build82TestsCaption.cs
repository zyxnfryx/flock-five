#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 82 pass b82f. Blank line after the first sentence, in the shared fitter.
    static class Build82TestsCaption
    {
        const float W = 1179f;
        const float H = 2556f;

        [MenuItem("Flock Five/Build 82 Caption Tests")]
        public static void Run()
        {
            int pass = 0;
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++;
                else fail++;
                Debug.Log("[build82f-caption] " + (ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            string sparrow = CaptionLayout.SparrowLine;
            string gapped = CaptionLayout.HeadlineGap(sparrow);
            bool sparrowGap = gapped.Contains("!\n\n") && gapped.Contains("there")
                && sparrow.Contains("there");
            Check("sparrow-gap", sparrowGap, gapped.Replace("\n", " / "));

            bool single = CaptionLayout.HeadlineGap("Tap to claim!") == "Tap to claim!"
                && CaptionLayout.HeadlineGap("Tap Daily for\nyour bonus!") == "Tap Daily for\nyour bonus!"
                && CaptionLayout.HeadlineGap("Double your coins.") == "Double your coins."
                && CaptionLayout.HeadlineGap("A!\n\nRest") == "A!\n\nRest";
            string swipe = CaptionLayout.HeadlineGap("Swipe to turn the page! Or tap a page number.");
            bool swipeGap = swipe.Contains("!\n\n") && swipe.Contains("Or tap a page number.");
            Check("single-and-swipe", single && swipeGap, swipe.Replace("\n", " / "));

            var lines = FlockFiveApp.PaintedLessonLines();
            var seen = new HashSet<string>();
            int bad = 0;
            string detail = "in bounds";
            float safePlate = CaptionLayout.SafePlateWidth(W, H);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i] ?? "";
                if (line.Length == 0 || !seen.Add(line)) continue;
                var gold = FlockFiveApp.GoldCaption(line, W, H);
                var fit = FlockFiveApp.LessonCaptionFit(line, W, H);
                string want = CaptionLayout.HeadlineGap(line);
                bool multi = want != line;
                bool gap = !multi || ((gold.Shown ?? "").Contains("\n\n")
                    && FlockFiveApp.LessonCaptionShown(line).Contains("\n\n"));
                bool bounds = !gold.Overflow
                    && gold.PlateW <= safePlate + 0.5f
                    && fit.PlateW <= safePlate + 0.5f
                    && fit.TextW > 8f && fit.TextH > 8f;
                if (gap && bounds) continue;
                bad++;
                detail = (gold.Shown ?? "").Replace("\n", " / ")
                    + " overflow=" + gold.Overflow
                    + " plate=" + gold.PlateW.ToString("0") + "/" + safePlate.ToString("0")
                    + " fit=" + fit.PlateW.ToString("0");
                if (bad >= 3) break;
            }
            int beeLines = FlockFiveApp.LessonCaptionLines(CaptionLayout.HiveGardenLine, W, H);
            var beeGold = FlockFiveApp.GoldCaption(CaptionLayout.HiveGardenLine, W, H);
            bool bee = beeLines >= 2 && beeLines <= 4 && beeGold.Lines >= 2 && beeGold.Lines <= 4 && !beeGold.Overflow;
            var sparGold = FlockFiveApp.GoldCaption(CaptionLayout.SparrowLine, W, H);
            bool spar = sparGold.Lines >= 2 && sparGold.Lines <= 3 && !sparGold.Overflow
                && (sparGold.Shown ?? "").Contains("\n\n");
            Check("lesson-bounds", bad == 0 && bee && spar,
                bad == 0 ? "beeLines=" + beeLines + " beeGold=" + beeGold.Lines + " spar=" + sparGold.Lines : detail);

            Debug.Log("[build82f-caption] " + (fail == 0 ? "BUILD82F_CAPTION_OK " + pass : "BUILD82F_CAPTION_FAIL " + fail));
            if (fail > 0) throw new System.Exception("BUILD82F_CAPTION_FAIL " + fail);
        }
    }
}
#endif
