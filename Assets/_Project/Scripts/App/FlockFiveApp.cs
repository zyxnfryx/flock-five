using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlockFive
{
    public sealed class FlockFiveApp : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBoot()
        {
            if (FindAnyObjectByType<FlockFiveApp>() != null) return;
            var go = new GameObject("FlockFiveApp");
            DontDestroyOnLoad(go);
            go.AddComponent<FlockFiveApp>();
        }

        Board _board;
        WorldBuilder.Garden _garden;
        int _sel = -1;
        bool _busy;
        bool _won;
        Board _seed;
        bool _keepStreak;
        struct CoinFly
        {
            public Vector2 A, B;
            public float T, Delay, Spin;
        }
        readonly System.Collections.Generic.List<CoinFly> _flies = new System.Collections.Generic.List<CoinFly>();
        // Piggy coin: hold with random wait, then a slow Y-flip.
        float _coinSpinT = -1f;
        float _coinHoldLeft;
        float _coinHoldAge;
        bool _coinArmed;
        // Splash streak neon: <0 idle; 0..1 sign-on, calc, roll, tuck to pig.
        float _streakSlide = -1f;
        int _streakAnnounced = -1;
        bool _streakChirped;
        bool _streakWinChimed;
        int _streakRollShown;
        float _pigBurst;
        float _pigJiggle;
        readonly HashSet<int> _locked = new HashSet<int>();
        int _combo;
        float _comboUntil = -99f;
        bool _collecting;
        bool _pestsArmed;
        float _nextTap;
#if UNITY_EDITOR
        bool _finalePreview;
#endif
        bool _splash = true;
        bool _levelHive;
        enum HomeFace { Splash, Hive, Poker }
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
        int _pokerHover = -1;
        bool _pokerKeepHint;
        bool _pokerShowPay;
        bool _pokerPendingStamp;
        int _pokerResultCue;
        bool _pokerStamp;
        float _pokerStampT;
        int _pokerStampKind = -1;
        bool _pokerPayOpen;
        float _pokerPayAnim;
        bool _pokerPayJewelHeld;
        bool _pokerBingo;
        enum GiftFace { None, Card, Movie, Thanks }
        GiftFace _gift;
        float _giftThanksAt;
        bool _frozen;
        bool _freezeOffer;
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
            public BirdColor Col;
        }
        SplashFlutter[] _splashFlutters;
        SplashFlutter[] _pokerFlutters;
        struct HiveHaloBee
        {
            public float Angle, Speed, RadiusK, BobPhase;
            public Color Tint;
            public float Appear, Expire;
        }
        readonly List<HiveHaloBee> _incomingHalo = new List<HiveHaloBee>();
        static Sprite _splashPointedV;

        public void InviteShareDone(string ok)
        {
            if (ok == "1") Invite.OnShared();
        }

        void Start()
        {
            Application.runInBackground = true;
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Sfx.Warm();
            Ads.Warm();
            Invite.Warm();
            SpriteCatalog.DropPokerArt();
            try { ShowSplash(); }
            catch (System.Exception e)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogException(e);
#endif
            }
#if UNITY_EDITOR
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
#endif
        }

        void Restart()
        {
            if (_splash) return;
            if (_busy && !_frozen) return;
            if (Purse.Streak > 0)
            {
                _keepStreak = true;
                _freezeOffer = false;
                _gift = GiftFace.Card;
                Sfx.Chirp(BirdColor.Gold);
                return;
            }
            StartCoroutine(SnapRound());
        }

        void ShowSplash()
        {
            _splash = true;
            _home = HomeFace.Splash;
            _busy = false;
            _won = false;
            _levelHive = false;
            _gift = GiftFace.None;
            _frozen = false;
            _freezeOffer = false;
            _sel = -1;
            StopPests();
            if (_garden.Root != null) Destroy(_garden.Root.gameObject);
            if (_garden.Cam != null) Destroy(_garden.Cam.gameObject);
            _garden = default;
            _board = null;
            WorldBuilder.MakeCamera(transform);
            if (MixDesk.Live != null) MixDesk.Live.SetSplash(true);
            Purse.Boot();
            Invite.Warm();
            PigPoke.Boot();
            BirdPoker.Boot();
            ArmStreakSlide();
        }

        void Load(int index)
        {
            _busy = false;
            _won = false;
            _sel = -1;
            _pestsArmed = false;
            _combo = 0;
            _comboUntil = -99f;
            _collecting = false;
            _locked.Clear();
            _levelBees.Clear();
            _incomingHalo.Clear();
            _levelHive = false;
            _gift = GiftFace.None;
            _frozen = false;
            _freezeOffer = false;
            _splash = false;
            if (MixDesk.Live != null) MixDesk.Live.SetSplash(false);
            _board = LevelData.Open(index);
            _seed = _board.Clone();
            Purse.BeginStage();
            if (_garden.Root != null) Destroy(_garden.Root.gameObject);
            if (_garden.Cam != null) Destroy(_garden.Cam.gameObject);
            SpriteCatalog.ForgetBirds();
            _garden = WorldBuilder.Build(transform);
            SyncAll();
            StartCoroutine(GardenFit.Tween(_garden, _board, true));
            Sfx.GardenWake();
            StopPests();
            _sparrowRun = StartCoroutine(SparrowView.Patrol(
                CanSparrowVisit, PestsArmed, LevelData.SparrowVisits, _garden.Feeders, _garden.Root));
            _hawkRun = StartCoroutine(HawkView.Patrol(
                CanHawkVisit, PestsArmed, LevelData.HawkVisits, _garden.Feeders, _garden.Root));
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
            if (HawkView.Live != null)
                Destroy(HawkView.Live.gameObject);
        }

        void StopPests()
        {
            StopSparrow();
            StopHawk();
        }

        bool CanSparrowVisit() =>
            !_splash && !_busy && !_won && !_frozen && !_levelHive
            && _gift == GiftFace.None && _board != null && !_board.Won
            && (HawkView.Live == null || !HawkView.Live.IsBlocking);

        bool CanHawkVisit() =>
            !_splash && !_busy && !_won && !_frozen && !_levelHive
            && _gift == GiftFace.None && _board != null && !_board.Won;

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
                for (int n = 0; n < 4; n++) Hive.TakeVisitor();
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
            yield return new WaitForSecondsRealtime(0.95f);
            yield return SnapShot(dir + "/streak-peek.png");
            yield return new WaitForSecondsRealtime(1.35f);
            yield return SnapShot(dir + "/streak-pop.png");
            yield return new WaitForSecondsRealtime(1.85f);
            yield return SnapShot(dir + "/streak-hold.png");
            yield return new WaitForSecondsRealtime(1.85f);
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
            Debug.Log("Flock Five: ShotPokerFaces start " + dir + " pass5-locks-thumb-arm");
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
            br.Birds.Add(new Bird(tip.Color, tip.Sex));
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

        void SyncAll()
        {
            for (int i = 0; i < _garden.Branches.Length; i++)
            {
                if (_locked.Contains(i)) continue;
                if (_board == null || i >= _board.Branches.Count) continue;
                _garden.Branches[i].Sync(_board.Branches[i], _board.IsSleeping(i));
                _garden.Branches[i].SetReady(i == _sel);
            }
            for (int i = 0; i < 2; i++)
                _garden.Feeders[i].Show(_board.Live[i]);
        }

        bool Locked(int i) => _locked.Contains(i);

        void Lock(int i) => _locked.Add(i);

        void Unlock(int i) => _locked.Remove(i);

        int NextCombo()
        {
            if (Time.unscaledTime > _comboUntil) _combo = 0;
            _combo = Mathf.Min(_combo + 1, Palette.ComboMax);
            _comboUntil = Time.unscaledTime + 4.5f;
            return _combo;
        }

        void Update()
        {
            if (_splash) return;
            if (_gift != GiftFace.None || _frozen)
            {
                if (Pressed(out var tap))
                {
                    if (HitHud(tap)) return;
                    if (_frozen && _gift == GiftFace.None) OpenGift();
                }
                return;
            }
            if (!_busy && !_won && SkyCycle.Courtesy != null)
                SkyCycle.Courtesy = null;
#if UNITY_EDITOR
            if (WantFinalePreview() && !_busy)
            {
                StartCoroutine(PreviewFinale());
                return;
            }
#endif
            if (!Pressed(out var screen)) return;
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
            if (_busy || _won || _board == null) return;
            if (Time.unscaledTime < _nextTap) return;
            _nextTap = Time.unscaledTime + 0.10f;

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
            if (hit >= 0 && GiftLocked(hit))
            {
                OpenGift();
                return;
            }
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
            Unlock(from);
            Unlock(to);
            SyncAll();

            int kicked = KickCollects();
            SyncAll();
            if (_locked.Count == 0)
                yield return GardenFit.Tween(_garden, _board, false);
            if (_board.JustUnveiled)
            {
                var visit = Hive.TakeVisitor();
                _levelBees.Add(visit);
                Sfx.BeeFound();
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
                movers[i].sortingOrder = 40;
            }
            float t = 0f;
            const float dur = 0.48f;
            while (t < dur)
            {
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
            _collecting = true;
            Lock(branch);
            if (combo >= 2)
            {
                Sfx.Combo(combo);
                CamShake.Combo(combo);
                var root = _garden.Root;
                StartCoroutine(Wow.SlamCombo(root, combo));
                StartCoroutine(Wow.FlockOver(root, combo));
                int bursts = Mathf.Clamp(combo, 2, Palette.ComboMax);
                for (int i = 0; i < bursts; i++)
                {
                    var p = new Vector3(Random.Range(-4.4f, 4.4f), Random.Range(2.5f, 6.8f), 0f);
                    StartCoroutine(Wow.SkyBurst(root, p, (BirdColor)(i % Palette.Max), i < 3, i * 0.08f));
                }
            }

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
                bird.sortingOrder = 42;
                birds[n++] = bird;
            }
            var flock = new Bird[n];
            for (int i = 0; i < n; i++)
            {
                var idle = birds[i] != null ? birds[i].GetComponent<BirdIdle>() : null;
                flock[i] = idle != null ? new Bird(idle.Color, idle.Sex) : new Bird(col, BirdSex.Neutral);
            }
            bool vsHawk = HawkView.Live != null && HawkView.Live.BlockingSlot == slot;
            bool vsSparrow = !vsHawk && SparrowView.Live != null && SparrowView.Live.BlockingSlot == slot;
            bool pest = vsHawk || vsSparrow;
            _board.ApplyCollect(branch, scoreFeeder: !pest);

            float haste = Mathf.Lerp(1f, 0.52f, Mathf.Clamp01((combo - 1) / 7f));
            float step = 0.192f * haste;
            float fly = 0.432f * haste;
            // Pest scraps stay at full tempo — combo haste made the fight unreadable.
            float fightHaste = (vsHawk || vsSparrow) ? 1f : haste;
            float fightFly = (vsHawk || vsSparrow) ? 0.58f : fly;
            if (vsHawk)
                yield return CollectVsHawk(birds, n, feeder, mouth, view, col, fightHaste, step, fightFly, combo, flock, branch);
            else if (vsSparrow)
                yield return CollectVsSparrow(birds, n, feeder, mouth, view, col, fightHaste, step, fightFly, combo, flock, branch);
            else
            {
                if (feeder != null) feeder.Hold();
                for (int i = 0; i < n; i++)
                {
                    float u = n <= 1 ? 0f : i / (float)(n - 1) - 0.5f;
                    var land = mouth + new Vector3(u * 0.95f, 0.06f * Mathf.Sin((i + 1) * 1.2f), 0f);
                    StartCoroutine(FlyHit(birds[i].transform, land, step * i, fly, i, feeder));
                }
                if (n > 0)
                {
                    yield return new WaitForSeconds(fly * 0.92f);
                    StartCoroutine(Wow.Burst(mouth + Vector3.up * 0.2f, col, _garden.Root, combo));
                }
                yield return new WaitForSeconds((n > 0 ? (n - 1) * step : 0f) + 0.216f * haste);

                if (feeder != null) yield return feeder.PullAway();
                yield return view.BreakAway();
            }
            Unlock(branch);
            _collecting = false;
            int more = KickCollects();
            SyncAll();
            if (_locked.Count == 0)
                yield return GardenFit.Tween(_garden, _board, false);
            if (more != 0) yield break;
            yield return SettleIfIdle();
            if (vsHawk || vsSparrow)
                yield return new WaitForSeconds(1.05f);
            CheckOver();
        }

        // Collect into a sparrow-blocked feeder: dive arcs, five dive-strikes, zigzag flee, scatter.
        IEnumerator CollectVsSparrow(
            SpriteRenderer[] birds, int n, FeederView feeder, Vector3 mouth,
            BranchView view, BirdColor col, float haste, float step, float fly, int combo,
            Bird[] flock, int broken)
        {
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
            if (n > 0)
                StartCoroutine(Wow.Burst(Body() + Vector3.up * 0.15f, col, _garden.Root, combo));

            // Five dive-strikes; irregular gaps between contacts; strikers loop back for more.
            for (int h = 0; h < hits; h++)
            {
                if (h > 0)
                    yield return new WaitForSeconds(Random.Range(0.12f, 0.22f) * haste);
                int striker = h % Mathf.Max(1, n);
                if (birds[striker] == null) continue;
                yield return DiveStrike(birds[striker].transform, Body, h);
            }

            if (SparrowView.Live != null)
                yield return SparrowView.Live.PanicFlee();

            yield return ScatterUp(birds, n);

            if (feeder != null) feeder.SnapHome();
            yield return view.BreakAway();
            yield return PerchOnRemain(birds, n, flock, broken);
        }

        // Collect into a hawk-blocked feeder. Same dive scrap as sparrow, but two
        // full collects to clear: first wounds (hawk stays perched), second flees.
        IEnumerator CollectVsHawk(
            SpriteRenderer[] birds, int n, FeederView feeder, Vector3 mouth,
            BranchView view, BirdColor col, float haste, float step, float fly, int combo,
            Bird[] flock, int broken)
        {
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
            if (n > 0)
                StartCoroutine(Wow.Burst(Body() + Vector3.up * 0.15f, col, _garden.Root, combo));

            for (int h = 0; h < hits; h++)
            {
                if (h > 0)
                    yield return new WaitForSeconds(Random.Range(0.12f, 0.22f) * haste);
                int striker = h % Mathf.Max(1, n);
                if (birds[striker] == null) continue;
                yield return DiveStrike(birds[striker].transform, Body, h);
            }

            bool cleared = hawk != null && hawk.AbsorbCollect();
            if (cleared && HawkView.Live != null)
                yield return HawkView.Live.PanicFlee();
            else if (hawk != null)
                hawk.EndScrap();

            yield return ScatterUp(birds, n);

            if (feeder != null) feeder.SnapHome();
            yield return view.BreakAway();
            yield return PerchOnRemain(birds, n, flock, broken);
        }

        IEnumerator DiveApproach(Transform tr, Vector3 hold, float delay, float dur, int pop)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (tr == null) yield break;
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
            float t = 0f;
            while (t < dur)
            {
                if (tr == null) yield break;
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
                var idle = tr.GetComponent<BirdIdle>();
                if (idle != null) idle.Flapping = true;
                yield return null;
            }
            // Ease back from overshoot to hold.
            if (tr == null) yield break;
            var from = tr.position;
            float back = 0f;
            const float backDur = 0.10f;
            while (back < backDur)
            {
                if (tr == null) yield break;
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
            while (t < dur)
            {
                if (tr == null) yield break;
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
                var idle = tr.GetComponent<BirdIdle>();
                if (idle != null) idle.Flapping = true;
                yield return null;
            }

            // Glance-off tight loop, then hover nearby for a possible return pass.
            if (tr == null) yield break;
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
                if (tr == null) yield break;
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
            if (n <= 0) yield break;
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
            yield return new WaitForSeconds(0.92f + n * 0.05f);
        }

        static IEnumerator ScatterHold(Transform tr, Vector3 dest, float delay)
        {
            if (tr == null) yield break;
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (tr == null) yield break;
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
                if (tr == null) yield break;
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, t / dur);
                var p = Vector3.Lerp(start, dest, u);
                p.y += Mathf.Sin(u * Mathf.PI) * lift;
                p.x += Mathf.Sin(u * Mathf.PI * 2.1f) * side * 0.22f * (1f - u);
                tr.position = p;
                tr.localScale = scale;
                yield return null;
            }
            if (tr != null) tr.position = dest;
        }

        // After a pest scrap the flock hops onto remaining limbs. Redistribute
        // must never land an auto-clear: skip any perch that would fill a
        // same-color Cap match (KickCollects would fire). No safe seat → scatter.
        IEnumerator PerchOnRemain(SpriteRenderer[] birds, int n, Bird[] flock, int broken)
        {
            if (n <= 0) yield break;
            var dests = new Vector3[n];
            var parked = new bool[n];
            for (int i = 0; i < n; i++)
            {
                var bird = i < flock.Length ? flock[i] : new Bird(BirdColor.Gold, BirdSex.Neutral);
                int home = FindParkBranch(broken, bird);
                if (home < 0 || _garden.Branches == null || home >= _garden.Branches.Length)
                    continue;
                var st = _board.Branches[home];
                int seat = st.Count;
                st.Birds.Add(bird);
                st.AlignShroud();
                var br = _garden.Branches[home];
                dests[i] = br.SeatWorld(seat) + br.transform.TransformVector(new Vector3(0f, BranchView.RestLift, 0f));
                parked[i] = true;
            }

            Sfx.FlockFlutter(Mathf.Max(1, n));
            float wait = 1.15f;
            for (int i = 0; i < n; i++)
            {
                if (birds[i] == null) continue;
                float delay = i * 0.08f;
                wait = Mathf.Max(wait, delay + 0.88f);
                if (parked[i])
                    StartCoroutine(PerchOne(birds[i].transform, dests[i], delay));
                else
                {
                    var off = ScatterDests(1);
                    StartCoroutine(ScatterOne(birds[i].transform, off[0], delay));
                }
            }
            yield return new WaitForSeconds(wait);
            yield return new WaitForSeconds(0.55f);
            // Fight sprites were parented to Root for the scrap; SyncAll owns
            // parked seat visuals, so drop the temps. Scattered birds already
            // fade/hide via ScatterOne.
            for (int i = 0; i < n; i++)
            {
                if (!parked[i] || birds[i] == null) continue;
                Destroy(birds[i].gameObject);
                birds[i] = null;
            }
        }

        int FindParkBranch(int broken, Bird bird)
        {
            if (_board == null) return -1;
            int best = -1;
            int bestFree = -1;
            for (int b = 0; b < _board.Branches.Count; b++)
            {
                if (b == broken) continue;
                var st = _board.Branches[b];
                if (st.Broken || st.AdLocked || st.Free <= 0) continue;
                if (_board.IsSleeping(b)) continue;
                if (_garden.Branches == null || b >= _garden.Branches.Length) continue;
                var view = _garden.Branches[b];
                if (view == null) continue;
                if (WouldFullMatchAfterAdd(st, bird)) continue;
                if (st.Free > bestFree)
                {
                    bestFree = st.Free;
                    best = b;
                }
            }
            return best;
        }

        // Same rules as BranchState.IsFullMatch, hypothetically after one add.
        static bool WouldFullMatchAfterAdd(BranchState st, Bird bird)
        {
            if (st == null || st.Broken) return false;
            if (st.Count + 1 != BranchState.Cap) return false;
            for (int i = 0; i < st.Count; i++)
            {
                if (st.IsShrouded(i)) return false;
                if (st.Birds[i].Color != bird.Color) return false;
            }
            return true;
        }

        static IEnumerator PerchOne(Transform tr, Vector3 dest, float delay)
        {
            if (tr == null) yield break;
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (tr == null) yield break;
            var start = tr.position;
            var scale = BranchView.BirdScale;
            float dur = 0.78f;
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
                sr.enabled = true;
                sr.flipX = dest.x < start.x;
                var c = sr.color;
                c.a = 1f;
                sr.color = c;
            }
            tr.gameObject.SetActive(true);
            float t = 0f;
            while (t < dur)
            {
                if (tr == null) yield break;
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, t / dur);
                var p = Vector3.Lerp(start, dest, u);
                p.y += Mathf.Sin(u * Mathf.PI) * 2.45f;
                tr.position = p;
                tr.localScale = scale;
                yield return null;
            }
            if (tr == null) yield break;
            tr.position = dest;
            tr.localScale = scale;
            if (idle != null)
            {
                idle.Frozen = false;
                idle.Flapping = false;
            }
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
            if (tr == null) yield break;
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (tr == null) yield break;
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
                if (tr == null) yield break;
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, t / dur);
                var p = Vector3.Lerp(start, dest, u);
                p.y += Mathf.Sin(u * Mathf.PI) * lift;
                p.x += Mathf.Sin(u * Mathf.PI * wobble) * side * sway * (1f - u * 0.65f);
                tr.position = p;
                tr.localScale = scale * Mathf.Lerp(1f, 0.68f, u);
                var sr = tr.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    var c = sr.color;
                    c.a = 1f - u * 0.88f;
                    sr.color = c;
                }
                yield return null;
            }
            if (tr != null)
            {
                tr.gameObject.SetActive(false);
                var sr = tr.GetComponent<SpriteRenderer>();
                if (sr != null) sr.enabled = false;
            }
        }

        IEnumerator SettleIfIdle()
        {
            if (_locked.Count > 0 || _won || _board == null || !_board.Won) yield break;
            _won = true;
            _busy = true;
            _levelHive = false;
            yield return FinaleShow.Play(_garden, this);
            yield return new WaitForSeconds(0.45f);
            yield return Ads.Interstitial();
            LevelData.RememberClear();
            Purse.AwardClear();
            ShowSplash();
            ArmCoinFly();
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

        IEnumerator FlyHit(Transform tr, Vector3 dest, float delay, float dur, int pop, FeederView feeder)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (tr == null) yield break;
            Sfx.FlockFlutter(1);
            var start = tr.position;
            var scale = tr.localScale;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, t / dur);
                var p = Vector3.Lerp(start, dest, u);
                p.y += Mathf.Sin(u * Mathf.PI) * 1.85f;
                tr.position = p;
                tr.localScale = scale;
                yield return null;
            }
            tr.position = dest;
            Sfx.ScorePop(pop);
            if (feeder != null) feeder.Pulse();
            yield return PopOff(tr);
        }

        static IEnumerator PopOff(Transform tr)
        {
            if (tr == null) yield break;
            var from = tr.localScale;
            float t = 0f;
            const float dur = 0.168f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float pop = u < 0.28f
                    ? Mathf.Lerp(1f, 1.28f, u / 0.28f)
                    : Mathf.Lerp(1.28f, 0f, (u - 0.28f) / 0.72f);
                tr.localScale = from * pop;
                yield return null;
            }
            tr.gameObject.SetActive(false);
            var sr = tr.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;
        }

        void CheckOver()
        {
            if (_busy || _collecting || _locked.Count > 0 || _frozen) return;
            if (_won || _board == null || _board.Won) return;
            if (_gift != GiftFace.None) return;
            if (GardenSolve.Look(_board) != GardenSolve.Outlook.Tangled) return;
            _frozen = true;
            StartCoroutine(FreezeOver());
        }

        IEnumerator FreezeOver()
        {
            if (_frozen || _won) yield break;
            _frozen = true;
            _busy = true;
            _freezeOffer = true;
            if (_sel >= 0)
            {
                _garden.Branches[_sel].SetReady(false);
                _sel = -1;
            }
            StillBirds(true);
            if (_garden.Ice != null) yield return _garden.Ice.Coat();
            yield return new WaitForSeconds(0.45f);
            _gift = GiftFace.Card;
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

        IEnumerator SnapRound()
        {
            _busy = true;
            _gift = GiftFace.None;
            _keepStreak = false;
            _frozen = false;
            _freezeOffer = false;
            _sel = -1;
            _combo = 0;
            _collecting = false;
            _locked.Clear();
            if (_garden.Ice != null)
                yield return _garden.Ice.Shatter(_garden.Root);
            StillBirds(false);
            if (_seed != null) _board = _seed.Clone();
            if (_garden.Branches != null && _board != null)
            {
                for (int i = 0; i < _garden.Branches.Length; i++)
                {
                    var v = _garden.Branches[i];
                    if (v == null) continue;
                    if (i >= _board.Branches.Count)
                    {
                        v.gameObject.SetActive(false);
                        continue;
                    }
                    v.Revive();
                    var want = v.GetComponent<GiftWant>();
                    if (want != null)
                    {
                        bool locked = _board.Branches[i].AdLocked;
                        want.On = locked;
                        want.enabled = locked;
                        if (v.Sign != null) v.Sign.gameObject.SetActive(locked);
                    }
                }
            }
            if (_garden.Feeders != null)
            {
                for (int i = 0; i < _garden.Feeders.Length; i++)
                    if (_garden.Feeders[i] != null) _garden.Feeders[i].SnapHome();
            }
            SyncAll();
            yield return GardenFit.Tween(_garden, _board, true);
            yield return SnapBirdsHome();
            _busy = false;
        }

        IEnumerator SnapBirdsHome()
        {
            if (_garden.Branches == null) yield break;
            var birds = new System.Collections.Generic.List<Transform>();
            var dests = new System.Collections.Generic.List<Vector3>();
            var starts = new System.Collections.Generic.List<Vector3>();
            for (int i = 0; i < _garden.Branches.Length; i++)
            {
                var v = _garden.Branches[i];
                if (v == null || !v.gameObject.activeInHierarchy) continue;
                for (int s = 0; s < v.Birds.Length; s++)
                {
                    if (v.Birds[s] == null || !v.Birds[s].enabled) continue;
                    var tr = v.Birds[s].transform;
                    birds.Add(tr);
                    dests.Add(tr.position);
                    var idle = v.Birds[s].GetComponent<BirdIdle>();
                    if (idle != null) idle.Frozen = true;
                    starts.Add(tr.position + new Vector3(Random.Range(-1.6f, 1.6f), Random.Range(1.6f, 3.8f), 0f));
                    tr.position = starts[starts.Count - 1];
                }
            }
            var fFrom = new Vector3[2];
            var fTo = new Vector3[2];
            if (_garden.Feeders != null)
            {
                for (int i = 0; i < 2; i++)
                {
                    if (_garden.Feeders[i] == null) continue;
                    fTo[i] = _garden.Feeders[i].transform.position;
                    fFrom[i] = fTo[i] + new Vector3(0f, 2.4f, 0f);
                    _garden.Feeders[i].transform.position = fFrom[i];
                }
            }
            Sfx.FlockFlutter(Mathf.Max(1, birds.Count / 3));
            float t = 0f;
            const float dur = 0.28f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float k = 1f - (1f - u) * (1f - u);
                for (int i = 0; i < birds.Count; i++)
                {
                    if (birds[i] == null) continue;
                    var p = Vector3.Lerp(starts[i], dests[i], k);
                    p.y += Mathf.Sin(u * Mathf.PI) * 0.7f;
                    birds[i].position = p;
                }
                if (_garden.Feeders != null)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        if (_garden.Feeders[i] == null) continue;
                        _garden.Feeders[i].transform.position = Vector3.Lerp(fFrom[i], fTo[i], k);
                    }
                }
                yield return null;
            }
            SyncAll();
            StillBirds(false);
        }

        void RestorePerch()
        {
            _sel = -1;
            _won = _board != null && _board.Won;
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
                    if (_board.Branches[v.Index].Broken) continue;
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
                if (st.Broken || st.IsFullMatch(out _)) continue;
                float reach = st.Empty && sending ? 3.45f : pad;
                float d = v.NearestPadSqr(world);
                if (d < reach * reach && d < best) { best = d; idx = v.Index; }
            }
            var overlap = Physics2D.OverlapCircleAll(world, sending ? 2.6f : 1.85f);
            for (int i = 0; i < overlap.Length; i++)
            {
                var v = overlap[i].GetComponentInParent<BranchView>();
                if (v == null || _board.Branches[v.Index].Broken) continue;
                if (_board.Branches[v.Index].IsFullMatch(out _)) continue;
                float d = v.NearestPadSqr(world);
                if (d < best) { best = d; idx = v.Index; }
            }
            return idx;
        }

        static bool Pressed(out Vector2 screen)
        {
            screen = default;
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

        void SnapHiveToHud()
        {
            if (_splash || _garden.Hive == null) return;
            var cam = _garden.Cam != null ? _garden.Cam : Camera.main;
            if (cam == null) return;
            HudLayout(out _, out _, out _, out _, out var hive);
            _garden.Hive.TrackHud(cam, hive);
        }

        void LateUpdate()
        {
#if UNITY_EDITOR
            if (!_pokerPlayrun && System.IO.File.Exists("/tmp/flock-five-poker-run"))
            {
                try { System.IO.File.Delete("/tmp/flock-five-poker-run"); } catch { }
                _pokerPlayrun = true;
                StartCoroutine(PokerPlayrun());
            }
            if (System.IO.File.Exists("/tmp/flock-five-poker-faces"))
            {
                try { System.IO.File.Delete("/tmp/flock-five-poker-faces"); } catch { }
                StartCoroutine(ShotPokerFaces());
            }
            if (System.IO.File.Exists("/tmp/flock-five-consumer-tour"))
            {
                try { System.IO.File.Delete("/tmp/flock-five-consumer-tour"); } catch { }
                StartCoroutine(ShotConsumerTour());
            }
            if (System.IO.File.Exists("/tmp/flock-five-shot"))
            {
                try { System.IO.File.Delete("/tmp/flock-five-shot"); } catch { }
                StartCoroutine(ShotHome());
            }
            if (System.IO.File.Exists("/tmp/flock-five-poker-stamp"))
            {
                try { System.IO.File.Delete("/tmp/flock-five-poker-stamp"); } catch { }
                StartCoroutine(ShotPokerStamp());
            }
            if (System.IO.File.Exists("/tmp/flock-five-hand-qa"))
            {
                try { System.IO.File.Delete("/tmp/flock-five-hand-qa"); } catch { }
                StartCoroutine(ShotHandQa());
            }
#endif
            SnapHiveToHud();
        }

        bool HitHud(Vector2 screen)
        {
            HudLayout(out _, out float top, out _, out var restart, out var hive);
            float gy = Screen.height - screen.y;
            var gui = new Vector2(screen.x, gy);
            if (_levelHive)
            {
                _levelHive = false;
                return true;
            }
            if (gui.y < top) return true;
            if (restart.Contains(gui))
            {
                Restart();
                return true;
            }
            if (hive.Contains(gui))
            {
                _levelHive = !_levelHive;
                return true;
            }
            HudLayout(out _, out _, out float bot, out _, out _);
            return gui.y > Screen.height - bot - 8f;
        }

        void OnGUI()
        {
            if (_splash)
            {
                if (_home == HomeFace.Hive) DrawHivePage();
                else if (_home == HomeFace.Poker) DrawPokerPage();
                else DrawSplash();
                return;
            }
            if (_board == null) return;
            HudLayout(out float s, out float top, out _, out var restart, out var hive);
            DrawRemainingBirds(s);
            var arrow = SpriteCatalog.Restart;
            if (arrow != null && arrow.texture != null)
                GUI.DrawTexture(restart, arrow.texture, ScaleMode.ScaleToFit, true);
            else
                GUI.Box(restart, "↩");
            DrawHiveButton(hive, s, orbit: true);
            if (_levelHive) DrawLevelHive(s);
            DrawGiftSign(s);
            if (_gift != GiftFace.None) DrawGiftOffer(s);
        }

        // Original splash hive size — pig matches this, then both bump together.
        static float SplashRailAnchor() =>
            Mathf.Clamp(Screen.width * 0.11f, 52f, 108f);

        static float SplashRailSize() => SplashRailAnchor() * 1.22f;

        static float SplashRailGap() => Mathf.Max(24f, SplashRailSize() * 0.30f);

        static Rect SplashHiveRect() => HomeRailRect(true, SplashRailSize());

        static Rect SplashShareRect()
        {
            float size = SplashRailSize() * 0.70f;
            var hive = SplashHiveRect();
            var left = HomeRailRect(false, size);
            return new Rect(left.x, hive.y + (hive.height - size) * 0.5f, size, size);
        }

        static Rect SplashPokerRect()
        {
            // A touch larger than hive/piggy so the chip reads as the poker button,
            // still a sibling on the rail — not a second play flower.
            float size = SplashRailSize() * 1.22f;
            var hive = SplashHiveRect();
            float x = hive.x + hive.width * 0.5f - size * 0.5f;
            return new Rect(x, hive.yMax + SplashRailGap(), size, size);
        }

        static Rect PiggyRect(float s)
        {
            float size = SplashRailSize();
            var hive = SplashHiveRect();
            return new Rect(hive.x, hive.y - size - SplashRailGap(), size, size);
        }

        static void DrawRailIcon(Rect r, Sprite spr)
        {
            if (spr == null || spr.texture == null) return;
            GUI.color = new Color(0.08f, 0.05f, 0.02f, 0.32f);
            GUI.DrawTexture(new Rect(r.x + 3f, r.y + 6f, r.width, r.height), spr.texture, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
            GUI.DrawTexture(r, spr.texture, ScaleMode.ScaleToFit, true);
        }

        void DrawHiveButton(Rect hive, float s, bool orbit = false)
        {
            float pulse = HiveView.GuiPulse;
            Rect draw = hive;
            if (pulse > 0.01f)
            {
                float k = 1f + 0.10f * pulse;
                float cx = hive.center.x, cy = hive.center.y;
                draw = new Rect(cx - hive.width * 0.5f * k, cy - hive.height * 0.5f * k,
                    hive.width * k, hive.height * k);
            }
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
            if (orbit) DrawHiveHalo(draw, s, false);
        }

        float DrawHiveTally(Rect hive, float s)
        {
            int have = Hive.Found;
            int cap = Mathf.Max(1, Hive.AlbumSlots);
            float u = Mathf.Clamp01(have / (float)cap);
            float h = Mathf.Clamp(hive.height * 0.20f, 16f * s, 26f * s);
            float w = hive.width * 0.88f;
            var plate = new Rect(hive.center.x - w * 0.5f, hive.yMax + 3f * s, w, h);
            GUI.color = new Color(0.08f, 0.05f, 0.02f, 0.84f);
            GUI.DrawTexture(plate, Texture2D.whiteTexture);
            if (u > 0.008f)
            {
                GUI.color = new Color(0.90f, 0.66f, 0.18f, 0.90f);
                GUI.DrawTexture(new Rect(plate.x, plate.y, plate.width * u, plate.height), Texture2D.whiteTexture);
            }
            GUI.color = new Color(1f, 0.82f, 0.28f, 0.62f);
            float t = Mathf.Max(1.5f, 2f * s);
            GUI.DrawTexture(new Rect(plate.x, plate.y, plate.width, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(plate.x, plate.yMax - t, plate.width, t), Texture2D.whiteTexture);
            GUI.color = Color.white;
            string n = have.ToString();
            var st = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false
            };
            st.fontSize = FitFont(st, n, plate.width * 0.82f, plate.height * 0.80f, 11, 22);
            StampOutlined(plate, n, st, new Color(1f, 0.95f, 0.78f), 1, 1);
            return plate.yMax;
        }

        void ArmIncomingHalo(BeeVisit visit)
        {
            var tint = visit.Kind.Tint;
            if (visit.Finish != BeeFinish.Normal)
                tint = Color.Lerp(tint, Color.white, visit.Finish == BeeFinish.Holo ? 0.18f : 0.28f);
            float now = Time.unscaledTime;
            _incomingHalo.Add(new HiveHaloBee
            {
                Angle = Random.Range(0f, Mathf.PI * 2f),
                Speed = 3.1f,
                RadiusK = 0.86f,
                BobPhase = now * 1.7f,
                Tint = tint,
                Appear = now + 0.32f,
                Expire = now + 0.95f
            });
        }

        void PruneIncomingHalo()
        {
            float now = Time.unscaledTime;
            for (int i = _incomingHalo.Count - 1; i >= 0; i--)
                if (now >= _incomingHalo[i].Expire) _incomingHalo.RemoveAt(i);
        }

        void DrawHiveHalo(Rect hive, float s, bool behind)
        {
            if (behind) PruneIncomingHalo();
            if (_incomingHalo.Count == 0) return;
            Vector2 c = hive.center;
            c.y -= hive.height * 0.08f;
            // Corner hives sit on the right bezel — keep the ring on-screen.
            float padR = Mathf.Max(6f, Screen.width - hive.xMax);
            if (padR < hive.width * 0.45f)
                c.x -= hive.width * 0.10f;
            float rx = hive.width * 0.58f;
            float ry = hive.height * 0.50f;
            float icon = Mathf.Clamp(hive.width * 0.32f, 16f * s, 36f * s);
            var prev = GUI.color;
            float now = Time.unscaledTime;
            float dt = behind ? Time.unscaledDeltaTime : 0f;
            for (int i = 0; i < _incomingHalo.Count; i++)
            {
                var b = _incomingHalo[i];
                if (now < b.Appear || now >= b.Expire) continue;
                if (behind)
                {
                    b.Angle = Mathf.Repeat(b.Angle + b.Speed * dt, Mathf.PI * 2f);
                    _incomingHalo[i] = b;
                }
                float depth = Mathf.Sin(b.Angle);
                bool isBehind = depth < 0f;
                if (behind != isBehind) continue;
                float x = c.x + Mathf.Cos(b.Angle) * rx * b.RadiusK;
                float y = c.y + Mathf.Sin(b.Angle) * ry * b.RadiusK;
                y += Mathf.Sin(Time.unscaledTime * 2.2f + b.BobPhase) * (2.4f * s);
                x = Mathf.Clamp(x, icon * 0.55f, Screen.width - icon * 0.55f);
                y = Mathf.Clamp(y, icon * 0.55f, Screen.height - icon * 0.45f);
                float life = Mathf.Clamp01((b.Expire - now) / 0.22f);
                float scale = (isBehind ? 0.86f : 1.06f) * Mathf.Lerp(0.55f, 1f, life);
                float iw = icon * scale;
                var spr = SpriteCatalog.BeeFrame(Time.unscaledTime * 14f + i * 2.4f);
                if (spr == null || spr.texture == null) continue;
                var r = new Rect(x - iw * 0.5f, y - iw * 0.5f, iw, iw);
                var tint = b.Tint;
                float dim = isBehind ? 0.78f : 1f;
                GUI.color = new Color(tint.r * dim, tint.g * dim, tint.b * dim,
                    (isBehind ? 0.82f : 0.96f) * life);
                bool faceLeft = Mathf.Cos(b.Angle) < 0f;
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
            var st = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            string lab = "Invite";
            st.fontSize = FitFont(st, lab, disc.width * 0.78f, disc.height * 0.55f, 14, 28);
            StampOutlined(disc, lab, st, new Color(1f, 0.92f, 0.62f), 1, Mathf.Max(2, Mathf.RoundToInt(st.fontSize * 0.08f)));
        }

        void ArmStreakSlide()
        {
            if (Purse.Streak <= 0)
            {
                _streakSlide = -1f;
                _streakAnnounced = 0;
                _streakChirped = false;
                return;
            }
            _streakAnnounced = Purse.Streak;
            // Neon tally only after a stage clear (coins incoming).
            if (Purse.Pending <= 0)
            {
                _streakSlide = -1f;
                return;
            }
            _streakSlide = 0f;
            _streakChirped = false;
            _streakWinChimed = false;
            _streakRollShown = 0;
        }

        void DrawStreakRewards(float s)
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

            // Bigger persistent balance: "$12" + coin sprite on the right.
            DrawCoinBalance(s, pig);

            // Streak pops in from the right, holds, then dismisses — frees room for coins.
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

        void DrawCoinBalance(float s, Rect pig)
        {
            var st = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight,
                wordWrap = false
            };
            string coins = Purse.Cash;
            // ~20% smaller coin; $ text FitFont tracks the icon height.
            float icon = Mathf.Clamp(pig.height * 0.48f * 0.80f, 29f * s, 51f * s);
            float gap = 8f * s;
            float labelW = Mathf.Max(120f * s, pig.x - 28f - icon - gap);
            float h = icon;
            var coinR = new Rect(pig.x - 12f - icon - gap - labelW, pig.y + (pig.height - h) * 0.5f, labelW, h);
            int lo = Mathf.Max(18, Mathf.RoundToInt(icon * 0.55f));
            int hi = Mathf.Max(lo + 2, Mathf.RoundToInt(icon * 0.92f));
            st.fontSize = FitFont(st, coins, coinR.width * 0.98f, coinR.height, lo, hi);
            StampOutlined(coinR, coins, st, new Color(0.42f, 0.26f, 0.08f), 2, 1);

            var coinSpr = SpriteCatalog.Coin;
            if (coinSpr != null && coinSpr.texture != null)
            {
                var ir = new Rect(coinR.xMax + gap, coinR.y + (coinR.height - icon) * 0.5f, icon, icon);
                DrawIdleCoin(ir, coinSpr.texture);
            }
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

        void DrawStreakToast(float s, Rect pig)
        {
            if (_streakSlide < 0f) return;
            if (Purse.Streak <= 0)
            {
                _streakSlide = -1f;
                return;
            }

            const float dur = 6.4f;
            if (GuiPaint())
                _streakSlide = Mathf.Min(1f, _streakSlide + Time.unscaledDeltaTime / dur);
            float u = _streakSlide;

            float enter = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u / 0.10f));
            float tuck = u < 0.86f ? 0f : Mathf.SmoothStep(0f, 1f, (u - 0.86f) / 0.14f);
            if (u >= 1f)
            {
                _streakSlide = -1f;
                Purse.Pending = 0;
                return;
            }

            float alpha = enter * (1f - tuck);
            if (alpha < 0.04f) return;

            float scale = Mathf.Lerp(0.82f, 1.06f, enter);
            scale = Mathf.Lerp(scale, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - 0.10f) / 0.08f)));
            if (u > 0.72f && u < 0.86f)
            {
                float breathe = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.6f);
                scale *= 1f + 0.018f * breathe;
            }
            scale = Mathf.Lerp(scale, 0.72f, tuck);

            int streak = Mathf.Max(1, Purse.LastStreak > 0 ? Purse.LastStreak : Purse.Streak);
            int stagePay = Purse.LastStagePay > 0 ? Purse.LastStagePay : Purse.StagePay;
            int login = Purse.LastLogin > 0 ? Purse.LastLogin : Purse.LoginMul;
            int win = Purse.LastWin > 0 ? Purse.LastWin : stagePay * streak * login;
            bool showLogin = login > 1;

            float titleTop = Mathf.Max(12f, Screen.height - Screen.safeArea.yMax + 6f);
            float restW = Mathf.Min(Screen.width * 0.62f, 400f * s);
            float restH = Mathf.Min(Screen.height * 0.20f, 176f * s);
            float restX = (Screen.width - restW) * 0.5f;
            float restY = titleTop + 56f * s * 2f + 4f * s;
            var pigC = pig.center;
            float x = Mathf.Lerp(restX, pigC.x - restW * 0.22f, tuck);
            float y = Mathf.Lerp(restY, pigC.y - restH * 0.35f, tuck);
            var board = new Rect(x, y, restW, restH);
            var c = board.center;
            board.width *= scale;
            board.height *= scale;
            board.x = c.x - board.width * 0.5f;
            board.y = c.y - board.height * 0.5f;

            var glow = GlowTex();
            float glowA = 0.22f + 0.20f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.3f));
            GUI.color = new Color(1f, 0.82f, 0.28f, glowA * alpha);
            float aura = 22f * s;
            GUI.DrawTexture(new Rect(board.x - aura, board.y - aura * 0.6f, board.width + aura * 2f, board.height + aura * 1.2f), glow, ScaleMode.ScaleToFit, true);

            GUI.color = new Color(0.04f, 0.02f, 0.01f, 0.90f * alpha);
            GUI.DrawTexture(board, Texture2D.whiteTexture);
            GUI.color = new Color(1f, 0.78f, 0.22f, (0.40f + 0.28f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.05f))) * alpha);
            float rim = 3f * s;
            GUI.DrawTexture(new Rect(board.x + rim, board.y + rim, board.width - rim * 2f, 3f * s), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(board.x + rim, board.yMax - rim - 3f * s, board.width - rim * 2f, 3f * s), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(board.x + rim, board.y + rim, 3f * s, board.height - rim * 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(board.xMax - rim - 3f * s, board.y + rim, 3f * s, board.height - rim * 2f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            DrawGiftMarquee(board, s, Time.unscaledTime);

            float padX = board.width * 0.12f;
            float innerX = board.x + padX;
            float innerW = board.width - padX * 2f;
            float pipH = board.height * 0.18f;
            var pipRow = new Rect(innerX, board.y + board.height * 0.14f, innerW, pipH);
            DrawStreakPips(pipRow, streak, u, s, alpha);

            var leftSt = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false
            };
            var rightSt = new GUIStyle(leftSt) { alignment = TextAnchor.MiddleRight };
            var winSt = new GUIStyle(leftSt) { alignment = TextAnchor.MiddleCenter };

            int rows = showLogin ? 3 : 2;
            float linesTop = pipRow.yMax + 6f * s;
            float winH = board.height * 0.22f;
            float linesH = board.yMax - 12f * s - winH - linesTop;
            float rowH = linesH / (rows + 0.35f);

            void Line(int index, float at, string left, string right, bool gold)
            {
                float appear = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - at) / 0.07f));
                if (appear < 0.04f) return;
                var row = new Rect(innerX, linesTop + index * rowH, innerW, rowH);
                leftSt.fontSize = FitFont(leftSt, left, row.width * 0.48f, row.height * 0.82f, 16, 28);
                rightSt.fontSize = leftSt.fontSize;
                var fill = gold
                    ? new Color(1f, 0.90f, 0.28f, appear * alpha)
                    : new Color(1f, 0.94f, 0.78f, appear * alpha);
                StampOutlined(new Rect(row.x, row.y, row.width * 0.52f, row.height), left, leftSt, fill, 1, 2);
                StampOutlined(new Rect(row.x + row.width * 0.48f, row.y, row.width * 0.52f, row.height), right, rightSt, fill, 1, 2);
            }

            Line(0, 0.20f, "STAGE", Purse.Dollars(stagePay), false);
            Line(1, 0.30f, "STREAK", "×" + streak, false);
            if (showLogin) Line(2, 0.40f, "LOGIN", "×" + login, false);

            float rollAt = showLogin ? 0.50f : 0.42f;
            float roll = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - rollAt) / 0.20f));
            if (roll > 0.02f)
            {
                int shown = Mathf.RoundToInt(win * roll);
                if (shown < 0) shown = 0;
                if (shown > _streakRollShown) _streakRollShown = shown;
                if (!_streakChirped && roll > 0.12f)
                {
                    _streakChirped = true;
                    Sfx.Clink();
                }
                if (!_streakWinChimed && roll >= 0.98f)
                {
                    _streakWinChimed = true;
                    Sfx.Clink();
                }
                var winR = new Rect(innerX, board.yMax - 10f * s - winH, innerW, winH);
                GUI.color = new Color(1f, 0.78f, 0.18f, (0.28f + 0.22f * roll) * alpha);
                GUI.DrawTexture(new Rect(winR.x - 8f * s, winR.y - 4f * s, winR.width + 16f * s, winR.height + 8f * s), glow, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
                string winTx = "WIN  " + Purse.Dollars(_streakRollShown);
                winSt.fontSize = FitFont(winSt, winTx, winR.width * 0.96f, winR.height * 0.90f, 28, 56);
                float punch = 1f + 0.10f * Mathf.Sin(Mathf.Clamp01((roll - 0.85f) / 0.15f) * Mathf.PI);
                var prevM = GUI.matrix;
                GUIUtility.ScaleAroundPivot(new Vector2(punch, punch), winR.center);
                StampOutlined(winR, winTx, winSt, new Color(1f, 0.90f, 0.28f, alpha), 1, Mathf.Max(3, Mathf.RoundToInt(winSt.fontSize * 0.12f)));
                GUI.matrix = prevM;
            }
            GUI.color = Color.white;
        }

        static void DrawStreakPips(Rect row, int streak, float u, float s, float alpha)
        {
            const int slots = 7;
            int lit = Mathf.Clamp(streak, 0, slots);
            float gap = 10f * s;
            float d = Mathf.Min((row.width - gap * (slots - 1)) / slots, row.height * 0.70f);
            float total = slots * d + (slots - 1) * gap;
            float x0 = row.center.x - total * 0.5f;
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
                    GUI.DrawTexture(new Rect(r.x - d * 0.28f, r.y - d * 0.28f, d * 1.56f, d * 1.56f), glow, ScaleMode.ScaleToFit, true);
                }
            }
            GUI.color = Color.white;
            if (streak > slots)
            {
                var extra = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = false
                };
                string more = "+" + (streak - slots);
                extra.fontSize = FitFont(extra, more, d * 1.6f, d, 12, 22);
                StampOutlined(new Rect(x0 + total + 6f * s, row.y, d * 1.8f, row.height), more, extra, new Color(1f, 0.90f, 0.28f, alpha), 1, 1);
            }
        }

        void DrawRemainingBirds(float s)
        {
            if (_board == null) return;
            float safeTop = Mathf.Max(8f, Screen.height - Screen.safeArea.yMax);
            float icon = 36f * s;
            float pad = 10f * s;
            var st = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false
            };
            string tx = "×" + _board.RemainingBirds;
            st.fontSize = Mathf.RoundToInt(28 * s);
            float tw = st.CalcSize(new GUIContent(tx)).x;
            float x = Mathf.Max(16f * s, Screen.safeArea.xMin + 12f * s);
            float y = safeTop + 4f * s;
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

        void ArmCoinFly()
        {
            _flies.Clear();
            int n = Mathf.Clamp(Purse.Pending, 0, 12);
            if (n <= 0) return;
            var pig = PiggyRect(Mathf.Max(Screen.height / 720f, 1f));
            var dest = new Vector2(pig.x + pig.width * 0.5f, pig.y + pig.height * 0.42f);
            for (int i = 0; i < n; i++)
            {
                _flies.Add(new CoinFly
                {
                    A = new Vector2(Screen.width * 0.5f + Random.Range(-70f, 70f), Screen.height * 0.30f + Random.Range(-24f, 36f)),
                    B = dest,
                    T = 0f,
                    Delay = 4.55f + i * 0.20f,
                    Spin = Random.Range(300f, 420f) * (Random.value < 0.5f ? -1f : 1f)
                });
            }
        }

        void DrawCoinFly(Rect pig)
        {
            if (_flies.Count == 0) return;
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
                f.T += dt / 1.32f;
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
                    _flies.RemoveAt(i);
                    continue;
                }
                _flies[i] = f;
            }
        }

        void DrawHomeWash(float a)
        {
            var bg = SpriteCatalog.GardenBg;
            if (bg != null && bg.texture != null)
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), bg.texture, ScaleMode.ScaleAndCrop);
            GUI.color = new Color(0.10f, 0.09f, 0.08f, a);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        static Rect HomeRailRect(bool right, float size)
        {
            var safe = Screen.safeArea;
            float y = Screen.height * 0.30f;
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
            }
        }

        static bool GuiPaint()
        {
            var e = Event.current;
            return e == null || e.type == EventType.Repaint;
        }

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

        void DrawSplash()
        {
            float s = Mathf.Max(Screen.height / 720f, 1f);
            DrawHomeWash(0.18f);

            // Home title: stacked FLOCK / FIVE via letter sprites + navy block extrude
            // (same mark family as FinaleShow smash hold — correlate, not shatter).
            DrawAmbientSplashBirds(s, behind: true);
            DrawSplashTitleMark(s);
            DrawAmbientSplashBirds(s, behind: false);
            DrawStreakRewards(s);

            int next = LevelData.NextPlay;
            var peek = LevelData.Peek(next);

            // Pig above hive: hit-test first so taps don't open the album.
            var pigR = PiggyRect(s);
            if (HitPad(pigR, out _))
                TryPigPoke();

            // Hive stays on the right rail; pig stacks above it (see PiggyRect).
            var hiveR = SplashHiveRect();
            if (HitPad(hiveR, out _))
                _home = HomeFace.Hive;
            DrawHiveButton(hiveR, s);

            var shareR = SplashShareRect();
            if (HitPad(shareR, out bool shareHeld))
            {
                Sfx.Chirp(BirdColor.Gold);
                Invite.Share();
            }
            DrawShareButton(shareR, shareHeld, s);

            // Third rail button: bird video poker.
            var pokerR = SplashPokerRect();
            if (HitPad(pokerR, out _))
            {
                BirdPoker.Boot();
                BirdPoker.BeginVisit();
                _pokerKeepHint = true;
                _pokerHover = -1;
                for (int i = 0; i < _pokerHoldSlide.Length; i++) _pokerHoldSlide[i] = 0f;
                _home = HomeFace.Poker;
            }
            DrawSplashPokerButton(pokerR);

#if UNITY_EDITOR
            string ease = _shotEase ?? LevelData.JokeEase(next);
            int number = _shotLevelNumber > 0 ? _shotLevelNumber : (peek != null ? peek.Number : next + 1);
#else
            string ease = LevelData.JokeEase(next);
            int number = peek != null ? peek.Number : next + 1;
#endif
            if (DrawFlowerPlay(s, ease, number))
            {
                Sfx.GateGo();
                Load(next);
            }
        }

        bool DrawFlowerPlay(float s, string ease, int number)
        {
            float botPad = Mathf.Max(14f, Screen.safeArea.yMin + 8f);
            float size = Mathf.Min(Screen.width * 0.94f, Screen.height * 0.50f);
            var rest = new Rect((Screen.width - size) * 0.5f, Screen.height - botPad - size, size, size);
            bool fired = HitPad(rest, out bool held);

            float sink = held ? rest.height * 0.030f : 0f;
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

        static Rect FlowerDisc(Rect rest, float sink)
        {
            return new Rect(
                rest.x + rest.width * 0.205f,
                rest.y + sink + rest.height * 0.10f,
                rest.width * 0.59f,
                rest.height * 0.38f);
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

            var lv = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = lvAlign,
                wordWrap = false
            };
            string level = "LEVEL " + number;
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
            int white = Mathf.Max(2, Mathf.RoundToInt(lv.fontSize * 0.055f));
            StampOutlined(lvR, level, lv, new Color(0.36f, 0.18f, 0.07f), white, 1);

            if (!hasEase) return;

            // Joke keeps full size; width tracks LEVEL+digits (not shrunk by high level counts).
            var joke = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
                wordWrap = false
            };
            float levelW = lv.CalcSize(new GUIContent(level)).x;
            float jokeMaxW = Mathf.Min(jokeR.width * 0.99f, Mathf.Max(levelW, jokeR.width * 0.55f));
            int jHi = ease.Length >= 14 ? 40 : 48;
            joke.fontSize = FitFont(joke, ease, jokeMaxW, jokeR.height * 0.95f, 18, jHi);
            int jWhite = Mathf.Max(2, Mathf.RoundToInt(joke.fontSize * 0.10f));
            StampOutlined(jokeR, ease, joke, new Color(0.30f, 0.15f, 0.06f), jWhite, 1);
        }

        static int FitFont(GUIStyle proto, string text, float maxW, float maxH, int lo, int hi)
        {
            var st = new GUIStyle(proto);
            var content = new GUIContent(text);
            for (int fs = hi; fs >= lo; fs--)
            {
                st.fontSize = fs;
                var sz = st.CalcSize(content);
                if (sz.x <= maxW && sz.y <= maxH) return fs;
            }
            return lo;
        }

        static int FitFontWrapped(GUIStyle proto, string text, float maxW, float maxH, int lo, int hi)
        {
            if (string.IsNullOrEmpty(text) || maxW < 8f || maxH < 8f) return lo;
            var st = new GUIStyle(proto) { wordWrap = true };
            var content = new GUIContent(text);
            for (int fs = hi; fs >= lo; fs--)
            {
                st.fontSize = fs;
                float h = st.CalcHeight(content, maxW);
                if (h <= maxH) return fs;
            }
            return lo;
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

        static void StampOutlined(Rect r, string text, GUIStyle st, Color fill, int whitePx, int blackPx)
        {
            float a = Mathf.Clamp01(fill.a);
            if (a < 0.04f) return;
            Paint(st, new Color(0.02f, 0.02f, 0.02f, a));
            Ring(r, text, st, whitePx + blackPx);
            Paint(st, new Color(1f, 1f, 1f, a));
            Ring(r, text, st, whitePx);
            Paint(st, fill);
            GUI.Label(r, text, st);
        }

        // Finale-family wordmark via letter sprites (yellow faces + navy ExtrudeNear/Far block).
        void DrawSplashTitleMark(float s)
        {
            float top = Mathf.Max(12f, Screen.height - Screen.safeArea.yMax + 6f);
            float maxW = Screen.width * 0.72f; // Brandon: reduce home logo size
            float capH = 56f * s;
            float rowGap = 4f * s;
            float tracking = -0.055f;
            DrawSplashWord("FLOCK", top, maxW, capH, tracking, s);
            DrawSplashWord("FIVE", top + capH + rowGap, maxW, capH, tracking, s);
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
                var st = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.RoundToInt(h * 0.92f),
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                var fill = new Color(1f, 0.92f, 0.42f);
                var stroke = new Color(0.02f, 0.02f, 0.04f, 1f);
                var near = new Color(28f / 255f, 44f / 255f, 102f / 255f, 1f);
                int steps = Mathf.Max(8, Mathf.RoundToInt(10f * s));
                float step = 1.35f * s * scale;
                var r = new Rect(x, y, tw, h);
                Paint(st, near);
                for (int d = steps; d >= 1; d--)
                    GUI.Label(new Rect(r.x + d * step * 0.78f, r.y + d * step, r.width, r.height), word, st);
                Paint(st, stroke);
                Ring(r, word, st, Mathf.Max(2, Mathf.RoundToInt(2.4f * s)));
                Paint(st, fill);
                GUI.Label(r, word, st);
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
            // Ring sits inside the halo so we never need a hard clamp (clamp parked birds on the glyphs).
            float rx = Mathf.Max(10f, h.width * 0.42f);
            float ry = Mathf.Max(10f, h.height * 0.48f);
            if (flutters != null && flutters.Length == n)
            {
                for (int i = 0; i < n; i++)
                {
                    float k = 0.74f + 0.05f * i;
                    flutters[i].RadiusX = rx * k;
                    flutters[i].RadiusY = ry * k;
                    flutters[i].Angle = Mathf.Repeat(flutters[i].Angle, Mathf.PI * 2f);
                }
                return;
            }
            flutters = new SplashFlutter[n];
            var cols = new[] { BirdColor.Ruby, BirdColor.Gold, BirdColor.Teal, BirdColor.Violet, BirdColor.Peach };
            float step = Mathf.PI * 2f / n;
            for (int i = 0; i < n; i++)
            {
                float k = 0.74f + 0.05f * i;
                flutters[i] = new SplashFlutter
                {
                    Angle = i * step,
                    Speed = Random.Range(0.40f, 0.52f),
                    RadiusX = rx * k,
                    RadiusY = ry * k,
                    BobPhase = i * 1.17f,
                    Col = cols[i]
                };
            }
        }

        // Soft bounds around the stacked title mark (logo companions only).
        static Rect SplashTitleHalo()
        {
            float top = Mathf.Max(12f, Screen.height - Screen.safeArea.yMax + 6f);
            float capH = 56f * Mathf.Max(Screen.height / 720f, 1f);
            float rowGap = 4f * Mathf.Max(Screen.height / 720f, 1f);
            float titleH = capH * 2f + rowGap;
            float padX = Screen.width * 0.18f; // tighter logo halo
            float padY = 16f * Mathf.Max(Screen.height / 720f, 1f);
            return new Rect(padX, Mathf.Max(4f, top - padY * 0.35f), Screen.width - padX * 2f, titleH + padY);
        }

        // behind=true: upper half of ellipse draws under letter faces (true z-weave).
        // Radii stay tight + positions clamped to halo — no #89 wander regress.
        void DrawAmbientSplashBirds(float s, bool behind)
        {
            EnsureSplashFlutters();
            DrawHaloBirds(ref _splashFlutters, SplashTitleHalo(), s, behind, HaloBirdIcon(s));
        }

        static float HaloBirdIcon(float s) => 36f * s;

        void DrawHaloBirds(ref SplashFlutter[] flutters, Rect h, float s, bool behind, float icon)
        {
            if (flutters == null || flutters.Length == 0) return;
            Vector2 c = h.center;
            var prev = GUI.color;
            float dt = behind ? Time.unscaledDeltaTime : 0f;
            for (int i = 0; i < flutters.Length; i++)
            {
                var f = flutters[i];
                if (behind)
                {
                    float speed = f.Speed * (0.95f + 0.05f * Mathf.Sin(Time.unscaledTime * 0.5f + f.BobPhase));
                    f.Angle = Mathf.Repeat(f.Angle + speed * dt, Mathf.PI * 2f);
                    flutters[i] = f;
                }
                // y-down: Sin<0 = above title = behind letters; Sin>=0 = below = in front.
                // Split at 0 with no dead band — a ±0.10 gap made birds blink out at the sides.
                float depth = Mathf.Sin(f.Angle);
                bool isBehind = depth < 0f;
                if (behind != isBehind) continue;

                float x = c.x + Mathf.Cos(f.Angle) * f.RadiusX;
                float y = c.y + Mathf.Sin(f.Angle) * f.RadiusY;
                y += Mathf.Sin(Time.unscaledTime * 2.0f + f.BobPhase) * (3.0f * s);

                float scale = behind ? 0.92f : 1.04f;
                float iw = icon * scale;
                float vx = -Mathf.Sin(f.Angle) * f.RadiusX;
                bool faceLeft = vx < 0f;
                var spr = SpriteCatalog.BirdFrame(f.Col, Time.unscaledTime * 11f + i * 2.1f, true);
                if (spr == null || spr.texture == null) continue;
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

        static void Ring(Rect r, string text, GUIStyle st, int px)
        {
            if (px <= 0) return;
            for (int d = 1; d <= px; d++)
            {
                int n = Mathf.Max(8, 8 * d);
                for (int i = 0; i < n; i++)
                {
                    float a = (i / (float)n) * Mathf.PI * 2f;
                    GUI.Label(new Rect(r.x + Mathf.Cos(a) * d, r.y + Mathf.Sin(a) * d, r.width, r.height), text, st);
                }
            }
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

        static void DrawFlowerHalo(Rect rest, float sink)
        {
            float t = Time.unscaledTime;
            float breathe = 0.5f + 0.5f * Mathf.Sin(t * 1.7f);
            float pad = rest.width * (0.03f + 0.035f * breathe);
            var glow = GlowTex();
            GUI.color = new Color(1f, 0.86f, 0.48f, 0.20f + 0.14f * breathe);
            GUI.DrawTexture(new Rect(rest.x - pad, rest.y + sink - pad, rest.width + pad * 2f, rest.height + pad * 2f), glow, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
        }

        static void DrawFlowerShimmer(Rect rest, float sink)
        {
            float t = Time.unscaledTime;
            var glow = GlowTex();
            var face = new Rect(rest.x, rest.y + sink, rest.width, rest.height);

            var disc = FlowerDisc(rest, sink);
            float sheenU = Mathf.Repeat(t * 0.32f, 1.65f);
            if (sheenU < 1f)
            {
                float fade = Mathf.Sin(sheenU * Mathf.PI);
                float x = disc.x + disc.width * (sheenU * 1.15f - 0.18f);
                var band = new Rect(x, disc.y + disc.height * 0.08f, disc.width * 0.28f, disc.height * 0.84f);
                GUI.color = new Color(1f, 0.96f, 0.82f, 0.48f * fade);
                GUI.DrawTexture(band, glow, ScaleMode.ScaleToFit, true);
            }

            GUI.color = new Color(1f, 0.92f, 0.62f, 0.18f + 0.12f * (0.5f + 0.5f * Mathf.Sin(t * 2.05f)));
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
                GUI.color = new Color(1f, 0.95f, 0.72f, 0.28f + 0.62f * tw);
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

        void DrawPokerPage()
        {
            float s = Mathf.Max(Screen.height / 720f, 1f);
            DrawHomeWash(0.28f);
            BirdPoker.Boot();

            var safe = Screen.safeArea;
            float top = Mathf.Max(20f, Screen.height - safe.yMax + 10f);
            var backLab = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(22 * s),
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            backLab.normal.textColor = new Color(1f, 0.94f, 0.72f);
            var back = new Rect(Mathf.Max(16f, safe.xMin + 10f), top, 132f * Mathf.Min(s, 1.6f), 44f * Mathf.Min(s, 1.6f));
            if (HitPad(back, out bool backHeld))
            {
                BirdPoker.ResetRound();
                _pokerMotion = PokerMotion.None;
                _pokerFan = false;
                _pokerChained = false;
                _pokerShowPay = false;
                _pokerDash = 0f;
                _pokerStamp = false;
                _pokerPayOpen = false;
                _pokerPayAnim = 0f;
                _pokerKeepHint = false;
                _pokerHover = -1;
                _home = HomeFace.Splash;
            }
            GUI.color = new Color(0.10f, 0.08f, 0.05f, backHeld ? 0.88f : 0.72f);
            GUI.DrawTexture(back, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(back, "Back", backLab);
            DrawPokerPurse(top, s);

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
            DrawHaloBirds(ref _pokerFlutters, halo, s, true, HaloBirdIcon(s));
            DrawPokerTitleMark(logoTop, s);
            DrawHaloBirds(ref _pokerFlutters, halo, s, false, HaloBirdIcon(s));
            float below = logoTop + logoH;

            // Hit-test pay-table tab / dismiss early so overlay blocks cards & Deal.
            if (!_pokerStamp)
                TickPokerPayTable(below, s);

            float gap = 2f * s;
            float maxRow = Screen.width - 24f * s;
            float cardW = Mathf.Min(Screen.width * 0.205f, 138f * s);
            cardW = Mathf.Min(cardW, (maxRow - (BirdPoker.HandSize - 1) * gap) / BirdPoker.HandSize);
            float cardH = cardW * 1.42f;
            float holdH = 32f * s;
            float rowW = BirdPoker.HandSize * cardW + (BirdPoker.HandSize - 1) * gap;
            float rowX = (Screen.width - rowW) * 0.5f;
            float btnSize = Mathf.Min(Screen.width * 0.46f, 200f * s);
            float botPad = Mathf.Max(12f, safe.yMin + 6f);
            float btnY = Screen.height - botPad - btnSize;
            float meterH = 36f * s;
            float head = below + 8f * s;
            float floor = btnY - holdH - meterH - 8f * s;
            var payTab = PokerPayTabRect(below, s);
            // Below the PAY TABLE chip so a right-seat keep never covers the tab.
            float rowY = payTab.yMax + 12f * s;
            float holdRight = payTab.x - 10f * s;
            if (rowX + rowW > holdRight)
            {
                rowW = Mathf.Max(cardW, holdRight - Mathf.Max(12f, safe.xMin + 8f));
                rowX = Mathf.Max(12f, safe.xMin + 8f);
                if (rowX + rowW > holdRight) rowX = holdRight - rowW;
            }
            if (GuiPaint())
            {
                TickPokerKick();
                TickPokerMotion();
                TickHoldSlide();
            }
            var rowBox = new Rect(rowX, rowY, rowW, cardH);
            bool payBlockedEarly = _pokerPayOpen || _pokerPayAnim > 0.35f;
            bool canHold = !PokerMotionBusy() && !payBlockedEarly && BirdPoker.PhaseNow == BirdPoker.Phase.Dealt;
            bool hideCards = _pokerPayAnim > 0.18f;
            HitPokerCards(canHold, rowBox, rowX, rowY, cardW, cardH, gap);
            if (!hideCards)
            {
                DrawPokerFanHand(rowBox, cardW, cardH, s, false);
                int[] order = PokerDrawOrder();
                // Palm, then every live fan card, then thumb. A later seat must
                // never redraw over the thumb (5-fan neighbor corners).
                for (int o = 0; o < order.Length; o++)
                {
                    int i = order[o];
                    if (PokerCardLifted(i)) continue;
                    var seat = new Rect(rowX + i * (cardW + gap), rowY, cardW, cardH);
                    DrawPokerCard(seat, i, s, holdH, rowBox);
                }
                DrawPokerFanHand(rowBox, cardW, cardH, s, true);
                for (int o = 0; o < order.Length; o++)
                {
                    int i = order[o];
                    if (!PokerCardLifted(i)) continue;
                    var seat = new Rect(rowX + i * (cardW + gap), rowY, cardW, cardH);
                    DrawPokerCard(seat, i, s, holdH, rowBox);
                }
                DrawPokerKeepHint(rowBox, s);
                DrawPokerFlames(rowBox, cardH, s);
                DrawPokerDealHand(rowBox, cardW, cardH, s);
            }

            float actX = Screen.width - Mathf.Max(10f, Screen.width - safe.xMax + 8f) - btnSize;
            var actR = new Rect(actX, btnY, btnSize, btnSize);
            float clusterL = Mathf.Max(12f, safe.xMin + 10f);
            var betR = new Rect(clusterL, btnY, Mathf.Max(80f, actR.x - 10f * s - clusterL), btnSize);

            bool payBlocked = _pokerPayOpen || _pokerPayAnim > 0.35f;
            bool busy = PokerMotionBusy() || _pokerStamp || payBlocked;
            if (GuiPaint()) TickPokerDash(s);
            TickPokerStamp();
            string act = BirdPoker.PhaseNow == BirdPoker.Phase.Dealt ? "DRAW" : "DEAL";
            bool steppers = BirdPoker.PhaseNow == BirdPoker.Phase.Idle;
            if (BirdPoker.PhaseNow == BirdPoker.Phase.Idle) BirdPoker.SyncBet();
            if (DrawPokerDash(betR, actR, s, busy, steppers, act) && !busy)
            {
                if (BirdPoker.PhaseNow == BirdPoker.Phase.Idle)
                    TryPokerDeal();
                else if (BirdPoker.PhaseNow == BirdPoker.Phase.Dealt)
                {
                    for (int i = 0; i < BirdPoker.HandSize; i++)
                    {
                        _pokerRedraw[i] = !BirdPoker.Hold[i];
                        _pokerPrev[i] = BirdPoker.Hand[i];
                    }
                    BirdPoker.Draw();
                    BeginPokerDraw();
                    _pokerPendingStamp = BirdPoker.LastPunchFresh;
                    _pokerResultCue = BirdPoker.LastPunchFresh ? 3 : (BirdPoker.LastWin > 0 ? 2 : 1);
                }
                else
                {
                    BirdPoker.Collect();
                    TryPokerDeal();
                }
            }

            // Stamp ceremony above everything; else pay-table overlay / jewel tab on top.
            if (_pokerStamp) DrawPokerStampCeremony(s);
            else DrawPokerPayTable(below, s);
        }


        Rect PokerPayTabRect(float top, float s)
        {
            float tabW = Mathf.Clamp(176f * s, 148f, 220f);
            float tabH = Mathf.Clamp(52f * s, 46f, 64f);
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
                float panelW = Screen.width * 0.90f;
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

                var head = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                string headTx = "PAY TABLE";
                head.fontSize = FitFont(head, headTx, paper.width * 0.80f, headH * 0.52f, 24, 44);
                StampOutlined(new Rect(paper.x, paper.y + 2f * s, paper.width, headH * 0.58f), headTx, head,
                    new Color(0.28f, 0.14f, 0.06f), 2, 1);
                var sub = new GUIStyle(head) { fontSize = Mathf.Max(16, head.fontSize - 6) };
                StampOutlined(new Rect(paper.x, paper.y + headH * 0.52f, paper.width, headH * 0.40f),
                    "BET " + Purse.Compact(BirdPoker.Bet), sub, new Color(0.62f, 0.28f, 0.08f), 1, 1);
                GUI.color = new Color(0.72f, 0.52f, 0.22f, 0.85f * u);
                float ruleY = paper.y + headH - 2f * s;
                GUI.DrawTexture(new Rect(paper.x + 10f * s, ruleY, paper.width - 20f * s, 2f * s), Texture2D.whiteTexture);
                GUI.color = Color.white;

                var nameSt = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = false
                };
                var numSt = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleRight,
                    wordWrap = false
                };
                int body = Mathf.RoundToInt(Mathf.Clamp(rowH * 0.46f, 22f, 40f));
                int jack = body + 4;
                float yRow = paper.y + headH + 2f * s;
                for (int i = 0; i < rows; i++)
                {
                    var rank = BirdPoker.PayTableRows[i];
                    int mult = BirdPoker.Multiplier(rank);
                    int dollars = mult * BirdPoker.Bet;
                    string name = BirdPoker.RankLabel(rank);
                    var row = new Rect(paper.x + 10f * s, yRow, paper.width - 20f * s, rowH);

                    bool jackpot = rank == BirdPoker.Rank.NaturalFive;
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
                    float nameW = row.width * 0.50f;
                    float multW = row.width * 0.18f;
                    float cashW = row.width * 0.32f;
                    int cap = jackpot ? jack : body;
                    nameSt.fontSize = FitFont(nameSt, name, nameW * 0.98f, row.height * 0.70f, 14, cap);
                    string multTx = mult + "×";
                    string cashTx = Purse.Compact(dollars);
                    numSt.fontSize = FitFont(numSt, cashTx, cashW * 0.98f, row.height * 0.70f, 14, cap);
                    StampOutlined(new Rect(row.x, row.y, nameW, row.height), name, nameSt, fill, 2, 1);
                    StampOutlined(new Rect(row.x + nameW, row.y, multW, row.height), multTx, numSt, fill, 2, 1);
                    StampOutlined(new Rect(row.x + nameW + multW, row.y, cashW, row.height), cashTx, numSt, fill, 2, 1);
                    yRow += rowH;
                }
            }

            DrawPokerPayJewel(tab, s);
        }

        void DrawPokerPayJewel(Rect tab, float s)
        {
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

            string arrow = _pokerPayOpen ? "▼" : "▶";
            string lab = "PAY TABLE " + arrow;
            var labSt = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false
            };
            labSt.fontSize = FitFont(labSt, lab, chip.width * 0.90f, chip.height * 0.72f, 14, 22);
            StampOutlined(chip, lab, labSt, new Color(1f, 0.94f, 0.72f), 1, 1);
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
            // Stamper hits the clipboard — shake the IMGUI overlay and the garden.
            if (_pokerStampT >= 1.05f && _pokerStampT - Time.unscaledDeltaTime < 1.05f)
            {
                PunchPoker(_pokerBingo ? 0.34f : 0.26f, _pokerBingo ? 28f : 20f, _pokerBingo ? 12f : 8f);
                Sfx.Rumble();
                Sfx.Crack();
                if (_pokerBingo) Sfx.Combo(5);
            }
            bool tap = Event.current != null && Event.current.type == EventType.MouseDown;
            float hold = _pokerBingo ? 5.2f : 2.6f;
            float tapAt = _pokerBingo ? 2.4f : 1.35f;
            if (_pokerStampT >= hold || (_pokerStampT >= tapAt && tap))
            {
                _pokerStamp = false;
                _pokerBingo = false;
            }
        }

        static void DrawInkStamp(Rect r, float rot, float alpha, bool word)
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
                var st = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = false
                };
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

            var title = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            string boardTitle = _pokerBingo ? "FLOCK COMPLETE" : "FLOCK FIVE  PUNCH CARD";
            title.fontSize = FitFont(title, boardTitle, paper.width * 0.92f, 28f * s, 14, 26);
            StampOutlined(new Rect(paper.x, paper.y + 6f * s, paper.width, 26f * s), boardTitle, title,
                _pokerBingo ? new Color(0.55f, 0.28f, 0.06f) : new Color(0.28f, 0.14f, 0.06f), 2, 1);
            if (BirdPoker.FullcardCount > 0 || _pokerBingo)
            {
                var life = new GUIStyle(title) { fontStyle = FontStyle.Bold };
                string lifeTx = "FULLCARDS  " + BirdPoker.FullcardCount;
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
                bool on = BirdPoker.IsPunched(i);
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
                bool covering = focus && t >= 0.55f && t < 1.05f;
                if (on && !covering)
                    DrawInkStamp(r, -10f, 0.92f, true);
            }

            DrawPokerStamper(x0, gridTop, cell, gap, cols, t, s);

            if (_pokerBingo && t >= 1.35f)
            {
                var glow = GlowTex();
                float burst = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(t * 3.1f));
                GUI.color = new Color(1f, 0.82f, 0.28f, 0.22f * burst);
                float halo = boardW * (1.08f + 0.08f * burst);
                GUI.DrawTexture(new Rect(board.center.x - halo * 0.5f, board.center.y - halo * 0.5f, halo, halo), glow, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
                float cascade = t - 1.35f;
                for (int i = 0; i < BirdPoker.PunchKinds; i++)
                {
                    if (i == _pokerStampKind) continue;
                    float at = i * 0.06f;
                    if (cascade < at) continue;
                    int col = i % cols;
                    int row = i / cols;
                    var cellR = new Rect(x0 + col * (cell + gap), gridTop + row * (cell + gap), cell, cell);
                    float u = Mathf.Clamp01((cascade - at) / 0.16f);
                    float sz = cell * Mathf.Lerp(1.45f, 1.0f, u);
                    var mark = new Rect(cellR.center.x - sz * 0.5f, cellR.center.y - sz * 0.5f, sz, sz);
                    DrawInkStamp(mark, Mathf.Lerp(-16f, -8f, u), 0.55f + 0.40f * u, u > 0.35f);
                }
            }

            if (t >= 1.4f)
            {
                var hint = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Italic,
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = Mathf.RoundToInt(14 * s)
                };
                string hintTx = _pokerBingo
                    ? "FULLCARD  " + Purse.Compact(BirdPoker.FullcardPrize) + "   ·   lifetime " + BirdPoker.FullcardCount
                    : "tap to continue";
                StampOutlined(new Rect(0f, board.yMax + 12f * s, Screen.width, 24f * s), hintTx, hint, new Color(1f, 0.92f, 0.7f), 1, 1);
            }
            GUI.matrix = prevGui;
        }

        const float StampDropAt = 0.55f;
        const float StampHitAt = 1.05f;
        const float StampToolU = 0.41f;
        const float StampToolV = 0.82f;
        const float StampToolAspect = 0.61f;

        void DrawPokerStamper(float x0, float gridTop, float cell, float gap, int cols, float t, float s)
        {
            if (t < StampDropAt || _pokerStampKind < 0) return;
            int col = _pokerStampKind % cols;
            int row = _pokerStampKind / cols;
            var cellR = new Rect(x0 + col * (cell + gap), gridTop + row * (cell + gap), cell, cell);
            float contactX = cellR.center.x;
            float contactY = cellR.center.y;
            float startY = contactY - Screen.height * 0.70f;
            float rubberX, rubberY, rot, squash;
            float liftEnd = StampHitAt + 0.55f;
            if (t < StampHitAt)
            {
                float u = Mathf.Clamp01((t - StampDropAt) / (StampHitAt - StampDropAt));
                float drop = 1f - Mathf.Pow(1f - u, 2.8f);
                rubberX = Mathf.Lerp(contactX + cell * 0.42f, contactX, drop);
                rubberY = Mathf.Lerp(startY, contactY, drop);
                rot = Mathf.Lerp(16f, -6f, drop);
                squash = 1f;
            }
            else
            {
                float u = Mathf.Clamp01((t - StampHitAt) / 0.50f);
                float peel = u < 0.16f ? 0f : (u - 0.16f) / 0.84f;
                peel = peel * peel;
                rubberX = contactX + peel * cell * 0.22f;
                rubberY = Mathf.Lerp(contactY, startY - 30f * s, peel);
                rot = Mathf.Lerp(-6f, 12f, peel);
                squash = t < StampHitAt + 0.10f
                    ? 1f - 0.13f * Mathf.Sin(Mathf.Clamp01((t - StampHitAt) / 0.10f) * Mathf.PI)
                    : 1f;
            }
            var tool = SpriteCatalog.StampTool;
            float a = 1f;
            if (t > liftEnd - 0.14f)
                a = Mathf.Clamp01((liftEnd - t) / 0.14f);
            if (a < 0.02f) return;
            if (tool != null && tool.texture != null)
            {
                float toolH = cell * 4.8f;
                float toolW = toolH * StampToolAspect;
                float x = rubberX - toolW * StampToolU;
                float y = rubberY - toolH * StampToolV * squash;
                var prev = GUI.matrix;
                GUIUtility.RotateAroundPivot(rot, new Vector2(rubberX, rubberY));
                GUI.color = new Color(1f, 1f, 1f, a);
                GUI.DrawTexture(new Rect(x, y, toolW, toolH * squash), tool.texture, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
                GUI.matrix = prev;
                return;
            }
            float stampU = Mathf.Clamp01((t - StampDropAt) / (StampHitAt - StampDropAt));
            float dropInk = 1f - Mathf.Pow(1f - stampU, 2.6f);
            float stampSize = Mathf.Lerp(cell * 2.15f, cell * 1.08f, dropInk);
            var stamp = new Rect(cellR.center.x - stampSize * 0.5f, rubberY - stampSize * 0.5f, stampSize, stampSize);
            DrawInkStamp(stamp, rot, Mathf.Clamp01(stampU * 1.5f) * a, true);
        }

        void DrawPokerPurse(float top, float s)
        {
            string coins = Purse.Cash;
            float icon = Mathf.Clamp(38f * s, 30f, 48f);
            float gap = 6f * s;
            var st = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight,
                wordWrap = false
            };
            st.fontSize = Mathf.RoundToInt(icon * 0.72f);
            float labelW = Mathf.Max(st.CalcSize(new GUIContent(coins)).x + 8f * s, 64f * s);
            float right = Screen.width - Mathf.Max(12f, Screen.width - Screen.safeArea.xMax + 10f);
            var ir = new Rect(right - icon, top + (40f * s - icon) * 0.5f, icon, icon);
            var coinR = new Rect(ir.x - gap - labelW, ir.y, labelW, icon);
            st.fontSize = FitFont(st, coins, coinR.width * 0.98f, coinR.height, 16, Mathf.RoundToInt(icon * 0.85f));
            StampOutlined(coinR, coins, st, new Color(0.42f, 0.26f, 0.08f), 2, 1);
            var coinSpr = SpriteCatalog.Coin;
            if (coinSpr != null && coinSpr.texture != null)
                DrawIdleCoin(ir, coinSpr.texture);
        }

        const float DealGatherT = 0.32f;
        const float DealRiffleT = 0.56f;
        const float DealFanT = 0.50f;
        const float DealBumpT = 0.24f;
        const float PokerFanScale = 1.42f;
        const float PokerFanSpan = 0.46f;
        // Full thumb pad/nail at the bottom-fan pinch (front layer = thumb only).
        const float PokerFanPinchU = 0.512f;
        const float PokerFanPinchV = 0.45f;
        const float PokerFanHandAspect = 0.80f;

        static float PokerAliveBreathe() => Mathf.Sin(Time.unscaledTime * 1.18f);
        static float PokerAliveTick() => Mathf.Sin(Time.unscaledTime * 2.02f);

        void TryPokerDeal()
        {
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
                    if (_pokerPendingStamp)
                    {
                        BeginPokerStamp(BirdPoker.LastPunchKind);
                        _pokerPendingStamp = false;
                    }
                    if (BirdPoker.LastRank >= BirdPoker.Rank.FullHouse) Sfx.Celebrate();
                    else if (_pokerResultCue == 2) Sfx.Clink();
                    else if (_pokerResultCue == 1) Sfx.Deny();
                    _pokerResultCue = 0;
                }
                _pokerMotion = PokerMotion.None;
            }
        }

        void BeginPokerDeal()
        {
            _pokerMotion = PokerMotion.Deal;
            _pokerMotionT = 0f;
            _pokerFan = false;
            _pokerChained = false;
            _pokerShowPay = false;
            _pokerChainBreak = -1f;
            _pokerHover = -1;
            for (int i = 0; i < _pokerRedraw.Length; i++)
            {
                _pokerRedraw[i] = true;
                _pokerHoldSlide[i] = 0f;
                _pokerKept[i] = false;
            }
        }

        void BeginPokerDraw()
        {
            _pokerMotion = PokerMotion.Draw;
            _pokerMotionT = 0f;
            _pokerPluckN = 0;
            for (int i = 0; i < BirdPoker.HandSize; i++)
            {
                _pokerDrawFrom[i] = _pokerPoseR[i];
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
            PunchPoker(0.16f, 4.2f, 1.0f);
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

        void PunchPoker(float dur, float amp, float twist)
        {
            _pokerKickDur = Mathf.Max(0.08f, dur);
            _pokerKickAmp = amp;
            _pokerKickTwist = twist;
            _pokerKickT = _pokerKickDur;
            if (CamShake.Live != null)
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
                {
                    Sfx.Riffle();
                    PunchPoker(0.28f, 5.5f, 1.4f);
                }
                float fanAt = DealGatherT + DealRiffleT;
                if (prev < fanAt && now >= fanAt)
                {
                    Sfx.CardRustle();
                    PunchPoker(0.16f, 3.2f, 0.8f);
                }
                for (int i = 0; i < BirdPoker.HandSize; i++)
                {
                    float slap = fanAt + 0.08f + i * 0.07f;
                    if (prev < slap && now >= slap) Sfx.CardSlap();
                }
                float bumpAt = fanAt + DealFanT;
                if (prev < bumpAt && now >= bumpAt)
                {
                    Sfx.CardBump();
                    PunchPoker(0.22f, 9.5f, 2.2f);
                }
            }
            else if (_pokerMotion == PokerMotion.Draw)
            {
                if (prev < 0.04f && now >= 0.04f && _pokerChainBreak >= 0f)
                {
                    Sfx.ChainSnap();
                    PunchPoker(0.28f, 10.5f, 2.4f);
                }
                int fi = 0;
                for (int i = 0; i < BirdPoker.HandSize; i++)
                {
                    if (!_pokerRedraw[i]) continue;
                    float at = _pokerPluckEnd + fi * 0.16f;
                    if (prev < at + 0.12f && now >= at + 0.12f)
                    {
                        Sfx.CardPop();
                        PunchPoker(0.12f, 5.2f, 1.1f);
                    }
                    fi++;
                }
                int di = 0;
                for (int i = 0; i < BirdPoker.HandSize; i++)
                {
                    if (!_pokerRedraw[i]) continue;
                    float at = _pokerReplaceEnd + di * 0.15f;
                    if (prev < at && now >= at) Sfx.CardSlap();
                    di++;
                }
                if (prev < _pokerRowEnd - 0.08f && now >= _pokerRowEnd - 0.08f)
                {
                    Sfx.CardBump();
                    PunchPoker(0.20f, 6.8f, 1.5f);
                }
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

        Rect PokerFanSeatAt(int fanIndex, int fanCount, Rect row, float bump)
        {
            float t = fanCount <= 1 ? 0.5f : fanIndex / (float)(fanCount - 1);
            float cardH = row.height * PokerFanScale;
            float cardW = cardH / 1.42f;
            float span = row.width * PokerFanSpan * Mathf.Lerp(0.36f, 1f, (fanCount - 1) / 4f);
            // Shrink the whole fan uniformly if it would clip — never shove ends independently.
            float leftPad = Screen.width * 0.07f;
            float rightPad = Screen.width * 0.07f;
            float maxSpan = Screen.width - leftPad - rightPad - cardW;
            if (maxSpan < 8f) maxSpan = 8f;
            if (span > maxSpan) span = maxSpan;
            float x0 = (Screen.width - (span + cardW)) * 0.5f;
            if (x0 < leftPad) x0 = leftPad;
            float x = x0 + span * t;
            float mid = 1f - Mathf.Abs(t * 2f - 1f);
            float y = row.y + row.height * 2.12f - mid * 12f + bump;
            float breathe = PokerAliveBreathe();
            // Ride the hand's breath. Independent bob made cards swim through fingers.
            x += breathe * 0.55f;
            float life = breathe * 1.05f;
            return new Rect(x, y + life, cardW, cardH);
        }

        float PokerFanRoll(int i)
        {
            int fanCount;
            int slot = PokerFanSlot(i, out fanCount);
            if (BirdPoker.Hold[i] || fanCount <= 0)
                return PokerFanRollAt(i / (float)(BirdPoker.HandSize - 1));
            float t = fanCount <= 1 ? 0.5f : slot / (float)(fanCount - 1);
            return PokerFanRollAt(t) + PokerAliveBreathe() * 0.40f;
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
            showFace = BirdPoker.PhaseNow != BirdPoker.Phase.Idle;
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
                    u = u * u * (3f - 2f * u);
                    var dest = classic;
                    r = LerpRect(from, dest, u);
                    float pop = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - 0.62f) / 0.38f));
                    float sc = Mathf.Lerp(1f, 0.08f, pop);
                    float nw = r.width * Mathf.Max(0.02f, sc);
                    float nh = r.height * Mathf.Max(0.02f, sc);
                    r = new Rect(r.center.x - nw * 0.5f, r.center.y - nh * 0.5f, nw, nh);
                    // Slow Y-axis flip while floating to the hold seat, then confetti.
                    yaw = u * 210f * (i % 2 == 0 ? 1f : -1f);
                    roll = PokerFanRoll(i) * (1f - u);
                    showFace = pop < 0.88f;
                    face = _pokerPrev[i];
                }
                else
                {
                    float at = _pokerReplaceEnd + discSlot * 0.15f;
                    float u = Mathf.Clamp01((_pokerMotionT - at) / 0.22f);
                    u = u * u * (3f - 2f * u);
                    var inbound = new Rect(-classic.width * 1.2f, classic.y - 20f, classic.width, classic.height);
                    r = LerpRect(inbound, classic, u);
                    yaw = (1f - u) * 180f;
                    showFace = u >= 0.45f;
                    roll = 0f;
                    face = u < 0.45f ? default : BirdPoker.Hand[i];
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

        void DrawPokerFanHand(Rect row, float cardW, float cardH, float s, bool front)
        {
            int live = 0;
            for (int i = 0; i < BirdPoker.HandSize; i++)
                if (!BirdPoker.Hold[i] && !(_pokerMotion == PokerMotion.Draw && _pokerKept[i])) live++;
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
                u = 1f - Mathf.Clamp01(_pokerMotionT / 0.22f);
            }
            if (!show || u < 0.02f) return;
            var spr = front ? SpriteCatalog.HandFanFront : SpriteCatalog.HandFan;
            if (spr == null || spr.texture == null) return;
            int gripN = Mathf.Max(1, live);
            var grip = PokerFanSeatAt(Mathf.Min(gripN - 1, gripN / 2), gripN, row, bump);
            float fanH = row.height * PokerFanScale;
            float h = fanH * 2.05f;
            float w = h * PokerFanHandAspect;
            float alive = u;
            float breathe = PokerAliveBreathe();
            float tick = PokerAliveTick();
            // Pads land on the remaining fan (tracks 1-card unhold, not row-center).
            // Breath only at the pinch pivot so pads stay on the bottom rims.
            float pinchX = grip.center.x - grip.width * 0.10f + _pokerKick.x + 0.85f * breathe * alive;
            float pinchY = grip.yMax - grip.height * 0.02f + _pokerKick.y
                + (0.90f * breathe + 0.18f * tick) * alive;
            float x = pinchX - w * PokerFanPinchU;
            float y = pinchY - h * PokerFanPinchV + (1f - u) * h * 0.35f;
            // Sleeve originates off the left/bottom. Keep the pinch locked.
            if (x > -6f)
            {
                w = (pinchX + 6f) / Mathf.Max(0.12f, PokerFanPinchU);
                h = w / PokerFanHandAspect;
                x = pinchX - w * PokerFanPinchU;
                y = pinchY - h * PokerFanPinchV + (1f - u) * h * 0.35f;
            }
            float needH = (Screen.height + 10f - pinchY) / Mathf.Max(0.12f, 1f - PokerFanPinchV);
            if (h < needH)
            {
                h = needH;
                w = h * PokerFanHandAspect;
                x = pinchX - w * PokerFanPinchU;
                y = pinchY - h * PokerFanPinchV + (1f - u) * h * 0.35f;
            }
            float ang = 3.2f + 0.55f * breathe * alive;
            float squash = 1f + 0.006f * breathe * alive;
            var prev = GUI.matrix;
            GUIUtility.RotateAroundPivot(ang, new Vector2(pinchX, pinchY));
            GUIUtility.ScaleAroundPivot(new Vector2(squash, 2f - squash), new Vector2(pinchX, pinchY));
            GUI.color = u >= 0.98f ? Color.white : new Color(1f, 1f, 1f, u);
            DrawSprite(new Rect(x, y, w, h), spr, true);
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

        void TickPokerDash(float s)
        {
            if (_pokerChainBreak >= 0f)
                _pokerChainBreak += Time.unscaledDeltaTime / 0.72f;
            float want = 0f;
            if (BirdPoker.PhaseNow != BirdPoker.Phase.Idle)
                want = 1f;
            if (_pokerMotion == PokerMotion.Deal)
            {
                float fanAt = DealGatherT + DealRiffleT;
                want = _pokerMotionT >= fanAt ? 1f : 0f;
            }
            _pokerDash = Mathf.MoveTowards(_pokerDash, want, Time.unscaledDeltaTime * 3.4f);
        }

        bool DrawPokerDash(Rect betR, Rect actR, float s, bool busy, bool steppers, string act)
        {
            float lift = _pokerDash * (betR.height + 48f * s);
            var bet = new Rect(betR.x, betR.y + lift, betR.width, betR.height);
            var actBox = actR;
            DrawPokerBetBar(bet, s, busy, steppers);
            DrawPokerWinBanner(betR, actR, s);
            bool fire = DrawFloralBtn(actBox, act, s, true);
            return fire;
        }

        void DrawPokerWinBanner(Rect betR, Rect actR, float s)
        {
            if (!_pokerShowPay || BirdPoker.LastWin <= 0) return;
            string win = "WIN " + Purse.Compact(BirdPoker.LastWin);
            float h = Mathf.Clamp(42f * s, 36f, 56f);
            float w = Mathf.Min(betR.width + actR.width * 0.15f, Screen.width * 0.52f);
            var r = new Rect(betR.x, betR.y - h - 8f * s, w, h);
            if (r.y < 8f) r.y = 8f;
            var st = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false
            };
            st.fontSize = Mathf.RoundToInt(h * 0.62f);
            int stroke = Mathf.Max(2, Mathf.RoundToInt(st.fontSize * 0.10f));
            StampOutlined(r, win, st, new Color(0.94f, 0.12f, 0.14f, 1f), stroke, 2);
        }

        void DrawPokerChain(Rect wrap, float s, bool behind)
        {
            if (_pokerChainBreak >= 1f) return;
            if (!_pokerChained && _pokerChainBreak < 0f) return;
            var spr = SpriteCatalog.Chain;
            var tex = spr != null ? spr.texture : null;
            float h = Mathf.Clamp(wrap.height * 0.24f, 24f * s, 44f * s);
            float midY = wrap.y + wrap.height * 0.44f;
            // End loops live behind the card; lock + inner links sit on the face.
            var back = new Rect(wrap.center.x - wrap.width * 0.68f, midY - h * 0.40f, wrap.width * 1.36f, h * 0.80f);
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
            if (tex == null)
            {
                GUI.color = new Color(0.72f, 0.55f, 0.16f, behind ? 0.55f : 0.95f);
                GUI.DrawTexture(behind ? back : front, Texture2D.whiteTexture);
                GUI.color = Color.white;
                return;
            }
            if (behind)
            {
                // End loops peek past the left/right edges from behind the card.
                GUI.color = new Color(0.70f, 0.62f, 0.38f, 1f);
                GUI.DrawTexture(back, tex, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
                return;
            }
            // Lock and inner links on the face — end loops stay on the back.
            GUI.color = Color.white;
            GUI.DrawTextureWithTexCoords(front, tex, new Rect(0.20f, 0f, 0.60f, 1f));
            // Short links turning around each side, joining front to back.
            float sideH = front.height * 0.70f;
            float sideW = front.height * 0.62f;
            var prev = GUI.matrix;
            var left = new Rect(card.x - sideW * 0.48f, front.center.y - sideH * 0.50f, sideW, sideH);
            GUIUtility.RotateAroundPivot(-78f, new Vector2(card.x, front.center.y));
            GUI.DrawTextureWithTexCoords(left, tex, new Rect(0.02f, 0f, 0.16f, 1f));
            GUI.matrix = prev;
            var right = new Rect(card.xMax - sideW * 0.52f, front.center.y - sideH * 0.50f, sideW, sideH);
            GUIUtility.RotateAroundPivot(78f, new Vector2(card.xMax, front.center.y));
            GUI.DrawTextureWithTexCoords(right, tex, new Rect(0.82f, 0f, 0.16f, 1f));
            GUI.matrix = prev;
            DrawPadlock(card, front.height);
        }

        static void DrawPadlock(Rect card, float chainH)
        {
            var spr = SpriteCatalog.Padlock;
            if (spr == null || spr.texture == null) return;
            float lockH = Mathf.Clamp(card.height * 0.22f, chainH * 0.92f, card.height * 0.26f);
            float lockW = lockH * 1.38f;
            // Mid-face, above the wild ribbon (~0.72) so the lock does not cover WILD.
            float y = card.y + card.height * 0.33f;
            var lr = new Rect(card.center.x - lockW * 0.5f, y, lockW, lockH);
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

        void DrawPokerFlames(Rect row, float cardH, float s)
        {
            if (_pokerMotion != PokerMotion.Draw) return;
            if (_pokerMotionT < _pokerPluckEnd || _pokerMotionT > _pokerReplaceEnd + 0.18f) return;
            var glow = GlowTex();
            int slot = 0;
            for (int i = 0; i < BirdPoker.HandSize; i++)
            {
                if (!_pokerRedraw[i]) continue;
                float at = _pokerPluckEnd + slot * 0.12f;
                float u = Mathf.Clamp01((_pokerMotionT - at) / 0.72f);
                slot++;
                if (u < 0.55f || u > 1.08f) continue;
                float pop = Mathf.Clamp01((u - 0.62f) / 0.38f);
                Rect r;
                float yaw, roll;
                bool showFace, sparkle;
                var face = _pokerPrev[i];
                var classic = new Rect(row.x, row.y, row.height / 1.42f, row.height);
                PokerPose(i, classic, row, out r, out yaw, out roll, out showFace, out face, out sparkle);
                Vector2 c = r.center;
                var pip = PokerPip(face.Wild ? BirdColor.Gold : face.Color);
                const int bits = 42;
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

        void DrawPokerDealHand(Rect row, float cardW, float cardH, float s)
        {
            if (_pokerMotion != PokerMotion.Draw) return;
            if (_pokerMotionT < _pokerReplaceEnd || _pokerMotionT > _pokerRowEnd) return;
            var spr = SpriteCatalog.HandPluck;
            if (spr == null || spr.texture == null) return;
            int slot = 0;
            int cur = 0;
            for (int i = 0; i < BirdPoker.HandSize; i++)
            {
                if (!_pokerRedraw[i]) continue;
                float at = _pokerReplaceEnd + slot * 0.15f;
                if (_pokerMotionT >= at) cur = i;
                slot++;
            }
            int n = 0;
            for (int i = 0; i < BirdPoker.HandSize; i++)
                if (_pokerRedraw[i]) n++;
            if (n == 0) return;
            float last = _pokerReplaceEnd + (n - 1) * 0.15f + 0.22f;
            float enter = Mathf.Clamp01((_pokerMotionT - _pokerReplaceEnd) / 0.10f);
            float exit = 1f - Mathf.Clamp01((_pokerMotionT - last) / 0.16f);
            float a = enter * exit;
            if (a < 0.02f) return;
            var seat = new Rect(row.x + cur * (cardW + 2f * s), row.y, cardW, cardH);
            float w = cardH * 2.2f;
            var hr = new Rect(seat.x - w * 0.72f + _pokerKick.x, seat.y + seat.height * 0.15f + _pokerKick.y, w, w);
            GUI.color = new Color(1f, 1f, 1f, a);
            DrawSprite(hr, spr, true);
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
            bool fanCard = dealt && _pokerFan && !PokerCardLifted(i) && _pokerHoldSlide[i] < 0.40f;
            _pokerPoseRoll[i] = roll;
            if (fanCard)
                _pokerPoseR[i] = new Rect(r.x - r.width * 0.03f, r.y - r.height * 0.16f, r.width * 1.06f, r.height * 1.16f);
            else
                _pokerPoseR[i] = r;

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
            if (fanCard)
                GUIUtility.ScaleAroundPivot(new Vector2(1f, 1.08f), rollPivot);
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
            if (!_pokerKeepHint) return;
            if (!_pokerFan || BirdPoker.PhaseNow != BirdPoker.Phase.Dealt) return;
            if (_pokerMotion != PokerMotion.None) return;
            // Same outlined BET style, one line.
            int type = Mathf.RoundToInt(Mathf.Clamp(28f * s, 26f, 40f));
            var st = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false
            };
            st.fontSize = type;
            float cap = type * 1.40f;
            var fan = PokerFanSeatAt(0, BirdPoker.HandSize, row, 0f);
            float y = fan.y - cap - 28f * s;
            if (y < 44f * s) y = 44f * s;
            var fill = new Color(0.94f, 0.12f, 0.14f, 1f);
            int stroke = Mathf.Max(2, Mathf.RoundToInt(type * 0.08f));
            StampOutlined(new Rect(0f, y, Screen.width, cap), "TAP A CARD TO KEEP", st, fill, stroke, 2);
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
            bool wild = card.Wild;
            float phase = r.x * 0.013f + r.y * 0.007f;
            if (wild && sparkle) DrawWildHalo(r, phase);
            GUI.color = wild
                ? new Color(0.42f, 0.26f, 0.08f, 1f)
                : new Color(0.22f, 0.12f, 0.08f, 1f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            if (wild)
            {
                GUI.color = new Color(0.86f, 0.68f, 0.22f, 1f);
                GUI.DrawTexture(new Rect(r.x + 1.6f * s, r.y + 1.6f * s, r.width - 3.2f * s, r.height - 3.2f * s), Texture2D.whiteTexture);
            }
            var inner = new Rect(r.x + 2.6f * s, r.y + 2.6f * s, r.width - 5.2f * s, r.height - 5.2f * s);
            DrawCardPaper(inner, wild);
            if (wild)
            {
                var joker = SpriteCatalog.Joker;
                if (joker != null && joker.texture != null)
                {
                    float pad = inner.width * 0.02f;
                    GUI.DrawTexture(
                        new Rect(inner.x + pad, inner.y + pad, inner.width - pad * 2f, inner.height - pad * 2f),
                        joker.texture, ScaleMode.ScaleToFit, true);
                }
                DrawWildBanner(inner, s, phase, sparkle);
                if (sparkle) DrawWildSparkles(r, inner, phase);
                return;
            }
            var spr = SpriteCatalog.Bird(card.Color, card.Sex);
            if (spr != null && spr.texture != null)
            {
                float pad = inner.width * 0.03f;
                GUI.DrawTexture(new Rect(inner.x + pad, inner.y + pad, inner.width - pad * 2f, inner.height - pad * 2f), spr.texture, ScaleMode.ScaleToFit, true);
            }
            DrawCardSheen(inner, phase);
            var pip = PokerPip(card.Color);
            float pr2 = r.width * 0.16f;
            GUI.color = new Color(0.99f, 0.96f, 0.88f, 0.55f);
            GUI.DrawTexture(new Rect(inner.x + 3f * s, inner.y + 3f * s, pr2 + 4f * s, pr2 + 4f * s), Texture2D.whiteTexture);
            GUI.color = pip;
            GUI.DrawTexture(new Rect(inner.x + 5f * s, inner.y + 5f * s, pr2, pr2), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        static void DrawCardSheen(Rect inner, float phase)
        {
            float t = Mathf.Repeat(Time.unscaledTime * 0.22f + phase * 0.15f, 1.8f);
            if (t > 1f) return;
            float fade = Mathf.Sin(t * Mathf.PI);
            var glow = GlowTex();
            float x = inner.x + inner.width * (t * 1.15f - 0.18f);
            var band = new Rect(x, inner.y + inner.height * 0.06f, inner.width * 0.22f, inner.height * 0.88f);
            GUI.color = new Color(1f, 0.97f, 0.88f, 0.16f * fade);
            GUI.DrawTexture(band, glow, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
        }

        static void DrawWildHalo(Rect r, float phase)
        {
            float t = Time.unscaledTime;
            float breathe = 0.5f + 0.5f * Mathf.Sin(t * 2.15f + phase);
            float pad = r.width * (0.10f + 0.06f * breathe);
            var glow = GlowTex();
            GUI.color = new Color(1f, 0.84f, 0.38f, 0.22f + 0.20f * breathe);
            GUI.DrawTexture(new Rect(r.x - pad, r.y - pad, r.width + pad * 2f, r.height + pad * 2f), glow, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
        }

        static void DrawWildSparkles(Rect r, Rect inner, float phase)
        {
            float t = Time.unscaledTime;
            var glow = GlowTex();
            GUI.BeginGroup(inner);
            float sheenU = Mathf.Repeat(t * 0.36f + phase, 1.75f);
            if (sheenU < 1f)
            {
                float fade = Mathf.Sin(sheenU * Mathf.PI);
                float x = inner.width * (sheenU * 1.25f - 0.22f);
                var band = new Rect(x, inner.height * 0.02f, inner.width * 0.30f, inner.height * 0.96f);
                GUI.color = new Color(1f, 0.96f, 0.78f, 0.36f * fade);
                GUI.DrawTexture(band, glow, ScaleMode.ScaleToFit, true);
            }
            GUI.EndGroup();

            var spark = SpriteCatalog.Sparkle;
            var tex = spark != null && spark.texture != null ? spark.texture : glow;
            float baseSz = r.width * 0.22f;
            for (int i = 0; i < WildGlints.Length; i++)
            {
                float tw = Mathf.Sin(t * 2.4f + phase + i * 1.17f);
                tw = Mathf.Max(0f, tw);
                tw = tw * tw;
                if (tw < 0.10f) continue;
                var uv = WildGlints[i];
                float sz = baseSz * (0.45f + 0.85f * tw);
                var gr = new Rect(
                    r.x + r.width * uv.x - sz * 0.5f,
                    r.y + r.height * uv.y - sz * 0.5f,
                    sz, sz);
                GUI.color = new Color(1f, 0.95f, 0.72f, 0.35f + 0.55f * tw);
                GUI.DrawTexture(gr, tex, ScaleMode.ScaleToFit, true);
            }
            GUI.color = Color.white;
        }

        static void DrawWildBanner(Rect inner, float s, float phase, bool sheen = true)
        {
            float bandH = Mathf.Max(22f * s, inner.height * 0.28f);
            // Centered on the card; swallowtails may sprawl past the edges.
            float breathe = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 1.15f + phase);
            float sprawl = sheen ? 1.02f + 0.06f * breathe : 1f;
            var prev = GUI.matrix;
            float bandW = inner.width * 2.08f;
            float bandY = inner.y + inner.height * 0.72f - bandH * 0.5f;
            var band = new Rect(inner.center.x - bandW * 0.5f, bandY, bandW, bandH);
            GUIUtility.ScaleAroundPivot(new Vector2(sprawl, 1f), band.center);
            var spr = SpriteCatalog.WildBanner;
            if (spr != null && spr.texture != null)
            {
                GUI.color = Color.white;
                DrawSprite(band, spr, true);
            }
            else
            {
                GUI.color = new Color(0.10f, 0.16f, 0.42f, 1f);
                GUI.DrawTexture(new Rect(band.x - 2f * s, band.y - 2f * s, band.width + 4f * s, band.height + 4f * s), Texture2D.whiteTexture);
                GUI.color = new Color(0.86f, 0.68f, 0.18f, 1f);
                GUI.DrawTexture(band, Texture2D.whiteTexture);
                GUI.color = new Color(0.12f, 0.22f, 0.62f, 1f);
                GUI.DrawTexture(new Rect(band.x + 3f * s, band.y + 3f * s, band.width - 6f * s, band.height - 6f * s), Texture2D.whiteTexture);
            }
            GUI.matrix = prev;

            var st = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false
            };
            string lab = "WILD";
            var textR = new Rect(inner.center.x - inner.width * 0.5f, band.y, inner.width, bandH);
            st.fontSize = FitFont(st, lab, textR.width * 0.78f, textR.height * 0.62f, 16, 36);
            int stroke = Mathf.Max(3, Mathf.RoundToInt(st.fontSize * 0.16f));
            var gold = new Color(1f, 0.86f, 0.28f, 1f);
            StampOutlined(textR, lab, st, gold, stroke, Mathf.Max(2, stroke - 1));
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

            var led = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false
            };
            var red = new Color(0.94f, 0.12f, 0.14f, 1f);
            led.fontSize = type;
            int betStroke = Mathf.Max(2, Mathf.RoundToInt(type * 0.08f));
            StampOutlined(amount, "BET " + Purse.Compact(BirdPoker.Bet), led, red, betStroke, 2);

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
            GUI.color = new Color(0.10f, 0.06f, 0.03f, 0.40f * a);
            GUI.DrawTexture(new Rect(chip.x + shadow, chip.y + shadow, chip.width, chip.height), Texture2D.whiteTexture);
            GUI.color = new Color(0.78f, 0.58f, 0.18f, (held ? 0.98f : 0.92f) * a);
            GUI.DrawTexture(chip, Texture2D.whiteTexture);
            float inset = Mathf.Max(2f, chip.width * 0.10f);
            GUI.color = new Color(0.96f, 0.90f, 0.62f, (held ? 1f : 0.94f) * a);
            GUI.DrawTexture(new Rect(chip.x + inset, chip.y + inset, chip.width - inset * 2f, chip.height - inset * 2f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var st = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            st.fontSize = Mathf.RoundToInt(chip.height * 0.56f);
            StampOutlined(chip, glyph, st, new Color(0.36f, 0.18f, 0.07f, a), 2, 1);
            return fire;
        }

        bool DrawFloralBtn(Rect r, string label, float s) => DrawFloralBtn(r, label, s, false);

        bool DrawFloralBtn(Rect r, string label, float s, bool hero)
        {
            bool fire = HitPad(r, out bool held);
            float sink = held ? r.height * 0.04f : 0f;
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
            var disc = FlowerDisc(r, sink);
            var st = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false
            };
            int lo = hero ? 22 : 14;
            int hi = hero ? 48 : 28;
            st.fontSize = FitFont(st, label, disc.width * (hero ? 0.92f : 0.84f), disc.height * (hero ? 0.72f : 0.62f), lo, hi);
            int stroke = hero ? Mathf.Max(2, Mathf.RoundToInt(st.fontSize * 0.06f)) : 2;
            StampOutlined(disc, label, st, new Color(0.36f, 0.18f, 0.07f), stroke, 1);
            return fire;
        }

        void DrawHivePage()
        {
            float s = Mathf.Max(Screen.height / 720f, 1f);
            DrawHomeWash(0.28f);

            var backLab = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(22 * s),
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            backLab.normal.textColor = new Color(1f, 0.94f, 0.72f);

            var safe = Screen.safeArea;
            float top = Mathf.Max(20f, Screen.height - safe.yMax + 10f);
            float btnH = 44f * Mathf.Min(s, 1.6f);
            float btnW = 132f * Mathf.Min(s, 1.6f);
            var back = new Rect(Mathf.Max(16f, safe.xMin + 10f), top, btnW, btnH);
            if (HitPad(back, out bool backHeld))
            {
                if (_hiveInspect >= 0)
                {
                    BeginPutAwayInspect();
                }
                else
                {
                    _hiveFlip = -1;
                    _hivePageTurn = 99f;
                    for (int k = 0; k < _hiveFaceBack.Length; k++) _hiveFaceBack[k] = false;
                    _hiveInspect = -1;
                    _hiveInspectT = 0f;
                    _hiveInspectClosing = false;
                    _home = HomeFace.Splash;
                }
            }
            GUI.color = new Color(0.10f, 0.08f, 0.05f, backHeld ? 0.88f : 0.72f);
            GUI.DrawTexture(back, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(back, "Back", backLab);

            float hiveSize = Mathf.Clamp(Screen.width * 0.30f, 128f, 220f);
            var hiveHead = new Rect(
                (Screen.width - hiveSize) * 0.5f,
                top + btnH + 12f * s,
                hiveSize, hiveSize);
            DrawHiveButton(hiveHead, s);
            var shareAlbum = new Rect(
                Screen.width - Mathf.Max(16f, Screen.width - safe.xMax + 10f) - btnW,
                top, btnW, btnH);
            if (HitPad(shareAlbum, out bool shareHeld))
            {
                Sfx.Chirp(BirdColor.Gold);
                Invite.Share();
            }
            GUI.color = new Color(0.10f, 0.08f, 0.05f, shareHeld ? 0.88f : 0.72f);
            GUI.DrawTexture(shareAlbum, Texture2D.whiteTexture);
            GUI.color = Color.white;
            var shareLab = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(20 * s),
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            shareLab.normal.textColor = new Color(1f, 0.94f, 0.72f);
            GUI.Label(shareAlbum, "Invite", shareLab);

            float hiveBottom = DrawHiveTally(hiveHead, s);
            int pages = Mathf.Max(1, (Hive.AlbumSlots + HivePageSize - 1) / HivePageSize);
            _hivePage = Mathf.Clamp(_hivePage, 0, pages - 1);

            if (_hiveFlip >= 0)
            {
                _hiveFlipT += Time.unscaledDeltaTime;
                if (_hiveFlipT >= 0.28f)
                {
                    _hiveFaceBack[_hiveFlip] = !_hiveFaceBack[_hiveFlip];
                    _hiveFlip = -1;
                    _hiveFlipT = 0f;
                }
            }

            bool turning = _hivePageTurn < 0.55f;
            if (turning) _hivePageTurn += Time.unscaledDeltaTime;

            // Ultra Pro clear page: 3×3 sleeves on a binder sheet
            float tabH = 40f * s;
            float pageBottom = Screen.height - 16f * s - tabH - 8f * s;
            float pageTop = hiveBottom + 22f * s;
            float pageH = pageBottom - pageTop;
            float pagePad = 22f * s;
            float pageW = Screen.width - pagePad * 2f;
            var sheet = new Rect(pagePad, pageTop, pageW, pageH);

            // Binder spine shadow + clear sheet
            GUI.color = new Color(0.08f, 0.06f, 0.04f, 0.55f);
            GUI.DrawTexture(new Rect(sheet.x - 6f * s, sheet.y + 8f * s, 14f * s, sheet.height), Texture2D.whiteTexture);
            GUI.color = new Color(0.78f, 0.88f, 0.92f, 0.22f);
            GUI.DrawTexture(sheet, Texture2D.whiteTexture);
            GUI.color = new Color(0.55f, 0.62f, 0.68f, 0.35f);
            // Sleeve grid lines
            float gap = 8f * s;
            float cellW = (sheet.width - pagePad * 0.5f - gap * 2f) / 3f;
            float cellH = (sheet.height - pagePad * 0.5f - gap * 2f) / 3f;
            // Keep cards portrait-ish inside sleeves
            float cardW = cellW - 6f * s;
            float cardH = Mathf.Min(cellH - 6f * s, cardW * 1.35f);
            float gridW = 3f * cellW + 2f * gap;
            float gridH = 3f * cellH + 2f * gap;
            float gx0 = sheet.x + (sheet.width - gridW) * 0.5f;
            float gy0 = sheet.y + (sheet.height - gridH) * 0.5f;

            int showPage = turning ? _hivePageFrom : _hivePage;
            int startIx = showPage * HivePageSize;

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

            // Page tabs along the bottom — a short strip, never 33 stacked digits.
            float tabY = pageBottom + 6f * s;
            float tabGap = 8f * s;
            int shown = Mathf.Min(pages, 5);
            float tabW = (Screen.width - 36f * s - (shown - 1) * tabGap) / Mathf.Max(1, shown);
            int first = 0;
            if (pages > shown)
                first = Mathf.Clamp(_hivePage - shown / 2, 0, pages - shown);
            float tabsW = shown * tabW + (shown - 1) * tabGap;
            float tabX0 = (Screen.width - tabsW) * 0.5f;
            var tabLab = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false
            };
            for (int n = 0; n < shown; n++)
            {
                int p = first + n;
                var tab = new Rect(tabX0 + n * (tabW + tabGap), tabY, tabW, tabH);
                bool on = p == _hivePage && !turning;
                bool held = false;
                bool hit = !turning && _hiveInspect < 0 && HitPad(tab, out held);
                GUI.color = on
                    ? new Color(0.92f, 0.72f, 0.28f, held ? 0.95f : 0.88f)
                    : new Color(0.18f, 0.14f, 0.10f, held ? 0.90f : 0.72f);
                GUI.DrawTexture(tab, Texture2D.whiteTexture);
                GUI.color = Color.white;
                string num = (p + 1).ToString();
                tabLab.fontSize = FitFont(tabLab, num, tab.width * 0.86f, tab.height * 0.70f, 16, 28);
                StampOutlined(tab, num, tabLab, on ? new Color(0.28f, 0.14f, 0.05f) : new Color(1f, 0.94f, 0.72f), 1, 1);
                if (hit && p != _hivePage)
                {
                    _hivePageFrom = _hivePage;
                    _hivePage = p;
                    _hivePageTurn = 0f;
                    _hiveFlip = -1;
                    Sfx.PageTurn();
                }
            }

            if (_hiveInspect >= 0)
                DrawHiveInspect(s);
        }

        void DrawHiveInspect(float s)
        {
            const float dur = 0.28f;
            _hiveInspectT += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(_hiveInspectT / dur);
            float ease = 1f - (1f - u) * (1f - u); // easeOut quad
            float t = _hiveInspectClosing ? (1f - ease) : ease;

            // Dim overlay
            GUI.color = new Color(0.02f, 0.02f, 0.04f, 0.72f * t);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Large portrait card ~1:1.4, fill the screen for 55+ reading
            float maxW = Screen.width * 0.92f;
            float maxH = Screen.height * 0.74f;
            float bigW, bigH;
            if (maxW * 1.4f <= maxH)
            {
                bigW = maxW;
                bigH = maxW * 1.4f;
            }
            else
            {
                bigH = maxH;
                bigW = maxH / 1.4f;
            }
            var to = new Rect(
                (Screen.width - bigW) * 0.5f,
                (Screen.height - bigH) * 0.40f,
                bigW,
                bigH);
            var from = _hiveInspectFrom;
            var big = new Rect(
                Mathf.Lerp(from.x, to.x, t),
                Mathf.Lerp(from.y, to.y, t),
                Mathf.Lerp(from.width, to.width, t),
                Mathf.Lerp(from.height, to.height, t));

            int ix = _hiveInspect;
            DrawBeeAlbumCard(big, ix, s, true);

            // Hint under card
            var hint = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false
            };
            string hintCopy = "Tap card to flip  ·  tap outside to put back";
            hint.fontSize = FitFont(hint, hintCopy, Screen.width * 0.86f, 28f * s, 16, 26);
            float hintY = big.yMax + 8f * s;
            StampOutlined(new Rect(0f, hintY, Screen.width, 32f * s), hintCopy, hint, new Color(0.96f, 0.90f, 0.70f, Mathf.Clamp01(t * 1.4f)), 2, 1);

            // Close X top-right of overlay
            var safe = Screen.safeArea;
            float top = Mathf.Max(20f, Screen.height - safe.yMax + 10f);
            float xSz = 44f * Mathf.Min(s, 1.6f);
            var xBtn = new Rect(Screen.width - Mathf.Max(16f, Screen.width - safe.xMax + 10f) - xSz, top, xSz, xSz);
            bool xHeld;
            bool xHit = HitPad(xBtn, out xHeld);
            GUI.color = new Color(0.10f, 0.08f, 0.05f, (xHeld ? 0.92f : 0.78f) * Mathf.Clamp01(t + 0.15f));
            GUI.DrawTexture(xBtn, Texture2D.whiteTexture);
            GUI.color = Color.white;
            var xLab = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(22 * s),
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            xLab.normal.textColor = new Color(1f, 0.94f, 0.72f);
            GUI.Label(xBtn, "X", xLab);

            bool fullyOpen = !_hiveInspectClosing && u >= 1f;
            // Always hit-test the card when open so taps don't fall through to "outside"
            if (fullyOpen && HitPad(big, out _))
            {
                if (_hiveFlip < 0)
                {
                    _hiveFlip = ix;
                    _hiveFlipT = 0f;
                    Sfx.PageTurn();
                }
            }

            // Tap dim outside card closes (after card/X so they win hit tests)
            var full = new Rect(0f, 0f, Screen.width, Screen.height);
            bool outside = !_hiveInspectClosing && u > 0.85f && HitPad(full, out _);
            if ((outside || xHit) && !_hiveInspectClosing)
                BeginPutAwayInspect();

            if (_hiveInspectClosing && u >= 1f)
                FinishPutAwayInspect();
        }

        void BeginPutAwayInspect()
        {
            if (_hiveInspect < 0 || _hiveInspectClosing) return;
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
            GUI.color = Color.white;
        }

        void DrawBeeAlbumCard(Rect card, int i, float s) => DrawBeeAlbumCard(card, i, s, false);

        void DrawBeeAlbumCard(Rect card, int i, float s, bool inspectView)
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
            Color wood = owned
                ? Color.Lerp(new Color(0.28f, 0.16f, 0.07f), kind.Tint, 0.35f)
                : new Color(0.14f, 0.12f, 0.10f, 0.92f);
            GUI.color = new Color(0f, 0f, 0f, 0.35f);
            GUI.DrawTexture(new Rect(r.x + 4f, r.y + 6f, r.width, r.height), Texture2D.whiteTexture);
            GUI.color = wood;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Inner face — use the plate; type needs every pixel for 55+ reading
            var face = new Rect(r.x + r.width * 0.045f, r.y + r.height * 0.035f, r.width * 0.91f, r.height * 0.93f);
            GUI.color = owned
                ? Color.Lerp(new Color(0.98f, 0.92f, 0.72f), kind.Tint, 0.18f)
                : new Color(0.22f, 0.20f, 0.18f, 0.95f);
            GUI.DrawTexture(face, Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Foil sheen: traveling band + rim (Holo cool silver; InverseRainbow hue-shift)
            if (owned && finish != BeeFinish.Normal && squash > 0.2f)
                DrawFoilSheen(face, finish, i, inspectView);

            if (!owned)
            {
                var mystery = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                mystery.fontSize = FitFont(mystery, "?", face.width * 0.55f, face.height * 0.40f, inspectView ? 48 : 28, inspectView ? 96 : 64);
                StampOutlined(new Rect(face.x, face.y + face.height * 0.16f, face.width, face.height * 0.42f), "?", mystery, new Color(0.55f, 0.48f, 0.38f), 2, 1);
                var lockSt = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperCenter,
                    wordWrap = true
                };
                string locked = "Still out buzzing";
                var lockBox = new Rect(face.x + face.width * 0.04f, face.yMax - face.height * 0.30f, face.width * 0.92f, face.height * 0.26f);
                lockSt.fontSize = FitFontWrapped(lockSt, locked, lockBox.width, lockBox.height, inspectView ? 18 : 12, inspectView ? 36 : 20);
                StampOutlined(lockBox, locked, lockSt, new Color(0.50f, 0.42f, 0.30f), inspectView ? 2 : 1, 1);
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
                var nameSt = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };
                float nameTop = inspectView ? 0.28f : 0.50f;
                float nameH = inspectView ? 0.18f : 0.18f;
                var nameBox = new Rect(face.x + face.width * 0.03f, face.y + face.height * nameTop, face.width * 0.94f, face.height * nameH);
                int nameHi = inspectView
                    ? Mathf.Clamp(Mathf.RoundToInt(nameBox.height * 0.88f), 32, 88)
                    : Mathf.Clamp(Mathf.RoundToInt(nameBox.height * 0.72f), 16, 28);
                nameSt.fontSize = FitFontWrapped(nameSt, kind.Name, nameBox.width, nameBox.height, inspectView ? 28 : 14, nameHi);
                int nameStroke = inspectView ? Mathf.Max(2, Mathf.RoundToInt(nameSt.fontSize * 0.06f)) : 2;
                StampOutlined(nameBox, kind.Name, nameSt, new Color(0.16f, 0.07f, 0.02f), nameStroke, 1);
                var frontSt = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperCenter,
                    wordWrap = true
                };
                var frontBox = new Rect(
                    face.x + face.width * 0.03f,
                    face.y + face.height * (nameTop + nameH + 0.01f),
                    face.width * 0.94f,
                    face.height * (0.96f - nameTop - nameH));
                int frontHi = inspectView
                    ? Mathf.Clamp(Mathf.RoundToInt(frontBox.height * 0.42f), 28, 88)
                    : Mathf.Clamp(Mathf.RoundToInt(frontBox.height * 0.36f), 13, 22);
                frontSt.fontSize = FitFontWrapped(frontSt, kind.Front, frontBox.width, frontBox.height, inspectView ? 22 : 13, frontHi);
                StampOutlined(frontBox, kind.Front, frontSt, new Color(0.20f, 0.09f, 0.03f), inspectView ? 2 : 1, 1);
                if (n > 1)
                {
                    var cnt = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperRight };
                    cnt.fontSize = Mathf.Max(inspectView ? 22 : 13, Mathf.RoundToInt((inspectView ? 20f : 13f) * s));
                    StampOutlined(new Rect(face.xMax - 56f * s, face.y + 4f * s, 52f * s, inspectView ? 32f * s : 20f * s), "×" + n, cnt, new Color(0.16f, 0.07f, 0.02f), inspectView ? 2 : 1, 1);
                }
                if (finish != BeeFinish.Normal)
                {
                    string foil = finish == BeeFinish.Holo ? "Holo" : "Inverse Rainbow";
                    var foilSt = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperLeft };
                    foilSt.fontSize = Mathf.RoundToInt(11 * s);
                    Color foilCol = finish == BeeFinish.Holo
                        ? new Color(0.15f, 0.48f, 0.92f)
                        : new Color(0.82f, 0.22f, 0.68f);
                    StampOutlined(new Rect(face.x + 4f * s, face.y + 4f * s, face.width * 0.7f, 18f * s), foil, foilSt, foilCol, 1, 1);
                }
            }
            else
            {
                var backTitle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };
                var titleBox = new Rect(face.x + face.width * 0.03f, face.y + face.height * 0.03f, face.width * 0.94f, face.height * 0.18f);
                int titleHi = inspectView
                    ? Mathf.Clamp(Mathf.RoundToInt(titleBox.height * 0.80f), 30, 80)
                    : Mathf.Clamp(Mathf.RoundToInt(titleBox.height * 0.70f), 14, 26);
                backTitle.fontSize = FitFontWrapped(backTitle, kind.Name, titleBox.width, titleBox.height, inspectView ? 26 : 14, titleHi);
                StampOutlined(titleBox, kind.Name, backTitle, new Color(0.16f, 0.07f, 0.02f), inspectView ? Mathf.Max(2, Mathf.RoundToInt(backTitle.fontSize * 0.06f)) : 2, 1);
                var blip = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperCenter,
                    wordWrap = true
                };
                float foilH = finish != BeeFinish.Normal ? 0.10f : 0f;
                var bodyBox = new Rect(
                    face.x + face.width * 0.04f,
                    face.y + face.height * 0.23f,
                    face.width * 0.92f,
                    face.height * (0.72f - foilH));
                int bodyHi = inspectView
                    ? Mathf.Clamp(Mathf.RoundToInt(bodyBox.height * 0.28f), 28, 96)
                    : Mathf.Clamp(Mathf.RoundToInt(bodyBox.height * 0.18f), 13, 20);
                blip.fontSize = FitFontWrapped(blip, kind.Back, bodyBox.width, bodyBox.height, inspectView ? 22 : 13, bodyHi);
                StampOutlined(bodyBox, kind.Back, blip, new Color(0.18f, 0.08f, 0.03f), inspectView ? 2 : 1, 1);
                if (finish != BeeFinish.Normal)
                {
                    string foil = finish == BeeFinish.Holo ? "Holo" : "Inverse Rainbow";
                    var foilSt = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.LowerCenter };
                    foilSt.fontSize = Mathf.Max(inspectView ? 18 : 12, Mathf.RoundToInt((inspectView ? 16f : 12f) * s));
                    Color foilCol = finish == BeeFinish.Holo
                        ? new Color(0.15f, 0.48f, 0.92f)
                        : new Color(0.82f, 0.22f, 0.68f);
                    StampOutlined(new Rect(face.x, face.yMax - face.height * 0.12f, face.width, face.height * 0.1f), foil, foilSt, foilCol, inspectView ? 2 : 1, 1);
                }
            }

            // Binder sleeve tap opens fullscreen inspect (flip lives inside inspect)
            if (!inspectView && owned && !flipping && _hivePageTurn >= 0.55f && _hiveInspect < 0 && HitPad(card, out _))
            {
                _hiveInspect = i;
                _hiveInspectT = 0f;
                _hiveInspectClosing = false;
                _hiveInspectFrom = card;
                _hiveFaceBack[i] = false;
                _hiveFlip = -1;
                _hiveFlipT = 0f;
                Sfx.PageTurn();
            }
        }

        void DrawLevelHive(float s)
        {
            var box = new Rect(20f * s, 90f * s, Screen.width - 40f * s, Mathf.Min(420f * s, Screen.height * 0.48f));
            GUI.Box(box, "");
            var title = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(24 * s), fontStyle = FontStyle.Bold };
            title.normal.textColor = new Color(1f, 0.92f, 0.7f);
            var row = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(20 * s) };
            GUILayout.BeginArea(new Rect(box.x + 16f, box.y + 10f, box.width - 32f, box.height - 20f));
            GUILayout.Label("This garden's visitors", title);
            GUILayout.Space(8f);
            if (_levelBees.Count == 0)
            {
                row.normal.textColor = new Color(0.7f, 0.66f, 0.55f);
                GUILayout.Label("No bees have dropped by yet.", row);
            }
            else
            {
                for (int i = 0; i < _levelBees.Count; i++)
                {
                    var v = _levelBees[i];
                    row.normal.textColor = v.Kind.Tint;
                    string label = v.Kind.Name;
                    if (v.Finish == BeeFinish.Holo) label += " · Holo";
                    else if (v.Finish == BeeFinish.InverseRainbow) label += " · Inverse Rainbow";
                    GUILayout.Label(label, row);
                }
            }
            GUILayout.EndArea();
        }

        bool GiftLocked(int i)
        {
            if (_board == null || (uint)i >= (uint)_board.Branches.Count) return false;
            return _board.Branches[i].AdLocked;
        }

        void OpenGift()
        {
            if (_gift != GiftFace.None || _won) return;
            if (!_frozen && _busy) return;
            if (_sel >= 0 && _garden.Branches != null)
            {
                _garden.Branches[_sel].SetReady(false);
                _sel = -1;
            }
            if (!_frozen) _freezeOffer = false;
            _gift = GiftFace.Card;
            Sfx.Chirp(BirdColor.Gold);
        }

        void CloseGift()
        {
            if (_gift == GiftFace.Movie) return;
            _gift = GiftFace.None;
        }

        IEnumerator WatchGift()
        {
            bool keep = _keepStreak;
            _gift = GiftFace.Movie;
            _busy = true;
            // GateGo is splash-only (long Lead). Ad start gets a short leave whoosh.
            Sfx.FeederLeave();
            yield return Ads.Rewarded();
            if (!Ads.LastGranted)
            {
                _gift = GiftFace.Card;
                _busy = false;
                yield break;
            }
            if (keep)
            {
                yield return SnapRound();
                _gift = GiftFace.None;
                _busy = false;
                yield break;
            }
            var popAt = GrantSpare();
            if (_frozen && _garden.Ice != null)
                yield return _garden.Ice.Shatter(_garden.Root);
            StillBirds(false);
            _frozen = false;
            _freezeOffer = false;
            yield return GardenFit.Tween(_garden, _board, false);
            _giftThanksAt = Time.unscaledTime;
            _gift = GiftFace.Thanks;
            if (_garden.Root != null)
                StartCoroutine(GiftPopBursts(popAt));
            yield return new WaitForSeconds(2.45f);
            _gift = GiftFace.None;
            _busy = false;
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
            if (_board == null) return Vector3.zero;
            int unlocked = -1;
            for (int i = 0; i < _board.Branches.Count; i++)
            {
                if (!_board.Branches[i].AdLocked) continue;
                _board.Branches[i].AdLocked = false;
                unlocked = i;
                var v = i < _garden.Branches.Length ? _garden.Branches[i] : null;
                if (v != null)
                {
                    var want = v.GetComponent<GiftWant>();
                    if (want != null) want.On = false;
                    if (v.Sign != null) v.Sign.gameObject.SetActive(false);
                    v.Shake();
                }
            }
            if (unlocked < 0) unlocked = AppendSpare();
            Sfx.FeederDone();
            var burst = unlocked >= 0 && unlocked < _garden.Branches.Length
                ? _garden.Branches[unlocked]
                : null;
            var pos = burst != null ? burst.transform.position : Vector3.zero;
            if (_garden.Root != null)
                StartCoroutine(Wow.Burst(pos, BirdColor.Gold, _garden.Root, 3));
            SyncAll();
            return pos;
        }

        int AppendSpare()
        {
            int i = _board.Branches.Count;
            _board.Branches.Add(new BranchState());
            var list = new System.Collections.Generic.List<BranchView>(_garden.Branches);
            var v = WorldBuilder.MakeSpare(i, new Vector2(WorldBuilder.EdgeX(_garden.Cam, 1.22f, WorldBuilder.GiftWoodScaleX), WorldBuilder.GiftY), _garden.Root);
            list.Add(v);
            _garden.Branches = list.ToArray();
            return i;
        }

        void DrawGiftSign(float s)
        {
            // Film+play is painted into fx_ad_sign art — no GUI sticker overlay.
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
            float top = Mathf.Max(18f, Screen.height - safe.yMax + 8f);
            float xSz = 40f * Mathf.Min(s, 1.45f);
            var xBtn = new Rect(
                Screen.width - Mathf.Max(14f, Screen.width - safe.xMax + 8f) - xSz,
                top, xSz, xSz);
            bool xHeld = false;
            if (!_freezeOffer && HitPad(xBtn, out xHeld))
            {
                if (_keepStreak)
                {
                    Purse.BreakStreak();
                    StartCoroutine(SnapRound());
                }
                else CloseGift();
                return;
            }

            float cardW = Mathf.Min(Screen.width * 0.88f, 600f * s);
            float cardH = cardW * (501f / 780f);
            float flower = Mathf.Min(Screen.width * 0.54f, 300f * s);
            float overlap = flower * 0.40f;
            float hiveY = Screen.height - 88f * s - 12f * s - 64f * s;
            float stackH = cardH + flower - overlap;
            float cardY = Mathf.Clamp(Screen.height * 0.24f, Screen.height * 0.18f, hiveY - 16f * s - stackH);
            var card = new Rect((Screen.width - cardW) * 0.5f, cardY, cardW, cardH);

            float t = Time.unscaledTime;
            float breathe = 0.5f + 0.5f * Mathf.Sin(t * 2.05f);
            var glow = GlowTex();
            float aura = card.width * (0.10f + 0.04f * breathe);
            GUI.color = new Color(1f, 0.78f, 0.22f, 0.30f + 0.22f * breathe);
            GUI.DrawTexture(new Rect(card.x - aura, card.y - aura * 0.6f, card.width + aura * 2f, card.height + aura * 1.2f), glow, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;

            var tex = SpriteCatalog.AdCard != null ? SpriteCatalog.AdCard.texture : null;
            if (tex != null)
            {
                GUI.color = new Color(0.10f, 0.06f, 0.03f, 0.40f);
                GUI.DrawTexture(new Rect(card.x + 6f, card.y + 14f, card.width, card.height), tex, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
                GUI.DrawTexture(card, tex, ScaleMode.ScaleToFit, true);
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

            string head = _keepStreak
                ? "Watch AD to keep ×" + Purse.Streak
                : (_freezeOffer ? "Icing over — thaw a limb" : "Watch AD for an extra branch");
            var headR = new Rect(plate.x + 10f * s, plate.y + 8f * s, plate.width - 20f * s, plate.height - 16f * s);
            GUI.color = new Color(1f, 0.72f, 0.16f, 0.40f + 0.22f * breathe);
            GUI.DrawTexture(new Rect(headR.x - 12f * s, headR.y - 8f * s, headR.width + 24f * s, headR.height + 16f * s), glow, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
            var title = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            title.fontSize = FitFont(title, head, headR.width, headR.height * 0.88f, 24, 44);
            int headStroke = Mathf.Max(2, Mathf.RoundToInt(title.fontSize * 0.08f));
            StampOutlined(headR, head, title, new Color(1f, 0.92f, 0.55f), 1, headStroke);

            DrawGiftMarquee(plate, s, t);

            var cta = new Rect((Screen.width - flower) * 0.5f, card.yMax - overlap, flower, flower);
            var discHit = FlowerDisc(cta, 0f);
            bool watch = HitPad(discHit, out bool held);
            var bloom = SpriteCatalog.PlayFlower;
            if (bloom != null && bloom.texture != null)
            {
                float sink = held ? cta.height * 0.028f : 0f;
                GUI.color = new Color(0.10f, 0.06f, 0.03f, 0.38f);
                GUI.DrawTexture(new Rect(cta.x + 5f, cta.y + 12f, cta.width, cta.height), bloom.texture, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
                if (!held) DrawFlowerHalo(cta, 0f);
                GUI.DrawTexture(cta, bloom.texture, ScaleMode.ScaleToFit, true);
                if (!held) DrawFlowerShimmer(cta, 0f);
                if (held) DrawDiscPress(cta, sink);
                var disc = FlowerDisc(cta, sink);
                var watchSt = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Normal,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = false
                };
                string watchLab = "Watch";
                watchSt.fontSize = FitFont(watchSt, watchLab, disc.width * 0.78f, disc.height * 0.58f, 22, 42);
                int ink = Mathf.Max(2, Mathf.RoundToInt(watchSt.fontSize * 0.08f));
                StampOutlined(disc, watchLab, watchSt, new Color(0.42f, 0.24f, 0.10f), 1, ink);
            }
            if (watch)
            {
                StartCoroutine(WatchGift());
                return;
            }

            if (!_freezeOffer) DrawGiftCloseX(xBtn, xHeld, s);
        }

        static void DrawGiftCloseX(Rect xBtn, bool held, float s)
        {
            var glow = GlowTex();
            GUI.color = new Color(0.04f, 0.03f, 0.02f, held ? 0.62f : 0.38f);
            GUI.DrawTexture(xBtn, glow, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
            var xLab = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false
            };
            xLab.fontSize = FitFont(xLab, "×", xBtn.width * 0.84f, xBtn.height * 0.84f, 20, 34);
            StampOutlined(xBtn, "×", xLab, new Color(1f, 0.88f, 0.42f, held ? 1f : 0.92f), 1, 2);
        }

        static void DrawGiftMarquee(Rect plate, float s, float t)
        {
            var bulb = SpriteCatalog.AdBulb;
            var glow = GlowTex();
            var tex = bulb != null ? bulb.texture : null;
            // Sit on the gold rim of the chalkboard, not the outer orchid wood.
            var frame = new Rect(plate.x - 6f * s, plate.y - 6f * s, plate.width + 12f * s, plate.height + 12f * s);
            const int n = 16;
            bool strobe = (Mathf.FloorToInt(t * 1.65f) % 7) == 0;
            bool strobeOn = ((int)(t * 14f) & 1) == 0;
            float perim = 2f * (frame.width + frame.height);
            float headCw = Mathf.Repeat(t * 0.52f, 1f);
            float headCcw = Mathf.Repeat(-t * 0.38f, 1f);
            float szOn = Mathf.Max(22f, plate.width * 0.048f);
            float szOff = szOn * 0.72f;
            for (int i = 0; i < n; i++)
            {
                float u = i / (float)n;
                float d = u * perim;
                Vector2 p;
                float ang;
                if (d < frame.width)
                {
                    p = new Vector2(frame.x + d, frame.y);
                    ang = 0f;
                }
                else if ((d -= frame.width) < frame.height)
                {
                    p = new Vector2(frame.xMax, frame.y + d);
                    ang = -90f;
                }
                else if ((d -= frame.height) < frame.width)
                {
                    p = new Vector2(frame.xMax - d, frame.yMax);
                    ang = 180f;
                }
                else
                {
                    d -= frame.width;
                    p = new Vector2(frame.x, frame.yMax - d);
                    ang = 90f;
                }
                float ring = Mathf.Min(Mathf.Abs(u - headCw), 1f - Mathf.Abs(u - headCw));
                float ringB = Mathf.Min(Mathf.Abs(u - headCcw), 1f - Mathf.Abs(u - headCcw));
                float comet = Mathf.Max(
                    Mathf.Clamp01(1f - ring * n / 3.4f),
                    Mathf.Clamp01(1f - ringB * n / 3.4f));
                float idle = 0.16f + 0.10f * (0.5f + 0.5f * Mathf.Sin(t * 2.2f + i * 0.7f));
                float lit = strobe ? (strobeOn ? 1f : idle) : Mathf.Max(idle, comet);
                float sz = Mathf.Lerp(szOff, szOn, lit);
                var r = new Rect(p.x - sz * 0.5f, p.y - sz * 0.5f, sz, sz);
                var prev = GUI.matrix;
                GUIUtility.RotateAroundPivot(ang, p);
                GUI.color = Color.Lerp(
                    new Color(1f, 0.48f, 0.10f, 0.18f),
                    new Color(1f, 0.92f, 0.38f, 1f),
                    lit);
                float halo = Mathf.Lerp(1.15f, 1.85f, lit);
                GUI.DrawTexture(new Rect(r.x - sz * (halo - 1f) * 0.5f, r.y - sz * (halo - 1f) * 0.5f, sz * halo, sz * halo), glow, ScaleMode.ScaleToFit, true);
                GUI.color = Color.Lerp(new Color(0.42f, 0.24f, 0.08f, 0.70f), Color.white, lit);
                if (tex != null) GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit, true);
                else GUI.DrawTexture(r, Texture2D.whiteTexture);
                GUI.matrix = prev;
            }
            GUI.color = Color.white;
        }

        void DrawGiftMovie(float s)
        {
            float w = Mathf.Min(Screen.width * 0.86f, 560f * s);
            float h = w * 0.56f;
            var stage = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.38f, w, h);
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
            var st = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.BoldAndItalic,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
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
            var st = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false
            };
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

        static void DrawGiftConfetti(float u, float s)
        {
            var pink = SpriteCatalog.PetalPink;
            var peach = SpriteCatalog.PetalPeach;
            var spark = SpriteCatalog.Sparkle;
            var glow = GlowTex();
            var pinkTex = pink != null ? pink.texture : null;
            var peachTex = peach != null ? peach.texture : null;
            var sparkTex = spark != null && spark.texture != null ? spark.texture : glow;
            const int n = 52;
            var origin = new Vector2(Screen.width * 0.5f, Screen.height * 0.40f);
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





