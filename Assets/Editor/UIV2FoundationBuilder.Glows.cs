using Project51.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.EditorTools
{
    /// <summary>
    /// C5 - bagliore morbido dietro i pulsanti principali, come nei mockup:
    /// - MainMenu: GIOCA della Home (home_B2), GIOCA COME OSPITE della schermata iniziale (01),
    ///   riga selezionata del pannello Modalita' (panel_modalita_v2, teal, segue la selezione);
    /// - GameScene: trofeo e RIVINCITA dei risultati (12).
    /// Accuso ha gia' il suo (AccusoWindowGlow, pulsa durante la finestra dell'accuso).
    ///
    /// Il bagliore e' un fratello messo subito prima del bersaglio (un figlio verrebbe disegnato
    /// sopra l'immagine del pulsante) e si misura sul bordo VISIBILE del bersaglio, non sul suo
    /// rettangolo: btn_gold_long ha molto margine trasparente, anche dentro i bordi 9-slice.
    /// Rilanciabile: rimuove e ricrea solo gli oggetti "SoftGlow".
    /// </summary>
    public static partial class UIV2FoundationBuilder
    {
        private const string SoftGlowName = "SoftGlow";

        // Il cerchio sfumato del kit resta per il pulviscolo (UIV2MoteField).
        private const string GlowCirclePath = GlowSheetDir + "Bagliore morbido cerchio.png";
        private const float GlowCircleBorder = 626f;

        // Bagliore dei pulsanti: gradiente tecnico generato qui (non e' un disegno del kit), 256 px,
        // alfa = smoothstep della distanza dal centro. Smoothstep parte e arriva con pendenza zero:
        // nessun "gradino" dove il bagliore finisce, che era quello che lo faceva sembrare una lastra.
        // 9-slice al centro (bordo 127): angoli a quarto di cerchio, cioe' una pillola.
        private const string GlowPillPath = "Assets/UIV2/Art/Generated/glow_soft_pill.png";
        private const int GlowPillSize = 256;
        private const float GlowPillBorder = 127f;

        // Colori pieni: al bordo del pulsante il bagliore e' gia' a meta' (parte sotto il pulsante).
        private static readonly Color GlowWarm = new Color(1f, 0.63f, 0.12f, 0.62f);
        private static readonly Color GlowWarmSoft = new Color(1f, 0.67f, 0.16f, 0.48f);
        private static readonly Color GlowTeal = new Color(0f, 0.85f, 0.72f, 0.85f);
        private static readonly Color GlowTrophy = new Color(1f, 0.80f, 0.32f, 0.55f);

        [MenuItem("Tools/UIV2/Build Soft Glows")]
        private static void BuildSoftGlows()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var circleGlow = EnsurePillGlowSprite();

            // MainMenu
            var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            var play = FindInScene(scene, "HomeScreenV2/BottomControlsGroup/PlayButtonSlot/UIV2_PrimaryGoldButton");
            AddPillGlowBehind(play, circleGlow, GlowWarm, 34f);
            var guest = FindInScene(scene, "StartScreenV2/DesignArea/GuestButton");
            AddPillGlowBehind(guest, circleGlow, GlowWarmSoft, 28f);

            var modeContent = FindInScene(scene, "ModalHost/QuickModeV2/PanelFrame/ScrollView/Viewport/Content");
            foreach (var rowName in new[] { "Row_1v1", "Row_2v2", "Row_1v3", "Row_1v1Bot", "Row_2v2Bot", "Row_1v3Bot" })
            {
                var row = modeContent.Find(rowName) as RectTransform;
                if (row == null) continue;
                // Il viewport taglia a 12 px dai lati della riga: di lato resta solo la coda piu' tenue.
                // Dietro tutte le righe, altrimenti il bagliore velerebbe il fondo della riga sopra.
                var glow = AddPillGlowBehind(row, circleGlow, GlowTeal, 22f, maxSideReach: 12f, behindSiblings: true);
                var toggle = row.GetComponent<SelectableToggleItem>();
                if (toggle != null && glow != null)
                {
                    var so = new SerializedObject(toggle);
                    so.FindProperty("glowObject").objectReferenceValue = glow.gameObject;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    var check = so.FindProperty("checkIcon").objectReferenceValue as GameObject;
                    glow.gameObject.SetActive(check != null && check.activeSelf);
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            // GameScene
            scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            var trophy = FindInScene(scene, "MatchResults/Design/Trophy");
            AddCircleGlowBehind(trophy, circleGlow, GlowTrophy, 620f);
            var rematch = FindInScene(scene, "MatchResults/Design/Rematch/Art");
            AddPillGlowBehind(rematch, circleGlow, GlowWarm, 26f);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[UIV2FoundationBuilder] C5: bagliori morbidi dietro Gioca, Gioca come ospite, riga Modalita', trofeo e Rivincita.");
        }

        private static RectTransform FindInScene(UnityEngine.SceneManagement.Scene scene, string path)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name != path.Substring(path.LastIndexOf('/') + 1)) continue;
                    var full = t.name;
                    for (var p = t.parent; p != null; p = p.parent) full = p.name + "/" + full;
                    if (full.EndsWith(path)) return t as RectTransform;
                }
            }
            throw new System.Exception($"{path} non trovato in {scene.name}");
        }

        /// <summary>
        /// Bagliore a pillola dietro al bersaglio: pieno sotto il bordo visibile, sfuma fino a zero
        /// a <paramref name="reach"/> px fuori. <paramref name="maxSideReach"/> accorcia solo i lati.
        /// </summary>
        private static Image AddPillGlowBehind(RectTransform target, Sprite sprite, Color color, float reach, float maxSideReach = -1f, bool behindSiblings = false)
        {
            if (target == null) return null;
            var parent = target.parent;
            var old = parent.Find(target.name + SoftGlowName);
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var glow = CreateUIObject(target.name + SoftGlowName, parent);
            glow.SetSiblingIndex(behindSiblings ? 0 : target.GetSiblingIndex());
            CopyRect(target, glow);

            // La sfumatura e' lunga il doppio di quanto sporge: meta' sta sotto il pulsante, cosi' al
            // bordo il bagliore e' gia' a meta' e fuori si spegne dolcemente in "reach" px.
            float multiplier = GlowPillBorder / (reach * 2f);
            var visible = VisibleInsets(target.GetComponent<Image>());
            float padX = maxSideReach >= 0f ? maxSideReach : reach;
            float padY = reach;
            // visible: x = sinistra, y = basso, z = destra, w = alto (rientri trasparenti del bersaglio)
            glow.offsetMin += new Vector2(visible.x - padX, visible.y - padY);
            glow.offsetMax -= new Vector2(visible.z - padX, visible.w - padY);

            var image = glow.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = multiplier;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>
        /// Bordi 9-slice al centro del cerchio sfumato (usati dal pulviscolo). Gli altri usi (alone
        /// della matta, suggerimento mosse, Accuso) lo disegnano come immagine semplice, che ignora i bordi.
        /// </summary>
        /// <summary>Genera (una volta) il gradiente a pillola e lo importa come sprite 9-slice.</summary>
        private static Sprite EnsurePillGlowSprite()
        {
            if (AssetDatabase.LoadAssetAtPath<Sprite>(GlowPillPath) == null)
            {
                CreateFolderRecursive(System.IO.Path.GetDirectoryName(GlowPillPath).Replace('\\', '/'));
                var tex = new Texture2D(GlowPillSize, GlowPillSize, TextureFormat.RGBA32, false);
                float half = GlowPillSize * 0.5f;
                for (int y = 0; y < GlowPillSize; y++)
                    for (int x = 0; x < GlowPillSize; x++)
                    {
                        float d = new Vector2(x + 0.5f - half, y + 0.5f - half).magnitude / half;
                        float u = Mathf.Clamp01(1f - d);
                        float a = u * u * (3f - 2f * u);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                System.IO.File.WriteAllBytes(GlowPillPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(GlowPillPath);
            }
            var importer = (TextureImporter)AssetImporter.GetAtPath(GlowPillPath);
            var border = new Vector4(GlowPillBorder, GlowPillBorder, GlowPillBorder, GlowPillBorder);
            if (importer.textureType != TextureImporterType.Sprite || importer.spriteBorder != border)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.spriteBorder = border;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(GlowPillPath);
        }

        private static void EnsureCircleGlowBorder()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(GlowCirclePath);
            var border = new Vector4(GlowCircleBorder, GlowCircleBorder, GlowCircleBorder, GlowCircleBorder);
            if (importer == null || importer.spriteBorder == border) return;
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }

        private static Image AddCircleGlowBehind(RectTransform target, Sprite sprite, Color color, float diameter)
        {
            if (target == null) return null;
            var parent = target.parent;
            var old = parent.Find(target.name + SoftGlowName);
            if (old != null) Object.DestroyImmediate(old.gameObject);

            // In fondo alla pila: il cerchio e' piu' grande del bersaglio e non deve velare nastro e titolo.
            var glow = CreateUIObject(target.name + SoftGlowName, parent);
            glow.SetSiblingIndex(0);
            glow.anchorMin = glow.anchorMax = target.anchorMin;
            glow.pivot = new Vector2(0.5f, 0.5f);
            // centro del bersaglio, qualunque sia il suo pivot
            var size = target.rect.size;
            glow.anchoredPosition = target.anchoredPosition + new Vector2((0.5f - target.pivot.x) * size.x, (0.5f - target.pivot.y) * size.y);
            glow.sizeDelta = new Vector2(diameter, diameter);

            var image = glow.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void CopyRect(RectTransform from, RectTransform to)
        {
            to.anchorMin = from.anchorMin;
            to.anchorMax = from.anchorMax;
            to.pivot = from.pivot;
            to.anchoredPosition = from.anchoredPosition;
            to.sizeDelta = from.sizeDelta;
            to.localScale = from.localScale;
            to.localRotation = from.localRotation;
        }

        /// <summary>
        /// Rientri trasparenti (alfa &lt;= 0.16) dell'immagine nel suo rettangolo, in unita' del
        /// canvas. Gestisce immagini semplici (scala lineare) e 9-slice (i bordi scalano con
        /// pixelsPerUnitMultiplier, il centro si stira).
        /// </summary>
        private static Vector4 VisibleInsets(Image image)
        {
            if (image == null || image.sprite == null) return Vector4.zero;
            var sprite = image.sprite;
            var tex = new Texture2D(2, 2);
            tex.LoadImage(System.IO.File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite.texture)));
            var r = sprite.rect;
            int x0 = int.MaxValue, y0 = int.MaxValue, x1 = -1, y1 = -1;
            for (int y = (int)r.y; y < (int)(r.y + r.height); y++)
                for (int x = (int)r.x; x < (int)(r.x + r.width); x++)
                    if (tex.GetPixel(x, y).a > 0.16f)
                    {
                        if (x < x0) x0 = x; if (x > x1) x1 = x;
                        if (y < y0) y0 = y; if (y > y1) y1 = y;
                    }
            Object.DestroyImmediate(tex);
            if (x1 < 0) return Vector4.zero;

            float padL = x0 - r.x, padR = r.x + r.width - 1 - x1, padB = y0 - r.y, padT = r.y + r.height - 1 - y1;
            var size = image.rectTransform.rect.size;
            if (image.type != Image.Type.Sliced)
            {
                return new Vector4(padL / r.width * size.x, padB / r.height * size.y,
                    padR / r.width * size.x, padT / r.height * size.y);
            }

            float m = image.pixelsPerUnitMultiplier * sprite.pixelsPerUnit / 100f;
            var b = sprite.border; // x sinistra, y basso, z destra, w alto
            return new Vector4(
                MapSliced(padL, r.width, b.x, b.z, size.x, m),
                MapSliced(padB, r.height, b.y, b.w, size.y, m),
                MapSliced(padR, r.width, b.z, b.x, size.x, m),
                MapSliced(padT, r.height, b.w, b.y, size.y, m));
        }

        /// <summary>Distanza dal bordo "start" in sorgente -> distanza a schermo, su un asse 9-slice.</summary>
        private static float MapSliced(float pad, float source, float borderStart, float borderEnd, float dest, float multiplier)
        {
            float bs = borderStart / multiplier, be = borderEnd / multiplier;
            if (pad <= borderStart) return pad / multiplier;
            float centerSource = Mathf.Max(1f, source - borderStart - borderEnd);
            float centerDest = Mathf.Max(0f, dest - bs - be);
            return bs + (pad - borderStart) * centerDest / centerSource;
        }
    }
}
