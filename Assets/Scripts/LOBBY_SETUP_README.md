# Sistema Lobby 51 - Guida alla Configurazione

## Panoramica

Questo sistema implementa le **3 porte** della lobby stile Clash Royale:

1. **Coda Pubblica (Quick Match)** - Un tap e giochi. Il sistema cerca/crea stanze automaticamente.
2. **Tavolo Privato (Private Room)** - Crea o unisciti con codice stanza. Waiting room con slot giocatori.
3. **Allenamento (Training)** - Partita offline vs Bot senza connessione.

## File Creati

### Core
- `Assets/Scripts/Core/MatchConfig.cs` - Configurazione completa di una partita (Intent, Format, Difficulty, etc.)

### Networking
- `Assets/Scripts/Networking/MatchmakingManager.cs` - Gestisce connessione Photon e matchmaking
- `Assets/Scripts/Networking/GameModeProviders.cs` - Provider per Training e Multiplayer

### UI
- `Assets/Scripts/UI/ModalitySelectorPanelUI.cs` - Pannello slide-up per selezione modalità
- `Assets/Scripts/UI/WaitingRoomUI.cs` - UI della sala d'attesa per stanze private
- `Assets/Scripts/UI/PlayerSlotUI.cs` - Singolo slot giocatore nella waiting room
- `Assets/Scripts/UI/JoinRoomPopupUI.cs` - Popup per inserire codice stanza
- `Assets/Scripts/UI/MatchmakingStatusUI.cs` - Mostra stato matchmaking (ricerca, connessione, etc.)
- `Assets/Scripts/UI/PrivateRoomOptionsUI.cs` - Opzioni "Crea" o "Unisciti" per stanza privata
- `Assets/Scripts/UI/GameLaunchController.cs` - Controller principale del flusso Home ? Game

### Gameplay
- `Assets/Scripts/Gameplay/GameSceneInitializer.cs` - Inizializza la scena di gioco con la config
- `Assets/Scripts/Gameplay/AppFlowManager.cs` (aggiornato) - Navigazione tra scene

## Configurazione Unity

### 1. Risolvere errori di Assembly

I nuovi file UI potrebbero non compilare inizialmente. Questo perché:
- La cartella `Assets/Scripts/UI/` non ha un assembly definition proprio
- I file usano TMPro, DOTween e Photon che sono in assembly separati

**Soluzione A - Riavviare Unity/VS**
Spesso basta riavviare Unity o Visual Studio per risolvere.

**Soluzione B - Creare Assembly Definition per UI**
1. Crea `Assets/Scripts/UI/Project51.UI.asmdef`
2. Aggiungi riferimenti a:
   - `Unity.TextMeshPro`
   - `DOTween.Modules` 
   - `PhotonUnityNetworking`
   - `PhotonRealtime`
   - `Project51.Core`
   - `Project51.Networking`

### 2. Setup nella Scena MainMenu

1. Crea un GameObject vuoto chiamato `MatchmakingManager`
2. Aggiungi il componente `MatchmakingManager`
3. Questo oggetto persiste tra le scene (DontDestroyOnLoad)

### 3. Setup nella Home

1. Nel tuo `HomePanelUI`, assegna:
   - `ModalitySelectorPanelUI` al campo `modalitySelectorPanel`
   - `GameLaunchController` al campo `gameLaunchController`

2. Crea un GameObject con `GameLaunchController` e assegna:
   - Il `ModalitySelectorPanelUI`
   - Il `WaitingRoomUI`
   - Il `JoinRoomPopupUI`
   - Il `MatchmakingStatusUI`

### 4. Creare i Prefab UI

#### ModalitySelectorPanel
Struttura consigliata:
```
ModalitySelectorPanel (SlideUpPanelUI)
??? PanelRoot
?   ??? BackgroundDim
?   ??? PanelContainer (animato)
?   ?   ??? MainMenu
?   ?   ?   ??? QuickMatchButton
?   ?   ?   ??? PrivateRoomButton  
?   ?   ?   ??? TrainingButton
?   ?   ??? FormatSelection
?   ?   ?   ??? TitleText
?   ?   ?   ??? Format1v1Button
?   ?   ?   ??? Format4PButton
?   ?   ?   ??? Format2v2Button
?   ?   ?   ??? DifficultyPanel (solo Training)
?   ?   ?   ?   ??? EasyButton
?   ?   ?   ?   ??? MediumButton
?   ?   ?   ?   ??? HardButton
?   ?   ?   ?   ??? ExpertButton
?   ?   ?   ??? BackButton
?   ?   ?   ??? ConfirmButton (solo Training)
?   ?   ??? PrivateRoomOptions
?   ?       ??? CreateRoomButton
?   ?       ??? JoinRoomButton
?   ?       ??? BackButton
```

#### WaitingRoom
```
WaitingRoomPanel
??? RoomCodeText
??? CopyCodeButton
??? PlayerSlotsContainer
?   ??? (PlayerSlotPrefab instances)
??? StatusText
??? StartButton (solo host)
??? LeaveButton
```

#### PlayerSlotPrefab
```
PlayerSlot
??? Background
??? PlayerNameText
??? SlotNumberText
??? HostCrown (icona)
??? ReadyCheckmark (icona)
```

### 5. Setup nella Scena Game

1. Aggiungi `GameSceneInitializer` a un GameObject
2. Assegna il riferimento al `TurnController`
3. Il componente leggerà automaticamente la MatchConfig dai PlayerPrefs

## Flusso Utente

### Quick Match
1. User preme "Gioca Online" nel ModalitySelector
2. Seleziona formato (1v1, 4P, 2v2)
3. MatchmakingManager si connette a Photon
4. Cerca stanza compatibile o ne crea una
5. Quando la stanza è piena ? carica GameScene

### Tavolo Privato - Crea
1. User preme "Gioca con Amici"
2. Preme "Crea Stanza"
3. Seleziona formato
4. MatchmakingManager crea stanza con codice
5. Mostra WaitingRoom con codice condivisibile
6. Host preme "Avvia" quando pronti

### Tavolo Privato - Unisciti
1. User preme "Gioca con Amici"
2. Preme "Unisciti"
3. Inserisce codice stanza
4. Entra nella WaitingRoom
5. Attende che l'host avvii

### Allenamento
1. User preme "Allenamento"
2. Seleziona formato e difficoltà
3. Parte subito (nessuna connessione)
4. GameSceneInitializer configura bot locali

## Enum Reference

```csharp
public enum MatchIntent
{
    QuickMatch,    // Coda pubblica
    PrivateRoom,   // Tavolo privato
    Training       // vs Bot
}

public enum GameFormat
{
    OneVsOne,      // 2 giocatori
    FourPlayers,   // 4 giocatori (default Cirulla)
    TwoVsTwo       // 4 giocatori a squadre
}

public enum BotDifficulty
{
    Easy,
    Medium,
    Hard,
    Expert
}
```

## Note Importanti

- **MatchConfig** viene salvato in PlayerPrefs quando si avvia una partita
- **GameSceneInitializer** legge la config e configura il GameModeService
- **GameModeService.Current** determina il provider (Training vs Multiplayer)
- Il **MasterClient** in Photon gestisce lo stato di gioco e sincronizza gli altri
