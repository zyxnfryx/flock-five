#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 82 pass b82b. Kit identity, the 3-set cap, and gardens 1-29.
    // Run from the menu, or drop a file on /tmp/flock-five-b82-tests.
    [InitializeOnLoad]
    static class Build82LevelTests
    {
        const string Cmd = "/tmp/flock-five-b82-tests";
        const string Out = "/tmp/flock-five-b82-tests.txt";

        static Build82LevelTests()
        {
            EditorApplication.delayCall += Maybe;
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (File.Exists(Cmd)) Maybe();
        }

        static void Maybe()
        {
            if (!File.Exists(Cmd)) return;
            if (EditorApplication.isCompiling) return;
            try { File.Delete(Cmd); }
            catch { return; }
            Run();
        }

        [MenuItem("Flock Five/Build 82 Level Tests")]
        public static void Run()
        {
            var sb = new StringBuilder();
            int pass = 0;
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                string line = (ok ? "PASS  " : "FAIL  ") + name + "  " + detail;
                if (ok) pass++;
                else fail++;
                sb.AppendLine(line);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build82] " + line);
            }

            Build82LevelChecks.Run(Check);
            string end = fail == 0 ? "ALL OK " + pass : "FAILED " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[build82] " + end);
        }
    }
}
#endif
