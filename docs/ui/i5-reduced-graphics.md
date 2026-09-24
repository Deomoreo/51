# I5 — Grafica ridotta (2.06, 23 settembre 2026)

Impostazione unica approvata dall'utente, salvata e condivisa tra Home e tavolo.
Sostituisce Animazioni veloci e mantiene la scelta precedente: precedenza alla nuova
chiave `Settings_ReducedGraphics`, poi `Settings_FastAnimations`, infine alla vecchia
chiave Home `Settings_AnimazioniVeloci`. Una scelta nuova esplicita prevale sempre.

## Comportamento

- Spegne particelle K6, pulviscolo/scoppi C6, coriandoli dei risultati, shader UI K4,
  sfondo animato, olografia delle carte e vecchi effetti olografici del MainHud.
- Ferma le pulsazioni decorative e il movimento delle carte nella schermata di
  caricamento. Suggerimenti di presa, matta, turno, countdown e progresso restano leggibili.
- Riduce le durate tramite il moltiplicatore esistente 1,6 (circa il 37,5% in meno),
  compresi i tempi del tavolo già coperti da Animazioni veloci. Nessun cambio alla
  traiettoria continua K5. Audio e scelta Vibrazione rimangono indipendenti.
- Cancella le catture di sfocatura pendenti. Un pannello già aperto conserva soltanto
  la piccola immagine già sfocata, senza mostrarla né mantenere il materiale di blur;
  la ripristina quando si torna alla grafica completa e la libera alla chiusura.
  Se il pannello è stato aperto in modalità ridotta, la riattivazione cattura il tavolo
  nascondendo temporaneamente il pannello con un CanvasGroup separato.
- Ogni effetto verifica la preferenza anche alla creazione e alla riapertura; nessuna
  scansione continua della scena. Gli effetti ascoltano il cambio senza disattivare
  il proprio componente osservatore, così il ripristino funziona.

## Integrazione

`GamePreferences` è la fonte della preferenza. Le vecchie API FastAnimations restano
alias per compatibilità. `GameFeedback` combina il suo interruttore tecnico delle
particelle con Grafica ridotta e pulisce gli emettitori attivi senza toccare la vibrazione.

`SettingsV2Integration` riusa la riga Home esistente; `InGameSettingsV2` mantiene il
riferimento serializzato FastAnimations. Il comando idempotente
`Tools/UIV2/Apply Reduced Graphics Settings` aggiorna le due scene senza ricostruirle.
Aggiornati anche i builder delle impostazioni.

## Verifiche

- Suite EditMode finale: **271 passati, zero fallimenti**, 7 casi marcati Explicit
  esclusi dal lancio automatico. I 3 Explicit K5 eseguiti anche via runtime sono passati.
- 10 test I5: migrazione delle due chiavi precedenti, precedenza, persistenza dopo
  reset cache, blocco particelle, indipendenza vibrazione, blur inattivo, ripristino
  snapshot, materiali delle carte, glow, coriandoli e materiale olografico già salvato.
- Runtime I5: pulizia delle particelle attive, nuovi shader e riapertura, ripristino
  olografico, mesh del pulviscolo vuota, glow nascosti, cattura pendente annullata.
- Runtime al tavolo: scelta condivisa, apertura ridotta senza snapshot, cattura pulita
  alla riattivazione e ripristino immediato dello snapshot già disponibile. Verificato
  anche il cambio preferenza con controller disattivato, senza avviare coroutine inattive.
- Runtime K6: pool limitato, scadenza, cleanup e RNG gameplay invariato.
- Verifica visiva Home/tavolo in GameView a 1080×1920; screenshot in questa cartella.
- Build native e misure di batteria/prestazioni su telefono non eseguite.

Screenshot: [Home](i5-home-settings.png), [tavolo ridotto](i5-table-reduced.png),
[sfocatura ripristinata](i5-table-restored.png).

Prossimo intervento nel backlog: **K7**. Nessun commit del lavoro preesistente o di I5.
