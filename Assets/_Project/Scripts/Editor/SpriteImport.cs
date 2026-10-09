#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    public sealed class SpriteImport : AssetPostprocessor
    {
        // Bump forces a reimport so the platform overrides land without hand-editing metas.
        public override uint GetVersion() => 72;

        void OnPreprocessTexture()
        {
            if (assetPath.IndexOf("/Art/Resources/Sprites/") < 0) return;
            var imp = (TextureImporter)assetImporter;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            // NPOT plus a mip chain makes this Unity ignore the ASTC override and
            // store RGBA32. These sprites are drawn near screen size, so no mips.
            imp.mipmapEnabled = false;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.isReadable = NeedsCpuPixels(assetPath);
            if (assetPath.Contains("bg_")) imp.spritePixelsPerUnit = 96f;
            else if (assetPath.Contains("branch")) imp.spritePixelsPerUnit = 140f;
            else if (assetPath.Contains("feeder")) imp.spritePixelsPerUnit = 180f;
            else if (assetPath.Contains("bird_")) imp.spritePixelsPerUnit = 220f;
            else if (assetPath.Contains("fx_")) imp.spritePixelsPerUnit = 200f;
            else imp.spritePixelsPerUnit = 256f;
            if (assetPath.Contains("fx_vine")) imp.spritePivot = new Vector2(0.5f, 0.94f);
            else if (assetPath.Contains("fx_leaf")) imp.spritePivot = new Vector2(0.5f, 0.08f);
            else imp.spritePivot = new Vector2(0.5f, 0.5f);
            if (TryCompressedFormat(assetPath, out var format))
            {
                ApplyPlatformAstc(imp, "iPhone", format);
                ApplyPlatformAstc(imp, "Android", format);
            }
        }

        // CPU copy doubles imported size. Only the sprites whose pixels drive
        // behavior stay readable. Hands and letters already tolerate a miss.
        public static bool NeedsCpuPixels(string assetPath)
        {
            int slash = string.IsNullOrEmpty(assetPath) ? -1 : assetPath.LastIndexOf('/');
            string name = slash >= 0 ? assetPath.Substring(slash + 1) : assetPath;
            if (string.IsNullOrEmpty(name)) return false;
            if (name.StartsWith("bird_")) return true;
            if (name == "branch.png" || name == "branch_gift.png") return true;
            if (name.StartsWith("fx_flame_")) return true;
            if (name.StartsWith("fx_crown")) return true;
            if (name == "fx_bow.png" || name == "fx_bowtie.png") return true;
            return false;
        }

        public static bool IsAndroidOpaqueAstc(string assetPath)
        {
            return TryCompressedFormat(assetPath, out var format)
                && format == TextureImporterFormat.ASTC_6x6;
        }

        // Season backgrounds and bird pose sheets: ASTC 6x6. Poker faces (type on
        // the card) stay on 4x4 so the print does not blur at device size.
        public static bool TryCompressedFormat(string assetPath, out TextureImporterFormat format)
        {
            format = TextureImporterFormat.ASTC_6x6;
            if (string.IsNullOrEmpty(assetPath)) return false;
            int slash = assetPath.LastIndexOf('/');
            string name = slash >= 0 ? assetPath.Substring(slash + 1) : assetPath;
            if (name.StartsWith("fx_poker_face_"))
            {
                format = TextureImporterFormat.ASTC_4x4;
                return true;
            }
            if (name.StartsWith("bg_")) return true;
            if (!name.StartsWith("bird_")) return false;
            return name.EndsWith("_1.png") || name.EndsWith("_2.png")
                || name.EndsWith("_3.png") || name.EndsWith("_4.png");
        }

        public static void ApplyAndroidOpaqueAstc(TextureImporter imp)
        {
            ApplyPlatformAstc(imp, "Android", TextureImporterFormat.ASTC_6x6);
        }

        public static void ApplyPlatformAstc(TextureImporter imp, string platform, TextureImporterFormat format)
        {
            var settings = imp.GetPlatformTextureSettings(platform);
            int max = settings.maxTextureSize > 0 ? settings.maxTextureSize : 2048;
            settings.overridden = true;
            settings.format = format;
            settings.textureCompression = TextureImporterCompression.Compressed;
            settings.compressionQuality = 100;
            settings.crunchedCompression = false;
            settings.maxTextureSize = max;
            imp.SetPlatformTextureSettings(settings);
        }

        static bool PlatformReady(TextureImporter imp, string platform, TextureImporterFormat format)
        {
            var cur = imp.GetPlatformTextureSettings(platform);
            return cur.overridden && cur.format == format
                && cur.compressionQuality == 100 && !cur.crunchedCompression;
        }

        public static void EnsureAndroidOpaqueAstc()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/_Project/Art/Resources/Sprites" });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!TryCompressedFormat(path, out var format)) continue;
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) throw new System.Exception("ANDROIDBUILD missing " + path);
                bool readable = NeedsCpuPixels(path);
                if (imp.mipmapEnabled || imp.isReadable != readable
                    || !PlatformReady(imp, "Android", format) || !PlatformReady(imp, "iPhone", format))
                {
                    imp.mipmapEnabled = false;
                    imp.npotScale = TextureImporterNPOTScale.None;
                    imp.isReadable = readable;
                    ApplyPlatformAstc(imp, "Android", format);
                    ApplyPlatformAstc(imp, "iPhone", format);
                    imp.SaveAndReimport();
                    imp = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (imp == null) throw new System.Exception("ANDROIDBUILD reimport failed " + path);
                }
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var want = format == TextureImporterFormat.ASTC_4x4 ? TextureFormat.ASTC_4x4 : TextureFormat.ASTC_6x6;
                if (tex == null || tex.format != want || tex.mipmapCount > 1)
                    throw new System.Exception("ANDROIDBUILD " + path
                        + " format=" + (tex == null ? "null" : tex.format.ToString())
                        + " mips=" + (tex == null ? 0 : tex.mipmapCount)
                        + " want=" + want);
                string tag = format == TextureImporterFormat.ASTC_4x4 ? "astc4x4" : "astc6x6";
                UnityEngine.Debug.Log("ANDROIDBUILD " + tag + " " + path
                    + " " + tex.width + "x" + tex.height
                    + " readable=" + tex.isReadable);
            }
        }

        void OnPreprocessAudio()
        {
            if (assetPath.IndexOf("/Art/Resources/Audio/") < 0) return;
            var imp = (AudioImporter)assetImporter;
            var s = imp.defaultSampleSettings;
            bool theme = assetPath.Contains("garden-theme") || assetPath.Contains("splash-theme");
            bool battle = assetPath.Contains("badger-battle");
            if (theme)
            {
                s.loadType = AudioClipLoadType.Streaming;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.7f;
            }
            else if (battle)
            {
                s.loadType = AudioClipLoadType.CompressedInMemory;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.7f;
            }
            else
            {
                s.loadType = AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = AudioCompressionFormat.PCM;
                s.quality = 1f;
            }
            s.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            imp.defaultSampleSettings = s;
            imp.forceToMono = true;
            imp.loadInBackground = theme;
        }
    }
}
#endif
