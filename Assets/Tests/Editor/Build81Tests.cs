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
            CheckGearLaunch(Check);
            CheckPass4(Check);
            CheckAvatarKit(Check);
            CheckPassH(Check);

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

        // The home paint used to fill the whole 512² cog on the first frame (~4 s).
        // The draw path may finish at most one row when the bake is hidden.
        static void CheckGearLaunch(System.Action<string, bool, string> Check)
        {
            GearIcon.ResetForTests();
            GearIcon.AllowBaked = false;
            Texture2D sliced = null;
            int rows = -1;
            double sliceMs = 0;
            try
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                sliced = GearIcon.Texture();
                watch.Stop();
                sliceMs = watch.Elapsed.TotalMilliseconds;
                rows = GearIcon.FilledRows;
            }
            finally
            {
                GearIcon.ResetForTests();
            }

            Check("gear-slice-not-sync",
                sliced == null && rows == 1 && sliceMs < 80.0,
                "tex=" + (sliced == null ? "null" : sliced.name)
                + " rows=" + rows
                + " ms=" + sliceMs.ToString("0.0"));

            var baked = AssetDatabase.LoadAssetAtPath<Sprite>(GearBake.AssetPath);
            bool size = baked != null && baked.texture != null
                && baked.texture.width == GearIcon.Resolution
                && baked.texture.height == GearIcon.Resolution;
            GearIcon.ResetForTests();
            var load = System.Diagnostics.Stopwatch.StartNew();
            var shown = GearIcon.Texture();
            load.Stop();
            Check("gear-shows-from-bake",
                size && shown != null && load.Elapsed.TotalMilliseconds < 80.0,
                "sprite=" + (baked != null) + " shown=" + (shown != null)
                + " ms=" + load.Elapsed.TotalMilliseconds.ToString("0.0"));
            GearIcon.ResetForTests();
        }

        static void CheckPass4(System.Action<string, bool, string> Check)
        {
            int bursts = FlockFiveApp.PlaneSkyBursts;
            int last = bursts - 1;
            int whites = 0;
            int blues = 0;
            int hearts = 0;
            bool alt = true;
            bool noLate = true;
            for (int i = 0; i < bursts; i++)
            {
                int k = FlockFiveApp.PlaneSkyKind(i);
                if (k == 0) whites++;
                else if (k == 1) blues++;
                else hearts++;
                if (i < last && k != (i & 1)) alt = false;
                float when = FlockFiveApp.PlaneSkyWhen(i);
                if (when >= FlockFiveApp.PlaneCrossSeconds) noLate = false;
                if (FlockFiveApp.PlaneSkyMayLaunch(i, 0f, true)) noLate = false;
                if (!FlockFiveApp.PlaneSkyMayLaunch(i, when + 0.01f, true)) noLate = false;
                if (FlockFiveApp.PlaneSkyMayLaunch(i, when - 0.05f, true)) noLate = false;
            }
            FlockFiveApp.PlaneRigSize(W, H, 485f / 264f, 724f / 184f, out float planeW, out float totalW);
            float exitU = FlockFiveApp.PlaneRigExitU(W, planeW, totalW);
            float exitT = exitU * FlockFiveApp.PlaneCrossSeconds;
            float fire = FlockFiveApp.PlaneSkyWhen(last) + FlockFiveApp.PlaneSkyRise;
            float life = FlockFiveApp.PlaneSkyHeartLife / FlockFiveApp.PlaneSkyHeartLifeWas;
            Check("plane-no-launch-after-exit",
                noLate && bursts == 7
                && fire < exitT
                && fire < FlockFiveApp.PlaneCrossSeconds
                && FlockFiveApp.PlaneSkyShowEnd(bursts) > FlockFiveApp.PlaneCrossSeconds,
                "fire=" + fire.ToString("0.00")
                + " spriteExit=" + exitT.ToString("0.00")
                + " rigExit=" + FlockFiveApp.PlaneCrossSeconds.ToString("0.00")
                + " show=" + FlockFiveApp.PlaneSkyShowEnd(bursts).ToString("0.00"));
            Check("plane-heart-last",
                hearts == 1 && FlockFiveApp.PlaneSkyKind(last) == 2 && alt,
                "hearts=" + hearts + " last=" + FlockFiveApp.PlaneSkyKind(last));
            Check("plane-white-blue-alt",
                whites == 3 && blues == 3 && whites + blues == last && alt,
                "white=" + whites + " blue=" + blues);
            Check("plane-heart-life",
                life >= 1.5f && life <= 2f
                && FlockFiveApp.PlaneSkyHeartAlpha(0.5f) > FlockFiveApp.PlaneSkyHeartFadeWasAt(0.5f) + 0.25f
                && FlockFiveApp.PlaneSkyHeartDrift > 0f
                && FlockFiveApp.PlaneSkyHeartDrift < FlockFiveApp.PlaneSkyHeartDriftWas,
                "life=" + FlockFiveApp.PlaneSkyHeartLife.ToString("0.00")
                + " was=" + FlockFiveApp.PlaneSkyHeartLifeWas.ToString("0.00")
                + " ratio=" + life.ToString("0.00")
                + " fade=" + FlockFiveApp.PlaneSkyHeartAlpha(0.5f).ToString("0.00")
                + " drift=" + FlockFiveApp.PlaneSkyHeartDrift.ToString("0.00"));

            string root = Application.dataPath;
            string daily = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.DailyBonus.cs"));
            string app = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.cs"));
            int ptr = daily.IndexOf("void DrawWheelPointer(");
            int ptrEnd = daily.IndexOf("public static float WheelPointerPinFrac(", ptr);
            string ptrBody = ptr >= 0 && ptrEnd > ptr ? daily.Substring(ptr, ptrEnd - ptr) : "";
            int wheelFn = daily.IndexOf("void DrawDailyWheel(");
            int wheelEnd = daily.IndexOf("void DrawWheelPointer(", wheelFn);
            string wheelBody = wheelFn >= 0 && wheelEnd > wheelFn ? daily.Substring(wheelFn, wheelEnd - wheelFn) : "";
            int draws = CountOf(ptrBody, "GUI.DrawTexture(");
            int calls = CountOf(daily, "DrawWheelPointer(");
            var px = FlockFiveApp.WheelPointerPixels(out int pw, out int ph);
            int parts = OpaqueParts(px, pw, ph, 90);
            bool cornerClear = CornerClear(px, pw, ph);
            Check("pointer-one-draw",
                draws == 1 && calls == 2
                && wheelBody.IndexOf("DrawWheelPointer(") >= 0
                && wheelBody.IndexOf("_wheelPointer") < 0
                && parts == 1
                && cornerClear
                && FlockFiveApp.PointerPointsDown(px, pw, ph),
                "draws=" + draws + " calls=" + calls + " parts=" + parts + " clear=" + cornerClear);

            bool ticks = true;
            string tickDetail = "";
            for (int w = 0; w < DailyBonus.WedgeCount; w++)
            {
                float to = DailyBonus.PointerAngle(w) + 360f * 5f;
                int n = DailyBonus.BoundaryCrossings(0f, to);
                int expect = Mathf.RoundToInt(Mathf.Abs(to) / DailyBonus.WedgeArc);
                int sum = 0;
                float a = 0f;
                while (a < to - 0.0001f)
                {
                    float b = Mathf.Min(to, a + 10f);
                    sum += DailyBonus.BoundaryCrossings(a, b);
                    a = b;
                }
                if (n != expect || sum != n)
                {
                    ticks = false;
                    tickDetail = "w=" + w + " n=" + n + " expect=" + expect + " sum=" + sum;
                    break;
                }
            }
            int tickFn = daily.IndexOf("void TickWheelClicks(");
            int tickEnd = daily.IndexOf("void StartDailySpin(", tickFn);
            string tickBody = tickFn >= 0 && tickEnd > tickFn ? daily.Substring(tickFn, tickEnd - tickFn) : "";
            float flick = DailyBonus.PointerFlick(DailyBonus.PointerFlickSeconds * 0.5f, -1f);
            float back = DailyBonus.PointerFlick(DailyBonus.PointerFlickSeconds * 0.5f, 1f);
            Check("pointer-tick-per-boundary",
                ticks
                && tickBody.IndexOf("BoundaryCrossings") >= 0
                && tickBody.IndexOf("Play(\"tick\", 0.22f, 0f)") >= 0
                && Mathf.Abs(DailyBonus.PointerFlick(0f, -1f)) < 0.001f
                && Mathf.Abs(DailyBonus.PointerFlick(DailyBonus.PointerFlickSeconds, -1f)) < 0.001f
                && flick < -1f && back > 1f,
                tickDetail.Length == 0 ? "flick=" + flick.ToString("0.00") : tickDetail);

            bool radial = true;
            bool upright = true;
            float worst = 0f;
            string radialDetail = "";
            for (int w = 0; w < DailyBonus.WedgeCount; w++)
            {
                float land = DailyBonus.PointerAngle(w);
                float[] spins = { 0f, land, land + 360f * 5f };
                for (int i = 0; i < spins.Length; i++)
                {
                    float spin = spins[i];
                    float theta = DailyBonus.SignedDegrees(DailyBonus.WedgeLabelDegrees(w, spin));
                    if (theta <= -90f || theta > 90f)
                    {
                        upright = false;
                        if (radialDetail.Length == 0)
                            radialDetail = "w=" + w + " spin=" + spin.ToString("0.0") + " theta=" + theta.ToString("0.0");
                    }
                    var want = DailyBonus.WedgeOffset(w, spin, 1f);
                    var got = DailyBonus.RotateClockwise(new Vector2(1f, 0f), DailyBonus.WedgeLabelDegrees(w, spin));
                    float dot = want.x * got.x + want.y * got.y;
                    float err = Mathf.Abs(Mathf.Abs(dot) - 1f);
                    if (err > worst) worst = err;
                    if (err > 0.02f)
                    {
                        radial = false;
                        if (radialDetail.Length == 0)
                            radialDetail = "w=" + w + " dot=" + dot.ToString("0.000");
                    }
                }
            }
            Check("wedge-labels-radial",
                radial && upright && wheelBody.IndexOf("WedgeLabelDegrees") >= 0,
                radialDetail.Length == 0 ? "worst=" + worst.ToString("0.000") : radialDetail);

            var face = DailyBonus.WedgeFace(DailyBonus.JackpotWedge, 0.45f);
            var other = DailyBonus.WedgeFace(0, 0.45f);
            var disc = FlockFiveApp.WheelDiscPixels(256);
            int stars = 0;
            var star = (Color32)DailyBonus.JackpotStar;
            for (int i = 0; i < disc.Length; i++)
            {
                var c = disc[i];
                if (c.a < 200) continue;
                if (Mathf.Abs(c.r - star.r) <= 2 && Mathf.Abs(c.g - star.g) <= 2 && Mathf.Abs(c.b - star.b) <= 2)
                    stars++;
            }
            bool ruby = face.r > face.g + 0.25f && face.r > face.b + 0.25f && face.r > 0.45f;
            bool notRuby = other.r < other.g + 0.15f || other.g > 0.25f;
            Check("jackpot-stands-out",
                ruby && notRuby && stars >= 12 && stars < 800
                && daily.IndexOf("WedgeFace(") >= 0
                && daily.IndexOf("JackpotStar") >= 0
                && daily.IndexOf("JackpotTrim") >= 0
                && wheelBody.IndexOf("_dailyJackpotPx") >= 0,
                "face=" + face + " other=" + other + " stars=" + stars);

            int helper = daily.IndexOf("void DrawWheelX2(");
            int claim = daily.IndexOf("void DrawDailyClaim(");
            string helperBody = helper >= 0 && claim > helper ? daily.Substring(helper, claim - helper) : "";
            int stamp = app.IndexOf("void DrawRewardStamp(");
            int wax = app.IndexOf("static void DrawWaxSplat(");
            string stampBody = stamp >= 0 && wax > stamp ? app.Substring(stamp, wax - stamp) : "";
            var panel = new Rect(80f, 200f, 640f, 880f);
            var seat = FlockFiveApp.RewardStampRect(panel, 2);
            bool topRight = seat.center.x > panel.xMax - panel.width * 0.28f
                && seat.center.y < panel.y + panel.height * 0.28f;
            var title = new Rect(120f, 160f, 700f, 80f);
            var wheel = new Rect(140f, 360f, 660f, 660f);
            var onWheel = FlockFiveApp.WheelResultBadgeRect(title, wheel, 2);
            var onPanel = FlockFiveApp.WheelResultBadgeRect(title, new Rect(120f, 160f, 700f, 980f), 2);
            bool clear = !title.Overlaps(onWheel) && !title.Overlaps(onPanel);
            int x2Uses = CountOf(daily, "DrawWheelX2(");
            Check("wheel-x2-stamp",
                topRight
                && clear
                && x2Uses >= 3
                && helperBody.IndexOf("DrawRewardStamp(seat, 2, 1f, age, sparkle, false)") >= 0
                && helperBody.IndexOf("RewardStampRect(") >= 0
                && helperBody.IndexOf("RewardCardShake(") >= 0
                && helperBody.IndexOf("BadgeBelowTitle(") >= 0
                && helperBody.IndexOf("PointerTipDir") < 0
                && stampBody.IndexOf("RewardStampPop") >= 0
                && stampBody.IndexOf("StreakBadge.Draw") >= 0
                && stampBody.IndexOf("if (thud)") >= 0
                && daily.IndexOf("DrawWheelX2(wheelR,") >= 0
                && daily.IndexOf("DrawWheelX2(bar,") >= 0
                && daily.IndexOf("DrawWheelX2(board,") < 0,
                "seat=" + seat + " wheel=" + onWheel + " panel=" + onPanel + " uses=" + x2Uses);
        }

        static int CountOf(string hay, string needle)
        {
            if (string.IsNullOrEmpty(hay) || string.IsNullOrEmpty(needle)) return 0;
            int n = 0;
            int from = 0;
            while (from >= 0)
            {
                int i = hay.IndexOf(needle, from);
                if (i < 0) break;
                n++;
                from = i + needle.Length;
            }
            return n;
        }

        static int OpaqueParts(Color32[] px, int w, int h, byte minA)
        {
            if (px == null || w < 2 || h < 2 || px.Length < w * h) return -1;
            var seen = new bool[w * h];
            int parts = 0;
            var stack = new int[w * h];
            for (int i = 0; i < px.Length; i++)
            {
                if (seen[i] || px[i].a <= minA) continue;
                parts++;
                int n = 0;
                stack[n++] = i;
                seen[i] = true;
                while (n > 0)
                {
                    int k = stack[--n];
                    int x = k % w;
                    int y = k / w;
                    if (x > 0) Push(px, seen, stack, ref n, k - 1, minA);
                    if (x + 1 < w) Push(px, seen, stack, ref n, k + 1, minA);
                    if (y > 0) Push(px, seen, stack, ref n, k - w, minA);
                    if (y + 1 < h) Push(px, seen, stack, ref n, k + w, minA);
                }
            }
            return parts;
        }

        static void Push(Color32[] px, bool[] seen, int[] stack, ref int n, int i, byte minA)
        {
            if (seen[i] || px[i].a <= minA) return;
            seen[i] = true;
            stack[n++] = i;
        }

        static bool CornerClear(Color32[] px, int w, int h)
        {
            int n = 10;
            for (int y = h - n; y < h; y++)
            {
                for (int x = w - n; x < w; x++)
                    if (px[y * w + x].a > 20) return false;
                for (int x = 0; x < n; x++)
                    if (px[y * w + x].a > 20) return false;
            }
            return true;
        }

        // Home avatar grew. Every bow and crown (and the other head kits) takes
        // its scale from the measured skull, so the home bird and the garden
        // birds keep one proportion. The home bird must clear a readable size.
        static void CheckAvatarKit(System.Action<string, bool, string> Check)
        {
            const float screenW = 1179f;
            const float screenH = 2556f;
            var colors = new[] { BirdColor.Ruby, BirdColor.Gold, BirdColor.Teal, BirdColor.Violet };
            var body = SpriteCatalog.Bird(BirdColor.Teal);
            bool bodyOk = body != null && body.texture != null && body.texture.isReadable;
            float head = bodyOk ? SpriteCatalog.HeadSpan(body) : 0f;
            float homePx = FlockFiveApp.HomeAvatarBirdPx(screenH);
            float world = bodyOk && body.pixelsPerUnit > 1f ? body.rect.width / body.pixelsPerUnit : 0f;
            float gardenPx = FlockFiveApp.GardenBirdPx(screenW, screenH, world);
            var birdBox = SpriteCatalog.MeasureOpaque(body);
            Check("kit-head-measured",
                bodyOk && head > 0.4f && head < 1.6f && homePx > gardenPx && gardenPx > 8f,
                "head=" + head.ToString("0.000") + " home=" + homePx.ToString("0") + " garden=" + gardenPx.ToString("0"));

            bool sameSkull = bodyOk;
            string skullDetail = "";
            for (int i = 0; i < colors.Length; i++)
            {
                var b = SpriteCatalog.Bird(colors[i]);
                float h = b != null ? SpriteCatalog.HeadSpan(b) : 0f;
                if (b == null || Mathf.Abs(h - head) > 0.04f)
                {
                    sameSkull = false;
                    skullDetail = colors[i] + " " + h.ToString("0.000");
                    break;
                }
            }
            Check("kit-head-shared-silhouette", sameSkull, skullDetail.Length == 0 ? "span=" + head.ToString("0.000") : skullDetail);

            float twice = BirdIdle.KitFit(head * 2f, 2f, BirdKit.Bow);
            float once = BirdIdle.KitFit(head, 2f, BirdKit.Bow);
            Check("kit-scale-tracks-head",
                head > 0.2f && once > 0.05f && Mathf.Abs(twice / once - 2f) < 0.001f,
                "once=" + once.ToString("0.000") + " twice=" + twice.ToString("0.000"));

            var worn = new[]
            {
                BirdKit.Bow, BirdKit.Bowtie, BirdKit.Crown,
                BirdKit.TopHat, BirdKit.Flower, BirdKit.Shades, BirdKit.Beanie
            };
            bool proportion = true;
            bool readable = true;
            bool gardenOk = true;
            bool onDome = true;
            string proportionDetail = "";
            string readableDetail = "";
            string gardenDetail = "";
            string domeDetail = "";
            for (int i = 0; i < worn.Length; i++)
            {
                var kitKind = worn[i];
                int colorsN = kitKind == BirdKit.Crown ? 5 : 1;
                for (int c = 0; c < colorsN; c++)
                {
                    var col = (BirdColor)c;
                    var kitBody = SpriteCatalog.Bird(col);
                    if (kitBody == null) kitBody = body;
                    var kit = SpriteCatalog.KitSprite(col, kitKind);
                    if (kit == null || kitBody == null)
                    {
                        proportion = false;
                        if (proportionDetail.Length == 0) proportionDetail = kitKind + " " + col + " missing";
                        continue;
                    }
                    float span = SpriteCatalog.HeadSpan(kitBody);
                    var box = SpriteCatalog.MeasureOpaque(kit);
                    BirdIdle.KitAnchor(kitKind, 0, false, kitBody, kit, out float x, out float y, out float scale, out float tilt, out bool flip);
                    float native = box.Ok ? box.Width : 1f;
                    float fit = BirdIdle.KitFit(span, native, kitKind);
                    float gpx = kitBody.pixelsPerUnit > 1f
                        ? FlockFiveApp.GardenBirdPx(screenW, screenH, kitBody.rect.width / kitBody.pixelsPerUnit)
                        : gardenPx;
                    float homeKit = BirdIdle.KitOpaquePx(kitBody, kit, scale, homePx);
                    float gardenKit = BirdIdle.KitOpaquePx(kitBody, kit, scale, gpx);
                    float homeHead = BirdIdle.HeadScreenPx(kitBody, homePx);
                    float gardenHead = BirdIdle.HeadScreenPx(kitBody, gpx);
                    float homeRatio = homeHead > 1f ? homeKit / homeHead : 0f;
                    float gardenRatio = gardenHead > 1f ? gardenKit / gardenHead : 0f;
                    float birdW = BirdIdle.KitOpaquePx(kitBody, kitBody, 1f, gpx);
                    bool bowOrCrown = kitKind == BirdKit.Bow || kitKind == BirdKit.Bowtie || kitKind == BirdKit.Crown;
                    if (proportion && (Mathf.Abs(scale - fit) > 0.002f
                        || Mathf.Abs(homeRatio - BirdIdle.ShareOf(kitKind)) > 0.03f
                        || Mathf.Abs(homeRatio - gardenRatio) > 0.02f
                        || Mathf.Abs(homeKit / Mathf.Max(1f, gardenKit) - homePx / Mathf.Max(1f, gpx)) > 0.03f))
                    {
                        proportion = false;
                        proportionDetail = kitKind + " " + col
                            + " scale=" + scale.ToString("0.000") + " fit=" + fit.ToString("0.000")
                            + " home=" + homeRatio.ToString("0.00") + " garden=" + gardenRatio.ToString("0.00")
                            + " px=" + homeKit.ToString("0") + "/" + gardenKit.ToString("0");
                    }
                    if (readable && bowOrCrown && homeKit + 0.5f < BirdIdle.KitReadableMinPx)
                    {
                        readable = false;
                        readableDetail = kitKind + " " + col + " px=" + homeKit.ToString("0.0");
                    }
                    if (gardenOk && (gardenKit + 1f >= homeKit || (birdW > 1f && gardenKit > birdW * BirdIdle.KitMaxOfBird)))
                    {
                        gardenOk = false;
                        gardenDetail = kitKind + " garden=" + gardenKit.ToString("0")
                            + " home=" + homeKit.ToString("0") + " bird=" + birdW.ToString("0");
                    }
                    var dome = SpriteCatalog.HeadDome(kitBody);
                    float contactY = y;
                    float yLimit = span * 0.22f;
                    var seat = FlockKit.SeatOf(kitKind);
                    if (seat == FlockKit.Seat.Head && box.Ok)
                        contactY = y + box.Bottom * scale;
                    else if (seat == FlockKit.Seat.Face && box.Ok)
                    {
                        contactY = y + box.Top * scale;
                        yLimit = span * 0.55f;
                    }
                    else if (kitKind == BirdKit.Bowtie)
                        yLimit = span * 0.60f;
                    else if (box.Ok)
                        contactY = y + (box.Bottom + box.Top) * 0.5f * scale;
                    float dx = Mathf.Abs(x - dome.x);
                    float dy = Mathf.Abs(contactY - dome.y);
                    BirdIdle.KitAnchor(kitKind, 0, true, kitBody, kit, out float mx, out _, out _, out _, out bool mflip);
                    if (onDome && (dx > span * 0.30f || dy > yLimit || flip || Mathf.Abs(tilt) < 1f
                        || !mflip || Mathf.Abs(mx + x) > 0.02f))
                    {
                        onDome = false;
                        domeDetail = kitKind + " " + col
                            + " anchor=" + x.ToString("0.00") + "," + contactY.ToString("0.00")
                            + " dome=" + dome.x.ToString("0.00") + "," + dome.y.ToString("0.00")
                            + " dx=" + dx.ToString("0.00") + " dy=" + dy.ToString("0.00");
                    }
                }
            }
            Check("kit-scale-proportional", proportion,
                proportionDetail.Length == 0 ? "home and garden share the skull" : proportionDetail);
            Check("kit-home-readable", readable,
                readableDetail.Length == 0 ? "bows and crowns >= " + BirdIdle.KitReadableMinPx.ToString("0") : readableDetail);
            Check("kit-garden-not-oversized", gardenOk,
                gardenDetail.Length == 0 ? "under " + BirdIdle.KitMaxOfBird.ToString("0.00") + " of the body" : gardenDetail);
            Check("kit-anchor-on-dome", onDome,
                domeDetail.Length == 0 ? "seat on the skull" : domeDetail);
        }

        static void CheckPassH(System.Action<string, bool, string> Check)
        {
            int oldFloor = FlockFiveApp.CaptionPxOf(FlockFiveApp.CaptionFloorRef);
            int floor = FlockFiveApp.LessonCaptionFloor(W, H);
            Debug.Log("[build81] caption-floor old=" + oldFloor + " new=" + floor
                + " ref=" + FlockFiveApp.CaptionFloorRef);
            var lines = FlockFiveApp.PaintedLessonLines();
            bool fonts = true;
            bool taps = true;
            string fontDetail = "n=" + lines.Length + " floor=" + floor;
            string tapDetail = "no Click";
            var seen = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i] ?? "";
                if (!seen.Add(line)) continue;
                string shown = FlockFiveApp.LessonCaptionShown(line);
                if (shown.IndexOf("Click", System.StringComparison.Ordinal) >= 0
                    || line.IndexOf("Click", System.StringComparison.Ordinal) >= 0)
                {
                    taps = false;
                    tapDetail = shown;
                }
                int px = FlockFiveApp.LessonCaptionFont(line, W, H);
                if (px < 60 || px > 70)
                {
                    fonts = false;
                    fontDetail = px + " outside 60-70 [" + shown.Replace("\n", " / ") + "]";
                }
            }
            int halfH = 1280;
            int halfFloor = FlockFiveApp.LessonCaptionFloor(W, halfH);
            int halfWant = Mathf.Max(oldFloor, Mathf.RoundToInt(FlockFiveApp.CaptionFloorPhonePx * (halfH / FlockFiveApp.CaptionFloorPhoneH)));
            int shortFloor = FlockFiveApp.LessonCaptionFloor(W, 720f);
            int shortWant = Mathf.Max(oldFloor, Mathf.RoundToInt(FlockFiveApp.CaptionFloorPhonePx * (720f / FlockFiveApp.CaptionFloorPhoneH)));
            var beeShown = FlockFiveApp.LessonCaptionShown(CaptionLayout.HiveGardenLine);
            bool emojiOk = beeShown.Contains("\U0001F41D")
                == CaptionLayout.DrawsEmoji(FlockFiveApp.CoachCaptionStyle().font);
            int phoneWant = Mathf.Max(oldFloor, Mathf.RoundToInt(FlockFiveApp.CaptionFloorPhonePx * (H / FlockFiveApp.CaptionFloorPhoneH)));
            Check("caption-floor",
                floor >= 60 && floor <= 70 && floor == phoneWant
                && halfFloor == halfWant && shortFloor == shortWant
                && fonts && emojiOk,
                "old=" + oldFloor + " new=" + floor + " half=" + halfFloor + "/" + halfWant
                + " short=" + shortFloor + "/" + shortWant + " " + fontDetail
                + " bee=[" + beeShown.Replace("\n", " / ") + "]");
            int beeLines = FlockFiveApp.LessonCaptionLines(CaptionLayout.HiveGardenLine, W, H);
            Check("caption-bee-wrap", beeLines >= 2 && beeLines <= 4,
                "lines=" + beeLines + " [" + beeShown.Replace("\n", " / ") + "]");
            Check("caption-says-tap", taps
                && beeShown.IndexOf("Tap", System.StringComparison.Ordinal) >= 0
                && CaptionLayout.AlbumHoneyBlock.IndexOf("Tap", System.StringComparison.Ordinal) >= 0
                && CaptionLayout.AlbumHoneyBlock.IndexOf("Click", System.StringComparison.Ordinal) < 0,
                tapDetail);

            var pad = CaptionLayout.BoxPad(H);
            Check("caption-pad",
                pad.x + 0.01f >= CaptionLayout.PadXMin && pad.y + 0.01f >= CaptionLayout.PadYMin
                && pad.x > 18f && pad.y > 12f,
                "pad=" + pad.x.ToString("0.0") + "x" + pad.y.ToString("0.0"));

            var safe = new Rect(0f, 102f, W, H - 102f - 177f);
            FlockFiveApp.GardenCorners(W, H, safe, out var restart, out var hive);
            float pt = H / 852f;
            float left = restart.xMin - safe.xMin;
            float right = safe.xMax - hive.xMax;
            Check("hud-mirror",
                Mathf.Abs(left - right) <= pt
                && Mathf.Abs(restart.center.y - hive.center.y) <= pt
                && Mathf.Abs(restart.width - hive.width) <= pt
                && restart.width > 8f,
                "left=" + left.ToString("0.0") + " right=" + right.ToString("0.0")
                + " cy=" + restart.center.y.ToString("0.0") + "/" + hive.center.y.ToString("0.0")
                + " size=" + restart.width.ToString("0.0") + "/" + hive.width.ToString("0.0"));

            var shifted = new Rect(36f, 102f, W - 36f - 18f, H - 102f - 177f);
            FlockFiveApp.GardenCorners(W, H, shifted, out var r2, out var h2);
            float left2 = r2.xMin - shifted.xMin;
            float right2 = shifted.xMax - h2.xMax;
            Check("hud-mirror-safe",
                Mathf.Abs(left2 - right2) <= pt
                && Mathf.Abs(r2.center.y - h2.center.y) <= pt,
                "left=" + left2.ToString("0.0") + " right=" + right2.ToString("0.0"));

            var off = new Vector2(13.5f, -8.25f);
            FlockFiveApp.GardenCornersShaken(W, H, safe, off, 0f, out var sr, out var sh);
            var dr = sr.center - restart.center;
            var dh = sh.center - hive.center;
            Check("hud-shake",
                Mathf.Abs(dr.x - dh.x) <= 0.05f && Mathf.Abs(dr.y - dh.y) <= 0.05f
                && Mathf.Abs(dr.x - off.x) <= 0.05f && Mathf.Abs(dr.y - off.y) <= 0.05f,
                "dRestart=" + dr + " dHive=" + dh);

            float hudScale = Mathf.Max(H / 720f, 1f);
            float hudMargin = CornerAnchor.BottomMarginRef * hudScale;
            bool HudIn(Rect r, Rect area)
            {
                float guiTop = H - area.yMax;
                float guiBot = H - area.yMin;
                return r.width > 8f && r.height > 8f
                    && r.xMin >= area.xMin - 0.5f && r.xMax <= area.xMax + 0.5f
                    && r.yMin >= guiTop - 0.5f
                    && r.yMax <= guiBot - hudMargin + 0.5f;
            }
            Check("hud-safe",
                HudIn(restart, safe) && HudIn(hive, safe) && HudIn(r2, shifted) && HudIn(h2, shifted),
                "restart=" + restart + " hive=" + hive + " margin=" + hudMargin.ToString("0.0"));

            string root = System.IO.Path.GetDirectoryName(Application.dataPath);
            string app = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "Assets/_Project/Scripts/App/FlockFiveApp.cs"));
            string coach = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "Assets/_Project/Scripts/App/FlockFiveApp.Coach.cs"));
            Check("hud-shared-helper",
                app.Contains("CornerAnchor.Garden") && app.Contains("HudShaken")
                && app.Contains("CornerAnchor.Shake") && coach.Contains("HudShaken"),
                "garden + shake");

            var phone = new Rect(0f, 102f, W, H - 102f - 177f);
            bool lessons = true;
            string lessonDetail = "card lessons clear";
            int[] steps = { 1, 2, 3 };
            for (int i = 0; i < steps.Length; i++)
            {
                int step = steps[i];
                Rect focus = step <= 1
                    ? FlockFiveApp.AlbumGridRect(W, H, phone, 0)
                    : FlockFiveApp.AlbumInspectRect(W, H);
                string line = step == 3 ? CaptionLayout.AlbumHoneyBlock
                    : step == 2 ? "Tap again to flip."
                    : "Tap a card you have\nto enlarge it.";
                var pose = FlockFiveApp.PoseLesson(step, focus, W, H, line);
                var text = new CoachSeat.FocusText { Title = pose.Title, Body = pose.Body, Flavor = pose.Flavor };
                bool below = step != 3 || pose.Plate.y + 1f >= focus.yMax;
                bool onGlass = step != 3 || (pose.Plate.yMax <= H - 2f && pose.Plate.yMin >= 2f);
                bool honeyAim = step != 3 || pose.Honey.width < 2f
                    || pose.Honey.Contains(pose.Aim) || pose.Glove.Overlaps(pose.Honey);
                if (!pose.Clear || text.Hits(pose.Plate) || text.Hits(pose.Glove) || !below || !onGlass || !honeyAim)
                {
                    lessons = false;
                    lessonDetail = "step " + step
                        + " clear=" + pose.Clear
                        + " plate=" + pose.Plate
                        + " glove=" + pose.Glove
                        + " title=" + pose.Title
                        + " flavor=" + pose.Flavor
                        + " honey=" + pose.Honey
                        + " aim=" + pose.Aim;
                    break;
                }
            }
            var spec = FlockFiveApp.CardText.InspectCard(W, H);
            var fullCard = FlockFiveApp.AlbumInspectRect(W, H);
            bool fullSize = Mathf.Abs(fullCard.width - spec.x) < 1f
                && Mathf.Abs(fullCard.height - spec.y) < 1f
                && fullCard.height > H * 0.50f
                && app.IndexOf("AlbumInspectRect(") >= 0
                && app.IndexOf("room - drop") < 0;
            if (!fullSize)
                lessonDetail = "card " + fullCard.width.ToString("0") + "x" + fullCard.height.ToString("0")
                    + " spec=" + spec.x.ToString("0") + "x" + spec.y.ToString("0");
            Check("card-lesson-clear", lessons && fullSize, lessonDetail);
        }
    }
}
#endif
