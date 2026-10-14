using UnityEngine;

namespace FlockFive
{
    public sealed partial class FlockFiveApp
    {
        // Splash, hive, poker, and the badger page are not storm gardens.
        public static bool GardenOpen => _app != null && !_app._splash && _app._board != null;
    }

    // Ticks the glass beads and hands the live ones to the lens pass.
    public sealed class StormGlassRig : MonoBehaviour
    {
        public struct Live
        {
            public float X;
            public float Y;
            public float Radius;
            public float Alpha;
        }

        const int BlockCap = 96;
        static StormGlassRig _rig;
        static int _visible;

        StormGlass.Bead[] _beads = new StormGlass.Bead[StormGlass.Cap];
        StormGlass.Block[] _blocks = new StormGlass.Block[BlockCap];
        StormGlass.Rng _rng;
        float _spawnIn = 0.05f;
        int _count;
        bool _logged;

        public static bool WantDraw => _visible > 0;

        void OnEnable()
        {
            _rig = this;
            _rng.State = 0x5EED1u;
            _spawnIn = 0.05f;
            _count = 0;
            _visible = 0;
            StormGlassFeature.Install();
        }

        void OnDisable()
        {
            if (_rig == this) _rig = null;
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
            int blocks = Gather(cam, _blocks);
            bool garden = FlockFiveApp.GardenOpen;
            _count = StormGlass.Tick(
                _beads, _count, PlayClock.Delta,
                garden, GardenStorm.Wet,
                area.x, area.y, area.width, area.height,
                _blocks, blocks, ref _rng, ref _spawnIn);
            int vis = 0;
            for (int i = 0; i < _count; i++)
            {
                if (StormGlass.Alpha(_beads[i].Age, _beads[i].Life) > 0.02f)
                    vis++;
            }
            _visible = vis;
        }

        // Screen pixels, bottom-left. The lens pass maps these into the color texture.
        public static int Fill(Camera cam, int texW, int texH, Vector4[] rects, Vector4[] beads)
        {
            if (_rig == null || cam == null || texW < 2 || texH < 2) return 0;
            var area = cam.pixelRect;
            if (area.width < 2f || area.height < 2f) return 0;
            int n = 0;
            int count = _rig._count;
            var src = _rig._beads;
            for (int i = 0; i < count && n < rects.Length && n < beads.Length; i++)
            {
                float a = StormGlass.Alpha(src[i].Age, src[i].Life);
                if (a < 0.02f) continue;
                float rad = StormGlass.ShownRadius(src[i].Age, src[i].Radius);
                float cx = (src[i].X - area.x) / area.width * texW;
                float cy = (src[i].Y - area.y) / area.height * texH;
                float rx = rad / area.width * texW;
                float ry = rad / area.height * texH;
                float scaleX = rx * 2f / texW;
                float scaleY = ry * 2f / texH;
                float offX = (cx - rx) / texW;
                // Bottom-left, same as the sim and the screenshot. A top-left
                // offY put the bead on the opposite half of the glass.
                float offY = (cy - ry) / texH;
                rects[n] = new Vector4(scaleX, scaleY, offX, offY);
                beads[n] = new Vector4(offX + scaleX * 0.5f, offY + scaleY * 0.5f, a, 0f);
                n++;
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

        public static int CopyLive(Live[] into)
        {
            if (_rig == null || into == null) return 0;
            int n = 0;
            int count = _rig._count;
            for (int i = 0; i < count && n < into.Length; i++)
            {
                float a = StormGlass.Alpha(_rig._beads[i].Age, _rig._beads[i].Life);
                if (a < 0.02f) continue;
                into[n++] = new Live
                {
                    X = _rig._beads[i].X,
                    Y = _rig._beads[i].Y,
                    Radius = StormGlass.ShownRadius(_rig._beads[i].Age, _rig._beads[i].Radius),
                    Alpha = a
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
            AddBirds(cam, blocks, ref n);
            return n;
        }

        static void AddBirds(Camera cam, StormGlass.Block[] blocks, ref int n)
        {
            var birds = Object.FindObjectsByType<BirdIdle>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < birds.Length; i++)
                AddSprites(cam, birds[i], blocks, ref n);
            var hawks = Object.FindObjectsByType<HawkView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < hawks.Length; i++)
                AddSprites(cam, hawks[i], blocks, ref n);
            var sparrows = Object.FindObjectsByType<SparrowView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < sparrows.Length; i++)
                AddSprites(cam, sparrows[i], blocks, ref n);
            var ostriches = Object.FindObjectsByType<OstrichMark>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < ostriches.Length; i++)
                AddSprites(cam, ostriches[i], blocks, ref n);
        }

        static void AddSprites(Camera cam, MonoBehaviour host, StormGlass.Block[] blocks, ref int n)
        {
            if (host == null || n >= blocks.Length) return;
            var sprites = host.GetComponentsInChildren<SpriteRenderer>(false);
            for (int i = 0; i < sprites.Length; i++)
            {
                if (n >= blocks.Length) return;
                var sr = sprites[i];
                if (sr == null || !sr.enabled || sr.sprite == null) continue;
                if (sr.color.a < 0.05f) continue;
                var b = sr.bounds;
                var p0 = cam.WorldToScreenPoint(new Vector3(b.min.x, b.min.y, b.center.z));
                var p1 = cam.WorldToScreenPoint(new Vector3(b.max.x, b.max.y, b.center.z));
                if (p0.z < 0f && p1.z < 0f) continue;
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
            }
        }

        static void Add(StormGlass.Block[] blocks, ref int n, StormGlass.Block block)
        {
            if (n >= blocks.Length || block.W < 1f || block.H < 1f) return;
            blocks[n++] = block;
        }
    }
}
