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
            CheckPadDoesNotStealFlower(Check);
            CheckBlockedRailFallsThrough(Check);
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
            var poker = FlockFiveApp.SettledRailIcon(w, h, safe, true, 2);
            var daily = FlockFiveApp.SettledRailIcon(w, h, safe, false, 0);
            var vip = FlockFiveApp.SettledRailIcon(w, h, safe, false, 1);
            var gear = FlockFiveApp.SettingsGearDrawnRect(w, h, safe);

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
            return LevelMayStart(level, point, icons, null);
        }

        static bool LevelMayStart(Rect level, Vector2 point, (string, Rect)[] icons, bool[] canAct)
        {
            if (!level.Contains(point)) return false;
            for (int i = 0; i < icons.Length; i++)
            {
                bool act = canAct == null || (i < canAct.Length && canAct[i]);
                if (FlockFiveApp.RailKeepsLevelTap(icons[i].Item2, act, point)) return false;
            }
            return true;
        }

        // The old pad reached past the cards and the gift into the flower.
        // A point in that slop, off the drawn art, still starts LEVEL.
        static void CheckPadDoesNotStealFlower(System.Action<string, bool, string> Check)
        {
            PhoneArea.PlayPixels(820, 1180, out int w, out int h);
            var safe = new Rect(0f, 0f, w, h);
            var level = SplashPress.LevelRect(w, h, safe);
            float gap = Mathf.Max(24f, FlockFiveApp.SplashRailSize(w) * 0.30f);
            var pokerArt = FlockFiveApp.SettledRailIcon(w, h, safe, true, 2);
            var dailyArt = FlockFiveApp.SettledRailIcon(w, h, safe, false, 0);
            var pokerPad = SplashPress.PadRail(pokerArt, gap);
            var dailyPad = SplashPress.PadRail(dailyArt, gap);
            var icons = new[] { ("poker", pokerArt), ("daily", dailyArt) };
            bool pokerEdge = EdgeOutsideArt(level, pokerArt, pokerPad, out var pokerPoint);
            bool dailyEdge = EdgeOutsideArt(level, dailyArt, dailyPad, out var dailyPoint);
            Check("820-poker-pad-still-starts-level",
                pokerEdge && LevelMayStart(level, pokerPoint, icons),
                "point " + pokerPoint + " art " + pokerArt + " pad " + pokerPad);
            Check("820-daily-pad-does-not-cover-level-or-starts",
                !dailyEdge || LevelMayStart(level, dailyPoint, icons),
                "edge " + dailyEdge + " point " + dailyPoint);
        }

        static bool EdgeOutsideArt(Rect level, Rect art, Rect pad, out Vector2 point)
        {
            point = new Vector2(art.center.x, art.yMax + 3f);
            if (pad.Contains(point) && !art.Contains(point) && level.Contains(point)) return true;
            point = new Vector2(art.xMax + 4f, art.center.y);
            if (pad.Contains(point) && !art.Contains(point) && level.Contains(point)) return true;
            point = new Vector2(art.xMin - 4f, art.center.y);
            return pad.Contains(point) && !art.Contains(point) && level.Contains(point);
        }

        static void CheckBlockedRailFallsThrough(System.Action<string, bool, string> Check)
        {
            PhoneArea.PlayPixels(820, 1180, out int w, out int h);
            var safe = new Rect(0f, 0f, w, h);
            var level = SplashPress.LevelRect(w, h, safe);
            var poker = FlockFiveApp.SettledRailIcon(w, h, safe, true, 2);
            var point = new Vector2(poker.center.x, Mathf.Max(poker.center.y, level.yMin + 4f));
            var icons = new[] { ("poker", poker) };
            Check("820-overlap-is-on-the-art",
                poker.Contains(point) && level.Contains(point),
                "point " + point);
            Check("820-open-rail-keeps-the-art",
                !LevelMayStart(level, point, icons, new[] { true }),
                "point " + point);
            Check("820-blocked-rail-lets-level-start",
                LevelMayStart(level, point, icons, new[] { false }),
                "point " + point);

            var samples = new List<SplashPress.Sample>
            {
                new SplashPress.Sample { TouchId = 8, Pos = point, Phase = SplashPress.Phase.Tap },
            };
            SplashPress.ResetBoard();
            SplashPress.Layout(SplashPress.Id.Level, level);
            var blocked = new SplashPress.Gesture();
            var fell = blocked.Apply(SplashPress.PressHit(SplashPress.Id.Level), false, samples, true, true, 70);
            Check("820-blocked-overlap-gesture-starts-level", fell.Launch, "launch=" + fell.Launch);

            SplashPress.ResetBoard();
            SplashPress.Reserve(samples, poker, 71);
            var held = new SplashPress.Gesture();
            var kept = held.Apply(SplashPress.PressHit(SplashPress.Id.Level), false, samples, true, true, 71);
            Check("820-open-art-gesture-does-not-start-level", !kept.Launch && !kept.Pressed,
                "launch=" + kept.Launch);
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
