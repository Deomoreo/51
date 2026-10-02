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
        /// <summary>Avviso di connessione al tavolo: testo (null = nascondi) e secondi prima di sparire (0 = resta).</summary>
        public static event Action<string,float> ConnectionNotice;
        public static void ShowConnectionNotice(string message,float seconds=0)=>ConnectionNotice?.Invoke(message,seconds);
        /// <summary>Avviso su un giocatore (mockup MomentoDisconnesso): posto, riga, riga sotto (null = nessuna), bordo rosso.</summary>
        public static event Action<int,string,string,bool> PlayerNotice;
        public static void ShowPlayerNotice(int seat,string title,string sub,bool alert)
        {if(PlayerNotice!=null)PlayerNotice.Invoke(seat,title,sub,alert);else ShowConnectionNotice(sub==null?title:title+": "+sub,4f);}
        /// <summary>Carte distribuite (mockup MomentoDistribuzione / MomentoUltima): mano n di total.</summary>
        public static event Action<int,int> HandDealt;
        public static void ShowHand(int hand,int total)=>HandDealt?.Invoke(hand,total);
        /// <summary>Scopa del giocatore (mockup MomentoScopa).</summary>
        public static event Action<int> Scopa;
        public static void ShowScopa(int player)=>Scopa?.Invoke(player);
        public static bool ShowRound(GameState state,Action next,Action menu)
        {if(RoundResults==null)return false;RoundResults.Invoke(state,next,menu);return true;}
        public static void CloseResults()=>HideResults?.Invoke();
        public static void RequestEmoticons()=>OpenEmoticons?.Invoke();
        public static void ShowEmoticon(int player,int emoticon)=>EmoticonReceived?.Invoke(player,emoticon);
        public static void ShowAccuso(int player,int accuso)=>AccusoReceived?.Invoke(player,accuso);
        /// <summary>Un'animazione a tutto tavolo (es. pugno dell'accuso) e' in corso: il gioco aspetta prima di muovere carte.</summary>
        public static bool IsBusy=>Time.unscaledTime<busyUntil;
        public static void MarkBusy(float seconds)=>busyUntil=Mathf.Max(busyUntil,Time.unscaledTime+seconds);
        private static float busyUntil;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset(){OpenEmoticons=null;RoundResults=null;HideResults=null;EmoticonReceived=null;AccusoReceived=null;ConnectionNotice=null;PlayerNotice=null;HandDealt=null;Scopa=null;busyUntil=0;}
    }
}
