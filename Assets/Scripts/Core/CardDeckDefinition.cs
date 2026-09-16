using UnityEngine;

namespace Project51.Core
{
    public sealed class CardDeckDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private Sprite back;
        [SerializeField] private Sprite[] faces = new Sprite[40];

        public string Id => id;
        public Sprite Back => back;
        public Sprite GetFace(Card card)
        {
            if (card == null) return null;
            int index = (int)card.Suit * 10 + card.Rank - 1;
            return faces != null && index >= 0 && index < faces.Length ? faces[index] : null;
        }
    }
}
