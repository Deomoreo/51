using Project51.UIV2.Components;
using Project51.UIV2.Screens;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.EditorTools
{
    /// <summary>
    /// Opzioni spostate dal Profilo (ingranaggio nascosto sopra all'avatar) alla colonna di azioni
    /// rapide della Home, sotto a Posta: stessa scheda di Premio/Classifica/Posta con ic_gear.
    /// L'ingranaggio del Profilo viene spento. Rilanciabile.
    /// </summary>
    public static partial class UIV2FoundationBuilder
    {
        private const string HomeSettingsName = "SettingsQuickAction";

        [MenuItem("Tools/UIV2/Build Home Settings Button")]
        private static void BuildHomeSettingsButton()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);

            var home = Object.FindObjectOfType<HomeScreenV2>(true);
            if (home == null) throw new System.Exception("HomeScreenV2 non trovato in MainMenu");
            var homeSo = new SerializedObject(home);
            var mail = (UIV2QuickActionButton)homeSo.FindProperty("mailButton").objectReferenceValue;
            if (mail == null) throw new System.Exception("HomeScreenV2.mailButton non collegato");
            var column = (RectTransform)mail.transform.parent;

            var old = column.Find(HomeSettingsName);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
                column.sizeDelta -= new Vector2(0f, QuickActionStep(column, mail));
            }

            var copy = Object.Instantiate(mail.gameObject, column);
            copy.name = HomeSettingsName;
            copy.transform.SetAsLastSibling();
            var settings = copy.GetComponent<UIV2QuickActionButton>();
            var so = new SerializedObject(settings);
            ((Image)so.FindProperty("icon").objectReferenceValue).sprite = LoadSprite(IconsPath, "ic_gear");
            ((TMP_Text)so.FindProperty("label").objectReferenceValue).text = "Opzioni";
            var badge = (GameObject)so.FindProperty("badgeRoot").objectReferenceValue;
            if (badge != null) badge.SetActive(false);
            column.sizeDelta += new Vector2(0f, QuickActionStep(column, mail));

            homeSo.FindProperty("settingsButton").objectReferenceValue = settings;
            homeSo.ApplyModifiedPropertiesWithoutUndo();

            var profile = Object.FindObjectOfType<ProfileScreenV2>(true);
            if (profile != null)
            {
                var gear = (Button)new SerializedObject(profile).FindProperty("settingsButton").objectReferenceValue;
                if (gear != null) gear.gameObject.SetActive(false);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UIV2FoundationBuilder] Opzioni nella colonna della Home sotto a Posta; ingranaggio del Profilo spento.");
        }

        /// <summary>Altezza di una scheda + spazio della colonna (VerticalLayoutGroup).</summary>
        private static float QuickActionStep(RectTransform column, UIV2QuickActionButton sample)
        {
            var layout = column.GetComponent<VerticalLayoutGroup>();
            return ((RectTransform)sample.transform).sizeDelta.y + (layout != null ? layout.spacing : 0f);
        }
    }
}
