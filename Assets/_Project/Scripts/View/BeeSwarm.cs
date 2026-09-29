using UnityEngine;

namespace FlockFive
{
    public sealed class BeeSwarm : MonoBehaviour
    {
        const int Count = 9;
        const int Smokes = 3;
        const int Blankets = 4;
        // Follows the exact per-spot seat heights (WorldBuilder.SeatYMain/Gift) of the covered birds.
        float BirdY = 0.38f + BranchView.RestLift;
        const float BeeScale = 0.16f;
        const float BeeHalf = 0.22f;
        const float Clear = 0.68f;

        // ~27% slower than the old cloud orbit, then each bee's own 0.8–1.2.
        const float Pace = 0.73f;

        public float TrunkDir = -1f;
        SpriteRenderer[] _bees;
        SpriteRenderer[] _smoke;
        SpriteRenderer[] _blanket;
        SpriteRenderer _haze;
        float[] _phase;
        float[] _radius;
        float[] _speedMul;
        float[] _liss;
        float[] _wobAmp;
        float[] _wobHz;
        float[] _bobAmp;
        float _deep;
        float _near;
        float _fence;
        int _hidCount;
        readonly float[] _hidX = new float[Blankets];
        bool _on;
        bool _scatter;
        float _scatterT;
        float _nextHum;
        Vector3[] _scatterVel;

        public bool Hidden => _on && !_scatter;

        public void Cover(bool shrouded, Transform[] seats, int lastHid, int occupied)
        {
            int hidCount = shrouded && seats != null && lastHid >= 0 ? lastHid + 1 : 0;
            _hidCount = Mathf.Clamp(hidCount, 0, Blankets);
            if (_hidCount > 0 && seats != null && seats[0] != null && seats[lastHid] != null)
            {
                float trunkX = seats[0].localPosition.x;
                float sumY = 0f; int nY = 0;
                for (int k = 0; k <= lastHid && k < seats.Length; k++)
                    if (seats[k] != null) { sumY += seats[k].localPosition.y; nY++; }
                if (nY > 0) BirdY = sumY / nY + BranchView.RestLift;
                float lastX = seats[lastHid].localPosition.x;
                _deep = trunkX + TrunkDir * 0.22f;
                float aisle = -TrunkDir;
                _near = lastX + aisle * 0.04f;
                int tip = lastHid + 1;
                float tipX = (occupied > tip && tip < seats.Length && seats[tip] != null)
                    ? seats[tip].localPosition.x
                    : lastX + aisle * 0.95f;
                _fence = tipX + TrunkDir * Clear;
                _near = ClampAisle(_near);
                _deep = ClampAisle(_deep);
                for (int i = 0; i < _hidCount; i++)
                    _hidX[i] = ClampAisle(seats[i] != null ? seats[i].localPosition.x : trunkX);
            }
            Ensure();
            if (shrouded)
            {
                bool fresh = !_on;
                _on = true;
                _scatter = false;
                SetCloud(true);
                LayoutCloud(Time.time);
                if (fresh)
                    _nextHum = Time.unscaledTime + Random.Range(0.4f, 1.2f);
                return;
            }
            if (_on && !_scatter)
            {
                _scatter = true;
                _scatterT = 0f;
                Sfx.BeeScatter();
                int n = _bees != null ? _bees.Length : 0;
                for (int i = 0; i < n; i++)
                {
                    float ang = Random.Range(0f, Mathf.PI * 2f);
                    _scatterVel[i] = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang) + 0.4f, 0f) * Random.Range(2.4f, 4.2f);
                }
            }
            else if (!_on)
            {
                HideNow();
            }
        }

        float ClampAisle(float x)
        {
            return TrunkDir < 0f ? Mathf.Min(x, _fence) : Mathf.Max(x, _fence);
        }

        void SetCloud(bool on)
        {
            if (_haze != null) _haze.enabled = on;
            if (_smoke != null)
                for (int i = 0; i < _smoke.Length; i++)
                    if (_smoke[i] != null) _smoke[i].enabled = on;
            if (_blanket != null)
                for (int i = 0; i < _blanket.Length; i++)
                    if (_blanket[i] != null) _blanket[i].enabled = false;
            if (_bees != null)
                for (int i = 0; i < _bees.Length; i++)
                    if (_bees[i] != null) _bees[i].enabled = on;
        }

        void HideNow()
        {
            _on = false;
            _scatter = false;
            SetCloud(false);
        }

        void Ensure()
        {
            if (_bees != null) return;
            _bees = new SpriteRenderer[Count];
            _smoke = new SpriteRenderer[Smokes];
            _blanket = new SpriteRenderer[Blankets];
            _phase = new float[Count];
            _radius = new float[Count];
            _speedMul = new float[Count];
            _liss = new float[Count];
            _wobAmp = new float[Count];
            _wobHz = new float[Count];
            _bobAmp = new float[Count];
            _scatterVel = new Vector3[Count];

            var hazeGo = WorldBuilder.Sprite("Haze", SpriteCatalog.Smoke, transform.position, 1f, 5, transform);
            _haze = hazeGo.GetComponent<SpriteRenderer>();
            _haze.color = new Color(0.08f, 0.06f, 0.04f, 0.35f);
            _haze.enabled = false;

            for (int i = 0; i < Smokes; i++)
            {
                var go = WorldBuilder.Sprite("Smoke" + i, SpriteCatalog.Smoke, transform.position, 1f, 5, transform);
                _smoke[i] = go.GetComponent<SpriteRenderer>();
                _smoke[i].color = new Color(0.07f, 0.05f, 0.04f, 0.4f);
                _smoke[i].enabled = false;
            }

            for (int i = 0; i < Blankets; i++)
            {
                var go = WorldBuilder.Sprite("Blanket" + i, SpriteCatalog.Blanket, transform.position, 1f, 6, transform);
                _blanket[i] = go.GetComponent<SpriteRenderer>();
                _blanket[i].color = new Color(0.06f, 0.05f, 0.04f, 0.55f);
                _blanket[i].enabled = false;
            }

            for (int i = 0; i < Count; i++)
            {
                var go = WorldBuilder.Sprite("Bee" + i, SpriteCatalog.Bee, transform.position, 1f, 8, transform);
                _bees[i] = go.GetComponent<SpriteRenderer>();
                _bees[i].enabled = false;
                _phase[i] = Random.Range(0f, Mathf.PI * 2f);
                float baseR = 0.16f + 0.14f * (i % 5) / 4f;
                _radius[i] = baseR * Random.Range(0.85f, 1.15f);
                _speedMul[i] = Random.Range(0.8f, 1.2f);
                _liss[i] = Random.Range(1.3f, 1.7f);
                _wobAmp[i] = Random.Range(0.08f, 0.18f);
                _wobHz[i] = Random.Range(1.2f, 2.4f);
                _bobAmp[i] = Random.Range(0.04f, 0.08f);
            }
        }

        void LayoutCloud(float t)
        {
            float lo = Mathf.Min(_deep, _near);
            float hi = Mathf.Max(_deep, _near);
            float span = Mathf.Max(0.45f, hi - lo);
            float mid = (_deep + _near) * 0.5f;
            mid = ClampAisle(mid);

            if (_haze != null)
            {
                _haze.enabled = true;
                _haze.color = new Color(0.08f, 0.06f, 0.04f, 0.10f);
                _haze.transform.localPosition = new Vector3(mid, BirdY + 0.06f, 0f);
                _haze.transform.localScale = new Vector3(span / 5.2f, 0.58f, 1f);
            }
            if (_smoke != null)
            {
                for (int i = 0; i < _smoke.Length; i++)
                {
                    if (_smoke[i] == null) continue;
                    float u = _smoke.Length <= 1 ? 0.5f : i / (float)(_smoke.Length - 1);
                    float along = ClampAisle(Mathf.Lerp(_deep, _near, u));
                    float lift = (i - 1) * 0.32f;
                    _smoke[i].transform.localPosition = new Vector3(
                        along,
                        BirdY + lift + 0.06f * Mathf.Sin(t * 1.8f + i),
                        0f);
                    _smoke[i].transform.localScale = new Vector3(0.14f, 0.30f, 1f);
                    _smoke[i].color = new Color(0.07f, 0.05f, 0.04f, 0.12f);
                    _smoke[i].enabled = true;
                }
            }
            if (_blanket != null)
            {
                for (int i = 0; i < Blankets; i++)
                {
                    if (_blanket[i] == null) continue;
                    _blanket[i].enabled = false;
                }
            }
            if (_bees == null) return;
            int nBees = _bees.Length;
            for (int i = 0; i < nBees; i++)
            {
                if (_bees[i] == null) continue;
                if (_phase == null || i >= _phase.Length) continue;
                if (_speedMul == null || i >= _speedMul.Length) continue;
                float rate = (4.4f + i * 0.31f) * Pace * _speedMul[i];
                float ax = t * rate + _phase[i];
                float r = _radius[i];
                float u = nBees <= 1 ? 0.5f : i / (float)(nBees - 1);
                float along = Mathf.Lerp(_deep, _near, u);
                // Lissajous: Y cycles faster than X so the loop is not a circle.
                float ox = Mathf.Cos(ax) * r * 0.55f;
                float oy = Mathf.Sin(ax * _liss[i]) * r * 0.78f;
                float tx = -Mathf.Sin(ax);
                float ty = Mathf.Cos(ax * _liss[i]) * _liss[i];
                float wob = Mathf.Sin(t * _wobHz[i] * Mathf.PI * 2f + _phase[i]) * _wobAmp[i];
                Vector3 wobL = WorldWobble(transform, tx, ty, wob);
                Vector3 bobL = transform.InverseTransformVector(
                    new Vector3(0f, Mathf.Sin(t * 1.15f + _phase[i]) * _bobAmp[i], 0f));
                float x = ClampAisle(along + ox + wobL.x);
                if (TrunkDir < 0f) x = Mathf.Min(x, _fence - BeeHalf);
                else x = Mathf.Max(x, _fence + BeeHalf);
                float laneT = ((i * 3 + 1) % nBees) / (float)Mathf.Max(1, nBees - 1);
                float lane = (laneT * 2f - 1f) * 0.54f;
                float y = BirdY + lane + oy + bobL.y + wobL.y;
                y = Mathf.Clamp(y, BirdY - 0.72f, BirdY + 0.82f);
                _bees[i].transform.localPosition = new Vector3(x, y, 0f);
                _bees[i].flipX = x * TrunkDir < 0f;
                _bees[i].sprite = SpriteCatalog.BeeFrame(t * 18f + _phase[i]);
                _bees[i].color = Color.white;
                _bees[i].sortingOrder = 8;
                float buzz = BeeScale * (1f + 0.08f * Mathf.Sin(t * 42f + _phase[i]));
                _bees[i].transform.localScale = Vector3.one * buzz;
                _bees[i].enabled = true;
            }
        }

        void LateUpdate()
        {
            if (_bees == null) return;
            if (_scatter)
            {
                _scatterT += Time.deltaTime;
                float u = Mathf.Clamp01(_scatterT / 0.5f);
                int n = _bees.Length;
                for (int i = 0; i < n; i++)
                {
                    if (_bees[i] == null) continue;
                    if (_scatterVel != null && i < _scatterVel.Length)
                        _bees[i].transform.localPosition += _scatterVel[i] * Time.deltaTime;
                    var c = _bees[i].color;
                    c.a = 1f - u;
                    _bees[i].color = c;
                }
                FadeCloud(1f - u);
                if (u >= 1f) HideNow();
                return;
            }
            if (!_on) return;
            if (Time.unscaledTime >= _nextHum)
            {
                Sfx.BeeHum();
                _nextHum = Time.unscaledTime + Random.Range(3.2f, 5.4f);
            }
            LayoutCloud(Time.time);
        }

        // World-unit offset along the normal of a local tangent, returned in parent space.
        static Vector3 WorldWobble(Transform host, float tx, float ty, float amp)
        {
            Vector3 tangentW = host.TransformVector(new Vector3(tx, ty, 0f));
            tangentW.z = 0f;
            Vector3 normalW = new Vector3(-tangentW.y, tangentW.x, 0f);
            if (normalW.sqrMagnitude < 1e-8f) normalW = Vector3.up;
            else normalW.Normalize();
            return host.InverseTransformVector(normalW * amp);
        }

        void FadeCloud(float a)
        {
            if (_haze != null)
            {
                var h = _haze.color;
                h.a = 0.12f * a;
                _haze.color = h;
            }
            if (_smoke != null)
            {
                for (int i = 0; i < _smoke.Length; i++)
                {
                    if (_smoke[i] == null) continue;
                    var c = _smoke[i].color;
                    c.a = 0.14f * a;
                    _smoke[i].color = c;
                }
            }
            if (_blanket == null) return;
            for (int i = 0; i < _blanket.Length; i++)
            {
                if (_blanket[i] == null) continue;
                var c = _blanket[i].color;
                c.a = 0.5f * a;
                _blanket[i].color = c;
            }
        }
    }
}
