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

        // B10 (S3): un cambio di account (Reset) scarta la risposta ancora in volo per l'account di prima.
        private static int generation, requests;

        /// <summary>Cambio di account: saldi a zero finche' Refresh non porta quelli nuovi.</summary>
        public static void Reset()
        {
            generation++;
            Coins = Gems = 0;
            IsLoaded = false;
            Changed?.Invoke();
        }

        /// <summary>Saldo detto dal server dopo un premio (CloudScript): negativo = quella valuta resta com'era.</summary>
        public static void Apply(int coins, int gems)
        {
            requests++; // una lettura del portafoglio partita prima porterebbe il saldo vecchio
            if (coins >= 0) Coins = coins;
            if (gems >= 0) Gems = gems;
            IsLoaded = true;
            Changed?.Invoke();
        }

        public static void Refresh(Action onDone = null)
        {
            if (!PlayFabClientAPI.IsClientLoggedIn()) { onDone?.Invoke(); return; }
            int gen = generation, seq = ++requests;

            PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(),
                result =>
                {
                    if (gen != generation || seq != requests) return; // B22 (M3): conta solo la risposta dell'ultima richiesta
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
