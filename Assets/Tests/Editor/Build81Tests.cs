#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 81. The home gear draws at 0.75 of the build-80 size and sits in
    // the safe-area top-left. The hit stays at least 44pt, anchored on that
    // corner, clear of the wordmark and off the title's vertical center.
    // Pass 2: Settings opens and closes on a four-phase machine. The cog
    // stays up, and while the sheet moves it paints above the dim.
    static class Build81Tests
    {
        const float W = 1179f;
        const float H = 2556f;

        [MenuItem("Flock Five/Build 81 Tests")]
        public static void Run()
        {
            int pass = 0;
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++;
                else fail++;
                Debug.Log("[build81] " + (ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            // iPhone 15 Pro safe area: 177px top (island), 102px bottom (home indicator).
            var safe = new Rect(0f, 102f, W, H - 102f - 177f);
            float pt = FlockFiveApp.SettingsGearPxPerPt(H);
            float b80 = FlockFiveApp.SettingsGearBuild80Size(H);
            var drawn = FlockFiveApp.SettingsGearDrawnRect(W, H, safe);
            var hit = FlockFiveApp.SettingsGearRect(W, H, safe);
            float ratio = b80 > 1f ? drawn.width / b80 : 0f;
            float marginPt = pt > 0f ? (drawn.x - safe.xMin) / pt : 0f;
            float safeTop = H - safe.yMax;

            Check("gear-drawn-075",
                drawn.width == drawn.height
                && ratio >= 0.70f && ratio <= 0.80f
                && Mathf.Abs(ratio - 0.75f) < 0.02f
                && Mathf.Abs(ratio - FlockFiveApp.SettingsGearShrink) < 0.001f
                && Mathf.Abs(FlockFiveApp.SettingsGearGrow - 1.3f) < 0.001f,
                "drawn=" + drawn + " b80=" + b80.ToString("0.0") + " ratio=" + ratio.ToString("0.00"));

            Check("gear-hit-44pt",
                hit.width >= 44f * pt - 0.01f
                && hit.height >= 44f * pt - 0.01f
                && hit.width == hit.height
                && hit.width + 0.01f >= drawn.width
                && Mathf.Abs(hit.x - drawn.x) < 0.01f
                && Mathf.Abs(hit.y - drawn.y) < 0.01f,
                "hit=" + hit + " min=" + (44f * pt).ToString("0.0") + " drawn=" + drawn.width.ToString("0.0"));

            Check("gear-safe-corner",
                FlockFiveApp.SettingsGearMarginPt >= 8f
                && FlockFiveApp.SettingsGearMarginPt <= 12f
                && marginPt >= 8f && marginPt <= 12f
                && Mathf.Abs(drawn.y - (safeTop + marginPt * pt)) < 0.05f
                && Mathf.Abs(hit.x - drawn.x) < 0.01f
                && Mathf.Abs(hit.y - drawn.y) < 0.01f
                && drawn.y >= safeTop - 0.01f
                && drawn.x >= safe.xMin - 0.01f,
                "origin=" + drawn.x.ToString("0.0") + "," + drawn.y.ToString("0.0")
                + " safeTop=" + safeTop.ToString("0.0") + " marginPt=" + marginPt.ToString("0.0"));

            // Inset safe area, so the anchor is the safe corner and not the screen corner.
            var shifted = new Rect(36f, 102f, W - 36f - 18f, H - 102f - 177f);
            var shiftedDrawn = FlockFiveApp.SettingsGearDrawnRect(W, H, shifted);
            float shiftTop = H - shifted.yMax;
            float m = FlockFiveApp.SettingsGearMargin(H);
            Check("gear-follows-safe-origin",
                Mathf.Abs(shiftedDrawn.x - (shifted.xMin + m)) < 0.05f
                && Mathf.Abs(shiftedDrawn.y - (shiftTop + m)) < 0.05f,
                "shifted=" + shiftedDrawn + " margin=" + m.ToString("0.0"));

            float s = Mathf.Max(H / 720f, 1f);
            float inset = Mathf.Max(0f, H - safe.yMax);
            float titleTop = Mathf.Max(12f, inset + 10f * s);
            float fit = s * 0.91f;
            float cap = 56f * fit;
            float flockMid = titleTop + cap * 0.5f;
            float blockH = cap * 2f + 4f * fit;
            float blockMid = titleTop + blockH * 0.5f;
            // Build 80 sat the gear on the title: TopHudBox + 2*s, center on FLOCK.
            float oldY = titleTop + 2f * s;
            Check("gear-not-on-title",
                Mathf.Abs(drawn.center.y - flockMid) > 20f
                && Mathf.Abs(drawn.center.y - blockMid) > 20f
                && Mathf.Abs(drawn.y - oldY) > 8f,
                "gearMid=" + drawn.center.y.ToString("0.0")
                + " flockMid=" + flockMid.ToString("0.0")
                + " blockMid=" + blockMid.ToString("0.0")
                + " oldY=" + oldY.ToString("0.0"));

            float logoW = W * 0.72f * 0.91f;
            float logoLeft = (W - logoW) * 0.5f;
            var word = new Rect(logoLeft, titleTop, logoW, blockH);
            Check("gear-clears-wordmark",
                !drawn.Overlaps(word)
                && !hit.Overlaps(word)
                && drawn.xMax < logoLeft - 4f
                && hit.xMax < logoLeft - 4f,
                "drawn=" + drawn + " hit=" + hit + " word=" + word);

            // A short screen draws under 44pt, so the hit grows past the icon
            // and stays glued to the same corner.
            const float shortH = 600f;
            var shortSafe = new Rect(0f, 20f, 400f, shortH - 40f);
            var shortDrawn = FlockFiveApp.SettingsGearDrawnRect(400f, shortH, shortSafe);
            var shortHit = FlockFiveApp.SettingsGearRect(400f, shortH, shortSafe);
            Check("gear-hit-extends",
                shortDrawn.width + 1f < FlockFiveApp.SettingsGearMinHit(shortH)
                && shortHit.width >= FlockFiveApp.SettingsGearMinHit(shortH) - 0.01f
                && shortHit.width > shortDrawn.width + 1f
                && Mathf.Abs(shortHit.x - shortDrawn.x) < 0.01f
                && Mathf.Abs(shortHit.y - shortDrawn.y) < 0.01f,
                "shortDrawn=" + shortDrawn + " shortHit=" + shortHit);

            var home = new HomeGearQuery { Home = true, Cover = HomeCover.Welcome, ForcedTap = false };
            var away = new HomeGearQuery { Home = false, Cover = HomeCover.None, ForcedTap = false };
            Check("gear-stays-on-home",
                HomeGear.Shown(home) && HomeGear.AboveDimmer(home) && HomeGear.AcceptsTap(home)
                && !HomeGear.Shown(away) && !HomeGear.AboveDimmer(away),
                "home shown");

            CheckSettingsMotion(Check);

            Debug.Log("[build81] " + (fail == 0 ? "BUILD81_OK " + pass : "BUILD81_FAIL " + fail + " passed " + pass));
            if (fail > 0)
                throw new System.Exception("BUILD81_FAIL " + fail + " passed " + pass);
        }

        static void CheckSettingsMotion(System.Action<string, bool, string> Check)
        {
            float step = 360f / GearIcon.Teeth;
            float spinMod = SettingsMotion.SpinDegrees % step;
            Check("settings-spin-band",
                SettingsMotion.OpenSeconds >= 0.35f && SettingsMotion.OpenSeconds <= 0.45f
                && SettingsMotion.CloseSeconds >= 0.25f && SettingsMotion.CloseSeconds <= 0.35f
                && SettingsMotion.OverlayBlack >= 0.55f && SettingsMotion.OverlayBlack <= 0.65f
                && SettingsMotion.SpinDegrees >= 180f && SettingsMotion.SpinDegrees <= 360f
                && spinMod < 0.01f,
                "open=" + SettingsMotion.OpenSeconds.ToString("0.00")
                + " close=" + SettingsMotion.CloseSeconds.ToString("0.00")
                + " dim=" + SettingsMotion.OverlayBlack.ToString("0.00")
                + " spin=" + SettingsMotion.SpinDegrees.ToString("0"));

            var covers = new[]
            {
                HomeCover.None,
                HomeCover.Settings,
                HomeCover.Welcome,
                HomeCover.Vip,
                HomeCover.DailyCard,
                HomeCover.Streak,
                HomeCover.Settings | HomeCover.Welcome | HomeCover.Vip | HomeCover.DailyCard
            };
            bool gearUp = true;
            string gearDetail = "";
            var phases = (SettingsPhase[])System.Enum.GetValues(typeof(SettingsPhase));
            for (int c = 0; c < covers.Length && gearUp; c++)
            {
                for (int p = 0; p < phases.Length; p++)
                {
                    var q = new HomeGearQuery { Home = true, Cover = covers[c], ForcedTap = false };
                    var forced = q;
                    forced.ForcedTap = true;
                    var pass = SettingsMotion.GearPassFor(true, phases[p]);
                    var want = phases[p] == SettingsPhase.Closed
                        ? SettingsMotion.GearPass.AboveDimmer
                        : SettingsMotion.GearPass.AboveOverlay;
                    if (!HomeGear.Shown(q) || !HomeGear.AboveDimmer(q) || !HomeGear.AcceptsTap(q)
                        || !HomeGear.Shown(forced) || !HomeGear.AboveDimmer(forced) || HomeGear.AcceptsTap(forced)
                        || pass != want
                        || SettingsMotion.GearPassFor(false, phases[p]) != SettingsMotion.GearPass.Hidden)
                    {
                        gearUp = false;
                        gearDetail = covers[c] + " " + phases[p] + " pass=" + pass;
                        break;
                    }
                }
            }
            Check("gear-visible-above-overlay", gearUp, gearDetail.Length == 0 ? "all home phases" : gearDetail);

            var phase = SettingsPhase.Closed;
            bool machine = true;
            phase = SettingsMotion.AfterHit(phase, SettingsMotion.Hit.Gear);
            machine &= phase == SettingsPhase.Opening;
            machine &= SettingsMotion.AfterHit(phase, SettingsMotion.Hit.Gear) == SettingsPhase.Opening;
            machine &= SettingsMotion.AfterHit(phase, SettingsMotion.Hit.Overlay) == SettingsPhase.Opening;
            machine &= SettingsMotion.AfterHit(phase, SettingsMotion.Hit.Close) == SettingsPhase.Opening;
            machine &= SettingsMotion.AfterHit(phase, SettingsMotion.Hit.Sheet) == SettingsPhase.Opening;
            machine &= SettingsMotion.Settled(phase, SettingsMotion.OpenSeconds - 0.02f) == SettingsPhase.Opening;
            phase = SettingsMotion.Settled(phase, SettingsMotion.OpenSeconds);
            machine &= phase == SettingsPhase.Open;
            machine &= SettingsMotion.AfterHit(phase, SettingsMotion.Hit.Sheet) == SettingsPhase.Open;
            machine &= SettingsMotion.AfterHit(phase, SettingsMotion.Hit.None) == SettingsPhase.Open;
            var byGear = SettingsMotion.AfterHit(SettingsPhase.Open, SettingsMotion.Hit.Gear);
            var byOverlay = SettingsMotion.AfterHit(SettingsPhase.Open, SettingsMotion.Hit.Overlay);
            var byX = SettingsMotion.AfterHit(SettingsPhase.Open, SettingsMotion.Hit.Close);
            machine &= byGear == SettingsPhase.Closing
                && byOverlay == SettingsPhase.Closing
                && byX == SettingsPhase.Closing;
            phase = byOverlay;
            machine &= SettingsMotion.AfterHit(phase, SettingsMotion.Hit.Gear) == SettingsPhase.Closing;
            machine &= SettingsMotion.AfterHit(phase, SettingsMotion.Hit.Overlay) == SettingsPhase.Closing;
            machine &= SettingsMotion.Settled(phase, SettingsMotion.CloseSeconds - 0.02f) == SettingsPhase.Closing;
            phase = SettingsMotion.Settled(phase, SettingsMotion.CloseSeconds);
            machine &= phase == SettingsPhase.Closed;
            machine &= SettingsMotion.AfterHit(SettingsPhase.Closed, SettingsMotion.Hit.Overlay) == SettingsPhase.Closed;
            machine &= SettingsMotion.BlocksInput(SettingsPhase.Opening)
                && SettingsMotion.BlocksInput(SettingsPhase.Closing)
                && !SettingsMotion.BlocksInput(SettingsPhase.Open)
                && !SettingsMotion.BlocksInput(SettingsPhase.Closed);
            Check("settings-state-machine", machine, "phase walk");

            float travel = 1800f;
            float openU = 0.30f;
            float openEase = PopupMotion.EaseOutCubic(openU);
            float openGear = SettingsMotion.GearDegrees(SettingsPhase.Opening, SettingsMotion.OpenSeconds * openU);
            float openSlide = SettingsMotion.PanelOffset(SettingsPhase.Opening, SettingsMotion.OpenSeconds * openU, travel);
            float closeU = 0.50f;
            float closeEase = PopupMotion.EaseInCubic(closeU);
            float closeGear = SettingsMotion.GearDegrees(SettingsPhase.Closing, SettingsMotion.CloseSeconds * closeU);
            bool openMono = true;
            bool closeMono = true;
            float prevOpen = -1f;
            float prevClose = SettingsMotion.SpinDegrees + 1f;
            for (int i = 0; i <= 8; i++)
            {
                float og = SettingsMotion.GearDegrees(SettingsPhase.Opening, SettingsMotion.OpenSeconds * i / 8f);
                float cg = SettingsMotion.GearDegrees(SettingsPhase.Closing, SettingsMotion.CloseSeconds * i / 8f);
                float oy = SettingsMotion.PanelOffset(SettingsPhase.Opening, SettingsMotion.OpenSeconds * i / 8f, travel);
                if (og + 0.001f < prevOpen || oy > 0.01f) openMono = false;
                if (cg > prevClose + 0.001f) closeMono = false;
                prevOpen = og;
                prevClose = cg;
            }
            float pad = 24f;
            float bottom = 1600f;
            float drop = SettingsMotion.DropTravel(bottom, pad);
            float startBottom = bottom + SettingsMotion.PanelOffset(SettingsPhase.Opening, 0f, drop);
            float endBottom = bottom + SettingsMotion.PanelOffset(SettingsPhase.Opening, SettingsMotion.OpenSeconds, drop);
            float awayBottom = bottom + SettingsMotion.PanelOffset(SettingsPhase.Closing, SettingsMotion.CloseSeconds, drop);
            bool ease = openMono && closeMono
                && openEase > openU + 0.15f
                && Mathf.Abs(openGear / SettingsMotion.SpinDegrees - openEase) < 0.02f
                && Mathf.Abs((-openSlide / travel) - (1f - openEase)) < 0.02f
                && closeEase < closeU - 0.2f
                && Mathf.Abs(closeGear / SettingsMotion.SpinDegrees - (1f - closeEase)) < 0.02f
                && startBottom <= -pad + 0.05f
                && Mathf.Abs(endBottom - bottom) < 0.05f
                && awayBottom <= -pad + 0.05f
                && SettingsMotion.GearDegrees(SettingsPhase.Open, 0f) == 0f
                && SettingsMotion.GearDegrees(SettingsPhase.Closed, 0f) == 0f
                && Mathf.Abs(SettingsMotion.GearDegrees(SettingsPhase.Opening, SettingsMotion.OpenSeconds) - SettingsMotion.SpinDegrees) < 0.01f
                && Mathf.Abs(SettingsMotion.GearDegrees(SettingsPhase.Closing, 0f) - SettingsMotion.SpinDegrees) < 0.01f
                && Mathf.Abs(SettingsMotion.OverlayAlpha(SettingsPhase.Open, 0f) - SettingsMotion.OverlayBlack) < 0.001f
                && SettingsMotion.OverlayAlpha(SettingsPhase.Opening, 0f) < 0.01f
                && SettingsMotion.OverlayAlpha(SettingsPhase.Closing, SettingsMotion.CloseSeconds) < 0.01f
                && SettingsMotion.OverlayAlpha(SettingsPhase.Closed, 1f) == 0f
                && SettingsMotion.PanelOffset(SettingsPhase.Open, 0f, travel) == 0f;
            Check("settings-ease", ease,
                "openEase=" + openEase.ToString("0.00")
                + " closeEase=" + closeEase.ToString("0.00")
                + " startBottom=" + startBottom.ToString("0")
                + " mono=" + openMono + "/" + closeMono);

            string root = UnityEngine.Application.dataPath;
            string settings = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.Settings.cs"));
            string motion = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "_Project/Scripts/App/SettingsSheet.cs"));
            string app = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.cs"));
            int sheet = settings.IndexOf("void DrawSettingsSheet");
            int block = sheet >= 0 ? settings.IndexOf("SettingsMotion.BlocksInput", sheet) : -1;
            int gearHit = sheet >= 0 ? settings.IndexOf("HitPad(gearHit", sheet) : -1;
            int dim = sheet >= 0 ? settings.IndexOf("DrawSettingsDim(", sheet) : -1;
            int wood = sheet >= 0 ? settings.IndexOf("DrawWoodPanel(", sheet) : -1;
            int gearDraw = sheet >= 0 ? settings.IndexOf("DrawSettingsGear(", sheet) : -1;
            int gearFn = settings.IndexOf("void DrawSettingsGear");
            int gearEnd = gearFn >= 0 ? settings.IndexOf("static void SwallowSettingsPointer", gearFn) : -1;
            string gearBody = gearFn >= 0 && gearEnd > gearFn ? settings.Substring(gearFn, gearEnd - gearFn) : "";
            int paint = settings.IndexOf("void PaintHomeGear");
            int paintEnd = paint >= 0 ? settings.IndexOf("bool HomeGearExempt", paint) : -1;
            string paintBody = paint >= 0 && paintEnd > paint ? settings.Substring(paint, paintEnd - paint) : "";
            int update = app.IndexOf("void Update()");
            bool wired = sheet >= 0 && block >= 0 && gearHit > block && dim > 0 && wood > dim && gearDraw > wood
                && paintBody.IndexOf("_settingsOpen) return") >= 0
                && paintBody.IndexOf("GearPassFor") >= 0
                && gearBody.IndexOf("RotateAroundPivot") >= 0
                && gearBody.IndexOf("Color.white") >= 0
                && gearBody.IndexOf("OverlayAlpha") < 0
                && gearBody.IndexOf(", 1f)") >= 0
                && settings.IndexOf("SettingsMotion.AfterHit") >= 0
                && settings.IndexOf("SwallowSettingsPointer") >= 0
                && motion.IndexOf("PopupMotion.EaseOutCubic") >= 0
                && motion.IndexOf("PopupMotion.EaseInCubic") >= 0
                && motion.IndexOf("PopupMotion.Smooth") >= 0
                && motion.IndexOf("PopupMotion.Beat") >= 0
                && app.IndexOf("if (_settingsOpen) DrawSettingsSheet(s);") >= 0
                && update >= 0 && app.IndexOf("TickSettingsMotion()", update) > update
                && app.IndexOf("PoseSettings(") >= 0;
            Check("settings-drawn-and-wired", wired,
                "block=" + block + " gearHit=" + gearHit + " dim=" + dim
                + " wood=" + wood + " gearDraw=" + gearDraw);
        }
    }
}
#endif
