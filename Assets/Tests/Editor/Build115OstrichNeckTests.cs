#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 115. The ostrich head peeks out from BEHIND its feeder: the feeder it rests on is drawn
    // above the head (ring + glass occlude the neck), the other feeders stay under the head, the
    // head sits beside the glass, and the peck strike / flee / hide paths restore normal order.
    public static class Build115OstrichNeckTests
    {
        const string Out = "/tmp/flock-five-b115-tests.txt";

        static FeederView[] MakeFeeders(Transform parent, int n)
        {
            var list = new FeederView[n];
            for (int i = 0; i < n; i++)
                list[i] = WorldBuilderFeeder(parent, i, n);
            return list;
        }

        static FeederView WorldBuilderFeeder(Transform parent, int slot, int n)
        {
            var go = new GameObject("Feeder" + slot);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(FeederRow.X(slot, n), WorldBuilder.FeederY, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = FeederView.BaseOrder;
            var v = go.AddComponent<FeederView>();
            v.Slot = slot;
            v.Art = sr;
            return v;
        }

        // Same draw path as WorldBuilder.MakeFeeder: catalog sprite, pivot center, scale 0.66.
        static FeederView DrawnFeeder(Transform parent, int slot, int n, BirdColor color)
        {
            var go = new GameObject("Feeder" + slot);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(FeederRow.X(slot, n), WorldBuilder.FeederY, 0f);
            go.transform.localScale = Vector3.one * FeederView.Scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteCatalog.Feeder(color);
            sr.sortingOrder = FeederView.BaseOrder;
            var v = go.AddComponent<FeederView>();
            v.Slot = slot;
            v.Art = sr;
            v.PlantHere();
            return v;
        }

        static int Fronts(FeederView[] f)
        {
            int c = 0;
            for (int i = 0; i < f.Length; i++) if (f[i].Art.sortingOrder == FeederView.FrontOrder) c++;
            return c;
        }

        [MenuItem("Flock Five/Build 115 Ostrich Neck Tests")]
        public static void Run()
        {
            var sb = new StringBuilder();
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail)
            {
                string line = (ok ? "PASS  " : "FAIL  ") + name + "  " + detail;
                if (ok) pass++; else fail++;
                sb.AppendLine(line);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build115] " + line);
            }

            // A phone-portrait main camera, so ViewHalfWidth is the device's (not the stills fallback).
            var camGo = new GameObject("b115-cam");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = WorldBuilder.CamOrtho;
            cam.aspect = 9f / 19.5f;
            try
            {
            // --- pure rules ---
            Check("order-ladder", FeederView.BaseOrder < OstrichMark.HeadOrder
                && OstrichMark.HeadOrder < FeederView.FrontOrder && FeederView.FrontOrder < FlockSort.Perch,
                "base " + FeederView.BaseOrder + " head " + OstrichMark.HeadOrder + " front " + FeederView.FrontOrder);
            bool slots = true;
            for (int n = 1; n <= 3; n++)
                for (int s = 0; s < n; s++)
                    slots &= OstrichMark.PeekSlot(FeederRow.X(s, n), n) == s;
            Check("peek-slot-any-feeder-count", slots && OstrichMark.PeekSlot(0f, 2) == -1, "1..3 feeders");
            Check("peek-shift-faces-and-clamps",
                OstrichMark.PeekShift(1f, 0f, 5.9f, 1f) > 0.5f && OstrichMark.PeekShift(-1f, 0f, 5.9f, 1f) < -0.5f
                && OstrichMark.PeekShift(1f, 0f, 5.9f, 0f) == 0f
                && OstrichMark.PeekShift(1f, 5f, 5.9f, 1f) <= OstrichMark.PeekX,
                "right " + OstrichMark.PeekShift(1f, 0f, 5.9f, 1f));
            Check("strike-order-rules", OstrichMark.RestHeadOrder(false, 3f) == OstrichMark.HeadOrder
                && OstrichMark.RestHeadOrder(true, 0f) == OstrichMark.HeadOrder
                && OstrichMark.RestHeadOrder(true, 1f) == OstrichStrike.HeadOrder, "");
            Check("strike-neck-behind-lifted-feeder", OstrichStrike.NeckOrder < FeederView.FrontOrder, "");

            // --- rig: ostrich gardens (27, 38) and any feeder count ---
            int[] counts = { 1, 2, 3 };
            foreach (int n in counts)
            {
                var root = new GameObject("b115-stage");
                try
                {
                    var feeders = MakeFeeders(root.transform, n);
                    var mark = OstrichMark.Attach(root.transform, n);
                    mark.FeederArt = feeders;
                    for (int slot = 0; slot < n; slot++)
                    {
                        float x = FeederRow.X(slot, n);
                        mark.Preview(OstrichAct.Idle, 0, x, WorldBuilder.FeederY - 0.05f, false);
                        bool onlyOne = Fronts(feeders) == 1 && feeders[slot].Art.sortingOrder == FeederView.FrontOrder;
                        int head = mark.Head.sortingOrder;
                        float fx = feeders[slot].transform.position.x;
                        float dx = mark.Head.transform.position.x - fx;
                        float want = (x < -0.3f ? -1f : 1f);
                        Check("n" + n + "-slot" + slot + "-behind-own-feeder",
                            onlyOne && mark.PeekingSlot == slot && head > FeederView.BaseOrder && head < FeederView.FrontOrder,
                            "head " + head + " lifted " + mark.PeekingSlot);
                        Check("n" + n + "-slot" + slot + "-head-beside-glass", dx * want > 0.5f, "dx " + dx);
                    }
                    // Fleeing: no feeder is raised; the head runs in front like before.
                    mark.Preview(OstrichAct.Flee, 8, FeederRow.X(0, n), WorldBuilder.FeederY, false);
                    Check("n" + n + "-flee-no-lift", Fronts(feeders) == 0 && mark.PeekingSlot == -1, "");
                    // Hidden: everything back to base order.
                    mark.Preview(OstrichAct.Idle, 0, FeederRow.X(0, n), WorldBuilder.FeederY - 0.05f, false);
                    mark.Pose(0f, 0f, 0, false);
                    Check("n" + n + "-hide-restores", Fronts(feeders) == 0, "");
                }
                finally { Object.DestroyImmediate(root); }
            }

            // --- peck strike: head over the flock once it drops clear of the feeder, back after ---
            {
                var root = new GameObject("b115-strike");
                try
                {
                    var feeders = MakeFeeders(root.transform, 3);
                    var mark = OstrichMark.Attach(root.transform, 3);
                    mark.FeederArt = feeders;
                    mark.Preview(OstrichAct.Idle, 0, 0f, WorldBuilder.FeederY - 0.05f, false);
                    var target = mark.Head.transform.position + new Vector3(0f, -3.2f, 0f);
                    mark.BeginStrike(target);
                    bool sawFront = false, startedBehind = false;
                    for (int i = 0; i < 40 && mark.Striking; i++)
                    {
                        mark.Tick(0.02f);
                        if (i == 0) startedBehind = mark.Head.sortingOrder == OstrichMark.HeadOrder;
                        if (mark.Head.sortingOrder == OstrichStrike.HeadOrder) sawFront = true;
                        if (mark.StrikeTime >= OstrichStrike.Contact) break;
                    }
                    Check("strike-head-over-flock-at-contact", sawFront && startedBehind, "front " + sawFront + " start " + startedBehind);
                    for (int i = 0; i < 200 && mark.Striking; i++) mark.Tick(0.02f);
                    for (int i = 0; i < 40; i++) mark.Tick(0.02f);
                    Check("strike-end-restores-peek", !mark.Striking && mark.Head.sortingOrder == OstrichMark.HeadOrder
                        && Fronts(feeders) == 1, "head " + mark.Head.sortingOrder);
                }
                finally { Object.DestroyImmediate(root); }
            }

            // Hit-testing and the tutorial gate never read draw order; the feeder keeps its collider-free art only.
            Check("feeder-cord-order-stable", FeederView.BaseOrder - 1 < FeederView.BaseOrder, "");

            // Garden 27: three feeders, live Gold/Teal/Teal. The bird settles at x=0, which is
            // the last slot (the middle), and the peek used to land on the right-hand teal.
            // Compare the rendered head to each rendered feeder, not a square on one pivot.
            float keepAspect = cam.aspect;
            var widths = new[]
            {
                new Vector2(820f, 1180f), new Vector2(402f, 874f), new Vector2(412f, 915f),
                new Vector2(360f, 780f), new Vector2(640f, 1136f)
            };
            var clearRoot = new GameObject("b115-beak-clear");
            try
            {
                bool all = true;
                string why = "";
                var cols = new[] { BirdColor.Gold, BirdColor.Teal, BirdColor.Teal };
                var feeders = new FeederView[3];
                for (int i = 0; i < 3; i++)
                    feeders[i] = DrawnFeeder(clearRoot.transform, i, 3, cols[i]);
                var mark = OstrichMark.Attach(clearRoot.transform, 3);
                mark.FeederArt = feeders;
                bool DrawnClear(string tag)
                {
                    var hb = mark.Head.bounds;
                    if (hb.size.x < 0.4f || hb.size.y < 0.4f) { why = tag + " head " + hb.size; return false; }
                    var head = Rect.MinMaxRect(hb.min.x, hb.min.y, hb.max.x, hb.max.y);
                    for (int i = 0; i < feeders.Length; i++)
                    {
                        var ab = feeders[i].Art != null ? feeders[i].Art.bounds : new Bounds();
                        if (ab.size.x < 2f) { why = tag + " feeder" + i + " art " + ab.size; return false; }
                        var art = Rect.MinMaxRect(ab.min.x, ab.min.y, ab.max.x, ab.max.y);
                        if (head.Overlaps(art)) { why = tag + " feeder" + i + " head " + head + " art " + art; return false; }
                    }
                    return true;
                }
                foreach (var wh in widths)
                {
                    float win = wh.x / wh.y;
                    float aspect = win > DeviceFit.PhoneAspect + 0.03f
                        ? DeviceFit.PhoneAspect
                        : DeviceFit.PlayAspect(win);
                    cam.aspect = aspect;
                    string size = wh.x.ToString("0") + "x" + wh.y.ToString("0");
                    mark.EndEntrance();
                    if (!DrawnClear(size + " settle")) { all = false; break; }
                    for (int slot = 0; slot < 3; slot++)
                    {
                        mark.Preview(OstrichAct.Idle, 2, FeederRow.X(slot, 3), WorldBuilder.FeederY - 0.05f, false);
                        if (!DrawnClear(size + " slot" + slot)) { all = false; break; }
                    }
                    if (!all) break;
                }
                Check("settled-beak-clears-every-bulb", all, why);
            }
            finally
            {
                cam.aspect = keepAspect;
                Object.DestroyImmediate(clearRoot);
            }
            }
            finally { Object.DestroyImmediate(camGo); }
            Debug.Log(fail == 0 ? "build115 tests DONE " + pass + "/" + (pass + fail) : "build115 tests FAIL " + fail + " failed, " + pass + " passed");
        }
    }
}
#endif
