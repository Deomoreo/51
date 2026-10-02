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
        [Tooltip("Area di tocco del mio profilo rapido: spenta mentre la scelta emoticon e' aperta (un tocco che la chiude non apre la scheda).")]
        public GameObject ProfileHit;
        public Button[] EmoticonButtons;
        public Button Close;
        public Sprite[] Sprites;
        public CanvasGroup[] Bubbles;
        public Image[] BubbleImages;
        public TMP_Text[] BubbleNames;
        public AccusoImpactV2 Impact;
        public Image[] AccusoCards;
        public TMP_Text Hint;

        // Scelta rapida (UI51 Fase 5, mockup Partita): la fila con le emoticon equipaggiate compare dentro al mio banner,
        // al posto di nome, livello e chip. Niente velo e niente modale: si continua a giocare, un tocco invia.
        // Costruita e collegata da Tools/UI51/Build Fase 5; senza, Emoji apre ancora il vecchio pannello.
        public CanvasGroup QuickBar;
        public RectTransform QuickBarAnchor;
        public Button[] QuickSlots;
        public Image[] QuickIcons;
        public TMP_Text QuickHint;
        // La mia emoticon sale dal banner in una nuvoletta (emoFly, 2,6 s); quelle degli altri prendono il posto dell'avatar.
        public RectTransform OwnFly;
        public Project51.UI51.EmoticonPlayer OwnFlyFace;
        private const float QuickAutoClose=3.5f;
        private Project51.Unity.UI.PlayerBannerManager bannerManager;
        private float quickFull,quickRight; // larghezza e bordo destro della fila come li ha messi il builder
        private int[] quickEquipped=new int[0];
        private float quickCloseAt;
        private bool quickOpen;

        private TurnController turns;
        private TableFeltRenderer felt;
        private readonly Queue<System.Action> pendingAccusi=new Queue<System.Action>();
        private RoundManager manager;
        private float lastSent=-10;
        private void Awake()
        {
            GamePresentation.OpenEmoticons+=Open;GamePresentation.EmoticonReceived+=ShowEmoticon;GamePresentation.AccusoReceived+=RemoteAccuso;
            Close.onClick.AddListener(()=>{EmoticonPanel.SetActive(false);GameAudio.PlayUi(SoundId.PopupClose);});
            for(int i=0;i<EmoticonButtons.Length;i++){int index=i;EmoticonButtons[i].onClick.AddListener(()=>Send(index));}
            EmoticonPanel.SetActive(false);foreach(var bubble in Bubbles)bubble.gameObject.SetActive(false);
            if(QuickSlots!=null)for(int k=0;k<QuickSlots.Length;k++){int slot=k;QuickSlots[k].onClick.AddListener(()=>{if(slot<quickEquipped.Length)Send(quickEquipped[slot]);});}
            if(QuickBar!=null){var bar=(RectTransform)QuickBar.transform;quickFull=bar.sizeDelta.x;quickRight=bar.anchoredPosition.x+quickFull*(1f-bar.pivot.x);QuickBar.gameObject.SetActive(false);}
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
            if(turns==null||turns.GameState==null||turns.GameState.RoundEnded||Muted())return;
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
            if(!CollectionCosmeticsV2.Equipped.Contains(index)||Time.unscaledTime-lastSent<1.5f||Muted())return;
            lastSent=Time.unscaledTime;EmoticonPanel.SetActive(false);HideQuickBar();
            if(GameModeService.Current.IsMultiplayer)NetworkGameController.Instance?.SendEmoticon(index);
            else ShowEmoticon(GameModeService.Current.LocalPlayerIndex,index);
        }
        // Emoticon spente per le segnalazioni (ModerationService): online non partono, l'avviso dice per quanto.
        private bool Muted()
        {
            if(!GameModeService.Current.IsMultiplayer||!Project51.Auth.ModerationService.IsMuted)return false;
            EmoticonPanel.SetActive(false);HideQuickBar();
            Project51.Unity.UI.UI51Toast.Show("Emoticon spente per le segnalazioni: ancora "+
                Project51.Unity.UI.UI51SuspensionView.Countdown(Project51.Auth.ModerationService.MuteSecondsLeft),Project51.Unity.UI.UI51Toast.Kind.Error);
            return true;
        }
        private void ShowQuickBar()
        {
            quickEquipped=CollectionCosmeticsV2.Equipped.Where(i=>i>=0&&i<Sprites.Length).Take(QuickSlots.Length).ToArray();
            for(int k=0;k<QuickSlots.Length;k++)
            {
                bool used=k<quickEquipped.Length;QuickSlots[k].gameObject.SetActive(used);
                if(!used)continue;
                QuickIcons[k].sprite=Sprites[quickEquipped[k]];
            }
            // Posto e misure li decide il builder; con 1-2 emoticon la fila si stringe su di loro (bordo destro fermo), cosi' il pop
            // parte dal loro centro come nel mockup. Vuota: larghezza piena per la scritta.
            int n=quickEquipped.Length;var bar=(RectTransform)QuickBar.transform;var row=QuickBar.GetComponent<HorizontalLayoutGroup>();
            float w=n==0||row==null||QuickSlots.Length==0?quickFull:Mathf.Min(quickFull,n*((RectTransform)QuickSlots[0].transform).sizeDelta.x+(n-1)*row.spacing);
            bar.sizeDelta=new Vector2(w,bar.sizeDelta.y);bar.anchoredPosition=new Vector2(quickRight-w*(1f-bar.pivot.x),bar.anchoredPosition.y);
            if(QuickHint!=null){QuickHint.text="Nessuna emoticon\nScegline in Collezione";QuickHint.gameObject.SetActive(n==0);}
            quickOpen=true;QuickBar.gameObject.SetActive(true);if(ProfileHit!=null)ProfileHit.SetActive(false);
            var own=UI51Seat(0);if(own!=null)own.SetPicking(true);
            Project51.UI51.UIAnim.Pop(bar);
            quickCloseAt=Time.unscaledTime+QuickAutoClose;
        }
        private void HideQuickBar()
        {
            if(QuickBar==null||!quickOpen)return;
            // Chiusura secca come nel mockup; spegnendosi il pop si ferma e lascia la fila intera per la prossima volta.
            quickOpen=false;QuickBar.gameObject.SetActive(false);if(ProfileHit!=null)ProfileHit.SetActive(true);
            var own=UI51Seat(0,false);if(own!=null)own.SetPicking(false); // anche a banner spento: nome e chip tornano al prossimo giro
        }
        /// <summary>Banner UI51 al posto relativo (0 io, 2 in alto); null se il posto usa ancora la vecchia grafica o (shown) e' spento.</summary>
        private Project51.UI51.PlayerBanner UI51Seat(int seat,bool shown=true)
        {
            if(bannerManager==null)bannerManager=FindObjectOfType<Project51.Unity.UI.PlayerBannerManager>();
            var banner=bannerManager!=null?bannerManager.UI51Banner(seat):null;
            return banner!=null&&(!shown||banner.isActiveAndEnabled)?banner:null;
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
            var p=PlayerAt(player);
            var name=p!=null?(p.NickName??string.Empty).Replace("<",string.Empty).Trim():string.Empty; // nome scelto dall'altro giocatore: niente tag rich text; vuoto = "Giocatore N"
            return name.Length>0?name:"Giocatore "+(player+1);
        }
        private static GameSceneInitializer seatMap;
        /// <summary>Giocatore Photon al posto assoluto player, anche inattivo nella finestra di rientro; null fuori stanza o
        /// prima che il roster sia fissato. Senza allocazioni: gira a 5 Hz per posto.</summary>
        public static Photon.Realtime.Player PlayerAt(int player)
        {
            var room=Photon.Pun.PhotonNetwork.CurrentRoom;if(room==null)return null;
            if(seatMap==null)seatMap=Object.FindObjectOfType<GameSceneInitializer>();if(seatMap==null)return null;
            foreach(var p in room.Players.Values)if(seatMap.GetPlayerIndexForActor(p.ActorNumber)==player)return p;
            return null;
        }
        /// <summary>Id PlayFab pubblicato dal giocatore al posto player (AuthBootstrapper.LookProps); null per bot, ospiti, fuori stanza.</summary>
        public static string PlayFabIdAt(int player)
        {
            var p=PlayerAt(player);if(p==null)return null;
            Project51.UIV2.Data.ProfileCosmetics.ReadStats(p.CustomProperties,out _,out _,out _,out string id);return id;
        }
        private int Seat(int player)
        {
            if(turns==null)turns=FindObjectOfType<TurnController>(); // un'emoticon arrivata prima del primo Update: 1v1, non 4 posti
            int count=turns?.GameState?.NumPlayers??4;int relative=(player-GameModeService.Current.LocalPlayerIndex+count)%count;
            return count==2&&relative==1?2:relative;
        }
        public void ShowEmoticon(int player,int index)
        {
            if(index<0||index>=Sprites.Length)return;
            // "Silenzia emoticon" del profilo rapido: niente suono ne' nuvoletta per quel giocatore (questo dispositivo).
            if(!GameModeService.Current.IsLocalPlayer(player)&&Project51.Auth.EmoticonMute.IsMuted(PlayFabIdAt(player)))return;
            int seat=Seat(player);var bubble=Bubbles[seat];
            if(!GameModeService.Current.IsLocalPlayer(player))GameAudio.Play(SoundId.Notification,sync:GameAudio.Sync.Onset);
            // UI51: la mia sale dal banner e sparisce (una nuova riparte da capo), quella di un altro sta 3,2 s al posto del suo avatar.
            if(seat==0&&OwnFly!=null)
            {
                OwnFly.gameObject.SetActive(true);if(OwnFlyFace!=null)OwnFlyFace.SetEmoticon(index);
                var fly=Project51.UI51.UIAnim.EmoticonBubble(OwnFly);var flyGo=OwnFly.gameObject;
                if(fly!=null)fly.OnComplete(()=>flyGo.SetActive(false));
                return;
            }
            var ui51=UI51Seat(seat);if(ui51!=null){ui51.ShowEmoticon(index);return;}
            bubble.DOKill();bubble.gameObject.SetActive(true);bubble.alpha=1;BubbleImages[seat].sprite=Sprites[index];BubbleNames[seat].text=PlayerName(player);
            // Emoticon animate (Tools/UIV2/Build Animated Emoticons): stato "E"+indice. Senza stato o con grafica
            // ridotta l'Animator resta spento e la nuvoletta mostra il frame 0.
            var animator=BubbleImages[seat].GetComponent<Animator>();
            if(animator!=null){int state=Animator.StringToHash("E"+index);animator.enabled=!GamePreferences.ReducedGraphics;
                if(animator.enabled&&animator.HasState(0,state))animator.Play(state,0,0);else animator.enabled=false;}
            bubble.transform.DOKill();bubble.transform.localScale=Vector3.one*.65f; bubble.transform.DOScale(1,.2f).SetEase(Ease.OutBack);
            bubble.DOFade(0,.3f).SetDelay(2.3f).OnComplete(()=>bubble.gameObject.SetActive(false));
        }
        private void RemoteAccuso(int player,int type)
        {if(turns?.GameState!=null&&player>=0&&player<turns.GameState.NumPlayers)Accuso(player,(AccusoType)type,turns.GameState.Players[player].Hand);}
        public void Accuso(int player,AccusoType type,List<Card> hand)
        {
            // Piu' accusi insieme (fine finestra, rete): uno alla volta, ognuno col suo nome, punti e carte. Il gioco aspetta tutti.
            if(Impact.Revealing)
            {
                pendingAccusi.Enqueue(()=>Accuso(player,type,hand));
                GamePresentation.MarkBusy(GamePreferences.Scaled(2.6f)*(pendingAccusi.Count+1)+0.1f);
                return;
            }
            var cv=FindObjectOfType<CardViewManager>();
            // Tavolo UI51 (mockup Partita): nel 1v1 e per il mio accuso le carte si girano in mano; sotto la scritta vanno solo
            // quelle di un altro nei 4 giocatori. Le carte UI51 hanno la cornice: si spegne la carta intera.
            bool ui51=Impact.IsUI51;int count=turns?.GameState?.NumPlayers??2;
            bool showCards=!ui51||count>2&&!GameModeService.Current.IsLocalPlayer(player);
            for(int i=0;i<AccusoCards.Length;i++)
            {
                bool show=showCards&&hand!=null&&i<hand.Count;(ui51?AccusoCards[i].transform.parent:AccusoCards[i].transform).gameObject.SetActive(show);
                if(show&&cv!=null)AccusoCards[i].sprite=cv.GetSpriteForCard(hand[i]);
            }
            string name=type==AccusoType.Decino?"DECINO!":type==AccusoType.Cirulla?"CIRULLA!":"ACCUSO!";
            var cards=ui51?new Transform[0]:Resources.FindObjectsOfTypeAll<CardView>().Where(v=>v.gameObject.scene.IsValid()&&v.gameObject.activeInHierarchy).Select(v=>v.transform).ToArray();
            float unit=0f;
            if(ui51)
            {
                name=name.TrimEnd('!');
                int points=(turns?.GameState?.Rules??MatchRules.Default).AccusoPoints(type==AccusoType.Decino?10:3);
                // Centro del tavolo sullo schermo e px per unita' del mockup, ridotti sui telefoni bassi (RevealScale).
                Vector2 at=default;float px=0f;var cam=Camera.main;
                if(cv!=null&&cam!=null&&cv.TryGetTableRim(out var rim,out unit))
                {
                    Vector2 mid=cam.WorldToScreenPoint(rim.center);at=mid;
                    px=(cam.WorldToScreenPoint(rim.center+Vector2.right*unit).x-mid.x)*AccusoImpactV2.RevealScale(rim.height/unit);
                }
                Impact.Stage(PlayerName(player)+" · <color=#FCE29A>+"+points+"</color>",at,px,count>2,showCards);
            }
            Impact.Play(ui51?name:PlayerName(player)+" · "+name,cards, () =>
                GameFeedback.ForPlayer(FeedbackKind.Accuso, player, new Vector2(.5f, .5f)),
                ()=>{if(pendingAccusi.Count>0)pendingAccusi.Dequeue()();});
            if(ui51&&unit>0f)
            {
                if(felt==null)felt=FindObjectOfType<TableFeltRenderer>();
                if(felt!=null)Project51.UI51.UIAnim.ShakeWorld(felt.transform,unit,.3f);
            }
            // La matta mostrata come 7 di coppe si trasforma nella carta che vale per l'accuso.
            int mattaValue=AccusiChecker.MattaValueForAccuso(hand);int mattaIndex=hand==null?-1:hand.FindIndex(c=>c.IsMatta);
            if(showCards&&cv!=null&&mattaValue>0&&mattaIndex>=0&&mattaIndex<AccusoCards.Length)Impact.QueueMattaFlip(AccusoCards[mattaIndex],cv.GetSpriteForCard(new Card(hand[mattaIndex].Suit,mattaValue)));
        }
        private void OnDestroy()
        {
            GamePresentation.OpenEmoticons-=Open;GamePresentation.EmoticonReceived-=ShowEmoticon;GamePresentation.AccusoReceived-=RemoteAccuso;
            if(manager!=null)manager.OnAccusoDeclared-=Accuso;
            foreach(var b in Bubbles){b.DOKill();b.transform.DOKill();}
            if(QuickBar!=null){QuickBar.DOKill();QuickBar.transform.DOKill();}
            if(OwnFly!=null)DOTween.Kill(OwnFly);
            pendingAccusi.Clear();
        }
    }
}
