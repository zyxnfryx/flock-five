using System.Collections;
using UnityEngine;

namespace FlockFive
{
    public sealed class GardenIce : MonoBehaviour
    {
        SpriteRenderer _a;
        SpriteRenderer _b;
        Camera _cam;
        bool _coated;
        Vector3 _fitA = Vector3.one;
        Vector3 _fitB = Vector3.one;

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
            if (_a != null) _a.enabled = false;
            if (_b != null) _b.enabled = false;
            _coated = false;
        }

        void Fit()
        {
            if (_cam == null) return;
            float h = _cam.orthographicSize * 2.24f;
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

        public IEnumerator Coat()
        {
            Fit();
            _coated = true;
            if (_a != null)
            {
                _a.enabled = true;
                _a.color = new Color(1f, 1f, 1f, 0f);
            }
            if (_b != null)
            {
                _b.enabled = true;
                _b.color = new Color(1f, 1f, 1f, 0f);
            }
            Sfx.FeederLeave();
            float t = 0f;
            const float dur = 0.72f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / dur);
                float k = u * u * (3f - 2f * u);
                if (_a != null)
                {
                    _a.color = new Color(1f, 1f, 1f, 0.92f * k);
                    float grow = Mathf.Lerp(1.10f, 1f, k);
                    _a.transform.localScale = _fitA * grow;
                }
                float u2 = Mathf.Clamp01((t - 0.18f) / 0.55f);
                float k2 = u2 * u2 * (3f - 2f * u2);
                if (_b != null)
                {
                    _b.color = new Color(1f, 1f, 1f, 0.78f * k2);
                    float grow = Mathf.Lerp(1.14f, 1f, k2);
                    _b.transform.localScale = _fitB * grow;
                }
                yield return null;
            }
            if (_a != null) { _a.color = Color.white; _a.transform.localScale = _fitA; }
            if (_b != null) { _b.color = new Color(1f, 1f, 1f, 0.78f); _b.transform.localScale = _fitB; }
        }

        public IEnumerator Shatter(Transform world)
        {
            if (!_coated)
            {
                Hide();
                yield break;
            }
            Sfx.Break();
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
            float hh = _cam.orthographicSize * 1.05f;
            float ww = hh * Mathf.Max(0.4f, _cam.aspect);
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
