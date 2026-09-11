using System.Collections;
using UnityEngine;

namespace FlockFive
{
    // Garden pest: rarer than sparrow. Perches on a feeder and blocks it until
    // TWO full collect scraps (HitsNeeded=2). First scrap wounds; second flees.
    // Landing boots any live sparrow. Never sets _busy. No tap-scare.
    public sealed class HawkView : MonoBehaviour
    {
        public static HawkView Live { get; private set; }

        public const int HitsNeeded = 2;
        public int HitsTaken { get; private set; }
        public int BlockingSlot { get; private set; } = -1;
        public bool IsBlocking => Live != null && BlockingSlot >= 0 && !_done;

        float _scale = 1.1f;
        SpriteRenderer _art;
        bool _done;
        bool _scrap;   // collect owns pose during dive-fight
        bool _evict;   // final flee — Visit yields to PanicFlee
        bool _fleeing;
        float _exitX;
        Color _tint = new Color(0.42f, 0.30f, 0.20f, 1f);
        float _flap;

        public static IEnumerator Patrol(System.Func<bool> allow, FeederView[] feeders, Transform parent)
        {
            yield return new WaitForSeconds(Random.Range(40f, 70f));
            while (parent != null)
            {
                if (allow != null && allow() && HasEnabled(feeders) && Live == null)
                    yield return Visit(feeders, parent);
                float wait = Random.Range(50f, 90f);
                float t = 0f;
                while (t < wait)
                {
                    if (parent == null) yield break;
                    t += Time.deltaTime;
                    yield return null;
                }
            }
        }

        public static IEnumerator Visit(FeederView[] feeders, Transform parent)
        {
            if (parent == null || feeders == null || Live != null) yield break;
            var picks = Enabled(feeders);
            if (picks.Length == 0) yield break;

            bool fromLeft = Random.value < 0.5f;
            float edgeX = fromLeft ? -7.6f : 7.6f;
            float exitX = -edgeX;
            var start = new Vector3(edgeX, Random.Range(5.8f, 9.0f), 0f);
            var target = picks[Random.Range(0, picks.Length)];
            var mouth = target.Mouth + new Vector3(Random.Range(-0.12f, 0.12f), 0.42f, 0f);

            float scale = Random.Range(1.05f, 1.15f);
            var go = WorldBuilder.Sprite("Hawk", SpriteCatalog.Hawk, start, scale, 43, parent);
            var view = go.AddComponent<HawkView>();
            view._scale = scale;
            view._art = go.GetComponent<SpriteRenderer>();
            view._art.color = SpriteCatalog.HawkIsPlaceholder ? view._tint : Color.white;
            view._art.flipX = !fromLeft;
            view._exitX = exitX;
            Live = view;

            // Fly in.
            float t = 0f;
            const float inDur = 1.05f;
            bool cried = false;
            while (t < inDur)
            {
                if (parent == null || go == null) yield break;
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / inDur));
                var p = Vector3.Lerp(start, mouth, u);
                p.y += Mathf.Sin(u * Mathf.PI) * 1.55f;
                go.transform.position = p;
                view.Flap(u < 0.92f);
                if (!cried && u > 0.78f)
                {
                    cried = true;
                    Sfx.HawkCry();
                }
                yield return null;
            }

            // Land + boot any sparrow (hawk presence clears sparrow).
            BootSparrow();

            if (go != null && target != null)
            {
                go.transform.position = target.Mouth + new Vector3(0f, 0.42f, 0f);
                view.BlockingSlot = target.Slot;
                target.Poke();

                float settle = 0f;
                while (settle < 0.28f && !view._evict)
                {
                    settle += Time.deltaTime;
                    if (go == null) yield break;
                    if (view._scrap) { yield return null; continue; }
                    go.transform.position = target.Mouth + new Vector3(
                        Mathf.Sin(Time.time * 8f) * 0.025f,
                        0.42f + Mathf.Sin(Time.time * 6f) * 0.035f,
                        0f);
                    view.Flap(true);
                    yield return null;
                }

                while (!view._evict)
                {
                    if (parent == null || go == null || target == null) yield break;
                    if (view._scrap)
                    {
                        yield return null;
                        continue;
                    }
                    if (!IsOn(target))
                    {
                        // Feeder vanished — leave quietly.
                        break;
                    }
                    go.transform.position = target.Mouth + new Vector3(
                        Mathf.Sin(Time.time * 5.8f) * 0.04f,
                        0.42f + Mathf.Sin(Time.time * 4.6f) * 0.05f,
                        0f);
                    view.Flap(Mathf.Sin(Time.time * 2.6f) > 0.4f);
                    yield return null;
                }
            }

            if (view._evict)
            {
                // Collect owns PanicFlee; wait until retired.
                while (go != null && !view._done)
                    yield return null;
            }
            else
            {
                view.BlockingSlot = -1;
                if (go != null)
                    yield return view.FlyOut(exitX);
            }

            if (Live == view) Live = null;
            if (go != null) Object.Destroy(go);
        }

        // Collect dive-fight owns pose; BlockingSlot stays set (still blocking).
        public void BeginScrap()
        {
            if (_done || _fleeing) return;
            _scrap = true;
        }

        public void EndScrap()
        {
            _scrap = false;
        }

        // One full collect counted. Returns true when hawk should PanicFlee.
        public bool AbsorbCollect()
        {
            if (_done || _fleeing) return false;
            HitsTaken = Mathf.Min(HitsNeeded, HitsTaken + 1);
            if (HitsTaken >= HitsNeeded)
            {
                _evict = true;
                BlockingSlot = -1;
                return true;
            }
            return false;
        }

        public void Smack() => TakeHit(0);

        public void TakeHit(int index = 0)
        {
            if (_done || _fleeing || _art == null) return;
            StartCoroutine(TakeHitCo(index));
        }

        IEnumerator TakeHitCo(int index)
        {
            var pos = transform.position;
            var parent = transform.parent;
            var baseScale = Vector3.one * _scale;
            float side = Random.value < 0.5f ? -1f : 1f;
            float hopX = side * Random.Range(0.06f, 0.14f);
            float hopY = Random.Range(0.10f, 0.22f);

            Sfx.HawkCry();
            if (index == 0 || index == 3)
                Sfx.FeederRattle();
            if (CamShake.Live != null)
                CamShake.Live.Punch(0.12f, 0.08f + 0.012f * index, 2.6f, 0.09f);
            SparrowBits.Burst(pos + new Vector3(0f, 0.1f, 0f), parent, _tint);

            float flash = 0f;
            const float flashDur = 0.06f;
            while (flash < flashDur && !_fleeing)
            {
                flash += Time.deltaTime;
                float u = Mathf.Clamp01(flash / flashDur);
                transform.localScale = new Vector3(
                    baseScale.x * Mathf.Lerp(1.18f, 1.04f, u),
                    baseScale.y * Mathf.Lerp(0.66f, 0.90f, u),
                    1f);
                if (_art != null)
                    _art.color = Color.Lerp(Color.white, SpriteCatalog.HawkIsPlaceholder ? _tint : Color.white, u);
                Flap(true);
                yield return null;
            }

            float crouch = 0f;
            const float crouchDur = 0.075f;
            while (crouch < crouchDur && !_fleeing)
            {
                crouch += Time.deltaTime;
                float u = Mathf.Clamp01(crouch / crouchDur);
                transform.localScale = new Vector3(
                    baseScale.x * Mathf.Lerp(1.06f, 1.14f, u),
                    baseScale.y * Mathf.Lerp(0.80f, 0.62f, u),
                    1f);
                transform.position = pos + new Vector3(0f, -0.035f * u, 0f);
                Flap(true);
                yield return null;
            }

            float hop = 0f;
            const float hopDur = 0.15f;
            float tilt = side * Random.Range(6f, 12f);
            while (hop < hopDur && !_fleeing)
            {
                hop += Time.deltaTime;
                float u = Mathf.Clamp01(hop / hopDur);
                float arc = Mathf.Sin(u * Mathf.PI);
                transform.position = pos + new Vector3(hopX * u, hopY * arc - 0.015f * (1f - arc), 0f);
                transform.localScale = new Vector3(
                    baseScale.x * Mathf.Lerp(1.10f, 0.96f, u),
                    baseScale.y * Mathf.Lerp(0.64f, 1.06f, arc),
                    1f);
                transform.localRotation = Quaternion.Euler(0f, 0f, tilt * arc);
                Flap(true);
                yield return null;
            }

            if (!_fleeing)
            {
                transform.localScale = baseScale;
                transform.localRotation = Quaternion.identity;
                transform.position = pos + new Vector3(hopX * 0.35f, 0.02f, 0f);
            }
        }

        public IEnumerator PanicFlee()
        {
            if (_done || _fleeing) yield break;
            _fleeing = true;
            _scrap = false;
            BlockingSlot = -1;
            var from = transform.position;
            float dir = Mathf.Sign(_exitX - from.x);
            if (dir == 0f) dir = _exitX >= 0f ? 1f : -1f;
            var dest = new Vector3(_exitX, from.y + Random.Range(1.8f, 3.4f), 0f);
            transform.localScale = Vector3.one * _scale;
            transform.localRotation = Quaternion.identity;
            if (_art != null) _art.flipX = dest.x < from.x;
            Sfx.FlockFlutter(1);
            Sfx.HawkCry();

            var a = from + new Vector3(dir * Random.Range(0.65f, 1.1f), Random.Range(0.6f, 1.15f), 0f);
            var b = a + new Vector3(-dir * Random.Range(0.8f, 1.35f), Random.Range(0.5f, 1.05f), 0f);
            var legs = new[] { a, b, dest };
            var durs = new[] { 0.17f, 0.19f, 0.30f };
            var prev = from;
            for (int leg = 0; leg < legs.Length; leg++)
            {
                var next = legs[leg];
                if (_art != null) _art.flipX = next.x < prev.x;
                float t = 0f;
                float dur = durs[leg];
                float sway = (leg % 2 == 0 ? 1f : -1f) * Random.Range(0.25f, 0.48f);
                while (t < dur)
                {
                    t += Time.deltaTime;
                    float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
                    float ease = u * u * (3f - 2f * u);
                    var p = Vector3.Lerp(prev, next, ease);
                    p.x += Mathf.Sin(u * Mathf.PI) * sway;
                    p.y += Mathf.Sin(u * Mathf.PI) * (0.4f + 0.22f * leg);
                    transform.position = p;
                    transform.localScale = Vector3.one * (_scale * Mathf.Lerp(1.1f, 0.7f, (leg + u) / 3f));
                    transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(u * Mathf.PI * 2.2f) * 18f * (1f - u * 0.5f));
                    Flap(true);
                    if (_art != null)
                    {
                        var c = _art.color;
                        c.a = 1f - ((leg + u) / 3f) * 0.22f;
                        _art.color = c;
                    }
                    yield return null;
                }
                prev = next;
            }
            _done = true;
        }

        IEnumerator FlyOut(float exitX)
        {
            _fleeing = true;
            var from = transform.position;
            var dest = new Vector3(exitX, from.y + Random.Range(0.5f, 1.8f), 0f);
            if (_art != null) _art.flipX = dest.x < from.x;
            float t = 0f;
            const float outDur = 0.95f;
            while (t < outDur)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / outDur));
                var p = Vector3.Lerp(from, dest, u);
                p.y += Mathf.Sin(u * Mathf.PI) * 1.35f;
                transform.position = p;
                Flap(true);
                yield return null;
            }
            _done = true;
        }

        void Flap(bool hard)
        {
            if (_art == null) return;
            _flap += Time.deltaTime * (hard ? 18f : 11f);
            _art.sprite = SpriteCatalog.HawkFrame(_flap);
            if (SpriteCatalog.HawkIsPlaceholder)
                _art.color = _tint;
            else
                _art.color = Color.white;
        }

        void OnDisable()
        {
            if (Live == this) Live = null;
            BlockingSlot = -1;
            _done = true;
        }

        static void BootSparrow()
        {
            var s = SparrowView.Live;
            if (s == null || !s.IsBlocking) return;
            s.BeginEvict();
            s.StartCoroutine(s.PanicFlee());
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
}
