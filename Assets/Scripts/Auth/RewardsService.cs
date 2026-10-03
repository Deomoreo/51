using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Json;
using UnityEngine;

namespace Project51.Auth
{
    /// <summary>Forziere aperto dal server: colore e cosa conteneva.</summary>
    [Serializable]
    public class ServerChest
    {
        public string colore;
        public int monete, gemme;
    }

    /// <summary>Sospensione del gioco online (moderazione): secondi che mancano, motivo ("segnalazioni", "abbandoni" o un testo).</summary>
    [Serializable]
    public class ServerSuspension
    {
        public int secondi;
        public string motivo;
        public int volte;
        public string fine; // ISO: la pagina si apre una volta per ogni "fine" diversa
    }

    /// <summary>Esito di una segnalazione fatta: data "AAAA-MM-GG" e formato ("1 vs 1"). Vuoto se non c'e'.</summary>
    [Serializable]
    public class ServerReportOutcome
    {
        public string data;
        public string modo;
    }

    /// <summary>Risposta delle funzioni CloudScript (Server/CloudScript/51.js): i campi che non servono restano a zero.</summary>
    [Serializable]
    public class ServerReward
    {
        public bool ok;
        public int giorno;
        public bool riscattato;
        public int secondi;
        public int monete, gemme;
        public ServerChest[] forzieri = new ServerChest[0];
        public string[] riscattati = new string[0];
        public bool tetto;
        public long ora; // "moderazione": orologio del server in millisecondi (sanzioni degli ospiti)
        public bool limiteAbbandoni; // vittoria per abbandono senza monete (tetti contro gli abbandoni concordati)
        public ServerSuspension sospensione, silenzio; // silenzio = emoticon spente
        public ServerReportOutcome[] esiti;
        // premioPartita: XP dati e statistiche del profilo dopo la partita (le scrive solo il server).
        public int xp;
        public bool statistiche;
        public int partite, vittorie, esperienza, scopeTotali, livello;
    }

    /// <summary>
    /// Giro del server (scelte dell'utente, 01/10): Posta, premi giornalieri e monete di fine partita li da' il CloudScript, mai il
    /// telefono. Start() una volta per sessione: consegna la "Posta per tutti" e legge lo stato dei premi. Se il CloudScript non e'
    /// ancora caricato su PlayFab le chiamate falliscono e le pagine lo dicono ("Non disponibile").
    /// </summary>
    public static class RewardsService
    {
        /// <summary>Stato dei premi giornalieri dall'ultimo Start/stato/riscatto, null finche' non arriva.</summary>
        public static ServerReward Daily { get; private set; }
        /// <summary>Quando e' arrivato Daily (Time.realtimeSinceStartup): Daily.secondi conta da li'.</summary>
        public static float DailyAt { get; private set; }

        public static event Action DailyChanged;

        private static bool started, starting;
        private static readonly List<Action> waiting = new List<Action>();

        /// <summary>Una volta per sessione; onDone parte comunque (anche se il server non risponde), dopo la Posta per tutti.</summary>
        public static void Start(Action onDone)
        {
            if (started) { onDone?.Invoke(); return; }
            if (onDone != null) waiting.Add(onDone);
            if (starting) return;
            starting = true;
            Call("inizio", null, r =>
            {
                started = true;
                SetDaily(r);
                Flush();
            }, _ => Flush());
        }

        private static void Flush()
        {
            starting = false;
            var list = new List<Action>(waiting);
            waiting.Clear();
            foreach (var a in list) a();
        }

        public static void RefreshDaily(Action<ServerReward> onDone, Action<string> onError) =>
            Call("statoPremi", null, r => { SetDaily(r); onDone?.Invoke(r); }, onError);

        public static void ClaimDaily(Action<ServerReward> onDone, Action<string> onError) =>
            Call("riscattaPremio", null, r => { SetDaily(r); onDone?.Invoke(r); }, onError);

        public static void ClaimMail(string id, Action<ServerReward> onDone, Action<string> onError) =>
            Call("riscattaPosta", new Dictionary<string, object> { { "id", id } }, onDone, onError);

        public static void ClaimAllMail(Action<ServerReward> onDone, Action<string> onError) =>
            Call("riscattaTuttaPosta", null, onDone, onError);

        /// <summary>
        /// Fine partita: monete (40/20, meta' coi bot, tetto giornaliero), XP e statistiche del profilo li decide il server dall'esito.
        /// forfeitOpponent non null = vinta per abbandono: il server la premia poche volte al giorno e una per avversario, che ricava
        /// dal record della partita (stanza e attore), mai da un id mandato dal telefono.
        /// </summary>
        /// <param name="room">Vittoria per abbandono: stanza e numero Photon di chi e' uscito, il server ne ricava l'id dal record della partita.</param>
        public static void MatchReward(bool won, bool training, int scope, int accusi, Action<ServerReward> onDone, Action<string> onError,
            string forfeitOpponent = null, string room = null, int actor = 0)
        {
            var args = new Dictionary<string, object> { { "vinta", won }, { "allenamento", training }, { "scope", scope }, { "accusi", accusi } };
            if (forfeitOpponent != null) args["abbandono"] = true;
            if (forfeitOpponent != null && room != null && actor > 0) { args["stanza"] = room; args["attore"] = actor; }
            Call("premioPartita", args, onDone, onError);
        }

        /// <summary>Partita lasciata a meta': per il server e' una partita persa nelle statistiche, senza XP ne' monete.</summary>
        public static void MatchQuit(Action<ServerReward> onDone) =>
            Call("premioPartita", new Dictionary<string, object> { { "uscita", true } }, onDone, null);

        /// <summary>Il nuovo account (o il logout) non deve vedere lo stato del precedente.</summary>
        public static void Reset()
        {
            started = starting = false;
            Daily = null;
            waiting.Clear();
        }

        private static void SetDaily(ServerReward r)
        {
            if (r == null || r.giorno <= 0) return;
            Daily = r;
            DailyAt = Time.realtimeSinceStartup;
            DailyChanged?.Invoke();
        }

        /// <summary>onDone riceve anche le risposte con ok=false (per esempio "gia' riscattato"): le decide chi chiama.</summary>
        internal static void Call(string function, object args, Action<ServerReward> onDone, Action<string> onError)
        {
            if (!PlayFabClientAPI.IsClientLoggedIn()) { onError?.Invoke("Non collegato"); return; }
            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest
            {
                FunctionName = function,
                FunctionParameter = args,
                GeneratePlayStreamEvent = false,
            }, result =>
            {
                if (result.Error != null || result.FunctionResult == null)
                {
                    string why = result.Error != null ? result.Error.Error + ": " + result.Error.Message : "nessuna risposta";
                    if (Debug.isDebugBuild) Debug.LogWarning($"[RewardsService] {function}: {why}");
                    onError?.Invoke(why);
                    return;
                }
                var reward = Parse(PlayFabSimpleJson.SerializeObject(result.FunctionResult));
                if (reward.monete > 0 || reward.gemme > 0) WalletService.Refresh();
                onDone?.Invoke(reward);
            }, error =>
            {
                if (Debug.isDebugBuild) Debug.LogWarning($"[RewardsService] {function}: {error.GenerateErrorReport()}");
                onError?.Invoke(error.ErrorMessage);
            });
        }

        public static ServerReward Parse(string json)
        {
            ServerReward r = null;
            try { r = JsonUtility.FromJson<ServerReward>(json); } catch (ArgumentException) { }
            r = r ?? new ServerReward();
            r.forzieri = r.forzieri ?? new ServerChest[0];
            r.riscattati = r.riscattati ?? new string[0];
            return r;
        }

        /// <summary>Suono di un premio riscattato: monete, piu' le gemme se ce ne sono.</summary>
        public static void PlaySound(ServerReward r)
        {
            Project51.Unity.GameAudio.Play(Project51.Unity.SoundId.RewardCoin);
            if (r.gemme > 0) Project51.Unity.GameAudio.Play(Project51.Unity.SoundId.RewardGem);
        }

        /// <summary>"+250 monete · +5 gemme" (vuota se non e' arrivato niente).</summary>
        public static string Summary(ServerReward r)
        {
            var parts = new List<string>();
            if (r.monete > 0) parts.Add("+" + r.monete + " monete");
            if (r.gemme > 0) parts.Add("+" + r.gemme + " gemme");
            return string.Join(" · ", parts);
        }
    }
}
