using Project51.Unity;
using Project51.Unity.UI;
using Project51.UIV2.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.EditorTools
{
    /// <summary>
    /// G1 - tavolo allineato su ogni proporzione dello schermo (vedi CameraResponsiveFit).
    /// In GameScene:
    /// - Main Camera: CameraResponsiveFit (area di design 5,625 x 10 sempre visibile, stessa scala dei Canvas);
    /// - GameCanvas e GamePresentationV2: PortraitCanvasMatch (larghezza sui telefoni stretti, altezza sui tablet);
    /// - i contenitori che seguono il tavolo (banner, pulsanti Emoji/Accuso, roulette del mazziere,
    ///   bolle delle emoticon) diventano un'area 1080x1920 centrata, lo stesso centro della camera:
    ///   i figli tengono le loro coordinate del mockup e restano sopra alle carte. Prima erano
    ///   agganciati all'angolo in alto a sinistra di uno schermo alto 1920.
    /// La barra in alto resta attaccata al bordo superiore (SafeAreaTopOffset); i pannelli modali
    /// continuano a stare nella safe area (DesignCanvasFit).
    /// Rilanciabile.
    /// </summary>
    public static class TableDesignAreaBuilder
    {
        private const string GameScenePath = "Assets/Scenes/GameScene.unity";
        private static readonly Vector2 DesignSize = new Vector2(1080f, 1920f);

        private static readonly string[] CenteredContainers =
        {
            "GameCanvas/PlayerBanners",
            "GameCanvas/TableActionButtons",
            "GameCanvas/DealerRoulette/Design",
            "GamePresentationV2/Design",
        };

        private static readonly string[] LocalSeatTargets =
        {
            "GameCanvas/PlayerBanners/Banner_Local",
            "GameCanvas/TableActionButtons/EmojiButton",
            "GameCanvas/TableActionButtons/AccusoWindowGlow",
            "GameCanvas/TableActionButtons/AccusoButton",
            "GameCanvas/TableActionButtons/AccusoWindowRing",
            "GameCanvas/TableActionButtons/AccusoWindowBadge",
            "GameCanvas/TableActionButtons/AccusoWindowPrompt",
            "GamePresentationV2/Design/Bubble0",
        };

        [MenuItem("Tools/UIV2/Apply Table Design Area")]
        private static void Apply()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

            var camera = Camera.main != null ? Camera.main : Object.FindObjectOfType<Camera>();
            if (camera.GetComponent<CameraResponsiveFit>() == null) camera.gameObject.AddComponent<CameraResponsiveFit>();
            camera.orthographicSize = CameraResponsiveFit.DesignWorldHeight * 0.5f; // valore a 9:16; a runtime lo adatta

            foreach (var canvasName in new[] { "GameCanvas", "GamePresentationV2" })
            {
                var canvas = GameObject.Find(canvasName);
                if (canvas == null) { Debug.LogWarning($"[TableDesignArea] {canvasName} non trovato"); continue; }
                var scaler = canvas.GetComponent<CanvasScaler>();
                scaler.referenceResolution = DesignSize;
                if (canvas.GetComponent<PortraitCanvasMatch>() == null) canvas.AddComponent<PortraitCanvasMatch>();
            }

            foreach (var path in CenteredContainers)
            {
                var rt = Find(path);
                if (rt == null) { Debug.LogWarning($"[TableDesignArea] {path} non trovato"); continue; }
                var fit = rt.GetComponent<DesignCanvasFit>();
                if (fit != null) Object.DestroyImmediate(fit);
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = DesignSize;
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
                EditorUtility.SetDirty(rt);
            }

            // Sui telefoni piu' allungati il posto locale (e quindi la mano) scende un po' verso il fondo.
            var gameCanvas = GameObject.Find("GameCanvas");
            var shift = gameCanvas.GetComponent<LocalSeatBottomShift>();
            if (shift == null) shift = gameCanvas.AddComponent<LocalSeatBottomShift>();
            var targets = new System.Collections.Generic.List<RectTransform>();
            foreach (var path in LocalSeatTargets)
            {
                var rt = Find(path);
                if (rt == null) { Debug.LogWarning($"[TableDesignArea] {path} non trovato"); continue; }
                targets.Add(rt);
            }
            var so = new SerializedObject(shift);
            var list = so.FindProperty("targets");
            list.arraySize = targets.Count;
            for (int i = 0; i < targets.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = targets[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[TableDesignArea] Tavolo su area di design 1080x1920 centrata.");
        }

        private static RectTransform Find(string path)
        {
            int slash = path.IndexOf('/');
            var root = GameObject.Find(path.Substring(0, slash));
            return root != null ? root.transform.Find(path.Substring(slash + 1)) as RectTransform : null;
        }
    }
}
