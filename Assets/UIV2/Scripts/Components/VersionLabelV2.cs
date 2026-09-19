using TMPro;
using UnityEngine;

namespace Project51.UIV2.Components
{
    /// <summary>
    /// Scrive il numero di versione vero all'avvio. Il testo lasciato dai builder invecchia a ogni
    /// pubblicazione (la schermata iniziale mostrava "v1.77" a versione 1.82): meglio leggerlo dal
    /// progetto che ricordarsi di riscriverlo a mano.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class VersionLabelV2 : MonoBehaviour
    {
        [Tooltip("Scritto prima del numero, es. \"v\".")]
        public string Prefix = "v";

        private void Awake() => GetComponent<TMP_Text>().text = Prefix + Application.version;
    }
}
