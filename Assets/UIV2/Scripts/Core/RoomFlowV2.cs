using System.Linq;
using DG.Tweening;
using Photon.Pun;
using Project51.Core;
using Project51.Networking;
using Project51.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Hashtable=ExitGames.Client.Photon.Hashtable;

namespace Project51.UIV2.Core
{
    public sealed class RoomFlowV2 : MonoBehaviour
    {
        public GameLaunchController Launcher;
        public GameObject CreatePanel,JoinPanel,SearchPanel,LobbyPanel;
        public Button Create,Join,Paste,Copy,StartGameButton;
        public Button[] CloseButtons,Formats,BotButtons;
        public TMP_InputField CodeInput;
        public TMP_Text[] CodeCells,PlayerNames,PlayerRoles;
        public TMP_Text[] SearchPlayers;
        public TMP_Text JoinError,SearchStatus,SearchDetail,LobbyTitle,LobbyCode,LobbyStatus,PlayerCount,CreateFormat,CopyFeedback;
        public RectTransform Spinner;
        public bool IsOpen=>CreatePanel.activeSelf||JoinPanel.activeSelf||SearchPanel.activeSelf||LobbyPanel.activeSelf;
        private MatchmakingManager manager;
        private GameFormat format=GameFormat.FourPlayers;
        private bool joining,busy;
        private float started,nextRefresh;
        private void Awake()
        {
            Create.onClick.AddListener(CreateRoom);Join.onClick.AddListener(JoinRoom);Paste.onClick.AddListener(()=>CodeInput.text=NormalizeCode(GUIUtility.systemCopyBuffer));
            Copy.onClick.AddListener(()=>{GUIUtility.systemCopyBuffer=PhotonNetwork.InRoom?PhotonNetwork.CurrentRoom.Name:LobbyCode.text;CopyFeedback.text="Codice copiato";});
            StartGameButton.onClick.AddListener(StartMatch);
            foreach(var b in CloseButtons)b.onClick.AddListener(Cancel);
            for(int i=0;i<Formats.Length;i++){int index=i;Formats[i].onClick.AddListener(()=>SelectFormat(index));}
            for(int i=0;i<BotButtons.Length;i++){int index=i;BotButtons[i].onClick.AddListener(()=>ToggleBot(index));}
            CodeInput.onValueChanged.AddListener(EditCode);CodeInput.onSubmit.AddListener(_=>JoinRoom());
            HideAll();
        }
        public static string NormalizeCode(string code)=>new string((code??"").Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).Take(5).ToArray());
        public static bool IsValidCode(string code)=>code!=null&&code.Length==5&&code.All(c=>c>='A'&&c<='Z'||c>='0'&&c<='9');
        public void OpenCreate(){joining=false;busy=false;SelectFormat(0);Show(CreatePanel);}
        public void OpenJoin(){joining=true;busy=false;Show(JoinPanel);CodeInput.text="";EditCode("");CodeInput.ActivateInputField();}
        private void SelectFormat(int index)
        {
            format=new[]{GameFormat.OneVsOne,GameFormat.TwoVsTwo,GameFormat.FourPlayers}[index];
            CreateFormat.text="Formato: "+new[]{"1 vs 1","2 vs 2","1 vs 3"}[index];
            for(int i=0;i<Formats.Length;i++)Formats[i].image.color=i==index?new Color(.45f,1,.83f):Color.white;
        }
        private MatchConfig Config()
        {
            var c=new MatchConfig{Intent=MatchIntent.PrivateRoom,Format=format,DeckBackId=CardDecks.SelectedId,Rules=MatchRules.Default.Clone()};
            if(format==GameFormat.OneVsOne){c.Rules.CappottoEndsGameImmediately=false;c.Rules.CappottoBonusPoints=0;}
            return c;
        }
        private void CreateRoom(){if(busy)return;busy=true;joining=false;started=Time.unscaledTime;Launcher.CreatePrivateRoom(Config());}
        private void JoinRoom()
        {
            if(busy)return;string code=NormalizeCode(CodeInput.text);
            if(!IsValidCode(code)){JoinError.text="Inserisci tutti i 5 caratteri del codice.";return;}
            busy=true;joining=true;started=Time.unscaledTime;Launcher.JoinPrivateRoom(code,Config());
        }
        private void EditCode(string value)
        {
            string code=NormalizeCode(value);if(code!=value)CodeInput.SetTextWithoutNotify(code);
            for(int i=0;i<CodeCells.Length;i++)CodeCells[i].text=i<code.Length?code[i].ToString():"_";
            Join.interactable=IsValidCode(code)&&!busy;JoinError.text="";
        }
        private void Subscribe()
        {
            if(manager!=null||MatchmakingManager.Instance==null)return;manager=MatchmakingManager.Instance;
            manager.OnStateChanged+=State;manager.OnError+=Error;manager.OnRoomCreated+=Created;manager.OnRoomJoined+=Joined;
        }
        private void State(MatchmakingState state)
        {
            if(state==MatchmakingState.Idle){busy=false;return;}
            if(state==MatchmakingState.InWaitingRoom){Joined();return;}
            if(state==MatchmakingState.Connecting){started=Time.unscaledTime;busy=true;}
            Show(SearchPanel);
            SearchStatus.text=state==MatchmakingState.Starting?"Partita trovata!":state==MatchmakingState.CreatingRoom?"Creazione stanza…":state==MatchmakingState.JoiningRoom?"Ingresso nella stanza…":state==MatchmakingState.Connecting?"Connessione…":"Ricerca giocatori…";
        }
        private void Created(string code){Joined();}
        private void Joined(){busy=false;CopyFeedback.text="";Show(LobbyPanel);RefreshPlayers();}
        public void Error(string error)
        {
            busy=false;
            if(joining){Show(JoinPanel);JoinError.text=error;Join.interactable=IsValidCode(CodeInput.text);}
            else {Show(SearchPanel);SearchStatus.text="Connessione non riuscita";SearchDetail.text=error;}
        }
        public void Cancel()
        {
            if(manager!=null&&(busy||PhotonNetwork.InRoom))manager.Cancel();busy=false;HideAll();
        }
        private void Show(GameObject panel)
        {
            if(panel.activeSelf)return;HideAll();panel.SetActive(true);var group=panel.GetComponent<CanvasGroup>();group.alpha=0;group.DOFade(1,.2f).SetUpdate(true);
        }
        private void HideAll(){foreach(var panel in new[]{CreatePanel,JoinPanel,SearchPanel,LobbyPanel}){panel.GetComponent<CanvasGroup>().DOKill();panel.SetActive(false);}}
        private int BotMask=>PhotonNetwork.InRoom&&PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("ui_bots",out var value)?(int)value:0;
        private void ToggleBot(int index)
        {
            if(!PhotonNetwork.InRoom||!PhotonNetwork.IsMasterClient||index<PhotonNetwork.CurrentRoom.PlayerCount)return;
            PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable{{"ui_bots",BotMask^(1<<index)}});
        }
        private bool CanStart()
        {
            if(!PhotonNetwork.InRoom||!PhotonNetwork.IsMasterClient)return false;
            for(int i=PhotonNetwork.CurrentRoom.PlayerCount;i<PhotonNetwork.CurrentRoom.MaxPlayers;i++)if((BotMask&(1<<i))==0)return false;
            return true;
        }
        private void StartMatch(){if(CanStart()){StartGameButton.interactable=false;manager.StartGame();}}
        public void RefreshPlayers()
        {
            if(!PhotonNetwork.InRoom)return;
            var room=PhotonNetwork.CurrentRoom;var players=PhotonNetwork.PlayerList.OrderBy(p=>p.ActorNumber).ToArray();
            LobbyTitle.text=PhotonNetwork.IsMasterClient?"STANZA PRIVATA":"SALA D'ATTESA";LobbyCode.text=room.Name;
            PlayerCount.text="GIOCATORI "+players.Length+" / "+room.MaxPlayers;
            for(int i=0;i<PlayerNames.Length;i++)
            {
                bool exists=i<room.MaxPlayers;PlayerNames[i].transform.parent.gameObject.SetActive(exists);if(!exists)continue;
                bool human=i<players.Length,bot=!human&&(BotMask&(1<<i))!=0;
                PlayerNames[i].text=human?(players[i].IsLocal?"Tu · ":"")+players[i].NickName:bot?"Bot "+i:"Slot libero";
                PlayerRoles[i].text=human?(players[i].IsMasterClient?"HOST":"PRONTO"):bot?"ALLENAMENTO":"In attesa…";
                BotButtons[i].gameObject.SetActive(!human&&PhotonNetwork.IsMasterClient);BotButtons[i].GetComponentInChildren<TMP_Text>().text=bot?"− BOT":"+ BOT";
            }
            StartGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);StartGameButton.interactable=CanStart();
            LobbyStatus.text=!PhotonNetwork.IsMasterClient?"In attesa che l'host avvii la partita…":CanStart()?"Tavolo pronto: puoi avviare la partita.":"Attendi i giocatori o aggiungi bot agli slot liberi.";
        }
        private void Update()
        {
            Subscribe();if(Input.GetKeyDown(KeyCode.Escape)&&IsOpen)Cancel();
            if(SearchPanel.activeSelf&&busy)
            {
                Spinner.Rotate(0,0,-130*Time.unscaledDeltaTime);
                int seconds=(int)(Time.unscaledTime-started);int count=PhotonNetwork.InRoom?PhotonNetwork.CurrentRoom.PlayerCount:0;
                int max=manager?.CurrentConfig?.PlayerCount??4;
                SearchDetail.text="Giocatori "+count+" / "+max+"   ·   "+(seconds/60).ToString("00")+":"+(seconds%60).ToString("00");
                var players=PhotonNetwork.InRoom?PhotonNetwork.PlayerList.OrderBy(p=>p.ActorNumber).ToArray():new Photon.Realtime.Player[0];
                for(int i=0;SearchPlayers!=null&&i<SearchPlayers.Length;i++)
                {SearchPlayers[i].transform.parent.gameObject.SetActive(i<max);SearchPlayers[i].text=i<players.Length?players[i].NickName+(players[i].IsMasterClient?" · HOST":" · PRONTO"):"In attesa…";}
                if(seconds>45&&!PhotonNetwork.InRoom){manager?.Cancel();Error("La connessione sta impiegando troppo tempo. Riprova dalla Home.");}
            }
            if(LobbyPanel.activeSelf&&Time.unscaledTime>nextRefresh){nextRefresh=Time.unscaledTime+.2f;RefreshPlayers();}
        }
        private void OnDestroy(){if(manager==null)return;manager.OnStateChanged-=State;manager.OnError-=Error;manager.OnRoomCreated-=Created;manager.OnRoomJoined-=Joined;}
    }
}
