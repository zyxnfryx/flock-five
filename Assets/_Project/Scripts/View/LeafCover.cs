using UnityEngine;

namespace FlockFive
{
    public sealed class LeafCover : MonoBehaviour
    {
        const int Count = 11;
        const float BirdY = 0.38f + BranchView.RestLift;
        const float LeafScale = 0.55f;

        public float TrunkDir = -1f;
        SpriteRenderer[] _leaves;
        SpriteRenderer[] _vines;
        float[] _phase;
        float[] _tilt;
        float _deep;
        float _near;
        bool _on;
        bool _lift;
        float _liftT;
        Vector3[] _liftVel;

        public bool Locked => _on && !_lift;

        public void Cover(bool locked, Transform[] seats, int occupied)
        {
            if (locked && seats != null && occupied > 0 && seats[0] != null)
            {
                float trunkX = seats[0].localPosition.x;
                int last = occupied - 1;
                float lastX = seats[last] != null ? seats[last].localPosition.x : trunkX;
                _deep = trunkX + TrunkDir * 0.18f;
                _near = lastX - TrunkDir * 0.12f;
            }
            Ensure();
            if (locked)
            {
                _on = true;
                _lift = false;
                SetLeaves(true);
                Layout(Time.time);
                return;
            }
            if (_on && !_lift)
            {
                _lift = true;
                _liftT = 0f;
                int n = _leaves != null ? _leaves.Length : 0;
                for (int i = 0; i < n; i++)
                {
                    float side = (i % 2 == 0) ? -1f : 1f;
                    _liftVel[i] = new Vector3(side * Random.Range(0.4f, 1.4f), Random.Range(1.6f, 3.2f), 0f);
                }
            }
            else if (!_on)
            {
                HideNow();
            }
        }

        void SetLeaves(bool on)
        {
            if (_leaves != null)
                for (int i = 0; i < _leaves.Length; i++)
                    if (_leaves[i] != null) _leaves[i].enabled = on;
            if (_vines != null)
                for (int i = 0; i < _vines.Length; i++)
                    if (_vines[i] != null) _vines[i].enabled = on;
        }

        void HideNow()
        {
            _on = false;
            _lift = false;
            SetLeaves(false);
        }

        void Ensure()
        {
            if (_leaves != null) return;
            _leaves = new SpriteRenderer[Count];
            _vines = new SpriteRenderer[3];
            _phase = new float[Count];
            _tilt = new float[Count];
            _liftVel = new Vector3[Count];

            for (int i = 0; i < _vines.Length; i++)
            {
                var go = WorldBuilder.Sprite("Vine" + i, SpriteCatalog.Vine, transform.position, 0.42f, 9, transform);
                _vines[i] = go.GetComponent<SpriteRenderer>();
                _vines[i].color = new Color(0.28f, 0.46f, 0.22f, 0.92f);
                _vines[i].enabled = false;
            }

            for (int i = 0; i < Count; i++)
            {
                var go = WorldBuilder.Sprite("Leaf" + i, SpriteCatalog.Leaf, transform.position, LeafScale, 10, transform);
                _leaves[i] = go.GetComponent<SpriteRenderer>();
                _leaves[i].color = Color.Lerp(
                    new Color(0.22f, 0.48f, 0.18f, 1f),
                    new Color(0.42f, 0.62f, 0.20f, 1f),
                    (i % 5) / 4f);
                _leaves[i].enabled = false;
                _phase[i] = Random.Range(0f, 40f);
                _tilt[i] = Random.Range(-28f, 28f);
            }
        }

        void Layout(float t)
        {
            float lo = Mathf.Min(_deep, _near);
            float hi = Mathf.Max(_deep, _near);
            float span = Mathf.Max(0.55f, hi - lo);

            if (_vines != null)
            {
                for (int i = 0; i < _vines.Length; i++)
                {
                    if (_vines[i] == null) continue;
                    float u = _vines.Length <= 1 ? 0.5f : i / (float)(_vines.Length - 1);
                    float along = Mathf.Lerp(_deep, _near, u);
                    _vines[i].enabled = true;
                    _vines[i].transform.localPosition = new Vector3(along, BirdY + 0.08f, 0f);
                    _vines[i].transform.localRotation = Quaternion.Euler(0f, 0f, TrunkDir * (12f - 8f * i));
                    _vines[i].transform.localScale = new Vector3(0.38f + 0.08f * i, 0.55f, 1f);
                    _vines[i].sortingOrder = 9;
                }
            }

            if (_leaves == null) return;
            int n = _leaves.Length;
            for (int i = 0; i < n; i++)
            {
                if (_leaves[i] == null) continue;
                float u = n <= 1 ? 0.5f : i / (float)(n - 1);
                float along = Mathf.Lerp(_deep, _near, u);
                float sway = 0.04f * Mathf.Sin(t * 1.6f + _phase[i]);
                float lift = (i % 3 - 1) * 0.16f + 0.03f * Mathf.Sin(t * 2.1f + _phase[i]);
                _leaves[i].enabled = true;
                _leaves[i].transform.localPosition = new Vector3(along + sway, BirdY + lift, 0f);
                _leaves[i].transform.localRotation = Quaternion.Euler(0f, 0f, _tilt[i] + 6f * Mathf.Sin(t * 1.4f + _phase[i]));
                float s = LeafScale * (0.85f + 0.18f * (i % 4) / 3f);
                _leaves[i].transform.localScale = new Vector3(s * (TrunkDir < 0f ? 1f : -1f), s, 1f);
                _leaves[i].sortingOrder = 10 + (i % 3);
                _leaves[i].flipX = i % 2 == 0;
            }
        }

        void LateUpdate()
        {
            if (_leaves == null) return;
            if (_lift)
            {
                _liftT += Time.deltaTime;
                float u = Mathf.Clamp01(_liftT / 0.55f);
                int n = _leaves.Length;
                for (int i = 0; i < n; i++)
                {
                    if (_leaves[i] == null) continue;
                    if (_liftVel != null && i < _liftVel.Length)
                    {
                        _liftVel[i].y -= 6.5f * Time.deltaTime;
                        _leaves[i].transform.localPosition += _liftVel[i] * Time.deltaTime;
                    }
                    var c = _leaves[i].color;
                    c.a = 1f - u;
                    _leaves[i].color = c;
                }
                if (_vines != null)
                {
                    for (int i = 0; i < _vines.Length; i++)
                    {
                        if (_vines[i] == null) continue;
                        var c = _vines[i].color;
                        c.a = 0.92f * (1f - u);
                        _vines[i].color = c;
                    }
                }
                if (u >= 1f) HideNow();
                return;
            }
            if (!_on) return;
            Layout(Time.time);
        }
    }
}
