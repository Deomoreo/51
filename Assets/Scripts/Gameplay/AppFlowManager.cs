using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

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
        public const string SCENE_LOBBY = "LobbyScene";
        public const string SCENE_WAITING_ROOM = "WaitingRoom";

        /// <summary>
        /// Carica il Main Menu.
        /// </summary>
        public static void GoToMainMenu()
        {
            Debug.Log("[AppFlow] Loading Main Menu...");
            
            // Se siamo connessi a Photon, disconnetti
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
            }

            SceneManager.LoadScene(SCENE_MAIN_MENU);
        }

        /// <summary>
        /// Carica la scena di gioco.
        /// </summary>
        /// <param name="usePhotonSync">Se true, usa PhotonNetwork.LoadLevel per sincronizzare con altri client.</param>
        public static void GoToGame(bool usePhotonSync = false)
        {
            Debug.Log("[AppFlow] Loading Game Scene...");

            if (usePhotonSync && PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel(SCENE_GAME);
            }
            else
            {
                SceneManager.LoadScene(SCENE_GAME);
            }
        }

        /// <summary>
        /// Carica la Lobby.
        /// </summary>
        public static void GoToLobby()
        {
            Debug.Log("[AppFlow] Loading Lobby...");
            
            if (Application.CanStreamedLevelBeLoaded(SCENE_LOBBY))
            {
                SceneManager.LoadScene(SCENE_LOBBY);
            }
            else
            {
                Debug.LogWarning($"[AppFlow] Scene '{SCENE_LOBBY}' not found. Staying in current scene.");
            }
        }

        /// <summary>
        /// Esci dalla partita e torna al menu principale.
        /// </summary>
        public static void LeaveGameAndGoToMenu()
        {
            Debug.Log("[AppFlow] Leaving game and going to menu...");

            // Se siamo in una stanza Photon, esci
            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom();
            }

            // Reset del GameModeService
            Core.GameModeService.Reset();

            GoToMainMenu();
        }

        /// <summary>
        /// Verifica se una scena esiste nel build.
        /// </summary>
        public static bool SceneExists(string sceneName)
        {
            return Application.CanStreamedLevelBeLoaded(sceneName);
        }

        /// <summary>
        /// Ricarica la scena corrente.
        /// </summary>
        public static void ReloadCurrentScene()
        {
            var currentScene = SceneManager.GetActiveScene().name;
            Debug.Log($"[AppFlow] Reloading scene: {currentScene}");
            SceneManager.LoadScene(currentScene);
        }
    }
}