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
    /// Segnala; per me la barra XP. Costruita da UI51TableBuilder.BuildQuickProfile, aperta da PlayerBannerManager.OpenProfile.
    /// </summary>
    public sealed class QuickProfileCard : MonoBehaviour
    {
        /// <summary>Dati di una scheda. Level &lt;= 0 = nascosto; PlayFabId null = niente pulsanti.</summary>
        public struct View
        {
            public string Name, Team, PlayFabId;
            public Sprite Avatar;
            public int Frame, Style, Level, Games, Wins, Scope, Xp;
            public bool Stats, Self;
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
        [SerializeField] private GameObject self;
        [SerializeField] private TMP_Text xpLevel, xpText;
        [SerializeField] private RectTransform xpFill;

        // Richieste gia' partite in questa sessione: riaprendo la scheda resta "inviata".
        private static readonly HashSet<string> s_Added = new HashSet<string>(), s_Reported = new HashSet<string>();
        private string playFabId;
        private bool wired;
        private float[] nameY; // y costruite di nome e squadra

        public bool IsOpen => gameObject.activeSelf;

        public void Show(View v)
        {
            Wire();
            playFabId = v.Self ? null : v.PlayFabId;
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
                FriendsService.AddFriend(id, () => { s_Added.Add(id); RefreshActions(); }, () => addButton.interactable = true);
            });
            muteButton.onClick.AddListener(() =>
            {
                if (playFabId == null) return;
                EmoticonMute.SetMuted(playFabId, !EmoticonMute.IsMuted(playFabId));
                RefreshActions();
            });
            reportButton.onClick.AddListener(() =>
            {
                string id = playFabId;
                if (id == null || s_Reported.Contains(id)) return;
                reportButton.interactable = false;
                FriendsService.ReportPlayer(id, "Segnalazione dal tavolo", () => { s_Reported.Add(id); RefreshActions(); },
                    () => reportButton.interactable = true);
            });
        }

        private void RefreshActions()
        {
            if (playFabId == null) return;
            bool added = s_Added.Contains(playFabId), reported = s_Reported.Contains(playFabId);
            addButton.gameObject.SetActive(!added);
            addButton.interactable = true;
            addedLabel.SetActive(added);
            muteLabel.text = EmoticonMute.IsMuted(playFabId) ? "Riattiva emoticon" : "Silenzia emoticon";
            reportLabel.text = reported ? "Segnalazione inviata" : "Segnala giocatore";
            reportButton.interactable = !reported;
        }
    }
}
