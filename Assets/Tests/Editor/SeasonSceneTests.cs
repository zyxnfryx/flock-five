#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Season transition scene (replaces the "Welcome to the <season>" banner). Rules under test:
    //   - timeline: ~2.5 s, wipe 0-0.8, particles 0.4-1.6, birds + sting at 1.0, settle + label 1.6-2.5
    //   - gating: ostrich gardens never get a scene, no scene during a tutorial, desert is not a season scene
    //   - tap skip, mid-scene tutorial cut, and badger completion (the scene is Done before a badger action)
    //   - sting is a clean hook (Sfx.SeasonStingHook) that falls back to the existing whoosh
    //   - winter: sparse fluttering flakes in place of rain, and never thunder or lightning
    // Pure checks only: no scene, no Unity instance needed beyond the editor domain.
    public static class SeasonSceneTests
    {
        const string Out = "/tmp/flock-five-season-scene-tests.txt";
        static readonly GardenScene[] Seasons =
            { GardenScene.Spring, GardenScene.Summer, GardenScene.Fall, GardenScene.Winter };

        [MenuItem("Flock Five/Season Scene Tests")]
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
                Debug.Log("[season-scene] " + line);
            }

            Timeline(Check);
            Gating(Check);
            ClockRules(Check);
            BadgerCompletion(Check);
            Birds(Check);
            StingHook(Check);
            Winter(Check);
            Wiring(Check);

            string end = fail == 0 ? "SEASON_SCENE_OK " + pass : "SEASON_SCENE_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[season-scene] " + end);
            if (fail > 0) throw new Exception(end);
        }

        static bool Near(float a, float b, float eps = 0.001f) => Mathf.Abs(a - b) <= eps;

        static void Timeline(Action<string, bool, string> Check)
        {
            Check("duration", Near(SeasonScene.Duration, 2.5f), "scene is 2.5 s");
            Check("wipe-0-0.8", Near(SeasonScene.Wipe(0f), 0f) && Near(SeasonScene.Wipe(0.4f), 0.5f)
                && Near(SeasonScene.Wipe(0.8f), 1f) && Near(SeasonScene.Wipe(2f), 1f), "wipe done at 0.8");
            Check("particles-0.4-1.6", Near(SeasonScene.ParticleIn(0.39f), 0f) && SeasonScene.ParticleIn(1.0f) > 0.4f
                && SeasonScene.ParticleIn(1.0f) < 0.6f && Near(SeasonScene.ParticleIn(1.6f), 1f), "enter 0.4 to 1.6");
            Check("settle-1.6-2.5", Near(SeasonScene.Burst(1.6f), 1f) && Near(SeasonScene.Burst(2.5f), 0f)
                && SeasonScene.Burst(2.0f) < 1f && SeasonScene.Burst(2.0f) > 0f && Near(SeasonScene.Energy(2.5f), 1f),
                "burst hands over to ambient by 2.5");
            Check("bird-and-sting-at-1.0", SeasonScene.BirdPhase(0.99f) < 0f && Near(SeasonScene.BirdPhase(1.0f), 0f)
                && Near(SeasonScene.BirdPhase(2.5f), 1f) && Near(SeasonScene.BirdAt, 1.0f) && Near(SeasonScene.StingAt, 1.0f),
                "reaction and sting start at 1.0");
            Check("label-fades-1.6-2.5", Near(SeasonScene.LabelAlpha(1.59f), 0f, 0.02f) && SeasonScene.LabelAlpha(2.05f) > 0.95f
                && Near(SeasonScene.LabelAlpha(2.5f), 0f) && SeasonScene.LabelAlpha(0.5f) == 0f, "in then out, none before 1.6");
        }

        static void Gating(Action<string, bool, string> Check)
        {
            bool all = true;
            for (int i = 0; i < Seasons.Length; i++)
                all &= SeasonScene.Gate(Seasons[i], false, false) == SeasonScene.Skip.None && SeasonScene.HasScene(Seasons[i]);
            Check("gate-plays-for-four-seasons", all, "spring summer fall winter");

            bool ostrich = true;
            for (int i = 0; i < Seasons.Length; i++)
                ostrich &= SeasonScene.Gate(Seasons[i], true, false) == SeasonScene.Skip.Ostrich
                    && SeasonScene.Gate(Seasons[i], true, true) == SeasonScene.Skip.Ostrich;
            Check("gate-ostrich-skip", ostrich, "ostrich gardens never get a scene, tutorial or not");

            bool tut = true;
            for (int i = 0; i < Seasons.Length; i++)
                tut &= SeasonScene.Gate(Seasons[i], false, true) == SeasonScene.Skip.Tutorial;
            Check("gate-tutorial-skip", tut, "no scene while a tutorial glove is up");

            Check("gate-desert-not-season", SeasonScene.Gate(GardenScene.Desert, false, false) == SeasonScene.Skip.NotSeason
                && !SeasonScene.HasScene(GardenScene.Desert), "desert keeps its own reveal");
        }

        static void ClockRules(Action<string, bool, string> Check)
        {
            // Exact binary steps (0.25 and 0.125) so float sums land on the boundaries.
            var c = new SeasonClock();
            bool early = true;
            for (int i = 0; i < 9; i++) { c.Tick(0.25f, false); early &= !c.Done; }
            // 2.25 s so far
            bool notYet = early && c.T < SeasonScene.Duration;
            c.Tick(0.25f, false);
            Check("clock-finishes-at-2.5", notYet && c.Done && !c.Skipped && Near(c.T, 2.5f), "T=" + c.T);

            var s = new SeasonClock();
            bool stingEarly = false;
            for (int i = 0; i < 7; i++) { s.Tick(0.125f, false); stingEarly |= s.TakeSting() || s.TakeBirds(); }
            s.Tick(0.125f, false);
            bool atOne = s.TakeSting() && s.TakeBirds();
            bool once = !s.TakeSting() && !s.TakeBirds();
            Check("sting-once-at-1.0", !stingEarly && atOne && once, "sting and birds fire once at 1.0");

            var tap = new SeasonClock();
            tap.Tick(0.3f, false);
            tap.Skip();
            tap.Tick(1f, false);
            Check("tap-skip", tap.Done && tap.Skipped && Near(tap.T, 2.5f) && !tap.TakeSting() && !tap.TakeBirds(),
                "skip finishes at once, no late sting");

            var cut = new SeasonClock();
            cut.Tick(0.5f, false);
            cut.Tick(0.016f, true);
            Check("tutorial-cuts-live-scene", cut.Done && cut.Skipped, "a tutorial starting mid-scene ends it");

            var paused = new SeasonClock();
            paused.Tick(0f, false);
            paused.Tick(0f, false);
            Check("pause-holds-clock", !paused.Done && Near(paused.T, 0f), "dt 0 (pause/ad) does not advance");
        }

        static void BadgerCompletion(Action<string, bool, string> Check)
        {
            var live = new SeasonClock();
            live.Tick(1.2f, false);
            bool blocked = !SeasonScene.BadgerMayStart(live);
            // A badger action calls SeasonCrossfade.FinishLive, which is clock.Skip().
            live.Skip();
            bool after = SeasonScene.BadgerMayStart(live) && live.Done && Near(live.T, SeasonScene.Duration);
            var done = new SeasonClock();
            for (int i = 0; i < 10; i++) done.Tick(0.25f, false);
            Check("badger-waits-for-scene", blocked && after && SeasonScene.BadgerMayStart(null) && SeasonScene.BadgerMayStart(done),
                "blocked mid-scene, allowed once Done, none = allowed");

            // Every scene finishes inside its own 2.5 s, which is shorter than any badger action's first beat,
            // and a badger action waits for the scene above. A season change never sits on an ostrich garden (grid rule).
            Check("scene-shorter-than-3s", SeasonScene.Duration <= 2.5f, "");
            Check("grid-has-no-ostrich-change", GardenSeason.FirstOstrichClash(220) == 0, "no season change on an ostrich garden");
        }

        static void Birds(Action<string, bool, string> Check)
        {
            bool rest = true;
            for (int i = 0; i < Seasons.Length; i++)
            {
                var a = SeasonScene.BirdPose(Seasons[i], -1f);
                var z = SeasonScene.BirdPose(Seasons[i], 1f);
                rest &= Near(a.Sx, 1f) && Near(a.Sy, 1f) && Near(a.Dy, 0f)
                    && Near(z.Sx, 1f, 0.01f) && Near(z.Sy, 1f, 0.01f) && Near(z.Dy, 0f, 0.01f) && Near(z.Rot, 0f, 0.5f) && Near(z.Dx, 0f, 0.01f);
            }
            Check("birds-rest-before-and-after", rest, "no pose before 1.0, back at rest by 2.5");

            float maxHop = 0f;
            for (float u = 0f; u <= 1f; u += 0.01f) maxHop = Mathf.Max(maxHop, SeasonScene.BirdPose(GardenScene.Spring, u).Dy);
            float maxStretch = 0f;
            for (float u = 0f; u <= 1f; u += 0.01f) maxStretch = Mathf.Max(maxStretch, SeasonScene.BirdPose(GardenScene.Summer, u).Sy);
            float maxTilt = 0f;
            for (float u = 0f; u <= 1f; u += 0.01f) maxTilt = Mathf.Max(maxTilt, Mathf.Abs(SeasonScene.BirdPose(GardenScene.Fall, u).Rot));
            float shiver = 0f, fluff = 0f;
            for (float u = 0f; u <= 0.49f; u += 0.01f) shiver = Mathf.Max(shiver, Mathf.Abs(SeasonScene.BirdPose(GardenScene.Winter, u).Dx));
            for (float u = 0.5f; u <= 1f; u += 0.01f) fluff = Mathf.Max(fluff, SeasonScene.BirdPose(GardenScene.Winter, u).Sx);
            Check("birds-react-per-season", maxHop > 0.08f && maxStretch > 1.08f && maxTilt > 6f && shiver > 0.008f && fluff > 1.08f,
                "hop " + maxHop.ToString("0.00") + " stretch " + maxStretch.ToString("0.00") + " tilt " + maxTilt.ToString("0.0")
                + " shiver " + shiver.ToString("0.000") + " fluff " + fluff.ToString("0.00"));
        }

        static void StingHook(Action<string, bool, string> Check)
        {
            var prev = Sfx.SeasonStingHook;
            int calls = 0;
            GardenScene got = GardenScene.Desert;
            try
            {
                Sfx.SeasonStingHook = sc => { calls++; got = sc; return true; };
                Sfx.SeasonSting(GardenScene.Fall);
                Check("sting-hook", calls == 1 && got == GardenScene.Fall, "a custom sting handler is called with the season and replaces the whoosh");
            }
            finally { Sfx.SeasonStingHook = prev; }
        }

        static void Winter(Action<string, bool, string> Check)
        {
            Check("winter-no-thunder", !GardenStorm.ThunderAllowed(GardenScene.Winter)
                && GardenStorm.ThunderAllowed(GardenScene.Summer) && GardenStorm.ThunderAllowed(GardenScene.Fall)
                && GardenStorm.ThunderAllowed(GardenScene.Spring) && GardenStorm.ThunderAllowed(GardenScene.Desert),
                "thunder and lightning are off in winter only");

            var root = new GameObject("season-winter-test").transform;
            try
            {
                var w = GardenStorm.Attach(root, GardenScene.Winter);
                w.JumpToStorm();
                Check("winter-jump-to-storm-inert", w.IsSnow && GardenStorm.Wet == 0f, "Wet stays 0 so no rain, sky veil or thunder");
            }
            finally { UnityEngine.Object.DestroyImmediate(root.gameObject); }

            Check("winter-sparse-flakes", GardenStorm.SnowCountMul <= 0.15f && GardenStorm.SnowMinCount <= 6
                && GardenLife.SparseSnow <= 3 && GardenLife.SparseSnowLow <= GardenLife.SparseSnow
                && GardenStorm.SnowFlipHz > 0f && GardenLife.SnowFlipHz > 0f,
                "occasional flakes that flutter and flip, not a curtain");

            string app = "Assets/_Project/Scripts/App/";
            string[] files = { "SeasonScene.cs" };
            bool clean = true;
            string bad = "";
            for (int i = 0; i < files.Length; i++)
            {
                string text = File.Exists(app + files[i]) ? File.ReadAllText(app + files[i]) : "";
                string[] banned = { "ThunderCrack", "ThunderRoll", "Sfx.Thunder", "StrikeAt", "StrikeBranch", "JumpToStorm", "LightningStrike" };
                for (int b = 0; b < banned.Length; b++)
                    if (text.Contains(banned[b])) { clean = false; bad = files[i] + ":" + banned[b]; }
            }
            // SeasonCrossfade (in SeasonReveal.cs) only drives wipe, burst, birds, sting and label.
            string rev = File.Exists(app + "SeasonReveal.cs") ? File.ReadAllText(app + "SeasonReveal.cs") : "";
            string[] storm = { "ThunderCrack", "ThunderRoll", "Sfx.Thunder", "StrikeAt", "GardenStorm", "LightningStrike" };
            for (int b = 0; b < storm.Length; b++)
                if (rev.Contains(storm[b])) { clean = false; bad = "SeasonReveal.cs:" + storm[b]; }
            Check("transition-never-calls-storm", clean, bad == "" ? "season scene code has no thunder or lightning calls" : bad);
        }

        static void Wiring(Action<string, bool, string> Check)
        {
            Check("labels-and-wipes",
                SeasonScene.Label(GardenScene.Spring) == "Spring" && SeasonScene.Label(GardenScene.Summer) == "Summer"
                && SeasonScene.Label(GardenScene.Fall) == "Fall" && SeasonScene.Label(GardenScene.Winter) == "Winter"
                && SeasonScene.WipeMode(GardenScene.Spring) == SeasonScene.WipeFromBottom
                && SeasonScene.WipeMode(GardenScene.Summer) == SeasonScene.WipeFromHorizon
                && SeasonScene.WipeMode(GardenScene.Fall) == SeasonScene.WipeFromSide
                && SeasonScene.WipeMode(GardenScene.Winter) == SeasonScene.WipeFromTop,
                "spring bottom, summer horizon, fall side, winter top");
            bool noWelcome = SeasonReveal.Banner(GardenScene.Spring) == "Spring"
                && SeasonReveal.Banner(GardenScene.Winter) == "Winter"
                && SeasonReveal.Banner(GardenScene.Desert) == "Welcome to the Desert";
            Check("no-welcome-banner-for-seasons", noWelcome, "seasons show a one-word label; desert unchanged");
        }
    }
}
#endif
