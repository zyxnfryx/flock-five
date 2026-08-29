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
        string _toast = "Tap a branch with birds, then tap where they should go.";
        readonly Stack<Board> _undo = new Stack<Board>();
        float _nextTap;
        bool _finalePreview;

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

        void Restart()
        {
            _busy = false;
            _won = false;
            _sel = -1;
            _undo.Clear();
            _toast = "Tap a branch with birds, then tap where they should go.";
            if (_garden.Root != null) Destroy(_garden.Root.gameObject);
            if (_garden.Cam != null) Destroy(_garden.Cam.gameObject);
            SpriteCatalog.ForgetBirds();
            _garden = WorldBuilder.Build(transform);
            _board = LevelData.Open(LevelData.Index);
            SyncAll();
            Sfx.GardenWake();
            Shot("flock-capture.png");
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
            _toast = "You raced the sunset.";
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
                _garden.Branches[i].Sync(_board.Branches[i], _board.IsSleeping(i));
                _garden.Branches[i].SetReady(i == _sel);
            }
            for (int i = 0; i < 2; i++)
                _garden.Feeders[i].Show(_board.Live[i]);
        }

        void Update()
        {
            if (!_busy && !_won && SkyCycle.Courtesy != null)
            {
                _toast = SkyCycle.Courtesy;
                SkyCycle.Courtesy = null;
            }
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
            _nextTap = Time.unscaledTime + 0.18f;

            int hit = HitBranch(world);
            if (hit < 0)
            {
                if (_sel >= 0)
                {
                    _garden.Branches[_sel].SetReady(false);
                    _sel = -1;
                    _toast = "Cancelled. Tap a bird or its branch.";
                }
                else _toast = "Tap a bird, or the branch it sits on.";
                return;
            }
            if (_sel < 0)
            {
                if (_board.Branches[hit].Broken || _board.Branches[hit].Empty)
                {
                    _toast = "Empty perch — tap birds, or a matching branch after selecting.";
                    _garden.Branches[hit].Shake();
                    return;
                }
                if (_board.IsSleeping(hit))
                {
                    _garden.Branches[hit].Shake();
                    Sfx.Sleep();
                    _toast = "They're napping until a matching feeder hangs.";
                    return;
                }
                Select(hit);
                return;
            }
            if (hit == _sel)
            {
                _garden.Branches[_sel].SetReady(false);
                _sel = -1;
                _toast = "Cancelled.";
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
                    _toast = "They're napping until a matching feeder hangs.";
                    return;
                }
                Select(hit);
                return;
            }
            Sfx.Deny();
            _garden.Branches[hit].Shake();
            _toast = "That perch can't take this color yet.";
        }

        void Select(int hit)
        {
            if (_sel >= 0 && _sel != hit) _garden.Branches[_sel].SetReady(false);
            _sel = hit;
            _garden.Branches[hit].SetReady(true);
            _toast = "Ready to fly. Tap a matching bird, empty matching perch, or its branch.";
            Sfx.Chirp();
        }

        IEnumerator DoMove(int from, int to)
        {
            _busy = true;
            _garden.Branches[from].SetReady(false);
            _sel = -1;
            if (!_board.CanMove(from, to, out int run))
            {
                Sfx.Deny();
                _garden.Branches[to].Shake();
                _busy = false;
                yield break;
            }

            _undo.Push(_board.Clone());
            int fromCount = _board.Branches[from].Count;
            int toCount = _board.Branches[to].Count;
            int wanted = _board.Branches[from].TipRun();
            _board.TryMove(from, to, out run);
            yield return Hop(from, to, run, fromCount, toCount);
            SyncAll();

            int combo = 0;
            int collect = _board.FindCollect();
            while (collect >= 0)
            {
                combo++;
                yield return Collect(collect, combo);
                collect = _board.FindCollect();
            }
            if (_board.Won)
            {
                _won = true;
                float dusk = SkyCycle.Dusk;
                if (dusk < 0.35f)
                {
                    _toast = combo >= 2 ? "COMBO x" + combo + " — you raced the sunset." : "You raced the sunset.";
                    yield return FinaleShow.Play(_garden, this);
                }
                else if (dusk > 0.62f)
                    _toast = combo >= 2 ? "COMBO x" + combo + " under the moon." : "Every bird found a feeder. The moon kept you company.";
                else
                    _toast = combo >= 2 ? "COMBO x" + combo + "!" : "Every bird found a feeder.";
            }
            else if (combo >= 2)
                _toast = combo == 2
                    ? "COMBO! Two flocks in one move."
                    : "COMBO x" + combo + "!";
            else if (_board.IsSleeping(to) && _board.Branches[to].IsFullMatch(out var nap))
            {
                Sfx.Sleep();
                _toast = nap + " is napping until a matching feeder hangs.";
            }
            else if (_board.JustUnveiled)
            {
                var unveiled = _board.Branches[from];
                int n = unveiled.TipRun();
                if (n > 1 && unveiled.Tip.HasValue)
                    _toast = "The bees flew off. " + n + " " + unveiled.Tip.Value + " birds are in the clear.";
                else
                    _toast = "The bees flew off. A bird is in the clear.";
                _garden.Branches[from].FlutterTip();
                Sfx.Chirp();
            }
            else if (run < wanted)
                _toast = "Only " + run + " could fit on that perch.";
            _busy = false;
        }

        IEnumerator Hop(int from, int to, int run, int fromCount, int toCount)
        {
            var src = _garden.Branches[from];
            var dst = _garden.Branches[to];
            Sfx.Chirp();
            Sfx.Takeoff(run);
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
                movers[i].sortingOrder = 14;
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
            Sfx.Land(run);
        }

        IEnumerator Collect(int branch, int combo = 1)
        {
            var br = _board.Branches[branch];
            br.IsFullMatch(out var col);
            int slot = _board.FeederSlotFor(col);
            var view = _garden.Branches[branch];
            var feeder = slot >= 0 ? _garden.Feeders[slot] : null;
            Vector3 mouth = feeder != null ? feeder.Mouth : view.transform.position + Vector3.up * 3f;
            if (combo >= 2)
            {
                Sfx.Combo(combo);
                CamShake.Combo(combo);
            }
            Sfx.Takeoff(5);
            StartCoroutine(Wow.Burst(view.transform.position + Vector3.up * 0.8f, col, _garden.Root, combo));
            if (feeder != null) StartCoroutine(feeder.Cheer());

            for (int i = 0; i < BranchState.Cap; i++)
            {
                var bird = view.Birds[i];
                if (bird == null) continue;
                var idle = bird.GetComponent<BirdIdle>();
                if (idle != null)
                {
                    idle.Sleeping = false;
                    idle.Frozen = true;
                    idle.Flapping = true;
                }
                bird.transform.SetParent(_garden.Root, true);
                bird.sortingOrder = 16;
                StartCoroutine(FlyTo(bird.transform, mouth, 0.08f * i, 0.52f));
            }
            yield return new WaitForSeconds(1.05f);
            if (feeder != null) yield return feeder.PullAway();
            yield return view.BreakAway();
            _board.ApplyCollect(branch);
            SyncAll();
            _toast = col + " flocked — wow!";
        }

        static IEnumerator FlyTo(Transform tr, Vector3 dest, float delay, float dur)
        {
            yield return new WaitForSeconds(delay);
            Sfx.FlapHard();
            var start = tr.position;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, t / dur);
                var p = Vector3.Lerp(start, dest, u);
                p.y += Mathf.Sin(u * Mathf.PI) * 1.6f;
                tr.position = p;
                tr.localScale = Vector3.Lerp(tr.localScale, Vector3.zero, u * 0.4f);
                yield return null;
            }
            tr.gameObject.SetActive(false);
        }

        void Undo()
        {
            if (_busy || _undo.Count == 0) return;
            _board = _undo.Pop();
            _sel = -1;
            _won = _board.Won;
            _toast = "Undid.";
            for (int i = 0; i < _garden.Branches.Length; i++)
            {
                var v = _garden.Branches[i];
                v.gameObject.SetActive(!_board.Branches[i].Broken);
                v.transform.rotation = Quaternion.identity;
            }
            SyncAll();
        }

        int HitBranch(Vector2 world)
        {
            if (_garden.Branches == null) return -1;
            int idx = -1;
            float bestBird = 0.95f * 0.95f;
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

            float best = 2.15f * 2.15f;
            for (int i = 0; i < _garden.Branches.Length; i++)
            {
                var v = _garden.Branches[i];
                if (v == null || !v.gameObject.activeInHierarchy) continue;
                if (_board.Branches[v.Index].Broken) continue;
                float d = ((Vector2)v.transform.position - world).sqrMagnitude;
                if (d < best) { best = d; idx = v.Index; }
            }
            var overlap = Physics2D.OverlapCircleAll(world, 1.35f);
            for (int i = 0; i < overlap.Length; i++)
            {
                var v = overlap[i].GetComponentInParent<BranchView>();
                if (v == null || _board.Branches[v.Index].Broken) continue;
                float d = ((Vector2)v.transform.position - world).sqrMagnitude;
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

        static void HudLayout(out float scale, out float top, out float bot, out Rect undo, out Rect restart)
        {
            scale = Mathf.Max(Screen.height / 720f, 1f);
            top = 72f * scale;
            bot = 72f * scale;
            float y = Screen.height - bot - 8f;
            float h = 56f * scale;
            undo = new Rect(16f, y, 160f * scale, h);
            restart = new Rect(16f + 160f * scale + 4f, y, 180f * scale, h);
        }

        bool HitHud(Vector2 screen)
        {
            HudLayout(out _, out float top, out _, out var undo, out var restart);
            float gy = Screen.height - screen.y;
            var gui = new Vector2(screen.x, gy);
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
            HudLayout(out _, out _, out float bot, out _, out _);
            return gui.y > Screen.height - bot - 8f;
        }

        void OnGUI()
        {
            if (_board == null) return;
            HudLayout(out float s, out float top, out _, out var undo, out var restart);
            var lab = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(22 * s) };
            lab.normal.textColor = Color.white;
            var btn = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(22 * s) };
            GUILayout.BeginArea(new Rect(16, 8, Screen.width - 32, top));
            string lv = LevelData.Current != null
                ? LevelData.Current.Number + "  " + LevelData.Current.Title + "    "
                : "";
            GUILayout.Label("FLOCK FIVE    " + lv + _board.RemainingBirds + " birds", lab);
            GUILayout.Label(_toast, lab);
            GUILayout.EndArea();
            var was = GUI.enabled;
            GUI.enabled = !_busy && _undo.Count > 0;
            GUI.Box(undo, "Undo", btn);
            GUI.enabled = !_busy;
            GUI.Box(restart, "Restart", btn);
            GUI.enabled = was;
        }
    }
}
