using UnityEngine;

namespace FlockFive
{
    public sealed partial class FlockFiveApp
    {
        bool _settingsOpen;
        SettingsPhase _settingsPhase;
        float _settingsAt;
        bool _smokePlayed;
        bool _smokeNoted;
        bool _gearHeld;
        AdConsent.HomeCard _homePrompt;

        // Build 79 drew the gear at 40px times scale. Build 80 grows that by 1.3.
        // Build 81 draws 0.75 of that and pins the top-left to the safe-area corner.
        public const float SettingsGearRef = 40f;
        public const float SettingsGearGrow = 1.3f;
        public const float SettingsGearShrink = 0.75f;
        // 10pt sits in the 8–12 pt corner margin.
        public const float SettingsGearMarginPt = 10f;

        // Pixels per point. 1179×2556 is 393×852 at 3×, so height / 852 is 3.
        public static float SettingsGearPxPerPt(float h)
        {
            float pt = h / 852f;
            if (pt < 1f) pt = 1f;
            return pt;
        }

        public static float SettingsGearScale(float h)
        {
            float s = h / 720f;
            if (s < 0.5f) s = 0.5f;
            return s;
        }

        // Build 80 drawn side, before the build 81 shrink.
        public static float SettingsGearBuild80Size(float h)
        {
            return Mathf.Max(44f, SettingsGearRef * SettingsGearGrow * SettingsGearScale(h));
        }

        public static float SettingsGearDrawnSize(float h)
        {
            return SettingsGearBuild80Size(h) * SettingsGearShrink;
        }

        // 44pt. The hit uses this when the shrunk icon is smaller.
        public static float SettingsGearMinHit(float h)
        {
            return 44f * SettingsGearPxPerPt(h);
        }

        public static float SettingsGearMargin(float h)
        {
            return SettingsGearMarginPt * SettingsGearPxPerPt(h);
        }

        // Safe-area top-left plus the corner margin. Empty safe area is the screen corner.
        static Vector2 SettingsGearOrigin(float h, Rect safe)
        {
            float m = SettingsGearMargin(h);
            float safeTop = 0f;
            float x = m;
            if (safe.width >= 2f && safe.height >= 2f)
            {
                safeTop = Mathf.Max(0f, h - safe.yMax);
                x = safe.xMin + m;
            }
            if (x < m) x = m;
            return new Vector2(x, safeTop + m);
        }

        public static Rect SettingsGearDrawnRect(float w, float h, Rect safe)
        {
            var o = SettingsGearOrigin(h, safe);
            float size = SettingsGearDrawnSize(h);
            return new Rect(o.x, o.y, size, size);
        }

        // Hit. Same top-left as the icon. Extends down and right past a small icon.
        public static Rect SettingsGearRect(float w, float h, Rect safe)
        {
            var drawn = SettingsGearDrawnRect(w, h, safe);
            float hit = Mathf.Max(drawn.width, SettingsGearMinHit(h));
            return new Rect(drawn.x, drawn.y, hit, hit);
        }

        HomeGearQuery HomeGearNow()
        {
            bool home = _splash && _home == HomeFace.Splash;
            HomeCover cover = HomeCover.None;
            if (_hiveIntroLive) cover |= HomeCover.HiveIntro;
            if (_pokerIntroLive) cover |= HomeCover.PokerIntro;
            if (_dailyIntroLive && !_dailyOpen) cover |= HomeCover.DailyIntro;
            if (_dailyIntroLive && _dailyOpen) cover |= HomeCover.DailyClaim;
            if (_welcomeOpen || _welcomeGlove) cover |= HomeCover.Welcome;
            if (_dailyAskOpen) cover |= HomeCover.DailyAsk;
            if (_adoptLive) cover |= HomeCover.Adopt;
            if (_avatarRename) cover |= HomeCover.AvatarRename;
            if (_streakSlide >= 0f) cover |= HomeCover.Streak;
            if (_restartAsk != RestartAsk.None) cover |= HomeCover.KeepMultiplier;
            if (VipOffer.IsOpen) cover |= HomeCover.Vip;
            if (_dailyOpen) cover |= HomeCover.DailyCard;
            if (_settingsOpen) cover |= HomeCover.Settings;
            bool forced = false;
            if (home)
            {
                // Rail gloves, the Daily Claim step, and the adopt look step.
                if (HomeStepRail() >= 0) forced = true;
                if (_dailyIntroLive && _dailyOpen && !_dailyAskOpen) forced = true;
                if (_adoptLive && _adoptStep == AdoptStep.Look) forced = true;
                if (ForcedGloveUp()) forced = true;
            }
            return new HomeGearQuery { Home = home, Cover = cover, ForcedTap = forced };
        }

        // Hit before the popups. A forced glove step keeps the gear up and drops the tap.
        // An open sheet owns the gear (tap again closes). Transitions swallow later.
        void PollHomeGear()
        {
            _gearHeld = false;
            if (_settingsOpen) return;
            var q = HomeGearNow();
            var r = SettingsGearRect(Screen.width, Screen.height, Screen.safeArea);
            if (!HomeGear.AcceptsTap(q) || VeilBlocks(r))
            {
                if (HomeGear.Shown(q) || VeilBlocks(r)) SwallowGearTap(r);
                return;
            }
            if (HitPad(r, out _gearHeld))
            {
                ApplySettingsHit(SettingsMotion.Hit.Gear);
                _hitLock = 1;
            }
        }

        void TickSettingsMotion()
        {
            var next = SettingsMotion.Settled(_settingsPhase, SettingsMotion.Age(AdPause.Unscaled, _settingsAt));
            if (next == _settingsPhase) return;
            _settingsPhase = next;
            _settingsAt = AdPause.Unscaled;
            _settingsOpen = next != SettingsPhase.Closed;
            if (next == SettingsPhase.Closed) AudioGuard.OnSceneOrPopupClosed();
            else AudioGuard.Hold("settings");
        }

        void ApplySettingsHit(SettingsMotion.Hit hit)
        {
            var next = SettingsMotion.AfterHit(_settingsPhase, hit);
            if (next == _settingsPhase) return;
            _settingsPhase = next;
            _settingsAt = AdPause.Unscaled;
            _settingsOpen = next != SettingsPhase.Closed;
            if (next == SettingsPhase.Closed) AudioGuard.OnSceneOrPopupClosed();
            else AudioGuard.Hold("settings");
            Sfx.CardTap();
        }

        // Editor stills plant a settled pose. Play taps go through AfterHit.
        void PoseSettings(bool open)
        {
            _settingsOpen = open;
            _settingsPhase = open ? SettingsPhase.Open : SettingsPhase.Closed;
            _settingsAt = AdPause.Unscaled;
        }

        static void SwallowGearTap(Rect r)
        {
            var e = Event.current;
            if (e == null || r.width < 2f) return;
            if (!r.Contains(e.mousePosition)) return;
            var t = e.type;
            if (t != EventType.MouseDown && t != EventType.MouseUp && t != EventType.MouseDrag) return;
            if (t == EventType.MouseUp) GUIUtility.hotControl = 0;
            e.Use();
        }

        // Painted after the home dimmers. The open sheet paints the cog itself,
        // above its overlay, so this pass stays out while Settings is up.
        void PaintHomeGear()
        {
            var q = HomeGearNow();
            if (!HomeGear.Shown(q) || !HomeGear.AboveDimmer(q) || _settingsOpen) return;
            if (SettingsMotion.GearPassFor(q.Home, _settingsPhase) != SettingsMotion.GearPass.AboveDimmer) return;
            DrawSettingsGear(0f, _gearHeld);
        }

        // Informational any-tap lessons leave the gear alone so it can open Settings.
        bool HomeGearExempt(Vector2 p)
        {
            if (_settingsOpen) return false;
            var q = HomeGearNow();
            if (!HomeGear.AcceptsTap(q)) return false;
            var r = SettingsGearRect(Screen.width, Screen.height, Screen.safeArea);
            return r.width >= 2f && r.Contains(p);
        }

        // Privacy link, then the Do Not Sell checkbox on the next row. The whole
        // checkbox row is the hit. The card is a flat wood panel (no oval vignette).
        // The dim fades under the card. The cog is painted after both.
        void DrawSettingsSheet(float s)
        {
            _hitLock = 0;
            if (s < 0.5f) s = 0.5f;
            float age = SettingsMotion.Age(AdPause.Unscaled, _settingsAt);
            var frame = SettingsSheet.Layout(Screen.width, Screen.height, Screen.safeArea, TopHud());
            float pad = Mathf.Max(16f, 12f * s);
            float travel = SettingsMotion.DropTravel(frame.Card.yMax, pad);
            float dy = SettingsMotion.PanelOffset(_settingsPhase, age, travel);
            float dim = SettingsMotion.OverlayAlpha(_settingsPhase, age);
            float spin = SettingsMotion.GearDegrees(_settingsPhase, age);
            frame = frame.OffsetY(dy);
            var card = frame.Card;
            var rows = frame.Body;
            var titleR = frame.Title;

            bool gearHeld = false;
            bool linkHeld = false;
            bool rowHeld = false;
            bool animHeld = false;
            bool xHeld = false;
            bool link = false;
            bool flip = false;
            bool flipAnim = false;
            var hit = SettingsMotion.Hit.None;
            if (SettingsMotion.BlocksInput(_settingsPhase))
                SwallowSettingsPointer();
            else
            {
                var gearHit = SettingsGearRect(Screen.width, Screen.height, Screen.safeArea);
                var screen = new Rect(0f, 0f, Screen.width, Screen.height);
                bool gearFire = HitPad(gearHit, out gearHeld);
                bool close = HitPad(CloseTapRect(frame.Close), out xHeld);
                link = HitPad(rows.PrivacyLink, out linkHeld);
                var rowHit = new Rect(rows.Label.x, rows.Label.y, rows.Toggle.xMax - rows.Label.x, rows.Label.height);
                flip = HitPad(rowHit, out rowHeld);
                var animHit = new Rect(rows.AnimateLabel.x, rows.AnimateLabel.y, rows.AnimateToggle.xMax - rows.AnimateLabel.x, rows.AnimateLabel.height);
                flipAnim = HitPad(animHit, out animHeld);
                HitPad(card, out _);
                bool overlay = HitPad(screen, out _);
                if (gearFire) hit = SettingsMotion.Hit.Gear;
                else if (close) hit = SettingsMotion.Hit.Close;
                else if (overlay) hit = SettingsMotion.Hit.Overlay;
            }

            if (Texture2D.whiteTexture != null)
            {
                DrawSettingsDim(dim);
                DrawWoodPanel(card, s);

                var title = GuiPool.Label(GuiSlot.SettingsTitle);
                title.fontStyle = FontStyle.Bold;
                title.alignment = TextAnchor.MiddleCenter;
                title.wordWrap = false;
                title.fontSize = FitFont(title, "Settings", titleR.width * 0.9f, titleR.height * 0.8f, 16, Mathf.Max(22, Mathf.RoundToInt(32f * s)));
                StampOutlined(titleR, "Settings", title, new Color(1f, 0.97f, 0.90f, 1f), 0, Mathf.Max(3, Mathf.RoundToInt(title.fontSize * 0.12f)));

                var linkSt = GuiPool.Label(GuiSlot.SettingsLink);
                linkSt.fontStyle = FontStyle.Bold;
                linkSt.alignment = TextAnchor.MiddleLeft;
                linkSt.wordWrap = false;
                float textW = Mathf.Max(8f, rows.Toggle.xMin - rows.PrivacyLink.x - 8f * s);
                linkSt.fontSize = FitFont(
                    linkSt, SettingsSheet.PrivacyLabel,
                    textW, rows.PrivacyLink.height * 0.62f,
                    14, Mathf.Max(16, Mathf.RoundToInt(22f * s)));
                var linkCol = linkHeld ? new Color(1f, 1f, 0.94f, 1f) : new Color(1f, 0.97f, 0.90f, 1f);
                StampOutlined(rows.PrivacyLink, SettingsSheet.PrivacyLabel, linkSt, linkCol, 0, Mathf.Max(2, Mathf.RoundToInt(linkSt.fontSize * 0.16f)));

                float sw = rows.Toggle.width;
                var chev = new Rect(
                    rows.Toggle.x,
                    rows.PrivacyLink.y + (rows.PrivacyLink.height - sw) * 0.5f,
                    sw, sw);
                var chevSt = GuiPool.Label(GuiSlot.SettingsChevron);
                chevSt.fontStyle = FontStyle.Bold;
                chevSt.alignment = TextAnchor.MiddleCenter;
                chevSt.wordWrap = false;
                chevSt.fontSize = FitFont(chevSt, "\u203A", chev.width * 0.8f, chev.height * 0.8f, 16, Mathf.Max(20, Mathf.RoundToInt(28f * s)));
                StampOutlined(chev, "\u203A", chevSt, linkCol, 0, Mathf.Max(2, Mathf.RoundToInt(chevSt.fontSize * 0.12f)));

                var rowSt = GuiPool.Label(GuiSlot.SettingsRow);
                rowSt.fontStyle = FontStyle.Bold;
                rowSt.alignment = TextAnchor.MiddleLeft;
                rowSt.wordWrap = true;
                rowSt.fontSize = FitFontWrapped(
                    rowSt, SettingsSheet.DoNotSellLabel,
                    rows.Label.width * 0.96f, rows.Label.height * 0.88f,
                    13, Mathf.Max(16, Mathf.RoundToInt(20f * s)));
                StampOutlined(
                    rows.Label, SettingsSheet.DoNotSellLabel, rowSt,
                    new Color(1f, 0.97f, 0.90f, 1f), 0, Mathf.Max(2, Mathf.RoundToInt(rowSt.fontSize * 0.14f)));

                bool on = AdConsent.DoNotSell;
                DrawCheckbox(rows.Toggle, on, rowHeld);
                if (on) DrawCheckMark(Inset(rows.Toggle, 0.22f), new Color(0.20f, 0.42f, 0.16f, 1f));

                var animSt = GuiPool.Label(GuiSlot.SettingsAnimate);
                animSt.fontStyle = FontStyle.Bold;
                animSt.alignment = TextAnchor.MiddleLeft;
                animSt.wordWrap = true;
                animSt.fontSize = FitFontWrapped(
                    animSt, SettingsSheet.AnimateLabel,
                    rows.AnimateLabel.width * 0.96f, rows.AnimateLabel.height * 0.88f,
                    13, Mathf.Max(16, Mathf.RoundToInt(20f * s)));
                StampOutlined(
                    rows.AnimateLabel, SettingsSheet.AnimateLabel, animSt,
                    new Color(1f, 0.97f, 0.90f, 1f), 0, Mathf.Max(2, Mathf.RoundToInt(animSt.fontSize * 0.14f)));
                bool animOn = BackgroundMotion.On;
                DrawCheckbox(rows.AnimateToggle, animOn, animHeld);
                if (animOn) DrawCheckMark(Inset(rows.AnimateToggle, 0.22f), new Color(0.20f, 0.42f, 0.16f, 1f));

                var xBtn = frame.Close;
                DrawGiftCloseX(xBtn, xHeld, s);
                var pass = SettingsMotion.GearPassFor(HomeGearNow().Home, _settingsPhase);
                if (pass == SettingsMotion.GearPass.AboveOverlay || _settingsOpen)
                    DrawSettingsGear(spin, gearHeld);

                var next = SettingsMotion.AfterHit(_settingsPhase, hit);
                if (next != _settingsPhase)
                {
                    ApplySettingsHit(hit);
                    return;
                }
                if (SettingsMotion.BlocksInput(_settingsPhase)) return;
                if (link)
                {
                    Sfx.CardTap();
                    if (!string.IsNullOrEmpty(SettingsSheet.PrivacyUrl))
                        Application.OpenURL(SettingsSheet.PrivacyUrl);
                    return;
                }
                if (flip)
                {
                    AdConsent.DoNotSell = !on;
                    Sfx.CardTap();
                }
                if (flipAnim)
                {
                    BackgroundMotion.On = !animOn;
                    Sfx.CardTap();
                }
                return;
            }

            DrawSettingsGear(spin, gearHeld);
            ApplySettingsHit(hit);
        }

        // Full-screen black under the card. The cog is drawn after this, at full color.
        void DrawSettingsDim(float alpha)
        {
            if (alpha > 1f) alpha = 1f;
            ModalBlock.Paint(ModalBlock.Id.Settings, new Color(0f, 0f, 0f, alpha));
        }

        // Corner cog. Positive degrees spin clockwise. Alpha stays 1 so the dim cannot grey it.
        void DrawSettingsGear(float degrees, bool held)
        {
            var q = HomeGearNow();
            if (!HomeGear.Shown(q) || !HomeGear.AboveDimmer(q)) return;
            var tex = GearIcon.Texture();
            if (tex == null) return;
            var drawn = SettingsGearDrawnRect(Screen.width, Screen.height, Screen.safeArea);
            var draw = drawn;
            if (held)
            {
                float k = 0.93f;
                float ox = drawn.width * (1f - k) * 0.5f;
                float oy = drawn.height * (1f - k) * 0.5f + drawn.height * 0.028f;
                draw = new Rect(drawn.x + ox, drawn.y + oy, drawn.width * k, drawn.height * k);
            }
            var prevM = GUI.matrix;
            var prevC = GUI.color;
            if (Mathf.Abs(degrees) > 0.05f)
                GUIUtility.RotateAroundPivot(degrees, draw.center);
            GUI.color = held ? new Color(0.62f, 0.52f, 0.40f, 1f) : Color.white;
            GUI.DrawTexture(draw, tex, ScaleMode.ScaleToFit, true);
            GUI.matrix = prevM;
            GUI.color = prevC;
        }

        static void SwallowSettingsPointer()
        {
            var e = Event.current;
            if (e == null) return;
            var t = e.type;
            if (t != EventType.MouseDown && t != EventType.MouseUp && t != EventType.MouseDrag) return;
            if (t == EventType.MouseUp) GUIUtility.hotControl = 0;
            e.Use();
        }

        // Square box. Empty when off. The check is drawn by the caller when on.
        static void DrawCheckbox(Rect box, bool on, bool held)
        {
            if (box.width < 2f || box.height < 2f || Texture2D.whiteTexture == null) return;
            var tex = Texture2D.whiteTexture;
            GUI.color = held ? new Color(0.95f, 0.78f, 0.32f, 1f) : new Color(0.62f, 0.42f, 0.14f, 1f);
            GUI.DrawTexture(box, tex);
            float inset = Mathf.Max(3f, box.width * 0.14f);
            var inner = new Rect(box.x + inset, box.y + inset, box.width - inset * 2f, box.height - inset * 2f);
            GUI.color = on ? new Color(0.99f, 0.96f, 0.88f, 1f) : new Color(0.93f, 0.88f, 0.76f, 1f);
            GUI.DrawTexture(inner, tex);
            GUI.color = Color.white;
        }

        // Flat wood slab and a gold rim. White texture only, so the blanket oval stays out.
        static void DrawWoodPanel(Rect card, float s)
        {
            var tex = Texture2D.whiteTexture;
            if (tex == null || card.width < 4f || card.height < 4f) return;
            float edge = Mathf.Max(8f, 11f * s);
            GUI.color = new Color(0.10f, 0.06f, 0.03f, 0.28f);
            GUI.DrawTexture(new Rect(card.x + 3f * s, card.y + 5f * s, card.width, card.height), tex);
            GUI.color = new Color(0.34f, 0.18f, 0.07f, 1f);
            GUI.DrawTexture(card, tex);
            GUI.color = new Color(0.86f, 0.68f, 0.30f, 1f);
            GUI.DrawTexture(new Rect(card.x + 3f, card.y + 3f, card.width - 6f, card.height - 6f), tex);
            GUI.color = new Color(0.55f, 0.36f, 0.12f, 1f);
            float lip = Mathf.Max(2f, 3f * s);
            GUI.DrawTexture(new Rect(card.x + edge, card.y + edge, card.width - edge * 2f, card.height - edge * 2f), tex);
            GUI.color = new Color(0.48f, 0.29f, 0.12f, 1f);
            GUI.DrawTexture(new Rect(card.x + edge + lip, card.y + edge + lip, card.width - (edge + lip) * 2f, card.height - (edge + lip) * 2f), tex);
            GUI.color = new Color(1f, 0.90f, 0.55f, 0.55f);
            GUI.DrawTexture(new Rect(card.x + edge, card.y + edge, card.width - edge * 2f, Mathf.Max(2f, 3f * s)), tex);
            GUI.color = Color.white;
        }

        // Same path as the LEVEL flower: badger leap, otherwise the gate sting and Load.
        bool PressLevelButton()
        {
            if (TakeBadgerFlower())
            {
                if (!LevelCurtain.LoadStarted) LevelCurtain.Clear();
                return true;
            }
            if (!LevelCurtain.Armed) LevelCurtain.Arm(AdPause.Unscaled);
            if (!LevelCurtain.LoadStarted)
            {
                LevelCurtain.BeginLoad();
                Sfx.GateGo();
                Load(LevelData.NextPlay);
                LevelCurtain.MarkReady();
                LevelCurtain.Fire(AdPause.Unscaled);
            }
            return true;
        }

        // Simulator hook. Absent the argument and the env var, this returns before it touches consent or the level.
        void MaybeSmokeLaunch()
        {
            if (_smokePlayed || !AdConsent.SmokeAutoPlay()) return;
            if (!AdConsent.FirstFrameReady) return;
            if (!_smokeNoted)
            {
                _smokeNoted = true;
                Debug.Log("FF_SMOKE state=played=" + (_smokePlayed ? 1 : 0)
                    + " first=" + (AdConsent.FirstFrameReady ? 1 : 0)
                    + " argc=" + AdConsent.SmokeArgc
                    + " argv=" + (AdConsent.SmokeArgv ? 1 : 0)
                    + " native=" + (AdConsent.SmokeNative ? 1 : 0)
                    + " env=" + (AdConsent.SmokeEnv ? 1 : 0)
                    + " splash=" + (_splash ? 1 : 0)
                    + " home=" + _home
                    + " badger=" + BadgerOwed());
            }
            if (!_splash || _home != HomeFace.Splash) return;
            _smokePlayed = true;
            AdConsent.DismissTransient();
            PressLevelButton();
        }

        void PollHomeAtt()
        {
            bool mid = !_splash;
            bool coach;
            if (_splash)
            {
                HomeModals(out bool hard, out bool soft);
                coach = hard || soft || HomeTutorLive() || TutorialGuideLive();
            }
            else
                coach = TutorialGuideLive();
            AdConsent.PollSystem(Application.isFocused, mid, coach);
        }

        void PrepareSoftPrompt(bool coachOrModal)
        {
            bool onHome = _splash && _home == HomeFace.Splash && !_settingsOpen;
            _homePrompt = AdConsent.LayoutHome(
                Screen.width, Screen.height, Screen.safeArea,
                onHome, coachOrModal || !onHome, LevelData.NextPlay);
        }

        void DrawSoftPrompt(float s)
        {
            var card = _homePrompt;
            if (card.Kind == AdConsent.Sheet.None)
            {
                AudioGuard.ReleaseReason("sheet");
                return;
            }
            AudioGuard.Hold("sheet");
            if (Texture2D.whiteTexture == null) return;
            if (s < 0.5f) s = 0.5f;
            var sheet = card.Kind == AdConsent.Sheet.Att ? ModalBlock.Id.Att : ModalBlock.Id.Consent;
            ModalBlock.Paint(sheet, new Color(0.05f, 0.04f, 0.03f, 0.28f));
            DrawWoodPanel(card.Card, s);

            string body = card.Kind == AdConsent.Sheet.Att ? AdConsent.AttBody : AdConsent.GdprBody;
            var st = GuiPool.Label(GuiSlot.PromptBody);
            st.fontStyle = FontStyle.Bold;
            st.alignment = TextAnchor.MiddleCenter;
            st.wordWrap = true;
            st.fontSize = FitFontWrapped(
                st, body, card.Body.width * 0.92f, card.Body.height * 0.92f,
                15, Mathf.Max(18, Mathf.RoundToInt(22f * s)));
            StampOutlined(card.Body, body, st, new Color(1f, 0.97f, 0.90f, 1f), 0, Mathf.Max(2, Mathf.RoundToInt(st.fontSize * 0.14f)));

            string yes = card.Kind == AdConsent.Sheet.Att ? AdConsent.ContinueLabel : AdConsent.AllowLabel;
            if (DrawSheetButton(card.Accept, yes, true, s))
            {
                if (card.Kind == AdConsent.Sheet.Att) AdConsent.ContinueAtt();
                else AdConsent.AcceptGdpr();
                Sfx.CardTap();
                return;
            }
            if (DrawSheetButton(card.Decline, AdConsent.NotNowLabel, false, s))
            {
                if (card.Kind == AdConsent.Sheet.Att) AdConsent.NotNow(LevelData.NextPlay);
                else AdConsent.DeclineGdpr();
                Sfx.CardTap();
            }
            float sw = Screen.width > 2f ? Screen.width : 2f;
            float sh = Screen.height > 2f ? Screen.height : 2f;
            ModalBlock.SwallowRest(ModalBlock.Raise(sheet, sw, sh));
        }

        bool DrawSheetButton(Rect rect, string label, bool gold, float s)
        {
            bool fire = HitPad(rect, out bool held);
            var face = DrawPopupButton(rect, held, gold ? PopupTint.Gold : PopupTint.Wood);
            var st = GuiPool.Label(GuiSlot.PromptButton);
            st.fontStyle = FontStyle.Bold;
            st.alignment = TextAnchor.MiddleCenter;
            st.wordWrap = false;
            st.fontSize = FitFont(st, label, face.width * 0.9f, face.height * 0.62f, 13, Mathf.Max(16, Mathf.RoundToInt(20f * s)));
            StampOutlined(face, label, st, new Color(1f, 0.98f, 0.92f, 1f), 0, Mathf.Max(2, Mathf.RoundToInt(st.fontSize * 0.16f)));
            return fire;
        }
    }
}
