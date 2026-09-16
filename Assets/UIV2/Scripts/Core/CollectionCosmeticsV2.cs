using System.Linq;
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
        public AccusoImpactV2 Preview;
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
            Screen.EmoticonsPanel.OnEmptySlotPressed+=Empty;
            Screen.AccusiPanel.OnPreviewPressed+=ShowPreview;
            Refresh();
        }
        private void Select(CollectionItemViewData item)
        {
            int index=int.Parse(item.Id);
            if(Equipped.Contains(index)){Feedback.text="Già equipaggiata. Usa la X per liberare lo slot.";return;}
            Feedback.text=Equip(index)?"Emoticon equipaggiata.":"Puoi usare 3 emoticon: rimuovine una con la X.";Refresh();
        }
        private void RemoveItem(CollectionItemViewData item){Remove(int.Parse(item.Id));Feedback.text="Slot libero: scegli un'emoticon qui sotto.";Refresh();}
        private void Empty(int index){Feedback.text="Scegli un'emoticon dalla collezione qui sotto.";}
        private void ShowPreview(AccusoViewData item){Preview.Play("PUGNO SUL TAVOLO",null);}
        public void Refresh()
        {
            var equipped=Equipped;
            Screen.EmoticonsPanel.Bind(Names.Select((name,i)=>new CollectionItemViewData{Id=i.ToString(),Title=name,Icon=Emoticons[i],Unlocked=true,Equipped=equipped.Contains(i)}).ToArray(),6);
            Screen.AccusiPanel.Bind(new[]{new AccusoViewData{Id="pugno",Title="Pugno sul tavolo",Subtitle="Standard · disponibile",Description="Un colpo sul tavolo accompagna il tuo accuso.",ShortDescription="Impatto e salto delle carte",Unlocked=true,Equipped=true}},1);
        }
        private void OnDestroy()
        {
            Screen.EmoticonsPanel.OnEmoticonPressed-=Select;Screen.EmoticonsPanel.OnRemovePressed-=RemoveItem;
            Screen.EmoticonsPanel.OnEmptySlotPressed-=Empty;Screen.AccusiPanel.OnPreviewPressed-=ShowPreview;
        }
    }
}
