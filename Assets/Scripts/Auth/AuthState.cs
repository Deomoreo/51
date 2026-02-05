namespace Project51.Auth
{
    /// <summary>
    /// Stati della macchina a stati di autenticazione.
    /// </summary>
    public enum AuthState
    {
        /// <summary>Stato iniziale, non ancora inizializzato.</summary>
        None,
        
        /// <summary>In fase di inizializzazione.</summary>
        Initializing,
        
        /// <summary>Login PlayFab guest in corso.</summary>
        LoggingInPlayFab,
        
        /// <summary>Recupero token Photon da PlayFab.</summary>
        GettingPhotonToken,
        
        /// <summary>Connessione a Photon in corso.</summary>
        ConnectingPhoton,
        
        /// <summary>Autenticazione completata, pronto.</summary>
        Ready,
        
        /// <summary>Errore durante l'autenticazione.</summary>
        Error
    }
}
