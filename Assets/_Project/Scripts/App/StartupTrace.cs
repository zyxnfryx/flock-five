using System.Diagnostics;
using System.Globalization;
using UnityEngine;

namespace FlockFive
{
    // First seconds after launch. Records named steps and the gap between
    // Updates so a hitch in the home halo shows up as one line:
    // FF_STARTUP worst=<ms> frame=<n> boot=<ms> frames=<n> top=<name>:<ms>,...
    // worst is the slowest Update-to-Update gap after the first (that first
    // gap is boot: scene setup and Start). Steps noted since the previous
    // Update belong to the gap that just closed.
    public static class StartupTrace
    {
        public const double WindowSec = 5.0;
        const int StackMax = 16;
        const int HitMax = 48;

        struct Hit
        {
            public string Name;
            public double Ms;
            public int Frame;
        }

        static readonly long[] _t0 = new long[StackMax];
        static readonly string[] _name = new string[StackMax];
        static readonly Hit[] _hits = new Hit[HitMax];
        static int _depth;
        static int _hitN;
        static double _worst = -1;
        static int _worstFrame;
        static string _worstSteps;
        static double _boot = -1;
        static string _bootSteps;
        static int _frames;
        static long _frameT0;
        static double _origin;
        static bool _originSet;
        static bool _closed;
        static string _frameSteps;

        public readonly struct Span : System.IDisposable
        {
            readonly int _slot;
            public Span(int slot) { _slot = slot; }
            public void Dispose()
            {
                if (_slot > 0) End(_slot);
            }
        }

        public static bool Open => !_closed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot() => Arm();

        public static void Arm()
        {
            if (_originSet || _closed) return;
            _originSet = true;
            _origin = Time.realtimeSinceStartupAsDouble;
            _frameT0 = Stopwatch.GetTimestamp();
        }

        public static Span Begin(string name)
        {
            if (_closed || string.IsNullOrEmpty(name)) return default;
            Arm();
            if (_depth >= StackMax) return default;
            _name[_depth] = name;
            _t0[_depth] = Stopwatch.GetTimestamp();
            _depth++;
            return new Span(_depth);
        }

        // Sprite loads only pay for a string while the window is open.
        public static Span BeginSprite(string path)
        {
            if (_closed) return default;
            return Begin("Sprite:" + path);
        }

        static void End(int slot)
        {
            if (_closed || slot <= 0 || slot != _depth) return;
            int i = slot - 1;
            long dt = Stopwatch.GetTimestamp() - _t0[i];
            double ms = dt * 1000.0 / Stopwatch.Frequency;
            string name = _name[i];
            _depth = i;
            Note(name, ms, Time.frameCount);
        }

        public static void Note(string name, double ms, int frame)
        {
            if (_closed || ms < 0.05 || string.IsNullOrEmpty(name)) return;
            if (_hitN < HitMax)
                _hits[_hitN++] = new Hit { Name = name, Ms = ms, Frame = frame };
            else
            {
                int small = 0;
                for (int i = 1; i < _hitN; i++)
                    if (_hits[i].Ms < _hits[small].Ms) small = i;
                if (ms <= _hits[small].Ms) return;
                _hits[small] = new Hit { Name = name, Ms = ms, Frame = frame };
            }
            if (ms < 1.0) return;
            if (_frameSteps != null && _frameSteps.Length > 480) return;
            string bit = name + ":" + ms.ToString("0.0", CultureInfo.InvariantCulture);
            _frameSteps = _frameSteps == null ? bit : _frameSteps + "," + bit;
        }

        // Call once at the start of Update. The first call closes the boot gap.
        public static void NoteFrame()
        {
            if (_closed) return;
            Arm();
            long now = Stopwatch.GetTimestamp();
            double ms = (now - _frameT0) * 1000.0 / Stopwatch.Frequency;
            _frameT0 = now;
            _frames++;
            if (_frames == 1)
            {
                _boot = ms;
                _bootSteps = _frameSteps;
            }
            else if (ms > _worst)
            {
                _worst = ms;
                _worstFrame = Time.frameCount;
                _worstSteps = _frameSteps;
            }
            _frameSteps = null;
            if (Time.realtimeSinceStartupAsDouble - _origin >= WindowSec && _frames >= 8)
                Flush();
        }

        // Editor probes bake once before play. Domain reload is off, so the
        // window has to be cleared or that bake is charged to the first frames.
        public static void ResetWindow()
        {
            _depth = 0;
            _hitN = 0;
            _worst = -1;
            _worstFrame = 0;
            _worstSteps = null;
            _boot = -1;
            _bootSteps = null;
            _frames = 0;
            _originSet = false;
            _closed = false;
            _frameSteps = null;
        }

        public static void Flush()
        {
            if (_closed) return;
            _closed = true;
            for (int i = 1; i < _hitN; i++)
            {
                var key = _hits[i];
                int j = i - 1;
                while (j >= 0 && _hits[j].Ms < key.Ms)
                {
                    _hits[j + 1] = _hits[j];
                    j--;
                }
                _hits[j + 1] = key;
            }
            var culture = CultureInfo.InvariantCulture;
            var top = "";
            int n = _hitN < 8 ? _hitN : 8;
            for (int i = 0; i < n; i++)
            {
                if (i > 0) top += ",";
                top += _hits[i].Name + ":" + _hits[i].Ms.ToString("0.0", culture)
                    + "@" + _hits[i].Frame.ToString(culture);
            }
            string line = "FF_STARTUP worst=" + _worst.ToString("0.0", culture)
                + "ms frame=" + _worstFrame.ToString(culture)
                + " boot=" + _boot.ToString("0.0", culture)
                + "ms frames=" + _frames.ToString(culture)
                + " top=" + top
                + " worstSteps=" + (_worstSteps ?? "")
                + " bootSteps=" + (_bootSteps ?? "");
            UnityEngine.Debug.Log(line);
            try { System.IO.File.WriteAllText("/tmp/ff-startup-result.txt", line); }
            catch (System.Exception) { }
        }
    }
}
