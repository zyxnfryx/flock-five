#if UNITY_IOS
using System.Collections.Generic;
using System.IO;
using FlockFive.Editor;
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
        // App Tracking Transparency prompt text. AdConsent asks before LevelPlay init.
        plist.root.SetString("NSUserTrackingUsageDescription",
            "Allowing tracking helps show you ads that fit you better and keeps Flock Five free.");
        // Runs after the notifications postprocessor (order 1). That step copies
        // Request Authorization on App Launch into this key; the daily reminder
        // asks only after a claim, from DailyReminder.Accept.
        plist.root.SetBoolean("UnityNotificationRequestAuthorizationOnAppLaunch", false);
        plist.root.SetBoolean("UnityNotificationRequestAuthorizationForRemoteNotificationsOnAppLaunch", false);
        AddSkAdNetworkIds(plist);
        plist.WriteToFile(plistPath);

        var projPath = PBXProject.GetPBXProjectPath(path);
        var proj = new PBXProject();
        proj.ReadFromFile(projPath);
        proj.AddFrameworkToProject(proj.GetUnityFrameworkTargetGuid(), "AppTrackingTransparency.framework", true);
        proj.WriteToFile(projPath);
    }

    // IronSource plus Unity Ads is the previous 82-id set. AppLovin and Meta
    // lists are in the same folder. LevelPlay's own post-process also merges
    // the installed adapters and skips ids that are already present.
    static void AddSkAdNetworkIds(PlistDocument plist)
    {
        var ids = SkAdNetworkIds.LoadMerged();
        if (ids.Count == 0) return;
        PlistElementArray array = null;
        if (plist.root.values != null && plist.root.values.ContainsKey("SKAdNetworkItems"))
            array = plist.root["SKAdNetworkItems"].AsArray();
        if (array == null)
        {
            if (plist.root.values != null && plist.root.values.ContainsKey("SKAdNetworkItems"))
                plist.root.values.Remove("SKAdNetworkItems");
            array = plist.root.CreateArray("SKAdNetworkItems");
        }
        var have = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        if (array.values != null)
        foreach (var el in array.values)
        {
            var dict = el.AsDict();
            if (dict == null || dict.values == null) continue;
            PlistElement value;
            if (!dict.values.TryGetValue("SKAdNetworkIdentifier", out value) || value == null) continue;
            string id = value.AsString();
            if (!string.IsNullOrEmpty(id)) have.Add(id);
        }
        for (int i = 0; i < ids.Count; i++)
        {
            if (!have.Add(ids[i])) continue;
            array.AddDict().SetString("SKAdNetworkIdentifier", ids[i]);
        }
    }
}
#endif
