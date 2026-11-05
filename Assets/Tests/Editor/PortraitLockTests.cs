#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // b120 item 7 (+ Readability R1): Flock Five is portrait only. A rotated phone or iPad must never get the
    // landscape layout (the portrait column shrunk to ~226px). Guards the Player settings and the iOS plist step.
    static class PortraitLockTests
    {
        public static void Run()
        {
            int fail = 0, n = 0;
            void Check(bool ok, string what) { n++; if (!ok) { fail++; Debug.LogError("[portrait-lock] FAIL " + what); } }
            Check(PlayerSettings.defaultInterfaceOrientation == UIOrientation.Portrait, "defaultScreenOrientation " + PlayerSettings.defaultInterfaceOrientation);
            Check(PlayerSettings.allowedAutorotateToPortrait, "allowedAutorotateToPortrait off");
            Check(!PlayerSettings.allowedAutorotateToPortraitUpsideDown, "allowedAutorotateToPortraitUpsideDown on");
            Check(!PlayerSettings.allowedAutorotateToLandscapeLeft, "allowedAutorotateToLandscapeLeft on");
            Check(!PlayerSettings.allowedAutorotateToLandscapeRight, "allowedAutorotateToLandscapeRight on");
            // iPad: the Xcode post-process (IosExportCompliance) writes UIRequiresFullScreen and a portrait-only
            // UISupportedInterfaceOrientations~ipad, so iPad multitasking can never rotate or resize the app.
            const string post = "Assets/_Project/Scripts/Editor/IosExportCompliance.cs";
            string src = File.Exists(post) ? File.ReadAllText(post) : "";
            Check(src.Contains("SetBoolean(\"UIRequiresFullScreen\", true)"), "UIRequiresFullScreen not set in " + post);
            Check(src.Contains("UISupportedInterfaceOrientations~ipad") && src.Contains("UIInterfaceOrientationPortrait\")")
                && !src.Contains("UIInterfaceOrientationLandscape"), "iPad orientations not portrait-only in " + post);
            Debug.Log("[portrait-lock] " + (fail == 0 ? "PASS " + n + "/" + n : "FAIL " + fail + "/" + n));
        }
    }
}
#endif
