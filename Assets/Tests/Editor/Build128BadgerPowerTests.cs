#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // b128: flat rounded-square power buttons with painted icons, and the 'Your pick.' caption moved off the fighters
    // into its own CaptionStyles seat just above the power row.
    public static class Build128BadgerPowerTests
    {
        const string Out = "/tmp/flock-five-b128-badger-power-tests.txt";

        [MenuItem("Flock Five/Build 128 Badger Power Tests")]
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
                Debug.Log("[build128] " + line);
            }

            // ---- caption: the table entry owns the spot ----
            string pickSpot = FlockFiveApp.BadgerCaptionSpot(BadgerCopy.YourPick);
            Check("pick-spot-from-table", pickSpot == "badger.pick" && CaptionStyles.AbovePower(pickSpot) && CaptionStyles.IsSnug(pickSpot)
                && CaptionStyles.MaxLines(pickSpot) == 2, pickSpot);
            Check("other-lines-keep-banner", FlockFiveApp.BadgerCaptionSpot(BadgerCopy.CoinsShort) == "badger.caption"
                && !CaptionStyles.AbovePower("badger.caption"), "");
            Check("pick-copy-two-lines-max", BadgerCopy.YourPick.Split('\n').Length <= CaptionStyles.MaxLines(pickSpot), BadgerCopy.YourPick.Replace("\n", "/"));

            // ---- caption rect never intersects the fighters, the grids or the power row; sits just above the power row ----
            var sizes = new[] { new Vector2(750, 1334), new Vector2(1170, 2532), new Vector2(1290, 2796),
                new Vector2(375, 667), new Vector2(390, 844), new Vector2(430, 932) };
            foreach (var sz in sizes)
            {
                var safe = new Rect(0, 0, sz.x, sz.y);
                var plate = FlockFiveApp.BadgerPickPlateAt(sz.x, sz.y, safe, BadgerCopy.YourPick, out var boss, out var arena, out var you, out var power,
                    out bool fits, out string detail);
                bool clear = !plate.Overlaps(arena) && !plate.Overlaps(boss) && !plate.Overlaps(you) && !plate.Overlaps(power);
                bool above = plate.yMax <= power.yMin + 0.5f && plate.yMin >= you.yMax - 0.5f && power.yMin - plate.yMax < 0.05f * sz.y;
                bool onScreen = plate.xMin >= 0f && plate.xMax <= sz.x;
                Check("pick-caption-clear-of-fighters-" + sz.x + "x" + sz.y, clear && above && onScreen, detail + " arena " + arena + " you " + you + " power " + power);
                Check("pick-caption-fits-two-lines-" + sz.x + "x" + sz.y, fits, detail);
            }

            // ---- power row: one shared helper, sprites, no pedestal / code-drawn icons ----
            string badger = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Badger.cs");
            string powers = Slice(badger, "void DrawBadgerPowers(", "bool BadgerPowerTile(");
            Check("one-shared-tile-helper", Count(powers, "BadgerPowerTile(") == 1 && !powers.Contains("BadgerButton(rr") && !powers.Contains("DrawPopupButton"), "");
            Check("ice-badge-on-frozen", powers.Contains("if (frozenOut) DrawBadgerIceBadge(rr);"), "");
            string tile = Slice(badger, "bool BadgerPowerTile(", "static Color Fade(");
            Check("tile-flat-no-pedestal", !tile.Contains("DrawPopupButton") && tile.Contains("DrawBadgerPowerIcon("), "");
            Check("no-code-drawn-icons", !badger.Contains("PLACEHOLDER art") && !badger.Contains("spray can: dark outline") && badger.Contains("SpriteCatalog.PowerIcon(p)"), "");
            foreach (var p in new[] { BadgerPower.X2, BadgerPower.X3, BadgerPower.HotSauce, BadgerPower.FreezeSpray })
            {
                var spr = SpriteCatalog.PowerIcon(p);
                Check("icon-sprite-" + p, spr != null && spr.texture != null && spr.texture.width > 32, spr != null ? spr.texture.width + "x" + spr.texture.height : "null");
            }
            Check("names-from-mock", FlockFiveApp.BadgerPowerTileName(BadgerPower.X2) == "Double" && FlockFiveApp.BadgerPowerTileName(BadgerPower.X3) == "Triple"
                && FlockFiveApp.BadgerPowerTileName(BadgerPower.HotSauce) == "Hot Sauce" && FlockFiveApp.BadgerPowerTileName(BadgerPower.FreezeSpray) == "Freeze", "");
            foreach (var n in new[] { "double", "triple", "hotsauce", "freeze" })
            {
                var imp = AssetImporter.GetAtPath("Assets/_Project/Art/Resources/Sprites/fx_power_" + n + ".png") as TextureImporter;
                var refImp = AssetImporter.GetAtPath("Assets/_Project/Art/Resources/Sprites/fx_padlock.png") as TextureImporter;
                bool same = imp != null && refImp != null && imp.textureType == refImp.textureType && imp.spriteImportMode == refImp.spriteImportMode
                    && imp.alphaIsTransparency == refImp.alphaIsTransparency && imp.mipmapEnabled == refImp.mipmapEnabled && imp.filterMode == refImp.filterMode;
                Check("import-like-ui-sprites-" + n, same, imp == null ? "missing" : imp.textureType + " " + imp.spriteImportMode);
            }

            // ---- #170 nits: tight gate rects, 4-arg PressPasses wired into the app ----
            string coach = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Coach.cs");
            Check("gate-uses-4arg-press-passes", coach.Contains("BadgerFrozenUi.PressPasses(e.mousePosition, default, _bgGateToggle, _bgGateFreeze)")
                && coach.Contains("_bgGateFreeze = BadgerGateFreezeRect();"), "");
            Check("gate-no-union-rect", !badger.Contains("one rect over both") && !badger.Contains("Mathf.Min(hs.xMin, fz.xMin)"), "");
            var hot = new Rect(130, 520, 80, 70); var fz = new Rect(218, 520, 80, 70); var between = new Vector2(214, 555);
            Check("gate-gap-between-buttons-eaten", !BadgerFrozenUi.PressPasses(between, default, hot, fz)
                && BadgerFrozenUi.PressPasses(hot.center, default, hot, fz) && BadgerFrozenUi.PressPasses(fz.center, default, hot, fz), "");
            Check("frozen-copy-untouched", BadgerCopy.FreezeSprayFrozen == "Frozen badger.\nFreeze Spray is on hold.", "");

            string end = fail == 0 ? "BUILD128_OK " + pass : "BUILD128_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[build128] " + end);
            if (fail > 0) throw new System.Exception(end);
        }

        // Batch: the badger suites that are not in RunEditorTests (b110, b123, b127) plus this one.
        public static void RunBadgerSet()
        {
            void Suite(string n, System.Action a)
            {
                try { a(); Debug.Log("[badgerset] " + n + " OK"); }
                catch (System.Exception ex) { Debug.LogError("[badgerset] " + n + " THREW " + ex.Message); }
            }
            Suite("build110-dialog", Build110BadgerDialogTests.Run);
            Suite("build110-freeze-frozen", Build110FreezeFrozenTests.Run);
            Suite("build123-tutorial", Build123BadgerTutorialTests.Run);
            Suite("build127", Build127Tests.Run);
            Suite("build128", Run);
            Debug.Log("BADGER_SET_DONE");
        }

        static string Slice(string s, string a, string b)
        {
            int i = s.IndexOf(a); if (i < 0) return "";
            int j = s.IndexOf(b, i + a.Length); if (j < 0) j = s.Length;
            return s.Substring(i, j - i);
        }

        static int Count(string s, string k) { int n = 0, i = 0; while ((i = s.IndexOf(k, i)) >= 0) { n++; i += k.Length; } return n; }
    }
}
#endif
