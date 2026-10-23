#if UNITY_IOS
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

// PlayerSettings.targetDevice stays iPhone-only, so Unity's iOS export writes
// only the iPhone slots in Images.xcassets/AppIcon.appiconset. IosExportCompliance
// then sets TARGETED_DEVICE_FAMILY to 1,2 and App Store validation requires the
// iPad icons (152, 167, and the rest of the iPad set) plus the 1024 marketing
// icon. This step fills those slots from the existing 1024 art.
//
// The whole file is compiled only for iOS (UNITY_IOS). Android builds never see it.
public static class IosIpadIcons
{
    const string SourceRelative = "_Project/Art/Icons/app-icon-1024.png";

    // Point size is what Contents.json stores. Pixels = round(points * scale).
    // 76@2x = 152, 83.5@2x = 167. Those two are the sizes build 118 failed on.
    struct Slot
    {
        public string Idiom, Size, Scale, File;
        public Slot(string idiom, string size, string scale, string file)
        {
            Idiom = idiom;
            Size = size;
            Scale = scale;
            File = file;
        }
    }

    static readonly Slot[] Required =
    {
        new Slot("ipad", "20x20", "1x", "Icon-iPad-20x20@1x.png"),
        new Slot("ipad", "20x20", "2x", "Icon-iPad-20x20@2x.png"),
        new Slot("ipad", "29x29", "1x", "Icon-iPad-29x29@1x.png"),
        new Slot("ipad", "29x29", "2x", "Icon-iPad-29x29@2x.png"),
        new Slot("ipad", "40x40", "1x", "Icon-iPad-40x40@1x.png"),
        new Slot("ipad", "40x40", "2x", "Icon-iPad-40x40@2x.png"),
        new Slot("ipad", "76x76", "1x", "Icon-iPad-76x76@1x.png"),
        new Slot("ipad", "76x76", "2x", "Icon-iPad-76x76@2x.png"),
        new Slot("ipad", "83.5x83.5", "2x", "Icon-iPad-83.5x83.5@2x.png"),
        new Slot("ios-marketing", "1024x1024", "1x", "Icon-iOS-Marketing-1024.png"),
    };

    [PostProcessBuild(910)]
    public static void OnPostprocessBuild(BuildTarget target, string path)
    {
        if (target != BuildTarget.iOS) return;

        string iconSet = FindIconSet(path);
        if (iconSet == null)
            throw new Exception("IosIpadIcons: AppIcon.appiconset not found under " + path);

        string sourcePath = Path.Combine(Application.dataPath, SourceRelative);
        if (!File.Exists(sourcePath))
            throw new Exception("IosIpadIcons: missing source icon " + sourcePath);

        Rgba src = LoadTopDown(sourcePath);
        string jsonPath = Path.Combine(iconSet, "Contents.json");
        List<IconImage> images = Parse(File.Exists(jsonPath) ? File.ReadAllText(jsonPath) : "");

        var wrote = new List<string>();
        for (int i = 0; i < Required.Length; i++)
        {
            Slot slot = Required[i];
            string filename = Ensure(images, slot);
            int px = Pixels(slot.Size, slot.Scale);
            Color32[] scaled = (px == src.Width && px == src.Height)
                ? src.Pixels
                : Resize(src.Pixels, src.Width, src.Height, px, px);
            byte[] png = PngRgb(FlattenRgb(scaled), px, px);
            File.WriteAllBytes(Path.Combine(iconSet, filename), png);
            wrote.Add(px + "x" + px + " " + filename);
        }

        File.WriteAllText(jsonPath, ToJson(Dedupe(images)));
        Debug.Log("IosIpadIcons wrote " + string.Join(", ", wrote));
    }

    static string FindIconSet(string buildPath)
    {
        string direct = Path.Combine(buildPath, "Unity-iPhone", "Images.xcassets", "AppIcon.appiconset");
        if (Directory.Exists(direct)) return direct;
        string found = null;
        Walk(buildPath, 0, ref found);
        return found;
    }

    static void Walk(string dir, int depth, ref string found)
    {
        if (found != null || depth > 3 || !Directory.Exists(dir)) return;
        string name = Path.GetFileName(dir);
        if (name == "Pods" || name == "Libraries" || name == "Il2CppOutputProject" || name == "DerivedData")
            return;
        if (name == "AppIcon.appiconset")
        {
            found = dir;
            return;
        }
        string[] subs;
        try { subs = Directory.GetDirectories(dir); }
        catch (IOException) { return; }
        for (int i = 0; i < subs.Length; i++)
            Walk(subs[i], depth + 1, ref found);
    }

    struct Rgba
    {
        public Color32[] Pixels;
        public int Width;
        public int Height;
    }

    static Rgba LoadTopDown(string path)
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            if (!tex.LoadImage(File.ReadAllBytes(path)))
                throw new Exception("IosIpadIcons: could not decode " + path);
            int w = tex.width;
            int h = tex.height;
            Color32[] bottom = tex.GetPixels32();
            var top = new Color32[bottom.Length];
            for (int y = 0; y < h; y++)
                Array.Copy(bottom, (h - 1 - y) * w, top, y * w, w);
            return new Rgba { Pixels = top, Width = w, Height = h };
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(tex);
        }
    }

    // Area average. Each destination pixel is the coverage-weighted mean of the
    // source pixels it covers, which is the right filter when scaling 1024 down.
    static Color32[] Resize(Color32[] src, int sw, int sh, int dw, int dh)
    {
        var dst = new Color32[dw * dh];
        double scaleX = (double)sw / dw;
        double scaleY = (double)sh / dh;
        for (int y = 0; y < dh; y++)
        {
            double y0 = y * scaleY;
            double y1 = (y + 1) * scaleY;
            int iy0 = (int)Math.Floor(y0);
            int iy1 = (int)Math.Ceiling(y1);
            if (iy1 > sh) iy1 = sh;
            for (int x = 0; x < dw; x++)
            {
                double x0 = x * scaleX;
                double x1 = (x + 1) * scaleX;
                int ix0 = (int)Math.Floor(x0);
                int ix1 = (int)Math.Ceiling(x1);
                if (ix1 > sw) ix1 = sw;
                double r = 0, g = 0, b = 0, a = 0, wsum = 0;
                for (int yy = iy0; yy < iy1; yy++)
                {
                    double ycov = Math.Min(yy + 1, y1) - Math.Max(yy, y0);
                    if (ycov <= 0) continue;
                    int row = yy * sw;
                    for (int xx = ix0; xx < ix1; xx++)
                    {
                        double xcov = Math.Min(xx + 1, x1) - Math.Max(xx, x0);
                        if (xcov <= 0) continue;
                        double w = xcov * ycov;
                        Color32 p = src[row + xx];
                        r += p.r * w;
                        g += p.g * w;
                        b += p.b * w;
                        a += p.a * w;
                        wsum += w;
                    }
                }
                if (wsum <= 0) wsum = 1;
                dst[y * dw + x] = new Color32(
                    Channel(r / wsum),
                    Channel(g / wsum),
                    Channel(b / wsum),
                    Channel(a / wsum));
            }
        }
        return dst;
    }

    static byte Channel(double v)
    {
        if (v <= 0) return 0;
        if (v >= 255) return 255;
        return (byte)Math.Round(v, MidpointRounding.AwayFromZero);
    }

    // App Store rejects an alpha channel even when every pixel is opaque (ITMS-90717).
    static byte[] FlattenRgb(Color32[] px)
    {
        var rgb = new byte[px.Length * 3];
        for (int i = 0; i < px.Length; i++)
        {
            Color32 p = px[i];
            byte r, g, b;
            if (p.a == 255)
            {
                r = p.r;
                g = p.g;
                b = p.b;
            }
            else
            {
                int a = p.a;
                r = (byte)((p.r * a + 255 * (255 - a) + 127) / 255);
                g = (byte)((p.g * a + 255 * (255 - a) + 127) / 255);
                b = (byte)((p.b * a + 255 * (255 - a) + 127) / 255);
            }
            rgb[i * 3] = r;
            rgb[i * 3 + 1] = g;
            rgb[i * 3 + 2] = b;
        }
        return rgb;
    }

    class IconImage
    {
        public string Size, Idiom, Filename, Scale, Role, Subtype;
    }

    static List<IconImage> Parse(string json)
    {
        var list = new List<IconImage>();
        if (string.IsNullOrEmpty(json)) return list;
        MatchCollection matches = Regex.Matches(json, @"\{[^{}]*\}");
        for (int i = 0; i < matches.Count; i++)
        {
            string body = matches[i].Value;
            string idiom = Field(body, "idiom");
            string size = Field(body, "size");
            string scale = Field(body, "scale");
            if (idiom == null || size == null || scale == null) continue;
            list.Add(new IconImage
            {
                Idiom = idiom,
                Size = size,
                Scale = scale,
                Filename = Field(body, "filename"),
                Role = Field(body, "role"),
                Subtype = Field(body, "subtype"),
            });
        }
        return list;
    }

    static string Field(string obj, string key)
    {
        Match m = Regex.Match(obj, "\"" + key + "\"\\s*:\\s*\"([^\"]*)\"");
        return m.Success ? m.Groups[1].Value : null;
    }

    static string Ensure(List<IconImage> images, Slot slot)
    {
        string filename = null;
        for (int i = 0; i < images.Count; i++)
        {
            IconImage im = images[i];
            if (im.Idiom != slot.Idiom || im.Size != slot.Size || im.Scale != slot.Scale) continue;
            if (filename == null) filename = SafeFile(im.Filename, slot.File);
            im.Filename = filename;
        }
        if (filename != null) return filename;
        images.Add(new IconImage
        {
            Idiom = slot.Idiom,
            Size = slot.Size,
            Scale = slot.Scale,
            Filename = slot.File,
        });
        return slot.File;
    }

    static string SafeFile(string name, string fallback)
    {
        if (string.IsNullOrEmpty(name)) return fallback;
        if (name.IndexOf('/') >= 0 || name.IndexOf('\\') >= 0 || name.IndexOf("..", StringComparison.Ordinal) >= 0)
            return fallback;
        if (name.IndexOf('"') >= 0) return fallback;
        if (!name.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return fallback;
        return name;
    }

    static List<IconImage> Dedupe(List<IconImage> images)
    {
        var seen = new HashSet<string>();
        var dst = new List<IconImage>();
        for (int i = 0; i < images.Count; i++)
        {
            IconImage im = images[i];
            if (!seen.Add(im.Idiom + "\n" + im.Size + "\n" + im.Scale)) continue;
            dst.Add(im);
        }
        return dst;
    }

    static int Pixels(string size, string scale)
    {
        string dim = size.Split('x')[0];
        double points = double.Parse(dim, CultureInfo.InvariantCulture);
        int factor = int.Parse(scale.Substring(0, scale.Length - 1), CultureInfo.InvariantCulture);
        return (int)Math.Round(points * factor, MidpointRounding.AwayFromZero);
    }

    static string ToJson(List<IconImage> images)
    {
        var sb = new StringBuilder();
        sb.Append("{\n  \"images\" : [\n");
        for (int i = 0; i < images.Count; i++)
        {
            IconImage im = images[i];
            sb.Append("    {\n");
            Field(sb, "size", im.Size);
            Field(sb, "idiom", im.Idiom);
            if (!string.IsNullOrEmpty(im.Filename)) Field(sb, "filename", im.Filename);
            if (!string.IsNullOrEmpty(im.Role)) Field(sb, "role", im.Role);
            if (!string.IsNullOrEmpty(im.Subtype)) Field(sb, "subtype", im.Subtype);
            sb.Append("      \"scale\" : \"").Append(im.Scale).Append("\"\n");
            sb.Append(i + 1 < images.Count ? "    },\n" : "    }\n");
        }
        sb.Append("  ],\n  \"info\" : {\n    \"version\" : 1,\n    \"author\" : \"xcode\"\n  }\n}\n");
        return sb.ToString();
    }

    static void Field(StringBuilder sb, string key, string value)
    {
        sb.Append("      \"").Append(key).Append("\" : \"").Append(value).Append("\",\n");
    }

    static readonly uint[] CrcTable = MakeCrc();

    static uint[] MakeCrc()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            uint c = n;
            for (int k = 0; k < 8; k++)
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            table[n] = c;
        }
        return table;
    }

    static uint Crc32(byte[] data, int offset, int count)
    {
        uint c = 0xFFFFFFFFu;
        for (int i = 0; i < count; i++)
            c = CrcTable[(c ^ data[offset + i]) & 0xFF] ^ (c >> 8);
        return c ^ 0xFFFFFFFFu;
    }

    // Color type 2 (RGB). No tRNS, no alpha.
    static byte[] PngRgb(byte[] rgb, int w, int h)
    {
        int stride = w * 3;
        var raw = new byte[(stride + 1) * h];
        for (int y = 0; y < h; y++)
        {
            int dst = y * (stride + 1);
            raw[dst] = 0;
            Buffer.BlockCopy(rgb, y * stride, raw, dst + 1, stride);
        }
        using (var ms = new MemoryStream())
        {
            ms.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, 0, 8);
            Chunk(ms, "IHDR", Ihdr(w, h));
            Chunk(ms, "IDAT", Zlib(raw));
            Chunk(ms, "IEND", new byte[0]);
            return ms.ToArray();
        }
    }

    static byte[] Ihdr(int w, int h)
    {
        var d = new byte[13];
        PutU32(d, 0, (uint)w);
        PutU32(d, 4, (uint)h);
        d[8] = 8;
        d[9] = 2;
        return d;
    }

    static void PutU32(byte[] d, int at, uint v)
    {
        d[at] = (byte)(v >> 24);
        d[at + 1] = (byte)(v >> 16);
        d[at + 2] = (byte)(v >> 8);
        d[at + 3] = (byte)v;
    }

    static void Chunk(Stream ms, string type, byte[] data)
    {
        WriteU32(ms, (uint)data.Length);
        byte[] typeBytes = Encoding.ASCII.GetBytes(type);
        ms.Write(typeBytes, 0, 4);
        if (data.Length > 0) ms.Write(data, 0, data.Length);
        var crcBuf = new byte[4 + data.Length];
        Buffer.BlockCopy(typeBytes, 0, crcBuf, 0, 4);
        if (data.Length > 0) Buffer.BlockCopy(data, 0, crcBuf, 4, data.Length);
        WriteU32(ms, Crc32(crcBuf, 0, crcBuf.Length));
    }

    static void WriteU32(Stream s, uint v)
    {
        s.WriteByte((byte)(v >> 24));
        s.WriteByte((byte)(v >> 16));
        s.WriteByte((byte)(v >> 8));
        s.WriteByte((byte)v);
    }

    static byte[] Zlib(byte[] data)
    {
        byte[] deflated;
        using (var raw = new MemoryStream())
        {
            using (var deflate = new DeflateStream(raw, CompressionLevel.Optimal, true))
                deflate.Write(data, 0, data.Length);
            deflated = raw.ToArray();
        }
        uint a = 1, b = 0;
        const uint mod = 65521;
        for (int i = 0; i < data.Length; i++)
        {
            a = (a + data[i]) % mod;
            b = (b + a) % mod;
        }
        uint adler = (b << 16) | a;
        var zlib = new byte[deflated.Length + 6];
        zlib[0] = 0x78;
        zlib[1] = 0x9C;
        Buffer.BlockCopy(deflated, 0, zlib, 2, deflated.Length);
        zlib[zlib.Length - 4] = (byte)(adler >> 24);
        zlib[zlib.Length - 3] = (byte)(adler >> 16);
        zlib[zlib.Length - 2] = (byte)(adler >> 8);
        zlib[zlib.Length - 1] = (byte)adler;
        return zlib;
    }
}
#endif
