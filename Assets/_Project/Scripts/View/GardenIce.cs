using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FlockFive
{
    public sealed class GardenIce : MonoBehaviour
    {
        SpriteRenderer _a;
        SpriteRenderer _b;
        Camera _cam;
        bool _coated;
        int _coatGen;
        Vector3 _fitA = Vector3.one;
        Vector3 _fitB = Vector3.one;
        readonly List<Creep> _creep = new List<Creep>(24);

        sealed class Creep
        {
            public Transform T;
            public SpriteRenderer Sr;
            public Vector3 From, To;
            public float Delay, Spin, Scale;
        }

        public static GardenIce Attach(Transform garden, Camera cam)
        {
            var go = new GameObject("Ice");
            go.transform.SetParent(cam != null ? cam.transform : garden, false);
            var ice = go.AddComponent<GardenIce>();
            ice._cam = cam;
            ice._a = MakePane(go.transform, SpriteCatalog.IceA, 40, 0f);
            ice._b = MakePane(go.transform, SpriteCatalog.IceB, 41, 3.5f);
            ice.Fit();
            ice.Hide();
            return ice;
        }

        static SpriteRenderer MakePane(Transform parent, Sprite spr, int order, float tilt)
        {
            var go = WorldBuilder.Sprite("Pane", spr, Vector3.zero, 1f, order, parent);
            go.transform.localPosition = new Vector3(0f, 0f, 8.4f);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, tilt);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.color = new Color(1f, 1f, 1f, 0f);
            return sr;
        }

        void Hide()
        {
            _coatGen++;
            ClearCreep();
            if (_a != null) _a.enabled = false;
            if (_b != null) _b.enabled = false;
            _coated = false;
        }

        void ClearCreep()
        {
            for (int i = 0; i < _creep.Count; i++)
            {
                var bit = _creep[i];
                if (bit != null && bit.T != null) Destroy(bit.T.gameObject);
            }
            _creep.Clear();
        }

        void Fit()
        {
            if (_cam == null) return;
            float h = _cam.orthographicSize * 2.24f * PortraitLock.TallFactor();
            float w = h * Mathf.Max(0.4f, _cam.aspect);
            _fitA = Cover(_a, w, h);
            _fitB = Cover(_b, w * 1.04f, h * 1.04f);
        }

        static Vector3 Cover(SpriteRenderer sr, float w, float h)
        {
            if (sr == null || sr.sprite == null) return Vector3.one;
            var size = sr.sprite.bounds.size;
            float sx = w / Mathf.Max(0.01f, size.x);
            float sy = h / Mathf.Max(0.01f, size.y);
            var s = new Vector3(sx, sy, 1f);
            sr.transform.localScale = s;
            return s;
        }

        // Frost sits in the rim of the pane art. Starting oversized holds that rim
        // off-screen; shrinking pulls it inward until the garden is covered.
        public IEnumerator Coat()
        {
            int gen = ++_coatGen;
            Fit();
            _coated = true;
            ClearCreep();
            SpawnCreep();
            if (_a != null)
            {
                _a.enabled = true;
                _a.color = new Color(1f, 1f, 1f, 0f);
                _a.transform.localScale = _fitA * 1.85f;
            }
            if (_b != null)
            {
                _b.enabled = true;
                _b.color = new Color(1f, 1f, 1f, 0f);
                _b.transform.localScale = _fitB * 2.05f;
            }
            Sfx.FeederLeave();
            float t = 0f;
            const float dur = 1.9f;
            while (t < dur)
            {
                if (gen != _coatGen) yield break;
                if (GamePause.Paused)
                {
                    yield return null;
                    continue;
                }
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / 1.65f);
                float k = u * u * (3f - 2f * u);
                if (_a != null)
                {
                    _a.color = new Color(1f, 1f, 1f, 0.58f * k);
                    _a.transform.localScale = _fitA * Mathf.Lerp(1.85f, 1f, k);
                }
                float u2 = Mathf.Clamp01((t - 0.42f) / 1.35f);
                float k2 = u2 * u2 * (3f - 2f * u2);
                if (_b != null)
                {
                    _b.color = new Color(1f, 1f, 1f, 0.46f * k2);
                    _b.transform.localScale = _fitB * Mathf.Lerp(2.05f, 1f, k2);
                }
                StepCreep(t);
                yield return null;
            }
            if (gen != _coatGen) yield break;
            if (_a != null) { _a.color = new Color(1f, 1f, 1f, 0.58f); _a.transform.localScale = _fitA; }
            if (_b != null) { _b.color = new Color(1f, 1f, 1f, 0.46f); _b.transform.localScale = _fitB; }
            ClearCreep();
        }

        void SpawnCreep()
        {
            var spr = SpriteCatalog.IceShard;
            if (spr == null || _cam == null) return;
            float hh = _cam.orthographicSize * 1.12f * PortraitLock.TallFactor();
            float ww = hh * Mathf.Max(0.4f, _cam.aspect);
            const int n = 22;
            for (int i = 0; i < n; i++)
            {
                float ang = (i / (float)n) * Mathf.PI * 2f + 0.35f;
                float ox = Mathf.Cos(ang);
                float oy = Mathf.Sin(ang);
                var from = new Vector3(ox * ww * 1.18f, oy * hh * 1.18f, 8.15f);
                var to = new Vector3(ox * ww * 0.55f, oy * hh * 0.48f, 8.15f);
                var go = WorldBuilder.Sprite("Creep", spr, transform.position, 0.16f, 42, transform);
                go.transform.localPosition = from;
                go.transform.localRotation = Quaternion.Euler(0f, 0f, ang * Mathf.Rad2Deg);
                var sr = go.GetComponent<SpriteRenderer>();
                sr.color = new Color(0.82f, 0.94f, 1f, 0f);
                _creep.Add(new Creep
                {
                    T = go.transform,
                    Sr = sr,
                    From = from,
                    To = to,
                    Delay = (i % 6) * 0.07f,
                    Spin = (i % 2 == 0 ? 1f : -1f) * (28f + (i % 5) * 11f),
                    Scale = 0.22f + (i % 4) * 0.06f
                });
            }
        }

        void StepCreep(float t)
        {
            for (int i = 0; i < _creep.Count; i++)
            {
                var bit = _creep[i];
                if (bit == null || bit.T == null) continue;
                float u = Mathf.Clamp01((t - bit.Delay) / 0.95f);
                float k = u * u * (3f - 2f * u);
                float fade = 1f;
                if (t > 1.35f) fade = Mathf.Clamp01(1f - (t - 1.35f) / 0.45f);
                bit.T.localPosition = Vector3.Lerp(bit.From, bit.To, k);
                bit.T.localScale = Vector3.one * Mathf.Lerp(0.06f, bit.Scale, k);
                bit.T.Rotate(0f, 0f, bit.Spin * Time.unscaledDeltaTime);
                if (bit.Sr != null)
                    bit.Sr.color = new Color(0.82f, 0.94f, 1f, k * fade);
            }
        }

        public IEnumerator Shatter(Transform world)
        {
            _coatGen++;
            ClearCreep();
            if (!_coated)
            {
                Hide();
                yield break;
            }
            Sfx.Break();
            Haptics.Play(Haptics.Tier.Medium);
            CamShake.Combo(4);
            var parent = world != null ? world : transform;
            SpawnShards(parent);
            if (_a != null) _a.enabled = false;
            yield return new WaitForSeconds(0.12f);
            Sfx.FeederLeave();
            if (_b != null) _b.enabled = false;
            yield return new WaitForSeconds(0.85f);
            Hide();
        }

        void SpawnShards(Transform parent)
        {
            var spr = SpriteCatalog.IceShard;
            if (spr == null || _cam == null) return;
            int n = 28;
            float ww = _cam.orthographicSize * 1.05f * Mathf.Max(0.4f, _cam.aspect);
            float hh = _cam.orthographicSize * 1.05f * PortraitLock.TallFactor();
            var origin = _cam.transform.position + _cam.transform.forward * 9f;
            origin.z = 0f;
            for (int i = 0; i < n; i++)
            {
                float x = Random.Range(-ww, ww);
                float y = Random.Range(-hh, hh);
                var p = origin + new Vector3(x, y, 0f);
                var go = WorldBuilder.Sprite("Shard", spr, p, Random.Range(0.18f, 0.42f), 42, parent);
                var sr = go.GetComponent<SpriteRenderer>();
                sr.color = new Color(0.82f, 0.94f, 1f, 1f);
                go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                var bit = go.AddComponent<IceShard>();
                bit.Vel = new Vector3(x * Random.Range(1.6f, 3.4f), Random.Range(2.2f, 6.4f) + y * 0.35f, 0f);
                bit.Spin = Random.Range(-420f, 420f);
            }
        }
    }

    public sealed class IceShard : MonoBehaviour
    {
        public Vector3 Vel;
        public float Spin;
        float _life = 1.05f;
        SpriteRenderer _sr;

        void Awake() => _sr = GetComponent<SpriteRenderer>();

        void Update()
        {
            float dt = Time.deltaTime;
            _life -= dt;
            Vel.y -= 18f * dt;
            transform.position += Vel * dt;
            transform.Rotate(0f, 0f, Spin * dt);
            if (_sr != null)
            {
                var c = _sr.color;
                c.a = Mathf.Clamp01(_life / 0.45f);
                _sr.color = c;
            }
            if (_life <= 0f) Destroy(gameObject);
        }
    }
}
