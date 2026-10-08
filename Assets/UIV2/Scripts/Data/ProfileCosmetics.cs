using System;
using Project51.UI51;

namespace Project51.UIV2.Data
{
    /// <summary>
    /// Cornici e banner del profilo (SPEC §7, mockup Profilo): id salvati nel profilo, nomi e sblocchi.
    /// I banner seguono l'ordine di BannerStyle.
    /// </summary>
    public static class ProfileCosmetics
    {
        public const int PorporaLevel = 10;

        public static readonly string[] FrameIds = { "classica", "oro", "smeraldo", "notte" };
        public static readonly string[] FrameNames = { "Classica", "Oro", "Smeraldo", "Notte" };
        public static readonly string[] BannerIds = { "notte", "smeraldo", "porpora", "aurora", "stellato" };
        // "Stellato" e non "Notte stellata" (mockup): con l'etichetta accanto non entra nei 162 px della cella.
        public static readonly string[] BannerNames = { "Notte", "Smeraldo", "Porpora", "Aurora", "Stellato" };
        public static readonly string[] BannerTags =
            { "STANDARD", "STANDARD", "LIVELLO 10", "ANIMATO · PASS", "ANIMATO · NEGOZIO" };

        /// <summary>Id vuoto o sconosciuto = Oro, la cornice iniziale del profilo.</summary>
        public static int FrameIndex(string id)
        {
            int i = Array.IndexOf(FrameIds, id);
            return i < 0 ? 1 : i;
        }

        /// <summary>Id vuoto o sconosciuto = Notte, il banner iniziale del profilo.</summary>
        public static int BannerIndex(string id) => Math.Max(0, Array.IndexOf(BannerIds, id));

        public static BannerStyle Banner(int index) => (BannerStyle)index;

        /// <summary>
        /// Aspetto di un altro giocatore dalle sue proprieta' Photon (AuthBootstrapper.PublishLook): le scrive un altro
        /// client, quindi solo i tipi attesi e valori nei limiti (id sconosciuto = Oro / Notte, livello 1..MaxLevel), mai
        /// un'eccezione dentro il Refresh. Dato solo da mostrare: mai per premi o matchmaking.
        /// False se non ne ha pubblicato uno (bot, ospite, versione senza aspetto in rete).
        /// </summary>
        public static bool ReadLook(System.Collections.IDictionary props, out int frame, out int style, out int level)
        {
            var id = props?[Project51.Auth.ProfileService.LookFrameKey] as string;
            frame = FrameIndex(id);
            style = BannerIndex(props?[Project51.Auth.ProfileService.LookBannerKey] as string);
            level = props?[Project51.Auth.ProfileService.LookLevelKey] is int l ? Math.Max(1, Math.Min(l, Project51.Core.PlayerXp.MaxLevel)) : 1;
            return id != null;
        }

        /// <summary>
        /// Statistiche del profilo rapido dalle proprieta' Photon di un altro giocatore (stesse regole di ReadLook: solo int,
        /// mai negativi, vittorie al massimo quante le partite). False se non le ha pubblicate (bot, ospite, versione vecchia).
        /// playFabId null = niente pulsanti Aggiungi amico / Silenzia / Segnala.
        /// </summary>
        public static bool ReadStats(System.Collections.IDictionary props, out int games, out int wins, out int scope, out string playFabId)
        {
            bool has = props?[Project51.Auth.ProfileService.LookGamesKey] is int;
            games = has ? Math.Max(0, (int)props[Project51.Auth.ProfileService.LookGamesKey]) : 0;
            wins = props?[Project51.Auth.ProfileService.LookWinsKey] is int w ? Math.Max(0, Math.Min(w, games)) : 0;
            scope = props?[Project51.Auth.ProfileService.LookScopeKey] is int s ? Math.Max(0, s) : 0;
            playFabId = props?[Project51.Auth.ProfileService.LookIdKey] as string;
            if (playFabId != null && (playFabId.Length == 0 || playFabId.Length > 32)) playFabId = null;
            return has;
        }

        /// <summary>Avatar pubblicato da un account (AuthBootstrapper.LookProps); null per bot, ospiti, versioni vecchie e valori strani.</summary>
        public static string ReadAvatar(System.Collections.IDictionary props)
        {
            string id = props?[Project51.Auth.ProfileService.LookAvatarKey] as string;
            return id != null && id.Length > 0 && id.Length <= 32 ? id : null;
        }

        /// <summary>Sprite di un avatar per id (nome dello sprite); id sconosciuto ("default", mai scelto) = il primo, come la Home.</summary>
        public static UnityEngine.Sprite AvatarFor(UnityEngine.Sprite[] avatars, string id)
        {
            if (avatars == null || avatars.Length == 0) return null;
            foreach (var a in avatars) if (a != null && a.name == id) return a;
            return avatars[0];
        }

        /// <summary>PlayFab ID della sessione di un ospite (AuthBootstrapper.LookProps), null per gli account e i valori strani.</summary>
        public static string GuestId(System.Collections.IDictionary props)
        {
            string id = props?[Project51.Auth.ProfileService.LookGuestIdKey] as string;
            return id != null && id.Length > 0 && id.Length <= 32 ? id : null;
        }

        /// <summary>Vittorie in percentuale intera ("58%"); senza partite "-" (il trattino lungo puo' mancare nell'atlante del font).</summary>
        public static string WinRate(int wins, int games) =>
            games <= 0 ? "-" : (int)Math.Round(100.0 * Math.Max(0, Math.Min(wins, games)) / games) + "%";

        // ponytail: Aurora (pass) e Stellato (negozio) restano chiusi finche' pass e negozio non esistono;
        // quando arrivano, qui va chiesto a loro.
        public static bool BannerUnlocked(int index, int level) => index < 2 || (index == 2 && level >= PorporaLevel);

        /// <summary>Classica = anello oro pieno e sottile (thin), le altre = cornice a gradiente (thick).</summary>
        public static void ApplyFrame(AvatarFrame avatar, int index, float thin, float thick)
        {
            if (avatar == null) return;
            avatar.ringWidth = index == 0 ? thin : thick;
            switch (index)
            {
                case 0: avatar.SetRing(UI51Tokens.Gold); break;
                case 2: avatar.SetFrame(FrameStyle.Smeraldo); break;
                case 3: avatar.SetFrame(FrameStyle.Blu); break;
                default: avatar.SetFrame(FrameStyle.Oro); break;
            }
        }
    }
}
