using UnityEngine;

namespace Project51.Unity
{
    public class PlayerDataProvider : MonoBehaviour
    {
        public PlayerData Data { get; private set; }

        private void Awake()
        {
            Data = new PlayerData();

            // placeholder iniziale
            Data.UpdateData(gold: 1000, gems: 50, level: 1);
        }
    }
}
