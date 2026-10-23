#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FlockFive.Editor
{
    // Brandon, iPhone:
    // 1. Welcome bonus (popup + coin blips) played while the app was backgrounded.
    //    Defer the grant until resume, focused, on the visible home. Coins once. No blips while paused.
    // 2. The studio card (3.0s + 0.5s hold + 0.25s fade) absorbs every raycast and the bonus waits it out.
    public static class WelcomeStudioGateTests
    {
        const string Out = "/tmp/flock-five-welcome-studio-tests.txt";

        [MenuItem("Flock Five/Welcome Studio Gate Tests")]
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail)
            {
                string line = (ok ? "PASS  " : "FAIL  ") + name + "  " + detail;
                if (ok) pass++; else fail++;
                sb.AppendLine(line);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[welcome-studio] " + line);
            }

            try
            {
                BonusWhilePaused(Check);
                StudioCardDefersBonus(Check);
                CoinAudioWhilePaused(Check);
                CardTiming(Check);
                ShieldRaycast(Check);
                Wiring(Check);
            }
            finally
            {
                ForegroundGate.ResetForTest();
            }

            string end = fail == 0 ? "WELCOME_STUDIO_OK " + pass : "WELCOME_STUDIO_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[welcome-studio] " + end);
            if (fail > 0) throw new System.Exception(end);
        }

        // Left the app just before the award. Nothing plays. Resume on the home plays it once.
        static void BonusWhilePaused(System.Action<string, bool, string> Check)
        {
            ForegroundGate.ResetForTest();
            bool granted = false;
            int plays = 0;
            WelcomeAward.Act Step(bool focus, bool pause, bool home, bool card)
            {
                var act = WelcomeAward.Decide(granted, false, false, true, false, focus, pause, home, card);
                if (WelcomeAward.PlaysCoins(act))
                {
                    granted = true;
                    plays++;
                }
                return act;
            }

            var away = Step(true, true, true, false);
            var blurred = Step(false, false, true, false);
            Check("bonus-deferred-while-paused", away == WelcomeAward.Act.Defer && plays == 0, away.ToString());
            Check("bonus-deferred-while-unfocused", blurred == WelcomeAward.Act.Defer && plays == 0, blurred.ToString());
            var again = Step(true, true, true, false);
            Check("bonus-stays-deferred-across-pause", again == WelcomeAward.Act.Defer && plays == 0, again.ToString());
            var back = Step(true, false, true, false);
            var twice = Step(true, false, true, false);
            Check("bonus-grants-once-on-resume", back == WelcomeAward.Act.Grant && plays == 1, back + " plays " + plays);
            Check("bonus-does-not-replay", twice == WelcomeAward.Act.None && plays == 1, twice + " plays " + plays);
            Check("bonus-needs-the-home",
                WelcomeAward.Decide(false, false, false, true, false, true, false, false, false) == WelcomeAward.Act.Defer,
                "home hidden defers");
            Check("may-present-requires-focus-and-home",
                WelcomeAward.MayPresent(true, false, true, false)
                && !WelcomeAward.MayPresent(true, true, true, false)
                && !WelcomeAward.MayPresent(false, false, true, false)
                && !WelcomeAward.MayPresent(true, false, false, false)
                && !WelcomeAward.MayPresent(true, false, true, true), "");
        }

        // Card up: no grant, no coin play. Card gone: the same owed bonus grants once.
        static void StudioCardDefersBonus(System.Action<string, bool, string> Check)
        {
            bool granted = false;
            int plays = 0;
            WelcomeAward.Act Step(bool card)
            {
                var act = WelcomeAward.Decide(granted, false, false, true, false, true, false, true, card);
                if (WelcomeAward.PlaysCoins(act)) { granted = true; plays++; }
                return act;
            }
            var under = Step(true);
            Check("studio-card-defers-bonus", under == WelcomeAward.Act.Defer && plays == 0
                && WelcomeAward.HoldCoinPlay(true, true), under.ToString());
            var shown = Step(false);
            var again = Step(false);
            Check("bonus-after-card-once", shown == WelcomeAward.Act.Grant && plays == 1 && again == WelcomeAward.Act.None,
                shown + " then " + again + " plays " + plays);
            Check("coins-play-only-on-grant", !WelcomeAward.PlaysCoins(WelcomeAward.Act.Defer)
                && !WelcomeAward.PlaysCoins(WelcomeAward.Act.None)
                && WelcomeAward.PlaysCoins(WelcomeAward.Act.Grant), "");
            Check("lesson-still-defers", WelcomeAward.Decide(false, false, false, true, true, true, false, true, false) == WelcomeAward.Act.Defer, "");
            Check("blocked-popup-does-not-grant", WelcomeAward.Decide(false, false, false, false, false, true, false, true, false) == WelcomeAward.Act.None, "");
        }

        static void CoinAudioWhilePaused(System.Action<string, bool, string> Check)
        {
            ForegroundGate.ResetForTest();
            int before = Sfx.LastShots;
            ForegroundGate.NotePause(true);
            Sfx.Clink();
            bool pausedSilent = Sfx.LastShots == before && !ForegroundGate.AllowsCoinAudio
                && WelcomeAward.HoldCoinPlay(ForegroundGate.AllowsCoinAudio, false);
            ForegroundGate.NotePause(false);
            ForegroundGate.NoteFocus(false);
            Sfx.Clink();
            bool blurSilent = Sfx.LastShots == before && !ForegroundGate.AllowsCoinAudio;
            ForegroundGate.ResetForTest();
            Check("no-coin-audio-while-paused", pausedSilent, "shots " + Sfx.LastShots + " before " + before);
            Check("no-coin-audio-while-unfocused", blurSilent, "");
            Check("coin-audio-when-foreground", ForegroundGate.AllowsCoinAudio
                && !WelcomeAward.HoldCoinPlay(true, false), "");
        }

        static void CardTiming(System.Action<string, bool, string> Check)
        {
            float span = FlockFiveApp.StudioMinSec + FlockFiveApp.CardEndHoldSec + FlockFiveApp.WarmCoverFadeSec;
            Check("studio-card-timing-unchanged",
                Mathf.Abs(FlockFiveApp.StudioMinSec - 3f) < 0.001f
                && Mathf.Abs(FlockFiveApp.CardEndHoldSec - 0.5f) < 0.001f
                && Mathf.Abs(FlockFiveApp.WarmCoverFadeSec - 0.25f) < 0.001f
                && Mathf.Abs(span - 3.75f) < 0.001f,
                span.ToString("0.00"));
        }

        static void ShieldRaycast(System.Action<string, bool, string> Check)
        {
            var made = new List<GameObject>();
            RenderTexture tex = null;
            try
            {
                var es = EventSystem.current;
                if (es == null)
                {
                    var esGo = new GameObject("welcome-gate-es");
                    made.Add(esGo);
                    es = esGo.AddComponent<EventSystem>();
                }

                var camGo = new GameObject("welcome-gate-cam");
                made.Add(camGo);
                var cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 5f;
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 100f;
                cam.transform.position = new Vector3(0f, 0f, -10f);
                tex = new RenderTexture(400, 800, 16);
                cam.targetTexture = tex;
                cam.pixelRect = new Rect(0f, 0f, 400f, 800f);
                cam.enabled = true;

                var home = new GameObject("home-canvas");
                made.Add(home);
                var hc = home.AddComponent<Canvas>();
                hc.renderMode = RenderMode.ScreenSpaceCamera;
                hc.worldCamera = cam;
                hc.planeDistance = 12f;
                hc.sortingOrder = 0;
                home.AddComponent<GraphicRaycaster>();
                var btnGo = new GameObject("HomeButton");
                btnGo.transform.SetParent(home.transform, false);
                var btn = btnGo.AddComponent<Image>();
                btn.color = Color.white;
                btn.raycastTarget = true;
                var brt = btn.rectTransform;
                brt.anchorMin = Vector2.zero;
                brt.anchorMax = Vector2.one;
                brt.offsetMin = Vector2.zero;
                brt.offsetMax = Vector2.zero;

                var host = new GameObject("shield-host");
                made.Add(host);
                var shield = StudioCardShield.Ensure(host.transform);
                shield.AdoptCamera(cam);
                shield.SetBlocking(true);

                Canvas.ForceUpdateCanvases();
                Uncull(btn);
                Uncull(shield.Plate);

                Check("shield-is-full-screen-blocker", shield.BlocksRaycasts && shield.CoversScreen
                    && shield.Canvas.sortingOrder > hc.sortingOrder
                    && shield.Plate.raycastTarget, "sort " + shield.Canvas.sortingOrder);

                var point = (Vector2)cam.WorldToScreenPoint(shield.Plate.rectTransform.position);
                var homeCaster = home.GetComponent<GraphicRaycaster>();
                var shieldCaster = shield.GetComponent<GraphicRaycaster>();
                var hits = Raycast(es, point, shieldCaster, homeCaster);
                int plate = HitIndex(hits, shield.Plate.gameObject);
                int button = HitIndex(hits, btn.gameObject);
                Check("studio-card-blocks-raycasts", plate >= 0 && (button < 0 || plate < button),
                    Describe(hits, point, shield.Plate.rectTransform, btn.rectTransform));

                shield.SetBlocking(false);
                Canvas.ForceUpdateCanvases();
                Uncull(btn);
                hits = Raycast(es, point, shieldCaster, homeCaster);
                plate = HitIndex(hits, shield.Plate.gameObject);
                button = HitIndex(hits, btn.gameObject);
                Check("home-raycast-when-card-is-gone", plate < 0 && button >= 0 && !shield.BlocksRaycasts,
                    Describe(hits, point, shield.Plate != null ? shield.Plate.rectTransform : null, btn.rectTransform));
            }
            finally
            {
                for (int i = 0; i < made.Count; i++)
                {
                    if (made[i] == null) continue;
                    var cam = made[i].GetComponent<Camera>();
                    if (cam != null) cam.targetTexture = null;
                }
                for (int i = made.Count - 1; i >= 0; i--)
                    if (made[i] != null) Object.DestroyImmediate(made[i]);
                if (tex != null)
                {
                    tex.Release();
                    Object.DestroyImmediate(tex);
                }
            }
        }

        static void Uncull(Graphic g)
        {
            if (g != null && g.canvasRenderer != null) g.canvasRenderer.cull = false;
        }

        static List<RaycastResult> Raycast(EventSystem es, Vector2 point, GraphicRaycaster front, GraphicRaycaster back)
        {
            var hits = new List<RaycastResult>();
            var data = new PointerEventData(es) { position = point };
            if (front != null && front.isActiveAndEnabled) front.Raycast(data, hits);
            if (back != null && back.isActiveAndEnabled) back.Raycast(data, hits);
            if (hits.Count == 0) es.RaycastAll(data, hits);
            hits.Sort((a, b) => SortKey(b).CompareTo(SortKey(a)));
            return hits;
        }

        static int SortKey(RaycastResult hit)
        {
            var canvas = hit.gameObject != null ? hit.gameObject.GetComponentInParent<Canvas>() : null;
            return canvas != null ? canvas.sortingOrder : hit.sortingOrder;
        }

        static int HitIndex(List<RaycastResult> hits, GameObject go)
        {
            for (int i = 0; i < hits.Count; i++)
                if (hits[i].gameObject == go) return i;
            return -1;
        }

        static string Describe(List<RaycastResult> hits, Vector2 point, RectTransform plate, RectTransform button)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("point ").Append(point).Append(" hits ").Append(hits.Count);
            for (int i = 0; i < hits.Count && i < 4; i++)
                sb.Append(" [").Append(hits[i].gameObject.name).Append(" s").Append(hits[i].sortingOrder).Append(']');
            if (plate != null) sb.Append(" plate ").Append(plate.rect);
            if (button != null) sb.Append(" btn ").Append(button.rect);
            return sb.ToString();
        }

        static void Wiring(System.Action<string, bool, string> Check)
        {
            string app = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs");
            string daily = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.DailyBonus.cs");
            string pre = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Preload.cs");
            string sfx = File.ReadAllText("Assets/_Project/Scripts/App/Sfx.cs");
            string storm = File.ReadAllText("Assets/_Project/Scripts/App/GardenStorm.cs");

            string offer = Slice(daily, "void OfferWelcome()", "IEnumerator OpenWelcomeWhenPaid()");
            int decide = offer.IndexOf("WelcomeAward.Decide(", System.StringComparison.Ordinal);
            int credit = offer.IndexOf("Purse.Credit(WelcomeBonusCoins)", System.StringComparison.Ordinal);
            int clink = offer.IndexOf("Sfx.Clink()", System.StringComparison.Ordinal);
            Check("offer-gates-before-coins", decide >= 0 && credit > decide && clink > decide, "");

            string open = Slice(daily, "IEnumerator OpenWelcomeWhenPaid()", "void OpenAskPopup()");
            int may = open.IndexOf("WelcomeAward.MayPresent(", System.StringComparison.Ordinal);
            int show = open.IndexOf("_welcomeOpen = true", System.StringComparison.Ordinal);
            Check("popup-waits-until-present", may >= 0 && show > may, "");

            string pause = Slice(app, "void OnApplicationPause(bool paused)", "void OnApplicationQuit()");
            string focus = Slice(app, "void OnApplicationFocus(bool focus)", "void Update()");
            Check("pause-and-focus-update-the-gate",
                pause.Contains("ForegroundGate.NotePause(paused)") && pause.Contains("NoteAppBackground();")
                && focus.Contains("ForegroundGate.NoteFocus(focus)") && focus.Contains("NoteAppResume();"), "");
            Check("storm-notes-the-same-gate",
                storm.Contains("ForegroundGate.NotePause(paused)") && storm.Contains("ForegroundGate.NoteFocus(focus)"), "");

            int clinkFn = sfx.IndexOf("public static void Clink()", System.StringComparison.Ordinal);
            int ensure = sfx.IndexOf("Ensure();", clinkFn, System.StringComparison.Ordinal);
            Check("clink-bails-before-audio", clinkFn >= 0 && ensure > clinkFn
                && sfx.IndexOf("ForegroundGate.AllowsCoinAudio", clinkFn, System.StringComparison.Ordinal) < ensure, "");

            Check("coin-fly-holds-while-away", app.Contains("WelcomeAward.HoldCoinPlay("), "");
            Check("card-eats-pointers-and-shields",
                app.Contains("EatStudioCardPointer();") && app.Contains("SyncStudioShield();")
                && pre.Contains("e.Use();") && pre.Contains("SetBlocking("), "");
            Check("no-award-tick-under-the-card",
                Slice(app, "if (!StudioCardOwnsScreen)", "if (_splash) return;").Contains("TickDailyBonus();"), "");
            Check("startup-hold-still-tied-to-the-cover", pre.Contains("StartupInputHeld => WarmCoverLive"), "");
        }

        static string Slice(string src, string from, string to)
        {
            int a = src.IndexOf(from, System.StringComparison.Ordinal);
            if (a < 0) return "";
            int b = src.IndexOf(to, a + from.Length, System.StringComparison.Ordinal);
            return b < 0 ? src.Substring(a) : src.Substring(a, b - a);
        }
    }
}
#endif
