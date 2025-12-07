using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Project51.EditorTools
{
    /// <summary>
    /// Editor utility to fix scenes that are missing the GameManager component.
    /// This fixes the NullReferenceException in GameManager.CurrentGameMode.
    /// Uses reflection to avoid assembly dependencies.
    /// </summary>
    public static class FixGameManagerScenes
    {
        [MenuItem("Project51/Fix/Add GameManager to Current Scene")]
        public static void FixCurrentScene()
        {
            // Use reflection to find TurnController to avoid assembly dependency
            var turnControllerType = System.Type.GetType("Project51.Unity.TurnController, Project51.Gameplay");
            if (turnControllerType == null)
            {
                Debug.LogError("Could not find TurnController type! Make sure Project51.Gameplay assembly exists.");
                return;
            }

            var turnController = Object.FindObjectOfType(turnControllerType) as MonoBehaviour;
            if (turnController == null)
            {
                Debug.LogError("No TurnController found in current scene! Make sure you have the game scene open.");
                return;
            }

            var gameManagerType = System.Type.GetType("Project51.Unity.GameManager, Project51.Networking");
            if (gameManagerType == null)
            {
                Debug.LogError("Could not find Project51.Unity.GameManager type! Make sure Project51.Networking assembly exists.");
                return;
            }

            // Check if GameManager already exists
            var existing = turnController.GetComponent(gameManagerType);
            if (existing != null)
            {
                Debug.Log("<color=green>? GameManager component already exists on " + turnController.gameObject.name + "</color>");
                return;
            }

            // Add the GameManager component
            turnController.gameObject.AddComponent(gameManagerType);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            
            Debug.Log("<color=green>? Added GameManager component to " + turnController.gameObject.name + "</color>");
            Debug.Log("<color=yellow>Don't forget to save the scene! (Ctrl+S or File > Save)</color>");
        }

        [MenuItem("Project51/Fix/Verify Current Scene Setup")]
        public static void VerifyCurrentScene()
        {
            Debug.Log("=== VERIFYING SCENE SETUP ===");
            
            // Check for TurnController using reflection
            var turnControllerType = System.Type.GetType("Project51.Unity.TurnController, Project51.Gameplay");
            if (turnControllerType == null)
            {
                Debug.LogError("? TurnController type NOT FOUND - Project51.Gameplay assembly missing?");
                return;
            }

            var turnController = Object.FindObjectOfType(turnControllerType) as MonoBehaviour;
            if (turnController == null)
            {
                Debug.LogError("? TurnController NOT FOUND - Scene is invalid!");
                return;
            }
            Debug.Log("? TurnController found on: " + turnController.gameObject.name);

            // Check for GameManager component
            var gameManagerType = System.Type.GetType("Project51.Unity.GameManager, Project51.Networking");
            if (gameManagerType == null)
            {
                Debug.LogError("? GameManager type NOT FOUND - Project51.Networking assembly missing?");
                return;
            }

            var gameManager = turnController.GetComponent(gameManagerType);
            if (gameManager == null)
            {
                Debug.LogError("? GameManager component NOT FOUND on " + turnController.gameObject.name);
                Debug.LogError("  ? Run 'Project51 > Fix > Add GameManager to Current Scene' to fix this!");
                return;
            }
            Debug.Log("? GameManager component found on: " + turnController.gameObject.name);

            // Check for CardViewManager using reflection
            var cardViewManagerType = System.Type.GetType("Project51.Unity.CardViewManager, Project51.Gameplay");
            if (cardViewManagerType != null)
            {
                var cardViewManager = Object.FindObjectOfType(cardViewManagerType) as MonoBehaviour;
                if (cardViewManager == null)
                {
                    Debug.LogWarning("? CardViewManager NOT FOUND - UI may not work!");
                }
                else
                {
                    Debug.Log("? CardViewManager found on: " + cardViewManager.gameObject.name);
                }
            }

            Debug.Log("<color=green>=== SCENE SETUP IS VALID ===</color>");
        }
    }
}
