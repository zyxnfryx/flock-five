#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // The garden multiplier chip stays up through every lesson. Splash, an ad,
    // and a full-screen popup still hide it. The finale smash does not.
    [InitializeOnLoad]
    static class GardenStampVisTests
    {
        static GardenStampVisTests()
        {
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            const string cmd = "/tmp/flock-five-stamp-tests";
            if (!System.IO.File.Exists(cmd) || EditorApplication.isCompiling) return;
            try { System.IO.File.Delete(cmd); }
            catch { return; }
            Run();
        }

        [MenuItem("Flock Five/Garden Stamp Tests")]
        public static void Run()
        {
            int pass = 0;
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++;
                else fail++;
                Debug.Log("[stamp] " + (ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            GardenStampQuery Open()
            {
                return new GardenStampQuery { ChipArmed = true };
            }

            var values = (GardenLesson[])Enum.GetValues(typeof(GardenLesson));
            int bits = 0;
            for (int i = 0; i < values.Length; i++)
            {
                var bit = values[i];
                if (bit == GardenLesson.None) continue;
                bits++;
                var q = Open();
                q.Lesson = bit;
                Check(bit + " keeps the chip", GardenStampVis.Shown(q), "lesson on, garden open");
            }

            var all = Open();
            all.Lesson = (GardenLesson)~0;
            Check("every lesson together", GardenStampVis.Shown(all) && bits >= 20, "bits=" + bits);

            var smash = Open();
            smash.FinaleSmash = true;
            smash.Lesson = GardenLesson.Sparrow | GardenLesson.CoachDim;
            Check("finale smash keeps the chip", GardenStampVis.Shown(smash), "world logo, HUD stays");

            var splash = Open();
            splash.Splash = true;
            splash.Lesson = GardenLesson.BadgerGuide | GardenLesson.Sparrow;
            Check("splash hides", !GardenStampVis.Shown(splash), "home and badger page");

            var ad = Open();
            ad.AdCover = true;
            ad.Lesson = GardenLesson.TutorPause | GardenLesson.Sparrow;
            Check("ad hides", !GardenStampVis.Shown(ad), "ad covers the glass");

            var popup = Open();
            popup.FullPopup = true;
            popup.Lesson = GardenLesson.AdHand | GardenLesson.CoachDim;
            Check("full popup hides", !GardenStampVis.Shown(popup), "gift, restart, inspect, clear");

            var bare = new GardenStampQuery();
            bare.Lesson = GardenLesson.Hawk | GardenLesson.Leaf | GardenLesson.Bee;
            Check("no streak hides", !GardenStampVis.Shown(bare), "chip not armed");

            Debug.Log(fail == 0
                ? "stamp tests DONE " + pass + "/" + (pass + fail)
                : "stamp tests FAIL " + fail + " failed, " + pass + " passed");
        }
    }
}
#endif
