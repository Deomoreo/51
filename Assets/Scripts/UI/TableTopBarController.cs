using UnityEngine;
using TMPro;
using Project51.Unity;

namespace Project51.Unity.UI
{
    /// <summary>
    /// Testi dinamici della barra superiore del tavolo (Assets/UI_SPEC_Tavolo.md, sezione 2):
    /// "Mano X di Y" da RoundManager.CurrentHandNumber/TotalHands, "Carte rimaste N" da
    /// GameState.Deck.Count. Il bottone Impostazioni resta solo visivo (vedi sezione 10):
    /// nessun pannello Impostazioni esiste nel progetto, ne' come script ne' in scena.
    /// </summary>
    public class TableTopBarController : MonoBehaviour
    {
        [SerializeField] private TMP_Text handText;
        [SerializeField] private TMP_Text cardsLeftText;

        private TurnController turnController;

        private void Start()
        {
            turnController = FindObjectOfType<TurnController>();
            InvokeRepeating(nameof(Refresh), 0.2f, 0.2f);
        }

        private void Refresh()
        {
            if (turnController == null)
            {
                turnController = FindObjectOfType<TurnController>();
            }
            if (turnController == null) return;

            var roundManager = turnController.RoundManager;
            if (handText != null && roundManager != null)
            {
                handText.text = $"Mano {roundManager.CurrentHandNumber} di {roundManager.TotalHands}";
            }

            var state = turnController.GameState;
            if (cardsLeftText != null && state != null)
            {
                cardsLeftText.text = $"Carte rimaste {state.Deck.Count}";
            }
        }
    }
}
