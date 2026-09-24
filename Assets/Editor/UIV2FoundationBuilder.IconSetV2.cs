using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.EditorTools
{
    /// <summary>
    /// Icone v2 (docs/ui/asset-refresh-brief.md): PNG singoli in SheetsDir che sostituiscono i vecchi sprite
    /// del kit. Importa i PNG a 256 px e scambia i riferimenti in MainMenu, GameScene e nei prefab UIV2;
    /// i builder caricano gia' i file nuovi con NewIcon. Rilanciabile dopo aver sostituito un PNG.
    /// Restano sul vecchio sprite: la X "rimuovi" della Collezione (serve un meno, ic_remove) e l'icona
    /// "Condividi" della fila inviti (le vicine lucchetto/persona sono ancora del kit vecchio).
    /// </summary>
    public static partial class UIV2FoundationBuilder
    {
        private static readonly string[] IconSetV2 =
            { "ic_chest", "ic_trophy", "ic_mail", "ic_option", "ic_arrowback", "ic_volume", "ic_questionmark", "ic_exit", "ic_X" };

        private static Sprite NewIcon(string name) => LoadSprite(SheetsDir + "/" + name + ".png", name);

        [MenuItem("Tools/UIV2/Apply Icon Set v2")]
        private static void ApplyIconSetV2()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach (var name in IconSetV2)
            {
                string path = SheetsDir + "/" + name + ".png";
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                // Sprite ritagliato sulla parte visibile, come quelli del kit: i PNG hanno ~10% di margine
                // trasparente per lato e i riquadri dei builder sono pensati per icone senza margine.
                importer.spriteImportMode = SpriteImportMode.Multiple;
#pragma warning disable 618
                importer.spritesheet = new[] { new SpriteMetaData { name = name, rect = VisibleRect(path), alignment = (int)SpriteAlignment.Center, pivot = new Vector2(.5f, .5f) } };
#pragma warning restore 618
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 256; // piu' grande a schermo: la coppa di Online, ~200 px
                importer.SaveAndReimport();
                _sheetCache.Remove(path);
            }

            var swap = new Dictionary<Sprite, Sprite>
            {
                { LoadSprite(IconsPath, "chest_green"), NewIcon("ic_chest") },
                { LoadSprite(IconsPath, "ic_trophy"), NewIcon("ic_trophy") },
                { LoadSprite(IconsPath, "ic_mail"), NewIcon("ic_mail") },
                { LoadSprite(IconsPath, "ic_gear"), NewIcon("ic_option") },
                { LoadSprite(IconsPath, "ic_x"), NewIcon("ic_X") },
                { LoadSprite(SheetsDir + "/ic_arrow_left.png", "ic_arrow_left"), NewIcon("ic_arrowback") },
            };
            var oldX = LoadSprite(IconsPath, "ic_x");
            var exit = NewIcon("ic_exit");
            int count = 0;

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { ComponentsPrefabDir.Substring(0, ComponentsPrefabDir.LastIndexOf('/')) }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                int swapped = SwapSprites(root, swap, oldX, exit);
                if (swapped > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
                count += swapped;
            }
            foreach (var scenePath in new[] { MainMenuScenePath, GameScenePath })
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                foreach (var root in scene.GetRootGameObjects()) count += SwapSprites(root, swap, oldX, exit);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log("[IconSetV2] " + count + " riferimenti aggiornati");
        }

        private static int SwapSprites(GameObject root, Dictionary<Sprite, Sprite> swap, Sprite oldX, Sprite exit)
        {
            int count = 0;
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null || component is Transform) continue;
                string path = AnimationUtility.CalculateTransformPath(component.transform, root.transform);
                if (path.Contains("RemoveButton") || path.Contains("InviteCondividi")) continue;
                var so = new SerializedObject(component);
                var property = so.GetIterator();
                bool changed = false;
                Sprite imageOld = null, imageNew = null;
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    var sprite = property.objectReferenceValue as Sprite;
                    if (sprite == null || !swap.TryGetValue(sprite, out var replacement)) continue;
                    // "Abbandona partita" e' un'uscita, non una chiusura: porta (ic_exit), come chiede il brief.
                    if (sprite == oldX && path.EndsWith("Abandon/Icon")) replacement = exit;
                    property.objectReferenceValue = replacement;
                    if (component is Image && property.propertyPath == "m_Sprite") { imageOld = sprite; imageNew = replacement; }
                    changed = true;
                    count++;
                }
                if (!changed) continue;
                so.ApplyModifiedPropertiesWithoutUndo();
                if (!(component is Image image)) continue;
                // Le icone v2 hanno il colore dipinto: niente tinta sopra (resta l'alfa).
                if (image.color.r < 1f || image.color.g < 1f || image.color.b < 1f)
                {
                    Debug.Log("[IconSetV2] tinta rimossa da " + path + " (" + image.color + ")");
                    image.color = new Color(1f, 1f, 1f, image.color.a);
                }
                if (imageOld != null) FitVisible(image, imageOld, imageNew);
            }
            return count;
        }

        private static Rect VisibleRect(string path)
        {
            var texture = new Texture2D(2, 2);
            texture.LoadImage(System.IO.File.ReadAllBytes(path));
            int w = texture.width, h = texture.height, minX = w, minY = h, maxX = -1, maxY = -1;
            var pixels = texture.GetPixels32();
            Object.DestroyImmediate(texture);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (pixels[y * w + x].a > 2) { minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); }
            const int margin = 4; // non tagliare l'ombra morbida
            minX = Mathf.Max(0, minX - margin); minY = Mathf.Max(0, minY - margin);
            maxX = Mathf.Min(w - 1, maxX + margin); maxY = Mathf.Min(h - 1, maxY + margin);
            return new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        // Il vecchio sprite e il nuovo hanno proporzioni e margini diversi: la parte
        // visibile della nuova icona prende il posto e la misura di quella vecchia (stesso criterio di MockSprite).
        private static void FitVisible(Image image, Sprite oldSprite, Sprite newSprite)
        {
            var rect = image.rectTransform;
            if (image.type != Image.Type.Simple || rect.anchorMin != rect.anchorMax) { image.preserveAspect = true; return; }
            Vector2 size = rect.sizeDelta, drawn = size, o = oldSprite.rect.size;
            if (image.preserveAspect) drawn = o * Mathf.Min(size.x / o.x, size.y / o.y);
            var po = VisiblePadding(oldSprite);
            Vector2 so = new Vector2(drawn.x / o.x, drawn.y / o.y);
            var visible = new Vector2((o.x - po.left - po.right) * so.x, (o.y - po.top - po.bottom) * so.y);
            var visibleCentre = new Vector2((po.left - po.right) * so.x, (po.bottom - po.top) * so.y) * 0.5f;

            var pn = VisiblePadding(newSprite); var n = newSprite.rect.size;
            float s = Mathf.Min(visible.x / (n.x - pn.left - pn.right), visible.y / (n.y - pn.top - pn.bottom));
            var newSize = n * s;
            var newCentre = visibleCentre - new Vector2(pn.left - pn.right, pn.bottom - pn.top) * s * 0.5f;
            var resizeShift = Vector2.Scale(new Vector2(0.5f, 0.5f) - rect.pivot, newSize - size);
            rect.anchoredPosition += newCentre - resizeShift;
            rect.sizeDelta = newSize;
            image.preserveAspect = false;
        }
    }
}
