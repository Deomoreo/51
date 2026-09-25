using Project51.UIV2.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Project51.EditorTools
{
    public static partial class UIV2FoundationBuilder
    {
        // 2.22: niente voci "prossimamente" nella prima build. Idempotente; da rilanciare dopo
        // Build Delete Account / Profile / Collection, che rimettono righe e posizioni originali.
        [MenuItem("Tools/UIV2/Hide Coming Soon (2.22)")]
        private static void HideComingSoon()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);

            // Impostazioni: via Notifiche e Supporto, le righe sotto salgono. Supporto resta
            // (nascosto) perche' Build Delete Account lo usa come modello.
            var settings = Object.FindObjectOfType<SettingsV2Integration>(true);
            var settingsSo = new SerializedObject(settings);
            var frame = ((GameObject)settingsSo.FindProperty("panel").objectReferenceValue).transform.Find("PanelFrame") as RectTransform;
            frame.Find("Row_Notifiche").gameObject.SetActive(false);
            frame.Find("Row_Supporto").gameObject.SetActive(false);
            SetTop(frame, "Section_ACCOUNT", 916f);
            SetTop(frame, "Row_Account", 962f);
            SetTop(frame, "Row_Lingua", 1108f);
            SetTop(frame, "Row_EliminaAccount", 1220f);
            SetTop(frame, "FooterText", 1326f);
            frame.sizeDelta = new Vector2(frame.sizeDelta.x, 1392f);
            var after = settingsSo.FindProperty("rowsAfterAccount");
            after.arraySize = 1;
            after.GetArrayElementAtIndex(0).objectReferenceValue = frame.Find("Row_Lingua");
            settingsSo.ApplyModifiedPropertiesWithoutUndo();

            // Profilo: i trofei non sono ancora popolati, via l'intestazione orfana e i suoi spazi.
            EditPrefab($"{ScreensPrefabDir}/ProfileScreenV2.prefab", "TrophiesHeader", true);
            // Collezione: via "Nuovi mazzi in arrivo..." e lo spazio sopra.
            EditPrefab($"{ScreensPrefabDir}/CollectionScreenV2.prefab", "FooterLabel", false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UIV2FoundationBuilder] 2.22: voci 'prossimamente' nascoste (Impostazioni, Profilo, Collezione).");
        }

        private static void SetTop(RectTransform frame, string name, float top)
        {
            var rt = (RectTransform)frame.Find(name);
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -top);
        }

        // Nasconde l'oggetto con lo Spacer che lo precede (e quello che lo segue se spacerAfter).
        private static void EditPrefab(string path, string target, bool spacerAfter)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Transform t = null;
                foreach (var c in root.GetComponentsInChildren<Transform>(true)) if (c.name == target) t = c;
                var parent = t.parent;
                int i = t.GetSiblingIndex();
                t.gameObject.SetActive(false);
                HideSpacer(parent, i - 1);
                if (spacerAfter) HideSpacer(parent, i + 1);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void HideSpacer(Transform parent, int index)
        {
            if (index < 0 || index >= parent.childCount) return;
            var s = parent.GetChild(index);
            if (s.name == "Spacer") s.gameObject.SetActive(false);
        }
    }
}
