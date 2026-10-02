using System;
using Project51.UI51;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UI
{
    /// <summary>
    /// Overlay di connessione (mockup Connessione e Conn*), sopra tutto: velo scuro che blocca i tocchi e una card.
    /// "Riconnessione…": anello che gira, archi del Wi-Fi che pulsano, tentativo N di M e, in partita, i secondi per cui il posto
    /// resta tuo. "Nessuna connessione": RIPROVA e un'uscita (Esci dalla partita / Continua offline).
    /// Prefab in Resources (UI51/Connection, costruito da UI51SystemBuilder), creato al primo uso e tenuto tra le scene.
    /// I tentativi li fanno NetworkGameController (in partita) e HomeConnectionWatcher (in Home).
    /// </summary>
    public sealed class UI51ConnectionOverlay : MonoBehaviour
    {
        public const string ResourcePath = "UI51/Connection";
        public const string HomeErrorText = "Controlla il Wi-Fi o i dati mobili e riprova. I tuoi progressi sono al sicuro.";
        public const string GameErrorText = "Non riusciamo a ricollegarti alla partita. Controlla il Wi-Fi o i dati mobili: " +
                                            "se torni in tempo puoi ancora rientrare al tavolo.";

        [SerializeField] private GameObject view;

        [Header("Riconnessione")]
        [SerializeField] private RectTransform reconnectCard;
        [SerializeField] private RectTransform spinner;
        [SerializeField] private RectTransform[] waves = new RectTransform[0];
        [SerializeField] private UI51Shape[] attemptBars = new UI51Shape[0];
        [SerializeField] private TMP_Text attemptLabel;
        [SerializeField] private GameObject seatBox;
        [SerializeField] private TMP_Text seatLabel;

        [Header("Errore")]
        [SerializeField] private RectTransform errorCard;
        [SerializeField] private RectTransform errorIcon;
        [SerializeField] private TMP_Text errorText;
        [SerializeField] private Button retry, exit;
        [SerializeField] private TMP_Text exitLabel;

        private enum Phase { Hidden, Reconnecting, Error }

        private static UI51ConnectionOverlay s_Instance;
        private Phase phase;
        private Action onRetry, onExit;

        public static bool IsShown => s_Instance != null && s_Instance.phase != Phase.Hidden;

        private static UI51ConnectionOverlay Instance
        {
            get
            {
                if (s_Instance != null) return s_Instance;
                var prefab = Resources.Load<UI51ConnectionOverlay>(ResourcePath);
                if (prefab == null) return null;
                s_Instance = Instantiate(prefab);
                s_Instance.name = prefab.name;
                DontDestroyOnLoad(s_Instance.gameObject);
                return s_Instance;
            }
        }

        /// <summary>Card "Riconnessione…". seatSeconds &lt; 0 = fuori partita (niente riquadro del posto).</summary>
        public static void ShowReconnecting(int attempt, int maxAttempts, int seatSeconds) =>
            Instance?.Reconnecting(attempt, maxAttempts, seatSeconds);

        /// <summary>Card "Nessuna connessione" con RIPROVA e l'uscita del contesto (partita o Home).</summary>
        public static void ShowError(bool inGame, Action onRetry, Action onExit) => Instance?.Error(inGame, onRetry, onExit);

        public static void Hide()
        {
            if (s_Instance == null) return;
            s_Instance.phase = Phase.Hidden;
            s_Instance.onRetry = s_Instance.onExit = null;
            s_Instance.view.SetActive(false);
        }

        private void Awake()
        {
            retry.onClick.AddListener(() => onRetry?.Invoke());
            exit.onClick.AddListener(() => onExit?.Invoke());
            view.SetActive(false);
        }

        private void Reconnecting(int attempt, int maxAttempts, int seatSeconds)
        {
            if (phase != Phase.Reconnecting)
            {
                phase = Phase.Reconnecting;
                view.SetActive(true);
                errorCard.gameObject.SetActive(false);
                reconnectCard.gameObject.SetActive(true);
                UIAnim.PopDialog(reconnectCard);
                UIAnim.Spin(spinner);
                UIAnim.WifiWave(waves);
            }
            attempt = Mathf.Clamp(attempt, 1, Mathf.Max(1, maxAttempts));
            for (int i = 0; i < attemptBars.Length; i++)
            {
                attemptBars[i].gameObject.SetActive(i < maxAttempts);
                attemptBars[i].color = i < attempt - 1 ? UI51Tokens.WithAlpha(UI51Tokens.Danger, 0.6f)
                    : i == attempt - 1 ? UI51Tokens.Gold : UI51Tokens.CreamA(0.2f);
            }
            attemptLabel.text = "Tentativo " + attempt + " di " + maxAttempts;
            seatBox.SetActive(seatSeconds >= 0);
            if (seatSeconds >= 0)
                seatLabel.text = "Il tuo posto al tavolo resta tuo per <color=#F3C969><b>" + seatSeconds + "s</b></color>. Gli altri giocatori ti aspettano.";
        }

        private void Error(bool inGame, Action retryAction, Action exitAction)
        {
            onRetry = retryAction;
            onExit = exitAction;
            errorText.text = inGame ? GameErrorText : HomeErrorText;
            exitLabel.text = inGame ? "Esci dalla partita" : "Continua offline";
            if (phase == Phase.Error) return;
            phase = Phase.Error;
            view.SetActive(true);
            reconnectCard.gameObject.SetActive(false);
            errorCard.gameObject.SetActive(true);
            UIAnim.PopDialog(errorCard);
            UIAnim.ShakeX(errorIcon);
        }
    }
}
