using DG.Tweening;
using Project51.Auth;
using Project51.UI51;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UI
{
    /// <summary>
    /// UI51 Fase 15: Forziere e ForziereAperto (mockup Forziere) per un premio con forziere, che il server ha gia' aperto
    /// (scelta utente 02/10: solo animazione, niente carte con rarita' ne' frammenti). Chiuso oscilla, al tocco si apre e girano
    /// le schede di monete e gemme del premio intero (gia' nel saldo). La aprono Premi e Posta. Grafica: UI51ProgressBuilder.
    /// </summary>
    public sealed class UI51ChestView : MonoBehaviour
    {
        [SerializeField] private GameObject closed, opened;
        [SerializeField] private Button chestButton, collect;
        [SerializeField] private RectTransform chest, tapLabel, glow, burst, smallChest, coinsCard, gemsCard, collectRect;
        [SerializeField] private Image chestImage, smallChestImage;
        [SerializeField] private Sprite green, purple;
        [SerializeField] private TMP_Text caption, title, sub, coinsText, gemsText;

        private static readonly UIKeyframes Wobble = new UIKeyframes(1.6f, UIEase.EaseInOut)
            .Track(AnimProp.Rotation, 0f, 0f, 0.1f, -4f, 0.2f, 4f, 0.3f, -3f, 0.4f, 3f, 0.5f, 0f, 1f, 0f);

        private void Awake()
        {
            chestButton.onClick.AddListener(Reveal);
            collect.onClick.AddListener(() => gameObject.SetActive(false));
        }

        /// <summary>
        /// Apre la schermata se nel premio c'e' un forziere; altrimenti falso (chi chiama festeggia a modo suo).
        /// Premi e Posta la aprono quando il server ha risposto, col premio vero (secondo giro 08/10: niente piu' premio previsto).
        /// </summary>
        public bool Open(ServerReward r)
        {
            if (r == null || r.forzieri == null || r.forzieri.Length == 0) return false;
            bool isGreen = r.forzieri[0].colore == "verde";
            chestImage.sprite = smallChestImage.sprite = isGreen ? green : purple;
            caption.text = isGreen ? "FORZIERE VERDE" : "FORZIERE VIOLA";
            Fill(r);

            gameObject.SetActive(true);
            closed.SetActive(true);
            opened.SetActive(false);
            title.text = "Hai un forziere da aprire";
            sub.text = "Dentro ci sono monete e gemme";
            UIAnim.FadeIn((RectTransform)transform, 0.25f);
            UIAnim.Pop(chest, 0.6f, 1.06f, 0.4f);
            if (UIAnim.DecorativeLoops) Wobble.Play(chestImage.rectTransform, 0.5f, -1); // sull'immagine: Play sul bottone ucciderebbe il pop
            // B21 (M1, scelta 07/10): il forziere si apre da solo dopo il pop, RACCOGLI e' l'unico tocco (prima: forziere, poi RACCOGLI).
            // Giro Android 08/10: dopo 1.4 s (era 0.7, l'apertura quasi non si vedeva).
            tapLabel.gameObject.SetActive(false);
            DOVirtual.DelayedCall(1.4f, Reveal, true).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            return true;
        }

        /// <summary>Contenuto deciso dal server.</summary>
        private void Fill(ServerReward r)
        {
            coinsText.text = "+" + r.monete;
            gemsText.text = "+" + r.gemme;
            coinsCard.gameObject.SetActive(r.monete > 0);
            gemsCard.gameObject.SetActive(r.gemme > 0);
        }

        private void Reveal()
        {
            if (opened.activeSelf) return; // gia' aperto (tocco e apertura automatica insieme)
            UIAnim.Stop(chestImage.rectTransform);
            UIAnim.Stop(tapLabel);
            closed.SetActive(false);
            opened.SetActive(true);
            title.text = "Ecco cosa hai trovato!";
            sub.text = "Già aggiunto al tuo saldo";
            UIAnim.RewardBurst(burst);
            UIAnim.Pop(smallChest, 0.6f, 1.08f, 0.35f);
            float delay = 0.45f;
            foreach (var card in new[] { coinsCard, gemsCard })
                if (card.gameObject.activeSelf) { UIAnim.Flip(card, delay); delay += 0.35f; }
            UIAnim.Pop(collectRect, 0.8f, 1.04f, 0.3f, delay + 0.2f);
        }
    }
}
