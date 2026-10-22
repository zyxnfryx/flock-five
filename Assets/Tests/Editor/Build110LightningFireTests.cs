#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b110 (Brandon): lightning fire is on by default, rolls more often, the first strike is guaranteed,
    // and the first fire gets a one-time lesson on the shared tutorial path.
    public static class Build110LightningFireTests
    {
        const string Out = "/tmp/flock-five-b110-lightning-fire-tests.txt";

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
                Debug.Log("[build110fire] " + line);
            }

            bool hadBolt = PlayerPrefs.HasKey(LightningStrike.PrefKey);
            int bolt = PlayerPrefs.GetInt(LightningStrike.PrefKey, 0);
            bool hadStruck = PlayerPrefs.HasKey(LightningStrike.StruckKey);
            int struck = PlayerPrefs.GetInt(LightningStrike.StruckKey, 0);
            float? keepWindow = DeviceFit.TestWindow;
            try
            {
                Odds(Check);
                Guarantee(Check);
                Lesson(Check);
                Caption(Check);
                Wiring(Check);
            }
            catch (System.Exception ex)
            {
                Check("exception", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                DeviceFit.TestWindow = keepWindow;
                if (hadBolt) PlayerPrefs.SetInt(LightningStrike.PrefKey, bolt);
                else PlayerPrefs.DeleteKey(LightningStrike.PrefKey);
                if (hadStruck) PlayerPrefs.SetInt(LightningStrike.StruckKey, struck);
                else PlayerPrefs.DeleteKey(LightningStrike.StruckKey);
                PlayerPrefs.Save();
            }

            string end = fail == 0 ? "BUILD110FIRE_OK " + pass : "BUILD110FIRE_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[build110fire] " + end);
            if (fail > 0) throw new System.Exception(end);
        }

        static Board EmptyGift()
        {
            var b = new Board();
            b.Branches.Add(new BranchState { IsBonus = true, AdLocked = true });
            return b;
        }

        static void Odds(System.Action<string, bool, string> Check)
        {
            PlayerPrefs.DeleteKey(LightningStrike.PrefKey);
            Check("on-by-default", LightningStrike.Enabled && LightningStrike.RemoteDefault, "enabled=" + LightningStrike.Enabled);
            LightningStrike.Enabled = false;
            Check("explicit-off-still-off", !LightningStrike.Enabled, "pref 0 wins over the default");

            Check("still-capped-one-per-level", LightningStrike.MaxPerLevel == 1, "cap=" + LightningStrike.MaxPerLevel);
            int hits = 0;
            const int n = 1000;
            for (int i = 0; i < n; i++)
                if (LightningStrike.RollHits((i + 0.5f) / n, 0.60f)) hits++;
            Check("roll-against-chance", hits == 600 && hits < n, "hits " + hits + "/" + n);

            // b113: cooldown 3 gardens, then 15% +15%/garden, cap 90%.
            bool cool = true;
            for (int since = 0; since <= LightningStrike.CooldownGardens; since++)
                cool &= LightningStrike.ChanceFor(since) == 0f;
            Check("cooldown-no-strikes", cool && LightningStrike.CooldownGardens == 3, "cooldown=" + LightningStrike.CooldownGardens);
            float prev = 0f;
            bool ramp = true;
            for (int since = 4; since <= 12; since++)
            {
                float c = LightningStrike.ChanceFor(since);
                ramp &= c > prev || (c == LightningStrike.CapChance && prev == LightningStrike.CapChance);
                prev = c;
            }
            Check("ramp-progressive-then-capped",
                ramp && Mathf.Abs(LightningStrike.ChanceFor(4) - 0.15f) < 0.0001f
                && Mathf.Abs(LightningStrike.ChanceFor(5) - 0.30f) < 0.0001f
                && Mathf.Abs(LightningStrike.ChanceFor(9) - 0.90f) < 0.0001f
                && Mathf.Abs(LightningStrike.ChanceFor(50) - 0.90f) < 0.0001f,
                "4=" + LightningStrike.ChanceFor(4) + " 9=" + LightningStrike.ChanceFor(9) + " 50=" + LightningStrike.ChanceFor(50));

            bool hadSince = PlayerPrefs.HasKey(LightningStrike.SinceKey);
            int sinceKeep = PlayerPrefs.GetInt(LightningStrike.SinceKey, 0);
            PlayerPrefs.DeleteKey(LightningStrike.SinceKey);
            LightningStrike.NoteGarden();
            LightningStrike.NoteGarden();
            bool counted = LightningStrike.Since == 2;
            LightningStrike.MarkStruck();
            bool reset = LightningStrike.Since == 0;
            LightningStrike.NoteGarden();
            bool restarts = LightningStrike.Since == 1 && LightningStrike.ChanceFor(LightningStrike.Since) == 0f;
            Check("counter-persists-and-resets-on-strike", counted && reset && restarts, "counted=" + counted + " reset=" + reset + " restart=" + restarts);
            if (hadSince) PlayerPrefs.SetInt(LightningStrike.SinceKey, sinceKeep);
            else PlayerPrefs.DeleteKey(LightningStrike.SinceKey);
        }

        static void Guarantee(System.Action<string, bool, string> Check)
        {
            LightningStrike.Enabled = true;
            var claimed = new bool[2];
            var burned = new bool[2];
            var win = EmptyGift();
            int bad = 0;
            // b126: the owed intro strike is garden 3's (index 2). A losing roll misses on the dice; owed skips them.
            int dice = LightningStrike.TryPick(win, claimed, burned, true, 0, 0.99f, false, false, 2, 0.5f);
            int owed = LightningStrike.TryPick(win, claimed, burned, true, 0, 0.99f, false, true, 2, 0.5f);
            if (dice >= 0) bad++;
            if (owed != 0) bad++;
            // ...and Decide only hands out an owed (Intro) plan on garden 3, behind the 3 s + Wet > 0.3 gate.
            var g3 = LightningStrike.Decide(2, false, false, LightningStrike.IntroDelay, true, false, false, false, 0.58f);
            var g3Early = LightningStrike.Decide(2, false, true, LightningStrike.IntroDelay - 0.1f, true, false, false, false, 1f);
            var g4 = LightningStrike.Decide(3, false, true, 60f, true, false, true, false, 1f);
            if (g3 != LightningStrike.Plan.Intro || g3Early != LightningStrike.Plan.None || g4 != LightningStrike.Plan.Roll) bad++;
            Check("owed-skips-the-dice", bad == 0, "dice=" + dice + " owed=" + owed + " g3=" + g3 + " g3early=" + g3Early + " g4=" + g4);

            // b113: garden 1 (index 0) never strikes: not by dice, not owed, not forced. Garden 2 (index 1) is fine.
            int g1Dice = LightningStrike.TryPick(win, claimed, burned, true, 0, 0.0f, false, false, 0, 1f);
            int g1Owed = LightningStrike.TryPick(win, claimed, burned, true, 0, 0.99f, false, true, 0, 1f);
            int g1Force = LightningStrike.TryPick(win, claimed, burned, true, 0, 0.0f, true, true, 0, 1f);
            int g2Owed = LightningStrike.TryPick(win, claimed, burned, true, 0, 0.99f, false, true, 1, 0f);
            int g3Owed = LightningStrike.TryPick(win, claimed, burned, true, 0, 0.99f, false, true, 2, 0f);
            Check("gardens-1-2-never-strike-garden-3-owed",
                g1Dice < 0 && g1Owed < 0 && g1Force < 0 && g2Owed < 0 && g3Owed == 0
                && !LightningStrike.GardenEligible(0) && !LightningStrike.GardenEligible(1) && LightningStrike.GardenEligible(2)
                && LightningStrike.MinGarden == 2,
                "g1=" + g1Dice + "/" + g1Owed + "/" + g1Force + " g2owed=" + g2Owed + " g3owed=" + g3Owed);

            int dry = LightningStrike.TryPick(win, claimed, burned, false, 0, 0.0f, false, true);
            int capped = LightningStrike.TryPick(win, claimed, burned, true, 1, 0.0f, false, true);
            LightningStrike.Enabled = false;
            int off = LightningStrike.TryPick(win, claimed, burned, true, 0, 0.0f, false, true);
            LightningStrike.Enabled = true;
            var normal = new Board();
            normal.Branches.Add(new BranchState());
            int none = LightningStrike.TryPick(normal, claimed, burned, true, 0, 0.0f, false, true);
            var stuck = new Board();
            var limb = new BranchState();
            limb.Birds.Add(new Bird(BirdColor.Ruby));
            stuck.Branches.Add(limb);
            stuck.Branches.Add(new BranchState { IsBonus = true, AdLocked = true });
            bool unsolvable = !LightningStrike.KeepsSolvable(stuck, 1);
            int brick = unsolvable ? LightningStrike.TryPick(stuck, claimed, burned, true, 0, 0.0f, false, true) : -2;
            Check("owed-never-overrides-fairness",
                dry < 0 && capped < 0 && off < 0 && none < 0 && (!unsolvable || brick < 0),
                "dry=" + dry + " cap=" + capped + " off=" + off + " noTarget=" + none + " brick=" + brick);

            PlayerPrefs.DeleteKey(LightningStrike.StruckKey);
            bool before = LightningStrike.FirstStrikeOwed;
            LightningStrike.MarkStruck();
            bool after = LightningStrike.FirstStrikeOwed;
            Check("guarantee-spent-once", before && !after, "before=" + before + " after=" + after);
        }

        static void Lesson(System.Action<string, bool, string> Check)
        {
            bool kind = CoachTap.Kind(CoachStep.Fire) == CoachTapKind.Informational
                && CoachTap.AnyTap(CoachStep.Fire)
                && CoachTap.Advances(CoachStep.Fire, false)
                && CoachTap.Rule(CoachStep.Fire).Pointer == CoachPointer.Arrow
                && CoachTap.Rule(CoachStep.Fire).Advances(TapZone.Blank)
                && CoachTap.Caption(CoachStep.Fire) == CaptionLayout.FireLine;
            Check("fire-step-is-informational-arrow", kind, CoachTap.Kind(CoachStep.Fire).ToString());

            Check("owed-once-seen-never-again",
                FlockFiveApp.FireLessonOwed(false, true, 5f, 5f)
                && !FlockFiveApp.FireLessonOwed(true, true, 99f, 0f)
                && !FlockFiveApp.FireLessonOwed(false, false, 99f, 0f)
                && !FlockFiveApp.FireLessonOwed(false, true, 4.9f, 5f)
                && FlockFiveApp.FireLessonDelay > 0.5f,
                "delay=" + FlockFiveApp.FireLessonDelay);
            // b113: the lesson can never arm on garden 1.
            string fl = Read("Assets/_Project/Scripts/App/FlockFiveApp.FireLesson.cs");
            Check("lesson-blocked-on-garden-1",
                fl.IndexOf("GardenEligible(_lightningGarden)") >= 0 && fl.Split(new[] { "GardenEligible(_lightningGarden)" }, System.StringSplitOptions.None).Length >= 3,
                "FireDue + FireCueAlive gated");
            Check("stamp-flag", (GardenLesson.Fire & GardenLesson.Bee) == 0, "own bit");
        }

        static void Caption(System.Action<string, bool, string> Check)
        {
            string line = CaptionLayout.FireLine;
            Check("copy-is-exact-text",
                line == "Lightning burned the branch!\nWatch out for dangerous weather"
                && line.IndexOf('\uD83D') < 0 && line.IndexOf('\uD83D') < 0,
                "[" + line.Replace("\n", " / ") + "]");
            int rows = line.Split('\n').Length;
            Check("copy-two-lines-max", rows == 2 && rows <= CaptionLayout.BannerMaxLines, "rows=" + rows);
            string gapped = CaptionLayout.HeadlineGap(line);
            Check("visible-gap-between-sentences", gapped == "Lightning burned the branch!\n\nWatch out for dangerous weather"
                && CaptionLayout.IsFireLine(gapped) && CaptionLayout.IsFireLine(line), "[" + gapped.Replace("\n", "|") + "]");
            string src = Read("Assets/_Project/Scripts/App/CaptionLayout.cs");
            Check("emoji-swap-removed-for-fire", src.IndexOf("FireEmojiLine") < 0, "no FireEmojiLine");
            var faces = new[] { new Vector2(1179f, 2556f), new Vector2(750f, 1334f), new Vector2(1536f, 2048f) };
            for (int i = 0; i < faces.Length; i++)
            {
                float w = faces[i].x, h = faces[i].y;
                DeviceFit.TestWindow = w / h;
                int px = CaptionLayout.BannerFont(w, h);
                var style = FlockFiveApp.CoachCaptionStyle();
                float inner = CaptionLayout.BannerMeasureWidth(w, style, line, px);
                bool padded = inner <= w * CaptionLayout.FireInsetFrac + 0.5f && CaptionLayout.FireInsetFrac < 0.9f;
                var fit = CaptionLayout.FitBanner(style, line, inner, px);
                // Like the bee line, a long row may shrink the font, never below 60%.
                bool ok = padded && fit.Lines >= 1 && fit.Lines <= CaptionLayout.BannerMaxLines
                    && fit.Font >= Mathf.RoundToInt(px * 0.6f) - 1;
                // Every painted row stays inside the padded column: side margin on both edges.
                ok &= CaptionLayout.ShownFits(style, fit.Shown, inner, fit.Font);
                Check("copy-fits-padded-" + (int)w + "x" + (int)h, ok, "lines " + fit.Lines + " font " + fit.Font + "/" + px
                    + " [" + (fit.Shown ?? "").Replace("\n", " / ") + "]");
            }
        }

        static string Read(string rel)
        {
            string path = Path.Combine(Application.dataPath, "..", rel);
            return File.Exists(path) ? File.ReadAllText(path) : "";
        }

        // Source-shape checks, the same style the earlier lightning tests use.
        static void Wiring(System.Action<string, bool, string> Check)
        {
            string tick = Read("Assets/_Project/Scripts/App/FlockFiveApp.Lightning.cs");
            string coach = Read("Assets/_Project/Scripts/App/FlockFiveApp.Coach.cs");
            Check("tick-guarantees-and-arms",
                tick.IndexOf("FirstStrikeOwed") >= 0 && tick.IndexOf("PestStageFree") >= 0
                && tick.IndexOf("ArmFireLesson") >= 0 && tick.IndexOf("MarkStruck") >= 0, "tick");
            Check("lesson-on-pest-cue-path",
                coach.IndexOf("PestCueFire") >= 0 && coach.IndexOf("CoachFireKey") >= 0
                && coach.IndexOf("CoachStep.Fire") >= 0 && coach.IndexOf("FireDue()") >= 0, "coach");
            string branch = Read("Assets/_Project/Scripts/View/BranchView.cs");
            Check("burn-timeline-lengthened",
                Mathf.Abs(BranchView.ScorchSeconds - 1.5f) < 0.0001f && BranchView.ScorchSeconds >= 0.58f * 2f
                && branch.IndexOf("AnimateFlames(t)") >= 0 && branch.IndexOf("while (t < ScorchSeconds)") >= 0
                && branch.IndexOf("yield return BreakAway()") >= 0 && FlockFiveApp.FireLessonDelay < BranchView.ScorchSeconds,
                "burn " + BranchView.ScorchSeconds + "s (was 0.58), lesson at " + FlockFiveApp.FireLessonDelay + "s");
            Check("burned-branch-disabled-immediately",
                Read("Assets/_Project/Scripts/App/LightningStrike.cs").IndexOf("st.Burned = true;\n            st.Broken = true") >= 0
                || Read("Assets/_Project/Scripts/App/LightningStrike.cs").IndexOf("st.Broken = true;") >= 0, "Burn() marks Broken at the strike");
            Check("fire-arrow-not-glove",
                coach.IndexOf("DrawFireArrow(s)") >= 0 && coach.IndexOf("CoachArrowAt(target, s)") >= 0
                && coach.IndexOf("if (_pestCue == PestCueFire)\n            {\n                _gloveVis = false;") >= 0,
                "arrow wired, glove hidden for the fire cue");
            string strike = Read("Assets/_Project/Scripts/App/LightningStrike.cs");
            Check("target-rules-untouched",
                strike.IndexOf("!st.AdLocked || st.Count > 0") >= 0 && strike.IndexOf("KeepsSolvable(board, index)") >= 0
                && strike.IndexOf("MaxPerLevel) return -1") >= 0, "strike");
        }
    }
}
#endif
