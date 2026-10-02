using System.Collections.Generic;
using DG.Tweening;
using Project51.UI51;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UI
{
    /// <summary>
    /// Sala privata (mockup SalaPrivata e SalaPrivataOspite) dentro LobbyHostPanel / LobbyGuestPanel di OnlineFlowV2: testata con
    /// indietro, codice a tessere con Copia / Condividi, "AL TAVOLO n / N" coi posti a griglia (HOST, BOT, posto libero),
    /// INVITA AMICI ONLINE e in fondo AVVIA PARTITA / "IN ATTESA DI N GIOCATORI" (host) o "Aspettiamo che X avvii la partita"
    /// (ospite). Indietro chiede "Uscire dalla stanza?"; Esci e' uno dei CloseButtons di RoomFlowV2. I dati li porta RoomFlowV2;
    /// costruita da UI51MatchBuilder (una per pannello, host acceso solo in quello dell'host).
    /// </summary>
    public sealed class UI51PrivateRoomView : MonoBehaviour
    {
        // Posti a griglia alti 92 a stacco 10 da top 308: gli amici iniziano 22 sotto l'ultima riga (mockup: 524 o 422).
        const float FriendsTopFour = 524f, FriendsTopTwo = 422f;
        const int FriendRowsFour = 3, FriendRowsTwo = 5; // quante righe da 56 stanno sopra il pulsante in fondo

        [SerializeField] private bool host;
        [SerializeField] private TMP_Text subtitle;
        [SerializeField] private Button back;
        [SerializeField] private TMP_Text[] codeChars = new TMP_Text[0];
        [SerializeField] private Button copy, share;
        [SerializeField] private TMP_Text copyLabel, countLabel;
        [SerializeField] private UI51SeatCard[] seats = new UI51SeatCard[0]; // righe da due
        [SerializeField] private GameObject secondRow;
        [SerializeField] private RectTransform friendsBlock;
        [SerializeField] private UI51FriendItem[] friendRows = new UI51FriendItem[0];
        [SerializeField] private GameObject friendsPanel, noFriends;
        [SerializeField] private UI51FriendsView friends;
        [SerializeField] private Button start;
        [SerializeField] private GameObject waiting, guestWait;
        [SerializeField] private TMP_Text waitingLabel, guestLabel;
        [SerializeField] private UI51Shape guestDot;
        [SerializeField] private GameObject leaveDialog;
        [SerializeField] private RectTransform leaveCard;
        [SerializeField] private TMP_Text leaveText;
        [SerializeField] private Button stay, scrim, exit;
        [SerializeField] private Sprite[] portraits = new Sprite[0];

        public Button Copy => copy;
        public Button Share => share;
        public Button StartButton => start;
        public Button Exit => exit;
        public UI51SeatCard[] Seats => seats;

        // ponytail: ritratto per posto (come al tavolo): l'avatar scelto da ognuno non e' pubblicato in rete.
        public Sprite Portrait(int slot) => portraits.Length > 0 ? portraits[slot % portraits.Length] : null;

        private readonly List<string> friendIds = new List<string>();
        private string format = "";
        private bool ready;
        private Tween copied;

        private void Awake()
        {
            back.onClick.AddListener(AskLeave);
            stay.onClick.AddListener(CloseLeave);
            scrim.onClick.AddListener(CloseLeave);
            exit.onClick.AddListener(CloseLeave);
            for (int i = 0; i < friendRows.Length; i++)
            {
                int index = i;
                friendRows[i].invite.onClick.AddListener(() => Invite(index));
            }
        }

        private void OnEnable()
        {
            leaveDialog.SetActive(false);
            ready = false;
            start.interactable = true; // RoomFlowV2 lo spegne al tocco su AVVIA PARTITA
            UIAnim.Pulse(guestDot, 9f, 1.6f, 0.6f);
        }

        /// <summary>"Copiato!" sul pulsante per 1,5 s (mockup).</summary>
        public void ShowCopied()
        {
            copied?.Kill();
            copyLabel.text = "Copiato!";
            copied = DOVirtual.DelayedCall(1.5f, () => copyLabel.text = "Copia", true).SetLink(gameObject);
        }

        /// <summary>
        /// mode = "2 VS 2"...; code = codice della stanza ("" finche' Photon non l'ha); seatInfo[i] null = posto libero;
        /// formatName = formato per l'invito agli amici ("2 vs 2"); roomNames = nomi di chi e' nella stanza (amici "Entrato").
        /// </summary>
        public void Bind(string mode, string code, UI51SeatCard.Info[] seatInfo, int total, bool canStart, string hostName,
            string formatName, ICollection<string> roomNames)
        {
            subtitle.text = mode + " · " + (host ? "hai creato tu la stanza" : "sei entrato con il codice");
            for (int i = 0; i < codeChars.Length; i++) codeChars[i].text = i < code.Length ? code[i].ToString() : "";
            copy.interactable = share.interactable = code.Length > 0;

            int seated = 0;
            for (int i = 0; i < seats.Length; i++)
            {
                if (i >= total) { seats[i].Hide(); continue; }
                var info = seatInfo != null && i < seatInfo.Length ? seatInfo[i] : null;
                if (info != null) seated++;
                seats[i].Bind(info, host ? "Tocca per un bot" : "Posto libero");
            }
            secondRow.SetActive(total > 2);
            countLabel.text = seated + " / " + total;

            int missing = total - seated;
            start.gameObject.SetActive(host && canStart);
            if (host && canStart && !ready) UIAnim.Pop((RectTransform)start.transform, 0.8f, 1.04f, 0.3f);
            ready = host && canStart;
            waiting.SetActive(host && !canStart);
            waitingLabel.text = "IN ATTESA DI " + (missing == 1 ? "1 GIOCATORE" : missing + " GIOCATORI");
            guestWait.SetActive(!host);
            guestLabel.text = "Aspettiamo che " + (string.IsNullOrEmpty(hostName) ? "l'host" : hostName) + " avvii la partita";

            format = formatName;
            BindFriends(total > 2, roomNames);
        }

        private void BindFriends(bool four, ICollection<string> roomNames)
        {
            var p = friendsBlock.anchoredPosition;
            friendsBlock.anchoredPosition = new Vector2(p.x, -(four ? FriendsTopFour : FriendsTopTwo));
            var list = friends != null ? friends.RoomFriends(roomNames) : new List<Project51.Auth.FriendEntry>();
            int rows = Mathf.Min(list.Count, four ? FriendRowsFour : FriendRowsTwo, friendRows.Length);
            friendsPanel.SetActive(rows > 0);
            noFriends.SetActive(rows == 0);
            for (int i = 0; i < friendRows.Length; i++)
            {
                var row = friendRows[i];
                row.gameObject.SetActive(i < rows);
                if (i >= rows) continue;
                var f = list[i];
                bool joined = roomNames.Contains(f.DisplayName);
                bool sent = friends.WasInvited(f.PlayFabId);
                row.title.text = f.DisplayName;
                if (i >= friendIds.Count || friendIds[i] != f.PlayFabId) row.avatar.SetAvatar(friends.Portrait(f.PlayFabId)); // ritaglio solo se cambia
                if (i < friendIds.Count) friendIds[i] = f.PlayFabId; else friendIds.Add(f.PlayFabId);
                row.invite.gameObject.SetActive(!joined && !sent);
                row.invited.SetActive(!joined && sent);
                row.busy.SetActive(joined);
            }
        }

        private void Invite(int index)
        {
            if (friends == null || index >= friendIds.Count) return;
            friends.InviteToRoom(friendIds[index], format);
        }

        private void AskLeave()
        {
            leaveText.text = host
                ? "Sei l’host: uscendo, la stanza passa a un altro giocatore. Se sei solo, si chiude."
                : "Potrai rientrare con lo stesso codice finché la partita non inizia.";
            leaveDialog.SetActive(true);
            UIAnim.PopDialog(leaveCard);
        }

        private void CloseLeave() => leaveDialog.SetActive(false);
    }
}
