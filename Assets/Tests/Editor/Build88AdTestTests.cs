#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // Test ads only for the allowlisted advertising IDs; everyone else gets normal ads.
    public static class Build88AdTestTests
    {
        const string Out = "/tmp/flock-five-b88-adtest-tests.txt";

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
                Debug.Log("[build88adtest] " + line);
            }

            Check("ios-1", AdTestDevices.IsTestDevice("AE170331-2D8E-46C9-B6E6-A72F4CE430BA"), "");
            Check("ios-2", AdTestDevices.IsTestDevice("54EEF4C4-C1A2-4792-A7EE-E5F695957D1A"), "");
            Check("ios-3", AdTestDevices.IsTestDevice("15314A07-CC91-455D-91C4-E73F125DC0BC"), "");
            Check("android", AdTestDevices.IsTestDevice("af3a2d10-4cb3-4099-920c-35f21f466305"), "");
            Check("case-insensitive", AdTestDevices.IsTestDevice("ae170331-2d8e-46c9-b6e6-a72f4ce430ba")
                && AdTestDevices.IsTestDevice("AF3A2D10-4CB3-4099-920C-35F21F466305"), "");
            Check("stranger", !AdTestDevices.IsTestDevice("11111111-2222-3333-4444-555555555555"), "");
            Check("zeroed-idfa", !AdTestDevices.IsTestDevice("00000000-0000-0000-0000-000000000000"), "");
            Check("empty", !AdTestDevices.IsTestDevice(null) && !AdTestDevices.IsTestDevice(""), "");
            Check("allowlist-size", AdTestDevices.Ios.Length == 3 && AdTestDevices.Android.Length == 1, "");
            Check("release-suite-off", !Ads.TestSuite, "TestSuite=" + Ads.TestSuite);

            string ads = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/Ads.cs"));
            Check("init-through-helper", ads.Contains("AdTestDevices.Resolve(") && ads.Contains("AdTestDevices.Apply("), "");

            Debug.Log("[build88adtest] " + pass + " pass, " + fail + " fail");
        }
    }
}
#endif
