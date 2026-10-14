#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // Every caption-table spot, on iPhone and iPad: one fixed banner size (the adopt
    // greet), at most three lines, and the banner rect stays off the main buttons.
    public static class Build88CaptionTests
    {
        const string Out = "/tmp/flock-five-b88-caption-tests.txt";

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
                Debug.Log("[build88caption] " + line);
            }

            float? keepWindow = DeviceFit.TestWindow;
            try
            {
                Face(Check, "iphone", 1179f, 2556f, new Rect(0f, 102f, 1179f, 2277f));
                Face(Check, "ipad", 1536f, 2048f, new Rect(0f, 20f, 1536f, 2008f));
            }
            catch (System.Exception ex)
            {
                Check("caption", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                DeviceFit.TestWindow = keepWindow;
            }

            string end = fail == 0 ? "BUILD88CAPTION_OK " + pass : "BUILD88CAPTION_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[build88caption] " + end);
            if (fail > 0) throw new System.Exception(end);
        }

        static void Face(System.Action<string, bool, string> check, string tag, float w, float h, Rect safe)
        {
            DeviceFit.TestWindow = w / h;
            float s = DeviceFit.Ui(w, h);
            int adoptPx = CaptionLayout.BannerFont(w, h);
            var style = FlockFiveApp.CoachCaptionStyle();
            var seat = new Rect(w * 0.2f, h * 0.42f, w * 0.6f, 120f);
            var buttons = new Rect[4];
            int bn = FlockFiveApp.FillMainButtons(w, h, safe, buttons);
            var badge = FlockFiveApp.AdoptVipBadgeRect(w, h, safe);
            int seen = 0;
            foreach (var kv in CaptionStyles.Banner)
            {
                seen++;
                string line = LineOf(kv.Key);
                bool adopt = kv.Key == "adopt.greet";
                float inner = CaptionLayout.BannerMeasureWidth(w, style, line, adoptPx);
                var band = FlockFiveApp.BannerRect(kv.Key, line, w, h, safe, seat, 0f, adopt, s, out int px);
                var fit = CaptionLayout.FitBanner(style, line, inner, adoptPx);
                bool size = px == adoptPx && fit.Font == adoptPx;
                bool rows = fit.Lines >= 1 && fit.Lines <= CaptionLayout.BannerMaxLines;
                string hit = "";
                bool clear = bn == 4;
                for (int i = 0; i < bn; i++)
                {
                    if (buttons[i].width > 2f && band.Overlaps(buttons[i]))
                    {
                        clear = false;
                        hit = " btn" + i + band + " vs " + buttons[i];
                    }
                }
                bool wide = true;
                if (CaptionStyles.IsBanner(kv.Key))
                    wide = RowsFit(style, fit.Shown, inner, adoptPx);
                bool place = true;
                if (kv.Key == "bee.garden")
                {
                    var hive = buttons[2];
                    place = !CaptionLayout.WantsBeeIcon(line)
                        && band.center.y < h * 0.45f
                        && hive.width > 2f
                        && band.yMax < hive.yMin - 4f;
                }
                bool cover = !adopt || Covers(band, badge);
                bool ok = size && rows && clear && wide && place && cover;
                check(tag + "-" + kv.Key, ok,
                    "px " + px + "/" + adoptPx + " lines " + fit.Lines
                    + " wide " + wide + " place " + place + " cover " + cover
                    + " [" + (fit.Shown ?? "").Replace("\n", " / ") + "]"
                    + (ok ? "" : Widths(style, fit.Shown, adoptPx))
                    + hit);
            }
            check(tag + "-table",
                seen == CaptionStyles.Banner.Count && seen >= 15,
                "seen " + seen + " table " + CaptionStyles.Banner.Count);

            string[] extra =
            {
                CaptionLayout.AdoptGreetLine,
                CaptionLayout.SparrowLine,
                CaptionLayout.HawkLine,
                CaptionLayout.OstrichLine,
                CaptionLayout.LeafLine,
                CaptionLayout.BeePestLine,
                CaptionLayout.HiveGardenLine,
            };
            for (int i = 0; i < extra.Length; i++)
            {
                float inner = CaptionLayout.BannerMeasureWidth(w, style, extra[i], adoptPx);
                var fit = CaptionLayout.FitBanner(style, extra[i], inner, adoptPx);
                var band = FlockFiveApp.BannerRect("coach.line", extra[i], w, h, safe, seat, 0f, false, s, out int px);
                bool clear = true;
                for (int b = 0; b < bn; b++)
                    if (buttons[b].width > 2f && band.Overlaps(buttons[b])) clear = false;
                bool rowsOk = fit.Lines <= CaptionLayout.BannerMaxLines
                    && RowsFit(style, fit.Shown, inner, adoptPx);
                bool ok = px == adoptPx && fit.Font == adoptPx && rowsOk && clear;
                check(tag + "-line-" + i, ok,
                    "px " + px + " lines " + fit.Lines
                    + " [" + (fit.Shown ?? "").Replace("\n", " / ") + "]"
                    + (ok ? "" : Widths(style, fit.Shown, adoptPx)));
            }
        }

        static string Widths(GUIStyle style, string shown, int font)
        {
            if (string.IsNullOrEmpty(shown)) return "";
            var parts = shown.Split('\n');
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < parts.Length; i++)
                sb.Append(" w").Append(i).Append('=').Append(CaptionLayout.LineWidth(style, parts[i], font).ToString("0"));
            return sb.ToString();
        }

        static bool RowsFit(GUIStyle style, string shown, float maxW, int font)
        {
            if (string.IsNullOrEmpty(shown)) return true;
            var parts = shown.Split('\n');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length == 0) continue;
                if (!CaptionLayout.LineFits(style, parts[i], maxW, font)) return false;
            }
            return true;
        }

        static bool Covers(Rect band, Rect badge)
        {
            return band.xMin <= badge.xMin + 0.5f
                && band.yMin <= badge.yMin + 0.5f
                && band.xMax >= badge.xMax - 0.5f
                && band.yMax >= badge.yMax - 0.5f;
        }

        static string LineOf(string spot)
        {
            switch (spot)
            {
                case "adopt.greet": return CaptionLayout.AdoptGreetLine;
                case "home.intro": return "Tap your bird for a closer look.";
                case "coach.line": return CaptionLayout.SparrowLine;
                case "badger.caption": return BadgerCopy.OpenLater;
                case "home.daily": return "Tap Daily for\nyour bonus!";
                case "daily.claim": return "Tap to claim!";
                case "bee.garden": return CaptionLayout.HiveGardenLine;
                case "home.poker": return "You earned coins!\nTap poker to bet them.";
                case "home.anchored": return "Tap the back button to return home.";
                case "badger.lesson": return BadgerCopy.LessonColumnTop + "\n" + BadgerCopy.LessonColumnBot;
                case "poker.table": return "Pick your bet, then tap Deal.";
                case "album.coach": return CaptionLayout.AlbumHoneyBlock;
                case "offer.clearads": return FlockFiveApp.ClearDoubleLine;
                case "vip.welcome": return "Welcome, VIP!";
                case "ad.hand": return "Tap to unlock your bonus!";
                default: return CaptionLayout.SparrowLine;
            }
        }
    }
}
#endif
