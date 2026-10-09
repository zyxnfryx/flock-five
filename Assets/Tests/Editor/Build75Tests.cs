#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 75. Informational gloves advance on any tap. Required gloves stay on
    // their target. Daily claim stays Claim-only. The badger column line breaks
    // on purpose and each piece fits one line at both phone sizes.
    static class Build75Tests
    {
        [MenuItem("Flock Five/Build 75 Tests")]
        public static void Run()
        {
            int pass = 0;
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++;
                else fail++;
                Debug.Log("[build75] " + (ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            var all = CoachTap.All;
            Check("step-count", all != null && all.Length == 38, "n=" + (all == null ? 0 : all.Length));
            if (all != null)
            {
                for (int i = 0; i < all.Length; i++)
                {
                    var step = all[i];
                    var want = Expect(step);
                    var got = CoachTap.Kind(step);
                    bool info = got == CoachTapKind.Informational;
                    bool gate = CoachTap.AnyTap(step) == info
                        && CoachTap.Consumes(step) == info
                        && CoachTap.Advances(step, false) == info
                        && CoachTap.Advances(step, true);
                    string cap = CoachTap.Caption(step) ?? "";
                    Debug.Log("[build75] CLASS " + step + " " + got + " | " + cap.Replace("\n", " / "));
                    Check(step.ToString(), got == want && gate, got + " want " + want + " | " + cap.Replace("\n", " / "));
                }
            }

            Check("lesson-count", BadgerCopy.LessonCount == 3, "n=" + BadgerCopy.LessonCount);
            for (int i = 0; i < 3; i++)
            {
                var step = CoachTap.BadgerLesson(i);
                bool ok = CoachTap.Kind(step) == CoachTapKind.Informational
                    && CoachTap.AnyTap(step)
                    && CoachTap.Consumes(step)
                    && CoachTap.Advances(step, false)
                    && CoachTap.Advances(step, true);
                Check("badger-lesson-" + i, ok, step.ToString());
            }
            Check("guide-count", BadgerCopy.Guide != null && BadgerCopy.Guide.Length == 3, "n=" + (BadgerCopy.Guide == null ? 0 : BadgerCopy.Guide.Length));
            for (int m = 1; m <= 3; m++)
            {
                var step = CoachTap.BadgerGuide(m);
                bool ok = CoachTap.Kind(step) == CoachTapKind.Required
                    && !CoachTap.AnyTap(step)
                    && !CoachTap.Consumes(step)
                    && !CoachTap.Advances(step, false)
                    && CoachTap.Advances(step, true);
                Check("badger-guide-" + m, ok, step.ToString());
            }

            var claim = CoachStep.DailyClaim;
            Check("daily-claim",
                CoachTap.Kind(claim) == CoachTapKind.ClaimOnly
                && !CoachTap.AnyTap(claim)
                && !CoachTap.Consumes(claim)
                && !CoachTap.Advances(claim, false)
                && CoachTap.Advances(claim, true),
                CoachTap.Kind(claim).ToString());
            var rail = CoachStep.DailyRail;
            Check("daily-rail",
                CoachTap.Kind(rail) == CoachTapKind.Required
                && CoachTap.Kind(rail) != CoachTapKind.ClaimOnly
                && !CoachTap.AnyTap(rail)
                && !CoachTap.Advances(rail, false)
                && CoachTap.Advances(rail, true),
                CoachTap.Kind(rail).ToString());

            string column = BadgerCopy.LessonColumnTop + "\n" + BadgerCopy.LessonColumnBot;
            Check("column-copy", BadgerCopy.LessonAt(2) == column, BadgerCopy.LessonAt(2));
            Check("verdict-unchanged", BadgerCopy.DontCare == "Honey badger don't care." && BadgerCopy.DontCare.IndexOf('\n') < 0, BadgerCopy.DontCare);

            CheckCaption(Check, "1170 full", 1170f, 2532f, new Rect(0f, 0f, 1170f, 2532f));
            CheckCaption(Check, "1170 notch", 1170f, 2532f, Safe(1170f, 2532f, 141f, 102f));
            CheckCaption(Check, "750 full", 750f, 1334f, new Rect(0f, 0f, 750f, 1334f));
            CheckCaption(Check, "750 status", 750f, 1334f, Safe(750f, 1334f, 40f, 0f));

            Debug.Log("[build75] " + (fail == 0 ? "ALL OK " + pass : "FAILED " + fail + " passed " + pass));
        }

        static Rect Safe(float w, float h, float top, float bot)
        {
            return new Rect(0f, bot, w, Mathf.Max(2f, h - top - bot));
        }

        static void CheckCaption(System.Action<string, bool, string> check, string name, float w, float h, Rect safe)
        {
            var preview = FlockFiveApp.PreviewBadgerColumn(w, h, safe);
            string shown = preview.Shown ?? "";
            var parts = shown.Split('\n');
            bool two = parts.Length == 2
                && parts[0] == BadgerCopy.LessonColumnTop
                && parts[1] == BadgerCopy.LessonColumnBot;
            var st = FlockFiveApp.CoachCaptionStyle();
            st.wordWrap = false;
            st.fontSize = preview.Font;
            float a = st.CalcSize(new GUIContent(parts.Length > 0 ? parts[0] : "")).x;
            float b = st.CalcSize(new GUIContent(parts.Length > 1 ? parts[1] : "")).x;
            bool fit = a <= preview.TextW + 0.5f && b <= preview.TextW + 0.5f;
            st.wordWrap = false;
            float th = st.CalcHeight(new GUIContent(shown), Mathf.Max(8f, preview.TextW));
            bool tall = th <= preview.TextH + 1f;
            check(name,
                two && fit && tall && preview.Font > 0,
                "shown=[" + shown.Replace("\n", " / ") + "] font=" + preview.Font
                + " text=" + preview.TextW.ToString("0.0") + "x" + preview.TextH.ToString("0.0")
                + " a=" + a.ToString("0.0") + " b=" + b.ToString("0.0") + " h=" + th.ToString("0.0"));
        }

        static CoachTapKind Expect(CoachStep step)
        {
            switch (step)
            {
                case CoachStep.BadgerPick:
                case CoachStep.BadgerPowers:
                case CoachStep.BadgerColumn:
                case CoachStep.Sparrow:
                case CoachStep.Hawk:
                case CoachStep.Bee:
                case CoachStep.Leaf:
                case CoachStep.AlbumHoney:
                case CoachStep.AlbumUpgrade:
                case CoachStep.AdoptGreet:
                case CoachStep.VipWelcome:
                    return CoachTapKind.Informational;
                case CoachStep.DailyClaim:
                    return CoachTapKind.ClaimOnly;
                case CoachStep.BadgerComb1:
                case CoachStep.BadgerComb2:
                case CoachStep.BadgerComb3:
                case CoachStep.CoachPickup:
                case CoachStep.CoachPlace:
                case CoachStep.CoachSecondHop:
                case CoachStep.CoachNowhere:
                case CoachStep.GiftStuck:
                case CoachStep.HiveGarden:
                case CoachStep.HiveHome:
                case CoachStep.PokerIntro:
                case CoachStep.PokerBet:
                case CoachStep.PokerHold:
                case CoachStep.PokerDeal:
                case CoachStep.PokerBack:
                case CoachStep.PokerKeep:
                case CoachStep.DailyRail:
                case CoachStep.AlbumTap:
                case CoachStep.AlbumEmpty:
                case CoachStep.AlbumFlip:
                case CoachStep.AlbumPage:
                case CoachStep.AdHand:
                case CoachStep.AdoptLook:
                case CoachStep.VipPitch:
                case CoachStep.RestartSure:
                case CoachStep.RestartKeep:
                    return CoachTapKind.Required;
            }
            throw new System.ArgumentOutOfRangeException(nameof(step), step, "Classify this lesson step");
        }
    }
}
#endif
