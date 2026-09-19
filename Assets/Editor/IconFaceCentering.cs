using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.EditorTools
{
    /// <summary>
    /// I2 - icone non centrate nei riquadri piccoli. I riquadri sq_blue e sq_gold hanno un bordo 3D
    /// piu' spesso sotto (e un po' d'ombra a destra): la faccia chiara, dove l'occhio cerca il centro,
    /// sta piu' in alto del centro del rettangolo. In piu' molte icone hanno margini trasparenti
    /// asimmetrici. Qui il centro VISIBILE di ogni icona viene messo sul centro della faccia del suo
    /// riquadro, tenendo conto dello spessore reale del bordo (9-slice, pixelsPerUnitMultiplier) a ogni
    /// dimensione. Tocca solo icone con ancora puntuale che stanno gia' vicino al centro del riquadro
    /// (entro 12 px: i badge d'angolo restano dove sono). Rilanciabile.
    /// </summary>
    public static class IconFaceCentering
    {
        // Centro della faccia rispetto al centro dello sprite, in pixel dello sprite (x a destra, y in su).
        // Misurati sull'anello scuro interno della faccia (Icons.png).
        private static readonly Dictionary<string, Vector2> FaceCenterOffset = new Dictionary<string, Vector2>
        {
            { "sq_blue", new Vector2(-2.5f, 4.5f) },
            { "sq_gold", new Vector2(-0.5f, 6.5f) },
        };

        private static readonly string[] Scenes = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/GameScene.unity" };
        private const float MaxExistingOffset = 12f; // oltre: e' un badge d'angolo o simile, non un'icona centrale
        private const float Tolerance = 0.75f;

        private static readonly Dictionary<Sprite, Rect> visibleCache = new Dictionary<Sprite, Rect>();

        [MenuItem("Tools/UIV2/Center Icons On Button Faces")]
        private static void Apply()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            visibleCache.Clear();
            int moved = 0;
            foreach (var path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int movedHere = 0;
                foreach (var icon in Resources.FindObjectsOfTypeAll<Image>())
                {
                    if (icon.gameObject.scene != scene || icon.sprite == null) continue;
                    if (TryCenter(icon, out string log)) { movedHere++; Debug.Log("[IconFaceCentering] " + log); }
                }
                if (movedHere > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
                moved += movedHere;
            }
            Debug.Log($"[IconFaceCentering] {moved} icone centrate sulla faccia del riquadro.");
        }

        private static bool TryCenter(Image icon, out string log)
        {
            log = null;
            var rect = icon.rectTransform;
            var box = rect.parent != null ? rect.parent.GetComponent<Image>() : null;
            if (box == null || box.sprite == null || !FaceCenterOffset.TryGetValue(box.sprite.name, out var faceOffsetSprite)) return false;
            if (icon.type != Image.Type.Simple || rect.anchorMin != rect.anchorMax) return false; // ancora puntuale, non stirata
            var boxRect = box.rectTransform;
            if (Vector2.Distance((Vector2)rect.localPosition, boxRect.rect.center) > MaxExistingOffset) return false;

            Vector2 faceCenter = boxRect.rect.center + FaceOffsetInBox(box, faceOffsetSprite);
            Vector2 iconVisibleOffset = VisibleCenterOffset(icon); // dal pivot, in unita' locali dell'icona
            Vector2 desiredLocal = faceCenter - Vector2.Scale(iconVisibleOffset, rect.localScale);
            Vector2 delta = desiredLocal - (Vector2)rect.localPosition;
            if (delta.magnitude < Tolerance) return false;

            Undo.RecordObject(rect, "Center icon on face");
            rect.anchoredPosition += delta;
            EditorUtility.SetDirty(rect);
            log = $"{PathOf(rect)}: {icon.sprite.name} in {box.sprite.name} {boxRect.rect.size} spostata di ({delta.x:0.0}, {delta.y:0.0})";
            return true;
        }

        /// <summary>Scostamento della faccia dal centro del riquadro, in unita' locali del riquadro.</summary>
        private static Vector2 FaceOffsetInBox(Image box, Vector2 spriteOffset)
        {
            var sprite = box.sprite;
            var size = box.rectTransform.rect.size;
            if (box.type == Image.Type.Sliced && sprite.border != Vector4.zero)
            {
                // Le zone del bordo si disegnano a 1/pixelsPerUnit; se il rettangolo e' piu' piccolo dei bordi, si riducono.
                float perPixel = 1f / Mathf.Max(0.0001f, box.pixelsPerUnit * box.pixelsPerUnitMultiplier);
                var border = sprite.border; // x=sinistra y=basso z=destra w=alto
                float sx = Mathf.Min(1f, size.x / Mathf.Max(0.0001f, (border.x + border.z) * perPixel));
                float sy = Mathf.Min(1f, size.y / Mathf.Max(0.0001f, (border.y + border.w) * perPixel));
                return new Vector2(spriteOffset.x * perPixel * sx, spriteOffset.y * perPixel * sy);
            }
            return new Vector2(spriteOffset.x * size.x / sprite.rect.width, spriteOffset.y * size.y / sprite.rect.height);
        }

        /// <summary>Centro dei pixel visibili dell'icona rispetto al suo pivot, in unita' locali.</summary>
        private static Vector2 VisibleCenterOffset(Image icon)
        {
            var rect = icon.rectTransform.rect;
            var sprite = icon.sprite;
            Vector2 drawSize = rect.size;
            Vector2 drawOrigin = rect.position;
            if (icon.preserveAspect)
            {
                float spriteAspect = sprite.rect.width / sprite.rect.height;
                if (rect.width / rect.height > spriteAspect) { float w = rect.height * spriteAspect; drawOrigin.x += (rect.width - w) * 0.5f; drawSize.x = w; }
                else { float h = rect.width / spriteAspect; drawOrigin.y += (rect.height - h) * 0.5f; drawSize.y = h; }
            }
            var visible = VisibleRect(sprite);
            return drawOrigin + Vector2.Scale(visible.center, drawSize);
        }

        /// <summary>Riquadro dei pixel con alfa &gt; 0,15, normalizzato 0..1 sullo sprite.</summary>
        private static Rect VisibleRect(Sprite sprite)
        {
            if (visibleCache.TryGetValue(sprite, out var cached)) return cached;
            var tex = sprite.texture;
            var previous = RenderTexture.active; // prima del Blit: il Blit cambia la RenderTexture attiva
            var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(tex, rt);
            RenderTexture.active = rt;
            var r = sprite.textureRect;
            var copy = new Texture2D((int)r.width, (int)r.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(r.x, r.y, r.width, r.height), 0, 0);
            copy.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);

            var pixels = copy.GetPixels32();
            int w = copy.width, h = copy.height, minX = w, minY = h, maxX = -1, maxY = -1;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (pixels[y * w + x].a <= 38) continue;
                if (x < minX) minX = x; if (x > maxX) maxX = x;
                if (y < minY) minY = y; if (y > maxY) maxY = y;
            }
            Object.DestroyImmediate(copy);
            var result = maxX < 0 ? new Rect(0f, 0f, 1f, 1f)
                : Rect.MinMaxRect(minX / (float)w, minY / (float)h, (maxX + 1) / (float)w, (maxY + 1) / (float)h);
            visibleCache[sprite] = result;
            return result;
        }

        private static string PathOf(Transform t)
        {
            string path = t.name;
            for (var p = t.parent; p != null; p = p.parent) path = p.name + "/" + path;
            return path;
        }
    }
}
