using System;
using UnityEngine;

namespace Project51.Core
{
    public static class GamePresentation
    {
        public static event Action OpenEmoticons;
        public static event Action<GameState,Action,Action> RoundResults;
        public static event Action HideResults;
        public static event Action<int,int> EmoticonReceived;
        public static event Action<int,int> AccusoReceived;
        public static bool ShowRound(GameState state,Action next,Action menu)
        {if(RoundResults==null)return false;RoundResults.Invoke(state,next,menu);return true;}
        public static void CloseResults()=>HideResults?.Invoke();
        public static void RequestEmoticons()=>OpenEmoticons?.Invoke();
        public static void ShowEmoticon(int player,int emoticon)=>EmoticonReceived?.Invoke(player,emoticon);
        public static void ShowAccuso(int player,int accuso)=>AccusoReceived?.Invoke(player,accuso);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset(){OpenEmoticons=null;RoundResults=null;HideResults=null;EmoticonReceived=null;AccusoReceived=null;}
    }
}
