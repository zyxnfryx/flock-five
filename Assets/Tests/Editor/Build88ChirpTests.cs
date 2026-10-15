#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 88. Sfx.Chirp(color) is the real hummingbird chip for that color.
    // Pitch stays at 1 on the lead seat. The firework whistle is not a chirp.
    public static class Build88ChirpTests
    {
        const string Out = "/tmp/flock-five-b88-chirp-tests.txt";

        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail)
            {
                string line = (ok ? "PASS  " : "FAIL  ") + name + "  " + detail;
                if (ok) pass++; else fail++;
                sb.AppendLine(line);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build88chirp] " + line);
            }

            try
            {
                AudioGuard.ResetForTests();
                if (Ads.IsShowing) Ads.ForceClear();
                for (int i = 0; i < Palette.Max; i++)
                {
                    var color = (BirdColor)i;
                    string want = "hum_sel_" + i.ToString("00");
                    int before = Sfx.LastShots;
                    Sfx.Chirp(color);
                    bool ok = Sfx.LastShots == before + 1
                        && Sfx.LastClipName == want
                        && Sfx.LastClipName != "fw-whistle"
                        && Mathf.Abs(Sfx.LastPitch - 1f) < 0.0001f
                        && Sfx.LastLayer == MixLayer.Lead;
                    Check("chirp-" + color.ToString().ToLowerInvariant(),
                        ok,
                        "clip=" + Sfx.LastClipName
                        + " pitch=" + Sfx.LastPitch.ToString("0.00")
                        + " layer=" + Sfx.LastLayer
                        + " shots=" + (Sfx.LastShots - before));
                }
            }
            catch (System.Exception ex)
            {
                Check("chirp", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                // The host is only for this check. Later suites must not inherit a desk.
                for (int n = 0; n < 8; n++)
                {
                    var go = GameObject.Find("Sfx");
                    if (go == null) break;
                    Object.DestroyImmediate(go);
                }
            }

            string end = fail == 0 ? "BUILD88CHIRP_OK " + pass : "BUILD88CHIRP_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[build88chirp] " + end);
            if (fail > 0) throw new System.Exception(end);
        }
    }
}
#endif
