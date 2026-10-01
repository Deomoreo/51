using System;

namespace Project51.Core
{
    /// <summary>
    /// Curva XP unica (E1), condivisa da progresso locale, profilo cloud e Home:
    /// per passare dal livello L al livello L+1 servono 100 + 20*(L-1) XP.
    /// </summary>
    public static class PlayerXp
    {
        public const int MaxLevel = 100;

        public static int XpToNext(int level) => 100 + 20 * (Math.Max(1, level) - 1);

        /// <summary>XP totali necessari per raggiungere il livello indicato (livello 1 = 0).</summary>
        public static int TotalForLevel(int level)
        {
            int l = Math.Max(1, Math.Min(level, MaxLevel)) - 1;
            return 100 * l + 10 * l * (l - 1);
        }

        public static int LevelOf(int totalXp)
        {
            int level = 1;
            while (level < MaxLevel && totalXp >= TotalForLevel(level + 1)) level++;
            return level;
        }

        /// <summary>XP gia' guadagnati dentro il livello corrente.</summary>
        public static int XpInLevel(int totalXp) => Math.Max(0, totalXp - TotalForLevel(LevelOf(totalXp)));

        /// <summary>
        /// XP di fine partita: 40 vittoria / 20 sconfitta, +2 per scopa e +5 per accuso (bonus max +20),
        /// meta' in allenamento.
        /// </summary>
        public static int MatchAward(bool won, int scope, int accusi, bool training)
        {
            int xp = (won ? 40 : 20) + Math.Min(20, 2 * Math.Max(0, scope) + 5 * Math.Max(0, accusi));
            return training ? xp / 2 : xp;
        }

        /// <summary>Titolo del profilo rapido (scelta utente 01/10): 1-4, 5-9, 10-14, 15-24, 25 e oltre.</summary>
        public static string Title(int level) =>
            level >= 25 ? "Gran Maestro" : level >= 15 ? "Maestro" : level >= 10 ? "Esperto" : level >= 5 ? "Apprendista" : "Principiante";

        /// <summary>
        /// Medaglie del profilo rapido (scelta utente 01/10), un bit per icona: 1 trofeo 10 vittorie, 2 sole 100 partite,
        /// 4 bastoni 100 scope, 8 spade livello 10. Calcolate dalle statistiche pubblicate, quindi uguali per tutti.
        /// </summary>
        public static int Medals(int games, int wins, int scope, int level) =>
            (wins >= 10 ? 1 : 0) | (games >= 100 ? 2 : 0) | (scope >= 100 ? 4 : 0) | (level >= 10 ? 8 : 0);
    }
}
