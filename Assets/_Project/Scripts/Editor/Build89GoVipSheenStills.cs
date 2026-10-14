#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 89: Go VIP buy-disc sheen at the trace start and mid-sweep, per device.
    // Launch with -executeMethod on this worktree; the arm lives in this editor's SessionState.
    public static class Build89GoVipSheenStills
    {
        public static void CapturePass1() => Capture("pass1");
        public static void CapturePass2() => Capture("pass2");
        public static void CapturePass2A() => Capture("pass2a");
        public static void CapturePass2B() => Capture("pass2b");
        public static void Capture() => Capture(System.Environment.GetEnvironmentVariable("FF_GOVIP_TAG") ?? "after");

        static void Capture(string tag)
        {
            // Armed for this editor session only, so a queue runner without the env still works.
            SessionState.SetString(FlockFive.FlockFiveApp.GoVipSheenArmKey, tag);
            AudioListener.volume = 0f;
            AudioListener.pause = true;
            Build79Stills.SetGameView(1179, 2556);
            EditorApplication.isPlaying = true;
        }
    }
}
#endif
