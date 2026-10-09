using System;
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
                CopyArgs(full, false, out int titleFloor, out float titleLead, out int flavorFloor, out float flavorLead, out float flavorRatio);
                int titlePx = DrawBlock(titleR, title, scale.Title, true, titleFloor, ink, titleLead);
                int flavorHi = titlePx > 0
                    ? Mathf.Max(flavorFloor, Mathf.RoundToInt(titlePx * flavorRatio))
                    : 0;
                DrawBlock(flavorR, flavor, scale.Flavor, false, flavorFloor, ink, flavorLead, flavorHi);
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
                CopyArgs(full, true, out int titleFloor, out float titleLead, out int flavorFloor, out float flavorLead, out float flavorRatio);
                int titlePx = DrawBlock(titleR, title, scale.Title, true, titleFloor, ink, titleLead);
                int flavorHi = titlePx > 0
                    ? Mathf.Max(flavorFloor, Mathf.RoundToInt(titlePx * flavorRatio))
                    : 0;
                DrawBlock(flavorR, flavor, scale.Flavor, false, flavorFloor, new Color(0.18f, 0.08f, 0.03f, 1f), flavorLead, flavorHi);
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
                // Drips hang from the hex tip and stay inside the honey zone.
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
                var titleFit = Fit(titleR, title, FitKind.Title, full ? 12 : 10, full ? 1.10f : 1.12f, 0);
                int flavorFloor = full ? 12 : 10;
                int flavorHi = titleFit.Size > 0 ? Mathf.Max(flavorFloor, Mathf.RoundToInt(titleFit.Size * (full ? 0.92f : 0.84f))) : 0;
                var flavorFit = Fit(flavorR, flavor, FitKind.Flavor, flavorFloor, full ? 1.12f : 1.15f, flavorHi);
                if (titleR.Overlaps(flavorR)) return false;
                return titleFit.Ok && flavorFit.Ok;
            }

            // Returns the fitted font size (0 if nothing drew). Optional hiCap keeps flavor
            // under the name on tall inspect bands without a separate draw path.
            static int DrawBlock(Rect box, string text, float scale, bool title, int floor, Color ink, float lead, int hiCap = 0)
            {
                if (box.height < 4f || string.IsNullOrEmpty(text)) return 0;
                var st = Style();
                st.wordWrap = false;
                st.alignment = TextAnchor.UpperCenter;
                // Card copy is static, so the fit (size plus wrapped lines) is cached per
                // block. Every album card used to re-run up to 18 sizes x a word wrap
                // (Split, concatenation, CalcSize) on every OnGUI pass.
                int leadKey = Mathf.RoundToInt(lead * 1000f);
                if (!BlockHit(text, box, scale, title, floor, hiCap, leadKey, out int size, out var lines))
                {
                    var fit = Fit(box, text, title ? FitKind.Title : FitKind.Flavor, floor, lead, hiCap);
                    size = fit.Size;
                    lines = fit.Lines;
                    BlockStore(text, box, scale, title, floor, hiCap, leadKey, size, lines);
                }
                if (lines == null || lines.Length == 0 || size < 1) return 0;
                PaintOutlined(box, lines, size, title ? FitKind.Title : FitKind.Flavor, lead, ink, TextAnchor.UpperCenter);
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
            static readonly int[] _bkHi = new int[BlockSlots];
            static readonly int[] _bkLead = new int[BlockSlots];
            static readonly int[] _bkSize = new int[BlockSlots];
            static readonly string[][] _bkLines = new string[BlockSlots][];
            static readonly int[] _bkUse = new int[BlockSlots];
            static int _bkSerial;
            static int _bkFont = int.MinValue;

            static bool BlockHit(string text, Rect box, float scale, bool title, int floor, int hiCap, int leadKey, out int size, out string[] lines)
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
                    if (_bkHi[i] != hiCap || _bkLead[i] != leadKey) continue;
                    if (_bkW[i] != box.width || _bkH[i] != box.height || _bkScale[i] != scale) continue;
                    if (!string.Equals(_bkText[i], text)) continue;
                    _bkUse[i] = ++_bkSerial;
                    size = _bkSize[i];
                    lines = _bkLines[i];
                    return true;
                }
                return false;
            }

            static void BlockStore(string text, Rect box, float scale, bool title, int floor, int hiCap, int leadKey, int size, string[] lines)
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
                _bkHi[slot] = hiCap;
                _bkLead[slot] = leadKey;
                _bkSize[slot] = size;
                _bkLines[slot] = lines;
                _bkUse[slot] = ++_bkSerial;
            }

            // One fit for every card name and flavor. Largest size that keeps every
            // word, wraps on spaces to at most 2 lines (3 for flavor), and leaves the
            // LabelRim outline inside the rect. A single long word shrinks until it
            // fits. The readable floor is preferred: wrapping to two lines happens
            // before the size drops under that floor. Nothing is truncated.
            public enum FitKind
            {
                Title = 0,
                Flavor = 1,
                Finish = 2,
                Column = 3,
            }

            public struct Fitted
            {
                public int Size;
                public int Outline;
                public float LineStep;
                // How far a descender (g, y, p, q, j) hangs past the style line box.
                // LineStep includes it, so every wrapped line keeps the tail.
                public float Descender;
                public float BlockH;
                public float MaxLineW;
                public string[] Lines;
                public bool Ok;
            }

            public static void TitleFitArgs(bool full, out int floor, out float lead)
            {
                floor = full ? 22 : 13;
                lead = full ? 1.12f : 1.15f;
            }

            // Shared by the album draw and the card-fit suite. Back title sits a step
            // under the front; flavor stays just under the fitted name.
            public static void CopyArgs(bool full, bool back, out int titleFloor, out float titleLead, out int flavorFloor, out float flavorLead, out float flavorRatio)
            {
                if (back)
                {
                    titleFloor = full ? 20 : 12;
                    titleLead = full ? 1.10f : 1.1f;
                    flavorFloor = full ? 15 : 11;
                }
                else
                {
                    TitleFitArgs(full, out titleFloor, out titleLead);
                    flavorFloor = full ? 16 : 11;
                }
                flavorLead = full ? 1.12f : 1.15f;
                flavorRatio = full ? 0.92f : 0.84f;
            }

            public static void FitCopy(Rect titleR, Rect flavorR, string name, string copy, bool full, bool back, out Fitted title, out Fitted flavor)
            {
                CopyArgs(full, back, out int titleFloor, out float titleLead, out int flavorFloor, out float flavorLead, out float flavorRatio);
                title = Fit(titleR, name, FitKind.Title, titleFloor, titleLead, 0);
                int hi = title.Size > 0 ? Mathf.Max(flavorFloor, Mathf.RoundToInt(title.Size * flavorRatio)) : 0;
                flavor = Fit(flavorR, copy, FitKind.Flavor, flavorFloor, flavorLead, hi);
            }

            public static void FinishFitArgs(bool full, float s, out int floor, out int hi, out float lead)
            {
                hi = full ? Mathf.Max(18, Mathf.RoundToInt(16f * s)) : Mathf.Max(12, Mathf.RoundToInt(14f * s));
                floor = full ? 16 : 11;
                lead = 1.08f;
            }

            public static Fitted FitTitle(Rect nameRect, string name, bool full)
            {
                TitleFitArgs(full, out int floor, out float lead);
                return Fit(nameRect, name, FitKind.Title, floor, lead, 0);
            }

            public static Fitted FitFinish(Rect band, string name, bool full, float s)
            {
                FinishFitArgs(full, s, out int floor, out int hi, out float lead);
                return Fit(band, name, FitKind.Finish, floor, lead, hi);
            }

            public static Fitted FitColumn(Rect band, string text, float s)
            {
                int floor = Mathf.Max(16, Mathf.RoundToInt(16f * s));
                int hi = Mathf.Max(floor + 2, Mathf.RoundToInt(20f * s));
                return Fit(band, text, FitKind.Column, floor, 1.05f, hi);
            }

            public static Fitted Fit(Rect box, string text, FitKind kind, int floor, float lead, int hiCap)
            {
                var fitted = new Fitted { Lines = Array.Empty<string>() };
                if (string.IsNullOrEmpty(text))
                {
                    fitted.Ok = true;
                    return fitted;
                }
                if (box.width < 4f || box.height < 4f) return fitted;
                if (lead < 0.8f) lead = 0.8f;
                if (floor < 1) floor = 1;
                var st = Style();
                int baseHi = Mathf.RoundToInt(box.height * (kind == FitKind.Flavor ? 0.40f : 0.46f));
                int hi = Mathf.Max(floor, baseHi);
                if (hiCap > 0)
                {
                    if (hi > hiCap) hi = hiCap;
                    if (hi < 1) hi = 1;
                }
                if (hi < 1) hi = 1;
                for (int size = hi; size >= 1; size--)
                {
                    MeasureAt(st, box, text, kind, size, lead, out fitted);
                    if (fitted.Ok) return fitted;
                }
                fitted.Ok = false;
                return fitted;
            }

            static int MaxLinesOf(FitKind kind) => kind == FitKind.Flavor ? 3 : 2;

            // Pixels the stroke extends past the glyph rect. Title and flavor match
            // StampOutlined(white 1, black ~8% of the size). Finish is the old 1+1 rim.
            // Column is StampReadable's drop, which reaches past the 1px LabelRim.
            static int InkOf(FitKind kind, int size)
            {
                if (size < 1) size = 1;
                if (kind == FitKind.Finish) return LabelRim.Radius(2f);
                if (kind == FitKind.Column)
                {
                    float drop = Mathf.Max(1.5f, size * 0.07f);
                    int farX = Mathf.Max(1, Mathf.RoundToInt(drop * 1.7f));
                    int farY = Mathf.Max(1, Mathf.RoundToInt(drop * 2.2f));
                    return Mathf.Max(LabelRim.Radius(1f), Mathf.Max(farX, farY));
                }
                int cap = kind == FitKind.Title ? 5 : 4;
                int dark = Mathf.Clamp(Mathf.RoundToInt(size * 0.08f), 1, cap);
                return LabelRim.Radius(1 + dark);
            }

            static void MeasureAt(GUIStyle st, Rect box, string text, FitKind kind, int size, float lead, out Fitted fit)
            {
                int ink = InkOf(kind, size);
                fit = new Fitted
                {
                    Size = size,
                    Outline = ink,
                    Lines = Array.Empty<string>(),
                };
                float innerW = box.width - ink * 2f;
                float innerH = box.height - ink * 2f;
                if (innerW < 2f || innerH < 2f) return;
                // One pixel of bold bearing stays inside the outline box.
                float measureW = Mathf.Max(1f, innerW - 1f);
                st.fontSize = size;
                st.wordWrap = false;
                Wrap(st, text, measureW);
                int n = _lines.Count;
                if (n <= 0)
                {
                    fit.Ok = true;
                    return;
                }
                if (n > MaxLinesOf(kind) || !WordsKept(text, _lines)) return;
                var lines = _lines.ToArray();
                float maxW = 0f;
                for (int i = 0; i < n; i++)
                {
                    st.wordWrap = false;
                    float lw = st.CalcSize(GuiPool.Text(GuiText.WrapWords, lines[i])).x;
                    if (lw > maxW) maxW = lw;
                    if (lw > measureW + 0.01f) 
                    {
                        fit.Lines = lines;
                        fit.MaxLineW = maxW;
                        return;
                    }
                }
                CopyMetrics(st, lines, size, lead, measureW, kind, out float lineHeight, out float descender, out float step);
                // One pixel of slack so a snapped LabelRim sample is not flush with the band.
                float slack = descender > 0f ? 1f : 0f;
                float block = step * n;
                fit.Lines = lines;
                fit.LineStep = step;
                fit.Descender = descender;
                fit.BlockH = block;
                fit.MaxLineW = maxW;
                fit.Ok = block <= innerH - slack + 0.01f;
            }

            // lineHeight is the style line box (lead, CalcSize, CalcHeight, and the
            // native line height when this block has a descender). descender is how
            // far g/y/p/q/j still hangs past that box. advance is the line step:
            // every line keeps the tail, so the next line does not cover it.
            static void CopyMetrics(GUIStyle st, string[] lines, int size, float lead, float measureW, FitKind kind,
                out float lineHeight, out float descender, out float advance)
            {
                lineHeight = StepOf(st, lines, size, lead, measureW);
                descender = 0f;
                if (kind == FitKind.Title || kind == FitKind.Flavor)
                {
                    float box = lineHeight;
                    float native = NativeLine(st);
                    if (native > box) box = native;
                    descender = TailPast(st, lines, size, box);
                    if (descender > 0f) lineHeight = box;
                }
                advance = lineHeight + descender;
            }

            // The card-fit suite measures the same tail the fitter reserves.
            public static void MeasureCopy(int size, float lead, string[] lines, float measureW, out float lineHeight, out float descender)
            {
                CopyMetrics(Style(), lines, size, lead, measureW, FitKind.Flavor, out lineHeight, out descender, out _);
            }

            public static bool HasDescender(string text)
            {
                if (string.IsNullOrEmpty(text)) return false;
                for (int i = 0; i < text.Length; i++)
                {
                    char c = text[i];
                    if (c == 'g' || c == 'y' || c == 'p' || c == 'q' || c == 'j') return true;
                }
                return false;
            }

            // Floor for the tail past the line box. TextCore's IMGUI path adds about
            // 6px of extra padding and then clips Overflow to the rect. 8% of a large
            // inspect size covers that; a thumbnail floors at 2px.
            public static float DescenderPad(int size)
            {
                if (size < 1) return 0f;
                return Mathf.Max(2f, Mathf.Ceil(size * 0.08f));
            }

            static float NativeLine(GUIStyle st)
            {
                if (st == null || st.fontSize < 1) return 0f;
                try
                {
                    float h = st.lineHeight;
                    if (h > 0.5f && h < st.fontSize * 3f) return h;
                }
                catch (Exception)
                {
                }
                return 0f;
            }

            static float TailPast(GUIStyle st, string[] lines, int size, float lineHeight)
            {
                if (size < 1 || lines == null || lines.Length == 0) return 0f;
                bool any = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (!HasDescender(lines[i])) continue;
                    any = true;
                    break;
                }
                if (!any) return 0f;
                float hang = GlyphHang(st, lines, size, lineHeight);
                float pad = DescenderPad(size);
                float tail = hang > pad ? hang : pad;
                float cap = size * 0.45f;
                if (tail > cap) tail = cap;
                return tail;
            }

            // How far a descender glyph's ink extends below the line box we paint.
            // CharacterInfo y is up, baseline at 0. A miss falls through to the pad.
            static float GlyphHang(GUIStyle st, string[] lines, int size, float lineHeight)
            {
                var font = st != null ? st.font : null;
                if (font == null || lineHeight < 1f) return 0f;
                var face = st.fontStyle;
                float worst = 0f;
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (!HasDescender(line)) continue;
                    try { font.RequestCharactersInTexture(line, size, face); }
                    catch (Exception) { continue; }
                    float ascent = font.ascent;
                    if (font.fontSize > 1) ascent *= size / (float)font.fontSize;
                    if (ascent < size * 0.4f || ascent > size * 1.6f) ascent = size * 0.90f;
                    bool got = false;
                    float minY = 0f;
                    for (int c = 0; c < line.Length; c++)
                    {
                        char ch = line[c];
                        if (ch != 'g' && ch != 'y' && ch != 'p' && ch != 'q' && ch != 'j') continue;
                        if (!font.GetCharacterInfo(ch, out var info, size, face)) continue;
                        if (!got || info.minY < minY) minY = info.minY;
                        got = true;
                    }
                    if (!got || minY > size * 0.05f) continue;
                    float ink = ascent - minY;
                    if (ink < size * 0.4f || ink > size * 2.5f) continue;
                    float past = ink - lineHeight;
                    if (past > worst) worst = past;
                }
                return worst > 0f ? worst : 0f;
            }

            static float StepOf(GUIStyle st, string[] lines, int size, float lead, float measureW)
            {
                float step = size * lead;
                st.fontSize = size;
                for (int i = 0; i < lines.Length; i++)
                {
                    var content = GuiPool.Text(GuiText.WrapWords, lines[i]);
                    st.wordWrap = false;
                    float y = st.CalcSize(content).y;
                    st.wordWrap = true;
                    float ch = st.CalcHeight(content, Mathf.Max(1f, measureW));
                    if (y > step) step = y;
                    if (ch > step) step = ch;
                }
                return step;
            }

            // Source words, in order. Spaces and the flavor's sentence newlines both
            // separate words. A clipped "Invers" or a dropped "Rainbow" fails.
            static bool WordsKept(string text, List<string> lines)
            {
                int k = 0;
                for (int i = 0; i < lines.Count; i++)
                {
                    var parts = lines[i].Split(' ');
                    for (int j = 0; j < parts.Length; j++)
                    {
                        if (parts[j].Length == 0) continue;
                        string token = NextToken(text, ref k);
                        if (token == null || !string.Equals(parts[j], token)) return false;
                    }
                }
                return NextToken(text, ref k) == null;
            }

            static string NextToken(string text, ref int k)
            {
                while (k < text.Length && (text[k] == ' ' || text[k] == '\n' || text[k] == '\r')) k++;
                if (k >= text.Length) return null;
                int start = k;
                while (k < text.Length && text[k] != ' ' && text[k] != '\n' && text[k] != '\r') k++;
                return text.Substring(start, k - start);
            }

            static void PaintOutlined(Rect box, string[] lines, int size, FitKind kind, float lead, Color ink, TextAnchor align)
            {
                if (lines == null || lines.Length == 0 || size < 1) return;
                int outline = InkOf(kind, size);
                float measureW = Mathf.Max(1f, box.width - outline * 2f - 1f);
                // Unity's advanced IMGUI path clips Overflow to the label rect, so the
                // rect itself has to hold the descender. No BeginGroup or scissor here:
                // a clip on this rect would cut the same tail. The last line's rim
                // hangs `outline` below the rect into the slack MeasureAt reserved.
                CopyMetrics(Style(), lines, size, lead, measureW, kind, out _, out _, out float step);
                var st = Style();
                st.alignment = align;
                st.fontSize = size;
                st.wordWrap = false;
                st.clipping = TextClipping.Overflow;
                int dark = Mathf.Max(1, outline - 1);
                float x = box.x + outline;
                float w = Mathf.Max(1f, box.width - outline * 2f);
                float y = box.y + outline;
                for (int i = 0; i < lines.Length; i++)
                {
                    StampOutlined(new Rect(x, y, w, step), lines[i], st, ink, 1, dark);
                    y += step;
                }
            }

            public static int DrawFinish(Rect band, string text, bool full, float s, Color ink)
            {
                FinishFitArgs(full, s, out int floor, out int hi, out float lead);
                var fit = Fit(band, text, FitKind.Finish, floor, lead, hi);
                PaintOutlined(band, fit.Lines, fit.Size, FitKind.Finish, lead, ink, TextAnchor.UpperLeft);
                return fit.Size;
            }

            public static int DrawColumn(Rect band, string text, float s, Color ink)
            {
                var fit = FitColumn(band, text, s);
                if (fit.Lines == null || fit.Lines.Length == 0 || fit.Size < 1) return 0;
                var st = Style();
                st.alignment = TextAnchor.MiddleCenter;
                st.fontSize = fit.Size;
                st.wordWrap = false;
                st.clipping = TextClipping.Overflow;
                float outer = fit.BlockH + fit.Outline * 2f;
                float y = band.y + Mathf.Max(0f, (band.height - outer) * 0.5f) + fit.Outline;
                float x = band.x + fit.Outline;
                float w = Mathf.Max(1f, band.width - fit.Outline * 2f);
                for (int i = 0; i < fit.Lines.Length; i++)
                {
                    StampReadable(new Rect(x, y, w, fit.LineStep), fit.Lines[i], st, ink);
                    y += fit.LineStep;
                }
                return fit.Size;
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
                bool wrap = st.wordWrap;
                st.wordWrap = false;
                var words = text.Split(' ');
                string cur = "";
                for (int i = 0; i < words.Length; i++)
                {
                    string w = words[i];
                    if (w.Length == 0) continue;
                    string trial = cur.Length == 0 ? w : cur + " " + w;
                    // A word wider than the line stays whole. The fitter shrinks it.
                    // It is never cut mid-glyph.
                    if (cur.Length == 0 || st.CalcSize(GuiPool.Text(GuiText.WrapWords, trial)).x <= width)
                        cur = trial;
                    else
                    {
                        _lines.Add(cur);
                        cur = w;
                    }
                }
                if (cur.Length > 0) _lines.Add(cur);
                st.wordWrap = wrap;
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
                var builtin = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (builtin != null) _style.font = builtin;
                _style.fontStyle = FontStyle.Bold;
                _style.alignment = TextAnchor.MiddleCenter;
                _style.wordWrap = false;
                _style.richText = false;
                // The fit owns the bounds. Clip would shave a bold bearing or the rim.
                _style.clipping = TextClipping.Overflow;
                _style.padding = new RectOffset(0, 0, 0, 0);
                _style.margin = new RectOffset(0, 0, 0, 0);
                _style.overflow = new RectOffset(0, 0, 0, 0);
                return _style;
            }

            public static GUIStyle MeasureStyle() => Style();

            public struct Grid
            {
                public float S, CardW, CardH, CellW, CellH, Gap, Gx0, Gy0, ColH, ColY, PageBottom, PagerHit, PagerLab;
                public Rect Sheet;
            }

            // Album grid card size. DrawHivePage places sleeves from this, and the
            // card-fit suite measures the same card on 1170×2532 and 750×1334.
            public static Grid GridOf(float screenW, float screenH, Rect safe, float hiveBottom)
            {
                float s = Mathf.Max(screenH / 720f, 1f);
                int typeFloor = Mathf.Max(16, Mathf.RoundToInt(17f * s));
                float colH = typeFloor * 2.2f + 6f * s;
                float colGap = 6f * s;
                float pagerHit = Mathf.Max(48f, 56f * s);
                float pagerLab = Mathf.Max(36f, 30f * s);
                float botInset = Mathf.Max(8f * s, safe.yMin + 4f);
                float footGap = 22f;
                float pageBottom = screenH - botInset - pagerHit - pagerLab - footGap - 16f - 6f * s;
                float pageTop = hiveBottom + colH + colGap;
                float pageH = pageBottom - pageTop;
                if (pageH < 72f * s) pageH = 72f * s;
                float pagePad = 10f * s;
                float pageW = screenW - pagePad * 2f;
                var sheet = new Rect(pagePad, pageTop, pageW, pageH);
                float inner = 8f * s;
                float gap = 10f * s;
                float cellW = (sheet.width - inner * 2f - gap * 2f) / 3f;
                float cellH = (sheet.height - inner * 2f - gap * 2f) / 3f;
                if (cellW < 8f) cellW = 8f;
                if (cellH < 8f) cellH = 8f;
                float cardW = cellW - 6f * s;
                float cardH = Mathf.Min(cellH - 6f * s, cardW * 1.35f);
                if (cardW < 8f) cardW = 8f;
                if (cardH < 8f) cardH = 8f;
                float gridW = 3f * cellW + 2f * gap;
                float gridH = 3f * cellH + 2f * gap;
                return new Grid
                {
                    S = s,
                    CardW = cardW,
                    CardH = cardH,
                    CellW = cellW,
                    CellH = cellH,
                    Gap = gap,
                    Gx0 = sheet.x + (sheet.width - gridW) * 0.5f,
                    Gy0 = sheet.y + (sheet.height - gridH) * 0.5f,
                    ColH = colH,
                    ColY = hiveBottom + 4f * s,
                    PageBottom = pageBottom,
                    PagerHit = pagerHit,
                    PagerLab = pagerLab,
                    Sheet = sheet,
                };
            }

            // Header plus the tally bar. Body height is measured with the card font
            // (LegacyRuntime, bold, word wrap), the same constants DrawAlbumHeader uses.
            public static float ShellBottom(float screenW, float screenH, Rect safe)
            {
                float s = Mathf.Max(screenH / 720f, 1f);
                float top = TopHudBox(screenH, safe, 0f);
                var back = BackMedalBox(s, top, safe);
                int floor = Mathf.Max(16, Mathf.RoundToInt(17f * s));
                float hiveSize = Mathf.Clamp(back.height * 0.80f, 44f, 92f);
                float hiveHeadBottom = back.center.y + hiveSize * 0.5f;
                float left = Mathf.Max(16f * s, safe.xMin + 10f);
                float textRight = screenW - Mathf.Max(16f * s, screenW - safe.xMax + 12f);
                float bodyW = Mathf.Max(48f, textRight - left);
                int bodyPx = Mathf.Max(floor - 1, Mathf.RoundToInt(17f * s));
                if (bodyPx < 15) bodyPx = 15;
                string body = HiveSubtitle + "\n" + HiveHowTo;
                var st = MeasureStyle();
                int prev = st.fontSize;
                var prevAlign = st.alignment;
                bool prevWrap = st.wordWrap;
                st.fontSize = bodyPx;
                st.alignment = TextAnchor.UpperLeft;
                st.wordWrap = true;
                float bodyH = Mathf.Max(bodyPx + 6f, st.CalcHeight(new GUIContent(body), bodyW) + 2f);
                st.fontSize = prev;
                st.alignment = prevAlign;
                st.wordWrap = prevWrap;
                float bodyY = Mathf.Max(hiveHeadBottom, back.yMax + TutorCaptionGap * s + 12f);
                float textBottom = bodyY + bodyH + 12f + 4f * s;
                return TallyBottom(textBottom, s);
            }

            public static void Thumb(float screenW, float screenH, Rect safe, out Grid grid, out Rect card, out Rect title, out Rect finish)
            {
                float shell = ShellBottom(screenW, screenH, safe);
                grid = GridOf(screenW, screenH, safe, shell);
                card = new Rect(0f, 0f, grid.CardW, grid.CardH);
                title = TitleOf(card, false);
                finish = FinishBand(card, false);
            }

            public static Vector2 InspectCard(float screenW, float screenH)
            {
                float maxW = screenW * 0.86f;
                float maxH = screenH * 0.62f;
                if (maxW * 1.4f <= maxH) return new Vector2(maxW, maxW * 1.4f);
                return new Vector2(maxH / 1.4f, maxH);
            }

            public static Rect FaceOf(Rect card)
            {
                return new Rect(card.x + card.width * 0.045f, card.y + card.height * 0.035f, card.width * 0.91f, card.height * 0.93f);
            }

            public static Rect TextFaceOf(Rect face, bool full)
            {
                float top = full ? 0.30f : 0.48f;
                return new Rect(face.x, face.y + face.height * top, face.width, face.height * (0.96f - top));
            }

            public static Rect TitleOf(Rect card, bool full)
            {
                var text = TextFaceOf(FaceOf(card), full);
                Split(text, false, full ? Full : Mid, out var title, out _, out _, out _, out _);
                return title;
            }

            // Front copy uses the lower text face. The back uses the whole face, and a
            // honey row shortens the flavor the same way DrawBack does.
            public static void CopyBands(Rect card, bool full, bool back, bool honey, out Rect title, out Rect flavor)
            {
                var face = FaceOf(card);
                var host = back ? face : TextFaceOf(face, full);
                Split(host, back && honey, full ? Full : Mid, out title, out flavor, out _, out _, out _);
            }

            // Top band of the face, inner width, tall enough for two wrapped lines.
            // Left-aligned finish names ("Holo", "Inverse Rainbow") share Fit().
            public static Rect FinishOf(Rect face, bool full)
            {
                float padX = Mathf.Max(4f, face.width * 0.05f);
                float padY = Mathf.Max(3f, face.height * 0.015f);
                float band = face.height * (full ? 0.14f : 0.26f);
                float limit = face.height * (full ? 0.24f : 0.40f);
                if (band > limit) band = limit;
                return new Rect(face.x + padX, face.y + padY, Mathf.Max(8f, face.width - padX * 2f), Mathf.Max(8f, band));
            }

            public static Rect FinishBand(Rect card, bool full) => FinishOf(FaceOf(card), full);

            public static Rect ColumnBand(Grid grid, int col)
            {
                return new Rect(grid.Gx0 + col * (grid.CellW + grid.Gap), grid.ColY, grid.CellW, grid.ColH);
            }
        }
    }
}
