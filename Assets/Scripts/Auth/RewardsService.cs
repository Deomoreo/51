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
        public bool altrove; // "moderazione"/"sessione": un altro telefono ha preso l'account dopo che questo era sparito (B14)
        public bool occupato; // "sessione" (nuova): l'account e' in uso su un altro telefono, secondi = quanto manca se quello e' spento
        public bool gia, ospite; // "premioTutorial": gia' riscattato / account ospite (B32)
        public bool limiteAbbandoni; // vittoria per abbandono senza monete (tetti contro gli abbandoni concordati)
        public ServerSuspension sospensione, silenzio; // silenzio = emoticon spente
        public ServerReportOutcome[] esiti;
        // premioPartita: XP dati e statistiche del profilo dopo la partita (le scrive solo il server).
        public int xp;
        public bool statistiche;
        public int partite, vittorie, esperienza, scopeTotali, livello;
        // Giro Android 08/10: saldo dopo il premio, dal server (-1 = valuta non toccata o revisione vecchia dello script).
        public int saldoMonete = -1, saldoGemme = -1;
        public string errore;
        // Terzo giro 08/10 (registro "Consegne" del server): premio consumato ma accredito non ancora confermato da PlayFab (arriva a un
        // tentativo dopo); recupero* = consegne rimaste da prima e arrivate con questa chiamata.
        public bool inConsegna;
        public int recuperoMonete, recuperoGemme;
        // Amici (giro Android 08/10): "amici" -> elenco; "richiestaAmico"/"accettaAmico" -> "inviata" o "amico".
        public ServerFriend[] amici;
        public string stato;
        // Biglietto di partita (#141): "inizioPartita" -> id dal server e se al tavolo c'erano altre persone (attendi = secondi prima di
        // un biglietto nuovo); "premioPartita" -> stato (confermata, abbandono, allenamento, inVerifica, incompleta, contestata, uscita,
        // sospeso, limite, daRiconciliare, senzaBiglietto, nonValida, troppoCorta, attendi), categoria di un'incongruenza, concordata
        // (partita fra sole persone confermata dai telefoni: conta per XPConcordato; non e' una validazione del server).
        public string partita, categoria;
        public int attendi;
        public bool online, partitaNonValida, allenamento, limitePartite, sospeso, concordata;
    }

    /// <summary>
    /// Risultato di una partita come lo dichiara il telefono (#141, Fase A): resta sul telefono finche' il server non da' una risposta
    /// definitiva e si ritenta. partita = biglietto del server, oppure vuoto e locale = id del telefono (biglietto mai arrivato).
    /// punti = totali di ogni concorrente (squadre in 2v2), vincitore = concorrente vincente, posto = indice del giocatore nello stato.
    /// stato = ultima risposta non definitiva del server (inVerifica, incompleta, troppoCorta, attendi).
    /// </summary>
    [Serializable]
    public class PendingMatch
    {
        public string partita, locale, stanza, versione, stato;
        public bool vinta, abbandono, squadre;
        public int scope, accusi, attore, smazzate, vincitore, posto, giocatori, rientri;
        public int[] punti = new int[0];
    }

    [Serializable]
    internal class PendingMatches
    {
        public List<PendingMatch> list = new List<PendingMatch>();
    }

    /// <summary>
    /// Giro del server (scelte dell'utente, 01/10): Posta, premi giornalieri e monete di fine partita li da' il CloudScript, mai il
    /// telefono. Start() una volta per sessione (ingresso in Home): consegna la "Posta per tutti", ritenta i premi rimasti in consegna e
    /// legge lo stato dei premi. Se il CloudScript non e'
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
        // Letture partite prima dell'ultimo riscatto: la loro risposta (stato vecchio) non deve annullarlo.
        private static int claims;
        // B11 (M4): cambi di account (Reset). Una risposta partita per l'account di prima non tocca lo stato di quello nuovo.
        private static int session;
        private static readonly List<Action> waiting = new List<Action>();

        /// <summary>Una volta per sessione; onDone parte comunque (anche se il server non risponde), dopo la Posta per tutti.</summary>
        public static void Start(Action onDone)
        {
            if (started) { onDone?.Invoke(); return; }
            if (onDone != null) waiting.Add(onDone);
            if (starting) return;
            starting = true;
            int at = claims, s = session;
            Call("inizio", null, r =>
            {
                if (s != session) return;
                started = true;
                if (r != null && at != claims) r.giorno = 0; // SetDaily la ignora
                SetDaily(r);
                Flush();
            }, _ => { if (s == session) Flush(); });
        }

        private static void Flush()
        {
            starting = false;
            var list = new List<Action>(waiting);
            waiting.Clear();
            foreach (var a in list) a();
        }

        public static void RefreshDaily(Action<ServerReward> onDone, Action<string> onError)
        {
            int at = claims, s = session;
            Call("statoPremi", null, r => { if (at == claims && s == session) SetDaily(r); onDone?.Invoke(r); }, onError);
        }

        public static void ClaimDaily(Action<ServerReward> onDone, Action<string> onError)
        {
            claims++;
            int s = session;
            Call("riscattaPremio", null, r => { if (s == session) SetDaily(r); onDone?.Invoke(r); }, onError);
        }

        public static void ClaimMail(string id, Action<ServerReward> onDone, Action<string> onError) =>
            Call("riscattaPosta", new Dictionary<string, object> { { "id", id } }, onDone, onError);

        public static void ClaimAllMail(Action<ServerReward> onDone, Action<string> onError) =>
            Call("riscattaTuttaPosta", null, onDone, onError);

        /// <summary>
        /// Biglietto della partita (#141): l'id lo genera il server, mai il telefono. room = stanza Photon (null in allenamento); local =
        /// id della partita sul telefono (allenamento: un biglietto per partita). Il server non restituisce mai i biglietti delle partite
        /// gia' finite su questo telefono ("dopo": in sospeso e ultime finite, anche dopo un riavvio). Rientro: stesso biglietto.
        /// </summary>
        public static void OpenMatch(string room, string local, Action<ServerReward> onDone, Action<string> onError)
        {
            var args = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(room)) args["stanza"] = room;
            if (!string.IsNullOrEmpty(local)) args["locale"] = local;
            var done = FinishedIds();
            if (done.Count > 0) args["dopo"] = done;
            Call("inizioPartita", args, onDone, onError);
        }

        /// <summary>
        /// Fine partita: monete, XP e statistiche li decide il server dal biglietto e dalle dichiarazioni di tutte le persone della
        /// partita (#142 Fase A). Il risultato resta sul telefono finche' il server non da' una risposta definitiva (anche dopo un riavvio,
        /// RetryMatches); lo stesso biglietto paga una volta sola. Senza biglietto (rete) si manda l'id del telefono: meta' premio.
        /// </summary>
        public static void MatchReward(PendingMatch m, Action<ServerReward> onDone, Action<string> onError)
        {
            m.versione = Application.version;
            if (!string.IsNullOrEmpty(m.partita)) AddFinished(m.partita);
            SavePending(m);
            Claim(m, onDone, onError);
        }

        /// <summary>Ritenta subito un risultato in sospeso (schermata dei risultati, "in verifica").</summary>
        public static void RetryMatch(string partita, string local, Action<ServerReward> onDone)
        {
            string key = PendingKey();
            if (key == null) return;
            var m = LoadPending(key).list.Find(x => Same(x, partita, local));
            if (m != null) Claim(m, onDone, null);
        }

        /// <summary>Risposta che lascia il risultato sul telefono: il server lo vuole ancora (verifica, riesame, troppo presto, occupato).</summary>
        public static bool KeepsPending(ServerReward r) =>
            r.occupato || r.stato == "inVerifica" || r.stato == "incompleta" || r.stato == "troppoCorta" || r.stato == "attendi";

        private static void Claim(PendingMatch m, Action<ServerReward> onDone, Action<string> onError)
        {
            string key = PendingKey();
            var args = new Dictionary<string, object>
            {
                { "vinta", m.vinta }, { "scope", m.scope }, { "accusi", m.accusi }, { "punti", m.punti ?? new int[0] }, { "smazzate", m.smazzate },
                { "vincitore", m.vincitore }, { "posto", m.posto }, { "squadre", m.squadre }, { "giocatori", m.giocatori },
                { "rientri", m.rientri }, { "versione", m.versione ?? string.Empty }
            };
            if (!string.IsNullOrEmpty(m.partita)) args["partita"] = m.partita;
            else { args["senzaBiglietto"] = m.locale; if (!string.IsNullOrEmpty(m.stanza)) args["stanza"] = m.stanza; }
            if (m.abbandono) { args["abbandono"] = true; args["attore"] = m.attore; }
            Call("premioPartita", args, r =>
            {
                if (key == PendingKey())
                {
                    if (KeepsPending(r)) { m.stato = r.stato; UpdatePending(m); }
                    else RemovePending(m);
                }
                onDone?.Invoke(r);
            }, onError);
        }

        // Risultati in sospeso per account: "Premi.PartiteInSospeso.<PlayFabId>" = PendingMatches (JSON), al massimo MaxPending (D1).
        // Ultimi biglietti finiti su questo telefono: "Premi.PartiteFinite.<PlayFabId>" (mai restituiti per una partita nuova).
        const string PendingPrefix = "Premi.PartiteInSospeso.", FinishedPrefix = "Premi.PartiteFinite.";
        public const int MaxPending = 10;
        const int MaxFinished = 10;

        private static string AccountId()
        {
            var auth = AuthBootstrapper.Instance != null ? AuthBootstrapper.Instance.PlayFabAuth : null;
            return auth != null && auth.HasRealLogin && !string.IsNullOrEmpty(auth.PlayFabId) ? auth.PlayFabId : null;
        }

        private static string PendingKey() { string id = AccountId(); return id != null ? PendingPrefix + id : null; }

        private static PendingMatches LoadPending(string key)
        {
            PendingMatches p = null;
            try { p = JsonUtility.FromJson<PendingMatches>(PlayerPrefs.GetString(key, string.Empty)); } catch (ArgumentException) { }
            return p ?? new PendingMatches();
        }

        private static void StorePending(string key, PendingMatches p)
        {
            PlayerPrefs.SetString(key, JsonUtility.ToJson(p));
            PlayerPrefs.Save();
        }

        private static bool Same(PendingMatch x, string partita, string local) =>
            !string.IsNullOrEmpty(partita) ? x.partita == partita : string.IsNullOrEmpty(x.partita) && x.locale == local;

        /// <summary>
        /// Aggiunge un risultato. Al limite (D1) niente sparisce in silenzio: prima esce il piu' vecchio "incompleta" (il server ha gia'
        /// la sua dichiarazione e la partecipazione: resta da riconciliare li'), poi il piu' vecchio con biglietto (il server ha il
        /// biglietto: resta registrato), con l'avviso; se sono tutti senza biglietto il nuovo non si salva e lo si dice.
        /// </summary>
        private static void SavePending(PendingMatch m)
        {
            string key = PendingKey();
            if (key == null) return;
            var p = LoadPending(key);
            p.list.RemoveAll(x => Same(x, m.partita, m.locale));
            if (p.list.Count >= MaxPending)
            {
                int drop = p.list.FindIndex(x => x.stato == "incompleta");
                if (drop < 0) drop = p.list.FindIndex(x => !string.IsNullOrEmpty(x.partita));
                if (drop < 0)
                {
                    Debug.LogWarning("[RewardsService] risultati in sospeso al limite: risultato senza biglietto non salvato");
                    Project51.Unity.UI.UI51Toast.Show("Risultato non salvato: " + MaxPending + " partite aspettano la rete", Project51.Unity.UI.UI51Toast.Kind.Error);
                    return;
                }
                var old = p.list[drop];
                p.list.RemoveAt(drop);
                Debug.LogWarning("[RewardsService] risultati in sospeso al limite: " + old.partita + " (" + old.stato + ") resta al server");
                if (old.stato != "incompleta")
                    Project51.Unity.UI.UI51Toast.Show("Una partita vecchia resta da verificare sul server", Project51.Unity.UI.UI51Toast.Kind.Error);
            }
            p.list.Add(m);
            StorePending(key, p);
        }

        private static void UpdatePending(PendingMatch m)
        {
            string key = PendingKey();
            if (key == null) return;
            var p = LoadPending(key);
            int i = p.list.FindIndex(x => Same(x, m.partita, m.locale));
            if (i < 0) return;
            p.list[i] = m;
            StorePending(key, p);
        }

        private static void RemovePending(PendingMatch m)
        {
            string key = PendingKey();
            if (key == null) return;
            var p = LoadPending(key);
            if (p.list.RemoveAll(x => Same(x, m.partita, m.locale)) > 0) StorePending(key, p);
        }

        /// <summary>Biglietti delle partite finite su questo telefono: in sospeso e ultime MaxFinished.</summary>
        private static List<string> FinishedIds()
        {
            var ids = new List<string>();
            string id = AccountId();
            if (id == null) return ids;
            foreach (var x in PlayerPrefs.GetString(FinishedPrefix + id, string.Empty).Split(','))
                if (x.Length > 0 && !ids.Contains(x)) ids.Add(x);
            foreach (var m in LoadPending(PendingPrefix + id).list)
                if (!string.IsNullOrEmpty(m.partita) && !ids.Contains(m.partita)) ids.Add(m.partita);
            return ids;
        }

        private static void AddFinished(string partita)
        {
            string id = AccountId();
            if (id == null || string.IsNullOrEmpty(partita)) return;
            var list = new List<string>(PlayerPrefs.GetString(FinishedPrefix + id, string.Empty).Split(','));
            list.RemoveAll(x => x.Length == 0 || x == partita);
            list.Add(partita);
            if (list.Count > MaxFinished) list.RemoveRange(0, list.Count - MaxFinished);
            PlayerPrefs.SetString(FinishedPrefix + id, string.Join(",", list));
            PlayerPrefs.Save();
        }

        /// <summary>All'arrivo in Home: i risultati di partita rimasti senza risposta definitiva, con l'avviso se arrivano monete.</summary>
        public static void RetryMatches()
        {
            string key = PendingKey();
            if (key == null) return;
            foreach (var m in LoadPending(key).list.ToArray())
            {
                bool wasIncomplete = m.stato == "incompleta";
                Claim(m, r =>
                {
                    if (r.monete <= 0 || !(r.ok || r.gia)) return;
                    PlaySound(r);
                    string what = wasIncomplete && r.stato == "confermata" ? "risultato confermato" : "partita precedente";
                    Project51.Unity.UI.UI51Toast.Show("+" + r.monete + " monete: " + what, Project51.Unity.UI.UI51Toast.Kind.Coins);
                }, null);
            }
        }

        // Giro Android 08/10: premio del tutorial chiesto ma senza risposta buona (rete, errore del server): si riprova in Home.
        const string TutorialPendingKey = "Premi.TutorialDaRiscattare.";

        private static string TutorialKey()
        {
            var auth = AuthBootstrapper.Instance != null ? AuthBootstrapper.Instance.PlayFabAuth : null;
            return auth != null && auth.HasRealLogin && !string.IsNullOrEmpty(auth.PlayFabId) ? TutorialPendingKey + auth.PlayFabId : null;
        }

        /// <summary>
        /// B32 (TU3): fine del tutorial, +200 monete una volta per account (ok=false con gia/ospite altrimenti). Finche' il server non
        /// risponde ok, gia', ospite o inConsegna (registrato, l'accredito lo ritenta il server) resta in sospeso per l'account
        /// (RetryTutorial): il server paga comunque una volta sola.
        /// </summary>
        public static void ClaimTutorial(Action<ServerReward> onDone, Action<string> onError)
        {
            string key = TutorialKey();
            if (key != null) { PlayerPrefs.SetInt(key, 1); PlayerPrefs.Save(); }
            Call("premioTutorial", null, r =>
            {
                if (key != null && (r.ok || r.gia || r.ospite || r.inConsegna)) { PlayerPrefs.DeleteKey(key); PlayerPrefs.Save(); }
                onDone?.Invoke(r);
            }, onError);
        }

        /// <summary>All'arrivo in Home: il premio del tutorial rimasto in sospeso per questo account, con l'avviso se arriva.</summary>
        public static void RetryTutorial()
        {
            string key = TutorialKey();
            if (key == null || PlayerPrefs.GetInt(key, 0) == 0) return;
            ClaimTutorial(r =>
            {
                if (!r.ok) return;
                PlaySound(r);
                Project51.Unity.UI.UI51Toast.Show("+" + r.monete + " monete: premio del tutorial", Project51.Unity.UI.UI51Toast.Kind.Coins);
            }, null);
        }

        /// <summary>Partita lasciata a meta': per il server e' una partita persa nelle statistiche, senza XP ne' monete.</summary>
        public static void MatchQuit(string match, Action<ServerReward> onDone)
        {
            var args = new Dictionary<string, object> { { "uscita", true } };
            if (!string.IsNullOrEmpty(match)) { args["partita"] = match; AddFinished(match); } // chiude il biglietto: non si paga piu'
            Call("premioPartita", args, onDone, null);
        }

        /// <summary>Il nuovo account (o il logout) non deve vedere lo stato del precedente.</summary>
        public static void Reset()
        {
            session++;
            started = starting = false;
            Daily = null;
            waiting.Clear();
            DailyChanged?.Invoke(); // via il pallino dei Premi dell'account di prima
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
                    // Giro Android 08/10: anche nelle build non di sviluppo (logcat), coi dettagli dello script (quale API e' fallita).
                    Debug.LogWarning($"[RewardsService] {function}: {why}\n{result.Error?.StackTrace}\n{Logs(result)}");
                    onError?.Invoke(why);
                    return;
                }
                var reward = Parse(PlayFabSimpleJson.SerializeObject(result.FunctionResult));
                if (!string.IsNullOrEmpty(reward.errore)) Debug.LogWarning($"[RewardsService] {function}: {reward.errore}");
                // Saldo nuovo nella risposta: la TopBar cambia subito, senza aspettare una lettura del portafoglio.
                if (reward.saldoMonete >= 0 || reward.saldoGemme >= 0) WalletService.Apply(reward.saldoMonete, reward.saldoGemme);
                else if (reward.monete > 0 || reward.gemme > 0) WalletService.Refresh();
                // Un premio rimasto in consegna e' arrivato ora (qualunque chiamata dei premi lo ritenta): lo si dice una volta.
                string recovered = Summary(new ServerReward { monete = reward.recuperoMonete, gemme = reward.recuperoGemme });
                if (recovered.Length > 0) Project51.Unity.UI.UI51Toast.Show(recovered + ": premio in sospeso arrivato", Project51.Unity.UI.UI51Toast.Kind.Coins);
                onDone?.Invoke(reward);
            }, error =>
            {
                if (Debug.isDebugBuild) Debug.LogWarning($"[RewardsService] {function}: {error.GenerateErrorReport()}");
                onError?.Invoke(error.ErrorMessage);
            });
        }

        private static string Logs(ExecuteCloudScriptResult result)
        {
            if (result.Logs == null) return "";
            var lines = new List<string>();
            foreach (var l in result.Logs) lines.Add(l.Level + ": " + l.Message + (l.Data != null ? " " + PlayFabSimpleJson.SerializeObject(l.Data) : ""));
            return string.Join("\n", lines);
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
