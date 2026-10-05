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
            public static readonly Scale Full = new Scale { Title = 1.70f, Flavor = 1.55f, Status = 1.40f };

            static readonly List<string> _lines = new List<string>(6);
            static readonly List<string> _audit = new List<string>(8);
            static GUIStyle _style;

            public delegate void StarDraw(Rect rect, int stars, float s);

            public static void DrawFront(Rect face, string title, string flavor, bool full, float s)
            {
                var scale = full ? Full : Mid;
                Split(face, false, scale, out var titleR, out var flavorR, out _, out _, out _);
                var ink = new Color(0.16f, 0.07f, 0.02f, 1f);
                DrawBlock(titleR, title, scale.Title, true, full ? 22 : 13, ink, 1.15f);
                DrawBlock(flavorR, flavor, scale.Flavor, false, full ? 16 : 11, ink, 1.35f);
            }

            public static void DrawBack(
                Rect face, string title, string flavor, string status, int stars, string[] attrs,
                bool full, float s, StarDraw starsDraw)
            {
                var scale = full ? Full : Mid;
                bool empty = attrs == null || attrs.Length == 0;
                Split(face, !empty, scale, out var titleR, out var flavorR, out var attrR, out var starR, out var ownR);
                var ink = new Color(0.16f, 0.07f, 0.02f, 1f);
                DrawBlock(titleR, title, scale.Title, true, full ? 20 : 12, ink, 1.1f);
                DrawBlock(flavorR, flavor, scale.Flavor, false, full ? 15 : 11, new Color(0.18f, 0.08f, 0.03f, 1f), 1.35f);
                DrawAttrs(attrR, attrs, full);
                if (starsDraw != null && starR.height > 4f) starsDraw(starR, stars, s);
                DrawBlock(ownR, status, scale.Status, true, full ? 14 : 11, ink, 1f);
            }

            // Empty list draws nothing. Flavor then uses the attributes band.
            public static void DrawAttrs(Rect zone, string[] attrs, bool full)
            {
                if (attrs == null || attrs.Length == 0 || zone.height < 8f) return;
                int n = attrs.Length > 4 ? 4 : attrs.Length;
                float gap = Mathf.Max(4f, zone.width * 0.03f);
                float w = (zone.width - gap * (n - 1)) / n;
                var st = Style();
                st.alignment = TextAnchor.MiddleCenter;
                st.wordWrap = false;
                for (int i = 0; i < n; i++)
                {
                    var slot = new Rect(zone.x + i * (w + gap), zone.y + zone.height * 0.08f, w, zone.height * 0.84f);
                    GUI.color = new Color(0.22f, 0.12f, 0.05f, 0.16f);
                    GUI.DrawTexture(slot, Texture2D.whiteTexture);
                    GUI.color = new Color(0.45f, 0.28f, 0.12f, 0.85f);
                    GUI.DrawTexture(new Rect(slot.x, slot.y, slot.width, 2f), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                    if (string.IsNullOrEmpty(attrs[i])) continue;
                    int hi = Mathf.Max(full ? 14 : 10, Mathf.RoundToInt(slot.height * 0.42f));
                    st.fontSize = FitFont(st, attrs[i], slot.width * 0.86f, slot.height * 0.72f, full ? 11 : 9, hi);
                    StampOutlined(slot, attrs[i], st, new Color(0.20f, 0.10f, 0.04f, 1f), 0, 2);
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
                float padX = face.width * 0.08f;
                float x = face.x + padX;
                float w = face.width - padX * 2f;
                float y = face.y + face.height * 0.035f;
                float titleH = face.height * (0.105f * scale.Title);
                titleR = new Rect(x, y, w, titleH);
                float bot = face.yMax - face.height * 0.03f;
                float ownH = face.height * (0.075f * Mathf.Clamp(scale.Status, 1f, 1.5f));
                float starH = face.height * 0.09f;
                ownR = new Rect(x, bot - ownH, w, ownH);
                starR = new Rect(x, ownR.y - starH, w, starH);
                float midTop = titleR.yMax + face.height * 0.02f;
                float midBot = starR.y - face.height * 0.015f;
                float midH = Mathf.Max(8f, midBot - midTop);
                if (!attrs)
                {
                    flavorR = new Rect(x, midTop, w, midH);
                    attrR = new Rect(x, midBot, w, 0f);
                    return;
                }
                float attrH = Mathf.Clamp(midH * 0.34f, face.height * 0.12f, midH * 0.42f);
                attrR = new Rect(x, midBot - attrH, w, attrH);
                flavorR = new Rect(x, midTop, w, Mathf.Max(8f, attrR.y - midTop - face.height * 0.01f));
            }

            static void DrawBlock(Rect box, string text, float scale, bool title, int floor, Color ink, float lead)
            {
                if (box.height < 4f || string.IsNullOrEmpty(text)) return;
                var st = Style();
                st.wordWrap = false;
                st.alignment = TextAnchor.MiddleCenter;
                // Card copy is static, so the fit (size plus wrapped lines) is cached per
                // block. Every album card used to re-run up to 18 sizes x a word wrap
                // (Split, concatenation, CalcSize) on every OnGUI pass.
                if (!BlockHit(text, box, scale, title, floor, out int size, out var lines))
                {
                    int baseHi = Mathf.RoundToInt(box.height * (title ? 0.62f : 0.28f));
                    int hi = Mathf.Max(floor + 2, Mathf.RoundToInt(baseHi * scale));
                    size = hi;
                    int guard = 0;
                    while (size > floor && guard < 18)
                    {
                        st.fontSize = size;
                        if (LineCount(st, text, box.width) <= 3) break;
                        size -= 1;
                        guard++;
                    }
                    st.fontSize = size;
                    Wrap(st, text, box.width);
                    lines = _lines.ToArray();
                    BlockStore(text, box, scale, title, floor, size, lines);
                }
                st.fontSize = size;
                int shown = lines.Length > 3 ? 3 : lines.Length;
                if (shown <= 0) return;
                float lineH = st.fontSize * lead;
                float blockH = shown * lineH;
                float y = box.y + Mathf.Max(0f, (box.height - blockH) * 0.5f);
                int dark = Mathf.Clamp(Mathf.RoundToInt(size * 0.10f), 2, 6);
                for (int i = 0; i < shown; i++)
                {
                    var line = new Rect(box.x, y + i * lineH, box.width, lineH);
                    StampOutlined(line, lines[i], st, ink, 1, dark);
                }
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
                _style = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = false,
                    clipping = TextClipping.Clip
                };
                return _style;
            }
        }
    }
}
