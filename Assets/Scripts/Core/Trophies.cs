using System;

namespace Project51.Core
{
    public enum TrophyStat { Games, Wins, Scope, Level }

    public sealed class Trophy
    {
        public readonly string Name, Description;
        /// <summary>Indice in Trophies.Categories.</summary>
        public readonly int Category;
        /// <summary>Icona: 0 trofeo, 1 sole, 2 bastoni, 3 spade (come i bit di PlayerXp.Medals).</summary>
        public readonly int Medal;
        public readonly TrophyStat Stat;
        public readonly int Target;

        public Trophy(string name, string description, int category, int medal, TrophyStat stat, int target)
        {
            Name = name; Description = description; Category = category; Medal = medal; Stat = stat; Target = target;
        }
    }

    /// <summary>
    /// Trofei (UI51 Fase 15, scelta utente 02/10): elenco fisso calcolato dalle statistiche che scrive solo il server (partite, vittorie,
    /// scope, livello). Niente premi, niente dati salvati, niente date. Ordine = difficolta' crescente, come li mostrano pagina e profilo.
    /// I quattro delle medaglie del profilo rapido (PlayerXp.Medals) sono qui con le stesse soglie.
    /// </summary>
    public static class Trophies
    {
        public static readonly string[] Categories = { "Partite", "Scope", "Livello" };

        public static readonly Trophy[] All =
        {
            new Trophy("Prima partita", "Gioca la tua prima partita", 0, 1, TrophyStat.Games, 1),
            new Trophy("Prima vittoria", "Vinci la tua prima partita", 0, 0, TrophyStat.Wins, 1),
            new Trophy("Prima scopa", "Fai la tua prima scopa", 1, 2, TrophyStat.Scope, 1),
            new Trophy("Apprendista", "Raggiungi il livello 5", 2, 3, TrophyStat.Level, 5),
            new Trophy("Dieci vittorie", "Vinci 10 partite", 0, 0, TrophyStat.Wins, 10),
            new Trophy("Esperto", "Raggiungi il livello 10", 2, 3, TrophyStat.Level, 10),
            new Trophy("Cento partite", "Gioca 100 partite", 0, 1, TrophyStat.Games, 100),
            new Trophy("Cento scope", "Fai 100 scope", 1, 2, TrophyStat.Scope, 100),
            new Trophy("Maestro", "Raggiungi il livello 15", 2, 3, TrophyStat.Level, 15),
            new Trophy("Cinquanta vittorie", "Vinci 50 partite", 0, 0, TrophyStat.Wins, 50),
            new Trophy("Gran Maestro", "Raggiungi il livello 25", 2, 3, TrophyStat.Level, 25),
            new Trophy("Re della scopa", "Fai 500 scope", 1, 2, TrophyStat.Scope, 500),
            new Trophy("Veterano", "Gioca 500 partite", 0, 1, TrophyStat.Games, 500),
            new Trophy("Campione", "Vinci 250 partite", 0, 0, TrophyStat.Wins, 250),
            new Trophy("Mille scope", "Fai 1000 scope", 1, 2, TrophyStat.Scope, 1000),
        };

        /// <summary>Progresso verso il trofeo, al massimo la soglia.</summary>
        public static int Progress(Trophy t, int games, int wins, int scope, int level)
        {
            int v = t.Stat == TrophyStat.Games ? games : t.Stat == TrophyStat.Wins ? wins : t.Stat == TrophyStat.Scope ? scope : level;
            return Math.Max(0, Math.Min(v, t.Target));
        }

        public static bool Earned(Trophy t, int games, int wins, int scope, int level) => Progress(t, games, wins, scope, level) >= t.Target;

        public static int CountEarned(int games, int wins, int scope, int level)
        {
            int n = 0;
            foreach (var t in All) if (Earned(t, games, wins, scope, level)) n++;
            return n;
        }
    }
}
