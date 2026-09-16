using System.Collections.Generic;
using Project51.UIV2.Core;
using Project51.Unity;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.EditorTools
{
    public static partial class FrontendExpansionBuilder
    {
        public static void Rooms()
        {
            if(Object.FindObjectOfType<RoomFlowV2>(true)!=null)throw new System.Exception("Room flow already exists");
            var root=Canvas("OnlineFlowV2",800);var c=root.gameObject.AddComponent<RoomFlowV2>();c.Launcher=Object.FindObjectOfType<GameLaunchController>(true);
            RectTransform frame;var closes=new List<UnityEngine.UI.Button>();
            c.CreatePanel=Modal("CreateRoom",root,"CREA STANZA",out frame).gameObject;
            Label(frame,"Hint","Scegli il tavolo e invita i tuoi amici",0,190,850,100,34);
            c.Formats=new UnityEngine.UI.Button[3];string[] labels={"1 vs 1","2 vs 2","1 vs 3"};
            for(int i=0;i<3;i++)c.Formats[i]=Button(frame,"Format"+i,labels[i],(i-1)*290,390,260,105);
            c.CreateFormat=Label(frame,"SelectedFormat","",0,550,850,80,40);
            Pic(At("Cards",frame,0,770,190,250),Icon("card_back_green"));
            Label(frame,"Explanation","Il codice viene creato quando la stanza è pronta.\nPotrai aggiungere bot ai posti liberi.",0,1020,850,150,32);
            c.Create=Button(frame,"Create","CREA STANZA",0,1270,750,115,"btn_gold_long");closes.Add(Button(frame,"Cancel","ANNULLA",0,1430,520,75,"btn_gray_small"));
            c.JoinPanel=Modal("JoinRoom",root,"ENTRA IN STANZA",out frame).gameObject;
            Label(frame,"Hint","Inserisci il codice a 5 caratteri\nche ti ha inviato il tuo amico",0,260,850,140,34);
            var field=At("CodeInput",frame,0,500,760,160);Pic(field,null,new Color(0,0,0,.001f)).raycastTarget=true;
            var input=field.gameObject.AddComponent<TMP_InputField>();input.characterLimit=5;input.contentType=TMP_InputField.ContentType.Alphanumeric;input.lineType=TMP_InputField.LineType.SingleLine;
            var text=Label(field,"InputText","",0,80,740,145,65) as TextMeshProUGUI;input.textComponent=text;input.textViewport=field;input.customCaretColor=true;input.caretColor=Color.clear;text.color=Color.clear;c.CodeInput=input;
            c.CodeCells=new TMP_Text[5];for(int i=0;i<5;i++)
            {var cell=At("Cell"+i,field,(i-2)*150,80,130,145);var outline=Pic(cell,Panel,new Color32(229,179,69,255));outline.type=UnityEngine.UI.Image.Type.Sliced;outline.preserveAspect=false;
                var inside=At("Inside",cell,0,72.5f,120,135);var image=Pic(inside,Panel,new Color32(13,28,45,255));image.type=UnityEngine.UI.Image.Type.Sliced;image.preserveAspect=false;c.CodeCells[i]=Label(cell,"Character","_",0,72,120,120,64);}
            c.JoinError=Label(frame,"Error","",0,730,860,145,32);c.JoinError.color=new Color32(255,139,139,255);
            c.Paste=Button(frame,"Paste","INCOLLA CODICE",0,930,690,100);
            c.Join=Button(frame,"Join","ENTRA",0,1260,750,115,"btn_gold_long");closes.Add(Button(frame,"Cancel","ANNULLA",0,1430,520,75,"btn_gray_small"));
            c.SearchPanel=Modal("SearchMatch",root,"RICERCA PARTITA",out frame).gameObject;
            c.Spinner=At("Spinner",frame,0,370,130,180);Pic(c.Spinner,Icon("card_back_green"));
            c.SearchStatus=Label(frame,"Status","Ricerca giocatori…",0,590,850,110,44);
            c.SearchDetail=Label(frame,"Detail","",0,740,850,180,32);
            Label(frame,"Hint","Ti avviseremo quando il tavolo sarà pronto.\nPuoi annullare la ricerca in qualsiasi momento.",0,1030,830,170,32);
            closes.Add(Button(frame,"Cancel","ANNULLA",0,1390,700,90,"btn_gray_small"));
            c.LobbyPanel=Modal("Lobby",root,"STANZA PRIVATA",out frame).gameObject;c.LobbyTitle=frame.Find("Title").GetComponent<TMP_Text>();
            Label(frame,"Hint","Condividi il codice con i tuoi amici",0,165,850,65,30);
            c.LobbyCode=Label(frame,"Code","",0,290,850,130,78);c.LobbyCode.characterSpacing=18;
            c.Copy=Button(frame,"Copy","COPIA CODICE",0,440,680,95,"btn_teal");c.CopyFeedback=Label(frame,"Copied","",0,525,820,60,26);
            c.PlayerCount=Label(frame,"PlayerCount","GIOCATORI",0,615,840,65,32);
            c.PlayerNames=new TMP_Text[4];c.PlayerRoles=new TMP_Text[4];c.BotButtons=new UnityEngine.UI.Button[4];
            for(int i=0;i<4;i++)
            {var row=At("Player"+i,frame,0,730+i*128,870,114);var bg=Pic(row,Panel,new Color32(35,59,84,255));bg.type=UnityEngine.UI.Image.Type.Sliced;bg.preserveAspect=false;
                Pic(At("Avatar",row,-360,57,76,76),Icon("avatar_frame"));
                c.PlayerNames[i]=Label(row,"Name","Slot libero",-40,33,530,52,30);c.PlayerNames[i].alignment=TextAlignmentOptions.Left;
                c.PlayerRoles[i]=Label(row,"Role","In attesa…",-40,81,530,38,22);c.PlayerRoles[i].alignment=TextAlignmentOptions.Left;c.PlayerRoles[i].color=new Color32(255,211,123,255);
                c.BotButtons[i]=Button(row,"Bot","+ BOT",330,57,175,65,"btn_green_small");c.BotButtons[i].GetComponentInChildren<TMP_Text>().fontSize=25;
            }
            c.LobbyStatus=Label(frame,"Status","",0,1250,870,100,28);c.StartGameButton=Button(frame,"Start","AVVIA PARTITA",0,1360,750,105,"btn_gold_long");
            closes.Add(Button(frame,"Leave","ESCI DALLA STANZA",0,1460,560,60,"btn_gray_small"));
            c.CloseButtons=closes.ToArray();Object.FindObjectOfType<QuickSelectionPanels>(true).RoomFlow=c;
            // Preserve legacy controllers and event wiring behind a permanently hidden parent.
            var hidden=Rect("LegacyOnlineViews",root);Fill(hidden);var gate=hidden.gameObject.AddComponent<CanvasGroup>();gate.alpha=0;gate.interactable=false;gate.blocksRaycasts=false;
            foreach(var type in new[]{typeof(WaitingRoomUI),typeof(JoinRoomPopupUI),typeof(MatchmakingStatusUI)})
            {var old=Object.FindObjectOfType(type,true) as Component;if(old!=null)old.transform.SetParent(hidden,false);}
            EditorUtility.SetDirty(c);EditorUtility.SetDirty(Object.FindObjectOfType<QuickSelectionPanels>(true));
        }
    }
}
