#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    public sealed class SpriteImport : AssetPostprocessor
    {
        // Bump forces a reimport so the platform overrides land without hand-editing metas.
        public override uint GetVersion() => 71;

        void OnPreprocessTexture()
        {
            if (assetPath.IndexOf("/Art/Resources/Sprites/") < 0) return;
            var imp = (TextureImporter)assetImporter;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.isReadable = true;
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
                imp.mipmapEnabled = true;
                ApplyPlatformAstc(imp, "iPhone", format);
                ApplyPlatformAstc(imp, "Android", format);
            }
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
                if (!imp.mipmapEnabled || !PlatformReady(imp, "Android", format) || !PlatformReady(imp, "iPhone", format))
                {
                    imp.mipmapEnabled = true;
                    ApplyPlatformAstc(imp, "Android", format);
                    ApplyPlatformAstc(imp, "iPhone", format);
                    imp.SaveAndReimport();
                    imp = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (imp == null) throw new System.Exception("ANDROIDBUILD reimport failed " + path);
                }
                string tag = format == TextureImporterFormat.ASTC_4x4 ? "astc4x4" : "astc6x6";
                UnityEngine.Debug.Log("ANDROIDBUILD " + tag + " " + path);
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
