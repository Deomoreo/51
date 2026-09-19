using System.Collections;
using DG.Tweening;
using Project51.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity
{
    /// <summary>
    /// Esito dell'accuso del mazziere (Dealer15/Dealer30): cartello "SCOPA DA 30!" con bagliore, che
    /// entra a scatto sopra al tavolo e sparisce mentre le carte volano nel mazzetto del mazziere.
    /// Le carte vere restano visibili sotto (vedi TurnController.PlayDealerAccusoRevealIfAny).
    /// Grafica costruita da Tools/UIV2/Build Dealer Accuso Reveal.
    /// </summary>
    public class DealerAccusoRevealController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform window;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private Image glow;

        [Header("Tempi")]
        [SerializeField] private float popSeconds = 0.28f;
        [SerializeField] private float holdSeconds = 1.5f;
        [SerializeField] private float fadeSeconds = 0.3f;

        /// <summary>Mostra il cartello e lo tiene; poi va chiuso con HideAnimated().</summary>
        public IEnumerator Show(string title, string detail)
        {
            if (panelRoot == null || resultText == null) yield break;

            resultText.text = title;
            if (detailText != null) detailText.text = detail ?? string.Empty;
            panelRoot.SetActive(true);

            if (group != null)
            {
                group.DOKill();
                group.alpha = 0f;
                DOTween.To(() => group.alpha, a => group.alpha = a, 1f, popSeconds * 0.6f).SetUpdate(true).SetTarget(group);
            }
            if (window != null)
            {
                window.DOKill();
                window.localScale = Vector3.one * 0.82f;
                window.DOScale(1f, popSeconds).SetEase(Ease.OutBack).SetUpdate(true);
            }
            if (glow != null)
            {
                glow.DOKill();
                var start = glow.color;
                start.a = 0f;
                glow.color = start;
                DOTween.To(() => glow.color.a, a => { var c = glow.color; c.a = a; glow.color = c; }, 0.85f, popSeconds).SetUpdate(true).SetTarget(glow);
            }

            yield return new WaitForSeconds(GamePreferences.Scaled(popSeconds + holdSeconds));
        }

        /// <summary>Sparisce senza bloccare: si sovrappone al volo delle carte verso il mazzetto.</summary>
        public void HideAnimated()
        {
            if (panelRoot == null || !panelRoot.activeSelf) return;
            float seconds = GamePreferences.Scaled(fadeSeconds);
            if (group == null)
            {
                panelRoot.SetActive(false);
                return;
            }

            group.DOKill();
            DOTween.To(() => group.alpha, a => group.alpha = a, 0f, seconds).SetUpdate(true).SetTarget(group)
                .OnComplete(() => panelRoot.SetActive(false));
            if (window != null) window.DOScale(0.94f, seconds).SetUpdate(true);
        }

        /// <summary>Compatibilita': mostra e chiude da solo (usata dai vecchi richiami).</summary>
        public IEnumerator ShowMessage(string message)
        {
            yield return Show(message, null);
            HideAnimated();
        }

        private void OnDestroy()
        {
            if (group != null) group.DOKill();
            if (window != null) window.DOKill();
            if (glow != null) glow.DOKill();
        }
    }
}
