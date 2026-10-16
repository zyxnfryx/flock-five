using UnityEngine;

namespace FlockFive
{
    public sealed partial class FlockFiveApp
    {
        // Splash, hive, poker, and the badger page are not storm gardens.
        public static bool GardenOpen => _app != null && !_app._splash && _app._board != null;

        // One block per perched bird, hawk, sparrow, or ostrich. Walks the
        // roster the garden already keeps, so the glass does not allocate.
        public static int CopyGlassCritters(Camera cam, StormGlass.Block[] blocks, int n)
        {
            if (_app == null || cam == null || blocks == null) return n;
            var branches = _app._garden.Branches;
            if (branches != null)
            {
                for (int i = 0; i < branches.Length; i++)
                {
                    var br = branches[i];
                    if (br == null) continue;
                    var birds = br.Birds;
                    for (int k = 0; k < birds.Length; k++)
                    {
                        var sr = birds[k];
                        if (sr == null || !sr.enabled || !sr.gameObject.activeInHierarchy) continue;
                        n = StormGlassRig.AddHost(cam, sr.transform, blocks, n);
                        if (n >= blocks.Length) return n;
                    }
                }
            }
            if (HawkView.Live != null)
            {
                n = StormGlassRig.AddHost(cam, HawkView.Live.transform, blocks, n);
                if (n >= blocks.Length) return n;
            }
            if (SparrowView.Live != null)
            {
                n = StormGlassRig.AddHost(cam, SparrowView.Live.transform, blocks, n);
                if (n >= blocks.Length) return n;
            }
            var ostrich = _app._garden.Ostrich;
            if (ostrich != null)
                n = StormGlassRig.AddHost(cam, ostrich.transform, blocks, n);
            return n;
        }
    }

    // Ticks the glass and hands the live lenses to the pass. No colliders,
    // no canvas, no raycast target: taps fall through to the garden.
    public sealed class StormGlassRig : MonoBehaviour
    {
        public struct Live
        {
            public float X;
            public float Y;
            public float Radius;
            public float Alpha;
            public float Vy;
            public float Splat;
            public byte Kind;
        }

        const int BlockCap = 96;
        // Splat slot value that tells the shader this is a residual speck: no hot glint.
        const float SpeckFlag = -1f;
        static StormGlassRig _rig;
        static int _visible;

        StormGlass.Bead[] _beads = new StormGlass.Bead[StormGlass.BeadCap];
        StormGlass.Speck[] _specks = new StormGlass.Speck[StormGlass.SpeckCap];
        StormGlass.Block[] _blocks = new StormGlass.Block[BlockCap];
        StormGlass.Rng _rng;
        float _spawnIn = 0.05f;
        int _count;
        int _speckN;
        int _blockN;
        bool _logged;

        public static bool WantDraw => _visible > 0;

        void OnEnable()
        {
            _rig = this;
            _rng.State = 0x5EED1u;
            _spawnIn = 0.05f;
            StormGlass.Clear(_beads, ref _count, _specks, ref _speckN);
            _visible = 0;
            StormGlassFeature.Install();
        }

        void OnDisable()
        {
            if (_rig == this) _rig = null;
            StormGlass.Clear(_beads, ref _count, _specks, ref _speckN);
            _visible = 0;
            StormGlassFeature.Uninstall();
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null || Screen.width < 2 || Screen.height < 2)
            {
                _visible = 0;
                return;
            }
            var area = cam.pixelRect;
            if (area.width < 2f || area.height < 2f)
            {
                _visible = 0;
                return;
            }
            _blockN = Gather(cam, _blocks);
            bool garden = FlockFiveApp.GardenOpen;
            _count = StormGlass.Tick(
                _beads, _count, PlayClock.Delta,
                garden, GardenStorm.Wet,
                area.x, area.y, area.width, area.height,
                _blocks, _blockN, ref _rng, ref _spawnIn,
                _specks, ref _speckN);
            int vis = 0;
            for (int i = 0; i < _count; i++)
            {
                if (StormGlass.Alpha(_beads[i].Age, _beads[i].Life, _beads[i].Kind) > 0.02f)
                    vis++;
            }
            for (int i = 0; i < _speckN; i++)
            {
                if (StormGlass.SpeckAlpha(_specks[i].Age, _specks[i].Life) > 0.02f)
                    vis++;
            }
            _visible = vis;
        }

        // Screen pixels, bottom-left. The lens pass maps these into the color texture.
        public static int Fill(Camera cam, int texW, int texH, Vector4[] rects, Vector4[] beads)
        {
            if (_rig == null || cam == null || texW < 2 || texH < 2) return 0;
            if (rects == null || beads == null) return 0;
            var area = cam.pixelRect;
            if (area.width < 2f || area.height < 2f) return 0;
            int cap = rects.Length < beads.Length ? rects.Length : beads.Length;
            if (cap > StormGlass.DrawCap) cap = StormGlass.DrawCap;
            int n = 0;
            var src = _rig._beads;
            int count = _rig._count;
            var blocks = _rig._blocks;
            int blockN = _rig._blockN;
            var specks = _rig._specks;
            int speckN = _rig._speckN;
            for (int i = 0; i < speckN && n < cap; i++)
            {
                float a = StormGlass.SpeckAlpha(specks[i].Age, specks[i].Life);
                a *= StormGlass.CoverMul(specks[i].X, specks[i].Y, blocks, blockN);
                if (a < 0.02f) continue;
                float r = specks[i].Radius;
                if (!Write(area, texW, texH, specks[i].X, specks[i].Y, r, r, a, SpeckFlag, 0f, 0f, 0f,
                        rects, beads, ref n))
                    break;
            }
            // Small beads first so a fat merge is the one you see on top.
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < count && n < cap; i++)
                {
                    bool fat = src[i].Radius >= 36f;
                    if (pass == 0 && fat) continue;
                    if (pass == 1 && !fat) continue;
                    float a = StormGlass.Alpha(src[i].Age, src[i].Life, src[i].Kind);
                    a *= StormGlass.CoverMul(src[i].X, src[i].Y, blocks, blockN);
                    if (a < 0.02f) continue;
                    StormGlass.Shape(src[i].Age, src[i].Radius, out float prx, out float pry, out float splat);
                    if (!Write(area, texW, texH, src[i].X, src[i].Y, prx, pry, a, splat,
                            src[i].Kind == StormGlass.Slide ? src[i].Vx : 0f,
                            src[i].Kind == StormGlass.Slide ? src[i].Vy : 0f,
                            src[i].Radius * 4.0f * (1f - splat),
                            rects, beads, ref n))
                        break;
                }
            }
            if (n > 0 && !_rig._logged)
            {
                _rig._logged = true;
                Debug.Log("STORM_GLASS beads " + n
                    + " cam " + area.x.ToString("0") + "," + area.y.ToString("0")
                    + " " + area.width.ToString("0") + "x" + area.height.ToString("0")
                    + " tex " + texW + "x" + texH
                    + " rect " + rects[0].ToString("0.000")
                    + " bead " + beads[0].ToString("0.000"));
            }
            return n;
        }

        static bool Write(
            Rect area, int texW, int texH,
            float x, float y, float prx, float pry, float alpha, float splat,
            float vx, float vy, float trailPx,
            Vector4[] rects, Vector4[] beads, ref int n)
        {
            if (n >= rects.Length || n >= beads.Length) return false;
            float cx = (x - area.x) / area.width * texW;
            float cy = (y - area.y) / area.height * texH;
            float rx = prx / area.width * texW;
            float ry = pry / area.height * texH;
            if (cx < -rx || cy < -ry || cx > texW + rx || cy > texH + ry) return true;
            float scaleX = rx * 2f / texW;
            float scaleY = ry * 2f / texH;
            float offX = (cx - rx) / texW;
            float offY = (cy - ry) / texH;
            rects[n] = new Vector4(scaleX, scaleY, offX, offY);
            float trailU = 0f;
            float trailV = 0f;
            float sp = Mathf.Sqrt(vx * vx + vy * vy);
            if (sp > 1f && trailPx > 1f)
            {
                float len = trailPx / sp;
                trailU = -vx * len / area.width;
                trailV = -vy * len / area.height;
            }
            beads[n] = new Vector4(trailU, trailV, alpha, splat);
            n++;
            return true;
        }

        public static int CopyLive(Live[] into)
        {
            if (_rig == null || into == null) return 0;
            int n = 0;
            int count = _rig._count;
            var blocks = _rig._blocks;
            int blockN = _rig._blockN;
            for (int i = 0; i < count && n < into.Length; i++)
            {
                var b = _rig._beads[i];
                float a = StormGlass.Alpha(b.Age, b.Life, b.Kind);
                a *= StormGlass.CoverMul(b.X, b.Y, blocks, blockN);
                if (a < 0.02f) continue;
                StormGlass.Shape(b.Age, b.Radius, out _, out _, out float splat);
                into[n++] = new Live
                {
                    X = b.X,
                    Y = b.Y,
                    Radius = StormGlass.ShownRadius(b.Age, b.Radius),
                    Alpha = a,
                    Vy = b.Vy,
                    Splat = splat,
                    Kind = b.Kind
                };
            }
            return n;
        }

        static int Gather(Camera cam, StormGlass.Block[] blocks)
        {
            int n = 0;
            if (FlockFiveApp.GardenOpen)
            {
                float h = Screen.height;
                var safe = Screen.safeArea;
                CornerAnchor.Garden(Screen.width, Screen.height, safe, out float scale, out _, out _, out var restart, out var hive);
                var pivot = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                restart = CornerAnchor.Shake(restart, CamShake.HudOffset, CamShake.HudTwist, pivot);
                hive = CornerAnchor.Shake(hive, CamShake.HudOffset, CamShake.HudTwist, pivot);
                var stamp = FlockFiveApp.GardenStampBox(Screen.width, Screen.height, safe, scale);
                stamp = CornerAnchor.Shake(stamp, CamShake.HudOffset, CamShake.HudTwist, pivot);
                Add(blocks, ref n, StormGlass.FromGui(restart, h));
                Add(blocks, ref n, StormGlass.FromGui(hive, h));
                Add(blocks, ref n, StormGlass.FromGui(stamp, h));
            }
            return FlockFiveApp.CopyGlassCritters(cam, blocks, n);
        }

        internal static int AddHost(Camera cam, Transform host, StormGlass.Block[] blocks, int n)
        {
            if (host == null || cam == null || blocks == null || n >= blocks.Length) return n;
            bool any = false;
            Bounds box = default;
            UnionSprites(host, ref any, ref box);
            if (!any) return n;
            var p0 = cam.WorldToScreenPoint(new Vector3(box.min.x, box.min.y, box.center.z));
            var p1 = cam.WorldToScreenPoint(new Vector3(box.max.x, box.max.y, box.center.z));
            if (p0.z < 0f && p1.z < 0f) return n;
            float x0 = Mathf.Min(p0.x, p1.x);
            float y0 = Mathf.Min(p0.y, p1.y);
            float x1 = Mathf.Max(p0.x, p1.x);
            float y1 = Mathf.Max(p0.y, p1.y);
            Add(blocks, ref n, new StormGlass.Block
            {
                X = x0,
                Y = y0,
                W = Mathf.Max(1f, x1 - x0),
                H = Mathf.Max(1f, y1 - y0)
            });
            return n;
        }

        static void UnionSprites(Transform t, ref bool any, ref Bounds box)
        {
            var sr = t.GetComponent<SpriteRenderer>();
            if (sr != null && sr.enabled && sr.sprite != null && sr.color.a >= 0.05f)
            {
                if (!any)
                {
                    box = sr.bounds;
                    any = true;
                }
                else box.Encapsulate(sr.bounds);
            }
            int c = t.childCount;
            for (int i = 0; i < c; i++)
                UnionSprites(t.GetChild(i), ref any, ref box);
        }

        static void Add(StormGlass.Block[] blocks, ref int n, StormGlass.Block block)
        {
            if (n >= blocks.Length || block.W < 1f || block.H < 1f) return;
            blocks[n++] = block;
        }
    }
}
