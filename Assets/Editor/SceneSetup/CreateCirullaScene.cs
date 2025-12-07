using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Project51.Unity; // direct type references to avoid reflection failures
using System.Reflection;

/// <summary>
/// Editor utility to create a starter scene, prefab and GameObjects for the Project51 demo.
/// Use the menu Project51/Setup/Create Main Scene inside the Unity Editor.
/// </summary>
public static class CreateCirullaScene
{
    private const string ScenesFolder = "Assets/Scenes";
    private const string PrefabsFolder = "Assets/Prefabs";
    private const string ScenePath = ScenesFolder + "/MainScene.unity";
    private const string CardViewPrefabPath = PrefabsFolder + "/CardView.prefab";

    [MenuItem("Project51/Setup/Create Main Scene")]
    public static void CreateMainScene()
    {
        // Ensure folders exist
        CreateFolderIfMissing("Assets", "Scenes");
        CreateFolderIfMissing("Assets", "Prefabs");

        // Create new scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Create a simple CardView prefab
        var cardViewGO = new GameObject("CardViewPrefab_Temp");
        var spriteRenderer = cardViewGO.AddComponent<SpriteRenderer>();

        // Add the CardView MonoBehaviour directly to avoid reflection issues
        var cardViewComp = cardViewGO.AddComponent<CardView>();

        // Save the prefab asset
        var prefab = PrefabUtility.SaveAsPrefabAsset(cardViewGO, CardViewPrefabPath);

        // Remove the temporary instance from the scene
        Object.DestroyImmediate(cardViewGO);

        if (prefab == null)
        {
            Debug.LogError("Failed to create CardView prefab.");
            return;
        }

        // Create GameManager with TurnController
        var gameManagerGO = new GameObject("GameManager");
        var turnController = gameManagerGO.AddComponent<TurnController>();
        
        // Add the new GameManager component (from Project51.Networking assembly)
        // This handles both single-player and multiplayer modes
        var gameManagerType = System.Type.GetType("Project51.Unity.GameManager, Project51.Networking");
        if (gameManagerType != null)
        {
            gameManagerGO.AddComponent(gameManagerType);
            Debug.Log("Added Project51.Unity.GameManager component to GameManager GameObject");
        }
        else
        {
            Debug.LogWarning("Could not find Project51.Unity.GameManager type - multiplayer may not work!");
        }

        // Create CardViewManager and assign references
        var cardViewManagerGO = new GameObject("CardViewManager");
        var cardViewManager = cardViewManagerGO.AddComponent<CardViewManager>();

        // Create containers
        var tableContainer = new GameObject("TableCardsContainer");
        tableContainer.transform.parent = cardViewManagerGO.transform;
        tableContainer.transform.position = new Vector3(0f, 1.5f, 0f);

        var handContainer = new GameObject("HumanHandContainer");
        handContainer.transform.parent = cardViewManagerGO.transform;
        handContainer.transform.position = new Vector3(0f, -2.5f, 0f);

        // Assign private serialized fields using reflection to avoid SerializedObject issues
        if (cardViewManager != null && turnController != null)
        {
            var cvm = cardViewManager as CardViewManager;
            if (cvm != null)
            {
                var type = typeof(CardViewManager);
                var fTurn = type.GetField("turnController", BindingFlags.Instance | BindingFlags.NonPublic);
                var fPrefab = type.GetField("cardViewPrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                var fTable = type.GetField("tableCardContainer", BindingFlags.Instance | BindingFlags.NonPublic);
                var fHand = type.GetField("humanHandContainer", BindingFlags.Instance | BindingFlags.NonPublic);

                try
                {
                    fTurn?.SetValue(cvm, turnController);
                    var loadedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardViewPrefabPath);
                    fPrefab?.SetValue(cvm, loadedPrefab);
                    fTable?.SetValue(cvm, tableContainer.transform);
                    fHand?.SetValue(cvm, handContainer.transform);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("Failed to set CardViewManager fields via reflection: " + ex.Message);
                }
            }
            else
            {
                Debug.LogWarning("CardViewManager component is null or of unexpected type.");
            }
        }

        // Save scene asset
        EditorSceneManager.SaveScene(scene, ScenePath);

        Debug.Log("MainScene created at " + ScenePath + " and CardView prefab at " + CardViewPrefabPath + ".\nOpen the scene and press Play to test.");
    }

    private static void CreateFolderIfMissing(string parent, string newFolder)
    {
        var path = parent + "/" + newFolder;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, newFolder);
        }
    }
}
