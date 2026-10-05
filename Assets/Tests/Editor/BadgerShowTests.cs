#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Honey badger Phase 4: leap timeline, opening landing order, sitter switch, art names.
    // Run from the menu, or drop a file on /tmp/flock-five-badger-show-tests.
    [InitializeOnLoad]
    static class BadgerShowTests
    {
        const string Cmd = "/tmp/flock-five-badger-show-tests";
        const string Out = "/tmp/flock-five-badger-show-tests.txt";
        const float Eps = 0.0005f;

        static BadgerShowTests()
        {
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (!File.Exists(Cmd) || EditorApplication.isCompiling) return;
            try { File.Delete(Cmd); }
            catch { return; }
            Run();
        }

        [MenuItem("Flock Five/Badger Show Tests")]
        public static void Run()
        {
            var sb = new StringBuilder();
            int pass = 0;
            int fail = 0;
            void Line(string t)
            {
                sb.AppendLine(t);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[badger-show] " + t);
            }
            void Check(string name, bool ok, string detail)
            {
                if (ok) { pass++; Line("PASS  " + name + "  " + detail); }
                else { fail++; Line("FAIL  " + name + "  " + detail); }
            }

            CheckLeapTimeline(Check);
            CheckLeapFrames(Check);
            CheckLeapWashAndSting(Check);
            CheckOpeningOrder(Check);
            CheckOpeningTiming(Check);
            CheckSitterSwitch(Check);
            CheckArt(Check);

            Line(fail == 0 ? "ALL OK  " + pass : "FAILED  " + fail + "  passed " + pass);
        }

        static bool Near(float a, float b) => Mathf.Abs(a - b) < Eps;

        static void CheckLeapTimeline(System.Action<string, bool, string> Check)
        {
            float sum = 0f;
            foreach (var b in BadgerLeap.Beats) sum += b;
            bool total = Near(sum, 3.0f) && Near(BadgerLeap.Duration, 3.0f) && BadgerLeap.Beats.Length == 4;
            bool starts = Near(BadgerLeap.BeatStart(BadgerLeapBeat.Hop), 0f)
                && Near(BadgerLeap.BeatStart(BadgerLeapBeat.Crouch), 0.8f)
                && Near(BadgerLeap.BeatStart(BadgerLeapBeat.Leap), 1.4f)
                && Near(BadgerLeap.BeatStart(BadgerLeapBeat.Wash), 2.6f)
                && Near(BadgerLeap.BeatStart(BadgerLeapBeat.Done), 3.0f);
            bool beats = BadgerLeap.BeatAt(0.1f) == BadgerLeapBeat.Hop
                && BadgerLeap.BeatAt(0.9f) == BadgerLeapBeat.Crouch
                && BadgerLeap.BeatAt(2.0f) == BadgerLeapBeat.Leap
                && BadgerLeap.BeatAt(2.8f) == BadgerLeapBeat.Wash
                && BadgerLeap.BeatAt(3.01f) == BadgerLeapBeat.Done;
            Check("leap timeline", total && starts && beats,
                "hop 0.8 + crouch 0.6 + leap 1.2 + wash 0.4 = " + sum.ToString("0.000") + " s");
        }

        static void CheckLeapFrames(System.Action<string, bool, string> Check)
        {
            bool hop = BadgerLeap.FrameAt(0.10f) == "badger_hop_a" && BadgerLeap.FrameAt(0.30f) == "badger_hop_b"
                && BadgerLeap.FrameAt(0.50f) == "badger_hop_a" && BadgerLeap.FrameAt(0.75f) == "badger_hop_b";
            bool crouch = BadgerLeap.FrameAt(1.0f) == "badger_crouch";
            bool leap = BadgerLeap.FrameAt(1.45f) == "badger_leap_0" && BadgerLeap.FrameAt(2.0f) == "badger_leap_1"
                && BadgerLeap.FrameAt(2.5f) == "badger_leap_3" && BadgerLeap.FrameAt(2.9f) == "badger_leap_3";
            bool grow = Near(BadgerLeap.Grow(1.0f), 0f) && Near(BadgerLeap.Grow(2.6f), 1f)
                && BadgerLeap.Grow(2.0f) > 0f && BadgerLeap.Grow(2.0f) < 1f && Near(BadgerLeap.Grow(2.9f), 1f);
            bool travel = Near(BadgerLeap.HopTravel(0f), 0f) && Near(BadgerLeap.HopTravel(0.8f), 1f)
                && Near(BadgerLeap.HopTravel(1.2f), 1f) && Near(BadgerLeap.HopLift(1.0f), 0f);
            Check("leap frames", hop && crouch && leap && grow && travel,
                "hop_a/hop_b alternate, crouch, leap_0 -> leap_1 -> leap_3, scale grows to full by 2.6 s");
        }

        static void CheckLeapWashAndSting(System.Action<string, bool, string> Check)
        {
            bool wash = Near(BadgerLeap.WashAlpha(2.5f), 0f) && Near(BadgerLeap.WashAlpha(2.6f), 0f)
                && BadgerLeap.WashAlpha(2.8f) > 0f && Near(BadgerLeap.WashAlpha(3.0f), 1f);
            bool sting = Near(BadgerLeap.TakeoffAt, 1.4f) && BadgerLeap.CrossesTakeoff(1.39f, 1.41f)
                && !BadgerLeap.CrossesTakeoff(1.41f, 1.5f) && !BadgerLeap.CrossesTakeoff(1.2f, 1.39f);
            bool stuck = !BadgerLeap.Stuck(true, 3.0f) && !BadgerLeap.Stuck(false, 99f)
                && BadgerLeap.Stuck(true, 3.0f + TutorialHeal.MaxGateSeconds + 0.1f);
            Check("leap wash + sting", wash && sting && stuck,
                "wash 0 -> 1 over 2.6-3.0 s; sting crosses once at 1.4 s; a dead leap heals after 3.0 + gate");
        }

        static void CheckOpeningOrder(System.Action<string, bool, string> Check)
        {
            var order = BadgerOpening.Order();
            bool count = BadgerOpening.Landings == 16 && order.Length == 16;
            bool grid = true;
            for (int k = 0; k < order.Length; k++)
            {
                if (order[k] != k) grid = false;
                if (BadgerOpening.OrderOf(BadgerOpening.TileAt(k)) != k) grid = false;
                if (k > 0 && !(BadgerOpening.LandAt(k) > BadgerOpening.LandAt(k - 1))) grid = false;
            }
            bool bees = BadgerOpening.BeeFrame(0) == "bee_1" && BadgerOpening.BeeFrame(4) == "bee_5"
                && BadgerOpening.BeeFrame(5) == "bee_1" && BadgerOpening.BeeFrame(15) == "bee_1";
            bool yard = BadgerOpening.BeeScale(true) < BadgerOpening.BeeScale(false);
            Check("opening order", count && grid && bees && yard,
                "16 landings, tile 0..15 in grid order, each later than the last; bee_1..bee_5 cycle; yard bee smaller");
        }

        static void CheckOpeningTiming(System.Action<string, bool, string> Check)
        {
            float d = BadgerOpening.Duration;
            bool about = d > 2.3f && d < 2.7f;
            bool stagger = Near(BadgerOpening.LandAt(1) - BadgerOpening.LandAt(0), 0.1f);
            bool counts = BadgerOpening.LandedCount(0f) == 0
                && BadgerOpening.LandedCount(BadgerOpening.LandAt(0)) == 1
                && BadgerOpening.LandedCount(d) == 16;
            bool flip = Near(BadgerOpening.TileLand(3, 0f), 0f) && Near(BadgerOpening.TileLand(3, BadgerOpening.LandAt(3)), 1f)
                && Near(BadgerOpening.TileLand(15, d), 1f);
            bool swipe = !BadgerOpening.HiveSwiped(0.1f) && BadgerOpening.HiveSwiped(0.3f)
                && BadgerOpening.SplatAlpha(0.3f) > 0f && Near(BadgerOpening.SplatAlpha(1.5f), 0f);
            bool fly = BadgerOpening.Flight(0, 0f) < 0f && BadgerOpening.Flight(0, BadgerOpening.LandAt(0)) > 1f;
            bool post = Near(BadgerOpening.PostSeconds, 0.6f);
            Check("opening timing", about && stagger && counts && flip && swipe && fly && post,
                "about " + d.ToString("0.00") + " s, 0.1 s stagger, landed frame is final, PostSeconds 0.6");
        }

        static void CheckSitterSwitch(System.Action<string, bool, string> Check)
        {
            bool keepOn = BadgerSchedule.Enabled;
            int keepFlag = BadgerSave.PendingFor(true);
            try
            {
                BadgerSave.Clear();
                BadgerSave.Set(15, true);
                BadgerSchedule.Enabled = false;
                bool off = !BadgerSitter.VisibleFor(false, false, false)
                    && !BadgerSitter.Visible(BadgerSave.Pending, false, false);
                BadgerSchedule.Enabled = true;
                bool on = BadgerSitter.VisibleFor(true, false, false)
                    && BadgerSitter.Visible(BadgerSave.Pending, false, false);
                bool leap = !BadgerSitter.VisibleFor(true, true, false);
                bool shot = !BadgerSitter.VisibleFor(true, false, true);
                BadgerSave.Clear();
                bool none = !BadgerSitter.VisibleFor(true, false, false);
                bool breathe = BadgerSitter.FrameAt(0.1f) == "badger_idle_a" && BadgerSitter.FrameAt(0.7f) == "badger_idle_b"
                    && BadgerSitter.FrameAt(1.3f) == "badger_idle_a";
                Check("sitter switch", off && on && leap && shot && none && breathe,
                    "hidden with Enabled false, with no flag, during the leap, and in editor shots; idle A/B at 0.6 s");
            }
            finally
            {
                BadgerSchedule.Enabled = keepOn;
                if (keepFlag > 0) BadgerSave.Set(keepFlag, true);
                else BadgerSave.Clear();
            }
        }

        static void CheckArt(System.Action<string, bool, string> Check)
        {
            string[] names =
            {
                "badger_hop_a", "badger_hop_b", "badger_crouch", "badger_leap_0", "badger_leap_1", "badger_leap_3",
                "badger_idle_a", "badger_idle_b", "badger_shrug", "hive_idle", "hive_swiped", "honey_splat",
                "bee_1", "bee_2", "bee_3", "bee_4", "bee_5"
            };
            var missing = new StringBuilder();
            foreach (var n in names)
                if (SpriteCatalog.BadgerArt(n) == null) missing.Append(n).Append(' ');
            Check("art", missing.Length == 0,
                missing.Length == 0 ? "all 17 frames load from Sprites/Badger/" : "missing: " + missing);
        }
    }
}
#endif
