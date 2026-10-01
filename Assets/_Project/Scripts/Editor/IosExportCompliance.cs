#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

// Also adds the tracking-prompt text and links AppTrackingTransparency.
// Declares no non-exempt encryption (only standard HTTPS), so TestFlight
// builds skip the export-compliance question and go straight to testers.
public static class IosExportCompliance
{
    [PostProcessBuild(900)]
    public static void OnPostprocessBuild(BuildTarget target, string path)
    {
        if (target != BuildTarget.iOS) return;
        var plistPath = Path.Combine(path, "Info.plist");
        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);
        plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
        // App Tracking Transparency prompt text (asked once after the first garden).
        plist.root.SetString("NSUserTrackingUsageDescription",
            "Allowing tracking helps show you ads that fit you better and keeps Flock Five free.");
        // Runs after the notifications postprocessor (order 1). That step copies
        // Request Authorization on App Launch into this key; the daily reminder
        // asks only after a claim, from DailyReminder.Accept.
        plist.root.SetBoolean("UnityNotificationRequestAuthorizationOnAppLaunch", false);
        plist.root.SetBoolean("UnityNotificationRequestAuthorizationForRemoteNotificationsOnAppLaunch", false);
        plist.WriteToFile(plistPath);

        var projPath = PBXProject.GetPBXProjectPath(path);
        var proj = new PBXProject();
        proj.ReadFromFile(projPath);
        proj.AddFrameworkToProject(proj.GetUnityFrameworkTargetGuid(), "AppTrackingTransparency.framework", true);
        proj.WriteToFile(projPath);
    }
}
#endif
