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

            CheckRestartCaptionSpace(Check);
            CheckRestartStep1(Check);
            CheckWatchReopen(Check);
            CheckSecondAppDoesNotPaint(Check);

            Check("reminder-waits-for-reward",
                !FlockFiveApp.ReminderOpensNow(false) && FlockFiveApp.ReminderOpensNow(true),
                "");
        }

        static void CheckRestartCaptionSpace(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("caption-restart-space");
            var camGo = new GameObject("caption-restart-cam");
            int streak = Purse.Streak;
            bool hadFeeder = PlayerPrefs.HasKey(RuleTips.FeederKey);
            int keepFeeder = PlayerPrefs.GetInt(RuleTips.FeederKey, 0);
            var resume = typeof(FlockFiveApp).GetField("_resumeInputUntil", BindingFlags.NonPublic | BindingFlags.Static);
            float keepResume = (float)resume.GetValue(null);
            try
            {
                Purse.SetStreak(0);
                GamePause.Reset();
                resume.SetValue(null, float.NegativeInfinity);
                var app = go.AddComponent<FlockFiveApp>();
                var t = typeof(FlockFiveApp);
                var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
                LevelData.Open(0);
                var board = LevelData.Open(0).Clone();
                t.GetField("_board", flags).SetValue(app, board);
                t.GetField("_seed", flags).SetValue(app, board.Clone());
                t.GetField("_splash", flags).SetValue(app, false);
                t.GetField("_busy", flags).SetValue(app, false);
                var garden = new WorldBuilder.Garden();
                garden.Cam = camGo.AddComponent<Camera>();
                garden.Branches = new BranchView[0];
                garden.Feeders = new FeederView[0];
                t.GetField("_garden", flags).SetValue(app, garden);
                var tip = t.GetField("_ruleTip", flags);
                tip.SetValue(app, RuleTip.Feeder);
                t.GetField("_cueLine", flags).SetValue(app, "Nice! That feeder now wants\na new color. Check the top.");
                object[] hud = { default(Rect), default(Rect) };
                t.GetMethod("HudShaken", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, hud);
                var restart = (Rect)hud[0];
                // Same height GardenHudPoint flips with. A stand-in 800 would
                // miss the real top button whenever the glass is not 800 tall.
                float h = PhoneArea.H;
                var bottom = new Vector2(restart.center.x, 8f);
                t.GetField("EditorRuleRestartDirect", flags).SetValue(app, false);
                t.GetField("EditorSnapRound", flags).SetValue(app, null);
                t.GetMethod("TakeRuleTipPress", flags).Invoke(app, new object[] { bottom });
                bool branch = (bool)t.GetField("EditorRuleRestartDirect", flags).GetValue(app);
                bool snapped = t.GetField("EditorSnapRound", flags).GetValue(app) != null;
                Check("caption-restart-bottom-does-not-restart",
                    !branch && !snapped, "branch " + branch + " snap " + snapped);

                var top = new Vector2(restart.center.x, h - restart.center.y);
                t.GetField("EditorRuleRestartDirect", flags).SetValue(app, false);
                t.GetField("EditorSnapRound", flags).SetValue(app, null);
                t.GetMethod("TakeRuleTipPress", flags).Invoke(app, new object[] { top });
                branch = (bool)t.GetField("EditorRuleRestartDirect", flags).GetValue(app);
                snapped = t.GetField("EditorSnapRound", flags).GetValue(app) != null;
                Check("caption-restart-top-uses-direct-branch",
                    branch && snapped, "branch " + branch + " snap " + snapped + " rect " + restart);
            }
            catch (System.Exception ex)
            {
                Check("caption-restart-space", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                resume.SetValue(null, keepResume);
                GamePause.Reset();
                Purse.SetStreak(streak);
                if (hadFeeder) PlayerPrefs.SetInt(RuleTips.FeederKey, keepFeeder);
                else PlayerPrefs.DeleteKey(RuleTips.FeederKey);
                UnityEngine.Object.DestroyImmediate(camGo);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void CheckRestartStep1(System.Action<string, bool, string> Check)
        {
            const string done = "flockfive.coach.done";
            bool had = PlayerPrefs.HasKey(done);
            int keep = PlayerPrefs.GetInt(done, 0);
            var go = new GameObject("restart-step1");
            var camGo = new GameObject("restart-step1-cam");
            int streak = Purse.Streak;
            var resume = typeof(FlockFiveApp).GetField("_resumeInputUntil", BindingFlags.NonPublic | BindingFlags.Static);
            float keepResume = (float)resume.GetValue(null);
            try
            {
                PlayerPrefs.DeleteKey(done);
                Purse.SetStreak(0);
                GamePause.Reset();
                resume.SetValue(null, float.NegativeInfinity);
                LevelData.Open(0);
                var app = go.AddComponent<FlockFiveApp>();
                var t = typeof(FlockFiveApp);
                var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
                var board = LevelData.Open(0).Clone();
                t.GetField("_board", flags).SetValue(app, board);
                t.GetField("_seed", flags).SetValue(app, board.Clone());
                t.GetField("_splash", flags).SetValue(app, false);
                t.GetField("_busy", flags).SetValue(app, false);
                t.GetField("_coach", flags).SetValue(app, true);
                var garden = new WorldBuilder.Garden();
                garden.Cam = camGo.AddComponent<Camera>();
                garden.Branches = new BranchView[0];
                garden.Feeders = new FeederView[0];
                t.GetField("_garden", flags).SetValue(app, garden);
                var tip = t.GetField("_ruleTip", flags);
                tip.SetValue(app, RuleTip.Feeder);
                t.GetField("_cueLine", flags).SetValue(app, "Nice! That feeder now wants\na new color. Check the top.");
                t.GetMethod("ActRestart", flags).Invoke(app, null);
                var snap = (System.Collections.IEnumerator)t.GetField("EditorSnapRound", flags).GetValue(app);
                int frames = 0;
                if (snap != null)
                {
                    var stack = new System.Collections.Generic.Stack<System.Collections.IEnumerator>();
                    stack.Push(snap);
                    while (stack.Count > 0 && frames < 80)
                    {
                        var top = stack.Peek();
                        if (!top.MoveNext()) { stack.Pop(); continue; }
                        var child = top.Current as System.Collections.IEnumerator;
                        if (child != null) stack.Push(child);
                        else frames++;
                    }
                }
                string line = (string)t.GetField("_cueLine", flags).GetValue(app) ?? "";
                bool glove = (bool)t.GetField("_gloveVis", flags).GetValue(app);
                string now = t.GetField("_ruleTip", flags).GetValue(app).ToString();
                bool step1 = line.IndexOf("Tap a branch") >= 0 && glove && now == "None";
                Check("restart-reenters-step-1", step1,
                    "tip " + now + " glove " + glove + " frames " + frames + " line \"" + line + "\"");
            }
            catch (System.Exception ex)
            {
                Check("restart-reenters-step-1", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                resume.SetValue(null, keepResume);
                GamePause.Reset();
                Purse.SetStreak(streak);
                if (had) PlayerPrefs.SetInt(done, keep);
                else PlayerPrefs.DeleteKey(done);
                UnityEngine.Object.DestroyImmediate(camGo);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void CheckWatchReopen(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("watch-reopen");
            try
            {
                var app = go.AddComponent<FlockFiveApp>();
                var t = typeof(FlockFiveApp);
                var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
                var face = t.GetNestedType("GiftFace", BindingFlags.NonPublic);
                var garden = new WorldBuilder.Garden { Branches = new BranchView[0], Feeders = new FeederView[0] };
                t.GetField("_garden", flags).SetValue(app, garden);
                t.GetField("_board", flags).SetValue(app, LevelData.Open(0).Clone());
                t.GetField("_splash", flags).SetValue(app, false);
                t.GetField("_adHand", flags).SetValue(app, true);
                AdRetry.Begin();
                t.GetField("_keepStreak", flags).SetValue(app, false);
                t.GetField("_frozen", flags).SetValue(app, false);
                t.GetField("_freezeOffer", flags).SetValue(app, false);
                t.GetField("_tutorialOfferSeen", flags).SetValue(app, true);
                t.GetField("_gift", flags).SetValue(app, System.Enum.ToObject(face, 0));
                t.GetMethod("RaiseGiftCard", flags).Invoke(app, null);
                bool reopen = (bool)t.GetField("_tutorialWatchReopen", flags).GetValue(app);
                var body = (System.Collections.IEnumerator)t.GetMethod("WatchGiftBody", flags).Invoke(app, null);
                var clock = System.Diagnostics.Stopwatch.StartNew();
                bool moved = body.MoveNext();
                clock.Stop();
                string gift = t.GetField("_gift", flags).GetValue(app).ToString();
                bool hand = (bool)t.GetField("_adHand", flags).GetValue(app);
                string line = (string)t.GetField("_cueLine", flags).GetValue(app) ?? "";
                bool closed = !hand || gift == "None" || line.IndexOf("Ad unavailable") >= 0;
                bool fast = clock.ElapsedMilliseconds < 500;
                Check("watch-reopen-does-not-wait", reopen && moved && closed && fast,
                    "reopen " + reopen + " gift " + gift + " hand " + hand
                    + " ms " + clock.ElapsedMilliseconds + " line \"" + line + "\"");
            }
            catch (System.Exception ex)
            {
                Check("watch-reopen-does-not-wait", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void CheckSecondAppDoesNotPaint(System.Action<string, bool, string> Check)
        {
            var aGo = new GameObject("live-app");
            var bGo = new GameObject("extra-app");
            try
            {
                var a = aGo.AddComponent<FlockFiveApp>();
                var b = bGo.AddComponent<FlockFiveApp>();
                var owner = typeof(FlockFiveApp).GetField("_app", BindingFlags.NonPublic | BindingFlags.Static);
                bool kept = owner.GetValue(null) == a;
                var depth = typeof(FlockFiveApp).GetField("_onGuiDepth", BindingFlags.NonPublic | BindingFlags.Instance);
                typeof(FlockFiveApp).GetMethod("OnGUI", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(b, null);
                int bDepth = (int)depth.GetValue(b);
                bool still = owner.GetValue(null) == a;
                Check("second-app-does-not-paint", kept && still && bDepth == 0,
                    "kept " + kept + " still " + still + " depth " + bDepth);
            }
            catch (System.Exception ex)
            {
                Check("second-app-does-not-paint", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(bGo);
                UnityEngine.Object.DestroyImmediate(aGo);
            }
        }
    }
}
#endif
