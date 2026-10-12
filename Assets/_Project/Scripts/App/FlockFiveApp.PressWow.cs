using System.Collections;
using UnityEngine;

namespace FlockFive
{
    // LEVEL flower polish. Paint only: nothing here reads input, gates the press or
    // waits on a clock, so the sink and Load still fire on the frame the tap lands.
    public sealed partial class FlockFiveApp
    {
        // Idle heartbeat period. Slow enough to read as breathing, not a blink.
        const float LevelBeat = 3.4f;

        // Glints that orbit the clay petals (outside the disc, so the caption stays clean).
        static readonly float[] LevelOrbitSpeed = { 0.52f, -0.41f, 0.67f };
        static readonly float[] LevelOrbitPhase = { 0.3f, 2.4f, 4.4f };
        static readonly Color[] LevelOrbitTint =
        {
            new Color(1f, 0.93f, 0.70f, 1f),
            new Color(1f, 0.82f, 0.46f, 1f),
            new Color(1f, 0.80f, 0.70f, 1f),
        };

        // Idle life for the LEVEL flower: a soft double-beat swell in the halo, three
        // comet glints orbiting the petals, and a rim catch-light once per beat.
        // Layered on top of the shared halo/shimmer; scale stays 1 (no quiver).
        static void DrawLevelIdleLife(Rect visual)
        {
            if (!GuiPaint() || visual.width < 16f || visual.height < 16f) return;
            float t = Time.unscaledTime;
            float w = visual.width;
            float h = visual.height;
            var c = new Vector2(visual.center.x, visual.y + h * 0.47f);

            // Heartbeat: lub-dub then a long rest. Gaussian bumps ease in and out.
            float u = Mathf.Repeat(t, LevelBeat) / LevelBeat;
            float d1 = (u - 0.08f) / 0.035f;
            float d2 = (u - 0.19f) / 0.045f;
            float beat = Mathf.Exp(-d1 * d1) + 0.6f * Mathf.Exp(-d2 * d2);
            if (beat > 0.02f)
            {
                float pad = w * (0.10f + 0.06f * beat);
                SparkleFx.DrawGlow(c, w + pad * 2f, new Color(1f, 0.80f, 0.42f, 0.20f * beat));
            }

            // Orbiting comets: head plus three fading echoes along the path.
            float rx = w * 0.47f;
            float ry = h * 0.43f;
            for (int k = 0; k < LevelOrbitSpeed.Length; k++)
            {
                float tw = 0.55f + 0.45f * Mathf.Sin(t * 3.1f + k * 1.7f);
                var tint = LevelOrbitTint[k];
                float dir = LevelOrbitSpeed[k] < 0f ? -1f : 1f;
                float head = t * LevelOrbitSpeed[k] + LevelOrbitPhase[k];
                for (int j = 3; j >= 0; j--)
                {
                    float ang = head - dir * j * 0.11f;
                    var p = new Vector2(c.x + Mathf.Cos(ang) * rx, c.y + Mathf.Sin(ang) * ry);
                    float fall = 1f - j * 0.26f;
                    float sz = w * (0.050f + 0.030f * tw) * fall;
                    float a = (0.42f + 0.40f * tw) * fall * fall;
                    if (j == 0)
                        SparkleFx.DrawGlow(p, sz * 2.4f, new Color(tint.r, tint.g, tint.b, 0.26f * a));
                    SparkleFx.DrawAt(p, sz, a, tint);
                }
            }

            // Catch-light: a star blooms on the upper-right rim mid-beat and fades.
            float ping = (u - 0.52f) * LevelBeat;
            if (ping > 0f && ping < 0.42f)
            {
                float g = ping < 0.10f ? ping / 0.10f : 1f - (ping - 0.10f) / 0.32f;
                g = g * g * (3f - 2f * g);
                var at = new Vector2(visual.x + w * 0.80f, visual.y + h * 0.17f);
                SparkleFx.DrawGlow(at, w * 0.30f * g, new Color(1f, 0.92f, 0.66f, 0.35f * g));
                SparkleFx.DrawAt(at, w * (0.10f + 0.12f * g), 0.95f * g, new Color(1f, 0.97f, 0.86f, 1f));
            }
            GUI.color = Color.white;
        }

        // Press-down time for the LEVEL flower. Paint clock only; set on the PressedEdge frame.
        float _levelPressAt = -1f;

        // Under the flower while held: a warm pool of light the disc sinks into.
        // Ease-out over 0.12 s so it lands with the thunk, not after it.
        static void DrawLevelPressUnder(Rect visual, float age)
        {
            if (!GuiPaint() || visual.width < 16f) return;
            if (age < 0f) age = 1f;
            float u = Mathf.Clamp01(age / 0.12f);
            float e = 1f - (1f - u) * (1f - u) * (1f - u);
            var c = new Vector2(visual.center.x, visual.y + visual.height * 0.42f);
            SparkleFx.DrawGlow(c, visual.width * (1.05f + 0.20f * e), new Color(1f, 0.74f, 0.32f, 0.42f * e));
            GUI.color = Color.white;
        }

        // Over the sunk disc while held. Frame 0 gets a contact flash so the tap reads
        // instantly; then six glints spiral in from the petals and circle tight, a
        // charge-up that the release burst pays off.
        static void DrawLevelPressOver(Rect visual, float age)
        {
            if (!GuiPaint() || visual.width < 16f) return;
            if (age < 0f) age = 1f;
            float t = Time.unscaledTime;
            float w = visual.width;
            var disc = FlowerDisc(visual, 0f);
            var c = disc.center;

            float flash = 1f - Mathf.Clamp01(age / 0.10f);
            if (flash > 0.01f)
                SparkleFx.DrawGlow(c, disc.width * (1.1f + 0.4f * (1f - flash)), new Color(1f, 0.95f, 0.78f, 0.55f * flash * flash));

            float gather = Mathf.Clamp01(age / 0.40f);
            float ge = 1f - (1f - gather) * (1f - gather);
            float r = w * Mathf.Lerp(0.62f, 0.40f, ge);
            for (int k = 0; k < 6; k++)
            {
                float ang = k * 1.0472f + t * 2.6f + (1f - ge) * 1.4f;
                var p = new Vector2(c.x + Mathf.Cos(ang) * r, c.y + Mathf.Sin(ang) * r * 0.78f);
                float tw = 0.6f + 0.4f * Mathf.Sin(t * 9f + k * 1.3f);
                float a = (0.35f + 0.55f * ge) * tw;
                float sz = w * (0.055f + 0.025f * ge);
                SparkleFx.DrawAt(p, sz, a, (k & 1) == 0 ? new Color(1f, 0.90f, 0.58f, 1f) : new Color(1f, 0.97f, 0.88f, 1f));
            }
            GUI.color = Color.white;
        }

        // Capture hook (/tmp/flock-five-press-shot). DrawFlowerPlay ORs these into the
        // real held / launch, so the shot drives the same press path a finger does.
        bool _pressShotHeld;
        bool _pressShotFire;

        bool TakePressShotFire()
        {
            if (!_pressShotFire) return false;
            _pressShotFire = false;
            return true;
        }

        static void SavePressShotFrame(string frames, int index, string still)
        {
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            if (tex == null) return;
            try
            {
                System.IO.File.WriteAllBytes(frames + "/" + index.ToString("D4") + ".jpg", tex.EncodeToJPG(92));
                if (still != null) System.IO.File.WriteAllBytes(still, tex.EncodeToPNG());
            }
            finally { UnityEngine.Object.Destroy(tex); }
        }

        // 1 s of idle, a 100 ms press, release (Load fires that frame), then 2.4 s on.
        // Time.captureFramerate = 60 locks the clock to one 60 fps step per captured frame.
        IEnumerator ShotSplashPress()
        {
            const string dir = "/tmp/paradice";
            string frames = dir + "/splash-press-60fps";
            System.IO.Directory.CreateDirectory(dir);
            try { if (System.IO.Directory.Exists(frames)) System.IO.Directory.Delete(frames, true); } catch { }
            System.IO.Directory.CreateDirectory(frames);
            int keepRate = Time.captureFramerate;
            bool hadNext = PlayerPrefs.HasKey("flockfive.next");
            int keepNext = PlayerPrefs.GetInt("flockfive.next", 0);
            _pressShotHeld = false;
            _pressShotFire = false;
            LevelCurtain.Reset();
            ShowSplash();
            _home = HomeFace.Splash;
            yield return new WaitForSecondsRealtime(0.8f);
            Time.captureFramerate = 60;
            int f = 0;
            for (int i = 0; i < 60; i++)
            {
                yield return new WaitForEndOfFrame();
                SavePressShotFrame(frames, f++, i == 30 ? dir + "/splash-press-0-idle.png" : null);
            }
            _pressShotHeld = true;
            for (int i = 0; i < 6; i++)
            {
                yield return new WaitForEndOfFrame();
                SavePressShotFrame(frames, f++, i == 3 ? dir + "/splash-press-1-press.png" : null);
            }
            _pressShotHeld = false;
            _pressShotFire = true;
            for (int k = 0; k < 144; k++)
            {
                yield return new WaitForEndOfFrame();
                string still = k == 5 ? dir + "/splash-press-2-burst-peak.png"
                    : k == 24 ? dir + "/splash-press-3-handoff.png"
                    : k == 66 ? dir + "/splash-press-4-settle.png"
                    : null;
                SavePressShotFrame(frames, f++, still);
            }
            Time.captureFramerate = keepRate;
            _pressShotHeld = false;
            _pressShotFire = false;
            EncodeSmashVideo(frames, dir + "/splash-press-60fps.mp4", 60f);
            yield return new WaitForSecondsRealtime(0.4f);
            LevelCurtain.Reset();
            ShowSplash();
            _home = HomeFace.Splash;
            if (hadNext) PlayerPrefs.SetInt("flockfive.next", keepNext);
            else PlayerPrefs.DeleteKey("flockfive.next");
            PlayerPrefs.Save();
            Debug.Log("FF_PRESS_SHOT_DONE frames=" + f);
        }
    }
}
