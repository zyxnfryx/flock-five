#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Bee-card queue: legible dark bees, warm comb rims, one flavor on both
    // faces, a rare moth, a player-run carousel, and a baked flip.
    static class BeeCardQueueTests
    {
        [MenuItem("Flock Five/Bee Card Tests")]
        public static void Run()
        {
            int pass = 0;
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++;
                else fail++;
                Debug.Log("[bee-cards] " + (ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            void Part(string name, System.Action<System.Action<string, bool, string>> body)
            {
                try { body(Check); }
                catch (System.Exception ex)
                {
                    Check(name + "-threw", false, ex.GetType().Name + ": " + ex.Message);
                }
            }

            Part("contrast", Contrast);
            Part("rims", RimsAndInk);
            Part("flavor", Flavor);
            Part("moth", Moth);
            Part("poke", Poke);
            Part("carousel", Carousel);
            Part("bake", Bake);
            Part("sources", Sources);

            Debug.Log("[bee-cards] " + pass + " passed, " + fail + " failed");
            if (fail > 0) Debug.LogError("[bee-cards] FAILED " + fail);
        }

        static void Contrast(System.Action<string, bool, string> check)
        {
            var roster = Hive.Roster;
            int dark = 0;
            int light = 0;
            float bass = 0f;
            for (int i = 0; i < roster.Length; i++)
            {
                var tint = roster[i].Tint;
                var mul = BeeInk.Mul(tint);
                float peak = BeeInk.Peak(mul);
                bool lifted = BeeInk.Peak(tint) < BeeInk.MinPeak;
                if (!lifted)
                {
                    light++;
                    bool same = Mathf.Abs(mul.r - tint.r) < 0.001f
                        && Mathf.Abs(mul.g - tint.g) < 0.001f
                        && Mathf.Abs(mul.b - tint.b) < 0.001f;
                    if (!same)
                        check("ink-keeps-" + roster[i].Id, false, "mul moved a light tint");
                    continue;
                }
                dark++;
                var body = BeeInk.Body(mul);
                var face = CardPlate.Face(tint, true);
                float contrast = BeeInk.Contrast(body, face);
                if (roster[i].Id == "bass") bass = contrast;
                bool ok = peak + 0.001f >= BeeInk.MinPeak && contrast >= 2.4f;
                if (!ok)
                    check("ink-" + roster[i].Id, false, "peak " + peak.ToString("0.00") + " contrast " + contrast.ToString("0.00"));
            }
            check("dark-bees-lifted", dark >= 4 && light >= 4, "dark " + dark + " light " + light);
            check("bass-note-contrast", bass >= 4f, bass.ToString("0.00"));
        }

        static void RimsAndInk(System.Action<string, bool, string> check)
        {
            var finishes = new[] { BeeFinish.Normal, BeeFinish.Holo, BeeFinish.InverseRainbow };
            for (int i = 0; i < finishes.Length; i++)
            {
                var rim = HoneyRim.For(finishes[i]);
                check("rim-" + finishes[i], HoneyRim.Warm(rim), rim.ToString());
            }
            bool differ = HoneyRim.For(BeeFinish.Normal) != HoneyRim.For(BeeFinish.Holo)
                && HoneyRim.For(BeeFinish.Holo) != HoneyRim.For(BeeFinish.InverseRainbow);
            check("rims-differ", differ, "three warm depths");

            var cream = new Color(0.98f, 0.92f, 0.72f, 1f);
            var binder = new Color(1f, 0.98f, 0.90f, 1f);
            var inverse = FinishInk.Tag(BeeFinish.InverseRainbow);
            float onPlate = BeeInk.Contrast(inverse, cream);
            float onBinder = BeeInk.Contrast(inverse, binder);
            check("inverse-on-plate", onPlate >= 4.5f, onPlate.ToString("0.00"));
            check("inverse-on-binder", onBinder >= 4.5f, onBinder.ToString("0.00"));
            check("inverse-label", FinishInk.Label(BeeFinish.InverseRainbow) == "Inverse Rainbow", FinishInk.Label(BeeFinish.InverseRainbow));
            check("holo-label", FinishInk.Label(BeeFinish.Holo) == "Holo", FinishInk.Label(BeeFinish.Holo));
        }

        static void Flavor(System.Action<string, bool, string> check)
        {
            var roster = Hive.Roster;
            int shown = 0;
            int mismatch = 0;
            for (int i = 0; i < roster.Length; i++)
            {
                string front = roster[i].Front ?? "";
                string back = roster[i].Back ?? "";
                string draw = CardCopy.Shown(roster[i]);
                if (front.Length > 0 && draw != front)
                    check("shown-" + roster[i].Id, false, draw);
                else
                    shown++;
                if (front.Length > 0 && back.Length > 0 && front != back)
                    mismatch++;
            }
            check("shown-is-front", shown == roster.Length, shown + "/" + roster.Length);
            var bass = Find("bass");
            check("bass-data-differs", bass.Front != bass.Back, bass.Front + " / " + bass.Back);
            check("bass-both-sides", CardCopy.Shown(bass) == bass.Front, CardCopy.Shown(bass));
            check("roster-still-has-backs", mismatch > 10, mismatch.ToString());
        }

        static void Moth(System.Action<string, bool, string> check)
        {
            int moth = IndexOf("moth");
            int other = moth == 0 ? 1 : 0;
            check("moth-weight", Hive.KindWeight(moth) == 1 && Hive.IsRareKind(moth), Hive.KindWeight(moth).ToString());
            check("common-weight", Hive.KindWeight(other) == 8 && !Hive.IsRareKind(other), Hive.Roster[other].Id);
            int before = 0;
            for (int k = 0; k < moth; k++) before += Hive.KindWeight(k);
            check("moth-at-weight", Hive.KindAtWeight(before, false) == moth, "roll " + before);
            check("weight-sum", Hive.WeightSum(false) == (Hive.Kinds - 1) * 8 + 1, Hive.WeightSum(false).ToString());

            int cx = MothArt.Size / 2;
            int cy = MothArt.Size / 2;
            var body = MothArt.Pixel(cx, cy);
            var wing = MothArt.Pixel(cx - Mathf.RoundToInt(MothArt.Size * 0.28f), cy);
            var corner = MothArt.Pixel(2, 2);
            check("moth-body", body.a > 200, "a " + body.a);
            check("moth-wing", wing.a > 40, "a " + wing.a);
            check("moth-corner", corner.a == 0, "a " + corner.a);

            int left = -1;
            int right = -1;
            int vary = 0;
            byte prev = 0;
            bool have = false;
            for (int x = 0; x < MothArt.Size; x++)
            {
                var p = MothArt.Pixel(x, cy);
                if (p.a < 30) continue;
                if (left < 0) left = x;
                right = x;
                if (have && p.a != prev) vary++;
                prev = p.a;
                have = true;
            }
            int span = right - left;
            check("moth-wide", span > MothArt.Size * 0.45f, "span " + span);
            check("moth-fuzz", vary > 8, "changes " + vary);
        }

        static void Poke(System.Action<string, bool, string> check)
        {
            HoneyPoke.Clear();
            const float now = 10f;
            bool filled = true;
            for (int i = 0; i < HoneyPoke.Cap; i++)
            {
                if (!HoneyPoke.Place(now + i * 0.02f, new Vector2(8f + i, 12f), 20f))
                    filled = false;
            }
            check("poke-five", filled && HoneyPoke.Placed == HoneyPoke.Cap, HoneyPoke.Placed.ToString());
            check("poke-stops", !HoneyPoke.Place(now + 1f, new Vector2(3f, 3f), 20f), "cap holds");
            float mid = HoneyPoke.Alpha(now + HoneyPoke.FadeSec * 0.5f, now);
            check("poke-fade", Mathf.Abs(HoneyPoke.Alpha(now, now) - 1f) < 0.001f
                && Mathf.Abs(mid - 0.75f) < 0.001f
                && HoneyPoke.Alpha(now + HoneyPoke.FadeSec, now) == 0f, mid.ToString("0.00"));
            HoneyPoke.Clear();
            check("poke-clear", HoneyPoke.Placed == 0, HoneyPoke.Placed.ToString());
        }

        static void Carousel(System.Action<string, bool, string> check)
        {
            check("tap-left", HiveCarousel.TapDir(10f, 100f, 4) == -1, "");
            check("tap-center", HiveCarousel.TapDir(50f, 100f, 4) == 0, "");
            check("tap-right", HiveCarousel.TapDir(90f, 100f, 4) == 1, "");
            check("tap-single", HiveCarousel.TapDir(10f, 100f, 1) == 0, "");
            check("nearest-rest", HiveCarousel.Nearest(0.2f, 5) == 0, "");
            check("nearest-wrap", HiveCarousel.Nearest(4.6f, 5) == 0, HiveCarousel.Nearest(4.6f, 5).ToString());
            float wrapped = HiveCarousel.Wrap(4f, 5);
            check("wrap", Mathf.Abs(wrapped - (-1f)) < 0.001f, wrapped.ToString("0.00"));
        }

        static void Bake(System.Action<string, bool, string> check)
        {
            const string title = "Honey Gold";
            const string flavor = "Sweetest sting in the yard and the gold standard of buzzness stays put when the card turns.";
            var wide = new Rect(0f, 0f, 420f, 640f);
            var narrow = new Rect(0f, 0f, 110f, 640f);
            string wideLines = CardFaceBake.FlavorJoined(wide, title, flavor, true, false, false);
            string narrowLines = CardFaceBake.FlavorJoined(narrow, title, flavor, true, false, false);
            check("bake-wide", wideLines.Length > 8, wideLines);
            check("bake-narrow", narrowLines.Length > 8, narrowLines);
            check("bake-reflow", wideLines != narrowLines, "wide and narrow differ");
            var page = CardFaceBake.Build(new CardFaceBake.Request
            {
                Width = 420,
                Height = 640,
                Title = title,
                Flavor = flavor,
                Finish = "Inverse Rainbow",
                Full = true,
                Back = false,
                Honey = false,
                Ink = new Color(0.16f, 0.07f, 0.02f, 1f),
                FinishInk = FinishInk.Tag(BeeFinish.InverseRainbow),
            });
            check("bake-matches", page.FlavorLines == wideLines, page.FlavorLines ?? "");
            int ink = 0;
            if (page.Tex != null)
            {
                var px = page.Tex.GetPixels32();
                for (int i = 0; i < px.Length; i++)
                    if (px[i].a > 40) ink++;
            }
            check("bake-ink", ink > 30, "pixels " + ink);
        }

        static void Sources(System.Action<string, bool, string> check)
        {
            string root = Application.dataPath;
            string app = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.cs"));
            string sfx = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/Sfx.cs"));
            string voice = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/Sfx.VoiceA.cs"));
            check("draw-shown", app.Contains("HiveBackCopy(CardCopy.Shown(kind))") && !app.Contains("HiveBackCopy(kind.Back)"), "one flavor");
            check("draw-critter", app.Contains("CardCritter.Draw("), "critter");
            check("draw-bake", app.Contains("CardFaceBake.Build("), "bake");
            check("draw-finish", app.Contains("FinishInk.Tag(finish)") && app.Contains("FinishInk.Tag(col == 1"), "ink");
            check("draw-poke", app.Contains("HoneyPoke.Place(") && app.Contains("Sfx.Squish()"), "poke");
            check("no-auto", !app.Contains("_levelHiveAuto") && !app.Contains("HiveCarouselStep"), "carousel");
            check("tap-dir", app.Contains("HiveCarousel.TapDir(") && app.Contains("i == centerIx"), "dots");

            int squish = voice.IndexOf("static AudioClip MakeSquish()", System.StringComparison.Ordinal);
            int next = squish < 0 ? -1 : voice.IndexOf("static AudioClip MakeBreak", squish, System.StringComparison.Ordinal);
            string body = squish >= 0 && next > squish ? voice.Substring(squish, next - squish) : "";
            check("squish-tone", body.Contains("150f") && body.Contains("68f") && body.Contains("1.5f")
                && !body.Contains("Soft(") && !body.Contains("Hash("), "body");
            check("squish-lead", sfx.Contains("Random.Range(0.97f, 1.03f)") && sfx.Contains("MarkLead(0.14f"), "lead");
            int warm = sfx.IndexOf("_warmOrder", System.StringComparison.Ordinal);
            int warmEnd = warm < 0 ? -1 : sfx.IndexOf("static int _warmBank", warm, System.StringComparison.Ordinal);
            string warmBody = warm >= 0 && warmEnd > warm ? sfx.Substring(warm, warmEnd - warm) : "";
            check("squish-lazy", warmBody.Length > 0 && !warmBody.Contains("squish") && !warmBody.Contains("Squish"), "not warmed");
        }

        static BeeKind Find(string id)
        {
            int i = IndexOf(id);
            return i >= 0 ? Hive.Roster[i] : default;
        }

        static int IndexOf(string id)
        {
            var roster = Hive.Roster;
            for (int i = 0; i < roster.Length; i++)
                if (roster[i].Id == id) return i;
            return -1;
        }
    }
}
#endif
