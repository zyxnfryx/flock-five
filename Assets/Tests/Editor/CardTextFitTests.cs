#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Title and flavor bands stay apart, and the wrapped copy fits each band
    // at a grid card and at a fullscreen inspect card. Includes Static Wing.
    // Run from the menu, or drop a file on /tmp/flock-five-card-fit.
    [InitializeOnLoad]
    static class CardTextFitTests
    {
        const string Cmd = "/tmp/flock-five-card-fit";
        const string Out = "/tmp/flock-five-card-fit.txt";

        static CardTextFitTests()
        {
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (!File.Exists(Cmd) || EditorApplication.isCompiling) return;
            try { File.Delete(Cmd); }
            catch { return; }
            Run();
        }

        [MenuItem("Flock Five/Card Text Fit Tests")]
        public static void Run()
        {
            var sb = new StringBuilder();
            int pass = 0;
            int fail = 0;
            void Check(string name, bool ok)
            {
                if (ok) pass++; else fail++;
                string line = (ok ? "PASS  " : "FAIL  ") + name;
                sb.AppendLine(line);
                Debug.Log("[card-fit] " + line);
            }

            // GUI.skin throws outside OnGUI, and -executeMethod is not OnGUI.
            // Probe measures with its own style, including the built-in font.

            // Mid grid card, fullscreen inspect, and a half-scale flip.
            var faces = new Rect[]
            {
                new Rect(0f, 0f, 220f, 320f),
                new Rect(0f, 0f, 420f, 600f),
                new Rect(0f, 0f, 780f, 1100f),
                new Rect(0f, 0f, 390f, 1100f)
            };
            string[] titles =
            {
                "Static Wing",
                "Distracted Wing",
                "Spaghetti Nest",
                "Phoenix Pollen"
            };
            string[] flavors =
            {
                "Charged personality.",
                "Light precipitation, heavy opinions.",
                "Faces the light. Judging you.",
                "Essential oils, optional attitude. Smells like calm; acts like overtime."
            };

            for (int f = 0; f < faces.Length; f++)
            {
                bool full = f >= 2;
                for (int i = 0; i < titles.Length; i++)
                {
                    string tag = (full ? "full " : "mid ") + faces[f].width + " " + titles[i];
                    bool front = FlockFiveApp.CardText.Probe(faces[f], titles[i], flavors[i], full, false, out var titleR, out var flavorR);
                    Check(tag + " front", front && !titleR.Overlaps(flavorR) && titleR.height > 4f && flavorR.height > 4f);
                    bool back = FlockFiveApp.CardText.Probe(faces[f], titles[i], flavors[i], full, true, out var bt, out var bf);
                    Check(tag + " back", back && !bt.Overlaps(bf) && bt.yMax <= bf.y + 0.5f);
                }
            }

            sb.AppendLine(fail == 0 ? "ALL OK  " + pass : "FAILED  " + fail + "  passed " + pass);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[card-fit] " + (fail == 0 ? "ALL OK " + pass : "FAILED " + fail));
        }
    }
}
#endif
