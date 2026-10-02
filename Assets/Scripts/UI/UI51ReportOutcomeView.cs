using System;
using System.Globalization;
using Project51.Auth;
using Project51.UI51;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UI
{
    /// <summary>
    /// UI51 Fase 11: Esito segnalazione (mockup SegnalazioneEsito). All'arrivo in Home, quando giocatori segnalati sono stati
    /// sanzionati: il server lo dice una volta sola (ModerationService.Refresh), tutti insieme (scelta dell'utente 01/10: non perderne
    /// nessuno). Per privacy niente nomi, solo data e formato.
    /// Grafica: UI51AccountBuilder (Tools/UI51/Build Fase 11).
    /// </summary>
    public sealed class UI51ReportOutcomeView : MonoBehaviour
    {
        [SerializeField] private Button ok, scrim;
        [SerializeField] private RectTransform card;
        [SerializeField] private TMP_Text title, text, info;

        private static readonly string[] Months =
            { "gennaio", "febbraio", "marzo", "aprile", "maggio", "giugno", "luglio", "agosto", "settembre", "ottobre", "novembre", "dicembre" };

        private void Awake()
        {
            ok.onClick.AddListener(Close);
            scrim.onClick.AddListener(Close);
        }

        public void Open(ServerReportOutcome[] outcomes)
        {
            int n = outcomes.Length;
            if (title != null) title.text = n == 1 ? "Grazie per la segnalazione!" : "Grazie per le segnalazioni!";
            if (text != null)
                text.text = (n == 1 ? "Un giocatore che hai segnalato è stato sanzionato." : n + " giocatori che hai segnalato sono stati sanzionati.") +
                            " Le tue segnalazioni aiutano a mantenere le partite corrette per tutti.";
            info.text = Lines(outcomes);
            gameObject.SetActive(true);
            UIAnim.FadeIn((RectTransform)transform, 0.2f);
            UIAnim.PopDialog(card);
        }

        public void Close() => gameObject.SetActive(false);

        /// <summary>"Segnalazione del 28 settembre · partita 1 vs 1. Per privacy non mostriamo il nome."</summary>
        public static string Info(string date, string mode) => Line(date, mode) + ". Per privacy non mostriamo il nome.";

        /// <summary>Una riga per esito (al massimo 3, poi "e altre N"), poi la nota sulla privacy.</summary>
        public static string Lines(ServerReportOutcome[] outcomes)
        {
            if (outcomes.Length == 1) return Info(outcomes[0].data, outcomes[0].modo);
            var lines = new System.Collections.Generic.List<string>();
            for (int i = 0; i < outcomes.Length && i < 3; i++) lines.Add(Line(outcomes[i].data, outcomes[i].modo) + ".");
            if (outcomes.Length > 3) lines.Add("E altre " + (outcomes.Length - 3) + ".");
            lines.Add("Per privacy non mostriamo i nomi.");
            return string.Join("\n", lines);
        }

        private static string Line(string date, string mode)
        {
            string when = DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
                ? " del " + d.Day + " " + Months[d.Month - 1] : string.Empty;
            string game = string.IsNullOrWhiteSpace(mode) ? string.Empty : " · partita " + mode.Trim();
            return "Segnalazione" + when + game;
        }
    }
}
