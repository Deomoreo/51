using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.EditorTools
{
    /// <summary>
    /// GameCanvas in GameScene.unity aveva Reference Resolution 1920x1080 (landscape),
    /// unico canvas del progetto non allineato a 1080x1920 (portrait) come MainMenu/HomeScreen.
    /// Vedi Assets/UI_SPEC_Tavolo.md.
    /// </summary>
    public static class GameCanvasResolutionFixer
    {
        private const string ScenePath = "Assets/Scenes/GameScene.unity";

        [MenuItem("Tools/51/Fix GameCanvas Resolution (1080x1920)")]
        private static void Fix()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var canvasGO = GameObject.Find("GameCanvas");
            if (canvasGO == null)
            {
                Debug.LogError("[GameCanvasResolutionFixer] GameObject 'GameCanvas' non trovato in GameScene.unity.");
                return;
            }

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                Debug.LogError("[GameCanvasResolutionFixer] CanvasScaler non trovato su GameCanvas.");
                return;
            }

            Undo.RecordObject(scaler, "Fix GameCanvas Resolution");

            var before = scaler.referenceResolution;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            EditorUtility.SetDirty(scaler);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"[GameCanvasResolutionFixer] GameCanvas: referenceResolution {before} -> {scaler.referenceResolution}. Salvato.");
        }
    }
}
