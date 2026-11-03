#if UNITY_EDITOR
using UnityEngine;
namespace FlockFive.Editor
{
    // b120: a future last-ad stamp (clock skew) must not block ads forever.
    static class AdClockSkewTests
    {
        public static void Run()
        {
            bool a = !Ads.Cooling(1000, 5000);
            bool b = Ads.Cooling(1000, 990);
            bool c = !Ads.Cooling(1000, 900);
            if (a && b && c) Debug.Log("[ad-clock-skew] PASS 3/3");
            else Debug.LogError("[ad-clock-skew] FAIL future=" + a + " recent=" + b + " old=" + c);
        }
    }
}
#endif
