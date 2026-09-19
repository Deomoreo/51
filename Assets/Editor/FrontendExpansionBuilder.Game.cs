using System.Linq;
using Project51.UIV2.Core;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.EditorTools
{
    public static partial class FrontendExpansionBuilder
    {
        public static void Game()
        {
            if(Object.FindObjectOfType<GameSocialV2>(true)!=null)throw new System.Exception("Game presentation already exists");
            var root=Canvas("GamePresentationV2",600);var social=root.gameObject.AddComponent<GameSocialV2>();
            var design=Rect("Design",root);Fill(design);design.gameObject.AddComponent<DesignCanvasFit>();
            RectTransform frame;social.EmoticonPanel=Modal("Emoticons",root,"EMOTICON",out frame).gameObject;
            frame.sizeDelta=new Vector2(980,980);frame.anchoredPosition=new Vector2(0,-1100);
            social.Close=Button(frame,"Close","X",420,55,75,70,"sq_blue");
            social.Hint=Label(frame,"Hint","",0,180,870,80,29);
            var all=AssetDatabase.LoadAllAssetsAtPath(Art+"14_emoticon_set.png").OfType<Sprite>().ToArray();string[] names={"emo_risata","emo_arrabbiato","emo_sorpreso","emo_pensieroso","emo_triste","emo_furbo"};social.Sprites=names.Select(n=>all.First(s=>s.name==n)).ToArray();
            social.EmoticonButtons=new UnityEngine.UI.Button[6];
            for(int i=0;i<6;i++)
            {var r=At("Emoticon"+i,frame,(i%3-1)*292,390+(i/3)*280,270,245);var img=Pic(r,Panel,new Color32(37,63,90,255));img.type=UnityEngine.UI.Image.Type.Sliced;img.preserveAspect=false;img.raycastTarget=true;
                var b=r.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=img;social.EmoticonButtons[i]=b;
                Pic(At("Icon",r,0,95,150,150),social.Sprites[i]);Label(r,"Name",CollectionCosmeticsV2.Names[i],0,205,255,50,28);
            }
            Label(frame,"Legend","Le emoticon non equipaggiate restano disabilitate",0,870,850,90,25);
            social.Bubbles=new CanvasGroup[4];social.BubbleImages=new UnityEngine.UI.Image[4];social.BubbleNames=new TMP_Text[4];
            float[] xs={-300,-390,0,390};float[] ys={1400,800,320,800};
            for(int i=0;i<4;i++)
            {var r=At("Bubble"+i,design,xs[i],ys[i],190,220);var bg=Pic(r,Panel,new Color32(17,32,51,245));bg.type=UnityEngine.UI.Image.Type.Sliced;bg.preserveAspect=false;
                social.Bubbles[i]=r.gameObject.AddComponent<CanvasGroup>();social.Bubbles[i].blocksRaycasts=false;
                social.BubbleImages[i]=Pic(At("Face",r,0,83,125,125),social.Sprites[0]);social.BubbleNames[i]=Label(r,"Name","Tu",0,183,185,55,24);r.gameObject.SetActive(false);}
            social.Impact=Impact(design);social.Impact.Fist.anchoredPosition=new Vector2(0,-195);social.Impact.Fist.sizeDelta=new Vector2(100,110);
            social.Impact.Caption.rectTransform.anchoredPosition=new Vector2(0,-310);
            social.AccusoCards=new UnityEngine.UI.Image[3];for(int i=0;i<3;i++)social.AccusoCards[i]=Pic(At("AccusoCard"+i,social.Impact.Group.transform,(i-1)*105,45,90,140),Icon("card_back_green"));
            // I risultati (MatchResultsV2) li costruisce UIV2FoundationBuilder.Online: Tools/UIV2/Build Match Results.
            EditorUtility.SetDirty(social);
        }

        // Avviso al tavolo per disconnessioni/rientri (GamePresentation.ConnectionNotice). Rilanciabile.
        [MenuItem("Tools/UIV2/Build Connection Notice")]
        public static void ConnectionNotice()
        {
            if(!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity",UnityEditor.SceneManagement.OpenSceneMode.Single);
            var design=Object.FindObjectOfType<GameSocialV2>(true).transform.Find("Design");
            var old=design.Find("ConnectionNotice");if(old!=null)Object.DestroyImmediate(old.gameObject);
            // Il componente resta su un contenitore sempre attivo: il banner figlio si accende/spegne.
            var host=Rect("ConnectionNotice",design);Fill(host);var notice=host.gameObject.AddComponent<ConnectionNoticeV2>();
            var banner=At("Banner",host,0,190,940,104);
            var border=Pic(banner,Panel,new Color32(229,179,69,255));border.type=UnityEngine.UI.Image.Type.Sliced;border.preserveAspect=false;
            var inner=Rect("Fill",banner);Fill(inner);inner.offsetMin=new Vector2(5,5);inner.offsetMax=new Vector2(-5,-5);
            var fill=Pic(inner,Panel,new Color32(17,32,51,245));fill.type=UnityEngine.UI.Image.Type.Sliced;fill.preserveAspect=false;
            notice.Group=banner.gameObject.AddComponent<CanvasGroup>();notice.Group.blocksRaycasts=false;
            notice.Message=Label(banner,"Message","",0,52,900,90,30);notice.Message.enableAutoSizing=true;notice.Message.fontSizeMin=20;notice.Message.fontSizeMax=30;
            banner.gameObject.SetActive(false);
            EditorUtility.SetDirty(notice);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        }
    }
}
