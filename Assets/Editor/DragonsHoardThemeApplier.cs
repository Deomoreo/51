using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Project51.Unity;

namespace Project51.EditorTools
{
    /// <summary>
    /// Applica ThemedButton agli oggetti selezionati in Hierarchy con la variante
    /// scelta esplicitamente dal menu. Non applica mai automaticamente a intere scene.
    /// </summary>
    public static class DragonsHoardThemeApplier
    {
        private const string ThemeResourcePath = "DragonsHoardTheme";

        [MenuItem("Tools/Dragons Hoard Theme/Apply As/Primary")]
        private static void ApplyPrimary() => ApplyToSelection(ThemedButton.Variant.Primary);

        [MenuItem("Tools/Dragons Hoard Theme/Apply As/Secondary")]
        private static void ApplySecondary() => ApplyToSelection(ThemedButton.Variant.Secondary);

        [MenuItem("Tools/Dragons Hoard Theme/Apply As/List Row")]
        private static void ApplyListRow() => ApplyToSelection(ThemedButton.Variant.ListRow);

        [MenuItem("Tools/Dragons Hoard Theme/Apply As/Icon Tab")]
        private static void ApplyIconTab() => ApplyToSelection(ThemedButton.Variant.IconTab);

        private static void ApplyToSelection(ThemedButton.Variant variant)
        {
            var selection = Selection.gameObjects;
            if (selection == null || selection.Length == 0)
            {
                Debug.LogWarning("[DragonsHoardThemeApplier] Nessun oggetto selezionato in Hierarchy.");
                return;
            }

            var theme = Resources.Load<UITheme>(ThemeResourcePath);
            if (theme == null)
            {
                Debug.LogError($"[DragonsHoardThemeApplier] UITheme non trovato in Resources/{ThemeResourcePath}.");
                return;
            }

            int applied = 0;
            foreach (var go in selection)
            {
                var themedButton = go.GetComponent<ThemedButton>();
                if (themedButton == null)
                {
                    themedButton = Undo.AddComponent<ThemedButton>(go);
                }
                else
                {
                    Undo.RecordObject(themedButton, "Apply Dragon's Hoard Theme");
                }

                var serialized = new SerializedObject(themedButton);
                serialized.FindProperty("theme").objectReferenceValue = theme;
                serialized.FindProperty("variant").enumValueIndex = (int)variant;
                serialized.ApplyModifiedProperties();

                themedButton.Apply();
                EditorUtility.SetDirty(themedButton);
                applied++;
            }

            Debug.Log($"[DragonsHoardThemeApplier] Applicata variante {variant} a {applied} oggetto/i selezionato/i.");
        }

        [MenuItem("Tools/Dragons Hoard Theme/List Candidates In Scene")]
        private static void ListCandidatesInScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var candidates = new List<GameObject>();

            foreach (var root in scene.GetRootGameObjects())
            {
                candidates.AddRange(
                    root.GetComponentsInChildren<Button>(true)
                        .Where(b => b.GetComponent<ThemedButton>() == null)
                        .Select(b => b.gameObject));
            }

            if (candidates.Count == 0)
            {
                Debug.Log("[DragonsHoardThemeApplier] Nessun candidato: tutti i Button nella scena hanno gia' un ThemedButton (o non ci sono Button).");
                return;
            }

            Debug.Log($"[DragonsHoardThemeApplier] {candidates.Count} Button senza ThemedButton nella scena '{scene.name}':");
            foreach (var go in candidates)
            {
                Debug.Log($"  - {GetHierarchyPath(go)}", go);
            }
        }

        private static string GetHierarchyPath(GameObject go)
        {
            string path = go.name;
            var t = go.transform;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }
    }
}
