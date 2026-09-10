using UnityEngine;

namespace FlockFive
{
    public sealed class GardenLife : MonoBehaviour
    {
        sealed class Bit
        {
            public Transform T;
            public SpriteRenderer Sr;
            public Vector3 Home;
            public float Phase, Speed, AmpX, AmpY, RotAmp, Spin, Scale, Fall, Planted;
            public int Kind;
            public Color Tint;
        }

        Bit[] _bits;
        float _grow;

        public static GardenLife Attach(Transform root)
        {
            var go = new GameObject("Life");
            go.transform.SetParent(root, false);
            var life = go.AddComponent<GardenLife>();
            life.Build();
            return life;
        }

        void Build()
        {
            var rng = new System.Random(17);
            var list = new System.Collections.Generic.List<Bit>(40);

            for (int k = 0; k < 8; k++)
            {
                float x = Mathf.Lerp(-3.6f, 3.6f, (float)rng.NextDouble());
                float y = Mathf.Lerp(-6.2f, 7.1f, (float)rng.NextDouble());
                bool bug = k < 2;
                var spr = bug ? SpriteCatalog.Firefly : SpriteCatalog.Glow;
                float sc = bug ? 0.09f : Mathf.Lerp(0.12f, 0.22f, (float)rng.NextDouble());
                var tint = Color.Lerp(new Color(1f, 0.92f, 0.55f, 0.45f), new Color(0.72f, 1f, 0.52f, 0.32f), (float)rng.NextDouble());
                var b = Make("Fly" + k, spr, new Vector3(x, y, 0f), sc, 1, tint, 0, rng);
                b.AmpX = 0.35f + 0.5f * (float)rng.NextDouble();
                b.AmpY = 0.28f + 0.42f * (float)rng.NextDouble();
                b.Speed = 0.32f + 0.58f * (float)rng.NextDouble();
                list.Add(b);
            }

            Sprite[] petals = { SpriteCatalog.PetalPink, SpriteCatalog.PetalPeach };
            for (int k = 0; k < 3; k++)
            {
                float x = Mathf.Lerp(-3.8f, 3.8f, (float)rng.NextDouble());
                float y = Mathf.Lerp(-7f, 8f, (float)rng.NextDouble());
                float sc = Mathf.Lerp(0.10f, 0.16f, (float)rng.NextDouble());
                var b = Make("Petal" + k, petals[k % 2], new Vector3(x, y, 0f), sc, 0, new Color(1f, 1f, 1f, 0.42f), 2, rng);
                b.AmpX = 0.45f + 0.5f * (float)rng.NextDouble();
                b.Fall = 0.26f + 0.24f * (float)rng.NextDouble();
                b.Spin = Mathf.Lerp(-38f, 38f, (float)rng.NextDouble());
                b.Speed = 0.4f + 0.4f * (float)rng.NextDouble();
                list.Add(b);
            }

            Vector3[] leafAt =
            {
                new Vector3(-3.85f, -6.55f, 0f),
                new Vector3(3.75f, -6.35f, 0f),
                new Vector3(-4.05f, -4.6f, 0f),
                new Vector3(4.1f, -4.9f, 0f),
                new Vector3(-4.0f, 5.35f, 0f),
                new Vector3(3.95f, 4.7f, 0f)
            };
            float[] leafRot = { 28f, -22f, 12f, -18f, 35f, -30f };
            for (int k = 0; k < leafAt.Length; k++)
            {
                float sc = k < 2 ? 0.62f : 0.48f;
                var b = Make("Leaf" + k, SpriteCatalog.Leaf, leafAt[k], sc, -6, Color.white, 3, rng);
                b.Planted = leafRot[k];
                b.RotAmp = 5.5f + 2f * (float)rng.NextDouble();
                b.Speed = 0.55f + 0.25f * (float)rng.NextDouble();
                if (leafAt[k].x > 0f) b.Sr.flipX = true;
                list.Add(b);
            }

            Vector3[] vineAt =
            {
                new Vector3(-4.15f, 8.05f, 0f),
                new Vector3(-4.28f, 2.85f, 0f),
                new Vector3(4.18f, 8.15f, 0f),
                new Vector3(4.22f, 3.15f, 0f)
            };
            for (int k = 0; k < vineAt.Length; k++)
            {
                var b = Make("Vine" + k, SpriteCatalog.Vine, vineAt[k], 0.58f, -7, Color.white, 4, rng);
                b.Planted = 0f;
                b.RotAmp = 3.8f + 1.5f * (float)rng.NextDouble();
                b.Speed = 0.38f + 0.22f * (float)rng.NextDouble();
                if (vineAt[k].x > 0f) b.Sr.flipX = true;
                list.Add(b);
            }

            for (int k = 0; k < 3; k++)
            {
                float x = Mathf.Lerp(-1.35f, 1.35f, k / 2f);
                var b = Make("Shaft" + k, SpriteCatalog.Glow, new Vector3(x, 3.5f, 0f), 1f, -12,
                    new Color(1f, 0.78f, 0.42f, 0.045f), 5, rng);
                b.T.localScale = new Vector3(0.5f + 0.12f * k, 7.4f, 1f);
                b.Planted = Mathf.Lerp(-8f, 8f, k / 2f);
                b.RotAmp = 2.4f;
                b.Speed = 0.16f + 0.05f * k;
                list.Add(b);
            }

            Vector3[] shootAt =
            {
                new Vector3(-3.55f, -7.15f, 0f),
                new Vector3(3.45f, -7.05f, 0f),
                new Vector3(-4.15f, -5.35f, 0f),
                new Vector3(4.22f, -5.55f, 0f),
                new Vector3(-3.92f, 3.85f, 0f),
                new Vector3(3.88f, 3.55f, 0f)
            };
            for (int k = 0; k < shootAt.Length; k++)
            {
                float sc = 0.22f + 0.06f * (k % 3);
                var b = Make("Shoot" + k, SpriteCatalog.Leaf, shootAt[k], sc, -5, Color.white, 6, rng);
                b.Planted = shootAt[k].x > 0f ? -18f : 18f;
                b.RotAmp = 4.2f + 1.6f * (float)rng.NextDouble();
                b.Speed = 0.48f + 0.22f * (float)rng.NextDouble();
                if (shootAt[k].x > 0f) b.Sr.flipX = true;
                list.Add(b);
            }

            _bits = list.ToArray();
        }

        Bit Make(string name, Sprite spr, Vector3 pos, float scale, int order, Color tint, int kind, System.Random rng)
        {
            var go = WorldBuilder.Sprite(name, spr, pos, scale, order, transform);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.color = tint;
            return new Bit
            {
                T = go.transform,
                Sr = sr,
                Home = pos,
                Phase = (float)rng.NextDouble() * 40f,
                Speed = 0.5f,
                Scale = scale,
                Kind = kind,
                Tint = tint
            };
        }

        void LateUpdate()
        {
            if (_bits == null) return;
            float t = Time.time;
            float dt = Time.deltaTime;
            float want = GardenStorm.Wet > 0.2f ? 1f : 0f;
            _grow = Mathf.MoveTowards(_grow, want, dt / 46f);
            var lush = new Color(0.72f, 1f, 0.68f, 1f);
            for (int i = 0; i < _bits.Length; i++)
            {
                var b = _bits[i];
                if (b.T == null) continue;
                float u = t * b.Speed + b.Phase;
                if (b.Kind == 0)
                {
                    var p = b.Home;
                    p.x += Mathf.Sin(u) * b.AmpX;
                    p.y += Mathf.Cos(u * 0.73f) * b.AmpY;
                    b.T.position = p;
                    float pulse = 0.28f + 0.72f * (0.5f + 0.5f * Mathf.Sin(u * 1.7f));
                    float night = 1f + 1.15f * SkyCycle.Dusk;
                    var c = b.Tint;
                    c.a = Mathf.Clamp01(b.Tint.a * pulse * night);
                    b.Sr.color = c;
                }
                else if (b.Kind == 2)
                {
                    var p = b.T.position;
                    float fall = b.Fall * (1f - 0.35f * _grow);
                    p.y -= fall * dt;
                    p.x = b.Home.x + Mathf.Sin(u) * b.AmpX * (1f + 0.25f * _grow);
                    if (p.y < -8.2f)
                    {
                        p.y = 8.3f;
                        b.Home.x = Random.Range(-3.8f, 3.8f);
                    }
                    b.T.position = p;
                    b.T.localRotation = Quaternion.Euler(0f, 0f, t * b.Spin + b.Phase);
                    float sc = b.Scale * (1f + 0.45f * _grow);
                    b.T.localScale = new Vector3(sc, sc, 1f);
                    var c = b.Tint;
                    c.a = Mathf.Lerp(b.Tint.a, 0.78f, _grow);
                    b.Sr.color = c;
                }
                else if (b.Kind == 3 || b.Kind == 4 || b.Kind == 6)
                {
                    float wind = 1f + 1.15f * GardenStorm.Wet;
                    float open = b.Kind == 6 ? Mathf.Lerp(0.55f, 1f, _grow) : 1f;
                    b.T.localRotation = Quaternion.Euler(0f, 0f, b.Planted * open + Mathf.Sin(u) * b.RotAmp * wind);
                    float swell = 1f + 0.28f * _grow;
                    if (b.Kind == 6) swell = 0.42f + 0.88f * _grow;
                    float sx = b.Scale * swell;
                    float sy = b.Scale * swell;
                    if (b.Kind == 4) sy *= 1f + 0.22f * _grow;
                    b.T.localScale = new Vector3(sx, sy, 1f);
                    b.Sr.color = Color.Lerp(Color.white, lush, 0.45f * _grow);
                }
                else if (b.Kind == 5)
                {
                    b.T.localRotation = Quaternion.Euler(0f, 0f, b.Planted + Mathf.Sin(u) * b.RotAmp);
                    var c = b.Tint;
                    float dusk = SkyCycle.Dusk;
                    c.r = Mathf.Lerp(b.Tint.r, 0.75f, dusk);
                    c.g = Mathf.Lerp(b.Tint.g, 0.82f, dusk);
                    c.b = Mathf.Lerp(b.Tint.b, 1f, dusk);
                    c.a = b.Tint.a * (0.7f + 0.3f * Mathf.Sin(u * 0.7f)) * Mathf.Lerp(1f, 0.55f, dusk);
                    b.Sr.color = c;
                }
            }
        }
    }
}
