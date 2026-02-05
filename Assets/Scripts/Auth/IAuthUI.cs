namespace Project51.Auth
{
    /// <summary>
    /// Interfaccia per l'UI di autenticazione.
    /// Implementare su un MonoBehaviour nella scena per ricevere callback di stato.
    /// </summary>
    public interface IAuthUI
    {
        /// <summary>
        /// Mostra/nasconde lo stato di caricamento.
        /// </summary>
        /// <param name="show">True per mostrare, false per nascondere.</param>
        /// <param name="message">Messaggio opzionale da mostrare.</param>
        void ShowLoading(bool show, string message = null);
        
        /// <summary>
        /// Mostra un messaggio di errore.
        /// </summary>
        /// <param name="errorMessage">Messaggio di errore da mostrare.</param>
        /// <param name="canRetry">True se l'utente può ritentare.</param>
        void ShowError(string errorMessage, bool canRetry = true);
        
        /// <summary>
        /// Imposta il badge di stato guest/protetto.
        /// </summary>
        /// <param name="isGuest">True se l'account è guest (non protetto).</param>
        void SetGuestBadge(bool isGuest);
        
        /// <summary>
        /// Imposta il nome del giocatore visualizzato.
        /// </summary>
        /// <param name="playerName">Nome del giocatore.</param>
        void SetPlayerName(string playerName);
        
        /// <summary>
        /// Chiamato quando l'autenticazione è completata con successo.
        /// </summary>
        void OnAuthReady();
        
        /// <summary>
        /// Chiamato quando lo stato di autenticazione cambia.
        /// </summary>
        /// <param name="newState">Nuovo stato.</param>
        void OnAuthStateChanged(AuthState newState);
    }
}
