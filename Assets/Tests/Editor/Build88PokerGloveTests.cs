#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // Poker lesson: the glove comes from the left and points right at the rail
    // button. The caption is noted before that glove, and lifted off the hand.
    public static class Build88PokerGloveTests
    {
        const string Out = "/tmp/flock-five-b88-poker-glove-tests.txt";

        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            int pass = 0, fail = 0;
            void Check(string n, bool ok, string d)
            {
                string line = (ok ? "PASS  " : "FAIL  ") + n + "  " + d;
                if (ok) pass++; else fail++;
                sb.AppendLine(line);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build88poker] " + line);
            }
            try
            {
                Check("glove-from-left", FlockFiveApp.PokerGloveFromLeft, "fromLeft");

                var aim = new Vector2(860f, 640f);
                var hand = FlockFiveApp.PokerPointHand(aim, 2f);
                Check("hand-points-from-the-left",
                    hand.width > 8f && hand.xMax <= aim.x + 1.5f && hand.xMin < aim.x - 8f
                    && hand.yMin < aim.y && hand.yMax > aim.y,
                    hand.ToString());

                var text = new Rect(180f, 600f, 420f, 90f);
                var button = new Rect(800f, 580f, 140f, 140f);
                var lifted = FlockFiveApp.LiftPokerCaption(text, 24f, 18f, button, hand, 48f, 12f);
                var plate = new Rect(lifted.x - 24f, lifted.y - 18f, lifted.width + 48f, lifted.height + 36f);
                float top = Mathf.Min(button.yMin, hand.yMin);
                Check("plate-lifts-off-the-row",
                    lifted.y < text.y && plate.yMax <= top - 12f + 0.6f && !plate.Overlaps(button) && !plate.Overlaps(hand),
                    "y " + text.y.ToString("0") + " -> " + lifted.y.ToString("0") + " plate " + plate);

                var clear = new Rect(180f, 80f, 280f, 60f);
                var stayed = FlockFiveApp.LiftPokerCaption(clear, 12f, 10f, button, hand, 8f, 12f);
                Check("clear-plate-stays", Mathf.Abs(stayed.y - clear.y) < 0.01f, stayed.ToString());

                // Floor pins the plate on the hand. It slides left of the hand and the button.
                var pinned = new Rect(700f, 600f, 280f, 70f);
                var slid = FlockFiveApp.LiftPokerCaption(pinned, 16f, 12f, button, hand, 590f, 12f);
                var slidPlate = new Rect(slid.x - 16f, slid.y - 12f, slid.width + 32f, slid.height + 24f);
                Check("floor-slides-the-plate-clear",
                    slid.x < pinned.x && !slidPlate.Overlaps(button) && !slidPlate.Overlaps(hand),
                    slidPlate.ToString());

                // A screen-wide plate cannot slide off the hand. It narrows instead.
                var wide = new Rect(40f, 560f, 900f, 120f);
                var wideHand = new Rect(700f, 500f, 220f, 180f);
                var wideBtn = new Rect(820f, 540f, 140f, 140f);
                var cut = FlockFiveApp.LiftPokerCaption(wide, 20f, 16f, wideBtn, wideHand, 540f, 12f);
                var cutPlate = new Rect(cut.x - 20f, cut.y - 16f, cut.width + 40f, cut.height + 32f);
                Check("wide-plate-clears-the-hand",
                    cut.width < wide.width && !cutPlate.Overlaps(wideHand) && !cutPlate.Overlaps(wideBtn),
                    cutPlate.ToString());

                // A phone-wide plate pinned on the hand stays right of the left rail.
                var railText = new Rect(220f, 600f, 640f, 140f);
                var railed = FlockFiveApp.LiftPokerCaption(railText, 36f, 24f, button, hand, 580f, 12f, 160f);
                var railPlate = new Rect(railed.x - 36f, railed.y - 24f, railed.width + 72f, railed.height + 48f);
                Check("plate-stays-right-of-the-rail",
                    railPlate.x >= 160f - 0.6f && !railPlate.Overlaps(button) && !railPlate.Overlaps(hand),
                    railPlate.ToString());

                string coach = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/FlockFiveApp.Coach.cs"));
                int spot = coach.IndexOf("if (_pokerIntroLive)");
                int end = coach.IndexOf("if (_dailyIntroLive)", spot);
                string body = spot >= 0 && end > spot ? coach.Substring(spot, end - spot) : "";
                int boxAt = coach.IndexOf("Rect PokerIntroBox");
                int growAt = coach.IndexOf("PokerCaptionGrow(", boxAt);
                int liftAt = coach.IndexOf("LiftPokerCaption(", boxAt);
                Check("poker-box-counts-paint-growth",
                    boxAt > 0 && growAt > boxAt && liftAt > growAt,
                    "box " + boxAt + " grow " + growAt + " lift " + liftAt);
                int pigAt = coach.IndexOf("pig.yMax", boxAt);
                Check("poker-box-seats-under-the-pig",
                    pigAt > boxAt && pigAt < liftAt,
                    "pig " + pigAt + " lift " + liftAt);
                int handEdge = coach.IndexOf("hand.xMin", boxAt);
                Check("poker-box-stops-at-the-glove",
                    handEdge > boxAt && handEdge < liftAt,
                    "hand " + handEdge + " lift " + liftAt);

                Check("poker-spot-notes-then-points",
                    body.IndexOf("NoteTutorPlate(") > 0
                    && body.IndexOf("PokerGloveFromLeft") > body.IndexOf("NoteTutorPlate(")
                    && body.IndexOf("GloveBelowDeg") < 0
                    && coach.IndexOf("LiftPokerCaption(") > 0,
                    "spot " + spot);
            }
            catch (System.Exception ex)
            {
                Check("poker-glove-threw", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                sb.AppendLine("TOTAL pass " + pass + " fail " + fail);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build88poker] TOTAL pass " + pass + " fail " + fail);
            }
        }
    }
}
#endif
