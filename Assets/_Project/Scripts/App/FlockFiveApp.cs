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
        readonly HashSet<int> _locked = new HashSet<int>();
        int _combo;
        float _comboUntil = -99f;
        bool _collecting;
        float _nextTap;
        bool _finalePreview;
        bool _splash = true;
        bool _levelHive;
        enum HomeFace { Splash, Hive }
        HomeFace _home;
        enum GiftFace { None, Card, Movie, Thanks }
        GiftFace _gift;
        bool _frozen;
        bool _freezeOffer;
        readonly List<BeeVisit> _levelBees = new List<BeeVisit>();

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
                var visit = Hive.TakeVisitor();
                _levelBees.Add(visit);
                _levelHive = true;
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
            bot = 72f * scale + safeBot;
            float y = Screen.height - bot - 8f;
            float h = 64f * scale;
            float x = Mathf.Max(16f, safe.xMin + 8f);
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
                else DrawSplash();
                return;
            }
            if (_board == null) return;
            HudLayout(out float s, out float top, out _, out var restart, out var hive);
            var lab = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(22 * s) };
            lab.normal.textColor = Color.white;
            GUILayout.BeginArea(new Rect(16, Mathf.Max(8f, Screen.height - Screen.safeArea.yMax), Screen.width - 32, top));
            string lv = LevelData.Current != null
                ? LevelData.Current.Number + "  " + LevelData.Current.Title + "    "
                : "";
            GUILayout.Label("FLOCK FIVE    " + lv + _board.RemainingBirds + " birds", lab);
            GUILayout.EndArea();
            var arrow = SpriteCatalog.Restart;
            if (arrow != null && arrow.texture != null)
                GUI.DrawTexture(restart, arrow.texture, ScaleMode.ScaleToFit, true);
            else
                GUI.Box(restart, "↩");
            DrawHudPurse(s);
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

        static Rect SplashHiveRect() => HomeRailRect(true, SplashRailSize());

        static Rect PiggyRect(float s)
        {
            // Top-right, stacked above the hive; same bumped size as the hive.
            float size = SplashRailSize();
            var hive = SplashHiveRect();
            float gap = Mathf.Max(8f, size * 0.10f);
            return new Rect(hive.x, hive.y - size - gap, size, size);
        }

        void DrawStreakRewards(float s)
        {
            var pig = PiggyRect(s);
            var spr = SpriteCatalog.Piggy;
            if (spr != null && spr.texture != null)
            {
                GUI.color = new Color(0.08f, 0.05f, 0.02f, 0.30f);
                GUI.DrawTexture(new Rect(pig.x + 3f, pig.y + 5f, pig.width, pig.height), spr.texture, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
                GUI.DrawTexture(pig, spr.texture, ScaleMode.ScaleToFit, true);
            }
            var st = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight,
                wordWrap = false
            };
            // Streak + coins sit left of the pig/hive rail so the icons stay clean.
            float labelW = Mathf.Max(120f, pig.x - 28f);
            var streakR = new Rect(16f, pig.y + pig.height * 0.08f, labelW, pig.height * 0.42f);
            string streak = "STREAK  ×" + Purse.Streak;
            st.fontSize = FitFont(st, streak, streakR.width * 0.95f, streakR.height, 16, 32);
            StampOutlined(streakR, streak, st, new Color(0.36f, 0.18f, 0.07f), 2, 1);
            string coins = Purse.Coins.ToString();
            var coinR = new Rect(16f, pig.y + pig.height * 0.50f, labelW, pig.height * 0.42f);
            st.fontSize = FitFont(st, coins, coinR.width * 0.7f, coinR.height, 16, 34);
            StampOutlined(coinR, coins, st, new Color(0.42f, 0.26f, 0.08f), 2, 1);
            DrawCoinFly(pig);
        }

        void DrawHudPurse(float s)
        {
            var safe = Screen.safeArea;
            float size = 52f * s;
            var pig = new Rect(Mathf.Max(16f, safe.xMin + 8f) + 72f * s, Screen.height - 72f * s - Mathf.Max(8f, Screen.safeArea.yMin) - 8f, size, size);
            var spr = SpriteCatalog.Piggy;
            if (spr != null && spr.texture != null)
                GUI.DrawTexture(pig, spr.texture, ScaleMode.ScaleToFit, true);
            var st = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            string tx = "×" + Purse.Streak + "   " + Purse.Coins;
            st.fontSize = Mathf.RoundToInt(20 * s);
            StampOutlined(new Rect(pig.xMax + 6f, pig.y, 220f * s, size), tx, st, new Color(0.36f, 0.18f, 0.07f), 2, 1);
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
                float x = Mathf.Min(Screen.width, safe.xMax) - 14f - size;
                return new Rect(x, y, size, size);
            }
            float lx = Mathf.Max(14f, safe.xMin + 10f);
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
            var hiveSpr = SpriteCatalog.Hive;
            if (hiveSpr != null && hiveSpr.texture != null)
            {
                GUI.color = new Color(0.08f, 0.05f, 0.02f, 0.35f);
                GUI.DrawTexture(new Rect(hiveR.x + 3f, hiveR.y + 6f, hiveR.width, hiveR.height), hiveSpr.texture, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
                GUI.DrawTexture(hiveR, hiveSpr.texture, ScaleMode.ScaleToFit, true);
            }

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
                _home = HomeFace.Splash;
            GUI.color = new Color(0.10f, 0.08f, 0.05f, backHeld ? 0.88f : 0.72f);
            GUI.DrawTexture(back, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(back, "Back", backLab);

            float hiveSize = Mathf.Clamp(Screen.width * 0.20f, 80f, 140f);
            var hiveSpr = SpriteCatalog.Hive;
            if (hiveSpr != null && hiveSpr.texture != null)
                GUI.DrawTexture(new Rect((Screen.width - hiveSize) * 0.5f, top + 8f, hiveSize, hiveSize), hiveSpr.texture, ScaleMode.ScaleToFit, true);

            float boxY = top + hiveSize + 16f;
            float rowH = 34f * Mathf.Min(s, 1.65f);
            float boxH = 56f * s + Hive.Kinds * rowH + 24f;
            float maxH = Screen.height - boxY - 20f;
            var box = new Rect(20f * s, boxY, Screen.width - 40f * s, Mathf.Min(boxH, maxH));
            GUI.color = new Color(0.12f, 0.10f, 0.07f, 0.82f);
            GUI.DrawTexture(box, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUILayout.BeginArea(new Rect(box.x + 18f, box.y + 12f, box.width - 36f, box.height - 24f));
            GUILayout.Label("Your hive  " + Hive.Found + " of " + Hive.Kinds, title);
            GUILayout.Space(8f);
            for (int i = 0; i < Hive.Kinds; i++)
            {
                int n = Hive.CountOf(i);
                var c = n > 0 ? Hive.Roster[i].Tint : new Color(0.70f, 0.66f, 0.55f, 0.85f);
                if (n > 0)
                {
                    float lum = 0.22f * c.r + 0.72f * c.g + 0.06f * c.b;
                    if (lum < 0.40f) c = Color.Lerp(c, new Color(0.94f, 0.88f, 0.74f), 0.55f);
                }
                row.normal.textColor = c;
                string line = n > 0
                    ? Hive.Roster[i].Name + (n > 1 ? "  ×" + n : "")
                    : "—  still out in the garden";
                GUILayout.Label(line, row);
            }
            GUILayout.EndArea();
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
                    GUILayout.Label(v.Kind.Name, row);
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

        void DrawGiftOffer        void DrawGiftOffer(float s)
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
                : (_freezeOffer ? "Iced over" : "A spare perch");
            title.fontSize = FitFont(title, head, face.width, face.height * 0.38f, 28, 56);
            StampOutlined(new Rect(face.x, face.y, face.width, face.height * 0.40f), head, title, new Color(1f, 0.94f, 0.78f), 3, 2);

            var body = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.BoldAndItalic,
                alignment = TextAnchor.UpperCenter,
                wordWrap = true
            };
            string copy = _keepStreak
                ? "Watch this little movie to hold ×" + Purse.Streak + "."
                : (_freezeOffer
                    ? "Hey — watch this little movie and an extra branch thaws the garden."
                    : "Hey — watch this little movie and this extra branch is yours.");
            body.fontSize = FitFont(body, copy, face.width, face.height * 0.48f, 20, 34);
            StampOutlined(new Rect(face.x, face.y + face.height * 0.40f, face.width, face.height * 0.52f), copy, body, new Color(0.98f, 0.90f, 0.70f), 3, 2);

            float flower = Mathf.Min(Screen.width * 0.62f, 340f * s);
            var cta = new Rect((Screen.width - flower) * 0.5f, card.yMax - flower * 0.18f, flower, flower);
            bool watch = HitPad(cta, out bool held);
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
            var later = new Rect((Screen.width - 80f * s) * 0.5f, cta.y + cta.height * 0.82f, 80f * s, 36f * s);
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
            if (HitPad(later, out _))
            {
                if (_keepStreak)
                {
                    Purse.BreakStreak();
                    StartCoroutine(SnapRound());
                }
                else CloseGift();
            }
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
            StampOutlined(stage, "A short garden movie…", st, new Color(1f, 0.92f, 0.72f), 2, 1);
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
