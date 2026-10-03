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

        private static bool returnToHome;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetNavigation() => returnToHome = false;

        public static bool ConsumeReturnToHome()
        {
            bool value = returnToHome;
            returnToHome = false;
            return value;
        }

        /// <summary>Return from a match to Home without repeating the entry gate.</summary>
        public static void GoToMainMenu()
        {
            returnToHome = SceneManager.GetActiveScene().name == SCENE_GAME;
            Debug.Log("[AppFlow] Loading Main Menu...");
            
            // Uscita volontaria da una partita online: LeaveRoom(false) lascia subito il posto (niente
            // finestra di rientro, gli altri vedono "ha lasciato la partita"). Disconnettersi invece
            // renderebbe il giocatore solo "inattivo" per tutta la PlayerTtl.
            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom(false);
            }
            else if (PhotonNetwork.IsConnected && PhotonNetwork.NetworkClientState != Photon.Realtime.ClientState.Leaving)
            {
                PhotonNetwork.Disconnect();
            }

            if (!Core.AppLoading.LoadScene(SCENE_MAIN_MENU)) SceneManager.LoadScene(SCENE_MAIN_MENU);
        }

        /// <summary>
        /// Esci dalla partita e torna al menu principale.
        /// </summary>
        public static void LeaveGameAndGoToMenu()
        {
            Debug.Log("[AppFlow] Leaving game and going to menu...");

            // Se siamo in una stanza Photon, esci
            // L'uscita dalla stanza Photon la gestisce GoToMainMenu.

            // Reset del GameModeService
            Core.GameModeService.Reset();

            GoToMainMenu();
        }
    }
}