using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Project51.EditorTools
{
    /// <summary>
    /// Fonte di verita' unica per gli sprite Dragons Hoard: cerca per nome esatto tra i
    /// sub-sprite di tutti i file sotto Assets/UI/Sprites/DragonsHoard con Texture Type
    /// "Sprite (2D and UI)" e Sprite Mode "Multiple" (i fogli che l'utente slicea/rinomina
    /// a mano nello Sprite Editor di Unity - Icons.png, Icons2.png, Avatars.png,
    /// 14_emoticon_set.png). Sostituisce la vecchia pipeline a file PNG singoli +
    /// import_manifest.json: nessun passaggio di esportazione, resta sempre sincronizzata
    /// con qualsiasi slice/rinomina fatta in Editor. I file in Sprite Mode "Single" (i
    /// vecchi 69 PNG separati, se ancora presenti) vengono ignorati automaticamente.
    /// </summary>
    public static class DragonsHoardSprites
    {
        private const string SearchRoot = "Assets/UI/Sprites/DragonsHoard";

        private static Dictionary<string, Sprite> _cache;

        public static Sprite Find(string spriteName)
        {
            EnsureLoaded();
            if (_cache.TryGetValue(spriteName, out var sprite))
            {
                return sprite;
            }
            Debug.LogWarning($"[DragonsHoardSprites] Sprite non trovato: '{spriteName}' (nessuno slice con questo nome nei fogli Multiple sotto {SearchRoot}).");
            return null;
        }

        [MenuItem("Tools/Dragons Hoard/Reload Sprite Cache")]
        private static void ReloadCacheMenuItem()
        {
            _cache = null;
            EnsureLoaded();
            Debug.Log($"[DragonsHoardSprites] Cache ricaricata: {_cache.Count} sprite trovati sotto {SearchRoot}.");
        }

        private static void EnsureLoaded()
        {
            if (_cache != null)
            {
                return;
            }

            _cache = new Dictionary<string, Sprite>();

            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SearchRoot });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer) ||
                    importer.spriteImportMode != SpriteImportMode.Multiple)
                {
                    continue;
                }

                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (!(asset is Sprite sprite))
                    {
                        continue;
                    }

                    if (_cache.ContainsKey(sprite.name))
                    {
                        Debug.LogWarning($"[DragonsHoardSprites] Nome duplicato '{sprite.name}' in {path} - tengo il primo trovato, rinomina uno dei due slice per toglierlo.");
                        continue;
                    }

                    _cache[sprite.name] = sprite;
                }
            }
        }
    }
}
