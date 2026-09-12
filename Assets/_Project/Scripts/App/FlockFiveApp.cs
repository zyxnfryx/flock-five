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
        // Splash streak peekaboo: <0 idle; 0..1 hide-peek-hide-pop-hold-tuck.
        float _streakSlide = -1f;
        int _streakAnnounced = -1;
        bool _streakChirped;
        float _pigBurst;
        float _pigJiggle;
        readonly HashSet<int> _locked = new HashSet<int>();
        int _combo;
        float _comboUntil = -99f;
        bool _collecting;
        float _nextTap;
        bool _finalePreview;
        bool _splash = true;
        bool _levelHive;
        enum HomeFace { Splash, Hive, Poker }
        HomeFace _home;
        enum PokerMotion { None, Deal, Draw, Shuffle }
        PokerMotion _pokerMotion;
        float _pokerMotionT = 99f;
        readonly bool[] _pokerRedraw = new bool[BirdPoker.HandSize];
        readonly BirdPoker.Card[] _pokerPrev = new BirdPoker.Card[BirdPoker.HandSize];
        bool _pokerStamp;
        float _pokerStampT;
        int _pokerStampKind = -1;
        bool _pokerPayOpen;
        float _pokerPayAnim;
        enum GiftFace { None, Card, Movie, Thanks }
        GiftFace _gift;
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
        bool _pokerPlayrun;
        Coroutine _sparrowRun;
        Coroutine _hawkRun;
        int _shotLevelNumber;
        string _shotEase;
        bool _recordSmash;
        int _recordFrameCount;
        // Home splash ambient flutters (1–2 birds; draw-only, no hit targets).
        struct SplashFlutter
        {
            public Vector2 From, To;
            public float T, Dur;
            public BirdColor Col;
            public bool FaceLeft;
        }
        SplashFlutter[] _splashFlutters;
        static Sprite _splashPointedV;

        void Start()
        {
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Sfx.Warm();
            try { ShowSplash(); }
            catch (System.Exception e) { Debug.LogException(e); }
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
            PigPoke.Boot();
            BirdPoker.Boot();
            ArmStreakSlide();
        }

        void Load(int index)
        {
            _busy = false;
            _won = false;
            _sel = -1;
            _combo = 0;
            _comboUntil = -99f;
            _collecting = false;
            _locked.Clear();
            _levelBees.Clear();
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
            _sparrowRun = StartCoroutine(SparrowView.Patrol(CanSparrowVisit, _garden.Feeders, _garden.Root));
            _hawkRun = StartCoroutine(HawkView.Patrol(CanHawkVisit, _garden.Feeders, _garden.Root));
            if (WantFinalePreview())
                StartCoroutine(PreviewFinale());
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
            System.IO.Directory.CreateDirectory("/tmp/paradice");
            Load(0);
            yield return new WaitForSecondsRealtime(1.15f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("/tmp/paradice/gift-branch.png");
            yield return new WaitForSecondsRealtime(0.35f);
            _gift = GiftFace.Card;
            yield return null;
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("/tmp/paradice/gift-dialog.png");
            yield return new WaitForSecondsRealtime(0.35f);
        }

        IEnumerator ShotHome()
        {
            System.IO.Directory.CreateDirectory("/tmp/paradice");
            int keep = PlayerPrefs.GetInt("flockfive.next", 0);
            PlayerPrefs.SetInt("flockfive.next", 0);
            PlayerPrefs.Save();
            yield return new WaitForSecondsRealtime(0.45f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("/tmp/paradice/splash-home.png");
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

        static void EncodeSmashVideo(string frames, string mp4, float fps)
        {
            string rate = fps.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "/opt/homebrew/bin/ffmpeg",
                Arguments = "-y -framerate " + rate + " -i \"" + frames + "/%04d.jpg\" -c:v libx264 -pix_fmt yuv420p -crf 23 -movflags +faststart \"" + mp4 + "\"",
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

                BirdPoker.ToggleHold(0);
                BirdPoker.ToggleHold(2);
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
            if (WantFinalePreview() && !_busy)
            {
                StartCoroutine(PreviewFinale());
                return;
            }
            if (!Pressed(out var screen)) return;
            if (HitHud(screen)) return;
            var cam = _garden.Cam != null ? _garden.Cam : Camera.main;
            if (cam == null) return;
            HandleTap(cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0f)));
        }

        public void ShotNow(string file) => Shot(file);

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
                if (_board.Branches[hit].Broken || _board.Branches[hit].Empty)
                {
                    _garden.Branches[hit].Shake();
                    return;
                }
                if (_board.IsSleeping(hit))
                {
                    _garden.Branches[hit].Shake();
                    Sfx.Sleep();
                    return;
                }
                if (TipLocked(hit))
                {
                    _garden.Branches[hit].Shake();
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
                if (_board.IsSleeping(hit))
                {
                    _garden.Branches[hit].Shake();
                    Sfx.Sleep();
                    return;
                }
                if (TipLocked(hit))
                {
                    _garden.Branches[hit].Shake();
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
            _board.TryMove(from, to, out run);
            yield return Hop(from, to, run, fromCount, toCount, hopCol);
            Unlock(from);
            Unlock(to);
            SyncAll();

            int kicked = KickCollects();
            SyncAll();
            if (_locked.Count == 0)
                yield return GardenFit.Tween(_garden, _board, false);
            if (kicked == 0 && _board.IsSleeping(to) && _board.Branches[to].IsFullMatch(out _))
                Sfx.Sleep();
            else if (kicked == 0 && _board.JustUnveiled)
            {
                // Record the visitor + fly-in, but do NOT auto-open the visitors sheet —
                // that was popping over play (and endgame) on every unveil. Tap hive to peek.
                var visit = Hive.TakeVisitor();
                _levelBees.Add(visit);
                if (_garden.Hive != null)
                {
                    SnapHiveToHud();
                    StartCoroutine(_garden.Hive.Welcome(visit, _garden.Branches[from].transform.position + Vector3.up * 0.7f));
                }
                _garden.Branches[from].FlutterTip();
            }
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
            _board.ApplyCollect(branch);

            float haste = Mathf.Lerp(1f, 0.52f, Mathf.Clamp01((combo - 1) / 7f));
            float step = 0.192f * haste;
            float fly = 0.432f * haste;
            bool vsHawk = HawkView.Live != null && HawkView.Live.BlockingSlot == slot;
            bool vsSparrow = !vsHawk && SparrowView.Live != null && SparrowView.Live.BlockingSlot == slot;
            if (vsHawk)
                yield return CollectVsHawk(birds, n, feeder, mouth, view, col, haste, step, fly, combo, flock, branch);
            else if (vsSparrow)
                yield return CollectVsSparrow(birds, n, feeder, mouth, view, col, haste, step, fly, combo, flock, branch);
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

            if (feeder != null) yield return feeder.PullAway();
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

            if (cleared)
            {
                if (feeder != null) yield return feeder.PullAway();
            }
            else if (feeder != null)
            {
                // Keep feeder planted so wounded hawk stays blocking for next collect.
                feeder.SnapHome();
            }
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
            yield return new WaitForSeconds(0.40f + n * 0.03f);
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
            float dur = Random.Range(0.28f, 0.40f);
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
            float wait = 0.48f;
            for (int i = 0; i < n; i++)
            {
                if (birds[i] == null) continue;
                float delay = i * 0.035f;
                wait = Mathf.Max(wait, delay + 0.52f);
                if (parked[i])
                    StartCoroutine(PerchOne(birds[i].transform, dests[i], delay));
                else
                {
                    var off = ScatterDests(1);
                    StartCoroutine(ScatterOne(birds[i].transform, off[0], delay));
                }
            }
            yield return new WaitForSeconds(wait);
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
            float dur = 0.48f;
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
            if (GardenSolve.Look(_board) != GardenSolve.Outlook.Tangled) return;
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
                    for (int s = 0; s < BranchState.Cap; s++)
                    {
                        if (v.Birds[s] == null || !v.Birds[s].enabled) continue;
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
                if (st.Broken) continue;
                float reach = st.Empty && sending ? 3.45f : pad;
                float d = v.NearestPadSqr(world);
                if (d < reach * reach && d < best) { best = d; idx = v.Index; }
            }
            var overlap = Physics2D.OverlapCircleAll(world, sending ? 2.6f : 1.85f);
            for (int i = 0; i < overlap.Length; i++)
            {
                var v = overlap[i].GetComponentInParent<BranchView>();
                if (v == null || _board.Branches[v.Index].Broken) continue;
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
            float hiveW = 170f * scale;
            hive = new Rect(Mathf.Min(Screen.width - hiveW - 16f, safe.xMax - hiveW - 8f), y, hiveW, h);
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
            if (!_pokerPlayrun && System.IO.File.Exists("/tmp/flock-five-poker-run"))
            {
                try { System.IO.File.Delete("/tmp/flock-five-poker-run"); } catch { }
                _pokerPlayrun = true;
                StartCoroutine(PokerPlayrun());
            }
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
            var hiveSpr = SpriteCatalog.Hive;
            if (hiveSpr != null && hiveSpr.texture != null)
                GUI.DrawTexture(hive, hiveSpr.texture, ScaleMode.ScaleToFit, true);
            else
                GUI.Box(hive, "Hive");
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
            // Big peekaboo only after a stage clear (coins incoming).
            if (Purse.Pending <= 0)
            {
                _streakSlide = -1f;
                return;
            }
            _streakSlide = 0f;
            _streakChirped = false;
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
            string coins = "$" + Purse.Coins;
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
            _streakSlide = Mathf.Min(1f, _streakSlide + Time.unscaledDeltaTime / dur);
            float u = _streakSlide;
            float reveal;
            float scale = 1f;
            if (u < 0.10f)
                reveal = 0f;
            else if (u < 0.26f)
                reveal = Mathf.SmoothStep(0f, 0.42f, (u - 0.10f) / 0.16f);
            else if (u < 0.36f)
                reveal = Mathf.Lerp(0.42f, 0.06f, Mathf.SmoothStep(0f, 1f, (u - 0.26f) / 0.10f));
            else if (u < 0.54f)
            {
                float t = Mathf.SmoothStep(0f, 1f, (u - 0.36f) / 0.18f);
                reveal = Mathf.Lerp(0.06f, 1.10f, t);
                scale = Mathf.Lerp(0.88f, 1.16f, t);
            }
            else if (u < 0.62f)
            {
                float t = (u - 0.54f) / 0.08f;
                reveal = Mathf.Lerp(1.10f, 1f, t);
                scale = Mathf.Lerp(1.16f, 1f, t);
            }
            else if (u < 0.82f)
            {
                reveal = 1f;
                scale = 1f + 0.04f * Mathf.Sin(Time.unscaledTime * 3.2f);
            }
            else
            {
                reveal = 1f - Mathf.SmoothStep(0f, 1f, (u - 0.82f) / 0.18f);
                if (u >= 1f) _streakSlide = -1f;
            }

            if (!_streakChirped && u >= 0.54f)
            {
                _streakChirped = true;
                Sfx.Chirp(BirdColor.Gold);
            }

            string streak = "STREAK  ×" + Purse.Streak;
            float h = Mathf.Max(pig.height * 0.48f, 44f * s);
            float w = Mathf.Max(188f * s, pig.x - 20f);
            float restX = pig.x - 10f - w;
            float hideX = pig.x - w * 0.18f;
            float x = Mathf.Lerp(hideX, restX, Mathf.Clamp01(reveal));
            float y = pig.y - h - 8f * s;
            var streakR = new Rect(x, y, w, h);
            var c = streakR.center;
            streakR.width *= scale;
            streakR.height *= scale;
            streakR.x = c.x - streakR.width * 0.5f;
            streakR.y = c.y - streakR.height * 0.5f;

            float alpha = Mathf.Clamp01(0.2f + reveal * 0.85f);
            var glow = GlowTex();
            var prev = GUI.color;
            GUI.color = new Color(1f, 0.86f, 0.42f, 0.22f * alpha);
            float pad = 10f * s;
            GUI.DrawTexture(new Rect(streakR.x - pad, streakR.y - pad, streakR.width + pad * 2f, streakR.height + pad * 2f), glow, ScaleMode.ScaleToFit, true);
            GUI.color = new Color(0.16f, 0.10f, 0.04f, 0.38f * alpha);
            GUI.DrawTexture(new Rect(streakR.x + 3f, streakR.y + 5f, streakR.width, streakR.height), Texture2D.whiteTexture);
            GUI.color = new Color(0.92f, 0.72f, 0.32f, 0.92f * alpha);
            GUI.DrawTexture(streakR, Texture2D.whiteTexture);
            GUI.color = new Color(0.99f, 0.93f, 0.70f, 0.96f * alpha);
            GUI.DrawTexture(new Rect(streakR.x + 4f * s, streakR.y + 4f * s, streakR.width - 8f * s, streakR.height - 8f * s), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 1f, 1f, alpha);
            var st = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false
            };
            st.fontSize = FitFont(st, streak, streakR.width * 0.90f, streakR.height * 0.78f, 22, 44);
            StampOutlined(streakR, streak, st, new Color(0.36f, 0.18f, 0.07f), 2, 1);
            GUI.color = prev;
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
                    A = new Vector2(Screen.width * 0.5f + Random.Range(-110f, 110f), Screen.height * 0.38f + Random.Range(-50f, 70f)),
                    B = dest,
                    T = 0f,
                    Delay = 2.85f + i * 0.26f,
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
            DrawSplashTitleMark(s);
            DrawAmbientSplashBirds(s);
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
            DrawRailIcon(hiveR, SpriteCatalog.Hive);

            // Third rail button: bird video poker.
            var pokerR = SplashPokerRect();
            if (HitPad(pokerR, out _))
            {
                BirdPoker.Boot();
                BirdPoker.ResetRound();
                _home = HomeFace.Poker;
            }
            DrawSplashPokerButton(pokerR);

            string ease = _shotEase ?? LevelData.JokeEase(next);
            int number = _shotLevelNumber > 0 ? _shotLevelNumber : (peek != null ? peek.Number : next + 1);
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
            Paint(st, new Color(0.02f, 0.02f, 0.02f, 1f));
            Ring(r, text, st, whitePx + blackPx);
            Paint(st, Color.white);
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

        void DrawSplashWord(string word, float y, float maxW, float capH, float tracking, float s)
        {
            int n = word.Length;
            var sprs = new Sprite[n];
            var widths = new float[n];
            float total = 0f;
            bool any = false;
            for (int i = 0; i < n; i++)
            {
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

        void EnsureSplashFlutters()
        {
            if (_splashFlutters != null && _splashFlutters.Length == 2) return;
            _splashFlutters = new SplashFlutter[2];
            for (int i = 0; i < 2; i++)
                RetargetSplashFlutter(i, true);
        }

        // Wander points in a soft halo around the stacked title mark (not whole homepage).
        static Rect SplashTitleHalo()
        {
            float top = Mathf.Max(10f, Screen.height - Screen.safeArea.yMax + 2f);
            float capH = 56f * Mathf.Max(Screen.height / 720f, 1f);
            float rowGap = 4f * Mathf.Max(Screen.height / 720f, 1f);
            float titleH = capH * 2f + rowGap;
            float padX = Screen.width * 0.14f;
            float padY = 22f * Mathf.Max(Screen.height / 720f, 1f);
            return new Rect(padX, Mathf.Max(4f, top - padY * 0.35f), Screen.width - padX * 2f, titleH + padY);
        }

        static Vector2 RandomInTitleHalo()
        {
            var h = SplashTitleHalo();
            // Prefer edges/corners of the halo so birds orbit the wordmark, not sit on glyphs.
            float u = Random.value;
            float x, y;
            if (u < 0.35f) // left band
            {
                x = Mathf.Lerp(h.xMin, h.xMin + h.width * 0.22f, Random.value);
                y = Mathf.Lerp(h.yMin, h.yMax, Random.value);
            }
            else if (u < 0.70f) // right band
            {
                x = Mathf.Lerp(h.xMax - h.width * 0.22f, h.xMax, Random.value);
                y = Mathf.Lerp(h.yMin, h.yMax, Random.value);
            }
            else // top/bottom band
            {
                x = Mathf.Lerp(h.xMin, h.xMax, Random.value);
                y = Random.value < 0.5f
                    ? Mathf.Lerp(h.yMin, h.yMin + h.height * 0.28f, Random.value)
                    : Mathf.Lerp(h.yMax - h.height * 0.28f, h.yMax, Random.value);
            }
            return new Vector2(x, y);
        }

        void RetargetSplashFlutter(int i, bool spawn)
        {
            Vector2 a = spawn ? RandomInTitleHalo() : _splashFlutters[i].To;
            Vector2 b;
            int guard = 0;
            do
            {
                b = RandomInTitleHalo();
                guard++;
            } while (Vector2.Distance(a, b) < Screen.width * 0.10f && guard < 10);

            var cols = new[] { BirdColor.Ruby, BirdColor.Gold, BirdColor.Teal, BirdColor.Violet };
            _splashFlutters[i] = new SplashFlutter
            {
                From = a,
                To = b,
                T = 0f,
                Dur = Random.Range(2.2f, 3.8f),
                Col = cols[(i + Random.Range(0, cols.Length)) % cols.Length],
                FaceLeft = b.x < a.x
            };
        }

        void DrawAmbientSplashBirds(float s)
        {
            EnsureSplashFlutters();
            float icon = 36f * s; // subtle companions around the logo
            var prev = GUI.color;
            for (int i = 0; i < _splashFlutters.Length; i++)
            {
                var f = _splashFlutters[i];
                f.T += Time.unscaledDeltaTime / Mathf.Max(0.2f, f.Dur);
                if (f.T >= 1f)
                {
                    RetargetSplashFlutter(i, false);
                    f = _splashFlutters[i];
                }
                float u = f.T * f.T * (3f - 2f * f.T); // smoothstep
                Vector2 p = Vector2.Lerp(f.From, f.To, u);
                p.y += Mathf.Sin(u * Mathf.PI) * (-18f * s);
                var spr = SpriteCatalog.BirdFrame(f.Col, Time.unscaledTime * 9f + i * 1.7f, true);
                if (spr == null || spr.texture == null)
                {
                    _splashFlutters[i] = f;
                    continue;
                }
                var r = new Rect(p.x - icon * 0.5f, p.y - icon * 0.5f, icon, icon);
                GUI.color = new Color(1f, 1f, 1f, 0.90f);
                if (f.FaceLeft)
                {
                    var m = GUI.matrix;
                    GUIUtility.ScaleAroundPivot(new Vector2(-1f, 1f), r.center);
                    GUI.DrawTexture(r, spr.texture, ScaleMode.ScaleToFit, true);
                    GUI.matrix = m;
                }
                else
                    GUI.DrawTexture(r, spr.texture, ScaleMode.ScaleToFit, true);
                _splashFlutters[i] = f;
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
                _pokerStamp = false;
                _pokerPayOpen = false;
                _pokerPayAnim = 0f;
                _home = HomeFace.Splash;
            }
            GUI.color = new Color(0.10f, 0.08f, 0.05f, backHeld ? 0.88f : 0.72f);
            GUI.DrawTexture(back, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(back, "Back", backLab);

            var title = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(30 * s),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            title.normal.textColor = new Color(1f, 0.94f, 0.72f);
            GUI.Label(new Rect(20f, top, Screen.width - 40f, 40f * s), "BIRD POKER", title);
            DrawPokerPurse(top, s);
            // Hit-test pay-table tab / dismiss early so overlay blocks cards & Deal.
            if (!_pokerStamp)
                TickPokerPayTable(top, s);

            float cardW = Mathf.Min(Screen.width * 0.176f, 118f * s);
            float cardH = cardW * 1.42f;
            float gap = 8f * s;
            float holdH = 32f * s;
            float rowW = BirdPoker.HandSize * cardW + (BirdPoker.HandSize - 1) * gap;
            float rowX = (Screen.width - rowW) * 0.5f;
            float btnSize = Mathf.Min(Screen.width * 0.40f, 168f * s);
            float botPad = Mathf.Max(12f, safe.yMin + 6f);
            float btnY = Screen.height - botPad - btnSize;
            float head = top + 52f * s;
            float floor = btnY - holdH - 16f * s;
            float rowY = head + Mathf.Max(0f, floor - head - cardH) * 0.62f;
            TickPokerMotion();
            for (int i = 0; i < BirdPoker.HandSize; i++)
            {
                var seat = new Rect(rowX + i * (cardW + gap), rowY, cardW, cardH);
                DrawPokerCard(seat, i, s, holdH);
            }

            var betR = new Rect(Screen.width * 0.5f - btnSize - 8f * s, btnY, btnSize, btnSize);
            var actR = new Rect(Screen.width * 0.5f + 8f * s, btnY, btnSize, btnSize);
            var dealR = new Rect((Screen.width - btnSize) * 0.5f, btnY, btnSize, btnSize);

            bool payBlocked = _pokerPayOpen || _pokerPayAnim > 0.35f;
            bool busy = PokerMotionBusy() || _pokerStamp || payBlocked;
            TickPokerStamp();
            if (BirdPoker.PhaseNow == BirdPoker.Phase.Idle)
            {
                if (!busy && DrawFloralBtn(betR, "BET $" + BirdPoker.Bet, s))
                    BirdPoker.CycleBet();
                if (!busy && DrawFloralBtn(actR, "DEAL", s))
                    TryPokerDeal();
            }
            else if (BirdPoker.PhaseNow == BirdPoker.Phase.Dealt)
            {
                if (!busy && DrawFloralBtn(dealR, "DRAW", s))
                {
                    for (int i = 0; i < BirdPoker.HandSize; i++)
                    {
                        _pokerRedraw[i] = !BirdPoker.Hold[i];
                        _pokerPrev[i] = BirdPoker.Hand[i];
                    }
                    BirdPoker.Draw();
                    BeginPokerDraw();
                    if (BirdPoker.LastPunchFresh)
                    {
                        BeginPokerStamp(BirdPoker.LastPunchKind);
                        Sfx.Combo(3);
                    }
                    else if (BirdPoker.LastWin > 0) Sfx.Clink();
                    else Sfx.Deny();
                }
            }
            else if (!busy && DrawFloralBtn(dealR, "DEAL", s))
            {
                BirdPoker.Collect();
                TryPokerDeal();
            }

            // Stamp ceremony above everything; else pay-table overlay / jewel tab on top.
            if (_pokerStamp) DrawPokerStampCeremony(s);
            else DrawPokerPayTable(top, s);
        }


        Rect PokerPayTabRect(float top, float s)
        {
            float tabW = Mathf.Clamp(132f * s, 112f, 168f);
            float tabH = Mathf.Clamp(38f * s, 34f, 48f);
            float right = Screen.width - Mathf.Max(6f, Screen.width - Screen.safeArea.xMax + 4f);
            float tabY = top + 46f * s;
            return new Rect(right - tabW, tabY, tabW, tabH);
        }

        void TickPokerPayTable(float top, float s)
        {
            float target = _pokerPayOpen ? 1f : 0f;
            _pokerPayAnim = Mathf.MoveTowards(_pokerPayAnim, target, Time.unscaledDeltaTime * 7f);

            var tab = PokerPayTabRect(top, s);
            if (HitPad(tab, out _))
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
                float panelW = Mathf.Min(Screen.width * 0.92f, 420f * s);
                float rowH = Mathf.Clamp(36f * s, 30f, 44f);
                int rows = BirdPoker.PayTableRows.Length;
                float headH = 36f * s;
                float pad = 14f * s;
                float panelH = headH + rows * rowH + pad * 2f;
                float cx = Screen.width * 0.5f;
                float panelTop = tab.yMax + 10f * s;
                float ease = 1f - Mathf.Pow(1f - u, 2.4f);
                float y = Mathf.Lerp(panelTop - 24f * s, panelTop, ease);
                var panel = new Rect(cx - panelW * 0.5f, y, panelW, panelH * Mathf.Lerp(0.92f, 1f, ease));

                GUI.color = new Color(0.10f, 0.07f, 0.04f, 0.55f * u);
                GUI.DrawTexture(new Rect(panel.x + 4f, panel.y + 6f, panel.width, panel.height), Texture2D.whiteTexture);
                GUI.color = new Color(0.42f, 0.28f, 0.12f, 0.98f * u);
                GUI.DrawTexture(panel, Texture2D.whiteTexture);
                GUI.color = new Color(0.72f, 0.52f, 0.22f, 0.95f * u);
                GUI.DrawTexture(new Rect(panel.x + 3f * s, panel.y + 3f * s, panel.width - 6f * s, panel.height - 6f * s), Texture2D.whiteTexture);
                var paper = new Rect(panel.x + 7f * s, panel.y + 7f * s, panel.width - 14f * s, panel.height - 14f * s);
                GUI.color = new Color(0.96f, 0.93f, 0.82f, 0.98f * u);
                GUI.DrawTexture(paper, Texture2D.whiteTexture);
                GUI.color = Color.white;

                var head = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                string headTx = "PAY TABLE  ·  BET $" + BirdPoker.Bet;
                head.fontSize = FitFont(head, headTx, paper.width * 0.92f, headH * 0.85f, 13, 22);
                StampOutlined(new Rect(paper.x, paper.y + 2f * s, paper.width, headH), headTx, head, new Color(0.28f, 0.14f, 0.06f), 2, 1);

                var nameSt = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = false
                };
                var paySt = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleRight,
                    wordWrap = false
                };
                float yRow = paper.y + headH + 2f * s;
                for (int i = 0; i < rows; i++)
                {
                    var rank = BirdPoker.PayTableRows[i];
                    int mult = BirdPoker.Multiplier(rank);
                    int dollars = mult * BirdPoker.Bet;
                    string name = BirdPoker.RankLabel(rank);
                    string pay = mult + "×   $" + dollars;
                    var row = new Rect(paper.x + 8f * s, yRow, paper.width - 16f * s, rowH);

                    bool jackpot = rank == BirdPoker.Rank.NaturalFive;
                    if (jackpot)
                    {
                        GUI.color = new Color(1f, 0.88f, 0.45f, 0.42f * u);
                        GUI.DrawTexture(row, Texture2D.whiteTexture);
                    }
                    else if ((i & 1) == 1)
                    {
                        GUI.color = new Color(0.78f, 0.68f, 0.48f, 0.22f * u);
                        GUI.DrawTexture(row, Texture2D.whiteTexture);
                    }
                    GUI.color = Color.white;

                    float nameH = jackpot ? rowH * 0.82f : rowH * 0.72f;
                    int lo = jackpot ? 15 : 13;
                    int hi = jackpot ? 26 : 22;
                    nameSt.fontSize = FitFont(nameSt, name, row.width * 0.58f, nameH, lo, hi);
                    paySt.fontSize = FitFont(paySt, pay, row.width * 0.38f, nameH, lo, hi);
                    var fill = jackpot
                        ? new Color(0.55f, 0.28f, 0.06f)
                        : new Color(0.32f, 0.16f, 0.06f);
                    StampOutlined(new Rect(row.x, row.y, row.width * 0.58f, row.height), name, nameSt, fill, 2, 1);
                    StampOutlined(new Rect(row.x + row.width * 0.55f, row.y, row.width * 0.45f, row.height), pay, paySt, fill, 2, 1);
                    yRow += rowH;
                }
            }

            DrawPokerPayJewel(tab, s);
        }

        void DrawPokerPayJewel(Rect tab, float s)
        {
            bool held = false;
            var e = Event.current;
            if (e != null && tab.Contains(e.mousePosition) && Input.GetMouseButton(0))
                held = true;

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
            labSt.fontSize = FitFont(labSt, lab, chip.width * 0.90f, chip.height * 0.70f, 11, 16);
            StampOutlined(chip, lab, labSt, new Color(1f, 0.94f, 0.72f), 1, 1);
        }

        void BeginPokerStamp(int kind)
        {
            _pokerPayOpen = false;
            _pokerPayAnim = 0f;
            _pokerStamp = true;
            _pokerStampT = 0f;
            _pokerStampKind = kind;
            if (CamShake.Live != null) CamShake.Live.Punch(0.22f, 0.12f, 2.4f, 0.10f);
            Sfx.Rumble();
        }

        void TickPokerStamp()
        {
            if (!_pokerStamp) return;
            _pokerStampT += Time.unscaledDeltaTime;
            // Stamp impact rumble
            if (_pokerStampT >= 1.05f && _pokerStampT - Time.unscaledDeltaTime < 1.05f)
            {
                if (CamShake.Live != null) CamShake.Live.Punch(0.34f, 0.16f, 2.8f, 0.12f);
                Sfx.Rumble();
                Sfx.Crack();
            }
            bool tap = Event.current != null && Event.current.type == EventType.MouseDown;
            if (_pokerStampT >= 2.6f || (_pokerStampT >= 1.35f && tap))
                _pokerStamp = false;
        }

        void DrawPokerStampCeremony(float s)
        {
            float t = _pokerStampT;
            // Dim table
            float veil = Mathf.Clamp01(t / 0.12f) * 0.72f;
            GUI.color = new Color(0.04f, 0.03f, 0.02f, veil);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Clipboard slam onto the table
            float boardW = Mathf.Min(Screen.width * 0.88f, 520f * s);
            float boardH = boardW * 1.22f;
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

            // Clipboard body (wood) + paper
            GUI.color = new Color(0.42f, 0.28f, 0.12f, 0.98f);
            GUI.DrawTexture(board, Texture2D.whiteTexture);
            var clip = new Rect(board.center.x - boardW * 0.12f, board.y - 18f * s, boardW * 0.24f, 36f * s);
            GUI.color = new Color(0.55f, 0.55f, 0.58f, 1f);
            GUI.DrawTexture(clip, Texture2D.whiteTexture);
            GUI.color = new Color(0.96f, 0.93f, 0.82f, 1f);
            var paper = new Rect(board.x + boardW * 0.07f, board.y + boardH * 0.08f, boardW * 0.86f, boardH * 0.84f);
            GUI.DrawTexture(paper, Texture2D.whiteTexture);
            GUI.color = Color.white;

            var title = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            title.fontSize = FitFont(title, "FLOCK FIVE  PUNCH CARD", paper.width * 0.92f, 28f * s, 14, 26);
            StampOutlined(new Rect(paper.x, paper.y + 8f * s, paper.width, 28f * s), "FLOCK FIVE  PUNCH CARD", title, new Color(0.28f, 0.14f, 0.06f), 2, 1);

            // Bird grid on the paper
            int cols = 5;
            float pad = 10f * s;
            float gridTop = paper.y + 42f * s;
            float gridH = paper.height - 70f * s;
            float cell = Mathf.Min((paper.width - pad * 2f - (cols - 1) * 6f * s) / cols, gridH / 3f - 6f * s);
            float gap = 6f * s;
            float gridW = cols * cell + (cols - 1) * gap;
            float x0 = paper.center.x - gridW * 0.5f;
            for (int i = 0; i < BirdPoker.PunchKinds; i++)
            {
                int col = i % cols;
                int row = i / cols;
                var r = new Rect(x0 + col * (cell + gap), gridTop + row * (cell + gap), cell, cell);
                bool on = BirdPoker.IsPunched(i);
                bool focus = i == _pokerStampKind;
                GUI.color = focus
                    ? new Color(1f, 0.92f, 0.55f, 0.95f)
                    : (on ? new Color(0.88f, 0.84f, 0.72f, 0.95f) : new Color(0.82f, 0.80f, 0.74f, 0.85f));
                GUI.DrawTexture(r, Texture2D.whiteTexture);
                GUI.color = Color.white;
                BirdColor c;
                BirdSex sex;
                BirdPoker.KindParts(i, out c, out sex);
                var spr = SpriteCatalog.Bird(c, sex);
                if (spr != null && spr.texture != null)
                {
                    float ip = cell * 0.1f;
                    GUI.color = on || focus ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                    GUI.DrawTexture(new Rect(r.x + ip, r.y + ip, cell - ip * 2f, cell - ip * 2f), spr.texture, ScaleMode.ScaleToFit, true);
                    GUI.color = Color.white;
                }
            }

            // Red COMPLETED stamp — drops after clipboard lands
            if (t >= 0.55f && _pokerStampKind >= 0)
            {
                int col = _pokerStampKind % cols;
                int row = _pokerStampKind / cols;
                var cellR = new Rect(x0 + col * (cell + gap), gridTop + row * (cell + gap), cell, cell);
                float stampU = Mathf.Clamp01((t - 0.55f) / 0.50f);
                float drop = 1f - Mathf.Pow(1f - stampU, 2.4f);
                float stampSize = cell * 1.55f;
                float stampY = Mathf.Lerp(cellR.y - Screen.height * 0.35f, cellR.center.y - stampSize * 0.5f, drop);
                float rot = Mathf.Lerp(-18f, -8f, drop);
                float pop = stampU >= 1f ? 1f + 0.12f * Mathf.Sin(Mathf.Clamp01((t - 1.05f) / 0.18f) * Mathf.PI) : 1f;
                stampSize *= pop;
                var stamp = new Rect(cellR.center.x - stampSize * 0.5f, stampY, stampSize, stampSize);

                // Draw rotated stamp via GUI matrix
                var prev = GUI.matrix;
                Vector2 pivot = stamp.center;
                GUIUtility.RotateAroundPivot(rot, pivot);
                // Outer red ring (cutthrough circle)
                GUI.color = new Color(0.82f, 0.08f, 0.10f, 0.92f * Mathf.Clamp01(stampU * 1.4f));
                GUI.DrawTexture(stamp, Texture2D.whiteTexture);
                // Hollow center — draw paper-colored disc
                float inset = stampSize * 0.14f;
                GUI.color = new Color(0.96f, 0.93f, 0.82f, 0.92f * Mathf.Clamp01(stampU * 1.4f));
                GUI.DrawTexture(new Rect(stamp.x + inset, stamp.y + inset, stampSize - inset * 2f, stampSize - inset * 2f), Texture2D.whiteTexture);
                // Inner red ring edge
                float inset2 = stampSize * 0.20f;
                GUI.color = new Color(0.82f, 0.08f, 0.10f, 0.88f * Mathf.Clamp01(stampU * 1.4f));
                // Approximate ring with thick border via four strips is heavy — use text as the cutthrough brand
                GUI.color = new Color(0.78f, 0.06f, 0.08f, 0.95f * Mathf.Clamp01(stampU * 1.4f));
                var st = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };
                st.fontSize = FitFont(st, "COMPLETED", stamp.width * 0.82f, stamp.height * 0.45f, 11, 28);
                // Red lettering cutthrough look: stamp label over the hollow
                GUI.Label(new Rect(stamp.x + stamp.width * 0.08f, stamp.y + stamp.height * 0.28f, stamp.width * 0.84f, stamp.height * 0.44f), "COMPLETED", st);
                // Force red via StampOutlined
                StampOutlined(new Rect(stamp.x + stamp.width * 0.08f, stamp.y + stamp.height * 0.28f, stamp.width * 0.84f, stamp.height * 0.44f), "COMPLETED", st, new Color(0.78f, 0.06f, 0.08f, 1f), 2, 1);
                GUI.matrix = prev;
                GUI.color = Color.white;
            }

            if (t >= 1.4f)
            {
                var hint = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Italic,
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = Mathf.RoundToInt(14 * s)
                };
                StampOutlined(new Rect(0f, board.yMax + 12f * s, Screen.width, 24f * s), "tap to continue", hint, new Color(1f, 0.92f, 0.7f), 1, 1);
            }
        }

        void DrawPokerPurse(float top, float s)
        {
            string coins = "$" + Purse.Coins;
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

        void TryPokerDeal()
        {
            if (!BirdPoker.Deal())
                Sfx.Deny();
            else
            {
                BeginPokerDeal();
                Sfx.Chirp(BirdColor.Gold);
            }
        }

        bool PokerMotionBusy() =>
            _pokerMotion != PokerMotion.None && _pokerMotionT < PokerMotionDur();

        float PokerMotionDur()
        {
            if (_pokerMotion == PokerMotion.Deal) return 0.08f * (BirdPoker.HandSize - 1) + 0.34f;
            if (_pokerMotion == PokerMotion.Draw) return 0.08f * (BirdPoker.HandSize - 1) + 0.40f;
            return 0f;
        }

        void TickPokerMotion()
        {
            if (_pokerMotion == PokerMotion.None) return;
            _pokerMotionT += Time.unscaledDeltaTime;
            if (_pokerMotionT >= PokerMotionDur())
                _pokerMotion = PokerMotion.None;
        }

        void BeginPokerDeal()
        {
            _pokerMotion = PokerMotion.Deal;
            _pokerMotionT = 0f;
            for (int i = 0; i < _pokerRedraw.Length; i++) _pokerRedraw[i] = true;
        }

        void BeginPokerDraw()
        {
            _pokerMotion = PokerMotion.Draw;
            _pokerMotionT = 0f;
        }

        void DrawPokerCard(Rect seat, int i, float s, float holdH)
        {
            bool dealt = BirdPoker.PhaseNow == BirdPoker.Phase.Dealt;
            bool canHold = !PokerMotionBusy() && !(_pokerPayOpen || _pokerPayAnim > 0.35f) && dealt;
            var holdR = new Rect(seat.x, seat.yMax + 6f * s, seat.width, holdH);
            if (canHold && (HitPad(seat, out _) || HitPad(holdR, out _)))
            {
                BirdPoker.ToggleHold(i);
                Sfx.Chirp(BirdColor.Gold);
            }

            bool showFace;
            float yaw;
            var face = BirdPoker.Hand[i];
            PokerFlip(i, out showFace, out yaw, ref face);

            float sx = Mathf.Cos(yaw * Mathf.Deg2Rad);
            if (Mathf.Abs(sx) < 0.07f)
                sx = 0.07f * Mathf.Sign(sx == 0f ? 1f : sx);
            var prevM = GUI.matrix;
            GUIUtility.ScaleAroundPivot(new Vector2(sx, 1f), seat.center);
            if (showFace) DrawPokerFace(seat, face, s);
            else DrawPokerBack(seat);
            GUI.matrix = prevM;

            bool held = dealt && BirdPoker.Hold[i] && !PokerMotionBusy();
            GUI.color = new Color(0.12f, 0.08f, 0.04f, 0.35f);
            GUI.DrawTexture(new Rect(holdR.x + 2f, holdR.y + 3f, holdR.width, holdR.height), Texture2D.whiteTexture);
            GUI.color = held ? new Color(0.86f, 0.62f, 0.28f, 0.96f) : new Color(0.78f, 0.58f, 0.32f, 0.55f);
            GUI.DrawTexture(holdR, Texture2D.whiteTexture);
            GUI.color = held ? new Color(0.98f, 0.90f, 0.62f, 1f) : new Color(0.94f, 0.84f, 0.58f, 0.72f);
            GUI.DrawTexture(new Rect(holdR.x + 3f * s, holdR.y + 3f * s, holdR.width - 6f * s, holdR.height - 6f * s), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var holdSt = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            holdSt.fontSize = Mathf.RoundToInt(13 * s);
            StampOutlined(holdR, "HOLD", holdSt, new Color(0.36f, 0.18f, 0.07f, held ? 1f : 0.45f), 1, 1);
        }

        void PokerFlip(int i, out bool showFace, out float yaw, ref BirdPoker.Card face)
        {
            yaw = 0f;
            showFace = BirdPoker.PhaseNow != BirdPoker.Phase.Idle;
            if (_pokerMotion == PokerMotion.Deal)
            {
                float u = Mathf.Clamp01((_pokerMotionT - i * 0.08f) / 0.32f);
                yaw = u * 180f;
                showFace = u >= 0.5f;
                face = BirdPoker.Hand[i];
            }
            else if (_pokerMotion == PokerMotion.Draw && _pokerRedraw[i])
            {
                float u = Mathf.Clamp01((_pokerMotionT - i * 0.08f) / 0.38f);
                yaw = u * 180f;
                showFace = u >= 0.5f;
                face = u < 0.5f ? _pokerPrev[i] : BirdPoker.Hand[i];
            }
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

        static void DrawPokerFace(Rect r, BirdPoker.Card card, float s)
        {
            GUI.color = new Color(0.22f, 0.12f, 0.08f, 1f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            var inner = new Rect(r.x + 2f * s, r.y + 2f * s, r.width - 4f * s, r.height - 4f * s);
            GUI.color = new Color(0.99f, 0.96f, 0.90f, 1f);
            GUI.DrawTexture(inner, Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (card.Wild)
            {
                var st = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                st.fontSize = FitFont(st, "WILD", inner.width * 0.88f, inner.height * 0.32f, 14, 32);
                StampOutlined(inner, "WILD", st, new Color(0.95f, 0.75f, 0.2f), 2, 1);
                return;
            }
            var spr = SpriteCatalog.Bird(card.Color, card.Sex);
            if (spr != null && spr.texture != null)
            {
                float pad = inner.width * 0.03f;
                GUI.DrawTexture(new Rect(inner.x + pad, inner.y + pad, inner.width - pad * 2f, inner.height - pad * 2f), spr.texture, ScaleMode.ScaleToFit, true);
            }
            var pip = PokerPip(card.Color);
            float pr = r.width * 0.16f;
            GUI.color = new Color(0.99f, 0.96f, 0.90f, 0.92f);
            GUI.DrawTexture(new Rect(inner.x + 3f * s, inner.y + 3f * s, pr + 4f * s, pr + 4f * s), Texture2D.whiteTexture);
            GUI.color = pip;
            GUI.DrawTexture(new Rect(inner.x + 5f * s, inner.y + 5f * s, pr, pr), Texture2D.whiteTexture);
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

        bool DrawFloralBtn(Rect r, string label, float s)
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
            st.fontSize = FitFont(st, label, disc.width * 0.84f, disc.height * 0.62f, 14, 28);
            StampOutlined(disc, label, st, new Color(0.36f, 0.18f, 0.07f), 2, 1);
            return fire;
        }

        void DrawHivePage()
        {
            float s = Mathf.Max(Screen.height / 720f, 1f);
            DrawHomeWash(0.28f);

            var title = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(28 * s),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            title.normal.textColor = new Color(1f, 0.94f, 0.72f);
            var row = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(20 * s) };
            var backLab = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(22 * s),
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            backLab.normal.textColor = new Color(1f, 0.94f, 0.72f);

            var safe = Screen.safeArea;
            float top = Mathf.Max(20f, Screen.height - safe.yMax + 10f);
            var back = new Rect(Mathf.Max(16f, safe.xMin + 10f), top, 132f * Mathf.Min(s, 1.6f), 44f * Mathf.Min(s, 1.6f));
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

            float hiveSize = Mathf.Clamp(Screen.width * 0.20f, 80f, 140f);
            var hiveSpr = SpriteCatalog.Hive;
            if (hiveSpr != null && hiveSpr.texture != null)
                GUI.DrawTexture(new Rect((Screen.width - hiveSize) * 0.5f, top + 8f, hiveSize, hiveSize), hiveSpr.texture, ScaleMode.ScaleToFit, true);

            float boxY = top + hiveSize + 4f * s;
            var head = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false
            };
            int pages = Mathf.Max(1, (Hive.AlbumSlots + HivePageSize - 1) / HivePageSize);
            _hivePage = Mathf.Clamp(_hivePage, 0, pages - 1);
            string album = "Bee Album  " + Hive.Found + " / " + Hive.AlbumSlots;
            head.fontSize = FitFont(head, album, Screen.width * 0.72f, 32f * s, 16, 28);
            StampOutlined(new Rect(0f, boxY, Screen.width, 30f * s), album, head, new Color(1f, 0.94f, 0.72f), 2, 1);
            var tip = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleCenter
            };
            tip.fontSize = Mathf.RoundToInt(13 * s);
            StampOutlined(new Rect(0f, boxY + 26f * s, Screen.width, 20f * s), "Tap a card to pull it out  ·  tabs turn the page", tip, new Color(0.92f, 0.82f, 0.58f), 1, 1);

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
            float pageTop = boxY + 50f * s;
            float pageH = pageBottom - pageTop;
            float pagePad = 16f * s;
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

            // Page tabs along the bottom
            float tabY = pageBottom + 6f * s;
            float tabGap = 6f * s;
            float tabW = Mathf.Min(56f * s, (Screen.width - 32f * s - (pages - 1) * tabGap) / pages);
            float tabsW = pages * tabW + (pages - 1) * tabGap;
            float tabX0 = (Screen.width - tabsW) * 0.5f;
            var tabLab = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(14 * s)
            };
            for (int p = 0; p < pages; p++)
            {
                var tab = new Rect(tabX0 + p * (tabW + tabGap), tabY, tabW, tabH);
                bool on = p == _hivePage && !turning;
                bool held = false;
                bool hit = !turning && _hiveInspect < 0 && HitPad(tab, out held);
                GUI.color = on
                    ? new Color(0.92f, 0.72f, 0.28f, held ? 0.95f : 0.88f)
                    : new Color(0.18f, 0.14f, 0.10f, held ? 0.90f : 0.72f);
                GUI.DrawTexture(tab, Texture2D.whiteTexture);
                GUI.color = Color.white;
                StampOutlined(tab, (p + 1).ToString(), tabLab, on ? new Color(0.28f, 0.14f, 0.05f) : new Color(1f, 0.94f, 0.72f), 1, 1);
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
            if (keep)
            {
                yield return SnapRound();
                _gift = GiftFace.None;
                _busy = false;
                yield break;
            }
            GrantSpare();
            if (_frozen && _garden.Ice != null)
                yield return _garden.Ice.Shatter(_garden.Root);
            StillBirds(false);
            _frozen = false;
            _freezeOffer = false;
            yield return GardenFit.Tween(_garden, _board, false);
            _gift = GiftFace.Thanks;
            yield return new WaitForSeconds(1.15f);
            _gift = GiftFace.None;
            _busy = false;
        }

        void GrantSpare()
        {
            if (_board == null) return;
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
            if (_garden.Root != null)
                StartCoroutine(Wow.Burst(burst != null ? burst.transform.position : Vector3.zero, BirdColor.Gold, _garden.Root, 3));
            SyncAll();
        }

        int AppendSpare()
        {
            int i = _board.Branches.Count;
            _board.Branches.Add(new BranchState());
            var list = new System.Collections.Generic.List<BranchView>(_garden.Branches);
            var v = WorldBuilder.MakeSpare(i, new Vector2(0f, WorldBuilder.GiftY), _garden.Root);
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
            GUI.color = new Color(0.08f, 0.06f, 0.04f, _gift == GiftFace.Card ? 0.62f : 0.78f);
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

            float cardW = Mathf.Min(Screen.width * 0.92f, 640f * s);
            float cardH = cardW * (501f / 780f);
            var card = new Rect((Screen.width - cardW) * 0.5f, Screen.height * 0.16f, cardW, cardH);
            var tex = SpriteCatalog.AdCard != null ? SpriteCatalog.AdCard.texture : null;
            if (tex != null)
            {
                GUI.color = new Color(0.10f, 0.06f, 0.03f, 0.40f);
                GUI.DrawTexture(new Rect(card.x + 6f, card.y + 14f, card.width, card.height), tex, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
                GUI.DrawTexture(card, tex, ScaleMode.ScaleToFit, true);
            }

            var face = new Rect(card.x + card.width * 0.12f, card.y + card.height * 0.24f, card.width * 0.76f, card.height * 0.62f);
            var title = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
                wordWrap = true
            };
            string head = _keepStreak
                ? "Keep the streak?"
                : (_freezeOffer ? "Garden frozen" : "Bonus branch");
            title.fontSize = FitFont(title, head, face.width, face.height * 0.38f, 28, 56);
            StampOutlined(new Rect(face.x, face.y, face.width, face.height * 0.40f), head, title, new Color(1f, 0.94f, 0.78f), 3, 2);

            var body = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.BoldAndItalic,
                alignment = TextAnchor.UpperCenter,
                wordWrap = true
            };
            string copy = _keepStreak
                ? "Watch a short video to keep ×" + Purse.Streak + "."
                : (_freezeOffer
                    ? "Watch a short video to thaw an extra branch."
                    : "Watch a short video to claim this perch.");
            body.fontSize = FitFont(body, copy, face.width, face.height * 0.48f, 20, 34);
            StampOutlined(new Rect(face.x, face.y + face.height * 0.40f, face.width, face.height * 0.52f), copy, body, new Color(0.98f, 0.90f, 0.70f), 3, 2);

            float flower = Mathf.Min(Screen.width * 0.62f, 340f * s);
            var cta = new Rect((Screen.width - flower) * 0.5f, card.yMax - flower * 0.18f, flower, flower);
            // [x] lives under the flower so it is never inside the Watch hit pad.
            var later = new Rect((Screen.width - 96f * s) * 0.5f, cta.yMax + 6f * s, 96f * s, 40f * s);
            bool dismiss = !_freezeOffer && HitPad(later, out _);
            if (dismiss)
            {
                if (_keepStreak)
                {
                    Purse.BreakStreak();
                    StartCoroutine(SnapRound());
                }
                else CloseGift();
                return;
            }

            // Watch only on the wood disc — not the whole flower sprite (petals used to eat [x]).
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
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = false
                };
                watchSt.fontSize = FitFont(watchSt, "WATCH", disc.width * 0.86f, disc.height * 0.72f, 26, 68);
                int stitch = Mathf.Max(3, Mathf.RoundToInt(watchSt.fontSize * 0.06f));
                StampOutlined(disc, "WATCH", watchSt, new Color(1f, 0.95f, 0.78f), stitch, 2);
            }
            if (watch)
            {
                StartCoroutine(WatchGift());
                return;
            }

            if (_freezeOffer) return;
            var laterSt = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Normal,
                fontSize = Mathf.RoundToInt(18 * s),
                alignment = TextAnchor.MiddleCenter
            };
            laterSt.normal.textColor = new Color(0.55f, 0.42f, 0.28f);
            laterSt.hover.textColor = laterSt.normal.textColor;
            laterSt.active.textColor = laterSt.normal.textColor;
            GUI.Label(later, "[x]", laterSt);
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
            var r = new Rect(40f * s, Screen.height * 0.38f, Screen.width - 80f * s, 80f * s);
            var st = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            st.fontSize = Mathf.RoundToInt(36 * s);
            StampOutlined(r, "It's yours.", st, new Color(1f, 0.9f, 0.62f), 3, 1);
        }
    }
}