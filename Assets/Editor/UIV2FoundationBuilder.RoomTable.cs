using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Project51.EditorTools
{
    public static partial class UIV2FoundationBuilder
    {
        private const string RoomBackgroundPath = "Assets/UI/Sprites/DragonsHoard/sprites_unity/sprites_unity/Immagine ChatGPT 25 set 2026, 16_14_59-4_upscayl_2x_upscayl-standard-4x.png";
        private const string TableKitDir = "Assets/Art/Table";

        /// <summary>
        /// 2.22: sfondo stanza (balcone notturno) su GameBackground e kit tavolo (feltro, vignetta,
        /// cornice 9-slice ritagliati dal foglio consegnato) su TableFeltRenderer. Rilanciabile.
        /// </summary>
        [MenuItem("Tools/UIV2/Apply Room And Table Kit (2.22)")]
        private static void ApplyRoomAndTableKit()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // 1882x3344: con 2048 i telefoni alti (2532 px) lo vedrebbero sgranato.
            var room = ImportTableSprite(RoomBackgroundPath, 100f, Vector4.zero, 4096);
            var felt = ImportTableSprite($"{TableKitDir}/table_felt.png", 100f, Vector4.zero, 1024);
            var vignette = ImportTableSprite($"{TableKitDir}/table_vignette.png", 100f, Vector4.zero, 1024);
            // PPU 350 = 1 px sorgente ~0.55 px di design: legno ~43 px sul tavolo da 1030.
            var frame = ImportTableSprite($"{TableKitDir}/table_frame.png", 350f, new Vector4(150, 150, 150, 150), 1024);

            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            var background = GameObject.Find("GameBackground").GetComponent<SpriteRenderer>();
            background.sprite = room;
            background.GetComponent<Project51.Unity.GameBackgroundFitter>().Apply(); // scala Cover salvata giusta
            EditorUtility.SetDirty(background);

            var table = Object.FindObjectOfType<Project51.Unity.TableFeltRenderer>(true);
            var so = new SerializedObject(table);
            so.FindProperty("feltSprite").objectReferenceValue = felt;
            so.FindProperty("vignetteSprite").objectReferenceValue = vignette;
            so.FindProperty("frameSprite").objectReferenceValue = frame;
            so.ApplyModifiedProperties();
            table.Rebuild();

            // Didascalie Emoji/ACCUSO: sul pavimento caldo della stanza servono il contorno navy.
            // Il materiale deve essere dello stesso font (Emoji medium, ACCUSO bold) o TMP lo scarta.
            foreach (var button in new[] { "EmojiButton", "AccusoButton" })
            {
                var caption = GameObject.Find("TableActionButtons").transform.Find(button + "/Caption").GetComponent<TMPro.TMP_Text>();
                caption.fontSharedMaterial = AssetDatabase.LoadAssetAtPath<Material>($"Assets/UIV2/Art/Fonts/{caption.font.name} Outline Navy.mat");
                EditorUtility.SetDirty(caption);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UIV2FoundationBuilder] 2.22: sfondo stanza e kit tavolo applicati in GameScene.");
        }

        private static Sprite ImportTableSprite(string path, float pixelsPerUnit, Vector4 border, int maxSize)
        {
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.spriteBorder = border;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; // richiesto da drawMode Sliced
            importer.SetTextureSettings(settings);
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = maxSize;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
