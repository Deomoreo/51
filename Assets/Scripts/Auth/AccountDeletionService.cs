using System;
using System.Collections;
using Project51.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace Project51.Auth
{
    /// <summary>Esito di una richiesta di eliminazione account, senza dettagli tecnici per la UI.</summary>
    public enum AccountDeletionOutcome
    {
        /// <summary>Il backend ha confermato l'eliminazione; logout e pulizia locale gia' fatti.</summary>
        Deleted,
        /// <summary>Backend non configurato in questa build (AppConfig.BackendBaseUrl vuoto o non valido).</summary>
        NotAvailable,
        /// <summary>Nessuna sessione PlayFab valida (non collegato, o ticket rifiutato dal backend).</summary>
        NotSignedIn,
        /// <summary>Rete assente, timeout o errore del backend.</summary>
        Failed
    }

    /// <summary>
    /// Eliminazione dell'account (H7). La UI chiama solo RequestDeletion e riceve un esito: qui dentro
    /// non c'e' nessuna chiamata PlayFab con privilegi. Il client manda al backend il SessionTicket della
    /// sessione corrente (POST {BackendBaseUrl}/api/delete-account, Authorization: Bearer ticket); e' il
    /// backend, con la Title Secret Key che vive solo li', a convalidare il ticket e cancellare il giocatore.
    ///
    /// Senza backend configurato NON si simula mai un successo: l'esito e' NotAvailable.
    /// </summary>
    public static class AccountDeletionService
    {
        public const string EndpointPath = "/api/delete-account";
        private const int TimeoutSeconds = 20;

        /// <summary>
        /// Chiavi PlayerPrefs legate all'account (identita', progressi, statistiche, collezione). Le
        /// preferenze del dispositivo (audio, formato partita, ecc.) restano.
        /// </summary>
        private static readonly string[] AccountPrefKeys =
        {
            "Project51_DeviceId", "Project51_SessionGuestId", "Project51_IsRegistered",
            "Project51_HasRealLogin", "Project51_HasEverLogged",
            "progress_exp", "progress_level", "progress_pendingExp", "progress_wins", "progress_totalGames",
            "PlayerData_V1", "PlayerNickname", "PlayerId",
            "Collection.Emoticons"
        };

        public static bool IsBusy { get; private set; }

        /// <summary>Vero se questa build ha un backend a cui mandare la richiesta.</summary>
        public static bool IsConfigured => BuildRequestUrl(AppConfig.Backend, Debug.isDebugBuild) != null;

        private static bool deletedNoticePending;

        /// <summary>
        /// Vero una sola volta dopo un'eliminazione riuscita: la schermata iniziale ricaricata lo usa per
        /// mostrare la conferma.
        /// </summary>
        public static bool ConsumeDeletedNotice()
        {
            bool pending = deletedNoticePending;
            deletedNoticePending = false;
            return pending;
        }

        /// <summary>
        /// Chiede al backend di eliminare l'account della sessione corrente. onDone arriva sempre, una
        /// volta sola, sul main thread. Se l'esito e' Deleted il logout e la pulizia locale sono gia' fatti.
        /// </summary>
        public static void RequestDeletion(Action<AccountDeletionOutcome> onDone)
        {
            if (IsBusy)
            {
                DevLog("richiesta gia' in corso, ignorata.");
                return;
            }

            string url = BuildRequestUrl(AppConfig.Backend, Debug.isDebugBuild);
            if (url == null)
            {
                DevLog(string.IsNullOrEmpty(AppConfig.Backend)
                    ? "BackendBaseUrl vuoto in Resources/AppConfig: eliminazione non disponibile in questa build."
                    : "BackendBaseUrl non valido (serve https://, http:// solo in development): " + AppConfig.Backend);
                onDone?.Invoke(AccountDeletionOutcome.NotAvailable);
                return;
            }

            var bootstrapper = AuthBootstrapper.Instance;
            string ticket = bootstrapper != null ? bootstrapper.PlayFabAuth?.SessionTicket : null;
            // Solo chi ha fatto un login vero (email/password) ha un account da eliminare: la sessione
            // ospite tecnica non contiene dati dell'utente. La UI non mostra nemmeno la voce.
            bool realAccount = bootstrapper != null && bootstrapper.PlayFabAuth != null && bootstrapper.PlayFabAuth.HasRealLogin;
            if (string.IsNullOrEmpty(ticket) || !realAccount)
            {
                DevLog(string.IsNullOrEmpty(ticket)
                    ? "nessun SessionTicket PlayFab: il giocatore non ha una sessione attiva."
                    : "sessione ospite: niente account da eliminare.");
                onDone?.Invoke(AccountDeletionOutcome.NotSignedIn);
                return;
            }

            IsBusy = true;
            bootstrapper.StartCoroutine(Send(url, ticket, onDone));
        }

        private static IEnumerator Send(string url, string ticket, Action<AccountDeletionOutcome> onDone)
        {
            AccountDeletionOutcome outcome;
            using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes("{}"));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + ticket);
                request.timeout = TimeoutSeconds;

                yield return request.SendWebRequest();

                bool networkError = request.result == UnityWebRequest.Result.ConnectionError;
                outcome = Classify(request.responseCode, networkError);
                if (outcome != AccountDeletionOutcome.Deleted)
                {
                    string body = request.downloadHandler != null ? request.downloadHandler.text : null;
                    if (body != null && body.Length > 300) body = body.Substring(0, 300);
                    DevLog($"POST {url} fallita: HTTP {request.responseCode}, {request.result}, {request.error}. Risposta: {body}");
                }
            }

            if (outcome == AccountDeletionOutcome.Deleted)
            {
                SignOutAndClearLocalData();
                deletedNoticePending = true;
            }

            IsBusy = false;
            onDone?.Invoke(outcome);
        }

        /// <summary>
        /// URL completo dell'endpoint, o null se la base manca o non e' accettabile. Fuori da development
        /// si accetta solo https: il SessionTicket non deve viaggiare in chiaro.
        /// </summary>
        public static string BuildRequestUrl(string baseUrl, bool allowHttp)
        {
            if (string.IsNullOrWhiteSpace(baseUrl)) return null;
            string trimmed = baseUrl.Trim().TrimEnd('/');
            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)) return null;
            bool secure = uri.Scheme == Uri.UriSchemeHttps;
            bool plain = uri.Scheme == Uri.UriSchemeHttp;
            if (!secure && !(plain && allowHttp)) return null;
            return trimmed + EndpointPath;
        }

        /// <summary>Solo 2xx e' un successo. 401/403 = sessione non valida. Tutto il resto e' un errore.</summary>
        public static AccountDeletionOutcome Classify(long responseCode, bool networkError)
        {
            if (networkError || responseCode == 0) return AccountDeletionOutcome.Failed;
            if (responseCode >= 200 && responseCode < 300) return AccountDeletionOutcome.Deleted;
            if (responseCode == 401 || responseCode == 403) return AccountDeletionOutcome.NotSignedIn;
            return AccountDeletionOutcome.Failed;
        }

        /// <summary>
        /// Dopo la conferma del backend: niente piu' identita' ne' dati dell'account sul dispositivo, poi
        /// logout (che disconnette Photon e riparte con un ospite nuovo, non con l'account cancellato).
        /// </summary>
        private static void SignOutAndClearLocalData()
        {
            foreach (var key in AccountPrefKeys) PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();

            if (PlayerProgressLocal.Instance != null) PlayerProgressLocal.Instance.ResetAllProgress();
            var playerData = UnityEngine.Object.FindObjectOfType<Project51.Networking.PlayerDataManager>();
            if (playerData != null) playerData.ResetPlayerData();

            var bootstrapper = AuthBootstrapper.Instance;
            if (bootstrapper != null) bootstrapper.LogoutAndRestart(clearRealAccountFlag: true);
        }

        private static void DevLog(string message)
        {
            if (Debug.isDebugBuild) Debug.LogWarning("[AccountDeletion] " + message);
        }
    }
}
