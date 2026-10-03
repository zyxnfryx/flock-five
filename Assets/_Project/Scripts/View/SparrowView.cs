using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FlockFive
{
    // Garden pest: flies in, perches on one feeder (blocking it), stays until
    // collect-evicted by a lifelike five-hit hummingbird scrap. Never sets _busy.
    public sealed class SparrowView : MonoBehaviour
    {
        public static SparrowView Live { get; private set; }

        public int BlockingSlot { get; private set; } = -1;
        public bool IsBlocking => Live != null && BlockingSlot >= 0 && !_done;
        public bool InScrap => _evict || _fleeing;
        public bool Settled { get; private set; }

        const float Scale = 0.78f; // bigger pest than hummingbirds (0.42)
        SpriteRenderer _art;
        bool _done;
        bool _evict;
        bool _fleeing;
        bool _abort;
        float _exitX;
        Color _tint = new Color(0.58f, 0.52f, 0.46f, 1f);
        float _flap;
        Coroutine _hitCo;
        int _hitGen;
        int _hitIndex = -1;

        public static IEnumerator Patrol(System.Func<bool> allow, System.Func<bool> armed, int visits, FeederView[] feeders, Transform parent)
        {
            if (visits <= 0) yield break;
            while (armed != null && !armed())
            {
                if (parent == null) yield break;
                yield return null;
            }
            yield return PlayClock.Wait(Random.Range(12f, 22f));
            int left = visits;
            while (parent != null && left > 0)
            {
                if (PestSchedule.StageFull) yield break;
                if (allow != null && allow() && HasEnabled(feeders) && PestSchedule.TryReserve())
                {
                    yield return Visit(feeders, parent);
                    PestSchedule.NoteGone();
                    left--;
                    if (left <= 0) yield break;
                }
                if (parent == null) yield break;
                yield return null;
            }
        }

        public static IEnumerator Visit(FeederView[] feeders, Transform parent)
        {
            if (parent == null || feeders == null) yield break;
            var picks = Enabled(feeders);
            if (picks.Length == 0) yield break;

            bool fromLeft = Random.value < 0.5f;
            float edgeX = fromLeft ? -7.4f : 7.4f;
            float exitX = -edgeX;
            var start = new Vector3(edgeX, Random.Range(5.6f, 8.6f), 0f);
            var target = picks[Random.Range(0, picks.Length)];
            var mouth = target.Mouth + new Vector3(Random.Range(-0.15f, 0.15f), 0.35f, 0f);

            GameObject go = null;
            SparrowView view = null;
            try
            {
            go = WorldBuilder.Sprite("Sparrow", SpriteCatalog.Sparrow, start, Scale, 42, parent);
            view = go.AddComponent<SparrowView>();
            view._art = go.GetComponent<SpriteRenderer>();
            view._art.color = view._tint;
            view._art.flipX = !fromLeft;
            view._exitX = exitX;
            Live = view;

            // Fly in. Warm the feather pool across these frames, before any hit.
            float t = 0f;
            const float inDur = 0.95f;
            bool chirped = false;
            while (t < inDur)
            {
                if (parent == null || go == null) yield break;
                PestPool.Prewarm(parent, 4);
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / inDur));
                var p = Vector3.Lerp(start, mouth, u);
                p.y += Mathf.Sin(u * Mathf.PI) * 1.35f;
                go.transform.position = p;
                view.Flap(u < 0.92f);
                if (!chirped && u > 0.82f)
                {
                    chirped = true;
                    Sfx.Chirp(BirdColor.Violet);
                }
                yield return null;
            }

            // Land + perch (block feeder) until collect eviction (or feeder vanishes).
            if (go != null && target != null)
            {
                go.transform.position = target.Mouth + new Vector3(0f, 0.35f, 0f);
                view.BlockingSlot = target.Slot;
                target.Poke();

                float settle = 0f;
                while (settle < 0.22f && !view._evict)
                {
                    settle += Time.deltaTime;
                    if (go == null || parent == null) yield break;
                    go.transform.position = target.Mouth + new Vector3(
                        Mathf.Sin(Time.time * 9f) * 0.03f,
                        0.35f + Mathf.Sin(Time.time * 7f) * 0.04f,
                        0f);
                    view.Flap(true);
                    yield return null;
                }
                if (!view._evict)
                    view.Settled = true;

                while (!view._evict)
                {
                    if (parent == null || go == null) yield break;
                    if (target == null || !IsOn(target))
                    {
                        // Feeder vanished — leave quietly.
                        break;
                    }
                    go.transform.position = target.Mouth + new Vector3(
                        Mathf.Sin(Time.time * 6.5f) * 0.045f,
                        0.35f + Mathf.Sin(Time.time * 5.2f) * 0.055f,
                        0f);
                    view.Flap(Mathf.Sin(Time.time * 3.1f) > 0.35f);
                    yield return null;
                }
            }

            if (view == null) yield break;
            view.BlockingSlot = -1;

            if (view._evict)
            {
                // Collect owns TakeHit / PanicFlee; wait until retired.
                while (go != null && !view._done)
                    yield return null;
            }
            else if (go != null)
                yield return view.FlyOut(exitX);
            }
            finally
            {
                if (view != null && Live == view) Live = null;
                if (go != null) Object.Destroy(go);
            }
        }

        // Feathers, cheers, and the hawk pass. Stage load and restart both call this.
        public static void ClearFx()
        {
            PestPool.Clear();
            HawkView.ClearShow();
        }

        public void BeginEvict()
        {
            if (_done || _evict) return;
            _evict = true;
            BlockingSlot = -1;
        }

        // Restart aborted the scrap before PanicFlee. Let Visit destroy this sparrow.
        // A flee already in the air (including the defeat tumble) stops on the next frame.
        public void FinishEvict()
        {
            if (_fleeing) _abort = true;
            if (_evict && !_done && !_fleeing) _done = true;
        }

        // Compat alias — collect dive-strikes call TakeHit.
        public void Smack() => TakeHit(0);

        // Lifelike hit: brief contact squash, crouch→hop flinch, wing flutter, yell, feather puff.
        public void TakeHit(int index = 0)
        {
            if (_art == null) return;
            if (_done || _fleeing)
            {
                AdLog.Add("sparrow hit ignored (defeat)");
                return;
            }
            if (_hitCo != null && _hitIndex == index)
            {
                AdLog.Add("sparrow hit ignored (duplicate)");
                return;
            }
            _hitIndex = index;
            int gen = ++_hitGen;
            if (_hitCo != null) StopCoroutine(_hitCo);
            _hitCo = StartCoroutine(TakeHitCo(index, gen));
        }

        void StopHit()
        {
            _hitGen++;
            if (_hitCo == null) return;
            var co = _hitCo;
            _hitCo = null;
            StopCoroutine(co);
        }

        IEnumerator TakeHitCo(int index, int gen)
        {
            var pos = transform.position;
            var parent = transform.parent;
            var baseScale = Vector3.one * Scale;
            float side = Random.value < 0.5f ? -1f : 1f;
            float hopX = side * Random.Range(0.08f, 0.18f);
            float hopY = Random.Range(0.14f, 0.28f);

            Sfx.SparrowYell();
            if (index == 0 || index == 3)
                Sfx.FeederRattle();
            if (CamShake.Live != null)
                CamShake.Live.Punch(0.10f, 0.07f + 0.01f * index, 2.4f, 0.08f);
            SparrowBits.Burst(pos + new Vector3(0f, 0.08f, 0f), parent, _tint);

            // Contact flash / squash.
            float flash = 0f;
            const float flashDur = 0.055f;
            while (flash < flashDur && !_fleeing)
            {
                flash += Time.deltaTime;
                float u = Mathf.Clamp01(flash / flashDur);
                transform.localScale = new Vector3(
                    baseScale.x * Mathf.Lerp(1.22f, 1.05f, u),
                    baseScale.y * Mathf.Lerp(0.62f, 0.88f, u),
                    1f);
                if (_art != null)
                    _art.color = Color.Lerp(Color.white, SpriteCatalog.SparrowIsPlaceholder ? _tint : Color.white, u);
                Flap(true);
                yield return null;
            }

            // Crouch then recoil hop with wing flutter.
            float crouch = 0f;
            const float crouchDur = 0.07f;
            while (crouch < crouchDur && !_fleeing)
            {
                crouch += Time.deltaTime;
                float u = Mathf.Clamp01(crouch / crouchDur);
                transform.localScale = new Vector3(
                    baseScale.x * Mathf.Lerp(1.08f, 1.18f, u),
                    baseScale.y * Mathf.Lerp(0.78f, 0.58f, u),
                    1f);
                transform.position = pos + new Vector3(0f, -0.04f * u, 0f);
                Flap(true);
                yield return null;
            }

            float hop = 0f;
            const float hopDur = 0.16f;
            float tilt = side * Random.Range(8f, 14f);
            while (hop < hopDur && !_fleeing)
            {
                hop += Time.deltaTime;
                float u = Mathf.Clamp01(hop / hopDur);
                float arc = Mathf.Sin(u * Mathf.PI);
                transform.position = pos + new Vector3(hopX * u, hopY * arc - 0.02f * (1f - arc), 0f);
                transform.localScale = new Vector3(
                    baseScale.x * Mathf.Lerp(1.12f, 0.95f, u),
                    baseScale.y * Mathf.Lerp(0.62f, 1.08f, arc),
                    1f);
                transform.localRotation = Quaternion.Euler(0f, 0f, tilt * arc);
                Flap(true);
                yield return null;
            }

            if (!_fleeing)
            {
                transform.localScale = baseScale;
                transform.localRotation = Quaternion.identity;
                // Settle slightly off perch — Visit loop no longer owns position while _evict.
                transform.position = pos + new Vector3(hopX * 0.45f, 0.02f, 0f);
            }
            if (gen == _hitGen) _hitCo = null;
        }

        // Panic zigzag bolt. defeated: the five-hit clear — hit-stop, then a tumble off.
        // Hawk-boot still uses the zigzag (no cheer).
        public IEnumerator PanicFlee(bool defeated = false)
        {
            if (_done || _fleeing)
            {
                AdLog.Add(defeated ? "sparrow defeat ignored (duplicate)" : "sparrow flee ignored (duplicate)");
                yield break;
            }
            _fleeing = true;
            Settled = false;
            StopHit();
            BlockingSlot = -1;
            if (defeated)
            {
                yield return DefeatArc();
                yield break;
            }
            var from = transform.position;
            float dir = Mathf.Sign(_exitX - from.x);
            if (dir == 0f) dir = _exitX >= 0f ? 1f : -1f;
            var dest = new Vector3(_exitX, from.y + Random.Range(1.6f, 3.2f), 0f);
            transform.localScale = Vector3.one * Scale;
            transform.localRotation = Quaternion.identity;
            if (_art != null) _art.flipX = dest.x < from.x;
            Sfx.FlockFlutter(1);

            // Three zigzag legs — panicked, not a straight arc.
            var a = from + new Vector3(dir * Random.Range(0.55f, 0.95f), Random.Range(0.55f, 1.05f), 0f);
            var b = a + new Vector3(-dir * Random.Range(0.7f, 1.2f), Random.Range(0.45f, 0.95f), 0f);
            var legs = new[] { a, b, dest };
            var durs = new[] { 0.24f, 0.28f, 0.42f };
            var prev = from;
            for (int leg = 0; leg < legs.Length && !_abort; leg++)
            {
                var next = legs[leg];
                if (_art != null) _art.flipX = next.x < prev.x;
                float t = 0f;
                float dur = durs[leg];
                float sway = (leg % 2 == 0 ? 1f : -1f) * Random.Range(0.22f, 0.42f);
                while (t < dur && !_abort)
                {
                    t += Time.deltaTime;
                    float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
                    // Accelerate out of each turn.
                    float ease = u * u * (3f - 2f * u);
                    var p = Vector3.Lerp(prev, next, ease);
                    p.x += Mathf.Sin(u * Mathf.PI) * sway;
                    p.y += Mathf.Sin(u * Mathf.PI) * (0.35f + 0.2f * leg);
                    transform.position = p;
                    transform.localScale = Vector3.one * (Scale * Mathf.Lerp(1.12f, 0.68f, (leg + u) / 3f));
                    transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(u * Mathf.PI * 2.4f) * 22f * (1f - u * 0.5f));
                    Flap(true);
                    if (_art != null)
                    {
                        var c = _art.color;
                        c.a = 1f - ((leg + u) / 3f) * 0.2f;
                        _art.color = c;
                    }
                    yield return null;
                }
                prev = next;
            }
            _done = true;
        }

        // Five-hit clear. Holds one beat, then spins off on a single arc. No yell —
        // the hits already yelled. Flock cheer is one chip, not a second melody.
        IEnumerator DefeatArc()
        {
            var parent = transform.parent;
            var from = transform.position;
            transform.localScale = Vector3.one * (Scale * 1.08f);
            transform.localRotation = Quaternion.identity;
            if (_art != null) _art.color = Color.white;
            if (CamShake.Live != null)
                CamShake.Live.Punch(0.10f, 0.045f, 1.0f, 0.03f);

            float hold = 0f;
            while (hold < 0.06f && !_abort)
            {
                hold += Time.deltaTime;
                if (_art != null) _art.color = Color.white;
                transform.localScale = Vector3.one * (Scale * 1.08f);
                transform.localRotation = Quaternion.identity;
                yield return null;
            }
            if (_abort)
            {
                _done = true;
                yield break;
            }
            SparrowBits.Burst(from + new Vector3(0f, 0.1f, 0f), parent, _tint, 10);

            float dir = Mathf.Sign(_exitX - from.x);
            if (dir == 0f) dir = _exitX >= 0f ? 1f : -1f;
            var dest = new Vector3(_exitX, from.y + Random.Range(1.3f, 2.1f), 0f);
            if (_art != null) _art.flipX = dest.x < from.x;
            float spin = -dir * Random.Range(120f, 160f);
            float t = 0f;
            const float dur = 1.5f;
            while (t < dur && !_abort)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float ease = Mathf.SmoothStep(0f, 1f, u);
                var p = Vector3.Lerp(from, dest, ease);
                p.y += Mathf.Sin(u * Mathf.PI) * 0.85f;
                transform.position = p;
                transform.localRotation = Quaternion.Euler(0f, 0f, spin * u * 0.35f + Mathf.Sin(u * Mathf.PI * 2f) * 12f);
                transform.localScale = Vector3.one * (Scale * Mathf.Lerp(1.04f, 0.94f, u));
                Flap(true);
                if (_art != null)
                {
                    var c = _art.color;
                    c.a = u > 0.86f ? Mathf.Lerp(1f, 0.2f, (u - 0.86f) / 0.14f) : 1f;
                    _art.color = c;
                }
                yield return null;
            }
            if (!_abort)
                PestCheer.Mark(from, parent, false);
            _done = true;
        }

        IEnumerator FlyOut(float exitX)
        {
            _fleeing = true;
            var from = transform.position;
            var dest = new Vector3(exitX, from.y + Random.Range(0.4f, 1.6f), 0f);
            if (_art != null) _art.flipX = dest.x < from.x;
            float t = 0f;
            const float outDur = 0.85f;
            while (t < outDur)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / outDur));
                var p = Vector3.Lerp(from, dest, u);
                p.y += Mathf.Sin(u * Mathf.PI) * 1.2f;
                transform.position = p;
                Flap(true);
                yield return null;
            }
            _done = true;
        }

        void Flap(bool hard)
        {
            if (_art == null) return;
            // Wingbeats per second; poses step at a steady rate so the flap never aliases.
            _flap += Time.deltaTime * (hard ? 4f : 2.6f);
            _art.sprite = SpriteCatalog.SparrowFrame(_flap);
            if (SpriteCatalog.SparrowIsPlaceholder)
                _art.color = _tint;
            else
                _art.color = Color.white;
        }

        void OnDisable()
        {
            if (Live == this) Live = null;
            BlockingSlot = -1;
            Settled = false;
            _done = true;
        }

        static bool HasEnabled(FeederView[] feeders)
        {
            if (feeders == null) return false;
            for (int i = 0; i < feeders.Length; i++)
                if (IsOn(feeders[i])) return true;
            return false;
        }

        static FeederView[] Enabled(FeederView[] feeders)
        {
            int n = 0;
            for (int i = 0; i < feeders.Length; i++)
                if (IsOn(feeders[i])) n++;
            var got = new FeederView[n];
            int w = 0;
            for (int i = 0; i < feeders.Length; i++)
                if (IsOn(feeders[i])) got[w++] = feeders[i];
            return got;
        }

        static bool IsOn(FeederView f) =>
            f != null && f.Art != null && f.Art.enabled;
    }

    // Feather / cheer sprites reused across scraps. Cleared on stage load and restart.
    static class PestPool
    {
        const int Cap = 48;
        const int Warm = 32;
        const int AfterCheerKeep = 16;
        const int MaxLive = 10;
        static readonly List<SpriteRenderer> Free = new List<SpriteRenderer>(Cap);
        static readonly List<IPestBurst> Live = new List<IPestBurst>(MaxLive);
        static Transform Root;
        static bool _trimCheer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Free.Clear();
            Live.Clear();
            Root = null;
            _trimCheer = false;
        }

        // A few disabled feathers ahead of the first hit. Further calls stop at Warm.
        public static void Prewarm(Transform garden, int batch)
        {
            if (garden == null || batch <= 0 || Free.Count >= Warm) return;
            Ensure(garden);
            int n = 0;
            int goal = Mathf.Min(Warm, Cap);
            while (n < batch && Free.Count < goal)
            {
                var go = WorldBuilder.Sprite("Feather", SpriteCatalog.Feather, Vector3.zero, 0.12f, 43, Root);
                var sr = go.GetComponent<SpriteRenderer>();
                sr.enabled = false;
                go.SetActive(false);
                Free.Add(sr);
                n++;
            }
        }

        // Cheer is over. Drop idle sprites once every burst has given them back.
        public static void AfterCheer()
        {
            _trimCheer = true;
            TryTrim();
        }

        public static void Clear()
        {
            for (int i = Live.Count - 1; i >= 0; i--)
            {
                var b = Live[i];
                if (b != null) b.ReleaseAndDie();
            }
            Live.Clear();
            Free.Clear();
            _trimCheer = false;
            if (Root != null) Object.Destroy(Root.gameObject);
            Root = null;
        }

        public static void Watch(IPestBurst burst)
        {
            if (burst == null) return;
            for (int i = Live.Count - 1; i >= 0; i--)
                if (Live[i] == null) Live.RemoveAt(i);
            while (Live.Count >= MaxLive)
            {
                var old = Live[0];
                Live.RemoveAt(0);
                if (old != null) old.ReleaseAndDie();
            }
            Live.Add(burst);
        }

        public static void Forget(IPestBurst burst)
        {
            Live.Remove(burst);
            TryTrim();
        }

        static void TryTrim()
        {
            if (!_trimCheer) return;
            for (int i = 0; i < Live.Count; i++)
                if (Live[i] != null) return;
            _trimCheer = false;
            while (Free.Count > AfterCheerKeep)
            {
                int last = Free.Count - 1;
                var sr = Free[last];
                Free.RemoveAt(last);
                if (sr != null) Object.Destroy(sr.gameObject);
            }
        }

        public static SpriteRenderer Take(Transform garden, string name, Sprite spr, Vector3 pos, float size, int order)
        {
            Ensure(garden);
            SpriteRenderer sr = null;
            while (Free.Count > 0)
            {
                int last = Free.Count - 1;
                sr = Free[last];
                Free.RemoveAt(last);
                if (sr != null) break;
                sr = null;
            }
            if (sr == null)
            {
                var go = WorldBuilder.Sprite(name, spr, pos, size, order, Root);
                sr = go.GetComponent<SpriteRenderer>();
            }
            else
            {
                var t = sr.transform;
                t.SetParent(Root, false);
                t.position = pos;
                t.localScale = Vector3.one * size;
                t.rotation = Quaternion.identity;
                sr.sprite = spr;
                sr.sortingOrder = order;
                sr.gameObject.SetActive(true);
            }
            sr.enabled = true;
            return sr;
        }

        public static void Give(SpriteRenderer sr)
        {
            if (sr == null) return;
            if (Free.Count >= Cap || Root == null)
            {
                Object.Destroy(sr.gameObject);
                return;
            }
            sr.enabled = false;
            sr.gameObject.SetActive(false);
            sr.transform.SetParent(Root, false);
            Free.Add(sr);
        }

        static void Ensure(Transform garden)
        {
            if (Root != null) return;
            var go = new GameObject("PestPool");
            if (garden != null) go.transform.SetParent(garden, false);
            Root = go.transform;
        }
    }

    interface IPestBurst
    {
        void ReleaseAndDie();
    }

    // Self-cleaning feather burst so smack/flee FX outlive the sparrow GO.
    sealed class SparrowBits : MonoBehaviour, IPestBurst
    {
        struct Puff
        {
            public SpriteRenderer Sr;
            public Vector3 Vel;
            public float Spin;
            public float Age;
        }

        const float PuffLife = 0.85f;
        static SparrowBits _burstHost;
        SpriteRenderer[] _bits;
        List<Puff> _puffs;
        bool _dead;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetHost() => _burstHost = null;

        public static void Burst(Vector3 pos, Transform parent, Color tint, int count = 0)
        {
            if (parent == null) return;
            // One host for the scrap. Later hits and the defeat puff join it
            // instead of stacking another feather runner.
            if (_burstHost != null && !_burstHost._dead)
            {
                _burstHost.Spawn(pos, tint, count);
                return;
            }
            var go = new GameObject("SparrowBits");
            go.transform.SetParent(parent, false);
            var bits = go.AddComponent<SparrowBits>();
            _burstHost = bits;
            PestPool.Watch(bits);
            bits.StartCoroutine(bits.Run(pos, tint, count));
        }

        // One drifting feather. Lives on the pool so a pass teardown can reclaim it.
        public static void Drop(Vector3 pos, Transform parent, Color tint)
        {
            if (parent == null) return;
            var go = new GameObject("FeatherDrop");
            go.transform.SetParent(parent, false);
            var bits = go.AddComponent<SparrowBits>();
            PestPool.Watch(bits);
            bits.StartCoroutine(bits.One(pos, tint));
        }

        public void ReleaseAndDie()
        {
            if (_dead) return;
            _dead = true;
            if (_burstHost == this) _burstHost = null;
            StopAllCoroutines();
            Release();
            if (this != null) Object.Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (_burstHost == this) _burstHost = null;
            PestPool.Forget(this);
            Release();
        }

        void Release()
        {
            if (_puffs != null)
            {
                for (int i = 0; i < _puffs.Count; i++)
                    PestPool.Give(_puffs[i].Sr);
                _puffs = null;
            }
            var bits = _bits;
            if (bits == null) return;
            _bits = null;
            for (int i = 0; i < bits.Length; i++)
                PestPool.Give(bits[i]);
        }

        void Spawn(Vector3 pos, Color tint, int count)
        {
            if (_puffs == null) _puffs = new List<Puff>(16);
            bool big = count > 0;
            int n = big ? count : Random.Range(5, 10);
            var spr = SpriteCatalog.Feather;
            var garden = Anchor();
            for (int i = 0; i < n; i++)
            {
                float ang = (i / (float)n) * Mathf.PI * 2f + Random.Range(-0.35f, 0.35f);
                float spd = big ? Random.Range(3.6f, 6.8f) : Random.Range(2.8f, 5.4f);
                float size = big ? Random.Range(0.18f, 0.34f) : Random.Range(0.12f, 0.22f);
                var sr = PestPool.Take(garden, "Feather", spr, pos, size, 43);
                sr.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-60f, 60f));
                float shade = Random.Range(0.75f, 1.05f);
                sr.color = new Color(tint.r * shade, tint.g * shade, tint.b * shade, 0.98f);
                _puffs.Add(new Puff
                {
                    Sr = sr,
                    Vel = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang) + 0.55f, 0f) * spd,
                    Spin = Random.Range(-420f, 420f)
                });
            }
        }

        Transform Anchor()
        {
            var p = transform.parent;
            // Hawk-pass drops parent the runner to the pass. Keep the pool on the garden
            // so clearing the pass does not take the shared feathers with it.
            if (p != null && p.GetComponent<HawkPass>() != null && p.parent != null)
                return p.parent;
            return p;
        }

        IEnumerator One(Vector3 pos, Color tint)
        {
            float size = Random.Range(0.10f, 0.16f);
            var sr = PestPool.Take(Anchor(), "Feather", SpriteCatalog.Feather, pos, size, 37);
            _bits = new SpriteRenderer[] { sr };
            float shade = Random.Range(0.8f, 1f);
            sr.color = new Color(tint.r * shade, tint.g * shade, tint.b * shade, 0.95f);
            var vel = new Vector3(Random.Range(-0.55f, 0.55f), Random.Range(0.35f, 1.15f), 0f);
            float spin = Random.Range(-220f, 220f);
            float life = 0.7f;
            float t = 0f;
            while (t < life && sr != null)
            {
                t += Time.deltaTime;
                vel.y -= 4.4f * Time.deltaTime;
                sr.transform.position += vel * Time.deltaTime;
                sr.transform.Rotate(0f, 0f, spin * Time.deltaTime);
                var c = sr.color;
                c.a = 0.95f * (1f - t / life);
                sr.color = c;
                yield return null;
            }
            ReleaseAndDie();
        }

        IEnumerator Run(Vector3 pos, Color tint, int count)
        {
            Spawn(pos, tint, count);
            while (!_dead)
            {
                float dt = Time.deltaTime;
                int live = 0;
                var puffs = _puffs;
                if (puffs == null) break;
                for (int i = 0; i < puffs.Count; i++)
                {
                    var b = puffs[i];
                    if (b.Sr == null) continue;
                    b.Age += dt;
                    if (b.Age >= PuffLife)
                    {
                        PestPool.Give(b.Sr);
                        b.Sr = null;
                        puffs[i] = b;
                        continue;
                    }
                    live++;
                    float u = b.Age / PuffLife;
                    b.Vel.y -= 6.2f * dt;
                    b.Sr.transform.position += b.Vel * dt;
                    b.Sr.transform.Rotate(0f, 0f, b.Spin * dt);
                    var c = b.Sr.color;
                    c.a = 0.98f * (1f - u) * (1f - u);
                    b.Sr.color = c;
                    puffs[i] = b;
                }
                if (live == 0) break;
                yield return null;
            }
            ReleaseAndDie();
        }
    }

    // Defeat pop: one flock chip, a few coins and sparkles, a hop. No collider.
    static class PestCheer
    {
        public static void Mark(Vector3 pos, Transform parent, bool heavy)
        {
            if (parent == null) return;
            Sfx.Chirp(BirdColor.Gold);
            int coins = heavy ? 7 : 4;
            int sparks = heavy ? 8 : 5;
            var go = new GameObject(heavy ? "HawkCheer" : "SparrowCheer");
            go.transform.SetParent(parent, false);
            go.AddComponent<CheerBits>().Begin(pos, coins, sparks);
            Hops(parent, pos, heavy);
        }

        static readonly List<BirdIdle> IdleBuf = new List<BirdIdle>(64);
        static readonly System.Comparison<BirdIdle> ByX = (a, b) =>
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;
            return a.transform.position.x.CompareTo(b.transform.position.x);
        };

        static void Hops(Transform root, Vector3 at, bool wave)
        {
            if (root == null) return;
            IdleBuf.Clear();
            // Branch seats only. Storm drops and the rest of the garden are not birds.
            // Fighters stay in BranchView.Birds after they are reparented to the garden.
            int children = root.childCount;
            for (int c = 0; c < children; c++)
            {
                var br = root.GetChild(c).GetComponent<BranchView>();
                if (br == null) continue;
                var birds = br.Birds;
                for (int i = 0; i < birds.Length; i++)
                {
                    var sr = birds[i];
                    if (sr == null || !sr.gameObject.activeInHierarchy) continue;
                    var idle = sr.GetComponent<BirdIdle>();
                    if (idle != null) IdleBuf.Add(idle);
                }
            }
            if (wave)
            {
                IdleBuf.Sort(ByX);
                int seat = 0;
                for (int i = 0; i < IdleBuf.Count; i++)
                {
                    var idle = IdleBuf[i];
                    if (idle == null) continue;
                    // Scrappers are Frozen and off the limb. Bounce them now so the
                    // wave on the branches is not still running when they scatter.
                    if (idle.Frozen)
                    {
                        idle.StartCoroutine(AirHop(idle.transform, Random.Range(0f, 0.04f), 0.2f));
                        continue;
                    }
                    idle.HopCheer(seat * 0.042f, 0.24f);
                    seat++;
                }
                return;
            }
            float r2 = 3.8f * 3.8f;
            for (int i = 0; i < IdleBuf.Count; i++)
            {
                var idle = IdleBuf[i];
                if (idle == null) continue;
                var d = idle.transform.position - at;
                d.z = 0f;
                if (d.sqrMagnitude > r2) continue;
                if (idle.Frozen)
                    idle.StartCoroutine(AirHop(idle.transform, Random.Range(0f, 0.05f), 0.18f));
                else
                    idle.HopCheer(Random.Range(0f, 0.05f), 0.2f);
            }
        }

        // Fight birds are Frozen, so BirdIdle will not write their position.
        // If restart or scatter moves the bird, drop the hop and leave their pose alone.
        static IEnumerator AirHop(Transform tr, float delay, float hop)
        {
            if (tr == null) yield break;
            var origin = tr.position;
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (tr == null || (tr.position - origin).sqrMagnitude > 0.0004f) yield break;
            var placed = origin;
            float t = 0f;
            const float dur = 0.22f;
            while (t < dur)
            {
                if (tr == null) yield break;
                if ((tr.position - placed).sqrMagnitude > 0.0004f) yield break;
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                placed = origin + new Vector3(0f, Mathf.Sin(u * Mathf.PI) * hop, 0f);
                tr.position = placed;
                yield return null;
            }
            if (tr != null && (tr.position - placed).sqrMagnitude <= 0.0004f)
                tr.position = origin;
        }
    }

    sealed class CheerBits : MonoBehaviour, IPestBurst
    {
        SpriteRenderer[] _bits;
        bool _dead;

        public void Begin(Vector3 pos, int coins, int sparks)
        {
            PestPool.Watch(this);
            StartCoroutine(Run(pos, coins, sparks));
        }

        public void ReleaseAndDie()
        {
            if (_dead) return;
            _dead = true;
            StopAllCoroutines();
            Release();
            PestPool.AfterCheer();
            if (this != null) Object.Destroy(gameObject);
        }

        void OnDestroy()
        {
            PestPool.Forget(this);
            Release();
        }

        void Release()
        {
            var bits = _bits;
            if (bits == null) return;
            _bits = null;
            for (int i = 0; i < bits.Length; i++)
                PestPool.Give(bits[i]);
        }

        IEnumerator Run(Vector3 pos, int coins, int sparks)
        {
            int n = coins + sparks;
            var bits = new SpriteRenderer[n];
            var vel = new Vector3[n];
            var garden = transform.parent;
            for (int i = 0; i < n; i++)
            {
                bool coin = i < coins;
                float ang = (i / (float)Mathf.Max(1, n)) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
                float spd = Random.Range(1.8f, coin ? 3.4f : 4.2f);
                vel[i] = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang) + 0.7f, 0f) * spd;
                var spr = coin ? SpriteCatalog.Coin : SpriteCatalog.Sparkle;
                float size = coin ? Random.Range(0.16f, 0.26f) : Random.Range(0.1f, 0.18f);
                bits[i] = PestPool.Take(garden, coin ? "Coin" : "Spark", spr, pos, size, coin ? 44 : 45);
                bits[i].color = coin ? new Color(1f, 0.9f, 0.38f, 1f) : Color.white;
            }
            _bits = bits;
            float life = 0.62f;
            float t = 0f;
            while (t < life)
            {
                t += Time.deltaTime;
                float u = t / life;
                for (int i = 0; i < n; i++)
                {
                    if (bits[i] == null) continue;
                    vel[i].y -= 5.4f * Time.deltaTime;
                    bits[i].transform.position += vel[i] * Time.deltaTime;
                    vel[i] *= 0.985f;
                    var c = bits[i].color;
                    c.a = (1f - u) * (1f - u);
                    bits[i].color = c;
                }
                yield return null;
            }
            ReleaseAndDie();
        }
    }
}

