using System.Linq;
using Project51.UIV2.Core;
using Project51.UIV2.Screens;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.EditorTools
{
    public static partial class FrontendExpansionBuilder
    {
        const string Art="Assets/UI/Sprites/DragonsHoard/sprites_unity/sprites_unity/";
        public static Sprite Icon(string name)=>AssetDatabase.LoadAllAssetsAtPath(Art+"Icons.png").OfType<Sprite>().First(s=>s.name==name);
        public static Sprite Panel=>AssetDatabase.LoadAllAssetsAtPath(Art+"PanelsNeutral_v2.png").OfType<Sprite>().First(s=>s.name=="panel_fill_r24");
        public static RectTransform Rect(string name,Transform parent){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);return r;}
        public static void Fill(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        public static RectTransform At(string name,Transform parent,float x,float y,float w,float h)
        {var r=Rect(name,parent);r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
        public static UnityEngine.UI.Image Pic(RectTransform r,Sprite sprite,Color? color=null)
        {var i=r.gameObject.AddComponent<UnityEngine.UI.Image>();i.sprite=sprite;i.color=color??Color.white;i.raycastTarget=false;i.preserveAspect=true;return i;}
        public static TMP_Text Label(Transform parent,string name,string value,float x,float y,float w,float h,float size=32)
        {var t=At(name,parent,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();t.font=TMP_Settings.defaultFontAsset;t.text=value;t.fontSize=size;t.alignment=TextAlignmentOptions.Center;t.color=new Color32(255,240,210,255);t.raycastTarget=false;return t;}
        public static UnityEngine.UI.Button Button(Transform parent,string name,string text,float x,float y,float w=740,float h=100,string art="btn_blue_long")
        {var r=At(name,parent,x,y,w,h);var i=Pic(r,Icon(art));i.preserveAspect=false;i.type=UnityEngine.UI.Image.Type.Sliced;i.raycastTarget=true;var b=r.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=i;Label(r,"Label",text,0,h/2,w-22,h-12,34).fontStyle=FontStyles.Bold;return b;}
        public static void Set(Object target,string field,Object value){var s=new SerializedObject(target);s.FindProperty(field).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
        public static RectTransform Canvas(string name,int order)
        {var r=Rect(name,null);var c=r.gameObject.AddComponent<UnityEngine.Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=order;var s=r.gameObject.AddComponent<CanvasScaler>();s.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;s.referenceResolution=new Vector2(1080,1920);s.matchWidthOrHeight=.5f;r.gameObject.AddComponent<GraphicRaycaster>();return r;}
        public static RectTransform Modal(string name,Transform parent,string title,out RectTransform frame)
        {
            var root=Rect(name,parent);Fill(root);root.gameObject.AddComponent<CanvasGroup>();
            var dim=Rect("Dim",root);Fill(dim);Pic(dim,null,new Color(0,0,0,.78f)).raycastTarget=true;
            var design=Rect("Design",root);Fill(design);design.gameObject.AddComponent<DesignCanvasFit>();
            frame=At("Frame",design,0,960,980,1520);var border=Pic(frame,Panel,new Color32(229,179,69,255));border.type=UnityEngine.UI.Image.Type.Sliced;border.preserveAspect=false;border.raycastTarget=true;
            var inner=Rect("Fill",frame);Fill(inner);inner.offsetMin=new Vector2(7,7);inner.offsetMax=new Vector2(-7,-7);var bg=Pic(inner,Panel,new Color32(24,43,65,255));bg.type=UnityEngine.UI.Image.Type.Sliced;bg.preserveAspect=false;
            Pic(At("Ribbon",frame,0,30,780,250),Icon("ribbon_teal"));Label(frame,"Title",title,0,25,600,80,44).fontStyle=FontStyles.Bold;
            root.gameObject.SetActive(false);return root;
        }
        public static AccusoImpactV2 Impact(Transform parent)
        {
            var host=Rect("AccusoImpact",parent);Fill(host);var c=host.gameObject.AddComponent<AccusoImpactV2>();
            var visual=At("Visual",host,0,750,700,350);c.Group=visual.gameObject.AddComponent<CanvasGroup>();c.Group.blocksRaycasts=false;
            var fist=At("Fist",visual,0,120,150,165);fist.gameObject.AddComponent<FistGraphic>().raycastTarget=false;c.Fist=fist;
            c.Caption=Label(visual,"Title","CIRULLA!",0,275,690,75,44);c.Caption.fontStyle=FontStyles.Bold;visual.gameObject.SetActive(false);return c;
        }
        public static void CollectionAndLoading()
        {
            var l=Object.FindObjectOfType<AppLoadingView>(true);var track=l.Progress.transform.parent;
            var r=track.Find("VectorBar") as RectTransform;if(r==null){r=Rect("VectorBar",track);Fill(r);}
            var bar=r.GetComponent<LoadingProgressBar>()??r.gameObject.AddComponent<LoadingProgressBar>();bar.Source=l.Progress;bar.raycastTarget=false;
            track.GetComponent<UnityEngine.UI.Image>().enabled=false;l.Progress.enabled=false;l.MinimumDuration=3;EditorUtility.SetDirty(l);
            var screen=Object.FindObjectOfType<CollectionScreenV2>(true);
            var c=screen.GetComponent<CollectionCosmeticsV2>()??screen.gameObject.AddComponent<CollectionCosmeticsV2>();c.Screen=screen;
            string[] names={"emo_risata","emo_arrabbiato","emo_sorpreso","emo_pensieroso","emo_triste","emo_furbo"};
            var sprites=AssetDatabase.LoadAllAssetsAtPath(Art+"14_emoticon_set.png").OfType<Sprite>().ToArray();c.Emoticons=names.Select(n=>sprites.First(s=>s.name==n)).ToArray();
            var emoticons=screen.EmoticonsPanel;var existing=emoticons.transform.Find("Feedback");
            c.Feedback=existing==null?Label(emoticons.transform,"Feedback","Scegli fino a 3 emoticon da usare al tavolo",0,45,920,60,24):existing.GetComponent<TMP_Text>();
            c.Feedback.rectTransform.anchorMin=c.Feedback.rectTransform.anchorMax=new Vector2(.5f,0);c.Feedback.rectTransform.anchoredPosition=new Vector2(0,38);
            var root=screen.GetComponentInParent<UIV2Root>();c.Preview=Impact(root.OverlayHost);
            var hero=(GameObject)new SerializedObject(screen.AccusiPanel).FindProperty("heroArtworkPlaceholder").objectReferenceValue;
            foreach(var g in hero.GetComponentsInChildren<Graphic>(true))g.enabled=false;
            var fist=Rect("Fist",hero.transform);Fill(fist);fist.offsetMin=new Vector2(20,20);fist.offsetMax=new Vector2(-20,-20);fist.gameObject.AddComponent<FistGraphic>().raycastTarget=false;
            EditorUtility.SetDirty(c);
        }
    }
}
