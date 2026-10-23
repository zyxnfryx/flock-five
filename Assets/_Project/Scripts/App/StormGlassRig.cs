using UnityEngine;

namespace FlockFive
{
    public sealed partial class FlockFiveApp
    {
        // Splash, hive, poker, and the badger page are not storm gardens.
        // True from the moment a level is solved until the next garden loads (win, scoring, finale, Double/Collect).
        public static bool RainHalted => _app != null && StormGlass.Halted(_app._won, _app._finaleHold, _app._gardenScoring);

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
        float _haltT;
        bool _wasHalted;
        static float _haltMul = 1f;
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
            _haltT = 0f;
            _wasHalted = false;
            _haltMul = 1f;
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
            if (cam == null || PhoneArea.W < 2 || PhoneArea.H < 2)
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
            bool open = FlockFiveApp.GardenOpen;
            bool halted = open && FlockFiveApp.RainHalted;
            if (halted)
            {
                _haltT += PlayClock.Delta;
                _wasHalted = true;
            }
            else
            {
                // The next garden starts fresh: nothing from the solved level survives into it.
                if (_wasHalted)
                {
                    StormGlass.Clear(_beads, ref _count, _specks, ref _speckN);
                    _spawnIn = 0.6f;
                    _rng.State = _rng.State * 1664525u + 1013904223u;
                    _wasHalted = false;
                }
                _haltT = 0f;
            }
            _haltMul = StormGlass.HaltMul(_haltT);
            // Halted: no new drops (wet 0), live ones ease out, then the pool is dropped for good.
            bool garden = open && !(halted && _haltT >= StormGlass.HaltFade);
            _count = StormGlass.Tick(
                _beads, _count, PlayClock.Delta,
                garden, halted ? 0f : GardenStorm.Wet,
                area.x, area.y, area.width, area.height,
                _blocks, _blockN, ref _rng, ref _spawnIn,
                _specks, ref _speckN);
            int vis = 0;
            for (int i = 0; i < _count; i++)
            {
                if (StormGlass.Alpha(_beads[i].Age, _beads[i].Life, _beads[i].Kind) * _haltMul > 0.02f)
                    vis++;
            }
            for (int i = 0; i < _speckN; i++)
            {
                if (StormGlass.SpeckAlpha(_specks[i].Age, _specks[i].Life) * _haltMul > 0.02f)
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
            float formScale = StormGlass.ScaleFor(area.height);
            for (int i = 0; i < speckN && n < cap; i++)
            {
                float a = StormGlass.SpeckAlpha(specks[i].Age, specks[i].Life);
                a *= StormGlass.CoverMul(specks[i].X, specks[i].Y, blocks, blockN) * _haltMul;
                if (a < 0.02f) continue;
                float r = StormGlass.SpeckRadius(specks[i].Age, specks[i].Radius);
                if (r < 0.5f) continue;
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
                    a *= StormGlass.CoverMul(src[i].X, src[i].Y, blocks, blockN) * _haltMul;
                    if (a < 0.02f) continue;
                    StormGlass.Form(src[i], formScale, out float prx, out float pry, out float splat, out float tailPx);
                    if (prx < 0.5f || pry < 0.5f) continue;
                    if (!Write(area, texW, texH, src[i].X, src[i].Y, prx, pry, a, splat,
                            src[i].Kind == StormGlass.Slide ? src[i].Vx : 0f,
                            src[i].Kind == StormGlass.Slide ? src[i].Vy : 0f,
                            tailPx,
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
                a *= StormGlass.CoverMul(b.X, b.Y, blocks, blockN) * _haltMul;
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

        static Texture2D _paintHi;
        static Texture2D _paintRim;
        static readonly Live[] _paintLive = new Live[StormGlass.DrawCap];

        static Texture2D MakeSoft(bool rim)
        {
            const int N = 64;
            var px = new Color32[N * N];
            float c = (N - 1) * 0.5f;
            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    float dx = (x - c) / c;
                    float dy = (y - c) / c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a;
                    if (rim) a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.86f) / 0.14f) * (d <= 1f ? 1f : 0f);
                    else
                    {
                        // soft glint, brightest toward the upper left, fading to the edge
                        float gx = (x - c * 0.6f) / c;
                        float gy = (y - c * 1.4f) / c;
                        float g = Mathf.Clamp01(1f - Mathf.Sqrt(gx * gx + gy * gy) / 0.7f);
                        a = g * g * (d <= 1f ? 1f : 0f);
                    }
                    px[y * N + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
                }
            }
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false);
            t.hideFlags = HideFlags.HideAndDontSave;
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            t.SetPixels32(px);
            t.Apply(false, true);
            return t;
        }

        // Droplets over the restart button and the hive. The lens pass is under IMGUI, so those two controls would hide
        // the drops; this paints a soft highlight and rim with each drop's alpha and radius. DrawTexture only: no controls, no input.
        public static void PaintOver(Rect restart, Rect hive)
        {
            if (Event.current == null || Event.current.type != EventType.Repaint) return;
            if (_rig == null) return;
            bool r0 = restart.width >= 1f && restart.height >= 1f;
            bool r1 = hive.width >= 1f && hive.height >= 1f;
            if (!r0 && !r1) return;
            int n = CopyLive(_paintLive);
            if (n <= 0) return;
            if (_paintHi == null) _paintHi = MakeSoft(false);
            if (_paintRim == null) _paintRim = MakeSoft(true);
            float h = PhoneArea.H;
            var keep = GUI.color;
            for (int i = 0; i < n; i++)
            {
                float gx = _paintLive[i].X;
                float gy = h - _paintLive[i].Y;
                float r = _paintLive[i].Radius;
                float a = _paintLive[i].Alpha;
                if (r < 1f) continue;
                bool over = (r0 && Grow(restart, r).Contains(new Vector2(gx, gy)))
                    || (r1 && Grow(hive, r).Contains(new Vector2(gx, gy)));
                if (!over) continue;
                var box = new Rect(gx - r, gy - r, r * 2f, r * 2f);
                GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(a * 0.5f));
                GUI.DrawTexture(box, _paintHi, ScaleMode.StretchToFill, true);
                GUI.color = new Color(0.85f, 0.92f, 1f, Mathf.Clamp01(a * 0.42f));
                GUI.DrawTexture(box, _paintRim, ScaleMode.StretchToFill, true);
            }
            GUI.color = keep;
        }

        static Rect Grow(Rect r, float by) => new Rect(r.x - by, r.y - by, r.width + by * 2f, r.height + by * 2f);

        static int Gather(Camera cam, StormGlass.Block[] blocks)
        {
            int n = 0;
            if (FlockFiveApp.GardenOpen)
            {
                float h = PhoneArea.H;
                var safe = PhoneArea.Safe;
                CornerAnchor.Garden(PhoneArea.W, PhoneArea.H, safe, out float scale, out _, out _, out _, out _);
                var pivot = new Vector2(PhoneArea.W * 0.5f, PhoneArea.H * 0.5f);
                var stamp = FlockFiveApp.GardenStampBox(PhoneArea.W, PhoneArea.H, safe, scale);
                stamp = CornerAnchor.Shake(stamp, CamShake.HudOffset, CamShake.HudTwist, pivot);
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
