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
            CheckPillarRuleHud(Check);
            CheckSecondAppDoesNotPaint(Check);
            CheckAppHandoff(Check);
            CheckSuccessorLowestId(Check);

            CheckFirstGardenGloves(Check);
            CheckGardenBirdPoke(Check);
            CheckFreshGardenPoke(Check);
            CheckHudHitMatchesDrawn(Check);
            CheckCoachTapEdgeFlip(Check);
            CheckGloveClearance(Check);
            CheckGloveDipFrames(Check);
            CheckStationaryTaps(Check);
            CheckBirdBeatsHomeGate(Check);
            CheckNotNowDuringOpen(Check);
            CheckFivesClose(Check);
            CheckFivesCloseConsumesPointer(Check);
            CheckBirdTapConsumesPointer(Check);
            CheckNotNowRelease(Check);
            CheckWatchConsumesPointer(Check);
            CheckGiftXConsumesPointer(Check);
            CheckGiftPressSkipsRestart(Check);
            CheckWatchPressStartsOnRelease(Check);
            CheckFivesPressSkipsRestart(Check);
            CheckNotNowPressSkipsRestart(Check);
            CheckNamingPressSkipsRestart(Check);
            CheckNamingControls(Check);
            CheckWindowGiftPress(Check);
            CheckPointerGuard(Check);
            CheckRuleReentrant(Check);
            CheckGiftStampBeforeGui(Check);
            CheckGiftRestartCovers(Check);
            CheckRestartDoesNotArmLessonGuard(Check);
            CheckSameFrameLifts(Check);
            CheckDistinctReleaseIds(Check);
            CheckRestartAskAfterResume(Check);
            CheckReminderAfterReward(Check);
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
                t.GetField("_busy", flags).SetValue(app, false);
                t.GetField("_restarting", flags).SetValue(app, false);
                tip.SetValue(app, RuleTip.Kit);
                t.GetField("_cueLine", flags).SetValue(app, "Crowns go with crowns. Stack five the same.");
                t.GetMethod("ActRestart", flags).Invoke(app, null);
                var snap2 = (System.Collections.IEnumerator)t.GetField("EditorSnapRound", flags).GetValue(app);
                if (snap2 != null)
                {
                    var stack2 = new System.Collections.Generic.Stack<System.Collections.IEnumerator>();
                    stack2.Push(snap2);
                    int frames2 = 0;
                    while (stack2.Count > 0 && frames2 < 80)
                    {
                        var top2 = stack2.Peek();
                        if (!top2.MoveNext()) { stack2.Pop(); continue; }
                        var child2 = top2.Current as System.Collections.IEnumerator;
                        if (child2 != null) stack2.Push(child2);
                        else frames2++;
                    }
                }
                string line2 = (string)t.GetField("_cueLine", flags).GetValue(app) ?? "";
                bool again = line2.IndexOf("Tap a branch") >= 0
                    && line2.IndexOf("Crowns") < 0 && line2.IndexOf("Nice!") < 0;
                Check("restart-second-tap-stays-step-1", again, "line \"" + line2 + "\"");
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
            bool ads = Ads.Enabled;
            bool granted = Ads.LastGranted;
            bool earned = Ads.LastEarned;
            int ready = (int)typeof(Ads).GetField("_readyOverride", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            bool hadPend = PlayerPrefs.HasKey(RewardedGrant.Pref);
            string pend = PlayerPrefs.GetString(RewardedGrant.Pref, "");
            bool hadPendX = PlayerPrefs.HasKey(RewardedGrant.Pref + ".x");
            string pendX = PlayerPrefs.GetString(RewardedGrant.Pref + ".x", "");
            try
            {
                // Earlier suites leave the ad unit, the earn bit, and the retry
                // flag wherever they finished. This hand is a fresh save.
                Ads.Enabled = true;
                Ads.LastGranted = false;
                Ads.LastEarned = false;
                Ads.SetReadyOverride(0);
                AdRetry.ResetForTests();
                RewardedGrant.ResetEmpty();
                var app = go.AddComponent<FlockFiveApp>();
                var t = typeof(FlockFiveApp);
                var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
                var face = t.GetNestedType("GiftFace", BindingFlags.NonPublic);
                var garden = new WorldBuilder.Garden { Branches = new BranchView[0], Feeders = new FeederView[0] };
                t.GetField("_garden", flags).SetValue(app, garden);
                t.GetField("_board", flags).SetValue(app, LevelData.Open(0).Clone());
                t.GetField("_splash", flags).SetValue(app, false);
                t.GetField("_adHand", flags).SetValue(app, true);
                t.GetField("_watchGiftLive", flags).SetValue(app, false);
                t.GetField("_keepStreak", flags).SetValue(app, false);
                t.GetField("_frozen", flags).SetValue(app, false);
                t.GetField("_freezeOffer", flags).SetValue(app, false);
                t.GetField("_tutorialOfferSeen", flags).SetValue(app, true);
                t.GetField("_tutorialWatchReopen", flags).SetValue(app, false);
                t.GetField("_gift", flags).SetValue(app, System.Enum.ToObject(face, 0));
                t.GetField("_coach", flags).SetValue(app, true);
                t.GetField("_gloveVis", flags).SetValue(app, true);
                t.GetField("_gloveTapRectOn", flags).SetValue(app, true);
                t.GetField("_gloveTapRect", flags).SetValue(app, new Rect(10f, 10f, 80f, 40f));
                t.GetMethod("CoachDone", flags).Invoke(app, null);
                bool lessonClear = !(bool)t.GetField("_gloveVis", flags).GetValue(app)
                    && !(bool)t.GetField("_gloveTapRectOn", flags).GetValue(app);
                Check("lesson-end-clears-glove-rect", lessonClear, "vis "
                    + t.GetField("_gloveVis", flags).GetValue(app) + " rect "
                    + t.GetField("_gloveTapRectOn", flags).GetValue(app));
                t.GetField("_adHand", flags).SetValue(app, true);
                t.GetField("_gloveTapRectOn", flags).SetValue(app, true);
                t.GetMethod("RaiseGiftCard", flags).Invoke(app, null);
                bool cardClear = !(bool)t.GetField("_gloveTapRectOn", flags).GetValue(app);
                Check("gift-card-clears-step-glove", cardClear, "rect " + t.GetField("_gloveTapRectOn", flags).GetValue(app));
                bool reopen = (bool)t.GetField("_tutorialWatchReopen", flags).GetValue(app);
                var body = (System.Collections.IEnumerator)t.GetMethod("WatchGiftBody", flags).Invoke(app, null);
                bool moved = body.MoveNext();
                var yielded = body.Current;
                bool adWait = yielded is WaitForSeconds || yielded is WaitForSecondsRealtime;
                string gift = t.GetField("_gift", flags).GetValue(app).ToString();
                bool hand = (bool)t.GetField("_adHand", flags).GetValue(app);
                string line = (string)t.GetField("_cueLine", flags).GetValue(app) ?? "";
                bool closed = !hand || gift == "None" || line.IndexOf("Ad unavailable") >= 0;
                Check("watch-reopen-does-not-wait", reopen && moved && closed && !adWait,
                    "reopen " + reopen + " gift " + gift + " hand " + hand
                    + " wait " + adWait + " line \"" + line + "\"");
            }
            catch (System.Exception ex)
            {
                Check("watch-reopen-does-not-wait", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                Ads.Enabled = ads;
                Ads.LastGranted = granted;
                Ads.LastEarned = earned;
                Ads.SetReadyOverride(ready);
                if (hadPend) PlayerPrefs.SetString(RewardedGrant.Pref, pend);
                else PlayerPrefs.DeleteKey(RewardedGrant.Pref);
                if (hadPendX) PlayerPrefs.SetString(RewardedGrant.Pref + ".x", pendX);
                else PlayerPrefs.DeleteKey(RewardedGrant.Pref + ".x");
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void CheckSecondAppDoesNotPaint(System.Action<string, bool, string> Check)
        {
            var aGo = new GameObject("live-app");
            var bGo = new GameObject("extra-app");
            try
            {
                var field = typeof(FlockFiveApp).GetField("_app", BindingFlags.NonPublic | BindingFlags.Static);
                var passes = typeof(FlockFiveApp).GetField("EditorGuiPasses", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                var prior = field.GetValue(null) as FlockFiveApp;
                bool priorLive = prior != null && prior.isActiveAndEnabled;
                var a = aGo.AddComponent<FlockFiveApp>();
                var b = bGo.AddComponent<FlockFiveApp>();
                var now = field.GetValue(null) as FlockFiveApp;
                // An enabled owner stays. A missing or disabled owner is claimed by the first new instance.
                bool kept = priorLive ? ReferenceEquals(now, prior) : ReferenceEquals(now, a);
                int before = (int)passes.GetValue(b);
                InvokeOnGui(b);
                bool silent = (int)passes.GetValue(b) == before && !ReferenceEquals(field.GetValue(null), b);
                bool still = ReferenceEquals(field.GetValue(null), now);
                Check("second-app-does-not-paint", kept && still && silent && now != null && now.isActiveAndEnabled,
                    "kept " + kept + " still " + still + " silent " + silent
                    + " prior " + priorLive + " owner " + (now == a ? "a" : now == prior ? "prior" : now == b ? "b" : "other"));
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

        static void CheckAppHandoff(System.Action<string, bool, string> Check)
        {
            var aGo = new GameObject("handoff-a");
            var bGo = new GameObject("handoff-b");
            FlockFiveApp[] parked = null;
            bool[] was = null;
            try
            {
                var field = typeof(FlockFiveApp).GetField("_app", BindingFlags.NonPublic | BindingFlags.Static);
                parked = UnityEngine.Object.FindObjectsByType<FlockFiveApp>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                was = new bool[parked.Length];
                for (int i = 0; i < parked.Length; i++)
                {
                    was[i] = parked[i] != null && parked[i].enabled;
                    if (was[i]) parked[i].enabled = false;
                }
                var a = aGo.AddComponent<FlockFiveApp>();
                var b = bGo.AddComponent<FlockFiveApp>();
                var owner = field.GetValue(null) as FlockFiveApp;
                bool aOwned = ReferenceEquals(owner, a);
                // Edit mode does not call OnDisable when enabled is cleared, unless
                // the behaviour is ExecuteAlways. The shipping method is the same
                // one play mode calls, so invoke it after the component is disabled
                // and FindObjects can no longer treat it as the live owner.
                a.enabled = false;
                typeof(FlockFiveApp).GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(a, null);
                var next = field.GetValue(null) as FlockFiveApp;
                bool promoted = ReferenceEquals(next, b) && b.isActiveAndEnabled;
                var passes = typeof(FlockFiveApp).GetField("EditorGuiPasses", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                var depth = typeof(FlockFiveApp).GetField("_onGuiDepth", BindingFlags.NonPublic | BindingFlags.Instance);
                int before = (int)passes.GetValue(b);
                InvokeOnGui(b);
                bool painted = (int)passes.GetValue(b) == before + 1 && (int)depth.GetValue(b) == 0;
                int aBefore = (int)passes.GetValue(a);
                a.enabled = true;
                typeof(FlockFiveApp).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(a, null);
                var after = field.GetValue(null) as FlockFiveApp;
                InvokeOnGui(a);
                bool onePainter = ReferenceEquals(after, b) && (int)passes.GetValue(a) == aBefore;
                Check("disabled-owner-hands-off", aOwned && promoted && painted && onePainter,
                    "owned " + aOwned + " promoted " + promoted + " painted " + painted + " one " + onePainter);
            }
            catch (System.Exception ex)
            {
                Check("disabled-owner-hands-off", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(bGo);
                UnityEngine.Object.DestroyImmediate(aGo);
                if (parked != null)
                {
                    for (int i = 0; i < parked.Length; i++)
                        if (parked[i] != null) parked[i].enabled = was[i];
                }
            }
        }

        static void CheckSuccessorLowestId(System.Action<string, bool, string> Check)
        {
            var gos = new[] { new GameObject("succ-a"), new GameObject("succ-b"), new GameObject("succ-c") };
            FlockFiveApp[] parked = null;
            bool[] was = null;
            try
            {
                var field = typeof(FlockFiveApp).GetField("_app", BindingFlags.NonPublic | BindingFlags.Static);
                var off = typeof(FlockFiveApp).GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance);
                parked = UnityEngine.Object.FindObjectsByType<FlockFiveApp>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                was = new bool[parked.Length];
                for (int i = 0; i < parked.Length; i++)
                {
                    was[i] = parked[i] != null && parked[i].enabled;
                    if (was[i]) parked[i].enabled = false;
                }
                var apps = new FlockFiveApp[3];
                for (int i = 0; i < 3; i++)
                    apps[i] = gos[i].AddComponent<FlockFiveApp>();
                var owner = field.GetValue(null) as FlockFiveApp;
                bool oneOf = owner == apps[0] || owner == apps[1] || owner == apps[2];
                int want = LowestEnabledId(apps, owner);
                owner.enabled = false;
                off.Invoke(owner, null);
                var first = field.GetValue(null) as FlockFiveApp;
                bool handed = first != null && first.GetInstanceID() == want && first.isActiveAndEnabled;
                field.SetValue(null, owner);
                off.Invoke(owner, null);
                var second = field.GetValue(null) as FlockFiveApp;
                bool stable = ReferenceEquals(second, first);
                Check("successor-lowest-id", oneOf && handed && stable,
                    "one " + oneOf + " handed " + handed + " stable " + stable
                    + " want " + want + " got " + (first != null ? first.GetInstanceID() : 0));
            }
            catch (System.Exception ex)
            {
                Check("successor-lowest-id", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                for (int i = 0; i < gos.Length; i++)
                    UnityEngine.Object.DestroyImmediate(gos[i]);
                if (parked != null)
                {
                    for (int i = 0; i < parked.Length; i++)
                        if (parked[i] != null) parked[i].enabled = was[i];
                }
            }
        }

        static int LowestEnabledId(FlockFiveApp[] apps, FlockFiveApp leaving)
        {
            int want = 0;
            bool any = false;
            for (int i = 0; i < apps.Length; i++)
            {
                var c = apps[i];
                if (c == null || c == leaving || !c.enabled) continue;
                int id = c.GetInstanceID();
                if (!any || id < want)
                {
                    want = id;
                    any = true;
                }
            }
            return want;
        }

        static void CheckPillarRuleHud(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("pillar-rule-hud");
            var camGo = new GameObject("pillar-rule-cam");
            int streak = Purse.Streak;
            var winOn = typeof(PhoneArea).GetField("EditorWindowOn", BindingFlags.NonPublic | BindingFlags.Static);
            var winW = typeof(PhoneArea).GetField("EditorWindowW", BindingFlags.NonPublic | BindingFlags.Static);
            var winH = typeof(PhoneArea).GetField("EditorWindowH", BindingFlags.NonPublic | BindingFlags.Static);
            bool keepOn = (bool)winOn.GetValue(null);
            int keepW = (int)winW.GetValue(null);
            int keepH = (int)winH.GetValue(null);
            var resume = typeof(FlockFiveApp).GetField("_resumeInputUntil", BindingFlags.NonPublic | BindingFlags.Static);
            float keepResume = (float)resume.GetValue(null);
            bool hadFeeder = PlayerPrefs.HasKey(RuleTips.FeederKey);
            int keepFeeder = PlayerPrefs.GetInt(RuleTips.FeederKey, 0);
            Event prev = Event.current;
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
                // 820 pillar first, then the phones. The window drives W, H, and OffX,
                // so each size lays out its own Restart and Home.
                foreach (var size in new[] { new Vector2(820f, 1180f), new Vector2(402f, 874f), new Vector2(412f, 915f) })
                {
                    winOn.SetValue(null, true);
                    winW.SetValue(null, (int)size.x);
                    winH.SetValue(null, (int)size.y);
                    int off = PhoneArea.OffX;
                    int areaW = PhoneArea.W;
                    string tag = size.x.ToString("0");
                    bool sized = size.x > 500f
                        ? areaW > 600 && off == ((int)size.x - areaW) / 2 && off > 0
                        : areaW == (int)size.x && off == 0;
                    object[] hud = { default(Rect), default(Rect) };
                    t.GetMethod("HudShaken", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, hud);
                    var restart = (Rect)hud[0];
                    var home = (Rect)t.GetMethod("HomeHudRect", flags).Invoke(app, null);
                    bool onGlass = restart.xMax <= areaW + 0.5f && home.xMax <= areaW + 0.5f && restart.xMin > home.xMax;
                    bool restartHit = false, homeHit = false, outRestart = false, outHome = false;
                    bool falseRestart = false, falseHome = false;
                    int swR = 0, prR = 0, swH = 0, prH = 0, swO = 0, prO = 0, swB = 0, prB = 0;
                    string faultR = null, faultH = null, faultO = null, faultB = null;
                    RuleHudTap(app, t, flags, new Vector2(restart.center.x + off, restart.center.y), out restartHit, out _, out swR, out prR, out faultR);
                    RuleHudTap(app, t, flags, new Vector2(home.center.x + off, home.center.y), out _, out homeHit, out swH, out prH, out faultH);
                    RuleHudTap(app, t, flags, new Vector2(restart.xMax + 6f + off, restart.center.y), out outRestart, out outHome, out swO, out prO, out faultO);
                    if (off != 0)
                        RuleHudTap(app, t, flags, new Vector2(restart.center.x, restart.center.y), out falseRestart, out falseHome, out swB, out prB, out faultB);
                    bool reached = prR > 0 && prH > 0 && prO > 0 && (off == 0 || prB > 0);
                    string faults = faultR ?? faultH ?? faultO ?? faultB;
                    Check("pillar-rule-hud-" + tag,
                        faults == null && reached && sized && onGlass && restartHit && homeHit && !outRestart && !outHome && !falseRestart && !falseHome,
                        "reached swallow " + swR + "/" + swH + "/" + swO + "/" + swB
                        + " press " + prR + "/" + prH + "/" + prO + "/" + prB
                        + " off " + off + " w " + areaW
                        + " restart " + restartHit + " home " + homeHit
                        + " outR " + outRestart + " outH " + outHome
                        + " bare " + falseRestart + "/" + falseHome
                        + " r " + restart + " h " + home
                        + (faults == null ? "" : " THREW " + faults));
                }
            }
            catch (System.Exception ex)
            {
                Check("pillar-rule-hud", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                Event.current = prev;
                winOn.SetValue(null, keepOn);
                winW.SetValue(null, keepW);
                winH.SetValue(null, keepH);
                resume.SetValue(null, keepResume);
                GamePause.Reset();
                Purse.SetStreak(streak);
                if (hadFeeder) PlayerPrefs.SetInt(RuleTips.FeederKey, keepFeeder);
                else PlayerPrefs.DeleteKey(RuleTips.FeederKey);
                UnityEngine.Object.DestroyImmediate(camGo);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void RuleHudTap(FlockFiveApp app, System.Type t, BindingFlags flags, Vector2 windowGui, out bool restart, out bool home, out int swallow, out int press, out string fault)
        {
            swallow = 0;
            press = 0;
            fault = null;
            t.GetField("_splash", flags).SetValue(app, false);
            t.GetField("_busy", flags).SetValue(app, false);
            t.GetField("_restarting", flags).SetValue(app, false);
            t.GetField("_gloveVis", flags).SetValue(app, false);
            t.GetField("_ruleEatGesture", flags).SetValue(app, false);
            var guard = t.GetField("_lessonGuardUntil", flags);
            if (guard != null) guard.SetValue(app, 0f);
            t.GetField("_ruleTip", flags).SetValue(app, RuleTip.Feeder);
            t.GetField("_cueLine", flags).SetValue(app, "Nice! That feeder now wants\na new color. Check the top.");
            t.GetField("EditorRuleRestartDirect", flags).SetValue(app, false);
            t.GetField("EditorSnapRound", flags).SetValue(app, null);
            t.GetField("EditorRuleSwallowEntered", flags).SetValue(app, 0);
            t.GetField("EditorRulePressEntered", flags).SetValue(app, 0);
            // OnGUIBody draws after the swallow and throws outside a native OnGUI.
            // Event.current is also not a reliable MouseDown there. Drive the
            // caption methods directly. Swallow still runs first so its counter
            // shows whether the event arrived; if it never calls the press, the
            // Swallow reads the window point through PlayPointer.Enter.
            Event.current = new Event { type = EventType.MouseDown, mousePosition = windowGui, button = 0 };
            fault = InvokeRule(t, flags, app, "SwallowRuleTipPointer", null);
            if (fault == null && (int)t.GetField("EditorRulePressEntered", flags).GetValue(app) == 0)
            {
                var local = PlayPointer.Enter(windowGui);
                var play = new Vector2(local.x, PhoneArea.H - local.y);
                fault = InvokeRule(t, flags, app, "TakeRuleTipPress", new object[] { play });
            }
            swallow = (int)t.GetField("EditorRuleSwallowEntered", flags).GetValue(app);
            press = (int)t.GetField("EditorRulePressEntered", flags).GetValue(app);
            bool direct = (bool)t.GetField("EditorRuleRestartDirect", flags).GetValue(app);
            bool snapped = t.GetField("EditorSnapRound", flags).GetValue(app) != null;
            bool splash = (bool)t.GetField("_splash", flags).GetValue(app);
            restart = direct && snapped && !splash;
            home = splash && !direct && !snapped;
        }

        static string InvokeRule(System.Type t, BindingFlags flags, FlockFiveApp app, string methodName, object[] args)
        {
            var method = t.GetMethod(methodName, flags);
            if (method == null) return "missing " + methodName;
            try
            {
                method.Invoke(app, args);
                return null;
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                var inner = ex.InnerException ?? ex;
                return inner.GetType().Name + ": " + inner.Message + "\n" + inner.StackTrace;
            }
        }

        static void InvokeOnGui(FlockFiveApp app)
        {
            try
            {
                typeof(FlockFiveApp).GetMethod("OnGUI", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(app, null);
            }
            catch (System.Reflection.TargetInvocationException)
            {
                // A real entry reaches GUI calls. Outside the player loop those throw
                // after the owner check has already counted the pass.
            }
        }

        static void CheckGloveDipFrames(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("glove-dip-frames");
            var winOn = typeof(PhoneArea).GetField("EditorWindowOn", BindingFlags.NonPublic | BindingFlags.Static);
            var winW = typeof(PhoneArea).GetField("EditorWindowW", BindingFlags.NonPublic | BindingFlags.Static);
            var winH = typeof(PhoneArea).GetField("EditorWindowH", BindingFlags.NonPublic | BindingFlags.Static);
            var safeTop = typeof(PhoneArea).GetField("EditorSafeTop", BindingFlags.NonPublic | BindingFlags.Static);
            var safeBot = typeof(PhoneArea).GetField("EditorSafeBottom", BindingFlags.NonPublic | BindingFlags.Static);
            bool keepOn = (bool)winOn.GetValue(null);
            int keepW = (int)winW.GetValue(null);
            int keepH = (int)winH.GetValue(null);
            float keepTop = (float)safeTop.GetValue(null);
            float keepBot = (float)safeBot.GetValue(null);
            try
            {
                var app = go.AddComponent<FlockFiveApp>();
                var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
                var coach = typeof(FlockFiveApp).GetMethod("CoachTapAt", flags);
                var t = typeof(FlockFiveApp);
                string[] sites = { "branch", "bird", "daily", "claim", "deal", "back", "poker", "watch", "play" };
                foreach (var size in new[] { new Vector2(820f, 1180f), new Vector2(402f, 874f), new Vector2(412f, 915f), new Vector2(360f, 800f) })
                {
                    winOn.SetValue(null, true);
                    winW.SetValue(null, (int)size.x);
                    winH.SetValue(null, (int)size.y);
                    safeTop.SetValue(null, 59f);
                    safeBot.SetValue(null, 34f);
                    float w = PhoneArea.W;
                    float h = PhoneArea.H;
                    float top = 59f;
                    float bot = h - 34f;
                    string bad = "";
                    for (int n = 0; n < sites.Length; n++)
                        bad += SweepPoke(app, t, flags, coach, sites[n], FlockFiveApp.CoachTapSiteRect(sites[n], w, h), FlockFiveApp.CoachTapLabel(FlockFiveApp.CoachTapSiteRect(sites[n], w, h)), w, h, top, bot);
                    float reach = FlockFiveApp.GloveSize(DeviceFit.Ui(w, h));
                    bad += SweepEdge(app, t, flags, coach, "left", new Rect(reach + 12f, top + 160f, 72f, 64f), w, h, top, bot);
                    bad += SweepEdge(app, t, flags, coach, "right", new Rect(w - reach - 84f, top + 160f, 72f, 64f), w, h, top, bot);
                    bad += SweepEdge(app, t, flags, coach, "top", new Rect(w * 0.5f + 24f, top + 4f, 80f, 150f), w, h, top, bot);
                    bad += SweepEdge(app, t, flags, coach, "bottom", new Rect(w * 0.28f, bot - 154f, 72f, 140f), w, h, top, bot);
                    Check("glove-dip-frame-in-safe-" + (int)size.x, bad.Length == 0, bad);
                }
            }
            catch (System.Exception ex)
            {
                Check("glove-dip-frame-in-safe", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                winOn.SetValue(null, keepOn);
                winW.SetValue(null, keepW);
                winH.SetValue(null, keepH);
                safeTop.SetValue(null, keepTop);
                safeBot.SetValue(null, keepBot);
                RenderTexture.active = null;
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static string SweepEdge(FlockFiveApp app, System.Type t, BindingFlags flags, System.Reflection.MethodInfo coach,
            string name, Rect target, float w, float h, float top, float bot)
        {
            bool preferLeft = target.center.x >= w * 0.5f;
            var wall = preferLeft
                ? new Rect(0f, top, Mathf.Max(4f, target.xMin + 2f), Mathf.Max(4f, bot - top))
                : new Rect(target.xMax - 2f, top, Mathf.Max(4f, w - (target.xMax - 2f)), Mathf.Max(4f, bot - top));
            return SweepPoke(app, t, flags, coach, name, target, wall, w, h, top, bot);
        }

        static string SweepPoke(FlockFiveApp app, System.Type t, BindingFlags flags, System.Reflection.MethodInfo coach,
            string name, Rect target, Rect avoid, float w, float h, float top, float bot)
        {
            t.GetField("_coachTapOn", flags).SetValue(app, false);
            t.GetField("_glovePhase", flags).SetValue(app, 0f);
            t.GetField("_coachFade", flags).SetValue(app, 1f);
            float bestDip = -1f;
            bool settledIn = false;
            bool settledClear = false;
            string bad = "";
            for (int frame = 0; frame < 24; frame++)
            {
                coach.Invoke(app, new object[] { target, avoid, 0.06f, null });
                var tip = (Vector2)t.GetField("_gloveShown", flags).GetValue(app);
                float dip = (float)t.GetField("_gloveDip", flags).GetValue(app);
                bool fromLeft = (bool)t.GetField("_gloveMirror", flags).GetValue(app);
                var box = FlockFiveApp.CoachTapFrameBox(tip, fromLeft, dip, w, h);
                bool inside = GloveInside(box, w, top, bot);
                bool covers = GloveCovers(box, target);
                if (!inside || covers)
                    bad += name + " f" + frame + " dip " + dip.ToString("0.00") + " " + box + "; ";
                if (dip >= bestDip)
                {
                    bestDip = dip;
                    settledIn = inside;
                    settledClear = !covers;
                }
            }
            if (bestDip > 0.9f && settledIn && settledClear && bad.Length == 0) return "";
            return bad + name + " settled dip " + bestDip.ToString("0.00")
                + " in " + settledIn + " clear " + settledClear + "; ";
        }

        static void CheckGloveClearance(System.Action<string, bool, string> Check)
        {
            var winOn = typeof(PhoneArea).GetField("EditorWindowOn", BindingFlags.NonPublic | BindingFlags.Static);
            var winW = typeof(PhoneArea).GetField("EditorWindowW", BindingFlags.NonPublic | BindingFlags.Static);
            var winH = typeof(PhoneArea).GetField("EditorWindowH", BindingFlags.NonPublic | BindingFlags.Static);
            var safeTop = typeof(PhoneArea).GetField("EditorSafeTop", BindingFlags.NonPublic | BindingFlags.Static);
            var safeBot = typeof(PhoneArea).GetField("EditorSafeBottom", BindingFlags.NonPublic | BindingFlags.Static);
            bool keepOn = (bool)winOn.GetValue(null);
            int keepW = (int)winW.GetValue(null);
            int keepH = (int)winH.GetValue(null);
            float keepTop = (float)safeTop.GetValue(null);
            float keepBot = (float)safeBot.GetValue(null);
            try
            {
                string[] sites = { "branch", "bird", "daily", "claim", "deal", "back", "poker", "watch", "play" };
                foreach (var size in new[] { new Vector2(820f, 1180f), new Vector2(402f, 874f), new Vector2(412f, 915f), new Vector2(360f, 800f) })
                {
                    winOn.SetValue(null, true);
                    winW.SetValue(null, (int)size.x);
                    winH.SetValue(null, (int)size.y);
                    safeTop.SetValue(null, 59f);
                    safeBot.SetValue(null, 34f);
                    float w = PhoneArea.W;
                    float h = PhoneArea.H;
                    float top = 59f;
                    float bot = h - 34f;
                    string cover = "";
                    string outside = "";
                    for (int s = 0; s < sites.Length; s++)
                    {
                        var target = FlockFiveApp.CoachTapSiteRect(sites[s], w, h);
                        var pose = FlockFiveApp.CoachTapSide(target, FlockFiveApp.CoachTapLabel(target), w, h, PhoneArea.Safe, default, default, default);
                        if (GloveCovers(pose.Glove, target)) cover += sites[s] + " " + pose.Glove + "; ";
                        if (!GloveInside(pose.Glove, w, top, bot)) outside += sites[s] + " " + pose.Glove + "; ";
                    }
                    float reach = FlockFiveApp.GloveSize(DeviceFit.Ui(w, h));
                    NoteEdge(w, h, top, bot, "left", new Rect(reach + 12f, top + 160f, 72f, 64f), ref cover, ref outside);
                    NoteEdge(w, h, top, bot, "right", new Rect(w - reach - 84f, top + 160f, 72f, 64f), ref cover, ref outside);
                    NoteEdge(w, h, top, bot, "top", new Rect(w * 0.5f + 24f, top + 4f, 80f, 150f), ref cover, ref outside);
                    NoteEdge(w, h, top, bot, "bottom", new Rect(w * 0.28f, bot - 154f, 72f, 140f), ref cover, ref outside);
                    string tag = ((int)size.x).ToString();
                    Check("glove-target-overlap-" + tag, cover.Length == 0, cover);
                    Check("glove-inside-play-and-safe-" + tag, outside.Length == 0, outside);
                }
            }
            finally
            {
                winOn.SetValue(null, keepOn);
                winW.SetValue(null, keepW);
                winH.SetValue(null, keepH);
                safeTop.SetValue(null, keepTop);
                safeBot.SetValue(null, keepBot);
            }
        }

        static void NoteEdge(float w, float h, float top, float bot, string name, Rect target, ref string cover, ref string outside)
        {
            bool preferLeft = target.center.x >= w * 0.5f;
            var wall = preferLeft
                ? new Rect(0f, top, Mathf.Max(4f, target.xMin + 2f), Mathf.Max(4f, bot - top))
                : new Rect(target.xMax - 2f, top, Mathf.Max(4f, w - (target.xMax - 2f)), Mathf.Max(4f, bot - top));
            var pose = FlockFiveApp.CoachTapSide(target, wall, w, h, PhoneArea.Safe, default, default, default);
            if (GloveCovers(pose.Glove, target)) cover += name + " " + pose.Glove + "; ";
            if (!GloveInside(pose.Glove, w, top, bot)) outside += name + " " + pose.Glove + "; ";
        }

        static bool GloveInside(Rect glove, float w, float top, float bot)
        {
            return glove.xMin >= -0.5f && glove.yMin >= top - 0.5f
                && glove.xMax <= w + 0.5f && glove.yMax <= bot + 0.5f;
        }

        static void CheckCoachTapEdgeFlip(System.Action<string, bool, string> Check)
        {
            var winOn = typeof(PhoneArea).GetField("EditorWindowOn", BindingFlags.NonPublic | BindingFlags.Static);
            var winW = typeof(PhoneArea).GetField("EditorWindowW", BindingFlags.NonPublic | BindingFlags.Static);
            var winH = typeof(PhoneArea).GetField("EditorWindowH", BindingFlags.NonPublic | BindingFlags.Static);
            var safeTop = typeof(PhoneArea).GetField("EditorSafeTop", BindingFlags.NonPublic | BindingFlags.Static);
            var safeBot = typeof(PhoneArea).GetField("EditorSafeBottom", BindingFlags.NonPublic | BindingFlags.Static);
            bool keepOn = (bool)winOn.GetValue(null);
            int keepW = (int)winW.GetValue(null);
            int keepH = (int)winH.GetValue(null);
            float keepTop = (float)safeTop.GetValue(null);
            float keepBot = (float)safeBot.GetValue(null);
            try
            {
                foreach (var size in new[] { new Vector2(820f, 1180f), new Vector2(402f, 874f), new Vector2(412f, 915f), new Vector2(360f, 800f) })
                {
                    winOn.SetValue(null, true);
                    winW.SetValue(null, (int)size.x);
                    winH.SetValue(null, (int)size.y);
                    safeTop.SetValue(null, 59f);
                    safeBot.SetValue(null, 34f);
                    float w = PhoneArea.W;
                    float h = PhoneArea.H;
                    float top = 59f;
                    float bot = h - 34f;
                    float reach = FlockFiveApp.GloveSize(DeviceFit.Ui(w, h));
                    string tag = ((int)size.x).ToString();
                    string bad = "";
                    bad += EdgeFlip(w, h, top, bot, "left", new Rect(reach + 12f, top + 160f, 72f, 64f));
                    bad += EdgeFlip(w, h, top, bot, "right", new Rect(w - reach - 84f, top + 160f, 72f, 64f));
                    bad += EdgeFlip(w, h, top, bot, "top", new Rect(w * 0.5f + 24f, top + 4f, 80f, 150f));
                    bad += EdgeFlip(w, h, top, bot, "bottom", new Rect(w * 0.28f, bot - 154f, 72f, 140f));
                    Check("coach-tap-edge-flip-" + tag, bad.Length == 0, bad);
                }
            }
            finally
            {
                winOn.SetValue(null, keepOn);
                winW.SetValue(null, keepW);
                winH.SetValue(null, keepH);
                safeTop.SetValue(null, keepTop);
                safeBot.SetValue(null, keepBot);
            }
        }

        static string EdgeFlip(float w, float h, float top, float bot, string name, Rect target)
        {
            bool preferLeft = target.center.x >= w * 0.5f;
            var wall = preferLeft
                ? new Rect(0f, top, Mathf.Max(4f, target.xMin + 2f), Mathf.Max(4f, bot - top))
                : new Rect(target.xMax - 2f, top, Mathf.Max(4f, w - (target.xMax - 2f)), Mathf.Max(4f, bot - top));
            var blocked = FlockFiveApp.CoachTapSide(target, wall, w, h, PhoneArea.Safe, default, default, default);
            bool inside = blocked.Glove.xMin >= -0.5f && blocked.Glove.yMin >= top - 0.5f
                && blocked.Glove.xMax <= w + 0.5f && blocked.Glove.yMax <= bot + 0.5f;
            bool flat = Mathf.Abs(Mathf.Abs(blocked.Angle) - 90f) < 1.5f;
            bool edge = blocked.FromLeft
                ? Mathf.Abs(blocked.Glove.xMax - target.xMin) <= 1.2f
                : Mathf.Abs(blocked.Glove.xMin - target.xMax) <= 1.2f;
            edge = edge && !GloveCovers(blocked.Glove, target);
            if (blocked.Flipped && inside && flat && edge && blocked.Mirror == blocked.FromLeft) return "";
            return name + " flip " + blocked.Flipped + " in " + inside + " flat " + flat
                + " edge " + edge + " glove " + blocked.Glove + " end " + blocked.End + "; ";
        }

        static void CheckGardenBirdPoke(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("garden-bird-poke");
            var winOn = typeof(PhoneArea).GetField("EditorWindowOn", BindingFlags.NonPublic | BindingFlags.Static);
            var winW = typeof(PhoneArea).GetField("EditorWindowW", BindingFlags.NonPublic | BindingFlags.Static);
            var winH = typeof(PhoneArea).GetField("EditorWindowH", BindingFlags.NonPublic | BindingFlags.Static);
            var safeTop = typeof(PhoneArea).GetField("EditorSafeTop", BindingFlags.NonPublic | BindingFlags.Static);
            var safeBot = typeof(PhoneArea).GetField("EditorSafeBottom", BindingFlags.NonPublic | BindingFlags.Static);
            bool keepOn = (bool)winOn.GetValue(null);
            int keepW = (int)winW.GetValue(null);
            int keepH = (int)winH.GetValue(null);
            float keepTop = (float)safeTop.GetValue(null);
            float keepBot = (float)safeBot.GetValue(null);
            try
            {
                AdPause.ResetForTest();
                GamePause.Reset();
                PlayClock.StepForTest(0.06f);
                float playDt = PlayClock.Delta;
                float showDt = ShowClock.Delta;
                var app = go.AddComponent<FlockFiveApp>();
                var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
                var coach = typeof(FlockFiveApp).GetMethod("CoachTapAt", flags);
                var t = typeof(FlockFiveApp);
                bool held = true;
                foreach (var size in new[] { new Vector2(820f, 1180f), new Vector2(402f, 874f), new Vector2(412f, 915f), new Vector2(360f, 800f) })
                {
                    winOn.SetValue(null, true);
                    winW.SetValue(null, (int)size.x);
                    winH.SetValue(null, (int)size.y);
                    safeTop.SetValue(null, 59f);
                    safeBot.SetValue(null, 34f);
                    float w = PhoneArea.W;
                    float h = PhoneArea.H;
                    var birds = FlockFiveApp.FirstGardenTipBirds(w, h);
                    var aim = FlockFiveApp.GardenPokeTarget(birds, w, h);
                    var center = FlockFiveApp.FirstGardenBranch(w, h);
                    var wood = FlockFiveApp.PlayGui(BranchHit.WoodRect(center, false, false), w, h);
                    object[] hud = { default(Rect), default(Rect) };
                    t.GetMethod("HudShaken", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, hud);
                    var restart = (Rect)hud[0];
                    var home = CornerAnchor.TopBarHome(restart, w, PhoneArea.Safe);
                    var pose = FlockFiveApp.CoachTapSide(aim, default, w, h, PhoneArea.Safe, home, restart, default);
                    float amp = Mathf.Abs(pose.Rest.x - pose.End.x);
                    bool flush = pose.FromLeft ? pose.Glove.xMin <= 1.5f : pose.Glove.xMax >= w - 1.5f;
                    bool capped = amp + 0.75f < FlockFiveApp.CoachTapPressPx && flush && amp > 0.5f;
                    var endpoint = new Rect(pose.End.x - 1f, pose.End.y - 1f, 2f, 2f);
                    bool onBird = birds.width > 8f && endpoint.Overlaps(birds)
                        && pose.End.x > birds.xMin + 0.5f && pose.End.x < birds.xMax - 0.5f
                        && pose.End.y > birds.yMin + 0.5f && pose.End.y < birds.yMax - 0.5f
                        && !pose.FromLeft && pose.End.x < wood.xMax - 12f;
                    bool inside = GloveInside(pose.Glove, w, 59f, h - 34f);
                    t.GetField("_coachTapOn", flags).SetValue(app, false);
                    t.GetField("_glovePhase", flags).SetValue(app, 0f);
                    t.GetField("_gloveVis", flags).SetValue(app, false);
                    t.GetField("_coachFade", flags).SetValue(app, 1f);
                    Vector2 start = default;
                    Vector2 end = default;
                    float bestDip = -1f;
                    float span = 0f;
                    if (held)
                    {
                        Vector2 still = default;
                        for (int i = 0; i < 4; i++)
                        {
                            coach.Invoke(app, new object[] { aim, default(Rect), 0f, null });
                            var tip = (Vector2)t.GetField("_gloveShown", flags).GetValue(app);
                            if (i == 0) still = tip;
                            else if ((tip - still).magnitude > 0.5f) held = false;
                        }
                        t.GetField("_coachTapOn", flags).SetValue(app, false);
                        t.GetField("_glovePhase", flags).SetValue(app, 0f);
                    }
                    for (int i = 0; i < 24; i++)
                    {
                        coach.Invoke(app, new object[] { aim, default(Rect), playDt, null });
                        var tip = (Vector2)t.GetField("_gloveShown", flags).GetValue(app);
                        float dip = (float)t.GetField("_gloveDip", flags).GetValue(app);
                        if (i == 0) start = tip;
                        span = Mathf.Max(span, (tip - start).magnitude);
                        if (dip >= bestDip) { bestDip = dip; end = tip; }
                    }
                    bool moved = bestDip > 0.9f && span + 0.6f >= amp && (end - pose.End).magnitude < 1.5f;
                    string tag = ((int)size.x).ToString();
                    string detail = "amp " + amp.ToString("0.0") + " span " + span.ToString("0.0")
                        + " end " + pose.End + " birds " + birds + " glove " + pose.Glove;
                    Check("garden-poke-amplitude-" + tag, (amp >= 20f || capped) && moved, detail);
                    Check("garden-fingertip-on-birds-" + tag, onBird, detail);
                    Check("garden-poke-inside-play-" + tag, inside, detail);
                }
                Check("garden-poke-follows-clock",
                    playDt > 0.05f && Mathf.Abs(showDt - playDt) < 0.0001f && held,
                    "play " + playDt.ToString("0.000") + " show " + showDt.ToString("0.000") + " held " + held);
            }
            catch (System.Exception ex)
            {
                Check("garden-bird-poke", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                winOn.SetValue(null, keepOn);
                winW.SetValue(null, keepW);
                winH.SetValue(null, keepH);
                safeTop.SetValue(null, keepTop);
                safeBot.SetValue(null, keepBot);
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
        }

        // Gear and the Settings X are drawn under the pillar matrix. The mouse
        // is already in that space, so the hit point is the drawn center.
        static void CheckHudHitMatchesDrawn(System.Action<string, bool, string> Check)
        {
            var winOn = typeof(PhoneArea).GetField("EditorWindowOn", BindingFlags.NonPublic | BindingFlags.Static);
            var winW = typeof(PhoneArea).GetField("EditorWindowW", BindingFlags.NonPublic | BindingFlags.Static);
            var winH = typeof(PhoneArea).GetField("EditorWindowH", BindingFlags.NonPublic | BindingFlags.Static);
            var safeTop = typeof(PhoneArea).GetField("EditorSafeTop", BindingFlags.NonPublic | BindingFlags.Static);
            var safeBot = typeof(PhoneArea).GetField("EditorSafeBottom", BindingFlags.NonPublic | BindingFlags.Static);
            bool keepOn = (bool)winOn.GetValue(null);
            int keepW = (int)winW.GetValue(null);
            int keepH = (int)winH.GetValue(null);
            float keepTop = (float)safeTop.GetValue(null);
            float keepBot = (float)safeBot.GetValue(null);
            var keepMatrix = GUI.matrix;
            try
            {
                foreach (var size in new[] { new Vector2(820f, 1180f), new Vector2(402f, 874f), new Vector2(412f, 915f), new Vector2(360f, 800f) })
                {
                    winOn.SetValue(null, true);
                    winW.SetValue(null, (int)size.x);
                    winH.SetValue(null, (int)size.y);
                    safeTop.SetValue(null, 59f);
                    safeBot.SetValue(null, 34f);
                    float w = PhoneArea.W;
                    float h = PhoneArea.H;
                    var safe = PhoneArea.Safe;
                    var gear = FlockFiveApp.SettingsGearDrawnRect(w, h, safe);
                    var gearHit = FlockFiveApp.SettingsGearRect(w, h, safe);
                    float top = FlockFiveApp.TopHudBox(h, safe, 0f);
                    var sheet = SettingsSheet.Layout(w, h, safe, top);
                    var close = sheet.Close;
                    GUI.matrix = PhoneArea.GuiBase;
                    var gearAt = PlayPointer.FromGuiMouse(gear.center);
                    var closeAt = PlayPointer.FromGuiMouse(close.center);
                    GUI.matrix = Matrix4x4.identity;
                    var gearWindow = PlayPointer.Enter(new Vector2(gear.center.x + PhoneArea.OffX, gear.center.y));
                    var closeWindow = PlayPointer.Enter(new Vector2(close.center.x + PhoneArea.OffX, close.center.y));
                    bool gearMatch = (gearAt - gear.center).magnitude <= 0.5f && gearHit.Contains(gearAt)
                        && (gearWindow - gear.center).magnitude <= 0.5f && gearHit.Contains(gearWindow);
                    bool closeMatch = (closeAt - close.center).magnitude <= 0.5f && close.Contains(closeAt)
                        && (closeWindow - close.center).magnitude <= 0.5f && close.Contains(closeWindow);
                    string tag = ((int)size.x).ToString();
                    string detail = "gear " + gear.center + " at " + gearAt
                        + " close " + close.center + " at " + closeAt
                        + " off " + PhoneArea.OffX;
                    Check("hud-hit-matches-drawn-" + tag, gearMatch && closeMatch, detail);
                }
            }
            catch (System.Exception ex)
            {
                Check("hud-hit-matches-drawn", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                GUI.matrix = keepMatrix;
                winOn.SetValue(null, keepOn);
                winW.SetValue(null, keepW);
                winH.SetValue(null, keepH);
                safeTop.SetValue(null, keepTop);
                safeBot.SetValue(null, keepBot);
            }
        }

        // The live Update seats the glove through CoachAdvance then CoachPlace.
        // CoachPlace used to clear the poke before that seat, so a direct
        // CoachTapAt loop moved and the game stayed on the rest pose.
        static void CheckFreshGardenPoke(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("fresh-garden-poke");
            var branchGo = new GameObject("fresh-branch");
            var camGo = new GameObject("fresh-cam");
            RenderTexture rt = null;
            var winOn = typeof(PhoneArea).GetField("EditorWindowOn", BindingFlags.NonPublic | BindingFlags.Static);
            var winW = typeof(PhoneArea).GetField("EditorWindowW", BindingFlags.NonPublic | BindingFlags.Static);
            var winH = typeof(PhoneArea).GetField("EditorWindowH", BindingFlags.NonPublic | BindingFlags.Static);
            var safeTop = typeof(PhoneArea).GetField("EditorSafeTop", BindingFlags.NonPublic | BindingFlags.Static);
            var safeBot = typeof(PhoneArea).GetField("EditorSafeBottom", BindingFlags.NonPublic | BindingFlags.Static);
            bool keepOn = (bool)winOn.GetValue(null);
            int keepW = (int)winW.GetValue(null);
            int keepH = (int)winH.GetValue(null);
            float keepTop = (float)safeTop.GetValue(null);
            float keepBot = (float)safeBot.GetValue(null);
            try
            {
                AdPause.ResetForTest();
                GamePause.Reset();
                var app = go.AddComponent<FlockFiveApp>();
                var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
                var t = typeof(FlockFiveApp);
                var advance = t.GetMethod("CoachAdvance", flags);
                var place = t.GetMethod("CoachPlace", flags);
                var view = branchGo.AddComponent<BranchView>();
                view.FromRight = false;
                view.IsGift = false;
                var cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                cam.nearClipPlane = -10f;
                cam.farClipPlane = 50f;
                cam.transform.position = new Vector3(0f, WorldBuilder.CamRestY, -10f);
                var board = new Board();
                var reds = new BranchState();
                for (int i = 0; i < 3; i++) reds.Birds.Add(new Bird(BirdColor.Ruby));
                var lone = new BranchState();
                lone.Birds.Add(new Bird(BirdColor.Ruby));
                board.Branches.Add(reds);
                board.Branches.Add(lone);
                t.GetField("_board", flags).SetValue(app, board);
                t.GetField("_coach", flags).SetValue(app, true);
                t.GetField("_coachMoves", flags).SetValue(app, 0);
                t.GetField("_splash", flags).SetValue(app, false);
                t.GetField("_sel", flags).SetValue(app, -1);
                t.GetField("_cueHand", flags).SetValue(app, false);
                t.GetField("_pestCue", flags).SetValue(app, 0);
                t.GetField("_leafIntro", flags).SetValue(app, false);
                t.GetField("_levelHive", flags).SetValue(app, false);
                t.GetField("_coachFade", flags).SetValue(app, 1f);
                foreach (var size in new[] { new Vector2(820f, 1180f), new Vector2(402f, 874f), new Vector2(412f, 915f), new Vector2(360f, 800f) })
                {
                    winOn.SetValue(null, true);
                    winW.SetValue(null, (int)size.x);
                    winH.SetValue(null, (int)size.y);
                    safeTop.SetValue(null, 59f);
                    safeBot.SetValue(null, 34f);
                    float w = PhoneArea.W;
                    float h = PhoneArea.H;
                    float window = w / Mathf.Max(1f, h);
                    float want = DeviceFit.PlayAspect(window);
                    cam.orthographicSize = WorldBuilder.CamOrtho;
                    cam.aspect = want;
                    if (window < want - 0.03f)
                    {
                        float vh = window / want;
                        cam.rect = new Rect(0f, (1f - vh) * 0.5f, 1f, vh);
                    }
                    else cam.rect = new Rect(0f, 0f, 1f, 1f);
                    if (rt != null) { rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
                    rt = new RenderTexture(Mathf.Max(1, (int)w), Mathf.Max(1, (int)h), 0);
                    rt.Create();
                    cam.targetTexture = rt;
                    var origin = FlockFiveApp.FirstGardenBranch(w, h);
                    branchGo.transform.position = new Vector3(origin.x, origin.y, 0f);
                    var garden = new WorldBuilder.Garden
                    {
                        Cam = cam,
                        Branches = new[] { view, null }
                    };
                    t.GetField("_garden", flags).SetValue(app, garden);
                    t.GetField("_coachTapOn", flags).SetValue(app, false);
                    t.GetField("_coachTapFrame", flags).SetValue(app, -1);
                    t.GetField("_glovePhase", flags).SetValue(app, 0f);
                    t.GetField("_gloveDip", flags).SetValue(app, 0f);
                    t.GetField("_gloveHoldDip", flags).SetValue(app, 0f);
                    t.GetField("_tapSent", flags).SetValue(app, false);
                    t.GetField("_cueSpoken", flags).SetValue(app, null);
                    t.GetField("_cueLine", flags).SetValue(app, null);
                    t.GetField("_coachFrom", flags).SetValue(app, -1);
                    t.GetField("_coachTo", flags).SetValue(app, -1);
                    t.GetField("_sel", flags).SetValue(app, -1);
                    var world = FlockFiveApp.TipBirdsWorld(origin, false, false, 3, 3);
                    var birds = ProjectBirds(world, cam, h);
                    float minX = float.MaxValue, maxX = float.MinValue;
                    Vector2 full = default;
                    float bestDip = -1f;
                    bool inside = true;
                    bool showed = false;
                    float contactRun = 0f, contactHold = 0f;
                    for (int frame = 0; frame < 30; frame++)
                    {
                        int now = Time.frameCount;
                        int seated = (int)t.GetField("_coachTapFrame", flags).GetValue(app);
                        bool on = (bool)t.GetField("_coachTapOn", flags).GetValue(app);
                        if (on && seated == now)
                            t.GetField("_coachTapFrame", flags).SetValue(app, now == 0 ? 1 : now - 1);
                        PlayClock.StepForTest(0.05f);
                        advance.Invoke(app, null);
                        place.Invoke(app, null);
                        bool vis = (bool)t.GetField("_gloveVis", flags).GetValue(app);
                        if (!vis) continue;
                        showed = true;
                        var shown = (Vector2)t.GetField("_gloveShown", flags).GetValue(app);
                        float ang = (float)t.GetField("_gloveShownAng", flags).GetValue(app);
                        bool hold = (bool)t.GetField("_gloveHoldPose", flags).GetValue(app);
                        float dip = hold
                            ? (float)t.GetField("_gloveHoldDip", flags).GetValue(app)
                            : (float)t.GetField("_gloveDip", flags).GetValue(app);
                        bool mirror = (bool)t.GetField("_gloveMirror", flags).GetValue(app);
                        var box = FlockFiveApp.ScreenGloveRect(shown, ang, FlockFiveApp.GloveSize(DeviceFit.Ui(w, h)), dip, mirror);
                        if (box.center.x < minX) minX = box.center.x;
                        if (box.center.x > maxX) maxX = box.center.x;
                        if (!GloveInside(box, w, 59f, h - 34f)) inside = false;
                        if (dip >= bestDip)
                        {
                            bestDip = dip;
                            full = shown;
                        }
                        if (dip >= 0.98f) contactRun += 0.05f;
                        else
                        {
                            if (contactRun > contactHold) contactHold = contactRun;
                            contactRun = 0f;
                        }
                    }
                    if (contactRun > contactHold) contactHold = contactRun;
                    // Positive means the tip is still in the gap, past the chest.
                    float pastEdge = full.x - birds.xMax;
                    bool touch = showed && bestDip > 0.9f && birds.width > 8f
                        && pastEdge <= 8f && pastEdge >= -8f
                        && full.y > birds.yMin && full.y < birds.yMax;
                    float travel = showed ? maxX - minX : 0f;
                    string tag = ((int)size.x).ToString();
                    string detail = "travel " + travel.ToString("0.0") + " dip " + bestDip.ToString("0.00")
                        + " hold " + contactHold.ToString("0.00")
                        + " gap " + pastEdge.ToString("0.0")
                        + " tip " + full + " birds " + birds + " vis " + showed;
                    Check("fresh-garden-poke-moves-" + tag, showed && travel >= 20f, detail);
                    Check("fresh-garden-fingertip-touches-" + tag, touch, detail);
                    Check("fresh-garden-contact-hold-" + tag, showed && contactHold >= 0.30f && contactHold <= 0.45f, detail);
                    Check("fresh-garden-glove-inside-" + tag, showed && inside, detail);
                }
            }
            catch (System.Exception ex)
            {
                var inner = ex.InnerException ?? ex;
                Check("fresh-garden-poke", false, inner.GetType().Name + " " + inner.Message);
            }
            finally
            {
                winOn.SetValue(null, keepOn);
                winW.SetValue(null, keepW);
                winH.SetValue(null, keepH);
                safeTop.SetValue(null, keepTop);
                safeBot.SetValue(null, keepBot);
                if (rt != null)
                {
                    rt.Release();
                    UnityEngine.Object.DestroyImmediate(rt);
                }
                UnityEngine.Object.DestroyImmediate(camGo);
                UnityEngine.Object.DestroyImmediate(branchGo);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static Rect ProjectBirds(Rect world, Camera cam, float screenH)
        {
            var a = cam.WorldToScreenPoint(new Vector3(world.xMin, world.yMax, 0f));
            var b = cam.WorldToScreenPoint(new Vector3(world.xMax, world.yMin, 0f));
            float x0 = Mathf.Min(a.x, b.x), x1 = Mathf.Max(a.x, b.x);
            float y0 = screenH - Mathf.Max(a.y, b.y);
            float y1 = screenH - Mathf.Min(a.y, b.y);
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }

        static void CheckFirstGardenGloves(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("first-garden-gloves");
            var winOn = typeof(PhoneArea).GetField("EditorWindowOn", BindingFlags.NonPublic | BindingFlags.Static);
            var winW = typeof(PhoneArea).GetField("EditorWindowW", BindingFlags.NonPublic | BindingFlags.Static);
            var winH = typeof(PhoneArea).GetField("EditorWindowH", BindingFlags.NonPublic | BindingFlags.Static);
            var safeTop = typeof(PhoneArea).GetField("EditorSafeTop", BindingFlags.NonPublic | BindingFlags.Static);
            var safeBot = typeof(PhoneArea).GetField("EditorSafeBottom", BindingFlags.NonPublic | BindingFlags.Static);
            bool keepOn = (bool)winOn.GetValue(null);
            int keepW = (int)winW.GetValue(null);
            int keepH = (int)winH.GetValue(null);
            float keepTop = (float)safeTop.GetValue(null);
            float keepBot = (float)safeBot.GetValue(null);
            try
            {
                var app = go.AddComponent<FlockFiveApp>();
                var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
                var coach = typeof(FlockFiveApp).GetMethod("CoachTapAt", flags);
                foreach (var size in new[] { new Vector2(820f, 1180f), new Vector2(402f, 874f), new Vector2(412f, 915f), new Vector2(360f, 800f) })
                {
                    winOn.SetValue(null, true);
                    winW.SetValue(null, (int)size.x);
                    winH.SetValue(null, (int)size.y);
                    safeTop.SetValue(null, 59f);
                    safeBot.SetValue(null, 34f);
                    float w = PhoneArea.W, h = PhoneArea.H;
                    string tag = ((int)size.x).ToString();
                    string bad = "";
                    bad += SampleTap(app, flags, coach, "branch", FlockFiveApp.L1BranchRect(0, w, h));
                    bad += SampleTap(app, flags, coach, "bird", new Rect(w * 0.62f, 59f + 28f, 54f, 48f));
                    bad += SampleTap(app, flags, coach, "daily", new Rect(18f, 59f + 86f, 64f, 64f));
                    bad += SampleTap(app, flags, coach, "claim", new Rect(w * 0.5f - 80f, h * 0.62f, 160f, 110f));
                    bad += SampleTap(app, flags, coach, "deal", new Rect(w - 86f, h - 34f - 96f, 72f, 72f));
                    bad += SampleTap(app, flags, coach, "back", new Rect(16f, 59f + 8f, 48f, 48f));
                    bad += SampleTap(app, flags, coach, "poker", new Rect(w - 86f, 59f + 150f, 64f, 64f));
                    bad += SampleTap(app, flags, coach, "watch", new Rect(w * 0.28f, h * 0.58f, 140f, 52f));
                    var play = new Rect(w * 0.5f - 70f, h * 0.72f, 140f, 64f);
                    bad += SampleTap(app, flags, coach, "play", play);
                    Check("first-garden-glove-" + tag, bad.Length == 0, bad);
                }
            }
            catch (System.Exception ex)
            {
                Check("first-garden-glove", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                winOn.SetValue(null, keepOn);
                winW.SetValue(null, keepW);
                winH.SetValue(null, keepH);
                safeTop.SetValue(null, keepTop);
                safeBot.SetValue(null, keepBot);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static string SampleTap(FlockFiveApp app, BindingFlags flags, System.Reflection.MethodInfo coach, string name, Rect target)
        {
            var label = FlockFiveApp.CoachTapLabel(target);
            var t = typeof(FlockFiveApp);
            t.GetField("_coachTapOn", flags).SetValue(app, false);
            t.GetField("_glovePhase", flags).SetValue(app, 0f);
            t.GetField("_gloveVis", flags).SetValue(app, false);
            t.GetField("_coachFade", flags).SetValue(app, 1f);
            Vector2 start = default;
            var path = new System.Text.StringBuilder();
            Vector2 end = default;
            float bestDip = -1f;
            float endAng = 0f;
            bool endMirror = false;
            bool vis = false;
            float span = 0f;
            for (int i = 0; i < 24; i++)
            {
                coach.Invoke(app, new object[] { target, label, 0.06f, null });
                vis = (bool)t.GetField("_gloveVis", flags).GetValue(app);
                var tip = (Vector2)t.GetField("_gloveShown", flags).GetValue(app);
                float dip = (float)t.GetField("_gloveDip", flags).GetValue(app);
                if (i == 0) start = tip;
                path.Append(tip.x.ToString("0")).Append(',').Append(tip.y.ToString("0")).Append(' ');
                span = Mathf.Max(span, (tip - start).magnitude);
                if (dip >= bestDip)
                {
                    bestDip = dip;
                    end = tip;
                    endAng = (float)t.GetField("_gloveShownAng", flags).GetValue(app);
                    endMirror = (bool)t.GetField("_gloveMirror", flags).GetValue(app);
                }
            }
            object[] hud = { default(Rect), default(Rect) };
            t.GetMethod("HudShaken", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, hud);
            var restart = (Rect)hud[0];
            var home = CornerAnchor.TopBarHome(restart, PhoneArea.W, PhoneArea.Safe);
            var pose = FlockFiveApp.CoachTapSide(target, label, PhoneArea.W, PhoneArea.H, PhoneArea.Safe, home, restart, default);
            bool horizontal = Mathf.Abs(Mathf.Abs(endAng) - 90f) < 1.5f;
            bool side = endMirror == pose.FromLeft;
            bool touch = pose.FromLeft
                ? Mathf.Abs(pose.Glove.xMax - target.xMin) <= 1.2f
                : Mathf.Abs(pose.Glove.xMin - target.xMax) <= 1.2f;
            bool covered = GloveCovers(pose.Glove, target);
            bool onEdge = touch && !covered
                && Mathf.Abs(end.x - pose.End.x) < 1.5f
                && Mathf.Abs(end.y - pose.End.y) < 1.5f;
            bool level = Mathf.Abs(start.y - end.y) < 1.5f;
            bool poke = span <= FlockFiveApp.CoachTapPressPx + 0.6f && span > 1f;
            float top = 59f;
            float bot = PhoneArea.H - 34f;
            bool inside = pose.Glove.xMin >= -0.5f && pose.Glove.yMin >= top - 0.5f
                && pose.Glove.xMax <= PhoneArea.W + 0.5f && pose.Glove.yMax <= bot + 0.5f;
            Debug.Log("[coach-tap] " + name + " " + PhoneArea.W
                + " start " + start.x.ToString("0.0") + "," + start.y.ToString("0.0")
                + " path " + path
                + " end " + end.x.ToString("0.0") + "," + end.y.ToString("0.0")
                + " dip " + bestDip.ToString("0.00"));
            if (vis && horizontal && side && onEdge && level && poke && inside && bestDip > 0.9f) return "";
            return name + " vis " + vis + " flat " + horizontal + " side " + side
                + " edge " + onEdge + " level " + level + " poke " + poke + " in " + inside
                + " dip " + bestDip.ToString("0.00") + " span " + span.ToString("0.0")
                + " ang " + endAng.ToString("0") + " end " + end + " glove " + pose.Glove + "; ";
        }

        static void CheckStationaryTaps(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("stationary-taps");
            var winOn = typeof(PhoneArea).GetField("EditorWindowOn", BindingFlags.NonPublic | BindingFlags.Static);
            var winW = typeof(PhoneArea).GetField("EditorWindowW", BindingFlags.NonPublic | BindingFlags.Static);
            var winH = typeof(PhoneArea).GetField("EditorWindowH", BindingFlags.NonPublic | BindingFlags.Static);
            var safeTop = typeof(PhoneArea).GetField("EditorSafeTop", BindingFlags.NonPublic | BindingFlags.Static);
            var safeBot = typeof(PhoneArea).GetField("EditorSafeBottom", BindingFlags.NonPublic | BindingFlags.Static);
            bool keepOn = (bool)winOn.GetValue(null);
            int keepW = (int)winW.GetValue(null);
            int keepH = (int)winH.GetValue(null);
            float keepTop = (float)safeTop.GetValue(null);
            float keepBot = (float)safeBot.GetValue(null);
            try
            {
                var app = go.AddComponent<FlockFiveApp>();
                var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
                var coach = typeof(FlockFiveApp).GetMethod("CoachTapAt", flags);
                foreach (var size in new[] { new Vector2(820f, 1180f), new Vector2(402f, 874f), new Vector2(412f, 915f), new Vector2(360f, 800f) })
                {
                    winOn.SetValue(null, true);
                    winW.SetValue(null, (int)size.x);
                    winH.SetValue(null, (int)size.y);
                    safeTop.SetValue(null, 59f);
                    safeBot.SetValue(null, 34f);
                    float playW = PhoneArea.W;
                    string bad = SampleTap(app, flags, coach, "rail", new Rect(playW - 86f, 59f + 150f, 64f, 64f));
                    Check("stationary-tap-" + (int)size.x, bad.Length == 0, bad);
                }
            }
            catch (System.Exception ex)
            {
                Check("stationary-tap", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                winOn.SetValue(null, keepOn);
                winW.SetValue(null, keepW);
                winH.SetValue(null, keepH);
                safeTop.SetValue(null, keepTop);
                safeBot.SetValue(null, keepBot);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void CheckBirdBeatsHomeGate(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("bird-beats-gate");
            try
            {
                var app = go.AddComponent<FlockFiveApp>();
                var t = typeof(FlockFiveApp);
                var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
                t.GetField("_splash", flags).SetValue(app, true);
                var home = t.GetField("_home", flags);
                home.SetValue(app, System.Enum.ToObject(home.FieldType, 0));
                t.GetField("_adoptLive", flags).SetValue(app, true);
                var step = t.GetField("_adoptStep", flags);
                step.SetValue(app, System.Enum.ToObject(step.FieldType, 2));
                t.GetField("_adoptPick", flags).SetValue(app, 0);
                var shown = (Vector2[])t.GetField("_adoptShown", flags).GetValue(app);
                shown[0] = new Vector2(220f, 280f);
                bool hit = (bool)t.GetMethod("AdoptLookOwnsTap", flags).Invoke(app, new object[] { shown[0] });
                bool opened = (bool)t.GetMethod("TakeAdoptLookTap", flags).Invoke(app, new object[] { shown[0] });
                bool named = (bool)t.GetField("_avatarRename", flags).GetValue(app);
                bool missed = (bool)t.GetMethod("TakeAdoptLookTap", flags).Invoke(app, new object[] { new Vector2(8f, 8f) });
                Check("bird-tap-beats-home-gate", hit && opened && named && !missed,
                    "owns " + hit + " opened " + opened + " rename " + named + " miss " + missed);
            }
            catch (System.Exception ex)
            {
                Check("bird-tap-beats-home-gate", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void CheckNotNowDuringOpen(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("not-now-open");
            try
            {
                var app = go.AddComponent<FlockFiveApp>();
                var t = typeof(FlockFiveApp);
                var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
                var no = new Rect(80f, 420f, 120f, 48f);
                t.GetField("_dailyAskOpen", flags).SetValue(app, true);
                t.GetField("_dailyAskAt", flags).SetValue(app, AdPause.Unscaled);
                bool took = (bool)t.GetMethod("TakeDailyAskNotNow", flags).Invoke(app, new object[] { no, no.center });
                bool closed = !(bool)t.GetField("_dailyAskOpen", flags).GetValue(app);
                t.GetField("_dailyAskOpen", flags).SetValue(app, true);
                t.GetField("_dailyAskAt", flags).SetValue(app, AdPause.Unscaled);
                bool missed = (bool)t.GetMethod("TakeDailyAskNotNow", flags).Invoke(app, new object[] { no, new Vector2(4f, 4f) });
                bool stayed = (bool)t.GetField("_dailyAskOpen", flags).GetValue(app);
                Check("not-now-takes-first-tap", took && closed && !missed && stayed,
                    "took " + took + " closed " + closed + " miss " + missed + " stayed " + stayed);
            }
            catch (System.Exception ex)
            {
                Check("not-now-takes-first-tap", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void CheckFivesClose(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("fives-close");
            try
            {
                var app = go.AddComponent<FlockFiveApp>();
                var t = typeof(FlockFiveApp);
                var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
                var geo = FlockFiveApp.LayoutFivesBoard(PhoneArea.W, PhoneArea.H, PhoneArea.Safe, DeviceFit.Ui());
                var close = FlockFiveApp.FivesCloseRect(geo.Panel, DeviceFit.Ui());
                t.GetField("_fivesOpen", flags).SetValue(app, true);
                bool shut = (bool)t.GetMethod("TakeFivesClose", flags).Invoke(app, new object[] { close.center });
                bool closed = !(bool)t.GetField("_fivesOpen", flags).GetValue(app);
                t.GetField("_fivesOpen", flags).SetValue(app, true);
                bool missed = (bool)t.GetMethod("TakeFivesClose", flags).Invoke(app, new object[] { new Vector2(4f, 4f) });
                bool still = (bool)t.GetField("_fivesOpen", flags).GetValue(app);
                Check("fives-close-x", close.width > 8f && shut && closed && !missed && still,
                    "rect " + close + " shut " + shut + " closed " + closed + " miss " + missed);
            }
            catch (System.Exception ex)
            {
                Check("fives-close-x", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void CheckFivesCloseConsumesPointer(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("fives-pointer");
            var prev = Event.current;
            var inject = FlockFiveApp.EditorPointerInject;
            try
            {
                Event.current = null;
                var app = go.AddComponent<FlockFiveApp>();
                var t = typeof(FlockFiveApp);
                var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
                float s = DeviceFit.Ui();
                var geo = FlockFiveApp.LayoutFivesBoard(PhoneArea.W, PhoneArea.H, PhoneArea.Safe, s);
                var close = FlockFiveApp.FivesCloseRect(geo.Panel, s);
                t.GetField("_fivesOpen", flags).SetValue(app, true);
                t.GetField("_fivesAnim", flags).SetValue(app, 1f);
                t.GetField("_pokerPayOpen", flags).SetValue(app, true);
                t.GetField("_pokerPayAnim", flags).SetValue(app, 1f);
                t.GetField("_pokerHover", flags).SetValue(app, 7);
                // Editor batch has no IMGUI event. A real tap here is EditorPointerInject,
                // Play space (x without the pillar, y up). TakeGuiPointer flips it back.
                InjectGuiRelease(close.center, 4);
                t.GetMethod("TakeGuiPointer", flags).Invoke(app, null);
                bool closed = !(bool)t.GetField("_fivesOpen", flags).GetValue(app);
                bool eaten = !FlockFiveApp.EditorPointerInject.On;
                string who = (string)t.GetField("_tapWho", flags).GetValue(app) ?? "";
                t.GetMethod("TickPokerPayTable", flags).Invoke(app, new object[] { 80f, s });
                bool payStayed = (bool)t.GetField("_pokerPayOpen", flags).GetValue(app);
                t.GetMethod("HitPokerCards", flags).Invoke(app, new object[] { true, new Rect(0f, 0f, 40f, 60f), 0f, 0f, 40f, 60f, 4f });
                int hover = (int)t.GetField("_pokerHover", flags).GetValue(app);
                Check("fives-x-consumes-pointer",
                    close.width > 8f && closed && eaten && who == "fives-close" && payStayed && hover == 7,
                    "closed " + closed + " eaten " + eaten + " who " + who + " payStayed " + payStayed + " hover " + hover);
            }
            catch (System.Exception ex)
            {
                Check("fives-x-consumes-pointer", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                Event.current = prev;
                FlockFiveApp.EditorPointerInject = inject;
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void CheckBirdTapConsumesPointer(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("bird-pointer");
            var prev = Event.current;
            var inject = FlockFiveApp.EditorPointerInject;
            try
            {
                Event.current = null;
                var app = go.AddComponent<FlockFiveApp>();
                var t = typeof(FlockFiveApp);
                var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
                t.GetField("_splash", flags).SetValue(app, true);
                var home = t.GetField("_home", flags);
                home.SetValue(app, System.Enum.ToObject(home.FieldType, 0));
                t.GetField("_adoptLive", flags).SetValue(app, true);
                var step = t.GetField("_adoptStep", flags);
                step.SetValue(app, System.Enum.ToObject(step.FieldType, 2));
                t.GetField("_adoptPick", flags).SetValue(app, 0);
                var level = SplashPress.LevelRect(PhoneArea.W, PhoneArea.H, PhoneArea.Safe);
                var at = level.width > 2f ? level.center : new Vector2(220f, 280f);
                var shown = (Vector2[])t.GetField("_adoptShown", flags).GetValue(app);
                shown[0] = at;
                InjectGuiRelease(at, 5);
                t.GetMethod("TakeGuiPointer", flags).Invoke(app, null);
                bool named = (bool)t.GetField("_avatarRename", flags).GetValue(app);
                bool splash = (bool)t.GetField("_splash", flags).GetValue(app);
                string who = (string)t.GetField("_tapWho", flags).GetValue(app) ?? "";
                bool eaten = !FlockFiveApp.EditorPointerInject.On;
                var entered = PlayPointer.Enter(new Vector2(at.x + PhoneArea.OffX, at.y));
                bool onBird = Mathf.Abs(entered.x - at.x) < 0.6f && Mathf.Abs(entered.y - at.y) < 0.6f;
                Check("bird-tap-consumes-pointer",
                    named && splash && who == "adopt-look" && eaten && onBird,
                    "named " + named + " splash " + splash + " who " + who + " eaten " + eaten + " onBird " + onBird);
            }
            catch (System.Exception ex)
            {
                Check("bird-tap-consumes-pointer", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                Event.current = prev;
                FlockFiveApp.EditorPointerInject = inject;
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void CheckNotNowRelease(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("not-now-pointer");
            var prev = Event.current;
            var inject = FlockFiveApp.EditorPointerInject;
            try
            {
                Event.current = null;
                var app = go.AddComponent<FlockFiveApp>();
                var t = typeof(FlockFiveApp);
                var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
                var no = new Rect(80f, 420f, 120f, 48f);
                t.GetField("_dailyAskOpen", flags).SetValue(app, true);
                t.GetField("_dailyAskAt", flags).SetValue(app, AdPause.Unscaled);
                t.GetField("_dailyAskNoHit", flags).SetValue(app, no);
                t.GetField("_dailyAskNoHitOn", flags).SetValue(app, true);
                InjectGuiPress(no.center, 6);
                t.GetMethod("TakeGuiPointer", flags).Invoke(app, null);
                bool still = (bool)t.GetField("_dailyAskOpen", flags).GetValue(app);
                InjectGuiRelease(no.center, 6);
                t.GetMethod("TakeGuiPointer", flags).Invoke(app, null);
                bool closed = !(bool)t.GetField("_dailyAskOpen", flags).GetValue(app);
                bool eaten = !FlockFiveApp.EditorPointerInject.On;
                string who = (string)t.GetField("_tapWho", flags).GetValue(app) ?? "";
                Check("not-now-release-consumes-pointer", still && closed && eaten && who == "not-now",
                    "pressOpen " + still + " closed " + closed + " eaten " + eaten + " who " + who);
            }
            catch (System.Exception ex)
            {
                Check("not-now-release-consumes-pointer", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                Event.current = prev;
                FlockFiveApp.EditorPointerInject = inject;
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void CheckWatchConsumesPointer(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("watch-pointer");
            var prev = Event.current;
            var inject = FlockFiveApp.EditorPointerInject;
            try
            {
                Event.current = null;
                var app = go.AddComponent<FlockFiveApp>();
                var t = typeof(FlockFiveApp);
                var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
                var face = t.GetNestedType("GiftFace", BindingFlags.NonPublic);
                t.GetField("_splash", flags).SetValue(app, true);
                t.GetField("_adHand", flags).SetValue(app, true);
                t.GetField("_freezeOffer", flags).SetValue(app, false);
                t.GetField("_gift", flags).SetValue(app, System.Enum.ToObject(face, 1));
                t.GetField("_watchGiftLive", flags).SetValue(app, false);
                t.GetField("_watchStarts", flags).SetValue(app, 0);
                float s = DeviceFit.Ui();
                object[] placed = { s, default(Rect), default(Rect) };
                t.GetMethod("GiftCardPlaced", flags).Invoke(app, placed);
                var watch = (Rect)t.GetMethod("FlowerHit", BindingFlags.NonPublic | BindingFlags.Static)
                    .Invoke(null, new object[] { (Rect)placed[2], s });
                InjectGuiRelease(watch.center, 7);
                t.GetMethod("TakeGuiPointer", flags).Invoke(app, null);
                int starts = (int)t.GetField("_watchStarts", flags).GetValue(app);
                bool eaten = !FlockFiveApp.EditorPointerInject.On;
                string who = (string)t.GetField("_tapWho", flags).GetValue(app) ?? "";
                InjectGuiRelease(watch.center, 7);
                t.GetMethod("TakeGuiPointer", flags).Invoke(app, null);
                int again = (int)t.GetField("_watchStarts", flags).GetValue(app);
                Check("watch-consumes-pointer", starts == 1 && again == 1 && eaten && who == "watch",
                    "starts " + starts + "/" + again + " eaten " + eaten + " who " + who);
            }
            catch (System.Exception ex)
            {
                Check("watch-consumes-pointer", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                Event.current = prev;
                FlockFiveApp.EditorPointerInject = inject;
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void CheckGiftXConsumesPointer(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("gift-x-pointer");
            var prev = Event.current;
            var inject = FlockFiveApp.EditorPointerInject;
            int streak = Purse.Streak;
            var resume = typeof(FlockFiveApp).GetField("_resumeInputUntil", BindingFlags.NonPublic | BindingFlags.Static);
            float keepResume = (float)resume.GetValue(null);
            try
            {
                Event.current = null;
                Purse.SetStreak(0);
                resume.SetValue(null, float.NegativeInfinity);
                var app = go.AddComponent<FlockFiveApp>();
                var t = typeof(FlockFiveApp);
                var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
                var face = t.GetNestedType("GiftFace", BindingFlags.NonPublic);
                t.GetField("_splash", flags).SetValue(app, false);
                t.GetField("_board", flags).SetValue(app, LevelData.Open(0));
                t.GetField("_adHand", flags).SetValue(app, true);
                t.GetField("_freezeOffer", flags).SetValue(app, false);
                t.GetField("_gift", flags).SetValue(app, System.Enum.ToObject(face, 1));
                t.GetField("_busy", flags).SetValue(app, false);
                t.GetField("_restarting", flags).SetValue(app, false);
                var x = (Rect)t.GetMethod("GiftCloseHit", BindingFlags.NonPublic | BindingFlags.Static)
                    .Invoke(null, null);
                InjectGuiRelease(x.center, 8);
                t.GetMethod("TakeGuiPointer", flags).Invoke(app, null);
                string gift = t.GetField("_gift", flags).GetValue(app).ToString();
                bool eaten = !FlockFiveApp.EditorPointerInject.On;
                string who = (string)t.GetField("_tapWho", flags).GetValue(app) ?? "";
                bool restart = t.GetField("EditorSnapRound", flags).GetValue(app) != null
                    || (bool)t.GetField("_restarting", flags).GetValue(app);
                Check("gift-x-consumes-pointer", gift == "None" && eaten && who == "gift-x" && !restart,
                    "gift " + gift + " eaten " + eaten + " who " + who + " restart " + restart);
            }
            catch (System.Exception ex)
            {
                Check("gift-x-consumes-pointer", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                Event.current = prev;
                FlockFiveApp.EditorPointerInject = inject;
                resume.SetValue(null, keepResume);
                Purse.SetStreak(streak);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void CheckGiftPressSkipsRestart(System.Action<string, bool, string> Check)
        {
            EachPhoneWidth(Check, "gift-x-press-skips-restart", (app, t, flags) =>
            {
                var face = t.GetNestedType("GiftFace", BindingFlags.NonPublic);
                t.GetField("_splash", flags).SetValue(app, false);
                t.GetField("_adHand", flags).SetValue(app, true);
                t.GetField("_freezeOffer", flags).SetValue(app, false);
                t.GetField("_gift", flags).SetValue(app, System.Enum.ToObject(face, 1));
                var x = (Rect)t.GetMethod("GiftCloseHit", BindingFlags.NonPublic | BindingFlags.Static)
                    .Invoke(null, null);
                bool step = PressThenRelease(app, t, flags, x.center);
                string gift = t.GetField("_gift", flags).GetValue(app).ToString();
                bool home = (bool)t.GetField("_splash", flags).GetValue(app);
                bool restart = RestartLive(t, flags, app);
                bool inject = FlockFiveApp.EditorPointerInject.On;
                string who = (string)t.GetField("_tapWho", flags).GetValue(app) ?? "";
                if (step && gift == "None" && !home && !restart && !inject) return null;
                return "step " + step + " gift " + gift + " splash " + home
                    + " restart " + restart + " inject " + inject + " who " + who
                    + " off " + PhoneArea.OffX;
            });
        }

        static void CheckWatchPressStartsOnRelease(System.Action<string, bool, string> Check)
        {
            EachPhoneWidth(Check, "watch-press-starts-on-release", (app, t, flags) =>
            {
                var face = t.GetNestedType("GiftFace", BindingFlags.NonPublic);
                t.GetField("_splash", flags).SetValue(app, false);
                t.GetField("_adHand", flags).SetValue(app, true);
                t.GetField("_freezeOffer", flags).SetValue(app, false);
                t.GetField("_gift", flags).SetValue(app, System.Enum.ToObject(face, 1));
                t.GetField("_watchStarts", flags).SetValue(app, 0);
                t.GetField("_watchGiftLive", flags).SetValue(app, false);
                float s = DeviceFit.Ui();
                object[] placed = { s, default(Rect), default(Rect) };
                t.GetMethod("GiftCardPlaced", flags).Invoke(app, placed);
                var watch = (Rect)t.GetMethod("FlowerHit", BindingFlags.NonPublic | BindingFlags.Static)
                    .Invoke(null, new object[] { (Rect)placed[2], s });
                t.GetField("EditorWatchRequests", flags).SetValue(app, 0);
                var window = new Vector2(watch.center.x + PhoneArea.OffX, watch.center.y);
                FlockFiveApp.EditorPointerInject = new FlockFiveApp.EditorPointer
                {
                    On = true, Down = true, Up = false, Id = 9, Window = window
                };
                t.GetMethod("UpdateBody", flags).Invoke(app, null);
                t.GetMethod("TakeGuiPointer", flags).Invoke(app, null);
                int afterPress = (int)t.GetField("_watchStarts", flags).GetValue(app);
                int seamPress = (int)t.GetField("EditorWatchRequests", flags).GetValue(app);
                FlockFiveApp.EditorPointerInject = new FlockFiveApp.EditorPointer
                {
                    On = true, Down = false, Up = true, Id = 9, Window = window
                };
                t.GetMethod("UpdateBody", flags).Invoke(app, null);
                t.GetMethod("TakeGuiPointer", flags).Invoke(app, null);
                int afterRelease = (int)t.GetField("_watchStarts", flags).GetValue(app);
                int seamRelease = (int)t.GetField("EditorWatchRequests", flags).GetValue(app);
                bool home = (bool)t.GetField("_splash", flags).GetValue(app);
                bool restart = RestartLive(t, flags, app);
                var entered = PlayPointer.Enter(window);
                bool onWatch = Mathf.Abs(entered.x - watch.center.x) < 0.6f && Mathf.Abs(entered.y - watch.center.y) < 0.6f;
                bool once = afterPress == 0 && afterRelease == 1 && seamPress == afterPress && seamRelease == afterRelease;
                if (once && !home && !restart && onWatch) return null;
                return "press " + afterPress + " release " + afterRelease
                    + " seam " + seamPress + "/" + seamRelease
                    + " splash " + home + " restart " + restart + " entered " + entered;
            });
        }

        static void CheckFivesPressSkipsRestart(System.Action<string, bool, string> Check)
        {
            // The collection stays open only on the poker splash. The release goes
            // through PlayPointer.Enter (window point = local + OffX) and closes the board.
            EachPhoneWidth(Check, "fives-press-skips-restart", (app, t, flags) =>
            {
                var home = t.GetField("_home", flags);
                home.SetValue(app, System.Enum.ToObject(home.FieldType, 2));
                t.GetField("_splash", flags).SetValue(app, true);
                t.GetField("_fivesOpen", flags).SetValue(app, true);
                t.GetField("_pokerPayOpen", flags).SetValue(app, true);
                t.GetField("_pokerPayAnim", flags).SetValue(app, 1f);
                t.GetField("_pokerHover", flags).SetValue(app, 7);
                float s = DeviceFit.Ui();
                var geo = FlockFiveApp.LayoutFivesBoard(PhoneArea.W, PhoneArea.H, PhoneArea.Safe, s);
                var at = FlockFiveApp.FivesCloseRect(geo.Panel, s).center;
                var window = new Vector2(at.x + PhoneArea.OffX, at.y);
                FlockFiveApp.EditorPointerInject = new FlockFiveApp.EditorPointer
                {
                    On = true, Down = true, Up = false, Id = 9, Window = window
                };
                t.GetMethod("UpdateBody", flags).Invoke(app, null);
                bool openAfterPress = (bool)t.GetField("_fivesOpen", flags).GetValue(app);
                t.GetMethod("TakeGuiPointer", flags).Invoke(app, null);
                openAfterPress = openAfterPress && (bool)t.GetField("_fivesOpen", flags).GetValue(app);
                bool pressEaten = !FlockFiveApp.EditorPointerInject.On;
                FlockFiveApp.EditorPointerInject = new FlockFiveApp.EditorPointer
                {
                    On = true, Down = false, Up = true, Id = 9, Window = window
                };
                t.GetMethod("UpdateBody", flags).Invoke(app, null);
                openAfterPress = openAfterPress && (bool)t.GetField("_fivesOpen", flags).GetValue(app);
                t.GetMethod("TakeGuiPointer", flags).Invoke(app, null);
                bool closed = !(bool)t.GetField("_fivesOpen", flags).GetValue(app);
                bool eaten = !FlockFiveApp.EditorPointerInject.On;
                t.GetMethod("TickPokerPayTable", flags).Invoke(app, new object[] { 80f, s });
                bool payStayed = (bool)t.GetField("_pokerPayOpen", flags).GetValue(app);
                t.GetMethod("HitPokerCards", flags).Invoke(app, new object[] { true, new Rect(0f, 0f, 40f, 60f), 0f, 0f, 40f, 60f, 4f });
                int hover = (int)t.GetField("_pokerHover", flags).GetValue(app);
                var entered = PlayPointer.Enter(window);
                bool onX = Mathf.Abs(entered.x - at.x) < 0.6f;
                if (openAfterPress && closed && pressEaten && eaten && !RestartLive(t, flags, app) && payStayed && hover == 7 && onX)
                    return null;
                return "open " + openAfterPress + " closed " + closed + " pressEaten " + pressEaten
                    + " eaten " + eaten + " restart " + RestartLive(t, flags, app)
                    + " pay " + payStayed + " hover " + hover + " off " + PhoneArea.OffX;
            });
        }

        static void CheckWindowGiftPress(System.Action<string, bool, string> Check)
        {
            EachPhoneWidth(Check, "window-gift-x", (app, t, flags) =>
            {
                var face = t.GetNestedType("GiftFace", BindingFlags.NonPublic);
                t.GetField("_splash", flags).SetValue(app, false);
                t.GetField("_adHand", flags).SetValue(app, true);
                t.GetField("_freezeOffer", flags).SetValue(app, false);
                t.GetField("_gift", flags).SetValue(app, System.Enum.ToObject(face, 1));
                var x = (Rect)t.GetMethod("GiftCloseHit", BindingFlags.NonPublic | BindingFlags.Static)
                    .Invoke(null, null);
                var window = new Vector2(x.center.x + PhoneArea.OffX, x.center.y);
                FlockFiveApp.EditorPointerInject = new FlockFiveApp.EditorPointer
                {
                    On = true, Down = true, Up = false, Id = 21, Window = window
                };
                t.GetMethod("UpdateBody", flags).Invoke(app, null);
                bool pressRestart = RestartLive(t, flags, app);
                bool pressHome = (bool)t.GetField("_splash", flags).GetValue(app);
                FlockFiveApp.EditorPointerInject = new FlockFiveApp.EditorPointer
                {
                    On = true, Down = false, Up = true, Id = 21, Window = window
                };
                t.GetMethod("UpdateBody", flags).Invoke(app, null);
                string gift = t.GetField("_gift", flags).GetValue(app).ToString();
                bool restart = RestartLive(t, flags, app);
                bool home = (bool)t.GetField("_splash", flags).GetValue(app);
                if (!pressRestart && !pressHome && !restart && !home && gift == "None") return null;
                return "pressRestart " + pressRestart + " pressHome " + pressHome
                    + " restart " + restart + " home " + home + " gift " + gift
                    + " window " + window + " off " + PhoneArea.OffX;
            }, new[] { 820 });
        }

        static void CheckRuleReentrant(System.Action<string, bool, string> Check)
        {
            EachPhoneWidth(Check, "rule-press-once", (app, t, flags) =>
            {
                t.GetField("_splash", flags).SetValue(app, false);
                t.GetField("_ruleTip", flags).SetValue(app, RuleTip.Feeder);
                t.GetField("EditorRulePressEntered", flags).SetValue(app, 0);
                t.GetField("EditorSnapRound", flags).SetValue(app, null);
                var home = (Rect)t.GetMethod("HomeHudRect", flags).Invoke(app, null);
                var window = new Vector2(home.center.x + PhoneArea.OffX, home.center.y);
                var local = PlayPointer.Enter(window);
                var bottom = new Vector2(local.x, PhoneArea.H - local.y);
                string fault = null;
                try { t.GetMethod("TakeRuleTipPress", flags).Invoke(app, new object[] { bottom }); }
                catch (System.Reflection.TargetInvocationException ex)
                {
                    fault = (ex.InnerException ?? ex).GetType().Name;
                }
                int presses = (int)t.GetField("EditorRulePressEntered", flags).GetValue(app);
                bool splash = (bool)t.GetField("_splash", flags).GetValue(app);
                if (fault == null && presses == 1 && splash) return null;
                return "fault " + (fault ?? "none") + " presses " + presses + " splash " + splash;
            }, new[] { 820, 402 });
        }

        static void CheckGiftStampBeforeGui(System.Action<string, bool, string> Check)
        {
            EachPhoneWidth(Check, "gift-stamp-before-gui", (app, t, flags) =>
            {
                var resume = t.GetField("_resumeInputUntil", BindingFlags.NonPublic | BindingFlags.Static);
                var face = t.GetNestedType("GiftFace", BindingFlags.NonPublic);
                var edgeNow = t.GetMethod("EdgeIdNow", BindingFlags.NonPublic | BindingFlags.Static);
                object[] hud = { default(Rect), default(Rect) };
                t.GetMethod("HudShaken", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, hud);
                var restart = (Rect)hud[0];
                float s = DeviceFit.Ui();
                float top = (float)t.GetMethod("TopHud", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(float) }, null)
                    .Invoke(null, new object[] { 0f });
                var xCorner = (Rect)t.GetMethod("CornerCloseRect", BindingFlags.NonPublic | BindingFlags.Static)
                    .Invoke(null, new object[] { s, top });
                var xPad = (Rect)t.GetMethod("CloseTapRect", BindingFlags.NonPublic | BindingFlags.Static)
                    .Invoke(null, new object[] { xCorner });
                var xClaim = (Rect)t.GetMethod("GiftCloseHit", BindingFlags.NonPublic | BindingFlags.Static)
                    .Invoke(null, null);
                // Same search as before. It does not pick a new pixel: if the
                // padded corner covers Restart, the aim stays on the center.
                Vector2 aim = restart.center;
                Vector2[] candidates =
                {
                    new Vector2(restart.xMin + 4f, restart.center.y),
                    new Vector2(restart.center.x, restart.yMax - 4f),
                    restart.center
                };
                for (int i = 0; i < candidates.Length; i++)
                {
                    if (restart.Contains(candidates[i]) && !xPad.Contains(candidates[i]))
                    {
                        aim = candidates[i];
                        break;
                    }
                }
                var window = new Vector2(aim.x + PhoneArea.OffX, aim.y);
                string Box(Rect r)
                {
                    return r.x.ToString("0.0") + "," + r.y.ToString("0.0")
                        + " " + r.width.ToString("0.0") + "x" + r.height.ToString("0.0");
                }
                string One(bool freeze, int heldId, int freshId)
                {
                    t.GetField("_noteFrame", flags).SetValue(app, -1);
                    t.GetField("_noteCount", flags).SetValue(app, 0);
                    t.GetField("_splash", flags).SetValue(app, false);
                    t.GetField("_adHand", flags).SetValue(app, true);
                    t.GetField("_freezeOffer", flags).SetValue(app, freeze);
                    t.GetField("_gift", flags).SetValue(app, System.Enum.ToObject(face, 1));
                    t.GetField("_restarting", flags).SetValue(app, false);
                    t.GetField("EditorSnapRound", flags).SetValue(app, null);
                    t.GetField("EditorEdgeStamps", flags).SetValue(app, 0);
                    t.GetField("EditorActRestartEntered", flags).SetValue(app, 0);
                    t.GetField("EditorActRestartCommitted", flags).SetValue(app, 0);
                    t.GetField("EditorActRestartBlock", flags).SetValue(app, "");
                    bool held = false, released = false, freshLive = false;
                    string steps = "";
                    string guardBeforeFirst = "";
                    string guardAfterFirst = "";
                    string guardAfterAct = "";
                    void Mark(string step, int latch)
                    {
                        bool live = RestartLive(t, flags, app);
                        if (latch == 1) held = live;
                        else if (latch == 2) released = live;
                        else if (latch == 3) freshLive = live;
                        string who = (string)t.GetField("_tapWho", flags).GetValue(app) ?? "none";
                        int actEntered = (int)t.GetField("EditorActRestartEntered", flags).GetValue(app);
                        int committed = (int)t.GetField("EditorActRestartCommitted", flags).GetValue(app);
                        string block = (string)t.GetField("EditorActRestartBlock", flags).GetValue(app) ?? "none";
                        float until = (float)resume.GetValue(null);
                        float now = AdPause.Unscaled;
                        int edge = int.MinValue;
                        if (FlockFiveApp.EditorPointerInject.On)
                        {
                            bool rel = FlockFiveApp.EditorPointerInject.Up && !FlockFiveApp.EditorPointerInject.Down;
                            edge = (int)edgeNow.Invoke(null, new object[] { rel });
                        }
                        string stop = (string)t.GetField("EditorTapStop", flags).GetValue(app) ?? "";
                        string note = (string)t.GetField("EditorNoteResult", flags).GetValue(app) ?? "";
                        string cover = (string)t.GetField("EditorRestartCover", flags).GetValue(app) ?? "";
                        var raw = FlockFiveApp.EditorPointerInject.Window;
                        var enterPt = PlayPointer.Enter(raw);
                        int hudNow = (int)t.GetMethod("HudControl", flags, null, new[] { typeof(Vector2) }, null)
                            .Invoke(app, new object[] { enterPt });
                        steps += step
                            + " raw " + raw.x.ToString("0.00") + "," + raw.y.ToString("0.00")
                            + " enter " + enterPt.x.ToString("0.00") + "," + enterPt.y.ToString("0.00")
                            + " id " + FlockFiveApp.EditorPointerInject.Id
                            + " held " + held + " released " + released + " fresh " + freshLive
                            + " live " + live + " who " + who + " edge " + edge
                            + " resume " + (now < until ? "open" : "closed")
                            + " elapsed " + (now - until).ToString("0.00")
                            + " act " + actEntered + " commit " + committed + " " + block
                            + " note " + note
                            + " stop " + stop
                            + " cover " + cover
                            + " hud " + hudNow
                            + " | ";
                    }
                    resume.SetValue(null, AdPause.Unscaled + 5f);
                    FlockFiveApp.EditorPointerInject = new FlockFiveApp.EditorPointer
                    {
                        On = true, Down = true, Up = false, Id = heldId, Window = window
                    };
                    Mark("down", 0);
                    if (!freeze) guardBeforeFirst = LessonGuardDiag(app, t);
                    t.GetMethod("UpdateBody", flags).Invoke(app, null);
                    if (!freeze) guardAfterFirst = LessonGuardDiag(app, t);
                    int afterUpdate = (int)t.GetField("EditorEdgeStamps", flags).GetValue(app);
                    Mark("update", 1);
                    t.GetMethod("TakeGuiPointer", flags).Invoke(app, null);
                    int afterGui = (int)t.GetField("EditorEdgeStamps", flags).GetValue(app);
                    Mark("gui", 1);
                    resume.SetValue(null, AdPause.Unscaled - 5f);
                    FlockFiveApp.EditorPointerInject = new FlockFiveApp.EditorPointer
                    {
                        On = true, Down = false, Up = true, Id = heldId, Window = window
                    };
                    t.GetMethod("UpdateBody", flags).Invoke(app, null);
                    t.GetMethod("TakeGuiPointer", flags).Invoke(app, null);
                    int afterUp = (int)t.GetField("EditorEdgeStamps", flags).GetValue(app);
                    Mark("up", 2);
                    t.GetField("EditorSnapRound", flags).SetValue(app, null);
                    t.GetField("_restarting", flags).SetValue(app, false);
                    var fresh = new Vector2(window.x + 1f, window.y);
                    var play = PlayPointer.Enter(fresh);
                    FlockFiveApp.EditorPointerInject = new FlockFiveApp.EditorPointer
                    {
                        On = true, Down = true, Up = false, Id = freshId, Window = fresh
                    };
                    t.GetMethod("UpdateBody", flags).Invoke(app, null);
                    if (!freeze) guardAfterAct = LessonGuardDiag(app, t);
                    Mark("freshDown", 3);
                    FlockFiveApp.EditorPointerInject = new FlockFiveApp.EditorPointer
                    {
                        On = true, Down = false, Up = true, Id = freshId, Window = fresh
                    };
                    t.GetMethod("UpdateBody", flags).Invoke(app, null);
                    Mark("freshUp", 3);
                    bool again = freshLive;
                    // Freeze keeps the card over Restart, so a later press still
                    // does not restart. A gift card leaves Restart live once the
                    // window has closed.
                    bool resumeOk = freeze ? !again : again;
                    bool overlap = xClaim.Overlaps(restart);
                    string detail = (freeze ? "freeze " : "gift ")
                        + "update " + afterUpdate + " gui " + afterGui
                        + " held " + held + " up " + afterUp + " released " + released
                        + " fresh " + again
                        + " win " + fresh + " play " + play
                        + " restart " + Box(restart)
                        + " corner " + Box(xCorner)
                        + " pad " + Box(xPad)
                        + " claim " + Box(xClaim)
                        + " inRestart " + restart.Contains(play)
                        + " inClaim " + xClaim.Contains(play)
                        + " overlap " + overlap
                        + " beforeFirst " + guardBeforeFirst
                        + " afterFirst " + guardAfterFirst
                        + " afterAct " + guardAfterAct
                        + " | " + steps;
                    Debug.Log("[inspector-262] gift-stamp-steps " + PhoneArea.W + " " + detail);
                    if (afterUpdate == 1 && afterGui == 1 && !held && afterUp == 2 && !released && resumeOk && !overlap)
                        return null;
                    return detail;
                }
                string gift = One(false, 30, 31);
                if (gift != null) return gift;
                return One(true, 40, 41);
            }, new[] { 820, 402, 412, 360 });
        }

        // LessonGuardUp reads this app's until and AdPause's clock. Printed, never changed.
        static string LessonGuardDiag(FlockFiveApp app, System.Type t)
        {
            const BindingFlags stat = BindingFlags.NonPublic | BindingFlags.Static;
            const BindingFlags inst = BindingFlags.NonPublic | BindingFlags.Instance;
            float until = (float)t.GetField("_lessonGuardUntil", inst).GetValue(app);
            bool guardUp = (bool)t.GetMethod("LessonGuardUp", inst).Invoke(app, null);
            bool ateNow = (bool)t.GetMethod("CoachAteThisFrame", inst).Invoke(app, null);
            var ad = typeof(AdPause);
            bool adOn = (bool)ad.GetField("_on", stat).GetValue(null);
            float adTotal = (float)ad.GetField("_total", stat).GetValue(null);
            float adStarted = (float)ad.GetField("_startedAt", stat).GetValue(null);
            return "guard " + guardUp
                + " ate " + ateNow
                + " until " + until.ToString("0.00")
                + " unscaled " + AdPause.Unscaled.ToString("0.00")
                + " adOn " + adOn
                + " adTotal " + adTotal.ToString("0.00")
                + " adStart " + adStarted.ToString("0.00");
        }

        static string PressGiftRestart(FlockFiveApp app, System.Type t, BindingFlags flags, bool expectRestart)
        {
            var face = t.GetNestedType("GiftFace", BindingFlags.NonPublic);
            var resume = t.GetField("_resumeInputUntil", BindingFlags.NonPublic | BindingFlags.Static);
            t.GetField("_splash", flags).SetValue(app, false);
            t.GetField("_adHand", flags).SetValue(app, true);
            t.GetField("_freezeOffer", flags).SetValue(app, false);
            t.GetField("_gift", flags).SetValue(app, System.Enum.ToObject(face, 1));
            t.GetField("_restarting", flags).SetValue(app, false);
            t.GetField("EditorSnapRound", flags).SetValue(app, null);
            t.GetField("EditorActRestartEntered", flags).SetValue(app, 0);
            t.GetField("EditorActRestartCommitted", flags).SetValue(app, 0);
            resume.SetValue(null, AdPause.Unscaled - 5f);
            object[] hud = { default(Rect), default(Rect) };
            t.GetMethod("HudShaken", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, hud);
            var restart = (Rect)hud[0];
            var window = new Vector2(restart.center.x + PhoneArea.OffX, restart.center.y);
            void Down(int id)
            {
                FlockFiveApp.EditorPointerInject = new FlockFiveApp.EditorPointer
                {
                    On = true, Down = true, Up = false, Id = id, Window = window
                };
                t.GetMethod("UpdateBody", flags).Invoke(app, null);
            }
            if (expectRestart)
            {
                string guardBeforeFirst = LessonGuardDiag(app, t);
                resume.SetValue(null, AdPause.Unscaled + 5f);
                Down(44);
                string guardAfterFirst = LessonGuardDiag(app, t);
                int held = (int)t.GetField("EditorActRestartCommitted", flags).GetValue(app);
                resume.SetValue(null, AdPause.Unscaled - 5f);
                Down(45);
                t.GetMethod("TakeGuiPointer", flags).Invoke(app, null);
                string guardAfterAct = LessonGuardDiag(app, t);
                int committed = (int)t.GetField("EditorActRestartCommitted", flags).GetValue(app);
                bool live = RestartLive(t, flags, app);
                Debug.Log("[inspector-262] gift-restart-guard " + PhoneArea.W
                    + " beforeFirst " + guardBeforeFirst
                    + " afterFirst " + guardAfterFirst
                    + " afterAct " + guardAfterAct);
                if (held == 0 && committed == 1 && live) return null;
                string restartCover = (string)t.GetField("EditorRestartCover", flags).GetValue(app) ?? "";
                return "held " + held + " commit " + committed + " live " + live + " cover " + restartCover
                    + " beforeFirst " + guardBeforeFirst
                    + " afterFirst " + guardAfterFirst
                    + " afterAct " + guardAfterAct;
            }
            Down(46);
            t.GetMethod("TakeGuiPointer", flags).Invoke(app, null);
            int covered = (int)t.GetField("EditorActRestartCommitted", flags).GetValue(app);
            bool restarted = RestartLive(t, flags, app);
            if (covered == 0 && !restarted) return null;
            string cover = (string)t.GetField("EditorRestartCover", flags).GetValue(app) ?? "";
            return "commit " + covered + " live " + restarted + " cover " + cover;
        }

        static void CheckRestartDoesNotArmLessonGuard(System.Action<string, bool, string> Check)
        {
            var host = new GameObject("restart-guard");
            var next = new GameObject("restart-guard-next");
            int streak = Purse.Streak;
            try
            {
                Purse.SetStreak(0);
                var app = host.AddComponent<FlockFiveApp>();
                var t = typeof(FlockFiveApp);
                const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
                t.GetField("_board", flags).SetValue(app, LevelData.Open(0));
                t.GetField("_garden", flags).SetValue(app, new WorldBuilder.Garden());
                t.GetField("_splash", flags).SetValue(app, false);
                t.GetField("_busy", flags).SetValue(app, false);
                t.GetField("_restarting", flags).SetValue(app, false);
                t.GetField("_adHand", flags).SetValue(app, true);
                t.GetMethod("Restart", flags, null, System.Type.EmptyTypes, null).Invoke(app, null);
                bool handStill = (bool)t.GetField("_adHand", flags).GetValue(app);
                if (handStill)
                {
                    var round = t.GetField("EditorSnapRound", flags).GetValue(app) as System.Collections.IEnumerator;
                    if (round != null) round.MoveNext();
                }
                bool handLeft = (bool)t.GetField("_adHand", flags).GetValue(app);
                bool guardUp = (bool)t.GetMethod("LessonGuardUp", flags, null, System.Type.EmptyTypes, null).Invoke(app, null);
                float until = (float)t.GetField("_lessonGuardUntil", flags).GetValue(app);
                Check("restart-leaves-lesson-guard-down", !guardUp && until == 0f && !handLeft,
                    "guard " + guardUp + " until " + until.ToString("0.00")
                    + " hand " + handLeft
                    + " unscaled " + AdPause.Unscaled.ToString("0.00"));

                var fresh = next.AddComponent<FlockFiveApp>();
                bool freshUp = (bool)t.GetMethod("LessonGuardUp", flags, null, System.Type.EmptyTypes, null).Invoke(fresh, null);
                float freshUntil = (float)t.GetField("_lessonGuardUntil", flags).GetValue(fresh);
                Check("fresh-app-starts-lesson-guard-down", !freshUp && freshUntil == 0f,
                    "guard " + freshUp + " until " + freshUntil.ToString("0.00"));

                t.GetField("_adHand", flags).SetValue(fresh, true);
                t.GetMethod("DismissAdHand", flags, null, System.Type.EmptyTypes, null).Invoke(fresh, null);
                bool armed = (bool)t.GetMethod("LessonGuardUp", flags, null, System.Type.EmptyTypes, null).Invoke(fresh, null);
                float armedUntil = (float)t.GetField("_lessonGuardUntil", flags).GetValue(fresh);
                Check("lesson-dismiss-arms-the-guard",
                    armed && armedUntil >= AdPause.Unscaled + CoachDismiss.GuardSeconds - 0.01f,
                    "guard " + armed + " until " + armedUntil.ToString("0.00")
                    + " unscaled " + AdPause.Unscaled.ToString("0.00"));
            }
            catch (System.Exception ex)
            {
                Check("restart-lesson-guard", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                Purse.SetStreak(streak);
                UnityEngine.Object.DestroyImmediate(host);
                UnityEngine.Object.DestroyImmediate(next);
            }
        }

        static void CheckGiftRestartCovers(System.Action<string, bool, string> Check)
        {
            int[] widths = { 820, 402, 412, 360 };
            EachPhoneWidth(Check, "gift-restart-swallows-lesson", (app, t, flags) =>
            {
                var guard = t.GetField("_lessonGuardUntil", flags);
                float keep = (float)guard.GetValue(app);
                try
                {
                    guard.SetValue(app, AdPause.Unscaled + 30f);
                    t.GetField("_ruleTip", flags).SetValue(app, RuleTip.Feeder);
                    string fault = PressGiftRestart(app, t, flags, false);
                    string tip = t.GetField("_ruleTip", flags).GetValue(app).ToString();
                    if (fault == null && tip == "Feeder") return null;
                    return (fault ?? "restarted") + " tip " + tip;
                }
                finally { guard.SetValue(app, keep); }
            }, widths);
            EachPhoneWidth(Check, "gift-restart-swallows-veil", (app, t, flags) =>
            {
                t.GetField("_settingsOpen", flags).SetValue(app, true);
                return PressGiftRestart(app, t, flags, false);
            }, widths);
            EachPhoneWidth(Check, "gift-restart-swallows-hive-inspect", (app, t, flags) =>
            {
                t.GetField("_hiveInspect", flags).SetValue(app, 0);
                t.GetField("_levelHive", flags).SetValue(app, true);
                t.GetField("_levelHiveRect", flags).SetValue(app, new Rect(0f, 0f, 4f, 4f));
                string fault = PressGiftRestart(app, t, flags, false);
                int inspect = (int)t.GetField("_hiveInspect", flags).GetValue(app);
                bool levelStill = (bool)t.GetField("_levelHive", flags).GetValue(app);
                if (fault != null || inspect != 0 || !levelStill)
                    return (fault ?? "restarted") + " inspect " + inspect + " level " + levelStill
                        + " cover " + (t.GetField("EditorRestartCover", flags).GetValue(app) ?? "");
                t.GetField("_hiveInspect", flags).SetValue(app, -1);
                string closed = PressGiftRestart(app, t, flags, false);
                bool level = (bool)t.GetField("_levelHive", flags).GetValue(app);
                if (closed == null && !level) return null;
                return (closed ?? "open") + " level " + level
                    + " cover " + (t.GetField("EditorRestartCover", flags).GetValue(app) ?? "");
            }, widths);
            EachPhoneWidth(Check, "gift-restart-fires-once", (app, t, flags) =>
            {
                return PressGiftRestart(app, t, flags, true);
            }, widths);
        }

        static void CheckSameFrameLifts(System.Action<string, bool, string> Check)
        {
            EachPhoneWidth(Check, "same-frame-lift", (app, t, flags) =>
            {
                var resume = t.GetField("_resumeInputUntil", BindingFlags.NonPublic | BindingFlags.Static);
                var ids = (System.Collections.Generic.HashSet<int>)t.GetField("_resumeOriginIds", flags).GetValue(app);
                object[] hud = { default(Rect), default(Rect) };
                t.GetMethod("HudShaken", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, hud);
                var restart = (Rect)hud[0];
                var window = new Vector2(restart.center.x + PhoneArea.OffX, restart.center.y);
                void Edge(int id, bool down)
                {
                    FlockFiveApp.EditorEndedIds.Clear();
                    FlockFiveApp.EditorPointerInject = new FlockFiveApp.EditorPointer
                    {
                        On = true, Down = down, Up = !down, Id = id, Window = window
                    };
                    t.GetMethod("UpdateBody", flags).Invoke(app, null);
                    t.GetMethod("TakeGuiPointer", flags).Invoke(app, null);
                }
                t.GetField("_splash", flags).SetValue(app, false);
                t.GetField("_adHand", flags).SetValue(app, false);
                t.GetField("_freezeOffer", flags).SetValue(app, false);
                t.GetField("_gift", flags).SetValue(app, System.Enum.ToObject(
                    t.GetNestedType("GiftFace", BindingFlags.NonPublic), 0));
                t.GetField("_restarting", flags).SetValue(app, false);
                t.GetField("EditorSnapRound", flags).SetValue(app, null);
                resume.SetValue(null, AdPause.Unscaled + 5f);
                Edge(4, true);
                Edge(9, true);
                if (!ids.Contains(4) || !ids.Contains(9))
                    return "down 4 " + ids.Contains(4) + " 9 " + ids.Contains(9);
                resume.SetValue(null, AdPause.Unscaled - 5f);
                FlockFiveApp.EditorEndedIds.Clear();
                FlockFiveApp.EditorEndedIds.Add(4);
                FlockFiveApp.EditorEndedIds.Add(9);
                FlockFiveApp.EditorPointerInject = new FlockFiveApp.EditorPointer
                {
                    On = true, Down = false, Up = true, Id = 4, Window = window
                };
                t.GetMethod("UpdateBody", flags).Invoke(app, null);
                t.GetMethod("TakeGuiPointer", flags).Invoke(app, null);
                bool bothGone = !ids.Contains(4) && !ids.Contains(9);
                bool restarted = RestartLive(t, flags, app);
                FlockFiveApp.EditorEndedIds.Clear();
                if (!bothGone || restarted)
                    return "both 4 " + ids.Contains(4) + " 9 " + ids.Contains(9) + " restart " + restarted;
                t.GetField("EditorSnapRound", flags).SetValue(app, null);
                t.GetField("_restarting", flags).SetValue(app, false);
                resume.SetValue(null, AdPause.Unscaled + 5f);
                Edge(4, true);
                Edge(PlayPointer.MouseId, true);
                resume.SetValue(null, AdPause.Unscaled - 5f);
                FlockFiveApp.EditorEndedIds.Clear();
                FlockFiveApp.EditorPointerInject = new FlockFiveApp.EditorPointer
                {
                    On = true, Down = false, Up = true, Id = PlayPointer.MouseId, Window = window
                };
                t.GetMethod("UpdateBody", flags).Invoke(app, null);
                t.GetMethod("TakeGuiPointer", flags).Invoke(app, null);
                bool touchLeft = ids.Contains(4);
                bool mouseLeft = ids.Contains(PlayPointer.MouseId);
                bool mouseRestart = RestartLive(t, flags, app);
                FlockFiveApp.EditorEndedIds.Clear();
                if (touchLeft && !mouseLeft && !mouseRestart) return null;
                return "mouse touch " + touchLeft + " mouse " + mouseLeft + " restart " + mouseRestart;
            }, new[] { 820, 402 });
        }

        static void CheckDistinctReleaseIds(System.Action<string, bool, string> Check)
        {
            EachPhoneWidth(Check, "release-id-keeps-other-finger", (app, t, flags) =>
            {
                var resume = t.GetField("_resumeInputUntil", BindingFlags.NonPublic | BindingFlags.Static);
                resume.SetValue(null, AdPause.Unscaled + 5f);
                t.GetField("_splash", flags).SetValue(app, false);
                t.GetField("EditorEdgeStamps", flags).SetValue(app, 0);
                t.GetField("EditorSnapRound", flags).SetValue(app, null);
                object[] hud = { default(Rect), default(Rect) };
                t.GetMethod("HudShaken", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, hud);
                var restart = (Rect)hud[0];
                var window = new Vector2(restart.center.x + PhoneArea.OffX, restart.center.y);
                void Edge(int id, bool down)
                {
                    FlockFiveApp.EditorPointerInject = new FlockFiveApp.EditorPointer
                    {
                        On = true, Down = down, Up = !down, Id = id, Window = window
                    };
                    t.GetMethod("UpdateBody", flags).Invoke(app, null);
                    t.GetMethod("TakeGuiPointer", flags).Invoke(app, null);
                }
                Edge(4, true);
                Edge(9, false);
                int stamps = (int)t.GetField("EditorEdgeStamps", flags).GetValue(app);
                bool otherUp = RestartLive(t, flags, app);
                resume.SetValue(null, AdPause.Unscaled - 5f);
                Edge(4, false);
                bool matched = RestartLive(t, flags, app);
                if (stamps >= 2 && !otherUp && !matched) return null;
                return "stamps " + stamps + " otherUp " + otherUp + " matched " + matched;
            }, new[] { 820, 402 });
        }

        static void CheckRestartAskAfterResume(System.Action<string, bool, string> Check)
        {
            EachPhoneWidth(Check, "restart-ask-after-resume", (app, t, flags) =>
            {
                var resume = t.GetField("_resumeInputUntil", BindingFlags.NonPublic | BindingFlags.Static);
                resume.SetValue(null, AdPause.Unscaled - 5f);
                Purse.SetStreak(2);
                t.GetField("_splash", flags).SetValue(app, false);
                t.GetField("_gift", flags).SetValue(app, System.Enum.ToObject(t.GetNestedType("GiftFace", BindingFlags.NonPublic), 0));
                t.GetField("_freezeOffer", flags).SetValue(app, false);
                t.GetField("EditorSnapRound", flags).SetValue(app, null);
                t.GetField("_restarting", flags).SetValue(app, false);
                object[] hud = { default(Rect), default(Rect) };
                t.GetMethod("HudShaken", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, hud);
                var restart = (Rect)hud[0];
                var window = new Vector2(restart.center.x + PhoneArea.OffX, restart.center.y);
                FlockFiveApp.EditorPointerInject = new FlockFiveApp.EditorPointer
                {
                    On = true, Down = true, Up = false, Id = 12, Window = window
                };
                t.GetMethod("UpdateBody", flags).Invoke(app, null);
                FlockFiveApp.EditorPointerInject = new FlockFiveApp.EditorPointer
                {
                    On = true, Down = false, Up = true, Id = 12, Window = window
                };
                t.GetMethod("UpdateBody", flags).Invoke(app, null);
                t.GetMethod("TakeGuiPointer", flags).Invoke(app, null);
                var ask = t.GetField("_restartAsk", flags).GetValue(app).ToString();
                bool snap = t.GetField("EditorSnapRound", flags).GetValue(app) != null;
                bool live = RestartLive(t, flags, app);
                if (ask != "None" && !snap && live) return null;
                return "ask " + ask + " snap " + snap + " live " + live + " off " + PhoneArea.OffX;
            }, new[] { 820, 402 });
        }

        static void CheckPointerGuard(System.Action<string, bool, string> Check)
        {
            string root = System.IO.Path.Combine(UnityEngine.Application.dataPath, "_Project/Scripts");
            var allow = new System.Collections.Generic.HashSet<string>
            {
                "PlayPointer.cs", "PhoneArea.cs", "FlockFiveApp.B129Shot.cs"
            };
            var bad = new System.Text.StringBuilder();
            foreach (var file in System.IO.Directory.GetFiles(root, "*.cs", System.IO.SearchOption.AllDirectories))
            {
                string name = System.IO.Path.GetFileName(file);
                if (allow.Contains(name)) continue;
                string src = StripNotes(StripUnityEditor(System.IO.File.ReadAllText(file)));
                if (src.Contains(".mousePosition") || src.Contains("Input.mousePosition") || src.Contains("PhoneArea.OffX")
                    || src.Contains("WindowGuiToPlay") || src.Contains("GloveWindowHit") || src.Contains("GloveWindowRect"))
                    bad.Append(name).Append("; ");
            }
            Check("pointer-enters-once", bad.Length == 0, bad.ToString());
        }

        static bool GloveCovers(Rect glove, Rect target)
        {
            float ix = Mathf.Min(glove.xMax, target.xMax) - Mathf.Max(glove.xMin, target.xMin);
            float iy = Mathf.Min(glove.yMax, target.yMax) - Mathf.Max(glove.yMin, target.yMin);
            return ix > 0.2f && iy > 0.2f;
        }

        static string StripUnityEditor(string src)
        {
            var sb = new System.Text.StringBuilder(src.Length);
            var kind = new System.Collections.Generic.Stack<int>();
            int skip = 0;
            int i = 0;
            while (i < src.Length)
            {
                int lineEnd = src.IndexOf('\n', i);
                if (lineEnd < 0) lineEnd = src.Length;
                string line = src.Substring(i, lineEnd - i);
                string trim = line.TrimStart();
                bool keep = skip == 0;
                if (trim.StartsWith("#if"))
                {
                    bool editor = trim.Contains("UNITY_EDITOR") && !trim.Contains("!UNITY_EDITOR");
                    kind.Push(editor ? 1 : 0);
                    if (editor) skip++;
                }
                else if (trim.StartsWith("#elif") || trim.StartsWith("#else"))
                {
                    if (kind.Count > 0 && kind.Peek() == 1)
                    {
                        kind.Pop();
                        kind.Push(2);
                        if (skip > 0) skip--;
                    }
                }
                else if (trim.StartsWith("#endif"))
                {
                    if (kind.Count > 0 && kind.Pop() == 1 && skip > 0) skip--;
                }
                if (keep) sb.Append(line).Append('\n');
                i = lineEnd < src.Length ? lineEnd + 1 : src.Length;
            }
            return sb.ToString();
        }

        static string StripNotes(string src)
        {
            var sb = new System.Text.StringBuilder(src.Length);
            bool line = false, block = false;
            for (int i = 0; i < src.Length; i++)
            {
                char c = src[i];
                char n = i + 1 < src.Length ? src[i + 1] : '\0';
                if (block)
                {
                    if (c == '*' && n == '/') { block = false; i++; }
                    else if (c == '\n') sb.Append('\n');
                    continue;
                }
                if (line)
                {
                    if (c == '\n') { line = false; sb.Append('\n'); }
                    continue;
                }
                if (c == '/' && n == '/') { line = true; continue; }
                if (c == '/' && n == '*') { block = true; continue; }
                sb.Append(c);
            }
            return sb.ToString();
        }

        static void CheckNotNowPressSkipsRestart(System.Action<string, bool, string> Check)
        {
            EachPhoneWidth(Check, "not-now-press-skips-restart", (app, t, flags) =>
            {
                var home = t.GetField("_home", flags);
                home.SetValue(app, System.Enum.ToObject(home.FieldType, 0));
                t.GetField("_splash", flags).SetValue(app, true);
                float s = DeviceFit.Ui();
                float btnH = 48f * s;
                var card = new Rect(24f, PhoneArea.H * 0.42f, PhoneArea.W - 48f, 220f * s);
                FlockFiveApp.AskButtonRects(card, s, btnH, out _, out var no);
                t.GetField("_dailyAskOpen", flags).SetValue(app, true);
                t.GetField("_dailyAskAt", flags).SetValue(app, AdPause.Unscaled);
                t.GetField("_dailyAskNoHit", flags).SetValue(app, no);
                t.GetField("_dailyAskNoHitOn", flags).SetValue(app, true);
                bool step = PressThenRelease(app, t, flags, no.center);
                bool closed = !(bool)t.GetField("_dailyAskOpen", flags).GetValue(app);
                if (step && closed && !RestartLive(t, flags, app)) return null;
                return "step " + step + " closed " + closed + " restart " + RestartLive(t, flags, app);
            });
        }

        static void CheckNamingPressSkipsRestart(System.Action<string, bool, string> Check)
        {
            EachPhoneWidth(Check, "naming-press-skips-restart", (app, t, flags) =>
            {
                var home = t.GetField("_home", flags);
                home.SetValue(app, System.Enum.ToObject(home.FieldType, 0));
                t.GetField("_splash", flags).SetValue(app, true);
                t.GetField("_avatarRename", flags).SetValue(app, true);
                t.GetMethod("RegisterAvatarPlate", flags).Invoke(app, new object[] { DeviceFit.Ui() });
                var plate = (Rect)t.GetField("_avatarRenameR", flags).GetValue(app);
                var controls = (System.Collections.Generic.List<Rect>)t.GetField("_plateControls", flags).GetValue(app);
                Vector2 at = default;
                bool found = false;
                for (float y = plate.yMin + 3f; y < plate.yMax - 3f && !found; y += 5f)
                {
                    for (float x = plate.xMin + 3f; x < plate.xMax - 3f && !found; x += 7f)
                    {
                        var p = new Vector2(x, y);
                        bool hit = false;
                        for (int c = 0; c < controls.Count; c++)
                            if (controls[c].Contains(p)) hit = true;
                        if (hit || !plate.Contains(p)) continue;
                        at = p;
                        found = true;
                    }
                }
                if (!found) return "no empty plate point off " + PhoneArea.OffX;
                bool step = PressThenRelease(app, t, flags, at);
                string who = (string)t.GetField("_tapWho", flags).GetValue(app) ?? "";
                bool still = (bool)t.GetField("_avatarRename", flags).GetValue(app);
                if (step && still && who == "naming" && !RestartLive(t, flags, app)) return null;
                return "step " + step + " still " + still + " who " + who + " restart " + RestartLive(t, flags, app);
            });
        }

        static void CheckNamingControls(System.Action<string, bool, string> Check)
        {
            var bow = typeof(FlockFiveApp).GetField("_avatarBowArt", BindingFlags.NonPublic | BindingFlags.Static);
            int keepBow = (int)bow.GetValue(null);
            try
            {
                bow.SetValue(null, 1);
                EachPhoneWidth(Check, "naming-done-and-chip", (app, t, flags) =>
                {
                    var homeFace = t.GetNestedType("HomeFace", BindingFlags.NonPublic);
                    var home = t.GetField("_home", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (home == null || homeFace == null) return "missing _home";
                    home.SetValue(app, System.Enum.Parse(homeFace, "Splash"));
                    t.GetField("_splash", flags).SetValue(app, true);
                    t.GetField("_avatarRename", flags).SetValue(app, true);
                    t.GetField("_avatarRenameText", flags).SetValue(app, "Pip");
                    t.GetMethod("RegisterAvatarPlate", flags).Invoke(app, new object[] { DeviceFit.Ui() });
                    var done = (Rect)t.GetField("_avatarDoneR", flags).GetValue(app);
                    var field = (Rect)t.GetField("_avatarFieldR", flags).GetValue(app);
                    var swatch = ((Rect[])t.GetField("_avatarSwatch", flags).GetValue(app))[0];
                    var kits = (Rect[])t.GetField("_avatarKitR", flags).GetValue(app);
                    int n = (int)t.GetField("_avatarKitN", flags).GetValue(app);
                    if (done.width < 2f || field.width < 2f || swatch.width < 2f || n < 2)
                        return "layout done " + done + " field " + field + " swatch " + swatch + " kits " + n;
                    var swatches = (Rect[])t.GetField("_avatarSwatch", flags).GetValue(app);
                    var color = swatches[2];
                    string Release(Rect r, int id)
                    {
                        var window = new Vector2(r.center.x + PhoneArea.OffX, r.center.y);
                        FlockFiveApp.EditorPointerInject = new FlockFiveApp.EditorPointer
                        {
                            On = true, Down = true, Up = false, Id = id, Window = window
                        };
                        t.GetMethod("TakeGuiPointer", flags).Invoke(app, null);
                        FlockFiveApp.EditorPointerInject = new FlockFiveApp.EditorPointer
                        {
                            On = true, Down = false, Up = true, Id = id, Window = window
                        };
                        t.GetMethod("TakeGuiPointer", flags).Invoke(app, null);
                        var entered = PlayPointer.Enter(window);
                        if (Mathf.Abs(entered.x - r.center.x) > 0.6f || Mathf.Abs(entered.y - r.center.y) > 0.6f)
                            return "enter " + entered + " want " + r.center;
                        return null;
                    }
                    var colField = t.GetField("_avatarCol", flags);
                    colField.SetValue(app, System.Enum.ToObject(colField.FieldType, 0));
                    string colorMsg = Release(color, 12);
                    int col = (int)colField.GetValue(app);
                    if (colorMsg != null || col != 2) return "swatch " + colorMsg + " col " + col;
                    string fieldMsg = Release(field, 13);
                    bool focused = (bool)t.GetField("_avatarRenameFocus", flags).GetValue(app);
                    bool open = (bool)t.GetField("_avatarRename", flags).GetValue(app);
                    if (fieldMsg != null || !focused || !open) return "field " + fieldMsg + " focus " + focused + " open " + open;
                    PlayerPrefs.SetInt("flockfive.avatar.kit", 0);
                    string kitMsg = Release(kits[1], 14);
                    int kit = PlayerPrefs.GetInt("flockfive.avatar.kit", 0);
                    if (kitMsg != null || kit != 1) return "kit " + kitMsg + " saved " + kit;
                    string doneMsg = Release(done, 11);
                    bool closed = !(bool)t.GetField("_avatarRename", flags).GetValue(app);
                    string name = PlayerPrefs.GetString("flockfive.avatar.name", "");
                    if (doneMsg != null || !closed || name != "Pip") return "done " + doneMsg + " closed " + closed + " name " + name;
                    if (RestartLive(t, flags, app)) return "restart";
                    return null;
                }, new[] { 820, 402 });
            }
            finally
            {
                bow.SetValue(null, keepBow);
            }
        }

        static void EachPhoneWidth(System.Action<string, bool, string> Check, string name,
            System.Func<FlockFiveApp, System.Type, BindingFlags, string> body, int[] widths = null)
        {
            if (widths == null) widths = new[] { 820, 402, 412, 360 };
            var winOn = typeof(PhoneArea).GetField("EditorWindowOn", BindingFlags.NonPublic | BindingFlags.Static);
            var winW = typeof(PhoneArea).GetField("EditorWindowW", BindingFlags.NonPublic | BindingFlags.Static);
            var winH = typeof(PhoneArea).GetField("EditorWindowH", BindingFlags.NonPublic | BindingFlags.Static);
            var safeTop = typeof(PhoneArea).GetField("EditorSafeTop", BindingFlags.NonPublic | BindingFlags.Static);
            var safeBot = typeof(PhoneArea).GetField("EditorSafeBottom", BindingFlags.NonPublic | BindingFlags.Static);
            bool keepOn = (bool)winOn.GetValue(null);
            int keepW = (int)winW.GetValue(null);
            int keepH = (int)winH.GetValue(null);
            float keepTop = (float)safeTop.GetValue(null);
            float keepBot = (float)safeBot.GetValue(null);
            try
            {
                for (int i = 0; i < widths.Length; i++)
                {
                    int w = widths[i];
                    int h = w == 820 ? 1180 : w == 402 ? 874 : w == 412 ? 915 : 800;
                    winOn.SetValue(null, true);
                    winW.SetValue(null, w);
                    winH.SetValue(null, h);
                    safeTop.SetValue(null, 59f);
                    safeBot.SetValue(null, 34f);
                    int wantOff = w == 820 ? 78 : 0;
                    if (PhoneArea.OffX != wantOff)
                    {
                        Check(name + "-" + w, false, "off " + PhoneArea.OffX + " want " + wantOff);
                        continue;
                    }
                    var go = new GameObject(name + "-" + w);
                    var prev = Event.current;
                    var inject = FlockFiveApp.EditorPointerInject;
                    int streak = Purse.Streak;
                    var resume = typeof(FlockFiveApp).GetField("_resumeInputUntil", BindingFlags.NonPublic | BindingFlags.Static);
                    float keepResume = (float)resume.GetValue(null);
                    try
                    {
                        Event.current = null;
                        FlockFiveApp.EditorEndedIds.Clear();
                        Purse.SetStreak(0);
                        resume.SetValue(null, float.NegativeInfinity);
                        var app = go.AddComponent<FlockFiveApp>();
                        var t = typeof(FlockFiveApp);
                        var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
                        t.GetField("_board", flags).SetValue(app, LevelData.Open(0));
                        t.GetField("_garden", flags).SetValue(app, new WorldBuilder.Garden());
                        t.GetField("_busy", flags).SetValue(app, false);
                        t.GetField("_restarting", flags).SetValue(app, false);
                        string detail = body(app, t, flags);
                        bool ok = string.IsNullOrEmpty(detail);
                        Check(name + "-" + w, ok, ok
                            ? "off " + PhoneArea.OffX + " playW " + PhoneArea.W
                            : detail);
                    }
                    catch (System.Exception ex)
                    {
                        Check(name + "-" + w, false, ex.GetType().Name + " " + ex.Message);
                    }
                    finally
                    {
                        Event.current = prev;
                        FlockFiveApp.EditorPointerInject = inject;
                        resume.SetValue(null, keepResume);
                        Purse.SetStreak(streak);
                        UnityEngine.Object.DestroyImmediate(go);
                    }
                }
            }
            finally
            {
                winOn.SetValue(null, keepOn);
                winW.SetValue(null, keepW);
                winH.SetValue(null, keepH);
                safeTop.SetValue(null, keepTop);
                safeBot.SetValue(null, keepBot);
            }
        }

        static bool PressThenRelease(FlockFiveApp app, System.Type t, BindingFlags flags, Vector2 gui)
        {
            InjectGuiPress(gui, 9);
            t.GetMethod("UpdateBody", flags).Invoke(app, null);
            if (RestartLive(t, flags, app)) return false;
            t.GetMethod("TakeGuiPointer", flags).Invoke(app, null);
            if (RestartLive(t, flags, app)) return false;
            InjectGuiRelease(gui, 9);
            t.GetMethod("UpdateBody", flags).Invoke(app, null);
            if (RestartLive(t, flags, app)) return false;
            t.GetMethod("TakeGuiPointer", flags).Invoke(app, null);
            return !RestartLive(t, flags, app) && !FlockFiveApp.EditorPointerInject.On;
        }

        static bool RestartLive(System.Type t, BindingFlags flags, FlockFiveApp app)
        {
            var ask = t.GetField("_restartAsk", flags).GetValue(app);
            return t.GetField("EditorSnapRound", flags).GetValue(app) != null
                || (bool)t.GetField("_restarting", flags).GetValue(app)
                || (ask != null && ask.ToString() != "None");
        }

        // gui is phone-local (the rects). The inject stores the real window point,
        // pillar included. PlayPointer is the only place that subtracts it.
        static void InjectGuiRelease(Vector2 gui, int id) => InjectGuiEdge(gui, id, false);

        static void InjectGuiPress(Vector2 gui, int id) => InjectGuiEdge(gui, id, true);

        static void InjectGuiEdge(Vector2 gui, int id, bool down)
        {
            FlockFiveApp.EditorPointerInject = new FlockFiveApp.EditorPointer
            {
                On = true,
                Down = down,
                Up = !down,
                Id = id,
                Window = new Vector2(gui.x + PhoneArea.OffX, gui.y)
            };
        }

        // Editor HitPad with whatever Event.current is. No draw.
        static bool HitPadQuiet(Rect r)
        {
            var hit = typeof(FlockFiveApp).GetMethod("HitPad", BindingFlags.NonPublic | BindingFlags.Static, null,
                new[] { typeof(Rect), typeof(bool).MakeByRefType() }, null);
            object[] args = { r, false };
            return (bool)hit.Invoke(null, args);
        }

        static void CheckReminderAfterReward(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("reminder-reward");
            try
            {
                var app = go.AddComponent<FlockFiveApp>();
                var t = typeof(FlockFiveApp);
                var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
                var idle = t.GetMethod("IdleAfterReward", flags);
                t.GetField("_welcomeOpen", flags).SetValue(app, true);
                t.GetField("_streakSlide", flags).SetValue(app, -1f);
                t.GetField("_rewardGap", flags).SetValue(app, false);
                PlayerPrefs.SetInt("flockfive.welcome.pending", 0);
                bool during = (bool)idle.Invoke(app, null);
                t.GetField("_welcomeOpen", flags).SetValue(app, false);
                t.GetField("_dailyOpen", flags).SetValue(app, false);
                t.GetField("_dailyAskOpen", flags).SetValue(app, false);
                t.GetField("_welcomeQueued", flags).SetValue(app, false);
                t.GetField("_welcomeGlove", flags).SetValue(app, false);
                bool after = (bool)idle.Invoke(app, null);
                bool queued = !FlockFiveApp.ReminderOpensNow(during) && FlockFiveApp.ReminderOpensNow(after);
                Check("streak-reminder-waits-for-reward-card", queued,
                    "during " + during + " after " + after);
            }
            catch (System.Exception ex)
            {
                Check("streak-reminder-waits-for-reward-card", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
#endif
