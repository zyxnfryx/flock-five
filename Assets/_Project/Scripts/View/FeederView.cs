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
        bool _held;
        float _gust;
        float _spin;

        void Awake() => _planted = transform.position;

        public void Show(BirdColor? color)
        {
            if (Art == null) return;
            if (color == null)
            {
                Art.enabled = false;
                return;
            }
            Art.enabled = true;
            Art.sprite = SpriteCatalog.Feeder(color.Value);
            if (!_held) Art.color = Color.white;
        }

        public Vector3 Mouth => transform.position + new Vector3(0f, -1.08f, 0f);

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
            _held = true;
            Sfx.FeederDone();
            float t = 0f;
            var start = transform.position;
            var want = start + new Vector3(0f, 2.8f, 0f);
            while (t < 0.45f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, t / 0.45f);
                transform.position = Vector3.Lerp(start, want, u);
                transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(u * Mathf.PI) * 12f);
                transform.localScale = Vector3.one * (Scale * (1f + u * 0.22f));
                if (Art != null)
                {
                    var c = Art.color;
                    c.a = 1f - u;
                    Art.color = c;
                }
                yield return null;
            }
            if (Art != null) Art.enabled = false;
            transform.position = _planted;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one * Scale;
            if (Art != null) Art.color = Color.white;
            _held = false;
        }

        void LateUpdate()
        {
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
