#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEngine;

namespace FlockFive.Editor
{
    // b110: a feeder's hanging cord is one child that hides with the feeder art.
    // Run via Run() (same pattern as Build84TestsFeeder).
    public static class FeederCordTests
    {
        const string Out = "/tmp/flock-five-feeder-cord-tests.txt";

        public static void Run()
        {
            var sb = new StringBuilder();
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (!ok) fail++;
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }
            var go = new GameObject("cord-test");
            try
            {
                var tex = new Texture2D(4, 4);
                var spr = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
                var art = go.AddComponent<SpriteRenderer>();
                art.sprite = spr;
                var view = go.AddComponent<FeederView>();
                view.Art = art;
                view.Seat(4f);
                int cords = 0;
                SpriteRenderer cord = null;
                foreach (Transform c in go.transform)
                    if (c.name == "Cord") { cords++; cord = c.GetComponent<SpriteRenderer>(); }
                Check("one-cord-child", cords == 1 && cord != null, "cords " + cords);
                Check("cord-shown-with-art", cord != null && cord.enabled == art.enabled, "");
                view.Show(null);
                Check("cord-hidden-when-feeder-empties", cord != null && !art.enabled && !cord.enabled, "");
                view.Seat(4.5f);
                int again = 0;
                foreach (Transform c in go.transform) if (c.name == "Cord") again++;
                Check("still-one-cord", again == 1, "cords " + again);
            }
            finally { Object.DestroyImmediate(go); }
            sb.AppendLine(fail == 0 ? "ALL PASS" : fail + " FAIL");
            File.WriteAllText(Out, sb.ToString());
            UnityEngine.Debug.Log("[feeder-cord] " + (fail == 0 ? "ALL PASS" : fail + " FAIL"));
        }
    }
}
#endif
