#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 82 pass c. Badger frames, streak badge, scoreboard, rigid labels, press, curtain.
    static class Build82TestsC
    {
        [MenuItem("Flock Five/Build 82c Tests")]
        public static void Run()
        {
            int pass = 0;
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++;
                else fail++;
                Debug.Log("[build82c] " + (ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            CheckFrames(Check);
            CheckBadge(Check);
            CheckMeter(Check);
            CheckLabels(Check);
            CheckPress(Check);
            CheckCurtain(Check);

            Debug.Log(fail == 0
                ? "build82c tests DONE " + pass + "/" + (pass + fail)
                : "build82c tests FAIL " + fail + " failed, " + pass + " passed");
        }

        static void CheckFrames(System.Action<string, bool, string> Check)
        {
            int loaded = SpriteCatalog.PreloadBadgerArt();
            int blank = 0;
            var names = SpriteAnim.Frames;
            for (int i = 0; i < names.Length; i++)
            {
                var spr = SpriteCatalog.BadgerArt(names[i]);
                if (spr == null || spr.rect.width < 8f || spr.rect.height < 8f || spr.texture == null)
                    blank++;
            }
            float fa = 0.5f, ua = 0.5f;
            SpriteAnim.Cover(ref fa, ref ua, true, true);
            bool covered = fa >= 0.99f || ua >= 0.99f;
            float missF = 0.4f, missU = 0.6f;
            SpriteAnim.Cover(ref missF, ref missU, false, true);
            bool held = missU >= 0.99f && missF <= 0.001f;
            var anchor = new Vector2(400f, 900f);
            var a = SpriteAnim.Planted("badger_idle_a", 363f, 634f, anchor, 1f);
            var b = SpriteAnim.Planted("badger_idle_b", 438f, 640f, anchor, 1f);
            var leap = SpriteAnim.Planted("badger_leap_1", 736f, 614f, anchor, 1f);
            bool feet = Vector2.Distance(a, anchor) < 0.6f
                && Vector2.Distance(b, anchor) < 0.6f
                && Vector2.Distance(leap, anchor) < 0.6f;
            var idle = BadgerAnim.Idle(0.6f);
            bool noHole = Mathf.Max(idle.FrameAlpha, idle.UnderAlpha) > 0.95f
                && idle.Frame == "badger_idle_b" && idle.Under == "badger_idle_a";
            var mid = BadgerAnim.Entrance(2.24f);
            bool blend = mid.FrameAlpha > 0.4f && mid.UnderAlpha > 0.2f
                && Mathf.Max(mid.FrameAlpha, mid.UnderAlpha) > 0.95f;
            Check("badger-frames",
                blank == 0 && loaded == names.Length && covered && held && feet && noHole && blend,
                "blank=" + blank + " loaded=" + loaded + "/" + names.Length
                + " cover=" + covered + " held=" + held + " feet=" + feet
                + " idleA=" + idle.FrameAlpha.ToString("0.00") + " blend=" + blend);
        }

        static void CheckBadge(System.Action<string, bool, string> Check)
        {
            var comb = new Rect(200f, 400f, 180f, 160f);
            float d = Mathf.Min(comb.width, comb.height) * 0.42f;
            var seat = StreakBadge.TopRight(comb, d);
            bool corner = seat.center.x > comb.center.x && seat.center.y < comb.center.y
                && seat.xMax > comb.xMax - 4f;
            var board = new Rect(80f, 200f, 640f, 880f);
            var stamp = FlockFiveApp.RewardStampRect(board, 2);
            var via = StreakBadge.TopRight(board, stamp.width);
            bool same = Mathf.Abs(stamp.x - via.x) < 0.05f && Mathf.Abs(stamp.y - via.y) < 0.05f
                && Mathf.Abs(stamp.width - via.width) < 0.05f;
            string badger = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/FlockFiveApp.Badger.cs"));
            int badge = badger.IndexOf("void DrawBadgerPowerBadge(");
            string body = badge >= 0 ? badger.Substring(badge, Mathf.Min(900, badger.Length - badge)) : "";
            bool wired = body.IndexOf("StreakBadge.TopRight") >= 0 && body.IndexOf("StreakBadge.Draw") >= 0
                && body.IndexOf("BadgerPower.X2") >= 0;
            string app = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/FlockFiveApp.cs"));
            int rect = app.IndexOf("public static Rect RewardStampRect(");
            string rectBody = rect >= 0 ? app.Substring(rect, Mathf.Min(500, app.Length - rect)) : "";
            bool helper = rectBody.IndexOf("StreakBadge.TopRight") >= 0;
            Check("honey-badge", corner && same && wired && helper,
                "corner=" + corner + " stamp=" + same + " wired=" + wired + " helper=" + helper
                + " seat=" + seat);
        }

        static void CheckMeter(System.Action<string, bool, string> Check)
        {
            var col = new Rect(8f, 120f, 78f, 900f);
            float unit = BadgerMeter.Unit(col, 18);
            int left = BadgerMeter.LeftToGo(18, 5);
            int done = BadgerMeter.LeftToGo(10, 10);
            int over = BadgerMeter.LeftToGo(10, 14);
            var plate = BadgerMeter.EndPlate(col, 18, unit);
            var top = BadgerMeter.Comb(col, 17, unit);
            bool touch = plate.yMax >= top.y - 1f && plate.y <= top.yMax + 2f;
            bool inside = plate.x >= col.x - 1f && plate.xMax <= col.xMax + 1f
                && plate.y >= col.y - 1f && plate.yMax <= col.yMax + 1f;
            bool tick = BadgerMeter.Tick(0f) > 0.95f
                && BadgerMeter.Tick(BadgerMeter.TickSeconds * 0.5f) < BadgerMeter.Tick(0f) - 0.2f
                && BadgerMeter.Tick(BadgerMeter.TickSeconds + 0.05f) == 0f
                && BadgerMeter.Tick(-1f) == 0f;
            string badger = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/FlockFiveApp.Badger.cs"));
            int meter = badger.IndexOf("void DrawBadgerMeter(");
            string body = meter >= 0 ? badger.Substring(meter, Mathf.Min(3600, badger.Length - meter)) : "";
            bool labels = body.IndexOf("LeftToGo") >= 0 && body.IndexOf("\"LEFT\"") >= 0
                && body.IndexOf("\"YOU\"") >= 0 && body.IndexOf("\"BADGER\"") >= 0
                && body.IndexOf("DrawSolidRound") >= 0 && body.IndexOf("EndPlate") >= 0;
            Check("left-to-go",
                left == 13 && done == 0 && over == 0 && touch && inside && tick && labels,
                "left=" + left + " touch=" + touch + " inside=" + inside + " tick=" + tick + " labels=" + labels);
        }

        static void CheckLabels(System.Action<string, bool, string> Check)
        {
            float[] scales = { 0.86f, 1f, 1.035f, 1.13f, 1.2f };
            float[] shifts = { 0f, 0.17f, 0.41f, 0.63f, 0.9f };
            var first = Vector2.zero;
            bool locked = true;
            bool constant = true;
            for (int i = 0; i < scales.Length; i++)
            {
                float k = scales[i];
                var pivot = new Vector2(180f, 420f);
                var m = Matrix4x4.TRS(new Vector3(pivot.x, pivot.y, 0f), Quaternion.identity, new Vector3(k, k, 1f))
                    * Matrix4x4.TRS(new Vector3(-pivot.x, -pivot.y, 0f), Quaternion.identity, Vector3.one);
                var rect = new Rect(40f + shifts[i], 80f + shifts[i] * 0.5f, 220f, 64f);
                var seat = LabelRim.Lock(rect, m, 2f);
                LabelRim.Drawn(seat, 2f, 0, 0, out float fx, out float fy);
                LabelRim.Drawn(seat, 2f, 5, -3, out float ox, out float oy);
                float dx = ox - fx;
                float dy = oy - fy;
                if (Mathf.Abs(dx - 5f) > 0.02f || Mathf.Abs(dy - (-3f)) > 0.02f) locked = false;
                if (i == 0) first = new Vector2(dx, dy);
                else if (Mathf.Abs(dx - first.x) > 0.001f || Mathf.Abs(dy - first.y) > 0.001f) constant = false;
            }
            string app = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/FlockFiveApp.cs"));
            int at = app.IndexOf("static void LabelAt(");
            string body = at >= 0 ? app.Substring(at, Mathf.Min(1200, app.Length - at)) : "";
            bool drawn = body.IndexOf("LabelRim.Drawn") >= 0;
            string vip = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/FlockFiveApp.VipOffer.cs"));
            int buy = vip.IndexOf("static void DrawBuy(");
            string buyBody = buy >= 0 ? vip.Substring(buy, Mathf.Min(1600, vip.Length - buy)) : "";
            bool still = buyBody.IndexOf("0.035f") < 0 && buyBody.IndexOf("PushButton.SheenU") >= 0
                && buyBody.IndexOf("PushButton.IdleScale") < 0;
            bool idle = Mathf.Abs(PushButton.IdleScale(1.7f) - 1f) < 0.0001f
                && PushButton.SheenPeriod >= 1.2f && PushButton.SheenPeriod <= 1.6f; // b86: 2x faster
            float u0 = PushButton.SheenU(0f);
            float u1 = PushButton.SheenU(PushButton.SheenPeriod);
            float mid = PushButton.SheenU(PushButton.SheenPeriod * 0.5f);
            bool period = Mathf.Abs(u0 - u1) < 0.001f && mid > 0.45f && mid < 0.55f;
            Check("vip-label",
                locked && constant && drawn && still && idle && period,
                "locked=" + locked + " constant=" + constant + " drawn=" + drawn
                + " still=" + still + " period=" + PushButton.SheenPeriod);
        }

        static void CheckPress(System.Action<string, bool, string> Check)
        {
            var layout = new Rect(100.4f, 800.2f, 280f, 280f);
            var hit = PushButton.HitRect(layout);
            var held = PushButton.At(true, -1f);
            var vis = PushButton.Visual(layout, held);
            bool hitSame = hit.x == layout.x && hit.y == layout.y
                && hit.width == layout.width && hit.height == layout.height;
            bool sank = vis.y > layout.y + 4f && vis.height < layout.height - 4f && vis.width > layout.width;
            var over = PushButton.At(false, PushButton.ReleaseSeconds * 0.42f);
            bool overshoot = over.Sink < -0.01f && over.SquashY > 1.03f;
            var back = PushButton.At(false, PushButton.ReleaseSeconds);
            bool settled = Mathf.Abs(back.Sink) < 0.001f && Mathf.Abs(back.SquashY - 1f) < 0.001f;
            var label = new Rect(150.2f, 860.4f, 140f, 48f);
            float wantX = (label.center.x - layout.x) / layout.width;
            float wantY = (label.center.y - layout.y) / layout.height;
            bool rigid = true;
            float[] ages = { -1f, 0.02f, 0.13f, 0.28f };
            for (int i = 0; i < ages.Length; i++)
            {
                var pose = i == 0 ? held : PushButton.At(false, ages[i]);
                var moved = PushButton.Visual(layout, pose);
                var followed = PushButton.Follow(label, layout, moved, 2f);
                float nx = (followed.center.x - moved.x) / moved.width;
                float ny = (followed.center.y - moved.y) / moved.height;
                if (Mathf.Abs(nx - wantX) > 0.02f || Mathf.Abs(ny - wantY) > 0.02f) rigid = false;
                var hitNow = PushButton.HitRect(layout);
                if (hitNow.y != layout.y || hitNow.height != layout.height) rigid = false;
            }
            string app = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/FlockFiveApp.cs"));
            bool shared = app.IndexOf("PushButton.Sample") >= 0 && app.IndexOf("PushButton.Follow") >= 0
                && app.IndexOf("Sfx.ButtonThunk") < 0;
            // Thunk lives in the shared sampler, not a one-off at the flower.
            string push = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/PushButton.cs"));
            bool thunk = push.IndexOf("Sfx.ButtonThunk") >= 0 && push.IndexOf("Haptics.Play") >= 0;
            Check("push-press",
                hitSame && sank && overshoot && settled && rigid && shared && thunk,
                "hit=" + hitSame + " sank=" + sank + " over=" + overshoot + " back=" + settled
                + " rigid=" + rigid + " thunk=" + thunk);
        }

        static void CheckCurtain(System.Action<string, bool, string> Check)
        {
            LevelCurtain.Reset();
            const float t0 = 5f;
            LevelCurtain.Arm(t0);
            bool clock = LevelCurtain.Armed && !LevelCurtain.LoadStarted;
            LevelCurtain.BeginLoad();
            bool started = LevelCurtain.LoadStarted && !LevelCurtain.Ready;
            float held = LevelCurtain.VisualAge(t0 + 2f);
            bool waiting = Mathf.Abs(held - LevelCurtain.Flood) < 0.001f;
            LevelCurtain.MarkReady();
            float early = LevelCurtain.VisualAge(t0 + 0.2f);
            bool plays = Mathf.Abs(early - 0.2f) < 0.001f;
            bool done = LevelCurtain.Finished(t0 + LevelCurtain.Duration)
                && !LevelCurtain.Finished(t0 + 0.5f);
            bool budget = LevelCurtain.Duration <= 1.1f && LevelCurtain.Duration >= 0.8f;
            string settings = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/FlockFiveApp.Settings.cs"));
            int press = settings.IndexOf("bool PressLevelButton()");
            string body = press >= 0 ? settings.Substring(press, Mathf.Min(700, settings.Length - press)) : "";
            int begin = body.IndexOf("LevelCurtain.BeginLoad");
            int load = body.IndexOf("Load(");
            bool order = begin >= 0 && load > begin;
            string app = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/FlockFiveApp.cs"));
            int flower = app.IndexOf("bool DrawFlowerPlay(");
            // b123 grew the preamble (the badger on the pedestal joins the LEVEL hit) past the
            // old 1600-character window. Arm still has to live in this method.
            string flowerBody = "";
            if (flower >= 0)
            {
                int depth = 0;
                int close = flower;
                bool started = false;
                for (int i = flower; i < app.Length; i++)
                {
                    if (app[i] == '{') { depth++; started = true; }
                    else if (app[i] == '}')
                    {
                        depth--;
                        if (started && depth == 0) { close = i; break; }
                    }
                }
                flowerBody = app.Substring(flower, close - flower);
            }
            bool arm = flowerBody.IndexOf("LevelCurtain.Arm") >= 0;
            LevelCurtain.Reset();
            Check("level-curtain",
                clock && started && waiting && plays && done && budget && order && arm,
                "clock=" + clock + " started=" + started + " hold=" + waiting
                + " plays=" + plays + " done=" + done + " order=" + order + " arm=" + arm);
        }
    }
}
#endif
