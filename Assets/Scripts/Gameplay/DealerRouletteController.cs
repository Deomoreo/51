using System.Collections;
using UnityEngine;
using Project51.Core;

namespace Project51.Unity
{
    /// <summary>
    /// Scelta del mazziere a inizio smazzata: la ruota del mockup Sorteggio (ui51Wheel, SorteggioView; UI51 Fase 5 S10)
    /// in 1 contro 1 e a 4 (Sorteggio4, quattro spicchi): tempi fissi online, AL TAVOLO la salta offline dopo il risultato.
    /// Chi distribuisce resta winningSlot (deciso dal master). Grafica: UI51TableBuilder.BuildSorteggio.
    /// </summary>
    public class DealerRouletteController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [Header("UI51 Sorteggio (Fase 5 S10)")]
        [Tooltip("Ruota del mockup Sorteggio (SorteggioView). Vuoto = niente sorteggio a schermo.")]
        [SerializeField] private GameObject ui51Wheel;

        /// <summary>Mockup Sorteggio: parte da sola a 1.2 s, gira 3.4 s, risultato a 4.7, conto 3-2-1, consegna a 7.7.</summary>
        public const float WheelSpinAt = 1.2f, WheelSpinSeconds = 3.4f, WheelResultAt = 4.7f, WheelEndAt = 7.7f;

        /// <summary>Online i tempi sono fissi (tutti aprono la finestra Accuso insieme); offline seguono le animazioni veloci.</summary>
        public static float Timing(float seconds, bool online) => online ? seconds : GamePreferences.Scaled(seconds);

        /// <summary>Ruota in corso: spicchi (2 o 4), mazziere come spicchio (0 = io, poi in ordine di posto), a coppie.</summary>
        public int WheelPlayers { get; private set; } = 2;
        public int WheelDealer { get; private set; }
        public bool WheelTeams { get; private set; }
        public bool WheelLocalDealer => WheelDealer == 0;
        public bool WheelOnline { get; private set; }

        private bool continuePressed;
        private int wheelShow;

        /// <summary>AL TAVOLO della ruota: offline chiude subito (online il pulsante non c'e').</summary>
        public void Continue() => continuePressed = true;

        /// <summary>Chiude subito, anche una ruota in corso (intro interrotta da un nuovo stato).</summary>
        public void Hide()
        {
            wheelShow++;
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        /// <summary>
        /// Fa girare la ruota e la ferma su winningSlot. Non blocca mai la sequenza di TurnController: senza ruota
        /// (o con un numero di giocatori diverso da 2 e 4) esce subito. namesBySlot non serve piu' alla ruota.
        /// </summary>
        public IEnumerator PlayRoulette(string[] namesBySlot, int winningSlot, int playerCount, bool teams = false)
        {
            if (panelRoot == null || ui51Wheel == null || (playerCount != 2 && playerCount != 4)) yield break;
            ui51Wheel.SetActive(false); // PlayWheel la riaccende: SorteggioView riparte da OnEnable
            // In 1 contro 1 i posti sono 0 e 2, sulla ruota spicchi 0 e 1.
            yield return PlayWheel(playerCount == 2 ? winningSlot / 2 : winningSlot, playerCount, teams);
        }

        /// <summary>
        /// Ruota: SorteggioView (OnEnable) la anima con gli stessi tempi; qui l'attesa, il suono del risultato e la
        /// consegna. Un Hide() nel frattempo la chiude (wheelShow cambia) senza che questa fine spenga una ruota nuova.
        /// </summary>
        private IEnumerator PlayWheel(int dealer, int players, bool teams)
        {
            int show = ++wheelShow;
            WheelDealer = dealer;
            WheelPlayers = players;
            WheelTeams = teams;
            WheelOnline = GameModeService.Current.IsMultiplayer;
            continuePressed = false;
            panelRoot.SetActive(true);
            ui51Wheel.SetActive(true);

            float result = Timing(WheelResultAt, WheelOnline), end = Timing(WheelEndAt, WheelOnline), t = 0f;
            bool chimed = false;
            while (show == wheelShow && t < end && (WheelOnline || !continuePressed))
            {
                if (!chimed && t >= result)
                {
                    chimed = true;
                    GameAudio.Play(SoundId.UiConfirm, 0.9f, GameAudio.Sync.Onset);
                }
                yield return null;
                t += Time.unscaledDeltaTime;
            }
            if (show == wheelShow) panelRoot.SetActive(false);
        }
    }
}
