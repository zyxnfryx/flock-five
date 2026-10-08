using UnityEngine;

namespace FlockFive
{
    // One juicy honeycomb. Card backs, badger tiles, and the badger meter all draw through here.
    // The hex mask, amber fill, wax lattice, gloss, and drips are baked once. A frame only stretches them.
    public static class HoneyArt
    {
        const int W = 96;
        const int H = 111;
        static Texture2D _mask;
        static Texture2D _fill;
        static Texture2D _lattice;
        static Texture2D _gloss;
        static Texture2D _dripA;
        static Texture2D _dripB;
        static bool _warm;

        // A card or popup hangs indices 0..DripCombCells-1 together. LayoutCell sizes
        // those drips from DripGroupSeed so the row cannot share a length or a width.
        public const int DripCombCells = 5;
        public const int DripGroupSeed = 1;

        // One step past 15% so ±length and ±aspect jitter cannot close the gap.
        const float DripStep = 1.26f;
        const float DripLenJitter = 0.008f;
        const float DripAspectJitter = 0.015f;
        // Largest rest length, as a fraction of the hex body. Stays under the
        // hang-room clamp so the drawn rect matches this length.
        const float DripLenMax = 0.33f;
        // Baked teardrop width/height (HoneyDripA). Width tracks length so the bulb stays round.
        const float DripAspect = 128f / 176f;

        public struct DripSize
        {
            public float Length;
            public float Width;
        }

        public static void Warm()
        {
            if (_warm) return;
            _warm = true;
            _mask = BakeMask();
            _fill = BakeFill();
            _lattice = BakeLattice();
            _gloss = BakeGloss();
            _dripA = BakeDrip(0);
            _dripB = BakeDrip(1);
        }

        public static Texture2D Mask()
        {
            Warm();
            return _mask;
        }

        public static Texture2D Fill()
        {
            Warm();
            return _fill;
        }

        // One painted layer. LayoutCell fills these; DrawJuicyCell alpha-blends them.
        // Rects use GUI space (y down). The still camera uses the same list.
        public struct Stamp
        {
            public Rect Rect;
            public Texture2D Tex;
            public Color Color;
        }

        static Stamp[] _stamps;

        // `hex` is the whole cell, drips included. `t` is seconds (inspect drip loop).
        // `s` is the screen scale (height/720) so the gloss stays a few pixels on a small phone.
        // juicy false is the spent / empty comb: the mask tinted by rim, no fill and no drip.
        // drips false keeps a tight grid (badger tiles, meter) from hanging into the next cell.
        public static void DrawJuicyCell(Rect hex, Color rim, float t, float s, int index = 0,
            bool juicy = true, bool animate = false, bool drips = true)
        {
            if (_stamps == null) _stamps = new Stamp[8];
            int n = LayoutCell(hex, rim, t, s, index, juicy, animate, drips, _stamps);
            for (int i = 0; i < n; i++)
            {
                var st = _stamps[i];
                if (st.Tex == null) continue;
                GUI.color = st.Color;
                GUI.DrawTexture(st.Rect, st.Tex, ScaleMode.StretchToFill, true);
            }
            GUI.color = Color.white;
        }

        // Back to front. `into` needs 8 slots. Returns how many were written.
        public static int LayoutCell(Rect hex, Color rim, float t, float s, int index,
            bool juicy, bool animate, bool drips, Stamp[] into)
        {
            if (into == null || into.Length < 1) return 0;
            if (hex.width < 3f || hex.height < 3f) return 0;
            Warm();
            if (_mask == null) return 0;
            int n = 0;
            if (!juicy)
            {
                into[n++] = new Stamp { Rect = hex, Tex = _mask, Color = rim };
                return n;
            }

            float bodyH = drips ? hex.height * 0.78f : hex.height;
            var body = new Rect(hex.x, hex.y, hex.width, bodyH);
            float lift = body.height * 0.045f;
            if (n < into.Length)
                into[n++] = new Stamp
                {
                    Rect = new Rect(body.x + lift * 0.35f, body.y + lift, body.width, body.height),
                    Tex = _mask,
                    Color = new Color(0f, 0f, 0f, 0.30f * rim.a)
                };
            if (n < into.Length)
                into[n++] = new Stamp { Rect = body, Tex = _mask, Color = rim };

            float inset = Mathf.Min(body.width, body.height) * 0.065f;
            var inner = new Rect(body.x + inset, body.y + inset, body.width - inset * 2f, body.height - inset * 2f);
            if (_fill != null && n < into.Length)
                into[n++] = new Stamp { Rect = inner, Tex = _fill, Color = new Color(1f, 1f, 1f, rim.a) };
            if (_lattice != null && n < into.Length)
            {
                float minor = Mathf.Min(inner.width, inner.height);
                float fade = LatticeFade(minor);
                if (fade > 0.02f)
                    into[n++] = new Stamp { Rect = inner, Tex = _lattice, Color = new Color(1f, 1f, 1f, rim.a * fade) };
            }
            if (_gloss != null && n < into.Length)
            {
                float pad = Mathf.Max(0f, s) * 0.15f;
                into[n++] = new Stamp
                {
                    Rect = new Rect(inner.x + pad, inner.y + pad, inner.width, inner.height),
                    Tex = _gloss,
                    Color = new Color(1f, 1f, 1f, rim.a)
                };
            }

            if (drips && bodyH < hex.height - 1f)
                n = LayoutDrips(hex, body, t, index, rim.a, animate, into, n);
            return n;
        }

        // Neck tucked into the hex tip, bulb hanging below. Inspect only stretches
        // height. Length and width come from the comb ladder so two drips in the
        // row cannot share a size. The rect keeps the baked teardrop aspect.
        static int LayoutDrips(Rect hex, Rect body, float t, int index, float alpha, bool animate,
            Stamp[] into, int n)
        {
            int count = DripCount(index);
            if (count == 0) return n;
            float stretch = 1f;
            if (animate)
                stretch = 0.82f + 0.28f * (0.5f + 0.5f * Mathf.Sin(t * (Mathf.PI * 2f / 3f) + index * 0.7f));
            var ink = new Color(1f, 1f, 1f, alpha);
            for (int d = 0; d < count; d++)
            {
                if (n >= into.Length) break;
                var tex = ((index + d) & 1) == 0 ? _dripA : _dripB;
                if (tex == null || tex.height < 1) continue;
                var sz = SizeFor(DripGroupSeed, index, d);
                float h0 = body.height * sz.Length;
                float h = h0 * stretch;
                float w = body.height * sz.Width;
                // Flat cut of the neck sits inside the hex. Extra stretch grows downward.
                float tuck = h0 * 0.22f;
                float y = body.yMax - tuck;
                float room = hex.yMax - y;
                if (room < 3f) continue;
                if (h > room) h = room;
                if (h < 2f || w < 2f) continue;
                float nudge = ((index * 13 + d * 5) % 7 - 3) * body.width * 0.018f;
                float side = d == 0 ? -body.width * 0.10f : body.width * 0.14f;
                float x = body.center.x + side + nudge - w * 0.5f;
                if (x < hex.x) x = hex.x;
                if (x + w > hex.xMax) x = hex.xMax - w;
                into[n++] = new Stamp { Rect = new Rect(x, y, w, h), Tex = tex, Color = ink };
            }
            return n;
        }

        static int DripCount(int index)
        {
            if (index < 0) index = -index;
            int kind = index % 3;
            if (kind == 2) return 0;
            return kind == 0 ? 2 : 1;
        }

        // Drips on a comb of `cells` honeycombs (indices 0..cells-1), in draw order.
        // Same numbers LayoutCell uses when seed is DripGroupSeed. A shorter comb is
        // the prefix of the card row, so a 2-honey and a 5-honey card cannot collide
        // inside the shared ladder.
        public static int CombDripSizes(int seed, int cells, DripSize[] into)
        {
            if (into == null || cells < 1) return 0;
            int limit = cells > DripCombCells ? DripCombCells : cells;
            int n = 0;
            for (int i = 0; i < limit && n < into.Length; i++)
            {
                int c = DripCount(i);
                for (int d = 0; d < c && n < into.Length; d++)
                    into[n++] = SizeFor(seed, i, d);
            }
            return n;
        }

        static readonly int[] _ranks = new int[8];
        static int _rankSeed = int.MinValue;
        static int _rankTotal = -1;

        static DripSize SizeFor(int seed, int index, int slot)
        {
            int cell = index < 0 ? 0 : index;
            if (cell >= DripCombCells) cell %= DripCombCells;
            int total = 0;
            int ordinal = 0;
            for (int i = 0; i < DripCombCells; i++)
            {
                int c = DripCount(i);
                if (i == cell)
                {
                    int s = slot < 0 ? 0 : slot;
                    if (c > 0 && s >= c) s = c - 1;
                    ordinal = total + (c == 0 ? 0 : s);
                }
                total += c;
            }
            if (total < 1) return new DripSize { Length = DripLenMax, Width = DripLenMax * DripAspect };
            EnsureRanks(seed, total);
            int rank = _ranks[ordinal];
            float len = DripLenMax * (StepPow(rank) / StepPow(total - 1));
            len *= 1f + DripLenJitter * SignedUnit(seed, ordinal, 1);
            float width = len * DripAspect * (1f + DripAspectJitter * SignedUnit(seed, ordinal, 2));
            return new DripSize { Length = len, Width = width };
        }

        static void EnsureRanks(int seed, int total)
        {
            if (total > _ranks.Length) total = _ranks.Length;
            if (_rankSeed == seed && _rankTotal == total) return;
            _rankSeed = seed;
            _rankTotal = total;
            for (int i = 0; i < total; i++) _ranks[i] = i;
            for (int i = total - 1; i > 0; i--)
            {
                int j = PositiveHash(seed, i, 0xC0FFEE) % (i + 1);
                int tmp = _ranks[i];
                _ranks[i] = _ranks[j];
                _ranks[j] = tmp;
            }
        }

        static float StepPow(int rank)
        {
            float s = 1f;
            for (int i = 0; i < rank; i++) s *= DripStep;
            return s;
        }

        static int PositiveHash(int a, int b, int salt)
        {
            unchecked
            {
                uint u = (uint)a * 2246822519u ^ (uint)b * 3266489917u ^ (uint)salt * 747796405u;
                u ^= u >> 16;
                u *= 0x7feb352du;
                u ^= u >> 15;
                u *= 0x846ca68bu;
                u ^= u >> 16;
                return (int)(u & 0x7fffffff);
            }
        }

        // -1..1, stable for the life of the seed.
        static float SignedUnit(int seed, int ordinal, int salt)
        {
            int h = PositiveHash(seed ^ (salt * 747796405), ordinal + 1, salt);
            return (h % 10001) / 5000f - 1f;
        }

        // Full by a small card cell. Gone on a tiny meter segment, where the
        // walls would be a fraction of a pixel.
        static float LatticeFade(float minor)
        {
            float t = Mathf.InverseLerp(14f, 36f, minor);
            return t * t * (3f - 2f * t);
        }

        static Texture2D BakeMask()
        {
            var px = new Color32[W * H];
            float a = W * 0.5f;
            float r = H * 0.5f;
            float k = r / (2f * a);
            float norm = Mathf.Sqrt(1f + k * k);
            for (int iy = 0; iy < H; iy++)
            {
                for (int ix = 0; ix < W; ix++)
                {
                    float x = Mathf.Abs(ix + 0.5f - a);
                    float y = Mathf.Abs(iy + 0.5f - r);
                    float d = Mathf.Min(a - x, (r - k * x - y) / norm);
                    float al = Mathf.Clamp01(d + 0.5f);
                    px[iy * W + ix] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(al * 255f));
                }
            }
            return Finish(px, W, H);
        }

        static Texture2D BakeFill()
        {
            var px = new Color32[W * H];
            float a = W * 0.5f;
            float r = H * 0.5f;
            float k = r / (2f * a);
            float norm = Mathf.Sqrt(1f + k * k);
            var deep = new Color(0.62f, 0.24f, 0.04f, 1f);
            var gold = new Color(1f, 0.84f, 0.28f, 1f);
            var glow = new Color(1f, 0.95f, 0.62f, 1f);
            for (int iy = 0; iy < H; iy++)
            {
                for (int ix = 0; ix < W; ix++)
                {
                    float x = Mathf.Abs(ix + 0.5f - a);
                    float y = Mathf.Abs(iy + 0.5f - r);
                    float d = Mathf.Min(a - x, (r - k * x - y) / norm);
                    float al = Mathf.Clamp01((d + 0.5f) / 1.4f);
                    if (al <= 0f)
                    {
                        px[iy * W + ix] = new Color32(0, 0, 0, 0);
                        continue;
                    }
                    // Texture y grows up. iy 0 is the bottom of the cell, and that edge is golden.
                    float up = 1f - iy / (float)(H - 1);
                    var c = Color.Lerp(deep, gold, Mathf.Clamp01(up * 1.05f));
                    float nx = (ix + 0.5f) / W - 0.5f;
                    float ny = (iy + 0.5f) / H - 0.42f;
                    float rad = Mathf.Sqrt(nx * nx + ny * ny);
                    c = Color.Lerp(c, glow, Mathf.Clamp01(0.45f - rad) * 0.55f);
                    c.a = al;
                    px[iy * W + ix] = (Color32)c;
                }
            }
            return Finish(px, W, H);
        }

        static Texture2D BakeGloss()
        {
            var px = new Color32[W * H];
            for (int iy = 0; iy < H; iy++)
            {
                for (int ix = 0; ix < W; ix++)
                {
                    float u = ix / (float)(W - 1);
                    float v = iy / (float)(H - 1);
                    float streak = Mathf.Exp(-Mathf.Pow((u - 0.30f) / 0.16f, 2f) - Mathf.Pow((v - 0.24f) / 0.07f, 2f));
                    float curve = Mathf.Exp(-Mathf.Pow((u - 0.22f - v * 0.15f) / 0.10f, 2f) - Mathf.Pow((v - 0.20f) / 0.05f, 2f));
                    float dot = Mathf.Exp(-Mathf.Pow((u - 0.38f) / 0.035f, 2f) - Mathf.Pow((v - 0.16f) / 0.030f, 2f));
                    float a = Mathf.Clamp01(streak * 0.55f + curve * 0.40f + dot * 0.85f);
                    byte al = (byte)Mathf.RoundToInt(a * 255f);
                    px[iy * W + ix] = new Color32(255, 255, 255, al);
                }
            }
            return Finish(px, W, H);
        }

        // Thin wax walls of a pointy-top lattice, plus a soft gloss in each cell.
        // Straight alpha. Drawn over the honey and under the comb gloss. Fades out
        // at tiny sizes via LatticeFade, so the bake can stay one texture.
        static Texture2D BakeLattice()
        {
            var px = new Color32[W * H];
            float a = W * 0.5f;
            float r = H * 0.5f;
            float k = r / (2f * a);
            float norm = Mathf.Sqrt(1f + k * k);
            const float aw = 14f;
            float rh = 2f * aw / Mathf.Sqrt(3f);
            float row = 1.5f * rh;
            var wax = new Color(1f, 0.90f, 0.62f, 1f);
            var hi = new Color(1f, 0.98f, 0.88f, 1f);
            var gloss = new Color(1f, 1f, 1f, 1f);
            const float wallIn = 1.15f;
            const float wallSoft = 1.05f;

            for (int iy = 0; iy < H; iy++)
            {
                for (int ix = 0; ix < W; ix++)
                {
                    float pxX = ix + 0.5f;
                    float pxY = iy + 0.5f;
                    float ox = Mathf.Abs(pxX - a);
                    float oy = Mathf.Abs(pxY - r);
                    float outer = Mathf.Min(a - ox, (r - k * ox - oy) / norm);
                    float cover = Mathf.Clamp01(outer + 0.5f);
                    float edge = Mathf.Clamp01((outer - 2.4f) / 2.6f);
                    float mask = cover * edge;
                    if (mask <= 0f)
                    {
                        px[iy * W + ix] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    float lx = pxX - a;
                    float ly = pxY - r;
                    NearestCell(lx, ly, aw, row, out float cx, out float cy);
                    float d = HexInside(lx - cx, ly - cy, aw, rh);
                    float wallT = 1f - Mathf.Clamp01((d - wallIn) / wallSoft);
                    wallT = wallT * wallT * (3f - 2f * wallT);
                    float side = Mathf.Clamp01(0.42f - (lx - cx) / (aw * 2.4f) + (ly - cy) / (rh * 3.2f));
                    var wallCol = Color.Lerp(wax, hi, side * wallT);
                    float wallA = wallT * Mathf.Lerp(0.34f, 0.52f, side);

                    float nx = (lx - cx) / aw + 0.16f;
                    float ny = (ly - cy) / rh - 0.22f;
                    float g = Mathf.Exp(-(nx * nx) / 0.18f - (ny * ny) / 0.11f);
                    float glossA = g * 0.20f * Mathf.Clamp01((d - wallIn) / 2.4f);

                    float awall = wallA;
                    float ag = glossA * (1f - wallA);
                    float asum = awall + ag;
                    if (asum <= 0.004f)
                    {
                        px[iy * W + ix] = new Color32(0, 0, 0, 0);
                        continue;
                    }
                    var rgb = (wallCol * awall + gloss * ag) / asum;
                    rgb.a = asum * mask;
                    px[iy * W + ix] = (Color32)rgb;
                }
            }
            var tex = Finish(px, W, H);
            tex.name = "HoneyLattice";
            return tex;
        }

        static void NearestCell(float x, float y, float aw, float row, out float cx, out float cy)
        {
            int row0 = Mathf.FloorToInt(y / row);
            float best = 1e12f;
            cx = 0f;
            cy = 0f;
            float pitch = aw * 2f;
            for (int rr = row0 - 1; rr <= row0 + 1; rr++)
            {
                float off = (rr & 1) != 0 ? aw : 0f;
                int col0 = Mathf.FloorToInt((x - off) / pitch);
                for (int cc = col0 - 1; cc <= col0 + 1; cc++)
                {
                    float px = cc * pitch + off;
                    float py = rr * row;
                    float dx = x - px;
                    float dy = y - py;
                    float d2 = dx * dx + dy * dy;
                    if (d2 >= best) continue;
                    best = d2;
                    cx = px;
                    cy = py;
                }
            }
        }

        static float HexInside(float x, float y, float aw, float rh)
        {
            x = Mathf.Abs(x);
            y = Mathf.Abs(y);
            float k = rh / (2f * aw);
            float norm = Mathf.Sqrt(1f + k * k);
            return Mathf.Min(aw - x, (rh - k * x - y) / norm);
        }

        // Texture y grows up (iy 0 is the bulb). The neck is the top rows so it
        // meets the hex tip. Same amber stops as BakeFill, plus a small specular.
        static Texture2D BakeDrip(int which)
        {
            int tw = which == 0 ? 128 : 104;
            int th = which == 0 ? 176 : 150;
            var px = new Color32[tw * th];
            float cx = tw * (which == 0 ? 0.50f : 0.47f);
            float rad = tw * (which == 0 ? 0.40f : 0.36f);
            const float pad = 2.2f;
            float bulbCy = pad + rad;
            const float theta = 58f * Mathf.Deg2Rad;
            float joinY = bulbCy + Mathf.Cos(theta) * rad;
            float joinHalf = Mathf.Sin(theta) * rad;
            float neckHalf = Mathf.Max(3.2f, rad * (which == 0 ? 0.18f : 0.15f));
            float topY = th;
            float span = Mathf.Max(1f, topY - joinY);
            float cot = Mathf.Cos(theta) / Mathf.Sin(theta);
            float power = cot * span / Mathf.Max(0.001f, joinHalf - neckHalf);
            if (power < 1.15f) power = 1.15f;
            if (power > 4.5f) power = 4.5f;
            float specX = cx - rad * 0.26f;
            float specY = bulbCy + rad * 0.22f;
            float specS = (rad * 0.10f) * (rad * 0.10f);
            var deep = new Color(0.62f, 0.24f, 0.04f, 1f);
            var gold = new Color(1f, 0.84f, 0.28f, 1f);
            var glow = new Color(1f, 0.95f, 0.62f, 1f);
            const float aa = 1.45f;

            for (int iy = 0; iy < th; iy++)
            {
                float fy = iy + 0.5f;
                for (int ix = 0; ix < tw; ix++)
                {
                    float fx = ix + 0.5f;
                    float sd = DripDistance(fx, fy, cx, bulbCy, rad, joinY, joinHalf, neckHalf, topY, span, power);
                    float u = Mathf.Clamp01((aa - sd) / (2f * aa));
                    float al = u * u * (3f - 2f * u);
                    float depth = Mathf.Clamp01(-sd / (rad * 0.62f));
                    float low = Mathf.Clamp01((fy - pad) / (rad * 1.15f));
                    float baseT = (0.25f + 0.75f * depth) * (0.55f + 0.45f * low);
                    var c = Color.Lerp(deep, gold, baseT);
                    float gx = (fx - (cx - rad * 0.05f)) / (rad * 2.2f);
                    float gy = (fy - (bulbCy + rad * 0.25f)) / (rad * 2.2f);
                    float g = Mathf.Max(0f, 0.55f - Mathf.Sqrt(gx * gx + gy * gy)) * depth;
                    c = Color.Lerp(c, glow, g * 0.75f);
                    float sdx = fx - specX;
                    float sdy = fy - specY;
                    float spec = Mathf.Exp(-(sdx * sdx + sdy * sdy) / (2f * specS));
                    c = Color.Lerp(c, Color.white, spec * 0.95f);
                    c.a = al;
                    px[iy * tw + ix] = (Color32)c;
                }
            }
            var tex = Finish(px, tw, th);
            tex.name = which == 0 ? "HoneyDripA" : "HoneyDripB";
            return tex;
        }

        // Negative inside. Circle bulb below the join, power-curve neck above it.
        // The two meet on a shared tangent so the outline has no corner.
        static float DripDistance(float fx, float fy, float cx, float bulbCy, float rad,
            float joinY, float joinHalf, float neckHalf, float topY, float span, float power)
        {
            float dx = fx - cx;
            float dy = fy - bulbCy;
            float sdC = Mathf.Sqrt(dx * dx + dy * dy) - rad;
            float half = DripHalf(fy, bulbCy, rad, joinY, joinHalf, neckHalf, topY, span, power);
            float slope = DripSlope(fy, bulbCy, rad, joinY, joinHalf, neckHalf, topY, span, power, half);
            float sdG = -(half - Mathf.Abs(dx)) / Mathf.Sqrt(1f + slope * slope);
            if (fy < bulbCy - rad) return sdC;
            if (fy >= joinY) return sdG;
            return Mathf.Min(sdG, sdC);
        }

        static float DripHalf(float fy, float bulbCy, float rad, float joinY, float joinHalf,
            float neckHalf, float topY, float span, float power)
        {
            if (fy <= joinY)
            {
                float dy = fy - bulbCy;
                float inside = rad * rad - dy * dy;
                return inside > 0f ? Mathf.Sqrt(inside) : 0f;
            }
            float t = Mathf.Clamp01((topY - fy) / span);
            return neckHalf + (joinHalf - neckHalf) * Mathf.Pow(t, power);
        }

        static float DripSlope(float fy, float bulbCy, float rad, float joinY, float joinHalf,
            float neckHalf, float topY, float span, float power, float half)
        {
            if (fy <= joinY)
            {
                if (half < 0.001f) return 0f;
                return -(fy - bulbCy) / half;
            }
            float t = Mathf.Clamp01((topY - fy) / span);
            float dht = power <= 1f || t > 0f
                ? (joinHalf - neckHalf) * power * Mathf.Pow(Mathf.Max(t, 0f), power - 1f)
                : 0f;
            return dht * (-1f / span);
        }

        static Texture2D Finish(Color32[] px, int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }
    }
}
