# 51 — Backlog dello sprint

Lista viva di tutto quello che resta da fare. Ogni nuova idea si aggiunge qui con un codice.
`UI_INTEGRATION_ROADMAP.md` resta il resoconto dettagliato delle consegne.

Stato: ☐ da fare · ◐ in corso · ☑ fatto · ⏸ rimandato · ❓ da decidere

Regola: bug e rifiniture di cose esistenti si fanno in ordine. Schermate e sistemi nuovi partono solo dopo il tuo via, uno alla volta.

---

## ▶ PUNTO DI RIPRESA — 24/09, versione 2.17

**2.17:** sfondo Home rifatto sul mockup (Assets/Mockup, 941x1672). Artwork tra bordo alto della safe area e nav
(Region/AboveNav, 170), cornice ancorata in alto: colonne intere sui telefoni alti, arco e gemma visibili su 9:16
e iPad (l'eccedenza scende dietro la nav). Verde dell'arco (Filler) nella barra di stato, bordo alto sfumato
(`Assets/Art/Generated/Home_TopFade.png`, 24 px d'artwork). Decorazioni ricollocate sul mockup; entrambi i nastri
da home_ribbon_right (sinistro specchiato). Verificato nel Simulator: iPhone 12, iPhone SE, iPad Mini 4.
Limiti: nastri non si avvolgono davanti/dietro le colonne, bastone con foglie (mockup liscio).

**2.16:** Home top bar: per gli ospiti esagono livello + barra XP nascosti (XpMax=0 in HomeV2Integration), verificato live.

**2.15:** sfondo Home a 1882x3344 (import 4096, ASTC). Stile icone scelto: CREMA. Rinominati ic_nav_back_cream,
ic_close_cream, ic_settings_cream. Mancano in crema: ic_chest, ic_mail, ic_trophy, ic_exit (poi cambio set in un colpo solo).

**2.13-2.14 (decisioni del report 24/09 + sfondo Home animato):**
- Ospite: niente più ingresso automatico (il login vero può ancora entrare da solo). Gli ospiti non prendono
  XP né ricompense: nel Profilo niente barra, al suo posto "Registrati per guadagnare XP". ID PlayFab nel
  Profilo: nascosto per gli ospiti, "#" + 8 caratteri per gli account.
- Musica: interruttore vero anche nelle Impostazioni della Home, stessa preferenza del tavolo.
- Etichetta "v1.83" → versione reale (`VersionLabelV2`, legge `Application.version`).
- Sfondo Home animato (`Tools/UIV2/Build Home Ambient`, `UIV2FoundationBuilder.HomeAmbient.cs`): base a
  riempimento senza bande, bagliore che respira, gemma sull'arco, nastri/denari/coppe/bastoni/spade ai bordi con
  micro-movimenti sfasati (`UIV2AmbientFloat`, DOTween, niente Update). Solo sulla pagina Gioca, fermo con Grafica
  ridotta (pose di riposo esatte). Centro libero per la Home. Posizioni/escursioni nella tabella
  `HomeAmbientLayers` del builder; decorazioni importate a 256/512 px con mipmap.
- Non collegati per ora: ic_volume, ic_questionmark, bar_pill_cream, badge_red (aspetta Posta/Premio), divider_gold.
- EditMode 281 passati, 0 falliti, 7 saltati. Verificato nel Simulator su iPhone 12 e iPhone SE.
  **Prossimo passo:** centro della Home (lo fa l'utente), asset mancanti del report 24/09.

**2.12 (E1+D1, 5 effetti, correzioni grafiche della sezione 1, asset nuovi):**
- XP: una sola curva `PlayerXp` 100+20×(L−1); fine partita 40 vittoria / 20 sconfitta, +2 a scopa e +5 ad accuso
  (max +20), metà in allenamento; l'abbandono conta come sconfitta. Riga "+XP" nei risultati con riempimento e lampo,
  scoppio al level up. Ospite: nome "Ospite XXXX", XP locale (`PlayerProgressLocal` ora si crea da solo).
- Home: numero del livello nell'esagono di bars.png; icone della colonna uniformi (riquadro 60×54), Premio non più tagliato.
- Tavolo: panno 60 px troppo in basso (scala camera vecchia nel builder) → banner bot 2/4 e bagliore Emoji/ACCUSO
  non stanno più sul bordo; intestazione più grande su sfumatura navy; ritratti avatar_0N nei banner.
- Emoticon nuove animate (Animator, 10 fps) al tavolo, frame 0 nei pannelli di scelta; set icone v2 al posto delle vecchie.
- Effetti: contorno che pulsa sulle carte giocabili, scia di 0,4 s sulla scopa, passaggi in dissolvenza di 0,25 s,
  vecchi canvas del MainMenu spenti davvero. Tutti spenti con Grafica ridotta.
- `SafeAreaFitter`/`SafeAreaTopOnly` ricalcolano anche al cambio di risoluzione: prima la Home restava schiacciata
  nel 64% sinistro su 16:9 dopo un cambio schermo (barre Android, foldable, Simulator). Verificato iPhone 12 → SE.
- EditMode 282 passati, 0 falliti, 7 saltati. **Prossimo passo: decisioni aperte nel report del 24/09**
  (identità ospite stabile, uso di ic_volume/ic_questionmark e dei pezzi inutilizzati di bars.png, centro Home vuoto).

**Fix 2.10 (robustezza flusso multiplayer, TurnController):** se il Master rifiuta una mossa del
giocatore di turno, rimanda a tutti il suo GameState (resync autoritativo; i duplicati da doppio tocco
fuori turno vengono ignorati). Il tocco del giocatore durante un'animazione non va più perso: viene
tenuto e rigiocato appena finisce, e prima dell'invio in rete si scarta se non è più valido.
**Da riprovare su due telefoni.**

**Fix 2.09 (presa doppia bloccata in multiplayer):** il tocco sul pannello "scegli la presa"
passava anche alla carta in mano sotto (`OnMouseDown` ignora la UI) e inviava uno scarto forzato
illegale: chi giocava lo applicava, l'altro client lo rifiutava → partite divergenti. Ora:
guard UI in `CardView.OnMouseDown`, niente più scarti forzati (CardViewManager/TurnController),
validazione mosse uguale su tutti i client, `RoundEndPanel` non intercetta più i tocchi da nascosto.
(I due punti aperti del 2.09 sono chiusi nel 2.10.)

**Fix build 2.08:** rimosso `SettingsModalUI.cs` (inutilizzato), che causava i 4 avvisi
"same field name is serialized multiple times". Multiplayer su dispositivo non funzionante:
nell'Editor partita rapida e privata funzionano. Causa probabile: la build release sceglieva
la regione Photon migliore per ogni telefono (l'Editor usa DevRegion eu), quindi i dispositivi
finivano in regioni diverse. Ora `FixedRegion = eu` in PhotonServerSettings. **Da riprovare
su due telefoni con la stessa build 2.08.**

**K7 completato (2.07):** l'ospite già entrato salta la schermata iniziale e arriva
alla Home; GIOCA avvia la partita con un tocco. Accesso e registrazione cancellano la
scorciatoia. La roulette del mazziere fa un solo giro breve, con lo stesso mazziere e
la stessa autorità. I risultati di smazzata proseguono da soli dopo un conto di 8 s,
una sola volta, solo offline o sull'host. La rivincita finale resta manuale. Verificato
con test EditMode e runtime su GameScene e MainMenu. Da provare con due client Photon
reali. Dettagli: `docs/ui/k7-flow-progress.md`.

**I5 completato (2.06):** Grafica ridotta è una scelta unica e persistente nelle
Impostazioni di Home e tavolo, con migrazione di Animazioni veloci. Spegne particelle,
coriandoli, shader decorativi, sfondo animato, sfocature e pulsazioni; accorcia i tempi
senza interrompere il volo K5. Indicatori utili fermi e vibrazione indipendente.
Ripristino anche a pannello aperto; effetti nuovi e riaperti rispettano la scelta.
**271 EditMode passati, zero fallimenti; runtime I5, 3 K5 e controlli K6 passati.**
Misure di batteria/prestazioni su telefono ancora da fare. Dettagli:
`docs/ui/i5-reduced-graphics.md`.

**K6 implementato (2.05):** feedback aptico sui tocchi accettati e sui momenti locali,
interruttore Vibrazione condiviso tra Home e tavolo; particelle finite su scopa,
accuso, vittoria locale e EXP realmente assegnata. Pool limitato a quattro emettitori,
disattivabile per I5. Volo K5 preservato. **261 EditMode passati, zero fallimenti;
3 runtime K5 e controlli runtime K6 passati.** Vibrazione fisica e build native ancora
da verificare su telefono. Dettagli: `docs/ui/k6-feedback-plan.md`. I5 completato nella 2.06.

**Correzione K5 (2.04):** segnalati scatti nel volo delle carte. La suddivisione della 2.03
fermava quasi la carta a metà volo e interrompeva lo spostamento all'80% della durata.
Ripristinata la traiettoria continua precedente, mantenendo ombre e cleanup; audio all'arrivo.
Nuovo test di continuità fallito sulla 2.03 e passato dopo la correzione. **255 EditMode e
3 runtime K5 passati**; campionamento del volo conferma assenza della pausa intermedia.

**Ripresa del 23/09:** G4 verificato nei commit esistenti; K3 completato con UIEffect 5.9.0
e UIParticle 4.11.4 già presente. Compilazione senza errori e **228/228 test EditMode passati**.
**K1 completato (2.00):** tempi comuni, pressione/rilascio dei pulsanti, pannelli e pagine,
contatori dei risultati, contatori valuta pronti e volo premi riutilizzabile. Builder
`Tools/UIV2/Apply Motion Kit` sui prefab e completamento automatico una volta per scena.
**Verifiche:** 229 test EditMode passati; test runtime esplicito passato separatamente in Play Mode.
**K2 completato (2.01):** tema condiviso, pulsanti oro/blu/piatto/icona, due pannelli,
scala Poppins 40/32/24/20; oro C9 anche su CONTINUA e RIVINCITA. Builder
`Tools/UIV2/Apply Design System`, completamento a runtime senza risalvare scene.
**Verifiche K2:** 237 test EditMode passati e test runtime K1 passato separatamente;
controllo visivo in Play Mode di avvio, accesso, impostazioni, Home, Collezione, Profilo e risultati.
Regole e compatibilità in `docs/ui/design-system.md`.
**K4 consegnato (2.02, 23/09):** riflessi sui pulsanti oro e sulle carte selezionate/toccate,
bagliori dalla silhouette reale, dissolvenza/bruciatura della matta, shader olografico opt-in,
fondo Home con luce lenta e sfocatura GPU senza lettura dei pixel sulla CPU.
Builder `Tools/UIV2/Apply Shader Kit` eseguito; completamento automatico a runtime.
**Verifiche:** 249 test EditMode passati, zero fallimenti; 3 test runtime K4 passati separatamente.
Il runtime K1 è rimasto esplicito e non rieseguito in questo giro. Avvio, Home, tavolo e sfocatura
Impostazioni controllati in Play Mode; corretto e ricontrollato il capovolgimento Direct3D.
**Da collegare più avanti:** olografico ai mazzi/oggetti realmente rari con F2/F5/J5 (oggi manca
la rarità nei dati). **Da provare in G2/I7:** Android, costo GPU e orientamento sugli altri backend,
giro visivo completo matta/accusi/risultati e bordi delle liste mascherate.
Dettagli K4 in `docs/ui/k4-shaders-plan.md`.
**K5 consegnato (2.03, 23/09):** panno con trama deterministica e luce centrale, ombre morbide
condivise sulle carte e sulle copie animate, risposta della mano con piccolo assestamento,
discesa più decisa e posa finale della giocata nei medesimi 0,35 secondi (audio all'impatto).
Corretti conflitti hover/selezione/matta, ripristino su interruzione/riuso e rilascio del feltro
rigenerato. Integrazione nei componenti esistenti: nessuna scena/prefab modificata a mano.
**Verifiche K5:** 254 EditMode passati, zero fallimenti; 3 runtime K5 e 2 runtime carte K4
passati separatamente. Tavolo e presa completa controllati in Play Mode; zero ombre isolate,
zero copie residue e console senza errori. Android e multiplayer reale restano in G2/I7.
Dettagli e immagini in `docs/ui/k5-table-plan.md`. **Prossimo task: K6 — vibrazione e particelle.**
Il volo di premi reali attende E2/F1; nessuna
valuta o ricompensa finta aggiunta. Nessuna build Android eseguita in questo giro.

**Ultimo lavoro fatto (19/09):**
- G1 chiuso (1.93): tavolo provato su tablet 3:4, 20:9 e iPhone; mano più vicina al fondo sui telefoni lunghi.
- I1 chiuso (1.94): dorso delle carte al tavolo. Vedi Sprint 6.
- I3 chiuso (1.95): emoticon rapide al tavolo al posto del pannello.
- I2 chiuso (1.96): icone centrate sulla faccia dei riquadri blu e oro.
- C9 chiuso (1.97): scritte GIOCA e GIOCA COME OSPITE con il contorno bruno del mockup.
- G6 chiuso (1.98): tavolo sfocato dietro il fine smazzata.

**G4 verificato il 23/09:** il lavoro dopo il checkpoint `b21dc11` è già salvato nei commit
`b8d7b49` e `8743964` (415 file modificati rispetto al checkpoint), sul branch
`codex/home-v2-training`. Il precedente avviso di lavoro non committato era rimasto obsoleto.

**Come riprendere (per chiunque, anche senza Claude):**
- Questo file è la lista viva. `UI_INTEGRATION_ROADMAP.md` è il resoconto dettagliato delle consegne.
- La UI si costruisce con gli script del menu Unity `Tools/UIV2/...` (cartella `Assets/Editor`):
  non modificare a mano le scene o i prefab che quegli script generano, rilancia lo script.
- Versione in `ProjectSettings > bundleVersion`: +0,01 a ogni giro di modifiche.
- Test: `Window > General > Test Runner > EditMode`, 255 passati; 3 runtime K5 passati nella 2.04. Ultima verifica dei 2 runtime carte K4 nella 2.03; runtime K1 e blur K4 non rieseguiti.
- Mockup in `Assets/Mockup/`. Da ora sono un punto di partenza, non un vincolo (vedi I8).

**Ordine deciso il 19/09 (prima la qualità, poi i sistemi):**
1. **G4** ☑ commit del lavoro precedente verificati il 23/09 (`b8d7b49`, `8743964`)
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
| B5 | Impostazioni in partita (mockup 26): animazioni veloci, suggerimenti mosse, musica, effetti, abbandona partita. Toglie anche l'ESCI in più dai risultati | ☑ 1.81 (musica ed effetti salvano solo la scelta: nel gioco non ci sono ancora suoni; l'abbandono conta come sconfitta dal 2.12) |
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
| C13 | Barra in basso sollevata dal fondo, icone piccole | ☑ 1.92 `BottomNavSafeAreaBleed`: sfondo fino al bordo e contenuto più in basso sui telefoni con barra di sistema; icone 62→72, linguetta oro 238x117→262x128 (9-slice); 2.12: 254x119, riempimento oro 226x82 come il mockup |

## Sprint 4 — Profilo, social, progressione

| # | Cosa | Stato |
|---|------|-------|
| D1 | Profilo: dati reali (livello, XP, statistiche) | ☑ 2.12 livello nell'esagono in Home, nome ospite "Ospite XXXX". 2.13: ospiti senza XP né ricompense (invito a registrarsi al posto della barra), ID accorciato a "#XXXXXXXX" e nascosto agli ospiti. Aperto: l'ospite è un account nuovo a ogni avvio (nome e statistiche cloud non restano) |
| D2 | Scelta di icona e banner nel profilo, visibili al tavolo | ☐ |
| D3 | Posta (mockup 06): messaggi e ricompense dal server | ☐ |
| D4 | Amici (mockup 07): lista, richieste, invito in stanza (attiva "Amici" in sala d'attesa) | ☐ |
| D5 | Classifica, tornei, eventi | ⏸ futuro |
| E1 | XP a fine partita e livelli (compreso l'allenamento, oggi non assegna XP) | ☑ 2.12 curva unica `PlayerXp` 100+20×(L−1) per locale, cloud e Home; 40 vittoria / 20 sconfitta, +2 scopa, +5 accuso (max +20), metà in allenamento; riga "+XP" nei risultati con riempimento, lampo e scoppio al level up |
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
| I5 | Impostazioni → **Grafica ridotta**: spegne particelle, bagliori pulsanti, sfocature, sfondo animato, shader e accorcia le animazioni (per telefoni lenti e batteria). Va fatta insieme a K1-K6, così ogni effetto nuovo nasce già spegnibile | ☑ 2.06 — unica scelta Home/tavolo con migrazione di Animazioni veloci; spegnimento e ripristino anche durante l'uso, inclusi effetti precedenti a K4/K6. Indicatori utili statici, volo K5 continuo, vibrazione indipendente. 271 EditMode passati, runtime I5/K5/K6 verificati. Misure su telefono ancora da fare. `docs/ui/i5-reduced-graphics.md` |
| I6 | Sfondo animato nella Home | ☑ 2.02 — shader con gradiente/luci lenti sul BackgroundLayer, insieme al pulviscolo C6; nessun video o asset aggiuntivo |
| I7 | **Giro di prova completo dell'app** (avvio → accesso → Home → partita → risultati → rivincita/Home, e online): trovare tutto ciò che è lento, macchinoso, poco chiaro o grezzo, e correggerlo. Obiettivo: velocità, fluidità e chiarezza al massimo | ◐ 2.12: chiusi numero livello in Home, targa oro barra in basso, vecchi canvas MainMenu (spenti; restano Canvas_Login e ModePanelRoot perché servono), avatar al tavolo, banner bot 2/4 e bagliore Emoji/ACCUSO sul bordo (panno 60 px troppo in basso), intestazione tavolo, riga XP nei risultati, etichetta GameFormat (ok). 2.11: Cirulla a tavolo vuoto e doppio accuso MP ☑. 2.13: "Musica" nelle Impostazioni della Home ☑, etichetta v1.83 → versione reale ☑; 2.14: sfondo Home animato ☑. Ancora aperti: monete e gemme (J4), centro Home vuoto (lo fa l'utente), resync a metà distribuzione che non ferma le coroutine, ritardo del pugno sul client in ritardo, riprova Cirulla a tavolo vuoto |
| I8 | Direzione: i mockup servivano a dare vita all'app, da ora le migliorie le decidiamo noi. UI pulita, niente dettagli inutili | regola |
| I9 | Icone nuove senza "quadrato + icona dentro"; navigazione e chiusure coerenti | Brief aggiornato il 23/09: `docs/ui/asset-refresh-brief.md`. Confermati «Indietro» nelle schermate e «Chiudi» nei popup al posto delle X di chiusura. Proposta: icone illustrate libere, meno cornici e materiali coerenti. Asset e integrazione ancora da fare. 2.12: set icone v2 integrato (`Tools/UIV2/Apply Icon Set v2`), emoticon animate; resta la rimozione dei riquadri (non approvata) |

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
| K1 | **Kit di movimento** unico, applicato ovunque da un builder: pulsanti che si schiacciano e rimbalzano al tocco, pannelli che entrano ed escono con un piccolo rimbalzo, numeri che contano, ricompense che volano verso il contatore, passaggi morbidi tra pagine e scene | ☑ 2.00 — `Tools/UIV2/Apply Motion Kit` + installazione una volta per scena. Contatori nei risultati e componenti pronti per valute/premi (dati reali con E2/F1). 229 test EditMode + test runtime esplicito passati; dettagli nella roadmap |
| K2 | **Sistema di design fissato**: 3-4 tipi di pulsante (primario oro, secondario blu, piatto, icona), 2 tipi di pannello, scala dei testi (titolo, sottotitolo, testo, didascalia), palette. Poi ogni schermata si riallinea a quello. Comprende lo stile oro di C9 su tutti i pulsanti oro (CONTINUA, RIVINCITA...) | ☑ 2.01 — `Tools/UIV2/Apply Design System`, tema condiviso e completamento scene a runtime. Poppins 40/32/24/20 con eccezioni calibrate; materiali oro condivisi. 237 test EditMode + runtime K1 passati; regole in `docs/ui/design-system.md` |
| K3 | Librerie gratuite MIT: **UIEffect** (riflessi, dissolvenze, gradienti, ombre sulla UI) e **UIParticle** (particelle vere dentro i pannelli) | ☑ 1.99 — UIEffect 5.9.0 installato tramite UPM, tag fissato; UIParticle 4.11.4 già presente, hash nel lockfile. Compilazione senza errori, 228/228 test EditMode |
| K4 | **Shader nostri**: riflesso di luce che passa su pulsanti, carte e oggetti rari; bagliore calcolato sulla forma esatta del pulsante (sostituisce i bagliori allungati, mai più storti); dissolvenza o bruciatura per carte speciali (accuso, matta); olografico per i mazzi rari; sfondo animato della Home (I6); sfocatura su scheda video, più veloce | ☑ 2.02 — Shader Kit, silhouette alpha, matta, Home, blur GPU. 249 EditMode + 3 runtime K4 passati; avvio/Home/tavolo/blur controllati. Olografico e riflessi per oggetti rari pronti opt-in, collegamento a dati reali con F2/F5/J5. Prestazioni Android da misurare in G2 |
| K5 | **Tavolo più ricco**: panno con texture e luce al centro, ombre sotto le carte, mano che reagisce al tocco, carte giocate con più peso | ☑ 2.04 — Panno e ombre K5; ripristinato volo continuo dopo gli scatti della 2.03. 255 EditMode + 3 runtime K5; dettagli in `docs/ui/k5-table-plan.md` |
| K6 | **Risposta a ogni tocco**: vibrazione (con interruttore nelle Impostazioni) e particelle sui momenti forti (scopa, accuso, vittoria, premi) | ☑ 2.05 — toggle Home/tavolo, aptica locale, pool finito; 261 EditMode + runtime K5/K6 passati. Prova aptica/build su telefono pendente; dettagli in `docs/ui/k6-feedback-plan.md` |
| K7 | **Flusso veloce**: dall'apertura alla partita in 2 tocchi (l'ospite già entrato salta la schermata iniziale), roulette del mazziere più breve o saltabile, risultati che proseguono da soli dopo qualche secondo | ☑ 2.07 (23/09): ospite diretto in Home, roulette a un giro, conto di 8 s solo host/offline, rivincita manuale. Manca la prova con due client reali. `docs/ui/k7-flow-progress.md` |
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
| G4 | Commit del lavoro fatto dopo il checkpoint b21dc11 | ☑ verificato il 23/09: `b8d7b49` e `8743964` |
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

**Aggiornamento 23/09 — revisione stile UI:** lista di produzione e specifiche in
[`docs/ui/asset-refresh-brief.md`](docs/ui/asset-refresh-brief.md).
Confermato: «Indietro» nelle schermate e «Chiudi» nei popup, nello stile del gioco.
Le altre scelte del brief sono proposte di brainstorming; nessun nuovo asset ancora prodotto.

| Priorità | Da preparare | Direzione |
|---|---|---|
| P0 | Campione coordinato: navigazione + Premio + Classifica + Posta | Validare insieme scala, materiali e leggibilità prima di produrre il set completo |
| P1 | Chevron opzionale per «Indietro» | Panna, morbido, senza disco di legno, cornice o riquadro; testo separato |
| P1 | Scrigno, coppa, busta | Silhouette autonome senza quadrato blu; oro e blu coerenti, etichetta sotto |
| P1 | Ingranaggio, audio, aiuto, uscita, rimozione | Un'unica famiglia semplice; uscita/rimozione distinte dalla chiusura |
| P2 | Carte, negozio, profilo/amici | Riallineare peso e materiali alle icone approvate, riusando ciò che funziona |
| P2 | Indicatore di selezione, badge notifica, divisore | Piccoli supporti discreti; evitare una nuova cornice intorno a ogni elemento |

**Non serve un PNG «Chiudi»:** testo localizzabile e componente esistente da adattare.
Rimozione dei riquadri, area cliccabile, stati premuto/disabilitato e disposizione dei
comandi sono lavoro UI, non illustrazioni da commissionare. Dettagli e criteri nel brief.

### Consegne precedenti

Consegnati il 17/09: Poppins Regular/Medium/SemiBold, bagliore morbido cerchio e rettangolo, velo "Sfocatura sfondo" (la sfocatura vera si fa via codice sotto al velo, fatta nella 1.81).

Consegnati il 17/09 anche i 31 file audio (musica + effetti), collegati nella 1.82.

Consegnato il 17/09: `ic_arrow_left` (pulsante indietro tondo, usato in 23 e 24).

Consegnato il 18/09: pacchetto legale/auth (`51_Legal_Auth_Package`) con Termini, Privacy, template
email PlayFab, pagine web e riferimenti backend. Termini e Privacy sono in app dalla 1.85.

Ancora da fare, ma NON sono asset da disegnare: vedi "Legale e account" qui sotto.
