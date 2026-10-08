using Project51.Auth;
using Project51.UIV2.Components;
using TMPro;
using UnityEngine;

namespace Project51.Unity.UI
{
    /// <summary>
    /// Build 3 #25 (scelta utente 07/10): monete e gemme in alto in Posta e Premi, cosi' il saldo nuovo si vede subito dopo un riscatto.
    /// Pillole della Home (UI51HomeBuilder.WalletPill), nascoste finche' il portafoglio non e' caricato.
    /// </summary>
    public sealed class UI51WalletPills : MonoBehaviour
    {
        [SerializeField] private GameObject group;
        [SerializeField] private TMP_Text coinsLabel, gemsLabel;

        private void OnEnable()
        {
            WalletService.Changed += Show;
            Show();
        }

        private void OnDisable() => WalletService.Changed -= Show;

        private void Show()
        {
            if (group != null) group.SetActive(WalletService.IsLoaded);
            if (coinsLabel != null) coinsLabel.text = UIV2TopBar.Amount(WalletService.Coins);
            if (gemsLabel != null) gemsLabel.text = UIV2TopBar.Amount(WalletService.Gems);
        }
    }
}
