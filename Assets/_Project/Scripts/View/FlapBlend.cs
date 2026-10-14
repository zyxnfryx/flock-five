using UnityEngine;

namespace FlockFive
{
    // Shared smooth wingbeat for a body the caller drives directly. One continuous
    // flap phase (never reset), read through SpriteCatalog.FlapPair, with the next
    // pose crossfaded over the current one by the in-step phase. No hard pose swaps.
    public sealed class FlapBlend
    {
        readonly SpriteRenderer _body;
        readonly SpriteRenderer _next;
        readonly BirdColor _color;
        float _t;

        public FlapBlend(SpriteRenderer body, BirdColor color)
        {
            _body = body;
            _color = color;
            if (body == null) return;
            var go = new GameObject("WingBlend");
            go.transform.SetParent(body.transform, false);
            _next = go.AddComponent<SpriteRenderer>();
            _next.sortingLayerID = body.sortingLayerID;
            Step(0f, 0f);
        }

        // beatsPerSec: full wingbeats per second. SpriteCatalog.FlapStep advances
        // 4 beats per unit of t, so the phase moves at beatsPerSec / 4.
        public void Step(float dt, float beatsPerSec)
        {
            if (_body == null) return;
            _t += Mathf.Max(0f, dt) * Mathf.Max(0f, beatsPerSec) * 0.25f;
            SpriteCatalog.FlapPair(_color, BirdSex.Neutral, _t, true, out var cur, out var nxt, out var ph);
            if (cur != null && _body.sprite != cur) _body.sprite = cur;
            if (_next == null) return;
            _next.sprite = nxt != null ? nxt : cur;
            _next.flipX = _body.flipX;
            _next.sortingOrder = _body.sortingOrder + 1;
            float w = ph * ph * (3f - 2f * ph);
            var c = _body.color;
            c.a *= w;
            _next.color = c;
        }
    }
}
