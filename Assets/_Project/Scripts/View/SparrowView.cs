using System.Collections;
using UnityEngine;

namespace FlockFive
{
    // Garden pest: flies in, perches on one feeder (blocking it), stays until
    // collect-evicted with five enjoyable smacks. Never sets _busy.
    public sealed class SparrowView : MonoBehaviour
    {
        public static SparrowView Live { get; private set; }

        public int BlockingSlot { get; private set; } = -1;
        public bool IsBlocking => Live != null && BlockingSlot >= 0 && !_done;

        const float Scale = 0.78f; // bigger pest than hummingbirds (0.42)
        SpriteRenderer _art;
        bool _done;
        bool _evict;
        bool _fleeing;
        float _exitX;
        Color _tint = new Color(0.58f, 0.52f, 0.46f, 1f);
        float _flap;

        public static IEnumerator Patrol(System.Func<bool> allow, FeederView[] feeders, Transform parent)
        {
            yield return new WaitForSeconds(Random.Range(12f, 22f));
            while (parent != null)
            {
                if (allow != null && allow() && HasEnabled(feeders))
                    yield return Visit(feeders, parent);
                float wait = Random.Range(18f, 35f);
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
            if (parent == null || feeders == null) yield break;
            var picks = Enabled(feeders);
            if (picks.Length == 0) yield break;

            bool fromLeft = Random.value < 0.5f;
            float edgeX = fromLeft ? -7.4f : 7.4f;
            float exitX = -edgeX;
            var start = new Vector3(edgeX, Random.Range(5.6f, 8.6f), 0f);
            var target = picks[Random.Range(0, picks.Length)];
            var mouth = target.Mouth + new Vector3(Random.Range(-0.15f, 0.15f), 0.35f, 0f);

            var go = WorldBuilder.Sprite("Sparrow", SpriteCatalog.Sparrow, start, Scale, 42, parent);
            var view = go.AddComponent<SparrowView>();
            view._art = go.GetComponent<SpriteRenderer>();
            view._art.color = view._tint;
            view._art.flipX = !fromLeft;
            view._exitX = exitX;
            Live = view;

            // Fly in.
            float t = 0f;
            const float inDur = 0.95f;
            bool chirped = false;
            while (t < inDur)
            {
                if (parent == null || go == null) yield break;
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
                    if (go == null) yield break;
                    go.transform.position = target.Mouth + new Vector3(
                        Mathf.Sin(Time.time * 9f) * 0.03f,
                        0.35f + Mathf.Sin(Time.time * 7f) * 0.04f,
                        0f);
                    view.Flap(true);
                    yield return null;
                }

                while (!view._evict)
                {
                    if (parent == null || go == null || target == null) yield break;
                    if (!IsOn(target))
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

            view.BlockingSlot = -1;

            if (view._evict)
            {
                // Collect owns Smack / PanicFlee; wait until retired.
                while (go != null && !view._done)
                    yield return null;
            }
            else if (go != null)
                yield return view.FlyOut(exitX);

            if (Live == view) Live = null;
            if (go != null) Object.Destroy(go);
        }

        public void BeginEvict()
        {
            if (_done || _evict) return;
            _evict = true;
            BlockingSlot = -1;
        }

        // One enjoyable smack: BAM squash + feathers + yell.
        public void Smack()
        {
            if (_done || _fleeing || _art == null) return;
            StartCoroutine(SmackCo());
        }

        IEnumerator SmackCo()
        {
            var pos = transform.position;
            var parent = transform.parent;
            Sfx.SparrowYell();
            if (CamShake.Live != null)
                CamShake.Live.Punch(0.16f, 0.12f, 3.6f, 0.12f);
            SparrowBits.Burst(pos, parent, _tint);

            float bam = 0f;
            const float bamDur = 0.12f;
            var baseScale = Vector3.one * Scale;
            while (bam < bamDur && !_fleeing)
            {
                bam += Time.deltaTime;
                float u = Mathf.Clamp01(bam / bamDur);
                float squash = 1f + 0.5f * Mathf.Sin(u * Mathf.PI);
                transform.localScale = new Vector3(
                    baseScale.x * (1.3f - 0.5f * u),
                    baseScale.y * (0.5f + 0.65f * u),
                    1f) * squash;
                if (_art != null)
                    _art.color = Color.Lerp(Color.white, SpriteCatalog.SparrowIsPlaceholder ? _tint : Color.white, u);
                Flap(true);
                yield return null;
            }
            if (!_fleeing)
                transform.localScale = Vector3.one * Scale;
        }

        // Panic fly-off after five hits (no opening yell — hits already yelled).
        public IEnumerator PanicFlee()
        {
            if (_done || _fleeing) yield break;
            _fleeing = true;
            BlockingSlot = -1;
            var parent = transform.parent;
            var from = transform.position;
            var dest = new Vector3(_exitX, from.y + Random.Range(1.2f, 2.6f), 0f);
            if (_art != null) _art.flipX = dest.x < from.x;
            transform.localScale = Vector3.one * Scale;

            float t = 0f;
            const float fleeDur = 0.48f;
            while (t < fleeDur)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / fleeDur));
                var p = Vector3.Lerp(from, dest, u);
                p.y += Mathf.Sin(u * Mathf.PI) * 2.1f;
                transform.position = p;
                transform.localScale = Vector3.one * (Scale * Mathf.Lerp(1.15f, 0.7f, u));
                transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(u * Mathf.PI * 3f) * 18f * (1f - u));
                Flap(true);
                if (_art != null)
                {
                    var c = _art.color;
                    c.a = 1f - u * 0.15f;
                    _art.color = c;
                }
                yield return null;
            }
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
            _flap += Time.deltaTime * (hard ? 22f : 14f);
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

    // Self-cleaning feather burst so smack/flee FX outlive the sparrow GO.
    sealed class SparrowBits : MonoBehaviour
    {
        public static void Burst(Vector3 pos, Transform parent, Color tint)
        {
            if (parent == null) return;
            var go = new GameObject("SparrowBits");
            go.transform.SetParent(parent, false);
            go.AddComponent<SparrowBits>().StartCoroutine(Run(go, pos, tint));
        }

        static IEnumerator Run(GameObject host, Vector3 pos, Color tint)
        {
            int n = Random.Range(8, 15);
            var bits = new SpriteRenderer[n];
            var vel = new Vector3[n];
            var spin = new float[n];
            var spr = SpriteCatalog.Feather;
            for (int i = 0; i < n; i++)
            {
                float ang = (i / (float)n) * Mathf.PI * 2f + Random.Range(-0.35f, 0.35f);
                float spd = Random.Range(2.8f, 5.4f);
                vel[i] = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang) + 0.55f, 0f) * spd;
                spin[i] = Random.Range(-420f, 420f);
                float size = Random.Range(0.12f, 0.22f);
                var go = WorldBuilder.Sprite("Feather", spr, pos, size, 43, host.transform);
                go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-60f, 60f));
                bits[i] = go.GetComponent<SpriteRenderer>();
                float shade = Random.Range(0.75f, 1.05f);
                bits[i].color = new Color(tint.r * shade, tint.g * shade, tint.b * shade, 0.98f);
            }

            float life = 0.85f;
            float t = 0f;
            while (t < life)
            {
                t += Time.deltaTime;
                float u = t / life;
                for (int i = 0; i < n; i++)
                {
                    if (bits[i] == null) continue;
                    vel[i].y -= 6.2f * Time.deltaTime;
                    bits[i].transform.position += vel[i] * Time.deltaTime;
                    bits[i].transform.Rotate(0f, 0f, spin[i] * Time.deltaTime);
                    var c = bits[i].color;
                    c.a = 0.98f * (1f - u) * (1f - u);
                    bits[i].color = c;
                }
                yield return null;
            }
            Object.Destroy(host);
        }
    }
}
