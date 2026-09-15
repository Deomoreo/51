using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Project51.Unity;

namespace Project51.EditorTools
{
    /// <summary>
    /// Aggiunge la cornice tavolo procedurale (TableFeltRenderer) a GameScene.unity e
    /// riporta GameBackground a un fondo scuro piatto (l'immagine attuale, marrone/arancio,
    /// non e' coerente con la palette scura #091220 del resto dell'app - vedi UI_SPEC_Tavolo.md).
    /// </summary>
    public static class TableFeltBuilder
    {
        private const string ScenePath = "Assets/Scenes/GameScene.unity";

        [MenuItem("Tools/51/Build Table Felt Background")]
        private static void Build()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            RecolorGameBackground();

            var tableCenter = GameObject.Find("TableCardsContainer");
            if (tableCenter == null)
            {
                Debug.LogWarning("[TableFeltBuilder] 'TableCardsContainer' non trovato (prefab CardViewManager non in scena?) - uso l'origine come centro tavolo.");
            }

            // GetComponentsInChildren/FindObjectsOfType con includeInactive=true invece di
            // GameObject.Find: quest'ultimo trova solo UN oggetto attivo, lasciando indietro
            // eventuali duplicati inattivi o ulteriori (stesso bug di fondo gia' visto con
            // PlayerBannerManager: ogni rilancio del tool poteva accumularne altri).
            var existingFelts = Object.FindObjectsOfType<TableFeltRenderer>(true);
            if (existingFelts.Length > 0)
            {
                Debug.LogWarning($"[TableFeltBuilder] {existingFelts.Length} 'TableFelt' trovati: li rimuovo tutti e ricreo uno pulito.");
                foreach (var existing in existingFelts)
                {
                    Object.DestroyImmediate(existing.gameObject);
                }
            }

            var feltGO = new GameObject("TableFelt", typeof(TableFeltRenderer));
            var renderer = feltGO.GetComponent<TableFeltRenderer>();

            var so = new SerializedObject(renderer);
            if (tableCenter != null)
            {
                so.FindProperty("tableCenterReference").objectReferenceValue = tableCenter.transform;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[TableFeltBuilder] TableFelt creato. La cornice si genera a runtime in Play Mode (Awake/Start), non e' visibile in Edit Mode.");
        }

        private const string FlatBackgroundAssetPath = "Assets/Art/Generated/GameBackground_FlatNavy.png";

        private static void RecolorGameBackground()
        {
            var bgGO = GameObject.Find("GameBackground");
            if (bgGO == null)
            {
                Debug.LogWarning("[TableFeltBuilder] 'GameBackground' non trovato: salto il recolor.");
                return;
            }

            var sr = bgGO.GetComponent<SpriteRenderer>();
            if (sr == null) return;

            var sprite = GetOrCreateFlatBackgroundSprite();
            if (sprite == null) return;

            Undo.RecordObject(sr, "Recolor GameBackground");
            sr.sprite = sprite;
            sr.sortingOrder = -2; // sotto TableFelt (-1) e sotto le carte (0+)
            EditorUtility.SetDirty(sr);
        }

        /// <summary>
        /// Salva un vero asset .png su disco (invece di uno Sprite solo in memoria) cosi'
        /// il riferimento sopravvive al salvataggio/ricarico della scena senza ambiguita'.
        /// </summary>
        private static Sprite GetOrCreateFlatBackgroundSprite()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(FlatBackgroundAssetPath);
            if (existing != null) return existing;

            const int size = 8;
            var darkNavy = new Color32(0x09, 0x12, 0x20, 0xFF); // stessa tinta della barra superiore, sezione 2
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = darkNavy;
            tex.SetPixels32(pixels);
            tex.Apply();

            var directory = Path.GetDirectoryName(FlatBackgroundAssetPath);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            File.WriteAllBytes(FlatBackgroundAssetPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(FlatBackgroundAssetPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(FlatBackgroundAssetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = size;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(FlatBackgroundAssetPath);
        }
    }
}
