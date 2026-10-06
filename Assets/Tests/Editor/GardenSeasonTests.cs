#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Garden backdrop schedule: fixed seasons, the 40+ rotation, and Brandon's rule that
    // no backdrop change ever lands on a honey badger garden. Season art files present.
    // Run from the menu, or drop a file on /tmp/flock-five-season-tests.
    [InitializeOnLoad]
    static class GardenSeasonTests
    {
        const string Cmd = "/tmp/flock-five-season-tests";
        const string Out = "/tmp/flock-five-season-tests.txt";
        const int Horizon = 220;

        static GardenSeasonTests()
        {
            EditorApplication.delayCall += Maybe;
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (File.Exists(Cmd)) Maybe();
        }

        static void Maybe()
        {
            if (!File.Exists(Cmd)) return;
            if (EditorApplication.isCompiling) return;
            try { File.Delete(Cmd); }
            catch { return; }
            Run();
        }

        [MenuItem("Flock Five/Garden Season Tests")]
        public static void Run()
        {
            var sb = new StringBuilder();
            int pass = 0;
            int fail = 0;
            void Line(string t)
            {
                sb.AppendLine(t);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[garden-season] " + t);
            }
            void Check(string name, bool ok, string detail)
            {
                if (ok) { pass++; Line("PASS  " + name + "  " + detail); }
                else { fail++; Line("FAIL  " + name + "  " + detail); }
            }

            int clash = GardenSeason.FirstBadgerClash(Horizon);
            Check("no-badger-clash", clash == 0, clash == 0 ? "through " + Horizon : "change on badger garden " + clash);

            var list = GardenSeason.ChangeLevels(Horizon);
            int bad = 0;
            var listText = new StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                if (GardenSeason.IsBadgerLevel(list[i])) bad++;
                if (i < 24) listText.Append(list[i]).Append(' ').Append(GardenSeason.ForLevel(list[i])).Append(", ");
            }
            Check("change-levels-clean", bad == 0, list.Count + " changes, " + bad + " on badger gardens");
            Line("      " + listText);

            // Every garden where the scene differs from the one before is in the list, and
            // nothing else is, so the list and ForLevel cannot drift apart.
            int drift = 0;
            for (int n = 2; n <= Horizon; n++)
            {
                bool listed = list.Contains(n);
                if (listed != GardenSeason.IsChangeLevel(n)) drift++;
                if (GardenSeason.IsChangeLevel(n) && GardenSeason.IsBadgerLevel(n)) drift++;
            }
            Check("list-matches-forlevel", drift == 0, drift + " mismatches");

            bool badger = GardenSeason.IsBadgerLevel(16) && GardenSeason.IsBadgerLevel(21)
                && GardenSeason.IsBadgerLevel(31) && GardenSeason.IsBadgerLevel(36)
                && !GardenSeason.IsBadgerLevel(15) && !GardenSeason.IsBadgerLevel(25)
                && !GardenSeason.IsBadgerLevel(32) && !GardenSeason.IsBadgerLevel(11);
            Check("badger-gardens", badger, "16,21,31,36 yes; 11,15,25,32 no");

            bool fixedOk = GardenSeason.ForLevel(1) == GardenScene.Summer
                && GardenSeason.ForLevel(14) == GardenScene.Summer
                && GardenSeason.ForLevel(15) == GardenScene.Desert
                && GardenSeason.ForLevel(24) == GardenScene.Desert
                && GardenSeason.ForLevel(25) == GardenScene.Winter
                && GardenSeason.ForLevel(31) == GardenScene.Winter
                && GardenSeason.ForLevel(32) == GardenScene.Spring
                && GardenSeason.ForLevel(39) == GardenScene.Spring
                && GardenSeason.ForLevel(40) == GardenScene.Summer
                && GardenSeason.ForLevel(48) == GardenScene.Fall
                && GardenSeason.ForLevel(56) == GardenScene.Fall
                && GardenSeason.ForLevel(57) == GardenScene.Winter;
            Check("fixed-seasons", fixedOk, "1-14 summer, 15-24 desert, 25-31 winter, 32-39 spring, 40 summer, 48 fall, 57 winter");

            int gapBad = 0;
            for (int i = 4; i < list.Count; i++)
            {
                int gap = list[i] - list[i - 1];
                if (gap < GardenSeason.RotateEvery || gap > GardenSeason.RotateEvery + 2) gapBad++;
            }
            Check("rotation-gaps", gapBad == 0, "every 8 (+1 off a badger garden)");

            string[] art = { "bg_fall", "bg_winter", "bg_spring" };
            for (int i = 0; i < art.Length; i++)
            {
                var path = "Assets/_Project/Art/Resources/Sprites/" + art[i] + ".png";
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                bool ok = sp != null && sp.texture != null && sp.texture.width == 1080 && sp.texture.height == 1920;
                Check("art-" + art[i], ok, ok ? "1080x1920 sprite" : "missing or wrong size at " + path);
            }

            Line(fail == 0 ? "ALL OK  " + pass : "FAILED  " + fail + "  passed " + pass);
        }
    }
}
#endif
