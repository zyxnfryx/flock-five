using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Batch entry: Unity -batchmode -quit -buildTarget iOS -executeMethod IosBuild.Run
// Env: FF_BUILD_NUMBER (CFBundleVersion), FF_XCODE_PATH (export dir).
public static class IosBuild
{
    public static void Run()
    {
        var ios = NamedBuildTarget.iOS;
        PlayerSettings.SetApplicationIdentifier(ios, "com.zfxgames.flockfive");
        PlayerSettings.iOS.buildNumber = Env("FF_BUILD_NUMBER");
        PlayerSettings.iOS.appleDeveloperTeamID = "23K8T866FT";
        PlayerSettings.iOS.appleEnableAutomaticSigning = true;
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var r = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Env("FF_XCODE_PATH"),
            target = BuildTarget.iOS,
            options = BuildOptions.None
        });
        Debug.Log("IOSBUILD result=" + r.summary.result + " errors=" + r.summary.totalErrors);
        EditorApplication.Exit(r.summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    static string Env(string k)
    {
        var v = Environment.GetEnvironmentVariable(k);
        if (string.IsNullOrEmpty(v)) throw new Exception("missing " + k);
        return v;
    }
}
