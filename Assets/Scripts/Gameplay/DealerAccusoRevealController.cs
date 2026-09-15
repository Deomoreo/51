using System.Collections;
using UnityEngine;
using TMPro;

namespace Project51.Unity
{
    /// <summary>
    /// Banner di testo per l'esito dell'accuso del dealer (Dealer15/Dealer30). Le carte vere
    /// vengono mostrate sul tavolo (carte "fantasma", vedi TurnController.PlayDealerAccusoRevealIfAny
    /// e CardViewManager.SpawnGhostCardView) - questo componente si limita al messaggio, un
    /// piccolo banner e non un pannello a schermo intero, cosi' non copre le carte sul tavolo
    /// dietro di lui. Grafica provvisoria, stesso spirito della roulette dealer.
    /// </summary>
    public class DealerAccusoRevealController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text resultText;

        [SerializeField] private float holdSeconds = 1.6f;

        public IEnumerator ShowMessage(string message)
        {
            if (panelRoot == null || resultText == null) yield break;

            resultText.text = message;
            panelRoot.SetActive(true);

            yield return new WaitForSeconds(holdSeconds);

            panelRoot.SetActive(false);
        }
    }
}
