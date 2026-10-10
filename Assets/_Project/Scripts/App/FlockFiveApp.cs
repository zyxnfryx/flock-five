using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlockFive
{
    public sealed partial class FlockFiveApp : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBoot()
        {
            ApplyFramePace();
            if (FindAnyObjectByType<FlockFiveApp>() != null) return;
            var go = new GameObject("FlockFiveApp");
            DontDestroyOnLoad(go);
            go.AddComponent<FlockFiveApp>();
        }

        Board _board;
        WorldBuilder.Garden _garden;
        int _sel = -1;
        bool _busy;
        bool _restarting;
        static int _motionGen;
        static int _perchWave;
        bool _won;
        Board _seed;
        bool _keepStreak;
        enum RestartAsk { None, Sure, Keep }
        RestartAsk _restartAsk;
        int _restartSavedMul;
        bool _restartAd;
        float _gardenStampAt = -1f;
        Rect _gardenStampRect;
        float _stampPulse = -1f;
        static float _waxBorn = -1f;
        int _collectDepth;
        bool _gardenScoring;
        static float _claimClunk = -1f;
        struct CoinFly
        {
            public Vector2 A, B;
            public float T, Delay, Spin;
            public int Worth;
        }
        readonly System.Collections.Generic.List<CoinFly> _flies = new System.Collections.Generic.List<CoinFly>();
        // Purse coins already saved. Hidden until that sprite lands, then the balance counts them.
        int _coinInFlight;
        // Displayed balance while a reward pay is ticking. -1 until the first draw.
        int _coinTick = -1;
        int _coinTickFrame = -1;
        // Reveal roll (RevealHeldCoins): eased count from _coinRollFrom up to the purse total.
        int _coinRollFrom;
        float _coinRollT;
        float _coinRollDur;
        bool _rewardPayLive;
        float _rewardHoldLeft;
        int _rewardHoldFrame = -1;
        // Garden overlay. The streak sign must not run under this, or its pay beat never fires.
        bool _clearReward;
        // Clear is saved and unspent. The homepage sign still owes the coin flight.
        bool _streakHomeDue;
        // RewardGap after that board tucks. Lessons wait. The sign is already gone.
        bool _rewardGap;
        int _rewardGapGen;
        // Piggy coin: hold with random wait, then a slow Y-flip.
        float _coinSpinT = -1f;
        float _coinHoldLeft;
        float _coinHoldAge;
        bool _coinArmed;
        // Splash streak sign: <0 idle. While it runs, 0..1 is the shared reward clock.
        float _streakSlide = -1f;
        static bool _rewardStampHit;
        static GUIStyle _rewardStampStyle;
        static GUIContent _rewardContent;
        static Texture2D _rewardDisc;
        static int _stampPx;
        static int _stampKey = int.MinValue;
        static GUIStyle _coinClusterStyle;
        static GUIContent _coinClusterContent;
        static int _coinPx;
        static int _coinKey = int.MinValue;
        int _streakAnnounced = -1;
        bool _streakChirped;
        bool _streakWinChimed;
        int _streakRollShown;
        int _streakTickBucket = -1;
        bool _streakFlyArmed;
        float _pigBurst;
        float _pigJiggle;
        // Splash rails. Presence eases 0..1; a hidden button takes no slot.
        const int RailPig = 0;
        const int RailHive = 1;
        const int RailPoker = 2;
        const int RailVip = 3;
        const int RailDaily = 4;
        const int RailCount = 5;
        const float SplashRailSlide = 0.42f;
        readonly float[] _railK = new float[RailCount];
        readonly float[] _railFrom = new float[RailCount];
        readonly float[] _railGoal = new float[RailCount];
        readonly float[] _railT0 = new float[RailCount];
        readonly Rect[] _railRect = new Rect[RailCount];
        readonly Rect[] _railSeat = new Rect[RailCount];
        bool _railInit;
        readonly HashSet<int> _locked = new HashSet<int>();
        int _combo;
        float _comboUntil = -99f;
        // Minimum seconds between COMBO wordmarks. NextCombo still climbs on
        // every collect. Steps that land inside the window stay queued, so a
        // fast chain still shows x2, then x3, then x4 instead of jumping ahead.
        const float ComboPopGap = 2f;
        float _comboPopAt = -99f;
        struct ComboCue
        {
            public int Combo;
            public bool Celebrate;
        }

        readonly Queue<ComboCue> _comboShows = new Queue<ComboCue>(4);
        int _comboPopGen;
        bool _comboPopPump;
        int _comboPopLive;
        bool _finaleHold;
        bool _collecting;
        // Set for the whole sparrow/hawk scrap, from the tap that starts it
        // through perch. Blocks a second move, scatter, or stage reload.
        bool _pestResolving;
        bool _pestFinishing;
        bool _pestClearScored;
        bool _scoreImmediate;
        bool _scatterBusy;
        bool _pestGuardNoted;
        bool _pestsArmed;
        float _nextTap;
#if UNITY_EDITOR
        bool _finalePreview;
#endif
        bool _splash = true;
        bool _levelHive;
        enum HomeFace { Splash, Hive, Poker, Badger }
        HomeFace _home;
        enum PokerMotion { None, Deal, Draw, Shuffle }
        PokerMotion _pokerMotion;
        float _pokerMotionT = 99f;
        readonly bool[] _pokerRedraw = new bool[BirdPoker.HandSize];
        readonly BirdPoker.Card[] _pokerPrev = new BirdPoker.Card[BirdPoker.HandSize];
        bool _pokerFan;
        float _pokerKickT;
        float _pokerKickDur = 0.12f;
        float _pokerKickAmp;
        float _pokerKickTwist;
        Vector2 _pokerKick;
        int _pokerPluckN;
        readonly int[] _pokerPluckIx = new int[BirdPoker.HandSize];
        readonly int[] _pokerDrawOrder = new int[BirdPoker.HandSize];
        float _pokerPluckEnd;
        float _pokerReplaceEnd;
        float _pokerRowEnd;
        float _pokerDash;
        bool _pokerChained;
        float _pokerChainBreak = -1f;
        readonly bool[] _pokerKept = new bool[BirdPoker.HandSize];
        readonly float[] _pokerHoldSlide = new float[BirdPoker.HandSize];
        readonly Rect[] _pokerPoseR = new Rect[BirdPoker.HandSize];
        readonly float[] _pokerPoseRoll = new float[BirdPoker.HandSize];
        readonly Rect[] _pokerHoldFrom = new Rect[BirdPoker.HandSize];
        readonly Rect[] _pokerDrawFrom = new Rect[BirdPoker.HandSize];
        // Rect each card was actually painted at (no hit-slop, no kick) — the draw flight starts here.
        readonly Rect[] _pokerDrawnR = new Rect[BirdPoker.HandSize];
        int _pokerHover = -1;
        bool _pokerKeepHint;
        // First visit to the table. 0 waiting, 1 bet, 2 deal, 3 hold, 4 draw, -1 done.
        const string CoachPokerPageKey = "flockfive.coach.pokerpage";
        // Set on the first Deal. The bet sentence never returns after that.
        const string CoachPokerDealtKey = "flockfive.coach.pokerdealt";
        const string CoachPokerBackKey = "flockfive.coach.pokerback";
        const string PokerBetLine = "Pick your bet, then tap Deal.";
        const string PokerHoldLine = "Tap a card to hold it, then tap Draw.";
        const string PokerBackLine = "Tap the back button to return home.";
        int _pokerPageStep;
        bool _pokerPageOn;
        float _pokerPageAge;
        bool _pokerTutorAimOk;
        Vector2 _pokerTutorAim;
        Rect _pokerTutorTap;
        // Repeat visits, after flockfive.coach.pokerpage. Hidden once Deal is tapped,
        // and kept hidden after that (flockfive.coach.pokerdealt).
        bool _pokerDealHint;
        bool _pokerDealAimOk;
        Vector2 _pokerDealAim;
        Rect _pokerDealTap;
        // One-time glove on the back medal after a hand pays. flockfive.coach.pokerback.
        bool _pokerBackKnown;
        bool _pokerBackDone;
        bool _pokerBackAimOk;
        Vector2 _pokerBackAim;
        Rect _pokerBackTap;
        Rect _pokerMinusR;
        Rect _pokerPlusR;
        bool _pokerShowPay;
        float _pokerSwayT;
        float _pokerWinT = -1f;
        // This hand's payouts still on Purse's display hold (armed by RunPokerDraw).
        int _pokerHeldWin;
        int _pokerHeldFull;
        string _pokerWinFitText;
        int _pokerWinFitW, _pokerWinFitH, _pokerWinFitRest, _pokerWinFitHero;
        // Bottom of the card row this frame; win speed lines never cross above it.
        float _pokerRowBottom;
        int _pokerWinHeroFont;
        int _pokerWinRestFont;
        bool _pokerPendingStamp;
        int _pokerResultCue;
        bool _pokerStamp;
        float _pokerStampT;
        int _pokerStampKind = -1;
        bool _pokerPayOpen;
        float _pokerPayAnim;
        bool _pokerPayJewelHeld;
        bool _pokerAdRunning;
        bool _pokerBingo;
        enum GiftFace { None, Card, Movie, Thanks }
        GiftFace _gift;
        float _giftThanksAt;
        bool _frozen;
        bool _freezeOffer;
        float _freezeShownAt = -1f;
        bool _iceCoating;
        // One flag per bottom gift. Stays set for the stage, including Restart.
        // Cleared only when a stage loads. Index is the gift's ordinal, not its branch.
        readonly bool[] _bonusOn = new bool[WorldBuilder.GiftCount];
        int _giftBranch = -1;
        float _suppressGiftUntil;
        float _swallowTapsUntil;
        int[] _census;
        readonly List<BeeVisit> _levelBees = new List<BeeVisit>();
        int _hiveFlip = -1;
        float _hiveFlipT;
        int _hivePage;
        int _hivePageFrom;
        float _hivePageTurn = 99f;
        readonly bool[] _hiveFaceBack = new bool[Hive.AlbumSlots];
        const int HivePageSize = 9;
        int _hiveInspect = -1; // album slot (kind×finish), -1 = closed
        float _hiveInspectT; // 0..1 pull animation (open), also used for close
        bool _hiveInspectClosing;
        Rect _hiveInspectFrom; // sleeve rect when opened (for lerp)
        // Fresh slots still waiting for a tap. Survives the splash, where the garden halo is cleared.
        readonly List<int> _hiveFreshSlots = new List<int>();
        int _hiveJumpSlot = -1;
        bool _hiveHowToNudge;
        int _levelHivePick;
        float _levelHiveShown;
        float _levelHiveAuto;
        int _levelHiveFrame = -1;
        int _levelHiveCountSeen = -1;
        Vector2 _levelHiveDrag0;
        float _levelHiveDragX;
        int _levelHiveDragId;
        bool _levelHiveDrag;
        bool _levelHiveSwallow;
        Rect _levelHiveRect;
        const float HiveCarouselStep = 1.5f;
        const string HiveTitle = "Collection";
        const string HiveSubtitle = "Find bees to earn cards.";
        const string HiveHowTo = "Tap a card to take a look, tap again to flip.";
        const string HiveFlipSeenKey = "flockfive.hive.flip.seen";
        // First visit to the album. Same done-flag pattern as the other coach lessons.
        const string CoachHiveAlbumKey = "flockfive.coach.hivealbum";
        const string AlbumTapLine = "Tap a card you have\nto enlarge it.";
        const string AlbumFlipLine = "Tap again to flip.";
        // After flip: honeycombs by rarity (shared CardText / HoneyOfFinish floor).
        const string AlbumHoneyLine = "Honeycombs are honey.\n2 normal · 3 foil · 5 inverse.";
        // Separate post-badger step (CoachHiveUpgradeKey).
        const string AlbumUpgradeLine = "Upgrade adds honey for 1000 coins.\nMax 1 · 2 · 3 by rarity.";
        const string AlbumPageLine = "Swipe to turn the page!\nOr tap a page number.";
        // Empty book: nothing to enlarge yet, so the glove points at the grid itself.
        const string AlbumEmptyLine = "Tap a card to take\na closer look!";
        const string CoachHiveUpgradeKey = "flockfive.coach.hiveupgrade";
        static readonly Color AlbumInk = new Color(1f, 0.98f, 0.90f, 1f);
        bool _hiveTutorOn;
        float _hiveTutorT;
        int _hiveTutorStep;
        int _hiveGuiFrame = -1;
        bool _albumTutorOn;
        bool _albumTutorKnown;
        bool _albumTutorDone;
        bool _albumTutorFlipped;
        int _albumTutorStep;
        int _albumTutorSlot = -1;
        bool _albumTutorAimOk;
        Vector2 _albumTutorAim;
        // Separate Collection step after first honey-badger encounter.
        bool _upgradeTutorOn;
        bool _upgradeTutorKnown;
        bool _upgradeTutorDone;
        bool _upgradeTutorAimOk;
        Vector2 _upgradeTutorAim;
        Rect _upgradeBtnR;
        float _albumOpenAt = -1f;
        int _albumOpenFrames;
        int _albumAgeFrame = -1;
        bool _albumCardOk;
        Rect _albumCardR;
        bool _albumPagerOk;
        Rect _albumPagerR;
        bool _albumInspectOk;
        Rect _albumInspectR;
        PageSwipe _pageSwipe;
        // Album sheet strip a page swipe starts in, GUI space. Set each album frame.
        Rect _albumSwipeR;
        static float AlbumSwipeMin(float s) => Mathf.Max(36f, 40f * s);
        int _wakeBranch = -1;
#if UNITY_EDITOR
        bool _pokerPlayrun;
#endif
        Coroutine _sparrowRun;
        Coroutine _hawkRun;
#if UNITY_EDITOR
        int _shotLevelNumber;
        string _shotEase;
        bool _recordSmash;
        int _recordFrameCount;
#endif
        // Home splash logo-halo orbits (five birds; draw-only, no hit targets).
        struct SplashFlutter
        {
            public float Angle;     // radians around title center
            public float Speed;     // rad/sec (sign = direction)
            public float RadiusX;   // ellipse radii (outside glyphs)
            public float RadiusY;
            public float BobPhase;
            public float Slot;      // eased angular target (radians)
            public BirdColor Col;
        }
        SplashFlutter[] _splashFlutters;
        SplashFlutter[] _pokerFlutters;
        int _splashHaloTick = -1;
        int _pokerHaloTick = -1;
        static float _titleWing = -1f;
        static readonly int[] _titleOrd = new int[8];
        static readonly float[] _titleAng = new float[8];
        struct HiveHaloBee
        {
            public float SlotFrom, SlotTo, SlotStart;
            public float RadiusPhase, RadiusOmega;
            public float BobPhase, BobOmega, BobGain;
            public float Loop;     // 0 ellipse, 1 figure-eight, 2 dart
            public float LoopAmp;  // per-bee radius, kept under 1
            public float Dart;
            public Color Tint;
            public float Appear, Expire;
        }
        readonly List<HiveHaloBee> _incomingHalo = new List<HiveHaloBee>();
        // Bird halo orbits at this ±0.06 rad/s (EnsureHaloFlutters). Bee halo base matches it.
        const float HaloOrbitSpeed = 0.46f;
        const float HaloEbbAmp = 0.25f;
        const float HaloEbbOmega = 0.65f;
        const float HaloRespaceSec = 0.26f;
        const float HaloRadiusWobble = 0.03f;
        const int HaloCap = 12;
        float _haloSpin;
        float _haloEbbPhase;
        bool _haloEbbReady;
        readonly float[] _haloGap = new float[HaloCap];
        readonly int[] _haloOrder = new int[HaloCap];
        readonly float[] _haloUnwrapped = new float[HaloCap];
        static Sprite _splashPointedV;

        // Home-splash favorite. flockfive.avatar is 0-4 (ruby, gold, teal, violet, peach).
        // Missing or out of range reads as gold. The bird stays hidden until level 1 is
        // cleared (flockfive.next >= 1) and flockfive.avatar.adopted is set. The name
        // lives in flockfive.avatar.name. Kit is flockfive.avatar.kit (0 none, 1 bow,
        // 2 crown) and does not change garden SexOf. The five halo birds stay on the logo.
        const string AvatarPref = "flockfive.avatar";
        const float AvatarFlapRate = 1.25f;
        BirdColor _avatarCol = BirdColor.Gold;
        bool _avatarPlaced;
        bool _avatarDrew;
        bool _avatarCross;
        bool _avatarCrossing;
        bool _avatarGliding;
        bool _avatarHold;
        bool _avatarTutor;
        bool _avatarFaceLeft;
        int _avatarTick = -1;
        int _avatarPerchIx;
        float _avatarClock;
        float _avatarBobPhase;
        float _avatarCrossT;
        float _avatarCrossDur = 1.15f;
        float _avatarGlideT;
        float _avatarGlideDur = 0.85f;
        float _avatarNextGlide;
        float _avatarNextFlap;
        float _avatarFlapT;
        float _avatarPreenT;
        float _avatarHappy;
        float _avatarFitIcon;
        Vector2 _avatarPos;
        Vector2 _avatarCrossFrom;
        Vector2 _avatarCrossTo;
        Vector2 _avatarGlideFrom;
        Vector2 _avatarGlideTo;
        Rect _avatarDrawR;
        readonly Vector2[] _avatarPerches = new Vector2[5];
        readonly Rect[] _avatarBlock = new Rect[4];
        Vector2 _limbCenter;
        float _limbW;
        float _limbH;
        float _limbPx;
        bool _limbOn;
        bool _awayOn;
        Vector2 _awayCenter;
        Vector2 _awaySeat;
        float _awayW;
        float _awayH;
        float _awayPx;

        public void InviteShareDone(string ok)
        {
            if (ok != "1") return;
            int before = Purse.Coins;
            Invite.OnShared();
            int gained = Purse.Coins - before;
            if (gained > 0) BeginRewardPay(gained);
        }

        static FlockFiveApp _app;
        static int _resumeNoteFrame = -1;
        static float _resumeInputUntil = -1f;

        void OnEnable()
        {
            _app = this;
        }

        void OnDisable()
        {
            if (_app == this) _app = null;
        }

        // Pause and focus both bookend a suspend. One call per frame drops the
        // resume spike, keeps the live lesson, and lets a shown ad settle once.
        public static void NoteAppBackground()
        {
            // Leaving mid-lesson: settle anything already half-done before the suspend.
            if (_app != null) _app.HealInterruptedTutorials();
        }

        public static void NoteAppResume()
        {
            if (_resumeNoteFrame == Time.frameCount) return;
            _resumeNoteFrame = Time.frameCount;
            PlayClock.DropResumeFrame();
            Ads.ArmResumeSettle();
            ApplyFramePace();
            if (_app == null) return;
            _resumeInputUntil = PlayClock.Now + 0.35f;
            _app.FreshTutorGlove();
            // The suspend may have cut a step off. Settle it before the first tap lands.
            _app.HealInterruptedTutorials();
        }

        void Start()
        {
            AdLog.Context = () => _splash ? "splash screen"
                : _board == null ? "no garden"
                : "garden " + LevelData.DisplayNumber + (_board.Won ? ", cleared" : ", MID-STAGE");
            AdLog.Add("app started");
            // iOS defaults to 30 fps. Ask for an even rate the panel can hold: 60 on
            // 60/120 Hz screens, 90 on 90 Hz ones (60 judders there). See FramePace.
            ApplyFramePace();
            AdLog.Add("frame pace " + Application.targetFrameRate + " fps on "
                + Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value) + " Hz");
            Application.runInBackground = true;
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Sfx.Warm();
            Ads.Warm();
            NoAds.Warm();
            Invite.Warm();
            SpriteCatalog.DropPokerArt();
            try { ShowSplash(); HealInterruptedTutorials(); }
            catch (System.Exception e)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogException(e);
#endif
            }
#if UNITY_EDITOR
            if (System.IO.File.Exists("/tmp/flock-five-b79-stills"))
            {
                try { System.IO.File.Delete("/tmp/flock-five-b79-stills"); } catch { }
                StartCoroutine(ShotBuild79());
            }
            if (System.IO.File.Exists("/tmp/flock-five-shot"))
            {
                try { System.IO.File.Delete("/tmp/flock-five-shot"); } catch { }
                StartCoroutine(ShotHome());
            }
            if (System.IO.File.Exists("/tmp/flock-five-gift-shot"))
            {
                try { System.IO.File.Delete("/tmp/flock-five-gift-shot"); } catch { }
                StartCoroutine(ShotGift());
            }
            if (System.IO.File.Exists("/tmp/flock-five-ice-shot"))
            {
                try { System.IO.File.Delete("/tmp/flock-five-ice-shot"); } catch { }
                StartCoroutine(ShotIce());
            }
            if (System.IO.File.Exists("/tmp/flock-five-poker-run"))
            {
                try { System.IO.File.Delete("/tmp/flock-five-poker-run"); } catch { }
                _pokerPlayrun = true;
                StartCoroutine(PokerPlayrun());
            }
            if (System.IO.File.Exists("/tmp/flock-five-poker-punches"))
            {
                try { System.IO.File.Delete("/tmp/flock-five-poker-punches"); } catch { }
                StartCoroutine(ShotPokerPunches());
            }
            if (System.IO.File.Exists("/tmp/flock-five-poker-faces"))
            {
                try { System.IO.File.Delete("/tmp/flock-five-poker-faces"); } catch { }
                StartCoroutine(ShotPokerFaces());
            }
            if (System.IO.File.Exists("/tmp/flock-five-poker-stamp"))
            {
                try { System.IO.File.Delete("/tmp/flock-five-poker-stamp"); } catch { }
                StartCoroutine(ShotPokerStamp());
            }
            if (System.IO.File.Exists("/tmp/flock-five-storm-shot"))
            {
                try { System.IO.File.Delete("/tmp/flock-five-storm-shot"); } catch { }
                StartCoroutine(ShotStorm());
            }
            if (System.IO.File.Exists("/tmp/flock-five-hive-inspect"))
            {
                try { System.IO.File.Delete("/tmp/flock-five-hive-inspect"); } catch { }
                StartCoroutine(ShotHiveInspect());
            }
            if (System.IO.File.Exists("/tmp/flock-five-sparrow-shot"))
            {
                try { System.IO.File.Delete("/tmp/flock-five-sparrow-shot"); } catch { }
                StartCoroutine(ShotSparrow());
            }
            if (System.IO.File.Exists("/tmp/flock-five-level6"))
            {
                try { System.IO.File.Delete("/tmp/flock-five-level6"); } catch { }
                StartCoroutine(ShotSplashButtons());
            }
            if (System.IO.File.Exists("/tmp/flock-five-leaves-shot"))
            {
                try { System.IO.File.Delete("/tmp/flock-five-leaves-shot"); } catch { }
                StartCoroutine(ShotLeaves());
            }
            if (System.IO.File.Exists("/tmp/flock-five-birds-shot"))
            {
                try { System.IO.File.Delete("/tmp/flock-five-birds-shot"); } catch { }
                StartCoroutine(ShotBirds());
            }
            if (System.IO.File.Exists("/tmp/flock-five-streak-shot"))
            {
                try { System.IO.File.Delete("/tmp/flock-five-streak-shot"); } catch { }
                StartCoroutine(ShotStreak());
            }
            if (System.IO.File.Exists("/tmp/flock-five-consumer-tour"))
            {
                try { System.IO.File.Delete("/tmp/flock-five-consumer-tour"); } catch { }
                StartCoroutine(ShotConsumerTour());
            }
            if (System.IO.File.Exists("/tmp/flock-five-hand-qa"))
            {
                try { System.IO.File.Delete("/tmp/flock-five-hand-qa"); } catch { }
                StartCoroutine(ShotHandQa());
            }
            if (System.IO.File.Exists("/tmp/flock-five-poker-feel"))
            {
                try { System.IO.File.Delete("/tmp/flock-five-poker-feel"); } catch { }
                StartCoroutine(ShotPokerFeel());
            }
#endif
        }

        // Clears the inspect, the flip, and a stale album tutor together.
        void CloseHiveInspect()
        {
            HiveOverlay.Close(ref _hiveInspect, ref _hiveInspectClosing, ref _hiveInspectT,
                ref _hiveFlip, ref _hiveFlipT, ref _albumTutorOn);
        }

        void Restart()
        {
            if (_splash) return;
            if (_restartAsk != RestartAsk.None || _restartAd) return;
            if (_busy && !_frozen) return;
            if (StreakTier.AtStake(Purse.Streak))
            {
                if (!GatePopup(PopupKind.Streak, true)) return;
                _restartSavedMul = StreakTier.Normalize(Purse.Streak);
                _restartAsk = RestartAsk.Sure;
                _gift = GiftFace.None;
                _freezeOffer = false;
                Sfx.CardTap();
                return;
            }
            StartCoroutine(SnapRound());
        }

        void ShowSplash()
        {
            CloseHiveInspect();
            SeasonCrossfade.RetireLive();
            GardenLife.DetachHome();
            _splash = true;
            _clearReward = false;
            CancelRewardGap();
            _gardenScoring = false;
            _gardenStampAt = -1f;
            _collectDepth = 0;
            GardenFit.ClearBusy();
            _leafIntro = false;
            PestSchedule.SetTutorialPause(false);
            NoteHiveIntroLeft();
            NotePokerIntroLeft();
            NoteDailyIntroLeft();
            CoachRelease();
            _incomingHalo.Clear(); // hive bees circle only inside a garden
            _home = HomeFace.Splash;
            _busy = false;
            _won = false;
            _finaleHold = false;
            _levelHive = false;
            _gift = GiftFace.None;
            _frozen = false;
            _freezeOffer = false;
            _iceCoating = false;
            _sel = -1;
            StopPests();
            PestSchedule.Watch(null, null);
            _lessonGuardUntil = 0f;
            // Destroy lands at frame end. Hide now so the old painting cannot flash over home.
            if (_garden.Root != null)
            {
                _garden.Root.gameObject.SetActive(false);
                Destroy(_garden.Root.gameObject);
            }
            if (_garden.Cam != null) Destroy(_garden.Cam.gameObject);
            _garden = default;
            _board = null;
            var homeCam = WorldBuilder.MakeCamera(transform);
            GardenLife.AttachHome(transform, homeCam);
            if (MixDesk.Live != null) MixDesk.Live.SetSplash(true);
            Purse.Boot();
            // A clear that never reached the pay step. Pending is memory-only, so a
            // fresh process has nothing left to present. Shots must not eat that owed.
            if (Purse.Pending <= 0)
                SettleUnshownStreakPay();
            Invite.Warm();
            PigPoke.Boot();
            BirdPoker.Boot();
            ArmStreakSlide();
            CloseAvatarRename();
            ArmAdopt();
            ArmDailyBonus();
            ArmDailyIntro();
            ArmHiveIntro();
            ArmPokerIntro();
        }

        void Load(int index)
        {
            // Before the garden (and its season banner) is built. A solved board
            // that is still browsing the inspect does not come through here.
            CloseHiveInspect();
            SeasonCrossfade.RetireLive();
            GardenLife.DetachHome();
            GiftWant.StageClear = false; // the new stage's signs start lit
            _autoResolveDone = false;
            _solvedMerging = false;
            _lessonGuardUntil = 0f;
            SetAutoResolveSpeed(1f);
            GardenFit.ClearBusy();
            DismissStreakSign();
            // Board never opened: still one credit. A pay beat that already ran is a no-op.
            SettleUnshownStreakPay();
            _streakHomeDue = false;
            _popupDefer &= ~(int)PopupKind.Streak;
            CancelRewardGap();
            _busy = false;
            _won = false;
            _finaleHold = false;
            _sel = -1;
            _pestsArmed = false;
            _combo = 0;
            _comboUntil = -99f;
            SfxLibrary.CloseCombo();
            ClearComboShows();
            _collecting = false;
            _collectDepth = 0;
            _gardenScoring = false;
            EndPest();
            _locked.Clear();
            _levelBees.Clear();
            _incomingHalo.Clear();
            _levelHive = false;
            _gift = GiftFace.None;
            _frozen = false;
            _freezeOffer = false;
            _iceCoating = false;
            _splash = false;
            CloseAvatarRename();
            NoteHiveIntroLeft();
            NotePokerIntroLeft();
            NoteDailyIntroLeft();
            if (MixDesk.Live != null)
            {
                MixDesk.Live.SetBadger(false);
                MixDesk.Live.SetSplash(false);
            }
            _board = LevelData.Open(index);
            _seed = _board.Clone();
            for (int i = 0; i < _bonusOn.Length; i++) _bonusOn[i] = false;
            _giftBranch = -1;
            _census = BoardValidator.Counts(_board);
            CoachBegin(index);
            ArmLeafIntro();
            ArmLevelBeeIntro();
            Purse.BeginStage();
            if (_garden.Root != null)
            {
                _garden.Root.gameObject.SetActive(false);
                Destroy(_garden.Root.gameObject);
            }
            if (_garden.Cam != null) Destroy(_garden.Cam.gameObject);
            SpriteCatalog.ForgetBirds();
            _garden = WorldBuilder.Build(transform);
            AlignBranchViews();
            SyncAll();
            StartCoroutine(GardenFit.Tween(_garden, _board, true));
            Sfx.GardenWake();
            StopPests();
            PestSchedule.BeginStage(LevelData.DisplayNumber);
            PestSchedule.SetTutorialPause(_leafIntro);
            PestSchedule.Watch(_board, () => _solvedMerging || _gardenScoring || _won);
            SyncOrbitHive();
            _sparrowRun = StartCoroutine(SparrowView.Patrol(
                CanSparrowVisit, PestsArmed, LevelData.SparrowVisits, _garden.Feeders, _garden.Root));
            _hawkRun = StartCoroutine(HawkView.Patrol(
                CanHawkVisit, PestsArmed, LevelData.HawkVisits, _garden.Feeders, _garden.Root));
            ArmGardenStamp();
            // Board is open and the garden is built. The smoke test waits for this line.
            if (_board != null && !_splash)
                Debug.Log("FF_LEVEL_STARTED");
#if UNITY_EDITOR
            if (WantFinalePreview())
                StartCoroutine(PreviewFinale());
#endif
        }

        void StopSparrow()
        {
            if (_sparrowRun != null)
            {
                StopCoroutine(_sparrowRun);
                _sparrowRun = null;
            }
            if (SparrowView.Live != null)
                Destroy(SparrowView.Live.gameObject);
        }

        void StopHawk()
        {
            if (_hawkRun != null)
            {
                StopCoroutine(_hawkRun);
                _hawkRun = null;
            }
            HawkView.ClearShow();
            if (HawkView.Live != null)
                Destroy(HawkView.Live.gameObject);
        }

        void StopPests()
        {
            StopSparrow();
            StopHawk();
            SparrowView.ClearFx();
        }

        bool CanSparrowVisit() =>
            !PestSchedule.SolvedNow
            && !_splash && !_busy && !_won && !_frozen && !_levelHive
            && _gift == GiftFace.None && _board != null && !SolvedRule.FinaleDue(_board)
            && (HawkView.Live == null || !HawkView.Live.IsBlocking);

        bool CanHawkVisit() =>
            !PestSchedule.SolvedNow
            && !_splash && !_busy && !_won && !_frozen && !_levelHive
            && _gift == GiftFace.None && _board != null && !SolvedRule.FinaleDue(_board);

        bool PestsArmed() => _pestsArmed;

        void ArmPests()
        {
            _pestsArmed = true;
        }

#if UNITY_EDITOR
        bool WantFinalePreview() =>
            !_finalePreview && System.IO.File.Exists("/tmp/flock-five-finale");

        IEnumerator PreviewFinale()
        {
            _finalePreview = true;
            yield return new WaitForSeconds(0.4f);
            _busy = true;
            _won = true;
            yield return FinaleShow.Play(_garden, this);
            Shot("flock-finale.png");
            try { System.IO.File.Delete("/tmp/flock-five-finale"); } catch { }
            _busy = false;
            _finalePreview = false;
        }

        IEnumerator ShotStorm()
        {
            System.IO.Directory.CreateDirectory("/tmp/paradice");
            System.IO.File.WriteAllText("/tmp/flock-five-storm-now", "1");
            Load(0);
            yield return new WaitForSecondsRealtime(3.6f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("/tmp/paradice/storm-rain.png");
            yield return new WaitForSecondsRealtime(0.35f);
        }

        IEnumerator ShotIce()
        {
            System.IO.Directory.CreateDirectory("/tmp/paradice");
            Load(0);
            yield return new WaitForSecondsRealtime(1.0f);
            _frozen = true;
            _busy = true;
            _freezeOffer = true;
            StillBirds(true);
            if (_garden.Ice != null) yield return _garden.Ice.Coat();
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("/tmp/paradice/ice-coat.png");
            yield return new WaitForSecondsRealtime(0.35f);
            _gift = GiftFace.Card;
            yield return null;
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("/tmp/paradice/ice-dialog.png");
            yield return new WaitForSecondsRealtime(0.35f);
        }

        // Editor QA: garden 1 (jungle) vs garden 16 (desert oasis) background.
        IEnumerator ShotOasis()
        {
#if UNITY_EDITOR
            EditorShotLive = true;
            UnityEditor.EditorApplication.isPaused = false;
            Application.runInBackground = true;
            try
            {
#endif
                Time.timeScale = 1f;
                const string dir = "/tmp/paradice/oasis";
                System.IO.Directory.CreateDirectory(dir);
                Load(15);
                yield return new WaitForSecondsRealtime(1.6f);
                yield return SnapShot(dir + "/garden16.png");
                Load(0);
                yield return new WaitForSecondsRealtime(1.6f);
                yield return SnapShot(dir + "/garden1.png");
                Debug.Log("Flock Five: ShotOasis done");
#if UNITY_EDITOR
            }
            finally
            {
                Time.timeScale = 1f;
                EditorShotLive = false;
            }
#endif
        }

        // Editor QA: Amber Grove (garden 10) has two leaf-locked limbs.
        IEnumerator ShotLeaves()
        {
#if UNITY_EDITOR
            EditorShotLive = true;
            UnityEditor.EditorApplication.isPaused = false;
            Application.runInBackground = true;
            try
            {
#endif
                Time.timeScale = 1f;
                const string dir = "/tmp/paradice/leaves";
                System.IO.Directory.CreateDirectory(dir);
                Load(9);
                yield return new WaitForSecondsRealtime(1.4f);
                yield return SnapShot(dir + "/leaves-a.png");
                yield return new WaitForSecondsRealtime(1.1f);
                yield return SnapShot(dir + "/leaves-b.png");
                Debug.Log("Flock Five: ShotLeaves done");
#if UNITY_EDITOR
            }
            finally
            {
                Time.timeScale = 1f;
                EditorShotLive = false;
            }
#endif
        }

        // Test-only capture: selected-bird outline and mid-flight frames on a real garden.
        IEnumerator ShotBirds()
        {
#if UNITY_EDITOR
            EditorShotLive = true;
            UnityEditor.EditorApplication.isPaused = false;
            Application.runInBackground = true;
            Time.timeScale = 1f;
#endif
            const string dir = "/tmp/paradice/birds";
            System.IO.Directory.CreateDirectory(dir);
            Debug.Log("Flock Five: ShotBirds start");
            Load(0);
            yield return new WaitForSecondsRealtime(1.3f);
            yield return SnapShot(dir + "/birds-rest.png");
            int from = -1, to = -1;
            for (int a = 0; a < _board.Branches.Count && to < 0; a++)
            {
                if (!_board.CanPick(a) || Locked(a) || GiftLocked(a)) continue;
                for (int b = 0; b < _board.Branches.Count; b++)
                {
                    if (b == a || Locked(b) || GiftLocked(b)) continue;
                    if (_board.CanMove(a, b, out _)) { from = a; to = b; break; }
                }
                if (to < 0 && from < 0) from = a;
            }
            if (from >= 0)
            {
                Select(from);
                yield return new WaitForSecondsRealtime(0.45f);
                yield return SnapShot(dir + "/birds-selected.png");
            }
            if (from >= 0 && to >= 0)
            {
                // Slow the world so editor capture latency doesn't skip the flight.
                Time.timeScale = 0.12f;
                StartCoroutine(DoMove(from, to));
                float t0 = Time.time;
                float[] at = { 0.08f, 0.16f, 0.24f, 0.34f, 0.46f };
                for (int k = 0; k < at.Length; k++)
                {
                    while (Time.time - t0 < at[k]) yield return null;
                    Time.timeScale = 0f;
                    yield return SnapShot(dir + "/birds-fly-" + k + ".png");
                    Time.timeScale = 0.12f;
                }
                Time.timeScale = 1f;
                yield return new WaitForSecondsRealtime(1.2f);
                yield return SnapShot(dir + "/birds-landed.png");
            }
            Debug.Log("Flock Five: ShotBirds done from=" + from + " to=" + to);
#if UNITY_EDITOR
            EditorShotLive = false;
#endif
        }

        IEnumerator ShotGift()
        {
            const string dir = "/tmp/paradice";
            System.IO.Directory.CreateDirectory(dir);
            Load(0);
            yield return new WaitForSecondsRealtime(1.15f);
            yield return SnapShot(dir + "/gift-branch.png");
            _gift = GiftFace.Card;
            yield return null;
            yield return SnapShot(dir + "/gift-dialog.png");
            _giftThanksAt = Time.unscaledTime;
            _gift = GiftFace.Thanks;
            if (_garden.Root != null && _garden.Branches != null && _garden.Branches.Length > WorldBuilder.GiftIndex)
            {
                var br = _garden.Branches[WorldBuilder.GiftIndex];
                if (br != null) StartCoroutine(GiftPopBursts(br.transform.position));
            }
            yield return new WaitForSecondsRealtime(0.38f);
            yield return SnapShot(dir + "/gift-thanks.png");
            _gift = GiftFace.None;
        }

        IEnumerator ShotBuild79()
        {
            const string dir = "/Users/zfxgames/wkspaces/birdshot/gb";
            System.IO.Directory.CreateDirectory(dir);
            int nextKeep = PlayerPrefs.GetInt("flockfive.next", 0);
            bool hadNext = PlayerPrefs.HasKey("flockfive.next");
            bool hadSell = PlayerPrefs.HasKey(AdConsent.DoNotSellKey);
            int sell = PlayerPrefs.GetInt(AdConsent.DoNotSellKey, 0);
            PlayerPrefs.SetInt("flockfive.next", 0);
            PlayerPrefs.Save();
            AdConsent.DismissTransient();
            AdConsent.EditorHoldAtt = false;
            Screen.SetResolution(1179, 2556, false);
            // The first frames are the camera clear (a flat cyan) until the home
            // painting is on the camera. Hold the splash until that paint is up.
            yield return WaitForHomePaint();

            yield return SnapBuild79(dir + "/b79-home.png", false, false);
            AdConsent.DoNotSell = false;
            yield return SnapBuild79(dir + "/b79-settings-off.png", true, false);
            AdConsent.DoNotSell = true;
            yield return SnapBuild79(dir + "/b79-settings-on.png", true, false);
            AdConsent.DoNotSell = false;
            yield return SnapBuild79(dir + "/b79-att-preprompt.png", false, true);

            AdConsent.EditorHoldAtt = false;
            AdConsent.ResetTransient();
            if (hadNext) PlayerPrefs.SetInt("flockfive.next", nextKeep);
            else PlayerPrefs.DeleteKey("flockfive.next");
            if (hadSell) AdConsent.DoNotSell = sell != 0;
            else
            {
                AdConsent.DoNotSell = false;
                PlayerPrefs.DeleteKey(AdConsent.DoNotSellKey);
            }
            PlayerPrefs.Save();
            Debug.Log("B79_STILLS_DONE");
            var exit = System.Type.GetType("UnityEditor.EditorApplication, UnityEditor");
            var method = exit != null ? exit.GetMethod("Exit", new[] { typeof(int) }) : null;
            if (method != null) method.Invoke(null, new object[] { 0 });
        }

        // Sample a spot under the logo and left of the coins. Cyan means the
        // painting has not reached the camera yet.
        IEnumerator WaitForHomePaint()
        {
            float t = 0f;
            while (t < 8f)
            {
                PoseBuild79(false, false);
                yield return new WaitForEndOfFrame();
                Texture2D tex = null;
                try { tex = ScreenCapture.CaptureScreenshotAsTexture(); }
                catch (System.Exception) { tex = null; }
                bool ready = false;
                if (tex != null)
                {
                    int x = Mathf.Clamp(Mathf.RoundToInt(tex.width * 0.18f), 0, tex.width - 1);
                    int yTop = Mathf.RoundToInt(tex.height * 0.16f);
                    int y = Mathf.Clamp(tex.height - 1 - yTop, 0, tex.height - 1);
                    var c = tex.GetPixel(x, y);
                    ready = !(c.r < 0.12f && c.g > 0.70f && c.b > 0.70f);
                    Destroy(tex);
                }
                if (ready && t > 0.3f) yield break;
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        IEnumerator SnapBuild79(string path, bool settings, bool att)
        {
            float t = 0f;
            while (t < 0.45f)
            {
                PoseBuild79(settings, att);
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            PoseBuild79(settings, att);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForSecondsRealtime(0.55f);
        }

        void PoseBuild79(bool settings, bool att)
        {
            _splash = true;
            _home = HomeFace.Splash;
            _settingsOpen = settings;
            _welcomeOpen = false;
            _welcomeGlove = false;
            _dailyOpen = false;
            _dailyAskOpen = false;
            _adoptLive = false;
            _avatarRename = false;
            _hiveIntroLive = false;
            _pokerIntroLive = false;
            _dailyIntroLive = false;
            _streakSlide = -1f;
            VipOffer.Close();
            AdConsent.DismissTransient();
            AdConsent.EditorHoldAtt = att && !settings;
        }

        IEnumerator ShotHome()
        {
            System.IO.Directory.CreateDirectory("/tmp/paradice");
            _home = HomeFace.Splash;
            _splash = true;
            _pokerPayOpen = false;
            _pokerPayAnim = 0f;
            int keep = PlayerPrefs.GetInt("flockfive.next", 0);
            PlayerPrefs.SetInt("flockfive.next", 0);
            PlayerPrefs.Save();
            yield return new WaitForSecondsRealtime(0.45f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("/tmp/paradice/splash-home.png");
            yield return new WaitForSecondsRealtime(0.4f);
            if (_splashFlutters != null)
            {
                for (int i = 0; i < _splashFlutters.Length; i++)
                    _splashFlutters[i].Angle = Mathf.Repeat(_splashFlutters[i].Angle + 437.2f + i * 11.7f, Mathf.PI * 2f);
            }
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("/tmp/paradice/splash-halo-soak.png");
            yield return new WaitForSecondsRealtime(0.4f);
            PlayerPrefs.SetInt("flockfive.next", 1);
            PlayerPrefs.Save();
            yield return null;
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("/tmp/paradice/splash-joke-easy.png");
            yield return new WaitForSecondsRealtime(0.4f);
            PlayerPrefs.SetInt("flockfive.next", 3);
            PlayerPrefs.Save();
            yield return null;
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("/tmp/paradice/splash-joke-super-easy.png");
            yield return new WaitForSecondsRealtime(0.4f);
            PlayerPrefs.SetInt("flockfive.next", 5);
            PlayerPrefs.Save();
            yield return null;
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("/tmp/paradice/splash-joke-super-duper-easy.png");
            yield return new WaitForSecondsRealtime(0.4f);
            _home = HomeFace.Hive;
            yield return null;
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("/tmp/paradice/splash-hive.png");
            yield return new WaitForSecondsRealtime(0.4f);
            _home = HomeFace.Splash;
            PlayerPrefs.SetInt("flockfive.next", keep);
            PlayerPrefs.Save();
        }

        IEnumerator ShotHiveInspect()
        {
            const string dir = "/tmp/paradice";
            System.IO.Directory.CreateDirectory(dir);
            ShowSplash();
            _home = HomeFace.Splash;
            yield return new WaitForSecondsRealtime(0.35f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(dir + "/splash-home.png");
            yield return new WaitForSecondsRealtime(0.35f);
            Load(0);
            yield return new WaitForSecondsRealtime(0.85f);
            SeedGardenFive();
            yield return new WaitForSecondsRealtime(0.30f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(dir + "/garden-hive.png");
            yield return new WaitForSecondsRealtime(0.35f);
            ShowSplash();
            _home = HomeFace.Hive;
            if (Hive.Found == 0)
            {
                for (int n = 0; n < 4; n++) Hive.GrantVisitor();
            }
            int ix = 0;
            for (int k = 0; k < Hive.AlbumSlots; k++)
            {
                if (Hive.CountOfSlot(k) > 0) { ix = k; break; }
            }
            yield return new WaitForSecondsRealtime(0.45f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(dir + "/hive-page.png");
            _hiveInspect = ix;
            _hiveInspectT = 0f;
            _hiveInspectClosing = false;
            _hiveFaceBack[ix] = false;
            _hiveFlip = -1;
            _hiveInspectFrom = new Rect(Screen.width * 0.35f, Screen.height * 0.42f, Screen.width * 0.22f, Screen.height * 0.18f);
            yield return new WaitForSecondsRealtime(0.45f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(dir + "/hive-inspect-front.png");
            yield return new WaitForSecondsRealtime(0.2f);
            _hiveFlip = ix;
            _hiveFlipT = 0f;
            yield return new WaitForSecondsRealtime(0.40f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(dir + "/hive-inspect-back.png");
            yield return new WaitForSecondsRealtime(0.2f);
            BeginPutAwayInspect();
            yield return new WaitForSecondsRealtime(0.45f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(dir + "/hive-after-putaway.png");
        }

        IEnumerator ShotSparrow()
        {
            const string dir = "/tmp/paradice";
            System.IO.Directory.CreateDirectory(dir);
            Load(0);
            yield return new WaitForSecondsRealtime(1.05f);
            if (_garden.Feeders != null && _garden.Root != null)
                StartCoroutine(SparrowView.Visit(_garden.Feeders, _garden.Root));
            yield return new WaitForSecondsRealtime(1.35f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(dir + "/sparrow-perch.png");
        }

        IEnumerator ShotSplashButtons()
        {
            // Quick home splash clip so bots can proof birds around the title.
            const string dir = "/tmp/paradice";
            System.IO.Directory.CreateDirectory(dir);
            _shotLevelNumber = 1;
            _shotEase = "";
            PlayerPrefs.SetInt("flockfive.next", 0);
            PlayerPrefs.Save();
            _home = HomeFace.Splash;
            _splash = true;
            yield return new WaitForSecondsRealtime(0.55f);

            string frames = dir + "/splash-frames";
            try { if (System.IO.Directory.Exists(frames)) System.IO.Directory.Delete(frames, true); } catch { }
            System.IO.Directory.CreateDirectory(frames);
            _recordSmash = true;
            var rec = StartCoroutine(RecordSmashFrames(frames));
            yield return new WaitForSecondsRealtime(4.2f);
            _recordSmash = false;
            yield return rec;
            float fps = _recordFrameCount / 4.2f;
            if (fps < 5f) fps = 15f;
            EncodeSmashVideo(frames, dir + "/splash-birds.mp4", fps);
        }

        IEnumerator ShotStreak()
        {
#if UNITY_EDITOR
            EditorShotLive = true;
            UnityEditor.EditorApplication.isPaused = false;
            Application.runInBackground = true;
#endif
            const string dir = "/tmp/paradice";
            System.IO.Directory.CreateDirectory(dir);
            yield return RunStreakShots(dir);
            Debug.Log("Flock Five: ShotStreak done");
#if UNITY_EDITOR
            EditorShotLive = false;
#endif
        }

        IEnumerator RunStreakShots(string dir)
        {
            System.IO.Directory.CreateDirectory(dir);
            PrefGuard.SetInt("flockfive.streak", 5);
            PlayerPrefs.SetInt("flockfive.instage", 0);
            PlayerPrefs.Save();
            ShowSplash();
            Purse.CueWin(5, Purse.StagePay * 5 * Mathf.Max(1, Purse.LoginMul));
            ArmStreakSlide();
            var beat = RewardBeats(true);
            float peek = beat.AmtAt;
            float pop = beat.StampAt + RewardStampDur * RewardStampLandU();
            float hold = beat.PayAt + RewardHoldDur * 0.5f;
            float tuckAt = beat.HoldEnd + RewardFadeDur * 0.45f;
            yield return new WaitForSecondsRealtime(peek);
            yield return SnapShot(dir + "/streak-peek.png");
            yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, pop - peek));
            yield return SnapShot(dir + "/streak-pop.png");
            yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, hold - pop));
            yield return SnapShot(dir + "/streak-hold.png");
            yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, tuckAt - hold));
            yield return SnapShot(dir + "/streak-tuck.png");
        }

        IEnumerator RecordSmashFrames(string frames)
        {
            int i = 0;
            _recordFrameCount = 0;
            float next = Time.unscaledTime;
            const float step = 1f / 30f;
            while (_recordSmash)
            {
                yield return new WaitForEndOfFrame();
                if (Time.unscaledTime < next) continue;
                var tex = ScreenCapture.CaptureScreenshotAsTexture();
                if (tex == null) continue;
                try
                {
                    System.IO.File.WriteAllBytes(
                        frames + "/" + i.ToString("D4") + ".jpg",
                        tex.EncodeToJPG(82));
                    i++;
                    _recordFrameCount = i;
                    next = Time.unscaledTime + step;
                }
                catch { }
                Destroy(tex);
            }
        }

        static void EncodeSmashVideo(string frames, string mp4, float fps, string wav = null)
        {
            string rate = fps.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            string args = "-y -framerate " + rate + " -i \"" + frames + "/%04d.jpg\" ";
            if (!string.IsNullOrEmpty(wav) && System.IO.File.Exists(wav))
                args += "-i \"" + wav + "\" -c:v libx264 -pix_fmt yuv420p -c:a aac -shortest -crf 23 -movflags +faststart \"" + mp4 + "\"";
            else
                args += "-c:v libx264 -pix_fmt yuv420p -crf 23 -movflags +faststart \"" + mp4 + "\"";
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "/opt/homebrew/bin/ffmpeg",
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            try
            {
                using (var p = System.Diagnostics.Process.Start(psi))
                {
                    if (p == null) return;
                    p.WaitForExit(120000);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("finale smash encode: " + e.Message);
            }
        }

        class MixTap : MonoBehaviour
        {
            public volatile bool On;
            readonly System.Collections.Generic.List<float> Buf = new System.Collections.Generic.List<float>(44100 * 8);
            public void Clear() { lock (Buf) Buf.Clear(); }
            public float[] Dump() { lock (Buf) return Buf.ToArray(); }
            void OnAudioFilterRead(float[] data, int channels)
            {
                if (!On) return;
                lock (Buf)
                {
                    for (int i = 0; i < data.Length; i++) Buf.Add(data[i]);
                }
            }
        }

        MixTap EnsureMixTap()
        {
            var cam = Camera.main;
            if (cam == null) return null;
            var tap = cam.GetComponent<MixTap>();
            if (tap == null) tap = cam.gameObject.AddComponent<MixTap>();
            return tap;
        }

        static void WriteWav(string path, float[] samples, int channels, int rate)
        {
            if (samples == null || samples.Length < channels) return;
            int frames = samples.Length / channels;
            short[] pcm = new short[samples.Length];
            for (int i = 0; i < samples.Length; i++)
            {
                float v = Mathf.Clamp(samples[i], -1f, 1f);
                pcm[i] = (short)Mathf.RoundToInt(v * 32767f);
            }
            using (var fs = new System.IO.FileStream(path, System.IO.FileMode.Create, System.IO.FileAccess.Write))
            using (var bw = new System.IO.BinaryWriter(fs))
            {
                int dataBytes = pcm.Length * 2;
                bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                bw.Write(36 + dataBytes);
                bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
                bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                bw.Write(16);
                bw.Write((short)1);
                bw.Write((short)channels);
                bw.Write(rate);
                bw.Write(rate * channels * 2);
                bw.Write((short)(channels * 2));
                bw.Write((short)16);
                bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                bw.Write(dataBytes);
                for (int i = 0; i < pcm.Length; i++) bw.Write(pcm[i]);
            }
        }

        IEnumerator ShotPokerPunches()
        {
            const string dir = "/tmp/paradice/poker-punches";
            const string seedPath = "/tmp/flock-five-poker-seeds.txt";
            System.IO.Directory.CreateDirectory(dir);
            _home = HomeFace.Poker;
            _splash = true;
            BirdPoker.Boot();
            Purse.Boot();
            if (Purse.Coins < 200000) Purse.Credit(200000 - Purse.Coins);
            EnsureMixTap();

            var seeds = new int[BirdPoker.PunchKinds];
            var ranks = new string[BirdPoker.PunchKinds];
            int bingoKind = 0, bingoSeed = -1;
            if (System.IO.File.Exists(seedPath))
            {
                var lines = System.IO.File.ReadAllLines(seedPath);
                for (int i = 0; i < lines.Length; i++)
                {
                    var p = lines[i].Trim().Split(' ');
                    if (p.Length < 3) continue;
                    if (p[0] == "bingo")
                    {
                        int.TryParse(p[1], out bingoKind);
                        int.TryParse(p[2], out bingoSeed);
                        continue;
                    }
                    int k, s;
                    if (!int.TryParse(p[0], out k) || !int.TryParse(p[1], out s)) continue;
                    if ((uint)k >= (uint)seeds.Length) continue;
                    seeds[k] = s;
                    ranks[k] = p[2];
                }
            }

            int keepFull = BirdPoker.FullcardCount;
            int keepCoins = Purse.Coins;
            var clips = new System.Collections.Generic.List<string>();
            // Fast review set: regular, bow, crown, wilds — then fullcard.
            int[] review =
            {
                BirdPoker.KindId(BirdColor.Ruby, BirdSex.Neutral),
                BirdPoker.KindId(BirdColor.Ruby, BirdSex.Female),
                BirdPoker.KindId(BirdColor.Ruby, BirdSex.Male),
                BirdPoker.WildKind
            };
            string[] reviewName = { "regular", "bow", "crown", "wilds" };
            for (int n = 0; n < review.Length; n++)
            {
                int k = review[n];
                string mp4 = dir + "/poker-punch-" + reviewName[n] + ".mp4";
                BirdPoker.ClearPunches();
                if (k == BirdPoker.WildKind && seeds[k] <= 0)
                    yield return ShotForcedWildPunch(mp4, 3.2f);
                else if (seeds[k] > 0)
                    yield return ShotOnePokerPunch(k, seeds[k], mp4, 3.2f);
                else if (k == BirdPoker.WildKind)
                    yield return ShotForcedWildPunch(mp4, 3.2f);
                else
                    continue;
                if (System.IO.File.Exists(mp4)) clips.Add(mp4);
            }
            int bk = bingoKind;
            int bs = bingoSeed;
            if (bs <= 0)
            {
                for (int k = 0; k < seeds.Length; k++)
                    if (seeds[k] > 0) { bk = k; bs = seeds[k]; break; }
            }
            string bingoMp4 = dir + "/poker-punch-bingo.mp4";
            if (bs > 0)
            {
                BirdPoker.FillPunchesExcept(bk);
                yield return ShotOnePokerPunch(bk, bs, bingoMp4, 5.8f);
            }
            else
            {
                BirdPoker.FillPunchesExcept(BirdPoker.WildKind);
                yield return ShotForcedWildPunch(bingoMp4, 5.8f);
            }
            if (System.IO.File.Exists(bingoMp4)) clips.Add(bingoMp4);
            BirdPoker.RestoreFullcardCount(keepFull);
            int delta = Purse.Coins - keepCoins;
            if (delta > 0) Purse.TrySpend(delta);
            else if (delta < 0) Purse.Credit(-delta);
            string reel = dir + "/flock-five-poker-achievements.mp4";
            string playtest = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(Application.dataPath, "../Playtest/poker-punches/flock-five-poker-achievements.mp4"));
            AssemblePunchReel(clips, reel);
            if (System.IO.File.Exists(reel))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(playtest));
                    System.IO.File.Copy(reel, playtest, true);
                }
                catch { }
                PlayReviewReel(System.IO.File.Exists(playtest) ? playtest : reel);
            }
            System.IO.File.WriteAllText("/tmp/paradice/poker-punches-done", "1");
        }

        IEnumerator ShotForcedWildPunch(string mp4, float seconds)
        {
            if (Purse.Coins < 50) Purse.Credit(80);
            BirdPoker.ResetRound();
            BirdPoker.ForceFiveWilds();
            string frames = "/tmp/paradice/poker-frames";
            try { if (System.IO.Directory.Exists(frames)) System.IO.Directory.Delete(frames, true); } catch { }
            System.IO.Directory.CreateDirectory(frames);
            var tap = EnsureMixTap();
            if (tap != null) { tap.Clear(); tap.On = true; }
            _recordSmash = true;
            _recordFrameCount = 0;
            var rec = StartCoroutine(RecordSmashFrames(frames));
            yield return new WaitForSecondsRealtime(0.45f);
            if (BirdPoker.LastPunchFresh)
                BeginPokerStamp(BirdPoker.LastPunchKind);
            yield return new WaitForSecondsRealtime(Mathf.Max(0.8f, seconds - 0.45f));
            _recordSmash = false;
            if (tap != null) tap.On = false;
            yield return rec;
            float fps = _recordFrameCount / Mathf.Max(0.4f, seconds);
            if (fps < 5f) fps = 12f;
            string wav = mp4.Replace(".mp4", ".wav");
            if (tap != null)
            {
                var samples = tap.Dump();
                int ch = AudioSettings.speakerMode == AudioSpeakerMode.Mono ? 1 : 2;
                WriteWav(wav, samples, ch, AudioSettings.outputSampleRate);
            }
            EncodeSmashVideo(frames, mp4, fps, System.IO.File.Exists(wav) ? wav : null);
            _pokerStamp = false;
            _pokerBingo = false;
        }

        static void AssemblePunchReel(System.Collections.Generic.List<string> clips, string dest)
        {
            if (clips == null || clips.Count == 0) return;
            string list = "/tmp/paradice/poker-punches/concat.txt";
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < clips.Count; i++)
            {
                if (!System.IO.File.Exists(clips[i])) continue;
                sb.Append("file '").Append(clips[i].Replace("'", "'\\''")).Append("'\n");
            }
            System.IO.File.WriteAllText(list, sb.ToString());
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "/opt/homebrew/bin/ffmpeg",
                Arguments = "-y -f concat -safe 0 -i \"" + list + "\" -c:v libx264 -pix_fmt yuv420p -c:a aac -movflags +faststart \"" + dest + "\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            try
            {
                using (var p = System.Diagnostics.Process.Start(psi))
                {
                    if (p != null) p.WaitForExit(180000);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("punch reel concat: " + e.Message);
            }
        }

        static void PlayReviewReel(string mp4)
        {
            if (string.IsNullOrEmpty(mp4) || !System.IO.File.Exists(mp4)) return;
            string scpt = "/tmp/flock-five-play-reel.scpt";
            string body =
                "tell application \"QuickTime Player\"\n" +
                "activate\n" +
                "open POSIX file \"" + mp4 + "\"\n" +
                "delay 0.4\n" +
                "try\n" +
                "set looping of document 1 to false\n" +
                "end try\n" +
                "play document 1\n" +
                "end tell\n";
            try
            {
                System.IO.File.WriteAllText(scpt, body);
                var qt = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "/usr/bin/osascript",
                    Arguments = scpt,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (var p = System.Diagnostics.Process.Start(qt))
                {
                    if (p != null) p.WaitForExit(8000);
                    if (p != null && p.ExitCode == 0) return;
                }
            }
            catch { }
            try
            {
                System.Diagnostics.Process.Start("open", "\"" + mp4 + "\"");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("play reel: " + e.Message);
            }
        }

        IEnumerator ShotOnePokerPunch(int kind, int seed, string mp4, float seconds)
        {
            if (Purse.Coins < 50) Purse.Credit(80);
            BirdPoker.ResetRound();
            BirdPoker.SeedRng(seed);
            if (!BirdPoker.Deal()) yield break;
            BirdPoker.HoldForKind(kind);
            BirdPoker.Draw();
            string frames = "/tmp/paradice/poker-frames";
            try { if (System.IO.Directory.Exists(frames)) System.IO.Directory.Delete(frames, true); } catch { }
            System.IO.Directory.CreateDirectory(frames);
            var tap = EnsureMixTap();
            if (tap != null) { tap.Clear(); tap.On = true; }
            _recordSmash = true;
            _recordFrameCount = 0;
            var rec = StartCoroutine(RecordSmashFrames(frames));
            yield return new WaitForSecondsRealtime(0.45f);
            if (BirdPoker.LastPunchFresh)
                BeginPokerStamp(BirdPoker.LastPunchKind);
            yield return new WaitForSecondsRealtime(Mathf.Max(0.8f, seconds - 0.45f));
            _recordSmash = false;
            if (tap != null) tap.On = false;
            yield return rec;
            float fps = _recordFrameCount / Mathf.Max(0.4f, seconds);
            if (fps < 5f) fps = 12f;
            string wav = mp4.Replace(".mp4", ".wav");
            if (tap != null)
            {
                var samples = tap.Dump();
                int ch = AudioSettings.speakerMode == AudioSpeakerMode.Mono ? 1 : 2;
                WriteWav(wav, samples, ch, AudioSettings.outputSampleRate);
            }
            EncodeSmashVideo(frames, mp4, fps, System.IO.File.Exists(wav) ? wav : null);
            _pokerStamp = false;
            _pokerBingo = false;
        }

        IEnumerator ShotSplashLevel(int number, int next, string ease, string path)
        {
            _shotLevelNumber = number;
            _shotEase = ease;
            PlayerPrefs.SetInt("flockfive.next", next);
            PlayerPrefs.Save();
            _home = HomeFace.Splash;
            _splash = true;
            yield return new WaitForSecondsRealtime(0.55f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForSecondsRealtime(0.4f);
        }

#if UNITY_EDITOR
        public static bool EditorShotLive;
#endif

        IEnumerator ShotPokerFaces()
        {
#if UNITY_EDITOR
            EditorShotLive = true;
            UnityEditor.EditorApplication.isPaused = false;
            Application.runInBackground = true;
            Time.timeScale = 1f;
#endif
            const string dir = "/tmp/paradice";
            yield return RunPokerFacesShots(dir);
            System.IO.File.WriteAllText(dir + "/poker-faces-done", "1");
#if UNITY_EDITOR
            EditorShotLive = false;
            Debug.Log("Flock Five: ShotPokerFaces done");
#endif
        }

        IEnumerator RunPokerFacesShots(string dir)
        {
            System.IO.Directory.CreateDirectory(dir);
            SpriteCatalog.DropPokerArt();
            Debug.Log("Flock Five: ShotPokerFaces start " + dir + " pass19-singular-card");
            _home = HomeFace.Poker;
            _splash = true;
            _pokerPayOpen = false;
            _pokerPayAnim = 0f;
            _pokerFan = false;
            _pokerMotion = PokerMotion.None;
            _pokerKeepHint = true;
            _pokerHover = -1;
            BirdPoker.Boot();
            Purse.Boot();
            if (Purse.Coins < 400) Purse.Credit(400 - Purse.Coins);
            BirdPoker.BeginVisit();
            for (int i = 0; i < 18; i++)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPaused = false;
#endif
                yield return null;
            }
            yield return SnapShot(dir + "/poker-backs.png");
            yield return SnapShot(dir + "/poker-idle-deal.png");
            Debug.Log("Flock Five: captured poker-backs");
            if (!BirdPoker.Deal()) yield break;
            BirdPoker.Hand[0] = BirdPoker.Card.Of(BirdColor.Ruby, BirdSex.Neutral);
            BirdPoker.Hand[1] = BirdPoker.Card.MakeWild();
            BirdPoker.Hand[2] = BirdPoker.Card.Of(BirdColor.Teal, BirdSex.Female);
            BirdPoker.Hand[3] = BirdPoker.Card.MakeWild();
            BirdPoker.Hand[4] = BirdPoker.Card.Of(BirdColor.Gold, BirdSex.Male);
            BeginPokerDeal();
            yield return new WaitForSecondsRealtime(1.85f);
            yield return SnapShot(dir + "/poker-fan.png");
            _pokerHover = 2;
            yield return new WaitForSecondsRealtime(0.20f);
            yield return SnapShot(dir + "/poker-fan-hover.png");
            ApplyPokerHold(0);
            _pokerHover = -1;
            yield return new WaitForSecondsRealtime(0.45f);
            yield return SnapShot(dir + "/poker-hold-one.png");
            ApplyPokerHold(2);
            yield return new WaitForSecondsRealtime(0.45f);
            yield return SnapShot(dir + "/poker-fan-hold.png");
            yield return SnapShot(dir + "/poker-hold-two.png");
            ApplyPokerHold(1);
            yield return new WaitForSecondsRealtime(0.40f);
            yield return SnapShot(dir + "/poker-hold-three.png");
            ApplyPokerHold(3);
            ApplyPokerHold(4);
            yield return new WaitForSecondsRealtime(0.50f);
            yield return SnapShot(dir + "/poker-hold-all.png");
            ApplyPokerHold(4);
            yield return new WaitForSecondsRealtime(0.45f);
            yield return SnapShot(dir + "/poker-unhold-one.png");
            ApplyPokerHold(4);
            ApplyPokerHold(1);
            ApplyPokerHold(3);
            ApplyPokerHold(4);
            for (int i = 0; i < BirdPoker.HandSize; i++)
            {
                _pokerRedraw[i] = !BirdPoker.Hold[i];
                _pokerPrev[i] = BirdPoker.Hand[i];
            }
            BirdPoker.Draw();
            BeginPokerDraw();
            yield return new WaitForSecondsRealtime(0.22f);
            yield return SnapShot(dir + "/poker-chain-break.png");
            // SnapShot waits 0.40s; land the burst in the first redraw pop (Y-spin + confetti).
            yield return new WaitForSecondsRealtime(0.02f);
            yield return SnapShot(dir + "/poker-burst.png");
            yield return new WaitForSecondsRealtime(1.00f);
            yield return SnapShot(dir + "/poker-row.png");
            _pokerMotion = PokerMotion.None;
            _pokerFan = false;
            yield return new WaitForSecondsRealtime(0.35f);
            yield return SnapShot(dir + "/poker-faces-paper.png");
            yield return new WaitForSecondsRealtime(0.45f);
            yield return SnapShot(dir + "/poker-faces-sparkle.png");
            _pokerPayOpen = true;
            _pokerPayAnim = 1f;
            yield return new WaitForSecondsRealtime(0.40f);
            yield return SnapShot(dir + "/poker-faces-pay.png");
        }

#if UNITY_EDITOR
        // Editor-only feel review: hand sway extremes, discard bursts on slots 2+4, win fanfare
        // frames and a timed JPG frame run for a clip. Triggered by /tmp/flock-five-poker-feel.
        const string FeelDir = "/tmp/paradice/feel";
        const float FeelSwayA = 39.666f; // turn ≈ +1.30°
        const float FeelSwayB = 28.214f; // turn ≈ −1.28°
        bool _feelRec;

        IEnumerator ShotPokerFeel()
        {
            EditorShotLive = true;
            UnityEditor.EditorApplication.isPaused = false;
            Application.runInBackground = true;
            Time.timeScale = 1f;
            System.IO.Directory.CreateDirectory(FeelDir);
            System.IO.Directory.CreateDirectory(FeelDir + "/frames");
            Debug.Log("Flock Five: ShotPokerFeel start");
            SpriteCatalog.DropPokerArt();
            _home = HomeFace.Poker;
            _splash = true;
            _pokerPayOpen = false;
            _pokerPayAnim = 0f;
            _pokerFan = false;
            _pokerMotion = PokerMotion.None;
            _pokerKeepHint = false;
            _pokerHover = -1;
            BirdPoker.Boot();
            Purse.Boot();
            int keepCoins = Purse.Coins;
            if (Purse.Coins < 5000) Purse.Credit(5000 - Purse.Coins);
            BirdPoker.BeginVisit();
            while (BirdPoker.CanNudge(1)) BirdPoker.NudgeBet(1);
            for (int i = 0; i < 18; i++) yield return null;
            yield return SnapShot(FeelDir + "/feel-idle-deal.png");

            // Pass 1: stills of the held fan at both sway extremes.
            FeelDeal();
            yield return new WaitForSecondsRealtime(1.85f);
            yield return FeelSwayShot("feel-fan");
            ApplyPokerHold(0);
            ApplyPokerHold(2);
            yield return new WaitForSecondsRealtime(0.55f);
            yield return FeelSwayShot("feel-fan-hold");
            ApplyPokerHold(0);
            ApplyPokerHold(2);
            yield return new WaitForSecondsRealtime(0.55f);

            // Pass 2: timed frame run — live sway, keep 0/1/3, draw (discards 2+4), full win fanfare.
            StartCoroutine(FeelRecord());
            yield return new WaitForSecondsRealtime(4.5f);
            ApplyPokerHold(0);
            yield return new WaitForSecondsRealtime(0.35f);
            ApplyPokerHold(1);
            yield return new WaitForSecondsRealtime(0.35f);
            ApplyPokerHold(3);
            yield return new WaitForSecondsRealtime(1.2f);
            FeelDraw();
            while (PokerMotionBusy() || PokerWinFlying()) yield return null;
            yield return new WaitForSecondsRealtime(1.2f);
            _feelRec = false;
            yield return new WaitForSecondsRealtime(0.3f);

            // Pass 3: crisp stills — discard mid-confetti, fanfare key frames, rested result + bet bar.
            BirdPoker.Collect();
            FeelDeal();
            yield return new WaitForSecondsRealtime(1.85f);
            ApplyPokerHold(0);
            ApplyPokerHold(1);
            ApplyPokerHold(3);
            yield return new WaitForSecondsRealtime(0.6f);
            FeelDraw();
            yield return FeelAtMotion(0.70f, "feel-discard-a.png");
            yield return FeelAtMotion(0.80f, "feel-discard-b.png");
            yield return FeelAtMotion(0.90f, "feel-discard-c.png");
            while (PokerMotionBusy()) yield return null;
            yield return FeelAtWin(0.17f, "feel-win-punch.png");
            yield return FeelAtWin(0.70f, "feel-win-hold.png");
            yield return FeelAtWin(WinWhooshAt + WinWhooshT * 0.35f, "feel-win-whoosh-a.png");
            yield return FeelAtWin(WinWhooshAt + WinWhooshT * 0.55f, "feel-win-whoosh-b.png");
            yield return FeelAtWin(WinLandAt + WinLandT * 0.45f, "feel-win-land.png");
            _pokerWinT = WinDoneAt;
            yield return new WaitForSecondsRealtime(0.8f);
            yield return SnapShot(FeelDir + "/feel-result-betbar.png");
            string bet = "bet " + BirdPoker.Bet + " phase " + BirdPoker.PhaseNow
                + " open " + BirdPoker.BetOpen + " plus " + BirdPoker.CanNudge(1) + " minus " + BirdPoker.CanNudge(-1)
                + " win " + BirdPoker.LastWin + " rank " + BirdPoker.RankLabel(BirdPoker.LastRank);
            BirdPoker.NudgeBet(-1);
            yield return SnapShot(FeelDir + "/feel-result-bet-nudged.png");
            bet += " | after minus bet " + BirdPoker.Bet + " coins " + Purse.Coins;
            System.IO.File.WriteAllText(FeelDir + "/feel-state.txt", bet + "\n");

            // Pass 4: stepped frame strips — the whole redeal, then the whoosh into the banner.
            System.IO.Directory.CreateDirectory(FeelDir + "/strip-redeal");
            System.IO.Directory.CreateDirectory(FeelDir + "/strip-whoosh");
            var stripLog = new System.Text.StringBuilder();
            BirdPoker.Collect();
            FeelDeal();
            yield return new WaitForSecondsRealtime(1.85f);
            ApplyPokerHold(0);
            ApplyPokerHold(1);
            ApplyPokerHold(3);
            yield return new WaitForSecondsRealtime(0.6f);
            FeelDraw();
            int frame = 0;
            for (float mt = 0f; mt < _pokerRowEnd; mt += 1f / 30f)
            {
                yield return null;
                _pokerMotionT = mt;
                string file = "strip-redeal/r" + frame.ToString("D3") + ".jpg";
                yield return FeelGrab(file);
                stripLog.Append(file).Append(" motionT ").Append(_pokerMotionT.ToString("F3")).Append('\n');
                frame++;
            }
            while (PokerMotionBusy()) yield return null;
            frame = 0;
            for (float wt = WinWhooshAt - 0.10f; wt < WinDoneAt + 0.10f; wt += 1f / 60f)
            {
                yield return null;
                _pokerWinT = wt;
                string file = "strip-whoosh/w" + frame.ToString("D3") + ".jpg";
                yield return FeelGrab(file);
                stripLog.Append(file).Append(" winT ").Append(_pokerWinT.ToString("F3")).Append('\n');
                frame++;
            }
            System.IO.File.WriteAllText(FeelDir + "/strip-times.txt", stripLog.ToString());

            BirdPoker.ResetRound();
            _pokerShowPay = false;
            _pokerWinT = -1f;
            if (Purse.Coins > keepCoins) Purse.TrySpend(Purse.Coins - keepCoins);
            else if (Purse.Coins < keepCoins) Purse.Credit(keepCoins - Purse.Coins);
            System.IO.File.WriteAllText(FeelDir + "/feel-done", "1");
            EditorShotLive = false;
            Debug.Log("Flock Five: ShotPokerFeel done");
        }

        void FeelDeal()
        {
            BirdPoker.ResetRound();
            if (!BirdPoker.Deal()) return;
            BirdPoker.Hand[0] = BirdPoker.Card.Of(BirdColor.Ruby, BirdSex.Neutral);
            BirdPoker.Hand[1] = BirdPoker.Card.MakeWild();
            BirdPoker.Hand[2] = BirdPoker.Card.Of(BirdColor.Teal, BirdSex.Female);
            BirdPoker.Hand[3] = BirdPoker.Card.MakeWild();
            BirdPoker.Hand[4] = BirdPoker.Card.Of(BirdColor.Gold, BirdSex.Male);
            BeginPokerDeal();
        }

        // Ruby + two wilds kept: the draw always pays (trips or better) at the real bet.
        void FeelDraw()
        {
            for (int i = 0; i < BirdPoker.HandSize; i++)
            {
                _pokerRedraw[i] = !BirdPoker.Hold[i];
                _pokerPrev[i] = BirdPoker.Hand[i];
            }
            BirdPoker.Draw();
            BeginPokerDraw();
            _pokerPendingStamp = false;
            _pokerResultCue = BirdPoker.LastWin > 0 ? 2 : 1;
        }

        IEnumerator FeelSwayShot(string name)
        {
            yield return null;
            _pokerSwayT = FeelSwayA;
            yield return SnapShot(FeelDir + "/" + name + "-swayA.png");
            yield return null;
            _pokerSwayT = FeelSwayB;
            yield return SnapShot(FeelDir + "/" + name + "-swayB.png");
        }

        IEnumerator FeelAtMotion(float at, string file)
        {
            while (_pokerMotion == PokerMotion.Draw && _pokerMotionT < at) yield return null;
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(FeelDir + "/" + file);
            System.IO.File.AppendAllText(FeelDir + "/feel-state.log", file + " motionT " + _pokerMotionT + "\n");
        }

        IEnumerator FeelAtWin(float at, string file)
        {
            yield return null;
            _pokerWinT = at;
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(FeelDir + "/" + file);
            System.IO.File.AppendAllText(FeelDir + "/feel-state.log", file + " winT " + _pokerWinT + "\n");
            yield return new WaitForSecondsRealtime(0.25f);
        }

        IEnumerator FeelGrab(string file)
        {
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            System.IO.File.WriteAllBytes(FeelDir + "/" + file, tex.EncodeToJPG(88));
            Destroy(tex);
        }

        IEnumerator FeelRecord()
        {
            _feelRec = true;
            var times = new System.Text.StringBuilder();
            float t0 = Time.unscaledTime;
            int n = 0;
            while (_feelRec)
            {
                yield return new WaitForEndOfFrame();
                var tex = ScreenCapture.CaptureScreenshotAsTexture();
                System.IO.File.WriteAllBytes(FeelDir + "/frames/f" + n.ToString("D4") + ".jpg", tex.EncodeToJPG(88));
                Destroy(tex);
                times.Append(n).Append(' ').Append((Time.unscaledTime - t0).ToString("F4")).Append('\n');
                n++;
            }
            System.IO.File.WriteAllText(FeelDir + "/frames/times.txt", times.ToString());
        }
#endif

        IEnumerator ShotHandQa()
        {
#if UNITY_EDITOR
            EditorShotLive = true;
            UnityEditor.EditorApplication.isPaused = false;
            Application.runInBackground = true;
            Time.timeScale = 1f;
#endif
            const string dir = "/tmp/paradice/qa";
            System.IO.Directory.CreateDirectory(dir);
            _home = HomeFace.Poker;
            _splash = true;
            _pokerPayOpen = false;
            _pokerPayAnim = 0f;
            _pokerFan = false;
            _pokerMotion = PokerMotion.None;
            _pokerKeepHint = true;
            _pokerHover = -1;
            BirdPoker.Boot();
            Purse.Boot();
            if (Purse.Coins < 400) Purse.Credit(400 - Purse.Coins);
            BirdPoker.BeginVisit();
            for (int i = 0; i < 18; i++)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPaused = false;
#endif
                yield return null;
            }
            if (!BirdPoker.Deal()) yield break;
            BirdPoker.Hand[0] = BirdPoker.Card.Of(BirdColor.Ruby, BirdSex.Neutral);
            BirdPoker.Hand[1] = BirdPoker.Card.MakeWild();
            BirdPoker.Hand[2] = BirdPoker.Card.Of(BirdColor.Teal, BirdSex.Female);
            BirdPoker.Hand[3] = BirdPoker.Card.MakeWild();
            BirdPoker.Hand[4] = BirdPoker.Card.Of(BirdColor.Gold, BirdSex.Male);
            BeginPokerDeal();
            yield return new WaitForSecondsRealtime(1.12f);
            yield return SnapShot(dir + "/hand-deal-flip.png");
            yield return new WaitForSecondsRealtime(0.85f);
            yield return SnapShot(dir + "/hand-00-fan.png");
            ApplyPokerHold(0);
            yield return new WaitForSecondsRealtime(0.40f);
            yield return SnapShot(dir + "/hand-01-keep.png");
            ApplyPokerHold(2);
            yield return new WaitForSecondsRealtime(0.40f);
            yield return SnapShot(dir + "/hand-02-keep.png");
            ApplyPokerHold(4);
            yield return new WaitForSecondsRealtime(0.40f);
            yield return SnapShot(dir + "/hand-03-keep.png");
            System.IO.File.WriteAllText(dir + "/hand-qa-done", "1");
#if UNITY_EDITOR
            EditorShotLive = false;
            Debug.Log("Flock Five: ShotHandQa done");
#endif
        }

        IEnumerator SnapShot(string path)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPaused = false;
#endif
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForSecondsRealtime(0.40f);
        }

        IEnumerator ShotConsumerTour()
        {
#if UNITY_EDITOR
            EditorShotLive = true;
            UnityEditor.EditorApplication.isPaused = false;
            Application.runInBackground = true;
            Time.timeScale = 1f;
#endif
            const string qa = "/tmp/paradice/qa";
            System.IO.Directory.CreateDirectory(qa);
            Debug.Log("Flock Five: ShotConsumerTour start");

            int keepNext = PlayerPrefs.GetInt("flockfive.next", 0);
            int keepCoins = PrefGuard.GetInt("flockfive.coins", 0);
            int keepStreak = PrefGuard.GetInt("flockfive.streak", 0);
            int keepStage = PlayerPrefs.GetInt("flockfive.instage", 0);

            PlayerPrefs.SetInt("flockfive.next", 0);
            PlayerPrefs.Save();
            Purse.Pending = 0;
            _streakSlide = -1f;
            _streakChirped = false;
            _streakAnnounced = 0;
            ShowSplash();
            Purse.Pending = 0;
            _streakSlide = -1f;
            yield return new WaitForSecondsRealtime(0.55f);
            yield return SnapShot(qa + "/splash-home.png");

            Load(0);
            yield return new WaitForSecondsRealtime(1.15f);
            SeedGardenFive();
            yield return new WaitForSecondsRealtime(0.35f);
            yield return SnapShot(qa + "/garden-five.png");

            yield return SnapShot(qa + "/gift-branch.png");
            _gift = GiftFace.Card;
            yield return null;
            yield return SnapShot(qa + "/gift-dialog.png");
            _gift = GiftFace.None;

            yield return RunPokerFacesShots(qa);
            yield return RunStreakShots(qa);

            PrefGuard.SetInt("flockfive.coins", keepCoins);
            PrefGuard.SetInt("flockfive.streak", keepStreak);
            PlayerPrefs.SetInt("flockfive.next", keepNext);
            PlayerPrefs.SetInt("flockfive.instage", keepStage);
            PlayerPrefs.Save();
            Purse.Boot();

            System.IO.File.WriteAllText(qa + "/consumer-tour-done", "1");
#if UNITY_EDITOR
            EditorShotLive = false;
            Debug.Log("Flock Five: ShotConsumerTour done");
#endif
        }

        void SeedGardenFive()
        {
            if (_board == null) return;
            int pick = -1;
            for (int i = 0; i < _board.Branches.Count; i++)
            {
                var st = _board.Branches[i];
                if (st.Broken || st.AdLocked || st.Count != 4) continue;
                bool same = true;
                for (int k = 1; k < st.Count; k++)
                    if (st.Birds[k].Color != st.Birds[0].Color) { same = false; break; }
                if (!same) continue;
                if (_board.LiveHas(st.Birds[0].Color)) continue;
                pick = i;
                if ((i & 1) == 1) break;
            }
            if (pick < 0)
            {
                for (int i = 0; i < _board.Branches.Count; i++)
                {
                    var st = _board.Branches[i];
                    if (st.Broken || st.AdLocked || st.Count >= BranchState.Cap) continue;
                    var col = (BirdColor)((st.Count + i) % Palette.Shipped);
                    while (st.Count < BranchState.Cap)
                    {
                        st.Birds.Add(new Bird(col, BirdSex.Neutral));
                        col = (BirdColor)(((int)col + 1) % Palette.Shipped);
                    }
                    st.AlignShroud();
                    SyncAll();
                    return;
                }
                return;
            }
            var br = _board.Branches[pick];
            var tip = br.Birds[0];
            br.Birds.Add(tip);
            br.AlignShroud();
            SyncAll();
        }

        IEnumerator ShotPokerStamp()
        {
            const string dir = "/tmp/paradice";
            System.IO.Directory.CreateDirectory(dir);
            _home = HomeFace.Poker;
            _splash = true;
            _pokerPayOpen = false;
            _pokerPayAnim = 0f;
            BirdPoker.Boot();
            Purse.Boot();
            if (Purse.Coins < 80) Purse.Credit(80 - Purse.Coins);
            BirdPoker.ResetRound();
            BirdPoker.ForceFiveWilds();
            int kind = BirdPoker.LastPunchKind;
            if (kind < 0) kind = BirdPoker.WildKind;
            BeginPokerStamp(kind);
            yield return new WaitForSecondsRealtime(0.82f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(dir + "/poker-stamp-drop.png");
            yield return new WaitForSecondsRealtime(0.28f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(dir + "/poker-stamp-hit.png");
            yield return new WaitForSecondsRealtime(0.42f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(dir + "/poker-stamp-mark.png");
            System.IO.File.WriteAllText("/tmp/paradice/poker-stamp-done", "1");
        }

        IEnumerator PokerPlayrun()
        {
            const string dir = "/tmp/paradice";
            const string logPath = "/tmp/flock-five-poker-run.log";
            System.IO.Directory.CreateDirectory(dir);
            var log = new System.Text.StringBuilder();
            void Line(string t)
            {
                log.AppendLine(t);
                System.IO.File.WriteAllText(logPath, log.ToString());
            }
            Line("poker playrun start");
            _home = HomeFace.Poker;
            BirdPoker.Boot();
            Purse.Boot();
            PigPoke.Boot();
            if (Purse.Coins < 40) Purse.Credit(40 - Purse.Coins);
            Line("coins " + Purse.Coins + " bet " + BirdPoker.Bet);
            yield return new WaitForSecondsRealtime(0.5f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(dir + "/poker-idle.png");
            yield return new WaitForSecondsRealtime(0.35f);

            for (int round = 1; round <= 3; round++)
            {
                Line("--- hand " + round + " ---");
                if (BirdPoker.PhaseNow == BirdPoker.Phase.Drawn)
                    BirdPoker.Collect();
                if (BirdPoker.PhaseNow != BirdPoker.Phase.Idle)
                    BirdPoker.ResetRound();
                int before = Purse.Coins;
                if (!BirdPoker.Deal())
                {
                    Line("FAIL deal hand " + round + " coins " + Purse.Coins);
                    break;
                }
                BeginPokerDeal();
                Line("dealt " + DescribeHand(BirdPoker.Hand) + " spent " + (before - Purse.Coins));
                yield return new WaitForSecondsRealtime(1.05f);
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(dir + "/poker-hand" + round + "-deal.png");
                yield return new WaitForSecondsRealtime(0.25f);

                ApplyPokerHold(0);
                ApplyPokerHold(2);
                Line("hold 0 and 2");
                yield return new WaitForSecondsRealtime(0.2f);

                for (int i = 0; i < BirdPoker.HandSize; i++)
                {
                    _pokerRedraw[i] = !BirdPoker.Hold[i];
                    _pokerPrev[i] = BirdPoker.Hand[i];
                }
                BirdPoker.Draw();
                BeginPokerDraw();
                Line("drawn " + DescribeHand(BirdPoker.Hand) + " rank " + BirdPoker.RankLabel(BirdPoker.LastRank) + " win " + BirdPoker.LastWin + " coins " + Purse.Coins);
                yield return new WaitForSecondsRealtime(1.15f);
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(dir + "/poker-hand" + round + "-draw.png");
                yield return new WaitForSecondsRealtime(0.3f);
                BirdPoker.Collect();
                Line("collect idle coins " + Purse.Coins);
            }
            Line("poker playrun DONE");
            _home = HomeFace.Splash;
        }

        static string DescribeHand(BirdPoker.Card[] hand)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < hand.Length; i++)
            {
                if (i > 0) sb.Append(',');
                if (hand[i].Wild) sb.Append("WILD");
                else sb.Append(hand[i].Color).Append(hand[i].Sex);
            }
            return sb.ToString();
        }

        void Shot(string file)
        {
            var cam = _garden.Cam;
            if (cam == null) return;
            int w = 1080;
            int h = 1920;
            var rt = new RenderTexture(w, h, 24);
            var prev = cam.targetTexture;
            var prevRect = cam.rect;
            var prevAspect = cam.aspect;
            cam.rect = new Rect(0f, 0f, 1f, 1f);
            cam.aspect = 9f / 16f;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            cam.targetTexture = prev;
            cam.rect = prevRect;
            cam.aspect = prevAspect;
            RenderTexture.active = null;
            Destroy(rt);
            System.IO.Directory.CreateDirectory("/tmp/paradice");
            System.IO.File.WriteAllBytes("/tmp/paradice/" + file, tex.EncodeToPNG());
            Destroy(tex);
            Debug.Log("Wrote /tmp/paradice/" + file + " " + w + "x" + h);
        }
#endif

        void SyncAll(bool force = false)
        {
            // In-flight hops and collects must not snap birds back onto perches
            // while a restart takeoff still owns them.
            if (_restarting && !force) return;
            // The limb in the scrap stays locked, so its fight sprites are not
            // pulled home. Every other limb tracks the board. No seat is held
            // for the flock until ResolveDisplaced.
            for (int i = 0; i < _garden.Branches.Length; i++)
            {
                if (_locked.Contains(i)) continue;
                if (_board == null || i >= _board.Branches.Count) continue;
                var branchView = _garden.Branches[i];
                // Limb break is already playing; Sync would hide it mid-fall.
                if (branchView == null || branchView.Breaking) continue;
                branchView.Sync(_board.Branches[i], _board.IsSleeping(i));
                branchView.SetReady(i == _sel);
            }
            for (int i = 0; i < 2; i++)
                _garden.Feeders[i].Show(_board.Live[i], !_restarting);
        }

        bool Locked(int i) => _locked.Contains(i);

        void Lock(int i) => _locked.Add(i);

        void Unlock(int i) => _locked.Remove(i);

        int NextCombo()
        {
            if (Time.unscaledTime > _comboUntil)
            {
                _combo = 0;
                SfxLibrary.CloseCombo();
                SfxLibrary.OpenCombo();
            }
            else
                SfxLibrary.SealComboStart();
            _combo = Mathf.Min(_combo + 1, Palette.ComboMax);
            _comboUntil = Time.unscaledTime + 4.5f;
            return _combo;
        }

        void ClearComboShows()
        {
            _comboPopGen++;
            _comboShows.Clear();
            _comboPopAt = -99f;
            _comboPopPump = false;
        }

        // Schedules the wordmark. The caller fires haptics after this so a buzz
        // failure cannot drop the step. The 2s window spaces lettering only.
        void QueueComboPop(int combo, bool celebrate = false)
        {
            if (combo < 2 || _finaleHold || _won) return;
            _comboShows.Enqueue(new ComboCue { Combo = combo, Celebrate = celebrate });
            if (_comboPopPump) return;
            _comboPopPump = true;
            StartCoroutine(PumpComboPops(_comboPopGen));
        }

        // True when this collect empties the last live branch.
        bool StageClearingCollect(int branch)
        {
            if (_board == null) return false;
            var list = _board.Branches;
            for (int i = 0; i < list.Count; i++)
            {
                if (i == branch) continue;
                var b = list[i];
                if (!b.Broken && b.Count > 0) return false;
            }
            return true;
        }

        // Sparrow leaves on this collect. Hawk leaves only on the second scrap.
        static bool PestWillLeave(bool vsHawk, bool vsSparrow)
        {
            if (vsSparrow) return SparrowView.Live != null;
            if (!vsHawk) return false;
            var hawk = HawkView.Live;
            return hawk != null && hawk.HitsTaken + 1 >= HawkView.HitsNeeded;
        }

        IEnumerator PumpComboPops(int gen)
        {
            while (gen == _comboPopGen && _comboShows.Count > 0)
            {
                while (gen == _comboPopGen && (GamePause.Paused || Time.unscaledTime < _comboPopAt))
                {
                    if (GamePause.Paused && _comboPopAt > -90f)
                        _comboPopAt += Time.unscaledDeltaTime;
                    yield return null;
                }
                if (gen != _comboPopGen || _comboShows.Count == 0) break;
                var show = _comboShows.Dequeue();
                _comboPopAt = Time.unscaledTime + ComboPopGap / _autoResolveScale;
                try { PlayComboShow(show.Combo, show.Celebrate); }
                catch (System.Exception) { }
            }
            if (gen == _comboPopGen)
                _comboPopPump = false;
        }

        void PlayComboShow(int combo, bool celebrate = false)
        {
            var root = _garden.Root;
            if (root != null)
                StartCoroutine(PlayComboPop(root, combo, celebrate));
            Sfx.Combo(combo, celebrate ? 0.96f : 0.76f);
            if (celebrate) Haptics.Play(Haptics.Tier.Strong);
            CamShake.Combo(celebrate ? Mathf.Max(combo, 6) : combo);
            if (root == null) return;
            if (celebrate) FinaleShow.BeginNightShow(root, this);
            StartCoroutine(Wow.FlockOver(root, combo));
            int bursts = Mathf.Clamp(combo, 2, Palette.ComboMax);
            if (celebrate) bursts = Mathf.Min(Palette.ComboMax, bursts + 6);
            for (int i = 0; i < bursts; i++)
            {
                var p = new Vector3(Random.Range(-4.4f, 4.4f), Random.Range(2.5f, 6.8f), 0f);
                StartCoroutine(Wow.SkyBurst(root, p, (BirdColor)(i % Palette.Max), i < 3 || celebrate, i * 0.08f));
            }
        }

        void OnApplicationPause(bool paused)
        {
            // Back to 1 in the background; SyncAutoResolveSpeed re-arms it from live state.
            if (paused) SetAutoResolveSpeed(1f);
            if (paused) NoteAppBackground();
            else
            {
                NoteAppResume();
                ResumeDailyReminder();
            }
        }

        void OnApplicationFocus(bool focus)
        {
            if (focus) NoteAppResume();
            else NoteAppBackground();
        }

        void Update()
        {
            MaybeSmokeLaunch();
            PollHomeAtt();
            // Bonus signs fade on the shared stage-clear state (limb stays; see GiftWant).
            GiftWant.StageClear = _won && !_splash;
            // Before the pause return and the speed sync: a solved board sends
            // a perched pest off so the freeze and the finale are not held.
            ReleaseSolvedPests();
            // Before the pause return: the base scale is set (or handed to GamePause) every frame.
            SyncAutoResolveSpeed();
            // Chain ended. A collect still in flight keeps the opening sting slot.
            if (!_collecting && Time.unscaledTime > _comboUntil)
                SfxLibrary.CloseCombo();
            // Interrupted-tutorial check: cheap, and it only acts on a stuck state.
            if ((Time.frameCount & 7) == 0) HealInterruptedTutorials();
            if (GamePause.Paused)
            {
                if (TutorialGuideLive() || _tutorPause != 0)
                    TickPausedTutorial();
                return;
            }
            TickUsaPlane(Time.unscaledDeltaTime);
            CoachAdvance();
            TickPopups();
            TickDailyBonus();
            if (_splash) return;
            if (_restartAsk != RestartAsk.None) return;
            // A break packs the column before the collect coroutine reaches its tween.
            // Start the shared ease here. SnapIfGapped no longer fits.
            if (!_restarting && _board != null && _garden.Branches != null
                && GardenFit.SnapIfGapped(_garden, _board))
                StartCoroutine(GardenFit.Tween(_garden, _board, false));
            if (_gift != GiftFace.None || _frozen)
            {
                if (Pressed(out var tap))
                {
                    if (EatGardenLessonTap(tap)) return;
                    if (LessonTapBlocked()) return;
                    if (HitHud(tap)) return;
                    if (_frozen && !_iceCoating && _gift == GiftFace.None) OpenGift();
                }
                return;
            }
            if (!_won && !_restarting)
                TryScoreGarden();
            // Polled every unfrozen frame, so a check skipped under a tutorial freeze, a pest
            // scrap or a collect is never consumed: it runs the first frame the garden is idle.
            TrySolvedFinish();
            if (!_busy && !_won && SkyCycle.Courtesy != null)
                SkyCycle.Courtesy = null;
#if UNITY_EDITOR
            if (WantFinalePreview() && !_busy)
            {
                StartCoroutine(PreviewFinale());
                return;
            }
#endif
            // Desert welcome only. Birds stay put; restart still works. Other seasons do not hold.
            if (SeasonReveal.HoldsPlay)
            {
                if (!Pressed(out var heldTap)) return;
                if (EatGardenLessonTap(heldTap)) return;
                if (LessonTapBlocked()) return;
                if (HitHud(heldTap)) return;
                return;
            }
            if (!Pressed(out var screen)) return;
            // Informational leaf and pest lessons eat the press before a stamp,
            // the hud, or a branch move. Restart is exempt inside the helper.
            // The dismiss also arms the short guard, so this same tap cannot
            // fall through onto a gift branch.
            if (EatGardenLessonTap(screen)) return;
            if (LessonTapBlocked()) return;
            if (GardenStampHit(screen))
            {
                _stampPulse = Time.unscaledTime;
                return;
            }
            if (HitHud(screen)) return;
            var cam = _garden.Cam != null ? _garden.Cam : Camera.main;
            if (cam == null) return;
            HandleTap(cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0f)));
        }

        public void ShotNow(string file)
        {
#if UNITY_EDITOR
            Shot(file);
#endif
        }

        void HandleTap(Vector2 world)
        {
            if (LessonTapBlocked()) return;
            if (_busy || _won || _board == null) return;
            if (PlayClock.Now < _swallowTapsUntil) return;
            if (Time.unscaledTime < _nextTap) return;
            _nextTap = Time.unscaledTime + 0.10f;
            if (CoachReject(world)) return;

            int sign = HitGiftSign(world);
            if (sign >= 0)
            {
                if (CanOfferBonus(sign)) OpenBonus(sign);
                return;
            }
            // Bird and limb taps must not open a gift a moment later.
            _suppressGiftUntil = PlayClock.Now + 1f;

            int feeder = HitFeeder(world);
            if (feeder >= 0)
            {
                if (_sel >= 0)
                {
                    _garden.Branches[_sel].SetReady(false);
                    _sel = -1;
                }
                _garden.Feeders[feeder].Poke();
                return;
            }

            int hit = HitBranch(world);
            if (hit < 0)
            {
                if (_sel >= 0)
                {
                    _garden.Branches[_sel].SetReady(false);
                    _sel = -1;
                }
                return;
            }
            if (Locked(hit) && _sel < 0)
            {
                _garden.Branches[hit].Shake();
                return;
            }
            if (_sel < 0)
            {
                if (!_board.CanPick(hit))
                {
                    _garden.Branches[hit].Shake();
                    if (_board.IsSleeping(hit)) Sfx.Sleep();
                    return;
                }
                Select(hit);
                return;
            }
            if (hit == _sel)
            {
                _garden.Branches[_sel].SetReady(false);
                _sel = -1;
                return;
            }
            if (Locked(_sel) || Locked(hit))
            {
                _garden.Branches[hit].Shake();
                return;
            }
            if (_board.CanMove(_sel, hit, out _))
            {
                StartCoroutine(DoMove(_sel, hit));
                return;
            }
            if (!_board.Branches[hit].Empty)
            {
                if (!_board.CanPick(hit))
                {
                    _garden.Branches[hit].Shake();
                    if (_board.IsSleeping(hit)) Sfx.Sleep();
                    return;
                }
                Select(hit);
                return;
            }
            Sfx.Deny();
            _garden.Branches[hit].Shake();
        }

        void Select(int hit)
        {
            if (_sel >= 0 && _sel != hit) _garden.Branches[_sel].SetReady(false);
            _sel = hit;
            _garden.Branches[hit].SetReady(true);
            if (_board.Branches[hit].Tip.HasValue)
                Sfx.Chirp(_board.Branches[hit].Tip.Value.Color);
        }

        IEnumerator DoMove(int from, int to)
        {
            int gen = _motionGen;
            _garden.Branches[from].SetReady(false);
            _sel = -1;
            if (Locked(from) || Locked(to) || !_board.CanMove(from, to, out int run))
            {
                Sfx.Deny();
                _garden.Branches[to].Shake();
                yield break;
            }

            int fromCount = _board.Branches[from].Count;
            int toCount = _board.Branches[to].Count;
            int wanted = _board.Branches[from].TipRun();
            var hopCol = _board.Branches[from].Tip.Value.Color;
            Lock(from);
            Lock(to);
            ArmPests();
            _board.TryMove(from, to, out run);
            yield return Hop(from, to, run, fromCount, toCount, hopCol);
            if (gen != _motionGen) yield break;
            Unlock(from);
            Unlock(to);
            SyncAll();

            int kicked = KickCollects();
            SyncAll();
            CoachMoved(from, to, kicked);
            if (_locked.Count == 0)
                yield return GardenFit.Tween(_garden, _board, false);
            if (gen != _motionGen) yield break;
            if (_board.JustUnveiled)
            {
                bool firstBee = !Hive.Collected;
                var visit = Hive.TakeVisitor();
                _levelBees.Add(visit);
                NoteHiveCue(visit);
                Sfx.BeeFound();
                if (firstBee)
                {
                    _hivePopping = true;
                    _hivePop = 0f;
                    _hivePopMul = 0.35f;
                    SyncOrbitHive();
                }
                if (visit.Fresh) ArmIncomingHalo(visit);
                if (_garden.Hive != null)
                {
                    SnapHiveToHud();
                    StartCoroutine(_garden.Hive.Welcome(visit, _garden.Branches[from].transform.position + Vector3.up * 0.7f));
                }
                _garden.Branches[from].FlutterTip();
            }
            if (kicked == 0 && _board.IsSleeping(to) && _board.Branches[to].IsFullMatch(out _))
                Sfx.Sleep();
            if (kicked == 0)
                CheckOver();
        }

        int KickCollects()
        {
            if (_collecting) return 0;
            int collect = _board.FindCollect();
            if (collect < 0 || Locked(collect)) return 0;
            StartCoroutine(Collect(collect, NextCombo()));
            return 1;
        }

        IEnumerator Hop(int from, int to, int run, int fromCount, int toCount, BirdColor col)
        {
            int gen = _motionGen;
            var src = _garden.Branches[from];
            var dst = _garden.Branches[to];
            Sfx.FlockFlutter(run);
            var movers = new SpriteRenderer[run];
            var starts = new Vector3[run];
            var ends = new Vector3[run];
            for (int i = 0; i < run; i++)
            {
                int oldSeat = fromCount - run + i;
                int newSeat = toCount + i;
                movers[i] = src.Birds[oldSeat];
                var idle = movers[i].GetComponent<BirdIdle>();
                if (idle != null)
                {
                    idle.Frozen = true;
                    idle.Flapping = true;
                    idle.Lift = 0f;
                }
                starts[i] = movers[i].transform.position;
                ends[i] = dst.SeatWorld(newSeat) + dst.transform.TransformVector(new Vector3(0f, BranchView.RestLift, 0f));
                movers[i].transform.SetParent(_garden.Root, true);
                movers[i].sortingOrder = FlockSort.Lift;
            }
            float t = 0f;
            const float dur = 0.48f;
            while (t < dur)
            {
                if (gen != _motionGen) yield break;
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, t / dur);
                for (int i = 0; i < run; i++)
                {
                    var p = Vector3.Lerp(starts[i], ends[i], u);
                    p.y += Mathf.Sin(u * Mathf.PI) * 2.45f;
                    movers[i].transform.position = p;
                }
                yield return null;
            }

        }

        IEnumerator Collect(int branch, int combo = 1)
        {
            int gen = _motionGen;
            var br = _board.Branches[branch];
            br.IsFullMatch(out var col);
            int slot = _board.FeederSlotFor(col);
            var view = _garden.Branches[branch];
            var feeder = slot >= 0 ? _garden.Feeders[slot] : null;
            Vector3 mouth = feeder != null ? feeder.Mouth : view.transform.position + Vector3.up * 3f;
            if (_sel == branch)
            {
                _garden.Branches[branch].SetReady(false);
                _sel = -1;
            }
            bool vsHawk = HawkView.Live != null && HawkView.Live.BlockingSlot == slot;
            bool vsSparrow = !vsHawk && SparrowView.Live != null && SparrowView.Live.BlockingSlot == slot;
            bool pest = vsHawk || vsSparrow;
            if (pest && _pestResolving)
            {
                AdLog.Add("pest sequence ignored (already resolving)");
                yield break;
            }
            if (pest)
            {
                _pestResolving = true;
                _pestGuardNoted = false;
            }
            BeginCollect();
            PestSchedule.NoteCollect();
            Lock(branch);
            if (combo >= 2)
            {
                bool clearing = StageClearingCollect(branch);
                QueueComboPop(combo, clearing);
                Haptics.OnCombo(clearing ? Mathf.Max(combo, 5) : combo);
            }
            else
                Haptics.OnCombo(0);

            var birds = new SpriteRenderer[BranchState.Cap];
            int n = 0;
            for (int i = 0; i < BranchState.Cap; i++)
            {
                var bird = view.Birds[i];
                if (bird == null || !bird.enabled) continue;
                var idle = bird.GetComponent<BirdIdle>();
                if (idle != null)
                {
                    idle.Sleeping = false;
                    idle.Frozen = true;
                    idle.Flapping = true;
                }
                bird.transform.SetParent(_garden.Root, true);
                bird.sortingOrder = FlockSort.Fly;
                birds[n++] = bird;
            }
            var flock = new Bird[n];
            for (int i = 0; i < n; i++)
            {
                var idle = birds[i] != null ? birds[i].GetComponent<BirdIdle>() : null;
                flock[i] = idle != null ? idle.AsBird() : new Bird(col, BirdSex.Neutral);
            }
            // Feeder matches score when the last bird lands. A pest scrap lifts
            // the flock now and does not pick a limb until the fight is over.
            // Hawk never drops the limb; a sparrow drops it only when every bird
            // still has a seat.
            bool scoreClear = pest && PestWillLeave(vsHawk, vsSparrow) && StageClearingCollect(branch);
            // A solved garden does not wait out the scrap. Dismiss the pest and score.
            if (pest && scoreClear)
            {
                StopPests();
                _scoreImmediate = true;
                yield return AfterPestLeaves(true, true, birds, n, feeder, view, slot, branch);
                _scoreImmediate = false;
            }
            else if (pest)
                PestPark.Hold(_board, branch);

            float haste = Mathf.Lerp(1f, 0.52f, Mathf.Clamp01((combo - 1) / 7f));
            float step = 0.192f * haste;
            float fly = 0.432f * haste;
            // Pest scraps stay at full tempo — combo haste made the fight unreadable.
            float fightHaste = (vsHawk || vsSparrow) ? 1f : haste;
            float fightFly = (vsHawk || vsSparrow) ? 0.58f : fly;
            if (pest && scoreClear)
            {
                // Fight skipped. The clear already landed in AfterPestLeaves.
            }
            else if (vsHawk)
                yield return CollectVsHawk(birds, n, feeder, mouth, view, col, fightHaste, step, fightFly, combo, flock, branch, scoreClear, slot);
            else if (vsSparrow)
                yield return CollectVsSparrow(birds, n, feeder, mouth, view, col, fightHaste, step, fightFly, combo, flock, branch, scoreClear, slot);
            else
            {
                const float beat = 0.12f;
                const float stagger = 0.05f;
                const float flyDur = 0.45f;
                var order = new int[n];
                for (int i = 0; i < n; i++) order[i] = i;
                for (int a = 0; a < n; a++)
                {
                    int best = a;
                    float bx = birds[order[a]] != null ? birds[order[a]].transform.position.x : 0f;
                    for (int b = a + 1; b < n; b++)
                    {
                        float x = birds[order[b]] != null ? birds[order[b]].transform.position.x : 0f;
                        if (x < bx)
                        {
                            best = b;
                            bx = x;
                        }
                    }
                    int swap = order[a];
                    order[a] = order[best];
                    order[best] = swap;
                }

                var home = new Vector3[n];
                var homeRot = new Quaternion[n];
                var homeScale = new Vector3[n];
                for (int k = 0; k < n; k++)
                {
                    int i = order[k];
                    if (birds[i] == null) continue;
                    home[i] = birds[i].transform.position;
                    homeRot[i] = birds[i].transform.rotation;
                    homeScale[i] = birds[i].transform.localScale;
                    var idle = birds[i].GetComponent<BirdIdle>();
                    if (idle != null) idle.Cheer(beat + stagger * k + 0.08f);
                }
                Sfx.Chirp(col);
                float bt = 0f;
                while (bt < beat)
                {
                    if (gen != _motionGen) yield break;
                    bt += Time.deltaTime;
                    float u = Mathf.SmoothStep(0f, 1f, bt / beat);
                    for (int i = 0; i < n; i++)
                    {
                        if (birds[i] == null) continue;
                        var p = home[i];
                        p.y += 0.18f * u;
                        birds[i].transform.position = p;
                        birds[i].transform.rotation = Quaternion.Slerp(homeRot[i], Quaternion.identity, u);
                        birds[i].transform.localScale = homeScale[i] * (1f + 0.06f * u);
                    }
                    yield return null;
                }
                if (gen != _motionGen) yield break;

                if (feeder != null) feeder.BeginScore();
                var landed = new int[1];
                int expect = 0;
                Vector3 aim = feeder != null ? feeder.RestMouth : mouth;
                for (int k = 0; k < n; k++)
                {
                    int i = order[k];
                    if (birds[i] == null) continue;
                    expect++;
                    float spread = n <= 1 ? 0f : k / (float)(n - 1) - 0.5f;
                    var land = aim + new Vector3(spread * 0.62f, 0.035f * ((k & 1) == 0 ? 1f : -1f), 0f);
                    birds[i].sortingOrder = FlockSort.Flight(k);
                    StartCoroutine(FlyHit(birds[i].transform, land, stagger * k, flyDur, k, feeder, landed));
                }
                while (landed[0] < expect)
                {
                    if (gen != _motionGen) yield break;
                    yield return null;
                }
                if (gen != _motionGen) yield break;

                BirdColor? liveBefore = null;
                if (slot >= 0 && _board.Live != null && (uint)slot < (uint)_board.Live.Length)
                    liveBefore = _board.Live[slot];
                NoteFlockLeft(br);
                _board.ApplyCollect(branch, scoreFeeder: true);
                _wakeBranch = WakeBranchFor(slot, liveBefore, branch);
                StartCoroutine(view.BreakAway());
                // Birds are already off the perch. Hold the next row until this feeder
                // has left and the next one has landed, so the two motions do not overlap.
                if (feeder != null)
                    yield return RetireFeeder(feeder, slot, gen);
            }
            if (gen != _motionGen)
            {
                EndCollect();
                Unlock(branch);
                if (pest) EndPest();
                _wakeBranch = -1;
                yield break;
            }
            bool pestScored = _pestClearScored;
            if (pest && !pestScored)
                yield return FinishPest(view, birds, n, branch, vsHawk, gen);
            if (gen != _motionGen)
            {
                EndCollect();
                Unlock(branch);
                if (pest) EndPest();
                _wakeBranch = -1;
                yield break;
            }
            if (_wakeBranch >= 0 && !_gardenScoring)
                yield return WakeRow(_wakeBranch, gen);
            _wakeBranch = -1;
            if (gen != _motionGen)
            {
                EndCollect();
                Unlock(branch);
                yield break;
            }
            Unlock(branch);
            EndCollect();
            if (pest) EndPest();
            if (!_gardenScoring) SyncAll();
            int more = 0;
            if (!_gardenScoring) more = KickCollects();
            // Survivors ease onto the packed column, including after a pest clear.
            if (!_gardenScoring && _locked.Count == 0)
                yield return GardenFit.Tween(_garden, _board, false);
            if (gen != _motionGen || more != 0) yield break;
            if (TryScoreGarden())
                yield return WaitGardenScore();
            if (gen != _motionGen) yield break;
            CheckOver();
        }

        // Collect into a sparrow-blocked feeder: dive arcs, five dive-strikes, zigzag flee, scatter.
        IEnumerator CollectVsSparrow(
            SpriteRenderer[] birds, int n, FeederView feeder, Vector3 mouth,
            BranchView view, BirdColor col, float haste, float step, float fly, int combo,
            Bird[] flock, int broken, bool scoreClear, int slot)
        {
            int gen = _motionGen;
            if (SparrowView.Live != null)
                SparrowView.Live.BeginEvict();
            if (feeder != null) feeder.Hold();

            Vector3 Body() =>
                SparrowView.Live != null
                    ? SparrowView.Live.transform.position
                    : mouth + new Vector3(0f, 0.35f, 0f);

            int hits = Mathf.Min(5, Mathf.Max(1, n));
            // Staggered dive approaches — not a neat landing line.
            float approachSpan = 0f;
            for (int i = 0; i < n; i++)
            {
                if (birds[i] == null) continue;
                float delay = i == 0 ? 0f : approachSpan + Random.Range(0.04f, 0.11f);
                approachSpan = delay;
                float bank = (i % 2 == 0 ? 1f : -1f) * Random.Range(0.55f, 1.05f);
                var hold = Body() + new Vector3(
                    bank * Random.Range(0.55f, 0.95f),
                    Random.Range(0.55f, 1.05f),
                    0f);
                StartCoroutine(DiveApproach(birds[i].transform, hold, delay, fly * Random.Range(0.78f, 1.05f), i));
            }
            yield return new WaitForSeconds(approachSpan + fly * 0.85f);
            if (gen != _motionGen) yield break;
            if (n > 0)
                StartCoroutine(Wow.Burst(Body() + Vector3.up * 0.15f, col, _garden.Root, combo));

            // Five dive-strikes; irregular gaps between contacts; strikers loop back for more.
            for (int h = 0; h < hits; h++)
            {
                if (h > 0)
                    yield return new WaitForSeconds(Random.Range(0.12f, 0.22f) * haste);
                if (gen != _motionGen) yield break;
                int striker = h % Mathf.Max(1, n);
                if (birds[striker] == null) continue;
                yield return DiveStrike(birds[striker].transform, Body, h);
                if (gen != _motionGen) yield break;
            }

            bool left = SparrowView.Live != null;
            if (left)
                yield return SparrowView.Live.PanicFlee(true);
            if (gen != _motionGen) yield break;
            yield return AfterPestLeaves(left, scoreClear, birds, n, feeder, view, slot, broken);
        }

        // Collect into a hawk-blocked feeder. Same dive scrap as sparrow, but two
        // full collects to clear: first wounds (hawk stays perched), second flees.
        IEnumerator CollectVsHawk(
            SpriteRenderer[] birds, int n, FeederView feeder, Vector3 mouth,
            BranchView view, BirdColor col, float haste, float step, float fly, int combo,
            Bird[] flock, int broken, bool scoreClear, int slot)
        {
            int gen = _motionGen;
            var hawk = HawkView.Live;
            if (hawk != null) hawk.BeginScrap();
            if (feeder != null) feeder.Hold();

            Vector3 Body() =>
                HawkView.Live != null
                    ? HawkView.Live.transform.position
                    : mouth + new Vector3(0f, 0.42f, 0f);

            int hits = Mathf.Min(5, Mathf.Max(1, n));
            float approachSpan = 0f;
            for (int i = 0; i < n; i++)
            {
                if (birds[i] == null) continue;
                float delay = i == 0 ? 0f : approachSpan + Random.Range(0.04f, 0.11f);
                approachSpan = delay;
                float bank = (i % 2 == 0 ? 1f : -1f) * Random.Range(0.55f, 1.05f);
                var hold = Body() + new Vector3(
                    bank * Random.Range(0.55f, 0.95f),
                    Random.Range(0.55f, 1.05f),
                    0f);
                StartCoroutine(DiveApproach(birds[i].transform, hold, delay, fly * Random.Range(0.78f, 1.05f), i));
            }
            yield return new WaitForSeconds(approachSpan + fly * 0.85f);
            if (gen != _motionGen) yield break;
            if (n > 0)
                StartCoroutine(Wow.Burst(Body() + Vector3.up * 0.15f, col, _garden.Root, combo));

            for (int h = 0; h < hits; h++)
            {
                if (h > 0)
                    yield return new WaitForSeconds(Random.Range(0.12f, 0.22f) * haste);
                if (gen != _motionGen) yield break;
                int striker = h % Mathf.Max(1, n);
                if (birds[striker] == null) continue;
                yield return DiveStrike(birds[striker].transform, Body, h);
                if (gen != _motionGen) yield break;
            }

            if (gen != _motionGen) yield break;
            bool left = hawk != null && hawk.AbsorbCollect();
            if (left && HawkView.Live != null)
                yield return HawkView.Live.PanicFlee(true);
            else if (hawk != null)
                hawk.EndScrap();
            if (gen != _motionGen) yield break;
            yield return AfterPestLeaves(left, scoreClear, birds, n, feeder, view, slot, broken);
        }

        IEnumerator DiveApproach(Transform tr, Vector3 hold, float delay, float dur, int pop)
        {
            int gen = _motionGen;
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (gen != _motionGen || tr == null) yield break;
            Sfx.FlockFlutter(1);
            var start = tr.position;
            var scale = tr.localScale;
            // Slight overshoot past hold, then settle.
            var delta = hold - start;
            var dir = delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector3.up;
            var over = hold + dir * Random.Range(0.18f, 0.42f);
            over.y += Random.Range(0.05f, 0.2f);
            FaceToward(tr, hold.x >= start.x);
            float lift = Random.Range(1.2f, 1.9f);
            var idle = tr.GetComponent<BirdIdle>();
            float t = 0f;
            while (t < dur)
            {
                if (gen != _motionGen || tr == null) yield break;
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                // Accelerate into the dive (ease-in), soft land.
                float dive = u * u * (1.6f - 0.6f * u);
                var mid = Vector3.Lerp(start, over, Mathf.Clamp01(dive));
                mid.y += Mathf.Sin(Mathf.Clamp01(dive) * Mathf.PI) * lift;
                // Bank flip mid-arc.
                if (u > 0.45f && u < 0.55f)
                    FaceToward(tr, hold.x < start.x);
                tr.position = mid;
                tr.localScale = scale;
                if (idle != null) idle.Flapping = true;
                yield return null;
            }
            // Ease back from overshoot to hold.
            if (gen != _motionGen || tr == null) yield break;
            var from = tr.position;
            float back = 0f;
            const float backDur = 0.10f;
            while (back < backDur)
            {
                if (gen != _motionGen || tr == null) yield break;
                back += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, back / backDur);
                tr.position = Vector3.Lerp(from, hold, u);
                yield return null;
            }
            if (tr != null) tr.position = hold;
            Sfx.ScorePop(pop);
        }

        IEnumerator DiveStrike(Transform tr, System.Func<Vector3> bodyFn, int hit)
        {
            int gen = _motionGen;
            if (tr == null) yield break;
            Sfx.FlockFlutter(1);
            var start = tr.position;
            var scale = tr.localScale;
            var body = bodyFn();
            // Aim at body with slight overshoot past the sparrow.
            var aim = body + new Vector3(Random.Range(-0.08f, 0.08f), Random.Range(-0.02f, 0.1f), 0f);
            var delta = aim - start;
            var dir = delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector3.right;
            var over = aim + dir * Random.Range(0.35f, 0.65f);
            FaceToward(tr, aim.x >= start.x);

            float dur = Random.Range(0.14f, 0.22f);
            float t = 0f;
            bool struck = false;
            var idle = tr.GetComponent<BirdIdle>();
            while (t < dur)
            {
                if (gen != _motionGen || tr == null) yield break;
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                // Accelerate hard into contact.
                float dive = u * u;
                tr.position = Vector3.Lerp(start, over, dive);
                tr.localScale = scale * (u < 0.7f ? 1f : Mathf.Lerp(1f, 0.92f, (u - 0.7f) / 0.3f));
                if (!struck && u >= 0.55f)
                {
                    struck = true;
                    // Hawk scrap keeps IsBlocking true through dive hits; sparrow fight
                    // clears BlockingSlot via BeginEvict so prefer Live sparrow only when
                    // no hawk is blocking.
                    if (HawkView.Live != null && HawkView.Live.IsBlocking)
                        HawkView.Live.TakeHit(hit);
                    else if (SparrowView.Live != null)
                        SparrowView.Live.TakeHit(hit);
                    // Brief contact squash on the hummer.
                    tr.localScale = scale * 1.12f;
                }
                if (idle != null) idle.Flapping = true;
                yield return null;
            }

            // Glance-off tight loop, then hover nearby for a possible return pass.
            if (gen != _motionGen || tr == null) yield break;
            var loopFrom = tr.position;
            float side = loopFrom.x >= body.x ? 1f : -1f;
            FaceToward(tr, side < 0f);
            float loop = 0f;
            float loopDur = Random.Range(0.16f, 0.24f);
            float rad = Random.Range(0.28f, 0.48f);
            var hover = body + new Vector3(
                side * Random.Range(0.7f, 1.15f),
                Random.Range(0.45f, 0.95f),
                0f);
            while (loop < loopDur)
            {
                if (gen != _motionGen || tr == null) yield break;
                loop += Time.deltaTime;
                float u = Mathf.Clamp01(loop / loopDur);
                float ang = u * Mathf.PI * 1.35f;
                var arc = loopFrom + new Vector3(
                    side * Mathf.Sin(ang) * rad,
                    Mathf.Cos(ang * 0.85f) * rad * 0.65f + u * 0.2f,
                    0f);
                tr.position = Vector3.Lerp(arc, hover, u * u);
                tr.localScale = scale;
                if (u > 0.5f) FaceToward(tr, hover.x >= tr.position.x);
                yield return null;
            }
            if (tr != null) tr.position = hover;
        }

        static void FaceToward(Transform tr, bool faceRight)
        {
            if (tr == null) return;
            var idle = tr.GetComponent<BirdIdle>();
            if (idle != null)
            {
                idle.FaceLeft = !faceRight;
                idle.Flapping = true;
            }
            var sr = tr.GetComponent<SpriteRenderer>();
            if (sr != null) sr.flipX = !faceRight;
        }

        IEnumerator ScatterBirds(SpriteRenderer[] birds, int n)
        {
            if (n <= 0) yield break;
            Sfx.Takeoff(n);
            Sfx.FlockFlutter(Mathf.Min(3, Mathf.Max(1, n)));
            var dests = ScatterDests(n);
            for (int i = 0; i < n; i++)
            {
                if (birds[i] == null) continue;
                var dest = dests[i % dests.Length];
                // Messy flock stagger — irregular, not a metronome.
                float delay = i * Random.Range(0.02f, 0.055f) + Random.Range(0f, 0.04f);
                StartCoroutine(ScatterOne(birds[i].transform, dest, delay));
            }
            yield return new WaitForSeconds(0.58f + n * 0.04f);
        }

        // Panic lift after a pest scrap — stay visible until the broken limb is gone.
        IEnumerator ScatterUp(SpriteRenderer[] birds, int n)
        {
            if (_scatterBusy)
            {
                AdLog.Add("scatter ignored (already scattering)");
                yield break;
            }
            if (n <= 0) yield break;
            _scatterBusy = true;
            Sfx.Takeoff(n);
            Sfx.FlockFlutter(Mathf.Min(3, Mathf.Max(1, n)));
            for (int i = 0; i < n; i++)
            {
                if (birds[i] == null) continue;
                var p = birds[i].transform.position;
                var hold = p + new Vector3(
                    Random.Range(-1.15f, 1.15f),
                    Random.Range(0.85f, 1.7f),
                    0f);
                float delay = i * Random.Range(0.02f, 0.05f);
                StartCoroutine(ScatterHold(birds[i].transform, hold, delay));
            }
            // Only wait for the flock to break off the sparrow. The limb break
            // (0.62s) runs while they are still climbing, so every lift (max
            // 0.2s stagger + 0.64s) lands just before PerchOnRemain takes over.
            // The old 0.92s + n*0.05s wait parked the whole row motionless
            // mid-screen for about a second before the branch even broke.
            yield return new WaitForSeconds(0.30f);
            _scatterBusy = false;
        }

        static IEnumerator ScatterHold(Transform tr, Vector3 dest, float delay)
        {
            int gen = _motionGen;
            if (tr == null) yield break;
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (gen != _motionGen || tr == null) yield break;
            var start = tr.position;
            var scale = BranchView.BirdScale;
            float side = dest.x >= start.x ? 1f : -1f;
            float lift = Random.Range(0.55f, 1.05f);
            float dur = Random.Range(0.48f, 0.64f);
            var idle = tr.GetComponent<BirdIdle>();
            if (idle != null)
            {
                idle.FaceLeft = dest.x < start.x;
                idle.Flapping = true;
                idle.Frozen = true;
            }
            var sr = tr.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.flipX = dest.x < start.x;
                var c = sr.color;
                c.a = 1f;
                sr.color = c;
                sr.enabled = true;
            }
            tr.gameObject.SetActive(true);
            float t = 0f;
            while (t < dur)
            {
                if (gen != _motionGen || tr == null) yield break;
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, t / dur);
                var p = Vector3.Lerp(start, dest, u);
                p.y += Mathf.Sin(u * Mathf.PI) * lift;
                p.x += Mathf.Sin(u * Mathf.PI * 2.1f) * side * 0.22f * (1f - u);
                tr.position = p;
                tr.localScale = scale;
                yield return null;
            }
            if (gen != _motionGen || tr == null) yield break;
            tr.position = dest;
            int wave = _perchWave;
            float phase = (tr.GetInstanceID() & 1023) * 0.017f;
            float hung = 0f;
            float chirpAt = 0.35f + (phase % 0.5f);
            bool chirped = false;
            while (gen == _motionGen && tr != null && wave == _perchWave)
            {
                hung += Time.deltaTime;
                float bob = Mathf.Sin(hung * 3.1f + phase) * 0.16f;
                float loopX = Mathf.Sin(hung * 1.5f + phase) * 0.20f;
                float loopY = Mathf.Cos(hung * 1.7f + phase * 1.3f) * 0.07f;
                tr.position = dest + new Vector3(loopX, bob + loopY, 0f);
                if (idle != null)
                    idle.Flapping = Mathf.Sin(hung * 6.5f + phase) > -0.15f;
                if (!chirped && hung >= chirpAt)
                {
                    chirped = true;
                    if ((tr.GetInstanceID() & 3) != 0) Sfx.Chirp();
                }
                yield return null;
            }
        }

        // Both scraps call this the moment the pest has left, or when the scrap
        // ends without a leave. A final-clear leave scores here. Collect then
        // runs the same win check as any other clear, with no perch wait.
        IEnumerator AfterPestLeaves(
            bool left, bool scoreClear,
            SpriteRenderer[] birds, int n, FeederView feeder,
            BranchView view, int slot, int branch)
        {
            int gen = _motionGen;
            if (!left || !scoreClear)
            {
                yield return ScatterUp(birds, n);
                if (gen != _motionGen) yield break;
                if (feeder != null) feeder.SnapHome();
                yield break;
            }
            _perchWave++;
            if (birds != null)
            {
                for (int i = 0; i < n; i++)
                {
                    var bird = birds[i];
                    if (bird == null) continue;
                    bird.gameObject.SetActive(false);
                    bird.enabled = false;
                }
            }
            BirdColor? liveBefore = null;
            if (slot >= 0 && _board != null && _board.Live != null && (uint)slot < (uint)_board.Live.Length)
                liveBefore = _board.Live[slot];
            ReleaseClearedFlock();
            if (_board != null)
            {
                // The slot was captured when the match was still on the limb.
                if ((uint)branch < (uint)_board.Branches.Count)
                {
                    var src = _board.Branches[branch];
                    if (src != null)
                    {
                        src.Birds.Clear();
                        src.Shrouded.Clear();
                        src.Broken = true;
                    }
                }
                _board.ScoreFeeder(slot);
            }
            _wakeBranch = WakeBranchFor(slot, liveBefore, branch);
            if (view != null)
                StartCoroutine(view.BreakAway());
            _pestClearScored = true;
            if (feeder != null)
            {
                if (_scoreImmediate) feeder.SnapHome();
                else yield return RetireFeeder(feeder, slot, gen);
            }
        }

        // PestPark already moved the last flock. Take those birds off the board
        // so the clear counts, and note them so Conserve does not put them back.
        void ReleaseClearedFlock()
        {
            if (_board == null) return;
            var list = _board.Branches;
            for (int i = 0; i < list.Count; i++)
            {
                var b = list[i];
                if (b == null || b.Count == 0) continue;
                NoteFlockLeft(b);
                b.Birds.Clear();
                b.Shrouded.Clear();
                if (!b.IsBonus) b.Broken = true;
            }
        }

        // Limb snap, then every fighter flies to the seat just chosen.
        // Sprites stay alive: SyncAll re-parents them. Destroying them here was
        // how a restored gift limb lost its birds.
        IEnumerator FinishPest(BranchView view, SpriteRenderer[] birds, int n, int branch, bool hawk, int gen)
        {
            if (_pestFinishing)
            {
                AdLog.Add("pest finish ignored (already running)");
                yield break;
            }
            _pestFinishing = true;
            // One path for sparrow, hawk, and any later pest that knocks birds off.
            var plan = ResolveDisplaced(branch, hawk);
            AlignBranchViews();
            if (view != null)
            {
                yield return view.BreakAway();
                if (gen != _motionGen) { _pestFinishing = false; yield break; }
                bool regrow = !plan.Break || (branch >= 0 && branch < _board.Branches.Count && _board.Branches[branch].IsBonus);
                if (regrow) view.Revive();
            }
            if (gen != _motionGen) { _pestFinishing = false; yield break; }
            yield return GardenFit.Tween(_garden, _board, false);
            if (gen != _motionGen) { _pestFinishing = false; yield break; }
            yield return PerchOnRemain(birds, n, plan);
            if (gen != _motionGen) { _pestFinishing = false; yield break; }
            SyncAll(true);
            Conserve("pest");
            _pestFinishing = false;
        }

        // Seats the flock Hold lifted. Called only after the scrap, so the
        // player had the whole fight to use every other limb.
        PestPark.Plan ResolveDisplaced(int branch, bool hawk)
        {
            if (_board == null) return default;
            return PestPark.Resolve(_board, branch, hawk);
        }

        void EndPest()
        {
            _pestResolving = false;
            _pestFinishing = false;
            _pestClearScored = false;
            _scatterBusy = false;
            _scoreImmediate = false;
        }

        void BeginCollect()
        {
            _collectDepth++;
            _collecting = true;
            // A collect while the leaf line is up is the breeze. It does not wait
            // for the foliage to finish lifting, and a reset parade is not a collect.
            if (_leafIntro && !_restarting)
                DismissLeafIntro();
        }

        void EndCollect()
        {
            if (_collectDepth > 0) _collectDepth--;
            _collecting = _collectDepth > 0;
        }

        bool PestOnStage()
        {
            return SparrowView.Live != null || HawkView.Live != null;
        }

        // A bird already bolting (perch dropped) does not hold the resolve.
        // One still on a feeder does, until ReleaseSolvedPests sends it off.
        bool PestBlocksResolve()
        {
            var s = SparrowView.Live;
            if (s != null && s.IsBlocking) return true;
            var h = HawkView.Live;
            if (h != null && h.IsBlocking) return true;
            return false;
        }

        void ReleaseSolvedPests()
        {
            if (_splash || _board == null || !PestSchedule.SolvedNow) return;
            if (SparrowView.Live != null) SparrowView.Live.LeaveForSolve();
            if (HawkView.Live != null) HawkView.Live.LeaveForSolve();
            bool blocking = (SparrowView.Live != null && SparrowView.Live.IsBlocking)
                || (HawkView.Live != null && HawkView.Live.IsBlocking);
            if (!blocking && (_pestCue == PestCueSparrow || _pestCue == PestCueHawk))
                PestIntroHide();
        }

        // One win latch. A pest on a solved garden does not wait for locks or the fit tween.
        bool TryScoreGarden()
        {
            if (_gardenScoring || _won) return true;
            if (_restarting || _splash || _frozen) return false;
            if (!SolvedRule.FinaleDue(_board)) return false;
            if (_collectDepth > 0 || _collecting) return false;
            if (!PestBlocksResolve() && (_locked.Count > 0 || GardenFit.Busy)) return false;
            _gardenScoring = true;
            StopPests();
            StartCoroutine(SettleIfIdle());
            return true;
        }

        // ---- Solved-board auto-resolve ----
        // SolvedRule.IsSolved: every remaining bird is already in a complete set of
        // five (or the board is empty). A 4+1 pest park is not solved, so it does not
        // join, does not clear, and does not start the finale. The player hop that
        // completes the five does. Polled from Update, and again when a tutorial
        // freeze lets go (ReleaseTutorPause). PestPark still refuses a seat that
        // would complete a five, so the flock home from a scrap stays split.
        bool _solvedMerging;

        bool SolvedFinishIdle()
        {
            if (_board == null || _won || _gardenScoring || _restarting || _splash || _frozen) return false;
            if (GamePause.Paused || _gift != GiftFace.None || _restartAsk != RestartAsk.None) return false;
            if (_solvedMerging || _collecting || _collectDepth > 0 || _locked.Count > 0 || GardenFit.Busy) return false;
            if (_pestResolving || _pestFinishing || _scatterBusy) return false;
            // A guided garden hop is the player's to make.
            if (_coach && _cueHand) return false;
            return SolvedRule.IsSolved(_board);
        }

        void TrySolvedFinish()
        {
            if (!SolvedFinishIdle()) return;
            if (!_board.NextSolvedMerge(out int from, out int to)) return;
            if (Locked(from) || Locked(to)) return;
            StartCoroutine(SolvedMerge(from, to));
        }

        IEnumerator SolvedMerge(int from, int to)
        {
            int gen = _motionGen;
            _solvedMerging = true;
            if (_sel >= 0 && _garden.Branches != null && (uint)_sel < (uint)_garden.Branches.Length)
                _garden.Branches[_sel].SetReady(false);
            _sel = -1;
            int fromCount = _board.Branches[from].Count;
            int toCount = _board.Branches[to].Count;
            var col = _board.Branches[from].Birds[0].Color;
            Lock(from);
            Lock(to);
            if (!_board.MergeRun(from, to, out int run))
            {
                Unlock(from);
                Unlock(to);
                _solvedMerging = false;
                yield break;
            }
            AdLog.Add("solved board: auto-join " + run + " " + col + " limb " + from + " -> " + to);
            yield return Hop(from, to, run, fromCount, toCount, col);
            if (gen != _motionGen) { _solvedMerging = false; yield break; }
            Unlock(from);
            Unlock(to);
            SyncAll();
            KickCollects();
            SyncAll();
            _solvedMerging = false;
            if (_locked.Count == 0)
                yield return GardenFit.Tween(_garden, _board, false);
        }

        // ---- Build 61: auto-resolve speed ----
        // The solved-board run (merges, collects, combo pops, feeder swaps, the pre-finale beat)
        // plays at AutoResolveSpeed. One knob: the base time scale, which every scaled timer in
        // that run already reads (Hop, Collect, FlyHit, RetireFeeder, BreakAway, GardenFit.Tween,
        // PlayComboPop). Sounds fire off the same timeline, so they stay on their beats. The two
        // real-time waits in the window (combo pop spacing, the pre-finale beat) divide by it by
        // hand. Back to 1 at the finale, Load, restart, home, a pest still on a feeder, an ad, background.
        // Build 63: another 20% (1.2 x 1.2).
        const float AutoResolveSpeed = 1.44f;
        float _autoResolveScale = 1f;
        bool _autoResolveDone;

        bool AutoResolveLive()
        {
            if (_board == null || _splash || _restarting || _autoResolveDone || Ads.IsShowing) return false;
            if (_frozen || _gift != GiftFace.None) return false;
            if (PestBlocksResolve() || _pestResolving || _pestFinishing) return false;
            return SolvedRule.IsSolved(_board);
        }

        // In-garden collection panel or bee-card inspect. One gate for "hive UI is up":
        // SettleIfIdle holds the finale on it so the board can finish resolving underneath.
        bool HiveCollectionOpen() => _levelHive || _hiveInspect >= 0;

        void SyncAutoResolveSpeed()
        {
            SetAutoResolveSpeed(AutoResolveLive() ? AutoResolveSpeed : 1f);
        }

        void SetAutoResolveSpeed(float scale)
        {
            if (Mathf.Approximately(scale, _autoResolveScale)) return;
            _autoResolveScale = scale;
            GamePause.SetBaseScale(scale);
        }

        IEnumerator WaitGardenScore()
        {
            float guard = 0f;
            while (_gardenScoring && !_splash && guard < 40f)
            {
                guard += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        void ArmGardenStamp()
        {
            _rewardStampHit = false;
            _waxBorn = -1f;
            if (_splash || !StreakTier.AtStake(Purse.Streak))
            {
                _gardenStampAt = -1f;
                return;
            }
            _gardenStampAt = Time.unscaledTime;
        }

        bool GardenStampHit(Vector2 screen)
        {
            if (_gardenStampAt < 0f || _gardenStampRect.width < 8f) return false;
            float gy = Screen.height - screen.y;
            return _gardenStampRect.Contains(new Vector2(screen.x, gy));
        }

        // One ad-log line per scrap. Mashing the screen must not fill the log.
        void NotePestGuard(string what)
        {
            if (_pestGuardNoted) return;
            _pestGuardNoted = true;
            AdLog.Add(what);
        }

        IEnumerator PerchOnRemain(SpriteRenderer[] birds, int n, PestPark.Plan plan)
        {
            int gen = _motionGen;
            if (n <= 0 || gen != _motionGen) yield break;
            var dests = new Vector3[n];
            var parked = new bool[n];
            var homes = plan.Homes;
            for (int i = 0; i < n; i++)
            {
                if (homes == null || i >= homes.Length) continue;
                int home = homes[i].Branch;
                int seat = homes[i].Seat;
                if (_garden.Branches == null || (uint)home >= (uint)_garden.Branches.Length) continue;
                var br = _garden.Branches[home];
                if (br == null || seat < 0 || seat >= BranchState.Cap) continue;
                dests[i] = br.SeatWorld(seat) + br.transform.TransformVector(new Vector3(0f, BranchView.RestLift, 0f));
                parked[i] = true;
            }

            Sfx.FlockFlutter(Mathf.Max(1, n));
            _perchWave++;
            float wait = 1.15f;
            for (int i = 0; i < n; i++)
            {
                if (birds[i] == null) continue;
                float delay = i * 0.08f;
                wait = Mathf.Max(wait, delay + 0.88f);
                bool faceLeft = parked[i] ? dests[i].x >= 0f : birds[i].transform.position.x >= 0f;
                if (parked[i] && homes != null && i < homes.Length)
                {
                    int home = homes[i].Branch;
                    if (_garden.Branches != null && (uint)home < (uint)_garden.Branches.Length && _garden.Branches[home] != null)
                        faceLeft = _garden.Branches[home].FromRight;
                }
                if (parked[i])
                    StartCoroutine(PerchOne(birds[i].transform, dests[i], delay, faceLeft));
                else
                    StartCoroutine(PerchOne(birds[i].transform, birds[i].transform.position + Vector3.up * 0.4f, delay, faceLeft));
            }
            yield return new WaitForSeconds(wait);
            if (gen != _motionGen) yield break;
            yield return new WaitForSeconds(0.35f);
        }

        static IEnumerator PerchOne(Transform tr, Vector3 dest, float delay, bool faceLeft)
        {
            int gen = _motionGen;
            if (tr == null) yield break;
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (gen != _motionGen || tr == null) yield break;
            var start = tr.position;
            var scale = BranchView.BirdScale;
            float dur = 0.78f;
            var idle = tr.GetComponent<BirdIdle>();
            if (idle != null)
            {
                // Land already facing center: right-half perches face left.
                idle.FaceLeft = faceLeft;
                idle.Flapping = true;
                idle.Frozen = true;
            }
            var sr = tr.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.enabled = true;
                sr.flipX = faceLeft;
                var c = sr.color;
                c.a = 1f;
                sr.color = c;
            }
            tr.gameObject.SetActive(true);
            float t = 0f;
            while (t < dur)
            {
                if (gen != _motionGen || tr == null) yield break;
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, t / dur);
                var p = Vector3.Lerp(start, dest, u);
                p.y += Mathf.Sin(u * Mathf.PI) * 2.45f;
                tr.position = p;
                tr.localScale = scale;
                if (sr != null) sr.flipX = faceLeft;
                if (idle != null) idle.FaceLeft = faceLeft;
                yield return null;
            }
            if (gen != _motionGen || tr == null) yield break;
            tr.position = dest;
            tr.localScale = scale;
            if (sr != null) sr.flipX = faceLeft;
            if (idle != null) idle.FaceLeft = faceLeft;
            // Stay Frozen: this is a temp fight sprite still parented to the
            // garden root. Unfreezing let BirdIdle write its old seat-local
            // RestLocal in root space, snapping the row to mid-screen until
            // PerchOnRemain destroys it and SyncAll shows the real seat bird.
            if (idle != null) idle.Flapping = false;
        }

        Vector3[] ScatterDests(int need)
        {
            var list = new System.Collections.Generic.List<Vector3>(16);
            if (_garden.Branches != null)
            {
                for (int b = 0; b < _garden.Branches.Length; b++)
                {
                    var br = _garden.Branches[b];
                    if (br == null) continue;
                    if (_board != null && b < _board.Branches.Count && _board.Branches[b].Broken)
                        continue;
                    for (int s = 0; s < BranchState.Cap; s++)
                    {
                        var seat = br.SeatWorld(s);
                        if (seat.sqrMagnitude > 0.01f)
                            list.Add(seat + new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(0.05f, 0.25f), 0f));
                    }
                }
            }
            while (list.Count < need)
            {
                float ang = Random.Range(0f, Mathf.PI * 2f);
                float rad = Random.Range(3.2f, 6.4f);
                list.Add(new Vector3(Mathf.Cos(ang) * rad, 2.5f + Mathf.Abs(Mathf.Sin(ang)) * 3.5f, 0f));
            }
            // Shuffle lightly.
            for (int i = 0; i < list.Count; i++)
            {
                int j = Random.Range(i, list.Count);
                var tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
            return list.ToArray();
        }

        static IEnumerator ScatterOne(Transform tr, Vector3 dest, float delay)
        {
            int gen = _motionGen;
            if (tr == null) yield break;
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (gen != _motionGen || tr == null) yield break;
            var start = tr.position;
            var scale = tr.localScale;
            float side = dest.x >= start.x ? 1f : -1f;
            float lift = Random.Range(1.5f, 2.9f);
            float sway = Random.Range(0.28f, 0.72f);
            float wobble = Random.Range(1.6f, 2.8f);
            float dur = Random.Range(0.44f, 0.66f);
            var idle = tr.GetComponent<BirdIdle>();
            if (idle != null)
            {
                idle.FaceLeft = dest.x < start.x;
                idle.Flapping = true;
            }
            var srFace = tr.GetComponent<SpriteRenderer>();
            if (srFace != null) srFace.flipX = dest.x < start.x;
            float t = 0f;
            while (t < dur)
            {
                if (gen != _motionGen || tr == null) yield break;
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, t / dur);
                var p = Vector3.Lerp(start, dest, u);
                p.y += Mathf.Sin(u * Mathf.PI) * lift;
                p.x += Mathf.Sin(u * Mathf.PI * wobble) * side * sway * (1f - u * 0.65f);
                tr.position = p;
                tr.localScale = scale * Mathf.Lerp(1f, 0.68f, u);
                if (srFace != null)
                {
                    var c = srFace.color;
                    c.a = 1f - u * 0.88f;
                    srFace.color = c;
                }
                yield return null;
            }
            if (gen != _motionGen || tr == null) yield break;
            tr.gameObject.SetActive(false);
            var hide = tr.GetComponent<SpriteRenderer>();
            if (hide != null) hide.enabled = false;
        }

        IEnumerator SettleIfIdle()
        {
            if (_won || !SolvedRule.FinaleDue(_board))
            {
                _gardenScoring = false;
                yield break;
            }
            if (!_gardenScoring && _locked.Count > 0) yield break;
            _won = true;
            _finaleHold = true;
            _busy = true;
            StopPests();
            // Keep an open collection / inspect up: resolve finishes under it; finale waits.
            while (_comboPopPump || _comboPopLive > 0 || _comboShows.Count > 0)
            {
                if (GamePause.Paused)
                {
                    yield return null;
                    continue;
                }
                yield return null;
            }
            ClearComboShows();
            float beat = 0f;
            while (beat < 0.55f * RewardPace)
            {
                // Real-time beat, so it takes the auto-resolve speed by hand.
                if (!GamePause.Paused) beat += Time.unscaledDeltaTime * _autoResolveScale;
                yield return null;
            }
            if (_restarting || _splash)
            {
                _gardenScoring = false;
                yield break;
            }
            // Auto-resolve window ends here (garden work done). Hive browse is real-time.
            _autoResolveDone = true;
            SetAutoResolveSpeed(1f);
            // Hold the finale until the player dismisses the collection / inspect.
            while (SolvedRule.FinaleHeldByHive(HiveCollectionOpen()))
            {
                if (_restarting || _splash)
                {
                    _gardenScoring = false;
                    yield break;
                }
                yield return null;
            }
            yield return FinaleShow.Play(_garden, this);
            if (_restarting || _splash)
            {
                _gardenScoring = false;
                yield break;
            }
            yield return new WaitForSeconds(0.45f * RewardPace);
            if (_restarting || _splash)
            {
                _gardenScoring = false;
                yield break;
            }
            yield return Ads.Interstitial();
            if (_restarting || _splash)
            {
                _gardenScoring = false;
                yield break;
            }
            // Badger flag first, then the unlock, with no yield between: a kill must not
            // unlock the next garden without the unpaid flag. DisplayNumber is still the
            // cleared level (RememberClear does not move Index). No-op while the switch is off.
            BadgerSave.SetIfDue(LevelData.DisplayNumber);
            LevelData.RememberClear();
            Purse.AwardClear();
            // Sparrow, hawk, and a plain clear all arrive here.
            // The homepage sign spends this clear and plays the coins.
            PlayClearReward();
            if (_restarting || _splash)
            {
                _clearReward = false;
                _streakHomeDue = false;
                _gardenScoring = false;
                yield break;
            }
            ArmAvatarCross();
            // Badger owed (flag set above): the leap plays over the live garden, then the
            // contest opens instead of the plain splash. Reads 0 while the switch is off.
            int owed = BadgerSave.Pending;
            if (owed > 0)
            {
                yield return BadgerClearLeap();
                if (_restarting || _splash)
                {
                    _bgLeap.Release();
                    if (MixDesk.Live != null) MixDesk.Live.SetBadger(false);
                    _gardenScoring = false;
                    yield break;
                }
                ShowSplash();
                if (!OpenBadgerFight(owed))
                {
                    _bgLeap.Release();
                    if (MixDesk.Live != null) MixDesk.Live.SetBadger(false);
                }
                yield break;
            }
            ShowSplash();
        }

        // Marks the homepage sign. Does not commit and does not spawn coins.
        // StepStreakReward pays once, on the home pay beat, through the shared flight.
        void PlayClearReward()
        {
            _clearReward = false;
            if (_restarting || _splash)
            {
                _streakHomeDue = false;
                return;
            }
            _streakHomeDue = Purse.Streak > 0 && Purse.Pending > 0;
        }

        bool TipLocked(int i)
        {
            var br = _board.Branches[i];
            return br.TipLocked;
        }

        int ShroudedTips()
        {
            int n = 0;
            for (int i = 0; i < _board.Branches.Count; i++)
            {
                var br = _board.Branches[i];
                if (br.Broken || br.Count == 0) continue;
                if (br.IsShrouded(br.Count - 1)) n++;
            }
            return n;
        }

        // A full row whose color just became the only live copy of that feeder.
        // Checked after ApplyCollect, while the perch view is still asleep.
        int WakeBranchFor(int slot, BirdColor? before, int just)
        {
            if (_board == null || slot < 0 || _board.Live == null) return -1;
            if ((uint)slot >= (uint)_board.Live.Length) return -1;
            var after = _board.Live[slot];
            if (!after.HasValue) return -1;
            if (before.HasValue && before.Value == after.Value) return -1;
            for (int i = 0; i < _board.Live.Length; i++)
            {
                if (i == slot) continue;
                if (_board.Live[i].HasValue && _board.Live[i].Value == after.Value) return -1;
            }
            var col = after.Value;
            for (int i = 0; i < _board.Branches.Count; i++)
            {
                if (i == just) continue;
                var br = _board.Branches[i];
                if (br.Broken || br.Count == 0) continue;
                if (br.IsFullMatch(out var c) && c == col) return i;
            }
            return -1;
        }

        // One alert for the row, then a short beat before anyone flies to feed.
        IEnumerator WakeRow(int branch, int gen)
        {
            if (_garden.Branches == null || (uint)branch >= (uint)_garden.Branches.Length) yield break;
            var view = _garden.Branches[branch];
            if (view == null || view.Birds == null) yield break;
            for (int i = 0; i < view.Birds.Length; i++)
            {
                var b = view.Birds[i];
                if (b == null || !b.enabled || !b.gameObject.activeInHierarchy) continue;
                var idle = b.GetComponent<BirdIdle>();
                if (idle == null || idle.Shrouded) continue;
                idle.WakeAlert();
                if (SfxLibrary.TakeWakeSting())
                    Sfx.RowAlert();
            }
            float t = 0f;
            const float dur = 0.50f;
            while (t < dur)
            {
                if (gen != _motionGen) yield break;
                if (GamePause.Paused) { yield return null; continue; }
                t += Time.deltaTime;
                yield return null;
            }
        }

        // Feeder leaves after the last tick, then the next color glides in.
        // The caller waits this out so the next flock does not launch into the exit.
        IEnumerator RetireFeeder(FeederView feeder, int slot, int gen)
        {
            float wait = 0f;
            while (wait < 0.16f)
            {
                if (gen != _motionGen || feeder == null) yield break;
                if (GamePause.Paused) { yield return null; continue; }
                wait += Time.deltaTime;
                yield return null;
            }
            if (gen != _motionGen || feeder == null) yield break;
            var pull = feeder.PullAway();
            while (gen == _motionGen && pull.MoveNext())
                yield return pull.Current;
            if (gen != _motionGen)
            {
                if (feeder != null) feeder.SnapHome();
                yield break;
            }
            if (feeder == null || _board == null) yield break;
            if ((uint)slot < (uint)_board.Live.Length)
                feeder.Show(_board.Live[slot]);
            float guard = 0f;
            while (gen == _motionGen && feeder != null && feeder.InTransit && guard < 2.2f)
            {
                if (GamePause.Paused) { yield return null; continue; }
                guard += Time.deltaTime;
                yield return null;
            }
            if (gen != _motionGen && feeder != null) feeder.SnapHome();
        }

        IEnumerator FlyHit(Transform tr, Vector3 dest, float delay, float dur, int pop, FeederView feeder, int[] landed)
        {
            int gen = _motionGen;
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (gen != _motionGen) yield break;
            if (tr == null)
            {
                NoteLanded(landed);
                yield break;
            }
            Sfx.FlockFlutter(1);
            var start = tr.position;
            var crest = tr.localScale;
            var delta = dest - start;
            var ctrl = (start + dest) * 0.5f;
            ctrl.y += Mathf.Clamp(1.05f + Mathf.Abs(delta.y) * 0.22f, 1.05f, 2.15f) + (pop % 3) * 0.12f;
            float bow = (0.34f + Mathf.Min(0.22f, Mathf.Abs(delta.x) * 0.05f)) * ((pop & 1) == 0 ? 1f : -1f);
            ctrl.x += bow;
            bool faceLeft = dest.x < start.x;
            var idle = tr.GetComponent<BirdIdle>();
            if (idle != null)
            {
                idle.Frozen = true;
                idle.Flapping = true;
                idle.FaceLeft = faceLeft;
                idle.SetFade(1f);
            }
            var sr = tr.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.flipX = faceLeft;
                sr.sortingOrder = FlockSort.Flight(pop);
            }
            bool scored = false;
            float t = 0f;
            while (t < dur)
            {
                if (gen != _motionGen) yield break;
                if (tr == null)
                {
                    NoteLanded(landed);
                    yield break;
                }
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
                tr.position = Quad(start, ctrl, dest, u);
                tr.localScale = crest * Mathf.Lerp(1f, 0.80f, u);
                float bank = Mathf.Sin(u * Mathf.PI) * (faceLeft ? 12f : -12f);
                tr.rotation = Quaternion.Euler(0f, 0f, bank);
                float fade = Mathf.InverseLerp(0.82f, 1f, u);
                if (idle != null) idle.SetFade(1f - fade);
                else if (sr != null)
                {
                    var c = sr.color;
                    c.a = 1f - fade;
                    sr.color = c;
                }
                if (!scored && u >= 0.92f)
                {
                    scored = true;
                    Sfx.ScorePop(pop);
                    if (feeder != null) feeder.ScoreTick();
                }
                yield return null;
            }
            if (gen != _motionGen) yield break;
            if (tr == null)
            {
                NoteLanded(landed);
                yield break;
            }
            tr.position = dest;
            tr.rotation = Quaternion.identity;
            tr.localScale = crest * 0.80f;
            if (!scored)
            {
                Sfx.ScorePop(pop);
                if (feeder != null) feeder.ScoreTick();
            }
            if (idle != null) idle.SetFade(1f);
            else if (sr != null)
            {
                var c = sr.color;
                c.a = 1f;
                sr.color = c;
            }
            tr.gameObject.SetActive(false);
            if (sr != null) sr.enabled = false;
            NoteLanded(landed);
        }

        static void NoteLanded(int[] landed)
        {
            if (landed != null && landed.Length > 0) landed[0]++;
        }

        static Vector3 Quad(Vector3 a, Vector3 b, Vector3 c, float u)
        {
            float k = 1f - u;
            return k * k * a + 2f * k * u * b + u * u * c;
        }

        void CheckOver()
        {
            if (_busy || _collecting || _locked.Count > 0 || _frozen) return;
            if (_won || SolvedRule.FinaleDue(_board)) return;
            if (_board == null || _board.Displaced.Count > 0) return;
            if (_gift != GiftFace.None) return;
            // A strict solve has nothing left to join. A 4+1 with a legal hop is
            // not iced; the player finishes the five. A split with no hop can ice.
            if (_board.NextSolvedMerge(out _, out _)) return;
            // Ice only when nothing can move. An open bonus limb counts, empty or not.
            // A search that finds no win is not enough: stage 1 still has hops.
            if (_board.HasHop() || _board.FindCollect() >= 0) return;
            // FreezeOver aborts if _frozen is already set. It has to set that flag itself.
            StartCoroutine(FreezeOver());
        }

        IEnumerator FreezeOver()
        {
            if (_frozen || _won) yield break;
            _frozen = true;
            _busy = true;
            _freezeOffer = true;
            _iceCoating = true;
            if (_sel >= 0)
            {
                _garden.Branches[_sel].SetReady(false);
                _sel = -1;
            }
            StillBirds(true);
            if (_garden.Ice != null) yield return _garden.Ice.Coat();
            else yield return new WaitForSeconds(0.4f);
            // Full ice holds before the card. A tap during the creep must not open it.
            yield return new WaitForSeconds(0.32f);
            _iceCoating = false;
            if (_restarting || !_frozen || _won || _gift != GiftFace.None) yield break;
            RaiseGiftCard();
        }

        void StillBirds(bool on)
        {
            if (_garden.Branches == null) return;
            for (int i = 0; i < _garden.Branches.Length; i++)
            {
                var v = _garden.Branches[i];
                if (v == null || v.Birds == null) continue;
                for (int k = 0; k < v.Birds.Length; k++)
                {
                    if (v.Birds[k] == null) continue;
                    var idle = v.Birds[k].GetComponent<BirdIdle>();
                    if (idle != null) idle.Frozen = on;
                }
            }
        }

        // Renderers hidden by VeilBoard. forceRenderingOff keeps enabled, color and
        // position alone, so LiveBird and the fly-in math read them as usual.
        readonly List<Renderer> _veiled = new List<Renderer>();

        // Stage reset only: hides (true) or restores (false) everything that sits on the
        // refreshed board between its sync and the fly-in. Always safe to call twice.
        void VeilBoard(bool on)
        {
            for (int i = 0; i < _veiled.Count; i++)
                if (_veiled[i] != null) _veiled[i].forceRenderingOff = false;
            _veiled.Clear();
            if (!on) return;
            if (_garden.Branches != null)
            {
                for (int i = 0; i < _garden.Branches.Length; i++)
                {
                    var v = _garden.Branches[i];
                    if (v == null || v.Birds == null) continue;
                    for (int k = 0; k < v.Birds.Length; k++)
                        VeilBird(v.Birds[k]);
                }
            }
            var bees = new List<SpriteRenderer>();
            var leaves = new List<SpriteRenderer>();
            GatherDecor(bees, leaves);
            for (int i = 0; i < bees.Count; i++) VeilOne(bees[i]);
            for (int i = 0; i < leaves.Count; i++) VeilOne(leaves[i]);
            if (_garden.Feeders != null)
            {
                for (int i = 0; i < _garden.Feeders.Length; i++)
                {
                    if (_garden.Feeders[i] == null) continue;
                    var parts = _garden.Feeders[i].GetComponentsInChildren<Renderer>(true);
                    for (int k = 0; k < parts.Length; k++) VeilOne(parts[k]);
                }
            }
        }

        // Bird bodies veil through BirdIdle so the kit, face and fx hide in the
        // same call. Lifting the veil (forceRenderingOff off) lets LateUpdate
        // dress the bird again wherever it is by then.
        void VeilBird(SpriteRenderer b)
        {
            if (b == null) return;
            var idle = b.GetComponent<BirdIdle>();
            if (idle != null) idle.Veil(true);
            VeilOne(b);
        }

        void VeilOne(Renderer r)
        {
            if (r == null) return;
            r.forceRenderingOff = true;
            _veiled.Add(r);
        }

        IEnumerator SnapRound()
        {
            if (_restarting)
            {
                // Second tap during the parade. Stay cut; do not resume in the gap.
                TickCoachReset(true, false);
                yield break;
            }
            _motionGen++;
            _restarting = true;
            _solvedMerging = false;
            SetAutoResolveSpeed(1f);
            GardenFit.ClearBusy();
            // Cut the caption and glove this frame, before the flock moves.
            TickCoachReset(true, false);
            _busy = true;
            // Stage attempt only. Album copies already saved stay in the collection.
            _levelBees.Clear();
            _levelHive = false;
            _levelHivePick = 0;
            _levelHiveShown = 0f;
            _levelHiveCountSeen = -1;
            _levelHiveDrag = false;
            _levelHiveDragX = 0f;
            _incomingHalo.Clear();
            _wakeBranch = -1;
            CloseHiveInspect();
            if (_garden.Hive != null) _garden.Hive.CancelVisitors();
            HoldDecor(true);
            _gift = GiftFace.None;
            _keepStreak = false;
            _frozen = false;
            _freezeOffer = false;
            _iceCoating = false;
            // Drop the lift + selection glow on the branch that was picked before Restart.
            if (_sel >= 0 && _garden.Branches != null && _sel < _garden.Branches.Length && _garden.Branches[_sel] != null)
                _garden.Branches[_sel].SetReady(false);
            _sel = -1;
            _combo = 0;
            _comboUntil = -99f;
            SfxLibrary.CloseCombo();
            ClearComboShows();
            _collecting = false;
            _collectDepth = 0;
            _gardenScoring = false;
            _won = false;
            EndPest();
            _locked.Clear();
            HaltBreaks();
            SparrowView.ClearFx();
            if (HawkView.Live != null) HawkView.Live.EndScrap();
            if (SparrowView.Live != null) SparrowView.Live.FinishEvict();
            // Own every visible bird before any yield, including ones already in the air
            // and silhouettes under bees or leaves, so a limb break or hop cannot hide them.
            var flock = ClaimFlock();
            if (_garden.Ice != null)
                yield return _garden.Ice.Shatter(_garden.Root);
            yield return FlyOffAll(flock);
            if (_seed != null) _board = _seed.Clone();
            BonusBranches.ApplyClaims(_board, _bonusOn);
            _census = BoardValidator.Counts(_board);
            AlignBranchViews();
            RefreshBonusSigns();
            if (_garden.Feeders != null)
            {
                for (int i = 0; i < _garden.Feeders.Length; i++)
                    if (_garden.Feeders[i] != null) _garden.Feeders[i].SnapHome();
            }
            SyncAll(true);
            // The refreshed board is now seated on its perches. Keep its birds, bees,
            // leaves and feeders unseen until SnapBirdsHome has moved them offscreen,
            // so the new board never flashes before the fly-in.
            VeilBoard(true);
            // Instant fit, same frame. A nested `yield return` costs a frame
            // even when the child never yields, and that frame drew the seated
            // board. Drain it here so SnapBirdsHome moves the flock offscreen
            // before anything renders. The veil stays as a backstop.
            var fit = GardenFit.Tween(_garden, _board, true);
            while (fit.MoveNext()) yield return fit.Current;
            yield return SnapBirdsHome();
            VeilBoard(false);
            HoldDecor(false);
            Conserve("restart");
            // Formation is home. This frame stays dark; the interrupted step fades in next.
            _restarting = false;
            TickCoachReset(false, true);
            StillBirds(false);
            _busy = false;
            ArmGardenStamp();
        }

        void RefreshBonusSigns()
        {
            if (_garden.Branches == null || _board == null) return;
            int n = Mathf.Min(_garden.Branches.Length, _board.Branches.Count);
            for (int i = 0; i < n; i++)
            {
                var v = _garden.Branches[i];
                if (v == null) continue;
                var want = v.GetComponent<GiftWant>();
                if (want == null) continue;
                bool locked = _board.Branches[i].AdLocked && !_board.Branches[i].Broken;
                want.On = locked;
                want.enabled = locked;
                if (v.Sign != null) v.Sign.gameObject.SetActive(locked);
            }
        }

        // Rebuild the limb row from the board. Old snap-off pieces and extra
        // gift views are destroyed first so a restart cannot leave a hole or a twin.
        void AlignBranchViews()
        {
            if (_board == null || _garden.Root == null) return;
            int n = _board.Branches.Count;
            var old = _garden.Branches ?? new BranchView[0];
            var next = new BranchView[n];
            var used = new bool[old.Length];
            for (int i = 0; i < n; i++)
            {
                bool bonus = _board.Branches[i] != null && _board.Branches[i].IsBonus;
                bool fromRight = SideRight(i);
                BranchView v = null;
                if (i < old.Length && !used[i] && old[i] != null
                    && old[i].IsGift == bonus && old[i].FromRight == fromRight)
                {
                    v = old[i];
                    used[i] = true;
                }
                for (int j = 0; v == null && j < old.Length; j++)
                {
                    if (used[j] || old[j] == null) continue;
                    if (old[j].IsGift != bonus || old[j].FromRight != fromRight) continue;
                    v = old[j];
                    used[j] = true;
                    break;
                }
                if (v == null)
                    v = bonus
                        ? WorldBuilder.MakeGift(i, fromRight, _garden.Root)
                        : WorldBuilder.MakePlain(i, fromRight, _garden.Root);
                v.Index = i;
                v.ClearDebris();
                if (_board.Branches[i] != null && !_board.Branches[i].Broken)
                    v.Revive();
                next[i] = v;
            }
            for (int j = 0; j < old.Length; j++)
            {
                if (used[j] || old[j] == null) continue;
                DestroyBranchView(old[j]);
            }
            _garden.Branches = next;
        }

        bool SideRight(int index)
        {
            if (_board == null || (uint)index >= (uint)_board.Branches.Count) return false;
            if (_board.Branches[index] != null && _board.Branches[index].IsBonus)
            {
                int n = 0;
                for (int i = 0; i < index; i++)
                    if (_board.Branches[i] != null && _board.Branches[i].IsBonus) n++;
                return (n & 1) == 1;
            }
            int plain = 0;
            for (int i = 0; i < index; i++)
            {
                var st = _board.Branches[i];
                if (st != null && !st.IsBonus) plain++;
            }
            return (plain & 1) == 1;
        }

        static void DestroyBranchView(BranchView v)
        {
            if (v == null) return;
            if (v.Birds != null)
            {
                for (int s = 0; s < v.Birds.Length; s++)
                    if (v.Birds[s] != null) Object.Destroy(v.Birds[s].gameObject);
            }
            Object.Destroy(v.gameObject);
        }

        // A feeder collect really removes those birds. The census is the seed
        // count, so without this Conserve treats them as lost and parks them
        // on the emptiest limb — a gift that just opened.
        void NoteFlockLeft(BranchState br)
        {
            if (_census == null || br == null) return;
            for (int k = 0; k < br.Count; k++)
            {
                int c = (int)br.Birds[k].Color;
                if ((uint)c < (uint)_census.Length && _census[c] > 0)
                    _census[c]--;
            }
        }

        void Conserve(string why)
        {
            if (_board == null || _census == null) return;
            var report = BoardValidator.Check(_board, _census);
            if (report.Ok) return;
            Debug.LogWarning("Flock Five conserve (" + why + "): " + report.Message);
            int put = BoardValidator.Restore(_board, _census);
            if (put > 0) AlignBranchViews();
            SyncAll(true);
            var again = BoardValidator.Check(_board, _census);
            if (!again.Ok)
                Debug.LogWarning("Flock Five conserve still short (" + why + "): " + again.Message);
        }

        void HaltBreaks()
        {
            if (_garden.Branches == null) return;
            for (int i = 0; i < _garden.Branches.Length; i++)
                if (_garden.Branches[i] != null) _garden.Branches[i].HaltBreak();
        }

        // Ease-in leaves (slow start, then off the screen). Ease-out arrives.
        static float EaseIn(float u)
        {
            u = Mathf.Clamp01(u);
            return u * u;
        }

        static float EaseOut(float u)
        {
            u = Mathf.Clamp01(u);
            float v = 1f - u;
            return 1f - v * v;
        }

        static float EaseInCubic(float u)
        {
            u = Mathf.Clamp01(u);
            return u * u * u;
        }

        static float EaseOutCubic(float u)
        {
            u = Mathf.Clamp01(u);
            float v = 1f - u;
            return 1f - v * v * v;
        }

        void CamPlane(out float camX, out float camY, out float halfW, out float halfH)
        {
            var cam = _garden.Cam != null ? _garden.Cam : Camera.main;
            if (cam == null)
            {
                camX = 0f;
                camY = 0f;
                halfW = 3.2f;
                halfH = 5f;
                return;
            }
            camX = cam.transform.position.x;
            camY = cam.transform.position.y;
            halfH = cam.orthographicSize;
            halfW = halfH * Mathf.Max(0.01f, cam.aspect);
        }

        static float SpriteHalfX(SpriteRenderer sr)
        {
            if (sr == null || sr.sprite == null) return 0.8f;
            return Mathf.Max(0.45f, sr.sprite.bounds.extents.x * Mathf.Abs(sr.transform.lossyScale.x));
        }

        Vector3 Offscreen(Vector3 from, SpriteRenderer sr)
        {
            CamPlane(out float camX, out float camY, out float halfW, out float halfH);
            float side = from.x >= camX ? 1f : -1f;
            float y = Mathf.Clamp(from.y + Random.Range(0.12f, 0.42f), camY - halfH + 0.35f, camY + halfH - 0.35f);
            return new Vector3(camX + side * (halfW + SpriteHalfX(sr) + 0.35f), y, from.z);
        }

        void PushPastEdge(SpriteRenderer sr)
        {
            if (sr == null || !sr.enabled) return;
            var cam = _garden.Cam != null ? _garden.Cam : Camera.main;
            if (cam == null) return;
            float halfH = cam.orthographicSize;
            float halfW = halfH * Mathf.Max(0.01f, cam.aspect);
            var c = cam.transform.position;
            var b = sr.bounds;
            if (b.max.x < c.x - halfW || b.min.x > c.x + halfW
                || b.max.y < c.y - halfH || b.min.y > c.y + halfH)
                return;
            float side = sr.transform.position.x >= c.x ? 1f : -1f;
            float gap = side > 0f ? (c.x + halfW) - b.min.x : b.max.x - (c.x - halfW);
            sr.transform.position += new Vector3(side * (gap + 0.08f), 0f, 0f);
        }

        static readonly Color FlightSilhouette = new Color(0.04f, 0.03f, 0.05f, 1f);

        void FlightScale(SpriteRenderer sr, BranchView branch)
        {
            if (sr == null) return;
            var rest = BranchView.BirdScale;
            var idle = sr.GetComponent<BirdIdle>();
            if (idle != null && idle.RestScale.sqrMagnitude > 0.0001f)
                rest = idle.RestScale;
            if (branch != null && sr.transform.parent == branch.transform)
            {
                sr.transform.localScale = new Vector3(Mathf.Abs(rest.x), Mathf.Abs(rest.y), 1f);
                return;
            }
            float bx = branch != null ? Mathf.Abs(branch.transform.lossyScale.x) : 1f;
            if (bx < 0.001f) bx = 1f;
            float px = 1f;
            if (sr.transform.parent != null)
            {
                px = Mathf.Abs(sr.transform.parent.lossyScale.x);
                if (px < 0.001f) px = 1f;
            }
            float s = Mathf.Abs(rest.x) * bx / px;
            sr.transform.localScale = new Vector3(s, s, 1f);
        }

        void ParkFlight(SpriteRenderer sr, BranchView branch)
        {
            if (sr == null) return;
            FlightScale(sr, branch);
            if (_garden.Root != null && sr.transform.parent != _garden.Root)
                sr.transform.SetParent(_garden.Root, true);
            var idle = sr.GetComponent<BirdIdle>();
            if (idle == null) return;
            idle.Frozen = true;
            idle.Flapping = false;
            idle.Lift = 0f;
        }

        void ArmFlight(SpriteRenderer sr, bool faceLeft)
        {
            if (sr == null) return;
            var idle = sr.GetComponent<BirdIdle>();
            bool shroud = idle != null && idle.Shrouded;
            if (idle != null)
            {
                idle.Sleeping = false;
                idle.Lift = 1f;
                idle.Frozen = true;
                idle.Flapping = true;
                idle.FaceLeft = faceLeft;
            }
            sr.flipX = faceLeft;
            sr.enabled = true;
            sr.sortingOrder = FlockSort.Fly;
            if (shroud)
                sr.color = FlightSilhouette;
            else if (idle != null)
                idle.SetFade(1f); // body, face and kit together (a cut-off collect fade)
            else
            {
                var c = sr.color;
                c.a = 1f;
                sr.color = c;
            }
        }

        void KeepFlight(SpriteRenderer sr, float landU)
        {
            if (sr == null) return;
            var idle = sr.GetComponent<BirdIdle>();
            bool shroud = idle != null && idle.Shrouded;
            if (idle != null)
            {
                idle.Sleeping = false;
                idle.Frozen = true;
                idle.Flapping = true;
                idle.Lift = 1f;
                if (shroud) sr.color = FlightSilhouette;
            }
            sr.enabled = true;
            // Duck a silhouette back under the leaves only as it lands.
            sr.sortingOrder = shroud && landU > 0.92f ? FlockSort.Shroud : FlockSort.Fly;
        }

        bool LiveBird(SpriteRenderer b)
        {
            return b != null && b.enabled && b.gameObject.activeInHierarchy;
        }

        List<SpriteRenderer> ClaimFlock()
        {
            var flock = new List<SpriteRenderer>();
            if (_garden.Branches == null) return flock;
            for (int i = 0; i < _garden.Branches.Length; i++)
            {
                var v = _garden.Branches[i];
                if (v == null || v.Birds == null) continue;
                for (int s = 0; s < v.Birds.Length; s++)
                {
                    var b = v.Birds[s];
                    if (!LiveBird(b)) continue;
                    ParkFlight(b, v);
                    flock.Add(b);
                }
            }
            return flock;
        }

        // Whole flock, including silhouettes, bees, and leaves, eases off past the
        // screen before the board resets. Exit 0.86s + up to 0.38s stagger
        // (was 0.22s + 0.08s). Scaled time so GamePause freezes the parade.
        IEnumerator FlyOffAll(List<SpriteRenderer> flock)
        {
            var bees = new List<SpriteRenderer>();
            var leaves = new List<SpriteRenderer>();
            GatherDecor(bees, leaves);
            bool anyBird = false;
            if (flock != null)
            {
                for (int i = 0; i < flock.Count; i++)
                    if (flock[i] != null) { anyBird = true; break; }
            }
            if (!anyBird && bees.Count == 0 && leaves.Count == 0) yield break;
            int gen = _motionGen;
            int birdN = flock != null ? flock.Count : 0;
            var starts = new Vector3[birdN];
            var dests = new Vector3[birdN];
            var delays = new float[birdN];
            var beeS = new Vector3[bees.Count];
            var beeD = new Vector3[bees.Count];
            var beeDelay = new float[bees.Count];
            var beeScale = new Vector3[bees.Count];
            var leafS = new Vector3[leaves.Count];
            var leafD = new Vector3[leaves.Count];
            var leafDelay = new float[leaves.Count];
            var leafRot = new Quaternion[leaves.Count];
            const float dur = 0.86f;
            const float spread = 0.38f;
            int n = 0;
            for (int i = 0; i < birdN; i++)
            {
                var b = flock[i];
                if (b == null) continue;
                n++;
                var start = b.transform.position;
                var dest = Offscreen(start, b);
                starts[i] = start;
                dests[i] = dest;
                delays[i] = Random.Range(0f, spread);
                ArmFlight(b, dest.x < start.x);
            }
            for (int i = 0; i < bees.Count; i++)
            {
                var b = bees[i];
                var start = b.transform.position;
                beeS[i] = start;
                beeD[i] = Offscreen(start, b);
                beeDelay[i] = Random.Range(0f, spread);
                beeScale[i] = b.transform.localScale;
                b.flipX = beeD[i].x < start.x;
                b.sortingOrder = FlockSort.Bee;
            }
            for (int i = 0; i < leaves.Count; i++)
            {
                var b = leaves[i];
                var start = b.transform.position;
                leafS[i] = start;
                leafD[i] = Offscreen(start, b);
                leafDelay[i] = Random.Range(0f, spread);
                leafRot[i] = b.transform.rotation;
                b.sortingOrder = FlockSort.Fly;
            }
            if (n > 0)
            {
                Sfx.Takeoff(n);
                Sfx.FlockFlutter(Mathf.Min(3, Mathf.Max(1, n / 3)));
            }
            float maxDelay = 0f;
            for (int i = 0; i < birdN; i++)
                if (flock[i] != null) maxDelay = Mathf.Max(maxDelay, delays[i]);
            for (int i = 0; i < beeDelay.Length; i++) maxDelay = Mathf.Max(maxDelay, beeDelay[i]);
            for (int i = 0; i < leafDelay.Length; i++) maxDelay = Mathf.Max(maxDelay, leafDelay[i]);
            float t = 0f;
            float total = dur + maxDelay;
            while (t < total)
            {
                if (gen != _motionGen) yield break;
                if (GamePause.Paused) { yield return null; continue; }
                t += Time.deltaTime;
                for (int i = 0; i < birdN; i++)
                {
                    var b = flock[i];
                    if (b == null) continue;
                    float u = Mathf.Clamp01((t - delays[i]) / dur);
                    var p = Vector3.Lerp(starts[i], dests[i], EaseInCubic(u));
                    p.y += Mathf.Sin(u * Mathf.PI) * 0.35f;
                    b.transform.position = p;
                    KeepFlight(b, 0f);
                }
                PoseBees(bees, beeS, beeD, beeDelay, beeScale, t, dur, true);
                PoseLeaves(leaves, leafS, leafD, leafDelay, leafRot, t, dur, true);
                yield return null;
            }
            for (int i = 0; i < birdN; i++)
            {
                var b = flock[i];
                if (b == null) continue;
                b.transform.position = dests[i];
                KeepFlight(b, 0f);
                PushPastEdge(b);
            }
            for (int i = 0; i < bees.Count; i++)
                if (bees[i] != null) bees[i].transform.position = beeD[i];
            for (int i = 0; i < leaves.Count; i++)
                if (leaves[i] != null) leaves[i].transform.position = leafD[i];
            yield return null;
        }

        void GatherDecor(List<SpriteRenderer> bees, List<SpriteRenderer> leaves)
        {
            if (_garden.Branches == null) return;
            for (int i = 0; i < _garden.Branches.Length; i++)
            {
                var v = _garden.Branches[i];
                if (v == null) continue;
                v.AppendBees(bees);
                v.AppendLeaves(leaves);
            }
        }

        void HoldDecor(bool on)
        {
            if (_garden.Branches == null) return;
            for (int i = 0; i < _garden.Branches.Length; i++)
                if (_garden.Branches[i] != null) _garden.Branches[i].HoldDecor(on);
        }

        static void PoseBees(List<SpriteRenderer> bees, Vector3[] from, Vector3[] to, float[] delay, Vector3[] scale, float t, float dur, bool depart)
        {
            for (int i = 0; i < bees.Count; i++)
            {
                var b = bees[i];
                if (b == null) continue;
                float u = Mathf.Clamp01((t - delay[i]) / dur);
                float e = depart ? EaseInCubic(u) : EaseOutCubic(u);
                var p = Vector3.Lerp(from[i], to[i], e);
                p.y += Mathf.Sin(u * Mathf.PI) * 0.42f;
                b.transform.position = p;
                b.sprite = SpriteCatalog.BeeFrame(Time.time * 18f + i);
                float buzz = 1f + 0.08f * Mathf.Sin(Time.time * 36f + i);
                b.transform.localScale = scale[i] * buzz;
                b.flipX = depart ? to[i].x < from[i].x : from[i].x > to[i].x;
                b.enabled = true;
            }
        }

        static void PoseLeaves(List<SpriteRenderer> leaves, Vector3[] from, Vector3[] to, float[] delay, Quaternion[] rot, float t, float dur, bool depart)
        {
            for (int i = 0; i < leaves.Count; i++)
            {
                var b = leaves[i];
                if (b == null) continue;
                float u = Mathf.Clamp01((t - delay[i]) / dur);
                float e = depart ? EaseInCubic(u) : EaseOutCubic(u);
                var p = Vector3.Lerp(from[i], to[i], e);
                float rustle = Mathf.Sin(Time.time * 7.5f + i * 0.8f) * (depart ? u : (1f - u)) * 22f;
                p.y += Mathf.Sin(u * Mathf.PI) * 0.18f;
                b.transform.position = p;
                b.transform.rotation = rot[i] * Quaternion.Euler(0f, 0f, rustle);
                b.enabled = true;
            }
        }

        IEnumerator SnapBirdsHome()
        {
            if (_garden.Branches == null) yield break;
            int gen = _motionGen;
            var birds = new List<SpriteRenderer>();
            var dests = new List<Vector3>();
            var starts = new List<Vector3>();
            var delays = new List<float>();
            const float dur = 1.08f;
            const float spread = 0.50f;
            for (int i = 0; i < _garden.Branches.Length; i++)
            {
                var v = _garden.Branches[i];
                if (v == null || v.Birds == null) continue;
                for (int s = 0; s < v.Birds.Length; s++)
                {
                    var b = v.Birds[s];
                    if (!LiveBird(b)) continue;
                    var home = b.transform.position;
                    var from = Offscreen(home, b);
                    from.y = home.y + Random.Range(0.2f, 0.55f);
                    birds.Add(b);
                    dests.Add(home);
                    starts.Add(from);
                    delays.Add(Random.Range(0f, spread));
                    ArmFlight(b, from.x > home.x);
                    if (_garden.Root != null && b.transform.parent != _garden.Root)
                        b.transform.SetParent(_garden.Root, true);
                    b.transform.position = from;
                }
            }
            var fFrom = new Vector3[2];
            var fTo = new Vector3[2];
            var fDelay = new float[2];
            if (_garden.Feeders != null)
            {
                for (int i = 0; i < 2; i++)
                {
                    if (_garden.Feeders[i] == null) continue;
                    fTo[i] = _garden.Feeders[i].transform.position;
                    fFrom[i] = fTo[i] + new Vector3(0f, 3.1f, 0f);
                    fDelay[i] = i * 0.12f;
                    _garden.Feeders[i].transform.position = fFrom[i];
                }
            }
            var bees = new List<SpriteRenderer>();
            var leaves = new List<SpriteRenderer>();
            GatherDecor(bees, leaves);
            var beeS = new Vector3[bees.Count];
            var beeD = new Vector3[bees.Count];
            var beeDelay = new float[bees.Count];
            var beeScale = new Vector3[bees.Count];
            var leafS = new Vector3[leaves.Count];
            var leafD = new Vector3[leaves.Count];
            var leafDelay = new float[leaves.Count];
            var leafRot = new Quaternion[leaves.Count];
            for (int i = 0; i < bees.Count; i++)
            {
                var b = bees[i];
                var home = b.transform.position;
                var from = Offscreen(home, b);
                from.y = home.y + Random.Range(0.25f, 0.6f);
                beeD[i] = home;
                beeS[i] = from;
                beeDelay[i] = Random.Range(0f, spread);
                beeScale[i] = b.transform.localScale;
                b.transform.position = from;
                b.flipX = from.x > home.x;
            }
            for (int i = 0; i < leaves.Count; i++)
            {
                var b = leaves[i];
                var home = b.transform.position;
                var from = Offscreen(home, b);
                from.y = home.y + Random.Range(0.15f, 0.45f);
                leafD[i] = home;
                leafS[i] = from;
                leafDelay[i] = Random.Range(0f, spread);
                leafRot[i] = b.transform.rotation;
                b.transform.position = from;
            }
            // Everything is offscreen now, so the board can show again without a flash.
            VeilBoard(false);
            if (birds.Count > 0)
                Sfx.FlockFlutter(Mathf.Max(1, birds.Count / 3));
            float maxDelay = 0f;
            for (int i = 0; i < delays.Count; i++) maxDelay = Mathf.Max(maxDelay, delays[i]);
            float t = 0f;
            float total = dur + maxDelay;
            while (t < total)
            {
                if (gen != _motionGen) yield break;
                if (GamePause.Paused) { yield return null; continue; }
                t += Time.deltaTime;
                for (int i = 0; i < birds.Count; i++)
                {
                    var b = birds[i];
                    if (b == null) continue;
                    float u = Mathf.Clamp01((t - delays[i]) / dur);
                    var p = Vector3.Lerp(starts[i], dests[i], EaseOutCubic(u));
                    p.y += Mathf.Sin(u * Mathf.PI) * 0.38f;
                    b.transform.position = p;
                    KeepFlight(b, u);
                }
                PoseBees(bees, beeS, beeD, beeDelay, beeScale, t, dur, false);
                PoseLeaves(leaves, leafS, leafD, leafDelay, leafRot, t, dur, false);
                if (_garden.Feeders != null)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        if (_garden.Feeders[i] == null) continue;
                        float fu = Mathf.Clamp01((t - fDelay[i]) / 0.92f);
                        float fk = EaseOutCubic(fu);
                        float bounce = Mathf.Sin(fu * Mathf.PI) * (1f - fu) * 0.18f;
                        var fp = Vector3.Lerp(fFrom[i], fTo[i], fk);
                        fp.y += bounce;
                        _garden.Feeders[i].transform.position = fp;
                    }
                }
                yield return null;
            }
            for (int i = 0; i < birds.Count; i++)
            {
                if (birds[i] == null) continue;
                birds[i].transform.position = dests[i];
            }
            SyncAll(true);
            StillBirds(false);
        }

        void RestorePerch()
        {
            _sel = -1;
            _won = SolvedRule.FinaleDue(_board);
            if (_garden.Branches == null) return;
            for (int i = 0; i < _garden.Branches.Length; i++)
            {
                var v = _garden.Branches[i];
                v.gameObject.SetActive(!_board.Branches[i].Broken);
                v.transform.rotation = Quaternion.identity;
            }
            SyncAll();
            StartCoroutine(GardenFit.Tween(_garden, _board, true));
        }

        int HitFeeder(Vector2 world)
        {
            if (_garden.Feeders == null) return -1;
            float best = 1.35f * 1.35f;
            int idx = -1;
            for (int i = 0; i < _garden.Feeders.Length; i++)
            {
                var f = _garden.Feeders[i];
                if (f == null || f.Art == null || !f.Art.enabled) continue;
                float d = ((Vector2)f.transform.position - world).sqrMagnitude;
                float dMouth = ((Vector2)f.Mouth - world).sqrMagnitude;
                float m = Mathf.Min(d, dMouth);
                if (f.Art.sprite != null)
                {
                    var b = f.Art.bounds;
                    if (b.Contains(new Vector3(world.x, world.y, b.center.z)))
                        m = 0f;
                }
                if (m < best) { best = m; idx = i; }
            }
            return idx;
        }

        int HitBranch(Vector2 world)
        {
            if (_garden.Branches == null) return -1;
            bool sending = _sel >= 0;
            int idx = -1;
            if (!sending)
            {
                float bestBird = 1.05f * 1.05f;
                for (int b = 0; b < _garden.Branches.Length; b++)
                {
                    var v = _garden.Branches[b];
                    if (v == null || !v.gameObject.activeInHierarchy) continue;
                    if (_board.Branches[v.Index].Broken || _board.Branches[v.Index].AdLocked) continue;
                    if (_board.Branches[v.Index].IsFullMatch(out _)) continue;
                    for (int s = 0; s < BranchState.Cap; s++)
                    {
                        if (v.Birds[s] == null || !v.Birds[s].enabled) continue;
                        if (v.Birds[s].transform.parent != v.transform) continue;
                        float d = ((Vector2)v.Birds[s].transform.position - world).sqrMagnitude;
                        if (d < bestBird) { bestBird = d; idx = v.Index; }
                    }
                }
                if (idx >= 0) return idx;
            }

            float pad = sending ? 3.25f : 2.45f;
            float best = pad * pad;
            for (int i = 0; i < _garden.Branches.Length; i++)
            {
                var v = _garden.Branches[i];
                if (v == null || !v.gameObject.activeInHierarchy) continue;
                var st = _board.Branches[v.Index];
                if (st.Broken || st.AdLocked || st.IsFullMatch(out _)) continue;
                float reach = st.Empty && sending ? 3.45f : pad;
                float d = v.NearestPadSqr(world);
                if (d < reach * reach && d < best) { best = d; idx = v.Index; }
            }
            var overlap = Physics2D.OverlapCircleAll(world, sending ? 2.6f : 1.85f);
            for (int i = 0; i < overlap.Length; i++)
            {
                var v = overlap[i].GetComponentInParent<BranchView>();
                if (v == null || _board.Branches[v.Index].Broken || _board.Branches[v.Index].AdLocked) continue;
                if (_board.Branches[v.Index].IsFullMatch(out _)) continue;
                float d = v.NearestPadSqr(world);
                if (d < best) { best = d; idx = v.Index; }
            }
            return idx;
        }

        // Resume delivers the pointer that was down when the app left. Eat it
        // once, for every IMGUI control, so a lesson cannot be tapped through.
        static void SwallowResumePointer()
        {
            if (PlayClock.Now >= _resumeInputUntil) return;
            var e = Event.current;
            if (e == null) return;
            var t = e.type;
            if (t != EventType.MouseDown && t != EventType.MouseUp && t != EventType.MouseDrag) return;
            if (t == EventType.MouseUp) GUIUtility.hotControl = 0;
            e.Use();
        }

        static bool Pressed(out Vector2 screen)
        {
            screen = default;
            if (PlayClock.Now < _resumeInputUntil) return false;
            if (LessonGuardUp()) return false;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screen = Mouse.current.position.ReadValue();
                return true;
            }
            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
            {
                screen = touch.primaryTouch.position.ReadValue();
                return true;
            }
            return false;
        }

        // Below the notch / Dynamic Island. Safe area is read every pass, so a
        // rotation or a late inset still clears the island.
        static float TopHud(float extra = 0f) => TopHudBox(Screen.height, Screen.safeArea, extra);

        // Screen-explicit so the album grid test uses the same top inset as play.
        static float TopHudBox(float screenH, Rect safe, float extra)
        {
            float s = Mathf.Max(screenH / 720f, 1f);
            float inset = (safe.width < 2f || safe.height < 2f)
                ? 0f
                : Mathf.Max(0f, screenH - safe.yMax);
            return Mathf.Max(12f, inset + 10f * s + extra);
        }

        static void HudLayout(out float scale, out float top, out float bot, out Rect restart, out Rect hive)
        {
            scale = Mathf.Max(Screen.height / 720f, 1f);
            var safe = Screen.safeArea;
            float safeTop = Screen.height - safe.yMax;
            float safeBot = safe.yMin;
            top = 72f * scale + safeTop;
            // Extra bottom inset so the wood restart arrow clears home-indicator / clipped edge.
            bot = 88f * scale + safeBot;
            float y = Screen.height - bot - 12f * scale;
            float h = 64f * scale;
            float x = Mathf.Max(40f * scale, safe.xMin + 32f * scale);
            restart = new Rect(x, y, h, h);
            float hiveS = 80f * scale;
            float hiveY = y + h - hiveS;
            hive = new Rect(
                Mathf.Min(Screen.width - hiveS - 10f, safe.xMax - hiveS - 8f),
                hiveY, hiveS, hiveS);
        }

        void SeatFeeders()
        {
            if (_splash || _garden.Feeders == null) return;
            float y = WorldBuilder.FeederY;
            for (int i = 0; i < _garden.Feeders.Length; i++)
                if (_garden.Feeders[i] != null) _garden.Feeders[i].Seat(y);
        }

        void SnapHiveToHud()
        {
            if (_splash || _garden.Hive == null) return;
            var cam = _garden.Cam != null ? _garden.Cam : Camera.main;
            if (cam == null) return;
            HudLayout(out _, out _, out _, out _, out var hive);
            _garden.Hive.TrackHud(cam, hive);
        }

        // The wood skep is the OnGUI button. This glow sits under it, so it follows the same gate.
        void SyncOrbitHive()
        {
            if (_garden.Hive == null) return;
            bool show = HiveReachable();
            var hive = _garden.Hive.transform;
            for (int i = 0; i < hive.childCount; i++)
            {
                var ch = hive.GetChild(i);
                string n = ch.name;
                if (n != "Comb" && n != "CombCore" && (n.Length < 9 || string.CompareOrdinal(n, 0, "CombGlint", 0, 9) != 0)) continue;
                var sr = ch.GetComponent<SpriteRenderer>();
                if (sr != null) sr.enabled = show;
            }
        }

        void LateUpdate()
        {
            if (GamePause.Paused)
            {
                // Pose steps on PlayClock, so a suspension cannot skip the arc.
                // Under an ad the clock holds and the hand does not jump when it lifts.
                if (TutorialGuideLive() || _tutorPause != 0 || _pokerPageOn || _pokerDealHint || PokerBackHintOn())
                    CoachPlace();
                return;
            }
#if UNITY_EDITOR
            if (!_pokerPlayrun && System.IO.File.Exists("/tmp/flock-five-poker-run"))
            {
                _dailyShotQuiet = true;
                try { System.IO.File.Delete("/tmp/flock-five-poker-run"); } catch { }
                _pokerPlayrun = true;
                StartCoroutine(PokerPlayrun());
            }
            if (System.IO.File.Exists("/tmp/flock-five-poker-faces"))
            {
                _dailyShotQuiet = true;
                try { System.IO.File.Delete("/tmp/flock-five-poker-faces"); } catch { }
                StartCoroutine(ShotPokerFaces());
            }
            if (System.IO.File.Exists("/tmp/flock-five-consumer-tour"))
            {
                _dailyShotQuiet = true;
                try { System.IO.File.Delete("/tmp/flock-five-consumer-tour"); } catch { }
                StartCoroutine(ShotConsumerTour());
            }
            if (System.IO.File.Exists("/tmp/flock-five-shot"))
            {
                _dailyShotQuiet = true;
                try { System.IO.File.Delete("/tmp/flock-five-shot"); } catch { }
                StartCoroutine(ShotHome());
            }
            if (System.IO.File.Exists("/tmp/flock-five-badger-fight"))
                EditorOpenBadgerFight();
            if (System.IO.File.Exists("/tmp/flock-five-poker-stamp"))
            {
                _dailyShotQuiet = true;
                try { System.IO.File.Delete("/tmp/flock-five-poker-stamp"); } catch { }
                StartCoroutine(ShotPokerStamp());
            }
            if (System.IO.File.Exists("/tmp/flock-five-hand-qa"))
            {
                _dailyShotQuiet = true;
                try { System.IO.File.Delete("/tmp/flock-five-hand-qa"); } catch { }
                StartCoroutine(ShotHandQa());
            }
            if (System.IO.File.Exists("/tmp/flock-five-force-five-wilds"))
            {
                try { System.IO.File.Delete("/tmp/flock-five-force-five-wilds"); } catch { }
                StartCoroutine(EditorForceFiveWilds());
            }
            if (System.IO.File.Exists("/tmp/flock-five-poker-feel"))
            {
                _dailyShotQuiet = true;
                try { System.IO.File.Delete("/tmp/flock-five-poker-feel"); } catch { }
                StartCoroutine(ShotPokerFeel());
            }
#endif
            SeatFeeders();
            SnapHiveToHud();
            SyncOrbitHive();
            TickSplashRails();
            CoachPlace();
            TickHomeAvatar();
        }

        bool HitHud(Vector2 screen)
        {
            if (LessonTapBlocked()) return false;
            HudLayout(out _, out float top, out _, out var restart, out var hive);
            float gy = Screen.height - screen.y;
            var gui = new Vector2(screen.x, gy);
            if (_hiveInspect >= 0)
                return true;
            if (_levelHive)
            {
                if (_levelHiveRect.width > 1f && _levelHiveRect.Contains(gui))
                    return true;
                CloseLevelHive();
                return true;
            }
            if (gui.y < top) return true;
            if (restart.Contains(gui))
            {
                Restart();
                return true;
            }
            if (HiveReachable() && hive.Contains(gui))
            {
                OpenLevelHive();
                return true;
            }
            HudLayout(out _, out _, out float bot, out _, out _);
            return gui.y > Screen.height - bot - 8f;
        }

        void OnGUI()
        {
            SwallowLessonGuard();
            SwallowResumePointer();
            SwallowBadgerShowPointer();
            if (GamePause.Paused)
            {
                // Card-flip tutor keeps running through a pause. An ad hides it so the
                // glove does not jump ahead on the unscaled clock while the ad is up.
                if (!Ads.IsShowing && _hiveTutorOn && _hiveInspect >= 0 && _home == HomeFace.Hive)
                    DrawHiveInspect(Mathf.Max(Screen.height / 720f, 1f));
                // The sparrow lesson freezes here and used to return before the chip,
                // so the multiplier badge vanished for the whole lesson. Paint it at
                // full alpha before the caption. An ad or a full-screen popup still
                // suppresses it inside GardenStampLive.
                if (!Ads.IsShowing && !_splash && _board != null)
                {
                    HudLayout(out float ps, out float pTop, out _, out _, out _);
                    DrawGardenStamp(ps);
                    if (TutorialGuideLive() || _tutorPause != 0)
                        DrawCoach(ps, pTop);
                    if (SeasonReveal.BannerLive)
                        DrawSeasonBanner(ps, pTop);
                }
                return;
            }
            // Shared tutorial tap gate: a step that waits on one control eats every other press.
            SwallowOffTargetTap();
            if (_splash)
            {
                // Hive, poker, and the garden do not draw the sign. Drop it here so a
                // tap away cannot leave bulbs parked on the slide clock.
                if (_home != HomeFace.Splash)
                {
                    DismissStreakSign();
                    VipOffer.Close();
                    FoldCoinFlies();
                }
                if (_home != HomeFace.Splash) _settingsOpen = false;
                if (_home == HomeFace.Hive) DrawHivePage();
                else if (_home == HomeFace.Poker) DrawPokerPage();
                else if (_home == HomeFace.Badger) DrawBadgerPage();
                else DrawSplash();
                DrawBadgerLeap();
                TickPreload();
                return;
            }
            if (_clearReward)
            {
                DrawClearReward();
                DrawBadgerLeap();
                return;
            }
            DismissStreakSign();
            VipOffer.Close();
            FoldCoinFlies();
            if (_board == null) return;
            // Veil first. The chip is drawn after it, at full alpha, so a coach
            // dim cannot grey the multiplier out.
            CoachDim();
            HudLayout(out float s, out float top, out _, out var restart, out var hive);
            var hudM = GUI.matrix;
            bool quake = CamShake.HudOffset.sqrMagnitude > 0.01f || Mathf.Abs(CamShake.HudTwist) > 0.001f;
            if (quake)
            {
                GUIUtility.RotateAroundPivot(CamShake.HudTwist, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
                GUI.matrix = Matrix4x4.Translate(new Vector3(CamShake.HudOffset.x, CamShake.HudOffset.y, 0f)) * GUI.matrix;
            }
            DrawGardenStamp(s);
            var arrow = SpriteCatalog.Restart;
            if (arrow != null && arrow.texture != null)
                GUI.DrawTexture(restart, arrow.texture, ScaleMode.ScaleToFit, true);
            else
                GUI.Box(restart, "↩");
            if (HiveReachable())
                DrawHiveButton(hive, s, orbit: true, pop: true);
            else
            {
                if (_levelHive) CloseLevelHive();
                if (_hiveInspect >= 0 || _albumTutorOn)
                    CloseHiveInspect();
            }
            if (quake) GUI.matrix = hudM;
            if (_levelHive) DrawLevelHive(s);
            if (_hiveInspect >= 0) DrawHiveInspect(s);
            DrawGiftSign(s);
            DrawCoach(s, top);
            DrawSeasonBanner(s, top);
            if (_gift != GiftFace.None) DrawGiftOffer(s);
            if (_restartAsk != RestartAsk.None) DrawRestartAsk(s);
            DrawBadgerLeap();
            TickPreload();
        }

        // Original splash hive size — pig matches this, then both bump together.
        static float SplashRailAnchor() =>
            Mathf.Clamp(Screen.width * 0.11f, 52f, 108f);

        // One scale for pig, hive, poker, VIP, and the daily gift, about 15% over the previous rail.
        const float SplashRailScale = 1.15f;

        static float SplashRailSize() => SplashRailAnchor() * 1.22f * SplashRailScale;

        static float SplashRailGap() => Mathf.Max(24f, SplashRailSize() * 0.30f);

        // Right rail, top to bottom: pig, hive, poker. Left: daily, then VIP.
        // Both pack from their top. A hidden button takes no slot and the rest slide up.
        void TickSplashRails()
        {
            if (LevelData.NextPlay < 1) _dailyRailLocked = true;
            bool face = _splash && _home == HomeFace.Splash;
            if (_railInit && !face) return;
            float hive = SplashHiveGoal();
            float vip = VipRailGoal();
            float poker = SplashPokerGoal();
            float daily = SplashDailyGoal();
            float now = Time.unscaledTime;
            if (!_railInit)
            {
                _railInit = true;
                for (int i = 0; i < RailCount; i++)
                {
                    float g = RailGoal(i, hive, vip, poker, daily);
                    _railK[i] = g;
                    _railFrom[i] = g;
                    _railGoal[i] = g;
                    _railT0[i] = now - SplashRailSlide;
                }
                return;
            }
            for (int i = 0; i < RailCount; i++)
            {
                float g = RailGoal(i, hive, vip, poker, daily);
                if (_railGoal[i] != g)
                {
                    _railFrom[i] = _railK[i];
                    _railGoal[i] = g;
                    _railT0[i] = now;
                }
                float u = Mathf.Clamp01((now - _railT0[i]) / SplashRailSlide);
                float e = 1f - (1f - u) * (1f - u) * (1f - u);
                _railK[i] = Mathf.Lerp(_railFrom[i], g, e);
            }
        }

        // Splash face only. The in-garden skep can show before the lesson; this rail does not.
        float SplashHiveGoal()
        {
            if (_splash) return SplashHiveShown() ? 1f : 0f;
            if (_hiveIntro && !_hiveIntroLive) return 0f;
            if (_hiveIntroLive) return 1f;
            return PlayerPrefs.GetInt(CoachHiveKey, 0) != 0 ? 1f : 0f;
        }

        // Build 58: owning VIP keeps the slot. The offer medallion becomes the diamond
        // badge (DrawNoAdsButton), so restore and reinstall show it too.
        static float VipRailGoal()
        {
            return (NoAds.Owned || LevelData.NextPlay >= 1) ? 1f : 0f;
        }

        // Hidden through level 1. The lesson keeps it out of the stack until the
        // line is up, then the same presence slide the hive uses grows it in.
        float SplashPokerGoal()
        {
            if (LevelData.NextPlay < 1) return 0f;
            if (_pokerIntro && !_pokerIntroLive) return 0f;
            return 1f;
        }

        // Same gate as poker. A player who was still on level 1 this session waits
        // for the lesson, then the button grows into the gap. Someone who already
        // cleared it is full size on the first sample and still gets the glove once.
        float SplashDailyGoal()
        {
            if (LevelData.NextPlay < 1) return 0f;
            if (_dailyRailLocked && _dailyIntro && !_dailyIntroLive) return 0f;
            return 1f;
        }

        static float RailGoal(int id, float hive, float vip, float poker, float daily)
        {
            if (id == RailHive) return hive;
            if (id == RailVip) return vip;
            if (id == RailPoker) return poker;
            if (id == RailDaily) return daily;
            return 1f;
        }

        void EnsureSplashRails()
        {
            if (!_railInit) TickSplashRails();
            ApplySplashRails();
        }

        void ApplySplashRails()
        {
            float size = SplashRailSize();
            float gap = SplashRailGap();
            var right = HomeRailRect(true, size);
            var left = HomeRailRect(false, size);
            float top = right.y - size - gap;
            float rightCx = right.center.x;
            float leftCx = left.center.x;
            for (int pass = 0; pass < 2; pass++)
            {
                bool visual = pass == 0;
                float y = top;
                PackRail(ref y, rightCx, gap, RailPig, size, visual);
                PackRail(ref y, rightCx, gap, RailHive, size, visual);
                PackRail(ref y, rightCx, gap, RailPoker, size, visual);
                y = top;
                PackRail(ref y, leftCx, gap, RailDaily, size, visual);
                PackRail(ref y, leftCx, gap, RailVip, size, visual);
            }
        }

        // Presence scales the button and the gap under it, so neighbors slide
        // and the new icon grows in the opening. At rest the gap is SplashRailGap.
        void PackRail(ref float y, float cx, float gap, int id, float size, bool visual)
        {
            float presence = visual ? _railK[id] : _railGoal[id];
            if (presence <= 0.001f)
            {
                var gone = new Rect(cx, y, 0f, 0f);
                if (visual) _railRect[id] = gone;
                else _railSeat[id] = gone;
                return;
            }
            float d = size * presence;
            var r = new Rect(cx - d * 0.5f, y, d, d);
            if (visual) _railRect[id] = r;
            else _railSeat[id] = r;
            y += d + gap * presence;
        }

        bool RailLive(int id) => _railK[id] > 0.012f;

        bool RailSettled(int id) => Mathf.Abs(_railK[id] - _railGoal[id]) <= 0.02f;

        Rect SplashRailSeat(int id)
        {
            EnsureSplashRails();
            return _railSeat[id];
        }

        Rect SplashHiveRect()
        {
            EnsureSplashRails();
            return _railRect[RailHive];
        }

        Rect SplashShareRect()
        {
            float size = SplashRailSize() * 0.70f;
            var hive = SplashRailSeat(RailHive);
            var left = HomeRailRect(false, size);
            float y = hive.height > 1f ? hive.center.y - size * 0.5f : HomeRailRect(true, SplashRailSize()).y;
            return new Rect(left.x, y, size, size);
        }

        Rect SplashNoAdsRect()
        {
            EnsureSplashRails();
            return _railRect[RailVip];
        }

        // "No Ads" nameplate just under the disc. Narrower than the medallion.
        static Rect SplashNoAdsRibbon(Rect medal)
        {
            float d = medal.width;
            float w = d * 0.92f;
            float h = d * 0.28f;
            float y = medal.yMax + d * 0.02f;
            return new Rect(medal.center.x - w * 0.5f, y, w, h);
        }

        Rect SplashPokerRect()
        {
            EnsureSplashRails();
            return _railRect[RailPoker];
        }

        Rect SplashDailyRect()
        {
            EnsureSplashRails();
            return _railRect[RailDaily];
        }

        // THE tap rect for a rail button (shared by the hit-test and the glove's tap check).
        // It is the drawn rect, padded a little, and for the Daily medal it also covers the
        // streak flame that hangs above the plate, so a tap on anything the player sees as
        // "the button" opens it. The pad never reaches into a neighbor: below it stops short
        // of half the rail gap, and the rail is a single column so nothing sits beside it.
        Rect RailTapRect(int id)
        {
            EnsureSplashRails();
            var r = _railRect[id];
            if (r.width < 2f || r.height < 2f) return r;
            float pad = r.width * 0.06f;
            float x0 = r.x - pad;
            float y0 = r.y - pad;
            float x1 = r.xMax + pad;
            float y1 = r.yMax + Mathf.Min(pad, SplashRailGap() * 0.25f);
            if (id == RailDaily)
            {
                DailyBonus.Boot();
                if (DailyBonus.IconState == DailyBonusIconState.Flame)
                {
                    var fl = DailyFlameRect(r);
                    x0 = Mathf.Min(x0, fl.x);
                    y0 = Mathf.Min(y0, fl.y);
                    x1 = Mathf.Max(x1, fl.xMax);
                }
            }
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }

        Rect SplashDailyTapRect() => RailTapRect(RailDaily);

        Rect PiggyRect(float s)
        {
            EnsureSplashRails();
            return _railRect[RailPig];
        }

        static void DrawRailIcon(Rect r, Sprite spr)
        {
            if (spr == null || spr.texture == null) return;
            GUI.color = new Color(0.08f, 0.05f, 0.02f, 0.32f);
            GUI.DrawTexture(new Rect(r.x + 3f, r.y + 6f, r.width, r.height), spr.texture, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
            GUI.DrawTexture(r, spr.texture, ScaleMode.ScaleToFit, true);
        }

        Rect HivePopRect(Rect hive, bool pop)
        {
            if (!(pop && _hivePopping)) return hive;
            float k = Mathf.Max(0.35f, _hivePopMul);
            float cx = hive.center.x, cy = hive.center.y;
            return new Rect(cx - hive.width * 0.5f * k, cy - hive.height * 0.5f * k,
                hive.width * k, hive.height * k);
        }

        void DrawHiveButton(Rect hive, float s, bool orbit = false, bool pop = false, bool quiet = false)
        {
            hive = HivePopRect(hive, pop);
            float pulse = quiet || (pop && _hivePopping) ? 0f : HiveView.GuiPulse;
            bool freshCue = _home == HomeFace.Splash && _hiveFreshSlots.Count > 0 && !quiet && !(pop && _hivePopping);
            if (freshCue)
                pulse = Mathf.Max(pulse, 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 5.2f));
            Rect draw = hive;
            if (pulse > 0.01f)
            {
                float k = 1f + (freshCue ? 0.08f : 0.10f) * pulse;
                float cx = hive.center.x, cy = hive.center.y;
                draw = new Rect(cx - hive.width * 0.5f * k, cy - hive.height * 0.5f * k,
                    hive.width * k, hive.height * k);
            }
            DrawHiveGlow(draw, freshCue ? 0.34f + 0.24f * Mathf.Clamp01(pulse) : 0.22f);
            if (orbit) DrawHiveHalo(draw, s, true);
            var spr = SpriteCatalog.Hive;
            if (spr != null && spr.texture != null)
            {
                GUI.color = new Color(0.08f, 0.05f, 0.02f, 0.32f);
                GUI.DrawTexture(new Rect(draw.x + 3f, draw.y + 6f, draw.width, draw.height), spr.texture, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
                GUI.DrawTexture(draw, spr.texture, ScaleMode.ScaleToFit, true);
            }
            else
                GUI.Box(draw, "Hive");
            DrawHiveSparkles(draw);
            if (!HiveOpenedSeen()) DrawHiveNewDot(draw);
            if (orbit) DrawHiveHalo(draw, s, false);
        }

        static readonly float[] HiveGlintX = { -0.22f, 0.26f, 0.04f, -0.28f, 0.18f };
        static readonly float[] HiveGlintY = { -0.26f, -0.08f, 0.28f, 0.10f, 0.22f };
        static readonly float[] HiveGlintPh = { 0.4f, 1.7f, 2.9f, 4.1f, 5.3f };

        static void DrawHiveGlow(Rect hive, float alpha)
        {
            var glow = GlowTex();
            if (glow == null || alpha < 0.02f) return;
            float g = hive.width * 0.18f;
            GUI.color = new Color(1f, 0.78f, 0.28f, alpha);
            GUI.DrawTexture(
                new Rect(hive.center.x - hive.width * 0.5f - g, hive.center.y - hive.height * 0.5f - g, hive.width + g * 2f, hive.height + g * 2f),
                glow, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
        }

        static void DrawHiveSparkles(Rect hive)
        {
            var spr = SpriteCatalog.Sparkle;
            var tex = spr != null && spr.texture != null ? spr.texture : GlowTex();
            if (tex == null) return;
            float t = Time.unscaledTime;
            for (int i = 0; i < HiveGlintX.Length; i++)
            {
                float tw = 0.5f + 0.5f * Mathf.Sin(t * (1.6f + i * 0.11f) + HiveGlintPh[i]);
                tw = tw * tw;
                if (tw < 0.12f) continue;
                float sz = hive.width * (0.14f + 0.04f * (i & 1));
                float x = hive.center.x + HiveGlintX[i] * hive.width * 0.72f - sz * 0.5f;
                float y = hive.center.y + HiveGlintY[i] * hive.height * 0.72f - sz * 0.5f;
                GUI.color = new Color(1f, 0.94f, 0.62f, 0.25f + 0.70f * tw);
                GUI.DrawTexture(new Rect(x, y, sz, sz), tex, ScaleMode.ScaleToFit, true);
            }
            GUI.color = Color.white;
        }

        static void DrawHiveNewDot(Rect plate)
        {
            float cx = plate.xMax - plate.width * 0.18f;
            float cy = plate.y + plate.width * 0.16f;
            DrawReadyBadge(cx, cy, plate.width);
        }

        float DrawHiveTally(Rect anchor, float s)
        {
            int have = Hive.Found;
            int cap = Mathf.Max(1, Hive.AlbumSlots);
            float u = Mathf.Clamp01(have / (float)cap);
            TallyMetrics(s, out float labelH, out float barH, out int floor, out int hi);
            string label = "Cards " + have + " / " + cap;
            var st = GuiPool.Label(GuiSlot.HiveTally);
            st.fontStyle = FontStyle.Bold;
            st.alignment = TextAnchor.MiddleCenter;
            st.wordWrap = false;
            var labelR = new Rect(anchor.x, anchor.y, anchor.width, labelH);
            st.fontSize = FitFont(st, label, labelR.width * 0.98f, labelR.height * 0.92f, floor, hi);
            StampReadable(labelR, label, st, AlbumInk);

            var plate = new Rect(anchor.x, labelR.yMax + 4f * s, anchor.width, barH);
            GUI.color = new Color(0.08f, 0.05f, 0.02f, 0.84f);
            GUI.DrawTexture(plate, Texture2D.whiteTexture);
            float inset = Mathf.Max(2f, 2.5f * s);
            var track = new Rect(plate.x + inset, plate.y + inset, Mathf.Max(0f, plate.width - inset * 2f), Mathf.Max(0f, plate.height - inset * 2f));
            GUI.color = new Color(0.24f, 0.17f, 0.10f, 0.95f);
            GUI.DrawTexture(track, Texture2D.whiteTexture);
            if (u > 0f)
            {
                GUI.color = new Color(0.90f, 0.66f, 0.18f, 0.95f);
                GUI.DrawTexture(new Rect(track.x, track.y, track.width * u, track.height), Texture2D.whiteTexture);
            }
            GUI.color = new Color(1f, 0.82f, 0.28f, 0.70f);
            float t = Mathf.Max(1.5f, 2f * s);
            GUI.DrawTexture(new Rect(plate.x, plate.y, plate.width, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(plate.x, plate.yMax - t, plate.width, t), Texture2D.whiteTexture);
            GUI.color = Color.white;
            return TallyBottom(anchor.y - 4f * s, s);
        }

        // Shared with CardText.ShellBottom. labelH is the point size plus 8, not the measured glyphs.
        static void TallyMetrics(float s, out float labelH, out float barH, out int floor, out int hi)
        {
            floor = Mathf.Max(16, Mathf.RoundToInt(17f * s));
            hi = Mathf.Max(floor + 2, Mathf.RoundToInt(24f * s));
            labelH = hi + 8f;
            barH = Mathf.Clamp(12f * s, 10f, 18f * s);
        }

        static float TallyBottom(float textBottom, float s)
        {
            TallyMetrics(s, out float labelH, out float barH, out _, out _);
            return textBottom + 4f * s + labelH + 4f * s + barH;
        }

        void ArmIncomingHalo(BeeVisit visit)
        {
            var tint = visit.Kind.Tint;
            if (visit.Finish != BeeFinish.Normal)
                tint = Color.Lerp(tint, Color.white, visit.Finish == BeeFinish.Holo ? 0.18f : 0.28f);
            float now = Time.unscaledTime;
            if (!_haloEbbReady)
            {
                _haloEbbPhase = Random.Range(0f, Mathf.PI * 2f);
                _haloEbbReady = true;
            }
            // Cap stays 12. Drop the oldest before the newcomer so the ring never holds 13.
            if (_incomingHalo.Count >= HaloCap) _incomingHalo.RemoveAt(0);
            float slot = HaloGapSlot(now);
            _incomingHalo.Add(new HiveHaloBee
            {
                SlotFrom = slot,
                SlotTo = slot,
                SlotStart = now,
                RadiusPhase = Random.Range(0f, Mathf.PI * 2f),
                RadiusOmega = Random.Range(0.8f, 1.8f),
                BobPhase = Random.Range(0f, Mathf.PI * 2f),
                BobOmega = Random.Range(0.9f, 1.7f),
                BobGain = Random.Range(0.65f, 1f),
                Loop = Random.Range(0f, 2.999f),
                LoopAmp = Random.Range(0.68f, 1f),
                Dart = Random.Range(0f, Mathf.PI * 2f),
                Tint = tint,
                // Collected bees keep circling the hive for the rest of the stage
                // (cleared on stage load). Appear waits until the respace has settled.
                Appear = now + 0.32f,
                Expire = float.MaxValue
            });
            RespaceHalo(now);
        }

        void PruneIncomingHalo()
        {
            float now = Time.unscaledTime;
            bool removed = false;
            for (int i = _incomingHalo.Count - 1; i >= 0; i--)
            {
                if (now < _incomingHalo[i].Expire) continue;
                _incomingHalo.RemoveAt(i);
                removed = true;
            }
            if (removed) RespaceHalo(now);
        }

        float HaloSlotNow(HiveHaloBee b, float now)
        {
            float u = Mathf.Clamp01((now - b.SlotStart) / HaloRespaceSec);
            u = u * u * (3f - 2f * u);
            return Mathf.Lerp(b.SlotFrom, b.SlotTo, u);
        }

        // Writes each bee's slot into _haloGap and a circular order into _haloOrder.
        int HaloSorted(float now, bool visibleOnly)
        {
            int n = _incomingHalo.Count;
            if (n > HaloCap) n = HaloCap;
            int w = 0;
            for (int i = 0; i < n; i++)
            {
                var b = _incomingHalo[i];
                if (visibleOnly && (now < b.Appear || now >= b.Expire)) continue;
                _haloOrder[w] = i;
                _haloGap[i] = Mathf.Repeat(HaloSlotNow(b, now), Mathf.PI * 2f);
                w++;
            }
            for (int i = 1; i < w; i++)
            {
                int key = _haloOrder[i];
                float ka = _haloGap[key];
                int j = i - 1;
                while (j >= 0 && _haloGap[_haloOrder[j]] > ka)
                {
                    _haloOrder[j + 1] = _haloOrder[j];
                    j--;
                }
                _haloOrder[j + 1] = key;
            }
            return w;
        }


        float HaloGapSlot(float now)
        {
            int n = HaloSorted(now, false);
            if (n <= 0) return Random.Range(0f, Mathf.PI * 2f);
            if (n == 1) return Mathf.Repeat(_haloGap[_haloOrder[0]] + Mathf.PI, Mathf.PI * 2f);
            int after = 0;
            float biggest = -1f;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                float a = _haloGap[_haloOrder[i]];
                float b = _haloGap[_haloOrder[j]];
                float gap = (j == 0 ? b + Mathf.PI * 2f : b) - a;
                if (gap > biggest)
                {
                    biggest = gap;
                    after = j;
                }
            }
            float left = _haloGap[_haloOrder[(after - 1 + n) % n]];
            return Mathf.Repeat(left + biggest * 0.5f, Mathf.PI * 2f);
        }

        // Even slots in current circular order. Linear lerp cannot cross.
        void RespaceHalo(float now)
        {
            int n = HaloSorted(now, false);
            if (n <= 0) return;
            if (n == 1)
            {
                int idx = _haloOrder[0];
                var only = _incomingHalo[idx];
                float cur = _haloGap[idx];
                only.SlotFrom = cur;
                only.SlotTo = cur;
                only.SlotStart = now;
                _incomingHalo[idx] = only;
                return;
            }
            int after = 0;
            float biggest = -1f;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                float a = _haloGap[_haloOrder[i]];
                float b = _haloGap[_haloOrder[j]];
                float gap = (j == 0 ? b + Mathf.PI * 2f : b) - a;
                if (gap > biggest)
                {
                    biggest = gap;
                    after = j;
                }
            }
            float prev = _haloGap[_haloOrder[after]];
            _haloUnwrapped[0] = prev;
            for (int k = 1; k < n; k++)
            {
                float a = _haloGap[_haloOrder[(after + k) % n]];
                while (a + 0.001f < prev) a += Mathf.PI * 2f;
                _haloUnwrapped[k] = a;
                prev = a;
            }
            float step = Mathf.PI * 2f / n;
            float mean = 0f;
            for (int k = 0; k < n; k++) mean += _haloUnwrapped[k] - k * step;
            mean /= n;
            float settle = now + HaloRespaceSec + 0.06f;
            for (int k = 0; k < n; k++)
            {
                int idx = _haloOrder[(after + k) % n];
                var bee = _incomingHalo[idx];
                bee.SlotFrom = _haloUnwrapped[k];
                bee.SlotTo = mean + k * step;
                bee.SlotStart = now;
                // Stay hidden until this ease finishes so a newcomer never draws in the gap.
                if (bee.Appear > now) bee.Appear = Mathf.Max(bee.Appear, settle);
                _incomingHalo[idx] = bee;
            }
        }

        void DrawHiveHalo(Rect hive, float s, bool behind)
        {
            if (behind) PruneIncomingHalo();
            if (_incomingHalo.Count == 0) return;
            float now = Time.unscaledTime;
            // Shared spin: every bee advances by the same angle, so slots stay even.
            if (behind)
            {
                float ebb = 1f + HaloEbbAmp * Mathf.Sin(now * HaloEbbOmega + _haloEbbPhase);
                _haloSpin = Mathf.Repeat(_haloSpin + HaloOrbitSpeed * ebb * Time.unscaledDeltaTime, Mathf.PI * 2f);
            }
            // Tight neighborhood around the skep (bottom right). The old lap grew
            // until twelve icons fit and the red/yellow paths reached screen center.
            Vector2 c = hive.center;
            float leftLim = Mathf.Max(c.x - hive.width * 0.62f, Screen.width * 0.62f);
            float rightLim = Screen.width - 4f;
            float topLim = Mathf.Max(4f, hive.y - hive.height * 0.45f);
            float botLim = Mathf.Min(Screen.height - 4f, hive.yMax + hive.height * 0.15f);
            float rx = Mathf.Min(hive.width * 0.36f, Mathf.Max(8f, Mathf.Min(c.x - leftLim, rightLim - c.x)));
            float ry = Mathf.Min(hive.height * 0.32f, Mathf.Max(8f, Mathf.Min(c.y - topLim, botLim - c.y)));
            float icon = Mathf.Clamp(hive.width * 0.19f, 8f * s, hive.width * 0.20f);

            var prev = GUI.color;
            for (int i = 0; i < _incomingHalo.Count; i++)
            {
                var b = _incomingHalo[i];
                if (now < b.Appear || now >= b.Expire) continue;
                float ang = Mathf.Repeat(_haloSpin + HaloSlotNow(b, now), Mathf.PI * 2f);
                float amp = Mathf.Clamp(b.LoopAmp, 0.68f, 1f);
                int shape = (int)b.Loop;
                float depth;
                float x;
                float y;
                if (shape == 1)
                {
                    float s2 = Mathf.Sin(ang);
                    float c2 = Mathf.Cos(ang);
                    x = c.x + c2 * rx * amp;
                    y = c.y + s2 * c2 * ry * 1.05f * amp;
                    depth = s2;
                }
                else if (shape == 2)
                {
                    float kick = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(ang + b.Dart)), 6f);
                    x = c.x + Mathf.Cos(ang) * rx * amp * (0.72f + 0.22f * kick);
                    y = c.y + Mathf.Sin(ang) * ry * amp * (0.78f + 0.12f * kick);
                    depth = Mathf.Sin(ang);
                }
                else
                {
                    float rk = 1f + HaloRadiusWobble * Mathf.Sin(now * b.RadiusOmega + b.RadiusPhase);
                    x = c.x + Mathf.Cos(ang) * rx * amp * rk;
                    y = c.y + Mathf.Sin(ang * 0.97f + b.Dart * 0.15f) * ry * amp * rk;
                    depth = Mathf.Sin(ang);
                }
                x += Mathf.Sin(now * (3.1f + amp) + b.Dart) * rx * 0.07f;
                y += Mathf.Cos(now * (2.4f + b.BobOmega) + b.RadiusPhase) * ry * 0.06f;
                y += Mathf.Sin(now * b.BobOmega + b.BobPhase) * (ry * 0.08f * b.BobGain);
                if (x < leftLim) x = leftLim;
                if (x > rightLim) x = rightLim;
                if (y < topLim) y = topLim;
                if (y > botLim) y = botLim;
                bool isBehind = depth < 0f;
                if (behind != isBehind) continue;
                float life = b.Expire == float.MaxValue ? Mathf.Clamp01((now - b.Appear) / 0.22f) : Mathf.Clamp01((b.Expire - now) / 0.22f);
                float scale = (isBehind ? 0.86f : 1.06f) * Mathf.Lerp(0.55f, 1f, life);
                float iw = icon * scale;
                var spr = SpriteCatalog.BeeFrame(Time.unscaledTime * 14f + i * 2.4f);
                if (spr == null || spr.texture == null) continue;
                var r = new Rect(x - iw * 0.5f, y - iw * 0.5f, iw, iw);
                var tint = b.Tint;
                float dim = isBehind ? 0.78f : 1f;
                GUI.color = new Color(tint.r * dim, tint.g * dim, tint.b * dim,
                    (isBehind ? 0.82f : 0.96f) * life);
                // Positive spin moves left along the lower half (GUI y grows downward).
                bool faceLeft = Mathf.Sin(ang) > 0f;
                if (faceLeft)
                {
                    var m = GUI.matrix;
                    GUIUtility.ScaleAroundPivot(new Vector2(-1f, 1f), r.center);
                    GUI.DrawTexture(r, spr.texture, ScaleMode.ScaleToFit, true);
                    GUI.matrix = m;
                }
                else
                    GUI.DrawTexture(r, spr.texture, ScaleMode.ScaleToFit, true);
            }
            GUI.color = prev;
        }

        void DrawShareButton(Rect r, bool held, float s)
        {
            float sink = held ? r.height * 0.03f : 0f;
            var disc = new Rect(r.x, r.y + sink, r.width, r.height);
            var glow = GlowTex();
            GUI.color = new Color(1f, 0.82f, 0.28f, held ? 0.34f : 0.18f);
            GUI.DrawTexture(new Rect(disc.x - 8f, disc.y - 8f, disc.width + 16f, disc.height + 16f), glow, ScaleMode.ScaleToFit, true);
            GUI.color = new Color(0.18f, 0.10f, 0.04f, held ? 0.88f : 0.72f);
            GUI.DrawTexture(disc, Texture2D.whiteTexture);
            GUI.color = new Color(1f, 0.84f, 0.32f, 0.85f);
            float t = 4f * s;
            GUI.DrawTexture(new Rect(disc.x + t, disc.y + t, disc.width - t * 2f, 3f * s), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(disc.x + t, disc.yMax - t - 3f * s, disc.width - t * 2f, 3f * s), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(disc.x + t, disc.y + t, 3f * s, disc.height - t * 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(disc.xMax - t - 3f * s, disc.y + t, 3f * s, disc.height - t * 2f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var st = GuiPool.Label(GuiSlot.ShareButton);
            st.fontStyle = FontStyle.Bold;
            st.alignment = TextAnchor.MiddleCenter;
            st.wordWrap = true;
            string lab = "Invite";
            st.fontSize = FitFont(st, lab, disc.width * 0.78f, disc.height * 0.55f, 14, 28);
            StampOutlined(disc, lab, st, new Color(1f, 0.92f, 0.62f), 1, Mathf.Max(2, Mathf.RoundToInt(st.fontSize * 0.08f)));
        }

        // Square bake of the round face. Scaled to SplashNoAdsRect at draw time.
        const int VipPlateN = 256;
        const int VipStuds = 12;
        const int VipCrownW = 192;
        const int VipCrownH = 130;
        // Cut-stone bake: half-width / half-height as a fraction of the texture.
        // 0.48 leaves a few pixels of padding so the points stay inside the bake.
        const float VipDiamondHx = 0.48f;
        const float VipDiamondHy = 0.48f;
        static Texture2D _vipPlate;
        static Texture2D _vipGem;
        static Texture2D _vipDiamond;
        static Texture2D _vipCrown;
        static Texture2D _vipGlint;
        static Texture2D _vipRibbon;
        static Texture2D _backMedal;
        const int BackMedalRev = 3;
        static int _backMedalBuilt;

        // Rail VIP spot. Not owned: the offer medallion (crown, "VIP" on the disc, "No Ads"
        // ribbon). Owned: the member badge (big diamond hero, small crown behind it,
        // "VIP" ribbon, twinkles). Same rect either way.
        // Tap opens VipOffer either way (offer card or the member thank-you card).
        void DrawNoAdsButton(Rect r, float s, bool held)
        {
            DrawVipMedal(r, s, held, NoAds.Owned, true);
        }

        // Shared VIP medallion. Offer (member=false): crown, "VIP" on the disc, "No Ads"
        // ribbon. Owned (member=true): DrawVipDiamond is the hero of the disc, a small
        // crown sits behind the stone, ribbon reads "VIP". Used by the rail button, the
        // member crest on the thank-you card and the welcome badge that lands on the rail.
        // ribbon=false drops the nameplate (the crest sits on the card and must not cover
        // the panel).
        static void DrawVipMedal(Rect r, float s, bool held, bool member, bool ribbon)
        {
            if (r.width < 4f || r.height < 4f) return;
            float sink = held ? r.height * 0.045f : 0f;
            var plate = new Rect(r.x, r.y + sink, r.width, r.height);
            var face = VipPlateTex();
            var glow = GlowTex();
            float d = plate.width;

            float breathe = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 1.7f);
            float bloom = d * (0.07f + 0.025f * breathe) * (member ? 1.3f : 1f);
            float bloomA = (held ? 0.14f : 0.28f) * (0.75f + 0.25f * breathe) * (member ? 1.35f : 1f);
            GUI.color = new Color(1f, 0.78f, 0.28f, bloomA);
            GUI.DrawTexture(new Rect(plate.x - bloom, plate.y - bloom, d + bloom * 2f, d + bloom * 2f), glow, ScaleMode.ScaleToFit, true);

            GUI.color = new Color(0.05f, 0.02f, 0.02f, held ? 0.26f : 0.44f);
            GUI.DrawTexture(new Rect(plate.x + d * 0.03f, plate.y + d * 0.055f, d, d), face, ScaleMode.ScaleToFit, true);

            GUI.color = held ? new Color(0.86f, 0.84f, 0.78f, 1f) : Color.white;
            GUI.DrawTexture(plate, face, ScaleMode.ScaleToFit, true);
            DrawVipShimmer(plate, held);
            DrawVipStuds(plate);

            Rect crown;
            if (member)
            {
                // Owned: diamond first. A small crown caps the stone; its base tucks
                // behind the gem so the peaks read and the jewel stays the hero.
                float cw = d * 0.40f;
                float ch = cw * (VipCrownH / (float)VipCrownW);
                crown = new Rect(plate.center.x - cw * 0.5f, plate.y + d * 0.006f, cw, ch);
                GUI.color = new Color(0.10f, 0.05f, 0.02f, held ? 0.16f : 0.30f);
                GUI.DrawTexture(new Rect(crown.x + d * 0.008f, crown.y + d * 0.010f, cw, ch), VipCrownTex(), ScaleMode.ScaleToFit, true);
                GUI.color = held ? new Color(0.92f, 0.90f, 0.84f, 1f) : Color.white;
                GUI.DrawTexture(crown, VipCrownTex(), ScaleMode.ScaleToFit, true);

                // Points sit at VipDiamondHy of the half-bake. 0.86 of the disc,
                // set low so the crown tips stay above the stone and the bottom
                // point stays off the ribbon.
                float fill = VipDiamondHy / 0.5f;
                float gem = d * (0.86f / fill);
                float inset = (1f - fill) * 0.5f;
                float gemTop = plate.y + d * 0.13f - gem * inset;
                var stone = new Rect(plate.center.x - gem * 0.5f, gemTop, gem, gem);
                DrawVipDiamond(stone, held);
            }
            else
            {
                float cw = d * 0.52f;
                float ch = cw * (VipCrownH / (float)VipCrownW);
                float crownTop = d * 0.155f;
                crown = new Rect(plate.center.x - cw * 0.5f, plate.y + crownTop, cw, ch);
                GUI.color = held ? new Color(0.92f, 0.90f, 0.84f, 1f) : Color.white;
                GUI.DrawTexture(crown, VipCrownTex(), ScaleMode.ScaleToFit, true);

                var vipSt = GuiPool.Label(GuiSlot.NoAdsVip);
                vipSt.fontStyle = FontStyle.Bold;
                vipSt.alignment = TextAnchor.MiddleCenter;
                vipSt.wordWrap = false;
                float vipTop = crownTop + ch + d * 0.018f;
                float vipH = Mathf.Max(d * 0.18f, d * 0.80f - vipTop);
                var vipR = new Rect(plate.center.x - d * 0.36f, plate.y + vipTop, d * 0.72f, vipH);
                int vipHi = Mathf.Max(22, Mathf.RoundToInt(d * 0.40f));
                vipSt.fontSize = FitFont(vipSt, "VIP", vipR.width * 0.94f, vipR.height * 0.90f, 13, vipHi);
                int vipDark = Mathf.Clamp(Mathf.RoundToInt(vipSt.fontSize * 0.14f), 2, 8);
                StampOutlined(vipR, "VIP", vipSt, new Color(1f, 0.95f, 0.62f, 1f), 1, vipDark);
            }

            if (ribbon) DrawVipRibbon(plate, held, s, member ? "VIP" : "No Ads");
            DrawVipGlints(plate, crown, held);
            if (member && !held)
            {
                float g = d * 0.30f;
                DrawVipTwinkles(new Rect(plate.x - g, plate.y - g * 0.6f, d + g * 2f, d + g * 1.6f), 0.80f, 0.11f);
            }
            GUI.color = Color.white;
        }

        // Faceted ownership diamond. Shared by every member=true DrawVipMedal
        // (rail badge, welcome landing, thank-you crest). Not the round rim stud.
        static void DrawVipDiamond(Rect r, bool held)
        {
            var tex = VipDiamondTex();
            GUI.color = held ? new Color(0.90f, 0.88f, 0.82f, 1f) : Color.white;
            GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit, true);
            if (!held)
            {
                float tw = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.3f);
                tw *= tw;
                if (tw > 0.08f)
                {
                    float g = r.width * 0.20f;
                    GUI.color = new Color(1f, 0.98f, 0.90f, 0.88f * tw);
                    GUI.DrawTexture(new Rect(r.x + r.width * 0.28f, r.y + r.height * 0.20f, g, g), VipGlintTex(), ScaleMode.ScaleToFit, true);
                }
            }
            GUI.color = Color.white;
        }

        static void DrawVipRibbon(Rect plate, bool held, float s, string label)
        {
            var ribbon = SplashNoAdsRibbon(plate);
            var tex = VipRibbonTex();
            GUI.color = new Color(0.05f, 0.02f, 0.02f, held ? 0.24f : 0.42f);
            GUI.DrawTexture(new Rect(ribbon.x + 1.5f, ribbon.y + 3f, ribbon.width, ribbon.height), tex, ScaleMode.StretchToFill, true);
            GUI.color = held ? new Color(0.88f, 0.86f, 0.82f, 1f) : Color.white;
            GUI.DrawTexture(ribbon, tex, ScaleMode.StretchToFill, true);

            var st = GuiPool.Label(GuiSlot.VipRibbon);
            st.fontStyle = FontStyle.Bold;
            st.alignment = TextAnchor.MiddleCenter;
            st.wordWrap = false;
            float padX = ribbon.width * 0.16f;
            var subR = new Rect(ribbon.x + padX, ribbon.y, ribbon.width - padX * 2f, ribbon.height);
            int hi = Mathf.Max(11, Mathf.RoundToInt(ribbon.height * 0.70f));
            int lo = Mathf.Min(hi, s >= 1f ? 10 : 8);
            st.fontSize = FitFont(st, label, subR.width * 0.98f, subR.height * 0.86f, lo, hi);
            int dark = Mathf.Clamp(Mathf.RoundToInt(st.fontSize * 0.18f), 1, 5);
            var ink = label == "VIP" ? new Color(1f, 0.92f, 0.52f, 1f) : new Color(1f, 0.97f, 0.88f, 1f);
            StampOutlined(subR, label, st, ink, 1, dark);
            GUI.color = Color.white;
        }

        static void DrawVipShimmer(Rect plate, bool held)
        {
            const float period = 4.8f;
            const float dur = 0.9f;
            float phase = Mathf.Repeat(Time.unscaledTime, period);
            if (phase >= dur) return;
            float t = phase / dur;
            float fade = Mathf.Sin(t * Mathf.PI);
            float rad = plate.width * 0.5f;
            float band = plate.width * 0.16f;
            float x = Mathf.Lerp(-band, plate.width, t);
            // Band stays inside the disc: its corners sit on the circle.
            float y0 = plate.height * 0.22f;
            float halfH = plate.height * 0.28f;
            float chord = Mathf.Sqrt(Mathf.Max(0f, rad * rad - halfH * halfH)) * 2f;
            float left = plate.width * 0.5f - chord * 0.5f;
            float drawX = Mathf.Max(x, left);
            float drawR = Mathf.Min(x + band, left + chord);
            if (drawR <= drawX) return;
            GUI.BeginGroup(plate);
            GUI.color = new Color(1f, 0.96f, 0.78f, (held ? 0.10f : 0.26f) * fade);
            GUI.DrawTexture(new Rect(drawX, y0, drawR - drawX, halfH * 2f), GlowTex(), ScaleMode.StretchToFill, true);
            GUI.EndGroup();
            GUI.color = Color.white;
        }

        static void DrawVipStuds(Rect plate)
        {
            var gem = VipGemTex();
            float sz = plate.width * 0.078f;
            for (int i = 0; i < VipStuds; i++)
            {
                VipStudPos(i, VipStuds, plate, out float x, out float y);
                GUI.color = new Color(0.10f, 0.05f, 0.02f, 0.48f);
                GUI.DrawTexture(new Rect(x - sz * 0.5f + 0.6f, y - sz * 0.5f + 1.1f, sz, sz), gem, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(x - sz * 0.5f, y - sz * 0.5f, sz, sz), gem, ScaleMode.ScaleToFit, true);
            }
            GUI.color = Color.white;
        }

        // Stud index, or -1 for a crown spot (nx, ny in the crown rect, y down). One lap every couple of seconds.
        static readonly int[] VipGlintStud = { 0, -1, 3, -1, 6, 9 };
        static readonly float[] VipGlintNx = { 0f, 0.50f, 0f, 0.26f, 0f, 0f };
        static readonly float[] VipGlintNy = { 0f, 0.18f, 0f, 0.34f, 0f, 0f };

        static void DrawVipGlints(Rect plate, Rect crown, bool held)
        {
            var tex = VipGlintTex();
            int n = VipGlintStud.Length;
            float phase = Mathf.Repeat(Time.unscaledTime / 2.2f, 1f) * n;
            float sz = plate.width * 0.048f;
            float dim = held ? 0.55f : 1f;
            for (int i = 0; i < n; i++)
            {
                float dist = Mathf.Abs(phase - i);
                if (dist > n * 0.5f) dist = n - dist;
                if (dist > 1.02f) continue;
                float a = Mathf.Cos(Mathf.Clamp01(dist) * Mathf.PI * 0.5f);
                if (a < 0.04f) continue;
                float x, y;
                if (VipGlintStud[i] >= 0)
                    VipStudPos(VipGlintStud[i], VipStuds, plate, out x, out y);
                else
                {
                    x = crown.x + crown.width * VipGlintNx[i];
                    y = crown.y + crown.height * VipGlintNy[i];
                }
                GUI.color = new Color(1f, 0.97f, 0.84f, 0.90f * a * dim);
                GUI.DrawTexture(new Rect(x - sz * 0.5f, y - sz * 0.5f, sz, sz), tex, ScaleMode.ScaleToFit, true);
            }
            GUI.color = Color.white;
        }

        // Jewel centers on the gold rim. Angle 0 sits at the top. GUI y grows downward.
        static void VipStudPos(int i, int n, Rect plate, out float x, out float y)
        {
            float ang = -Mathf.PI * 0.5f + i * (Mathf.PI * 2f / Mathf.Max(1, n));
            float rad = plate.width * 0.435f;
            x = plate.center.x + Mathf.Cos(ang) * rad;
            y = plate.center.y + Mathf.Sin(ang) * rad;
        }

        static Texture2D VipPlateTex()
        {
            if (_vipPlate != null) return _vipPlate;
            int n = VipPlateN;
            var tex = NewVipTex(n, n, "VipPlate");
            var px = new Color32[n * n];
            float c0 = n * 0.5f;
            float rad = c0 - 1.4f;
            float rimIn = rad * 0.76f;
            const float lip = 5.5f;
            const float shade = 9f;
            for (int y = 0; y < n; y++)
            {
                float py = y + 0.5f;
                // High texture y is the top of the medallion.
                float up = py / (n - 1f);
                for (int x = 0; x < n; x++)
                {
                    float dx = x + 0.5f - c0;
                    float dy = py - c0;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float sd = dist - rad;
                    float aa = Mathf.Clamp01(1.05f - sd);
                    if (aa <= 0.004f)
                    {
                        px[y * n + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }
                    float ring = rad - dist;
                    Color c;
                    if (ring < lip)
                    {
                        float k = Mathf.Clamp01(ring / lip);
                        c = Color.Lerp(new Color(0.05f, 0.02f, 0.012f, 1f), new Color(0.72f, 0.46f, 0.12f, 1f), k);
                        c = Color.Lerp(c, new Color(1f, 0.92f, 0.62f, 1f), k * up * 0.45f);
                    }
                    else if (dist >= rimIn)
                    {
                        float span = Mathf.Max(1f, rad - lip - rimIn);
                        float t = Mathf.Clamp01((dist - rimIn) / span);
                        float bevel = Mathf.Sin(t * Mathf.PI);
                        Color deep = new Color(0.40f, 0.20f, 0.04f, 1f);
                        Color mid = new Color(0.90f, 0.64f, 0.14f, 1f);
                        Color bright = new Color(1f, 0.95f, 0.68f, 1f);
                        c = Color.Lerp(deep, mid, Mathf.Lerp(0.22f, 1f, up));
                        c = Color.Lerp(c, bright, bevel * Mathf.Lerp(0.12f, 0.85f, up));
                        float inner = Mathf.Clamp01(1f - t * 3.2f);
                        c = Color.Lerp(c, new Color(0.28f, 0.13f, 0.03f, 1f), inner * 0.55f);
                    }
                    else if (dist >= rimIn - shade)
                    {
                        float t = Mathf.Clamp01((rimIn - dist) / shade);
                        float belly = Mathf.Sin(t * Mathf.PI);
                        c = Color.Lerp(new Color(0.22f, 0.10f, 0.03f, 1f), new Color(0.03f, 0.008f, 0.014f, 1f), t);
                        c = Color.Lerp(c, new Color(0.012f, 0.004f, 0.008f, 1f), belly * 0.72f);
                    }
                    else
                    {
                        float u = dist / Mathf.Max(1f, rimIn - shade);
                        c = Color.Lerp(new Color(0.11f, 0.028f, 0.040f, 1f), new Color(0.035f, 0.010f, 0.018f, 1f), Mathf.SmoothStep(0f, 1f, u));
                        c = Color.Lerp(c, new Color(0.16f, 0.045f, 0.05f, 1f), up * (1f - u) * 0.22f);
                    }
                    float lipD = Mathf.Abs(dist - rimIn);
                    if (lipD < 1.7f && sd < -lip)
                        c = Color.Lerp(c, new Color(1f, 0.90f, 0.55f, 1f), (1f - lipD / 1.7f) * (0.35f + 0.65f * up));
                    float hx = dx / rad;
                    float hy = (dy - rad * 0.34f) / (rad * 0.48f);
                    float h2 = hx * hx * 1.35f + hy * hy;
                    if (h2 < 1f)
                    {
                        float gloss = 1f - h2;
                        gloss *= gloss;
                        float gain = dist >= rimIn ? 0.62f : 0.18f;
                        c = Color.Lerp(c, new Color(1f, 0.98f, 0.90f, 1f), gloss * gain);
                    }
                    c.a = aa;
                    px[y * n + x] = (Color32)c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _vipPlate = tex;
            return tex;
        }

        static Texture2D VipRibbonTex()
        {
            if (_vipRibbon != null) return _vipRibbon;
            const int w = 230;
            const int h = 70;
            var tex = NewVipTex(w, h, "VipRibbon");
            var px = new Color32[w * h];
            float mid = h * 0.5f;
            float notch = h * 0.36f;
            for (int y = 0; y < h; y++)
            {
                float ay = Mathf.Abs((y + 0.5f) - mid) / mid;
                float cut = notch * (1f - Mathf.Clamp01(ay));
                float up = y / (float)(h - 1);
                for (int x = 0; x < w; x++)
                {
                    float fx = x + 0.5f;
                    float hin = Mathf.Min(fx - cut, (w - cut) - fx);
                    float vin = Mathf.Min(y + 0.5f, h - (y + 0.5f));
                    float edge = Mathf.Min(hin, vin);
                    if (edge < -0.85f)
                    {
                        px[y * w + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }
                    Color cloth = Color.Lerp(new Color(0.40f, 0.06f, 0.10f, 1f), new Color(0.68f, 0.11f, 0.15f, 1f), Mathf.SmoothStep(0.08f, 0.92f, up));
                    float fold = Mathf.Clamp01(1f - Mathf.Abs(up - 0.30f) / 0.16f);
                    cloth = Color.Lerp(cloth, new Color(0.84f, 0.26f, 0.28f, 1f), fold * 0.40f);
                    Color c = cloth;
                    if (edge < 3.6f)
                    {
                        Color trim = edge < 1.35f
                            ? new Color(0.24f, 0.10f, 0.03f, 1f)
                            : new Color(1f, 0.86f, 0.38f, 1f);
                        c = Color.Lerp(trim, cloth, Mathf.Clamp01((edge - 1.15f) / 2.4f));
                    }
                    c.a = Mathf.Clamp01(edge + 0.8f);
                    px[y * w + x] = (Color32)c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _vipRibbon = tex;
            return tex;
        }

        // Round blue rim stud (DrawVipStuds). Not the owned-face diamond.
        static Texture2D VipGemTex()
        {
            if (_vipGem != null) return _vipGem;
            const int n = 48;
            var tex = NewVipTex(n, n, "VipGem");
            var px = new Color32[n * n];
            float c0 = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = (x - c0) / c0;
                    float v = (y - c0) / c0;
                    float dist = Mathf.Sqrt(u * u + v * v);
                    if (dist > 0.94f)
                    {
                        px[y * n + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }
                    float up = v * 0.5f + 0.5f;
                    Color deep = new Color(0.12f, 0.32f, 0.72f, 1f);
                    Color mid = new Color(0.42f, 0.74f, 1f, 1f);
                    Color c = Color.Lerp(deep, mid, up);
                    float bevel = Mathf.Clamp01((dist - 0.55f) / 0.28f);
                    c = Color.Lerp(c, new Color(0.86f, 0.95f, 1f, 1f), (1f - dist) * 0.55f);
                    c = Color.Lerp(c, new Color(0.08f, 0.18f, 0.42f, 1f), bevel * 0.72f);
                    float sx = u + 0.16f;
                    float sy = v - 0.28f;
                    if (sx * sx + sy * sy < 0.045f)
                        c = Color.Lerp(c, Color.white, 0.92f);
                    c.a = Mathf.Clamp01((0.94f - dist) / 0.08f);
                    px[y * n + x] = (Color32)c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _vipGem = tex;
            return tex;
        }

        // Point-up cut stone: ice table, sapphire pavilion, one ruby facet, gold girdle.
        // Square bake; DrawVipDiamond scales it onto the disc. Facets are colour blocks
        // so the cut still reads when the rail badge is small.
        static Texture2D VipDiamondTex()
        {
            if (_vipDiamond != null) return _vipDiamond;
            const int n = 256;
            var tex = NewVipTex(n, n, "VipDiamond");
            var px = new Color32[n * n];
            float c0 = n * 0.5f;
            float hx = n * VipDiamondHx;
            float hy = n * VipDiamondHy;
            var gold = new Color(1f, 0.90f, 0.46f, 1f);
            var goldDeep = new Color(0.55f, 0.32f, 0.07f, 1f);
            var ice = new Color(0.78f, 0.90f, 1f, 1f);
            var iceLit = new Color(0.96f, 0.99f, 1f, 1f);
            var pale = new Color(0.48f, 0.72f, 0.98f, 1f);
            var sap = new Color(0.16f, 0.38f, 0.82f, 1f);
            var sapDeep = new Color(0.08f, 0.20f, 0.55f, 1f);
            var ruby = new Color(0.78f, 0.12f, 0.18f, 1f);
            var rubyLit = new Color(0.98f, 0.48f, 0.42f, 1f);
            const float rimPx = 12f;
            const float girdle = 0.060f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = x + 0.5f - c0;
                    float dy = y + 0.5f - c0;
                    float u = dx / hx;
                    float v = dy / hy;
                    float diamond = Mathf.Abs(u) + Mathf.Abs(v);
                    float norm = Mathf.Sqrt(1f / (hx * hx) + 1f / (hy * hy));
                    float pin = (1f - diamond) / norm;
                    if (pin < -1.6f)
                    {
                        px[y * n + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }
                    float up = Mathf.Clamp01(v * 0.5f + 0.5f);
                    float tu = u / 0.38f;
                    float tv = (v - 0.50f) / 0.13f;
                    float td = Mathf.Abs(tu) + Mathf.Abs(tv);
                    bool table = td <= 1f && v > 0.30f && pin >= rimPx;
                    bool onRim = pin < rimPx;
                    bool onGirdle = Mathf.Abs(v) < girdle && !onRim;
                    Color c;
                    if (onRim)
                    {
                        float k = Mathf.Clamp01((pin + 1.6f) / (rimPx + 1.6f));
                        c = Color.Lerp(goldDeep, gold, Mathf.Lerp(0.20f, 1f, up) * Mathf.Lerp(0.45f, 1f, k));
                        if (up > 0.45f)
                            c = Color.Lerp(c, new Color(1f, 0.98f, 0.84f, 1f), (up - 0.45f) * 1.15f * k);
                    }
                    else if (onGirdle)
                    {
                        float band = 1f - Mathf.Abs(v) / girdle;
                        c = Color.Lerp(goldDeep, gold, 0.40f + 0.60f * up);
                        c = Color.Lerp(c, new Color(1f, 0.97f, 0.78f, 1f), band * band * 0.70f);
                    }
                    else if (table)
                    {
                        c = Color.Lerp(iceLit, Color.white, 0.72f);
                        c = Color.Lerp(ice, c, Mathf.Clamp01(1.05f - td));
                        if (td > 0.86f)
                            c = Color.Lerp(c, ice, (td - 0.86f) / 0.14f * 0.55f);
                    }
                    else if (v >= 0.62f)
                    {
                        float lit = Mathf.Clamp01(0.55f + 0.45f * v - 0.12f * u);
                        c = Color.Lerp(ice, iceLit, lit);
                    }
                    else if (v >= 0f)
                    {
                        float lit = Mathf.Clamp01(0.30f + 0.70f * v);
                        c = u < 0f
                            ? Color.Lerp(ice, iceLit, lit)
                            : Color.Lerp(pale, ice, lit * 0.55f);
                    }
                    else if (u > 0f)
                    {
                        float lit = Mathf.Clamp01(0.18f + 0.82f * (1f + v));
                        c = Color.Lerp(ruby, rubyLit, lit);
                    }
                    else
                    {
                        float lit = Mathf.Clamp01(0.12f + 0.88f * (1f + v));
                        c = Color.Lerp(sapDeep, sap, lit);
                    }
                    if (!onRim && pin < rimPx + 7f)
                    {
                        float k = 1f - Mathf.Clamp01((pin - rimPx) / 7f);
                        if (v >= 0f)
                            c = Color.Lerp(c, new Color(1f, 0.99f, 0.96f, 1f), k * 0.62f);
                        else
                            c = Color.Lerp(c, new Color(0.05f, 0.07f, 0.16f, 1f), k * 0.40f);
                    }
                    float sx = (u + 0.08f) / 0.15f;
                    float sy = (v - 0.52f) / 0.11f;
                    float spark = sx * sx + sy * sy;
                    if (spark < 1f && !onRim)
                    {
                        float k = 1f - spark;
                        c = Color.Lerp(c, Color.white, k * k * 0.94f);
                    }
                    float rx = (u - 0.36f) / 0.13f;
                    float ry = (v + 0.36f) / 0.11f;
                    float chip = rx * rx + ry * ry;
                    if (chip < 1f && v < -0.05f && !onRim)
                    {
                        float k = 1f - chip;
                        c = Color.Lerp(c, new Color(1f, 0.86f, 0.82f, 1f), k * k * 0.85f);
                    }
                    c.a = Mathf.Clamp01(pin + 1.25f);
                    px[y * n + x] = (Color32)c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _vipDiamond = tex;
            return tex;
        }

        static Texture2D VipGlintTex()
        {
            if (_vipGlint != null) return _vipGlint;
            const int n = 32;
            var tex = NewVipTex(n, n, "VipGlint");
            var px = new Color32[n * n];
            float c0 = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = (x - c0) / c0;
                    float v = (y - c0) / c0;
                    float ax = Mathf.Abs(u);
                    float ay = Mathf.Abs(v);
                    float vert = ax / 0.12f + ay / 1.02f;
                    float horz = ay / 0.12f + ax / 1.02f;
                    float d = Mathf.Min(vert, horz);
                    if (d > 1f)
                    {
                        px[y * n + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }
                    float edge = Mathf.Clamp01((1f - d) / 0.28f);
                    float core = Mathf.Clamp01(1.15f - d * 2.4f);
                    Color c = Color.Lerp(new Color(1f, 0.90f, 0.58f, 1f), Color.white, core);
                    c.a = edge * (0.72f + 0.28f * core);
                    px[y * n + x] = (Color32)c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _vipGlint = tex;
            return tex;
        }

        // Texture y grows up. Band narrows into a curved bottom; five points flare outward.
        static float[] _vipCrownPoly;
        static int _vipCrownN;

        static Texture2D VipCrownTex()
        {
            if (_vipCrown != null) return _vipCrown;
            BuildVipCrownPoly();
            int w = VipCrownW;
            int h = VipCrownH;
            var tex = NewVipTex(w, h, "VipCrown");
            var px = new Color32[w * h];
            const float outlineR = 3.6f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float fx = x + 0.5f;
                    float fy = y + 0.5f;
                    bool body = VipInPoly(fx, fy);
                    bool blue = VipCrownDisc(fx, fy, 96f, 104f, 15f);
                    bool jewel = VipCrownDisc(fx, fy, 96f, 30f, 7.5f);
                    bool ball = VipCrownDisc(fx, fy, 20f, 78f, 12f)
                        || VipCrownDisc(fx, fy, 54f, 92f, 12f)
                        || VipCrownDisc(fx, fy, 138f, 92f, 12f)
                        || VipCrownDisc(fx, fy, 172f, 78f, 12f);
                    bool fill = body || blue || jewel || ball;
                    if (!fill && !VipCrownNear(fx, fy, outlineR))
                    {
                        px[y * w + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }
                    Color c;
                    if (!fill)
                        c = new Color(0.08f, 0.025f, 0.04f, 0.94f);
                    else if (blue)
                        c = VipOrbColor(fx, fy, 96f, 104f, 15f, true);
                    else if (ball)
                        c = VipOrbColor(fx, fy, VipCrownBallAt(fx, fy), true);
                    else if (jewel)
                        c = VipJewelColor(fx, fy);
                    else
                    {
                        float up = fy / (h - 1f);
                        float g = up * up * (3f - 2f * up);
                        c = Color.Lerp(new Color(0.50f, 0.28f, 0.07f, 1f), new Color(1f, 0.90f, 0.48f, 1f), g);
                        if (!VipInPoly(fx - 2.8f, fy))
                            c = Color.Lerp(c, new Color(1f, 0.96f, 0.78f, 1f), 0.85f);
                        if (!VipInPoly(fx, fy - 2.4f))
                            c = Color.Lerp(c, new Color(0.36f, 0.18f, 0.04f, 1f), 0.65f);
                        float sx = (fx - 74f) / 34f;
                        float sy = (fy - 40f) / 11f;
                        float sheen = sx * sx + sy * sy;
                        if (sheen < 1f)
                        {
                            float k = 1f - sheen;
                            c = Color.Lerp(c, new Color(1f, 0.97f, 0.80f, 1f), k * k * 0.75f);
                        }
                    }
                    px[y * w + x] = (Color32)c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _vipCrown = tex;
            return tex;
        }

        static void BuildVipCrownPoly()
        {
            if (_vipCrownPoly != null) return;
            var p = new float[64];
            int n = 0;
            VipCrownBez(p, ref n, 48f, 20f, 32f, 32f, 26f, 44f, 5, false);
            VipCrownPt(p, ref n, 16f, 82f);
            VipCrownPt(p, ref n, 48f, 46f);
            VipCrownPt(p, ref n, 52f, 96f);
            VipCrownPt(p, ref n, 80f, 46f);
            VipCrownPt(p, ref n, 96f, 112f);
            VipCrownPt(p, ref n, 112f, 46f);
            VipCrownPt(p, ref n, 140f, 96f);
            VipCrownPt(p, ref n, 144f, 46f);
            VipCrownPt(p, ref n, 176f, 82f);
            VipCrownPt(p, ref n, 166f, 44f);
            VipCrownBez(p, ref n, 166f, 44f, 160f, 32f, 144f, 20f, 5, true);
            VipCrownBez(p, ref n, 144f, 20f, 96f, 8f, 48f, 20f, 8, true);
            _vipCrownPoly = p;
            _vipCrownN = n / 2;
        }

        static void VipCrownPt(float[] dst, ref int n, float x, float y)
        {
            dst[n++] = x;
            dst[n++] = y;
        }

        static void VipCrownBez(float[] dst, ref int n, float x0, float y0, float x1, float y1, float x2, float y2, int steps, bool skipFirst)
        {
            int i0 = skipFirst ? 1 : 0;
            for (int i = i0; i <= steps; i++)
            {
                float t = i / (float)steps;
                float u = 1f - t;
                dst[n++] = u * u * x0 + 2f * u * t * x1 + t * t * x2;
                dst[n++] = u * u * y0 + 2f * u * t * y1 + t * t * y2;
            }
        }

        static bool VipInPoly(float x, float y)
        {
            var p = _vipCrownPoly;
            int n = _vipCrownN;
            bool inside = false;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float yi = p[i * 2 + 1];
                float yj = p[j * 2 + 1];
                if ((yi > y) == (yj > y)) continue;
                float xi = p[i * 2];
                float xj = p[j * 2];
                if (x < (xj - xi) * (y - yi) / (yj - yi) + xi) inside = !inside;
            }
            return inside;
        }

        static bool VipCrownDisc(float x, float y, float cx, float cy, float r)
        {
            float dx = x - cx;
            float dy = y - cy;
            return dx * dx + dy * dy <= r * r;
        }

        static bool VipCrownFilled(float x, float y)
        {
            if (VipInPoly(x, y)) return true;
            if (VipCrownDisc(x, y, 96f, 104f, 15f)) return true;
            if (VipCrownDisc(x, y, 96f, 30f, 7.5f)) return true;
            if (VipCrownDisc(x, y, 20f, 78f, 12f)) return true;
            if (VipCrownDisc(x, y, 54f, 92f, 12f)) return true;
            if (VipCrownDisc(x, y, 138f, 92f, 12f)) return true;
            return VipCrownDisc(x, y, 172f, 78f, 12f);
        }

        static bool VipCrownNear(float x, float y, float r)
        {
            if (VipCrownFilled(x + r, y) || VipCrownFilled(x - r, y)) return true;
            if (VipCrownFilled(x, y + r) || VipCrownFilled(x, y - r)) return true;
            float d = r * 0.7f;
            return VipCrownFilled(x + d, y + d) || VipCrownFilled(x - d, y + d)
                || VipCrownFilled(x + d, y - d) || VipCrownFilled(x - d, y - d);
        }

        static Vector3 VipCrownBallAt(float x, float y)
        {
            if (VipCrownDisc(x, y, 20f, 78f, 12f)) return new Vector3(20f, 78f, 12f);
            if (VipCrownDisc(x, y, 54f, 92f, 12f)) return new Vector3(54f, 92f, 12f);
            if (VipCrownDisc(x, y, 138f, 92f, 12f)) return new Vector3(138f, 92f, 12f);
            return new Vector3(172f, 78f, 12f);
        }

        static Color VipOrbColor(float x, float y, float cx, float cy, float r, bool blue)
        {
            float dx = x - cx;
            float dy = y - cy;
            float dist = Mathf.Sqrt(dx * dx + dy * dy) / Mathf.Max(0.01f, r);
            float lit = Mathf.Clamp01(0.34f + 0.72f * ((dy * 0.85f - dx * 0.30f) / r));
            Color c = blue
                ? Color.Lerp(new Color(0.10f, 0.28f, 0.62f, 1f), new Color(0.72f, 0.92f, 1f, 1f), lit)
                : Color.Lerp(new Color(0.55f, 0.32f, 0.06f, 1f), new Color(1f, 0.92f, 0.55f, 1f), lit);
            float spec = (dx + 1.2f) * (dx + 1.2f) + (dy - r * 0.34f) * (dy - r * 0.34f);
            if (spec < r * r * 0.10f)
                c = Color.Lerp(c, new Color(1f, 0.99f, 0.94f, 1f), 0.85f);
            if (dist > 0.78f)
            {
                Color rim = blue ? new Color(0.05f, 0.12f, 0.32f, 1f) : new Color(0.36f, 0.18f, 0.04f, 1f);
                c = Color.Lerp(rim, c, Mathf.Clamp01((0.96f - dist) / 0.18f));
            }
            return c;
        }

        static Color VipOrbColor(float x, float y, Vector3 ball, bool gold)
        {
            return VipOrbColor(x, y, ball.x, ball.y, ball.z, !gold);
        }

        static Color VipJewelColor(float x, float y)
        {
            const float cx = 96f;
            const float cy = 30f;
            const float r = 7.5f;
            float dx = x - cx;
            float dy = y - cy;
            float dist = Mathf.Sqrt(dx * dx + dy * dy) / r;
            float lit = Mathf.Clamp01(0.30f + 0.75f * (dy / r));
            Color c = Color.Lerp(new Color(0.45f, 0.06f, 0.10f, 1f), new Color(0.95f, 0.28f, 0.32f, 1f), lit);
            if (dx * dx + (dy - 2.2f) * (dy - 2.2f) < 7f)
                c = Color.Lerp(c, new Color(1f, 0.90f, 0.90f, 1f), 0.8f);
            if (dist > 0.72f)
                c = Color.Lerp(new Color(0.28f, 0.04f, 0.06f, 1f), c, Mathf.Clamp01((1f - dist) / 0.28f));
            return c;
        }

        static Texture2D NewVipTex(int w, int h, string texName)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = texName
            };
        }

        // Left arrow in radius units. +x right, +y up. Thick shaft, broad head.
        static readonly float[] BackChevronPoly =
        {
            -0.62f, 0f,
            -0.02f, 0.56f,
            -0.02f, 0.30f,
            0.56f, 0.30f,
            0.56f, -0.30f,
            -0.02f, -0.30f,
            -0.02f, -0.56f
        };

        static bool BackInChevron(float nx, float ny)
        {
            var p = BackChevronPoly;
            bool inside = false;
            int n = p.Length / 2;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float yi = p[i * 2 + 1];
                float yj = p[j * 2 + 1];
                if ((yi > ny) == (yj > ny)) continue;
                float xi = p[i * 2];
                float xj = p[j * 2];
                float xAt = (xj - xi) * (ny - yi) / (yj - yi) + xi;
                if (nx < xAt) inside = !inside;
            }
            return inside;
        }

        // Clay disc, gold rim (same bevel as the VIP plate), gold left chevron.
        static Texture2D BackMedalTex()
        {
            if (_backMedal != null && _backMedalBuilt == BackMedalRev) return _backMedal;
            if (_backMedal != null)
            {
                Object.Destroy(_backMedal);
                _backMedal = null;
            }
            const int n = 160;
            var tex = NewVipTex(n, n, "BackMedal");
            var px = new Color32[n * n];
            float c0 = n * 0.5f;
            float rad = c0 - 1.4f;
            float rimIn = rad * 0.76f;
            const float lip = 7f;
            for (int y = 0; y < n; y++)
            {
                float py = y + 0.5f;
                float up = py / (n - 1f);
                for (int x = 0; x < n; x++)
                {
                    float dx = x + 0.5f - c0;
                    float dy = py - c0;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float sd = dist - rad;
                    float aa = Mathf.Clamp01(0.85f - sd);
                    if (aa <= 0.004f)
                    {
                        px[y * n + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }
                    float ring = rad - dist;
                    Color c;
                    if (ring < lip)
                    {
                        c = Color.Lerp(new Color(0.10f, 0.04f, 0.015f, 1f), new Color(0.55f, 0.32f, 0.08f, 1f), Mathf.Clamp01(ring / lip));
                    }
                    else if (dist >= rimIn)
                    {
                        float t = Mathf.InverseLerp(rad - lip, rimIn, dist);
                        Color deep = new Color(0.42f, 0.22f, 0.05f, 1f);
                        Color mid = new Color(0.93f, 0.68f, 0.18f, 1f);
                        Color bright = new Color(1f, 0.95f, 0.68f, 1f);
                        float bevel = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
                        c = Color.Lerp(deep, mid, 0.35f + 0.65f * up);
                        c = Color.Lerp(c, bright, bevel * (0.28f + 0.72f * up));
                        float groove = Mathf.Clamp01((t - 0.78f) / 0.22f);
                        c = Color.Lerp(c, new Color(0.30f, 0.15f, 0.04f, 1f), groove * 0.8f);
                        float spec = Mathf.Clamp01(1.05f - Mathf.Sqrt(dx * dx * 1.3f + (dy - rad * 0.42f) * (dy - rad * 0.42f)) / rad);
                        c = Color.Lerp(c, new Color(1f, 0.98f, 0.84f, 1f), spec * spec * 0.55f);
                    }
                    else
                    {
                        float u = dist / Mathf.Max(1f, rimIn);
                        float grain = 0.5f + 0.5f * Mathf.Sin(x * 0.09f + Mathf.Sin(y * 0.05f) * 2.4f);
                        float streak = 0.5f + 0.5f * Mathf.Sin(y * 0.055f + x * 0.012f);
                        c = Color.Lerp(new Color(0.50f, 0.30f, 0.15f, 1f), new Color(0.84f, 0.60f, 0.34f, 1f), grain * 0.75f + 0.12f);
                        c = Color.Lerp(c, new Color(0.68f, 0.44f, 0.22f, 1f), streak * 0.22f);
                        c = Color.Lerp(c, new Color(0.38f, 0.20f, 0.10f, 1f), Mathf.SmoothStep(0.42f, 1f, u) * 0.58f);
                        c = Color.Lerp(c, new Color(0.96f, 0.84f, 0.60f, 1f), Mathf.Clamp01(up * (1f - u) * 0.42f));
                        float glaze = Mathf.Clamp01(1.12f - Mathf.Sqrt(dx * dx * 1.15f + (dy - rad * 0.22f) * (dy - rad * 0.22f)) / (rad * 0.62f));
                        c = Color.Lerp(c, new Color(0.98f, 0.90f, 0.74f, 1f), glaze * glaze * 0.32f);
                    }
                    float lipD = Mathf.Abs(dist - rimIn);
                    if (lipD < 2.6f && sd < -lip)
                    {
                        float k = 1f - lipD / 2.6f;
                        c = Color.Lerp(c, new Color(1f, 0.90f, 0.52f, 1f), k * 0.70f);
                    }
                    if (dist < rimIn - 1.5f)
                    {
                        float nx = dx / rad;
                        float ny = dy / rad;
                        float e = 2.2f / rad;
                        bool on = BackInChevron(nx, ny);
                        if (!on && BackInChevron(nx - e, ny + e * 1.25f))
                            c = Color.Lerp(c, new Color(0.20f, 0.10f, 0.04f, 1f), 0.50f);
                        if (on)
                        {
                            bool aboveOut = !BackInChevron(nx, ny + e);
                            bool around = aboveOut
                                || !BackInChevron(nx, ny - e)
                                || !BackInChevron(nx + e, ny)
                                || !BackInChevron(nx - e, ny);
                            if (!around)
                            {
                                float lit = Mathf.Clamp01(0.28f + 0.72f * (ny + 0.52f));
                                c = Color.Lerp(new Color(0.62f, 0.40f, 0.08f, 1f), new Color(1f, 0.94f, 0.60f, 1f), lit);
                            }
                            else if (aboveOut)
                                c = new Color(1f, 0.95f, 0.70f, 1f);
                            else
                                c = new Color(0.36f, 0.18f, 0.05f, 1f);
                        }
                    }
                    c.a = aa;
                    px[y * n + x] = (Color32)c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _backMedalBuilt = BackMedalRev;
            _backMedal = tex;
            return tex;
        }

        // Shared disc for every screen that leaves. About 15% over the old 88pt, never under 44.
        static Rect BackMedalRect(float s, float top) => BackMedalBox(s, top, Screen.safeArea);

        static Rect BackMedalBox(float s, float top, Rect safe)
        {
            float size = 101.2f * Mathf.Clamp(s, 1f, 1.35f);
            if (size < 44f) size = 44f;
            float x = Mathf.Max(16f, safe.xMin + 10f);
            return new Rect(x, top, size, size);
        }

        // Hit pad stays full size. The face scales down while the finger is down.
        bool DrawBackMedal(Rect hit)
        {
            bool fire = HitPad(hit, out bool held);
            if (!held && GlovePresses(hit)) held = true;
            if (fire) Sfx.CardTap();
            PaintBackMedal(hit, held);
            return fire;
        }

        void PaintBackMedal(Rect hit, bool held)
        {
            float k = held ? 0.94f : 1f;
            float side = Mathf.Min(hit.width, hit.height);
            float d = side * k;
            var face = new Rect(hit.center.x - d * 0.5f, hit.center.y - d * 0.5f, d, d);
            var tex = BackMedalTex();
            var glow = GlowTex();
            float pad = face.width * 0.12f;
            GUI.color = new Color(1f, 0.82f, 0.36f, held ? 0.08f : 0.16f);
            GUI.DrawTexture(new Rect(face.x - pad, face.y - pad, face.width + pad * 2f, face.height + pad * 2f), glow, ScaleMode.ScaleToFit, true);
            float sh = held ? 2f : 4f;
            GUI.color = new Color(0.08f, 0.05f, 0.02f, held ? 0.22f : 0.40f);
            GUI.DrawTexture(new Rect(face.x + sh * 0.45f, face.y + sh, face.width, face.height), tex, ScaleMode.ScaleToFit, true);
            GUI.color = held ? new Color(0.88f, 0.84f, 0.76f, 1f) : Color.white;
            GUI.DrawTexture(face, tex, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
        }

        void DismissStreakSign()
        {
            bool live = _streakSlide >= 0f;
            bool finished = _streakSlide >= 1f;
            _streakSlide = -1f;
            if (!live) return;
            // Leaving early still pays once. The pay step already cleared Owed if it ran.
            PayStreakStep();
            Purse.Pending = 0;
            // The tuck finished on the homepage. Breath, then the bird lesson.
            // An early leave (level start, hive, poker) does not take that breath.
            if (finished && _splash && _home == HomeFace.Splash && AdoptHoldsQueue())
                BeginRewardGap();
        }

        // Lessons only. The sign is already gone, so this is not an input block.
        void BeginRewardGap()
        {
#if UNITY_EDITOR
            if (_dailyShotQuiet || _pokerPlayrun || EditorShotLive) return;
#endif
            if (_rewardGap) return;
            int gen = ++_rewardGapGen;
            _rewardGap = true;
            StartCoroutine(RunRewardGap(gen));
        }

        void CancelRewardGap()
        {
            _rewardGapGen++;
            _rewardGap = false;
        }

        IEnumerator RunRewardGap(int gen)
        {
            yield return WaitRewardPay(true);
            if (gen != _rewardGapGen) yield break;
            _rewardGap = false;
        }

        // Streak board, or the breath after it. Splash lessons wait on this.
        bool RewardLessonHold()
        {
            return _streakSlide >= 0f || _rewardGap;
        }

        // One call adds the clear. A repeat, including the fade's dismiss, does not.
        void PayStreakStep()
        {
#if UNITY_EDITOR
            if (EditorShotLive) return;
#endif
            int before = Purse.Coins;
            if (!Purse.CommitStreakPay()) return;
            ArmStreakCoins(Purse.Coins - before);
        }

        // Editor captures must not commit a real owed clear.
        void SettleUnshownStreakPay()
        {
#if UNITY_EDITOR
            if (EditorShotLive) return;
#endif
            Purse.CommitStreakPay();
        }

        void ArmStreakSlide()
        {
            if (Purse.Streak <= 0)
            {
                _streakSlide = -1f;
                _streakAnnounced = 0;
                _streakChirped = false;
                _streakHomeDue = false;
                SettleUnshownStreakPay();
                return;
            }
            _streakAnnounced = Purse.Streak;
            // Owed is still unspent. The sign's pay beat launches the coins.
            if (_streakHomeDue)
            {
                _streakHomeDue = false;
                OpenStreakBoard();
                return;
            }
            // Neon tally only after a stage clear (coins incoming).
            if (Purse.Pending <= 0)
            {
                _streakSlide = -1f;
                SettleUnshownStreakPay();
                return;
            }
            OpenStreakBoard();
        }

        void OpenStreakBoard()
        {
            if (!GatePopup(PopupKind.Streak, false)) return;
            _streakSlide = 0f;
            _streakChirped = false;
            _streakWinChimed = false;
            _streakRollShown = 0;
            _streakTickBucket = -1;
            _streakFlyArmed = false;
            _rewardStampHit = false;
            _waxBorn = -1f;
        }

        void StepStreakReward()
        {
            if (_clearReward || _streakSlide < 0f) return;
            if (Purse.Streak <= 0)
            {
                DismissStreakSign();
                return;
            }
            int streak;
            int fromPay;
            int toPay;
            RewardPays(out streak, out fromPay, out toPay);
            var beat = RewardBeats(RewardMulOn(streak, fromPay, toPay));
            float dur = beat.SeqDur > 0.05f ? beat.SeqDur : 0.05f;
            float t = _streakSlide * dur;
            // Tuck waits until the shared flight, the balance tick, and the payout sound finish.
            bool fadeGate = t >= beat.HoldEnd && RewardPayBusy();
            if (!fadeGate)
                _streakSlide = Mathf.Min(1f, _streakSlide + PlayClock.Delta / dur);
            t = _streakSlide * dur;
            if (t >= beat.PayAt)
                PayStreakStep();
            if (_streakSlide >= 1f && !RewardPayBusy())
                DismissStreakSign();
        }

        void ArmStreakCoins(int coins)
        {
            if (_streakFlyArmed) return;
            _streakFlyArmed = true;
            FoldCoinFlies();
            BeginRewardPay(coins);
        }

        void DrawStreakRewards(float s, bool raiseSign = false)
        {
            var pig = PiggyRect(s);
            var prevM = GUI.matrix;
            if (_pigJiggle > 0f)
            {
                _pigJiggle = Mathf.Max(0f, _pigJiggle - Time.unscaledDeltaTime / 0.30f);
                float j = _pigJiggle;
                float wobble = Mathf.Sin(j * Mathf.PI * 3.2f) * 7f * j;
                float sx = 1f + 0.14f * Mathf.Sin(j * Mathf.PI);
                float sy = 1f - 0.18f * Mathf.Sin(j * Mathf.PI);
                GUIUtility.RotateAroundPivot(wobble, pig.center);
                GUIUtility.ScaleAroundPivot(new Vector2(sx, sy), pig.center);
            }
            DrawRailIcon(pig, SpriteCatalog.Piggy);
            GUI.matrix = prevM;

            // Step before the balance so a pay this frame can hold its coins until they land.
            if (GuiPaint())
                StepStreakReward();

            // Bigger persistent balance: "$12" + coin sprite on the right.
            DrawCoinBalance(s, pig.x - 12f, pig.center.y);
            if (!raiseSign) DrawStreakSignLayer(s);
        }

        // The reward streak pop-up as one layer: sign (box, multiplier, brass sparkles), the
        // coins it launches, and the piggy's catch glow. Draw-only, no hits. The home screen
        // paints it after the side rails (DrawSplash) so the rails never cover it; every other
        // caller gets it inside DrawStreakRewards as before.
        void DrawStreakSignLayer(float s)
        {
            var pig = PiggyRect(s);
            // Streak sign holds, then fades on its own.
            DrawStreakToast(s, pig);
            DrawCoinFly(pig);
            if (_pigBurst > 0f)
            {
                _pigBurst = Mathf.Max(0f, _pigBurst - Time.unscaledDeltaTime / 0.55f);
                var glow = GlowTex();
                float b = _pigBurst;
                float pad = pig.width * (0.12f + 0.22f * b);
                GUI.color = new Color(1f, 0.88f, 0.40f, 0.18f + 0.38f * b);
                GUI.DrawTexture(new Rect(pig.x - pad, pig.y - pad, pig.width + pad * 2f, pig.height + pad * 2f), glow, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
            }
        }

        // THE coin balance ($ amount + coin icon), shared by the home screen and the poker page:
        // one icon size, one type scale, one font. rightX is the icon's right edge, centerY its
        // middle (home: just left of the piggy; poker: the top-right corner on the back-medal
        // row, where the home rail slot would sit on the PAY TABLE tab). Held-coin count-up
        // rides CoinsDrawn() inside DrawCoinCluster, so both screens roll the same way.
        const float CoinBalanceType = 0.95f;

        // Icon edge from the rail size at rest (the piggy is the rail's size), so the poker
        // page, which has no rail, gets the exact home size.
        static float CoinBalanceIcon(float s) =>
            Mathf.Clamp(SplashRailSize() * 0.48f * 0.80f, 29f * s, 51f * s);

        void DrawCoinBalance(float s, float rightX, float centerY)
        {
            float icon = CoinBalanceIcon(s);
            var ir = new Rect(rightX - icon, centerY - icon * 0.5f, icon, icon);
            DrawCoinCluster(ir, s, CoinBalanceType);
        }

        // Amount matches the coin times typeScale, then shrinks on width only.
        // Home and poker go through DrawCoinBalance (CoinBalanceType); the badger page passes 0.86.
        void DrawCoinCluster(Rect iconR, float s, float typeScale = 1f)
        {
            var coinSpr = SpriteCatalog.Coin;
            string coins = Money.Format(CoinsDrawn());
            if (_coinClusterStyle == null)
            {
                _coinClusterStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleRight,
                    wordWrap = false,
                    clipping = TextClipping.Overflow
                };
            }
            if (_coinClusterContent == null) _coinClusterContent = new GUIContent();
            var st = _coinClusterStyle;
            var content = _coinClusterContent;
            content.text = coins;
            float leftLimit = Mathf.Max(8f, Screen.safeArea.xMin + 8f);
            float maxW = Mathf.Max(36f, iconR.x - 8f * s - leftLimit);
            int key = coins.GetHashCode() ^ (coins.Length * 104729) ^ (Mathf.RoundToInt(maxW) * 17) ^ (Mathf.RoundToInt(iconR.height * typeScale) * 131);
            if (key != _coinKey || _coinPx < 14)
            {
                st.fontSize = Mathf.Max(14, Mathf.RoundToInt(iconR.height * typeScale));
                int guard = 0;
                while (st.fontSize > 14 && guard < 48 && st.CalcSize(content).x > maxW)
                {
                    st.fontSize -= 1;
                    guard++;
                }
                _coinPx = st.fontSize;
                _coinKey = key;
            }
            else
                st.fontSize = _coinPx;
            var size = st.CalcSize(content);
            var coinR = new Rect(iconR.x - 8f * s - size.x, iconR.center.y - size.y * 0.5f, size.x + 4f, size.y);
            int dark = Mathf.Max(2, Mathf.RoundToInt(st.fontSize * 0.12f));
            StampOutlined(coinR, coins, st, new Color(0.22f, 0.10f, 0.03f), 2, dark);
            if (coinSpr != null && coinSpr.texture != null)
                DrawIdleCoin(iconR, coinSpr.texture);
        }

        static readonly Vector2[] CoinGlints =
        {
            new Vector2(0.18f, 0.22f),
            new Vector2(0.82f, 0.18f),
            new Vector2(0.12f, 0.58f),
            new Vector2(0.88f, 0.52f),
            new Vector2(0.50f, -0.08f),
            new Vector2(0.50f, 1.08f)
        };

        void ArmCoinIdle()
        {
            _coinArmed = true;
            _coinSpinT = -1f;
            _coinHoldAge = 0f;
            _coinHoldLeft = Random.Range(4.5f, 9.0f);
        }

        // Slow Y-axis flip, then a random wait with shimmer + sparkle.
        void DrawIdleCoin(Rect ir, Texture tex)
        {
            if (!_coinArmed) ArmCoinIdle();
            const float spinDur = 2.6f;
            float dt = Time.unscaledDeltaTime;
            bool spinning = _coinSpinT >= 0f;
            float yaw = 0f;
            if (spinning)
            {
                _coinSpinT += dt / spinDur;
                if (_coinSpinT >= 1f)
                {
                    _coinSpinT = -1f;
                    _coinHoldAge = 0f;
                    _coinHoldLeft = Random.Range(5.5f, 12.0f);
                    spinning = false;
                }
                else
                {
                    float s = Mathf.SmoothStep(0f, 1f, _coinSpinT);
                    yaw = s * 360f;
                }
            }
            else
            {
                _coinHoldAge += dt;
                _coinHoldLeft -= dt;
                if (_coinHoldLeft <= 0f)
                    _coinSpinT = 0f;
            }

            // Cosine width: face-on → edge → mirrored face. Height stays put.
            float sx = Mathf.Cos(yaw * Mathf.Deg2Rad);
            if (Mathf.Abs(sx) < 0.06f)
                sx = 0.06f * Mathf.Sign(sx == 0f ? 1f : sx);

            var glow = GlowTex();
            var prevM = GUI.matrix;
            GUIUtility.ScaleAroundPivot(new Vector2(sx, 1f), ir.center);

            GUI.color = new Color(0.08f, 0.05f, 0.02f, 0.32f);
            GUI.DrawTexture(new Rect(ir.x + 2f, ir.y + 3f, ir.width, ir.height), tex, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
            GUI.DrawTexture(ir, tex, ScaleMode.ScaleToFit, true);

            if (!spinning)
            {
                float sheenU = Mathf.Repeat(_coinHoldAge * 0.55f, 1.35f);
                if (sheenU < 1f)
                {
                    float fade = Mathf.Sin(sheenU * Mathf.PI);
                    float x = ir.x + ir.width * (sheenU * 1.2f - 0.2f);
                    GUI.color = new Color(1f, 0.96f, 0.72f, 0.42f * fade);
                    GUI.DrawTexture(new Rect(x, ir.y + ir.height * 0.08f, ir.width * 0.22f, ir.height * 0.84f), glow, ScaleMode.ScaleToFit, true);
                }
            }

            GUI.matrix = prevM;
            GUI.color = Color.white;

            if (spinning) return;
            for (int i = 0; i < CoinGlints.Length; i++)
            {
                float tw = Mathf.Sin(_coinHoldAge * 2.05f + i * 1.07f);
                tw = Mathf.Max(0f, tw);
                tw = tw * tw;
                if (tw < 0.10f) continue;
                var uv = CoinGlints[i];
                float sz = ir.width * (0.22f + 0.38f * tw);
                var r = new Rect(
                    ir.x + ir.width * uv.x - sz * 0.5f,
                    ir.y + ir.height * uv.y - sz * 0.5f,
                    sz, sz);
                GUI.color = new Color(1f, 0.95f, 0.70f, 0.22f + 0.70f * tw);
                GUI.DrawTexture(r, glow, ScaleMode.ScaleToFit, true);
            }
            GUI.color = Color.white;
        }

        // One clock for every streak and every login day. Each start is the
        // previous step's end, so the beats do not overlap.
        // word: REWARD fades in. amt: the money fades in.
        // stamp + roll run only when a streak multiplier applies (badge thud,
        // then the amount ticks up). pay credits once, holds, then the sign fades.
        // RewardPace is the single knob. 1.32 holds each beat about a third longer.
        const float RewardPace = 1.32f;
        const float RewardWordDur = 0.45f * RewardPace;
        const float RewardAmtDur = 0.40f * RewardPace;
        const float RewardStampDur = 0.40f * RewardPace;
        const float RewardRollDur = 0.55f * RewardPace;
        const float RewardHoldDur = 1.20f * RewardPace;
        const float RewardFadeDur = 0.75f * RewardPace;
        const float RewardShakeDur = 0.22f * RewardPace;
        // Breath after the homepage streak board tucks, before the bird lesson.
        // Wall clock, inside 0.5–0.8s. WaitRewardPay is the only waiter.
        const float RewardGap = 0.65f;

        struct RewardBeat
        {
            public float WordAt;
            public float AmtAt;
            public float StampAt;
            public float RollAt;
            public float PayAt;
            public float HoldEnd;
            public float SeqDur;
            public bool Mul;
        }

        // Amount before the stamp is base × login day. The badge is the streak
        // only, and the roll lands on LastWin (base × streak × login) a single time.
        static void RewardPays(out int streak, out int fromPay, out int toPay)
        {
            streak = StreakTier.Display(Purse.LastStreak > 0 ? Purse.LastStreak : Purse.Streak);
            int stagePay = Purse.LastStagePay > 0 ? Purse.LastStagePay : Purse.StagePay;
            if (stagePay < 0) stagePay = 0;
            int login = Purse.LastLogin > 0 ? Purse.LastLogin : Purse.LoginMul;
            if (login < 1) login = 1;
            fromPay = stagePay * login;
            int win = Purse.LastWin > 0 ? Purse.LastWin : fromPay * streak;
            toPay = win > fromPay ? win : fromPay;
        }

        static bool RewardMulOn(int streak, int fromPay, int toPay)
        {
            return streak > 1 && toPay > fromPay;
        }

        static RewardBeat RewardBeats(bool mul)
        {
            var beat = new RewardBeat();
            beat.Mul = mul;
            beat.WordAt = 0f;
            beat.AmtAt = RewardWordDur;
            beat.StampAt = beat.AmtAt + RewardAmtDur;
            beat.RollAt = beat.StampAt + RewardStampDur;
            beat.PayAt = mul ? beat.RollAt + RewardRollDur : beat.StampAt;
            beat.HoldEnd = beat.PayAt + RewardHoldDur;
            beat.SeqDur = beat.HoldEnd + RewardFadeDur;
            return beat;
        }

        // x2 = 1, x3 = 1.20, x5 = 1.45, x10 = 1.85. Smooth between the anchors.
        static float RewardStampScale(int mul)
        {
            float m = Mathf.Max(2, mul);
            if (m <= 3f) return Mathf.Lerp(1f, 1.20f, m - 2f);
            if (m <= 5f) return Mathf.Lerp(1.20f, 1.45f, (m - 3f) / 2f);
            if (m < 10f) return Mathf.Lerp(1.45f, 1.85f, (m - 5f) / 5f);
            return 1.85f;
        }

        // EaseOutBack (c1 = 2.2) first reaches 1 here. That frame is the thud.
        static float RewardStampLandU()
        {
            const float c1 = 2.2f;
            return 1f - c1 / (c1 + 1f);
        }

        static float RewardStampPop(float age)
        {
            float u = Mathf.Clamp01(age / RewardStampDur);
            return Mathf.LerpUnclamped(2.2f, 1f, EaseOutBack(u));
        }

        static Vector2 RewardCardShake(float age, float s)
        {
            float since = age - RewardStampDur * RewardStampLandU();
            if (since <= 0f || since >= RewardShakeDur) return Vector2.zero;
            float k = 1f - since / RewardShakeDur;
            k *= k;
            float mag = 10f * s * k;
            return new Vector2(Mathf.Sin(since * 58f) * mag, Mathf.Sin(since * 43f) * mag * 0.42f);
        }

        // Oversized disc on the top-right corner. Higher streaks grow a little more.
        static Rect RewardStampRect(Rect board, int mul)
        {
            float sc = RewardStampScale(mul);
            float grow = Mathf.Lerp(1f, 1.12f, Mathf.Clamp01((sc - 1f) / 0.85f));
            float d = Mathf.Min(board.width * 0.46f, board.height * 0.70f) * grow;
            float inset = d * 0.22f;
            float cx = board.xMax - inset;
            float cy = board.y + inset;
            return new Rect(cx - d * 0.5f, cy - d * 0.5f, d, d);
        }

        static Texture2D RewardDiscTex()
        {
            if (_rewardDisc != null) return _rewardDisc;
            const int n = 96;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = "RewardDisc"
            };
            var px = new Color32[n * n];
            float c = n * 0.5f;
            float rad = c - 1.2f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = x + 0.5f - c;
                    float dy = y + 0.5f - c;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(rad - dist + 0.75f);
                    px[y * n + x] = a <= 0.004f
                        ? new Color32(0, 0, 0, 0)
                        : new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _rewardDisc = tex;
            return tex;
        }

        // REWARD at the full-open seat. openK is the appear/tuck scale only.
        // The lamp breathe stays on the frame and must not reach this rect.
        void DrawRewardRow(Rect rest, float appear, float alpha, GUIStyle labelSt, int labelHi, float openK, Vector2 pivot)
        {
            appear = Mathf.Clamp01(appear);
            if (appear <= 0.04f || rest.width < 4f || rest.height < 4f) return;
            labelSt.alignment = TextAnchor.MiddleLeft;
            labelSt.wordWrap = false;
            var shadow = rest;
            shadow.x += 2f;
            shadow.y += 2f;
            var shade = new Color(0.16f, 0.07f, 0.02f, appear * alpha * 0.62f);
            DrawPopupTitle(shadow, "REWARD", labelSt, shade, 12, labelHi, 0.92f, 0.90f, 0, -1, 0.16f, 0.001f, openK, pivot);
            var gold = new Color(1f, 0.91f, 0.58f, appear * alpha);
            DrawPopupTitle(rest, "REWARD", labelSt, gold, 12, labelHi, 0.92f, 0.90f, 0, -1, 0.16f, 0.001f, openK, pivot);
        }

        // Same pad as the live inner block, at scale 1, so the word lands on the
        // seat the card has when the open finishes.
        static Rect RewardWordRect(Rect face, float s, bool showPips)
        {
            float padX = Mathf.Max(8f * s, face.width * 0.055f);
            float padY = Mathf.Max(4f * s, face.height * 0.04f);
            var inner = new Rect(face.x + padX, face.y + padY, face.width - padX * 2f, face.height - padY * 2f);
            float pipH = 0f;
            if (showPips)
                pipH = Mathf.Min(20f * s, inner.height * 0.16f);
            float bodyH = Mathf.Max(8f, inner.height - pipH);
            float metaH = Mathf.Clamp(bodyH * 0.24f, 26f * s, bodyH * 0.32f);
            float linesTop = inner.y + pipH;
            float inX = Mathf.Max(8f, metaH * 0.42f);
            float down = Mathf.Max(5f, metaH * 0.26f);
            float labelW = Mathf.Max(8f, inner.width * 0.62f - inX);
            return new Rect(inner.x + inX, linesTop + down, labelW, metaH);
        }

        // sparkle 0 still lets x10 twinkle inside StreakBadge.Draw.
        void DrawRewardStamp(Rect r, int mul, float alpha, float age, float sparkle = 0f)
        {
            if (r.width < 8f || alpha < 0.02f || age < 0f) return;
            mul = StreakTier.Display(mul);
            float land = RewardStampDur * RewardStampLandU();
            if (GuiPaint() && !_rewardStampHit && age >= land)
            {
                _rewardStampHit = true;
                _waxBorn = Time.unscaledTime;
                Sfx.CardBump();
            }
            float pop = RewardStampPop(age);
            if (_stampPulse >= 0f)
            {
                float since = Time.unscaledTime - _stampPulse;
                if (since >= 0f && since < 0.22f)
                    pop *= 1f + 0.08f * Mathf.Sin(since / 0.22f * Mathf.PI);
            }
            var ink = StreakBadge.Colors(mul);
            if (_rewardStampStyle == null)
            {
                _rewardStampStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = false,
                    clipping = TextClipping.Overflow
                };
            }
            if (_rewardContent == null) _rewardContent = new GUIContent();
            string text = MulChip(mul);
            _rewardContent.text = text;
            var st = _rewardStampStyle;
            var content = _rewardContent;
            int rw = Mathf.RoundToInt(r.width / 16f);
            int rh = Mathf.RoundToInt(r.height / 16f);
            int key = mul * 131 + rw * 17 + rh;
            if (key != _stampKey || _stampPx < 12)
            {
                StreakBadge.NumeralFit(r, out float fitW, out float fitH);
                int hi = Mathf.Max(18, Mathf.RoundToInt(fitH));
                st.fontSize = FitFont(st, text, fitW, fitH, 12, hi);
                int guard = 0;
                while (st.fontSize > 12 && guard < 24 && st.CalcSize(content).x > fitW)
                {
                    st.fontSize -= 1;
                    guard++;
                }
                _stampPx = st.fontSize;
                _stampKey = key;
            }
            else
                st.fontSize = _stampPx;
            var prev = GUI.matrix;
            Vector2 pivot = r.center;
            GUIUtility.RotateAroundPivot(-15f, pivot);
            GUIUtility.ScaleAroundPivot(new Vector2(pop, pop), pivot);
            StreakBadge.Draw(r, text, st, ink, alpha, sparkle);
            float wax = _waxBorn < 0f ? 9f : Time.unscaledTime - _waxBorn;
            if (wax >= 0f && wax < 0.5f) DrawWaxSplat(r, ink.Fill, ink.Edge, wax, alpha);
            GUI.matrix = prev;
            GUI.color = Color.white;
        }

        // Wax droplets and a squished rim. Lives on the shared stamp, so home and garden both get it.
        static void DrawWaxSplat(Rect r, Color fill, Color edge, float age, float alpha)
        {
            float u = Mathf.Clamp01(age / 0.5f);
            float fade = (1f - u) * alpha;
            if (fade < 0.02f) return;
            var blob = RewardDiscTex();
            if (blob == null) return;
            float reach = Mathf.Lerp(r.width * 0.08f, r.width * 0.42f, u);
            float sz = Mathf.Lerp(r.width * 0.09f, r.width * 0.045f, u);
            var wax = Color.Lerp(fill, edge, 0.35f);
            wax.a = 0.85f * fade;
            const int n = 7;
            for (int i = 0; i < n; i++)
            {
                float ang = (i / (float)n) * Mathf.PI * 2f + 0.4f;
                float x = r.center.x + Mathf.Cos(ang) * reach - sz * 0.5f;
                float y = r.center.y + Mathf.Sin(ang) * reach * 0.72f - sz * 0.5f;
                GUI.color = wax;
                GUI.DrawTexture(new Rect(x, y, sz, sz * 0.82f), blob, ScaleMode.ScaleToFit, true);
            }
            float rimW = r.width * Mathf.Lerp(1.02f, 1.16f, u);
            float rimH = r.height * Mathf.Lerp(1.0f, 0.90f, u);
            var rim = new Rect(r.center.x - rimW * 0.5f, r.center.y - rimH * 0.5f, rimW, rimH);
            GUI.color = new Color(edge.r, edge.g, edge.b, 0.55f * fade);
            GUI.DrawTexture(rim, blob, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
        }

        // Garden play only. One gate for the draw, the glove keep-out, and the
        // caption drop. Lessons never hide the chip. Splash, an ad, and a
        // full-screen popup do. The finale smash is a world logo, so it does not.
        bool GardenStampLive()
        {
            return GardenStampVis.Shown(new GardenStampQuery
            {
                Splash = _splash,
                ChipArmed = _gardenStampAt >= 0f && StreakTier.AtStake(Purse.Streak),
                AdCover = Ads.IsShowing,
                FullPopup = StampPopupCover(),
                FinaleSmash = FinaleShow.Playing,
                Lesson = StampLessons()
            });
        }

        // Gift card, restart ask, the clear-reward board, and the bee-card inspect
        // each paint a wash over the whole glass. The level-hive panel does not.
        bool StampPopupCover()
        {
            if (_clearReward) return true;
            if (_gift != GiftFace.None) return true;
            if (_restartAsk != RestartAsk.None) return true;
            if (_hiveInspect >= 0) return true;
            return false;
        }

        // Every live lesson, passed through so the shared check can exempt them.
        GardenLesson StampLessons()
        {
            var lesson = GardenLesson.None;
            if (_pestCue == PestCueSparrow) lesson |= GardenLesson.Sparrow;
            if (_pestCue == PestCueHawk) lesson |= GardenLesson.Hawk;
            if (_pestCue == PestCueBee) lesson |= GardenLesson.Bee;
            if (_leafIntro) lesson |= GardenLesson.Leaf;
            if (BadgerGuideLive()) lesson |= GardenLesson.BadgerGuide;
            if (_bgLessonLive) lesson |= GardenLesson.BadgerLesson;
            if (_coach && _cueHand) lesson |= GardenLesson.Coach;
            if (_cueHand && _coachFade >= 0.03f && _cueHolePx >= 8f) lesson |= GardenLesson.CoachDim;
            if (_hiveLevelLive) lesson |= GardenLesson.HiveGarden;
            if (_hiveIntroLive) lesson |= GardenLesson.HiveHome;
            if (_hiveTutorOn) lesson |= GardenLesson.HiveAlbum;
            if (_pokerIntroLive) lesson |= GardenLesson.PokerHome;
            if (_pokerPageOn) lesson |= GardenLesson.PokerPage;
            if (_pokerDealHint) lesson |= GardenLesson.PokerDeal;
            if (PokerBackGateLive()) lesson |= GardenLesson.PokerBack;
            if (_dailyIntroLive) lesson |= GardenLesson.Daily;
            if (_adHand) lesson |= GardenLesson.AdHand;
            if (_welcomeGlove) lesson |= GardenLesson.Welcome;
            if (_adoptLive) lesson |= GardenLesson.Adopt;
            if (_cueGift) lesson |= GardenLesson.GiftCue;
            if (_albumTutorOn) lesson |= GardenLesson.Album;
            if (_upgradeTutorOn) lesson |= GardenLesson.Upgrade;
            if (_tutorPause != 0) lesson |= GardenLesson.TutorPause;
            if (!string.IsNullOrEmpty(_cueLine)) lesson |= GardenLesson.CoachCaption;
            return lesson;
        }

        // Top-left chip, about 1.7× the previous 81px disc (66..117). TopHud keeps it
        // under the notch / Dynamic Island. The cap stays clear of the feeders.
        // Glints stay inside this rect. The reward-stamp pop keeps its own rect.
        Rect GardenStampRect(float s)
        {
            float d = Mathf.Clamp(138f * s, 112f, 190f);
            float x = Mathf.Max(10f * s, Screen.safeArea.xMin + 8f);
            float y = TopHud() + 10f * s;
            var safe = Screen.safeArea;
            if (safe.width > 8f && safe.height > 8f)
            {
                float maxX = safe.xMax - 6f;
                if (x + d > maxX) x = Mathf.Max(safe.xMin + 4f, maxX - d);
                float minY = Screen.height - safe.yMax + 4f;
                if (y < minY) y = minY;
            }
            return new Rect(x, y, d, d);
        }

        void DrawGardenStamp(float s)
        {
            _gardenStampRect = default;
            if (!GardenStampLive()) return;
            // A leftover veil tint must not fade the chip. Lessons draw it at full alpha.
            GUI.color = Color.white;
            var r = GardenStampRect(s);
            _gardenStampRect = r;
            float age = Time.unscaledTime - _gardenStampAt;
            // Gentle twinkle on every tier. x10 adds the gold sweep inside Draw.
            DrawRewardStamp(r, StreakTier.Display(Purse.Streak), 1f, age, 0.48f);
        }

        void DrawRestartAsk(float s)
        {
            if (_restartAsk == RestartAsk.None) return;
            GUI.color = new Color(0.04f, 0.03f, 0.02f, 0.78f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            float w = StandardPopupWidth(s);
            bool sure = _restartAsk == RestartAsk.Sure;
            float h = sure ? Mathf.Max(220f * s, w * 0.46f) : Mathf.Max(300f * s, w * 0.58f);
            float capH = Screen.height * 0.62f;
            if (h > capH) h = capH;
            var card = PlacePopup(s, w, h, 0.42f);
            float band = Mathf.Clamp(w * 0.045f, 10f * s, 18f * s);
            var board = new Rect(card.x + band, card.y + band, Mathf.Max(8f, card.width - band * 2f), Mathf.Max(8f, card.height - band * 2f));
            DrawDailyFrame(card, board, band, s, 0.5f, GlowTex());
            var inner = new Rect(board.x + 8f * s, board.y + 8f * s, Mathf.Max(8f, board.width - 16f * s), Mathf.Max(8f, board.height - 16f * s));
            if (_giftTitle == null)
                _giftTitle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            var st = _giftTitle;
            float bw = inner.width * 0.44f;
            float bh = Mathf.Max(PopupBarHeight(s, w) * 1.28f, 56f * s);
            if (sure)
            {
                const string ask = "Are you sure you want to restart?";
                var titleR = new Rect(inner.x, inner.y, inner.width, Mathf.Max(40f, inner.height - bh - 12f * s));
                st.wordWrap = true;
                int hi = Mathf.Max(20, Mathf.RoundToInt(36f * s));
                st.fontSize = FitFontWrapped(st, ask, titleR.width, titleR.height * 0.9f, 16, hi);
                StampOutlined(titleR, ask, st, new Color(1f, 0.97f, 0.90f, 1f), 0, Mathf.Max(3, Mathf.RoundToInt(st.fontSize * 0.12f)));
                var noR = new Rect(inner.x, inner.yMax - bh, bw, bh);
                var yesR = new Rect(inner.xMax - bw, inner.yMax - bh, bw, bh);
                bool no = HitPad(noR, out bool noHeld);
                bool yes = HitPad(yesR, out bool yesHeld);
                DrawPopupButton(noR, noHeld, PopupTint.Red);
                DrawPopupButton(yesR, yesHeld, PopupTint.Green);
                DrawPopupWord(noR, "No", st, s);
                DrawPopupWord(yesR, "Yes", st, s);
                if (no) { _restartAsk = RestartAsk.None; Sfx.CardTap(); return; }
                if (yes)
                {
                    Purse.BreakStreak();
                    _restartAsk = RestartAsk.Keep;
                    Sfx.CardTap();
                }
                return;
            }
            var head = new Rect(inner.x, inner.y, inner.width, Mathf.Max(48f, inner.height - bh - 10f * s));
            DrawStreakCaution(head, s, st);
            var noThanks = new Rect(inner.x, inner.yMax - bh, bw, bh);
            var watchR = new Rect(inner.xMax - bw, inner.yMax - bh, bw, bh);
            float xSz = Mathf.Max(40f, 42f * s);
            var xBtn = new Rect(card.xMax - xSz - 6f, card.y + 6f, xSz, xSz);
            bool decline = HitPad(noThanks, out bool declineHeld);
            bool watch = HitPad(watchR, out bool watchHeld);
            bool close = HitPad(CloseTapRect(xBtn), out bool xHeld);
            DrawPopupButton(noThanks, declineHeld, PopupTint.Red);
            DrawPopupButton(watchR, watchHeld, PopupTint.Green);
            DrawPopupWord(noThanks, "No thanks", st, s);
            DrawPopupWord(watchR, "Watch Ad", st, s);
            DrawGiftCloseX(xBtn, xHeld, s);
            if (watch)
            {
                StartCoroutine(WatchKeepStreak());
                return;
            }
            if (decline || close) StartCoroutine(FinishRestart(false));
        }

        // White face, dark outline, about 60% of the drawn button. Hit rect stays the button.
        static void DrawPopupWord(Rect btn, string word, GUIStyle st, float s)
        {
            var lab = new Rect(btn.x + btn.width * 0.04f, btn.y + btn.height * 0.20f, btn.width * 0.92f, btn.height * 0.60f);
            int cap = Mathf.Max(18, Mathf.RoundToInt(34f * s));
            st.wordWrap = false;
            st.alignment = TextAnchor.MiddleCenter;
            st.fontStyle = FontStyle.Bold;
            st.fontSize = FitFont(st, word, lab.width, lab.height, 14, cap);
            int dark = Mathf.Clamp(Mathf.RoundToInt(st.fontSize * 0.16f), 2, 8);
            StampOutlined(lab, word, st, Color.white, 0, dark);
        }

        IEnumerator WatchKeepStreak()
        {
            if (_restartAd) yield break;
            _restartAd = true;
            int saved = _restartSavedMul;
            yield return Ads.Rewarded();
            _restartAd = false;
            _restartAsk = RestartAsk.None;
            if (Ads.LastEarned && saved >= 2) Purse.SetStreak(saved);
            yield return SnapRound();
        }

        IEnumerator FinishRestart(bool keep)
        {
            _restartAsk = RestartAsk.None;
            if (keep && _restartSavedMul >= 2) Purse.SetStreak(_restartSavedMul);
            yield return SnapRound();
        }

        void DrawStreakToast(float s, Rect pig)
        {
            if (_clearReward || _streakSlide < 0f) return;
            if (Purse.Streak <= 0)
            {
                DismissStreakSign();
                return;
            }

            int streak;
            int fromPay;
            int toPay;
            RewardPays(out streak, out fromPay, out toPay);
            var beat = RewardBeats(RewardMulOn(streak, fromPay, toPay));
            if (_streakSlide >= 1f)
            {
                if (!RewardPayBusy()) DismissStreakSign();
                GUI.color = Color.white;
                return;
            }

            float t = _streakSlide * beat.SeqDur;
            // Shell fades in with REWARD. The amount waits until that fade finishes.
            float wordAppear = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - beat.WordAt) / RewardWordDur));
            float amtAppear = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - beat.AmtAt) / RewardAmtDur));
            float tuck = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(beat.HoldEnd, beat.SeqDur, t));
            float alpha = wordAppear * (1f - tuck);

            float openK = Mathf.Lerp(0.82f, 1f, wordAppear);
            // Title scale is the open and the tuck. Soft breathe stays on the frame glow.
            float titleK = Mathf.Lerp(openK, 0.72f, tuck);
            float scale = openK;
            // Freeze the glow clock on the way out so breathe cannot flash after the board thins.
            float lampT = Time.unscaledTime - Mathf.Max(0f, t - beat.HoldEnd);
            if (t >= beat.AmtAt)
            {
                float breathe = 0.5f + 0.5f * Mathf.Sin(lampT * 2.6f);
                scale *= 1f + 0.018f * breathe * (1f - tuck);
            }
            // Rect scale (not GUI.matrix) so nested StampFit pivots stay correct.
            scale = Mathf.Lerp(scale, 0.72f, tuck);

            float stampAge = t - beat.StampAt;
            bool stampLive = beat.Mul && stampAge >= 0f;

            // Build 58 dropped the marquee bulbs (kept: no bulbs) but also dropped the bulb
            // reserve from the fit loop, so the board converged on the whole band (~1.4-1.7x
            // the build 57 board, not the intended ~12%). Build 59: the loop keeps that reserve
            // as plain air (old glass geometry), which lands exactly on the build 57 rest face,
            // then restScale sizes it from there (b59 0.95, b61 1.05: ~10% up on b59).
            // Sparkles ride the brass edge only.
            const float bandFrac = 0.046f;
            const float maxPop = 1.06f;
            const float airGlass = 1.22f;    // old bulb glassOut, now just air around the band
            const float restScale = 1.05f;   // ~5% over the build 57 board (b59-60 had 0.95)

            // The payout owns the band under the wordmark, inside the safe area.
            // Bottom stops at the play-disc top (the disc starts ~10% into the bloom).
            // Read safe area every pass so a late inset still keeps the card clear.
            float titleBottom = TopHud() + (56f * 2f + 4f) * s;
            float gap = 10f * s;
            var safe = Screen.safeArea;
            bool safeOk = safe.width > 2f && safe.height > 2f;
            float safeL = safeOk ? safe.xMin : 0f;
            float safeR = safeOk ? safe.xMax : Screen.width;
            float leftLimit = safeL + gap;
            float rightLimit = safeR - gap;
            if (rightLimit - leftLimit < 120f * s)
            {
                leftLimit = safeL + 4f;
                rightLimit = Mathf.Max(leftLimit + 120f * s, safeR - 4f);
            }
            var playBloom = FlowerPlayRect();
            float botPad = Screen.height - playBloom.yMax;
            float flowerTop = playBloom.y;
            float topLimit = titleBottom + gap;
            float botLimit = Mathf.Min(Screen.height - botPad - 6f, flowerTop + playBloom.height * 0.06f);
            if (botLimit < topLimit + 88f * s)
                botLimit = Mathf.Min(Screen.height - botPad - 6f, topLimit + 88f * s);
            float faceW = Mathf.Max(120f * s, rightLimit - leftLimit) / maxPop;
            float faceH = Mathf.Max(72f * s, botLimit - topLimit) / maxPop;

            // Board face: same fit as build 57 (air reserve where the bulb glass was), so a
            // short band between the wordmark and the play disc still shrinks the air first.
            float restW = faceW * 0.66f;
            float restH = faceH * 0.58f;
            for (int fit = 0; fit < 8; fit++)
            {
                float airSz = Mathf.Min(restH * 0.36f, restW * 0.22f);
                float margin = airGlass * airSz + restW * bandFrac * 0.5f;
                float floorH = 64f * s;
                if (faceH - 2f * margin < floorH)
                {
                    margin = Mathf.Max(5f * s, (faceH - floorH) * 0.5f);
                    airSz = Mathf.Max(7f * s, (margin - restW * bandFrac * 0.5f) / airGlass);
                    margin = airGlass * airSz + restW * bandFrac * 0.5f;
                }
                float nextW = Mathf.Max(72f * s, faceW - 2f * margin);
                float nextH = Mathf.Max(52f * s, faceH - 2f * margin);
                if (nextW > nextH * 2.15f) nextW = nextH * 2.15f;
                if (nextH > nextW * 0.90f) nextH = nextW * 0.90f;
                restW = Mathf.Lerp(restW, nextW, 0.65f);
                restH = Mathf.Lerp(restH, nextH, 0.65f);
            }
            {
                float airSz = Mathf.Min(restH * 0.36f, restW * 0.22f);
                float air = airGlass * airSz + restW * bandFrac * 0.5f;
                float visW = restW + 2f * air;
                float visH = restH + 2f * air;
                float squeeze = Mathf.Min(1f, faceW / Mathf.Max(1f, visW), faceH / Mathf.Max(1f, visH));
                if (squeeze < 0.999f)
                {
                    restW *= squeeze;
                    restH *= squeeze;
                }
            }
            restW *= restScale;
            restH *= restScale;
            {
                // Fit guard for the larger board: brass band and full pop stay inside the band
                // between the wordmark and the play disc (no-op on iPhone SE / 15 / Pro Max).
                float fitK = Mathf.Min(1f,
                    (rightLimit - leftLimit) / Mathf.Max(1f, maxPop * restW * (1f + 2f * bandFrac)),
                    (botLimit - topLimit) / Mathf.Max(1f, maxPop * (restH + 2f * restW * bandFrac)));
                restW *= fitK;
                restH *= fitK;
            }
            float bandPad = restW * bandFrac;
            // Soft aura / combo halo as a fraction of the board (replaces old bulb crownFrac).
            float crownFrac = 0.10f;

            float cx = (leftLimit + rightLimit) * 0.5f;
            float halfVis = (restW * 0.5f + bandPad) * maxPop;
            float minCx = leftLimit + halfVis;
            float maxCx = rightLimit - halfVis;
            cx = maxCx >= minCx ? Mathf.Clamp(cx, minCx, maxCx) : (leftLimit + rightLimit) * 0.5f;
            float centerY = (topLimit + botLimit) * 0.5f;
            float visualBottom = centerY + maxPop * (restH * 0.5f + bandPad);
            if (visualBottom > botLimit) centerY -= visualBottom - botLimit;
            float visualTop = centerY - maxPop * (restH * 0.5f + bandPad);
            if (visualTop < topLimit) centerY += topLimit - visualTop;
            float restX = cx - restW * 0.5f;
            float restY = centerY - restH * 0.5f;
            var pigC = pig.center;
            float x = Mathf.Lerp(restX, pigC.x - restW * 0.22f, tuck);
            float y = Mathf.Lerp(restY, pigC.y - restH * 0.35f, tuck);
            var kick = stampLive ? RewardCardShake(stampAge, s) : Vector2.zero;
            var board = new Rect(x, y, restW, restH);
            var c = board.center;
            board.width *= scale;
            board.height *= scale;
            board.x = c.x - board.width * 0.5f + kick.x;
            board.y = c.y - board.height * 0.5f + kick.y;

            var glow = GlowTex();
            float glowA = 0.10f + 0.08f * (0.5f + 0.5f * Mathf.Sin(lampT * 2.3f));
            GUI.color = new Color(1f, 0.82f, 0.28f, glowA * alpha);
            // Aura scales and tucks with the sign.
            float aura = board.width * crownFrac * 0.55f;
            GUI.DrawTexture(new Rect(board.x - aura, board.y - aura * 0.6f, board.width + aura * 2f, board.height + aura * 1.2f), glow, ScaleMode.ScaleToFit, true);

            // Brass frame band. Thickness tracks the animated board so tuck shrinks it.
            float band = board.width * bandFrac;
            var outer = new Rect(board.x - band, board.y - band, board.width + band * 2f, board.height + band * 2f);
            float sh = 3f * s * scale;
            GUI.color = new Color(0f, 0f, 0f, 0.45f * alpha); // drop shadow
            GUI.DrawTexture(new Rect(outer.x + sh, outer.y + sh * 1.6f, outer.width, outer.height), Texture2D.whiteTexture);
            GUI.color = new Color(0.30f, 0.16f, 0.05f, alpha); // band body
            GUI.DrawTexture(outer, Texture2D.whiteTexture);
            GUI.color = new Color(0.58f, 0.36f, 0.12f, alpha); // band face
            float face = 2f * s * scale;
            GUI.DrawTexture(new Rect(outer.x + face, outer.y + face, outer.width - face * 2f, outer.height - face * 2f), Texture2D.whiteTexture);
            float edge = Mathf.Max(1f, 1.6f * s * scale);
            GUI.color = new Color(1f, 0.86f, 0.46f, 0.85f * alpha); // outer top/left bevel light
            GUI.DrawTexture(new Rect(outer.x + face, outer.y + face, outer.width - face * 2f, edge), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(outer.x + face, outer.y + face, edge, outer.height - face * 2f), Texture2D.whiteTexture);
            GUI.color = new Color(0.14f, 0.07f, 0.02f, 0.9f * alpha); // outer bottom/right bevel shade
            GUI.DrawTexture(new Rect(outer.x + face, outer.yMax - face - edge, outer.width - face * 2f, edge), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(outer.xMax - face - edge, outer.y + face, edge, outer.height - face * 2f), Texture2D.whiteTexture);

            GUI.color = new Color(0.04f, 0.02f, 0.01f, alpha);
            GUI.DrawTexture(board, Texture2D.whiteTexture);
            GUI.color = new Color(1f, 0.78f, 0.22f, (0.18f + 0.10f * (0.5f + 0.5f * Mathf.Sin(lampT * 2.05f))) * alpha);
            float rim = 3f * s * scale;
            float line = 2f * s * scale;
            GUI.DrawTexture(new Rect(board.x + rim, board.y + rim, board.width - rim * 2f, line), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(board.x + rim, board.yMax - rim - line, board.width - rim * 2f, line), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(board.x + rim, board.y + rim, line, board.height - rim * 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(board.xMax - rim - line, board.y + rim, line, board.height - rim * 2f), Texture2D.whiteTexture);
            GUI.color = new Color(0f, 0f, 0f, 0.55f * alpha); // inner shadow where board meets band
            float shade = 2f * s * scale;
            GUI.DrawTexture(new Rect(board.x, board.y, board.width, shade), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(board.x, board.y, shade, board.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Build 52 approach: no DrawGiftMarquee / no marquee bulbs on this frame.
            // Twinkles sit on the brass band's outer edge only (shared SparkleFx border
            // mode): capped under 2x the band, so none reaches the dark board or the text.
            if (alpha > 0.05f)
                SparkleFx.DrawBorder(outer, band, alpha * 0.70f, gold: true, sizeFrac: 0.095f);

            float padX = Mathf.Max(8f * s * scale, board.width * 0.055f);
            float padY = Mathf.Max(4f * s * scale, board.height * 0.04f);
            var inner = new Rect(board.x + padX, board.y + padY, board.width - padX * 2f, board.height - padY * 2f);

            // Row count from the resting face so pop/tuck cannot swap the layout.
            // A caption row wants ~18*s so a 14*s glyph fits; the payout keeps the rest.
            float lineH = 18f * s;
            float padRest = Mathf.Max(4f * s, restH * 0.04f);
            float innerRest = Mathf.Max(24f * s, restH - padRest * 2f);
            float pipNeed = 20f * s;
            bool showPips = innerRest >= lineH + 56f * s + pipNeed;
            float pipH = 0f;
            if (showPips)
            {
                pipH = Mathf.Min(pipNeed * scale, inner.height * 0.16f);
                float pipU = Mathf.Clamp01((t - beat.WordAt) / RewardWordDur);
                DrawStreakPips(new Rect(inner.x, inner.y, inner.width, pipH), streak, pipU, alpha, s);
            }

            float bodyH = Mathf.Max(8f, inner.height - pipH);
            float metaH = Mathf.Clamp(bodyH * 0.24f, 26f * s * Mathf.Clamp(scale, 0.72f, 1.06f), bodyH * 0.32f);
            float heroH = bodyH - metaH;
            float linesTop = inner.y + pipH;

            var labelSt = GuiPool.Label(GuiSlot.StreakLabel);
            labelSt.fontStyle = FontStyle.Bold;
            labelSt.alignment = TextAnchor.MiddleLeft;
            labelSt.wordWrap = false;
            var heroSt = GuiPool.From(GuiSlot.StreakHero, labelSt);
            heroSt.alignment = TextAnchor.MiddleCenter;
            // Caps follow the board: build 57's 38/52 times restScale (b58 had 42/58, b59 36/50).
            int labelHi = Mathf.Max(20, Mathf.RoundToInt(38f * restScale * s));
            int heroHi = Mathf.Max(28, Mathf.RoundToInt(52f * restScale * s));
            var heroInk = new Color(1f, 0.95f, 0.42f, 1f);

            void StampFit(Rect r, string text, GUIStyle st, Color fill, int hi, float widthFrac, float inkA)
            {
                if (r.width < 4f || r.height < 4f || string.IsNullOrEmpty(text)) return;
                st.wordWrap = false;
                float fitW = Mathf.Max(4f, r.width * widthFrac);
                float fitH = r.height * 0.62f;
                st.fontSize = FitFont(st, text, fitW, fitH, 8, hi);
                int dark = Mathf.Clamp(Mathf.RoundToInt(st.fontSize * 0.14f), 2, 6);
                int light = Mathf.Clamp(Mathf.RoundToInt(st.fontSize * 0.05f), 1, 3);
                var ink = fill;
                ink.a *= inkA;
                StampOutlined(r, text, st, ink, light, dark, 0.001f);
            }

            var wordFace = new Rect(x + kick.x, y + kick.y, restW, restH);
            var wordRest = RewardWordRect(wordFace, s, showPips);
            DrawRewardRow(wordRest, wordAppear, 1f - tuck, labelSt, labelHi, titleK, wordFace.center);

            int shown = toPay;
            float roll = 1f;
            if (beat.Mul)
            {
                float sinceRoll = t - beat.RollAt;
                if (sinceRoll <= 0f)
                {
                    shown = fromPay;
                    roll = 0f;
                }
                else
                {
                    roll = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(sinceRoll / RewardRollDur));
                    shown = Mathf.RoundToInt(Mathf.Lerp(fromPay, toPay, roll));
                    int bucket = roll >= 0.999f ? 8 : Mathf.Clamp(Mathf.FloorToInt(roll * 8f), 0, 7);
                    if (GuiPaint() && bucket != _streakTickBucket)
                    {
                        _streakTickBucket = bucket;
                        if (bucket > 0) Sfx.Clink();
                    }
                }
            }
            _streakRollShown = shown;
            float heroA = alpha * amtAppear;
            if (heroA > 0.02f)
            {
                var hero = new Rect(inner.x, linesTop + metaH, inner.width, heroH);
                string amt = Money.Format(shown);
                float punch = 1f + 0.08f * Mathf.Sin(Mathf.Clamp01((roll - 0.85f) / 0.15f) * Mathf.PI);
                var prevM = GUI.matrix;
                GUIUtility.ScaleAroundPivot(new Vector2(punch, punch), hero.center);
                float glowPad = 10f * s * scale;
                GUI.color = new Color(1f, 0.78f, 0.18f, (0.22f + 0.28f * roll) * heroA);
                GUI.DrawTexture(new Rect(hero.x - glowPad, hero.y - glowPad * 0.35f, hero.width + glowPad * 2f, hero.height + glowPad), glow, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
                StampFit(hero, amt, heroSt, heroInk, heroHi, 0.66f, heroA);
                GUI.matrix = prevM;
            }
            if (stampLive)
                DrawRewardStamp(RewardStampRect(board, streak), streak, alpha, stampAge);
            float halo = board.width * crownFrac + aura;
            NoteComboStreak(new Rect(board.x - halo, board.y - halo, board.width + halo * 2f, board.height + halo * 2f));
            GUI.color = Color.white;
        }

        static void DrawStreakPips(Rect row, int streak, float u, float alpha, float s)
        {
            const int slots = 6;
            int lit = Mathf.Clamp(StreakTier.PipCount(streak), 0, slots);
            string more = null;
            // Gap and diameter come from the row, which is already inside the scaled sign.
            float gap = Mathf.Clamp(row.height * 0.28f, 3f, Mathf.Max(3f, row.width * 0.05f));
            float labelW = 0f;
            if (more != null)
                labelW = Mathf.Min(row.width * 0.30f, row.height * (0.52f * more.Length + 0.40f));
            float slotsW = Mathf.Max(slots * 4f, row.width - labelW - (more != null ? gap : 0f));
            float maxGap = (slotsW * 0.42f) / (slots - 1);
            if (gap > maxGap) gap = maxGap;
            float d = (slotsW - gap * (slots - 1)) / slots;
            float dCap = row.height * 0.72f;
            if (d > dCap) d = dCap;
            if (d < 4f) d = 4f;
            float used = slots * d + (slots - 1) * gap + (more != null ? gap + labelW : 0f);
            float x0 = row.x + Mathf.Max(0f, (row.width - used) * 0.5f);
            var glow = GlowTex();
            for (int i = 0; i < slots; i++)
            {
                float at = 0.10f + 0.016f * i;
                float on = i < lit ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - at) / 0.05f)) : 0f;
                var r = new Rect(x0 + i * (d + gap), row.y + (row.height - d) * 0.5f, d, d);
                GUI.color = new Color(0.18f, 0.10f, 0.04f, 0.85f * alpha);
                GUI.DrawTexture(r, glow, ScaleMode.ScaleToFit, true);
                var inner = new Rect(r.x + d * 0.18f, r.y + d * 0.18f, d * 0.64f, d * 0.64f);
                GUI.color = Color.Lerp(new Color(0.45f, 0.28f, 0.10f, 0.55f * alpha), new Color(1f, 0.90f, 0.38f, alpha), on);
                GUI.DrawTexture(inner, glow, ScaleMode.ScaleToFit, true);
                if (on > 0.4f)
                {
                    GUI.color = new Color(1f, 0.86f, 0.32f, 0.55f * on * alpha);
                    GUI.DrawTexture(new Rect(r.x - d * 0.22f, r.y - d * 0.22f, d * 1.44f, d * 1.44f), glow, ScaleMode.ScaleToFit, true);
                }
            }
            GUI.color = Color.white;
            if (more != null)
            {
                var extra = GuiPool.Label(GuiSlot.StreakPipsExtra);
                extra.fontStyle = FontStyle.Bold;
                extra.alignment = TextAnchor.MiddleLeft;
                extra.wordWrap = false;
                var lab = new Rect(x0 + slots * (d + gap), row.y, labelW, row.height);
                if (lab.xMax > row.xMax) lab.x = row.xMax - lab.width;
                int floor = Mathf.Max(8, Mathf.RoundToInt(14f * s));
                int hi = Mathf.Max(floor, Mathf.RoundToInt(lab.height * 0.78f));
                extra.fontSize = FitFont(extra, more, lab.width * 0.96f, lab.height * 0.88f, 8, hi);
                int dark = Mathf.Max(2, Mathf.RoundToInt(extra.fontSize * 0.14f));
                StampOutlined(lab, more, extra, new Color(1f, 0.95f, 0.42f, alpha), 1, dark, 0.001f);
            }
        }

        // Left inset shared by garden HUD text and the poker win banner.
        static float HudTextLeft(float s) => Mathf.Max(16f * s, Screen.safeArea.xMin + 12f * s);

        void DrawRemainingBirds(float s)
        {
            if (_board == null) return;
            float safeTop = TopHud();
            float icon = 36f * s;
            float pad = 10f * s;
            var st = GuiPool.Label(GuiSlot.RemainingBirds);
            st.fontStyle = FontStyle.Bold;
            st.alignment = TextAnchor.MiddleLeft;
            st.wordWrap = false;
            string tx = GuiPool.Num(GuiSlot.RemainingBirds, "×", _board.RemainingBirds);
            st.fontSize = Mathf.RoundToInt(28 * s);
            float tw = st.CalcSize(GuiPool.Text(GuiText.RemainingBirds, tx)).x;
            float x = HudTextLeft(s);
            float y = safeTop;
            var bird = SpriteCatalog.Bird(BirdColor.Gold);
            if (bird != null && bird.texture != null)
                GUI.DrawTexture(new Rect(x, y, icon, icon), bird.texture, ScaleMode.ScaleToFit, true);
            else
            {
                // Fallback if bird art missing — still readable chip.
                st.alignment = TextAnchor.MiddleCenter;
                StampOutlined(new Rect(x, y, icon, icon), "🦅", st, new Color(0.36f, 0.18f, 0.07f), 2, 1);
            }
            StampOutlined(new Rect(x + icon + pad * 0.4f, y, tw + 8f, icon), tx, st, new Color(0.36f, 0.18f, 0.07f), 2, 1);
        }

        void DrawHudPurse(float s)
        {
            // In-garden: restart arrow only — purse $ lives on splash / poker.
        }

        // Saved balance, minus coins still in the air and coins a reveal still holds
        // (Purse.HoldDisplay / CreditHeld).
        int CoinsShown()
        {
            int n = Purse.DisplayCoins - _coinInFlight;
            if (n < 0) n = 0;
            return n;
        }

        // What the purse label draws. During a pay it ticks; otherwise it matches the save.
        int CoinsDrawn()
        {
            int goal = CoinsShown();
            if (_coinTick < 0) _coinTick = goal;
            if (!GuiPaint()) return _rewardPayLive ? _coinTick : goal;
            if (_coinTickFrame != Time.frameCount)
            {
                _coinTickFrame = Time.frameCount;
                StepCoinTick(goal);
            }
            return _rewardPayLive ? _coinTick : goal;
        }

        void StepCoinTick(int goal)
        {
            if (!_rewardPayLive)
            {
                _coinTick = goal;
                _coinRollDur = 0f;
                return;
            }
            if (_coinRollDur > 0f)
            {
                // Held-coin reveal: one eased roll from the old label to the new total.
                float slice = PlayClock.Delta;
                if (slice > 0f) _coinRollT += slice;
                float u = Mathf.Clamp01(_coinRollT / Mathf.Max(0.05f, _coinRollDur * RewardPace));
                float e = 1f - (1f - u) * (1f - u) * (1f - u);
                if (u >= 1f || goal <= _coinRollFrom)
                {
                    _coinTick = goal;
                    _coinRollDur = 0f;
                }
                else
                    _coinTick = _coinRollFrom + (int)((long)(goal - _coinRollFrom) * (long)Mathf.RoundToInt(e * 10000f) / 10000L);
            }
            if (goal < _coinTick) _coinTick = goal;
            if (_coinRollDur <= 0f && _coinTick != goal)
            {
                int delta = goal - _coinTick;
                float dur = 0.45f * RewardPace;
                if (dur < 0.05f) dur = 0.05f;
                float slice = PlayClock.Delta;
                if (slice <= 0f) return;
                int step = Mathf.CeilToInt(Mathf.Abs(delta) * slice / dur);
                if (step < 1) step = 1;
                if (delta > 0) _coinTick += step;
                else _coinTick -= step;
                if ((delta > 0 && _coinTick > goal) || (delta < 0 && _coinTick < goal))
                    _coinTick = goal;
            }
            if (_coinTick == goal && _flies.Count == 0 && _coinInFlight == 0 && _rewardHoldLeft <= 0f && !Sfx.PayoutBusy)
                _rewardPayLive = false;
        }

        // Shared reveal for coins a feature credited earlier under Purse.CreditHeld /
        // HoldDisplay: releases amount (all held when < 0) and rolls the purse label up from
        // what it shows now to the new total over rollSeconds (RewardPace stretches it).
        // No flying sprites; the feature's own celebration is the visual.
        void RevealHeldCoins(int amount, float rollSeconds)
        {
            int from = CoinsDrawn();
            if (Purse.ReleaseDisplay(amount) <= 0) return;
            _rewardPayLive = true;
            _coinTick = from;
            _coinRollFrom = from;
            _coinRollT = 0f;
            _coinRollDur = Mathf.Max(0.2f, rollSeconds);
        }

        // Splash is gone, so the flight cannot finish on the balance. Show the saved total.
        void FoldCoinFlies()
        {
            if (_flies.Count == 0 && _coinInFlight == 0) return;
            _flies.Clear();
            _coinInFlight = 0;
            _coinTick = CoinsShown();
            if (_rewardHoldLeft <= 0f && !Sfx.PayoutBusy) _rewardPayLive = false;
        }

        // Shared award. FlyCoinsToBalance carries the sprites. holdSeconds is a beat
        // that RewardPace stretches (gift thanks). Zero for a coin pay.
        void BeginRewardPay(int coins, float holdSeconds = 0f)
        {
            _rewardPayLive = true;
            float paced = holdSeconds * RewardPace;
            if (paced > _rewardHoldLeft) _rewardHoldLeft = paced;
            if (coins > 0) FlyCoinsToBalance(coins);
        }

        void StepRewardHold()
        {
            if (_rewardHoldFrame == Time.frameCount) return;
            _rewardHoldFrame = Time.frameCount;
            if (_rewardHoldLeft <= 0f || GamePause.Paused) return;
            _rewardHoldLeft -= Time.deltaTime;
            if (_rewardHoldLeft < 0f) _rewardHoldLeft = 0f;
        }

        // True while coins are in the air, the balance is still counting, the
        // payout sound is still playing, or a reward beat has time left.
        bool RewardPayBusy()
        {
            StepRewardHold();
            bool ticking = _rewardPayLive && _coinTick >= 0 && _coinTick != CoinsShown();
            bool busy = _rewardHoldLeft > 0f
                || _flies.Count > 0
                || _coinInFlight > 0
                || ticking
                || Sfx.PayoutBusy;
            if (!busy) _rewardPayLive = false;
            return busy;
        }

        // One wait. Callers fade or open the next segment only after this returns.
        // gapAfter adds RewardGap, the named breath before the next home lesson.
        IEnumerator WaitRewardPay(bool gapAfter = false)
        {
            float guard = 0f;
            while (RewardPayBusy() && guard < 45f)
            {
                guard += PlayClock.Delta;
                yield return null;
            }
            if (RewardPayBusy()) FinishRewardPay();
            if (!gapAfter) yield break;
            float breath = 0f;
            while (breath < RewardGap)
            {
                breath += PlayClock.Delta;
                yield return null;
            }
        }

        void FinishRewardPay()
        {
            _flies.Clear();
            _coinInFlight = 0;
            _rewardHoldLeft = 0f;
            _coinRollDur = 0f;
            _coinTick = CoinsShown();
            Sfx.ClearPayout();
            _rewardPayLive = false;
        }

        // Not the streak payoff. Those coins launch on the homepage sign.
        void DrawClearReward()
        {
            float s = Mathf.Max(Screen.height / 720f, 1f);
            GUI.color = new Color(0.05f, 0.04f, 0.03f, 0.45f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            DrawStreakRewards(s);
        }

        // Streak sign and daily claim. The purse already has the coins; each sprite
        // carries an even share, and the balance counts that share when it lands.
        void FlyCoinsToBalance(int coins)
        {
            int n = Mathf.Clamp(coins, 0, 12);
            if (n <= 0) return;
            int share = coins / n;
            int extra = coins - share * n;
            var pig = PiggyRect(Mathf.Max(Screen.height / 720f, 1f));
            var dest = new Vector2(pig.x + pig.width * 0.5f, pig.y + pig.height * 0.42f);
            _coinInFlight += coins;
            for (int i = 0; i < n; i++)
            {
                int worth = share;
                if (i < extra) worth += 1;
                _flies.Add(new CoinFly
                {
                    A = new Vector2(Screen.width * 0.5f + Random.Range(-70f, 70f), Screen.height * 0.30f + Random.Range(-24f, 36f)),
                    B = dest,
                    T = 0f,
                    Delay = i * 0.10f * RewardPace,
                    Spin = Random.Range(300f, 420f) * (Random.value < 0.5f ? -1f : 1f),
                    Worth = worth
                });
            }
        }

        void DrawCoinFly(Rect pig)
        {
            if (_flies.Count == 0)
            {
                _coinInFlight = 0;
                return;
            }
            var spr = SpriteCatalog.Coin;
            var tex = spr != null ? spr.texture : null;
            var glow = GlowTex();
            float dt = Time.unscaledDeltaTime;
            float size = 68f * Mathf.Max(Screen.height / 720f, 1f);
            var dest = new Vector2(pig.x + pig.width * 0.5f, pig.y + pig.height * 0.42f);
            for (int i = _flies.Count - 1; i >= 0; i--)
            {
                var f = _flies[i];
                f.Delay -= dt;
                if (f.Delay > 0f)
                {
                    _flies[i] = f;
                    continue;
                }
                f.T += dt / (1.32f * RewardPace);
                float u = Mathf.Clamp01(f.T);
                float k = u * u * (3f - 2f * u);
                var p = Vector2.Lerp(f.A, dest, k);
                p.y -= Mathf.Sin(u * Mathf.PI) * 160f;
                float sc = size * Mathf.Lerp(1.22f, 0.48f, k);
                float yaw = f.Spin * k;
                float sx = Mathf.Cos(yaw * Mathf.Deg2Rad);
                if (Mathf.Abs(sx) < 0.08f)
                    sx = 0.08f * Mathf.Sign(sx == 0f ? 1f : sx);
                var coinR = new Rect(p.x - sc * 0.5f, p.y - sc * 0.5f, sc, sc);
                if (tex != null)
                {
                    GUI.color = new Color(1f, 0.86f, 0.40f, 0.22f * (1f - k));
                    GUI.DrawTexture(new Rect(coinR.x - 8f, coinR.y - 8f, coinR.width + 16f, coinR.height + 16f), glow, ScaleMode.ScaleToFit, true);
                    GUI.color = Color.white;
                    var prevM = GUI.matrix;
                    GUIUtility.ScaleAroundPivot(new Vector2(sx, 1f), coinR.center);
                    GUI.DrawTexture(coinR, tex, ScaleMode.ScaleToFit, true);
                    GUI.matrix = prevM;
                }
                if (u >= 1f)
                {
                    Sfx.Clink();
                    _pigBurst = 1f;
                    _coinInFlight -= f.Worth;
                    if (_coinInFlight < 0 || _flies.Count == 1)
                        _coinInFlight = 0;
                    _flies.RemoveAt(i);
                    continue;
                }
                _flies[i] = f;
            }
        }

        void DrawHomeWash(float a)
        {
            if (!GuiPaint()) return;
            // The living painting is the world backdrop. The wash stays so the pages read.
            if (!GardenLife.HomeLive)
            {
                var bg = SpriteCatalog.GardenBg;
                if (bg != null && bg.texture != null)
                    GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), bg.texture, ScaleMode.ScaleAndCrop);
            }
            GUI.color = new Color(0.10f, 0.09f, 0.08f, a);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        static Rect HomeRailRect(bool right, float size)
        {
            var safe = Screen.safeArea;
            // Brandon: drop the whole rail so the $ row clears the logo.
            float y = Screen.height * 0.335f;
            if (right)
            {
                float x = Mathf.Min(Screen.width, safe.xMax) - 20f - size;
                return new Rect(x, y, size, size);
            }
            float lx = Mathf.Max(20f, safe.xMin + 12f);
            return new Rect(lx, y, size, size);
        }

        void TryPigPoke()
        {
            Sfx.Oink();
            _pigJiggle = 1f;
            _pigBurst = Mathf.Max(_pigBurst, 0.85f);
            var award = PigPoke.Poke();
            if (award.Coins > 0)
            {
                Sfx.Clink();
                _pigBurst = 1f;
                BeginRewardPay(award.Coins);
            }
        }

        static bool GuiPaint()
        {
            var e = Event.current;
            return e == null || e.type == EventType.Repaint;
        }

        // This OnGUI pass: a tiny home target (bird or name plate) owns the pointer.
        static bool _homeTapTaken;

        // Finger slop around the fixed 1.48× body. Does not change the drawn bird.
        const float HomeBirdHitPad = 8f;

        static bool HitPad(Rect r, out bool held)
        {
            int id = GUIUtility.GetControlID(FocusType.Passive);
            var e = Event.current;
            bool inside = r.Contains(e.mousePosition);
            bool fired = false;
            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (inside && e.button == 0)
                    {
                        GUIUtility.hotControl = id;
                        e.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        e.Use();
                        if (inside) fired = true;
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id) e.Use();
                    break;
            }
            held = GUIUtility.hotControl == id;
            return fired;
        }

        // One gesture for any paged screen. The caller keeps the state.
        // Dir is +1 when the finger moved left (next page) and -1 when it moved right.
        // A tiny drag, or one that travels farther vertically than sideways, stays Dir 0
        // and does not set PageSwipeAte, so a tap under the finger still lands.
        struct PageSwipe
        {
            public Vector2 Origin;
            public bool Tracking;
            public bool Claimed;
            public int Dir;
        }

        static bool PageSwipeAte;

        static void WatchPageSwipe(ref PageSwipe swipe, Rect area, float minPx)
        {
            swipe.Dir = 0;
            var e = Event.current;
            if (e == null) return;
            var kind = e.type;
            if (kind == EventType.MouseDown && e.button == 0)
            {
                swipe.Tracking = area.width > 2f && area.height > 2f && area.Contains(e.mousePosition);
                swipe.Origin = e.mousePosition;
                swipe.Claimed = false;
                PageSwipeAte = false;
                return;
            }
            if (!swipe.Tracking || kind != EventType.MouseUp) return;
            swipe.Tracking = false;
            if (e.button != 0) return;
            int dir;
            bool took = PageSwipeDir(e.mousePosition - swipe.Origin, minPx, out dir);
            swipe.Claimed = took;
            PageSwipeAte = took;
            if (took) swipe.Dir = dir;
        }

        static bool PageSwipeDir(Vector2 delta, float minPx, out int dir)
        {
            dir = 0;
            float ax = Mathf.Abs(delta.x);
            float ay = Mathf.Abs(delta.y);
            if (ax < minPx || ax <= ay) return false;
            dir = delta.x < 0f ? 1 : -1;
            return true;
        }

        // Shared hit for every tiny home target that sits on the play flower.
        // Call before DrawFlowerPlay. HitPad Uses the event: MouseDown takes
        // hotControl, and MouseUp Uses the release. DrawFlowerPlay reads
        // _homeTapTaken as a gate, so the flower neither sinks nor starts.
        static bool HitHomeFirst(Rect r, out bool held)
        {
            var e = Event.current;
            int hotBefore = GUIUtility.hotControl;
            EventType typeBefore = e != null ? e.type : EventType.Ignore;
            bool fired = HitPad(r, out held);
            if (fired || held)
                _homeTapTaken = true;
            else if (e != null && hotBefore != 0 && GUIUtility.hotControl == 0
                && typeBefore == EventType.MouseUp && e.type == EventType.Used)
                _homeTapTaken = true;
            return fired;
        }

        // Medallion disc, plus the ribbon under it. Same test the VIP press and its open use.
        static bool VipContains(Rect medal, Vector2 m)
        {
            return SplashPress.DiscContains(medal, SplashNoAdsRibbon(medal), m);
        }

        void DrawSplash()
        {
            _homeTapTaken = false;
            float s = Mathf.Max(Screen.height / 720f, 1f);
            // Purchase / restore / reinstall: closes the offer card and runs the one-time
            // "Welcome, VIP!" moment when home is free. Member taps open the thank-you card.
            VipOffer.Tick();
            HomeModals(out bool hardModal, out bool softModal);
            bool modal = hardModal || softModal;
            // Captured before the dismiss taps below, so that same click cannot poke the bird.
            bool tutorUp = HomeTutorLive();
            PrepareSoftPrompt(modal || tutorUp);
            DismissAvatarRename(s);
            // Rail steps hold until their own button. A miss does not skip poker or Daily,
            // and the looping glove does not open the card.
            DrawHomeWash(0.18f);
            // Home title: stacked FLOCK / FIVE via letter sprites + navy block extrude
            // (same mark family as FinaleShow smash hold — correlate, not shatter).
            DrawAmbientSplashBirds(s, behind: true);
            DrawSplashTitleMark(s);
            DrawAmbientSplashBirds(s, behind: false);
            // After the wordmark, on a path that clears it.
            DrawUsaPlane(s);
            // Piggy and balance here; the streak sign layer waits until the rails are drawn.
            DrawStreakRewards(s, raiseSign: true);

            if (HomeLimbVisible())
            {
                if (_adoptLive)
                    _adoptLineR = SeatedAdoptCaption(s);
                PlaceHomeBranch(s);
                DrawHomeLimbSide(true);
            }

            int next = LevelData.NextPlay;

            // VIP before the pig so a medallion tap cannot run TryPigPoke.
            // Drawn from the packed rect so a shrink-out keeps its hit on the icon.
            EnsureSplashRails();
            bool offerVip = VipRailGoal() > 0.5f;
            bool vipDraw = RailLive(RailVip);
            var noAdsR = SplashNoAdsRect();
            // Welcome fly owns the slot. The seat stays packed so the medal has a stable home.
            bool vipHold = VipOffer.VipWelcomeHoldsRail;
            if (vipDraw)
                SplashPress.LayoutDisc(SplashPress.Id.Vip, noAdsR, SplashNoAdsRibbon(noAdsR));
            var vipGates = HomePressGates(RailVip);
            vipGates.Consent = AdConsent.BlocksControl(SplashPress.PressHit(SplashPress.Id.Vip).Rect);
            vipGates.Hidden = !vipDraw;
            vipGates.WelcomeHold = vipHold;
            vipGates.Inactive = !offerVip;
            vipGates.PopupGate = PopupBlocked(PopupKind.Vip);
            vipGates.StepGate = StepGateBlocks(SplashPress.PressHit(SplashPress.Id.Vip).Rect);
            var vipView = StepHomePress(SplashPress.Id.Vip, !SplashPress.Blocks(vipGates));
            bool noAdsHeld = vipView.Pressed;
            bool vipTap = vipView.Launch;

            // Pig above hive: hit-test before the album so taps don't open it.
            // The offer card swallows the rail so a dismiss tap cannot oink.
            var pigR = PiggyRect(s);
            if (!modal && HomeTapAllowed(RailPig) && !AdConsent.BlocksControl(pigR) && HitPad(pigR, out _) && !(vipDraw && offerVip && !vipHold && VipContains(noAdsR, Event.current.mousePosition)))
                TryPigPoke();

            // Hive lesson hides that slot. The packer closes it; poker slides up under the pig.
            bool hiveDraw = RailLive(RailHive);
            var hiveR = SplashHiveRect();
            bool hiveOpening = hiveDraw && !RailSettled(RailHive);
            bool hivePop = _hivePopping && !hiveOpening;
            var hiveHit = HivePopRect(hiveR, hivePop);
            SplashPress.Layout(SplashPress.Id.Hive, hiveHit);
            var hiveGates = HomePressGates(RailHive);
            hiveGates.Consent = AdConsent.BlocksControl(hiveHit);
            hiveGates.Hidden = !hiveDraw;
            hiveGates.Inactive = !SplashHiveShown();
            hiveGates.StepGate = StepGateBlocks(hiveHit);
            if (StepHomePress(SplashPress.Id.Hive, !SplashPress.Blocks(hiveGates)).Launch)
            {
                DismissHiveIntro();
                OpenHiveAlbum();
            }
            if (hiveDraw)
                DrawHiveButton(hiveR, s, pop: hivePop, quiet: _hivePopping && hiveOpening);

            // NextPlay is the next level index. Clearing level 1 (index 0) stores 1.
            // Owned swaps the offer crown for the diamond badge in the same slot. The tap
            // opens the offer (Buy stays on that card) or the member thank-you card.
            if (vipDraw && !vipHold)
            {
                if (vipTap)
                {
                    VipOffer.Anchor(noAdsR);
                    VipOffer.Show();
                }
                DrawNoAdsButton(noAdsR, s, noAdsHeld);
            }

            // Poker is the next packed sibling. Absent until level 1 is cleared,
            // so the rail closes under the hive. Its hit is the sliding rect.
            bool pokerDraw = RailLive(RailPoker);
            var pokerR = SplashPokerRect();
            SplashPress.Layout(SplashPress.Id.Poker, pokerR);
            var pokerGates = HomePressGates(RailPoker);
            pokerGates.Consent = AdConsent.BlocksControl(pokerR);
            pokerGates.Hidden = !pokerDraw;
            pokerGates.StepGate = StepGateBlocks(pokerR);
            if (StepHomePress(SplashPress.Id.Poker, !SplashPress.Blocks(pokerGates)).Launch)
            {
                DismissPokerIntro();
                BirdPoker.Boot();
                BirdPoker.BeginVisit();
                _pokerKeepHint = true;
                _pokerHover = -1;
                for (int i = 0; i < _pokerHoldSlide.Length; i++) _pokerHoldSlide[i] = 0f;
                _pokerDealHint = PlayerPrefs.GetInt(CoachPokerPageKey, 0) != 0
                    && PlayerPrefs.GetInt(CoachPokerDealtKey, 0) == 0;
                _pokerDealAimOk = false;
                if (_pokerDealHint)
                {
                    _gloveReady = false;
                    _gloveVis = false;
                    _glovePhase = 0f;
                    _tapSent = false;
                    _coachFade = 0f;
                }
                _home = HomeFace.Poker;
            }
            if (pokerDraw)
                DrawSplashPokerButton(pokerR);

            // Daily gift sits above VIP on the same rail gap. Absent until level 1 is cleared. A fresh
            // unlock stays out of the pack until its lesson, then grows into the gap.
            bool dailyDraw = RailLive(RailDaily);
            var dailyR = SplashDailyRect();
            // Hit = RailTapRect (drawn plate + flame, padded). The sink and the open
            // both use that rect. While the Daily lesson points at this button, a coin
            // payout still ticking (soft modal) must not eat the tap: the glove already
            // faded on it, so the card has to open on that first tap. The streak sign
            // itself still blocks the rail: it sits on top.
            var dailyTap = SplashDailyTapRect();
            SplashPress.Layout(SplashPress.Id.Daily, dailyTap);
            bool dailyLesson = HomeStepRail() == RailDaily;
            var dailyGates = HomePressGates(RailDaily);
            dailyGates.Consent = AdConsent.BlocksControl(dailyTap);
            dailyGates.Hidden = !dailyDraw;
            dailyGates.DailySoftOk = dailyLesson && _streakSlide < 0f;
            dailyGates.PopupGate = PopupBlocked(PopupKind.Daily);
            dailyGates.StepGate = StepGateBlocks(dailyTap);
            var dailyView = StepHomePress(SplashPress.Id.Daily, !SplashPress.Blocks(dailyGates));
            if (dailyView.Launch)
                OpenDailyCard();
            if (dailyDraw)
                DrawDailyRail(dailyR, s, dailyView.Pressed);
            // Reward streak pop-up above the side rails (pig / hive / VIP / poker / daily), as
            // one unit. Same rects as before; only the paint order moved.
            DrawStreakSignLayer(s);

#if UNITY_EDITOR
            string ease = _shotEase ?? LevelData.JokeEase(next);
            int number = _shotLevelNumber > 0 ? _shotLevelNumber : next + 1;
#else
            string ease = LevelData.JokeEase(next);
            int number = next + 1;
#endif
            // Bird and name plate before the flower, so a tap on either is Used
            // before the play button can arm or fire on release.
            HitHomeAvatar(s, tutorUp);
            HitAdoptTutor(s);
            // The greet's taps never get here: StepTapGate's any-tap step (GateAdoptGreet)
            // eats them at the top of OnGUI and advances to the look step.
            // Owed bird lesson keeps LEVEL from starting under the breath or the greet.
            if (DrawFlowerPlay(s, ease, number))
                PressLevelButton();
            DrawHomeAvatar(s);
            if (_adoptLive) DrawAdoptScene(s);
            DrawHiveIntro(s);
            DrawPokerIntro(s);
            DrawDailyIntro(s);
            if (VipOffer.IsOpen) VipOffer.Draw(s);
            if (_dailyOpen) DrawDailyBonus(s);
            if (_dailyAskOpen) DrawDailyAsk(s);
            if (_welcomeOpen) DrawWelcome(s);
            if (_dailyOpen) DrawDailyClaimLine(s);
            if (_welcomeGlove || (_dailyIntroLive && _dailyOpen && !_dailyAskOpen))
                DrawTutorOverlay(s);
            if (_avatarRename) DrawAvatarRename(s);
            if (!_settingsOpen && !modal && !tutorUp)
            {
                if (DrawSettingsEntry(s))
                {
                    _settingsOpen = true;
                    Sfx.CardTap();
                }
            }
            if (_settingsOpen) DrawSettingsSheet(s);
            else DrawSoftPrompt(s);
        }

        bool DrawFlowerPlay(float s, string ease, int number)
        {
            // Layout writes the one LEVEL hit. The sink and the launch both read it.
            FlowerPlayRect();
            var hit = SplashPress.PressHit(SplashPress.Id.Level);
            var rest = hit.Rect;
            var gates = HomePressGates(-1);
            gates.Consent = AdConsent.BlocksControl(rest);
            gates.AdoptQueue = AdoptHoldsQueue();
            gates.HomeTaken = _homeTapTaken;
            gates.StepGate = StepGateBlocks(rest);
            var view = StepHomePress(SplashPress.Id.Level, !SplashPress.Blocks(gates));
            bool held = view.Pressed;
            bool fired = view.Launch;

            float sink = held ? rest.height * FlowerPressSink : 0f;
            var flower = SpriteCatalog.PlayFlower;
            var tex = flower != null ? flower.texture : null;
            if (tex != null)
            {
                var shadow = new Rect(rest.x + 6f, rest.y + 16f, rest.width, rest.height);
                GUI.color = new Color(0.10f, 0.06f, 0.03f, 0.40f);
                GUI.DrawTexture(shadow, tex, ScaleMode.ScaleToFit, true);
                if (!held) DrawFlowerHalo(rest, 0f);
                GUI.color = Color.white;
                GUI.DrawTexture(rest, tex, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
                if (!held) DrawFlowerShimmer(rest, 0f);
                if (held) DrawDiscPress(rest, sink);
            }

            DrawBadgerSitter(sink);
            DrawFlowerCaption(rest, sink, ease, number);
            return fired;
        }

        static Rect Inset(Rect r, float u)
        {
            return new Rect(
                r.x + r.width * u,
                r.y + r.height * u,
                r.width * (1f - 2f * u),
                r.height * (1f - 2f * u));
        }

        static void DrawDiscPress(Rect rest, float sink)
        {
            var face = FlowerDisc(rest, 0f);
            var well = Inset(face, 0.015f);
            var plate = Inset(FlowerDisc(rest, sink), 0.03f);
            var blob = SpriteCatalog.Blanket != null ? SpriteCatalog.Blanket.texture : Texture2D.whiteTexture;
            GUI.color = new Color(0.38f, 0.24f, 0.08f, 0.88f);
            GUI.DrawTexture(well, blob, ScaleMode.StretchToFill, true);
            GUI.color = new Color(0.78f, 0.58f, 0.26f, 1f);
            GUI.DrawTexture(plate, blob, ScaleMode.StretchToFill, true);
            GUI.color = Color.white;
        }

        // Shared clay for wide pop-up bars. Gold and wood stay the flower / wood pair.
        // Red and green are the restart confirm and the keep-multiplier ask.
        enum PopupTint { Gold, Wood, Red, Green }

        static readonly Texture2D[] _popupFace = new Texture2D[8];

        // Square pop-up CTAs reuse the clay flower (DEAL / LEVEL / Watch).
        // Wide ones get a rounded plate in the same clay. Returns the label rect.
        // shadow is the dark slab under the face. Streak ask and welcome pass false.
        static Rect DrawPopupButton(Rect r, bool held, bool gold, float glow = 1f, bool shadow = true)
        {
            return DrawPopupButton(r, held, gold ? PopupTint.Gold : PopupTint.Wood, glow, shadow);
        }

        static Rect DrawPopupButton(Rect r, bool held, PopupTint tint, float glow = 1f, bool shadow = true)
        {
            if (r.width < 2f || r.height < 2f) return r;
            bool square = r.width > 8f && r.height < r.width * 1.25f && r.width < r.height * 1.25f;
            var bloom = SpriteCatalog.PlayFlower;
            if (square && bloom != null && bloom.texture != null && (tint == PopupTint.Gold || tint == PopupTint.Wood))
                return DrawPedestalFace(r, held, tint == PopupTint.Gold, bloom.texture, glow, shadow);
            return DrawRoundedFace(r, held, tint, shadow);
        }

        static Rect DrawPedestalFace(Rect r, bool held, bool gold, Texture tex, float glow, bool shadow)
        {
            float sink = held ? r.height * 0.028f : 0f;
            if (shadow)
            {
                float drop = r.height * 0.15f;
                if (drop < 12f) drop = 12f;
                GUI.color = new Color(0.10f, 0.06f, 0.03f, gold ? 0.38f : 0.20f);
                GUI.DrawTexture(new Rect(r.x + 5f, r.y + drop, r.width, r.height), tex, ScaleMode.ScaleToFit, true);
            }
            GUI.color = gold ? Color.white : new Color(0.84f, 0.82f, 0.76f, 1f);
            if (!held && gold) DrawFlowerHalo(r, 0f, glow);
            GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit, true);
            if (!held && gold) DrawFlowerShimmer(r, 0f, glow);
            GUI.color = Color.white;
            if (held) DrawDiscPress(r, sink);
            return FlowerDisc(r, sink);
        }

        static Rect DrawRoundedFace(Rect r, bool held, PopupTint tint, bool shadow)
        {
            float sink = held ? Mathf.Max(2f, r.height * 0.07f) : 0f;
            var face = new Rect(r.x, r.y + sink, r.width, Mathf.Max(8f, r.height - sink));
            var tex = PopupButtonTex(tint, shadow);
            if (shadow)
            {
                float drop = held ? 1.5f : Mathf.Max(3f, r.height * 0.11f);
                GUI.color = new Color(0.05f, 0.03f, 0.015f, held ? 0.22f : 0.40f);
                DrawBevelPlate(tex, new Rect(face.x + 1.2f, face.y + drop, face.width, face.height), 28f);
            }
            GUI.color = held ? new Color(0.86f, 0.86f, 0.86f, 1f) : Color.white;
            DrawBevelPlate(tex, face, 28f);
            GUI.color = Color.white;
            float ix = face.width * 0.05f;
            float iy = face.height * 0.14f;
            return new Rect(face.x + ix, face.y + iy, Mathf.Max(4f, face.width - ix * 2f), Mathf.Max(4f, face.height - iy * 2f));
        }

        static Texture2D PopupButtonTex(PopupTint tint, bool shadow)
        {
            int i = ((int)tint) * 2 + (shadow ? 1 : 0);
            if (i < 0 || i >= _popupFace.Length) i = shadow ? 1 : 0;
            if (_popupFace[i] == null) _popupFace[i] = BakePopupButton(tint, shadow);
            return _popupFace[i];
        }

        static void PopupClay(PopupTint tint, out Color mid, out Color top, out Color bot)
        {
            switch (tint)
            {
                case PopupTint.Wood:
                    mid = new Color(0.46f, 0.27f, 0.12f, 1f);
                    top = new Color(0.74f, 0.52f, 0.28f, 1f);
                    bot = new Color(0.16f, 0.08f, 0.03f, 1f);
                    break;
                case PopupTint.Red:
                    mid = new Color(0.78f, 0.16f, 0.13f, 1f);
                    top = new Color(1f, 0.48f, 0.40f, 1f);
                    bot = new Color(0.36f, 0.05f, 0.04f, 1f);
                    break;
                case PopupTint.Green:
                    mid = new Color(0.16f, 0.58f, 0.24f, 1f);
                    top = new Color(0.55f, 0.88f, 0.46f, 1f);
                    bot = new Color(0.05f, 0.28f, 0.08f, 1f);
                    break;
                default:
                    mid = new Color(0.93f, 0.66f, 0.22f, 1f);
                    top = new Color(1f, 0.95f, 0.74f, 1f);
                    bot = new Color(0.40f, 0.20f, 0.05f, 1f);
                    break;
            }
        }

        // shadow keeps the dark foot and the rim that reads as a slab.
        // Without it the plate stays the same rounded fill, with only the top sheen.
        static Texture2D BakePopupButton(PopupTint tint, bool shadow)
        {
            const int n = 96;
            const float rad = 28f;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = "Popup" + tint + (shadow ? "" : "Flat")
            };
            var px = new Color32[n * n];
            PopupClay(tint, out Color mid, out Color top, out Color bot);
            Color sheen = new Color(1f, 0.98f, 0.90f, 1f);
            for (int y = 0; y < n; y++)
            {
                bool capTop = y >= n - rad;
                bool capBot = y < rad;
                for (int x = 0; x < n; x++)
                {
                    float d = PopupSdf(x + 0.5f, y + 0.5f, n, n, rad);
                    float a = Mathf.Clamp01(0.65f - d);
                    if (a <= 0f) continue;
                    Color c = mid;
                    if (capTop)
                    {
                        float k = (y - (n - rad)) / rad;
                        c = Color.Lerp(mid, top, Mathf.SmoothStep(0f, 1f, k));
                        if (k > 0.62f) c = Color.Lerp(c, sheen, (k - 0.62f) / 0.38f * 0.55f);
                    }
                    else if (shadow && capBot)
                    {
                        float k = 1f - y / Mathf.Max(1f, rad);
                        c = Color.Lerp(mid, bot, Mathf.SmoothStep(0f, 1f, k));
                    }
                    if (shadow)
                    {
                        float rim = Mathf.Clamp01((d + 3.4f) / 3.4f);
                        c = Color.Lerp(c, bot, rim * 0.62f);
                    }
                    c.a = a;
                    px[y * n + x] = (Color32)c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        static float PopupSdf(float x, float y, float w, float h, float rad)
        {
            float cx = Mathf.Abs(x - w * 0.5f) - (w * 0.5f - rad);
            float cy = Mathf.Abs(y - h * 0.5f) - (h * 0.5f - rad);
            float ox = Mathf.Max(cx, 0f);
            float oy = Mathf.Max(cy, 0f);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(cx, cy), 0f) - rad;
        }

        // Nine quads. The face, lip, and frame are bands of one baked plate.
        // Snap the rect, keep each band's samples half a texel inside itself,
        // and overlap neighbors by a pixel. Abutted fractional edges round apart
        // and the dark board shows through as a line where the face meets the lip.
        static void DrawBevelPlate(Texture2D tex, Rect box, float srcRad)
        {
            if (tex == null || box.width < 2f || box.height < 2f) return;
            float x0 = Mathf.Round(box.x);
            float y0 = Mathf.Round(box.y);
            float x1 = Mathf.Round(box.xMax);
            float y1 = Mathf.Round(box.yMax);
            if (x1 < x0 + 2f || y1 < y0 + 2f) return;
            float tw = Mathf.Max(1f, tex.width);
            float th = Mathf.Max(1f, tex.height);
            float u = Mathf.Clamp(srcRad / tw, 0.02f, 0.45f);
            float v = Mathf.Clamp(srcRad / th, 0.02f, 0.45f);
            float bw = x1 - x0;
            float bh = y1 - y0;
            float cw = Mathf.Min(Mathf.Max(1f, Mathf.Round(srcRad)), Mathf.Floor(bw * 0.5f));
            float ch = Mathf.Min(Mathf.Max(1f, Mathf.Round(srcRad)), Mathf.Floor(bh * 0.5f));
            float lap = (cw + 1f <= bw && ch + 1f <= bh) ? 1f : 0f;
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
        }

        // Top of LEVEL on the pedestal. Higher of the blank and joke stacks,
        // matching DrawFlowerCaption, so the perch stays off the words either way.
        static float FlowerLevelTop()
        {
            var disc = FlowerDisc(FlowerPlayRect(), 0f);
            float aloneH = disc.height * 0.55f;
            float aloneY = disc.y + (disc.height - aloneH) * 0.5f + disc.height * 0.018f;
            float stackH = disc.height * 0.56f;
            float stackY = disc.y + (disc.height - stackH) * 0.68f;
            return aloneY < stackY ? aloneY : stackY;
        }

        // How far a held play flower sinks (fraction of its rect). The poker cover plate reads it.
        const float FlowerPressSink = 0.030f;

        static Rect FlowerDisc(Rect rest, float sink)
        {
            return new Rect(
                rest.x + rest.width * 0.205f,
                rest.y + sink + rest.height * 0.10f,
                rest.width * 0.59f,
                rest.height * 0.38f);
        }

        // Same pad the Watch and Claim flowers use. The glove accepts this rect, not the square.
        static Rect FlowerHit(Rect flower, float s)
        {
            var disc = FlowerDisc(flower, 0f);
            float hitGrow = 18f * s;
            var hit = new Rect(disc.x - hitGrow, disc.y - hitGrow * 0.65f, disc.width + hitGrow * 2f, disc.height + hitGrow * 1.3f);
            if (hit.height < 52f * s)
            {
                float extra = 52f * s - hit.height;
                hit.y -= extra * 0.5f;
                hit.height += extra;
            }
            return hit;
        }

        static Rect GiftWatchTapRect(float s)
        {
            GiftCardLayout(s, out _, out var flower);
            return FlowerHit(flower, s);
        }

        // Difficulty line under LEVEL. One size for every joke, including a long one.
        // EaseSubtextMinPx is the floor. The band wraps or widens; the glyphs do not shrink.
        const float EaseSubtextPt = 18f;
        const int EaseSubtextMinPx = 18;
        static GUIContent _easeContent;

        static int EaseSubtextPx(float s)
        {
            if (s < 1f) s = 1f;
            int px = Mathf.RoundToInt(EaseSubtextPt * s);
            if (px < EaseSubtextMinPx) px = EaseSubtextMinPx;
            return px;
        }

        static void DrawFlowerCaption(Rect rest, float sink, string ease, int number)
        {
            var disc = FlowerDisc(rest, sink);
            bool hasEase = !string.IsNullOrEmpty(ease);
            // LEVEL over joke as one field, biased down off the top rim.
            // Blank: LEVEL alone slightly down + right. Flat type (no plate tilt squash).
            Rect lvR;
            Rect jokeR;
            TextAnchor lvAlign;
            if (hasEase)
            {
                float padX = disc.width * 0.04f;
                float stackH = disc.height * 0.56f;
                // Hairline: joke stack down a smidge vs be47453.
                float stackY = disc.y + (disc.height - stackH) * 0.68f;
                float gap = disc.height * 0.005f;
                float lvH = stackH * 0.52f;
                float jokeH = stackH - lvH - gap;
                lvR = new Rect(disc.x + padX, stackY, disc.width - padX * 2f, lvH);
                jokeR = new Rect(disc.x + padX, stackY + lvH + gap, disc.width - padX * 2f, jokeH);
                lvAlign = TextAnchor.LowerCenter;
            }
            else
            {
                float padX = disc.width * 0.04f;
                float aloneH = disc.height * 0.55f;
                // Hairline: blank LEVEL up a smidge (was +0.045f).
                float aloneY = disc.y + (disc.height - aloneH) * 0.5f + disc.height * 0.018f;
                float aloneX = disc.x + padX + disc.width * 0.035f;
                lvR = new Rect(aloneX, aloneY, disc.width - padX * 2f - disc.width * 0.035f, aloneH);
                jokeR = default;
                lvAlign = TextAnchor.MiddleCenter;
            }

            var lv = GuiPool.Label(GuiSlot.FlowerLevel);
            lv.fontStyle = FontStyle.Bold;
            lv.alignment = lvAlign;
            lv.wordWrap = false;
            string level = GuiPool.Num(GuiSlot.FlowerLevel, "LEVEL ", number);
            bool quad = number >= 1000;
            bool triple = number >= 100;
            string fitProbe = level;
            // 1000: shrink less — probe actual level, milder hi/lo so type stays nearer 2/4/6.
            if (quad) fitProbe = level;
            else if (hasEase || triple) fitProbe = "LEVEL 888";
            int lvHi = hasEase
                ? (quad ? 72 : (triple ? 64 : 84))
                : (quad ? 82 : (triple ? 78 : 96));
            lv.fontSize = FitFont(
                lv, fitProbe,
                lvR.width * (quad ? 0.98f : (triple ? 0.96f : 0.94f)),
                lvR.height * (hasEase ? 0.95f : 0.80f),
                quad ? 30 : (triple ? 26 : 30), lvHi);
            StampBannerText(lvR, level, lv, 1f, true);

            if (!hasEase) return;

            // Shared size. Does not follow the 100+ LEVEL cap or the length of the line.
            float s = Mathf.Max(Screen.height / 720f, 1f);
            int jokePx = EaseSubtextPx(s);
            var joke = GuiPool.Label(GuiSlot.FlowerJoke);
            joke.fontStyle = FontStyle.Normal;
            joke.alignment = TextAnchor.UpperCenter;
            joke.wordWrap = false;
            joke.fontSize = jokePx;
            if (_easeContent == null) _easeContent = new GUIContent();
            _easeContent.text = ease;
            float side = disc.width * 0.03f;
            float lineW = disc.width - side * 2f;
            if (lineW < jokeR.width) lineW = jokeR.width;
            var ink = joke.CalcSize(_easeContent);
            float textH = ink.y;
            if (ink.x > lineW)
            {
                joke.wordWrap = true;
                textH = joke.CalcHeight(_easeContent, lineW);
            }
            float bandH = textH + 4f;
            if (bandH < jokeR.height) bandH = jokeR.height;
            float y = jokeR.y;
            float bottom = disc.yMax - 2f;
            if (y + textH > bottom)
            {
                float minY = lvR.yMax + disc.height * 0.004f;
                float y2 = bottom - textH;
                if (y2 < minY) y2 = minY;
                if (y2 < y) y = y2;
            }
            if (y + bandH > bottom && bandH > textH) bandH = Mathf.Max(textH, bottom - y);
            var jokeDraw = new Rect(disc.center.x - lineW * 0.5f, y, lineW, bandH);
            StampBannerText(jokeDraw, ease, joke, 1f, false);
        }

        static GUIStyle _fitScratch;
        static GUIContent _fitContent;
        const int FitSlots = 24;
        const float FitKeep = 0.05f;
        static readonly string[] _fitText = new string[FitSlots];
        static readonly int[] _fitLo = new int[FitSlots];
        static readonly int[] _fitHi = new int[FitSlots];
        static readonly int[] _fitStamp = new int[FitSlots];
        static readonly int[] _fitPx = new int[FitSlots];
        static readonly float[] _fitW = new float[FitSlots];
        static readonly float[] _fitH = new float[FitSlots];
        static readonly int[] _fitUse = new int[FitSlots];
        static int _fitSerial;

        static void BindFit(GUIStyle proto, string text, bool wrap)
        {
            if (_fitScratch == null) _fitScratch = new GUIStyle(proto);
            _fitScratch.font = proto.font;
            _fitScratch.fontStyle = proto.fontStyle;
            _fitScratch.alignment = proto.alignment;
            _fitScratch.wordWrap = wrap;
            _fitScratch.richText = proto.richText;
            _fitScratch.clipping = proto.clipping;
            _fitScratch.padding = proto.padding;
            if (_fitContent == null) _fitContent = new GUIContent();
            _fitContent.text = text ?? "";
        }

        static int FitFont(GUIStyle proto, string text, float maxW, float maxH, int lo, int hi) =>
            CachedFit(proto, text, maxW, maxH, lo, hi, proto != null && proto.wordWrap);

        static int FitFontWrapped(GUIStyle proto, string text, float maxW, float maxH, int lo, int hi) =>
            CachedFit(proto, text, maxW, maxH, lo, hi, true);

        // Same text and a box within 5% reuses the fitted size. A lamp pulse is
        // about ±2%, so a full-size popup label does not refit every frame.
        static int CachedFit(GUIStyle proto, string text, float maxW, float maxH, int lo, int hi, bool wrap)
        {
            if (lo > hi) lo = hi;
            string key = text ?? "";
            int stamp = FitStamp(proto, wrap);
            int slot = -1;
            int oldest = 0;
            int oldestUse = int.MaxValue;
            for (int i = 0; i < FitSlots; i++)
            {
                if (_fitUse[i] < oldestUse)
                {
                    oldestUse = _fitUse[i];
                    oldest = i;
                }
                if (_fitText[i] != key || _fitLo[i] != lo || _fitHi[i] != hi || _fitStamp[i] != stamp)
                    continue;
                if (!FitClose(_fitW[i], maxW) || !FitClose(_fitH[i], maxH)) continue;
                _fitSerial++;
                _fitUse[i] = _fitSerial;
                return _fitPx[i];
            }
            slot = oldest;
            int size = wrap
                ? MeasureFitWrapped(proto, key, maxW, maxH, lo, hi)
                : MeasureFit(proto, key, maxW, maxH, lo, hi);
            _fitText[slot] = key;
            _fitLo[slot] = lo;
            _fitHi[slot] = hi;
            _fitStamp[slot] = stamp;
            _fitW[slot] = maxW;
            _fitH[slot] = maxH;
            _fitPx[slot] = size;
            _fitSerial++;
            _fitUse[slot] = _fitSerial;
            return size;
        }

        static bool FitClose(float stored, float now)
        {
            float den = stored > 8f ? stored : 8f;
            float d = now - stored;
            if (d < 0f) d = -d;
            return d <= den * FitKeep;
        }

        static int FitStamp(GUIStyle proto, bool wrap)
        {
            if (proto == null) return wrap ? 1 : 0;
            int font = proto.font != null ? proto.font.GetInstanceID() : 0;
            int bits = wrap ? 1 : 0;
            if (proto.richText) bits += 2;
            return font * 17 + (int)proto.fontStyle * 3 + bits;
        }

        static int MeasureFit(GUIStyle proto, string text, float maxW, float maxH, int lo, int hi)
        {
            BindFit(proto, text, proto != null && proto.wordWrap);
            var st = _fitScratch;
            var content = _fitContent;
            for (int fs = hi; fs >= lo; fs--)
            {
                st.fontSize = fs;
                var sz = st.CalcSize(content);
                if (sz.x <= maxW && sz.y <= maxH) return fs;
            }
            return lo;
        }

        static int MeasureFitWrapped(GUIStyle proto, string text, float maxW, float maxH, int lo, int hi)
        {
            if (string.IsNullOrEmpty(text) || maxW < 8f || maxH < 8f) return lo;
            BindFit(proto, text, true);
            var st = _fitScratch;
            var content = _fitContent;
            for (int fs = hi; fs >= lo; fs--)
            {
                st.fontSize = fs;
                float h = st.CalcHeight(content, maxW);
                if (h <= maxH) return fs;
            }
            return lo;
        }

        // Greedy wrap that keeps an orphan word off the last line. Explicit newlines stay.
        static string BalanceWrap(GUIStyle proto, string text, float width, int fontSize)
        {
            if (string.IsNullOrEmpty(text) || width < 8f) return text ?? "";
            var parts = text.Split('\n');
            string built = "";
            for (int p = 0; p < parts.Length; p++)
            {
                if (p > 0) built += "\n";
                built += BalanceParagraph(proto, parts[p], width, fontSize);
            }
            return built;
        }

        static string BalanceParagraph(GUIStyle proto, string paragraph, float width, int fontSize)
        {
            if (string.IsNullOrEmpty(paragraph)) return "";
            var raw = paragraph.Split(' ');
            int n = 0;
            for (int i = 0; i < raw.Length; i++)
                if (raw[i].Length > 0) n++;
            if (n <= 1) return paragraph.Trim();
            var words = new string[n];
            int k = 0;
            for (int i = 0; i < raw.Length; i++)
                if (raw[i].Length > 0) words[k++] = raw[i];
            const int maxLines = 12;
            var start = new int[maxLines];
            var count = new int[maxLines];
            int lines = 0;
            int at = 0;
            BindFit(proto, words[0], false);
            _fitScratch.fontSize = Mathf.Max(1, fontSize);
            while (at < n && lines < maxLines)
            {
                int take = 1;
                string line = words[at];
                while (at + take < n)
                {
                    string trial = line + " " + words[at + take];
                    _fitContent.text = trial;
                    if (_fitScratch.CalcSize(_fitContent).x > width) break;
                    line = trial;
                    take++;
                }
                start[lines] = at;
                count[lines] = take;
                lines++;
                at += take;
            }
            if (at < n && lines > 0) count[lines - 1] += n - at;
            if (lines >= 2 && count[lines - 1] == 1 && count[lines - 2] >= 2)
            {
                string prev = words[start[lines - 2]];
                for (int j = 1; j < count[lines - 2] - 1; j++)
                    prev += " " + words[start[lines - 2] + j];
                _fitContent.text = prev;
                if (_fitScratch.CalcSize(_fitContent).x <= width)
                {
                    count[lines - 2]--;
                    start[lines - 1]--;
                    count[lines - 1]++;
                }
            }
            string built = "";
            for (int L = 0; L < lines; L++)
            {
                if (L > 0) built += "\n";
                for (int j = 0; j < count[L]; j++)
                {
                    if (j > 0) built += " ";
                    int ix = start[L] + j;
                    if ((uint)ix < (uint)n) built += words[ix];
                }
            }
            return built;
        }

        static string ClipLines(GUIStyle proto, string text, float maxW, float maxH, int fontSize)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (maxW < 8f || maxH < 8f) return "…";
            BindFit(proto, text, true);
            var st = _fitScratch;
            var content = _fitContent;
            st.fontSize = Mathf.Max(1, fontSize);
            if (st.CalcHeight(content, maxW) <= maxH + 0.5f) return text;
            int lo = 1;
            int hi = text.Length;
            int best = 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                content.text = EllipsisAt(text, mid);
                if (st.CalcHeight(content, maxW) <= maxH + 0.5f)
                {
                    best = mid;
                    lo = mid + 1;
                }
                else hi = mid - 1;
            }
            return EllipsisAt(text, best);
        }

        static string EllipsisAt(string text, int keep)
        {
            if (string.IsNullOrEmpty(text) || keep >= text.Length) return text ?? "";
            if (keep < 1) return "…";
            int n = keep;
            while (n > 1 && !char.IsWhiteSpace(text[n - 1]) && n < text.Length && !char.IsWhiteSpace(text[n])) n--;
            if (n < keep / 2) n = keep;
            return text.Substring(0, n).TrimEnd() + "…";
        }

        static void StampBox(Rect box, string text, GUIStyle st, Color fill, int whitePx, int blackPx)
        {
            int pad = whitePx + blackPx + 2;
            var clip = new Rect(box.x - pad, box.y - pad, box.width + pad * 2f, box.height + pad * 2f);
            GUI.BeginGroup(clip);
            StampOutlined(new Rect(pad, pad, box.width, box.height), text, st, fill, whitePx, blackPx);
            GUI.EndGroup();
        }

        // At most two sentence breaks, so the card starts at three lines before width wrap.
        public static string HiveBackCopy(string back)
        {
            if (string.IsNullOrEmpty(back)) return "";
            var sb = new System.Text.StringBuilder(back.Length + 4);
            int breaks = 0;
            for (int i = 0; i < back.Length; i++)
            {
                char c = back[i];
                sb.Append(c);
                if (breaks >= 2) continue;
                if ((c == '.' || c == '!' || c == '?') && i + 1 < back.Length && back[i + 1] == ' ')
                {
                    sb.Append('\n');
                    i++;
                    breaks++;
                }
            }
            return sb.ToString();
        }

        static void Paint(GUIStyle st, Color c)
        {
            st.normal.textColor = c;
            st.hover.textColor = c;
            st.active.textColor = c;
            st.focused.textColor = c;
            st.onNormal.textColor = c;
            st.onHover.textColor = c;
            st.onActive.textColor = c;
        }

        static void StampArced(Rect disc, string text, GUIStyle st, Color fill, int whitePx, int blackPx)
        {
            if (string.IsNullOrEmpty(text)) return;
            int n = text.Length;
            var widths = new float[n];
            float total = 0f;
            // Extra tracking on the long joke so glyphs don't crunch together.
            float tracking = st.fontSize * (n >= 14 ? 0.10f : (n >= 10 ? 0.05f : 0.025f));
            for (int i = 0; i < n; i++)
            {
                float w = st.CalcSize(new GUIContent(text[i].ToString())).x;
                if (text[i] == ' ') w = Mathf.Max(w, st.fontSize * 0.48f);
                widths[i] = Mathf.Max(2f, w) + tracking;
                total += widths[i];
            }
            if (total < 1f) return;

            // Longer strings: wider, flatter bow on the plate rim (less inward lean).
            float spanMax = n >= 14 ? 1.02f : (n >= 10 ? 1.18f : 1.28f);
            float rx = disc.width * (n >= 14 ? 0.48f : 0.40f);
            float span = total / Mathf.Max(rx, 1f);
            if (span > spanMax)
            {
                rx = Mathf.Min(disc.width * (n >= 14 ? 0.52f : 0.44f), total / (spanMax * 0.92f));
                span = total / Mathf.Max(rx, 1f);
            }
            span = Mathf.Clamp(span, 0.48f, spanMax);
            float aspect = n >= 14
                ? 0.56f
                : Mathf.Clamp(disc.height / Mathf.Max(disc.width, 1f), 0.55f, 0.82f);
            float ry = rx * aspect;
            float cx = disc.center.x;
            // Drop the bow further toward LEVEL (post-#64 stills still had open air).
            float cy = disc.y + disc.height * (n >= 14 ? 0.26f : 0.28f) + ry;
            float start = -span * 0.5f;
            float acc = 0f;
            float h = st.CalcSize(new GUIContent("Ag")).y;
            var prev = GUI.matrix;
            for (int i = 0; i < n; i++)
            {
                float u = (acc + widths[i] * 0.5f) / total;
                float ang = start + u * span;
                float x = cx + rx * Mathf.Sin(ang);
                float y = cy - ry * Mathf.Cos(ang);
                var r = new Rect(x - widths[i] * 0.5f, y - h * 0.52f, widths[i], h);
                GUI.matrix = prev;
                GUIUtility.RotateAroundPivot(ang * Mathf.Rad2Deg, new Vector2(x, y));
                StampOutlined(r, text[i].ToString(), st, fill, whitePx, blackPx);
                acc += widths[i];
            }
            GUI.matrix = prev;
        }

        // Bold white, black edge at BannerOutline of the point size. Subtext passes bold false
        // (same face, thinner). Level disc, status banners, and tutorial captions all use this.
        const float BannerOutline = 0.16f;

        static int BannerOutlinePx(int fontPx)
        {
            int px = Mathf.RoundToInt(fontPx * BannerOutline);
            if (px < 2) px = 2;
            return px;
        }

        static void StampBannerText(Rect r, string text, GUIStyle st, float alpha, bool bold)
        {
            if (st == null || string.IsNullOrEmpty(text) || alpha < 0.04f) return;
            var prev = st.fontStyle;
            st.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            StampOutlined(r, text, st, new Color(1f, 1f, 1f, alpha), 0, BannerOutlinePx(st.fontSize));
            st.fontStyle = prev;
        }

        static GUIContent _rimContent;
        static System.Func<Vector2, Vector2> _unclip;
        static bool _unclipFailed;
        static System.Func<float> _pixels;
        static bool _pixelsFailed;

        static GUIContent RimContent(string text)
        {
            if (_rimContent == null) _rimContent = new GUIContent();
            if (_rimContent.text != text) _rimContent.text = text ?? "";
            return _rimContent;
        }

        // Group offset lives on the clip stack, not in GUI.matrix. Three probes rebuild
        // the affine map IMGUI actually uses, so the snap is in that space. Outside OnGUI
        // (or if the clip probe is junk) GUI.matrix alone is the map.
        static Matrix4x4 PixelMatrix(float x, float y)
        {
            if (Event.current != null && TryPixelMatrix(x, y, out Matrix4x4 full))
                return full;
            return GUI.matrix;
        }

        static bool TryPixelMatrix(float x, float y, out Matrix4x4 full)
        {
            full = Matrix4x4.identity;
            if (!TryUnclip(new Vector2(x, y), out Vector2 o)) return false;
            if (!TryUnclip(new Vector2(x + 1f, y), out Vector2 ox)) return false;
            if (!TryUnclip(new Vector2(x, y + 1f), out Vector2 oy)) return false;
            float a00 = ox.x - o.x;
            float a10 = ox.y - o.y;
            float a01 = oy.x - o.x;
            float a11 = oy.y - o.y;
            float col0 = a00 * a00 + a10 * a10;
            float col1 = a01 * a01 + a11 * a11;
            if (col0 < 0.0025f || col0 > 400f || col1 < 0.0025f || col1 > 400f) return false;
            full = Matrix4x4.identity;
            full.m00 = a00;
            full.m10 = a10;
            full.m01 = a01;
            full.m11 = a11;
            full.m03 = o.x - (a00 * x + a01 * y);
            full.m13 = o.y - (a10 * x + a11 * y);
            return true;
        }

        static bool TryUnclip(Vector2 p, out Vector2 w)
        {
            w = p;
            var fn = UnclipFn();
            if (fn == null) return false;
            w = fn(p);
            return true;
        }

        static System.Func<Vector2, Vector2> UnclipFn()
        {
            if (_unclip != null || _unclipFailed) return _unclip;
            try
            {
                var t = typeof(GUI).Assembly.GetType("UnityEngine.GUIClip");
                var m = t != null
                    ? t.GetMethod("UnclipToWindow",
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
                        null, new[] { typeof(Vector2) }, null)
                    : null;
                if (m == null) { _unclipFailed = true; return null; }
                _unclip = (System.Func<Vector2, Vector2>)System.Delegate.CreateDelegate(typeof(System.Func<Vector2, Vector2>), m);
            }
            catch
            {
                _unclipFailed = true;
                _unclip = null;
            }
            return _unclip;
        }

        // pixelsPerPoint is assembly-internal. One cached delegate, then 1 if the
        // lookup is stripped. Integer offsets stay locked to the fill either way.
        static float GuiPixels()
        {
            var fn = PixelsFn();
            float p = fn != null ? fn() : 1f;
            return p < 0.01f ? 1f : p;
        }

        static System.Func<float> PixelsFn()
        {
            if (_pixels != null || _pixelsFailed) return _pixels;
            try
            {
                var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
                var prop = typeof(GUIUtility).GetProperty("pixelsPerPoint", flags);
                var m = prop != null ? prop.GetGetMethod(true) : null;
                if (m == null) m = typeof(GUIUtility).GetMethod("get_pixelsPerPoint", flags);
                if (m == null) { _pixelsFailed = true; return null; }
                _pixels = (System.Func<float>)System.Delegate.CreateDelegate(typeof(System.Func<float>), m);
            }
            catch
            {
                _pixelsFailed = true;
                _pixels = null;
            }
            return _pixels;
        }

        static LabelRim.Seat LockSeat(Rect r)
        {
            return LabelRim.Lock(r, PixelMatrix(r.x, r.y), GuiPixels());
        }

        static void LabelAt(LabelRim.Seat seat, int dx, int dy, GUIContent text, GUIStyle st)
        {
            seat.Local(dx, dy, out float x, out float y);
            GUI.Label(new Rect(x, y, seat.Width, seat.Height), text, st);
        }

        // Solid disc of integer offsets from one snapped origin. Cached per radius.
        // The cached GUIContent is the only text object; the loop allocates nothing.
        static void Rim(LabelRim.Seat seat, GUIContent text, GUIStyle st, float px)
        {
            int rad = LabelRim.Radius(px);
            if (rad <= 0 || text == null) return;
            int n = LabelRim.Count(rad);
            for (int i = 0; i < n; i++)
            {
                LabelRim.At(rad, i, out int dx, out int dy);
                LabelAt(seat, dx, dy, text, st);
            }
        }

        static void StampOutlined(Rect r, string text, GUIStyle st, Color fill, int whitePx, int blackPx, float minA = 0.04f)
        {
            float a = Mathf.Clamp01(fill.a);
            if (a < minA) return;
            var body = RimContent(text);
            var seat = LockSeat(r);
            Paint(st, new Color(0.02f, 0.02f, 0.02f, a));
            Rim(seat, body, st, whitePx + blackPx);
            if (whitePx > 0)
            {
                Paint(st, new Color(1f, 1f, 1f, a));
                Rim(seat, body, st, whitePx);
            }
            Paint(st, fill);
            LabelAt(seat, 0, 0, body, st);
        }

        // Busy backgrounds. Thin dark rim, a soft shadow down-right, then the bright fill.
        // StampOutlined stays the thick white-and-black rim. This is the lighter one.
        // The shadow is one drop, not a second stroke. It shares the snapped origin.
        static void StampReadable(Rect r, string text, GUIStyle st, Color fill)
        {
            if (string.IsNullOrEmpty(text) || st == null) return;
            float a = Mathf.Clamp01(fill.a);
            if (a < 0.04f) return;
            float drop = Mathf.Max(1.5f, st.fontSize * 0.07f);
            var body = RimContent(text);
            var seat = LockSeat(r);
            int farX = Mathf.Max(1, Mathf.RoundToInt(drop * 1.7f));
            int farY = Mathf.Max(1, Mathf.RoundToInt(drop * 2.2f));
            int nearX = Mathf.Max(1, Mathf.RoundToInt(drop * 0.75f));
            int nearY = Mathf.Max(1, Mathf.RoundToInt(drop));
            Paint(st, new Color(0.02f, 0.03f, 0.06f, a * 0.20f));
            LabelAt(seat, farX, farY, body, st);
            Paint(st, new Color(0.02f, 0.03f, 0.06f, a * 0.40f));
            LabelAt(seat, nearX, nearY, body, st);
            Paint(st, new Color(0.05f, 0.04f, 0.03f, a));
            Rim(seat, body, st, 1f);
            Paint(st, fill);
            LabelAt(seat, 0, 0, body, st);
        }

        // Poker chrome. Black disc at 2, white disc at 1, fill on the same snapped origin.
        // Label count is stable across Layout and Repaint (HitPad ids).
        static void StampLight(Rect r, string text, GUIStyle st, Color fill)
        {
            float a = Mathf.Clamp01(fill.a);
            if (a < 0.04f) return;
            var body = RimContent(text);
            var seat = LockSeat(r);
            Paint(st, new Color(0.02f, 0.02f, 0.02f, a));
            Rim(seat, body, st, 2f);
            Paint(st, new Color(1f, 1f, 1f, a));
            Rim(seat, body, st, 1f);
            Paint(st, fill);
            LabelAt(seat, 0, 0, body, st);
        }

        // Home wordmark vs the 0.72-wide mark. Top edge stays; FIVE clears the coin rail.
        // Splash halo radii, center, and bird size read this same factor.
        const float SplashLogoScale = 0.91f;

        // Finale-family wordmark via letter sprites (yellow faces + navy ExtrudeNear/Far block).
        void DrawSplashTitleMark(float s)
        {
            float top = TopHud();
            float fit = s * SplashLogoScale;
            float maxW = Screen.width * 0.72f * SplashLogoScale;
            float capH = 56f * fit;
            float rowGap = 4f * fit;
            float tracking = -0.055f;
            DrawSplashWord("FLOCK", top, maxW, capH, tracking, fit);
            DrawSplashWord("FIVE", top + capH + rowGap, maxW, capH, tracking, fit);
        }

        // Same yellow-navy letter family as the splash mark, with POKER as the third row.
        float PokerTitleHeight(float s)
        {
            float capSmall = 22f * s;
            float capPoker = 30f * s;
            float gap = 1.5f * s;
            return capSmall * 2f + capPoker + gap * 2f;
        }

        float DrawPokerTitleMark(float top, float s)
        {
            float maxW = Mathf.Min(Screen.width * 0.62f, 280f * s);
            float capSmall = 22f * s;
            float capPoker = 30f * s;
            float gap = 1.5f * s;
            DrawSplashWord("FLOCK", top, maxW, capSmall, -0.055f, s);
            DrawSplashWord("FIVE", top + capSmall + gap, maxW, capSmall, -0.055f, s);
            DrawSplashWord("POKER", top + capSmall * 2f + gap * 2f, maxW, capPoker, -0.05f, s);
            return PokerTitleHeight(s);
        }

        void DrawSplashWord(string word, float y, float maxW, float capH, float tracking, float s)
        {
            int n = word.Length;
            var sprs = new Sprite[n];
            var widths = new float[n];
            float total = 0f;
            bool any = false;
            for (int i = 0; i < n; i++)
            {
                if (word[i] == ' ')
                {
                    sprs[i] = null;
                    widths[i] = capH * 0.28f;
                    total += widths[i];
                    continue;
                }
                // V: Resources fx_let_V is missing/magenta — use Finale pointed V bytes (same as FinaleShow).
                sprs[i] = word[i] == 'V' ? SplashPointedV() : SpriteCatalog.Letter(word[i]);
                if (sprs[i] != null && sprs[i].texture != null && !IsMagentaPlaceholder(sprs[i]))
                {
                    any = true;
                    float aspect = sprs[i].rect.width / Mathf.Max(1f, sprs[i].rect.height);
                    widths[i] = capH * Mathf.Clamp(aspect, 0.45f, 1.15f);
                }
                else
                {
                    sprs[i] = null;
                    widths[i] = capH * 0.72f;
                }
                total += widths[i];
            }
            total += tracking * capH * (n - 1);
            float scale = Mathf.Min(1f, maxW / Mathf.Max(1f, total));
            float h = capH * scale;
            float tw = total * scale;
            float x = (Screen.width - tw) * 0.5f;

            // Fallback: Bold stamp if letter art missing.
            if (!any)
            {
                var st = GuiPool.Label(GuiSlot.SplashWord);
                st.fontSize = Mathf.RoundToInt(h * 0.92f);
                st.fontStyle = FontStyle.Bold;
                st.alignment = TextAnchor.MiddleCenter;
                var fill = new Color(1f, 0.92f, 0.42f);
                var stroke = new Color(0.02f, 0.02f, 0.04f, 1f);
                var near = new Color(28f / 255f, 44f / 255f, 102f / 255f, 1f);
                int steps = Mathf.Max(8, Mathf.RoundToInt(10f * s));
                float step = 1.35f * s * scale;
                var r = new Rect(x, y, tw, h);
                var body = RimContent(word);
                var seat = LockSeat(r);
                int sx = Mathf.Max(1, Mathf.RoundToInt(step * 0.78f));
                int sy = Mathf.Max(1, Mathf.RoundToInt(step));
                Paint(st, near);
                for (int d = steps; d >= 1; d--)
                    LabelAt(seat, d * sx, d * sy, body, st);
                Paint(st, stroke);
                Rim(seat, body, st, Mathf.Max(2, Mathf.RoundToInt(2.4f * s)));
                Paint(st, fill);
                LabelAt(seat, 0, 0, body, st);
                return;
            }

            var extrudeNear = new Color(28f / 255f, 44f / 255f, 102f / 255f, 1f);
            var extrudeFar = new Color(4f / 255f, 7f / 255f, 18f / 255f, 1f);
            int layers = 11;
            float stepPx = 1.25f * s * scale;
            float cursor = x;
            var prev = GUI.color;
            for (int i = 0; i < n; i++)
            {
                float w = widths[i] * scale;
                var face = new Rect(cursor, y, w, h);
                if (sprs[i] != null && sprs[i].texture != null)
                {
                    for (int d = layers; d >= 1; d--)
                    {
                        float u = d / (float)layers;
                        GUI.color = Color.Lerp(extrudeNear, extrudeFar, u);
                        GUI.DrawTexture(
                            new Rect(face.x + d * stepPx * 0.78f, face.y + d * stepPx, w, h),
                            sprs[i].texture, ScaleMode.ScaleToFit, true);
                    }
                    GUI.color = Color.white;
                    GUI.DrawTexture(face, sprs[i].texture, ScaleMode.ScaleToFit, true);
                }
                cursor += w + tracking * h;
            }
            GUI.color = prev;
        }

        static bool IsMagentaPlaceholder(Sprite spr)
        {
            if (spr == null || spr.texture == null) return true;
            // LoadNew missing art is a solid magenta tex — reject it for title glyphs.
            try
            {
                var tex = spr.texture;
                if (!tex.isReadable) return spr.name != null && spr.name.IndexOf("missing", System.StringComparison.OrdinalIgnoreCase) >= 0;
                var c = tex.GetPixel(tex.width / 2, tex.height / 2);
                return c.r > 0.9f && c.g < 0.15f && c.b > 0.9f;
            }
            catch { return false; }
        }

        static Sprite SplashPointedV()
        {
            if (_splashPointedV != null) return _splashPointedV;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            if (!tex.LoadImage(FinaleVBytes.Png))
                return SpriteCatalog.Letter('V');
            _splashPointedV = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 200f);
            _splashPointedV.name = "SplashFinaleV";
            return _splashPointedV;
        }

        void EnsureSplashFlutters() => EnsureHaloFlutters(ref _splashFlutters, SplashTitleHalo());

        void EnsureHaloFlutters(ref SplashFlutter[] flutters, Rect h)
        {
            const int n = 5;
            // One ring. Nested radii stacked birds on the same ray; a hair of
            // depth stays, and the per-frame spacer keeps that from overlapping.
            float rx = Mathf.Max(10f, h.width * 0.42f);
            float ry = Mathf.Max(10f, h.height * 0.48f);
            const float ring = 0.86f;
            if (flutters != null && flutters.Length == n)
            {
                for (int i = 0; i < n; i++)
                {
                    float k = ring * (0.984f + 0.008f * i);
                    flutters[i].RadiusX = rx * k;
                    flutters[i].RadiusY = ry * k;
                    flutters[i].Angle = Mathf.Repeat(flutters[i].Angle, Mathf.PI * 2f);
                }
                return;
            }
            var prev = flutters;
            flutters = new SplashFlutter[n];
            var cols = new[] { BirdColor.Ruby, BirdColor.Gold, BirdColor.Teal, BirdColor.Violet, BirdColor.Peach };
            float step = Mathf.PI * 2f / n;
            for (int i = 0; i < n; i++)
            {
                float k = ring * (0.984f + 0.008f * i);
                bool keep = prev != null && i < prev.Length;
                flutters[i] = new SplashFlutter
                {
                    // Join/leave keeps the angle it already had so the ease can
                    // walk it to the new slot instead of popping.
                    Angle = keep ? Mathf.Repeat(prev[i].Angle, Mathf.PI * 2f) : i * step,
                    Slot = i * step,
                    Speed = keep ? prev[i].Speed : HaloOrbitSpeed + Random.Range(-0.06f, 0.06f),
                    RadiusX = rx * k,
                    RadiusY = ry * k,
                    BobPhase = keep ? prev[i].BobPhase : i * 1.17f,
                    Col = keep ? prev[i].Col : cols[i]
                };
            }
        }

        // Soft bounds around the stacked title mark (logo companions only).
        // Scaled about the fixed top edge with SplashLogoScale so the ring
        // stays tight on the smaller wordmark and clear of the right rail.
        static Rect SplashTitleHalo()
        {
            float top = TopHud();
            float ui = Mathf.Max(Screen.height / 720f, 1f);
            float capH = 56f * ui;
            float rowGap = 4f * ui;
            float titleH = capH * 2f + rowGap;
            float padX = Screen.width * 0.18f; // tighter logo halo
            float padY = 16f * ui;
            float x = padX;
            float y = Mathf.Max(4f, top - padY * 0.35f);
            float w = Screen.width - padX * 2f;
            float haloH = titleH + padY;
            float mid = Screen.width * 0.5f;
            x = mid + (x - mid) * SplashLogoScale;
            y = top + (y - top) * SplashLogoScale;
            w *= SplashLogoScale;
            haloH *= SplashLogoScale;
            return new Rect(x, y, w, haloH);
        }

        // behind=true: upper half of ellipse draws under letter faces (true z-weave).
        // Radii stay tight + positions clamped to halo — no #89 wander regress.
        void DrawAmbientSplashBirds(float s, bool behind)
        {
            EnsureSplashFlutters();
            DrawHaloBirds(ref _splashFlutters, SplashTitleHalo(), s, behind, HaloBirdIcon(s) * SplashLogoScale, ref _splashHaloTick);
        }

        // One saved bird on a short leafy twig between the title and the play flower,
        // after level 1 is cleared and a color has been adopted. Toes stay on that
        // one pad while it bobs and preens. It flaps only in flight, then lands again.
        // A clear owes a left-to-right settle. The streak board fills that gap until
        // it tucks, so the cross waits; a home lesson perches aside. The adoption
        // settle clears the owed cross so it does not play a second time.
        void ArmAvatarCross()
        {
            _avatarCross = true;
            _avatarCrossing = false;
            _avatarGliding = false;
            _avatarDrew = false;
            _avatarHappy = 0f;
        }

        void TickHomeAvatar()
        {
            if (!_splash || _home != HomeFace.Splash || HomeLessonUp() || !AvatarAdopted())
                CloseAvatarRename();
            if (GamePause.Paused) return;
            if (!_splash || _home != HomeFace.Splash) return;
            if (_avatarTick == Time.frameCount) return;
            _avatarTick = Time.frameCount;

            float s = Mathf.Max(Screen.height / 720f, 1f);
            if (!AvatarHomeOn())
            {
                _avatarTail = 0f;
                return;
            }
            _avatarTail = AvatarPlateDrop(s);
            float dt = Mathf.Clamp(Time.unscaledDeltaTime, 0f, 0.05f);
            _avatarClock += dt;
            if (_avatarFlapT > 0f) _avatarFlapT = Mathf.Max(0f, _avatarFlapT - dt);
            if (_avatarHappy > 0f) _avatarHappy = Mathf.Max(0f, _avatarHappy - dt / 0.36f);
            _avatarCol = SavedAvatar();

            float icon = HomeAvatarIcon(s);
            FillAvatarPerches(s);
            if (DailyCardHoldsBird())
            {
                _avatarCrossing = false;
                _avatarGliding = false;
                _avatarHold = false;
                _avatarPose = AvatarPose.Perched;
                StickAvatarToPerch();
                PoseAvatar(dt, false);
                return;
            }
            if (!_avatarPlaced)
            {
                _avatarPlaced = true;
                _avatarPerchIx = HomeBranchAnchor();
                _avatarPos = AvatarHomePoint();
                _avatarPose = AvatarPose.Perched;
                _awayOn = false;
                _avatarBobPhase = Random.Range(0f, 6.28f);
                _avatarNextGlide = _avatarClock + Random.Range(3.4f, 6.2f);
                _avatarNextFlap = _avatarClock + Random.Range(2.8f, 5.2f);
            }
            TrackGlideTarget();

            bool tutor = HomeTutorLive();
            if (tutor)
            {
                // A rename sheet steps the bird aside without eating the hop or
                // replaying the entrance. A real lesson still cancels both.
                bool renameOnly = _avatarRename && !HomeLessonUp();
                // Tapping the named bird opens the rename sheet and the bird stays on
                // its branch (PerchAnchor via StickAvatarToPerch). It used to yield to
                // the "clearest seat", which flew/teleported it to the left side.
                if (renameOnly && !_avatarCrossing && !_avatarGliding
                    && _avatarPose == AvatarPose.Perched && _avatarPlaced)
                {
                    _avatarHold = false;
                    PoseAvatar(dt, false);
                    return;
                }
                if (!renameOnly)
                    _avatarHappy = 0f;
                if (_avatarCrossing)
                {
                    _avatarCrossing = false;
                    if (!renameOnly)
                        _avatarCross = true;
                }
                _avatarHold = false;
                TickAvatarYield(s, icon, dt);
                _avatarTutor = true;
                PoseAvatar(dt, false);
                return;
            }

            bool leftTutor = _avatarTutor;
            _avatarTutor = false;
            // Payout sign owns the gap. Hold the entrance offscreen until it leaves.
            if (_avatarCross && _streakSlide >= 0f)
            {
                _avatarHold = true;
                _avatarCrossing = false;
                _avatarGliding = false;
                return;
            }

            _avatarHold = false;
            if (_avatarCross && !_avatarCrossing)
                BeginAvatarCross(icon);
            else if (leftTutor)
                BeginAvatarReturn(s);
            else if (!_avatarCrossing && !_avatarGliding && _avatarPose == AvatarPose.Perched
                && _avatarClock >= _avatarNextGlide)
                BeginAvatarGlide();

            if (_avatarPose == AvatarPose.Landing)
                StepAvatarLand(dt, s);
            else if (_avatarCrossing)
                TickAvatarCross(dt, s);
            else if (_avatarGliding)
                TickAvatarSlide(dt, s, 10f * s, false);
            PoseAvatar(dt, true);
        }

        // On a pad: toes planted, no bob (PerchAnchor), and the occasional preen.
        // In the air the pose is already Flying or Landing, so the wings stay out.
        void PoseAvatar(float dt, bool preen)
        {
            bool flying = _avatarPose == AvatarPose.Flying
                || _avatarPose == AvatarPose.Landing
                || _avatarCrossing
                || _avatarGliding;
            if (flying)
            {
                _avatarPreenT = 0f;
                _avatarFlapT = 0.3f;
                return;
            }
            _avatarFlapT = 0f;
            StickAvatarToPerch();
            if (_avatarPreenT > 0f)
                _avatarPreenT = Mathf.Max(0f, _avatarPreenT - dt);
            if (!preen || _avatarHappy > 0.05f) return;
            if (_avatarClock < _avatarNextFlap) return;
            _avatarPreenT = 0.72f;
            _avatarNextFlap = _avatarClock + Random.Range(3.4f, 6.8f);
        }

        // Toes on the fixed home pad. Off that pad, or still in the air, is not parked.
        bool AvatarParked()
        {
            if (_avatarPose == AvatarPose.Flying || _avatarPose == AvatarPose.Landing) return false;
            if (_avatarCrossing || _avatarGliding) return false;
            if (!_avatarPlaced) return false;
            var seat = AvatarHomePoint();
            var d = seat - _avatarPos;
            return d.sqrMagnitude <= 36f;
        }

        void StickAvatarToPerch()
        {
            if (_avatarPose == AvatarPose.Flying || _avatarPose == AvatarPose.Landing) return;
            if (_avatarCrossing || _avatarGliding || _avatarHold || !_avatarPlaced) return;
            _awayOn = false;
            if (_avatarPerchIx >= 0 && _avatarPerchIx < _avatarPerches.Length)
                _avatarPos = _avatarPerches[_avatarPerchIx];
            _avatarFaceLeft = _avatarPos.x >= Screen.width * 0.5f;
        }

        void TrackGlideTarget()
        {
            if (!_avatarGliding) return;
            if (_avatarPerchIx < 0 || _avatarPerchIx >= _avatarPerches.Length) return;
            // A lesson glide aims at clear air. Only a hop between pads tracks the seat.
            if (_avatarPose != AvatarPose.Flying) return;
            if ((_avatarGlideTo - _avatarPerches[_avatarPerchIx]).sqrMagnitude > 64f) return;
            _avatarGlideTo = _avatarPerches[_avatarPerchIx];
        }

        // Lesson or rename covers the pad: circle in the clear, wings out.
        // Pad is free: glide home, then land. Never a rest pose in the air, and
        // never move the twig to follow the bird.
        void TickAvatarYield(float s, float icon, float dt)
        {
            int blocks = FillAvatarBlocks(s);
            var look = AvatarTutorLook(s);
            float pad = 6f * s;
            int seat = BestClearSeat(s, icon, blocks, look);
            bool open = seat >= 0;
            if (open)
            {
                _avatarPerchIx = seat;
                _awayOn = false;
                var home = _avatarPerches[seat];
                if (_avatarPose == AvatarPose.Landing)
                {
                    StepAvatarLand(dt, s);
                    return;
                }
                float near = 20f * s;
                if ((_avatarPos - home).sqrMagnitude <= near * near)
                {
                    if (_avatarPose == AvatarPose.Flying || _avatarGliding)
                        BeginAvatarLand(_avatarPos, home, false);
                    else
                    {
                        _avatarGliding = false;
                        _avatarPose = AvatarPose.Perched;
                        _avatarPos = home;
                        return;
                    }
                }
                else
                    AimAvatarFlight(home, s, 240f);
                if (_avatarPose == AvatarPose.Landing)
                    StepAvatarLand(dt, s);
                else if (_avatarGliding)
                    TickAvatarSlide(dt, s, 10f * s, true);
                return;
            }

            _awayOn = false;
            _avatarGliding = false;
            _avatarCrossing = false;
            _avatarPose = AvatarPose.Flying;
            var spot = AvatarClearPoint(s, icon, blocks, look);
            var lap = AvatarCircle(spot, icon, _avatarClock, out bool face);
            // Same flap reach as HomeWanderBounds, so the drawn quad stays on screen.
            float reach = icon * 0.5f * HomeFlapFit;
            float minX = reach + 4f;
            float maxX = Screen.width - reach - 4f;
            if (maxX < minX)
            {
                float mid = Screen.width * 0.5f;
                minX = mid;
                maxX = mid;
            }
            if (lap.x < minX) lap.x = minX;
            if (lap.x > maxX) lap.x = maxX;
            float minY = CaptionFloorY(s) + reach;
            float maxY = Screen.height - reach - 8f;
            if (maxY < minY)
            {
                float mid = (minY + maxY) * 0.5f;
                if (mid < reach) mid = reach;
                if (mid > Screen.height - reach) mid = Screen.height - reach;
                minY = mid;
                maxY = mid;
            }
            if (lap.y < minY) lap.y = minY;
            if (lap.y > maxY) lap.y = maxY;
            _avatarPos = lap;
            _avatarFaceLeft = face;
        }

        // Keep an in-flight glide when the pad is still the target. Otherwise start one.
        void AimAvatarFlight(Vector2 dest, float s, float speed)
        {
            _avatarPose = AvatarPose.Flying;
            float slack = 18f * s;
            if (_avatarGliding && (dest - _avatarGlideTo).sqrMagnitude <= slack * slack) return;
            float dist = Vector2.Distance(_avatarPos, dest);
            StartAvatarGlide(dest, Mathf.Clamp(dist / (speed * s), 0.28f, 0.70f));
        }

        void StartAvatarGlide(Vector2 dest, float dur)
        {
            _avatarPose = AvatarPose.Flying;
            _avatarCrossing = false;
            _avatarGliding = true;
            _avatarGlideFrom = _avatarPos;
            _avatarGlideTo = dest;
            _avatarGlideT = 0f;
            if (dur < 0.2f) dur = 0.2f;
            _avatarGlideDur = dur;
            if (Mathf.Abs(dest.x - _avatarPos.x) > 2f)
                _avatarFaceLeft = dest.x < _avatarPos.x;
        }

        void BeginAvatarCross(float icon)
        {
            _avatarCrossing = true;
            _avatarCross = false;
            _avatarGliding = false;
            _avatarPose = AvatarPose.Flying;
            _avatarCrossT = 0f;
            _avatarCrossDur = 1.15f;
            var home = AvatarHomePoint();
            float s = Mathf.Max(Screen.height / 720f, 1f);
            var box = HomeWanderBounds(s, icon);
            var from = _avatarDrew ? _avatarPos : new Vector2(box.xMin, box.center.y);
            if (from.x < 0f || from.y < 0f || from.x > Screen.width || from.y > Screen.height)
                from = new Vector2(box.xMin, box.center.y);
            _avatarCrossFrom = from;
            _avatarCrossTo = home;
            _avatarPos = _avatarCrossFrom;
            _avatarPerchIx = HomeBranchAnchor();
            _awayOn = false;
            _avatarFaceLeft = _avatarCrossTo.x < _avatarCrossFrom.x;
        }

        void BeginAvatarGlide()
        {
            int next = _avatarPerchIx;
            for (int guard = 0; guard < 6; guard++)
            {
                int pick = Random.Range(0, _avatarPerches.Length);
                if (pick == _avatarPerchIx) continue;
                next = pick;
                break;
            }
            var dest = _avatarPerches[next];
            if ((dest - _avatarPos).sqrMagnitude < 9f)
            {
                _avatarNextGlide = _avatarClock + Random.Range(2.5f, 4.5f);
                return;
            }
            _avatarPerchIx = next;
            _awayOn = false;
            StartAvatarGlide(dest, Random.Range(0.72f, 1.05f));
            _avatarNextGlide = _avatarClock + Random.Range(4.6f, 8.2f);
        }

        // A lesson may have sent the bird into the air. Fly back, then fold the wings.
        void BeginAvatarReturn(float s)
        {
            var home = AvatarHomePoint();
            float slack = 28f * s;
            float slack2 = slack * slack;
            _awayOn = false;
            _avatarPerchIx = HomeBranchAnchor();
            if (_avatarPose == AvatarPose.Landing) return;
            if (_avatarGliding && (_avatarGlideTo - home).sqrMagnitude <= slack2)
            {
                _avatarPose = AvatarPose.Flying;
                return;
            }
            if (!_avatarGliding && (_avatarPos - home).sqrMagnitude <= slack2)
            {
                if (_avatarPose == AvatarPose.Flying)
                    BeginAvatarLand(_avatarPos, home, false);
                else
                {
                    _avatarPose = AvatarPose.Perched;
                    _avatarPos = home;
                }
                return;
            }
            float dist = Vector2.Distance(_avatarPos, home);
            StartAvatarGlide(home, Mathf.Clamp(dist / (280f * s), 0.40f, 0.95f));
            _avatarNextGlide = _avatarClock + Random.Range(4.6f, 8.2f);
        }

        void TickAvatarCross(float dt, float s)
        {
            _avatarCrossT += dt;
            float u = _avatarCrossDur > 0.01f ? Mathf.Clamp01(_avatarCrossT / _avatarCrossDur) : 1f;
            float e = Mathf.SmoothStep(0f, 1f, u);
            var p = Vector2.Lerp(_avatarCrossFrom, _avatarCrossTo, e);
            p.y -= Mathf.Sin(u * Mathf.PI) * 28f * s;
            _avatarPos = p;
            _avatarFlapT = 0.3f;
            _avatarFaceLeft = _avatarCrossTo.x < _avatarCrossFrom.x;
            if (u < 1f) return;
            _avatarCrossing = false;
            _avatarPos = _avatarCrossTo;
            _avatarPerchIx = HomeBranchAnchor();
            _avatarPose = AvatarPose.Perched;
            _avatarFlapT = 0f;
            _avatarPreenT = 0f;
            _awayOn = false;
            _avatarNextGlide = _avatarClock + Random.Range(4.2f, 7.5f);
        }

        void TickAvatarSlide(float dt, float s, float arc, bool tutorHop)
        {
            if (tutorHop) arc = Mathf.Min(arc, 12f * s);
            _avatarGlideT += dt;
            float u = _avatarGlideDur > 0.01f ? Mathf.Clamp01(_avatarGlideT / _avatarGlideDur) : 1f;
            float e = Mathf.SmoothStep(0f, 1f, u);
            var p = Vector2.Lerp(_avatarGlideFrom, _avatarGlideTo, e);
            p.y -= Mathf.Sin(u * Mathf.PI) * arc;
            _avatarPos = p;
            _avatarFlapT = 0.25f;
            if (Mathf.Abs(_avatarGlideTo.x - _avatarGlideFrom.x) > 2f)
                _avatarFaceLeft = _avatarGlideTo.x < _avatarGlideFrom.x;
            if (u < 1f) return;
            _avatarPos = _avatarGlideTo;
            _avatarGliding = false;
            _avatarFlapT = 0f;
            _avatarPreenT = 0f;
            _awayOn = false;
            bool landed = _avatarPerchIx >= 0 && _avatarPerchIx < _avatarPerches.Length
                && (_avatarGlideTo - _avatarPerches[_avatarPerchIx]).sqrMagnitude <= 36f;
            _avatarPose = landed ? AvatarPose.Perched : AvatarPose.Flying;
        }

        // Same center the home bird draws at. Fixed body is HomeAvatarIcon (1.48×).
        bool HomeAvatarFrame(float s, out Vector2 c, out float icon, out bool onPerch)
        {
            c = default;
            icon = 0f;
            onPerch = false;
            if (!AvatarHomeOn()) return false;
            _avatarTail = AvatarPlateDrop(s);
            if (_avatarHold || !_avatarPlaced) return false;
            // Payout sign fills the gap. A lesson still draws so the perch stays visible under the caption.
            if (_streakSlide >= 0f && !_avatarTutor) return false;
            icon = HomeAvatarIcon(s);
            FillAvatarPerches(s);
            // The open Daily card holds the bird on the fixed pad at the shared size.
            if (DailyCardHoldsBird())
            {
                _avatarCrossing = false;
                _avatarGliding = false;
                _avatarPose = AvatarPose.Perched;
                StickAvatarToPerch();
            }
            else if (_avatarPose != AvatarPose.Flying && _avatarPose != AvatarPose.Landing
                && !_avatarCrossing && !_avatarGliding)
                StickAvatarToPerch();
            onPerch = AvatarParked() && _avatarHappy <= 0.02f;
            // Rest pose: no bob, no hover. PerchAnchor is the one shared seat.
            c = PerchAnchor(_avatarPos, icon, onPerch, _avatarHappy, icon * 0.16f);
            if (onPerch)
                _avatarFaceLeft = c.x >= Screen.width * 0.5f;
            return true;
        }

        void DrawHomeAvatar(float s)
        {
            _avatarDrawR = default;
            _avatarPlateR = default;
            if (!HomeAvatarFrame(s, out var c, out float icon, out bool onPerch)) return;
            DrawHomeLimbSide(false);
            float preen = 0f;
            if (onPerch && _avatarPreenT > 0f)
            {
                float u = 1f - Mathf.Clamp01(_avatarPreenT / 0.72f);
                preen = Mathf.Sin(u * Mathf.PI) * (_avatarFaceLeft ? 16f : -16f);
            }
            var prevMat = GUI.matrix;
            if (Mathf.Abs(preen) > 0.4f)
                GUIUtility.RotateAroundPivot(preen, new Vector2(c.x, c.y + icon * 0.31f));
            var prev = GUI.color;
            GUI.color = Color.white;
            var drawn = DrawAvatarBird(_avatarCol, SavedAvatarKit(), c, icon, _avatarFaceLeft, onPerch, _avatarClock, false, BirdIdle.KitSlotHome);
            GUI.matrix = prevMat;
            GUI.color = prev;
            if (drawn.width <= 2f) return;
            _avatarDrawR = drawn;
            _avatarDrew = true;
            bool wings = BirdIdle.UseFlyingPose(onPerch, false);
            _avatarPlateR = DrawAvatarPlate(c, icon, SavedAvatarName(), s, _avatarCol, _avatarFaceLeft, wings, _avatarClock, true);
        }

        // Rails already took their taps. The play flower has not. A lesson click
        // is still swallowed here so the flower cannot start under the bird.
        bool HitHomeAvatar(float s, bool tutorUp)
        {
            if (!HomeAvatarFrame(s, out var c, out float icon, out bool onPerch)) return false;
            if (VipOffer.IsOpen || _dailyOpen || _dailyAskOpen || _welcomeOpen || _adoptLive || _settingsOpen) return false;
            float pad = HomeBirdHitPad * s;
            var bird = new Rect(
                c.x - icon * 0.5f - pad,
                c.y - icon * 0.5f - pad,
                icon + pad * 2f,
                icon + pad * 2f);
            bool wings = BirdIdle.UseFlyingPose(onPerch, false);
            var plate = NameTagRect(c, icon, s, _avatarCol, SavedAvatarName(), _avatarFaceLeft, wings, _avatarClock, true);
            bool birdHit = HitHomeFirst(bird, out _);
            bool plateHit = plate.width > 2f && HitHomeFirst(plate, out _);
            if (tutorUp || HomeTutorLive()) return false;
            if (birdHit || plateHit)
                PokeAvatar();
            return birdHit || plateHit;
        }

        void PokeAvatar()
        {
            _avatarHappy = 1f;
            Sfx.Chirp(_avatarCol);
            OpenAvatarRename();
        }

        static BirdColor SavedAvatar()
        {
            int n = PlayerPrefs.GetInt(AvatarPref, (int)BirdColor.Gold);
            if (n < (int)BirdColor.Ruby || n > (int)BirdColor.Peach) return BirdColor.Gold;
            return (BirdColor)n;
        }

        bool HomeTutorLive()
        {
            if (!_splash || _home != HomeFace.Splash) return false;
            return HomeLessonUp() || _avatarRename;
        }

        float AvatarIcon(float s) => HomeBirdIcon(s);

        // The LEVEL square, and the shared hit the press and the launch both use.
        static Rect FlowerPlayRect()
        {
            return SplashPress.LayoutLevel(Screen.width, Screen.height, Screen.safeArea).Rect;
        }

        void AvatarGap(float s, float icon, out float top, out float bot, out float left, out float right)
        {
            float half = icon * 0.5f;
            var flower = FlowerPlayRect();
            float titleBottom = TopHud() + (56f * 2f + 4f) * s;
            float tail = AvatarPlateDrop(s);
            top = titleBottom + half + 8f * s;
            bot = flower.yMin - half - tail - 8f * s;
            if (bot < top)
            {
                float mid = (titleBottom + flower.yMin) * 0.5f;
                top = mid;
                bot = mid;
            }
            AvatarChannel(s, out float chL, out float chR);
            left = chL + half;
            right = chR - half;
            if (right < left)
            {
                float mid = (chL + chR) * 0.5f;
                left = mid;
                right = mid;
            }
        }

        // Right-edge twig. The bird size is not an input.
        void FillAvatarPerches(float s) => PlaceHomeBranch(s);

        int FillAvatarBlocks(float s)
        {
            int n = 0;
            float pad = 8f * s;
            if (_avatarRename && _avatarRenameR.width > 2f)
                _avatarBlock[n++] = _avatarRenameR;
            bool line = _hiveIntroLive || _pokerIntroLive || (_dailyIntroLive && !_dailyOpen && !_dailyAskOpen);
            if (line)
                _avatarBlock[n++] = CoachPanelRect(SplashIntroBand(s));
            if (_welcomeOpen && n < _avatarBlock.Length)
            {
                WelcomeLayout(s, out var card, out _, out _, out _, out _);
                _avatarBlock[n++] = PadRect(card, pad);
            }
            if (_dailyAskOpen && n < _avatarBlock.Length)
            {
                DailyAskLayout(s, out var card, out _, out _, out _);
                _avatarBlock[n++] = PadRect(card, pad);
            }
            if (_dailyIntroLive && _dailyOpen && !_dailyAskOpen && n < _avatarBlock.Length)
            {
                DailyLayout(s, out var card, out var flower, out _, out _);
                _avatarBlock[n++] = PadRect(UnionRect(card, flower), pad);
            }
            if (n < _avatarBlock.Length && HomeGloveBox(s, out var glove))
                _avatarBlock[n++] = glove;
            return n;
        }

        Vector2 AvatarTutorLook(float s)
        {
            if (_avatarRename && _avatarRenameR.width > 2f)
                return _avatarRenameR.center;
            if (_gloveVis) return _cueAimGui;
            if (_welcomeOpen)
            {
                WelcomeLayout(s, out var card, out _, out _, out _, out _);
                return card.center;
            }
            if (_dailyAskOpen)
            {
                DailyAskLayout(s, out var card, out _, out _, out _);
                return card.center;
            }
            if (_dailyIntroLive && _dailyOpen) return DailyClaimAim(s);
            if (_hiveIntroLive)
            {
                var hive = SplashHiveRect();
                if (hive.width > 2f) return hive.center;
            }
            if (_pokerIntroLive)
            {
                var poker = SplashPokerRect();
                if (poker.width > 2f) return poker.center;
            }
            if (_dailyIntroLive)
            {
                var daily = SplashDailyRect();
                if (daily.width > 2f) return daily.center;
            }
            if (_welcomeGlove)
            {
                var vip = SplashNoAdsRect();
                if (vip.width > 2f) return vip.center;
            }
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.45f);
        }

        bool HomeGloveBox(float s, out Rect box)
        {
            box = default;
            if (!_gloveVis || _coachFade < 0.03f) return false;
            if (!HomeTutorLive()) return false;
            float dh = GloveDh(s);
            var pivot = _gloveShown;
            float bob = Mathf.Sin(Time.unscaledTime * 2.35f) * dh * 0.028f * (1f - _gloveDip);
            float rad = _gloveShownAng * Mathf.Deg2Rad;
            pivot += new Vector2(Mathf.Sin(rad), -Mathf.Cos(rad)) * bob;
            var rect = GloveRect(pivot, dh, _gloveMirror);
            return RotAabb(rect.x, rect.y, rect.width, rect.height, pivot, _gloveShownAng, 8f * s, out box);
        }

        // A seat that misses the caption and the glove. Stay put when the current pad is already clear.
        int BestClearSeat(float s, float icon, int blocks, Vector2 look)
        {
            float pad = 6f * s;
            if (blocks <= 0) return HomeBranchAnchor();
            if (_avatarPerchIx >= 0 && _avatarPerchIx < _avatarPerches.Length
                && !AvatarHits(_avatarPerches[_avatarPerchIx], icon, blocks, pad))
                return _avatarPerchIx;
            int best = -1;
            int any = -1;
            float bestScore = -1f;
            float anyScore = -1f;
            for (int i = 0; i < _avatarPerches.Length; i++)
            {
                var p = _avatarPerches[i];
                if (AvatarHits(p, icon, blocks, pad)) continue;
                float score = (p - look).sqrMagnitude;
                if (!AvatarHitsRails(p, icon, s) && score > bestScore)
                {
                    bestScore = score;
                    best = i;
                }
                if (score > anyScore)
                {
                    anyScore = score;
                    any = i;
                }
            }
            return best >= 0 ? best : any;
        }

        Vector2 AvatarClearPoint(float s, float icon, int blocks, Vector2 look)
        {
            AvatarGap(s, icon, out float top, out float bot, out float left, out float right);
            bool wantLeft = look.x >= Screen.width * 0.5f;
            float side = wantLeft ? left : right;
            float other = wantLeft ? right : left;
            float mid = (top + bot) * 0.5f;
            float pad = 6f * s;
            Vector2 spot;
            if (TryAvatarSpot(new Vector2(side, mid), icon, blocks, pad, s, true, out spot)) return spot;
            if (TryAvatarSpot(new Vector2(side, top), icon, blocks, pad, s, true, out spot)) return spot;
            if (TryAvatarSpot(new Vector2(side, bot), icon, blocks, pad, s, true, out spot)) return spot;
            if (TryAvatarSpot(new Vector2(other, mid), icon, blocks, pad, s, true, out spot)) return spot;
            if (TryAvatarSpot(new Vector2(other, top), icon, blocks, pad, s, true, out spot)) return spot;
            if (TryAvatarSpot(new Vector2(other, bot), icon, blocks, pad, s, true, out spot)) return spot;
            float reach = icon * 0.5f + pad + 2f;
            float minX = icon * 0.5f;
            float maxX = Mathf.Max(minX, Screen.width - minX);
            float minY = minX;
            float maxY = Mathf.Max(minY, Screen.height - minY);
            for (int i = 0; i < blocks; i++)
            {
                var b = _avatarBlock[i];
                float y = Mathf.Clamp(b.center.y, top, bot);
                float outWant = wantLeft ? b.xMin - reach : b.xMax + reach;
                float outOther = wantLeft ? b.xMax + reach : b.xMin - reach;
                // Keep a side perch that clears the plate. Clamping it back into the gap
                // can drop it onto the caption, and the short push then climbs into the title.
                if (outWant >= minX && outWant <= maxX
                    && (TryAvatarSpot(new Vector2(outWant, y), icon, blocks, pad, s, true, out spot)
                        || TryAvatarSpot(new Vector2(outWant, y), icon, blocks, pad, s, false, out spot)))
                    return spot;
                if (outOther >= minX && outOther <= maxX
                    && (TryAvatarSpot(new Vector2(outOther, y), icon, blocks, pad, s, true, out spot)
                        || TryAvatarSpot(new Vector2(outOther, y), icon, blocks, pad, s, false, out spot)))
                    return spot;
                float x = Mathf.Clamp(b.center.x, left, right);
                if (TryAvatarSpot(new Vector2(x, Mathf.Clamp(b.yMin - reach, top, bot)), icon, blocks, pad, s, true, out spot)) return spot;
                if (TryAvatarSpot(new Vector2(x, Mathf.Clamp(b.yMax + reach, top, bot)), icon, blocks, pad, s, true, out spot)) return spot;
            }
            if (TryAvatarSpot(new Vector2(side, mid), icon, blocks, pad, s, false, out spot)) return spot;
            if (TryAvatarSpot(new Vector2(side, top), icon, blocks, pad, s, false, out spot)) return spot;
            if (TryAvatarSpot(new Vector2(other, mid), icon, blocks, pad, s, false, out spot)) return spot;

            Vector2 p = new Vector2(side, mid);
            for (int n = 0; n < 12; n++)
            {
                if (!AvatarHits(p, icon, blocks, pad)) break;
                Vector2 pushed = PushAvatar(p, icon, blocks, pad);
                if ((pushed - p).sqrMagnitude < 0.01f) break;
                p = pushed;
            }
            var clamped = new Vector2(Mathf.Clamp(p.x, minX, maxX), Mathf.Clamp(p.y, minY, maxY));
            // Clamping a side push back onto a full-width card would sit inside it.
            if (!AvatarHits(clamped, icon, blocks, pad)) return clamped;
            float midX = (left + right) * 0.5f;
            if (TryAvatarEscape(side, other, midX, blocks, icon, pad, out spot)) return spot;
            if (!AvatarHits(p, icon, blocks, pad)) return p;
            return clamped;
        }

        // On-screen perch just outside a caption, card, or glove. Rails may lose.
        bool TryAvatarEscape(float side, float other, float midX, int blocks, float icon, float pad, out Vector2 spot)
        {
            spot = default;
            float reach = icon * 0.5f + pad + 2f;
            float minX = icon * 0.5f;
            float maxX = Mathf.Max(minX, Screen.width - minX);
            float minY = icon * 0.5f;
            float maxY = Mathf.Max(minY, Screen.height - minY);
            float xWant = Mathf.Clamp(side, minX, maxX);
            float xOther = Mathf.Clamp(other, minX, maxX);
            float xMid = Mathf.Clamp(midX, minX, maxX);
            for (int i = 0; i < blocks; i++)
            {
                var b = _avatarBlock[i];
                float above = b.yMin - reach;
                float below = b.yMax + reach;
                if (above >= minY && above <= maxY)
                {
                    if (TryAvatarSpot(new Vector2(xWant, above), icon, blocks, pad, 0f, false, out spot)) return true;
                    if (TryAvatarSpot(new Vector2(xOther, above), icon, blocks, pad, 0f, false, out spot)) return true;
                    if (TryAvatarSpot(new Vector2(xMid, above), icon, blocks, pad, 0f, false, out spot)) return true;
                }
                if (below >= minY && below <= maxY)
                {
                    if (TryAvatarSpot(new Vector2(xWant, below), icon, blocks, pad, 0f, false, out spot)) return true;
                    if (TryAvatarSpot(new Vector2(xOther, below), icon, blocks, pad, 0f, false, out spot)) return true;
                    if (TryAvatarSpot(new Vector2(xMid, below), icon, blocks, pad, 0f, false, out spot)) return true;
                }
                float yMid = Mathf.Clamp(b.center.y, minY, maxY);
                if (TryAvatarSpot(new Vector2(b.xMin - reach, yMid), icon, blocks, pad, 0f, false, out spot)
                    && spot.x >= minX && spot.x <= maxX)
                    return true;
                if (TryAvatarSpot(new Vector2(b.xMax + reach, yMid), icon, blocks, pad, 0f, false, out spot)
                    && spot.x >= minX && spot.x <= maxX)
                    return true;
            }
            return false;
        }

        bool TryAvatarSpot(Vector2 p, float icon, int blocks, float pad, float s, bool rails, out Vector2 spot)
        {
            spot = p;
            if (AvatarHits(p, icon, blocks, pad)) return false;
            if (rails && AvatarHitsRails(p, icon, s)) return false;
            return true;
        }

        Vector2 PushAvatar(Vector2 p, float icon, int blocks, float pad)
        {
            var body = AvatarBody(p, icon, pad);
            for (int i = 0; i < blocks; i++)
            {
                var b = _avatarBlock[i];
                if (!body.Overlaps(b)) continue;
                float pushL = body.xMax - b.xMin;
                float pushR = b.xMax - body.xMin;
                float pushU = body.yMax - b.yMin;
                float pushD = b.yMax - body.yMin;
                float best = pushL;
                int axis = 0;
                if (pushR < best) { best = pushR; axis = 1; }
                if (pushU < best) { best = pushU; axis = 2; }
                if (pushD < best) { best = pushD; axis = 3; }
                float step = best + 2f;
                if (axis == 0) p.x -= step;
                else if (axis == 1) p.x += step;
                else if (axis == 2) p.y -= step;
                else p.y += step;
                body = AvatarBody(p, icon, pad);
            }
            return p;
        }

        bool AvatarHits(Vector2 p, float icon, int blocks, float pad)
        {
            if (blocks <= 0) return false;
            var body = AvatarBody(p, icon, pad);
            for (int i = 0; i < blocks; i++)
                if (body.Overlaps(_avatarBlock[i])) return true;
            return false;
        }

        bool AvatarSegmentHits(Vector2 a, Vector2 b, float icon, int blocks, float pad, float arc)
        {
            for (int i = 0; i <= 4; i++)
            {
                float u = i / 4f;
                var p = Vector2.Lerp(a, b, u);
                p.y -= Mathf.Sin(u * Mathf.PI) * arc;
                if (AvatarHits(p, icon, blocks, pad)) return true;
            }
            return false;
        }

        bool AvatarHitsRails(Vector2 p, float icon, float s) => AvatarRectHitsRails(AvatarBody(p, icon, 4f * s), s);

        bool AvatarRectHitsRails(Rect body, float s)
        {
            if (RailHits(body, PiggyRect(s))) return true;
            if (RailHits(body, SplashHiveRect())) return true;
            if (RailHits(body, SplashPokerRect())) return true;
            if (RailHits(body, SplashDailyRect())) return true;
            var vip = SplashNoAdsRect();
            if (RailHits(body, vip)) return true;
            if (vip.width > 2f && RailHits(body, SplashNoAdsRibbon(vip))) return true;
            if (RailHits(body, FlowerPlayRect())) return true;
            return false;
        }

        static bool RailHits(Rect body, Rect rail) =>
            rail.width > 2f && rail.height > 2f && body.Overlaps(rail);

        Rect AvatarBody(Vector2 c, float icon, float pad)
        {
            float ui = Mathf.Max(Screen.height / 720f, 1f);
            float plateW = AvatarPlateW(icon, ui);
            float w = Mathf.Max(icon, plateW) + pad * 2f;
            float h = icon + pad * 2f + _avatarTail;
            return new Rect(c.x - w * 0.5f, c.y - icon * 0.5f - pad, w, h);
        }

        static Rect PadRect(Rect r, float p) =>
            new Rect(r.x - p, r.y - p, r.width + p * 2f, r.height + p * 2f);

        static Rect UnionRect(Rect a, Rect b)
        {
            float x0 = a.xMin < b.xMin ? a.xMin : b.xMin;
            float y0 = a.yMin < b.yMin ? a.yMin : b.yMin;
            float x1 = a.xMax > b.xMax ? a.xMax : b.xMax;
            float y1 = a.yMax > b.yMax ? a.yMax : b.yMax;
            return new Rect(x0, y0, Mathf.Max(0f, x1 - x0), Mathf.Max(0f, y1 - y0));
        }

        static float HaloBirdIcon(float s) => 36f * s;
        const float HaloFlapRate = 1.25f;

        void DrawHaloBirds(ref SplashFlutter[] flutters, Rect h, float s, bool behind, float icon, ref int stepFrame)
        {
            if (!GuiPaint()) return;
            if (flutters == null || flutters.Length == 0) return;
            Vector2 c = h.center;
            var prev = GUI.color;
            // OnGUI fires more than once a frame. Step the ring a single time or the
            // spacer double-applies and the orbit runs hot.
            if (behind && stepFrame != Time.frameCount)
            {
                stepFrame = Time.frameCount;
                StepTitleHalo(flutters, icon, s);
            }
            for (int i = 0; i < flutters.Length; i++)
            {
                var f = flutters[i];
                // y-down: Sin<0 = above title = behind letters; Sin>=0 = below = in front.
                // Split at 0 with no dead band — a ±0.10 gap made birds blink out at the sides.
                float depth = Mathf.Sin(f.Angle);
                bool isBehind = depth < 0f;
                if (behind != isBehind) continue;

                var off = TitleOffset(f, s);
                float x = c.x + off.x;
                float y = c.y + off.y;

                float scale = behind ? 0.92f : 1.04f;
                float iw = icon * scale;
                float vx = -Mathf.Sin(f.Angle) * f.RadiusX;
                bool faceLeft = vx < 0f;
                // Same wingbeat as perched birds (BirdIdle FlapRate 1.25 = 40 poses/s).
                // It was *11 (352 poses/s): far above the screen rate, so each
                // frame showed a random pose and the flap strobed.
                var spr = SpriteCatalog.BirdFrame(f.Col, Time.unscaledTime * HaloFlapRate + i * 0.37f, true);
                if (spr == null || spr.texture == null) continue;
                // Flight frames are bigger canvases (1504 px at a different PPU)
                // than the rest pose (1024 px). GUI ignores PPU, so size each
                // frame by its world size vs the rest pose or the bird pops.
                var restSpr = SpriteCatalog.BirdFrame(f.Col, 0f, false);
                if (restSpr != null && restSpr != spr && restSpr.pixelsPerUnit > 0f && spr.pixelsPerUnit > 0f)
                {
                    float restU = restSpr.rect.width / restSpr.pixelsPerUnit;
                    float frameU = spr.rect.width / spr.pixelsPerUnit;
                    if (restU > 0f) iw *= frameU / restU;
                }
                var r = new Rect(x - iw * 0.5f, y - iw * 0.5f, iw, iw);
                float dim = behind ? 0.74f : 0.98f;
                GUI.color = new Color(dim, dim, dim, behind ? 0.80f : 0.95f);
                if (faceLeft)
                {
                    var m = GUI.matrix;
                    GUIUtility.ScaleAroundPivot(new Vector2(-1f, 1f), r.center);
                    GUI.DrawTexture(r, spr.texture, ScaleMode.ScaleToFit, true);
                    GUI.matrix = m;
                }
                else
                    GUI.DrawTexture(r, spr.texture, ScaleMode.ScaleToFit, true);
            }
            GUI.color = prev;
        }

        // Widest flight frame vs the rest pose. GUI sizes by this, so the gap has to too.
        static float TitleWingMul()
        {
            if (_titleWing > 0f) return _titleWing;
            var rest = SpriteCatalog.BirdFrame(BirdColor.Gold, 0f, false);
            if (rest == null || rest.pixelsPerUnit <= 0f) return 1f;
            float rw = rest.rect.width / rest.pixelsPerUnit;
            if (rw <= 0.01f) return 1f;
            float max = rw;
            for (int k = 0; k < 8; k++)
            {
                var spr = SpriteCatalog.BirdFrame(BirdColor.Gold, k * 0.2f, true);
                if (spr == null || spr.pixelsPerUnit <= 0f) continue;
                float w = spr.rect.width / spr.pixelsPerUnit;
                if (w > max) max = w;
            }
            _titleWing = Mathf.Clamp(max / rw, 1f, 1.9f);
            return _titleWing;
        }

        // Same offset the drawer uses (ellipse + bob), so the gap test matches pixels.
        static Vector2 TitleOffset(SplashFlutter f, float s)
        {
            float rk = 1f + 0.018f * Mathf.Sin(Time.unscaledTime * 0.85f + f.BobPhase);
            float x = Mathf.Cos(f.Angle) * f.RadiusX * rk;
            float y = Mathf.Sin(f.Angle) * f.RadiusY * rk;
            float bobHz = 1.75f + 0.35f * Mathf.Sin(f.BobPhase * 1.3f);
            float bob = (1.3f + 0.5f * (0.5f + 0.5f * Mathf.Sin(f.BobPhase))) * s;
            y += Mathf.Sin(Time.unscaledTime * bobHz + f.BobPhase) * bob;
            return new Vector2(x, y);
        }

        // Each bird owns an angular slot on one ring. Speed/bob may lead a slot,
        // but a neighbor inside ~1.2× bird width is pushed out before the frame draws.
        // Join/leave keeps the old angle and eases toward the new even slots — no snap.
        static void StepTitleHalo(SplashFlutter[] fl, float icon, float s)
        {
            int n = fl.Length;
            if (n <= 0) return;
            if (n > _titleOrd.Length) n = _titleOrd.Length;
            float dt = Mathf.Clamp(Time.unscaledDeltaTime, 0f, 0.05f);
            if (n == 1)
            {
                var only = fl[0];
                only.Angle = Mathf.Repeat(only.Angle + only.Speed * dt, Mathf.PI * 2f);
                only.Slot = only.Angle;
                fl[0] = only;
                return;
            }

            float rate = 0f;
            float rMin = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                rate += fl[i].Speed;
                rMin = Mathf.Min(rMin, Mathf.Min(fl[i].RadiusX, fl[i].RadiusY));
            }
            rate /= n;
            for (int i = 0; i < n; i++)
            {
                var f = fl[i];
                f.Angle = Mathf.Repeat(f.Angle + rate * dt, Mathf.PI * 2f);
                fl[i] = f;
            }

            for (int i = 0; i < n; i++) _titleOrd[i] = i;
            for (int i = 1; i < n; i++)
            {
                int key = _titleOrd[i];
                float ka = fl[key].Angle;
                int j = i - 1;
                while (j >= 0 && fl[_titleOrd[j]].Angle > ka)
                {
                    _titleOrd[j + 1] = _titleOrd[j];
                    j--;
                }
                _titleOrd[j + 1] = key;
            }
            _titleAng[0] = fl[_titleOrd[0]].Angle;
            for (int k = 1; k < n; k++)
            {
                float a = fl[_titleOrd[k]].Angle;
                while (a + 0.0001f < _titleAng[k - 1]) a += Mathf.PI * 2f;
                _titleAng[k] = a;
            }
            float step = Mathf.PI * 2f / n;
            float mean = 0f;
            for (int k = 0; k < n; k++) mean += _titleAng[k] - k * step;
            mean /= n;

            rMin = Mathf.Max(8f, rMin);
            float minD = icon * 1.04f * TitleWingMul() * 1.2f;
            float chord = 2f * rMin * Mathf.Sin(Mathf.PI / n);
            if (chord > 1f) minD = Mathf.Min(minD, chord * 0.90f);
            float minAng = minD / rMin;
            float slack = Mathf.Max(0.02f, (step - minAng) * 0.32f);

            float gain = 1f - Mathf.Exp(-dt / 0.32f);
            for (int k = 0; k < n; k++)
            {
                int idx = _titleOrd[k];
                var f = fl[idx];
                float bias = Mathf.Clamp((f.Speed - rate) * 2.4f, -slack, slack);
                float wob = Mathf.Sin(Time.unscaledTime * (0.42f + 0.07f * idx) + f.BobPhase) * slack * 0.40f;
                float slot = mean + k * step + bias + wob;
                f.Slot = slot;
                float err = Mathf.DeltaAngle(f.Angle * Mathf.Rad2Deg, slot * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                f.Angle = Mathf.Repeat(f.Angle + err * gain, Mathf.PI * 2f);
                fl[idx] = f;
            }

            for (int pass = 0; pass < 8; pass++)
            {
                bool any = false;
                for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                {
                    if (!TitlePush(fl[i], fl[j], minD, s, out float half)) continue;
                    any = true;
                    var a = fl[i];
                    var b = fl[j];
                    a.Angle = Mathf.Repeat(a.Angle - half, Mathf.PI * 2f);
                    b.Angle = Mathf.Repeat(b.Angle + half, Mathf.PI * 2f);
                    fl[i] = a;
                    fl[j] = b;
                }
                if (!any) break;
            }
        }

        static bool TitlePush(SplashFlutter a, SplashFlutter b, float minD, float s, out float half)
        {
            half = 0f;
            float d = Vector2.Distance(TitleOffset(a, s), TitleOffset(b, s));
            float ang = Mathf.DeltaAngle(a.Angle * Mathf.Rad2Deg, b.Angle * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            float r = Mathf.Max(8f, (Mathf.Min(a.RadiusX, a.RadiusY) + Mathf.Min(b.RadiusX, b.RadiusY)) * 0.5f);
            float needAng = Mathf.Max(0f, minD / r - Mathf.Abs(ang));
            float needPix = d < minD ? (minD - d) / r : 0f;
            float need = Mathf.Max(needAng, needPix);
            if (need <= 0.0004f) return false;
            float sgn = ang >= 0f ? 1f : -1f;
            if (Mathf.Abs(ang) < 0.0001f) sgn = 1f;
            half = sgn * need * 0.5f;
            return true;
        }

        static readonly Vector2[] FlowerGlints =
        {
            new Vector2(0.50f, 0.12f),
            new Vector2(0.22f, 0.22f),
            new Vector2(0.78f, 0.20f),
            new Vector2(0.16f, 0.38f),
            new Vector2(0.84f, 0.36f),
            new Vector2(0.32f, 0.50f),
            new Vector2(0.68f, 0.49f)
        };

        static Texture2D GlowTex()
        {
            var g = SpriteCatalog.Glow;
            return g != null ? g.texture : Texture2D.whiteTexture;
        }

        static void DrawFlowerHalo(Rect rest, float sink, float gain = 1f)
        {
            if (!GuiPaint()) return;
            float t = Time.unscaledTime;
            float breathe = 0.5f + 0.5f * Mathf.Sin(t * 1.7f);
            float pad = rest.width * (0.03f + 0.035f * breathe);
            var glow = GlowTex();
            float a = (0.20f + 0.14f * breathe) * Mathf.Clamp01(gain);
            GUI.color = new Color(1f, 0.86f, 0.48f, a);
            GUI.DrawTexture(new Rect(rest.x - pad, rest.y + sink - pad, rest.width + pad * 2f, rest.height + pad * 2f), glow, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
        }

        static void DrawFlowerShimmer(Rect rest, float sink, float gain = 1f)
        {
            if (!GuiPaint()) return;
            float t = Time.unscaledTime;
            var glow = GlowTex();
            var face = new Rect(rest.x, rest.y + sink, rest.width, rest.height);
            gain = Mathf.Clamp01(gain);

            var disc = FlowerDisc(rest, sink);
            float sheenU = Mathf.Repeat(t * 0.32f, 1.65f);
            if (sheenU < 1f)
            {
                float fade = Mathf.Sin(sheenU * Mathf.PI);
                float x = disc.x + disc.width * (sheenU * 1.15f - 0.18f);
                var band = new Rect(x, disc.y + disc.height * 0.08f, disc.width * 0.28f, disc.height * 0.84f);
                GUI.color = new Color(1f, 0.96f, 0.82f, 0.48f * fade * gain);
                GUI.DrawTexture(band, glow, ScaleMode.ScaleToFit, true);
            }

            GUI.color = new Color(1f, 0.92f, 0.62f, (0.18f + 0.12f * (0.5f + 0.5f * Mathf.Sin(t * 2.05f))) * gain);
            GUI.DrawTexture(disc, glow, ScaleMode.ScaleToFit, true);

            float baseSz = rest.width * 0.10f;
            for (int i = 0; i < FlowerGlints.Length; i++)
            {
                float tw = Mathf.Sin(t * 2.05f + i * 1.13f);
                tw = Mathf.Max(0f, tw);
                tw = tw * tw;
                if (tw < 0.08f) continue;
                var uv = FlowerGlints[i];
                float sz = baseSz * (0.50f + 0.80f * tw);
                var r = new Rect(
                    face.x + face.width * uv.x - sz * 0.5f,
                    face.y + face.height * uv.y - sz * 0.5f,
                    sz, sz);
                GUI.color = new Color(1f, 0.95f, 0.72f, (0.28f + 0.62f * tw) * gain);
                GUI.DrawTexture(r, glow, ScaleMode.ScaleToFit, true);
            }
            GUI.color = Color.white;
        }

        void DrawSplashPokerButton(Rect r)
        {
            var spr = SpriteCatalog.Poker;
            if (spr != null && spr.texture != null && spr.rect.width > 32f)
            {
                DrawRailIcon(r, spr);
                return;
            }
            GUI.color = new Color(0.12f, 0.10f, 0.07f, 0.82f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // Card row centers on the screen. Safe-area insets apply only when left and right match.
        static void PokerRowSpan(out float x, out float width)
        {
            float sw = Mathf.Max(1f, Screen.width);
            var safe = Screen.safeArea;
            float left = 0f, right = 0f;
            if (safe.width > 8f && safe.height > 8f)
            {
                left = Mathf.Max(0f, safe.xMin);
                right = Mathf.Max(0f, sw - safe.xMax);
            }
            if (Mathf.Abs(left - right) <= 1f)
            {
                x = left;
                width = Mathf.Max(8f, sw - left - right);
            }
            else
            {
                x = 0f;
                width = sw;
            }
        }

        void MaybePokerHandAd()
        {
            if (_pokerAdRunning || !Ads.PokerAdDue) return;
            if (!PokerHandAdReady()) return;
            _pokerAdRunning = true;
            StartCoroutine(PokerHandAd());
        }

        // Deal button is up, the draw and the payout are finished, nothing else is on screen.
        // Shared with the tap (PokerDealGate.AdCanShow). The table is a splash-shell face,
        // so _splash must not count as "not on the table" — that left every 10th-hand ad
        // unready and the DEAL tap discarded the press.
        bool PokerHandAdReady()
        {
            return PokerDealGate.AdCanShow(
                _home == HomeFace.Poker,
                _pokerPageOn,
                Ads.IsBusy || Ads.IsShowing,
                IsTutorialBlocking || TutorialOwnPopup() != PopupKind.None,
                _welcomeOpen,
                BirdPoker.PhaseNow == BirdPoker.Phase.Dealt,
                PokerMotionBusy(),
                _pokerStamp,
                _pokerPayOpen || _pokerPayAnim > 0.2f,
                _pokerWinT >= 0f && _pokerWinT < WinDoneAt);
        }

        IEnumerator PokerHandAd()
        {
            yield return null;
            if (!PokerHandAdReady() || !Ads.PokerAdDue)
            {
                _pokerAdRunning = false;
                yield break;
            }
            yield return Ads.PokerInterstitial();
            _pokerAdRunning = false;
        }

        // Editor captures and the stamp ceremony own the screen. The tutor waits them out.
        bool PokerPageTutorBlocked()
        {
#if UNITY_EDITOR
            if (_pokerPlayrun || EditorShotLive) return true;
#endif
            if (_pokerStamp || _pokerPayOpen) return true;
            if (_pokerWinT >= 0f && _pokerWinT < WinDoneAt) return true;
            return false;
        }

        void ArmPokerPageTutor()
        {
            if (_pokerPageOn || _pokerPageStep < 0) return;
#if UNITY_EDITOR
            if (_pokerPlayrun || EditorShotLive) return;
#endif
            if (PlayerPrefs.GetInt(CoachPokerPageKey, 0) != 0)
            {
                _pokerPageStep = -1;
                return;
            }
            if (_pokerIntroLive) return;
            _pokerPageOn = true;
            _pokerPageStep = 1;
            _pokerPageAge = 0f;
            _gloveReady = false;
            _coachFade = 0f;
        }

        void AdvancePokerTutor(int step)
        {
            if (_pokerPageStep == step) return;
            bool hold = _pokerPageStep >= 3;
            _pokerPageStep = step;
            _pokerPageAge = 0f;
            if ((step >= 3) != hold)
            {
                _glovePhase = 0f;
                _tapSent = false;
                _gloveVis = false;
                _gloveReady = false;
                _coachFade = 0f;
                _coachLineHeld = false;
            }
        }

        void FinishPokerPageTutor()
        {
            _pokerPageOn = false;
            _pokerPageStep = -1;
            _pokerTutorAimOk = false;
            _gloveVis = false;
            _gloveReady = false;
            PlayerPrefs.SetInt(CoachPokerPageKey, 1);
            PlayerPrefs.Save();
        }

        // True while this lesson owns the glove. Leaving the table does not stamp the flag.
        bool TickPokerPageTutor(float dt)
        {
            if (!_pokerPageOn) return false;
            if (_home != HomeFace.Poker)
            {
                _gloveVis = false;
                return false;
            }
            var phase = BirdPoker.PhaseNow;
            if (phase == BirdPoker.Phase.Drawn)
            {
                FinishPokerPageTutor();
                return true;
            }
            bool hand = phase != BirdPoker.Phase.Idle || PokerMotionBusy() || _pokerMotion == PokerMotion.Deal;
            if (PokerPageTutorBlocked() || hand || _pokerPageStep >= 3 || !_pokerTutorAimOk)
            {
                _gloveVis = false;
                _gloveReady = false;
                _glovePhase = 0f;
                _tapSent = false;
                _pokerPageAge = 0f;
                if (!PokerPageTutorBlocked())
                {
                    if (_pokerPageStep <= 1 && phase != BirdPoker.Phase.Idle)
                        AdvancePokerTutor(2);
                    if (_pokerPageStep == 2 && phase == BirdPoker.Phase.Dealt)
                        AdvancePokerTutor(3);
                    if (_pokerPageStep == 3 && phase == BirdPoker.Phase.Dealt)
                        AdvancePokerTutor(4);
                }
                return true;
            }
            float s = Mathf.Max(Screen.height / 720f, 1f);
            _coachFade = Mathf.Min(1f, _coachFade + dt / 0.30f);
            LessonLine(PokerBetLine);
            CoachGloveAt(_pokerTutorAim, dt, s, float.NaN, false, float.NaN, _pokerTutorTap);
            bool parked = Vector2.Distance(_gloveTip, _gloveRest) > 32f * s;
            if (parked) _pokerPageAge = 0f;
            else _pokerPageAge += dt;
            // Both early steps aim at DEAL. Crossing 1 → 2 does not restart the poke.
            if (_pokerPageStep <= 1 && _pokerPageAge >= TapCycle)
                AdvancePokerTutor(2);
            return true;
        }

        void NotePokerTutorAims(Rect actR, Rect row, float cardW, float gap)
        {
            if (!_pokerPageOn)
            {
                _pokerTutorAimOk = false;
                return;
            }
            if (_pokerPageStep >= 3
                || BirdPoker.PhaseNow != BirdPoker.Phase.Idle
                || _pokerMotion == PokerMotion.Deal)
            {
                _pokerTutorAimOk = false;
                return;
            }
            _pokerTutorAim = DealPlatformAim(actR);
            _pokerTutorTap = actR;
            _pokerTutorAimOk = true;
        }

        // ---- Poker page lesson captions ----
        // Build 59: every poker page line (bet, deal hint, hold, back) is the standard garden
        // caption: StandardCaptionBox width, height and font range, PaintCoachCaption padding
        // and plate. Build 58 measured poker-only plates (58*s-tall bet box, a hold plate that
        // stepped its font down to 14) so the lines read small. Only the seat is poker's: the
        // first band (x = top, y = bottom, GUI px) that holds the standard box wins, else the
        // roomier band; a short band trims the box height only, and PaintCoachCaption then
        // fits the font inside it (never under the garden floor). Set once per step: the
        // first frame seats it, later frames reuse it (a new line or screen size re-seats).
        string _pokerCapFor;
        float _pokerTitleBottom;
        int _pokerCapScreenW;
        int _pokerCapScreenH;
        Rect _pokerCapSeat;
        bool _pokerCapTop;

        // Build 61: each poker step picks its caption anchor here, and only here.
        //   Bet  (deal hint too): between the cards and BET/DEAL, else above the cards (centred).
        //   Hold: the build 60 box, unchanged (same size, so the same font), with the build 61
        //         plate slack; only its seat moves: plate top PokerCapClearGap under the PAY TABLE
        //         tab and the logo. The cards do not move. In the hold step the dealt cards ride
        //         the fanned hand near BET/DEAL (a held card slides up to the flat row), so the
        //         plate clears the fan; on a short screen it may run over the top of the cards.
        //   Back: the full standard box under the dealt row (PokerCaptionTop gap), centred; if
        //         BET/DEAL is close it rides up over the bottom of the (inert) cards, never shrinks.
        // Hold and Back are pinned (static, never moved by the glove); Bet keeps the latch.
        enum PokerCapAnchor { BetBand, UnderPayTab, BelowCards }

        static PokerCapAnchor PokerStepAnchor(string line) =>
            line == PokerHoldLine ? PokerCapAnchor.UnderPayTab
            : line == PokerBackLine ? PokerCapAnchor.BelowCards
            : PokerCapAnchor.BetBand;

        static bool PokerStepPinned(PokerCapAnchor a) => a != PokerCapAnchor.BetBand;

        // Clear air between the PAY TABLE tab / logo and the hold caption plate (x s:
        // SE ~23 px = 12 pt, 15 ~44 px = 15 pt, Pro Max ~48 px = 16 pt).
        const float PokerCapClearGap = 12.5f;
        // Air kept above BET/DEAL under a below-the-cards caption plate.
        const float PokerCapControlsGap = 8f;

        // Page geometry the anchors read, refreshed by DrawPokerPage every pass.
        struct PokerCapGeo
        {
            public Rect Row, Bet, Act, Tab;
            public float TitleBottom, LogoClear;
        }
        PokerCapGeo _pokerCapGeo;

        // Seat (the caption's text box; its plate is CoachPanelRect of it) for a step line.
        // Set once per line and screen size.
        Rect PokerCaptionSeat(string line, float s, out bool topAlign)
        {
            if (_pokerCapFor == line && _pokerCapScreenW == Screen.width && _pokerCapScreenH == Screen.height)
            {
                topAlign = _pokerCapTop;
                return _pokerCapSeat;
            }
            StandardCaptionBox(s, out float w, out float h, out _, out _);
            const float plateY = 12f;      // CoachPanelRect's vertical pad, per side
            var g = _pokerCapGeo;
            float reach = PokerBandAboveCards(s, g.Row, 0f).y;
            float x = (Screen.width - w) * 0.5f;
            float y;
            float boxH;
            topAlign = false;
            switch (PokerStepAnchor(line))
            {
                case PokerCapAnchor.UnderPayTab:
                {
                    // Build 60 box height, exactly as build 60 sized it (its two bands above the
                    // flat row: under the tab, else under the title; the roomier when the first
                    // is short), so the fitted font is build 60's.
                    float ceilA = g.Tab.yMax;
                    if (g.Row.y - ceilA < 60f * s) ceilA = g.TitleBottom;
                    float gapTop = 4f * s;
                    float roomA = reach - ceilA - plateY * 2f - gapTop;
                    float roomB = reach - g.TitleBottom - plateY * 2f - gapTop;
                    float room = (roomA < h && roomB > roomA) ? roomB : roomA;
                    boxH = Mathf.Max(24f, Mathf.Min(h, room));
                    // New seat: the painted plate's top PokerCapClearGap below the tab and logo.
                    float plateH = CaptionPlateHeight(line, s, w, boxH);
                    y = PokerHoldCeiling(g) + plateH * 0.5f - boxH * 0.5f;
                    break;
                }
                case PokerCapAnchor.BelowCards:
                {
                    float top = PokerCaptionTop(s, g.Row.yMax);
                    float bot = Mathf.Min(g.Bet.y, g.Act.y) - PokerCapControlsGap * s;
                    float plateH = StandardPlateHeight(line, s);
                    float plateTop = top;
                    if (plateTop + plateH > bot) plateTop = bot - plateH;  // over the cards, not shrunk
                    boxH = h;
                    y = plateTop + plateH * 0.5f - h * 0.5f;
                    break;
                }
                default:
                {
                    // Bet: build 59 two-band seat (first band that holds the box, else the
                    // roomier), centred in its band.
                    var bandA = PokerBandBelowCards(s, g.Row, g.Bet, g.Act);
                    float ceil = g.Tab.yMax;
                    if (g.Row.y - ceil < 60f * s) ceil = g.TitleBottom;
                    var bandB = PokerBandAboveCards(s, g.Row, ceil);
                    float gapTop = 4f * s;
                    float roomA = bandA.y - bandA.x - plateY * 2f - gapTop;
                    float roomB = bandB.y - bandB.x - plateY * 2f - gapTop;
                    var band = bandA;
                    float room = roomA;
                    if (roomA < h && roomB > roomA)
                    {
                        band = bandB;
                        room = roomB;
                    }
                    boxH = Mathf.Max(24f, Mathf.Min(h, room));
                    y = band.x + gapTop + plateY + Mathf.Max(0f, (room - boxH) * 0.5f);
                    break;
                }
            }
            _pokerCapFor = line;
            _pokerCapScreenW = Screen.width;
            _pokerCapScreenH = Screen.height;
            _pokerCapTop = topAlign;
            _pokerCapSeat = new Rect(x, y, w, boxH);
            return _pokerCapSeat;
        }

        // Top of the hold band: under the PAY TABLE tab and the logo halo, PokerCapClearGap clear.
        static float PokerHoldCeiling(PokerCapGeo g) =>
            Mathf.Max(g.LogoClear, g.Tab.width > 2f ? g.Tab.yMax : 0f) + PokerCapClearGap * Mathf.Max(Screen.height / 720f, 1f);

        // Standard caption for a poker page step; the anchor and pin come from PokerStepAnchor.
        void DrawPokerStandardLine(string line, float s)
        {
            if (CoachResetHolds()) return;
            var r = PokerCaptionSeat(line, s, out bool topAlign);
            StandardCaptionBox(s, out _, out _, out int lo, out int hi);
            if (GuiPaint())
                _coachFade = Mathf.Min(1f, _coachFade + Time.unscaledDeltaTime / 0.30f);
            bool pin = PokerStepPinned(PokerStepAnchor(line));
            var seat = pin ? r : SeatTutorialCaption(line, s, r.y, r.width, r.height, r.y, r.x);
            PaintCoachCaption(line, seat, s, lo, hi, default, false, StandardCaptionSlack, topAlign);
        }

        // Above the cards: from ceilingY down to the top of the card reach (held cards and the
        // hover puff ride ~14% of a card above the row).
        static Vector2 PokerBandAboveCards(float s, Rect row, float ceilingY)
        {
            float reach = row.y - Mathf.Max(row.height * 0.14f, 14f * s) - 2f * s;
            return new Vector2(ceilingY, reach);
        }

        // Below the cards, above the bet / deal controls.
        static Vector2 PokerBandBelowCards(float s, Rect row, Rect betR, Rect actR)
        {
            return new Vector2(PokerCaptionTop(s, row.yMax), Mathf.Min(betR.y, actR.y) - 2f * s);
        }

        void DrawPokerPageTutor(float s, Rect row, Rect betR, Rect actR, float ceilingY, float titleBottom)
        {
            if (!_pokerPageOn || PokerPageTutorBlocked()) return;
            // Bet sentence leaves the moment the first hand is dealt, including
            // the deal animation. It does not come back for the hold step.
            string line = null;
            if (_pokerPageStep >= 3)
                line = PokerHoldLine;
            else if (BirdPoker.PhaseNow == BirdPoker.Phase.Idle
                && _pokerMotion != PokerMotion.Deal
                && PlayerPrefs.GetInt(CoachPokerDealtKey, 0) == 0)
                line = PokerBetLine;
            if (line != null)
                DrawPokerStandardLine(line, s);   // seat from PokerStepAnchor
            DrawTutorOverlay(s);
        }

        // Same sentence as the first-visit bet step, for later visits only.
        // The lesson owns the glove and the caption while it is running.
        bool TickPokerDealHint(float dt)
        {
            if (_pokerPageOn || !_pokerDealHint || _home != HomeFace.Poker)
                return false;
            if (PokerPageTutorBlocked() || PokerMotionBusy() || BirdPoker.PhaseNow == BirdPoker.Phase.Dealt || !_pokerDealAimOk)
            {
                _gloveVis = false;
                _glovePhase = 0f;
                _tapSent = false;
                return true;
            }
            float handS = Mathf.Max(Screen.height / 720f, 1f);
            _coachFade = Mathf.Min(1f, _coachFade + dt / 0.30f);
            LessonLine(PokerBetLine);
            CoachGloveAt(_pokerDealAim, dt, handS, float.NaN, false, float.NaN, _pokerDealTap);
            return true;
        }

        void DrawPokerDealHint(float s, Rect row, Rect betR, Rect actR)
        {
            if (_pokerPageOn || !_pokerDealHint || PokerPageTutorBlocked() || PokerMotionBusy()
                || BirdPoker.PhaseNow == BirdPoker.Phase.Dealt)
            {
                _pokerDealAimOk = false;
                return;
            }
            var disc = FlowerDisc(actR, 0f);
            _pokerDealAim = DealPlatformAim(actR);
            _pokerDealTap = actR;
            _pokerDealAimOk = disc.width > 2f;
            DrawPokerStandardLine(PokerBetLine, s);
            DrawTutorOverlay(s);
        }

        bool PokerBackDone()
        {
            if (_pokerBackKnown) return _pokerBackDone;
            _pokerBackKnown = true;
            _pokerBackDone = PlayerPrefs.GetInt(CoachPokerBackKey, 0) != 0;
            return _pokerBackDone;
        }

        // The back step is live on the poker page (shown after the first hand). Shared by
        // CoachAdvance and LateUpdate so this step keeps its glove cycle and caption fade
        // the same way the bet, deal and hold steps do.
        bool PokerBackHintOn() => _home == HomeFace.Poker && !PokerBackDone();

        // The back step owns the table (shared StepTapGate): the first hand is paid and the
        // draw has landed, so only the back medal may take a tap. DEAL, bet +/-, the cards and
        // the pay table wait until Back. The stamp ceremony, an open pay table and a flying WIN
        // keep their own taps; while they run the step is hidden and DEAL is already busy.
        bool PokerBackGateLive()
        {
            if (!_splash || _home != HomeFace.Poker || PokerBackDone()) return false;
#if UNITY_EDITOR
            if (_pokerPlayrun || EditorShotLive) return false;
#endif
            if (_pokerPageOn || _pokerDealHint) return false;
            if (BirdPoker.PhaseNow != BirdPoker.Phase.Drawn || PokerMotionBusy()) return false;
            if (_pokerStamp || _pokerPayOpen || PokerWinFlying()) return false;
            return true;
        }

        void MarkPokerBackDone()
        {
            if (PokerBackDone()) return;
#if UNITY_EDITOR
            if (_pokerPlayrun || EditorShotLive) return;
#endif
            _pokerBackDone = true;
            _pokerBackKnown = true;
            _pokerBackAimOk = false;
            _gloveVis = false;
            PlayerPrefs.SetInt(CoachPokerBackKey, 1);
            PlayerPrefs.Save();
            NoteLessonDismiss(CoachStep.PokerBack);
        }

        // After the result is up. Hidden through the next deal; dismissed on Back.
        bool TickPokerBackHint(float dt)
        {
            if (PokerBackDone() || _home != HomeFace.Poker) return false;
            if (_pokerPageOn || _pokerDealHint) return false;
            if (BirdPoker.PhaseNow != BirdPoker.Phase.Drawn || PokerMotionBusy()
                || PokerPageTutorBlocked() || !_pokerBackAimOk)
            {
                _gloveVis = false;
                _gloveReady = false;
                _glovePhase = 0f;
                _tapSent = false;
                return true;
            }
            float handS = Mathf.Max(Screen.height / 720f, 1f);
            _coachFade = Mathf.Min(1f, _coachFade + dt / 0.30f);
            LessonLine(PokerBackLine);
            // Standard shared arc: the hand rides over the top and the fingertip comes straight
            // down onto the back button. Only a tap inside the back rect is a valid tap.
            CoachGloveAt(_pokerBackAim, dt, handS, float.NaN, false, float.NaN, _pokerBackTap);
            return true;
        }

        void DrawPokerBackHint(float s, Rect back, Rect row, float logoBottom)
        {
            if (PokerBackDone() || _pokerPageOn || _pokerDealHint) return;
            if (BirdPoker.PhaseNow != BirdPoker.Phase.Drawn || PokerMotionBusy() || PokerPageTutorBlocked())
            {
                _pokerBackAimOk = false;
                return;
            }
            _pokerBackAim = back.center;
            _pokerBackTap = back;
            _pokerBackAimOk = back.width > 2f;
            // Standard caption under the dealt row (PokerStepAnchor: BelowCards), clear of the
            // PAY TABLE tab, the logo and BET/DEAL; the glove still points up at Back.
            DrawPokerStandardLine(PokerBackLine, s);
            DrawTutorOverlay(s);
        }

        void DrawPokerPage()
        {
            float s = Mathf.Max(Screen.height / 720f, 1f);
            DrawHomeWash(0.28f);
            BirdPoker.Boot();
            ArmPokerPageTutor();

            var safe = Screen.safeArea;
            float top = TopHud();
            var back = BackMedalRect(s, top);
            if (DrawBackMedal(back))
            {
                if (BirdPoker.PhaseNow == BirdPoker.Phase.Drawn || _pokerShowPay)
                    MarkPokerBackDone();
                BirdPoker.ResetRound();
                ClearPokerHeldPay();
                _pokerMotion = PokerMotion.None;
                _pokerFan = false;
                _pokerChained = false;
                _pokerShowPay = false;
                _pokerWinT = -1f;
                _pokerDash = 0f;
                _pokerStamp = false;
                _pokerPayOpen = false;
                _pokerPayAnim = 0f;
                _pokerKeepHint = false;
                _pokerDealHint = false;
                _pokerDealAimOk = false;
                _pokerHover = -1;
                _home = HomeFace.Splash;
            }
            DrawPokerPurse(back, top, s);

            float logoTop = top + back.height + 4f * s;
            float logoH = PokerTitleHeight(s);
            float markW = Mathf.Min(Screen.width * 0.50f, 240f * s);
            float haloW = Mathf.Min(Screen.width * 0.82f, markW * 1.28f);
            var halo = new Rect(
                (Screen.width - haloW) * 0.5f,
                Mathf.Max(4f, logoTop - 8f * s),
                haloW,
                logoH + 14f * s);
            EnsureHaloFlutters(ref _pokerFlutters, halo);
            DrawHaloBirds(ref _pokerFlutters, halo, s, true, HaloBirdIcon(s), ref _pokerHaloTick);
            DrawPokerTitleMark(logoTop, s);
            DrawHaloBirds(ref _pokerFlutters, halo, s, false, HaloBirdIcon(s), ref _pokerHaloTick);
            float below = logoTop + logoH;

            // Hit-test pay-table tab / dismiss early so overlay blocks cards & Deal.
            if (!_pokerStamp)
                TickPokerPayTable(below, s);

            float gap = 2f * s;
            PokerRowSpan(out float spanX, out float spanW);
            float side = 12f * s;
            float maxRow = Mathf.Max(8f, spanW - side * 2f);
            float cardW = Mathf.Min(Screen.width * 0.205f, 138f * s);
            cardW = Mathf.Min(cardW, (maxRow - (BirdPoker.HandSize - 1) * gap) / BirdPoker.HandSize);
            float cardH = cardW * 1.42f;
            float holdH = 32f * s;
            float rowW = BirdPoker.HandSize * cardW + (BirdPoker.HandSize - 1) * gap;
            float rowX = spanX + (spanW - rowW) * 0.5f;
            float btnSize = Mathf.Min(Screen.width * 0.46f, 200f * s);
            float botPad = Mathf.Max(12f, safe.yMin + 6f);
            float btnY = Screen.height - botPad - btnSize;
            var payTab = PokerPayTabRect(below, s);
            // Equal air between the wordmark and the bet/deal controls. Drop below the PAY TABLE chip when that still fits.
            float bandTop = below;
            float bandBot = btnY;
            float rowY = bandTop + Mathf.Max(0f, bandBot - bandTop - cardH) * 0.5f - PokerRowLiftPx();
            float clearTab = payTab.yMax + 8f * s;
            if (rowY < clearTab && clearTab + cardH <= bandBot - 8f * s)
                rowY = clearTab;
            float maxY = bandBot - cardH - 8f * s;
            if (rowY > maxY && maxY >= bandTop)
                rowY = maxY;
            if (rowY < bandTop) rowY = bandTop;
            if (GuiPaint())
            {
                TickPokerWarm();
                TickPokerKick();
                TickPokerMotion();
                TickHoldSlide();
                TickPokerSway();
                MaybePokerHandAd();
            }
            var rowBox = new Rect(rowX, rowY, rowW, cardH);
            _pokerRowBottom = rowBox.yMax;
            bool payBlockedEarly = _pokerPayOpen || _pokerPayAnim > 0.35f;
            bool canHold = !PokerMotionBusy() && !payBlockedEarly && BirdPoker.PhaseNow == BirdPoker.Phase.Dealt;
            bool hideCards = _pokerPayAnim > 0.18f;
            HitPokerCards(canHold, rowBox, rowX, rowY, cardW, cardH, gap);
            if (!hideCards)
            {
                // LOCK: garden → rest-of-hand → ALL fused cards → thumb only.
                DrawPokerFanHand(rowBox, cardW, cardH, s, false);
                DrawPokerDealHand(rowBox, cardW, gap, s, false);
                int[] order = PokerDrawOrder();
                for (int o = 0; o < order.Length; o++)
                {
                    int i = order[o];
                    if (PokerCardLifted(i)) continue;
                    var seat = PokerSeat(rowBox, cardW, gap, i);
                    DrawPokerCard(seat, i, s, holdH, rowBox);
                }
                for (int o = 0; o < order.Length; o++)
                {
                    int i = order[o];
                    if (!PokerCardLifted(i)) continue;
                    var seat = PokerSeat(rowBox, cardW, gap, i);
                    DrawPokerCard(seat, i, s, holdH, rowBox);
                }
                DrawPokerKeepHint(rowBox, s);
                DrawPokerFlames(rowBox, cardW, gap, s);
                DrawPokerDealHand(rowBox, cardW, gap, s, true);
                // Thumb last — pinching agent stays on top of every live fan card.
                DrawPokerFanHand(rowBox, cardW, cardH, s, true);
            }

            float actX = Screen.width - Mathf.Max(10f, Screen.width - safe.xMax + 8f) - btnSize;
            var actR = new Rect(actX, btnY, btnSize, btnSize);
            float clusterL = Mathf.Max(12f, safe.xMin + 10f);
            var betR = new Rect(clusterL, btnY, Mathf.Max(80f, actR.x - 10f * s - clusterL), btnSize);

            bool payBlocked = _pokerPayOpen || _pokerPayAnim > 0.35f;
            if (GuiPaint()) TickPokerWin();
            bool winFlying = PokerWinFlying();
            bool busy = PokerMotionBusy() || _pokerStamp || payBlocked || winFlying;
            // A settled Drawn hand is not a deal yet (CanDeal is false). Settle it
            // back to Idle once nothing is playing and the back lesson is not holding
            // the result, so DEAL appears only when a deal can actually start.
            // The result cards stay face-up until the next deal's own motion.
            if (BirdPoker.PhaseNow == BirdPoker.Phase.Drawn && !busy && !PokerBackGateLive())
                BirdPoker.Settle();
            if (GuiPaint()) TickPokerDash(s);
            TickPokerStamp();
            if (winFlying && !_pokerStamp && !payBlocked) SkipPokerWinOnTap();
            string act = PokerActLabel();
            // Between hands (idle or showing a result) the bet steers the next DEAL.
            bool steppers = BirdPoker.BetOpen && !PokerMotionBusy();
            if (steppers) BirdPoker.SyncBet();
            // Always paint the bet bar and the win banner. The floral control
            // itself stays out until ActLabel has a word (DEAL or DRAW).
            if (DrawPokerDash(betR, actR, s, busy, steppers, act))
                OnPokerDealPressed();
            DrawPokerBrokeHint(betR, actR, s);
            NotePokerTutorAims(actR, rowBox, cardW, gap);
            float holdCeil = payTab.yMax;
            if (rowBox.y - holdCeil < 60f * s) holdCeil = below;
            _pokerTitleBottom = below;
            _pokerCapGeo = new PokerCapGeo
            {
                Row = rowBox,
                Bet = betR,
                Act = actR,
                Tab = payTab,
                TitleBottom = below,
                LogoClear = Mathf.Max(below, halo.yMax)
            };
            DrawPokerPageTutor(s, rowBox, betR, actR, holdCeil, below);
            DrawPokerDealHint(s, rowBox, betR, actR);
            DrawPokerBackHint(s, back, rowBox, below);
            DrawPokerWinFanfare(betR, actR, s);

            // Stamp ceremony above everything; else pay-table overlay / jewel tab on top.
            if (_pokerStamp) DrawPokerStampCeremony(s);
            else DrawPokerPayTable(below, s);
        }


        Rect PokerPayTabRect(float top, float s)
        {
            float tabW = Mathf.Clamp(211f * s, 178f, 264f);
            float tabH = Mathf.Clamp(62f * s, 55f, 78f);
            float right = Screen.width - Mathf.Max(6f, Screen.width - Screen.safeArea.xMax + 4f);
            float tabY = top + 4f * s;
            return new Rect(right - tabW, tabY, tabW, tabH);
        }

        void TickPokerPayTable(float top, float s)
        {
            if (GuiPaint())
            {
                float target = _pokerPayOpen ? 1f : 0f;
                _pokerPayAnim = Mathf.MoveTowards(_pokerPayAnim, target, Time.unscaledDeltaTime * 7f);
            }

            var tab = PokerPayTabRect(top, s);
            if (HitPad(tab, out bool tabHeld))
            {
                _pokerPayOpen = !_pokerPayOpen;
                Sfx.Chirp(BirdColor.Gold);
            }
            else if (_pokerPayOpen && _pokerPayAnim > 0.55f)
            {
                // Full-screen dismiss (tab already handled above).
                var outside = new Rect(0f, 0f, Screen.width, Screen.height);
                if (HitPad(outside, out _))
                {
                    _pokerPayOpen = false;
                    Sfx.Chirp(BirdColor.Gold);
                }
            }
            _pokerPayJewelHeld = tabHeld;
        }

        void DrawPokerPayTable(float top, float s)
        {
            if (!GuiPaint()) return;
            if (_pokerPayAnim <= 0.01f && !_pokerPayOpen)
            {
                DrawPokerPayJewel(PokerPayTabRect(top, s), s);
                return;
            }

            float u = _pokerPayAnim;
            if (u > 0.02f)
            {
                GUI.color = new Color(0.04f, 0.03f, 0.02f, 0.55f * u);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            var tab = PokerPayTabRect(top, s);
            if (u > 0.01f)
            {
                float panelW = Screen.width * 0.94f;
                int rows = BirdPoker.PayTableRows.Length;
                var safe = Screen.safeArea;
                float btnSize = Mathf.Min(Screen.width * 0.46f, 200f * s);
                float botPad = Mathf.Max(12f, safe.yMin + 6f);
                float btnY = Screen.height - botPad - btnSize;
                float panelTop = tab.yMax + 8f * s;
                float floor = btnY - 12f * s;
                float avail = Mathf.Max(320f, floor - panelTop);
                float headH = Mathf.Clamp(avail * 0.12f, 52f * s, 78f * s);
                float pad = 14f * s;
                float rowH = (avail - headH - pad * 2f) / rows;
                rowH = Mathf.Max(40f * s, rowH);
                float panelH = headH + rows * rowH + pad * 2f;
                if (panelTop + panelH > floor)
                {
                    panelH = floor - panelTop;
                    rowH = Mathf.Max(28f * s, (panelH - headH - pad * 2f) / rows);
                }
                float cx = Screen.width * 0.5f;
                float ease = 1f - Mathf.Pow(1f - u, 2.4f);
                float y = Mathf.Lerp(panelTop - 24f * s, panelTop, ease);
                var panel = new Rect(cx - panelW * 0.5f, y, panelW, panelH * Mathf.Lerp(0.92f, 1f, ease));

                GUI.color = new Color(0.10f, 0.07f, 0.04f, 0.55f * u);
                GUI.DrawTexture(new Rect(panel.x + 4f, panel.y + 6f, panel.width, panel.height), Texture2D.whiteTexture);
                GUI.color = new Color(0.42f, 0.28f, 0.12f, 0.98f * u);
                GUI.DrawTexture(panel, Texture2D.whiteTexture);
                GUI.color = new Color(0.78f, 0.58f, 0.22f, 0.95f * u);
                GUI.DrawTexture(new Rect(panel.x + 3f * s, panel.y + 3f * s, panel.width - 6f * s, panel.height - 6f * s), Texture2D.whiteTexture);
                var paper = new Rect(panel.x + 7f * s, panel.y + 7f * s, panel.width - 14f * s, panel.height - 14f * s);
                DrawCardPaper(paper, false, 0.98f * u);
                GUI.color = Color.white;

                var head = PokerBold(ref _payHead, TextAnchor.MiddleCenter);
                head.clipping = TextClipping.Clip;
                var sub = PokerBold(ref _paySub, TextAnchor.MiddleCenter);
                sub.clipping = TextClipping.Clip;
                string headTx = "PAY TABLE";
                int fitKey = BirdPoker.Bet
                    ^ (Mathf.RoundToInt(paper.width) * 397)
                    ^ (Mathf.RoundToInt(rowH * 10f) * 8191)
                    ^ (Mathf.RoundToInt(headH) << 20)
                    ^ (rows << 28);
                if (fitKey == int.MinValue) fitKey = -2;
                bool fitMiss = fitKey != _payFitKey || rows > _payCashTx.Length;
                if (fitMiss && rows <= _payCashTx.Length) _payFitKey = fitKey;
                if (fitMiss)
                {
                    head.fontSize = FitFont(head, headTx, paper.width * 0.80f, headH * 0.52f, 24, 44);
                    sub.fontSize = Mathf.Max(16, head.fontSize - 6);
                }
                StampLight(new Rect(paper.x, paper.y + 2f * s, paper.width, headH * 0.58f), headTx, head,
                    new Color(0.28f, 0.14f, 0.06f));
                StampLight(new Rect(paper.x, paper.y + headH * 0.52f, paper.width, headH * 0.40f),
                    PokerBetText(), sub, new Color(0.62f, 0.28f, 0.08f));
                GUI.color = new Color(0.72f, 0.52f, 0.22f, 0.85f * u);
                float ruleY = paper.y + headH - 2f * s;
                GUI.DrawTexture(new Rect(paper.x + 10f * s, ruleY, paper.width - 20f * s, 2f * s), Texture2D.whiteTexture);
                GUI.color = Color.white;

                var nameSt = PokerBold(ref _payName, TextAnchor.MiddleLeft);
                nameSt.clipping = TextClipping.Clip;
                var numSt = PokerBold(ref _payNum, TextAnchor.MiddleRight);
                numSt.clipping = TextClipping.Clip;
                var midSt = PokerBold(ref _payMid, TextAnchor.MiddleCenter);
                midSt.clipping = TextClipping.Clip;
                int body = Mathf.RoundToInt(Mathf.Clamp(rowH * 0.46f, 22f, 40f));
                int jack = body + 4;
                float yRow = paper.y + headH + 2f * s;
                for (int i = 0; i < rows; i++)
                {
                    var rank = BirdPoker.PayTableRows[i];
                    int mult = BirdPoker.Multiplier(rank);
                    long payout = (long)mult * BirdPoker.Bet;
                    string name = BirdPoker.RankLabel(rank);
                    float barInset = 8f * s;
                    var row = new Rect(paper.x + barInset, yRow, paper.width - barInset * 2f, rowH);

                    // Gold bar (and the bigger cap / warmer ink) on the top rank only (FIVE WILDS);
                    // NATURAL FIVE and the rest take the plain alternating rows.
                    bool jackpot = PayRowGold(rank);
                    if (jackpot)
                    {
                        GUI.color = new Color(1f, 0.84f, 0.32f, 0.38f * u);
                        GUI.DrawTexture(row, Texture2D.whiteTexture);
                    }
                    else if ((i & 1) == 1)
                    {
                        GUI.color = new Color(0.62f, 0.48f, 0.28f, 0.16f * u);
                        GUI.DrawTexture(row, Texture2D.whiteTexture);
                    }
                    GUI.color = Color.white;

                    var fill = jackpot
                        ? new Color(0.52f, 0.26f, 0.06f)
                        : new Color(0.30f, 0.15f, 0.06f);
                    float textPad = row.width * 0.06f;
                    var inner = new Rect(row.x + textPad, row.y, row.width - textPad * 2f, row.height);
                    float nameW = inner.width * 0.50f;
                    float multW = inner.width * 0.18f;
                    float cashW = inner.width - nameW - multW;
                    int cap = jackpot ? jack : body;
                    int floorPx = 11;
                    string multTx;
                    string cashTx;
                    if (fitMiss || i >= _payCashTx.Length)
                    {
                        nameSt.fontSize = FitFont(nameSt, name, nameW - 6f, row.height * 0.62f, floorPx, cap);
                        multTx = mult + "×";
                        cashTx = Money.Format(payout);
                        midSt.fontSize = FitFont(midSt, multTx, multW - 4f, row.height * 0.62f, floorPx, cap);
                        numSt.fontSize = FitFont(numSt, cashTx, cashW - 6f, row.height * 0.62f, floorPx, cap);
                        if (i < _payCashTx.Length)
                        {
                            _payNamePx[i] = nameSt.fontSize;
                            _payMidPx[i] = midSt.fontSize;
                            _payCashPx[i] = numSt.fontSize;
                            _payMultTx[i] = multTx;
                            _payCashTx[i] = cashTx;
                        }
                    }
                    else
                    {
                        nameSt.fontSize = _payNamePx[i];
                        midSt.fontSize = _payMidPx[i];
                        numSt.fontSize = _payCashPx[i];
                        multTx = _payMultTx[i];
                        cashTx = _payCashTx[i];
                    }
                    StampLight(new Rect(inner.x, inner.y, nameW, inner.height), name, nameSt, fill);
                    StampLight(new Rect(inner.x + nameW, inner.y, multW, inner.height), multTx, midSt, fill);
                    StampLight(new Rect(inner.x + nameW + multW, inner.y, cashW, inner.height), cashTx, numSt, fill);
                    yRow += rowH;
                }
            }

            DrawPokerPayJewel(tab, s);
        }

        void DrawPokerPayJewel(Rect tab, float s)
        {
            if (!GuiPaint()) return;
            // Input System only — do not call UnityEngine.Input.GetMouseButton.
            bool held = _pokerPayJewelHeld;

            float sink = held ? 2f : 0f;
            var chip = new Rect(tab.x, tab.y + sink, tab.width, tab.height - sink);
            GUI.color = new Color(0.10f, 0.06f, 0.03f, 0.45f);
            GUI.DrawTexture(new Rect(chip.x + 2f, chip.y + 3f, chip.width, chip.height), Texture2D.whiteTexture);
            GUI.color = new Color(0.78f, 0.58f, 0.18f, held ? 0.98f : 0.92f);
            GUI.DrawTexture(chip, Texture2D.whiteTexture);
            GUI.color = new Color(0.62f, 0.12f, 0.16f, 0.96f);
            GUI.DrawTexture(new Rect(chip.x + 3f * s, chip.y + 3f * s, chip.width - 6f * s, chip.height - 6f * s), Texture2D.whiteTexture);
            GUI.color = new Color(0.88f, 0.28f, 0.32f, 0.55f);
            GUI.DrawTexture(new Rect(chip.x + 5f * s, chip.y + 4f * s, chip.width - 10f * s, chip.height * 0.38f), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 0.92f, 0.55f, 0.55f);
            GUI.DrawTexture(new Rect(chip.x + 4f * s, chip.y + 3f * s, chip.width - 8f * s, 3f * s), Texture2D.whiteTexture);
            GUI.color = Color.white;

            string lab = _pokerPayOpen ? "PAY TABLE ▼" : "PAY TABLE ▶";
            if (_pokerPayStyle == null)
            {
                _pokerPayStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = false,
                    clipping = TextClipping.Clip
                };
            }
            var labSt = _pokerPayStyle;
            int jewelKey = (_pokerPayOpen ? 1 : 0)
                ^ (Mathf.RoundToInt(chip.width) * 17)
                ^ (Mathf.RoundToInt(chip.height) << 12);
            if (jewelKey == int.MinValue) jewelKey = -2;
            if (jewelKey != _payJewelKey)
            {
                _payJewelKey = jewelKey;
                _payJewelPx = FitFont(labSt, lab, chip.width * 0.90f, chip.height * 0.72f, 14, 26);
            }
            labSt.fontSize = _payJewelPx;
            StampLight(chip, lab, labSt, new Color(1f, 0.94f, 0.72f));
        }

        void BeginPokerStamp(int kind)
        {
            _pokerPayOpen = false;
            _pokerPayAnim = 0f;
            _pokerStamp = true;
            _pokerStampT = 0f;
            _pokerStampKind = kind;
            _pokerBingo = BirdPoker.LastPunchBingo;
            if (CamShake.Live != null) CamShake.Live.Punch(_pokerBingo ? 0.38f : 0.22f, _pokerBingo ? 0.18f : 0.12f, 2.4f, 0.10f);
            Sfx.Rumble();
            if (_pokerBingo) Sfx.Combo(4);
        }

        void TickPokerStamp()
        {
            if (!_pokerStamp) return;
            if (GuiPaint())
                _pokerStampT += Time.unscaledDeltaTime;
            // Seal hits the clipboard — shake the IMGUI overlay and the garden.
            if (_pokerStampT >= StampHitAt && _pokerStampT - Time.unscaledDeltaTime < StampHitAt)
            {
                PunchPoker(_pokerBingo ? 0.34f : 0.26f, _pokerBingo ? 28f : 20f, _pokerBingo ? 12f : 8f);
                Sfx.Rumble();
                Sfx.Crack();
                if (_pokerBingo) Sfx.Combo(5);
            }
            // The FULLCARD prize rolls onto the purse with the bingo cascade / FULLCARD line.
            if (_pokerBingo && _pokerStampT >= StampBingoAt) RevealPokerPay(true);
            bool tap = Event.current != null && Event.current.type == EventType.MouseDown;
            float hold = _pokerBingo ? 5.2f : 2.6f;
            float tapAt = _pokerBingo ? 2.4f : 1.35f;
            if (_pokerStampT >= hold || (_pokerStampT >= tapAt && tap))
            {
                _pokerStamp = false;
                _pokerBingo = false;
                RevealPokerPay(true);
            }
        }

        // Resting wax-seal diameter in punch cells. The art is round (aspect 1) and sits centred
        // on its cell, just inside it so neighbours stay clear; only the impact squash briefly
        // spills into the gap.
        const float PunchSealCells = 0.94f;
        // Extra size at full lift (1 = high above the card, 0 = pressed on it).
        const float SealLiftGrow = 1.45f;
        // Impact squash-and-settle length.
        const float SealSettle = 0.34f;
        // FULLCARD wave: each seal hops off the card and re-presses.
        const float SealHop = 0.22f;

        // Hand-pressed tilt per spot (-14..-4 deg), so a full card doesn't look machine-printed.
        static float PunchSealRot(int kind) => -14f + (kind * 7 % 11);

        // Wax spreading on the hit: squashes out, dips under, settles back to 1.
        static Vector2 SealSquash(float since)
        {
            if (since < 0f || since >= SealSettle) return Vector2.one;
            float v = since / SealSettle;
            float k = 0.11f * (1f - v) * (1f - v) * Mathf.Cos(v * 3f * Mathf.PI);
            return new Vector2(1f + k, 1f + k * 0.72f);
        }

        // The one draw path for a punch mark: the red wax seal, face-down on the card, with a
        // contact shadow. Inked spots, the falling stamper and the FULLCARD wave all come here.
        // c: seal centre; cell: punch cell size; lift: 0 pressed .. 1 high above the card;
        // squash: impact spread (Vector2.one at rest).
        static void DrawInkStamp(Vector2 c, float cell, float rot, float alpha, float lift, Vector2 squash)
        {
            if (alpha <= 0.02f) return;
            lift = Mathf.Clamp01(lift);
            float d = cell * PunchSealCells * (1f + lift * SealLiftGrow);
            var seal = SpriteCatalog.WaxSeal;
            if (seal == null || seal.texture == null)
            {
                DrawInkRing(new Rect(c.x - d * 0.5f, c.y - d * 0.5f, d, d), rot, alpha, lift < 0.05f);
                return;
            }
            float spread = Mathf.Max(0f, squash.x - 1f);
            float w = d * squash.x;
            float h = d * squash.y;
            float rest = cell * PunchSealCells;
            // Soft cast shadow: drifts away and fades as the seal rises, darkens on the hit.
            var glow = GlowTex();
            var off = new Vector2(0.05f + lift * 0.24f, 0.08f + lift * 0.40f) * rest;
            float shA = alpha * Mathf.Clamp01(0.46f - lift * 0.30f + spread * 2.4f);
            float sh = d * (1.34f + lift * 0.30f);
            GUI.color = new Color(0.10f, 0.03f, 0.02f, shA);
            GUI.DrawTexture(new Rect(c.x + off.x - sh * squash.x * 0.5f, c.y + off.y - sh * squash.y * 0.5f,
                sh * squash.x, sh * squash.y), glow, ScaleMode.StretchToFill, true);
            var prev = GUI.matrix;
            GUIUtility.RotateAroundPivot(rot, c);
            var r = new Rect(c.x - w * 0.5f, c.y - h * 0.5f, w, h);
            // Tight seal-shaped contact shadow, only once it is almost on the paper.
            float tightA = alpha * Mathf.Clamp01(1f - lift * 4f) * (0.42f + spread * 2f);
            if (tightA > 0.02f)
            {
                GUI.color = new Color(0.12f, 0.02f, 0.01f, Mathf.Clamp01(tightA));
                var cr = new Rect(r.x + rest * 0.025f, r.y + rest * 0.045f, w * 1.02f, h * 1.02f);
                GUI.DrawTexture(cr, seal.texture, ScaleMode.StretchToFill, true);
            }
            // The seal itself, a touch darker while it is being pressed in.
            float lum = 1f - spread * 1.1f;
            GUI.color = new Color(lum, lum, lum, alpha);
            GUI.DrawTexture(r, seal.texture, ScaleMode.StretchToFill, true);
            GUI.matrix = prev;
            GUI.color = Color.white;
        }

        // Missing-art fallback only: the old red ink ring + COMPLETED bar.
        static void DrawInkRing(Rect r, float rot, float alpha, bool word)
        {
            if (alpha <= 0.02f) return;
            var ring = SpriteCatalog.StampRing;
            var prev = GUI.matrix;
            GUIUtility.RotateAroundPivot(rot, r.center);
            GUI.color = new Color(1f, 1f, 1f, alpha);
            if (ring != null && ring.texture != null)
                GUI.DrawTexture(r, ring.texture, ScaleMode.ScaleToFit, true);
            else
            {
                GUI.color = new Color(0.82f, 0.08f, 0.10f, alpha);
                GUI.DrawTexture(r, Texture2D.whiteTexture);
            }
            if (word)
            {
                // Ink bar through the diameter, word riding it.
                float barH = Mathf.Max(10f, r.height * 0.22f);
                var bar = new Rect(r.x + r.width * 0.08f, r.center.y - barH * 0.5f, r.width * 0.84f, barH);
                GUI.color = new Color(0.82f, 0.08f, 0.10f, 0.92f * alpha);
                GUI.DrawTexture(bar, Texture2D.whiteTexture);
                GUI.color = Color.white;
                var st = GuiPool.Label(GuiSlot.InkStamp);
                st.fontStyle = FontStyle.Bold;
                st.alignment = TextAnchor.MiddleCenter;
                st.wordWrap = false;
                string lab = "COMPLETED";
                st.fontSize = FitFont(st, lab, bar.width * 0.96f, bar.height * 0.92f, 8, 28);
                StampOutlined(bar, lab, st, new Color(1f, 0.95f, 0.88f, alpha), 1, 1);
            }
            GUI.matrix = prev;
            GUI.color = Color.white;
        }

        static Rect DrawPokerClipboard(Rect board, float s, Texture clipTex)
        {
            GUI.color = new Color(0.04f, 0.02f, 0.01f, 0.42f);
            var shadow = new Rect(board.x + 10f * s, board.y + 14f * s, board.width, board.height);
            if (clipTex != null)
                GUI.DrawTexture(shadow, clipTex, ScaleMode.ScaleToFit, true);
            else
                GUI.DrawTexture(shadow, Texture2D.whiteTexture);
            GUI.color = Color.white;

            if (clipTex != null)
            {
                GUI.DrawTexture(board, clipTex, ScaleMode.ScaleToFit, true);
                // Paper sheet below the steel clip jaw, inside the painted board.
                return new Rect(
                    board.x + board.width * 0.18f,
                    board.y + board.height * 0.295f,
                    board.width * 0.64f,
                    board.height * 0.57f);
            }

            GUI.color = new Color(0.22f, 0.12f, 0.05f, 1f);
            GUI.DrawTexture(board, Texture2D.whiteTexture);
            var body = new Rect(board.x + 4f * s, board.y + 4f * s, board.width - 8f * s, board.height - 8f * s);
            GUI.color = new Color(0.50f, 0.32f, 0.14f, 1f);
            GUI.DrawTexture(body, Texture2D.whiteTexture);
            GUI.color = new Color(0.72f, 0.52f, 0.26f, 0.55f);
            GUI.DrawTexture(new Rect(body.x + 3f * s, body.y + 3f * s, body.width - 6f * s, 5f * s), Texture2D.whiteTexture);
            GUI.color = new Color(0.28f, 0.16f, 0.06f, 0.35f);
            GUI.DrawTexture(new Rect(body.x + 3f * s, body.yMax - 6f * s, body.width - 6f * s, 4f * s), Texture2D.whiteTexture);
            for (int i = 0; i < 11; i++)
            {
                float x = body.x + body.width * (0.10f + i * 0.075f);
                GUI.color = new Color(0.28f, 0.14f, 0.05f, 0.16f + (i % 3) * 0.04f);
                GUI.DrawTexture(new Rect(x, body.y + 8f * s, 2.2f * s, body.height - 16f * s), Texture2D.whiteTexture);
            }
            float screw = 7f * s;
            Vector2[] screws =
            {
                new Vector2(body.x + 10f * s, body.y + 10f * s),
                new Vector2(body.xMax - 10f * s - screw, body.y + 10f * s),
                new Vector2(body.x + 10f * s, body.yMax - 10f * s - screw),
                new Vector2(body.xMax - 10f * s - screw, body.yMax - 10f * s - screw)
            };
            for (int i = 0; i < screws.Length; i++)
            {
                GUI.color = new Color(0.72f, 0.55f, 0.22f, 1f);
                GUI.DrawTexture(new Rect(screws[i].x, screws[i].y, screw, screw), Texture2D.whiteTexture);
                GUI.color = new Color(0.38f, 0.26f, 0.08f, 0.85f);
                GUI.DrawTexture(new Rect(screws[i].x + screw * 0.28f, screws[i].y + screw * 0.28f, screw * 0.44f, screw * 0.44f), Texture2D.whiteTexture);
            }

            var paper = new Rect(
                board.x + board.width * 0.11f,
                board.y + board.height * 0.16f,
                board.width * 0.78f,
                board.height * 0.74f);
            GUI.color = new Color(0.22f, 0.14f, 0.08f, 0.28f);
            GUI.DrawTexture(new Rect(paper.x + 3f * s, paper.y + 4f * s, paper.width, paper.height), Texture2D.whiteTexture);
            GUI.color = new Color(0.96f, 0.92f, 0.80f, 1f);
            GUI.DrawTexture(paper, Texture2D.whiteTexture);
            GUI.color = new Color(0.78f, 0.70f, 0.52f, 0.35f);
            int rules = 14;
            for (int i = 1; i < rules; i++)
            {
                float y = paper.y + paper.height * (i / (float)rules);
                GUI.DrawTexture(new Rect(paper.x + 8f * s, y, paper.width - 16f * s, 1.2f * s), Texture2D.whiteTexture);
            }

            float clipW = board.width * 0.30f;
            float clipH = 40f * s;
            var clip = new Rect(board.center.x - clipW * 0.5f, board.y - 8f * s, clipW, clipH);
            GUI.color = new Color(0.32f, 0.32f, 0.34f, 1f);
            GUI.DrawTexture(clip, Texture2D.whiteTexture);
            GUI.color = new Color(0.72f, 0.74f, 0.76f, 1f);
            GUI.DrawTexture(new Rect(clip.x + 3f * s, clip.y + 3f * s, clip.width - 6f * s, clip.height - 8f * s), Texture2D.whiteTexture);
            GUI.color = new Color(0.92f, 0.93f, 0.94f, 0.70f);
            GUI.DrawTexture(new Rect(clip.x + 6f * s, clip.y + 4f * s, clip.width - 12f * s, 5f * s), Texture2D.whiteTexture);
            float hole = 8f * s;
            GUI.color = new Color(0.10f, 0.10f, 0.12f, 1f);
            GUI.DrawTexture(new Rect(clip.center.x - hole * 0.5f, clip.y + 5f * s, hole, hole), Texture2D.whiteTexture);
            GUI.color = Color.white;
            return paper;
        }

        void DrawPokerStampCeremony(float s)
        {
            float t = _pokerStampT;
            var prevGui = GUI.matrix;
            GUI.matrix = Matrix4x4.Translate(new Vector3(_pokerKick.x, _pokerKick.y, 0f)) * GUI.matrix;
            // Dim table
            float veil = Mathf.Clamp01(t / 0.12f) * (_pokerBingo ? 0.58f : 0.72f);
            GUI.color = _pokerBingo
                ? new Color(0.28f, 0.14f, 0.02f, veil)
                : new Color(0.04f, 0.03f, 0.02f, veil);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Clipboard slam onto the table
            var clipSpr = SpriteCatalog.Clipboard;
            var clipTex = clipSpr != null ? clipSpr.texture : null;
            float boardW = Mathf.Min(Screen.width * 0.88f, 520f * s);
            float aspect = clipTex != null
                ? clipTex.height / Mathf.Max(1f, (float)clipTex.width)
                : 1.33f;
            float boardH = boardW * aspect;
            float maxH = Screen.height * 0.78f;
            if (boardH > maxH)
            {
                boardH = maxH;
                boardW = boardH / aspect;
            }
            float slamU = Mathf.Clamp01(t / 0.38f);
            float ease = 1f - Mathf.Pow(1f - slamU, 3f);
            float yOff = Mathf.Lerp(-Screen.height * 0.55f, 0f, ease);
            // Impact squash
            float squash = slamU >= 1f && t < 0.55f
                ? 1f + 0.06f * Mathf.Sin((t - 0.38f) / 0.17f * Mathf.PI)
                : 1f;
            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.48f + yOff;
            var board = new Rect(cx - boardW * 0.5f, cy - boardH * 0.5f * squash, boardW, boardH * squash);
            var paper = DrawPokerClipboard(board, s, clipTex);

            var title = GuiPool.Label(GuiSlot.PokerStampTitle);
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            string boardTitle = _pokerBingo ? "FLOCK COMPLETE" : "FLOCK FIVE PUNCH CARD";
            title.fontSize = FitFont(title, boardTitle, paper.width * 0.92f, 28f * s, 14, 26);
            StampOutlined(new Rect(paper.x, paper.y + 6f * s, paper.width, 26f * s), boardTitle, title,
                _pokerBingo ? new Color(0.55f, 0.28f, 0.06f) : new Color(0.28f, 0.14f, 0.06f), 2, 1);
            if (BirdPoker.FullcardCount > 0 || _pokerBingo)
            {
                var life = GuiPool.From(GuiSlot.PokerStampLife, title);
                life.fontStyle = FontStyle.Bold;
                string lifeTx = GuiPool.Num(GuiSlot.PokerStampLife, "FULLCARDS  ", BirdPoker.FullcardCount);
                life.fontSize = FitFont(life, lifeTx, paper.width * 0.80f, 16f * s, 10, 16);
                StampOutlined(new Rect(paper.x, paper.y + 30f * s, paper.width, 16f * s), lifeTx, life, new Color(0.42f, 0.22f, 0.06f), 1, 1);
            }

            // 4×4 grid: 15 birds + five-wilds.
            int cols = 4;
            float pad = 8f * s;
            float gridTop = paper.y + 48f * s;
            float gridH = paper.height - 78f * s;
            float gap = 5f * s;
            float cell = Mathf.Min((paper.width - pad * 2f - (cols - 1) * gap) / cols, (gridH - 3f * gap) / 4f);
            float gridW = cols * cell + (cols - 1) * gap;
            float x0 = paper.center.x - gridW * 0.5f;
            for (int i = 0; i < BirdPoker.PunchKinds; i++)
            {
                int col = i % cols;
                int row = i / cols;
                var r = new Rect(x0 + col * (cell + gap), gridTop + row * (cell + gap), cell, cell);
                bool on = PokerPunchInked(i, t);
                bool focus = i == _pokerStampKind;
                bool wildCell = BirdPoker.IsWildKind(i);
                GUI.color = focus
                    ? new Color(1f, 0.90f, 0.45f, 0.62f)
                    : (wildCell
                        ? new Color(0.18f, 0.28f, 0.72f, on || focus ? 0.42f : 0.22f)
                        : (on ? new Color(0.96f, 0.92f, 0.78f, 0.38f) : new Color(0.22f, 0.14f, 0.08f, 0.16f)));
                GUI.DrawTexture(r, Texture2D.whiteTexture);
                GUI.color = new Color(0.36f, 0.22f, 0.10f, focus ? 0.70f : 0.28f);
                GUI.DrawTexture(new Rect(r.x, r.y, r.width, 1.5f * s), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(r.x, r.yMax - 1.5f * s, r.width, 1.5f * s), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(r.x, r.y, 1.5f * s, r.height), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(r.xMax - 1.5f * s, r.y, 1.5f * s, r.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
                Sprite spr;
                if (wildCell) spr = SpriteCatalog.Joker;
                else
                {
                    BirdColor c;
                    BirdSex sex;
                    BirdPoker.KindParts(i, out c, out sex);
                    spr = SpriteCatalog.Bird(c, sex);
                }
                if (spr != null && spr.texture != null)
                {
                    float ip = cell * 0.1f;
                    GUI.color = on || focus ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                    GUI.DrawTexture(new Rect(r.x + ip, r.y + ip, cell - ip * 2f, cell - ip * 2f), spr.texture, ScaleMode.ScaleToFit, true);
                    GUI.color = Color.white;
                }
            }
            // Seals ride above every cell (the press squash can spill into the gap).
            for (int i = 0; i < BirdPoker.PunchKinds; i++)
            {
                if (!PokerPunchInked(i, t)) continue;
                var r = new Rect(x0 + (i % cols) * (cell + gap), gridTop + (i / cols) * (cell + gap), cell, cell);
                PokerSealPose(i, t, out float sealLift, out Vector2 sealSquash, out float sealRot);
                DrawInkStamp(r.center, cell, PunchSealRot(i) + sealRot, 1f, sealLift, sealSquash);
            }

            DrawPokerStamper(x0, gridTop, cell, gap, cols, t, s);

            if (_pokerBingo && t >= StampBingoAt)
            {
                // The seals' hop-and-repress wave rides PokerSealPose in the grid's seal pass.
                var glow = GlowTex();
                float burst = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(t * 3.1f));
                GUI.color = new Color(1f, 0.82f, 0.28f, 0.22f * burst);
                float halo = boardW * (1.08f + 0.08f * burst);
                GUI.DrawTexture(new Rect(board.center.x - halo * 0.5f, board.center.y - halo * 0.5f, halo, halo), glow, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
            }

            if (t >= 1.4f)
            {
                var hint = GuiPool.Label(GuiSlot.PokerStampHint);
                hint.fontStyle = FontStyle.Italic;
                hint.alignment = TextAnchor.MiddleCenter;
                hint.fontSize = Mathf.RoundToInt(14 * s);
                string hintTx = _pokerBingo
                    ? "FULLCARD  " + Money.Format(BirdPoker.FullcardPrize) + "   ·   lifetime " + BirdPoker.FullcardCount
                    : "tap to continue";
                StampOutlined(new Rect(0f, board.yMax + 12f * s, Screen.width, 24f * s), hintTx, hint, new Color(1f, 0.92f, 0.7f), 1, 1);
            }
            GUI.matrix = prevGui;
        }

        const float StampDropAt = 0.55f;
        const float StampHitAt = 1.05f;
        const float StampBingoAt = 1.35f;

        // Punch-card ink for a spot. BirdPoker saves the punch at Draw, but the spot being
        // stamped right now stays unmarked until the seal hits (StampHitAt): no early
        // seal while the clipboard slides in.
        bool PokerPunchInked(int kind, float t)
        {
            if (!BirdPoker.IsPunched(kind)) return false;
            return !_pokerStamp || kind != _pokerStampKind || t >= StampHitAt;
        }

        // Pose of an inked seal: the fresh one squashes and settles from its hit; on a FULLCARD
        // every other seal hops off the card and re-presses in a wave.
        void PokerSealPose(int kind, float t, out float lift, out Vector2 squash, out float rotAdd)
        {
            lift = 0f;
            squash = Vector2.one;
            rotAdd = 0f;
            if (!_pokerStamp) return;
            if (kind == _pokerStampKind)
            {
                float since = t - StampHitAt;
                squash = SealSquash(since);
                if (since >= 0f && since < SealSettle)
                {
                    float v = since / SealSettle;
                    rotAdd = 3.5f * (1f - v) * (1f - v) * Mathf.Sin(v * 2.5f * Mathf.PI);
                }
                return;
            }
            if (!_pokerBingo) return;
            float c = t - (StampBingoAt + kind * 0.06f);
            if (c < 0f) return;
            if (c < SealHop)
            {
                float hop = Mathf.Sin(c / SealHop * Mathf.PI);
                lift = 0.22f * hop;
                rotAdd = -6f * hop;
                return;
            }
            squash = SealSquash(c - SealHop);
        }

        // The seal on its way down, face-down out of the camera: big and soft-shadowed high up,
        // hanging a beat, then accelerating into the paper. Pre-impact only: from StampHitAt the
        // seal is the spot's ink and the grid's seal pass picks it up in the same pose (lift 0,
        // same tilt) and squashes it, so the mark lands exactly on the hit.
        void DrawPokerStamper(float x0, float gridTop, float cell, float gap, int cols, float t, float s)
        {
            if (t < StampDropAt || t >= StampHitAt || _pokerStampKind < 0) return;
            int col = _pokerStampKind % cols;
            int row = _pokerStampKind / cols;
            var cellR = new Rect(x0 + col * (cell + gap), gridTop + row * (cell + gap), cell, cell);
            float u = Mathf.Clamp01((t - StampDropAt) / (StampHitAt - StampDropAt));
            float lift = 1f - Mathf.Pow(u, 2.2f);
            var c = cellR.center + new Vector2(cell * 0.90f, -cell * 2.20f) * lift;
            float rot = PunchSealRot(_pokerStampKind) + 24f * lift;
            float a = Mathf.Clamp01(u / 0.20f);
            DrawInkStamp(c, cell, rot, a, lift, Vector2.one);
        }

        // Pay table row style: only the top rank (PayTableRows[0], FIVE WILDS) is gold.
        static bool PayRowGold(BirdPoker.Rank rank) => rank == BirdPoker.PayTableRows[0];

        static GUIStyle _pokerPayStyle, _pokerLed, _pokerStep, _pokerHero, _pokerWinCenter, _pokerWinLeft;
        static GUIStyle _payHead, _paySub, _payName, _payNum, _payMid;
        static GUIContent _pokerGui;
        static string _pokerBetText;
        static int _pokerBetSeen = int.MinValue;
        static string _pokerWinText;
        static int _pokerWinAmt = int.MinValue;
        static int _pokerHeroKey = int.MinValue;
        static int _pokerHeroPx;
        static int _payJewelKey = int.MinValue;
        static int _payJewelPx;
        static int _payFitKey = int.MinValue;
        static readonly int[] _payNamePx = new int[8];
        static readonly int[] _payMidPx = new int[8];
        static readonly int[] _payCashPx = new int[8];
        static readonly string[] _payCashTx = new string[8];
        static readonly string[] _payMultTx = new string[8];

        static GUIStyle PokerBold(ref GUIStyle slot, TextAnchor align)
        {
            if (slot != null) return slot;
            slot = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = align,
                wordWrap = false
            };
            return slot;
        }

        static GUIContent PokerGui(string text)
        {
            if (_pokerGui == null) _pokerGui = new GUIContent();
            _pokerGui.text = text ?? "";
            return _pokerGui;
        }

        static string PokerBetText()
        {
            int bet = BirdPoker.Bet;
            if (bet != _pokerBetSeen || _pokerBetText == null)
            {
                _pokerBetSeen = bet;
                _pokerBetText = "BET " + Money.Format(bet);
            }
            return _pokerBetText;
        }

        // Same balance as home (shared DrawCoinBalance: size, font, icon), seated in the
        // top-right corner on the back-medal row. Build 60 drew it at the medal's height
        // (icon = back.height, type 0.86), bigger than home.
        void DrawPokerPurse(Rect back, float top, float s)
        {
            float right = Screen.width - Mathf.Max(12f, Screen.width - Screen.safeArea.xMax + 10f);
            float cy = Mathf.Max(back.center.y, top + CoinBalanceIcon(s) * 0.5f);
            DrawCoinBalance(s, right, cy);
        }

        const float DealGatherT = 0.32f;
        const float DealRiffleT = 0.56f;
        const float DealFanT = 0.50f;
        const float DealBumpT = 0.24f;
        const float PokerFanScale = 1.42f;
        const float PokerFanSpan = 0.46f;
        // Constructed hand: palm + digits share this UV; thumb is pinch-anchored.
        // Native thumb joint on the same canvas as the palm (image 31).
        const float PokerFanPinchU = 0.586f;
        const float PokerFanPinchV = 0.28f;
        const float PokerDealArmFade = 0.14f; // share of the dealing forearm that fades out
        // fx_hand_thumb.png holds ONLY the thumb (alpha spans U 0.499-0.675, centred on the
        // pinch at U 0.586). The whole canvas is the front layer; never crop it by U, or the
        // right half of the nail and tip are lost and the cards show through there.
        const float PokerFanHandAspect = 0.80f;
        // Raise only the card row (and everything seated on it) by ~5% of screen height.
        const float PokerRowLiftFrac = 0.05f;
        static float PokerRowLiftPx() => Screen.height * PokerRowLiftFrac;

        // Wrist on the hand canvas (where the forearm meets the palm): sway pivot.
        const float PokerFanWristU = 0.33f;
        const float PokerFanWristV = 0.70f;
        // Hand sway: slow layered turn about the wrist + a small drift/bob (fractions of card height).
        const float PokerSwayTurnDeg = 1.00f;
        const float PokerSwayTurnDeg2 = 0.30f;
        const float PokerSwayDriftX = 0.012f;
        const float PokerSwayBobY = 0.018f;
        const float PokerSwayBobY2 = 0.006f;

        // One rigid motion (turn about the wrist, then drift). Palm, every fan card and the
        // thumb all go through Apply + Turn, so the pinch can never slide against the cards.
        struct PokerSway
        {
            public Vector2 Wrist;
            public float Turn;
            public Vector2 Drift;

            public Vector2 Apply(Vector2 p)
            {
                float rad = Turn * Mathf.Deg2Rad;
                float cs = Mathf.Cos(rad);
                float sn = Mathf.Sin(rad);
                var d = p - Wrist;
                return Wrist + new Vector2(cs * d.x - sn * d.y, sn * d.x + cs * d.y) + Drift;
            }
        }

        void TickPokerSway()
        {
            // Frozen through a draw so the plucked cards' captured poses still match the fading hand.
            if (_pokerMotion == PokerMotion.Draw) return;
            _pokerSwayT += Time.unscaledDeltaTime;
        }

        float PokerSwayTurn()
        {
            float t = _pokerSwayT;
            return PokerSwayTurnDeg * Mathf.Sin(t * 0.83f) + PokerSwayTurnDeg2 * Mathf.Sin(t * 1.91f + 1.3f);
        }

        PokerSway PokerSwayNow(Rect row, float bump)
        {
            float t = _pokerSwayT;
            float h = row.height;
            var hand = PokerHandRest(row, bump, out _);
            return new PokerSway
            {
                Wrist = new Vector2(hand.x + hand.width * PokerFanWristU, hand.y + hand.height * PokerFanWristV),
                Turn = PokerSwayTurn(),
                Drift = new Vector2(
                    PokerSwayDriftX * h * Mathf.Sin(t * 0.61f + 2.1f),
                    PokerSwayBobY * h * Mathf.Sin(t * 1.18f + 0.6f) + PokerSwayBobY2 * h * Mathf.Sin(t * 2.47f))
            };
        }

        // One DEAL press. PokerDealGate owns the gates (motion, stamp, pay table,
        // win flight, a due ad, the $5 floor). A settled no-pay hand drops a leftover
        // lock here, then deals. Coins under the bet step down inside SyncBet.
        void OnPokerDealPressed()
        {
            bool payBlocked = _pokerPayOpen || _pokerPayAnim > 0.35f;
            var tap = PokerDealGate.Tap(new PokerDealGate.Snap
            {
                Phase = BirdPoker.PhaseNow,
                MotionBusy = PokerMotionBusy(),
                Stamp = _pokerStamp,
                PayBlocked = payBlocked,
                WinFlying = PokerWinFlying(),
                AdRunning = _pokerAdRunning,
                AdShowing = Ads.IsShowing,
                AdDue = Ads.PokerAdDue,
                AdReady = PokerHandAdReady(),
                Coins = Purse.Coins,
                Ceremony = BirdPoker.LastWin > 0
            });
            if (tap.DropAdLock) _pokerAdRunning = false;
            if (tap.DropWin) _pokerWinT = -1f;
            if (tap.DropStamp)
            {
                _pokerStamp = false;
                _pokerPendingStamp = false;
            }
            switch (tap.Act)
            {
                case PokerDealGate.Act.Ad:
                    MaybePokerHandAd();
                    break;
                case PokerDealGate.Act.Draw:
                    RunPokerDraw();
                    break;
                case PokerDealGate.Act.Deal:
                    if (BirdPoker.PhaseNow == BirdPoker.Phase.Drawn)
                        BirdPoker.Collect();
                    BirdPoker.SyncBet();
                    TryPokerDeal();
                    break;
                case PokerDealGate.Act.Broke:
                    Sfx.Deny();
                    break;
            }
        }

        void DrawPokerBrokeHint(Rect betR, Rect actR, float s)
        {
            if (!PokerDealGate.ShowBrokeHint(Purse.Coins, BirdPoker.PhaseNow)) return;
            int type = Mathf.RoundToInt(Mathf.Clamp(22f * s, 18f, 34f));
            var st = PokerBold(ref _pokerLed, TextAnchor.MiddleCenter);
            st.fontSize = type;
            float h = type * 1.35f;
            float top = Mathf.Min(betR.y, actR.y);
            float y = top - h - 8f * s;
            if (y < 8f) y = 8f;
            StampLight(new Rect(0f, y, Screen.width, h), PokerDealGate.BrokeHint, st,
                new Color(0.94f, 0.12f, 0.14f, 1f));
        }

        void TryPokerDeal()
        {
            FinishPokerWarm();
            if (BirdPoker.PhaseNow != BirdPoker.Phase.Dealt)
                BirdPoker.SyncBet();
            if (!BirdPoker.CanDeal() || !BirdPoker.Deal())
                Sfx.Deny();
            else
            {
                BeginPokerDeal();
                Sfx.CardRustle();
            }
        }

        bool PokerMotionBusy() =>
            _pokerMotion != PokerMotion.None && _pokerMotionT < PokerMotionDur();

        float PokerMotionDur()
        {
            if (_pokerMotion == PokerMotion.Deal)
                return DealGatherT + DealRiffleT + DealFanT + DealBumpT;
            if (_pokerMotion == PokerMotion.Draw)
                return _pokerRowEnd;
            return 0f;
        }

        void TickPokerMotion()
        {
            if (_pokerMotion == PokerMotion.None) return;
            float prev = _pokerMotionT;
            _pokerMotionT += Time.unscaledDeltaTime;
            FirePokerCineSfx(prev, _pokerMotionT);
            if (_pokerMotionT >= PokerMotionDur())
            {
                if (_pokerMotion == PokerMotion.Deal) _pokerFan = true;
                if (_pokerMotion == PokerMotion.Draw)
                {
                    _pokerFan = false;
                    _pokerChained = false;
                    _pokerShowPay = true;
                    BeginPokerWin();
                    if (_pokerPendingStamp)
                    {
                        BeginPokerStamp(BirdPoker.LastPunchKind);
                        _pokerPendingStamp = false;
                    }
                    // A jackpot's fanfare plays with its rank banner (TickPokerWin), not here.
                    if (BirdPoker.LastRank >= BirdPoker.Rank.Flush)
                    {
                        if (!BirdPoker.IsJackpot(BirdPoker.LastRank)) Sfx.Celebrate();
                    }
                    else if (_pokerResultCue == 2) Sfx.Clink();
                    else if (_pokerResultCue == 1) Sfx.Deny();
                    _pokerResultCue = 0;
                }
                _pokerMotion = PokerMotion.None;
            }
        }

        void BeginPokerDeal()
        {
            CoachHideNow();
            _pokerDealHint = false;
            _pokerDealAimOk = false;
            bool stampDealt = PlayerPrefs.GetInt(CoachPokerDealtKey, 0) == 0;
#if UNITY_EDITOR
            if (_pokerPlayrun || EditorShotLive) stampDealt = false;
#endif
            if (stampDealt)
            {
                PlayerPrefs.SetInt(CoachPokerDealtKey, 1);
                PlayerPrefs.Save();
            }
            _pokerMotion = PokerMotion.Deal;
            _pokerMotionT = 0f;
            _pokerKickT = 0f;
            _pokerKick = Vector2.zero;
            _pokerFan = false;
            _pokerChained = false;
            _pokerShowPay = false;
            _pokerWinT = -1f;
            ClearPokerHeldPay();
            _pokerChainBreak = -1f;
            _pokerHover = -1;
            for (int i = 0; i < _pokerRedraw.Length; i++)
            {
                _pokerRedraw[i] = true;
                _pokerHoldSlide[i] = 0f;
                _pokerKept[i] = false;
            }
        }

        // DRAW: the button and the editor debug hand share this, so the reveal, stamp,
        // win fanfare and result sfx all run the same way.
        void RunPokerDraw()
        {
            for (int i = 0; i < BirdPoker.HandSize; i++)
            {
                _pokerRedraw[i] = !BirdPoker.Hold[i];
                _pokerPrev[i] = BirdPoker.Hand[i];
            }
            if (BirdPoker.Draw())
            {
                // Draw already credited + saved these (Purse.CreditHeld); the purse label
                // shows them only at the reveal (RevealPokerPay).
                _pokerHeldWin = BirdPoker.LastWin;
                _pokerHeldFull = BirdPoker.LastFullcardPay;
                Ads.NotePokerHand();
                BeginPokerDraw();
                _pokerPendingStamp = BirdPoker.LastPunchFresh;
                _pokerResultCue = BirdPoker.LastPunchFresh ? 3 : (BirdPoker.LastWin > 0 ? 2 : 1);
            }
        }

#if UNITY_EDITOR
        // Flock Five/Debug/Force Five Wilds (Play Mode): opens poker, deals a real hand,
        // rigs the draw to FIVE WILDS and draws through RunPokerDraw, so the full jackpot
        // celebration plays (reveal, punch stamp if WildKind is unpunched, fanfare, sfx).
        IEnumerator EditorForceFiveWilds()
        {
            if (_home != HomeFace.Poker) BirdPoker.BeginVisit();
            _splash = true;
            _home = HomeFace.Poker;
            _pokerPayOpen = false;
            _pokerPayAnim = 0f;
            BirdPoker.Boot();
            Purse.Boot();
            if (BirdPoker.PhaseNow == BirdPoker.Phase.Drawn) BirdPoker.Collect();
            if (BirdPoker.PhaseNow == BirdPoker.Phase.Idle)
            {
                if (Purse.Coins < BirdPoker.FloorBet * 4) Purse.Credit(BirdPoker.FloorBet * 4 - Purse.Coins);
                BirdPoker.SyncBet();
                yield return null;
                TryPokerDeal();
            }
            float wait = 0f;
            while ((PokerMotionBusy() || BirdPoker.PhaseNow != BirdPoker.Phase.Dealt) && wait < 6f)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }
            if (BirdPoker.PhaseNow != BirdPoker.Phase.Dealt)
            {
                Debug.LogWarning("[poker] Force Five Wilds: no dealt hand (coins " + Purse.Coins + ")");
                yield break;
            }
            yield return new WaitForSecondsRealtime(0.35f);
            if (!BirdPoker.RigFiveWildsDraw())
            {
                Debug.LogWarning("[poker] Force Five Wilds: could not rig the shoe");
                yield break;
            }
            yield return new WaitForSecondsRealtime(0.35f);
            RunPokerDraw();
            Debug.Log("[poker] Force Five Wilds: " + BirdPoker.RankLabel(BirdPoker.LastRank) + " win " + BirdPoker.LastWin);
        }
#endif

        void BeginPokerDraw()
        {
            _pokerMotion = PokerMotion.Draw;
            _pokerMotionT = 0f;
            _pokerKickT = 0f;
            _pokerKick = Vector2.zero;
            _pokerPluckN = 0;
            for (int i = 0; i < BirdPoker.HandSize; i++)
            {
                // _pokerPoseR is the enlarged tap rect for fan cards; starting there made plucked cards jump.
                _pokerDrawFrom[i] = _pokerDrawnR[i].width > 2f ? _pokerDrawnR[i] : _pokerPoseR[i];
                if (!BirdPoker.Hold[i]) continue;
                _pokerPluckIx[_pokerPluckN] = i;
                _pokerPluckN++;
            }
            _pokerChained = false;
            for (int i = 0; i < BirdPoker.HandSize; i++)
            {
                _pokerKept[i] = BirdPoker.Hold[i];
                if (_pokerKept[i]) _pokerChained = true;
            }
            _pokerChainBreak = _pokerChained ? 0f : -1f;
            _pokerShowPay = false;
            int disc = 0;
            for (int i = 0; i < BirdPoker.HandSize; i++)
                if (_pokerRedraw[i]) disc++;
            _pokerPluckEnd = 0.16f;
            _pokerReplaceEnd = _pokerPluckEnd + 0.12f * Mathf.Max(0, disc - 1) + 0.72f;
            _pokerRowEnd = _pokerReplaceEnd + 0.16f * Mathf.Max(1, disc) + 0.40f;
        }

        void TickHoldSlide()
        {
            float sp = Time.unscaledDeltaTime * 4.6f;
            for (int i = 0; i < BirdPoker.HandSize; i++)
            {
                bool keep = BirdPoker.Hold[i] || (_pokerMotion == PokerMotion.Draw && _pokerKept[i]);
                float want = keep ? 1f : 0f;
                _pokerHoldSlide[i] = Mathf.MoveTowards(_pokerHoldSlide[i], want, sp);
            }
        }

        // Table nudge for a stamp or a win landing. Screen shake is opt-in:
        // this helper used to punch the camera on every call, and the deal,
        // the draw, and the card flips all call it. The home camera is the
        // garden behind every splash face (GardenLife since build 69), so
        // those punches shook the screen. Nothing in poker opts in. The
        // badger word slam is BadgerSlam.Shake, not this.
        void PunchPoker(float dur, float amp, float twist, bool shake = false)
        {
            _pokerKickDur = Mathf.Max(0.08f, dur);
            _pokerKickAmp = amp;
            _pokerKickTwist = twist;
            _pokerKickT = _pokerKickDur;
            if (!shake || CamShake.Live == null) return;
            CamShake.Live.Punch(dur, amp * 0.012f, twist * 0.45f, amp * 0.004f);
        }

        void TickPokerKick()
        {
            if (_pokerKickT <= 0f)
            {
                _pokerKick = Vector2.zero;
                return;
            }
            _pokerKickT -= Time.unscaledDeltaTime;
            if (_pokerKickT <= 0f)
            {
                _pokerKick = Vector2.zero;
                return;
            }
            float u = Mathf.Clamp01(_pokerKickT / _pokerKickDur);
            float decay = u * u;
            float w = Time.unscaledTime * 58f;
            _pokerKick = new Vector2(
                Mathf.Sin(w) * _pokerKickAmp * decay + Mathf.Cos(w * 1.31f) * _pokerKickTwist * 0.35f * decay,
                Mathf.Cos(w * 1.19f) * _pokerKickAmp * 0.72f * decay);
        }

        void FirePokerCineSfx(float prev, float now)
        {
            if (_pokerMotion == PokerMotion.Deal)
            {
                if (prev < DealGatherT && now >= DealGatherT)
                    Sfx.Riffle();
                float fanAt = DealGatherT + DealRiffleT;
                if (prev < fanAt && now >= fanAt)
                    Sfx.CardRustle();
                for (int i = 0; i < BirdPoker.HandSize; i++)
                {
                    float slap = fanAt + 0.08f + i * 0.07f;
                    if (prev < slap && now >= slap) Sfx.CardSlap();
                }
                float bumpAt = fanAt + DealFanT;
                if (prev < bumpAt && now >= bumpAt)
                    Sfx.CardBump();
            }
            else if (_pokerMotion == PokerMotion.Draw)
            {
                if (prev < 0.04f && now >= 0.04f && _pokerChainBreak >= 0f)
                    Sfx.ChainSnap();
                int fi = 0;
                for (int i = 0; i < BirdPoker.HandSize; i++)
                {
                    if (!_pokerRedraw[i]) continue;
                    float at = _pokerPluckEnd + fi * 0.16f;
                    if (prev < at + 0.12f && now >= at + 0.12f)
                        Sfx.CardPop();
                    fi++;
                }
                int di = 0;
                for (int i = 0; i < BirdPoker.HandSize; i++)
                {
                    if (!_pokerRedraw[i]) continue;
                    // Slap when the inbound card reaches its seat, not when it leaves the dealer.
                    float at = _pokerReplaceEnd + di * DrawDealStagger + DrawInboundT * DrawInboundHitU;
                    if (prev < at && now >= at) Sfx.CardSlap();
                    di++;
                }
                // Row settles after the replacement cards land: bump sound only. Build 61 dropped
                // the PunchPoker(0.20, 6.8, 1.5) table/camera shake here (not needed).
                if (prev < _pokerRowEnd - 0.08f && now >= _pokerRowEnd - 0.08f)
                    Sfx.CardBump();
            }
        }

        int[] PokerDrawOrder()
        {
            var order = _pokerDrawOrder;
            int n = 0;
            bool fan = _pokerFan || _pokerMotion == PokerMotion.Deal || _pokerMotion == PokerMotion.Draw;
            if (!fan)
            {
                for (int i = 0; i < order.Length; i++) order[i] = i;
                return order;
            }
            // Unheld fan, left→right, hovered last so it sits on top.
            for (int i = 0; i < BirdPoker.HandSize; i++)
            {
                if (PokerCardLifted(i)) continue;
                if (i == _pokerHover) continue;
                order[n++] = i;
            }
            if (_pokerHover >= 0 && _pokerHover < BirdPoker.HandSize && !PokerCardLifted(_pokerHover))
                order[n++] = _pokerHover;
            for (int i = 0; i < BirdPoker.HandSize; i++)
            {
                if (!PokerCardLifted(i)) continue;
                order[n++] = i;
            }
            while (n < order.Length)
            {
                for (int i = 0; i < BirdPoker.HandSize && n < order.Length; i++)
                {
                    bool seen = false;
                    for (int k = 0; k < n; k++)
                        if (order[k] == i) { seen = true; break; }
                    if (!seen) order[n++] = i;
                }
            }
            return order;
        }

        bool PokerCardLifted(int i) =>
            (BirdPoker.Hold[i] && _pokerHoldSlide[i] > 0.22f)
            || (_pokerMotion == PokerMotion.Draw && _pokerKept[i]);

        int PokerFanSlot(int card, out int fanCount)
        {
            fanCount = 0;
            int slot = 0;
            for (int k = 0; k < BirdPoker.HandSize; k++)
            {
                if (BirdPoker.Hold[k]) continue;
                if (k == card) slot = fanCount;
                fanCount++;
            }
            return slot;
        }

        Rect PokerFanSeat(int i, Rect row, float bump)
        {
            int fanCount;
            int slot = PokerFanSlot(i, out fanCount);
            if (BirdPoker.Hold[i] || fanCount <= 0)
            {
                slot = i;
                fanCount = BirdPoker.HandSize;
            }
            return PokerFanSeatAt(slot, Mathf.Max(1, fanCount), row, bump);
        }

        // Fan seat riding the hand: the card's roll pivot (bottom-centre) goes through the
        // shared sway; PokerFanRoll adds the same turn.
        Rect PokerFanSeatAt(int fanIndex, int fanCount, Rect row, float bump)
        {
            var rest = PokerFanRestAt(fanIndex, fanCount, row, bump);
            var foot = PokerSwayNow(row, bump).Apply(new Vector2(rest.center.x, rest.yMax));
            return new Rect(foot.x - rest.width * 0.5f, foot.y - rest.height, rest.width, rest.height);
        }

        // Fan seat with the hand at rest (no sway).
        Rect PokerFanRestAt(int fanIndex, int fanCount, Rect row, float bump)
        {
            float t = fanCount <= 1 ? 0.5f : fanIndex / (float)(fanCount - 1);
            float cardH = row.height * PokerFanScale;
            float cardW = cardH / 1.42f;
            float span = row.width * PokerFanSpan * Mathf.Lerp(0.36f, 1f, (fanCount - 1) / 4f);
            // Shrink the whole fan uniformly if it would clip — never shove ends independently.
            PokerRowSpan(out float spanX, out float spanW);
            float pad = spanW * 0.07f;
            float maxSpan = spanW - pad * 2f - cardW;
            if (maxSpan < 8f) maxSpan = 8f;
            if (span > maxSpan) span = maxSpan;
            float x0 = spanX + (spanW - (span + cardW)) * 0.5f;
            float x = x0 + span * t;
            float mid = 1f - Mathf.Abs(t * 2f - 1f);
            // Idle row is centered under the logo. The dealt fan stays just above the deal disc.
            float sc = Mathf.Max(Screen.height / 720f, 1f);
            float btnSize = Mathf.Min(Screen.width * 0.46f, 200f * sc);
            float botPad = Mathf.Max(12f, Screen.safeArea.yMin + 6f);
            float btnY = Screen.height - botPad - btnSize;
            float y = btnY - 8f * sc - cardH - mid * 12f + bump - PokerRowLiftPx();
            if (y < 0f) y = 0f;
            return new Rect(x, y, cardW, cardH);
        }

        Rect PokerFanCover(int fanCount, Rect row, float bump)
        {
            var a = PokerFanSeatAt(0, fanCount, row, bump);
            var b = PokerFanSeatAt(Mathf.Max(0, fanCount - 1), fanCount, row, bump);
            float x = Mathf.Min(a.x, b.x);
            float y = Mathf.Min(a.y, b.y);
            float x2 = Mathf.Max(a.xMax, b.xMax);
            float y2 = Mathf.Max(a.yMax, b.yMax);
            return new Rect(x - 8f, y - 10f, x2 - x + 16f, y2 - y + 28f);
        }

        float PokerFanRoll(int i)
        {
            int fanCount;
            int slot = PokerFanSlot(i, out fanCount);
            if (BirdPoker.Hold[i] || fanCount <= 0)
                return PokerFanRollAt(i / (float)(BirdPoker.HandSize - 1)) + PokerSwayTurn();
            float t = fanCount <= 1 ? 0.5f : slot / (float)(fanCount - 1);
            float mag = Mathf.Lerp(6f, 16f, (fanCount - 1) / 4f);
            return Mathf.Lerp(-mag, mag, t) + PokerSwayTurn();
        }

        float PokerFanRollAt(float t) => Mathf.Lerp(-16f, 16f, t);

        Rect PokerStackSeat(Rect row)
        {
            float w = row.height / 1.42f;
            return new Rect(row.center.x - w * 0.5f, row.y + 18f, w, row.height);
        }

        void PokerPose(int i, Rect classic, Rect row, out Rect r, out float yaw, out float roll, out bool showFace, out BirdPoker.Card face, out bool sparkle)
        {
            r = classic;
            yaw = 0f;
            roll = 0f;
            face = BirdPoker.Hand[i];
            // Idle hides faces, except a settled result left on the table. The next
            // deal's own motion takes over the moment it starts (gather plays from
            // these seats, face-down).
            showFace = BirdPoker.PhaseNow != BirdPoker.Phase.Idle
                || (BirdPoker.ResultOnTable && _pokerMotion != PokerMotion.Deal);
            sparkle = false;
            var stack = PokerStackSeat(row);

            if (_pokerMotion == PokerMotion.Deal)
            {
                float t = _pokerMotionT;
                if (t < DealGatherT)
                {
                    float u = Mathf.Clamp01(t / DealGatherT);
                    u = u * u * (3f - 2f * u);
                    r = LerpRect(classic, stack, u);
                    showFace = false;
                }
                else if (t < DealGatherT + DealRiffleT)
                {
                    float u = Mathf.Clamp01((t - DealGatherT) / DealRiffleT);
                    int pkt = i & 1;
                    float split = (pkt == 0 ? -1f : 1f) * classic.width * 0.62f;
                    var splitR = new Rect(stack.x + split, stack.y - 6f, stack.width, stack.height);
                    if (u < 0.30f)
                    {
                        float s = u / 0.30f;
                        r = LerpRect(stack, splitR, s * s * (3f - 2f * s));
                    }
                    else
                    {
                        float d = (u - 0.30f) / 0.70f;
                        float drop = Mathf.Sin(d * Mathf.PI) * 36f * (1f - d);
                        float jitter = Mathf.Sin((d * 9f + i) * 3.1f) * 5f * (1f - d);
                        r = new Rect(Mathf.Lerp(splitR.x, stack.x, d) + jitter, stack.y - drop, stack.width, stack.height);
                        roll = Mathf.Sin(d * Mathf.PI * 3f) * (pkt == 0 ? -10f : 10f);
                    }
                    showFace = false;
                }
                else if (t < DealGatherT + DealRiffleT + DealFanT)
                {
                    float u = Mathf.Clamp01((t - DealGatherT - DealRiffleT) / DealFanT);
                    u = u * u * (3f - 2f * u);
                    var fan = PokerFanSeat(i, row, 0f);
                    r = LerpRect(stack, fan, u);
                    yaw = u * 180f;
                    showFace = u >= 0.5f;
                    roll = Mathf.Lerp(0f, PokerFanRoll(i), u);
                    face = BirdPoker.Hand[i];
                }
                else
                {
                    float u = Mathf.Clamp01((t - DealGatherT - DealRiffleT - DealFanT) / DealBumpT);
                    float bump = 18f * Mathf.Sin(Mathf.Clamp01(u / 0.55f) * Mathf.PI)
                        - 6f * Mathf.Sin(Mathf.Clamp01((u - 0.45f) / 0.55f) * Mathf.PI);
                    r = PokerFanSeat(i, row, bump);
                    roll = PokerFanRoll(i);
                    showFace = true;
                    yaw = 0f;
                }
            }
            else if (_pokerMotion == PokerMotion.Draw)
            {
                bool keep = !_pokerRedraw[i];
                var from = _pokerDrawFrom[i].width > 2f ? _pokerDrawFrom[i] : PokerFanSeat(i, row, 0f);
                int discSlot = 0;
                for (int k = 0; k < i; k++)
                    if (_pokerRedraw[k]) discSlot++;
                if (keep)
                {
                    r = classic;
                    roll = 0f;
                    showFace = true;
                    face = BirdPoker.Hand[i];
                }
                else if (_pokerMotionT < _pokerPluckEnd)
                {
                    r = from;
                    roll = PokerFanRoll(i);
                    showFace = true;
                    face = _pokerPrev[i];
                }
                else if (_pokerMotionT < _pokerReplaceEnd)
                {
                    float at = _pokerPluckEnd + discSlot * 0.12f;
                    float u = Mathf.Clamp01((_pokerMotionT - at) / 0.72f);
                    // Toss: quick ease-out to the seat on a small upward arc, hang, then burst.
                    float pu = 1f - Mathf.Pow(1f - Mathf.Clamp01(u / 0.66f), 3f);
                    var dest = classic;
                    r = LerpRect(from, dest, pu);
                    r.y -= Mathf.Sin(pu * Mathf.PI) * classic.height * 0.30f;
                    u = u * u * (3f - 2f * u);
                    // Shared discard pop: scale reaches exactly 0 (no 0.08 floor / 0.02 clamp),
                    // so the card is gone when the pop finishes. Slot stays empty until inbound.
                    float sc = PokerCardAnim.DiscardScale(u);
                    float nw = r.width * sc;
                    float nh = r.height * sc;
                    r = new Rect(r.center.x - nw * 0.5f, r.center.y - nh * 0.5f, nw, nh);
                    // Slow Y-axis flip while floating to the hold seat, then confetti.
                    yaw = u * 210f * (i % 2 == 0 ? 1f : -1f);
                    roll = PokerFanRoll(i) * (1f - u);
                    showFace = PokerCardAnim.DiscardShowFace(u);
                    face = _pokerPrev[i];
                }
                else
                {
                    float at = _pokerReplaceEnd + discSlot * DrawDealStagger;
                    float u = Mathf.Clamp01((_pokerMotionT - at) / DrawInboundT);
                    // Thrown in from the dealer (low-left, a little high), flips face-up mid-air,
                    // overshoots the seat a touch and settles; lands at u = DrawInboundHitU.
                    float hu = Mathf.Clamp01(u / DrawInboundHitU);
                    float px = u < DrawInboundHitU ? 1f - (1f - hu) * (1f - hu) * (1f - hu)
                        : 1f + 0.05f * Mathf.Sin((u - DrawInboundHitU) / (1f - DrawInboundHitU) * Mathf.PI);
                    float py = 1f - (1f - hu) * (1f - hu);
                    var inbound = new Rect(-classic.width * 1.2f, classic.y - classic.height * 0.45f, classic.width, classic.height);
                    r = new Rect(Mathf.LerpUnclamped(inbound.x, classic.x, px), Mathf.Lerp(inbound.y, classic.y, py), classic.width, classic.height);
                    // Landing squash, bottom-anchored, then settle.
                    if (u > DrawInboundHitU)
                    {
                        float q = (u - DrawInboundHitU) / (1f - DrawInboundHitU);
                        float sq = 0.07f * Mathf.Sin(q * Mathf.PI) * (1f - q * 0.4f);
                        float nw = r.width * (1f + sq);
                        float nh = r.height * (1f - sq);
                        r = new Rect(r.center.x - nw * 0.5f, r.yMax - nh, nw, nh);
                    }
                    float fu = Mathf.Clamp01(u / (DrawInboundHitU * 0.9f));
                    yaw = (1f - fu * fu * (3f - 2f * fu)) * 180f;
                    showFace = yaw < 90f;
                    roll = (1f - hu) * -14f;
                    face = showFace ? BirdPoker.Hand[i] : default;
                }
            }
            else if (_pokerFan && BirdPoker.PhaseNow == BirdPoker.Phase.Dealt)
            {
                float slide = _pokerHoldSlide[i];
                Rect fan;
                if (BirdPoker.Hold[i] && _pokerHoldFrom[i].width > 2f)
                    fan = _pokerHoldFrom[i];
                else
                    fan = PokerFanSeat(i, row, 0f);
                r = LerpRect(fan, classic, slide);
                roll = PokerFanRoll(i) * (1f - slide);
                showFace = true;
            }

            r = new Rect(r.x + _pokerKick.x, r.y + _pokerKick.y, r.width, r.height);
            if (i == _pokerHover && !BirdPoker.Hold[i] && _pokerHoldSlide[i] < 0.35f
                && _pokerFan && BirdPoker.PhaseNow == BirdPoker.Phase.Dealt)
            {
                float puff = 1.12f;
                float nw = r.width * puff;
                float nh = r.height * puff;
                r = new Rect(r.center.x - nw * 0.5f, r.y - 18f * Mathf.Max(1f, r.height / 180f), nw, nh);
            }
            float sx = Mathf.Abs(Mathf.Cos(yaw * Mathf.Deg2Rad));
            sparkle = showFace && face.Wild && sx > 0.88f && Mathf.Abs(roll) < 36f;
            if (_pokerMotion == PokerMotion.Deal)
            {
                float fanAt = DealGatherT + DealRiffleT;
                if (_pokerMotionT < fanAt + DealFanT * 0.55f) sparkle = false;
            }
            if (_pokerMotion == PokerMotion.Draw && _pokerRedraw[i] && _pokerMotionT < _pokerReplaceEnd && yaw > 20f && yaw < 160f)
                sparkle = false;
        }

        static Rect LerpRect(Rect a, Rect b, float u)
        {
            return new Rect(
                Mathf.Lerp(a.x, b.x, u),
                Mathf.Lerp(a.y, b.y, u),
                Mathf.Lerp(a.width, b.width, u),
                Mathf.Lerp(a.height, b.height, u));
        }

        // Fan cards the hand is still pinching (not kept / not being kept by a draw).
        int PokerFanLive()
        {
            int live = 0;
            for (int i = 0; i < BirdPoker.HandSize; i++)
                if (!BirdPoker.Hold[i] && !(_pokerMotion == PokerMotion.Draw && _pokerKept[i])) live++;
            return live;
        }

        // Hand sprite rect at rest (no sway, fully shown) and its pinch point on the grip card.
        Rect PokerHandRest(Rect row, float bump, out Vector2 pinch)
        {
            int gripN = Mathf.Max(1, PokerFanLive());
            var grip = PokerFanRestAt(Mathf.Min(gripN - 1, gripN / 2), gripN, row, bump);
            float fanH = row.height * PokerFanScale;
            float h = fanH * 1.88f;
            float w = h * PokerFanHandAspect;
            pinch = new Vector2(grip.center.x - grip.width * 0.08f, grip.yMax - grip.height * 0.02f);
            float x = pinch.x - w * PokerFanPinchU;
            if (x > -12f)
            {
                w = (pinch.x + 12f) / Mathf.Max(0.12f, PokerFanPinchU);
                h = w / PokerFanHandAspect;
                x = pinch.x - w * PokerFanPinchU;
            }
            return new Rect(x, pinch.y - h * PokerFanPinchV, w, h);
        }

        void DrawPokerFanHand(Rect row, float cardW, float cardH, float s, bool front)
        {
            if (!GuiPaint()) return;
            int live = PokerFanLive();
            if (live == 0 && _pokerMotion != PokerMotion.Deal) return;
            float u = 0f;
            float bump = 0f;
            bool show = false;
            if (_pokerMotion == PokerMotion.Deal)
            {
                float fanAt = DealGatherT + DealRiffleT;
                if (_pokerMotionT >= fanAt)
                {
                    show = true;
                    u = Mathf.Clamp01((_pokerMotionT - fanAt) / DealFanT);
                    if (_pokerMotionT >= fanAt + DealFanT)
                    {
                        u = 1f;
                        float b = Mathf.Clamp01((_pokerMotionT - fanAt - DealFanT) / DealBumpT);
                        bump = 18f * Mathf.Sin(Mathf.Clamp01(b / 0.55f) * Mathf.PI)
                            - 6f * Mathf.Sin(Mathf.Clamp01((b - 0.45f) / 0.55f) * Mathf.PI);
                    }
                }
            }
            else if (_pokerFan && BirdPoker.PhaseNow == BirdPoker.Phase.Dealt && _pokerMotion == PokerMotion.None)
            {
                show = true;
                u = 1f;
            }
            else if (_pokerMotion == PokerMotion.Draw)
            {
                show = true;
                // Shared hand/arm fade: the holding hand is gone 0.22 s into the draw.
                u = PokerHandFade(_pokerMotionT, float.NegativeInfinity, 0f, PokerFanHandGoneT, PokerFanHandGoneT, out _, out _);
            }
            if (!show || u < 0.02f) return;
            var rest = PokerHandRest(row, bump, out Vector2 restPinch);
            var sway = PokerSwayNow(row, bump);
            // Same rigid sway as the cards (pinch through Apply, tilt + Turn), kick added after like the cards.
            Vector2 pinch = sway.Apply(restPinch) + _pokerKick;
            var dest = new Rect(
                pinch.x + (rest.x - restPinch.x),
                pinch.y + (rest.y - restPinch.y) + (1f - u) * rest.height * 0.35f,
                rest.width, rest.height);
            float ang = 2.4f + sway.Turn;
            var prev = GUI.matrix;
            GUIUtility.RotateAroundPivot(ang, pinch);
            GUI.color = u >= 0.98f ? Color.white : new Color(1f, 1f, 1f, u);
            if (!front)
            {
                // Full palm and sleeve behind the cards, from the pinch down (sleeve runs off-screen).
                DrawPokerPalm(dest, true);
            }
            else
            {
                // The only front layer: the whole thumb, last over every card part.
                DrawPokerThumb(dest);
            }
            GUI.matrix = prev;
            GUI.color = Color.white;
        }

        static void DrawSprite(Rect dest, Sprite spr, bool scaleToFit)
        {
            if (spr == null || spr.texture == null) return;
            var tex = spr.texture;
            var r = spr.textureRect;
            float tw = Mathf.Max(1f, tex.width);
            float th = Mathf.Max(1f, tex.height);
            var uv = new Rect(r.x / tw, r.y / th, r.width / tw, r.height / th);
            if (scaleToFit && r.height > 1f)
            {
                float texA = r.width / r.height;
                float destA = dest.height > 1f ? dest.width / dest.height : texA;
                if (texA > destA)
                {
                    float hh = dest.width / texA;
                    dest = new Rect(dest.x, dest.y + (dest.height - hh) * 0.5f, dest.width, hh);
                }
                else
                {
                    float ww = dest.height * texA;
                    dest = new Rect(dest.x + (dest.width - ww) * 0.5f, dest.y, ww, dest.height);
                }
            }
            GUI.DrawTextureWithTexCoords(dest, tex, uv);
        }

        // Vertical slice of a sprite. top0/top1 are fractions down from the top of the
        // letterboxed dest (0 = top, 1 = bottom), matching PokerFanPinchV. fadeOut (0..1) is the
        // share of the slice, counted from its bottom, that ramps from opaque to clear in
        // PokerBandFadeSteps strips, so a cut bottom edge dissolves instead of ending in a hard line.
        const int PokerBandFadeSteps = 14;
        static void DrawSpriteBand(Rect dest, Sprite spr, float top0, float top1, float fadeOut = 0f)
        {
            if (spr == null || spr.texture == null) return;
            if (top1 - top0 < 0.01f) return;
            var tex = spr.texture;
            var r = spr.textureRect;
            float tw = Mathf.Max(1f, tex.width);
            float th = Mathf.Max(1f, tex.height);
            var uv = new Rect(r.x / tw, r.y / th, r.width / tw, r.height / th);
            if (r.height > 1f)
            {
                float texA = r.width / r.height;
                float destA = dest.height > 1f ? dest.width / dest.height : texA;
                if (texA > destA)
                {
                    float hh = dest.width / texA;
                    dest = new Rect(dest.x, dest.y + (dest.height - hh) * 0.5f, dest.width, hh);
                }
                else
                {
                    float ww = dest.height * texA;
                    dest = new Rect(dest.x + (dest.width - ww) * 0.5f, dest.y, ww, dest.height);
                }
            }
            float band = Mathf.Clamp01(top1) - Mathf.Clamp01(top0);
            if (band < 0.01f) return;
            float t0 = Mathf.Clamp01(top0);
            float uvTop = uv.y + uv.height;
            float fade = Mathf.Clamp01(fadeOut) * band;
            float solid = band - fade;
            if (solid > 0.0001f)
            {
                var slice = new Rect(dest.x, dest.y + dest.height * t0, dest.width, dest.height * solid);
                var sliceUv = new Rect(uv.x, uvTop - uv.height * (t0 + solid), uv.width, uv.height * solid);
                GUI.DrawTextureWithTexCoords(slice, tex, sliceUv);
            }
            if (fade <= 0.0001f) return;
            var keep = GUI.color;
            float stepH = fade / PokerBandFadeSteps;
            for (int k = 0; k < PokerBandFadeSteps; k++)
            {
                float y0 = t0 + solid + stepH * k;
                var slice = new Rect(dest.x, dest.y + dest.height * y0, dest.width, dest.height * stepH + 0.5f);
                var sliceUv = new Rect(uv.x, uvTop - uv.height * (y0 + stepH), uv.width, uv.height * stepH);
                GUI.color = new Color(keep.r, keep.g, keep.b, keep.a * (1f - (k + 0.5f) / PokerBandFadeSteps));
                GUI.DrawTextureWithTexCoords(slice, tex, sliceUv);
            }
            GUI.color = keep;
        }

        // The one thumb draw. Holding hand and dealing hand both end here, so the thumb is the
        // same whole-canvas art, letterboxed like the palm, wherever it is drawn.
        // Soft contact shadow on the cards first, then the thumb: the pinch presses into the card.
        static void DrawPokerThumb(Rect dest)
        {
            DrawSprite(dest, SpriteCatalog.HandThumbShadow, true);
            DrawSprite(dest, SpriteCatalog.HandThumb, true);
        }

        // The one rest-of-hand draw (palm, fingers, forearm, sleeve), from the pinch down, always
        // under the cards. Shared by the holding hand and the dealing hand. sleeveTail is true only
        // for the holding hand: its sleeve runs on past the screen bottom so the arm never ends in a
        // floating cut on tall phones. The dealing hand glides and flicks across the row, so it
        // keeps the plain palm-and-forearm span (the build 49 look); a tail there stretches into a
        // long arm.
        // armFade is the share of the forearm, from its cut end up, that dissolves (dealing hand
        // only; the holding hand has the tail and needs no fade).
        static void DrawPokerPalm(Rect dest, bool sleeveTail, float armFade = 0f)
        {
            if (sleeveTail)
                DrawSpriteTail(dest, SpriteCatalog.HandPalm, Screen.height + dest.width * 0.25f + 40f);
            DrawSpriteBand(dest, SpriteCatalog.HandPalm, PokerFanPinchV, 1f, armFade);
        }

        // Continue the sprite past its fitted bottom edge down to toY: its bottom texel row is
        // extruded along the forearm's axis (sheared down-left, slope armSlope) so the sleeve
        // runs on off-screen seamlessly. Local GUI space, so it follows the hand's rotation.
        static void DrawSpriteTail(Rect dest, Sprite spr, float toY)
        {
            if (spr == null || spr.texture == null) return;
            var tex = spr.texture;
            var r = spr.textureRect;
            if (r.height <= 1f || r.width <= 1f) return;
            float tw = Mathf.Max(1f, tex.width);
            float th = Mathf.Max(1f, tex.height);
            float texA = r.width / r.height;
            float destA = dest.height > 1f ? dest.width / dest.height : texA;
            if (texA > destA)
            {
                float hh = dest.width / texA;
                dest = new Rect(dest.x, dest.y + (dest.height - hh) * 0.5f, dest.width, hh);
            }
            else
            {
                float ww = dest.height * texA;
                dest = new Rect(dest.x + (dest.width - ww) * 0.5f, dest.y, ww, dest.height);
            }
            if (toY <= dest.yMax) return;
            const float armSlope = 0.6f;
            float texel = dest.height / r.height;
            float cutY = dest.yMax - texel * 0.75f;
            var keep = GUI.matrix;
            var shear = Matrix4x4.identity;
            shear.m01 = -armSlope;          // x' = x - armSlope * (y - cutY)
            shear.m03 = armSlope * cutY;
            GUI.matrix = keep * shear;
            var uv = new Rect(r.x / tw, (r.y + 0.5f) / th, r.width / tw, 1f / th);
            GUI.DrawTextureWithTexCoords(new Rect(dest.x, cutY, dest.width, toY - cutY), tex, uv);
            GUI.matrix = keep;
        }

        void TickPokerWarm()
        {
            Sfx.PokerWarmStep();
            TouchPokerSprite(SpriteCatalog.PokerArtWarmStep());
        }

        void FinishPokerWarm()
        {
            Sfx.PokerWarmFinish();
            while (!SpriteCatalog.PokerArtReady)
                TouchPokerSprite(SpriteCatalog.PokerArtWarmStep());
        }

        static void TouchPokerSprite(Sprite spr)
        {
            if (spr == null || spr.texture == null) return;
            var prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.004f);
            GUI.DrawTexture(new Rect(-8f, -8f, 2f, 2f), spr.texture, ScaleMode.StretchToFill, true);
            GUI.color = prev;
        }

        void TickPokerDash(float s)
        {
            if (_pokerChainBreak >= 0f)
                _pokerChainBreak += Time.unscaledDeltaTime / 0.72f;
            float want = PokerBetBarAway() ? 1f : 0f;
            _pokerDash = Mathf.MoveTowards(_pokerDash, want, Time.unscaledDeltaTime * 3.4f);
        }

        // Bet bar tucks under the screen while the fan hand needs the space
        // (deal fan-in, held hand, draw); it returns as soon as a result settles.
        bool PokerBetBarAway()
        {
            if (_pokerMotion == PokerMotion.Deal)
                return _pokerMotionT >= DealGatherT + DealRiffleT;
            if (PokerMotionBusy() || PokerWinFlying()) return true;
            return BirdPoker.PhaseNow == BirdPoker.Phase.Dealt;
        }

        bool DrawPokerDash(Rect betR, Rect actR, float s, bool busy, bool steppers, string act)
        {
            float lift = _pokerDash * (betR.height + 48f * s);
            var bet = new Rect(betR.x, betR.y + lift, betR.width, betR.height);
            var actBox = actR;
            DrawPokerBetBar(bet, s, busy, steppers);
            DrawPokerWinBanner(betR, actR, s);
            if (string.IsNullOrEmpty(act)) return false;
            bool fire = DrawFloralBtn(actBox, act, s, true);
            return fire;
        }

        // Win fanfare: "WIN $X" punches in big at centre with rays, sparkles and confetti,
        // holds, then whooshes (speed lines + ghost trail) into the enlarged resting banner.
        const float WinPunchT = 0.32f;
        const float WinHoldT = 0.90f;
        const float WinWhooshT = 0.26f;
        const float WinLandT = 0.30f;
        // Speed lines / ghosts span the word's own path over this much time.
        const float WinTrailT = 0.065f;
        // Jackpot ranks (BirdPoker.IsJackpot: FIVE WILDS, NATURAL FIVE) open with a gold rank
        // banner alone for WinTitleLeadT, then the WIN amount punches in under it, and the pair
        // holds WinJackpotHoldT longer before the WIN word flies to the resting banner.
        const float WinTitleLeadT = 0.70f;
        const float WinJackpotHoldT = 1.10f;
        const float WinJackpotBurstK = 1.35f;
        static bool WinJackpot => BirdPoker.IsJackpot(BirdPoker.LastRank);
        static float WinLead => WinJackpot ? WinTitleLeadT : 0f;
        static float WinHold => WinHoldT + (WinJackpot ? WinJackpotHoldT : 0f);
        static float WinWhooshAt => WinLead + WinPunchT + WinHold;
        static float WinLandAt => WinWhooshAt + WinWhooshT;
        static float WinDoneAt => WinLandAt + WinLandT;
        // Purse roll for a revealed hand win (Purse display hold → RevealHeldCoins).
        const float PokerWinRollT = 1.15f;
        const float PokerJackpotRollT = 2.40f;
        const float PokerFullcardRollT = 2.00f;
        static readonly Color PokerWinRed = new Color(0.94f, 0.12f, 0.14f, 1f);
        static readonly Color PokerWinGold = new Color(1f, 0.86f, 0.22f, 1f);

        void BeginPokerWin() => _pokerWinT = BirdPoker.LastWin > 0 ? 0f : -1f;

        // Punch / hold / whoosh still playing (the landing bounce already counts as rested).
        bool PokerWinFlying() => _pokerWinT >= 0f && _pokerWinT < WinLandAt;

        void TickPokerWin()
        {
            if (_pokerWinT < 0f) return;
            // A fresh punch-card stamp plays first; the fanfare (and its purse roll) waits for it.
            if (_pokerStamp) return;
            RevealPokerPay(true);
            if (_pokerWinT >= WinLead) RevealPokerPay(false);
            if (_pokerWinT >= WinDoneAt) return;
            float prev = _pokerWinT;
            _pokerWinT += Time.unscaledDeltaTime;
            if (WinJackpot)
            {
                // Rank banner slams in with the poker fanfare; the WIN amount follows with a chime.
                if (prev <= 0f && _pokerWinT > 0f)
                {
                    Sfx.Celebrate();
                    PunchPoker(0.30f, 9f, 2.2f);
                }
                if (prev < WinLead && _pokerWinT >= WinLead)
                {
                    Sfx.Combo(5, 0.60f);
                    PunchPoker(0.18f, 5f, 1.2f);
                    RevealPokerPay(false);
                }
            }
            if (prev < WinWhooshAt && _pokerWinT >= WinWhooshAt) Sfx.CardWhoosh();
            if (prev < WinLandAt && _pokerWinT >= WinLandAt) LandPokerWin();
        }

        // Shows this hand's held payout on the purse (rolling tick-up). fullcard = the FULLCARD
        // prize (stamp bingo line); else the hand win (its WIN word punching in). Each amount
        // releases once; ResetRound releases anything left if the hand is abandoned.
        void RevealPokerPay(bool fullcard)
        {
            int amt = fullcard ? _pokerHeldFull : _pokerHeldWin;
            if (amt <= 0) return;
            if (fullcard) _pokerHeldFull = 0;
            else _pokerHeldWin = 0;
            float roll = fullcard ? PokerFullcardRollT
                : (BirdPoker.IsJackpot(BirdPoker.LastRank) ? PokerJackpotRollT : PokerWinRollT);
            RevealHeldCoins(amt, roll);
        }

        void ClearPokerHeldPay()
        {
            _pokerHeldWin = 0;
            _pokerHeldFull = 0;
        }

        // DEAL/DRAW face. Drawn from CanDeal: hidden while the phase is still Drawn.
        string PokerActLabel()
        {
            return PokerDealGate.ActLabel(BirdPoker.PhaseNow, BirdPoker.CanDeal());
        }

        bool PokerResultPlaying() =>
            PokerMotionBusy() || _pokerStamp || (_pokerWinT >= 0f && _pokerWinT < WinLandAt);

        // The word hits the banner still moving: clink + a short table punch sell the impact.
        void LandPokerWin()
        {
            Sfx.Clink();
            PunchPoker(0.14f, 4.0f, 0.8f);
        }

        void SkipPokerWinOnTap()
        {
            var e = Event.current;
            if (e.type != EventType.MouseDown) return;
            RevealPokerPay(true);
            RevealPokerPay(false);
            _pokerWinT = WinLandAt;
            LandPokerWin();
            e.Use();
        }

        static string PokerWinText()
        {
            int win = BirdPoker.LastWin;
            if (win != _pokerWinAmt || _pokerWinText == null)
            {
                _pokerWinAmt = win;
                _pokerWinText = "WIN " + Money.Format(win);
            }
            return _pokerWinText;
        }

        static GUIStyle PokerWinStyle(TextAnchor align)
        {
            if (align == TextAnchor.MiddleLeft)
                return PokerBold(ref _pokerWinLeft, TextAnchor.MiddleLeft);
            return PokerBold(ref _pokerWinCenter, TextAnchor.MiddleCenter);
        }

        static float PokerWinHeroH(float s) => Mathf.Min(Screen.width * 0.26f, 190f * s);

        // Resting banner: 1.75x the pre-fanfare height (1.4 * 1.25). FitFont's hi is
        // height * WinRestFontFrac, so the landed word is ~25% larger. Left edge is
        // HudTextLeft. The flight lands on this rect + font via PokerWinGeom (restC / restK).
        const float WinRestGrow = 1.75f;
        // Build 52: the red poker status texts ("WIN $x" at rest and "TAP A CARD TO KEEP") share
        // ONE size boost, so they stay consistent. WIN's 0.62 base fraction * 1.25 = 0.775.
        // That fraction also drives the fanfare's landing scale (restK), so the flight arrives
        // at the same size. Position, color and outline are unchanged; FitFont still caps it
        // by the rect, so a cramped screen shrinks it instead of overlapping BET or the cards.
        const float PokerStatusTextBoost = 1.25f;
        const float WinRestFontFrac = 0.62f * PokerStatusTextBoost;

        static Rect PokerWinRestRect(Rect betR, Rect actR, float s, float cardBottom)
        {
            var safe = PokerSafeGui();
            float h = Mathf.Clamp(42f * s, 36f, 56f) * WinRestGrow;
            float x = HudTextLeft(s);
            float w = Mathf.Min(safe.xMax - 12f - x, Screen.width * 0.92f);
            if (w < 8f) w = 8f;
            float top = Mathf.Min(betR.y, actR.y);
            float y = top - h - 10f * s;
            float cardClear = cardBottom + 8f * s;
            if (cardBottom > 8f && y < cardClear)
            {
                float room = top - 10f * s - cardClear;
                if (room >= 24f * s)
                {
                    h = room;
                    y = cardClear;
                }
            }
            if (y < 8f) y = 8f;
            return new Rect(x, y, w, h);
        }

        // Screen.safeArea in GUI coordinates (top-left origin).
        static Rect PokerSafeGui()
        {
            var a = Screen.safeArea;
            if (a.width < 8f || a.height < 8f) return new Rect(0f, 0f, Screen.width, Screen.height);
            return new Rect(a.x, Screen.height - a.yMax, a.width, a.height);
        }

        // Largest scale the hero word reaches: EaseOutBack punch peak (~1.116) plus hold wobble.
        const float WinHeroMaxK = 1.13f;

        // FitFont walks sizes; cache per payout text so the fanfare never refits every frame.
        void FitPokerWinFonts(string text, Rect rest, float heroH)
        {
            int sw = Screen.width;
            int sh = Screen.height;
            int rh = Mathf.RoundToInt(rest.height);
            int hh = Mathf.RoundToInt(heroH);
            if (text == _pokerWinFitText && sw == _pokerWinFitW && sh == _pokerWinFitH
                && rh == _pokerWinFitRest && hh == _pokerWinFitHero)
                return;
            _pokerWinFitText = text;
            _pokerWinFitW = sw;
            _pokerWinFitH = sh;
            _pokerWinFitRest = rh;
            _pokerWinFitHero = hh;
            var st = PokerWinStyle(TextAnchor.MiddleCenter);
            // Hero must stay inside the safe area even at the punch-in overshoot, outline included
            // (at 0.80 of the raw screen width "WIN $1,000" clipped the left edge at the peak).
            float safeW = PokerSafeGui().width;
            _pokerWinHeroFont = FitFont(st, text, safeW * 0.90f / WinHeroMaxK / 1.06f, heroH, 24, Mathf.RoundToInt(heroH));
            _pokerWinRestFont = FitFont(st, text, rest.width * 0.96f, rest.height, 16, Mathf.RoundToInt(rest.height * WinRestFontFrac));
        }

        static float EaseOutBack(float u)
        {
            const float c1 = 2.2f;
            const float c3 = c1 + 1f;
            float v = u - 1f;
            return 1f + c3 * v * v * v + c1 * v * v;
        }

        // Accelerate hard and arrive still moving (~60% of peak speed); the landing squash absorbs
        // the hit. SmoothStep crawled through its last third, which read as floaty.
        static float WinWhooshEase(float t)
        {
            float u = Mathf.Clamp01((t - WinWhooshAt) / WinWhooshT);
            return u * u * (2.2f - 1.2f * u);
        }

        struct PokerWinGeo
        {
            public string Text;
            public GUIStyle St;
            public Vector2 Size, HeroC, RestC;
            public float RestK;
        }

        // Shared by the flight and the resting banner so the handoff uses identical numbers:
        // restC = banner text centre, restK = restFont / heroFont.
        PokerWinGeo PokerWinGeom(Rect betR, Rect actR, float s)
        {
            var g = new PokerWinGeo { Text = PokerWinText() };
            var rest = PokerWinRestRect(betR, actR, s, _pokerRowBottom);
            FitPokerWinFonts(g.Text, rest, PokerWinHeroH(s));
            g.St = PokerWinStyle(TextAnchor.MiddleCenter);
            g.St.fontSize = _pokerWinHeroFont;
            var content = PokerGui(g.Text);
            g.Size = g.St.CalcSize(content);
            var rst = PokerWinStyle(TextAnchor.MiddleLeft);
            rst.fontSize = _pokerWinRestFont;
            float restW = rst.CalcSize(content).x;
            g.HeroC = new Vector2(PokerSafeGui().center.x, Screen.height * 0.47f);
            g.RestC = new Vector2(rest.x + restW * 0.5f, rest.center.y);
            g.RestK = _pokerWinRestFont / (float)Mathf.Max(1, _pokerWinHeroFont);
            return g;
        }

        // Fanfare word pose at time t: centre + scale (1 = hero size).
        static void PokerWinPose(float t, Vector2 heroC, Vector2 restC, float restK, out Vector2 c, out float k)
        {
            float tw = t - WinLead;
            if (tw < WinPunchT)
            {
                c = heroC;
                k = Mathf.LerpUnclamped(0.25f, 1f, EaseOutBack(Mathf.Clamp01(tw / WinPunchT)));
                return;
            }
            float windUp = 0.06f * Mathf.SmoothStep(0f, 1f, (t - (WinWhooshAt - 0.14f)) / 0.14f);
            if (t < WinWhooshAt)
            {
                float h = tw - WinPunchT;
                c = heroC;
                k = 1f + 0.035f * Mathf.Sin(h * 9f) * Mathf.Exp(-h * 2.4f) + windUp;
                return;
            }
            float e = WinWhooshEase(t);
            var d = restC - heroC;
            var perp = new Vector2(-d.y, d.x).normalized;
            // Swoop low and shrink ahead of the travel so the word never clips the screen edge.
            c = Vector2.Lerp(heroC, restC, e) - perp * Mathf.Sin(e * Mathf.PI) * d.magnitude * 0.10f;
            k = Mathf.Lerp(1f + windUp, restK, 1f - (1f - e) * (1f - e));
        }

        void DrawPokerWinFanfare(Rect betR, Rect actR, float s)
        {
            if (!PokerWinFlying() || _pokerStamp || !_pokerShowPay || BirdPoker.LastWin <= 0) return;
            float t = _pokerWinT;
            var g = PokerWinGeom(betR, actR, s);
            string text = g.Text;
            var st = g.St;
            var size = g.Size;
            var heroC = g.HeroC;
            var restC = g.RestC;
            float restK = g.RestK;

            bool jackpot = WinJackpot;
            // The burst opens with the first thing on screen (the rank banner on a jackpot).
            float punch = Mathf.Clamp01(t / WinPunchT);
            float whoosh = WinWhooshEase(t);
            float burst = Mathf.SmoothStep(0f, 1f, punch) * (1f - Mathf.Clamp01(whoosh * 2.5f));
            // Bigger hands throw more rays (shared level: FIVE WILDS >= NaturalFive).
            int rays = 12 + 2 * BirdPoker.CelebrationLevel(BirdPoker.LastRank);
            float burstK = jackpot ? WinJackpotBurstK : 1f;
            if (burst > 0.01f)
            {
                float reach = size.x * 0.95f * burstK * (0.35f + 0.65f * Mathf.Sin(punch * Mathf.PI * 0.5f));
                // Jackpot: a slower counter-turning outer ring behind the main spokes.
                if (jackpot) DrawPokerWinRays(heroC, reach * 1.30f, -t * 0.6f, burst * 0.45f, rays / 2);
                DrawPokerWinRays(heroC, reach, t, burst, rays);
            }
            if (t < WinLandAt) DrawGiftConfetti(t, s, heroC);
            if (jackpot) DrawPokerRankBanner(g, t, s, whoosh);

            if (t >= WinLead)
            {
                PokerWinPose(t, heroC, restC, restK, out Vector2 c, out float k);
                DrawPokerWinTrail(g, t, s, Color.Lerp(PokerWinGold, PokerWinRed, whoosh), _pokerRowBottom + 10f * s);
                DrawPokerWinWord(text, st, c, size, k, Color.Lerp(PokerWinGold, PokerWinRed, whoosh), 1f, true);
            }
            if (burst > 0.01f) DrawPokerWinSparkles(heroC, size * burstK, t, burst);
        }

        static GUIStyle _pokerRankSt;
        static string _pokerRankFitText;
        static int _pokerRankFitW = -1, _pokerRankFitH = -1, _pokerRankPx;

        // Jackpot rank banner ("FIVE WILDS" / "NATURAL FIVE"): big gold title above the hero WIN
        // word. Punches in first, gently breathes through the hold, fades as WIN flies to rest.
        void DrawPokerRankBanner(in PokerWinGeo g, float t, float s, float whoosh)
        {
            string title = BirdPoker.RankLabel(BirdPoker.LastRank);
            if (string.IsNullOrEmpty(title)) return;
            float alpha = 1f - Mathf.Clamp01(whoosh * 2.2f);
            if (alpha < 0.02f) return;
            var st = PokerBold(ref _pokerRankSt, TextAnchor.MiddleCenter);
            float heroH = PokerWinHeroH(s);
            var safe = PokerSafeGui();
            int sw = Screen.width, sh = Screen.height;
            if (title != _pokerRankFitText || sw != _pokerRankFitW || sh != _pokerRankFitH)
            {
                _pokerRankFitText = title;
                _pokerRankFitW = sw;
                _pokerRankFitH = sh;
                // Room for the punch overshoot and the outline inside the safe area.
                _pokerRankPx = FitFont(st, title, safe.width * 0.90f / WinHeroMaxK / 1.06f, heroH * 0.82f, 22,
                    Mathf.RoundToInt(heroH * 0.82f));
            }
            st.fontSize = _pokerRankPx;
            var size = st.CalcSize(PokerGui(title));
            float gapY = Mathf.Max(g.Size.y * 0.5f, heroH * 0.40f) + size.y * 0.5f + 6f * s;
            var c = new Vector2(g.HeroC.x, Mathf.Max(safe.y + size.y * 0.6f, g.HeroC.y - gapY));
            float pu = Mathf.Clamp01(t / (WinPunchT * 1.15f));
            float k = pu < 1f
                ? Mathf.LerpUnclamped(0.30f, 1f, EaseOutBack(pu))
                : 1f + 0.025f * Mathf.Sin((t - WinPunchT) * 4.2f);
            // Soft gold plate behind the title so it reads over the rays and confetti.
            float plateA = 0.42f * alpha * Mathf.SmoothStep(0f, 1f, pu);
            GUI.color = new Color(1f, 0.78f, 0.24f, plateA);
            var plate = new Rect(c.x - size.x * 0.70f * k, c.y - size.y * 0.85f * k, size.x * 1.40f * k, size.y * 1.70f * k);
            GUI.DrawTexture(plate, GlowTex(), ScaleMode.StretchToFill, true);
            GUI.color = Color.white;
            // Light sweep: the gold brightens as a highlight passes, then settles.
            float sheen = 0.5f + 0.5f * Mathf.Sin(t * 3.0f);
            var fill = Color.Lerp(PokerWinGold, new Color(1f, 0.96f, 0.70f, 1f), 0.25f * sheen);
            DrawPokerWinWord(title, st, c, size, k, fill, alpha, true);
        }

        // outline=false paints fill only (ghost trail) — stacked outlines turn to white smears.
        static void DrawPokerWinWord(string text, GUIStyle st, Vector2 c, Vector2 size, float k, Color fill, float alpha, bool outline)
        {
            var r = new Rect(c.x - size.x * 0.5f, c.y - size.y * 0.5f, size.x, size.y);
            var prev = GUI.matrix;
            GUIUtility.ScaleAroundPivot(new Vector2(k, k), c);
            var tint = new Color(fill.r, fill.g, fill.b, alpha);
            if (outline)
                StampLight(r, text, st, tint);
            else
            {
                Paint(st, tint);
                GUI.Label(r, text, st);
            }
            GUI.matrix = prev;
        }

        static void DrawPokerWinRays(Vector2 c, float reach, float t, float alpha, int count)
        {
            var glow = GlowTex();
            GUI.color = new Color(1f, 0.84f, 0.30f, 0.70f * alpha);
            GUI.DrawTexture(new Rect(c.x - reach * 1.1f, c.y - reach * 0.7f, reach * 2.2f, reach * 1.4f), glow, ScaleMode.StretchToFill, true);
            var prev = GUI.matrix;
            for (int i = 0; i < count; i++)
            {
                bool major = (i & 1) == 0;
                float len = reach * (major ? 1f : 0.70f);
                float th = reach * (major ? 0.16f : 0.10f);
                GUI.matrix = prev;
                GUIUtility.RotateAroundPivot(i * 360f / count + t * 24f, c);
                GUI.color = major
                    ? new Color(1f, 0.94f, 0.58f, 0.95f * alpha)
                    : new Color(1f, 0.66f, 0.24f, 0.80f * alpha);
                GUI.DrawTexture(new Rect(c.x, c.y - th * 0.5f, len, th), glow, ScaleMode.StretchToFill, true);
                // Second pass doubles the spoke so it reads as a ray, not haze.
                GUI.DrawTexture(new Rect(c.x + len * 0.15f, c.y - th * 0.25f, len * 0.85f, th * 0.5f), glow, ScaleMode.StretchToFill, true);
            }
            GUI.matrix = prev;
            GUI.color = Color.white;
        }

        static void DrawPokerWinSparkles(Vector2 c, Vector2 size, float t, float alpha)
        {
            var spark = SpriteCatalog.Sparkle;
            var tex = spark != null && spark.texture != null ? spark.texture : GlowTex();
            const int n = 10;
            for (int i = 0; i < n; i++)
            {
                float tw = Mathf.Max(0f, Mathf.Sin(t * 6.2f + i * 1.7f));
                tw *= tw;
                float ang = i * Mathf.PI * 2f / n + t * 0.6f;
                float rx = size.x * (0.56f + 0.06f * (i % 3));
                float ry = size.y * (0.95f + 0.10f * (i % 2));
                float sz = size.y * 0.30f * (0.35f + 0.9f * tw);
                GUI.color = new Color(1f, 0.97f, 0.82f, (0.35f + 0.65f * tw) * alpha);
                GUI.DrawTexture(new Rect(c.x + Mathf.Cos(ang) * rx - sz * 0.5f, c.y + Mathf.Sin(ang) * ry - sz * 0.5f, sz, sz), tex, ScaleMode.ScaleToFit, true);
            }
            GUI.color = Color.white;
        }

        // Speed lines + ghost trail, both sampled from the word's real path: every line spans
        // where the word was WinTrailT ago → where it is now, so it sits right behind the moving
        // word, follows the arc's actual heading, grows/shrinks with actual speed, and collapses
        // into the banner after landing instead of vanishing (the banner keeps drawing it).
        static void DrawPokerWinTrail(in PokerWinGeo g, float t, float s, Color fill, float ceilY)
        {
            float tail = t - WinTrailT;
            if (t <= WinWhooshAt || tail >= WinLandAt) return;
            float tNow = Mathf.Min(t, WinLandAt);
            tail = Mathf.Max(tail, WinWhooshAt);
            PokerWinPose(tNow, g.HeroC, g.RestC, g.RestK, out Vector2 c, out float k);
            PokerWinPose(tail, g.HeroC, g.RestC, g.RestK, out Vector2 cb, out float kb);
            var seg = c - cb;
            float len = seg.magnitude;
            if (len < 3f) return;
            // Peak ease slope is ~1.34, so this is the longest trail the flight can make.
            float peak = (g.RestC - g.HeroC).magnitude * 1.34f * WinTrailT / WinWhooshT;
            float speed = Mathf.Clamp01(len / Mathf.Max(1f, peak));

            // Ghosts first (fill only): earlier poses on the same path.
            for (int gi = 3; gi >= 1; gi--)
            {
                float gt = Mathf.Clamp(t - gi * WinTrailT / 3f, WinWhooshAt, WinLandAt);
                PokerWinPose(gt, g.HeroC, g.RestC, g.RestK, out Vector2 gc, out float gk);
                if ((gc - c).sqrMagnitude < 16f) continue;
                DrawPokerWinWord(g.Text, g.St, gc, g.Size, gk, fill, 0.34f * speed / gi, false);
            }

            var glow = GlowTex();
            var dir = seg / len;
            var perp = new Vector2(-dir.y, dir.x);
            float back = Mathf.Atan2(-dir.y, -dir.x) * Mathf.Rad2Deg;
            var wordSz = g.Size * k;
            var half = wordSz * 0.5f;
            // Emerge from inside the word's box on its trailing side (the old fixed 0.2·width
            // offset put the roots above a wide word's top edge, so lines floated detached).
            float inset = 0.55f * Mathf.Min(half.x / Mathf.Max(0.05f, Mathf.Abs(dir.x)), half.y / Mathf.Max(0.05f, Mathf.Abs(dir.y)));
            // Spread lanes across the word's silhouette as seen along the motion.
            float spread = Mathf.Abs(perp.x) * half.x + Mathf.Abs(perp.y) * half.y;
            var prev = GUI.matrix;
            const int n = 9;
            for (int i = 0; i < n; i++)
            {
                float hbit = (i * 37 + 11) * 0.173f;
                hbit -= Mathf.Floor(hbit);
                float lat = (i / (float)(n - 1) - 0.5f) * 2f * spread * 0.85f;
                // Lanes start inside the word (hidden under it) and run back along the path.
                float lane = len * (0.75f + 0.50f * hbit) + inset;
                float th = Mathf.Max(2.5f, wordSz.y * (0.045f + 0.045f * hbit));
                var root = c - dir * inset + perp * lat;
                // Never streak up over the card row: clip the tail at the row's bottom edge.
                if (dir.y > 0.02f)
                {
                    float room = (root.y - ceilY) / dir.y;
                    if (room < 6f) continue;
                    lane = Mathf.Min(lane, room);
                }
                GUI.matrix = prev;
                GUIUtility.RotateAroundPivot(back, root);
                GUI.color = new Color(1f, 0.90f, 0.55f, 0.55f * speed);
                GUI.DrawTexture(new Rect(root.x, root.y - th * 1.5f, lane, th * 3f), glow, ScaleMode.StretchToFill, true);
                GUI.color = new Color(1f, 1f, 0.96f, 0.85f * speed);
                GUI.DrawTexture(new Rect(root.x, root.y - th * 0.18f, lane * 0.85f, th * 0.36f), Texture2D.whiteTexture);
            }
            GUI.matrix = prev;
            GUI.color = Color.white;
        }

        void DrawPokerWinBanner(Rect betR, Rect actR, float s)
        {
            if (!_pokerShowPay || BirdPoker.LastWin <= 0 || PokerWinFlying()) return;
            var g = PokerWinGeom(betR, actR, s);
            string text = g.Text;
            var r = PokerWinRestRect(betR, actR, s, _pokerRowBottom);
            var st = PokerWinStyle(TextAnchor.MiddleLeft);
            st.fontSize = _pokerWinRestFont;
            var textR = new Rect(r.x, r.y, st.CalcSize(PokerGui(text)).x, r.height);
            bool landing = _pokerWinT >= WinLandAt && _pokerWinT < WinDoneAt;
            float land = landing ? Mathf.Clamp01((_pokerWinT - WinLandAt) / WinLandT) : 1f;
            float hit = landing ? 1f - land : 0f;
            // Glow plate eases in over the landing (it used to pop in at the handoff frame).
            float plateIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(land / 0.7f));
            float breathe = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.2f);
            var plate = new Rect(textR.x - r.height * 0.5f, textR.y - r.height * 0.3f, textR.width + r.height, textR.height * 1.6f);
            GUI.color = new Color(1f, 0.80f, 0.28f, (0.30f + 0.12f * breathe) * plateIn);
            GUI.DrawTexture(plate, GlowTex(), ScaleMode.StretchToFill, true);
            if (hit > 0.01f)
            {
                // Impact flash: ramps in over ~2 frames, blooms outward and fades.
                float flashIn = Mathf.Clamp01((_pokerWinT - WinLandAt) / 0.035f);
                float grow = 1f + 0.55f * (1f - hit);
                var fr = new Rect(plate.center.x - plate.width * grow * 0.5f, plate.center.y - plate.height * grow * 0.5f,
                    plate.width * grow, plate.height * grow);
                GUI.color = new Color(1f, 0.93f, 0.62f, 0.85f * hit * hit * flashIn);
                GUI.DrawTexture(fr, GlowTex(), ScaleMode.StretchToFill, true);
            }
            GUI.color = Color.white;
            // Streaks/ghosts collapse into the banner behind the text.
            if (landing) DrawPokerWinTrail(g, _pokerWinT, s, PokerWinRed, _pokerRowBottom + 10f * s);
            // Impact squash → stretch → settle; starts at 1 so the handoff scale is continuous.
            float pop = 1f + 0.28f * Mathf.Sin(land * Mathf.PI * 2f) * Mathf.Exp(-land * 3.2f);
            var prev = GUI.matrix;
            // Pivot on the left edge so the squash grows away from the screen edge.
            if (Mathf.Abs(pop - 1f) > 0.001f) GUIUtility.ScaleAroundPivot(new Vector2(pop, 2f - pop), new Vector2(textR.x, textR.center.y));
            StampLight(r, text, st, PokerWinRed);
            GUI.matrix = prev;
            if (hit > 0.01f) DrawPokerWinImpactSparks(textR, land);
        }

        // Sparkle ring thrown off the banner on impact.
        static void DrawPokerWinImpactSparks(Rect textR, float land)
        {
            var spark = SpriteCatalog.Sparkle;
            var tex = spark != null && spark.texture != null ? spark.texture : GlowTex();
            var c = textR.center;
            float out1 = 1f - (1f - land) * (1f - land) * (1f - land);
            const int n = 12;
            for (int i = 0; i < n; i++)
            {
                float hbit = (i * 29 + 7) * 0.131f;
                hbit -= Mathf.Floor(hbit);
                float ang = (i + 0.5f * hbit) * Mathf.PI * 2f / n;
                float rx = textR.width * (0.30f + (0.42f + 0.25f * hbit) * out1);
                float ry = textR.height * (0.30f + (0.70f + 0.45f * hbit) * out1);
                float sz = textR.height * (0.42f + 0.30f * hbit) * (1f - 0.6f * land);
                GUI.color = new Color(1f, 0.96f, 0.78f, (1f - land) * (0.7f + 0.3f * hbit));
                GUI.DrawTexture(new Rect(c.x + Mathf.Cos(ang) * rx - sz * 0.5f, c.y + Mathf.Sin(ang) * ry - sz * 0.5f, sz, sz), tex, ScaleMode.ScaleToFit, true);
            }
            GUI.color = Color.white;
        }

        void DrawPokerChain(Rect wrap, float s, bool behind)
        {
            // Front belt only. The far half that would pass behind the card is not drawn.
            if (behind) return;
            if (_pokerChainBreak >= 1f) return;
            if (!_pokerChained && _pokerChainBreak < 0f) return;
            var spr = SpriteCatalog.Chain;
            var tex = spr != null ? spr.texture : null;
            float h = Mathf.Clamp(wrap.height * 0.11f, 16f * s, 28f * s);
            // Belt: slightly below mid, like pants on hips.
            float midY = wrap.y + wrap.height * 0.58f;
            var back = new Rect(wrap.x - wrap.width * 0.10f, midY - h * 0.42f, wrap.width * 1.20f, h * 0.84f);
            var front = new Rect(wrap.x, midY - h * 0.50f, wrap.width, h);
            if (_pokerChainBreak < 0f)
            {
                DrawChainAround(tex, wrap, back, front, behind);
                return;
            }
            if (behind) return;
            float u = Mathf.Clamp01(_pokerChainBreak);
            float taut = Mathf.Clamp01(u / 0.18f);
            float flyU = Mathf.Clamp01((u - 0.16f) / 0.84f);
            flyU = flyU * flyU;
            // Taut flash before the snap.
            if (taut < 1f)
            {
                float pulse = 1f + 0.12f * Mathf.Sin(taut * Mathf.PI);
                var tautR = new Rect(
                    front.center.x - front.width * 0.5f * pulse,
                    front.center.y - front.height * 0.5f * pulse,
                    front.width * pulse, front.height * pulse);
                GUI.color = new Color(1f, 0.92f, 0.55f, 1f);
                if (tex != null) GUI.DrawTextureWithTexCoords(tautR, tex, new Rect(0.20f, 0f, 0.60f, 1f));
                var glow = GlowTex();
                GUI.color = new Color(1f, 0.86f, 0.35f, 0.55f * Mathf.Sin(taut * Mathf.PI));
                float gsz = h * (1.6f + 1.8f * taut);
                GUI.DrawTexture(new Rect(front.center.x - gsz * 0.5f, front.center.y - gsz * 0.5f, gsz, gsz), glow, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
                if (flyU <= 0f) return;
            }
            // Four tumbling shards + lock drop.
            float grav = flyU * flyU * 140f * s;
            DrawChainShard(tex, front, 0.00f, 0.28f, -1.15f, -0.15f, -48f, grav * 0.85f, flyU, s);
            DrawChainShard(tex, front, 0.22f, 0.28f, -0.45f, 0.25f, -22f, grav * 1.05f, flyU, s);
            DrawChainShard(tex, front, 0.50f, 0.28f, 0.50f, 0.20f, 26f, grav * 1.10f, flyU, s);
            DrawChainShard(tex, front, 0.72f, 0.28f, 1.20f, -0.10f, 52f, grav * 0.90f, flyU, s);
            // Spark burst at the snap.
            var spark = SpriteCatalog.Sparkle;
            var sp = spark != null && spark.texture != null ? spark.texture : GlowTex();
            float burst = Mathf.Sin(Mathf.Clamp01(flyU * 2.2f) * Mathf.PI);
            if (burst > 0.02f)
            {
                for (int k = 0; k < 6; k++)
                {
                    float ang = k * 1.047f + flyU * 2.4f;
                    float dist = (18f + k * 10f) * s * (0.35f + flyU);
                    float sz = h * (0.55f - 0.28f * flyU);
                    GUI.color = new Color(1f, 0.92f, 0.55f, 0.85f * burst);
                    GUI.DrawTexture(new Rect(
                        front.center.x + Mathf.Cos(ang) * dist - sz * 0.5f,
                        front.center.y + Mathf.Sin(ang) * dist - sz * 0.5f,
                        sz, sz), sp, ScaleMode.ScaleToFit, true);
                }
                GUI.color = Color.white;
            }
        }

        static void DrawChainAround(Texture tex, Rect card, Rect back, Rect front, bool behind)
        {
            float beltH = front.height;
            float hipY = front.center.y;
            float sag = card.height * 0.075f;
            float sagY = hipY + sag;
            if (tex == null)
            {
                GUI.color = new Color(0.72f, 0.55f, 0.16f, behind ? 0.55f : 0.95f);
                GUI.DrawTexture(behind ? back : front, Texture2D.whiteTexture);
                GUI.color = Color.white;
                return;
            }
            if (behind)
            {
                // Hip loops peek past the left/right edges from behind.
                GUI.color = new Color(0.70f, 0.62f, 0.38f, 1f);
                GUI.DrawTexture(back, tex, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
                return;
            }
            GUI.color = Color.white;
            var prev = GUI.matrix;
            float hipW = beltH * 0.72f;
            float hipH = beltH * 0.95f;
            // Side rails hug the card edges like a belt on hips.
            var leftHip = new Rect(card.x - hipW * 0.42f, hipY - hipH * 0.50f, hipW, hipH);
            GUIUtility.RotateAroundPivot(-12f, new Vector2(card.x, hipY));
            GUI.DrawTextureWithTexCoords(leftHip, tex, new Rect(0.02f, 0f, 0.16f, 1f));
            GUI.matrix = prev;
            var rightHip = new Rect(card.xMax - hipW * 0.58f, hipY - hipH * 0.50f, hipW, hipH);
            GUIUtility.RotateAroundPivot(12f, new Vector2(card.xMax, hipY));
            GUI.DrawTextureWithTexCoords(rightHip, tex, new Rect(0.82f, 0f, 0.16f, 1f));
            GUI.matrix = prev;
            // Front belt: two rails from each hip down to the sagged center.
            float half = card.width * 0.52f;
            float dipAng = Mathf.Atan2(sag, card.width * 0.50f) * Mathf.Rad2Deg;
            var leftBelt = new Rect(card.x, hipY - beltH * 0.50f, half, beltH);
            GUIUtility.RotateAroundPivot(dipAng, new Vector2(card.x, hipY));
            GUI.DrawTextureWithTexCoords(leftBelt, tex, new Rect(0.02f, 0f, 0.18f, 1f));
            GUI.matrix = prev;
            var rightBelt = new Rect(card.xMax - half, hipY - beltH * 0.50f, half, beltH);
            GUIUtility.RotateAroundPivot(-dipAng, new Vector2(card.xMax, hipY));
            GUI.DrawTextureWithTexCoords(rightBelt, tex, new Rect(0.80f, 0f, 0.18f, 1f));
            GUI.matrix = prev;
            // Lock sits on the sagged front at card center.
            float lockH = Mathf.Clamp(card.height * 0.16f, beltH * 1.05f, card.height * 0.20f);
            DrawPadlock(card, lockH, card.center.x, sagY - lockH * 0.28f);
        }

        static void DrawPadlock(Rect card, float chainH, float hangX = -1f, float hangY = -1f)
        {
            var spr = SpriteCatalog.Padlock;
            if (spr == null || spr.texture == null) return;
            float lockH = Mathf.Clamp(card.height * 0.16f, chainH * 0.85f, card.height * 0.20f);
            float lockW = lockH * 1.38f;
            float x = hangX >= 0f ? hangX : card.center.x;
            float y = hangY >= 0f ? hangY : card.y + card.height * 0.58f - lockH * 0.25f;
            var lr = new Rect(x - lockW * 0.5f, y, lockW, lockH);
            DrawSprite(lr, spr, true);
        }

        static void DrawChainShard(Texture tex, Rect chain, float u0, float uW, float dirX, float dirY, float spin, float drop, float fly, float s)
        {
            if (tex == null) return;
            float w = chain.width * uW;
            var shard = new Rect(chain.x + chain.width * u0, chain.y, w, chain.height);
            shard.x += dirX * fly * 110f * s;
            shard.y += dirY * fly * 36f * s + drop;
            float a = 1f - fly * 0.72f;
            var prev = GUI.matrix;
            GUIUtility.RotateAroundPivot(spin * fly, shard.center);
            GUI.color = new Color(1f, 1f, 1f, a);
            GUI.DrawTexture(shard, tex, ScaleMode.ScaleToFit, true);
            GUI.matrix = prev;
            GUI.color = Color.white;
        }

        // Classic-row seat for card i — shared by cards, discard bursts and the dealer hand.
        static Rect PokerSeat(Rect row, float cardW, float gap, int i) =>
            new Rect(row.x + i * (cardW + gap), row.y, cardW, row.height);

        void DrawPokerFlames(Rect row, float cardW, float gap, float s)
        {
            if (!GuiPaint()) return;
            if (_pokerMotion != PokerMotion.Draw) return;
            if (_pokerMotionT < _pokerPluckEnd || _pokerMotionT > _pokerReplaceEnd + 0.18f) return;
            float cardH = row.height;
            var glow = GlowTex();
            int slot = 0;
            for (int i = 0; i < BirdPoker.HandSize; i++)
            {
                if (!_pokerRedraw[i]) continue;
                float at = _pokerPluckEnd + slot * 0.12f;
                float u = Mathf.Clamp01((_pokerMotionT - at) / 0.72f);
                slot++;
                if (u < 0.55f || u > 1.08f) continue;
                float pop = PokerCardAnim.DiscardPopLinear(u);
                Rect r;
                float yaw, roll;
                bool showFace, sparkle;
                var face = _pokerPrev[i];
                PokerPose(i, PokerSeat(row, cardW, gap, i), row, out r, out yaw, out roll, out showFace, out face, out sparkle);
                Vector2 c = r.center;
                var pip = PokerPip(face.Wild ? BirdColor.Gold : face.Color);
                const int bits = 16;
                var prev = GUI.matrix;
                for (int b = 0; b < bits; b++)
                {
                    float hbit = (b * 17 + i * 31) * 0.137f;
                    hbit = hbit - Mathf.Floor(hbit);
                    float ang = hbit * Mathf.PI * 2f + i * 0.4f + b * 0.11f;
                    float speed = 90f + 160f * hbit;
                    float grav = 160f * pop * pop;
                    float px = c.x + Mathf.Cos(ang) * speed * pop * s;
                    float py = c.y + Mathf.Sin(ang) * speed * 0.78f * pop * s + grav * s;
                    float sz = Mathf.Lerp(cardH * 0.20f, cardH * 0.045f, pop) * (0.50f + 0.85f * hbit);
                    float spin = (b % 2 == 0 ? 260f : -210f) * pop;
                    float a = (1f - pop) * (0.62f + 0.38f * Mathf.Sin(pop * Mathf.PI));
                    if (a < 0.02f) continue;
                    bool ribbon = (b % 4) == 0;
                    var bit = ribbon
                        ? new Rect(px - sz * 0.85f, py - sz * 0.22f, sz * 1.7f, sz * 0.44f)
                        : new Rect(px - sz * 0.5f, py - sz * 0.5f, sz, sz);
                    GUI.matrix = prev;
                    GUIUtility.RotateAroundPivot(spin, bit.center);
                    bool paper = (b % 5) != 0;
                    if (paper)
                    {
                        GUI.color = Color.Lerp(new Color(0.99f, 0.96f, 0.88f, a), new Color(0.28f, 0.20f, 0.12f, a * 0.7f), pop);
                        GUI.DrawTexture(bit, Texture2D.whiteTexture);
                        GUI.color = new Color(pip.r, pip.g, pip.b, a * 0.88f);
                        GUI.DrawTexture(new Rect(bit.x + bit.width * 0.16f, bit.y + bit.height * 0.16f, bit.width * 0.68f, bit.height * 0.68f), Texture2D.whiteTexture);
                    }
                    else if (glow != null)
                    {
                        GUI.color = new Color(1f, 0.86f, 0.42f, a * 0.70f);
                        GUI.DrawTexture(bit, glow, ScaleMode.ScaleToFit, true);
                    }
                }
                GUI.matrix = prev;
                GUI.color = Color.white;
            }
        }

        // Shared hand/arm fade for the poker hands (holding fan hand and dealing hand), so palm,
        // thumb and the faded forearm always leave together. Returns alpha; enter ramps 0→1 over
        // inT from inAt, exit ramps 1→0 over outT and reaches 0 exactly at goneAt.
        const float PokerFanHandGoneT = 0.22f;
        const float PokerDealHandOutT = 0.12f;
        static float PokerHandFade(float t, float inAt, float inT, float goneAt, float outT, out float enter, out float exit)
        {
            enter = inT > 0f ? Mathf.Clamp01((t - inAt) / inT) : (t >= inAt ? 1f : 0f);
            exit = outT > 0f ? 1f - Mathf.Clamp01((t - (goneAt - outT)) / outT) : (t < goneAt ? 1f : 0f);
            return enter * exit;
        }

        // Redeal timing shared by the inbound cards, the slap SFX and the dealer hand.
        const float DrawDealStagger = 0.15f;
        const float DrawInboundT = 0.26f;
        const float DrawInboundHitU = 0.62f;

        // Dealer hand x-slot at time t: glides to the next discarded seat just before that card
        // is thrown (it used to teleport), with a small flick up on each throw.
        float PokerDealHandSlot(float t, out float flick)
        {
            flick = 0f;
            float slotX = -1f;
            int slot = 0;
            int prevI = -1;
            for (int i = 0; i < BirdPoker.HandSize; i++)
            {
                if (!_pokerRedraw[i]) continue;
                float at = _pokerReplaceEnd + slot * DrawDealStagger;
                if (prevI < 0) slotX = i;
                else
                {
                    const float glide = 0.10f;
                    float g = Mathf.Clamp01((t - (at - glide)) / glide);
                    slotX = Mathf.Lerp(slotX, i, g * g * (3f - 2f * g));
                }
                float f = Mathf.Clamp01((t - at) / 0.14f);
                if (f > 0f && f < 1f) flick = Mathf.Sin(f * Mathf.PI);
                prevI = i;
                slot++;
            }
            return slotX;
        }

        void DrawPokerDealHand(Rect row, float cardW, float gap, float s, bool front)
        {
            if (!GuiPaint()) return;
            if (_pokerMotion != PokerMotion.Draw) return;
            if (_pokerMotionT < _pokerReplaceEnd || _pokerMotionT > _pokerRowEnd) return;
            var palm = SpriteCatalog.HandPalm;
            var thumb = SpriteCatalog.HandThumb;
            if (palm == null || thumb == null) return;
            int n = 0;
            for (int i = 0; i < BirdPoker.HandSize; i++)
                if (_pokerRedraw[i]) n++;
            if (n == 0) return;
            float slotX = PokerDealHandSlot(_pokerMotionT, out float flick);
            // The hand throws the last card and leaves: fully gone (arm included) by the time
            // that card lands, instead of hanging over the row until it had settled.
            float lastThrow = _pokerReplaceEnd + (n - 1) * DrawDealStagger;
            float gone = lastThrow + DrawInboundT * DrawInboundHitU - 0.01f;
            // A single redraw throws as the hand arrives: let it reach full alpha before leaving.
            gone = Mathf.Max(gone, _pokerReplaceEnd + 0.16f + PokerDealHandOutT);
            float a = PokerHandFade(_pokerMotionT, _pokerReplaceEnd, 0.16f, gone, PokerDealHandOutT, out float enter, out float exit);
            if (a < 0.02f) return;
            var seat = new Rect(row.x + slotX * (cardW + gap), row.y, cardW, row.height);
            float h = seat.height * 1.88f;
            float w = h * PokerFanHandAspect;
            float slide = (1f - enter) * w * 0.62f + (1f - exit) * w * 0.40f;
            float lift = flick * seat.height * 0.05f;
            var pinch = new Vector2(
                seat.center.x - seat.width * 0.08f - slide + _pokerKick.x,
                seat.yMax - seat.height * 0.02f - lift + _pokerKick.y);
            var dest = new Rect(pinch.x - w * PokerFanPinchU, pinch.y - h * PokerFanPinchV, w, h);
            var prev = GUI.matrix;
            GUIUtility.RotateAroundPivot(-7f * flick, pinch);
            GUI.color = new Color(1f, 1f, 1f, a);
            if (!front)
            {
                // Palm and forearm only, no sleeve tail (the build 49 dealing hand), with the cut
                // end of the forearm dissolving so the arm doesn't float.
                DrawPokerPalm(dest, false, PokerDealArmFade);
            }
            else
                DrawPokerThumb(dest);
            GUI.matrix = prev;
            GUI.color = Color.white;
        }

        void DrawPokerCard(Rect seat, int i, float s, float holdH, Rect rowBox) // holdH reserved for row layout
        {
            bool dealt = BirdPoker.PhaseNow == BirdPoker.Phase.Dealt;
            Rect r;
            float yaw, roll;
            bool showFace, sparkle;
            var face = BirdPoker.Hand[i];
            PokerPose(i, seat, rowBox, out r, out yaw, out roll, out showFace, out face, out sparkle);
            _pokerDrawnR[i] = new Rect(r.x - _pokerKick.x, r.y - _pokerKick.y, r.width, r.height);
            bool fanCard = dealt && _pokerFan && !PokerCardLifted(i) && _pokerHoldSlide[i] < 0.40f;
            _pokerPoseRoll[i] = roll;
            if (fanCard)
                _pokerPoseR[i] = new Rect(r.x - r.width * 0.03f, r.y - r.height * 0.16f, r.width * 1.06f, r.height * 1.16f);
            else
                _pokerPoseR[i] = r;
            // Pose stays current for HitPokerCards. Textures only on Repaint.
            if (!GuiPaint()) return;
            // Discard pop ends at scale 0 — skip the draw so no speck remains until redeal.
            if (r.width < 0.5f || r.height < 0.5f) return;

            bool kept = _pokerMotion == PokerMotion.Draw && _pokerKept[i];
            bool held = (BirdPoker.Hold[i] || kept) && (_pokerHoldSlide[i] > 0.45f || kept);
            bool chainLive = _pokerChainBreak < 1f
                && (dealt || kept || (_pokerMotion == PokerMotion.Draw && _pokerChainBreak >= 0f));

            // Abs so a Y-flip never mirrors the face (that reads as upside-down).
            float sx = Mathf.Abs(Mathf.Cos(yaw * Mathf.Deg2Rad));
            if (sx < 0.07f) sx = 0.07f;
            var prevM = GUI.matrix;
            Vector2 rollPivot = new Vector2(r.center.x, r.yMax);
            if (Mathf.Abs(roll) > 0.2f)
                GUIUtility.RotateAroundPivot(roll, rollPivot);
            GUIUtility.ScaleAroundPivot(new Vector2(sx, 1f), r.center);
            bool wrapChain = held && showFace && chainLive;
            if (wrapChain) DrawPokerChain(r, s, true);
            if (showFace) DrawPokerFace(r, face, s, sparkle);
            else DrawPokerBack(r);
            if (wrapChain) DrawPokerChain(r, s, false);
            GUI.matrix = prevM;
        }

        static bool PointInRolled(Vector2 p, Rect r, float roll)
        {
            if (r.width < 2f || r.height < 2f) return false;
            Vector2 pivot = new Vector2(r.center.x, r.yMax);
            Vector2 d = p - pivot;
            float rad = -roll * Mathf.Deg2Rad;
            float cs = Mathf.Cos(rad);
            float sn = Mathf.Sin(rad);
            var q = new Vector2(pivot.x + cs * d.x - sn * d.y, pivot.y + sn * d.x + cs * d.y);
            return r.Contains(q);
        }

        void HitPokerCards(bool canHold, Rect rowBox, float rowX, float rowY, float cardW, float cardH, float gap)
        {
            var e = Event.current;
            var mouse = e.mousePosition;
            _pokerHover = -1;
            if (canHold)
            {
                for (int i = BirdPoker.HandSize - 1; i >= 0; i--)
                {
                    if (PokerCardLifted(i)) continue;
                    if (PointInRolled(mouse, _pokerPoseR[i], _pokerPoseRoll[i]))
                    {
                        _pokerHover = i;
                        break;
                    }
                }
            }

            int top = -1;
            if (canHold)
            {
                for (int i = BirdPoker.HandSize - 1; i >= 0; i--)
                {
                    if (!PokerCardLifted(i)) continue;
                    if (PointInRolled(mouse, _pokerPoseR[i], _pokerPoseRoll[i]))
                    { top = i; break; }
                }
                if (top < 0) top = _pokerHover;
            }

            for (int i = 0; i < BirdPoker.HandSize; i++)
            {
                int id = GUIUtility.GetControlID(FocusType.Passive);
                switch (e.GetTypeForControl(id))
                {
                    case EventType.MouseDown:
                        if (canHold && i == top && e.button == 0)
                        {
                            GUIUtility.hotControl = id;
                            e.Use();
                        }
                        break;
                    case EventType.MouseUp:
                        if (GUIUtility.hotControl == id)
                        {
                            GUIUtility.hotControl = 0;
                            e.Use();
                            if (canHold && i == top)
                                ApplyPokerHold(i);
                        }
                        break;
                    case EventType.MouseDrag:
                        if (GUIUtility.hotControl == id) e.Use();
                        break;
                }
            }
        }

        void ApplyPokerHold(int i)
        {
            _pokerHoldFrom[i] = _pokerPoseR[i].width > 2f ? _pokerPoseR[i] : _pokerHoldFrom[i];
            bool was = BirdPoker.Hold[i];
            BirdPoker.ToggleHold(i);
            if (BirdPoker.Hold[i] == was) return;
            _pokerKeepHint = false;
            bool any = false;
            for (int k = 0; k < BirdPoker.HandSize; k++)
                if (BirdPoker.Hold[k]) any = true;
            _pokerChained = any;
            if (!any) _pokerChainBreak = -1f;
            if (BirdPoker.Hold[i]) Sfx.ChainLock();
            else Sfx.CardTap();
        }

        void DrawPokerKeepHint(Rect row, float s)
        {
            if (_pokerPageOn) return;
            if (!_pokerKeepHint) return;
            if (!_pokerFan || BirdPoker.PhaseNow != BirdPoker.Phase.Dealt) return;
            if (_pokerMotion != PokerMotion.None) return;
            // Same outlined BET style, one line.
            // Boosted by the shared red status-text size (PokerStatusTextBoost). The plate's
            // bottom edge stays at fan.y - 28s, so the larger line grows upward, off the cards.
            int type = Mathf.RoundToInt(Mathf.Clamp(28f * s, 26f, 40f) * PokerStatusTextBoost);
            var st = PokerBold(ref _pokerLed, TextAnchor.MiddleCenter);
            st.fontSize = type;
            float cap = type * 1.40f;
            var fan = PokerFanRestAt(0, BirdPoker.HandSize, row, 0f);
            float y = fan.y - cap - 28f * s;
            if (y < 44f * s) y = 44f * s;
            var fill = new Color(0.94f, 0.12f, 0.14f, 1f);
            StampLight(new Rect(0f, y, Screen.width, cap), "TAP A CARD TO KEEP", st, fill);
        }

        static void DrawPokerKeptMark(Rect r, float s)
        {
            float t = Mathf.Max(2f, 2.2f * s);
            var gold = new Color(0.86f, 0.68f, 0.22f, 0.92f);
            GUI.color = gold;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.yMax - t, r.width, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.y, t, r.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.xMax - t, r.y, t, r.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var leaf = SpriteCatalog.Leaf;
            if (leaf == null || leaf.texture == null) return;
            float w = r.width * 0.38f;
            float h = w * 1.55f;
            var pin = new Rect(r.center.x - w * 0.5f, r.y - h * 0.42f, w, h);
            GUI.DrawTexture(pin, leaf.texture, ScaleMode.ScaleToFit, true);
        }

        static void DrawPokerBack(Rect r)
        {
            var spr = SpriteCatalog.CardBack;
            if (spr != null && spr.texture != null)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(r, spr.texture, ScaleMode.ScaleToFit, true);
                return;
            }
            GUI.color = new Color(0.62f, 0.12f, 0.14f, 1f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        static void DrawCardPaper(Rect inner, bool wild, float alpha = 1f)
        {
            var paper = SpriteCatalog.CardPaper;
            if (paper != null && paper.texture != null)
            {
                GUI.color = wild
                    ? new Color(1f, 0.90f, 0.58f, alpha)
                    : new Color(1f, 1f, 1f, alpha);
                GUI.DrawTexture(inner, paper.texture, ScaleMode.ScaleAndCrop, true);
                GUI.color = Color.white;
                return;
            }
            GUI.color = wild
                ? new Color(1f, 0.94f, 0.78f, alpha)
                : new Color(0.99f, 0.96f, 0.90f, alpha);
            GUI.DrawTexture(inner, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        static readonly Vector2[] WildGlints =
        {
            new Vector2(0.12f, 0.10f),
            new Vector2(0.88f, 0.12f),
            new Vector2(0.08f, 0.46f),
            new Vector2(0.92f, 0.50f),
            new Vector2(0.16f, 0.88f),
            new Vector2(0.84f, 0.86f),
            new Vector2(0.50f, 0.07f),
            new Vector2(0.74f, 0.28f)
        };

        static void DrawPokerFace(Rect r, BirdPoker.Card card, float s, bool sparkle = true)
        {
            // Singular sprite: cream + bird + pip already one texture.
            // Never SpriteCatalog.Bird / ScaleToFit bird-on-face here.
            var fused = SpriteCatalog.PokerFace(card);
            if (fused == null || fused.texture == null) return;
            GUI.color = Color.white;
            GUI.DrawTexture(r, fused.texture, ScaleMode.StretchToFill, false);
            // WILD lettering + ribbon are baked into fx_poker_face_wild; only the foil moves.
            if (card.Wild && sparkle) DrawWildFoil(r, r.x * 0.013f);
        }

        static void DrawWildFoil(Rect r, float phase)
        {
            if (!GuiPaint()) return;
            // Holographic foil: three baked hue-shifted rainbow layers (fx_poker_face_wild_foil0..2,
            // masked to the gold frame, stars, ribbon and WILD lettering) crossfade so the rainbow
            // drifts across the card; paper and jester stay clean. cos^2 weights over three phases
            // sum to a constant, so overall foil strength never pulses.
            float t = Time.unscaledTime;
            float cyc = Mathf.Repeat(t * 0.28f + phase, 1f);
            for (int i = 0; i < 3; i++)
            {
                var foil = SpriteCatalog.PokerWildFoil(i);
                if (foil == null || foil.texture == null) continue;
                float w = 0.5f + 0.5f * Mathf.Cos((cyc - i / 3f) * Mathf.PI * 2f);
                w *= w;
                if (w < 0.02f) continue;
                GUI.color = new Color(1f, 1f, 1f, 0.55f * w);
                GUI.DrawTexture(r, foil.texture, ScaleMode.StretchToFill, true);
            }

            // Glint band sweeping across, then a pause.
            var glow = GlowTex();
            float u = Mathf.Repeat(t * 0.36f + phase, 1.75f);
            if (u < 1f)
            {
                GUI.BeginGroup(r);
                float fade = Mathf.Sin(u * Mathf.PI);
                float x = r.width * (u * 1.25f - 0.22f);
                GUI.color = new Color(1f, 0.98f, 0.90f, 0.30f * fade);
                GUI.DrawTexture(new Rect(x, r.height * 0.02f, r.width * 0.30f, r.height * 0.96f), glow, ScaleMode.ScaleToFit, true);
                GUI.EndGroup();
            }

            // Twinkles around the frame.
            var spark = SpriteCatalog.Sparkle;
            var tex = spark != null && spark.texture != null ? spark.texture : glow;
            float baseSz = r.width * 0.16f;
            for (int i = 0; i < WildGlints.Length; i++)
            {
                float tw = Mathf.Max(0f, Mathf.Sin(t * 2.4f + phase * 6f + i * 1.17f));
                tw *= tw;
                if (tw < 0.10f) continue;
                var uv = WildGlints[i];
                float sz = baseSz * (0.45f + 0.85f * tw);
                GUI.color = new Color(1f, 0.97f, 0.85f, 0.30f + 0.55f * tw);
                GUI.DrawTexture(new Rect(r.x + r.width * uv.x - sz * 0.5f, r.y + r.height * uv.y - sz * 0.5f, sz, sz), tex, ScaleMode.ScaleToFit, true);
            }
            GUI.color = Color.white;
        }

        static Color PokerPip(BirdColor c)
        {
            switch (c)
            {
                case BirdColor.Ruby: return new Color(0.77f, 0.16f, 0.19f);
                case BirdColor.Gold: return new Color(0.83f, 0.62f, 0.19f);
                case BirdColor.Teal: return new Color(0.14f, 0.58f, 0.58f);
                case BirdColor.Violet: return new Color(0.52f, 0.28f, 0.69f);
                default: return new Color(0.86f, 0.44f, 0.35f);
            }
        }

        void DrawPokerBetBar(Rect slot, float s, bool busy, bool steppers)
        {
            // Compact stepper: [−] $xx [+] aligned with DEAL's ochre disc.
            // BET $XXX type stays fixed; chip size is independent so shrinking +/− does not shrink the label.
            float side = Mathf.Clamp(30f * s, 26f, 36f * s);
            float gap = 6f * s;
            float betW = Mathf.Clamp(slot.width - side * 2f - gap * 2f, 110f * s, 240f * s);
            float clusterW = side + gap + betW + gap + side;
            if (clusterW > slot.width)
            {
                betW = Mathf.Max(80f * s, slot.width - side * 2f - gap * 2f);
                clusterW = side + gap + betW + gap + side;
            }
            int type = Mathf.RoundToInt(17.5f * s);
            float amountH = Mathf.Max(side, type * 1.35f);
            float x0 = slot.x;
            float midY = slot.y + slot.height * 0.29f;
            var minus = new Rect(x0, midY - side * 0.5f, side, side);
            var amount = new Rect(minus.xMax + gap, midY - amountH * 0.5f, betW, amountH);
            var plus = new Rect(amount.xMax + gap, minus.y, side, side);
            if (plus.xMax > slot.xMax)
                plus.x = slot.xMax - plus.width;
            _pokerMinusR = minus;
            _pokerPlusR = plus;

            var led = PokerBold(ref _pokerLed, TextAnchor.MiddleCenter);
            led.fontSize = type;
            StampLight(amount, PokerBetText(), led, PokerLabelGold);

            bool live = steppers && !busy;
            if (DrawBetStepper(minus, "−", live && BirdPoker.CanNudge(-1)) && live)
            {
                if (BirdPoker.NudgeBet(-1)) Sfx.BetDown();
                else Sfx.Deny();
            }
            if (DrawBetStepper(plus, "+", live && BirdPoker.CanNudge(1)) && live)
            {
                if (BirdPoker.NudgeBet(1)) Sfx.BetUp();
                else Sfx.Deny();
            }
        }

        bool DrawBetStepper(Rect r, string glyph, bool on)
        {
            bool fire = HitPad(r, out bool held);
            float sink = held && on ? 2f : 0f;
            // Keep the drop shadow inside the hit rect so the chip does not clip the DEAL disc or screen edge.
            float shadow = 2f;
            var chip = new Rect(r.x, r.y + sink, r.width - shadow, r.height - sink - shadow);
            float a = on ? 1f : 0.38f;
            if (GuiPaint())
            {
                GUI.color = new Color(0.10f, 0.06f, 0.03f, 0.40f * a);
                GUI.DrawTexture(new Rect(chip.x + shadow, chip.y + shadow, chip.width, chip.height), Texture2D.whiteTexture);
                GUI.color = new Color(0.78f, 0.58f, 0.18f, (held ? 0.98f : 0.92f) * a);
                GUI.DrawTexture(chip, Texture2D.whiteTexture);
                float inset = Mathf.Max(2f, chip.width * 0.10f);
                GUI.color = new Color(0.96f, 0.90f, 0.62f, (held ? 1f : 0.94f) * a);
                GUI.DrawTexture(new Rect(chip.x + inset, chip.y + inset, chip.width - inset * 2f, chip.height - inset * 2f), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            var st = PokerBold(ref _pokerStep, TextAnchor.MiddleCenter);
            st.fontSize = Mathf.RoundToInt(chip.height * 0.56f);
            StampLight(chip, glyph, st, new Color(0.36f, 0.18f, 0.07f, a));
            return fire;
        }

        bool DrawFloralBtn(Rect r, string label, float s) => DrawFloralBtn(r, label, s, false);

        static Vector2 DealPlatformAim(Rect actR)
        {
            var disc = FlowerDisc(actR, 0f);
            if (disc.width < 2f || disc.height < 2f) return actR.center;
            return GloveTarget(disc);
        }

        bool GlovePresses(Rect disc)
        {
            if (!_gloveVis || _coachFade < 0.2f || _gloveDip < 0.55f) return false;
            return disc.width > 2f && disc.Contains(_cueAimGui);
        }

        bool DrawFloralBtn(Rect r, string label, float s, bool hero)
        {
            bool fire = HitPad(r, out bool held);
            var pressDisc = FlowerDisc(r, 0f);
            if (!held && GlovePresses(pressDisc)) held = true;
            float sink = held ? r.height * 0.04f : 0f;
            if (GuiPaint())
            {
                var flower = SpriteCatalog.PlayFlower;
                var tex = flower != null ? flower.texture : null;
                if (tex != null)
                {
                    GUI.color = new Color(0.10f, 0.06f, 0.03f, 0.40f);
                    GUI.DrawTexture(new Rect(r.x + 4f, r.y + 10f, r.width, r.height), tex, ScaleMode.ScaleToFit, true);
                    if (!held) DrawFlowerHalo(r, 0f);
                    GUI.color = Color.white;
                    GUI.DrawTexture(new Rect(r.x, r.y + sink, r.width, r.height), tex, ScaleMode.ScaleToFit, true);
                    if (!held) DrawFlowerShimmer(r, sink);
                    if (held) DrawDiscPress(r, sink);
                }
                else
                {
                    GUI.color = new Color(0.78f, 0.58f, 0.26f, held ? 0.95f : 0.82f);
                    GUI.DrawTexture(r, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
            }
            var disc = FlowerDisc(r, sink);
            var st = PokerBold(ref _pokerHero, TextAnchor.MiddleCenter);
            int lo = hero ? 22 : 14;
            int hi = hero ? 48 : 28;
            float maxW = disc.width * (hero ? 0.92f : 0.84f);
            float maxH = disc.height * (hero ? 0.72f : 0.62f);
            int heroKey = hero ? 17 : 3;
            if (!string.IsNullOrEmpty(label))
            {
                for (int i = 0; i < label.Length; i++)
                    heroKey = heroKey * 33 + label[i];
            }
            heroKey ^= Mathf.RoundToInt(maxW) * 13;
            heroKey ^= Mathf.RoundToInt(maxH) << 10;
            if (heroKey == int.MinValue) heroKey = -2;
            if (heroKey != _pokerHeroKey)
            {
                _pokerHeroKey = heroKey;
                _pokerHeroPx = FitFont(st, label, maxW, maxH, lo, hi);
            }
            st.fontSize = _pokerHeroPx;
            StampLight(disc, label, st, new Color(0.36f, 0.18f, 0.07f));
            return fire;
        }

        // Compact header. The hive icon and "Collection" title ride the Back medal's row
        // (right of it), and the instruction text sits in one plate right under that row,
        // so the card grid gets the freed height. Returns the bottom of the text plate.
        float DrawAlbumHeader(float s, Rect back)
        {
            int floor = Mathf.Max(16, Mathf.RoundToInt(17f * s));
            float left = Mathf.Max(16f * s, Screen.safeArea.xMin + 10f);
            float textRight = Screen.width - Mathf.Max(16f * s, Screen.width - Screen.safeArea.xMax + 12f);
            float hiveSize = Mathf.Clamp(back.height * 0.80f, 44f, 92f);
            var hiveHead = new Rect(back.xMax + 12f * s, back.center.y - hiveSize * 0.5f, hiveSize, hiveSize);
            float textX = hiveHead.xMax + 12f * s;
            float textW = Mathf.Max(48f, textRight - textX);
            var titleSt = GuiPool.Label(GuiSlot.AlbumTitle);
            titleSt.fontStyle = FontStyle.Bold;
            titleSt.alignment = TextAnchor.MiddleLeft;
            titleSt.wordWrap = false;
            int titlePx = Mathf.Max(floor + 8, Mathf.RoundToInt(30f * s));
            float titleH = titlePx + 8f;
            var titleR = new Rect(textX, hiveHead.center.y - titleH * 0.5f, textW, titleH);
            titleSt.fontSize = FitFont(titleSt, HiveTitle, titleR.width, titleR.height * 0.9f, floor, titlePx);

            // Two short lines, always. Content rects go to DrawCoachPanel (shared pad);
            // do not pre-inflate or the plate overlaps the Back medal.
            string body = HiveSubtitle + "\n" + HiveHowTo;
            var bodySt = GuiPool.Label(GuiSlot.AlbumBody);
            bodySt.fontStyle = FontStyle.Bold;
            bodySt.alignment = TextAnchor.UpperLeft;
            bodySt.wordWrap = true;
            int bodyPx = Mathf.Max(floor - 1, Mathf.RoundToInt(17f * s));
            if (bodyPx < 15) bodyPx = 15;
            float bodyW = Mathf.Max(48f, textRight - left);
            bodySt.fontSize = bodyPx;
            float bodyH = Mathf.Max(bodyPx + 6f, bodySt.CalcHeight(GuiPool.Text(GuiText.AlbumBody, body), bodyW) + 2f);
            float bodyY = Mathf.Max(hiveHead.yMax, CaptionBelowBack(s, TopHud()));
            var bodyR = new Rect(left, bodyY, bodyW, bodyH);

            var titlePlate = new Rect(hiveHead.x, hiveHead.y, Mathf.Max(48f, textRight - hiveHead.x), hiveHead.height);
            DrawCoachPanel(titlePlate, 1f);
            DrawCoachPanel(bodyR, 1f);
            DrawHiveButton(hiveHead, s);
            StampReadable(titleR, HiveTitle, titleSt, AlbumInk);
            GUI.color = new Color(1f, 0.84f, 0.38f, 0.92f);
            float ruleW = Mathf.Min(textW * 0.46f, 140f * s);
            GUI.DrawTexture(new Rect(titleR.x, titleR.yMax - 1f, ruleW, Mathf.Max(2f, 3f * Mathf.Min(s, 1.35f))), Texture2D.whiteTexture);
            GUI.color = Color.white;
            StampReadable(bodyR, body, bodySt, AlbumInk);
            return CoachPanelRect(bodyR).yMax + 4f * s;
        }

        void NoteAlbumOpen()
        {
            var e = Event.current;
            if (e == null || e.type != EventType.Repaint) return;
            if (_albumAgeFrame == Time.frameCount) return;
            _albumAgeFrame = Time.frameCount;
            if (_albumOpenAt < 0f) _albumOpenAt = PlayClock.Now;
            if (_albumOpenFrames < 12) _albumOpenFrames++;
        }

        void ResetAlbumOpen()
        {
            _albumOpenAt = -1f;
            _albumOpenFrames = 0;
            _albumAgeFrame = -1;
            _pageSwipe.Tracking = false;
            _pageSwipe.Claimed = false;
            _pageSwipe.Dir = 0;
        }

        bool AlbumPageSettled()
        {
            if (_albumOpenFrames < 2 || _albumOpenAt < 0f) return false;
            if (PlayClock.Now - _albumOpenAt < 0.34f) return false;
            if (_hivePageTurn < 0.55f) return false;
            return true;
        }

        bool AlbumTutorDone()
        {
            if (_albumTutorKnown) return _albumTutorDone;
            _albumTutorKnown = true;
            _albumTutorDone = PlayerPrefs.GetInt(CoachHiveAlbumKey, 0) != 0;
            return _albumTutorDone;
        }

        static int AlbumOwnedOnPage(int page)
        {
            if (page < 0) return -1;
            int start = page * HivePageSize;
            for (int n = 0; n < HivePageSize; n++)
            {
                int slot = start + n;
                if (slot >= Hive.AlbumSlots) break;
                if (Hive.CountOfSlot(slot) > 0) return slot;
            }
            return -1;
        }

        // The card the lesson glove points at on a page: the first card the player owns,
        // or on an empty book the middle sleeve of the grid. -1 when neither exists.
        static int AlbumTutorCardSlot(int page)
        {
            int own = AlbumOwnedOnPage(page);
            if (own >= 0) return own;
            if (Hive.Found > 0 || page < 0) return -1;
            int mid = page * HivePageSize + HivePageSize / 2;
            return mid < Hive.AlbumSlots ? mid : -1;
        }

        static int AlbumOwnedSlot(int preferPage)
        {
            int here = AlbumOwnedOnPage(preferPage);
            if (here >= 0) return here;
            int pages = Mathf.Max(1, (Hive.AlbumSlots + HivePageSize - 1) / HivePageSize);
            for (int p = 0; p < pages; p++)
            {
                if (p == preferPage) continue;
                int slot = AlbumOwnedOnPage(p);
                if (slot >= 0) return slot;
            }
            return -1;
        }

        bool TurnHivePage(int page)
        {
            int pages = Mathf.Max(1, (Hive.AlbumSlots + HivePageSize - 1) / HivePageSize);
            if (_hivePageTurn < 0.55f) return false;
            if (page < 0 || page >= pages || page == _hivePage) return false;
            ClearHivePageFresh(_hivePage);
            _hivePageFrom = _hivePage;
            _hivePage = page;
            _hivePageTurn = 0f;
            _hiveFlip = -1;
            Sfx.PageTurn();
            if (_albumTutorOn && _albumTutorStep >= AlbumStepPage)
                FinishAlbumTutor();
            return true;
        }

        bool AlbumInspectReady()
        {
            return _hiveInspect >= 0 && !_hiveInspectClosing && _hiveInspectT >= 0.28f;
        }

        // Steps: 1 enlarge · 2 flip · 3 honeycombs by rarity · 4 page.
        // Upgrade is a separate post-badger tutor (CoachHiveUpgradeKey).
        const int AlbumStepPage = 4;

        static CoachStep AlbumCoachStep(int step)
        {
            if (step >= AlbumStepPage) return CoachStep.AlbumPage;
            if (step == 3) return CoachStep.AlbumHoney;
            if (step == 2) return CoachStep.AlbumFlip;
            return step <= 0 ? CoachStep.AlbumEmpty : CoachStep.AlbumTap;
        }

        string AlbumTutorLine()
        {
            if (_albumTutorStep >= AlbumStepPage) return AlbumPageLine;
            if (_albumTutorStep == 3) return AlbumHoneyLine;
            if (_albumTutorStep == 2) return AlbumFlipLine;
            return Hive.Found <= 0 ? AlbumEmptyLine : AlbumTapLine;
        }

        void BeginAlbumTutor(int step)
        {
            _albumTutorOn = true;
            _albumTutorStep = step;
            _albumTutorFlipped = false;
            _albumTutorAimOk = false;
            _coachFade = 0f;
            _tutorSeatOn = false;
            _cueSpoken = null;
            _cueLine = null;
            GloveVeilReset();
            _gloveVis = false;
            _gloveReady = false;
        }

        void AdvanceAlbumTutor(int step)
        {
            if (_albumTutorStep == step) return;
            if (_albumTutorOn) NoteLessonDismiss(AlbumCoachStep(_albumTutorStep));
            _albumTutorStep = step;
            _tutorSeatOn = false;
            _coachFade = 0f;
            CueLine(AlbumTutorLine());
        }

        void FinishAlbumTutor()
        {
            bool was = _albumTutorOn;
            var step = AlbumCoachStep(_albumTutorStep);
            _albumTutorOn = false;
            _albumTutorStep = 0;
            _albumTutorFlipped = false;
            _albumTutorAimOk = false;
            _albumTutorDone = true;
            _albumTutorKnown = true;
            _gloveVis = false;
            _gloveReady = false;
            if (was) GloveVeilReset();
            if (_cueLine == AlbumTapLine || _cueLine == AlbumFlipLine || _cueLine == AlbumHoneyLine
                || _cueLine == AlbumPageLine || _cueLine == AlbumEmptyLine)
                _cueLine = null;
            _cueSpoken = null;
            if (was) NoteLessonDismiss(step);
#if UNITY_EDITOR
            // Shots must not write the done flags. A later real visit can still teach.
            if (_dailyShotQuiet || _pokerPlayrun || EditorShotLive)
            {
                _albumTutorDone = false;
                _albumTutorKnown = false;
                return;
            }
#endif
            bool save = false;
            if (PlayerPrefs.GetInt(CoachHiveAlbumKey, 0) == 0)
            {
                PlayerPrefs.SetInt(CoachHiveAlbumKey, 1);
                save = true;
            }
            // An empty-book lesson never flipped a card, so the flip lesson stays owed
            // (ArmHiveTutor teaches it on the first card inspected).
            if (Hive.Found > 0 && PlayerPrefs.GetInt(HiveFlipSeenKey, 0) == 0)
            {
                PlayerPrefs.SetInt(HiveFlipSeenKey, 1);
                save = true;
            }
            if (save) PlayerPrefs.Save();
        }

        bool UpgradeTutorDone()
        {
            if (_upgradeTutorKnown) return _upgradeTutorDone;
            _upgradeTutorKnown = true;
            _upgradeTutorDone = PlayerPrefs.GetInt(CoachHiveUpgradeKey, 0) != 0;
            return _upgradeTutorDone;
        }

        void BeginUpgradeTutor()
        {
            _upgradeTutorOn = true;
            _upgradeTutorAimOk = false;
            _coachFade = 0f;
            _tutorSeatOn = false;
            _cueSpoken = null;
            _cueLine = null;
            GloveVeilReset();
            _gloveVis = false;
            _gloveReady = false;
            CueLine(AlbumUpgradeLine);
        }

        void FinishUpgradeTutor()
        {
            bool was = _upgradeTutorOn;
            _upgradeTutorOn = false;
            _upgradeTutorAimOk = false;
            _upgradeTutorDone = true;
            _upgradeTutorKnown = true;
            _gloveVis = false;
            _gloveReady = false;
            if (was) GloveVeilReset();
            if (_cueLine == AlbumUpgradeLine) _cueLine = null;
            _cueSpoken = null;
            if (was) NoteLessonDismiss(CoachStep.AlbumUpgrade);
#if UNITY_EDITOR
            if (_dailyShotQuiet || _pokerPlayrun || EditorShotLive)
            {
                _upgradeTutorDone = false;
                _upgradeTutorKnown = false;
                return;
            }
#endif
            if (PlayerPrefs.GetInt(CoachHiveUpgradeKey, 0) == 0)
            {
                PlayerPrefs.SetInt(CoachHiveUpgradeKey, 1);
                PlayerPrefs.Save();
            }
        }

        void TryArmUpgradeTutor()
        {
            if (_home != HomeFace.Hive) return;
            if (_upgradeTutorOn || UpgradeTutorDone()) return;
            if (!Hive.UpgradesUnlocked) return;
            if (_albumTutorOn || !AlbumTutorDone()) return;
            if (!AlbumPageSettled() || !TutorialGateClear()) return;
            // Needs an open owned inspect so the Upgrade control is on screen.
            if (_hiveInspect < 0 || !AlbumInspectReady()) return;
            if (Hive.CountOfSlot(_hiveInspect) <= 0) return;
            if (!Hive.UpgradeRoom(_hiveInspect)) return;
            BeginUpgradeTutor();
        }

        void TryArmAlbumTutor()
        {
            // Back sets _home before this runs, in the same pass. Do not arm on the way out.
            if (_home != HomeFace.Hive) return;
            if (_albumTutorOn || AlbumTutorDone()) return;
            if (!AlbumPageSettled() || !TutorialGateClear()) return;
            // An empty book still teaches: the glove points at the grid (AlbumEmptyLine).
            if (_hiveInspect >= 0)
            {
                if (!AlbumInspectReady() || Hive.CountOfSlot(_hiveInspect) <= 0) return;
                BeginAlbumTutor(2);
                return;
            }
            if (_albumTutorSlot < 0 || !_albumCardOk)
            {
                if (Hive.Found <= 0) return;
                int other = AlbumOwnedSlot(_hivePage);
                if (other < 0) return;
                int page = other / HivePageSize;
                if (page != _hivePage)
                {
                    _hivePage = page;
                    _hivePageFrom = page;
                    _hivePageTurn = 99f;
                    _albumTutorSlot = other;
                }
                return;
            }
            BeginAlbumTutor(1);
        }

        // Owns the shared glove while the album lesson is up. False when another
        // lesson should keep posing. Caption and glove draw from the album page.
        bool TickAlbumTutor(float dt)
        {
            if (!_albumTutorOn) return false;
            if (_home != HomeFace.Hive)
            {
                _gloveVis = false;
                _albumTutorAimOk = false;
                return false;
            }
            if (!AlbumPageSettled() || !TutorialGateClear())
            {
                _gloveVis = false;
                if (OtherTutorialLive()) return false;
                return true;
            }
            if (_albumTutorStep <= 1 && _hiveInspect >= 0)
            {
                AdvanceAlbumTutor(2);
                _gloveVis = false;
                return true;
            }
            // Flip done → honeycombs-by-rarity step (stay on the open back).
            if (_albumTutorStep == 2 && _albumTutorFlipped)
            {
                AdvanceAlbumTutor(3);
                _gloveVis = false;
                return true;
            }
            if (_albumTutorStep == 2 && _hiveInspect < 0)
            {
                AdvanceAlbumTutor(1);
                _gloveVis = false;
                return true;
            }
            if (_albumTutorStep == 2 && !AlbumInspectReady())
            {
                _gloveVis = false;
                return true;
            }
            // Honey step: any put-away advances to the page lesson.
            if (_albumTutorStep == 3)
            {
                if (_hiveInspect < 0)
                {
                    AdvanceAlbumTutor(AlbumStepPage);
                    _gloveVis = false;
                    return true;
                }
                if (!AlbumInspectReady())
                {
                    _gloveVis = false;
                    return true;
                }
            }
            if (_albumTutorStep >= AlbumStepPage && _hiveInspect >= 0)
            {
                _gloveVis = false;
                return true;
            }
            Vector2 aim = _albumTutorAim;
            Rect tap = default;
            bool aimed = false;
            if (_albumTutorStep >= AlbumStepPage)
            {
                aimed = _albumPagerOk;
                if (aimed)
                {
                    aim = GloveTarget(_albumPagerR);
                    tap = _albumPagerR;
                }
            }
            else if (_albumTutorStep == 3 || _albumTutorStep == 2)
            {
                aimed = _albumInspectOk && AlbumInspectReady();
                if (aimed)
                {
                    aim = GloveTarget(_albumInspectR);
                    tap = _albumInspectR;
                }
            }
            else
            {
                aimed = _albumCardOk && _albumTutorSlot >= 0;
                if (aimed)
                {
                    aim = GloveTarget(_albumCardR);
                    tap = _albumCardR;
                }
            }
            _albumTutorAim = aim;
            _albumTutorAimOk = aimed;
            if (!aimed)
            {
                _gloveVis = false;
                return true;
            }
            if (dt < 0f) dt = 0f;
            CueLine(AlbumTutorLine());
            _coachFade = Mathf.Min(1f, _coachFade + dt / 0.30f);
            float handS = Mathf.Max(Screen.height / 720f, 1f);
            CoachGloveAt(aim, dt, handS, float.NaN, false, float.NaN, tap);
            return true;
        }

        bool TickUpgradeTutor(float dt)
        {
            if (!_upgradeTutorOn) return false;
            if (_home != HomeFace.Hive || !Hive.UpgradesUnlocked)
            {
                _gloveVis = false;
                _upgradeTutorAimOk = false;
                return false;
            }
            if (!AlbumPageSettled() || !TutorialGateClear())
            {
                _gloveVis = false;
                if (OtherTutorialLive()) return false;
                return true;
            }
            if (_hiveInspect < 0 || !AlbumInspectReady() || Hive.CountOfSlot(_hiveInspect) <= 0)
            {
                _gloveVis = false;
                _upgradeTutorAimOk = false;
                return true;
            }
            if (_upgradeBtnR.width < 8f)
            {
                _gloveVis = false;
                _upgradeTutorAimOk = false;
                return true;
            }
            if (dt < 0f) dt = 0f;
            CueLine(AlbumUpgradeLine);
            _coachFade = Mathf.Min(1f, _coachFade + dt / 0.30f);
            var aim = GloveTarget(_upgradeBtnR);
            _upgradeTutorAim = aim;
            _upgradeTutorAimOk = true;
            float handS = Mathf.Max(Screen.height / 720f, 1f);
            CoachGloveAt(aim, dt, handS, float.NaN, false, float.NaN, _upgradeBtnR);
            return true;
        }

        void DrawAlbumTutor(float s)
        {
            if (_upgradeTutorOn)
            {
                DrawUpgradeTutor(s);
                return;
            }
            if (!_albumTutorOn || _home != HomeFace.Hive) return;
            if (!AlbumPageSettled() || !TutorialGateClear()) return;
            if (_albumTutorStep == 2 && (_albumTutorFlipped || !AlbumInspectReady())) return;
            if (_albumTutorStep == 3 && !AlbumInspectReady()) return;
            if (_albumTutorStep >= AlbumStepPage && _hiveInspect >= 0) return;
            if (_albumTutorStep <= 1 && _hiveInspect >= 0) return;
            if (!_albumTutorAimOk) return;
            string line = AlbumTutorLine();
            if (string.IsNullOrEmpty(line)) return;
            DrawAlbumCoachLine(line, s, _albumTutorStep);
            DrawTutorOverlay(s);
        }

        void DrawUpgradeTutor(float s)
        {
            if (!_upgradeTutorOn || _home != HomeFace.Hive) return;
            if (!AlbumPageSettled() || !TutorialGateClear()) return;
            if (!_upgradeTutorAimOk || _upgradeBtnR.width < 8f) return;
            DrawAlbumCoachLine(AlbumUpgradeLine, s, -1);
            DrawTutorOverlay(s);
        }

        // Shared Collection caption: StandardCaptionBox + PaintCoachCaption + Back clearance.
        void DrawAlbumCoachLine(string line, float s, int step)
        {
            if (string.IsNullOrEmpty(line)) return;
            StandardCaptionBox(s, out float stdW, out float stdH, out _, out _);
            float w = stdW;
            float h = line.IndexOf('\n') >= 0 ? stdH * 0.72f : stdH * 0.48f;
            float top = TopHud();
            float minY = CaptionBelowBack(s, top);
            float y = minY;
            if (step < 0 && _upgradeBtnR.width > 8f)
                y = _upgradeBtnR.y - h - 14f * s;
            else if (step >= AlbumStepPage && _albumPagerOk)
                y = _albumPagerR.y - h - 18f * s;
            else if ((step == 2 || step == 3) && _albumInspectOk)
                y = _albumInspectR.y - h - 12f * s;
            else if (_albumCardOk)
                y = _albumCardR.y - h - 16f * s;
            if (y < minY) y = minY;
            float maxY = Screen.height - h - 8f;
            if (y > maxY) y = maxY;
            var seat = new Rect((Screen.width - w) * 0.5f, y, w, h);
            PaintCoachCaption(line, seat, s, 12, Mathf.RoundToInt(28f * s), default, false, StandardCaptionSlack);
        }

        void DrawHivePage()
        {
            float s = Mathf.Max(Screen.height / 720f, 1f);
            DrawHomeWash(0.28f);

            float top = TopHud();
            var back = BackMedalRect(s, top);
            PageSwipeAte = false;
            NoteAlbumOpen();
            if (_hiveInspect < 0 && DrawBackMedal(back))
            {
                ClearHivePageFresh(_hivePage);
                _hiveFlip = -1;
                _hivePageTurn = 99f;
                for (int k = 0; k < _hiveFaceBack.Length; k++) _hiveFaceBack[k] = false;
                _hiveInspect = -1;
                _hiveInspectT = 0f;
                _hiveInspectClosing = false;
                _hiveHowToNudge = false;
                ResetAlbumOpen();
                _home = HomeFace.Splash;
            }

            float textBottom = DrawAlbumHeader(s, back);
            float tallyW = Mathf.Min(Screen.width - 32f * s, 460f * s);
            var tallyAnchor = new Rect((Screen.width - tallyW) * 0.5f, textBottom + 4f * s, tallyW, 8f);
            float hiveBottom = DrawHiveTally(tallyAnchor, s);
            int pages = Mathf.Max(1, (Hive.AlbumSlots + HivePageSize - 1) / HivePageSize);
            _hivePage = Mathf.Clamp(_hivePage, 0, pages - 1);

            bool turning = _hivePageTurn < 0.55f;
            if (turning) _hivePageTurn += Time.unscaledDeltaTime;

            // Ultra Pro clear page: 3×3 sleeves on a binder sheet.
            // Pager hit is the painted chip and is at least 48pt.
            // Card size comes from the same GridOf the thumbnail fit test measures.
            int typeFloor = Mathf.Max(16, Mathf.RoundToInt(17f * s));
            const float footGap = 22f;
            var grid = CardText.GridOf(Screen.width, Screen.height, Screen.safeArea, hiveBottom);
            float colH = grid.ColH;
            float pagerHit = grid.PagerHit;
            float pagerLab = grid.PagerLab;
            float pageBottom = grid.PageBottom;
            var sheet = grid.Sheet;
            // Same strip WatchPageSwipe tracks; the album step's tap gate lets a swipe start here.
            _albumSwipeR = new Rect(0f, sheet.y, Screen.width, sheet.height);

            // Binder spine shadow + clear sheet
            GUI.color = new Color(0.08f, 0.06f, 0.04f, 0.62f);
            GUI.DrawTexture(new Rect(sheet.x - 6f * s, sheet.y + 8f * s, 14f * s, sheet.height), Texture2D.whiteTexture);
            GUI.color = new Color(0.86f, 0.93f, 0.96f, 0.30f);
            GUI.DrawTexture(sheet, Texture2D.whiteTexture);
            GUI.color = new Color(0.55f, 0.62f, 0.68f, 0.35f);
            // Sleeve grid. Same cell and card size GridOf just measured.
            float gap = grid.Gap;
            float cellW = grid.CellW;
            float cellH = grid.CellH;
            float cardW = grid.CardW;
            float cardH = grid.CardH;
            float gx0 = grid.Gx0;
            float gy0 = grid.Gy0;
            GUI.color = Color.white;
            DrawHiveColumns(hiveBottom + 4f * s, colH, gx0, cellW, gap, s);

            // During turn: draw outgoing page curling away, then incoming
            float turnU = turning ? Mathf.Clamp01(_hivePageTurn / 0.52f) : 1f;
            bool forward = _hivePage >= _hivePageFrom;

            void DrawPageCards(int pageIndex, float curl)
            {
                int baseIx = pageIndex * HivePageSize;
                // curl 0 = flat, 1 = fully turned (edge-on then gone)
                float widthScale = Mathf.Max(0.04f, Mathf.Abs(Mathf.Cos(curl * Mathf.PI * 0.5f)));
                float xShift = forward
                    ? Mathf.Lerp(0f, sheet.width * 0.55f, curl)
                    : Mathf.Lerp(0f, -sheet.width * 0.55f, curl);
                float shade = 1f - 0.35f * curl;
                for (int slot = 0; slot < HivePageSize; slot++)
                {
                    int i = baseIx + slot;
                    if (i >= Hive.AlbumSlots) break;
                    int col = slot % 3;
                    int row = slot / 3;
                    float cx = gx0 + col * (cellW + gap) + cellW * 0.5f + xShift;
                    float cy = gy0 + row * (cellH + gap) + cellH * 0.5f;
                    float w = cardW * widthScale;
                    var card = new Rect(cx - w * 0.5f, cy - cardH * 0.5f, w, cardH);
                    if (curl < 0.02f && i == _albumTutorSlot)
                    {
                        _albumCardR = card;
                        _albumCardOk = true;
                    }
                    // Sleeve pocket
                    var sleeve = new Rect(cx - cellW * 0.5f * widthScale + xShift * 0f, cy - cellH * 0.5f, cellW * widthScale, cellH);
                    // recompute sleeve with shift
                    sleeve = new Rect(
                        gx0 + col * (cellW + gap) + (cellW - cellW * widthScale) * 0.5f + xShift,
                        gy0 + row * (cellH + gap),
                        cellW * widthScale,
                        cellH);
                    GUI.color = new Color(0.90f, 0.94f, 0.96f, 0.40f * shade);
                    GUI.DrawTexture(sleeve, Texture2D.whiteTexture);
                    GUI.color = new Color(1f, 1f, 1f, shade);
                    if (widthScale > 0.12f)
                        DrawBeeAlbumCard(card, i, s);
                    GUI.color = Color.white;
                }
            }

            if (!_albumTutorOn || _albumTutorStep <= 1)
                _albumTutorSlot = AlbumTutorCardSlot(_hivePage);
            _albumCardOk = false;
            int albumSwipeDir = 0;
            if (_hiveInspect < 0 && !turning)
            {
                float swipeMin = AlbumSwipeMin(s);
                var swipeArea = _albumSwipeR;
                WatchPageSwipe(ref _pageSwipe, swipeArea, swipeMin);
                albumSwipeDir = _pageSwipe.Dir;
            }
            else
            {
                _pageSwipe.Tracking = false;
                _pageSwipe.Claimed = false;
            }

            if (turning)
            {
                // Old page curls away first half; new page settles second half
                if (turnU < 0.55f)
                {
                    float curl = turnU / 0.55f;
                    DrawPageCards(_hivePageFrom, curl);
                }
                else
                {
                    float curl = 1f - (turnU - 0.55f) / 0.45f;
                    DrawPageCards(_hivePage, Mathf.Clamp01(curl));
                }
                // Soft page sheet overlay during flip
                float wipe = forward ? turnU : (1f - turnU);
                float wipeX = sheet.x + sheet.width * wipe;
                GUI.color = new Color(0.95f, 0.93f, 0.88f, 0.35f * Mathf.Sin(turnU * Mathf.PI));
                GUI.DrawTexture(new Rect(wipeX - 18f * s, sheet.y, 36f * s, sheet.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            else
            {
                DrawPageCards(_hivePage, 0f);
            }

            if (albumSwipeDir != 0)
                TurnHivePage(_hivePage + albumSwipeDir);

            // Page chips. The painted box is the hit, at least 48pt.
            float tabY = pageBottom + 6f * s;
            float tabGap = 8f;
            int shown = Mathf.Min(pages, 5);
            float vis = pagerHit;
            int first = 0;
            if (pages > shown)
                first = Mathf.Clamp(_hivePage - shown / 2, 0, pages - shown);
            float tabsW = shown * pagerHit + (shown - 1) * tabGap;
            float tabX0 = (Screen.width - tabsW) * 0.5f;
            var tabLab = GuiPool.Label(GuiSlot.PageTab);
            tabLab.fontStyle = FontStyle.Bold;
            tabLab.alignment = TextAnchor.MiddleCenter;
            tabLab.wordWrap = false;
            for (int n = 0; n < shown; n++)
            {
                int p = first + n;
                var tab = new Rect(tabX0 + n * (pagerHit + tabGap), tabY, pagerHit, pagerHit);
                var box = new Rect(tab.center.x - vis * 0.5f, tab.center.y - vis * 0.5f, vis, vis);
                bool on = p == _hivePage && !turning;
                bool held;
                bool hit = HitPad(tab, out held);
                if (turning || _hiveInspect >= 0 || PageSwipeAte)
                {
                    hit = false;
                    held = false;
                }
                GUI.color = on
                    ? new Color(0.95f, 0.74f, 0.22f, held ? 1f : 0.95f)
                    : new Color(0.18f, 0.14f, 0.10f, held ? 0.92f : 0.72f);
                GUI.DrawTexture(box, Texture2D.whiteTexture);
                if (on)
                {
                    GUI.color = new Color(1f, 0.94f, 0.62f, 0.9f);
                    GUI.DrawTexture(new Rect(box.x + 2f, box.y + 2f, box.width - 4f, 3f), Texture2D.whiteTexture);
                }
                GUI.color = Color.white;
                string num = (p + 1).ToString();
                int numHi = Mathf.Max(22, Mathf.RoundToInt(28f * s));
                tabLab.fontSize = FitFont(tabLab, num, box.width * 0.72f, box.height * 0.68f, 18, numHi);
                StampReadable(box, num, tabLab, AlbumInk);
                if (HivePageHasFresh(p))
                {
                    float dot = Mathf.Max(7f, 6f * s);
                    GUI.color = new Color(0.93f, 0.22f, 0.16f, 0.95f);
                    GUI.DrawTexture(new Rect(box.xMax - dot * 0.35f, box.y - dot * 0.35f, dot, dot), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
                if (hit && p != _hivePage)
                    TurnHivePage(p);
            }

            var ofSt = GuiPool.Label(GuiSlot.PageOf);
            ofSt.fontStyle = FontStyle.Bold;
            ofSt.alignment = TextAnchor.MiddleCenter;
            ofSt.wordWrap = false;
            int bookHi = Mathf.Max(typeFloor + 4, Mathf.RoundToInt(24f * s));
            string ofBook = "Page " + (_hivePage + 1) + " of " + pages + "   ·   " + Hive.Found + " found";
            var bookR = new Rect(16f * s, tabY + pagerHit + footGap, Screen.width - 32f * s, pagerLab);
            ofSt.fontSize = FitFont(ofSt, ofBook, bookR.width * 0.92f, bookR.height * 0.86f, typeFloor, bookHi);
            DrawCoachPanel(bookR, 1f);
            StampReadable(bookR, ofBook, ofSt, AlbumInk);
            _albumPagerR = new Rect(tabX0, tabY, tabsW, pagerHit);
            _albumPagerOk = shown > 0 && tabsW > 8f;
            if (_albumTutorOn && _albumTutorStep >= 3 && pages < 2)
                FinishAlbumTutor();
            if (_hiveInspect >= 0)
                DrawHiveInspect(s);
            else
                _albumInspectOk = false;
            NoteEmptyAlbumTap(turning);
            TryArmAlbumTutor();
            TryArmUpgradeTutor();
            DrawAlbumTutor(s);
        }

        // Empty book, step 1: a release on the sleeve the glove points at is the valid
        // tap (nothing opens, there is no card yet), so the lesson moves on to the pager.
        // rawType survives a HitPad Use, so a sleeve that eats the press still counts.
        void NoteEmptyAlbumTap(bool turning)
        {
            if (!_albumTutorOn || _albumTutorStep > 1 || Hive.Found > 0 || turning) return;
            if (!_albumCardOk || _hiveInspect >= 0 || PageSwipeAte) return;
            var e = Event.current;
            if (e == null || e.rawType != EventType.MouseUp || e.button != 0) return;
            if (!_albumCardR.Contains(e.mousePosition)) return;
            AdvanceAlbumTutor(3);
        }

        void DrawHiveInspect(float s)
        {
            bool tick = HiveGuiTick();
            const float dur = 0.28f;
            if (tick) _hiveInspectT += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(_hiveInspectT / dur);
            float ease = 1f - (1f - u) * (1f - u); // easeOut quad
            float t = _hiveInspectClosing ? (1f - ease) : ease;
            if (tick) TickHiveFlip();

            // Dim overlay
            GUI.color = new Color(0.02f, 0.02f, 0.04f, 0.72f * t);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Large portrait card ~1:1.4, fill the screen for 55+ reading.
            // Same size the card-fit suite measures.
            var bigSize = CardText.InspectCard(Screen.width, Screen.height);
            float bigW = bigSize.x;
            float bigH = bigSize.y;
            var to = new Rect(
                (Screen.width - bigW) * 0.5f,
                (Screen.height - bigH) * 0.42f,
                bigW,
                bigH);
            var from = _hiveInspectFrom;
            var big = new Rect(
                Mathf.Lerp(from.x, to.x, t),
                Mathf.Lerp(from.y, to.y, t),
                Mathf.Lerp(from.width, to.width, t),
                Mathf.Lerp(from.height, to.height, t));

            int ix = _hiveInspect;
            _albumInspectR = big;
            _albumInspectOk = big.width > 8f && big.height > 8f;
            if (tick && _hiveTutorOn && !Ads.IsShowing && !_hiveInspectClosing)
                AdvanceHiveTutor(ix);
            DrawBeeAlbumCard(big, ix, s, true);
            if (t > 0.4f)
                DrawFlipArrows(big, s, Mathf.Clamp01(t));
            float hintTop = big.yMax + 12f * s;
            if (t > 0.4f)
                DrawFlipHint(hintTop, s, Mathf.Clamp01(t));
            bool upgradeHit = false;
            if (t > 0.55f && !_hiveInspectClosing && !_hiveTutorOn)
                upgradeHit = DrawHiveUpgrade(ix, hintTop, s, Mathf.Clamp01(t));
            if (_hiveTutorOn)
                DrawHiveTutorGlove(big, s);

            // Shared Back medallion, painted above the dim. Same control as Poker and the album.
            float top = TopHud();
            var back = BackMedalRect(s, top);
            bool backHit = DrawBackMedal(back);

            // Close X top-right of overlay. 44pt, opposite the shared Back.
            var safe = Screen.safeArea;
            float xSz = Mathf.Max(44f, 44f * Mathf.Min(s, 1.4f));
            var xBtn = new Rect(Screen.width - Mathf.Max(16f, Screen.width - safe.xMax + 10f) - xSz, top, xSz, xSz);
            bool xHeld;
            bool xHit = HitPad(xBtn, out xHeld);
            GUI.color = new Color(0.10f, 0.08f, 0.05f, (xHeld ? 0.92f : 0.78f) * Mathf.Clamp01(t + 0.15f));
            GUI.DrawTexture(xBtn, Texture2D.whiteTexture);
            GUI.color = Color.white;
            var xLab = GuiPool.Label(GuiSlot.InspectX);
            xLab.fontSize = Mathf.RoundToInt(22 * s);
            xLab.alignment = TextAnchor.MiddleCenter;
            xLab.fontStyle = FontStyle.Bold;
            xLab.normal.textColor = new Color(1f, 0.94f, 0.72f);
            GUI.Label(xBtn, "X", xLab);

            bool fullyOpen = !_hiveInspectClosing && u >= 1f;
            // Card taps during the demo are swallowed so they neither flip nor dismiss.
            if (_hiveTutorOn)
                HitPad(big, out _);
            else if (fullyOpen && !upgradeHit && HitPad(big, out _) && !PageSwipeAte)
                BeginHiveFlip(ix);

            // Tap dim outside card closes (after card/X/Back/upgrade so they win hit tests)
            var full = new Rect(0f, 0f, Screen.width, Screen.height);
            bool outside = !upgradeHit && !_hiveInspectClosing && u > 0.85f && HitPad(full, out _);
            if ((outside || xHit || backHit) && !_hiveInspectClosing)
                BeginPutAwayInspect();

            if (_hiveInspectClosing && u >= 1f)
                FinishPutAwayInspect();
        }

        // One Repaint per frame. OnGUI also runs for layout, and a second tick would rush the flip.
        bool HiveGuiTick()
        {
            var e = Event.current;
            if (e == null || e.type != EventType.Repaint) return false;
            if (_hiveGuiFrame == Time.frameCount) return false;
            _hiveGuiFrame = Time.frameCount;
            return true;
        }

        void TickHiveFlip()
        {
            if (_hiveFlip < 0) return;
            _hiveFlipT += Time.unscaledDeltaTime;
            if (_hiveFlipT < 0.28f) return;
            if ((uint)_hiveFlip < (uint)_hiveFaceBack.Length)
                _hiveFaceBack[_hiveFlip] = !_hiveFaceBack[_hiveFlip];
            _hiveFlip = -1;
            _hiveFlipT = 0f;
        }

        void BeginHiveFlip(int ix)
        {
            if (_hiveFlip >= 0) return;
            if ((uint)ix >= (uint)_hiveFaceBack.Length) return;
            _hiveFlip = ix;
            _hiveFlipT = 0f;
            if (_albumTutorOn && _albumTutorStep == 2)
                _albumTutorFlipped = true;
            Sfx.PageTurn();
        }

        void ArmHiveTutor()
        {
            if (_hiveTutorOn) return;
            if (_albumTutorOn || !AlbumTutorDone()) return;
            if (PlayerPrefs.GetInt(HiveFlipSeenKey, 0) != 0) return;
            _hiveTutorOn = true;
            _hiveTutorT = 0f;
            _hiveTutorStep = 0;
        }

        void FinishHiveTutor()
        {
            bool was = _hiveTutorOn;
            _hiveTutorOn = false;
            _hiveTutorT = 0f;
            _hiveTutorStep = 0;
            if (!was) return;
            if (PlayerPrefs.GetInt(HiveFlipSeenKey, 0) != 0) return;
            PlayerPrefs.SetInt(HiveFlipSeenKey, 1);
            PlayerPrefs.Save();
        }

        // Scripted tap, flip to the back, flip home, fade. No idle timer.
        void AdvanceHiveTutor(int ix)
        {
            float tutorDt = PlayClock.Delta;
            if (tutorDt > 0.05f) tutorDt = 0.05f;
            _hiveTutorT += tutorDt;
            if (_hiveTutorStep == 0 && _hiveTutorT >= 0.62f)
            {
                _hiveTutorStep = 1;
                _hiveTutorT = 0f;
                BeginHiveFlip(ix);
                return;
            }
            if (_hiveTutorStep == 1 && _hiveFlip < 0)
            {
                _hiveTutorStep = 2;
                _hiveTutorT = 0f;
                return;
            }
            if (_hiveTutorStep == 2 && _hiveTutorT >= 0.40f)
            {
                _hiveTutorStep = 3;
                _hiveTutorT = 0f;
                BeginHiveFlip(ix);
                return;
            }
            if (_hiveTutorStep == 3 && _hiveFlip < 0)
            {
                _hiveTutorStep = 4;
                _hiveTutorT = 0f;
                return;
            }
            if (_hiveTutorStep >= 4 && _hiveTutorT >= 0.45f)
                FinishHiveTutor();
        }

        void DrawHiveTutorGlove(Rect card, float s)
        {
            if (!_hiveTutorOn) return;
            float fade = _hiveTutorStep >= 4 ? 1f - Mathf.Clamp01(_hiveTutorT / 0.45f) : 1f;
            if (fade < 0.02f) return;
            var aim = GloveTarget(card);
            bool mirror = aim.x >= Screen.width * 0.5f;
            float ang = ClampUpright(mirror);
            var away = new Vector2(mirror ? -1f : 1f, 0f);
            var rest = aim + away * (GloveDh(s) * 0.85f) + new Vector2(0f, -GloveRise(s));
            NoteTutorPlate(FlipHintRect(card.yMax + 12f * s, s));
            ClearGloveOfCaption(aim, s, ref rest, ref away, ref mirror, ref ang, float.NaN);
            float travel = _hiveTutorStep == 0 ? Mathf.Clamp01(_hiveTutorT / 0.48f) : 1f;
            travel = travel * travel * (3f - 2f * travel);
            TapArc(rest, aim, away, s, travel, out var pivot, out _, mirror, _gloveApexMinY);
            float dip = 0f;
            if (_hiveTutorStep == 0 && _hiveTutorT > 0.40f)
                dip = Mathf.Sin(Mathf.Clamp01((_hiveTutorT - 0.40f) / 0.22f) * Mathf.PI);
            if (travel < 0.84f)
                SeatGlove(ref pivot, ref ang, aim, s, ref mirror, dip);
            DrawGloveAt(pivot, ang, GloveDh(s), fade, dip, mirror);
        }

        Rect FlipHintRect(float y, float s)
        {
            const string wider = "Tap outside to put back";
            int hintHi = Mathf.Clamp(Mathf.RoundToInt(32f * s), 28, 56);
            var measure = GuiPool.Label(GuiSlot.FlipHintMeasure);
            measure.fontStyle = FontStyle.Bold;
            measure.alignment = TextAnchor.MiddleCenter;
            measure.wordWrap = false;
            measure.fontSize = hintHi;
            float hintW = Mathf.Min(Screen.width * 0.92f, measure.CalcSize(GuiPool.Text(GuiText.FlipHint, wider)).x + 48f * s);
            float hintH = hintHi * 2.45f;
            var pill = new Rect((Screen.width - hintW) * 0.5f, y, hintW, hintH);
            float edge = 28f * s;
            if (pill.yMax > Screen.height - edge) pill.y = Screen.height - edge - pill.height;
            return pill;
        }

        // Hidden until first honey-badger encounter (Hive.UpgradesUnlocked).
        // Returns true when the finger is on the control (blocks flip / dismiss).
        bool DrawHiveUpgrade(int slot, float hintTop, float s, float alpha)
        {
            _upgradeBtnR = default;
            if (alpha < 0.04f || (uint)slot >= (uint)Hive.AlbumSlots) return false;
            if (!Hive.UpgradesUnlocked || Hive.CountOfSlot(slot) <= 0) return false;
            var pill = FlipHintRect(hintTop, s);
            float btnH = Mathf.Max(44f, 48f * Mathf.Min(s, 1.35f));
            float btnW = Mathf.Min(Screen.width * 0.72f, 420f * s);
            float y = pill.yMax + 10f * s;
            var btn = new Rect((Screen.width - btnW) * 0.5f, y, btnW, btnH);
            _upgradeBtnR = btn;
            bool room = Hive.UpgradeRoom(slot);
            bool can = Hive.CanUpgrade(slot);
            string line = !room
                ? "Max honey"
                : "Upgrade  ·  " + Money.Format(Hive.UpgradeCost);
            bool held;
            bool hit = HitPad(btn, out held);
            float fade = alpha * (room ? 1f : 0.55f);
            Color plate = can
                ? new Color(0.18f, 0.12f, 0.05f, (held ? 0.96f : 0.88f) * fade)
                : new Color(0.12f, 0.10f, 0.08f, 0.72f * fade);
            GUI.color = plate;
            GUI.DrawTexture(btn, Texture2D.whiteTexture);
            GUI.color = can
                ? new Color(1f, 0.82f, 0.28f, 0.95f * fade)
                : new Color(0.70f, 0.64f, 0.50f, 0.55f * fade);
            float edge = Mathf.Max(2f, 3f * s);
            GUI.DrawTexture(new Rect(btn.x, btn.y, btn.width, edge), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(btn.x, btn.yMax - edge, btn.width, edge), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(btn.x, btn.y, edge, btn.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(btn.xMax - edge, btn.y, edge, btn.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var st = GuiPool.Label(GuiSlot.FlipHint);
            st.fontStyle = FontStyle.Bold;
            st.alignment = TextAnchor.MiddleCenter;
            st.wordWrap = false;
            st.fontSize = FitFont(st, line, btn.width * 0.90f, btn.height * 0.70f, 16, Mathf.RoundToInt(28f * s));
            Color ink = can ? new Color(1f, 0.96f, 0.82f, fade) : new Color(0.78f, 0.74f, 0.64f, fade * 0.9f);
            StampOutlined(btn, line, st, ink, 1, 2);
            if (hit)
            {
                if (room && Hive.TryUpgrade(slot))
                {
                    Sfx.CardBump();
                    Haptics.Play(Haptics.Tier.Light);
                    if (_upgradeTutorOn) FinishUpgradeTutor();
                }
                else
                    Sfx.Deny();
            }
            return hit || held;
        }

        void DrawFlipHint(float y, float s, float alpha)
        {
            if (alpha < 0.04f) return;
            const string a = "Tap to flip!";
            const string b = "Tap outside to put back";
            int hi = Mathf.Clamp(Mathf.RoundToInt(32f * s), 28, 56);
            var st = GuiPool.Label(GuiSlot.FlipHint);
            st.fontStyle = FontStyle.Bold;
            st.alignment = TextAnchor.MiddleCenter;
            st.wordWrap = false;
            st.fontSize = hi;
            var pill = FlipHintRect(y, s);
            var inner = new Rect(pill.x + 18f, pill.y + 12f, Mathf.Max(8f, pill.width - 36f), Mathf.Max(8f, pill.height - 24f));
            NoteTutorPlate(CoachPanelRect(inner));
            DrawCoachPanel(inner, alpha);
            float h = pill.height;
            var r1 = new Rect(pill.x + 16f * s, pill.y + h * 0.06f, pill.width - 32f * s, h * 0.42f);
            var r2 = new Rect(pill.x + 16f * s, pill.y + h * 0.50f, pill.width - 32f * s, h * 0.42f);
            st.fontSize = FitFont(st, a, r1.width, r1.height * 0.92f, 24, hi);
            StampBannerText(r1, a, st, alpha, true);
            int bHi = Mathf.Max(st.fontSize, hi);
            st.fontSize = FitFont(st, b, r2.width, r2.height * 0.92f, 20, bHi);
            StampBannerText(r2, b, st, alpha, true);
        }

        void DrawFlipArrows(Rect card, float s, float alpha)
        {
            DrawFlipArrow(card, true, s, alpha);
            DrawFlipArrow(card, false, s, alpha);
        }

        void DrawFlipArrow(Rect card, bool left, float s, float alpha)
        {
            BeeFlare.Draw(card, left, s, alpha);
        }

        void BeginPutAwayInspect()
        {
            if (_hiveInspect < 0 || _hiveInspectClosing) return;
            if (_hiveTutorOn) FinishHiveTutor();
            int ix = _hiveInspect;
            _hiveInspectClosing = true;
            _hiveInspectT = 0f;
            Sfx.PageTurn();
            if ((uint)ix >= (uint)_hiveFaceBack.Length)
            {
                _hiveFlip = -1;
                _hiveFlipT = 0f;
                return;
            }
            if (_hiveFaceBack[ix])
            {
                if (_hiveFlip != ix)
                {
                    _hiveFlip = ix;
                    _hiveFlipT = 0f;
                }
            }
            else
            {
                _hiveFlip = -1;
                _hiveFlipT = 0f;
            }
        }

        void FinishPutAwayInspect()
        {
            int ix = _hiveInspect;
            if ((uint)ix < (uint)_hiveFaceBack.Length)
                _hiveFaceBack[ix] = false;
            _hiveInspect = -1;
            _hiveInspectClosing = false;
            _hiveInspectT = 0f;
            _hiveFlip = -1;
            _hiveFlipT = 0f;
        }

        void DrawFoilSheen(Rect face, BeeFinish finish, int slot, bool inspectView)
        {
            float t = Time.unscaledTime;
            float phase = slot * 0.37f;
            float amp = inspectView ? 1.15f : 1f;

            // Soft wash so the finish reads even mid-sweep
            float wash = (0.06f + 0.04f * Mathf.Sin(t * 2.1f + phase)) * amp;
            if (finish == BeeFinish.Holo)
                GUI.color = new Color(0.70f, 0.90f, 1f, wash);
            else
            {
                float h = Mathf.Repeat(t * 0.22f + phase * 0.05f, 1f);
                Color c = Color.HSVToRGB(h, 0.55f, 1f);
                c.a = wash * 1.25f;
                GUI.color = c;
            }
            GUI.DrawTexture(face, Texture2D.whiteTexture);

            // Traveling specular band (coin-style sweep)
            float speed = finish == BeeFinish.Holo ? 0.42f : 0.55f;
            float sheenU = Mathf.Repeat(t * speed + phase, 1.55f);
            if (sheenU < 1f)
            {
                float fade = Mathf.Sin(sheenU * Mathf.PI);
                float bandW = face.width * (finish == BeeFinish.Holo ? 0.22f : 0.28f);
                float x = face.x + face.width * (sheenU * 1.25f - 0.2f);
                var band = new Rect(x, face.y, bandW, face.height);
                if (finish == BeeFinish.Holo)
                    GUI.color = new Color(0.85f, 0.95f, 1f, 0.22f * fade * amp);
                else
                {
                    float h2 = Mathf.Repeat(sheenU * 0.85f + t * 0.15f + phase, 1f);
                    Color c2 = Color.HSVToRGB(h2, 0.65f, 1f);
                    c2.a = 0.30f * fade * amp;
                    GUI.color = c2;
                }
                GUI.DrawTexture(band, Texture2D.whiteTexture);

                // Second thinner flash for InverseRainbow "high texture"
                if (finish == BeeFinish.InverseRainbow)
                {
                    float x2 = face.x + face.width * (Mathf.Repeat(sheenU + 0.33f, 1f) * 1.2f - 0.15f);
                    GUI.color = new Color(1f, 1f, 1f, 0.16f * fade * amp);
                    GUI.DrawTexture(new Rect(x2, face.y, face.width * 0.10f, face.height), Texture2D.whiteTexture);
                }
            }

            // Prism rim
            float rim = (inspectView ? 3.2f : 2.2f);
            float rimA = (0.18f + 0.10f * Mathf.Sin(t * 3.0f + phase)) * amp;
            if (finish == BeeFinish.Holo)
                GUI.color = new Color(0.55f, 0.82f, 1f, rimA);
            else
            {
                Color rc = Color.HSVToRGB(Mathf.Repeat(t * 0.35f + phase, 1f), 0.7f, 1f);
                rc.a = rimA * 1.2f;
                GUI.color = rc;
            }
            GUI.DrawTexture(new Rect(face.x, face.y, face.width, rim), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(face.x, face.yMax - rim, face.width, rim), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(face.x, face.y, rim, face.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(face.xMax - rim, face.y, rim, face.height), Texture2D.whiteTexture);
            for (int sp = 0; sp < 3; sp++)
            {
                float u = Mathf.Repeat(t * 0.40f + phase + sp * 0.31f, 1f);
                float a = Mathf.Sin(u * Mathf.PI) * amp;
                float sz = Mathf.Max(3f, face.width * 0.040f);
                float x = face.x + face.width * (0.12f + 0.68f * Mathf.Repeat(u + sp * 0.37f, 1f));
                float y = face.y + face.height * (0.05f + 0.12f * sp);
                GUI.color = new Color(1f, 1f, 1f, 0.70f * a);
                GUI.DrawTexture(new Rect(x, y, sz, sz * 0.28f), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(x + sz * 0.36f, y - sz * 0.36f, sz * 0.28f, sz), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
        }

        // The album card's wood frame and plate colors. The badger tiles read the same two,
        // so a tile and its sleeve card are the same tint.
        static Color AlbumWood(Color tint, bool owned) => owned
            ? Color.Lerp(new Color(0.28f, 0.16f, 0.07f), tint, 0.35f)
            : new Color(0.14f, 0.12f, 0.10f, 0.92f);

        static Color AlbumFace(Color tint, bool owned) => owned
            ? Color.Lerp(new Color(0.98f, 0.92f, 0.72f), tint, 0.18f)
            : new Color(0.22f, 0.20f, 0.18f, 0.95f);

        void DrawBeeAlbumCard(Rect card, int i, float s) => DrawBeeAlbumCard(card, i, s, false, true);

        void DrawBeeAlbumCard(Rect card, int i, float s, bool inspectView) =>
            DrawBeeAlbumCard(card, i, s, inspectView, true);

        void DrawBeeAlbumCard(Rect card, int i, float s, bool inspectView, bool tappable)
        {
            // Pulled-out sleeve: faint ghost so the card looks removed
            if (!inspectView && i == _hiveInspect)
            {
                GUI.color = new Color(0.95f, 0.92f, 0.85f, 0.14f);
                GUI.DrawTexture(card, Texture2D.whiteTexture);
                GUI.color = Color.white;
                return;
            }

            if ((uint)i >= (uint)Hive.AlbumSlots) return;
            if (!inspectView && HiveCardFresh(i))
            {
                float p = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5.2f);
                GUI.color = new Color(1f, 0.82f, 0.28f, 0.28f + 0.50f * p);
                float m = (3f + 3f * p) * Mathf.Max(1f, s);
                GUI.DrawTexture(new Rect(card.x - m, card.y - m, card.width + m * 2f, card.height + m * 2f), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            int kindIx = Hive.KindOfSlot(i);
            BeeFinish finish = Hive.FinishOfSlot(i);
            int n = Hive.CountOf(kindIx, finish);
            bool owned = n > 0;
            bool flipping = _hiveFlip == i;
            float flipU = flipping ? Mathf.Clamp01(_hiveFlipT / 0.28f) : 0f;
            float squash = flipping ? Mathf.Abs(Mathf.Cos(flipU * Mathf.PI)) : 1f;
            // Sleeves always show the front; the back only lives in inspect.
            bool faceBack = inspectView && _hiveFaceBack[i];
            bool showBack = inspectView && (flipping ? (flipU >= 0.5f ? !faceBack : faceBack) : faceBack);

            float cx = card.center.x;
            float w = card.width * Mathf.Max(0.08f, squash);
            var r = new Rect(cx - w * 0.5f, card.y, w, card.height);

            var kind = Hive.Roster[kindIx];
            Color wood = AlbumWood(kind.Tint, owned);
            GUI.color = new Color(0f, 0f, 0f, 0.35f);
            GUI.DrawTexture(new Rect(r.x + 4f, r.y + 6f, r.width, r.height), Texture2D.whiteTexture);
            GUI.color = wood;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Inner face — use the plate; type needs every pixel for 55+ reading.
            // FaceOf is the rect the thumbnail fit test measures.
            var face = CardText.FaceOf(r);
            Color faceFill = AlbumFace(kind.Tint, owned);
            GUI.color = faceFill;
            GUI.DrawTexture(face, Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Foil sheen stays inside the face, including the rounded corners.
            if (owned && finish != BeeFinish.Normal && squash > 0.2f)
            {
                var local = new Rect(0f, 0f, face.width, face.height);
                CardClip.Begin(face);
                DrawFoilSheen(local, finish, i, inspectView);
                CardClip.Corners(local, faceFill, face.width * 0.06f);
                CardClip.End();
            }

            if (!owned)
            {
                if (finish == BeeFinish.Holo || finish == BeeFinish.InverseRainbow)
                {
                    Color edge = finish == BeeFinish.Holo
                        ? new Color(0.15f, 0.48f, 0.92f, 0.95f)
                        : new Color(0.82f, 0.22f, 0.68f, 0.95f);
                    float edgeH = Mathf.Max(3f, 4f * s);
                    GUI.color = edge;
                    GUI.DrawTexture(new Rect(face.x, face.y, face.width, edgeH), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
                var lockedBee = SpriteCatalog.Bee;
                if (lockedBee != null && lockedBee.texture != null)
                {
                    float bh = face.height * 0.46f;
                    var br = new Rect(face.center.x - bh * 0.5f, face.y + face.height * 0.08f, bh, bh);
                    GUI.color = new Color(kind.Tint.r * 0.45f, kind.Tint.g * 0.45f, kind.Tint.b * 0.45f, 0.80f);
                    GUI.DrawTexture(br, lockedBee.texture, ScaleMode.ScaleToFit, true);
                    GUI.color = Color.white;
                }
                var mystery = GuiPool.Label(GuiSlot.AlbumMystery);
                mystery.fontStyle = FontStyle.Bold;
                mystery.alignment = TextAnchor.MiddleCenter;
                mystery.fontSize = FitFont(mystery, "?", face.width * 0.55f, face.height * 0.40f, inspectView ? 48 : 28, inspectView ? 96 : 64);
                StampOutlined(new Rect(face.x, face.y + face.height * 0.28f, face.width, face.height * 0.42f), "?", mystery, new Color(0.62f, 0.54f, 0.42f), 2, 1);
            }
            else if (!showBack)
            {
                var bee = SpriteCatalog.Bee;
                float beeShare = inspectView ? 0.24f : 0.38f;
                if (bee != null && bee.texture != null)
                {
                    float bh = face.height * beeShare;
                    float bw = bh;
                    var br = new Rect(face.center.x - bw * 0.5f, face.y + face.height * 0.03f, bw, bh);
                    GUI.color = kind.Tint;
                    GUI.DrawTexture(br, bee.texture, ScaleMode.ScaleToFit, true);
                    GUI.color = Color.white;
                }
                var textFace = CardText.TextFaceOf(face, inspectView);
                CardText.DrawFront(textFace, kind.Name, kind.Front, inspectView, s);
                if (n > 1)
                {
                    var cnt = GuiPool.Label(GuiSlot.AlbumCount);
                    cnt.fontStyle = FontStyle.Bold;
                    cnt.alignment = TextAnchor.UpperRight;
                    cnt.fontSize = Mathf.Max(inspectView ? 22 : Mathf.RoundToInt(14f * s), Mathf.RoundToInt((inspectView ? 20f : 14f) * s));
                    StampOutlined(new Rect(face.xMax - 56f * s, face.y + 4f * s, 52f * s, inspectView ? 32f * s : 20f * s), "×" + n, cnt, new Color(0.16f, 0.07f, 0.02f), inspectView ? 2 : 1, 1);
                }
                if (finish != BeeFinish.Normal)
                {
                    string foil = finish == BeeFinish.Holo ? "Holo" : "Inverse Rainbow";
                    Color foilCol = finish == BeeFinish.Holo
                        ? new Color(0.15f, 0.48f, 0.92f)
                        : new Color(0.82f, 0.22f, 0.68f);
                    CardText.DrawFinish(CardText.FinishOf(face, inspectView), foil, inspectView, s, foilCol);
                }
            }
            else
            {
                string backCopy = HiveBackCopy(kind.Back);
                // Flip attributes: honeycombs only. Base by finish + purchased upgrades.
                int honey = BadgerSchedule.Enabled ? Hive.HoneyOfSlot(i) : 0;
                CardText.DrawBack(face, kind.Name, backCopy, honey, kind.Tint, finish, inspectView, s);
            }

            if (!inspectView && owned && HiveCardFresh(i))
                DrawNewPill(card, s);

            // Owned sleeve opens inspect. A locked "?" re-shows the how-to and stays shut.
            if (tappable && !inspectView && !flipping && _hivePageTurn >= 0.55f && _hiveInspect < 0
                && HitPad(card, out _) && !PageSwipeAte)
            {
                if (owned) OpenHiveInspect(i, card);
                else _hiveHowToNudge = true;
            }
        }

        static Texture2D _hiveStar;

        static Texture2D HiveStarTex()
        {
            if (_hiveStar != null) return _hiveStar;
            const int n = 48;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = "HiveStar"
            };
            var px = new Color32[n * n];
            float outer = n * 0.46f;
            float inner = outer * 0.40f;
            float c = n * 0.5f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = x + 0.5f - c;
                    float dy = c - (y + 0.5f);
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float local = Mathf.Repeat(Mathf.Atan2(dy, dx) + Mathf.PI * 0.5f, Mathf.PI * 2f / 5f);
                    float half = Mathf.PI / 5f;
                    float k = Mathf.Abs(local - half) / half;
                    float rad = Mathf.Lerp(outer, inner, k);
                    float edge = rad - r;
                    float a = Mathf.Clamp01(edge);
                    px[y * n + x] = a <= 0.004f
                        ? new Color32(0, 0, 0, 0)
                        : new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _hiveStar = tex;
            return tex;
        }

        void DrawHiveStars(Rect band, int stars, float s)
        {
            var tex = HiveStarTex();
            if (tex == null || stars <= 0) return;
            float sz = Mathf.Clamp(band.height * 0.92f, 16f, 34f * Mathf.Max(1f, s));
            float gap = sz * 0.16f;
            float row = stars * sz + (stars - 1) * gap;
            float x = band.center.x - row * 0.5f;
            float y = band.center.y - sz * 0.5f;
            var prev = GUI.color;
            for (int i = 0; i < stars; i++)
            {
                GUI.color = new Color(0.42f, 0.22f, 0.05f, 0.9f);
                GUI.DrawTexture(new Rect(x + 1.5f, y + 2f, sz, sz), tex, ScaleMode.ScaleToFit, true);
                GUI.color = new Color(1f, 0.82f, 0.26f, 1f);
                GUI.DrawTexture(new Rect(x, y, sz, sz), tex, ScaleMode.ScaleToFit, true);
                x += sz + gap;
            }
            GUI.color = prev;
        }

        void DrawNewPill(Rect card, float s)
        {
            var st = GuiPool.Label(GuiSlot.NewPill);
            st.fontStyle = FontStyle.Bold;
            st.alignment = TextAnchor.MiddleCenter;
            st.wordWrap = false;
            st.fontSize = Mathf.Max(11, Mathf.RoundToInt(12f * s));
            float h = Mathf.Max(16f, st.fontSize + 6f);
            float w = st.CalcSize(GuiPool.Text(GuiText.NewPill, "NEW")).x + 12f;
            var r = new Rect(card.xMax - w - 4f, card.y + 4f, w, h);
            GUI.color = new Color(0.55f, 0.28f, 0.05f, 0.95f);
            GUI.DrawTexture(new Rect(r.x + 1f, r.y + 1.5f, r.width, r.height), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 0.82f, 0.22f, 0.98f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
            StampOutlined(r, "NEW", st, new Color(0.28f, 0.12f, 0.03f), 1, 1);
        }

        void DrawLevelHive(float s)
        {
            int count = _levelBees.Count;
            bool cards = count > 0;
            int floor = Mathf.RoundToInt(14f * s);
            float boxW = Mathf.Min(Screen.width - 24f * s, 560f * s);
            float boxH = cards
                ? Mathf.Min(Screen.height * 0.72f, 560f * s)
                : Mathf.Min(Screen.height * 0.30f, 200f * s);
            float y = TopHud() + 6f * s;
            var box = new Rect((Screen.width - boxW) * 0.5f, y, boxW, boxH);
            _levelHiveRect = box;

            GUI.color = new Color(0.10f, 0.07f, 0.04f, 0.94f);
            GUI.DrawTexture(box, Texture2D.whiteTexture);
            GUI.color = new Color(0.96f, 0.78f, 0.32f, 0.90f);
            float edge = Mathf.Max(2f, 3f * s);
            GUI.DrawTexture(new Rect(box.x, box.y, box.width, edge), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(box.x, box.yMax - edge, box.width, edge), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(box.x, box.y, edge, box.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(box.xMax - edge, box.y, edge, box.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var titleSt = GuiPool.Label(GuiSlot.LevelHiveTitle);
            titleSt.fontStyle = FontStyle.Bold;
            titleSt.alignment = TextAnchor.MiddleCenter;
            titleSt.wordWrap = false;
            const string roundHead = "Cards collected this round";
            int titlePx = Mathf.Max(floor + 4, Mathf.RoundToInt(26f * s));
            var titleR = new Rect(box.x + 12f * s, box.y + 12f * s, box.width - 24f * s, titlePx + 10f);
            titleSt.fontSize = FitFont(titleSt, roundHead, titleR.width * 0.98f, titleR.height * 0.92f, floor, titlePx);
            StampBannerText(titleR, roundHead, titleSt, 1f, true);

            if (!cards)
            {
                var howSt = GuiPool.Label(GuiSlot.LevelHiveHow);
                howSt.fontStyle = FontStyle.Bold;
                howSt.alignment = TextAnchor.MiddleCenter;
                howSt.wordWrap = false;
                const string empty = "Nothing yet…";
                var howR = new Rect(box.x + 16f * s, titleR.yMax + 8f * s, box.width - 32f * s, box.yMax - titleR.yMax - 20f * s);
                int howHi = Mathf.Max(floor + 8, Mathf.RoundToInt(28f * s));
                howSt.fontSize = FitFont(howSt, empty, howR.width * 0.96f, howR.height * 0.8f, floor, howHi);
                StampOutlined(howR, empty, howSt, new Color(1f, 0.96f, 0.82f), 2, 2);
                return;
            }

            if (count != _levelHiveCountSeen)
            {
                _levelHivePick = count - 1;
                _levelHiveShown = _levelHivePick;
                _levelHiveAuto = 0f;
                _levelHiveCountSeen = count;
                _levelHiveDrag = false;
                _levelHiveDragX = 0f;
            }
            _levelHivePick = Mathf.Clamp(_levelHivePick, 0, count - 1);

            var ev = Event.current;
            if (ev != null && ev.type == EventType.Repaint && Time.frameCount != _levelHiveFrame)
            {
                _levelHiveFrame = Time.frameCount;
                if (count > 1 && _hiveInspect < 0 && !_levelHiveDrag)
                {
                    _levelHiveAuto += Time.unscaledDeltaTime;
                    if (_levelHiveAuto >= HiveCarouselStep)
                    {
                        _levelHiveAuto = 0f;
                        _levelHivePick = (_levelHivePick + 1) % count;
                    }
                }
                if (!_levelHiveDrag) EaseHiveCarousel(count);
            }

            float capH = 0f;
            float dotsH = count > 1 ? 26f * s : 0f;
            float stageTop = titleR.yMax + 6f * s;
            float stageBot = box.yMax - 10f * s - capH - dotsH;
            float stageH = Mathf.Max(48f * s, stageBot - stageTop);
            var stage = new Rect(box.x + 8f * s, stageTop, box.width - 16f * s, stageH);
            float cardH = stage.height * 0.94f;
            float cardW = Mathf.Min(cardH / 1.35f, stage.width * 0.62f);
            cardH = cardW * 1.35f;
            float stride = cardW * 0.78f;
            float shown = _levelHiveShown;
            if (_levelHiveDrag && count > 1)
                shown -= _levelHiveDragX / Mathf.Max(1f, stride);

            int centerIx = 0;
            float best = 999f;
            for (int i = 0; i < count; i++)
            {
                float a = Mathf.Abs(HiveWrap(i - shown, count));
                if (a < best) { best = a; centerIx = i; }
            }

            GUI.BeginGroup(stage);
            float cx = stage.width * 0.5f;
            float cy = stage.height * 0.5f;
            Rect centerLocal = default;
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < count; i++)
                {
                    bool center = i == centerIx;
                    if (center != (pass == 1)) continue;
                    float off = HiveWrap(i - shown, count);
                    if (!center && Mathf.Abs(off) > 1.25f) continue;
                    float sc = Mathf.Lerp(1f, 0.72f, Mathf.Clamp01(Mathf.Abs(off)));
                    float w = cardW * sc;
                    float h = cardH * sc;
                    var r = new Rect(cx + off * stride - w * 0.5f, cy - h * 0.5f, w, h);
                    if (center) centerLocal = r;
                    int slot = _levelBees[i].Slot;
                    if ((uint)slot < (uint)Hive.AlbumSlots)
                        DrawBeeAlbumCard(r, slot, s, false, false);
                }
            }
            bool tap = false;
            int dir = 0;
            if (_levelHiveSwallow)
            {
                var swallow = Event.current;
                bool released = swallow != null && swallow.type == EventType.MouseUp && swallow.button == 0;
                bool up = Mouse.current == null || !Mouse.current.leftButton.isPressed;
                if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                    up = false;
                if (released || (swallow != null && swallow.type == EventType.Repaint && up))
                    _levelHiveSwallow = false;
            }
            else if (_hiveInspect < 0)
                CarouselDrag(new Rect(0f, 0f, stage.width, stage.height), s, count, out tap, out dir);
            GUI.EndGroup();

            if (dir != 0)
            {
                _levelHivePick = (_levelHivePick + dir + count) % count;
                _levelHiveShown = _levelHivePick;
                _levelHiveAuto = 0f;
            }
            else if (tap)
            {
                var from = centerLocal;
                from.x += stage.x;
                from.y += stage.y;
                OpenHiveInspect(_levelBees[centerIx].Slot, from);
            }

            if (count > 1 && count <= 12)
            {
                float dot = 12f * s;
                float gap = 8f * s;
                float rowW = count * dot + (count - 1) * gap;
                float x0 = box.center.x - rowW * 0.5f;
                float dy = box.yMax - 8f * s - dot;
                for (int i = 0; i < count; i++)
                {
                    var d = new Rect(x0 + i * (dot + gap), dy, dot, dot);
                    if (_hiveInspect < 0 && !_levelHiveSwallow && HitPad(d, out _))
                    {
                        _levelHivePick = i;
                        _levelHiveShown = i;
                        _levelHiveAuto = 0f;
                    }
                    bool on = i == _levelHivePick;
                    GUI.color = on
                        ? new Color(1f, 0.82f, 0.28f, 0.95f)
                        : new Color(1f, 0.94f, 0.72f, 0.35f);
                    GUI.DrawTexture(d, Texture2D.whiteTexture);
                }
                GUI.color = Color.white;
            }
            else if (count > 12)
            {
                var pageSt = GuiPool.Label(GuiSlot.LevelHivePage);
                pageSt.fontStyle = FontStyle.Bold;
                pageSt.alignment = TextAnchor.MiddleCenter;
                pageSt.wordWrap = false;
                pageSt.fontSize = floor;
                string page = (_levelHivePick + 1) + " / " + count;
                float pageH = floor + 8f;
                StampOutlined(new Rect(box.x, box.yMax - pageH - 6f * s, box.width, pageH), page, pageSt, new Color(1f, 0.94f, 0.72f), 1, 1);
            }
        }

        void CarouselDrag(Rect r, float s, int count, out bool tap, out int dir)
        {
            tap = false;
            dir = 0;
            int id = GUIUtility.GetControlID(FocusType.Passive);
            _levelHiveDragId = id;
            var e = Event.current;
            if (e == null) return;
            bool inside = r.Contains(e.mousePosition);
            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (inside && e.button == 0)
                    {
                        GUIUtility.hotControl = id;
                        _levelHiveDrag0 = e.mousePosition;
                        _levelHiveDragX = 0f;
                        _levelHiveDrag = true;
                        _levelHiveAuto = 0f;
                        e.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        _levelHiveDragX = e.mousePosition.x - _levelHiveDrag0.x;
                        e.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        float dx = e.mousePosition.x - _levelHiveDrag0.x;
                        _levelHiveDrag = false;
                        _levelHiveDragX = 0f;
                        e.Use();
                        float slop = Mathf.Max(28f, 36f * s);
                        if (count > 1 && Mathf.Abs(dx) >= slop)
                            dir = dx < 0f ? 1 : -1;
                        else if (inside)
                            tap = true;
                    }
                    break;
            }
        }

        void OpenLevelHive()
        {
            _levelHive = true;
            DismissHiveIntro();
            MarkHiveOpened();
            int n = _levelBees.Count;
            _levelHivePick = Mathf.Max(0, n - 1);
            _levelHiveShown = _levelHivePick;
            _levelHiveAuto = 0f;
            _levelHiveCountSeen = n;
            _levelHiveDrag = false;
            _levelHiveDragX = 0f;
            // The tap that opens the panel must not also tap the card underneath.
            _levelHiveSwallow = true;
        }

        void CloseLevelHive()
        {
            _levelHive = false;
            _levelHiveDrag = false;
            _levelHiveDragX = 0f;
            if (_levelHiveDragId != 0 && GUIUtility.hotControl == _levelHiveDragId)
                GUIUtility.hotControl = 0;
        }

        const string HiveOpenedKey = "flockfive.hive.opened";

        void MarkHiveOpened()
        {
            if (PlayerPrefs.GetInt(HiveOpenedKey, 0) != 0) return;
            PlayerPrefs.SetInt(HiveOpenedKey, 1);
            PlayerPrefs.Save();
        }

        bool HiveOpenedSeen() => PlayerPrefs.GetInt(HiveOpenedKey, 0) != 0;

        void OpenHiveAlbum()
        {
            ResetAlbumOpen();
            if (_hiveJumpSlot < 0 && _hiveFreshSlots.Count > 0)
                _hiveJumpSlot = _hiveFreshSlots[_hiveFreshSlots.Count - 1];
            if (_hiveJumpSlot >= 0)
            {
                int pages = Mathf.Max(1, (Hive.AlbumSlots + HivePageSize - 1) / HivePageSize);
                _hivePage = Mathf.Clamp(_hiveJumpSlot / HivePageSize, 0, pages - 1);
                _hivePageFrom = _hivePage;
                _hivePageTurn = 99f;
                _hiveFlip = -1;
                _hiveJumpSlot = -1;
            }
            MarkHiveOpened();
            _home = HomeFace.Hive;
        }

        void NoteHiveCue(BeeVisit visit)
        {
            _hiveJumpSlot = visit.Slot;
            if (!visit.Fresh) return;
            for (int i = 0; i < _hiveFreshSlots.Count; i++)
                if (_hiveFreshSlots[i] == visit.Slot) return;
            _hiveFreshSlots.Add(visit.Slot);
        }

        bool HiveCardFresh(int slot)
        {
            for (int i = 0; i < _hiveFreshSlots.Count; i++)
                if (_hiveFreshSlots[i] == slot) return true;
            return false;
        }

        void ClearHiveFresh(int slot)
        {
            for (int i = _hiveFreshSlots.Count - 1; i >= 0; i--)
                if (_hiveFreshSlots[i] == slot) _hiveFreshSlots.RemoveAt(i);
        }

        void ClearHivePageFresh(int page)
        {
            int start = page * HivePageSize;
            int end = Mathf.Min(start + HivePageSize, Hive.AlbumSlots);
            for (int i = start; i < end; i++) ClearHiveFresh(i);
        }

        bool HivePageHasFresh(int page)
        {
            int start = page * HivePageSize;
            int end = Mathf.Min(start + HivePageSize, Hive.AlbumSlots);
            for (int i = start; i < end; i++)
                if (HiveCardFresh(i)) return true;
            return false;
        }

        void OpenHiveInspect(int slot, Rect from)
        {
            if ((uint)slot >= (uint)Hive.AlbumSlots) return;
            if (Hive.CountOfSlot(slot) <= 0) return;
            _hiveInspect = slot;
            _hiveInspectT = 0f;
            _hiveInspectClosing = false;
            _hiveInspectFrom = from;
            if ((uint)slot < (uint)_hiveFaceBack.Length)
                _hiveFaceBack[slot] = false;
            _hiveFlip = -1;
            _hiveFlipT = 0f;
            ClearHiveFresh(slot);
            if (_albumTutorOn && _albumTutorStep <= 1)
                AdvanceAlbumTutor(2);
            ArmHiveTutor();
            Sfx.PageTurn();
        }

        void DrawHiveColumns(float y, float h, float gx0, float cellW, float gap, float s)
        {
            for (int col = 1; col < 3; col++)
            {
                string text = col == 1 ? "Holo" : "Inverse Rainbow";
                Color ink = col == 1
                    ? new Color(0.72f, 0.93f, 1f)
                    : new Color(1f, 0.74f, 0.94f);
                var r = new Rect(gx0 + col * (cellW + gap), y, cellW, h);
                CardText.DrawColumn(r, text, s, ink);
            }
        }

        static float HiveWrap(float d, int count)
        {
            if (count <= 1) return 0f;
            float n = count;
            return Mathf.Repeat(d + n * 0.5f, n) - n * 0.5f;
        }

        void EaseHiveCarousel(int count)
        {
            if (count <= 1)
            {
                _levelHiveShown = 0f;
                _levelHivePick = 0;
                return;
            }
            float d = HiveWrap(_levelHivePick - _levelHiveShown, count);
            float step = Time.unscaledDeltaTime / 0.28f;
            if (Mathf.Abs(d) <= step) _levelHiveShown = _levelHivePick;
            else _levelHiveShown += Mathf.Sign(d) * step;
            if (_levelHiveShown < 0f) _levelHiveShown += count;
            else if (_levelHiveShown >= count) _levelHiveShown -= count;
        }

        bool GiftLocked(int i)
        {
            if (_board == null || (uint)i >= (uint)_board.Branches.Count) return false;
            return _board.Branches[i].AdLocked;
        }

        void OpenGift()
        {
            if (_iceCoating) return;
            if (!GatePopup(PopupKind.Gift, true)) return;
            _giftBranch = -1;
            if (_frozen) _freezeOffer = true;
            OpenGiftCard();
        }

        void OpenBonus(int branch)
        {
            if (LessonTapBlocked()) return;
            if (!CanOfferBonus(branch)) return;
            if (!GatePopup(PopupKind.Gift, true)) return;
            _giftBranch = branch;
            OpenGiftCard();
            if (_gift == GiftFace.Card) ArmAdHand();
        }

        bool CanOfferBonus(int branch)
        {
            if (_sel >= 0 || _won) return false;
            if (_busy || _collecting || _locked.Count > 0) return false;
            if (PlayClock.Now < _suppressGiftUntil) return false;
            if (_gift != GiftFace.None) return false;
            if (_board == null || (uint)branch >= (uint)_board.Branches.Count) return false;
            var st = _board.Branches[branch];
            if (st == null || !st.IsBonus || st.Broken || !st.AdLocked) return false;
            int ord = BonusBranches.Ordinal(_board, branch);
            if (ord < 0 || ord >= _bonusOn.Length || _bonusOn[ord]) return false;
            return true;
        }

        // Whole limb and sign. The sign BoxCollider2D does not receive the tap:
        // it was shrunk to 72% of the sprite, the left sign is mirrored with a
        // negative scale (2D physics drops that shape), and every miss refreshed
        // the 1s gift lockout so a follow-up tap was swallowed too.
        int HitGiftSign(Vector2 world)
        {
            if (_garden.Branches == null || _board == null) return -1;
            if (NearPlayBird(world)) return -1;
            int best = -1;
            float bestD = float.MaxValue;
            for (int i = 0; i < _garden.Branches.Length; i++)
            {
                var v = _garden.Branches[i];
                if (v == null || !v.gameObject.activeInHierarchy) continue;
                int idx = v.Index;
                if ((uint)idx >= (uint)_board.Branches.Count) continue;
                var st = _board.Branches[idx];
                if (st == null || !st.IsBonus || st.Broken || !st.AdLocked) continue;
                if (!BonusSpot(v, world)) continue;
                float d = ((Vector2)v.transform.position - world).sqrMagnitude;
                if (d < bestD) { bestD = d; best = idx; }
            }
            return best;
        }

        // Same radius HitBranch uses to claim a bird, so a perch tap beside the
        // bottom gifts still selects that bird.
        bool NearPlayBird(Vector2 world)
        {
            const float r2 = 1.05f * 1.05f;
            for (int b = 0; b < _garden.Branches.Length; b++)
            {
                var v = _garden.Branches[b];
                if (v == null || !v.gameObject.activeInHierarchy) continue;
                if ((uint)v.Index >= (uint)_board.Branches.Count) continue;
                var st = _board.Branches[v.Index];
                if (st == null || st.Broken || st.AdLocked || st.IsFullMatch(out _)) continue;
                for (int s = 0; s < BranchState.Cap; s++)
                {
                    var bird = v.Birds[s];
                    if (bird == null || !bird.enabled) continue;
                    if (bird.transform.parent != v.transform) continue;
                    if (((Vector2)bird.transform.position - world).sqrMagnitude <= r2)
                        return true;
                }
            }
            return false;
        }

        static bool BonusSpot(BranchView v, Vector2 world)
        {
            if (SpriteHit(v.Wood, world, 0.36f)) return true;
            if (v.Sign != null && v.Sign.gameObject.activeInHierarchy)
            {
                var sr = v.Sign.GetComponent<SpriteRenderer>();
                if (SpriteHit(sr, world, 0.42f)) return true;
            }
            return false;
        }

        static bool SpriteHit(SpriteRenderer sr, Vector2 world, float pad)
        {
            if (sr == null || !sr.enabled || sr.sprite == null) return false;
            var b = sr.bounds;
            return world.x >= b.min.x - pad && world.x <= b.max.x + pad
                && world.y >= b.min.y - pad && world.y <= b.max.y + pad;
        }

        void OpenGiftCard()
        {
            if (_gift != GiftFace.None || _won) return;
            if (!_frozen && _busy) return;
            if (_sel >= 0 && _garden.Branches != null)
            {
                _garden.Branches[_sel].SetReady(false);
                _sel = -1;
            }
            if (!_frozen) _freezeOffer = false;
            RaiseGiftCard();
            Sfx.Chirp(BirdColor.Gold);
        }

        void RaiseGiftCard()
        {
            if (_gift == GiftFace.Card) return;
            if (!GatePopup(PopupKind.Gift, false)) return;
            _gift = GiftFace.Card;
            if (_freezeOffer) _freezeShownAt = Time.unscaledTime;
            AdLog.Add("branch offer shown (" + OfferReason() + ")");
        }

        string OfferReason()
        {
            if (_keepStreak) return "keep streak";
            if (_freezeOffer || _frozen)
            {
                bool noMove = _board == null || (!_board.HasHop() && _board.FindCollect() < 0);
                return noMove ? "stuck, no moves" : "stuck, no way to win";
            }
            return "gift sign tapped";
        }

        void CloseGift()
        {
            DismissAdHand();
            if (_gift == GiftFace.Movie) return;
            _gift = GiftFace.None;
            _swallowTapsUntil = PlayClock.Now + 0.45f;
            _suppressGiftUntil = PlayClock.Now + 1f;
        }

        // The Watch ad stage. The flag marks the coroutine alive, so the interruption
        // check can tell a running stage from one that was cut off.
        IEnumerator WatchGift()
        {
            _watchGiftLive = true;
            var body = WatchGiftBody();
            try
            {
                while (body.MoveNext()) yield return body.Current;
            }
            finally
            {
                _watchGiftLive = false;
            }
        }

        IEnumerator WatchGiftBody()
        {
            bool keep = _keepStreak;
            _gift = GiftFace.Movie;
            _busy = true;
            // GateGo is splash-only (long Lead). Ad start gets a short leave whoosh.
            Sfx.FeederLeave();
            yield return Ads.Rewarded();
            // Freeze continue pays only for a finished view (or ads switched off).
            // Cancel and a show that never played leave the card up. The gift
            // sign keeps the older permissive grant.
            bool freeze = _frozen || _freezeOffer;
            bool pay = freeze
                ? BonusBranches.ContinuePays(Ads.LastEarned, Ads.LastGranted, Ads.Enabled)
                : Ads.LastGranted;
            if (!pay)
            {
                _busy = false;
                RaiseGiftCard();
                yield break;
            }
            if (keep)
            {
                yield return SnapRound();
                _gift = GiftFace.None;
                _busy = false;
                yield break;
            }
            // Ice first, then the same claim the sign uses, so the limb shakes
            // and bursts on the open garden instead of under the pane.
            if (_frozen && _garden.Ice != null)
                yield return _garden.Ice.Shatter(_garden.Root);
            var popAt = GrantSpare();
            StillBirds(false);
            _frozen = false;
            _freezeOffer = false;
            yield return GardenFit.Tween(_garden, _board, false);
            _giftThanksAt = Time.unscaledTime;
            _gift = GiftFace.Thanks;
            if (_garden.Root != null)
                StartCoroutine(GiftPopBursts(popAt));
            BeginRewardPay(0, 2.45f);
            yield return WaitRewardPay();
            _gift = GiftFace.None;
            _swallowTapsUntil = PlayClock.Now + 0.45f;
            _suppressGiftUntil = PlayClock.Now + 1f;
            _busy = false;
            Conserve("bonus");
            CheckOver();
        }

        IEnumerator GiftPopBursts(Vector3 pos)
        {
            var root = _garden.Root;
            if (root == null) yield break;
            StartCoroutine(Wow.Burst(pos, BirdColor.Gold, root, 4));
            yield return new WaitForSeconds(0.14f);
            StartCoroutine(Wow.Burst(pos + new Vector3(0f, 0.85f, 0f), BirdColor.Peach, root, 2));
            yield return new WaitForSeconds(0.16f);
            StartCoroutine(Wow.Burst(pos + new Vector3(-1.05f, 0.35f, 0f), BirdColor.Teal, root, 2));
            yield return new WaitForSeconds(0.14f);
            StartCoroutine(Wow.Burst(pos + new Vector3(1.05f, 0.35f, 0f), BirdColor.Ruby, root, 2));
        }

        Vector3 GrantSpare()
        {
            int i = BonusBranches.ContinueIndex(_board, _bonusOn, _giftBranch);
            return UnlockBonus(i);
        }

        // One grant per gift per stage. Never grows a new limb, so a later tap
        // cannot run the ad again. A stale preferred index falls through to the
        // next locked gift (freeze continue after an earlier claim).
        Vector3 UnlockBonus(int i)
        {
            int grant = BonusBranches.Claim(_board, _bonusOn, i);
            if (grant == 0)
            {
                RefreshBonusSigns();
                return Vector3.zero;
            }
            if (grant < 0) return Vector3.zero;
            // Conserve below puts short birds on the perch with the most free
            // seats. Claim opens empty first, and FindSeat will not use a gift.
            var v = i < _garden.Branches.Length ? _garden.Branches[i] : null;
            if (v != null)
            {
                var want = v.GetComponent<GiftWant>();
                if (want != null) want.On = false;
                if (v.Sign != null) v.Sign.gameObject.SetActive(false);
                v.Shake();
            }
            Sfx.FeederDone();
            var pos = v != null ? v.transform.position : Vector3.zero;
            if (_garden.Root != null)
                StartCoroutine(Wow.Burst(pos, BirdColor.Gold, _garden.Root, 3));
            SyncAll();
            Conserve("bonus");
            return pos;
        }

        void DrawGiftSign(float s)
        {
            // Film+play is painted into fx_ad_sign art — no GUI sticker overlay.
        }

        static GUIStyle _giftTitle, _giftWatch, _giftX, _freezeCont;

        const string StreakKeepTitle = "Wait!";
        static string _streakKeepBody;
        static int _streakKeepMul = -1;

        static readonly string[] _mulChip = { "x0", "x1", "x2", "x3", "x4", "x5", "x6", "x7", "x8", "x9", "x10" };

        static string MulChip(int mul)
        {
            int d = StreakTier.Display(mul);
            if ((uint)d < (uint)_mulChip.Length) return _mulChip[d];
            return "x" + d;
        }

        static string StreakKeepLine(int mul)
        {
            int shown = StreakTier.Display(mul);
            if (shown < 2) shown = 2;
            if (_streakKeepMul != shown)
            {
                _streakKeepMul = shown;
                _streakKeepBody = "Restarting resets your x" + shown + " streak. Watch an ad to keep it?";
            }
            return _streakKeepBody;
        }

        void DrawStreakCaution(Rect headR, float s, GUIStyle title)
        {
            int mul = _restartSavedMul >= 2 ? _restartSavedMul : 2;
            float d = Mathf.Min(headR.width * 0.30f, 78f * s);
            var chip = new Rect(headR.center.x - d * 0.5f, headR.y, d, d);
            string numeral = MulChip(mul);
            var ink = StreakBadge.Colors(mul);
            title.wordWrap = false;
            title.alignment = TextAnchor.MiddleCenter;
            title.fontStyle = FontStyle.Bold;
            StreakBadge.NumeralFit(chip, out float fitW, out float fitH);
            int chipHi = Mathf.Max(16, Mathf.RoundToInt(fitH));
            title.fontSize = FitFont(title, numeral, fitW, fitH, 12, chipHi);
            StreakBadge.Draw(chip, numeral, title, ink, 1f, 0.4f);
            float g = d * 0.40f;
            var tri = new Rect(chip.xMax - g * 0.55f, chip.y - g * 0.06f, g, g * 0.88f);
            DrawCautionMark(tri);
            title.fontSize = FitFont(title, "!", tri.width * 0.7f, tri.height * 0.55f, 12, Mathf.Max(14, Mathf.RoundToInt(g * 0.55f)));
            StampOutlined(new Rect(tri.x, tri.y + tri.height * 0.18f, tri.width, tri.height * 0.62f), "!", title, Color.white, 0, 2);
            float titleH = Mathf.Max(28f * s, headR.height * 0.22f);
            var titleR = new Rect(headR.x, chip.yMax + 4f * s, headR.width, titleH);
            float bodyTop = titleR.yMax + 2f * s;
            var bodyR = new Rect(headR.x, bodyTop, headR.width, Mathf.Max(20f, headR.yMax - bodyTop));
            var fill = new Color(1f, 0.98f, 0.92f, 1f);
            title.wordWrap = false;
            int hi = Mathf.Max(22, Mathf.RoundToInt(40f * s));
            DrawPopupTitle(titleR, StreakKeepTitle, title, fill, 16, hi, 1f, 0.9f, 0, 3, 0f, 0.04f, 1f, titleR.center);
            title.wordWrap = true;
            string body = StreakKeepLine(mul);
            int bodyHi = Mathf.Max(16, Mathf.RoundToInt(26f * s));
            title.fontSize = FitFontWrapped(title, body, bodyR.width, bodyR.height * 0.92f, 13, bodyHi);
            int bodyInk = Mathf.Max(2, Mathf.RoundToInt(title.fontSize * 0.12f));
            StampOutlined(bodyR, body, title, fill, 0, bodyInk);
        }

        void DrawGiftOffer(float s)
        {
            float wash = _gift == GiftFace.Card ? 0.72f
                : (_gift == GiftFace.Thanks ? 0.28f : 0.82f);
            GUI.color = new Color(0.04f, 0.03f, 0.02f, wash);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            if (_gift == GiftFace.Movie)
            {
                DrawGiftMovie(s);
                return;
            }
            if (_gift == GiftFace.Thanks)
            {
                DrawGiftThanks(s);
                return;
            }

            var safe = Screen.safeArea;
            float top = TopHud();
            var xBtn = CornerCloseRect(s, top);
            bool xHeld = false;
            if (!_freezeOffer && HitPad(CloseTapRect(xBtn), out xHeld))
            {
                DismissAdHand();
                if (_keepStreak)
                {
                    Purse.BreakStreak();
                    StartCoroutine(SnapRound());
                }
                else CloseGift();
                return;
            }

            GiftCardPlaced(s, out var card, out var cta);
            Rect cont = default;
            if (_freezeOffer) PlaceFreeze(s, out card, out cta, out cont);
            var retryRest = cta;
            float retryU = 1f;
            float contU = 1f;
            float frostA = 1f;
            if (_freezeOffer)
            {
                float age = _freezeShownAt < 0f ? 0f : Time.unscaledTime - _freezeShownAt;
                float signU = PopupMotion.Beat(age, 0f, 0.48f);
                card.y += PopupMotion.Fall(signU, -Mathf.Max(40f * s, card.height * 0.42f));
                retryU = PopupMotion.Beat(age, 0.18f, 0.32f);
                contU = PopupMotion.Beat(age, 0.30f, 0.34f);
                frostA = PopupMotion.EnterAlpha(signU);
                SparkleFx.Tick(Time.unscaledDeltaTime);
                SparkleFx.DriftSnow(new Rect(0f, 0f, Screen.width, Screen.height), FoliageSway.LowPower ? 3 : 6);
                DrawFrostVignette(s, frostA);
            }

            float t = Time.unscaledTime;
            float breathe = 0.5f + 0.5f * Mathf.Sin(t * 2.05f);
            var glow = GlowTex();
            float aura = card.width * (0.06f + 0.02f * breathe);
            GUI.color = new Color(1f, 0.78f, 0.22f, 0.14f + 0.08f * breathe);
            GUI.DrawTexture(new Rect(card.x - aura, card.y - aura * 0.6f, card.width + aura * 2f, card.height + aura * 1.2f), glow, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;

            var tex = SpriteCatalog.AdCard != null ? SpriteCatalog.AdCard.texture : null;
            if (tex != null)
            {
                GUI.color = new Color(0.10f, 0.06f, 0.03f, 0.40f);
                GUI.DrawTexture(new Rect(card.x + 6f, card.y + 14f, card.width, card.height), tex, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
                GUI.DrawTexture(card, tex, ScaleMode.ScaleToFit, true);
                QuietFrameFlowers(card);
            }

            var plate = new Rect(
                card.x + card.width * 0.13f,
                card.y + card.height * 0.22f,
                card.width * 0.74f,
                card.height * 0.54f);
            GUI.color = new Color(0.04f, 0.02f, 0.01f, 0.92f);
            GUI.DrawTexture(plate, Texture2D.whiteTexture);
            GUI.color = new Color(1f, 0.78f, 0.22f, 0.42f + 0.28f * breathe);
            GUI.DrawTexture(new Rect(plate.x + 2f * s, plate.y + 2f * s, plate.width - 4f * s, 3f * s), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(plate.x + 2f * s, plate.yMax - 5f * s, plate.width - 4f * s, 3f * s), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(plate.x + 2f * s, plate.y + 2f * s, 3f * s, plate.height - 4f * s), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(plate.xMax - 5f * s, plate.y + 2f * s, 3f * s, plate.height - 4f * s), Texture2D.whiteTexture);
            GUI.color = Color.white;

            string head = _freezeOffer
                ? "No more moves, you're frozen!"
                : "Watch AD for an extra branch";
            // Inset past the screw bases so the gift headline never sits under a bulb.
            // The freeze sign has no bulbs, so its line uses that same inset on every side.
            float insetX = Mathf.Max(12f * s, plate.width * 0.08f);
            float insetY = Mathf.Max(10f * s, plate.height * 0.12f);
            float topInset = insetY;
            var headR = new Rect(plate.x + insetX, plate.y + topInset, plate.width - insetX * 2f, plate.height - topInset - insetY);
            GUI.color = new Color(1f, 0.72f, 0.16f, 0.40f + 0.22f * breathe);
            GUI.DrawTexture(new Rect(headR.x - 8f * s, headR.y - 6f * s, headR.width + 16f * s, headR.height + 12f * s), glow, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
            if (_giftTitle == null)
                _giftTitle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };
            var title = _giftTitle;
            title.fontStyle = FontStyle.Bold;
            title.wordWrap = true;
            if (_keepStreak)
                DrawStreakCaution(headR, s, title);
            else
            {
                if (_freezeOffer)
                {
                    var cap = CoachLineStyle();
                    if (cap != null && cap.font != null) title.font = cap.font;
                }
                int headLo = _freezeOffer
                    ? Mathf.Max(15, Mathf.RoundToInt(17f * s))
                    : Mathf.Max(14, Mathf.RoundToInt(16f * s));
                int headHi = Mathf.Max(headLo + 4, Mathf.RoundToInt(40f * s));
                title.fontSize = FitFontWrapped(title, head, headR.width, headR.height * 0.92f, headLo, headHi);
                var headContent = GuiPool.Text(GuiText.GiftHead, head);
                int headGuard = 0;
                while (title.fontSize > 8 && headGuard < 28
                    && title.CalcHeight(headContent, headR.width) > headR.height * 0.96f)
                {
                    title.fontSize -= 1;
                    headGuard++;
                }
                int headStroke = Mathf.Max(2, Mathf.RoundToInt(title.fontSize * 0.10f));
                StampOutlined(headR, head, title, new Color(1f, 0.96f, 0.72f), 1, headStroke);
            }

            // Fewer, smaller lamps than the shared 16-bulb floor. FitMarquee keeps
            // the ring mirrored and evenly spaced. The freeze page has no bulbs.
            if (_freezeOffer)
                DrawIceCrystals(card, s, frostA);
            else
                DrawGiftMarquee(plate, s, t, 1f, -1f, 0f, 10, 0.048f, false, 0.16f, 0f, card);

            if (_freezeOffer)
            {
                bool retryReady = retryU > 0.62f;
                bool contReady = contU > 0.62f;
                var retryDisc = FlowerDisc(retryRest, 0f);
                float pad = 8f * s;
                var retryHit = new Rect(retryDisc.x - pad, retryDisc.y - pad * 0.35f, retryDisc.width + pad * 2f, retryDisc.height + pad * 0.7f);
                // Held stays false when the pop has not opened the hit yet.
                bool retryHeld = false;
                bool contHeld = false;
                bool retry = retryReady && HitPad(retryHit, out retryHeld);
                bool contGo = contReady && HitPad(cont, out contHeld);
                float retryPop = PopupMotion.PopScale(retryU);
                if (retryPop > 0.04f)
                {
                    var prev = GUI.matrix;
                    GUIUtility.ScaleAroundPivot(new Vector2(retryPop, retryPop), retryRest.center);
                    DrawFreezeRetry(retryRest, retryHeld, s);
                    GUI.matrix = prev;
                }
                float pulse = 1f;
                if (!contHeld && contU >= 1f)
                    pulse = 1f + 0.028f * Mathf.Sin(t * 2.5f);
                float contPop = PopupMotion.PopScale(contU) * pulse;
                if (contPop > 0.04f)
                    DrawFreezeWatch(cont, contHeld, s, contPop);
                if (GuiPaint()) SparkleFx.DrawDrift(frostA);
                if (retry)
                {
                    DismissAdHand();
                    Restart();
                    return;
                }
                if (contGo)
                {
                    DismissAdHand();
                    StartCoroutine(WatchGift());
                    return;
                }
                return;
            }

            var discHit = FlowerHit(cta, s);
            bool watch = HitPad(discHit, out bool held);
            var pressDisc = FlowerDisc(cta, 0f);
            if (!held && GlovePresses(pressDisc)) held = true;
            var disc = DrawPopupButton(cta, held, true, 0.35f);
            if (_giftWatch == null)
                _giftWatch = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = false
                };
            var watchSt = _giftWatch;
            watchSt.fontStyle = FontStyle.Bold;
            string watchLab = "Watch";
            // Round cap is inset from FlowerDisc. Fit inside it, then leave the outline's pixels.
            int ink = 1;
            float capW = Mathf.Max(24f, disc.width * 0.58f - ink * 4f);
            float capH = disc.height * 0.42f;
            int watchHi = Mathf.Max(16, Mathf.RoundToInt(26f * s));
            watchSt.fontSize = FitFont(watchSt, watchLab, capW, capH, 8, watchHi);
            var watchContent = GuiPool.Text(GuiText.GiftWatch, watchLab);
            int watchGuard = 0;
            while (watchSt.fontSize > 8 && watchGuard < 24
                && watchSt.CalcSize(watchContent).x > capW)
            {
                watchSt.fontSize -= 1;
                watchGuard++;
            }
            StampOutlined(disc, watchLab, watchSt, new Color(0.28f, 0.12f, 0.04f), 1, ink);
            // Sentence first, glove last. The Watch hit is already stored, so painting
            // here does not change which control won the click.
            if (!_freezeOffer) DrawGiftCloseX(xBtn, xHeld, s);
            if (_adHand)
            {
                DrawSplashIntroLine(AdHandLine, AdHandCaptionRect(s), s);
                DrawTutorOverlay(s);
            }
            if (watch)
            {
                DismissAdHand();
                StartCoroutine(WatchGift());
                return;
            }
        }

        // Frozen board: the wood sign, the round Retry, then a wide green
        // continue bar. The stack stays above the hive and the back arrow.
        static void PlaceFreeze(float s, out Rect card, out Rect retry, out Rect cont)
        {
            HudLayout(out _, out _, out _, out var restart, out var hive);
            float topLimit = TopHud() + 6f * s;
            float botLimit = Mathf.Min(restart.y, hive.y) - 12f * s;
            if (botLimit < topLimit + 48f) botLimit = topLimit + 48f;
            float room = botLimit - topLimit;

            float cardW = StandardPopupWidth(s);
            float cardH = cardW * (501f / 780f);
            float retrySz = PopupButtonSize(s, cardW);
            float overlap = retrySz * 0.36f;
            float contH = Mathf.Max(74f * s, PopupBarHeight(s, cardW) * 1.9f);
            float contW = Mathf.Min(cardW * 0.98f, Screen.width - 20f * s);
            if (contW < 8f) contW = 8f;
            float gap = 12f * s;
            float stack = cardH + retrySz - overlap + gap + contH;
            if (stack > room && room > 1f)
            {
                float fit = room / stack;
                if (fit < 0.62f) fit = 0.62f;
                cardW *= fit;
                cardH *= fit;
                retrySz *= fit;
                overlap = retrySz * 0.36f;
                contW *= fit;
                contH = Mathf.Max(60f * s, contH * fit);
                stack = cardH + retrySz - overlap + gap + contH;
                if (stack > room)
                {
                    float cut = stack - room;
                    float cardCut = Mathf.Min(cut, cardH * 0.22f);
                    cardH -= cardCut;
                    cut -= cardCut;
                    if (cut > 0f) contH = Mathf.Max(56f * s, contH - cut);
                    stack = cardH + retrySz - overlap + gap + contH;
                }
            }
            float cardY = botLimit - stack;
            if (cardY < topLimit) cardY = topLimit;
            card = new Rect((Screen.width - cardW) * 0.5f, cardY, cardW, cardH);
            retry = new Rect((Screen.width - retrySz) * 0.5f, card.yMax - overlap, retrySz, retrySz);
            cont = new Rect((Screen.width - contW) * 0.5f, retry.yMax + gap, contW, contH);
            if (cont.yMax > botLimit)
            {
                float shift = cont.yMax - botLimit;
                card.y -= shift;
                retry.y -= shift;
                cont.y -= shift;
            }
            if (card.y < topLimit)
            {
                float push = topLimit - card.y;
                float roomLeft = botLimit - cont.yMax;
                if (push > roomLeft) push = roomLeft > 0f ? roomLeft : 0f;
                card.y += push;
                retry.y += push;
                cont.y += push;
            }
        }

        static void DrawFreezeRetry(Rect flower, bool held, float s)
        {
            var disc = DrawPopupButton(flower, held, true, 0.35f);
            if (_giftWatch == null)
                _giftWatch = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = false
                };
            var st = _giftWatch;
            st.fontStyle = FontStyle.Bold;
            const string lab = "Retry";
            int ink = 1;
            float capW = Mathf.Max(24f, disc.width * 0.62f - ink * 4f);
            float capH = disc.height * 0.46f;
            int hi = Mathf.Max(16, Mathf.RoundToInt(30f * s));
            st.fontSize = FitFont(st, lab, capW, capH, 8, hi);
            var content = GuiPool.Text(GuiText.FreezeRetry, lab);
            int guard = 0;
            while (st.fontSize > 8 && guard < 24 && st.CalcSize(content).x > capW)
            {
                st.fontSize -= 1;
                guard++;
            }
            StampOutlined(disc, lab, st, new Color(0.28f, 0.12f, 0.04f), 1, ink);
        }

        // Wide green continue. Same clay as the restart Keep button. Play mark
        // on the left, two lines of the shared caption face.
        static void DrawFreezeWatch(Rect bar, bool held, float s, float pop)
        {
            var prev = GUI.matrix;
            GUIUtility.ScaleAroundPivot(new Vector2(pop, pop), bar.center);
            if (!held)
            {
                float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.5f);
                var glow = GlowTex();
                float pad = bar.height * (0.16f + 0.05f * wave);
                GUI.color = new Color(0.50f, 0.95f, 0.58f, 0.18f + 0.10f * wave);
                GUI.DrawTexture(new Rect(bar.x - pad, bar.y - pad * 0.45f, bar.width + pad * 2f, bar.height + pad), glow, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
            }
            var face = DrawPopupButton(bar, held, PopupTint.Green);
            DrawPlayAdIcon(new Rect(face.x, face.center.y - face.height * 0.42f, face.height * 0.84f, face.height * 0.84f));
            float textX = face.x + face.height * 0.92f;
            var words = new Rect(textX, face.y, Mathf.Max(8f, face.xMax - textX), face.height);
            // Two bands with a gap so the title outline does not sit on the bonus line.
            float gap = Mathf.Max(2f, words.height * 0.04f);
            float titleH = (words.height - gap) * 0.58f;
            var titleR = new Rect(words.x, words.y, words.width, titleH);
            var subR = new Rect(words.x, titleR.yMax + gap, words.width, Mathf.Max(8f, words.yMax - titleR.yMax - gap));
            if (_giftWatch == null)
                _giftWatch = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = false
                };
            var st = _giftWatch;
            var cap = CoachLineStyle();
            if (cap != null && cap.font != null) st.font = cap.font;
            st.fontStyle = FontStyle.Bold;
            st.alignment = TextAnchor.MiddleCenter;
            st.wordWrap = false;
            const string watch = "Watch Ad to Continue";
            int hi = Mathf.Max(16, Mathf.RoundToInt(26f * s));
            st.fontSize = FitFont(st, watch, titleR.width, titleR.height, 11, hi);
            var content = GuiPool.Text(GuiText.FreezeContinue, watch);
            int guard = 0;
            while (st.fontSize > 10 && guard < 20 && st.CalcSize(content).x > titleR.width)
            {
                st.fontSize -= 1;
                guard++;
            }
            int ink = Mathf.Clamp(Mathf.RoundToInt(st.fontSize * 0.14f), 2, 6);
            StampOutlined(titleR, watch, st, Color.white, 0, ink);
            if (_freezeCont == null)
                _freezeCont = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = false
                };
            var sub = _freezeCont;
            if (cap != null && cap.font != null) sub.font = cap.font;
            sub.fontStyle = FontStyle.Bold;
            sub.alignment = TextAnchor.MiddleCenter;
            sub.wordWrap = false;
            const string bonus = "+ Bonus Branch";
            int subHi = Mathf.Max(12, Mathf.RoundToInt(st.fontSize * 0.72f));
            sub.fontSize = FitFont(sub, bonus, subR.width, subR.height, 10, subHi);
            var subContent = GuiPool.Text(GuiText.FreezeBonus, bonus);
            int subGuard = 0;
            while (sub.fontSize > 9 && subGuard < 16 && sub.CalcSize(subContent).x > subR.width)
            {
                sub.fontSize -= 1;
                subGuard++;
            }
            StampOutlined(subR, bonus, sub, new Color(0.93f, 1f, 0.86f, 1f), 0, Mathf.Max(1, ink - 1));
            GUI.matrix = prev;
            GUI.color = Color.white;
        }

        static Texture2D _playAdIcon;

        static void DrawPlayAdIcon(Rect r)
        {
            if (r.width < 4f || r.height < 4f) return;
            var tex = PlayAdIcon();
            GUI.color = Color.white;
            GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit, true);
        }

        static Texture2D PlayAdIcon()
        {
            if (_playAdIcon != null) return _playAdIcon;
            const int n = 48;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = "PlayAd"
            };
            var px = new Color32[n * n];
            float cx = n * 0.46f;
            float cy = n * 0.50f;
            float rad = n * 0.36f;
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float nx = (x + 0.5f - cx) / rad;
                float ny = (y + 0.5f - cy) / rad;
                const float left = -0.70f;
                const float right = 0.96f;
                float t = (nx - left) / (right - left);
                if (t < 0f || t > 1f) continue;
                if (Mathf.Abs(ny) > (1f - t) * 0.92f) continue;
                px[y * n + x] = new Color32(255, 255, 255, 255);
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _playAdIcon = tex;
            return tex;
        }

        static void DrawFrostVignette(float s, float alpha)
        {
            if (alpha < 0.02f) return;
            var glow = GlowTex();
            if (glow == null) return;
            float w = Screen.width;
            float h = Screen.height;
            float d = Mathf.Max(w, h) * 0.62f;
            GUI.color = new Color(0.68f, 0.88f, 1f, 0.18f * alpha);
            GUI.DrawTexture(new Rect(-d * 0.32f, -d * 0.34f, d, d), glow, ScaleMode.ScaleToFit, true);
            GUI.DrawTexture(new Rect(w - d * 0.68f, -d * 0.34f, d, d), glow, ScaleMode.ScaleToFit, true);
            GUI.DrawTexture(new Rect(-d * 0.32f, h - d * 0.66f, d, d), glow, ScaleMode.ScaleToFit, true);
            GUI.DrawTexture(new Rect(w - d * 0.68f, h - d * 0.66f, d, d), glow, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
        }

        // Angles are fixed. Positions are computed in the loop so OnGUI does not allocate.
        static readonly float[] IceAng = { -16f, 22f, 14f, -26f };

        static void DrawIceCrystals(Rect card, float s, float alpha)
        {
            if (alpha < 0.02f) return;
            var spr = SpriteCatalog.IceShard;
            if (spr == null || spr.texture == null) return;
            float sz = Mathf.Clamp(card.width * 0.10f, 16f * s, 40f * s);
            var tex = spr.texture;
            var prev = GUI.matrix;
            GUI.color = new Color(0.88f, 0.96f, 1f, 0.78f * alpha);
            for (int i = 0; i < IceAng.Length; i++)
            {
                float x;
                float y;
                if (i == 0) { x = card.x + sz * 0.15f; y = card.y + sz * 0.05f; }
                else if (i == 1) { x = card.xMax - sz * 0.2f; y = card.y + sz * 0.2f; }
                else if (i == 2) { x = card.x + sz * 0.05f; y = card.yMax - sz * 0.35f; }
                else { x = card.xMax - sz * 0.1f; y = card.yMax - sz * 0.15f; }
                var at = new Vector2(x, y);
                GUI.matrix = prev;
                GUIUtility.RotateAroundPivot(IceAng[i], at);
                GUI.DrawTexture(new Rect(at.x - sz * 0.5f, at.y - sz * 0.5f, sz, sz * 0.72f), tex, ScaleMode.ScaleToFit, true);
            }
            GUI.matrix = prev;
            GUI.color = Color.white;
        }

        // Card plus the flower the Watch label sits on. Shared with the ad-hand aim
        // and the VIP offer.
        static void GiftCardLayout(float s, out Rect card, out Rect flower)
        {
            float cardW = StandardPopupWidth(s);
            float cardH = cardW * (501f / 780f);
            float flowerSz = PopupButtonSize(s, cardW);
            // Same overlap the daily claim uses. The disc still clears the plaque.
            float overlap = PopupCtaOverlap(flowerSz, 0f);
            float stackH = cardH + flowerSz - overlap;
            var placed = PlacePopup(s, cardW, stackH, 0.32f);
            card = new Rect(placed.x, placed.y, cardW, cardH);
            flower = new Rect((Screen.width - flowerSz) * 0.5f, card.yMax - overlap, flowerSz, flowerSz);
            float bot = Screen.height - Mathf.Max(8f, Screen.safeArea.yMin + 6f);
            if (flower.yMax > bot)
            {
                float shift = flower.yMax - bot;
                card.y -= shift;
                flower.y -= shift;
            }
            float hud = TopHud() + 8f * s;
            if (card.y < hud)
            {
                float push = hud - card.y;
                float room = bot - flower.yMax;
                if (push > room) push = room > 0f ? room : 0f;
                card.y += push;
                flower.y += push;
            }
        }

        // Same stack as GiftCardLayout. The bonus sign stays at that higher
        // seat; the coach line anchors above it and does not push the card down.
        void GiftCardPlaced(float s, out Rect card, out Rect flower)
        {
            GiftCardLayout(s, out card, out flower);
        }

        // Orchids live in the card art, outside the chalkboard. A soft wash over
        // those corners only; the plate drawn next covers any spill onto the sign.
        static void QuietFrameFlowers(Rect card)
        {
            var glow = GlowTex();
            float d = card.width * 0.50f;
            GUI.color = new Color(0.05f, 0.03f, 0.025f, 0.38f);
            GUI.DrawTexture(
                new Rect(card.x + card.width * 0.20f - d * 0.5f, card.y + card.height * 0.18f - d * 0.38f, d, d * 0.78f),
                glow, ScaleMode.ScaleToFit, true);
            GUI.DrawTexture(
                new Rect(card.x + card.width * 0.82f - d * 0.5f, card.y + card.height * 0.18f - d * 0.38f, d, d * 0.78f),
                glow, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
        }

        // Shared close button (Daily Bonus, Go VIP, gift / freeze card, restart-streak card).
        // CornerCloseRect seats the screen-corner one; CloseTapRect is the hit for all of them.
        // Build 63 (Brandon): the drawn x is 1.5x. Only the glyph grows: it is fitted in a
        // label box CloseGlyphGrow times the button, centred, so its ink (about 0.6 of the
        // button side) stays inside the button and the dark disc. The button rect, and so
        // every seat next to the logo or a card, is unchanged.
        const float CloseGlyphGrow = 1.5f;

        // 44 pt in pixels. dpi / 150 rounds up on 2x (326 dpi) and 3x (460 dpi) iPhones.
        static float MinTapPx()
        {
            float dpi = Screen.dpi;
            float pt = dpi > 1f ? Mathf.Max(1f, dpi / 150f) : Mathf.Max(1f, Screen.height / 720f);
            return 44f * pt;
        }

        static Rect CornerCloseRect(float s, float top)
        {
            var safe = Screen.safeArea;
            float xSz = Mathf.Max(48f * s, 44f);
            return new Rect(Screen.width - Mathf.Max(14f, Screen.width - safe.xMax + 8f) - xSz, top, xSz, xSz);
        }

        // Tap area at least MinTapPx on each side, centred on the drawn button.
        static Rect CloseTapRect(Rect xBtn)
        {
            float min = MinTapPx();
            float w = Mathf.Max(xBtn.width, min);
            float h = Mathf.Max(xBtn.height, min);
            return new Rect(xBtn.center.x - w * 0.5f, xBtn.center.y - h * 0.5f, w, h);
        }

        static void DrawGiftCloseX(Rect xBtn, bool held, float s)
        {
            var glow = GlowTex();
            GUI.color = new Color(0.04f, 0.03f, 0.02f, held ? 0.62f : 0.38f);
            GUI.DrawTexture(xBtn, glow, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
            if (_giftX == null)
                _giftX = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = false
                };
            var xLab = _giftX;
            int xLo = Mathf.Max(18, Mathf.RoundToInt(22f * s));
            int xHi = Mathf.Max(xLo + 4, Mathf.RoundToInt(36f * s * CloseGlyphGrow));
            float gw = xBtn.width * CloseGlyphGrow;
            float gh = xBtn.height * CloseGlyphGrow;
            var glyph = new Rect(xBtn.center.x - gw * 0.5f, xBtn.center.y - gh * 0.5f, gw, gh);
            xLab.fontSize = FitFont(xLab, "×", glyph.width * 0.84f, glyph.height * 0.84f, 8, xHi);
            int xInk = Mathf.Clamp(Mathf.RoundToInt(xLab.fontSize * 0.12f), 2, 8);
            StampOutlined(glyph, "×", xLab, new Color(1f, 0.94f, 0.62f, held ? 1f : 0.96f), 2, xInk);
        }

        // `plate` is the OUTER frame rect. grow < 0 keeps that edge; a positive grow
        // is only an explicit extra rim. The screw base sits just outside that edge.
        // bulbFrac > 0 sizes every bulb from the plate (streak sign, already scaled).
        // bulbFrac == 0 keeps the 16-bulb gift floor unless lampWant/lampFrac override it.
        // pinFrac stays in the signature. It is not a seat offset: pulling the lip
        // onto the rim parks the screw on the frame face.
        // glassPx > 0 replaces that glass size (daily and welcome pass a smaller glass).
        // flowerCard: fx_ad_card rect. Bulbs whose body meets a corner orchid are skipped.
        static void DrawGiftMarquee(Rect plate, float s, float t, float alpha = 1f, float grow = -1f, float bulbFrac = 0f, int lampWant = 16, float lampFrac = 0f, bool steady = false, float pinFrac = 0.16f, float glassPx = 0f, Rect flowerCard = default)
        {
            var bulb = SpriteCatalog.AdBulb;
            var glow = GlowTex();
            var tex = bulb != null ? bulb.texture : null;
            if (grow < 0f) grow = 0f;
            if (pinFrac < 0f) pinFrac = 0f;
            var frame = new Rect(plate.x - grow, plate.y - grow, plate.width + grow * 2f, plate.height + grow * 2f);
            float perim = 2f * (frame.width + frame.height);
            int want = 16;
            float szOn = Mathf.Max(30f * s, plate.width * 0.085f);
            float haloMul = 1f;
            if (bulbFrac > 0f)
            {
                szOn = plate.width * bulbFrac;
                int fitN = Mathf.FloorToInt(perim * 0.60f / Mathf.Max(1f, szOn));
                want = Mathf.Clamp(fitN, 8, 16);
            }
            else if (lampWant != 16 || lampFrac > 0f)
            {
                if (lampWant >= 8) want = Mathf.Clamp(lampWant, 8, 16);
                if (lampFrac > 0f)
                {
                    szOn = Mathf.Max(13f * s, plate.width * lampFrac);
                    haloMul = 0.72f;
                }
            }
            if (glassPx > 0f) szOn = glassPx;
            WorldBuilder.FitMarquee(frame.width, frame.height, want, 0f, out int hSegs, out int vSegs);
            int n = WorldBuilder.MarqueeCount(hSegs, vSegs);
            float pitch = Mathf.Min(frame.width / hSegs, frame.height / vSegs);
            float cap = pitch * 0.62f;
            if (szOn > cap) szOn = cap;
            float szOff = szOn * 0.82f;
            for (int i = 0; i < n; i++)
            {
                WorldBuilder.MarqueeSpot(frame.x, frame.y, frame.xMax, frame.yMax, hSegs, vSegs, i, out var p, out float ang);
                float lit = MarqueeLit(i, n, t);
                float sz = steady ? szOn : Mathf.Lerp(szOff, szOn, lit);
                WorldBuilder.SeatBulb(p, ang, sz * 0.5f, true, out var seat, out float spin);
                if (BulbOnCornerFlower(seat, flowerCard, sz)) continue;
                DrawMarqueeLamp(p, seat, spin, WorldBuilder.MarqueeOutward(ang), sz, lit, alpha, haloMul, tex, glow);
            }
            GUI.color = Color.white;
        }

        // One readable chase (~2.6s) plus a slower counter-glow.
        static float MarqueeLit(int i, int n, float t)
        {
            float u = n > 0 ? i / (float)n : 0f;
            float headCw = Mathf.Repeat(t * 0.38f, 1f);
            float headCcw = Mathf.Repeat(-t * 0.15f, 1f);
            float pulse = Mathf.Repeat(t, 4.6f);
            float bloom = 0f;
            if (pulse < 0.62f)
            {
                float up = Mathf.SmoothStep(0f, 1f, pulse / 0.16f);
                float down = Mathf.SmoothStep(1f, 0f, (pulse - 0.16f) / 0.46f);
                bloom = up * down;
            }
            float ring = Mathf.Min(Mathf.Abs(u - headCw), 1f - Mathf.Abs(u - headCw));
            float ringB = Mathf.Min(Mathf.Abs(u - headCcw), 1f - Mathf.Abs(u - headCcw));
            float comet = Mathf.Max(
                Mathf.Clamp01(1f - ring * n / 2.6f),
                0.55f * Mathf.Clamp01(1f - ringB * n / 2.1f));
            float idle = 0.20f + 0.10f * (0.5f + 0.5f * Mathf.Sin(t * 1.6f + i * 0.55f));
            float lit = Mathf.Max(idle, comet);
            return Mathf.Lerp(lit, 1f, bloom * 0.80f);
        }

        // Screw is the top of fx_ad_bulb and points at the frame. The quad's inner edge
        // is `rim`; the glass hangs outward. Halo stays outside that edge.
        static void DrawMarqueeLamp(Vector2 rim, Vector2 center, float spin, Vector2 outward, float sz, float lit, float alpha, float haloMul, Texture tex, Texture glow)
        {
            if (sz < 1f) return;
            float sock = sz * 0.34f;
            var sockC = rim + outward * (sock * 0.5f);
            GUI.color = new Color(0.16f, 0.09f, 0.03f, 0.92f * alpha);
            GUI.DrawTexture(new Rect(sockC.x - sock * 0.5f, sockC.y - sock * 0.5f, sock, sock), glow, ScaleMode.ScaleToFit, true);
            var glass = rim + outward * (sz * 0.62f);
            float halo = Mathf.Lerp(1.05f, 1.55f, lit) * sz * haloMul;
            float room = sz * 1.20f;
            if (halo > room) halo = room;
            var glowCol = Color.Lerp(
                new Color(1f, 0.48f, 0.10f, 0.18f),
                new Color(1f, 0.92f, 0.38f, 1f),
                lit);
            glowCol.a *= alpha;
            GUI.color = glowCol;
            GUI.DrawTexture(new Rect(glass.x - halo * 0.5f, glass.y - halo * 0.5f, halo, halo), glow, ScaleMode.ScaleToFit, true);
            var prev = GUI.matrix;
            GUIUtility.RotateAroundPivot(spin, center);
            var r = new Rect(center.x - sz * 0.5f, center.y - sz * 0.5f, sz, sz);
            var bulbCol = Color.Lerp(new Color(0.86f, 0.60f, 0.30f, 1f), Color.white, lit);
            bulbCol.a *= alpha;
            GUI.color = bulbCol;
            if (tex != null) GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit, true);
            else GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.matrix = prev;
        }

        // Corner orchids and bottom petals on fx_ad_card. An empty card skips nothing,
        // so a brass sign with no flowers keeps its corner bulbs.
        static bool BulbOnCornerFlower(Vector2 p, Rect card, float sz)
        {
            if (card.width < 8f || card.height < 8f) return false;
            float reach = Mathf.Max(0f, sz) * 0.5f;
            var body = new Rect(p.x - reach, p.y - reach, Mathf.Max(1f, sz), Mathf.Max(1f, sz));
            float topW = card.width * 0.34f;
            float topH = card.height * 0.46f;
            float botW = card.width * 0.28f;
            float botH = card.height * 0.32f;
            var topLeft = new Rect(card.x, card.y, topW, topH);
            var topRight = new Rect(card.xMax - topW, card.y, topW, topH);
            var botLeft = new Rect(card.x, card.yMax - botH, botW, botH);
            var botRight = new Rect(card.xMax - botW, card.yMax - botH, botW, botH);
            if (body.Overlaps(topLeft)) return true;
            if (body.Overlaps(topRight)) return true;
            if (body.Overlaps(botLeft)) return true;
            if (body.Overlaps(botRight)) return true;
            return false;
        }

        void DrawGiftMovie(float s)
        {
            float w = StandardPopupWidth(s);
            float h = w * 0.56f;
            var stage = PlacePopup(s, w, h, 0.38f);
            GUI.color = new Color(0.12f, 0.08f, 0.04f, 0.92f);
            GUI.DrawTexture(stage, Texture2D.whiteTexture);
            GUI.color = new Color(0.82f, 0.62f, 0.28f, 1f);
            GUI.DrawTexture(new Rect(stage.x - 4f, stage.y - 4f, stage.width + 8f, 4f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(stage.x - 4f, stage.yMax, stage.width + 8f, 4f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(stage.x - 4f, stage.y, 4f, stage.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(stage.xMax, stage.y, 4f, stage.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            float sheen = Mathf.Repeat(Time.unscaledTime * 0.55f, 1.4f);
            if (sheen < 1f)
            {
                float fade = Mathf.Sin(sheen * Mathf.PI);
                var band = new Rect(stage.x + stage.width * (sheen * 1.1f - 0.2f), stage.y, stage.width * 0.22f, stage.height);
                GUI.color = new Color(1f, 0.92f, 0.7f, 0.18f * fade);
                GUI.DrawTexture(band, Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            var st = GuiPool.Label(GuiSlot.GiftMovie);
            st.fontStyle = FontStyle.BoldAndItalic;
            st.alignment = TextAnchor.MiddleCenter;
            st.wordWrap = true;
            st.fontSize = Mathf.RoundToInt(26 * s);
            StampOutlined(stage, "Playing…", st, new Color(1f, 0.92f, 0.72f), 2, 1);
        }

        void DrawGiftThanks(float s)
        {
            float u = Mathf.Max(0f, Time.unscaledTime - _giftThanksAt);
            DrawGiftConfetti(u, s);

            float pop = Mathf.Clamp01(u / 0.22f);
            pop = pop * pop * (3f - 2f * pop);
            float punch = 1f + 0.16f * Mathf.Sin(Mathf.Clamp01(u / 0.32f) * Mathf.PI);
            float scale = Mathf.Lerp(0.52f, 1f, pop) * punch;
            float breathe = 0.5f + 0.5f * Mathf.Sin(u * 3.4f);

            string lab = "It's yours.";
            var st = GuiPool.Label(GuiSlot.GiftThanks);
            st.fontStyle = FontStyle.Bold;
            st.alignment = TextAnchor.MiddleCenter;
            st.wordWrap = false;
            float w = Screen.width * 0.92f;
            float h = 120f * s;
            var r = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.34f, w, h);
            st.fontSize = FitFont(st, lab, r.width * 0.92f, r.height * 0.82f, 42, 92);
            var glow = GlowTex();
            GUI.color = new Color(1f, 0.78f, 0.22f, (0.32f + 0.22f * breathe) * pop);
            float aura = 48f * s;
            GUI.DrawTexture(new Rect(r.x - aura, r.y - aura * 0.4f, r.width + aura * 2f, r.height + aura * 0.8f), glow, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
            var prev = GUI.matrix;
            GUIUtility.ScaleAroundPivot(new Vector2(scale, scale), r.center);
            int stroke = Mathf.Max(4, Mathf.RoundToInt(st.fontSize * 0.12f));
            StampOutlined(r, lab, st, new Color(1f, 0.90f, 0.28f), 1, stroke);
            GUI.matrix = prev;
        }

        static void DrawGiftConfetti(float u, float s) =>
            DrawGiftConfetti(u, s, new Vector2(Screen.width * 0.5f, Screen.height * 0.40f));

        static void DrawGiftConfetti(float u, float s, Vector2 origin)
        {
            var pink = SpriteCatalog.PetalPink;
            var peach = SpriteCatalog.PetalPeach;
            var spark = SpriteCatalog.Sparkle;
            var glow = GlowTex();
            var pinkTex = pink != null ? pink.texture : null;
            var peachTex = peach != null ? peach.texture : null;
            var sparkTex = spark != null && spark.texture != null ? spark.texture : glow;
            const int n = 52;
            for (int i = 0; i < n; i++)
            {
                float wave = (i % 3) * 0.15f;
                float t = u - wave;
                if (t < 0f || t > 1.20f) continue;
                float pop = Mathf.Clamp01(t / 0.62f);
                pop = pop * (2f - pop);
                float hbit = (i * 17 + 31) * 0.137f;
                hbit = hbit - Mathf.Floor(hbit);
                float h2 = (i * 53 + 11) * 0.091f;
                h2 = h2 - Mathf.Floor(h2);
                float ang = hbit * Mathf.PI * 2f + i * 0.21f;
                float side = (i % 3) - 1f;
                float ox = origin.x + side * Screen.width * 0.16f;
                float oy = origin.y - (i % 5) * 8f * s;
                float speed = (110f + 170f * hbit) * s;
                float grav = 220f * pop * pop * s;
                float px = ox + Mathf.Cos(ang) * speed * pop;
                float py = oy + Mathf.Sin(ang) * speed * 0.72f * pop + grav;
                float sz = Mathf.Lerp(38f, 10f, pop) * (0.55f + 0.90f * h2) * s;
                float spin = (i % 2 == 0 ? 280f : -240f) * pop;
                float a = (1f - Mathf.Clamp01((t - 0.55f) / 0.65f)) * (0.70f + 0.30f * Mathf.Sin(pop * Mathf.PI));
                if (a < 0.03f) continue;
                var bit = new Rect(px - sz * 0.5f, py - sz * 0.5f, sz, sz);
                var prev = GUI.matrix;
                GUIUtility.RotateAroundPivot(spin, bit.center);
                int kind = i % 5;
                if (kind == 0 && pinkTex != null)
                {
                    GUI.color = new Color(1f, 0.72f, 0.82f, a);
                    GUI.DrawTexture(bit, pinkTex, ScaleMode.ScaleToFit, true);
                }
                else if (kind == 1 && peachTex != null)
                {
                    GUI.color = new Color(1f, 0.78f, 0.52f, a);
                    GUI.DrawTexture(bit, peachTex, ScaleMode.ScaleToFit, true);
                }
                else if (kind == 2 && sparkTex != null)
                {
                    GUI.color = new Color(1f, 0.92f, 0.45f, a * 0.95f);
                    GUI.DrawTexture(bit, sparkTex, ScaleMode.ScaleToFit, true);
                }
                else
                {
                    GUI.color = new Color(1f, 0.86f, 0.28f, a * 0.88f);
                    GUI.DrawTexture(bit, glow != null ? glow : Texture2D.whiteTexture, ScaleMode.ScaleToFit, true);
                }
                GUI.matrix = prev;
            }
            GUI.color = Color.white;
        }
    }
}





