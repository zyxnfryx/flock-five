using System.Collections.Generic;
using UnityEngine;

namespace FlockFive
{
    // Opaque wingspan of one sprite, in GUI y-down. In-game named birds pass the
    // camera screen rect of that sprite as the sheet. Home and adopt use the same
    // quad DrawCatalogBird draws, so flying and resting do not share one box.
    public static class BirdNameTag
    {
        public struct Span
        {
            public float U0, U1, V0, V1;
            public bool Ok;
            public float MidU => (U0 + U1) * 0.5f;
            public float BotV => V1;
        }

        static readonly Dictionary<int, Span> _span = new Dictionary<int, Span>();

        public static bool TrySpan(Sprite spr, out Span span)
        {
            span = default;
            var tex = spr != null ? spr.texture : null;
            if (tex == null || !tex.isReadable || tex.width < 2 || tex.height < 2) return false;
            int id = tex.GetInstanceID();
            if (id != 0 && _span.TryGetValue(id, out span)) return span.Ok;
            span = Measure(tex);
            if (id != 0) _span[id] = span;
            return span.Ok;
        }

        // Alpha above the fringe. A wide sheet is sampled every other pixel.
        static Span Measure(Texture2D tex)
        {
            var span = new Span();
            int w = tex.width;
            int h = tex.height;
            var px = tex.GetPixels32();
            if (px == null || px.Length < (long)w * h) return span;
            int step = (w > 800 || h > 800) ? 2 : 1;
            int minX = w, maxX = -1, minY = h, maxY = -1;
            for (int y = 0; y < h; y += step)
            {
                int row = y * w;
                for (int x = 0; x < w; x += step)
                {
                    if (px[row + x].a <= 40) continue;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
            if (maxX < 0) return span;
            int pad = step - 1;
            if (minX > pad) minX -= pad;
            else minX = 0;
            if (minY > pad) minY -= pad;
            else minY = 0;
            maxX = Mathf.Min(w - 1, maxX + pad);
            maxY = Mathf.Min(h - 1, maxY + pad);
            span.U0 = minX / (float)w;
            span.U1 = (maxX + 1) / (float)w;
            span.V0 = 1f - (maxY + 1) / (float)h;
            span.V1 = 1f - minY / (float)h;
            span.Ok = span.U1 > span.U0 && span.V1 > span.V0;
            return span;
        }

        public static bool TryVisual(Sprite spr, Rect sheet, bool faceLeft, out Rect visual)
        {
            visual = default;
            if (!TrySpan(spr, out var span) || sheet.width < 1f || sheet.height < 1f) return false;
            var tex = spr.texture;
            var fit = FitSheet(sheet, tex.width, tex.height);
            float u0 = span.U0;
            float u1 = span.U1;
            if (faceLeft)
            {
                float flip = 1f - u1;
                u1 = 1f - u0;
                u0 = flip;
            }
            float x0 = fit.x + u0 * fit.width;
            float x1 = fit.x + u1 * fit.width;
            float y0 = fit.y + span.V0 * fit.height;
            float y1 = fit.y + span.V1 * fit.height;
            if (x1 < x0)
            {
                float swap = x0;
                x0 = x1;
                x1 = swap;
            }
            visual = new Rect(x0, y0, Mathf.Max(1f, x1 - x0), Mathf.Max(1f, y1 - y0));
            return true;
        }

        // Same letterbox as GUI.DrawTexture ScaleToFit.
        static Rect FitSheet(Rect sheet, float texW, float texH)
        {
            if (texW < 1f || texH < 1f || sheet.width < 1f || sheet.height < 1f) return sheet;
            float sa = sheet.width / sheet.height;
            float ta = texW / texH;
            if (ta > sa)
            {
                float h = sheet.width / ta;
                return new Rect(sheet.x, sheet.y + (sheet.height - h) * 0.5f, sheet.width, h);
            }
            float w = sheet.height * ta;
            return new Rect(sheet.x + (sheet.width - w) * 0.5f, sheet.y, w, sheet.height);
        }

        public static Rect Place(float centerX, float visualBottom, float width, float height, float gap)
        {
            if (width < 1f) width = 1f;
            if (height < 1f) height = 1f;
            return new Rect(centerX - width * 0.5f, visualBottom + gap, width, height);
        }

        // Pose offset from the bird center, not a screen point. A glide then
        // carries the tag with the bird, and flap frames do not buzz it.
        public static void Follow(ref float x, ref float bottom, ref bool ready, float wantX, float wantBottom, float dt)
        {
            if (!ready)
            {
                x = wantX;
                bottom = wantBottom;
                ready = true;
                return;
            }
            if (dt <= 0f) return;
            float k = 1f - Mathf.Exp(-6f * dt);
            x += (wantX - x) * k;
            bottom += (wantBottom - bottom) * k;
        }
    }

    // Home bird stays off until level 1 is cleared (flockfive.next >= 1) and the
    // player has adopted. The first splash: the bird flies beside the right-hand
    // twig, a tap lands it, then the shared glove points at the perched bird.
    // The twig is a FromRight limb: root off the right edge, bird facing center.
    // Done on the name page leaves it there. Daily's glove waits until that
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
        // Tip pad of branch.png (the outer end). The splash draws the limb mirrored
        // (FromRight), so this end points toward the screen and the root stays on
        // the right edge. Seat 2 is the middle pad and is not the home perch.
        const int HomeBranchTip = 4;
        static int HomeBranchAnchor() => HomeBranchTip;
        // Root through the tip. Pixels, origin top-left of the sheet. The cut root
        // is the low-x end; the draw mirror puts that end off the right edge.
        const float HomeTwigX0 = 40f;
        const float HomeTwigX1 = 1240f;
        const float HomeTwigTop = 200f;
        const float HomeTwigBot = 490f;
        const float HomeTwigTexW = 1280f;
        const float HomeTwigTexH = 720f;
        // 2.75x sits inside the 2.5–3x ask. Kept so older notes still name it.
        const float AvatarGrow = 2.75f;
        // Home splash body. 1.48× a garden bird (1024px @ 280ppu × BirdScale).
        // ~14% up from 1.30. Feet stay on the pad: LimbBirdSeated lifts the
        // center by RestLift × birdPx, then PerchAnchor sinks a fixed fraction.
        // First sight and the bird after naming share this. Swatches do not.
        const float HomeAvatarMul = 1.48f;
        // Flap sheets are 1536px on the 1024 rest canvas, same ppu. DrawCatalogBird
        // grows the quad by this, so a waiting center needs the wider reach.
        const float HomeFlapFit = 1536f / 1024f;
        // Home twig scale. Half a garden limb (WoodScaleX by 0.50). PlaceHomeBranch
        // uses this once and does not grow the wood to clear the rail.
        const float AvatarBranchMul = 0.5f;
        const float AvatarPlateMul = 0.65f;

        static readonly string[] AvatarNames = { "Rosie", "Goldie", "Pip", "Plum", "Peach" };
        static readonly string[] AvatarBad =
        {
            "fuck", "shit", "bitch", "asshole", "bastard", "damn", "crap", "piss",
            "dick", "cunt", "slut", "whore"
        };

        enum AdoptStep { Off, Fly, Look, Settle }

        // Off the wood the avatar flies. Landing ends on the perch, then the
        // glove may point. Folded wings are only Perched or GlovePointing.
        enum AvatarPose { Flying, Landing, Perched, GlovePointing }

        AdoptStep _adoptStep;
        AvatarPose _avatarPose = AvatarPose.Perched;
        bool _adoptOwed;
        bool _adoptLive;
        Rect _adoptLineR;
        float _adoptPx;
        int _adoptPick = -1;
        float _adoptClock;
        float _adoptIcon;
        // Daily's glove and caption wait until the adopted bird is perched.
        bool _birdSettledOnBranch = true;
        BirdColor _adoptCol = BirdColor.Gold;
        string _adoptName = "";
        readonly float[] _adoptHop = new float[5];
        readonly Vector2[] _adoptShown = new Vector2[5];
        Vector2 _avatarLandFrom;
        Vector2 _avatarLandTo;
        float _avatarLandT;
        float _avatarPerchHold;
        bool _avatarLandGlove;
        // Greet caption has been dismissed. The look line is up while the bird
        // may still be flying to the tip. A LEVEL tap sets this and does not Load.
        bool _adoptGreetDone;
        Rect _adoptBirdR;

        bool _avatarRename;
        bool _avatarRenameFocus;
        string _avatarRenameText = "";
        Rect _avatarRenameR;
        Rect _avatarPlateR;
        // Pose offset from the bird center. Not a screen point, so a glide
        // does not leave the tag behind.
        float _nameTagDx;
        float _nameTagDy;
        bool _nameTagReady;
        int _nameTagFrame = -1;
        float _adoptTagDx;
        float _adoptTagDy;
        bool _adoptTagReady;
        int _adoptTagFrame = -1;
        float _avatarTail;
        int _avatarOpenFrame = -1;
        int _avatarKitN;
        readonly Rect[] _avatarSwatch = new Rect[5];
        readonly Rect[] _avatarKitR = new Rect[3];
        readonly int[] _avatarKitId = new int[3];

        static int _avatarBowArt = -1;
        static int _avatarCrownArt = -1;

        static Texture2D _avatarPlateTex;
        static Texture2D _avatarFieldTex;
        static GUIStyle _avatarField;
        static GUIContent _avatarTagContent;

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

        // The open Daily card holds the bird on the fixed pad. It does not resize it.
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

        static string AvatarTagName(string name, BirdColor col)
        {
            if (string.IsNullOrEmpty(name)) name = AvatarSuggestion(col);
            name = ClipAvatarName(name);
            if (name.Length == 0) name = AvatarSuggestion(col);
            return name;
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
            _adoptPick = 1;
            _avatarPose = AvatarPose.Flying;
            _avatarLandGlove = false;
            _avatarPerchHold = 0f;
            _adoptGreetDone = false;
            _adoptBirdR = default;
            _birdSettledOnBranch = false;
            _adoptCol = BirdColor.Gold;
            _adoptName = AvatarSuggestion(_adoptCol);
            _coachFade = 0f;
            _gloveReady = false;
            _gloveVis = false;
            CloseAvatarRename();
            // Same prefs the old accept path wrote, so a later splash does not ask again.
            MarkAdopted(_adoptCol, _adoptName);
            float s = Mathf.Max(Screen.height / 720f, 1f);
            var seed = HomeWanderBounds(s, HomeAvatarIcon(s)).center;
            for (int i = 0; i < 5; i++)
            {
                _adoptHop[i] = 0f;
                _adoptShown[i] = seed;
            }
        }

        void TickAdoptMotion(float dt)
        {
            float s = Mathf.Max(Screen.height / 720f, 1f);
            _avatarTail = AvatarPlateDrop(s);
            int pick = _adoptPick;
            if (pick < 0 || pick > 4) pick = 1;
            PlaceHomeBranch(s);
            float icon = HomeAvatarIcon(s);
            _adoptIcon = icon;
            _adoptPx = _limbPx;
            var perch = AvatarHomePoint();

            if (_avatarPose == AvatarPose.Flying)
            {
                var box = HomeWanderBounds(s, icon);
                var p = HomeWanderPoint(box, _adoptClock, out bool face);
                _adoptShown[pick] = p;
                _avatarPos = p;
                _avatarFaceLeft = face;
            }
            else if (_avatarPose == AvatarPose.Landing)
            {
                StepAvatarLand(dt, s);
                _adoptShown[pick] = _avatarPos;
            }
            else
            {
                _adoptShown[pick] = perch;
                _avatarPos = perch;
                _avatarPerchIx = HomeBranchAnchor();
                _avatarFaceLeft = perch.x >= Screen.width * 0.5f;
                if (_avatarPose == AvatarPose.Perched && _avatarLandGlove)
                {
                    _avatarPerchHold -= dt;
                    if (_avatarPerchHold <= 0f)
                    {
                        _avatarPose = AvatarPose.GlovePointing;
                        _adoptStep = AdoptStep.Look;
                        _avatarLandGlove = false;
                        _gloveReady = false;
                        _gloveVis = false;
                    }
                }
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
            _adoptStep = AdoptStep.Settle;
            _avatarPose = AvatarPose.Perched;
            _avatarLandGlove = false;
            _gloveVis = false;
            _gloveReady = false;
            GUIUtility.keyboardControl = 0;
            PlaceHomeBranch(Mathf.Max(Screen.height / 720f, 1f));
            _avatarPos = AvatarHomePoint();
            FinishAdopt();
        }

        void FinishAdopt()
        {
            if (_adoptStep != AdoptStep.Settle) return;
            _birdSettledOnBranch = true;
            float s = Mathf.Max(Screen.height / 720f, 1f);
            _avatarTail = AvatarPlateDrop(s);
            FillAvatarPerches(s);
            _avatarPos = AvatarHomePoint();
            _avatarPerchIx = HomeBranchAnchor();
            _avatarPlaced = true;
            _avatarCross = false;
            _avatarCrossing = false;
            _avatarGliding = false;
            _avatarHold = false;
            _avatarTutor = false;
            _avatarFaceLeft = _avatarPos.x >= Screen.width * 0.5f;
            _avatarHappy = 0f;
            _avatarFlapT = 0f;
            _avatarPreenT = 0f;
            _avatarCol = _adoptCol;
            _avatarPose = AvatarPose.Perched;
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

        static Rect AdoptCaptionRect(float s)
        {
            float textH = Mathf.Clamp(84f * s, 68f * s, 120f * s);
            float capW = Mathf.Min(460f * s, Mathf.Max(120f, Screen.width - 24f));
            return PlaceCaption(s, capW, textH, CaptionFloorY(s));
        }

        // Greet until a LEVEL tap or the perch glove. Each sentence latches once.
        string AdoptCaptionLine()
        {
            if (_adoptGreetDone || _avatarPose == AvatarPose.GlovePointing)
                return AdoptLookLine;
            return AdoptGreetLine;
        }

        bool AdoptGreetUp()
        {
            if (!_adoptLive || _avatarRename) return false;
            return AdoptCaptionLine() == AdoptGreetLine;
        }

        // Any tap during the greet (StepTapGate's any-tap step, GateAdoptGreet) dismisses that
        // sentence; the tap is eaten, so LEVEL does not start. In the air, the bird flies to
        // the tip and the glove poses fresh once it perches. On the wood, the glove step starts.
        void AdvanceAdoptGreet()
        {
            NoteLessonDismiss(CoachStep.AdoptGreet);
            _adoptGreetDone = true;
            int pick = _adoptPick;
            if (pick < 0 || pick > 4) pick = 1;
            if (_avatarPose == AvatarPose.Flying)
                BeginAvatarLand(_adoptShown[pick], AvatarHomePoint(), true);
            else if (_avatarPose != AvatarPose.Landing)
            {
                _avatarPose = AvatarPose.GlovePointing;
                _adoptStep = AdoptStep.Look;
                _avatarLandGlove = false;
                _gloveReady = false;
                _gloveVis = false;
            }
        }

        // Same latch SeatTutorialCaption uses for the greet and look lines, so the
        // branch, the bubble, and ClearGloveOfCaption share one plate.
        Rect SeatedAdoptCaption(float s)
        {
            var pref = AdoptCaptionRect(s);
            string line = AdoptCaptionLine();
            return SeatTutorialCaption(line, s, pref.y, pref.width, pref.height, pref.y, pref.x);
        }

        void AdoptLayout(float s, out Rect caption, out Rect row, out float icon, out Rect field, out Rect keep)
        {
            caption = SeatedAdoptCaption(s);
            _adoptLineR = caption;
            PlaceHomeBranch(s);
            icon = HomeAvatarIcon(s);
            _adoptIcon = icon;
            _adoptPx = _limbPx;
            HomeTwigRect(_limbCenter, _limbW, _limbH, out row);
            field = default;
            keep = default;
        }

        void DrawAdoptScene(float s)
        {
            if (!_adoptLive && _adoptStep != AdoptStep.Settle) return;
            AdoptLayout(s, out _, out _, out _, out _, out _);
            float greetIcon = HomeAvatarIcon(s);
            _adoptIcon = greetIcon;

            // Tap is claimed in HitAdoptTutor, before the LEVEL flower.
            DrawHomeLimbSide(false);
            DrawAdoptBirds(s, greetIcon);
            if (_avatarRename) return;
            DrawAdoptBubble(AdoptCaptionLine(), s);
            if (_avatarPose == AvatarPose.GlovePointing)
                DrawTutorOverlay(s);
        }

        void DrawAdoptBubble(string line, float s)
        {
            if (CoachResetHolds()) return;
            var bubble = _adoptLineR;
            if (bubble.width < 2f) return;
            NoteTutorPlate(CoachPanelRect(bubble));
            DrawCoachPanel(bubble, EaseOutCubic(_coachFade));
            var text = _adoptLineR;
            var st = CoachLineStyle();
            int hi = Mathf.Max(18, Mathf.RoundToInt(30f * s));
            st.fontSize = FitFontWrapped(st, line, text.width, text.height, 15, hi);
            StampBannerText(text, line, st, EaseOutCubic(_coachFade), true);
        }

        // Before DrawFlowerPlay. HitHomeFirst Uses the press so the LEVEL flower
        // cannot arm on that finger. Look opens the closer-look card. Fly lands.
        void HitAdoptTutor(float s)
        {
            if (!_adoptLive || _avatarRename) return;
            int pick = _adoptPick;
            if (pick < 0 || pick > 4) return;
            float icon = HomeAvatarIcon(s);
            if (_avatarPose == AvatarPose.Flying)
            {
                if (HitAdoptBird(pick, icon))
                    BeginAvatarLand(_adoptShown[pick], AvatarHomePoint(), true);
                return;
            }
            if (_avatarPose == AvatarPose.GlovePointing)
                HitAdoptBirds(icon);
        }

        // Same center DrawAdoptBirds uses, hop included. No bob: PerchAnchor is the
        // shared rest seat, so the adopt bird sits on the branch like the home bird.
        Vector2 AdoptBirdCenter(int i, float icon, float s, out bool onPerch)
        {
            onPerch = AvatarPoseOnWood() && _adoptHop[i] <= 0.05f;
            return PerchAnchor(_adoptShown[i], icon, onPerch, _adoptHop[i], 14f * s);
        }

        bool AdoptLookRects(float icon, out Rect bird, out Rect plate)
        {
            bird = default;
            plate = default;
            if (_avatarRename) return false;
            if (_adoptStep != AdoptStep.Look) return false;
            int i = _adoptPick;
            if (i < 0 || i > 4) return false;
            float s = Mathf.Max(Screen.height / 720f, 1f);
            float drawIcon = HomeAvatarIcon(s);
            if (drawIcon < 8f) drawIcon = icon;
            if (drawIcon < 8f) return false;
            float pad = HomeBirdHitPad * s;
            var c = AdoptBirdCenter(i, drawIcon, s, out bool onPerch);
            bird = new Rect(c.x - drawIcon * 0.5f - pad, c.y - drawIcon * 0.5f - pad, drawIcon + pad * 2f, drawIcon + pad * 2f);
            bool faceLeft = c.x >= Screen.width * 0.5f;
            bool wings = BirdIdle.UseFlyingPose(onPerch, false);
            float clock = _adoptClock + i * 0.17f;
            plate = NameTagRect(c, drawIcon, s, _adoptCol, _adoptName, faceLeft, wings, clock, false);
            return bird.width > 2f;
        }

        void HitAdoptBirds(float icon)
        {
            if (!AdoptLookRects(icon, out var r, out var plate)) return;
            bool birdHit = HitHomeFirst(r, out _);
            bool plateHit = plate.width > 2f && HitHomeFirst(plate, out _);
            if (!birdHit && !plateHit) return;
            int i = _adoptPick;
            _adoptHop[i] = 1f;
            Sfx.Chirp(_adoptCol);
            OpenAvatarRename();
        }

        void DrawAdoptBirds(float s, float icon)
        {
            _adoptBirdR = default;
            int i = _adoptPick;
            if (i < 0 || i > 4) return;
            float drawIcon = HomeAvatarIcon(s);
            if (drawIcon < 8f) drawIcon = icon;
            var c = AdoptBirdCenter(i, drawIcon, s, out bool onPerch);
            // In the air, face the way it is moving. On the wood, face center.
            // Right half faces left. DrawAvatarBird does the shared flip.
            bool traveling = _avatarPose == AvatarPose.Flying || _avatarPose == AvatarPose.Landing;
            bool faceLeft = traveling ? _avatarFaceLeft : c.x >= Screen.width * 0.5f;
            var prev = GUI.color;
            GUI.color = Color.white;
            float clock = _adoptClock + i * 0.17f;
            _adoptBirdR = DrawAvatarBird(_adoptCol, SavedAvatarKit(), c, drawIcon, faceLeft, onPerch, clock, false, BirdIdle.KitSlotAdopt);
            GUI.color = prev;
            if (!onPerch) return;
            bool wings = BirdIdle.UseFlyingPose(onPerch, false);
            DrawAvatarPlate(c, drawIcon, _adoptName, s, _adoptCol, faceLeft, wings, clock, false);
        }

        bool HitAdoptBird(int i, float icon)
        {
            if (i < 0 || i > 4) return false;
            float s = Mathf.Max(Screen.height / 720f, 1f);
            float pad = 8f * s;
            var c = _adoptShown[i];
            var r = new Rect(c.x - icon * 0.55f - pad, c.y - icon * 0.55f - pad, icon * 1.1f + pad * 2f, icon * 1.1f + pad * 2f);
            return HitHomeFirst(r, out _);
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
            // No hop. A hop clears the perch, opens the flap sheet (larger quad),
            // and lifts the bird into the leaves. Color keeps the seat, the 1.48
            // size, and the facing. A tap on the bird still hops.
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
            float innerW = Mathf.Min(width, StandardPopupWidth(s));
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
            NoteTutorPlate(_avatarRenameR);
            DrawCoachPanel(content, 1f);
            var st = CoachLineStyle();
            st.fontSize = FitFont(st, prompt, line.width, line.height, 14, Mathf.RoundToInt(26f * s));
            StampBannerText(line, prompt, st, 1f, true);

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
            }
            // buttonSeat on KitAnchor. All five wear the saved kit. The home bird does not.
            DrawAvatarBird(col, SavedAvatarKit(), c, icon, false, true, 0f, true);
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

        static Rect AvatarPlateRect(Vector2 c, float icon, float s)
        {
            float w = AvatarPlateW(icon, s);
            float h = AvatarPlateH(s);
            return new Rect(c.x - w * 0.5f, c.y + icon * 0.5f + 1f * s, w, h);
        }

        // Home name. 16px at a 720-tall screen (was 18, about 11% smaller).
        // Never under NameTagMinPx. Scales with s. The wood follows the glyphs.
        const float NameTagPt = 16f;
        const int NameTagMinPx = 16;

        static int NameTagFont(float s)
        {
            if (s < 1f) s = 1f;
            int px = Mathf.RoundToInt(NameTagPt * s);
            if (px < NameTagMinPx) px = NameTagMinPx;
            return px;
        }

        // Pad leaves room for the shared outline. The type stays at NameTagPt.
        static void NameTagBox(string name, float s, out float w, out float h)
        {
            var st = CoachLineStyle();
            bool wrap = st.wordWrap;
            st.wordWrap = false;
            st.fontStyle = FontStyle.Bold;
            st.fontSize = NameTagFont(s);
            float padX = 10f * s;
            float padY = 6f * s;
            if (_avatarTagContent == null) _avatarTagContent = new GUIContent();
            _avatarTagContent.text = name ?? "";
            var sz = st.CalcSize(_avatarTagContent);
            w = sz.x + padX * 2f;
            h = sz.y + padY * 2f;
            float minW = 25f * s;
            if (w < minW) w = minW;
            st.wordWrap = wrap;
        }

        // Hidden until the bird is named. Center and tail come from this pose's
        // opaque wingspan. The offset is smoothed once per frame.
        Rect NameTagRect(Vector2 c, float icon, float s, BirdColor col, string name, bool faceLeft, bool wings, float clock, bool homeTag)
        {
            if (AvatarNeedsName())
            {
                if (homeTag) _nameTagReady = false;
                else _adoptTagReady = false;
                return default;
            }
            name = AvatarTagName(name, col);
            var spr = SpriteCatalog.BirdFrame(col, wings ? clock * AvatarFlapRate : 0f, wings);
            var rest = SpriteCatalog.BirdFrame(col, 0f, false);
            if (spr == null || spr.texture == null)
                return AvatarPlateRect(c, icon, s);
            var sheet = CatalogBirdRect(spr, rest, c, icon);
            if (!BirdNameTag.TryVisual(spr, sheet, faceLeft, out var visual))
                return AvatarPlateRect(c, icon, s);
            float wantDx = visual.center.x - c.x;
            float wantDy = visual.yMax - c.y;
            float dx;
            float dy;
            int frame = Time.frameCount;
            if (homeTag)
            {
                if (_nameTagFrame != frame)
                {
                    BirdNameTag.Follow(ref _nameTagDx, ref _nameTagDy, ref _nameTagReady, wantDx, wantDy, Time.unscaledDeltaTime);
                    _nameTagFrame = frame;
                }
                dx = _nameTagDx;
                dy = _nameTagDy;
            }
            else
            {
                if (_adoptTagFrame != frame)
                {
                    BirdNameTag.Follow(ref _adoptTagDx, ref _adoptTagDy, ref _adoptTagReady, wantDx, wantDy, Time.unscaledDeltaTime);
                    _adoptTagFrame = frame;
                }
                dx = _adoptTagDx;
                dy = _adoptTagDy;
            }
            NameTagBox(name, s, out float w, out float h);
            return BirdNameTag.Place(c.x + dx, c.y + dy, w, h, 4f * s);
        }

        Rect DrawAvatarPlate(Vector2 c, float icon, string name, float s, BirdColor col, bool faceLeft, bool wings, float clock, bool homeTag)
        {
            // Suggested names are not the player's. The wooden sign waits for Done.
            name = AvatarTagName(name, col);
            var r = NameTagRect(c, icon, s, col, name, faceLeft, wings, clock, homeTag);
            if (r.width < 2f) return default;
            var st = CoachLineStyle();
            st.fontSize = NameTagFont(s);
            DrawNameplate(r, name, st, s);
            return r;
        }

        // Cream plaque, thin dark rim, inner highlight. Home bird and the intro share it.
        static void DrawNameplate(Rect r, string name, GUIStyle st, float s)
        {
            if (r.width < 2f || st == null || string.IsNullOrEmpty(name)) return;
            float rad = Mathf.Min(r.height * 0.48f, 14f * s);
            if (rad < 4f) rad = 4f;
            var shadow = new Rect(r.x + 1.5f * s, r.y + 2f * s, r.width, r.height);
            DrawSolidRound(shadow, rad, 0, 0.28f);
            DrawSolidRound(r, rad, 1, 1f);
            bool wrap = st.wordWrap;
            st.wordWrap = false;
            StampBannerText(r, name, st, 1f, true);
            st.wordWrap = wrap;
            // VIP: small gold crown on the tag's corner (draw-only, same rect and hit).
            DrawVipTagAccent(r, s);
        }

        // Sheet quad. Flap art is wider in world units than the rest pose, so the
        // drawn square grows by that ratio. Hit and the name tag share this.
        static Rect CatalogBirdRect(Sprite spr, Sprite rest, Vector2 c, float icon)
        {
            float iw = icon;
            if (spr != null && rest != null && rest != spr && rest.pixelsPerUnit > 0f && spr.pixelsPerUnit > 0f)
            {
                float restU = rest.rect.width / rest.pixelsPerUnit;
                float frameU = spr.rect.width / spr.pixelsPerUnit;
                if (restU > 0f) iw *= frameU / restU;
            }
            return new Rect(c.x - iw * 0.5f, c.y - iw * 0.5f, iw, iw);
        }

        Rect DrawCatalogBird(BirdColor col, Vector2 c, float icon, bool faceLeft, bool wings, float clock)
        {
            var spr = SpriteCatalog.BirdFrame(col, wings ? clock * AvatarFlapRate : 0f, wings);
            if (spr == null || spr.texture == null) return default;
            var rest = SpriteCatalog.BirdFrame(col, 0f, false);
            var r = CatalogBirdRect(spr, rest, c, icon);
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

        // Off the perch, wing frames. On the perch, folded wings. One path for
        // the adopt bird, the home bird, the name-dialog portrait, and the finale
        // rule BirdIdle.UseFlyingPose already applies to garden birds.
        Rect DrawAvatarBird(BirdColor col, int kit, Vector2 c, float icon, bool faceLeft, bool onPerch, float clock, bool buttonSeat = false, int glideSlot = -1)
        {
            bool wings = BirdIdle.UseFlyingPose(onPerch, false);
            return DrawDressedBird(col, kit, c, icon, faceLeft, wings, clock, buttonSeat, glideSlot);
        }

        // Bow sits behind the body. Crown sits in front. Neither path writes SexOf.
        // buttonSeat is the five color buttons. Home and adopt birds leave it false.
        Rect DrawDressedBird(BirdColor col, int kit, Vector2 c, float icon, bool faceLeft, bool wings, float clock, bool buttonSeat = false, int glideSlot = -1)
        {
            if (buttonSeat) glideSlot = -1;
            if (kit == 1) DrawAvatarKit(1, col, c, icon, faceLeft, wings, clock, buttonSeat, glideSlot);
            var drawn = DrawCatalogBird(col, c, icon, faceLeft, wings, clock);
            if (kit == 2) DrawAvatarKit(2, col, c, icon, faceLeft, wings, clock, buttonSeat, glideSlot);
            return drawn;
        }

        void DrawAvatarKit(int kit, BirdColor col, Vector2 c, float icon, bool faceLeft, bool wings, float clock, bool buttonSeat, int glideSlot)
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
            float t = wings ? clock * AvatarFlapRate : 0f;
            Sprite body;
            Sprite next;
            float phase;
            if (wings)
                SpriteCatalog.FlapPair(col, BirdSex.Neutral, t, true, out body, out next, out phase);
            else
            {
                // Same rest sprite DrawCatalogBird shows, including peach's female sheet.
                body = SpriteCatalog.BirdFrame(col, 0f, false);
                next = body;
                phase = 0f;
            }
            float lx, ly, fit, tilt;
            bool flip;
            if (glideSlot < 0)
                BirdIdle.KitAnchor(!bow, 0, faceLeft, body, spr, out lx, out ly, out fit, out tilt, out flip, buttonSeat);
            else
            {
                float dt = Time.unscaledDeltaTime;
                if (dt < 0f) dt = 0f;
                else if (dt > 0.05f) dt = 0.05f;
                BirdIdle.KitFollow(!bow, body, next, phase, faceLeft, spr, glideSlot, dt,
                    out lx, out ly, out fit, out tilt, out flip);
            }
            // One local unit in GUI pixels. The imported bird is 220 ppu on a 1024 rest
            // sheet. Flap quads grow with the sheet, so the rest sheet keeps the unit.
            var rest = SpriteCatalog.Bird(col);
            float ppu = rest != null && rest.pixelsPerUnit > 1f ? rest.pixelsPerUnit : 220f;
            float texW = rest != null && rest.rect.width > 1f ? rest.rect.width : 1024f;
            float unit = ppu * (icon / texW);
            float dw = (spr.rect.width / 200f) * fit * unit;
            float dh = (spr.rect.height / 200f) * fit * unit;
            if (dw < 1f || dh < 1f) return;
            float x = c.x + lx * unit;
            float y = c.y - ly * unit;
            var r = new Rect(x - dw * 0.5f, y - dh * 0.5f, dw, dh);
            var m = GUI.matrix;
            // Flip, then tilt. Same order as SpriteRenderer, so KitAnchor matches the garden.
            if (flip)
                GUIUtility.ScaleAroundPivot(new Vector2(-1f, 1f), r.center);
            if (Mathf.Abs(tilt) > 0.4f)
                GUIUtility.RotateAroundPivot(tilt, r.center);
            GUI.DrawTexture(r, spr.texture, ScaleMode.ScaleToFit, true);
            GUI.matrix = m;
        }

        // Shared splash glove. CoachGloveAt arcs over the top and ClearGloveOfCaption
        // keeps that path off the sentence. BodyTouch is the drawn quad's center,
        // so the fingertip meets the body and is not the LEVEL disc. A right-half
        // bird is met from the left.
        void AdoptPlaceGlove(float dt, float s)
        {
            if (!AdoptGloveAim(s, out var aim))
            {
                _gloveVis = false;
                return;
            }
            float lift = GloveRise(s);
            float hang = GloveDh(s) * 0.48f;
            if (lift < hang) lift = hang;
            CoachGloveAt(aim, dt, s, lift, false, float.NaN, default, FnAdoptTap());
        }

        // Bird body or name plate. Same rects HitAdoptBirds claims, without using the event.
        bool AdoptGloveTap(Vector2 screen)
        {
            if (!_adoptLive || _avatarPose != AvatarPose.GlovePointing) return false;
            if (!AdoptLookRects(HomeAvatarIcon(Mathf.Max(Screen.height / 720f, 1f)), out var bird, out var plate))
                return false;
            var gui = new Vector2(screen.x, Screen.height - screen.y);
            if (bird.Contains(gui)) return true;
            return plate.width > 2f && plate.Contains(gui);
        }

        bool AdoptGloveAim(float s, out Vector2 aim)
        {
            aim = default;
            if (_avatarRename) return false;
            if (_avatarPose != AvatarPose.GlovePointing) return false;
            Rect bird = _adoptBirdR;
            if (bird.width < 2f)
            {
                float icon = HomeAvatarIcon(s);
                var c = AvatarHomePoint();
                bird = new Rect(c.x - icon * 0.5f, c.y - icon * 0.5f, icon, icon);
            }
            if (bird.width < 2f) return false;
            aim = BodyTouch(bird);
            return true;
        }

        // Rest pose only. The shared arc still lands the fingertip on BodyTouch.
        // The palm on the wind-up perch must stay off the body.
        bool GloveHitsAdoptBird(Vector2 pivot, float ang, float s, bool mirror)
        {
            if (!_adoptLive || _avatarPose != AvatarPose.GlovePointing) return false;
            float icon = HomeAvatarIcon(s);
            if (icon < 8f) return false;
            var c = AvatarHomePoint();
            float cap = icon * 0.20f;
            var body = new Rect(
                c.x - icon * 0.46f,
                c.y - icon * 0.50f + cap,
                icon * 0.92f,
                icon * 0.96f - cap);
            float dh = GloveDh(s);
            var rect = GloveRect(pivot, dh, mirror);
            if (!RotAabb(rect.x, rect.y, rect.width, rect.height, pivot, ang, 2f * s, out var box))
                return false;
            return box.Overlaps(body);
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

        bool AvatarPoseOnWood() =>
            _avatarPose == AvatarPose.Perched || _avatarPose == AvatarPose.GlovePointing;

        // Lesson yield only. The greet wait uses HomeWanderPoint inside HomeWanderBounds.
        // Circle just above the given point. Velocity faces the way the bird moves.
        static Vector2 AvatarCircle(Vector2 perch, float icon, float clock, out bool faceLeft)
        {
            float ang = clock * 1.35f;
            float rx = icon > 8f ? icon * 0.90f : 28f;
            float ry = icon > 8f ? icon * 0.36f : 12f;
            float sn = Mathf.Sin(ang);
            var p = new Vector2(perch.x + Mathf.Cos(ang) * rx, perch.y - icon * 0.62f + sn * ry);
            faceLeft = sn > 0f;
            return p;
        }

        // Legal centers while the home bird waits for a tap. Reuses AvatarGap (title,
        // rails, and the LEVEL flower's tap square), then the inner middle. The flap
        // quad stays on screen and off that tap square. The greet has no name plate.
        Rect HomeWanderBounds(float s, float icon)
        {
            AvatarGap(s, icon, out float top, out float bot, out float left, out float right);
            float half = icon * 0.5f;
            float reach = half * HomeFlapFit;
            float extra = reach - half;
            if (extra < 0f) extra = 0f;
            if (_adoptLive) bot += AvatarPlateDrop(s);
            left += extra;
            right -= extra;
            if (_adoptLive)
            {
                float under = AdoptCaptionRect(s).yMax + half;
                if (under > top) top = under;
            }
            float minX = reach;
            float maxX = Screen.width - reach;
            float minY = reach;
            float maxY = Screen.height - reach;
            if (maxX < minX)
            {
                float mid = Screen.width * 0.5f;
                minX = mid;
                maxX = mid;
            }
            if (maxY < minY)
            {
                float mid = Screen.height * 0.5f;
                minY = mid;
                maxY = mid;
            }
            if (left < minX) left = minX;
            if (right > maxX) right = maxX;
            if (top < minY) top = minY;
            if (bot > maxY) bot = maxY;
            float flowerY = FlowerPlayRect().yMin - reach;
            if (bot > flowerY) bot = flowerY;
            if (right < left)
            {
                float mid = (minX + maxX) * 0.5f;
                left = mid;
                right = mid;
            }
            if (bot < top)
            {
                float mid = flowerY;
                if (mid < minY) mid = minY;
                if (mid > maxY) mid = maxY;
                top = mid;
                bot = mid;
            }
            float cx = (left + right) * 0.5f;
            float cy = (top + bot) * 0.5f;
            float spanX = right - left;
            float spanY = bot - top;
            if (spanX < 0f) spanX = 0f;
            if (spanY < 0f) spanY = 0f;
            float capX = Screen.width * 0.58f;
            if (spanX > capX) spanX = capX;
            spanX *= 0.84f;
            spanY *= 0.84f;
            return new Rect(cx - spanX * 0.5f, cy - spanY * 0.5f, spanX, spanY);
        }

        // Slow drift inside HomeWanderBounds. Faces the way it is moving.
        static Vector2 HomeWanderPoint(Rect box, float clock, out bool faceLeft)
        {
            float ax = clock * 0.45f;
            float ay = clock * 0.29f;
            float x = box.center.x + Mathf.Cos(ax) * (box.width * 0.5f);
            float y = box.center.y + Mathf.Sin(ay) * (box.height * 0.5f);
            float x0 = box.xMin;
            float x1 = box.xMax;
            float y0 = box.yMin;
            float y1 = box.yMax;
            if (x < x0) x = x0;
            if (x > x1) x = x1;
            if (y < y0) y = y0;
            if (y > y1) y = y1;
            faceLeft = Mathf.Sin(ax) > 0f;
            return new Vector2(x, y);
        }

        void BeginAvatarLand(Vector2 from, Vector2 perch, bool thenGlove)
        {
            _avatarPose = AvatarPose.Landing;
            _avatarLandFrom = from;
            _avatarLandTo = perch;
            _avatarLandT = 0f;
            _avatarPerchHold = 0f;
            _avatarLandGlove = thenGlove;
            _avatarGliding = false;
            _avatarCrossing = false;
        }

        void StepAvatarLand(float dt, float s)
        {
            _avatarLandT += dt;
            const float dur = 0.58f;
            float u = Mathf.Clamp01(_avatarLandT / dur);
            float e = Mathf.SmoothStep(0f, 1f, u);
            var p = Vector2.Lerp(_avatarLandFrom, _avatarLandTo, e);
            p.y -= Mathf.Sin(u * Mathf.PI) * 22f * s;
            _avatarPos = p;
            if (Mathf.Abs(_avatarLandTo.x - _avatarLandFrom.x) > 2f)
                _avatarFaceLeft = _avatarLandTo.x < _avatarLandFrom.x;
            if (u < 1f) return;
            _avatarPos = _avatarLandTo;
            _avatarPerchIx = HomeBranchAnchor();
            _awayOn = false;
            _avatarGliding = false;
            _avatarPose = AvatarPose.Perched;
            _avatarFaceLeft = _avatarPos.x >= Screen.width * 0.5f;
            if (!_avatarLandGlove) return;
            _avatarPerchHold = 0.18f;
        }

        // FromRight garden limb at half size. AvatarBranchMul is the only scale:
        // growing branchPx so the middle pad cleared the rail is what made the
        // twig full size and left it floating under the caption. PlaceRight seats
        // the visible wood, then the root bleeds off the right edge so the twig
        // grows out of the scenery. The bird is on HomeBranchAnchor, the tip.
        void PlaceHomeBranch(float s)
        {
            WoodWorld(out float worldW, out float worldH);
            float h = Screen.height > 2f ? Screen.height : s * 720f;
            float ppu = h / (WorldBuilder.CamOrtho * 2f);
            if (ppu < 1f) ppu = 1f;

            float branchPx = ppu * AvatarBranchMul;
            float woodW = branchPx * worldW;
            float woodH = branchPx * worldH;
            HomeTwigRect(Vector2.zero, woodW, woodH, out var probe);
            float visH = probe.height > 8f ? probe.height : 8f;
            float visW = probe.width > 8f ? probe.width : 8f;

            float drop = AvatarNeedsName() ? 0f : AvatarPlateDrop(s);
            float levelTop = FlowerLevelTop();
            float y = levelTop - visH - drop - 8f * s;
            var disc = FlowerDisc(FlowerPlayRect(), 0f);
            if (disc.width > 2f && y + visH > disc.yMin - 4f * s)
                y = disc.yMin - visH - 8f * s;
            if (_adoptLive && _adoptLineR.height > 2f && y < _adoptLineR.yMax && y + visH > _adoptLineR.yMin)
                y = _adoptLineR.yMax + 8f * s;
            float cap = CaptionFloorY(s);
            if (y < cap) y = cap;

            var mounted = PlaceRight(s, visW, visH, y);
            float inset = Mathf.Max(8f * s, Screen.width - Screen.safeArea.xMax + 6f);
            mounted.x += inset + 14f * s;

            _limbCenter = new Vector2(mounted.x - probe.x, mounted.y - probe.y);
            _limbW = woodW;
            _limbH = woodH;
            _limbPx = branchPx;
            _limbOn = true;
            float icon = HomeAvatarIcon(s);
            _avatarFitIcon = icon;
            float world = AvatarBirdWorld();
            float birdPx = world > 0.001f ? icon / world : 0.01f;
            var seated = LimbBirdSeated(_limbCenter, branchPx, birdPx, true, HomeBranchAnchor());
            for (int i = 0; i < _avatarPerches.Length; i++)
                _avatarPerches[i] = seated;
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
            int i = HomeBranchAnchor();
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

        // Toes settle this fraction of the bird body into the bark, so a resting bird
        // reads as planted and never as hovering over the branch tip.
        const float PerchToeSink = 0.02f;

        // The ONE resting-bird center. seat is the branch-tip point LimbBirdSeated
        // returned. A bird at rest does not bob: it sits at the seat, sunk by
        // PerchToeSink. Only a happy hop (hop01 counts 1 to 0, hopPx is its height)
        // lifts it. Off the perch (flying, landing) the caller's position is kept.
        // Home bird, adopt bird, and their name tags/hit rects all read this.
        static Vector2 PerchAnchor(Vector2 seat, float icon, bool onPerch, float hop01, float hopPx)
        {
            float lift = hop01 > 0f ? Mathf.Sin((1f - hop01) * Mathf.PI) * hopPx : 0f;
            float sink = onPerch ? icon * PerchToeSink : 0f;
            return new Vector2(seat.x, seat.y + sink - lift);
        }

        // Adopt scene, or the saved bird once it has a perch. Hidden under the streak sign.
        bool HomeLimbVisible()
        {
            if (_adoptLive || _adoptStep == AdoptStep.Settle) return true;
            if (!AvatarHomeOn()) return false;
            if (_avatarHold || !_avatarPlaced) return false;
            if (_streakSlide >= 0f && !_avatarTutor) return false;
            return true;
        }

        // edgeRoot: the length that runs behind the right rail and off the screen.
        // The perch side is drawn later, on top of the flower and under the bird.
        void DrawHomeLimbSide(bool edgeRoot)
        {
            if (!_limbOn) return;
            float s = Mathf.Max(Screen.height / 720f, 1f);
            var rail = SplashRailColumn(true, 3, s);
            float cut = rail.width > 2f ? rail.xMin : Screen.width;
            if (edgeRoot)
                DrawHomeLimbs(cut, Screen.width + 24f);
            else
                DrawHomeLimbs(0f, cut);
        }

        void DrawHomeLimbs(float x0, float x1)
        {
            if (!_limbOn || Screen.height < 2f || x1 < x0 + 2f) return;
            var clip = new Rect(x0, 0f, x1 - x0, Screen.height);
            GUI.BeginGroup(clip);
            DrawHomeTwig(new Vector2(_limbCenter.x - x0, _limbCenter.y), _limbW, _limbH);
            GUI.EndGroup();
            // Perch half only (group starts at x = 0). Leaves rotate via GUI.matrix.
            // RotateAroundPivot writes identity first; inside BeginGroup that inverts
            // the clip, and the bird drawn next — a new color texture — paints under
            // the foliage. World birds use FlockSort.Apply for the same "stay in
            // front of what should be behind you" rule. This bird is IMGUI.
            if (x0 < 1f)
            {
                HomeTwigRect(_limbCenter, _limbW, _limbH, out var vis);
                DrawHomeTwigLeaves(_limbCenter, vis.height, _limbPx);
            }
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

        // In-place flip. HomeTwigX0 + HomeTwigX1 must stay equal to HomeTwigTexW,
        // so this is the sheet-center FromRight mirror LimbBirdSeated reads.
        // A matrix flip inside BeginGroup inverts the clip and hides the limb.
        static Rect HomeTwigUv()
        {
            float u0 = HomeTwigX0 / HomeTwigTexW;
            float u1 = HomeTwigX1 / HomeTwigTexW;
            float v = (HomeTwigTexH - HomeTwigBot) / HomeTwigTexH;
            float vh = (HomeTwigBot - HomeTwigTop) / HomeTwigTexH;
            return new Rect(u1, v, u0 - u1, vh);
        }

        void DrawHomeTwig(Vector2 center, float w, float h)
        {
            if (w < 4f || h < 4f) return;
            var wood = SpriteCatalog.Branch;
            if (wood == null || wood.texture == null) return;
            HomeTwigRect(center, w, h, out var r);
            if (r.width < 2f || r.height < 2f) return;
            var prev = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTextureWithTexCoords(r, wood.texture, HomeTwigUv());
            GUI.color = prev;
        }

        void DrawHomeTwigLeaves(Vector2 center, float visH, float px)
        {
            var leaf = SpriteCatalog.Leaf;
            if (leaf == null || leaf.texture == null || px < 0.5f) return;
            float lh = Mathf.Clamp(px * 0.42f, visH * 0.16f, visH * 0.38f);
            float t = Time.unscaledTime;
            DrawOneLeaf(leaf, center, px, true, HomeBranchAnchor(), -0.20f, lh * 0.90f, -18f, t, 0);
            DrawOneLeaf(leaf, center, px, true, HomeBranchAnchor(), 0.12f, lh * 0.70f, 14f, t, 1);
            DrawOneLeaf(leaf, center, px, true, HomeBranchAnchor(), 0.30f, lh * 0.82f, 22f, t, 2);
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
            // FromRight mirrors the art and the lean, same as LeafDraw.Pose.
            float tilt = fromRight ? -(deg + sway) : deg + sway;
            var uv = fromRight ? new Rect(1f, 0f, -1f, 1f) : new Rect(0f, 0f, 1f, 1f);
            var m = GUI.matrix;
            var prev = GUI.color;
            float g = 0.82f + 0.06f * (i & 1);
            GUI.color = new Color(g, 1f, g * 0.9f, 1f);
            if (Mathf.Abs(tilt) > 0.4f)
                GUIUtility.RotateAroundPivot(tilt, new Vector2(r.center.x, r.yMax));
            GUI.DrawTextureWithTexCoords(r, leaf.texture, uv, true);
            GUI.matrix = m;
            GUI.color = prev;
        }
    }
}
