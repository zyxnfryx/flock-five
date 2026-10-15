using System.Collections.Generic;
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

        // Seats (radians, 0 = 3 o'clock, clockwise) on the flat ring. Five across the top arc
        // (-PI..0), two low on the sides. Phases keep neighbours out of step.
        static readonly float[] LevelRimAng = { -2.70f, -2.25f, -1.80f, -1.34f, -0.89f, -0.42f, 0.45f, 2.70f };
        static readonly float[] LevelRimPh = { 0.4f, 1.9f, 3.3f, 4.6f, 0.9f, 2.6f, 5.2f, 3.8f };

        // Idle life for the LEVEL flower: a soft double-beat swell in the halo, three
        // comet glints orbiting the petals, and a rim catch-light once per beat.
        // Layered on top of the shared halo/shimmer; scale stays 1 (no quiver).
        static void DrawLevelIdleLife(Rect visual)
        {
            if (!GuiPaint() || visual.width < 16f || visual.height < 16f) return;
            float t = AdPause.Unscaled;
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
            for (int k = 0; k < LevelOrbitSpeed.Length; k++)
            {
                float tw = 0.55f + 0.45f * Mathf.Sin(t * 3.1f + k * 1.7f);
                var tint = LevelOrbitTint[k];
                float dir = LevelOrbitSpeed[k] < 0f ? -1f : 1f;
                float head = t * LevelOrbitSpeed[k] + LevelOrbitPhase[k];
                for (int j = 3; j >= 0; j--)
                {
                    float ang = head - dir * j * 0.11f;
                    var p = SparkleFx.OnFlatRing(visual, ang);
                    float fall = 1f - j * 0.26f;
                    float sz = w * (0.050f + 0.030f * tw) * fall;
                    float a = (0.42f + 0.40f * tw) * fall * fall;
                    if (j == 0)
                        SparkleFx.DrawGlow(p, sz * 2.4f, new Color(tint.r, tint.g, tint.b, 0.26f * a));
                    SparkleFx.DrawAt(p, sz, a, tint);
                }
            }

            // Fixed twinkle seats on the same flat ring, weighted to the top arc so the upper
            // edge always has life (the comets alone leave it empty for long stretches).
            for (int k = 0; k < LevelRimAng.Length; k++)
            {
                float twk = 0.5f + 0.5f * Mathf.Sin(t * (1.9f + 0.17f * k) + LevelRimPh[k]);
                if (twk < 0.25f) continue;
                var q = SparkleFx.OnFlatRing(visual, LevelRimAng[k]);
                var rt = LevelOrbitTint[k % LevelOrbitTint.Length];
                SparkleFx.DrawAt(q, w * (0.030f + 0.030f * twk), 0.30f + 0.55f * twk, rt);
            }

            // Catch-light: a star blooms on the upper-right rim mid-beat and fades.
            float ping = (u - 0.52f) * LevelBeat;
            if (ping > 0f && ping < 0.42f)
            {
                float g = ping < 0.10f ? ping / 0.10f : 1f - (ping - 0.10f) / 0.32f;
                g = g * g * (3f - 2f * g);
                var at = SparkleFx.OnFlatRing(visual, -0.95f);
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
        // instantly; then small glints pulse off the disc together toward the player,
        // a simmer that the release burst pays off.
        static void DrawLevelPressOver(Rect visual, float age)
        {
            if (!GuiPaint() || visual.width < 16f) return;
            if (age < 0f) age = 1f;
            float w = visual.width;
            var disc = FlowerDisc(visual, 0f);
            var c = disc.center;

            float flash = 1f - Mathf.Clamp01(age / 0.10f);
            if (flash > 0.01f)
                SparkleFx.DrawGlow(c, disc.width * (1.1f + 0.4f * (1f - flash)), new Color(1f, 0.95f, 0.78f, 0.55f * flash * flash));

            // Simmer: every 0.5 s six glints leave the disc together on mirrored lanes,
            // rise a little and come back at the player, growing. The release pays it off.
            float build = Mathf.Clamp01(age / 0.30f);
            float ta = Mathf.Repeat(age, 0.5f);
            for (int k = 0; k < 6; k++)
            {
                float tx = c.x + SparkleFx.Lane(k, 6, 0.5f, w * 0.55f);
                if (!SparkleFx.DepthArcAt(ta, 0.5f, c.x, c.y, tx, c.y - w * 0.42f, c.y + w * 0.30f,
                        0.6f, 1.0f, 0.55f, out var p))
                    continue;
                SparkleFx.DrawDepth(p, w * 0.035f, (k & 2) == 0 ? new Color(1f, 0.90f, 0.58f, 1f) : new Color(1f, 0.97f, 0.88f, 1f),
                    0.35f + 0.55f * build);
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
            SavePressShotFrame(frames, index, still, false, out _, out _);
        }

        // measure: mean luma of 12 horizontal bands, sampled on a sparse grid; min/max
        // across bands proves the mask is even top to bottom.
        static void SavePressShotFrame(string frames, int index, string still, bool measure, out float rowMin, out float rowMax)
        {
            rowMin = rowMax = -1f;
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            if (tex == null) return;
            try
            {
                System.IO.File.WriteAllBytes(frames + "/" + index.ToString("D4") + ".jpg", tex.EncodeToJPG(92));
                if (still != null) System.IO.File.WriteAllBytes(still, tex.EncodeToPNG());
                if (measure)
                {
                    const int bands = 12, sx = 48, sy = 16;
                    rowMin = float.MaxValue; rowMax = float.MinValue;
                    for (int b = 0; b < bands; b++)
                    {
                        float sum = 0f;
                        for (int j = 0; j < sy; j++)
                        {
                            int py = (int)((b + (j + 0.5f) / sy) / bands * tex.height);
                            for (int k = 0; k < sx; k++)
                            {
                                int px = (int)((k + 0.5f) / sx * tex.width);
                                var c = tex.GetPixel(px, py);
                                sum += 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
                            }
                        }
                        float m = 255f * sum / (sx * sy);
                        if (m < rowMin) rowMin = m;
                        if (m > rowMax) rowMax = m;
                    }
                }
            }
            finally { UnityEngine.Object.Destroy(tex); }
        }

        // The shot is running. StartupProbe keeps batch play mode alive while it is set.
        public static bool PressShotLive;

        // DrawFlowerPlay notes every repaint it makes and the LEVEL gates it saw, so the
        // shot can wait for a button that is really on screen and really pressable.
        int _levelDrawFrame = -1;
        int _levelDrawRun;
        SplashPress.Gates _levelGates;

        void NoteLevelDrawn(SplashPress.Gates gates)
        {
            if (!GuiPaint()) return;
            int f = Time.frameCount;
            if (f == _levelDrawFrame) return;
            _levelDrawRun = f == _levelDrawFrame + 1 ? _levelDrawRun + 1 : 1;
            _levelDrawFrame = f;
            _levelGates = gates;
        }

        // Null when the home splash is idle and LEVEL would take a real press. Otherwise
        // the first thing in the way, for the timeout log.
        string PressShotBlocker()
        {
            if (WarmCoverLive) return StudioCardHold ? "studio-card" : "warm-cover-fading";
            if (!_splash) return "not-splash";
            if (_home != HomeFace.Splash) return "home-face-" + _home;
            if (Ads.IsShowing) return "ad-showing";
            if (AdPause.Active) return "ad-pause";
            if (GamePause.Paused) return "game-paused";
            if (VipOffer.IsOpen) return "vip-offer";
            if (_dailyOpen) return "daily-card";
            if (_dailyAskOpen) return "daily-ask";
            if (_welcomeOpen) return "welcome-card";
            if (_adoptLive) return "adopt";
            if (_settingsOpen) return "settings";
            if (_avatarRename) return "avatar-rename";
            if (_streakSlide >= 0f) return "streak-sign";
            if (RewardPayBusy()) return "reward-pay";
            if (HomeTutorLive()) return "home-lesson";
            if (TutorialGuideLive()) return "tutorial-guide";
            if (LeafCaptionLive()) return "caption";
            if (BadgerOwed() > 0) return "badger-owed";
            if (LevelCurtain.Armed) return "curtain-armed";
            if (Time.frameCount - _levelDrawFrame > 2) return "level-not-drawn";
            if (_levelDrawRun < 4) return "level-drawn-" + _levelDrawRun + "-frames";
            var g = _levelGates;
            if (g.Consent) return "gate-consent";
            if (g.StepGate) return "gate-step";
            if (g.AdoptQueue) return "gate-adopt-queue";
            if (g.RailLesson) return "gate-glove";
            if (g.LessonGuard) return "gate-lesson-guard";
            if (g.ShowHolds) return "gate-badger-show";
            if (g.ResumeSwallow) return "gate-resume";
            if (g.CoachAte) return "gate-coach";
            if (g.HomeTaken) return "gate-home-taken";
            if (SplashPress.Blocks(g)) return "gate-other";
            return null;
        }

        // Session-only dismissals through the game's own close paths. Nothing here
        // writes a consent / ATT / reminder choice or skips a lesson. The streak sign
        // pays out exactly as it does when LEVEL is pressed under it.
        void PressShotClearSoft(string why)
        {
            if (why == null) return;
            if (why == "gate-consent") AdConsent.DismissTransient();
            else if (why == "vip-offer") VipOffer.Close();
            else if (why == "welcome-card") CloseWelcome();
            else if (why == "settings") _settingsOpen = false;
            else if (why == "avatar-rename") CloseAvatarRename();
            else if (why == "streak-sign") DismissStreakSign();
        }

        // Waits for an idle, interactive home splash (30 s cap), settles 0.5 s, then
        // records 1 s idle, a 100 ms press, release (Load fires that frame) and 2.4 s on.
        // Time.captureFramerate = 60 steps the clock one 60 fps frame per capture.
        // Device flags: /tmp/flock-five-press-shot (iPhone 1179x2556),
        // /tmp/flock-five-press-shot-ipad (2048x2732), /tmp/flock-five-press-shot-android
        // (1080x2400). Any mix may be set; they run in that order in one session.
        static readonly string[] PressShotTags = { "iphone", "ipad", "android" };
        static readonly string[] PressShotFlags = {
            "/tmp/flock-five-press-shot", "/tmp/flock-five-press-shot-ipad", "/tmp/flock-five-press-shot-android" };
        static readonly int[][] PressShotSizes = { new[] { 1179, 2556 }, new[] { 2048, 2732 }, new[] { 1080, 2400 } };

#if UNITY_EDITOR
        void TryStartPressShot()
        {
            if (PressShotLive) return;
            var run = new List<int>();
            for (int d = 0; d < PressShotFlags.Length; d++)
            {
                if (!System.IO.File.Exists(PressShotFlags[d])) continue;
                try { System.IO.File.Delete(PressShotFlags[d]); } catch { }
                run.Add(d);
            }
            if (run.Count == 0) return;
            _dailyShotQuiet = true;
            PressShotLive = true;
            StartCoroutine(ShotSplashPressDevices(run));
        }
#else
        void TryStartPressShot() { }
#endif
#if UNITY_EDITOR

        IEnumerator ShotSplashPressDevices(List<int> run)
        {
            int keepW = Screen.width, keepH = Screen.height;
            float? keepTest = DeviceFit.TestWindow;
            for (int r = 0; r < run.Count; r++)
            {
                int d = run[r];
                int w = PressShotSizes[d][0], h = PressShotSizes[d][1];
                PressShotLive = true;
                DeviceFit.TestWindow = w / (float)h;
#if UNITY_EDITOR
                if (EditorGameSize != null) EditorGameSize(w, h); else Screen.SetResolution(w, h, false);
#else
                Screen.SetResolution(w, h, false);
#endif
                yield return new WaitForSecondsRealtime(1.2f);
                Debug.Log("FF_PRESS_SHOT_DEVICE " + PressShotTags[d] + " want=" + w + "x" + h
                    + " got=" + Screen.width + "x" + Screen.height);
                yield return ShotSplashPress(PressShotTags[d]);
            }
            DeviceFit.TestWindow = keepTest;
#if UNITY_EDITOR
            if (EditorGameSize != null) EditorGameSize(keepW, keepH); else Screen.SetResolution(keepW, keepH, false);
#else
            Screen.SetResolution(keepW, keepH, false);
#endif
            yield return new WaitForSecondsRealtime(0.4f);
            Debug.Log("FF_PRESS_SHOT_ALL_DONE devices=" + run.Count);
            PressShotLive = false;
        }

#endif
        static bool _shotCover, _shotSwap, _shotReveal, _shotGarden;

#if UNITY_EDITOR
        IEnumerator ShotSplashPress(string tag)
        {
            const string dir = "/tmp/paradice";
            string pre = dir + "/splash-press-" + tag + "-";
            string frames = pre + "60fps";
            PressShotLive = true;
            _dailyShotQuiet = true;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPaused = false;
#endif
            Application.runInBackground = true;
            _pressShotHeld = false;
            _pressShotFire = false;
            int keepRate = Time.captureFramerate;
            bool hadNext = PlayerPrefs.HasKey("flockfive.next");
            int keepNext = PlayerPrefs.GetInt("flockfive.next", 0);

            float t0 = Time.realtimeSinceStartup;
            float clearSince = -1f;
            string why = "start";
            string lastWhy = null;
            while (true)
            {
                yield return null;
                why = PressShotBlocker();
                if (why != lastWhy)
                {
                    Debug.Log("FF_PRESS_SHOT_WAIT " + (why ?? "clear") + " t=" + (Time.realtimeSinceStartup - t0).ToString("0.00"));
                    lastWhy = why;
                }
                if (why != null)
                {
                    clearSince = -1f;
                    PressShotClearSoft(why);
                }
                else if (clearSince < 0f) clearSince = Time.realtimeSinceStartup;
                else if (Time.realtimeSinceStartup - clearSince >= 0.5f) break;
                if (Time.realtimeSinceStartup - t0 > 30f)
                {
                    Debug.Log("FF_PRESS_SHOT_TIMEOUT blocker=" + (why ?? "settling") + " after=30s");
                    yield break;
                }
            }

            System.IO.Directory.CreateDirectory(dir);
            try { if (System.IO.Directory.Exists(frames)) System.IO.Directory.Delete(frames, true); } catch { }
            System.IO.Directory.CreateDirectory(frames);
            Debug.Log("FF_PRESS_SHOT_START waited=" + (Time.realtimeSinceStartup - t0).ToString("0.00") + "s");
            Time.captureFramerate = 60;
            int f = 0;
            for (int i = 0; i < 60; i++)
            {
                yield return new WaitForEndOfFrame();
                SavePressShotFrame(frames, f++, i == 30 ? pre + "0-idle.png" : null);
            }
            string late = PressShotBlocker();
            if (late != null) Debug.Log("FF_PRESS_SHOT_WARN blocker before press=" + late);
            _pressShotHeld = true;
            for (int i = 0; i < 6; i++)
            {
                yield return new WaitForEndOfFrame();
                SavePressShotFrame(frames, f++, i == 3 ? pre + "1-press.png" : null);
            }
            bool armed = LevelCurtain.Armed;
            _pressShotHeld = false;
            _pressShotFire = true;
            LevelCurtain.DrawMsSum = 0.0;
            LevelCurtain.DrawMsMax = 0.0;
            LevelCurtain.DrawCount = 0;
            double gapSum = 0.0, gapMax = 0.0;
            int gapN = 0;
            float lastAfterCapture = -1f;
            _shotCover = _shotSwap = _shotReveal = _shotGarden = false;
            for (int k = 0; k < 144; k++)
            {
                yield return new WaitForEndOfFrame();
                // Frame time without the capture: from the end of the last save to here.
                float now = Time.realtimeSinceStartup;
                if (lastAfterCapture >= 0f && k <= 60)
                {
                    double ms = (now - lastAfterCapture) * 1000.0;
                    gapSum += ms;
                    if (ms > gapMax) gapMax = ms;
                    gapN++;
                }
                if (k == 0)
                    Debug.Log("FF_PRESS_SHOT_PRESS armedOnHold=" + armed + " loadFired=" + LevelCurtain.LoadStarted
                        + " fired=" + (LevelCurtain.FiredAt >= 0f) + " splash=" + _splash + " shotFireLeft=" + _pressShotFire);
                // Key stills by the rainbow's own clock (frames since Fire).
                float ra = PressRainbow.Age();
                bool cover = !_shotCover && ra >= PressRainbow.CoverAt + 0.02f;
                bool swap = !_shotSwap && PressRainbow.SwapFrame >= 0;
                bool reveal = !_shotReveal && ra >= (PressRainbow.RevealAt + PressRainbow.RevealEnd) * 0.5f;
                bool garden = !_shotGarden && (ra < 0f || ra >= PressRainbow.Life + 0.05f) && _shotSwap;
                string still = k == 5 ? pre + "2-burst-peak.png"
                    : k == 24 ? pre + "3-handoff.png"
                    : cover ? pre + "coverage.png"
                    : swap ? pre + "swap.png"
                    : reveal ? pre + "reveal.png"
                    : garden ? pre + "garden.png"
                    : null;
                SavePressShotFrame(frames, f++, still, cover, out float rMin, out float rMax);
                if (cover)
                {
                    _shotCover = true;
                    var ci2 = System.Globalization.CultureInfo.InvariantCulture;
                    Debug.Log("FF_RAINBOW_MASK coverage=" + (1f - (1f - PressRainbow.LastVeil) * 0.45f).ToString("0.00", ci2)
                        + " rowLumaMin=" + rMin.ToString("0.0", ci2) + " rowLumaMax=" + rMax.ToString("0.0", ci2)
                        + " rowSpread=" + (rMax > 0f ? (rMax - rMin) / rMax : 0f).ToString("0.000", ci2)
                        + " swapFrame=" + PressRainbow.SwapFrame + " t=" + ra.ToString("0.000", ci2) + " device=" + tag);
                }
                if (swap)
                {
                    _shotSwap = true;
                    Debug.Log("FF_RAINBOW_MASK swapFrame=" + PressRainbow.SwapFrame + " shotFrame=" + Time.frameCount
                        + " heldFaceShown=" + (PressRainbow.SwapFrame >= 0) + " device=" + tag);
                }
                if (reveal) _shotReveal = true;
                if (garden) _shotGarden = true;
                lastAfterCapture = Time.realtimeSinceStartup;
                if (k == 60)
                {
                    int n = LevelCurtain.DrawCount;
                    var ci = System.Globalization.CultureInfo.InvariantCulture;
                    Debug.Log("FF_PRESS_SHOT_PERF burst=1.0s curtainDrawAvgMs=" + (n > 0 ? LevelCurtain.DrawMsSum / n : 0.0).ToString("0.00", ci)
                        + " curtainDrawMaxMs=" + LevelCurtain.DrawMsMax.ToString("0.00", ci) + " draws=" + n
                        + " frameAvgMs=" + (gapN > 0 ? gapSum / gapN : 0.0).ToString("0.00", ci)
                        + " frameMaxMs=" + gapMax.ToString("0.00", ci)
                        + " (frame time excludes capture; editor/batch, not device)");
                }
            }
            Time.captureFramerate = keepRate;
            _pressShotHeld = false;
            _pressShotFire = false;
            EncodeSmashVideo(frames, pre + "60fps.mp4", 60f);
            yield return new WaitForSecondsRealtime(0.4f);
            LevelCurtain.Reset();
            ShowSplash();
            _home = HomeFace.Splash;
            if (hadNext) PlayerPrefs.SetInt("flockfive.next", keepNext);
            else PlayerPrefs.DeleteKey("flockfive.next");
            PlayerPrefs.Save();
            Debug.Log("FF_PRESS_SHOT_DONE device=" + tag + " frames=" + f);
        }
#endif
    }
}
