using UnityEngine;

namespace FlockFive
{
    // One showcase burst: sparks, a short flame, smoke, embers, then the limb
    // itself splitting into glowing chunks. Built once, stepped in place, then
    // destroyed. The bolt, flash, and shake stay on GardenStorm.
    public sealed class StrikeFx : MonoBehaviour
    {
        public const float SplitDelay = 0.18f;
        const int N = 36;
        const float Dur = 2.05f;

        struct Bit
        {
            public Transform T;
            public SpriteRenderer Sr;
            public Vector3 Base;
            public float VX, VY, Spin, Life, Delay;
            public byte Kind;
        }

        Bit[] _bits;
        float _age;
        static readonly System.Collections.Generic.List<StrikeFx> Live = new System.Collections.Generic.List<StrikeFx>();

        public static void Play(Transform root, Vector3 at)
        {
            Play(root, at, null, Vector3.one, false);
        }

        public static void Play(Transform root, Vector3 at, Sprite limb, Vector3 limbScale, bool flip)
        {
            if (root == null) return;
            var go = new GameObject("StrikeFx");
            go.transform.SetParent(root, false);
            var fx = go.AddComponent<StrikeFx>();
            fx.Build(at, limb, limbScale, flip);
        }

        public static void ClearAll()
        {
            for (int i = Live.Count - 1; i >= 0; i--)
            {
                if (Live[i] != null) Destroy(Live[i].gameObject);
            }
            Live.Clear();
        }

        void OnEnable() => Live.Add(this);

        void OnDisable() => Live.Remove(this);

        void Build(Vector3 at, Sprite limb, Vector3 limbScale, bool flip)
        {
            _bits = new Bit[N];
            int debris = 0;
            for (int i = 0; i < N; i++)
            {
                byte kind = i < 10 ? (byte)0 : i < 16 ? (byte)1 : i < 22 ? (byte)2 : i < 30 ? (byte)3 : (byte)4;
                float scale = kind == 2 ? Random.Range(0.55f, 0.95f)
                    : kind == 1 ? Random.Range(0.28f, 0.48f)
                    : kind == 0 ? Random.Range(0.07f, 0.15f)
                    : kind == 3 ? Random.Range(0.09f, 0.18f)
                    : 1f;
                int order = kind == 0 ? 22 : kind == 2 ? 8 : kind == 4 ? 16 : 15;
                var spr = kind == 4 && limb != null ? limb : SpriteCatalog.Glow;
                var go = WorldBuilder.Sprite("Ember" + i, spr, at, scale, order, transform);
                var sr = go.GetComponent<SpriteRenderer>();
                if (kind == 4)
                {
                    if (flip) sr.flipX = true;
                    float u = (debris + 0.5f) / 6f;
                    float span = Mathf.Max(0.9f, limbScale.x);
                    float x = (u - 0.5f) * span * 0.92f;
                    go.transform.position = at + new Vector3(x, 0.02f, 0f);
                    float sx = Mathf.Max(0.18f, limbScale.x * 0.22f);
                    float sy = Mathf.Max(0.22f, limbScale.y * 0.92f);
                    go.transform.localScale = new Vector3(sx, sy, 1f);
                    sr.enabled = false;
                    debris++;
                }
                var bit = new Bit();
                bit.T = go.transform;
                bit.Sr = sr;
                bit.Base = go.transform.localScale;
                bit.Kind = kind;
                bit.Life = kind == 4 ? Random.Range(1.05f, 1.35f) : Random.Range(0.55f, 1.15f);
                bit.Delay = kind == 4 ? SplitDelay : (kind == 2 ? Random.Range(0f, 0.12f) : 0f);
                float ang = Random.Range(0f, 6.28f);
                if (kind == 0)
                {
                    float sp = Random.Range(3.2f, 6.4f);
                    bit.VX = Mathf.Cos(ang) * sp;
                    bit.VY = Mathf.Abs(Mathf.Sin(ang)) * sp * 0.85f + 1.6f;
                    bit.Spin = Random.Range(-10f, 10f);
                    sr.color = new Color(1f, 0.97f, 0.82f, 1f);
                }
                else if (kind == 1)
                {
                    bit.VX = Random.Range(-0.45f, 0.45f);
                    bit.VY = Random.Range(1.1f, 2.2f);
                    bit.Spin = Random.Range(-2f, 2f);
                    sr.color = new Color(1f, 0.48f, 0.08f, 0.95f);
                }
                else if (kind == 2)
                {
                    bit.VX = Random.Range(-0.22f, 0.22f);
                    bit.VY = Random.Range(0.45f, 0.9f);
                    bit.Spin = Random.Range(-0.4f, 0.4f);
                    sr.color = new Color(0.32f, 0.30f, 0.28f, 0f);
                }
                else if (kind == 3)
                {
                    bit.VX = Random.Range(-1.6f, 1.6f);
                    bit.VY = Random.Range(1.2f, 2.8f);
                    bit.Spin = Random.Range(-8f, 8f);
                    sr.color = new Color(1f, 0.55f, 0.12f, 0.95f);
                }
                else
                {
                    float dir = go.transform.position.x >= at.x ? 1f : -1f;
                    bit.VX = dir * Random.Range(0.7f, 1.7f);
                    bit.VY = Random.Range(1.6f, 2.8f);
                    bit.Spin = dir * Random.Range(2.5f, 6.5f);
                    sr.color = new Color(1f, 0.72f, 0.28f, 0f);
                }
                _bits[i] = bit;
            }
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt < 0f) dt = 0f;
            if (dt > 0.05f) dt = 0.05f;
            _age += dt;
            for (int i = 0; i < _bits.Length; i++)
            {
                var bit = _bits[i];
                if (bit.T == null) continue;
                if (_age < bit.Delay)
                {
                    if (bit.Sr != null && bit.Kind != 4) bit.Sr.enabled = bit.Kind != 2;
                    continue;
                }
                float local = _age - bit.Delay;
                float life = local / bit.Life;
                if (bit.Kind == 4) bit.VY -= 6.4f * dt;
                else if (bit.Kind == 3) bit.VY -= 4.6f * dt;
                else if (bit.Kind == 0) bit.VY -= 2.2f * dt;
                bit.T.position += new Vector3(bit.VX, bit.VY, 0f) * dt;
                bit.T.Rotate(0f, 0f, bit.Spin * dt * 40f);
                if (bit.Kind == 2)
                {
                    float grow = 1f + local * 0.55f;
                    bit.T.localScale = new Vector3(bit.Base.x * grow, bit.Base.y * grow, 1f);
                }
                var c = bit.Sr != null ? bit.Sr.color : Color.white;
                float a = 1f - Mathf.Clamp01(life);
                if (bit.Kind == 2) a *= 0.42f;
                if (bit.Kind == 1) a *= 0.9f;
                if (bit.Kind == 4)
                {
                    var hot = new Color(1f, 0.78f, 0.32f, 1f);
                    var ash = new Color(0.16f, 0.07f, 0.04f, 1f);
                    c = Color.Lerp(hot, ash, Mathf.Clamp01(life * 1.15f));
                    a = Mathf.Clamp01(1.15f - life);
                    if (bit.Sr != null && !bit.Sr.enabled) bit.Sr.enabled = true;
                }
                c.a = a < 0f ? 0f : a;
                if (bit.Sr != null) bit.Sr.color = c;
                _bits[i] = bit;
            }
            if (_age >= Dur) Destroy(gameObject);
        }
    }
}
