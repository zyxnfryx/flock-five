#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Title and flavor bands stay apart, and the wrapped copy fits each band
    // at a grid card and at a fullscreen inspect card. Includes Static Wing.
    // Thumbnail pass: every catalog name, plus "Inverse Rainbow", fits the
    // album grid's name rect on 1170×2532 and 750×1334 (outline included).
    // Descenders: every name, flavor, and back description, at thumbnail and
    // inspect size, keeps lines * line height + descender + outline inside
    // the rect. Neon Nectar's flavor ends in "grind."
    // Run from the menu, or drop a file on /tmp/flock-five-card-fit.
    [InitializeOnLoad]
    static class CardTextFitTests
    {
        const string Cmd = "/tmp/flock-five-card-fit";
        const string Out = "/tmp/flock-five-card-fit.txt";

        static CardTextFitTests()
        {
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (!File.Exists(Cmd) || EditorApplication.isCompiling) return;
            try { File.Delete(Cmd); }
            catch { return; }
            Run();
        }

        [MenuItem("Flock Five/Card Text Fit Tests")]
        public static void Run()
        {
            var sb = new StringBuilder();
            int pass = 0;
            int fail = 0;
            void Check(string name, bool ok)
            {
                if (ok) pass++; else fail++;
                string line = (ok ? "PASS  " : "FAIL  ") + name;
                sb.AppendLine(line);
                Debug.Log("[card-fit] " + line);
            }

            // GUI.skin throws outside OnGUI, and -executeMethod is not OnGUI.
            // Probe measures with its own style, including the built-in font.

            // Mid grid card, fullscreen inspect, and a half-scale flip.
            var faces = new Rect[]
            {
                new Rect(0f, 0f, 220f, 320f),
                new Rect(0f, 0f, 420f, 600f),
                new Rect(0f, 0f, 780f, 1100f),
                new Rect(0f, 0f, 390f, 1100f)
            };
            string[] titles =
            {
                "Static Wing",
                "Distracted Wing",
                "Spaghetti Nest",
                "Phoenix Pollen"
            };
            string[] flavors =
            {
                "Charged personality.",
                "Light precipitation, heavy opinions.",
                "Faces the light. Judging you.",
                "Essential oils, optional attitude. Smells like calm; acts like overtime."
            };

            for (int f = 0; f < faces.Length; f++)
            {
                bool full = f >= 2;
                for (int i = 0; i < titles.Length; i++)
                {
                    string tag = (full ? "full " : "mid ") + faces[f].width + " " + titles[i];
                    bool front = FlockFiveApp.CardText.Probe(faces[f], titles[i], flavors[i], full, false, out var titleR, out var flavorR);
                    Check(tag + " front", front && !titleR.Overlaps(flavorR) && titleR.height > 4f && flavorR.height > 4f);
                    bool back = FlockFiveApp.CardText.Probe(faces[f], titles[i], flavors[i], full, true, out var bt, out var bf);
                    Check(tag + " back", back && !bt.Overlaps(bf) && bt.yMax <= bf.y + 0.5f);
                }
            }

            RankNames(sb);

            // iPhone 14 pixel size (1170×2532, 47pt / 34pt insets at 3x) and a small
            // phone (750×1334, 20pt status bar at 2x). Full-bleed is the editor game
            // view with no safe area. Width does not use the inset; height does.
            CheckPhone(Check, sb, "1170x2532 notch", 1170f, 2532f, Safe(1170f, 2532f, 141f, 102f));
            CheckPhone(Check, sb, "1170x2532 full", 1170f, 2532f, new Rect(0f, 0f, 1170f, 2532f));
            CheckPhone(Check, sb, "750x1334 status", 750f, 1334f, Safe(750f, 1334f, 40f, 0f));
            CheckPhone(Check, sb, "750x1334 full", 750f, 1334f, new Rect(0f, 0f, 750f, 1334f));

            sb.AppendLine(fail == 0 ? "ALL OK  " + pass : "FAILED  " + fail + "  passed " + pass);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[card-fit] " + (fail == 0 ? "ALL OK " + pass : "FAILED " + fail));
        }

        static Rect Safe(float w, float h, float top, float bot)
        {
            return new Rect(0f, bot, w, Mathf.Max(2f, h - top - bot));
        }

        static void RankNames(StringBuilder sb)
        {
            var st = FlockFiveApp.CardText.MeasureStyle();
            st.wordWrap = false;
            st.fontSize = 48;
            var roster = Hive.Roster;
            var order = new int[roster.Length];
            var width = new float[roster.Length];
            for (int i = 0; i < roster.Length; i++)
            {
                order[i] = i;
                width[i] = st.CalcSize(new GUIContent(roster[i].Name)).x;
            }
            for (int i = 0; i < order.Length; i++)
            {
                int best = i;
                for (int j = i + 1; j < order.Length; j++)
                    if (width[order[j]] > width[order[best]]) best = j;
                int tmp = order[i];
                order[i] = order[best];
                order[best] = tmp;
            }
            int n = order.Length < 10 ? order.Length : 10;
            for (int i = 0; i < n; i++)
            {
                string line = "WIDE  " + (i + 1) + "  " + roster[order[i]].Name + "  " + width[order[i]].ToString("0.0");
                sb.AppendLine(line);
                Debug.Log("[card-fit] " + line);
            }
            float inv = st.CalcSize(new GUIContent("Inverse Rainbow")).x;
            string foil = "WIDE  finish  Inverse Rainbow  " + inv.ToString("0.0");
            sb.AppendLine(foil);
            Debug.Log("[card-fit] " + foil);
        }

        static void CheckPhone(System.Action<string, bool> check, StringBuilder sb, string phone, float w, float h, Rect safe)
        {
            FlockFiveApp.CardText.Thumb(w, h, safe, out var grid, out var card, out var title, out var finish);
            float s = Mathf.Max(h / 720f, 1f);
            FlockFiveApp.CardText.TitleFitArgs(false, out _, out float titleLead);
            FlockFiveApp.CardText.FinishFitArgs(false, s, out _, out _, out float finishLead);
            string size = grid.CardW.ToString("0.0") + "x" + grid.CardH.ToString("0.0")
                + " title " + title.width.ToString("0.0") + "x" + title.height.ToString("0.0")
                + " finish " + finish.width.ToString("0.0") + "x" + finish.height.ToString("0.0");
            sb.AppendLine("THUMB " + phone + "  " + size);
            Debug.Log("[card-fit] THUMB " + phone + "  " + size);

            var roster = Hive.Roster;
            string bad = null;
            for (int i = 0; i < roster.Length; i++)
            {
                var fit = FlockFiveApp.CardText.FitTitle(title, roster[i].Name, false);
                if (Holds(title, fit, roster[i].Name, titleLead)) continue;
                bad = roster[i].Name;
                break;
            }
            check(phone + " every title " + roster.Length, bad == null);
            if (bad != null) Debug.Log("[card-fit] FAIL title " + phone + " " + bad);

            var inverseTitle = FlockFiveApp.CardText.FitTitle(title, "Inverse Rainbow", false);
            check(phone + " Inverse Rainbow title", Holds(title, inverseTitle, "Inverse Rainbow", titleLead));
            LogFit(sb, phone + " title Inverse Rainbow", inverseTitle);

            var inverse = FlockFiveApp.CardText.FitFinish(finish, "Inverse Rainbow", false, s);
            check(phone + " Inverse Rainbow finish", Holds(finish, inverse, "Inverse Rainbow", finishLead));
            LogFit(sb, phone + " finish Inverse Rainbow", inverse);

            var holo = FlockFiveApp.CardText.FitFinish(finish, "Holo", false, s);
            check(phone + " Holo finish", Holds(finish, holo, "Holo", finishLead));

            var column = FlockFiveApp.CardText.ColumnBand(grid, 2);
            var colFit = FlockFiveApp.CardText.FitColumn(column, "Inverse Rainbow", s);
            check(phone + " column Inverse Rainbow", Holds(column, colFit, "Inverse Rainbow", 1.05f));

            var inspect = FlockFiveApp.CardText.InspectCard(w, h);
            var inspectCard = new Rect(0f, 0f, inspect.x, inspect.y);
            var inspectTitle = FlockFiveApp.CardText.TitleOf(inspectCard, true);
            FlockFiveApp.CardText.TitleFitArgs(true, out _, out float inspectLead);
            string inspectBad = null;
            for (int i = 0; i < roster.Length; i++)
            {
                var fit = FlockFiveApp.CardText.FitTitle(inspectTitle, roster[i].Name, true);
                if (Holds(inspectTitle, fit, roster[i].Name, inspectLead)) continue;
                inspectBad = roster[i].Name;
                break;
            }
            check(phone + " inspect every title", inspectBad == null);
            if (inspectBad != null) Debug.Log("[card-fit] FAIL inspect " + phone + " " + inspectBad);

            var inspectFinish = FlockFiveApp.CardText.FinishBand(inspectCard, true);
            FlockFiveApp.CardText.FinishFitArgs(true, s, out _, out _, out float inspectFinishLead);
            var inspectInverse = FlockFiveApp.CardText.FitFinish(inspectFinish, "Inverse Rainbow", true, s);
            check(phone + " inspect Inverse Rainbow", Holds(inspectFinish, inspectInverse, "Inverse Rainbow", inspectFinishLead));

            string thumbMiss = FirstCopyMiss(card, false);
            check(phone + " thumb name flavor description", thumbMiss == null);
            if (thumbMiss != null) Debug.Log("[card-fit] FAIL copy " + phone + " thumb " + thumbMiss);

            string inspectMiss = FirstCopyMiss(inspectCard, true);
            check(phone + " inspect name flavor description", inspectMiss == null);
            if (inspectMiss != null) Debug.Log("[card-fit] FAIL copy " + phone + " inspect " + inspectMiss);

            bool neonThumb = NeonGrind(card, false, out var neonThumbFit, out string neonThumbWhy);
            check(phone + " Neon Nectar grind thumb", neonThumb);
            if (neonThumb) LogFit(sb, phone + " Neon Nectar flavor thumb", neonThumbFit);
            else Debug.Log("[card-fit] FAIL neon " + phone + " thumb " + neonThumbWhy);

            bool neonInspect = NeonGrind(inspectCard, true, out var neonInspectFit, out string neonInspectWhy);
            check(phone + " Neon Nectar grind inspect", neonInspect);
            if (neonInspect) LogFit(sb, phone + " Neon Nectar flavor inspect", neonInspectFit);
            else Debug.Log("[card-fit] FAIL neon " + phone + " inspect " + neonInspectWhy);
        }

        // Every roster name, front flavor, and back description. Honey shortens the
        // back flavor; both that band and the full band have to hold the tail.
        static string FirstCopyMiss(Rect card, bool full)
        {
            string front = CopyPass(card, full, false, false);
            if (front != null) return front;
            string back = CopyPass(card, full, true, false);
            if (back != null) return back;
            return CopyPass(card, full, true, true);
        }

        static string CopyPass(Rect card, bool full, bool back, bool honey)
        {
            FlockFiveApp.CardText.CopyBands(card, full, back, honey, out var titleR, out var flavorR);
            FlockFiveApp.CardText.CopyArgs(full, back, out _, out float titleLead, out _, out float flavorLead, out _);
            var roster = Hive.Roster;
            for (int i = 0; i < roster.Length; i++)
            {
                string copy = back ? FlockFiveApp.HiveBackCopy(roster[i].Back) : roster[i].Front;
                FlockFiveApp.CardText.FitCopy(titleR, flavorR, roster[i].Name, copy, full, back, out var title, out var flavor);
                if (!honey)
                {
                    if (!Clears(titleR, title, roster[i].Name, titleLead, out string why))
                        return roster[i].Name + " name " + why;
                }
                if (string.IsNullOrEmpty(copy)) continue;
                if (!Clears(flavorR, flavor, copy, flavorLead, out string flavorWhy))
                    return roster[i].Name + (back ? (honey ? " honey " : " back ") : " flavor ") + flavorWhy;
            }
            return null;
        }

        static bool NeonGrind(Rect card, bool full, out FlockFiveApp.CardText.Fitted flavor, out string why)
        {
            flavor = default;
            why = "missing";
            var roster = Hive.Roster;
            BeeKind kind = default;
            bool found = false;
            for (int i = 0; i < roster.Length; i++)
            {
                if (roster[i].Name != "Neon Nectar") continue;
                kind = roster[i];
                found = true;
                break;
            }
            if (!found) return false;
            FlockFiveApp.CardText.CopyBands(card, full, false, false, out var titleR, out var flavorR);
            FlockFiveApp.CardText.CopyArgs(full, false, out _, out float titleLead, out _, out float flavorLead, out _);
            FlockFiveApp.CardText.FitCopy(titleR, flavorR, kind.Name, kind.Front, full, false, out var title, out flavor);
            if (!Clears(titleR, title, kind.Name, titleLead, out why)) return false;
            if (!Clears(flavorR, flavor, kind.Front, flavorLead, out why)) return false;
            bool grind = false;
            if (flavor.Lines != null)
            {
                for (int i = 0; i < flavor.Lines.Length; i++)
                {
                    if (flavor.Lines[i] != null && flavor.Lines[i].EndsWith("grind.")) grind = true;
                }
            }
            if (!grind)
            {
                why = "no grind. line";
                return false;
            }
            float pad = FlockFiveApp.CardText.DescenderPad(flavor.Size);
            if (flavor.Descender + 0.01f < pad)
            {
                why = "descender " + flavor.Descender.ToString("0.00") + " < " + pad.ToString("0.00");
                return false;
            }
            why = null;
            return true;
        }

        // lines * line height + descender + outline (both rims) inside the rect.
        // The painted step repeats the tail on every line, which is at least that.
        static bool Clears(Rect rect, FlockFiveApp.CardText.Fitted fit, string text, float lead, out string why)
        {
            why = "not ok";
            if (!fit.Ok || fit.Lines == null || fit.Lines.Length == 0 || fit.Size < 1) return false;
            if (!Intact(text.Replace('\n', ' '), fit.Lines))
            {
                why = "words";
                return false;
            }
            if (fit.Outline < 1)
            {
                why = "ink";
                return false;
            }
            int n = fit.Lines.Length;
            float measureW = Mathf.Max(1f, rect.width - fit.Outline * 2f - 1f);
            FlockFiveApp.CardText.MeasureCopy(fit.Size, lead, fit.Lines, measureW, out float lineH, out float desc);
            if (Mathf.Abs(fit.Descender - desc) > 0.51f)
            {
                why = "descender drift";
                return false;
            }
            if (Mathf.Abs(fit.LineStep - (lineH + desc)) > 0.51f)
            {
                why = "step drift";
                return false;
            }
            if (Mathf.Abs(fit.BlockH - fit.LineStep * n) > 0.51f)
            {
                why = "block drift";
                return false;
            }
            float spec = n * lineH + desc + fit.Outline * 2f;
            float painted = n * fit.LineStep + fit.Outline * 2f;
            if (spec > rect.height + 0.05f)
            {
                why = "spec " + spec.ToString("0.0") + " > " + rect.height.ToString("0.0");
                return false;
            }
            if (painted > rect.height + 0.05f)
            {
                why = "painted " + painted.ToString("0.0") + " > " + rect.height.ToString("0.0");
                return false;
            }
            if (fit.MaxLineW + fit.Outline * 2f > rect.width + 0.05f)
            {
                why = "width";
                return false;
            }
            bool tail = FlockFiveApp.CardText.HasDescender(text);
            if (tail)
            {
                float pad = FlockFiveApp.CardText.DescenderPad(fit.Size);
                if (fit.Descender + 0.01f < pad)
                {
                    why = "pad";
                    return false;
                }
            }
            else if (fit.Descender > 0.01f)
            {
                why = "unexpected tail";
                return false;
            }
            why = null;
            return true;
        }

        static void LogFit(StringBuilder sb, string tag, FlockFiveApp.CardText.Fitted fit)
        {
            string lines = fit.Lines == null ? "" : string.Join(" / ", fit.Lines);
            string line = "FIT   " + tag + "  size " + fit.Size + "  ink " + fit.Outline
                + "  " + (fit.Ok ? "ok" : "NO") + "  [" + lines + "]";
            sb.AppendLine(line);
            Debug.Log("[card-fit] " + line);
        }

        // Independent of Fitted.Ok: real CalcSize / CalcHeight, plus the outline,
        // must sit inside the rect, and every source word must be whole.
        static bool Holds(Rect rect, FlockFiveApp.CardText.Fitted fit, string name, float lead)
        {
            if (!fit.Ok || fit.Lines == null || fit.Lines.Length == 0 || fit.Size < 1) return false;
            if (!Intact(name, fit.Lines)) return false;
            if (fit.Outline < 1) return false;
            var st = FlockFiveApp.CardText.MeasureStyle();
            st.fontSize = fit.Size;
            st.wordWrap = false;
            float ink = fit.Outline;
            float innerW = rect.width - ink * 2f;
            float innerH = rect.height - ink * 2f;
            if (innerW < 2f || innerH < 2f) return false;
            float maxW = 0f;
            float step = 0f;
            for (int i = 0; i < fit.Lines.Length; i++)
            {
                var content = new GUIContent(fit.Lines[i]);
                st.wordWrap = false;
                var sz = st.CalcSize(content);
                if (sz.x > maxW) maxW = sz.x;
                st.wordWrap = true;
                float ch = st.CalcHeight(content, Mathf.Max(1f, innerW - 1f));
                float line = sz.y;
                if (ch > line) line = ch;
                float led = fit.Size * lead;
                if (led > line) line = led;
                if (line > step) step = line;
            }
            float block = step * fit.Lines.Length;
            if (maxW + ink * 2f > rect.width + 0.05f) return false;
            if (block + ink * 2f > rect.height + 0.05f) return false;
            return true;
        }

        static bool Intact(string name, string[] lines)
        {
            var src = name.Split(' ');
            int k = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrEmpty(lines[i])) return false;
                var parts = lines[i].Split(' ');
                for (int j = 0; j < parts.Length; j++)
                {
                    if (parts[j].Length == 0) continue;
                    while (k < src.Length && src[k].Length == 0) k++;
                    if (k >= src.Length || parts[j] != src[k]) return false;
                    k++;
                }
            }
            while (k < src.Length && src[k].Length == 0) k++;
            return k == src.Length;
        }
    }
}
#endif
