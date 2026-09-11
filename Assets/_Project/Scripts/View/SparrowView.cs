using System.Collections;
using UnityEngine;

namespace FlockFive
{
    // Placeholder garden pest: flies in, rattles a feeder via Poke(), flies out.
    // Tap while on-screen for BAM + feathers + yell + panicked flee. Never sets _busy.
    public sealed class SparrowView : MonoBehaviour
    {
        public static SparrowView Live { get; private set; }

        const float Scale = 0.78f; // bigger pest than hummingbirds (0.42)
        const float HitPad = 1.15f;

        SpriteRenderer _art;
        bool _scared;
        bool _done;
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
            Live = view;

            // Fly in.
            float t = 0f;
            const float inDur = 0.95f;
            bool chirped = false;
            while (t < inDur)
            {
                if (view._scared) break;
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

            // Hover + poke (skip if scared mid-arrival).
            if (!view._scared && go != null)
            {
                go.transform.position = mouth;
                float hover = 0f;
                const float hoverDur = 0.28f;
                while (hover < hoverDur && !view._scared)
                {
                    hover += Time.deltaTime;
                    go.transform.position = mouth + new Vector3(
                        Mathf.Sin(Time.time * 14f) * 0.04f,
                        Mathf.Sin(Time.time * 11f) * 0.05f,
                        0f);
                    view.Flap(true);
                    yield return null;
                }

                if (!view._scared)
                {
                    target.Poke();
                    if (Random.value < 0.30f)
                    {
                        var other = OtherEnabled(feeders, target);
                        if (other != null)
                        {
                            float stagger = 0f;
                            while (stagger < 0.15f && !view._scared)
                            {
                                stagger += Time.deltaTime;
                                view.Flap(true);
                                yield return null;
                            }
                            if (!view._scared) other.Poke();
                        }
                    }

                    float linger = 0f;
                    while (linger < 0.18f && !view._scared)
                    {
                        linger += Time.deltaTime;
                        view.Flap(true);
                        yield return null;
                    }
                }
            }

            if (view._scared && go != null)
                yield return view.ScareFlee(exitX, parent);
            else if (go != null)
                yield return view.FlyOut(exitX);

            if (Live == view) Live = null;
            if (go != null) Object.Destroy(go);
        }

        public bool TryHit(Vector2 world)
        {
            if (_done || _art == null || !_art.enabled) return false;
            var b = _art.bounds;
            b.Expand(HitPad);
            return b.Contains(new Vector3(world.x, world.y, b.center.z));
        }

        public void Scare()
        {
            if (_done || _scared) return;
            _scared = true;
        }

        IEnumerator ScareFlee(float exitX, Transform parent)
        {
            _done = true;
            var pos = transform.position;
            Sfx.SparrowYell();
            if (CamShake.Live != null)
                CamShake.Live.Punch(0.22f, 0.18f, 4.5f, 0.16f);
            SparrowBits.Burst(pos, parent, _tint);

            // BAM squash / flash.
            float bam = 0f;
            const float bamDur = 0.14f;
            var baseScale = Vector3.one * Scale;
            while (bam < bamDur)
            {
                bam += Time.deltaTime;
                float u = Mathf.Clamp01(bam / bamDur);
                float squash = 1f + 0.55f * Mathf.Sin(u * Mathf.PI);
                transform.localScale = new Vector3(baseScale.x * (1.35f - 0.55f * u), baseScale.y * (0.45f + 0.7f * u), 1f) * squash;
                if (_art != null)
                    _art.color = Color.Lerp(Color.white, _tint, u);
                yield return null;
            }

            // Panicked flee — opposite side, fast, high arc.
            var from = transform.position;
            var dest = new Vector3(exitX, from.y + Random.Range(1.2f, 2.6f), 0f);
            if (_art != null) _art.flipX = dest.x < from.x;
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
        }

        IEnumerator FlyOut(float exitX)
        {
            _done = true;
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

        static FeederView OtherEnabled(FeederView[] feeders, FeederView skip)
        {
            for (int i = 0; i < feeders.Length; i++)
                if (feeders[i] != skip && IsOn(feeders[i])) return feeders[i];
            return null;
        }

        static bool IsOn(FeederView f) =>
            f != null && f.Art != null && f.Art.enabled;
    }

    // Self-cleaning feather burst so scare FX outlive the sparrow GO.
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
