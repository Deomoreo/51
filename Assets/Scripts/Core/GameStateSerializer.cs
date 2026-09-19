using System;
using System.Collections.Generic;
using System.Globalization;

namespace Project51.Core
{
    /// <summary>
    /// Formato testuale dello GameState scambiato in rete dall'host (NetworkGameController).
    /// Sezioni separate da "||": numPlayers, dealer, current, deck, table, un giocatore per posto,
    /// lastCapture, roundEnded, roundIndex, teamMode, regole, totali di partita. Le sezioni finali sono
    /// opzionali in lettura per restare compatibili con stati prodotti da versioni precedenti.
    /// </summary>
    public static class GameStateSerializer
    {
        private const string SectionSeparator = "||";

        public static string Serialize(GameState gs)
        {
            var parts = new List<string>
            {
                gs.NumPlayers.ToString(),
                gs.DealerIndex.ToString(),
                gs.CurrentPlayerIndex.ToString(),
                SerializeCards(gs.Deck),
                SerializeCards(gs.Table)
            };
            for (int i = 0; i < gs.NumPlayers; i++)
                parts.Add(SerializePlayer(gs.Players[i]));

            parts.Add(gs.LastCapturePlayerIndex.ToString());
            parts.Add(gs.RoundEnded ? "1" : "0");
            parts.Add(gs.RoundIndex.ToString());
            parts.Add(gs.TeamMode ? "1" : "0");
            var rules = gs.Rules ?? MatchRules.Default;
            parts.Add(string.Join(",",
                rules.EnableAccusi ? "1" : "0",
                rules.AccusiPointMultiplier.ToString(CultureInfo.InvariantCulture),
                rules.CappottoEndsGameImmediately ? "1" : "0",
                rules.CappottoBonusPoints.ToString()));
            parts.Add(gs.MatchTotals != null ? string.Join(",", gs.MatchTotals) : "");
            return string.Join(SectionSeparator, parts);
        }

        /// <summary>Lancia FormatException se il testo non e' uno stato valido.</summary>
        public static GameState Deserialize(string data)
        {
            string[] parts = data.Split(new[] { SectionSeparator }, StringSplitOptions.None);
            if (parts.Length < 5)
                throw new FormatException($"GameState incompleto: {parts.Length} sezioni");

            int numPlayers = int.Parse(parts[0]);
            if (parts.Length < 5 + numPlayers)
                throw new FormatException($"GameState senza tutti i {numPlayers} giocatori");

            var gs = new GameState(numPlayers)
            {
                DealerIndex = int.Parse(parts[1]),
                CurrentPlayerIndex = int.Parse(parts[2])
            };
            gs.Deck.AddRange(DeserializeCards(parts[3]));
            gs.Table.AddRange(DeserializeCards(parts[4]));

            for (int i = 0; i < numPlayers; i++)
                DeserializePlayer(parts[5 + i], gs.Players[i]);

            int tail = 5 + numPlayers;
            if (parts.Length > tail + 2)
            {
                gs.LastCapturePlayerIndex = int.Parse(parts[tail]);
                gs.RoundEnded = parts[tail + 1] == "1";
                gs.RoundIndex = int.Parse(parts[tail + 2]);
            }
            if (parts.Length > tail + 5)
            {
                gs.TeamMode = parts[tail + 3] == "1";
                var rules = parts[tail + 4].Split(',');
                gs.Rules = new MatchRules
                {
                    EnableAccusi = rules[0] == "1",
                    AccusiPointMultiplier = float.Parse(rules[1], CultureInfo.InvariantCulture),
                    CappottoEndsGameImmediately = rules[2] == "1",
                    CappottoBonusPoints = int.Parse(rules[3])
                };
                gs.MatchTotals = string.IsNullOrEmpty(parts[tail + 5])
                    ? null
                    : Array.ConvertAll(parts[tail + 5].Split(','), int.Parse);
            }
            return gs;
        }

        // Giocatore: mano;prese;scope;accusi;accusiSmazzata;punteggio;carteScopa
        private static string SerializePlayer(PlayerState player)
        {
            return string.Join(";",
                SerializeCards(player.Hand),
                SerializeCards(player.CapturedCards),
                player.ScopaCount.ToString(),
                player.AccusiPoints.ToString(),
                player.RoundAccusiPoints.ToString(),
                player.TotalScore.ToString(),
                SerializeCards(player.ScopaCards));
        }

        private static void DeserializePlayer(string data, PlayerState player)
        {
            var values = data.Split(';');
            player.Hand.AddRange(DeserializeCards(values[0]));
            player.CapturedCards.AddRange(DeserializeCards(values.Length > 1 ? values[1] : ""));
            if (values.Length > 5)
            {
                player.ScopaCount = int.Parse(values[2]);
                player.AccusiPoints = int.Parse(values[3]);
                player.RoundAccusiPoints = int.Parse(values[4]);
                player.TotalScore = int.Parse(values[5]);
                if (values.Length > 6) player.ScopaCards.AddRange(DeserializeCards(values[6]));
            }
        }

        private static string SerializeCards(List<Card> cards)
        {
            if (cards == null || cards.Count == 0) return "";
            return string.Join(",", cards.ConvertAll(c => $"{c.Suit}:{c.Rank}"));
        }

        private static List<Card> DeserializeCards(string data)
        {
            var cards = new List<Card>();
            if (string.IsNullOrEmpty(data)) return cards;
            foreach (var item in data.Split(','))
            {
                if (string.IsNullOrEmpty(item)) continue;
                var card = item.Split(':');
                cards.Add(new Card((Suit)Enum.Parse(typeof(Suit), card[0]), int.Parse(card[1])));
            }
            return cards;
        }
    }
}
