#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 87: kit-bird sparkle seats stay off the bow, face and name, scale with
    // DeviceFit.Ui(), and a bright star is up often enough for a capture to wait.
    public static class Build87SparkleTests
    {
        const string Out = "/tmp/flock-five-b87-sparkle-tests.txt";

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
                Debug.Log("[build87spark] " + line);
            }
            float? keepWindow = DeviceFit.TestWindow;
            try
            {
                var stars = new SparkleFx.KitStar[SparkleFx.KitStarCount];
                float phoneUi = DeviceFit.Ui(1179f, 2556f);
                DeviceFit.TestWindow = 1536f / 2048f;
                float padUi = DeviceFit.Ui(1536f, 2048f);
                DeviceFit.TestWindow = null;
                Check("ipad-ui-eased", padUi < phoneUi * 0.75f && padUi > phoneUi * 0.55f,
                    "phone " + phoneUi.ToString("0.00") + " ipad " + padUi.ToString("0.00"));

                float t0 = (Mathf.PI * 0.5f - 0.20f) / 2.2f;
                var body = new Rect(100f, 100f, 500f, 500f);
                int n2 = SparkleFx.PlaceKitBird(body, false, 2f, 2000f, 3000f, t0, false, stars);
                float s2 = StarSize(stars, 0);
                int n4 = SparkleFx.PlaceKitBird(body, false, 4f, 2000f, 3000f, t0, false, stars);
                float s4 = StarSize(stars, 0);
                Check("size-tracks-ui", n2 > 0 && n4 > 0 && s2 > 2f && Mathf.Abs(s4 / s2 - 2f) < 0.02f,
                    "ui2 " + s2.ToString("0.0") + " ui4 " + s4.ToString("0.0"));
                Check("size-is-ui-times-px", Mathf.Abs(s2 - SparkleFx.KitStarPx * 2f) < 0.05f,
                    s2.ToString("0.00") + " vs " + (SparkleFx.KitStarPx * 2f).ToString("0.00"));

                int dark = 0, hits = 0, samples = 0;
                Sweep(new Rect(450f, 700f, 274f, 274f), false, false, phoneUi, 1179f, 2556f, stars, ref dark, ref hits, ref samples);
                Sweep(new Rect(450f, 700f, 274f, 274f), true, false, phoneUi, 1179f, 2556f, stars, ref dark, ref hits, ref samples);
                Sweep(new Rect(450f, 700f, 411f, 411f), false, true, phoneUi, 1179f, 2556f, stars, ref dark, ref hits, ref samples);
                Sweep(new Rect(450f, 700f, 411f, 411f), true, true, phoneUi, 1179f, 2556f, stars, ref dark, ref hits, ref samples);
                Sweep(new Rect(600f, 700f, 219f, 219f), false, false, padUi, 1536f, 2048f, stars, ref dark, ref hits, ref samples);
                Sweep(new Rect(600f, 700f, 219f, 219f), true, false, padUi, 1536f, 2048f, stars, ref dark, ref hits, ref samples);
                Sweep(new Rect(600f, 700f, 330f, 330f), false, true, padUi, 1536f, 2048f, stars, ref dark, ref hits, ref samples);
                Sweep(new Rect(600f, 700f, 330f, 330f), true, true, padUi, 1536f, 2048f, stars, ref dark, ref hits, ref samples);
                Check("centered-always-bright", dark == 0 && samples > 0, dark + "/" + samples);
                Check("clear-of-bow-face-plate", hits == 0, hits + " overlaps");

                int perchDark = 0, perchHits = 0, perchN = 0, perchOff = 0;
                Edge(new Rect(1179f - 274f - 40f, 1100f, 274f, 274f), false, phoneUi, 1179f, 2556f, stars, ref perchDark, ref perchHits, ref perchN, ref perchOff);
                Edge(new Rect(1179f - 411f - 40f, 900f, 411f, 411f), true, phoneUi, 1179f, 2556f, stars, ref perchDark, ref perchHits, ref perchN, ref perchOff);
                Edge(new Rect(1536f - 219f - 36f, 900f, 219f, 219f), false, padUi, 1536f, 2048f, stars, ref perchDark, ref perchHits, ref perchN, ref perchOff);
                Edge(new Rect(1536f - 330f - 36f, 800f, 330f, 330f), true, padUi, 1536f, 2048f, stars, ref perchDark, ref perchHits, ref perchN, ref perchOff);
                Check("perch-finds-a-bright-frame", perchDark < perchN, perchDark + "/" + perchN);
                Check("perch-on-screen-off-face", perchHits == 0 && perchOff == 0, "hit " + perchHits + " off " + perchOff);

                string root = Application.dataPath;
                string avatar = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.Avatar.cs"));
                string home = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.cs"));
                string shot = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.Build87.cs"));
                string fx = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/SparkleFx.cs"));
                Check("adopt-draws-kit-stars", avatar.Contains("SparkleFx.DrawKitBird") && avatar.Contains("SavedAvatarKit() != 0"), "adopt path");
                Check("home-draws-kit-stars", home.Contains("SparkleFx.DrawKitBird") && home.Contains("SavedAvatarKit() != 0"), "home path");
                Check("shot-waits-for-bright", shot.Contains("KitBirdPeak") && shot.Contains("KitStarBright"), "capture gate");
                int drawAt = fx.IndexOf("void DrawKitBird");
                int drawCall = drawAt >= 0 ? fx.IndexOf("DrawAt(", drawAt) : -1;
                Check("kit-uses-drawat", drawAt >= 0 && drawCall > drawAt && drawCall < drawAt + 900, "shared DrawAt");
            }
            catch (System.Exception ex)
            {
                Check("sparkle", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                DeviceFit.TestWindow = keepWindow;
            }
            string end = fail == 0 ? "BUILD87SPARK_OK " + pass : "BUILD87SPARK_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[build87spark] " + end);
            if (fail > 0) throw new System.Exception(end);
        }

        static float StarSize(SparkleFx.KitStar[] stars, int i)
        {
            return stars[i].On != 0 ? stars[i].Size : 0f;
        }

        static void Sweep(Rect body, bool faceLeft, bool wings, float ui, float sw, float sh, SparkleFx.KitStar[] stars, ref int dark, ref int hits, ref int samples)
        {
            const int n = 48;
            float period = Mathf.PI * 2f / 2.2f;
            for (int k = 0; k < n; k++)
            {
                float t = k * period / n;
                SparkleFx.PlaceKitBird(body, faceLeft, ui, sw, sh, t, wings, stars);
                samples++;
                float peak = 0f;
                for (int i = 0; i < stars.Length; i++)
                {
                    if (stars[i].On == 0) continue;
                    if (stars[i].Alpha > peak) peak = stars[i].Alpha;
                    if (BadSeat(body, faceLeft, wings, stars[i], sw, sh)) hits++;
                }
                if (peak < SparkleFx.KitStarBright) dark++;
            }
        }

        static void Edge(Rect body, bool wings, float ui, float sw, float sh, SparkleFx.KitStar[] stars, ref int dark, ref int hits, ref int samples, ref int off)
        {
            const int n = 48;
            float period = Mathf.PI * 2f / 2.2f;
            for (int k = 0; k < n; k++)
            {
                float t = k * period / n;
                SparkleFx.PlaceKitBird(body, true, ui, sw, sh, t, wings, stars);
                samples++;
                float peak = 0f;
                for (int i = 0; i < stars.Length; i++)
                {
                    if (stars[i].On == 0) continue;
                    if (stars[i].Alpha > peak) peak = stars[i].Alpha;
                    if (BadSeat(body, true, wings, stars[i], sw, sh)) hits++;
                    float m = stars[i].Size * 0.5f;
                    if (stars[i].X - m < 0f || stars[i].Y - m < 0f || stars[i].X + m > sw || stars[i].Y + m > sh)
                        off++;
                }
                if (peak < SparkleFx.KitStarBright) dark++;
            }
        }

        // Independent of the seat table. The sheet square is mostly empty, so a
        // star in that padding is fine. The torso is the opaque middle. The
        // upper box is the bow, crown and face. The plaque sits under the feet
        // and higher on the flap sheet.
        static bool BadSeat(Rect body, bool faceLeft, bool wings, SparkleFx.KitStar s, float sw, float sh)
        {
            float sign = faceLeft ? -1f : 1f;
            float dx = (s.X - body.center.x) / body.width * sign;
            float dy = (s.Y - body.center.y) / body.height;
            float tx0 = wings ? -0.22f : -0.16f;
            float ty0 = wings ? -0.36f : -0.12f;
            float tx1 = wings ? 0.12f : 0.14f;
            float ty1 = wings ? 0.10f : 0.12f;
            if (dx > tx0 && dx < tx1 && dy > ty0 && dy < ty1) return true;
            var at = new Vector2(s.X, s.Y);
            float pad = s.Size * 0.2f;
            float top = body.y - body.height * 0.12f - pad;
            float bot = body.y + body.height * 0.38f + pad;
            float x0 = faceLeft ? body.x - pad : body.x + body.width * 0.40f - pad;
            float x1 = faceLeft ? body.x + body.width * 0.60f + pad : body.xMax + pad;
            if (at.x >= x0 && at.x <= x1 && at.y >= top && at.y <= bot) return true;
            float hw = wings ? 0.24f : 0.30f;
            float oy0 = wings ? 0.32f : 0.50f;
            float oy1 = wings ? 0.56f : 0.70f;
            float px0 = body.center.x - body.width * hw - pad;
            float px1 = body.center.x + body.width * hw + pad;
            float py0 = body.center.y + oy0 * body.height - pad;
            float py1 = body.center.y + oy1 * body.height + pad;
            if (at.x >= px0 && at.x <= px1 && at.y >= py0 && at.y <= py1) return true;
            float m = s.Size * 0.5f;
            if (at.x - m < -1f || at.y - m < -1f || at.x + m > sw + 1f || at.y + m > sh + 1f) return true;
            return false;
        }
    }
}
#endif
