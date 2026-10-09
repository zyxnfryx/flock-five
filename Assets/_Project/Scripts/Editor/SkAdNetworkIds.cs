#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // Local copies of the LevelPlay SKAdNetwork lists. The package post-process
    // downloads the same lists for each installed adapter at iOS build time.
    // IronSource plus Unity Ads is 82 ids. AppLovin and Meta are merged here,
    // and again into Info.plist, with duplicates dropped.
    public static class SkAdNetworkIds
    {
        // Absolute so batchmode and the iOS post-process do not depend on cwd.
        public static string Folder => Path.Combine(Application.dataPath, "LevelPlay/Editor/SKAdNetwork");

        public static List<string> Parse(string xml)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(xml)) return list;
            const string open = "<string>";
            const string close = "</string>";
            int i = 0;
            while (true)
            {
                int a = xml.IndexOf(open, i, StringComparison.Ordinal);
                if (a < 0) break;
                a += open.Length;
                int b = xml.IndexOf(close, a, StringComparison.Ordinal);
                if (b < 0) break;
                string id = xml.Substring(a, b - a).Trim();
                if (id.EndsWith(".skadnetwork", StringComparison.OrdinalIgnoreCase))
                    list.Add(id);
                i = b + close.Length;
            }
            return list;
        }

        public static List<string> Dedup(IEnumerable<string> ids)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var list = new List<string>();
            if (ids == null) return list;
            foreach (string raw in ids)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                string id = raw.Trim();
                if (!seen.Add(id)) continue;
                list.Add(id);
            }
            return list;
        }

        public static List<string> LoadMerged(string folder = null)
        {
            var all = new List<string>();
            string dir = string.IsNullOrEmpty(folder) ? Folder : folder;
            if (!Directory.Exists(dir)) return all;
            string[] files = Directory.GetFiles(dir, "*.plist.xml");
            Array.Sort(files, StringComparer.Ordinal);
            for (int i = 0; i < files.Length; i++)
                all.AddRange(Parse(File.ReadAllText(files[i])));
            return Dedup(all);
        }
    }
}
#endif
