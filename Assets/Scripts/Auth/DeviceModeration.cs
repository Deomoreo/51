using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project51.Auth
{
    /// <summary>
    /// Sanzioni degli ospiti (scelta dell'utente 01/10: "anche gli ospiti, nei limiti tecnici"). L'ospite cambia account PlayFab a ogni
    /// avvio, quindi abbandoni e sanzioni restano su questo dispositivo, con le stesse regole del server (MOD_DEFAULTS di 51.js):
    /// 5 abbandoni in 7 giorni; sospensione 24 ore, 3 giorni, 3 giorni, poi 7; emoticon spente 24 ore, 3 giorni, poi 7; dopo 30 giorni
    /// puliti si riparte. Limite: cancellando i dati dell'app o spostando l'ora del telefono si azzerano.
    /// </summary>
    [Serializable]
    public sealed class DeviceModeration
    {
        public const string Key = "Moderazione.Dispositivo";
        public static readonly int[] SuspensionHours = { 24, 72, 72, 168 }, MuteHours = { 24, 72, 168 };
        const int Abandons = 5;
        const long Week = 7 * 86400L, Clean = 30 * 86400L;

        // Tempi in secondi unix.
        public List<long> abbandoni = new List<long>();
        public int volte, silenzioVolte;
        public long fine, silenzioFine;
        public string motivo = string.Empty;
        // "fine" delle ultime sanzioni del server gia' portate qui (segnalazioni prese in una sessione d'ospite): una volta sola.
        public string serverFine = string.Empty, serverSilenzioFine = string.Empty;

        /// <summary>Ora del server quando nota (SyncClock a ogni arrivo in Home), altrimenti quella del telefono.</summary>
        public static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds() + clockOffset;
        private static long clockOffset;

        /// <summary>Spostare l'ora del telefono non accorcia le sanzioni: da qui si conta col server (ms da "moderazione").</summary>
        public static void SyncClock(long serverMs)
        {
            if (serverMs > 0) clockOffset = serverMs / 1000 - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public static DeviceModeration Load()
        {
            try
            {
                var r = JsonUtility.FromJson<DeviceModeration>(PlayerPrefs.GetString(Key, string.Empty));
                if (r != null) return r;
            }
            catch (ArgumentException) { }
            return new DeviceModeration();
        }

        public void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
        }

        /// <summary>Un abbandono: al quinto nei 7 giorni, se non e' gia' sospeso, parte la sospensione.</summary>
        public void AddAbandon(long now)
        {
            abbandoni.RemoveAll(t => now - t >= Week);
            abbandoni.Add(now);
            if (abbandoni.Count < Abandons || fine > now) return;
            volte = Next(volte, fine, now);
            fine = now + Hours(SuspensionHours, volte) * 3600L;
            motivo = "abbandoni";
            abbandoni.Clear();
        }

        /// <summary>Sanzioni del server sull'account di questa sessione: diventano del dispositivo, con le sue volte.</summary>
        public void Mirror(ServerSuspension suspension, ServerSuspension mute, long now)
        {
            if (Active(suspension) && suspension.fine != serverFine && fine <= now)
            {
                serverFine = suspension.fine;
                volte = Next(volte, fine, now);
                fine = now + Hours(SuspensionHours, volte) * 3600L;
                motivo = suspension.motivo ?? string.Empty;
            }
            if (Active(mute) && mute.fine != serverSilenzioFine && silenzioFine <= now)
            {
                serverSilenzioFine = mute.fine;
                silenzioVolte = Next(silenzioVolte, silenzioFine, now);
                silenzioFine = now + Hours(MuteHours, silenzioVolte) * 3600L;
            }
        }

        public ServerSuspension Suspension(long now) => fine > now
            ? new ServerSuspension { secondi = (int)(fine - now), motivo = motivo, volte = volte, fine = fine.ToString() } : null;

        public ServerSuspension Mute(long now) => silenzioFine > now
            ? new ServerSuspension { secondi = (int)(silenzioFine - now), motivo = "emoticon", volte = silenzioVolte, fine = silenzioFine.ToString() } : null;

        private static bool Active(ServerSuspension s) => s != null && s.secondi > 0;
        private static int Next(int times, long lastEnd, long now) => (lastEnd > 0 && now - lastEnd > Clean ? 0 : times) + 1;
        private static int Hours(int[] list, int times) => list[Math.Min(times, list.Length) - 1];
    }
}
