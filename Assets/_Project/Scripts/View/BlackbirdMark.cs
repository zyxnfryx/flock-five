using UnityEngine;

namespace FlockFive
{
    // Original silhouette. Sits on his cap-1 stub. Swoops are visual only.
    public sealed class BlackbirdMark : MonoBehaviour
    {
        public const float TellSec = BlackbirdCue.TellSec;

        SpriteRenderer _body;
        Transform _perch;
        int _edge;
        float _swoop;
        float _tell;
        bool _rattled;
        bool _busy;
        bool _gone;

        public bool Rattled => _rattled;
        public bool Gone => _gone;

        public static BlackbirdMark Attach(Transform root)
        {
            var go = new GameObject("Blackbird");
            go.transform.SetParent(root, false);
            var mark = go.AddComponent<BlackbirdMark>();
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = BlackbirdPaint.Sprite();
            sr.sortingOrder = 14;
            mark._body = sr;
            go.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
            return mark;
        }

        public void Sit(Transform perch)
        {
            _perch = perch;
            _gone = false;
            if (perch != null) transform.position = perch.position + Vector3.up * 0.42f;
        }

        public void SetRattled(bool on) => _rattled = on && !_gone;

        public void BeginTell()
        {
            _tell = BlackbirdCue.TellSec;
            _busy = true;
        }

        public void EndTell() => _busy = false;

        public void Hide()
        {
            _gone = true;
            _rattled = false;
            if (_body != null) _body.enabled = false;
        }

        public void Shown(bool on)
        {
            if (_gone) return;
            if (_body != null) _body.enabled = on;
        }

        void Update()
        {
            if (_gone || _perch == null || _body == null) return;
            var home = _perch.position + Vector3.up * 0.42f;
            if (_tell > 0f)
            {
                _tell -= AdPause.UnscaledDelta;
                float w = 1f + 0.08f * Mathf.Sin(Time.unscaledTime * 28f);
                transform.localScale = new Vector3(0.55f * w, 0.55f / w, 1f);
                transform.position = home + Vector3.up * 0.12f;
                return;
            }
            transform.localScale = new Vector3(0.55f, 0.55f, 1f);
            if (!_rattled || _busy)
            {
                transform.position = home;
                _swoop = 0f;
                return;
            }
            _swoop += AdPause.UnscaledDelta / 0.85f;
            if (_swoop >= 1f)
            {
                _swoop = 0f;
                _edge = (_edge + 1) % 3;
                transform.position = home;
                return;
            }
            var perch = new BlackbirdRival.Point { X = home.x, Y = home.y };
            float halfW = 4.2f;
            var cam = Camera.main;
            if (cam != null && cam.orthographic) halfW = cam.orthographicSize * cam.aspect;
            var p = BlackbirdRival.Flight.Swoop(perch, _edge, _swoop, halfW, halfW * 1.6f);
            transform.position = new Vector3(p.X, p.Y, 0f);
            if (_body != null) _body.flipX = _edge != 1;
        }
    }

    public static class BlackbirdPaint
    {
        static Sprite _sprite;

        public static Sprite Sprite()
        {
            if (_sprite != null) return _sprite;
            const int w = 96;
            const int h = 64;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (x + 0.5f) / w;
                    float ny = (y + 0.5f) / h;
                    bool body = Ellipse(nx, ny, 0.46f, 0.52f, 0.22f, 0.16f);
                    bool head = Ellipse(nx, ny, 0.70f, 0.60f, 0.12f, 0.11f);
                    bool tail = nx < 0.40f && nx > 0.08f && Mathf.Abs(ny - 0.48f) < 0.05f + (0.40f - nx) * 0.15f;
                    bool beak = nx > 0.78f && nx < 0.96f && Mathf.Abs(ny - 0.58f) < 0.035f * (0.96f - nx) / 0.18f + 0.012f;
                    bool eye = Ellipse(nx, ny, 0.74f, 0.66f, 0.025f, 0.028f);
                    Color c = new Color(0f, 0f, 0f, 0f);
                    if (tail) c = new Color(0.08f, 0.07f, 0.09f, 1f);
                    if (body || head) c = new Color(0.06f, 0.05f, 0.07f, 1f);
                    if (beak) c = new Color(0.72f, 0.48f, 0.16f, 1f);
                    if (eye) c = new Color(0.93f, 0.86f, 0.55f, 1f);
                    px[y * w + x] = c;
                }
            }
            tex.SetPixels(px);
            tex.Apply(false, true);
            _sprite = Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.55f, 0.15f), 140f);
            _sprite.name = "blackbird";
            return _sprite;
        }

        static bool Ellipse(float x, float y, float cx, float cy, float rx, float ry)
        {
            float dx = (x - cx) / rx;
            float dy = (y - cy) / ry;
            return dx * dx + dy * dy <= 1f;
        }
    }

    // Short wood on the cap-1 stub, and a burst of the garden sparkle when he bolts.
    public static class BlackbirdPerch
    {
        public static void Stub(BranchView view)
        {
            if (view == null || view.Wood == null) return;
            var s = view.Wood.transform.localScale;
            float sign = s.x < 0f ? -1f : 1f;
            s.x = 0.22f * sign;
            view.Wood.transform.localScale = s;
        }

        public static void Sparks(Transform parent, Vector3 at)
        {
            if (parent == null) return;
            var spark = SpriteCatalog.Sparkle;
            for (int i = 0; i < 6; i++)
            {
                var go = new GameObject("BbSpark");
                go.transform.SetParent(parent, false);
                go.transform.position = at;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = spark;
                sr.sortingOrder = 20;
                sr.color = new Color(1f, 0.92f, 0.55f, 1f);
                float ang = i * Mathf.PI * 2f / 6f;
                go.transform.localScale = Vector3.one * 0.35f;
                var drift = go.AddComponent<BlackbirdSpark>();
                drift.Vel = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * 1.6f;
            }
        }
    }

    public sealed class BlackbirdSpark : MonoBehaviour
    {
        public Vector3 Vel;
        float _life = 0.45f;

        void Update()
        {
            float dt = AdPause.UnscaledDelta;
            _life -= dt;
            transform.position += Vel * dt;
            Vel *= 0.92f;
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                var c = sr.color;
                c.a = Mathf.Clamp01(_life / 0.45f);
                sr.color = c;
            }
            if (_life <= 0f) Destroy(gameObject);
        }
    }
}
