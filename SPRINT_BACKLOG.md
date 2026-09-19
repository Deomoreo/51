# 51 — Backlog dello sprint

Lista viva di tutto quello che resta da fare. Ogni nuova idea si aggiunge qui con un codice.
`UI_INTEGRATION_ROADMAP.md` resta il resoconto dettagliato delle consegne.

Stato: ☐ da fare · ◐ in corso · ☑ fatto · ⏸ rimandato · ❓ da decidere

Regola: bug e rifiniture di cose esistenti si fanno in ordine. Schermate e sistemi nuovi partono solo dopo il tuo via, uno alla volta.

---

## ▶ PUNTO DI RIPRESA — 19/09, versione 1.98

**Ultimo lavoro fatto (19/09):**
- G1 chiuso (1.93): tavolo provato su tablet 3:4, 20:9 e iPhone; mano più vicina al fondo sui telefoni lunghi.
- I1 chiuso (1.94): dorso delle carte al tavolo. Vedi Sprint 6.
- I3 chiuso (1.95): emoticon rapide al tavolo al posto del pannello.
- I2 chiuso (1.96): icone centrate sulla faccia dei riquadri blu e oro.
- C9 chiuso (1.97): scritte GIOCA e GIOCA COME OSPITE con il contorno bruno del mockup.
- G6 chiuso (1.98): tavolo sfocato dietro il fine smazzata.

**⚠ ATTENZIONE, prima di tutto: G4.** Tutto il lavoro dopo il checkpoint `b21dc11` (circa 330 file:
UIV2, audio, legale, builder, dorsi...) **non è ancora in un commit git**. Branch: `codex/home-v2-training`.
Se il disco si rompe o qualcuno fa un reset, si perde. Va fatto un commit appena possibile.

**Come riprendere (per chiunque, anche senza Claude):**
- Questo file è la lista viva. `UI_INTEGRATION_ROADMAP.md` è il resoconto dettagliato delle consegne.
- La UI si costruisce con gli script del menu Unity `Tools/UIV2/...` (cartella `Assets/Editor`):
  non modificare a mano le scene o i prefab che quegli script generano, rilancia lo script.
- Versione in `ProjectSettings > bundleVersion`: +0,01 a ogni giro di modifiche.
- Test: `Window > General > Test Runner > EditMode`, oggi 228 su 228 passati.
- Mockup in `Assets/Mockup/`. Da ora sono un punto di partenza, non un vincolo (vedi I8).

**Ordine deciso il 19/09 (prima la qualità, poi i sistemi):**
1. **G4** commit di tutto (10 minuti, toglie il rischio più grosso)
2. **Qualità** sull'app che c'è già: K3 librerie → K1 kit di movimento → K2 sistema di design →
   K4 shader → K5 tavolo → K6 vibrazione e particelle → I5 grafica ridotta → K7 flusso veloce
3. **I7** giro di prova vero dell'app (anche il bug della Cirulla a tavolo vuoto)
4. **Sistemi**, sopra una base già curata, seguendo "Economia e regole" nello Sprint 6:
   E1+D1 (XP, livelli, profilo) → F1 + J4 (monete, gemme, barra in alto) → E5 (tavoli con puntata) →
   E2 (giornaliere) → E3 (missioni) → F2 (negozio) → F5/J5 (sblocchi) → J2 (pass mensile) → F4 + K11
   (video e pubblicità) → F3 (acquisti) → D3 (posta) → D2 (avatar e cornice) → I4 (profilo rapido) →
   D4 (amici) → J3 (frasi rapide e chat amici) → K8 (tutorial)
5. **G2** build Android e prova su telefono vero (anche prima, quando si vuole provare in mano)

---

## Sprint 1 — Tavolo: bug

| # | Cosa | Stato |
|---|------|-------|
| A1 | Ultima carta del giro: l'animazione si ferma, le carte del tavolo spariscono e ricompaiono solo a fine timer accuso | ☑ 1.79 |
| A2 | Scelta tra più prese: resta a schermo un quadratino giallo; le opzioni sono rettangoli di testo con simboli illeggibili → nuova grafica | ☑ 1.79 |
| A3 | Ventaglio della mano: la carta centrale è coperta da quelle ai lati | ☑ 1.79 |
| A4 | Mani degli avversari (posti 1, 2, 3): il banner copre le carte, soprattutto al posto 2 | ☑ 1.79 |
| A5 | Prese dei giocatori sparse sul tavolo → posizione ordinata vicino al banner (le scope dietro al banner restano) | ☑ 1.79 |
| A6 | Emoticon in partita: solo le 3 equipaggiate, non tutte e 6 | ☑ 1.79 |
| A7 | Roulette mazziere: con 2 giocatori mostra 4 posti | ☑ 1.79 |
| A8 | Barra in alto: la scritta del turno si sovrappone a "Mano X di Y" | ☑ 1.80 |
| A9 | Chip MAZZIERE sul banner copre avatar e nome | ☑ 1.80 |
| A10 | Accuso del mazziere: il mazzetto mostra già le carte prese prima dell'animazione; testo "Tu fa Scopa da 30!" da correggere | ☑ 1.80 |
| A11 | Carte scoperte di un avversario dopo un accuso: la matta appare come carta gialla (va con B4) | ☑ 1.80 |
| A12 | Pannello emoticon: pulsante chiudi con "X" di testo invece dell'icona del mockup | ☑ 1.80 |

## Sprint 2 — Tavolo: sequenze ed effetti

| # | Cosa | Stato |
|---|------|-------|
| B1 | Roulette mazziere dai mockup 27–28: nastro, riquadri ai posti, trofeo, chip MAZZIERE, CONTINUA che si chiude da solo dopo 4 secondi | ☑ 1.79 (bagliore del vincitore e sfocatura vera: ☑ 1.81) |
| B2 | Accuso manuale più evidente, finestra di almeno 5 secondi | ☑ 1.80 |
| B3 | Animazione pugno: tutte le carte (mani e tavolo) saltano a caso restando visibili al proprio posto, poi si riprende a giocare | ☑ 1.80 |
| B4 | Effetto del 7 di coppe (matta) che si trasforma in un'altra carta per l'accuso | ☑ 1.80 |
| B5 | Impostazioni in partita (mockup 26): animazioni veloci, suggerimenti mosse, musica, effetti, abbandona partita. Toglie anche l'ESCI in più dai risultati | ☑ 1.81 (musica ed effetti salvano solo la scelta: nel gioco non ci sono ancora suoni; l'abbandono non registra la sconfitta finché non c'è D1/E1) |
| B6 | Reveal accuso del mazziere: tempi e grafica definitivi | ☑ 1.82 |
| B7 | Multiplayer: i client che non sono host non vedono la sequenza del mazziere | ☑ 1.82 (provato simulando un client nell'Editor, non ancora con due dispositivi veri) |

## Sprint 3 — Accesso, Home, stile

| # | Cosa | Stato |
|---|------|-------|
| C1 | Login V2 (mockup 23) | ☑ 1.84 (ritarato sui pixel del mockup nella 1.84) |
| C2 | Registrazione V2 (mockup 24) | ☑ 1.84 |
| C3 | Novità (mockup 25): contenuto e punto da cui si apre | ☑ 1.87 pagina dal pulsante "Novità" della schermata iniziale; contenuti da **PlayFab Title News** (Game Manager → Content → Title News), NUOVO = non ancora viste. Oggi PlayFab non ha notizie: si vede "Nessuna novità per ora" |
| C4 | Poppins su tutta la UI. Tutti i pesi presenti (Regular→ExtraBold); Poppins Medium è il font predefinito dal 17/09. Resta da applicarlo alle schermate esistenti | ☑ 1.90 `Tools/UIV2/Apply Poppins Everywhere`: 519 testi da LiberationSans a Poppins (grassetto → Poppins Bold, resto → Medium), contorni navy/marrone ricreati sul nuovo atlas. Controllati nel Simulator: iniziale, Home, Modalità, Impostazioni, Collezione, Profilo, tavolo |
| C5 | Bagliore morbido dietro Accuso, Gioca, trofeo e pulsanti principali | ☑ 1.91 `Tools/UIV2/Build Soft Glows`: GIOCA (Home), GIOCA COME OSPITE, riga selezionata di Modalità (teal, segue la selezione), trofeo e RIVINCITA dei risultati. Accuso aveva già il suo (pulsa durante la finestra). Il bagliore a pillola usa il "Bagliore morbido cerchio" in 9-slice al centro |
| C6 | Effetti particellari: schermata iniziale, Home, pagine, ricompense, vittoria | ◐ 1.91 `Tools/UIV2/Build Mote Fields` + componente `UIV2MoteField`: pulviscolo oro in schermata iniziale e Home/pagine, scoppio di luce dal trofeo quando vinci. **Ricompense**: manca la schermata (E2/F1), il componente ha già `Burst()` per quando ci sarà. Scintille a stella: asset mancante |
| C7 | Pannello Modalità allineato al mockup panel_modalita_v2 | ☑ 1.91 `Tools/UIV2/Calibrate Quick Mode Panel`: righe 28/19 ExtraBold con contorno navy spesso, sottotitoli del mockup, titoli di sezione con la linea, "Difficoltà" grigio chiaro, pillole con contorno, barra di scorrimento oro, bagliore teal sulla riga scelta |
| C8 | Impostazioni: "51Cirulla · v…" usciva sotto la cornice (dalla 1.86, riga Elimina account) | ☑ 1.90 cornice 1650, scritta dentro; corretto nel builder Build Delete Account |
| C9 | Scritte GIOCA (Home) e GIOCA COME OSPITE senza il contorno marrone spesso del mockup | ☑ 1.97 `Tools/UIV2/Style Gold Button Labels`: Poppins ExtraBold, faccia #FFFCF2 piatta (il prefab aveva un gradiente che la tingeva di crema), contorno bruno #945408 di 5-6 px, corpo dall'altezza delle maiuscole del mockup (38 e 22 px). Misurato al pixel contro i mockup. Gli altri pulsanti oro (CONTINUA, RIVINCITA, REGISTRATI PER SALVARE...) hanno ancora lo stile vecchio: da uniformare in I7 se ti piace questo |
| C10 | Bagliori dietro i pulsanti: sembravano una lastra rettangolare | ☑ 1.92 gradiente smoothstep generato (`glow_soft_pill`), parte sotto il pulsante e sfuma in 20-35 px |
| C11 | Titoli sui nastri dei pannelli (Modalità, Mazzo, Impostazioni, stanze, risultati, roulette...) | ☑ 1.92 `Tools/UIV2/Calibrate Panel Titles`: 14 titoli in ExtraBold bianco con contorno verde scuro, centrati sul nastro come nei mockup |
| C12 | Chiudere i pannelli toccando fuori | ☑ 1.92 `DismissOnBackdrop` (`Tools/UIV2/Build Backdrop Dismiss`): Modalità, Mazzo, Crea/Entra stanza, Emoticon, Personalizza. Esclusi di proposito ricerca partita, sale d'attesa, risultati, roulette |
| C13 | Barra in basso sollevata dal fondo, icone piccole | ☑ 1.92 `BottomNavSafeAreaBleed`: sfondo fino al bordo e contenuto più in basso sui telefoni con barra di sistema; icone 62→72, linguetta oro 238x117→262x128 (9-slice) |

## Sprint 4 — Profilo, social, progressione

| # | Cosa | Stato |
|---|------|-------|
| D1 | Profilo: dati reali (livello, XP, statistiche) | ☐ |
| D2 | Scelta di icona e banner nel profilo, visibili al tavolo | ☐ |
| D3 | Posta (mockup 06): messaggi e ricompense dal server | ☐ |
| D4 | Amici (mockup 07): lista, richieste, invito in stanza (attiva "Amici" in sala d'attesa) | ☐ |
| D5 | Classifica, tornei, eventi | ⏸ futuro |
| E1 | XP a fine partita e livelli (compreso l'allenamento, oggi non assegna XP) | ☐ |
| E2 | Ricompense giornaliere (mockup 08) | ☐ |
| E3 | Missioni (servono anche per sbloccare i mazzi) | ☐ |
| E4 | Penalità per abbandoni ripetuti | ☐ |
| E5 | ~~Sistema di energia~~ → **tavoli con puntata in monete** (deciso il 17/09): allenamento sempre gratis, ricarica con bonus giornaliero, video e negozio. Va con F1 e F4 | ☐ |

## Sprint 5 — Negozio e sblocchi

| # | Cosa | Stato |
|---|------|-------|
| F1 | Valute (monete e gemme) con saldo salvato sul server | ☐ |
| F2 | Negozio (mockup 16) con acquisti in gemme | ☐ |
| F3 | Acquisti con soldi veri (Google Play / App Store) | ☐ |
| F4 | Video pubblicitari con premio | ☐ |
| F5 | Mazzi bloccati con anteprima, sblocco con acquisto, missioni o livello | ☐ |
| F6 | Tavoli da gioco sbloccabili | ⏸ futuro |

## Sprint 6 — Richieste del 19/09 (da provare e rifinire, non più "copiare il mockup")

### Bug e rifiniture: si fanno in ordine, senza bisogno di via

| # | Cosa | Stato |
|---|------|-------|
| I1 | Dorso delle carte al tavolo: si vedeva sempre il napoletano | ☑ 1.94. Due cause: (1) il tavolo leggeva il mazzo dal `MatchConfig` salvato in PlayerPrefs, che valeva "default" (= napoletano) o un mazzo vecchio in ogni percorso di avvio che non lo riscriveva; ora usa sempre il mazzo scelto dal giocatore (`CardDecks.LoadForMatch()`). (2) Il mazzo Classico non aveva un dorso suo: collegato `51_CARD_BACK_MASTER.png` (sorgente 4x, PPU tarato perché abbia la stessa misura delle facce). Test: ogni mazzo ha un dorso diverso |
| I2 | Icone nei riquadri piccoli non centrate | ☑ 1.96 causa: i riquadri `sq_blue`/`sq_gold` hanno il bordo 3D più spesso sotto, quindi la faccia chiara sta 4-7 px più in alto del centro del rettangolo e le icone sembravano basse (più i margini trasparenti delle icone). `Tools/UIV2/Center Icons On Button Faces` mette il centro visibile di ogni icona sul centro della faccia: 14 icone (Home, Profilo, X di chiusura, schermata iniziale, tavolo). Controllato a schermo prima/dopo. Coperti solo i riquadri blu e oro in MainMenu e GameScene: se ne vedi altri storti, dimmi quali |
| I3 | Emoticon al tavolo: si apriva un pannello che copriva il tavolo e fermava il gioco. Ora c'è una **scelta rapida** | ☑ 1.95 striscia sopra Emoji con le 3 equipaggiate (stile dei banner), un tocco invia e chiude, si chiude da sola dopo 3,5 s, un tocco fuori la chiude e passa comunque sotto, niente velo. `Tools/UIV2/Build Emoticon Quick Bar`. Segue il pulsante anche quando scende sui telefoni lunghi. Il vecchio pannello `GamePresentationV2/Emoticons` resta in scena ma non si apre più (è il ripiego se la striscia manca). Il tocco fuori non è stato provato con un dito vero (solo in Editor) |
| I4 | Profilo rapido al tavolo: tocco sul banner di un giocatore → scheda piccola (mockup `10_profilo_rapido`) | ☐ non esiste ancora (dipende da D1/D2 per i dati veri) |
| I5 | Impostazioni → **Grafica ridotta**: spegne particelle, bagliori pulsanti, sfocature, sfondo animato, shader e accorcia le animazioni (per telefoni lenti e batteria). Va fatta insieme a K1-K6, così ogni effetto nuovo nasce già spegnibile | ☐ oggi c'è solo "animazioni veloci" nelle impostazioni del tavolo. Proposta da confermare: una sola scelta, valida sia nelle Impostazioni della Home sia in quelle al tavolo |
| I6 | Sfondo animato nella Home | ☐ c'è solo il pulviscolo oro (C6). Da decidere cosa si muove: gradiente lento, luci, parallasse. Può servire un asset |
| I7 | **Giro di prova completo dell'app** (avvio → accesso → Home → partita → risultati → rivincita/Home, e online): trovare tutto ciò che è lento, macchinoso, poco chiaro o grezzo, e correggerlo. Obiettivo: velocità, fluidità e chiarezza al massimo | ☐ Già notato il 19/09: la Cirulla di un bot a inizio smazzata è partita (pugno e carte dell'accuso) quando al tavolo non c'era ancora nessuna carta, né mano né tavolo: il pugno "fa saltare" un tavolo vuoto. Da verificare l'ordine distribuzione → finestra accuso |
| I8 | Direzione: i mockup servivano a dare vita all'app, da ora le migliorie le decidiamo noi. UI pulita, niente dettagli inutili | regola |
| I9 | Icone nuove senza "quadrato + icona dentro" (non piacciono) | ⏸ futuro: servono asset nuovi |

### Sistemi: ognuno parte solo con il tuo via

| # | Cosa | Stato |
|---|------|-------|
| J1 | **Decidere l'economia**, prima di costruire i sistemi sotto | ☑ decisa il 19/09: vedi "Economia e regole". I numeri sono valori di partenza da tarare giocando |
| J2 | Pass **mensile** (deciso il 19/09, non settimanale): circa 30 livelli, fila gratis + premium, tema grafico per stagione; missioni nuove ogni settimana | ☐ dopo E1, F1, E3 |
| J3 | Chat (deciso il 19/09): al tavolo **solo frasi rapide** pronte ("Bella giocata!", "Ancora una?") più emoticon; **chat libera solo tra amici**, con filtro parolacce, segnala e blocca | ☐ dopo D4. Servizio chat da scegliere (es. Photon Chat) |
| J4 | Barra in alto della Home funzionante: livello e XP si leggono già; **monete e gemme non ci sono** (`SetResources(null)`) | ☐ con F1 |
| J5 | Sblocco di animazioni accuso e tavoli (oltre ai mazzi di F5) | ☐ dopo J1 |

### Qualità e stile (deciso il 19/09: si fa PRIMA dei sistemi)

Obiettivo: l'app deve sembrare disegnata, non assemblata. Pubblico misto (adulti e giovani): base
pulita e leggibile, con animazioni e premi che danno soddisfazione. Grafica: per ora asset attuali
rifiniti da noi; l'artista UI arriva dopo il lancio (K10).

| # | Cosa | Stato |
|---|------|-------|
| K1 | **Kit di movimento** unico, applicato ovunque da un builder: pulsanti che si schiacciano e rimbalzano al tocco, pannelli che entrano ed escono con un piccolo rimbalzo, numeri che contano, ricompense che volano verso il contatore, passaggi morbidi tra pagine e scene. Oggi quasi tutto compare di colpo | ☐ |
| K2 | **Sistema di design fissato**: 3-4 tipi di pulsante (primario oro, secondario blu, piatto, icona), 2 tipi di pannello, scala dei testi (titolo, sottotitolo, testo, didascalia), palette. Poi ogni schermata si riallinea a quello. Comprende lo stile oro di C9 su tutti i pulsanti oro (CONTINUA, RIVINCITA...) | ☐ |
| K3 | Librerie gratuite MIT: **UIEffect** (riflessi, dissolvenze, gradienti, ombre sulla UI) e **UIParticle** (particelle vere dentro i pannelli) | ☐ approvate |
| K4 | **Shader nostri**: riflesso di luce che passa su pulsanti, carte e oggetti rari; bagliore calcolato sulla forma esatta del pulsante (sostituisce i bagliori allungati, mai più storti); dissolvenza o bruciatura per carte speciali (accuso, matta); olografico per i mazzi rari; sfondo animato della Home (I6); sfocatura su scheda video, più veloce | ☐ approvati |
| K5 | **Tavolo più ricco**: panno con texture e luce al centro, ombre sotto le carte, mano che reagisce al tocco, carte giocate con più peso | ☐ |
| K6 | **Risposta a ogni tocco**: vibrazione (con interruttore nelle Impostazioni) e particelle sui momenti forti (scopa, accuso, vittoria, premi) | ☐ |
| K7 | **Flusso veloce**: dall'apertura alla partita in 2 tocchi (l'ospite già entrato salta la schermata iniziale), roulette del mazziere più breve o saltabile, risultati che proseguono da soli dopo qualche secondo | ☐ |
| K8 | **Tutorial**: la prima volta una partita guidata contro un bot (prese, scope, accuso) + pagina Regole sempre consultabile | ☐ deciso |
| K9 | Segnalazione crash e statistiche d'uso (dove la gente abbandona). Da dichiarare nella Privacy (H3) | ☐ |
| K10 | Artista UI per guida di stile e pezzi chiave (pulsanti, pannelli, icone, cornici; vedi I9) | ⏸ dopo il lancio, quando i giocatori crescono |
| K11 | Pubblicità tra le partite, regola leggera: al massimo una ogni 3 partite finite e non prima di 3 minuti dall'ultima; mai durante la partita, mai nelle prime 2 partite del giorno, mai a chi ha comprato qualcosa | ☐ con F4 |

### Economia e regole (deciso il 19/09; numeri di partenza da tarare)

Principi: **mai pagare per vincere**, si vende solo estetica. Guadagno da estetica, pass premium,
video facoltativi e poca pubblicità (K11), senza dare fastidio. Saldi e premi decisi dal server
(PlayFab), mai dal telefono, altrimenti si imbroglia.

- **XP** (livelli, sblocchi): online vittoria 40, sconfitta 20, +2 per scopa, +5 per accuso (bonus massimo +20).
  Allenamento: metà XP. XP per passare di livello: 100 + 20 × (livello - 1).
- **Monete** (valuta di gioco): servono per entrare nei **tavoli con puntata** (E5). Fasce 100 / 500 /
  2.000 / 10.000, sbloccate ai livelli 1 / 5 / 10 / 20. Chi vince prende il piatto meno il 10% del banco
  (a coppie si divide). Allenamento: 10 monete a partita, massimo 100 al giorno.
- **Ricarica gratis**: sotto il minimo del tavolo più basso, una volta al giorno 500 monete, più un video
  facoltativo per averne altre 500. Nessuno resta mai bloccato.
- **Gemme** (valuta premium): si comprano; poche gratis (ogni 5 livelli, pass, 7° giorno delle giornaliere).
  Servono per estetica e pass premium.
- **Giornaliere (E2)**: calendario di 7 giorni, il 7° con gemme; se salti un giorno la serie riparte.
- **Missioni (E3)**: 3 giornaliere + 5 settimanali; danno monete e punti del pass.
- **Pass (J2)**: mensile, circa 30 livelli, fila gratis + premium.
- **Sblocchi, misti**:
  - mazzi: alcuni col livello, alcuni nel pass, i più belli nel negozio, qualcuno con missioni o eventi;
  - tavoli: livello + monete;
  - animazioni accuso: pass e negozio;
  - emoticon: 6 di base, altre da pass e negozio;
  - avatar e cornici: traguardi, pass, negozio.
- **Video facoltativi (F4)**: raddoppiare le monete di fine partita, ricarica extra, un premio in più al giorno.

Precisazioni sulle voci già esistenti:
- **F2 Negozio**: esiste solo la grafica (`ShopScreenV2`, con dati finti di anteprima), non è collegato a niente.
- **D3 Posta**: oggi il badge è fisso a 0 e non arrivano messaggi. Deve funzionare anche per gli ospiti,
  non solo per chi ha fatto l'accesso (anche l'ospite ha un account PlayFab).
- **D2 Profilo**: nella pagina Profilo un pannello per scegliere avatar e cornice, visibili poi nel banner
  al tavolo e nel profilo rapido (I4).
- **E2 Ricompense giornaliere**: confermate, dopo J1.
- **D4 Amici**: tutta la logica (lista, richieste, invito in stanza) è da fare; la schermata c'è.

## Già aperti dalle sessioni precedenti

| # | Cosa | Stato |
|---|------|-------|
| G1 | Adattamento a schermi con proporzioni diverse | ☑ 1.93 — 1.92 tavolo su area di design 1080x1920 centrata: `CameraResponsiveFit` sulla camera (larghezza bloccata sui telefoni stretti, altezza sui tablet), `PortraitCanvasMatch` sui Canvas del tavolo, banner/pulsanti/roulette/bolle ancorati al centro (`Tools/UIV2/Apply Table Design Area`), carte in pixel del mockup (mano 272, tavolo 157: erano giganti). Verificato nel Simulator iPhone 1170x2532 e a 1080x1920. ☑ 1.93: provati in partita tablet 1536x2048 (3:4) e 1080x2400 (20:9), tutto allineato. Il limite 2,1 non è attivo (modalità "Native Aspect Ratio": il valore conta solo in Custom), quindi niente bande nere. Sui telefoni allungati il posto locale (banner, Emoji/Accuso, bolla, e con loro mano e prese) scende di metà dello spazio libero in basso, safe area esclusa, al massimo 120 px di design (`LocalSeatBottomShift`, applicato dal builder): 20:9 ≈ 120, iPhone ≈ 57, 9:16 e tablet 0 |
| G2 | Build Android e prova su dispositivo | ☐ |
| G3 | Invito tramite link in sala d'attesa | ☐ |
| G4 | Commit del lavoro fatto dopo il checkpoint b21dc11 | ☐ |
| G5 | Musica ed effetti sonori | ☑ 1.82 (31 file consegnati il 17/09 e collegati; `Resources/Audio/SoundLibrary` per regolare i volumi) |
| G7 | Audio non ancora usato: premi in monete e gemme (servono con F1/E2), ui_confirm_01/03, match_start_TEMP e il doppione home_theme_loop.wav | ☐ |
| G8 | Annulla/esci da "crea stanza" e "entra in stanza privata" tornava alla Home invece che al pannello Modalità | ☑ 1.83 |
| G9 | Suoni della partita che continuavano dopo essere usciti dal tavolo | ☑ 1.83 |
| G10 | Roulette del mazziere: partiva (con i suoi suoni) mentre il caricamento copriva ancora lo schermo, e la fanfara d'inizio arrivava per prima | ☑ 1.83 |
| G6 | Sfocatura vera e velo anche dietro fine smazzata (mockup 13), fine partita ed emoticon | ☑ 1.98 fine smazzata con il tavolo sfocato + velo "Sfocatura sfondo" al posto del nero al 72% (`Tools/UIV2/Build Round Results Blur`, foto scattata prima che il pannello compaia). Fine partita lasciato così: è a schermo intero con fondo pieno (mockup 12), la sfocatura non si vedrebbe. Emoticon: il pannello non si apre più (I3) |
| H1 | Termini e Privacy leggibili in app (finestra scrollabile dai link della registrazione) | ☑ 1.85 |
| H2 | Recupero password: template configurabile, messaggio neutro, nessun errore PlayFab grezzo a schermo | ☑ 1.85 (manca il template su PlayFab: vedi sotto) |
| H3 | Privacy: dichiarati gli SDK realmente presenti (PlayFab, Photon, Google Play Games) | ☑ 1.85 |
| H4 | URL pubblici (Termini, Privacy, Elimina account, Reset password) + deploy della cartella `Web/` | ☐ **serve te** |
| H5 | Template email `51_PasswordRecovery` su PlayFab Game Manager + SMTP del titolo | ☐ **serve te** |
| H6 | Backend/serverless per reset password ed eliminazione account (Secret Key solo lato server) | ☐ **serve te** |
| H8 | La X di Termini/Privacy aperti da Accesso/Registrazione non funzionava (solo Esc) | ☑ 1.87 |
| H9 | Logo 51 della schermata iniziale in Accesso e Registrazione al posto del riquadro "51" | ☑ 1.87 |
| H10 | Da ospite già entrato: Accesso non offre più "Accedi come ospite"; Opzioni → riga Account apre la Registrazione V2 (non il vecchio pannello Ospite) | ☑ 1.88 |
| H11 | Opzioni spostate dal Profilo alla colonna della Home, sotto Posta | ☑ 1.88 |
| H12 | Pannello account per chi ha un login vero (prima il vecchio "Logout / Back" senza grafica V2) | ☑ 1.89 "Il tuo account": nome, nome utente, email (da PlayFab), ID giocatore, ESCI DALL'ACCOUNT → schermata iniziale |
| H7 | `Impostazioni → Account → Elimina account` in app, con doppia conferma | ☑ 1.86 lato app (`AccountDeletionService` → `POST {BackendBaseUrl}/api/delete-account`, `Authorization: Bearer <SessionTicket>`). Finché H6 non c'è e `BackendBaseUrl` è vuoto mostra "non ancora disponibile", mai un falso successo. 1.87: la voce compare solo dopo l'ingresso e solo con login vero (email), non per gli ospiti; prima dell'ingresso nelle Opzioni non c'è nessuna riga Account |

---

## G1 — perché il tavolo è storto nel Simulator (diagnosi del 17/09)

Il tavolo (carte, pile, feltro) è **mondo 3D**, disegnato da una camera ortografica; i banner, la
barra e i pulsanti sono **UI**. I due mondi oggi seguono regole diverse, e questo è tutto il difetto.

1. `Main Camera` di GameScene è ferma a `orthographicSize = 5`: mezza altezza fissa, larghezza
   visibile = `5 x aspect`. Cambia il telefono, cambia la larghezza del tavolo.
   - 1080x1920 (9:16) → area visibile 5,63 x 10
   - 1170x2532 (iPhone, quello del Simulator) → **4,62 x 10: 18% più stretta**
   - tablet 3:4 → 7,50 x 10: molto più larga, tutto sperduto al centro
2. I Canvas usano invece `ScaleWithScreenSize 1080x1920` con `match = 0,5`, cioè una media fra
   larghezza e altezza. Fuori dal 9:16 **UI e tavolo scivolano uno rispetto all'altro**: i banner
   non stanno più dove stanno le carte.
3. Esiste già uno script `CameraResponsiveFit` che farebbe il lavoro giusto, ma **non è attaccato
   alla camera**, ed è tarato su un'area di riferimento 12 x 8,5 (contro il 5 di adesso): non basta
   accenderlo, le posizioni del tavolo vanno riconciliate con l'area scelta.
4. ~~`androidMaxAspectRatio = 2,1` manda in bande nere i 20:9~~ Corretto il 19/09: con
   `androidSupportedAspectRatio: 1` (Native Aspect Ratio) quel valore non viene usato.

Lavoro necessario: scegliere un'area di gioco di riferimento, agganciarci la camera, allineare la
regola dei Canvas e riverificare le posizioni del tavolo a tre proporzioni (9:16, 20:9, tablet 3:4).
Non è una spunta da mettere: tocca il posizionamento di tutto il tavolo.

## Legale e account — stato al 18/09

Fatto in app (1.85), senza dipendere da niente di esterno:
- I testi stanno in `Assets/Legal/` come file, non nel codice: si aggiornano senza ricompilare.
- Dalla registrazione, "Termini di servizio" e "Privacy Policy" aprono una finestra scrollabile
  (`LegalModalV2`, costruita sul `UIV2ModalHost` e su `AnimatedModalV2` esistenti). Toccare un link
  NON spunta la casella: leggere non è accettare.
- `Assets/Resources/AppConfig.asset` raccoglie gli indirizzi pubblici e l'ID del template email.
  **Tutti i campi sono vuoti**: nessun URL è stato inventato. Qui dentro va solo roba pubblica.
- Recupero password: usa il template configurato (vuoto = quello predefinito di PlayFab) e risponde
  sempre la stessa frase, esista o no l'indirizzo, così nessuno può scoprire chi ha un account.
  Gli errori del servizio finiscono nel log, mai a schermo.
- Un segnaposto non ancora configurato non arriva mai sotto gli occhi dell'utente: sparisce insieme
  alla frase che lo conteneva.

### Due modifiche ai testi consegnati
1. Tolta dalla copia in app la nota **"Prima della pubblicazione: aggiornare questa sezione..."**:
   era un promemoria per lo sviluppatore, sarebbe finito sotto gli occhi degli utenti. L'originale
   nel pacchetto è intatto.
2. Riscritta la sezione 3 della Privacy con gli SDK **realmente presenti** nel progetto: PlayFab,
   Photon (Exit Games) e Google Play Games Services. Unity Analytics risulta disattivato
   (`UnityConnectSettings` tutto a 0) e non è dichiarato. Da rivedere se si aggiungono pubblicità,
   acquisti o crash reporting.

### Attenzione
La Privacy in app dice già che si può cancellare l'account da **Impostazioni → Account → Elimina
account**. Quel percorso **non esiste ancora** (H7) e senza backend (H6) non può funzionare.
Finché non c'è, il documento promette una cosa che l'app non fa.

## Parere sul sistema di energia

Consiglio di **non** mettere l'energia che blocca le partite, almeno all'inizio.

- **Pochi giocatori online.** Un gioco multiplayer appena uscito vive di persone in coda. Se l'energia ferma chi vuole giocare, le code si allungano e aumentano le partite con i bot.
- **Il 51 si gioca a sessioni lunghe.** Chi gioca a carte fa molte partite di fila. Fermarlo dopo 5 partite è il modo più veloce per fargli disinstallare l'app.
- **Nelle app di carte italiane funziona un altro modello:** tavoli con puntata in monete. Vinci e guadagni, perdi e paghi l'ingresso. Le monete si ricaricano con il bonus giornaliero, i video pubblicitari o il negozio. È un freno "morbido": l'allenamento contro i bot resta sempre gratis e senza limiti.
- **Guadagni migliori:** oggetti estetici (mazzi, tavoli, emoticon, animazioni accuso, banner), pass stagionale, acquisto per togliere la pubblicità e video facoltativi per raddoppiare i premi di fine partita.

**Deciso (17/09):** niente energia, E5 diventa "tavoli con puntata in monete".

## Asset mancanti da creare

Consegnati il 17/09: Poppins Regular/Medium/SemiBold, bagliore morbido cerchio e rettangolo, velo "Sfocatura sfondo" (la sfocatura vera si fa via codice sotto al velo, fatta nella 1.81).

Consegnati il 17/09 anche i 31 file audio (musica + effetti), collegati nella 1.82.

Consegnato il 17/09: `ic_arrow_left` (pulsante indietro tondo, usato in 23 e 24).

Consegnato il 18/09: pacchetto legale/auth (`51_Legal_Auth_Package`) con Termini, Privacy, template
email PlayFab, pagine web e riferimenti backend. Termini e Privacy sono in app dalla 1.85.

Ancora da fare, ma NON sono asset da disegnare: vedi "Legale e account" qui sotto.
