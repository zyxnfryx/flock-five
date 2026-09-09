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
        readonly Stack<Board> _undo = new Stack<Board>();
        readonly HashSet<int> _locked = new HashSet<int>();
        int _combo;
        float _comboUntil = -99f;
        bool _collecting;
        float _nextTap;
        bool _finalePreview;
        bool _album;

        void Start()
        {
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Sfx.Warm();
            try { Restart(); }
            catch (System.Exception e) { Debug.LogException(e); }
        }

        void Restart() => Load(LevelData.Index);

        void Load(int index)
        {
            _busy = false;
            _won = false;
            _sel = -1;
            _combo = 0;
            _comboUntil = -99f;
            _collecting = false;
            _locked.Clear();
            _undo.Clear();
            _board = LevelData.Open(index);
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
                Sfx.Chirp(_board.Branches[hit].Tip.Value);
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

            _undo.Push(_board.Clone());
            int fromCount = _board.Branches[from].Count;
            int toCount = _board.Branches[to].Count;
            int wanted = _board.Branches[from].TipRun();
            var hopCol = _board.Branches[from].Tip.Value;
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
                if (_garden.Hive != null)
                    StartCoroutine(_garden.Hive.Welcome(visit, _garden.Branches[from].transform.position + Vector3.up * 0.7f));
                _garden.Branches[from].FlutterTip();
            }
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
            KickCollects();
            SyncAll();
            if (_locked.Count == 0)
                yield return GardenFit.Tween(_garden, _board, false);
            yield return SettleIfIdle();
        }

        IEnumerator SettleIfIdle()
        {
            if (_locked.Count > 0 || _won || _board == null || !_board.Won) yield break;
            _won = true;
            _busy = true;
            yield return FinaleShow.Play(_garden, this);
            yield return new WaitForSeconds(0.45f);
            yield return Ads.Interstitial();
            if (LevelData.HasNext)
            {
                Load(LevelData.Index + 1);
                yield break;
            }
            _busy = false;
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
        }

        void Undo()
        {
            if (_busy || _locked.Count > 0 || _undo.Count == 0) return;
            _board = _undo.Pop();
            _sel = -1;
            _won = _board.Won;
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

        static void HudLayout(out float scale, out float top, out float bot, out Rect undo, out Rect restart, out Rect hive)
        {
            scale = Mathf.Max(Screen.height / 720f, 1f);
            var safe = Screen.safeArea;
            float safeTop = Screen.height - safe.yMax;
            float safeBot = safe.yMin;
            top = 72f * scale + safeTop;
            bot = 72f * scale + safeBot;
            float y = Screen.height - bot - 8f;
            float h = 56f * scale;
            float x = Mathf.Max(16f, safe.xMin + 8f);
            undo = new Rect(x, y, 140f * scale, h);
            restart = new Rect(x + 140f * scale + 4f, y, 150f * scale, h);
            float hiveW = 170f * scale;
            hive = new Rect(Mathf.Min(Screen.width - hiveW - 16f, safe.xMax - hiveW - 8f), y, hiveW, h);
        }

        bool HitHud(Vector2 screen)
        {
            HudLayout(out _, out float top, out _, out var undo, out var restart, out var hive);
            float gy = Screen.height - screen.y;
            var gui = new Vector2(screen.x, gy);
            if (_album)
            {
                _album = false;
                return true;
            }
            if (gui.y < top) return true;
            if (undo.Contains(gui))
            {
                Undo();
                return true;
            }
            if (restart.Contains(gui))
            {
                Restart();
                return true;
            }
            if (hive.Contains(gui))
            {
                _album = !_album;
                return true;
            }
            HudLayout(out _, out _, out float bot, out _, out _, out _);
            return gui.y > Screen.height - bot - 8f;
        }

        void OnGUI()
        {
            if (_board == null) return;
            HudLayout(out float s, out float top, out _, out var undo, out var restart, out var hive);
            var lab = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(22 * s) };
            lab.normal.textColor = Color.white;
            var btn = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(22 * s) };
            GUILayout.BeginArea(new Rect(16, Mathf.Max(8f, Screen.height - Screen.safeArea.yMax), Screen.width - 32, top));
            string lv = LevelData.Current != null
                ? LevelData.Current.Number + "  " + LevelData.Current.Title + "    "
                : "";
            GUILayout.Label("FLOCK FIVE    " + lv + _board.RemainingBirds + " birds", lab);
            GUILayout.EndArea();
            var was = GUI.enabled;
            GUI.enabled = !_busy && _locked.Count == 0 && _undo.Count > 0;
            GUI.Box(undo, "Undo", btn);
            GUI.enabled = !_busy;
            GUI.Box(restart, "Restart", btn);
            GUI.Box(hive, "Hive " + Hive.Found + "/" + Hive.Kinds, btn);
            GUI.enabled = was;
            if (_album) DrawAlbum(s);
        }

        void DrawAlbum(float s)
        {
            var box = new Rect(20f * s, 90f * s, Screen.width - 40f * s, Mathf.Min(520f * s, Screen.height * 0.58f));
            GUI.Box(box, "");
            var title = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(24 * s), fontStyle = FontStyle.Bold };
            title.normal.textColor = new Color(1f, 0.92f, 0.7f);
            var row = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(20 * s) };
            GUILayout.BeginArea(new Rect(box.x + 16f, box.y + 10f, box.width - 32f, box.height - 20f));
            GUILayout.Label("Your hive  " + Hive.Found + " of " + Hive.Kinds + "  ·  " + Hive.Visitors + " visits", title);
            GUILayout.Space(8f);
            for (int i = 0; i < Hive.Kinds; i++)
            {
                int n = Hive.CountOf(i);
                row.normal.textColor = n > 0 ? Hive.Roster[i].Tint : new Color(0.55f, 0.5f, 0.42f, 0.7f);
                string line = n > 0
                    ? Hive.Roster[i].Name + (n > 1 ? "  ×" + n : "")
                    : "—  still out in the garden";
                GUILayout.Label(line, row);
            }
            GUILayout.EndArea();
        }
    }
}
