using UnityEngine;

namespace FlockFive
{
    // One showcase burst: sparks, a short flame, smoke, and ember debris.
    // Built once, stepped in place, then destroyed. The bolt, flash, and
    // shake stay on GardenStorm. The limb drop stays on BranchView.BreakAway.
    public sealed class StrikeFx : MonoBehaviour
    {
        const int N = 26;
        const float Dur = 2.4f;

        struct Bit
        {
            public Transform T;
            public SpriteRenderer Sr;
            public float VX, VY, Spin, Life;
            public byte Kind;
        }

        Bit[] _bits;
        float _age;

        public static void Play(Transform root, Vector3 at)
        {
            if (root == null) return;
            var go = new GameObject("StrikeFx");
            go.transform.SetParent(root, false);
            var fx = go.AddComponent<StrikeFx>();
            fx.Build(at);
        }

        void Build(Vector3 at)
        {
            _bits = new Bit[N];
            for (int i = 0; i < N; i++)
            {
                byte kind = i < 7 ? (byte)0 : i < 15 ? (byte)1 : i < 18 ? (byte)2 : (byte)3;
                float scale = kind == 2 ? Random.Range(0.42f, 0.7f)
                    : kind == 1 ? Random.Range(0.32f, 0.56f)
                    : kind == 0 ? Random.Range(0.08f, 0.16f)
                    : Random.Range(0.14f, 0.24f);
                int order = kind == 0 ? 19 : kind == 2 ? 12 : 15;
                var go = WorldBuilder.Sprite("Ember" + i, SpriteCatalog.Glow, at, scale, order, transform);
                var sr = go.GetComponent<SpriteRenderer>();
                var bit = new Bit();
                bit.T = go.transform;
                bit.Sr = sr;
                bit.Kind = kind;
                bit.Life = Random.Range(0.9f, 1.8f);
                float ang = Random.Range(0f, 6.28f);
                if (kind == 0)
                {
                    float sp = Random.Range(2.4f, 4.6f);
                    bit.VX = Mathf.Cos(ang) * sp;
                    bit.VY = Mathf.Sin(ang) * sp * 0.7f + 1.2f;
                    bit.Spin = Random.Range(-8f, 8f);
                    sr.color = new Color(1f, 0.97f, 0.86f, 1f);
                }
                else if (kind == 1)
                {
                    bit.VX = Random.Range(-0.35f, 0.35f);
                    bit.VY = Random.Range(0.7f, 1.5f);
                    bit.Spin = Random.Range(-1.5f, 1.5f);
                    sr.color = new Color(1f, 0.55f, 0.16f, 0.9f);
                }
                else if (kind == 2)
                {
                    bit.VX = Random.Range(-0.25f, 0.25f);
                    bit.VY = Random.Range(0.35f, 0.7f);
                    bit.Spin = Random.Range(-0.6f, 0.6f);
                    sr.color = new Color(0.45f, 0.42f, 0.4f, 0.38f);
                }
                else
                {
                    bit.VX = Random.Range(-1.1f, 1.1f);
                    bit.VY = Random.Range(0.4f, 1.6f);
                    bit.Spin = Random.Range(-6f, 6f);
                    sr.color = new Color(1f, 0.42f, 0.1f, 0.95f);
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
            float u = _age / Dur;
            for (int i = 0; i < _bits.Length; i++)
            {
                var bit = _bits[i];
                if (bit.T == null) continue;
                float life = _age / bit.Life;
                if (bit.Kind == 3) bit.VY -= 4.2f * dt;
                else if (bit.Kind == 0) bit.VY -= 1.4f * dt;
                bit.T.position += new Vector3(bit.VX, bit.VY, 0f) * dt;
                bit.T.Rotate(0f, 0f, bit.Spin * dt * 40f);
                var c = bit.Sr != null ? bit.Sr.color : Color.white;
                float a = 1f - Mathf.Clamp01(life);
                if (bit.Kind == 2) a *= 0.45f;
                if (bit.Kind == 1) a *= 0.85f;
                c.a = a < 0f ? 0f : a;
                if (bit.Sr != null) bit.Sr.color = c;
                _bits[i] = bit;
            }
            if (u >= 1f) Destroy(gameObject);
        }
    }
}
