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
            var results=root.gameObject.AddComponent<MatchResultsV2>();results.Panel=Modal("Results",root,"FINE SMAZZATA",out frame).gameObject;
            results.Title=frame.Find("Title").GetComponent<TMP_Text>();results.Subtitle=Label(frame,"Subtitle","",0,180,850,90,31);
            results.Trophy=Pic(At("Trophy",frame,0,295,115,115),Icon("ic_trophy")).gameObject;
            results.Names=new TMP_Text[4];results.Scores=new TMP_Text[4];results.Deltas=new TMP_Text[4];results.Progress=new UnityEngine.UI.Image[4];
            for(int i=0;i<4;i++)
            {var r=At("Score"+i,frame,0,420+i*125,870,112);var bg=Pic(r,Panel,new Color32(33,54,80,255));bg.type=UnityEngine.UI.Image.Type.Sliced;bg.preserveAspect=false;
                results.Names[i]=Label(r,"Name","",-75,31,650,50,32);results.Names[i].alignment=TextAlignmentOptions.Left;
                results.Deltas[i]=Label(r,"Delta","",-70,70,640,35,22);results.Deltas[i].alignment=TextAlignmentOptions.Left;
                results.Scores[i]=Label(r,"Points","",340,39,170,60,38);results.Scores[i].color=new Color32(255,213,130,255);
                var track=At("Progress",r,-40,96,690,13);var source=Pic(track,Icon("bar_knob"));source.enabled=false;results.Progress[i]=source;
                var graphicRect=Rect("Vector",track);Fill(graphicRect);var graphic=graphicRect.gameObject.AddComponent<LoadingProgressBar>();graphic.Source=source;graphic.raycastTarget=false;}
            results.Details=Label(frame,"Details","",0,1060,850,360,27);results.Details.alignment=TextAlignmentOptions.TopLeft;
            results.Continue=Button(frame,"Continue","CONTINUA",-220,1375,420,105,"btn_gold_long");results.ContinueLabel=results.Continue.GetComponentInChildren<TMP_Text>();results.ContinueLabel.enableAutoSizing=true;results.ContinueLabel.fontSizeMin=20;results.ContinueLabel.fontSizeMax=34;
            results.Menu=Button(frame,"Menu","MENU",220,1375,420,105);
            EditorUtility.SetDirty(social);EditorUtility.SetDirty(results);
        }
    }
}
