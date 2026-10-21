#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Oct 2026 (Brandon): one shared wind for the season gardens.
    //   - SeasonMotion.Wind ramps up WindLead seconds BEFORE the rain (GardenStorm.GustTarget reads ahead)
    //   - the wind drives foliage sway, falling leaves, tumbling ground leaves and the rain slant
    //   - falling leaves are no longer dimmed by rain; they are born in the close canopy overhead
    //   - ground leaves: 16 (8 low-power) in every season garden, seasonal tint; winter drifts snow
    //   - every season garden is a close giant-sequoia painting with its own canopy sway mask
    //   - winter keeps no thunder / lightning / rain (SeasonSceneTests.winter-no-thunder)
    public static class SeasonWindTests
    {
        const string Out = "/tmp/flock-five-season-wind-tests.txt";

        [MenuItem("Flock Five/Season Wind Tests")]
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail)
            {
                string line = (ok ? "PASS  " : "FAIL  ") + name + "  " + detail;
                if (ok) pass++; else fail++;
                sb.AppendLine(line);
                Debug.Log("[season-wind] " + line);
            }
            bool keepOn = BackgroundMotion.On;
            var keepScene = SeasonMotion.Scene;
            float keepGust = SeasonMotion.StormGust;
            try
            {
                Wind(Check);
                Ground(Check);
                Art(Check);
                Wiring(Check);
            }
            finally
            {
                BackgroundMotion.On = keepOn;
                SeasonMotion.Scene = keepScene;
                SeasonMotion.StormGust = keepGust;
                SeasonMotion.SetWindForTest(0f);
            }
            string end = fail == 0 ? "SEASON_WIND_OK " + pass : "SEASON_WIND_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[season-wind] " + end);
            if (fail > 0) throw new Exception(end);
        }

        static void Wind(Action<string, bool, string> Check)
        {
            float first = GardenStorm.FirstWait;
            float lead = SeasonMotion.WindLead;
            Check("wind-lead-2-5s", lead >= 2f && lead <= 5f, "lead " + lead);
            // Before the storm window the gust already wants to blow; rain does not.
            float early = first - lead + 0.25f;
            Check("gust-before-rain", GardenStorm.GustTarget(early, false, false) > 0.99f
                && GardenStorm.GustTarget(first - lead - 0.5f, false, false) < 0.01f, "t=" + early);
            Check("gust-off-when-halted", GardenStorm.GustTarget(first + 5f, false, true) == 0f, "");
            Check("gust-clear-sky-start", GardenStorm.GustTarget(0f, false, false) == 0f, "clear StartDelay keeps the garden calm");

            bool breezeOk = true;
            for (float t = 0f; t < 60f; t += 0.37f)
            {
                float b = SeasonMotion.Breeze(t);
                if (b < SeasonMotion.BreezeMin - 1e-4f || b > SeasonMotion.BreezeMax + 1e-4f) breezeOk = false;
            }
            Check("breeze-in-band", breezeOk, SeasonMotion.BreezeMin + ".." + SeasonMotion.BreezeMax);
            Check("wind-at", Mathf.Approximately(SeasonMotion.WindAt(0.2f, 0f), 0.2f)
                && Mathf.Approximately(SeasonMotion.WindAt(0.2f, 1f), 1f)
                && SeasonMotion.WindAt(0.2f, 0.5f) > 0.2f && SeasonMotion.WindAt(0.2f, 0.5f) < 1f, "breeze under, full gust = 1");

            BackgroundMotion.On = true;
            SeasonMotion.Scene = GardenScene.Fall;
            SeasonMotion.StormGust = 1f;
            SeasonMotion.TickWind(3f);
            float fall = SeasonMotion.Wind;
            SeasonMotion.Scene = GardenScene.Winter;
            SeasonMotion.TickWind(3f);
            float winter = SeasonMotion.Wind;
            Check("fall-full-gust", fall > 0.99f, "wind " + fall);
            Check("winter-soft-drift", winter < 0.8f && winter > SeasonMotion.BreezeMin, "wind " + winter);
            BackgroundMotion.On = false;
            SeasonMotion.TickWind(3f);
            Check("still-painting-no-wind", SeasonMotion.Wind == 0f, "");
            BackgroundMotion.On = true;
            SeasonMotion.Scene = GardenScene.Fall;
            SeasonMotion.SetWindForTest(1f);
            float windy = SeasonMotion.RainLeanWindy();
            SeasonMotion.SetWindForTest(0f);
            float calm = SeasonMotion.RainLeanWindy();
            Check("rain-slant-follows-wind", windy > calm * 1.4f && Mathf.Approximately(calm, SeasonMotion.RainLean()), calm + " -> " + windy);
            Check("winter-still-no-thunder", !GardenStorm.ThunderAllowed(GardenScene.Winter) && GardenStorm.UsesSnow(GardenScene.Winter), "");
        }

        static void Ground(Action<string, bool, string> Check)
        {
            var seasons = new[] { GardenScene.Summer, GardenScene.Fall, GardenScene.Winter, GardenScene.Spring };
            bool counts = true;
            string why = "";
            foreach (var sc in seasons)
            {
                var hi = SeasonMotion.Lively(sc, false);
                var lo = SeasonMotion.Lively(sc, true);
                if (hi.Ground != 16 || lo.Ground != 8 || hi.Streak < 2 || hi.Streak > 4) { counts = false; why = sc + " " + hi.Ground + "/" + lo.Ground; }
            }
            Check("ground-leaves-16-8", counts, why == "" ? "every season" : why);
            Check("desert-no-ground-leaves", SeasonMotion.Lively(GardenScene.Desert, false).Ground == 0, "");
            var a = GardenLife.GroundTint(GardenScene.Fall, 0);
            var b = GardenLife.GroundTint(GardenScene.Fall, 1);
            var c = GardenLife.GroundTint(GardenScene.Fall, 2);
            Check("fall-three-colours", !a.Equals(b) && !b.Equals(c) && !a.Equals(c) && a.r > a.b && b.r > b.b, "rust, amber, ochre");
            var w = GardenLife.GroundTint(GardenScene.Winter, 1);
            Check("winter-drifts-snow", w.r > 220 && w.g > 220 && w.b > 220, "white snow puffs");
            Check("lift-0.3-0.8", Mathf.Approximately(GardenLife.LiftMin, 0.3f) && Mathf.Approximately(GardenLife.LiftMax, 0.8f), "");
            Check("ground-band-lower-third", GardenLife.GroundBandLo >= 0f && GardenLife.GroundBandHi <= 0.36f, "");
            Check("leaves-not-dimmed-by-rain", GardenLife.LeafRainFloor >= 1f, "was ~15%");
            Check("canopy-spawn", GardenLife.CanopyDepth > 0.5f && GardenLife.CanopyDepth < 3f, "born under the top edge");
        }

        static void Art(Action<string, bool, string> Check)
        {
            foreach (var name in FoliageSway.SequoiaArt)
            {
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Resources/Sprites/" + name + ".png");
                bool size = sp != null && sp.texture != null && sp.texture.width == 1080 && sp.texture.height == 1920;
                var mask = Resources.Load<Texture2D>(FoliageSway.MaskFor(name, GardenScene.Fall));
                Check("sequoia-" + name, size && mask != null, (size ? "1080x1920" : "bad sprite") + (mask != null ? " + mask" : " no mask"));
            }
            Check("home-keeps-garden-wash", !FoliageSway.IsSequoia("bg_garden") && FoliageSway.MaskFor("bg_garden", GardenScene.Summer) == FoliageSway.MaskPath(GardenScene.Summer), "");
            // Trunk slivers: the outer birds' columns (8%..92% of width) are not dark trunk bark at mid height.
            foreach (var name in FoliageSway.SequoiaArt)
            {
                var path = "Assets/_Project/Art/Resources/Sprites/" + name + ".png";
                var bytes = File.Exists(path) ? File.ReadAllBytes(path) : null;
                if (bytes == null) { Check("sliver-" + name, false, "missing"); continue; }
                var tex = new Texture2D(2, 2);
                tex.LoadImage(bytes);
                int y = tex.height / 2;
                float edge = Lum(tex.GetPixel((int)(tex.width * 0.03f), y));
                float inL = Lum(tex.GetPixel((int)(tex.width * 0.10f), y));
                float inR = Lum(tex.GetPixel((int)(tex.width * 0.90f), y));
                UnityEngine.Object.DestroyImmediate(tex);
                Check("sliver-" + name, inL > edge && inR > edge, "edge " + edge.ToString("0.00") + " in " + inL.ToString("0.00") + "/" + inR.ToString("0.00"));
            }
        }

        static float Lum(Color c) => 0.3f * c.r + 0.59f * c.g + 0.11f * c.b;

        static void Wiring(Action<string, bool, string> Check)
        {
            string app = "Assets/_Project/Scripts/App/";
            string sway = File.ReadAllText(app + "FoliageSway.cs");
            string life = File.ReadAllText(app + "GardenLife.cs");
            string storm = File.ReadAllText(app + "GardenStorm.cs");
            Check("sway-reads-wind", sway.Contains("SeasonMotion.Wind * WindSway"), "");
            Check("life-ticks-wind", life.Contains("SeasonMotion.TickWind(t)") && life.Contains("SeasonMotion.WindDir"), "");
            Check("rain-slant-wind", storm.Contains("SeasonMotion.RainLeanWindy()") && storm.Contains("SeasonMotion.StormGust = _gust"), "");
            Check("rain-no-leaf-dim", life.Contains("Mathf.Max(wet, LeafRainFloor)"), "");
        }
    }
}
#endif
