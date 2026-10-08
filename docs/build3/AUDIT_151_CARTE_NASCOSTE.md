# #151 Carte nascoste: verifica, rischi e piano (niente è stato cambiato nel gioco)

08/10. Verifica del codice di rete del tavolo (1v1, 2v2, 1v3) e proposta. Nessuna modifica strutturale fatta: come chiesto, prima i
rischi e il piano. Decisioni D8-D10 e strada scelta: sezione 6. Unica aggiunta: la prova `MoveSerializerTests.HiddenCards_KnownLeak151_StateCarriesOpponentHandAndDeck`, che
**fotografa la falla** (passa oggi; quando #151 sarà fatto deve fallire e va rovesciata).

**In breve.** Oggi ogni telefono riceve e tiene **tutte** le carte: mani di tutti e ordine del mazzo. Un client modificato le legge
senza fatica. Non è un errore in un punto: tutto il tavolo è costruito così (ogni telefono distribuisce, controlla e all'occorrenza
gioca al posto degli altri). Filtrare lo stato è fattibile ma tocca distribuzione, controllo delle mosse, accuso, rientro e cambio di
Master Client. E anche fatto bene **il Master Client continua a vedere tutto**: il rischio anti-cheat non sarebbe risolto, solo
ristretto a chi fa da host.

## 1. Cosa esce oggi dal telefono dell'host

| Canale | Contenuto | A chi | Quando | Dove |
|---|---|---|---|---|
| `RPC_ReceiveInitialGameState` | stato completo: **mazzo in ordine**, tavolo, **mani di tutti**, prese, scope, accusi, punteggi | tutti gli altri (`RpcTarget.Others`) | inizio partita | `NetworkGameController.cs:680-700`, `GameStateSerializer.cs:24` e `:27-28` |
| stesso RPC, su richiesta | come sopra | chi lo chiede, **senza controlli** su chi è né quante volte | in qualsiasi momento (`RPC_RequestInitialGameState`) | `NetworkGameController.cs:595-630` |
| stesso RPC, al cambio di Master | come sopra | tutti gli altri | ogni migrazione del Master | `NetworkGameController.cs:489-516` |
| stesso RPC, al rientro | come sopra | chi rientra | rientro entro 60 s | `NetworkGameController.cs:562`, `:586` |
| `RPC_ExecuteMove` (`AllViaServer`) | mossa: posto, carta giocata, prese | tutti | ogni mossa | `NetworkGameController.cs:669` |
| `RPC_ReceiveAccuso` | posto, tipo, punti, smazzata, carte nel mazzo (non le carte) | tutti | accuso | `NetworkGameController.cs:51` |
| Proprietà della stanza e dei giocatori, log | niente carte | — | — | verificato: pulite; nessun `RaiseEvent` |

Il generatore del mazzo è `System.Random` statico (`Rules51.cs:9`, mescolata `:100-112`), con seme dall'orologio del telefono. Anche
filtrando lo stato, chi vede le proprie 3 carte e le 4 del tavolo può cercare il seme al computer e ricostruire il mazzo. Va sostituito
con un generatore crittografico (le prove che rimettono il seme a mano restano su `Rng`).

## 2. Pubblico e privato

| Informazione | Chi può saperla |
|---|---|
| Carte sul tavolo, carta giocata, prese, scope, punteggi, numero di carte in mano e nel mazzo | tutti |
| Carte rivelate da un accuso | tutti, dal momento dell'accuso |
| Mano di un giocatore | solo quel giocatore (in 2v2 **neanche il compagno**) |
| Ordine del mazzo | nessuno |
| Mani dei bot | nessun giocatore; oggi le conosce chi gioca per loro (l'host) |

## 3. Perché oggi ogni telefono ha bisogno di tutte le carte

| Chi usa le carte nascoste | Dove | Cosa serve per farne a meno |
|---|---|---|
| Distribuzione: ogni telefono pesca dal proprio `gs.Deck` a ogni giro | `RoundManager.cs:326-339` | il Master distribuisce e manda a ciascuno solo le sue carte |
| Tre assi alla distribuzione | `RoundManager.cs:227-228`, `:346` | lo decide il Master e lo annuncia rivelando le carte |
| Controllo delle mosse altrui (carta davvero in mano) | `TurnController.cs:1282-1294` | gli altri controllano solo la regola sul tavolo; l'appartenenza la controlla il Master |
| Controllo dell'accuso | `TurnController.cs:1866-1875` | `RPC_ReceiveAccuso` porta le carte rivelate |
| Arbitro di riserva: un telefono qualsiasi gioca i bot e le mosse forzate se il Master è fermo | `TurnController.cs:440-454`, `GameSceneInitializer.cs:209-221` | togliere l'arbitro di riserva: lo fa solo il Master |
| Carte degli avversari disegnate con la loro identità vera | `CardViewManager.cs:208`, `:1444`, `:1460` | dorsi con identità segnaposto; la carta vera arriva con la mossa |
| Rientro e cambio di Master: il nuovo Master riparte dalla sua copia completa | `NetworkGameController.cs:489-516` | **senza un server non c'è una copia completa da cui ripartire** (punto 4) |

## 4. Rischi del filtro per destinatario

1. **Cambio di Master.** Oggi se l'host cade un altro telefono diventa Master e continua, perché ha tutto. Con il filtro il nuovo
   Master non conosce il mazzo né le mani degli altri: **la partita non può continuare**. Strade: (a) la partita finisce senza
   vincitore (premi: partecipazione, come una incompleta); (b) si aspetta il rientro dell'host per 60 s, ma solo se l'app non è stata
   chiusa (dopo un riavvio la copia completa è persa); (c) il server tiene una copia (punto 6). Questo cambia la regola già decisa "rientro 60 s,
   bot dopo 30 s" proprio per il posto dell'host.
2. **Bot al posto di chi esce.** Il bot gioca con la mano di chi è uscito: può farlo solo il Master (lo sa già oggi). Se a uscire è
   il Master, si torna al punto 1.
3. **Arbitro di riserva e mosse forzate.** Con l'host fermo ma nella stanza (`MasterRemoved`, 2.57) oggi un altro telefono forza le
   mosse e gioca i bot. Senza le mani non può: bisogna attendere il passaggio del Master da Photon, cioè ancora il punto 1.
4. **Desincronizzazioni.** Distribuzione e mosse diventano RPC mirate più RPC per tutti: l'ordine fra le due vale solo se partono
   dallo stesso mittente sullo stesso canale. Va rifatto il percorso "stato per chi resta indietro" (`SendStateWhenSettled`) per
   destinatario. È il tipo di modifica che ha già causato giri di bug (2.57-2.61).
5. **Grafica.** Le animazioni che spostano una carta dalla mano dell'avversario la cercano per identità: con i segnaposto va
   cambiato il modo di trovarla.
6. **Compatibilità.** Le versioni vecchie non si incontrano con le nuove (AppVersion Photon = versione dell'app), quindi niente tavoli
   misti; serve però la nuova versione per la regola.
7. **Quello che non risolve.** L'host continua a vedere tutte le carte (e in 1v3 o 2v2 con bot anche quelle dei bot, che gioca lui).
   Un imbroglione che crea le stanze resta host spesso. Il filtro **toglie l'imbroglio facile agli altri tre**, non all'host.

## 5. Livello 1: stato filtrato per destinatario (NON si fa, decisione D8)

Stima: 4-6 giorni con le prove sul telefono, in un giro a parte, prima di classifiche con premi. Ordine:

1. **Generatore crittografico** per il mazzo delle partite vere (`RandomNumberGenerator`); `Rng` resta solo per prove e tutorial
   (seme 759353 fissato da una prova). Piccolo, indipendente: si può fare subito.
2. **`GameStateSerializer.SerializeFor(gs, posto)`**: mano propria vera; mani altrui come N segnaposto distinti; mazzo come solo
   numero; il resto uguale. La prova `HiddenCards_KnownLeak151` si rovescia: il destinatario vede solo la propria mano.
3. **Invio per destinatario** in `SendInitialGameState`, `SendStateWhenSettled` e alla richiesta. La richiesta di stato si accetta solo
   da un actor seduto nel posto (niente stato a chi non siede) e al massimo una volta ogni pochi secondi.
4. **Distribuzione dal Master**: a ogni giro il Master pesca e manda a ciascuno solo le sue tre carte (`RPC_ReceiveHand`, mirata),
   insieme al conteggio pubblico; gli altri non pescano più da soli (`RoundManager` aspetta le carte). Tre assi deciso dal Master e
   annunciato con le carte.
5. **Mosse**: la carta giocata sostituisce un segnaposto; gli altri controllano solo la regola sul tavolo; il Master controlla anche
   l'appartenenza e, se falsa, rimanda lo stato vero e segna la partita (registro, non sanzione).
6. **Accuso**: `RPC_ReceiveAccuso` porta le carte rivelate; il controllo resta uguale su chi riceve.
7. **Arbitro di riserva tolto**; mosse forzate e bot solo dal Master.
8. **Cambio di Master**: strada (a) o (b) del rischio 1, da decidere (D8).
9. `CardViewManager`: dorsi con segnaposto per gli avversari.
10. Prove: EditMode (filtro per 1v1/2v2/1v3, compagno in 2v2, accuso, rientro), più prova di gioco a 2 e 4 telefoni con cambio di
    Master, rientro e uscita.

## 6. Decisioni dell'08/10 e strada scelta (in attesa di approvazione)

- **D8 = C, temporaneamente.** Il livello 1 qui sopra **non** si fa: chiuderebbe le partite al cambio di Master. Rientro e cambio di
  Master restano come sono.
- **D9.** #151 va risolta **prima del lancio pubblico**: nascondere le carte nella grafica non basta, i dati non devono arrivare ai
  client.
- **D10.** Confronto fra CloudScript come arbitro, servizio di partita dedicato e plugin Photon, con costi, latenza, riuso del codice,
  parità delle regole e prototipo: [PIANO_RISULTATI_AUTOREVOLI.md](PIANO_RISULTATI_AUTOREVOLI.md), sezione 6. Raccomandazione: un
  piccolo servizio di partita autorevole in .NET che riusa le regole di `Assets/Scripts/Core` senza copiarle. Nessuna migrazione prima
  della tua approvazione (D12).

Finché il servizio non c'è, la falla resta com'è ed è un **blocco per il lancio pubblico**. La prova
`HiddenCards_KnownLeak151_StateCarriesOpponentHandAndDeck` continua a fotografarla.
