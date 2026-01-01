using System;

namespace Project51.Unity
{
    public class PlayerData
    {
        public int Gold { get; private set; }
        public int Gems { get; private set; }
        public int Level { get; private set; }

        public event Action OnChanged;

        public void UpdateData(int gold, int gems, int level)
        {
            Gold = gold;
            Gems = gems;
            Level = level;
            OnChanged?.Invoke();
        }
    }
}