using System;
using System.Collections.Generic;
using Photon.Pun;
using Project51.UIV2.Core;
using Project51.Unity;
using UnityEngine;

namespace Project51.Auth
{
    /// <summary>
    /// Moderazione (scelte dell'utente, 01/10): segnalazioni e abbandoni degli account li conta il server (CloudScript "segnala",
    /// "abbandono", "moderazione"); qui lo stato della sospensione, che blocca il gioco online (contro i bot si gioca sempre),
    /// delle emoticon spente e gli esiti delle segnalazioni fatte. Abbandono = partita online con almeno un'altra persona nella stanza,
    /// lasciata a meta': con ESCI (contato subito) o chiudendo l'app durante la partita (contato al prossimo arrivo in Home).
    /// Le partite contro i soli bot non contano mai. Gli ospiti cambiano account a ogni avvio: per loro conta il dispositivo
    /// (DeviceModeration).
    /// </summary>
    public static class ModerationService
    {
        public const string ReasonEmoticon = "emoticon", ReasonGame = "gioco", ReasonName = "nome";

        const string InProgressKey = "Moderazione.PartitaInCorso"; // "proprietario|esecuzione|1 vs 1|stanza" dalla prima mossa alla fine
        const string ContactKey = "Moderazione.UltimaPausa";       // ticks UTC dell'ultima pausa durante la partita (posto tenuto 60 s)
        /// <summary>Quanto Photon tiene il posto di chi esce senza volerlo (PlayerTtl in MatchmakingManager).</summary>
        public const int SeatSeconds = Project51.Networking.MatchmakingManager.RejoinWindowMilliseconds / 1000;
        const string SeenKey = "Moderazione.Vista.";              // + proprietario: la "fine" dell'ultima sospensione mostrata
        const string GuestOwner = "ospite";                         // per gli ospiti il proprietario e' il dispositivo

        /// <summary>Questa esecuzione dell'app: un segno lasciato da un'altra vuol dire app chiusa durante la partita.</summary>
        private static readonly string Run = Guid.NewGuid().ToString("N");

        private static ServerSuspension suspension, mute;
        private static float suspensionAt, muteAt;
        private static bool abandonSending;
        private static Action afterAbandon, afterRejoin;

        /// <summary>App riaperta e rientro al tavolo in corso (HomeConnectionWatcher): la Home aspetta prima di contare l'abbandono.</summary>
        public static bool RejoinPending { get; private set; }
        /// <summary>Rientrati nella stanza dopo il riavvio, in attesa dello stato della partita (NetworkGameController).</summary>
        public static bool RejoinedAfterRestart { get; private set; }

        public static bool IsSuspended => SecondsLeft > 0;
        public static int SecondsLeft => Left(suspension, suspensionAt);
        /// <summary>Emoticon spente per le segnalazioni: non si possono mandare nelle partite online.</summary>
        public static bool IsMuted => MuteSecondsLeft > 0;
        public static int MuteSecondsLeft => Left(mute, muteAt);
        /// <summary>"segnalazioni", "abbandoni" o un testo scritto a mano in Game Manager.</summary>
        public static string Reason => suspension != null ? suspension.motivo ?? string.Empty : string.Empty;
        public static int Times => suspension != null ? suspension.volte : 0;
        /// <summary>Uscire adesso conterebbe come abbandono (per l'avviso della finestra Abbandona).</summary>
        public static bool AbandonCounts => CountedMode() != null;

        private static PlayFabAuthService Auth => AuthBootstrapper.Instance != null ? AuthBootstrapper.Instance.PlayFabAuth : null;
        private static bool Playing => Auth != null && Auth.IsLoggedIn;
        private static bool Registered => Playing && Auth.HasRealLogin;
        private static bool IsGuest => Playing && !Auth.HasRealLogin;
        private static string Owner => Registered ? Auth.PlayFabId : GuestOwner;

        /// <summary>
        /// A ogni arrivo in Home (anche tornando dal tavolo). Una partita di questo proprietario rimasta "in corso" da un'altra esecuzione
        /// vuol dire app chiusa mentre si giocava: conta. Della stessa esecuzione (connessione persa) no. Il segno di un altro account
        /// resta finche' quell'account non torna. onDone(esiti da mostrare o null, true se la sospensione va mostrata ora: una volta sola).
        /// </summary>
        public static void Refresh(Action<ServerReportOutcome[], bool> onDone)
        {
            if (!Playing) { Reset(); return; }
            if (RejoinPending) { afterRejoin = () => Refresh(onDone); return; }
            string[] pending = Marker();
            // Rientro dopo il riavvio gia' riuscito (si e' al tavolo): il segno resta per AdoptRejoinedMatch. Se poi il rientro salta,
            // al prossimo arrivo in Home non si e' piu' in stanza e conta come prima.
            bool mine = pending.Length >= 3 && pending[0] == Owner && !(RejoinedAfterRestart && PhotonNetwork.InRoom);
            if (mine) ClearMarker();
            bool closedMidMatch = mine && pending[1] != Run;

            if (IsGuest)
            {
                // Prima l'ora del server, poi si conta (l'ora del telefono si puo' spostare); senza rete vale quella del telefono.
                RewardsService.Call("moderazione", null, r =>
                {
                    DeviceModeration.SyncClock(r.ora);
                    GuestRefresh(closedMidMatch, r);
                    Done(onDone, null);
                }, _ => { GuestRefresh(closedMidMatch, null); Done(onDone, null); });
                return;
            }

            if (closedMidMatch)
                RewardsService.Call("abbandono", ModeArgs(pending[2]), _ => Fetch(onDone), _ => Fetch(onDone));
            // ESCI appena premuto: la sospensione che ne esce va letta dopo, se no la Home arriva prima della risposta.
            else if (abandonSending) afterAbandon = () => Fetch(onDone);
            else Fetch(onDone);
        }

        private static float checkedAt = float.MinValue;

        private static void Fetch(Action<ServerReportOutcome[], bool> onDone)
        {
            RewardsService.Call("moderazione", null, r =>
            {
                checkedAt = Time.unscaledTime;
                Apply(r);
                Done(onDone, r.esiti != null && r.esiti.Length > 0 ? r.esiti : null);
            }, null);
        }

        private static void Done(Action<ServerReportOutcome[], bool> onDone, ServerReportOutcome[] outcomes) =>
            onDone?.Invoke(outcomes, IsSuspended && FirstTimeSeen());

        /// <summary>
        /// RIVINCITA (scelta dell'utente 01/10): e' una partita nuova, quindi si guarda lo stato fresco sul server (senza consumare gli
        /// esiti, che restano per la Home). Se la rete non risponde vale l'ultimo stato noto.
        /// </summary>
        public static void CheckBeforeRematch() => CheckNow(null);

        /// <summary>Stato fresco dal server senza consumare gli esiti, anche quando il server rifiuta l'ingresso in una stanza.</summary>
        /// <param name="freshSeconds">Se lo stato e' arrivato dal server da meno di questi secondi si usa quello, senza chiamata.</param>
        public static void CheckNow(Action onDone, float freshSeconds = 0f)
        {
            if (!Playing) { onDone?.Invoke(); return; }
            if (Time.unscaledTime - checkedAt < freshSeconds) { onDone?.Invoke(); return; }
            RewardsService.Call("moderazione", new Dictionary<string, object> { { "stato", true } }, r =>
            {
                checkedAt = Time.unscaledTime;
                if (IsGuest) { DeviceModeration.SyncClock(r.ora); GuestRefresh(false, r); }
                else Apply(r);
                onDone?.Invoke();
            }, _ => onDone?.Invoke());
        }

        // Sanzioni dell'ospite sul dispositivo, piu' quelle prese dalla sua sessione di oggi sul server (portate una volta sola).
        private static void GuestRefresh(bool closedMidMatch, ServerReward r)
        {
            var device = DeviceModeration.Load();
            if (closedMidMatch) device.AddAbandon(DeviceModeration.Now);
            if (r != null) device.Mirror(r.sospensione, r.silenzio, DeviceModeration.Now);
            ApplyDevice(device);
        }

        /// <summary>Prima mossa del giocatore in una partita online (NetworkGameController): la partita e' "in corso".</summary>
        public static void MatchInProgress()
        {
            string current = PlayerPrefs.GetString(InProgressKey, string.Empty);
            string mode = CountedMode();
            // Restano solo bot (l'altra persona e' uscita): da qui chiudere l'app non conta piu'.
            if (mode == null) { MatchEnded(); return; }
            string marker = Owner + "|" + Run + "|" + mode + "|" + (PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.Name : string.Empty);
            if (current == marker) return;
            PlayerPrefs.SetString(InProgressKey, marker);
            PlayerPrefs.Save();
        }

        /// <summary>Partita finita normalmente (MatchResultsV2.RecordMatch). Tocca solo il segno di questa esecuzione.</summary>
        public static void MatchEnded()
        {
            if (PlayerPrefs.GetString(InProgressKey, string.Empty).Contains("|" + Run + "|")) ClearMarker();
        }

        private static void ClearMarker()
        {
            PlayerPrefs.DeleteKey(InProgressKey);
            PlayerPrefs.DeleteKey(ContactKey);
            PlayerPrefs.Save();
        }

        // Vecchio formato a 3 parti (prima della 2.55) compreso.
        private static string[] Marker() => PlayerPrefs.GetString(InProgressKey, string.Empty).Split(new[] { '|' }, 4);

        /// <summary>App in pausa o chiusa durante una partita di questa esecuzione: da qui partono i 60 s del posto tenuto.</summary>
        public static void Paused(bool paused)
        {
            if (!PlayerPrefs.GetString(InProgressKey, string.Empty).Contains("|" + Run + "|")) return;
            if (paused) PlayerPrefs.SetString(ContactKey, DateTime.UtcNow.Ticks.ToString());
            else PlayerPrefs.DeleteKey(ContactKey);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Riaprendo l'app dopo averla chiusa durante una partita online (scelta dell'utente 01/10): la stanza in cui rientrare e i
        /// secondi di posto rimasti. Solo account (l'ospite cambia identita' a ogni avvio, Photon non lo riconosce). Senza pausa
        /// registrata (crash) si prova lo stesso: decide Photon. Null se non c'e' niente da riprendere o il posto e' scaduto.
        /// </summary>
        public static string RejoinRoom(out int seconds) => RejoinRoom(Marker(), Owner, Registered, Run,
            PlayerPrefs.GetString(ContactKey, string.Empty), DateTime.UtcNow.Ticks, out seconds);

        public static string RejoinRoom(string[] marker, string owner, bool registered, string run, string pause, long now, out int seconds)
        {
            seconds = SeatSeconds;
            if (!registered || marker.Length < 4 || marker[0] != owner || marker[1] == run || marker[3].Length == 0) return null;
            if (long.TryParse(pause, out long at)) seconds = SeatSeconds - (int)((now - at) / TimeSpan.TicksPerSecond);
            return seconds > 0 ? marker[3] : null;
        }

        public static void RejoinStarted() => RejoinPending = true;

        /// <summary>Rientro finito: riuscito si va al tavolo (l'abbandono si decide quando arriva lo stato), fallito la Home conta come prima.</summary>
        public static void RejoinFinished(bool joined)
        {
            RejoinPending = false;
            RejoinedAfterRestart = joined;
            var refresh = afterRejoin;
            afterRejoin = null;
            if (!joined) refresh?.Invoke(); // riuscito: la Home si rinfresca al ritorno dal tavolo
        }

        /// <summary>Rientrati ma lo stato della partita non arriva: si torna in Home, dove l'abbandono conta come prima.</summary>
        public static void RejoinGivenUp() => RejoinedAfterRestart = false;

        /// <summary>Stato della partita ricevuto dopo il rientro: la partita torna di questa esecuzione, niente abbandono.</summary>
        public static void AdoptRejoinedMatch()
        {
            RejoinedAfterRestart = false;
            string[] pending = Marker();
            if (pending.Length < 3 || pending[0] != Owner || pending[1] == Run) return;
            pending[1] = Run;
            PlayerPrefs.SetString(InProgressKey, string.Join("|", pending));
            PlayerPrefs.DeleteKey(ContactKey);
            PlayerPrefs.Save();
        }

        /// <summary>ESCI da una partita non finita (MatchResultsV2.RecordAbandon): conta solo online con altre persone.</summary>
        public static void Abandoned()
        {
            string mode = CountedMode();
            MatchEnded();
            if (mode == null) return;
            if (IsGuest)
            {
                var device = DeviceModeration.Load();
                device.AddAbandon(DeviceModeration.Now);
                ApplyDevice(device);
                return;
            }
            abandonSending = true;
            RewardsService.Call("abbandono", ModeArgs(mode), r => { SetSuspension(r.sospensione); AbandonSent(); }, _ => AbandonSent());
        }

        private static void AbandonSent()
        {
            abandonSending = false;
            var next = afterAbandon;
            afterAbandon = null;
            next?.Invoke();
        }

        /// <summary>
        /// "Segnala giocatore" del profilo rapido, col motivo (ReasonEmoticon, ReasonGame, ReasonName). Il server conta account diversi
        /// da partite diverse: la stanza Photon e' la partita.
        /// </summary>
        public static void Report(string playFabId, string reason, Action onDone, Action onError)
        {
            var args = ModeArgs(CurrentMode());
            args["id"] = playFabId;
            args["motivo"] = reason;
            args["stanza"] = PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.Name : string.Empty;
            RewardsService.Call("segnala", args, r => { if (r.ok) onDone?.Invoke(); else onError?.Invoke(); }, _ => onError?.Invoke());
        }

        /// <summary>Cambio d'account o logout: le sanzioni erano del vecchio account.</summary>
        public static void Reset() { suspension = mute = null; checkedAt = float.MinValue; } // stato fresco solo per l'account che l'ha letto

        /// <summary>Formato della partita in corso se l'abbandono conta (online, almeno un'altra persona), altrimenti null.</summary>
        private static string CountedMode()
        {
            if (!Playing || !PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) return null;
            // Chi e' nella finestra di rientro resta nella stanza: conta come persona. I bot non sono giocatori Photon.
            return PhotonNetwork.CurrentRoom.PlayerCount > 1 ? CurrentMode() : null;
        }

        private static string CurrentMode()
        {
            var config = GameSceneInitializer.ActiveConfig;
            return config != null ? RoomFlowV2.FormatName(config.Format) : string.Empty;
        }

        private static Dictionary<string, object> ModeArgs(string mode) => new Dictionary<string, object> { { "modo", mode ?? string.Empty } };

        private static void Apply(ServerReward r)
        {
            SetSuspension(r.sospensione);
            mute = r.silenzio != null && r.silenzio.secondi > 0 ? r.silenzio : null;
            muteAt = Time.realtimeSinceStartup;
        }

        private static void ApplyDevice(DeviceModeration device)
        {
            device.Save();
            long now = DeviceModeration.Now;
            SetSuspension(device.Suspension(now));
            mute = device.Mute(now);
            muteAt = Time.realtimeSinceStartup;
        }

        private static void SetSuspension(ServerSuspension s)
        {
            suspension = s != null && s.secondi > 0 ? s : null;
            suspensionAt = Time.realtimeSinceStartup;
        }

        private static int Left(ServerSuspension s, float at) =>
            s == null ? 0 : Mathf.Max(0, s.secondi - Mathf.FloorToInt(Time.realtimeSinceStartup - at));

        private static bool FirstTimeSeen()
        {
            // Per "fine" e non per "volte": vale anche per una sanzione scritta o allungata a mano in Game Manager.
            string key = SeenKey + Owner, end = suspension.fine ?? string.Empty;
            if (PlayerPrefs.GetString(key, string.Empty) == end) return false;
            PlayerPrefs.SetString(key, end);
            PlayerPrefs.Save();
            return true;
        }
    }
}
