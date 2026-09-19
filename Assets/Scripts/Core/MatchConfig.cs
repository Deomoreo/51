using System;

namespace Project51.Core
{
    /// <summary>
    /// Configurazione completa di una partita.
    /// Contiene tutte le informazioni necessarie per avviare un match.
    /// </summary>
    [Serializable]
    public class MatchConfig
    {
        /// <summary>
        /// L'intento principale: cosa vuole fare il giocatore.
        /// </summary>
        public MatchIntent Intent { get; set; } = MatchIntent.Training;

        /// <summary>
        /// Formato del gioco: numero di giocatori.
        /// </summary>
        public GameFormat Format { get; set; } = GameFormat.FourPlayers;

        /// <summary>
        /// Difficolt� dei bot (solo per Training).
        /// </summary>
        public BotDifficulty BotDifficulty { get; set; } = BotDifficulty.Medium;

        /// <summary>
        /// Punteggio target per vincere la partita.
        /// </summary>
        public int TargetScore { get; set; } = 51;

        /// <summary>
        /// Codice stanza per Tavolo Privato (se applicabile).
        /// </summary>
        public string RoomCode { get; set; }

        /// <summary>
        /// Se true, siamo l'host della stanza privata.
        /// </summary>
        public bool IsHost { get; set; }

        /// <summary>
        /// ID del back del mazzo selezionato.
        /// </summary>
        public string DeckBackId { get; set; } = "default";

        /// <summary>
        /// Optional rules tweaks for specific modes (e.g. 1v1).
        /// If null, defaults are used.
        /// </summary>
        public MatchRules Rules { get; set; } = MatchRules.Default;

        /// <summary>
        /// Restituisce il numero di giocatori basato sul formato.
        /// </summary>
        public int PlayerCount => Format switch
        {
            GameFormat.OneVsOne => 2,
            GameFormat.FourPlayers => 4,
            GameFormat.TwoVsTwo => 4,
            _ => 4
        };

        /// <summary>
        /// Crea una copia della configurazione.
        /// </summary>
        public MatchConfig Clone()
        {
            return new MatchConfig
            {
                Intent = Intent,
                Format = Format,
                BotDifficulty = BotDifficulty,
                TargetScore = TargetScore,
                RoomCode = RoomCode,
                IsHost = IsHost,
                DeckBackId = DeckBackId,
                Rules = Rules != null ? Rules.Clone() : MatchRules.Default
            };
        }

        public override string ToString()
        {
            return $"[MatchConfig] Intent={Intent}, Format={Format}, Target={TargetScore}, Bot={BotDifficulty}, Rules={(Rules != null ? Rules.ToString() : "<null>")}";
        }
    }

    /// <summary>
    /// Le 3 porte principali della lobby.
    /// </summary>
    public enum MatchIntent
    {
        /// <summary>
        /// Coda pubblica - Quick Match (un tap e gioco).
        /// </summary>
        QuickMatch,

        /// <summary>
        /// Tavolo privato - Gioca con amici via codice stanza.
        /// </summary>
        PrivateRoom,

        /// <summary>
        /// Allenamento - Partita vs Bot offline.
        /// </summary>
        Training
    }

    /// <summary>
    /// Formato del gioco.
    /// </summary>
    public enum GameFormat
    {
        /// <summary>
        /// 1 vs 1 (duello)
        /// </summary>
        OneVsOne,

        /// <summary>
        /// 4 giocatori tutti contro tutti (formato classico Cirulla/51)
        /// </summary>
        FourPlayers,

        /// <summary>
        /// 2 vs 2 (squadre)
        /// </summary>
        TwoVsTwo
    }

    /// <summary>
    /// Difficolt� dei bot.
    /// </summary>
    public enum BotDifficulty
    {
        Easy,
        Medium,
        Hard,
        Expert
    }

    /// <summary>
    /// Helper statico per salvare/caricare MatchConfig tra scene.
    /// </summary>
    public static class MatchConfigStorage
    {
        private const string KEY_INTENT = "MatchIntent";
        private const string KEY_FORMAT = "GameFormat";
        private const string KEY_DIFFICULTY = "BotDifficulty";
        private const string KEY_TARGET = "TargetScore";
        private const string KEY_DECK = "DeckBackId";
        private const string KEY_ACCUSI = "MatchRules_EnableAccusi";
        private const string KEY_ACCUSI_MULTIPLIER = "MatchRules_AccusiMultiplier";
        private const string KEY_CAPPOTTO_IMMEDIATE = "MatchRules_CappottoImmediate";
        private const string KEY_CAPPOTTO_BONUS = "MatchRules_CappottoBonus";

        /// <summary>
        /// Salva la config in PlayerPrefs.
        /// </summary>
        public static void Save(MatchConfig config)
        {
            if (config == null) return;

            UnityEngine.PlayerPrefs.SetInt(KEY_INTENT, (int)config.Intent);
            UnityEngine.PlayerPrefs.SetInt(KEY_FORMAT, (int)config.Format);
            UnityEngine.PlayerPrefs.SetInt(KEY_DIFFICULTY, (int)config.BotDifficulty);
            UnityEngine.PlayerPrefs.SetInt(KEY_TARGET, config.TargetScore);
            UnityEngine.PlayerPrefs.SetString(KEY_DECK, config.DeckBackId ?? "default");
            var rules = config.Rules ?? MatchRules.Default;
            UnityEngine.PlayerPrefs.SetInt(KEY_ACCUSI, rules.EnableAccusi ? 1 : 0);
            UnityEngine.PlayerPrefs.SetFloat(KEY_ACCUSI_MULTIPLIER, rules.AccusiPointMultiplier);
            UnityEngine.PlayerPrefs.SetInt(KEY_CAPPOTTO_IMMEDIATE, rules.CappottoEndsGameImmediately ? 1 : 0);
            UnityEngine.PlayerPrefs.SetInt(KEY_CAPPOTTO_BONUS, rules.CappottoBonusPoints);
            UnityEngine.PlayerPrefs.Save();
        }

        /// <summary>
        /// Carica la config da PlayerPrefs.
        /// </summary>
        public static MatchConfig Load()
        {
            return new MatchConfig
            {
                Intent = (MatchIntent)UnityEngine.PlayerPrefs.GetInt(KEY_INTENT, (int)MatchIntent.Training),
                Format = (GameFormat)UnityEngine.PlayerPrefs.GetInt(KEY_FORMAT, (int)GameFormat.FourPlayers),
                BotDifficulty = (BotDifficulty)UnityEngine.PlayerPrefs.GetInt(KEY_DIFFICULTY, (int)BotDifficulty.Medium),
                TargetScore = UnityEngine.PlayerPrefs.GetInt(KEY_TARGET, 51),
                DeckBackId = UnityEngine.PlayerPrefs.GetString(KEY_DECK, "default"),
                Rules = new MatchRules
                {
                    EnableAccusi = UnityEngine.PlayerPrefs.GetInt(KEY_ACCUSI, MatchRules.Default.EnableAccusi ? 1 : 0) != 0,
                    AccusiPointMultiplier = UnityEngine.PlayerPrefs.GetFloat(KEY_ACCUSI_MULTIPLIER, MatchRules.Default.AccusiPointMultiplier),
                    CappottoEndsGameImmediately = UnityEngine.PlayerPrefs.GetInt(KEY_CAPPOTTO_IMMEDIATE, MatchRules.Default.CappottoEndsGameImmediately ? 1 : 0) != 0,
                    CappottoBonusPoints = UnityEngine.PlayerPrefs.GetInt(KEY_CAPPOTTO_BONUS, MatchRules.Default.CappottoBonusPoints)
                }
            };
        }

        /// <summary>
        /// Pulisce i dati salvati.
        /// </summary>
        public static void Clear()
        {
            UnityEngine.PlayerPrefs.DeleteKey(KEY_INTENT);
            UnityEngine.PlayerPrefs.DeleteKey(KEY_FORMAT);
            UnityEngine.PlayerPrefs.DeleteKey(KEY_DIFFICULTY);
            UnityEngine.PlayerPrefs.DeleteKey(KEY_TARGET);
            UnityEngine.PlayerPrefs.DeleteKey(KEY_DECK);
            UnityEngine.PlayerPrefs.DeleteKey(KEY_ACCUSI);
            UnityEngine.PlayerPrefs.DeleteKey(KEY_ACCUSI_MULTIPLIER);
            UnityEngine.PlayerPrefs.DeleteKey(KEY_CAPPOTTO_IMMEDIATE);
            UnityEngine.PlayerPrefs.DeleteKey(KEY_CAPPOTTO_BONUS);
        }
    }

    /// <summary>
    /// Implementazione di IGameModeProvider per le partite vs Bot.
    /// </summary>
    public class TrainingGameModeProvider : IGameModeProvider
    {
        private readonly int _numPlayers;
        private readonly BotDifficulty _difficulty;

        public TrainingGameModeProvider(int numPlayers, BotDifficulty difficulty = BotDifficulty.Medium)
        {
            _numPlayers = numPlayers;
            _difficulty = difficulty;
        }

        public bool IsMultiplayer => false;
        public bool IsMasterClient => true;
        public int LocalPlayerIndex => 0;
        public BotDifficulty BotDifficulty => _difficulty;

        public bool IsLocalPlayer(int playerIndex) => playerIndex == 0;
        public bool IsHumanPlayer(int playerIndex) => playerIndex == 0;
        public bool IsBotPlayer(int playerIndex) => playerIndex != 0;
    }

    /// <summary>
    /// Implementazione di IGameModeProvider per le partite multiplayer.
    /// Non dipende da Photon - l'indice locale viene passato dal chiamante.
    /// </summary>
    public class MultiplayerGameModeProvider : IGameModeProvider
    {
        private readonly int _localPlayerIndex;
        private readonly int _numPlayers;
        private readonly bool _isMasterClient;
        private readonly System.Collections.Generic.HashSet<int> _botPlayerIndices;

        /// <param name="botPlayerIndices">
        /// Indici dei posti "riempiti con bot" (stanza privata avviata dall'host con meno
        /// giocatori reali del formato). Null o vuoto = nessun bot (comportamento originale).
        /// I bot sono giocati dal Master Client tramite CirullaAI e le mosse vengono comunque
        /// propagate via RPC agli altri client, esattamente come una mossa umana locale
        /// (vedi TurnController.ExecuteMove).
        /// </param>
        public MultiplayerGameModeProvider(int localPlayerIndex, int numPlayers, bool isMasterClient, System.Collections.Generic.HashSet<int> botPlayerIndices = null)
        {
            _localPlayerIndex = localPlayerIndex;
            _numPlayers = numPlayers;
            _isMasterClient = isMasterClient;
            _botPlayerIndices = botPlayerIndices;
        }

        public bool IsMultiplayer => true;
        public bool IsMasterClient => _isMasterClient;
        public int LocalPlayerIndex => _localPlayerIndex;

        public bool IsLocalPlayer(int playerIndex) => playerIndex == _localPlayerIndex;
        public bool IsHumanPlayer(int playerIndex) => playerIndex >= 0 && playerIndex < _numPlayers && !IsBotPlayer(playerIndex);
        public bool IsBotPlayer(int playerIndex) => _botPlayerIndices != null && _botPlayerIndices.Contains(playerIndex);
    }

    [Serializable]
    public class MatchRules
    {
        /// <summary>
        /// Regole standard, sempre un oggetto nuovo: prima era un'istanza condivisa e chi la modificava
        /// (es. il cappotto spento per la 1v1) la cambiava per tutte le partite della sessione.
        /// </summary>
        public static MatchRules Default => new MatchRules();

        /// <summary>
        /// If false, Cirulla/Decino are disabled entirely.
        /// </summary>
        public bool EnableAccusi { get; set; } = true;

        /// <summary>
        /// Optional multiplier for accusi points (e.g. 0.5 for 1v1 balancing).
        /// Applied to any accusi points awarded.
        /// </summary>
        public float AccusiPointMultiplier { get; set; } = 1f;

        /// <summary>
        /// If false, capturing all 10 denari does NOT end the game immediately.
        /// </summary>
        public bool CappottoEndsGameImmediately { get; set; } = true;

        /// <summary>
        /// Points granted for Cappotto when not ending the game.
        /// </summary>
        public int CappottoBonusPoints { get; set; } = 0;

        /// <summary>Punti effettivi di un accuso (3 Cirulla, 10 Decino...) con queste regole.</summary>
        public int AccusoPoints(int basePoints)
        {
            if (!EnableAccusi || AccusiPointMultiplier <= 0f)
                return 0;
            return (int)System.Math.Round(basePoints * AccusiPointMultiplier, System.MidpointRounding.AwayFromZero);
        }

        public MatchRules Clone()
        {
            return new MatchRules
            {
                EnableAccusi = EnableAccusi,
                AccusiPointMultiplier = AccusiPointMultiplier,
                CappottoEndsGameImmediately = CappottoEndsGameImmediately,
                CappottoBonusPoints = CappottoBonusPoints
            };
        }

        public override string ToString()
        {
            return $"Accusi={(EnableAccusi ? "ON" : "OFF")},AccMul={AccusiPointMultiplier},CappottoImmediate={(CappottoEndsGameImmediately ? "YES" : "NO")},CappottoBonus={CappottoBonusPoints}";
        }
    }
}
