using UnityEngine;

namespace FlockFive
{
    // Home bird stays off until level 1 is cleared (flockfive.next >= 1) and the
    // player has adopted. The first splash after that clear is three taps:
    // the bird flies in and a caption waits, a tap on the bird opens the avatar
    // page, and Done sends it to the right perch. Daily's glove waits until that
    // perch is settled. Kit is splash-only. The logo orbit stays.
    public sealed partial class FlockFiveApp
    {
        const string AvatarNamePref = "flockfive.avatar.name";
        const string AvatarAdoptedPref = "flockfive.avatar.adopted";
        const string AvatarKitPref = "flockfive.avatar.kit";
        const string AvatarNamedPref = "flockfive.avatar.named";
        const string AdoptGreetLine = "Look, a bird followed you home!";
        const string AdoptLookLine = "Tap your bird for a closer look.";
        const string AdoptNameLine = "Please give me a name!";
        const string AvatarRenameLine = "What should we call them?";
        const string AvatarDoneLabel = "Done";
        const int AvatarNameMax = 12;
        // Middle pad of branch.png. The splash draws only the crop around this seat.
        const int AvatarHomeSeat = 2;
        // One pad, plus the orchids on that stretch. Pixels, origin top-left of the sheet.
        const float HomeTwigX0 = 500f;
        const float HomeTwigX1 = 910f;
        const float HomeTwigTop = 190f;
        const float HomeTwigBot = 530f;
        const float HomeTwigTexW = 1280f;
        const float HomeTwigTexH = 720f;
        // 2.75x sits inside the 2.5–3x ask. AvatarBirdMul is 1.15x that build-36 body.
        // The fit clamp keeps the body and the name plate off the title, rails, and flower.
        const float AvatarGrow = 2.75f;
        const float AvatarBirdMul = 1.15f;
        // Visible perch is half the limb that matched the build-36 bird.
        const float AvatarBranchMul = 0.5f;
        const float AvatarPlateMul = 0.65f;

        static readonly string[] AvatarNames = { "Rosie", "Goldie", "Pip", "Plum", "Peach" };
        static readonly string[] AvatarBad =
        {
            "fuck", "shit", "bitch", "asshole", "bastard", "damn", "crap", "piss",
            "dick", "cunt", "slut", "whore"
        };

        enum AdoptStep { Off, Fly, Look, Settle }

        AdoptStep _adoptStep;
        bool _adoptOwed;
        bool _adoptLive;
        Rect _adoptLineR;
        Vector2 _adoptWood;
        float _adoptWoodW;
        float _adoptWoodH;
        float _adoptPx;
        int _adoptPick = -1;
        float _adoptClock;
        float _adoptSettle;
        float _adoptIcon;
        // Daily's glove and caption wait until the adopted bird is perched.
        bool _birdSettledOnBranch = true;
        Vector2 _adoptWoodFrom;
        float _adoptWoodFromW;
        float _adoptWoodFromH;
        float _adoptWoodFromPx;
        float _adoptFromIcon;
        BirdColor _adoptCol = BirdColor.Gold;
        string _adoptName = "";
        readonly float[] _adoptFly = new float[5];
        readonly float[] _adoptHop = new float[5];
        readonly Vector2[] _adoptShown = new Vector2[5];
        readonly Vector2[] _adoptLeaveFrom = new Vector2[5];

        bool _avatarRename;
        bool _avatarRenameFocus;
        string _avatarRenameText = "";
        Rect _avatarRenameR;
        Rect _avatarPlateR;
        float _avatarTail;
        int _avatarOpenFrame = -1;
        int _avatarKitN;
        readonly Rect[] _avatarSwatch = new Rect[5];
        readonly Rect[] _avatarKitR = new Rect[3];
        readonly int[] _avatarKitId = new int[3];

        // Rest and flap columns match BirdIdle. Rows there are identical per color.
        static readonly float[] AvatarBowX = { -0.11f, -0.270f, -0.259f, -0.270f, -0.270f };
        static readonly float[] AvatarBowY = { 1.055f, 0.831f, 0.843f, 0.831f, 0.831f };
        static readonly float[] AvatarCrownX = { -0.03f, -0.168f, -0.156f, -0.168f, -0.168f };
        static readonly float[] AvatarCrownY = { 1.374f, 1.252f, 1.264f, 1.252f, 1.252f };
        static int _avatarBowArt = -1;
        static int _avatarCrownArt = -1;

        static Texture2D _avatarPlateTex;
        static Texture2D _avatarFieldTex;
        static GUIStyle _avatarField;

        static bool AvatarAdopted()
        {
            return PlayerPrefs.GetInt(AvatarAdoptedPref, 0) != 0;
        }

        bool AvatarHomeOn()
        {
            if (_adoptLive) return false;
            if (LevelData.NextPlay < 1) return false;
            return AvatarAdopted();
        }

        // Hive, poker, daily, welcome, and the ask. The avatar page is not one of these.
        bool HomeLessonUp()
        {
            if (_hiveIntroLive || _pokerIntroLive || _dailyIntroLive) return true;
            if (_welcomeOpen || _welcomeGlove || _dailyAskOpen) return true;
            return false;
        }

        // Daily, hive, and poker wait until the adoption scene has finished.
        bool AdoptHoldsQueue()
        {
            if (_adoptLive || _adoptStep != AdoptStep.Off) return true;
            return _adoptOwed && !AvatarAdopted() && LevelData.NextPlay >= 1;
        }

        bool BirdSettledOnBranch() => _birdSettledOnBranch;

        // Small right perch while the Daily lesson is on the splash. The first
        // glove may not move it. The open card holds the perch in DailyCardHoldsBird.
        bool DailyRestPose()
        {
            if (!_birdSettledOnBranch) return false;
            if (_dailyOpen || _dailyAskOpen) return false;
            return _dailyIntro || _dailyIntroLive;
        }

        // The open Daily card must not rebuild the home bird. Dropping the rest
        // pose was scaling the full perch bird up behind the logo.
        bool DailyCardHoldsBird() => _dailyOpen && _avatarPlaced;

        // 0 none, 1 bow, 2 crown. Out of range stays bare. Never writes SexOf.
        static int SavedAvatarKit()
        {
            int k = PlayerPrefs.GetInt(AvatarKitPref, 0);
            if (k < 0 || k > 2) return 0;
            return k;
        }

        // Missing flag: an older save that already has a name is not prompted again.
        static bool AvatarNeedsName()
        {
            int flag = PlayerPrefs.GetInt(AvatarNamedPref, -1);
            if (flag == 1) return false;
            if (flag == 0) return true;
            return ClipAvatarName(PlayerPrefs.GetString(AvatarNamePref, "")).Length == 0;
        }

        static bool AvatarBowArt()
        {
            if (_avatarBowArt >= 0) return _avatarBowArt == 1;
            var spr = SpriteCatalog.Bow;
            _avatarBowArt = spr != null && spr.texture != null && spr.texture.width >= 32 ? 1 : 0;
            return _avatarBowArt == 1;
        }

        static bool AvatarCrownArt()
        {
            if (_avatarCrownArt >= 0) return _avatarCrownArt == 1;
            var spr = SpriteCatalog.Crown;
            _avatarCrownArt = spr != null && spr.texture != null && spr.texture.width >= 32 ? 1 : 0;
            return _avatarCrownArt == 1;
        }

        static string AvatarSuggestion(BirdColor c)
        {
            int i = (int)c;
            if (i < 0 || i >= AvatarNames.Length) return AvatarNames[1];
            return AvatarNames[i];
        }

        static string SavedAvatarName()
        {
            string n = ClipAvatarName(PlayerPrefs.GetString(AvatarNamePref, ""));
            if (n.Length == 0 || AvatarNameDirty(n)) return AvatarSuggestion(SavedAvatar());
            return n;
        }

        static float AvatarPlateH(float s) => 28f * s * AvatarPlateMul;

        static float AvatarPlateW(float icon, float s) =>
            Mathf.Max(54f * s * AvatarPlateMul, icon * 0.92f * AvatarPlateMul);

        static float AvatarPlateDrop(float s) => AvatarPlateH(s) + 1f * s;

        void AvatarChannel(float s, out float left, out float right)
        {
            float L = Mathf.Max(16f * s, Screen.safeArea.xMin + 10f);
            float safeRight = Mathf.Max(0f, Screen.width - Screen.safeArea.xMax);
            float R = Screen.width - Mathf.Max(16f * s, safeRight + 10f);
            void Eat(Rect r)
            {
                if (r.width < 2f || r.height < 2f) return;
                if (r.center.x < Screen.width * 0.5f)
                    L = Mathf.Max(L, r.xMax + 8f * s);
                else
                    R = Mathf.Min(R, r.xMin - 8f * s);
            }
            Eat(PiggyRect(s));
            Eat(SplashHiveRect());
            Eat(SplashPokerRect());
            Eat(SplashDailyRect());
            var vip = SplashNoAdsRect();
            Eat(vip);
            if (vip.width > 2f) Eat(SplashNoAdsRibbon(vip));
            // A collapsed gap stays between the rails. Opening it to the screen
            // edges put the name plate on Daily and VIP.
            if (R < L)
            {
                float edge = R;
                L = edge;
                R = edge;
            }
            left = L;
            right = R;
        }

        void ArmAdopt()
        {
            _adoptLive = false;
            _adoptStep = AdoptStep.Off;
            _adoptOwed = false;
            _adoptPick = -1;
            CloseAvatarRename();
            if (LevelData.NextPlay < 1)
            {
                _birdSettledOnBranch = true;
                return;
            }
            if (AvatarAdopted())
            {
                _birdSettledOnBranch = true;
                return;
            }
            _adoptOwed = true;
            _birdSettledOnBranch = false;
        }

        bool AdoptTurn()
        {
            if (_adoptStep == AdoptStep.Settle)
                return _splash && _home == HomeFace.Splash && !GamePause.Paused && !Ads.IsBusy;
            if (_adoptStep == AdoptStep.Off)
            {
                if (!_adoptOwed || AvatarAdopted()) return false;
                if (LevelData.NextPlay < 1) return false;
                return SplashLessonRoom();
            }
            if (!_splash || _home != HomeFace.Splash || GamePause.Paused || Ads.IsBusy) return false;
            return SplashLessonRoom();
        }

        void TickAdopt()
        {
            if (!AdoptTurn())
            {
                if (_adoptLive && _adoptStep != AdoptStep.Settle)
                {
                    _adoptLive = false;
                    _gloveVis = false;
                }
                return;
            }
            bool was = _adoptLive;
            if (_adoptStep == AdoptStep.Off)
                BeginAdopt();
            if (!was)
            {
                _gloveReady = false;
                _gloveVis = false;
            }
            _adoptLive = true;
            float dt = Mathf.Clamp(Time.unscaledDeltaTime, 0f, 0.05f);
            _adoptClock += dt;
            _coachFade = Mathf.Min(1f, _coachFade + dt / 0.30f);
            TickAdoptMotion(dt);
        }

        void BeginAdopt()
        {
            _adoptStep = AdoptStep.Fly;
            _adoptClock = 0f;
            _adoptSettle = 0f;
            _adoptPick = 1;
            _birdSettledOnBranch = false;
            _adoptCol = BirdColor.Gold;
            _adoptName = AvatarSuggestion(_adoptCol);
            _coachFade = 0f;
            _gloveReady = false;
            _gloveVis = false;
            CloseAvatarRename();
            // Same prefs the old accept path wrote, so a later splash does not ask again.
            MarkAdopted(_adoptCol, _adoptName);
            for (int i = 0; i < 5; i++)
            {
                _adoptFly[i] = 0f;
                _adoptHop[i] = 0f;
                _adoptShown[i] = new Vector2(-80f, Screen.height * 0.42f);
            }
        }

        void TickAdoptMotion(float dt)
        {
            float s = Mathf.Max(Screen.height / 720f, 1f);
            _avatarTail = AvatarPlateDrop(s);
            int pick = _adoptPick;
            if (pick < 0 || pick > 4) pick = 1;

            if (_adoptStep == AdoptStep.Settle)
            {
                _adoptSettle = Mathf.Min(1f, _adoptSettle + dt / 0.72f);
                if (_adoptHop[pick] > 0f)
                    _adoptHop[pick] = Mathf.Max(0f, _adoptHop[pick] - dt / 0.36f);
                PoseAdoptSettle(s);
                if (_adoptSettle >= 1f && _adoptHop[pick] <= 0.05f)
                    FinishAdopt();
                return;
            }

            AdoptLayout(s, out _, out _, out _, out _, out _);
            float icon = _adoptPx > 0.01f ? _adoptPx * AvatarBirdWorld() : AvatarIcon(s);
            _adoptIcon = icon;
            var greet = AdoptGreetPoint(icon);
            var from = new Vector2(-icon * 0.85f, greet.y);
            if (_adoptStep == AdoptStep.Fly)
            {
                if (_adoptClock < 0.05f)
                    _adoptShown[pick] = from;
                else
                {
                    _adoptFly[pick] = Mathf.Min(1f, _adoptFly[pick] + dt / 0.78f);
                    float u = Mathf.SmoothStep(0f, 1f, _adoptFly[pick]);
                    var p = Vector2.Lerp(from, greet, u);
                    p.y -= Mathf.Sin(u * Mathf.PI) * 24f * s;
                    _adoptShown[pick] = p;
                }
            }
            else
            {
                float k = 1f - Mathf.Exp(-dt / 0.14f);
                _adoptShown[pick] = Vector2.Lerp(_adoptShown[pick], greet, k);
            }
            if (_adoptHop[pick] > 0f)
                _adoptHop[pick] = Mathf.Max(0f, _adoptHop[pick] - dt / 0.36f);
        }

        // Color, suggested name, and adopted. named stays 0 until Done on the page.
        void MarkAdopted(BirdColor col, string birdName)
        {
            PlayerPrefs.SetInt(AvatarNamedPref, 0);
            SaveAvatar(col, birdName);
        }

        void BeginSettle()
        {
            for (int i = 0; i < 5; i++)
                _adoptLeaveFrom[i] = _adoptShown[i];
            _adoptFromIcon = _adoptIcon;
            _adoptWoodFrom = _adoptWood;
            _adoptWoodFromW = _adoptWoodW;
            _adoptWoodFromH = _adoptWoodH;
            _adoptWoodFromPx = _adoptPx;
            _adoptStep = AdoptStep.Settle;
            _adoptSettle = 0f;
            _gloveVis = false;
            _gloveReady = false;
            GUIUtility.keyboardControl = 0;
        }

        void FinishAdopt()
        {
            if (_adoptStep != AdoptStep.Settle) return;
            _birdSettledOnBranch = true;
            float s = Mathf.Max(Screen.height / 720f, 1f);
            _avatarTail = AvatarPlateDrop(s);
            float icon = AvatarIcon(s);
            FillAvatarPerches(s, icon);
            _avatarPos = AvatarHomePoint();
            _avatarPerchIx = AvatarHomeSeat;
            _avatarPlaced = true;
            _avatarCross = false;
            _avatarCrossing = false;
            _avatarGliding = false;
            _avatarHold = false;
            _avatarTutor = false;
            _avatarFaceLeft = false;
            _avatarHappy = 0f;
            _avatarFlapT = 0f;
            _avatarPreenT = 0f;
            _avatarCol = _adoptCol;
            _avatarClock = 0f;
            _avatarBobPhase = 0f;
            _avatarNextGlide = 4.8f;
            _avatarNextFlap = 2.4f;
            _awayOn = false;
            _avatarDrew = true;
            _gloveVis = false;
            _gloveReady = false;
            _coachFade = 0f;
            _adoptPx = 0f;
            _adoptIcon = 0f;
            _adoptLive = false;
            _adoptOwed = false;
            _adoptStep = AdoptStep.Off;
            GUIUtility.keyboardControl = 0;
        }

        void SaveAvatar(BirdColor col, string name)
        {
            PlayerPrefs.SetInt(AvatarPref, (int)col);
            PlayerPrefs.SetString(AvatarNamePref, name);
            PlayerPrefs.SetInt(AvatarAdoptedPref, 1);
            PlayerPrefs.Save();
        }

        void AdoptLayout(float s, out Rect caption, out Rect row, out float icon, out Rect field, out Rect keep)
        {
            if (_adoptStep == AdoptStep.Settle && _adoptPx > 1f)
            {
                caption = _adoptLineR.height > 2f
                    ? _adoptLineR
                    : PlaceCaption(s, 280f * s, 72f * s, CaptionFloorY(s));
                row = new Rect(_adoptWood.x - _adoptWoodW * 0.5f, _adoptWood.y - _adoptWoodH * 0.5f, _adoptWoodW, _adoptWoodH);
                icon = _adoptPx * AvatarBirdWorld();
                field = default;
                keep = default;
                return;
            }
            float titleBottom = TopHud() + (56f * 2f + 4f) * s;
            var flower = FlowerPlayRect();
            float top = titleBottom + 8f * s;
            float bot = flower.yMin - 8f * s;
            AvatarChannel(s, out float left, out float right);
            float width = Mathf.Max(48f, right - left);
            float textH = Mathf.Clamp(84f * s, 68f * s, 120f * s);
            float capW = Mathf.Min(Mathf.Max(120f, width - 8f), 460f * s);
            caption = new Rect(left + (width - capW) * 0.5f, top, capW, textH);
            _adoptLineR = caption;

            // The shared plate pads past the words. Keep the branch under that pad.
            float branchTop = caption.yMax + 22f;
            float branchBot = bot;
            if (branchBot < branchTop + 28f * s) branchBot = branchTop + 28f * s;

            FitWood(width, Mathf.Max(28f * s, branchBot - branchTop), out float woodW, out float woodH, out float px);
            icon = px * AvatarBirdWorld();
            float head = icon * 0.55f;
            float roomH = Mathf.Max(28f * s, branchBot - branchTop);
            if (woodH + head > roomH && woodH > 8f)
            {
                float k = Mathf.Clamp((roomH - head) / woodH, 0.35f, 1f);
                woodW *= k;
                woodH *= k;
                px *= k;
                icon = px * AvatarBirdWorld();
                head = icon * 0.55f;
            }

            float cx = (left + right) * 0.5f;
            float cy = branchTop + head + woodH * 0.5f;
            if (cy + woodH * 0.5f > branchBot) cy = branchBot - woodH * 0.5f;
            _adoptWood = new Vector2(cx, cy);
            _adoptWoodW = woodW;
            _adoptWoodH = woodH;
            _adoptPx = px;
            row = new Rect(cx - woodW * 0.5f, cy - woodH * 0.5f, woodW, woodH);

            // Heads stay under the caption. Toes stay on the wood, so shift the whole limb.
            float highest = float.MaxValue;
            for (int i = 0; i < 5; i++)
            {
                float y = LimbBird(_adoptWood, _adoptPx, false, i).y - icon * 0.5f;
                if (y < highest) highest = y;
            }
            float minTop = branchTop;
            if (highest < minTop)
            {
                float drop = minTop - highest;
                float below = branchBot - (_adoptWood.y + _adoptWoodH * 0.5f);
                if (drop > below) drop = Mathf.Max(0f, below);
                _adoptWood.y += drop;
                row.y += drop;
            }

            field = default;
            keep = default;
        }

        void DrawAdoptScene(float s)
        {
            if (!_adoptLive && _adoptStep != AdoptStep.Settle) return;
            int pick = _adoptPick;
            if (pick < 0 || pick > 4) pick = 1;
            if (_adoptStep == AdoptStep.Settle)
                PoseAdoptSettle(s);
            else
                AdoptLayout(s, out _, out _, out _, out _, out _);

            float greetIcon = _adoptPx > 0.01f ? _adoptPx * AvatarBirdWorld() : AvatarIcon(s);
            if (_adoptStep != AdoptStep.Settle)
                _adoptIcon = greetIcon;

            bool landed = _adoptStep == AdoptStep.Fly && _adoptFly[pick] >= 1f;
            if (landed && AdoptGreetTap())
            {
                _adoptStep = AdoptStep.Look;
                _gloveReady = false;
                _gloveVis = false;
            }
            else if (_adoptStep == AdoptStep.Look)
                HitAdoptBirds(greetIcon);

            if (_adoptStep == AdoptStep.Settle)
                DrawHomeLimbs();
            else if (_adoptWoodW > 4f && _adoptWoodH > 4f)
                DrawHomeTwig(_adoptWood, _adoptWoodW, _adoptWoodH, _adoptPx);
            DrawAdoptBirds(s, greetIcon);
            if (!_avatarRename && _adoptStep == AdoptStep.Fly)
                DrawAdoptBubble(AdoptGreetLine, s);
            else if (!_avatarRename && _adoptStep == AdoptStep.Look)
                DrawAdoptBubble(AdoptLookLine, s);
        }

        void DrawAdoptBubble(string line, float s)
        {
            var bubble = _adoptLineR;
            if (bubble.width < 2f) return;
            DrawCoachPanel(bubble, EaseOutCubic(_coachFade));
            var text = _adoptLineR;
            var st = CoachLineStyle();
            int hi = Mathf.Max(18, Mathf.RoundToInt(30f * s));
            st.fontSize = FitFontWrapped(st, line, text.width, text.height, 15, hi);
            int black = Mathf.Clamp(Mathf.CeilToInt(CoachOutlinePx * s), CoachOutlinePx, 8);
            StampOutlined(text, line, st, new Color(1f, 0.98f, 0.90f, EaseOutCubic(_coachFade)), 0, black);
        }

        void HitAdoptBirds(float icon)
        {
            if (_avatarRename) return;
            if (_adoptStep != AdoptStep.Look) return;
            int i = _adoptPick;
            if (i < 0 || i > 4) return;
            float s = Mathf.Max(Screen.height / 720f, 1f);
            float pad = 4f * s;
            var c = _adoptShown[i];
            var r = new Rect(c.x - icon * 0.5f - pad, c.y - icon * 0.5f - pad, icon + pad * 2f, icon + pad * 2f);
            if (!HitPad(r, out _)) return;
            _adoptHop[i] = 1f;
            Sfx.Chirp(_adoptCol);
            OpenAvatarRename();
        }

        void DrawAdoptBirds(float s, float icon)
        {
            int i = _adoptPick;
            if (i < 0 || i > 4) return;
            float drawIcon = icon;
            if (_adoptStep == AdoptStep.Settle && _adoptIcon > 1f)
                drawIcon = _adoptIcon;
            float hop = _adoptHop[i] > 0f ? Mathf.Sin((1f - _adoptHop[i]) * Mathf.PI) * 14f * s : 0f;
            bool onPerch = !((_adoptStep == AdoptStep.Fly && _adoptFly[i] < 1f)
                || (_adoptStep == AdoptStep.Settle && _adoptSettle < 0.98f)
                || _adoptHop[i] > 0.05f);
            bool wings = BirdIdle.UseFlyingPose(onPerch, false);
            float bob = wings ? 0f : Mathf.Sin((_adoptClock + i * 0.4f) * 1.7f) * Mathf.Min(4f * s, drawIcon * 0.04f);
            var c = new Vector2(_adoptShown[i].x, _adoptShown[i].y - bob - hop);
            bool faceLeft = _adoptStep == AdoptStep.Settle && AvatarHomePoint().x < _adoptLeaveFrom[i].x;
            var prev = GUI.color;
            GUI.color = Color.white;
            DrawDressedBird(_adoptCol, SavedAvatarKit(), c, drawIcon, faceLeft, wings, _adoptClock + i * 0.17f);
            GUI.color = prev;
            if (_adoptStep == AdoptStep.Settle)
                DrawAvatarPlate(c, drawIcon, _adoptName, s);
        }

        void DismissAvatarRename(float s)
        {
            if (!_avatarRename || Event.current == null) return;
            if (Event.current.type != EventType.MouseDown || Event.current.button != 0) return;
            AvatarRenameLayout(s, out _, out _, out _);
            if (_avatarRenameR.Contains(Event.current.mousePosition)) return;
            CloseAvatarRename();
            Event.current.Use();
        }

        void OpenAvatarRename()
        {
            if (_avatarRename || !AvatarAdopted()) return;
            _avatarRename = true;
            _avatarRenameText = SavedAvatarName();
            _avatarRenameFocus = AvatarNeedsName();
            _avatarOpenFrame = Time.frameCount;
            _gloveVis = false;
            _gloveReady = false;
        }

        void CloseAvatarRename()
        {
            if (!_avatarRename && string.IsNullOrEmpty(_avatarRenameText)) return;
            _avatarRename = false;
            _avatarRenameText = "";
            _avatarRenameFocus = false;
            GUIUtility.keyboardControl = 0;
        }

        void CommitAvatarRename()
        {
            string n = ClipAvatarName(_avatarRenameText);
            if (n.Length == 0 || AvatarNameDirty(n))
            {
                _avatarRenameText = AvatarSuggestion(SavedAvatar());
                _avatarRenameFocus = true;
                return;
            }
            _adoptName = n;
            PlayerPrefs.SetString(AvatarNamePref, n);
            PlayerPrefs.SetInt(AvatarNamedPref, 1);
            PlayerPrefs.Save();
            bool look = _adoptLive && _adoptStep == AdoptStep.Look;
            CloseAvatarRename();
            if (look) BeginSettle();
        }

        // SexOf stays the garden rule. This only writes the splash color.
        void PickAvatarColor(int i)
        {
            if (i < 0 || i > 4) return;
            var prev = _adoptCol;
            var col = (BirdColor)i;
            _adoptCol = col;
            _avatarCol = col;
            if (AvatarNeedsName())
            {
                string was = AvatarSuggestion(prev);
                string typed = _avatarRenameText ?? "";
                if (typed.Length == 0 || typed == was || typed == _adoptName)
                {
                    string sug = AvatarSuggestion(col);
                    _avatarRenameText = sug;
                    _adoptName = sug;
                    PlayerPrefs.SetString(AvatarNamePref, sug);
                }
            }
            PlayerPrefs.SetInt(AvatarPref, i);
            PlayerPrefs.Save();
            if (_adoptPick >= 0 && _adoptPick < _adoptHop.Length)
                _adoptHop[_adoptPick] = 1f;
            Sfx.Chirp(col);
        }

        void PickAvatarKit(int kit)
        {
            if (kit < 0 || kit > 2) return;
            if (kit == 1 && !AvatarBowArt()) return;
            if (kit == 2 && !AvatarCrownArt()) return;
            PlayerPrefs.SetInt(AvatarKitPref, kit);
            PlayerPrefs.Save();
            Sfx.CardTap();
        }

        int AvatarKitChoices()
        {
            int n = 0;
            _avatarKitId[n++] = 0;
            if (AvatarBowArt()) _avatarKitId[n++] = 1;
            if (AvatarCrownArt()) _avatarKitId[n++] = 2;
            if (n < 2) n = 0;
            _avatarKitN = n;
            return n;
        }

        static string AvatarKitLabel(int kit)
        {
            if (kit == 1) return "Bow";
            if (kit == 2) return "Crown";
            return "None";
        }

        void AvatarRenameLayout(float s, out Rect line, out Rect field, out Rect done)
        {
            // Plate top is the logo halo's bottom plus a margin. Wider and lower
            // is fine, including over the rails. Never climb into the wordmark.
            float minTop = SplashTitleHalo().yMax + 14f * s;
            var flower = FlowerPlayRect();
            float botLimit = flower.yMin - 8f * s;
            AvatarChannel(s, out float chanL, out float chanR);
            float bleed = SplashRailSize() * 0.55f;
            float edgeL = Mathf.Max(8f, Screen.safeArea.xMin + 4f);
            float edgeR = Mathf.Min(Screen.width - 8f, Screen.safeArea.xMax - 4f);
            float left = Mathf.Max(edgeL, chanL - bleed);
            float right = Mathf.Min(edgeR, chanR + bleed);
            if (right < left + 120f)
            {
                left = edgeL;
                right = edgeR;
            }
            float width = Mathf.Max(80f, right - left);
            int kits = AvatarKitChoices();
            float gap = 10f * s;
            float lineH = 36f * s;
            // 67 is 15% over the old 58 row, so the color birds and the dressed preview grow together.
            float swH = 67f * s;
            float kitH = kits > 0 ? 48f * s : 0f;
            float rowH = 46f * s;
            float pad = 12f * s;
            float innerW = Mathf.Min(width, 420f * s);
            float innerH = pad + lineH + gap + rowH + gap + swH;
            if (kits > 0) innerH += gap + kitH;
            innerH += pad;
            // CoachPanelRect paints 12px above the inner block.
            float top = minTop + 12f;
            float bot = botLimit - 12f;
            float room = bot - top;
            if (innerH > room && room > 80f * s)
            {
                float k = Mathf.Clamp(room / innerH, 0.62f, 1f);
                lineH *= k;
                swH *= k;
                kitH *= k;
                rowH *= k;
                gap *= k;
                pad *= k;
                innerH = pad + lineH + gap + rowH + gap + swH;
                if (kits > 0) innerH += gap + kitH;
                innerH += pad;
            }
            float span = edgeR - edgeL;
            float maxInner = span - 36f;
            if (maxInner < 80f) maxInner = Mathf.Max(40f, span - 8f);
            innerW = Mathf.Min(innerW, maxInner);
            float x = left + (width - innerW) * 0.5f;
            // CoachPanelRect paints 18px past the inner block. Keep that plate on screen.
            if (x - 18f < edgeL) x += edgeL - (x - 18f);
            if (x + innerW + 18f > edgeR) x -= (x + innerW + 18f) - edgeR;
            float y = top + Mathf.Max(0f, (room - innerH) * 0.22f);
            if (y + innerH > bot) y = Mathf.Max(top, bot - innerH);
            float x0 = Mathf.Round(x);
            float y0 = Mathf.Round(y);
            var inner = new Rect(x0, y0, Mathf.Max(2f, Mathf.Round(x + innerW) - x0), Mathf.Max(2f, Mathf.Round(y + innerH) - y0));
            _avatarRenameR = CoachPanelRect(inner);
            if (_avatarRenameR.y < minTop)
            {
                inner.y = Mathf.Ceil(inner.y + (minTop - _avatarRenameR.y));
                _avatarRenameR = CoachPanelRect(inner);
            }
            float cx = Mathf.Round(inner.x + pad);
            float cw = Mathf.Max(40f, Mathf.Round(inner.xMax - pad) - cx);
            float yy = Mathf.Round(inner.y + pad);
            line = new Rect(cx, yy, cw, Mathf.Max(2f, Mathf.Round(lineH)));
            yy = Mathf.Round(yy + line.height + gap);
            float gapPx = Mathf.Max(4f, Mathf.Round(gap));
            float doneW = Mathf.Round(92f * s);
            float fieldW = cw - doneW - gapPx;
            if (fieldW < 40f)
            {
                fieldW = Mathf.Max(24f, cw * 0.62f);
                doneW = Mathf.Max(28f, cw - fieldW - gapPx);
            }
            float row = Mathf.Max(2f, Mathf.Round(rowH));
            done = new Rect(Mathf.Round(cx + cw - doneW), yy, doneW, row);
            field = new Rect(cx, yy, Mathf.Max(2f, done.x - cx - gapPx), row);
            yy = Mathf.Round(yy + row + gap);
            float sw = Mathf.Max(2f, Mathf.Round(swH));
            float cell = cw / 5f;
            for (int i = 0; i < 5; i++)
            {
                float sx = Mathf.Round(cx + cell * i);
                float sx1 = Mathf.Round(cx + cell * (i + 1f));
                _avatarSwatch[i] = new Rect(sx, yy, Mathf.Max(2f, sx1 - sx), sw);
            }
            if (kits <= 0) return;
            yy = Mathf.Round(yy + sw + gap);
            float kw = (cw - gap * (kits - 1)) / kits;
            float kh = Mathf.Max(2f, Mathf.Round(kitH));
            for (int i = 0; i < kits; i++)
            {
                float kx = Mathf.Round(cx + (kw + gap) * i);
                float kx1 = i == kits - 1 ? cx + cw : Mathf.Round(kx + kw);
                _avatarKitR[i] = new Rect(kx, yy, Mathf.Max(2f, kx1 - kx), kh);
            }
        }

        void DrawAvatarRename(float s)
        {
            if (!_avatarRename) return;
            bool cue = AvatarNeedsName();
            string prompt = cue ? AdoptNameLine : AvatarRenameLine;
            AvatarRenameLayout(s, out var line, out var field, out var done);
            var content = new Rect(
                _avatarRenameR.x + 18f,
                _avatarRenameR.y + 12f,
                _avatarRenameR.width - 36f,
                _avatarRenameR.height - 24f);
            DrawCoachPanel(content, 1f);
            var st = CoachLineStyle();
            st.fontSize = FitFont(st, prompt, line.width, line.height, 14, Mathf.RoundToInt(26f * s));
            StampOutlined(line, prompt, st, new Color(1f, 0.98f, 0.90f, 1f), 0,
                Mathf.Clamp(Mathf.CeilToInt(CoachOutlinePx * s * 0.45f), 2, 4));

            bool arm = Time.frameCount == _avatarOpenFrame;
            int shown = (int)SavedAvatar();
            if (shown < 0 || shown > 4) shown = (int)BirdColor.Gold;
            for (int i = 0; i < 5; i++)
            {
                bool held = false;
                bool fire = !arm && HitPad(_avatarSwatch[i], out held);
                DrawColorSwatch(_avatarSwatch[i], (BirdColor)i, s, i == shown, held);
                if (fire) PickAvatarColor(i);
            }
            int kitNow = SavedAvatarKit();
            for (int i = 0; i < _avatarKitN; i++)
            {
                int kit = _avatarKitId[i];
                bool held = false;
                bool fire = !arm && HitPad(_avatarKitR[i], out held);
                DrawChoiceChip(_avatarKitR[i], AvatarKitLabel(kit), s, kit == kitNow, held);
                if (fire) PickAvatarKit(kit);
            }

            var fieldTex = AvatarFieldTex();
            if (fieldTex != null)
            {
                DrawSliced(fieldTex, new Rect(field.x, field.y + 2f * s, field.width, field.height), 12f, 10f * s, new Color(0.25f, 0.12f, 0.05f, 0.28f));
                DrawSliced(fieldTex, field, 12f, 10f * s, Color.white);
            }
            var fieldSt = AvatarFieldStyle(s);
            GUI.SetNextControlName("avatar-name");
            _avatarRenameText = GUI.TextField(field, _avatarRenameText ?? "", AvatarNameMax, fieldSt);
            if (_avatarRenameFocus)
            {
                GUI.FocusControl("avatar-name");
                _avatarRenameFocus = false;
            }
            bool doneHeld = false;
            bool doneFire = !arm && HitPad(done, out doneHeld);
            DrawPlaqueChip(done, AvatarDoneLabel, s, doneHeld);
            if (doneFire) CommitAvatarRename();
        }

        void DrawColorSwatch(Rect cell, BirdColor col, float s, bool on, bool held)
        {
            float icon = Mathf.Min(cell.width, cell.height) * 0.82f;
            var c = cell.center;
            if (held) c.y += 2f * s;
            if (on)
            {
                var plate = AvatarPlateTex();
                float glow = icon + 16f * s;
                var gr = new Rect(c.x - glow * 0.5f, c.y - glow * 0.5f, glow, glow);
                DrawSliced(plate, gr, 16f, 10f * s, new Color(1f, 0.91f, 0.62f, 0.32f));
                float ring = icon + 8f * s;
                var rr = new Rect(c.x - ring * 0.5f, c.y - ring * 0.5f, ring, ring);
                DrawSliced(plate, rr, 16f, 8f * s, new Color(1f, 0.98f, 0.90f, 1f));
                DrawDressedBird(col, SavedAvatarKit(), c, icon, false, false, 0f);
            }
            else
                DrawCatalogBird(col, c, icon, false, false, 0f);
        }

        void DrawChoiceChip(Rect chip, string label, float s, bool on, bool held)
        {
            var r = chip;
            if (held) r.y += 2f * s;
            if (on)
            {
                var glow = new Rect(r.x - 6f * s, r.y - 5f * s, r.width + 12f * s, r.height + 10f * s);
                DrawSliced(AvatarFieldTex(), glow, 12f, 10f * s, new Color(1f, 0.93f, 0.70f, 0.34f));
                var ring = new Rect(r.x - 3f * s, r.y - 3f * s, r.width + 6f * s, r.height + 6f * s);
                DrawSliced(AvatarFieldTex(), ring, 12f, 8f * s, new Color(1f, 0.98f, 0.90f, 1f));
            }
            DrawPlaqueChip(r, label, s, false);
        }

        void DrawPlaqueChip(Rect keep, string label, float s, bool held)
        {
            if (keep.width < 2f || string.IsNullOrEmpty(label)) return;
            var r = keep;
            if (held) r.y += 2f * s;
            var tex = AvatarPlateTex();
            if (tex != null)
            {
                DrawSliced(tex, new Rect(r.x, r.y + 2f * s, r.width, r.height), 16f, 10f * s, new Color(0.25f, 0.12f, 0.05f, 0.35f));
                DrawSliced(tex, r, 16f, 10f * s, Color.white);
            }
            var st = CoachLineStyle();
            bool wrap = st.wordWrap;
            st.wordWrap = false;
            int hi = Mathf.Max(14, Mathf.RoundToInt(18f * s));
            st.fontSize = FitFont(st, label, r.width - 10f, r.height - 6f, 11, hi);
            StampOutlined(r, label, st, new Color(0.33f, 0.15f, 0.05f, 1f), 1, 1);
            st.wordWrap = wrap;
        }

        Rect DrawAvatarPlate(Vector2 c, float icon, string name, float s)
        {
            if (string.IsNullOrEmpty(name)) name = AvatarSuggestion(_avatarCol);
            name = ClipAvatarName(name);
            if (name.Length == 0) name = AvatarSuggestion(SavedAvatar());
            float w = AvatarPlateW(icon, s);
            float h = AvatarPlateH(s);
            var r = new Rect(c.x - w * 0.5f, c.y + icon * 0.5f + 1f * s, w, h);
            var tex = AvatarPlateTex();
            if (tex != null)
            {
                DrawSliced(tex, new Rect(r.x, r.y + 3f * s, r.width, r.height), 16f, 11f * s, new Color(0.22f, 0.10f, 0.04f, 0.32f));
                DrawSliced(tex, r, 16f, 11f * s, Color.white);
            }
            var st = CoachLineStyle();
            bool wrap = st.wordWrap;
            st.wordWrap = false;
            int hi = Mathf.Max(12, Mathf.RoundToInt(16f * s));
            st.fontSize = FitFont(st, name, r.width - 12f, r.height - 4f, 9, hi);
            StampOutlined(r, name, st, new Color(0.33f, 0.15f, 0.05f, 1f), 1, 1);
            st.wordWrap = wrap;
            return r;
        }

        Rect DrawCatalogBird(BirdColor col, Vector2 c, float icon, bool faceLeft, bool wings, float clock)
        {
            var spr = SpriteCatalog.BirdFrame(col, wings ? clock * AvatarFlapRate : 0f, wings);
            if (spr == null || spr.texture == null) return default;
            float iw = icon;
            var rest = SpriteCatalog.BirdFrame(col, 0f, false);
            if (rest != null && rest != spr && rest.pixelsPerUnit > 0f && spr.pixelsPerUnit > 0f)
            {
                float restU = rest.rect.width / rest.pixelsPerUnit;
                float frameU = spr.rect.width / spr.pixelsPerUnit;
                if (restU > 0f) iw *= frameU / restU;
            }
            var r = new Rect(c.x - iw * 0.5f, c.y - iw * 0.5f, iw, iw);
            if (faceLeft)
            {
                var m = GUI.matrix;
                GUIUtility.ScaleAroundPivot(new Vector2(-1f, 1f), r.center);
                GUI.DrawTexture(r, spr.texture, ScaleMode.ScaleToFit, true);
                GUI.matrix = m;
            }
            else
                GUI.DrawTexture(r, spr.texture, ScaleMode.ScaleToFit, true);
            return r;
        }

        // Bow sits behind the body. Crown sits in front. Neither path writes SexOf.
        Rect DrawDressedBird(BirdColor col, int kit, Vector2 c, float icon, bool faceLeft, bool wings, float clock)
        {
            if (kit == 1) DrawAvatarKit(1, col, c, icon, faceLeft, wings, clock);
            var drawn = DrawCatalogBird(col, c, icon, faceLeft, wings, clock);
            if (kit == 2) DrawAvatarKit(2, col, c, icon, faceLeft, wings, clock);
            return drawn;
        }

        void DrawAvatarKit(int kit, BirdColor col, Vector2 c, float icon, bool faceLeft, bool wings, float clock)
        {
            if (icon < 2f) return;
            bool bow = kit == 1;
            if (bow)
            {
                if (!AvatarBowArt()) return;
            }
            else if (kit != 2 || !AvatarCrownArt())
                return;
            var spr = bow ? SpriteCatalog.Bow : SpriteCatalog.CrownFor(col);
            if (spr == null || spr.texture == null || spr.texture.width < 32) return;
            int fi = 0;
            if (wings)
            {
                var body = SpriteCatalog.BirdFrame(col, clock * AvatarFlapRate, true);
                fi = SpriteCatalog.PoseIndex(body, col, BirdSex.Neutral);
                if (fi < 0 || fi > 4) fi = 0;
            }
            float lx = bow ? AvatarBowX[fi] : AvatarCrownX[fi];
            float ly = bow ? AvatarBowY[fi] : AvatarCrownY[fi];
            // Garden X is the back of a right-facing head. One horizontal mirror
            // (offset plus a single sprite flip) seats the kit on the face.
            // faceLeft mirrors that pose with the body, so the art is not flipped twice.
            bool mirrorKit = !faceLeft;
            if (mirrorKit) lx = -lx;
            float unit = 280f * (icon / 1024f);
            float fit = bow ? SpriteCatalog.BowScale : 0.42f;
            float dw = (spr.rect.width / 200f) * fit * unit;
            float dh = (spr.rect.height / 200f) * fit * unit;
            if (dw < 1f || dh < 1f) return;
            float x = c.x + lx * unit;
            float y = c.y - ly * unit;
            var r = new Rect(x - dw * 0.5f, y - dh * 0.5f, dw, dh);
            // Flip runs after the rotate, so it mirrors the angle. Negating tilt
            // as well would turn the crown the wrong way on the dome.
            float tilt = bow ? 12f : 26f;
            var m = GUI.matrix;
            if (Mathf.Abs(tilt) > 0.4f)
                GUIUtility.RotateAroundPivot(tilt, r.center);
            if (mirrorKit)
                GUIUtility.ScaleAroundPivot(new Vector2(-1f, 1f), r.center);
            GUI.DrawTexture(r, spr.texture, ScaleMode.ScaleToFit, true);
            GUI.matrix = m;
        }

        // Side-on glove, same pose as the other splash lessons. A phone's row fills
        // the channel between the rails, so the hand stays down when it would cover
        // a bird, the caption, a rail, the title, or the flower.
        void AdoptPlaceGlove(float dt, float s)
        {
            if (!AdoptGloveAim(s, out var aim))
            {
                _gloveVis = false;
                return;
            }
            CoachGloveAt(aim, dt, s);
            if (AdoptGloveNowHits(s, aim))
                _gloveVis = false;
        }

        bool AdoptGloveAim(float s, out Vector2 aim)
        {
            aim = default;
            if (_avatarRename) return false;
            if (_adoptStep != AdoptStep.Look) return false;
            if (_adoptPick < 0 || _adoptPick > 4) return false;
            float icon = _adoptIcon > 1f ? _adoptIcon : AvatarIcon(s);
            var c = _adoptShown[_adoptPick];
            var bird = new Rect(c.x - icon * 0.5f, c.y - icon * 0.5f, icon, icon);
            aim = TopTouch(bird);
            return bird.width > 2f && GloveWouldClear(aim, s);
        }

        bool GloveWouldClear(Vector2 aim, float s)
        {
            CoachAimAway(aim, s, out var away, out float gap);
            bool mirror = aim.x >= Screen.width * 0.5f;
            float ang = ClampUpright(mirror);
            var rest = aim + away * gap + new Vector2(0f, -GloveRise(s));
            float dh = GloveDh(s);
            GloveSpan(rest, ang, dh, 0f, mirror, out float x0, out float y0, out float x1, out float y1);
            var box = new Rect(x0, y0, Mathf.Max(0f, x1 - x0), Mathf.Max(0f, y1 - y0));
            return !AdoptGloveHits(box, aim, s);
        }

        bool AdoptGloveNowHits(float s, Vector2 aim)
        {
            if (!_gloveVis || _coachFade < 0.03f) return false;
            float dh = GloveDh(s);
            var pivot = _gloveShown;
            float bob = Mathf.Sin(Time.unscaledTime * 2.35f) * dh * 0.028f * (1f - _gloveDip);
            float rad = _gloveShownAng * Mathf.Deg2Rad;
            pivot += new Vector2(Mathf.Sin(rad), -Mathf.Cos(rad)) * bob;
            var rect = GloveRect(pivot, dh, _gloveMirror);
            if (!RotAabb(rect.x, rect.y, rect.width, rect.height, pivot, _gloveShownAng, 4f * s, out var box))
                return false;
            return AdoptGloveHits(box, aim, s);
        }

        bool AdoptGloveHits(Rect box, Vector2 aim, float s)
        {
            AdoptLayout(s, out var caption, out _, out _, out _, out _);
            float icon = _adoptIcon > 1f ? _adoptIcon : AvatarIcon(s);
            if (HitsExceptAim(CoachPanelRect(caption), box, aim)) return true;
            float titleBottom = TopHud() + (56f * 2f + 4f) * s;
            if (HitsExceptAim(new Rect(0f, 0f, Screen.width, titleBottom), box, aim)) return true;
            if (HitsExceptAim(FlowerPlayRect(), box, aim)) return true;
            if (HitsExceptAim(PiggyRect(s), box, aim)) return true;
            if (HitsExceptAim(SplashHiveRect(), box, aim)) return true;
            if (HitsExceptAim(SplashPokerRect(), box, aim)) return true;
            if (HitsExceptAim(SplashDailyRect(), box, aim)) return true;
            var vip = SplashNoAdsRect();
            if (HitsExceptAim(vip, box, aim)) return true;
            if (vip.width > 2f && HitsExceptAim(SplashNoAdsRibbon(vip), box, aim)) return true;
            int pick = _adoptPick;
            if (pick < 0 || pick > 4) return false;
            float pad = 4f * s;
            var c = _adoptShown[pick];
            var bird = new Rect(c.x - icon * 0.5f - pad, c.y - icon * 0.5f - pad, icon + pad * 2f, icon + pad * 2f);
            return HitsExceptAim(bird, box, aim);
        }

        static bool HitsExceptAim(Rect obstacle, Rect box, Vector2 aim)
        {
            if (obstacle.width < 2f || obstacle.height < 2f) return false;
            if (!obstacle.Overlaps(box)) return false;
            return !obstacle.Contains(aim);
        }

        static GUIStyle AvatarFieldStyle(float s)
        {
            if (_avatarField == null)
            {
                var blank = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Point,
                    name = "AvatarFieldBlank"
                };
                blank.SetPixel(0, 0, new Color(0f, 0f, 0f, 0f));
                blank.Apply(false, true);
                _avatarField = new GUIStyle(GUI.skin.textField)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    clipping = TextClipping.Clip,
                    border = new RectOffset(0, 0, 0, 0),
                    padding = new RectOffset(8, 8, 4, 4)
                };
                _avatarField.normal.background = blank;
                _avatarField.focused.background = blank;
                _avatarField.hover.background = blank;
                _avatarField.active.background = blank;
                var ink = new Color(0.33f, 0.15f, 0.05f, 1f);
                _avatarField.normal.textColor = ink;
                _avatarField.focused.textColor = ink;
                _avatarField.hover.textColor = ink;
                _avatarField.active.textColor = ink;
            }
            _avatarField.fontSize = Mathf.Max(14, Mathf.RoundToInt(18f * s));
            return _avatarField;
        }

        static Texture2D AvatarPlateTex()
        {
            if (_avatarPlateTex != null) return _avatarPlateTex;
            _avatarPlateTex = PaintPlaque("AvatarPlate", 128, 48, 16f,
                new Color(0.96f, 0.78f, 0.46f, 1f),
                new Color(0.55f, 0.32f, 0.12f, 1f));
            return _avatarPlateTex;
        }

        static Texture2D AvatarFieldTex()
        {
            if (_avatarFieldTex != null) return _avatarFieldTex;
            _avatarFieldTex = PaintPlaque("AvatarField", 96, 40, 12f,
                new Color(1f, 0.96f, 0.86f, 1f),
                new Color(0.62f, 0.40f, 0.16f, 1f));
            return _avatarFieldTex;
        }

        static Texture2D PaintPlaque(string name, int w, int h, float rad, Color fill, Color rim)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = name
            };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float d = PanelSdf(x + 0.5f, y + 0.5f, w, h, rad);
                    float a = Mathf.Clamp01(0.9f - d * 0.7f);
                    a = a * a * (3f - 2f * a);
                    float t = Mathf.Clamp01((-d) / 4.2f);
                    float light = Mathf.Clamp01((h - y) / (float)h);
                    var c = Color.Lerp(rim, fill, t);
                    c.r = Mathf.Clamp01(c.r + light * 0.05f);
                    c.g = Mathf.Clamp01(c.g + light * 0.035f);
                    c.a = a;
                    px[y * w + x] = c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        // One plaque, nine quads. Edges sample the painted border, not the fill,
        // and neighbors overlap by a pixel so the rim does not score.
        static void DrawSliced(Texture2D tex, Rect box, float srcRad, float dstRad, Color tint)
        {
            if (tex == null || box.width < 2f || box.height < 2f) return;
            float x0 = Mathf.Round(box.x);
            float y0 = Mathf.Round(box.y);
            float x1 = Mathf.Round(box.xMax);
            float y1 = Mathf.Round(box.yMax);
            if (x1 < x0 + 2f || y1 < y0 + 2f) return;
            var prev = GUI.color;
            GUI.color = tint;
            float tw = Mathf.Max(1f, tex.width);
            float th = Mathf.Max(1f, tex.height);
            float u = Mathf.Clamp(srcRad / tw, 0.02f, 0.45f);
            float v = Mathf.Clamp(srcRad / th, 0.02f, 0.45f);
            float bw = x1 - x0;
            float bh = y1 - y0;
            float cw = Mathf.Min(Mathf.Max(1f, Mathf.Round(dstRad)), Mathf.Floor(bw * 0.5f));
            float ch = Mathf.Min(Mathf.Max(1f, Mathf.Round(dstRad)), Mathf.Floor(bh * 0.5f));
            float lap = (cw + 1f <= bw * 0.5f && ch + 1f <= bh * 0.5f) ? 1f : 0f;
            float hx = 0.5f / tw;
            float hy = 0.5f / th;
            float u0 = hx;
            float u1 = u - hx;
            float u2 = u + hx;
            float u3 = 1f - u - hx;
            float u4 = 1f - u + hx;
            float u5 = 1f - hx;
            if (u1 <= u0) { u0 = 0f; u1 = u; }
            if (u3 <= u2) { u2 = u; u3 = 1f - u; }
            if (u5 <= u4) { u4 = 1f - u; u5 = 1f; }
            float v0 = hy;
            float v1 = v - hy;
            float v2 = v + hy;
            float v3 = 1f - v - hy;
            float v4 = 1f - v + hy;
            float v5 = 1f - hy;
            if (v1 <= v0) { v0 = 0f; v1 = v; }
            if (v3 <= v2) { v2 = v; v3 = 1f - v; }
            if (v5 <= v4) { v4 = 1f - v; v5 = 1f; }
            float midW = bw - cw * 2f;
            float midH = bh - ch * 2f;
            void Slice(float x, float y, float w, float h, float su, float sv, float su1, float sv1)
            {
                if (w < 0.5f || h < 0.5f) return;
                GUI.DrawTextureWithTexCoords(new Rect(x, y, w, h), tex,
                    new Rect(su, sv, Mathf.Max(0.001f, su1 - su), Mathf.Max(0.001f, sv1 - sv)));
            }
            Slice(x0 + cw, y0 + ch, midW, midH, u2, v2, u3, v3);
            Slice(x0 + cw - lap, y1 - (ch + lap), midW + lap * 2f, ch + lap, u2, v0, u3, v1);
            Slice(x0 + cw - lap, y0, midW + lap * 2f, ch + lap, u2, v4, u3, v5);
            Slice(x0, y0 + ch - lap, cw + lap, midH + lap * 2f, u0, v2, u1, v3);
            Slice(x1 - cw - lap, y0 + ch - lap, cw + lap, midH + lap * 2f, u4, v2, u5, v3);
            float cornerW = cw + lap;
            float cornerH = ch + lap;
            Slice(x0, y1 - cornerH, cornerW, cornerH, u0, v0, u1, v1);
            Slice(x1 - cornerW, y1 - cornerH, cornerW, cornerH, u4, v0, u5, v1);
            Slice(x0, y0, cornerW, cornerH, u0, v4, u1, v5);
            Slice(x1 - cornerW, y0, cornerW, cornerH, u4, v4, u5, v5);
            GUI.color = prev;
        }

        static string ClipAvatarName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            var chars = new char[AvatarNameMax];
            int n = 0;
            bool gap = false;
            for (int i = 0; i < raw.Length && n < AvatarNameMax; i++)
            {
                char c = raw[i];
                bool ok = char.IsLetterOrDigit(c) || c == '\'' || c == '-' || c == ' ';
                if (!ok) continue;
                if (c == ' ')
                {
                    if (n == 0 || gap) continue;
                    gap = true;
                }
                else gap = false;
                chars[n++] = c;
            }
            while (n > 0 && (chars[n - 1] == ' ' || chars[n - 1] == '-' || chars[n - 1] == '\'')) n--;
            if (n == 0) return "";
            return new string(chars, 0, n);
        }

        static bool AvatarNameDirty(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string low = name.ToLowerInvariant();
            string squash = AvatarSquash(low);
            for (int i = 0; i < AvatarBad.Length; i++)
            {
                string bad = AvatarBad[i];
                if (squash == bad) return true;
                if (bad.Length >= 4 && squash.Contains(bad)) return true;
                if (AvatarTokenHas(low, bad)) return true;
            }
            return false;
        }

        static string AvatarSquash(string low)
        {
            var chars = new char[low.Length];
            int n = 0;
            for (int i = 0; i < low.Length; i++)
                if (char.IsLetter(low[i])) chars[n++] = low[i];
            return new string(chars, 0, n);
        }

        static bool AvatarTokenHas(string low, string bad)
        {
            int n = low.Length;
            int i = 0;
            while (i < n)
            {
                while (i < n && !char.IsLetter(low[i])) i++;
                int a = i;
                while (i < n && char.IsLetter(low[i])) i++;
                if (i - a != bad.Length) continue;
                bool match = true;
                for (int k = 0; k < bad.Length; k++)
                {
                    if (low[a + k] == bad[k]) continue;
                    match = false;
                    break;
                }
                if (match) return true;
            }
            return false;
        }

        // Arrival seat under the greet caption. The right-hand rest perch is later.
        Vector2 AdoptGreetPoint(float icon)
        {
            float world = AvatarBirdWorld();
            float birdPx = world > 0.001f ? icon / world : icon;
            float branchPx = _adoptPx > 0.01f ? _adoptPx : birdPx;
            return LimbBirdSeated(_adoptWood, branchPx, birdPx, false, AvatarHomeSeat);
        }

        // Any tap after the fly-in. The avatar page is a second tap, on the bird.
        bool AdoptGreetTap()
        {
            var e = Event.current;
            if (e == null || e.type != EventType.MouseUp || e.button != 0) return false;
            e.Use();
            return true;
        }

        // One branch rides with the bird from the greet twig to the right perch.
        void PoseAdoptSettle(float s)
        {
            int pick = _adoptPick;
            if (pick < 0 || pick > 4) pick = 1;
            PlaceClearRest(s);
            var destC = _limbCenter;
            float destW = _limbW;
            float destH = _limbH;
            float destPx = _limbPx;
            float destIcon = _avatarFitIcon > 1f ? _avatarFitIcon : _adoptFromIcon;
            float e = Mathf.SmoothStep(0f, 1f, _adoptSettle);
            _limbCenter = Vector2.Lerp(_adoptWoodFrom, destC, e);
            _limbW = Mathf.Lerp(_adoptWoodFromW, destW, e);
            _limbH = Mathf.Lerp(_adoptWoodFromH, destH, e);
            _limbPx = Mathf.Lerp(_adoptWoodFromPx, destPx, e);
            _limbOn = true;
            float icon = Mathf.Lerp(_adoptFromIcon, destIcon, e);
            float world = AvatarBirdWorld();
            float birdPx = world > 0.001f && icon > 1f ? icon / world : 0.01f;
            float branchPx = _limbPx > 0.01f ? _limbPx : birdPx;
            _adoptShown[pick] = LimbBirdSeated(_limbCenter, branchPx, birdPx, false, AvatarHomeSeat);
            _adoptIcon = icon > 1f ? icon : destIcon;
            _avatarFitIcon = destIcon;
        }

        // Small bird on the right, clear of the rails, the pedestal, Daily,
        // the Daily caption, and the glove's path onto that button.
        void PlaceClearRest(float s)
        {
            _avatarTail = AvatarPlateDrop(s);
            float titleBottom = TopHud() + (56f * 2f + 4f) * s;
            var halo = SplashTitleHalo();
            float topLim = halo.yMax > titleBottom ? halo.yMax : titleBottom;
            float floorY = FlowerPlayRect().yMin - 4f * s;
            float level = FlowerLevelTop() - 4f * s;
            if (level < floorY) floorY = level;
            var leftCol = SplashRailColumn(false, 2, s);
            var rightCol = SplashRailColumn(true, 3, s);
            float railL = leftCol.xMax + 4f * s;
            float railR = rightCol.xMin - 4f * s;
            if (railR < railL)
            {
                float mid = (leftCol.xMax + rightCol.xMin) * 0.5f;
                railL = mid;
                railR = mid;
            }

            float useIcon = 16f * s;
            Vector2 perch = default;
            Vector2 center = default;
            float woodW = 0f;
            float woodH = 0f;
            float px = 0f;
            bool found = false;
            float start = 42f * s;
            for (int attempt = 0; attempt < 5 && !found; attempt++)
            {
                float icon = Mathf.Max(16f * s, start * (1f - attempt * 0.16f));
                float plate = AvatarPlateDrop(s);
                float reach = Mathf.Max(icon * 0.5f, AvatarPlateW(icon, s) * 0.5f) + 8f * s;
                float xR = railR - reach;
                float xL = railL + reach;
                if (xR < xL) xR = (railL + railR) * 0.5f;
                float yLow = floorY - plate - icon * 0.55f;
                float yHigh = topLim + icon * 0.55f + 6f * s;
                if (yLow < yHigh)
                {
                    float midY = (topLim + floorY) * 0.5f;
                    yLow = midY;
                    yHigh = midY;
                }
                var cap = DailyCaptionKeepout(s);
                float xCap = cap.xMax + reach;
                float xSide = xCap > xL && xCap < xR ? xCap : xR;
                for (int pass = 0; pass < 3 && !found; pass++)
                {
                    float x = pass == 0 ? xR : pass == 1 ? xSide : Mathf.Lerp(xR, xL, 0.35f);
                    if (x < xL) x = xL;
                    if (x > xR) x = xR;
                    for (int yi = 0; yi < 6; yi++)
                    {
                        float y = Mathf.Lerp(yLow, yHigh, yi / 5f);
                        if (!TryRestAt(s, icon, x, y, topLim, out perch, out center, out woodW, out woodH, out px))
                            continue;
                        useIcon = icon;
                        found = true;
                        break;
                    }
                }
            }
            if (!found)
            {
                useIcon = 16f * s;
                float reach = Mathf.Max(useIcon * 0.5f, AvatarPlateW(useIcon, s) * 0.5f) + 8f * s;
                float x = railR - reach;
                if (x < railL) x = (railL + railR) * 0.5f;
                float y = floorY - AvatarPlateDrop(s) - useIcon * 0.55f;
                TryRestAt(s, useIcon, x, y, topLim, out perch, out center, out woodW, out woodH, out px);
            }

            _limbCenter = center;
            _limbW = woodW;
            _limbH = woodH;
            _limbPx = px;
            _limbOn = woodW > 1f && woodH > 1f;
            _avatarFitIcon = Mathf.Max(8f, useIcon);
            for (int i = 0; i < _avatarPerches.Length; i++)
                _avatarPerches[i] = perch;
        }

        bool TryRestAt(float s, float icon, float birdX, float birdY, float topLim,
            out Vector2 perch, out Vector2 center, out float woodW, out float woodH, out float px)
        {
            perch = default;
            center = default;
            woodW = 0f;
            woodH = 0f;
            px = 0f;
            float world = AvatarBirdWorld();
            if (world < 0.001f || icon < 2f) return false;
            float birdPx = icon / world;
            px = birdPx * (AvatarBranchMul / AvatarBirdMul);
            WoodWorld(out float worldW, out float worldH);
            woodW = px * worldW;
            woodH = px * worldH;
            var toe = LimbBirdSeated(Vector2.zero, px, birdPx, false, AvatarHomeSeat);
            center = new Vector2(birdX - toe.x, birdY - toe.y);
            perch = LimbBirdSeated(center, px, birdPx, false, AvatarHomeSeat);
            var body = AvatarBody(perch, icon, 4f * s);
            if (body.yMin < topLim) return false;
            HomeTwigRect(center, woodW, woodH, out var vis);
            float leaf = vis.height * 0.42f;
            var twig = new Rect(vis.x, vis.y - leaf, vis.width, vis.height + leaf);
            if (RestBlocked(body, s) || RestBlocked(twig, s)) return false;
            return true;
        }

        bool RestBlocked(Rect body, float s)
        {
            if (body.width < 1f || body.height < 1f) return true;
            var caption = DailyCaptionKeepout(s);
            if (caption.width > 2f && body.Overlaps(caption)) return true;
            var daily = DailyButtonSoon();
            if (daily.width > 2f && body.Overlaps(PadRect(daily, 6f * s))) return true;
            var glove = DailyGlovePath(s, daily);
            if (glove.width > 2f && body.Overlaps(glove)) return true;
            if (body.Overlaps(SplashRailColumn(false, 2, s))) return true;
            if (body.Overlaps(SplashRailColumn(true, 3, s))) return true;
            if (body.Overlaps(SplashTitleHalo())) return true;
            if (AvatarRectHitsRails(body, s)) return true;
            float level = FlowerLevelTop();
            var flower = FlowerPlayRect();
            if (body.yMax > level && body.xMax > flower.xMin && body.xMin < flower.xMax)
                return true;
            return false;
        }

        // The Daily sentence sits in this band. The rest perch stays outside it,
        // and the rect does not follow the rail while that button slides in.
        Rect DailyCaptionKeepout(float s)
        {
            float flowerTop = FlowerPlayRect().y;
            float titleBottom = TopHud() + (56f * 2f + 4f) * s;
            var halo = SplashTitleHalo();
            if (halo.yMax > titleBottom) titleBottom = halo.yMax;
            var left = SplashRailColumn(false, 2, s);
            var right = SplashRailColumn(true, 3, s);
            float x0 = left.xMax + 6f * s;
            float x1 = right.xMin - 6f * s;
            float span = x1 - x0;
            if (span < 48f)
            {
                span = 48f;
                x0 = (x0 + x1) * 0.5f - span * 0.5f;
            }
            float gapTop = titleBottom + 8f * s;
            float gapBot = flowerTop - 8f * s;
            float bandH = Mathf.Min(168f * s, Mathf.Max(72f * s, (gapBot - gapTop) * 0.62f));
            float y = gapTop;
            if (gapBot - gapTop > bandH) y = gapTop + (gapBot - gapTop - bandH) * 0.32f;
            return PadRect(new Rect(x0, y, span, bandH), 8f * s);
        }

        // Full Daily disc at the top of the left rail, including before it slides in.
        Rect DailyButtonSoon()
        {
            float size = SplashRailSize();
            float gap = SplashRailGap();
            var left = HomeRailRect(false, size);
            var right = HomeRailRect(true, size);
            float top = right.y - size - gap;
            return new Rect(left.center.x - size * 0.5f, top, size, size);
        }

        // Entrance from the screen edge, the wind-up perch, and the tap onto Daily.
        Rect DailyGlovePath(float s, Rect daily)
        {
            var aim = daily.width > 2f ? daily.center : new Vector2(24f * s, TopHud() + 80f * s);
            float dh = GloveDh(s);
            float lift = GloveRise(s);
            float arch = GloveArch(s);
            bool fromRight = aim.x < Screen.width * 0.5f;
            float awayX = fromRight ? 1f : -1f;
            float gapHi = 48f * s + 20f * s * 7f;
            float restX = aim.x + awayX * gapHi;
            float restY = aim.y - lift;
            float top = Mathf.Min(restY, aim.y) - arch - dh * 0.45f;
            float bot = aim.y + dh * 0.90f;
            float left = Mathf.Min(aim.x, restX) - dh;
            float right = Mathf.Max(aim.x, restX) + dh;
            if (fromRight) right = Screen.width + dh;
            else left = -dh;
            if (bot < top + 4f) bot = top + 4f;
            return new Rect(left, top, Mathf.Max(4f, right - left), bot - top);
        }

        // Full-size rail stack, so a button that is still sliding in stays reserved.
        Rect SplashRailColumn(bool right, int slots, float s)
        {
            float size = SplashRailSize();
            float gap = SplashRailGap();
            var anchor = HomeRailRect(right, size);
            float top = anchor.y - size - gap;
            int n = slots < 1 ? 1 : slots;
            float h = size * n + gap * (n - 1);
            if (!right) h += size * 0.32f;
            float pad = 6f * s;
            return new Rect(anchor.x - pad, top - pad, size + pad * 2f, h + pad * 2f);
        }

        Vector2 AvatarHomePoint()
        {
            int i = AvatarHomeSeat;
            if (i < 0 || i >= _avatarPerches.Length) i = 0;
            return _avatarPerches[i];
        }

        // Rest body is 1024px at 280 ppu, drawn at the garden bird scale.
        static float AvatarBirdWorld() => (1024f / 280f) * BranchView.BirdScale.y;

        static void WoodWorld(out float worldW, out float worldH)
        {
            var wood = SpriteCatalog.Branch;
            float ppu = wood != null && wood.pixelsPerUnit > 1f ? wood.pixelsPerUnit : 140f;
            float bw = wood != null ? wood.rect.width : 1280f;
            float bh = wood != null ? wood.rect.height : 720f;
            worldW = bw / ppu * WorldBuilder.WoodScaleX;
            worldH = bh / ppu * 0.50f;
            if (worldW < 0.1f) worldW = 5.85f;
            if (worldH < 0.1f) worldH = 2.57f;
        }

        static void FitWood(float maxW, float maxH, out float woodW, out float woodH, out float px)
        {
            WoodWorld(out float worldW, out float worldH);
            float aspect = worldW / worldH;
            woodW = Mathf.Max(8f, maxW);
            woodH = woodW / aspect;
            if (maxH > 8f && woodH > maxH)
            {
                woodH = maxH;
                woodW = woodH * aspect;
            }
            px = woodW / worldW;
        }

        // Bird center whose toes meet the painted pad. GUI Y grows downward.
        // branchPx and the bird scale match, so one px places both the seat and the lift.
        static Vector2 LimbBird(Vector2 center, float px, bool fromRight, int seat)
        {
            return LimbBirdSeated(center, px, px, fromRight, seat);
        }

        // Same seat on a limb drawn at branchPx. RestLift uses birdPx, so a larger
        // bird still plants its toe row on the bark instead of hovering above it.
        static Vector2 LimbBirdSeated(Vector2 center, float branchPx, float birdPx, bool fromRight, int seat)
        {
            if (seat < 0) seat = 0;
            if (seat > 4) seat = 4;
            var local = WorldBuilder.SeatLocal(seat, false, fromRight);
            float y = local.y * branchPx + BranchView.RestLift * birdPx;
            return new Vector2(center.x + local.x * branchPx, center.y - y);
        }

        void DrawHomeLimbs()
        {
            if (_awayOn && _avatarPerchIx < 0)
                DrawHomeTwig(_awayCenter, _awayW, _awayH, _awayPx);
            else if (_limbOn)
                DrawHomeTwig(_limbCenter, _limbW, _limbH, _limbPx);
        }

        // Visible slice of the full sheet. center/woodW/woodH are the uncropped limb.
        static void HomeTwigRect(Vector2 center, float woodW, float woodH, out Rect vis)
        {
            float u0 = HomeTwigX0 / HomeTwigTexW;
            float u1 = HomeTwigX1 / HomeTwigTexW;
            float topFrac = HomeTwigTop / HomeTwigTexH;
            float botFrac = HomeTwigBot / HomeTwigTexH;
            float x = center.x - woodW * 0.5f + u0 * woodW;
            float y = center.y - woodH * 0.5f + topFrac * woodH;
            vis = new Rect(x, y, (u1 - u0) * woodW, (botFrac - topFrac) * woodH);
        }

        static Rect HomeTwigUv()
        {
            float u0 = HomeTwigX0 / HomeTwigTexW;
            float u1 = HomeTwigX1 / HomeTwigTexW;
            float v = (HomeTwigTexH - HomeTwigBot) / HomeTwigTexH;
            float vh = (HomeTwigBot - HomeTwigTop) / HomeTwigTexH;
            return new Rect(u0, v, u1 - u0, vh);
        }

        void DrawHomeTwig(Vector2 center, float w, float h, float px)
        {
            if (w < 4f || h < 4f) return;
            var wood = SpriteCatalog.Branch;
            if (wood == null || wood.texture == null) return;
            HomeTwigRect(center, w, h, out var r);
            if (r.width < 2f || r.height < 2f) return;
            var prev = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTextureWithTexCoords(r, wood.texture, HomeTwigUv());
            DrawHomeTwigLeaves(center, r.height, px);
            GUI.color = prev;
        }

        void DrawHomeTwigLeaves(Vector2 center, float visH, float px)
        {
            var leaf = SpriteCatalog.Leaf;
            if (leaf == null || leaf.texture == null || px < 0.5f) return;
            float lh = Mathf.Clamp(px * 0.42f, visH * 0.16f, visH * 0.38f);
            float t = Time.unscaledTime;
            DrawOneLeaf(leaf, center, px, false, AvatarHomeSeat, -0.20f, lh * 0.90f, -18f, t, 0);
            DrawOneLeaf(leaf, center, px, false, AvatarHomeSeat, 0.12f, lh * 0.70f, 14f, t, 1);
            DrawOneLeaf(leaf, center, px, false, AvatarHomeSeat, 0.30f, lh * 0.82f, 22f, t, 2);
        }

        static void DrawOneLeaf(Sprite leaf, Vector2 center, float px, bool fromRight, int seat, float along, float height, float deg, float time, int i)
        {
            var local = WorldBuilder.SeatLocal(seat, false, fromRight);
            float side = fromRight ? -1f : 1f;
            float x = center.x + (local.x + side * along) * px;
            float stem = center.y - local.y * px;
            float aspect = leaf.rect.height > 1f ? leaf.rect.width / leaf.rect.height : 0.56f;
            float lw = height * aspect;
            var r = new Rect(x - lw * 0.5f, stem - height, lw, height);
            float sway = Mathf.Sin(time * 1.25f + i * 1.7f) * 5f;
            var m = GUI.matrix;
            var prev = GUI.color;
            float g = 0.82f + 0.06f * (i & 1);
            GUI.color = new Color(g, 1f, g * 0.9f, 1f);
            GUIUtility.RotateAroundPivot(deg + sway, new Vector2(r.center.x, r.yMax));
            GUI.DrawTexture(r, leaf.texture, ScaleMode.ScaleToFit, true);
            GUI.matrix = m;
            GUI.color = prev;
        }
    }
}
