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

        sealed class Sheet
        {
            public Mesh Mesh;
            public MeshRenderer Rend;
            public Vector3[] Verts;
            public Color32[] Cols;
            public float[] X, Y, VX, VY, Phase, Spin, HW, HH;
            public byte[] Kind;
            public byte[] R, G, B, A;
            public int N;
            public Material Mat;
        }

        static Transform _homeRoot;
        static readonly int SpritePropsId = Shader.PropertyToID("unity_SpriteProps");
        static readonly int SpriteColorId = Shader.PropertyToID("unity_SpriteColor");

        Sheet[] _sheets;
        int _sheetN;
        Camera _cam;
        bool _home;
        int _wraps;

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
            _sheets = new Sheet[2];
            _sheetN = 0;
            var rng = new System.Random(scene == GardenScene.Desert ? 29 : 17 + (int)scene * 3);
            if (scene == GardenScene.Winter)
                AddSheet(SpriteCatalog.Glow, low ? 8 : 18, Snow, rng);
            else if (scene == GardenScene.Fall)
                AddSheet(SpriteCatalog.PetalPeach, low ? 3 : 6, Leaf, rng);
            else if (scene == GardenScene.Spring)
                AddSheet(SpriteCatalog.PetalPink, low ? 3 : 7, Petal, rng);
            else if (scene == GardenScene.Desert)
            {
                AddSheet(SpriteCatalog.Glow, low ? 3 : 6, Wisp, rng);
                AddSheet(SpriteCatalog.PetalPeach, 1, Tumble, rng);
            }
            else
            {
                AddSheet(SpriteCatalog.Firefly, low ? 3 : 6, Fly, rng);
                AddSheet(SpriteCatalog.Glow, low ? 2 : 5, Seed, rng);
            }
        }

        void AddSheet(Sprite spr, int n, byte kind, System.Random rng)
        {
            if (spr == null || n <= 0 || _sheetN >= _sheets.Length) return;
            var sheet = new Sheet();
            sheet.N = n;
            sheet.X = new float[n];
            sheet.Y = new float[n];
            sheet.VX = new float[n];
            sheet.VY = new float[n];
            sheet.Phase = new float[n];
            sheet.Spin = new float[n];
            sheet.HW = new float[n];
            sheet.HH = new float[n];
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
                sheet.HW[i] = Mathf.Lerp(0.035f, 0.07f, u);
                sheet.HH[i] = sheet.HW[i];
                sheet.VY[i] = -(0.28f + 0.45f * (float)rng.NextDouble());
                sheet.VX[i] = Mathf.Lerp(-0.15f, 0.15f, (float)rng.NextDouble());
                sheet.Spin[i] = Mathf.Lerp(-0.6f, 0.6f, (float)rng.NextDouble());
                byte w = (byte)(210 + (int)(u * 40f));
                sheet.R[i] = w;
                sheet.G[i] = w;
                sheet.B[i] = 235;
                sheet.A[i] = 150;
            }
            else if (kind == Leaf)
            {
                sheet.HW[i] = Mathf.Lerp(0.10f, 0.16f, u);
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
                sheet.HW[i] = Mathf.Lerp(0.07f, 0.12f, u);
                sheet.HH[i] = sheet.HW[i] * 1.4f;
                sheet.VY[i] = -(0.12f + 0.16f * (float)rng.NextDouble());
                sheet.VX[i] = Mathf.Lerp(-0.18f, 0.18f, (float)rng.NextDouble());
                sheet.Spin[i] = Mathf.Lerp(-0.8f, 0.8f, (float)rng.NextDouble());
                bool peach = rng.NextDouble() > 0.55;
                sheet.R[i] = 255;
                sheet.G[i] = peach ? (byte)210 : (byte)170;
                sheet.B[i] = peach ? (byte)190 : (byte)185;
                sheet.A[i] = 150;
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

        void LateUpdate()
        {
            if (_sheetN == 0) return;
            float dt = PlayClock.Delta;
            if (dt <= 0f) return;
            float t = PlayClock.Now;
            Frame(out float left, out float right, out float bottom, out float top);
            float dusk = SkyCycle.Dusk;
            if (_home && dusk < 0.62f) dusk = 0.62f;
            float wet = 1f - GardenStorm.Wet * 0.85f;
            if (wet < 0.15f) wet = 0.15f;
            for (int s = 0; s < _sheetN; s++)
                Step(_sheets[s], dt, t, left, right, bottom, top, dusk, wet);
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
                if (kind == Tumble)
                {
                    if (sheet.Phase[i] > 0f)
                    {
                        sheet.Phase[i] -= dt;
                        x = left - 1.6f;
                    }
                    else
                    {
                        x += sheet.VX[i] * dt;
                        y = -4.2f + Mathf.Sin(t * 2.2f + i) * 0.08f;
                        if (x > right + 1.2f)
                        {
                            x = left - 1.6f;
                            sheet.Phase[i] = 12f;
                        }
                    }
                }
                else
                {
                    float sway = Mathf.Sin(t * 0.7f + sheet.Phase[i]);
                    x += (sheet.VX[i] + sway * (kind == Wisp ? 0.04f : 0.18f)) * dt;
                    y += sheet.VY[i] * dt;
                    if (kind == Fly || kind == Seed)
                    {
                        y += Mathf.Sin(t * 0.45f + sheet.Phase[i]) * 0.12f * dt;
                        if (x < left) x = right;
                        else if (x > right) x = left;
                        if (y < bottom) y = top;
                        else if (y > top) y = bottom;
                    }
                    else if (y < bottom - 0.4f)
                    {
                        y = top + 0.3f;
                        _wraps++;
                        x = Mathf.Lerp(left, right, Hash01(i, _wraps));
                    }
                    else if (x > right + 0.6f)
                        x = left - 0.4f;
                }
                sheet.X[i] = x;
                sheet.Y[i] = y;

                byte a = sheet.A[i];
                if (kind == Fly)
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
                else
                {
                    int av = (int)(a * wet);
                    if (av < 0) av = 0;
                    else if (av > 255) av = 255;
                    a = (byte)av;
                }
                var col = new Color32(sheet.R[i], sheet.G[i], sheet.B[i], a);
                int v = i * 4;
                sheet.Cols[v] = sheet.Cols[v + 1] = sheet.Cols[v + 2] = sheet.Cols[v + 3] = col;

                float ang = (t * sheet.Spin[i] + sheet.Phase[i]);
                float cs = Mathf.Cos(ang);
                float sn = Mathf.Sin(ang);
                float hw = sheet.HW[i];
                float hh = sheet.HH[i];
                Put(sheet.Verts, v, x, y, -hw, -hh, cs, sn);
                Put(sheet.Verts, v + 1, x, y, hw, -hh, cs, sn);
                Put(sheet.Verts, v + 2, x, y, -hw, hh, cs, sn);
                Put(sheet.Verts, v + 3, x, y, hw, hh, cs, sn);
            }
            sheet.Mesh.SetVertices(sheet.Verts, 0, sheet.Verts.Length, MeshUpdateFlags.DontRecalculateBounds);
            sheet.Mesh.SetColors(sheet.Cols, 0, sheet.Cols.Length, MeshUpdateFlags.DontRecalculateBounds);
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
