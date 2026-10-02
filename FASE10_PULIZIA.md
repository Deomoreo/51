# Fase 10, Pulizia: lista da confermare (02/10, versione 2.63)

Niente è stato ancora cancellato o modificato. Ogni blocco parte solo col tuo sì.
Per ogni voce ho controllato chi la usa ancora: riferimenti nelle scene (dal vivo in Unity), nei prefab e nel codice.

**Fuori dalla pulizia, restano:** tutto UI51 (script, builder, prefab, `UI51_Gallery.unity`), gli SDK esterni (PlayFab, Photon, Google Play),
`Assets/Mockup`, la grafica dei mazzi, l'audio, gli script che si installano da soli all'avvio (`UIV2MotionInstaller`, `UIV2ShaderKit`,
`UIV2FeedbackParticles` e quelli che aggiungono, come `UIV2HapticButton`, `UIV2PageEntrance`, `UIV2ShapeGlow`).

**Come lo faccio:** gli oggetti di scena li tolgo dall'Editor (non a mano nel file della scena); dopo ogni blocco ricompilo, lancio i test
EditMode e provo il percorso nel Simulator. Se qualcosa risulta ancora usato, lo rimetto e te lo dico.

---

## Blocco 1. La vecchia scena `HomeScreen.unity` e tutto quello che serviva solo a lei (consiglio: sì)

Scena di lavoro abbandonata a settembre (non è nei Build Settings, nessuno la apre). Nessun effetto sul gioco.

- Scena: `Assets/Scenes/HomeScreen.unity` (1,9 MB)
- Builder (`Assets/Editor/`): `AmiciBuilder`, `ClassificaBuilder`, `DeckPageBuilder`, `EmoticonKitBuilder`, `EmoticonScreenExactBuilder`,
  `EmoticonWireframeBuilder`, `HomeScreenBuilder`, `ImpostazioniBuilder`, `NegozioBuilder`, `PanelMazzoBuilder`, `PanelModalitaBuilder`,
  `PostaBuilder`, `PremiBuilder`, `ProfiloBuilder`, `DragonsHoardSprites` (aiuto usato solo da questi), `DragonsHoardThemeApplier`,
  `FrontendFlowBuilder` (spostamento una tantum da HomeScreen a MainMenu, già fatto)
- Script: `PanelAmiciController`, `PanelPremiController`, `PanelPostaController`, `PanelClassificaController`, `PanelImpostazioniController`,
  `PanelMazzoController`, `PanelModalitaController`, `HomeNavBarController`, `DeckPageController`, `OpenPanelShortcut`, `StubActionButton`,
  `Kit/UICollectionGrid`, `Kit/UICollectionCard`, `Kit/UIEquippedRow`, `Kit/UIHeaderWidget`, `Kit/UIScreenRoot`, `Kit/UITabsRow`
- Prefab: `Assets/Prefabs/UI/Kit/` (7 prefab, `UI_EquippedRow3` da solo pesa 460 KB). `Kit/UIBottomNavBar.cs` resta: lo usa `UIV2BottomNav`.
- Documenti: `Assets/UI_SPEC_Home.md`, `UI_SPEC_PanelMazzo.md`, `UI_SPEC_PanelModalita.md`, `UI_SPEC_Collezione.md`, `ASSET_MAPPING_Collezione.md`

## Blocco 2. Scene di prova vecchie (consiglio: sì)

- `Assets/Scenes/TESTPHOTN.unity` (spenta nei Build Settings) e `Assets/Scripts/Networking/NetworkTestUI.cs` (usato solo lì).
  Tolgo anche la riga spenta dai Build Settings.
- 5 scene di anteprima UIV2 in `Assets/UIV2/Tests/` (`UIV2_Sandbox`, `UIV2_HomeV2_Preview`, `UIV2_CollectionV2_Preview`,
  `UIV2_ProfileV2_Preview`, `UIV2_ShopV2_Preview`), i loro 5 script `*Bootstrap.cs` e il builder `UIV2SandboxSceneBuilder`
- Usati solo da quelle anteprime: `FriendsScreenController`, `FriendRowView`, `FriendViewData`, prefab `FriendRow` e `FriendsScreenV2`

## Blocco 3. Script e prefab che nessuno usa (consiglio: sì)

Nessun riferimento in scene, prefab o codice.

- Script: `Auth/LoginGateUI` (346 righe, sostituito da `AuthUIController`), `Gameplay/CardShineOverlayMove`,
  `Gameplay/ProceduralShineStripeTexture`, `UI/AnchorToPosition`, `UI/DebugUIRaycastLogger` (file vuoto), `UI/IModal`,
  `UI/PrivateRoomOptionsUI`, `UI/TabButtonUI`, `UI/PrimaryButtonUI`
- Dati UIV2 mai usati (`Assets/UIV2/Scripts/Data/`): `GameResultEntryViewData`, `LobbyPlayerViewData`, `MailMessageViewData`,
  `MatchmakingPlayerViewData`, `RankingEntryViewData`, `RewardViewData`, `RoundResultEntryViewData`, `ShopItemViewData`
- Prefab: `Assets/Prefabs/Card.prefab`, `Assets/Prefabs/MoveSelectionPanel.prefab`, `Assets/Prefabs/UI/SectionCard_1.prefab`,
  `Assets/Prefabs/UI/SettingsPanel.prefab`
- **Da decidere tu:** `Assets/UI51/Scripts/Data/CosmeticCatalog.cs` non è usato, ma è il modello del catalogo cosmetici (banner, cornici,
  dorsi, rarità) pensato per il futuro negozio. Lo tengo se non mi dici di toglierlo.

## Blocco 4. Oggetti spenti nelle scene che nessuno usa (consiglio: sì)

Nessun riferimento da script attivi (controllato dal vivo in Unity).

- **MainMenu:** `Canvas_Background` (vuoto), `hierarchyDumper` (strumento di debug, con lo script `Scripts/DEBUG/HierarchyDumper.cs`),
  il vecchio `Design` spento di `LoginPanel`, `LegalV2`, `DeleteAccountV2`, `LoadingView` (`Background` e `DesignArea`),
  i vecchi veli `Dim` spenti di `SearchMatch`, `LobbyHost`, `LobbyGuest`
- **GameScene:** `TurnIndicator`, `PanelPersonalizzaOverlay` (rimasto da un vecchio builder), `ConnectionNotice` (sostituito dagli avvisi UI51
  della Fase 9) con lo script `ConnectionNoticeV2`, `EmoticonQuickBar` (vecchia striscia emoticon), `TableTopBar/Background` e
  `TableTopBar/SettingsButton`, i vecchi `Blur`, `Veil` e `Dim` spenti di `RoundResults` e il `Dim` di `MatchResults`
- Builder che servivano solo a questi oggetti: `GameSceneStrayCleanup`, `UIV2FoundationBuilder.EmoticonQuickBar.cs`,
  `FrontendExpansionBuilder.Game.cs` (Connection Notice)

## Blocco 5. Vecchi disegni spenti ma ancora collegati al codice (consiglio: sì, piccole modifiche al codice)

Sono invisibili ma uno script ci scrive ancora sopra; per toglierli tolgo prima quei campi dal codice.

- **MainMenu, finestre online:** il vecchio `Design` di `SearchMatch`, `LobbyHost`, `LobbyGuest` (ci sono ancora i vecchi pulsanti di chiusura
  in `RoomFlowV2.CloseButtons`).
- **MainMenu, registrazione:** il vecchio `Design` di `RegisterPanel` (`AuthScreensV2` ci colora ancora le barre della password e il bagliore).
- **MainMenu, vecchia sala online:** `LegacyOnlineViews` (`WaitingRoomPanel`, `JoinRoomPopup`, `MatchmakingStatusUI`), sostituiti dalla
  Ricerca e dalla Sala privata UI51. Con loro: i campi in `GameLaunchController`, gli script `WaitingRoomUI`, `JoinRoomPopupUI`,
  `MatchmakingStatusUI`, `PlayerSlotUI`, i prefab `WaitingRoomPanel`, `JoinRoomPopup`, `PlayerSlot`, il builder `LobbyPrefabBuilder`,
  e `Assets/Scripts/LOBBY_SETUP_README.md`.
- **GameScene, risultati:** il vecchio `Design` di `RoundResults` e `MatchResults`. `MatchResultsV2` scrive ancora nelle vecchie righe
  (oltre 200 collegamenti) e ha la strada "senza UI51 usa i vecchi pannelli": tolgo quella strada e i campi.
- **GameScene, sorteggio del mazziere:** la vecchia roulette (`DealerRoulette/Design`, Blur, Veil, nuvolette dei posti). Oggi gira sempre la ruota
  UI51 (a 2 e a 4 giocatori; a 3 non si gioca mai). Già approvato da te alla Fase 6: tolgo il vecchio pannello e la sua strada in
  `DealerRouletteController`.
- **GameScene, barra in alto:** `HandText` e `CardsLeftText` spenti, scritti ancora da `TableTopBarController`.

## Blocco 6. ⚠ Trovato durante il controllo: la vecchia pagina Notizie è ancora raggiungibile

Nel registro c'era scritto "`NewsV2` non più raggiungibile", ma non è così: il pulsante **Notizie della schermata d'inizio**
(quella con Ospite / Accedi / Registrati) apre ancora la vecchia pagina `NewsV2`. La Home invece apre già la pagina UI51.
**Proposta:** collego quel pulsante alla pagina Notizie UI51 e poi tolgo `NewsV2`, `NewsScreenV2`, `NewsItemViewV2` e il builder
`UIV2FoundationBuilder.News.cs`. Va provato che la pagina UI51 si apra bene anche prima dell'accesso (da ospite non ancora entrato).

## Blocco 7. Vecchia HUD della Home (consiglio: decidi tu, è il pezzo più delicato)

Spenta da settimane ma ancora collegata: `Canvas_TapToEnter`, `Canvas_Overlay` (vecchia barra in alto e `SettingsModal`),
`Canvas_Static` (vecchia barra in basso), `Canvas_Dynamic` (`MainHud`, `DeckPanelRoot`, `ModePanelRoot`), `ModalManager`, `GameData`;
prefab `MainHud` (con `DeckPanel`, `HomePanel`, `ProfilePanel`, `ShopPanel`), `BottomNavBar`, `TopBar`, `ModalManager`, `SettingsModal`,
`IconLoading`; una ventina di script (`MainHudController`, `PanelSwipeController`, `BottomNavBarUI`, `BottomNavController`, `TopBarUI`,
`BannerUI`, `HomePanelUI`, `DeckSelectorPanelUI`, `ModalitySelectorPanelUI`...).

Il nodo: la Home UI51 si appoggia ancora a `ModalitySelectorPanelUI` (vecchio pannello Modalità, invisibile) per **ricordare la modalità e il
formato scelti** e passarli alla partita; `HomeV2Integration` si spegne se mancano la vecchia barra o le vecchie pagine (che però non usa).
Toglierla vuol dire spostare la scelta della modalità in un piccolo oggetto di dati e riprovare tutti i percorsi di partenza (allenamento,
online 1v1/2v2/1v3, stanza privata, rivincita). Due strade:
- **7a.** La faccio adesso, per ultima, con la prova completa dei percorsi.
- **7b.** La lascio così (spenta, non costa niente al telefono) e la metto tra i debiti tecnici.

## Blocco 8. Vecchi pannelli Crea stanza ed Entra (bloccato da una tua scelta aperta)

`OnlineFlowV2/CreateRoom` e `JoinRoom` (disegno UIV2) sono ancora usati: **INVITA dalla pagina Amici apre il vecchio "Crea stanza"**
(punto rimandato alla 2.45). Si possono togliere solo dopo che decidi dove si sceglie il formato quando inviti un amico:
- **8a.** Si apre il pannello Modalità UI51 sopra Amici, sulla scheda Stanza privata.
- **8b.** La stanza si crea subito nell'ultimo formato usato.
- **8c.** Per ora restano.

## Blocco 9. Builder vecchi che rifarebbero l'aspetto UIV2 (consiglio: sì, tranne quelli indicati)

Rilanciarli oggi rimetterebbe il vecchio aspetto sopra UI51: tolti, non si può più rigenerare la grafica UIV2 (ma resta in git).

- Parti di `UIV2FoundationBuilder` (`Assets/Editor/UIV2FoundationBuilder.*.cs`): `Accuso`, `AnimatedEmoticons`, `Auth`, `Collection`,
  `ComingSoon`, `Dealer`, `DealerReveal`, `DeleteAccount`, `HomeQuickActions`, `HomeSettings`, `IconSetV2`, `Legal`, `Online`, `Profile`,
  `QuickDeckPolish`, `ResultsBlur`, `RoomTable`, `Settings`, `Table` (più `EmoticonQuickBar` e `News` già nei blocchi 4 e 6)
- **Restano:** il file principale `UIV2FoundationBuilder.cs`, `Account` (pagina Account), `Shop` (il Negozio non ha ancora una versione UI51),
  `HomeAmbient` (sfondo della Home UI51), `Glows`, `Motes`, e per la schermata d'inizio ancora UIV2 `StartScreenAuthButtonsBuilder` e
  `GoldButtonLabelStyle`
- Altri builder una tantum già passati: `BackdropDismissBuilder`, `BottomNavPolishBuilder`, `QuickModeRowsCalibration`,
  `FrontendExpansionBuilder.cs`, `GameCanvasResolutionFixer`, `TableActionButtonsBuilder`, `TableDesignAreaBuilder`, `TableFeltBuilder`,
  `TablePlayerBannersBuilder`, `TableTopBarBuilder`, `IconFaceCentering`, `PanelTitlesCalibration`, `PoppinsMigration` (rilanciato rimetterebbe
  Poppins su tutto), `UIV2FeedbackBuilder`, `UIV2ReducedGraphicsBuilder`, `UIV2ShaderBuilder`, `UIV2DesignBuilder`, `SceneSetup/CreateCirullaScene`,
  `Tools/FixGameManagerScenes`
- `UIV2MotionBuilder`: lo usa un test (`UIV2MotionTests`); lo tolgo e il test chiama direttamente `UIV2MotionInstaller`.
- Prefab UIV2 mai usati: `UIV2_CollectionCard`, `ContentPanel`, `FlatButton`, `IconButton`, `ListRow`, `ModalFrame`, `PlayerRow`,
  `SectionHeader`, `StatTile`, `TealButton` (`FlatButton` lo carica un test: sistemo il test) e i loro script se restano senza usi.

## Blocco 10. Documenti vecchi (consiglio: sì)

- `Assets/Networking/` (26 file .md, 300 KB, la cartella contiene solo documenti): diari della prima versione online (BUGFIX, SUMMARY,
  BUILD_STATUS, NEXT_STEPS...). **Proposta:** tolgo i 20 diari e tengo i 6 di riferimento (`ARCHITECTURE`, `EVENTS_REFERENCE`,
  `SETUP_PHOTON_PUN2`, `MULTIPLAYER_README`, `READY_SYSTEM_GUIDE`, `CODE_EXAMPLES`).
- `Assets/Scripts/UI/README_UIFoundation.md`, `Assets/UIV2/README.md` (dice ancora "nessuna schermata completata"),
  i tre `_RESERVED.md` vuoti di `Assets/UIV2`
- Restano: `UI_SPEC_Tavolo.md`, `RoundEndPanel_README.md`, `README_AUTH.md` (ancora in parte validi)

## Blocco 11. Immagini e file grandi (decidi tu voce per voce)

- **`Assets/Screenshots`**: 125 catture di prova (26,8 MB), nessuno le usa. Consiglio: sì.
- `Assets/UIV2/Art`: 3 materiali LiberationSans non usati. Consiglio: sì.
- Nella cartella DragonsHoard: 11 "Immagine ChatGPT 25 set..." (una copia ingrandita da 6,1 MB), non collegate a niente. Le hai fatte tu: decidi tu.
- `Assets/Art`: `SecondBanner.png` (2,6 MB), `TapToEnterBtn.png`, "ChatGPT Image...", `New Material.mat`, `New Render Texture`, `Generated/*`.
  Non usati. Decidi tu.
- `Assets/2D Casual UI/GUI.psd` (4,4 MB, avanzo dell'Asset Store). Decidi tu.
- Restano in ogni caso: `Assets/Mockup`, `Art/Decks` (originali dei mazzi), le icone `ic_*` (le cercano per nome i builder UI51), l'audio.
