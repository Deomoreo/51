using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Project51.Core
{
    /// <summary>
    /// Termini di servizio e Privacy Policy: i testi stanno in Assets/Legal come file, non nel
    /// codice, cosi' si aggiornano senza ricompilare. Qui vengono solo preparati per essere letti a
    /// schermo - titoli in evidenza e segnaposto risolti.
    /// </summary>
    public static class LegalDocuments
    {
        private static readonly Regex Placeholder = new Regex(@"\{([A-Z_]+)\}");
        private static readonly Regex Bold = new Regex(@"\*\*(.+?)\*\*");

        /// <summary>Colore dei titoli di sezione, in tono con il resto della UI.</summary>
        private const string HeadingColor = "#F5D79C";

        /// <summary>
        /// Prepara un documento per TextMeshPro: le righe markdown "# Titolo" e "## 1. Sezione"
        /// diventano titoli in oro e grassetto, il resto resta com'e'.
        /// </summary>
        public static string Format(TextAsset asset)
        {
            if (asset == null) return string.Empty;
            return Format(asset.text);
        }

        public static string Format(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;

            var output = new StringBuilder(raw.Length + 256);
            foreach (string line in raw.Replace("\r\n", "\n").Split('\n'))
            {
                string trimmed = line.TrimEnd();
                if (trimmed.StartsWith("#"))
                {
                    int level = 0;
                    while (level < trimmed.Length && trimmed[level] == '#') level++;
                    string heading = trimmed.Substring(level).Trim();
                    // Il titolo del documento e' gia' nella barra del pannello: non si ripete.
                    if (level == 1) continue;
                    output.Append("<b><color=").Append(HeadingColor).Append('>')
                          .Append(heading).Append("</color></b>\n");
                    continue;
                }
                // **grassetto** del markdown -> grassetto vero, altrimenti a schermo si leggono gli asterischi.
                output.Append(Bold.Replace(trimmed, "<b>$1</b>")).Append('\n');
            }

            return ResolvePlaceholders(output.ToString()).Trim();
        }

        /// <summary>
        /// Sostituisce {NOME} col valore configurato. Se un indirizzo non e' ancora stato definito,
        /// viene tolta l'intera frase che lo conteneva invece di mostrare il segnaposto grezzo:
        /// un utente non deve mai leggere "{DELETE_ACCOUNT_URL}" dentro un documento legale.
        /// </summary>
        public static string ResolvePlaceholders(string text)
        {
            if (string.IsNullOrEmpty(text) || !Placeholder.IsMatch(text)) return text;

            var lines = text.Replace("\r\n", "\n").Split('\n');
            var output = new StringBuilder(text.Length);
            foreach (string line in lines)
            {
                string result = line;
                bool drop = false;
                foreach (Match match in Placeholder.Matches(line))
                {
                    string value = AppConfig.Resolve(match.Groups[1].Value);
                    if (string.IsNullOrEmpty(value)) { drop = true; break; }
                    result = result.Replace(match.Value, value);
                }
                if (drop) result = RemoveSentencesWithPlaceholders(line);
                output.Append(result).Append('\n');
            }
            return output.ToString();
        }

        /// <summary>Toglie solo le frasi che contengono un segnaposto irrisolto, tenendo il resto del paragrafo.</summary>
        private static string RemoveSentencesWithPlaceholders(string line)
        {
            var kept = new StringBuilder(line.Length);
            foreach (string sentence in Regex.Split(line, @"(?<=\.)\s+"))
            {
                if (Placeholder.IsMatch(sentence)) continue;
                if (kept.Length > 0) kept.Append(' ');
                kept.Append(sentence);
            }
            return kept.ToString();
        }
    }
}
