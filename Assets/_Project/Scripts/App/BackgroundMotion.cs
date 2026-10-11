using UnityEngine;

namespace FlockFive
{
    // The Settings "Animate background" switch. Missing key defaults ON, and
    // OFF when iOS Reduce Motion is on and the player has not chosen yet.
    // An explicit PlayerPrefs value wins over Reduce Motion. Birds, limbs,
    // and bees do not read this.
    public static class BackgroundMotion
    {
        public const string PrefKey = "flockfive.bg.anim";
        public const string Label = "Animate background";

        // Filled by Probe on iOS. Tests set it directly.
        public static bool ReduceMotion;

        public static bool On
        {
            get
            {
                if (PlayerPrefs.HasKey(PrefKey))
                    return PlayerPrefs.GetInt(PrefKey, 1) != 0;
                return !ReduceMotion;
            }
            set
            {
                PlayerPrefs.SetInt(PrefKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static void Probe()
        {
#if UNITY_IOS && !UNITY_EDITOR
            try { ReduceMotion = FlockFive_ReduceMotion() != 0; }
            catch (System.Exception e)
            {
                Debug.LogWarning("Reduce Motion probe skipped: " + e.Message);
            }
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        static extern int FlockFive_ReduceMotion();
#endif
    }
}
