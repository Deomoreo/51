using System.Collections.Generic;
using Project51.Auth;
using Project51.Core;
using Project51.UI51;
using Project51.UIV2.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UI
{
    /// <summary>
    /// Profilo rapido al tavolo (mockup Partita/Partita4 "profilo", PartitaMioProfilo): scheda da 300 col banner del giocatore,
    /// avatar, livello e titolo, statistiche, medaglie; per gli altri giocatori con account Aggiungi amico, Silenzia emoticon,
    /// Segnala (col motivo) e Blocca; per gli ospiti solo Silenzia e Segnala; per me la barra XP. Costruita da
    /// UI51TableBuilder.BuildQuickProfile, aperta da PlayerBannerManager.OpenProfile.
    /// </summary>
    public sealed class QuickProfileCard : MonoBehaviour
    {
        /// <summary>Dati di una scheda. Level &lt;= 0 = nascosto; PlayFabId null = niente pulsanti.</summary>
        public struct View
        {
            public string Name, Team, PlayFabId;
            public Sprite Avatar;
            public int Frame, Style, Level, Games, Wins, Scope, Xp;
            public bool Stats, Self, Guest; // Guest: PlayFabId e' quello della sessione d'ospite
            public bool ViewerGuest; // chi guarda e' un ospite: puo' solo silenziare (B12/E2), per la sessione
            public float Top; // bordo alto della scheda nel mockup (390x844)
        }

        private const float CenterY = 422f; // meta' dei 844 del mockup: la scheda si mette rispetto al centro come il visore delle scope

        [SerializeField] private RectTransform content;
        [SerializeField] private RectTransform card;
        [SerializeField] private UI51Shape banner;
        [SerializeField] private AvatarFrame avatar;
        [SerializeField] private TMP_Text nameText, levelText, titleText, teamText;
        [SerializeField] private GameObject levelPill;
        [SerializeField] private GameObject stats;
        [SerializeField] private TMP_Text gamesText, winsText, scopeText;
        [SerializeField] private GameObject medals;
        [SerializeField] private GameObject[] medalIcons = new GameObject[0];
        [SerializeField] private GameObject actions;
        [SerializeField] private Button addButton, muteButton, reportButton;
        [SerializeField] private GameObject addedLabel;
        [SerializeField] private TMP_Text muteLabel, reportLabel;
        [SerializeField] private GameObject actionRow;
        [SerializeField] private Button blockButton;
        [SerializeField] private TMP_Text blockLabel;
        [SerializeField] private GameObject reasons;
        [SerializeField] private Button[] reasonButtons = new Button[0]; // stesso ordine di ReasonIds
        [SerializeField] private Button cancelReasons;
        [SerializeField] private GameObject self;
        [SerializeField] private TMP_Text xpLevel, xpText;
        [SerializeField] private RectTransform xpFill;

        // Richieste mandate (s_Friends: amici subito) e segnalazioni fatte da questo account in questa sessione: riaprendo la scheda
        // restano. B17: di un solo account.
        private static readonly HashSet<string> s_Added = new HashSet<string>(), s_Reported = new HashSet<string>(), s_Friends = new HashSet<string>();
        private static string s_Owner;
        /// <summary>Motivi della segnalazione (scelta dell'utente 01/10), come i pulsanti del builder.</summary>
        public static readonly string[] ReasonIds = { ModerationService.ReasonEmoticon, ModerationService.ReasonName, ModerationService.ReasonGame };
        public static readonly string[] ReasonLabels = { "Emoticon offensive", "Nome offensivo", "Gioco scorretto" };

        private string playFabId;
        private bool wired, guest, viewerGuest;
        private float[] nameY; // y costruite di nome e squadra

        public bool IsOpen => gameObject.activeSelf;

        public void Show(View v)
        {
            Wire();
            string owner = AuthBootstrapper.Instance?.PlayFabAuth?.PlayFabId;
            if (owner != s_Owner) { s_Owner = owner; s_Added.Clear(); s_Reported.Clear(); s_Friends.Clear(); }
            playFabId = v.Self ? null : v.PlayFabId;
            guest = v.Guest;
            viewerGuest = v.ViewerGuest;
            content.anchoredPosition = new Vector2(0f, (CenterY - v.Top) * content.localScale.y);
            UI51Banners.Apply(banner, ProfileCosmetics.Banner(v.Style));
            if (v.Avatar != null) avatar.SetAvatar(v.Avatar);
            ProfileCosmetics.ApplyFrame(avatar, v.Frame, 3f, 3f);

            nameText.text = v.Name;
            bool level = v.Level > 0;
            levelPill.SetActive(level);
            titleText.gameObject.SetActive(level);
            levelText.text = level ? v.Level.ToString() : "";
            titleText.text = level ? PlayerXp.Title(v.Level) : "";
            teamText.text = v.Team;
            // Senza la riga del livello (bot, ospiti) nome e squadra si centrano nei 64 come nel flex del mockup.
            if (nameY == null) nameY = new[] { nameText.rectTransform.anchoredPosition.y, teamText.rectTransform.anchoredPosition.y };
            SetY(nameText, level ? nameY[0] : nameY[0] - 12f);
            SetY(teamText, level ? nameY[1] : nameY[0] - 36f);

            stats.SetActive(v.Stats);
            gamesText.text = v.Games.ToString();
            winsText.text = ProfileCosmetics.WinRate(v.Wins, v.Games);
            scopeText.text = v.Scope.ToString();
            int mask = v.Stats ? PlayerXp.Medals(v.Games, v.Wins, v.Scope, v.Self ? PlayerXp.LevelOf(v.Xp) : v.Level) : 0;
            medals.SetActive(mask != 0);
            for (int i = 0; i < medalIcons.Length; i++) medalIcons[i].SetActive((mask & (1 << i)) != 0);

            actions.SetActive(playFabId != null);
            if (reasons != null) reasons.SetActive(false);
            RefreshActions();

            self.SetActive(v.Self && v.Stats);
            int lv = PlayerXp.LevelOf(v.Xp), into = PlayerXp.XpInLevel(v.Xp), need = PlayerXp.XpToNext(lv);
            xpLevel.text = "Livello " + lv;
            xpText.text = into + " / " + need + " XP";
            xpFill.anchorMax = new Vector2(Mathf.Clamp01((float)into / need), 1f);

            bool opening = !gameObject.activeSelf;
            gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(card);
            card.anchoredPosition = new Vector2(0f, -card.rect.height * 0.5f); // pivot al centro per il pop, bordo alto su Top
            if (opening)
            {
                UIAnim.FadeIn((RectTransform)transform, 0.2f);
                UIAnim.PopDialog(card);
            }
        }

        public void Hide() => gameObject.SetActive(false);

        private static void SetY(TMP_Text text, float y) =>
            text.rectTransform.anchoredPosition = new Vector2(text.rectTransform.anchoredPosition.x, y);

        private void Wire()
        {
            if (wired) return;
            wired = true;
            addButton.onClick.AddListener(() =>
            {
                string id = playFabId;
                if (id == null) return;
                addButton.interactable = false;
                // Giro Android 08/10: e' una richiesta (l'altro accetta); amici subito solo se l'aveva gia' chiesta lui.
                FriendsService.AddFriend(id, now =>
                {
                    s_Added.Add(id);
                    if (now) s_Friends.Add(id);
                    UI51Toast.Show(now ? "Ora siete amici" : "Richiesta d'amicizia inviata", UI51Toast.Kind.Success);
                    if (this != null) RefreshActions();
                }, error =>
                {
                    if (this == null) return;
                    addButton.interactable = true;
                    UI51Toast.Show(error, UI51Toast.Kind.Error);
                });
            });
            muteButton.onClick.AddListener(() =>
            {
                if (playFabId == null) return;
                EmoticonMute.SetMuted(playFabId, !EmoticonMute.IsMuted(playFabId), guest);
                RefreshActions();
            });
            reportButton.onClick.AddListener(() =>
            {
                if (playFabId != null && !s_Reported.Contains(playFabId)) ShowReasons(true);
            });
            for (int i = 0; i < reasonButtons.Length && i < ReasonIds.Length; i++)
            {
                string reason = ReasonIds[i];
                reasonButtons[i].onClick.AddListener(() => Report(reason));
            }
            if (cancelReasons != null) cancelReasons.onClick.AddListener(() => ShowReasons(false));
            if (blockButton != null) blockButton.onClick.AddListener(() =>
            {
                if (playFabId == null || guest) return;
                bool block = !BlockList.IsBlocked(playFabId);
                if (block) BlockList.RememberName(playFabId, nameText.text);
                BlockList.SetBlocked(playFabId, block);
                if (block) UI51Toast.Show("Giocatore bloccato: niente emoticon, inviti o amicizia da lui");
                RefreshActions();
                Relayout();
            });
        }

        /// <summary>Al server (moderazione): 5 account diversi da almeno 3 partite in 7 giorni sanzionano il segnalato.</summary>
        private void Report(string reason)
        {
            string id = playFabId;
            if (id == null || s_Reported.Contains(id)) return;
            SetReasonsInteractable(false);
            ModerationService.Report(id, reason, () =>
            {
                s_Reported.Add(id);
                SetReasonsInteractable(true);
                UI51Toast.Show("Segnalazione inviata. Grazie!");
                if (this != null && playFabId == id) { ShowReasons(false); RefreshActions(); }
            }, () =>
            {
                SetReasonsInteractable(true);
                UI51Toast.Show("Segnalazione non inviata. Riprova tra poco.", UI51Toast.Kind.Error);
            });
        }

        private void SetReasonsInteractable(bool on)
        {
            foreach (var b in reasonButtons) if (b != null) b.interactable = on;
        }

        /// <summary>Segnala apre i motivi al posto dei pulsanti; la scheda cresce verso il basso dal suo bordo alto.</summary>
        private void ShowReasons(bool on)
        {
            if (reasons == null) return;
            actions.SetActive(!on);
            reasons.SetActive(on);
            Relayout();
        }

        private void Relayout()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(card);
            card.anchoredPosition = new Vector2(0f, -card.rect.height * 0.5f);
        }

        private void RefreshActions()
        {
            if (playFabId == null) return;
            bool added = s_Added.Contains(playFabId), reported = s_Reported.Contains(playFabId);
            bool blocked = !guest && BlockList.IsBlocked(playFabId);
            // Bloccato: restano Segnala e Sblocca. Ospite: niente amicizia ne' blocco (il suo account dura una sessione).
            if (actionRow != null) actionRow.SetActive(!blocked);
            // Ospite che guarda: solo "Silenzia emoticon" (niente amicizia, blocco ne' segnalazione dal suo account usa e getta).
            var bottom = reportButton.transform.parent;
            bottom.gameObject.SetActive(!viewerGuest);
            var gap = bottom.parent != null ? bottom.parent.Find("Gap") : null;
            if (gap != null) gap.gameObject.SetActive(!viewerGuest);
            addButton.gameObject.SetActive(!guest && !viewerGuest && !added);
            addButton.interactable = true;
            addedLabel.SetActive(!guest && !viewerGuest && added);
            var addedText = addedLabel.GetComponentInChildren<TMP_Text>(true);
            if (addedText != null) addedText.text = s_Friends.Contains(playFabId) ? "Tra i tuoi amici" : "Richiesta inviata";
            if (blockButton != null) blockButton.gameObject.SetActive(!guest);
            if (blockLabel != null) blockLabel.text = blocked ? "Sblocca giocatore" : "Blocca giocatore";
            muteLabel.text = EmoticonMute.IsMuted(playFabId) ? "Riattiva emoticon" : "Silenzia emoticon";
            reportLabel.text = reported ? "Segnalazione inviata" : "Segnala giocatore";
            reportButton.interactable = !reported;
        }
    }
}
