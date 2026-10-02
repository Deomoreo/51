# 51 — Backlog dello sprint

Lista viva di tutto quello che resta da fare. Ogni nuova idea si aggiunge qui con un codice.
`UI_INTEGRATION_ROADMAP.md` resta il resoconto dettagliato delle consegne.

Stato: ☐ da fare · ◐ in corso · ☑ fatto · ⏸ rimandato · ❓ da decidere

Regola: bug e rifiniture di cose esistenti si fanno in ordine. Schermate e sistemi nuovi partono solo dopo il tuo via, uno alla volta.

---

## ▶ PUNTO DI RIPRESA — 02/10, versione 2.63

**Giro lungo sui mockup (01/10, "continuiamo coi mockup, non fermarti"):** si va avanti fase per fase; ogni versione qui sotto è una fase o un pezzo di fase. Le scelte che ho preso da solo sono elencate in ogni versione sotto "Scelte mie, da confermare".

**Dove siamo (2.63):** Fase 15 (Progressione) fatta con le tue quattro scelte del 02/10: LivelloSu, Forziere + ForziereAperto, Trofei, Classifica (sotto, alla 2.63). Resta solo la Fase 10 (Pulizia): prima ti preparo la lista file per file e tu la confermi. Da fare tu per la Classifica: ricaricare `51.carica.js` e creare la classifica settimanale nel Game Manager (sotto).

**2.63:** Fase 15, Progressione ("continuiamo coi mockup, non fermarti"; tue scelte del 02/10: LivelloSu solo cose vere, Forziere solo animazione, Classifica su PlayFab, Trofei dalle statistiche). Build pulito (0 errori); test EditMode 412 totali: 405 ok, 0 falliti, 7 saltati (Explicit); prove del CloudScript tutte passate. Provata nel Simulator (iPhone 12). Costruita da **Tools/UI51/Build Fase 15 (Progressione)** (`UI51ProgressBuilder.cs`, scena MainMenu). Non committata.
- ☑ **LivelloSu** (`UI51LevelUpView`): al ritorno in Home dopo una o più partite che fanno salire di livello (da `ProfileService.NoteLevelUp`, conta dal livello di partenza). Livello nuovo, titolo nuovo solo se cambia, sblocchi veri (oggi solo il banner Porpora al 10; la sezione sparisce se non c'è niente), barra verso il livello dopo. Niente monete. Provato 9→10 (Porpora, Apprendista → Esperto) e 13→14.
- ☑ **Forziere + ForziereAperto** (`UI51ChestView`): anima il forziere che il server ha già aperto (premio giornaliero giorno 5 verde, giorno 7 viola, e posta). Chiuso che oscilla, "TOCCA PER APRIRE", poi monete e gemme vere. Canvas suo (750) sopra Premi e Posta.
- ☑ **Trofei** (`Trophies` in Core, `UI51TrophiesView`, `UI51TrophySummary`): 15 trofei fissi calcolati dalle statistiche del server (partite, vittorie, scope, livello), nessun dato nuovo salvato. Sezione TROFEI nel Profilo (ultimi 4 ottenuti, 2 in corso, "N / 15 · Vedi tutti"), pagina con categorie, griglia a 3 e dettaglio dal basso.
- ☑ **Classifica** (`LeaderboardService`, `UI51RankingView`): dal pulsante Classifica della Home. Schede Settimana / Amici / Sempre, podio, posizioni fino alla 50, la tua riga fissata in basso con quanto manca alla top 10. Gli ospiti vedono "Registrati per entrare in classifica". Nuova statistica `XPSettimana` scritta da `premioPartita` (51.js + test.js).
- ☑ **Non fatti, per tua scelta (02/10):** `NuovoOggetto` (Porpora lo mostra già LivelloSu), `NuoviFrammenti` e le 3 carte di rarità del forziere (niente frammenti), premi per fascia della Classifica (resta competitiva e visiva).
- ☑ **Scelte confermate da te (02/10):** Classifica sugli XP; carte del forziere che si girano da sole (sequenza rapida); "Ottenuto" al posto della data; categoria "Scope".
- ☐ **Debiti tecnici (02/10):**
  - Avatar degli altri in Classifica: oggi dall'id PlayFab (sempre lo stesso per lo stesso giocatore), solo segnaposto. Quando salveremo `SelectedAvatarId` di ogni giocatore, la Classifica deve usare quello.
  - Trofei: se un giorno salviamo `UnlockedAt`, mostrare la data al posto di "Ottenuto".
  - Builder: oggi rilanciare la Fase 4 (Profilo) cancella la sezione TROFEI, quindi l'ordine è **Fase 4 → poi Fase 15**. Da sistemare: la Fase 4 non deve distruggere quello che aggiunge la Fase 15.
- **Da fare tu:**
  1. `node Server/CloudScript/carica.js` e caricare `51.carica.js` (ora contiene XPSettimana).
  2. ☑ PlayFab Game Manager → Leaderboards (Legacy): `XPSettimana` con azzeramento Weekly e aggregazione Last, controllato il 02/10 (Last è giusto: il server scrive già il totale della settimana). Va premuto Save.
  3. Facoltativo: Client Profile Options → abilitare le statistiche, così le righe della Classifica mostrano il livello.
- **ASSET MANCANTI DA CREARE:** nessuno (tutte le immagini dei mockup della Fase 15 ci sono; quelle non usate servono solo alle parti escluse).

**Prima (2.62):** Photon e CloudScript chiusi per bene (le tue risposte sulla 2.61 e "risolvi tutto tu"): nomi segreti dei webhook, nessun webhook rifiuta più, la sospensione la ferma il telefono con uno stato fresco prima di ogni partita online, statistiche scritte solo dal server, Photon riautenticato dopo "Accedi". Da fare tu, in ordine: caricare `51.carica.js` (non più `51.js`) e subito dopo i nuovi nomi nel pannello Photon (sotto, alla 2.62); sostituiscono i passi del pannello Photon delle versioni 2.56-2.61. 02/10, fatto da te e verificato dall'Editor: revisione 13 del CloudScript attiva (quella nuova), PathCreate e PathClose funzionano, client anonimi rifiutati. Ancora da fare: spegnere "Allow client to post player statistics" su PlayFab (il telefono riesce ancora a scrivere le statistiche). PathJoin si verifica solo con due giocatori. Prossimo: la prova vera su due telefoni.

**Prima:** alla 2.61 (solo server) tre controlli sulle monete della vittoria per abbandono (partita esistente, roster, una volta sola) e pulizia degli Shared Group orfani. Alla 2.60 le tue risposte sulla 2.59: id di partita generato dal server a ogni stanza (record con il roster), permesso di rientro legato alla partita, avversario dal record anche se esce mentre sei fuori (sotto; da fare tu: ricaricare 51.js e aggiungere PathJoin `RoomJoined` e PathClose `RoomClosed`). Alla 2.59 le tue due correzioni sulla 2.58: stato completo al rientro (le mosse in viaggio si rigiocano, abbandoni avvenuti mentre eri fuori) e permesso di rientro da sospeso che nasce all'ingresso (sotto; da fare tu: ricaricare 51.js). Alla 2.58 le tue risposte del 02/10 sulla 2.57: il conto dei turni fermi è del giocatore e resta se cambia il master, arbitro di riserva per il turno del master, numero di turno sulle mosse, chi è tolto per inattività perde per abbandono anche se resta nella stanza, rientro da sospeso solo nella propria stanza (sotto; da fare tu: ricaricare 51.js e aggiungere PathLeave). Alla 2.57 le tue risposte del 02/10 sulla 2.56: il master gioca la carta di chi è fermo dopo 30 + 10 s e al terzo turno fermo il posto passa al bot, rientro da sospeso solo nella propria partita, nomi dei bloccati ricordati (sotto). Alla 2.56 le tre cose rimandate che hai approvato il 02/10: controllo della sospensione sul server, tempo del turno con uscita per inattività, Giocatori bloccati. Alla 2.55 le tue risposte del 01/10 sulla moderazione. Alla 2.54: fatte le fasi 9, 13, 12, 14 (il timer del turno è arrivato alla 2.56) e la 11 (tranne Lingua e Notifiche, più avanti per tua scelta). Restano la Fase 15 (Progressione) e la Fase 10 (Pulizia), saltate come concordato, e i rimandati elencati versione per versione: ognuno aspetta il tuo sì o no. Tutto dalla 2.38 in poi è **non committato**.

**2.62:** le tue risposte del 02/10 sulla 2.61 e "controlla bene tutta questa situazione con Photon e CloudScript, risolvi tutto tu". Build pulito (0 errori); test EditMode 409 totali: 402 ok, 0 falliti, 7 saltati (Explicit); prove del CloudScript tutte passate. Tre revisioni del codice (workflow, ogni difetto verificato da un secondo agente che prova a smontarlo): tutti i difetti confermati corretti (sotto). Non committata.
- ☑ **Webhook chiamabili dal telefono (controllato nel codice: prima lo erano):** ogni funzione del CloudScript si può chiamare da un telefono, anche quelle delle revisioni vecchie. Ora i webhook di Photon hanno nomi con un segreto (`RoomCreated_<segreto>`, `RoomJoined_<segreto>`) che sta solo in `Server/CloudScript/segreto.txt` e nel pannello Photon; le funzioni per il telefono rifiutano le chiamate fatte per conto di un altro giocatore o senza giocatore. Il repository è pubblico: `segreto.txt` e `51.carica.js` sono fuori da git. `node Server/CloudScript/carica.js` prepara il file da caricare e stampa i nomi per il pannello (`--nuovo` cambia il segreto).
- ☑ **Nessun webhook rifiuta più:** un rifiuto mostra al telefono un errore che può contenere l'URL del webhook (chiave Photon e segreto). Restano tre percorsi: PathCreate, PathJoin, PathClose; PathBeforeJoin e PathLeave vuoti. Tolti il controllo all'ingresso e il permesso di rientro da sospeso, che non servono più.
- ☑ **Sospensione:** la ferma il telefono, con uno stato fresco dal server prima di ogni partita online (partita veloce, crea stanza, entra con codice; al massimo una chiamata ogni 10 s). Un'app modificata entra lo stesso ma siede come gli altri (si può segnalare; se abbandona, chi vince è pagato) e per le partite online il server non le dà niente; allenamento e uscite contano come per tutti.
- ☑ **Record della partita legato ad app, versione e regione di Photon:** lo stesso codice di stanza in un'altra app (AppId), versione o regione non tocca il record (niente posti, niente cancellazione). I nomi dei dati delle partite contengono un pezzo del segreto: le revisioni vecchie scrivono altrove.
- ☑ **Statistiche solo dal server:** partite, vittorie, XP, livello e scope li scrive `premioPartita` (massimo 60 risultati al giorno); il telefono mostra quello che risponde il server. Uscita a metà = persa, senza XP né monete.
- ☑ **Nomi nuovi per sospensioni, silenzio delle emoticon, segnalazioni e storico:** le revisioni vecchie scrivevano i nomi vecchi. Effetto: sospensioni e segnalazioni di prima ripartono da zero.
- ☑ **Photon con l'account giusto dopo "Accedi":** Photon si scollega subito, senza credenziali né biglietto vecchi, e si ricollega con il token del nuovo account (il nostro sistema di riconnessione riprova finché resti dentro, attese crescenti fino a 30 s). "Accedi" aspetta se l'accesso da ospite dell'avvio è ancora in viaggio. Il rientro dopo un riavvio vale solo per lo stesso account.
- Correzioni dalle revisioni (in breve):
  - **(serio)** Un rifiuto per sospensione poteva mostrare l'URL del webhook: tolti tutti i rifiuti.
  - **(serio)** Una stanza con lo stesso codice in un'altra versione o regione poteva iscrivere posti o cancellare il record di una partita vera (e da lì segnalazioni finte): record legato a versione e regione.
  - **(serio)** Le revisioni vecchie dello script restano chiamabili: dati delle partite e della moderazione con nomi nuovi.
  - **(serio)** Dopo "Accedi" Photon poteva restare (o tornare, con la riconnessione della Home) sull'account dell'avvio: ora no, provato dal vivo.
  - Server: pulizia dei record orfani solo dopo 2 ore (mai una partita in corso); vittoria per abbandono contro un sospeso pagata; una segnalazione non si blocca se la notifica a uno fallisce; vittoria per abbandono non valida senza XP.
  - Telefono: lo stato fresco della sospensione vale solo per l'account che l'ha letto; Annulla, una partenza nuova o l'allenamento scartano una creazione o un ingresso ancora in attesa del controllo; un solo controllo per stanza (prima due di fila, e una sospensione letta dal secondo lasciava la schermata bloccata); RIPROVA salta l'attesa tra un tentativo e l'altro; la riconnessione non taglia una connessione già partita con l'account giusto; il velo di caricamento si toglie anche se la Home non c'è più.
- Provato dal vivo (Editor): cambio account (Photon torna sullo stesso PlayFabId con autenticazione Custom, il biglietto vecchio non rientra); riconnessione della Home senza credenziali; controllo fresco della sospensione (la prima volta chiede al server, una seconda entro 10 s parte subito); partenza annullata che non parte; RIPROVA che si ricollega in 1,2 s invece di aspettare 8 s. Non provato: i webhook veri su PlayFab/Photon (dopo il caricamento) e due telefoni.
- Da fare tu, in ordine, quando nessuno sta giocando:
  1. `node Server/CloudScript/carica.js`, poi su PlayFab Upload + Deploy di `Server/CloudScript/51.carica.js` (non più `51.js`).
  2. Subito dopo, pannello Photon, Webhooks: PathCreate e PathJoin con i nomi stampati da carica.js, PathClose `RoomClosed`, PathBeforeJoin e PathLeave vuoti, BaseUrl senza graffe, HasErrorInfo spento.
  3. Photon, Authentication: "Allow anonymous clients" spento.
  4. PlayFab, impostazioni delle API: "Allow client to post player statistics" spento.
  5. Facoltativo: una API Access Policy di PlayFab che nega ai client le API degli Shared Group.
  6. Tieni `segreto.txt` al sicuro e non committarlo (è già escluso da git).
- Limiti accettati: una vittoria normale la dichiara il telefono (tetto di monete e 60 risultati al giorno); l'abbandono lo dichiara il telefono; silenzio delle emoticon e blocco della sospensione li applica il telefono; due richieste nello stesso istante; le revisioni vecchie pagano anche un sospeso, dentro il tetto giornaliero; cosmetici e nome visibile scritti dal telefono.
- Scelte confermate da te il 02/10 (tutte sì):
  - I webhook non rifiutano mai: un sospeso con un'app modificata entra, ma online non guadagna niente. Limite accettato in modo esplicito: la sospensione non è un blocco multiplayer inviolabile (l'app normale la rispetta, il server nega premi e statistiche).
  - Controllo fresco della sospensione al massimo una volta ogni 10 s.
  - Record della partita legato ad app (AppId), versione e regione di Photon, più il codice della stanza e l'id di partita del server (AppId aggiunto dopo la tua risposta, con una prova).
  - Nomi nuovi per i dati della moderazione: sospensioni e segnalazioni di prima ripartono da zero. Va bene ora che il gioco è in sviluppo; dopo la pubblicazione niente azzeramenti di questo tipo (servirà una migrazione).
  - Al massimo 60 risultati contati per account e per giorno del server (ora italiana dall'orologio di PlayFab, mai quello del telefono): scritto anche nel codice.
  - Dopo "Accedi" il nostro sistema di riconnessione (AuthBootstrapper, non Photon) riprova da solo, aspettando sempre di più tra un tentativo e l'altro (fino a 30 s), finché resti dentro. Photon offre solo `Reconnect`/`ReconnectAndRejoin`: tentativi e attese sono nostri.
  - "Accedi" durante l'accesso da ospite dell'avvio: messaggio "Connessione al server in corso: riprova tra un attimo."; finito quell'accesso, il pulsante funziona di nuovo.
- Rimandato (confermato da te il 02/10):
  - **Vittorie normali verificate dal server e mosse validate dal server:** un solo lavoro, con il futuro server autorevole o i plugin, non due sistemi separati.
  - **Segnale immediato al tocco:** solo dopo la prova vera su due telefoni (Wi-Fi↔Wi-Fi, poi Wi-Fi↔4G/5G), e solo se si sente il ritardo.
- ASSET MANCANTI DA CREARE: nessuno.

**2.61:** le tue risposte del 02/10 sulla 2.60 (architettura invariata: tre controlli sulla ricompensa per abbandono e pulizia degli Shared Group orfani). Solo server (`51.js`) e prove; nessun file C# toccato. Build pulito (0 errori); test EditMode 409 totali: 402 ok, 0 falliti, 7 saltati (Explicit); prove del CloudScript tutte passate. Revisione del codice (agente deep-reviewer): tre difetti seri e tre minori, corretti (sotto); due limiti accettati. Non committata.
- ☑ **Monete per abbandono, tre controlli sul record della partita:** la partita deve esistere; chi chiede deve sedere nel roster; al posto indicato deve esserci un altro giocatore (che diventa l'avversario). Ogni partita paga una volta sola per giocatore (`pagato_<id>` nel record). Se un controllo fallisce: vittoria valida, 0 monete (`abbandonoNonValido`). I tetti di prima (3 al giorno, uno per avversario) restano.
- ☑ **Pulizia degli Shared Group orfani:** chi crea una stanza tiene un indice delle ultime 20; i record più vecchi di 2 ore li cancella il server alla sua prossima stanza creata o al prossimo avvio dell'app (al massimo 5 per volta), solo se il record è ancora di quella partita (stesso id). RoomClosed chiamato da un telefono non cancella niente.
- Correzioni dalla revisione:
  - **(serio)** Un telefono poteva scrivere sé stesso al posto di un altro nel roster: ora un posto preso non cambia più e al tavolo si iscrivono al massimo 4.
  - **(serio)** Un telefono poteva rifare il record di una partita viva (con un finto "stanza creata") cancellando il roster di tutti: un record con meno di 2 ore non si sostituisce più.
  - **(serio)** Un telefono poteva creare da sé un gruppo con un roster inventato (le API Client di PlayFab lo permettono): ora vale solo un gruppo senza membri, cioè creato dal server.
  - Indice oltre 20 stanze: le più vecchie ora vengono pulite invece di andare perse; una cancellazione fallita si riprova la volta dopo; alla creazione della stanza al massimo 2 pulizie (per non rallentare l'ingresso), all'avvio 5.
  - Limiti accettati: un telefono modificato può ancora iscriversi a un posto libero di una stanza di cui conosce il codice (per chiuderlo serve un segnale che venga solo da Photon), e due richieste nello stesso istante possono pagare due volte (PlayFab non ha scritture condizionate). In ogni caso una vittoria normale (non per abbandono) il server non la controlla affatto: il tetto giornaliero resta il limite vero.
- Da fare tu: ricaricare `Server/CloudScript/51.js`. Pannello Photon: PathCreate `RoomCreated`, PathBeforeJoin `RoomBeforeJoin`, PathJoin `RoomJoined` (ora indispensabile per le monete dell'abbandono), PathLeave `RoomLeft`, PathClose `RoomClosed`; BaseUrl senza graffe.
- Scelte mie, da confermare:
  - Il gruppo resta chiamato col codice ("stanza_<codice>"), ma ogni operazione controlla l'id della partita e un codice riusato ricrea il gruppo da zero: in pratica vale come chiave l'id. Chiamarlo con l'id vorrebbe dire far sapere l'id a ogni webhook, ma Photon passa solo il codice.
  - Abbandono non valido: sul telefono resta "+0 monete" senza spiegazione.
  - Pulizia solo quando il creatore torna (nuova stanza o avvio): chi non torna mai lascia al massimo le sue ultime 20 stanze.
- Rimandato, da confermare uno per uno:
  - **Mosse validate dal server:** serve Photon Enterprise o un server di gioco.
  - **Segnale immediato al tocco:** dopo la prova vera su due telefoni (Wi-Fi↔Wi-Fi, poi Wi-Fi↔4G/5G).
- ASSET MANCANTI DA CREARE: nessuno.

**2.60:** le tue risposte del 02/10 sulla 2.59. Build pulito (0 errori); test EditMode 409 totali: 402 ok, 0 falliti, 7 saltati (Explicit); prove del CloudScript tutte passate. Revisione del codice (agente deep-reviewer): un difetto serio, uno medio e cinque minori, tutti corretti (sotto). Non committata.
- ☑ **Id di partita generato dal server:** a ogni stanza creata (PathCreate) il server genera un id nuovo e tiene il record della partita in uno Shared Group di PlayFab "stanza_<codice>": id, codice, ora di creazione e chi siede a ogni numero Photon (PathJoin). Una stanza nuova con lo stesso codice ha un id nuovo e il roster vecchio sparisce. Quando Photon chiude la stanza (PathClose) il record si cancella.
- ☑ **Permesso di rientro legato alla partita, non al codice:** scade a max(ingresso + 2 h, caduta + 70 s) e vale solo per quell'id: un codice riusato non fa entrare un sospeso, anche con RoomLeft guasto. Le stanze senza record (create prima di questo script) restano legate al codice, come prima.
- ☑ **Avversario uscito mentre eri fuori:** per le monete l'id lo decide il server dal roster della partita, non il telefono; il telefono lo ricorda per gli XP (anche dopo un riavvio). "Sconosciuto" solo se manca il dato.
- ☑ **Controllo all'ingresso anche su PathJoin:** PathJoin registra chi siede e dà il permesso se BeforeJoin non l'ha già dato. Rifiutare resta compito di PathCreate e PathBeforeJoin: per Photon gli altri webhook non fermano l'ingresso. Il primo giocatore della partita veloce passava già da PathCreate, controllato dalla 2.56.
- Correzioni dalla revisione:
  - **(serio)** Questi webhook li può chiamare anche un telefono modificato: poteva costruirsi da solo il permesso di rientro o toccare i dati degli altri. Ora una chiamata da telefono agisce solo su chi chiama, il permesso si allunga solo a chi siede in quella partita e un sospeso non si iscrive al roster da solo.
  - Stanze senza record: il permesso torna legato al codice (prima un sospeso caduto lì non rientrava).
  - La chiusura in ritardo di una stanza vecchia non cancella il record di quella nuova con lo stesso codice.
  - Uscire da una stanza toglie solo il permesso di quella stanza.
  - Creazione del record con una chiamata in meno.
  - Telefono: gli id ricordati si aggiornano (codice riusato) e si salvano subito; nella rivincita l'avversario della partita prima non resta appeso.
- Provato: prove del CloudScript (permesso per id di partita, codice riusato, roster, chiusura, chiamate da telefono, premio con l'id dal record). Non provato dal vivo: i webhook veri su PlayFab/Photon.
- Da fare tu:
  1. Caricare di nuovo `Server/CloudScript/51.js`.
  2. Nel pannello Photon (Webhooks), oltre a quelli che hai: **PathJoin** = `RoomJoined` e **PathClose** = `RoomClosed`. PathClose è **necessario**: senza, ogni partita veloce lascia un record su PlayFab che non si può più cancellare. Restano PathCreate `RoomCreated`, PathBeforeJoin `RoomBeforeJoin`, PathLeave `RoomLeft`, e il BaseUrl senza graffe.
- Scelte mie, da confermare:
  - Il record della partita sta in uno Shared Group di PlayFab, uno per stanza aperta, cancellato alla chiusura.
  - Una chiamata ai webhook da telefono agisce solo su chi chiama (non so ancora con certezza chi risulta chiamante quando chiama Photon; il controllo funziona in entrambi i casi).
  - Il premio della vittoria per abbandono si fida del telefono per "ho vinto" e per quale stanza/posto indica (come prima): il limite di 3 al giorno resta la protezione.
- Rimandato, da confermare uno per uno:
  - **Mosse validate dal server:** i plugin Photon non ci sono sul Public Cloud (servono Enterprise Cloud o Photon Server in proprio): è una scelta di architettura da fare a parte.
  - **Feedback immediato al tocco:** da provare con due telefoni veri (uno Wi-Fi, uno 4G/5G); se il ritardo si sente, la carta reagisce subito e la giocata si completa alla conferma.
- ASSET MANCANTI DA CREARE: nessuno.

**2.59:** le tue risposte del 02/10 sulla 2.58 (le due correzioni: stato completo al rientro, permesso di rientro senza dipendere da RoomLeft). Build pulito (0 errori); test EditMode 409 totali: 402 ok, 0 falliti, 7 saltati (Explicit); prove del CloudScript tutte passate. Revisione del codice (agente deep-reviewer): due difetti seri e sei minori, tutti corretti (sotto). Non committata.
- ☑ **Stato completo al rientro, niente "si sistema alla mossa dopo":** lo stato che manda il master conteneva già tutto (mani, tavolo, prese, scope, accusi, punteggi della smazzata e della partita, mazziere, turno, regole; il numero di turno si ricava da lì); i posti (bot, scollegati, tolti per inattività, conto dei turni fermi) li legge dalla stanza. Il buco era la mossa in viaggio: ora ogni telefono ricorda le ultime mosse arrivate e, quando riceve lo stato, rigioca da solo quelle dal turno dello stato in poi. Vale per chi rientra e per ogni riallineamento.
- ☑ **Abbandoni avvenuti mentre eri scollegato:** al rientro, se un avversario è uscito davvero o è stato tolto per inattività mentre eri fuori, vale la vittoria per abbandono come se fossi stato presente (prima giocavi contro il bot). Il suo id non si sa più: per il premio conta come "avversario sconosciuto" (resta il tetto di 3 al giorno).
- ☑ **Permesso di rientro da sospeso senza dipendere da RoomLeft:** nasce già quando entri nella stanza (webhook di ingresso, che c'è già): vale solo per quella stanza, al massimo 2 ore, mai per crearne una. Se RoomLeft funziona lo porta ad almeno 70 s dalla caduta (mai più corto di quello dell'ingresso) e lo toglie se esci davvero.
- Correzioni dalla revisione:
  - **(serio)** Se arrivavano due stati di fila (succede quasi sempre al rientro) mentre una mossa era in animazione, la stessa mossa poteva entrare due volte e rovinare il tavolo. Ora un'animazione partita sullo stato vecchio non applica più la sua mossa: la rigioca lo stato nuovo, una volta sola.
  - **(serio)** Una mossa arrivata nell'istante subito dopo lo stato poteva ancora perdersi. Ora le mosse ricordate entrano subito, prima di quelle in arrivo.
  - Il controllo degli abbandoni parte solo a un rientro vero e mai a partita finita (prima poteva dare una vittoria per abbandono finta nella rivincita contro i bot).
  - Chi è stato tolto lui stesso per inattività mentre era fuori, rientrando esce come allora e non può vincere per abbandono.
  - Una mossa rigiocata non conta due volte nei turni fermi.
  - Per lo stesso turno si ricordano tutte le mosse arrivate, non solo la prima: una mossa sbagliata non nasconde quella giusta.
  - Le mosse di una partita finita non restano in memoria nella rivincita.
  - CloudScript: una seconda caduta nella stessa partita non dipende più da RoomLeft.
- Provato dal vivo (partita resa "client online" per finta in Editor): stato del master rimasto indietro rispetto a una mossa già arrivata, la mossa viene rigiocata da sola (turno 163 → 164); mossa arrivata "in anticipo" e scartata, poi arriva lo stato: rigiocata appena arriva lo stato (165 → 166); dopo le correzioni, due stati arrivati durante l'animazione della mossa: applicata una volta sola (164 → 165, 40 carte in tutto).
- Non provato dal vivo: vittoria per abbandono al rientro (serve una stanza vera), il permesso di rientro sul server vero.
- Da fare tu: caricare di nuovo `Server/CloudScript/51.js` (sostituisce quello della 2.58). Il pannello Photon resta com'è: PathCreate `RoomCreated`, PathBeforeJoin `RoomBeforeJoin`, PathLeave `RoomLeft` (consigliato, non più indispensabile), e il BaseUrl senza graffe.
- Scelte mie, da confermare:
  - Il permesso dall'ingresso dura 2 ore (la tua soglia); con RoomLeft funzionante scende a 70 s dalla caduta.
  - Al rientro, per il premio della vittoria per abbandono l'avversario uscito mentre eri fuori conta come sconosciuto.
  - Feedback al tocco: oggi la carta parte quando la mossa torna dal server; un segnale immediato al tocco lo valuto sui telefoni veri, in Editor il ritardo non si vede.
- Rimandato, da confermare uno per uno:
  - **Mosse validate dal server (plugin Photon o game server):** oggi il server inoltra e ordina, non controlla.
  - **Da verificare sul server vero:** che il webhook di ingresso scatti anche per la partita veloce (ingresso in una stanza a caso). Se non scatta, chi entra così non riceve il permesso, e da sempre il controllo della sospensione non lo ferma.
  - **Codice privato riusato entro 2 ore con RoomLeft guasto:** un sospeso potrebbe entrare in una stanza nuova con lo stesso codice della sua partita. Serve un id di partita che il server veda (con un plugin Photon).
- ASSET MANCANTI DA CREARE: nessuno.

**2.58:** le tue risposte del 02/10 sulla 2.57. Build pulito (0 errori); test EditMode 409 totali: 402 ok, 0 falliti, 7 saltati (Explicit); prove del CloudScript tutte passate. Revisione del codice (agente deep-reviewer): un difetto serio e quattro minori, corretti (sotto). Non committata.
- ☑ **Il conto dei turni fermi è del giocatore:** sta nella stanza, non sul telefono del master. Se il master cambia, il nuovo lo eredita (Francesco a 2/3 resta a 2/3). Lo aggiorna chi fa da arbitro di quel turno: +1 per ogni carta scelta allo scadere (dal suo telefono o forzata), 0 quando gioca da sé.
- ☑ **Arbitro di riserva per il turno del master:** quando tocca al master, a 40 s la carta la gioca un altro giocatore presente (quello con l'ActorNumber più basso dopo il master). Vale anche per i turni dei bot, che gioca il master: se il master è in secondo piano, il tavolo non si ferma comunque.
- ☑ **Numero di turno:** ogni mossa porta il numero del turno e passa dal server, così arriva a tutti nello stesso ordine. Per lo stesso turno vale solo la prima; quelle arrivate dopo si scartano senza chiedere lo stato al master. Se il master torna a 40,1 s e gioca mentre l'arbitro ne gioca una per lui, ne entra una sola, uguale per tutti.
  - Effetto: la propria carta parte quando torna dal server, un attimo dopo il tocco (il tempo di andata e ritorno).
- ☑ **Tolto per inattività = abbandono della partita, anche se resta nella stanza:** si applicano le regole di sempre: dopo almeno una smazzata finita vince subito chi resta; nella prima smazzata il bot la finisce e poi vince chi resta; premio con i limiti di sempre (3 al giorno, 1 per avversario). Un'app modificata non evita più la sconfitta restando collegata. Il posto resta del bot anche se rientra.
- ☑ **Il master non registra abbandoni degli altri:** decide solo cosa succede al tavolo (il posto passa al bot, la vittoria per abbandono). Per le sospensioni conta solo quello che il telefono di chi esce ha visto da sé (sotto).
- ☑ **Rientro da sospeso solo nella propria partita:** il permesso nasce solo quando Photon segnala che sei caduto da quella stanza e ti tiene il posto (webhook nuovo "PathLeave"), vale solo per quella stanza e per 70 s dalla caduta (il posto ne dura 60), molto meno delle 2 ore. Una stanza nuova con lo stesso codice non vale: lì non hai un posto. Se esci davvero il permesso sparisce.
- Correzioni dalla revisione:
  - **(serio)** Un master modificato poteva farti uscire per inattività "finta" e farti contare l'abbandono nelle sospensioni. Ora il tuo telefono conta per le sospensioni solo i tempi scaduti che ha visto davvero (la carta l'ha scelta lui allo scadere, o per lui erano passati almeno 30 s, secondo piano compreso). Se l'uscita arriva senza tre tempi così, perdi la partita ma non conta come abbandono.
  - Se il master cambia mentre una mossa è ancora in viaggio, lo stato mandato a chi rientra aspetta che la mossa sia arrivata (massimo 5 s), così nessuno riparte da uno stato vecchio.
  - Se il master è stato tolto per inattività, i turni dei bot li gioca l'arbitro di riserva subito, non dopo 40 s.
  - Il segno "tolto per inattività" è salvato posto per posto nella stanza (due uscite insieme non si cancellano a vicenda).
  - Finestra del rientro da sospeso ridotta a 70 s (era 2 minuti).
  - Fuori dalla revisione: chiudendo l'app durante una partita online partiva un tentativo di riconnessione (in Editor lasciava un oggetto "Connection" dopo lo stop). Ora la chiusura dell'app non lo avvia, in partita e in Home.
- Provato dal vivo (partita resa "online" per finta in Editor, questo telefono = master e arbitro): avversario fermo giocato a 40 s; tuo tempo scaduto contato (1), azzerato quando giochi tu; conto dell'avversario 1, 2, al terzo il posto tolto; una mossa con un numero di turno già passato scartata senza resync; una carta forzata per te prima dei 30 s conta per il tavolo (2/3) ma non per le sospensioni (resta 1).
- Non provato dal vivo: il giro vero a più telefoni (arbitro di riserva, cambio di master col conto che resta, vittoria per abbandono di chi resta nella stanza), il webhook PathLeave.
- Da fare tu:
  1. Caricare di nuovo `Server/CloudScript/51.js` (sostituisce quello della 2.57), prima di toccare Photon.
  2. Nel pannello Photon, Webhooks: aggiungere **PathLeave** = `RoomLeft` (gli altri restano: PathCreate `RoomCreated`, PathBeforeJoin `RoomBeforeJoin`, PathJoin vuoto). Senza PathLeave il gioco funziona lo stesso, ma un sospeso caduto non riesce a rientrare.
- Scelte mie, da confermare:
  - L'uscita per inattività conta per le sospensioni solo se il tuo telefono ha visto da sé i tre tempi scaduti; altrimenti è solo una partita persa.
  - Il permesso di rientro da sospeso dura 70 s dalla caduta, solo per quella stanza.
  - L'arbitro di riserva gioca anche i turni dei bot se il master è fermo (subito se il master è stato tolto).
  - Tutte le mosse passano dal server, con il piccolo ritardo sulla propria carta.
  - Se cadi dalla rete prima che Photon se ne accorga, un tuo turno può finire forzato e contare 1 al tavolo (non per le sospensioni).
- Rimandato, da confermare uno per uno:
  - **Uscite per inattività contate dal server:** quando avremo un controllo del tempo lato server (un plugin Photon o un server di gioco), anche quelle di un'app modificata entreranno nel conto delle sospensioni.
  - **Un'app modificata che diventa master** può ancora decidere cose al tavolo (è l'autorità della partita): si chiude solo con un server autorevole.
  - **Mossa in viaggio durante un rientro:** una mossa di un terzo telefono ancora in volo può mancare nello stato mandato a chi rientra; si sistema da sola alla mossa dopo (resync).
  - **Se il webhook RoomLeft non risponde**, un sospeso caduto non può rientrare nella sua partita (il webhook lascia passare tutto il resto).
- ASSET MANCANTI DA CREARE: nessuno.

**2.57:** le tue risposte del 02/10 sulla 2.56. Build pulito (0 errori); test EditMode 408 totali: 401 ok, 0 falliti, 7 saltati (Explicit); prove del CloudScript tutte passate. Revisione del codice (agente deep-reviewer): nessun difetto bloccante, i tre punti utili corretti (sotto). Non committata.
- ☑ **Il master gioca la carta di chi è fermo:** 30 s per giocare, dopo i 30 s il telefono di chi gioca sceglie la carta con l'IA; se quel telefono non manda niente (secondo piano, app bloccata o modificata), dopo altri 10 s la carta la gioca il master. Nessuno può più tenere fermo il tavolo.
- ☑ **3 turni di fila giocati dal master = fuori:** il posto passa al bot per il resto della partita (anche se quel telefono rientra) e gli altri vedono "Marco è uscito: 3 turni senza giocare / Al suo posto gioca un bot". Se il suo telefono risponde esce come con Abbandona e l'abbandono si conta come sempre.
  - Anche le carte giocate dal master contano tra i "tempi scaduti" sul telefono di chi era fermo: se torna attivo, al terzo esce.
- ☑ **Mosse solo dal proprio telefono:** una carta vale solo se arriva dal telefono di quel posto o dal master (bot, posti scollegati, carta allo scadere). Un'app modificata non può giocare per gli altri né per un posto tolto per inattività.
- ☑ **La rete che cade non dà tempo infinito:** appena Photon si accorge che sei scollegato, al tuo posto gioca il bot (come prima); se Photon ci mette qualche secondo, dopo 40 s gioca comunque il master. Il tuo tempo riparte da 30 solo al turno dopo il rientro.
- ☑ **Sospensione e rientro (lato server):** il CloudScript ricorda l'ultima stanza in cui sei entrato; da sospeso puoi rientrare solo in quella, mai crearne o entrare in una nuova. Funziona anche se Photon chiede il permesso pure per il rientro (non è documentato).
- ☑ **Nomi dei giocatori bloccati:** il telefono ricorda l'ultimo nome visto (quando blocchi dal profilo, e ogni volta che PlayFab lo manda); "Giocatore" solo se non l'ha mai visto.
- ☑ Tempo fisso a 30 s per tutti i turni (niente turni "difficili"), come hai deciso.
- Revisione del codice, corretti:
  - Il permesso di rientro da sospeso non scadeva mai: con i codici corti delle stanze private si poteva entrare in una stanza nuova con lo stesso codice. Ora vale 2 ore.
  - Una carta vecchia (arrivata dopo quella del master) azzerava il conto dei turni fermi: ora lo azzera solo una carta valida giocata davvero dal giocatore.
  - Un telefono scollegato nel momento in cui un posto veniva tolto non lo sapeva e lo ridava al giocatore al rientro: ora il posto tolto è scritto anche nella stanza.
- Provato dal vivo (partita resa "online" per finta in Editor, questo telefono = master): turno dell'avversario fermo giocato dal master a 40 s (due volte), al terzo il posto tolto senza mossa doppia; la carta del master non azzera il conto, quella giocata da sé sì; sul telefono del fermo le carte del master contano e l'uscita scatta una volta sola.
- Non provato dal vivo: il giro vero a due telefoni (mossa del master, posto tolto, controllo del mittente), il rientro da sospeso su Photon.
- Da fare tu: caricare di nuovo `Server/CloudScript/51.js` (sostituisce quello della 2.56; il resto dei passi della 2.56 non cambia).
- Scelte mie, da confermare:
  - Durante i 10 s di tolleranza gli altri vedono "0s" sul banner di chi è fermo.
  - Il conto dei turni giocati dal master riparte da zero se il master cambia (esce chi faceva da master).
  - Chi viene tolto per inattività resta al bot anche se rientra nella stanza; non ha senso ridargli il posto nella stessa partita.
  - Il testo per gli altri: "Marco è uscito: 3 turni senza giocare", sotto "Al suo posto gioca un bot".
- Rimandato, da confermare uno per uno:
  - **Abbandono contato dal server per un'app modificata:** oggi l'abbandono lo registra il telefono di chi esce; un'app modificata può non farlo. Farlo registrare al master vuol dire fidarsi del master (anche lui potrebbe essere modificato). Lo vuoi, e con quale controllo?
  - **Vittoria per abbandono contro un'app modificata che resta nella stanza:** il suo posto passa al bot ma lui non "esce", quindi la vittoria per abbandono non scatta; si finisce la partita contro il bot.
  - **Tempo del master stesso:** il turno del master lo controlla solo il suo telefono. Se il master va in secondo piano il tavolo aspetta finché Photon non lo scollega (circa 60 s), poi un altro diventa master e il bot gioca. Per farlo controllare dagli altri serve una regola nuova (chi gioca al posto del master?). Lo vuoi?
- ASSET MANCANTI DA CREARE: nessuno.

**2.56:** le tre cose rimandate che hai approvato il 02/10, nel tuo ordine (1 controllo della sospensione sul server, 2 tempo del turno e inattività, 3 giocatori bloccati). Build pulito (0 errori); test EditMode 408 totali: 401 ok, 0 falliti, 7 saltati (Explicit); prove del CloudScript tutte passate. Revisione del codice (agente deep-reviewer) sulle parti online: i punti veri sono corretti (vedi sotto). Non committata.
- ☑ **Controllo della sospensione sul server:** quando si crea una stanza online o si prova a entrarci, Photon chiede al nostro CloudScript se il giocatore è sospeso; se sì rifiuta (errore 32752), anche con un'app modificata che salta il controllo del telefono. Il telefono allora rilegge lo stato: se è sospeso apre la Sospensione, altrimenti "Il gioco online non è disponibile in questo momento. Riprova tra poco.". Se il CloudScript ha un errore lascia passare (un guasto non deve bloccare tutti).
- ☑ **Ora del server per gli ospiti:** le sanzioni degli ospiti (salvate sul dispositivo) usano l'ora del server quando c'è. Cambiare l'ora del telefono non le accorcia più; cancellare i dati dell'app sì, come avevi accettato.
- ☑ **Tempo del turno online: 30 secondi.**
  - Solo online: l'allenamento coi bot resta senza tempo.
  - Il tempo si ferma durante animazioni, distribuzione e finestra degli accusi, e mentre la rete è caduta.
  - Come nel mockup MomentiPartita e nella SPEC, il timer si vede solo negli ultimi 5 secondi. Se tocca a un altro, il bordo e l'anello del suo banner diventano rossi e compare la pillola "5s Marco" (sotto il banner in alto, sopra quelli laterali). Se tocca a te, il tuo banner diventa rosso e sopra le tue carte compare "5 Gioca adesso, o la carta verrà scelta per te".
  - Allo scadere la carta la sceglie l'IA e parte come una mossa normale.
- ☑ **3 tempi scaduti di fila = abbandono:**
  - All'ultimo turno utile la pillola dice "Gioca adesso, o uscirai dalla partita".
  - Al terzo tempo scaduto si esce come con Abbandona: il posto passa a un bot e conta come abbandono, con le stesse regole della 2.55 (compresa la vittoria per abbandono degli altri). Compare "Sei uscito dalla partita: 3 turni senza giocare".
  - Una carta giocata a mano azzera il conto; una partita nuova (anche la rivincita) riparte da zero.
- ☑ **Giocatori bloccati:** Impostazioni > PRIVACY E SOCIALE > Giocatori bloccati, solo con un account (gli ospiti non bloccano).
  - Pagina semplice: una riga di spiegazione, poi l'elenco col nome e "Sblocca" ("Hai sbloccato Marco_93"), oppure "Nessun giocatore bloccato".
  - Le Impostazioni ora scorrono: con la sezione nuova l'account non ci stava più sull'iPhone.
- ☑ Anche: una sola mossa per turno. Un doppio tocco veloce poteva mandarne due e costringere tutti a ricaricare lo stato della partita.
- Revisione del codice, corretti:
  - Il blocco sul server all'ingresso non avrebbe funzionato: il webhook "PathJoin" di Photon è solo un avviso. Rifiutare si può solo con "PathBeforeJoin" (verificato sulla documentazione Photon).
  - Una carta toccata mentre la rete era caduta poteva bloccare il turno per sempre dopo il rientro.
  - Il tempo correva anche con la rete caduta: si poteva uscire "per inattività" per colpa della connessione.
  - Il conto dei tempi scaduti restava nella rivincita.
  - Con l'app in secondo piano il tempo si fermava invece di continuare.
  - Un rifiuto arrivato in ritardo poteva chiudere una ricerca appena ripartita.
  - Da sospeso comparivano due messaggi insieme (la Sospensione e "Gioco online sospeso."): ora solo la Sospensione.
  - "Sblocca" toglieva la riga anche quando il salvataggio non partiva.
- Provato dal vivo su iPhone 12 e SE (partita resa "online" per finta in Editor):
  - pillola del tuo turno e dell'avversario, con le misure del mockup (alte 30 e 26, 12 sotto il banner);
  - anello rosso, carta scelta allo scadere, mossa a mano che azzera il conto, scritta dell'ultimo turno, uscita al terzo, avviso;
  - tempo fermo con la rete caduta, ripartito da 30 al rientro;
  - Impostazioni che scorrono con PRIVACY E SOCIALE, pagina Giocatori bloccati vuota e piena;
  - rifiuto del server come ospite: messaggio "non disponibile" e, con una sospensione finta, la pagina Sospensione.
- Non provato dal vivo:
  - il giro vero a due telefoni (tempo e uscita veri, rifiuto vero di Photon);
  - i nomi veri nella pagina dei bloccati e Sblocca su un account vero (in Editor entro solo come ospite).
- Da fare tu, in quest'ordine:
  1. PlayFab: caricare di nuovo `Server/CloudScript/51.js`: Automation → CloudScript → Revisions (Legacy) → Upload new revision → Deploy. Va fatto **prima** di Photon.
  2. PlayFab Game Manager → Add-ons → Photon: generare la "secret key" se non c'è.
  3. Pannello Photon (la tua app Realtime) → Webhooks:
     - BaseUrl `https://{TitleId}.playfablogic.com/webhook/1/prod/{secret key}`
     - PathCreate `RoomCreated`
     - PathBeforeJoin `RoomBeforeJoin`
     - PathJoin vuoto
  4. Pannello Photon → Authentication: controllare che gli accessi anonimi siano spenti, così Photon conosce sempre l'account PlayFab di chi entra.
  5. Prova subito con un account normale: deve creare stanze ed entrarci come prima. PlayFab non elenca "BeforeJoin" tra i suoi webhook (passa il nome del percorso al CloudScript, ma non è verificato): se nessuno entra più nelle stanze, togli PathBeforeJoin e dimmelo.
  6. Prova con un account di prova sospeso (Internal Data `Sanzioni`, per esempio `{"volte":1,"fine":"2026-12-31T00:00:00Z","motivo":"abbandoni"}`): la ricerca deve fermarsi con la Sospensione. Con un account normale si deve giocare come prima.
  7. Se nessuno riesce più a creare stanze: togli PathCreate e PathBeforeJoin dal pannello Photon e il gioco torna come prima. Photon rifiuta la creazione anche quando PlayFab non risponde, per questo il CloudScript va caricato prima.
- Scelte mie, da confermare:
  - Il timer si vede solo negli ultimi 5 secondi (come dice la SPEC); prima c'è solo l'anello di sempre (blu per gli altri, oro per te).
  - La carta allo scadere la sceglie l'IA della partita, non una carta a caso.
  - All'ultimo tempo prima dell'uscita la pillola cambia in "Gioca adesso, o uscirai dalla partita".
  - Il tempo continua con l'app in secondo piano: chi torna a tempo scaduto gioca subito la carta scelta, e conta come tempo scaduto.
  - Se la rete cade il tempo si ferma; al rientro riparte da 30 secondi.
  - Il tempo lo fa rispettare solo il telefono di chi gioca, così non partono mai due mosse. Un telefono Android in secondo piano tiene il posto fino a 60 s, poi gioca il bot: in quel caso gli altri vedono "0s" e aspettano.
  - Il controllo sul server non dovrebbe bloccare il rientro nella propria partita dopo una caduta di rete: PathBeforeJoin scatta solo per un giocatore nuovo al tavolo. Non è verificato su Photon; se lo bloccasse, chi viene sospeso a metà partita e perde la rete non rientra.
  - Le Impostazioni ora scorrono. La sezione si chiama "PRIVACY E SOCIALE" e sta prima di ACCOUNT.
  - I nomi nella pagina dei bloccati si chiedono a PlayFab a ogni apertura (la lista salva solo gli id); se non arrivano resta "Giocatore".
- Rimandato, da confermare uno per uno:
  - **Tempo controllato anche dal master:** per un telefono rimasto in secondo piano (fino a 60 s) o un'app modificata che non gioca, il master potrebbe giocare la carta al posto suo dopo 30 + 10 s. Serve un controllo in più per non avere due mosse. Lo faccio?
  - **Tempo più lungo per i turni difficili (35-40 s):** avevi detto "puoi poi salire"; per ora sono 30 per tutti. Lo vuoi, e con che regola (per esempio quando ci sono più prese possibili)?
- ASSET MANCANTI DA CREARE: nessuno (pillole, anelli e pagina fatti con forme e testi esistenti).

**2.55:** le tue risposte del 01/10 sulla moderazione: le 6 idee (tutte sì) e le correzioni alle mie scelte. Build pulito (0 errori); test EditMode 406 totali: 399 ok, 0 falliti, 7 saltati (Explicit); prove del CloudScript tutte passate. Due revisioni del codice (agente deep-reviewer) sulle parti online, i punti trovati sono corretti (vedi sotto). Non committata.
- ☑ **Motivo della segnalazione:** "Segnala giocatore" nel profilo rapido apre "Perché lo segnali?" con **Emoticon offensive**, **Nome offensivo**, **Gioco scorretto** e Annulla. Emoticon → emoticon spente (24 ore, poi 3 giorni, poi 7); nome e gioco → sospensione del gioco online. I due conti sono separati.
- ☑ **Soglia segnalazioni:** 5 account diversi, da almeno 3 partite diverse, negli ultimi 7 giorni (così un gruppo di amici nella stessa partita non basta).
- ☑ **Durate:** sospensione 24 ore → 3 giorni → 3 giorni → 7 giorni (poi 7); dopo 30 giorni senza sanzioni si riparte da 24 ore. Il server tiene comunque uno storico completo che non si azzera mai (`StoricoSanzioni`).
- ☑ **Ospiti:** abbandoni e sanzioni contati sul dispositivo (stesse regole: 5 abbandoni in 7 giorni, stesse durate), e le sanzioni prese nella loro sessione restano sul dispositivo. Ora gli ospiti si possono anche segnalare (pubblicano l'id della sessione). Emoticon spente e blocco del gioco online valgono anche per loro.
- ☑ **RIVINCITA:** chi è sospeso finisce la partita in corso ma non ne inizia un'altra. A fine partita il server viene ricontrollato; l'host sospeso vede solo **Torna alla Home** (sparisce RIVINCITA, niente due pulsanti uguali); un altro giocatore sospeso esce quando la rivincita parte, prima di qualsiasi mossa, quindi non conta come abbandono.
- ☑ **Blocca giocatore** (nel profilo rapido, accanto a Segnala): emoticon spente, niente inviti né messaggi da lui, tolto dagli amici. "Sblocca giocatore" per tornare indietro. Salvato sul tuo account PlayFab (`Bloccati`).
- ☑ **Regole di comportamento:** la sezione 3 dei Termini ora si chiama "Regole di comportamento (fair play)" e ha un paragrafo nuovo su abbandoni, inattività, segnalazioni e sanzioni (anche automatiche, anche per gli ospiti sul dispositivo). Dalla Sospensione i Termini si aprono direttamente lì. Corretto anche lo scorrimento alla prima apertura, che finiva 61 punti più giù.
- ☑ **Tempo che manca:** "1g 23h 59m", sotto un'ora "42:18".
- ☑ **Esiti delle segnalazioni:** tutti quelli non letti in una finestra sola ("Hai 3 aggiornamenti…"): una riga per esito, fino a 3, poi "E altre N.".
- ☑ **Cambia password:** testo neutro "Se g•••••@mail.com è l'email di un account 51, il link è in arrivo."
- ☑ **Vittoria per abbandono:** se gli avversari umani escono davvero dalla partita (con ESCI, oppure posto scaduto dopo 60 s), chi resta vince subito. Schermata VITTORIA con "L'AVVERSARIO HA ABBANDONATO" (o "GLI AVVERSARI HANNO ABBANDONATO"), vittoria, XP e monete come una partita vinta, poi **Torna alla Home** (niente rivincita). Protezioni:
  - serve almeno una smazzata finita: prima resta il bot, e alla fine della smazzata si vince;
  - monete e XP per abbandono al massimo 3 volte al giorno e una sola volta per avversario; oltre, la vittoria conta ma senza monete ("Niente monete: abbandoni già premiati oggi");
  - a chi esce l'abbandono conta come sempre;
  - una rivincita contro il bot, dopo che l'altro è uscito a fine partita, non vale.
- ☑ **Rientro dopo la chiusura dell'app:** chi ha un account e chiude l'app durante una partita online torna al tavolo se la riapre entro 60 s dall'ultima uscita ("Riconnessione…" coi tentativi), e l'abbandono non conta. Se non fa in tempo, se la stanza non c'è più o se nessuno ha più lo stato della partita, va in Home e l'abbandono conta come prima. I posti al tavolo ora restano giusti anche se intanto qualcuno è uscito: l'ordine dei posti è salvato nella stanza.
- Revisione del codice, corretti:
  - un errore grave che avevo introdotto: la schermata di fine partita normale restava vuota (trovato prima di consegnare);
  - una vittoria per abbandono possibile in una rivincita contro il bot;
  - una mossa vincente ancora in volo scartata dall'abbandono;
  - il tavolo che poteva restare a metà animazione;
  - l'XP per abbandono senza limiti;
  - l'avversario sbagliato nel conteggio;
  - (seconda revisione, sul rientro) se dopo il rientro lo stato della partita non arrivava, lo stato "rientro in corso" restava acceso e poteva rimandare in Home dalla partita online successiva;
  - (seconda revisione) l'ordine dei posti salvato nella stanza ora lo legge solo chi entra, l'host lo ricalcola sempre.
- Provato dal vivo su iPhone 12 e SE:
  - profilo rapido con motivi e Blocca, anche nella versione ospite;
  - avviso delle emoticon spente;
  - finestra con più esiti;
  - conto alla rovescia;
  - Termini aperti sulla sezione 3, anche alla prima apertura;
  - schermata di vittoria per abbandono: didascalia su una riga, solo Torna alla Home, riga delle monete;
  - fine partita normale.
- Non provato dal vivo: il giro vero online a due telefoni (abbandono vero, rientro vero dopo la chiusura, segnalazioni vere sul server). Il rientro funziona solo con un account vero, e in Editor posso entrare solo come ospite. Da provare tu con due account di prova.
- Da fare tu su PlayFab (Game Manager):
  - Caricare di nuovo `Server/CloudScript/51.js`: Automation → CloudScript → Revisions (Legacy) → Upload new revision → Deploy.
  - Facoltativo: Title Data `Moderazione` con le chiavi nuove, per esempio `{"segnalazioni":5,"partite":3,"abbandoni":5,"giorni":7,"ore":[24,72,72,168],"silenzio":[24,72,168],"azzeramento":30}`.
  - Facoltativo: Title Data `Economia` → `partita.abbandoni`, cioè quante vittorie per abbandono premiare al giorno (oggi 3).
  - Spegnere le emoticon a mano: Internal Data del giocatore, chiave `Silenzi`, stesso formato di `Sanzioni`.
- Da fare tu: rileggere il paragrafo nuovo nei Termini (sezione 3) e, se i Termini sono anche sul sito, aggiornare la copia web.
- Scelte mie, da confermare:
  - Il rientro dopo la chiusura vale solo per chi ha un account: l'ospite ha un'identità nuova a ogni avvio e Photon non lo riconosce. Si può fare anche per l'ospite tenendo la stessa identità se riapre entro 60 s, ma cambia la regola "ospite nuovo a ogni avvio": decidi tu.
  - I 60 s del rientro partono dall'ultima volta che l'app è andata in secondo piano; se l'app si è chiusa da sola senza passarci (crash) si prova comunque.
  - Vittoria per abbandono solo dopo almeno una smazzata finita; prima resta il bot e, a fine smazzata, vince chi è rimasto.
  - Tetti per abbandono: 3 al giorno e uno per avversario, per monete e XP.
  - Le sanzioni degli ospiti stanno sul dispositivo: cancellando i dati dell'app o cambiando l'ora del telefono si azzerano.
  - Gli ospiti non possono bloccare né aggiungere amici (non hanno un account dove salvarlo).
  - Le richieste d'amicizia non esistono ancora (oggi "Aggiungi amico" aggiunge e basta): il blocco toglie dagli amici e ferma inviti e messaggi.
  - Motivi: "nome" e "gioco" contano insieme (sospensione), "emoticon" a parte (emoticon spente).
- Rimandato, da confermare uno per uno:
  - **Inattività (3 turni saltati di fila = abbandono):** serve prima il timer del turno, che oggi non esiste (Fase 14, rimandato). Lo faccio? E quanti secondi per turno?
  - **Controllo della sospensione anche sul server alla partenza della ricerca** (hai detto "poi").
  - **Elenco dei giocatori bloccati** fuori dal tavolo, per sbloccare qualcuno che non incontri più: è una schermata nuova, aspetto il tuo sì.
- ASSET MANCANTI DA CREARE: nessuno (motivi e Blocca fatti con forme e testi esistenti).

**2.54:** Fase 11 chiusa con le tue scelte del 01/10: **Cambia password**, **segnalazioni e abbandoni contati sul server**, **Sospensione** ed **Esito segnalazione**. Build pulito (0 errori); test EditMode 399 totali: 392 ok, 0 falliti, 7 saltati (Explicit), 15 nuovi in `ModerationViewTests`; prove del CloudScript (`node Server/CloudScript/test.js`) tutte passate. Non committata.
- ☑ **Cambia password** (scelta A): riga nuova nelle Impostazioni, sezione Account, tra Email ed Esci (solo con un account vero). Un tocco manda subito il link all'email dell'account e apre "Controlla la posta" con "Il link è in arrivo a g•••••@mail.com." e **CHIUDI** (al posto di TORNA AL LOGIN). Stessa pagina e stessa chiamata della Password dimenticata, ora con un suo Canvas a 2100 così sta sopra alle Impostazioni.
- ☑ **Segnalazioni sul server:** "Segnala giocatore" del profilo rapido ora chiama il CloudScript `segnala` (prima `ReportPlayer` di PlayFab, che scrive solo un evento e non conta niente). Il server tiene i segnalatori diversi degli ultimi 7 giorni: alla 5ª persona diversa sospende per 24 ore (la volta dopo 3 giorni), azzera le segnalazioni e lascia un esito a ognuno dei segnalatori.
- ☑ **Abbandoni contati** (solo chi ha fatto il login, mai contro i soli bot): conta l'uscita con ESCI da una partita online non finita con almeno un'altra persona nella stanza, e la chiusura dell'app durante una partita così (contata al primo arrivo in Home di quell'account, anche se intanto è entrato un altro). 5 abbandoni in 7 giorni = stessa sospensione (24 ore, poi 3 giorni). La connessione persa non conta.
- ☑ **Avviso, senza conto alla rovescia:** nella finestra Abbandona, solo quando l'uscita conterebbe, si aggiunge una frase: "Chi abbandona spesso le partite online viene sospeso per un po’." Niente "ti restano N abbandoni".
- ☑ **Sospensione** (mockup `Sospensione`): blocca solo il gioco online (partita veloce, crea e entra in stanza privata); toccando GIOCA online si apre la pagina. Scudo rosso, "Account sospeso", motivo, tempo che manca (si aggiorna da solo e chiude la pagina allo scadere), **GIOCA CONTRO I BOT** (apre Modalità sulla scheda Allenamento) e **Regole di comportamento**. All'arrivo in Home compare da sola una volta per sospensione.
- ☑ **Esito segnalazione** (mockup `SegnalazioneEsito`): all'arrivo in Home, se una persona che hai segnalato è stata sospesa: scudo d'oro, "Grazie per la segnalazione!", riquadro "Segnalazione del 28 settembre · partita 1 vs 1. Per privacy non mostriamo il nome." e **PREGO!**. Una volta sola.
- Revisione del codice (agente deep-reviewer) con 7 punti, corretti 6:
  - il segno "partita in corso" ora vale per esecuzione dell'app, non per "prima Home": entrare come ospite o con un altro account dopo aver chiuso l'app a metà non lo cancella più;
  - se l'altra persona esce e restano solo bot, il segno si toglie alla mossa successiva (prima la chiusura dell'app contava lo stesso);
  - creare o entrare in una stanza privata dalle vecchie strade (popup del codice) ora è bloccato anche lì;
  - dopo il 5° abbandono la Home aspetta la risposta del server, così la Sospensione compare subito;
  - la pagina Sospensione si riapre per ogni nuova data di fine, anche per le sanzioni scritte o allungate a mano;
  - il 7° (due segnalazioni nello stesso istante possono perderne una, mai una sanzione doppia) l'ho lasciato con una nota nel codice.
- Menu: stesso **Tools/UI51/Build Fase 11 (Account)**. Scudi disegnati dal tracciato del mockup (`Tools/ui51/shields.py` → `shield_alert.png`, `shield_check.png`).
- Provato dal vivo su iPhone 12 e SE: Sospensione (misure come il mockup: pannello a 410, pulsante a 707 contro 704), GIOCA CONTRO I BOT, i due blocchi online (la ricerca non parte, nessuna stanza), Esito segnalazione, riga Cambia password e stato "inviata" sopra alle Impostazioni, avviso nella finestra Abbandona (4 righe, nessun taglio). Sospensione ed esito messi in scena a mano.
- Non provato: il giro vero col server (il CloudScript nuovo va caricato, vedi sotto), l'invio vero del link di Cambia password (andrebbe a un indirizzo reale) e l'accesso col nome utente della 2.53. Da provare tu con account di prova.
- Da fare tu su PlayFab (Game Manager):
  - Caricare `Server/CloudScript/51.js`: Automation → CloudScript → Revisions (Legacy) → Upload new revision → Deploy. Senza questo, Segnala dà errore e niente viene contato.
  - Facoltativo: Content → Title Data, chiave `Moderazione`, per cambiare soglie e durate senza aggiornare l'app, per esempio `{"segnalazioni":6,"abbandoni":5,"giorni":7,"ore":[24,72]}`.
  - Sospendere a mano: Players → (giocatore) → Internal Data, chiave `Sanzioni`, per esempio `{"volte":2,"fine":"2026-10-05T18:00:00Z","motivo":"Linguaggio offensivo"}`. La pagina si apre da sola alla prossima Home; `volte` serve solo a far durare di più la sospensione automatica successiva.
- Scelte mie, da confermare:
  - Soglia segnalazioni **5** (tu hai detto 5 o 6): si cambia da Title Data senza aggiornare l'app.
  - Soglia abbandoni **5 in 7 giorni** (il numero non l'avevi detto).
  - Dalla terza sospensione in poi resta 3 giorni; le volte non si azzerano mai (vedi idee).
  - Possono segnalare solo i giocatori con un account (gli ospiti non vedono il pulsante, il server li rifiuta comunque); ogni giocatore conta una volta sola anche se segnala più volte.
  - Gli ospiti non vengono mai sospesi (non hanno il pulsante Segnala addosso e i loro abbandoni non contano).
  - Se l'altra persona esce prima di te e restano solo bot, la tua uscita non conta.
  - La RIVINCITA resta nella stessa stanza e non controlla la sospensione (una sospensione arriva solo in Home).
  - Il blocco è nell'app, non un "ban" di PlayFab: il ban di PlayFab blocca tutto (anche l'allenamento, il profilo e i premi), contro la tua scelta di lasciar giocare contro i bot. Limite: un'app modificata potrebbe aggirarlo.
  - Sospensione: ho aggiunto il pulsante indietro in alto (nel mockup non c'è e la pagina sarebbe stata un vicolo cieco).
  - "Regole di comportamento" apre i Termini di servizio (non c'è una pagina di regole a parte).
  - Motivi mostrati: "Abbandoni ripetuti", "Segnalazioni dei giocatori", oppure il testo scritto a mano in Game Manager; testo sotto il titolo diverso per motivo.
  - Tempo che manca nel formato "1g 23:59:59" (sotto un giorno "23:59:59").
  - Se arrivano più esiti prima che tu apra l'app, ne vedi uno solo (l'ultimo).
  - Cambia password manda il link al primo tocco, senza chiedere conferma (scelta A).
- Rimandato, da confermare uno per uno:
  - **Bloccare la chat per comportamenti scorretti:** oggi non c'è nessuna chat scritta (al tavolo solo emoticon; Photon Chat serve solo per gli inviti). Si fa quando arriva la chat (J3).
  - **Aggiungere gli abbandoni ripetuti ai Termini di servizio**, sezione sul comportamento: il testo legale lo scrivi tu.
  - **Lingua** e **Notifiche** (righe L1 e L2): più avanti, come hai detto.
- Idee mie, aspettano il tuo sì (una per riga):
  - **Motivo della segnalazione:** Segnala chiede il perché (emoticon offensive, nome offensivo, gioco scorretto). Per le emoticon la pena è il silenzio delle emoticon per 24 ore, non la sospensione: è il "blocco della chat" che si può fare oggi.
  - **Rientro dopo la chiusura dell'app:** riaprendo entro 60 s si torna al tavolo (la stanza tiene il posto già 60 s) e non conta come abbandono.
  - **Compenso a chi resta:** se l'avversario abbandona, chi resta prende comunque la vittoria piena o un piccolo premio.
  - **Blocca giocatore:** oltre a silenziare, niente più inviti e richieste d'amicizia da lui.
  - **Le volte si azzerano:** dopo 30 giorni senza sanzioni si riparte da 24 ore.
  - **Inattività:** quando ci sarà il timer del turno (Fase 14, rimandato), troppi turni saltati di fila contano come abbandono.
- ASSET MANCANTI DA CREARE: nessuno (scudi disegnati dal tracciato SVG del mockup; ic_warn_cream e Bagliore_morbido già nell'inventario).

**2.53:** risposte tue sulla Fase 11 (01/10) e **accesso col nome utente**. Build pulito (0 errori); test EditMode 384 totali: 377 ok, 0 falliti, 7 saltati (Explicit). Non committata.
- ☑ **Accesso con email o nome utente:** la casella dell'Accesso diceva già "Email o nome utente" ma accettava solo l'email. Ora senza "@" entra col nome utente scelto alla registrazione (PlayFab `LoginWithPlayFab`), con "@" con l'email come prima; errore "Nome utente o password non corretti". Una sola modifica in `PlayFabAuthService.LoginWithEmail`, che usano tutti.
- Non provato dal vivo: l'accesso vero col nome utente (serve un account reale, da provare tu).
- Le tue risposte:
  - **SceltaNome:** niente schermata. La registrazione chiede sia l'email sia il nome utente (era già così) e si accede con l'uno o con l'altro (fatto qui sopra).
  - **Lingua:** per ora resta "Italiano"; altre lingue più avanti (riga L1).
  - **Notifiche:** per ora nascoste; arriveranno più avanti (riga L2).
  - **CambiaPassword**, **SegnalazioneEsito** e **Sospensione:** ti ho spiegato come si possono fare, aspetto la tua scelta.

**2.52:** Fase 11, la parte fattibile: **Password dimenticata** (`PasswordDimenticata` + `PasswordInviata`). Menu nuovo **Tools/UI51/Build Fase 11 (Account)** (`Assets/UI51/Editor/UI51AccountBuilder.cs`), vista `UI51RecoveryView` in `MainMenu` subito sopra all'Accesso. Build pulito (0 errori); test EditMode 384 totali: 377 ok, 0 falliti, 7 saltati (Explicit). Non committata.
- ☑ **Password dimenticata:** il link dell'Accesso ora apre la pagina del mockup (prima scriveva solo una riga sotto al modulo): indietro, cerchio col lucchetto, "Password dimenticata?", testo, campo email (già riempito se nell'Accesso c'era un'email) e **INVIA IL LINK**. Dopo l'invio: busta, "Controlla la posta", riquadro con la spunta verde, **TORNA AL LOGIN** e "Puoi reinviarla tra 30s" che diventa **Non è arrivata? Reinvia**. Email vuota o sbagliata: avviso rosso sotto. La chiamata a PlayFab è la stessa di prima, spostata nella pagina.
- Provato dal vivo su iPhone 12 e SE: apertura dal link con e senza email già scritta, avviso con email vuota, stato "inviata" e reinvio dopo 30 s. Misure: cerchio a 96-180 e pulsante che finisce a 458, come il mockup.
- Non provato: l'invio vero dell'email (manderebbe una mail a un indirizzo reale: lo stato "inviata" l'ho messo in scena). Da provare tu con un account di prova.
- Scelte mie, da confermare:
  - Il riquadro dice **"Se g•••••@mail.com è l'email di un account 51, il link è in arrivo."** al posto di "Email inviata a g•••••@mail.com. Il link scade tra 30 minuti.": la risposta resta uguale che l'account esista o no (come prima, per non far scoprire chi gioca qui) e PlayFab non documenta una scadenza fissa del link.
  - La spunta verde è disegnata col tracciato del mockup (l'icona ic_check_cream ha già un suo cerchio).
- Rimandato, da confermare uno per uno:
  - **SceltaNome** (avatar e nome dopo la registrazione): la nostra Registrazione chiede già il nome utente e lo usa come nome al tavolo, quindi sarebbe un doppione. Da decidere: farla al posto del campo, solo quando il nome è già preso, oppure lasciarla.
  - **CambiaPassword:** resta nascosta (decisione 2.30, aspetta il modello di email di recupero su PlayFab).
  - **Lingua:** resta la riga "Italiano" (decisione 2.30, una sola lingua).
  - **Notifiche:** resta nascosta (decisione 2.30, l'app non manda notifiche).
  - **SegnalazioneEsito** e **Sospensione:** servono segnalazioni e sanzioni sul server (vedi ❓).
- ASSET MANCANTI DA CREARE: nessuno (ic_lock_cream, ic_mail, ic_nav_back_cream, home_bg_base già nell'inventario).

**2.51:** Fase 12, seconda parte: **Benvenuto** e **partita guidata** (`Benvenuto`, `TutorialPartita` coi passi 2/3/5, `TutorialSalta`, `TutorialFine`), "Rifai il tutorial" acceso in Regole. Stesso menu **Tools/UI51/Build Fase 12 (Regole e tutorial)**: ora costruisce anche il Benvenuto in `MainMenu` e la guida in `GameScene` (viste nuove `UI51WelcomeView`, `UI51TutorialView`). Build pulito (0 errori); test EditMode 384 totali: 377 ok, 0 falliti, 7 saltati (Explicit), col test nuovo sulla distribuzione del tutorial. Non committata.
- ☑ **Benvenuto:** al primo ingresso in Home (da ospite o con l'accesso) compare una volta: logo col bagliore, "Benvenuto al tavolo!", "Conosci già la Cirulla?", **No, insegnami** (bordo d'oro, dorso Giada, "Una partita guidata di 3 minuti") e **Sì, voglio giocare** (resta in Home), nota "Il tutorial e le regole sono sempre nelle Impostazioni".
- ☑ **Partita guidata:** allenamento 1 contro 1 col bot facile, con una **distribuzione fissata**: mazziere il bot, tu di mano con Asso di denari, 7 e 2 di bastoni; in tavolo Asso di bastoni, Re di coppe, 4 di spade e 3 di coppe; nessun accuso e niente 15/30. Finita la distribuzione (e i 5 secondi dell'accuso) arriva Nonna Rosa coi 6 passi del mockup: velo scuro con il **buco sulle carte vere** (la tua mano, tavolo e mano, il tavolo, il bottone ACCUSA, il punteggio) e l'anello d'oro che pulsa, il **dito** che tocca l'Asso di bastoni e il 7, i pallini e **Salta** in alto, il fumetto con Indietro e AVANTI (FINE all'ultimo). Il fumetto si mette sopra o sotto al buco da solo.
- ☑ **Salta:** "Saltare il tutorial?" con CONTINUA IL TUTORIAL e **Salta e vai alla Home**. **Fine:** "TUTORIAL COMPLETATO · Sei pronto per il tavolo!", **GIOCA LA PRIMA PARTITA** (si gioca questa stessa partita, a carte già spiegate) e **Leggi tutte le regole** (torna in Home e apre Regole).
- ☑ **Rifai il tutorial** in Regole riparte da capo. Dopo la distribuzione del tutorial le smazzate tornano casuali.
- Provato dal vivo su iPhone 12 (Benvenuto al primo ingresso, No insegnami, i 6 passi, Salta e Continua, fine, GIOCA LA PRIMA PARTITA col tavolo che torna giocabile) e SE (tutto il giro, più Leggi tutte le regole, Rifai il tutorial, Salta e vai alla Home). Misure del Benvenuto: le scelte a 498 e 586 come il mockup; larghe quanto la Safe dell'iPhone 12 (431 meno i margini), come le altre schermate.
- Non provato: il Benvenuto dopo un accesso vero (provato da ospite), su telefono vero.
- Scelte mie, da confermare:
  - **La guida sta sopra a una partita vera** e le carte non si giocano mentre parla Nonna Rosa (il velo prende i tocchi); alla fine **GIOCA LA PRIMA PARTITA continua questa partita** invece di tornare in Home come il link del mockup.
  - **Distribuzione fissata da un seme** (cercato apposta) invece di carte scelte a mano: il test nuovo controlla che resti quella.
  - **Bot facile** nella partita guidata.
  - Passo 1: **"basta toccarla"** al posto di "tocca una carta per sceglierla, toccala di nuovo per giocarla": da noi la carta parte al primo tocco.
  - Passo 5: "A ogni distribuzione questo bottone si accende: se hai un accuso, premilo entro 5 secondi" al posto di "Quando puoi, compare questo bottone": il bottone c'è sempre e si accende a ogni distribuzione (per non rivelare chi ha l'accuso).
  - La guida parte **dopo i 5 secondi dell'accuso**, non appena posate le carte.
  - **Niente premi:** tolti il "+200" del Benvenuto, i riquadri "+200" e "Dorso Smeraldo" della fine e la frase "Il premio di benvenuto resta disponibile finché non lo completi" (il server non li dà ancora).
  - Il **Benvenuto compare una volta per telefono**, anche a chi gioca già (dopo l'aggiornamento lo vede una volta).
  - Buco e fumetto **calcolati sulle carte e sui bottoni veri** (margini 12-16) invece delle posizioni fisse del mockup; lo spazio fra fumetto e buco è 24.
  - **Il dito** è disegnato in PNG dallo stesso tracciato del mockup (`Tools/ui51/tutorial_finger.py` → `Assets/UI51/Art/Common/tutorial_finger.png`).
  - AVANTI e FINE larghi uguali (104). L'alone dell'anello si allarga di 8 e svanisce; con la grafica ridotta niente alone e dito fermo.
- Rimandato, da confermare uno per uno:
  - Premio del tutorial (+200 monete e dorso Smeraldo) dato dal server, con i riquadri della fine e il "+200" del Benvenuto.
  - Ricordare che il tutorial è stato completato (serve solo quando c'è il premio).
  - Ingresso "Partita guidata" anche da Modalità → Allenamento.
- ASSET MANCANTI DA CREARE: nessuno (logo_51, Bagliore_morbido, back_giada, ic_nav_back_cream, home_bg_base, av_8 già nell'inventario; il dito ricavato dal mockup).

**2.50:** Fase 12, prima parte: pagina **Regole** (`Regole`, `RegoleAccusi`, `RegolePunteggio`) e riga **"Regole e tutorial"** nelle Impostazioni (sezione Generale, sotto Lingua). Menu nuovo **Tools/UI51/Build Fase 12 (Regole e tutorial)** (`Assets/UI51/Editor/UI51RulesBuilder.cs`), vista `UI51RulesView` in `MainMenu` subito sopra alle Impostazioni; la riga la costruisce **Tools/UI51/Build Fase 4** (rieseguito). Build pulito (0 errori); test EditMode 383 totali: 376 ok, 0 falliti, 7 saltati (Explicit). Non committata.
- ☑ **Regole:** indietro, "Regole", cinque schede (Prese · Scopa · Accusi · Punteggio · Formati) che scorrono di lato se non ci stanno. Prese, Scopa e Accusi hanno riquadri con titolo, testo ed **esempio a carte** sul panno verde (carta giocata col filo d'oro, freccia, carte prese, scritta oro a destra), con le **carte del mazzo scelto** dal giocatore. Punteggio e Formati hanno la tabella. In fondo il consiglio col bordo tratteggiato.
- ☑ **Testi controllati sul codice delle regole** (uno per uno): precedenza della carta uguale, 15, asso piglia tutto, scopa, ultima giocata, 5 secondi per l'accuso, Cirulla e Decino (Decino vince sulla Cirulla), 15/30 del mazziere, soglie 6 e 21 (a 4 basta la maggioranza), primiera, grande, piccola, 6 e 3 mani, cappotto, tre assi, pareggio. Tutti tornano.
- Provato dal vivo su iPhone 12 (tutte e cinque le schede dalla riga delle Impostazioni) e SE (Impostazioni da ospite: la riga nuova sta e il piè di pagina non si sovrappone; scheda Accusi).
- Scelte mie, da confermare:
  - **Decino con tre carte nell'esempio** (coppia di 6 più il 7 di coppe): il mockup ne mostra solo due ma la scritta dice "coppia di 6 + matta".
  - **Panno verde a gradiente orizzontale** al posto del radiale del mockup (la forma UI non ha il radiale): chiaro al centro, scuro ai lati.
  - Cambiando scheda il contenuto **compare senza salire di 6 px** (la salita lo lascerebbe fuori posto dentro allo scorrimento).
  - "Rifai il tutorial" era spento nella 2.50; acceso nella 2.51.
- ASSET MANCANTI DA CREARE: nessuno (ic_nav_back_cream, carte dei mazzi, sfondo già nell'inventario).

**2.49:** Fase 14, seconda parte: spareggio (`MomentoSpareggio`) e vittorie immediate (`Cappotto`, `TreAssi`). Stesso menu **Tools/UI51/Build Fase 5 (Tavolo 1v1)** (passi nuovi `BuildTie` e `BuildInstant` in `UI51ResultsBuilder.cs`), dentro alla vista dei risultati `UI51ResultsView`. Build pulito (0 errori); test EditMode 383 totali: 376 ok, 0 falliti, 7 saltati (Explicit), col controllo nuovo "TRE ASSI" nella pillola. Non committata.
- ☑ **Spareggio:** se a fine smazzata due o più sono in testa a pari punti sopra al traguardo, 1,2 s dopo i risultati compare la finestra "SPAREGGIO · Parità a 53!" con gli avatar dei primi (i tuoi in oro, gli altri in blu, le coppie accavallate) col "=" in mezzo e i punti, "Avete superato 51 a pari punti: si gioca un'altra smazzata. Vince chi resta in testa da solo." e il pulsante d'oro **SMAZZATA DI SPAREGGIO** (fa partire la smazzata come PROSSIMA SMAZZATA; per chi non è l'host chiude solo la finestra). Un tocco fuori la chiude.
- ☑ **Cappotto vinto:** al posto di nastro e sfida, schermata "PARTITA 1 VS 1 · VITTORIA IMMEDIATA", **CAPPOTTO!** grande con l'ombra marrone, "Hai preso tutti e 10 i denari" (a coppie "Tutti e 10 i denari presi da Tu e Giulia"), il tuo avatar grande (a coppie i due accavallati) con l'alone, bagliore che scoppia, raggi, **pioggia di monete** (le 28 del mockup, coi loro tempi). Sotto: ricompense (XP e monete vere), RIVINCITA e Torna alla Home.
- ☑ **Tre assi vinti:** stessa schermata con "VITTORIA IMMEDIATA", **TRE ASSI!**, "Hai ricevuto tre assi in mano: la partita è tua" (a coppie, se li ha il compagno: "Giulia ha ricevuto tre assi in mano: la partita è vostra") e i **tre assi veri** della mano che cadono ruotati al loro posto.
- ☑ **Chi perde** vede la solita SCONFITTA, ma col punteggio scritto "TRE ASSI" invece di "CAPPOTTO" quando la partita l'hanno chiusa i tre assi.
- ☑ **Pillola del punteggio:** "TRE ASSI" (a quattro "3 ASSI") invece di "CAPPOTTO", e non lo scrive più mentre le carte volano: compare insieme al cartello TRE ASSI!, a carte posate (prima si leggeva durante la distribuzione).
- Provato dal vivo su iPhone 12 (tre assi veri da una distribuzione fissata: pillola "0" durante il volo e "TRE ASSI" a carte posate, cartello, schermata coi tre assi della mano; spareggio 1v1 a 53 e cappotto 1v1 messi in scena) e SE (cappotto 2v2 e spareggio 2v2 messi in scena). Misure: finestra dello spareggio larga 342 e alta 326, come il mockup (342 per circa 327).
- Non provato: uno spareggio e un cappotto arrivati giocando (messi in scena coi punteggi), lo spareggio online con l'host e un ospite (servono due telefoni).
- Scelte mie, da confermare:
  - **Schermata Cappotto / Tre assi solo per chi vince**; chi perde ha la SCONFITTA normale con "CAPPOTTO" o "TRE ASSI" al posto dei punti.
  - **Ricompense vere** (XP e monete del server) al posto di "RICOMPENSE SPECIALI +300 monete, +250 XP, Trofeo Cappotto" del mockup: premi speciali e trofei non esistono ancora (Trofei è la Fase 15).
  - **Niente coriandoli** sulla vittoria immediata: piovono già le monete. Con la grafica ridotta niente pioggia.
  - **Spareggio sopra ai risultati della smazzata**, 1,2 s dopo; il conto alla rovescia "Si riparte da sola" continua sotto (chi non tocca nulla riparte comunque, anche online).
  - **"3 ASSI"** nella pillola a quattro giocatori (come "CAPP." per il cappotto).
  - I tre assi sono **centrati**: nel mockup sono 9 punti più a destra.
  - **Testo dello spareggio quando tu non sei fra i primi** (a quattro): "Marco e Luca hanno superato 51 a pari punti…" invece di "Avete superato".
- Rimandato, da confermare uno per uno:
  - **Premi speciali del cappotto e dei tre assi** (+300 monete, +250 XP, trofeo): servono una regola dei premi e la CloudScript; da decidere insieme ai Trofei (Fase 15).
- ASSET MANCANTI DA CREARE: nessuno (ic_coin, Bagliore_morbido, rays_conic e le carte del mazzo già nell'inventario; medal_sun del trofeo non usato finché non c'è il trofeo).

**2.48:** Fase 14, prima parte: momenti al tavolo (`MomentiPartita`: `MomentoTurno4`, `MomentoDisconnesso`, `MomentoScopa`, `MomentoDistribuzione`, `MomentoUltima`). Menu **Tools/UI51/Build Fase 5 (Tavolo 1v1)** (passo nuovo in `Assets/UI51/Editor/UI51MomentsBuilder.cs`), vista nuova `UI51TableMoments` in `GameCanvas/UI51Moments`. Build pulito (0 errori); test EditMode 383 totali: 376 ok, 0 falliti, 7 saltati (Explicit). Non committata.
- ☑ **Turno degli altri:** bordo e anello che pulsa **blu** sul banner di chi gioca (1,4 s come nel mockup); il tuo turno resta oro.
- ☑ **MANO 3 DI 6:** a ogni distribuzione (anche la prima), finito il volo delle carte, chip "MANO n DI 6" (a 4: "DI 3") 100 sotto al banner in alto; compare, resta 2,5 s e svanisce. All'ultima distribuzione al suo posto **"ULTIMA MANO · MAZZO FINITO"** col bordo rosso.
- ☑ **SCOPA!:** velo scuro, bagliore che scoppia, "SCOPA!" grande che entra ruotando e si posa (con l'ombra dura marrone), "Tu · +1" (o il nome), otto scintille dal centro; poi svanisce (2,2 s). Insieme a suono, scintille e scia di luce che c'erano già.
- ☑ **Giocatore disconnesso:** avviso breve sotto al banner in alto col suo avatar spento, "Marco si è disconnesso" / "Gioca un bot finché non rientra" (bordo rosso); sul suo banner velo scuro e tondo rosso col Wi-Fi barrato finché non rientra (sui banner laterali a 4 il tondo sta sull'angolo dell'avatar). "Marco è rientrato in partita" e "Sei di nuovo in partita!" usano lo stesso avviso col bordo oro. Il vecchio avviso di connessione del tavolo è spento.
- Provato dal vivo su iPhone 12 (1v1: MANO 1 DI 6 alla distribuzione vera; SCOPA!, disconnesso e ultima mano lanciati a mano) e SE (4 giocatori: MANO 1 DI 3 vera, disconnesso con due banner laterali, SCOPA! di un bot). Misure: chip alta 30 e avviso alto 46 come il mockup.
- Non provato: una scopa vera in partita (il momento parte dallo stesso punto del suono della scopa), una disconnessione vera (servono due telefoni).
- Scelte mie, da confermare:
  - **Testo del disconnesso:** il mockup dice "Tra 30s lo sostituisce un bot", ma da noi il bot entra subito e il giocatore può rientrare entro 60 s; ho scritto **"Gioca un bot finché non rientra"** (e "Al suo posto gioca un bot" se ha lasciato la partita).
  - **Ritratto del disconnesso spento**, non in bianco e nero (la UI non desatura senza un materiale in più).
  - **SCOPA! senza le due carte che spazzano** del mockup: la presa vera vola già nel mazzetto in quel momento, due carte disegnate in più si sovrapporrebbero.
  - **ULTIMA MANO al posto di "MANO 6 DI 6"** (non tutte e due).
  - Posizioni: chip e avvisi **sotto al banner in alto** (100 e 18 come nel mockup), non a una y fissa: così restano giusti su SE e a 4 giocatori.
- Rimandato, da confermare uno per uno:
  - **Timer del turno** (`MomentoTurno` negli ultimi 5 s, `MomentoTempo` "Gioca adesso, o la carta verrà scelta per te"): il gioco oggi non ha un tempo per turno né una carta scelta da sola. Serve una regola nuova da decidere: quanti secondi, cosa succede allo scadere (carta giocata dal bot?), anche contro i bot o solo online.
- ASSET MANCANTI DA CREARE: nessuno (Bagliore_morbido già nell'inventario; Wi-Fi barrato e scintille disegnati).

**2.47:** Fase 13, terza parte e fine: scheda **Stanza privata** del pannello Modalità (mockup v3) ed errori d'ingresso (`StanzaErrore`, `StanzaPiena`, `StanzaIniziata`). Stesso menu **Tools/UI51/Build Fase 13 (Dalla Home al tavolo)** (ricostruisce anche la scheda; `UI51HomeBuilder` ora la chiede a lui), vista nuova `UI51RoomErrorView`. Build pulito (0 errori); test EditMode 383 totali: 376 ok, 0 falliti, 7 saltati (Explicit). Non committata.
- ☑ **Scheda Stanza privata:** riquadro "Crea una stanza" (cerchio col link, "Ricevi un codice da condividere con gli amici", formati 1 VS 1 / 2 VS 2 / 1 VS 3, CREA STANZA d'oro), "OPPURE", riquadro "Entra con un codice" (campo "ES. A7K2Q" con Incolla dentro, ENTRA). La scheda non ha CONFERMA: prende anche il suo spazio. Il codice si scrive solo in maiuscole e cifre, massimo 5 (anche incollato: "zz-9qx7" diventa "ZZ9QX").
- ☑ **CREA STANZA** crea subito la stanza nel formato scelto e porta alla sala d'attesa; **ENTRA** entra subito ("Ingresso nella stanza…", poi la sala).
- ☑ **Ingresso rifiutato:** finestra sopra la scheda Stanza privata (riaperta col codice ancora scritto): "Codice non valido" (RIPROVA), "Stanza piena" (lucchetto, HO CAPITO), "Partita già iniziata" (carte, HO CAPITO); bordo rosso, icona nel cerchio, "Gioca online invece" sotto. La sceglie il codice di Photon (stanza inesistente, piena, chiusa).
- ☑ **Annulla dalla ricerca o dalla sala** riporta alla scheda Stanza privata (prima tornava su Online o Allenamento, quello che era scelto).
- Fase 13 completa (☑ nell'elenco delle fasi).
- Provato dal vivo su iPhone 12 (codice corto: il campo trema e non parte nulla; codice inesistente vero su Photon: "Codice non valido"; RIPROVA lascia il codice; Piena e Iniziata mostrate a mano; "Gioca online invece" apre la ricerca 1v1; CREA STANZA in 2v2 crea una stanza vera da 4) e SE (la scheda sta tutta, ENTRA e il riquadro in basso visibili senza scorrere).
- Non provato: "Stanza piena" e "Partita già iniziata" da una stanza vera (servono due telefoni).
- Scelte mie, da confermare:
  - **CREA STANZA crea subito** nel formato della scheda (prima si apriva un pannello per scegliere il formato: ora la scelta è nella scheda).
  - **ENTRA con meno di 5 caratteri:** il campo trema e non parte nulla (il mockup non lo dice).
  - Gli errori che non dipendono dal codice (rete, tempo scaduto) restano nella schermata di ricerca con "Connessione non riuscita", come per la creazione; la finestra rossa solo per codice sbagliato, stanza piena, partita iniziata.
  - **"Gioca online invece"** parte con una partita veloce nel formato della scheda Stanza privata; annullando quella ricerca si torna alla scheda Stanza privata.
  - Sotto la finestra: velo scuro senza sfocatura (come le altre finestre UI51).
  - I vecchi pannelli Crea e Entra restano nascosti nella scena (pulizia nella Fase 10).
- Rimandato, da confermare uno per uno:
  - **INVITA dalla pagina Amici apre ancora il vecchio pannello "Crea stanza"** (disegno UIV2): la pagina Amici sta sopra la Home e il pannello Modalità si aprirebbe sotto. Da decidere dove si sceglie il formato quando inviti da Amici (pannello Modalità che si apre sopra Amici, o stanza creata subito nell'ultimo formato usato).
- ASSET MANCANTI DA CREARE: nessuno (ic_link_cream, ic_paste_cream, ic_warn_cream, ic_lock_cream, ic_cards_cream già nell'inventario).

**2.46:** Fase 13, seconda parte: sala privata (`SalaPrivata`, `SalaPrivataOspite`). Stesso menu **Tools/UI51/Build Fase 13 (Dalla Home al tavolo)**, vista `UI51PrivateRoomView` (una per l'host e una per l'ospite, come i due pannelli di prima). Build pulito (0 errori); test EditMode 383 totali: 376 ok, 0 falliti, 7 saltati (Explicit). Non committata.
- ☑ **Sala:** sfondo sfocato, indietro, "Stanza privata" con "2 VS 2 · hai creato tu la stanza" (o "sei entrato con il codice"); pannello CODICE DELLA STANZA a tessere, Copia (diventa "Copiato!" per 1,5 s) e Condividi; "AL TAVOLO n / N"; posti a griglia: avatar, nome, HOST o BOT in alto a destra, anello oro per la tua squadra e blu per gli altri; posti vuoti tratteggiati col "+".
- ☑ **Bot:** come prima, li mette solo l'host: tocca un posto vuoto ("Tocca per un bot") per aggiungerlo, tocca il bot per toglierlo.
- ☑ **INVITA AMICI ONLINE:** gli amici online (Photon Chat) con INVITA, che manda il codice di questa stanza (l'invito arriva col banner della 2.43); dopo "Invitato…", e "Entrato ✓" quando l'amico è nella stanza. Senza amici online: "Nessun amico online: condividi il codice".
- ☑ **Fondo:** host: "IN ATTESA DI N GIOCATORI" finché ci sono posti vuoti, poi AVVIA PARTITA d'oro (con pop). Ospite: "Aspettiamo che Giulia avvii la partita" col pallino che pulsa.
- ☑ **Uscire dalla stanza?** dall'indietro: RESTA / Esci (Esci fa quello che faceva ESCI prima).
- Tolti da `RoomFlowV2` i campi della vecchia sala (righe, celle del codice, scritte); il vecchio disegno resta nascosto nei pannelli (Fase 10).
- Provato dal vivo su iPhone 12 (stanza 2v2 vera da host: vuota, coi bot, pronta, finestra d'uscita, Esci che chiude; ospite e 1v1 con dati finti; amici finti online) e SE (stanza 2v2 vera, bot aggiunto toccando il posto): posizioni come il mockup (pannello del codice a 84, AL TAVOLO a 286, posti a 308, amici a 524 o 422, fondo a 28, finestra a 292 contro 290).
- Non provato: un secondo telefono che entra davvero (ospite vero, "Entrato ✓" vero, passaggio dell'host), INVITA verso un amico vero.
- Scelte mie, da confermare:
  - **Codice da 5 caratteri** (il mockup ne disegna 6): è il codice che usiamo già.
  - Testo d'uscita dell'host: il mockup dice "uscendo la stanza viene chiusa e gli altri tornano alla Home", ma da noi la stanza passa a un altro giocatore; ho scritto **"Sei l'host: uscendo, la stanza passa a un altro giocatore. Se sei solo, si chiude."**
  - Posto vuoto per l'host: **"Tocca per un bot"** al posto di "Posto libero" (l'ospite vede "Posto libero"); il bot porta l'etichetta **BOT** dove l'host ha HOST.
  - Anello: **oro per la tua squadra, blu per gli altri** (il mockup alterna oro e blu per posto); nel 2v2 la prima riga è la tua squadra.
  - Amici mostrati: **al massimo 3 righe con 4 posti, 5 con 2** (quelle che stanno sopra il pulsante); se gli online sono di più, gli altri dalla pagina Amici.
  - Copia: il pulsante dice **"Copiato!"** come nel mockup, niente toast (correggo quanto scritto nella 2.44). Condividi, dove il telefono non ha la condivisione, copia l'invito e lo dice col toast.
  - Gli amici anche nella sala dell'ospite (come nel mockup): l'invito manda lo stesso codice.
- ASSET MANCANTI DA CREARE: nessuno (home_bg_blur, ic_nav_back_cream, ic_copy_cream, ic_share_cream, avatar già nell'inventario; "+", tratteggi e spunta disegnati).

**2.45:** Fase 13, prima parte: ricerca della partita (`Matchmaking`, `Matchmaking2v2`, `MatchmakingTrovato`). Menu **Tools/UI51/Build Fase 13 (Dalla Home al tavolo)** (`Assets/UI51/Editor/UI51MatchBuilder.cs`) in `MainMenu.unity`, viste `UI51MatchmakingView` + `UI51SeatCard` (la tessera del posto servirà anche alla sala privata). Build pulito (0 errori); test EditMode 383 totali: 376 ok, 0 falliti, 7 saltati (Explicit). Non committata.
- ☑ **Ricerca:** "PARTITA 1 VS 1 · ONLINE" (o 2 VS 2, TUTTI CONTRO TUTTI), "Cerco giocatori…", "Tempo di attesa 0:05"; anello d'oro che gira sul bagliore e i cinque dorsi del mazzo scelto che ondeggiano. Posti: il tuo (avatar, "Tu", livello o "Ospite") e quelli vuoti tratteggiati, col "?" che pulsa e "In ricerca…". 1v1 una riga; 2v2 "LA TUA SQUADRA" (oro) e "AVVERSARI" (blu); tutti contro tutti due righe. Chi entra prende il primo posto libero (nel 2v2 nella squadra giusta). In fondo "Se non arriva nessuno, tra N s si gioca coi bot" e "Annulla ricerca".
- ☑ **Trovata:** "Partita trovata!", "Il mazziere viene sorteggiato al tavolo", anello spento, i posti rimasti vuoti diventano "Bot N · Computer", pillola d'oro AL TAVOLO al posto di Annulla mentre si carica il tavolo.
- ☑ **Stanza privata** (creazione e ingresso): stessa schermata con la sola testata ("STANZA PRIVATA", "Creazione stanza…", "Ingresso nella stanza…"); i posti li mostra la sala d'attesa. Errore di creazione: "Connessione non riuscita" col messaggio.
- ☑ **Test della ruota del mazziere resi indipendenti:** uscendo dal Play l'Editor non ricarica il codice, quindi dopo una partita online provata in Editor la ruota restava "online" e 8 test di `K7FlowTests` fallivano. Ora ogni test riparte offline. Nel gioco vero non succedeva (tornando al menu si azzera già).
- Provato dal vivo su iPhone 12 (1v1 e 2v2, ricerca e trovata coi bot) e SE (2v2, ricerca e trovata): posizioni misurate uguali al mockup (testata a 44, posti a 400, fondo a 30, lati a 20).
- Non provato: due telefoni veri che si trovano (l'altro giocatore che entra nel posto); tutti contro tutti dal vivo (stesso codice delle due righe del 2v2).
- Scelte mie, da confermare:
  - **AL TAVOLO senza conto alla rovescia e non toccabile:** il mockup ha "AL TAVOLO · 3" cliccabile, ma da noi il tavolo si carica da solo appena la stanza è piena; un conto finto direbbe una cosa non vera.
  - Testo d'attesa: il mockup dice "Tempo stimato circa 20 secondi"; io mostro **i secondi veri all'arrivo dei bot** ("tra 26 s si gioca coi bot"). Il testo del mockup resta solo quando quel tempo non si sa.
  - I bot si chiamano **"Bot 2/3/4", sotto "Computer"** (come al tavolo).
  - Ritratti: **il tuo è il tuo avatar**; gli altri hanno il ritratto del loro posto (come al tavolo), perché l'avatar scelto da ognuno non passa in rete. Sotto il nome "Liv. N" se il giocatore ha pubblicato il suo aspetto, altrimenti "Ospite".
  - Stanza privata durante creazione o ingresso: **solo la testata**, senza posti.
  - Stanza privata che parte: titolo **"Si parte!"** (il mockup ha solo la partita veloce).
- ASSET MANCANTI DA CREARE: nessuno (home_bg_base, Bagliore_morbido, dorsi del mazzo, avatar_1/av_2/av_4/av_5 già nell'inventario; anello, tratteggi e "?" disegnati).

**2.44:** Fase 9 (`Toast`, `Connessione` + `Conn*`, `Aggiornamento`, `Manutenzione`). Build pulito (0 errori); test EditMode 383 totali: 376 ok, 0 falliti, 7 saltati (Explicit). Non committata.
- ☑ **Toast:** pillola in basso (a 96 dal fondo), sale, resta e svanisce in 2,8 s; tre tipi (spunta verde, moneta, "!" rossa). Un solo componente per tutta l'app, vale in Home e al tavolo. Usato in Amici: nome copiato, "X aggiunto agli amici", "Sei tu!", errori (prima la riga "Il tuo: ..." cambiava per 2,5 s).
- ☑ **Riconnessione in partita:** se cade la rete al tavolo, card "Riconnessione…" con anello che gira, Wi-Fi che pulsa, tentativo N di 5 e "Il tuo posto al tavolo resta tuo per Ns" (i 60 s del rientro). Dopo 5 tentativi (uno ogni 3 s almeno) card "Nessuna connessione" con RIPROVA (altri 5 tentativi finché il posto c'è) e "Esci dalla partita". Finito il posto, come prima: avviso e ritorno al menu.
- ☑ **Riconnessione in Home:** se Photon cade dopo l'ingresso, si riprova in silenzio per 3 s, poi la stessa card senza il riquadro del posto; dopo 5 tentativi "Nessuna connessione" con RIPROVA e "Continua offline".
- ☑ **Aggiornamento obbligatorio e Manutenzione:** schermate a tutto schermo del mockup. Si comandano da PlayFab Game Manager → Content → Title Data, senza build:
  - `Aggiornamento` = `{"minima":"2.50","novita":["Banner animati per il tuo profilo","..."],"link":"https://..."}`: chi ha una versione più vecchia di `minima` vede solo AGGIORNA ORA (fino a 3 novità). `link` facoltativo: senza, su Android apre la scheda Play Store dell'app; su iOS serve il link (non abbiamo ancora l'id dell'App Store).
  - `Manutenzione` = `{"fine":"2026-10-02T04:00:00Z"}` (ora UTC): fino a quell'ora si vede il conto alla rovescia ("42:18", da un'ora in su "1:42:18") con "Fine prevista alle 6:00" in ora locale. RIPROVA ricontrolla, a zero ricontrolla da sola; "Leggi le novità" apre Notizie e la schermata torna quando Notizie si chiude. Per riaprire: togliere la chiave o lasciare un'ora passata.
  - Si controlla quando l'accesso è pronto e a ogni ritorno in Home. Se PlayFab non risponde si gioca (meglio un controllo saltato che un'app bloccata per un errore di rete).
- ☑ **Corretto in `UIKeyframes` (tutte le animazioni UI51):** un'animazione senza spostamenti (pop, dissolvenze) riscriveva comunque la posizione ogni fotogramma; su un elemento appena acceso dentro un layout la bloccava su quella di prima del layout (la card "Nessuna connessione" usciva mezza fuori a sinistra). Ora la posizione si tocca solo se l'animazione la muove.
- Provato dal vivo su iPhone 12 (e Aggiornamento/Manutenzione anche su SE): toast nei tre tipi; card riconnessione (in partita e Home) a 241 dall'alto e 29 dai lati contro 240/28; card errore a 220; Aggiornamento e Manutenzione con tutti i blocchi misurati uguali al mockup (testi da 310 e 150, pannelli da 470, pulsanti a 30 dal fondo, lati 24); giro Notizie andata e ritorno; RIPROVA con il vero Title Data (chiavi assenti: la schermata sparisce). Test nuovi in `ServiceGateTests` (4).
- Non provato: rete che cade davvero (in partita e in Home, serve un telefono o staccare il Wi-Fi a mano), AGGIORNA ORA su Android, i dati veri su Title Data (le chiavi non sono state create).
- Scelte mie, da confermare:
  - Errore di connessione in Home: il mockup dice "Torna alla schermata iniziale", io ho messo **"Continua offline"** (l'allenamento coi bot funziona senza rete e il gioco online si ricollega da solo quando serve).
  - Riconnessione in partita: **5 tentativi** dentro i 60 s del posto, poi la card d'errore; RIPROVA ne fa altri 5 finché il posto c'è.
  - In Home la card compare solo **dopo 3 s** di caduta (i cali brevi, tipo il rientro dall'app in pausa, si risolvono senza mostrare niente).
  - Velo dietro le card **.65 senza sfocatura** (il mockup ha .55 con sfocatura; gli altri pannelli fanno già così).
  - I secondi del posto sono in **Nunito grassetto oro**, non in Cinzel (TMP non cambia font dentro la stessa riga senza un asset in più).
  - "Sei di nuovo in partita!" e gli altri avvisi brevi del tavolo restano quelli di oggi fino alla Fase 14 (`MomentiPartita`).
  - Il **Toast in Posta non c'è**: RISCATTA dice già sul pulsante cosa è arrivato, un toast lo ripeterebbe. La copia del codice stanza passa al toast con la Fase 13 (`SalaPrivata`).
  - **Aggiornamento e Manutenzione bloccano tutta l'app**, anche l'allenamento offline, come nel mockup (nessuna uscita). Se preferisci lasciare l'allenamento durante la manutenzione, si aggiunge un'uscita.
  - Se ci sono sia aggiornamento sia manutenzione, **vince l'aggiornamento** (si può aggiornare intanto).
  - Versioni confrontate come numeri decimali (come le numeriamo: 2.5 = 2.50, più nuova di 2.43).
  - Il ✦ delle novità non c'è in nessun font del progetto: è disegnato (stellina a 4 punte), uguale a vista.
- ASSET MANCANTI DA CREARE: nessuno (home_bg_base, logo_51, Bagliore_morbido, ic_settings_cream, ic_coin già nell'inventario UI51; Wi-Fi, spunta e ✦ disegnati a tratti come l'SVG del mockup).

**2.43:** Fase 8 completata (`AmiciVuoto`, `PostaVuota`, `InvitoRicevuto`). Build pulito (0 errori); test EditMode 379 totali: 372 ok, 0 falliti, 7 saltati (Explicit). Non committata.
- ☑ **Amici senza amici:** due cerchi (tratteggiato con la persona, oro col +), "Il tavolo è più bello in compagnia", testo e pulsante "Condividi il tuo nome" (apre la condivisione del telefono; dove non c'è, copia il nome e lo dice).
- ☑ **Posta vuota:** busta sul bagliore, "Nessun messaggio", "Qui arrivano regali degli amici, premi delle stagioni e avvisi del Team 51.", TORNA A GIOCARE (chiude la Posta).
- ☑ **Invito ricevuto:** al posto del dialog della 2.39, il banner in alto del mockup: avatar, "Giulia ti invita a giocare", "Stanza privata · 2 vs 2", secondi rimasti e barra d'oro che si accorcia, Rifiuta / ACCETTA. Scende dall'alto, sparisce da solo dopo 20 s. Un secondo invito prende il posto del primo.
- Provato dal vivo su iPhone 12 (amici e posta vuoti, invito finto): posizioni misurate uguali al mockup (disegno a 270, titolo a 390, testo a 424, pulsante a 477 contro 479); il banner sparisce a 20 s e con Rifiuta. ACCETTA non provato (serve un invito vero a due telefoni).
- Scelte mie, da confermare:
  - "Condividi il tuo ID" del mockup è diventato "Condividi il tuo **nome**": l'ID corto #51-... non esiste ancora (arriva col server), e oggi gli amici si aggiungono per nome. Anche il testo dice "Condividi il tuo nome".
  - **20 s contro 2 minuti (il ❓ qui sotto):** non si contraddicono. 20 s è quanto resta il banner per accettare (mockup). 2 minuti resta il limite tecnico per scartare inviti vecchi arrivati in ritardo. Quindi il ❓ si chiude così, se ti va bene.
  - Il mockup dice anche "2 posti liberi": tolto, perché il telefono di chi riceve non lo sa (si saprebbe solo al momento dell'invio e poi cambia).
- ASSET MANCANTI DA CREARE: nessuno (ic_person_cream, ic_share_cream, ic_mail, Bagliore_morbido, av_2 già nell'inventario UI51; il cerchio tratteggiato è disegnato a trattini).

**2.42:** si vede perché si perde coi Tre assi. Build pulito (0 errori); test EditMode 379 totali: 372 ok, 0 falliti, 7 saltati (Explicit). Non committata.
- ☑ **Momento dei Tre assi:** prima dei risultati compare il cartello "TRE ASSI!" (lo stesso del 15/30 del mazziere) con chi li ha ricevuti e chi vince ("Bot 2 ha ricevuto tre assi: vince la partita", nel 2v2 "...: la partita è vostra" / "vince la sua coppia"). Le tre carte si girano a faccia in su col bordo oro, anche nella mano del giocatore. Suono e vibrazione dell'accuso. Dopo circa 3,5 s, i risultati.
- ☑ **Tre assi in una distribuzione successiva** (2ª-6ª nel 1v1, 2ª-3ª a 4): prima si vede l'animazione delle carte nuove, poi il cartello, poi i risultati.
- Provato dal vivo nel 1v1: tre assi a me alla prima distribuzione, al Bot alla prima e al Bot alla seconda.
- Non provato: tavolo a 4 (cartello sopra le carte girate dei giocatori laterali) e due telefoni in rete.
- Resta per la Fase 14 (schermata `TreAssi`), d'accordo il 01/10: risultati e pillola del punteggio scrivono "CAPPOTTO"; la pillola lo mostra già durante la distribuzione.

**2.41:** regole dell'handoff v2/v3 (SPEC §10 punti 1-4, §11 punto 5), con il tuo via del 01/10. Build pulito (0 errori); test EditMode 378 totali: 371 ok, 0 falliti, 7 saltati (Explicit). Non committata.
- ☑ **Carta uguale obbligatoria:** se in tavola c'è la carta dello stesso valore si prende quella; niente somme né 15 (`Rules51.GetValidMoves`).
- ☑ **1v3, maggioranza semplice:** punto Carte e punto Denari a chi ne ha di più, senza le soglie 21 e 6 (che restano in 1v1 e 2v2). Pari merito: nessun punto (`PunteggioManager`).
- ☑ **Asso piglia tutto:** il codice lo faceva già (scopa solo se il tavolo resta vuoto). Aggiunti i test.
- ☑ **15/30 del mazziere:** contava già come accuso (1 o 2 punti). Ora il cartello dice "ACCUSO 15 · +1" / "ACCUSO 30 · +2" (con gli accusi spenti solo "ACCUSO 15"), con suono e vibrazione dell'accuso. Provato dal vivo.
- ☑ **Tre assi:** chi li riceve vince subito la partita, in tutte le modalità; nel 2v2 vince la coppia. Si controlla a ogni distribuzione, prima degli accusi e del 15/30 del mazziere: niente finestra Accuso, nessun accuso accettato. La matta non vale come asso. Vale anche in rete: i client vedono la distribuzione e poi la fine. Provato dal vivo nel 1v1, alla prima mano: arrivano le carte e poi subito VITTORIA.
- Test nuovi in `RulesDecisionsTests` (9). Aggiornato `MatchScoreTests`: nel 1v3 il posto 0 ora prende carte e denari (6 punti invece di 4).
- Rimandati: i tre assi non si vedevano e mancava l'animazione della distribuzione successiva (fatti nella 2.42); "CAPPOTTO" su risultati e pillola (Fase 14).
- Prossimo: scegliere la fase da cui ripartire (vedi l'elenco qui sotto).

**2.40:** avatar della Home. Non committata.
- ☑ **Avatar non più stirato:** gli sprite avatar non sono quadrati (avatar_1 354x428) e venivano schiacciati nel cerchio. Ora si ritagliano come negli altri avatar UI51 (`AvatarFrame.FitPortrait`), sia l'avatar di riserva sia quello scelto nel profilo. Provato dal vivo.
- ☑ **Tocco sull'avatar → pagina Profilo** (account e ospite), come la voce Profilo della barra in basso. Provato dal vivo da ospite.
- Prossimo: riprendere le fasi (vedi i rimandati qui sotto).

**Handoff v2/v3 (01/10):** una sola cartella, `Design/51_handoff/51_handoff/` (contenuto della v3, più `ic_shop.png` della v1). Le copie `51_handoff_unity_v2/v3` sono nel Cestino. Nuove schermate (SPEC §10-§11) assegnate alle fasi, ognuna parte col tuo via. La Pulizia resta Fase 10 perché molte note dicono già "si toglie nella Fase 10".
- ☑ **Fase 8, completamento:** `AmiciVuoto`, `PostaVuota` (stati vuoti), `InvitoRicevuto` (prende il posto del "X ti invita" della 2.39). Fatto nella 2.43.
- ☑ **Fase 9, Connessione e avvisi:** `Conn*` (già previsti), `Toast` (avvisi brevi, un solo componente per partita, amici e posta), `Aggiornamento`, `Manutenzione`. Fatto nella 2.44.
- ☐ **Fase 10, Pulizia:** invariata.
- ☑ **Fase 11, Account:** (Password dimenticata 2.52; accesso col nome utente 2.53 al posto di SceltaNome; CambiaPassword, SegnalazioneEsito e Sospensione 2.54; Lingua e Notifiche più avanti, righe L1 e L2) `SceltaNome`, `PasswordDimenticata` + `PasswordInviata`, `CambiaPassword`, `Lingua`, `Notifiche`, `SegnalazioneEsito`, `Sospensione`.
- ☑ **Fase 12, Primo avvio, tutorial e regole (§10):** (Regole e riga nelle Impostazioni 2.50, Benvenuto e partita guidata 2.51; premio del tutorial rimandato) `Benvenuto`, `TutorialPartita` + `Tutorial2/3/5` + `TutorialSalta` + `TutorialFine`, `Regole` + `RegoleAccusi` + `RegolePunteggio`, riga "Regole e tutorial" in `Impostazioni`.
- ☑ **Fase 13, Dalla Home al tavolo** (ricerca 2.45, sala privata 2.46, scheda Stanza privata ed errori 2.47): `Matchmaking` + `Matchmaking2v2` + `MatchmakingTrovato`, `SalaPrivata` + `SalaPrivataOspite`, `StanzaErrore` + `StanzaPiena` + `StanzaIniziata`.
- ◐ **Fase 14, Momenti di partita e vittorie immediate** (momenti al tavolo 2.48, spareggio, cappotto e tre assi 2.49; manca solo il timer del turno, rimandato): `MomentiPartita` (`MomentoTurno4`, `MomentoTempo`, `MomentoDisconnesso`, `MomentoScopa`, `MomentoDistribuzione`, `MomentoUltima`, `MomentoSpareggio`), `Cappotto` + `TreAssi`.
- ☑ **Fase 15, Progressione (2.63; NuovoOggetto e NuoviFrammenti non fatti, vedi 2.63):** `LivelloSu`, `Forziere` + `ForziereAperto`, `NuovoOggetto` + `NuoviFrammenti`, `Classifica`, `Trofei`.
- ❓ **Da decidere prima delle fasi:**
  - Il forziere a 3 carte con frammenti contraddice la scelta del 01/10 (forzieri aperti subito dal server, niente inventario, frammenti "idea futura").
  - Sistemi che ancora non esistono: classifica, trofei, notifiche push, lingue (oggi solo italiano). Segnalazioni e sospensioni fatte nella 2.54.
  - L'invito ricevuto dura 20 s nel mockup, 2 minuti nel codice.
  - Il premio del tutorial (+200 monete, dorso Smeraldo) va dato dal server.

## Versione 2.39

**2.39:** Fase 8, pagine Premi e Amici, e giro del server (le tue scelte del 01/10). Build pulito (0 errori); test EditMode 369 totali: 362 ok, 0 falliti, 7 saltati (Explicit). Non committata.
- ☑ **Dove sta la Posta:** dati in sola lettura del giocatore, chiave `Posta` (confermato).
- ☑ **Messaggi per tutti (scelta mia):** gli avvisi senza regali (Manutenzione) vanno nelle **Notizie**. I messaggi con regali (Benvenuto, risarcimenti) li scrivi una volta in Title Data `PostaGlobale` (stesso formato della `Posta`, con `id` obbligatorio). Il server li copia nella Posta di ogni giocatore al primo accesso dopo, una volta sola.
- ☑ **Premi (mockup `Premi` e `PremiRiscattato`):**
  - In alto: serie di accessi, "Il premio scade tra" / "Prossimo premio tra" col conto alla rovescia.
  - Giorni 1-6: oggi in oro con la pillola, i giorni passati con la spunta. Il giorno 7 è la scheda grande col forziere viola.
  - Pulsanti: RISCATTA IL PREMIO DI OGGI / TORNA A GIOCARE.
  - Riscatto: bagliore e premio che sale. Pallino rosso sul pulsante della Home se il premio di oggi è ancora da prendere.
  - Il giorno cambia a mezzanotte italiana. Saltando un giorno si riparte dal giorno 1.
  - Provato dal vivo su iPhone 12 e SE.
- ☑ **Amici:**
  - Pulsante nuovo nella colonna della Home: Amici, Posta, Notizie.
  - Due schede: Amici e Richieste (vuota per ora).
  - Ogni riga: online, in partita o "Visto 2 ore fa"; livello; pulsante Invita.
  - Aggiungi per nome (tua scelta: l'ID corto arriva col server). L'amicizia per ora è a senso unico.
  - Invita: apre un tavolo privato e manda l'invito vero con **Photon Chat**. All'amico compare "X ti invita" con ENTRA / No grazie; l'invito vale 2 minuti.
  - Provato dal vivo su iPhone 12 con amici finti. Online e inviti si provano solo con due telefoni e l'app Chat configurata (vedi sotto).
- ☑ **Giro del server (CloudScript, `Server/CloudScript/51.js`):**
  - RISCATTA e Raccogli tutto della Posta danno davvero monete, gemme e forzieri.
  - I premi di Premi li decide il server.
  - Forzieri aperti subito: verde 150-300 monete + 3-8 gemme, viola 400-800 + 10-25.
  - Monete di fine partita: vittoria 40, sconfitta 20, metà contro i bot, tetto 400 al giorno, niente agli ospiti. Si vedono nel riquadro RICOMPENSE sotto gli XP ("+40 monete", oppure "Tetto di monete di oggi raggiunto").
  - Le cifre si cambiano senza nuova versione in Title Data `Economia`. Unica eccezione: la tabella dei giorni 1-6 mostrata dal telefono va aggiornata anche a mano in `UI51RewardsView.Week`.
  - Prove del server: `node Server/CloudScript/test.js` (tutte passate).
- ☑ **Corretto durante la prova:** le statistiche di fine partita finivano sopra il riquadro RICOMPENSE (ora più alto per le monete). L'animazione d'entrata si teneva la posizione vecchia.
- **Da fare da te su PlayFab e Photon (finché non lo fai, Premi e Posta dicono "non disponibile" e gli amici risultano offline):**
  1. Game Manager → Automation → CloudScript → Revisions (Legacy): carica `Server/CloudScript/51.js` e pubblicalo (Deploy).
  2. Economy → Currencies: valute `CO` (monete) e `GE` (gemme), se non ci sono già.
  3. Title settings → Client Profile Options: spunta Statistics e Last Login time (servono per livello e "Visto ..." degli amici).
  4. Facoltativo: Title Data `PostaGlobale` (per esempio il Benvenuto) ed `Economia` (per cambiare le cifre).
  5. Dashboard Photon: crea un'app **Chat** con Custom Authentication PlayFab come quella di PUN. Metti il suo AppId in PhotonServerSettings → App Id Chat, e aggiungilo nell'add-on Photon di PlayFab.
- ASSET MANCANTI DA CREARE: nessuno.
- **Rimandati, ognuno col tuo via:**
  - Richieste di amicizia vere (servono al server).
  - ID corto #51-xxxxx.
  - Prova a due telefoni di online e inviti.
  - Bordo tratteggiato della nota di Premi (UI51Shape non lo fa).
  - Festa del giorno 7: mostra solo le monete, non le gemme.
  - Togliere le vecchie `PanelAmiciController` e `PanelPremiController` (Fase 10).

## Versione 2.38

**2.38:** Fase 8, pagina Posta (tua scelta del 01/10: Posta come prossima schermata, solo grafica). Prima ho committato in locale 2.35-2.37 (`6654de8`, senza push, `docs/art/` fuori). Build pulito (0 errori); test EditMode 366 totali: 359 ok, 0 falliti, 7 saltati (Explicit). Non committata.
- ☑ Pulsante Posta della Home: ora si tocca (prima era spento tra le "azioni da fare") e il numero rosso conta i messaggi non letti. L'ospite non lo vede, come prima.
- ☑ Pagina Posta (mockup `Posta`): indietro, titolo con "N messaggi non letti" / "Tutto letto", Raccogli tutto (solo con 2 o più messaggi da riscattare), elenco con tessera colorata per tipo (team, stagione, amico, avviso, torneo), pallino rosso, titolo, quando, anteprima, pillole degli allegati (+monete, +gemme, Forziere), "Scade tra N giorni", "Riscattato" con la spunta. In fondo "I messaggi vengono eliminati dopo 30 giorni". Senza messaggi: "Nessun messaggio per ora."
- ☑ Messaggio aperto dal basso (mockup `PostaMessaggio`): tessera, titolo, "mittente · quando", testo, ALLEGATI con i riquadri, e un pulsante: RISCATTA (oro), RISCATTATO (verde) o Chiudi (senza allegati). Aprirlo lo segna letto.
- ☑ RISCATTA e Raccogli tutto per 2 secondi dicono "Presto in arrivo" (tua scelta: i premi li darà il server nel giro dedicato).
- ☑ Da dove arrivano i messaggi (scelta mia, da confermare): dati del giocatore **in sola lettura** su PlayFab, chiave `Posta` (Game Manager → Players → il giocatore → Player Data → Read Only). Il telefono li legge e basta, quindi nessuno può regalarsi monete; domani il CloudScript scriverà lì e darà i premi. Formato: un elenco JSON, per esempio
  `[{"id":"benvenuto","tipo":"team","titolo":"Benvenuto a 51!","testo":"Grazie per esserti unito al tavolo!","data":"2026-10-01","allegati":[{"tipo":"monete","quantita":200},{"tipo":"gemme","quantita":10}]}]`
  Campi facoltativi: `da` (senza: Team 51), `scade` (data), `riscattato` (true/false); allegati `monete`, `gemme`, `forziere`. "Letto" resta sul telefono. Spariscono i messaggi più vecchi di 30 giorni e quelli scaduti non riscattati.
- ☑ Provato dal vivo su iPhone 12 e SE con i 5 messaggi del mockup (finti, su PlayFab oggi non ce ne sono): elenco, i tre tipi di messaggio aperto, RISCATTA → PRESTO IN ARRIVO, numero sul pulsante. Corretto durante la prova: titoli non scritti, righe di testo allargate su tutta l'altezza, la ✓ che Nunito e Cinzel non hanno (ora è una spunta disegnata), titolo un po' alto rispetto all'ora.
- Costruita da **Tools/UI51/Build Fase 8 (Notizie, Posta)** (`UI51SocialBuilder.cs`), vista `UI51MailView` + `UI51MailItem`, servizio `MailService`, test `MailServiceTests`. La vecchia `PanelPostaController` (vecchia HUD) resta: si toglie nella Fase 10.
- ASSET MANCANTI DA CREARE: nessuno (ic_mail, chest_purple, av_2, ic_warn_cream, medal_trophy, ic_coin, ic_gem già nell'inventario UI51).
- Da decidere, ognuno col tuo via:
  - Va bene leggere la Posta dai dati in sola lettura del giocatore (chiave `Posta`), o preferisci un altro posto?
  - Messaggi per tutti (Benvenuto, Manutenzione): oggi vanno scritti giocatore per giocatore; nel giro del server il CloudScript li manderà a tutti. In alternativa usi le Notizie.
  - Fase 8, prossima schermata: Premi (solo grafica) oppure Amici.
  - Giro del server (CloudScript): RISCATTA, Raccogli tutto, premi di Premi e di fine partita.

## Versione 2.37

**2.37:** chiusi i due mockup rimasti della Fase 5 e iniziata la Fase 8 con la pagina Notizie (tue scelte del 01/10: prima Notizie; Posta e Premi solo grafica, il server in un giro dedicato). Build pulito (0 errori); test EditMode 361 totali: 354 ok, 0 falliti, 7 saltati (Explicit). Non committata (2.35, 2.36 e 2.37 insieme).
- ☑ `PartitaTavoloPieno` (8 carte in tavola) e `PartitaBanner` (banner animato aurora): già fatti dalla Fase 5, provati dal vivo su iPhone 12. 8 carte su 2 righe da 4, larghe 60 con 10 di spazio come nel mockup; il mio banner aurora si muove.
- ☑ Notizie (mockup `Notizie` e `NotizieArticolo`): pulsante Notizie sotto Posta nella Home, col pallino rosso se ci sono notizie non ancora viste (come Posta, l'ospite non lo vede). Pagina con fondo sfocato, carosello delle notizie in evidenza (frecce, puntini, dissolvenza), ULTIME NOTIZIE con miniatura, etichetta colorata, data e pallino "nuova"; il tocco apre l'articolo dal basso con testata illustrata, testo che scorre e pulsante d'oro. GIOCA porta alla Home, COLLEZIONE e PROFILO alla loro pagina, gli altri pulsanti chiudono l'articolo. Senza notizie: "Nessuna notizia per ora".
- ☑ Le notizie vengono da PlayFab (Game Manager → Content → Title News), come prima. In testa al testo della notizia si possono scrivere queste righe, poi una riga vuota e i paragrafi (separati da una riga vuota):
  - `etichetta: torneo` (NOVITÀ, TORNEO, COLLEZIONE, AGGIORNAMENTO, EVENTO, AVVISO, CONSIGLI; decide colori, sfondo e icona)
  - `sottotitolo: Sabato e domenica · premi in forzieri`
  - `evidenza: sì` (va nel carosello, al massimo 3; se nessuna lo è, ci va la più recente)
  - `pulsante: iscriviti` (senza: HO CAPITO)
- ☑ Provato dal vivo su iPhone 12 e SE con 7 notizie finte uguali a quelle del mockup (su PlayFab oggi non ce ne sono). Corretto durante la prova: la foto della testata usciva sotto nell'articolo; il foglio dell'articolo e il pulsante ora seguono la larghezza utile (431 su iPhone 12); gli aloni sono sfumature lisce come nel CSS (il bagliore con i raggi non andava bene).
- Costruita da **Tools/UI51/Build Fase 8 (Notizie)** (`Assets/UI51/Editor/UI51SocialBuilder.cs`), vista `UI51NewsView` + `UI51NewsItem`. La vecchia pagina Novità (`NewsV2`) resta in scena, non più raggiungibile: si toglie nella Fase 10.
- ASSET MANCANTI DA CREARE: nessuno (icone, carte e sfondi già nell'inventario UI51).
- Da decidere, ognuno col tuo via:
  - Fase 8, prossima schermata: Posta o Premi (solo grafica, Riscatta "presto in arrivo") oppure Amici.
  - Notizie: immagine diversa per ogni notizia (oggi la decide l'etichetta: coppa per TORNEO, dorso per COLLEZIONE).
  - Notizie: scorrere il carosello col dito (il mockup ha solo frecce e puntini, così com'è ora).
  - Scrivere le prime notizie vere su PlayFab (lo fai tu da Game Manager, col formato qui sopra).

## Versione 2.36

**2.36:** medaglie del profilo rapido e UI51 Fase 7 (fine smazzata e fine partita), con le tue risposte del 01/10. Build pulito (0 errori); test EditMode 357 totali: 350 ok, 0 falliti, 7 saltati (Explicit). Revisione chiusa con 1 correzione: online, per chi non siede al posto 0, i numeri "22 – 18" sotto le righe seguivano l'ordine dei posti invece delle colonne (io per primo). Non committata (2.35 e 2.36 insieme).
- ☑ Medaglie del profilo rapido (tue regole): trofeo 10 vittorie, sole 100 partite, bastoni 100 scope, spade livello 10. Calcolate dalle statistiche già pubblicate, quindi uguali per tutti; la fila compare solo se ce n'è almeno una.
- ☑ Grafiche nuove importate in `Assets/UI51/Art/Common`: `crown_gold` (corona del vincitore), `rays_conic` (raggi che girano dietro al titolo), `ribbon_gray` (nastro della sconfitta). Vanno bene così; i raggi li tengo molto tenui come nel mockup (oro al 16%, grigio-blu all'8% nella sconfitta).
- ☑ Fine smazzata (mockup `FineSmazzata`, 1v1 / 2v2 / tutti contro tutti): modo e mano, teste di colonna con avatar (anello oro io o la mia coppia, blu gli altri; coppie con due avatar accavallati, "Noi" e "Loro"), 8 righe con icona, dettaglio e pillole dei punti (Carte, Denari, Settebello con chi l'ha preso, Primiera, Grande, Piccola con le carte, Scope, Accusi con chi li ha dichiarati), IN QUESTA MANO, corsa al 51 con le barre che si riempiono da prima a dopo la smazzata e la nota "N punti alla vittoria" ("in testa: …" a quattro), PROSSIMA SMAZZATA e sotto "Si riparte da sola tra N secondi". Animazioni del mockup (righe da sinistra, pillole che scoppiano, totali e corsa che salgono). Fondo: il fondo della Home sfocato e scurito, come nel mockup (non più la foto del tavolo).
- ☑ Conto alla rovescia 10 secondi (era 8), anche offline contro i bot. Online lo vede solo chi fa proseguire; gli altri leggono ATTENDI L'HOST.
- ☑ Fine partita (mockup `FinePartita`): nastro VITTORIA (oro) o SCONFITTA (grigio), raggi, coriandoli solo a chi vince; a due la sfida coi punteggi, corona e alone al vincitore e l'altro spento; a quattro la classifica con medaglie oro/argento/bronzo e pari merito condivisi (1, 2, 2, 4); RICOMPENSE con +XP, barra e livello (ospite: "Registrati per guadagnare XP"); statistiche della partita Scope, Accusi, Settebelli (le mie, anche nel 2 contro 2); RIVINCITA e Torna alla Home. Nel tutti contro tutti RIVINCITA e VITTORIA/SCONFITTA come hai chiesto (non "NUOVA PARTITA" e "2° POSTO").
- ☑ Provato dal vivo su iPhone 12 e SE: 1v1, tutti contro tutti e 2v2, fine smazzata e fine partita (vittoria e sconfitta), con una smazzata preparata a mano (non giocata fino in fondo). Corretto durante la prova: su iPhone 12 l'area utile è larga 431, non 390; colonne, corsa e pulsanti ora seguono la larghezza come i pannelli.
- ☐ Ricompense oltre agli XP (monete, forzieri): il posto c'è (`UI51RewardsExtra`, spento), le statistiche per ora salgono subito sotto alle ricompense. Da fare presto (tua richiesta).
- ❓ Regola "bonus cappotto" (se attivata nelle regole): il totale IN QUESTA MANO lo comprende ma nessuna riga lo mostra. Con la regola spenta (predefinita) non succede.
- ⏸ Da provare online con due telefoni insieme al resto: ATTENDI L'HOST, conto solo per l'host, passaggio dell'host.
- Costruito da **Tools/UI51/Build Fase 5 (Tavolo 1v1)** (`UI51ResultsBuilder.cs`, parte di `UI51TableBuilder`). Il vecchio aspetto resta in scena spento. Non rilanciare `Tools/UIV2/Build Match Results` né "Build Round Results Blur".

## Versione 2.35

**2.35:** le tue risposte del 01/10 sui rimandati della 2.34. La 2.34 è committata in locale (`a9dabf8`, senza push, `docs/art/` lasciata fuori). Build pulito (0 errori); test EditMode 351 totali: 344 ok, 0 falliti, 7 saltati (Explicit). Non committata.
- ☑ 1v1: le carte accusate dell'avversario sono carte normali, senza bordo d'oro e senza bagliore (scelta tua). A 4 giocatori il bordo resta.
- ☑ Visore "Carte accusate da X": finito il ventaglio la matta (7 di coppe) si gira e diventa la carta che vale per l'accuso (stessa regola del tavolo: solo con le 3 carte in mano), passa davanti alle altre e prende un bordo d'oro che pulsa e il cartellino "MATTA". Con la grafica ridotta niente pulsazione. Provato dal vivo su iPhone 12 (Decino con matta: 7 di coppe -> 5 di coppe).
- ☑ Visore delle accusate: carte in fila affiancate (passo 104, 8 di stacco) invece del ventaglio, così si leggono tutte (tua richiesta). Le scope restano a ventaglio.
- ☑ Carta che si alzava dopo una giocata: nell'Editor (anche nel Simulator) il puntatore è un mouse, resta dove hai cliccato e la carta vicina, scivolando nel posto libero, ci finiva sotto e l'hover la sollevava. Ora l'hover solleva una carta solo quando è il puntatore a muoversi; una carta che arriva sotto un puntatore fermo resta giù (vale anche a inizio smazzata). Sul telefono già non succedeva: senza dito sullo schermo non c'è hover. Provato dal vivo: dito fermo dopo la giocata, la vicina ci scivola sotto e resta giù; il dito si sposta di 8 pixel e si solleva.
- ☑ 1v1: il tocco sulle carte accusate dell'avversario apre il visore (deciso insieme: mostra "+N punti" e la matta trasformata).
- ☑ Nomi lunghi nei banner verticali dei posti laterali (larghi 58): la scritta scende da 10 fino a 8, poi i puntini (tua scelta); il nome intero sarà nel profilo rapido. Misurato: "Marco_93", "Tore_NA", "Giocatore 3" restano a 10, "GiocatoreNapo" 8,2, "Ospite ABCD" 9,2; solo un nome da 17 lettere ("Francesco_Marotta") prende i puntini.
- ☑ A 4 giocatori le carte accusate di chi sta in alto restano dritte (ok tuo). Vecchia roulette a 4: si toglie nella Fase 10 (ok tuo). `PartitaTavoloPieno` e `PartitaBanner`: restano da fare (ok tuo).
- ⏸ Prova online con due telefoni: la fai più avanti (stessa build per tutti).
- ☑ Scheda del profilo rapido (`PartitaProfilo`, `Partita4Profilo`, `PartitaMioProfilo`), risposte tue del 01/10: tocco sul banner di un giocatore (il mio: avatar e nome, 130x50 a sinistra; spento mentre è aperta la scelta emoticon) → scheda da 300 col suo banner, avatar e cornice, livello e titolo (1-4 Principiante, 5-9 Apprendista, 10-14 Esperto, 15-24 Maestro, 25+ Gran Maestro), squadra; statistiche partite, % vittorie, scope pubblicate online come livello e cornice, quindi visibili anche per gli altri; Aggiungi amico / Richiesta inviata, Silenzia emoticon (le sue emoticon non compaiono più), Segnala giocatore per chi ha un account (da ospite niente pulsanti); per me la barra XP. Bot e ospiti: solo nome, ritratto e squadra, centrati. Dove le scope si sovrappongono al banner vince il visore delle scope; senza scope il tocco passa al profilo. Si chiude col velo, la X, a fine smazzata e all'apertura dell'accuso. Le scope ora contano davvero nel profilo cloud (prima `TotalScope` non veniva mai scritto). Costruita da `UI51TableBuilder.BuildQuickProfile` (stesso menu Fase 5). Provato dal vivo su iPhone 12 e SE, 1v1 e 4 giocatori (posizioni del mockup: io 300, in alto 140/150, ai lati 220), tocchi verificati con raycast.
- ☑ Medaglie del profilo rapido: regole tue fatte nella 2.36 (vedi sopra).
- ☑ Fase 7 (`FineSmazzata*`, `FinePartita*`): fatta nella 2.36 con le tue risposte (vedi sopra).
- ☐ Ricompense a fine partita oltre agli XP: vedi 2.36.

## Versione 2.34

**2.34:** UI51 Fase 6 (tavolo a 4 giocatori: mockup `Partita4` nella disposizione compatta, e Sorteggio a 4) costruita e provata in Unity. La costruisce lo stesso menu della Fase 5, **Tools/UI51/Build Fase 5 (Tavolo 1v1)** (`Assets/UI51/Editor/UI51TableBuilder.cs`), dentro i suoi passi: rilanciarlo rifà tutte e due le fasi. Build pulito (0 errori); test EditMode 349 totali: 342 ok, 0 falliti, 7 saltati (Explicit). Committata in locale il 01/10 (`a9dabf8`).
- ☑ S1 pillola del punteggio a 4: tutti contro tutti "TU · A 51 · " e i 3 avversari nell'ordine dei posti, nomi tagliati a 5 lettere; nel 2 contro 2 "NOI" e "LORO".
- ☑ S2 posti laterali: banner verticali 64x100 del mockup a sinistra e a destra, scope coricate che sporgono verso il tavolo, gettone "M" del mazziere, dorsi piccoli coricati. Toccando le scope di un laterale si apre il visore, come per il posto in alto.
- ☑ S3 altezza dei posti laterali: si mettono tra il mio banner e quello in alto, in modo che la fascia delle carte in tavola resti alta 190 come nel mockup, senza salire oltre il posto del mockup né toccare il cuscino del mazzo.
- ☑ S4 tavolo a 4: fascia e griglia delle carte in tavola tra i posti laterali e la mia mano. Su iPhone SE la mia mano passa da 143 a 98 di altezza solo quando la fascia del tavolo resterebbe sotto 150 (anche quando non ci sarebbe spazio affatto).
- ☑ S5 compagno nel 2 contro 2: bot e ospiti con anello d'oro per il compagno e blu per gli avversari; i giocatori con un account tengono la loro cornice. Sotto al nome del compagno c'è "Compagno" al posto del livello; il suo banner in alto è più largo di 16 perché "Compagno" e il chip delle carte prese ci stiano interi (nel mockup il chip sborda), e resta centrato con gettone e scope alla solita distanza.
- ☑ S6 carte accusate: le carte accusate degli altri hanno un bordo d'oro netto (il bagliore morbido su carte così piccole non si vedeva). Toccandole si apre il visore "Carte accusate da X" con i chip "Accuso" e "+N punti". Nel 1v1 le carte accusate dell'avversario si vedono grandi (alte 80, passo 60) e dritte come nel mockup, con bordo e bagliore; toccate aprono lo stesso visore.
- ☑ S7 Sorteggio a 4: la ruota del mockup con 4 spicchi, la croce d'oro e i 4 avatar coi nomi sempre dritti; in alto "PARTITA 2 VS 2" o "TUTTI CONTRO TUTTI". Gira solo a inizio partita e alla rivincita, come nel 1v1 (scelta tua): la vecchia roulette a ogni smazzata non parte più. Sotto al mazziere: "Distribuisce X: inizia Y", oppure "giochi tu per primo", oppure "Distribuisci tu: gli altri giocano prima di te".
- Provato nel Simulator su iPhone 12 e SE: 2 contro 2, tutti contro tutti, e il 1v1 per controllare che non sia cambiato niente.
- Revisione del codice chiusa con 2 correzioni: una carta passata da una mano accusata al tavolo o alla mia mano non risponde più al tocco con la vibrazione; su schermi così bassi che il tavolo non avrebbe spazio, la mia mano si rimpicciolisce invece di restare grande.
- ASSET MANCANTI DA CREARE: nessuno.
- Rimandati o da decidere, ognuno col tuo via:
  - Scheda del profilo rapido (`Partita4Profilo`): ancora rimandata, come nella Fase 5.
  - 1v1: il bordo delle carte accusate è pieno, senza lo stacco sottile tra carta e bordo che c'è nel mockup.
  - Il visore delle carte accusate mostra la matta come carta normale, non trasformata.
  - A 4 giocatori, carte accusate di chi sta in alto: le ho abbassate di 6 e messe dritte (leggibili da te), mentre ai lati restano coricate. Non c'è un mockup: scelta mia.
  - Resta a te: prova online con due telefoni con la stessa versione 2.34 (ora anche a 4 la ruota gira solo a inizio partita).
  - Vecchia roulette a 4 (`Design`, nuvolette) ancora nella scena: si toglie nella Fase 10 (pulizia).
  - Della Fase 5 restano da fare `PartitaTavoloPieno` e `PartitaBanner` (provati nella 2.37: erano già fatti).
- Prossimo passo: Fase 7 (`FineSmazzata*`, `FinePartita*`), poi Fase 8 (Amici, Posta, Notizie, Premi), Fase 9 (`Conn*`), Fase 10 (pulizia). Ognuna parte col tuo via.

**2.33:** le correzioni chieste il 01/10. Build pulito (0 errori); test EditMode 345 totali: 338 ok, 0 falliti, 7 saltati (Explicit). Non committata, come la 2.32: serve il tuo via.
- Pannelli: toccando dentro un pannello (scelta dell'icona, modalità, mazzo, stanza privata...) non si chiude più; si chiude toccando fuori o sulla X. Il foglio non prendeva il tocco, che passava al velo dietro. Provati dal vivo tutti i pannelli del menu che si possono aprire (Modalità, Mazzo, Editor del profilo, Impostazioni, Termini, Elimina account, Crea/Entra stanza) con 25 tocchi dentro ciascuno: nessuno arriva al velo. Al tavolo opzioni e abbandono erano già a posto. Restano nella scena 3 pannelli vecchi che nessun pulsante apre più (`SettingsModal`, `ModePanelContainer`, `DeckPanelRoot`).
- Barra di caricamento: sale solo in avanti e a velocità costante, non si deforma più (estremità sempre tonde, il riflesso resta dentro) e la schermata si chiude solo a barra piena.
- Carta che pulsava "grande piccola": succedeva quando l'ultimo tocco (per esempio "Continua" o "AL TAVOLO") restava sul bordo basso di una carta della mano. L'hover la solleva, lei sfuggiva al punto toccato, riscendeva e ripartiva, senza fine. Ora resta sollevata finché il puntatore è sul suo posto; sui telefoni l'hover c'è solo con il dito sullo schermo, quindi a inizio smazzata nessuna carta resta su. Provato dal vivo: prima oscillava di continuo, ora sale una volta e sta ferma, e scende quando il puntatore va via. Test: `HoverKeepsTheCardWhileThePointerIsOnItsRestPlace`.
- Mazzo: toccandolo più volte il medaglione resta aperto (sparisce 2,4 s dopo l'ultimo tocco) senza ripartire con sollevamento e conteggio.
- Carta giocata: le carte in tavola ora si spostano mentre la carta vola, così atterra nel suo posto libero invece che a metà sopra quella centrale; dopo una presa le carte rimaste chiudono i buchi scivolando. Prima il tavolo si sistemava di scatto dopo l'atterraggio e, con l'ultima carta della mano, solo alla distribuzione seguente. Provato con 34 giocate automatiche: nessuna carta atterrata sopra un'altra, nessuno scatto del tavolo.
- Emblema del sole: il tuo `Sun_fix.png` al posto di `sun_emblem`, al centro della ruota del Sorteggio e sul tavolo. Il pezzo di nastro in basso non c'è più.
- ASSET MANCANTI DA CREARE: nessuno.
- Prossimo passo: i mockup che mancano, a partire dalla Fase 6 (tavolo a 4 giocatori). Il tuo via è arrivato il 01/10 ("non ci fermiamo").

**2.32:** i quattro punti a cui hai detto sì il 30/09. Build pulito (0 errori); test EditMode 344 totali: 337 ok, 0 falliti, 7 saltati (Explicit). Non committata: serve il tuo via.
- Soglia di trascinamento: in tutte le scene ora vale circa 1,6 mm di dito invece di 10 pixel fissi (29 pixel su iPhone 12, 20 su iPhone SE, 10 nell'Editor). Un tocco che rotola appena non diventa più un trascinamento, quindi i pulsanti dentro le liste che scorrono partono. Si vede solo su un telefono vero. Test: `DragThresholdIsAboutOnePointSixMillimetres`.
- Photon: le stanze ora sono separate per versione dell'app (2.32 gioca solo con 2.32). Prima la versione restava vuota e telefoni con build diverse finivano insieme, perdendo per esempio gli accusi. Tolte le vecchie righe che provavano a impostarla senza effetto.
- Fase 4 rilanciata: i 4 interruttori delle Impostazioni del menu (Effetti, Musica, Vibrazione, Grafica ridotta) ora prendono il tocco su tutta la loro area. Provato dal vivo; nella scena cambiano solo quelli e il numero di versione.
- Worktree vecchio `.claude/worktrees/focused-heisenberg-90035f` cancellato (le correzioni degli accusi erano già tutte nel commit della Fase 5). Resta una cartella vuota che Windows tiene occupata: si può cancellare a mano.
- Resta a te: prova online con due telefoni con la STESSA versione (accusi e ruota del Sorteggio). Con la 2.32 due versioni diverse non si trovano più: installa la stessa build su entrambi.

**2.31:** Fase 5 (Tavolo 1v1) finita: fatti anche S9 (opzioni e abbandono) e S10 (Sorteggio). Build pulito (0 errori); test EditMode 343 totali: 336 ok, 0 falliti, 7 saltati (Explicit). Committata in locale il 30/09 (commit 53bc0ac, nessun push).
- S9 e S10: dettagli nel blocco della Fase 5 qui sotto.
- ASSET MANCANTI DA CREARE: emblema del sole (`sun_emblem`) senza il pezzo di nastro che si vede in basso, al centro della ruota del Sorteggio. Arrivato il 01/10 (`Sun_fix.png`), messo nella 2.33.

**2.30:** chiusi i 13 punti rimasti aperti dalla Fase 4. Build pulito (0 errori); test EditMode 316 totali: 309 ok, 0 falliti, 7 saltati (Explicit). Committata in locale il 29/09 insieme alla 2.29 (commit 1457601, nessun push).
- Fatto e provato nel Simulator su iPhone 12 e SE:
  - Emoticon: il tocco in griglia toglie un'emoticon già in uso e aggiunge in coda una libera. A slot pieni il tocco su una libera non cambia niente. Testo d'aiuto: "Tocca un'emoticon qui sotto per aggiungerla" oppure "Slot pieni: tocca un'emoticon per toglierla". Non è un aggancio opzionale: vale anche per la schermata classica (`CollectionCosmeticsV2.Toggle`). Test: `EmoticonToggleRemovesOneInUseAndAppendsAFreeOne`.
  - Profilo ospite: il testo ora dice "Mazzi, emoticon e accusi da sbloccare". Righe dei vantaggi alte 47 (erano 55): su iPhone SE CREA UN ACCOUNT si vede intero senza scorrere.
  - Salvataggio dell'aspetto: una sola scrittura per avatar, cornice e banner (`ProfileService.SetCosmetics`), con l'esito a schermo. Durante l'invio SALVA è attenuato e il foglio non si chiude. Se va bene: "Aspetto salvato" e chiusura. Se fallisce: "Non salvato: le tue scelte sono ancora qui. Riprova." e il foglio resta aperto. Dopo 15 secondi senza risposta si può riprovare. Test: `TheLookIsSavedInOneWriteAndTheCacheOnlyAdoptsWhatTheCloudAccepted`.
  - Dialogo Elimina account: se la tastiera copre i pulsanti, la finestra sale quanto basta (`DeleteAccountModalV2.KeyboardLift`, 4 test). Nell'Editor la tastiera non esiste, quindi lì non cambia niente.
- Revisione del codice (3 aree): un solo difetto, corretto. Toccando Elimina o Annulla la tastiera si chiude e la finestra sarebbe scesa sotto il dito, facendo perdere il tocco: ora non scende finché c'è un dito sullo schermo. Si vede solo su un telefono vero.
- Decisioni prese (restano così finché non le cambi):
  - Notifiche: riga nascosta. L'app non manda notifiche.
  - "Cambia password": nascosta finché su PlayFab non c'è il modello di e-mail per il recupero (H5). Intanto si usa "Password dimenticata?" dall'Accesso.
  - Lingua: riga informativa "Italiano", non si tocca. C'è una sola lingua.
  - Riga "prossimo sblocco" nel Profilo: non costruita. L'unico sblocco a livello che esiste (banner Porpora al livello 10) si vede già nell'editor con il lucchetto.
  - Collezione: "Prossimi sblocchi", "Nuova emoticon", monete e gemme restano nascosti finché non esistono negozio, missioni e valute.
  - Sfocatura dietro i pannelli del menu: resta solo il velo scuro. Al tavolo la sfocatura c'è già e la usano i pannelli della Fase 5.
  - Cornice e banner scelti visibili al tavolo (D2): diventa il primo passo della Fase 5, con i banner nuovi.
- Restano a te (non si possono fare dall'Editor):
  - Prova con un accesso vero: Profilo con account, editor, SALVA (salvataggio reale su PlayFab), registrazione dall'app.
  - Prova su un telefono vero: tastiera nel dialogo Elimina account.
- Limite noto del salvataggio: se la risposta arriva dopo i 15 secondi e nel frattempo si è premuto di nuovo SALVA, il messaggio può non corrispondere all'ultima richiesta. L'aspetto salvato resta comunque quello accettato da PlayFab.
- ASSET MANCANTI DA CREARE: nessuno.
- Fase 5 (Tavolo 1v1): via ricevuto il 29/09. Scelte tue: pulsante ACCUSA sempre visibile (come oggi, grafica nuova); barra in alto col solo punteggio (carte rimaste toccando il mazzo); Sorteggio rifatto come ultimo passo; scheda profilo rapido rimandata.
- Fase 5, ordine dei passi: S0 preparazione, S1 barra in alto, S2 banner (con cornice e banner veri), S2b aspetto dell'avversario online, S3 carte, S4 sfondo e mazzo, S5 emoticon, S6 accuso, S7 scope, S8 scelta della presa, S9 opzioni e abbandono, S10 Sorteggio.
- Fase 5, stato al 30/09 sera: tutti i passi fatti, committata in locale con la 2.31. Menu **Tools/UI51/Build Fase 5 (Tavolo 1v1)** (`Assets/UI51/Editor/UI51TableBuilder.cs`) in `GameScene.unity`. Test EditMode 343 totali: 336 ok, 0 falliti, 7 saltati (Explicit).
  - ☑ S0 preparazione: emblema del sole importato, builder del tavolo creato.
  - ☑ S1 barra in alto: Abbandona, pillola del punteggio ("TU · A 51 · avversario"), Opzioni. Provata su iPhone 12 e SE. Revisione fatta: corretto il nome dell'avversario online, che poteva contenere tag di formattazione.
  - ☑ S2 banner dei giocatori: banner nuovi per me e per l'avversario in alto, scope dietro al banner (4 carte e poi "+N"), gettone "M" del mazziere, cornice, banner e livello veri per chi ha un account. Provato su iPhone 12 e SE. Revisione fatta: un nome lungo ora finisce in "..." invece di schiacciare l'avatar; un nome online vuoto diventa "Giocatore N"; nelle partite a 4 tutti contro tutti l'avversario in alto mostra i suoi punti ("34 pt") al posto del livello, finché la pillola a 4 punteggi della Fase 6 non li mostra tutti.
  - ☑ S2b aspetto dell'avversario online: chi ha un account manda cornice, banner e livello agli altri al tavolo (di nuovo a ogni ingresso in stanza, anche al rientro); ospiti e bot restano col banner avversario di sempre. "Liv. 100" con due cifre di carte prese ora entra intero. Scritto e provato in Unity, revisione chiusa (niente errore Photon uscendo dalla stanza; il tuo livello al tavolo e' come in Home anche se il profilo online non arriva). Da provare con due telefoni veri (serve un account vero, tocca a te).
  - ☑ S3 carte e posizioni definitive dei banner: le mie 3 carte in fila sopra al mio banner, i dorsi dell'avversario in fila sotto al suo; le carte in tavola in una griglia nella fascia libera tra le due mani (piu' colonne quando lo spazio in altezza non basta, fino a 12 carte anche su iPhone SE); la carta scelta sul tavolo sale di poco; le carte prese volano verso la pastiglia delle prese del banner. Provato su iPhone 12 e SE (1, 4, 5, 8, 9 e 12 carte), controllo veloce anche a 4 giocatori e 2 contro 2. Revisione chiusa con 2 correzioni: piu' spazio tra le righe del tavolo, cosi' due carte scelte una sopra l'altra non si toccano piu'; una carta che passa dal tavolo a una mano (rientro online, rivincita) non resta sollevata.
  - ☑ S4 sfondo, tavolo e mazzo: sfondo della Home sfocato; tavolo nuovo disegnato dal gioco (legno a tre toni, filo d'oro, feltro verde piu' chiaro al centro, sole all'8%) che segue i banner, quindi i dorsi dell'avversario e la mia mano restano dentro al bordo anche su iPhone SE; cuscino rosso col mazzo in alto a sinistra, che sparisce quando le carte sono finite. Toccando il mazzo si solleva e compare per 2,4 secondi il medaglione con le carte rimaste. Le carte ora partono dal mazzo quando si distribuisce, in tutte le modalita' (scelta mia: se preferisci che partano dal posto del mazziere come prima e' una riga). Provato su iPhone 12 e SE, controllo veloce a 4 giocatori. Revisione chiusa con 1 correzione: con l'accuso del mazziere (15/30) le carte da scoprire non restano piu' ferme e scoperte sopra al mazzo prima di volare.
  - ☑ S5 emoticon: pulsante Emoji tondo come nel mockup; toccandolo, le emoticon scelte in Collezione (fino a 3) compaiono in fila dentro al mio banner al posto di nome e carte prese, quindi non coprono piu' la mano ne' il gettone "M". Si chiude inviando, ritoccando Emoji, toccando fuori o dopo 3,5 secondi; senza emoticon scelte dice "Nessuna emoticon - Scegline in Collezione". La mia emoticon sale dal banner in una nuvoletta crema e sparisce dopo 2,6 secondi (una nuova riparte da capo); quella dell'avversario prende per 3,2 secondi il posto del suo avatar. Nei 4 giocatori lo stesso per chi sta in alto, i due laterali tengono le nuvolette di prima fino alla Fase 6. Online niente di nuovo da mandare. Provato su iPhone 12 e SE e a 4 giocatori. Revisione del piano e del codice chiusa: area di tocco di Emoji sicura (con lo stesso effetto di pressione degli altri tondi), fila centrata anche con 1-2 emoticon, emoticon dell'avversario un po' piu' piccola cosi' il vapore di "arrabbiato" non esce dall'anello.
  - ☑ S6 accuso: il pulsante ACCUSA e' il medaglione d'oro col pugno del mockup, sempre visibile. Quando si apre la finestra dell'accuso pulsa un alone d'oro, passa un riflesso, l'anello del tempo si svuota, i secondi stanno in un pallino sull'anello e fra la mano e il banner compare "Hai un accuso? Tocca il pugno". Toccato con un accuso smette di chiamare; senza accuso trema e fa il suono d'errore, come prima. L'accuso di chiunque (io, bot, avversario online) ora e' quello del mockup: un pugno solo cade al centro del tavolo con due onde d'urto e il bagliore, il tavolo trema, sotto c'e' CIRULLA o DECINO con "chi · +punti" (i punti veri: +10 per il Decino). Le carte non saltano piu'. Nel 1v1 le carte dell'avversario si girano nella sua mano (ora anche nelle mani successive alla prima); nei 4 giocatori, per l'accuso di un altro, le sue 3 carte compaiono sotto la scritta con il bordo d'oro e la matta si trasforma. Sui telefoni bassi (SE) tutto un po' piu' piccolo per non coprire la mano. Provato su iPhone 12 e SE e a 4 giocatori. Revisione chiusa con 1 correzione: se due giocatori accusano insieme (fine finestra, o online) si vedono uno dopo l'altro, ognuno col suo nome, punti e carte, e il gioco riparte dopo l'ultimo (prima il secondo cancellava il primo).
  - ☑ Accusi online (bug gia' esistente trovato dalla revisione di S6), corretto il 30/09 in un worktree a parte e poi portato qui e provato in Unity: lo 15/30 del mazziere conta solo nel totale di smazzata, quindi non scopre piu' la sua mano ne' toglie punti al suo Cirulla/Decino sugli altri telefoni; chi riceve un accuso si allinea al totale del dichiarante e conta anche l'accuso per il bonus XP (prima si perdeva se lo dichiarava il Master in automatico); un solo accuso per giocatore per mano, anche se un rientro riapre la finestra (e un secondo tocco su un accuso gia' contato non da' piu' la scossa d'errore); un accuso arrivato a un telefono ancora alla mano prima aspetta la sua ultima mossa (prima poteva essere pagato due volte a tutti). Un Cirulla da 2 punti (moltiplicatore 0,5) ora scopre le carte. Due revisioni avversarie chiuse, test nuovi in `NetworkAccusoSyncTests`. Da provare con due telefoni con la STESSA versione (il messaggio dell'accuso e' cambiato: una versione vecchia nella stessa stanza perde gli accusi).
  - ☑ S7 scope: toccando le scope sotto al mio banner o a quello dell'avversario in alto si apre il visore del mockup: velo scuro, "Le tue scope" (o "Le scope di ..."), le carte a ventaglio che entrano una dopo l'altra, due chip ("12 carte prese", "6 scope") e "Tocca ovunque per chiudere". Si chiude toccando ovunque, a fine smazzata e quando si apre la finestra dell'accuso; il gioco non si ferma. Oltre 6 scope le carte si stringono nella stessa larghezza di 6. Senza scope il tocco non fa nulla (niente suono ne' vibrazione). Nei 4 giocatori vale per il posto in alto; i banner laterali aspettano la Fase 6. Provato su iPhone 12 e SE (misure uguali al mockup), 9 scope, fine smazzata e 4 giocatori. Corretto anche un difetto dei telefoni veri: un tocco su un pannello sopra alle carte poteva giocare la carta sotto (il controllo usava i dati del dito del fotogramma prima); ora il controllo guarda il punto toccato in quel momento. Da provare su un telefono vero. Revisione chiusa con 1 correzione: toccando le scope subito dopo l'apertura della finestra dell'accuso (primo quinto di secondo) il visore non si apriva; ora si apre e resta aperto, mentre un visore gia' aperto si chiude quando la finestra si apre. Provato dal vivo.
  - ☑ S8 scelta della presa: quando la carta puo' prendere in piu' modi sale dal basso il vassoio del mockup (carta giocata, freccia, "SCEGLI LA PRESA", X; sotto le prese in fila, ognuna col suo colore oro, verde acqua, rosa, viola, con numero, mini carte e chip "+N", denari, SCOPA). Sul tavolo le carte si alzano con un anello del colore della prima presa e un numero per ogni presa che le contiene; toccare una carta del tavolo gioca la sua prima presa. Con tante prese la fila scende fino al 75% e poi scorre col dito (dalla quinta i colori ripartono, i numeri no). Un secondo tocco sulla stessa carta non fa rimbalzare il vassoio, il doppio tocco non sceglie nulla, la X ha un'area di tocco piu' grande del disegno, aprendo il visore delle scope il vassoio si chiude. Provato su iPhone 12 e SE (misure uguali al mockup), con 5 e 8 prese e a 4 giocatori. Revisione chiusa con 2 correzioni: con le prese che entrano tutte lo scorrimento resta spento (sui telefoni veri un tocco che si muove di mezzo millimetro veniva preso per un trascinamento e la presa non partiva); a 4 giocatori la pila dei numeri si stringe per restare dentro la carta. Limite noto: a 4 giocatori su iPhone SE con 11 carte in tavola la fila piu' bassa del tavolo finisce 6,6 unita' sotto il bordo del vassoio (anelli e numeri restano visibili).
  - ☑ S9 opzioni e abbandono:
    - Opzioni (rotella in alto a destra) è un foglio che sale dal basso: "Opzioni" e "La partita continua mentre sei qui", poi AUDIO (Musica, Effetti sonori, Vibrazione), GRAFICA (Grafica ridotta) e PARTITA (Suggerimenti mosse), tutti interruttori, e TORNA AL TAVOLO.
    - Abbandona (in alto a sinistra) apre il dialogo rosso "Abbandonare la partita?" con i chip Sconfitta e Nessuna esperienza, RESTA AL TAVOLO e Abbandona.
    - Il testo del dialogo cambia col tipo di partita:
      - in allenamento a 2: "La vittoria andrà a [avversario]";
      - online con altre persone: il tuo posto lo prende un bot e "[nome] continuerà la partita" (l'avversario nel 1v1, il compagno nel 2 contro 2), altrimenti "la partita continuerà".
    - Indietro chiude il dialogo.
    - Provato su iPhone 12 e SE e a 4 giocatori.
    - Revisione chiusa con 1 correzione: chi è uscito da poco ed è ancora nella finestra di rientro viene chiamato col suo nome e non "Bot N".
    - Restano minori: con altri pannelli aperti Indietro chiude prima il dialogo; toccando Abbandona i suoni di apertura sono due.
  - ☑ S10 Sorteggio (1 contro 1):
    - Al posto del vecchio pannello c'è la ruota del mockup: "PARTITA 1 VS 1", "Chi fa il mazziere?", ruota verde e blu con i due avatar, lancetta d'oro in alto e sole al centro.
    - Tempi:
      - la ruota parte dopo 1,2 secondi e gira per 3,4;
      - si ferma con il mazziere sotto la lancetta (lo decide sempre il master);
      - a 4,7 secondi sale la scheda MAZZIERE ("Sei tu!" oppure il nome dell'avversario, con chi gioca per primo);
      - poi "La partita inizia tra 3, 2, 1" e al tavolo dopo 7,7 secondi.
    - Online i tempi sono fissi, uguali su tutti i telefoni.
    - Offline:
      - con animazioni veloci o grafica ridotta tutto dura 4,8 secondi;
      - AL TAVOLO compare col risultato e chiude subito.
    - Scelta tua (30/09): la ruota gira solo a inizio partita e alla rivincita. Nelle smazzate dopo non c'è sorteggio e si sposta solo il gettone "M". Questo sostituisce, per il 1v1, la "roulette sotto i 2 secondi a ogni smazzata" di K7. A 4 giocatori resta la roulette di prima (Fase 6).
    - Avatar e nomi restano sempre dritti mentre la ruota gira (scelta tua, 30/09; nel mockup girano con la ruota). In cima stanno come nel mockup, in fondo l'avatar resta staccato dal sole.
    - Provato su iPhone 12 e SE (misure come da progetto), animazioni veloci, grafica ridotta, seconda smazzata senza ruota, 4 giocatori con la roulette vecchia.
    - Revisione chiusa: nessun difetto. Una rifinitura fatta: online con le animazioni veloci la dissolvenza finiva un decimo di secondo prima della consegna.
    - Limiti noti:
      - dopo un blocco del telefono di oltre 3 secondi il suono del risultato può saltare;
      - se l'app va in background durante la ruota, grafica e tempo di consegna potrebbero non coincidere (non verificato).
  - Da decidere (S5): la nuvoletta della mia emoticon copre per 2,6 secondi meta' della prima carta della mano, come nel mockup. Se preferisci che resti sopra al banner basta spostarla.
  - Partite a 4: banner mio e in alto nuovi, sinistra e destra vecchi fino alla Fase 6.
  - Da non rilanciare: `TablePlayerBannersBuilder`, `UIV2FoundationBuilder.Settings` e, da S5, "Build Emoticon Quick Bar", "Build Animated Emoticons" e `FrontendExpansionBuilder` Game, da S6 `TableActionButtonsBuilder` e "Tools/UIV2/Build Accuso Window" (cancellano i collegamenti nuovi).

**2.29:** UI51 Fase 4 (Collezione, Profilo, Impostazioni) costruita e provata in Unity. Menu **Tools/UI51/Build Fase 4 (Collezione, Profilo, Impostazioni)** (`Assets/UI51/Editor/UI51MetaBuilder.cs`) in `MainMenu.unity`. Build pulito (0 errori); test EditMode 310 totali: 303 ok, 0 falliti, 7 saltati (Explicit). Committata con la 2.30.
- Costruito:
  - Impostazioni e dialogo "Elimina account".
  - Profilo: stato ospite, stato account, editor di Avatar, Cornice e Banner.
  - Collezione: intestazione, schede a segmenti col conteggio ("Mazzi 4", "Emoticon 3/3", "Accuso 1"), griglia dei mazzi, emoticon (3 slot "In partita" più griglia), scheda Accuso.
- Decisioni dell'utente: editor del profilo costruito e collegato; "Grafica ridotta" tenuta nelle Impostazioni (sezione GRAFICA).
- Script toccati, solo agganci opzionali (vuoti = UIV2 classica): `CollectionScreenV2` (schede a segmenti, `SetTabCount`), `CollectionEmoticonsPanel` (`equippedCountLabel`), `CollectionCosmeticsV2` (`PreviewFist`, ordine degli slot), `DeckCardView` (`preserveArtAspect`), `UIV2CollectionCard` (`orderLabel`), `CollectionItemViewData` (`Order`), `ProfileScreenV2`, `ProfileEditorV2` e `ProfileCosmetics` (nuovi), `SettingsV2Integration`, `DeleteAccountModalV2`, `HomeV2Integration`.
- Verificato nel Simulator su iPhone 12 e SE: Impostazioni, dialogo Elimina account, Profilo ospite, le tre schede della Collezione. Provati: scelta del mazzo, rimozione e aggiunta di un'emoticon, messaggi d'aiuto, ANTEPRIMA dell'accuso, tocchi, dissolvenza delle intestazioni. Preferenze del giocatore rimesse com'erano dopo le prove.
- Verificato solo con dati di prova: Profilo con account ed editor (nessun accesso reale, SALVA mai premuto). "Elimina" non è mai stato premuto.
- Correzioni:
  - Intestazioni di Collezione e Profilo: in gioco uscivano in Poppins invece che in Cinzel e Nunito. `UIV2DesignSystem` ora salta ogni nodo il cui nome comincia per "UI51" (prima solo il nome esatto). Nella prima verifica del Profilo l'errore era sfuggito. Test: `StylingSkipsEverythingUnderUI51Nodes`.
  - Scheda Accuso su iPhone SE: sforava di 1 unità. Area del pugno alta 186 invece di 190.
- Correzioni nate dalla revisione del codice (4 aree, ogni difetto controverificato):
  - "Esci" dalle Impostazioni: non ricaricava la scena, e Indietro riportava nella Home come ospite non scelto. Ora fa la stessa uscita del pannello account (`SettingsV2Integration.Logout`). Provato in gioco da ospite su iPhone 12: si torna all'Accesso con "Continua come ospite".
  - Registrazione dall'app: l'account restava in veste ospite fino al riavvio (niente editor, niente "Elimina account", niente XP). Difetto precedente alla Fase 4. Ora `AuthUIController` chiama `PlayFabAuthService.MarkRegistered(email)`. Test: `MarkRegistered_TurnsTheSessionIntoARealLoginWithEmail`. Non provato con una registrazione vera.
  - Editor del profilo: con rete lenta, subito dopo un accesso, poteva aprirsi sui dati del profilo precedente. Ora usa la stessa condizione della carta (`HomeV2Integration.CloudReady`).
  - CREA UN ACCOUNT nel Profilo ospite: dopo il primo tocco restava rimpicciolito al 97% (due effetti al tocco sullo stesso pulsante). Il builder ora toglie `UI51Press` da quel pulsante. Provato in gioco: torna al 100%.
  - Segnalazione smentita dal controllo in Unity: lo scorrimento di Profilo e Collezione usa la colonna nuova, anche dopo aver riaperto la scena da disco e in gioco.
  - Salvataggio dell'aspetto (fino a 3 scritture separate, senza messaggio se una falliva): corretto in 2.30.
- Differenze dal mockup rimaste (ognuna col via):
  - Impostazioni: riga Notifiche e "Cambia password" nascoste; Lingua "Italiano" non si tocca; velo dietro i pannelli senza sfocatura; il dialogo tiene il cerchio rosso anche nel messaggio finale.
  - Profilo: l'intestazione sta nella barra in alto e il contenuto parte circa 17 unità più in basso; si vedono solo dati veri; 8 avatar; banner "Stellato"; cerchio dell'ospite a bordo continuo; la riga "prossimo sblocco" non c'è. Chiusi in 2.30: CREA UN ACCOUNT su iPhone SE, esito del salvataggio dell'aspetto.
  - Collezione: scheda "Mazzi" al posto di "Dorsi" (dalla 2.30 anche nel testo del Profilo ospite); 4 mazzi veri; "Prossimi sblocchi", "Nuova emoticon", monete e gemme nascosti; conteggio "6 / 6" invece di "6 disponibili"; bordi tratteggiati resi continui; arte dei mazzi adattata alla carta (fino al 6% di deformazione); carte dei mazzi senza animazione al tocco.
  - Emoticon: dalla 2.30 il tocco in griglia toglie un'emoticon già in uso, come nel mockup.
  - Accuso: ANTEPRIMA fa battere il pugno sul posto, senza suono; "In uso" è fisso perché l'accuso è uno solo; "Nuovi accusi in arrivo" è un testo fisso.
- Da provare su un telefono vero: la tastiera nel dialogo Elimina account (dalla 2.30 la finestra sale da sola).
- ASSET MANCANTI DA CREARE: nessuno.
- Prossimo: Fase 5 (Tavolo 1v1), col via.

**2.28:** UI51 Fase 3 (Home e pannelli) costruita e provata in Unity. Menu **Tools/UI51/Build Fase 3 (Home)** (`Assets/UI51/Editor/UI51HomeBuilder.cs`) in `MainMenu.unity`. Build pulito (0 errori, 0 warning); test EditMode 288 ok, 0 falliti, 7 saltati (Explicit). Committata in `42bef09`, non inviata al server.
- Costruito: testata account (avatar, "Livello N", barra XP) e testata ospite (badge OSPITE, Registrati), colonna dei pulsanti laterali, tile Modalità e Mazzo, GIOCA, barra in basso, pannello Modalità a 3 schede (Online, Allenamento, Stanza privata) e pannello Mazzo.
- Decisioni dell'utente: Amici, Missioni e Notizie nascoste per ora; ambiente animato della Home mantenuto.
- Script toccati, solo agganci opzionali (vuoti = UIV2 classica): `UIV2TopBar` (SetGuest, Registrati), `UIV2SelectorChip` (SetBadge), `SelectorOptionViewData` (Caption, ShortName), `QuickSelectionPanels` (schede, CONFERMA, testo difficoltà), `HomeScreenV2` (SetGuest), `HomeV2Integration`.
- Verificato nel Simulator su iPhone 12 e SE: Home ospite, Home con account, le tre schede di Modalità, pannello Mazzo. Su iPhone 12 le misure coincidono col mockup (avatar 58, margini 20/14, tile 169×64, GIOCA 350×60, 24 sopra la nav). Su SE tutto entra senza sovrapposizioni.
- Correzioni:
  - Testi spariti: i rect erano più bassi della riga TMP. Altezze alzate di 2-4 unità.
  - Celle e schede alte 0: nelle righe serviva `childForceExpandHeight`.
  - Ospite: Opzioni sale in cima, la colonna destra ora è impilata.
  - Spunta di selezione fatta con due tratti `UI51Shape` (nel mockup è un tratto SVG, non l'icona).
  - "ALLENAMENTO" nella tile era tagliato: il testo si riduce da 9 a 7 pt.
  - GIOCA si intravedeva dietro i pannelli: fondo dei pannelli pieno (il .97 del mockup conta su una sfocatura che non abbiamo).
- Differenze dal mockup rimaste (ognuna col via):
  - Mancano: titolo di rango; monete e gemme (nascoste); azioni di Posta, Premi e Classifica; campo del codice nella scheda Stanza privata; sfocatura dietro i pannelli; mazzi bloccati.
  - Diverse: barra in basso alta 61 invece di 72; 3 livelli di difficoltà invece di 4; angoli delle anteprime dei mazzi quasi squadrati; il pulsante del mazzo dice "USA QUESTO MAZZO" con nome e numero di carte sulla riga sopra.
  - Barra in basso: l'icona selezionata si ingrandisce un po'; le icone inattive non sono attenuate; etichette tutte in grassetto.
  - Pulsanti laterali: l'avviso di novità è un pallino, non un numero.
  - I mockup dei pannelli non si aprono nel browser (manca `support.js`): confrontati con le misure del loro sorgente.
- ASSET MANCANTI DA CREARE: nessuno. Monete e gemme non sono tra le icone controllate, ma per ora sono nascoste.
- Seguita dalla Fase 4 (vedi 2.29).

**2.27:** Fase 2 UI51 provata in Unity. Build Fase 2 pulito (0 errori, 0 warning), Play senza errori in console. Login, Registrazione, Termini/Privacy e Caricamento verificati nel Simulator su iPhone 12 e SE contro i mockup (`Design/51_handoff/.../mockups/*.dc.html`).
- Campi di `AppLoadingView` collegati. Aggiunto `Percent`: la percentuale sotto la barra, vuota quando il caricamento è indeterminato.
- Correzioni:
  - `UI51Input`: i campi erano invisibili, perché `color` è una tinta. Ora si imposta `fill`.
  - Lo sheet ora arriva fino al bordo, con un'estensione di 80 px sotto la safe area.
  - I titoli di Termini/Privacy erano vuoti per via dell'Ellipsis su un rect basso. Ora usano NoWrap.
  - Nella schermata legale la dissolvenza in basso si estende oltre la safe area.
  - Caricamento: le carte dell'onda erano schiacciate a sinistra. Il LayoutGroup è stato tolto e ogni carta ha una posizione assoluta, perché UIKeyframes legge la posizione quando parte.
  - Caricamento: il suggerimento veniva tagliato. Ora è un figlio diretto di Safe.
- Scostamenti residui:
  - Sui telefoni col notch `DesignCanvasFit` scala a circa 0.9x, quindi logo e titoli sono un po' più bassi del mockup. Si può cambiare, serve il via.
  - L'etichetta versione mostra "v2.27" invece del segnaposto del mockup.
  - I testi legali sono quelli veri (10-11 voci d'indice).
  - Il colore dell'etichetta Step è leggermente diverso.

**2.26:** UI51 Fase 2 (Accesso), scritta nel cloud. Menu **Tools/UI51/Build Fase 2 (Accesso)** (`Assets/UI51/Editor/UI51AccessBuilder.cs`) in `MainMenu.unity`.
- Main (login), Registrazione, Termini/Privacy (un solo LegalModalV2 con indice e sezioni), Caricamento. Solo grafica: gli script esistenti restano, il builder ricollega i campi serializzati.
- Ogni schermata ha la struttura UI51 → Bg (envelope) → Overlay → Safe (`DesignCanvasFit` 390×844, ora con `Reference` e `Fill` pubblici). Le grafiche legacy (`Design`, `Dim`, `Background`, `DesignArea`) vengono spente, non cancellate.

**Rimandati Fase 2:**
- Grafica non ancora animata o collegata: barre e bagliore della robustezza password; globo (lingua) senza funzione. (Le animazioni del caricamento funzionano dalla 2.27.)
- AuthUIController non modificato (file ISO-8859/CRLF): i suoi bottoni indietro restano sui vecchi oggetti, quelli nuovi li governa AuthScreensV2. `LoginBack` aggiunto anche se non è nel mockup.
- Approssimazioni: sfondo "center 30%"; interlinea TMP; riempimento della barra senza estremità arrotondata; "mt -4" di Password dimenticata; bordo laterale e inferiore dello sheet nascosti dall'offset; ombra del titolo della registrazione; stile della scrollbar legale.
- Dipende dallo script: formato di "Lo sapevi?". Omessa la nota segnaposto in fondo ai documenti legali.

**2.25:** Fase 1 UI51 provata in Unity. Build All pulito (0 errori, 0 warning); test EditMode 288 ok, 0 falliti, 7 saltati (Explicit). Gallery verificata nel Simulator su iPhone 12 e SE.
- Correzioni: i fogli emoticon ora hanno 8 fotogrammi (serviva SetDirty sull'importer); `PlayerBanner.m_Name` rinominato `nameText` (era duplicato).
- Sorgenti degli atlas non compresse, quindi niente più warning di compressione. Il font non segnala più finti "caratteri mancanti".
- Aggiunto il riempimento sotto la BottomNav.
- Scrim di sheet e dialog estesi oltre la safe area (coprono notch e home indicator); lo sheet arriva al bordo inferiore.
- Il Dialog senza icona non lascia più lo spazio vuoto in alto: `m_IconRow` viene nascosto.
- Da rifinire: le etichette di sezione della gallery (oro 12 px) si leggono male sul pavimento chiaro dello sfondo. È solo la scena di prova.

**2.24:** UI51 Fase 1, Fondamenta del nuovo design (`Design/51_handoff`, SPEC autorevole). Solo file nuovi sotto `Assets/UI51/`: nessuna scena o asset esistente toccato.
- Arte importata per area in `Assets/UI51/Art/<Area>/` con un Sprite Atlas per area (sfondi esclusi). Font Cinzel/Nunito come TMP dinamici con accentate italiane.
- Token (colori, raggi, font, letter-spacing), shader `UI51/Shape` (rettangolo arrotondato con gradiente) e `UI51/Banner` (parametrico, 6 stili), preset `UIAnim` sez. 6.
- Componenti e prefab sez. 3: UI51_Root (Canvas 390×844, match 0.5, SafeArea), bottoni, toggle, pannello, tab, AvatarFrame, badge, PlayerBanner ×3, BottomNav, BottomSheet, Dialog, emoticon da fogli 4×2.
- Scena di prova `Assets/UI51/Scenes/UI51_Gallery.unity` (fuori dai Build Settings) con i bottoni PROVE.
- Menu unico: **Tools/UI51/Build All (Fase 1)**. Si ferma se una scena aperta ha modifiche non salvate.
- Test EditMode `UI51FoundationTests` (fotogrammi emoticon, letter-spacing). Compilazione, test e refresh del grafo vanno fatti in locale.

**Aperti UI51 (ognuno col via):**
- Fase 2 committata (`aeb3bfc`). Fase 3 committata (`42bef09`). Fase 4 (Collezione, Profilo, Impostazioni) provata (vedi 2.29), da committare col via. Fasi 5–10: Tavolo 1v1, Tavolo 2v2/1v3, Fine smazzata/partita, Amici/Posta/Notizie/Premi, Overlay connessione, Pulizia (lista file per file da confermare).
- Rimandati: coriandoli → F7; ConnectionOverlay → F9; ventaglio carte prese, picker emoticon, "+N" ed emo-fly → F5; input nel Dialog → F4.
- Scostamenti noti: niente blur di sfondo; gradienti conici resi lineari; bordi superiori di sheet e nav approssimati.

**2.23:** correzioni chieste dall'utente dopo la 2.22.
- La tab Negozio è tornata nella bottom bar (BottomNavPolishBuilder, RestoreShop). La 2.22 l'aveva tolta seguendo la revisione approvata. L'icona è ancora `ic_cart` arancio: manca `ic_cart_cream`.
- Bottom bar più bassa, senza la striscia sopra.
  - `BottomNavSafeAreaBleed`: sink 0.5; l'host si accorcia di quanto scende il contenuto.
  - Effetto collaterale: nella Home GIOCA e le pillole scendono di ~51 px; lo spazio fino alla nav passa da ~190 a ~108 px.
- Pile laterali degli avversari più verso il centro: `CardViewManager.sideHandInsetFromBannerEdge` = 78.
  - Se cambia un valore predefinito del prefab, serve un reimport ForceUpdate.
- Il velo scuro delle finestre copriva già la bottom bar, misurato in pixel. La barra è già blu notte, per questo sembrava scoperta. Non cambiato.
- Icone rapide della Home: una sola striscia d'ombra morbida (`ShadowStrip`) dietro tutta la colonna al posto degli aloni per icona (Build Home Quick Actions).
- Verificato nel Simulator su iPhone 12, SE e iPad Mini. G4 fatto: commit della 2.23 su `codex/home-v2-training`, non pushato.

**Aperti (ognuno col via):**
- Riportare GIOCA/pillole più in alto nella Home, se lo spazio in basso ora sembra poco.
- Su iPad la scritta Opzioni finisce sotto la pillola MAZZO (già noto dalla 2.19).
- Icona crema del carrello per la tab Negozio (asset dell'utente).

**2.22:** collegati gli asset consegnati e fatte le correzioni rapide approvate della revisione 2.20.
- Icone crema (16) collegate: rilanciato Tools/UIV2/Apply Icon Set v2. Al tavolo Emoji usa `ic_chat` e ACCUSO `ic_accuso`.
- Tavolo: nuovo builder Tools/UIV2/Apply Room And Table Kit (2.22).
  - Imposta lo sfondo stanza 2x (1882x3344, max 4096) su GameBackground con scala Cover salvata.
  - Imposta il kit feltro/vignetta/cornice 9-slice (`Assets/Art/Table/`) su TableFeltRenderer, in modalità kit.
  - Mette il contorno navy alle didascalie Emoji/ACCUSO; il materiale deve essere dello stesso font.
  - Va rilanciato dopo Apply Table Layout V4, che non imposta più lo sfondo e rifà le didascalie senza contorno.
  - La cornice sostituisce l'ammorbidimento del bordo del tavolo.
- Accesso: ACCEDI e REGISTRATI funzionano (Canvas_Login riattivato in MainMenu; deve restare attivo).
- Tolte le voci "prossimamente": Tools/UIV2/Hide Coming Soon (2.22), da rilanciare dopo Build Delete Account, Profile o Collection. Tolta anche la tab Negozio.
- Lobby: stato di caricamento invece del codice KKKKK e di "Stanza di" vuota.
- Verificato nel Simulator su iPhone 12, SE e iPad Mini.

**Aperti:** chiusi nella 2.23.

**2.21:** nuovo mazzo **Barocco** (nome provvisorio) dell'utente integrato: `Assets/Art/Decks/51_BAROCCO_PNG_Unity/`
(facce 287x452, dorso 874x1376, PPU = altezza/1.8), `Resources/CardDecks/barocco.asset`, 4ª voce del catalogo.
Polish Quick Deck Panel ora clona la cella per i mazzi nuovi (griglia 3 colonne, passo 310x286). Test mazzi estesi
a barocco. Verificato nel Simulator (iPhone 12): pannello Mazzo, anteprima, tavolo (mano, carte in tavola, dorsi).
Napoletano resta il predefinito così com'è (scelta dell'utente, 25/09).
Consegnati dall'utente e **non ancora collegati:** 2 fogli icone crema (16 icone), kit tavolo (cornice, feltro,
vignetta), sfondo sala (941x1672 RGB: serve 1882x3344).
**Prossimo:** ritagliare e collegare le icone crema, kit tavolo, poi le correzioni rapide della revisione (ognuna con il via).

**2.20:** fiamme Home provvisorie meno in risalto (nucleo spento verso l'arancio, un po' di trasparenza,
ondeggiamento più calmo: devono leggersi come sfondo) in attesa dei frame flipbook dell'utente. Tolto lo spazio
residuo sotto la bottom bar. Bottom bar rifinita (BottomNavPolishBuilder): icone centrate, linguetta del selettore
119 -> 132 verso il basso, scritte ExtraBold con contorno e ombra attaccata. Icone rapide Home (Tools/UIV2/Build Home
Quick Actions): niente quadrato blu (resta area di tocco a alfa 0), alone scuro morbido, scritte con contorno — le
scritte restano. Schermata iniziale: ACCEDI/REGISTRATI ExtraBold crema con contorno scuro, REGISTRATI ciano.
Pannello Mazzo (Tools/UIV2/Polish Quick Deck Panel): dorsi veri ovunque, anteprima arrotondata con anello,
didascalia "Nome · 40 carte" a 32 su una riga (misurata sul mockup), scritte con contorno. Il chip MODALITÀ della
Home ripete titolo e icona della riga scelta nel pannello Modalità. Verificato nel Simulator (iPhone 12).
**Da rilanciare dopo un rebuild completo di Home/FrontendFlow:** Build Home Quick Actions e Polish Quick Deck Panel.
**Aperti:** frame flipbook fiamme (utente), proposta swipe pannelli (sfondo condiviso dietro UIV2Pager + contenuto
pagine vuote), proposta animazioni cambio tab (asset utente), cella mazzo selezionata con bagliore oro morbido invece
del pulsante pieno, icone modalità per numero di giocatori (1v1/2v2/1v3). Commit G4 ancora da fare.

**2.19:** set icone crema collegato in un colpo solo (mappa `CreamIcons` in `NewIcon`: chest/trophy/exit/nav_back/
close/settings crema, ic_mail resta com'è; Tools/UIV2/Apply Icon Set v2 ripassa anche i riferimenti v2 oro).
Fiamme: niente più accendi/spegni A/B, una fiamma per torcia con shader `UIV2/FlameWobble` (lingue che salgono,
allungamento e tremolio, ferma con Grafica ridotta tramite `_UIV2Still`). Bagliore torce più piccolo (0.75) e tenue
(alpha 0.1). Luci del castello riallineate alle finestre. Sfondo a tutta pagina: tolta la sfumatura in alto, la
Region ignora il bordo alto della safe area e l'artwork arriva fino alla nav (AboveNav 124). Aggiunti home_bushes
(in primo piano, costruiti per ultimi); esclusi home_vines (ingombranti, doppiano l'edera della base).
Verificato nel Simulator: iPhone 12, iPhone SE, iPad Mini 4. Aperti: fiamma destra in parte dietro il tasto Opzioni,
MODALITÀ/MAZZO sulla balaustra su iPad (centro Home, compito dell'utente). G4 commit ancora da fare.

**2.18:** sfondo Home animato rifatto sui nuovi asset (BackgroundHome/, vecchi nastri/semi/gemma cancellati).
Base + overlay 1:1 a tutto schermo: stelle e luci del castello che respirano, stella cadente ogni ~10 s
(`UIV2AmbientFloat.pause`), bagliore torce leggero; due fiamme per torcia (home_flame_a/b) in dissolvenza incrociata,
perno sul fondo visibile della fiamma. Valori in `HomeOverlayLayers`/`HomeFlameLayers` del builder, misurati in pixel
sul mockup (coppe, fiamme, castello). Esclusi home_vines e home_bushes: nel mockup ci sono già quelli della base.
Icone crema ic_chest/ic_trophy/ic_exit importate, NON ancora collegate: manca ic_mail crema (cambio set in un colpo solo).
Verificato nel Simulator: iPhone 12, iPhone SE, iPad Mini 4 (anche Grafica ridotta = posa di riposo).

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
| D2 | Scelta di icona e banner nel profilo, visibili al tavolo | ◐ 2.29: editor di avatar, cornice e banner nel Profilo; l'avatar scelto si vede anche nella Home. Provato solo con dati di prova. Aperto: al tavolo non si vedono ancora |
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
| I4 | Profilo rapido al tavolo: tocco sul banner di un giocatore → scheda piccola (mockup `10_profilo_rapido`) | ☑ 2.35 `UI51TableBuilder.BuildQuickProfile` (mockup Partita/Partita4 profilo); medaglie da definire |
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
| K7 | **Flusso veloce**: dall'apertura alla partita in 2 tocchi (l'ospite già entrato salta la schermata iniziale), roulette del mazziere più breve o saltabile, risultati che proseguono da soli dopo qualche secondo | ☑ 2.07 (23/09): ospite diretto in Home, roulette a un giro, conto di 8 s solo host/offline, rivincita manuale. Dal 30/09 (2.31) nel 1v1 c'è la ruota del Sorteggio solo a inizio partita e alla rivincita (7,7 s). Manca la prova con due client reali. `docs/ui/k7-flow-progress.md` |
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
| L1 | Altre lingue oltre all'italiano (riga Lingua delle Impostazioni, mockup `Lingua`). Decisione tua 01/10: più avanti | ⏸ |
| L2 | Notifiche push (riga Notifiche delle Impostazioni, mockup `Notifiche`). Decisione tua 01/10: più avanti | ⏸ |

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

Consegnati il 25/09 (dopo la revisione 2.20): 2 fogli icone crema 4x2, kit tavolo, sfondo sala (poi riconsegnato 2x),
mazzo Barocco (collegato nella 2.21); il resto è collegato nella 2.22. File in `DragonsHoard/sprites_unity/sprites_unity/Immagine ChatGPT 25 set 2026*`.

Consegnati il 17/09: Poppins Regular/Medium/SemiBold, bagliore morbido cerchio e rettangolo, velo "Sfocatura sfondo" (la sfocatura vera si fa via codice sotto al velo, fatta nella 1.81).

Consegnati il 17/09 anche i 31 file audio (musica + effetti), collegati nella 1.82.

Consegnato il 17/09: `ic_arrow_left` (pulsante indietro tondo, usato in 23 e 24).

Consegnato il 18/09: pacchetto legale/auth (`51_Legal_Auth_Package`) con Termini, Privacy, template
email PlayFab, pagine web e riferimenti backend. Termini e Privacy sono in app dalla 1.85.

Ancora da fare, ma NON sono asset da disegnare: vedi "Legale e account" qui sotto.
