using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project51.Unity
{
    /// <summary>
    /// Gestisce il flusso di navigazione tra le scene principali dell'applicazione.
    /// Centralizza le chiamate a SceneManager per facilitare manutenzione e transizioni.
    /// </summary>
    public static class AppFlowManager
    {
        public const string SCENE_MAIN_MENU = "MainMenu";
        public const string SCENE_GAME = "GameScene";
        public const string SCENE_LOBBY = "LobbyScene"; // Futura implementazione

        public static void GoToMainMenu()
        {
            Debug.Log("[AppFlow] Loading Main Menu...");
            SceneManager.LoadScene(SCENE_MAIN_MENU);
        }

        public static void GoToGame()
        {
            Debug.Log("[AppFlow] Loading Game Scene...");
            SceneManager.LoadScene(SCENE_GAME);
        }

        public static void GoToLobby()
        {
            Debug.Log("[AppFlow] Loading Lobby...");
            // Se la scena non esiste ancora, fallback sul menu o log di errore
            if (Application.CanStreamedLevelBeLoaded(SCENE_LOBBY))
            {
                SceneManager.LoadScene(SCENE_LOBBY);
            }
            else
            {
                Debug.LogWarning($"[AppFlow] Scene '{SCENE_LOBBY}' not found. Staying in current scene.");
            }
        }
    }
}