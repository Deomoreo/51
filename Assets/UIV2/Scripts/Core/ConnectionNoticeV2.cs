using DG.Tweening;
using Project51.Core;
using TMPro;
using UnityEngine;

namespace Project51.UIV2.Core
{
    /// <summary>
    /// Avviso in alto al tavolo per gli eventi di connessione (GamePresentation.ConnectionNotice):
    /// giocatore disconnesso/rientrato, nostra riconnessione in corso. Non blocca il tocco.
    /// </summary>
    public sealed class ConnectionNoticeV2 : MonoBehaviour
    {
        public CanvasGroup Group;
        public TMP_Text Message;
        private Tween hideTween;

        private void Awake()
        {
            GamePresentation.ConnectionNotice += Show;
            Group.blocksRaycasts = false;
            Group.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            GamePresentation.ConnectionNotice -= Show;
            hideTween?.Kill();
            Group.DOKill();
        }

        private void Show(string message, float seconds)
        {
            hideTween?.Kill();
            Group.DOKill();
            if (string.IsNullOrEmpty(message))
            {
                Hide();
                return;
            }
            Message.text = message;
            if (!Group.gameObject.activeSelf)
            {
                Group.gameObject.SetActive(true);
                Group.alpha = 0f;
            }
            Group.DOFade(1f, 0.2f).SetUpdate(true).SetLink(gameObject);
            if (seconds > 0f) hideTween = DOVirtual.DelayedCall(seconds, Hide, ignoreTimeScale: true).SetLink(gameObject);
        }

        private void Hide()
        {
            Group.DOKill();
            Group.DOFade(0f, 0.25f).SetUpdate(true).SetLink(gameObject).OnComplete(() => Group.gameObject.SetActive(false));
        }
    }
}
