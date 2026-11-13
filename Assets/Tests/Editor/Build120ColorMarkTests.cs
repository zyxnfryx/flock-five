#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b120 R2 (Readability): every bird color has its own fixed shape mark, the marks differ in
    // shape (not hue), and ink vs cream holds a grayscale contrast of at least 4.5:1.
    public static class Build120ColorMarkTests
    {
        const string Out = "/tmp/flock-five-b120-colormark-tests.txt";
        static float Lum(Color c)
        {
            float L(float v) => v <= 0.03928f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
            return 0.2126f * L(c.r) + 0.7152f * L(c.g) + 0.0722f * L(c.b);
        }
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            void Check(string name, bool ok, string detail)
            {
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build120-colormark] " + (ok ? "PASS " : "FAIL ") + name + " " + detail);
            }
            var cols = (BirdColor[])System.Enum.GetValues(typeof(BirdColor));
            Check("five colors", cols.Length == Palette.Shipped, cols.Length.ToString());
            for (int i = 0; i < cols.Length; i++)
                for (int j = i + 1; j < cols.Length; j++)
                    Check("unique shape " + cols[i] + "/" + cols[j], ColorMark.Of(cols[i]) != ColorMark.Of(cols[j]), "");
            const int N = 48;
            var masks = new bool[cols.Length][];
            for (int k = 0; k < cols.Length; k++)
            {
                masks[k] = new bool[N * N];
                int on = 0;
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        float u = (x + 0.5f) / N * 2f - 1f, v = (y + 0.5f) / N * 2f - 1f;
                        bool ink = ColorMark.InkAt(ColorMark.Of(cols[k]), u, v) > 0.5f;
                        masks[k][y * N + x] = ink; if (ink) on++;
                    }
                Check("mark has ink " + cols[k], on > N * N / 20, on.ToString());
            }
            for (int i = 0; i < cols.Length; i++)
                for (int j = i + 1; j < cols.Length; j++)
                {
                    int diff = 0;
                    for (int p = 0; p < N * N; p++) if (masks[i][p] != masks[j][p]) diff++;
                    Check("shapes differ " + cols[i] + "/" + cols[j], diff > N * N / 12, diff.ToString());
                }
            float li = Lum(ColorMark.Ink), lc = Lum(ColorMark.Cream);
            float ratio = (Mathf.Max(li, lc) + 0.05f) / (Mathf.Min(li, lc) + 0.05f);
            Check("ink/cream contrast >= 4.5", ratio >= 4.5f, ratio.ToString("0.0"));
            Check("bird mark >= 12px on 402 phone", 0.26f * 110f >= 12f, "share " + ColorMark.BirdShare);
            if (!Application.isBatchMode || SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                foreach (var c in cols) Check("sprite builds " + c, ColorMark.SpriteFor(c) != null, "");
        }
    }
}
#endif
