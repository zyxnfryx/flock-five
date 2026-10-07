using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FlockFive
{
    public sealed partial class FlockFiveApp
    {
        // Shared card type for the album grid (mid) and the fullscreen inspect (full).
        public static class CardText
        {
            public struct Scale
            {
                public float Title;
                public float Flavor;
                public float Status;
            }

            public static readonly Scale Mid = new Scale { Title = 1.55f, Flavor = 1.50f, Status = 1.40f };
            // Inspect flavor stays close to the title. The band height, not a tiny cap, keeps it in.
            public static readonly Scale Full = new Scale { Title = 1.85f, Flavor = 1.70f, Status = 1.30f };

            static readonly List<string> _lines = new List<string>(6);
            static readonly List<string> _audit = new List<string>(8);
            static GUIStyle _style;

            public delegate void StarDraw(Rect rect, int stars, float s);

            public static void DrawFront(Rect face, string title, string flavor, bool full, float s)
            {
                var scale = full ? Full : Mid;
                Split(face, false, scale, out var titleR, out var flavorR, out _, out _, out _);
                var ink = new Color(0.16f, 0.07f, 0.02f, 1f);
                // Name first, then flavor under it. Flavor hi is capped to the title size so
                // a tall inspect flavor band cannot outrank the name (shared for grid + inspect).
                int titlePx = DrawBlock(titleR, title, scale.Title, true, full ? 22 : 13, ink, full ? 1.12f : 1.15f);
                int flavorFloor = full ? 16 : 11;
                int flavorHi = titlePx > 0
                    ? Mathf.Max(flavorFloor, Mathf.RoundToInt(titlePx * (full ? 0.92f : 0.84f)))
                    : 0;
                DrawBlock(flavorR, flavor, scale.Flavor, false, flavorFloor, ink, full ? 1.12f : 1.15f, flavorHi);
            }

            // Flip side: name, flavor, then honeycombs only (2 / 3 / 5 by finish).
            // No text chips, stars, or Owned line — those fought the flavor on inspect.
            public static void DrawBack(
                Rect face, string title, string flavor, int honey, Color tint, BeeFinish finish,
                bool full, float s)
            {
                var scale = full ? Full : Mid;
                bool showHoney = honey > 0;
                Split(face, showHoney, scale, out var titleR, out var flavorR, out var honeyR, out _, out _);
                var ink = new Color(0.16f, 0.07f, 0.02f, 1f);
                int titlePx = DrawBlock(titleR, title, scale.Title, true, full ? 20 : 12, ink, full ? 1.10f : 1.1f);
                int flavorFloor = full ? 15 : 11;
                int flavorHi = titlePx > 0
                    ? Mathf.Max(flavorFloor, Mathf.RoundToInt(titlePx * (full ? 0.92f : 0.84f)))
                    : 0;
                DrawBlock(flavorR, flavor, scale.Flavor, false, flavorFloor, new Color(0.18f, 0.08f, 0.03f, 1f), full ? 1.12f : 1.15f, flavorHi);
                if (showHoney) DrawHoneyRow(honeyR, honey, tint, finish, full);
            }

            // Shared honeycomb attributes: N hex tiles from HoneyOfFinish (2/3/5).
            // HoneyArt is the one juicy cell (cards, badger tiles, the meter).
            public static void DrawHoneyRow(Rect zone, int honey, Color tint, BeeFinish finish, bool full)
            {
                if (honey <= 0 || zone.height < 8f || zone.width < 8f) return;
                int n = honey > 5 ? 5 : honey;
                float gap = Mathf.Max(3f, zone.width * 0.02f);
                float cellH = zone.height * (full ? 0.96f : 0.92f);
                float cellW = cellH * (96f / 111f) * 0.78f;
                float rowW = n * cellW + (n - 1) * gap;
                if (rowW > zone.width)
                {
                    float k = zone.width / rowW;
                    cellW *= k;
                    cellH *= k;
                    gap *= k;
                    rowW = n * cellW + (n - 1) * gap;
                }
                float x = zone.center.x - rowW * 0.5f;
                float y = zone.center.y - cellH * 0.5f;
                Color rim = finish == BeeFinish.Holo ? new Color(0.15f, 0.48f, 0.92f, 1f)
                    : finish == BeeFinish.InverseRainbow ? new Color(0.82f, 0.22f, 0.68f, 1f)
                    : AlbumWood(tint, true);
                // Drips live inside the cell (bottom 22%), so they stay in the honey zone.
                // The full inspect stretches a drip on a 3s loop. The grid stays still.
                float now = full ? Time.unscaledTime : 0f;
                for (int i = 0; i < n; i++)
                {
                    var hex = new Rect(x + i * (cellW + gap), y, cellW, cellH);
                    HoneyArt.DrawJuicyCell(hex, rim, now, 1f, i, true, full, true);
                }
            }

            // Legacy text chips kept for any non-album caller; empty slots draw nothing.
            public static void DrawAttrs(Rect zone, string[] attrs, bool full)
            {
                if (attrs == null || attrs.Length == 0 || zone.height < 8f) return;
                int n = 0;
                for (int i = 0; i < attrs.Length && i < 4; i++)
                    if (!string.IsNullOrEmpty(attrs[i])) n++;
                if (n <= 0) return;
                float gap = Mathf.Max(4f, zone.width * 0.03f);
                float w = (zone.width - gap * (n - 1)) / n;
                var st = Style();
                st.alignment = TextAnchor.MiddleCenter;
                st.wordWrap = false;
                int slot = 0;
                for (int i = 0; i < attrs.Length && i < 4; i++)
                {
                    if (string.IsNullOrEmpty(attrs[i])) continue;
                    var r = new Rect(zone.x + slot * (w + gap), zone.y + zone.height * 0.08f, w, zone.height * 0.84f);
                    GUI.color = new Color(0.22f, 0.12f, 0.05f, 0.16f);
                    GUI.DrawTexture(r, Texture2D.whiteTexture);
                    GUI.color = new Color(0.45f, 0.28f, 0.12f, 0.85f);
                    GUI.DrawTexture(new Rect(r.x, r.y, r.width, 2f), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                    int hi = Mathf.Max(full ? 14 : 10, Mathf.RoundToInt(r.height * 0.42f));
                    st.fontSize = FitFont(st, attrs[i], r.width * 0.86f, r.height * 0.72f, full ? 11 : 9, hi);
                    StampOutlined(r, attrs[i], st, new Color(0.20f, 0.10f, 0.04f, 1f), 0, 2);
                    slot++;
                }
            }

            static string[] _honeyDigits;

            // The battle tile number. Same ink as the attribute chips (0.20, 0.10, 0.04) with a
            // cream edge so it reads on any tint. Two labels, no outline ring: sixteen to thirty-two
            // of these draw every frame. Used by every honeycomb tile, in the grids and the arena.
            public static void DrawHoneyDigit(Rect box, int honey, float alpha = 1f)
            {
                if (_honeyDigits == null)
                {
                    _honeyDigits = new string[41];
                    for (int i = 0; i < _honeyDigits.Length; i++) _honeyDigits[i] = i.ToString();
                }
                string text = honey >= 0 && honey < _honeyDigits.Length ? _honeyDigits[honey] : honey.ToString();
                DrawHoneyDigit(box, text, alpha);
            }

            public static void DrawHoneyDigit(Rect box, string text, float alpha = 1f)
            {
                if (string.IsNullOrEmpty(text) || box.width < 4f || box.height < 4f || alpha < 0.04f) return;
                var st = Style();
                st.alignment = TextAnchor.MiddleCenter;
                st.wordWrap = false;
                st.fontSize = FitFont(st, text, box.width * 0.86f, box.height * 0.86f, 10, 220);
                float lift = Mathf.Max(1.5f, st.fontSize * 0.04f);
                Paint(st, new Color(1f, 0.96f, 0.80f, 0.85f * alpha));
                GUI.Label(new Rect(box.x + lift, box.y + lift, box.width, box.height), text, st);
                Paint(st, new Color(0.20f, 0.10f, 0.04f, alpha));
                GUI.Label(box, text, st);
            }

            // Cards whose flavor exceeds 3 lines at the preferred size, before the last-resort shrink.
            public static string FlavorAudit(float width, int fontSize)
            {
                _audit.Clear();
                var roster = Hive.Roster;
                if (roster == null) return "";
                var st = Style();
                st.wordWrap = true;
                st.fontSize = Mathf.Max(10, fontSize);
                for (int i = 0; i < roster.Length; i++)
                {
                    string flavor = HiveBackCopy(roster[i].Back);
                    if (string.IsNullOrEmpty(flavor)) continue;
                    int lines = LineCount(st, flavor, width);
                    if (lines <= 3) continue;
                    _audit.Add(roster[i].Name + " (" + lines + ")");
                }
                if (_audit.Count == 0) return "";
                var sb = new StringBuilder();
                for (int i = 0; i < _audit.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(_audit[i]);
                }
                return sb.ToString();
            }

            static void Split(
                Rect face, bool attrs, Scale scale,
                out Rect titleR, out Rect flavorR, out Rect attrR, out Rect starR, out Rect ownR)
            {
                // 8% inner padding. Title (2 lines) and flavor (3 lines) are separate bands.
                float padX = face.width * 0.08f;
                float x = face.x + padX;
                float w = face.width - padX * 2f;
                float topPad = face.height * 0.05f;
                float y = face.y + topPad;
                float titleH = face.height * 0.34f;
                titleR = new Rect(x, y, w, titleH);
                float bot = face.yMax - face.height * 0.04f;
                ownR = new Rect(x, bot, w, 0f);
                starR = new Rect(x, bot, w, 0f);
                float gap = face.height * 0.03f;
                float midTop = titleR.yMax + gap;
                float midBot = bot;
                if (!attrs)
                {
                    float flavorH = Mathf.Max(8f, midBot - midTop);
                    flavorR = new Rect(x, midTop, w, flavorH);
                    attrR = new Rect(x, midBot, w, 0f);
                    return;
                }
                // Honey owns the bottom band. Flavor stops above it, with a gap.
                float attrH = Mathf.Clamp(face.height * 0.24f, 18f, Mathf.Max(18f, midBot - midTop - gap));
                attrR = new Rect(x, midBot - attrH, w, attrH);
                float flavorH2 = Mathf.Max(8f, attrR.y - gap - midTop);
                flavorR = new Rect(x, midTop, w, flavorH2);
            }

            // EditMode probe. Bands do not intersect. Wrapped text fits each band at this rect.
            public static bool Probe(Rect face, string title, string flavor, bool full, bool back,
                out Rect titleR, out Rect flavorR)
            {
                var scale = full ? Full : Mid;
                Split(face, back, scale, out titleR, out flavorR, out _, out _, out _);
                var st = Style();
                int titlePx = FitBlock(st, titleR, title, true, full ? 12 : 10, full ? 1.10f : 1.12f, 0);
                int flavorFloor = full ? 12 : 10;
                int flavorHi = titlePx > 0 ? Mathf.Max(flavorFloor, Mathf.RoundToInt(titlePx * (full ? 0.92f : 0.84f))) : 0;
                int flavorPx = FitBlock(st, flavorR, flavor, false, flavorFloor, full ? 1.12f : 1.15f, flavorHi);
                if (titleR.Overlaps(flavorR)) return false;
                return BlockFits(st, titleR, title, titlePx, true, full ? 1.10f : 1.12f)
                    && BlockFits(st, flavorR, flavor, flavorPx, false, full ? 1.12f : 1.15f);
            }

            // Returns the fitted font size (0 if nothing drew). Optional hiCap keeps flavor
            // under the name on tall inspect bands without a separate draw path.
            static int DrawBlock(Rect box, string text, float scale, bool title, int floor, Color ink, float lead, int hiCap = 0)
            {
                if (box.height < 4f || string.IsNullOrEmpty(text)) return 0;
                var st = Style();
                st.wordWrap = false;
                // Title: top of its band. Flavor: top of its band under the name (not vertically centered in a huge leftover).
                st.alignment = title ? TextAnchor.UpperCenter : TextAnchor.UpperCenter;
                // Card copy is static, so the fit (size plus wrapped lines) is cached per
                // block. Every album card used to re-run up to 18 sizes x a word wrap
                // (Split, concatenation, CalcSize) on every OnGUI pass.
                // Cache key ignores hiCap; callers that cap flavor pass a stable title-derived cap.
                if (!BlockHit(text, box, scale, title, floor, out int size, out var lines) || (hiCap > 0 && size > hiCap))
                {
                    size = FitBlock(st, box, text, title, floor, lead, hiCap);
                    Wrap(st, text, box.width);
                    lines = _lines.ToArray();
                    BlockStore(text, box, scale, title, floor, size, lines);
                }
                if (hiCap > 0 && size > hiCap) size = hiCap;
                st.fontSize = size;
                int shown = lines.Length > (title ? 2 : 3) ? (title ? 2 : 3) : lines.Length;
                if (shown <= 0) return 0;
                float lineH = st.fontSize * lead;
                float blockH = shown * lineH;
                // Top-align in the band with a small inset so outline rings stay inside the face.
                float inset = Mathf.Max(2f, size * 0.08f);
                float y = box.y + inset;
                if (y + blockH > box.yMax - inset)
                    y = Mathf.Max(box.y, box.yMax - inset - blockH);
                int dark = Mathf.Clamp(Mathf.RoundToInt(size * 0.08f), 1, title ? 5 : 4);
                for (int i = 0; i < shown; i++)
                {
                    var line = new Rect(box.x, y + i * lineH, box.width, lineH);
                    StampOutlined(line, lines[i], st, ink, 1, dark);
                }
                return size;
            }

            // ---- shared block-fit cache (one per CardText) ----
            const int BlockSlots = 64;
            static readonly string[] _bkText = new string[BlockSlots];
            static readonly float[] _bkW = new float[BlockSlots];
            static readonly float[] _bkH = new float[BlockSlots];
            static readonly float[] _bkScale = new float[BlockSlots];
            static readonly bool[] _bkTitle = new bool[BlockSlots];
            static readonly int[] _bkFloor = new int[BlockSlots];
            static readonly int[] _bkSize = new int[BlockSlots];
            static readonly string[][] _bkLines = new string[BlockSlots][];
            static readonly int[] _bkUse = new int[BlockSlots];
            static int _bkSerial;
            static int _bkFont = int.MinValue;

            static bool BlockHit(string text, Rect box, float scale, bool title, int floor, out int size, out string[] lines)
            {
                size = 0;
                lines = null;
                var font = Style().font;
                int fontId = font != null ? font.GetInstanceID() : 0;
                if (fontId != _bkFont)
                {
                    _bkFont = fontId;
                    for (int i = 0; i < BlockSlots; i++) _bkText[i] = null;
                    return false;
                }
                for (int i = 0; i < BlockSlots; i++)
                {
                    if (_bkText[i] == null || _bkTitle[i] != title || _bkFloor[i] != floor) continue;
                    if (_bkW[i] != box.width || _bkH[i] != box.height || _bkScale[i] != scale) continue;
                    if (!string.Equals(_bkText[i], text)) continue;
                    _bkUse[i] = ++_bkSerial;
                    size = _bkSize[i];
                    lines = _bkLines[i];
                    return true;
                }
                return false;
            }

            static void BlockStore(string text, Rect box, float scale, bool title, int floor, int size, string[] lines)
            {
                int slot = 0;
                int oldest = int.MaxValue;
                for (int i = 0; i < BlockSlots; i++)
                {
                    if (_bkText[i] == null) { slot = i; break; }
                    if (_bkUse[i] < oldest)
                    {
                        oldest = _bkUse[i];
                        slot = i;
                    }
                }
                _bkText[slot] = text;
                _bkW[slot] = box.width;
                _bkH[slot] = box.height;
                _bkScale[slot] = scale;
                _bkTitle[slot] = title;
                _bkFloor[slot] = floor;
                _bkSize[slot] = size;
                _bkLines[slot] = lines;
                _bkUse[slot] = ++_bkSerial;
            }

            // Shrink until the wrap is at most 2 (title) or 3 (flavor) lines, every line
            // fits the band width, and the wrapped height fits the band. CalcHeight, not
            // a single-line size. The cache key is the live rect, so a zoom or flip refits.
            static int FitBlock(GUIStyle st, Rect box, string text, bool title, int floor, float lead, int hiCap)
            {
                if (string.IsNullOrEmpty(text) || box.width < 4f || box.height < 4f) return floor;
                int maxLines = title ? 2 : 3;
                float innerH = box.height * 0.90f;
                float innerW = box.width * 0.98f;
                int baseHi = Mathf.RoundToInt(box.height * (title ? 0.46f : 0.40f));
                int hi = Mathf.Max(floor, baseHi);
                if (hiCap > 0 && hi > hiCap) hi = hiCap;
                if (hi < floor) hi = floor;
                int size = hi;
                // A fullscreen inspect band starts near half its height, so a long
                // title on a narrow card needs every step down to 8. A fixed step
                // cap stopped early and the line spilled past the band.
                while (size > 8)
                {
                    st.fontSize = size;
                    st.wordWrap = true;
                    Wrap(st, text, innerW);
                    bool wide = false;
                    float h = 0f;
                    int n = _lines.Count;
                    for (int i = 0; i < n; i++)
                    {
                        float lw = st.CalcSize(GuiPool.Text(GuiText.WrapWords, _lines[i])).x;
                        if (lw > innerW) wide = true;
                        h += st.CalcHeight(GuiPool.Text(GuiText.WrapWords, _lines[i]), innerW);
                    }
                    if (n == 0) h = st.CalcHeight(GuiPool.Text(GuiText.WrapWords, text), innerW);
                    if (h < size * lead) h = size * lead * Mathf.Max(1, n);
                    if (n <= maxLines && !wide && h <= innerH) break;
                    size -= 1;
                }
                st.fontSize = size;
                Wrap(st, text, innerW);
                return size;
            }

            static bool BlockFits(GUIStyle st, Rect box, string text, int size, bool title, float lead)
            {
                if (string.IsNullOrEmpty(text)) return true;
                st.fontSize = Mathf.Max(1, size);
                st.wordWrap = true;
                float innerW = box.width * 0.98f;
                float innerH = box.height * 0.92f;
                Wrap(st, text, innerW);
                int maxLines = title ? 2 : 3;
                if (_lines.Count > maxLines) return false;
                float h = 0f;
                for (int i = 0; i < _lines.Count; i++)
                {
                    if (st.CalcSize(GuiPool.Text(GuiText.WrapWords, _lines[i])).x > box.width) return false;
                    h += st.CalcHeight(GuiPool.Text(GuiText.WrapWords, _lines[i]), innerW);
                }
                if (h < size * lead) h = size * lead * Mathf.Max(1, _lines.Count);
                return h <= innerH + 1f;
            }

            static int LineCount(GUIStyle st, string text, float width)
            {
                Wrap(st, text, width);
                return _lines.Count;
            }

            static void Wrap(GUIStyle st, string text, float width)
            {
                _lines.Clear();
                if (string.IsNullOrEmpty(text)) return;
                var parts = text.Split('\n');
                for (int p = 0; p < parts.Length; p++)
                    WrapWords(st, parts[p], width);
            }

            static void WrapWords(GUIStyle st, string text, float width)
            {
                if (string.IsNullOrEmpty(text)) return;
                var words = text.Split(' ');
                string cur = "";
                for (int i = 0; i < words.Length; i++)
                {
                    string w = words[i];
                    if (w.Length == 0) continue;
                    string trial = cur.Length == 0 ? w : cur + " " + w;
                    if (cur.Length == 0 || st.CalcSize(GuiPool.Text(GuiText.WrapWords, trial)).x <= width)
                        cur = trial;
                    else
                    {
                        _lines.Add(cur);
                        cur = w;
                    }
                }
                if (cur.Length > 0) _lines.Add(cur);
            }

            static GUIStyle Style()
            {
                if (_style != null) return _style;
                // GUI.skin is only legal inside OnGUI. Batch suites call Probe
                // from -executeMethod, so fall back to a plain style plus the
                // built-in font the default skin labels actually measure with.
                GUIStyle proto = null;
                try
                {
                    var skin = GUI.skin;
                    if (skin != null) proto = skin.label;
                }
                catch (System.ArgumentException)
                {
                    proto = null;
                }
                _style = proto != null ? new GUIStyle(proto) : new GUIStyle();
                if (_style.font == null)
                    _style.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _style.fontStyle = FontStyle.Bold;
                _style.alignment = TextAnchor.MiddleCenter;
                _style.wordWrap = false;
                _style.clipping = TextClipping.Clip;
                return _style;
            }
        }
    }
}
