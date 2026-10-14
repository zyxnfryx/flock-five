#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 89 smoothness pass. The master soft-clip is transparent under its knee and holds
    // its ceiling on overs; AudioPolish keeps length and loop seams; music imports keep
    // stereo, no normalize, preload on.
    static class SmoothPassAudioTests
    {
        [MenuItem("Flock Five/Smooth Pass Audio Tests")]
        public static void Run()
        {
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++; else fail++;
                Debug.Log("[smooth-audio] " + (ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            var go = new GameObject("SmoothTest");
            try
            {
                var lim = go.AddComponent<MasterSoftClip>();
                lim.Setup(48000);
                int look = Mathf.RoundToInt(48000 * 0.002f);
                // 1. Quiet signal: output is the input, delayed by the look-ahead, bit for bit.
                var quiet = new float[48000 * 2];
                for (int i = 0; i < quiet.Length / 2; i++)
                    quiet[2 * i] = quiet[2 * i + 1] = 0.4f * Mathf.Sin(2f * Mathf.PI * 440f * i / 48000f);
                var q = (float[])quiet.Clone();
                lim.Process(q, 2);
                float diff = 0f;
                for (int i = look; i < quiet.Length / 2; i++)
                    diff = Mathf.Max(diff, Mathf.Abs(q[2 * i] - quiet[2 * (i - look)]));
                Check("transparent under the knee", diff < 1e-6f, "maxDiff=" + diff);

                // 2. +8 dB over full scale: never past the ceiling's saturator, never 1.0.
                lim.Setup(48000);
                var hot = new float[48000 * 2];
                for (int i = 0; i < hot.Length / 2; i++)
                    hot[2 * i] = hot[2 * i + 1] = 2.5f * Mathf.Sin(2f * Mathf.PI * 220f * i / 48000f);
                lim.Process(hot, 2);
                float peak = 0f;
                for (int i = 0; i < hot.Length; i++) peak = Mathf.Max(peak, Mathf.Abs(hot[i]));
                float peakDb = 20f * Mathf.Log10(peak);
                Check("ceiling holds on +8 dB overs", peakDb <= MasterSoftClip.CeilingDb + 0.2f && peak < 1f,
                    "peak=" + peakDb.ToString("0.00") + " dBFS");

                // 3. AudioPolish keeps length, finite, loop seam continuous.
                var loop = new float[22050 * 2 * 4];
                for (int i = 0; i < loop.Length / 2; i++)
                    loop[2 * i] = loop[2 * i + 1] = 0.5f * Mathf.Sin(2f * Mathf.PI * 147f * i / 22050f);
                int n0 = loop.Length;
                AudioPolish.Apply(loop, 2, 22050, true);
                bool finite = true;
                for (int i = 0; i < loop.Length; i++) if (float.IsNaN(loop[i]) || float.IsInfinity(loop[i])) finite = false;
                float seam = Mathf.Abs(loop[0] - loop[loop.Length - 2]);
                Check("AudioPolish keeps length and seam", loop.Length == n0 && finite && seam < 0.05f, "seam=" + seam);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }

            // 4. Import settings for every music clip.
            string[] music =
            {
                "Assets/_Project/Art/Resources/Audio/Bed/garden-theme.wav",
                "Assets/_Project/Art/Resources/Audio/Bed/splash-theme.wav",
                "Assets/_Project/Art/Resources/Audio/Bed/badger-battle.wav",
                "Assets/_Project/Art/Resources/Audio/Bed/badger-entrance.wav",
                "Assets/_Project/Art/Resources/Audio/Bed/badger-win.wav",
                "Assets/_Project/Art/Resources/Audio/Bed/badger-lose.wav",
                "Assets/_Project/Art/Resources/Audio/zfx_jingle.ogg",
            };
            foreach (var path in music)
            {
                var imp = AssetImporter.GetAtPath(path) as AudioImporter;
                bool ok = imp != null && !imp.forceToMono && !imp.normalize && imp.defaultSampleSettings.preloadAudioData;
                Check("import " + System.IO.Path.GetFileName(path), ok,
                    imp == null ? "missing" : "mono=" + imp.forceToMono + " normalize=" + imp.normalize
                        + " preload=" + imp.defaultSampleSettings.preloadAudioData);
            }

            Debug.Log("[smooth-audio] " + pass + " pass, " + fail + " fail");
        }
    }
}
#endif
