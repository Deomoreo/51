using System.Collections;
using UnityEngine;
using TMPro;

namespace Project51.Unity
{
    /// <summary>
    /// Animazione "roulette" per la scelta del mazziere a inizio smazzata: i nomi dei 4
    /// giocatori si illuminano in sequenza, sempre piu' lenta, finche' non si fermano su chi
    /// diventa davvero il dealer. Richiesta esplicita dell'utente ("una roulette con i 4 player
    /// icone e nomi e quello che viene scelto poi diventa dealer"), al posto della semplice chip
    /// statica precedente.
    /// </summary>
    public class DealerRouletteController : MonoBehaviour
    {
        [Tooltip("Ordine: 0=Locale, 1=Sinistra, 2=Alto, 3=Destra - stessa convenzione usata altrove nel progetto (vedi PlayerBannerManager.ResolveRelativeSlot).")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text[] slotNameTexts = new TMP_Text[4];
        [SerializeField] private GameObject[] slotHighlights = new GameObject[4];

        [Header("Timing (provvisorio - verra' rifatto con una vera grafica/animazione)")]
        [SerializeField] private int loops = 3;
        [Tooltip("Prima era 0.06: troppo veloce per capire cosa stesse succedendo fin dall'inizio.")]
        [SerializeField] private float minStepDelay = 0.14f;
        [SerializeField] private float maxStepDelay = 0.42f;
        [SerializeField] private float finalHoldSeconds = 1.0f;

        /// <summary>
        /// Fa girare la roulette e la ferma esattamente su winningRelativeSlot. Non fa nulla
        /// (yield break immediato) se il setup non e' completo, cosi' la sequenza dealer di
        /// TurnController puo' continuare comunque senza bloccarsi.
        /// </summary>
        public IEnumerator PlayRoulette(string[] namesByRelativeSlot, int winningRelativeSlot)
        {
            if (namesByRelativeSlot == null || namesByRelativeSlot.Length != 4) yield break;
            if (slotNameTexts == null || slotNameTexts.Length != 4 || slotHighlights == null || slotHighlights.Length != 4) yield break;

            if (panelRoot != null) panelRoot.SetActive(true);

            for (int i = 0; i < 4; i++)
            {
                if (slotNameTexts[i] != null) slotNameTexts[i].text = namesByRelativeSlot[i];
                if (slotHighlights[i] != null) slotHighlights[i].SetActive(false);
            }

            int winning = Mathf.Clamp(winningRelativeSlot, 0, 3);
            int totalSteps = loops * 4 + winning + 1;
            int currentSlot = -1;

            for (int step = 0; step < totalSteps; step++)
            {
                if (currentSlot >= 0 && slotHighlights[currentSlot] != null)
                {
                    slotHighlights[currentSlot].SetActive(false);
                }

                currentSlot = step % 4;
                if (slotHighlights[currentSlot] != null)
                {
                    slotHighlights[currentSlot].SetActive(true);
                }

                // Accelera il rallentamento verso la fine (t*t) cosi' l'ultimo tratto si legge
                // chiaramente come "sta per fermarsi", non un semplice ticchettio regolare.
                float t = totalSteps > 1 ? (float)step / (totalSteps - 1) : 1f;
                float delay = Mathf.Lerp(minStepDelay, maxStepDelay, t * t);
                yield return new WaitForSeconds(delay);
            }

            yield return new WaitForSeconds(finalHoldSeconds);

            if (panelRoot != null) panelRoot.SetActive(false);
        }
    }
}
