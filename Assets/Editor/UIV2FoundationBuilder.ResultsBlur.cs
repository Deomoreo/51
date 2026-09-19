using Project51.UIV2.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Project51.EditorTools
{
    /// <summary>
    /// G6 - fine smazzata (mockup 13) con il tavolo sfocato dietro, come roulette e impostazioni in
    /// partita: foto sfocata (BackdropBlur) + velo "Sfocatura sfondo" al posto del nero al 72%.
    /// Il fine partita resta com'e': e' a schermo intero con fondo pieno (mockup 12). Rilanciabile.
    /// </summary>
    public static partial class UIV2FoundationBuilder
    {
        [MenuItem("Tools/UIV2/Build Round Results Blur")]
        private static void BuildRoundResultsBlur()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            var results = Object.FindObjectOfType<MatchResultsV2>(true);
            if (results == null || results.RoundPanel == null) throw new System.Exception("[RoundResultsBlur] MatchResultsV2 o RoundPanel non trovati");

            var panel = results.RoundPanel.transform;
            foreach (var name in new[] { "Blur", "Veil" })
            {
                var old = panel.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }
            results.RoundBlur = AddBlurBackdrop(panel);

            // Il vecchio velo nero resta in scena spento (non e' la radice del pannello, solo un'immagine).
            var dim = panel.Find("Dim");
            if (dim != null) dim.gameObject.SetActive(false);

            EditorUtility.SetDirty(results);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[RoundResultsBlur] Fine smazzata con tavolo sfocato e velo.");
        }
    }
}
