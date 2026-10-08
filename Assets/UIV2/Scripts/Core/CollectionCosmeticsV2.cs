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
        // B12 (E3): emoticon in uso per account su questo telefono. L'ospite le cambia solo per la sua sessione (il suo account usa e
        // getta): se si registra le porta nell'account, che ha lo stesso PlayFabId.
        private const string Key = "Collection.Emoticons.", Default = "0,1,2";
        private static string guestId, guestValue;
        public static string Stored
        {
            get
            {
                var auth = Project51.Auth.AuthBootstrapper.Instance?.PlayFabAuth;
                string id = auth?.PlayFabId, guest = guestId == id ? guestValue : null;
                if (auth == null || !auth.HasRealLogin || string.IsNullOrEmpty(id)) return guest ?? Default;
                return PlayerPrefs.GetString(Key + id, guest ?? Default);
            }
            set
            {
                var auth = Project51.Auth.AuthBootstrapper.Instance?.PlayFabAuth;
                string id = auth?.PlayFabId;
                if (auth == null || !auth.HasRealLogin || string.IsNullOrEmpty(id)) { guestId = id; guestValue = value; return; }
                PlayerPrefs.SetString(Key + id, value);
                PlayerPrefs.Save();
            }
        }
        public static int[] Equipped => Stored.Split(',')
            .Select(s => int.TryParse(s,out var i)?i:-1).Where(i=>i>=0&&i<6).Distinct().Take(3).ToArray();
        public static bool Equip(int index)
        {
            var list=Equipped.ToList();
            if(index<0||index>=6||list.Count>=3||list.Contains(index))return false;
            list.Add(index);Save(list.ToArray());return true;
        }
        public static void Remove(int index) => Save(Equipped.Where(i=>i!=index).ToArray());
        private static void Save(int[] indices) => Stored = string.Join(",", indices);
        private void Start()
        {
            Screen.EmoticonsPanel.OnEmoticonPressed+=Select;
            Screen.EmoticonsPanel.OnRemovePressed+=RemoveItem;
            Screen.AccusiPanel.OnPreviewPressed+=ShowPreview;
            Refresh();
            // Dopo "Accedi" (senza ricaricare la scena) la pagina mostra quelle del nuovo account.
            if (Project51.Auth.AuthBootstrapper.Instance?.Profile != null) Project51.Auth.AuthBootstrapper.Instance.Profile.OnProfileLoaded += Refresh;
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
            Screen.AccusiPanel.Bind(new[]{new AccusoViewData{Id="pugno",Title="Pugno sul tavolo",Subtitle="Standard · disponibile",Description="Quando accusi, batti il pugno e fai tremare il tavolo: tutti i giocatori lo vedranno.",ShortDescription="Pugno e tavolo che trema",Artwork=PugnoArtwork,Unlocked=true,Equipped=true}});
            Screen.SetTabCount(CollectionTab.Accusi,"1");
            if(Feedback!=null)Feedback.text=equipped.Length<3?"Tocca un'emoticon qui sotto per aggiungerla":"Slot pieni: tocca un'emoticon per toglierla";
        }
        private void OnDestroy()
        {
            Screen.EmoticonsPanel.OnEmoticonPressed-=Select;Screen.EmoticonsPanel.OnRemovePressed-=RemoveItem;
            Screen.AccusiPanel.OnPreviewPressed-=ShowPreview;
            if (Project51.Auth.AuthBootstrapper.Instance?.Profile != null) Project51.Auth.AuthBootstrapper.Instance.Profile.OnProfileLoaded -= Refresh;
        }
    }
}
