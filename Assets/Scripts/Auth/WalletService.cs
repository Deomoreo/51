using System;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

namespace Project51.Auth
{
    /// <summary>
    /// Monete e gemme come valute virtuali PlayFab (Economy -> Currencies: codici "CO" e "GE").
    /// Solo lettura: dare o spendere valuta va fatto lato server (CloudScript / catalogo), mai dal client.
    /// Finche' la sessione non e' pronta o le valute non esistono sul titolo, i saldi restano 0.
    /// </summary>
    public static class WalletService
    {
        public const string CoinsCode = "CO";
        public const string GemsCode = "GE";

        public static int Coins { get; private set; }
        public static int Gems { get; private set; }
        public static bool IsLoaded { get; private set; }
        public static event Action Changed;

        public static void Refresh(Action onDone = null)
        {
            if (!PlayFabClientAPI.IsClientLoggedIn()) { onDone?.Invoke(); return; }

            PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(),
                result =>
                {
                    var vc = result.VirtualCurrency;
                    Coins = vc != null && vc.TryGetValue(CoinsCode, out int c) ? c : 0;
                    Gems = vc != null && vc.TryGetValue(GemsCode, out int g) ? g : 0;
                    IsLoaded = true;
                    Changed?.Invoke();
                    onDone?.Invoke();
                },
                error =>
                {
                    if (Debug.isDebugBuild) Debug.LogWarning("[WalletService] GetUserInventory fallita: " + error.GenerateErrorReport());
                    onDone?.Invoke();
                });
        }
    }
}
