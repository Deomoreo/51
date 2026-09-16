using System.Linq;

namespace Project51.Core
{
    public sealed class MatchScoreTracker
    {
        public int[] Totals {get; private set;}
        public int[] LastRound {get; private set;}
        public int RoundNumber {get; private set;}
        private GameState last;
        public void Record(GameState state)
        {
            if(last!=null&&last.RoundIndex==state.RoundIndex)return;
            if(Totals==null||Totals.Length!=state.NumPlayers)Totals=new int[state.NumPlayers];
            LastRound=state.Players.Select(p=>p.TotalScore).ToArray();
            for(int i=0;i<Totals.Length;i++)Totals[i]+=LastRound[i];
            last=state;RoundNumber++;
        }
        public int[] Scores(GameFormat format)=>format==GameFormat.TwoVsTwo&&Totals.Length==4?new[]{Totals[0]+Totals[2],Totals[1]+Totals[3]}:(int[])Totals.Clone();
        public bool IsFinished(GameFormat format,int target)=>Totals!=null&&Scores(format).Max()>=target;
        public int[] Winners(GameFormat format){var s=Scores(format);int best=s.Max();return Enumerable.Range(0,s.Length).Where(i=>s[i]==best).ToArray();}
        public void Reset(){Totals=null;LastRound=null;last=null;RoundNumber=0;}
    }
}
