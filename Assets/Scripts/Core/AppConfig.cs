using UnityEngine;

namespace Project51.Core
{
    /// <summary>
    /// Configurazione centrale dell'app: indirizzi pubblici e identificativi di servizio che non
    /// stanno nel codice. Vive in Resources/AppConfig e si modifica dall'Inspector.
    ///
    /// ATTENZIONE: qui dentro va SOLO roba pubblica. La Title Secret Key di PlayFab non deve mai
    /// finire in Unity, nei PlayerPrefs, in Resources o nel repository: sta solo sul backend come
    /// variabile d'ambiente. Tutto quello che e' dentro l'app e' leggibile da chi la scarica.
    /// </summary>
    [CreateAssetMenu(fileName = "AppConfig", menuName = "51/App Config")]
    public sealed class AppConfig : ScriptableObject
    {
        public const string ResourcesPath = "AppConfig";

        [Header("Indirizzi pubblici (https://...)")]
        [Tooltip("Pagina pubblica dei Termini di servizio. Vuoto = in app si leggono solo dal testo incluso.")]
        public string TermsUrl;
        [Tooltip("Pagina pubblica della Privacy Policy. Vuoto = in app si legge solo dal testo incluso.")]
        public string PrivacyUrl;
        [Tooltip("Pagina pubblica per richiedere l'eliminazione dell'account (richiesta da Google Play).")]
        public string DeleteAccountUrl;
        [Tooltip("Pagina che apre il link dell'email di recupero password.")]
        public string ResetPasswordUrl;
        [Tooltip("Base del backend che convalida i ticket e cancella gli account. Senza, l'eliminazione in app non puo' funzionare.")]
        public string BackendBaseUrl;

        [Header("PlayFab")]
        [Tooltip("ID del modello di email creato in Game Manager (Account Recovery). Vuoto = PlayFab usa il suo modello predefinito.")]
        public string PasswordRecoveryEmailTemplateId;

        private static AppConfig instance;

        /// <summary>Configurazione caricata da Resources. Puo' essere nulla se l'asset non c'e'.</summary>
        public static AppConfig Instance
        {
            get
            {
                if (instance == null) instance = Resources.Load<AppConfig>(ResourcesPath);
                return instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() => instance = null;

        /// <summary>Valore configurato, o null se l'asset manca o il campo e' ancora vuoto.</summary>
        private static string Get(System.Func<AppConfig, string> field)
        {
            var config = Instance;
            if (config == null) return null;
            string value = field(config);
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        public static string Terms => Get(c => c.TermsUrl);
        public static string Privacy => Get(c => c.PrivacyUrl);
        public static string DeleteAccount => Get(c => c.DeleteAccountUrl);
        public static string ResetPassword => Get(c => c.ResetPasswordUrl);
        public static string Backend => Get(c => c.BackendBaseUrl);
        public static string RecoveryEmailTemplateId => Get(c => c.PasswordRecoveryEmailTemplateId);

        /// <summary>
        /// Sostituisce nei testi i segnaposto {NOME}. Un segnaposto ancora senza valore non viene
        /// lasciato a schermo: sparisce insieme alla frase che lo conteneva (vedi LegalDocuments),
        /// perche' far leggere "{DELETE_ACCOUNT_URL}" a un utente e' peggio che non dire nulla.
        /// </summary>
        public static string Resolve(string token)
        {
            switch (token)
            {
                case "TERMS_URL": return Terms;
                case "PRIVACY_URL": return Privacy;
                case "DELETE_ACCOUNT_URL": return DeleteAccount;
                case "RESET_PASSWORD_URL": return ResetPassword;
                case "BACKEND_BASE_URL": return Backend;
                case "PLAYFAB_EMAIL_TEMPLATE_ID": return RecoveryEmailTemplateId;
                default: return null;
            }
        }
    }
}
