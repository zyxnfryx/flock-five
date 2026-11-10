#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Shipping path. At iPad portrait the LEVEL square reaches the cards icon.
    // A tap on that art must open Poker, not start the garden. 402 and 412
    // stay clear, and an open spot on the flower still starts the level.
    public static class HomeRailPriorityTests
    {
        const string Out = "/tmp/flock-five-home-rail-priority-tests.txt";

        [MenuItem("Flock Five/Home Rail Priority Tests")]
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (!ok) fail++;
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[home-rail] " + (ok ? "PASS " : "FAIL ") + name + " " + detail);
            }

            CheckWindow("820", 820, 1180, expectPokerOverlap: true, Check);
            CheckWindow("402", 402, 874, expectPokerOverlap: false, Check);
            CheckWindow("412", 412, 915, expectPokerOverlap: false, Check);
            CheckSecondPointer(Check);

            string end = fail == 0 ? "HOME_RAIL_PRIORITY_OK" : "HOME_RAIL_PRIORITY_FAIL " + fail;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[home-rail] " + end);
            if (fail > 0) throw new System.Exception(end);
        }

        static void CheckWindow(string tag, int screenW, int screenH, bool expectPokerOverlap, System.Action<string, bool, string> Check)
        {
            PhoneArea.PlayPixels(screenW, screenH, out int w, out int h);
            var safe = new Rect(0f, 0f, w, h);
            var level = SplashPress.LevelRect(w, h, safe);
            var pig = FlockFiveApp.SettledRailIcon(w, h, safe, true, 0);
            var hive = FlockFiveApp.SettledRailIcon(w, h, safe, true, 1);
            var poker = SplashPress.PadRail(FlockFiveApp.SettledRailIcon(w, h, safe, true, 2), Mathf.Max(24f, FlockFiveApp.SplashRailSize(w) * 0.30f));
            var daily = SplashPress.PadRail(FlockFiveApp.SettledRailIcon(w, h, safe, false, 0), Mathf.Max(24f, FlockFiveApp.SplashRailSize(w) * 0.30f));
            var vip = FlockFiveApp.SettledRailIcon(w, h, safe, false, 1);
            var gear = FlockFiveApp.SettingsGearRect(w, h, safe);

            bool pokerOverlap = Overlaps(level, poker);
            Check(tag + "-poker-vs-level", pokerOverlap == expectPokerOverlap,
                "overlap=" + pokerOverlap + " level " + level + " poker " + poker);

            var icons = new[]
            {
                ("pig", pig), ("hive", hive), ("poker", poker), ("daily", daily), ("vip", vip), ("gear", gear),
            };
            for (int i = 0; i < icons.Length; i++)
            {
                var icon = icons[i].Item2;
                var name = icons[i].Item1;
                var point = icon.center;
                bool onLevel = level.Contains(point);
                bool railWins = icon.Contains(point);
                Check(tag + "-" + name + "-center-is-the-icon",
                    railWins && (!onLevel || !LevelMayStart(level, point, icons)),
                    "point " + point + " level " + onLevel);
            }

            var open = new Vector2(level.center.x, level.yMax - 8f);
            Check(tag + "-open-flower-starts",
                level.Contains(open) && LevelMayStart(level, open, icons),
                "point " + open);
        }

        static bool LevelMayStart(Rect level, Vector2 point, (string, Rect)[] icons)
        {
            if (!level.Contains(point)) return false;
            for (int i = 0; i < icons.Length; i++)
                if (icons[i].Item2.width > 2f && icons[i].Item2.Contains(point)) return false;
            return true;
        }

        static bool Overlaps(Rect a, Rect b)
        {
            return a.width > 1f && b.width > 1f && a.Overlaps(b);
        }

        // Two pointers on the cards/LEVEL overlap. The icon keeps the first.
        // The second must not start the garden.
        static void CheckSecondPointer(System.Action<string, bool, string> Check)
        {
            var level = new Rect(100f, 500f, 500f, 500f);
            var poker = new Rect(520f, 470f, 140f, 140f);
            var point = new Vector2(560f, 560f);
            var samples = new List<SplashPress.Sample>
            {
                new SplashPress.Sample { TouchId = 4, Pos = point, Phase = SplashPress.Phase.Began },
                new SplashPress.Sample { TouchId = 9, Pos = point, Phase = SplashPress.Phase.Began },
            };
            SplashPress.ResetBoard();
            SplashPress.Layout(SplashPress.Id.Poker, poker);
            SplashPress.Layout(SplashPress.Id.Level, level);
            var icon = new SplashPress.Gesture();
            var first = icon.Apply(SplashPress.PressHit(SplashPress.Id.Poker), false, samples, true, true, 40);
            SplashPress.Reserve(samples, SplashPress.PressHit(SplashPress.Id.Poker), 40);
            var play = new SplashPress.Gesture();
            var stole = play.Apply(SplashPress.PressHit(SplashPress.Id.Level), false, samples, true, true, 40);
            Check("second-pointer-does-not-start-level",
                first.Pressed && !stole.Launch && !stole.Pressed,
                "iconPressed=" + first.Pressed + " levelLaunch=" + stole.Launch);

            SplashPress.ResetBoard();
            var only = new List<SplashPress.Sample>
            {
                new SplashPress.Sample { TouchId = 3, Pos = new Vector2(level.center.x, level.yMax - 6f), Phase = SplashPress.Phase.Tap },
            };
            var flower = new SplashPress.Gesture();
            var go = flower.Apply(SplashPress.PressHit(SplashPress.Id.Level), false, only, true, true, 41);
            Check("open-flower-tap-still-starts", go.Launch, "launch=" + go.Launch);
        }
    }
}
#endif
