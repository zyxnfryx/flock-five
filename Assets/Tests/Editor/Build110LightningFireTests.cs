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
                DevSkip(Check);
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
            Check("fire-caption-roomier-gap", CaptionLayout.GapKeepFor(gapped) > CaptionLayout.HeadlineGapKeep
                && Mathf.Abs(CaptionLayout.GapKeepFor("One.\n\nTwo.") - CaptionLayout.HeadlineGapKeep) < 1e-5f, "keep " + CaptionLayout.FireGapKeep);
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
            // L3 plate: same bottom garden seat and 8s dismiss as the bee plate. Not in
            // GardenBottomLines, so that shared seat height is unchanged.
            Check("lightning-shares-bee-plate",
                coach.IndexOf("_pestCueUntil = (kind == PestCueSparrow || kind == PestCueOstrich)") >= 0
                && coach.IndexOf("kind == PestCueBee || kind == PestCueFire ? BeePlateSeconds") >= 0
                && coach.IndexOf("PestCueOstrich || kind == PestCueFire") < 0
                && coach.IndexOf("IsGardenBottomLine(text) || CaptionLayout.IsFireLine(text)") >= 0
                && coach.IndexOf("CaptionLayout.FireLine,") < 0,
                "timer + bottom seat");
            // PlayClock drops hitch frames and the paused tick never reads it. The plate
            // starts BeePlateSeconds on GateNow when BeginPestCue shows it.
            Check("lightning-starts-bee-seconds",
                coach.IndexOf("_firePlateUntil = GateNow + BeePlateSeconds") >= 0
                && coach.IndexOf("bool FirePlateTimedOut()") >= 0
                && coach.IndexOf("if (FirePlateTimedOut()) DismissPestIntro();") >= 0
                && coach.IndexOf("if (FirePlateTimedOut())") >= 0,
                "GateNow deadline");
            // L5 bee plate: same GateNow arm and the same dismiss, including while paused.
            Check("bee-plate-uses-gate-now",
                coach.IndexOf("if (kind == PestCueBee || kind == PestCueFire)") >= 0
                && coach.IndexOf("(_pestCue == PestCueBee || _pestCue == PestCueFire)") >= 0
                && coach.IndexOf("_ruleTip == RuleTip.None") >= 0,
                "bee shares the lightning deadline");
            // L5: full plate on the frame the 8s starts. Lightning still fades in.
            Check("bee-plate-text-and-panel-together",
                coach.IndexOf("if (_pestCue == PestCueBee)\n                _coachFade = 1f;") >= 0
                && coach.IndexOf("_coachFade = Mathf.Min(1f, _coachFade + PlayClock.Delta / 0.30f);") >= 0
                && coach.IndexOf("_firePlateUntil = GateNow + BeePlateSeconds") >= 0,
                "bee fade 1, lightning ramp kept");
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

        // ?fflevel=3 / ?fflevel=5. The loader itself is WebGL development builds only.
        static void DevSkip(System.Action<string, bool, string> Check)
        {
            Check("fflevel-absent", DevLevelSkip.DisplayFromUrl("http://h/index.html") == 0
                && DevLevelSkip.DisplayFromUrl(null) == 0 && DevLevelSkip.DisplayFromUrl("") == 0, "none");
            Check("fflevel-5", DevLevelSkip.DisplayFromUrl("http://h/index.html?fflevel=5") == 5, "5");
            Check("fflevel-3-among", DevLevelSkip.DisplayFromUrl("http://h/?a=1&fflevel=3&b=2#x") == 3, "3");
            Check("fflevel-junk", DevLevelSkip.DisplayFromUrl("http://h/?fflevel=L5") == 0
                && DevLevelSkip.DisplayFromUrl("http://h/?fflevel=") == 0
                && DevLevelSkip.DisplayFromUrl("http://h/?fflevel=0") == 0, "junk");
            var l3 = DevLevelSkip.Plan("http://h/?fflevel=3", 50);
            Check("plan-l3-keeps-fire", l3.Index == 2 && l3.KeepFire && !l3.KeepBee && !l3.SkipOnboarding, "index " + l3.Index);
            var l5 = DevLevelSkip.Plan("http://h/?fflevel=5", 50);
            Check("plan-l5-keeps-bee", l5.Index == 4 && l5.KeepBee && !l5.KeepFire && !l5.SkipOnboarding, "index " + l5.Index);
            var other = DevLevelSkip.Plan("http://h/?fflevel=4", 50);
            Check("plan-other-stamps-both", other.Index == 3 && !other.KeepBee && !other.KeepFire && !other.SkipOnboarding, "index " + other.Index);
            var bad = DevLevelSkip.Plan("http://h/?fflevel=99", 50);
            Check("plan-out-of-range", bad.Index < 0 && DevLevelSkip.Plan("http://h/?fflevel=5", 0).Index < 0, "index " + bad.Index);
            Check("onboard-absent", !DevLevelSkip.OnboardingFromUrl("http://h/index.html")
                && !DevLevelSkip.OnboardingFromUrl(null) && !DevLevelSkip.OnboardingFromUrl(""), "none");
            Check("onboard-1", DevLevelSkip.OnboardingFromUrl("http://h/portrait?w=820&h=1180&skipOnboarding=1"), "1");
            Check("onboard-among", DevLevelSkip.OnboardingFromUrl("http://h/?fflevel=5&skipOnboarding=1#x")
                && DevLevelSkip.OnboardingFromUrl("http://h/?skipOnboarding=1&fflevel=3"), "both");
            Check("onboard-junk", !DevLevelSkip.OnboardingFromUrl("http://h/?skipOnboarding=0")
                && !DevLevelSkip.OnboardingFromUrl("http://h/?skipOnboarding=")
                && !DevLevelSkip.OnboardingFromUrl("http://h/?skipOnboarding=true")
                && !DevLevelSkip.OnboardingFromUrl("http://h/?skipOnboarding=2"), "junk");
            var garden2 = DevLevelSkip.Plan("http://h/portrait?w=820&h=1180&skipOnboarding=1", 50);
            Check("plan-onboard-garden-2", garden2.Index == 1 && garden2.SkipOnboarding && !garden2.KeepBee && !garden2.KeepFire
                && !garden2.KeepTips && !garden2.ForceFeeder && !garden2.ForceNap && !garden2.OpenPoker,
                "index " + garden2.Index);
            Check("force-feeder-1", DevLevelSkip.ForceFeederFromUrl(
                "http://h/portrait?w=820&h=1180&skipOnboarding=1&keepTips=1&forceFeeder=1"), "1");
            Check("force-nap-1", DevLevelSkip.ForceNapFromUrl(
                "http://h/?skipOnboarding=1&forceNap=1"), "1");
            Check("open-poker-1", DevLevelSkip.OpenPokerFromUrl(
                "http://h/portrait?w=820&h=1180&skipOnboarding=1&openPoker=1"), "1");
            Check("force-junk", !DevLevelSkip.ForceFeederFromUrl("http://h/?forceFeeder=0")
                && !DevLevelSkip.ForceFeederFromUrl("http://h/?forceFeeder=true")
                && !DevLevelSkip.ForceNapFromUrl("http://h/?forceNap=")
                && !DevLevelSkip.ForceNapFromUrl("http://h/?forceNap=2")
                && !DevLevelSkip.OpenPokerFromUrl("http://h/?openPoker=0")
                && !DevLevelSkip.OpenPokerFromUrl(null), "junk");
            var feed = DevLevelSkip.Plan("http://h/portrait?w=820&h=1180&skipOnboarding=1&keepTips=1&forceFeeder=1", 50);
            Check("plan-force-feeder", feed.Index == 1 && feed.SkipOnboarding && feed.KeepTips && feed.ForceFeeder
                && !feed.ForceNap && !feed.OpenPoker, "index " + feed.Index);
            var nap = DevLevelSkip.Plan("http://h/?skipOnboarding=1&forceNap=1", 50);
            Check("plan-force-nap", nap.Index == 1 && nap.SkipOnboarding && nap.ForceNap && !nap.KeepTips
                && !nap.ForceFeeder && !nap.OpenPoker, "index " + nap.Index);
            Check("nap-plate-not-before-zzz", !DevLevelSkip.NapPlateReady(false, false, 2), "asleep only");
            Check("nap-plate-not-under-curtain", !DevLevelSkip.NapPlateReady(true, true, 2), "held");
            Check("nap-plate-needs-a-frame", !DevLevelSkip.NapPlateReady(true, false, 0), "same frame");
            Check("nap-plate-after-visible-zzz", DevLevelSkip.NapPlateReady(true, false, 1), "open");
            var poker = DevLevelSkip.Plan("http://h/portrait?w=820&h=1180&skipOnboarding=1&openPoker=1", 50);
            Check("plan-open-poker", poker.Index == 1 && poker.SkipOnboarding && poker.OpenPoker
                && !poker.ForceFeeder && !poker.ForceNap && !poker.KeepTips, "index " + poker.Index);
            var pokerBad = DevLevelSkip.Plan("http://h/?fflevel=99&skipOnboarding=1&openPoker=1", 50);
            Check("plan-open-poker-bad-level", pokerBad.Index < 0 && pokerBad.SkipOnboarding && pokerBad.OpenPoker,
                "index " + pokerBad.Index);
            var forceAlone = DevLevelSkip.Plan("http://h/?fflevel=4&forceFeeder=1&forceNap=1&openPoker=1", 50);
            Check("plan-force-needs-skip", forceAlone.Index == 3 && !forceAlone.SkipOnboarding
                && !forceAlone.ForceFeeder && !forceAlone.ForceNap && !forceAlone.OpenPoker, "index " + forceAlone.Index);
            Check("keeptips-absent", !DevLevelSkip.KeepTipsFromUrl("http://h/index.html")
                && !DevLevelSkip.KeepTipsFromUrl(null) && !DevLevelSkip.KeepTipsFromUrl(""), "none");
            Check("keeptips-1", DevLevelSkip.KeepTipsFromUrl(
                "http://h/portrait?w=820&h=1180&skipOnboarding=1&keepTips=1"), "1");
            Check("keeptips-among", DevLevelSkip.KeepTipsFromUrl("http://h/?fflevel=5&skipOnboarding=1&keepTips=1#x")
                && DevLevelSkip.KeepTipsFromUrl("http://h/?keepTips=1&skipOnboarding=1"), "both");
            Check("keeptips-junk", !DevLevelSkip.KeepTipsFromUrl("http://h/?keepTips=0")
                && !DevLevelSkip.KeepTipsFromUrl("http://h/?keepTips=")
                && !DevLevelSkip.KeepTipsFromUrl("http://h/?keepTips=true")
                && !DevLevelSkip.KeepTipsFromUrl("http://h/?keepTips=2"), "junk");
            var kept = DevLevelSkip.Plan("http://h/portrait?w=820&h=1180&skipOnboarding=1&keepTips=1", 50);
            Check("plan-keeptips-garden-2", kept.Index == 1 && kept.SkipOnboarding && kept.KeepTips
                && !kept.KeepBee && !kept.KeepFire, "index " + kept.Index);
            var kept5 = DevLevelSkip.Plan("http://h/?skipOnboarding=1&fflevel=5&keepTips=1", 50);
            Check("plan-keeptips-keeps-fflevel", kept5.Index == 4 && kept5.SkipOnboarding && kept5.KeepTips
                && kept5.KeepBee && !kept5.KeepFire, "index " + kept5.Index);
            var tipsOnly = DevLevelSkip.Plan("http://h/?fflevel=4&keepTips=1", 50);
            Check("plan-keeptips-needs-skip", tipsOnly.Index == 3 && !tipsOnly.SkipOnboarding && !tipsOnly.KeepTips
                && !tipsOnly.KeepBee && !tipsOnly.KeepFire, "index " + tipsOnly.Index);
            var with5 = DevLevelSkip.Plan("http://h/?skipOnboarding=1&fflevel=5", 50);
            Check("plan-onboard-keeps-fflevel", with5.Index == 4 && with5.SkipOnboarding && with5.KeepBee && !with5.KeepFire,
                "index " + with5.Index);
            var with3 = DevLevelSkip.Plan("http://h/?fflevel=3&skipOnboarding=1", 50);
            Check("plan-onboard-keeps-fire", with3.Index == 2 && with3.SkipOnboarding && with3.KeepFire && !with3.KeepBee
                && !with3.KeepTips, "index " + with3.Index);
            Check("plan-default-does-not-keep-tips",
                !l3.KeepTips && !l5.KeepTips && !other.KeepTips && !with5.KeepTips && !with3.KeepTips, "off");
            var skipBad = DevLevelSkip.Plan("http://h/?fflevel=99&skipOnboarding=1", 50);
            Check("plan-onboard-bad-fflevel", skipBad.Index < 0
                && DevLevelSkip.Plan("http://h/?skipOnboarding=1", 1).Index < 0, "index " + skipBad.Index);
            Check("onboard-coins", DevLevelSkip.OnboardingCoins >= 1000
                && DevLevelSkip.CoinsToSeed(0) == DevLevelSkip.OnboardingCoins
                && DevLevelSkip.CoinsToSeed(400) == DevLevelSkip.OnboardingCoins - 400
                && DevLevelSkip.CoinsToSeed(DevLevelSkip.OnboardingCoins) == 0
                && DevLevelSkip.CoinsToSeed(DevLevelSkip.OnboardingCoins + 500) == 0
                && DevLevelSkip.CoinsToSeed(-3) == DevLevelSkip.OnboardingCoins,
                "stake " + DevLevelSkip.OnboardingCoins);
            string settings = Read("Assets/_Project/Scripts/App/FlockFiveApp.Settings.cs");
            string app = Read("Assets/_Project/Scripts/App/FlockFiveApp.cs");
            int gate = settings.IndexOf("#if UNITY_WEBGL && DEVELOPMENT_BUILD");
            int hook = settings.IndexOf("void MaybeDevLevelSkip()");
            int end = hook >= 0 ? settings.IndexOf("#endif", hook) : -1;
            int call = app.IndexOf("MaybeDevLevelSkip();");
            int appGate = app.IndexOf("#if UNITY_WEBGL && DEVELOPMENT_BUILD");
            string gated = gate >= 0 && end > gate ? settings.Substring(gate, end - gate) : "";
            Check("skip-is-webgl-dev-only",
                gate >= 0 && hook > gate && end > hook
                && settings.IndexOf("?fflevel=3") >= 0 && settings.IndexOf("?fflevel=5") >= 0
                && appGate >= 0 && call > appGate && app.IndexOf("#endif", call) > call,
                "gate");
            int stamp = gated.IndexOf("void StampDevOnboardingSeen()");
            int coins = stamp >= 0 ? gated.IndexOf("Purse.Credit", stamp) : -1;
            int sleepSet = gated.IndexOf("SetInt(RuleTips.SleepKey, 1)");
            int sleepDrop = gated.IndexOf("DeleteKey(RuleTips.SleepKey)");
            int early = gated.IndexOf("_devOnboardingStamped = true");
            int earlyDismiss = early >= 0 ? gated.IndexOf("AdConsent.DismissTransient()", early) : -1;
            int loadGate = earlyDismiss >= 0 ? gated.IndexOf("if (!AdConsent.FirstFrameReady) return;", earlyDismiss) : -1;
            Check("onboard-stamps-live-keys",
                gated.IndexOf("FF_DEV_SKIP_ONBOARDING") >= 0
                && gated.IndexOf("coins=") >= 0
                && stamp >= 0
                && coins > stamp
                && gated.IndexOf("DevLevelSkip.CoinsToSeed") >= 0
                && gated.IndexOf("AdConsent.AttAskKey") >= 0
                && gated.IndexOf("AdConsent.EditorHoldAtt = false") >= 0
                && early >= 0 && earlyDismiss > early && loadGate > earlyDismiss
                && gated.IndexOf("AvatarAdoptedPref") >= 0
                && gated.IndexOf("AvatarNamedPref") >= 0
                && gated.IndexOf("AvatarNamePref") >= 0
                && gated.IndexOf("CoachDailyKey") >= 0
                && gated.IndexOf("CoachPokerKey") >= 0
                && gated.IndexOf("SetInt(CoachPokerBackKey") < 0
                && gated.IndexOf("CoachHiveKey") >= 0
                && gated.IndexOf("CoachHiveGardenKey") >= 0
                && gated.IndexOf("CoachHiveAlbumKey") >= 0
                && sleepDrop >= 0 && sleepDrop < stamp
                && sleepSet > sleepDrop
                && gated.IndexOf("if (plan.SkipOnboarding) PlayerPrefs.DeleteKey(RuleTips.SleepKey);") >= 0
                && settings.IndexOf("FF_DEV_SKIP_ONBOARDING") == gated.IndexOf("FF_DEV_SKIP_ONBOARDING") + gate
                && settings.IndexOf("FF_DEV_SKIP_ONBOARDING", end) < 0
                && settings.IndexOf("StampDevOnboardingSeen", end) < 0,
                "stamps");
            int keepIf = gated.IndexOf("if (plan.KeepTips)");
            int keepElse = keepIf >= 0 ? gated.IndexOf("\n            else", keepIf) : -1;
            string keepBody = keepIf >= 0 && keepElse > keepIf ? gated.Substring(keepIf, keepElse - keepIf) : "";
            int leafSet = gated.IndexOf("SetInt(CoachLeafKey, 1)");
            int coachSet = gated.IndexOf("SetInt(CoachKey, 1)");
            Check("keeptips-owes-rule-plates",
                keepIf >= 0 && keepElse > keepIf && keepElse < stamp
                && keepBody.IndexOf("DeleteKey(RuleTips.KitKey)") >= 0
                && keepBody.IndexOf("DeleteKey(RuleTips.FeederKey)") >= 0
                && keepBody.IndexOf("DeleteKey(RuleTips.SleepKey)") >= 0
                && keepBody.IndexOf("SetInt(RuleTips") < 0
                && gated.IndexOf("SetInt(RuleTips.KitKey, 1)") > keepElse
                && gated.IndexOf("SetInt(RuleTips.FeederKey, 1)") > keepElse
                && gated.IndexOf("if (plan.SkipOnboarding) PlayerPrefs.DeleteKey(RuleTips.SleepKey);") > keepElse
                && coachSet >= 0 && coachSet < keepIf
                && leafSet >= 0 && leafSet < keepIf
                && gated.IndexOf("keepTips=\" +") > keepElse && gated.IndexOf("keepTips=\" +") < stamp
                && settings.IndexOf("keepTips=\" +", end) < 0
                && gated.IndexOf("Purse.Credit", stamp) > 0
                && gated.IndexOf("AdConsent.AttAskKey", stamp) > 0,
                "owed");
            int pokerIf = gated.IndexOf("if (plan.OpenPoker)");
            int loadCall = gated.IndexOf("Load(plan.Index)");
            int showFeeder = gated.IndexOf("ShowRuleTip(RuleTip.Feeder)");
            int raiseSleep = gated.IndexOf("RaiseRuleSleepTip()");
            int forceLog = gated.IndexOf("FF_DEV_FORCE");
            Check("dev-force-plates",
                pokerIf > keepElse && loadCall > pokerIf
                && showFeeder > loadCall
                && raiseSleep > loadCall
                && gated.IndexOf("ShowRuleTip(RuleTip.Sleep)") < 0
                && gated.IndexOf("_devNapHold = true") >= 0
                && gated.IndexOf("NapPlateReady") > gated.IndexOf("_devNapHold = true")
                && gated.IndexOf("RaiseRuleSleepTip()") > gated.IndexOf("NapPlateReady")
                && gated.IndexOf("DeleteKey(CoachPokerBackKey)") > pokerIf
                && gated.IndexOf("SetInt(CoachPokerBackKey") < 0
                && gated.IndexOf("HomeFace.Poker") > pokerIf
                && gated.IndexOf("_pokerLessonHand = true") > pokerIf
                && gated.IndexOf("BirdPoker.Draw()") > pokerIf
                && gated.IndexOf("IsSleeping(branch)") > 0
                && forceLog >= 0 && forceLog < gated.Length
                && settings.IndexOf("FF_DEV_FORCE", end) < 0
                && settings.IndexOf("DevOpenPokerTable", end) < 0
                && settings.IndexOf("DevForceSleepingFlock", end) < 0
                && settings.IndexOf("ShowRuleTip(RuleTip.Feeder)", end) < 0,
                "force");
        }
    }
}
#endif
