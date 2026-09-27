using System.Collections.Generic;
using UnityEngine;

namespace FlockFive
{
    // Leaf lock: a curtain of hanging foliage over the whole locked limb.
    // Leaves hang stem-up from a canopy line above the birds' heads, in a back
    // row and a shorter front row (ragged bottom edge), with a crown of small
    // upright leaves along the top and a vine dangling past each end. All of it
    // draws above resting birds (12-14) and below lifted/flying birds (40+),
    // so nothing on the locked limb pokes through.
    public sealed class LeafCover : MonoBehaviour
    {
        // Sprite geometry (fx_leaf / fx_vine import at 256 ppu, center pivot).
        const float LeafStemOff = 380f / 256f;   // stem end below sprite center
        const float LeafLen = 760f / 256f;       // visible stem-to-tip length
        const float VineTopOff = 512f / 256f;    // vine top above sprite center
        // Bird body (220 ppu, BirdScale 0.42, center pivot).
        const float BirdTop = 0.58f;
        const float BirdBottom = 0.84f;
        const float BirdHalfW = 0.72f;

        const float Canopy = BirdTop + 0.36f;     // above the head
        const float BackGap = 0.30f;
        const float CapGap = 0.46f;

        const int OrderVineBack = 15;
        const int OrderBack = 16;
        const int OrderFront = 17;
        const int OrderCap = 18;
        const int OrderVineFront = 19;

        public float TrunkDir = -1f;

        struct Leaf
        {
            public SpriteRenderer Sr;
            public Vector2 Anchor;   // stem attach point (local)
            public float Angle;      // 180 = hanging straight down
            public float Scale;
            public float Phase;
            public float SwayDeg;
            public bool Vine;
            public Color Tint;
        }

        readonly List<Leaf> _items = new List<Leaf>();
        readonly List<SpriteRenderer> _pool = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _vinePool = new List<SpriteRenderer>();
        Vector3[] _liftVel = new Vector3[0];
        float[] _liftSpin = new float[0];
        bool _on;
        bool _lift;
        float _liftT;
        int _sig = int.MinValue;

        public bool Locked => _on && !_lift;

        public void Cover(bool locked, Transform[] seats, int occupied)
        {
            if (locked && seats != null && occupied > 0 && seats[0] != null)
            {
                int sig = occupied * 131 + (TrunkDir < 0f ? 1 : 0);
                for (int k = 0; k < occupied && k < seats.Length; k++)
                    if (seats[k] != null)
                        sig = sig * 31 + Mathf.RoundToInt(seats[k].localPosition.x * 100f) * 7
                                       + Mathf.RoundToInt(seats[k].localPosition.y * 100f);
                if (sig != _sig || !_on || _lift)
                {
                    _sig = sig;
                    Build(seats, occupied);
                }
                _on = true;
                _lift = false;
                Layout(Time.time);
                return;
            }
            if (locked) return;
            if (_on && !_lift)
            {
                _lift = true;
                _liftT = 0f;
                int n = _items.Count;
                if (_liftVel.Length < n) { _liftVel = new Vector3[n]; _liftSpin = new float[n]; }
                for (int i = 0; i < n; i++)
                {
                    float side = Random.value < 0.5f ? -1f : 1f;
                    _liftVel[i] = new Vector3(side * Random.Range(0.4f, 1.6f), Random.Range(1.6f, 3.4f), 0f);
                    _liftSpin[i] = Random.Range(-260f, 260f);
                }
            }
            else if (!_on)
            {
                HideNow();
            }
        }

        void HideNow()
        {
            _on = false;
            _lift = false;
            _sig = int.MinValue;
            for (int i = 0; i < _pool.Count; i++) if (_pool[i] != null) _pool[i].enabled = false;
            for (int i = 0; i < _vinePool.Count; i++) if (_vinePool[i] != null) _vinePool[i].enabled = false;
        }

        // Seat height (local, bird center) at x, interpolated across occupied seats.
        static float BirdYAt(Transform[] seats, int occupied, float x)
        {
            int n = Mathf.Min(occupied, seats.Length);
            float bestLo = float.NegativeInfinity, yLo = 0f, bestHi = float.PositiveInfinity, yHi = 0f;
            bool haveLo = false, haveHi = false;
            for (int k = 0; k < n; k++)
            {
                if (seats[k] == null) continue;
                var p = seats[k].localPosition;
                if (p.x <= x && p.x > bestLo) { bestLo = p.x; yLo = p.y; haveLo = true; }
                if (p.x >= x && p.x < bestHi) { bestHi = p.x; yHi = p.y; haveHi = true; }
            }
            float y;
            if (haveLo && haveHi) y = bestHi - bestLo < 1e-4f ? yLo : Mathf.Lerp(yLo, yHi, (x - bestLo) / (bestHi - bestLo));
            else if (haveLo) y = yLo;
            else if (haveHi) y = yHi;
            else y = 0f;
            return y + BranchView.RestLift;
        }

        void Build(Transform[] seats, int occupied)
        {
            _items.Clear();
            _orders.Clear();
            float trunkX = seats[0].localPosition.x;
            int last = Mathf.Min(occupied, seats.Length) - 1;
            float lastX = seats[last] != null ? seats[last].localPosition.x : trunkX;
            float a = trunkX + TrunkDir * (BirdHalfW - 0.10f);
            float b = lastX - TrunkDir * (BirdHalfW + 0.02f);
            float lo = Mathf.Min(a, b), hi = Mathf.Max(a, b);
            float span = Mathf.Max(0.6f, hi - lo);

            // Stable per-limb randomness so a rebuild doesn't reshuffle the look.
            var rng = new System.Random(_sig);
            float R(float x0, float x1) => x0 + (float)rng.NextDouble() * (x1 - x0);

            // Back row: long leaves, a bit darker for depth.
            int nb = Mathf.Max(3, Mathf.CeilToInt(span / BackGap) + 1);
            for (int i = 0; i < nb; i++)
            {
                float x = lo + span * i / (nb - 1) + R(-0.05f, 0.05f);
                float top = BirdYAt(seats, occupied, x) + Canopy + R(-0.04f, 0.06f);
                float need = top - (BirdYAt(seats, occupied, x) - BirdBottom) + 0.12f;
                float s = Mathf.Max(need / LeafLen, 0.58f) * R(1.0f, 1.14f);
                float g = R(0.66f, 0.80f);
                Add(false, new Vector2(x, top), 180f + R(-12f, 12f), s, OrderBack,
                    new Color(g * 0.92f, g, g * 0.86f, 1f), R(3f, 5f), rng);
            }

            // Front row: offset half a gap, shorter and brighter, so the bottom edge is ragged.
            int nf = nb - 1;
            for (int i = 0; i < nf; i++)
            {
                float x = lo + span * (i + 0.5f) / (nb - 1) + R(-0.06f, 0.06f);
                float top = BirdYAt(seats, occupied, x) + Canopy - R(0.14f, 0.26f);
                float s = R(0.46f, 0.60f);
                float g = R(0.90f, 1.0f);
                Add(false, new Vector2(x, top), 180f + R(-20f, 20f), s, OrderFront,
                    new Color(g, g, g * 0.94f, 1f), R(4f, 7f), rng);
            }

            // Crown: small leaves standing up and fanning outward along the canopy line.
            int nc = Mathf.Max(2, Mathf.CeilToInt(span / CapGap) + 1);
            for (int i = 0; i < nc; i++)
            {
                float u = nc <= 1 ? 0.5f : i / (float)(nc - 1);
                float x = lo + span * u + R(-0.08f, 0.08f);
                float top = BirdYAt(seats, occupied, x) + Canopy - 0.05f;
                float lean = Mathf.Lerp(38f, -38f, u) + R(-14f, 14f);
                float g = R(0.86f, 1.0f);
                Add(false, new Vector2(x, top), lean, R(0.28f, 0.38f), OrderCap,
                    new Color(g * 0.96f, g, g * 0.88f, 1f), R(5f, 9f), rng);
            }

            // Vines: one dangling past each end and one behind the middle.
            float yl = BirdYAt(seats, occupied, lo) + Canopy + 0.04f;
            float yh = BirdYAt(seats, occupied, hi) + Canopy + 0.04f;
            float ym = BirdYAt(seats, occupied, (lo + hi) * 0.5f) + Canopy + 0.04f;
            Add(true, new Vector2(lo - 0.04f, yl), 180f + 8f, 0.40f, OrderVineFront, Color.white, 3f, rng);
            Add(true, new Vector2(hi + 0.04f, yh), 180f - 8f, 0.44f, OrderVineFront, Color.white, 3f, rng);
            Add(true, new Vector2((lo + hi) * 0.5f, ym), 180f, 0.46f, OrderVineBack, new Color(0.8f, 0.84f, 0.8f, 1f), 2f, rng);

            // Bind sprites.
            int leafUsed = 0, vineUsed = 0;
            for (int i = 0; i < _items.Count; i++)
            {
                var it = _items[i];
                it.Sr = it.Vine ? Take(_vinePool, ref vineUsed, true) : Take(_pool, ref leafUsed, false);
                it.Sr.color = it.Tint;
                it.Sr.enabled = true;
                _items[i] = it;
            }
            for (int i = leafUsed; i < _pool.Count; i++) if (_pool[i] != null) _pool[i].enabled = false;
            for (int i = vineUsed; i < _vinePool.Count; i++) if (_vinePool[i] != null) _vinePool[i].enabled = false;
        }

        void Add(bool vine, Vector2 anchor, float angle, float scale, int order, Color tint, float sway, System.Random rng)
        {
            _items.Add(new Leaf
            {
                Anchor = anchor, Angle = angle, Scale = scale, Vine = vine, Tint = tint,
                Phase = (float)rng.NextDouble() * 40f, SwayDeg = sway,
                Sr = null,
            });
            _orders.Add(order);
        }

        readonly List<int> _orders = new List<int>();

        SpriteRenderer Take(List<SpriteRenderer> pool, ref int used, bool vine)
        {
            if (used >= pool.Count)
            {
                var go = WorldBuilder.Sprite(vine ? "Vine" + pool.Count : "Leaf" + pool.Count,
                    vine ? SpriteCatalog.Vine : SpriteCatalog.Leaf, transform.position, 0.5f, OrderBack, transform);
                pool.Add(go.GetComponent<SpriteRenderer>());
            }
            return pool[used++];
        }

        void Layout(float t)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                var it = _items[i];
                if (it.Sr == null) continue;
                float ang = it.Angle + it.SwayDeg * Mathf.Sin(t * 1.3f + it.Phase);
                Place(it, ang, i < _orders.Count ? _orders[i] : OrderBack);
            }
        }

        // Rotate around the stem (leaf) or top (vine) so sway swings from the attach point.
        void Place(Leaf it, float ang, int order)
        {
            var tr = it.Sr.transform;
            float mirror = TrunkDir < 0f ? 1f : -1f;
            var rot = Quaternion.Euler(0f, 0f, ang);
            Vector3 local = it.Vine
                ? new Vector3(0f, VineTopOff * it.Scale, 0f)
                : new Vector3(0f, -LeafStemOff * it.Scale, 0f);
            Vector3 offset = rot * local;
            tr.localPosition = new Vector3(it.Anchor.x - offset.x, it.Anchor.y - offset.y, 0f);
            tr.localRotation = rot;
            tr.localScale = new Vector3(it.Scale * mirror, it.Scale, 1f);
            it.Sr.sortingOrder = order;
        }

        void LateUpdate()
        {
            if (_items.Count == 0) return;
            if (_lift)
            {
                _liftT += Time.deltaTime;
                float u = Mathf.Clamp01(_liftT / 0.6f);
                for (int i = 0; i < _items.Count; i++)
                {
                    var it = _items[i];
                    if (it.Sr == null) continue;
                    if (i < _liftVel.Length)
                    {
                        _liftVel[i].y -= 6.5f * Time.deltaTime;
                        it.Sr.transform.localPosition += _liftVel[i] * Time.deltaTime;
                        it.Sr.transform.localRotation *= Quaternion.Euler(0f, 0f, _liftSpin[i] * Time.deltaTime);
                    }
                    var c = it.Tint;
                    c.a = 1f - u;
                    it.Sr.color = c;
                }
                if (u >= 1f)
                {
                    HideNow();
                    for (int i = 0; i < _items.Count; i++)
                        if (_items[i].Sr != null) _items[i].Sr.color = _items[i].Tint;
                }
                return;
            }
            if (!_on) return;
            Layout(Time.time);
        }
    }
}
