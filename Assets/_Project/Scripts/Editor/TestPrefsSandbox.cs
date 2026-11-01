#if UNITY_EDITOR
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace FlockFive.Editor
{
    // The edit-mode suites call PlayerPrefs directly (hive, ads, daily, ...), and in the editor those
    // land in the real prefs domain (macOS: ~/Library/Preferences/unity.<company>.<product>.plist),
    // the same one a local play session uses. RunEditorTests wraps the suites in this sandbox:
    // snapshot every key before, then wipe and put every key back (same type, same value) in finally.
    // On macOS the snapshot is the exported plist and is also re-imported through cfprefsd at the end,
    // so values PlayerPrefs cannot round-trip still come back exactly. No gameplay code changes.
    static class TestPrefsSandbox
    {
        internal enum Kind { Int, Float, String }

        internal struct Entry { public Kind kind; public string raw; }

        internal sealed class Scope : System.IDisposable
        {
            readonly Dictionary<string, Entry> snapshot;
            readonly string exportPath;
            bool disposed;

            internal Scope(Dictionary<string, Entry> snapshot, string exportPath)
            {
                this.snapshot = snapshot;
                this.exportPath = exportPath;
            }

            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                Restore(snapshot, exportPath);
            }
        }

        static string Domain => "unity." + PlayerSettings.companyName + "." + PlayerSettings.productName;

        internal static Scope Begin()
        {
            PlayerPrefs.Save();
            var snapshot = new Dictionary<string, Entry>();
            string exportPath = null;
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                exportPath = Path.Combine(Path.GetTempPath(),
                    "flockfive-prefs-" + System.DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Process.GetCurrentProcess().Id + ".plist");
                int code = Run("/usr/bin/defaults", "export \"" + Domain + "\" \"" + exportPath + "\"", out _);
                if (code != 0 || !File.Exists(exportPath))
                {
                    exportPath = null; // domain did not exist yet: restoring means leaving it empty
                }
                else
                {
                    Run("/usr/bin/plutil", "-convert xml1 -o - \"" + exportPath + "\"", out string xml);
                    Parse(xml, snapshot);
                }
            }
            Debug.Log("[prefs-sandbox] snapshot " + snapshot.Count + " keys of '" + Domain + "'" +
                      (exportPath != null ? " -> " + exportPath : ""));
            return new Scope(snapshot, exportPath);
        }

        static void Restore(Dictionary<string, Entry> snapshot, string exportPath)
        {
            PlayerPrefs.DeleteAll();
            foreach (var kv in snapshot)
            {
                var e = kv.Value;
                switch (e.kind)
                {
                    case Kind.Int:
                        if (int.TryParse(e.raw, out int i)) PlayerPrefs.SetInt(kv.Key, i);
                        break;
                    case Kind.Float:
                        if (float.TryParse(e.raw, System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out float f))
                            PlayerPrefs.SetFloat(kv.Key, f);
                        break;
                    default:
                        PlayerPrefs.SetString(kv.Key, e.raw);
                        break;
                }
            }
            PlayerPrefs.Save();

            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                if (exportPath != null)
                {
                    // Authoritative: put the exact exported domain back through cfprefsd.
                    int code = Run("/usr/bin/defaults", "import \"" + Domain + "\" \"" + exportPath + "\"", out _);
                    if (code != 0) Debug.LogError("[prefs-sandbox] defaults import failed (" + code + "), snapshot kept at " + exportPath);
                    else File.Delete(exportPath);
                }
                else
                {
                    Run("/usr/bin/defaults", "delete \"" + Domain + "\"", out _);
                }
            }
            Debug.Log("[prefs-sandbox] restored " + snapshot.Count + " keys of '" + Domain + "'");
        }

        static void Parse(string xml, Dictionary<string, Entry> into)
        {
            if (string.IsNullOrEmpty(xml)) return;
            var doc = new XmlDocument { XmlResolver = null };
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null };
            using (var r = XmlReader.Create(new StringReader(xml), settings)) doc.Load(r);
            var dict = doc.SelectSingleNode("/plist/dict");
            if (dict == null) return;
            string key = null;
            foreach (XmlNode n in dict.ChildNodes)
            {
                if (n.NodeType != XmlNodeType.Element) continue;
                if (n.Name == "key") { key = n.InnerText; continue; }
                if (key == null) continue;
                switch (n.Name)
                {
                    case "integer": into[key] = new Entry { kind = Kind.Int, raw = n.InnerText }; break;
                    case "real": into[key] = new Entry { kind = Kind.Float, raw = n.InnerText }; break;
                    case "string": into[key] = new Entry { kind = Kind.String, raw = n.InnerText }; break;
                    // other plist types are not PlayerPrefs values; defaults import restores them on macOS
                }
                key = null;
            }
        }

        static int Run(string file, string args, out string stdout)
        {
            var psi = new ProcessStartInfo(file, args)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using (var p = Process.Start(psi))
            {
                stdout = p.StandardOutput.ReadToEnd();
                p.StandardError.ReadToEnd();
                p.WaitForExit();
                return p.ExitCode;
            }
        }
    }
}
#endif
