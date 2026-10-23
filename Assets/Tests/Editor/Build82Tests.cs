#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 82 pass b82a. Honeycomb copy, card-lesson glove, caption width,
    // gold plates, the bee callout, and forced-glove input.
    static class Build82Tests
    {
        const float W = 1179f;
        const float H = 2556f;

        [MenuItem("Flock Five/Build 82 Tests")]
        public static void Run()
        {
            int pass = 0;
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++;
                else fail++;
                Debug.Log("[build82] " + (ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            CheckHoney(Check);
            CheckGlove(Check);
            CheckCaptions(Check);
            CheckBee(Check);
            CheckGate(Check);
            CheckPress(Check);
            Debug.Log("[build82] " + (fail == 0 ? "BUILD82_OK " + pass : "BUILD82_FAIL " + fail + " passed " + pass));
        }

        static void CheckHoney(System.Action<string, bool, string> Check)
        {
            const string sentence = "Honeycombs show the card's value based on rarity.";
            string block = CaptionLayout.AlbumHoneyBlock;
            bool exact = block.StartsWith(sentence + "\n")
                && block.IndexOf("Tap to flip!") > 0
                && block.IndexOf("Tap outside to put back") > 0;
            string banned = ScanBanned();
            Check("honey-copy", exact && banned.Length == 0,
                exact ? (banned.Length == 0 ? block.Replace("\n", " / ") : banned) : block.Replace("\n", " / "));
        }

        static string ScanBanned()
        {
            string root = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Assets/_Project");
            if (!Directory.Exists(root)) return "missing " + root;
            var files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string text = File.ReadAllText(files[i]);
                if (text.IndexOf("2 normal") >= 0) return "2 normal in " + files[i];
                if (text.IndexOf("Honeycombs are honey") >= 0) return "Honeycombs are honey in " + files[i];
            }
            return "";
        }

        static void CheckGlove(System.Action<string, bool, string> Check)
        {
            var focus = FlockFiveApp.AlbumInspectRect(W, H);
            int[] counts = { 2, 3, 5 };
            bool ok = true;
            string detail = "glove misses combs";
            for (int i = 0; i < counts.Length; i++)
            {
                int count = counts[i];
                var pose = FlockFiveApp.PoseLesson(3, focus, W, H, CaptionLayout.AlbumHoneyBlock, count);
                var cells = new Rect[5];
                int n = FlockFiveApp.CardText.HoneyCells(pose.Honey, count, true, cells);
                bool side = pose.Approach == 0f || pose.Approach == -90f || pose.Approach == 90f;
                bool miss = n == count;
                float edge = 999f;
                for (int c = 0; c < n; c++)
                {
                    if (pose.Glove.Overlaps(cells[c])) miss = false;
                    float dx = Mathf.Min(Mathf.Abs(pose.Aim.x - cells[c].xMin), Mathf.Abs(pose.Aim.x - cells[c].xMax));
                    float dy = Mathf.Abs(pose.Aim.y - cells[c].yMax);
                    float d = Mathf.Abs(pose.Approach) < 1f ? dy : dx;
                    if (d < edge) edge = d;
                }
                if (miss && side && edge <= 6f) continue;
                ok = false;
                detail = "count " + count + " n=" + n + " miss=" + miss + " side=" + side
                    + " edge=" + edge.ToString("0.0") + " ang=" + pose.Approach.ToString("0")
                    + " aim=" + pose.Aim + " glove=" + pose.Glove;
                break;
            }
            Check("honey-glove", ok, detail);
        }

        static void CheckCaptions(System.Action<string, bool, string> Check)
        {
            var lines = FlockFiveApp.PaintedLessonLines();
            var seen = new HashSet<string>();
            int bad = 0;
            string detail = "all plates inside the safe width";
            float safeW = CaptionLayout.SafeTextWidth(W, H);
            float safePlate = CaptionLayout.SafePlateWidth(W, H);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i] ?? "";
                if (line.Length == 0 || !seen.Add(line)) continue;
                var gold = FlockFiveApp.GoldCaption(line, W, H);
                bool bee = line == CaptionLayout.HiveGardenLine;
                bool honey = line == CaptionLayout.AlbumHoneyBlock;
                // b89 inserts a blank row after the first sentence, then the floor font wraps
                // each piece. A gapped caption may use four content lines and still has to
                // stay inside the safe plate. Ungapped lines stay at three.
                int cap = honey ? 5 : (bee ? 4 : 3);
                if (cap < 4 && CaptionLayout.HeadlineGap(line) != line) cap = 4;
                if (!gold.Overflow && gold.PlateW <= safePlate + 0.5f && gold.TextW <= safeW + 0.5f && gold.Lines <= cap)
                    continue;
                bad++;
                string one = "lines=" + gold.Lines + " overflow=" + gold.Overflow
                    + " plate=" + gold.PlateW.ToString("0") + "/" + safePlate.ToString("0")
                    + " text=" + gold.TextW.ToString("0") + "/" + safeW.ToString("0")
                    + " [" + (gold.Shown ?? "").Replace("\n", " / ") + "]";
                detail = bad == 1 ? one : detail + " || " + one;
                if (bad >= 4) break;
            }
            var steps = CoachTap.All;
            for (int i = 0; i < steps.Length && bad < 4; i++)
            {
                string line = CoachTap.Caption(steps[i]) ?? "";
                if (line.Length == 0 || !seen.Add(line)) continue;
                var gold = FlockFiveApp.GoldCaption(line, W, H);
                int cap = CaptionLayout.HeadlineGap(line) != line ? 4 : 3;
                if (!gold.Overflow && gold.PlateW <= safePlate + 0.5f && gold.Lines <= cap) continue;
                bad++;
                detail = (bad == 1 ? "" : detail + " || ") + steps[i] + " " + (gold.Shown ?? "").Replace("\n", " / ");
            }
            bool ok = bad == 0;
            bool daily = false;
            for (int i = 0; i < lines.Length; i++)
                if (lines[i] == "Tap Daily for\nyour bonus!") daily = true;
            Check("caption-safe", ok && daily, detail);
        }

        static void CheckBee(System.Action<string, bool, string> Check)
        {
            string line = CaptionLayout.HiveGardenLine;
            bool copy = line.StartsWith("You found a bee!\n")
                && !CaptionLayout.WantsBeeIcon(line)
                && !CaptionLayout.WantsBeeIcon("Tap Daily for\nyour bonus!");
            float? keep = DeviceFit.TestWindow;
            DeviceFit.TestWindow = W / H;
            var safe = new Rect(0f, 102f, W, H - 102f - 177f);
            FlockFiveApp.GardenCorners(W, H, safe, out _, out var hive);
            float s = DeviceFit.Ui(W, H);
            int adopt = CaptionLayout.BannerFont(W, H);
            var fit = CaptionLayout.FitBanner(FlockFiveApp.CoachCaptionStyle(), line, W * 0.92f, adopt);
            var band = FlockFiveApp.BannerRect("bee.garden", line, W, H, safe, new Rect(0f, H * 0.5f, W * 0.6f, 120f), 0f, false, s, out int px);
            bool placed = px == adopt && fit.Font == adopt && fit.Lines >= 2 && fit.Lines <= CaptionLayout.BannerMaxLines
                && band.width > 8f && !band.Overlaps(hive) && band.yMax < hive.yMin - 4f
                && band.center.y < H * 0.45f;
            var gold = FlockFiveApp.GoldCaption(line, W, H);
            DeviceFit.TestWindow = keep;
            Check("bee-callout", copy && placed && gold.Lines >= 2 && gold.Lines <= 4 && !gold.Overflow,
                "lines=" + fit.Lines + " px=" + px + "/" + adopt + " band=" + band + " hive=" + hive + " gold=" + gold.Lines);
        }

        static void CheckGate(System.Action<string, bool, string> Check)
        {
            var controls = new[]
            {
                HomeControl.Poker, HomeControl.Daily, HomeControl.Vip, HomeControl.Hive, HomeControl.Gear,
            };
            bool blocked = true;
            string detail = "non-targets blocked";
            for (int i = 0; i < controls.Length; i++)
            {
                var press = GloveGate.Resolve(true, HomeControl.Vip, controls[i]);
                bool allow = controls[i] == HomeControl.Vip;
                if (press.AllowPress == allow && press.Navigate == allow && press.Advance == allow) continue;
                blocked = false;
                detail = controls[i] + " press=" + press.AllowPress + " go=" + press.Navigate + " adv=" + press.Advance;
                break;
            }
            for (int i = 0; i < controls.Length && blocked; i++)
            {
                var press = GloveGate.Resolve(true, HomeControl.None, controls[i]);
                if (!press.AllowPress && !press.Navigate && !press.Advance) continue;
                blocked = false;
                detail = "none-target " + controls[i];
            }
            var poker = GloveGate.Resolve(true, HomeControl.Poker, HomeControl.Poker);
            var idle = GloveGate.Resolve(false, HomeControl.Poker, HomeControl.Daily);
            Check("glove-gate",
                blocked && poker.AllowPress && poker.Navigate && poker.Advance
                && idle.AllowPress && idle.Navigate && !idle.Advance,
                detail + " poker=" + poker.AllowPress + "/" + poker.Navigate + "/" + poker.Advance);
        }

        static void CheckPress(System.Action<string, bool, string> Check)
        {
            SplashPress.ResetBoard();
            var hit = new SplashPress.Hit();
            hit.Rect = new Rect(40f, 80f, 120f, 120f);
            var began = new List<SplashPress.Sample>
            {
                new SplashPress.Sample { TouchId = 7, Pos = hit.Rect.center, Phase = SplashPress.Phase.Began },
            };
            var fresh = new SplashPress.Gesture();
            var denied = fresh.Apply(hit, true, began, true, true, 40);
            bool fromStart = !denied.Pressed && !denied.Launch;

            SplashPress.ResetBoard();
            var live = new SplashPress.Gesture();
            var armed = live.Apply(hit, false, began, true, true, 41);
            var held = new List<SplashPress.Sample>
            {
                new SplashPress.Sample { TouchId = 7, Pos = hit.Rect.center, Phase = SplashPress.Phase.Held },
            };
            var dropped = live.Apply(hit, true, held, false, true, 42);
            bool mid = armed.Pressed && !armed.Launch && !dropped.Pressed && !dropped.Launch;
            Check("glove-press", fromStart && mid,
                "start=" + denied.Pressed + "/" + denied.Launch
                + " armed=" + armed.Pressed + " dropped=" + dropped.Pressed + "/" + dropped.Launch);
        }
    }
}
#endif
