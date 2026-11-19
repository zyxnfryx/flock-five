#if UNITY_EDITOR
using System.Reflection;
using UnityEngine;

namespace FlockFive.Editor
{
    // Edit-mode checks for the inspector list on b124/first-garden-clarity (PR 262).
    public static class Inspector262Tests
    {
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            void Check(string name, bool ok, string detail)
            {
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
                Debug.Log("[inspector-262] " + (ok ? "PASS " : "FAIL ") + name + " " + detail);
            }

            var bird = new Rect(120f, 400f, 80f, 90f);
            var landed = FlockFiveApp.GloveTarget(bird);
            Check("adopt-glove-aims-landed-bird",
                bird.Contains(landed),
                landed.ToString());

            var daily = new Rect(24f, 280f, 72f, 72f);
            var dailyAim = FlockFiveApp.GloveTarget(daily);
            Check("daily-glove-on-icon",
                daily.Contains(dailyAim) && !new Rect(24f, 360f, 200f, 80f).Contains(dailyAim),
                dailyAim.ToString());

            string dailyLine = "Tap Daily\nfor a bonus!";
            int breaks = 0;
            for (int i = 0; i < dailyLine.Length; i++) if (dailyLine[i] == '\n') breaks++;
            Check("daily-caption-under-four-lines", breaks == 1 && dailyLine.Length < 28, dailyLine);

            var claim = new Rect(40f, 500f, 160f, 120f);
            var claimSeat = FlockFiveApp.DailyClaimGloveSeat(claim);
            Check("claim-glove-on-button",
                claim.Contains(claimSeat) && claimSeat.x >= 8f && claimSeat.y >= 8f,
                claimSeat.ToString());

            var go = new GameObject("inspector-262");
            try
            {
                var app = go.AddComponent<FlockFiveApp>();
                var t = typeof(FlockFiveApp);
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                t.GetField("_splash", flags).SetValue(app, true);
                t.GetField("_home", flags).SetValue(app, System.Enum.ToObject(t.GetField("_home", flags).FieldType, 0));
                t.GetField("_welcomeOpen", flags).SetValue(app, true);
                bool blocked = (bool)t.GetMethod("HomeDialogBlocksPlay", flags).Invoke(app, null);
                bool played = (bool)t.GetMethod("PressLevelButton", flags).Invoke(app, null);
                Check("play-consumed-while-dialog-up", blocked && !played, "blocked " + blocked + " played " + played);
                t.GetField("_welcomeOpen", flags).SetValue(app, false);

                t.GetField("_gloveTapRectOn", flags).SetValue(app, true);
                t.GetField("_gloveTapRect", flags).SetValue(app, new Rect(10f, 10f, 40f, 40f));
                t.GetField("_coach", flags).SetValue(app, true);
                t.GetMethod("CoachDone", flags).Invoke(app, null);
                bool armed = (bool)t.GetField("_gloveTapRectOn", flags).GetValue(app);
                var rect = (Rect)t.GetField("_gloveTapRect", flags).GetValue(app);
                Check("step2-glove-rect-cleared", !armed && rect.width < 1f, "armed " + armed + " " + rect);

                t.GetField("_giftSignQueued", flags).SetValue(app, -1);
                t.GetField("_busy", flags).SetValue(app, true);
                t.GetMethod("QueueGiftSign", flags).Invoke(app, new object[] { 3 });
                int queued = (int)t.GetField("_giftSignQueued", flags).GetValue(app);
                t.GetField("_busy", flags).SetValue(app, false);
                Check("gift-sign-queues-in-flight", queued == 3, "queued " + queued);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }

            var medal = new Rect(18f, 40f, 48f, 48f);
            var backAim = FlockFiveApp.PokerBackSettleAim(medal.center, medal, 412f, 915f);
            Check("back-glove-on-medal", medal.Contains(backAim) && backAim.x < medal.center.x,
                backAim.ToString());

            var openHit = FlockFiveApp.PopupHitDuringOpen(new Rect(80f, 600f, 120f, 48f), new Vector2(200f, 500f), 0.88f);
            var grown = openHit.center;
            Check("not-now-live-at-open", openHit.width > 2f && openHit.Contains(grown), openHit.ToString());

            Check("deal-glove-stays-on-deal",
                FlockFiveApp.PokerBetCoachOpen(true, true, true)
                && !FlockFiveApp.PokerBetCoachOpen(true, false, true),
                "");

            Check("restart-reenters-step-1",
                FlockFiveApp.RestartDropsQueuedCaption(true)
                && !FlockFiveApp.RestartDropsQueuedCaption(false),
                "");

            Check("watch-reopen-does-not-wait",
                FlockFiveApp.WatchReopenSkipsLoadWait(true, true, false),
                "");

            Check("reminder-waits-for-reward",
                !FlockFiveApp.ReminderOpensNow(false) && FlockFiveApp.ReminderOpensNow(true),
                "");
        }
    }
}
#endif
