using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
#if UNITY_ANDROID
using UnityEditor.Android;
#endif

// Batch entry: Unity -executeMethod AndroidBuild.Run
// Keystore path, alias, password, version code, and AAB path come from the environment.
public static class AndroidBuild
{
    public static void Run()
    {
        var android = NamedBuildTarget.Android;
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(android, BuildTarget.Android))
                throw new Exception("SwitchActiveBuildTarget Android failed");
        }

        PlayerSettings.SetApplicationIdentifier(android, "com.zfxgames.flockfive");
        PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        EditorUserBuildSettings.buildAppBundle = true;

        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = Env("FF_KEYSTORE");
        PlayerSettings.Android.keystorePass = Env("FF_KEYSTORE_PASS");
        PlayerSettings.Android.keyaliasName = Env("FF_KEY_ALIAS");
        PlayerSettings.Android.keyaliasPass = Env("FF_KEYSTORE_PASS");
        PlayerSettings.Android.bundleVersionCode = int.Parse(Env("FF_VERSION_CODE"));
        AssignAndroidIcons();

        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var opts = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Env("FF_AAB_PATH"),
            target = BuildTarget.Android,
            options = BuildOptions.None
        };
        var r = BuildPipeline.BuildPlayer(opts);
        Debug.Log("ANDROIDBUILD result=" + r.summary.result + " errors=" + r.summary.totalErrors);
        EditorApplication.Exit(r.summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    static string Env(string k)
    {
        var v = Environment.GetEnvironmentVariable(k);
        if (string.IsNullOrEmpty(v)) throw new Exception("missing " + k);
        return v;
    }

    const string IconDir = "Assets/_Project/Art/AndroidIcons";

    static void AssignAndroidIcons()
    {
#if UNITY_ANDROID
        var bg = PrepareIcon(IconDir + "/adaptive-background.png");
        var fg = PrepareIcon(IconDir + "/adaptive-foreground.png");
        var legacy = PrepareIcon(IconDir + "/legacy-192.png");
        ApplyIcons(AndroidPlatformIconKind.Adaptive, icon =>
        {
            icon.SetTexture(bg, 0);
            icon.SetTexture(fg, 1);
        });
        // minSdk is API 25, so the deprecated round and legacy slots still ship.
#pragma warning disable CS0618
        ApplyIcons(AndroidPlatformIconKind.Round, icon => icon.SetTexture(legacy, 0));
        ApplyIcons(AndroidPlatformIconKind.Legacy, icon => icon.SetTexture(legacy, 0));
#pragma warning restore CS0618
        Debug.Log("ANDROIDBUILD icons fg=" + fg.width + "x" + fg.height
            + " bg=" + bg.width + "x" + bg.height
            + " legacy=" + legacy.width + "x" + legacy.height);
#else
        throw new Exception("ANDROIDBUILD icons require an Android build target");
#endif
    }

#if UNITY_ANDROID
    static void ApplyIcons(PlatformIconKind kind, Action<PlatformIcon> assign)
    {
        var icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
        if (icons == null || icons.Length == 0)
            throw new Exception("ANDROIDBUILD no icon slots for " + kind);
        foreach (var icon in icons)
            assign(icon);
        PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, icons);
        Debug.Log("ANDROIDBUILD icons kind=" + kind + " slots=" + icons.Length);
    }

    static Texture2D PrepareIcon(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) throw new Exception("ANDROIDBUILD icon missing " + path);
        imp.textureType = TextureImporterType.Default;
        imp.mipmapEnabled = false;
        imp.npotScale = TextureImporterNPOTScale.None;
        imp.isReadable = true;
        imp.sRGBTexture = true;
        imp.alphaIsTransparency = true;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        var platform = imp.GetPlatformTextureSettings("Android");
        platform.overridden = true;
        platform.format = TextureImporterFormat.RGBA32;
        platform.textureCompression = TextureImporterCompression.Uncompressed;
        platform.crunchedCompression = false;
        platform.maxTextureSize = 2048;
        imp.SetPlatformTextureSettings(platform);
        imp.SaveAndReimport();
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null) throw new Exception("ANDROIDBUILD failed to load " + path);
        return tex;
    }
#endif
}
