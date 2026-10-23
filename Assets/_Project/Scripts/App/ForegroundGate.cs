using UnityEngine;

namespace FlockFive
{
    // OS foreground. OnApplicationPause and OnApplicationFocus both bookend a suspend
    // (they do not always arrive as a pair), so each one is tracked on its own.
    // Coin blips and the welcome award read this. GamePause is a different freeze.
    public static class ForegroundGate
    {
        public static bool Focused { get; private set; } = true;
        public static bool Paused { get; private set; }

        // False while the phone is away, or while the studio card still covers the home.
        public static bool AllowsCoinAudio => Focused && !Paused && !FlockFiveApp.StudioCardOwnsScreen;

        public static void NotePause(bool paused) => Paused = paused;

        public static void NoteFocus(bool focus) => Focused = focus;

        public static void ResetForTest()
        {
            Focused = true;
            Paused = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnLoad() => ResetForTest();
    }
}
