using Project51.UIV2.Components;
using Project51.UIV2.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Project51.EditorTools
{
    /// <summary>
    /// C6 - particelle (UIV2MoteField, puntini di luce morbidi dal "Bagliore morbido cerchio"):
    /// - schermata iniziale: pulviscolo oro che sale dietro logo e pulsanti;
    /// - Home e pagine (Collezione, Negozio, Profilo): un solo pulviscolo piu' rado sopra lo
    ///   sfondo comune, dietro alle pagine (che sono trasparenti);
    /// - vittoria: scoppio di luce dal trofeo, insieme ai coriandoli (MatchResultsV2.TrophyBurst).
    /// Le ricompense (E2/F1) non hanno ancora una schermata: il componente ha gia' Burst() per loro.
    /// Rilanciabile: rimuove e ricrea solo gli oggetti "Motes".
    /// </summary>
    public static partial class UIV2FoundationBuilder
    {
        private static readonly Color MoteGold = new Color(1f, 0.84f, 0.45f, 0.85f);
        private static readonly Color MoteGoldSoft = new Color(1f, 0.84f, 0.45f, 0.6f);
        private static readonly Color MoteBurst = new Color(1f, 0.85f, 0.42f, 0.95f);

        [MenuItem("Tools/UIV2/Build Mote Fields")]
        private static void BuildMoteFields()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureCircleGlowMipmaps();
            var mote = LoadSprite(GlowCirclePath, "Bagliore morbido cerchio");
            if (mote == null) throw new System.Exception("Sprite del bagliore non trovato");

            var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            var startBackground = FindInScene(scene, "StartScreenV2/Background");
            var start = AddMoteField(startBackground.parent, "StartMotes", startBackground.GetSiblingIndex() + 1, mote, MoteGold);
            Configure(start, ambient: 24, burst: 0);

            var homeBackground = FindInScene(scene, "UIV2_Home/BackgroundLayer");
            var home = AddMoteField(homeBackground.parent, "HomeMotes", homeBackground.GetSiblingIndex() + 1, mote, MoteGoldSoft);
            Configure(home, ambient: 18, burst: 0);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            var trophy = FindInScene(scene, "MatchResults/Design/Trophy");
            var burst = AddMoteField(trophy.parent, "TrophyMotes", trophy.GetSiblingIndex() + 1, mote, MoteBurst);
            // Centrato sul trofeo: Burst() parte dal centro del rettangolo.
            var rt = burst.rectTransform;
            rt.anchorMin = rt.anchorMax = trophy.anchorMin;
            rt.pivot = new Vector2(0.5f, 0.5f);
            var size = trophy.rect.size;
            rt.anchoredPosition = trophy.anchoredPosition + new Vector2((0.5f - trophy.pivot.x) * size.x, (0.5f - trophy.pivot.y) * size.y);
            rt.sizeDelta = new Vector2(200f, 200f);
            Configure(burst, ambient: 0, burst: 40);
            var results = Object.FindObjectOfType<MatchResultsV2>(true);
            if (results == null) throw new System.Exception("MatchResultsV2 non trovato in GameScene");
            results.TrophyBurst = burst;
            EditorUtility.SetDirty(results);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[UIV2FoundationBuilder] C6: pulviscolo in schermata iniziale e Home/pagine, scoppio dal trofeo in vittoria.");
        }

        /// <summary>
        /// Senza mipmap lo sprite (1254 px) ridotto a 20-50 px sfarfalla e perde luce: i puntini
        /// sembravano grigi. Migliora anche gli altri usi ridotti del cerchio (alone matta, mosse).
        /// </summary>
        private static void EnsureCircleGlowMipmaps()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(GlowCirclePath);
            if (importer == null || importer.mipmapEnabled) return;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
        }

        private static UIV2MoteField AddMoteField(Transform parent, string name, int siblingIndex, Sprite sprite, Color color)
        {
            var old = parent.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var rect = CreateUIObject(name, parent);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.SetSiblingIndex(Mathf.Min(siblingIndex, parent.childCount - 1));
            var field = rect.gameObject.AddComponent<UIV2MoteField>();
            SetPrivateField(field, "moteSprite", sprite);
            field.color = color;
            field.raycastTarget = false;
            return field;
        }

        private static void Configure(UIV2MoteField field, int ambient, int burst)
        {
            SetPrivateField(field, "ambientCount", ambient);
            SetPrivateField(field, "burstCount", burst);
            EditorUtility.SetDirty(field);
        }
    }
}
