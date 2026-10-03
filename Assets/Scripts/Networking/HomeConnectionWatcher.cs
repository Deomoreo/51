using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using Project51.Auth;
using Project51.Core;
using Project51.UIV2.Core;
using Project51.Unity.UI;
using UnityEngine;

namespace Project51.Networking
{
    /// <summary>
    /// Connessione persa in Home (mockup ConnHomeRiconnessione / ConnHomeErrore). Se Photon cade dopo l'ingresso in Home si
    /// riprova da soli: un calo breve (rientro dall'app in pausa) si risolve in silenzio; se dopo QuietSeconds non si e' ancora
    /// collegati compare "Riconnessione…" coi tentativi, e dopo MaxAttempts "Nessuna connessione" con RIPROVA e
    /// "Continua offline" (l'allenamento coi bot funziona senza rete; il gioco online si ricollega da solo quando serve).
    /// Prima dell'ingresso ci pensa l'accesso (AuthBootstrapper e schermata di caricamento). Chiede anche lo stato del servizio
    /// (aggiornamento obbligatorio, manutenzione). Messo da UI51SystemBuilder su StartScreenV2 in MainMenu.
    /// </summary>
    public sealed class HomeConnectionWatcher : MonoBehaviourPunCallbacks
    {
        public const int MaxAttempts = 5;
        const float QuietSeconds = 3f;

        [SerializeField] private StartScreenV2 startScreen;

        private Coroutine routine, rejoin;
        private bool rejoinGaveUp;

        // Aggiornamento obbligatorio e manutenzione (UI51ServiceScreen): appena l'accesso e' pronto e a ogni ritorno in Home.
        private void Start()
        {
            var auth = AuthBootstrapper.Instance;
            if (auth == null) return;
            auth.OnAuthReady += UI51ServiceScreen.Check;
            auth.OnAuthReady += TryRejoin;
            // Un account (non l'ospite) entra con "Accedi" dopo l'avvio: il rientro va provato da li'.
            if (auth.PlayFabAuth != null) auth.PlayFabAuth.OnLoginSuccess += OnAccountChanged;
            if (auth.IsReady) { UI51ServiceScreen.Check(); TryRejoin(); }
        }

        private void OnAccountChanged(string _) => TryRejoin();

        private void OnDestroy()
        {
            if (AuthBootstrapper.Instance == null) return;
            AuthBootstrapper.Instance.OnAuthReady -= UI51ServiceScreen.Check;
            AuthBootstrapper.Instance.OnAuthReady -= TryRejoin;
            if (AuthBootstrapper.Instance.PlayFabAuth != null) AuthBootstrapper.Instance.PlayFabAuth.OnLoginSuccess -= OnAccountChanged;
        }

        // App chiusa durante una partita online e riaperta entro 60 s (scelta dell'utente 01/10): si torna al tavolo e l'abbandono non conta.
        private void TryRejoin()
        {
            if (rejoin != null || PhotonNetwork.InRoom) return;
            string room = ModerationService.RejoinRoom(out int seconds);
            if (room != null) rejoin = StartCoroutine(Rejoin(room, seconds));
        }

        private IEnumerator Rejoin(string room, int seconds)
        {
            ModerationService.RejoinStarted();
            rejoinGaveUp = false;
            float until = Time.unscaledTime + seconds, nextAttempt = 0f;
            int attempt = 0;
            // Subito dopo un'uscita forzata Photon tiene ancora attivo il vecchio collegamento (~10 s): si riprova finche' c'e' posto.
            while (!rejoinGaveUp && Time.unscaledTime < until)
            {
                // Solo con Photon collegato all'account del posto: dopo "Accedi" Photon si ricollega (RebindPhoton), prima e' quello vecchio.
                if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InRoom && SameAccount && Time.unscaledTime >= nextAttempt)
                {
                    if (attempt >= MaxAttempts) break;
                    attempt++;
                    nextAttempt = Time.unscaledTime + 3f;
                    PhotonNetwork.RejoinRoom(room);
                }
                UI51ConnectionOverlay.ShowReconnecting(attempt, MaxAttempts, Mathf.CeilToInt(until - Time.unscaledTime));
                yield return new WaitForSecondsRealtime(0.25f);
            }
            rejoin = null;
            UI51ConnectionOverlay.Hide();
            ModerationService.RejoinFinished(false);
        }

        private static bool SameAccount =>
            AuthBootstrapper.Instance != null && AuthBootstrapper.Instance.PlayFabAuth != null
            && PhotonNetwork.LocalPlayer.UserId == AuthBootstrapper.Instance.PlayFabAuth.PlayFabId;

        public override void OnJoinedRoom()
        {
            if (rejoin == null) return;
            StopCoroutine(rejoin);
            rejoin = null;
            UI51ConnectionOverlay.Hide();
            // Nessun altro attivo al tavolo: nessuno ha lo stato della partita da mandare, il rientro non serve.
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LeaveRoom(false);
                ModerationService.RejoinFinished(false);
                return;
            }
            // Il tavolo legge la config salvata (GameSceneInitializer): con quella dell'allenamento giocherebbe offline.
            var config = MatchConfigStorage.Load();
            if (config.Intent == MatchIntent.Training)
            {
                var props = PhotonNetwork.CurrentRoom.CustomProperties;
                config.Intent = MatchIntent.QuickMatch;
                if (props.ContainsKey(MatchmakingManager.PropFormat)) config.Format = (GameFormat)(int)props[MatchmakingManager.PropFormat];
                if (props.ContainsKey(MatchmakingManager.PropTarget)) config.TargetScore = (int)props[MatchmakingManager.PropTarget];
                MatchConfigStorage.Save(config);
            }
            // La scena del tavolo la carica Photon (AutomaticallySyncScene), lo stato lo chiede NetworkGameController.
            ModerationService.RejoinFinished(true);
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            // Vecchio collegamento ancora attivo: si riprova. Stanza finita, posto scaduto o chiusa: si rinuncia.
            if (rejoin != null && returnCode != ErrorCode.JoinFailedFoundActiveJoiner) rejoinGaveUp = true;
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            if (cause == DisconnectCause.DisconnectByClientLogic || cause == DisconnectCause.ApplicationQuit) return; // uscita voluta (fine partita, annulla ricerca, esci)
            if (startScreen == null || !startScreen.HasEntered || routine != null) return;
            routine = StartCoroutine(Reconnect(QuietSeconds));
        }

        private IEnumerator Reconnect(float quietSeconds)
        {
            float quietUntil = Time.unscaledTime + quietSeconds;
            int attempt = 0;
            float nextAttempt = 0f;
            while (!PhotonNetwork.IsConnectedAndReady)
            {
                bool idle = PhotonNetwork.NetworkClientState == ClientState.Disconnected;
                if (idle && attempt >= MaxAttempts) break;
                if (idle && Time.unscaledTime >= nextAttempt)
                {
                    attempt++;
                    nextAttempt = Time.unscaledTime + 3f;
                    Connect();
                }
                if (Time.unscaledTime >= quietUntil) UI51ConnectionOverlay.ShowReconnecting(attempt, MaxAttempts, -1);
                yield return new WaitForSecondsRealtime(0.25f);
            }
            routine = null;
            if (PhotonNetwork.IsConnectedAndReady) { UI51ConnectionOverlay.Hide(); yield break; }
            UI51ConnectionOverlay.ShowError(false, Retry, UI51ConnectionOverlay.Hide);
        }

        /// <summary>
        /// Un tentativo, sempre con l'account di adesso. PhotonNetwork.Reconnect() riusa il biglietto dell'ultimo accesso: va bene per un
        /// calo di rete, non se il biglietto e' scaduto o rifiutato, ne' durante un cambio di account (RebindPhoton toglie credenziali e
        /// indirizzo, e Reconnect non parte). Li' un token nuovo da PlayFab (AuthBootstrapper.ReconnectPhotonNow).
        /// </summary>
        private static void Connect()
        {
            var cause = PhotonNetwork.NetworkingClient.DisconnectedCause;
            bool stale = PhotonNetwork.AuthValues == null || cause == DisconnectCause.AuthenticationTicketExpired
                || cause == DisconnectCause.InvalidAuthentication || cause == DisconnectCause.CustomAuthenticationFailed;
            if (stale || !PhotonNetwork.Reconnect()) AuthBootstrapper.Instance?.ReconnectPhotonNow();
        }

        // Ricollegati dopo "Nessuna connessione" (per esempio il cambio di account finito dopo i tentativi): via l'avviso.
        public override void OnConnectedToMaster()
        {
            if (routine == null && rejoin == null && startScreen != null && startScreen.HasEntered) UI51ConnectionOverlay.Hide();
        }

        private void Retry()
        {
            if (routine == null) routine = StartCoroutine(Reconnect(0f));
        }

        public override void OnDisable()
        {
            base.OnDisable();
            routine = null;
            if (rejoin != null) { rejoin = null; ModerationService.RejoinFinished(PhotonNetwork.InRoom); }
            UI51ConnectionOverlay.Hide();
        }
    }
}
