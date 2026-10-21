using UnityEngine;
using UnityEngine.Rendering;

namespace FlockFive
{
    // The one ambient-life helper. Keyed by the painting behind the garden
    // (and by summer on the home screen). FoliageSway moves the paint.
    // Particles are one mesh per texture, filled at build, then rewritten
    // in place. Nothing is allocated per frame. Counts drop on FoliageSway.LowPower.
    public sealed class GardenLife : MonoBehaviour
    {
        const byte Snow = 0;
        const byte Leaf = 1;
        const byte Petal = 2;
        const byte Fly = 3;
        const byte Seed = 4;
        const byte Wisp = 5;
        const byte Tumble = 6;
        const byte Cloud = 7;
        const byte Glint = 8;
        const byte Blossom = 9;
        const byte Butterfly = 10;
        // Oct 2026 (Brandon): leaves tumble along the ground on the shared wind, most of all as rain comes.
        const byte Ground = 11;
        const byte Streak = 12;

        // Ground band (share of the frame height from the bottom) where ground leaves rest and skid.
        public const float GroundBandLo = 0.035f;
        public const float GroundBandHi = 0.30f;
        // Wind push (world units / s at full wind) for falling leaves and for ground skids.
        public const float FallWindPush = 2.4f;
        public const float GroundSkid = 4.2f;
        public const float GroundCreep = 0.22f;
        public const float GroundGravity = 6f;
        // A gust lifts a few ground leaves 0.3..0.8 units, then they drop back.
        public const float LiftMin = 0.3f, LiftMax = 0.8f;
        // Falling leaves stay readable in rain (they used to fade to 15%).
        public const float LeafRainFloor = 1f;
        // Falling leaves are born in the close canopy: this band under the top edge (world units).
        public const float CanopyDepth = 1.6f;

        sealed class Sheet
        {
            public Mesh Mesh;
            public MeshRenderer Rend;
            public Vector3[] Verts;
            public Color32[] Cols;
            public float[] X, Y, VX, VY, Phase, Spin, HW, HH, Rest, Roll, Age;
            public byte[] Kind;
            public byte[] R, G, B, A;
            public int N;
            public bool Burst;
            public Material Mat;
        }

        static Transform _homeRoot;
        static GardenScene _seedScene = GardenScene.Summer;
        static readonly int SpritePropsId = Shader.PropertyToID("unity_SpriteProps");
        static readonly int SpriteColorId = Shader.PropertyToID("unity_SpriteColor");

        Sheet[] _sheets;
        int _sheetN;
        Camera _cam;
        bool _home;
        int _wraps;
        float _drift = 1f;
        float _cloud = 1f;
        float _shimmer = 1f;
        bool _motion = true;
        // Season scene burst (SeasonCrossfade drives these). _burstA scales the burst sheet's alpha,
        // _energy scales ambient drift while the scene plays; both rest at 0 / 1.
        float _burstA;
        float _energy = 1f;
        float _driftNow = 1f;
        float _wind;
        Sheet _burst;
        // Winter ambient: only the occasional flake or two flutters by and flips around.
        public const int SparseSnow = 3;
        public const int SparseSnowLow = 2;
        public const float SnowFlipHz = 2.3f;

        public static bool HomeLive => _homeRoot != null;

        public static GardenLife Attach(Transform root, GardenScene scene, Camera cam)
        {
            return Attach(root, scene, cam, false);
        }

        public static void AttachHome(Transform parent, Camera cam)
        {
            DetachHome();
            var root = new GameObject("HomeLife").transform;
            root.SetParent(parent, false);
            _homeRoot = root;
            var bg = WorldBuilder.Sprite("Bg", SpriteCatalog.GardenBg, new Vector3(0f, -0.15f, 8f), 1f, -20, root);
            bg.layer = PortraitLock.BleedLayer;
            var fit = bg.AddComponent<BackgroundFitter>();
            fit.Cam = cam;
            fit.FollowCamera = false;
            fit.Untinted = true;
            fit.WorldCenter = new Vector3(0f, -0.15f, 8f);
            fit.WorldSize = new Vector2(13.6f, 24.0f);
            fit.Apply();
            FoliageSway.Apply(bg.GetComponent<SpriteRenderer>(), GardenScene.Summer);
            Attach(root, GardenScene.Summer, cam, true);
        }

        // Disable before Destroy so the painting does not linger one frame over the next garden.
        public static void DetachHome()
        {
            if (_homeRoot == null) return;
            _homeRoot.gameObject.SetActive(false);
            Object.Destroy(_homeRoot.gameObject);
            _homeRoot = null;
        }

        static GardenLife Attach(Transform root, GardenScene scene, Camera cam, bool home)
        {
            var go = new GameObject("Life");
            go.transform.SetParent(root, false);
            var life = go.AddComponent<GardenLife>();
            life._cam = cam;
            life._home = home;
            life.Build(scene);
            return life;
        }

        void Build(GardenScene scene)
        {
            bool low = FoliageSway.LowPower;
            _sheets = new Sheet[12];
            _seedScene = scene;
            _sheetN = 0;
            var tune = SeasonMotion.For(scene);
            _drift = tune.Drift;
            _cloud = tune.Cloud;
            _shimmer = tune.Shimmer;
            SeasonMotion.Scene = scene;
            var n = SeasonMotion.Lively(scene, low);
            var rng = new System.Random(scene == GardenScene.Desert ? 29 : 17 + (int)scene * 3);
            // Winter is sparse: a flake or two at a time, not a curtain (Brandon). Budget table untouched.
            int snowN = scene == GardenScene.Winter ? Mathf.Min(n.Snow, low ? SparseSnowLow : SparseSnow) : n.Snow;
            if (snowN > 0) AddSheet(SpriteCatalog.Glow, snowN, Snow, rng);
            if (n.Leaf > 0) AddSheet(SpriteCatalog.PetalPeach, n.Leaf, Leaf, rng);
            if (n.Petal > 0) AddSheet(SpriteCatalog.PetalPink, n.Petal, Petal, rng);
            if (n.Wisp > 0) AddSheet(SpriteCatalog.Glow, n.Wisp, Wisp, rng);
            if (n.Tumble > 0) AddSheet(SpriteCatalog.PetalPeach, n.Tumble, Tumble, rng);
            if (n.Fly > 0) AddSheet(SpriteCatalog.Firefly, n.Fly, Fly, rng);
            if (n.Seed > 0) AddSheet(SpriteCatalog.Glow, n.Seed, Seed, rng);
            if (n.Cloud > 0) AddSheet(SpriteCatalog.Glow, n.Cloud, Cloud, rng);
            if (n.Glint > 0) AddSheet(SpriteCatalog.Glow, n.Glint, Glint, rng);
            if (n.Blossom > 0) AddSheet(SpriteCatalog.PetalPeach, n.Blossom, Blossom, rng);
            if (n.Butterfly > 0) AddSheet(SpriteCatalog.PetalPeach, n.Butterfly, Butterfly, rng);
            // Ground leaves (winter: drifting snow puffs) and faint gust streaks across the ground band.
            if (n.Ground > 0) AddSheet(GroundSprite(scene), n.Ground, Ground, rng);
            if (n.Streak > 0) AddSheet(SpriteCatalog.Glow, n.Streak, Streak, rng);
        }

        static Sprite GroundSprite(GardenScene scene)
        {
            if (scene == GardenScene.Winter) return SpriteCatalog.Glow;
            if (scene == GardenScene.Spring) return SpriteCatalog.PetalPink;
            return SpriteCatalog.PetalPeach;
        }

        // Season colours for ground leaves: fall rust / amber / ochre, summer greens, spring pink and green,
        // winter white snow puffs.
        public static Color32 GroundTint(GardenScene scene, int pick)
        {
            pick = ((pick % 3) + 3) % 3;
            switch (scene)
            {
                case GardenScene.Fall:
                    return pick == 0 ? new Color32(214, 74, 34, 245) : pick == 1 ? new Color32(250, 168, 48, 245) : new Color32(240, 204, 86, 245);
                case GardenScene.Spring:
                    return pick == 0 ? new Color32(255, 170, 190, 230) : pick == 1 ? new Color32(150, 200, 110, 230) : new Color32(255, 214, 220, 230);
                case GardenScene.Winter:
                    return new Color32(236, 242, 255, 170);
                default:
                    return pick == 0 ? new Color32(96, 150, 70, 230) : pick == 1 ? new Color32(140, 170, 72, 230) : new Color32(170, 120, 60, 230);
            }
        }

        // Season scene entrance: one extra sheet of the season's own particles, alpha 0 until driven.
        // Spring petals drift down and sideways, summer motes and glints rise, fall leaves tumble,
        // winter snow falls (the same flutter-flake sheet as the ambient look, just more of it).
        public void BeginBurst(GardenScene scene)
        {
            if (_burst != null || _sheets == null) return;
            bool low = FoliageSway.LowPower;
            int n = low ? SeasonScene.BurstCountLow : SeasonScene.BurstCount;
            byte kind;
            Sprite spr;
            switch (scene)
            {
                case GardenScene.Spring: kind = Petal; spr = SpriteCatalog.PetalPink; break;
                case GardenScene.Summer: kind = Seed; spr = SpriteCatalog.Glow; break;
                case GardenScene.Fall: kind = Leaf; spr = SpriteCatalog.PetalPeach; break;
                case GardenScene.Winter: kind = Snow; spr = SpriteCatalog.Glow; break;
                default: return;
            }
            int before = _sheetN;
            AddSheet(spr, n, kind, new System.Random(101 + (int)scene), scene, true);
            if (_sheetN > before) _burst = _sheets[_sheetN - 1];
            _burstA = 0f;
        }

        public void SetBurst(float alpha, float energy)
        {
            _burstA = alpha;
            _energy = energy;
            if (_burst != null && _burst.Rend != null) _burst.Rend.enabled = alpha > 0.003f;
        }

        public void EndBurst()
        {
            _burstA = 0f;
            _energy = 1f;
            if (_burst == null) return;
            if (_burst.Rend != null) _burst.Rend.enabled = false;
            if (_burst.Cols == null) return;
            for (int i = 0; i < _burst.Cols.Length; i++)
            {
                var c = _burst.Cols[i];
                c.a = 0;
                _burst.Cols[i] = c;
            }
            if (_burst.Mesh != null)
                _burst.Mesh.SetColors(_burst.Cols, 0, _burst.Cols.Length, MeshUpdateFlags.DontRecalculateBounds);
        }

        void AddSheet(Sprite spr, int n, byte kind, System.Random rng)
        {
            AddSheet(spr, n, kind, rng, GardenScene.Desert, false);
        }

        void AddSheet(Sprite spr, int n, byte kind, System.Random rng, GardenScene burstScene, bool burst)
        {
            if (spr == null || n <= 0 || _sheetN >= _sheets.Length) return;
            var sheet = new Sheet();
            sheet.N = n;
            sheet.Burst = burst;
            sheet.X = new float[n];
            sheet.Y = new float[n];
            sheet.VX = new float[n];
            sheet.VY = new float[n];
            sheet.Phase = new float[n];
            sheet.Spin = new float[n];
            sheet.HW = new float[n];
            sheet.HH = new float[n];
            sheet.Rest = new float[n];
            sheet.Roll = new float[n];
            sheet.Age = new float[n];
            sheet.Kind = new byte[n];
            sheet.R = new byte[n];
            sheet.G = new byte[n];
            sheet.B = new byte[n];
            sheet.A = new byte[n];
            sheet.Verts = new Vector3[n * 4];
            sheet.Cols = new Color32[n * 4];
            var tris = new int[n * 6];
            var uvs = new Vector2[n * 4];
            Frame(out float left, out float right, out float bottom, out float top);
            for (int i = 0; i < n; i++)
            {
                sheet.Kind[i] = kind;
                sheet.Phase[i] = (float)rng.NextDouble() * 6.28f;
                sheet.X[i] = Mathf.Lerp(left, right, (float)rng.NextDouble());
                sheet.Y[i] = Mathf.Lerp(bottom, top, (float)rng.NextDouble());
                SeedSlot(sheet, i, rng);
                if (burst) SeedBurst(sheet, i, burstScene, rng);
                int v = i * 4;
                int t = i * 6;
                tris[t] = v;
                tris[t + 1] = v + 2;
                tris[t + 2] = v + 1;
                tris[t + 3] = v + 2;
                tris[t + 4] = v + 3;
                tris[t + 5] = v + 1;
                uvs[v] = new Vector2(0f, 0f);
                uvs[v + 1] = new Vector2(1f, 0f);
                uvs[v + 2] = new Vector2(0f, 1f);
                uvs[v + 3] = new Vector2(1f, 1f);
            }

            var donor = WorldBuilder.Sprite("LifeDonor", spr, new Vector3(0f, -40f, 0f), 1f, -12, transform);
            var donorSr = donor.GetComponent<SpriteRenderer>();
            if (donorSr.sharedMaterial != null)
                sheet.Mat = new Material(donorSr.sharedMaterial);
            Destroy(donor);
            if (sheet.Mat == null) return;
            sheet.Mat.mainTexture = spr.texture;
            sheet.Mat.SetVector(SpritePropsId, new Vector4(1f, 1f, -1f, 0f));
            sheet.Mat.SetVector(SpriteColorId, new Vector4(1f, 1f, 1f, 1f));

            sheet.Mesh = new Mesh { name = "Life" + _sheetN };
            sheet.Mesh.MarkDynamic();
            sheet.Mesh.vertices = sheet.Verts;
            sheet.Mesh.uv = uvs;
            sheet.Mesh.colors32 = sheet.Cols;
            sheet.Mesh.triangles = tris;
            sheet.Mesh.bounds = new Bounds(new Vector3(0f, WorldBuilder.CamRestY, 0f), new Vector3(40f, 48f, 2f));

            var host = new GameObject("LifeSheet");
            host.transform.SetParent(transform, false);
            var mf = host.AddComponent<MeshFilter>();
            mf.sharedMesh = sheet.Mesh;
            sheet.Rend = host.AddComponent<MeshRenderer>();
            sheet.Rend.sharedMaterial = sheet.Mat;
            sheet.Rend.sortingOrder = -12;
            sheet.Rend.shadowCastingMode = ShadowCastingMode.Off;
            sheet.Rend.lightProbeUsage = LightProbeUsage.Off;
            sheet.Rend.reflectionProbeUsage = ReflectionProbeUsage.Off;
            sheet.Rend.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            _sheets[_sheetN++] = sheet;
        }

        static void SeedSlot(Sheet sheet, int i, System.Random rng)
        {
            byte kind = sheet.Kind[i];
            float u = (float)rng.NextDouble();
            if (kind == Snow)
            {
                // Fluttering flake: a bit bigger so the flip reads, slow fall, wide side flutter, tumbling spin.
                sheet.HW[i] = Mathf.Lerp(0.06f, 0.10f, u);
                sheet.HH[i] = sheet.HW[i];
                sheet.VY[i] = -(0.28f + 0.45f * (float)rng.NextDouble());
                sheet.VX[i] = Mathf.Lerp(-0.15f, 0.15f, (float)rng.NextDouble());
                sheet.Spin[i] = Mathf.Lerp(-1.8f, 1.8f, (float)rng.NextDouble());
                byte w = (byte)(210 + (int)(u * 40f));
                sheet.R[i] = w;
                sheet.G[i] = w;
                sheet.B[i] = 235;
                sheet.A[i] = 150;
            }
            else if (kind == Leaf)
            {
                // Oct 2026: a touch bigger so the canopy leaves read against the close sequoia art.
                sheet.HW[i] = Mathf.Lerp(0.14f, 0.22f, u);
                sheet.HH[i] = sheet.HW[i] * 1.35f;
                sheet.VY[i] = -(0.32f + 0.28f * (float)rng.NextDouble());
                sheet.VX[i] = Mathf.Lerp(-0.12f, 0.20f, (float)rng.NextDouble());
                sheet.Spin[i] = Mathf.Lerp(-1.1f, 1.1f, (float)rng.NextDouble());
                sheet.R[i] = (byte)(210 + (int)(u * 40f));
                sheet.G[i] = (byte)(120 + (int)((float)rng.NextDouble() * 50f));
                sheet.B[i] = 48;
                sheet.A[i] = 170;
            }
            else if (kind == Petal)
            {
                // Build 110: cherry petals, about twice the old size, brighter, drifting right on a breeze.
                sheet.HW[i] = Mathf.Lerp(0.13f, 0.22f, u);
                sheet.HH[i] = sheet.HW[i] * 1.4f;
                sheet.VY[i] = -(0.22f + 0.22f * (float)rng.NextDouble());
                sheet.VX[i] = Mathf.Lerp(0.04f, 0.30f, (float)rng.NextDouble());
                sheet.Spin[i] = Mathf.Lerp(-1.2f, 1.2f, (float)rng.NextDouble());
                bool peach = rng.NextDouble() > 0.55;
                sheet.R[i] = 255;
                sheet.G[i] = peach ? (byte)205 : (byte)150;
                sheet.B[i] = peach ? (byte)190 : (byte)175;
                sheet.A[i] = 225;
            }
            else if (kind == Fly)
            {
                sheet.HW[i] = Mathf.Lerp(0.09f, 0.14f, u);
                sheet.HH[i] = sheet.HW[i];
                sheet.VX[i] = Mathf.Lerp(-0.22f, 0.22f, (float)rng.NextDouble());
                sheet.VY[i] = Mathf.Lerp(-0.12f, 0.16f, (float)rng.NextDouble());
                sheet.Spin[i] = 0.7f + 1.1f * (float)rng.NextDouble();
                sheet.R[i] = 255;
                sheet.G[i] = 236;
                sheet.B[i] = 150;
                sheet.A[i] = 0;
            }
            else if (kind == Wisp)
            {
                sheet.HW[i] = Mathf.Lerp(0.22f, 0.42f, u);
                sheet.HH[i] = Mathf.Lerp(0.018f, 0.034f, (float)rng.NextDouble());
                sheet.VX[i] = 0.35f + 0.55f * (float)rng.NextDouble();
                sheet.VY[i] = Mathf.Lerp(-0.05f, 0.06f, (float)rng.NextDouble());
                sheet.Spin[i] = 0f;
                sheet.R[i] = 214;
                sheet.G[i] = 180;
                sheet.B[i] = 130;
                sheet.A[i] = 70;
            }
            else if (kind == Tumble)
            {
                sheet.HW[i] = 0.16f;
                sheet.HH[i] = 0.13f;
                sheet.VX[i] = 0.85f;
                sheet.VY[i] = 0f;
                sheet.Spin[i] = 2.4f;
                sheet.R[i] = 148;
                sheet.G[i] = 104;
                sheet.B[i] = 62;
                sheet.A[i] = 0;
                sheet.X[i] = -8f;
                sheet.Y[i] = -4.2f;
                sheet.Phase[i] = 5f;
            }
            else if (kind == Ground)
            {
                bool snow = _seedScene == GardenScene.Winter;
                sheet.HW[i] = snow ? Mathf.Lerp(0.22f, 0.40f, u) : Mathf.Lerp(0.20f, 0.30f, u);
                sheet.HH[i] = snow ? sheet.HW[i] * 0.30f : sheet.HW[i] * 0.62f;
                sheet.VX[i] = 0f;
                sheet.VY[i] = 0f;
                // Per-leaf skid weight: some leaves catch the wind more.
                sheet.Spin[i] = Mathf.Lerp(0.65f, 1.25f, (float)rng.NextDouble());
                var c = GroundTint(_seedScene, rng.Next(3));
                sheet.R[i] = c.r;
                sheet.G[i] = c.g;
                sheet.B[i] = c.b;
                sheet.A[i] = c.a;
                sheet.Roll[i] = (float)rng.NextDouble() * 6.28f;
                sheet.Rest[i] = Mathf.Lerp(GroundBandLo, GroundBandHi, (float)rng.NextDouble()); // band share, set to world y on first step
                sheet.Age[i] = -1f;
            }
            else if (kind == Streak)
            {
                sheet.HW[i] = Mathf.Lerp(0.9f, 1.6f, u);
                sheet.HH[i] = Mathf.Lerp(0.03f, 0.05f, (float)rng.NextDouble());
                sheet.VX[i] = -(4.0f + 2.0f * (float)rng.NextDouble());
                sheet.VY[i] = 0f;
                sheet.Spin[i] = 0f;
                sheet.R[i] = 255;
                sheet.G[i] = 255;
                sheet.B[i] = 255;
                sheet.A[i] = 64; // ~0.25
                sheet.Rest[i] = Mathf.Lerp(GroundBandLo + 0.03f, GroundBandHi, (float)rng.NextDouble());
                sheet.Age[i] = -1f;
            }
            else if (kind == Blossom)
            {
                // Whole blossoms: larger, slower, wide flutter.
                sheet.HW[i] = Mathf.Lerp(0.18f, 0.28f, u);
                sheet.HH[i] = sheet.HW[i] * 1.2f;
                sheet.VY[i] = -(0.14f + 0.14f * (float)rng.NextDouble());
                sheet.VX[i] = Mathf.Lerp(0.02f, 0.22f, (float)rng.NextDouble());
                sheet.Spin[i] = Mathf.Lerp(-0.7f, 0.7f, (float)rng.NextDouble());
                sheet.R[i] = 255;
                sheet.G[i] = 225;
                sheet.B[i] = 220;
                sheet.A[i] = 215;
            }
            else if (kind == Butterfly)
            {
                sheet.HW[i] = Mathf.Lerp(0.16f, 0.22f, u);
                sheet.HH[i] = sheet.HW[i] * 0.9f;
                sheet.VX[i] = Mathf.Lerp(-0.25f, 0.25f, (float)rng.NextDouble());
                sheet.VY[i] = Mathf.Lerp(-0.05f, 0.05f, (float)rng.NextDouble());
                sheet.Spin[i] = 8f + 3f * (float)rng.NextDouble(); // wing beats per ~2pi seconds
                bool gold = rng.NextDouble() > 0.5;
                sheet.R[i] = 255;
                sheet.G[i] = gold ? (byte)214 : (byte)160;
                sheet.B[i] = gold ? (byte)70 : (byte)95;
                sheet.A[i] = 235;
                sheet.Y[i] = Mathf.Lerp(-2.0f, 3.0f, (float)rng.NextDouble());
            }
            else if (kind == Cloud)
            {
                sheet.HW[i] = Mathf.Lerp(0.85f, 1.45f, u) * (_seedScene == GardenScene.Spring ? 1.25f : 1f);
                sheet.HH[i] = Mathf.Lerp(0.22f, 0.38f, (float)rng.NextDouble());
                sheet.VX[i] = 0.28f + 0.08f * (float)rng.NextDouble();
                sheet.VY[i] = 0f;
                sheet.Spin[i] = 0f;
                sheet.R[i] = 255;
                sheet.G[i] = 250;
                sheet.B[i] = 244;
                sheet.A[i] = (byte)(_seedScene == GardenScene.Spring ? 96 : 52);
                sheet.X[i] = Mathf.Lerp(-3.4f, 3.4f, (float)rng.NextDouble());
                sheet.Y[i] = Mathf.Lerp(3.6f, 7.4f, (float)rng.NextDouble());
            }
            else if (kind == Glint)
            {
                sheet.HW[i] = Mathf.Lerp(0.08f, 0.16f, u);
                sheet.HH[i] = sheet.HW[i] * 0.35f;
                sheet.VX[i] = 0.12f + 0.08f * (float)rng.NextDouble();
                sheet.VY[i] = 0f;
                sheet.Spin[i] = 0f;
                sheet.R[i] = 255;
                sheet.G[i] = 248;
                sheet.B[i] = 220;
                sheet.A[i] = 140;
                sheet.X[i] = Mathf.Lerp(-3.2f, 3.2f, (float)rng.NextDouble());
                sheet.Y[i] = Mathf.Lerp(-6.4f, -3.6f, (float)rng.NextDouble());
            }
            else
            {
                sheet.HW[i] = Mathf.Lerp(0.035f, 0.06f, u);
                sheet.HH[i] = sheet.HW[i];
                sheet.VX[i] = Mathf.Lerp(-0.10f, 0.10f, (float)rng.NextDouble());
                sheet.VY[i] = Mathf.Lerp(-0.06f, 0.08f, (float)rng.NextDouble());
                sheet.Spin[i] = Mathf.Lerp(-0.4f, 0.4f, (float)rng.NextDouble());
                sheet.R[i] = 245;
                sheet.G[i] = 236;
                sheet.B[i] = 196;
                sheet.A[i] = 80;
            }
        }

        // Burst slots: season-specific motion while the scene plays.
        static void SeedBurst(Sheet sheet, int i, GardenScene scene, System.Random rng)
        {
            sheet.A[i] = 235;
            if (scene == GardenScene.Spring)
            {
                // Down and sideways on a breeze.
                sheet.VX[i] = Mathf.Lerp(0.30f, 0.70f, (float)rng.NextDouble());
                sheet.VY[i] = -(0.40f + 0.30f * (float)rng.NextDouble());
            }
            else if (scene == GardenScene.Summer)
            {
                // Warm gold light motes and sun glints that rise.
                sheet.HW[i] = Mathf.Lerp(0.05f, 0.11f, (float)rng.NextDouble());
                sheet.HH[i] = sheet.HW[i];
                sheet.VX[i] = Mathf.Lerp(-0.10f, 0.10f, (float)rng.NextDouble());
                sheet.VY[i] = 0.45f + 0.45f * (float)rng.NextDouble();
                sheet.R[i] = 255;
                sheet.G[i] = 226;
                sheet.B[i] = 140;
                sheet.A[i] = 210;
            }
            else if (scene == GardenScene.Fall)
            {
                // Leaves tumble: strong spin, side gusts.
                sheet.VX[i] = Mathf.Lerp(-0.25f, 0.45f, (float)rng.NextDouble());
                sheet.VY[i] = -(0.45f + 0.35f * (float)rng.NextDouble());
                sheet.Spin[i] = Mathf.Lerp(-2.6f, 2.6f, (float)rng.NextDouble());
            }
            else if (scene == GardenScene.Winter)
            {
                // Same fluttering flakes as the ambient look, just more of them while the scene plays.
                sheet.VY[i] = -(0.35f + 0.45f * (float)rng.NextDouble());
            }
        }

        void LateUpdate()
        {
            if (_sheetN == 0 || _sheets == null) return;
            if (_sheetN > _sheets.Length) _sheetN = _sheets.Length;
            bool on = BackgroundMotion.On;
            if (!on)
            {
                if (_motion) HoldStill();
                _motion = false;
                return;
            }
            if (!_motion) ShowAgain();
            _motion = true;
            float dt = PlayClock.Delta;
            if (dt <= 0f) return;
            float t = PlayClock.Now;
            Frame(out float left, out float right, out float bottom, out float top);
            float dusk = SkyCycle.Dusk;
            if (_home && dusk < 0.62f) dusk = 0.62f;
            _driftNow = _drift * _energy;
            // Shared wind (SeasonMotion.Wind): breeze, plus the pre-rain gust from GardenStorm.
            SeasonMotion.TickWind(t);
            _wind = SeasonMotion.Wind;
            // Rain dims the glints and fireflies only. Falling and ground leaves stay readable (were ~15%).
            float wet = 1f - GardenStorm.Wet * 0.85f;
            if (wet < 0.15f) wet = 0.15f;
            for (int s = 0; s < _sheetN; s++)
                if (_sheets[s] != null) Step(_sheets[s], dt, t, left, right, bottom, top, dusk, wet);
        }

        void Step(Sheet sheet, float dt, float t, float left, float right, float bottom, float top, float dusk, float wet)
        {
            int n = sheet.N;
            float fly = Mathf.SmoothStep(0.32f, 0.78f, dusk);
            for (int i = 0; i < n; i++)
            {
                byte kind = sheet.Kind[i];
                float x = sheet.X[i];
                float y = sheet.Y[i];
                if (kind == Cloud)
                {
                    // Build 85: rigid sideways drift that wraps off-screen on both sides (a cloud
                    // re-enters only once fully out of view, keeping the same spacing).
                    x += sheet.VX[i] * _cloud * dt;
                    float pad = sheet.HW[i] + 0.25f;
                    if (x > right + pad) x -= (right - left) + 2f * pad;
                }
                else if (kind == Glint)
                {
                    x += sheet.VX[i] * _shimmer * dt;
                    if (x > right + 0.4f) x = left - 0.4f;
                }
                else if (kind == Ground)
                {
                    float span = top - bottom;
                    if (sheet.Age[i] < 0f)
                    {
                        sheet.Rest[i] = bottom + span * sheet.Rest[i];
                        y = sheet.Rest[i];
                        sheet.Age[i] = 0f;
                    }
                    float rest = sheet.Rest[i];
                    float w = _wind;
                    // Calm: short creeping hops to the left. Gust: skid right to left, harder with the wind.
                    float hop = Mathf.Sin(t * 0.9f + sheet.Phase[i]);
                    hop = hop > 0f ? hop * hop * hop * hop : 0f;
                    float target = SeasonMotion.WindDir * sheet.Spin[i] * (GroundCreep * hop + GroundSkid * w * w);
                    float vx = sheet.VX[i];
                    vx += (target - vx) * Mathf.Min(1f, dt * 3f);
                    x += vx * dt;
                    // A strong gust lifts a few leaves (0.3..0.8 units) that drop back.
                    float vy = sheet.VY[i];
                    if (y <= rest + 0.001f && w > 0.55f && Mathf.Sin(t * 1.7f + sheet.Phase[i] * 3.1f) > 0.985f)
                    {
                        float h = Mathf.Lerp(LiftMin, LiftMax, Hash01(i, (int)(t * 2f))) * w;
                        vy = Mathf.Sqrt(2f * GroundGravity * h);
                    }
                    vy -= GroundGravity * dt;
                    y += vy * dt;
                    if (y < rest) { y = rest; vy = 0f; }
                    sheet.VY[i] = vy;
                    sheet.VX[i] = vx;
                    // Flip end over end while it moves.
                    sheet.Roll[i] += Mathf.Abs(vx) * 3.2f * dt + (y > rest ? 6f * dt : 0f);
                    if (x < left - 0.4f)
                    {
                        _wraps++;
                        x = right + 0.2f + Hash01(i, _wraps) * 0.8f;
                        y = rest;
                        sheet.VY[i] = 0f;
                    }
                    else if (x > right + 1.2f) x = left;
                }
                else if (kind == Streak)
                {
                    if (sheet.Age[i] < 0f)
                    {
                        sheet.Rest[i] = bottom + (top - bottom) * sheet.Rest[i];
                        sheet.Age[i] = 0f;
                        x = Mathf.Lerp(left, right, Hash01(i, 7));
                    }
                    y = sheet.Rest[i] + Mathf.Sin(t * 1.3f + sheet.Phase[i]) * 0.12f;
                    x += sheet.VX[i] * (0.4f + _wind) * dt;
                    if (x < left - sheet.HW[i] - 0.4f)
                    {
                        _wraps++;
                        x = right + sheet.HW[i] + Hash01(i, _wraps) * 2.5f;
                    }
                }
                else if (kind == Tumble)
                {
                    if (sheet.Phase[i] > 0f)
                    {
                        sheet.Phase[i] -= dt;
                        x = left - 1.6f;
                    }
                    else
                    {
                        x += sheet.VX[i] * _driftNow * dt;
                        y = -4.2f + Mathf.Sin(t * 2.2f * _driftNow + i) * 0.08f;
                        if (x > right + 1.2f)
                        {
                            x = left - 1.6f;
                            sheet.Phase[i] = 12f;
                        }
                    }
                }
                else
                {
                    float sway = Mathf.Sin(t * 0.7f * _driftNow + sheet.Phase[i]);
                    float flutter = kind == Wisp ? 0.04f : (kind == Petal || kind == Blossom ? 0.42f : (kind == Snow ? 0.55f : 0.18f));
                    bool falls = kind == Leaf || kind == Petal || kind == Blossom || kind == Snow;
                    float push = falls ? SeasonMotion.WindDir * _wind * _wind * FallWindPush : 0f;
                    float faster = falls ? 1f + 0.9f * _wind : 1f;
                    x += (sheet.VX[i] * _driftNow + sway * flutter + push) * dt;
                    y += sheet.VY[i] * _driftNow * faster * dt;
                    sheet.Age[i] += dt;
                    if (kind == Butterfly)
                    {
                        // Lazy loops: drift on two slow sines, wrap at the screen edges.
                        x += Mathf.Sin(t * 0.9f + sheet.Phase[i]) * 0.55f * dt;
                        y += Mathf.Cos(t * 1.3f + sheet.Phase[i] * 1.7f) * 0.40f * dt;
                        if (x < left) x = right;
                        else if (x > right) x = left;
                        if (y < bottom + 1.5f) y = bottom + 1.5f;
                        else if (y > top - 3f) y = top - 3f;
                    }
                    else if (kind == Fly || kind == Seed)
                    {
                        y += Mathf.Sin(t * 0.45f * _driftNow + sheet.Phase[i]) * 0.12f * _driftNow * dt;
                        if (x < left) x = right;
                        else if (x > right) x = left;
                        if (y < bottom) y = top;
                        else if (y > top) y = bottom;
                    }
                    else if (y < bottom - 0.4f || x < left - 0.6f)
                    {
                        _wraps++;
                        if (kind == Leaf || kind == Petal || kind == Blossom)
                        {
                            // Born in the close canopy overhead, mostly toward the trunks at either side.
                            float h = Hash01(i, _wraps);
                            float side = h < 0.5f ? h * 2f * 0.38f : 1f - (h - 0.5f) * 2f * 0.38f;
                            if (_wind > 0.35f) side = Mathf.Lerp(side, 0.55f + 0.45f * side, 0.5f); // gusts carry them in from the right
                            x = Mathf.Lerp(left, right, side);
                            y = top - 0.4f - Hash01(i + 31, _wraps) * CanopyDepth;
                            sheet.Age[i] = 0f;
                        }
                        else
                        {
                            y = top + 0.3f;
                            x = Mathf.Lerp(left, right, Hash01(i, _wraps));
                        }
                    }
                    else if (x > right + 0.6f)
                        x = left - 0.4f;
                }
                sheet.X[i] = x;
                sheet.Y[i] = y;

                byte a = sheet.A[i];
                if (kind == Glint)
                {
                    float w = 0.35f + 0.65f * Mathf.Sin(t * 2.2f * _shimmer + sheet.Phase[i]);
                    if (w < 0f) w = 0f;
                    int av = (int)(a * w * w * wet);
                    if (av < 0) av = 0;
                    else if (av > 255) av = 255;
                    a = (byte)av;
                }
                else if (kind == Fly)
                {
                    float w = 0.5f + 0.5f * Mathf.Sin(t * sheet.Spin[i] + sheet.Phase[i]);
                    float blink = w * w * w;
                    int av = (int)(210f * blink * fly * wet);
                    if (av < 0) av = 0;
                    else if (av > 255) av = 255;
                    a = (byte)av;
                }
                else if (kind == Tumble)
                    a = sheet.Phase[i] > 0f ? (byte)0 : (byte)(sheet.A[i] > 0 ? sheet.A[i] : 160);
                else if (kind == Ground)
                {
                    // Winter snow puffs show mostly while the wind drifts them.
                    float k = _seedScene == GardenScene.Winter ? 0.25f + 0.75f * _wind : 1f;
                    a = (byte)Mathf.Clamp((int)(sheet.A[i] * k), 0, 255);
                }
                else if (kind == Streak)
                {
                    float k = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 0.9f, _wind));
                    a = (byte)Mathf.Clamp((int)(sheet.A[i] * k), 0, 255);
                }
                else if (kind == Leaf || kind == Petal || kind == Blossom || kind == Snow)
                {
                    // Not dimmed by rain (LeafRainFloor); a newborn canopy leaf fades in over 0.5 s.
                    float k = Mathf.Max(wet, LeafRainFloor) * Mathf.Clamp01(sheet.Age[i] < 0f ? 1f : (sheet.Age[i] + 0.02f) / 0.5f);
                    int av = (int)(a * k);
                    a = (byte)(av < 0 ? 0 : (av > 255 ? 255 : av));
                }
                else
                {
                    int av = (int)(a * wet);
                    if (av < 0) av = 0;
                    else if (av > 255) av = 255;
                    a = (byte)av;
                }
                if (sheet.Burst)
                {
                    int bv = (int)(a * _burstA);
                    a = (byte)(bv < 0 ? 0 : (bv > 255 ? 255 : bv));
                }
                var col = new Color32(sheet.R[i], sheet.G[i], sheet.B[i], a);
                int v = i * 4;
                sheet.Cols[v] = sheet.Cols[v + 1] = sheet.Cols[v + 2] = sheet.Cols[v + 3] = col;

                float ang = (t * sheet.Spin[i] + sheet.Phase[i]);
                float hw = sheet.HW[i];
                if (kind == Butterfly)
                {
                    // Wings: width beats between 15% and 100%; the body only tilts.
                    hw *= 0.15f + 0.85f * Mathf.Abs(Mathf.Sin(ang));
                    ang = 0.3f * Mathf.Sin(t * 1.3f + sheet.Phase[i]);
                }
                if (kind == Ground)
                {
                    // End over end: the leaf turns edge-on as it rolls; a small tilt with the roll.
                    float roll = sheet.Roll[i];
                    if (_seedScene != GardenScene.Winter) hw *= 0.2f + 0.8f * Mathf.Abs(Mathf.Cos(roll));
                    ang = sheet.Phase[i] + 0.35f * Mathf.Sin(roll);
                }
                else if (kind == Streak)
                    ang = 0.06f; // leans with the rain
                if (kind == Snow)
                {
                    // Flip: the flake turns edge-on and back as it tumbles.
                    hw *= 0.15f + 0.85f * Mathf.Abs(Mathf.Cos(t * SnowFlipHz + sheet.Phase[i] * 1.7f));
                }
                float cs = Mathf.Cos(ang);
                float sn = Mathf.Sin(ang);
                float hh = sheet.HH[i];
                float drawY = y;
                Put(sheet.Verts, v, x, drawY, -hw, -hh, cs, sn);
                Put(sheet.Verts, v + 1, x, drawY, hw, -hh, cs, sn);
                Put(sheet.Verts, v + 2, x, drawY, -hw, hh, cs, sn);
                Put(sheet.Verts, v + 3, x, drawY, hw, hh, cs, sn);
            }
            sheet.Mesh.SetVertices(sheet.Verts, 0, sheet.Verts.Length, MeshUpdateFlags.DontRecalculateBounds);
            sheet.Mesh.SetColors(sheet.Cols, 0, sheet.Cols.Length, MeshUpdateFlags.DontRecalculateBounds);
        }

        void HoldStill()
        {
            for (int s = 0; s < _sheetN; s++)
            {
                var sheet = _sheets[s];
                if (sheet == null || sheet.Cols == null) continue;
                for (int i = 0; i < sheet.Cols.Length; i++)
                {
                    var c = sheet.Cols[i];
                    c.a = 0;
                    sheet.Cols[i] = c;
                }
                if (sheet.Mesh != null)
                    sheet.Mesh.SetColors(sheet.Cols, 0, sheet.Cols.Length, MeshUpdateFlags.DontRecalculateBounds);
                if (sheet.Rend != null) sheet.Rend.enabled = false;
            }
        }

        void ShowAgain()
        {
            for (int s = 0; s < _sheetN; s++)
            {
                var sheet = _sheets[s];
                if (sheet != null && sheet.Rend != null) sheet.Rend.enabled = true;
            }
        }

        static void Put(Vector3[] verts, int v, float cx, float cy, float x, float y, float c, float s)
        {
            verts[v].x = cx + x * c - y * s;
            verts[v].y = cy + x * s + y * c;
            verts[v].z = 0f;
        }

        static float Hash01(int i, int n)
        {
            uint h = (uint)(i * 374761393 + n * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            return (h & 65535) / 65535f;
        }

        void Frame(out float left, out float right, out float bottom, out float top)
        {
            var cam = _cam;
            float half = cam != null ? cam.orthographicSize : WorldBuilder.CamOrtho;
            float y = cam != null ? cam.transform.position.y : WorldBuilder.CamRestY;
            float x = cam != null ? cam.transform.position.x : 0f;
            half *= PortraitLock.TallFactor();
            float w = half * (cam != null ? cam.aspect : WorldBuilder.PortraitAspect);
            left = x - w - 0.4f;
            right = x + w + 0.4f;
            bottom = y - half - 0.4f;
            top = y + half + 0.4f;
        }

        void OnDestroy()
        {
            if (_sheets == null) return;
            for (int i = 0; i < _sheetN; i++)
            {
                var sheet = _sheets[i];
                if (sheet == null) continue;
                if (sheet.Mesh != null) Destroy(sheet.Mesh);
                if (sheet.Mat != null) Destroy(sheet.Mat);
            }
        }
    }
}
