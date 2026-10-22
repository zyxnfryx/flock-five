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

            int clash = GardenSeason.FirstOstrichClash(Horizon);
            Check("no-ostrich-clash", clash == 0, clash == 0 ? "through " + Horizon : "change on ostrich garden " + clash);

            var list = GardenSeason.ChangeLevels(Horizon);
            int bad = 0;
            var listText = new StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                if (GardenSeason.IsOstrichLevel(list[i])) bad++;
                if (i < 24) listText.Append(list[i]).Append(' ').Append(GardenSeason.ForLevel(list[i])).Append(", ");
            }
            Check("change-levels-clean", bad == 0, list.Count + " changes, " + bad + " on ostrich gardens");
            Line("      " + listText);

            // Every garden where the scene differs from the one before is in the list, and
            // nothing else is, so the list and ForLevel cannot drift apart.
            int drift = 0;
            for (int n = 2; n <= Horizon; n++)
            {
                bool listed = list.Contains(n);
                if (listed != GardenSeason.IsChangeLevel(n)) drift++;
                if (GardenSeason.IsChangeLevel(n) && GardenSeason.IsOstrichLevel(n)) drift++;
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
                && GardenSeason.ForLevel(30) == GardenScene.Winter
                && GardenSeason.ForLevel(31) == GardenScene.Spring
                && GardenSeason.ForLevel(32) == GardenScene.Spring
                && GardenSeason.ForLevel(39) == GardenScene.Spring
                && GardenSeason.ForLevel(40) == GardenScene.Summer
                && GardenSeason.ForLevel(48) == GardenScene.Fall
                && GardenSeason.ForLevel(55) == GardenScene.Fall
                && GardenSeason.ForLevel(56) == GardenScene.Winter;
            Check("fixed-seasons", fixedOk, "1-14 summer, 15-24 desert, 25-30 winter, 31-39 spring, 40 summer, 48 fall, 56 winter");

            int gapBad = 0;
            for (int i = 3; i < list.Count; i++)
            {
                int expect = 40 + 8 * (i - 3);
                if (list[i] != expect && !(list[i] > expect && GardenSeason.IsOstrichLevel(list[i] - 1))) gapBad++;
            }
            Check("rotation-gaps", gapBad == 0, "fixed grid 40+8k, +1 only off an ostrich garden");
            Check("rotation-explicit", list.Contains(40) && list.Contains(48) && list.Contains(56) && list.Contains(64)
                && list.Contains(72) && list.Contains(80) && list.Contains(88) && list.Contains(96),
                "40 48 56 64 72 80 88 96 (badger levels 56 and 96 stay)");

            bool ostrichClean = !GardenSeason.IsChangeLevel(27) && !GardenSeason.IsChangeLevel(38) && GardenSeason.IsOstrichLevel(27)
                && GardenSeason.IsOstrichLevel(38) && !GardenSeason.IsOstrichLevel(31) && !GardenSeason.IsOstrichLevel(32);
            for (int n = 1; n <= LevelData.Count; n++)
                if (LevelData.Peek(n - 1).Ostrich && GardenSeason.IsChangeLevel(n)) ostrichClean = false;
            Check("no-season-change-on-ostrich", ostrichClean, "no ostrich garden in 1-" + LevelData.Count + " is a season change");

            bool due = SeasonReveal.Due(15, false) && !SeasonReveal.Due(16, false) && !SeasonReveal.Due(14, false)
                && !SeasonReveal.Due(15, true) && !SeasonReveal.Due(32, false)
                && SeasonReveal.Due(25, false) && SeasonReveal.Due(31, false) && SeasonReveal.Due(40, false);
            Check("reveal-due", due, "15, 25, 31, 40 once; 14, 16, 32 and a seen 15 never");

            float desertFade = SeasonReveal.FadeOf(GardenScene.Desert);
            float otherFade = SeasonReveal.FadeOf(GardenScene.Winter);
            bool timing = desertFade > otherFade
                && desertFade >= 2.2f && desertFade <= 3.2f
                && otherFade == SeasonScene.WipeEnd
                && SeasonReveal.FadeOf(GardenScene.Summer) == otherFade
                && SeasonReveal.BannerOf(GardenScene.Desert) >= 2f
                && SeasonReveal.BannerOf(GardenScene.Spring) == SeasonReveal.BannerSeconds
                && SeasonReveal.BannerOf(GardenScene.Spring) <= SeasonScene.Duration - SeasonScene.SettleStart + 0.001f;
            Check("reveal-timing", timing, "desert wipe " + desertFade + "s, seasons " + otherFade + "s wipe inside the 2.5s scene");

            bool words = SeasonReveal.Banner(GardenScene.Desert) == "Welcome to the Desert"
                && SeasonReveal.Banner(GardenScene.Winter) == "Winter"
                && SeasonReveal.Banner(GardenScene.Spring) == "Spring"
                && SeasonReveal.Banner(GardenScene.Summer) == "Summer"
                && SeasonReveal.Banner(GardenScene.Fall) == "Fall";
            Check("reveal-banners", words, "Welcome to the Desert; seasons are a one-word label");

            bool blocks = SeasonReveal.BlocksIntros(GardenScene.Desert, true)
                && !SeasonReveal.BlocksIntros(GardenScene.Desert, false)
                && !SeasonReveal.BlocksIntros(GardenScene.Winter, true)
                && !SeasonReveal.BlocksIntros(GardenScene.Summer, true);
            Check("reveal-blocks", blocks, "desert holds intros only while its reveal is playing");

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
