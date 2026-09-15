# UIV2 - Foundation

Nuova UI (mockup definitivo), costruita in parallelo alla UI legacy (`Assets/Scripts/UI`,
`Assets/Prefabs/UI`). La legacy non e' stata toccata. Questa fase copre solo la foundation:
shell + component library + un esempio data-driven (Friends). Nessuna schermata reale e'
stata completata.

## Struttura

```
Assets/UIV2/
  Prefabs/
    Core/        UIV2_Root.prefab (shell)
    Components/  libreria riusabile
    Screens/     FriendRow.prefab, FriendsScreenV2.prefab (esempio)
    Modals/      riservato (vedi _RESERVED.md)
    GameHUD/     riservato (vedi _RESERVED.md)
  Scripts/
    Core/        UIV2Root, UIV2Theme, UIV2ModalHost, IUIV2Modal
    Data/        view model (ViewData) - non nei prefab, passati a runtime
    Components/  script dei componenti riusabili
    Screens/     FriendRowView, FriendsScreenController
    Animations/  hook di animazione (IUIV2*, UIV2AnimatedComponent)
  Art/           riservato per sprite esportati dai mockup + UIV2Theme.asset
  Tests/         UIV2_Sandbox.unity + UIV2SandboxBootstrap.cs (verifica visiva con dati mock)
```

## Responsabilita'

- **UIV2Root** (`Scripts/Core/UIV2Root.cs`): locator statico per BackgroundLayer/SafeArea
  (TopBarHost/ScreenHost/BottomNavHost)/ModalHost/OverlayHost/FXHost. Nessuna logica di
  navigazione: solo riferimenti.
- **UIV2Theme** (`Scripts/Core/UIV2Theme.cs`, asset in `Art/UIV2Theme.asset`): design token
  (colori placeholder + slot Sprite vuoti da riempire quando l'export grafico e' pronto).
- **UIV2ModalHost / IUIV2Modal**: apertura/chiusura di un modal alla volta, senza conoscerne
  il contenuto (`UIV2ModalFrame` e' l'unica implementazione per ora).
- **UIV2Button**: un solo script per le 4 varianti bottone (`Style` enum); 4 prefab distinti
  (`UIV2_PrimaryGoldButton`, `UIV2_SecondaryBlueButton`, `UIV2_TealButton`,
  `UIV2_GreenSmallButton`) impostano solo lo stile.
- **UIV2SectionHeader / UIV2ProgressBar / UIV2ModalFrame / UIV2CollectionCard /
  UIV2ListRow / UIV2StatTile / UIV2AvatarBadge / UIV2PlayerRow / UIV2ResourcePill /
  UIV2TopBar / UIV2BottomNav**: componenti riusabili, ognuno con una API `Bind`/`Set*` che
  scrive solo contenuto - zero testo/nomi/numeri hardcoded nei prefab.
- **FriendRowView / FriendsScreenController**: riferimento concreto per il pattern
  data-driven richiesto (Populate(IReadOnlyList<FriendViewData>) - nessun amico nel prefab).
- **Animations** (`UIV2AnimatedComponent` + interfacce `IUIV2*`): le animazioni Show/Hide/
  Press/Selected/Unlock/Reward agiscono su `visualRoot` (child dedicato), mai sul
  RectTransform strutturale controllato da un LayoutGroup esterno.

## Dati vs mockup

Le view data (`Scripts/Data/*.cs`) descrivono la FORMA dei dati (es. `FriendViewData`), non
i valori. I valori mostrati nei mockup (Marco_88, 12.500 oro, ecc.) esistono SOLO in
`Tests/UIV2SandboxBootstrap.cs`, usato esclusivamente per la verifica visiva della sandbox
scene - nessun componente/prefab riusabile contiene dati finti imposti a fuoco.

## Come e' stato costruito

`Assets/Editor/UIV2FoundationBuilder.cs` (menu `Tools/UIV2/Build Foundation`) e
`Assets/Editor/UIV2SandboxSceneBuilder.cs` (`Tools/UIV2/Build Sandbox Scene`) generano tutto
in modo anchor/LayoutGroup-based (nessuna coordinata pixel assoluta legata a una
risoluzione), lavorando in una scena scratch mai salvata - a differenza dei builder legacy
Dragon's Hoard che scrivono coordinate fisse su una scena precisa. Rilanciare i due menu
rigenera prefab e sandbox da zero in modo deterministico.
