using UnityEngine;

namespace FlockFive
{
    // Home bird stays off until level 1 is cleared (flockfive.next >= 1) and the
    // player has adopted. The first splash after that clear is the one-time scene:
    // five birds land on a branch, one asks to come home, Yes opens the name.
    // Not yet asks again on the next splash. A later tap only renames.
    public sealed partial class FlockFiveApp
    {
        const string AvatarNamePref = "flockfive.avatar.name";
        const string AvatarAdoptedPref = "flockfive.avatar.adopted";
        const string AdoptHello = "Can I come home with you?";
        const string AdoptNameLine = "Please give me a name!";
        const string AdoptYesLabel = "Yes";
        const string AdoptLaterLabel = "Not yet";
        const string AvatarRenameLine = "What should we call them?";
        const int AvatarNameMax = 12;
        // Middle pad on the home limb. Outer seats are the side perches.
        const int AvatarHomeSeat = 2;
        // 2.75x sits inside the 2.5–3x ask, then the fit clamp keeps the body and
        // the name plate between the title, the rails, and the play flower.
        const float AvatarGrow = 2.75f;

        static readonly string[] AvatarNames = { "Rosie", "Goldie", "Pip", "Plum", "Peach" };
        static readonly string[] AvatarBad =
        {
            "fuck", "shit", "bitch", "asshole", "bastard", "damn", "crap", "piss",
            "dick", "cunt", "slut", "whore"
        };

        enum AdoptStep { Off, Fly, Pick, Name, Settle }

        AdoptStep _adoptStep;
        bool _adoptOwed;
        bool _adoptLive;
        bool _adoptSkip;
        Rect _adoptLineR;
        Rect _adoptYesR;
        Rect _adoptNoR;
        Vector2 _adoptWood;
        float _adoptWoodW;
        float _adoptWoodH;
        float _adoptPx;
        bool _adoptArmFocus;
        int _adoptPick = -1;
        float _adoptClock;
        float _adoptSettle;
        float _adoptIcon;
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

        // Hive, poker, daily, welcome, and the ask. Rename is not one of these.
        bool HomeLessonUp()
        {
            if (_hiveIntroLive || _pokerIntroLive || _dailyIntroLive) return true;
            if (_welcomeOpen || _welcomeGlove || _dailyAskOpen) return true;
            return false;
        }

        // Daily, hive, and poker wait until the adoption scene has saved.
        bool AdoptHoldsQueue()
        {
            if (_adoptSkip) return false;
            if (_adoptLive || _adoptStep != AdoptStep.Off) return true;
            return _adoptOwed && !AvatarAdopted() && LevelData.NextPlay >= 1;
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

        static float AvatarPlateH(float s) => 28f * s;

        static float AvatarPlateDrop(float s) => AvatarPlateH(s) + 4f * s;

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
            if (R < L + 80f * s)
            {
                L = 16f * s;
                R = Screen.width - 16f * s;
            }
            left = L;
            right = R;
        }

        void ArmAdopt()
        {
            _adoptLive = false;
            _adoptStep = AdoptStep.Off;
            _adoptOwed = false;
            _adoptSkip = false;
            _adoptPick = -1;
            CloseAvatarRename();
            if (LevelData.NextPlay < 1) return;
            if (AvatarAdopted()) return;
            _adoptOwed = true;
        }

        bool AdoptTurn()
        {
            if (_adoptStep == AdoptStep.Settle)
                return _splash && _home == HomeFace.Splash && !GamePause.Paused && !Ads.IsBusy;
            if (_adoptSkip) return false;
            if (!_adoptOwed || AvatarAdopted()) return false;
            if (LevelData.NextPlay < 1) return false;
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
            _adoptCol = BirdColor.Gold;
            _adoptName = "";
            _adoptArmFocus = false;
            _coachFade = 0f;
            _gloveReady = false;
            _gloveVis = false;
            CloseAvatarRename();
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
            if (_adoptStep == AdoptStep.Settle)
            {
                _adoptSettle = Mathf.Min(1f, _adoptSettle + dt / 0.72f);
                float e = Mathf.SmoothStep(0f, 1f, _adoptSettle);
                float icon = AvatarIcon(s);
                _avatarTail = AvatarPlateDrop(s);
                FillAvatarPerches(s, icon);
                var home = AvatarHomePoint();
                for (int i = 0; i < 5; i++)
                {
                    if (i == _adoptPick)
                        _adoptShown[i] = Vector2.Lerp(_adoptLeaveFrom[i], home, e);
                    else
                    {
                        var off = new Vector2(Screen.width + _adoptFromIcon, _adoptLeaveFrom[i].y - 28f * s);
                        _adoptShown[i] = Vector2.Lerp(_adoptLeaveFrom[i], off, e);
                    }
                    if (_adoptHop[i] > 0f)
                        _adoptHop[i] = Mathf.Max(0f, _adoptHop[i] - dt / 0.36f);
                }
                return;
            }

            AdoptLayout(s, out _, out _, out float rowIcon, out _, out _);
            _adoptIcon = rowIcon;
            bool landed = _adoptClock > 0.35f;
            for (int i = 0; i < 5; i++)
            {
                var dest = LimbBird(_adoptWood, _adoptPx, false, i);
                var from = new Vector2(-rowIcon * 0.85f, dest.y);
                if (_adoptStep == AdoptStep.Fly)
                {
                    float delay = 0.08f * i;
                    if (_adoptClock < delay)
                    {
                        _adoptShown[i] = from;
                        landed = false;
                    }
                    else
                    {
                        _adoptFly[i] = Mathf.Min(1f, _adoptFly[i] + dt / 0.78f);
                        float u = Mathf.SmoothStep(0f, 1f, _adoptFly[i]);
                        var p = Vector2.Lerp(from, dest, u);
                        p.y -= Mathf.Sin(u * Mathf.PI) * 24f * s;
                        _adoptShown[i] = p;
                        if (_adoptFly[i] < 1f) landed = false;
                    }
                }
                else
                {
                    float k = 1f - Mathf.Exp(-dt / 0.14f);
                    _adoptShown[i] = Vector2.Lerp(_adoptShown[i], dest, k);
                }
                if (_adoptHop[i] > 0f)
                    _adoptHop[i] = Mathf.Max(0f, _adoptHop[i] - dt / 0.36f);
            }
            if (_adoptStep == AdoptStep.Fly && landed)
                _adoptStep = AdoptStep.Pick;
        }

        void AdoptChoose(int i)
        {
            if (i < 0 || i > 4) return;
            if (_adoptStep != AdoptStep.Pick && _adoptStep != AdoptStep.Name) return;
            var col = (BirdColor)i;
            string prev = AvatarSuggestion(_adoptCol);
            bool untouched = _adoptStep != AdoptStep.Name || _adoptName == prev || string.IsNullOrEmpty(_adoptName);
            _adoptCol = col;
            _adoptPick = i;
            _adoptHop[i] = 1f;
            // The hop picks who is speaking. Yes is what opens the name.
            Sfx.Chirp(col);
            if (untouched)
                _adoptName = AvatarSuggestion(col);
        }

        void AdoptYes()
        {
            if (_adoptStep != AdoptStep.Pick) return;
            if (_adoptPick < 0 || _adoptPick > 4)
            {
                _adoptPick = 1;
                _adoptCol = BirdColor.Gold;
            }
            _adoptCol = (BirdColor)_adoptPick;
            _adoptStep = AdoptStep.Name;
            _adoptName = AvatarSuggestion(_adoptCol);
            _adoptArmFocus = true;
            _gloveReady = false;
            _gloveVis = false;
            Sfx.Chirp(_adoptCol);
        }

        // This splash stands down. The next ShowSplash asks again.
        void AdoptNotYet()
        {
            if (_adoptStep != AdoptStep.Pick && _adoptStep != AdoptStep.Fly) return;
            _adoptSkip = true;
            _adoptLive = false;
            _adoptStep = AdoptStep.Off;
            _adoptPick = -1;
            _gloveVis = false;
            _gloveReady = false;
            _coachFade = 0f;
            GUIUtility.keyboardControl = 0;
            Sfx.CardTap();
        }

        void BeginSettle()
        {
            for (int i = 0; i < 5; i++)
                _adoptLeaveFrom[i] = _adoptShown[i];
            _adoptFromIcon = _adoptIcon;
            _adoptStep = AdoptStep.Settle;
            _adoptSettle = 0f;
            _gloveVis = false;
            _gloveReady = false;
            GUIUtility.keyboardControl = 0;
        }

        void FinishAdopt()
        {
            if (_adoptStep != AdoptStep.Settle) return;
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
                caption = _adoptLineR.width > 2f
                    ? _adoptLineR
                    : new Rect(0f, 0f, 0f, 0f);
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
            bool asking = _adoptStep == AdoptStep.Fly || _adoptStep == AdoptStep.Pick;
            bool naming = _adoptStep == AdoptStep.Name;

            float textH = Mathf.Clamp(50f * s, 42f * s, 66f * s);
            float capW = Mathf.Min(Mathf.Max(120f, width - 8f), 440f * s);
            caption = new Rect(left + (width - capW) * 0.5f, top, capW, textH);
            _adoptLineR = caption;

            _adoptYesR = default;
            _adoptNoR = default;
            float btnH = asking ? 42f * s : 0f;
            float btnGap = asking ? 8f * s : 0f;
            if (asking)
            {
                float yesW = Mathf.Min(88f * s, capW * 0.32f);
                float noW = Mathf.Min(146f * s, capW * 0.50f);
                float gap = 10f * s;
                float chips = yesW + noW + gap;
                float room = capW - 4f;
                if (chips > room && chips > 1f)
                {
                    float k = room / chips;
                    yesW *= k;
                    noW *= k;
                    gap *= k;
                    chips = yesW + noW + gap;
                }
                float bx = caption.center.x - chips * 0.5f;
                float by = caption.yMax + btnGap;
                _adoptYesR = new Rect(bx, by, yesW, btnH);
                _adoptNoR = new Rect(bx + yesW + gap, by, noW, btnH);
            }

            float bubbleBottom = asking ? _adoptNoR.yMax : caption.yMax;
            // The shared plate pads past the words. Keep the branch under that pad.
            float branchTop = bubbleBottom + 22f;
            float nameH = naming ? 46f * s : 0f;
            float nameGap = naming ? 10f * s : 0f;
            float branchBot = bot - nameH - nameGap;
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
            if (!naming) return;
            float ky = row.yMax + nameGap;
            if (ky + nameH > bot) ky = Mathf.Max(row.yMax + 4f, bot - nameH);
            float keepW = 78f * s;
            keep = new Rect(right - keepW, ky, keepW, nameH);
            field = new Rect(left, ky, Mathf.Max(48f, keep.x - left - 8f * s), nameH);
        }

        void DrawAdoptScene(float s)
        {
            if (!_adoptLive && _adoptStep != AdoptStep.Settle) return;
            AdoptLayout(s, out _, out _, out float icon, out var field, out var keep);
            bool ask = _adoptStep == AdoptStep.Pick;
            bool yesHeld = false;
            bool noHeld = false;
            bool yes = false;
            bool no = false;
            if (ask)
            {
                yes = HitPad(_adoptYesR, out yesHeld);
                no = HitPad(_adoptNoR, out noHeld);
            }
            if (no)
            {
                AdoptNotYet();
                return;
            }
            if (yes)
            {
                AdoptYes();
                AdoptLayout(s, out _, out _, out icon, out field, out keep);
                ask = false;
            }
            if (_adoptStep == AdoptStep.Pick || _adoptStep == AdoptStep.Name)
                HitAdoptBirds(icon);
            if (_adoptStep == AdoptStep.Settle)
                FillAvatarPerches(s, AvatarIcon(s));
            if (_adoptStep != AdoptStep.Settle || _adoptSettle < 0.45f)
                DrawLimb(_adoptWood, _adoptWoodW, _adoptWoodH, false, _adoptPx);
            if (_adoptStep == AdoptStep.Settle)
                DrawHomeLimbs();
            DrawAdoptBirds(s, icon);
            if (_adoptStep != AdoptStep.Settle && _adoptStep != AdoptStep.Fly)
            {
                string line = _adoptStep == AdoptStep.Name ? AdoptNameLine : AdoptHello;
                DrawAdoptBubble(line, s);
            }
            if (ask)
            {
                DrawPlaqueChip(_adoptYesR, AdoptYesLabel, s, yesHeld);
                DrawPlaqueChip(_adoptNoR, AdoptLaterLabel, s, noHeld);
            }
            if (_adoptStep == AdoptStep.Name)
                DrawAdoptName(s, field, keep);
            if (_adoptStep == AdoptStep.Settle && _adoptSettle >= 1f && GuiPaint())
                FinishAdopt();
        }

        void DrawAdoptBubble(string line, float s)
        {
            var bubble = _adoptLineR;
            if (_adoptYesR.width > 2f) bubble = UnionRect(bubble, _adoptYesR);
            if (_adoptNoR.width > 2f) bubble = UnionRect(bubble, _adoptNoR);
            if (bubble.width < 2f) return;
            DrawCoachPanel(bubble, EaseOutCubic(_coachFade));
            var text = _adoptLineR;
            var st = CoachLineStyle();
            int hi = Mathf.Max(18, Mathf.RoundToInt(32f * s));
            st.fontSize = FitFontWrapped(st, line, text.width, text.height, 15, hi);
            int black = Mathf.Clamp(Mathf.CeilToInt(CoachOutlinePx * s), CoachOutlinePx, 8);
            StampOutlined(text, line, st, new Color(1f, 0.98f, 0.90f, EaseOutCubic(_coachFade)), 0, black);
        }

        void DrawAdoptName(float s, Rect field, Rect keep)
        {
            var st = AvatarFieldStyle(s);
            GUI.SetNextControlName("avatar-name");
            _adoptName = GUI.TextField(field, _adoptName ?? "", AvatarNameMax, st);
            if (_adoptArmFocus)
            {
                GUI.FocusControl("avatar-name");
                _adoptArmFocus = false;
            }
            bool held = false;
            bool fire = HitPad(keep, out held);
            DrawKeepChip(keep, s, held);
            if (!fire) return;
            string n = ClipAvatarName(_adoptName);
            if (n.Length == 0 || AvatarNameDirty(n))
            {
                _adoptName = AvatarSuggestion(_adoptCol);
                _adoptArmFocus = true;
                return;
            }
            _adoptName = n;
            SaveAvatar(_adoptCol, n);
            BeginSettle();
        }

        void HitAdoptBirds(float icon)
        {
            float s = Mathf.Max(Screen.height / 720f, 1f);
            float pad = 4f * s;
            for (int i = 0; i < 5; i++)
            {
                var c = _adoptShown[i];
                var r = new Rect(c.x - icon * 0.5f - pad, c.y - icon * 0.5f - pad, icon + pad * 2f, icon + pad * 2f);
                if (!HitPad(r, out _)) continue;
                AdoptChoose(i);
                return;
            }
        }

        void DrawAdoptBirds(float s, float icon)
        {
            float drawIcon = icon;
            if (_adoptStep == AdoptStep.Settle)
            {
                float homeIcon = _avatarFitIcon > 1f ? _avatarFitIcon : AvatarIcon(s);
                drawIcon = Mathf.Lerp(_adoptFromIcon, homeIcon, Mathf.SmoothStep(0f, 1f, _adoptSettle));
            }
            for (int i = 0; i < 5; i++)
            {
                if (_adoptStep == AdoptStep.Settle && i != _adoptPick && _adoptSettle > 0.92f) continue;
                bool chosen = i == _adoptPick && _adoptPick >= 0;
                float hop = _adoptHop[i] > 0f ? Mathf.Sin((1f - _adoptHop[i]) * Mathf.PI) * 14f * s : 0f;
                float size = _adoptStep == AdoptStep.Settle
                    ? (chosen ? drawIcon : _adoptFromIcon)
                    : icon;
                bool wings = (_adoptStep == AdoptStep.Fly && _adoptFly[i] < 1f)
                    || (_adoptStep == AdoptStep.Settle && _adoptSettle < 0.98f)
                    || _adoptHop[i] > 0.05f;
                float bob = wings ? 0f : Mathf.Sin((_adoptClock + i * 0.4f) * 1.7f) * Mathf.Min(4f * s, size * 0.04f);
                var c = new Vector2(_adoptShown[i].x, _adoptShown[i].y - bob - hop);
                bool faceLeft = false;
                if (_adoptStep == AdoptStep.Settle && chosen)
                    faceLeft = AvatarHomePoint().x < _adoptLeaveFrom[i].x;
                var prev = GUI.color;
                if (_adoptPick >= 0 && !chosen && (_adoptStep == AdoptStep.Pick || _adoptStep == AdoptStep.Name))
                    GUI.color = new Color(1f, 1f, 1f, 0.5f);
                else
                    GUI.color = Color.white;
                float clock = _adoptClock + i * 0.17f;
                DrawCatalogBird((BirdColor)i, c, size, faceLeft, wings, clock);
                GUI.color = prev;
                if (chosen && _adoptStep == AdoptStep.Settle)
                    DrawAvatarPlate(c, drawIcon, _adoptName, s);
            }
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
            if (_avatarRename || _adoptLive || !AvatarAdopted()) return;
            _avatarRename = true;
            _avatarRenameText = SavedAvatarName();
            _avatarRenameFocus = true;
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
                _avatarRenameText = SavedAvatarName();
                _avatarRenameFocus = true;
                return;
            }
            PlayerPrefs.SetString(AvatarNamePref, n);
            PlayerPrefs.Save();
            CloseAvatarRename();
        }

        void AvatarRenameLayout(float s, out Rect line, out Rect field, out Rect keep)
        {
            AvatarChannel(s, out float left, out float right);
            float titleBottom = TopHud() + (56f * 2f + 4f) * s;
            var flower = FlowerPlayRect();
            float top = titleBottom + 18f;
            float bot = flower.yMin - 18f;
            float width = Mathf.Max(80f, right - left);
            float innerW = Mathf.Min(width - 36f, 300f * s);
            float innerH = 112f * s;
            float x = left + (width - innerW) * 0.5f;
            float y = top + Mathf.Max(0f, (bot - top - innerH) * 0.4f);
            if (y + innerH > bot) y = Mathf.Max(top, bot - innerH);
            var inner = new Rect(x, y, innerW, innerH);
            _avatarRenameR = CoachPanelRect(inner);
            line = new Rect(inner.x + 8f, inner.y + 8f * s, inner.width - 16f, 36f * s);
            float rowH = 40f * s;
            float rowY = inner.yMax - rowH - 12f * s;
            float keepW = 74f * s;
            keep = new Rect(inner.xMax - keepW - 10f * s, rowY, keepW, rowH);
            field = new Rect(inner.x + 10f * s, rowY, Mathf.Max(40f, keep.x - inner.x - 18f * s), rowH);
        }

        void DrawAvatarRename(float s)
        {
            if (!_avatarRename) return;
            AvatarRenameLayout(s, out var line, out var field, out var keep);
            var inner = new Rect(
                _avatarRenameR.x + 18f,
                _avatarRenameR.y + 12f,
                _avatarRenameR.width - 36f,
                _avatarRenameR.height - 24f);
            DrawCoachPanel(inner, 1f);
            var st = CoachLineStyle();
            st.fontSize = FitFont(st, AvatarRenameLine, line.width, line.height, 14, Mathf.RoundToInt(26f * s));
            StampOutlined(line, AvatarRenameLine, st, new Color(1f, 0.98f, 0.90f, 1f), 0,
                Mathf.Clamp(Mathf.CeilToInt(CoachOutlinePx * s * 0.45f), 2, 4));

            var fieldSt = AvatarFieldStyle(s);
            GUI.SetNextControlName("avatar-name");
            _avatarRenameText = GUI.TextField(field, _avatarRenameText ?? "", AvatarNameMax, fieldSt);
            if (_avatarRenameFocus)
            {
                GUI.FocusControl("avatar-name");
                _avatarRenameFocus = false;
            }
            bool held = false;
            bool fire = HitPad(keep, out held);
            DrawKeepChip(keep, s, held);
            if (fire) CommitAvatarRename();
        }

        void DrawKeepChip(Rect keep, float s, bool held) => DrawPlaqueChip(keep, "Keep", s, held);

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
            float w = Mathf.Max(54f * s, icon * 0.92f);
            float h = AvatarPlateH(s);
            var r = new Rect(c.x - w * 0.5f, c.y + icon * 0.5f + 4f * s, w, h);
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
            if (_adoptStep == AdoptStep.Name)
            {
                AdoptLayout(s, out _, out _, out _, out _, out var keep);
                aim = TopTouch(keep);
                return keep.width > 2f && GloveWouldClear(aim, s);
            }
            if (_adoptStep == AdoptStep.Pick)
            {
                AdoptLayout(s, out _, out _, out _, out _, out _);
                aim = TopTouch(_adoptYesR);
                return _adoptYesR.width > 2f && GloveWouldClear(aim, s);
            }
            return false;
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
            AdoptLayout(s, out var caption, out _, out float icon, out var field, out _);
            if (HitsExceptAim(CoachPanelRect(caption), box, aim)) return true;
            if (HitsExceptAim(_adoptNoR, box, aim)) return true;
            if (field.width > 2f && HitsExceptAim(field, box, aim)) return true;
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
            float pad = 4f * s;
            for (int i = 0; i < 5; i++)
            {
                if (_adoptStep == AdoptStep.Name && i != _adoptPick) continue;
                var c = _adoptShown[i];
                var bird = new Rect(c.x - icon * 0.5f - pad, c.y - icon * 0.5f - pad, icon + pad * 2f, icon + pad * 2f);
                if (HitsExceptAim(bird, box, aim)) return true;
            }
            return false;
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
                var tex = AvatarFieldTex();
                _avatarField = new GUIStyle(GUI.skin.textField)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    clipping = TextClipping.Clip,
                    border = new RectOffset(12, 12, 12, 12),
                    padding = new RectOffset(8, 8, 4, 4)
                };
                _avatarField.normal.background = tex;
                _avatarField.focused.background = tex;
                _avatarField.hover.background = tex;
                _avatarField.active.background = tex;
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

        static void DrawSliced(Texture2D tex, Rect box, float srcRad, float dstRad, Color tint)
        {
            if (tex == null || box.width < 2f || box.height < 2f) return;
            var prev = GUI.color;
            GUI.color = tint;
            float u = srcRad / tex.width;
            float v = srcRad / tex.height;
            float cw = Mathf.Min(dstRad, box.width * 0.5f);
            float ch = Mathf.Min(dstRad, box.height * 0.5f);
            var bl = new Rect(0f, 0f, u, v);
            var br = new Rect(1f - u, 0f, u, v);
            var tl = new Rect(0f, 1f - v, u, v);
            var tr = new Rect(1f - u, 1f - v, u, v);
            GUI.DrawTextureWithTexCoords(new Rect(box.x, box.yMax - ch, cw, ch), tex, bl);
            GUI.DrawTextureWithTexCoords(new Rect(box.xMax - cw, box.yMax - ch, cw, ch), tex, br);
            GUI.DrawTextureWithTexCoords(new Rect(box.x, box.y, cw, ch), tex, tl);
            GUI.DrawTextureWithTexCoords(new Rect(box.xMax - cw, box.y, cw, ch), tex, tr);
            var edgeH = new Rect(u, 0.40f, Mathf.Max(0.04f, 1f - 2f * u), 0.20f);
            var edgeV = new Rect(0.40f, v, 0.20f, Mathf.Max(0.04f, 1f - 2f * v));
            float midW = Mathf.Max(0f, box.width - cw * 2f);
            float midH = Mathf.Max(0f, box.height - ch * 2f);
            GUI.DrawTextureWithTexCoords(new Rect(box.x + cw, box.yMax - ch, midW, ch), tex, edgeH);
            GUI.DrawTextureWithTexCoords(new Rect(box.x + cw, box.y, midW, ch), tex, edgeH);
            GUI.DrawTextureWithTexCoords(new Rect(box.x, box.y + ch, cw, midH), tex, edgeV);
            GUI.DrawTextureWithTexCoords(new Rect(box.xMax - cw, box.y + ch, cw, midH), tex, edgeV);
            var mid = new Rect(u, v, Mathf.Max(0.04f, 1f - 2f * u), Mathf.Max(0.04f, 1f - 2f * v));
            GUI.DrawTextureWithTexCoords(new Rect(box.x + cw, box.y + ch, midW, midH), tex, mid);
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
        static Vector2 LimbBird(Vector2 center, float px, bool fromRight, int seat)
        {
            if (seat < 0) seat = 0;
            if (seat > 4) seat = 4;
            var local = WorldBuilder.SeatLocal(seat, false, fromRight);
            float y = local.y + BranchView.RestLift;
            return new Vector2(center.x + local.x * px, center.y - y * px);
        }

        void DrawHomeLimbs()
        {
            if (_limbOn)
                DrawLimb(_limbCenter, _limbW, _limbH, _limbFromRight, _limbPx);
            if (_awayOn)
                DrawLimb(_awayCenter, _awayW, _awayH, _awayFromRight, _awayPx);
        }

        void DrawLimb(Vector2 center, float w, float h, bool fromRight, float px)
        {
            if (w < 4f || h < 4f) return;
            var wood = SpriteCatalog.Branch;
            if (wood == null || wood.texture == null) return;
            var r = new Rect(center.x - w * 0.5f, center.y - h * 0.5f, w, h);
            var prev = GUI.color;
            GUI.color = Color.white;
            if (fromRight)
            {
                var m = GUI.matrix;
                GUIUtility.ScaleAroundPivot(new Vector2(-1f, 1f), r.center);
                GUI.DrawTexture(r, wood.texture, ScaleMode.StretchToFill, true);
                GUI.matrix = m;
            }
            else
                GUI.DrawTexture(r, wood.texture, ScaleMode.StretchToFill, true);
            DrawLimbLeaves(center, h, fromRight, px);
            GUI.color = prev;
        }

        // Catalog leaves sprout from the bark outside the outer pads. The middle
        // keeps the orchids already painted on branch.png, and the bird stays in front.
        void DrawLimbLeaves(Vector2 center, float woodH, bool fromRight, float px)
        {
            var leaf = SpriteCatalog.Leaf;
            if (leaf == null || leaf.texture == null || px < 0.5f) return;
            float lh = Mathf.Clamp(px * 0.62f, woodH * 0.22f, woodH * 0.48f);
            float t = Time.unscaledTime;
            DrawOneLeaf(leaf, center, px, fromRight, 0, -0.28f, lh * 0.92f, -16f, t, 0);
            DrawOneLeaf(leaf, center, px, fromRight, 0, 0.22f, lh * 0.70f, 18f, t, 1);
            DrawOneLeaf(leaf, center, px, fromRight, 4, 0.08f, lh * 0.66f, -12f, t, 2);
            DrawOneLeaf(leaf, center, px, fromRight, 4, 0.40f, lh * 0.84f, 22f, t, 3);
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
