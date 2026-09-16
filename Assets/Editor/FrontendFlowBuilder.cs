using System.Linq;
using Project51.UIV2.Core;
using Project51.UIV2.Screens;
using Project51.Unity;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Project51.EditorTools
{
    // Explicit migration, invoked in Edit Mode with MainMenu and reference HomeScreen loaded.
    public static class FrontendFlowBuilder
    {
        private const string Art = "Assets/UI/Sprites/DragonsHoard/sprites_unity/sprites_unity/";
        private static Sprite Icon(string name) => AssetDatabase.LoadAllAssetsAtPath(Art + "Icons.png").OfType<Sprite>().First(s => s.name == name);
        private static RectTransform Rect(string name, Transform parent)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false); return r;
        }
        private static void Fill(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
        private static RectTransform At(string name, Transform parent, float x, float y, float w, float h)
        {
            var r = Rect(name, parent); r.anchorMin = r.anchorMax = new Vector2(.5f, 1); r.pivot = new Vector2(.5f,.5f);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w,h); return r;
        }
        private static Image Image(RectTransform r, Sprite sprite = null)
        {
            var image = r.gameObject.AddComponent<Image>(); image.sprite = sprite; image.raycastTarget = false;
            image.preserveAspect = sprite != null; return image;
        }
        private static TMP_Text Text(RectTransform r, string value, float size = 34)
        {
            var text = r.gameObject.AddComponent<TextMeshProUGUI>(); text.font = TMP_Settings.defaultFontAsset;
            text.text = value; text.fontSize = size; text.alignment = TextAlignmentOptions.Center;
            text.color = new Color32(255,240,208,255); text.raycastTarget = false;
            return text;
        }
        private static Button Button(Transform parent, string name, string label, string sprite, float y, float w=780, float h=120, float x=0)
        {
            var r=At(name,parent,x,y,w,h); var image=Image(r,Icon(sprite)); image.preserveAspect=false; image.type=UnityEngine.UI.Image.Type.Sliced; image.raycastTarget=true;
            var button=r.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var t=Rect("Label",r);Fill(t);var text=Text(t,label,36);text.fontStyle=FontStyles.Bold;
            text.outlineColor=new Color32(23,31,43,255);text.outlineWidth=.2f;
            return button;
        }
        private static void Set(Object target, string field, Object value)
        {
            var so=new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static T Main<T>() where T:Component => Object.FindObjectsOfType<T>(true).First(x=>x.gameObject.scene.name=="MainMenu");
        private static RectTransform CanvasRoot(string name,int order)
        {
            var r=Rect(name,null);var canvas=r.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=order;
            var scaler=r.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1920);scaler.matchWidthOrHeight=.5f;
            r.gameObject.AddComponent<GraphicRaycaster>();return r;
        }
        private static RectTransform DesignArea(Transform parent)
        {
            var r=Rect("DesignArea",parent);Fill(r);
            r.gameObject.AddComponent<DesignCanvasFit>();
            return r;
        }
        private static void Background(Transform parent)
        {
            const string path="Assets/UIV2/Art/FlowBackdrop.mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("UIV2/FlowBackdrop"));AssetDatabase.CreateAsset(mat,path);}
            var r=Rect("Background",parent);Fill(r);var image=Image(r);image.material=mat;image.raycastTarget=true;
        }
        private static AnimatedModalV2 CloneModal(string sourceType, string name, UIV2Root root)
        {
            var source=Object.FindObjectsOfType<MonoBehaviour>(true).First(x=>x.gameObject.scene.name=="HomeScreen"&&x.GetType().Name==sourceType);
            var sourceRoot=(GameObject)new SerializedObject(source).FindProperty("panelRoot").objectReferenceValue;
            var go=Object.Instantiate(sourceRoot,root.ModalHost,false);go.name=name;
            foreach(var group in go.GetComponentsInChildren<SelectableToggleGroup>(true))Object.DestroyImmediate(group);
            var modal=go.AddComponent<AnimatedModalV2>();modal.Group=go.GetComponent<CanvasGroup>();modal.Frame=go.transform.Find("PanelFrame").GetComponent<RectTransform>();
            modal.CloseButton=modal.Frame.Find("CloseButton").GetComponent<Button>();modal.Dimmer=go.transform.Find("DimBackground").GetComponent<Button>();
            modal.Group.alpha=1;modal.Group.blocksRaycasts=true;modal.Group.interactable=true;go.SetActive(false);return modal;
        }
        public static void BuildPanels()
        {
            SceneManager.SetActiveScene(SceneManager.GetSceneByName("MainMenu"));
            var bridge=Main<HomeV2Integration>();var root=bridge.GetComponent<UIV2Root>();
            if(bridge.GetComponent<QuickSelectionPanels>()!=null)throw new System.Exception("Quick panels already exist");
            var q=bridge.gameObject.AddComponent<QuickSelectionPanels>();q.Modes=Main<ModalitySelectorPanelUI>();
            q.DeckModal=CloneModal("PanelMazzoController","QuickDeckV2",root);
            q.ModeModal=CloneModal("PanelModalitaController","QuickModeV2",root);
            var deckFrame=q.DeckModal.Frame;var content=deckFrame.Find("ScrollView/Viewport/Content");
            var first=content.GetChild(0); foreach(Transform child in content)child.gameObject.SetActive(false);
            var entries=Project51.Core.CardDecks.Catalog.Entries;q.DeckButtons=new Button[entries.Count];
            for(int i=0;i<entries.Count;i++)
            {
                var cell=Object.Instantiate(first.gameObject,content,false);cell.name="Deck_"+entries[i].Id;cell.SetActive(true);
                cell.GetComponent<RectTransform>().anchoredPosition = new Vector2(i * 315, -44);
                cell.transform.Find("CardArt").GetComponent<Image>().sprite=entries[i].Artwork;
                cell.transform.Find("CardArt").GetComponent<Image>().preserveAspect=true;
                var label=cell.transform.Find("NameText").GetComponent<TMP_Text>();
                label.text=entries[i].DisplayName;label.fontSize=30;label.enableAutoSizing=true;label.fontSizeMin=24;label.fontSizeMax=30;
                q.DeckButtons[i]=cell.GetComponent<Button>();q.DeckButtons[i].interactable=true;
            }
            var contentRect=(RectTransform)content;contentRect.sizeDelta=new Vector2(contentRect.sizeDelta.x,330);
            q.Fan=deckFrame.Find("PreviewBox").GetComponentsInChildren<Image>(true).Where(x=>x.name=="CardArt").ToArray();
            q.Caption=deckFrame.Find("PreviewBox/Caption").GetComponent<TMP_Text>();q.Confirm=deckFrame.Find("ConfirmButton").GetComponent<Button>();
            var modeContent=q.ModeModal.Frame.Find("ScrollView/Viewport/Content");
            q.ModeButtons=new[]{"Row_1v1","Row_2v2","Row_1v3","Row_1v1Bot","Row_2v2Bot","Row_1v3Bot"}.Select(n=>modeContent.Find(n).GetComponent<Button>()).ToArray();
            q.DifficultyButtons=new[]{"Pill_Facile","Pill_Medio","Pill_Difficile"}.Select(n=>modeContent.Find(n).GetComponent<Button>()).ToArray();
            q.CreateRoom=modeContent.Find("Row_Crea").GetComponent<Button>();q.JoinRoom=modeContent.Find("Row_Entra").GetComponent<Button>();
            q.ModeScroll=q.ModeModal.Frame.Find("ScrollView").GetComponent<ScrollRect>();
            Set(bridge,"quickPanels",q);EditorUtility.SetDirty(q);
        }
        public static void BuildPager()
        {
            var bridge=Main<HomeV2Integration>();var root=bridge.GetComponent<UIV2Root>();
            if(bridge.GetComponent<UIV2Pager>()!=null)throw new System.Exception("Pager already exists");
            var pager=bridge.gameObject.AddComponent<UIV2Pager>();
            var home=Main<HomeScreenV2>().GetComponent<RectTransform>();var cards=Main<CollectionScreenV2>().GetComponent<RectTransform>();var profile=Main<ProfileScreenV2>().GetComponent<RectTransform>();
            var shop=Rect("ShopPageV2",root.ScreenHost);Fill(shop);
            Text(At("Title",shop,0,350,900,100),"NEGOZIO",48);
            Text(At("ComingSoon",shop,0,470,860,130),"Il negozio sarà disponibile prossimamente.",30);
            Image(At("Icon",shop,0,650,180,180),Icon("ic_cart"));
            var pages=new[]{home,cards,shop,profile};
            var ps=new SerializedObject(pager);ps.FindProperty("viewport").objectReferenceValue=root.ScreenHost;var array=ps.FindProperty("pages");array.arraySize=4;
            for(int i=0;i<4;i++)
            {
                var p=pages[i];p.gameObject.SetActive(true);Fill(p);p.anchoredPosition=new Vector2(i*1080,0);array.GetArrayElementAtIndex(i).objectReferenceValue=p;
                PrefabUtility.RecordPrefabInstancePropertyModifications(p);PrefabUtility.RecordPrefabInstancePropertyModifications(p.gameObject);
                var hit=p.GetComponent<Image>();if(hit==null){hit=p.gameObject.AddComponent<Image>();hit.color=Color.clear;}hit.raycastTarget=true;
                var surface=p.gameObject.AddComponent<PageSwipeSurface>();surface.Pager=pager;
                foreach(var scroll in p.GetComponentsInChildren<ScrollRect>(true))
                {
                    var target=scroll.gameObject;var content=scroll.content;var viewport=scroll.viewport;var bar=scroll.verticalScrollbar;
                    bool inertia=scroll.inertia;float sensitivity=scroll.scrollSensitivity;var movement=scroll.movementType;
                    Object.DestroyImmediate(scroll);var replacement=target.AddComponent<PagedVerticalScrollRect>();replacement.Pager=pager;
                    replacement.content=content;replacement.viewport=viewport;replacement.verticalScrollbar=bar;replacement.horizontal=false;replacement.vertical=true;
                    replacement.inertia=inertia;replacement.scrollSensitivity=sensitivity;replacement.movementType=movement;
                }
            }
            ps.ApplyModifiedPropertiesWithoutUndo();root.ScreenHost.gameObject.AddComponent<RectMask2D>();
            root.BottomNavHost.gameObject.AddComponent<PageSwipeSurface>().Pager=pager;
            Set(bridge,"pager",pager);
            var launch=Main<GameLaunchController>();var ls=new SerializedObject(launch);ls.FindProperty("useFakeMatchmakingForTraining").boolValue=false;ls.ApplyModifiedPropertiesWithoutUndo();
        }
        public static void BuildEntry()
        {
            SceneManager.SetActiveScene(SceneManager.GetSceneByName("MainMenu"));
            if(Object.FindObjectsOfType<StartScreenV2>(true).Length>0)throw new System.Exception("Start screen already exists");
            var bridge=Main<HomeV2Integration>();var old=Main<Project51.UI.TapToEnterUI>();old.gameObject.SetActive(false);
            var logo=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"logo_51.png");
            var startup=CanvasRoot("StartScreenV2",1000);var start=startup.gameObject.AddComponent<StartScreenV2>();
            start.View=startup.gameObject.AddComponent<CanvasGroup>();Background(startup);var design=DesignArea(startup);
            Image(At("Logo",design,0,650,670,670),logo);
            start.GuestButton=Button(design,"GuestButton","GIOCA COME OSPITE","btn_gold_long",1150);
            start.LoginButton=Button(design,"LoginButton","ACCEDI","btn_blue_long",1315);
            start.RegisterButton=Button(design,"RegisterButton","REGISTRATI","btn_teal",1480);
            start.OptionsButton=Button(design,"OptionsButton","","sq_blue",245,105,100,425);
            Image(At("Gear",start.OptionsButton.transform,0,50,57,57),Icon("ic_gear"));
            Text(At("OptionsLabel",design,425,330,200,45),"Opzioni",26);
            var news=Button(design,"NewsButton","","sq_blue",245,105,100,290);news.interactable=false;
            Image(At("Mail",news.transform,0,50,57,57),Icon("ic_mail"));Text(At("NewsLabel",design,290,330,200,45),"Novità",26);
            Text(At("Hint",design,0,1610,900,80),"Registrati per salvare i tuoi progressi",28);
            Text(At("Version",design,0,1810,600,70),"v"+Application.version,26);
            start.AuthUI=Main<Project51.Auth.AuthUIController>();start.Settings=Main<SettingsV2Integration>();
            var settingsPanel=(GameObject)new SerializedObject(start.Settings).FindProperty("panel").objectReferenceValue;
            start.SettingsCanvas=settingsPanel.AddComponent<Canvas>();start.SettingsCanvas.overrideSorting=true;start.SettingsCanvas.sortingOrder=20;settingsPanel.AddComponent<GraphicRaycaster>();
            Set(bridge,"startScreen",start);EditorUtility.SetDirty(start);
            var loading=CanvasRoot("AppLoadingV2",3000);var controller=loading.gameObject.AddComponent<AppLoadingView>();
            var view=Rect("LoadingView",loading);Fill(view);controller.View=view.gameObject.AddComponent<CanvasGroup>();Background(view);
            var ld=DesignArea(view);Image(At("Logo",ld,0,670,400,400),logo);
            controller.Cards=new RectTransform[4];
            for(int i=0;i<4;i++){var card=At("Card"+i,ld,(i-1.5f)*130,975,160,230);Image(card,Icon("card_back_green"));card.localEulerAngles=new Vector3(0,0,(1.5f-i)*7);controller.Cards[i]=card;}
            Text(At("Title",ld,0,1155,900,90),"CARICAMENTO",48).fontStyle=FontStyles.Bold;
            var track=At("ProgressTrack",ld,0,1270,790,44);var trackImage=Image(track,Icon("bar_empty"));trackImage.preserveAspect=false;trackImage.type=UnityEngine.UI.Image.Type.Sliced;
            var fill=Rect("Fill",track);Fill(fill);controller.Progress=Image(fill,Icon("bar_knob"));controller.Progress.preserveAspect=false;controller.Progress.type=UnityEngine.UI.Image.Type.Filled;controller.Progress.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;
            controller.Status=Text(At("Status",ld,0,1345,880,80),"Preparazione…",30);
            var tip=At("TipPanel",ld,0,1560,875,210);Image(tip).color=new Color32(17,32,51,255);
            Text(At("TipTitle",tip,0,45,800,60),"LO SAPEVI?",30).color=new Color32(255,210,117,255);
            Text(At("Tip",tip,0,130,790,110),"Il 7 di coppe è la Matta: può cambiare valore per completare un accuso.",28);
            controller.RetryButton=Button(ld,"RetryButton","RIPROVA","btn_teal",1780,370,88,-205);
            controller.CancelButton=Button(ld,"CancelButton","INDIETRO","btn_blue_long",1780,370,88,205);
            view.gameObject.SetActive(false);EditorUtility.SetDirty(controller);
        }
    }
}
