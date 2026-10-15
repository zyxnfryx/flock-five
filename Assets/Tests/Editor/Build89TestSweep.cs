#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 89. Runs every editor test class (a static Run() in FlockFive.Editor
    // whose name holds "Tests" or "Checks") one after another and sums the
    // PASS/FAIL lines each class logs. Batch: -executeMethod
    // FlockFive.Editor.Build89TestSweep.RunAll. Exit code 0 only when nothing failed.
    public static class Build89TestSweep
    {
        const string Out = "/tmp/flock-five-b89-all-tests.txt";

        public static void RunAll()
        {
            var types = typeof(Build89TestSweep).Assembly.GetTypes()
                .Where(t => t.Namespace == "FlockFive.Editor" && t != typeof(Build89TestSweep)
                    && (t.Name.Contains("Tests") || t.Name.Contains("Checks")))
                .Select(t => new { t, m = t.GetMethod("Run", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null) })
                .Where(x => x.m != null)
                .OrderBy(x => x.t.Name, StringComparer.Ordinal)
                .ToList();
            var sb = new StringBuilder();
            int totalPass = 0, totalFail = 0, crashed = 0;
            foreach (var x in types)
            {
                int pass = 0, fail = 0;
                var fails = new StringBuilder();
                Application.LogCallback hook = (msg, stack, kind) =>
                {
                    if (msg == null) return;
                    int close = msg.IndexOf("] ", StringComparison.Ordinal);
                    string body = msg.StartsWith("[") && close > 0 ? msg.Substring(close + 2) : msg;
                    if (body.StartsWith("PASS")) pass++;
                    else if (body.StartsWith("FAIL")) { fail++; fails.AppendLine("    " + body); }
                };
                Application.logMessageReceived += hook;
                string crash = null;
                try { x.m.Invoke(null, null); }
                catch (Exception e) { crash = (e.InnerException ?? e).GetType().Name + ": " + (e.InnerException ?? e).Message; }
                finally { Application.logMessageReceived -= hook; }
                if (crash != null) { crashed++; fail++; fails.AppendLine("    CRASH " + crash); }
                totalPass += pass;
                totalFail += fail;
                sb.AppendLine((fail == 0 ? "OK    " : "FAIL  ") + x.t.Name + "  " + pass + " pass, " + fail + " fail");
                sb.Append(fails);
                File.WriteAllText(Out, sb.ToString());
            }
            string sum = "TOTAL classes " + types.Count + "  pass " + totalPass + "  fail " + totalFail + "  crashed " + crashed;
            sb.AppendLine(sum);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[b89sweep] " + sum);
            if (Application.isBatchMode) EditorApplication.Exit(totalFail == 0 ? 0 : 1);
        }
    }
}
#endif
