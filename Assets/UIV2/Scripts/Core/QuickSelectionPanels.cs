using Project51.Core;
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
        public ModalitySelectorPanelUI Modes;
        public Button[] DeckButtons;
        public Image[] Fan;
        public TMP_Text Caption;
        public Button Confirm;
        public Button[] ModeButtons;
        public Button[] DifficultyButtons;
        public Button CreateRoom;
        public Button JoinRoom;
        public ScrollRect ModeScroll;
        public RoomFlowV2 RoomFlow;
        private string pendingDeck;
        public bool IsOpen => DeckModal.IsOpen || ModeModal.IsOpen;

        private void Awake()
        {
            for (int i = 0; i < DeckButtons.Length; i++) { int index = i; DeckButtons[i].onClick.AddListener(() => PreviewDeck(index)); }
            for (int i = 0; i < ModeButtons.Length; i++) { int index = i; ModeButtons[i].onClick.AddListener(() => ChooseMode(index)); }
            for (int i = 0; i < DifficultyButtons.Length; i++) { int index = i; DifficultyButtons[i].onClick.AddListener(() => ChooseDifficulty(index)); }
            Confirm.onClick.AddListener(ConfirmDeck);
            CreateRoom.onClick.AddListener(() => { ModeModal.Close(); if(RoomFlow!=null)RoomFlow.OpenCreate();else Modes.Select_CreatePrivateRoom(); });
            JoinRoom.onClick.AddListener(() => { ModeModal.Close(); if(RoomFlow!=null)RoomFlow.OpenJoin();else Modes.Select_JoinPrivateRoom(); });
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
            Caption.text = entry.DisplayName + " · 40 carte";
            foreach (var image in Fan) { image.sprite = entry.Artwork; image.preserveAspect = true; }
            for (int i = 0; i < DeckButtons.Length; i++) DeckButtons[i].GetComponent<SelectableToggleItem>().SetSelected(i == index);
        }
        public void ConfirmDeck() { if (CardDecks.Select(pendingDeck)) DeckModal.Close(); }
        public void OpenModes()
        {
            if (IsOpen) return;
            ModeModal.Open(); ModeScroll.verticalNormalizedPosition = 1; RefreshMode();
        }
        private void ChooseMode(int index)
        {
            var formats = new[] { GameFormat.OneVsOne, GameFormat.TwoVsTwo, GameFormat.FourPlayers };
            var config = new MatchConfig { Intent = index < 3 ? MatchIntent.QuickMatch : MatchIntent.Training,
                Format = formats[index % 3], BotDifficulty = Modes.CurrentSelection.BotDifficulty,
                DeckBackId = CardDecks.SelectedId, Rules = MatchRules.Default.Clone() };
            if (config.Format == GameFormat.OneVsOne) { config.Rules.CappottoEndsGameImmediately = false; config.Rules.CappottoBonusPoints = 0; }
            Modes.SetSelection(config); RefreshMode();
        }
        private void ChooseDifficulty(int index)
        {
            var config = Modes.CurrentSelection.Clone();
            config.BotDifficulty = new[] { BotDifficulty.Easy, BotDifficulty.Medium, BotDifficulty.Hard }[index];
            Modes.SetSelection(config); RefreshMode();
        }
        private void RefreshMode()
        {
            var c = Modes.CurrentSelection;
            int selected = c.Format == GameFormat.OneVsOne ? 0 : c.Format == GameFormat.TwoVsTwo ? 1 : 2;
            if (c.Intent == MatchIntent.Training) selected += 3;
            if (c.Intent == MatchIntent.PrivateRoom) selected = -1;
            for (int i = 0; i < ModeButtons.Length; i++) ModeButtons[i].GetComponent<SelectableToggleItem>().SetSelected(i == selected);
            var levels = new[] { BotDifficulty.Easy, BotDifficulty.Medium, BotDifficulty.Hard };
            for (int i = 0; i < DifficultyButtons.Length; i++) DifficultyButtons[i].GetComponent<SelectableToggleItem>().SetSelected(levels[i] == c.BotDifficulty);
        }
    }
}
