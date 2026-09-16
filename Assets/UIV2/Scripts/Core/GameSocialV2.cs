using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Project51.Core;
using Project51.Networking;
using Project51.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UIV2.Core
{
    public sealed class GameSocialV2 : MonoBehaviour
    {
        public GameObject EmoticonPanel;
        public Button[] EmoticonButtons;
        public Button Close;
        public Sprite[] Sprites;
        public CanvasGroup[] Bubbles;
        public Image[] BubbleImages;
        public TMP_Text[] BubbleNames;
        public AccusoImpactV2 Impact;
        public Image[] AccusoCards;
        public TMP_Text Hint;
        private TurnController turns;
        private RoundManager manager;
        private float lastSent=-10;
        private void Awake()
        {
            GamePresentation.OpenEmoticons+=Open;GamePresentation.EmoticonReceived+=ShowEmoticon;GamePresentation.AccusoReceived+=RemoteAccuso;
            Close.onClick.AddListener(()=>EmoticonPanel.SetActive(false));
            for(int i=0;i<EmoticonButtons.Length;i++){int index=i;EmoticonButtons[i].onClick.AddListener(()=>Send(index));}
            EmoticonPanel.SetActive(false);foreach(var bubble in Bubbles)bubble.gameObject.SetActive(false);
        }
        private void Update()
        {
            if(turns==null)turns=FindObjectOfType<TurnController>();
            if(turns!=null&&turns.RoundManager!=manager)
            {
                if(manager!=null)manager.OnAccusoDeclared-=Accuso;
                manager=turns.RoundManager;if(manager!=null)manager.OnAccusoDeclared+=Accuso;
            }
            if(Input.GetKeyDown(KeyCode.Escape))EmoticonPanel.SetActive(false);
        }
        public void Open()
        {
            if(turns==null||turns.GameState==null||turns.GameState.RoundEnded)return;
            var equipped=CollectionCosmeticsV2.Equipped;
            for(int i=0;i<EmoticonButtons.Length;i++)EmoticonButtons[i].interactable=equipped.Contains(i);
            Hint.text=equipped.Length==0?"Equipaggia le emoticon nella Collezione.":"Usa le emoticon equipaggiate in Collezione";
            EmoticonPanel.SetActive(true);
        }
        public void Send(int index)
        {
            if(!CollectionCosmeticsV2.Equipped.Contains(index)||Time.unscaledTime-lastSent<1.5f)return;
            lastSent=Time.unscaledTime;EmoticonPanel.SetActive(false);
            if(GameModeService.Current.IsMultiplayer)NetworkGameController.Instance?.SendEmoticon(index);
            else ShowEmoticon(GameModeService.Current.LocalPlayerIndex,index);
        }
        public static string PlayerName(int player)
        {
            if(GameModeService.Current.IsLocalPlayer(player))return "Tu";
            if(GameModeService.Current.IsBotPlayer(player))return "Bot "+(player+1);
            var init=Object.FindObjectOfType<GameSceneInitializer>();
            if(init!=null)foreach(var p in Photon.Pun.PhotonNetwork.PlayerList)if(init.GetPlayerIndexForActor(p.ActorNumber)==player)return p.NickName;
            return "Giocatore "+(player+1);
        }
        private int Seat(int player)
        {
            int count=turns?.GameState?.NumPlayers??4;int relative=(player-GameModeService.Current.LocalPlayerIndex+count)%count;
            return count==2&&relative==1?2:relative;
        }
        public void ShowEmoticon(int player,int index)
        {
            if(index<0||index>=Sprites.Length)return;int seat=Seat(player);var bubble=Bubbles[seat];
            bubble.DOKill();bubble.gameObject.SetActive(true);bubble.alpha=1;BubbleImages[seat].sprite=Sprites[index];BubbleNames[seat].text=PlayerName(player);
            bubble.transform.DOKill();bubble.transform.localScale=Vector3.one*.65f; bubble.transform.DOScale(1,.2f).SetEase(Ease.OutBack);
            bubble.DOFade(0,.3f).SetDelay(2.3f).OnComplete(()=>bubble.gameObject.SetActive(false));
        }
        private void RemoteAccuso(int player,int type)
        {if(turns?.GameState!=null&&player>=0&&player<turns.GameState.NumPlayers)Accuso(player,(AccusoType)type,turns.GameState.Players[player].Hand);}
        public void Accuso(int player,AccusoType type,List<Card> hand)
        {
            var cv=FindObjectOfType<CardViewManager>();
            for(int i=0;i<AccusoCards.Length;i++)
            {
                bool show=hand!=null&&i<hand.Count;AccusoCards[i].gameObject.SetActive(show);
                if(show&&cv!=null)AccusoCards[i].sprite=cv.GetSpriteForCard(hand[i]);
            }
            string name=type==AccusoType.Decino?"DECINO!":type==AccusoType.Cirulla?"CIRULLA!":"ACCUSO!";
            var cards=Resources.FindObjectsOfTypeAll<CardView>().Where(v=>v.gameObject.scene.IsValid()&&v.gameObject.activeInHierarchy).Select(v=>v.transform).ToArray();
            Impact.Play(PlayerName(player)+" · "+name,cards);
        }
        private void OnDestroy()
        {
            GamePresentation.OpenEmoticons-=Open;GamePresentation.EmoticonReceived-=ShowEmoticon;GamePresentation.AccusoReceived-=RemoteAccuso;
            if(manager!=null)manager.OnAccusoDeclared-=Accuso;
            foreach(var b in Bubbles){b.DOKill();b.transform.DOKill();}
        }
    }
}
