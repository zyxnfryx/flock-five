#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // b112: compressed money (K/M/B) always keeps one decimal, so a count-up does not change width
    // ("1K" -> "1.1K" -> "1.2K"). Floors, so no label reads above the amount. Under 10,000 is unchanged.
    public static class Build112MoneyFormatTests
    {
        const string Out = "/tmp/flock-five-b112-money-tests.txt";

        [MenuItem("Flock Five/Build 112 Money Format Tests")]
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            int pass = 0, fail = 0;
            void Eq(long n, string want)
            {
                string got = Money.Format(n);
                bool ok = got == want;
                if (ok) pass++; else fail++;
                string line = (ok ? "PASS  " : "FAIL  ") + "format-" + n + "  want " + want + " got " + got;
                sb.AppendLine(line);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build112] " + line);
            }

            // small numbers: no suffix, no decimals
            Eq(0, "$0"); Eq(5, "$5"); Eq(999, "$999"); Eq(1000, "$1,000"); Eq(9999, "$9,999");
            // K keeps .0
            Eq(10000, "$10.0K"); Eq(10099, "$10.0K"); Eq(10100, "$10.1K"); Eq(11600, "$11.6K");
            Eq(100000, "$100.0K"); Eq(250000, "$250.0K"); Eq(999949, "$999.9K");
            // K -> M edge: floors, never 1000.0K, never reads above the amount
            Eq(999950, "$999.9K"); Eq(999999, "$999.9K"); Eq(1000000, "$1.0M");
            Eq(1049999, "$1.0M"); Eq(1100000, "$1.1M"); Eq(25000000, "$25.0M"); Eq(250000000, "$250.0M");
            Eq(999999999, "$999.9M"); Eq(1000000000, "$1.0B"); Eq(2500000000, "$2.5B");
            Eq(-12000, "-$12.0K");

            // roll-up: every value from 10K up through 12K keeps the same character count
            int width = Money.Format(10000).Length; int bad = 0;
            for (int n = 10000; n <= 12000; n += 7) if (Money.Format(n).Length != width) bad++;
            for (long n = 1000000; n <= 1200000; n += 997) if (Money.Format(n).Length != "$1.0M".Length) bad++;
            bool okw = bad == 0;
            if (okw) pass++; else fail++;
            sb.AppendLine((okw ? "PASS  " : "FAIL  ") + "rollup-stable-width  bad " + bad);

            sb.AppendLine("DONE pass=" + pass + " fail=" + fail);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[build112] DONE pass=" + pass + " fail=" + fail);
        }
    }
}
#endif
