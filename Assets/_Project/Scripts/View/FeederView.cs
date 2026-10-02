using System.Collections;
using UnityEngine;

namespace FlockFive
{
    public sealed class FeederView : MonoBehaviour
    {
        public const float Scale = 0.66f;
        public int Slot;
        public SpriteRenderer Art;
        Vector3 _planted;
        Transform _cord;
        bool _held;
        bool _scoring;
        bool _departing;
        bool _arriving;
        BirdColor? _color;
        float _scoreAmp;
        float _gust;
        float _spin;
        SpriteRenderer[] _glint;

        public bool InTransit => _departing || _arriving;

        void Awake() => _planted = transform.position;

        static Sprite _px;
        // Hanging cord from the art's hook up past the real screen top, so the feeder never floats.
        void Start() => FitCord();

        // Safe-area top can arrive a frame late, and it changes with rotation.
        public void Seat(float y)
        {
            if (Mathf.Abs(_planted.y - y) < 0.01f && _cord != null) return;
            _planted = new Vector3(_planted.x, y, _planted.z);
            if (!_held && !_scoring && !_departing)
                transform.position = _planted;
            FitCord();
        }

        void FitCord()
        {
            if (Art == null || Art.sprite == null) return;
            if (_px == null)
            {
                var t = new Texture2D(1, 1) { filterMode = FilterMode.Bilinear };
                t.SetPixel(0, 0, Color.white); t.Apply();
                _px = Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0f), 1f);
            }
            float hookY = Art.sprite.bounds.max.y - 0.06f;            // local units, sprite space
            float worldTop = WorldBuilder.ScreenTop() + 1.5f;         // past the bezel, island included
            float len = (worldTop - _planted.y) / Scale - hookY;
            if (len <= 0.05f) len = 0.05f;
            if (_cord == null)
            {
                var go = new GameObject("Cord");
                go.transform.SetParent(transform, false);
                _cord = go.transform;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _px;
                sr.color = new Color(0.20f, 0.13f, 0.08f, 1f);
                sr.sortingOrder = Art.sortingOrder - 1;
                go.layer = gameObject.layer;
            }
            _cord.localPosition = new Vector3(0f, hookY, 0.01f);
            _cord.localScale = new Vector3(0.05f / Scale, len, 1f);
        }

        public void Show(BirdColor? color, bool glide = true)
        {
            if (Art == null) return;
            if (color == null)
            {
                if (_departing) return;
                _color = null;
                _arriving = false;
                Art.enabled = false;
                ClearGlints();
                return;
            }
            if (_departing) return;
            bool same = _color == color && Art.enabled && !_arriving;
            _color = color;
            Art.sprite = SpriteCatalog.Feeder(color.Value);
            FitCord();
            if (same) return;
            if (_held && !_arriving)
            {
                Art.enabled = true;
                Art.color = Color.white;
                return;
            }
            if (!glide || _scoring)
            {
                _arriving = false;
                Art.enabled = true;
                Art.color = Color.white;
                if (!_held && !_departing)
                {
                    transform.position = _planted;
                    transform.localRotation = Quaternion.identity;
                    transform.localScale = Vector3.one * Scale;
                }
                return;
            }
            StopCoroutine(nameof(Arrive));
            StartCoroutine(Arrive());
        }

        public Vector3 Mouth => transform.position + new Vector3(0f, -1.08f, 0f);

        // Stable aim point so a score arc does not chase sway.
        public Vector3 RestMouth => _planted + new Vector3(0f, -1.08f, 0f);

        public void Hold() => _held = true;

        public void BeginScore()
        {
            if (_departing) return;
            _scoring = true;
            _held = true;
        }

        // One shared impulse. Overlapping arrivals refresh it instead of stacking coroutines.
        public void ScoreTick()
        {
            if (_departing) return;
            _scoring = true;
            _held = true;
            _scoreAmp = 1f;
        }

        public void Poke()
        {
            if (Art == null || !Art.enabled) return;
            if (_held) return;
            StopAllCoroutines();
            StartCoroutine(PokeCo());
        }

        IEnumerator PokeCo()
        {
            _held = true;
            Sfx.FeederRattle();
            var basePos = _planted;
            float t = 0f;
            const float dur = 0.42f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float kick = (1f - u) * (1f - u);
                float wobble = Mathf.Sin(u * Mathf.PI * 7f) * 14f * kick;
                float bob = Mathf.Sin(u * Mathf.PI * 5f) * 0.12f * kick;
                transform.localRotation = Quaternion.Euler(0f, 0f, wobble);
                transform.position = basePos + new Vector3(Mathf.Sin(u * Mathf.PI * 6f) * 0.08f * kick, bob, 0f);
                transform.localScale = Vector3.one * (Scale * (1f + 0.14f * Mathf.Sin(u * Mathf.PI) * kick));
                yield return null;
            }
            transform.position = _planted;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one * Scale;
            _held = false;
        }

        public void Pulse()
        {
            _held = true;
            StartCoroutine(PulseCo());
        }

        IEnumerator PulseCo()
        {
            float t = 0f;
            const float dur = 0.14f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI);
                transform.localScale = Vector3.one * (Scale * (1f + 0.18f * u));
                yield return null;
            }
            transform.localScale = Vector3.one * Scale;
        }

        public IEnumerator Cheer()
        {
            _held = true;
            float t = 0f;
            while (t < 0.55f)
            {
                t += Time.deltaTime;
                float u = Mathf.Sin(t / 0.55f * Mathf.PI);
                transform.localScale = Vector3.one * (Scale * (1f + 0.16f * u));
                transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 14f) * 7f * (1f - t / 0.55f));
                if (Art != null) Art.color = Color.Lerp(Color.white, new Color(1f, 0.95f, 0.55f), u);
                yield return null;
            }
            transform.localScale = Vector3.one * Scale;
            if (Art != null) Art.color = Color.white;
            _held = false;
        }

        public IEnumerator PullAway()
        {
            _departing = true;
            _arriving = false;
            _scoring = false;
            _scoreAmp = 0f;
            _held = true;
            StopCoroutine(nameof(Arrive));
            Sfx.FeederLeave();
            float t = 0f;
            const float dur = 0.82f;
            var start = transform.position;
            var rot0 = transform.localRotation;
            while (t < dur)
            {
                if (GamePause.Paused)
                {
                    yield return null;
                    continue;
                }
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float swing = u < 0.42f ? Mathf.Sin((u / 0.42f) * Mathf.PI * 2.2f) * (1f - u / 0.42f) : 0f;
                float lift = u < 0.38f ? 0f : Mathf.SmoothStep(0f, 1f, (u - 0.38f) / 0.62f);
                lift = lift * lift * (3f - 2f * lift);
                float burst = Mathf.Sin(Mathf.Clamp01(u / 0.38f) * Mathf.PI);
                transform.position = start + new Vector3(swing * 0.16f, lift * 3.4f, 0f);
                transform.localRotation = rot0 * Quaternion.Euler(0f, 0f, swing * 22f);
                transform.localScale = Vector3.one * (Scale * (1f + burst * 0.16f + lift * 0.08f));
                if (Art != null)
                {
                    var glow = Color.Lerp(Color.white, new Color(1f, 0.92f, 0.55f), burst * 0.85f);
                    glow.a = 1f - lift;
                    Art.color = glow;
                }
                yield return null;
            }
            if (Art != null) Art.enabled = false;
            transform.position = _planted;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one * Scale;
            if (Art != null) Art.color = Color.white;
            ClearGlints();
            _held = false;
            _departing = false;
        }

        IEnumerator Arrive()
        {
            _arriving = true;
            _held = true;
            if (Art != null)
            {
                Art.enabled = true;
                Art.color = Color.white;
            }
            var home = _planted;
            var from = home + new Vector3(0f, 3.1f, 0f);
            transform.position = from;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one * (Scale * 0.92f);
            Sfx.FeederArrive();
            BurstGlints();
            float t = 0f;
            // 15% shorter than the old 0.88s drop.
            const float dur = 0.748f;
            while (t < dur)
            {
                if (GamePause.Paused)
                {
                    yield return null;
                    continue;
                }
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float e = 1f - u;
                float drop = 1f - e * e * e;
                float bounce = Mathf.Sin(u * Mathf.PI) * e * 0.22f;
                float sway = Mathf.Sin(u * Mathf.PI * 2.4f) * e * 0.10f;
                transform.position = Vector3.Lerp(from, home, drop) + new Vector3(sway, bounce, 0f);
                transform.localRotation = Quaternion.Euler(0f, 0f, sway * 90f);
                transform.localScale = Vector3.one * (Scale * (0.94f + 0.10f * Mathf.Sin(u * Mathf.PI)));
                TickGlints(1f - u);
                yield return null;
            }
            transform.position = _planted;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one * Scale;
            if (Art != null) Art.color = Color.white;
            ClearGlints();
            _held = false;
            _arriving = false;
        }

        void BurstGlints()
        {
            var spr = SpriteCatalog.Sparkle;
            if (spr == null || Art == null) return;
            if (_glint == null) _glint = new SpriteRenderer[4];
            for (int i = 0; i < _glint.Length; i++)
            {
                if (_glint[i] == null)
                {
                    var go = WorldBuilder.Sprite("FeederGlint" + i, spr, transform.position, 0.2f, 20, transform);
                    _glint[i] = go.GetComponent<SpriteRenderer>();
                }
                float ang = i * (Mathf.PI * 0.5f) + 0.4f;
                _glint[i].transform.localPosition = new Vector3(Mathf.Cos(ang) * 0.35f, 0.4f + Mathf.Sin(ang) * 0.2f, 0f);
                _glint[i].transform.localScale = Vector3.one * 0.22f;
                _glint[i].color = new Color(1f, 0.94f, 0.62f, 0.9f);
                _glint[i].enabled = true;
            }
        }

        void TickGlints(float a)
        {
            if (_glint == null) return;
            for (int i = 0; i < _glint.Length; i++)
            {
                if (_glint[i] == null || !_glint[i].enabled) continue;
                var c = _glint[i].color;
                c.a = 0.85f * Mathf.Clamp01(a);
                _glint[i].color = c;
                _glint[i].transform.localScale = Vector3.one * (0.18f + 0.1f * a);
            }
        }

        void ClearGlints()
        {
            if (_glint == null) return;
            for (int i = 0; i < _glint.Length; i++)
                if (_glint[i] != null) _glint[i].enabled = false;
        }

        public void SnapHome()
        {
            StopAllCoroutines();
            _held = false;
            _scoring = false;
            _departing = false;
            _arriving = false;
            _scoreAmp = 0f;
            ClearGlints();
            transform.position = _planted;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one * Scale;
            if (Art != null) Art.color = Color.white;
        }

        void LateUpdate()
        {
            if (_departing) return;
            if (_scoring)
            {
                _scoreAmp = Mathf.MoveTowards(_scoreAmp, 0f, Time.deltaTime / 0.16f);
                float a = _scoreAmp;
                float wob = Mathf.Sin(Time.time * 32f) * 7f * a;
                float hop = Mathf.Sin(Time.time * 21f) * 0.05f * a;
                float sway = Mathf.Sin(Time.time * 26f) * 0.035f * a;
                var pose = _planted + new Vector3(sway, hop, 0f);
                var rot = Quaternion.Euler(0f, 0f, wob);
                var scale = Vector3.one * (Scale * (1f + 0.07f * a));
                float k = 1f - Mathf.Exp(-16f * Time.deltaTime);
                transform.position = Vector3.Lerp(transform.position, pose, k);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, rot, k);
                transform.localScale = Vector3.Lerp(transform.localScale, scale, k);
                return;
            }
            if (_held) return;
            if (Art == null || !Art.enabled)
            {
                transform.position = _planted;
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one * Scale;
                return;
            }
            float t = Time.time;
            float phase = Slot * 2.15f + 0.35f;
            float lazy = Slot == 0 ? 0.38f : 0.52f;
            float pendulum = Mathf.Sin(t * lazy + phase);
            float wiggle = Mathf.Sin(t * 1.55f + phase * 1.8f);
            float bob = Mathf.Sin(t * 0.82f + phase * 0.6f);
            float figure = Mathf.Sin(t * 0.64f + phase * 0.5f);
            _gust = Mathf.MoveTowards(_gust, 0f, Time.deltaTime * 1.4f);
            if (_gust <= 0.02f && Mathf.Sin(t * 0.21f + phase * 2.4f) > 0.92f)
                _gust = Random.Range(0.55f, 1f);
            float kick = _gust * Mathf.Sin(t * 3.1f + phase);
            float ang = pendulum * 5.4f + wiggle * 2.2f + kick * 6.5f;
            _spin = Mathf.Lerp(_spin, ang, 0.12f);
            transform.localRotation = Quaternion.Euler(0f, 0f, _spin);
            transform.position = _planted + new Vector3(
                pendulum * 0.055f + figure * 0.03f,
                bob * 0.045f + Mathf.Abs(pendulum) * 0.012f,
                0f);
            float breathe = 1f + 0.035f * bob + 0.02f * _gust;
            transform.localScale = Vector3.one * (Scale * breathe);
        }
    }
}
