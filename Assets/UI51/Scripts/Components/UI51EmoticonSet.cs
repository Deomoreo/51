using System;
using UnityEngine;

namespace Project51.UI51
{
    /// <summary>
    /// Fotogrammi delle 6 emoticon (fogli emo_*_sheet_8frames, 4x2). Asset in Resources/UI51/EmoticonSet,
    /// creato da Tools/UI51/Import Settings + Atlases.
    /// </summary>
    [CreateAssetMenu(menuName = "UI51/Emoticon Set", fileName = "EmoticonSet")]
    public class UI51EmoticonSet : ScriptableObject
    {
        /// <summary>
        /// Ordine = indice di rete di NetworkGameController.SendEmoticon / GamePresentation.EmoticonReceived
        /// (stesso ordine di CollectionCosmeticsV2.Names). Non riordinare.
        /// </summary>
        public static readonly string[] Order = { "risata", "arrabbiato", "sorpreso", "pensieroso", "triste", "furbo" };

        [Serializable]
        public class Entry
        {
            public string id;
            public Sprite[] frames = new Sprite[0];
        }

        [SerializeField] Entry[] m_Entries = new Entry[0];

        static UI51EmoticonSet s_Instance;

        public static UI51EmoticonSet Instance
        {
            get
            {
                if (s_Instance == null) s_Instance = Resources.Load<UI51EmoticonSet>("UI51/EmoticonSet");
                return s_Instance;
            }
        }

        public static int Count => Order.Length;

        /// <summary>Fotogrammi per indice di rete; null se l'indice o l'asset mancano.</summary>
        public static Sprite[] Frames(int index)
        {
            var set = Instance;
            if (set == null || index < 0 || index >= Order.Length) return null;
            return set.Get(Order[index]);
        }

        public Sprite[] Get(string id)
        {
            foreach (var e in m_Entries)
                if (e != null && e.id == id) return e.frames;
            return null;
        }

        public void Set(string id, Sprite[] frames)
        {
            for (int i = 0; i < m_Entries.Length; i++)
            {
                if (m_Entries[i] == null || m_Entries[i].id != id) continue;
                m_Entries[i].frames = frames;
                return;
            }
            Array.Resize(ref m_Entries, m_Entries.Length + 1);
            m_Entries[m_Entries.Length - 1] = new Entry { id = id, frames = frames };
        }
    }
}
