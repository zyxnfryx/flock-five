#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 76. The LEVEL flower's press and its launch are one hit, and a gate
    // or a second finger cannot leave the sink down without starting the level.
    static class Build76Tests
    {
        [MenuItem("Flock Five/Build 76 Tests")]
        public static void Run()
        {
            int pass = 0;
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++;
                else fail++;
                Debug.Log("[build76] " + (ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            Phone(Check, "1170", 1170f, 2532f, new Rect(0f, 0f, 1170f, 2532f));
            Phone(Check, "750", 750f, 1334f, new Rect(0f, 0f, 750f, 1334f));

            var hit = new SplashPress.Hit { Rect = new Rect(10f, 20f, 100f, 80f) };
            var inn = new Vector2(60f, 60f);
            var outp = new Vector2(0f, 0f);

            var g = Fresh();
            var down = g.Apply(hit, false, One(1, SplashPress.Phase.Began, inn), true, true, 1);
            var up = g.Apply(hit, false, One(1, SplashPress.Phase.Ended, inn), false, true, 2);
            Check("press-release-inside",
                down.Pressed && !down.Launch && up.Launch && !up.Pressed && !g.Pressed,
                "down=" + down.Pressed + " up=" + up.Launch + " stuck=" + g.Pressed);

            g = Fresh();
            g.Apply(hit, false, One(1, SplashPress.Phase.Began, inn), true, true, 3);
            var cancel = g.Apply(hit, false, One(1, SplashPress.Phase.Ended, outp), false, true, 4);
            var after = g.Apply(hit, false, One(1, SplashPress.Phase.Held, outp), false, true, 5);
            Check("press-release-outside",
                !cancel.Launch && !cancel.Pressed && !after.Pressed && !after.Launch && !g.Pressed,
                "launch=" + cancel.Launch + " pressed=" + cancel.Pressed + " after=" + after.Pressed + " stuck=" + g.Pressed);

            bool gated = SplashPress.Blocks(new SplashPress.Gates { HardModal = true });
            g = Fresh();
            var blockedDown = g.Apply(hit, gated, One(1, SplashPress.Phase.Began, inn), true, true, 6);
            var blockedUp = g.Apply(hit, gated, One(1, SplashPress.Phase.Ended, inn), false, true, 7);
            Check("gated-no-press",
                gated && !blockedDown.Pressed && !blockedDown.Launch && !blockedUp.Launch && !g.Pressed,
                "blocked=" + gated + " down=" + blockedDown.Pressed + " launch=" + blockedUp.Launch + " stuck=" + g.Pressed);

            Check("popup-gate-blocks",
                SplashPress.Blocks(new SplashPress.Gates { PopupGate = true })
                && SplashPress.Blocks(new SplashPress.Gates { CoachAte = true })
                && SplashPress.Blocks(new SplashPress.Gates { HomeTaken = true }),
                "popup/coach/bird");
            Check("daily-lesson-lets-soft",
                !SplashPress.Blocks(new SplashPress.Gates { SoftModal = true, DailySoftOk = true })
                && SplashPress.Blocks(new SplashPress.Gates { SoftModal = true }),
                "soft");

            g = Fresh();
            int launches = 0;
            void Count(SplashPress.View v) { if (v.Launch) launches++; }
            Count(g.Apply(hit, false, One(1, SplashPress.Phase.Began, inn), true, true, 8));
            var mid = g.Apply(hit, false, Two(1, SplashPress.Phase.Held, inn, 2, SplashPress.Phase.Began, inn), true, true, 9);
            int midFinger = g.TouchId;
            Count(mid);
            Count(g.Apply(hit, false, Two(1, SplashPress.Phase.Ended, inn, 2, SplashPress.Phase.Ended, inn), false, true, 10));
            Count(g.Apply(hit, false, One(2, SplashPress.Phase.Ended, inn), false, true, 11));
            Check("second-touch-one-launch",
                launches == 1 && mid.Pressed && midFinger == 1 && !g.Pressed,
                "launches=" + launches + " mid=" + mid.Pressed + " finger=" + midFinger + " stuck=" + g.Pressed);

            g = Fresh();
            g.Apply(hit, false, One(1, SplashPress.Phase.Began, inn), true, true, 12);
            var drop = g.Apply(hit, false, Two(1, SplashPress.Phase.Ended, outp, 2, SplashPress.Phase.Held, inn), false, true, 13);
            var stray = g.Apply(hit, false, One(2, SplashPress.Phase.Ended, inn), false, true, 14);
            Check("second-touch-no-stick",
                !drop.Launch && !drop.Pressed && !stray.Launch && !g.Pressed,
                "drop=" + drop.Launch + " stray=" + stray.Launch + " stuck=" + g.Pressed);

            g = Fresh();
            g.Apply(hit, false, One(1, SplashPress.Phase.Began, inn), true, true, 15);
            var shut = g.Apply(hit, true, One(1, SplashPress.Phase.Held, inn), false, true, 16);
            Check("gate-closes",
                !shut.Pressed && !shut.Launch && !g.Pressed,
                "pressed=" + shut.Pressed + " launch=" + shut.Launch);

            Rail(Check, SplashPress.Id.Poker);
            Rail(Check, SplashPress.Id.Hive);
            Rail(Check, SplashPress.Id.Daily);
            var vip = SplashPress.PressHit(SplashPress.Id.Vip);
            SplashPress.LayoutDisc(SplashPress.Id.Vip, new Rect(4f, 8f, 40f, 40f), new Rect(8f, 48f, 32f, 12f));
            Check("vip-same-hit",
                ReferenceEquals(vip, SplashPress.LaunchHit(SplashPress.Id.Vip)) && vip.Disc
                && vip.Contains(new Vector2(24f, 28f))
                && vip.Contains(new Vector2(20f, 52f))
                && !vip.Contains(new Vector2(4f, 8f)),
                "disc=" + vip.Disc);

            Debug.Log("[build76] " + (fail == 0 ? "ALL OK " + pass : "FAILED " + fail + " passed " + pass));
        }

        static void Phone(System.Action<string, bool, string> check, string name, float w, float h, Rect safe)
        {
            var shared = SplashPress.LayoutLevel(w, h, safe);
            var press = SplashPress.PressHit(SplashPress.Id.Level);
            var launch = SplashPress.LaunchHit(SplashPress.Id.Level);
            var expect = ExpectLevel(w, h, safe);
            bool same = ReferenceEquals(shared, press) && ReferenceEquals(press, launch);
            bool values = Near(press.Rect, launch.Rect) && Near(press.Rect, expect);
            SplashPress.ResetBoard();
            var g = new SplashPress.Gesture();
            var inn = press.Rect.center;
            var began = g.Apply(press, false, One(3, SplashPress.Phase.Began, inn), true, true, 21);
            var ended = g.Apply(launch, false, One(3, SplashPress.Phase.Ended, inn), false, true, 22);
            check(name + "-same-hit",
                same && values && began.Pressed && ended.Launch && !g.Pressed,
                "same=" + same + " rect=" + press.Rect + " expect=" + expect
                + " launch=" + ended.Launch + " stuck=" + g.Pressed);
        }

        static void Rail(System.Action<string, bool, string> check, SplashPress.Id id)
        {
            var rect = new Rect(12f, 30f, 48f, 48f);
            SplashPress.Layout(id, rect);
            var press = SplashPress.PressHit(id);
            var launch = SplashPress.LaunchHit(id);
            check(id + "-same-hit",
                ReferenceEquals(press, launch) && Near(press.Rect, rect) && Near(launch.Rect, rect) && !press.Disc,
                press.Rect.ToString());
        }

        // The LEVEL square: safe-area floor, then the smaller of 94% width and half height.
        static Rect ExpectLevel(float w, float h, Rect safe)
        {
            float s = h / 720f;
            if (s < 1f) s = 1f;
            float clear = safe.yMin + 2f;
            if (clear < 4f) clear = 4f;
            float old = safe.yMin + 8f;
            if (old < 14f) old = 14f;
            float pad = old - 28f * s;
            if (pad < clear) pad = clear;
            float size = w * 0.94f;
            float half = h * 0.50f;
            if (half < size) size = half;
            return new Rect((w - size) * 0.5f, h - pad - size, size, size);
        }

        static bool Near(Rect a, Rect b)
        {
            return Mathf.Abs(a.x - b.x) < 0.02f
                && Mathf.Abs(a.y - b.y) < 0.02f
                && Mathf.Abs(a.width - b.width) < 0.02f
                && Mathf.Abs(a.height - b.height) < 0.02f;
        }

        static SplashPress.Gesture Fresh()
        {
            SplashPress.ResetBoard();
            return new SplashPress.Gesture();
        }

        static SplashPress.Sample[] One(int id, SplashPress.Phase phase, Vector2 pos)
        {
            return new[] { new SplashPress.Sample { TouchId = id, Pos = pos, Phase = phase } };
        }

        static SplashPress.Sample[] Two(int a, SplashPress.Phase pa, Vector2 aPos, int b, SplashPress.Phase pb, Vector2 bPos)
        {
            return new[]
            {
                new SplashPress.Sample { TouchId = a, Pos = aPos, Phase = pa },
                new SplashPress.Sample { TouchId = b, Pos = bPos, Phase = pb },
            };
        }
    }
}
#endif
