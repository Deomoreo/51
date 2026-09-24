# K6 — feedback tattile e particelle, 2.05

Implementazione autorizzata il 23 settembre 2026. Conservare il DOPath continuo
ripristinato in 2.04: K6 non interviene sulla traiettoria o sulla durata del volo.

## Comportamento

- `GameFeedback` centralizza aptica e richieste visive. Vibrazione persistente
  `Settings_Vibration`, attiva per default, condivisa tra Home e tavolo.
- I Button usano onClick accettato, anche se un listener precedente chiude il pannello.
  Un Button disabilitato non genera il click. Le carte vibrano al tocco nel turno locale.
- Scopa normale e del mazziere, primo impatto accuso (anche dichiarazioni simultanee),
  vittoria locale finale. Gli eventi di bot/remoti possono mostrare particelle ma non
  generano vibrazione locale. Dedupe vittoria sulla stessa istanza GameState mostrata.
- Premi: solo `PlayerProgressLocal.OnExpChanged` con incremento positivo e app in focus.
  Nessuna ricompensa simulata, nessun ascolto aggiuntivo di OnPendingExpClaimed.
- Un pool persistente di quattro UIParticle, 18/24/48 particelle secondo evento;
  emissione manuale, vita massima .95 s, pulizia entro 1.1 s. Nessun raycast e nessun
  consumo di UnityEngine.Random. Cambio scena e disabilitazione svuotano il pool.
- `GameFeedback.SetParticlesEnabled(false)` prepara I5, senza introdurre ora un'altra
  impostazione utente. I coriandoli preesistenti dei risultati restano indipendenti.

## Piattaforme

Android usa performHapticFeedback: VIRTUAL_KEY per tocchi, CONFIRM da API30 per eventi,
LONG_PRESS come fallback. Non aggiunge permessi. iOS usa UIImpactFeedbackGenerator;
plugin importato solo su iOS. Un token invalida impulsi in coda al cambio preferenza/focus.
Il valore restituito da TryHaptic indica solo che la policy accetta l'impulso.

Riferimenti verificati:
- https://developer.android.com/develop/ui/views/haptics/haptics-apis
- https://developer.android.com/reference/android/view/HapticFeedbackConstants
- https://developer.apple.com/documentation/uikit/uiimpactfeedbackgenerator

## Verifica eseguita

- Suite completa: 268 casi, 261 passati, zero fallimenti, 7 espliciti esclusi dal run.
- Sei test K6 EditMode: preferenza/persistenza, remoto e throttle, pulsante che chiude
  il pannello, pulsante disabilitato, accusi consecutivi, spegnimento delle particelle.
- Harness runtime K6: venti burst riusano quattro emettitori, cap 48, scadenza,
  disabilitazione immediata e RNG di gameplay invariato. Passato.
- Tre runtime K5 rieseguiti e passati. Il test EditMode di continuità del volo passa.
- Toggle Home e tavolo provati via callback reale e preferenza ripristinata; layout
  controllati a 1080x1920. Particelle osservate tramite ScreenCapture in GameView.
  Per le Opzioni dello StartScreen usare OptionsButton: alza il Canvas sopra lo start.
- Builder idempotente `Tools/UIV2/Apply Feedback Kit`, applicato a MainMenu/GameScene.
- Corretto il limite dimensionale del ParticleSystemRenderer: il default .5 nella
  camera ortografica di bake riduceva le stelle a pochi pixel. Verifica finale visiva
  e harness runtime passati con maxParticleSize 100.

Immagini di verifica: [Home](k6-home-settings.png), [tavolo](k6-table-settings.png),
[burst fermato a 0.2 s per ispezione](k6-burst.png). Il burst nella schermata iniziale
è una sonda di verifica, non un premio o una celebrazione attivata automaticamente.

Restano da verificare build native e sensazione/performance su Android/iPhone reali.
Editor non prova il motore aptico. Nessun profiling GPU su dispositivo eseguito.
Un GameState finale ricostruito come nuova istanza può ripetere la celebrazione;
il flusso attuale riusa la stessa istanza. I5 è il prossimo lavoro del backlog.
