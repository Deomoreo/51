using Project51.Auth;
using Project51.UI51;
using Project51.UIV2.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UI
{
    /// <summary>
    /// UI51 Fase 11: Gioco online sospeso (mockup Sospensione). Compare all'arrivo in Home la prima volta per ogni sospensione e
    /// ogni volta che si prova a giocare online (BlocksOnline); contro i bot si gioca sempre. Lo stato e' di ModerationService.
    /// Grafica: UI51AccountBuilder (Tools/UI51/Build Fase 11).
    /// </summary>
    public sealed class UI51SuspensionView : MonoBehaviour
    {
        [SerializeField] private Button back, play, rules;
        [SerializeField] private TMP_Text text, reason, left, note;
        [SerializeField] private QuickSelectionPanels modes;
        [SerializeField] private LegalModalV2 legal;

        private void Awake()
        {
            back.onClick.AddListener(Close);
            play.onClick.AddListener(() => { Close(); if (modes != null) modes.OpenTraining(); });
            rules.onClick.AddListener(() => { Close(); if (legal != null) legal.ShowTerms("3"); }); // Regole di comportamento (fair play)
        }

        /// <summary>
        /// Il server non ha fatto creare o entrare in una stanza (webhook Photon RoomCreated/RoomBeforeJoin nel CloudScript, errore 32752),
        /// per esempio sospensione arrivata dopo l'ultimo arrivo in Home: si rilegge lo stato e, se e' sospeso, si apre questa schermata.
        /// Sospeso: solo questa schermata (la ricerca si e' gia' chiusa); altrimenti showError col messaggio.
        /// </summary>
        public static void ServerRefused(System.Action<string> showError)
        {
            ModerationService.CheckNow(() =>
            {
                if (!BlocksOnline()) showError?.Invoke("Il gioco online non è disponibile in questo momento. Riprova tra poco.");
            });
        }

        /// <summary>
        /// Prima di ogni partita online: il server non rifiuta piu' l'ingresso nelle stanze (2.62), quindi una sospensione arrivata dopo
        /// l'ultimo arrivo in Home si legge qui (stato fresco, al massimo uno ogni 10 s). Sospeso: questa schermata e blocked; se no go.
        /// Senza rete vale l'ultimo stato noto.
        /// </summary>
        public static void WhenOnlineAllowed(System.Action go, System.Action blocked = null)
        {
            if (BlocksOnline()) { blocked?.Invoke(); return; }
            ModerationService.CheckNow(() => { if (BlocksOnline()) blocked?.Invoke(); else go(); }, 10f);
        }

        /// <summary>Vero (e apre questa schermata) se il gioco online e' sospeso: chi chiama non parte.</summary>
        public static bool BlocksOnline()
        {
            if (!ModerationService.IsSuspended) return false;
            var view = FindObjectOfType<UI51SuspensionView>(true);
            if (view != null) view.Open();
            return true;
        }

        public void Open()
        {
            string why = ModerationService.Reason;
            text.text = Body(why);
            reason.text = ReasonLabel(why);
            note.text = ModerationService.Times > 1
                ? "Gioca in modo corretto e non avrai nessun problema."
                : "Se succede ancora la sospensione sarà più lunga. Prima volta? Nessun problema: basta giocare in modo corretto.";
            left.text = Countdown(ModerationService.SecondsLeft);
            gameObject.SetActive(true);
            UIAnim.FadeIn((RectTransform)transform, 0.25f);
        }

        public void Close() => gameObject.SetActive(false);

        private void Update()
        {
            if (!ModerationService.IsSuspended) { Close(); return; }
            left.text = Countdown(ModerationService.SecondsLeft);
        }

        /// <summary>"1g 23h 59m", "23h 59m"; sotto l'ora "42:18" (scelta dell'utente 01/10: niente secondi quando manca tanto).</summary>
        public static string Countdown(int seconds)
        {
            seconds = Mathf.Max(0, seconds);
            int days = seconds / 86400, h = seconds % 86400 / 3600, m = seconds % 3600 / 60, s = seconds % 60;
            if (seconds < 3600) return $"{m:00}:{s:00}";
            return days > 0 ? $"{days}g {h}h {m}m" : $"{h}h {m}m";
        }

        /// <summary>Riga MOTIVO: i due motivi automatici, oppure il testo scritto a mano in Game Manager.</summary>
        public static string ReasonLabel(string reason)
        {
            if (reason == "abbandoni") return "Abbandoni ripetuti";
            if (reason == "segnalazioni") return "Segnalazioni dei giocatori";
            return string.IsNullOrWhiteSpace(reason) ? "Comportamento scorretto" : reason.Trim();
        }

        public static string Body(string reason)
        {
            string first = reason == "abbandoni" ? "Hai lasciato a metà diverse partite online."
                : reason == "segnalazioni" ? "Abbiamo ricevuto diverse segnalazioni sulle tue ultime partite."
                : "Nelle ultime partite non sono state rispettate le regole di comportamento.";
            return first + " Per un po’ non puoi giocare online, ma puoi continuare ad allenarti contro i bot.";
        }
    }
}
