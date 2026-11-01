#if UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace FlockFive.Editor
{
    // Play-mode still captures (Pr221Stills.Capture, ...) are a second entry point that the RunEditorTests
    // sandbox does not cover: a whole play session writes PlayerPrefs (hive, ads, daily ...) into the real
    // domain. A using-scope cannot span the play-mode domain reload, so this guard keeps its snapshot on disk:
    // Begin() exports the domain and records the path in a marker file; when play mode ends or the editor
    // quits, the exported domain is imported back through cfprefsd (exact restore), then the marker is cleared.
    [InitializeOnLoad]
    static class ShotPrefsGuard
    {
        const string Marker = "/tmp/flock-five-shot-prefs-pending";

        static string Domain => "unity." + PlayerSettings.companyName + "." + PlayerSettings.productName;

        static ShotPrefsGuard()
        {
            EditorApplication.playModeStateChanged += s => { if (s == PlayModeStateChange.EnteredEditMode) RestorePending(); };
            EditorApplication.quitting += RestorePending;
        }

        internal static void Begin()
        {
            if (Application.platform != RuntimePlatform.OSXEditor) return;
            if (File.Exists(Marker)) return; // an earlier capture never restored: keep its (older, real) snapshot
            PlayerPrefs.Save();
            string path = Path.Combine(Path.GetTempPath(), "flockfive-shot-prefs-" + System.DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".plist");
            if (Run("export \"" + Domain + "\" \"" + path + "\"") != 0 || !File.Exists(path)) path = "";
            File.WriteAllText(Marker, path);
            Debug.Log("[shot-prefs] snapshot '" + Domain + "' -> " + (path == "" ? "(none)" : path));
        }

        static void RestorePending()
        {
            if (!File.Exists(Marker)) return;
            string path = File.ReadAllText(Marker).Trim();
            PlayerPrefs.Save();
            int code = path == "" ? Run("delete \"" + Domain + "\"") : Run("import \"" + Domain + "\" \"" + path + "\"");
            if (path != "" && code != 0) { Debug.LogError("[shot-prefs] import failed (" + code + "), snapshot kept at " + path); return; }
            File.Delete(Marker);
            if (path != "") File.Delete(path);
            Debug.Log("[shot-prefs] restored '" + Domain + "'");
        }

        static int Run(string args)
        {
            var psi = new ProcessStartInfo("/usr/bin/defaults", args) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
            using (var p = Process.Start(psi)) { p.StandardOutput.ReadToEnd(); p.StandardError.ReadToEnd(); p.WaitForExit(); return p.ExitCode; }
        }
    }
}
#endif
