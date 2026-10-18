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

            Check("chance-raised", Mathf.Abs(LightningStrike.Chance - 0.60f) < 0.0001f && LightningStrike.Chance > 0.45f,
                "chance=" + LightningStrike.Chance);
            Check("still-capped-one-per-level", LightningStrike.MaxPerLevel == 1, "cap=" + LightningStrike.MaxPerLevel);
            int hits = 0;
            const int n = 1000;
            for (int i = 0; i < n; i++)
                if (LightningStrike.RollHits((i + 0.5f) / n)) hits++;
            Check("not-every-strike", hits == 600 && hits < n, "hits " + hits + "/" + n);
        }

        static void Guarantee(System.Action<string, bool, string> Check)
        {
            LightningStrike.Enabled = true;
            var claimed = new bool[2];
            var burned = new bool[2];
            var win = EmptyGift();
            int bad = 0;
            int dice = LightningStrike.TryPick(win, claimed, burned, true, 0, 0.99f, false, false);
            int owed = LightningStrike.TryPick(win, claimed, burned, true, 0, 0.99f, false, true);
            if (dice >= 0) bad++;
            if (owed != 0) bad++;
            Check("owed-skips-the-dice", bad == 0, "dice=" + dice + " owed=" + owed);

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
                && CoachTap.Rule(CoachStep.Fire).Pointer == CoachPointer.Glove
                && CoachTap.Caption(CoachStep.Fire) == CaptionLayout.FireLine;
            Check("fire-step-is-informational-glove", kind, CoachTap.Kind(CoachStep.Fire).ToString());

            Check("owed-once-seen-never-again",
                FlockFiveApp.FireLessonOwed(false, true, 5f, 5f)
                && !FlockFiveApp.FireLessonOwed(true, true, 99f, 0f)
                && !FlockFiveApp.FireLessonOwed(false, false, 99f, 0f)
                && !FlockFiveApp.FireLessonOwed(false, true, 4.9f, 5f)
                && FlockFiveApp.FireLessonDelay > 0.5f,
                "delay=" + FlockFiveApp.FireLessonDelay);
            Check("stamp-flag", (GardenLesson.Fire & GardenLesson.Bee) == 0, "own bit");
        }

        static void Caption(System.Action<string, bool, string> Check)
        {
            string line = CaptionLayout.FireLine;
            int rows = line.Split('\n').Length;
            Check("copy-two-lines-max", rows <= 2 && rows <= CaptionLayout.BannerMaxLines, "rows=" + rows);
            var faces = new[] { new Vector2(1179f, 2556f), new Vector2(750f, 1334f), new Vector2(1536f, 2048f) };
            for (int i = 0; i < faces.Length; i++)
            {
                float w = faces[i].x, h = faces[i].y;
                DeviceFit.TestWindow = w / h;
                int px = CaptionLayout.BannerFont(w, h);
                var style = FlockFiveApp.CoachCaptionStyle();
                float inner = CaptionLayout.BannerMeasureWidth(w, style, line, px);
                var fit = CaptionLayout.FitBanner(style, line, inner, px);
                bool ok = fit.Lines >= 1 && fit.Lines <= CaptionLayout.BannerMaxLines;
                Check("copy-fits-" + (int)w + "x" + (int)h, ok, "lines " + fit.Lines + " font " + fit.Font + "/" + px
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
            string strike = Read("Assets/_Project/Scripts/App/LightningStrike.cs");
            Check("target-rules-untouched",
                strike.IndexOf("!st.AdLocked || st.Count > 0") >= 0 && strike.IndexOf("KeepsSolvable(board, index)") >= 0
                && strike.IndexOf("MaxPerLevel) return -1") >= 0, "strike");
        }
    }
}
#endif
