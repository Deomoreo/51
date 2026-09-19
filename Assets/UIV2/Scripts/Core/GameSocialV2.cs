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

        // I3 - scelta rapida: una striscia sopra il pulsante Emoji con le emoticon equipaggiate.
        // Niente velo e niente modale: il tavolo resta visibile, si continua a giocare, un tocco invia.
        // Costruita da Tools/UIV2/Build Emoticon Quick Bar; senza, Emoji apre ancora il vecchio pannello.
        public CanvasGroup QuickBar;
        public RectTransform QuickBarAnchor;
        public Button[] QuickSlots;
        public Image[] QuickIcons;
        public TMP_Text QuickHint;
        private const float QuickSlotSize=96f,QuickGap=14f,QuickPadding=16f,QuickHintWidth=470f,QuickAutoClose=3.5f;
        // Angolo in basso a destra della striscia rispetto al centro del pulsante Emoji: allineata al
        // bordo destro di Accuso (110 px piu' a destra) e 16 px sopra i pulsanti.
        private static readonly Vector2 QuickBarCornerFromAnchor=new Vector2(154f,60f);
        private int[] quickEquipped=new int[0];
        private float quickCloseAt;
        private bool quickOpen;

        private TurnController turns;
        private RoundManager manager;
        private float lastSent=-10;
        private void Awake()
        {
            GamePresentation.OpenEmoticons+=Open;GamePresentation.EmoticonReceived+=ShowEmoticon;GamePresentation.AccusoReceived+=RemoteAccuso;
            Close.onClick.AddListener(()=>{EmoticonPanel.SetActive(false);GameAudio.PlayUi(SoundId.PopupClose);});
            for(int i=0;i<EmoticonButtons.Length;i++){int index=i;EmoticonButtons[i].onClick.AddListener(()=>Send(index));}
            EmoticonPanel.SetActive(false);foreach(var bubble in Bubbles)bubble.gameObject.SetActive(false);
            if(QuickSlots!=null)for(int k=0;k<QuickSlots.Length;k++){int slot=k;QuickSlots[k].onClick.AddListener(()=>{if(slot<quickEquipped.Length)Send(quickEquipped[slot]);});}
            if(QuickBar!=null)QuickBar.gameObject.SetActive(false);
        }
        private void Update()
        {
            if(turns==null)turns=FindObjectOfType<TurnController>();
            if(turns!=null&&turns.RoundManager!=manager)
            {
                if(manager!=null)manager.OnAccusoDeclared-=Accuso;
                manager=turns.RoundManager;if(manager!=null)manager.OnAccusoDeclared+=Accuso;
            }
            if(Input.GetKeyDown(KeyCode.Escape)){EmoticonPanel.SetActive(false);HideQuickBar();}
            UpdateQuickBar();
        }
        public void Open()
        {
            if(turns==null||turns.GameState==null||turns.GameState.RoundEnded)return;
            if(QuickBar!=null){if(quickOpen)HideQuickBar();else ShowQuickBar();return;}
            // Solo le emoticon equipaggiate (max 3), su una riga e nell'ordine scelto in Collezione.
            var equipped=CollectionCosmeticsV2.Equipped.Where(i=>i>=0&&i<EmoticonButtons.Length).ToArray();
            for(int i=0;i<EmoticonButtons.Length;i++)EmoticonButtons[i].gameObject.SetActive(false);
            for(int k=0;k<equipped.Length;k++)
            {var button=EmoticonButtons[equipped[k]];button.gameObject.SetActive(true);button.interactable=true;((RectTransform)button.transform).anchoredPosition=new Vector2((k-(equipped.Length-1)/2f)*292,-330);}
            var frame=(RectTransform)EmoticonButtons[0].transform.parent;frame.sizeDelta=new Vector2(980,560);frame.anchoredPosition=new Vector2(0,-1220);
            var legend=frame.Find("Legend");if(legend!=null)legend.gameObject.SetActive(false);
            Hint.text="Equipaggia le emoticon nella Collezione.";Hint.gameObject.SetActive(equipped.Length==0);((RectTransform)Hint.transform).anchoredPosition=new Vector2(0,-330);
            EmoticonPanel.SetActive(true);
            GameAudio.PlayUi(SoundId.PopupOpen);
        }
        public void Send(int index)
        {
            if(!CollectionCosmeticsV2.Equipped.Contains(index)||Time.unscaledTime-lastSent<1.5f)return;
            lastSent=Time.unscaledTime;EmoticonPanel.SetActive(false);HideQuickBar();
            if(GameModeService.Current.IsMultiplayer)NetworkGameController.Instance?.SendEmoticon(index);
            else ShowEmoticon(GameModeService.Current.LocalPlayerIndex,index);
        }
        private void ShowQuickBar()
        {
            quickEquipped=CollectionCosmeticsV2.Equipped.Where(i=>i>=0&&i<Sprites.Length).Take(QuickSlots.Length).ToArray();
            for(int k=0;k<QuickSlots.Length;k++)
            {
                bool used=k<quickEquipped.Length;QuickSlots[k].gameObject.SetActive(used);
                if(!used)continue;
                QuickIcons[k].sprite=Sprites[quickEquipped[k]];
                var rect=(RectTransform)QuickSlots[k].transform;
                rect.anchoredPosition=new Vector2(QuickPadding+QuickSlotSize*.5f+k*(QuickSlotSize+QuickGap),QuickPadding+QuickSlotSize*.5f);
            }
            bool empty=quickEquipped.Length==0;
            if(QuickHint!=null){QuickHint.text="Equipaggia le emoticon nella Collezione";QuickHint.gameObject.SetActive(empty);}
            float width=empty?QuickHintWidth:QuickPadding*2+quickEquipped.Length*QuickSlotSize+(quickEquipped.Length-1)*QuickGap;
            var bar=(RectTransform)QuickBar.transform;
            bar.sizeDelta=new Vector2(width,QuickSlotSize+QuickPadding*2);
            // Segue il pulsante Emoji, anche quando LocalSeatBottomShift lo abbassa sui telefoni lunghi.
            if(QuickBarAnchor!=null)bar.anchoredPosition=QuickBarAnchor.anchoredPosition+QuickBarCornerFromAnchor;
            bar.SetAsLastSibling();
            quickOpen=true;QuickBar.gameObject.SetActive(true);
            QuickBar.DOKill();bar.DOKill();
            QuickBar.alpha=0;QuickBar.DOFade(1,.12f).SetUpdate(true);
            bar.localScale=Vector3.one*.9f;bar.DOScale(1,.16f).SetEase(Ease.OutBack).SetUpdate(true);
            quickCloseAt=Time.unscaledTime+QuickAutoClose;
        }
        private void HideQuickBar()
        {
            if(QuickBar==null||!quickOpen)return;
            quickOpen=false;QuickBar.DOKill();QuickBar.transform.DOKill();
            QuickBar.DOFade(0,.1f).SetUpdate(true).OnComplete(()=>QuickBar.gameObject.SetActive(false));
        }
        private void UpdateQuickBar()
        {
            if(!quickOpen)return;
            if(Time.unscaledTime>quickCloseAt||turns?.GameState==null||turns.GameState.RoundEnded){HideQuickBar();return;}
            // Un tocco fuori chiude la striscia ma passa comunque sotto (carte, pulsanti): non ferma il gioco.
            // Il tocco su Emoji lo gestisce Open (apri/chiudi).
            if(Input.GetMouseButtonDown(0)&&!Contains(QuickBar.transform,Input.mousePosition)&&!Contains(QuickBarAnchor,Input.mousePosition))HideQuickBar();
        }
        private static bool Contains(Transform target,Vector3 screenPoint)
        {
            var rect=target as RectTransform;if(rect==null)return false;
            var canvas=rect.GetComponentInParent<Canvas>();
            var camera=canvas!=null&&canvas.renderMode!=RenderMode.ScreenSpaceOverlay?canvas.rootCanvas.worldCamera:null;
            return RectTransformUtility.RectangleContainsScreenPoint(rect,screenPoint,camera);
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
            if(!GameModeService.Current.IsLocalPlayer(player))GameAudio.Play(SoundId.Notification,sync:GameAudio.Sync.Onset);
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
            // La matta mostrata come 7 di coppe si trasforma nella carta che vale per l'accuso.
            int mattaValue=AccusiChecker.MattaValueForAccuso(hand);int mattaIndex=hand==null?-1:hand.FindIndex(c=>c.IsMatta);
            if(cv!=null&&mattaValue>0&&mattaIndex>=0&&mattaIndex<AccusoCards.Length)Impact.QueueMattaFlip(AccusoCards[mattaIndex],cv.GetSpriteForCard(new Card(hand[mattaIndex].Suit,mattaValue)));
        }
        private void OnDestroy()
        {
            GamePresentation.OpenEmoticons-=Open;GamePresentation.EmoticonReceived-=ShowEmoticon;GamePresentation.AccusoReceived-=RemoteAccuso;
            if(manager!=null)manager.OnAccusoDeclared-=Accuso;
            foreach(var b in Bubbles){b.DOKill();b.transform.DOKill();}
            if(QuickBar!=null){QuickBar.DOKill();QuickBar.transform.DOKill();}
        }
    }
}
