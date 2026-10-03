using System.Linq;
using Project51.UI51;
using Project51.UIV2.Data;
using Project51.UIV2.Screens;
using TMPro;
using UnityEngine;

namespace Project51.UIV2.Core
{
    public sealed class CollectionCosmeticsV2 : MonoBehaviour
    {
        public CollectionScreenV2 Screen;
        public Sprite[] Emoticons;
        public TMP_Text Feedback;
        public Sprite PugnoArtwork;
        [Tooltip("UI51: il pugno della scheda Accuso, che ANTEPRIMA fa battere sul posto.")]
        public RectTransform PreviewFist;
        public static readonly string[] Names = { "Risata", "Arrabbiato", "Sorpreso", "Pensieroso", "Triste", "Furbo" };
        public static int[] Equipped => PlayerPrefs.GetString("Collection.Emoticons", "0,1,2").Split(',')
            .Select(s => int.TryParse(s,out var i)?i:-1).Where(i=>i>=0&&i<6).Distinct().Take(3).ToArray();
        public static bool Equip(int index)
        {
            var list=Equipped.ToList();
            if(index<0||index>=6||list.Count>=3||list.Contains(index))return false;
            list.Add(index);Save(list.ToArray());return true;
        }
        public static void Remove(int index) => Save(Equipped.Where(i=>i!=index).ToArray());
        private static void Save(int[] indices){PlayerPrefs.SetString("Collection.Emoticons",string.Join(",",indices));PlayerPrefs.Save();}
        private void Start()
        {
            Screen.SetTabInteractable(CollectionTab.Emoticons,true);Screen.SetTabInteractable(CollectionTab.Accusi,true);
            Screen.EmoticonsPanel.OnEmoticonPressed+=Select;
            Screen.EmoticonsPanel.OnRemovePressed+=RemoveItem;
            Screen.AccusiPanel.OnPreviewPressed+=ShowPreview;
            Refresh();
        }
        /// <summary>Come nel mockup: il tocco in griglia toglie quella in uso, altrimenti la aggiunge in coda. Falso se non cambia nulla (slot pieni).</summary>
        public static bool Toggle(int index){if(!Equipped.Contains(index))return Equip(index);Remove(index);return true;}
        private void Select(CollectionItemViewData item){if(Toggle(int.Parse(item.Id)))Refresh();}
        private void RemoveItem(CollectionItemViewData item){Remove(int.Parse(item.Id));Refresh();}
        private void ShowPreview(AccusoViewData item){if(PreviewFist!=null)UIAnim.Thump(PreviewFist);}
        public void Refresh()
        {
            var equipped=Equipped;
            var items=Names.Select((name,i)=>new CollectionItemViewData{Id=i.ToString(),Title=name,Icon=Emoticons[i],Unlocked=true,Equipped=equipped.Contains(i),Order=System.Array.IndexOf(equipped,i)+1}).ToArray();
            Screen.EmoticonsPanel.Bind(items,6);
            // Slot nell'ordine scelto, lo stesso che GameSocialV2 usa al tavolo (Bind li metteva in ordine di catalogo).
            Screen.EmoticonsPanel.SetEquipped(equipped.Select(i=>items[i]).ToArray());
            Screen.SetTabCount(CollectionTab.Emoticons,equipped.Length+"/3");
            Screen.AccusiPanel.Bind(new[]{new AccusoViewData{Id="pugno",Title="Pugno sul tavolo",Subtitle="Standard · disponibile",Description="Quando accusi, batti il pugno e fai tremare il tavolo: tutti i giocatori lo vedranno.",ShortDescription="Pugno e tavolo che trema",Artwork=PugnoArtwork,Unlocked=true,Equipped=true}},1);
            Screen.SetTabCount(CollectionTab.Accusi,"1");
            if(Feedback!=null)Feedback.text=equipped.Length<3?"Tocca un'emoticon qui sotto per aggiungerla":"Slot pieni: tocca un'emoticon per toglierla";
        }
        private void OnDestroy()
        {
            Screen.EmoticonsPanel.OnEmoticonPressed-=Select;Screen.EmoticonsPanel.OnRemovePressed-=RemoveItem;
            Screen.AccusiPanel.OnPreviewPressed-=ShowPreview;
        }
    }
}
