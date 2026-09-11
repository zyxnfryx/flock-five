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
        // Splash streak toast: <0 idle; 0..1 in+hold+out.
        float _streakSlide = -1f;
        int _streakAnnounced = -1;
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
            if (System.IO.File.Exists("/tmp/flock-five-storm-shot"))
            {
                try { System.IO.File.Delete("/tmp/flock-five-storm-shot"); } catch { }
                StartCoroutine(ShotStorm());
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
            if (_garden.Root != null) Destroy(_garden.Root.gameObject);
            if (_garden.Cam != null) Destroy(_garden.Cam.gameObject);
            _garden = default;
            _board = null;
            WorldBuilder.MakeCamera(transform);
            if (MixDesk.Live != null) MixDesk.Live.SetSplash(true);
            Purse.Boot();
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
            if (WantFinalePreview())
                StartCoroutine(PreviewFinale());
        }

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
            ScreenCapture.CaptureScreenshot("/tmp/paradice/splash-easy.png");
            yield return new WaitForSecondsRealtime(0.4f);
            PlayerPrefs.SetInt("flockfive.next", 5);
            PlayerPrefs.Save();
            yield return null;
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("/tmp/paradice/splash-sde.png");
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
            _board.ApplyCollect(branch);

            float haste = Mathf.Lerp(1f, 0.52f, Mathf.Clamp01((combo - 1) / 7f));
            float step = 0.192f * haste;
            float fly = 0.432f * haste;
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
                return;
            }
            // Re-arm when the streak value changes, or every splash return.
            _streakAnnounced = Purse.Streak;
            _streakSlide = 0f;
        }

        void DrawStreakRewards(float s)
        {
            var pig = PiggyRect(s);
            DrawRailIcon(pig, SpriteCatalog.Piggy);

            // Bigger persistent balance: "$12" + coin sprite on the right.
            DrawCoinBalance(s, pig);

            // Streak pops in from the right, holds, then dismisses — frees room for coins.
            DrawStreakToast(s, pig);
            DrawCoinFly(pig);
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

            // 0–0.22 in, 0.22–0.72 hold, 0.72–1 out (~2.4s total).
            const float dur = 2.4f;
            _streakSlide = Mathf.Min(1f, _streakSlide + Time.unscaledDeltaTime / dur);
            float u = _streakSlide;
            float vis;
            if (u < 0.22f) vis = Mathf.SmoothStep(0f, 1f, u / 0.22f);
            else if (u < 0.72f) vis = 1f;
            else
            {
                vis = 1f - Mathf.SmoothStep(0f, 1f, (u - 0.72f) / 0.28f);
                if (u >= 1f) _streakSlide = -1f;
            }

            var st = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight,
                wordWrap = false
            };
            string streak = "STREAK  ×" + Purse.Streak;
            float h = Mathf.Max(pig.height * 0.38f, 36f * s);
            float w = Mathf.Max(160f * s, pig.x - 24f);
            float restX = pig.x - 12f - w;
            float off = (1f - vis) * (Screen.width * 0.55f);
            var streakR = new Rect(restX + off, pig.y - h - 6f * s, w, h);
            st.fontSize = FitFont(st, streak, streakR.width * 0.95f, streakR.height, 18, 36);
            var prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(vis));
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
                    A = new Vector2(Screen.width * 0.5f + Random.Range(-80f, 80f), Screen.height * 0.42f + Random.Range(-40f, 40f)),
                    B = dest,
                    T = 0f,
                    Delay = i * 0.07f,
                    Spin = Random.Range(-220f, 220f)
                });
            }
        }

        void DrawCoinFly(Rect pig)
        {
            if (_flies.Count == 0) return;
            var spr = SpriteCatalog.Coin;
            var tex = spr != null ? spr.texture : null;
            float dt = Time.unscaledDeltaTime;
            float size = 54f * Mathf.Max(Screen.height / 720f, 1f);
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
                f.T += dt / 0.55f;
                float u = Mathf.Clamp01(f.T);
                float k = u * u * (3f - 2f * u);
                var p = Vector2.Lerp(f.A, dest, k);
                p.y -= Mathf.Sin(u * Mathf.PI) * 90f;
                float sc = size * (1f - 0.35f * u);
                if (tex != null)
                    GUI.DrawTexture(new Rect(p.x - sc * 0.5f, p.y - sc * 0.5f, sc, sc), tex, ScaleMode.ScaleToFit, true);
                if (u >= 1f)
                {
                    Sfx.Clink();
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

            var title = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(44 * s),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            title.normal.textColor = new Color(1f, 0.94f, 0.72f);

            float top = Mathf.Max(24f, Screen.height - Screen.safeArea.yMax + 12f);
            GUI.Label(new Rect(20, top, Screen.width - 40, 70 * s), "FLOCK FIVE", title);
            DrawStreakRewards(s);

            int next = LevelData.NextPlay;
            var peek = LevelData.Peek(next);

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

            string ease = LevelData.JokeEase(next);
            int number = peek != null ? peek.Number : next + 1;
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
            var easeR = new Rect(disc.x + disc.width * 0.12f, disc.y + disc.height * 0.08f, disc.width * 0.76f, disc.height * 0.20f);
            var lvR = hasEase
                ? new Rect(disc.x + disc.width * 0.02f, disc.y + disc.height * 0.34f, disc.width * 0.96f, disc.height * 0.56f)
                : new Rect(disc.x, disc.y + disc.height * 0.08f, disc.width, disc.height * 0.84f);

            if (hasEase)
            {
                var joke = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.BoldAndItalic,
                    alignment = TextAnchor.LowerCenter,
                    wordWrap = false
                };
                joke.fontSize = FitFont(joke, ease, easeR.width, easeR.height, 18, 32);
                StampOutlined(easeR, ease, joke, new Color(0.34f, 0.18f, 0.07f), 2, 1);
            }

            var lv = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = hasEase ? TextAnchor.UpperCenter : TextAnchor.MiddleCenter,
                wordWrap = false
            };
            string level = "LEVEL " + number;
            lv.fontSize = FitFont(lv, level, lvR.width * 0.88f, lvR.height * 0.80f, 32, 96);
            int white = Mathf.Max(2, Mathf.RoundToInt(lv.fontSize * 0.055f));
            int black = 1;
            StampOutlined(lvR, level, lv, new Color(0.36f, 0.18f, 0.07f), white, black);
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

        static void StampOutlined(Rect r, string text, GUIStyle st, Color fill, int whitePx, int blackPx)
        {
            Paint(st, new Color(0.02f, 0.02f, 0.02f, 1f));
            Ring(r, text, st, whitePx + blackPx);
            Paint(st, Color.white);
            Ring(r, text, st, whitePx);
            Paint(st, fill);
            GUI.Label(r, text, st);
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

            var purseSt = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(24 * s),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight
            };
            StampOutlined(new Rect(Screen.width - 200f * s, top, 180f * s, 40f * s), "$" + Purse.Coins, purseSt, new Color(0.42f, 0.26f, 0.08f), 2, 1);

            float cardW = Mathf.Min(Screen.width * 0.17f, 110f * s);
            float cardH = cardW * 1.28f;
            float gap = 8f * s;
            float rowW = BirdPoker.HandSize * cardW + (BirdPoker.HandSize - 1) * gap;
            float rowX = (Screen.width - rowW) * 0.5f;
            float rowY = top + 70f * s;
            var deckPile = new Rect((Screen.width - cardW) * 0.5f, top + 8f * s, cardW * 0.72f, cardH * 0.72f);
            TickPokerMotion();
            DrawPokerDeckPile(deckPile, s);
            for (int i = 0; i < BirdPoker.HandSize; i++)
            {
                var seat = new Rect(rowX + i * (cardW + gap), rowY, cardW, cardH);
                DrawPokerCard(seat, deckPile, i, s);
            }

            var info = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(22 * s),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            info.normal.textColor = new Color(1f, 0.94f, 0.72f);
            string line = BirdPoker.PhaseNow == BirdPoker.Phase.Idle
                ? "Bet $" + BirdPoker.Bet + "  ·  Deal five  ·  Natural five wins big"
                : BirdPoker.PhaseNow == BirdPoker.Phase.Dealt
                    ? "Tap cards to HOLD  ·  then DRAW"
                    : (BirdPoker.LastWin > 0
                        ? BirdPoker.RankLabel(BirdPoker.LastRank) + "  +$" + BirdPoker.LastWin
                        : "No pay  ·  try again");
            GUI.Label(new Rect(16f, rowY + cardH + 12f * s, Screen.width - 32f, 36f * s), line, info);

            float punchY = rowY + cardH + 52f * s;
            DrawPokerPunchCard(punchY, s);

            float btnW = Mathf.Min(Screen.width * 0.42f, 220f * s);
            float btnH = 56f * s;
            float btnY = Mathf.Max(punchY + 78f * s, Screen.height - Mathf.Max(24f, safe.yMin + 16f) - btnH * 2.4f);
            var betR = new Rect(Screen.width * 0.5f - btnW - 8f * s, btnY, btnW, btnH);
            var actR = new Rect(Screen.width * 0.5f + 8f * s, btnY, btnW, btnH);
            var againR = new Rect((Screen.width - btnW) * 0.5f, btnY + btnH + 10f * s, btnW, btnH);

            bool busy = PokerMotionBusy() || _pokerStamp;
            TickPokerStamp();
            if (_pokerStamp) DrawPokerStampCeremony(s);
            if (BirdPoker.PhaseNow == BirdPoker.Phase.Idle)
            {
                if (!busy && DrawPokerBtn(betR, "BET $" + BirdPoker.Bet, s))
                    BirdPoker.CycleBet();
                if (!busy && DrawPokerBtn(actR, "DEAL", s))
                {
                    if (!BirdPoker.Deal())
                        Sfx.Deny();
                    else
                    {
                        BeginPokerDeal();
                        Sfx.Chirp(BirdColor.Gold);
                    }
                }
            }
            else if (BirdPoker.PhaseNow == BirdPoker.Phase.Dealt)
            {
                if (!busy && DrawPokerBtn(actR, "DRAW", s))
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
            else
            {
                if (!busy && DrawPokerBtn(againR, "AGAIN", s))
                {
                    BirdPoker.Collect();
                    BeginPokerShuffle();
                    Sfx.Chirp(BirdColor.Teal);
                }
            }
        }

        void BeginPokerStamp(int kind)
        {
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

        void DrawPokerPunchCard(float y, float s)
        {
            int cols = 5;
            float cell = Mathf.Min((Screen.width - 48f * s) / cols, 52f * s);
            float gap = 6f * s;
            float gridW = cols * cell + (cols - 1) * gap;
            float x0 = (Screen.width - gridW) * 0.5f;
            var lab = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(12 * s)
            };
            lab.normal.textColor = new Color(1f, 0.94f, 0.72f);
            GUI.Label(new Rect(16f, y - 22f * s, Screen.width - 32f, 20f * s),
                "PUNCH CARD  " + BirdPoker.PunchFound() + " / " + BirdPoker.PunchKinds, lab);

            for (int i = 0; i < BirdPoker.PunchKinds; i++)
            {
                int col = i % cols;
                int row = i / cols;
                var r = new Rect(x0 + col * (cell + gap), y + row * (cell + gap), cell, cell);
                bool on = BirdPoker.IsPunched(i);
                GUI.color = on ? new Color(0.20f, 0.16f, 0.08f, 0.92f) : new Color(0.10f, 0.08f, 0.05f, 0.72f);
                GUI.DrawTexture(r, Texture2D.whiteTexture);
                GUI.color = Color.white;
                BirdColor c;
                BirdSex sex;
                BirdPoker.KindParts(i, out c, out sex);
                var spr = SpriteCatalog.Bird(c, sex);
                if (spr != null && spr.texture != null)
                {
                    float pad = cell * 0.12f;
                    var ir = new Rect(r.x + pad, r.y + pad, cell - pad * 2f, cell - pad * 2f);
                    if (!on) GUI.color = new Color(1f, 1f, 1f, 0.28f);
                    GUI.DrawTexture(ir, spr.texture, ScaleMode.ScaleToFit, true);
                    GUI.color = Color.white;
                }
                if (on)
                {
                    // Punch hole mark.
                    GUI.color = new Color(1f, 0.86f, 0.35f, 0.85f);
                    float hole = cell * 0.22f;
                    GUI.DrawTexture(new Rect(r.xMax - hole - 4f * s, r.y + 4f * s, hole, hole), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
            }
        }

        bool PokerMotionBusy() =>
            _pokerMotion != PokerMotion.None && _pokerMotionT < PokerMotionDur();

        float PokerMotionDur()
        {
            if (_pokerMotion == PokerMotion.Deal) return 0.22f * BirdPoker.HandSize + 0.38f;
            if (_pokerMotion == PokerMotion.Draw) return 0.20f * BirdPoker.HandSize + 0.55f;
            if (_pokerMotion == PokerMotion.Shuffle) return 0.85f;
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

        void BeginPokerShuffle()
        {
            _pokerMotion = PokerMotion.Shuffle;
            _pokerMotionT = 0f;
        }

        void DrawPokerDeckPile(Rect r, float s)
        {
            // Quiet shoe in the center-top — shuffle wiggle when resetting.
            float wiggle = 0f;
            if (_pokerMotion == PokerMotion.Shuffle)
            {
                float u = Mathf.Clamp01(_pokerMotionT / 0.85f);
                wiggle = Mathf.Sin(u * Mathf.PI * 6f) * (1f - u) * 10f * s;
            }
            var pile = new Rect(r.x + wiggle, r.y, r.width, r.height);
            GUI.color = new Color(0.08f, 0.06f, 0.04f, 0.55f);
            GUI.DrawTexture(new Rect(pile.x + 4f, pile.y + 5f, pile.width, pile.height), Texture2D.whiteTexture);
            GUI.color = new Color(0.16f, 0.12f, 0.08f, 0.92f);
            GUI.DrawTexture(pile, Texture2D.whiteTexture);
            GUI.color = new Color(0.22f, 0.17f, 0.10f, 0.95f);
            GUI.DrawTexture(new Rect(pile.x - 3f * s, pile.y - 3f * s, pile.width, pile.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var st = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(11 * s)
            };
            StampOutlined(pile, "SHOE", st, new Color(0.95f, 0.88f, 0.65f), 1, 1);
        }

        void DrawPokerCard(Rect seat, Rect deck, int i, float s)
        {
            bool empty = BirdPoker.PhaseNow == BirdPoker.Phase.Idle && _pokerMotion != PokerMotion.Deal;
            bool held = !empty && BirdPoker.Hold[i] && _pokerMotion != PokerMotion.Deal;
            bool canHold = !PokerMotionBusy() && BirdPoker.PhaseNow == BirdPoker.Phase.Dealt;
            if (!empty && canHold && HitPad(seat, out _))
            {
                BirdPoker.ToggleHold(i);
                held = BirdPoker.Hold[i];
                Sfx.Chirp(BirdColor.Gold);
            }

            Rect drawR = seat;
            float alpha = 1f;
            float scale = 1f;
            if (_pokerMotion == PokerMotion.Deal)
            {
                float delay = i * 0.10f;
                float u = Mathf.Clamp01((_pokerMotionT - delay) / 0.36f);
                u = u * u * (3f - 2f * u);
                if (u <= 0.001f) return;
                float x = Mathf.Lerp(deck.x, seat.x, u);
                float y = Mathf.Lerp(deck.y - 20f * s, seat.y, u) - Mathf.Sin(u * Mathf.PI) * 28f * s;
                scale = Mathf.Lerp(0.55f, 1f, u);
                float w = seat.width * scale;
                float h = seat.height * scale;
                drawR = new Rect(x + (seat.width - w) * 0.5f, y + (seat.height - h) * 0.5f, w, h);
                alpha = Mathf.Lerp(0.15f, 1f, u);
            }
            else if (_pokerMotion == PokerMotion.Draw && _pokerRedraw[i])
            {
                float delay = i * 0.08f;
                float u = Mathf.Clamp01((_pokerMotionT - delay) / 0.48f);
                if (u < 0.45f)
                {
                    // Lift old face toward the shoe.
                    float o = u / 0.45f;
                    o = o * o * (3f - 2f * o);
                    float x = Mathf.Lerp(seat.x, deck.x, o);
                    float y = Mathf.Lerp(seat.y, deck.y - 12f * s, o) - Mathf.Sin(o * Mathf.PI) * 18f * s;
                    scale = Mathf.Lerp(1f, 0.6f, o);
                    alpha = 1f - o;
                    float w = seat.width * scale;
                    float h = seat.height * scale;
                    drawR = new Rect(x + (seat.width - w) * 0.5f, y + (seat.height - h) * 0.5f, w, h);
                }
                else
                {
                    // Settle new face from the shoe.
                    float o = (u - 0.45f) / 0.55f;
                    o = o * o * (3f - 2f * o);
                    float x = Mathf.Lerp(deck.x, seat.x, o);
                    float y = Mathf.Lerp(deck.y - 12f * s, seat.y, o) - Mathf.Sin(o * Mathf.PI) * 24f * s;
                    scale = Mathf.Lerp(0.6f, 1f, o);
                    alpha = o;
                    float w = seat.width * scale;
                    float h = seat.height * scale;
                    drawR = new Rect(x + (seat.width - w) * 0.5f, y + (seat.height - h) * 0.5f, w, h);
                }
            }

            var prev = GUI.color;
            GUI.color = new Color(0.10f, 0.08f, 0.05f, 0.88f * alpha);
            GUI.DrawTexture(drawR, Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (held)
            {
                GUI.color = new Color(1f, 0.86f, 0.35f, 0.35f * alpha);
                GUI.DrawTexture(drawR, Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            if (empty)
            {
                GUI.color = prev;
                return;
            }

            if (alpha > 0.05f)
            {
                GUI.color = new Color(1f, 1f, 1f, alpha);
                bool liftAway = _pokerMotion == PokerMotion.Draw && _pokerRedraw[i]
                    && (_pokerMotionT - i * 0.08f) / 0.48f < 0.45f;
                var card = liftAway ? _pokerPrev[i] : BirdPoker.Hand[i];
                if (card.Wild)
                {
                    var st = new GUIStyle(GUI.skin.label)
                    {
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter
                    };
                    st.fontSize = FitFont(st, "WILD", drawR.width * 0.85f, drawR.height * 0.35f, 14, 28);
                    StampOutlined(drawR, "WILD", st, new Color(0.95f, 0.75f, 0.2f), 2, 1);
                }
                else
                {
                    var spr = SpriteCatalog.Bird(card.Color, card.Sex);
                    if (spr != null && spr.texture != null)
                    {
                        float pad = drawR.width * 0.08f;
                        GUI.DrawTexture(new Rect(drawR.x + pad, drawR.y + pad, drawR.width - pad * 2f, drawR.height - pad * 2f), spr.texture, ScaleMode.ScaleToFit, true);
                    }
                }
                GUI.color = Color.white;
            }
            if (held && !PokerMotionBusy())
            {
                var hold = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.LowerCenter
                };
                hold.fontSize = Mathf.RoundToInt(14 * s);
                StampOutlined(new Rect(seat.x, seat.yMax - 22f * s, seat.width, 22f * s), "HOLD", hold, new Color(0.36f, 0.18f, 0.07f), 2, 1);
            }
            GUI.color = prev;
        }

        bool DrawPokerBtn(Rect r, string label, float s)
        {
            bool fire = HitPad(r, out bool held);
            GUI.color = new Color(0.12f, 0.10f, 0.07f, held ? 0.92f : 0.78f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
            var st = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            st.fontSize = FitFont(st, label, r.width * 0.9f, r.height * 0.7f, 16, 28);
            StampOutlined(r, label, st, new Color(1f, 0.94f, 0.72f), 2, 1);
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
                    if (!_hiveInspectClosing)
                    {
                        _hiveInspectClosing = true;
                        _hiveInspectT = 0f;
                        _hiveFlip = -1;
                    }
                }
                else
                {
                    _hiveFlip = -1;
                    _hivePageTurn = 99f;
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

            // Large portrait card ~1:1.4, near-fullscreen
            float maxW = Screen.width * 0.86f;
            float maxH = Screen.height * 0.62f;
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
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(14 * s)
            };
            float hintY = big.yMax + 10f * s;
            StampOutlined(new Rect(0f, hintY, Screen.width, 22f * s), "Tap card to flip  ·  tap outside to put back", hint, new Color(0.92f, 0.82f, 0.58f, Mathf.Clamp01(t * 1.4f)), 1, 1);

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
                    Sfx.Chirp(BirdColor.Gold);
                }
            }

            // Tap dim outside card closes (after card/X so they win hit tests)
            var full = new Rect(0f, 0f, Screen.width, Screen.height);
            bool outside = !_hiveInspectClosing && u > 0.85f && HitPad(full, out _);
            if ((outside || xHit) && !_hiveInspectClosing)
            {
                _hiveInspectClosing = true;
                _hiveInspectT = 0f;
                _hiveFlip = -1;
            }

            if (_hiveInspectClosing && u >= 1f)
            {
                _hiveInspect = -1;
                _hiveInspectClosing = false;
                _hiveInspectT = 0f;
                _hiveFlip = -1;
            }
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
            bool showBack = flipping ? (flipU >= 0.5f ? !_hiveFaceBack[i] : _hiveFaceBack[i]) : _hiveFaceBack[i];

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

            // Inner face
            var face = new Rect(r.x + r.width * 0.06f, r.y + r.height * 0.05f, r.width * 0.88f, r.height * 0.90f);
            GUI.color = owned
                ? Color.Lerp(new Color(0.98f, 0.92f, 0.72f), kind.Tint, 0.18f)
                : new Color(0.22f, 0.20f, 0.18f, 0.95f);
            GUI.DrawTexture(face, Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Subtle foil sheen for owned non-Normal finishes (locked stays grey)
            if (owned && finish != BeeFinish.Normal && squash > 0.2f)
            {
                float sheen = 0.10f + 0.06f * Mathf.Sin(Time.unscaledTime * 2.4f + i * 0.35f);
                if (finish == BeeFinish.Holo)
                    GUI.color = new Color(0.55f, 0.85f, 1f, sheen);
                else
                    GUI.color = new Color(0.95f, 0.55f, 0.85f, sheen * 1.15f);
                GUI.DrawTexture(new Rect(face.x, face.y, face.width * 0.35f, face.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            if (!owned)
            {
                var mystery = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                mystery.fontSize = FitFont(mystery, "?", face.width * 0.5f, face.height * 0.35f, 28, 64);
                StampOutlined(new Rect(face.x, face.y + face.height * 0.18f, face.width, face.height * 0.4f), "?", mystery, new Color(0.55f, 0.48f, 0.38f), 2, 1);
                var lockSt = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Italic,
                    alignment = TextAnchor.UpperCenter,
                    wordWrap = true
                };
                lockSt.fontSize = FitFont(lockSt, "Still out buzzing", face.width * 0.9f, face.height * 0.2f, 12, 18);
                StampOutlined(new Rect(face.x + face.width * 0.05f, face.yMax - face.height * 0.28f, face.width * 0.9f, face.height * 0.22f), "Still out buzzing", lockSt, new Color(0.62f, 0.56f, 0.45f), 1, 1);
            }
            else if (!showBack)
            {
                var bee = SpriteCatalog.Bee;
                if (bee != null && bee.texture != null)
                {
                    float bh = face.height * 0.42f;
                    float bw = bh;
                    var br = new Rect(face.center.x - bw * 0.5f, face.y + face.height * 0.08f, bw, bh);
                    GUI.color = kind.Tint;
                    GUI.DrawTexture(br, bee.texture, ScaleMode.ScaleToFit, true);
                    GUI.color = Color.white;
                }
                var nameSt = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperCenter,
                    wordWrap = true
                };
                nameSt.fontSize = FitFont(nameSt, kind.Name, face.width * 0.92f, face.height * 0.16f, 14, 24);
                StampOutlined(new Rect(face.x + face.width * 0.04f, face.y + face.height * 0.52f, face.width * 0.92f, face.height * 0.18f), kind.Name, nameSt, new Color(0.32f, 0.16f, 0.06f), 2, 1);
                var frontSt = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Italic,
                    alignment = TextAnchor.UpperCenter,
                    wordWrap = true
                };
                frontSt.fontSize = FitFont(frontSt, kind.Front, face.width * 0.9f, face.height * 0.22f, 11, 16);
                StampOutlined(new Rect(face.x + face.width * 0.05f, face.y + face.height * 0.70f, face.width * 0.9f, face.height * 0.24f), kind.Front, frontSt, new Color(0.42f, 0.24f, 0.10f), 1, 1);
                if (n > 1)
                {
                    var cnt = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperRight };
                    cnt.fontSize = Mathf.RoundToInt(13 * s);
                    StampOutlined(new Rect(face.xMax - 48f * s, face.y + 4f * s, 44f * s, 20f * s), "×" + n, cnt, new Color(0.36f, 0.18f, 0.07f), 1, 1);
                }
                if (finish != BeeFinish.Normal)
                {
                    string foil = finish == BeeFinish.Holo ? "Holo" : "Inverse Rainbow";
                    var foilSt = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperLeft };
                    foilSt.fontSize = Mathf.RoundToInt(11 * s);
                    Color foilCol = finish == BeeFinish.Holo
                        ? new Color(0.25f, 0.55f, 0.85f)
                        : new Color(0.72f, 0.28f, 0.62f);
                    StampOutlined(new Rect(face.x + 4f * s, face.y + 4f * s, face.width * 0.7f, 18f * s), foil, foilSt, foilCol, 1, 1);
                }
            }
            else
            {
                var backTitle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperCenter,
                    wordWrap = true
                };
                backTitle.fontSize = FitFont(backTitle, kind.Name, face.width * 0.9f, face.height * 0.18f, 13, 22);
                StampOutlined(new Rect(face.x + face.width * 0.05f, face.y + face.height * 0.08f, face.width * 0.9f, face.height * 0.2f), kind.Name, backTitle, new Color(0.32f, 0.16f, 0.06f), 2, 1);
                var blip = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.BoldAndItalic,
                    alignment = TextAnchor.UpperCenter,
                    wordWrap = true
                };
                blip.fontSize = FitFont(blip, kind.Back, face.width * 0.88f, face.height * 0.55f, 12, 18);
                StampOutlined(new Rect(face.x + face.width * 0.06f, face.y + face.height * 0.32f, face.width * 0.88f, face.height * 0.55f), kind.Back, blip, new Color(0.40f, 0.22f, 0.08f), 1, 1);
                if (finish != BeeFinish.Normal)
                {
                    string foil = finish == BeeFinish.Holo ? "Holo" : "Inverse Rainbow";
                    var foilSt = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.LowerCenter };
                    foilSt.fontSize = Mathf.RoundToInt(12 * s);
                    Color foilCol = finish == BeeFinish.Holo
                        ? new Color(0.25f, 0.55f, 0.85f)
                        : new Color(0.72f, 0.28f, 0.62f);
                    StampOutlined(new Rect(face.x, face.yMax - face.height * 0.12f, face.width, face.height * 0.1f), foil, foilSt, foilCol, 1, 1);
                }
            }

            // Binder sleeve tap opens fullscreen inspect (flip lives inside inspect)
            if (!inspectView && owned && !flipping && _hivePageTurn >= 0.55f && _hiveInspect < 0 && HitPad(card, out _))
            {
                _hiveInspect = i;
                _hiveInspectT = 0f;
                _hiveInspectClosing = false;
                _hiveInspectFrom = card;
                _hiveFlip = -1;
                Sfx.Chirp(BirdColor.Gold);
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