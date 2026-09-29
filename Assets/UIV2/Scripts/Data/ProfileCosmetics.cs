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
