using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;

namespace Project51.UI51.EditorTools
{
    /// <summary>
    /// Fase 1: impostazioni di import delle immagini del handoff (Assets/UI51/Art) e uno Sprite Atlas V2 per area.
    /// Tutte le PNG: Sprite, alpha trasparente, niente mipmap, 100 PPU, max 2048.
    /// Emoticons: fogli 4x2 da 1024x512 tagliati in 8 fotogrammi 256x256 (&lt;id&gt;_0..7, riga alta prima) e
    /// registrati in Resources/UI51/EmoticonSet.asset nell'ordine di rete di UI51EmoticonSet.Order.
    /// Backgrounds: niente atlas (1153x2048, ognuno riempirebbe da solo una pagina).
    /// Idempotente: l'atlas si crea solo se manca, le impostazioni si riapplicano sempre.
    /// </summary>
    public static class UI51ImportBuilder
    {
        const string AtlasFolder = UI51Build.Root + "/Atlases";
        const string EmoticonSetPath = UI51Build.Root + "/Resources/UI51/EmoticonSet.asset";
        const int EmoCols = 4, EmoRows = 2;

        static readonly string[] AtlasAreas = { "Avatars", "Cards", "Common", "Emoticons", "Shapes" };
        static readonly string[] SpriteAreas = { "Avatars", "Backgrounds", "Cards", "Common", "Emoticons", "Shapes" };

        [MenuItem("Tools/UI51/Import Settings + Atlases")]
        private static void Menu() => Build();

        public static void Build()
        {
            foreach (string area in SpriteAreas)
            {
                string folder = $"{UI51Build.ArtRoot}/{area}";
                if (!AssetDatabase.IsValidFolder(folder)) { Debug.LogError($"[UI51 Import] Cartella mancante: {folder}"); continue; }
                foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!path.EndsWith(".png")) continue;
                    if (area == "Emoticons") ImportEmoticonSheet(path);
                    else ImportSprite(path);
                }
            }
            foreach (string area in AtlasAreas) EnsureAtlas(area);
            BuildEmoticonSet();
            AssetDatabase.SaveAssets();
            Debug.Log("[UI51 Import] Import e atlas completati.");
        }

        static void ImportSprite(string path)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) return;
            bool changed = ApplyCommon(ti, 2048, !path.Contains("/Backgrounds/"));
            if (ti.spriteImportMode != SpriteImportMode.Single) { ti.spriteImportMode = SpriteImportMode.Single; changed = true; }
            if (changed) ti.SaveAndReimport();
        }

        /// <summary>Foglio 4x2: fotogramma f in colonna f%4, riga f/4 dall'alto (origine Unity in basso a sinistra).</summary>
        static void ImportEmoticonSheet(string path)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) return;
            bool changed = ApplyCommon(ti, 1024, true);
            if (ti.spriteImportMode != SpriteImportMode.Multiple) { ti.spriteImportMode = SpriteImportMode.Multiple; changed = true; }

            ti.GetSourceTextureWidthAndHeight(out int w, out int h);
            if (w != 1024 || h != 512)
                Debug.LogWarning($"[UI51 Import] {path} e' {w}x{h}, atteso 1024x512: taglio comunque in 4x2.");
            float cw = w / (float)EmoCols, ch = h / (float)EmoRows;
            string key = System.IO.Path.GetFileNameWithoutExtension(path).Replace("_sheet_8frames", "");
            var sheet = Enumerable.Range(0, EmoticonPlayer.FrameCount).Select(f => new SpriteMetaData
            {
                name = key + "_" + f,
                rect = new Rect(f % EmoCols * cw, (EmoRows - 1 - f / EmoCols) * ch, cw, ch),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f)
            }).ToArray();
#pragma warning disable 618
            var old = ti.spritesheet;
            bool same = old != null && old.Length == sheet.Length &&
                        old.Zip(sheet, (a, b) => a.name == b.name && a.rect == b.rect).All(x => x);
            if (!same) { ti.spritesheet = sheet; changed = true; }
#pragma warning restore 618
            // 2022.3: il setter di spritesheet non sporca l'importer, senza SetDirty i rect non finiscono nel .meta.
            if (changed) { EditorUtility.SetDirty(ti); ti.SaveAndReimport(); }
        }

        /// <summary>atlased: sorgente di uno Sprite Atlas, va non compressa (comprime l'atlas).</summary>
        static bool ApplyCommon(TextureImporter ti, int maxSize, bool atlased)
        {
            bool changed = false;
            if (ti.textureType != TextureImporterType.Sprite) { ti.textureType = TextureImporterType.Sprite; changed = true; }
            if (!ti.alphaIsTransparency) { ti.alphaIsTransparency = true; changed = true; }
            if (ti.mipmapEnabled) { ti.mipmapEnabled = false; changed = true; }
            if (ti.maxTextureSize != maxSize) { ti.maxTextureSize = maxSize; changed = true; }
            if (!Mathf.Approximately(ti.spritePixelsPerUnit, 100f)) { ti.spritePixelsPerUnit = 100f; changed = true; }
            if (ti.wrapMode != TextureWrapMode.Clamp) { ti.wrapMode = TextureWrapMode.Clamp; changed = true; }
            if (atlased && ti.textureCompression != TextureImporterCompression.Uncompressed) { ti.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }
            return changed;
        }

        static void EnsureAtlas(string area)
        {
            UI51Build.EnsureFolder(AtlasFolder);
            string path = $"{AtlasFolder}/UI51_{area}.spriteatlasv2";
            if (AssetImporter.GetAtPath(path) == null)
            {
                var folder = AssetDatabase.LoadAssetAtPath<Object>($"{UI51Build.ArtRoot}/{area}");
                if (folder == null) { Debug.LogError($"[UI51 Import] Cartella mancante per l'atlas: {area}"); return; }
                var asset = new SpriteAtlasAsset();
                asset.Add(new[] { folder });
                SpriteAtlasAsset.Save(asset, path);
                AssetDatabase.ImportAsset(path);
                Debug.Log($"[UI51 Import] Creato {path}");
            }
            var importer = AssetImporter.GetAtPath(path) as SpriteAtlasImporter;
            if (importer == null) { Debug.LogError($"[UI51 Import] {path} non e' uno Sprite Atlas V2. Aborto."); return; }
            importer.includeInBuild = true;
            importer.packingSettings = new SpriteAtlasPackingSettings
            {
                blockOffset = 1,
                padding = 4,
                enableRotation = false,
                enableTightPacking = false,
                enableAlphaDilation = true
            };
            importer.textureSettings = new SpriteAtlasTextureSettings
            {
                readable = false,
                generateMipMaps = false,
                sRGB = true,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 1
            };
            var platform = importer.GetPlatformSettings("DefaultTexturePlatform");
            platform.maxTextureSize = 2048;
            importer.SetPlatformSettings(platform);
            importer.SaveAndReimport();
        }

        static void BuildEmoticonSet()
        {
            UI51Build.EnsureFolder(System.IO.Path.GetDirectoryName(EmoticonSetPath).Replace('\\', '/'));
            var set = AssetDatabase.LoadAssetAtPath<UI51EmoticonSet>(EmoticonSetPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<UI51EmoticonSet>();
                AssetDatabase.CreateAsset(set, EmoticonSetPath);
                Debug.Log($"[UI51 Import] Creato {EmoticonSetPath}");
            }
            foreach (string id in UI51EmoticonSet.Order)
            {
                string path = $"{UI51Build.ArtRoot}/Emoticons/emo_{id}_sheet_8frames.png";
                var frames = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
                    .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 99)
                    .ToArray();
                if (frames.Length != EmoticonPlayer.FrameCount)
                    Debug.LogError($"[UI51 Import] {path}: {frames.Length} fotogrammi invece di {EmoticonPlayer.FrameCount}.");
                set.Set(id, frames);
            }
            EditorUtility.SetDirty(set);
        }
    }
}
