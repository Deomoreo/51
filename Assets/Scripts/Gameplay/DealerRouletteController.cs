using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity
{
    /// <summary>
    /// Scelta del mazziere a inizio smazzata (mockup 27_selezione_mazziere_ciclo e
    /// 28_selezione_mazziere_risultato): un riquadro per ogni giocatore al suo posto, il bordo oro
    /// gira rallentando fino a fermarsi sul mazziere, poi trofeo, chip MAZZIERE e CONTINUA.
    /// Se nessuno preme CONTINUA il pannello si chiude da solo dopo autoContinueSeconds.
    /// Grafica costruita da Tools/UIV2/Build Dealer Roulette.
    /// </summary>
    public class DealerRouletteController : MonoBehaviour
    {
        [Tooltip("Posti: 0=Locale (basso), 1=Sinistra, 2=Alto, 3=Destra - stessa convenzione di PlayerBannerManager.")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private GameObject[] slotRoots = new GameObject[4];
        [SerializeField] private TMP_Text[] slotNameTexts = new TMP_Text[4];
        [SerializeField] private Image[] slotRings = new Image[4];
        [SerializeField] private GameObject[] slotTrophies = new GameObject[4];
        [SerializeField] private GameObject[] slotDealerChips = new GameObject[4];
        [Tooltip("Bagliore oro dietro al riquadro evidenziato (Bagliore morbido rettangolo).")]
        [SerializeField] private Image[] slotGlows = new Image[4];
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button continueButton;
        [Tooltip("Sfocatura vera del tavolo sotto al velo. Facoltativa.")]
        [SerializeField] private BackdropBlur backdropBlur;

        [Header("Tempi")]
        [SerializeField] private int loops = 3;
        [Tooltip("Pausa fra la comparsa del pannello e il primo scatto: prima si vede, poi si sente.")]
        [SerializeField] private float startDelaySeconds = 0.5f;
        [SerializeField] private float minStepDelay = 0.16f;
        [SerializeField] private float maxStepDelay = 0.5f;
        [Tooltip("Dopo il risultato, il pannello si chiude da solo se non si preme CONTINUA.")]
        [SerializeField] private float autoContinueSeconds = 4f;

        [Header("Bagliore")]
        [SerializeField, Range(0f, 1f)] private float cycleGlowAlpha = 0.45f;
        [SerializeField, Range(0f, 1f)] private float winnerGlowAlpha = 0.85f;

        [Header("Colori")]
        [SerializeField] private Color ringNormal = new Color32(53, 78, 115, 255);
        [SerializeField] private Color ringActive = new Color32(232, 178, 74, 255);
        [SerializeField] private Color nameNormal = new Color32(170, 182, 200, 255);
        [SerializeField] private Color nameActive = Color.white;

        private bool continuePressed;

        private void Awake()
        {
            if (continueButton != null) continueButton.onClick.AddListener(() => continuePressed = true);
        }

        /// <summary>
        /// Fa girare la selezione tra i posti occupati e la ferma su winningSlot. Non blocca mai la
        /// sequenza di TurnController: se il pannello non e' configurato esce subito.
        /// </summary>
        public IEnumerator PlayRoulette(string[] namesBySlot, int winningSlot, int playerCount)
        {
            if (panelRoot == null || namesBySlot == null || namesBySlot.Length != 4 || slotRoots == null || slotRoots.Length != 4) yield break;

            // Ordine orario come al tavolo: basso, sinistra, alto, destra. In 1v1 solo basso e alto.
            var slots = playerCount == 2 ? new List<int> { 0, 2 } : new List<int> { 0, 1, 2, 3 };
            int winnerStep = Mathf.Max(0, slots.IndexOf(winningSlot));

            // Foto sfocata del tavolo presa prima che il pannello compaia.
            if (backdropBlur != null) yield return backdropBlur.Capture();
            panelRoot.SetActive(true);
            continuePressed = false;
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            if (statusText != null) statusText.text = "Selezione in corso...";

            for (int i = 0; i < 4; i++)
            {
                bool used = slots.Contains(i);
                if (slotRoots[i] != null) slotRoots[i].SetActive(used);
                if (Get(slotNameTexts, i) != null) slotNameTexts[i].text = namesBySlot[i];
                SetHighlight(i, false);
                SetActive(slotTrophies, i, false);
                SetActive(slotDealerChips, i, false);
            }

            // Il pannello si mostra prima di cominciare a girare: senza questa pausa il primo
            // scatto (e il suo suono) arrivava nello stesso istante in cui compariva la grafica.
            if (startDelaySeconds > 0f) yield return new WaitForSeconds(Project51.Core.GamePreferences.Scaled(startDelaySeconds));

            int totalSteps = loops * slots.Count + winnerStep + 1;
            int current = -1;
            for (int step = 0; step < totalSteps; step++)
            {
                if (current >= 0) SetHighlight(current, false);
                current = slots[step % slots.Count];
                SetHighlight(current, true);
                GameAudio.Play(SoundId.UiTab, 0.6f);

                // Rallenta sempre di piu' (t*t): l'ultimo tratto si legge come "sta per fermarsi".
                float t = totalSteps > 1 ? (float)step / (totalSteps - 1) : 1f;
                yield return new WaitForSeconds(Project51.Core.GamePreferences.Scaled(Mathf.Lerp(minStepDelay, maxStepDelay, t * t)));
            }

            SetActive(slotTrophies, current, true);
            SetActive(slotDealerChips, current, true);
            SetGlow(current, winnerGlowAlpha);
            GameAudio.Play(SoundId.UiConfirm, 0.9f, GameAudio.Sync.Onset);
            if (statusText != null)
            {
                string name = namesBySlot[current];
                statusText.text = name == "Tu" ? "Distribuisci tu per primo/a" : $"{name} distribuirà per primo/a";
            }
            if (continueButton != null) continueButton.gameObject.SetActive(true);

            float elapsed = 0f;
            while (!continuePressed && elapsed < autoContinueSeconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            panelRoot.SetActive(false);
        }

        private void SetHighlight(int slot, bool on)
        {
            if (Get(slotRings, slot) != null) slotRings[slot].color = on ? ringActive : ringNormal;
            if (Get(slotNameTexts, slot) != null) slotNameTexts[slot].color = on ? nameActive : nameNormal;
            SetGlow(slot, on ? cycleGlowAlpha : 0f);
        }

        private void SetGlow(int slot, float alpha)
        {
            var glow = Get(slotGlows, slot);
            if (glow == null) return;
            glow.gameObject.SetActive(alpha > 0f);
            var color = glow.color;
            color.a = alpha;
            glow.color = color;
        }

        private static T Get<T>(T[] items, int index) where T : Object
        {
            return items != null && index >= 0 && index < items.Length ? items[index] : null;
        }

        private static void SetActive(GameObject[] items, int index, bool active)
        {
            var item = Get(items, index);
            if (item != null) item.SetActive(active);
        }
    }
}
