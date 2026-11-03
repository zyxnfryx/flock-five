#if UNITY_EDITOR
using UnityEngine;
namespace FlockFive.Editor
{
    // b120: BET label compact notation (no "$1,000" running into +).
    static class MoneyCompactTests
    {
        public static void Run()
        {
            var c = new (long, string)[] { (0, "$0"), (999, "$999"), (1000, "$1K"), (1049, "$1K"), (1050, "$1.1K"), (2500, "$2.5K"), (9999, "$10K"), (10000, "$10K"), (99950, "$100K"), (999499, "$999K"), (999999, "$1M"), (1250000, "$1.3M"), (1000000000, "$1B"), (2500000000000, "$2.5T"), (999999999999999, "$999T+"), (long.MaxValue, "$999T+"), (-2500, "-$2.5K") };
            int f = 0;
            foreach (var (n, w) in c) { var g = Money.Compact(n); if (g != w) { f++; Debug.LogError("[money-compact] FAIL " + n + " want " + w + " got " + g); } }
            Debug.Log("[money-compact] " + (f == 0 ? "PASS " + c.Length + "/" + c.Length : "FAIL " + f));
        }
    }
}
#endif
