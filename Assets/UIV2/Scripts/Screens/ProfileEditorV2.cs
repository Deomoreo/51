using System;
using Project51.UI51;
using Project51.UIV2.Core;
using Project51.UIV2.Data;
using Project51.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UIV2.Screens
{
    /// <summary>
    /// Foglio "Personalizza profilo" (mockup Profilo): avatar, cornice e banner in tre schede.
    /// Non salva niente da solo: SALVA manda la scelta a chi ascolta OnSave e aspetta l'esito (SaveFinished):
    /// chiude solo se e' salvato.
    /// </summary>
    public sealed class ProfileEditorV2 : MonoBehaviour
    {
        private const int BannerTab = 2;

        [SerializeField] private AnimatedModalV2 modal;
        [SerializeField] private SegmentedTabs tabs;
        [SerializeField] private GameObject[] pages;
        [SerializeField] private AvatarFrame preview;
        [SerializeField] private TMP_Text previewLabel;
        [SerializeField] private TMP_Text previewName;
        [SerializeField] private Sprite[] avatars;
        [SerializeField] private Button[] avatarButtons;
        [SerializeField] private Button[] frameButtons;
        [SerializeField] private Button[] bannerButtons;
        [SerializeField] private AvatarFrame[] avatarSamples;
        [SerializeField] private AvatarFrame[] frameSamples;
        [SerializeField] private AvatarFrame[] bannerSamples;
        [SerializeField] private UI51Shape[] bannerShapes;
        [SerializeField] private TMP_Text[] bannerPlayerNames;
        [SerializeField] private GameObject[] bannerLocks;
        [SerializeField] private Button save;
        [SerializeField] private TMP_Text status; // facoltativo: senza, nessun messaggio

        public const string SavingText = "Salvataggio in corso...";
        public const string SavedText = "Aspetto salvato";
        public const string FailedText = "Non salvato: le tue scelte sono ancora qui. Riprova.";
        private const float SavedSeconds = 0.7f, TimeoutSeconds = 15f;

        private int avatar, frame, banner, level;
        private bool saving;

        /// <summary>Id di avatar, cornice e banner scelti.</summary>
        public event Action<string, string, string> OnSave;

        public bool IsOpen => modal != null && modal.IsOpen;

        private void Awake()
        {
            for (int i = 0; i < avatarButtons.Length; i++)
            {
                int index = i;
                avatarButtons[i].onClick.AddListener(() => { if (saving) return; avatar = index; Refresh(); });
            }
            for (int i = 0; i < frameButtons.Length; i++)
            {
                int index = i;
                frameButtons[i].onClick.AddListener(() => { if (saving) return; frame = index; Refresh(); });
            }
            for (int i = 0; i < bannerButtons.Length; i++)
            {
                int index = i;
                bannerButtons[i].onClick.AddListener(() =>
                {
                    if (saving || !ProfileCosmetics.BannerUnlocked(index, level)) return;
                    banner = index;
                    Refresh();
                });
            }
            // I banner animati usano un materiale creato a runtime: si applicano qui, non nella scena.
            for (int i = 0; i < bannerShapes.Length; i++)
                UI51Banners.Apply(bannerShapes[i], ProfileCosmetics.Banner(i));
            if (tabs != null) tabs.onTabChanged.AddListener(ShowTab);
            if (save != null) save.onClick.AddListener(Save);
        }

        /// <summary>Avatar per l'id salvato nel profilo (nome dello sprite); id vuoto o sconosciuto = il primo.</summary>
        public Sprite AvatarFor(string id) => ProfileCosmetics.AvatarFor(avatars, id);

        public void Open(string avatarId, string frameId, string bannerId, int playerLevel, string playerName)
        {
            if (modal == null) return;
            CancelInvoke();
            SetSaving(false);
            Say(null, default);
            avatar = AvatarIndex(avatarId);
            frame = ProfileCosmetics.FrameIndex(frameId);
            banner = ProfileCosmetics.BannerIndex(bannerId);
            level = playerLevel;
            modal.Open();
            foreach (var label in bannerPlayerNames)
                if (label != null) label.text = playerName;
            if (tabs != null) tabs.Select(0, notify: false);
            ShowTab(0);
        }

        private int AvatarIndex(string id)
        {
            for (int i = 0; i < avatars.Length; i++)
                if (avatars[i] != null && avatars[i].name == id) return i;
            return 0;
        }

        private void ShowTab(int index)
        {
            for (int i = 0; i < pages.Length; i++)
                if (pages[i] != null) pages[i].SetActive(i == index);
            Refresh();
        }

        private void Refresh()
        {
            var sprite = avatars.Length > 0 ? avatars[avatar] : null;
            Paint(preview, sprite, frame, 3f, 6f);
            for (int i = 0; i < avatarSamples.Length; i++)
            {
                if (avatarSamples[i] == null) continue;
                bool chosen = i == avatar;
                avatarSamples[i].ringWidth = chosen ? 3f : 2f;
                avatarSamples[i].SetRing(chosen ? UI51Tokens.Gold : UI51Tokens.GoldA(0.3f));
            }
            for (int i = 0; i < frameSamples.Length; i++) Paint(frameSamples[i], sprite, i, 2f, 4f);
            foreach (var sample in bannerSamples) Paint(sample, sprite, frame, 2f, 2f);
            Mark(avatarButtons, avatar);
            Mark(frameButtons, frame);
            Mark(bannerButtons, banner);
            for (int i = 0; i < bannerLocks.Length; i++)
                if (bannerLocks[i] != null) bannerLocks[i].SetActive(!ProfileCosmetics.BannerUnlocked(i, level));

            bool onBanner = tabs != null && tabs.selectedIndex == BannerTab;
            if (previewLabel != null) previewLabel.text = onBanner ? "Banner" : "Cornice";
            if (previewName != null)
                previewName.text = onBanner ? ProfileCosmetics.BannerNames[banner] : ProfileCosmetics.FrameNames[frame];
        }

        private static void Paint(AvatarFrame target, Sprite sprite, int frameIndex, float thin, float thick)
        {
            if (target == null) return;
            target.SetAvatar(sprite);
            ProfileCosmetics.ApplyFrame(target, frameIndex, thin, thick);
        }

        private static void Mark(Button[] buttons, int selected)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                var item = buttons[i] != null ? buttons[i].GetComponent<SelectableToggleItem>() : null;
                if (item != null) item.SetSelected(i == selected);
            }
        }

        private void Save()
        {
            if (saving || avatars.Length == 0 || avatars[avatar] == null) return;
            if (OnSave == null) { modal.Close(); return; }
            // Prima dell'invio: chi ascolta puo' rispondere subito.
            SetSaving(true);
            Say(SavingText, UI51Tokens.CreamA(0.6f));
            // ponytail: se PlayFab non risponde mai, dopo 15 s si torna a poter riprovare o chiudere.
            Invoke(nameof(SaveTimedOut), TimeoutSeconds);
            OnSave.Invoke(avatars[avatar].name, ProfileCosmetics.FrameIds[frame], ProfileCosmetics.BannerIds[banner]);
        }

        /// <summary>Esito, da chi ascolta OnSave. Salvato: conferma e chiusura. Non salvato: il foglio resta aperto con le scelte.</summary>
        public void SaveFinished(bool saved)
        {
            if (!saving) return;
            CancelInvoke(nameof(SaveTimedOut));
            Say(saved ? SavedText : FailedText, saved ? UI51Tokens.SuccessText : UI51Tokens.DangerText);
            if (saved) Invoke(nameof(CloseSaved), SavedSeconds); // resta bloccato fino alla chiusura
            else SetSaving(false);
        }

        private void SaveTimedOut() => SaveFinished(false);

        private void CloseSaved()
        {
            SetSaving(false);
            modal.Close();
        }

        private void SetSaving(bool on)
        {
            saving = on;
            if (save != null) save.interactable = !on;
            // La X vale anche per il velo (DismissOnBackdrop); Esc e tasto indietro passano da HandleEscape.
            if (modal.CloseButton != null) modal.CloseButton.interactable = !on;
            modal.HandleEscape = !on;
        }

        private void Say(string text, Color color)
        {
            if (status == null) return;
            status.gameObject.SetActive(!string.IsNullOrEmpty(text));
            status.text = text ?? string.Empty;
            status.color = color;
        }
    }
}
