using Project51.Core;
using Project51.UIV2.Data;
using Project51.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UIV2.Core
{
    public sealed class QuickSelectionPanels : MonoBehaviour
    {
        public AnimatedModalV2 DeckModal;
        public AnimatedModalV2 ModeModal;
        public Button[] DeckButtons;
        public TMP_Text Caption;
        public Button Confirm;
        public Button[] ModeButtons;
        public Button[] DifficultyButtons;
        public Button CreateRoom;
        public Button JoinRoom;
        public ScrollRect ModeScroll;
        public RoomFlowV2 RoomFlow;
        // UI51 (opzionali): schede Online/Allenamento/Stanza privata, CONFERMA che chiude, testo della difficolta'.
        public Button[] Tabs;
        public GameObject[] TabPages;
        public Button ModeConfirm;
        public TMP_Text DifficultyInfo;
        // UI51 Fase 13 (opzionali), scheda Stanza privata del mockup v3: formato, codice con Incolla; CreateRoom e JoinRoom
        // creano ed entrano subito.
        public Button[] PrivateFormats;
        public TMP_InputField PrivateCode;
        public Button PrivatePaste;
        private static readonly GameFormat[] Formats = { GameFormat.OneVsOne, GameFormat.TwoVsTwo, GameFormat.FourPlayers };
        private float modeScrollHeight;
        private string pendingDeck;
        public bool IsOpen => DeckModal.IsOpen || ModeModal.IsOpen;

        // Scelta di partita della Home: statica, cosi' resta tra MainMenu e tavolo per tutta la sessione.
        private static MatchConfig selection;
        public event System.Action<MatchConfig> SelectionChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSelection() => selection = null;

        /// <summary>Scelta corrente; all'avvio Allenamento 1v3, bot Medio.</summary>
        public MatchConfig Selection => selection ?? (selection = new MatchConfig
        {
            Intent = MatchIntent.Training, Format = GameFormat.FourPlayers, BotDifficulty = BotDifficulty.Medium,
            Rules = MatchRules.Default.Clone()
        });

        private void Select(MatchConfig config)
        {
            selection = config.Clone();
            SelectionChanged?.Invoke(selection);
            RefreshMode();
        }

        private void Awake()
        {
            for (int i = 0; i < DeckButtons.Length; i++) { int index = i; DeckButtons[i].onClick.AddListener(() => PreviewDeck(index)); }
            for (int i = 0; i < ModeButtons.Length; i++) { int index = i; ModeButtons[i].onClick.AddListener(() => ChooseMode(index)); }
            for (int i = 0; i < DifficultyButtons.Length; i++) { int index = i; DifficultyButtons[i].onClick.AddListener(() => ChooseDifficulty(index)); }
            Confirm.onClick.AddListener(ConfirmDeck);
            CreateRoom.onClick.AddListener(() => OpenRoomFlow(true));
            JoinRoom.onClick.AddListener(() => OpenRoomFlow(false));
            if (Tabs != null) for (int i = 0; i < Tabs.Length; i++) { int index = i; Tabs[i].onClick.AddListener(() => ShowTab(index)); }
            if (ModeConfirm != null) ModeConfirm.onClick.AddListener(() => ModeModal.Close());
            if (PrivateFormats != null) for (int i = 0; i < PrivateFormats.Length; i++) { int index = i; PrivateFormats[i].onClick.AddListener(() => ChoosePrivateFormat(index)); }
            if (PrivateCode != null)
            {
                PrivateCode.onValueChanged.AddListener(value =>
                {
                    string code = RoomFlowV2.NormalizeCode(value);
                    if (code != value) PrivateCode.SetTextWithoutNotify(code);
                });
                PrivateCode.onSubmit.AddListener(_ => OpenRoomFlow(false));
                PrivatePaste.onClick.AddListener(() => PrivateCode.text = RoomFlowV2.NormalizeCode(GUIUtility.systemCopyBuffer));
            }
            // "Gioca online invece" della StanzaErrore parte con la ricerca: Modalita' (riaperta sotto l'errore) non resta dietro.
            if (RoomFlow != null && RoomFlow.RoomError != null) RoomFlow.RoomError.Alt.onClick.AddListener(() => ModeModal.Close());
            var scrollSize = ModeScroll.GetComponent<LayoutElement>();
            if (scrollSize != null) modeScrollHeight = scrollSize.preferredHeight;
        }

        private void ShowTab(int index)
        {
            if (Tabs == null || TabPages == null) return;
            for (int i = 0; i < TabPages.Length; i++) TabPages[i].SetActive(i == index);
            for (int i = 0; i < Tabs.Length; i++) Tabs[i].GetComponent<SelectableToggleItem>().SetSelected(i == index);
            if (ModeConfirm != null)
            {
                ModeConfirm.gameObject.SetActive(index != 2);
                // Stanza privata non ha CONFERMA: la pagina (due riquadri nel mockup v3) prende anche il suo spazio.
                var scrollSize = ModeScroll.GetComponent<LayoutElement>();
                var sheet = ModeConfirm.transform.parent.GetComponent<VerticalLayoutGroup>();
                var confirmSize = ModeConfirm.GetComponent<LayoutElement>();
                if (scrollSize != null && sheet != null && confirmSize != null && PrivateFormats != null && PrivateFormats.Length > 0)
                    scrollSize.preferredHeight = modeScrollHeight + (index == 2 ? confirmSize.preferredHeight + sheet.spacing : 0f);
            }
            ModeScroll.verticalNormalizedPosition = 1;
            RefreshPrivateFormats();
        }

        private void ChoosePrivateFormat(int index)
        {
            if (RoomFlow != null) RoomFlow.Format = Formats[index];
            RefreshPrivateFormats();
        }

        private void RefreshPrivateFormats()
        {
            if (PrivateFormats == null || RoomFlow == null) return;
            for (int i = 0; i < PrivateFormats.Length; i++)
                PrivateFormats[i].GetComponent<SelectableToggleItem>().SetSelected(Formats[i] == RoomFlow.Format);
        }
        /// <summary>
        /// Crea/entra in stanza privata: il pannello Modalita' si chiude per lasciare il posto al
        /// flusso online, ma si riapre se da li' si annulla - annullando si torna indietro di un
        /// passo, non fino alla Home.
        /// </summary>
        private void OpenRoomFlow(bool create)
        {
            if (!create && !RoomFlowV2.IsValidCode(RoomFlowV2.NormalizeCode(PrivateCode.text)))
            {
                Project51.UI51.UIAnim.ShakeX((RectTransform)PrivateCode.transform); // meno di 5 caratteri: resta qui
                return;
            }
            ModeModal.Close();
            // Prima di partire (un ingresso rifiutato torna subito qui), e sulla scheda Stanza privata: la scelta in corso puo'
            // essere Online o Allenamento, ma si era qui.
            RoomFlow.ReturnOnCancel = () => { ShowModes(); ShowTab(2); };
            if (create) RoomFlow.CreateNow();
            else RoomFlow.JoinCode(PrivateCode.text);
        }

        public void OpenDecks()
        {
            if (IsOpen) return;
            DeckModal.Open();
            var entries = CardDecks.Catalog.Entries;
            for (int i = 0; i < entries.Count; i++) if (entries[i].Id == CardDecks.SelectedId) PreviewDeck(i);
        }
        public void PreviewDeck(int index)
        {
            var entries = CardDecks.Catalog.Entries;
            if (index < 0 || index >= entries.Count) return;
            var entry = entries[index]; pendingDeck = entry.Id;
            Caption.text = string.IsNullOrEmpty(entry.Subtitle) ? entry.DisplayName : entry.DisplayName + " · " + entry.Subtitle;
            for (int i = 0; i < DeckButtons.Length; i++) DeckButtons[i].GetComponent<SelectableToggleItem>().SetSelected(i == index);
        }
        public void ConfirmDeck() { if (CardDecks.Select(pendingDeck)) DeckModal.Close(); }
        public void OpenModes()
        {
            if (IsOpen) return;
            ShowModes();
        }

        /// <summary>INVITA degli Amici: Modalita' sulla scheda Stanza privata (CREA STANZA manda poi il codice all'amico).</summary>
        public void OpenPrivateRoom()
        {
            if (!ModeModal.IsOpen) ShowModes();
            ShowTab(2);
        }

        /// <summary>"GIOCA CONTRO I BOT" della Sospensione: Modalita' sulla scheda Allenamento, col formato che era scelto.</summary>
        public void OpenTraining()
        {
            var c = Selection;
            int format = c.Format == GameFormat.OneVsOne ? 0 : c.Format == GameFormat.TwoVsTwo ? 1 : 2;
            if (c.Intent != MatchIntent.Training) ChooseMode(3 + format);
            if (!ModeModal.IsOpen) ShowModes();
            ShowTab(1);
        }

        /// <summary>
        /// Riapre Modalita' tornando indietro dal flusso stanze. Senza il controllo "e' gia'
        /// aperto" di OpenModes: qui si sta rientrando, e il pannello potrebbe risultare ancora
        /// aperto per la manciata di centesimi della sua animazione di chiusura.
        /// </summary>
        private void ShowModes()
        {
            ModeModal.Open(); ModeScroll.verticalNormalizedPosition = 1; RefreshMode();
            var intent = Selection.Intent;
            ShowTab(intent == MatchIntent.Training ? 1 : intent == MatchIntent.PrivateRoom ? 2 : 0);
        }
        private void ChooseMode(int index)
        {
            var config = new MatchConfig { Intent = index < 3 ? MatchIntent.QuickMatch : MatchIntent.Training,
                Format = Formats[index % 3], BotDifficulty = Selection.BotDifficulty,
                Rules = MatchRules.ForFormat(Formats[index % 3]) };
            Select(config);
        }
        private void ChooseDifficulty(int index)
        {
            var config = Selection.Clone();
            config.BotDifficulty = new[] { BotDifficulty.Easy, BotDifficulty.Medium, BotDifficulty.Hard }[index];
            Select(config);
        }
        private static int ModeIndex(MatchConfig c)
        {
            if (c.Intent == MatchIntent.PrivateRoom) return -1;
            int index = c.Format == GameFormat.OneVsOne ? 0 : c.Format == GameFormat.TwoVsTwo ? 1 : 2;
            return c.Intent == MatchIntent.Training ? index + 3 : index;
        }

        /// <summary>Testi della tile Modalita' della Home (didascalia, valore, sigla) come nel mockup UI51.</summary>
        public SelectorOptionViewData ModeOption(MatchConfig c)
        {
            int index = ModeIndex(c);
            var row = index >= 0 && index < ModeButtons.Length ? ModeButtons[index].transform : CreateRoom.transform;
            var icon = row.Find("Icon");
            int f = c.Format == GameFormat.OneVsOne ? 0 : c.Format == GameFormat.TwoVsTwo ? 1 : 2;
            string label = new[] { "1 vs 1", "2 vs 2", "1 vs 3" }[f], shortName = new[] { "1v1", "2v2", "1v3" }[f];
            string level = c.BotDifficulty == BotDifficulty.Easy ? "Facile" : c.BotDifficulty == BotDifficulty.Hard ? "Difficile" : "Medio";
            bool bot = c.Intent == MatchIntent.Training, room = c.Intent == MatchIntent.PrivateRoom;
            return new SelectorOptionViewData
            {
                Id = c.Intent.ToString(),
                DisplayName = bot ? shortName + " · " + level : label,
                Caption = bot ? "ALLENAMENTO" : room ? "STANZA PRIVATA" : "ONLINE",
                ShortName = bot ? "BOT" : room ? "PRIV" : shortName,
                Icon = icon != null ? icon.GetComponent<Image>().sprite : null,
            };
        }

        private void RefreshMode()
        {
            var c = Selection;
            int selected = ModeIndex(c);
            for (int i = 0; i < ModeButtons.Length; i++) ModeButtons[i].GetComponent<SelectableToggleItem>().SetSelected(i == selected);
            var levels = new[] { BotDifficulty.Easy, BotDifficulty.Medium, BotDifficulty.Hard };
            for (int i = 0; i < DifficultyButtons.Length; i++) DifficultyButtons[i].GetComponent<SelectableToggleItem>().SetSelected(levels[i] == c.BotDifficulty);
            if (DifficultyInfo != null) DifficultyInfo.text = c.BotDifficulty == BotDifficulty.Easy ? "Il bot commette errori: ideale per imparare le regole."
                : c.BotDifficulty == BotDifficulty.Hard ? "Il bot conta le carte e punta alle combinazioni migliori."
                : "Il bot gioca in modo equilibrato, senza strategie avanzate.";
        }
    }
}
