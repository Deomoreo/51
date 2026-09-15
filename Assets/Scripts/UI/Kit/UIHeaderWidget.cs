using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UIKit
{
    /// <summary>
    /// Header riutilizzabile (UI_Header): 4 sotto-aree (Profile/Currency/Energy/Progress) gia'
    /// presenti come GameObject distinti nel prefab - questo componente si limita a scrivere i
    /// valori, nessuna coordinata di schermata. Le altre schermate che condividono lo stesso
    /// header (valute/energia/livello sempre visibili) riusano il prefab intero cosi' com'e'.
    /// </summary>
    public class UIHeaderWidget : MonoBehaviour
    {
        [Header("Profile")]
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text playerNameText;

        [Header("Currency")]
        [SerializeField] private TMP_Text coinValueText;
        [SerializeField] private Button addCurrencyButton;

        [Header("Energy")]
        [SerializeField] private RectTransform energyBarFill;
        [SerializeField] private TMP_Text energyValueText;
        [SerializeField] private float energyBarMaxWidth = 140f;

        [Header("Progress")]
        [SerializeField] private TMP_Text progressText;

        public Button AddCurrencyButton => addCurrencyButton;

        public void SetLevel(int level)
        {
            if (levelText != null) levelText.text = level.ToString();
        }

        public void SetPlayerName(string name)
        {
            if (playerNameText != null) playerNameText.text = name;
        }

        public void SetCoins(int amount)
        {
            if (coinValueText != null) coinValueText.text = amount.ToString("N0");
        }

        public void SetEnergy(int current, int max)
        {
            if (energyValueText != null) energyValueText.text = current.ToString();
            if (energyBarFill != null)
            {
                float frac = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
                var sd = energyBarFill.sizeDelta;
                energyBarFill.sizeDelta = new Vector2(energyBarMaxWidth * frac, sd.y);
            }
        }

        public void SetProgress(int current, int max)
        {
            if (progressText != null) progressText.text = $"{current} / {max}";
        }
    }
}
