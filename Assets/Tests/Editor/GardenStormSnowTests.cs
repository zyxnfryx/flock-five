#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // b110: winter shows gentle snow, every other garden keeps the rain storm.
    // Run from the menu, or drop a file on /tmp/flock-five-snow-tests.
    [InitializeOnLoad]
    static class GardenStormSnowTests
    {
        const string Cmd = "/tmp/flock-five-snow-tests";
        const string Out = "/tmp/flock-five-snow-tests.txt";

        static GardenStormSnowTests()
        {
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (File.Exists(Cmd)) Maybe();
        }

        static void Maybe()
        {
            try { File.Delete(Cmd); } catch { }
            Run();
        }

        [MenuItem("Flock Five/Tests/Winter snow")]
        public static void Run()
        {
            var sb = new StringBuilder();
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (!ok) fail++;
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            bool onlyWinter = GardenStorm.UsesSnow(GardenScene.Winter)
                && !GardenStorm.UsesSnow(GardenScene.Summer) && !GardenStorm.UsesSnow(GardenScene.Desert)
                && !GardenStorm.UsesSnow(GardenScene.Fall) && !GardenStorm.UsesSnow(GardenScene.Spring);
            Check("only-winter-is-snow", onlyWinter, "");
            Check("snow-is-gentle", GardenStorm.SnowSpeedMax < 8.5f && GardenStorm.SnowSizeMax < 0.2f
                && GardenStorm.SnowCountMul < 1f && GardenStorm.SnowSpeedMin > 0f,
                "speed " + GardenStorm.SnowSpeedMin + ".." + GardenStorm.SnowSpeedMax);
            var root = new GameObject("snow-test").transform;
            try
            {
                var w = GardenStorm.Attach(root, GardenScene.Winter);
                var f = GardenStorm.Attach(root, GardenScene.Fall);
                Check("attach-marks-scene", w.IsSnow && !f.IsSnow, "");
            }
            finally { Object.DestroyImmediate(root.gameObject); }

            sb.AppendLine(fail == 0 ? "ALL PASS" : fail + " FAIL");
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[snow] " + (fail == 0 ? "ALL PASS" : fail + " FAIL"));
        }
    }
}
#endif
