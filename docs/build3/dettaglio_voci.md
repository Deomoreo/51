# Build 3 — dettaglio per voce (generato dalla diagnosi del 07/10)

Materiale di lavoro per i blocchi di `DIAGNOSI.md`: per ogni voce la causa trovata dall'agente, le prove (file:riga), la correzione proposta e il giudizio del verificatore indipendente. In inglese perché è materiale tecnico. Le righe si riferiscono al codice del 07/10 (2.65 non committata): ricontrollarle prima di ogni modifica. Si cancella a Build 3 chiusa.


## Turno / input / spam carte

### T3 — root_cause_confirmed · verifier: confirmed · size S

**Current behaviour.** A card tapped while the game state already says it is your turn, but the table is still busy (cards being dealt, the 5 s ACCUSA window, the accuso punch, the end of the opponent's crossfade), is stored and played by itself as soon as the table is free. This happens on every hand where the local player plays first (the dealer's opponent in 1v1), which is why the user saw it only 'a volte'.

**Root cause.** This was deliberate (v2.10, memory feedback_onmousedown_ui_passthrough: 'Local human taps made while busy are buffered in pendingLocalMove and replayed from Update'), and the user's report now overrides it. Two things combine. (1) Every input check uses only the game state: CardView.IsLocalPlayersTurn and CardViewManager.IsMyTurnToPlay check IsHumanPlayerTurn plus IsLocalPlayer(CurrentPlayerIndex), and neither looks at the busy flags. At the start of each hand CurrentPlayerIndex already points to the first player, while isRedealPendingVisual or isRedealAnimationInProgress stay true through the whole deal and accuso window. RunAccusoWindowCoroutine calls ForceRefresh with suppression off, so RenderHumanHand makes the visible hand clickable. (2) ExecuteMove does not drop a local tap made while busy: it stores it in pendingLocalMove, and Update replays it at the first free frame. In multiplayer the replay goes through the normal send path, so the card is actually sent online.

**Evidence.**
- `Assets/Scripts/Gameplay/TurnController.cs:1153-1174` localAnimationBusy branch: a local human move becomes pendingLocalMove = move (1172) instead of being dropped
- `Assets/Scripts/Gameplay/TurnController.cs:1276-1283` Update replays pendingLocalMove via ExecuteMove as soon as no busy flag is set
- `Assets/Scripts/Gameplay/TurnController.cs:713, 764-765, 778-779` DeclareInitialAccusiWithDelay: isRedealPendingVisual true for roulette + deal + accuso window, cleared in finally; then PlayYourTurnCue (794)
- `Assets/Scripts/Gameplay/TurnController.cs:1579, 1658-1659, 1673-1675` HandleNewHandsRevealSequence (mid-smazzata redeal): isRedealAnimationInProgress for deal + accuso window
- `Assets/Scripts/Gameplay/TurnController.cs:1085-1091` RunAccusoWindowCoroutine: SetSuppressNewCardVisibility(false) + ForceRefresh, then the 5 s window, with the hand visible and clickable
- `Assets/Scripts/Gameplay/CardViewManager.cs:1172-1174, 1201, 1218` IsMyTurnToPlay is state-only and is written into IsClickable at every refresh
- `Assets/Scripts/Gameplay/CardView.cs:231-253, 472-482` OnMouseDown gate = isClickable + IsLocalPlayersTurn (state-only), then haptic + OnCardClicked
- `Assets/Scripts/Gameplay/CardViewManager.cs:1466-1543` OnHumanCardClicked: same state-only check, then ExecuteMove and SetSelected(false); the user sees the card bounce, then it plays seconds later
- `Assets/Scripts/Gameplay/TurnController.cs:1189-1198` MP: the replayed move passes RefreshValidMoves/Contains and is sent (OnLocalPlayerMoveRequested -> NetworkGameController.SendMove AllViaServer)
- `Assets/Scripts/Gameplay/TurnController.cs:1465-1474` Smaller window: ApplyMoveInternal hands the turn to the local player before the 0.08 s crossfade, while isMoveAnimationInProgress is still true

**Fix sketch.** One live gate in TurnController, used at the input entry points, and drop the buffer.
```csharp
/// Il giocatore di questo telefono puo' giocare ora: suo turno, tavolo fermo, nessuna mossa gia' mandata.
public bool AcceptsLocalInput => gameState != null && !gameState.RoundEnded && !halted
    && IsHumanPlayerTurn && GameModeService.Current.IsLocalPlayer(CurrentPlayerIndex)
    && !IsBusy && !isDealFlightPending && !isAccusoWindowOpen
    && !(sentKey == TurnKey() && sentState == gameState);
```
- TurnController.ExecuteMove: delete the `else if (local human) pendingLocalMove = move;` branch (a local move made while busy is dropped). Delete the pendingLocalMove field (253), the Update replay (1276-1283), its term in IsBusy (426), and its resets at 69, 434 and 567.
- CardView.IsLocalPlayersTurn (480): return _turnControllerCache.AcceptsLocalInput.
- CardViewManager.OnHumanCardClicked (1468) and OnHumanCardDoubleClicked (1392): check turnController.AcceptsLocalInput instead of IsMyTurnToPlay.
- Leave IsMyTurnToPlay as it is for IsClickable, hints and tray-cancel, which are computed at refresh time. The gate must be read live at tap time: the finally blocks call ForceRefresh BEFORE clearing isRedealPendingVisual, so baking the gate into IsClickable would leave the hand unclickable for the whole turn.

**Files.** Assets/Scripts/Gameplay/TurnController.cs, Assets/Scripts/Gameplay/CardView.cs, Assets/Scripts/Gameplay/CardViewManager.cs

**Risk.** - A capture choice tapped in the tray at a moment when the table is busy is now dropped silently (it closes the tray), where before it was queued. This is rare, because the tray only opens through the gate.
- TurnController.IsBusy loses the pendingLocalMove term. Its readers become free earlier and that is harmless: MatchResultsV2.cs:205 (forfeit check), NetworkGameController.cs:550 (SendStateWhenSettled) and TickTurnTimer:404.
- The turn-timer auto-move (TickTurnTimer, local) only fires when !IsBusy, so it is unaffected.
- Tutorial: UI51TutorialView.cs:99 already waits for !IsDealInProgress && !IsAccusoWindowOpen.
- The gate also hides taps during the network round trip (sentKey). See T4.

**Test plan.** - EditMode test (new): AddComponent<TurnController> as in TurnControllerResolveAIDifficultyTests. Set the private gameState via reflection to Rules51.CreateNewGame(2) with CurrentPlayerIndex = 0 (single-player provider). Set isRedealPendingVisual = true and assert AcceptsLocalInput is false. Call ExecuteMove(valid local move), clear the flag, and assert IsBusy is false and no animation is pending. Then assert AcceptsLocalInput is true.
- Play Mode in Editor: Training 1v1 on a smazzata where the bot deals. Tap a hand card during 'MANO 1 DI 6' and the accuso window; after the window the card must NOT be played.
- Repeat with the 'Fake online table' recipe, checking that no SendMove log appears.
- Device check: the same flow on a phone.

**Verifier (confirmed).** I traced the whole path myself. At the start of every smazzata, CurrentPlayerIndex is the dealer's left/opponent before any visual (Rules51.cs:78 and :162). DeclareInitialAccusiWithDelay sets isRedealPendingVisual=true (TurnController.cs:713) and clears it only in the finally (782), after RunAccusoWindowCoroutine (765). The mid-smazzata redeal does the same through HandleNewHandsDealt/HandleNewHandsRevealSequence (1571, 1579, cleared 1673-1675). RunAccusoWindowCoroutine turns suppression off and calls ForceRefresh (1087-1088). RenderHumanHand then writes IsClickable = IsMyTurnToPlay, which is state-only (CardViewManager.cs:1172-1175, 1218), and re-enables the renderers (1225). No UI blocks the hand during the window: the AccusoWindowPrompt CanvasGroup has m_BlocksRaycasts: 0 and is only 580x62 (GameScene.unity:47096-47127). So the CardView.OnMouseDown gate passes (isClickable plus the state-only IsLocalPlayersTurn, CardView.cs:233-236, 472-482), and so does OnHumanCardClicked (CardViewManager.cs:1468). ExecuteMove sees localAnimationBusy and stores the tap in pendingLocalMove (TurnController.cs:1153-1173). Update replays it at the first free frame (1276-1283). In single player it passes currentValidMoves (already refreshed at 1548 or 607). In MP it goes through RefreshValidMoves, the sentKey check and OnLocalPlayerMoveRequested (1187-1198), then SendMove AllViaServer (NetworkGameController.cs:595). I found no guard on the server or on other clients that refuses it: the move is valid. The memory quote about v2.10 buffering is accurate (feedback_onmousedown_ui_passthrough.md). The 0.08 s crossfade window (1465-1474) is also real.

- `Assets/Scenes/GameScene.unity:47116-47127` AccusoWindowPrompt CanvasGroup m_BlocksRaycasts: 0, so IsPointerOverUI does not stop hand taps during the 5 s window
- `Assets/Scripts/Core/Rules51.cs:78, 162` CurrentPlayerIndex = first player is set at deal time, before any visual
- `Assets/Scripts/Gameplay/TurnController.cs:1277, 1509` Replay also requires pendingNetworkMoves.Count == 0. The finally starts a queued network move first, so a buffered tap can outlive another player's move and fire on the next own turn (see missed_issues)

**Fix concerns.** The fix is sound. Notes:
- The isDealFlightPending and isAccusoWindowOpen terms in AcceptsLocalInput are redundant. Both are always set inside an isRedealPendingVisual or isRedealAnimationInProgress span (713-714/763/782; 1571-1572/1650/1673-1675; 1090/1103 run inside those coroutines). They are harmless and can be dropped for a shorter gate.
- The gate is checked only at tap time. The tray's onChoose callback (CardViewManager.cs:1604-1613) calls ExecuteMove directly, so a choice made while busy is now dropped silently, as the risk note says.
- A tap in the 0.08 s crossfade, after the turn is already shown as local (TurnController.cs:1465-1474), is now lost with no feedback. This is acceptable but should be the intended UX.
- Test plan: ExecuteMove needs currentValidMoves, and RefreshValidMoves is private (1127), so the EditMode test must call it via reflection or set the field.

### T4 — root_cause_confirmed · verifier: confirmed · size S

**Current behaviour.** No double move reaches the game state today. The remaining problem is that the UI is not locked after a valid move. In single player, a second card tapped while the first is flying rises and drops. If that card has several capture options, the SCEGLI LA PRESA tray reopens for the old state, and is closed by the next refresh. In multiplayer, between sending a move and the server echo, the hand stays fully interactive: the tapped card drops straight back into the hand, other cards can be selected, and the tray can open. Every ignored tap still vibrates.

**Root cause.** There is no 'input locked' state between a valid move and the moment the game is ready again. The game state is protected: the 2.56 one-move-per-turn guard (sentKey/TurnKey), validation against currentValidMoves on every client, and Move.Equals includes PlayerIndex, so a replayed or extra move from the wrong player is rejected. The UI, however, decides with the same state-only check as T3. Until ApplyMoveInternal runs at the END of the animation (or until the server echo in MP), CurrentPlayerIndex is still the local player. So every tap passes OnMouseDown (and fires a haptic), runs SetSelected and the capture-option logic, and only then hits ExecuteMove, which buffers it (single player) or returns at the sentKey guard (MP).

**Evidence.**
- `Assets/Scripts/Gameplay/TurnController.cs:1189-1198` MP: one move per turn guard (sentKey == TurnKey && sentState == gameState), so the second move is never sent
- `Assets/Scripts/Gameplay/TurnController.cs:1230-1245` Same validation on every client; Move.Equals compares PlayerIndex (Assets/Scripts/Core/Move.cs:32)
- `Assets/Scripts/Gameplay/TurnController.cs:1340-1343, 1465` ExecuteMoveWithAnimation: state (CurrentPlayerIndex) changes only at ApplyMoveInternal, after 0.35 s (PlayOnly) to 1.2 s (capture: CardAnimationController.cs:18-20)
- `Assets/Scripts/Gameplay/CardViewManager.cs:1484-1487, 1567-1600` Second card tapped mid-flight: selected, and ShowCaptureOptions can open the tray with options computed for the stale state
- `Assets/Scripts/Gameplay/CardViewManager.cs:935-938` The stale tray is only closed at the next refresh, when !IsMyTurnToPlay
- `Assets/Scripts/Networking/NetworkGameController.cs:566-596` SendMove: AllViaServer, so locally nothing changes until the echo RPC_ExecuteMove (657-697); meanwhile the hand stays live
- `Assets/Scripts/Gameplay/CardView.cs:240` Haptic fires before any turn or busy check beyond the state-only one, so ignored taps vibrate

**Fix sketch.** Same block as T3: the live gate TurnController.AcceptsLocalInput (it includes isMoveAnimationInProgress through IsBusy, and the sent-move key) checked in CardView.OnMouseDown before the haptic and in both CardViewManager click handlers. After the first valid tap, every later tap is ignored at the source (no haptic, no select, no tray) until the turn is really ready again. No new system.
Optional polish, user decision: keep the sent card raised until the MP echo instead of dropping it at once (CardViewManager.cs:1529-1530, 1542-1543). On 4G the drop makes the tap look lost and invites the spam behind T2. If done, it must deselect before ApplyMoveInternal: a selected card is not moved by SetPosition (CardView.cs:603).

**Files.** Assets/Scripts/Gameplay/TurnController.cs, Assets/Scripts/Gameplay/CardView.cs, Assets/Scripts/Gameplay/CardViewManager.cs

**Risk.** Same as T3.
- In MP, if the echo never arrives (connection lost), the gate stays closed until SetConnected(true) clears sentKey (TurnController.cs:376-380). That is the same lifetime as the existing send guard.
- The tray's Annulla/X path is unaffected.

**Test plan.** - EditMode (same new test class): after ExecuteMove of a valid single-player move, assert AcceptsLocalInput is false while isMoveAnimationInProgress is set. With a fake MP provider and sentKey set, assert it is false.
- Play Mode: SetupScenarioForCurrentPlayer with a card that has 2+ capture sets. Play another card, then invoke OnHumanCardClicked on the multi-capture card during the flight: the tray must not open and no card must rise.
- Device check: rapid taps on several cards, single player and online over 4G.

**Verifier (confirmed).** Confirmed. The game state is protected: in MP the sentKey/TurnKey one-move-per-turn guard applies (TurnController.cs:1194-1197); every client validates moves (1236); Move.Equals compares PlayerIndex (Move.cs:31). In single player a tap buffered during your own flight is replayed at the first free frame and rejected, because CurrentPlayerIndex is now the bot's. ApplyMove always advances the turn (Rules51.cs:406), and the bot cannot move earlier (ExecuteAITurn is Invoked after aiMoveDelay and polls GamePresentation.IsBusy, 1719-1722). The UI is not locked. CurrentPlayerIndex changes only in ApplyMoveInternal (1465/1524), after PlayCardToTable (0.35 s) or after preview plus capture (0.45 s + 0.4 s) (CardAnimationController.cs:18-20). Until then, OnMouseDown passes (CardView.cs:233-240, including the haptic), and OnHumanCardClicked selects the card and can open the tray using the pre-move currentValidMoves (CardViewManager.cs:1482-1487, 1536). The stale tray closes only at the next refresh (935-938). In MP, ExecuteMove returns at the sentKey guard and OnHumanCardClicked then deselects everything (1529-1530, 1542-1543), so the hand stays live until the echo.

- `Assets/Scripts/Core/Rules51.cs:225-244` Captures are mandatory per card, not per hand. A non-capturing card A can be played as PlayOnly while card B still has multiple capture sets, so the tray can open on B during A's flight (see missed_issues)
- `Assets/Scripts/Gameplay/TurnController.cs:378-382` SetConnected(true) clears sentKey, the only release of the MP send lock other than a new TurnKey

**Fix concerns.** Same as T3.
- The gate stops clicks, the haptic and the tray, but not hover. TryBeginHover (CardView.cs:389-403) still raises a touched hand card during the flight. That is harmless visually, but hover coroutines then remain one of the T2 sources (see T2).
- The optional 'keep the sent card raised' polish must deselect before ApplyMoveInternal. As the diagnosis says, SetPosition skips selected cards (CardView.cs:603) and RenderTableCards would leave the view raised in the hand slot.

### T2 — root_cause_confirmed · verifier: partially_confirmed · size S

**Current behaviour.** Spam taps on a card that is already flying still reach its view: the view is hidden but its collider stays live, and the state still says it is your turn. Each tap starts a select/deselect pose animation (0.12 s) or a hover animation (0.1 s). If the move commits (ApplyMoveInternal -> ForceRefresh) during one of these animations, RenderTableCards moves the view to its table slot and gives it table sorting order 10+i. The still-running animation then snaps the view back to its OLD hand position and scale. The card ends up hand-sized, behind the hand cards (order 40+), and stays there until a later move changes the table layout. This matches the report 'resta dietro la mano fino alla mossa successiva'.

**Root cause.** Two defects meet.
(1) Input is not gated during the move animation (see T3/T4). The played card's CardRenderer is disabled, but its collider and isClickable stay on, and IsLocalPlayersTurn is still true, so taps keep calling OnHumanCardClicked: SetSelected(true) then SetSelected(false), or the double-tap path.
(2) CardView.SelectionCoroutine and HoverCoroutine capture targetPos/targetScale from originalPosition/displayScale ONCE at start, and assign them at the end. SetPosition/SetDisplayScale update originalPosition and move the transform, but do not stop or retarget a running pose coroutine, so the old pose wins. SetPosition then early-outs on later refreshes because originalPosition already equals the new slot, so nothing corrects the transform until the table positions change (GlideTableCards or a new layout).
Deterministic variant: a spam tap on a multi-capture card reopens the tray during its own flight. RefreshCardViews cancels the tray BEFORE re-laying out the table; the cancel callback starts SelectionCoroutine(false) aimed at the hand position, and the card always ends up behind the hand. With PlayOnly cards it depends on timing: about one tap in the last 0.12 s of a 0.35 s flight.

**Evidence.**
- `Assets/Scripts/Gameplay/TurnController.cs:1369` Only the renderer of the played view is disabled; its collider stays tappable during the flight
- `Assets/Scripts/Gameplay/CardView.cs:543-570` SelectionCoroutine: targetPos/targetScale computed once (547-549), forced at the end (567-568)
- `Assets/Scripts/Gameplay/CardView.cs:643-663` HoverCoroutine: same stale-target pattern (649, 660-661)
- `Assets/Scripts/Gameplay/CardView.cs:595-614` SetPosition: early-out when unchanged (597); moves the transform but never stops a running pose coroutine
- `Assets/Scripts/Gameplay/CardViewManager.cs:935-938` RefreshCardViews cancels the tray first, so the cancel callback (1614-1621) deselects with the OLD originalPosition
- `Assets/Scripts/Gameplay/CardViewManager.cs:1072-1096` RenderTableCards for the played view: EnableHover false, renderer re-enabled (1085), SetDisplayScale/SetPosition to the table, SetBaseSortingOrder(10+i), below hand cards at CenterFirstSortingOrder(40, ...) (1228)
- `Assets/Scripts/Gameplay/CardViewManager.cs:1484-1487, 1529-1530, 1542-1543` Each spam tap runs SetSelected(true) then SetSelected(false) on the in-flight card
- `Assets/Scripts/Gameplay/CardView.cs:244-253` Alternate taps become double-clicks: OnPlayerDoubleClick, then ExecuteMove, then pendingLocalMove (single player)
- `Assets/Scripts/Gameplay/CardViewManager.cs:908-913` Only the next move's GlideTableCards (or a new table position) corrects the stale view, hence 'until the next move'
- `Assets/Scripts/Gameplay/CardViewManager.cs:1225` HYPOTHESIS, secondary path: any ForceRefresh during a local flight (e.g. TurnController.cs:115-118 OnPlayerConvertedToBot) re-enables the hidden played card in the hand while its copy flies

**Fix sketch.** (a) The T3/T4 gate removes the spam taps on the in-flight card at the source.
(b) Fix the stale-target pose animations once, where every caller passes through: read the rest pose every frame instead of capturing it.
```csharp
// SelectionCoroutine loop and end; HoverCoroutine the same with hoverRaiseAmount / hoverScaleMultiplier
var targetScale = select ? displayScale * 1.12f : displayScale;
var targetPos = select ? originalPosition + Vector3.up * Raise : originalPosition;
```
(move both lines inside the while loop and recompute them before the final assignment). A SetPosition, SetDisplayScale or GlideTo during a pose animation then retargets it instead of being overwritten. About 6 lines in CardView.cs; no change to SetPosition's early-out, which keeps GlideTo uninterrupted.

**Files.** Assets/Scripts/Gameplay/CardView.cs, Assets/Scripts/Gameplay/TurnController.cs, Assets/Scripts/Gameplay/CardViewManager.cs

**Risk.** - A hand re-layout during a deselect now slides the card to the new slot within 0.12 s instead of snapping. That is visually better.
- Matta width is handled in LateUpdate (SetFlipFactor, CardView.cs:1043-1045) and GetPoseScale, so reading displayScale per frame does not change it.
- The K5TableTests Explicit pose tests (163-188) keep originalPosition fixed during the animation, so they are unaffected.
- No other callers of the coroutines.

**Test plan.** - Explicit [UnityTest] in K5TableTests style: SetSelected(true); SetSelected(false); then in the same frame SetDisplayScale(.5f) and SetPosition(new Vector3(3,0,0)); wait 0.25 s; assert the transform is at (3,0,0) with scale .5.
- Play Mode repro: SetupScenarioForCurrentPlayer with a 2-capture-set card. Choose option 0 via the tray, then invoke private OnHumanCardClicked on the same view once 0.2 s later. Before the fix the view ends at its hand position with sortingOrder 10+i; after the fix (with the gate) the tray does not reopen and the card sits on its table slot.
- Device check: spam-tap a card 6-8 times per second, in training and online.

**Verifier (partially_confirmed).** The main mechanism is confirmed for PlayOnly flights. Only the renderer of the played view is hidden (TurnController.cs:1369); its collider stays at the hand slot, and isClickable plus the state-only turn check stay true. Each single tap runs SetSelected(true) then SetSelected(false) in the same frame (CardViewManager.cs:1486, 1542-1543). Each double tap skips selection but still runs hover in and out. SelectionCoroutine and HoverCoroutine capture their targets once (CardView.cs:547-549, 648-649) and write them at the end (567-568, 660-661). When ApplyMoveInternal runs ForceRefresh, RenderTableCards sets EnableHover=false, re-enables the renderer, and calls SetDisplayScale, SetPosition and SetBaseSortingOrder(10+i) (CardViewManager.cs:1074-1096). The stale coroutine then snaps the view back to the old hand pose. SetPosition and SetDisplayScale skip later calls because their values are unchanged (CardView.cs:211, 597). Only the next move's GlideTableCards/GlideTo corrects it (TurnController.cs:1381, CardView.cs:620-630). SnapToRestPose (CardView.cs:498) has no callers.

The 'deterministic variant' is refuted as written. It assumes a multi-capture card. A card played as a capture goes to CapturedCards, not the Table (Rules51.cs:377-388), and CleanupOldViews destroys its view in the same RefreshCardViews, right after the tray Cancel (CardViewManager.cs:943, 1010-1031; CardView.cs:692-701). It cannot end up behind the hand. The Play Mode repro in the test plan (choose option 0, then tap the same view) would therefore show a destroyed view, not a stuck one.

The deterministic case does exist for a different card; see corrected_root_cause.

**Corrected cause.** There are two defects, as stated: input is not gated during the move flight, and pose coroutines capture stale rest-pose targets. They affect more than the same card.

(1) PlayOnly card A. A spam tap on its hidden view in the last 0.12 s (select/deselect) or 0.1 s (hover) of the 0.35 s flight leaves A at its old hand pose with table sorting order 10+i. This is the reported 'dietro la mano'.

(2) Deterministic: another hand card B that can capture (captures are per card, Rules51.cs:225-244) is tapped during A's PlayOnly flight, and the tray opens for B. At commit, RefreshCardViews cancels the tray (CardViewManager.cs:935-938) BEFORE RenderHumanHand re-lays out the hand (957, 1239). B's SelectionCoroutine(false) therefore drives B back to its OLD slot. It stays misplaced and overlapping until the hand count changes again.

(3) Hover coroutines started by touches are not gated by any input check: TryBeginHover (CardView.cs:389-403), and EndHover on finger release via Update (CardView.cs:276-280, 425-436). These include the original tap's hover-out when the finger is held near commit. The input gate (a) alone therefore cannot remove all stale pose animations; fix (b) is required.

- `Assets/Scripts/Core/Rules51.cs:372-388` PlayOnly puts the card on the Table; a capture puts the played card in CapturedCards, so its view is destroyed at commit
- `Assets/Scripts/Gameplay/CardViewManager.cs:943, 1010-1031` CleanupOldViews runs right after the tray Cancel and destroys the captured played card's view
- `Assets/Scripts/Gameplay/CardView.cs:84-88, 433` The EnableHover setter does not stop a running hover; EndHover animates out only if enableHover is true
- `Assets/Scripts/Gameplay/CardView.cs:498-503` SnapToRestPose exists but has no callers
- `Assets/Scripts/Gameplay/CardViewManager.cs:935-938, 957, 1239` Tray cancel (deselect toward the old slot) runs before RenderHumanHand re-lays out the remaining hand cards

**Fix concerns.** Fix (b) as sketched leaves a hole.
- With live targets, a hover-in coroutine (enter=true) that is running at commit keeps aiming at originalPosition + hoverRaiseAmount at x1.08 scale. RenderTableCards sets EnableHover=false (CardViewManager.cs:1074), but the setter cancels nothing (CardView.cs:84-88). EndHover then no longer animates out (CardView.cs:433). The played card lands raised and enlarged on the table until the next glide.
- This path survives fix (a), because hover is never gated.
- Use `enter && enableHover` for the live hover target, or stop the pose coroutines when a view moves hand to table in RenderTableCards (reusing the dead SnapToRestPose plus StopPoseAnimations).
- Live targets with a fixed startPos also cause a brief backward jump, from the table toward the hand midpoint and back, within 0.12 s. Minor.
- Test plan: replace the tray repro with (i) a PlayOnly card and a tap on its view at about 0.25-0.34 s, and (ii) the deterministic B-tray repro: open the tray on capture card B during A's PlayOnly flight, then check B's transform against its new slot.

### T1 — feature_missing · verifier: confirmed · size M

**Current behaviour.** Today the local turn is signalled by:
(a) Own banner: border to gold .85 plus a pulse ring (7 px, 1.8 s, alpha .55), polled every 0.2 s from CurrentPlayerIndex. With reduced graphics the ring stays off (UIAnim.DecorativeLoops) and only the border colour changes.
(b) A soft gold glow behind the non-capture hand cards (alpha .5 x .7-1), and the cyan move hint if 'Suggerimenti mosse' is on. Both are off with reduced graphics.
(c) your_turn.wav at volume .55, only if effects are on. HYPOTHESIS: on iPhone it is likely muted by the silent switch.
(d) No haptic at turn start. No text: the top bar is score-only by the user's decision of 29/09. The turn timer appears only online and only in the last 5 s.
(e) Signals disagree in time: the banner gold and the hand glow switch on as soon as the state says it is your turn (during the deal and the 5 s accuso window), but the sound plays only after the window. The user cannot learn when a tap will count, which ties into T3.

**Root cause.** There is no prominent 'your turn' moment. The only indicators are small (banner border/ring, faint glow) or optional (sound, graphics level), and none uses text or haptics. All of them read CurrentPlayerIndex (state), not input readiness, so they light up while input is not really ready. There is no single 'turn starts now' hook: PlayYourTurnCue is called from 3 places with different conditions.

**Evidence.**
- `Assets/Scripts/UI/PlayerBannerManager.cs:60, 403` Banner turn = InvokeRepeating Refresh every 0.2 s, SetTurn(p == CurrentPlayerIndex, p != local), state-driven
- `Assets/UI51/Scripts/Components/PlayerBanner.cs:146-175, 210-213` SetTurn: border GoldA(.85) + UIAnim.Pulse(ring) defaults 7 px / 1.8 s / .55
- `Assets/UI51/Scripts/Anim/UIAnim.cs:250, 418-440` Pulse returns at rest (alpha 0) when ReducedGraphics, so with reduced graphics the ring is gone
- `Assets/Scripts/Gameplay/CardViewManager.cs:1268-1290` ApplyMoveHints: turnGlow = myTurn && !ReducedGraphics, TurnGlowColor alpha .5; myTurn = IsMyTurnToPlay && !suppressNewCardVisibility (true during the accuso window)
- `Assets/Scripts/Gameplay/CardView.cs:1051-1059` Glow drawn at card sortingOrder -10, behind the hand cards, alpha .7-1 x .5
- `Assets/Scripts/Gameplay/TurnController.cs:1934-1939, 794, 1549, 1691` PlayYourTurnCue = sound only; 3 call sites (after intro, after a move if no redeal, after a redeal)
- `Assets/Editor/SoundLibraryBuilder.cs:66` YourTurn volume .55 MinInterval .5 (asset Resources/Audio/SoundLibrary.asset:150-157 = Assets/Audio/your_turn.wav)
- `Assets/Scripts/Gameplay/Audio/GameAudio.cs:121` Skipped entirely when effects are disabled
- `Assets/Scripts/UI/UI51TableMoments.cs:78-88` Turn timer pill only when TurnTimeLeft <= 5 s, and TurnTimeLeft is -1 offline (TurnController.cs:400-401)
- `Assets/Scripts/UI/TableTopBarController.cs:8-12` Top bar is the score pill only (user decision 29/09, memory project_ui51_fase5_plan)
- `Assets/Scripts/Core/GameFeedback.cs:43-53` TryHaptic exists, but is used only on taps, scope and accuso, never at turn start

**Fix sketch.** Reuse what exists; the visual form must be decided first.
1. One 'turn starts' edge in TurnController.Update, next to TickTurnTimer: `bool ready = AcceptsLocalInput; if (ready && !wasReady) PlayYourTurnCue(); wasReady = ready;`. Delete the 3 PlayYourTurnCue call sites (794, 1549, 1691).
2. PlayYourTurnCue adds GameFeedback.TryHaptic(true) and GamePresentation.ShowYourTurn(), a new 2-line event in GamePresentation. UI51TableMoments shows a short 'TOCCA A TE' chip by reusing the existing hand chip and Hand() animation.
3. Make the persistent signals follow the same gate:
   - PlayerBannerManager.cs:403 becomes `SetTurn(p == CurrentPlayerIndex && (p != local || turnController.AcceptsLocalInput), p != local)`.
   - The hand glow is re-applied on the edge through the OnGamePreferencesChanged pattern (ApplyMoveHints on the hand only; NOT ForceRefresh, because CardViewManager.cs:1225 would re-show an in-flight card).
   - Decide whether to keep a static glow under reduced graphics (CardViewManager.cs:1274).
4. Optional: wire the v07 your_turn.wav (see cross_cutting).

**Files.** Assets/Scripts/Gameplay/TurnController.cs, Assets/Scripts/Core/GamePresentation.cs, Assets/Scripts/UI/UI51TableMoments.cs, Assets/Scripts/UI/PlayerBannerManager.cs, Assets/Scripts/Gameplay/CardViewManager.cs

**Risk.** - The cue moves about 0.08 s later (after the crossfade) and no longer plays during the accuso window. That is intended.
- In MP the gate goes false after sending a move: the own banner ring turns off at the tap, before the animation. Acceptable, but visible.
- If the RPC is lost and SetConnected re-opens the gate, the cue plays again (correct: it is your turn again).
- UI51TableMoments chip placement must be checked in the Simulator on iPhone 12 and SE (ui-verify skill).

**Decision.** Which signal makes the turn 'riconoscibile subito'. Options to choose from:
- a 'TOCCA A TE' chip above the hand for about 1.5 s at turn start;
- a haptic pulse at turn start;
- dimming or lowering the hand when it is NOT your turn;
- a stronger or larger banner ring and glow;
- whether the signals stay on with reduced graphics;
- whether to switch to the new v07 your_turn.wav.
The top bar is score-only by your decision of 29/09; a turn text there would reverse that.

**Test plan.** - EditMode: a static helper or the gate itself (shared with T3).
- Play Mode: count YourTurn plays (GameAudio log) per turn in training 1v1, including after a redeal and an accuso window: exactly one, after the window.
- Simulator check of the chip on iPhone 12 and SE.
- Device-only: perceived visibility, haptic, and iOS silent switch behaviour.

**Verifier (confirmed).** The inventory and the root cause hold.
- The banner turn is polled every 0.2 s from CurrentPlayerIndex (PlayerBannerManager.cs:60, 403). SetTurn sets the gold border (.85) and the pulse ring (PlayerBanner.cs:146-176, 209-213). UIAnim.Pulse rests at alpha 0 when ReducedGraphics is on (UIAnim.cs:250, 440).
- The gold turn glow requires !ReducedGraphics (CardViewManager.cs:1274), alpha .5 (1290).
- PlayYourTurnCue is sound only (TurnController.cs:1934-1939), with three call sites (794, 1549, 1691). YourTurn plays at .55 (SoundLibraryBuilder.cs:66; SoundLibrary.asset:150-157 guid c3d99c29... = Assets/Audio/your_turn.wav) and is skipped when effects are off (GameAudio.cs:119-120).
- TryHaptic is never used at turn start; its only callers are CardView taps, GameFeedback.Present and UIV2HapticButton.
- The timer pill appears only when TurnTimeLeft <= 5 (UI51TableMoments.cs:82); TurnTimeLeft is -1 offline (TurnController.cs:400-401).
- The untracked v07 your_turn.wav exists and differs (md5).

Small corrections:
- (e) The hand glow does NOT light during the deal. ApplyMoveHints requires !suppressNewCardVisibility (CardViewManager.cs:1271), and the deal runs suppressed (TurnController.cs:644, 1602). It lights from the accuso window (1087-1088). Only the banner lights during the deal.
- (b) The cyan move hint is NOT disabled by reduced graphics: `active` does not check it (CardViewManager.cs:1273), and the glow alpha is a static .85 under reduced graphics (CardView.cs:1064). The cited glow lines are actually CardView.cs:1057-1065.

- `Assets/Scripts/Gameplay/CardViewManager.cs:1271-1274` myTurn requires !suppressNewCardVisibility (no glow during the deal); the move hint ignores ReducedGraphics
- `Assets/Scripts/Gameplay/CardView.cs:1064` Static .85 glow under reduced graphics; the hint is not removed
- `Assets/Scripts/UI/UI51TableMoments.cs:12-21, 100-107, 131` The hand chip sits 100 below the TOP banner. The 'Gioca adesso' tempo pill is the element placed above the own banner (hand)

**Fix concerns.** - Reusing the existing hand chip and Hand() puts 'TOCCA A TE' under the opponent's top banner (ChipGap 100, UI51TableMoments.cs:12-21), not 'above the hand' as the option list says. The 'Gioca adesso' tempo pill, positioned by PlaceTimer(mine) at max.y + 222 (UI51TableMoments.cs:100-107, 131), is the existing above-the-hand element to reuse.
- Making the glow follow the gate needs two things. First, ApplyMoveHints' myTurn must use AcceptsLocalInput; today it already shows during the accuso window, so re-applying on the rising edge changes nothing. Second, the falling edge must also be handled: in MP after a send, the gate closes but the glow stays until the echo's ForceRefresh.
- The Update edge will also re-fire after a resync (SetNetworkGameState resets the flags, TurnController.cs:69-75) and after SetConnected(true). The diagnosis treats this as correct.
- The iOS silent-switch point remains a hypothesis; I did not verify it.

### Missed by the investigator (found by the verifier)

- **T2**: Deterministic T2/T4 variant on a different card. Captures are mandatory per card, not per hand, so a non-capturing card A can be played as PlayOnly while hand card B has several capture sets. If B is tapped during A's flight, the tray opens for B with options computed on the old table. At commit, RefreshCardViews cancels the tray first: the callback deselects B, aiming at B's old slot. Only then does RenderHumanHand move B to its new, re-centred slot. The stale SelectionCoroutine snaps B back, and later SetPosition calls skip it, so B stays misplaced or overlapping in the hand until the hand count changes again. Both the T3/T4 gate (no tray during the flight) and a pose-target fix remove it.
  - `Assets/Scripts/Core/Rules51.cs:225-244` PlayOnly is allowed for cards that cannot capture even when other cards can
  - `Assets/Scripts/Gameplay/CardViewManager.cs:935-938, 957, 1239` Tray Cancel (deselect) runs before RenderHumanHand re-lays out the hand
  - `Assets/Scripts/Gameplay/CardViewManager.cs:1615-1621` Cancel callback: SetSelected(false) on all views
  - `Assets/Scripts/Gameplay/CardView.cs:547-549, 567-568, 597` Stale deselect target, then the SetPosition skip keeps the wrong pose
- **T2**: Hover coroutines are an ungated source of the T2 stale pose. TryBeginHover runs on touch-down (OnMouseEnter/OnMouseOver) and EndHover runs on finger release (via Update when PointerLive goes false). Neither passes through OnMouseDown, so the proposed AcceptsLocalInput gate does not stop them. This includes the hover-out of the original tap when the finger is held until about 0.25-0.35 s into the flight. With the current code they leave the played card at its hand pose. With fix (b) as sketched, a running hover-in leaves it raised and enlarged on the table, because EnableHover=false neither cancels the coroutine nor lets EndHover animate out.
  - `Assets/Scripts/Gameplay/CardView.cs:274-281, 389-403, 425-436` Hover in/out are driven by pointer events, not by the click gate
  - `Assets/Scripts/Gameplay/CardView.cs:84-88` The EnableHover setter does not stop a running hover
  - `Assets/Scripts/Gameplay/CardViewManager.cs:1074` RenderTableCards sets EnableHover=false when the card reaches the table
- **T3**: Low-likelihood extra T3 path in MP: a buffered tap can survive across another player's move. Update replays pendingLocalMove only when pendingNetworkMoves is empty. The finally of the local animation dequeues and starts a queued network move first, which sets isMoveAnimationInProgress synchronously. So a second card tapped during your own move's flight is kept through the opponent's animation, and is validated and sent when the turn returns. It needs the opponent's move to arrive before the local animation ends, which is unlikely with symmetric AllViaServer latency. Removing the buffer, as proposed, covers it.
  - `Assets/Scripts/Gameplay/TurnController.cs:1271-1283` Local replay waits for an empty network queue and a free table
  - `Assets/Scripts/Gameplay/TurnController.cs:1502-1509, 1343` The finally starts the queued network move, which sets busy again before the next Update

### Cross-cutting notes

- **input-gating** (T1, T2, T3, T4): There is no single 'local player may act now' check. Every input path (card tap, double tap, capture tray, hand glow, banner ring) uses state-only checks (CurrentPlayerIndex / IsHumanPlayerTurn), while the presentation state (deal, accuso window, animations, move in flight) lives in private TurnController flags. This one gap explains T1-T4. Any future table input, such as emoticon or accuso shortcuts, should use the same gate (TurnController.AcceptsLocalInput proposed in T3).
  - `Assets/Scripts/Gameplay/CardView.cs:472-482` IsLocalPlayersTurn state-only
  - `Assets/Scripts/Gameplay/CardViewManager.cs:1172-1174` IsMyTurnToPlay state-only, reused for clicks, IsClickable, hints, tray cancel
  - `Assets/Scripts/UI/PlayerBannerManager.cs:403` Banner turn ring state-only
  - `Assets/Scripts/Gameplay/TurnController.cs:425-426` IsBusy exists but is never consulted by input code
- **ui-busy-state** (T3, T4): The pendingLocalMove buffer is a deliberate v2.10 design (memory feedback_onmousedown_ui_passthrough), and the user's BUILD 3 report reverses it ('mai in coda'). Removing it also makes TurnController.IsBusy false earlier. Its other readers: MatchResultsV2 forfeit decision, NetworkGameController.SendStateWhenSettled and TickTurnTimer. All only get faster, but the forfeit and resync clusters should know about it.
  - `Assets/Scripts/Gameplay/TurnController.cs:426` pendingLocalMove != null keeps IsBusy true
  - `Assets/UIV2/Scripts/Core/MatchResultsV2.cs:205` Forfeit check waits for !turn.IsBusy
  - `Assets/Scripts/Networking/NetworkGameController.cs:550` Master delays a resync state while IsBusy
- **audio-wiring** (T1): The untracked 2.65 folder Assets/Audio/51_Audio_v07_FINAL_CANDIDATE contains a different your_turn.wav ('click tattile + cue brillante molto corto', recommended volume .6) and other new SFX. None is wired: SoundLibraryBuilder reads flat files from Assets/Audio/, and SoundLibrary.asset still points to the old clips. This is relevant for the audio cluster and for T1.
  - `Assets/Editor/SoundLibraryBuilder.cs:17, 66` AudioDir = Assets/Audio/, YourTurn -> your_turn.wav
  - `Assets/Resources/Audio/SoundLibrary.asset:150-157` YourTurn clip guid c3d99c29... = Assets/Audio/your_turn.wav (md5 differs from the v07 file)
  - `Assets/Audio/51_Audio_v07_FINAL_CANDIDATE/Docs/Unity_mapping_recommendations.csv:16` YourTurn,your_turn.wav,0.6
- **other** (T2): CardViewManager.RenderHumanHand re-enables every hand card renderer on each ForceRefresh, but TurnController hides the played card's renderer only for the duration of its flight. A refresh triggered by another subsystem during a local flight shows the card both in hand and in flight. Example: TurnController.OnPlayerConvertedToBot when an opponent drops; NetworkGameController's accuso refresh is guarded only by IsDealInProgress. Relevant to the disconnect/presence cluster. This is a hypothesis: not reproduced.
  - `Assets/Scripts/Gameplay/CardViewManager.cs:1223-1226` CardRenderer.enabled = !suppressNewCardVisibility on every refresh
  - `Assets/Scripts/Gameplay/TurnController.cs:115-118` OnPlayerConvertedToBot -> ForceRefresh with no busy check
  - `Assets/Scripts/Networking/NetworkGameController.cs:98` ForceRefresh guarded only by IsDealInProgress
  - `Assets/Scripts/Gameplay/TurnController.cs:1369` Played view hidden only via renderer.enabled during the flight

## Accuso / Scopa / Matta / suggerimento mosse / testi e branding

### A1 — likely_cause · verifier: partially_confirmed · size S

**Current behaviour.** The same world card responds differently depending on gesture and whose turn it is.
- Own hand, own turn: a touch plays the card at touch-DOWN, or opens the capture choice. A long press can never preview anything.
- Own hand, off turn: holding the finger while moving shows the real 7 di Coppe for a Matta that is showing an accuso value. It restores on release. A perfectly still hold shows nothing.
- Opponent's accused face-up cards (shown after an accuso): the accused-cards viewer opens at touch-DOWN, not on release. Every other HUD element (scope hits, ACCUSA button, viewer backdrop) acts on release.
- On the same press, the hover system lifts the card under the veil and swaps the Matta back to the 7. Meanwhile the viewer flips it TO its accuso value.
- Result: tap and long press look like two different features.

**Root cause.** Two independent gesture paths drive the same world card. Neither is a defined long press.
- Path 1, CardView.OnMouseDown: instant, fires at touch-down. It plays the card, or calls Tapped for the viewer.
- Path 2, hover/hold preview: runs every frame while the pointer is held and is gated by PointerMoved(), which needs more than 1 px of movement.
- Both are enabled on accused opponent cards and on the whole human hand.
- uGUI acts on release, so world cards and UI use opposite edges of the touch.
- CONFIRMED, not a bug: a viewer opened at touch-down does not close on the same release. The veil's Graphic is enabled in that frame and gets depth -1, and GraphicRaycaster skips depth -1 graphics.
- HYPOTHESIS: the exact symptom the QA tester saw; needs a device test.

**Evidence.**
- `Assets/Scripts/Gameplay/CardView.cs:231-260` OnMouseDown: when the card is clickable and it is the local turn, it fires OnCardClicked at touch-down. Otherwise it calls Tapped(this) (258), also at touch-down.
- `Assets/Scripts/Gameplay/CardView.cs:290, 304, 389-416` Hover preview: PointerLive, PointerMoved(). TryBeginHover returns early unless the pointer moved (392), then shows originalFaceSprite, the real 7 di Coppe (405-414).
- `Assets/Scripts/Gameplay/CardView.cs:438-447` EndHover restores the temporary Matta value on release.
- `Assets/Scripts/Gameplay/CardViewManager.cs:256, 292, 354` Accused opponent cards get Tapped = OnAccusedCardTapped and EnableHover = accused. Both paths are active on the same card.
- `Assets/Scripts/Gameplay/CardViewManager.cs:1203, 1220` Human hand: EnableHover is always on ('for Matta visual').
- `Assets/Scripts/Gameplay/CardViewManager.cs:366-375, 1466-1544` OnAccusedCardTapped raises the static AccusedHandTapped event. OnHumanCardClicked plays immediately when there is a single capture set.
- `Assets/Scripts/UI/PlayerBannerManager.cs:63-64, 155-172, 348-369` Subscribes to AccusedHandTapped. OpenAccused/OpenViewer open the viewer. FlipMatta animates the Matta to its value and adds the 'MATTA' tag.
- `Assets/UI51/Editor/UI51TableBuilder.cs:706-787` Scope hits and the viewer Backdrop are uGUI Buttons, so they act on release.
- `Library/PackageCache/com.unity.ugui@1.0.0/Runtime/UI/Core/GraphicRaycaster.cs:322-323` Graphics with depth == -1 (enabled this frame) are skipped. The veil opened on touch-down is not hit by the same touch.
- `Assets/Scripts/UI/TableActionButtonsController.cs:onClick wiring` ACCUSA uses Button.onClick, which fires on release.

**Fix sketch.** Smallest fix that gives one predictable gesture and keeps the current architecture:
1. CardView: move the non-clickable `Tapped` branch out of OnMouseDown into OnMouseUpAsButton. It uses the same touch-simulated mouse message and fires only if the finger is released over the same collider. The viewer then opens on release, like every other HUD tap.
2. CardViewManager.RenderAIHandsDynamic: set EnableHover = false on opponents' accused cards. The viewer already gives a large preview with the MATTA tag, so no lift or 7-swap happens under the veil.
3. Once A3 adds a permanent Matta marker, drop the hover swap to originalFaceSprite (CardView 405-414) or limit it to the human hand off-turn. Also drop the PointerMoved gate for that swap, or replace the swap with the marker.
- Do not add a long-press timer system unless the user explicitly chooses 'hold = preview'.

**Files.** Assets/Scripts/Gameplay/CardView.cs, Assets/Scripts/Gameplay/CardViewManager.cs

**Risk.** Low to medium.
- OnMouseUpAsButton does not fire if the finger slides off a small card collider, so taps near the edge may be lost. Check with fake touches on device sizes.
- The clickable (play) path is untouched, so double-click detection is unaffected.
- Removing the hover on accused cards changes a behaviour some testers may rely on.

**Decision.** Which gesture is THE preview: tap opens the viewer, or long press. Also whether 'hold to see the real 7' should stay once a permanent MATTA marker exists (see A3).

**Test plan.** 1. In the Simulator (not the Game view), force a deal with an accuso for an opponent in 1v1 and 2v2 by reseeding Rules51.Rng.
2. On the opponent's accused cards: single tap, then long press without moving, then long press while moving. The viewer must open exactly once, on release, with no card lift or 7-swap under the veil. The next tap must close it.
3. Own hand with the Matta transformed: tap and hold on and off turn, and confirm the agreed gesture.
4. Repeat on a device with real touch.

**Verifier (partially_confirmed).** The root cause holds. Two gesture paths drive the same world card, and they fire on opposite edges of the touch.
- CardView.OnMouseDown (CardView.cs:231-260) plays the card, or calls Tapped, at touch-down.
- All uGUI controls (UI51Build.Button -> UnityEngine.UI.Button, UI51Build.cs:208-214; ACCUSA onClick, TableActionButtonsController.cs:44) act on release.
- Accused opponent cards get both Tapped and EnableHover=accused (CardViewManager.cs:256, 292, 354). The human hand always has EnableHover=true (1203, 1220).
- The 'viewer does not close on the same release' claim is right. OpenAccused -> OpenViewer -> Refresh -> ShowScope does scopeViewer.SetActive(true) synchronously (PlayerBannerManager.cs:155-172, 300-301). SendMouseEvents runs in PreUpdate, before EventSystem.Update. GraphicRaycaster skips depth==-1 graphics (GraphicRaycaster.cs:322-323). So the press never lands on the veil, and the release has no pointerPress to click.

The current_behavior text is wrong on one point: 'a perfectly still hold shows nothing' / 'holding the finger while moving shows the real 7'.
- PointerMoved() compares Input.mousePosition with the previous frame (CardView.cs:304-313). On a phone the pointer jumps from the last touch's position to the new one on the touch-down frame. The author says so in the comment at CardView.cs:300-302 ("Il dito che tocca sposta il puntatore: si solleva").
- So TryBeginHover (389-416) lifts the card and swaps the transformed Matta back to originalFaceSprite on every new touch, moving or not.
- What actually differs: a tap flashes the real 7 for a moment, a hold keeps it until release (Update -> PointerStillOver needs PointerLive, 274-292; EndHover restores, 438-447).
- On accused opponent cards, Unity's SendMouseEvents sends OnMouseDown before OnMouseEnter in the same frame. The viewer opens first, then the card under the veil lifts and turns into the 7 until release.
- No long-press system exists anywhere (grep for long-press/hold handlers finds only UI51Press/UIV2ButtonFeedback scale feedback).

**Corrected cause.** Same two-path cause (touch-down OnMouseDown vs release-based uGUI, plus the hover/7-swap on the same card). Correction: the hover/7-swap is not gated by finger motion in practice. Each touch-down moves the pointer, so it fires on every touch, tap or hold; only the duration differs. A3's missing permanent marker makes the brief flash of the 7 on a tap look like a different feature from the hold.

- `Assets/Scripts/Gameplay/CardView.cs:299-313` PointerMoved compares against the last frame's mousePosition. The comment says the touching finger counts as movement, so the card lifts on touch.
- `Assets/Scripts/Gameplay/CardView.cs:454-464` IsPointerOverUI does a fresh RaycastAll at Input.mousePosition. It guards Tapped at touch-down only, not the hover.
- `Assets/Scripts/UI/PlayerBannerManager.cs:161-172, 300-301` OpenViewer -> Refresh -> ShowScope activates the veil synchronously inside OnMouseDown.
- `ProjectSettings/ProjectSettings.asset:936` activeInputHandler: 0 (legacy input). OnMouseDown/OnMouseUpAsButton come from SendMouseEvents with touch-to-mouse simulation, and the UI uses StandaloneInputModule.

**Fix concerns.** Step 1 is sound. Tapped is used only for accused cards (CardViewManager.cs:256), so moving it to OnMouseUpAsButton touches no other caller.
- It relies on step 2. If hover stays on, the lifted card takes its collider up with it (the hysteresis problem described at CardView.cs:269-272). A release near the card's lower edge would then miss the collider and OnMouseUpAsButton would not fire.

Step 3 ('drop the PointerMoved gate for the swap') partly reintroduces the 01/10 complaint the gate fixed: a card sliding under a stationary mouse in the Editor or on desktop. On phones PointerLive already prevents it.

The human hand's play-on-touch-down path is untouched, so on your own turn a tap can never be a preview. That needs the user's gesture decision.

### A2 — likely_cause · verifier: partially_confirmed · size S

**Current behaviour.** Scopa cards are drawn as small strips peeking out from behind each player banner.
- Strip sizes: own banner shows about 8 units of the first card, then 13-unit steps. The top seat uses 13-unit strips upward, the side seats 13-unit steps along the rotated axis.
- Cards overlap each other by about 50%. With 1-2 scope they read as hidden behind the banner.
- Over 4 scope a '+N' badge appears:
  - on the top seat it is placed over card 0 (moreAt 131,-24);
  - on the own seat its right edge (about x=326) touches the ACCUSA ring (about x=324).
- Hit rectangles are fixed sizes, whatever the count: own 86x50 at x=180, top 87x49, sides 44x72.
  - They cover part of the banner plate.
  - The quick-profile hit is the first sibling and the banner prefabs have no raycast target, so once a player has scope a tap on that part of the banner opens the scope viewer instead of the profile.
  - With 1 scopa most of the hit area is over empty banner.

**Root cause.** The builder geometry was tuned for the static mockup, not for variable counts.
- The scope container is created before the banner prefab, so it renders behind it.
- The step is small (13 units) and the first protrusion is 8 units.
- Hit rects are constant and partly overlap the banner and profile hit.
- There is no runtime layout by count.
- CONFIRMED from builder code. The exact visual result per device aspect is a HYPOTHESIS: GameScene was not open in the Editor (read-only rule), so needs a Simulator check.

**Evidence.**
- `Assets/UI51/Editor/UI51TableBuilder.cs:157-247` Banner():
- own scope card x = 186+8+13*i-26 at y 5, 26x40, moreAt (266,13);
- top x = 83-13*i at y -13, 24x37, moreAt (131,-24);
- sides (-7.5 / 47.5, 19.5+13*i, 90 deg), moreAt (23,91)/(78,91).
The scope container is created before the banner prefab, so it sits behind it.
- `Assets/UI51/Editor/UI51TableBuilder.cs:706-787` BuildScope: ScopeHit(own,0,180,0,86,50), (left,1,-14,22,44,72), (top,2,44,-24,87,49), (right,3,34,22,44,72). Fixed sizes.
- `Assets/UI51/Editor/UI51TableBuilder.cs:1543-1555, 1588-1594` ScopeHit and Box helpers: top-left anchor, rotation -degrees.
- `Assets/UI51/Editor/UI51TableBuilder.cs:1103-1139` BuildQuickProfile: UI51ProfileHit is the first sibling, own 130x50, others stretch. Scope hits above it win the raycast.
- `Assets/UI51/Editor/UI51TableBuilder.cs:259-312` BuildLayout: local banner at x 153 with width 186, ACCUSA at x 353. The own '+N' badge ends near the ACCUSA ring.
- `Assets/UI51/Scripts/Components/PlayerBanner.cs:196-207` SetScope shows at most m_Scope.Length (4) cards, then m_ScopeMore.SetCount(count-4).
- `Assets/Scripts/UI/PlayerBannerManager.cs:60, 105-110, 152` Refresh every 0.2 s. Scope hits are interactable/raycast only when hasScope. OpenScope.
- `Assets/Scripts/UI/LocalSeatBottomShift.cs:whole file` Seats move as a group. Not a cause.

**Fix sketch.** In UI51TableBuilder (geometry only, no new system):
1. Shrink each ScopeHit to the part that sticks out of the banner, so it no longer covers the profile hit:
   - own: x 186..266;
   - top: y -24..0;
   - sides: the outer strip.
2. Move the top seat moreAt so it no longer covers card 0, and pull the own '+N' left of the ACCUSA ring.
3. Optional, needs the user's OK to deviate from the mockup: a larger first protrusion (8 to about 14 units) and step (13 to about 16 units).
4. Re-run Tools/UI51/Build Fase 5 with the forced scene open.
Alternative that avoids a builder re-run: in PlayerBannerManager.Refresh (already polls every 0.2 s), set each hit rect's width from the visible scope count.

**Files.** Assets/UI51/Editor/UI51TableBuilder.cs, Assets/Scenes/GameScene.unity (regenerated by the builder, not hand-edited), Assets/Scripts/UI/PlayerBannerManager.cs (only for the runtime alternative)

**Risk.** Medium.
- Re-running UI51TableBuilder regenerates the whole table HUD. Any later manual or other-builder tweaks under GameCanvas can be lost.
- Other agents may be using the Editor concurrently.
- Changing protrusion and step deviates from mockup / SPEC section 5.

**Decision.** Whether the scope protrusion and step may deviate from the mockup / SPEC section 5 to stay readable, and how far.

**Test plan.** 1. In the Simulator on iPhone SE, a 19.5:9 phone and a tablet, use the forced capture scenario recipe to give 0, 1, 2, 4, 5 and 8 scope to each seat in 1v1, 2v2 and 1v3.
2. Measure the visible strip widths in pixels against the mockup.
3. Check that '+N' never overlaps a card or the ACCUSA ring.
4. Tap the banner body: it must open the quick profile. Tap the scope strip: it must open the scope viewer, in each count case.

**Verifier (partially_confirmed).** Geometry confirmed from the builder, and GameScene matches it:
- Scope cards: own x=168+13i, 26x40, so 8 then 13 units stick out. Top x=83-13i, y=-13, 24x37. Sides rotated 90 degrees (UI51TableBuilder.cs:161-171).
- The scope container is a sibling before the banner prefab (203 vs 222-224), so it renders behind the banner. That is by design ('Scope dietro al banner (SPEC §5)', PlayerBanner.cs:195).
- Hit rects are fixed (723-724) and match the scene YAML: own centre (223,-25) size 86x50; top 87x49; sides 44x72.
- Banner prefabs have zero raycast targets (PlayerBanner_Own/Opponent/Vertical: 0 'm_RaycastTarget: 1'), and Refresh flips the hit's raycast with hasScope (PlayerBannerManager.cs:105-110).
- The own '+N' right edge is at design x 60+266=326. The ACCUSA ring (58 wide, centre 353) starts at about 324 and the button (50 wide) at 328, so they touch. The ring is only shown during the accuso window (TableActionButtonsController.cs:93).

Corrections:
1. The own seat's scope hit does NOT overlap the own profile hit. Profile is 0..130 (UI51TableBuilder.cs:1127; scene centre 65, size 130), scope is 180..266. Only the top and side hits overlap the profile hit.
2. That overlap is intentional per the builder's own comments: 'In alto: ... meta' alta del banner' (719-720), 'dove si sovrappongono alle scope vince il visore' (1100), and 702-703.
3. The top-seat '+N' is NOT over card 0. Badge_More has a 22-high content-fit pill with 5+5 padding and a 10pt label (UI51PrefabBuilder.cs:314-320), about 22 wide for '+N'. With pivot (1,1) at x=131 it spans about x109-131. Card 0 spans x83-107, so they are adjacent. It only overlaps by a few units with 3-character labels (14+ scope).
4. For own, 'with 1 scopa most of the hit area is over empty banner' is wrong: 194..266 is empty table to the right of the banner.

**Corrected cause.** Fixed mockup geometry (8/13-unit protrusion and fixed hit rects, with no runtime layout by count) is the cause of 'hidden / hard to read'. The hit-area overlap with the top and side banners is a documented design choice to get a usable touch target, not an accident. The 'covers the profile' problem applies only to the top and side seats.

- `Assets/Scenes/GameScene.unity:UI51ScopeHit / UI51ProfileHit RectTransforms` Scene values match the builder. Own profile hit pos (65,-25) size (130,50). Own scope hit pos (223,-25) size (86,50). No overlap on the own seat.
- `Assets/UI51/Editor/UI51TableBuilder.cs:700-704, 719-720, 1097-1101` Comments state the scope hits sit behind banner/emoticons and deliberately win over the profile hit where they overlap.
- `Assets/UI51/Editor/UI51PrefabBuilder.cs:314-320` Badge_More is a 22-high, content-fit pill (padding 5/5, 10pt label), about 22 wide for '+N'.
- `Assets/UI51/Editor/UI51TableBuilder.cs:213-219` '+N' is the last sibling in Scope, pivot top-right at moreAt, and grows to the left.
- `Assets/Scripts/UI/TableActionButtonsController.cs:93` The ACCUSA ring is only active while the accuso window is open.

**Fix concerns.** Shrinking hits to only the visible strip makes targets too small for a finger.
- Top: 24 design units tall (y -24..0). Sides: about 14 units wide.
- That is roughly half of a 44 pt target, and it reverses an overlap the builder documents as intentional (1100).

Safer options:
- Keep the overlap and only cap the hit width by count, at runtime in PlayerBannerManager.Refresh, which already polls every 0.2 s.
- Or leave the hit sizes and just move the own '+N' left of the ACCUSA ring.

Re-running Build Fase 5 regenerates the whole table HUD (BuildBanners/BuildLayout/BuildScope/BuildAccuso), which carries the risk the investigator noted.

### A3 — root_cause_confirmed · verifier: confirmed · size M

**Current behaviour.** When the Matta (7 di Coppe) takes an accuso value, its sprite is replaced by the target card's face (for example Asso di Denari). Nothing on the card itself says it is the Matta.
- The only cue is a soft pulsing halo behind it:
  - the halo is mostly covered by the neighbouring cards;
  - its glow sprite fades to roughly 30/255 alpha (about 12%) around the card edge.
- The move-hint glow is switched off on the Matta while the halo is on.
- In 2v2/1v3 the accuso impact flip (AccusoImpactV2.FlipMatta) only swaps the sprite. Its three reveal cards have a gold frame and no Matta tag.
- Only the accused-cards viewer (PlayerBannerManager) shows a 'MATTA' tag.
- When the transformed card is played it lands on the table as a 7 di Coppe, because table rendering clears the transform. That is correct by the rules but surprising with no marker.

**Root cause.** The per-card marker mechanism already exists (CardMarkerOverlay via ShowTemporaryValue(temp, marker)). The Matta flip calls it with marker = null, so no permanent marker is ever drawn. The only remaining cue is a soft glow that is close to invisible at the card edge. The UI51 accuso reveal was built without the Matta child that BuildScope's Card0 already has.

**Evidence.**
- `Assets/Scripts/Gameplay/CardView.cs:768-788` CardMarkerOverlay: child sprite at local (0, 0.6), sortingOrder +5. A permanent marker mechanism already exists.
- `Assets/Scripts/Gameplay/CardView.cs:792-824` ShowTemporaryValue(temp, marker) swaps the face and shows the marker only if marker != null.
- `Assets/Scripts/Gameplay/CardView.cs:850-866, 899` ShowMattaTransform -> MattaFlipRoutine calls ShowTemporaryValue(target, null): no marker.
- `Assets/Scripts/Gameplay/CardView.cs:933-950, 1042-1074` EnsureMattaHalo uses the soft glow sprite at 2x/1.7x scale, sortingOrder card-10. The hint glow is disabled while the halo is on (1060).
- `Assets/Scripts/Gameplay/CardViewManager.cs:106-111, 357-361, 1105-1126, 1259` mattaHaloSprite field. ApplyMattaSpecialVisual is used for the AI accused hand and the human hand (after SetOutline at 1232).
- `Assets/Scripts/Gameplay/CardViewManager.cs:1074-1078` Table render clears the Matta transform, so a played Matta shows as a 7.
- `Assets/UIV2/Scripts/Core/AccusoImpactV2.cs:126-139` FlipMatta: sprite swap plus scale punch, no tag.
- `Assets/UI51/Editor/UI51TableBuilder.cs:650-661` BuildAccuso: 3 face cards with a gold Frame, no Matta child.
- `Assets/UI51/Editor/UI51TableBuilder.cs:706-787` BuildScope Card0 HAS a 'Matta' child (Frame/Pulse/Tag 'MATTA'), a ready pattern to copy.
- `Assets/Scripts/UI/PlayerBannerManager.cs:348-369` The viewer's FlipMatta enables the gold Frame, Pulse and 'MATTA' tag. This is the only place that labels the Matta.
- `Assets/UI/... sprite guid 105ec177c8b8e974b8cda17e79e870d4 ('Bagliore morbido cerchio', 1254x1254):pixel measurement` Alpha by relative radius: 0 -> 253, 0.3 -> 162, 0.48 -> 87/69, 0.55 -> 44, 0.625 -> 30/24, 0.7 -> 13/8, 0.8 and beyond -> 0. Nearly transparent where it would show past the card edge.

**Fix sketch.** 1. World cards (hand and accused hands), in CardView.ShowMattaTransform, clearing in ClearMattaTransform:
   - a crisp outline via the existing CardView.SetOutline, in a Matta-only color (not the tre-assi color);
   - a small permanent 'MATTA' tag as a child. Either a world TextMeshPro (TMP is already referenced by Project51.Gameplay, guid 6055be8e..., used by DealerAccusoRevealController) or a marker sprite passed to the existing ShowTemporaryValue(target, marker) at line 899.
   - ApplyMattaSpecialVisual already runs after the SetOutline reset (1232 -> 1259), so the order holds.
2. 2v2/1v3 impact: in UI51TableBuilder.BuildAccuso, add a Matta child to the reveal cards (copy BuildScope Card0's Frame/Pulse/Tag). Enable it in AccusoImpactV2.FlipMatta. Re-run the Fase 5 builder.
3. Then A1 can drop the hover swap to the 7.
- Per the asset gap policy, check the inventory for a Matta badge sprite first. If there is none, use TMP text, not a reused wrong icon.

**Files.** Assets/Scripts/Gameplay/CardView.cs, Assets/Scripts/Gameplay/CardViewManager.cs (only if the marker sprite is passed from here), Assets/UIV2/Scripts/Core/AccusoImpactV2.cs, Assets/UI51/Editor/UI51TableBuilder.cs, Assets/Scenes/GameScene.unity (regenerated by the builder)

**Risk.** Medium.
- The outline and tag must be cleared on every re-render path (table render 1074-1078, redeal, rejoin replay), or a 7 on the table could keep a stale MATTA tag.
- The world tag's sortingOrder must follow the card's dynamic sortingOrder during the hover lift.
- Re-running the builder carries the same risk as A2.

**Decision.** How the Matta marker should look: outline color, 'MATTA' text tag or a small 7-di-Coppe corner badge, and position. Also whether 'hold to see the real 7' stays once the marker exists.

**Test plan.** 1. Force a deal where the human holds 7 di Coppe plus a pair: Decino with the Matta as the pair's rank, and a Cirulla with the Matta as 1.
2. In the Simulator, check the marker is visible in hand on and off turn and while hovering.
3. Check it disappears once played and on the next hand.
4. In 2v2 and 1v3, check the impact reveal shows the tag and the opponent's accused face-up hand shows the marker.
5. EditMode: an existing ShowTemporaryValue/Clear test, or a new small one, asserts the marker is active after ShowMattaTransform and inactive after ClearMattaTransform.

**Verifier (confirmed).** I saw the mechanism myself.
- ShowMattaTransform -> MattaFlipRoutine -> ShowTemporaryValue(target, null) (CardView.cs:850-866, 899). Also ShowTemporaryValue(targetSprite, null) when inactive (864). So the existing CardMarkerOverlay (768-788) never gets a sprite.
- The only cue is the halo: the soft glow sprite at 2x/1.7x (933-950).
- The hint glow is disabled while the halo is on (1060).
- AccusoImpactV2.FlipMatta only swaps the sprite and punches the scale (AccusoImpactV2.cs:126-139). The builder's three reveal cards have a Frame and Face only (UI51TableBuilder.cs:653-661). BuildScope Card0 has the Matta/Frame/Pulse/Tag 'MATTA' child (755-764). PlayerBannerManager.FlipMatta is the only place that turns on a 'MATTA' label (348-369).
- Table render clears the transform (CardViewManager.cs:1078).
- Sprite measurement confirmed: 1254x1254. My alpha at r=0.476/0.625 was about 71/24 horizontally and 43/10 vertically.

Minor corrections:
- The halo is drawn at CardRenderer.sortingOrder - 1 (CardView.cs:1070), not card-10. Only the hint/turn glow uses -10 (1063).
- At the card's left and right edges the glow is about 20-28% alpha, not about 12%. About 12% or less applies only above and below the card.
- Hand cards are 92 wide at a step of 99 (CardViewManager.cs:52), so the halo shows in the 7-unit gaps and above and below the card. It is still faint.

**Corrected cause.** As stated. One addition: for the human hand, ApplyMattaSpecialVisual runs on every RenderHumanHand (CardViewManager.cs:1259), and AccusiChecker.MattaValueForAccuso only inspects the hand. So the Matta is shown as the other card from the moment of the deal, before and regardless of the player pressing ACCUSA. The 'looks like a real copy' effect is therefore present for the whole 3-card hand, not only after an accuso.

- `Assets/Scripts/Gameplay/CardView.cs:1068-1073` The halo sortingOrder is card-1 (not card-10). Its alpha pulses between 0.6 and 0.85, plus the burst.
- `Assets/Scripts/Gameplay/CardViewManager.cs:1105-1126, 1258-1259` The Matta transform is applied to the human hand on every render whenever the hand qualifies. No declared-accuso check.
- `Assets/Scripts/Gameplay/CardViewManager.cs:52` Hand cards are 92 wide at a step of 99, so neighbours do not overlap; the halo shows in the gaps and above/below.
- `Assets/UIV2/Scripts/Core/GameSocialV2.cs:219-221` QueueMattaFlip is called after Impact.Play. Play nulls mattaCard (AccusoImpactV2.cs:93-94), so the order matters and is currently correct.

**Fix concerns.** The outline is ONE shared SpriteRenderer per card. It is already used for the 4-player accused gold border (CardViewManager.cs:254) and for tre assi (1232).
- If ClearMattaTransform also clears the outline, as the sketch implies, the following breaks in 2v2/1v3. An accused opponent's hand drops from 3 to 2 cards. RenderAIHandsDynamic sets the gold AccusedOutline (254), then ApplyMattaSpecialVisual -> MattaValueForAccuso returns -1 -> ClearMattaTransform (360 -> 1117). That would strip the gold border from the Matta only.
- A Matta-coloured outline would also overwrite the accused gold border on that hand while it is transformed.
- The marker path (ShowTemporaryValue with a sprite) fixes markerRenderer.sortingOrder once, at creation (CardView.cs:777), and never follows SetBaseSortingOrder or the hover. TryBeginHover also hides it (412).
- The marker or tag must also be off for FlipToFaceDown/reuse (CardViewManager.cs:239-241 already calls ClearMattaTransform).

### O1 — code_looks_correct_needs_device_test · verifier: partially_confirmed · size S

**Current behaviour.** The wiring works.
- The 'Suggerimenti mosse' switch (default on) writes Settings_MoveHints, raises GamePreferences.Changed, and the hand refreshes immediately.
- On the local turn, hand cards that can capture get a cyan soft glow (SetMoveHint). All other hand cards get a gold 'your turn' soft glow at 0.5 alpha (SetGlow), unless Grafica Bassa is on.
- Both glows use the same soft circle sprite at sortingOrder card-10. They sit behind the card and its neighbours, and fade to about 30/255 alpha at the card edge.
- So in practice every card glows faintly, and the capture card differs only by a faint hue. Testers can easily conclude hints do nothing.
- With Grafica Bassa only the cyan hint remains, so it is clearer there.
- The Matta never shows a hint (halo suppresses it).
- There are no automated tests for ApplyMoveHints.

**Root cause.** The feature logic is correct, but the hint shares its visual treatment with the always-on turn glow and the Matta halo. That treatment (a soft glow sprite behind the card) is close to invisible where it pokes out. The problem is design and visibility, not wiring. Device test needed to confirm visibility.

**Evidence.**
- `Assets/Scripts/Core/GamePreferences.cs:MoveHintsKey / SetMoveHints` Key 'Settings_MoveHints', default 1. SetMoveHints -> Write -> Changed.
- `Assets/UIV2/Scripts/Core/InGameSettingsV2.cs:60, 87` HintsSwitch -> SetMoveHints. SetIsOn on open.
- `Assets/Scenes/GameScene.unity:3152` HintsSwitch reference wired.
- `Assets/UI51/Editor/UI51TableBuilder.cs:1001` Label 'Suggerimenti mosse', sub 'Evidenzia le carte che fanno una presa'.
- `Assets/Scripts/Gameplay/CardViewManager.cs:1268-1287, 1290` ApplyMoveHints: active = MoveHints && myTurn. Capture cards get SetMoveHint(cyan) (1284). The others get SetGlow(turnGlow, gold, alpha 0.5) (1285).
- `Assets/Scripts/Gameplay/CardViewManager.cs:1304-1310, 1457-1460` OnGamePreferencesChanged reapplies the hints. Unsubscribed in OnDestroy, so no leak.
- `Assets/Scripts/Gameplay/CardViewManager.cs:106-111, 1293-1302` moveHintGlowSprite is also used for the dealer accuso glow. The field is null in Assets/Prefabs/CardViewManager.prefab:136-137 but assigned by the GameScene override (GameScene.unity:97277, 97293).
- `Assets/Scripts/Gameplay/CardView.cs:958-984, 1060, 1063` SetMoveHint/SetGlow use the soft sprite at 2.1x/1.6x. Hint disabled under the Matta halo. Glow sortingOrder = card - 10.
- `Assets/Scripts/Gameplay/CardView.cs:992-1020` SetOutline is a crisp runtime 9-slice border, already proven visible (tre assi). A ready, visible alternative.
- `SPRINT_BACKLOG.md:822, 1207` Options sheet 'PARTITA (Suggerimenti mosse)'. B5 move hints marked done in 1.81.

**Fix sketch.** 1. In CardViewManager.ApplyMoveHints, mark capture cards with view.SetOutline(true, hint color) (the crisp border used for tre assi) instead of, or in addition to, the soft SetMoveHint glow.
2. While hints are on, skip the gold turn glow on non-capture cards, or keep it only when no card captures. The contrast then reads as 'this one'.
3. Let the Matta show the hint outline too, with the marker from A3.
4. Add one EditMode test: given a hand and table with one capture, ApplyMoveHints marks exactly that card. Or test the capture-set query it uses.

**Files.** Assets/Scripts/Gameplay/CardViewManager.cs, Assets/Scripts/Gameplay/CardView.cs (only if the outline color must coexist with tre-assi/Matta outlines), Assets/Tests/EditMode/ (one new small test)

**Risk.** Low.
- The outline is shared with the tre-assi and the A3 Matta marker. A priority rule is needed: tre assi, then Matta, then hint, otherwise they overwrite each other on re-render.

**Decision.** Whether the hint should replace the gold 'your turn' glow while hints are on. Whether 'Suggerimento mosse' should only mark cards that can capture (current) or point to the best move, which would reuse CirullaAI and is a bigger change.

**Test plan.** 1. In the Simulator with a forced deal that has exactly one capture card: hints on, the capture card is clearly outlined and the others are not. Hints off from the settings sheet mid-turn: the outline disappears immediately.
2. Opponent's turn: no outlines.
3. Repeat with Grafica Bassa on and off, and on a real phone at normal brightness.
4. Run the new EditMode test.

**Verifier (partially_confirmed).** The wiring works:
- Key 'Settings_MoveHints', default 1. Write -> Changed (GamePreferences.cs:14, 74, 100, 108-114).
- The switch is wired (InGameSettingsV2.cs:60, 87; GameScene.unity HintsSwitch {fileID: 1713102302}).
- CardViewManager subscribes (400) and unsubscribes (1459). ApplyMoveHints runs after every RenderHumanHand (1260), and ForceRefresh follows every un-suppress (TurnController.cs:776-777, 1087-1088, 1668-1669).
- Capture cards get the cyan SetMoveHint, others the gold SetGlow (1281-1286). Both use the soft sprite at card-10 (CardView.cs:958-984, 1063).
- moveHintGlowSprite is null in the prefab (CardViewManager.prefab:137) and set by the GameScene override (97293-97295).
So the logic is correct, and the visibility verdict is a plausible design hypothesis.

Corrections to the description:
1. The capture card does not differ 'only by a faint hue'. MoveHintColor alpha is 1 (CardView.cs:955) and TurnGlowColor alpha is 0.5 (CardViewManager.cs:1290). LateUpdate multiplies by the colour's alpha (CardView.cs:1065), so the hint is about twice as intense as the turn glow, as well as cyan instead of gold.
2. 'The Matta never shows a hint' is wrong. Only the transformed Matta, with its halo active (3-card qualifying hand), suppresses it (1056-1060). A plain 7 di Coppe shows the hint normally.
3. As in A3, the glow at the card's side edges is about 28% sprite alpha, not about 12%.

No automated test covers ApplyMoveHints.

**Corrected cause.** The feature works. Visibility is weak because the hint uses the same soft glow sprite, behind the card at order-10, as the always-on gold turn glow. With normal graphics it is roughly 2x brighter and a different hue, which on a phone may still read as 'all cards glow'. Device test still needed.

- `Assets/Scripts/Gameplay/CardView.cs:955, 1064-1065` MoveHintColor alpha is 1. The glow alpha is pulse x colour.a, so the hint is about 0.7-1.0 and the turn glow about 0.35-0.5.
- `Assets/Scripts/Gameplay/CardViewManager.cs:1290` TurnGlowColor alpha is 0.5.
- `Assets/Scripts/Gameplay/CardView.cs:1056-1060` The hint is hidden only when the Matta halo is active.

**Fix concerns.** view.SetOutline has the signature (Color color, float width), not (bool, Color) (CardView.cs:992).
- OnGamePreferencesChanged calls only ApplyMoveHints (CardViewManager.cs:1304-1310). An outline-based hint must restore whatever outline the card should otherwise have (tre assi / Matta marker from A3) when hints are turned off. Otherwise toggling the option leaves a wrong border.
- Removing the gold turn glow takes away the only 'your turn' cue on the hand, so it needs the user's decision.

### X1 — feature_missing · verifier: confirmed · size S

**Current behaviour.** 'Cirulla' still appears in player-visible text:
- the 2v2/1v3 accuso impact caption 'CIRULLA';
- the rules page item 'Cirulla · 3 punti' and its tip;
- the welcome sheet 'Conosci già la Cirulla?';
- the tutorial line;
- the access screen subtitle 'Unisciti alla sfida di Cirulla-51'.
The MainMenu.unity serialized copies come from builders. Class names (CirullaAI, AccusoType.Cirulla), the 'Debug.Log CIRULLA', a code comment and the CreateAssetMenu path '51 Cirulla/UIV2/Theme' are not player-visible. The Android package id com.project51.cirulla is not visible in the UI; productName is '51' and the iPhone id is com.deomoreo.51.

**Root cause.** The rename was never done. The accuso type and the game were both called 'Cirulla', and the hard-coded Italian strings in builders and runtime scripts kept the old name.

**Evidence.**
- `Assets/UIV2/Scripts/Core/GameSocialV2.cs:200` Impact caption: type==Cirulla ? "CIRULLA" : ...
- `Assets/UI51/Editor/UI51RulesBuilder.cs:77` "Cirulla · 3 punti"
- `Assets/UI51/Editor/UI51RulesBuilder.cs:83` Tip "Se la mano è sia Cirulla che Decino vale il Decino..."
- `Assets/UI51/Editor/UI51RulesBuilder.cs:186` "Conosci già la Cirulla?"
- `Assets/Scripts/UI/UI51TutorialView.cs:44` "...hai la Cirulla (3 punti)..."
- `Assets/UI51/Editor/UI51AccessBuilder.cs:167` "Unisciti alla sfida di Cirulla-51"
- `Assets/Scenes/MainMenu.unity:146053, 206099, 235135, 250601` Serialized builder output of the four builder strings above. Regenerate, do not hand-edit.
- `Assets/UIV2/Scripts/Core/UIV2Theme.cs:10` CreateAssetMenu path, Editor-only, not player-visible.
- `Assets/Scripts/Gameplay/TurnController.cs:1845` Debug.Log only, not player-visible.
- `ProjectSettings/ProjectSettings.asset:16, 166, 168` productName '51'. Android id com.project51.cirulla (not shown in the UI). iPhone com.deomoreo.51.

**Fix sketch.** 1. Replace the 6 literals: GameSocialV2:200, UI51RulesBuilder:77/83/186, UI51TutorialView:44, UI51AccessBuilder:167. Use the accuso name the user picks, and '51' for the game name ('Unisciti alla sfida di 51', 'Conosci già il 51?').
2. Re-run the Fase 12 rules/welcome builder and the access builder so MainMenu.unity is regenerated.
3. Do not rename AccusoType.Cirulla, CirullaAI, files or the package id.
4. Update the tutorial-seed test only if it asserts on text.
5. Check the non-ASCII bytes of each file before editing (memory: non-UTF8 sources).

**Files.** Assets/UIV2/Scripts/Core/GameSocialV2.cs, Assets/UI51/Editor/UI51RulesBuilder.cs, Assets/Scripts/UI/UI51TutorialView.cs, Assets/UI51/Editor/UI51AccessBuilder.cs, Assets/Scenes/MainMenu.unity (regenerated by builders)

**Risk.** Low for code. Medium for the builder re-runs (MainMenu regeneration can wipe later manual changes). Accented characters must not be corrupted.

**Decision.** The new player-facing name for the 'Cirulla' accuso (3 cards with sum 9 or less). Whether the Android package id com.project51.cirulla stays (recommended: keep, since changing it creates a new store app). Store listing and PlayFab TitleData texts are outside the repo; the user must check them.

**Test plan.** 1. After the change, grep -rni cirulla over Assets/**/*.cs string literals and Assets/Scenes. Only identifiers, comments, Debug.Log and the menu path may remain.
2. In the Simulator, open the access screen, Welcome, Rules and the tutorial.
3. Force a Cirulla-type accuso in 2v2 and check the impact caption.
4. Check the Collection accusi panel and the accused viewer use the new name.

**Verifier (confirmed).** All six player-visible literals exist:
- GameSocialV2.cs:200;
- UI51RulesBuilder.cs:77, 83, 186;
- UI51TutorialView.cs:44;
- UI51AccessBuilder.cs:167.
Their serialized copies are in MainMenu.unity:146053, 206099, 235135, 250601. GameScene.unity has no 'Cirulla'.

A repo-wide grep (excluding Library/.vs) finds no Cirulla in Server/CloudScript, Assets/Legal (PrivacyPolicy/TerminiDiServizio), or Resources. The remaining hits are identifiers, comments, Debug.Log (TurnController.cs:1845), the CreateAssetMenu path (UIV2Theme.cs:10), docs/specs/mockups, and the Android id (ProjectSettings.asset:166, AndroidResolverDependencies.xml:11). No test asserts on the visible text.

Minor correction: the 'CIRULLA' caption is not limited to 2v2/1v3.
- AccusoImpactV2.Play always sets Caption/CaptionShadow (AccusoImpactV2.cs:98-99).
- UIAnim.Accuso animates Parts.text in every mode (UIAnim.cs:474-479).
- GameSocialV2.Accuso calls Play for 1v1 too (count==2).
So it also shows in 1v1 and for your own accuso.

**Corrected cause.** As stated. The caption appears in every mode, not only 2v2/1v3.

- `Assets/UIV2/Scripts/Core/AccusoImpactV2.cs:87-114` Play sets Caption.text = title for every accuso and every mode.
- `Assets/UI51/Scripts/Anim/UIAnim.cs:454-479` Accuso shows the root and animates the title text regardless of player count.

**Fix concerns.** Same as stated. Builder re-runs (UI51RulesBuilder Fase 12, UI51AccessBuilder) regenerate MainMenu hierarchies. Check the files for non-ASCII bytes first: MainMenu shows \xE0/\xB7/\xE8 escapes, and the builders contain à/è/·.

### X2 — root_cause_confirmed · verifier: confirmed · size S

**Current behaviour.** The texts for smazzate, cards left, turn, reconnection, result, Scope, Accuso and end of match are mostly coherent, but have these concrete problems:
1. Result header 'MODO · MANO N' uses RoundIndex, which is the SMAZZATA number. In-table 'MANO x DI y' means the 3-card hand, so the same word means two things.
2. Auto-advance caption 'Si riparte da sola tra 1 secondi': singular/plural bug in the last second; 'da sola' reads oddly.
3. 'ATTENDI L'HOST': English jargon for guests.
4. Reconnection notices use the raw Photon NickName. The banners show sanitized GameSocialV2/PlayFab names, the dealer and tre-assi details show 'Giocatore N', so the same player can appear under different names, or blank.
5. Notices are masculine-only: 'si è disconnesso', 'è rientrato', 'è uscito'.
6. The local reconnection overlay says 'Gli altri giocatori ti aspettano', but opponents are told 'Gioca un bot finché non rientra'. The two messages contradict each other.
7. 'Capp.' abbreviation in the race totals is cryptic.
8. Deck medal says only 'RIMASTE' with no noun.
9. 'CIRULLA' caption (X1).
10. Scene placeholders that flash or show if a binding fails: 'SCOPA DA 30!' (GO Title of the dealer reveal), 'New Text' (MessageText), 'Scegli mossa'.
Scope texts handle singular/plural correctly ('1 scopa' / 'N scope', '1 carta presa'), and the forfeit caption already handles plural.

**Root cause.** Hard-coded strings spread over builders and runtime scripts, with no shared glossary or name helper. Three or four different display-name sources are in use.

**Evidence.**
- `Assets/Scripts/UI/UI51ResultsView.cs:155` roundMode.text = ModeLabel(state) + " · MANO " + state.RoundIndex. RoundIndex is the smazzata.
- `Assets/Scripts/UI/UI51TableMoments.cs:139` 'ULTIMA MANO · MAZZO FINITO' / 'MANO {hand} DI {total}'. Here mano is the 3-card hand.
- `Assets/UIV2/Scripts/Core/MatchResultsV2.cs:420` "ATTENDI L'HOST"
- `Assets/UIV2/Scripts/Core/MatchResultsV2.cs:422-426` "Si riparte da sola tra " + seconds + " secondi" with seconds = CeilToInt(Remaining), so it shows '1 secondi'.
- `Assets/UIV2/Scripts/Core/MatchResultsV2.cs:107` Forfeit caption is already singular/plural aware. OK.
- `Assets/Scripts/Networking/NetworkGameController.cs:273-275` $"{otherPlayer.NickName} si è disconnesso" / "ha lasciato la partita" + "Gioca un bot finché non rientra" / "Al suo posto gioca un bot". Raw NickName, masculine.
- `Assets/Scripts/Networking/NetworkGameController.cs:293` $"{newPlayer.NickName} è rientrato in partita". Raw NickName, masculine.
- `Assets/Scripts/Networking/NetworkGameController.cs:352, 383` 'Impossibile rientrare nella partita. Ritorno al menu…' / 'Sei di nuovo in partita!'
- `Assets/Scripts/Networking/NetworkGameController.cs:730` $"{name} è uscito: 3 turni senza giocare". Uses GameSocialV2.PlayerName (consistent), masculine.
- `Assets/Scripts/UI/UI51ConnectionOverlay.cs:107` 'Il tuo posto al tavolo resta tuo per Ns. Gli altri giocatori ti aspettano.' Contradicts the bot notice.
- `Assets/UI51/Editor/UI51SystemBuilder.cs:150-152, 174, 195` Builder copy of the same reconnection texts.
- `Assets/Scripts/UI/UI51ResultsView.cs:224` 'Capp.' for cappotto in the race totals.
- `Assets/Scripts/UI/UI51ResultsView.cs:240-243` Tie text 'Avete superato… / X e Y hanno superato… si gioca un’altra smazzata'. OK, uses a typographic apostrophe (glyph coverage to verify).
- `Assets/UI51/Editor/UI51TableBuilder.cs:437` Deck medal label 'RIMASTE' (GameScene.unity:9612).
- `Assets/Scripts/Gameplay/TurnController.cs:1020-1028, 830, 844-850, 973-975` GetSimpleDisplayName returns 'Tu' / 'Giocatore N' / 'Bot N' for the dealer and tre-assi details, while banners show real nicknames.
- `Assets/Scripts/Gameplay/TurnController.cs:1945-1950` DealerAccusoTitle 'ACCUSO {sum} · +{points}'.
- `Assets/Scripts/UI/PlayerBannerManager.cs:244-257, 508-522` Scope and accused viewer texts (correct plural). GetDisplayName is a 4th name source.
- `Assets/Scripts/UI/UI51TableMoments.cs:103-104, 217` Timer texts. Scopa moment '{PlayerName} · +1'.
- `Assets/Scenes/GameScene.unity:2143, 3607, 8207` Placeholders 'SCOPA DA 30!' (overwritten by DealerAccusoRevealController.Show), 'New Text', 'Scegli mossa'.
- `Assets/UI51/Editor/UI51RulesBuilder.cs:74` 'premere ACCUSA' wording in the rules.
- `Assets/Scripts/Gameplay/CardViewManager.cs:1497` Invalid-move toast 'Devi prendere con un'altra carta'. OK.

**Fix sketch.** Text-only edits:
- UI51ResultsView:155: ' · SMAZZATA ' + RoundIndex.
- MatchResultsV2:426: seconds == 1 ? '1 secondo' : 'N secondi', and 'Si riparte tra'.
- MatchResultsV2:420: 'IN ATTESA DELL'ORGANIZZATORE' or 'ATTENDI GLI ALTRI'.
- NetworkGameController:273-275 and 293: GameSocialV2.PlayerName(seat), as line 730 already does, instead of the raw NickName; use neutral forms ('{name}: connessione persa', '{name} è di nuovo al tavolo', '{name} ha lasciato il tavolo').
- UI51ConnectionOverlay:107 and UI51SystemBuilder:174: wording consistent with the bot ('Intanto gioca un bot al tuo posto').
- 'Capp.' -> 'Cappotto' if it fits.
- 'RIMASTE' -> 'CARTE RIMASTE' only if it fits.
- TurnController.GetSimpleDisplayName: delegate to GameSocialV2.PlayerName. It is UIV2 code; check the assembly boundary first and use reflection or keep it local if Gameplay cannot reference it.
The GameScene placeholders are harmless while the bindings run. Optionally empty them in the builder.

**Files.** Assets/Scripts/UI/UI51ResultsView.cs, Assets/UIV2/Scripts/Core/MatchResultsV2.cs, Assets/Scripts/Networking/NetworkGameController.cs, Assets/Scripts/UI/UI51ConnectionOverlay.cs, Assets/UI51/Editor/UI51SystemBuilder.cs, Assets/Scripts/Gameplay/TurnController.cs (name helper only, mind the asmdef boundary)

**Risk.** Low.
- Text length changes may overflow fixed TMP rects ('SMAZZATA' is longer than 'MANO'). Verify with the Ellipsis rect rule.
- Changing the name source in the TurnController detail may cross the Gameplay/UI assembly boundary.

**Decision.** - Reconnection wording: 'gli altri ti aspettano' or 'gioca un bot al tuo posto'.
- Whether notices must be gender-neutral.
- Whether the result header should say SMAZZATA (recommended) or keep MANO.
- The replacement for 'HOST'.

**Test plan.** 1. In the Simulator: end of smazzata and end of match in 1v1, 2v2 and 1v3, as master and as guest. Check the captions and the countdown reaching 1.
2. Two-client Photon session: disconnect a client, then reconnect. Check that both sides' notices use the banner name and that the waiting text matches the bot behaviour.
3. Dealer accuso and tre assi details show the same names as the banners.
4. Pixel-check that no text overflows.

**Verifier (confirmed).** Each cited text checks out:
- UI51ResultsView.cs:155 uses ' · MANO ' + RoundIndex. RoundIndex is the smazzata counter (MatchScore.cs:76, 80), while UI51TableMoments.cs:139 uses MANO for the 3-card hand.
- MatchResultsV2.cs:420 'ATTENDI L'HOST'.
- MatchResultsV2.cs:422-426 shows 'tra 1 secondi' (CeilToInt).
- NetworkGameController.cs:273-275 and 293 use the raw NickName, masculine. Line 730 is masculine and uses PlayerName.
- UI51ConnectionOverlay.cs:107 and UI51SystemBuilder 'Gli altri giocatori ti aspettano' contradict MarkPlayerDisconnected, which converts the seat to a bot immediately (GameSceneInitializer.cs:298-312, TurnController.OnPlayerConvertedToBot).
- 'Capp.' (UI51ResultsView.cs:224).
- 'RIMASTE' (UI51TableBuilder.cs:437; GameScene.unity:9612).
- GetSimpleDisplayName 'Giocatore N' (TurnController.cs:1020-1028, used at 830, 895, 975).

The placeholders are correctly called harmless:
- MessageText is itself inactive.
- MoveSelectionUI hides titleText/messageText at build (MoveSelectionUI.cs:282-283).
- DealerAccusoReveal is inactive until Show.

**Corrected cause.** As stated, plus a fifth name source the investigator missed. While a player is disconnected, their seat is in botIndices: GameSceneInitializer.SetupGameModeProvider adds _disconnectedPlayerIndices (351-353), and MultiplayerGameModeProvider.IsHumanPlayer = !IsBotPlayer (MatchConfig.cs:236-237). So the banner (PlayerBannerManager.GetDisplayName -> 'Bot N', 511/521) and every GameSocialV2.PlayerName consumer (135: scope viewer title, scopa moment, accuso 'Who', results race names) show 'Bot N' for that player. Meanwhile the notice says '<NickName> si è disconnesso'. ApplyLook also reverts the banner to the default bot look (LookOf 456-463).

- `Assets/Scripts/Gameplay/GameSceneInitializer.cs:298-312, 349-353` MarkPlayerDisconnected rebuilds the provider with the disconnected seat as a bot.
- `Assets/Scripts/Core/MatchConfig.cs:236-237` IsHumanPlayer = !IsBotPlayer. A disconnected human counts as a bot for every name helper.
- `Assets/Scripts/UI/PlayerBannerManager.cs:508-522` The banner name for a non-human seat is 'Bot {p+1}'.
- `Assets/UIV2/Scripts/Core/GameSocialV2.cs:132-139` PlayerName returns 'Bot N' once IsBotPlayer is true. It strips '<' from nicknames, which the raw notices do not.
- `Assets/Scripts/Networking/NetworkGameController.cs:266, 273-275, 722-723` The disconnect notice is built after MarkPlayerDisconnected (266). RPC_SeatInactive builds its name before MarkPlayerInactive (722 before 723).
- `Assets/Scripts/Gameplay/MoveSelectionUI.cs:282-283` The placeholder title/message texts are hidden at runtime.

**Fix concerns.** The sketch says to swap NetworkGameController:273-275 to GameSocialV2.PlayerName(seat) 'as line 730 already does'. Done literally, that prints 'Bot 2 si è disconnesso': MarkPlayerDisconnected at line 266 has already made the seat a bot, so PlayerName returns 'Bot N'. The name must be read before line 266, as RPC_SeatInactive does at 722.
- Line 293 (rejoin) is fine, because MarkPlayerReconnected runs first and restores the human seat.
- TurnController (Gameplay asmdef) cannot reference GameSocialV2 (UIV2 depends on Gameplay). The name helper needs reflection or must stay local.
- 'SMAZZATA' is longer than 'MANO', so check the roundMode rect.

### Missed by the investigator (found by the verifier)

- **X2**: While a player is disconnected (inside the rejoin window), their seat counts as a bot. Banner name, banner look, scope-viewer title, scopa-moment and accuso 'Who' lines, and results race names all switch to 'Bot N'. The notice at the same moment says '<NickName> si è disconnesso / Gioca un bot finché non rientra', so the same person appears under two names. This also breaks the X2 fix as sketched: GameSocialV2.PlayerName called after MarkPlayerDisconnected returns 'Bot N'.
  - `Assets/Scripts/Gameplay/GameSceneInitializer.cs:349-353` _disconnectedPlayerIndices are added to botIndices.
  - `Assets/Scripts/Core/MatchConfig.cs:236-237` IsHumanPlayer = !IsBotPlayer.
  - `Assets/Scripts/UI/PlayerBannerManager.cs:393, 508-522` RefreshUI51 sets the banner name from GetDisplayName, which gives 'Bot {p+1}' for a non-human seat.
  - `Assets/UIV2/Scripts/Core/GameSocialV2.cs:135` PlayerName returns 'Bot N' for bot seats.
  - `Assets/Scripts/Networking/NetworkGameController.cs:266, 273-275` The seat is converted before the notice text is built.
- **A3**: The human's Matta is transformed into the accuso card at deal time, on every hand render, whether or not the player declares (presses the fist). The 'looks like a real copy' effect is present for the whole 3-card hand, including the accuso window, not only after an accuso.
  - `Assets/Scripts/Gameplay/CardViewManager.cs:1258-1259` ApplyMattaSpecialVisual(handCards) runs unconditionally after the human hand layout.
  - `Assets/Scripts/Gameplay/CardViewManager.cs:1105-1126` Only AccusiChecker.MattaValueForAccuso(hand) decides. No declared-accuso check.
- **A1**: Touch-down itself counts as pointer movement (the pointer jumps from the previous touch's position). The hover lift and the swap back to the real 7 therefore fire on every touch, not only on a moving hold. Unity's SendMouseEvents sends OnMouseDown before OnMouseEnter in the same frame, so on accused opponent cards the viewer opens first and then the card under the veil lifts and turns into the 7 until release. On your own off-turn Matta, a tap flashes the 7 and a hold keeps it: same feature, different duration.
  - `Assets/Scripts/Gameplay/CardView.cs:299-313` PointerMoved compares with the last frame. The comment states the touching finger moves the pointer and lifts the card.
  - `Assets/Scripts/Gameplay/CardView.cs:381-416` OnMouseEnter/OnMouseOver -> TryBeginHover -> lift plus originalFaceSprite swap.
  - `Assets/Scripts/Gameplay/CardView.cs:274-292, 425-449` The hold persists while a finger is down (PointerLive). EndHover on release restores the temporary value.
- **A3**: The A3 fix as sketched would clear a shared outline inside ClearMattaTransform. In 2v2/1v3 that removes the gold accused border from an opponent's Matta once their hand drops to 2 cards: RenderAIHandsDynamic sets AccusedOutline, then ApplyMattaSpecialVisual calls ClearMattaTransform when MattaValueForAccuso returns -1. The outline is one SpriteRenderer per card, shared by tre assi, accused gold, and (as proposed) the Matta marker and move hint.
  - `Assets/Scripts/Gameplay/CardViewManager.cs:254, 357-361` The accused gold outline is set per card, then ApplyMattaSpecialVisual runs for the accused hand.
  - `Assets/Scripts/Gameplay/CardViewManager.cs:1114-1118` ClearMattaTransform is called when the Matta no longer qualifies (2 cards left).
  - `Assets/Scripts/Gameplay/CardView.cs:987-1020` There is a single 'Outline' child renderer per card.

### Cross-cutting notes

- **text-content** (X2, reconnection notices, dealer accuso / tre assi details, results race names): The same player can be named differently on the same screen, or blank. Four helpers produce display names:
- TurnController.GetSimpleDisplayName ('Giocatore N');
- raw Photon NickName in NetworkGameController notices;
- GameSocialV2.PlayerName (sanitized, 'Tu', 'Bot N', 'Giocatore N' fallback);
- PlayerBannerManager.GetDisplayName (PlayFab name for local).
  - `Assets/Scripts/Gameplay/TurnController.cs:1020-1028` GetSimpleDisplayName
  - `Assets/Scripts/Networking/NetworkGameController.cs:273-275, 293` Raw otherPlayer.NickName / newPlayer.NickName
  - `Assets/UIV2/Scripts/Core/GameSocialV2.cs:132-139` PlayerName
  - `Assets/Scripts/UI/PlayerBannerManager.cs:508-522` GetDisplayName
- **input-gating** (A1, accuso window flow, card play during redeal): World cards act on touch-DOWN (OnMouseDown) while all uGUI acts on release. In addition, a card tapped during the accuso window or the redeal visual is queued as pendingLocalMove and auto-played once TurnController stops being busy. A tap meant as 'look' while the accuso fist is up can play a card seconds later.
  - `Assets/Scripts/Gameplay/CardView.cs:231-260` OnMouseDown acts at touch-down
  - `Assets/Scripts/Gameplay/TurnController.cs:713` DeclareInitialAccusiWithDelay sets isRedealPendingVisual; the accuso window runs inside it
  - `Assets/Scripts/Gameplay/TurnController.cs:1145-1174, 1277-1283` A local move while busy is stored in pendingLocalMove and replayed in Update
  - `Assets/Scripts/Gameplay/TurnController.cs:235, 425` IsAccusoWindowOpen / IsBusy
- **design-gap** (A3, O1, dealer accuso glow): One soft circular glow sprite ('Bagliore morbido cerchio', alpha about 30/255 at the card edge, drawn at card sortingOrder - 10 behind the card and its neighbours) is used for four different meanings: the move hint, the gold 'your turn' glow, the Matta halo and the dealer accuso glow. None of them reads clearly on a phone, and they suppress or compete with each other. The crisp CardView.SetOutline border already exists and is proven visible (tre assi).
  - `Assets/Scripts/Gameplay/CardView.cs:933-950, 958-984, 1060-1063` Halo, hint and glow all on the soft sprite. Hint disabled under the halo. sortingOrder card-10.
  - `Assets/Scripts/Gameplay/CardViewManager.cs:1284-1285, 1293-1302` Hint, turn glow and dealer accuso glow share moveHintGlowSprite
  - `Assets/Scripts/Gameplay/CardView.cs:992-1020` SetOutline crisp 9-slice border
- **other** (A2, A3, X1, X2): Builder re-run risk. A2, A3 (accuso reveal), X1 and part of X2 need changes in UI51TableBuilder, UI51RulesBuilder, UI51AccessBuilder and UI51SystemBuilder, then a re-run that regenerates GameScene/MainMenu hierarchies. Any later manual tweak under those roots is lost. Builders must force-open their target scene, and other agents may be using the Editor concurrently.
  - `Assets/UI51/Editor/UI51TableBuilder.cs:157-247, 550-696, 706-787` Banner, scope and accuso geometry lives only in the builder
  - `Assets/Scenes/MainMenu.unity:146053, 206099, 235135, 250601` Serialized builder texts
- **text-content** (X2): GameScene still holds design-time placeholder strings that become visible if a binding fails or for one frame before the runtime overwrite.
  - `Assets/Scenes/GameScene.unity:2143` 'SCOPA DA 30!' in the dealer reveal Title (overwritten by DealerAccusoRevealController.Show)
  - `Assets/Scenes/GameScene.unity:3607` 'New Text' in MessageText
  - `Assets/Scenes/GameScene.unity:8207` 'Scegli mossa' TitleText

## Riconnessione / sincronizzazione partita / UI disconnessione

### R1 — likely_cause · verifier: partially_confirmed · size M

**Current behaviour.** In-game rejoin flow (CONFIRMED by code): OnDisconnected pauses the turn timer and starts ReconnectAndRejoinLoop (NetworkGameController.cs:306-317). On OnJoinedRoom (370-400) the client immediately sets SetConnected(true) (381, which also clears the one-move-per-turn guard, TurnController.cs:378-382), hides the overlay and shows 'Sei di nuovo in partita!' (382-383), BEFORE any state has arrived. Then: if it is not master it sends ONE fire-and-forget RequestResync (398, 502-513), the master answers via SendStateWhenSettled after up to 5 s (520-556), and SetNetworkGameState replaces hand/table/deck/turn/dealer index/scores/accusi and replays recent moves (TurnController.cs:23-97). If the rejoiner IS master it requests nothing and broadcasts nothing: it keeps its own state as authority (389-394, comment 'Tutti gli altri sono usciti'). What the snapshot carries (GameStateSerializer.cs:17-41): hands, captures, scope, accusi, TotalScore, MatchTotals, table, deck, dealer, current player, round index, rules. NOT carried/re-presented: turn timer (rejoiner restarts at 30 s, referee keeps its own count, TurnController.cs:403), round/match-end results (ShowRoundEndPanel only from ApplyMoveInternal/intro, TurnController.cs:789,1543,1686), dealer chip (only set inside the intro, TurnController.cs:913). Players/avatars are fine: banners poll the live state and Photon props every 0.2 s (PlayerBannerManager.cs:60).

**Root cause.** The snapshot itself is complete enough; the defect is in WHO sends it and WHEN, plus how a stale phone is (not) detected. CONFIRMED in code: (1) A rejoiner that is master never resyncs and never re-broadcasts (NetworkGameController.cs:389-394). PUN's own source says this happens: 'the Master Client disconnects locally and uses ReconnectAndRejoin before anyone (including the server) notices' (Assets/Photon/PhotonUnityNetworking/Code/PhotonHandler.cs:341-342). Moves the server relayed to the dead connection in that gap are lost on the master, and the other phone is ahead. (2) A stale phone is only noticed when a later move arrives with a HIGHER TurnId (TurnController.cs:1217-1221) or turns out invalid (1236-1248). Moves with a LOWER TurnId are dropped silently on every other phone, and the sender is never told. The sender, though, applies its own AllViaServer echo against its stale state because the TurnId matches there, so the two phones drift further apart. On the master, an 'ahead' move from a seat that is not its current player is ignored too (1322-1325). In practice the move that finally breaks the deadlock is the referee's forced move after 30+10 s (TickTurnTimer 411-421): it arrives ahead or invalid and triggers SendInitialGameState/RequestResync. That matches 'si sistema solo quando scade il timer e viene giocata una carta in automatico'. (3) The non-master resync is a single unacknowledged request, sent with input already enabled and the overlay already gone. If that request or its answer is lost (master switching at that moment, master phone itself backgrounded), nothing retries. Recovery again waits for an ahead move. HYPOTHESIS (needs device logs): which of (1) or (3) the tester hit. Previous fix 2.59 (SPRINT_BACKLOG.md:158) only covered the 'move in flight' case.

**Evidence.**
- `Assets/Scripts/Networking/NetworkGameController.cs:370-400` OnJoinedRoom: SetConnected(true)+Hide overlay before the state; master branch resumes from own state, non-master sends one RequestResync
- `Assets/Scripts/Networking/NetworkGameController.cs:502-513, 546-556` RequestResync fire-and-forget; master waits up to 5 s (IsBusy / own move in flight) before answering
- `Assets/Photon/PhotonUnityNetworking/Code/PhotonHandler.cs:341-342` PUN source: inactive master is replaced, BUT a master can ReconnectAndRejoin before the server notices (stays master)
- `Assets/Scripts/Gameplay/TurnController.cs:1215-1222` behind move: dropped with no resync; ahead move: RequestNetworkResync
- `Assets/Scripts/Gameplay/TurnController.cs:1320-1334` master ignores an ahead/invalid move unless it is for its current player; otherwise broadcasts its own (possibly stale) state
- `Assets/Scripts/Gameplay/TurnController.cs:394-422` referee forces the stalled seat's move at 30+10 s: the move that 'fixes' the table
- `Assets/Scripts/Gameplay/TurnController.cs:357-362` TurnId counts cards only: two divergent states can have the same TurnId, so a stale phone validates and applies moves silently
- `Assets/Scripts/Core/GameStateSerializer.cs:17-41` snapshot fields: no timer, no results/finished flag, no player look
- `Assets/Scripts/Gameplay/TurnController.cs:913` dealer chip set only in PlayDealerDeclareSequence; SetNetworkGameState non-fresh path (91-94) never refreshes it
- `SPRINT_BACKLOG.md:158` 2.59 claimed 'niente si sistema alla mossa dopo' by replaying moves in flight: the master-rejoin and lost-request cases were not covered

**Fix sketch.** All in NetworkGameController.cs, reusing what is there. (a) OnJoinedRoom master branch (389-394): if any other non-inactive player is in the room, StartCoroutine(SendStateWhenSettled(null)) before OnBecameMasterClient, so every phone converges on one state at once instead of after a timer-driven invalid move. (b) Non-master branch: do not call SetConnected(true), Hide the overlay or show the notice yet. Set _awaitingRejoinState=true and reuse RequestInitialGameStateUntilReceived (469-494) with its loop condition widened to 'GameState == null || _awaitingRejoinState'. It re-sends RPC_RequestInitialGameState every 1.5 s. If this phone becomes master meanwhile, it does (a) and stops. In RPC_ReceiveInitialGameState (737+), when _awaitingRejoinState is set: clear it, then SetConnected(true), UI51ConnectionOverlay.Hide() and the 'Sei di nuovo in partita!' notice. The overlay veil already blocks card taps (CardView.IsPointerOverUI, CardView.cs:239/255), so input stays gated until the snapshot. Bound the wait by the seat deadline, then ShowError. Optional second step (needs decision): in TurnController.cs:1220, when this phone is master and a behind move arrives from a non-referee sender, send that sender the state (throttled 2 s).

**Files.** Assets/Scripts/Networking/NetworkGameController.cs, Assets/Scripts/Gameplay/TurnController.cs (only for the optional behind-move reply)

**Risk.** Broadcasting the master's state rolls back moves that only the other phones saw: that player's card goes back to hand, and SetNetworkGameState gives the current player a fresh timer (new state reference resets turnElapsed, TurnController.cs:403). The stateGen bump skips in-flight animations, which are replayed from recentNetworkMoves. Deferring SetConnected(true) keeps the timer paused and the overlay up longer: if the master never answers, the bound and ShowError must still fire. The RejoinedAfterRestart path in RPC_ReceiveInitialGameState (758-766) must keep working: same method, different flag. The optional behind-move reply also fires on legitimate owner/referee duplicates (harmless identical state, but it resets the receiver's timer).

**Decision.** When a rejoining master missed moves the others already applied: accept rolling everyone back to the master's state (smallest, consistent), or have the master pull the state from the most advanced active phone (new RPC, bigger)? Also: should the master answer behind moves with a state (optional step)?

**Test plan.** Play Mode with ParrelSync (Editor + clone), 1v1 private room. (1) On the MASTER instance call PhotonNetwork.NetworkingClient.SimulateConnectionLoss(true) for about 3 s, then false, while the clone plays a card in that gap. After the rejoin, compare GameStateSerializer.Serialize(turnController.GameState) on both instances (UnityMCP execute_code, read-only): they must match within 2 s without waiting for the 40 s referee move. (2) Same on the NON-master, plus drop the first RPC_RequestInitialGameState (temporarily ignore it on the master): the retry must still sync and the overlay must stay up until then. No useful EditMode seam beyond the existing serializer tests. Device (needed because the iOS suspend timing cannot be reproduced in the Editor): two TestFlight phones, background each phone (master and non-master) for 20 s during its own turn and the opponent's, and capture the Xcode console '[NET]' lines ('Requesting', 'Resending', 'Mossa per il turno').

**Verifier (partially_confirmed).** Every code mechanism cited is real. OnJoinedRoom calls SetConnected(true), hides the overlay and shows the notice before any state arrives (NetworkGameController.cs:381-383). A master rejoiner neither requests nor broadcasts a state (389-394). The non-master sends one RequestResync (398, 502-513). Behind moves are dropped silently (TurnController.cs:1217-1221). The master ignores ahead moves for seats other than its current player (1322-1325). TurnId counts cards only (357-362). The rejoiner's timer restarts at 30 s (381, 403). The causal claim is weaker than presented. In the most common real case (iOS background longer than the server timeout), the server marks the actor inactive and moves master to the other phone (NetworkGameController.cs:416-442). The rejoiner is then a NON-master, and the code does resync: RequestResync, then SendStateWhenSettled within 5 s (546-556), then SetNetworkGameState. Neither (1) nor (3) explains a phone staying stale for 30-40 s on that path. Hypothesis (1), a master that rejoins as master before the server notices, depends on the server accepting a rejoin while the old actor is still active. The bundled Realtime source says that join is refused with 32746 'ActiveActors already contains an actor' (LoadBalancingPeer.cs:1294-1296). The project already handles 32746 as 'old connection still active, retry' in HomeConnectionWatcher.cs:66,116-120. If the server refuses, case (1) never happens. Instead the in-game loop gets stuck: see R3 and missed_issues. The PUN comment (PhotonHandler.cs:341-342) contradicts this, so (1) is not refuted, only unproven. Hypothesis (3) is a narrow race, since RPCs are reliable. One statement is wrong: the snapshot DOES carry the round/match-finished information. RoundEnded is GameStateSerializer.cs:31 and MatchTotals is :40, so 'finished' can be derived via MatchScore.IsFinished. Only the presentation is missing (R2). The R2 presentation gaps (dealer chip, stale deal coroutines, suppressed card visibility) can also make a correctly applied snapshot look stale. Device logs are needed before choosing a fix.

**Corrected cause.** Confirmed in code: the rejoin unlocks input and hides the overlay before the snapshot arrives, the resync is a single request, a master rejoiner never re-broadcasts, and divergence is detected only through ahead or invalid moves. Not proven to be the cause of the report: on the usual iOS-background path the rejoiner is non-master and the master answers within 5 s. A rejoin that is still master requires the Photon server to accept an active-actor rejoin. Bundled docs (LoadBalancingPeer.cs:1294-1296) say it returns 32746, and the in-game loop then never retries (NetworkGameController.cs:329-336). Candidate causes to tell apart with device logs: (a) the master-rejoin race, (b) a lost or late resync, (c) a snapshot that was applied but is rendered stale by the R2 presentation gaps (stale intro/redeal coroutine, dealer chip, visibility suppression).

- `Assets/Scripts/Networking/NetworkGameController.cs:416-442` When the server notices the master went inactive, the other phone becomes master and broadcasts. The rejoiner is then non-master and does resync.
- `Assets/Photon/PhotonRealtime/Code/LoadBalancingPeer.cs:1294-1296` 32746: join refused while ActiveActors still contains the actor. Contradicts the assumption that a master can rejoin before the server notices.
- `Assets/Scripts/Networking/HomeConnectionWatcher.cs:66, 116-120` The team already observed and handles 32746 as 'old connection still active ~10 s, retry'
- `Assets/Scripts/Core/GameStateSerializer.cs:31, 40` RoundEnded and MatchTotals ARE serialized; the 'no finished flag' note is wrong
- `Assets/Scripts/Networking/NetworkGameController.cs:546-556` Master answer is bounded at 5 s even while busy
- `SPRINT_BACKLOG.md:221` Backlog says the timer restarts at 30 only 'al turno dopo il rientro'. The code resets it immediately (TurnController.cs:381 timerKey=-1, then 403).

**Fix concerns.** (b) If the waiting rejoiner becomes master (OnMasterClientSwitched, NetworkGameController.cs:416-442), that path must also clear the wait flag and call SetConnected(true) and Hide(). Otherwise timerPaused stays true (TurnController.cs:380, 401) and the new master never referees stalled human seats. The overlay countdown freezes once the loop stops, because ShowReconnecting is only called from ReconnectAndRejoinLoop (337): it needs a 'sync' label or periodic updates. (a) Rolling back to the master's state discards moves the others applied, and it gives the current player a fresh 30 s (403). RPC_ReceiveInitialGameState accepts a state from any sender, with no info.Sender.IsMasterClient check (737-755). Widening its role (wait gate, master broadcast) makes this forged-state hole matter more, so add the sender check in the same change. Do not ship (a) before device logs show the master-rejoin case actually happens.

### R2 — root_cause_confirmed · verifier: confirmed · size M

**Current behaviour.** Traced in code, one phase at a time. (a) DURING YOUR OWN TURN: the turn timer pauses on the disconnect (NetworkGameController.cs:311). A card tapped in the gap is lost (SendMove runs while InRoom is still true), and sentKey blocks a second tap until the rejoin resets it (TurnController.cs:1194-1198, 381). On the other phones the seat stays 'human' until the server notices. Until then the referee forces the move at 40 s and counts a strike (TurnController.cs:365-372, 411). Once Photon raises Leave(inactive), the master turns the seat into a bot (NetworkGameController.cs:266, GameSceneInitializer.cs:298-312) and the bot plays it 2 s later (TurnController.cs:115-128). After the rejoin the seat is human again (NetworkGameController.cs:282-294). The rejoiner's timer restarts at 30 s, while the referee counts from its own turn start. (b) DURING THE OPPONENT'S TURN: their moves are lost for the dropped phone and recovered only by the snapshot (see R1). (c) DURING THE DEAL: the intro coroutine (DeclareInitialAccusiWithDelay, TurnController.cs:711-806) and the mid-round redeal coroutine (HandleNewHandsRevealSequence, 1577-1703) keep running after a non-fresh snapshot. SetNetworkGameState clears their flags (70-76) but does not stop them; the redeal coroutine has no handle and no stateGen check at all. They then re-stage and animate stale card views and re-open a 5 s accuso window on a mid-round hand (RunAccusoWindowCoroutine sets isAccusoWindowOpen=true, 1090), which also pauses the turn timer (404). Card visibility suppression (SetSuppressNewCardVisibility) is not reset by SetNetworkGameState. A rejoin that misses a deal gets no deal animation, no accuso window and no 'MANO n DI m' notice; the master's fallback still declares the accuso, so no points are lost. A fresh snapshot replays the whole intro, so the rejoiner lags several seconds while moves queue up. (d) BETWEEN SMAZZATE: a RoundEnded snapshot shows the bare table with no results panel (SetNetworkGameState never calls ShowRoundEndPanel), until the master auto-advances (MatchResultsV2.cs:474-481). If the MATCH ended while you were offline: no match results, no RecordMatch (no XP/coins/stats request), and you stay on a dead table. A rematch from the host rescues you; a host who leaves does not. The dealer chip stays on the previous dealer if the new smazzata's intro was missed. (e) RIGHT AFTER A CAPTURE: a capture animation running when the snapshot lands is cut correctly (stateGen, TurnController.cs:1342,1459,1502). But if that capture emptied the hands (redeal) or ended the smazzata, cases (c) and (d) apply.

**Root cause.** SetNetworkGameState (TurnController.cs:23-97) swaps the data but not the presentation pipeline. Deal/redeal coroutines started on the old state are not cancelled and have no stateGen guard. The non-fresh path does not re-run what the normal flow does after a state change: dealer indicator, results panel for a RoundEnded state, reset of card-visibility suppression. The timer is not part of the snapshot by design: 'al rientro riparte da 30', SPRINT_BACKLOG.md:221/294.

**Evidence.**
- `Assets/Scripts/Gameplay/TurnController.cs:62-94` SetNetworkGameState resets flags, ForceRefresh only; no StopCoroutine(introCoroutine), no suppression reset, no dealer chip, no results
- `Assets/Scripts/Gameplay/TurnController.cs:697-702` introCoroutine stopped only when a NEW fresh intro starts (and its finally is skipped)
- `Assets/Scripts/Gameplay/TurnController.cs:1564-1575` redeal coroutine started without a handle; guard isRedealAnimationInProgress is cleared by SetNetworkGameState, so a second one can run concurrently
- `Assets/Scripts/Gameplay/TurnController.cs:1085-1091` accuso window re-opened by the stale coroutine on the new state
- `Assets/Scripts/Gameplay/TurnController.cs:786-790, 1541-1545, 1683-1687` only callers of ShowRoundEndPanel: a RoundEnded snapshot never shows results
- `Assets/UIV2/Scripts/Core/MatchResultsV2.cs:47, 102, 292-327` RecordMatch only via Show; de-dup is by object reference (comment at 47 already flags that a resync could count twice)
- `Assets/Scripts/Gameplay/TurnController.cs:874-916` dealer chip set only in PlayDealerDeclareSequence (913)
- `Assets/Scripts/Networking/NetworkGameController.cs:241-276, 282-294` seat goes to bot on Leave(inactive), back to human on rejoin Join event (confirmed in PUN LoadBalancingClient.cs:3436-3453, 3466-3485)

**Fix sketch.** TurnController only. (1) In DeclareInitialAccusiWithDelay and HandleNewHandsRevealSequence, capture int gen = stateGen at the start. After each yield, if (gen != stateGen) yield break, and wrap their finally flag/visibility resets in if (gen == stateGen) so they do not clobber a newer intro. (2) In SetNetworkGameState's non-fresh branch (91-94): call cardViewManager.SetSuppressNewCardVisibility(false) before ForceRefresh, then SetDealerIndicatorViaReflection(true). If gameState.RoundEnded, call ShowRoundEndPanel(). (3) In MatchResultsV2, de-dup Show/RecordMatch by value (RoundIndex + MatchTotals + RoundEnded) instead of by reference (47, 102, 278), so a re-sent end-of-smazzata state does not count twice.

**Files.** Assets/Scripts/Gameplay/TurnController.cs, Assets/UIV2/Scripts/Core/MatchResultsV2.cs

**Risk.** The intro/redeal finally blocks are the safety net that restores card visibility. Gating them on gen means SetNetworkGameState must take over that duty (point 2), or cards can stay invisible. Showing results from a snapshot can call RecordMatch, which requests server rewards. Coins and stats are capped server-side (60/day) but a normal win is client-asserted, so the value de-dup in (3) is required before (2). The Tre assi fresh state goes through the fresh path, so do not double-handle it. Training is unaffected (no network snapshots).

**Decision.** Should a phone that rejoins after the round or match has ended see the results panel (and get its XP/coins)? Or is the current behaviour (table only, wait for the next smazzata) acceptable?

**Test plan.** Play Mode with ParrelSync and SimulateConnectionLoss on the non-master: drop (i) right after the master's StartNewGame (deal), (ii) on the last card of a hand (redeal), (iii) on the last move of a smazzata and of the match, (iv) right after a capture. Check: a single accuso window, cards visible after the snapshot, correct dealer chip, results panel shown exactly once, and RewardsService.MatchReward called once (log). EditMode: add a test for the MatchResultsV2 value-based de-dup helper (pure function over two GameStates).

**Verifier (confirmed).** I saw each mechanism myself. SetNetworkGameState resets the animation and deal flags (TurnController.cs:62-77) and only calls ForceRefresh on the non-fresh path (91-94). It does not stop introCoroutine, which is only stopped by a new fresh intro at 697-702. HandleNewHandsRevealSequence is started without a handle (1574) and has no stateGen check (1577-1703). Its guard isRedealAnimationInProgress is cleared by SetNetworkGameState (73), so a replayed redeal can start a second one (1566). A stale coroutine re-runs RunAccusoWindowCoroutine, which sets isAccusoWindowOpen=true (1090). That pauses TickTurnTimer (404). ShowRoundEndPanel is only called at 789, 1543 and 1686, never from SetNetworkGameState. The dealer chip is only set at 913: PlayerBannerManager.Refresh (66-122) never touches it, only SetDealerIndicatorForPlayer (134-149) does. Auto-advance runs only for a non-finished round on the master (MatchResultsV2.cs:474-481), and Next only on the master (439-448). A finished match therefore waits for a manual rematch. RecordMatch is reachable only from Show (102, 292). The capture path is cut by stateGen (1342, 1459, 1502). Seat to bot on Leave(inactive) and back on rejoin (NetworkGameController.cs:266, 292; GameSceneInitializer.cs:273-312) is confirmed. Nuance: SetNetworkGameState does not reset the visibility suppression, but a stale coroutine that keeps running resets it later (RunAccusoWindowCoroutine 1085-1088, finally 1666-1671). Invisible cards are therefore transient today. They become permanent only if the coroutine is cut, which the proposed fix does.

**Corrected cause.** As stated, plus two consequences found. (1) SetNetworkGameState clears pendingRedealVisualCopies (74) without DestroyVisualCopy. Played-card ghost copies parked there by ExecuteMoveWithAnimation (1490-1493) then leak if a snapshot lands before the redeal coroutine's cleanup (1632-1639). (2) A rejoiner whose match ended offline never calls RecordMatch, so ModerationService.MatchEnded is never called (MatchResultsV2.cs:296). If the user kills the app on the dead table, the next launch counts an abandonment (ModerationService.cs:66-91).

- `Assets/Scripts/Gameplay/TurnController.cs:74, 1490-1493, 1632-1639` Redeal ghost copies are dropped from the list without being destroyed when a snapshot lands mid-redeal
- `Assets/Scripts/Gameplay/TurnController.cs:1085-1088, 1666-1671` A still-running stale coroutine resets suppression later, so invisibility is transient today
- `Assets/Scripts/UI/PlayerBannerManager.cs:66-122, 134-149` The 0.2 s banner refresh never sets the dealer chip; only the intro's reflection call does
- `Assets/UIV2/Scripts/Core/MatchResultsV2.cs:439-448, 474-481` Auto-advance only when !finished and on the master; a finished match waits for the host's RIVINCITA
- `Assets/Scripts/Auth/ModerationService.cs:66-91, 153-156` Marker cleared only by MatchEnded (from RecordMatch); a dead-table app kill counts as abandonment at the next launch

**Fix concerns.** (2) Calling ShowRoundEndPanel from SetNetworkGameState also fires on duplicate RoundEnded snapshots, for example the master's SendInitialGameState from RequestNetworkResync (TurnController.cs:1333) or SendStateWhenSettled after a master switch. MatchResultsV2.Show would replay Reveal: animation, PopupOpen/Victory sound, autoAdvance restart (254-273). It needs a guard of the form 'already showing an equal state', not only RecordMatch de-dup. (1) Gating the coroutines' finally on gen moves the visibility and flag cleanup to SetNetworkGameState (suppression reset, destroy pendingRedealVisualCopies before Clear at 74). On the master stateGen never changes, so the master is unaffected. (3) Value de-dup must ignore an object that is mutated in place. Key it on RoundIndex plus a MatchTotals snapshot copied at Show time, not on references.

### R3 — root_cause_confirmed · verifier: confirmed · size M

**Current behaviour.** MatchmakingManager is DontDestroyOnLoad (MatchmakingManager.cs:43-52). Its OnDisconnected raises OnError($"Disconnesso: {cause}") for EVERY non-client-logic disconnect, whatever the matchmaking state (552-562). In MainMenu, RoomFlowV2 subscribes to OnError (175-176), and RoomFlowV2.Error unconditionally opens the matchmaking SearchPanel (215-217). It shows ModeLabel 'PARTITA 1 VS 1 · ONLINE' (CurrentConfig is null after MainMenu load, 62-69 and 378-383), the title 'Connessione non riuscita' and the raw cause as subtitle (e.g. 'Disconnesso: ClientTimeout'). Because isFound is false, the spinner keeps turning and 'Annulla ricerca' stays visible (UI51MatchmakingView.cs:45-57). So any Photon drop in the Home (an iOS app switch is enough), or a late OnDisconnected arriving after returning from a failed in-game rejoin, pops a searching-looking matchmaking card. HomeConnectionWatcher shows the proper overlay only after 3 s (HomeConnectionWatcher.cs:122-127, 144) and hides it on reconnect (148, 166-169), leaving the search card behind. In-game, the proper overlay is used, but with problems: (1) the copy says 'Gli altri giocatori ti aspettano' while a bot is actually playing your seat (UI51ConnectionOverlay.cs:107 vs NetworkGameController.cs:266, 273-275). (2) The seat countdown starts when the disconnect is DETECTED (NetworkGameController.cs:315), i.e. after resume on iOS, so it overstates the time left. ModerationService.Paused deletes the pause stamp on resume (ModerationService.cs:169-175). (3) After a refused rejoin, PUN goes back to the master server, not to Disconnected (LoadBalancingClient.cs:3008-3013, 2908-2917), and ReconnectAndRejoin only runs from Disconnected (1337-1341). The loop's idle check (NetworkGameController.cs:329-336) therefore never retries. It counts down 'Tentativo 1 di 5' for the full 60 s even when the seat or room is already gone (32748/32758), and only then shows 'Impossibile rientrare' (351-354). (4) The restart-rejoin from Home gives up silently: overlay hidden, no message (HomeConnectionWatcher.cs:80-82).

**Root cause.** MatchmakingManager.OnDisconnected reports every disconnect as a matchmaking error, and RoomFlowV2.Error opens the search panel without checking whether a search, room creation or join is actually in progress. Connection loss, rejoin attempt and rejoin failure are therefore rendered with the matchmaking UI. The dedicated overlay exists (UI51ConnectionOverlay, Fase 9) but its in-game copy and failure path are inaccurate, as listed above.

**Evidence.**
- `Assets/Scripts/Networking/MatchmakingManager.cs:552-562` OnError for any disconnect, raw enum text
- `Assets/UIV2/Scripts/Core/RoomFlowV2.cs:206-218` Error always shows SearchPanel with 'Connessione non riuscita'
- `Assets/UIV2/Scripts/Core/RoomFlowV2.cs:378-383` null config -> 'PARTITA 1 VS 1 · ONLINE' (the user's 'Ricerca 1v1 online')
- `Assets/Scripts/UI/UI51MatchmakingView.cs:45-57` isFound=false keeps spinner and 'Annulla ricerca': looks like a running search
- `Assets/Scripts/Networking/HomeConnectionWatcher.cs:122-150` proper Home overlay appears only after QuietSeconds=3 and hides on reconnect; the search card stays
- `Assets/Scripts/UI/UI51ConnectionOverlay.cs:107` 'Gli altri giocatori ti aspettano' contradicts the bot taking the seat
- `Assets/Scripts/Networking/NetworkGameController.cs:323-355` loop retries only from Disconnected; seat deadline set at detection time
- `Assets/Photon/PhotonRealtime/Code/LoadBalancingClient.cs:3008-3013, 2908-2917, 1337-1341` failed join on GameServer -> back to MasterServer + OnJoinRoomFailed; ReconnectAndRejoin needs a Disconnected peer
- `Assets/Scripts/Networking/HomeConnectionWatcher.cs:80-82` restart-rejoin failure hides the overlay without any message

**Fix sketch.** Core (smallest, fixes the reported UI): in MatchmakingManager.OnDisconnected, raise OnError only when State is Connecting, Searching, CreatingRoom, JoiningRoom, WaitingForPlayers or InWaitingRoom, with a readable text (e.g. 'Connessione persa. Controlla la rete e riprova.'). Home losses are then handled only by HomeConnectionWatcher's overlay and in-game losses only by NetworkGameController's. Failure path (same files as R1): cache the room name in NetworkGameController.Start. In the loop, when PhotonNetwork.IsConnectedAndReady && !InRoom, call PhotonNetwork.RejoinRoom(roomName) every 3 s, the same call HomeConnectionWatcher.cs:75 uses. Add OnJoinRoomFailed: 32746 (still active) retry; any other code ends the countdown at once and shows the existing 'Impossibile rientrare' notice. Record pause time in NetworkGameController.OnApplicationPause(true) and start _seatDeadline from it when the disconnect is detected within a few seconds of resume. Fix the overlay copy at UI51ConnectionOverlay.cs:107. On Home rejoin give-up, call UI51Toast.Show(..., Error) (existing API).

**Files.** Assets/Scripts/Networking/MatchmakingManager.cs, Assets/Scripts/Networking/NetworkGameController.cs, Assets/Scripts/Networking/HomeConnectionWatcher.cs, Assets/Scripts/UI/UI51ConnectionOverlay.cs

**Risk.** Gating OnError on State must keep the real search failures: a drop while searching or waiting must still reach RoomFlowV2. GameLaunchController only logs OnError (GameLaunchController.cs:185-188). RejoinRoom from the master server is the call HomeConnectionWatcher already uses for the same purpose. Its OnJoinedRoom also runs MatchmakingManager.OnJoinedRoom, which returns early for a closed room (424-428), so no side effects. Overlay text: TMP glyph coverage for any new character (avoid dingbats, see Fase 9 memory). The 32746 handling depends on Photon server behaviour, which PhotonHandler.cs:341-342 says can also be an accepted takeover, so keep both paths.

**Decision.** Wording for the in-game overlay (e.g. 'Intanto gioca un bot al tuo posto. Rientra entro Ns per riprenderlo.') and for the Home rejoin-failure toast.

**Test plan.** Play Mode in Editor: after entering the Home, run PhotonNetwork.NetworkingClient.SimulateConnectionLoss(true). Expect no SearchPanel, the UI51ConnectionOverlay after 3 s, and nothing left after SimulateConnectionLoss(false) and reconnect. Start a quick match, simulate loss: the search card shows the readable error. In GameScene (ParrelSync), drop the non-master for longer than PlayerTtl (or close the room on the master): the overlay must end quickly with 'Impossibile rientrare', not after 60 s. Device: iOS app switch of 20 s in Home and in game; check the seat seconds shown after resume. No EditMode test needed for the one-line state gate.

**Verifier (confirmed).** MatchmakingManager is DontDestroyOnLoad (MatchmakingManager.cs:43-52). It raises OnError($"Disconnesso: {cause}") on every disconnect other than client-logic, whatever its state (552-562). RoomFlowV2 sits on OnlineFlowV2, an active root GameObject in MainMenu.unity, so its Update runs and Subscribe hooks OnError (RoomFlowV2.cs:170-176, 448-450). Error always shows the SearchPanel with 'Connessione non riuscita' (206-218). After MainMenu loads, CurrentConfig is null (MatchmakingManager.cs:62-69), so ModeLabel shows 'PARTITA 1 VS 1 · ONLINE' (378-383). isFound=false keeps the spinner and 'Annulla ricerca' (UI51MatchmakingView.cs:45-57). busy=false after Error, so nothing auto-closes it (RoomFlowV2.cs:453-461). HomeConnectionWatcher's overlay appears after 3 s and hides on reconnect (HomeConnectionWatcher.cs:122-150, 166-169), leaving the search card behind. In GameScene only GameLaunchController listens, and it only logs; it lives only in MainMenu (GameLaunchController.cs:185-188). In-game: the overlay copy is at UI51ConnectionOverlay.cs:107. The seat deadline is set at detection time (NetworkGameController.cs:315), and the pause stamp is deleted on resume (ModerationService.cs:169-175). A failed join on the GameServer returns to the master server via DisconnectToReconnect, never to Disconnected (LoadBalancingClient.cs:3006-3010, 2908-2917, 3266-3269). ReconnectAndRejoin needs a Disconnected peer (1337-1341), and the loop only attempts when idle (NetworkGameController.cs:329-336), so it never retries. The restart rejoin gives up silently (HomeConnectionWatcher.cs:80-82). This also covers 32746 (old actor still active), not only 32748/32758. In that case the seat would become rejoinable seconds later, yet the player is held on 'Tentativo 1 di 5' and kicked after 60 s.

**Corrected cause.** As stated. Add: the failure path also covers the 32746 'old connection still active' refusal (LoadBalancingPeer.cs:1294-1296), which the Home path already retries (HomeConnectionWatcher.cs:116-120). In game, a valid seat is lost because the loop never calls RejoinRoom from the master server.

- `Assets/Scenes/MainMenu.unity:OnlineFlowV2 GameObject (fileID 152073541)` Active root with RoomFlowV2: subscribed to OnError for the whole MainMenu session, including before entry
- `Assets/Photon/PhotonRealtime/Code/LoadBalancingClient.cs:3225-3232, 3256-3258` On an unexpected disconnect from a game server, OnLeftRoom fires BEFORE OnDisconnected
- `Assets/Scripts/Networking/MatchmakingManager.cs:546-550` OnLeftRoom sets State=Idle, so the state is already Idle when OnDisconnected runs for in-room disconnects
- `Assets/Scripts/Networking/MatchmakingManager.cs:493-501` In GameScene CurrentConfig is non-null, so a failed in-game rejoin also fires OnJoinFailed/OnError (harmless today) and RefusedByServer, which can open UI51SuspensionView in game

**Fix concerns.** The proposed gate ('raise OnError only when State is Connecting/Searching/CreatingRoom/JoiningRoom/WaitingForPlayers/InWaitingRoom') breaks the in-room cases it means to keep. For a disconnect while in a room, Photon calls OnLeftRoom first (LoadBalancingClient.cs:3229-3232), and MatchmakingManager.OnLeftRoom already set State=Idle (546-550). By OnDisconnected, WaitingForPlayers, InWaitingRoom and Starting read as Idle. The quick-match wait or private lobby would then stay open with no error: RoomFlowV2.State(Idle) only clears busy (RoomFlowV2.cs:183) and does not hide the lobby. Fix: remember the state before Idle in OnLeftRoom, or decide in OnLeftRoom. RejoinRoom from the master server in GameScene also goes through MatchmakingManager.OnJoinRoomFailed (CurrentConfig non-null), and its RefusedByServer branch (508-515) can pop the suspension screen over the table. Treat 32746 as retry, and every other code as stop.

### Missed by the investigator (found by the verifier)

- **R3**: In-game rejoin refused with 32746 (old actor still active, e.g. the client timed out before the server did) or any other join error: PUN goes back to the master server, and the loop only acts when the client state is Disconnected, so it never retries. The player stays on 'Tentativo 1 di 5' and is sent to the menu after 60 s, even though the seat becomes rejoinable a few seconds later. RIPROVA (RetryRejoin) restarts the same no-op loop. The Home restart-rejoin already handles this with RejoinRoom plus a 32746 retry; the in-game path does not. This also argues against R1 hypothesis (1).
  - `Assets/Scripts/Networking/NetworkGameController.cs:327-336, 357-360` Attempts only when NetworkClientState==Disconnected; RetryRejoin restarts the same loop
  - `Assets/Photon/PhotonRealtime/Code/LoadBalancingClient.cs:3006-3010, 2908-2917` Failed join on the GameServer leads to DisconnectToReconnect, then ConnectedToMasterServer, then OnJoinRoomFailed
  - `Assets/Photon/PhotonRealtime/Code/LoadBalancingPeer.cs:1294-1296` 32746 = ActiveActors already contains the actor
  - `Assets/Scripts/Networking/HomeConnectionWatcher.cs:66-76, 116-120` The Home path already retries RejoinRoom on 32746
- **R2**: A phone that rejoins after its match ended offline never runs RecordMatch, so ModerationService.MatchEnded is never called and the in-progress marker stays. On a dead table with no results, the natural reaction is to kill the app; at the next launch ModerationService.Refresh sees a marker from another run and calls 'abbandono' (a strike, or a device abandon for guests) for a match that actually finished.
  - `Assets/UIV2/Scripts/Core/MatchResultsV2.cs:102, 292-296` MatchEnded only through RecordMatch, only through Show
  - `Assets/Scripts/Gameplay/TurnController.cs:91-94` Non-fresh snapshot path never shows results
  - `Assets/Scripts/Auth/ModerationService.cs:66-91` Marker from a previous run means closedMidMatch, which triggers the 'abbandono' call
- **R2**: SetNetworkGameState empties pendingRedealVisualCopies without destroying the visual copies. If a snapshot lands after a move's animation parked its played-card copy for the redeal (isRedealPendingVisual) but before HandleNewHandsRevealSequence destroys them, a ghost card stays on the table until the scene changes. The window is short (move finally plus a 0.2 s delay), but it widens with iOS suspend and resume.
  - `Assets/Scripts/Gameplay/TurnController.cs:74` pendingRedealVisualCopies.Clear() with no DestroyVisualCopy
  - `Assets/Scripts/Gameplay/TurnController.cs:1490-1493` Copies parked while a redeal is pending
  - `Assets/Scripts/Gameplay/TurnController.cs:1632-1639` The only place they are destroyed
- **R1**: RPC_ReceiveInitialGameState applies a full GameState from any sender: there is no info.Sender.IsMasterClient check. A modified client can overwrite hands, scores and the dealer on every phone. The R1 fix gives this RPC a bigger role (rejoin gate, master re-broadcast), so the sender check belongs in the same change. RPC_SeatInactive already checks the sender.
  - `Assets/Scripts/Networking/NetworkGameController.cs:736-755` No sender validation before SetNetworkGameState
  - `Assets/Scripts/Networking/NetworkGameController.cs:716` For comparison, RPC_SeatInactive validates master/referee

### Cross-cutting notes

- **resync-snapshot** (R1, any desync / stuck-match item in other clusters): There is no state version check. TurnId counts only cards (RoundIndex*100+99-cards), so two phones with different hands or tables share the same TurnId and validate each other's moves without noticing. Moves behind the local TurnId are dropped without telling the sender, and the master ignores ahead moves from seats other than its current player. Any QA item about 'different cards on the two phones', 'match stuck until the timer' or 'phantom card' probably shares this cause.
  - `Assets/Scripts/Gameplay/TurnController.cs:357-362` TurnId definition
  - `Assets/Scripts/Gameplay/TurnController.cs:1215-1222` behind moves silently dropped
  - `Assets/Scripts/Gameplay/TurnController.cs:1320-1325` master returns without resync for a non-current-player move
- **event-subscription-leak** (private room join errors (other cluster)): RoomFlowV2 subscribes an anonymous lambda to the DontDestroyOnLoad MatchmakingManager.OnJoinFailed and never removes it. Each MainMenu load adds one more delegate that captures a destroyed RoomFlowV2 (it writes a field, so it is harmless today, but it leaks).
  - `Assets/UIV2/Scripts/Core/RoomFlowV2.cs:175` manager.OnJoinFailed += code => joinFailure = code
  - `Assets/UIV2/Scripts/Core/RoomFlowV2.cs:471-479` OnDestroy unsubscribes everything except OnJoinFailed
- **photon-callbacks-order** (matchmaking / room cluster): MatchmakingManager keeps CurrentConfig for the whole GameScene; it is cleared only when MainMenu loads. Its OnPlayerEnteredRoom therefore runs LockRoomForMatch, SetState(Starting) and OnMatchFound on EVERY rejoin in a quick match, because Room.PlayerCount includes inactive players. Nothing in GameScene listens today, but any future OnMatchFound listener there would reload the table.
  - `Assets/Scripts/Networking/MatchmakingManager.cs:525-539` quick-match full-room check on any player entering
  - `Assets/Scripts/Networking/MatchmakingManager.cs:62-69` config cleared only on MainMenu load
  - `Assets/Photon/PhotonRealtime/Code/Room.cs:174-182` PlayerCount = Players.Count (inactive players included)
- **text-content** (R3, login/entrance cluster): The raw Photon DisconnectCause enum is shown to users ('Disconnesso: ClientTimeout', 'Disconnesso: CustomAuthenticationFailed'). It can also appear during login, because RoomFlowV2 is in MainMenu alongside the start screen.
  - `Assets/Scripts/Networking/MatchmakingManager.cs:556-559` OnError($"Disconnesso: {cause}")
- **server-idempotency** (R2, rewards / coins cluster): Rewards depend on the results panel. A phone that comes back after the match ended (via snapshot) never calls RecordMatch, so no XP/coins/stats request is made. Conversely, de-duplication is by object reference, so showing results from a re-sent snapshot could request rewards twice (normal wins are client-asserted; only the 60/day cap protects).
  - `Assets/UIV2/Scripts/Core/MatchResultsV2.cs:47` reference guard acknowledged as weak
  - `Assets/UIV2/Scripts/Core/MatchResultsV2.cs:102, 292-327` RecordMatch only from Show
- **session-state** (R3, moderation / abandon cluster): ModerationService.Paused deletes the pause timestamp on resume, so after an iOS background no in-game code knows when the seat countdown really started. NetworkGameController starts the 60 s at detection time. The abandon marker logic relies on the same stamp, but only across restarts.
  - `Assets/Scripts/Auth/ModerationService.cs:169-175` Paused(false) deletes ContactKey
  - `Assets/Scripts/Networking/NetworkGameController.cs:315` _seatDeadline = now + 60 at detection

## Sessione / account / guest / cache / doppio login

### S3 — root_cause_confirmed · verifier: confirmed · size S

**Current behaviour.** Going Account A -> Esci -> Ospite -> Accedi B carries data across identities. Three separate causes, all confirmed in code. (1) The 'guest' can actually be account A's own PlayFab account. (2) Account data sits in PlayerPrefs keys shared by the whole device. (3) Static caches are not cleared on logout or login: wallet, profile, block list.

**Root cause.** (1) PlayFabAuthService.LoginAsGuest picks its CustomId from PlayerPrefs keys that logout never clears. Logout runs LogoutAndRestart -> Logout() + ClearRealLoginFlag -> StartAuthentication -> LoginAsGuest -> GetOrCreateSessionGuestId, which returns the GUID stored earlier. If A was registered from that guest session (AddUsernamePassword adds credentials to the existing CustomID account, verified with Context7), that GUID still logs into A. Example: day 1 the user registers, so G1 becomes A. On day 2 the app starts with HasRealLogin=1, so Start skips the reset, and logout logs back into A. ForceGuestIdentity only clears DisplayName and the flags. It keeps whatever session exists, which on a returning-user launch is the persistent device-id account. The real-account branch at PlayFabAuthService.cs:194-212 even documents this: it hides the old registered name but keeps the account. Result: a 'guest' that is really A connects to Photon as A, publishes A's PlayFabId as the guest id 'og' (so reports against 'a guest' land on A), loads A's profile cache and is registered in the server's match records as A. (2) Device-global PlayerPrefs hold account data: Collection.Emoticons (see E3), progress_exp/progress_wins/progress_totalGames (local XP is the fallback for a registered account whose cloud profile has not loaded), Mail.ReadIds (global mail keeps the same id for every account, and MarkRead prunes the ids of other accounts), Social.MutedEmoticons, SelectedDeckId and Moderazione.AbbandoniPremiati. AccountDeletionService lists progress_* and Collection.Emoticons as account data, but only deletion clears them; logout does not. (3) Statics: WalletService.Coins/Gems/IsLoaded are never reset (new in 2.65: the top bar now shows them). ProfileService has no Reset, so IsLoaded and its caches stay those of the previous account until the new load succeeds. BlockList.Current keys its cache by PlayFabId plus the loaded flag, so a read in the window after login pins the previous account's list under the new id.

**Evidence.**
- `Assets/Scripts/Auth/AuthBootstrapper.cs:113-117` guest ids reset only at app Start and only when HasRealLogin==0
- `Assets/Scripts/Auth/AuthBootstrapper.cs:268-302` LogoutAndRestart resets FriendsChat/Rewards/Moderation only; no guest-id, Wallet or Profile reset; PlayFabAuth.Logout() (293) runs before ClearRealLoginFlag (296)
- `Assets/Scripts/Auth/PlayFabAuthService.cs:103-113` session guest id persisted in PlayerPrefs Project51_SessionGuestId and reused
- `Assets/Scripts/Auth/PlayFabAuthService.cs:157-161` returning users (HasRealLogin=1) log in with the persistent device-id CustomId, a separate 'device account'
- `Assets/Scripts/Auth/PlayFabAuthService.cs:194-212` registered account reached by CustomId is kept but shown as guest when flag is 0
- `Assets/Scripts/Auth/PlayFabAuthService.cs:121-128` ForceGuestIdentity keeps the current PlayFab session
- `Assets/Scripts/Auth/PlayFabAuthService.cs:306-319` Logout keeps device/session ids and never calls PlayFabClientAPI.ForgetAllCredentials (SDK keeps the old ticket)
- `Assets/Scripts/Auth/AuthBootstrapper.cs:165-167` guest publishes PlayFabId as 'og' guest id: equals A when guest==A
- `Assets/Scripts/Auth/WalletService.cs:18-41` static balances, no Reset; error path keeps old values
- `Assets/UIV2/Scripts/Core/HomeV2Integration.cs:270, 329` ReloadAccountProfile -> RefreshProfile -> RefreshWallet shows the previous balance before WalletService.Refresh (277)
- `Assets/Scripts/Auth/ProfileService.cs:49, 71-102` IsLoaded never cleared, no owner id; caches cleared only on successful reload (263, 297)
- `Assets/Scripts/Auth/AuthBootstrapper.cs:151` HasRealProfile = IsLoaded && HasRealLogin: true with previous account cache during reload
- `Assets/Scripts/Auth/FriendsService.cs:162-171` BlockList cache keyed PlayFabId+loaded; stale IsLoaded pins previous list under new id
- `Assets/UIV2/Scripts/Core/HomeV2Integration.cs:292` registered + cloud not loaded -> device-global PlayerProgressLocal.Exp
- `Assets/Scripts/UI/PlayerBannerManager.cs:446-452` table level falls back to device-global local XP
- `Assets/UIV2/Scripts/Core/MatchResultsV2.cs:309-311` xpFrom falls back to device-global local XP
- `Assets/Scripts/Auth/AccountDeletionService.cs:39-46` account-scoped keys acknowledged here; cleared only on deletion
- `Assets/Scripts/Auth/MailService.cs:45, 131-143` Mail.ReadIds device-global; MarkRead prunes ids not in the current account's list
- `Assets/Scripts/Auth/FriendsService.cs:101-122` Social.MutedEmoticons device-global (documented as device-only)
- `Server/CloudScript/51.js:403-406` server treats any account with Username as registered: a 'guest' that is really A is A server-side

**Fix sketch.** Block A, identity reset. (a) Make the hidden guest session always ephemeral. LoginAsGuest uses an in-memory GUID created once per process and set to null in Logout(). This deletes GetOrCreateSessionGuestId, the device-id branch and the HasRealLogin guard in AuthBootstrapper.Start, and turns the serverHasRealAccount branch into dead code. Minimal alternative that keeps the PlayerPrefs keys: always call ResetGuestDeviceId in Start and in LogoutAndRestart, and always use the session id. (b) Call PlayFabClientAPI.ForgetAllCredentials() in PlayFabAuthService.Logout(). (c) Add WalletService.Reset() (zero the balances, IsLoaded=false, raise Changed) and ProfileService.Reset() (clear both caches, IsLoaded=false). Call both from LogoutAndRestart and RebindPhoton, next to RewardsService.Reset/ModerationService.Reset. This also fixes HasRealProfile, BlockList and PublishLook during the reload window. (d) Local XP fallback: either drop progress.Exp for registered users (the server writes stats since 2.62) or delete the progress_* keys at logout. (e) Device-global prefs (muted list, deck, mail read state): leave them until the user decides.

**Files.** Assets/Scripts/Auth/PlayFabAuthService.cs, Assets/Scripts/Auth/AuthBootstrapper.cs, Assets/Scripts/Auth/WalletService.cs, Assets/Scripts/Auth/ProfileService.cs, Assets/UIV2/Scripts/Core/HomeV2Integration.cs

**Risk.** (a) creates one new throwaway PlayFab player per launch for returning registered users too. This already happens for guests; the device-id account was reused before. The memory note 'a real login may still auto-login' is wrong for the current flow (no LinkCustomID anywhere; the start screen always asks to log in), so nothing visible is lost. Older builds whose registered account was the device-id account still have username/password, so email login keeps working. ProfileService.Reset makes PlayerBannerManager/MatchResultsV2 fall back to local XP during the reload unless (d) is done. HomeV2Integration.CloudReady already guards the Home. Two EditMode tests call `new PlayFabAuthService()`: AccountDeletionTests:103 keeps working.

**Decision.** Should the hidden technical session be a fresh throwaway account on every launch (my default; today there is NO real auto-login), or do you want real auto-login of the last account (a new feature: link a device id at login, unlink at Esci)? Also: per account or per device for the muted-player list, selected deck and mail read state?

**Test plan.** EditMode: (1) PlayFabAuthService exposes the guest CustomId it would use; after Logout() it differs. (2) WalletService.Reset and ProfileService.Reset clear IsLoaded/balances. Play Mode in the Editor with two test accounts: log in A -> Esci -> Accedi come ospite. The console guest PlayFabId must differ from A, and the Photon LocalPlayer.UserId must not be A. Then log in B: the top bar shows no balance until B's arrives, and Profile/Collection show B's data. Device: the same sequence on a phone after a cold start with HasRealLogin=1 (the returning-user path), since that path only happens across launches.

**Verifier (confirmed).** I traced all three causes in the code myself.

(1) Guest identity reuse.
- AuthBootstrapper.cs:113-117 resets the guest ids only at Start, and only when HasRealLogin==0.
- LogoutAndRestart (AuthBootstrapper.cs:268-302) never resets them. It calls Logout() at 293 and then ClearRealLoginFlag at 296.
- StartAuthentication -> AuthenticationFlow:323-329 -> LoginAsGuest (PlayFabAuthService.cs:161) -> GetOrCreateSessionGuestId (103-113) returns the stored GUID.
- If that GUID's account received AddUsernamePassword (AuthUIController.cs:250), it logs straight back into A. OnLoginSuccessInternal:199-212 then hides the name, because HasRealLogin is 0.
- Both callers of Esci pass clearRealAccountFlag:true (SettingsV2Integration.cs:159, AuthScreensV2.cs:199).
- ForceGuestIdentity (121-128) keeps the session.
- PublishLook (AuthBootstrapper.cs:165-167) publishes PlayFabAuth.PlayFabId as 'og' for non-real profiles.
- Reports against that og id land on A server-side: segnala writes the sanction on args.id (51.js:463-503).

(2) Device-global PlayerPrefs: Collection.Emoticons (CollectionCosmeticsV2.cs:19,28), progress_* (PlayerProgressLocal.cs:30), Mail.ReadIds (MailService.cs:45, MarkRead prune 132-140; global mail keeps g.id per account, 51.js:207-216), Social.MutedEmoticons (FriendsService.cs:103), SelectedDeckId (CardDecks.cs:9) and Moderazione.AbbandoniPremiati (MatchResultsV2.cs:306). AccountDeletionService.cs:39-46 lists the account-scoped keys, but only deletion clears them.

(3) Statics.
- WalletService.cs:18-41 has no Reset, and its error path keeps the old values.
- ProfileService has no Reset and no owner id.
- BlockList.Current (FriendsService.cs:163-175) keys its cache by PlayFabId plus the loaded flag.
- The local XP fallbacks are at HomeV2Integration.cs:292, PlayerBannerManager.cs:451-452 and MatchResultsV2.cs:313.

Minor inaccuracies, none of which changes the verdict:
- 'IsLoaded never cleared' is wrong. ProfileService.cs:84 sets IsLoaded = !hasError, so a failed reload clears it. It stays true only during the reload window.
- The stale wallet value is not always A's. walletGroup sits inside accountGroup (UIV2TopBar.cs:29-30, SetGuest 44-48), so guests never see a balance. After Esci, the new scene's CloudProfileLoaded (HomeV2Integration.cs:258-263) usually refreshes the wallet with the technical guest's 0/0. B therefore briefly sees the technical session's balance. It sees A's balance only when the guest session is A, or when the guest refresh had not finished.

**Corrected cause.** Same three causes. Two refinements.

(a) The persistent 'device account' (D) is not rotated by ResetGuestDeviceId. GetOrCreateDeviceId (PlayFabAuthService.cs:444-469) falls back to SystemInfo.deviceUniqueIdentifier, which is stable, so D is the same PlayFab account for every returning-user launch on that phone. If anyone ever registers while the session is D (returning launch -> 'Accedi come ospite' -> Registrati from Profilo), D becomes a registered account. From then on, every returning launch's hidden session is that account: OnLoginSuccessInternal:199-205 even sets its DisplayName and IsRegistered. Every 'Accedi come ospite' on such a launch is also that account.

(b) ProfileService.IsLoaded is reset by a failed LoadProfile (line 84). The stale-cache window is the in-flight reload, or the whole session when the reload partially succeeds.

- `Assets/Scripts/Auth/PlayFabAuthService.cs:444-469` GetOrCreateDeviceId re-derives SystemInfo.deviceUniqueIdentifier after ResetGuestDeviceId: the device account never changes
- `Assets/Scripts/Auth/ProfileService.cs:84` IsLoaded = !hasError: a failed reload does clear it
- `Assets/UIV2/Scripts/Components/UIV2TopBar.cs:29-30, 44-48, 72-77` walletGroup lives inside accountGroup: guests never see a balance; the leak is only into the next registered account
- `Server/CloudScript/51.js:463-503` segnala sanctions args.id: reports against a guest-that-is-A suspend A

**Fix concerns.** (a) The 'minimal alternative' (ResetGuestDeviceId everywhere) only works if LoginAsGuest also stops calling GetOrCreateDeviceId. Deleting DEVICE_ID_KEY alone just regenerates the same deviceUniqueIdentifier.

(c) A ProfileService.Reset alone is not enough. A LoadProfile already in flight for the previous session (CheckComplete, ProfileService.cs:79-92, has no owner or generation check) can repopulate the caches and set IsLoaded=true after the Reset. Capture the PlayFabId at LoadProfile start and drop the results if it changed.

(d) Dropping progress.Exp for registered users breaks MatchResultsV2. xpFrom becomes -1 (line 313), and ServerXp only corrects the row when xpFrom >= 0 (lines 335-338). The end-of-match XP row would then never show server XP when the profile was not loaded. Clearing progress_* at logout is the safer option.

WalletService.Reset raising Changed is safe, because HomeV2Integration unsubscribes in OnDestroy (341).

### E3 — root_cause_confirmed · verifier: confirmed · size XS

**Current behaviour.** The equipped emoticons are one device-wide list. Account A's choice shows up for the guest and for account B, and changes made by the guest or B overwrite A's.

**Root cause.** CollectionCosmeticsV2.Equipped reads and writes a single PlayerPrefs key, 'Collection.Emoticons', with no account in it. GameSocialV2 reads the same static at the table. Only account deletion removes the key; logout and login never touch it. The Collection page also renders only in Start() and on taps, so even a per-account key would show the old set after an in-scene login (from the start screen or Profilo) until the next tap.

**Evidence.**
- `Assets/UIV2/Scripts/Core/CollectionCosmeticsV2.cs:19-20` Equipped = PlayerPrefs.GetString("Collection.Emoticons", "0,1,2") - no owner
- `Assets/UIV2/Scripts/Core/CollectionCosmeticsV2.cs:28` Save writes the same global key
- `Assets/UIV2/Scripts/Core/CollectionCosmeticsV2.cs:29-35` Refresh only at Start (and on taps): no re-render on identity change
- `Assets/UIV2/Scripts/Core/GameSocialV2.cs:69, 85` table quick bar and Send use the same global list
- `Assets/Scripts/Auth/AccountDeletionService.cs:45` only deletion clears it

**Fix sketch.** Smallest fix: build the key from the owner. Use "Collection.Emoticons." + PlayFabId when HasRealLogin, otherwise "Collection.Emoticons.ospite" (the same owner rule as ModerationService.Owner). Delete the guest key when a new guest identity starts (ForceGuestIdentity / LoginAsGuest), so every guest starts from the defaults 0,1,2. Have CollectionCosmeticsV2 subscribe to PlayFabAuth.OnDisplayNameChanged, which already fires on login, logout, guest and registration, and call Refresh() there; unsubscribe in OnDestroy. Optionally migrate: on the first read for an account, copy the old global value.

**Files.** Assets/UIV2/Scripts/Core/CollectionCosmeticsV2.cs, Assets/Scripts/Auth/AccountDeletionService.cs, Assets/Tests/Editor/FrontendExpansionTests.cs

**Risk.** FrontendExpansionTests:22-41 write the literal key 'Collection.Emoticons' and must use the new key function. AccountDeletionService should delete the per-account key, using the PlayFabId before logout. The selection becomes device-local per account; it does not follow the account to another phone (see the decision).

**Decision.** Should the equipped emoticons stay on this phone per account (PlayerPrefs, XS), or follow the account across phones in PlayFab user data like AvatarId/FrameId/BannerId (S; reuses ProfileService.SetPlayerData and the LoadPlayerData keys)? Can a guest change emoticons for its session, or is it locked to the default three?

**Test plan.** EditMode: the key function returns different keys for account A, account B and the guest. Equip on A, read as B: the defaults come back. Play Mode: A equips 3 / 4 / 5 -> Esci -> Ospite sees 0 / 1 / 2 -> Accedi B sees its own set -> Esci -> Accedi A sees 3 / 4 / 5. Also check the table quick bar in a training match after each switch.

**Verifier (confirmed).** - CollectionCosmeticsV2.Equipped (line 19) and Save (line 28) use the single key 'Collection.Emoticons', with no owner.
- GameSocialV2.cs:69 (Send) and :85 (ShowQuickBar) read the same static at the table.
- Only AccountDeletionService.cs:45 deletes the key. LogoutAndRestart (AuthBootstrapper.cs:268-302), ForceGuestIdentity and LoginWithEmail never touch it.
- The Collection page renders only in Start (line 34) and in the tap handlers (38-39). The Esci path reloads MainMenu, so after Esci the page re-renders. After an in-scene login (start screen or Profilo, StartScreenV2.cs:45-49, no scene reload) it would show the old set until the next tap, once the key is per account.

- `Assets/Scripts/Auth/PlayFabAuthService.cs:365-372` LoginWithEmail invokes OnDisplayNameChanged (367) BEFORE IsRegistered/HAS_REAL_LOGIN are set (370-372)
- `Assets/Scripts/Auth/PlayFabAuthService.cs:77-81` MarkRegistered raises no event; OnDisplayNameChanged follows only if UpdateDisplayName succeeds (287-293)

**Fix concerns.** The proposed refresh trigger does not work as written.

1. Wrong owner on login. PlayFabAuth.OnDisplayNameChanged fires inside LoginWithEmail before HAS_REAL_LOGIN is set (PlayFabAuthService.cs:367 vs 370-372). A Refresh() hooked to that event computes the owner with the ModerationService.Owner rule as 'ospite' and shows the guest set for B. Do the S4 reorder first, or hook AuthUIController.OnLoginSuccess / OnRegistrationSuccess (HomeV2Integration already uses those, lines 90-91).

2. Registration loses the guest's choice. Registration keeps the same PlayFab account, but the owner switches from 'ospite' to the PlayFabId, so the selection made as a guest is lost. MarkRegistered should copy the guest key into the account key.

3. Mismatch when the name update fails. If UpdateDisplayName fails, no event fires at all. The Collection page would keep showing the guest set, while GameSocialV2 (which reads Equipped live) uses the account key.

4. The optional 'migrate the old global value on first read' would hand whichever account reads first the selection of whoever used the phone last.

FrontendExpansionTests.cs:22-41 hard-code the key, as the diagnosis notes.

### S2 — likely_cause · verifier: partially_confirmed · size S

**Current behaviour.** After registration the account UI flips at once (HasRealLogin is set by MarkRegistered and HomeV2Integration re-renders). The name, however, stays 'Ospite XXXX' until the asynchronous UpdateUserTitleDisplayName succeeds. If that call fails, the name stays 'Ospite XXXX' for the whole session. Other players keep seeing 'Ospite XXXX' even when it succeeds.

**Root cause.** (1) LIKELY: the name comes only from the success callback of the second call, UpdateDisplayName(username). The call has no onError, so a failure is silent: NameNotAvailable or ProfaneDisplayName (verified with Context7: UpdateUserTitleDisplayName can return E_PF_NAME_NOT_AVAILABLE / E_PF_PROFANE_DISPLAY_NAME; the name must be 3-25 characters). DisplayName then stays null, so GetBestDisplayName returns 'Ospite ' + id for the session. After closing and reopening the app, logging in with email sets DisplayName = profile.DisplayName ?? Username. That is exactly 'until the app is reopened'. A failure also leaves the account without a title display name, so friends cannot add it by name (AddFriend uses FriendTitleDisplayName) and the Friends page shows an empty own name. (2) CONFIRMED: PhotonNetwork.NickName is set only inside ConnectToPhoton, and registration does not reconnect Photon. PublishLook, which runs on OnDisplayNameChanged, only sets custom properties. Opponents, room seats and disconnect notices therefore show 'Ospite XXXX' until the next real reconnect. Checked in the installed Photon source: NickName can be set at any time and syncs into the room (Player.cs:78-98).

**Evidence.**
- `Assets/Scripts/Auth/AuthUIController.cs:254-261` MarkRegistered(email) then UpdateDisplayName(username) with no onError: failure is silent
- `Assets/Scripts/Auth/PlayFabAuthService.cs:77-81` MarkRegistered sets flags+Email but not DisplayName
- `Assets/Scripts/Auth/PlayFabAuthService.cs:130-137` null DisplayName -> 'Ospite XXXX'
- `Assets/Scripts/Auth/PlayFabAuthService.cs:287-299` DisplayName and OnDisplayNameChanged only on success
- `Assets/Scripts/Auth/PlayFabAuthService.cs:365` next email login falls back to Username: explains 'fixed after reopen'
- `Assets/UIV2/Scripts/Core/HomeV2Integration.cs:90-102, 255, 265-279` Home does re-render on registration and on OnDisplayNameChanged (correct when the update succeeds)
- `Assets/Scripts/Auth/PhotonAuthConnector.cs:136-140` only place NickName is set, and only when not already connected
- `Assets/Scripts/Auth/AuthBootstrapper.cs:97-99, 159-168` OnDisplayNameChanged -> PublishLook sets custom props only, never NickName
- `Assets/UIV2/Scripts/Core/GameSocialV2.cs:137` opponents' names come from Player.NickName
- `Assets/Scripts/UI/UI51FriendsView.cs:75-81` own friend name = PlayFabAuth.DisplayName: empty after a failed update
- `Assets/Scripts/Auth/FriendsService.cs:69` AddFriend by FriendTitleDisplayName: impossible without a title display name

**Fix sketch.** (a) Add a username parameter to MarkRegistered(email, username) and set DisplayName = username, then raise OnDisplayNameChanged. This is optimistic and matches what the next login shows (the Username fallback). Then call UpdateDisplayName with an onError that writes a status line (e.g. 'Nome gia' in uso') and logs the error. (b) In AuthBootstrapper.Awake, make the OnDisplayNameChanged handler also set PhotonNetwork.NickName = PlayFabAuth.GetBestDisplayName() when PlayFabAuth.IsLoggedIn. One line, and it covers registration, login and guest.

**Files.** Assets/Scripts/Auth/PlayFabAuthService.cs, Assets/Scripts/Auth/AuthUIController.cs, Assets/Scripts/Auth/AuthBootstrapper.cs, Assets/Tests/Editor/AccountDeletionTests.cs

**Risk.** MarkRegistered has one other caller, the EditMode test AccountDeletionTests:107. Setting NickName while in a room sends a player-property update, which is harmless since names change only outside matches. On Logout(), OnDisplayNameChanged(null) fires; the IsLoggedIn guard avoids GetBestDisplayName creating the device-id pref.

**Decision.** When the chosen name is already in use as a display name (registration itself has already succeeded), what should happen? Options: show 'nome gia' in uso' and let the user change it, as in the open SceltaNome question in SPRINT_BACKLOG line 426; add an automatic suffix; or keep the username locally with no friend-findable name.

**Test plan.** EditMode: MarkRegistered(email, 'Mario') -> GetBestDisplayName()=='Mario' and OnDisplayNameChanged fired once. Play Mode, Editor as guest plus a second client (another Editor or a device) in a private room: register on one side and check that the other side's seat or name updates. Device: register with a username that equals an existing display name to force NameNotAvailable. Before the fix, the console shows '[PlayFabAuth] Failed to update display name' and the name stays Ospite.

**Verifier (partially_confirmed).** (2) Photon NickName: CONFIRMED.
- PhotonNetwork.NickName is written only in PhotonAuthConnector.ConnectToPhoton:136-140, after the 'already connected' early return at 116-121.
- The only other write is the empty-string reset in LogoutAndRestart (AuthBootstrapper.cs:287).
- Registration never reconnects: AuthUIController.cs:250-269 has no RebindPhoton.
- The OnDisplayNameChanged handler (AuthBootstrapper.cs:99) only runs PublishLook (custom properties).
- Opponents read NickName in GameSocialV2.cs:137, RoomFlowV2.cs:365-375 and NetworkGameController.cs:274/293.

(1) Silent UpdateDisplayName failure: PLAUSIBLE, not proven.
- The call at AuthUIController.cs:260 passes no onError.
- MarkRegistered (PlayFabAuthService.cs:77-81) does not set DisplayName, so GetBestDisplayName returns 'Ospite XXXX' (130-136).
- Context7 confirms title display names are unique by default and the call can return NAME_NOT_AVAILABLE / PROFANE_DISPLAY_NAME. The PlayFab docs also say looking players up by title display name always fails when non-unique names are enabled, so this project, which adds friends by name (FriendsService.cs:69), must keep them unique, and collisions are possible.
- The next email login falls back to Username (PlayFabAuthService.cs:365), which matches 'fixed after reopen'.
- In the success case the Home does refresh on its own: OnDisplayNameChanged -> HomeV2Integration.DisplayNameChanged (102, 255), plus ReloadAccountProfile (91, 265-279). So the self-view symptom needs the failure path.
- No console evidence was available (read_console returned nothing for PlayFabAuth).

**Corrected cause.** Two separate defects. Other players: confirmed stale Photon NickName after registration. Own view: only reproducible if UpdateUserTitleDisplayName fails silently (NameNotAvailable or profanity). This is unverified, and no other code path keeps the own name as 'Ospite' after a successful update.

- `Assets/Scripts/Auth/PhotonAuthConnector.cs:116-121` already connected -> returns before the NickName write at 137-140
- `Assets/Scripts/Auth/FriendsService.cs:59` friend lists fall back to Username when TitleDisplayName is null; AddFriendByName (69) cannot find the player

**Fix concerns.** - Optimistic DisplayName = username: when the server update then fails, it hides the failure from the user, while friends, the ranking and AddFriendByName still see no title display name. The onError status line is therefore mandatory, not optional.
- NickName in the OnDisplayNameChanged handler: LoginWithEmail raises the event while Photon is still connected as the previous account (RebindPhoton runs afterwards, AuthUIController.cs:311). This is harmless out of a room, because RebindPhoton reconnects with GetBestDisplayName anyway (AuthBootstrapper.cs:260).
- Changing the MarkRegistered signature breaks AccountDeletionTests.cs:107.

### S4 — root_cause_confirmed · verifier: partially_confirmed · size S

**Current behaviour.** Lifecycle as built. Launch -> AuthBootstrapper.Start -> PlayFab CustomID login (hidden technical session) -> Photon token -> Photon connect -> LoadProfile (does not block) -> Ready/OnAuthReady. The start screen always asks for Login. Accedi: LoginWithEmail -> RebindPhoton -> ReloadAccountProfile. Esci: LogoutAndRestart + MainMenu reload. Defects in the order of the steps: (1) Some state is not cleared on logout or login (see S3). (2) Presence and Photon properties are published in the middle of flag changes. (3) Data loaded for the technical session is not reloaded after a login from the start screen or Profilo. (4) Register works only once the session exists.

**Root cause.** Ordering and lifecycle problems, each confirmed. (1) Returning users never auto-log into their account. With HasRealLogin=1 the hidden session is the persistent device-id account (PlayFabAuthService.cs:161), and nothing links it to the email account (no LinkCustomID calls). Before login, HasRealLogin-gated code runs against that device account: Friends Load -> FriendsChat connects to Photon Chat as the device account; Mail and Rewards fetch its data. (2) Presence published mid-transition. LoginWithEmail raises OnDisplayNameChanged before it sets HasRealLogin, so PublishLook publishes B's PlayFabId as a guest 'og' id. LogoutAndRestart calls Logout() before ClearRealLoginFlag, so PublishLook publishes A's frame, banner and stats with id=null. Both heal once the next PublishLook runs, but they are visible to others in that window. (3) After a login without a scene reload: Mail (UI51MailView.Start->Load) and the Rewards badge (DailyChanged) were computed for the technical session. RebindPhoton's RewardsService.Reset clears Daily without raising DailyChanged, so badges stay those of the technical session. Friends presence and invites (FriendsChat) start only after UI51FriendsView.Fill, so account B is offline to friends and gets no invites until it opens Amici. (4) OnRegisterClicked has no auth-state guard, unlike OnLoginClicked. If auth is still logging in or is in Error, AddUsernamePassword throws PlayFabException NotLoggedIn (PlayFabClientAPI.cs:116) after SetLoading(true), so the 'Registrazione in corso' overlay and _isProcessing stay stuck. (5) Logout never clears the SDK's static credentials, so in the logout window or after a failed guest login, PlayFabClientAPI.IsClientLoggedIn() is still true for the old account (WalletService/RewardsService/FillAccount check that).

**Evidence.**
- `Assets/Scripts/Auth/AuthBootstrapper.cs:308-460` auth flow; LoadProfile at 453 then Ready/OnAuthReady
- `Assets/UIV2/Scripts/Core/StartScreenV2.cs:27-35` start screen always shows Login; no real auto-login
- `Assets/Scripts/Auth/PlayFabAuthService.cs:356-380` OnDisplayNameChanged (367) before IsRegistered/HAS_REAL_LOGIN (370-372)
- `Assets/Scripts/Auth/AuthBootstrapper.cs:291-297` Logout() fires OnDisplayNameChanged->PublishLook while HasRealLogin is still 1
- `Assets/Scripts/Auth/AuthBootstrapper.cs:196-209` RebindPhoton resets Rewards/Moderation/FriendsChat only; no view reload
- `Assets/Scripts/Auth/RewardsService.cs:130-135` Reset clears Daily without DailyChanged
- `Assets/Scripts/UI/UI51MailView.cs:89, 105-116` mail + inizio fetched once at Start for whatever session is ready
- `Assets/Scripts/UI/UI51RewardsView.cs:84-89` badge only from DailyChanged/Start
- `Assets/Scripts/UI/UI51FriendsView.cs:97, 116-128, 156-163` friends + FriendsChat only at Start/Open; guest returns early; no reload on login
- `Assets/Scripts/Auth/FriendsChat.cs:142-145` Chat connects as whatever PlayFabId has HasRealLogin, including the device account before login
- `Assets/Scripts/Auth/AuthUIController.cs:232-249 vs 283-289` register lacks the auth-state guard that login has
- `Assets/PlayFabSDK/Client/PlayFabClientAPI.cs:116` AddUsernamePassword throws NotLoggedIn instead of calling the error callback
- `Assets/Scripts/Auth/PlayFabAuthService.cs:306-319` no PlayFabClientAPI.ForgetAllCredentials (exists at PlayFabClientAPI.cs:31)

**Fix sketch.** Ship with S3 block A. (1) Covered by S3(a), the ephemeral technical session. (2) Reorder: in the LoginWithEmail success handler set IsRegistered/HAS_REAL_LOGIN before OnDisplayNameChanged; in LogoutAndRestart call ClearRealLoginFlag/ClearRegisteredFlag before PlayFabAuth.Logout(). (3) UI51MailView and UI51FriendsView subscribe to PlayFabAuth.OnLoginSuccess (and unsubscribe) and call Load(); OnLoginSuccess already fires before onSuccess -> RebindPhoton. Make RewardsService.Reset raise DailyChanged so the Rewards badge clears. (4) Copy OnLoginClicked's guard into OnRegisterClicked, plus `!PlayFabClientAPI.IsClientLoggedIn()` -> status text and return. (5) Call ForgetAllCredentials in Logout() (S3 b).

**Files.** Assets/Scripts/Auth/PlayFabAuthService.cs, Assets/Scripts/Auth/AuthBootstrapper.cs, Assets/Scripts/Auth/AuthUIController.cs, Assets/Scripts/Auth/RewardsService.cs, Assets/Scripts/UI/UI51MailView.cs, Assets/Scripts/UI/UI51FriendsView.cs

**Risk.** MailService.Fetch after login can overlap with RewardsService.Start('inizio') of the new account; Start already queues callers. FriendsChat.Ensure right after RebindPhoton's Destroy in the same frame returns the doomed instance (Destroy is deferred), so Load on login must run after RebindPhoton. Today it does: OnLoginSuccess fires first, but FriendsService.GetFriends is async, so Fill comes later. HomeConnectionWatcher also listens to OnLoginSuccess (TryRejoin); ordering is unchanged.

**Decision.** Should friend presence and invites start at login for every account, as the FriendsChat comment intends ('cosi' gli inviti arrivano anche senza aprire Amici'), or only when Amici is opened?

**Test plan.** EditMode: a register click with the bootstrapper not ready shows the status line and leaves AppLoading not busy (fake AuthBootstrapper state). Play Mode: start screen -> Accedi with an account that has unread mail. The mail badge must appear without opening Posta, and a second client sees the account online in Amici without opening Amici there. Log in, log out and log in again while watching the LocalPlayer 'og'/'id' custom properties in the console: no frame with B as 'og'. Device (only there): airplane mode at launch -> Registrati -> no stuck overlay.

**Verifier (partially_confirmed).** (1) No auto-login: CONFIRMED.
- StartScreenV2.cs:33-34 always shows Login, unless returning from the table.
- With HasRealLogin=1 the hidden session uses GetOrCreateDeviceId (PlayFabAuthService.cs:161).
- No LinkCustomID exists anywhere (grep).
- UI51FriendsView.Load (119-134) passes its HasRealLogin gate for that device account. Fill -> FriendsChat.Ensure/Watch -> Connect (FriendsChat.cs:91-97, 129-146) connects to Chat as that account.

(2) Mid-transition publishes: the mechanism is real (PlayFabAuthService.cs:367 before 370-372; AuthBootstrapper.cs:293 before 296). The claim that others see it does not hold in practice:
- LogoutAndRestart disconnects Photon first (279-280).
- Login is followed by RebindPhoton's disconnect.
- Out of a room, SetCustomProperties stays local. The next join sends whatever the latest PublishLook left (LoadBalancingClient.cs:2935-2943).
- PublishLook always runs again before a join is possible: HomeV2Integration.cs:275 runs it even when the load fails, and AuthBootstrapper.cs:453 for the guest. MatchmakingManager.OnJoinedRoom:422 republishes on top.

(3) Badges and presence after an in-scene login: CONFIRMED.
- UI51MailView loads only at Start/Open (89-96, 105-116).
- The UI51RewardsView badge updates only on DailyChanged or Start (84-89, 98-105).
- RewardsService.Reset (130-135) raises no event.
- UI51FriendsView loads only at Start/Open (99, 101-110).
- RebindPhoton destroys FriendsChat (AuthBootstrapper.cs:198), and nothing recreates it until Amici is opened.

(4) Register without a session: CONFIRMED, and worse than described.
- OnRegisterClicked (232-250) has no guard. PlayFabClientAPI.cs:116 throws NotLoggedIn after SetLoading(true).
- AppLoadingView.Show hides Retry/Cancel (AppLoadingView.cs:74) and blocks raycasts with no timeout (85-89), so the whole app is frozen.

(5) Stale SDK credentials after logout: CONFIRMED. There is no ForgetAllCredentials call (grep), so PlayFabClientAPI.IsClientLoggedIn stays true for the old ticket (RewardsService.cs:148, WalletService.cs:25, AuthScreensV2.cs:162).

**Corrected cause.** Points (1), (3), (4) and (5) are correct as stated. Point (2) is real local state, but it is not observable by other players: every login and logout path re-runs PublishLook before any room join, and OnJoinedRoom republishes. It is only a hygiene issue, not a visible presence bug.

- `Assets/Photon/PhotonRealtime/Code/LoadBalancingClient.cs:2935-2943` join sends LocalPlayer.CustomProperties as they are at join time
- `Assets/Scripts/Networking/MatchmakingManager.cs:417-422` OnJoinedRoom republishes the look
- `Assets/UIV2/Scripts/Core/AppLoadingView.cs:74, 85-89` busy overlay hides Retry/Cancel and never times out
- `Assets/Scripts/Auth/RewardsService.cs:76-96, 130-135` Start queues callers in 'waiting'; Reset clears 'waiting'

**Fix concerns.** Subscribing UI51MailView to PlayFabAuth.OnLoginSuccess has a concrete ordering bug.
- The event fires at PlayFabAuthService.cs:378, before AuthUIController's onSuccess calls RebindPhoton (AuthUIController.cs:311).
- Mail Load -> RewardsService.Start('inizio') would queue the MailService.Fetch callback in 'waiting'. RebindPhoton -> RewardsService.Reset then clears 'waiting' and the started/starting flags (RewardsService.cs:130-135) while 'inizio' is still in flight.
- The mail fetch is silently dropped, and the late response sets started=true for the new account without fetching.
- Fix: hook AuthUIController.OnLoginSuccess / OnRegistrationSuccess (after RebindPhoton), as HomeV2Integration already does (lines 90-91).

PlayFabAuth.OnLoginSuccess also fires for every guest or technical login (PlayFabAuthService.cs:218), including the one right after Esci.

The register guard should check PlayFabAuth.IsLoggedIn rather than the SDK's IsClientLoggedIn, which stays true after Logout until (5) is fixed.

### S1 — feature_missing · verifier: confirmed · size M

**Current behaviour.** The same account can be logged in and play on two devices at once. Each device keeps its own PlayFab session and its own Photon connection. Photon blocks only joining the same room twice: in PUN 2 CheckUserOnJoin is always on, so a second join fails with JoinFailedFoundActiveJoiner 32746. Two devices can still play different rooms simultaneously and share per-account server caps, the rejoin pass and match records.

**Root cause.** No single-session mechanism exists anywhere. LoginWithEmail only replaces the local ticket. CloudScript has no session handler, and inizio/moderazione keep no session id. Nothing in the installed Photon client code (Realtime DisconnectCause, Chat ChatDisconnectCause) disconnects an older connection with the same UserId. The PlayFab docs checked via Context7 describe no built-in 'one active session' feature, so it must be done in the app (a HYPOTHESIS that older tickets stay valid; not tested live). Device-local markers (Moderazione.PartitaInCorso) also assume one device per account.

**Evidence.**
- `Assets/Scripts/Auth/PlayFabAuthService.cs:356-380` login stores ticket locally; nothing invalidates other devices
- `Assets/Photon/PhotonRealtime/Code/LoadBalancingPeer.cs:236-238` CheckUserOnJoin forced on: only same-room duplicates are refused
- `Assets/Scripts/Networking/HomeConnectionWatcher.cs:117-120` JoinFailedFoundActiveJoiner handled only as 'old connection still active' for rejoin
- `Server/CloudScript/51.js:337-343, 526-537` inizio/moderazione: no session bookkeeping
- `Assets/Scripts/Auth/ModerationService.cs:64-92, 117-128` existing per-Home and pre-online-match server calls (Refresh / CheckNow) a session check can ride on
- `Assets/Scripts/UI/UI51SuspensionView.cs:47-50` WhenOnlineAllowed -> CheckNow before every online match

**Fix sketch.** Last login wins, adding no extra API calls. The client creates a SessionId GUID on each real login (the LoginWithEmail success handler and MarkRegistered). For registered users, ModerationService sends {sessione, nuova} in the existing 'moderazione' call: at Home arrival, before every online match, and on resume (add CheckNow in an existing OnApplicationPause(false)). CloudScript moderazione (already phoneOnly) writes internal data 'Sessione' when nuova=true, and otherwise returns altrove:true if the stored value differs. On altrove the client shows a UI51Toast 'Account in uso su un altro dispositivo' and runs the existing Esci path (LogoutAndRestart(true) + MainMenu reload). Never kick in the middle of a match: the check runs only at the existing checkpoints. Guests are exempt (ephemeral accounts). The user must re-upload 51.carica.js.

**Files.** Server/CloudScript/51.js, Server/CloudScript/test.js, Assets/Scripts/Auth/PlayFabAuthService.cs, Assets/Scripts/Auth/ModerationService.cs, Assets/Scripts/Auth/RewardsService.cs, Assets/UIV2/Scripts/Core/StartScreenV2.cs

**Risk.** Adds new behaviour on top of the moderation call; ModerationService.Reset must also clear the session flag. Old app versions do not send 'sessione' and must not be kicked: only compare when the arg is present. A race between two logins is last-writer-wins (accepted limit, same as other client-gated rules). A modded client can ignore 'altrove' (same accepted limit as the suspension gate).

**Decision.** Should the last login win (the older device gets logged out), or should a second login be refused while the first is active (that needs a heartbeat/expiry)? When is the old device kicked: at its next Home, online match or resume (my default), or immediately even mid-match? Are guests exempt?

**Test plan.** Node: Server/CloudScript/test.js cases nuova=true writes the value, a different sessione returns altrove, a missing sessione is a no-op. Play Mode: two Editor instances, or Editor plus a device, on the same test account: log in on the second, return to Home or press Gioca online on the first -> kicked to the login screen with the toast. Device-only: background/resume on the first phone after logging in on the second (needs real app pause).

**Verifier (confirmed).** There is no single-session mechanism anywhere:
- no session or 'sessione' bookkeeping in Server/CloudScript/51.js (grep);
- inizio (51.js:336-342) and moderazione (51.js:526-537) keep no device state;
- LoginWithEmail only replaces the local ticket (PlayFabAuthService.cs:356-380);
- no ForgetAllCredentials or LinkCustomID calls exist.

PUN forces CheckUserOnJoin (LoadBalancingPeer.cs:236-238), so only a duplicate join of the same room is refused (HomeConnectionWatcher.cs:117-120 treats JoinFailedFoundActiveJoiner as an old connection).

PlayFab allowing concurrent tickets is consistent with the docs I could reach (instanced multi-login sample) but was not live-tested. The diagnosis correctly labels it a hypothesis. I could not verify whether Photon Chat's server kicks a duplicate UserId; the enum-based inference (ChatDisconnectCause has DisconnectByServerLogic) is weak but not decisive for the verdict.

- `Assets/Scripts/Networking/NetworkGameController.cs:238` only in-match OnApplicationPause hook
- `Assets/UIV2/Scripts/Core/MatchResultsV2.cs:485` in-match OnApplicationPause hook
- `Assets/Scripts/Auth/FriendsChat.cs:84-88` OnApplicationPause exists only once FriendsChat was created (after Amici Fill)

**Fix concerns.** - No existing OnApplicationPause(false) suits a resume check. The existing ones are in-match (NetworkGameController.cs:238, MatchResultsV2.cs:485), which contradicts 'never kick mid-match', or in FriendsChat, which only exists after Amici is opened. A Home-side hook is needed, e.g. in HomeConnectionWatcher.
- The returning-launch path where the hidden device account is itself registered (OnLoginSuccessInternal:199-205) creates HasRealLogin=1 sessions without any login, so they get no SessionId. Exclude them, or ship S3(a) first.
- ModerationService.Refresh's registered branch calls 'moderazione' with null args (ModerationService.cs:98). It must start sending args without setting 'stato', which would stop the report outcomes being consumed.

### Missed by the investigator (found by the verifier)

- **S3**: The 'guest that is really A' (S3 cause 1) also exposes A's server-side data to whoever taps 'Accedi come ospite' after A's Esci on a shared phone.
- Posta is not gated on HasRealLogin: UI51MailView.Load reads the current session's 'Posta'.
- Daily rewards are not gated either.
- The CloudScript claim handlers do not check for a Username.
The 'guest' therefore reads A's mail and can claim A's mail gifts and daily reward.
  - `Assets/Scripts/UI/UI51MailView.cs:105-116` no HasRealLogin gate before RewardsService.Start/MailService.Fetch
  - `Assets/Scripts/UI/UI51RewardsView.cs:190-196` Claim has no guest gate
  - `Server/CloudScript/51.js:349-366` riscattaPremio / riscattaPosta grant to currentPlayerId with no Username check (unlike premioPartita at 406 and segnala at 469)
- **S3**: For a 'guest' that is really A, ModerationService.Refresh takes the guest branch.
- It calls 'moderazione' without args, so the server deletes A's EsitoSegnalazione, and the guest code discards the outcomes (Done(onDone, null)). A permanently loses its report-outcome notices.
- GuestRefresh also mirrors A's server sanctions into the device-level DeviceModeration, so A's suspension or mute persists on the phone for later real guests.
  - `Assets/Scripts/Auth/ModerationService.cs:75-84, 131-137` guest branch: moderazione with null args, outcomes dropped, sanctions mirrored to device
  - `Server/CloudScript/51.js:526-537` without args.stato the handler removes EsitoSegnalazione
- **S3**: The BlockList pin is not only a display problem.
- Once Current() caches the previous session's list under the new PlayFabId while IsLoaded is still true, the owner key never changes again in that session, so B's real list is never read.
- The next SetBlocked writes the pinned set into B's private 'Bloccati'. This overwrites B's server-side block list (data loss), and can copy another account's blocked ids into B.
  - `Assets/Scripts/Auth/FriendsService.cs:163-175` owner = PlayFabId + loaded flag; re-read only when that string changes
  - `Assets/Scripts/Auth/FriendsService.cs:151-158` SetBlocked persists the cached set to PlayFab player data
- **S3**: The persistent device account is permanent per phone.
- ResetGuestDeviceId deletes DEVICE_ID_KEY, but GetOrCreateDeviceId re-derives the stable SystemInfo.deviceUniqueIdentifier.
- If any user ever registers while the session is that account (returning launch -> 'Accedi come ospite' -> Registrati from Profilo), every later returning-user launch's hidden session is that registered account. OnLoginSuccessInternal then sets its DisplayName and IsRegistered, and every 'Accedi come ospite' on such a launch is that account.
- AccountDeletionService deleting Project51_DeviceId does not detach it either.
  - `Assets/Scripts/Auth/PlayFabAuthService.cs:95-101, 444-469` reset deletes the pref, but the id is regenerated from deviceUniqueIdentifier
  - `Assets/Scripts/Auth/PlayFabAuthService.cs:199-205` returning-registered branch adopts that account's name/flags

### Cross-cutting notes

- **session-state** (wallet/top bar items, E-economy): New in 2.65 (uncommitted): the top-bar wallet reads the static WalletService, which is never reset on logout or account change. After an in-scene login the previous account's coins and gems show until GetUserInventory returns, and they stay if it fails (the error path keeps the old values). This affects any economy or wallet item outside this cluster.
  - `Assets/Scripts/Auth/WalletService.cs:18-41` static state, no Reset
  - `Assets/UIV2/Scripts/Core/HomeV2Integration.cs:106, 262, 277, 329-335` 2.65 wiring; RefreshWallet runs before Refresh completes
- **photon-callbacks-order** (multiplayer name/seat display items, private room lobby): PhotonNetwork.NickName is set only at connect time, so any name change in a live session (registration, a future rename) is not seen by opponents, room seat lists or 'X ha lasciato la partita' notices until a reconnect.
  - `Assets/Scripts/Auth/PhotonAuthConnector.cs:136-140` sole NickName write
  - `Assets/UIV2/Scripts/Core/RoomFlowV2.cs:365-375` room seats use NickName
  - `Assets/Scripts/Networking/NetworkGameController.cs:274, 293` notices use NickName
- **presence-timing** (friends online status, friend invites to private rooms): Friends presence and invites (Photon Chat) start only when UI51FriendsView.Fill runs. After a login from the start screen, which is the common path, the account is offline to friends and misses invites until it opens Amici. Before login on a returning-user launch, FriendsChat can connect as the hidden device account.
  - `Assets/Scripts/UI/UI51FriendsView.cs:97, 116-128, 156-163` Load only at Start/Open
  - `Assets/Scripts/Auth/FriendsChat.cs:142-145` connects as current PlayFabId gated only by the HasRealLogin flag
  - `Assets/Scripts/Auth/AuthBootstrapper.cs:198` RebindPhoton destroys it; nothing recreates it
- **async-load-vs-ui-refresh** (Posta badge, Premi badge): The Mail unread badge and the Rewards badge are computed once for whatever session was ready at MainMenu Start (the technical guest or device account) and are not recomputed after Accedi. RewardsService.Reset does not raise DailyChanged.
  - `Assets/Scripts/UI/UI51MailView.cs:89, 115, 142` badge from first Fill only
  - `Assets/Scripts/UI/UI51RewardsView.cs:84-102` badge only on DailyChanged/Start
  - `Assets/Scripts/Auth/RewardsService.cs:130-135` Reset is silent
- **other** (moderation/report items, forfeit/match record items): Moderation: a 'guest' can be a registered account (S3 root cause 1), so the guest id 'og' published to other players can be account A's PlayFabId. Reports against 'a guest' and server-side mirroring of guest sanctions can then land on a registered account, and Photon webhooks seat A in match records while the client treats it as a guest.
  - `Assets/Scripts/Auth/AuthBootstrapper.cs:165-167, 186-188` og = PlayFabAuth.PlayFabId for guests
  - `Assets/Scripts/UI/PlayerBannerManager.cs:212-216` opponents report the og id
- **ui-busy-state** (registration/login UX items, mobile-keyboard/auth screen items): Pressing Registrati before the hidden PlayFab session exists (slow network, or AuthState.Error) throws PlayFabException NotLoggedIn after SetLoading(true): the authentication-busy overlay and _isProcessing stay stuck. The login button has a guard; registration does not.
  - `Assets/Scripts/Auth/AuthUIController.cs:232-250` no guard, SetLoading(true) before the throwing call
  - `Assets/PlayFabSDK/Client/PlayFabClientAPI.cs:116` throws instead of calling errorCallback
- **static-singleton-cache** (table banner/quick profile stats items, end-of-match XP row): ProfileService (IsLoaded, stats and data caches) has no reset and no owner id. Besides the Home, which guards itself with profileOwnerId, the table banners (PlayerBannerManager), MatchResultsV2 xpFrom, PublishLook (other players' quick profile) and BlockList trust IsLoaded alone and can briefly use the previous account's cache.
  - `Assets/Scripts/Auth/ProfileService.cs:49` IsLoaded never cleared
  - `Assets/Scripts/UI/PlayerBannerManager.cs:198-204, 446-452` HasRealProfile/IsLoaded only
  - `Assets/UIV2/Scripts/Core/MatchResultsV2.cs:309-311` cloudLoaded = IsLoaded only

## Presenza amici / LastSeen / inviti / lobby / 2v2 / aggiungi amico

### P1 — root_cause_confirmed · verifier: confirmed · size S

**Current behaviour.** Friends' online/offline status is unreliable and slow. An account becomes visible as Online to its friends only after its own phone has opened the Amici page, or reloaded MainMenu, since the last login, background or network drop. After any drop it stays Offline, and its own list shows everyone Offline, until Amici is reopened. 'Visto ...' is the PlayFab LastLogin time, not the last time the player was seen.

**Root cause.** The Photon Chat connection that carries presence has no lifecycle of its own. (1) FriendsChat is created and connected only from UI51FriendsView.Fill (FriendsChat.Ensure plus Watch). Fill runs only after GetFriends, from Start (MainMenu load) or Open. (2) 'Accedi' calls AuthBootstrapper.RebindPhoton, which destroys FriendsChat. The scene is not reloaded and nothing calls Load again, so the real account never connects to Chat until the user opens Amici. UI51FriendsView only listens to OnAuthReady, which does not fire again after Accedi. (3) OnDisconnected never retries. OnApplicationPause(false) and OnSceneChanged reconnect only when the local friend set is not empty. On resume, Connect() returns early while CanChat is still true, because Photon only notices the timeout at the next Service() in Update (likely; this depends on Photon internals). (4) LastSeen is the profile LastLogin, which updates only at login.

**Evidence.**
- `Assets/Scripts/Auth/FriendsChat.cs:57-65` Ensure() is the only factory. Its only caller is UI51FriendsView.Fill (grep: no other FriendsChat.Ensure call).
- `Assets/Scripts/UI/UI51FriendsView.cs:99, 101-110, 119-134, 150-165` Start/Open -> Load -> GetFriends -> Fill -> Ensure + Watch -> Connect. It waits for OnAuthReady only if auth is not ready yet.
- `Assets/Scripts/Auth/AuthBootstrapper.cs:196-209` RebindPhoton (called on Accedi, AuthUIController.cs:311) destroys FriendsChat and nothing recreates it.
- `Assets/Scripts/Auth/FriendsChat.cs:84-88` On resume it reconnects only if friends.Count > 0, and Connect() exits when IsConnected is still true (line 131).
- `Assets/Scripts/Auth/FriendsChat.cs:177-183` OnDisconnected clears presence and fires OnPresenceChanged. It never reconnects.
- `Assets/Scripts/Auth/FriendsChat.cs:154-159` A scene change reconnects only if friends.Count > 0.
- `Assets/Scripts/Auth/FriendsChat.cs:81` Service() runs only in Update. UseBackgroundWorkerForSending is not set, so no ACKs are sent in background.
- `Assets/Photon/PhotonChat/Code/ChatClient.cs:777-781` Bundled SDK docs: status goes to anyone who added your id with AddFriends, and the friends list is flushed on disconnect. Presence therefore needs a live connection on both sides.
- `Assets/Scripts/UI/UI51FriendsView.cs:233, 245-251` An offline row shows LastSeen(LastLogin). Under 1 h it reads 'Visto poco fa'.
- `Assets/Scripts/Auth/FriendsService.cs:41, 60` LastLogin comes from the PlayFab profile (ShowLastLogin). Context7 (PlayFab docs) confirms it is the last login time and needs Client Profile Options.

**Fix sketch.** One presence block, no new system. (a) FriendsChat.OnDisconnected: when client.DisconnectedCause != DisconnectByClientLogic, Invoke(nameof(Connect), delay) with a growing delay (5 s doubling to a 30 s cap, reset in OnConnected). Log the cause in debug builds. (b) OnApplicationPause: on resume call Connect() with no friends.Count guard. Optionally call client.Disconnect() on pause(true) so going to background shows Offline at once and resume always reconnects cleanly; this also removes the CanChat race (user decision). Drop the friends.Count guard in OnSceneChanged too. (c) Set client.EnableProtocolFallback = true, as PUN already does. (d) UI51FriendsView: subscribe to AuthBootstrapper.Instance.PlayFabAuth.OnLoginSuccess -> Load() and unsubscribe in OnDestroy. Make PlayFabAuthService.MarkRegistered raise OnLoginSuccess too, so Accedi and registration connect presence without opening Amici. (e) Change the LastSeen copy to 'Ultimo accesso ...', or build a real last-seen (open question).

**Files.** Assets/Scripts/Auth/FriendsChat.cs, Assets/Scripts/UI/UI51FriendsView.cs, Assets/Scripts/Auth/PlayFabAuthService.cs

**Risk.** Retries cost one PlayFab GetPhotonAuthenticationToken call per attempt, so keep the backoff. Do not retry on InvalidAuthentication or CustomAuthenticationFailed. OnLoginSuccess also fires for the bootstrap guest login (PlayFabAuthService.cs:218); Load stays gated by the real-login check. The only other OnLoginSuccess subscriber is HomeConnectionWatcher.OnAccountChanged -> TryRejoin, which does nothing without a marker. Disconnect-on-pause makes quick app switches show Offline.

**Test plan.** Device only: Photon Chat needs a real account and two phones, and the Editor runs as guest. Phone A and phone B with mutual friends. After the fix, check each step: Accedi on B without opening Amici, A sees Online within a few seconds; B goes to background for 30 s and returns, Online again without opening Amici; B toggles airplane mode for 20 s, back Online; B plays a match, 'In partita', then Online on return. Collect the [FriendsChat] disconnect-cause log with logcat or the Xcode console. Existing EditMode tests (FriendsChatTests) must stay green.

**Verifier (confirmed).** I traced every step myself. FriendsChat.Ensure has one caller, UI51FriendsView.Fill (UI51FriendsView.cs:156; grep found no other). Fill runs only after GetFriends, which Load calls from Start or Open (UI51FriendsView.cs:99, 109, 128). AuthUIController.OnLoginClicked success calls bs.RebindPhoton() (AuthUIController.cs:311), and RebindPhoton destroys FriendsChat (AuthBootstrapper.cs:198). The Accedi path does not reload the scene: StartScreenV2.Enter goes to AppLoadingView.EnterHome, a coroutine that waits and then invokes ready (AppLoadingView.cs:91-124). HomeV2Integration.ReloadAccountProfile only reloads the profile (HomeV2Integration.cs:265-279). OnAuthReady is raised only by AuthenticationFlow (AuthBootstrapper.cs:457); ReconnectPhoton never raises it. The friends.Count guards are at FriendsChat.cs:87 and :157. OnDisconnected never retries (FriendsChat.cs:177-183). IsConnected is client.CanChat, which is State==ConnectedToFrontEnd && HasPeer (ChatClient.cs:101-104), so a stale connected state makes Connect return early at FriendsChat.cs:131. Without UseBackgroundWorkerForSending, Service() is the only sender (ChatClient.cs:373-383). The stale-state race on resume is still inference, as the investigator says. LastSeen is the profile LastLogin (FriendsService.cs:41, 60). One nuance the investigator left out: when the device flag is 1 at launch, the Chat connection that starts at launch is for the bootstrap device account, not the real account (PlayFabAuthService.cs:161, FriendsChat.cs:133). That connection only wastes a token, unless that device account is itself registered (P2).

- `Assets/Scripts/Auth/AuthUIController.cs:306-318` Order on Accedi: PlayFabAuth.OnLoginSuccess fires inside LoginWithEmail (PlayFabAuthService.cs:378), then onSuccess runs RebindPhoton, then the UI OnLoginSuccess. Nothing in that chain reloads MainMenu or calls UI51FriendsView.Load.
- `Assets/UIV2/Scripts/Core/AppLoadingView.cs:91-124` EnterHome only waits for IsReady and preloads the deck. No scene load, so UI51FriendsView.Start does not run again.
- `Assets/Photon/PhotonChat/Code/ChatClient.cs:101-104, 365-384` CanChat is based only on the state. Outgoing ACKs and pings are sent only from Service() unless UseBackgroundWorkerForSending is set.

**Fix concerns.** (d) is safe in timing: OnLoginSuccess fires before RebindPhoton's Destroy (AuthUIController.cs:311), but GetFriends is an HTTP call, so Fill/Ensure runs after the old FriendsChat has been destroyed at the end of that frame. PlayFabAuthService is a plain C# object owned by the DontDestroyOnLoad bootstrapper, so the view must unsubscribe in OnDestroy, as the investigator notes. (a) is the part that fixes the resume race. Without it, (b) alone does not help, because Connect still returns early on a stale CanChat. Invoke(nameof(Connect)) works on a private method, and a pending Invoke dies with the destroyed component. Do not retry on InvalidAuthentication or CustomAuthenticationFailed. Dropping the friends.Count guards still leaves Connect gated by HasRealLogin, so guests stay disconnected.

### P2 — likely_cause · verifier: partially_confirmed · size S

**Current behaviour.** A wrong-password login fails, yet friends see the account as 'Visto poco fa' (and in one code path even Online).

**Root cause.** Two confirmed code paths. (A) 'Visto poco fa' means a successful PlayFab login of that account in the last 59 minutes from any device (UI51FriendsView.LastSeen < 1 h). A failed login does not create a session, so a recent earlier login is enough to explain the report. (B) Before any credentials, the app start silently runs LoginWithCustomID. When the device flag HasRealLogin=1 (PlayerPrefs, set by any earlier real login on this phone), it uses the persistent device ID. If an account was ever registered while the session was that device account (AddUsernamePassword converts the current session), the app logs straight into that real account at every launch. That updates LastLogin, and UI51FriendsView.Start -> Load (gated only by the device flag) -> FriendsChat connects it to Chat as Online, all while the login screen is shown. The password typed afterwards does not matter. Whether the tester's account had this device-ID link is unverified.

**Evidence.**
- `Assets/Scripts/Auth/AuthBootstrapper.cs:111-121, 323-340` Start runs the bootstrap login at launch, before StartScreenV2 shows the login panel.
- `Assets/Scripts/Auth/PlayFabAuthService.cs:161` With HasRealLogin=1 it uses the persistent device ID (GetOrCreateDeviceId), otherwise a per-launch random ID.
- `Assets/Scripts/Auth/PlayFabAuthService.cs:199-205` If the account behind the custom ID has a username/email and the flag is set, it is restored as a registered user. This is leftover auto-login.
- `Assets/Scripts/Auth/AuthUIController.cs:243-254` Registration converts the CURRENT session (which may be the device account) into a real account, linking the device custom ID to it.
- `Assets/UIV2/Scripts/Core/StartScreenV2.cs:27-35` The visible UI always asks for login at launch, so the silent restore contradicts it.
- `Assets/Scripts/UI/UI51FriendsView.cs:99, 122-128` Friends and presence load as soon as bootstrap is Ready, gated by the device flag, not by the user having entered.
- `Assets/Scripts/Auth/FriendsChat.cs:133` Connect gate is auth.HasRealLogin, the device-level flag.
- `Assets/Scripts/Auth/PlayFabAuthService.cs:74` HasRealLogin = PlayerPrefs flag (device-scoped), used as if it described the current session.
- `Assets/Scripts/UI/UI51FriendsView.cs:248` 'Visto poco fa' covers any LastLogin under 1 h.

**Fix sketch.** Make the pre-entry session never a real account. AuthBootstrapper.Start: always call PlayFabAuth.ClearRealLoginFlag() and ResetGuestDeviceId(). PlayFabAuthService.LoginAsGuest: always use GetOrCreateSessionGuestId(). Delete GetOrCreateDeviceId, DEVICE_ID_KEY and the 'returning registered user' branch at 199-205, which become dead. HasRealLogin is then set only by LoginWithEmail or MarkRegistered in the current run, so Friends and Chat (P1 fix) start only after valid authentication, and LastLogin moves only on a real login. LastSeen semantics: see P1(e).

**Files.** Assets/Scripts/Auth/AuthBootstrapper.cs, Assets/Scripts/Auth/PlayFabAuthService.cs

**Risk.** Rejoin-after-restart for registered accounts (HomeConnectionWatcher.TryRejoin, ModerationService.Registered uses the flag) will start only after Accedi via OnAccountChanged, so the 60 s window may lapse while the user types. Stops creating a phantom device account on every launch (good). 'Accedi come ospite' becomes a true ephemeral guest (today it can carry a real PlayFabId, see cross_cutting). AccountDeletionService.cs:41 still lists the old keys; that is harmless.

**Decision.** Confirm there is no 'remember me' / auto-login wanted. Today the screen always asks, but the bootstrap silently logs into whatever account owns this phone's device ID.

**Test plan.** Device: in PlayFab Game Manager, check the tester account's linked Custom ID and Last login. Launch the app, type a wrong password, close it. Expected after the fix: Last login unchanged and friends never see it Online. Before the fix, if the account has the device custom ID linked, Last login jumps at app start. EditMode: if LoginAsGuest is refactored to a pure ID chooser, a small test that it never returns the persistent device ID.

**Verifier (partially_confirmed).** Both of the investigator's paths are real. (A) LastSeen under 1 h reads 'Visto poco fa' (UI51FriendsView.cs:248), and a failed LoginWithEmail changes nothing (PlayFabAuthService.cs:381-386). (B) With the device flag at 1, LoginAsGuest uses the persistent device ID (PlayFabAuthService.cs:161) and restores a registered account (199-205). Context7 confirms that AddUsernamePassword 'adds playfab username/password auth to an existing account created via an anonymous auth method', so the custom ID stays linked. The investigator missed a more common path that matches the report even better: an in-run logout. SESSION_GUEST_ID_KEY is reset only in AuthBootstrapper.Start, and only when the flag is 0 (AuthBootstrapper.cs:114-117; grep: ResetGuestDeviceId has no other caller). The default way to create an account is first launch (flag 0) -> session guest S -> Registrati, and AddUsernamePassword turns S's account into the real account R (AuthUIController.cs:243-254). From then on SESSION_GUEST_ID_KEY points at R. The tester then logs out (Settings or Esci -> LogoutAndRestart(clearRealAccountFlag: true), AuthBootstrapper.cs:268-301, which clears the flag but not the key). StartAuthentication -> LoginAsGuest -> GetOrCreateSessionGuestId returns S, and LoginWithCustomID logs silently into R. R's LastLogin becomes now, and the session is shown as 'Ospite' because serverHasRealAccount is true but HasRealLogin is false (PlayFabAuthService.cs:199-212). The tester types the wrong password, the login fails, and friends who reopen Amici see R as 'Visto poco fa' (not Online, because Chat is gated by the cleared flag at FriendsChat.cs:133). The same happens on any later run that launched with the flag at 1, because Start skips the reset. Minor extra contributor: a LastLogin in the future relative to the viewer's clock (clock skew) also reads 'poco fa', since a negative span is under 1 h (UI51FriendsView.cs:247-248).

**Corrected cause.** The pre-entry PlayFab session can be a real account through two custom IDs: the persistent device ID (investigator's path, flag=1 at launch) and the session-guest ID. The session-guest ID is never cleared on logout or after registration, so every logout of a registration-created account silently logs back into that account and refreshes its LastLogin. Either path makes LastLogin move without a valid password entry. Only the device path also connects Chat (Online), and only while the device flag is still 1.

- `Assets/Scripts/Auth/AuthBootstrapper.cs:114-117, 268-301` The only ResetGuestDeviceId call is in Start, guarded by !HasRealLogin. LogoutAndRestart clears the flag and calls StartAuthentication without resetting the session-guest key.
- `Assets/Scripts/Auth/PlayFabAuthService.cs:103-113, 161` After the flag is cleared, LoginAsGuest reuses the stored SESSION_GUEST_ID_KEY. That ID was linked to the real account if registration ran on that session.
- `Assets/Scripts/Auth/AuthUIController.cs:243-254` Registration adds credentials to the CURRENT session account, so the session-guest custom ID becomes a login key for the real account.
- `Assets/UIV2/Scripts/Core/SettingsV2Integration.cs:154-161` Logout -> LogoutAndRestart(clearRealAccountFlag: true), then MainMenu reload. The silent re-login into the registered account happens here.

**Fix concerns.** The proposed fix, ClearRealLoginFlag and ResetGuestDeviceId at Start plus always using the session-guest ID, does NOT cover the in-run logout path: register -> logout in the same run still logs into R. It also needs PlayFabAuth.ResetGuestDeviceId() in LogoutAndRestart before StartAuthentication, or in MarkRegistered, because after registration that session ID belongs to a real account. Deleting GetOrCreateDeviceId breaks compilation: GetBestDisplayName calls it (PlayFabAuthService.cs:138), so that fallback must change too. ForceGuestIdentity ('Accedi come ospite') keeps whatever session the bootstrap made (PlayFabAuthService.cs:121-128); with the full fix that session is always ephemeral, which is the intended behaviour.

### P3 — root_cause_confirmed · verifier: partially_confirmed · size S

**Current behaviour.** Presence and 'Visto' by event (what friends see) today. Correct login (Accedi): Offline until the user opens Amici (Chat destroyed by RebindPhoton, not recreated); LastLogin=now. Failed login: no change from the attempt, but if the device flag is set the bootstrap account may already be Online/'poco fa' (P2). Logout (Settings or Esci): FriendsChat destroyed -> explicit Disconnect -> Offline promptly; MainMenu reloads (correct). Force close: no explicit disconnect, Offline only after the Chat server timeout. Background: no ACKs, server timeout -> Offline. On resume it usually stays Offline: reconnect only if friends>0, and it races the timeout detection. Network loss: OnDisconnected, never retried, Offline until Amici is opened or the scene changes. Reconnect: HomeConnectionWatcher reconnects PUN only; Chat is untouched. Normal close: OnDestroy -> Disconnect -> Offline (on mobile a swipe-kill behaves like force close). Entering GameScene: 'In partita' only if already connected. 'Visto' everywhere = login time, never logout or close time.

**Root cause.** Same block as P1 and P2. The Chat connection is tied to the Amici view's data load instead of the authenticated session. It has no retry or resume handling, and LastSeen uses LastLogin.

**Evidence.**
- `Assets/Scripts/Auth/AuthBootstrapper.cs:198, 271` Account change and logout destroy FriendsChat. Only logout is followed by a scene reload (SettingsV2Integration.cs:159-160, AuthScreensV2.cs:199-200).
- `Assets/Scripts/Auth/FriendsChat.cs:74-79` OnDestroy -> client.Disconnect() gives an immediate Offline on logout or normal quit.
- `Assets/Scripts/Auth/FriendsChat.cs:84-88, 177-183` No reconnect after a pause race or a network drop.
- `Assets/Scripts/Networking/HomeConnectionWatcher.cs:30-40` PUN reconnect and rejoin only. No FriendsChat reference (grep).
- `Assets/Photon/PhotonChat/Code/ChatClient.cs:1169-1180` A client timeout ends in Disconnect(ClientTimeout) -> OnDisconnected, distinguishable from DisconnectByClientLogic.

**Fix sketch.** Covered by the P1 and P2 fixes. Target after the fix: Accedi -> Online in seconds; failed login -> nothing; logout or normal close -> Offline at once; background -> Offline (or Online for a grace period, user decision) and Online again on resume; network loss -> automatic retry with backoff; force close -> Offline after the server timeout (cannot be faster without a server); 'Visto' = last login unless a server last-seen is approved.

**Files.** Assets/Scripts/Auth/FriendsChat.cs, Assets/Scripts/UI/UI51FriendsView.cs, Assets/Scripts/Auth/PlayFabAuthService.cs, Assets/Scripts/Auth/AuthBootstrapper.cs

**Risk.** See P1 and P2.

**Decision.** Background: Offline at once, or stay Online for a grace period (for example 60 s)? A real last-seen time (needs a server handler and a write at pause or entry), or rename the label to 'Ultimo accesso'?

**Test plan.** Device only, two phones, scripted matrix: correct login, wrong password, logout, force close (record seconds until Offline on the other phone), background 10 s / 60 s, airplane mode 20 s, Wi-Fi to 4G switch, normal close, enter and leave a match. Note the presence on the other phone and the 'Visto' text for each step.

**Verifier (partially_confirmed).** Most of the matrix matches the code. Logout destroys FriendsChat and OnDestroy calls Disconnect (AuthBootstrapper.cs:271, FriendsChat.cs:74-79), then MainMenu reloads (SettingsV2Integration.cs:159-160, AuthScreensV2.cs:199-200). The resume and network-drop gaps are confirmed (FriendsChat.cs:84-88, 177-183). HomeConnectionWatcher only reconnects PUN (HomeConnectionWatcher.cs:122-163; no FriendsChat reference). Two corrections. (1) 'Visto everywhere = login time, never logout' is wrong for registration-created accounts: logout silently logs back into that account through the stale session-guest ID and sets LastLogin to the logout moment (see P2). (2) 'In partita only if already connected' is too strong. OnSceneChanged sets playingNote before Connect (FriendsChat.cs:156-157), and OnConnected -> PublishStatus publishes Playing (FriendsChat.cs:170-175, 161-166). So a connection that completes in GameScene still shows 'In partita'. It is missed only when friends.Count == 0 and no connection existed. Addition for 'Accedi come ospite': when the bootstrap had connected Chat (flag 1 at launch), ForceGuestIdentity does not tear the connection down. That account stays Online until a disconnect, while resume no longer reconnects it.

**Corrected cause.** Same block as P1 plus the corrected P2: Chat is tied to the Amici data load, there is no retry or resume handling, LastSeen = LastLogin, and logout can itself count as a login for accounts created by registration.

- `Assets/Scripts/Auth/FriendsChat.cs:154-175` playingNote is set on the scene change and published from OnConnected too, so 'In partita' does not require an existing connection.
- `Assets/Scripts/Auth/PlayFabAuthService.cs:121-128` ForceGuestIdentity clears flags and name only. It neither destroys FriendsChat nor changes the session.

**Fix concerns.** Same as P1 and P2. The P2 fix must also reset the session-guest key on logout, or the logout row of the matrix will still bump LastLogin.

### P4 — likely_cause · verifier: partially_confirmed · size S

**Current behaviour.** One Android device appeared always Offline to the others.

**Root cause.** Not Android-specific in code. Ranked candidates: (1) CONFIRMED mechanism: that phone was not connected to Chat because Amici was not reopened after Accedi, after background or after a network blip (P1). It is worse if that account has no friends of its own: friendship is one-way, so the iOS user can watch Android while Android's list is empty, and the resume/scene-change reconnects are skipped when friends.Count == 0. (2) HYPOTHESIS: a different build version. PUN and Chat both pass Application.version as AppVersion; bundleVersion went from 2.64 to 1.0.0 on 04/10 (ab9eb1c) and Android versionCode is still 264. Separation is confirmed for PUN (room codes not found), not verified for Chat (Context7 has no statement on it). (3) HYPOTHESIS: UDP blocked on the Android's network. ChatClient is built with the UDP default and EnableProtocolFallback=false, while PUN has fallback on, so PUN works but Chat fails. Failures are silent because OnDisconnected does not log the cause.

**Evidence.**
- `Assets/Scripts/Auth/FriendsChat.cs:87, 157` Reconnect on resume or scene change is skipped when the local friend set is empty (one-way friendship).
- `Assets/Scripts/Auth/FriendsService.cs:28-31` AddFriend is immediate and one-way. Context7 PlayFab docs: adds the user to the local user's friend list.
- `Assets/Scripts/Auth/FriendsChat.cs:146` Chat appVersion = Application.version.
- `Assets/Scripts/Auth/PhotonAuthConnector.cs:47-52` PUN AppVersion = Application.version, so only identical builds match.
- `ProjectSettings/ProjectSettings.asset:143, 175` bundleVersion 1.0.0, AndroidBundleVersionCode 264. An Android build made before ab9eb1c reports 2.64.
- `Assets/Scripts/Auth/FriendsChat.cs:141` new ChatClient(this): UDP by default, no fallback.
- `Assets/Photon/PhotonChat/Code/ChatClient.cs:59, 236, 1140-1153` EnableProtocolFallback defaults to false. ExceptionOnConnect without fallback disconnects.
- `Assets/Photon/PhotonUnityNetworking/Resources/PhotonServerSettings.asset:27` PUN EnableProtocolFallback: 1.
- `Assets/Scripts/Auth/FriendsChat.cs:177-183` Disconnect cause is never logged, so device logs cannot show it today.

**Fix sketch.** Same block as P1 (reconnect, no friends.Count guards, connect on login), plus client.EnableProtocolFallback = true and a debug-build log of DisconnectedCause. No code fix for a version mismatch: show the version on both phones and test only matching builds.

**Files.** Assets/Scripts/Auth/FriendsChat.cs

**Risk.** Protocol fallback only adds a second connect attempt. Low risk.

**Test plan.** Device: compare the version label (Application.version) on the Android and iOS builds. Check whether the Android account has the iOS account in its own list. On Android open Amici and watch iOS (Online?). Then background Android for 30 s and return (stays Offline today). Run logcat filtered on [FriendsChat] after adding the cause log. Repeat on Wi-Fi and on 4G.

**Verifier (partially_confirmed).** Mechanism (1) is confirmed in code. The friends.Count guards are at FriendsChat.cs:87 and :157, AddFriend is one-way (FriendsService.cs:29-31, 79-81), and no Android-specific presence code exists (grep found UNITY_ANDROID only in GameFeedback and UI51ServiceScreen). Hypothesis (3) checks out in code: new ChatClient(this) uses UDP (ChatClient.cs:236), EnableProtocolFallback defaults to off and is never set (ChatClient.cs:59, FriendsChat.cs:141), while PUN has fallback on (PhotonServerSettings.asset:27), and OnDisconnected does not log the cause. Hypothesis (2) is stronger than stated. The bundled Chat SDK documents the version separation: AppVersion 'A new version also creates a new virtual app to separate players from older client versions' (ChatClient.cs:120; see also 287). Mixed builds therefore never see each other's presence or invites. Correction: bundleVersion changed 2.64 -> 1.0.0 in commit 26a8b49 ('Prepare 51 for iOS build', 2026-10-04), not ab9eb1c. Which candidate hit the Android phone cannot be told from code.

**Corrected cause.** Same ranking, with (2) upgraded: the SDK documents that Chat AppVersion separates clients into virtual apps, so an Android build from before 26a8b49 (Application.version 2.64) is invisible to a 1.0.0 build.

- `Assets/Photon/PhotonChat/Code/ChatClient.cs:120, 287` AppVersion doc: a new version creates a new 'virtual app' that separates players. That confirms the version split for Chat.
- `ProjectSettings/ProjectSettings.asset:143` git log -S shows that 26a8b49 (2026-10-04) changed bundleVersion 2.64 -> 1.0.0. ab9eb1c did not touch it.

**Fix concerns.** EnableProtocolFallback is low risk. The fallback reconnect reuses AuthValues (ChatClient.cs:1102-1109), so the custom auth parameters carry over.

### L1 — root_cause_confirmed · verifier: confirmed · size XS

**Current behaviour.** After inviting a friend once, the INVITA button for that friend is replaced by 'Invitato…' for the whole MainMenu lifetime, both on the Amici page and in every later private lobby. It resets only when MainMenu reloads (after a match or logout), which is why re-inviting works only 'sometimes'. Other failures look the same: an invite sent from Amici while Chat is disconnected fails silently; the receiver drops invites silently while in a lobby or the search panel, or when its FriendsChat is not connected (P1); and an invite is dropped if the two phones' clocks differ by more than 120 s.

**Root cause.** UI51FriendsView.invited is a HashSet that is only ever added to (never cleared or expired). Both views hide INVITA for any id in it. The other contributors are listed under evidence.

**Evidence.**
- `Assets/Scripts/UI/UI51FriendsView.cs:65` private readonly HashSet<string> invited, never cleared (grep: only Add/Contains).
- `Assets/Scripts/UI/UI51FriendsView.cs:235-237` Amici row: INVITA hidden once invited.
- `Assets/Scripts/UI/UI51FriendsView.cs:356-360, 377-382` Added on send. The RoomCreated path has no else branch, so a failed send gives no feedback.
- `Assets/Scripts/UI/UI51PrivateRoomView.cs:138, 142-143` Lobby row shows 'Invitato' forever via WasInvited (UI51FriendsView.cs:371).
- `Assets/Scripts/UI/UI51FriendsView.cs:388` Receiver drops invites silently while InRoom or roomFlow.IsOpen.
- `Assets/Scripts/Auth/FriendsChat.cs:113, 123-124` Invite age is measured with the SENDER's clock against the receiver's clock (±120 s).
- `Assets/Scripts/UI/UI51InviteBanner.cs:16, 71` The receiver's banner disappears after 20 s, so the host has no way to send again.

**Fix sketch.** Replace the HashSet with Dictionary<string,(string room, float at)>. WasInvited(id) = PhotonNetwork.InRoom && the same room code && Time.unscaledTime - at < UI51InviteBanner.Seconds. BindRow uses WasInvited, which is false outside a room. The lobby already re-binds every 0.2 s, so the button comes back on its own after 20 s. In RoomCreated, show the same 'Invito non inviato' toast as InviteToRoom when SendInvite returns false. Optional: widen InviteMaxAgeSeconds or ignore negative ages to tolerate clock skew.

**Files.** Assets/Scripts/UI/UI51FriendsView.cs

**Risk.** A host can re-send every 20 s; the receiver's banner simply refreshes (UI51InviteBanner.Show replaces the current invite). No other callers of the invited set.

**Test plan.** EditMode: extract a pure static InvitePending(sentRoom, sentAt, room, now) and assert the same room within 20 s is true, and a different room or later time is false. Device (two phones): invite, Rifiuta, wait 20 s, re-invite (must arrive); leave the room, create a new one, invite the same friend again.

**Verifier (confirmed).** The invited HashSet (UI51FriendsView.cs:65) is only ever added to (lines 358 and 379); grep shows no Remove or Clear. The Amici row hides INVITA for invited ids (235-237), and the lobby row uses WasInvited (UI51PrivateRoomView.cs:138, 142-143, via UI51FriendsView.cs:371). The view is a MainMenu scene object, so the set resets only when MainMenu reloads. The RoomCreated failure is silent (356-360, no else). The receiver drop when InRoom or roomFlow.IsOpen is at line 388. The ±120 s window is measured on the sender's clock (FriendsChat.cs:113, 123-124). The banner auto-hides after 20 s (UI51InviteBanner.cs:16, 71). A further silent failure: SendPrivateMessage returns true while the sender is connected even if the receiver is not connected to Chat (P1), so the sender sees 'Invitato…' for an invite that never arrives.

**Fix concerns.** The proposed Dictionary plus WasInvited (InRoom, same room, under 20 s) is safe. Both callers (BindRow and UI51PrivateRoomView.BindFriends) re-render: the lobby every 0.2 s (RoomFlowV2.cs:463-468), the Amici page on Render. Re-sending every 20 s is acceptable, since Show replaces the current invite (UI51InviteBanner.cs:36-50).

### L2 — feature_missing · verifier: confirmed · size L

**Current behaviour.** Teams in 2v2 are not random. They follow the server's join order (ActorNumber ascending): 1st and 2nd to enter are partners (seats 0+2), 3rd and 4th are the other pair (seats 1+3). The lobby shows this only as two rows of two seats with a gold or blue ring; there is no 'Squadra 1/Squadra 2' label and no way to choose. Because the order depends on who reaches the server first, re-entries or the host leaving, it looks random.

**Root cause.** Deliberate rule chosen by the user on 16/09 (memory: 'host and first friend who joins are partners', [0,2,1,3] by join order). It is applied everywhere through SeatLayout.SeatForJoinOrder.

**Evidence.**
- `Assets/Scripts/Core/SeatLayout.cs:14-29` TeamSeats {0,2,1,3}; BotSeats assumes the humans are join indices 0..n-1.
- `Assets/Scripts/Gameplay/GameSceneInitializer.cs:134-152` The master recomputes the roster from ActorNumber order and writes room prop 'roster'; non-masters read it.
- `Assets/Scripts/Gameplay/GameSceneInitializer.cs:160-168, 344-348` Seat lookup and bot seats are derived from join index and human count.
- `Assets/UIV2/Scripts/Core/RoomFlowV2.cs:327-331, 354-372` Lobby grid slot = join order; ally ring from SeatForJoinOrder.

**Fix sketch.** If approved, reuse the existing 'roster' room prop instead of adding a new system. In the lobby the master writes a seat choice; players tap a seat in Squadra 1 or Squadra 2, or the host swaps players (user decision). At start the master writes 'roster' already in the order SeatForJoinOrder expects ([A1, A2, B1, B2]), using a placeholder (actor 0) for bot-held positions. GameSceneInitializer: the master uses the lobby roster instead of recomputing; BotSeats must take the occupied positions rather than a count; Roster() and SyncSeatsWithRoom must skip placeholders. RoomFlowV2 and UI51PrivateRoomView: 'Squadra 1/Squadra 2' headers, tap to move, bot mask keyed by seat. The builder change (UI51MatchBuilder) must go through the Fase 13 builder. Quick match keeps join order. CloudScript is unaffected because it records ActorNr, not seats.

**Files.** Assets/Scripts/Core/SeatLayout.cs, Assets/Scripts/Gameplay/GameSceneInitializer.cs, Assets/UIV2/Scripts/Core/RoomFlowV2.cs, Assets/Scripts/UI/UI51PrivateRoomView.cs, Assets/UI51/Editor/UI51MatchBuilder.cs, Assets/Tests/Editor/MatchScoreTests.cs

**Risk.** Seat mapping is central: rejoin (roster prop), forfeit (Roster()), referee (RefereeActorFor), inactivity strikes and the bot provider all read the join-order mapping. A wrong mapping deals the wrong hands, so EditMode coverage of the mapping is mandatory.

**Decision.** Do you want team choice? Who decides (each player taps a seat, or the host arranges)? Can a player switch after others are seated? Do bots fill per team? Quick match keeps join order?

**Test plan.** EditMode: roster-to-seat mapping with placeholders (2 humans same team, opposite teams, 3 humans) and BotSeats. Play Mode in Editor cannot host 4 real clients; final check on 2-4 devices: choose teams, start, verify partners sit opposite and scores pool correctly; then rejoin mid-match.

**Verifier (confirmed).** SeatLayout.TeamSeats {0,2,1,3} maps join order to seats (SeatLayout.cs:14-20). The master recomputes the roster by ActorNumber and writes 'roster'; non-masters read it (GameSceneInitializer.cs:134-152). The lobby grid uses join order and SameTeam (RoomFlowV2.cs:327-331, 354-372). BotSeats assumes humans occupy join indices 0..n-1 (SeatLayout.cs:23-28; GameSceneInitializer.cs:345-348). MatchScoreTests.cs:211-220 pins 'Host and first friend are partners'. No randomness exists; order depends on ActorNumber, and a re-entry gets a new, higher number. CloudScript records ActorNr in the match group (51.js:655-664), so no seat or team logic is on the server.

**Fix concerns.** With placeholder actors (0) in the roster, more than the listed files must skip them. MatchResultsV2 iterates Roster() at lines 153, 181 and 242, NetworkGameController.cs:726 maps seat->actor through Roster(), RefereeActorFor loops the join order (GameSceneInitializer.cs:216-223), and GetLocalPlayerIndex uses the join index (GameSceneInitializer.cs:436, 447). realPlayers = _stableActorOrder.Count (GameSceneInitializer.cs:345) would count placeholders. The safest approach is for Roster() and GetPlayerIndexForActor to filter placeholders centrally, and for BotSeats to take the occupied set. MatchScoreTests.cs:211-220 must be updated.

### L3 — works_as_designed · verifier: confirmed · size M

**Current behaviour.** Host leaving: Photon makes another player master; the lobby swaps that player to the host panel (0.2 s poll) and they can start (StartGame checks only IsMasterClient). Non-host invites: allowed, since the guest lobby has INVITA AMICI ONLINE. Order of entry: decides the teams (L2). Team change: not possible. Full lobby: MaxPlayers = format size, so a 5th player gets 'La stanza è piena' (32765); after start the room is closed (32764). Bot-marked free slots can still be taken by an arriving human. Confirmed side effects: when someone leaves or re-enters (new higher ActorNumber), or the host leaves, everyone's join index shifts, so teams and portraits shift. The bot toggles are bits per grid slot, so they shift relative to the players.

**Root cause.** Host migration is a documented choice of mine (backlog 536), which differs from the mockup text 'la stanza viene chiusa'; it still needs the user's confirmation. Non-host invites match the mockup: SalaPrivata with ruolo='ospite' still lists friends (filter host || f.id !== 'giulia'). The side effects come from the join-order seating (L2), from portraits indexed by slot rather than by player, and from the bot mask indexed by slot.

**Evidence.**
- `Assets/Scripts/UI/UI51PrivateRoomView.cs:154-161` Host leave text 'la stanza passa a un altro giocatore'.
- `SPRINT_BACKLOG.md:536, 541` Host-migration text was my choice against the mockup; guest lobby lists friends.
- `Design/51_handoff/51_handoff/mockups/SalaPrivata.dc.html:112` friends: fr.filter((f) => host || ...): the guest also sees invites. Mockup leaveText for host: room closes.
- `Assets/UI51/Editor/UI51MatchBuilder.cs:35-36, 282-296` The friends block is built for both host and guest pages.
- `Assets/UIV2/Scripts/Core/RoomFlowV2.cs:203, 351, 463-468` The host panel follows IsMasterClient every 0.2 s.
- `Assets/Scripts/Networking/MatchmakingManager.cs:263-284, 322-338, 499-500` StartGame is master-only; MaxPlayers = PlayerCount; full or closed error messages.
- `Assets/UIV2/Scripts/Core/RoomFlowV2.cs:286-303, 367, 371` ui_bots bitmask and portraits are by slot index, so both shift when the order changes.

**Fix sketch.** No bug-level fix needed for host migration or guest invites. If join-order teams stay, the smallest stabiliser is: the master records the lobby order once per actor in a room prop (appended on OnPlayerEnteredRoom, removed on leave) and RefreshPlayers and LockStableActorRosterIfNeeded use it; portraits keyed by ActorNumber. This is best folded into L2. If you prefer the mockup behaviour, the host leaving closes the room: on master switch the new master calls LeaveRoom for everyone (RPC) and the guest text changes.

**Files.** Assets/UIV2/Scripts/Core/RoomFlowV2.cs, Assets/Scripts/Gameplay/GameSceneInitializer.cs, Assets/Scripts/UI/UI51PrivateRoomView.cs

**Risk.** Touches the same seat mapping as L2.

**Decision.** Host leaving: keep migration (current) or close the room as in the mockup? Non-host invites: keep (mockup) or host only?

**Test plan.** Device, 3 phones: host plus 2 guests in 2v2. Host leaves: verify the new host panel, AVVIA works, teams shown, invite from the guest arrives. Guest leaves and rejoins: verify the team shown (today it changes). Fill with bots, then a human joins a bot slot.

**Verifier (confirmed).** Host migration works as described. RoomFlowV2 swaps panels on IsMasterClient every 0.2 s (RoomFlowV2.cs:203, 351, 463-468). StartGame checks only IsMasterClient (MatchmakingManager.cs:263-284), and GameLaunchController.GoToSceneForConfig loads the level when IsMasterClient (GameLaunchController.cs:227-230). The new master's CurrentConfig.IsHost stays false, but nothing on the start path reads it, and its Format was aligned from the room props in OnJoinedRoom (MatchmakingManager.cs:437-444). MaxPlayers = PlayerCount (MatchmakingManager.cs:326), the full and closed errors are 32765 and 32764 (499-500), and LockRoomForMatch closes the room (105-106). The guest page gets the friends block from the builder (UI51MatchBuilder.cs:282-296, 363-367), and the host-leave text matches the backlog choice (SPRINT_BACKLOG.md:536). Portraits and ui_bots are keyed by slot (RoomFlowV2.cs:286-303, 367, 371). Small inconsistency: backlog line 539 says 'nel 2v2 la prima riga è la tua squadra', but RefreshPlayers lays out seats in join order. For the 3rd and 4th joiners the first row is the opposing pair (only the ring colour marks the team), which adds to the 'random teams' impression.

- `Assets/Scripts/UI/GameLaunchController.cs:190-231` Start after migration: OnMatchFound -> GoToSceneForConfig -> PhotonNetwork.LoadLevel when IsMasterClient. IsHost is not checked.
- `SPRINT_BACKLOG.md:539` Backlog says the first row is your team. The code orders seats by join index, so this holds only for the first two joiners.

**Fix concerns.** The investigator's stabiliser (a lobby order prop keyed by ActorNumber) touches the same mapping as L2. Folding it into L2 is right.

### SO1 — root_cause_confirmed · verifier: confirmed · size XS

**Current behaviour.** 'Aggiungi amico' always adds immediately and one-way through PlayFab AddFriend; no request is created and the other player is not notified. Two UIs describe it differently. The Amici page says 'X aggiunto agli amici', which is accurate. The table's quick profile says 'Richiesta inviata', which implies a request; hence 'sometimes it looks like an immediate add'. The 'Richieste' tab is hard-wired to 0 and empty. The quick profile does not know who is already a friend, so tapping 'Aggiungi amico' on an existing friend fails silently (UsersAlreadyFriends is only logged). Its 'added' state is a static set that survives account switches.

**Root cause.** PlayFab has no friend requests (verified with Context7: AddFriend 'Adds the PlayFab user... to the friend list of the local user'). No CloudScript friend handler exists. The quick-profile copy was written for a request flow that was never built.

**Evidence.**
- `Assets/Scripts/Auth/FriendsService.cs:28-31, 66-81` AddFriendByName and AddFriend call PlayFabClientAPI.AddFriend directly.
- `Assets/Scripts/UI/QuickProfileCard.cs:57, 128-134, 205-211` Static s_Added; success shows addedLabel; error only re-enables the button.
- `Assets/UI51/Editor/UI51TableBuilder.cs:1212, 1223` addedLabel text 'Richiesta inviata'.
- `Assets/Scripts/UI/UI51FriendsView.cs:180, 311` Richieste count hard-wired "0"; toast 'aggiunto agli amici'.
- `Server/CloudScript/51.js:337-673` Handlers: inizio, statoPremi, riscatta*, premioPartita, segnala, abbandono, moderazione, webhooks. No friend handlers.
- `SPRINT_BACKLOG.md:352` Documented: friend requests do not exist yet, Aggiungi amico just adds.

**Fix sketch.** If friendship stays one-way (XS): change the builder label to 'Aggiunto agli amici' (UI51TableBuilder.cs:1223, then re-run the Fase 5 builder); show a toast on error, mapping UsersAlreadyFriends via FriendsService.AddError; clear s_Added and s_Reported on account change, or key them by owner PlayFabId. Hide or label the Richieste tab until requests exist. Real requests (L): CloudScript handlers for request, accept and decline (pending list in the target's internal or read-only data; server AddFriend in both directions on accept; BlockList filter), plus a client Richieste tab and badge.

**Files.** Assets/UI51/Editor/UI51TableBuilder.cs, Assets/Scripts/UI/QuickProfileCard.cs, Assets/Scripts/UI/UI51FriendsView.cs

**Risk.** Copy change only. A builder re-run regenerates GameCanvas/UI51QuickProfile; use the Fase 5 builder as usual.

**Decision.** Keep one-way friendship and fix the wording, or build real mutual requests on CloudScript (size L, new handlers, deploy via carica.js)?

**Test plan.** Editor Play Mode: open the quick profile of an account player (needs real profile data), tap Aggiungi, check the label and toast; tap again on an existing friend and expect an error toast. Visual check through the ui-verify skill.

**Verifier (confirmed).** FriendsService.AddFriendByName and AddFriend call PlayFabClientAPI.AddFriend directly (FriendsService.cs:66-81). Context7 (PlayFab docs) and the file's own header (lines 29-30) say AddFriend is immediate and one-way. QuickProfileCard keeps a static s_Added and s_Reported (QuickProfileCard.cs:57) that are never cleared. On error it only re-enables the button (line 133); FriendsService.Fail logs only in debug builds (line 95). The builder label is 'Richiesta inviata' (UI51TableBuilder.cs:1223). The Amici toast says 'aggiunto agli amici' (UI51FriendsView.cs:311). The Richieste count is hard-wired to '0' (UI51FriendsView.cs:180). 51.js has no friend handlers (handlers listed at 337-673). Guests get no buttons (PlayerBannerManager.cs:216).

**Fix concerns.** The copy change needs a re-run of the Fase 5 table builder, as stated. Mapping the error needs the PlayFabErrorCode, but FriendsService.AddFriend's onError has no parameter today (FriendsService.cs:79-81). Switch to the AddError(e.Error) pattern of AddFriendByName.

### SO2 — root_cause_confirmed · verifier: confirmed · size S

**Current behaviour.** Presence, invites and friend data look frozen until the Amici screen is reopened. Lobby seats do refresh live (0.2 s poll). Friend avatars are never the friend's real avatar.

**Root cause.** (1) Reopening Amici is the only reconnect trigger for the Chat presence connection (Open -> Load -> Fill -> Watch -> Connect). Every drop clears all presence to Offline and is not retried (P1). The invite listener is subscribed only in Fill, so after Accedi invites do not arrive at all until Amici is opened. (2) The friends list, levels and LastLogin are fetched only at Start or Open, with no refresh; this is acceptable once presence is live. (3) Avatars are a hash of the PlayFabId in Amici and by slot in the lobby, because the chosen avatar is not published (LookProps has no avatar key). This is a design gap, not a refresh bug.

**Evidence.**
- `Assets/Scripts/UI/UI51FriendsView.cs:101-110, 156-163` Open reloads, and Fill re-subscribes OnPresenceChanged and OnInvite and reconnects.
- `Assets/Scripts/Auth/AuthBootstrapper.cs:198` Accedi destroys FriendsChat, so the view's subscriptions die with it.
- `Assets/Scripts/UI/UI51FriendsView.cs:255-263` PortraitFor hashes the id; comment says the profile avatar is not published.
- `Assets/Scripts/Auth/AuthBootstrapper.cs:174-189` LookProps: frame, banner, level, stats, id; no avatar.
- `Assets/UIV2/Scripts/Core/RoomFlowV2.cs:367, 371, 463-468` Lobby refresh every 0.2 s; other players' portraits by slot.
- `Assets/Scripts/UI/UI51PrivateRoomView.cs:127` Lobby friend rows = RoomFriends (online by Chat presence), so they are empty when Chat is disconnected.

**Fix sketch.** Presence and invites are fixed by the P1 block (S). Real avatars: publish the selected avatar id in the PlayFab profile (avatar URL or player data) and in LookProps, then read it in Amici, Classifica, the lobby and the table (M; memory notes the SelectedAvatarId debt). This needs the user's go.

**Files.** Assets/Scripts/Auth/FriendsChat.cs, Assets/Scripts/UI/UI51FriendsView.cs

**Risk.** See P1.

**Decision.** Publish the chosen avatar so friends, lobby and table show it (new published field)?

**Test plan.** Device, two phones: after the P1 fix, keep Amici open on A while B logs in, backgrounds and returns; the row must change without reopening. Send an invite to B right after B's Accedi (Amici never opened); the banner must appear.

**Verifier (confirmed).** Opening Amici is the only path to Fill -> Watch -> Connect and to the OnInvite and OnPresenceChanged subscriptions (UI51FriendsView.cs:101-110, 156-163). RebindPhoton destroys FriendsChat (AuthBootstrapper.cs:198), so after Accedi the new account is not connected to Chat at all, and invites cannot arrive until Amici is opened. Lobby seats poll every 0.2 s (RoomFlowV2.cs:463-468). Avatars: PortraitFor hashes the id (UI51FriendsView.cs:255-263), lobby portraits are by slot (RoomFlowV2.cs:367, 371), and LookProps has no avatar key (AuthBootstrapper.cs:174-189). The friends list also reloads after a successful add (UI51FriendsView.cs:312), a minor addition to 'only at Start or Open'.

**Fix concerns.** Covered by the P1 fix (d). Publishing the avatar is a separate decision (new LookProps key plus a profile field).

### Missed by the investigator (found by the verifier)

- **P2**: Logout silently logs back into an account created by in-app registration. SESSION_GUEST_ID_KEY is reset only in AuthBootstrapper.Start, and only when the device flag is 0. Registration (AddUsernamePassword) converts the current session-guest account into the real account, so that stored custom ID now opens the real account. On logout (Settings or Esci) LogoutAndRestart clears the flag and re-runs LoginAsGuest with the same stored session-guest ID. The real account is then logged in as 'Ospite': its LastLogin is refreshed ('Visto poco fa' to friends), CloudScript 'inizio' runs for it from Mail/Rewards Start, and if the user picks 'Accedi come ospite' they play online under the real PlayFabId, published as the guest id 'og', so reports and sanctions land on the real account. This is the most likely explanation of P2, and the proposed P2 fix does not cover it.
  - `Assets/Scripts/Auth/AuthBootstrapper.cs:114-117` The only ResetGuestDeviceId call (grep), at Start and only when !HasRealLogin.
  - `Assets/Scripts/Auth/AuthBootstrapper.cs:268-301` LogoutAndRestart clears the flags and restarts auth without resetting the session-guest key.
  - `Assets/Scripts/Auth/PlayFabAuthService.cs:103-113, 161, 199-212` With the flag at 0, the stored session-guest ID is reused. A server-side real account is then treated as a guest (DisplayName null) but keeps its PlayFabId.
  - `Assets/Scripts/Auth/AuthUIController.cs:243-254` Registration converts the current session account (Context7: AddUsernamePassword adds credentials to an existing anonymous account).
  - `Assets/Scripts/Auth/AuthBootstrapper.cs:165-167` In guest mode PublishLook publishes PlayFabAuth.PlayFabId as the guest id, which here is the real account's id.
- **P3**: 'Accedi come ospite' (ForceGuestIdentity) clears the device flag but neither destroys FriendsChat nor changes the PlayFab session. If the bootstrap connected Chat (flag at 1 at launch, Amici loaded at Start), that account stays Online in Photon Chat while the user plays as a guest. If the device account is a registered one (investigator's path B), its friends see it Online although no one logged in. The connection then never reconnects after a drop, because Connect is now gated off.
  - `Assets/Scripts/Auth/PlayFabAuthService.cs:121-128` Only DisplayName and the flags are cleared.
  - `Assets/UIV2/Scripts/Core/StartScreenV2.cs:38-42` PlayAsGuest -> ForceGuestIdentity -> Enter. FriendsChat is untouched.
  - `Assets/Scripts/Auth/FriendsChat.cs:133` HasRealLogin is checked only when a connection starts, never for an existing one.
- **P2**: The proposed P2 fix would not compile as written. It deletes GetOrCreateDeviceId and DEVICE_ID_KEY, but GetBestDisplayName still calls GetOrCreateDeviceId as its fallback when PlayFabId is empty.
  - `Assets/Scripts/Auth/PlayFabAuthService.cs:130-143` GetBestDisplayName -> GetOrCreateDeviceId() at line 138.
- **P2**: LastSeen treats a LastLogin in the future, relative to the viewer's phone clock, as under 1 hour and shows 'Visto poco fa'. A viewer whose clock is behind sees 'poco fa' for logins that are hours old. This is a minor extra contributor to the P2 symptom.
  - `Assets/Scripts/UI/UI51FriendsView.cs:245-248` span = now - then. A negative span passes TotalHours < 1.
- **P4**: The cross-cutting 'other' entry and P4 credit the wrong commit for the version change. bundleVersion 2.64 -> 1.0.0 happened in 26a8b49 'Prepare 51 for iOS build' (2026-10-04), not in ab9eb1c. Also, Photon Chat separation by AppVersion is not unverified: the bundled SDK documents it.
  - `ProjectSettings/ProjectSettings.asset:143` git log -S 'bundleVersion: 1.0.0' points to 26a8b49.
  - `Assets/Photon/PhotonChat/Code/ChatClient.cs:120` 'A new version also creates a new virtual app to separate players from older client versions.'

### Cross-cutting notes

- **playfab-login-lifecycle** (Posta badge/data, Premi badge/data, UI51FriendsView): Home views load their data at MainMenu Start for the pre-entry bootstrap session. 'Accedi' changes the PlayFab account without reloading the scene and only RebindPhoton resets services, so the Posta and Premi badges and data stay those of the bootstrap session (device or guest account) until each view is reopened. Only HomeV2Integration reloads on authUI.OnLoginSuccess.
  - `Assets/Scripts/UI/UI51MailView.cs:89, 108-117, 142` Start -> Load (waits for OnAuthReady only), sets the mail badge.
  - `Assets/Scripts/UI/UI51RewardsView.cs:89, 102` Start -> FromServer, sets the rewards badge.
  - `Assets/Scripts/Auth/AuthBootstrapper.cs:196-209` RebindPhoton resets RewardsService and ModerationService but notifies no view.
  - `Assets/UIV2/Scripts/Core/HomeV2Integration.cs:88-92` The only view that reloads on authUI.OnLoginSuccess / OnRegistrationSuccess.
- **session-state** (Moderation (guest reports), XP/rewards gating, Rejoin after restart): HasRealLogin is a device PlayerPrefs flag used as if it described the current session. Before entry, the bootstrap session counts as 'registered' (ModerationService.Registered, Friends, Chat). After a failed login, 'Accedi come ospite' (ForceGuestIdentity) keeps playing on the bootstrap PlayFab account. If that account is a device-linked real account, the 'guest' carries a real PlayFabId, published as the guest id 'og' and therefore reportable or sanctionable under the real account.
  - `Assets/Scripts/Auth/PlayFabAuthService.cs:74, 121-128, 161` Device flag; ForceGuestIdentity clears only the name and flags, not the session.
  - `Assets/Scripts/Auth/ModerationService.cs:54-57` Registered = IsLoggedIn && HasRealLogin; Owner derived from it.
  - `Assets/Scripts/Auth/AuthBootstrapper.cs:165-167` Guest look publishes PlayFabAuth.PlayFabId as the guest id.
  - `Assets/UIV2/Scripts/Core/StartScreenV2.cs:38-42` PlayAsGuest keeps the bootstrap session.
- **static-singleton-cache** (Segnala giocatore (moderation), SO1): Quick-profile 'Richiesta inviata' and 'Segnalazione inviata' states are static, per app run, keyed only by the target id. After logout and login with another account on the same phone, the new account sees the previous account's added or reported state and cannot report that player.
  - `Assets/Scripts/UI/QuickProfileCard.cs:57, 141-144, 164-167` static s_Added and s_Reported, never cleared.
- **event-subscription-leak** (Private room join errors): RoomFlowV2 subscribes a lambda to the DontDestroyOnLoad MatchmakingManager.OnJoinFailed and never removes it. Each MainMenu reload adds one more handler that writes into a destroyed RoomFlowV2. Harmless today, but it grows per match.
  - `Assets/UIV2/Scripts/Core/RoomFlowV2.cs:175, 471-479` OnJoinFailed += lambda; OnDestroy does not remove it.
  - `Assets/Scripts/Networking/MatchmakingManager.cs:43-52` Singleton with DontDestroyOnLoad.
- **other** (All BUILD 3 two-device multiplayer items, P4, L1): Build version gates matchmaking. PUN (confirmed) and probably Chat use Application.version as AppVersion; bundleVersion went from 2.64 to 1.0.0 on 04/10. Any two-phone test with mixed builds (an Android APK from before ab9eb1c next to TestFlight 1.0.0) cannot find each other's rooms: invites and codes fail with 'Codice non valido'. Rejoin, timer, forfeit and presence results from such tests are invalid.
  - `Assets/Scripts/Auth/PhotonAuthConnector.cs:47-52` AppVersion = Application.version.
  - `Assets/Scripts/Auth/FriendsChat.cs:146` Chat Connect(AppId, Application.version, ...).
  - `ProjectSettings/ProjectSettings.asset:143, 175` bundleVersion 1.0.0 vs AndroidBundleVersionCode 264.
- **design-gap** (Lobby, Amici, Classifica, Tavolo banners, SO2): Other players' avatars are never real anywhere: lobby and search use a per-slot portrait, Amici and Classifica use a PlayFabId hash, and the table uses a per-seat portrait. The chosen avatar is not in LookProps or the PlayFab profile, so 'avatar not updated' reports in any screen share this cause.
  - `Assets/Scripts/Auth/AuthBootstrapper.cs:174-189` LookProps without an avatar.
  - `Assets/UIV2/Scripts/Core/RoomFlowV2.cs:367, 371, 440` Portrait by slot.
  - `Assets/Scripts/UI/UI51FriendsView.cs:255-263` Hash portrait, also used by Classifica.

## Statistiche / profilo / trofei / avatar online / emoticon ospite

### ST1 — root_cause_confirmed · verifier: partially_confirmed · size M

**Current behaviour.** The stats shown on the main profile, in the quick profile, on the results screen and in Home can differ from the server values. This happens right after a match, after a login and after logout/login. Restarting or logging in again repairs them because both paths reload the profile from PlayFab.

**Root cause.** There are three confirmed defects. All three come from the same flaw: the client copy of the profile is not tied to an account and is not resynced from the server.
(1) ProfileService keeps one cache for the whole app session. LogoutAndRestart and RebindPhoton reset RewardsService and ModerationService, but nothing ever clears ProfileService. LoadProfile never sets IsLoaded back to false at the start of a load and has no owner check or generation check. Result: between an account switch and the end of the next load, every consumer except HomeV2Integration (which has its own CloudReady/profileOwnerId guard) reads the previous account's stats with IsLoaded=true. The affected consumers are the quick profile (via HasRealProfile), the Photon look/stats props (PublishLook, which fires on OnDisplayNameChanged at login), the trophy summary and page, MatchResultsV2.xpFrom and BlockList. HYPOTHESIS, timing-dependent: a guest or device-account LoadProfile already in flight can finish after the new account's load. Its responses then overwrite the cache, and its OnProfileLoaded makes HomeV2 treat the stale data as the new account's (CloudProfileLoaded sets profileOwnerId to the current PlayFabId).
(2) After a match, the cache is updated only from the premioPartita response (ApplyServerStats) and only when r.statistiche is true. Several paths skip the update and nothing re-reads the server until the next LoadProfile: the error callback (network failure, timeout, CloudScript error), the early returns (guest account, suspended, more than 60 results per day) and an older deployed revision that does not return 'statistiche'. On the error path the results XP row keeps the client estimate (+40), PlayerProgressLocal still records that XP, and the profile does not change. This is exactly 'subito dopo una partita' that 'heals after restart'.
(3) Whenever the cloud profile is not ready, Home, the profile and the own seat level fall back to PlayerProgressLocal. That fallback covers the reload after login and any LoadProfile where one of its 3 calls failed: IsLoaded then stays false and nothing retries. PlayerProgressLocal is device-wide PlayerPrefs (progress_exp) that adds up the client XP estimates of every account that ever played on the phone, so after login or logout/login the level and XP are wrong.
There is also a minor inconsistency: trophies use the server 'Level' stat while every other screen uses PlayerXp.LevelOf(XP).

**Evidence.**
- `Assets/Scripts/Auth/ProfileService.cs:49,71-102` IsLoaded is set only at completion (line 84: IsLoaded = !hasError). There is no reset at the start, no generation or owner guard, and no Reset() method anywhere in the class.
- `Assets/Scripts/Auth/ProfileService.cs:215-225` ApplyServerStats returns early unless r.statistiche is true. It is the only in-session stats update.
- `Assets/Scripts/Auth/AuthBootstrapper.cs:196-209,268-302` RebindPhoton and LogoutAndRestart call RewardsService.Reset() and ModerationService.Reset() but never reset Profile.
- `Assets/Scripts/Auth/AuthBootstrapper.cs:97-99,151,159-168` PublishLook runs on OnDisplayNameChanged (fired by the login itself). HasRealProfile = IsLoaded && HasRealLogin, so the previous account's frame, level and stats are published with the new PlayFabId until the reload finishes.
- `Assets/Scripts/Auth/AuthBootstrapper.cs:452-457` Step 4 LoadProfile runs after Photon connects without being awaited. It can overlap HomeV2Integration.ReloadAccountProfile.
- `Assets/Scripts/Auth/AuthUIController.cs:286-291` Login is blocked only while Initializing or LoggingInPlayFab, so it can start while the boot flow is getting the Photon token or connecting Photon.
- `Assets/UIV2/Scripts/Core/HomeV2Integration.cs:98,169-170,265-279` Only Home has an ownership guard (profileOwnerId + loadingProfile). Line 98 adopts a stale cache after the logout scene reload.
- `Assets/UIV2/Scripts/Core/HomeV2Integration.cs:292,313-316` Registered user with the cloud not ready: XP and level come from PlayerProgressLocal.Exp.
- `Assets/Scripts/Auth/PlayerProgressLocal.cs:30-32,44,89-111` Keys progress_exp, progress_wins and progress_totalGames are device-wide and never scoped per PlayFabId.
- `Assets/Scripts/UI/PlayerBannerManager.cs:196-205,445-452` Own quick profile uses auth.Profile under HasRealProfile only, with no owner check. Own seat level falls back to PlayerProgressLocal.Level.
- `Assets/UIV2/Scripts/Core/MatchResultsV2.cs:310-324` xpFrom comes from cloud.IsLoaded with no owner check. local.RecordGameResult adds the estimated XP. The error callback (line 323) only shows the coins failure: it does not correct the XP row and does not resync the stats.
- `Server/CloudScript/51.js:403-455` premioPartita returns without 'statistiche' for a guest (406), a suspended player (410) and the 60-results-per-day limit (415). Only the full path returns statistiche:true (454).
- `Assets/Scripts/UI/UI51TrophiesView.cs:50` Trophies use p.Level (server 'Level' stat). Home and the quick profile use PlayerXp.LevelOf(XP).
- `Assets/Scripts/UI/UI51TrophySummary.cs:168` Same p.Level usage.

**Fix sketch.** Reuse the existing Reset pattern; no new system.
(a) ProfileService: add a private int loadGen. LoadProfile does `int gen = ++loadGen; IsLoaded = false;` and every callback (and CheckComplete) returns early when gen != loadGen, so a superseded load can no longer write the cache or fire OnProfileLoaded. Add `public void Reset() { loadGen++; IsLoaded = false; _statisticsCache.Clear(); _playerDataCache.Clear(); DisplayName = null; LevelUpFrom = LevelUpTo = 0; }`. Call Profile.Reset() next to RewardsService.Reset()/ModerationService.Reset() in AuthBootstrapper.RebindPhoton and LogoutAndRestart.
(b) Add `public void RefreshStatistics()`, a thin public wrapper over the existing private LoadStatistics that fires OnProfileUpdated (and NoteLevelUp) on success. MatchResultsV2 calls it in the premioPartita error callback and when !r.statistiche.
(c) Trophies use PlayerXp.LevelOf(profile.XP) instead of profile.Level (UI51TrophiesView and UI51TrophySummary, one line each).
(d) PlayerProgressLocal fallback for registered users: needs a user decision (see needs_user_decision).

**Files.** Assets/Scripts/Auth/ProfileService.cs, Assets/Scripts/Auth/AuthBootstrapper.cs, Assets/UIV2/Scripts/Core/MatchResultsV2.cs, Assets/Scripts/UI/UI51TrophiesView.cs, Assets/Scripts/UI/UI51TrophySummary.cs, Assets/UIV2/Scripts/Core/HomeV2Integration.cs

**Risk.** While a reload is running, IsLoaded=false makes every IsLoaded consumer show its 'not loaded' state for a moment: Home shows '—', PublishLook sends null props so others briefly see the default frame, and the quick profile shows no stats. That is already the behaviour when a load fails. BlockList.Current() then keys on '<id>?' and reads the list again once loaded, which is the desired effect. MatchResultsV2.RecordMatch ending during a reload uses the local xpFrom estimate (rare). UI51ProgressTests calls ApplyServerStats: keep its signature unchanged. ProfileV2BindingTests builds a ProfileService: Reset() must not break SetCosmetics.

**Decision.** While the cloud profile is loading or failed to load, should a registered user see (a) no level/XP ('—', the current 'Progressi non disponibili' state), or (b) a last-known value stored per PlayFabId? Today it shows the device-wide PlayerProgressLocal total, which is wrong for every account but the first one on the phone.

**Test plan.** EditMode: (1) after Reset(), IsLoaded is false and TotalGames, XP and Blocked are back to their defaults. (2) Extend UI51ProgressTests: ApplyServerStats followed by Reset() clears LevelUpFrom/To. (3) Trophy level comes from LevelOf(XP). Play Mode in Editor: log in as account A, log out, log in as account B, check Profile, trophy summary and quick-profile stats against Game Manager. Turn the network off at the end of a match: the results XP row and Home must match the server after RefreshStatistics. Device (TestFlight): two accounts on one phone, 5 quick logout/login cycles with password autofill. The race and real network failures do not reproduce in the Editor.

**Verifier (partially_confirmed).** I checked each mechanism in the code. (1) There is no reset on account switch: ProfileService.cs:71-102 sets IsLoaded only in CheckComplete (line 84), there is no Reset(), and AuthBootstrapper.cs:196-209 and 268-302 reset only RewardsService and ModerationService. The diagnosis overstates how far account A's data really leaks into account B. Logout always goes through a guest boot first (LogoutAndRestart -> StartAuthentication -> LoginAsGuest -> step-4 LoadProfile at AuthBootstrapper.cs:453), and that boot overwrites A's cache before the user can log in as B. A's data reaches B only if that guest load never runs or fails. The stale look at login (PublishLook through OnDisplayNameChanged) also happens only when HasRealLogin was already 1, because PlayFabAuthService.cs:367 fires OnDisplayNameChanged before line 371 sets HAS_REAL_LOGIN. After a logout HasRealLogin is 0 at that moment, so nulls are published, not A's look. In practice the 'previous identity' is the throwaway device-CustomID account on cold start, or the guest session. HomeV2Integration.cs:98 does adopt the stale cache after the logout reload, but it has no visible effect: with HasRealLogin=0, RefreshProfile (lines 289-316) ignores cloud data. (2) The error callback at MatchResultsV2.cs:323 has no resync: confirmed. The early returns (guest 51.js:406, suspended 410, results limit 415) do NOT create a stats mismatch, because the server did not write stats either, and ServerXp (MatchResultsV2.cs:335) already corrects the row to +0 when statistiche is false. Stats go stale only when the server committed but the client lost the response, or when an old CloudScript revision is deployed (still unknown). (3) The device-wide PlayerProgressLocal fallback is confirmed: PlayerProgressLocal.cs:30-32 and 44, HomeV2Integration.cs:292, PlayerBannerManager.cs:450-452. So does the lack of retry after a failed load (HomeV2Integration.cs:271-278, AuthBootstrapper.cs:453). The trophy Level vs LevelOf(XP) point is confirmed (UI51TrophiesView.cs:50 and UI51TrophySummary.cs:44). The diagnosis cites UI51TrophySummary line 168, but the file has 82 lines.

**Corrected cause.** The client's stats copy is refreshed only by a full LoadProfile at login or boot and by the premioPartita response. Nothing retries or resyncs after that. The three confirmed contributors: (a) a lost premioPartita response after the server committed, or an old deployed revision with no 'statistiche', leaves the cache stale until the next restart or login; (b) while a load is running, or for the whole session after a failed one, Home, the own-seat level and the profile fall back to device-wide PlayerProgressLocal XP; (c) a partial load failure writes part of the new account's data into the cache but fires no event (OnProfileLoaded only on full success, ProfileService.cs:84-89), so event-driven views keep the previous identity's values while HasRealProfile is false. A cross-account leak from A to B needs the intermediate guest load to have failed or never run. On cold start, the identity whose data is shown in the meantime is the unlinked device-CustomID account (PlayFabAuthService.cs:161), which has zero stats and default cosmetics.

- `Assets/Scripts/Auth/PlayFabAuthService.cs:365-372` OnDisplayNameChanged (which triggers PublishLook) fires before HAS_REAL_LOGIN is set to 1. After a logout the login publishes nulls plus 'og' = the real account's PlayFabId, not account A's look.
- `Assets/UIV2/Scripts/Core/MatchResultsV2.cs:335` ServerXp with statistiche=false sets from=to=xpFrom, so the row already shows +0 for guest/suspended/limit. Those paths are consistent with the server, not stale.
- `Assets/Scripts/Auth/ProfileService.cs:263,297,84-89` Each sub-load clears and rewrites its own cache on success, but events fire only if all 3 succeed. A partial failure silently changes the data without notifying anyone.
- `Assets/UIV2/Scripts/Core/MatchResultsV2.cs:353-356` RecordAbandon sends MatchQuit to the server only if cloud.IsLoaded. When the profile failed to load, the local copy records the loss but the server never does, which is a stats mismatch.
- `Assets/UIV2/Scripts/Core/StartScreenV2.cs:33-34` A cold start always shows Login, so the device-account data loaded by the boot is never shown in Home. It is only published to Photon and fed to BlockList/TrophySummary until the reload.

**Fix concerns.** (a) The generation guard as sketched drops the superseded load's callbacks, onComplete included. HomeV2Integration.ReloadAccountProfile (lines 268-278) clears loadingProfile only inside onComplete. The boot's step-4 LoadProfile (AuthBootstrapper.cs:453) can start AFTER Home's reload: login is allowed during GettingPhotonToken and ConnectingPhoton (AuthUIController.cs:285), and RebindPhoton's disconnect can make the boot retry through HandleError. In that case Home's load is superseded, loadingProfile stays true for the whole session, CloudReady is false, Home stays on the local fallback and the profile editor silently does nothing. A superseded load must still invoke its onComplete (for example by chaining to the newest load), or Home must clear loadingProfile in CloudProfileLoaded. (b) Profile.Reset() in RebindPhoton runs after the PublishLook fired by OnDisplayNameChanged (PlayFabAuthService.cs:367 runs before onSuccess at AuthUIController.cs:311). It therefore does not prevent the stale publish at login; the reload's PublishLook still has to correct it. (c) Setting IsLoaded=false during a reload also blocks RecordAbandon's MatchQuit (MatchResultsV2.cs:355). That gate should be removed, because the server call does not need the local cache. (d) Calling RefreshStatistics on the premioPartita error path is harmless but fixes nothing when the error is 'Non collegato' (RewardsService.cs:148).

### ST2 — likely_cause · verifier: partially_confirmed · size S

**Current behaviour.** On the first opening of the Profile, the TROFEI section (and 'Vedi tutti') shows no or too few trophies. After a restart or logout/login they appear.

**Root cause.** The section binds from the local ProfileService cache. That cache only becomes correct through LoadProfile, which runs only at login and at restart, so every ST1 cause applies here. The most likely trigger: trophies earned in this session (e.g. 'Prima partita') when premioPartita gave no 'statistiche' or failed, or a login whose load was overwritten or failed. Restart and logout-login fix it because both run LoadProfile.
Two trophy-specific weaknesses, both confirmed by reading the code:
(a) UI51TrophySummary subscribes to OnProfileLoaded/OnProfileUpdated only if AuthBootstrapper.Instance is non-null at OnEnable, and it never retries. AuthBootstrapper sits in the same MainMenu scene (GameObject 'AuthSystem', execution order 0 like the summary), and the summary's Account group is active in the saved scene. On a cold launch with HasRealLogin=1 the group is never toggled, so if OnEnable runs before AuthBootstrapper.Awake the summary stays unsubscribed for the whole session. HYPOTHESIS: Awake/OnEnable order between root objects is undefined.
(b) UI51TrophiesView.Open reads the stats once and never listens for updates, so a page opened while a load is running stays stale until reopened.
Neither summary nor page checks IsLoaded or the owner, so they can show the previous account's trophies.

**Evidence.**
- `Assets/Scripts/UI/UI51TrophySummary.cs:152-157` Profile captured in OnEnable; subscribes only if Instance != null; no retry.
- `Assets/Scripts/UI/UI51TrophySummary.cs:164-168` Binds from the cache with no IsLoaded or owner check.
- `Assets/Scripts/UI/UI51TrophiesView.cs:46-59` One-shot bind on Open; no subscription to profile events.
- `Assets/Scenes/MainMenu.unity` Parsed: ProfileScreenV2/Viewport/UI51/Account/Trophies is active (m_IsActive 1) at load. AuthBootstrapper is on root 'AuthSystem' in the same scene.
- `Assets/Scripts/Auth/AuthBootstrapper.cs.meta` executionOrder 0. UI51TrophySummary.cs.meta also 0 and neither has [DefaultExecutionOrder], so the order is undefined.
- `Assets/UIV2/Scripts/Screens/ProfileScreenV2.cs:79` accountGroup.SetActive(!IsGuest) toggles only on a guest/account change, so the summary's OnEnable may run only once per scene.
- `Assets/UIV2/Scripts/Core/MatchResultsV2.cs:320-323` No stats resync when premioPartita fails (see ST1).

**Fix sketch.** Ships with the ST1 fix (Reset + generation + RefreshStatistics). On top of it:
(1) UI51TrophySummary: add `private void Start() { if (profile == null) OnEnable(); }`, which retries the subscription once the scene's Awakes are done. OnEnable's own guard stops a double subscription because it re-reads Instance; alternatively move the subscription into Bind's lazy getter.
(2) UI51TrophiesView: in Open, subscribe Bind-all to profile.OnProfileLoaded/OnProfileUpdated and unsubscribe in OnDisable. This mirrors the summary.
(3) Optional: both views show the 'loading' state (empty) while !profile.IsLoaded instead of previous-account data.

**Files.** Assets/Scripts/UI/UI51TrophySummary.cs, Assets/Scripts/UI/UI51TrophiesView.cs

**Risk.** Low. Watch for a double subscription if OnEnable and Start both subscribe: guard on a field. UI51TrophiesView.Open is also the 'Vedi tutti' handler: unsubscribe in OnDisable, because the page root is deactivated by back.

**Test plan.** Play Mode in Editor: start with HasRealLogin=1, log in, open Profile and check that 'N / 15' matches the stats. Finish a match with the network off (premioPartita fails): after the ST1 resync the trophies update without a restart. Open 'Vedi tutti' immediately after login: the page refreshes when the load lands. Device: a fresh account plays its first match and opens Profile right away; expect 'Prima partita'. The Awake-order hypothesis is device/build-specific: add a one-time debug log in OnEnable when Instance == null to confirm it.

**Verifier (partially_confirmed).** The code facts hold. UI51TrophySummary.OnEnable (lines 28-33, not 152-157) subscribes only when AuthBootstrapper.Instance is non-null and never retries. Bind (lines 40-44, not 164-168) has no IsLoaded or owner check. UI51TrophiesView.Open (46-59) binds once. I re-parsed MainMenu.unity: Trophies, Account, UI51, Viewport, ProfileScreenV2, ScreenHost, SafeArea and UIV2_Home are all active at load. UI51Trophies (the page) is inactive. AuthBootstrapper is on root AuthSystem in the same scene, and both .meta files have executionOrder 0. UIV2Pager only moves pages and never deactivates them (UIV2Pager.cs:64-82). The Awake-order hypothesis does not match the reported symptom, though. It applies only to a cold start with HasRealLogin=1, and Unity's undefined order is in practice stable for a given build and scene, so a restart would repeat the failure instead of fixing it. Only logout-login fixes it under that hypothesis, because ProfileScreenV2.cs:79 toggles accountGroup (guest, then account) and OnEnable re-subscribes. A match also fixes it: MainMenu reloads with the DDOL Instance already set. The explanation that fits both 'after restart' and 'after logout-login' is the ST1 in-session resync gap (no stats after a lost or old-revision premioPartita response). Another fit is a partial reload failure: LoadStatistics writes B's stats but no OnProfileLoaded fires (ProfileService.cs:84-89), so the subscribed summary keeps showing the previous identity's 0 stats.

**Corrected cause.** The most likely cause is that the summary's data source is not resynced in-session (ST1: the premioPartita response is lost after the server committed, or the deployed revision returns no 'statistiche'). It can also be a reload that partly failed: the cache changed but no event fired, and Bind runs only on OnProfileLoaded/OnProfileUpdated. Both explain why restart and logout-login fix it, since both run a full LoadProfile. The Awake-order subscription gap is real code fragility, but it explains at most the logout-login half of the report, not the restart half.

- `Assets/Scripts/UI/UI51TrophySummary.cs:28-33,40-44` Correct line numbers (the file has 82 lines). Level comes from profile.Level at line 44.
- `Assets/UIV2/Scripts/Core/UIV2Pager.cs:64-82` Pages are never deactivated, so the summary's OnEnable runs once at scene load unless accountGroup toggles.
- `Assets/Scripts/Auth/ProfileService.cs:84-89,263` Statistics are rewritten on success even when another call fails, and no event fires.
- `Assets/Scenes/MainMenu.unity` Hierarchy walk: Trophies(1) <- Account(1) <- UI51(1) <- Viewport(1) <- ProfileScreenV2(1) <- ScreenHost(1) <- SafeArea(1) <- UIV2_Home(1). UI51Trophies(0).

**Fix concerns.** The Start() retry is safe: all scene Awakes run before any Start, and OnEnable re-reads Instance, so there is no double subscription. The UI51TrophiesView subscription has to be removed in OnDisable, because back sets the page inactive. Neither change fixes the partial-load case. If Bind should also react to partial updates, ProfileService should fire OnProfileUpdated when any sub-load succeeds, or the views should show a loading state while !IsLoaded (as the diagnosis proposes).

### ST3 — code_looks_correct_needs_device_test · verifier: confirmed · size XS

**Current behaviour.** The user reports stretched or deformed trophy icons.

**Root cause.** Not found in the code or the scenes. Every trophy and medal Image is built with preserveAspect=true. The working-tree scenes and commit ab9eb1c (closest to the iOS BUILD 3) both have m_PreserveAspect: 1 on all of them: Trophies page cards and detail, Profile summary slots, quick-profile medals, round-results rows. The medal PNGs are near-square (105x108, alpha bbox 97x100). The UI51_Common atlas has rotation and tight packing off. DesignCanvasFit scales uniformly. Inside layout groups the medal rect is wider than tall (e.g. 94x50 in a trophy card), which only looks right because of preserveAspect. So whatever screen shows deformed trophies is either not one of these, or something at runtime drops preserveAspect. No such runtime code was found: BindCard and Fill only set sprite and color.

**Evidence.**
- `Assets/UI51/Editor/UI51Build.cs:196-203` Image() helper defaults preserveAspect=true.
- `Assets/UI51/Editor/UI51ProgressBuilder.cs:288-289,327,370` Detail medal, card medal and summary slot medal use the default (preserve).
- `Assets/UI51/Editor/UI51TableBuilder.cs:1210` Quick-profile medals: 34x34 with the default (preserve).
- `Assets/UI51/Editor/UI51ResultsBuilder.cs:120` Results row icons (medal_sun, medal_trophy, medal_club) preserve unless they are a card.
- `Assets/Scenes/MainMenu.unity` Parsed: all 20 medal Images under UI51Trophies and ProfileScreenV2/.../Trophies have m_PreserveAspect 1. Same in git show ab9eb1c.
- `Assets/Scenes/GameScene.unity` Parsed: UI51QuickProfile Medal0-3 and RoundResults Row3-5/Icon have m_PreserveAspect 1.
- `Assets/UI51/Atlases/UI51_Common.spriteatlasv2.meta` enableRotation 0, enableTightPacking 0.
- `Assets/UIV2/Scripts/Core/DesignCanvasFit.cs:26-29` Uniform localScale.
- `Assets/Scripts/UI/UI51TrophiesView.cs:72-87` Runtime rebinding changes only sprite and color.

**Fix sketch.** Get a device screenshot first to identify which screen it is. If it is one of the screens above, check live with Unity-MCP for a scene drift (the 2.65 working tree has a 17.9k-line MainMenu diff). Otherwise the fix is a one-flag change in that screen's builder (preserveAspect=true, or size the rect from the sprite aspect as UI51MailView.SetIcon does). Then re-run that builder. Never hand-edit the YAML.

**Risk.** None until the screen is identified.

**Test plan.** Device or Simulator: open Profile > TROFEI, 'Vedi tutti', the detail sheet, the quick profile with medals, and the end of a round. Ask the tester for a screenshot of the deformed icon. ui-verify skill in the Simulator (not the Game view) on iPhone SE and 12.

**Verifier (confirmed).** I re-verified the scenes myself. MainMenu.unity has 20 'Medal' Images (medal_club, sun, sword and trophy sprites) and an 'Icon' using ic_trophy_cream, all with m_PreserveAspect 1 and m_Type 0 (Simple). GameScene.unity has Medal0-3, three RoundResults 'Icon' rows and two 'Crown' images, all preserveAspect 1 and Simple. The builder helper UI51Build.Image (UI51Build.cs:196-203) forces type Simple and preserveAspect true by default. No builder disables preserveAspect for a medal or trophy sprite: every preserveAspect=false call is a background, a hit area, a line or card art. UIAnim.Pop and SheetUp animate a uniform Scale track, and UI51TrophiesView.BindCard changes only sprite and color. The other users of medal sprites, UI51Mail (UI51MailView.SetIcon sizes the rect from the sprite aspect, lines 248-253) and UI51News (TagStyle.Icon is assigned but never applied to an Image), cannot stretch them either. I found no deformation mechanism in code or scene data, so a device screenshot is needed.

- `Assets/Scripts/UI/UI51MailView.cs:248-253` Mail medal icons take their height from the sprite aspect.
- `Assets/UI51/Scripts/Anim/UIAnim.cs:255-286` Pop, PopDialog, SheetUp and FadeIn use a uniform Scale or Alpha. No per-axis scale could leave an icon squashed.

**Fix concerns.** None until the screen is identified.

### ST4 — feature_missing · verifier: confirmed · size M

**Current behaviour.** After changing the avatar, the other players at the table, in the sorteggio, in the results and in the quick profile never see it. They see a fixed DragonsHoard portrait per seat index. The local player also sees that seat portrait on their own banner. Frame, banner and level do propagate right away.

**Root cause.** The chosen avatar id (PlayFab user data 'AvatarId') is never published to Photon. LookProps carries fr/bn/lv/gp/gw/sc/id/og but no avatar. Every GameScene surface draws seatAvatars[p % n]: the banner (RefreshUI51), and SeatAvatar() used by the quick profile, sorteggio, results and moments. Those sprites come from the DragonsHoard Avatars.png sheet, not the 8 UI51 profile avatars. In the lobby and matchmaking only the local seat uses HomeV2Integration.LocalAvatar; the others use per-slot placeholders. The timing is already handled: SetCosmetics fires OnProfileUpdated, which calls PublishLook (cached out of room, sent on join, republished in OnJoinedRoom). Only the data is missing. This is the known 'SelectedAvatarId' debt from Fase 15 memory (ranking and friends use a hash placeholder for the same reason).

**Evidence.**
- `Assets/Scripts/Auth/AuthBootstrapper.cs:174-190` LookProps has no avatar key.
- `Assets/Scripts/Auth/ProfileService.cs:34,57,163-176` AvatarId saved and cached; SetCosmetics fires OnProfileUpdated, which triggers PublishLook.
- `Assets/Scripts/UI/PlayerBannerManager.cs:372,395-400` SeatAvatar and RefreshUI51 use seatAvatars[p % length] for every seat, own seat included.
- `Assets/Scenes/GameScene.unity` PlayerBannerManager.seatAvatars = 4 sub-sprites of DragonsHoard Avatars.png, not UI51/Art/Avatars av_*.
- `Assets/Scripts/UI/SorteggioView.cs:70,107` Uses banners.SeatAvatar.
- `Assets/Scripts/UI/UI51ResultsView.cs:319,385` Uses banners.SeatAvatar.
- `Assets/Scripts/UI/UI51TableMoments.cs:163` Uses banners.SeatAvatar.
- `Assets/UIV2/Scripts/Core/RoomFlowV2.cs:367,440` Other players get view.Portrait(slot) / SearchView.Portrait(slot) placeholders.
- `Assets/Scripts/Networking/MatchmakingManager.cs:417-422` PublishLook on every OnJoinedRoom: propagation timing is already correct.
- `Assets/Photon/PhotonRealtime/Code/Player.cs` Verified in the local PUN source: local SetCustomProperties out of room caches the values and sends them on join.

**Fix sketch.** Follow the existing look pattern.
(1) ProfileService: add `LookAvatarKey = "av"`.
(2) AuthBootstrapper.LookProps: add an optional `string avatar = null` parameter and the entry `{av, real ? avatar : null}`. PublishLook passes Profile.AvatarId.
(3) ProfileCosmetics: add `ReadAvatar(props)`, which accepts only a string of 1-32 chars.
(4) PlayerBannerManager: add `[SerializeField] Sprite[] profileAvatars`, wired by UI51TableBuilder from the same 8 UI51/Art/Avatars sprites as ProfileEditorV2.avatars. SeatAvatar(p) resolves: the local player with HasRealProfile uses Profile.AvatarId; a remote human uses ReadAvatar(props); in both cases a name match in profileAvatars wins, else the current seatAvatars[p]. RefreshUI51 calls SeatAvatar(p); its existing sprite-change cache handles late prop updates. One shared function covers banner, quick profile, sorteggio, results and moments.
(5) Lobby/matchmaking (optional, user decision): add a static HomeV2Integration.AvatarFor(id) delegating to profileEditor.AvatarFor, and use it in RoomFlowV2 for others' portraits.

**Files.** Assets/Scripts/Auth/ProfileService.cs, Assets/Scripts/Auth/AuthBootstrapper.cs, Assets/UIV2/Scripts/Data/ProfileCosmetics.cs, Assets/Scripts/UI/PlayerBannerManager.cs, Assets/UI51/Editor/UI51TableBuilder.cs, Assets/UIV2/Scripts/Core/RoomFlowV2.cs, Assets/UIV2/Scripts/Core/HomeV2Integration.cs, Assets/Tests/Editor/UI51TableTests.cs

**Risk.** Changing seat portraits changes the look of bots and guests too unless the fallback keeps seatAvatars for them, so keep the fallback. Builder change: re-run Build Fase 5 (unity-builder skill), which must preserve the Fase 6 steps. Players on old builds publish no 'av' and fall back to the seat portrait. The 2 tests calling LookProps (UI51TableTests 478-492) keep compiling thanks to the optional parameter. The ranking/friends hash placeholders stay as they are unless the user extends the scope.

**Decision.** Scope: table only, or also the lobby/matchmaking seat cards and ranking/friends (which would need reading others' public 'AvatarId' user data)? Should the local player's own table banner switch from the DragonsHoard seat portrait to the chosen avatar? Today nobody, including the player, sees their chosen avatar at the table.

**Test plan.** EditMode: LookProps(real) contains av; guest/null look clears av; ReadAvatar rejects non-strings and strings over 32 chars; SeatAvatar falls back for bots. Play Mode in Editor: local seat shows the chosen avatar (Editor is guest-only per memory, so stage with a fake loaded profile). Device, 2 phones: A changes the avatar, enters the lobby at once and starts a match; B must see it on A's banner, quick profile, sorteggio and results. Then A changes it mid-session between rematches.

**Verifier (confirmed).** LookProps (AuthBootstrapper.cs:174-190) has no avatar key. SeatAvatar (PlayerBannerManager.cs:372) and RefreshUI51 (395-400) use seatAvatars[p % length] for every seat, the local one included. GameScene.unity:26565-26569 wires seatAvatars to 4 sub-sprites of guid cb0a5e9a… = DragonsHoard Avatars.png. SorteggioView:70,107, UI51ResultsView:319,385 and UI51TableMoments:163 all go through SeatAvatar. RoomFlowV2:367,440 uses LocalAvatar only for the local seat. SetCosmetics fires OnProfileUpdated, which triggers PublishLook (ProfileService.cs:173-175, AuthBootstrapper.cs:98). Photon Player.SetCustomProperties out of a room merges locally (Player.cs ~408-416) and MatchmakingManager.cs:422 republishes on join, so only the avatar data is missing; the timing is already right.

**Fix concerns.** (a) Profile.AvatarId defaults to 'default' (ProfileService.cs:57), and ProfileEditorV2.AvatarIndex maps any unknown id to avatars[0] (ProfileEditorV2.cs:101-106), so Home shows av_0 for a player who never chose an avatar. The proposed table resolution ('name match wins, else seatAvatars[p]') would give that same player the DragonsHoard seat portrait at the table. Real profiles should use the same rule (unknown means avatars[0]), keeping seatAvatars only for bots, guests and old builds. (b) A changed avatar triggers AvatarFrame.SetAvatar again through the ui51Avatars cache, which is fine. Wiring the 8 UI51 avatar sprites in UI51TableBuilder requires re-running Build Fase 5, which must keep the Fase 6 steps (memory note).

### E1 — likely_cause · verifier: partially_confirmed · size S

**Current behaviour.** A guest does not see the opponent's emoticons at the table.

**Root cause.** There is no guest-specific gate on the receive path. The RPC goes to All (NetworkGameController.RPC_Emoticon → GamePresentation.ShowEmoticon → GameSocialV2.ShowEmoticon → banner). The only filter is EmoticonMute.IsMuted(PlayFabIdAt(player)), and that state is not scoped to the current identity, so it carries over. Confirmed mechanisms:
(1) The 'Silenzia emoticon' list is one device-wide PlayerPrefs key ('Social.MutedEmoticons'), never cleared and never scoped per account. A mute set while logged in persists into every later guest session on that phone. The guest cannot see or undo it, because guests get no quick-profile buttons (PlayerBannerManager.cs:216).
(2) IsMuted also checks BlockList. BlockList memoizes its set per PlayFabId, adding '?' only while the profile is not loaded. ProfileService.IsLoaded stays true from the previous account after logout (ST1), so a BlockList read in that window stores the previous account's 'Bloccati' under the guest's id for the whole session.
Other possibility: bots never send emoticons (only GameSocialV2.Send calls SendEmoticon). If the 'opponent' was a bot, this is working as designed.
Neither mechanism explains a guest on a clean device facing a human; that needs a device log.

**Evidence.**
- `Assets/Scripts/Networking/NetworkGameController.cs:130-146` RPC to RpcTarget.All; drops only invalid index, unknown seat or 1.5 s spam. No guest check.
- `Assets/UIV2/Scripts/Core/GameSocialV2.cs:161-177` The only filter is EmoticonMute.IsMuted(PlayFabIdAt(player)).
- `Assets/Scripts/Auth/FriendsService.cs:101-121` EmoticonMute: single device-wide PlayerPrefs key; IsMuted also returns true for BlockList.IsBlocked.
- `Assets/Scripts/Auth/FriendsService.cs:165-176` BlockList.Current memoizes by PlayFabId + ('?' only if !IsLoaded) and reads auth.Profile.Blocked.
- `Assets/Scripts/Auth/AuthBootstrapper.cs:268-302` Logout resets neither Profile nor EmoticonMute or BlockList state.
- `Assets/Scripts/UI/PlayerBannerManager.cs:216` A guest viewer gets PlayFabId=null, so no buttons and no way to unmute.
- `Assets/UIV2/Scripts/Core/GameSocialV2.cs:67-73` Emoticons are sent only by a human tapping; CirullaAI never sends any.

**Fix sketch.** Ships with E2 and ST1 (Profile.Reset makes BlockList key on '<id>?' during a reload).
EmoticonMute: scope the persisted key per account, `Key + "." + PlayFabId` for HasRealLogin; a guest only uses an in-memory session set (see E2). The old global key is either ignored, which drops existing mutes, or migrated once to the first account that reads it (user decision).
For the diagnosis, add a Debug.isDebugBuild log in GameSocialV2.ShowEmoticon when a received emoticon is dropped (muted, blocked, or no active banner).

**Files.** Assets/Scripts/Auth/FriendsService.cs, Assets/UIV2/Scripts/Core/GameSocialV2.cs

**Risk.** Changing the key drops the mutes that existing accounts have stored device-wide. That is acceptable only if the user agrees. Two places read IsMuted: QuickProfileCard labels and GameSocialV2. Both must use the same id resolution.

**Decision.** Was the opponent in the report a human online or a bot? Bots never send emoticons by design. Had the test phone previously been logged into an account that used Silenzia or Blocca?

**Test plan.** EditMode: a mute set as account A is not muted for account B or for a guest; a guest session mute does not touch PlayerPrefs. Device, 2 phones: (a) clean guest phone vs account phone, both send emoticons and check both directions; (b) account mutes the opponent, logs out, plays as guest vs the same opponent, and must see the emoticons; (c) read the new drop log in the Xcode console. The Editor cannot host 2 humans (memory: Editor = guest only).

**Verifier (partially_confirmed).** The receive path has no guest gate: NetworkGameController.cs:130-146 sends to RpcTarget.All and drops only an invalid index, an unknown seat or messages within the 1.5 s spam window. GameSocialV2.cs:161-177 filters only through EmoticonMute.IsMuted(PlayFabIdAt). Mechanism 1 is confirmed: 'Social.MutedEmoticons' is one device-wide PlayerPrefs key (FriendsService.cs:103-120). IsMuted ignores the current identity, logout never clears the key, and AccountDeletionService.cs:39-46 does not clear it either. Guests cannot undo a mute because PlayerBannerManager.cs:216 removes their buttons. Mechanism 2 (BlockList memo) exists in the code but the window the diagnosis describes is practically unreachable. The memo would need a BlockList read between the guest's PlayFab login and the end of its LoadPlayerData, and that gap is a few hundred ms on the start screen, before a match can exist. A more reachable path is a failed LoadPlayerData. _playerDataCache.Clear() runs only on success (ProfileService.cs:297), so the previous identity's 'Bloccati' stays in the cache with IsLoaded=false. BlockList then keys on '<guestId>?' and still reads the old list (FriendsService.cs:166-172), for the whole session. Bots never send emoticons (the only SendEmoticon caller is GameSocialV2.cs:71): confirmed. Nothing found explains a clean device facing a human.

**Corrected cause.** Confirmed: device-wide mutes set while logged into an account carry over to guest sessions on the same phone, and the guest cannot see or undo them. A failed LoadPlayerData also makes the previous identity's block list apply for the session. The narrow-window memo case is theoretical. A clean device facing a human is unexplained by the code and needs the proposed drop log.

- `Assets/Scripts/Auth/ProfileService.cs:294-314` On error, the previous identity's player data (Bloccati) is left in the cache.
- `Assets/Scripts/Auth/AccountDeletionService.cs:39-46` Social.MutedEmoticons and BloccatoNome_* are not in AccountPrefKeys, so they also survive account deletion.

**Fix concerns.** Scoping the key per PlayFabId drops existing mutes unless they are migrated. QuickProfileCard.RefreshActions (line 214) and GameSocialV2.ShowEmoticon must resolve the id the same way. If an account muting a guest target persists 'og' session ids, the PlayerPrefs list grows without bound with ids that never match again, so mutes of guest targets should stay session-only.

### E2 — feature_missing · verifier: confirmed · size S

**Current behaviour.** A guest who taps an opponent's banner gets a quick profile with no buttons, so they cannot mute that opponent's emoticons. Related confirmed bug: when an account mutes a GUEST opponent, the mute is stored but never applied.

**Root cause.** (1) Deliberate earlier rule: 'da ospite niente pulsanti'. OpenProfile clears view.PlayFabId when the local player has no real login, and QuickProfileCard hides all actions when playFabId is null. Guests cannot report (CloudScript segnala requires a Username) or block (BlockList is per-account PlayFab data), but nothing stops a local mute; one was just never offered.
(2) Bug: the quick profile stores a guest opponent's mute under their session id ('og', ProfileCosmetics.GuestId). ShowEmoticon checks only PlayFabIdAt(player), which reads the 'id' key and is null for guests. Muting a guest therefore has no effect.
(3) Persistence: EmoticonMute writes PlayerPrefs, so even if enabled for guests the mute would be permanent and device-wide, which the user explicitly does not want.

**Evidence.**
- `Assets/Scripts/UI/PlayerBannerManager.cs:209-217` Guest viewer → view.PlayFabId = null. Guest target → PlayFabId = GuestId (og), Guest=true.
- `Assets/Scripts/UI/QuickProfileCard.cs:71,98,135-140,202-217` actions hidden when playFabId is null; mute toggles EmoticonMute (PlayerPrefs).
- `Assets/UIV2/Scripts/Core/GameSocialV2.cs:151-155,165` PlayFabIdAt reads only the 'id' key, so a mute stored under a guest's 'og' id is never matched.
- `Assets/UIV2/Data/ProfileCosmetics.cs:65-70` GuestId reads 'og'. Actual path: Assets/UIV2/Scripts/Data/ProfileCosmetics.cs.
- `Assets/Scripts/Auth/FriendsService.cs:101-121` Mute persisted in device-wide PlayerPrefs.
- `Server/CloudScript/51.js:469` segnala refuses accounts without a Username, so report must stay hidden for guests.
- `Assets/UI51/Editor/UI51TableBuilder.cs:1213-1243` Actions = Row(Add, Added, Mute) + Bottom(Report, Block): Mute can be shown alone with no builder change.

**Fix sketch.** Runtime only, no builder change.
(1) QuickProfileCard.View: add `bool ViewerGuest`. PlayerBannerManager.OpenProfile: replace line 216 with `view.ViewerGuest = auth == null || auth.PlayFabAuth == null || !auth.PlayFabAuth.HasRealLogin;` and keep the id.
(2) QuickProfileCard.RefreshActions: when viewerGuest, hide addButton, addedLabel and blockButton, and hide reportButton.transform.parent so only 'Silenzia emoticon' remains. The mute click calls `EmoticonMute.SetMuted(id, !muted, persist: !viewerGuest)`.
(3) EmoticonMute: add a static HashSet<string> session. SetMuted with persist=false touches only that set, and IsMuted also checks it. Clear it in AuthBootstrapper.LogoutAndRestart/RebindPhoton (and at match end if the user wants per-match).
(4) GameSocialV2: add MuteIdAt(player) = ReadStats id ?? ProfileCosmetics.GuestId(props). ShowEmoticon uses it, which also fixes muting guest opponents for accounts.

**Files.** Assets/Scripts/UI/PlayerBannerManager.cs, Assets/Scripts/UI/QuickProfileCard.cs, Assets/Scripts/Auth/FriendsService.cs, Assets/UIV2/Scripts/Core/GameSocialV2.cs, Assets/Scripts/Auth/AuthBootstrapper.cs

**Risk.** With Bottom hidden the card gets shorter; Relayout already recentres it from the top edge. Check that QuickProfileCard's s_Added/s_Reported logic is skipped for viewerGuest. Bots still have no id, so no buttons, unchanged. Changes the 01/10 rule 'da ospite niente pulsanti' only for mute, as the report asks.

**Decision.** How long should a guest's local mute last: until the end of the match, or until the app is closed (the guest identity's lifetime)? Should guests also see a disabled 'Segnala' with 'Registrati per segnalare', or no button at all?

**Test plan.** EditMode: SetMuted(persist:false) does not write PlayerPrefs and IsMuted is true in-session; Reset/clear empties it; MuteIdAt returns og for a guest look and id for an account look (via LookProps like UI51TableTests 478-492). Play Mode in Editor: the guest quick profile shows only 'Silenzia emoticon' (staged opponent props). Device, 2 phones: the guest mutes the account and its emoticons stop; restart the app and the mute is gone; the account mutes the guest and the guest's emoticons stop (the regression for the og bug).

**Verifier (confirmed).** PlayerBannerManager.cs:213-216 marks a guest target (Guest=true, PlayFabId=og) and then nulls PlayFabId whenever the viewer has no real login. QuickProfileCard.Show (line 98) then hides all actions, and the mute handler (135-140) writes PlayerPrefs. The bug of muting a guest opponent is confirmed. The card stores the mute under 'og' (ProfileCosmetics.GuestId), but ShowEmoticon checks PlayFabIdAt, which returns only the 'id' key (GameSocialV2.cs:151-155, ProfileCosmetics.ReadStats). LookProps sets 'id' to null for guests (AuthBootstrapper.cs:186), so the mute is never applied even though the label switches to 'Riattiva emoticon'. segnala refuses accounts without a Username (51.js:469). Builder structure: Actions = Row(Add, Added, Mute) + Gap(8) + Bottom(Report, Block) (UI51TableBuilder.cs:1213-1243).

**Fix concerns.** Hiding Bottom leaves the 8 px 'Gap' spacer (UI51TableBuilder.cs:1236), so hide it as well or accept the extra space. Keep the target-guest flag ('guest', used for block and add) separate from the new viewerGuest flag in RefreshActions. The session mute set has to be cleared in EmoticonMute (Auth assembly), which AuthBootstrapper can reach directly. MuteIdAt must be used in both ShowEmoticon and the card's label, or the label and the behaviour diverge again.

### Missed by the investigator (found by the verifier)

- **ST1**: The proposed ProfileService generation guard (ST1 fix a) would leave HomeV2Integration.loadingProfile stuck at true. Home clears it only in LoadProfile's onComplete. The boot's unawaited step-4 LoadProfile can start after Home's ReloadAccountProfile, because login is allowed during GettingPhotonToken and ConnectingPhoton, and RebindPhoton's disconnect can make the boot retry through HandleError. The superseded Home load would then never complete: CloudReady stays false for the session, Home stays on the local fallback and the profile editor does nothing.
  - `Assets/UIV2/Scripts/Core/HomeV2Integration.cs:265-279` loadingProfile=false only inside the onComplete callback.
  - `Assets/Scripts/Auth/AuthBootstrapper.cs:453` The boot's LoadProfile runs after the Photon connect, so it can come after the login.
  - `Assets/Scripts/Auth/AuthUIController.cs:285` The login gate covers only Initializing and LoggingInPlayFab.
- **ST2**: A partial LoadProfile failure silently changes the cache. Each sub-load clears and rewrites its own dictionary on success, but OnProfileLoaded fires only when all 3 succeed, and OnProfileUpdated never fires. Event-driven views (TrophySummary, Home) keep the previous identity's values while the cache already holds part of the new data. A failed LoadPlayerData also keeps the previous identity's Bloccati, AvatarId and FrameId.
  - `Assets/Scripts/Auth/ProfileService.cs:84-89,263,297` Clear() runs per sub-load on success. Events fire only on full success.
- **ST1**: RecordAbandon sends the quit to the server (MatchQuit) only when the local profile is loaded. After a failed load, an abandon is recorded locally (PlayerProgressLocal) but never on the server, which is an extra source of stats mismatch.
  - `Assets/UIV2/Scripts/Core/MatchResultsV2.cs:353-356` cloud.IsLoaded gates RewardsService.MatchQuit.
- **ST1**: Login from a guest session publishes the new real account's PlayFabId as the guest id 'og' on Photon. PlayFabAuthService fires OnDisplayNameChanged, which triggers PublishLook, before it sets HAS_REAL_LOGIN, so HasRealProfile is false and LookProps puts the real PlayFabId in 'og' until the reload's PublishLook. Others see the account as a reportable guest with no look or stats in the meantime.
  - `Assets/Scripts/Auth/PlayFabAuthService.cs:365-372` OnDisplayNameChanged fires at 367, before HasRealLogin=1 at 371.
  - `Assets/Scripts/Auth/AuthBootstrapper.cs:165-167,188` The non-real branch passes PlayFabId as guestId.
- **ST1**: The cross-cutting claim that the forfeit cap mirror 'can give +0 XP that the server would pay' is overstated. The server computes XP independently, and ServerXp corrects the row when the response arrives. Only the PlayerProgressLocal fallback records 0.
  - `Assets/UIV2/Scripts/Core/MatchResultsV2.cs:304-309,333-339` The client estimate is zeroed, then replaced by the server's esperienza and xp.
- **E1**: Account deletion does not clear the device-wide emoticon mute list or the blocked-name cache, so the mutes survive into later guest sessions.
  - `Assets/Scripts/Auth/AccountDeletionService.cs:39-46` Social.MutedEmoticons and BloccatoNome_* are not listed.

### Cross-cutting notes

- **session-state** (ST1, ST2, E1, friends/ranking/rewards items using Profile, LevelUp screen): No reset on account switch. ProfileService (stats, user data including Bloccati and AvatarId, DisplayName, IsLoaded) is never reset on logout or on 'Accedi'. Only RewardsService and ModerationService are. The static ProfileService.LevelUpFrom/To also survives, so a LevelUp screen earned by account A can appear after logging in as B. QuickProfileCard's static s_Added/s_Reported survive too, so B sees 'Segnalazione inviata' for players only A reported.
  - `Assets/Scripts/Auth/AuthBootstrapper.cs:196-209,268-302` Resets RewardsService and ModerationService only.
  - `Assets/Scripts/Auth/ProfileService.cs:203` Static LevelUpFrom/LevelUpTo, never cleared on logout.
  - `Assets/Scripts/UI/UI51LevelUpView.cs:24-31` Pending uses the static LevelUpTo.
  - `Assets/Scripts/UI/QuickProfileCard.cs:57` Static per-session sets not keyed by viewer account.
- **playfab-login-lifecycle** (ST1, ST2, presence/online items, moderation owner logic): Returning registered users are logged in, before they type a password, into a different anonymous PlayFab account. With HasRealLogin=1 the boot flow calls LoginWithCustomID with a persistent device id (SystemInfo.deviceUniqueIdentifier) and CreateAccount=true. That CustomID is never linked to the email account: the project has no LinkCustomID call. Until the real login completes, HasRealLogin is true and the loaded profile belongs to that throwaway account, so HasRealProfile is true and PublishLook, BlockList and the quick profile read it. Separately, login is allowed while the boot flow is still getting the Photon token or connecting Photon, so the boot LoadProfile and HomeV2's ReloadAccountProfile run concurrently and their responses can interleave.
  - `Assets/Scripts/Auth/PlayFabAuthService.cs:157-180,444-469` HasRealLogin → GetOrCreateDeviceId → LoginWithCustomID CreateAccount=true.
  - `Assets/Scripts/Auth/AuthBootstrapper.cs:111-121,323-340,452-457` Boot login, then an unawaited LoadProfile.
  - `Assets/UIV2/Scripts/Core/StartScreenV2.cs:27-34` The login panel is always shown on a cold start.
  - `Assets/Scripts/Auth/AuthUIController.cs:286-291` Gate only covers Initializing and LoggingInPlayFab.
- **playerprefs-not-account-scoped** (ST1, E1, E2, collection/emoticon items, forfeit/abandon items): Several per-player values are device-wide PlayerPrefs, so they leak between accounts and guest sessions on the same phone: local XP/level fallback (progress_*), emoticon mute list (Social.MutedEmoticons), equipped emoticons (Collection.Emoticons), and the forfeit-XP daily cap mirror (Moderazione.AbbandoniPremiati). The last one means a second account on the phone can get +0 XP for a forfeit win that the server would pay.
  - `Assets/Scripts/Auth/PlayerProgressLocal.cs:30-32` progress_* keys.
  - `Assets/Scripts/Auth/FriendsService.cs:103` Social.MutedEmoticons.
  - `Assets/UIV2/Scripts/Core/CollectionCosmeticsV2.cs:19,28` Collection.Emoticons.
  - `Assets/UIV2/Scripts/Core/MatchResultsV2.cs:306-308` Moderazione.AbbandoniPremiati zeroes client XP per device.
- **async-load-vs-ui-refresh** (ST1, ST2, profile editor 'button does nothing' reports, quick profile/frame for others): Failed loads are never retried. LoadProfile reports success only if all 3 calls (profile, statistics, user data) succeed. On any failure IsLoaded stays false and nothing retries until the next login or restart. Everything gated on CloudReady/IsLoaded stays degraded for the whole session: Home stats '—', a device-wide XP fallback, Photon look published as null so others see the default frame, BlockList empty. The profile editor button also silently does nothing, because OpenProfileEditor returns when !CloudReady with no feedback.
  - `Assets/Scripts/Auth/ProfileService.cs:79-91` IsLoaded = !hasError; OnProfileLoaded only on full success.
  - `Assets/UIV2/Scripts/Core/HomeV2Integration.cs:142-150` OpenProfileEditor returns silently when !CloudReady().
  - `Assets/UIV2/Scripts/Core/HomeV2Integration.cs:271-278` Reload completion does not retry on failure.
- **resync-snapshot** (ST1, ST2, wallet/coins after match items): Stats and XP are resynced only from the premioPartita response and from LoadProfile at login. A lost response, a CloudScript error or an older deployed revision leaves the client copy stale for the whole session. The same pattern probably applies to wallet and rewards: WalletService.Refresh is called only when monete/gemme are > 0 or at profile load.
  - `Assets/UIV2/Scripts/Core/MatchResultsV2.cs:320-323` No resync in the error callback.
  - `Assets/Scripts/Auth/RewardsService.cs:154-165` WalletService.Refresh only when the reward is > 0.
- **design-gap** (ST4, ranking items, friends/invite items, lobby items): The chosen avatar is stored (PlayFab public user data 'AvatarId') but never shared. Every remote surface uses placeholders: table seat portraits, lobby/matchmaking seat cards, ranking rows (deterministic hash), friends rows (hash), invite banner. This is the open 'SelectedAvatarId' debt from Fase 15.
  - `Assets/Scripts/UI/UI51RankingView.cs:134` Own avatar uses LocalAvatar, others use the PortraitFor hash.
  - `Assets/Scripts/UI/UI51FriendsView.cs:222,253,390` Friends and invites use the PortraitFor hash.
  - `Assets/UIV2/Scripts/Core/RoomFlowV2.cs:367,440` Lobby placeholders for other players.

## Ricompense / posta / pallino rosso

### M1 — likely_cause · verifier: partially_confirmed · size S

**Current behaviour.** Premi: RISCATTA (or today's tile) sends riscattaPremio once and the page flips to TORNA A GIOCARE when the server answers. While the request is in flight the button stays interactable and still plays its press animation. Only the label changes to the ellipsis. On days with a chest (day 5 green, day 7 purple, and Posta gifts with a forziere) a full-screen chest page opens. The player must tap the chest itself, then tap RACCOGLI. The 'TOCCA PER APRIRE' text below the chest is not a hit area. On day 7 the big grand-prize card cannot be tapped either.

**Root cause.** Two causes, both confirmed in the code. Which one the tester hit has to be checked on a device. (a) The chest flow needs 2 extra taps by construction. UI51ChestView.Open never reveals the chest by itself, so the player taps once to reveal and once for RACCOGLI. The button covers only the 220x191 chest rect (y 289..480 in the 390x844 layout). The 'TOCCA PER APRIRE' label at y 530 invites a tap but has no button, so a tap on the label does nothing and a second tap is needed. (b) The claim button never shows a busy state. UI51Build.Button uses Transition.None, and Claim() only sets the label to the ellipsis and a private 'claiming' flag. A legacy CloudScript round trip (0.5-2 s) looks like a tap that did not register. The player taps again and the second tap is ignored silently, so it 'took two taps'. Also checked and ruled out: the drag threshold is scaled by DPI at runtime (UIV2MotionInstaller), and the Premi page has no ScrollRect. The PlayFab 2000 ms RequestTimeout does not apply, because the transport is PlayFabUnityHttp and it sets no timeout. Minor: the day-7 grand card has no Button.

**Evidence.**
- `Assets/Scripts/UI/UI51ChestView.cs:28-29,33-54,56-70` chestButton->Reveal and collect->close; Open() leaves the chest closed until it is tapped
- `Assets/UI51/Editor/UI51ProgressBuilder.cs:455-462` hit area is only the 'Chest' rect (195,385 220x191); the 'TOCCA PER APRIRE' label at TopBand 530 has no Button
- `Assets/UI51/Editor/UI51ProgressBuilder.cs:476-479` RACCOGLI is the second required tap
- `Assets/Scripts/UI/UI51RewardsView.cs:190-210` Claim(): guard claimed||claiming, label set to the ellipsis, button stays interactable; chest.Open(r) at 202
- `Assets/UI51/Editor/UI51Build.cs:208-217` every UI51 Button has Transition.None, so interactable=false shows no change by itself
- `Assets/UI51/Scripts/Components/UI51Press.cs:17-18,35,58` the press animation still plays while interactable; the dimmed disabled look needs m_DisabledGroup, which GoldBody never wires
- `Assets/UI51/Editor/UI51SocialBuilder.cs:338-344,858-887` only days 1-6 are UI51RewardDay buttons; the Grand card has no Button
- `Assets/UIV2/Scripts/Animations/UIV2MotionInstaller.cs:27,35` pixelDragThreshold scaled by DPI, so the drag threshold is ruled out
- `Assets/PlayFabSDK/Shared/Public/PluginManager.cs:89-96` transport is PlayFabUnityHttp (the asset has RequestType 2, not HttpWebRequest); PlayFabUnityHttp.Post (lines 137-165) sets no timeout, so a client timeout is ruled out

**Fix sketch.** Smallest fix, in two parts. (1) UI51ChestView.Open: after the pop, call Reveal() automatically, e.g. DOVirtual.DelayedCall(0.45f, Reveal).SetLink(gameObject, KillOnDisable), and hide the 'TOCCA PER APRIRE' label. RACCOGLI is then the only tap. Posta and Premi both route through this view, so one change fixes both. (2) UI51RewardsView.Claim: set claim.interactable=false and every days[i].button.interactable=false before the call. In the error callback set claim.interactable=true; BindDay already restores the tiles on Render. Re-enable claim in Render, because claim.SetActive(!claimed) already handles the claimed case. Optional, needs the builder: add a Button on Grand that calls Claim when today==7.

**Files.** Assets/Scripts/UI/UI51ChestView.cs, Assets/Scripts/UI/UI51RewardsView.cs

**Risk.** Auto-reveal changes the 02/10 chest decision (the mockup has a 'TOCCA PER APRIRE' step). The user must confirm which single tap they want. UI51ChestView is also opened by UI51MailView.Claim, which needs the same check. Disabling the tiles must not survive a failed call: the error path has to re-enable them.

**Decision.** Which tap should remain? Option A: chest auto-opens and RACCOGLI closes it. Option B: tapping the chest opens it and the page closes by itself. Option C: no chest page, just the celebration. Please also confirm which screen the 'two taps' report came from (Premi RISCATTA, chest, or Posta).

**Test plan.** Play Mode in Editor: set the server Premi read-only data to {giorno:4, ultimo:<yesterday>} to reach day 5. Tap RISCATTA once, then confirm the chest reveals by itself and one RACCOGLI tap closes it. Device (iPhone, Network Link Conditioner 3G): tap RISCATTA, then confirm the button stops responding to presses until the answer arrives. Only a device shows the real CloudScript latency.

**Verifier (partially_confirmed).** Everything the diagnosis cites checks out in the code, but it misses the most likely cause on the Premi page.

Confirmed:
- Chest flow: Awake wires chestButton to Reveal and collect to close (UI51ChestView.cs:28-29). Open() leaves the chest closed (44-52).
- The only hit area is the Chest rect at 195,385, 220x191 (UI51ProgressBuilder.cs:455-458). The 'TOCCA PER APRIRE' label is a TMP text with raycastTarget=false (UI51Build.cs:176) and has no Button (461-462).
- Claim() only changes the label (UI51RewardsView.cs:192-195). Buttons use Transition.None (UI51Build.cs:208-217).
- UI51Press plays the press animation while the button is interactable (UI51Press.cs:35). It dims only with m_DisabledGroup (56-59), and ButtonBody never wires that (UI51PrefabBuilder.cs:91-101).
- The Grand card has no Button (UI51SocialBuilder.cs:858-887).
- Timeout ruled out: in Unity 2018.2+ the enum is UnityWebRequest=0, HttpWebRequest=1, CustomHttp=2 (PlayFabSettings.cs:9-17). RequestType:2 is CustomHttp, so PluginManager.cs:89-101 falls back to PlayFabUnityHttp, which sets no timeout (PlayFabUnityHttp.cs:130-165).

Missed mechanism:
- Open() calls FromServer and then RewardsService.RefreshDaily (UI51RewardsView.cs:94-95), so a statoPremi request is in flight exactly when the player taps RISCATTA.
- When statoPremi answers, SetDaily raises DailyChanged (RewardsService.cs:98-99, 137-143). That runs FromServer, then Show, then Render, which runs claimLabel.DOKill() and resets the text to ClaimText (UI51RewardsView.cs:140-141). BindDay also re-enables today's tile (148). Both happen while `claiming` is still true.
- So the '…' disappears, the tap looks lost, and the second tap is swallowed by the guard at line 192.
- Worse: statoPremi reads Premi before the claim writes it (51.js:345-347 vs 355). If its answer lands after the riscattaPremio answer, nothing orders them and SetDaily overwrites the state with riscattato=false. The page flips back to RISCATTA and the Home Premi dot returns. A second tap then gets 'gia' riscattato' with riscattato:true (51.js:352), and the page flips to claimed. That is literally two taps.
- Players normally tap right after opening Premi, inside the statoPremi round trip, so this window is the common case.
- The '…' label is a visible change. Claim (b) alone (no busy state) is weaker than the diagnosis says.

**Corrected cause.** Three mechanisms:
(1) Design: chest days (5, 7, and Posta forzieri) need a reveal tap plus RACCOGLI (UI51ChestView.cs:28-29, 44-52).
(2) Most likely for the Premi button: the RefreshDaily that Open() fires (UI51RewardsView.cs:95) races the claim. Its DailyChanged runs Render, which wipes the '…' busy label and re-enables the tiles mid-claim (140-141, 148). RewardsService.SetDaily has no ordering guard (137-143), so a late statoPremi answer can revert a successful claim to unclaimed and force a second tap.
(3) No visible disabled state: Transition.None plus no m_DisabledGroup.

- `Assets/Scripts/UI/UI51RewardsView.cs:91-96` Open() renders from the cache, then fires RefreshDaily (statoPremi) on every open
- `Assets/Scripts/UI/UI51RewardsView.cs:98-105,121-143` FromServer, Show and Render ignore `claiming`; Render resets claimLabel to ClaimText at 140-141
- `Assets/Scripts/UI/UI51RewardsView.cs:147-148` BindDay sets d.button.interactable=isToday on every Render, which would undo any lock set in Claim()
- `Assets/Scripts/Auth/RewardsService.cs:98-102,137-143` RefreshDaily and ClaimDaily both feed SetDaily, which applies whichever answer arrives last
- `Server/CloudScript/51.js:345-360` statoPremi reads Premi; riscattaPremio reads then writes. A statoPremi answer read before the write can arrive after the claim's answer
- `Assets/PlayFabSDK/Shared/Public/PlayFabSettings.cs:9-17` in 2018.2+ RequestType 2 is CustomHttp, so the transport falls back to PlayFabUnityHttp (no timeout, parallel coroutines, so answers can arrive out of order)

**Fix concerns.** Fix (2), as sketched, would not hold:
- It sets claim and days[i].button interactable=false in Claim(), but the concurrent DailyChanged (statoPremi from Open, 'inizio' from UI51MailView.Load, the midnight RefreshDaily at UI51RewardsView.cs:241) re-runs BindDay, which sets interactable=isToday.
- 'Re-enable claim in Render' would unlock the button mid-flight.
- Render and BindDay must honour `claiming`: keep the '…' label, keep tiles and claim non-interactable.
- RewardsService must drop statoPremi/inizio answers requested before the latest claim. For example: a counter bumped by ClaimDaily and captured by RefreshDaily/Start. Or Open() could skip RefreshDaily when Daily is fresh.
- interactable=false is invisible with Transition.None; it only stops the UI51Press scale.

Fix (1), auto-reveal:
- It changes the 02/10 chest design.
- It also changes Posta, through UI51MailView.cs:271.
- The DelayedCall must die with the view (SetLink KillOnDisable).

### M2 — root_cause_confirmed · verifier: confirmed · size S

**Current behaviour.** Posta RISCATTA and Raccogli tutto set a private 'claiming' flag, so only one request leaves the device at a time. The button still looks and feels pressable: it is interactable, the press animation plays, and only the label changes. After a success the same button shows '+N MONETE' for 1.6 s and still accepts presses. If the server answers ok:false the button returns to RISCATTA after 2 s with no state change, and every further tap repeats the 'NON DISPONIBILE' loop. Server: claimMail marks riscattato before granting, so a repeated request after the first one completes gets 'niente da riscattare' (idempotent for sequential calls). Two requests running at the same moment would both grant.

**Root cause.** Client: no visible input lock. 'claiming' is only a silent early return, Button.interactable is never set to false, and Transition.None means a disabled button would look the same anyway. No reconciliation on ok:false or error. One known server/client mismatch can produce a permanent ok:false. For a message without 'data', the client takes the record's LastUpdated as the date and shows RISCATTA. The server's mailDate() returns 0, so canClaim() fails the 30-day test forever, and pruneMail deletes the message at the next PostaGlobale delivery. That only applies if a message was written by hand without 'data' (hypothesis to check in Game Manager). Server idempotency under true concurrency (two devices, or a modded client) is not guaranteed. UpdateUserReadOnlyData has no version precondition (verified in Context7: it is an additive update that overwrites existing keys). PlayFab docs also state that CloudScript is not atomic. The trust-model memory lists 'races (no conditional writes)' as an accepted limit.

**Evidence.**
- `Assets/Scripts/UI/UI51MailView.cs:259-279` Claim(): silent guard at 261, label only at 264, ok:false -> 'NON DISPONIBILE' at 269 with no refetch, error at 274-278 with no refetch
- `Assets/Scripts/UI/UI51MailView.cs:273` success leaves the RISCATTA button visible for 1.6 s before OpenMessage swaps in RISCATTATO
- `Assets/Scripts/UI/UI51MailView.cs:281-298` ClaimAll shares the same silent guard
- `Assets/Scripts/UI/UI51MailView.cs:300-305` MarkClaimed flips riscattato only on objects in the current 'messages' list; if a Fill replaced the list, 'opened' is stale and stays claimable
- `Assets/Scripts/Auth/MailService.cs:100` client: a missing data falls back to the record's LastUpdated, so the message is visible and claimable
- `Server/CloudScript/51.js:187-197` server: mailDate()=0 when data is missing, so canClaim is false forever
- `Server/CloudScript/51.js:224-228` pruneMail drops messages without data whenever deliverGlobalMail saves
- `Server/CloudScript/51.js:230-248` claimMail: read, mark, save (245), then grant (246); safe for sequential repeats, but a concurrent pair can both read riscattato=false
- `Server/CloudScript/51.js:350-360` riscattaPremio uses the same read-then-write pattern
- `Server/CloudScript/test.js:87,109,114` node tests cover the sequential double claim only

**Fix sketch.** Client, in UI51MailView only. In Claim()/ClaimAll(), set claim.interactable=false and claimAll.interactable=false at the start. Re-enable both only in OpenMessage()/Render() and in the error callback. On !r.ok or error, reconcile through the same path as M3 (refetch plus wallet refresh) instead of only changing the label. Server, XS and separate: treat a missing data the way the client does, i.e. no age filter. In canClaim, use `var d = mailDate(m); if (d && now - d > MAIL_DAYS*86400000) return false;` and keep data-less mail in pruneMail. Add a test.js case. Full concurrent idempotency needs a store with a precondition: Entity Objects SetObjects with ExpectedProfileVersion, or Economy v2 with IdempotencyId. That is a new system, so it needs the user's go.

**Files.** Assets/Scripts/UI/UI51MailView.cs, Server/CloudScript/51.js, Server/CloudScript/test.js

**Risk.** The button must be re-enabled on every path (success, ok:false, error, sheet re-opened for another message), or it stays dead. OpenMessage is the single place that should reset it. The server change alters which hand-written messages are claimable, and needs a 51.carica.js re-upload. Concurrency stays an accepted limit unless the user decides otherwise.

**Decision.** Is client lock plus sequential idempotency enough? That is the trust-model limit accepted on 02/10. Or should claims move to a store with conditional writes, so two devices or a modded app can never double-claim? The second option is a new system.

**Test plan.** Play Mode: tap RISCATTA repeatedly, then confirm a single ExecuteCloudScript in the PlayFab request log and that taps have no effect while waiting. Feed Fill() a message with no data and check what RISCATTA does. Node: add test.js cases 'riscattaPosta on a message without data' and 'second call returns ok:false'. Device: a slow network, to check the lock is visible.

**Verifier (confirmed).** Client:
- Claim() has a silent guard and only changes the label (UI51MailView.cs:261-264).
- ok:false shows NON DISPONIBILE with no refetch (269). The error path has no refetch either (274-278). ClaimAll shares the same guard (283-297).
- Button.interactable is never touched.
- At most one request is in flight, because `claiming` is set synchronously at 262.

Server:
- claimMail saves riscattato before granting (51.js:245-246). A sequential repeat gets 'niente da riscattare' (test.js:109, 114).
- Data-less mismatch confirmed. The client falls back to LastUpdated (MailService.cs:100). The server's mailDate returns 0 (51.js:187-190), so canClaim fails the 30-day test (196). pruneMail drops such mail (224-228).
- deliverGlobalMail fills in data (212), so only hand-written messages are affected. test.js:101-106 has no data-less case.
- Context7 (PlayFab docs): only Entity SetObjects has an optimistic-concurrency version. Legacy UpdateUserReadOnlyData has none, so concurrent pairs can double-grant.

Additional mechanism the diagnosis listed as evidence but under-stated:
- The success callback at 273 calls OpenMessage(m) with the `opened` object.
- MarkClaimed only flips objects inside `messages` (303).
- If a Fill (132-137) replaced the list after the row was tapped, `opened` is the old object with riscattato=false. Line 222 then shows RISCATTA again after a successful claim, and each tap gives NON DISPONIBILE until the sheet is closed.
- Trigger: a second Open() in the same MainMenu session. Old rows stay tappable while the refetch is in flight, because Load only shows Caricamento when spawned.Count==0 (107).
- Possible id mismatch: the server matches `ids.indexOf(m.id||m.titolo)` against String(args.id) (51.js:237, 368). A numeric id in hand-written JSON never matches. Uncertain how JsonUtility maps a numeric id on the client.

**Corrected cause.** As diagnosed: a silent client guard with no visible lock or reconcile, the data-less date mismatch, and sequential-only server idempotency.

Plus: a stale `opened` object (list replaced by Fill while the sheet is open) makes the post-success rebind at UI51MailView.cs:273 bring RISCATTA back. That is a concrete way the claim button 'can be pressed again' after a successful claim.

- `Assets/Scripts/UI/UI51MailView.cs:105-107` Load keeps the old rows on re-open (status only when spawned.Count==0), so a row can bind the old object before Fill arrives
- `Assets/Scripts/UI/UI51MailView.cs:222,273,300-304` MarkClaimed never updates `opened`; OpenMessage(stale m) shows claim again because m.CanClaim is still true
- `Server/CloudScript/51.js:235-237,365-368` server id is m.id||m.titolo compared by indexOf against String(id), so a numeric id never matches
- `Server/CloudScript/test.js:101-106` every fixture has data; no data-less or concurrency case

**Fix concerns.** - interactable=false is invisible (UI51Build.cs:208-217 Transition.None). It only stops the UI51Press scale.
- MarkClaimed should also set riscattato on `opened` (match by id). Otherwise the stale-object path survives the lock.
- OpenMessage is a bad single re-enable point. It calls sheet.Open(), which replays FadeIn on the scrim and SheetUp from full height (BottomSheet.cs Open; UIAnim.cs:275-276). Every rebind visibly re-slides the sheet, and it also re-runs MarkRead. Split a bind-only helper out of OpenMessage.
- The server canClaim/pruneMail change needs the 51.carica.js re-upload, plus a test.js case.
- Concurrency remains the accepted trust-model limit.

### M3 — likely_cause · verifier: confirmed · size S

**Current behaviour.** On a positive answer, single RISCATTA re-renders the list and the Home badge at once, through MarkClaimed and Render. The open message switches to RISCATTATO only after the 1.6 s summary. Raccogli tutto re-renders the list only after 2 s, deliberately. The balance is refreshed (WalletService.Refresh, then Changed, then the Home top bar), but the pills exist only in the uncommitted 2.65 Home top bar, which the Posta/Premi canvas (order 700) covers. On ok:false or error nothing is refetched: not the mail list, not the wallet. The stale state lasts until Posta is reopened, because Open() calls Load() and refetches. Even reopening Posta does not refresh the wallet.

**Root cause.** (1) Confirmed: the reconcile paths are missing. UI51MailView's ok:false and error callbacks, and RewardsService.Call's error path, never call MailService.Fetch or WalletService.Refresh. RewardsService.Call refreshes the wallet only when monete>0 || gemme>0. A request that succeeded on the server but failed on the client is the typical case: CloudScript is not atomic, has a 4 s limit (verified in Context7), and mobile networks drop requests. In that case the reward is credited, the client shows RIPROVA, and the next tap shows NON DISPONIBILE. Only close and reopen fixes the screen. (2) Confirmed and deliberate: the 1.6 s and 2 s presentation delays before the sheet and the 'Raccogli tutto' list update. (3) Confirmed design gap: no balance is visible inside Posta or Premi, so the player sees the new balance only after closing. (4) Hypothesis: two quick claims fire two GetUserInventory calls over parallel UnityWebRequests. WalletService has no sequence guard, so the older answer arriving last leaves a stale balance until the next refresh.

**Evidence.**
- `Assets/Scripts/UI/UI51MailView.cs:269,274-278,290,293-297` ok:false and error paths only change the label; no Fetch, no Wallet refresh
- `Assets/Scripts/UI/UI51MailView.cs:273,291-292` deliberate delays: the sheet updates after 1.6 s; Raccogli tutto calls MarkClaimed(r,false) and Renders after 2 s
- `Assets/Scripts/UI/UI51MailView.cs:91-96,105-116` only Open() refetches, which is why close-and-reopen fixes it
- `Assets/Scripts/Auth/RewardsService.cs:156-170` error paths return without any refresh; the wallet refreshes only when monete/gemme>0 (164)
- `Assets/Scripts/Auth/WalletService.cs:23-42` no in-flight sequence guard; no Reset
- `Assets/UIV2/Scripts/Components/UIV2TopBar.cs:29-32,72-77` the wallet pills are the only balance display (2.65, uncommitted), inside the Home top bar
- `Assets/UI51/Editor/UI51ProgressBuilder.cs:446-448` Premi/Posta sit on canvas 700, full-screen over the Home top bar
- `Assets/Scripts/UI/UI51RewardsView.cs:205-209` Premi error path: no RefreshDaily, no wallet refresh

**Fix sketch.** (a) UI51MailView: add a Resync() that runs WalletService.Refresh() and MailService.Fetch(Fill, null). Call it from both ok:false branches and both error callbacks. In Fill(), if sheet.isOpen && opened != null, find the new message with the same id and call OpenMessage(it), so the open sheet re-binds. (b) UI51RewardsView error callback: also call RewardsService.RefreshDaily(null,null) and WalletService.Refresh(). (c) ClaimAll: call Render() at once, then keep claimAll active with its summary label until the delayed Render. (d) WalletService.Refresh: an int sequence counter, ignoring answers older than the latest request (2 lines). (e) A balance inside Posta/Premi needs a decision: wallet pills in their headers via WalletService.Changed and UIV2TopBar.Amount, or UI51Toast Kind.Coins (2.44 deliberately said no toast in Posta).

**Files.** Assets/Scripts/UI/UI51MailView.cs, Assets/Scripts/UI/UI51RewardsView.cs, Assets/Scripts/Auth/WalletService.cs

**Risk.** Fill() re-binding the sheet calls OpenMessage, which marks the message read and Renders. That is fine but should not re-trigger the chest. Refetch on error doubles one GetUserReadOnlyData. WalletService is also used after a match (MatchResultsV2 -> premioPartita), so the sequence guard affects every caller (positively).

**Decision.** Should Posta/Premi show the balance inside the page (header pills), or should the summary also go to a Coins toast? Also: should the 1.6 s and 2 s summary delays stay?

**Test plan.** EditMode: WalletService ordering is hard to unit-test without a mock, so cover it in Play Mode. Play Mode in Editor: block the network in the middle of a claim (disable the adapter after the request leaves), then confirm the list and wallet resync without closing Posta. Device: claim with a forziere and two quick claims, then compare the Home pills with the Game Manager VC balance.

**Verifier (confirmed).** - On single success, MarkClaimed calls Render immediately (UI51MailView.cs:270, 300-304). The sheet rebinds after 1.6 s (273). ClaimAll renders after 2 s (291-292).
- The ok:false and error paths never call Fetch or WalletService.Refresh (269, 274-278, 290, 293-297).
- RewardsService.Call refreshes the wallet only when monete>0||gemme>0 (RewardsService.cs:164). Its error branches only report (156-162, 166-170).
- Only Open() refetches (91-96, 115), so close-and-reopen fixes the screen. Open() never refreshes the wallet.
- The wallet pills exist only in the uncommitted 2.65 diff (UIV2TopBar SetWallet; HomeV2Integration RefreshWallet).
- Posta and Premi sit on canvas sortingOrder 700 over the Home (UI51SocialBuilder.cs:24, 75-95). The balance is therefore only visible after closing.
- WalletService has no sequence guard (WalletService.cs:23-42), and PlayFabUnityHttp runs requests as parallel coroutines, so (4) is plausible but unproven.

Two extra stale-state paths that only close and reopen fix:
(a) The stale `opened` object (see M2) makes the open sheet revert to RISCATTA after 1.6 s, even though the list is already correct.
(b) The GetUserReadOnlyData from Open() can be served before the claim's saveReadOnly and arrive after MarkClaimed. Fill (132-137) then blindly replaces `messages` with riscattato=false copies, so the rows, the Raccogli tutto button and the badge revert.

Visual side effect: the 1.6 s rebind replays the sheet's slide-up and scrim fade (OpenMessage calls sheet.Open()).

**Corrected cause.** As diagnosed:
- Missing reconcile on ok:false and error.
- Deliberate 1.6 s / 2 s delays.
- No balance visible inside Posta or Premi.

Plus two snapshot-ordering bugs, with no guard in Fill or in MarkClaimed:
- A Fill whose fetch was answered before the claim's write can overwrite a successful claim.
- A stale `opened` object reverts the sheet to RISCATTA.

- `Assets/Scripts/UI/UI51MailView.cs:132-137` Fill replaces `messages` unconditionally; no ordering against a claim answered in the meantime
- `Assets/UI51/Scripts/Components/BottomSheet.cs:Open()` Open() kills tweens, runs FadeIn on the scrim and SheetUp again; the call at UI51MailView.cs:273 re-slides an already-open sheet
- `Assets/UI51/Scripts/Anim/UIAnim.cs:275-276` SheetUp animates Y from Height(t) to 0, a full re-entry
- `Assets/UI51/Editor/UI51SocialBuilder.cs:24,203-262` canvas 700; the Posta header has no wallet display

**Fix concerns.** (a) Re-binding the open sheet from Fill through OpenMessage replays SheetUp and the scrim fade, and calls MarkRead. Use a bind-only helper.
- Fill should keep riscattato=true for ids claimed in this session (or drop fetches issued before the last claim). Otherwise Resync can itself bring stale state back.
(c) Calling Render immediately in ClaimAll hides the claimAll button: Render sets claimAll active only when claimable > 1 (146), so the summary label disappears. Keep the button visible until the delayed Render.
(d) The sequence guard affects every WalletService caller, including after a match. That is benign.
(e) The balance-in-page option is a user decision.

### M4 — root_cause_confirmed · verifier: confirmed · size S

**Current behaviour.** The Home Posta dot is the count of messages never opened on this device (MailService.UnreadCount over PlayerPrefs 'Mail.ReadIds'). It is set only by UI51MailView.Render, which runs on scene Start and when Posta opens. 'Raccogli tutto' collects gifts without marking the messages read, so the dot stays. Read state is one device-wide key, and every MarkRead rewrites it with only the current account's ids, so a second account on the same phone wipes the first account's read state. After Accedi, the dot keeps whatever the previous session (usually the guest) computed, until Posta is opened.

**Root cause.** Three confirmed causes. (1) Wrong criterion: claimed (riscattato) messages still count as unread, and ClaimAll/MarkClaimed never call MarkRead. (2) PlayerPrefs not scoped per account: the ReadKey is global, and MarkRead prunes it to the ids in the current list. Account A reads a1; account B opens b1, so the key becomes {b1}; back on A, a1 is unread and the dot comes back. It is also lost on reinstall and not shared across devices. (3) No refresh on account change. UI51MailView loads only in Start()/Open(). RebindPhoton/LogoutAndRestart call RewardsService.Reset(), which raises no event. HomeV2Integration.ReloadAccountProfile refreshes profile and wallet only. Guests run Load() at boot: 'inizio' delivers PostaGlobale (e.g. Benvenuto) to the guest, and the badge is set on the hidden guest button. HomeScreenV2.SetGuest(false) then shows that stale count for the real account. The Premi dot has the same staleness: Reset() nulls Daily without DailyChanged. A smaller race also exists: an in-flight guest 'inizio' answer arriving after Reset sets started=true and the guest's Daily for the new account.

**Evidence.**
- `Assets/Scripts/UI/UI51MailView.cs:139-143` badge = MailService.UnreadCount(messages), ignores riscattato
- `Assets/Scripts/UI/UI51MailView.cs:281-305` ClaimAll -> MarkClaimed sets riscattato and never calls MarkRead
- `Assets/Scripts/UI/UI51MailView.cs:89,91-96,105-116` badge computed only on Start and Open; Load has no guest check
- `Assets/Scripts/Auth/MailService.cs:45,121-143` global key 'Mail.ReadIds'; MarkRead keeps only ids present in the current account's list
- `Assets/Scripts/Auth/RewardsService.cs:82-87,130-135` Reset raises no event; the in-flight 'inizio' callback is not session-guarded
- `Assets/Scripts/Auth/AuthBootstrapper.cs:196-199,268-272` account switch and logout only call RewardsService.Reset()
- `Assets/Scripts/Auth/AuthUIController.cs:311-317` login success: RebindPhoton, then OnLoginSuccess; no mail reload
- `Assets/UIV2/Scripts/Core/HomeV2Integration.cs:90-91,265-279` OnLoginSuccess -> ReloadAccountProfile refreshes profile and wallet, not mail or rewards
- `Assets/UIV2/Scripts/Screens/HomeScreenV2.cs:89-92,105-109` the badge survives on the hidden guest button and reappears on SetGuest(false)
- `Assets/Scripts/UI/UI51RewardsView.cs:98-105` Premi dot is updated only via DailyChanged and Open
- `Server/CloudScript/51.js:337-343,200-221` inizio delivers PostaGlobale to every account, guests included

**Fix sketch.** (1) MailService: in IsRead/UnreadCount, count a message as read when it is in ReadIds OR (m.HasGifts && m.riscattato). One helper, so the row marker, the label and the badge all follow it. (2) MailService: ReadKey -> 'Mail.ReadIds.' + PlayFabId, from AuthBootstrapper.Instance?.PlayFabAuth?.PlayFabId. On the first read, fall back to the legacy key once. (3) UI51MailView: in Start, subscribe to AuthBootstrapper.Instance.Profile.OnProfileLoaded (existing event, fires at boot and after Accedi via ReloadAccountProfile). If PlayFabId differs from the id last loaded, clear messages and rows, call home.SetMailBadge(0), then Load(). Unsubscribe in OnDestroy. (4) RewardsService: Reset() increments a session counter and invokes DailyChanged, so the Premi dot clears. Start's callback ignores answers from an older session. Item (3) also refreshes the Premi dot, because Load re-runs RewardsService.Start, then SetDaily, then DailyChanged.

**Files.** Assets/Scripts/Auth/MailService.cs, Assets/Scripts/UI/UI51MailView.cs, Assets/Scripts/Auth/RewardsService.cs

**Risk.** Scoping the key resets read state once for existing installs, unless the legacy fallback is kept. Treating claimed as read changes the row style (no bold, no unread dot) for claimed but unopened messages, which is intended. OnProfileLoaded also fires at boot: guarding by PlayFabId avoids a double fetch. OnProfileLoaded does not fire if the profile load fails, which keeps today's behaviour in that case. Invoking DailyChanged from Reset runs FromServer, which handles a null Daily.

**Decision.** Should read state stay per device (scoped per account), or move server-side so it syncs across devices? Server-side means a new 'letto' field written by CloudScript.

**Test plan.** EditMode (new MailServiceTests cases, PlayerPrefs with cleanup): UnreadCount is 0 for a claimed-but-unopened message, and MarkRead under id A does not drop A's ids when B marks. Play Mode in Editor: start as guest with a PostaGlobale message in TitleData, then Accedi with an account that has read everything, and confirm the dot is off without opening Posta. Logout, Accedi with a second account, back to the first: the dot does not come back. Device: the same sequence on TestFlight with two accounts on one phone.

**Verifier (confirmed).** Wrong criterion:
- The badge counts ReadIds only (UI51MailView.cs:139-143; MailService.cs:121-129).
- ClaimAll and MarkClaimed never call MarkRead (281-305). Single claims mark read only because OpenMessage marks read (205-206).

Not scoped per account:
- The read state is one global key (MailService.cs:45).
- MarkRead keeps only ids present in the current list (132-140), so a second account wipes the first account's read ids.

No reload on account switch:
- The view loads only in Start/Open (89, 91-96, 105-116).
- Login success calls RebindPhoton and then OnLoginSuccess (AuthUIController.cs:311-317).
- RebindPhoton and LogoutAndRestart call RewardsService.Reset (AuthBootstrapper.cs:196-199, 268-272). Reset raises no event (RewardsService.cs:130-135).
- ReloadAccountProfile refreshes profile and wallet only (HomeV2Integration.cs:265-279).
- SetGuest hides the buttons without clearing their badges (HomeScreenV2.cs:105-109).
- In-flight race confirmed: the 'inizio' callback sets started=true and Daily after a Reset (RewardsService.cs:82-87).

This happens on every cold start, not only after a manual account switch:
- StartScreenV2.Start always shows Login unless the player is returning from a match (StartScreenV2.cs:33-34).
- The boot session is never the email account. It is a fresh guest per launch (AuthBootstrapper.cs:112-116 ResetGuestDeviceId; PlayFabAuthService.cs:103-113, 161). For HasRealLogin users it is the DEVICE_ID custom-ID account, which nothing ever links to the email account (no LinkCustomID anywhere in Assets/Scripts).
- UI51MailView.Start loads that boot account. 'inizio' delivers PostaGlobale to it, dated today when the global has no date (51.js:212).
- A global whose copy on the real account aged past 30 days (MailService.cs:105) has already been pruned from Mail.ReadIds by MarkRead. So it counts as unread again on the boot account.
- After Accedi the dot shows, opening Posta clears it, and the next launch brings it back. That matches 'ritorna visibile alla riapertura'.

**Corrected cause.** The three diagnosed causes are confirmed.

Cause (3) is systematic: every cold start goes Login, boot account, Accedi, then the email account. The Home Posta badge is computed for the boot account (fresh guest or unlinked device-id account) and is never recomputed after Accedi. ReadIds pruning (cause 2) then makes aged-out global messages count as unread on each launch.

- `Assets/UIV2/Scripts/Core/StartScreenV2.cs:27-34` a cold start always opens the Login screen; Accedi happens after the boot session has already loaded Posta
- `Assets/Scripts/Auth/AuthBootstrapper.cs:110-119,329` guest device id reset every launch unless HasRealLogin; the boot login is always LoginAsGuest
- `Assets/Scripts/Auth/PlayFabAuthService.cs:157-176` boot uses a session guest id or the device id; no LinkCustomID exists, so it is never the email account
- `Server/CloudScript/51.js:199-221` each new boot or guest account gets PostaGlobale copies dated today
- `Assets/Scripts/Auth/ProfileService.cs:79-90` OnProfileLoaded fires only when all three loads succeed

**Fix concerns.** Fix (3), the reload trigger:
- OnProfileLoaded is skipped when any profile sub-load fails (ProfileService.cs:84-89).
- AuthUIController.OnLoginSuccess / OnRegistrationSuccess are a more reliable trigger. HomeV2Integration.cs:89-91 already uses this pattern, and they fire after RebindPhoton's Reset (AuthUIController.cs:311-317).
- Do NOT hook PlayFabAuthService.OnLoginSuccess. It fires before onSuccess runs RebindPhoton, so RewardsService.Start would still see the guest's started=true.

Fix (2), the per-account key:
- The boot account's badge would count every global as unread until the reload. The immediate SetMailBadge(0) on account change is therefore required, not optional.

Session reset:
- After Reset, the Premi page still renders the previous account's today/claimed fields when Daily is null (UI51RewardsView.cs:104, 114-118). Fix (4) should clear those as well.

### Missed by the investigator (found by the verifier)

- **M1**: A Premi claim races with the statoPremi request that Open() fires. When statoPremi answers, Render wipes the '…' busy label and re-enables the tiles while the claim is still in flight. If the statoPremi answer arrives after the claim's answer, SetDaily (which has no ordering guard) reverts the page to unclaimed and brings back the Home Premi dot. A second tap is then needed: the server answers 'gia' riscattato' and the page flips to claimed. This is the most concrete source of 'two taps' on Premi.
  - `Assets/Scripts/UI/UI51RewardsView.cs:91-96` Open fires RefreshDaily on every open
  - `Assets/Scripts/UI/UI51RewardsView.cs:139-148` Render resets claimLabel to ClaimText and BindDay re-enables today's tile, regardless of `claiming`
  - `Assets/Scripts/Auth/RewardsService.cs:98-102,137-143` statoPremi and claim answers both go through SetDaily; whichever arrives last wins
  - `Server/CloudScript/51.js:345-360` statoPremi can read Premi before riscattaPremio writes it
- **M3**: After every successful single Posta claim, the 1.6 s callback calls OpenMessage on an already-open sheet. OpenMessage calls BottomSheet.Open(), which replays the scrim FadeIn and SheetUp from full height, so the sheet visibly drops and slides up again. Any fix that rebinds the sheet through OpenMessage (diagnosis fixes M2 and M3a) inherits this glitch.
  - `Assets/Scripts/UI/UI51MailView.cs:227,273` OpenMessage always calls sheet.Open(); the success callback re-calls it while the sheet is open
  - `Assets/UI51/Scripts/Components/BottomSheet.cs:Open()` DOTween.Kill, then FadeIn on the scrim and SheetUp on the sheet, every call
  - `Assets/UI51/Scripts/Anim/UIAnim.cs:275-276` SheetUp animates Y from Height(t) to 0
- **M2**: Posta snapshot ordering. Open() always refetches the mail. If GetUserReadOnlyData is served before the claim's saveReadOnly but answers after MarkClaimed, Fill overwrites `messages` with riscattato=false copies, and the row, the badge and Raccogli tutto revert to claimable. Tapping RISCATTA again gives NON DISPONIBILE. Separately, a row tapped before Fill binds an object that MarkClaimed never updates, so the sheet shows RISCATTA again after a successful claim. Both are fixed only by closing and reopening.
  - `Assets/Scripts/UI/UI51MailView.cs:105-107,132-137` re-open keeps the old rows tappable; Fill replaces the list unconditionally
  - `Assets/Scripts/UI/UI51MailView.cs:222,273,300-304` MarkClaimed updates only `messages`, never `opened`
- **M4**: After RewardsService.Reset (Accedi or logout), the Premi page renders the previous account's day and claimed state when opened. FromServer with Daily==null calls Render() using the `today`/`claimed` fields left by the last Show(), so the real account briefly sees the guest's day (usually day 1 RISCATTA) until statoPremi answers. A tap in that window claims whatever day the server decides.
  - `Assets/Scripts/UI/UI51RewardsView.cs:98-105,114-119` Daily null leads to Render with stale today/claimed fields
  - `Assets/Scripts/Auth/RewardsService.cs:130-135` Reset nulls Daily without notifying the view

### Cross-cutting notes

- **session-state** (M3, M4): Static services keep state across an account switch with no event to their UI. WalletService has no Reset, so Coins, Gems and IsLoaded of the previous account show in the top bar until the refresh after login returns. RewardsService.Reset gives no notification and does not guard callbacks still in flight. Views that load once in Start (Mail, Rewards, probably News and Friends) never reload after Accedi or Registrati. Any cluster about the wrong data after login or logout (profile, friends, moderation) should check the same pattern.
  - `Assets/Scripts/Auth/WalletService.cs:18-21` static balances, no Reset
  - `Assets/UIV2/Scripts/Core/HomeV2Integration.cs:327-335` RefreshProfile pushes the stale Wallet values right after login
  - `Assets/Scripts/Auth/RewardsService.cs:82-87,130-135` an in-flight callback can write the old account's Daily after Reset
  - `Assets/Scripts/Auth/AuthBootstrapper.cs:196-199,268-272` only RewardsService/ModerationService are reset
- **playerprefs-not-account-scoped** (M4): Several PlayerPrefs keys that hold per-player state are device-wide. One account's choices or state leak into the other account on the same phone.
  - `Assets/Scripts/Auth/MailService.cs:45` Mail.ReadIds
  - `Assets/UIV2/Scripts/Core/CollectionCosmeticsV2.cs:19,28` Collection.Emoticons (equipped emoticons) shared by all accounts
  - `Assets/Scripts/Auth/FriendsService.cs:103` Social.MutedEmoticons shared by all accounts
  - `Assets/UIV2/Scripts/Core/MatchResultsV2.cs:306-307` Moderazione.AbbandoniPremiati shared by all accounts
  - `Assets/Scripts/UI/UI51WelcomeView.cs:15` UI51WelcomeSeen per device (may be intended)
- **ui-busy-state** (M1, M2): UI51 buttons are built with Selectable.Transition.None, and UI51Press dims only when m_DisabledGroup is wired, which the shared GoldBody/ButtonBody never do. So setting interactable=false is invisible everywhere. Every 'lock the button while the server answers' fix in other clusters (login, register, invites, report, results) hits the same gap. Wiring m_DisabledGroup once in UI51PrefabBuilder.ButtonBody (CanvasGroup on the root) would give every UI51 button a disabled look in one place, but it needs a builder re-run.
  - `Assets/UI51/Editor/UI51Build.cs:208-217` Transition.None
  - `Assets/UI51/Editor/UI51PrefabBuilder.cs:91-101` ButtonBody adds UI51Press without m_DisabledGroup
  - `Assets/UI51/Scripts/Components/UI51Press.cs:17-18,56-59` the disabled alpha only applies with m_DisabledGroup
- **resync-snapshot** (M3): RewardsService.Call error paths never reconcile, yet CloudScript is not atomic and has a 4 s limit (PlayFab docs via Context7). The end-of-match flow shares this. UI51ResultsView shows 'Monete non disponibili' on error while premioPartita may already have written stats and coins. Neither the wallet nor the profile stats are re-read, so the end-of-match / XP cluster probably shows the same mismatch.
  - `Assets/Scripts/Auth/RewardsService.cs:156-170` errors only reported, no refresh
  - `Assets/Scripts/UI/UI51ResultsView.cs:488-496` 'Monete non disponibili' on failure
  - `Assets/UIV2/Scripts/Core/MatchResultsV2.cs:321,356` MatchReward/MatchQuit callers
- **input-gating** (): Outside this cluster, probably a bug. HomeV2Integration.Start calls home.SetPendingActionsInteractable(false), which sets the Classifica quick-action Button.interactable=false. Nothing in the code ever sets it back to true, so the Home Classifica button probably never fires OnRankingPressed. Verify with one tap on a device or in Play Mode.
  - `Assets/UIV2/Scripts/Core/HomeV2Integration.cs:83` only caller, passes false
  - `Assets/UIV2/Scripts/Screens/HomeScreenV2.cs:111-120` disables rankingButton.Button
  - `Assets/Scenes/MainMenu.unity:309730` rankingButton is wired in the scene
  - `Assets/Scripts/UI/UI51RankingView.cs:45` Classifica opens only via home.OnRankingPressed
- **server-idempotency** (M2): Every CloudScript handler that reads, modifies and then writes (riscattaPremio, claimMail, premioPartita's daily caps, segnala, abbandono) is safe for sequential repeats but not for concurrent ones. UpdateUserReadOnlyData and UpdateUserInternalData have no version precondition (verified in Context7). Only Entity Objects (ExpectedProfileVersion) or Economy v2 (IdempotencyId) offer one. This matches the limit accepted in the 02/10 trust model; any cluster asking for strict once-only semantics depends on the same decision.
  - `Server/CloudScript/51.js:230-248,350-360,427-437` read-then-write without a precondition; the ponytail comment at 427 already notes the race

## Login / registrazione / input mobile / font / nickname

### I1 — root_cause_confirmed · verifier: confirmed · size S

**Current behaviour.** After several wrong passwords the Login status label shows 'Errore: <raw English PlayFab message>'. The ACCEDI button comes back on as soon as the error arrives, so the player can keep retrying with no wait and no explanation.

**Root cause.** All login and registration errors go through one mapper, PlayFabAuthService.GetUserFriendlyError. It has no case for the throttling codes, so they fall to the default branch, which prints error.ErrorMessage as-is. The codes are FailedLoginAttemptRateLimitExceeded (1356) and APIClientRequestRateLimitExceeded (1199); both exist in the SDK enum. Nothing on the client tracks repeated attempts. Which of 1356 or 1199 the device actually returned is not known, but both reach the same default branch.

**Evidence.**
- `Assets/Scripts/Auth/PlayFabAuthService.cs:399-428` GetUserFriendlyError: default branch at 425-426 returns $"Errore: {error.ErrorMessage}"; no case for 1356/1199/1342/ConnectionError
- `Assets/Scripts/Auth/PlayFabAuthService.cs:381-386` LoginWithEmail failure lambda: logs raw, passes the mapped string to onError
- `Assets/PlayFabSDK/Shared/Internal/PlayFabErrors.cs:204, 346, 360, 968` APIClientRequestRateLimitExceeded=1199, APIConcurrentRequestLimitExceeded=1342, FailedLoginAttemptRateLimitExceeded=1356; PlayFabError.RetryAfterSeconds exists
- `Assets/Scripts/Auth/AuthUIController.cs:320-325, 333-340` onError: SetLoading(false) turns loginButton back on at once; no cooldown or attempt count
- `Assets/Scripts/Auth/AuthUIController.cs:342-349` SetStatusText paints errors Color.red/green, while AuthScreensV2 uses UI51 tokens (AuthScreensV2.cs:73-74) on the same labels
- `Assets/Scripts/UI/UI51RecoveryView.cs:30-31, 62-68` Existing pattern to reuse: a 30 s resend cooldown with resendAt + Time.unscaledTime
- `context7:/microsoftdocs/playfab-docs global-api-method-error-codes` VERIFIED: 1199, 1342 and 1123 are listed as 'safe to retry with backoff', meaning the client calls too fast.

**Fix sketch.** Do this in the same block as I2 (same CP1252 file; re-encode it first). (1) GetUserFriendlyError: map 1356 to 'Troppi tentativi con la password sbagliata. Aspetta qualche minuto o usa Password dimenticata?'. Map 1199, 1342 and ServiceUnavailable to 'Troppe richieste ravvicinate, riprova tra qualche secondo'. Map ConnectionError to 'Connessione assente, controlla la rete'. Make the default a generic Italian message; the raw text is already logged at :384. Make the method public so tests can reach it. (2) Add the cooldown in the one shared function: in the LoginWithEmail failure lambda, on 1356 or 1199 set a field loginBlockedUntil = Time.unscaledTime + (error.RetryAfterSeconds ?? 60). At the top of LoginWithEmail, if the cooldown has not expired, call onError("Troppi tentativi: riprova tra Ns") without calling PlayFab. Optional one-liner: use UI51Tokens.DangerText instead of Color.red in AuthUIController.SetStatusText.

**Files.** Assets/Scripts/Auth/PlayFabAuthService.cs, Assets/Tests/Editor/AccountDeletionTests.cs (or a new small AuthErrorTests.cs)

**Risk.** There is only one caller of LoginWithEmail (AuthUIController.OnLoginClicked). GetUserFriendlyError is also used by guest login (OnLoginErrorInternal :221-226, which only logs) and by registration (AuthUIController:275), and the new mappings improve both. A cooldown that is too long could lock out a user who fixed a typo; 60 s is a guess until the real RetryAfterSeconds is seen.

**Decision.** Cooldown length when PlayFab sends no RetryAfterSeconds (proposed 60 s). Also: should the app suggest 'Password dimenticata?' after N consecutive wrong passwords, even before PlayFab throttles?

**Test plan.** EditMode: GetUserFriendlyError(new PlayFabError{Error=FailedLoginAttemptRateLimitExceeded}) returns Italian text that does not start with 'Errore:'; the same for 1199 and ConnectionError; the default never contains ErrorMessage. Device (TestFlight, real account): about 10 wrong passwords. Log error.Error, HttpCode and RetryAfterSeconds once to learn the real code and wait time, then check that the message and cooldown appear.

**Verifier (confirmed).** I checked each step myself. GetUserFriendlyError (PlayFabAuthService.cs:399-428) has no case for 1356 (FailedLoginAttemptRateLimitExceeded), 1199, 1342 or ConnectionError(2), so those codes reach `default: return $"Errore: {error.ErrorMessage}"` at :425-426. The LoginWithEmail failure lambda (:381-386) passes that string to AuthUIController's onError (AuthUIController.cs:320-325). There SetLoading(false) (:333-340) sets loginButton.interactable=true straight away. Nothing counts attempts or adds a cooldown. The SDK enum has 1199 (PlayFabErrors.cs:204), 1342 (:346) and 1356 (:360), and PlayFabError.RetryAfterSeconds (:968) is filled from the JSON retryAfterSeconds (PlayFabHTTP.cs:415). Context7 (playfab-docs throttling/best-practices) confirms that 429 + 1199 comes with retryAfterSeconds and a raw English errorMessage. Callers are confirmed: guest login (:223), LoginWithEmail (:383) and registration (AuthUIController.cs:275). The tests have no asmdef (Assets/Tests has no .asmdef), so they compile into Assembly-CSharp-Editor and cannot see an `internal` member. Making the method public is therefore needed, as the diagnosis says. I could not check which code the device actually got (1356 or 1199); the diagnosis already says so.

- `Assets/PlayFabSDK/Shared/Internal/PlayFabHttp/PlayFabHTTP.cs:415` RetryAfterSeconds is parsed from errorDict['retryAfterSeconds'] and is null when absent
- `Assets/Tests/Editor/AccountDeletionTests.cs:77-85` Tests live in Assembly-CSharp-Editor (no asmdef), so GetUserFriendlyError must become public to be tested
- `Assets/Scripts/Auth/PlayFabAuthService.cs:417-418` AccountNotFound → 'Account non trovato' (the enumeration leak noted in cross_cutting is real)

**Fix concerns.** A single global loginBlockedUntil also blocks a correct login to a different account (1356 may be per account). Consider keying the cooldown to the identifier typed, or applying the global block only on 1199/1342. GetUserFriendlyError is also used for registration (AuthUIController.cs:275), so the default text must still make sense for AddUsernamePassword errors.

### I2 — likely_cause · verifier: partially_confirmed · size S

**Current behaviour.** On the TestFlight build, 'Questa email è già in uso' (shown when the email is already registered) renders è and à as small squares or blanks. The same text looks fine in the Windows Editor.

**Root cause.** The string is not in a font. It is a C# literal in a source file saved as Windows-1252 (ISO-8859) with no BOM. Only 6 of the project's .cs files are like this. Unity's compile has no /codepage switch, so Roslyn first tries strict UTF-8 and then falls back to the OS ANSI code page. On Windows that is 1252, so è decodes correctly. On macOS/.NET Core the fallback is UTF-8, so each accented byte becomes U+FFFD. HYPOTHESIS, strongly supported: the iOS player was compiled on a Mac. This Library holds no iOS player build; the last player build here is Android, HostPlatform Windows. TMP then cannot find U+FFFD in Nunito or Cinzel, whose fallback lists are empty. It substitutes U+25A1 □ from the default font poppins-medium → fallback LiberationSans SDF, which is exactly the 'quadratino'. The fonts are not the problem: all UI51 fonts are dynamic and already contain è à é ì ò ù È.

**Evidence.**
- `Assets/Scripts/Auth/PlayFabAuthService.cs:412, 414` 'Questa email è già in uso' / 'Questo username è già in uso': `file` reports ISO-8859 text, CRLF, no BOM
- `Assets/Scripts/Networking/MatchmakingManager.cs:500` Same encoding: 'La stanza è piena…', 'La partita è già iniziata…', 'non più disponibile' (room-code errors)
- `Assets/Scripts/Auth/AuthUIController.cs, ProfileService.cs, Core/Rules51.cs, UI/SafeAreaUtil.cs` The other 4 non-UTF8 files. Their non-ASCII bytes are in comments only, so they are harmless but should be converted too.
- `Library/Bee/artifacts/1300b0aP.dag/Assembly-CSharp.rsp` Compiler flags: /utf8output only, no /codepage. Encoding is auto-detected per file.
- `Library/Bee/Playera590a4dd-inputdata.json` Last player build on this PC: HostPlatform Windows, Android. Only an iOS *editor* graph exists (Library/Bee/900b0aE.dag, 04/10 17:20), so the iOS player was not built here.
- `Assets/UI51/Resources/UI51/Fonts/Nunito-Regular SDF.asset:136, 3617` AtlasPopulationMode 1 (dynamic) with the source TTF set; the character table holds 232/224/233/236/242/249/200; m_FallbackFontAssetTable: [] (same for every Nunito and Cinzel asset)
- `Assets/TextMesh Pro/Resources/TMP Settings.asset:22, 24, 33` missingGlyphCharacter 0 (TMP substitutes U+25A1); default font = poppins-medium SDF; global fallback list empty
- `Assets/TextMesh Pro/Resources/Fonts & Materials/poppins-medium SDF.asset:5676-5677` Falls back to LiberationSans SDF (guid 8f586378…), the only asset that contains U+25A1 (9633). None contain U+FFFD.
- `Assets/Scripts/UI/UI51FriendsView.cs:27` Control: accented literals in UTF-8 files (99 of them) should render fine on iOS, so the defect is limited to the CP1252 files

**Fix sketch.** Re-encode the 6 files from CP1252 to UTF-8, byte-safe so CRLF is kept: python p.write_bytes(p.read_bytes().decode('cp1252').encode('utf-8')). Do not use sed -i, which strips CRLF. Add one EditMode test that reads every .cs under Assets/Scripts, Assets/UI51, Assets/UIV2, Assets/Editor and Assets/Tests and decodes it with new UTF8Encoding(false, true); it fails today on exactly these 6 files and blocks the problem coming back. No font or TMP change is needed. Afterwards, update memory feedback_non_utf8_source_files.md, which becomes obsolete.

**Files.** Assets/Scripts/Auth/PlayFabAuthService.cs, Assets/Scripts/Auth/AuthUIController.cs, Assets/Scripts/Auth/ProfileService.cs, Assets/Scripts/Core/Rules51.cs, Assets/Scripts/Networking/MatchmakingManager.cs, Assets/Scripts/UI/SafeAreaUtil.cs, Assets/Tests/Editor/<new SourceEncodingTests.cs>

**Risk.** Very low: only the accented bytes change and the compiled strings stay the same on Windows. Rules51.cs is pinned by the tutorial-seed test, but only comment bytes change there, so the logic is untouched.

**Decision.** Confirm where BUILD 3 for iOS was compiled (Mac Unity Editor, Unity Build Automation, or this Windows PC). If it was built on Windows, this hypothesis is wrong and the glyph path must be checked on the device.

**Test plan.** EditMode: the encoding test fails before the fix and passes after. Device: on the next iOS build from the same machine as BUILD 3, open Registrazione with an email that is already used and read the message, then join a full private room (32765) and read the room error. Before the fix both show squares; after, è and à render. Quick confirmation without a build: the Android APK built on Windows (Builds/b10_dev.apk) should already show è correctly.

**Verifier (partially_confirmed).** Every part I could check on this PC holds. (1) A strict-UTF-8 scan of every Assets/**/*.cs file fails on exactly 6 files: AuthUIController, PlayFabAuthService, ProfileService, Rules51, MatchmakingManager and SafeAreaUtil. Accented bytes sit inside string literals only in PlayFabAuthService.cs:412,414 (E8/E0) and MatchmakingManager.cs:500 (E8,E8,E0,E8,F9); everywhere else they are in comments. (2) The rsp (Library/Bee/artifacts/1300b0aP.dag/Assembly-CSharp.rsp) has /utf8output and no /codepage. (3) In the Windows-built DLLs (Library/Bee/artifacts/1300b0aP.dag/Assembly-CSharp.dll and Library/ScriptAssemblies/Assembly-CSharp.dll), the literal 'Questa email ' is followed by U+00E8 and later U+00E0. So a Windows compile decodes these files as CP1252 correctly, which explains why the Editor and Android look fine. (4) All 6 UI51 font assets contain 224/232/233/236/242/249/200, have ClearDynamicDataOnBuild 0, and have an empty fallback table. (5) TMP replaces a missing character with 9633 (TMPro_UGUI_Private.cs:1228-1262). I parsed the TTF cmaps: Nunito Regular/SemiBold/Bold have neither U+FFFD nor U+25A1. The default poppins-medium falls back to LiberationSans SDF, which contains 9633. LiberationSans SDF also falls back to the dynamic 'LiberationSans SDF - Fallback', which the diagnosis missed, but LiberationSans.ttf has no U+FFFD either. So the □ result holds. (6) No iOS player compile happened on this PC. Bee DAG prefixes encode the BuildTarget (900b0a = iOS, 1300b0a = Android). Only 900b0aE (iOS editor) exists, with no 900b0aP. Playera590a4dd (04/10 22:12) and LastBuild.buildreport (22:13) are Android, even though iOSSupport is installed (PlaybackEngines/iOSSupport). What I could not verify here is the macOS Roslyn fallback to UTF-8 (CodePagesEncodingProvider returns null for code page 0 off Windows, then Encoding.Default is UTF-8) and where BUILD 3 was compiled. Both stay hypotheses, as the diagnosis says. One correction: MatchmakingManager.cs is mixed-encoding, not pure CP1252 (see fix_concerns).

**Corrected cause.** Same as the diagnosis: CP1252 string literals in PlayFabAuthService.cs:412,414 and MatchmakingManager.cs:500, decoded on a non-Windows compile host as U+FFFD; TMP then draws □ from LiberationSans SDF. MatchmakingManager.cs is mixed-encoding: line 299 already has a valid UTF-8 'à' (C3 A0) in a comment, while lines 479 and 500 are CP1252. Windows decodes the whole file as CP1252, so that comment is already mojibake in Windows compiles; it is harmless because it is a comment.

- `Library/ScriptAssemblies/Assembly-CSharp.dll` The user string 'Questa email è già in us…' proves that a Windows compile yields correct code points
- `Assets/Scripts/Networking/MatchmakingManager.cs:299` Bytes C3 A0 = a valid UTF-8 'à' in a comment, in a file that is otherwise CP1252 (lines 479, 500)
- `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset` Falls back to 'LiberationSans SDF - Fallback' (dynamic, source Assets/TextMesh Pro/Fonts/LiberationSans.ttf, includeFontData 1). The TTF cmap has 0x25A1 but no 0xFFFD, so □ (not �) is drawn
- `Library/Bee/` Only 900b0aE.dag (iOS editor, 04/10 17:20); no 900b0aP.dag. Playera590a4dd-inputdata.json is HostPlatform Windows + AndroidPlayer
- `Assets/UI51/Fonts/Nunito-SemiBold.ttf` The cmap has neither U+FFFD nor U+25A1, so the status font (NunitoSemiBold, UI51AccessBuilder.cs:758) cannot draw either

**Fix concerns.** The proposed one-liner `p.read_bytes().decode('cp1252').encode('utf-8')` double-encodes MatchmakingManager.cs:299 (C3 A0 becomes C3 83 C2 A0, i.e. 'rifÃ '). The new strict-UTF-8 test would still pass, so it would not catch this. Convert line by line instead (try UTF-8 per line, else CP1252), or fix that line by hand. ProfileService.cs is LF, not CRLF; the bytes approach keeps each file's line endings, which is fine.

### I3 — feature_missing · verifier: partially_confirmed · size M

**Current behaviour.** On iOS and Android the keyboard covers the active field on Login (email/username, password), Registrazione (nome utente, email, password, conferma), Recupero password, codice stanza and Aggiungi amico. Only the delete-account confirm field moves up.

**Root cause.** No keyboard avoidance exists except a private copy inside DeleteAccountModalV2. All 10 inputs in the app come from one builder function, UI51AccessBuilder.BuildInput. Most of them sit in bottom-anchored sheets, which is exactly where the keyboard opens. Today the user can still see what they type only through the native preview box (I4).

**Evidence.**
- `Assets/UIV2/Scripts/Core/DeleteAccountModalV2.cs:59-73, 189-219` The only TouchScreenKeyboard.area consumer in the repo: KeyboardLift(), already tested, plus a LateUpdate that lifts Modal.Frame and does not lower while Input.touchCount>0
- `Assets/Tests/Editor/AccountDeletionTests.cs:77-85` KeyboardLift test cases (iPhone 12 numbers)
- `Assets/UI51/Editor/UI51AccessBuilder.cs:647-665` Sheet: anchored to the bottom of Safe with an 80 px bleed. Login and Registrazione fields live in it.
- `Assets/UI51/Editor/UI51AccessBuilder.cs:679-724` BuildInput: the single source of all inputs (also called from UI51AccountBuilder.cs:121, UI51MatchBuilder.cs:577, UI51MetaBuilder.cs:338, UI51SocialBuilder.cs:466)
- `Assets/Scenes/MainMenu.unity` 10 TMP_InputFields. Each sits under a block that is the direct child of a 'Safe' with DesignCanvasFit: Login/Register→Sheet, QuickModeV2 Code→Sheet, Friends Name→Add, Recovery Email→Body, DeleteAccount Input→Card. No LayoutGroup on any Safe.
- `Assets/UIV2/Scripts/Core/DesignCanvasFit.cs:14-30` Rewrites Safe.anchoredPosition every LateUpdate, so the lift must move the block under Safe, not Safe or its parent
- `context7:/websites/unity3d_manual MobileKeyboard` VERIFIED: TouchScreenKeyboard.area returns (0,0,0,0) until the keyboard is fully shown. Size comes from visible and area.
- `memory reference_ui51_builder_gotchas.md` Gotcha: pointer-down on a button closes the keyboard and the click fires on release, so UI must not move down while Input.touchCount>0

**Fix sketch.** Generalise the existing DeleteAccountModalV2 lift; do not write a new system. UI51Input is already on all 10 fields. In OnEnable it finds its block (the ancestor whose parent has DesignCanvasFit) and adds, if missing, a ~40-line UI51KeyboardLift on that block. UI51KeyboardLift.LateUpdate: the focused field is the TMP_InputField under the block that isFocused. keyboard = TouchScreenKeyboard.visible ? area.height : 0. target = KeyboardLift(keyboard, fieldBottomPx - lift*unit, fieldTopPx - lift*unit, Screen.safeArea.yMax, unit), using the field's corners instead of the frame's. Keep the existing guards: skip while a parent CanvasGroup alpha < 1 (AnimatedModalV2 tween); never lower while Input.touchCount > 0; never lower while another field of the same block is being focused (I5). Apply block.anchoredPosition += up*(target-lift). On disable, remove the lift, or only reset the counter when the block is an AnimatedModalV2.Frame, because ResetPresentation already restores it. Move KeyboardLift/KeyboardGap into the new component, delete DeleteAccountModalV2.LateUpdate/lift/OnDisable (clean-codebase rule), and update the test reference. Runtime only: no builder rerun and no scene YAML change.

**Files.** Assets/UI51/Scripts/Components/UI51Input.cs, Assets/UI51/Scripts/Components/UI51KeyboardLift.cs (new, replaces the DeleteAccountModalV2 copy), Assets/UIV2/Scripts/Core/DeleteAccountModalV2.cs, Assets/Tests/Editor/AccountDeletionTests.cs

**Risk.** Two writers of anchoredPosition on the same block: AnimatedModalV2 open/close tweens on QuickModeV2 Sheet and DeleteAccount Card, guarded by the alpha<1 skip; UIKeyframes/UIAnim on Recovery and Friends roots must also be checked. A double lift would happen if DeleteAccountModalV2.LateUpdate were left in place. Android TouchScreenKeyboard.area reporting varies by device. The Editor is a no-op because area is 0, so it cannot regress Editor tests.

**Decision.** Ship together with I4: hiding the native preview box without this lift would leave the typed text invisible on covered fields.

**Test plan.** EditMode: the moved KeyboardLift cases still pass; add one case where only the field, not the whole frame, needs to clear the keyboard. Device-only, since the Editor and Simulator have no soft keyboard: iPhone SE plus a notch iPhone and one Android phone. Focus each of the 10 fields; the field and its status line stay above the keyboard; tapping ACCEDI or REGISTRATI with the keyboard open still triggers the click.

**Verifier (partially_confirmed).** The core claim is confirmed. The only TouchScreenKeyboard consumer in project code is DeleteAccountModalV2.cs:200 (LateUpdate 192-216, KeyboardLift 66-72). MainMenu.unity has 10 TMP_InputFields and 10 UI51Input (counted by script guid); GameScene has none. Every field comes from BuildInput (UI51AccessBuilder.cs:679-724). Login and Register fields sit in the bottom-anchored Sheet (:647-665, used at :111-116 and :169-176). DesignCanvasFit (DesignCanvasFit.cs:14-30) rewrites Safe each LateUpdate. AnimatedModalV2.ResetPresentation restores Frame.anchoredPosition (AnimatedModalV2.cs:90-101). The list of covered fields is overstated, though. The Friends 'Add' block is a TopBand at y=80..180 of the 844 design (UI51SocialBuilder.cs:443), so no soft keyboard can cover that field, and the lift there would do nothing. Recovery 'Body' is a TopBand at y=340 (UI51AccountBuilder.cs:117), so its field ends around 46% from the top and is covered only on small phones, or once the iOS preview bar is added. The real hotspots are Login and Register (Sheet) and QuickMode Code.

**Corrected cause.** No keyboard avoidance outside DeleteAccountModalV2. It actually hurts the bottom-sheet fields: Login (2), Registrazione (4) and QuickMode Code. Recovery Email is borderline on small screens. Aggiungi amico sits at the top of its page and is not covered by the keyboard, so a report about 'ricerca amici' must have another cause (for example the I4 preview box or the Friends page itself) and needs a screenshot.

- `Assets/UI51/Editor/UI51SocialBuilder.cs:443` Add panel = TopBand(safe, 20, 20, top 80, h 100): at the top of the screen
- `Assets/UI51/Editor/UI51AccountBuilder.cs:117-124` Recovery Body = TopBand at 340, Email input inside Form
- `Assets/UIV2/Scripts/Core/AnimatedModalV2.cs:36, 55, 90-101` Open() writes Frame.anchoredPosition = origin+down*24 and tweens it; ResetPresentation restores origin on disable
- `ProjectSettings/ProjectSettings.asset:70` androidRenderOutsideSafeArea: 1; no custom windowSoftInputMode in Assets/Plugins/Android

**Fix concerns.** The Register Sheet is tall (4 fields + terms + status + button + 80 px bleed). Lifting it far enough to clear Confirm moves the Sheet over the title and subtitle on Safe (UI51AccessBuilder.cs:162-167). That is acceptable, but needs a visual check. Login and Register Sheets are not AnimatedModalV2 frames, so the generic component must restore the position itself in OnDisable (AuthUIController.HideAllPanels deactivates the panels), not just reset its counter. Adding a component at runtime from UI51Input.OnEnable departs from the builder-wired pattern used everywhere else; wiring it in BuildInput or the builders is closer to the project rules, but needs a builder rerun.

### I4 — likely_cause · verifier: confirmed · size XS

**Current behaviour.** When an input is focused on a phone, a native rectangle appears above the keyboard, over or next to the fields. It is a text box that mirrors what the user types (with a Done/OK button on iOS and Android).

**Root cause.** This is Unity's native 'text preview box' above the soft keyboard, not TMP drawing anything. Every TMP_InputField has m_HideMobileInput = 0. TMP sets TouchScreenKeyboard.hideInput = shouldHideMobileInput before opening the keyboard, so the preview is shown. In that mode TMP also stops drawing its own caret and selection (non-in-place editing), so the visible text lives in the native box. BuildInput never sets shouldHideMobileInput.

**Evidence.**
- `Assets/Scenes/MainMenu.unity` All 10 TMP_InputFields: m_HideMobileInput: 0, m_HideSoftKeyboard: 0, m_OnFocusSelectAll: 1
- `Assets/UI51/Editor/UI51AccessBuilder.cs:701-715` BuildInput configures lineType, contentType, caret and selection, but not shouldHideMobileInput
- `Library/PackageCache/com.unity.textmeshpro@3.0.7/Scripts/Runtime/TMP_InputField.cs:383-397, 4076-4087` VERIFIED in the package source: shouldHideMobileInput returns m_HideMobileInput on Android/iOS; TouchScreenKeyboard.hideInput = shouldHideMobileInput, then TouchScreenKeyboard.Open
- `Library/PackageCache/com.unity.textmeshpro@3.0.7/Scripts/Runtime/TMP_InputField.cs:1379-1390, 3506-3508` InPlaceEditing() is false when the mobile input is not hidden, and then 'No need to draw a cursor on mobile' applies. So the rectangle is not a TMP selection highlight.
- `context7:/websites/unity3d_manual MobileKeyboard` VERIFIED: 'By default, a text preview box appears above the keyboard. This can be disabled by setting TouchScreenKeyboard.hideInput to true' (it may not apply to every keyboard type).

**Fix sketch.** In UI51Input.OnEnable: if (m_Field != null) m_Field.shouldHideMobileInput = true. This is one line, covers all 10 fields with no builder rerun, and makes TMP edit in place with its own gold caret. Mirror it in BuildInput (field.shouldHideMobileInput = true) so the next rebuild keeps it. It must land in the same block as I3.

**Files.** Assets/UI51/Scripts/Components/UI51Input.cs, Assets/UI51/Editor/UI51AccessBuilder.cs

**Risk.** In-place editing on Android IMEs (Gboard, Samsung: composition and predictive text) and on iOS secure fields needs a device check. Without the I3 lift, covered fields would hide what the user types. A secondary possibility for the 'rectangle' is the iOS password AutoFill/QuickType bar on password fields, which this change does not remove.

**Decision.** Confirm hiding the native preview everywhere (standard for in-game UIs), knowing that it depends on I3.

**Test plan.** Device-only (an iOS and an Android phone): focus Email, Password, codice stanza and the friend name field. No native box above the keyboard, the TMP caret shows in the field, typing, deleting and selection work, and passwords are masked. Ask the tester for a screenshot of the original rectangle to confirm it is the native preview and not the iOS AutoFill bar.

**Verifier (confirmed).** I checked the mechanism in the package source and the scene. All 10 fields have m_HideMobileInput: 0, m_HideSoftKeyboard: 0 and m_OnFocusSelectAll: 1 (MainMenu.unity, aggregated). shouldHideMobileInput returns m_HideMobileInput on Android/iOS (TMP_InputField.cs:383-397). ActivateInputFieldInternal sets TouchScreenKeyboard.hideInput = shouldHideMobileInput, then calls Open (:4076-4087). InPlaceEditing() is false in that configuration (:1384-1395), so the caret and selection are not drawn (:3506-3508). BuildInput never sets shouldHideMobileInput (UI51AccessBuilder.cs:701-715). The setter also works at runtime on device (:398-411). That the user's 'rectangle' is this preview box is still inferred, not seen, which matches the diagnosis's 'likely_cause' label.

- `Library/PackageCache/com.unity.textmeshpro@3.0.7/Scripts/Runtime/TMP_InputField.cs:398-411` The setter writes m_HideMobileInput on Android/iOS, so setting it at runtime in UI51Input.OnEnable takes effect
- `Assets/Scenes/MainMenu.unity` All 10 fields have m_OnFocusSelectAll: 1; selectionColor GoldA(0.35) is set by BuildInput (UI51AccessBuilder.cs:715)

**Fix concerns.** Once in-place editing is on, TMP starts drawing its own selection. With OnFocusSelectAll=1, focusing a field that already has text (Recovery email prefilled from Login at UI51RecoveryView.cs:47, Login fields after an error, room code) paints a gold 35% rectangle over the text. That could bring back a 'strange rectangle over the input' report. Consider onFocusSelectAll=false in the same change. Android IME composition with in-place editing still needs a device check, as the diagnosis says.

### I5 — feature_missing · verifier: confirmed · size S

**Current behaviour.** Pressing the keyboard's return key (labelled Fine/Invio/Done; there is no 'Next') on Login or Registrazione closes the keyboard and does nothing else. The user has to tap the next field, which may be under the keyboard. Only codice stanza and Aggiungi amico react to return (they submit).

**Root cause.** No field-to-field chaining exists. BuildInput sets Navigation.Mode.None, and onSubmit is wired only on QuickSelectionPanels.PrivateCode and UI51FriendsView.nameInput. TMP does raise onSubmit when the soft keyboard reports Done, so the hook is there. Unity's TouchScreenKeyboard API has no return-key type, so the label 'Next/Avanti' cannot be set without a native plugin.

**Evidence.**
- `Assets/UI51/Editor/UI51AccessBuilder.cs:704-706` nav.mode = Navigation.Mode.None on every field
- `Assets/UIV2/Scripts/Core/QuickSelectionPanels.cs:77` PrivateCode.onSubmit → OpenRoomFlow(false) (one of only 2 onSubmit users)
- `Assets/Scripts/UI/UI51FriendsView.cs:91` nameInput.onSubmit → AddFriend (the other one)
- `Library/PackageCache/com.unity.textmeshpro@3.0.7/Scripts/Runtime/TMP_InputField.cs:1570-1577, 4211-4222` VERIFIED: soft keyboard Status.Done → OnSubmit(null) → SendOnSubmit, then OnDeselect(null) in the same frame. On desktop, Enter also fires onSubmit (line 2168).
- `Library/PackageCache/com.unity.textmeshpro@3.0.7/Scripts/Runtime/TMP_InputField.cs:4086-4087` TouchScreenKeyboard.Open(text, type, autocorrect, multiline, secure, alert, placeholder, limit): there is no return-key parameter

**Fix sketch.** In UI51Input, subscribe m_Field.onSubmit to FocusNext in OnEnable and unsubscribe in OnDisable. FocusNext: next = the TMP_InputField after this one in block.GetComponentsInChildren<TMP_InputField>(false). If there is one, wait one frame (TMP calls OnDeselect right after onSubmit), then next.Select() and next.ActivateInputField(). The I3 lift follows the newly focused field and must not drop in between. Fields that already submit (Code, Friends Name, DeleteAccount Input) are alone in their block, so nothing changes for them.

**Files.** Assets/UI51/Scripts/Components/UI51Input.cs

**Risk.** The keyboard closes and reopens between fields on iOS, which can flicker; this is acceptable without a native plugin. An event-subscription leak is possible if OnDisable does not remove the listener; the existing Focus/Blur pattern in UI51Input shows the right way.

**Decision.** (a) Accept the system return-key label (Fine/Invio) with 'go to next field' behaviour, or approve a native iOS/Android plugin for a real 'Avanti' label? (b) On the last field (Login password, Conferma), should return also press ACCEDI/REGISTRATI?

**Test plan.** Play Mode in the Editor: on Login, type in Email and press Enter; Password gets focus. On Registrazione, Enter moves Nome utente → Email → Password → Conferma. Device: the same with the return key, and the field stays visible (with I3).

**Verifier (confirmed).** Navigation.Mode.None is set at UI51AccessBuilder.cs:704-706. The only onSubmit users are QuickSelectionPanels.cs:77 and UI51FriendsView.cs:91 (grep of all of Assets/Scripts, UI51 and UIV2; no onEndEdit users). On device, when the soft keyboard reports Status.Done, TMP calls OnSubmit(null) and then OnDeselect(null) in the same LateUpdate (TMP_InputField.cs:1570-1577). isKeyboardUsingEvents() returns false on Android/iOS (:479-500), so this path also runs once in-place editing is on (I4). On desktop, SendOnSubmit is followed by DeactivateInputField in the same frame (:2163-2168). The one-frame delay in the fix sketch is therefore needed on both paths. TouchScreenKeyboard.Open has no return-key parameter (:4086-4087).

**Fix concerns.** OnDeselect(null) in the submitting field calls DeactivateInputField, which sets m_SoftKeyboard.active=false. If next.ActivateInputField() ran in the same frame, it would be closed again; the one-frame delay is required, not optional. Enter on the last field (Login Password, Conferma) does nothing until decision (b) is made.

### N1 — feature_missing · verifier: confirmed · size S

**Current behaviour.** Registrazione checks only that the nickname (PlayFab Username) has at least 3 characters. Whether it is already taken is known only after REGISTRATI, from AddUsernamePassword's UsernameNotAvailable message, which also suffers from the I2 accent bug. The field has no 20-character limit, although PlayFab caps usernames at 3-20.

**Root cause.** No availability lookup is implemented. The PlayFab Client API can do it: GetAccountInfo accepts a Username to find another account, and the bootstrap guest session is already logged in while the form is open.

**Evidence.**
- `Assets/UIV2/Scripts/Core/AuthScreensV2.cs:101-104, 283-303` onValueChanged → RefreshRegisterForm; the only username rule is Length >= 3
- `Assets/Scenes/MainMenu.unity` RegisterPanel/…/Username: m_CharacterLimit 0, ContentType Standard
- `Assets/Scripts/Auth/AuthUIController.cs:243-277` Uniqueness is enforced only server-side by AddUsernamePassword; the error is mapped through GetUserFriendlyError:414 (CP1252 string)
- `Assets/PlayFabSDK/Client/PlayFabClientModels.cs:1610-1629` VERIFIED in SDK: GetAccountInfoRequest has Username/Email/TitleDisplayName lookup fields
- `context7:/microsoftdocs/playfab-docs AddUsernamePasswordRequest` VERIFIED: username 3-20 chars, password 6-100; display name 3-25

**Fix sketch.** In AuthScreensV2: in Awake set RegisterUsername.characterLimit = 20. In RefreshRegisterForm, when the username is 3-20 chars and changed, CancelInvoke and then Invoke(CheckName, 0.6f) to debounce and avoid the 1199 rate limit. CheckName: if PlayFabClientAPI.IsClientLoggedIn(), call GetAccountInfo{Username=name}. Success means taken: set nameTaken = true and show 'Nome utente già in uso'. AccountNotFound means free: show 'Nome disponibile' in the info colour. Ignore other errors. Drop a response whose name no longer matches the field. Add '&& nameTaken != true' to the 'complete' check. AddUsernamePassword stays the authoritative check because of races.

**Files.** Assets/UIV2/Scripts/Core/AuthScreensV2.cs

**Risk.** Calls are made from the guest session (the API policy allows GetAccountInfo by default; to verify). Whether lookups ignore case is unknown, so 'Mario' vs 'mario' must be tested. The single status label is shared with the other form messages, so the order of messages must be decided in RefreshRegisterForm.

**Decision.** Also check email availability live? It reveals whether an email is registered, which goes against the privacy choice in UI51RecoveryView (lines 14-15). Proposal: check the username only.

**Test plan.** Play Mode in the Editor (the bootstrap session is live): type an existing username, then 'già in uso' appears and REGISTRATI stays off; type a random one, then 'disponibile' appears; a 21st character cannot be typed. Repeat with different capitalisation of an existing name.

**Verifier (confirmed).** RefreshRegisterForm (AuthScreensV2.cs:283-303) checks only username.Length >= 3. onValueChanged is wired at :101-104. The Register Username field has m_CharacterLimit 0; the scene has 7 fields with 0 and one each with 12, 25 and 5. Uniqueness is enforced only by AddUsernamePassword (AuthUIController.cs:243-277). The SDK has GetAccountInfoRequest.Username (PlayFabClientModels.cs:1626-1629) and AddUsernamePasswordRequest documents 'PlayFab username (3-20 characters)' (:160). A bootstrap guest session exists while the form is open (AuthBootstrapper AuthenticationFlow), and OnLoginClicked already guards the in-flight case.

**Fix concerns.** Before calling GetAccountInfo, validate characters locally. The app's own mapping says usernames must be letters and digits only (PlayFabAuthService.cs:415-416, InvalidUsername). Otherwise a name like 'mario rossi' could come back AccountNotFound, show 'Nome disponibile', and then fail at REGISTRATI. The lookup runs from whatever account the bootstrap session holds (a per-device account when HasRealLogin=1, see cross_cutting); it works, but it ties the check to that session being up.

### N2 — works_as_designed · verifier: confirmed · size S

**Current behaviour.** Today nicknames cannot be duplicated. The nickname is the PlayFab Username, unique per title, set at registration; the display name is then set to the same string. Login by nickname (2.53) uses LoginWithPlayFab(Username). The technical identity is already the PlayFab ID everywhere. There is no in-app rename. Guests are 'Ospite' plus 4 hex characters of their PlayFab ID, which can collide but only matters at a table.

**Root cause.** No defect in the uniqueness itself. There are two gaps: (1) UpdateUserTitleDisplayName after registration is fire-and-forget, so a failure (NameNotAvailable 1058, ProfaneDisplayName 1234, network) leaves an account with no display name for the rest of the session, and 'Aggiungi amico per nome' cannot find it; (2) PhotonNetwork.NickName is set only when Photon connects, so a player who has just registered keeps appearing as 'Ospite XXXX' to others until the next login or restart. Whether display names are unique is a PlayFab title setting that is not visible in the repo. Per the PlayFab docs, if non-unique display names are enabled, lookups by display name always return AccountNotFound, which would break FriendsService.AddFriendByName.

**Evidence.**
- `Assets/Scripts/Auth/AuthUIController.cs:243-261` AddUsernamePassword(Username, Email, Password), then bs.PlayFabAuth.UpdateDisplayName(username) with no onError
- `Assets/Scripts/Auth/PlayFabAuthService.cs:274-301` UpdateDisplayName: logs error.ErrorMessage; the only caller is AuthUIController:260
- `Assets/Scripts/Auth/PlayFabAuthService.cs:347, 388-393` Login by nickname = LoginWithPlayFab{Username} when there is no '@', so it depends on the username, not the display name
- `Assets/Scripts/Auth/PlayFabAuthService.cs:130-146` GetBestDisplayName: DisplayName, else 'Ospite '+4 hex of PlayFabId
- `Assets/Scripts/Auth/FriendsService.cs:66-77` AddFriendByName uses FriendTitleDisplayName, so it depends on display-name uniqueness; AccountNotFound → 'Nessun giocatore con questo nome.'
- `Assets/Scripts/Auth/AuthBootstrapper.cs:99, 159-168, 227` OnDisplayNameChanged → PublishLook sets custom props only, never NickName; Photon identity = LocalPlayer.UserId == PlayFabId
- `Assets/Scripts/Auth/PhotonAuthConnector.cs:136-140` The only place NickName is set (plus clearing at AuthBootstrapper.cs:287)
- `Assets/Photon/PhotonRealtime/Code/Player.cs:84-97` VERIFIED in local PUN source: setting the local NickName calls SetPlayerNameProperty, so it syncs to the room
- `Assets/UIV2/Scripts/Screens/ProfileEditorV2.cs:17-49` The profile editor has avatar, frame and banner only; no nickname edit exists
- `Server/CloudScript/51.js:406, 469` CloudScript reads only UserInfo.Username (to tell guests apart); no display-name logic, no uniqueness logic
- `context7:/microsoftdocs/playfab-docs GetAccountInfoRequest.titleDisplayName` VERIFIED: with non-unique Title Display Names enabled, lookups by Title Display Name always return AccountNotFound

**Fix sketch.** Keep the current model: nickname = unique PlayFab Username, PlayFab ID as identity. Small fixes: (1) AuthBootstrapper.cs:99 handler also sets PhotonNetwork.NickName = PlayFabAuth.GetBestDisplayName(), one line, and PUN syncs it to the room. (2) Self-heal: in the LoginWithEmail success path, if profile?.DisplayName is empty and accountInfo.Username exists, call UpdateDisplayName(Username). Pass an onError at AuthUIController:260 that logs. (3) Optional: AddFriendByName uses FriendUsername instead of FriendTitleDisplayName; it is unique by construction and immune to the title display-name setting. A '#1234' discriminator would only be needed if nicknames become editable or non-unique; it is not needed today.

**Files.** Assets/Scripts/Auth/AuthBootstrapper.cs, Assets/Scripts/Auth/PlayFabAuthService.cs, Assets/Scripts/Auth/AuthUIController.cs, Assets/Scripts/Auth/FriendsService.cs (optional)

**Risk.** The NickName setter while joining or leaving a room: PublishLook already guards with CurrentRoom != null && !InRoom (AuthBootstrapper.cs:164), so reuse that guard. Switching friend lookup to FriendUsername breaks only if some legacy account's display name differs from its username (none can via the app).

**Decision.** Are nicknames unique (status quo, recommended), or duplicable with a discriminator (needs a new display-name format, friend search by code or username, and the PlayFab title setting changed)? Will a 'change nickname' feature ever exist? If yes, decide whether login-by-nickname keeps using the original username.

**Test plan.** Play Mode in the Editor with a test account on the user's title: register from a guest session and check that PhotonNetwork.NickName changes from 'Ospite XXXX' to the username (log). Two devices: a guest registers while in a private-room lobby, and the other phone's lobby shows the new name. Check the Game Manager setting 'Allow non-unique player display names'.

**Verifier (confirmed).** Confirmed. Registration calls AddUsernamePassword and then a fire-and-forget UpdateDisplayName(username) (AuthUIController.cs:243-261). UpdateDisplayName's only caller is :260, and on error it just logs (PlayFabAuthService.cs:274-301). Login by nickname uses LoginWithPlayFab{Username} (:388-393). GetBestDisplayName falls back to 'Ospite '+ShortId (:130-146). AddFriendByName uses FriendTitleDisplayName (FriendsService.cs:66-72). The only NickName writers are PhotonAuthConnector.cs:139 and AuthBootstrapper.cs:287; OnDisplayNameChanged → PublishLook sets only custom properties (AuthBootstrapper.cs:99, 159-168). Registration triggers no Photon rebind; only login does, via RebindPhoton at AuthUIController.cs:311. CloudScript reads UserInfo.Username only (51.js:406, 469). The SDK confirms that title-display-name lookups return AccountNotFound when non-unique names are enabled (PlayFabClientModels.cs:1621-1624). Two corrections. The SDK says usernames and emails are unique 'in the PlayFab service' (PlayFabClientModels.cs:166-169), which per PlayFab means the studio namespace, not just the title. Also, the local name is not broken after a display-name failure on the next login, because LoginWithEmail falls back to accountInfo.Username (:357); only the server-side display name (friend lookup, leaderboards with ShowDisplayName, LeaderboardService.cs:75) stays empty.

- `Assets/Scripts/Auth/PlayFabAuthService.cs:357` DisplayName = profile?.DisplayName ?? accountInfo?.Username: the local name recovers on the next login even when the PlayFab display name is missing
- `Assets/Scripts/Auth/LeaderboardService.cs:75` Leaderboards show DisplayName, so an account whose UpdateDisplayName failed shows no name there

**Fix concerns.** Setting PhotonNetwork.NickName inside the OnDisplayNameChanged handler (AuthBootstrapper.cs:99) causes two problems. (a) LogoutAndRestart clears NickName at :287 and then calls PlayFabAuth.Logout(), which fires OnDisplayNameChanged(null) (PlayFabAuthService.cs:310), so the handler would at once overwrite the deliberate clear with 'Ospite <deviceId>'. (b) The LoginWithEmail success lambda fires OnDisplayNameChanged (:359) before onSuccess → RebindPhoton (AuthUIController.cs:311), so the old guest Photon peer would be renamed to the new account's name before it disconnects. Guard the setter with PhotonOnThisAccount, i.e. IsConnectedAndReady && LocalPlayer.UserId == PlayFabId (AuthBootstrapper.cs:226-227), and skip null names.

### Missed by the investigator (found by the verifier)

- **I2**: MatchmakingManager.cs is a mixed-encoding file: one comment is already valid UTF-8 and the rest is CP1252. The I2 fix one-liner (decode cp1252 → encode utf-8 on the whole file) would double-encode that comment, and the new UTF-8 test would not catch it. Re-encode line by line.
  - `Assets/Scripts/Networking/MatchmakingManager.cs:299` Bytes C3 A0 ('à' in UTF-8) inside a comment
  - `Assets/Scripts/Networking/MatchmakingManager.cs:479, 500` CP1252 bytes E8/E0/F9 (500 is a string literal)
- **I3**: The I3 list of covered fields is wrong for 'Aggiungi amico'. The Friends Add panel is anchored at the top (y 80-180 of 844), so the soft keyboard cannot cover it, and a keyboard lift will not fix a 'ricerca amici' complaint. Ask the tester for a screenshot; the cause there may be the I4 preview box or something else.
  - `Assets/UI51/Editor/UI51SocialBuilder.cs:443-466` addPanel = TopBand(..., 80f, 100f); the Name input is inside it
- **I4**: Once I4 lands (in-place editing), OnFocusSelectAll=1 on all 10 fields makes TMP paint its gold selection box (GoldA 0.35) over existing text on focus. This is a new 'rectangle over the input'. Consider turning onFocusSelectAll off in the same change.
  - `Assets/Scenes/MainMenu.unity` m_OnFocusSelectAll: 1 on all 10 TMP_InputFields
  - `Assets/UI51/Editor/UI51AccessBuilder.cs:715` selectionColor = GoldA(0.35f)
  - `Library/PackageCache/com.unity.textmeshpro@3.0.7/Scripts/Runtime/TMP_InputField.cs:3506-3508` Caret/selection drawing is skipped only when InPlaceEditing() is false (the current state)
- **N2**: The N2 NickName fix as sketched (set NickName in the OnDisplayNameChanged handler) fights LogoutAndRestart's explicit NickName clear. It also renames the old Photon identity during email login, because OnDisplayNameChanged fires before RebindPhoton.
  - `Assets/Scripts/Auth/AuthBootstrapper.cs:287, 293` NickName = string.Empty, then PlayFabAuth.Logout()
  - `Assets/Scripts/Auth/PlayFabAuthService.cs:310, 359` Logout and the LoginWithEmail success path both fire OnDisplayNameChanged
  - `Assets/Scripts/Auth/AuthUIController.cs:311` RebindPhoton runs only after onSuccess, i.e. after the event
- **I1**: The cross-cutting 'playfab-login-lifecycle' claim is correct in substance, but 'never happens' is too strong. The 'returning registered user' branch is reached if a user with HasRealLogin=1 picks Registrazione on the forced cold-start screen: AddUsernamePassword then runs on the per-device account that the bootstrap reuses. There is no LinkCustomID anywhere in Assets/Scripts, UI51, UIV2 or 51.js, so the common path (register from an ephemeral session guest, or log in by email) does land on an unlinked device account at the next cold start.
  - `Assets/Scripts/Auth/PlayFabAuthService.cs:157-160, 194-200` Device ID when HasRealLogin; the returning-user branch needs a username on that account
  - `Assets/Scripts/Auth/AuthBootstrapper.cs:113-118, 322-340` No guest-id reset when HasRealLogin; the bootstrap always logs in as guest when not logged in
  - `Assets/UIV2/Scripts/Core/StartScreenV2.cs:27-34, 43` The Login screen is forced at every cold start, so Register is reachable on the device account
- **N1**: N1: the live availability check needs a local character and length rule before calling PlayFab. Otherwise names that PlayFab will reject (InvalidUsername) get reported as 'disponibile'.
  - `Assets/Scripts/Auth/PlayFabAuthService.cs:415-416` InvalidUsername → 'Username non valido (usa solo lettere e numeri)'
  - `Assets/UIV2/Scripts/Core/AuthScreensV2.cs:290-297` Only a length rule today

### Cross-cutting notes

- **other** (I2, room-code / private-room error items (other cluster)): Source-file encoding: 6 runtime .cs files are Windows-1252 without a BOM. On a Mac or cloud iOS build, their accented literals turn into U+FFFD, which TMP draws as □. Outside this cluster this hits the private-room join errors (32765 room full, 32764 game started or room closed, invalid code). Whoever owns room-code or matchmaking items should expect squares there on iOS. Fixing I2 (re-encode plus the UTF-8 test) fixes them too.
  - `Assets/Scripts/Networking/MatchmakingManager.cs:500` 'La stanza è piena…', 'La partita è già iniziata…', 'non più disponibile' in an ISO-8859 file
  - `Assets/Scripts/Auth/PlayFabAuthService.cs:412, 414` Same encoding
- **mobile-keyboard** (I3, I4, I5, delete-account / private-room / friends keyboard items (other clusters)): Every input in the app (10) is built by UI51AccessBuilder.BuildInput and carries UI51Input, including the delete-account confirm, room code, friend search and password recovery fields. Any keyboard, overlay or return-key item in other clusters (delete account, private room, friends) has this same root cause and the same fix point. DeleteAccountModalV2 has its own lift that must be removed when the generic one lands, or the Card will be lifted twice.
  - `Assets/UI51/Editor/UI51AccessBuilder.cs:679-724` BuildInput
  - `Assets/UIV2/Scripts/Core/DeleteAccountModalV2.cs:189-219` Private keyboard lift
- **session-state** (N2, lobby/table player-name items (other clusters)): PhotonNetwork.NickName is never refreshed after a display-name change. After registering from a guest session, the player plays the rest of that session as 'Ospite XXXX' in lobbies, seat cards and table banners. Any 'wrong or old name at the table/lobby' item in other clusters likely shares this cause.
  - `Assets/Scripts/Auth/PhotonAuthConnector.cs:136-140` The only NickName setter, used only on connect
  - `Assets/Scripts/Auth/AuthUIController.cs:258-261` Registration updates the PlayFab display name only
  - `Assets/UIV2/Scripts/Core/RoomFlowV2.cs:365, 430` Lobby names read players[i].NickName
  - `Assets/UIV2/Scripts/Core/GameSocialV2.cs:137` Table names read p.NickName
- **playfab-login-lifecycle** (I1, I3, session/profile-after-restart items (other clusters)): On a device that has logged in before (HasRealLogin=1), the startup bootstrap logs in with LoginWithCustomID(deviceUniqueIdentifier, CreateAccount=true). That custom ID is never linked to the registered or email account (no LinkCustomID anywhere): registration happens on the per-session guest ID, and email login is a separate account. So the 'returning registered user' branch is effectively dead. Each cold start silently uses a blank per-device account while the HasRealLogin and IsRegistered flags stay 1. The app copes only because the Login screen is forced at every cold start, which means retyping the password every time and raises how often I1 (throttling) and I3 (keyboard) are hit. Other clusters may see this as 'profile/progress missing until I log in' or 'guest progress on a strange account'.
  - `Assets/Scripts/Auth/PlayFabAuthService.cs:157-180` LoginAsGuest: device ID when HasRealLogin, else session guest ID; CreateAccount=true
  - `Assets/Scripts/Auth/PlayFabAuthService.cs:194-212` The 'returning registered user' branch needs a username on the device account, which never happens
  - `Assets/Scripts/Auth/AuthBootstrapper.cs:113-121, 322-338` Start: no guest-ID reset when HasRealLogin; bootstrap always does a guest login
  - `Assets/UIV2/Scripts/Core/StartScreenV2.cs:27-35, 43` Every cold start shows the Login panel
- **text-content** (I1): Login says 'Account non trovato' (AccountNotFound), which reveals whether an account exists. Password recovery deliberately answers the same way for known and unknown emails to avoid that leak. The two screens contradict each other.
  - `Assets/Scripts/Auth/PlayFabAuthService.cs:417-418` AccountNotFound → 'Account non trovato'
  - `Assets/Scripts/UI/UI51RecoveryView.cs:14-15, 107-110` Same answer whether the address has an account or not

## Tutorial / audio v07 / opzioni vibrazione e grafica ridotta

### TU1 — root_cause_confirmed · verifier: confirmed · size S

**Current behaviour.** The rule is wrong in the code as well as in the texts. A match ends as soon as one competitor is alone in the lead with total >= 51, so exactly 51 wins. Nowhere does an exact 51 drop back to 0. The tutorial (title 'Arriva a 51' and 'Chi arriva per primo a 51 vince') and the Rules page Punteggio intro ('Vince chi arriva per primo a 51', baked into MainMenu.unity) say the same wrong thing. The results screen's race note ('N punti alla vittoria') and the tie check use >= target, so they are wrong under the real rule.

**Root cause.** MatchScore.IsFinished uses `best >= target`, and MatchScore.ContinueMatch carries the raw totals into the next smazzata without resetting an exact-target total to 0. Rules51, RoundManager and PunteggioManager contain no target logic at all (the only '51' hits are comments). Match points are added to Players[].TotalScore only at smazzata end (RoundManager.cs:392), so an exact 51 can only happen at smazzata end. That makes the reset unambiguous to place in ContinueMatch. The texts were written from the same misunderstanding (UI51RulesBuilder says 'Testi del mockup, controllati sul codice delle regole', and that code was wrong).

**Evidence.**
- `Assets/Scripts/Core/MatchScore.cs:44-50` IsFinished: `return best >= target && totals.Count(t => t == best) == 1;` -> exact 51 wins
- `Assets/Scripts/Core/MatchScore.cs:66-82` ContinueMatch: `next.MatchTotals = Totals(previous);` raw carry-over, no 51->0
- `Assets/Scripts/Core/RoundManager.cs:392` TotalScore += Points + AccusiPoints only at round end (cappotto 238/308/377): totals never change mid-smazzata
- `Assets/Scripts/Gameplay/TurnController.cs:592` ContinueMatch(previous, gameState, cfg.TargetScore or 51): run by master online; MatchTotals travels inside the state so all clients agree
- `Assets/UIV2/Scripts/Core/MatchResultsV2.cs:94-101` finished = IsFinished(state,target); winner = order[0] by raw totals: stays correct under the new rule (an entry at exactly 51 is always below any entry >51)
- `Assets/Scripts/UI/UI51TutorialView.cs:34` step title "Arriva a 51"
- `Assets/Scripts/UI/UI51TutorialView.cs:46` "... Chi arriva per primo a 51 vince."
- `Assets/UI51/Editor/UI51RulesBuilder.cs:88` Punteggio Intro "Vince chi arriva per primo a 51." (no row about 51 esatti -> 0)
- `Assets/Scenes/MainMenu.unity:155984-155985` baked text of the Rules page (needs Fase 12 rebuild, not hand-YAML)
- `Assets/Scripts/Core/ResultsSheet.cs:73-79` RaceNote: left = target - score -> at 50 says '1 punto alla vittoria' but +1 would make exactly 51 = back to 0
- `Assets/Scripts/UI/UI51ResultsView.cs:230` tiePending uses totals[leader] >= target: two players at exactly 51 would show 'Parità a 51! Avete superato 51' (241-243) instead of both going to 0
- `Assets/Scripts/UI/UI51ResultsView.cs:222-226` race bars/labels show raw 'after /51'; no 51->0 moment exists
- `Assets/Scripts/UI/TableTopBarController.cs:73` pill 'A 51' (UI51TableBuilder.cs:105): ambiguous wording, user decision
- `Assets/Tests/Editor/UI51ResultsTests.cs:56-59` pins the old RaceNote ('1 punto alla vittoria' at 50) -> must change
- `Assets/Tests/Editor/MatchScoreTests.cs:100-164` existing finish/carry/tie tests stay valid (53, 52/52, 1000); no test for exact 51
- `Design/51_handoff/51_handoff/SPEC.md:156` design doc also says 'arrivare a 51' (source of the wrong texts)
- `Assets/Scripts/Auth/PhotonAuthConnector.cs:51` Photon AppVersion = Application.version = 1.0.0 for every TestFlight build: an old build (>=51 wins) and a new build could share a room and disagree on IsFinished

**Fix sketch.** Core (one file): MatchScore.IsFinished -> `best > target`; ContinueMatch -> `next.MatchTotals = Totals(previous).Select(t => t == target ? 0 : t).ToArray()` (cappotto 1000 unaffected). ResultsSheet.RaceNote -> `left = target + 1 - score`, plus a note when a total is exactly target ('51 esatti: si riparte da 0'). UI51ResultsView.cs:230 `> target`, and a race-row label '51 -> 0' when after == target. Texts: UI51TutorialView Titles[5]/Texts[5] ('Supera il 51' / '... vince chi supera 51; con 51 esatti si torna a 0'), UI51RulesBuilder.cs:88 intro plus a Punteggio row, then run Tools/UI51/Build Fase 12 (it rewrites MainMenu and GameScene). Tests: new MatchScoreTests (exact 51 resets, 52 wins, 51 vs 53 -> 53 wins, 51/51 -> both 0 and continue, 2v2 team total 51 -> 0), update UI51ResultsTests RaceNote. Gate builds so old and new rule clients cannot match (see cross_cutting).

**Files.** Assets/Scripts/Core/MatchScore.cs, Assets/Scripts/Core/ResultsSheet.cs, Assets/Scripts/UI/UI51ResultsView.cs, Assets/Scripts/UI/UI51TutorialView.cs, Assets/UI51/Editor/UI51RulesBuilder.cs, Assets/Tests/Editor/MatchScoreTests.cs, Assets/Tests/Editor/UI51ResultsTests.cs

**Risk.** Online consistency: every client computes IsFinished locally from the shared state, so mixed builds (same AppVersion 1.0.0) would desync the end of the match. The Fase 12 builder rebuilds both scenes, including the GameScene tutorial (destructive-builder risk, use the unity-builder skill). CirullaAI does not know about the 51 trap (Easy is random anyway). The race bars clamp after/target, so a reset row needs its own label. The tie screen text changes.

**Decision.** Confirm: the reset applies only at smazzata end (that is the only time points are added); in 2v2 it applies to the team total and in 1v3 to each player; two or more at exactly 51 all go to 0; the pill reads 'A 51' or 'OLTRE 51'; show a '51 -> 0' moment on the results screen.

**Test plan.** EditMode: MatchScoreTests for exact-51 reset / >51 win / ties; UI51ResultsTests RaceNote. Play Mode in Editor: force MatchTotals (e.g. 45/40) with the reseed recipe and play a smazzata to land exactly on 51; check results, next-smazzata pill = 0 and the tie screen. Device: one online match between two phones on the same build.

**Verifier (confirmed).** I checked the mechanism myself. MatchScore.IsFinished returns `best >= target && unique` (Assets/Scripts/Core/MatchScore.cs:49), so exactly 51 wins. ContinueMatch carries the raw totals forward (MatchScore.cs:79) with no 51->0 step. Rules51, PunteggioManager and RoundManager contain no target logic: grep for '51' outside namespaces finds nothing. TotalScore changes only at EndSmazzata (RoundManager.cs:392) or through the 1000-point cappotto / Tre assi paths (RoundManager.cs:238, 308, 377), so a reset placed in ContinueMatch is unambiguous. Every IsFinished consumer goes through the same function (MatchResultsV2.cs:96, 129, 151, 234, 350; TurnController.cs:592), so changing it once covers all callers. The server trusts the client's 'vinta' flag (Server/CloudScript/51.js:393, 407) and has no target logic, so nothing server-side guards the rule. The wrong texts are confirmed: UI51TutorialView.cs:34 and :46 (runtime arrays, no rebuild needed), UI51RulesBuilder.cs:88, baked at MainMenu.unity:155984-155985. Also confirmed: RaceNote `left = target - score` (ResultsSheet.cs:75-77) and tiePending `>= target` (UI51ResultsView.cs:230). The existing MatchScoreTests (53, 52/52, 1000) and RulesDecisionsTests.cs:168 (1000) stay valid under `>`. The cross-cutting Photon point is also confirmed: AppVersion = Application.version (PhotonAuthConnector.cs:51), bundleVersion is 1.0.0 (ProjectSettings.asset:143), and the room has no rules or protocol property (MatchmakingManager.cs:41 only has format/target).

**Corrected cause.** As stated. Two small corrections. (1) The UI51ResultsTests RaceNote test (UI51ResultsTests.cs:56-59) changes in every numeric assertion under `target+1-score` (17->18, 1->2, 11->12), not just the '1 punto' case. (2) RaceNote's `left <= 0` branch prints 'pari: si gioca ancora' (ResultsSheet.cs:76). Today that already fires for a lone player at exactly 51 in the 2-player view; after the fix it needs its own '51 esatti -> 0' text.

- `Assets/UI51/Editor/UI51RulesBuilder.cs:113` Rules > Formati row 'Pareggio: se due giocatori sono in testa oltre 51 si gioca un'altra smazzata' (baked at MainMenu.unity:185754). The investigator did not list it. Its wording already matches the new rule but contradicts today's code, where a tie at exactly 51 also replays.
- `Assets/Scripts/Core/MatchConfig.cs:30, 159, 178` TargetScore is configurable and persisted in PlayerPrefs (a test uses 71), and it travels as room property 'target' (MatchmakingManager.cs:333, 443). The reset must compare against `target`, not a literal 51, as the sketch does.
- `Assets/Scripts/Core/ResultsSheet.cs:76` 'pari: si gioca ancora' when left <= 0
- `Assets/Tests/Editor/RulesDecisionsTests.cs:168` IsFinished on a 1000 Tre assi total: unaffected by `>`

**Fix concerns.** The sketch is sound and goes through the single shared function. Also review the Formati 'Pareggio' row (UI51RulesBuilder.cs:113). In 1v3 the four-player RaceNote counts from the leader, so if the leader sits at exactly 51 the note should refer to the next player. The tie text baked at UI51ResultsBuilder.cs:465 / GameScene.unity:29351 is overwritten at runtime by BindTie (UI51ResultsView.cs:243), so it needs no rebuild. Only the Rules page needs the destructive Fase 12 rebuild; the tutorial strings are C#. Mixed builds really would desync the end of a match: IsFinished is evaluated locally on every client (MatchResultsV2.cs:96) while only the master runs ContinueMatch. Ship this together with a bundleVersion bump or a rules key in AppVersion.

### TU2 — feature_missing · verifier: confirmed · size L

**Current behaviour.** The tutorial is a real Training 1v1 match against the Easy bot with a fixed first deal (seed 759353). Before the first human move, Nonna Rosa shows 6 explanation steps (AVANTI/Indietro) while a veil blocks all taps. 'GIOCA LA PRIMA PARTITA' then hides the overlay and leaves a normal random match to 51 against a randomly playing bot, which can take many smazzate. No move is guided or blocked, no step happens after the first turn, and 51->0 and >51 are never shown. The Welcome promises 'Una partita guidata di 3 minuti'.

**Root cause.** The design only covers the first board state. Only the deal is deterministic: the deck is shuffled once per smazzata, so the seed fixes every hand of smazzata 1. The bot (CirullaAI Easy) picks moves at random with an unseeded System.Random, and the human is free after step 6. So the table diverges from any script after the first move. The match starts at 0-0 (ContinueMatch zeroes the totals), so a 5-8 min tutorial cannot reach 51.

**Evidence.**
- `Assets/Scripts/UI/UI51TutorialView.cs:56-67` Launch: Training 1v1 Easy + GameSceneInitializer.Tutorial = true
- `Assets/Scripts/UI/UI51TutorialView.cs:93-110` coach starts once at the first human turn after deal/accuso window; `if (step >= 0) return;` = never again
- `Assets/Scripts/UI/UI51TutorialView.cs:89` play button just SetActive(false): rest is a free full match
- `Assets/Scripts/UI/UI51TutorialView.cs:145-247` reusable: Target() spotlights real CardViews/UI rects, MoveSpot/finger/bubble/dots
- `Assets/Scripts/Gameplay/GameSceneInitializer.cs:29-38, 470-474` TutorialSeed 759353, Reseed before StartNewGame then TickCount (later smazzate random)
- `Assets/Scripts/Core/Rules51.cs:9-12, 116-161` static Rng + Reseed; deck shuffled once in DealInitialCards; CreateNewGame(n, providedDeck) still shuffles (37-54)
- `Assets/Scripts/Core/RoundManager.cs:325-337` later hands drawn from state.Deck without reshuffle -> the whole first smazzata is fixed by the seed
- `Assets/Tests/Editor/Rules51CoreTests.cs:354-372` test pins the seed deal: pattern for pinning a longer scripted deal
- `Assets/Scripts/Core/CirullaAI.cs:25-31, 59-61` rng = new System.Random() unseeded; Easy = random choice -> bot not scriptable
- `Assets/Scripts/Gameplay/TurnController.cs:1145` ExecuteMove: single entry for every local human/AI move -> hook point for a tutorial move gate
- `Assets/Scripts/Gameplay/TurnController.cs:1712-1745` ExecuteAITurn -> cirullaAI.ChooseMove at 1731: hook point for a scripted bot move
- `Assets/Scripts/Gameplay/TurnController.cs:284` public event OnMoveExecuted: tutorial can advance its script per move
- `Assets/Scripts/Gameplay/MoveSelectionUI.cs:235` existing UiError feedback for a refused choice
- `Assets/Scripts/Core/MatchScore.cs:73-77` first smazzata MatchTotals = zeros: a tutorial would preset totals (offline only) after StartNewGame
- `Assets/Scripts/Gameplay/TurnController.cs:163, 219` aiMoveDelay 2 s, accuso window 5 s per deal: 1v1 smazzata = 6 deals, 36 moves -> ~5-7 min total, fits 5-8 min
- `Assets/UI51/Editor/UI51RulesBuilder.cs:190` Welcome promises 'Una partita guidata di 3 minuti'

**Fix sketch.** Feasible inside the current architecture, with no new systems. (1) Deal: keep the seed approach and find a seed with a test that pins the whole scripted first smazzata (including an accuso hand), or add one static Rules51 'stacked next deck' consumed instead of ShuffleDeck (offline only). (2) In TurnController (Gameplay asmdef, the UI sets them): a static `Func<Move,bool> TutorialGate` checked in ExecuteMove for offline local human moves (refuse = UiError + coach hint), and a static scripted-bot override before cirullaAI.ChooseMove in ExecuteAITurn; both null outside the tutorial and reset at SubsystemRegistration like GameSceneInitializer.Tutorial. (3) UI51TutorialView: replace the fixed Titles/Texts with a per-move script driven by OnMoveExecuted, reusing Show/Target/MoveSpot. Free turns = gate returns true for any move. (4) Score story in ONE smazzata: preset MatchTotals after StartNewGame (e.g. bot 45, player 44) so the bot lands exactly on 51 -> 0 and the player goes >51 and wins (needs TU1). Free turns change captures, so either keep the free turns on score-neutral hands or adjust the preset totals at the last hand.

**Files.** Assets/Scripts/UI/UI51TutorialView.cs, Assets/Scripts/Gameplay/TurnController.cs, Assets/Scripts/Gameplay/GameSceneInitializer.cs, Assets/Scripts/Core/Rules51.cs, Assets/Tests/Editor/Rules51CoreTests.cs, Assets/UI51/Editor/UI51RulesBuilder.cs

**Risk.** Gate and override statics must never leak into normal or online matches (reset on scene load and at SubsystemRegistration; offline only). A seed is fragile: any change to Rules51 shuffling or dealing breaks the script (the existing test already guards this). The tutorial match currently counts as a real Training match (premioPartita, stats), and Abbandona records a loss. Depends on TU1.

**Decision.** Script content: who experiences 51->0 (the bot or the player), which turns are free (2-3), whether preset starting totals are acceptable, whether the bot plays scripted moves, and whether the tutorial match should count in stats/coins.

**Test plan.** EditMode: seed/stacked-deck test that asserts every hand of the scripted smazzata; a test that replays the script moves through RoundManager and asserts the final totals (bot 51 -> 0, player > 51). Play Mode in Editor: run the tutorial end to end with the Simulator, try wrong cards (refused + hint), and time it (5-8 min target).

**Verifier (confirmed).** Confirmed that only the deal is deterministic. GameSceneInitializer.cs:470-474 reseeds before StartNewGame and back to TickCount after it. Both CreateNewGame(2) (Rules51.cs:27-34) and StartSmazzata->DealInitialCards (RoundManager.cs:93) shuffle synchronously inside StartNewGame (TurnController.cs:589-602), and mid-smazzata hands come from state.Deck without reshuffling (RoundManager.cs:321-334). So the seed fixes the whole first smazzata's card order (pinned by Rules51CoreTests.cs:354-372). The bot is unseeded (CirullaAI.cs:31). Easy is random but with a 70% preference for captures (CirullaAI.cs:62-70), not purely random. The coach runs only once (`if (step >= 0) return;` UI51TutorialView.cs:95), and the play button only hides the overlay (:89). The hook points are real: ExecuteMove (TurnController.cs:1145) is the single entry for local moves (callers CardViewManager.cs:1528, 1541, 1611, 1627 and the network path), ExecuteAITurn calls ChooseMove at :1731, and OnMoveExecuted is at :284. Note it is also invoked with null at :95 and :2006, so a script must ignore null moves. The turn timer is online-only (TurnController.cs:401), so it won't interfere offline. The accuso window always runs a fixed 5 s per deal (TurnController.cs:1090-1098), and aiMoveDelay is 2 in the scene (GameScene.unity:27117). The 5-8 min estimate is plausible but unmeasured. The welcome text '3 minuti' is at UI51RulesBuilder.cs:190.

**Corrected cause.** As stated, plus one gap the investigator missed. The coach veil deliberately swallows every tap, including inside the spotlight hole (UI51RulesBuilder.cs:265: 'il velo col buco prende tutti i tocchi (anche nel buco)'). There is no ICanvasRaycastFilter anywhere in the project (grep: 0 hits). CardView refuses taps whenever the pointer is over UI (CardView.cs:239). So 'guide the move on the highlighted card' needs a raycast filter on the Spot that lets taps through the hole. Without it the gate in ExecuteMove never sees a tap while the coach is showing.

- `Assets/UI51/Editor/UI51RulesBuilder.cs:265-268` veil with hole is a full raycast target by design
- `Assets/Scripts/Gameplay/CardView.cs:239` IsPointerOverUI blocks card taps under any UI raycast target
- `Assets/Scripts/Core/CirullaAI.cs:62-70` Easy = 70% capture preference, otherwise random
- `Assets/Scripts/Gameplay/TurnController.cs:95, 2006` OnMoveExecuted(null) on resync/redeal

**Fix concerns.** (1) Scripting both events in one smazzata does not let the user actually live 51->0 under the TU1 fix. If the bot ends exactly on 51 while the player goes above 51 at the same smazzata end, IsFinished ends the match, and ContinueMatch (the only place the reset happens) never runs for a finished match. The '51 -> 0' would only be a label on the results screen. Really living it needs a second (shortened) smazzata, or a story where the 51->0 smazzata does not end the match. (2) Spot hole raycast pass-through is required (see above). (3) UI51TutorialView lives in Assembly-CSharp and can set statics on TurnController (Gameplay asmdef, autoReferenced) directly, so no reflection is needed. (4) Preset MatchTotals must be applied after StartNewGame and offline only, and the master's StartNewGame would overwrite them on any restart. (5) Results/abandon paths (MatchResultsV2.cs:298-326, 346-357, 449-454 Exit) treat the tutorial as a real training match.

### TU3 — feature_missing · verifier: confirmed · size M

**Current behaviour.** There is no tutorial reward. The Welcome shows 'No, insegnami' with 'Una partita guidata di 3 minuti' and no reward, and the Done screen has no reward row. The server has no tutorial handler or flag. The Welcome 'seen' state lives in device PlayerPrefs, not on the account. After skipping, the tutorial is reachable only from Impostazioni -> Regole e tutorial -> the tutorial button on the Rules page; there is no 'Aiuto' page and no Modalità -> Allenamento entry, which the SPEC asks for. The only thing the tutorial pays is the normal training-match reward, and only if the user plays the whole match to the end with an account.

**Root cause.** This was deliberately left out in Fase 12: the builder comments say the +200 coins and the Smeraldo back were removed because the server does not give them. Nothing records completion per account.

**Evidence.**
- `Assets/UI51/Editor/UI51RulesBuilder.cs:159, 237, 382` 'Il +200 della prima scelta non c'e''; 'Le ricompense del mockup (+200 monete, dorso Smeraldo) non ci sono: non le da' ancora il server'
- `Server/CloudScript/51.js:337-403` handlers inizio/statoPremi/riscattaPremio/riscattaPosta/premioPartita: none for the tutorial; grep 'tutorial' = 0 hits
- `Server/CloudScript/51.js:350-360` riscattaPremio = reusable once-per-key pattern (state check, saveReadOnly, grant)
- `Server/CloudScript/51.js:162-182` grant() supports only monete/gemme/forziere: no card-back item grant
- `Server/CloudScript/51.js:404-406` premioPartita: guests (no Username) get nothing; same rule would apply
- `Assets/Scripts/UI/UI51WelcomeView.cs:78-100` PlayerPrefs 'UI51WelcomeSeen' device-scoped: a second account on the same phone never sees the offer
- `Assets/UIV2/Scripts/Core/StartScreenV2.cs:55-61` Welcome opened at first Home entrance (guest or login)
- `Assets/UIV2/Scripts/Core/SettingsV2Integration.cs:73` 'Regole e tutorial' row -> UI51RulesView
- `Assets/Scripts/UI/UI51RulesView.cs:27` tutorial button -> UI51TutorialView.Launch (only other caller: UI51WelcomeView.cs:84)
- `Design/51_handoff/51_handoff/SPEC.md:156` SPEC: +200 monete e dorso Smeraldo, only on completion, replay from Regole e tutorial and Modalita' -> Allenamento
- `Assets/Scripts/Auth/RewardsService.cs:98-107, 184-188` client CloudScript call pattern and reward sound to reuse
- `Assets/UIV2/Scripts/Core/MatchResultsV2.cs:317-321, 346-357` tutorial match pays the normal training reward at match end; Abbandona -> MatchQuit counts a loss

**Fix sketch.** Server: `handlers.premioTutorial = phoneOnly(...)`, which requires a Username (account), reads an internal key 'Tutorial', returns {ok:false, gia:true} if it is set, otherwise saves it and grant()s eco.tutorial (TitleData, default [{tipo:'monete',quantita:200}]). Add `tutorial:true/false` to the `inizio` reply so the client knows whether the reward is still available. Add a test in Server/CloudScript/test.js. Client: RewardsService.ClaimTutorial, called from UI51TutorialView.Finish (or at the end of the TU2 scripted match), then PlaySound plus a toast. UI: the Welcome 'No, insegnami' option and the Done screen show '+200 monete' (UI51RulesBuilder BuildWelcome/BuildTutorial, rebuild Fase 12). The Rules page tutorial button shows the reward while still unclaimed. The user must re-upload 51.carica.js.

**Files.** Server/CloudScript/51.js, Server/CloudScript/51.carica.js, Server/CloudScript/test.js, Assets/Scripts/Auth/RewardsService.cs, Assets/Scripts/UI/UI51TutorialView.cs, Assets/Scripts/UI/UI51WelcomeView.cs, Assets/Scripts/UI/UI51RulesView.cs, Assets/UI51/Editor/UI51RulesBuilder.cs

**Risk.** The server cannot verify completion (client-claimed, like the daily reward); once per account is enforceable. PlayFab has no conditional writes, so two simultaneous calls could both pay (same accepted limit as 51.js:443). Guests: no Username means no reward, which conflicts with showing '+200' to a guest before the choice. The Welcome is device-scoped. The Fase 12 rebuild touches both scenes.

**Decision.** Reward content (+200 monete? the Smeraldo back does not exist and grant() cannot give items); what counts as 'completed' (coach steps finished or scripted match won); guests (no reward, or claimable after registering); entry points ('Aiuto' = the existing Regole e tutorial row, or also Modalità -> Allenamento); whether the tutorial match should also pay the normal training reward and count in stats.

**Test plan.** Node: test.js for first claim pays, second claim refused, guest refused. EditMode: RewardsService parse. Device or Editor with a real account: complete the tutorial and check the coins; replay it and check nothing is paid; skip and replay from Regole e tutorial.

**Verifier (confirmed).** No tutorial reward exists anywhere. The builder comments say the reward was deliberately left out (UI51RulesBuilder.cs:159, 237, 382). CloudScript has no tutorial handler: the handler list at 51.js:337-673 has inizio/statoPremi/riscattaPremio/riscattaPosta/riscattaTuttaPosta/premioPartita/segnala/abbandono/moderazione/RoomClosed, and grep 'tutorial' finds 0. grant() handles only monete/gemme/forziere (51.js:162-182), so a card-back grant is impossible today. riscattaPremio (51.js:350-360) is the once-per-key pattern to reuse. premioPartita gives nothing to guests (no Username) (51.js:405). Entry points: Welcome 'No, insegnami' (UI51WelcomeView.cs:21) and the Rules page tutorial button (UI51RulesView.cs:27), reached from Settings 'Regole e tutorial' (SettingsV2Integration.cs:73). There is no 'Aiuto' page (grep 0 hits) and no Modalità/Allenamento entry. The 'seen' flag is device PlayerPrefs. The investigator's line numbers are wrong: UI51WelcomeView.cs has only 39 lines; the key is at :15-17 and :34-35 and the Launch caller is at :21, not 78-100 / :84. The mechanism is right. The SPEC requirement is at SPEC.md:156.

- `Assets/Scripts/UI/UI51WelcomeView.cs:15-17, 21, 34-35` correct lines: SeenKey 'UI51WelcomeSeen' in PlayerPrefs; teach -> Close + UI51TutorialView.Launch
- `Assets/UIV2/Scripts/Core/StartScreenV2.cs:60` Welcome opened when UI51WelcomeView.Pending (device flag)
- `Assets/UIV2/Scripts/Core/MatchResultsV2.cs:298-326` tutorial match pays the normal training premioPartita for accounts only

**Fix concerns.** A completion claim can be forged by the client, the same as the daily reward. Once-per-account is enforceable with a server key; completion itself is not. Read-then-write in CloudScript is not atomic (51.js:90-101 helpers), so two parallel calls could both pay, which is the same accepted limit as the daily reward. Showing '+200' before the choice to a guest conflicts with the no-guest-rewards rule (51.js:405, MatchResultsV2.cs:302-305). The Welcome flag is device-scoped, so whether the reward is still available must come from the server (e.g. in the `inizio` reply), not from PlayerPrefs. Requires re-uploading 51.carica.js.

### AU1 — feature_missing · verifier: partially_confirmed · size S

**Current behaviour.** Nothing from v07 is wired. SoundLibrary.asset references only the flat files in Assets/Audio (GUIDs match). Mapping (SoundId int: current file -> v07 file -> where it plays): UiClick 0: ui_click -> SFX/UI/ui_click, auto-hooked on every Button by name (GameAudio.ClassifyButton/HookButtons), MoveSelectionUI:139. UiBack 1: ui_back -> SFX/UI/ui_back, buttons named close/back/esci/backdrop. UiTab 2: ui_tab -> SFX/UI/ui_tab, nav/tabs. UiConfirm 3: ui_confirm_02 -> SFX/UI/ui_confirm_01/02/03 (3 takes), buttons gioca/confirm/start + roulette chime DealerRouletteController:79. UiError 4: ui_error -> SFX/UI/ui_error, MoveSelectionUI:235, TableActionButtonsController:153. PopupOpen/PopupClose 5/6: -> SFX/UI/popup_open/close, InGameSettingsV2:72/124/149/160, MoveSelectionUI:221/384, round results MatchResultsV2:271. Notification 7: -> SFX/Events/notification_soft, connection/player notices GameAudio:111-116 and others' emoticons GameSocialV2:167. CardPlay 8: card_play_01-03 -> SFX/Gameplay/card_play_01..05, CardAnimationController:146 Sync.Hit on the card landing (play flight 0.35 s). CardCapture 9: card_capture_01-03 -> 01..04, CaptureSequence start (after the 0.45 s preview) CardAnimationController:236 and dealer 15/30 sweep :345. CardDeal 10: card_deal_3 -> SFX/Gameplay/card_deal_3, PlayDealSound when the deal animation is <=1 s = hands (1v1 6 cards 0.445 s, 4p 12 cards 0.715 s). CardDealLong 11: card_deal_6 -> SFX/Gameplay/card_deal_6, when >1 s = the 4 table cards at 0.35 s stagger (1.27 s) and the dealer 15/30 ghost cards; with fast animations the table deal is 0.79 s and becomes CardDeal. Scopa 12: scopa.wav 3.6 s -> scopa_01..03, TurnController:1429 at capture-preview end, at the same moment as CardCapture. Accuso 13: accuso_01-03 -> SFX/Events/accuso_01..03, AccusoImpactV2:122 on the fist slam (second slam 0.75 vol), Tre assi TurnController:827, dealer 15/30 banner :976. YourTurn 14: -> SFX/Events/your_turn, TurnController:1934 (called 794/1549/1691). MatchStart 15: kept, TurnController:907 after the round-1 roulette. Victory/Defeat 16/17: victory_short 3.65 s / defeat -> v07 1.65 s / 1.05 s, MatchResultsV2:272 at match-results reveal. RewardCoin/Gem 18/19: kept, RewardsService:184-188 on reward claims. Shuffle: no SoundId and no shuffle animation (grep 'shuffle' only finds Rules51); Extras/deck_shuffle_01..03, deal_hit_01..06 and deck_handle_01..03 are unmapped. Music: v07 Music/home_theme_loop.ogg is the same track kept; keep the existing root reference.

**Root cause.** The builder hard-codes the flat v01-v03 file list in Assets/Audio and keeps the previous Volume/PitchJitter/MinInterval of every sound on rebuild. A plain rebuild therefore cannot pick up the v07 subfolders or the v07 recommended values.

**Evidence.**
- `Assets/Editor/SoundLibraryBuilder.cs:17-20, 52-71` AudioDir 'Assets/Audio/' flat; Entries list v01-v03 files (UiConfirm only ui_confirm_02, Scopa only scopa.wav, 3 CardPlay, 3 CardCapture)
- `Assets/Editor/SoundLibraryBuilder.cs:89-96` rebuild keeps previous Volume/PitchJitter/MinInterval -> v07 values (jitter .015/.01) would be ignored
- `Assets/Resources/Audio/SoundLibrary.asset:Sounds` values equal the builder defaults (no hand-tuning to lose); Id serialized as int 0..19
- `Assets/Scripts/Gameplay/Audio/SoundLibrary.cs:7-29` enum SoundId: has no Shuffle; new ids must be appended after RewardGem (serialized ints)
- `Assets/Audio/51_Audio_v07_FINAL_CANDIDATE/Docs/Unity_mapping_recommendations.csv:1-21` author's mapping + volumes/jitter/min interval
- `Assets/Scripts/Gameplay/CardAnimationController.cs:146, 236, 345, 358-363` CardPlay Sync.Hit at landing; CardCapture; deal sound chosen by duration (>1 s = CardDealLong), not by card count
- `Assets/Scripts/Gameplay/TurnController.cs:827, 907, 976, 1429, 1934-1939` Accuso (Tre assi, dealer 15/30), MatchStart, Scopa, YourTurn
- `Assets/UIV2/Scripts/Core/AccusoImpactV2.cs:117-124` player accuso sound on the slam, Sync.Hit 0.03
- `Assets/UIV2/Scripts/Core/MatchResultsV2.cs:271-272` PopupOpen (round) / Victory or Defeat (match)
- `Assets/Scenes/GameScene.unity:71698-71702` playDuration .35, captureDuration .4, dealRevealDuration .22, dealRevealStagger .045; tableCardStagger .35 (27124)

**Fix sketch.** SoundLibraryBuilder only: point Entries at '51_Audio_v07_FINAL_CANDIDATE/SFX/...' (CardPlay 01..05, CardCapture 01..04, Scopa 01..03, UiConfirm 01..03, the rest one-to-one, MatchStart/Reward unchanged) with the CSV values. For this one switch, take the entry values instead of the preserved ones; nothing is lost because the asset equals the old defaults. Run Tools/Audio/Build Sound Library, which re-imports and re-measures Onset/Hit. The old files stay in Assets/Audio, as the user asked, unreferenced and not built. Shuffle: only if the user wants it, append SoundId.Shuffle after RewardGem and play it before the deal in TurnController.DeclareInitialAccusiWithDelay (no shuffle visual exists).

**Files.** Assets/Editor/SoundLibraryBuilder.cs, Assets/Resources/Audio/SoundLibrary.asset (via the builder), Assets/Scripts/Gameplay/Audio/SoundLibrary.cs (only if Shuffle is added)

**Risk.** Inserting an enum value in the middle would silently remap every serialized Id. The v07 clips are much shorter (card_play 0.3 s vs 0.84 s, scopa 1.2 s vs 3.6 s, victory 1.65 s vs 3.65 s): Hit offsets are re-measured, but listen to the sync. The v07 scopa has its own physical layer and plays on top of CardCapture. card_deal_6 (6 hits) plays for the 4 table cards spaced 0.35 s, so hits and landings don't match. The 'keep codebase clean' memory rule conflicts with keeping the old audio; the user's explicit instruction wins.

**Decision.** Approve v07 after listening to the Previews. Shuffle: where (no shuffle animation exists). Deal: keep deal_3/deal_6 or play Extras/deal_hit_* per card landing (perfect sync, needs a new appended SoundId). Delete old audio later or keep it.

**Test plan.** EditMode: GameAudioTests style check that every SoundId has >=1 variant with a clip in the rebuilt library and CardPlay has 5 / CardCapture 4 / Scopa 3 / UiConfirm 3. Play Mode in Editor: one training match listening for landing sync, scopa, accuso and the deal. Device: loudness balance on the phone speaker.

**Verifier (partially_confirmed).** The SoundId mapping and play sites are right. Every GameAudio.Play/PlayUi call matches the listed locations: CardAnimationController.cs:146, 236, 345, 362; TurnController.cs:827, 907, 976, 1429, 1938; AccusoImpactV2.cs:122; MatchResultsV2.cs:271-272; MoveSelectionUI.cs:139, 221, 235, 384; InGameSettingsV2.cs:72, 124, 149, 160; DealerRouletteController.cs:79; TableActionButtonsController.cs:153; GameSocialV2.cs:167; RewardsService.cs:186-187. The deal-duration rule checks out: (n-1)*stagger + 0.22 gives 0.445 / 0.715 / 1.27 s using the GameScene values (GameScene.unity:71698-71702, 27124). The builder hard-codes the flat Assets/Audio files and preserves previous Volume/Jitter/MinInterval (SoundLibraryBuilder.cs:17, 50-72, 89-96). The asset values equal the builder defaults (SoundLibrary.asset Ids 0-19), so nothing hand-tuned would be lost. v07 home_theme_loop.ogg is byte-identical to the current one (same md5). Clip lengths confirmed: card_play 0.84 -> 0.29 s, scopa 3.6 -> 1.39 s, victory 3.65 -> 1.65 s, defeat 1.3 -> 1.05 s. No shuffle code or animation exists (grep shuffle/mescol finds only Rules51 and a TurnController comment). What the investigator missed: v07 also ships SFX/Events/match_start.wav and SFX/Rewards/reward_coin.wav / reward_gem.wav. The README lists them as 'kept', but their bytes differ from the current files (md5 d969->2945, 7ff7->63f1, e26e->d014) and the CSV calls them 'kept/remastered', with new values (MatchStart 0.78, RewardCoin 0.72/jitter 0.02, RewardGem 0.75). The sketch's 'MatchStart/Reward unchanged' would leave the old masters in use. Several UI sounds also change jitter (UiClick 0.03->0, UiTab 0.02->0 per CSV).

**Corrected cause.** As stated (flat hard-coded file list plus value preservation). The mapping should include MatchStart/RewardCoin/RewardGem -> v07 SFX/Events/match_start.wav and SFX/Rewards/reward_*.wav (remastered), or the user should decide explicitly to keep the old masters.

- `Assets/Audio/51_Audio_v07_FINAL_CANDIDATE/Docs/Unity_mapping_recommendations.csv:17, 20-21` MatchStart/RewardCoin/RewardGem 'kept/remastered' with new volume/jitter
- `Assets/Audio/51_Audio_v07_FINAL_CANDIDATE/Docs/README_v07.txt:16-20` author lists match_start/reward_* under 'COSA HO TENUTO' (but the files differ by hash from Assets/Audio/*)
- `Assets/Scripts/Gameplay/TurnController.cs:711-760` DeclareInitialAccusiWithDelay is the deal intro (dealer declare -> hands -> table): a valid Shuffle hook before PlayDealerDeclareSequence

**Fix concerns.** Append any new SoundId after RewardGem (the Id is serialized as an int). The large Previews/*.wav reels sit under Assets/ and get imported, but they are not in builds unless referenced. Keeping the old audio conflicts with the 'keep codebase clean' memory, but the user's explicit instruction wins. With the shorter v07 card_play, Sync.Hit becomes a PlayDelayed path (GameAudio.cs:209-222), which is fine but needs a listening check.

### AU2 — feature_missing · verifier: confirmed · size S

**Current behaviour.** CardPlay uses 3 takes with PitchJitter ±0.04 and CardCapture 3 takes with ±0.03. Variants never repeat twice in a row. Scopa has a single take. Music is never ducked: Scopa, Accuso and Victory/Defeat play over the music at full level, and the only master control is AudioListener.volume on/off. Home and table use the same single music track, and only the volume changes (0.3 -> 0.15). That change completes in about 0.18 s, not the configured 1.2 s. The music keeps playing through the loading curtain.

**Root cause.** GameAudio has one music AudioSource whose volume UpdateMusic recomputes every frame from the active scene and MusicEnabled only; there is no duck factor and no AudioMixer in the project. The fade step is `deltaTime / MusicFadeSeconds` in absolute volume units, so a 0.15 change takes 0.15 x 1.2 s, contradicting the field's tooltip ('seconds to go from one volume to the other'). Pitch jitter compensates for having few takes, and v07 now provides more.

**Evidence.**
- `Assets/Scripts/Gameplay/Audio/GameAudio.cs:186-200` variant pick (no immediate repeat) + `pitch = 1 + Random(-PitchJitter, PitchJitter)`
- `Assets/Resources/Audio/SoundLibrary.asset:Id 8/9` CardPlay PitchJitter 0.04, CardCapture 0.03
- `Assets/Scripts/Gameplay/Audio/GameAudio.cs:261-273` UpdateMusic: target by scene name and MusicEnabled only; no ducking
- `Assets/Scripts/Gameplay/Audio/GameAudio.cs:268-269` step = dt / MusicFadeSeconds (absolute) -> 0.3->0.15 in ~0.18 s
- `Assets/Scripts/Gameplay/Audio/SoundLibrary.cs:63-68` one Music clip; MusicFadeSeconds tooltip promises a time per transition
- `Assets/Scripts/Core/GameAudioPreferences.cs:54` AudioListener.volume used only as master on/off
- `Assets/UIV2/Scripts/Core/AppLoadingView.cs:139-141` loading stops effects only; music not dipped (GameAudio.cs:94-109)
- `Assets/Scripts/Gameplay/TurnController.cs:1429` Scopa plays at the same moment as CardCapture (CardAnimationController.cs:236): two physical layers with v07 scopa
- `Assets/Audio/51_Audio_v07_FINAL_CANDIDATE/Docs/README_v07.txt:NOTA IMPORTANTE` author: jitter ~0.015/0.010, MUSIC ducking Accuso/Scopa -4/-5 dB with ~0.8 s return, Victory/Defeat -7/-9 dB

**Fix sketch.** All changes in GameAudio.cs, no mixer. (a) Duck: in PlayNow set `duckTarget` (Scopa/Accuso ~x0.58 = -4.7 dB, Victory/Defeat ~x0.4 = -8 dB) and `duckUntil = now + clip length/pitch`. In UpdateMusic keep a separate `duck` float that moves to duckTarget fast (~0.08 s) and back to 1 over ~0.8 s after duckUntil, then `music.volume = base * duck`. (b) Fade: make the step relative to the range (`dt * Mathf.Max(MusicVolumeHome, MusicVolumeTable) / MusicFadeSeconds`) so 1.2 s really means 1.2 s. Optionally dip to 0 while AppLoadingView is showing (it already calls GameAudio.StopAllEffects) and fade back in on the new scene. (c) More takes plus jitter 0.015/0.01 come from AU1. (d) Optional: skip or lower CardCapture when the move is a scopa.

**Files.** Assets/Scripts/Gameplay/Audio/GameAudio.cs, Assets/UIV2/Scripts/Core/AppLoadingView.cs (optional dip), Assets/Tests/Editor/GameAudioTests.cs

**Risk.** The duck must respect MusicEnabled and the Pause path at volume 0 (GameAudio.cs:271-272). Overlapping events must extend the duck, not stack it. Slower fades mean Home->table is audible for 1.2 s. A separate table track or crossfade needs a second clip (v07 has none).

**Decision.** Ducking amounts and release; Home <-> table: same track with a slow fade/dip, or a distinct table track (needs a new asset); whether scopa replaces the capture sound.

**Test plan.** EditMode: pure static helpers (duck gain over time, fade step) in GameAudioTests. Play Mode in Editor: trigger a scopa/accuso/results and watch music.volume (read-only inspection) or listen. Device: listen on the speaker and with headphones.

**Verifier (confirmed).** GameAudio has one music AudioSource and no mixer (find *.mixer outside Plugins: none). Volume is recomputed every frame only from the scene name and MusicEnabled (GameAudio.cs:261-273), so nothing ducks it. The fade step is `unscaledDeltaTime / MusicFadeSeconds` in absolute volume units (GameAudio.cs:268). With MusicFadeSeconds 1.2 and Home 0.3 -> table 0.15 (SoundLibrary.asset:16-18), the change takes 0.18 s, contradicting the 'Secondi per passare da un volume all'altro' tooltip (SoundLibrary.cs:67). Both scenes use one music clip (GameAudio.cs:264). AppLoadingView only stops effects (AppLoadingView.cs:139-141 -> GameAudio.cs:94-109). The master control is AudioListener.volume on/off (GameAudioPreferences.cs:54). Variant picking avoids immediate repeats and applies ±PitchJitter (GameAudio.cs:185-200); CardPlay is 0.04 and CardCapture 0.03 (SoundLibrary.asset:96, 110). Scopa plays in the same frame as CardCapture: TurnController.cs:1429 is right before CaptureSequence, which plays CardCapture at its start (CardAnimationController.cs:236). The README asks for music-only ducking (README_v07.txt:36-39).

**Corrected cause.** As stated, plus one small defect relevant to 'more takes'. lastVariant defaults to 0 when an id has never played (GameAudio.cs:188-190), so the first play of every multi-take sound after launch never picks variant 0. With 5 CardPlay takes, take 01 is excluded from the first play.

- `Assets/Scripts/Gameplay/Audio/GameAudio.cs:188-190` TryGetValue leaves previous=0 -> index in [1,n-1] on first play
- `Assets/Scripts/Gameplay/TurnController.cs:1460-1463` fallback path (no capture animation) presents Scopa feedback without the Scopa sound

**Fix concerns.** The duck must follow PlayDelayed starts (Sync.Hit with a negative offset starts later than PlayNow), so key the duck window on the actual start time. Overlapping events should extend the duck, not stack it. Keep the Pause-at-0 path working (GameAudio.cs:271-272) by applying the duck as a multiplier on the target, not on the MoveTowards state. Fix the first-play variant bias when adding takes.

### O2 — code_looks_correct_needs_device_test · verifier: partially_confirmed · size S

**Current behaviour.** The toggle is wired correctly in Home and at the table. Both write Settings_Vibration, which every haptic call reads through the same cached static. Vibrating events: light pulse (Android VIRTUAL_KEY, iOS UIImpact Light) on every UI Button present at scene load, except dimmer/backdrop/overlay buttons, and on a card tap on your own turn. Strong pulse (Android CONFIRM on API30+ else LONG_PRESS, iOS Medium) only for the local human: own scopa, own accuso at the first fist slam, Tre assi and dealer 15/30 when they are yours, match victory, and any XP gain. The XP gain also fires after a DEFEAT for accounts (20 XP), so a loss vibrates like a win. Not vibrating: settings switches (UI51Toggle is not a Button, and this includes the Vibrazione switch itself), buttons spawned at runtime (list rows, toasts and DDOL overlays), your turn, errors/illegal moves, defeat, and the timer. An 80 ms throttle drops a light pulse right after a strong one. Physical vibration has never been tested on a phone (K6 notes).

**Root cause.** The code path is correct. If the user felt nothing, the likely causes are platform-level: Android performHapticFeedback only works when system touch feedback is enabled (verified in the Android docs), and iOS UIImpactFeedbackGenerator is silent when System Haptics is off (hypothesis, device check). The incoherence is real, though: haptics are installed per Button only at sceneLoaded, switches are not Buttons, and 'Reward' piggybacks on any XP change.

**Evidence.**
- `Assets/Scripts/Core/GamePreferences.cs:15, 31-32, 108-114` Settings_Vibration default on; Write fires Changed
- `Assets/UIV2/Scripts/Core/SettingsV2Integration.cs:69, 100` Home switch -> SetVibrationEnabled, refreshed on Open
- `Assets/UIV2/Scripts/Core/InGameSettingsV2.cs:58, 86` table switch, same key
- `Assets/Scenes/MainMenu.unity:90393` vibrationSwitch wired (GameScene.unity:3150 VibrationSwitch wired)
- `Assets/Scripts/Core/GameFeedback.cs:47-57` TryHaptic: localHuman, focus, VibrationEnabled, 80 ms throttle
- `Assets/Scripts/Core/GameFeedback.cs:67-100` Android: VIRTUAL_KEY(1) light, CONFIRM(16) API>=30 else LONG_PRESS(0) strong on decor view; iOS K6Impact
- `Assets/Plugins/iOS/K6Haptics.mm:7-21` Light vs Medium UIImpactFeedbackGenerator, generation token
- `Assets/UIV2/Scripts/Animations/UIV2MotionInstaller.cs:10-29, 50-57` UIV2HapticButton added only on sceneLoaded -> runtime-instantiated buttons get none
- `Assets/UI51/Scripts/Components/UI51Toggle.cs:17, 50-54` switch is IPointerClickHandler, not a Button: no haptic, no click sound
- `Assets/Scripts/Gameplay/CardView.cs:240, 257` light pulse on card tap
- `Assets/Scripts/Gameplay/TurnController.cs:826, 970, 1432, 1462` strong: Tre assi, dealer 15/30, scopa (local only via ForPlayer)
- `Assets/UIV2/Scripts/Core/GameSocialV2.cs:210-213` accuso strong pulse at the first slam
- `Assets/UIV2/Scripts/Animations/UIV2FeedbackParticles.cs:67-71` any XP gain -> Present(Reward) strong pulse
- `Assets/UIV2/Scripts/Core/MatchResultsV2.cs:303, 315, 269` RecordGameResult(won, xp) with 20 XP on a loss fires the Reward pulse; Victory pulse in the same frame is throttled
- `Assets/Scripts/Gameplay/TurnController.cs:1934-1939` YourTurn is sound only
- `docs/archivio/ui/k6-feedback-plan.md:Restano da verificare` native builds and haptic feel never verified on phones

**Fix sketch.** No fix is needed for the toggle itself. Smallest coherence fixes: (1) UI51Toggle.OnPointerClick: `GameFeedback.TryHaptic(false); GameAudio.PlayUi(SoundId.UiClick);` (Assembly-CSharp can reference both). (2) Reward-on-defeat: in UIV2FeedbackParticles.ExpChanged keep the particles but drop the haptic, or present it only on a win (decision). (3) Runtime-spawned buttons: either call UIV2MotionInstaller.Apply on the spawned row/overlay, or move the light haptic into GameAudio's existing per-button hook (it already scans every Button each second; Gameplay can call Core GameFeedback) and delete UIV2HapticButton, keeping its dimmer/backdrop exclusions. (4) Optional: REJECT/light pulse on UiError, light pulse on YourTurn.

**Files.** Assets/UI51/Scripts/Components/UI51Toggle.cs, Assets/UIV2/Scripts/Animations/UIV2FeedbackParticles.cs, Assets/UIV2/Scripts/Animations/UIV2MotionInstaller.cs or Assets/Scripts/Gameplay/Audio/GameAudio.cs

**Risk.** K6FeedbackTests pin the throttle and the accepted-click behaviour. Adding haptics to every switch or button may feel like too much. Moving haptics into GameAudio keeps the 1 s scan delay for brand-new buttons.

**Decision.** Which events should vibrate (your turn? errors? defeat? reward after a loss?) and whether every button tap should vibrate.

**Test plan.** Device only (Editor has no haptic engine): iPhone with System Haptics on/off and Android with Touch feedback on/off. Toggle off -> nothing; on -> button taps light, scopa/accuso/victory strong; check a loss. EditMode: K6FeedbackTests extended for UI51Toggle (pulse consumed when the pref is on).

**Verifier (partially_confirmed).** The toggle wiring is confirmed. Home and table switches call GamePreferences.SetVibrationEnabled (SettingsV2Integration.cs:69, InGameSettingsV2.cs:58), are refreshed on open (:100, :86), and are wired in both scenes (MainMenu.unity:90393, GameScene.unity:3150). TryHaptic reads the same cached static and has an 80 ms throttle (GameFeedback.cs:47-57). The Android constants and iOS Light/Medium generators are confirmed (GameFeedback.cs:88, K6Haptics.mm:13-19). UI51Toggle is only an IPointerClickHandler with no haptic or sound (UI51Toggle.cs:17, 50-54). Reward fires on any XP gain (UIV2FeedbackParticles.cs:67-71). RecordMatch gives XP on a loss (PlayerXp.cs:36-39: 20 online, 10 in training), so a loss gets a strong pulse, and on a win the Victory pulse in the same frame is throttled (MatchResultsV2.cs:101 then 265-269). Two corrections. (1) The cross-cutting claim that runtime list rows never vibrate is partly wrong. Friends/Mail/Ranking/Blocked/News rows are Instantiate'd from in-scene templates (UI51FriendsView.cs:53, 207; UI51MailView.cs:44, 158; UI51RankingView.cs:111; UI51BlockedView.cs:43). UIV2MotionInstaller.Apply walks GetComponentsInChildren<Button>(true) at sceneLoaded (UIV2MotionInstaller.cs:50-57), which includes the inactive templates, so the templates get UIV2HapticButton and the clones inherit it. Only Resources/prefab-asset instances lack it: DDOL Toast/Connection/Service (UI51Toast.cs:32-34) and Collection cards (CollectionDecksPanel.cs:31, prefab guid at MainMenu.unity:311240). No prefab bakes UIV2HapticButton (its script guid appears in no .prefab or .unity). (2) A light pulse also fires when tapping an opponent's revealed accused hand (CardView.cs:255-258 via CardViewManager.cs:256), not only on your own turn.

**Corrected cause.** The code path is correct; device and system settings are the probable cause if nothing is felt. The incoherence is real: switches are not Buttons, Reward piggybacks on any XP gain including losses, and prefab-asset or DDOL runtime buttons get no haptic. In-scene template rows do vibrate.

- `Assets/UIV2/Scripts/Animations/UIV2MotionInstaller.cs:50-57` includeInactive=true -> templates get UIV2HapticButton, clones inherit it
- `Assets/Scripts/UI/UI51FriendsView.cs:53, 93, 207` rowTemplate is an in-scene child, deactivated in Awake, cloned with Instantiate
- `Assets/Scripts/Gameplay/CardView.cs:255-258` light pulse on tapping an accused hand
- `Assets/Scripts/Core/PlayerXp.cs:36-39` loss XP 20, halved in training (10)

**Fix concerns.** Fix (3) as written would add work for rows that already have haptics. Limit it to prefab-asset / DDOL instances (call UIV2MotionInstaller.Apply on the spawned root) to avoid a double pulse (the throttle would drop it, but it would still be redundant). Adding TryHaptic to UI51Toggle is compile-safe: UI51 is in Assembly-CSharp and Core/Gameplay are autoReferenced (Project51.Gameplay.asmdef). K6FeedbackTests pin the throttle.

### O3 — likely_cause · verifier: partially_confirmed · size M

**Current behaviour.** 'Grafica ridotta' (QualityLow) only turns off decorative layers: K6 particles, confetti, K4 UI/card shaders and the scopa sweep, backdrop blur, Home motes and ambient float, Home flame animation, pulsing glows and rings, and decorative DOTween loops. It freezes the banner shader animation, which still runs the same per-pixel shader with time = 0, so there is no GPU saving. It also turns on fast animations (x1.6), which changes game pacing. It does not touch any real frame-cost lever: frame rate, resolution, MSAA or HDR, card drop shadows, or the per-second Button scan. No targetFrameRate is set anywhere, so on iOS and Android the app is capped at a fixed 30 fps (verified in the Unity 2022.3 docs). FPS therefore cannot go up with reduced graphics; only frame time, battery and heat can improve.

**Root cause.** I5 was implemented as an 'effects off' switch. Its own doc says native battery and performance measurements were never done. Nothing in the project sets Application.targetFrameRate or QualitySettings at runtime. Both scene cameras have HDR on and MSAA allowed. The quality levels are Built-in RP with MSAA 2x on Very High/Ultra, and there is no per-platform default (m_PerPlatformDefaultQuality empty, current = 5 Ultra). The level that runs on iOS is unknown (hypothesis: HDR + MSAA render targets add bandwidth cost). GameAudio scans every Button in the scene, including inactive ones, every second with FindObjectsOfType, which allocates garbage (hypothesis: periodic CPU/GC spikes on large scenes).

**Evidence.**
- `Assets/Scripts/Core/GamePreferences.cs:56, 93-98` ReducedGraphics = QualityLow; SetReducedGraphics also sets FastAnimations
- `Assets/Scripts/Core/GamePreferences.cs:18, 77-80` fast animations x1.6 shorten table timings (pacing, not performance)
- `Assets/Scripts/Gameplay/BackdropBlur.cs:35, 41, 92, 201` blur capture skipped (event-time cost, not per frame)
- `Assets/UI51/Scripts/Core/UI51Banners.cs:132` SyncStill sets _UI51Still; Banner.shader:109,138 still runs the same shader with time=0 (no GPU saving)
- `Assets/UI51/Scripts/Anim/UIAnim.cs:246-250` DecorativeLoops gate
- `Assets/Scripts/Gameplay/CardView.cs:1064-1073` glow/halo still updated every frame in reduced mode (constant values)
- `Assets/Scripts/Gameplay/CardDropShadow.cs:44-56` per-card shadow sprite (overdraw), not gated
- `Assets/Scripts/Gameplay/Audio/GameAudio.cs:28, 246-250, 277` FindObjectsOfType<Button>(true) every 1 s on every screen
- `Assets/Scenes/GameScene.unity:80059-80060` camera m_HDR: 1, m_AllowMSAA: 1 (MainMenu.unity:102434-102435 same)
- `ProjectSettings/QualitySettings.asset:7, 221, 270, 304` m_CurrentQuality 5 (Ultra), antiAliasing 2 on Very High/Ultra, m_PerPlatformDefaultQuality {}
- `ProjectSettings/GraphicsSettings.asset:42, 47` Built-in RP, empty tier settings
- `Assets/PlayFabSDK/Shared/Public/PlayFabDataGatherer.cs:98` only reference to targetFrameRate in Assets (read, never set) -> mobile default fixed 30 fps (Unity 2022.3 ScriptReference verified)
- `docs/archivio/ui/i5-reduced-graphics.md:Verifiche` 'Build native e misure di batteria/prestazioni su telefono non eseguite'
- `Assets/Tests/Editor/I5ReducedGraphicsTests.cs:12-356` tests cover the preference migration and effect switching only, not performance

**Fix sketch.** Measure first: a Development build on the iPhone with the Unity Profiler and Xcode FPS/GPU gauges, Home and table, normal vs reduced. Then pick the cheapest real levers, applied in one place that already reacts to GamePreferences.Changed (e.g. GameFeedback.PolicyChanged or a small static in GamePreferences.SetGraphicsQuality, plus once at boot): (1) frame-rate policy `Application.targetFrameRate = ReducedGraphics ? 30 : 60` (decision; today everything is 30); (2) `QualitySettings.antiAliasing = 0` and camera HDR off, since a 2D sprite/UI game gains nothing from MSAA or HDR (if measured, do it in all modes through the builders or at runtime in CameraResponsiveFit); (3) disable CardDropShadow when reduced; (4) make the GameAudio button scan cheap (scan on sceneLoaded and on demand rather than every second), which helps every mode. Optional lever: Screen.SetResolution scale under reduced (text sharpness trade-off).

**Files.** Assets/Scripts/Core/GamePreferences.cs or Assets/Scripts/Core/GameFeedback.cs, Assets/Scripts/Gameplay/CardDropShadow.cs, Assets/Scripts/Gameplay/Audio/GameAudio.cs, Assets/Scripts/Gameplay/CameraResponsiveFit.cs (HDR/MSAA at runtime) or the scene builders

**Risk.** 60 fps in normal mode increases battery use and heat versus today's 30 (it improves smoothness, not cost). Turning off HDR could change colours if any effect relies on HDR values (check BackdropBlur and K4 shaders). The FastAnimations coupling means turning reduced graphics on also speeds up the game. Changing the button scan affects click sounds everywhere.

**Decision.** What 'reduced' may cost visually: 30 fps vs 60 fps policy, lower render resolution, no card shadows. Whether to decouple fast animations from reduced graphics.

**Test plan.** Device only for the numbers: Profiler (CPU main thread ms, GC alloc per second, GPU ms) on Home idle, table mid-hand and results with confetti, normal vs reduced, before and after. EditMode: extend I5ReducedGraphicsTests to assert targetFrameRate/antiAliasing follow the preference. Editor: Stats window for batches/overdraw (indicative only).

**Verifier (partially_confirmed).** Confirmed: no Application.targetFrameRate, vSyncCount or QualitySettings write anywhere in project code (grep: only the PlayFabDataGatherer.cs:98 read). Phones therefore run at the platform default, which is 30 fps on iOS/Android in Unity 2022.3 (ProjectVersion 2022.3.60f1). That matches the documented default, but the unity_docs fetch did not return that paragraph, so I did not re-verify the wording. Also confirmed: SetReducedGraphics couples fast animations (GamePreferences.cs:93-98, used by both switches SettingsV2Integration.cs:70 and InGameSettingsV2.cs:59); camera HDR and MSAA allowed in both scenes (GameScene.unity:80059-80060, MainMenu.unity:102434-102435); MSAA 2x only on Very High/Ultra and empty m_PerPlatformDefaultQuality (QualitySettings.asset:221, 270, 304); card shadows not gated (CardDropShadow.cs:44-60 has no ReducedGraphics check); a FindObjectsOfType<Button>(true) scan every second (GameAudio.cs:28, 246-250, 277); no native measurement (docs/archivio/ui/i5-reduced-graphics.md:51). Overstated: 'it does not touch any real frame-cost lever'. Reduced mode releases the animated K4 UISurface shader on Home's BackgroundLayer, a large full-screen per-pixel sin/pow shader (UIV2ShaderKit.cs:46-47, UIV2SurfaceEffect.cs:31, K4/UISurface.shader:47-63; MainMenu has one BackgroundLayer), and on every btn_gold_* button (sweep, UIV2ShaderKit.cs:28-31). It also stops UIV2MoteField's per-frame Update/mesh work (UIV2MoteField.cs:115), particle emitters, and per-frame DOTween loops that dirty canvases (UIAnim.cs:250 and its callers). Those are real GPU fill and CPU savings, just unmeasured and invisible under a 30 fps cap. The banner freeze does skip the sheen block (Banner.shader:138); the gradient and stars still run, so the saving is small, which is close to the claim.

**Corrected cause.** FPS cannot rise because of the default 30 fps mobile cap. Reduced mode does cut some real per-frame cost: the Home background K4 shader, gold-button sweeps, mote fields, particles and canvas-dirtying loops. It leaves the bigger levers alone (frame-rate policy, MSAA/HDR, render scale, card shadows, the 1 s Button scan). Without device profiling nobody can say whether the change is noticeable. Which quality level iOS actually uses with an empty per-platform default is unknown.

- `Assets/UIV2/Scripts/Core/UIV2ShaderKit.cs:28-31, 46-47` K4 UISurface applied to gold buttons and to BackgroundLayer
- `Assets/UIV2/Scripts/Animations/UIV2SurfaceEffect.cs:31, 67` ReducedGraphics -> ReleaseMaterial (back to the default UI shader)
- `Assets/Resources/K4/UISurface.shader:47-63` time-animated per-pixel wave/light/rainbow
- `Assets/UIV2/Scripts/Components/UIV2MoteField.cs:115` per-frame Update skipped when reduced
- `Assets/UI51/Resources/UI51/Banner.shader:109, 138` still: time=0 and the sheen branch skipped; gradient/stars still computed

**Fix concerns.** 60 fps in normal mode raises battery and heat compared with today. Disabling HDR may change the look of BackdropBlur and K4 output, so check before shipping. Changing the Button scan affects click sounds everywhere (GameAudio.cs:275-283), and template rows also rely on it for sound. Decoupling FastAnimations from reduced graphics needs a separate toggle: today SetReducedGraphics(false) also forces fast animations off (GamePreferences.cs:95).

### Missed by the investigator (found by the verifier)

- **TU1**: The Rules page has a second score-rule text the diagnosis did not list: Formati > 'Pareggio: se due giocatori sono in testa oltre 51 si gioca un'altra smazzata'. It already matches the corrected rule, but today's code also replays a tie at exactly 51. Review it together with the TU1 text changes; it is in the same Fase 12 rebuild.
  - `Assets/UI51/Editor/UI51RulesBuilder.cs:113` Pareggio row
  - `Assets/Scenes/MainMenu.unity:185754` baked text
- **TU2**: Guided moves need taps to reach the highlighted card. The coach veil swallows every tap, including inside the spotlight hole, by design. No ICanvasRaycastFilter exists in the project, and CardView drops taps whenever the pointer is over UI. A TU2 move gate in ExecuteMove would therefore never receive a tap while the coach is visible unless the Spot gets a raycast filter for the hole.
  - `Assets/UI51/Editor/UI51RulesBuilder.cs:265` 'il velo col buco prende tutti i tocchi (anche nel buco)'
  - `Assets/Scripts/Gameplay/CardView.cs:239` IsPointerOverUI -> return
- **TU2**: Under the TU1 fix, a one-smazzata tutorial cannot let the user live 51->0. The reset happens only in ContinueMatch for a match that has not finished. If the bot lands on 51 while the player goes above 51 at the same smazzata end, the match ends and the bot is never reset, so 51->0 would only be narrated. A second (shortened) smazzata, or a script where the 51 smazzata does not end the match, is needed.
  - `Assets/Scripts/Core/MatchScore.cs:66-79` reset path only when !IsFinished(previous)
- **AU1**: The v07 package contains remastered match_start.wav, reward_coin.wav and reward_gem.wav, with different bytes from the current files and new recommended volume/jitter. The AU1 sketch keeps MatchStart/Reward on the old masters without saying so.
  - `Assets/Audio/51_Audio_v07_FINAL_CANDIDATE/Docs/Unity_mapping_recommendations.csv:17, 20-21` kept/remastered, 0.78 / 0.72 (jitter 0.02) / 0.75
  - `Assets/Editor/SoundLibraryBuilder.cs:67, 70-71` builder points at the flat Assets/Audio/match_start.wav and reward_*.wav
- **AU2**: Variant selection never plays variant 0 the first time a multi-take sound plays after launch: lastVariant defaults to 0, and the index is shifted past it. More v07 takes would keep this bias on the first play.
  - `Assets/Scripts/Gameplay/Audio/GameAudio.cs:188-190` TryGetValue(out previous) -> 0 when missing; index >= previous -> index++
- **O2**: Correction to the runtime-buttons claim. Rows cloned from in-scene templates (Friends, Mail, News, Ranking, Blocked) inherit UIV2HapticButton, because the installer scans inactive children at sceneLoaded. Only prefab-asset and Resources instances get no haptic: Collection cards, and the DDOL Toast/Connection/Service overlays.
  - `Assets/UIV2/Scripts/Animations/UIV2MotionInstaller.cs:50-57` GetComponentsInChildren<Button>(true)
  - `Assets/UIV2/Scripts/Screens/CollectionDecksPanel.cs:31` Instantiate(deckCardPrefab), a prefab asset (MainMenu.unity:311240)
  - `Assets/Scripts/UI/UI51Toast.cs:32-34` Resources prefab instantiated at runtime

### Cross-cutting notes

- **other** (TU1, multiplayer/matchmaking items, update-gate items): Photon AppVersion is set to Application.version, which is frozen at 1.0.0 for every TestFlight build (only the build number changes). Builds with different game rules or protocols can therefore be matched into the same room. The TU1 51->0 change would make an old and a new client disagree on when the match ends. The ServiceGate update check also compares Application.version, so it cannot force an update between builds.
  - `Assets/Scripts/Auth/PhotonAuthConnector.cs:51` settings.AppSettings.AppVersion = Application.version
  - `Assets/Scripts/Auth/FriendsChat.cs:146` Chat connects with Application.version
  - `ProjectSettings/ProjectSettings.asset:143, 169-175` bundleVersion 1.0.0; buildNumber iPhone 2; AndroidBundleVersionCode 264
  - `Assets/Scripts/Auth/ServiceGate.cs:46` update gate parses against Application.version
- **audio-wiring** (O2, any 'no sound/no vibration on X' report in social, mail or overlay screens): UI feedback is installed by scanning, not at creation. Haptics (UIV2HapticButton) are added only on sceneLoaded, and click sounds are hooked by a 1 s FindObjectsOfType scan. Buttons instantiated later (Friends/Mail/News/Ranking/Blocked rows, Collection panels, DDOL Toast/Connection/Service overlays) never vibrate, and they make no click sound until the next scan, up to 1 s.
  - `Assets/UIV2/Scripts/Animations/UIV2MotionInstaller.cs:10-29` Apply only on sceneLoaded / initial scene
  - `Assets/Scripts/Gameplay/Audio/GameAudio.cs:243-250, 275-283` click listener added by a 1 s scan
  - `Assets/Scripts/UI/UI51Toast.cs:36` DDOL overlay created at runtime (also UI51ConnectionOverlay.cs:58, UI51ServiceScreen.cs:58)
  - `Assets/Scripts/UI/UI51FriendsView.cs:n/a` rows via Instantiate (same in UI51MailView, UI51NewsView, UI51RankingView, UI51BlockedView, CollectionDecksPanel, CollectionEmoticonsPanel)
- **options-wiring** (O2, settings/options items): Every settings switch (Effetti, Musica, Vibrazione, Grafica ridotta, Suggerimenti) is a UI51Toggle, which is not a Button. Switches therefore get neither the click sound nor the haptic that every other control has, so toggling Vibrazione on gives no tactile confirmation.
  - `Assets/UI51/Scripts/Components/UI51Toggle.cs:17, 50-54` IPointerClickHandler only
  - `Assets/UI51/Editor/UI51MetaBuilder.cs:1224-1233` Switch = UI51Toggle + transparent Hit image, no Button
- **playerprefs-not-account-scoped** (TU3, account/onboarding items): The Welcome/tutorial offer ('UI51WelcomeSeen') is stored per device. A second account on the same phone never sees it, and a reinstall shows it again. Any once-per-account reward (TU3) must be decided server-side, not from this flag.
  - `Assets/Scripts/UI/UI51WelcomeView.cs:78-100` PlayerPrefs.GetInt/SetInt("UI51WelcomeSeen")
  - `Assets/UIV2/Scripts/Core/StartScreenV2.cs:60` Welcome opened from CompleteEntrance for any entrance
- **other** (O3, animation/fluidity items in other clusters): No Application.targetFrameRate is set anywhere, so iOS and Android run at a fixed 30 fps (verified in the Unity 2022.3 ScriptReference). Any device report that animations, card flights or scrolling feel 'not fluid' is explained by this cap before any effect cost.
  - `Assets/PlayFabSDK/Shared/Public/PlayFabDataGatherer.cs:98` only targetFrameRate reference (read-only)
  - `ProjectSettings/QualitySettings.asset:304` no per-platform quality default
- **other** (O3, stutter/perf items anywhere): GameAudio runs FindObjectsOfType<Button>(includeInactive: true) every second on every scene and allocates a new array each time. On the large MainMenu (16 canvases) this is a plausible periodic CPU/GC hitch in every mode. Hypothesis to confirm with the device Profiler.
  - `Assets/Scripts/Gameplay/Audio/GameAudio.cs:28, 246-250, 277` ButtonScanInterval = 1 s, full scene scan
- **other** (TU2, TU3, stats/XP items): The tutorial match is a normal Training match. For accounts it calls premioPartita at the end (stats TotalGames/XP/coins), and leaving it through Abbandona records a loss (RecordAbandon -> MatchQuit). Only 'Salta' in the coach skips this. The tutorial can therefore pollute stats and XP.
  - `Assets/UIV2/Scripts/Core/MatchResultsV2.cs:317-321, 346-357` MatchReward(training) / RecordAbandon -> MatchQuit
  - `Assets/UIV2/Scripts/Core/InGameSettingsV2.cs:164-170` ConfirmLeave -> RecordAbandon
  - `Assets/Scripts/UI/UI51TutorialView.cs:286` Salta -> LeaveGameAndGoToMenu without RecordAbandon
- **text-content** (TU1, results screen items, table HUD items): Score texts outside the tutorial depend on the >=51 rule: the results race note ('N punti alla vittoria', one too few), the tie screen ('Parità a 51 / avete superato 51' triggered at exactly 51), and the table pill 'A 51'. All of them change with TU1.
  - `Assets/Scripts/Core/ResultsSheet.cs:73-79` RaceNote
  - `Assets/Scripts/UI/UI51ResultsView.cs:214-243` race panel + tiePending >= target + tie text
  - `Assets/Scripts/UI/TableTopBarController.cs:73` 'A' + TargetScore
