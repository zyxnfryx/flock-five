using UnityEngine;

namespace FlockFive
{
    // The limb burns in place, chars from the strike, then cracks. Halves fall
    // charred with ember edges. A jagged stump stays on the trunk. The bolt,
    // sky flash, and shake stay on GardenStorm.
    [DefaultExecutionOrder(9500)]
    public sealed class StrikeFx : MonoBehaviour
    {
        public const float SplitDelay = 1.05f;
        const float Dur = 4.4f;
        const float ShadeHold = 2.2f;
        const float RimRadius = 3.35f;

        struct Bit
        {
            public Transform T;
            public SpriteRenderer Sr;
            public SpriteRenderer Halo;
            public Vector3 Base;
            public Vector3 Home;
            public float VX, VY, Spin, Life, Delay, Along;
            public byte Kind;
        }

        struct Saved
        {
            public SpriteRenderer Sr;
            public Color Color;
        }

        struct Rim
        {
            public SpriteRenderer Body;
            public SpriteRenderer Glow;
            public float K;
        }

        Bit[] _bits;
        Saved[] _shade;
        Rim[] _rims;
        Material[] _chars;
        float _age;
        bool _stumpDetached;
        Vector3 _at;
        Vector3 _axis;
        static readonly System.Collections.Generic.List<StrikeFx> Live = new System.Collections.Generic.List<StrikeFx>();
        static Material _add;

        public static float CharSpread(float age)
        {
            float u = age / SplitDelay;
            if (u < 0f) u = 0f;
            else if (u > 1f) u = 1f;
            return Mathf.SmoothStep(0.05f, 0.72f, u);
        }

        public static void Play(Transform root, Vector3 at)
        {
            Play(root, at, null, Vector3.one, false);
        }

        public static void Play(Transform root, Vector3 at, Sprite limb, Vector3 limbScale, bool flip)
        {
            Play(root, at, limb, limbScale, flip, at, Vector3.right, null);
        }

        // trunk is the bark end on the tree. axis runs from that end to the tip.
        // spare is the struck wood: the char shader owns it, so the shade skips it.
        public static void Play(Transform root, Vector3 at, Sprite limb, Vector3 limbScale, bool flip, Vector3 trunk, Vector3 axis, SpriteRenderer spare)
        {
            if (root == null) return;
            var go = new GameObject("StrikeFx");
            go.transform.SetParent(root, false);
            var fx = go.AddComponent<StrikeFx>();
            fx.Build(root, at, limb, limbScale, flip, trunk, axis, spare);
        }

        public static void ClearAll()
        {
            for (int i = Live.Count - 1; i >= 0; i--)
            {
                if (Live[i] != null) Destroy(Live[i].gameObject);
            }
            Live.Clear();
            var holds = Object.FindObjectsByType<StumpHold>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < holds.Length; i++)
                if (holds[i] != null) Destroy(holds[i].gameObject);
        }

        void OnEnable() => Live.Add(this);

        void OnDisable()
        {
            Live.Remove(this);
            RestoreShade();
            ClearRims();
            if (_chars != null)
            {
                for (int i = 0; i < _chars.Length; i++)
                    if (_chars[i] != null) Destroy(_chars[i]);
                _chars = null;
            }
        }

        void Build(Transform root, Vector3 at, Sprite limb, Vector3 limbScale, bool flip, Vector3 trunk, Vector3 axis, SpriteRenderer spare)
        {
            _at = at;
            if (axis.sqrMagnitude < 0.01f) axis = Vector3.right;
            _axis = axis;
            TakeShade(root, spare);
            TakeRims(root, at, spare);

            var bits = new System.Collections.Generic.List<Bit>(56);
            var glow = SpriteCatalog.Glow;
            float span = axis.magnitude;
            if (span < 0.4f) span = 2.4f;
            Vector3 dir = axis / span;

            for (int i = 0; i < 12; i++)
            {
                float ang = Random.Range(0f, 6.28f);
                var bit = Make(glow, at, Random.Range(0.07f, 0.15f), 14, 0, transform);
                bit.Life = Random.Range(0.35f, 0.7f);
                float sp = Random.Range(2.4f, 5.2f);
                bit.VX = Mathf.Cos(ang) * sp;
                bit.VY = Mathf.Abs(Mathf.Sin(ang)) * sp * 0.7f + 1.4f;
                bit.Spin = Random.Range(-8f, 8f);
                bit.Sr.color = new Color(1f, 0.96f, 0.78f, 1f);
                bits.Add(bit);
            }

            var add = Additive();
            for (int i = 0; i < 12; i++)
            {
                float u = (i + 0.5f) / 12f;
                var pos = trunk + axis * u + Vector3.up * 0.22f;
                var bit = Make(glow, pos, 1f, 7, 1, transform);
                if (add != null) bit.Sr.sharedMaterial = add;
                float w = Random.Range(0.28f, 0.55f);
                float h = Random.Range(0.9f, 1.65f);
                bit.T.localScale = new Vector3(w, h, 1f);
                bit.Base = bit.T.localScale;
                bit.Home = pos;
                bit.Along = u;
                bit.Life = Random.Range(0.7f, 1.15f);
                bit.VY = Random.Range(1.2f, 2.3f);
                bit.VX = Random.Range(-0.35f, 0.35f);
                bit.Spin = Random.Range(-3f, 3f);
                float hot = Random.Range(0f, 1f);
                bit.Sr.color = Color.Lerp(new Color(1f, 0.78f, 0.22f, 0f), new Color(1f, 0.32f, 0.05f, 0f), hot);
                bit.Sr.enabled = false;
                bits.Add(bit);
            }

            for (int i = 0; i < 8; i++)
            {
                var pos = at + new Vector3(Random.Range(-span * 0.35f, span * 0.35f), 0.15f, 0f);
                var bit = Make(glow, pos, Random.Range(0.55f, 1.05f), 7, 2, transform);
                bit.Home = pos;
                bit.Life = Random.Range(0.9f, 1.5f);
                bit.Delay = Random.Range(0.15f, 0.45f);
                bit.VX = Random.Range(-0.18f, 0.18f);
                bit.VY = Random.Range(0.4f, 0.85f);
                bit.Spin = Random.Range(-0.4f, 0.4f);
                bit.Sr.color = new Color(0.28f, 0.26f, 0.24f, 0f);
                bits.Add(bit);
            }

            for (int i = 0; i < 12; i++)
            {
                bool burst = i >= 4;
                float u = Random.Range(0.1f, 0.9f);
                var pos = burst ? at + Random.insideUnitSphere * 0.15f : trunk + axis * u;
                pos.z = 0f;
                var bit = Make(glow, pos, Random.Range(0.08f, 0.16f), 15, 3, transform);
                if (add != null) bit.Sr.sharedMaterial = add;
                bit.Home = pos;
                bit.Life = Random.Range(0.7f, 1.25f);
                bit.Delay = burst ? SplitDelay : Random.Range(0.05f, 0.4f);
                bit.VX = Random.Range(-1.8f, 1.8f);
                bit.VY = Random.Range(1.1f, 2.8f);
                bit.Spin = Random.Range(-8f, 8f);
                bit.Sr.color = new Color(1f, 0.5f, 0.1f, burst ? 0f : 0.9f);
                bit.Sr.enabled = !burst;
                bits.Add(bit);
            }

            Vector3 pose = spare != null ? spare.transform.position : at;
            Quaternion woodRot = spare != null ? spare.transform.rotation : Quaternion.identity;
            Vector3 woodScale = spare != null ? AbsScale(spare.transform.lossyScale) : AbsScale(limbScale);
            var chars = new System.Collections.Generic.List<Material>(12);

            int fallN = 6;
            for (int i = 0; i < fallN; i++)
            {
                bool half = i < 2;
                var spr = limb != null ? limb : glow;
                float side = i == 0 ? -1f : 1f;
                if (!half) side = Random.value < 0.5f ? -1f : 1f;
                float along = half ? 0.5f : Random.Range(0.15f, 0.85f);
                var pos = half ? pose : trunk + axis * along;
                var bit = Make(spr, pos, 1f, 16, 4, transform);
                if (flip) bit.Sr.flipX = true;
                if (half)
                    PoseWood(bit.T, pose, woodRot, woodScale);
                else
                {
                    float chip = Random.Range(0.34f, 0.52f);
                    PoseStump(bit.T, pose, woodRot, woodScale * chip, trunk, Random.Range(-28f, 28f));
                }
                bit.Base = bit.T.localScale;
                bit.Home = bit.T.position;
                bit.Life = half ? Random.Range(1.15f, 1.45f) : Random.Range(0.85f, 1.2f);
                bit.Delay = SplitDelay;
                bit.VX = side * (half ? Random.Range(0.7f, 1.45f) : Random.Range(0.4f, 1.7f));
                bit.VY = half ? Random.Range(1.4f, 2.4f) : Random.Range(1.8f, 3.2f);
                bit.Spin = side * Random.Range(half ? 1.4f : 3f, half ? 3.2f : 7f);
                var mat = CharBit(chars, limb, 1.05f, half ? (i == 0 ? 1f : 2f) : 3f, Random.Range(0.2f, 9f), Random.Range(0.08f, 0.16f));
                if (mat != null) bit.Sr.sharedMaterial = mat;
                bit.Sr.color = new Color(1f, 1f, 1f, 0f);
                bit.Sr.enabled = false;
                if (!half)
                {
                    var halo = WorldBuilder.Sprite("Halo", glow, pos, 1f, 17, bit.T);
                    halo.transform.localPosition = Vector3.zero;
                    halo.transform.localRotation = Quaternion.identity;
                    WorldScale(halo.transform, Random.Range(0.28f, 0.5f));
                    bit.Halo = halo.GetComponent<SpriteRenderer>();
                    if (add != null) bit.Halo.sharedMaterial = add;
                    bit.Halo.color = new Color(1f, 0.42f, 0.06f, 0f);
                    bit.Halo.enabled = false;
                }
                bits.Add(bit);
            }

            for (int i = 0; i < 5; i++)
            {
                var spr = limb != null ? limb : glow;
                float jag = Random.Range(-36f, 36f);
                float clip = Random.Range(0.22f, 0.38f);
                var bit = Make(spr, pose, 1f, 4, 5, transform);
                if (flip) bit.Sr.flipX = true;
                float thick = Random.Range(0.92f, 1.12f);
                PoseStump(bit.T, pose, woodRot, new Vector3(woodScale.x, woodScale.y * thick, woodScale.z), trunk, jag);
                bit.Base = bit.T.localScale;
                bit.Home = bit.T.position;
                bit.Life = 4f;
                bit.Delay = SplitDelay;
                var mat = CharBit(chars, limb, 1.05f, 3f, 2.5f + i * 1.7f, clip);
                if (mat != null) bit.Sr.sharedMaterial = mat;
                bit.Sr.color = new Color(1f, 1f, 1f, 0f);
                bit.Sr.enabled = false;
                // Two faint coals. The splinter itself is the stump.
                if (i < 2)
                {
                    var tip = trunk + dir * (span * clip * 0.72f);
                    var ember = WorldBuilder.Sprite("Ember", glow, tip, 1f, 6, bit.T);
                    WorldScale(ember.transform, Random.Range(0.06f, 0.12f));
                    bit.Halo = ember.GetComponent<SpriteRenderer>();
                    if (add != null) bit.Halo.sharedMaterial = add;
                    bit.Halo.color = new Color(1f, 0.46f, 0.08f, 0f);
                    bit.Halo.enabled = false;
                }
                bits.Add(bit);
            }

            _chars = chars.ToArray();
            _bits = bits.ToArray();
        }

        static void WorldScale(Transform t, float size)
        {
            var p = t.parent != null ? t.parent.lossyScale : Vector3.one;
            t.localScale = new Vector3(
                size / Mathf.Max(0.05f, Mathf.Abs(p.x)),
                size / Mathf.Max(0.05f, Mathf.Abs(p.y)),
                1f);
        }

        static Vector3 AbsScale(Vector3 s)
        {
            return new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
        }

        static void PoseWood(Transform t, Vector3 pose, Quaternion rot, Vector3 scale)
        {
            t.rotation = rot;
            t.position = pose;
            t.localScale = scale;
        }

        // Pivot the cut end on the trunk so a jagged turn still meets the tree.
        static void PoseStump(Transform t, Vector3 pose, Quaternion rot, Vector3 scale, Vector3 trunk, float jag)
        {
            var jagRot = rot * Quaternion.Euler(0f, 0f, jag);
            Vector3 localTrunk = Quaternion.Inverse(rot) * (trunk - pose);
            t.rotation = jagRot;
            t.position = trunk - jagRot * localTrunk;
            t.localScale = scale;
        }

        static Material CharBit(System.Collections.Generic.List<Material> into, Sprite spr, float spread, float mask, float seed, float clip)
        {
            var sh = Shader.Find("FlockFive/WoodChar");
            if (sh == null || spr == null) return null;
            var m = new Material(sh) { name = "WoodCharBit", hideFlags = HideFlags.HideAndDontSave };
            if (spr.texture != null) m.mainTexture = spr.texture;
            m.SetFloat("_Spread", spread);
            m.SetFloat("_Hit", 0.5f);
            m.SetFloat("_Mask", mask);
            m.SetFloat("_Seed", seed);
            m.SetFloat("_Clip", clip);
            into.Add(m);
            return m;
        }

        static Bit Make(Sprite spr, Vector3 pos, float scale, int order, byte kind, Transform parent)
        {
            var go = WorldBuilder.Sprite(kind == 5 ? "Stump" : "Ember", spr, pos, scale, order, parent);
            var bit = new Bit();
            bit.T = go.transform;
            bit.Sr = go.GetComponent<SpriteRenderer>();
            bit.Base = go.transform.localScale;
            bit.Home = pos;
            bit.Kind = kind;
            return bit;
        }

        void TakeShade(Transform root, SpriteRenderer spare)
        {
            var all = root.GetComponentsInChildren<SpriteRenderer>(false);
            var list = new System.Collections.Generic.List<Saved>(all.Length);
            for (int i = 0; i < all.Length; i++)
            {
                var sr = all[i];
                if (sr == null || sr == spare) continue;
                if (SkipWorld(sr.transform)) continue;
                list.Add(new Saved { Sr = sr, Color = sr.color });
            }
            _shade = list.ToArray();
        }

        static bool SkipWorld(Transform t)
        {
            while (t != null)
            {
                string n = t.name;
                if (n == "Storm" || n == "Hive" || n == "Bg" || n == "StrikeFx") return true;
                if (n.Length >= 6 && string.CompareOrdinal(n, 0, "Feeder", 0, 6) == 0) return true;
                t = t.parent;
            }
            return false;
        }

        void TakeRims(Transform root, Vector3 at, SpriteRenderer spare)
        {
            var all = root.GetComponentsInChildren<SpriteRenderer>(false);
            var list = new System.Collections.Generic.List<Rim>(16);
            for (int i = 0; i < all.Length; i++)
            {
                var sr = all[i];
                if (sr == null || sr == spare || !sr.enabled || sr.sprite == null) continue;
                if (!RimName(sr.gameObject.name)) continue;
                float dx = sr.bounds.center.x - at.x;
                float dy = sr.bounds.center.y - at.y;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > RimRadius) continue;
                float k = 1f - d / RimRadius;
                if (k < 0.08f) continue;
                // Tint the sprite itself. A scaled silhouette copy read as a white sticker.
                list.Add(new Rim { Body = sr, Glow = null, K = k });
                if (list.Count >= 16) break;
            }
            _rims = list.ToArray();
        }

        static bool RimName(string n)
        {
            if (n == "Wood") return true;
            if (n.Length >= 4 && string.CompareOrdinal(n, 0, "Bird", 0, 4) == 0) return true;
            if (n.Length >= 4 && string.CompareOrdinal(n, 0, "Leaf", 0, 4) == 0) return true;
            if (n.Length >= 4 && string.CompareOrdinal(n, 0, "Vine", 0, 4) == 0) return true;
            return false;
        }

        void RestoreShade()
        {
            if (_shade == null) return;
            for (int i = 0; i < _shade.Length; i++)
            {
                if (_shade[i].Sr != null) _shade[i].Sr.color = _shade[i].Color;
            }
            _shade = null;
        }

        void ClearRims()
        {
            if (_rims == null) return;
            for (int i = 0; i < _rims.Length; i++)
            {
                if (_rims[i].Glow != null) Destroy(_rims[i].Glow.gameObject);
            }
            _rims = null;
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt < 0f) dt = 0f;
            if (dt > 0.05f) dt = 0.05f;
            _age += dt;
            ApplyShade();
            if (_bits == null) return;
            float spread = CharSpread(_age);
            for (int i = 0; i < _bits.Length; i++)
            {
                var bit = _bits[i];
                if (bit.T == null) continue;
                if (bit.Kind == 1 && _age < SplitDelay)
                {
                    Lick(ref bit, spread);
                    _bits[i] = bit;
                    continue;
                }
                if (bit.Kind == 5)
                {
                    HoldStump(ref bit);
                    _bits[i] = bit;
                    continue;
                }
                if (_age < bit.Delay)
                {
                    if (bit.Sr != null && bit.Kind != 4 && bit.Kind != 2) bit.Sr.enabled = bit.Kind != 2;
                    _bits[i] = bit;
                    continue;
                }
                float local = _age - bit.Delay;
                float life = bit.Life > 0.01f ? local / bit.Life : 1f;
                if (bit.Kind == 4) bit.VY -= 6.2f * dt;
                else if (bit.Kind == 3) bit.VY -= 4.4f * dt;
                else if (bit.Kind == 0) bit.VY -= 2.2f * dt;
                bit.T.position += new Vector3(bit.VX, bit.VY, 0f) * dt;
                if (bit.Kind != 5) bit.T.Rotate(0f, 0f, bit.Spin * dt * 40f);
                if (bit.Kind == 2)
                {
                    float grow = 1f + local * 0.55f;
                    bit.T.localScale = new Vector3(bit.Base.x * grow, bit.Base.y * grow, 1f);
                }
                var c = bit.Sr != null ? bit.Sr.color : Color.white;
                float a = 1f - Mathf.Clamp01(life);
                if (bit.Kind == 2) a *= 0.55f;
                if (bit.Kind == 1) a *= 0.9f;
                if (bit.Kind == 4)
                {
                    c = new Color(1f, 1f, 1f, 1f);
                    a = Mathf.Clamp01(1.08f - life);
                    if (bit.Sr != null && !bit.Sr.enabled) bit.Sr.enabled = true;
                    if (bit.Halo != null)
                    {
                        bit.Halo.enabled = true;
                        float glow = a * (0.55f + 0.4f * Mathf.Clamp01(1.1f - life));
                        bit.Halo.color = new Color(1f, 0.42f, 0.06f, glow);
                    }
                }
                c.a = a < 0f ? 0f : a;
                if (bit.Sr != null) bit.Sr.color = c;
                _bits[i] = bit;
            }
            if (_age >= Dur) Finish();
        }

        void Lick(ref Bit bit, float spread)
        {
            float du = bit.Along - 0.5f;
            if (du < 0f) du = -du;
            bool on = du <= spread + 0.07f;
            if (bit.Sr == null) return;
            bit.Sr.enabled = on;
            float flick = 0.72f + 0.28f * Mathf.Sin(_age * 26f + bit.Along * 18f);
            float h = 0.8f + 0.45f * Mathf.Sin(_age * 16f + bit.Along * 11f);
            bit.T.position = bit.Home + new Vector3(Mathf.Sin(_age * 12f + bit.Along * 8f) * 0.045f, 0.08f * h, 0f);
            bit.T.localScale = new Vector3(bit.Base.x * (0.75f + 0.35f * flick), bit.Base.y * h, 1f);
            var c = bit.Sr.color;
            c.a = on ? 0.9f * flick * Mathf.Clamp01(spread * 4f) : 0f;
            bit.Sr.color = c;
        }

        void HoldStump(ref Bit bit)
        {
            if (_age < bit.Delay)
            {
                if (bit.Sr != null) bit.Sr.enabled = false;
                return;
            }
            if (bit.Sr != null)
            {
                bit.Sr.enabled = true;
                bit.Sr.color = Color.white;
            }
            if (bit.Halo != null)
            {
                bit.Halo.enabled = true;
                float u = (_age - bit.Delay) / 2.4f;
                if (u < 0f) u = 0f;
                else if (u > 1f) u = 1f;
                float pulse = 0.75f + 0.25f * Mathf.Sin(_age * 9f + bit.Home.x * 6f);
                bit.Halo.color = new Color(1f, 0.42f, 0.06f, (1f - u) * 0.18f * pulse);
            }
        }

        void ApplyShade()
        {
            float shade;
            if (_age < 0.1f) shade = _age / 0.1f;
            else if (_age < ShadeHold) shade = 1f;
            else shade = 1f - (_age - ShadeHold) / 0.6f;
            if (shade < 0f) shade = 0f;
            else if (shade > 1f) shade = 1f;
            var dim = Color.Lerp(Color.white, new Color(0.38f, 0.42f, 0.52f), shade);
            if (_shade != null)
            {
                for (int i = 0; i < _shade.Length; i++)
                {
                    if (_shade[i].Sr != null) _shade[i].Sr.color = _shade[i].Color * dim;
                }
            }
            float rim;
            if (_age < 0.06f) rim = _age / 0.06f;
            else if (_age < 0.46f) rim = 1f - (_age - 0.06f) / 0.4f;
            else rim = 0f;
            if (rim < 0f) rim = 0f;
            if (_rims == null) return;
            for (int i = 0; i < _rims.Length; i++)
            {
                float a = rim * _rims[i].K;
                var body = _rims[i].Body;
                if (body != null && a > 0.02f)
                {
                    var c = body.color;
                    body.color = Color.Lerp(c, new Color(1f, 0.78f, 0.42f, c.a), 0.85f * a);
                }
            }
        }

        void Finish()
        {
            DetachStump();
            RestoreShade();
            ClearRims();
            Destroy(gameObject);
        }

        void DetachStump()
        {
            if (_stumpDetached || _bits == null) return;
            _stumpDetached = true;
            Transform parent = transform.parent;
            var holdGo = new GameObject("CharStump");
            if (parent != null) holdGo.transform.SetParent(parent, false);
            int n = 0;
            for (int i = 0; i < _bits.Length; i++)
            {
                if (_bits[i].Kind != 5 || _bits[i].T == null) continue;
                _bits[i].T.SetParent(holdGo.transform, true);
                n++;
            }
            if (n == 0)
            {
                Destroy(holdGo);
                return;
            }
            holdGo.AddComponent<StumpHold>();
        }

        static Material Additive()
        {
            if (_add != null) return _add;
            var sh = Shader.Find("FlockFive/SpriteAdd");
            if (sh == null) return null;
            _add = new Material(sh) { name = "SpriteAdd", hideFlags = HideFlags.HideAndDontSave };
            return _add;
        }
    }

    // Charred stump left on the trunk after the strike fx packs up.
    // Embers fade. The splinters stay until a restart clears them.
    public sealed class StumpHold : MonoBehaviour
    {
        SpriteRenderer[] _embers;
        float[] _base;
        float _age;

        void Awake()
        {
            var all = GetComponentsInChildren<SpriteRenderer>(true);
            int n = 0;
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null && all[i].gameObject.name == "Ember") n++;
            _embers = new SpriteRenderer[n];
            _base = new float[n];
            int w = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null || all[i].gameObject.name != "Ember") continue;
                _embers[w] = all[i];
                _base[w] = all[i].color.a;
                w++;
            }
        }

        void LateUpdate()
        {
            _age += Time.deltaTime;
            float u = _age / 2.6f;
            if (u < 0f) u = 0f;
            else if (u > 1f) u = 1f;
            float a = 1f - u;
            for (int i = 0; i < _embers.Length; i++)
            {
                if (_embers[i] == null) continue;
                var c = _embers[i].color;
                c.a = _base[i] * a;
                _embers[i].color = c;
            }
        }
    }
}
