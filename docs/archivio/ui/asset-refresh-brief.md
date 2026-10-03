# Revisione asset UI — brainstorming del 23 settembre 2026

## Obiettivo e stato

Rendere il 51 più coerente e meno composto da pezzi di kit diversi, mantenendo
il carattere illustrato, il blu profondo e l'oro. Richiesta: rivedere X, frecce,
riquadri con icone e individuare altri punti da alleggerire.

**Confermato dall'utente:** «Indietro» nelle schermate e «Chiudi» nei popup,
nello stile del gioco, sostituiscono le X usate per chiudere.
**Proposte da valutare:** stile delle nuove icone, priorità e misure sotto.
Questo documento aggiorna la lista di lavoro; non descrive modifiche già applicate.

Analisi basata sui mockup `home_B2 (1).png` e `26_impostazioni_ingame.png`,
sul design system K2 e sull'ispezione dei builder. I mockup sono riferimenti storici:
non è stata effettuata una nuova verifica dell'app in esecuzione.

## Cosa stona e perché

- **Indietro con disco di legno:** il builder Auth descrive legno + cornice oro +
  freccia in un solo sprite. Introduce un materiale diverso dalla famiglia blu/oro.
- **Contenitori ripetuti:** nel mockup Home scrigno, coppa e busta hanno già una
  silhouette riconoscibile; il quadrato lucido aggiunge una seconda cornice e peso.
  Il backlog I9 chiedeva già di eliminarlo, mentre K2 prescrive ancora il riquadro blu.
- **X ambigua:** nei builder compare per chiusura, abbandono e rimozione.
  Nel mockup Impostazioni anche chiusura e abbandono condividono la X rossa:
  un'azione innocua sembra pericolosa. Cambiare solo il disegno non risolve il significato.
- **Troppe superfici protagoniste:** nel mockup Home oro, blu lucido, verde e turchese
  competono. Conservare la ricchezza nei premi e nell'azione principale; rendere più
  tranquilli comandi, selettori e contenuti. Verificare quanto K2 abbia già uniformato.
- **Cornice dentro cornice:** nel mockup Impostazioni ogni riga è incorniciata dentro
  un pannello già incorniciato. Provare righe aperte con spazio e divisori leggeri.
- **Tipografia decorativa ovunque:** conservare il carattere di GIOCA e dei titoli,
  ma evitare contorni pesanti su ogni etichetta e testo di servizio. Riutilizzare Poppins.

## Tre direzioni possibili

1. **Consigliata: illustrato più pulito.** Oggetti riconoscibili senza scatola;
   navigazione sobria; oro concentrato su azioni importanti, premi e identità.
   Mantiene il carattere attuale e richiede un set limitato di sostituzioni.
2. **Tutto minimale.** Icone piatte e controlli quasi solo testuali: molto leggibile,
   ma richiede un riallineamento più ampio per convivere con carte e avatar decorati.
3. **Tutto ornamentale.** Ogni comando diventa un medaglione: coerente se rifatto
   integralmente, ma rischia di conservare proprio l'affollamento lamentato.

## Chiusura e navigazione

| Contesto | Comando proposto | Regola |
|---|---|---|
| Schermata secondaria | «Indietro» in alto a sinistra | Testo panna; piccolo chevron facoltativo, senza medaglione |
| Popup informativo | «Chiudi» nel bordo inferiore | Sempre visibile, fuori dall'area che scorre; secondario, non oro |
| Impostazioni al tavolo | «Chiudi» | Chiude il pannello e torna al tavolo; non abbandona la partita |
| Conferma di azione distruttiva | «Annulla» + verbo esplicito | Per esempio «Abbandona»; rosso riservato all'azione distruttiva |
| Rimozione da slot/lista | «Rimuovi» o simbolo meno dedicato | Non convertire indiscriminatamente tutte le X in «Chiudi» |

«Chiudi» e «Indietro» restano testi localizzabili, mai incorporati nei PNG.
Non duplicare X e pulsante testuale. Il tocco esterno può chiudere popup innocui
come comodità aggiuntiva, senza diventare l'unico modo di uscire; non deve attivare
accidentalmente il tavolo sottostante. Evitare perdita di modifiche nei moduli.
Tasto/gesto indietro deve seguire la stessa gerarchia e chiudere prima il popup.
Rivedere anche le istruzioni esistenti che citano «la X in alto».

## Specifiche per il nuovo set

Due famiglie coordinate: **oggetti illustrati** per premi/destinazioni e
**simboli semplici** per comandi. Stessa morbidezza, palette e direzione della luce;
non rendere un ingranaggio prezioso quanto una ricompensa.

- Palette di partenza K2: blu `#16283C`, oro `#E8B24A`, panna `#FAF4E0`,
  secondario `#8FA6BC`, bordo discreto `#2E4F6C`. Ombre e luci possono variare
  attorno a questi toni; niente legno, cromature o nuovi materiali per i comandi.
- Oggetti: volume morbido, luce dall'alto a sinistra, un'ombra corta e pulita;
  evitare più bordi concentrici, riflessi bianchi duri e dettagli minuscoli.
- Comandi: forme piene o tratti arrotondati uniformi, senza scatola né alone fisso.
  Le icone devono restare riconoscibili anche senza colore.
- Dimensioni seguenti **di produzione proposte**, da verificare alla scala UI reale:
  master oggetti 512×512, master simboli 256×256, PNG RGBA trasparente + sorgente
  modificabile. Silhouette nel 75–80% del riquadro, con margine per ombra; pivot centrale.
- Centrare otticamente la forma visibile, non soltanto il file. Coppa, busta e
  scrigno devono avere peso visivo simile; controllare insieme, non separatamente.
- Nessun testo, numero, badge o stato selezionato dipinto nel file dell'icona.
  Esportare supporti e simboli separati; documentare eventuali bordi per 9-slice.
- Verifica indicativa: oggetti leggibili a 48 e 64 px, simboli a 24 e 32 px.
  Queste sono prove di leggibilità del raster, non misure imposte ai prefab.
  Conservare un'area di tocco comoda anche quando l'icona visibile si riduce.

| Asset / gruppo | Come deve essere | Da evitare | Priorità |
|---|---|---|---|
| `ic_nav_back` opzionale | Chevron panna corto e arrotondato, accanto a «Indietro» | Freccia sottile, disco di legno, bottone incorporato | P1 |
| `ic_reward` | Scrigno blu con pochi dettagli oro, sagoma leggibile | Quadrato di fondo, tesoro minuscolo, bagliore permanente | P1 |
| `ic_ranking` | Coppa oro compatta, manici ampi, base semplice | Cornice aggiunta, incisioni illeggibili | P1 |
| `ic_mail` | Busta panna con piega chiara e dettaglio oro discreto | Medaglione, sigilli troppo piccoli, bordo blu esterno | P1 |
| `ic_settings`, `ic_audio`, `ic_help` | Ingranaggio, altoparlante, punto interrogativo della stessa famiglia panna | Stile emoji di sistema, cromature, quadrato contenitore | P1 |
| `ic_leave`, `ic_remove` | Porta/uscita e meno; significati distinti, accompagnati dal testo dove necessario | Riutilizzare la X rossa per qualsiasi azione | P1 |
| Carte, negozio, profilo/amici | Riutilizzare dove possibile; uniformare luce, dimensione percepita e saturazione | Ridisegnare asset già coerenti senza necessità | P2 |
| Selezione navigazione | Piccolo segno o luce locale sotto l'icona; etichetta chiaramente attiva | Grande placca oro dietro ogni destinazione | P2 |
| Notifica | Badge piccolo separato, rosso solo quando utile; numero testo runtime | Numero dipinto nell'icona, badge decorativi fissi | P2 |
| Divisori / righe | Linea blu attenuata o semplice spazio | Una nuova cornice per ogni riga | P2 |

## Ordine di lavoro e accettazione

1. **P0: campione prima del set.** Presentare insieme «Indietro», un popup con
   «Chiudi» e scrigno/coppa/busta sullo sfondo reale. Valutare densità e leggibilità.
2. Approvato il campione, produrre P1; uniformare P2 solo dove resta incoerenza.
3. Integrare nei componenti e nei builder: K2 oggi reintroduce il riquadro blu.
   Non basta cambiare lo sprite nei singoli prefab.
4. Verificare avvio/accesso, Home, popup, Impostazioni al tavolo, Collezione e Profilo:
   testo leggibile, nessuna doppia chiusura, nessun comando perso, area di tocco
   conservata, navigazione coerente e nessuna sovrapposizione su telefoni stretti.
5. Provare premuto, disabilitato, selezionato e notifiche; preferire tint/scala già
   disponibili a esportazioni duplicate. Distinguere gli stati anche senza il solo colore.

Il primo lotto non richiede un nuovo sfondo, nuovi font o un'intera skin.
La pulizia dei layout, i testi, gli stati e la rimozione dei contenitori sono attività
UI: non aggiungerli alla commissione come decine di immagini nuove.

Riferimenti: `SPRINT_BACKLOG.md` I2/I8/I9/K2/K10; `docs/ui/design-system.md`;
`Assets/Editor/UIV2FoundationBuilder.Auth.cs` (freccia), builder Settings/Collection/Table
(usi della X), `Assets/Editor/UIV2DesignBuilder.cs` (stile dei pulsanti icona).
