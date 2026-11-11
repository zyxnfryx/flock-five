#if UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace FlockFive.Editor
{
    // DEVELOPMENT_BUILD Settings reads DevShaStamp.Sha. This writes that file
    // from `git rev-parse --short=8 HEAD` when the editor loads. A player build
    // compiles scripts before OnPreprocessBuild, so the stamp in a build is the
    // HEAD from the last domain load, not a hash typed into the source.
    [InitializeOnLoad]
    public static class DevShaStampWriter
    {
        const string Rel = "Assets/_Project/Scripts/App/DevSha.Generated.cs";

        static DevShaStampWriter()
        {
            Write();
        }

        public class Hook : IPreprocessBuildWithReport
        {
            public int callbackOrder => 0;
            public void OnPreprocessBuild(BuildReport report) => Write();
        }

        public static void Write()
        {
            string sha = GitShort();
            if (sha.Length < 7) return;
            string path = Path.Combine(Directory.GetCurrentDirectory(), Rel);
            string body = Body(sha);
            if (File.Exists(path))
            {
                string have = File.ReadAllText(path);
                if (have == body) return;
            }
            File.WriteAllText(path, body);
            AssetDatabase.ImportAsset(Rel);
        }

        static string Body(string sha)
        {
            return
                "// Rewritten from git HEAD when the editor loads and before a player build.\n" +
                "// A commit cannot contain its own hash, so this fallback is the HEAD at the\n" +
                "// last time the writer ran.\n" +
                "namespace FlockFive\n" +
                "{\n" +
                "    public static class DevShaStamp\n" +
                "    {\n" +
                "        public const string Sha = \"" + sha + "\";\n" +
                "    }\n" +
                "}\n";
        }

        static string GitShort()
        {
            try
            {
                var psi = new ProcessStartInfo("git", "rev-parse --short=8 HEAD")
                {
                    WorkingDirectory = Directory.GetCurrentDirectory(),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (var p = Process.Start(psi))
                {
                    if (p == null) return "";
                    string sha = p.StandardOutput.ReadToEnd().Trim();
                    p.WaitForExit(4000);
                    if (p.ExitCode != 0) return "";
                    return sha;
                }
            }
            catch
            {
                return "";
            }
        }
    }
}
#endif
