using System;
using System.Linq;
using DG.Tweening;
using Project51.Core;
using Project51.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UIV2.Core
{
    public sealed class MatchResultsV2 : MonoBehaviour
    {
        public GameObject Panel;
        public TMP_Text Title,Subtitle,Details,ContinueLabel;
        public TMP_Text[] Names,Scores,Deltas;
        public Image[] Progress;
        public Button Continue,Menu;
        public GameObject Trophy;
        private readonly MatchScoreTracker tracker=new MatchScoreTracker();
        private Action next,menu;
        private bool finished,clicked;
        public MatchScoreTracker Tracker=>tracker;
        private void Awake()
        {
            GamePresentation.RoundResults+=Show;GamePresentation.HideResults+=Hide;
            Continue.onClick.AddListener(Next);Menu.onClick.AddListener(()=>{if(clicked)return;clicked=true;menu?.Invoke();});Panel.SetActive(false);
        }
        public void Show(GameState state,Action nextRound,Action mainMenu)
        {
            tracker.Record(state);next=nextRound;menu=mainMenu;clicked=false;
            var config=GameSceneInitializer.ActiveConfig??new MatchConfig();int target=config.TargetScore;
            var scores=tracker.Scores(config.Format);finished=tracker.IsFinished(config.Format,target);var winners=tracker.Winners(config.Format);
            bool team=config.Format==GameFormat.TwoVsTwo;int local=GameModeService.Current.LocalPlayerIndex;int localEntry=team?local%2:local;
            Title.text=finished?(winners.Length>1?"PAREGGIO":winners.Contains(localEntry)?"HAI VINTO!":"FINE PARTITA"):"FINE SMAZZATA";
            Subtitle.text=finished?"Traguardo "+target+" punti":"Smazzata "+tracker.RoundNumber+" · mazzo esaurito";
            Trophy.SetActive(finished);var order=Enumerable.Range(0,scores.Length).OrderByDescending(i=>scores[i]).ToArray();
            for(int row=0;row<Names.Length;row++)
            {
                bool show=row<order.Length;Names[row].transform.parent.gameObject.SetActive(show);if(!show)continue;int player=order[row];
                Names[row].text=(row+1)+". "+(team?"Squadra "+(player+1):GameSocialV2.PlayerName(player));
                Scores[row].text=scores[player]>=1000?"Cappotto":scores[player].ToString();
                int delta=team?tracker.LastRound[player]+tracker.LastRound[player+2]:tracker.LastRound[player];
                Deltas[row].text=(delta>=1000?"Cappotto":"+"+delta)+" questa smazzata";Progress[row].fillAmount=Mathf.Clamp01((float)scores[player]/Mathf.Max(1,target));
            }
            var breakdown=FindObjectOfType<RoundEndPanel>().CalculateScoreDetails(state);
            Func<Func<ScoreDetails,bool>,string> awarded=predicate=>string.Join(", ",Enumerable.Range(0,state.NumPlayers).Where(i=>predicate(breakdown[i])).Select(GameSocialV2.PlayerName));
            string cards=awarded(d=>d.WonCards),denari=awarded(d=>d.WonDenari),sette=awarded(d=>d.HasSetteBello),primiera=awarded(d=>d.WonPrimiera);
            var d=breakdown[Mathf.Clamp(local,0,breakdown.Length-1)];
            Details.text="PUNTI ASSEGNATI\nCarte: "+(cards==""?"Nessuno":cards)+"\nDenari: "+(denari==""?"Nessuno":denari)+"\nSettebello: "+(sette==""?"Nessuno":sette)+"\nPrimiera: "+(primiera==""?"Nessuno":primiera)
                +"\n\nIL TUO RIEPILOGO\nScope +"+d.ScopaCount+"   ·   Accusi +"+d.AccusiPoints+"\nGrande +"+(d.HasGrande?5:0)+"   ·   Piccola +"+(d.HasPiccola?3+d.PiccolaExtras:0);
            bool canAdvance=!GameModeService.Current.IsMultiplayer||GameModeService.Current.IsMasterClient;
            Continue.interactable=canAdvance;ContinueLabel.text=canAdvance?(finished?"RIVINCITA":"CONTINUA"):"ATTENDI L'HOST";
            Panel.SetActive(true);var g=Panel.GetComponent<CanvasGroup>();g.alpha=0;g.DOFade(1,.3f).SetUpdate(true);
        }
        private void Next()
        {
            if(clicked||GameModeService.Current.IsMultiplayer&&!GameModeService.Current.IsMasterClient)return;
            clicked=true;if(finished)tracker.Reset();var callback=next;Hide();callback?.Invoke();
        }
        private void Update()
        {
            // The host's next-round state also dismisses the results on remote clients.
            if(Panel.activeSelf&&GameModeService.Current.IsMultiplayer&&!GameModeService.Current.IsMasterClient)
            {
                var turn=FindObjectOfType<TurnController>();
                if(turn?.GameState!=null&&!turn.GameState.RoundEnded){if(finished)tracker.Reset();Hide();}
            }
        }
        public void Hide(){Panel.GetComponent<CanvasGroup>().DOKill();Panel.SetActive(false);}
        private void OnDestroy(){GamePresentation.RoundResults-=Show;GamePresentation.HideResults-=Hide;}
    }
}
