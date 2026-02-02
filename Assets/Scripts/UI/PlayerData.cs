using System;

namespace Project51.Unity
{
    public class PlayerData
    {
        public int Gold { get; private set; }
        public int Gems { get; private set; }
        public int Level { get; private set; }
        public int CurrentXp { get; private set; }
        public int NextLevelXp { get; private set; }

        public event Action OnChanged;

        public void UpdateData(int gold, int gems, int level, int currentXp = 0, int nextLevelXp = 0)
        {
            Gold = gold;
            Gems = gems;
            Level = level;
            CurrentXp = currentXp;
            NextLevelXp = nextLevelXp;
            OnChanged?.Invoke();
        }
    }
}