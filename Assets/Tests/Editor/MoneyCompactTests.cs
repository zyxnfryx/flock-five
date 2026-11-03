#if UNITY_EDITOR
using UnityEngine;
namespace FlockFive.Editor
{
    // b120: BET label compact notation (no "$1,000" running into +).
    static class MoneyCompactTests
    {
        public static void Run()
        {
            var c = new (long, string)[] { (999, "$999"), (1000, "$1K"), (2500, "$2.5K"), (10000, "$10K"), (1000000, "$1M"), (1250000, "$1.2M") };
            int f = 0;
            foreach (var (n, w) in c) { var g = Money.Compact(n); if (g != w) { f++; Debug.LogError("[money-compact] FAIL " + n + " want " + w + " got " + g); } }
            Debug.Log("[money-compact] " + (f == 0 ? "PASS 6/6" : "FAIL " + f));
        }
    }
}
#endif
