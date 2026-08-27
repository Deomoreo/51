using UnityEngine;
using Project51.Core;

namespace Project51.Unity
{
    public class CapturedPileManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TurnController turnController;
        [SerializeField] private GameObject playerCapturedPileViewPrefab;

        [Header("Layout Settings")]
        [SerializeField] private Transform[] playerPilePositions;
        [SerializeField] private bool autoCreatePositions = true;
        [SerializeField] private bool positionPilesBesidePlayerHands = true;
        [SerializeField] private float scopeOffsetFromCapturedPile = 1.1f;
        [SerializeField, Range(0.5f, 1.5f)] private float sharedPileCardScale = 1.15f;

        private PlayerCapturedPileView[] playerPileViews;
        private CardViewManager cardViewManager;
        private bool cardViewManagerSearched;
        private bool isInitialized = false;

        private void Start()
        {
            if (turnController == null)
                turnController = FindObjectOfType<TurnController>();

            CacheCardViewManager();

            if (autoCreatePositions && (playerPilePositions == null || playerPilePositions.Length == 0))
                CreateDefaultPilePositions();

            InvokeRepeating(nameof(TryInitializeAndRefresh), 0.5f, 0.5f);
        }

        private void TryInitializeAndRefresh()
        {
            if (!isInitialized) Initialize();
            if (isInitialized) RefreshAllPiles();
        }

        private void Initialize()
        {
            if (isInitialized) return;

            if (turnController == null)
            {
                turnController = FindObjectOfType<TurnController>();
                if (turnController == null) return;
            }

            if (turnController.GameState == null) return;

            InitializePileViews();
            isInitialized = true;
        }

        private void CreateDefaultPilePositions()
        {
            // Dynamic: create positions based on numPlayers
            // Default to 4 for fallback, but will be recreated when game starts with actual player count
            int numPlayers = 4;
            if (turnController != null && turnController.GameState != null)
                numPlayers = turnController.GameState.NumPlayers;

            playerPilePositions = new Transform[numPlayers];

            if (numPlayers == 2)
            {
                // 1v1: faccia a faccia (tu in basso, avversario in alto)
                var pos0 = new GameObject("Player0_CapturedPilePos").transform;
                pos0.SetParent(transform);
                pos0.localPosition = new Vector3(3.0f, -3.5f, 0);
                pos0.localRotation = Quaternion.identity;
                playerPilePositions[0] = pos0;

                var pos1 = new GameObject("Player1_CapturedPilePos").transform;
                pos1.SetParent(transform);
                pos1.localPosition = new Vector3(-2.0f, 3.5f, 0);
                pos1.localRotation = Quaternion.Euler(0, 0, 180); // ruotata 180� (capovolto)
                playerPilePositions[1] = pos1;
            }
            else
            {
                // 4P: layout originale
                var pos0 = new GameObject("Player0_CapturedPilePos").transform;
                pos0.SetParent(transform);
                pos0.localPosition = new Vector3(3.0f, -3.5f, 0);
                pos0.localRotation = Quaternion.identity;
                playerPilePositions[0] = pos0;

                var pos1 = new GameObject("Player1_CapturedPilePos").transform;
                pos1.SetParent(transform);
                pos1.localPosition = new Vector3(-6.5f, 0.0f, 0);
                pos1.localRotation = Quaternion.Euler(0, 0, -90);
                playerPilePositions[1] = pos1;

                var pos2 = new GameObject("Player2_CapturedPilePos").transform;
                pos2.SetParent(transform);
                pos2.localPosition = new Vector3(-2.0f, 3.5f, 0);
                pos2.localRotation = Quaternion.Euler(0, 0, 180);
                playerPilePositions[2] = pos2;

                var pos3 = new GameObject("Player3_CapturedPilePos").transform;
                pos3.SetParent(transform);
                pos3.localPosition = new Vector3(6.5f, 0.0f, 0);
                pos3.localRotation = Quaternion.Euler(0, 0, -90);
                playerPilePositions[3] = pos3;
            }
        }

        private void InitializePileViews()
        {
            if (turnController == null || turnController.GameState == null) return;

            int numPlayers = turnController.GameState.NumPlayers;
            playerPileViews = new PlayerCapturedPileView[numPlayers];

            for (int i = 0; i < numPlayers && i < playerPilePositions.Length; i++)
            {
                if (playerPilePositions[i] == null) continue;

                GameObject pileViewObj;
                if (playerCapturedPileViewPrefab != null)
                    pileViewObj = Instantiate(playerCapturedPileViewPrefab, playerPilePositions[i]);
                else
                {
                    pileViewObj = new GameObject($"Player{i}_CapturedPileView");
                    pileViewObj.transform.SetParent(playerPilePositions[i], false);
                    pileViewObj.AddComponent<PlayerCapturedPileView>();
                }

                pileViewObj.transform.localPosition = Vector3.zero;

                var pileView = pileViewObj.GetComponent<PlayerCapturedPileView>();
                if (pileView != null)
                {
                    SetupPileViewContainers(pileView);
                    playerPileViews[i] = pileView;
                }
            }
        }

        private void SetupPileViewContainers(PlayerCapturedPileView pileView)
        {
            Transform normalContainer = null;
            Transform scopeContainer = null;

            foreach (Transform child in pileView.transform)
            {
                if (child.name == "NormalCapturesContainer")
                    normalContainer = child;
                else if (child.name == "ScopeContainer")
                    scopeContainer = child;
            }

            if (normalContainer == null)
            {
                normalContainer = new GameObject("NormalCapturesContainer").transform;
                normalContainer.SetParent(pileView.transform, false);
                normalContainer.localPosition = Vector3.zero;
            }

            if (scopeContainer == null)
            {
                scopeContainer = new GameObject("ScopeContainer").transform;
                scopeContainer.SetParent(pileView.transform, false);
                scopeContainer.localPosition = Vector3.right * 1.2f;
            }

            pileView.normalCapturesContainer = normalContainer;
            pileView.scopeContainer = scopeContainer;
        }

        private int GetLocalPlayerIndex()
        {
            // Fonte di verita' reale, popolata da GameSceneInitializer. In precedenza usava
            // reflection su un assembly "Project51.Networking" che non esiste piu' (rinominato/
            // rimosso), quindi tornava sempre 0: le pile di carte catturate finivano tutte
            // ancorate alla posizione "player 0" invece che a quella del player locale reale.
            return GameModeService.Current.LocalPlayerIndex;
        }

        private int MapToViewIndex(int playerIndex, int localIndex, int numPlayers)
        {
            // Rotate indices so localIndex maps to 0 (bottom), then left/top/right = 1/2/3 respectively
            int relative = (playerIndex - localIndex + numPlayers) % numPlayers;
            return relative;
        }

        private void RefreshAllPiles()
        {
            if (turnController == null || turnController.GameState == null || playerPileViews == null) return;

            var players = turnController.GameState.Players;
            int numPlayers = players.Count;
            int localIndex = GetLocalPlayerIndex();

            for (int p = 0; p < numPlayers; p++)
            {
                int viewIndex = MapToViewIndex(p, localIndex, numPlayers);
                if (viewIndex < 0 || viewIndex >= playerPileViews.Length) continue;
                var pileView = playerPileViews[viewIndex];
                if (pileView == null) continue;

                PositionPileBesideHand(viewIndex, numPlayers);
                pileView.SetPileCardScale(sharedPileCardScale);

                var playerState = players[p];
                pileView.UpdatePile(playerState);

                if (playerState.ScopaCards != null && playerState.ScopaCards.Count > 0)
                {
                    pileView.UpdateScopePileWithCards(playerState.ScopaCards);
                }
            }
        }

        private void PositionPileBesideHand(int relativePlayerIndex, int playerCount)
        {
            if (!positionPilesBesidePlayerHands || playerPilePositions == null ||
                relativePlayerIndex < 0 || relativePlayerIndex >= playerPilePositions.Length)
            {
                return;
            }

            var pilePosition = playerPilePositions[relativePlayerIndex];
            if (pilePosition == null)
            {
                return;
            }

            CacheCardViewManager();

            if (cardViewManager == null)
            {
                return;
            }

            Vector3 handAnchor = cardViewManager.GetPlayerHandAnchor(relativePlayerIndex, playerCount);
            Vector3 playerRight = cardViewManager.GetPlayerRightDirection(relativePlayerIndex, playerCount);
            pilePosition.position = handAnchor + playerRight * cardViewManager.GetCapturedPileDistance() + cardViewManager.GetCapturedPileScreenDownOffset();

            var pileView = playerPileViews[relativePlayerIndex];
            if (pileView != null && pileView.scopeContainer != null)
            {
                pileView.scopeContainer.position = pilePosition.position + playerRight * scopeOffsetFromCapturedPile;
            }
        }

        private void CacheCardViewManager()
        {
            if (cardViewManagerSearched)
            {
                return;
            }

            cardViewManagerSearched = true;
            cardViewManager = FindObjectOfType<CardViewManager>();
        }

        public void ForceRefresh()
        {
            if (!isInitialized) Initialize();
            RefreshAllPiles();
        }

        public void ClearAllPiles()
        {
            if (playerPileViews == null) return;
            
            foreach (var pileView in playerPileViews)
            {
                if (pileView != null) pileView.Clear();
            }
        }

        public PlayerCapturedPileView GetPileView(int playerIndex)
        {
            if (playerPileViews == null || playerIndex < 0 || playerIndex >= playerPileViews.Length)
                return null;
            
            return playerPileViews[playerIndex];
        }
    }
}

