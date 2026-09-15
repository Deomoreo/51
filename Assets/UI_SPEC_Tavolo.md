# UI Spec — Tavolo di Gioco

**ATTENZIONE PRIMA DI LEGGERE OLTRE:** questo non è uno schermo da costruire da zero.
`CardView`, `CardViewManager`, `TurnController`, `AccusoPanelController`,
`PlayerCapturedPileView`, `TurnIndicator` esistono già e sono funzionanti — animazioni,
distribuzione, accusi, tutto reale. Questa spec riguarda **solo l'aspetto visivo**
degli elementi che mancano o vanno rivestiti. Non modificare la logica di rendering
carte esistente.

Coordinate ricavate dal codice che ha generato `table_v4.png` / `table_v4_accuso.png`.
Canvas di riferimento: 1080 × 1920.

---

## 0. Ricognizione obbligatoria PRIMA di aggiungere qualsiasi cosa

Per ciascuno di questi, verificare cosa esiste già e la sua struttura reale:
1. `PlayerCapturedPileView` — mostra già un conteggio prese? Che aspetto ha oggi?
2. `AccusoPanelController` / `AccusoUIBridge` — come rivela oggi le carte di un accuso? Dimensione, posizione?
3. `TurnIndicator` — come segnala oggi il turno attivo?
4. Esiste già un banner nome/punteggio per giocatore, o solo testo nudo?
5. Esiste già un contatore "scope fatte" per giocatore, in qualunque forma?

Se una qualunque di queste esiste già in forma funzionante, **rivestirla con lo stile sotto**, non duplicarla con un nuovo componente parallelo.

---

## 1. Cornice tavolo (feltro)

- Box: **X 40 → 1040, Y 470 → 1250** (1000×780), radius 220
- Cornice esterna: legno `#3A2610`, spessa 16px oltre il bordo del feltro
- Riempimento feltro: verde `#124032`, con luce calda al centro-alto (glow radiale, opzionale — cosmetico)
- Bordo oro 5px `#E8B24A`

## 2. Barra superiore

| Elemento | Box | Note |
|---|---|---|
| Sfondo barra | Y 0→124, stretch X | `#091220`, alpha ~95% |
| Bottone impostazioni | X=26, Y=22, 84×84 | sprite `sq_blue`, icona `ic_gear` H=46 |
| Testo "Mano X di Y" | centrato-70 orizzontale, Y=64 | TMP 24 Bold — **dinamico** |
| Testo "Carte rimaste N" | centrato+150, Y=64 | TMP 22 Medium, TextMuted — **dinamico** |

**Nota**: niente pulsante di uscita qui — va dentro Impostazioni, come deciso in precedenza.

---

## 3. Componente riutilizzabile `PlayerBanner`

Un solo componente per tutti e 4 i giocatori (te compreso), parametrico su: nome, punteggio, turno attivo, lista scope, dimensioni.

```
PlayerBanner [bw × bh, parametrico]
├── TurnGlow      [attivo solo se e' il turno di questo giocatore — bagliore oro dietro]
├── Background    [Sliced, colore #0E1C30, bordo 3px (5px+oro se turno)]
├── AvatarFrame   [H = 78% di bh, sinistra]
├── NameText      [TMP 21 Bold, Cream — dinamico]
├── ScoreText     [TMP 19 "{score} punti", Gold — dinamico]
├── TurnLabel     [TMP 15 "TURNO", destra — visibile solo se turno attivo]
└── ScopeRow      [vedi §4 — ancorato SOPRA il banner, non dentro]
```

### Istanze e dimensioni esatte

| Giocatore | Posizione centro (X,Y) | bw × bh | Note |
|---|---|---|---|
| Avversario alto | **470, 250** | 290×88 | |
| Avversario sinistra | **190, 588** | 270×84 | |
| Avversario destra | **890, 588** | 270×84 | |
| Tu (giocatore locale) | **210, 1360** | 300×92 | |

---

## 4. Scope — carte vere che spuntano da dietro il banner

**Non gettoni, non icone generiche — carte in miniatura**, tagliate a metà (solo la parte superiore visibile), ancorate appena sopra il bordo del banner.

- Dimensione carta miniatura: **54×76**
- Ancoraggio: top del gruppo a **38px sopra** il bordo superiore del banner, centrato sulla stessa X del banner
- Overlap tra carte: si sovrappongono di 18px l'una sull'altra
- **Massimo 4 visibili**, oltre le 4 mostrare badge "+N" (cerchietto scuro bordo oro) invece di continuare ad aggiungere carte
- Z-order: le carte scope stanno DIETRO il banner (il banner le copre per la metà inferiore)

---

## 5. Pile prese (`PlayerCapturedPileView` — verificare se già esiste prima di creare)

- 3 dorsi carta sovrapposti con offset 3px (effetto "mazzetto"), altezza variabile per contesto:
  - Avversario alto: H=64, ancorata a **X = centro_banner + 250, Y = centro_banner**
  - Avversari laterali: H=56, ancorata a **(288, 690)** sinistra / **(792, 690)** destra
  - Tu: H=70, ancorata a **(450, 1360)**
- Badge conteggio: cerchio scuro bordo oro, 44×44, in basso a destra del mazzetto, numero al centro

---

## 6. Carte sul tavolo

**Layout dinamico** (il numero di carte varia durante la partita):
- Dimensione carta: **120×170**
- Gap orizzontale tra carte: **16px**
- Righe da **massimo 4 carte**, gap verticale tra righe: **16px**
- Prima riga inizia a **Y=830**, centrata orizzontalmente rispetto al tavolo
- Righe successive impilate sotto con lo stesso gap
- Ombra propria sotto ogni carta (drop shadow leggero)
- La carta appena giocata/disponibile per interazione ha bordo evidenziato (verde chiaro `#78F0BE` invece di oro) — verificare come questo stato è già gestito da `CardView` prima di aggiungere logica duplicata

## 7. Mano del giocatore locale

**Layout a ventaglio** (numero carte variabile 0-10):
- Dimensione carta: **194×274**
- Overlap tra carte adiacenti: si sovrappongono di **30px**
- Rotazione: la carta centrale è dritta, le altre ruotano di **∓7° per posizione** dal centro
- La carta centrale (o quella sotto il dito) si solleva di **26px** rispetto alle altre
- Ancoraggio verticale: base del ventaglio a **Y=1476** (bordo superiore della carta più bassa)
- Bagliore dorato dietro la carta sollevata/selezionata

## 8. Accuso — rivelazione carte (verificare `AccusoPanelController` prima di creare)

Quando un giocatore dichiara un accuso, le sue carte si girano **alla stessa dimensione dei dorsi che aveva in mano in quel momento** (non quella delle carte del tavolo, non quella della mano del giocatore locale — è l'errore che avevamo già corretto nel mockup).

- Per l'avversario alto (dorsi normalmente H=90): le carte rivelate diventano larghezza `90×0.68≈61`, altezza `90+14=104`
- Bordo carte rivelate: verde chiaro `#78F0BE` (stesso colore di "carta giocabile"), con bagliore dietro tutto il gruppo
- Testo esito (es. "CIRULLA!") sotto le carte rivelate, TMP 26 Bold, Gold
- Durante l'accuso, il banner del giocatore che dichiara **non mostra "TURNO"** anche se è il suo turno — l'attenzione va sulle carte

## 9. Pulsanti azione (Emoji / Accuso)

Fissi in basso a destra, sopra la mano:

| Bottone | Box | Note |
|---|---|---|
| Emoji | X=832, Y=1316, 88×88 | sprite `sq_blue`, icona `ic_person` (segnaposto — verrà sostituita quando le emoticon sono pronte lato codice) |
| Accuso | X=942, Y=1316, 88×88 | sprite `sq_gold`, icona `ic_warn`, bagliore dorato dietro sempre attivo |

---

## 10. Cosa NON fare in questo passaggio

- Non toccare `CardView`, `CardViewManager`, `TurnController`, `CardAnimationController` — solo aggiungere/rivestire elementi UI attorno
- Non reimplementare pile prese o rivelazione accuso se esistono già funzionanti — solo applicare sprite/colori nuovi
- Non collegare ancora il pulsante Emoji a un pannello reale (le emoticon PNG esistono, il pannello di selezione no)
- Il pulsante Impostazioni deve aprire il pannello Impostazioni quando esiste — se non esiste ancora, lasciarlo visivamente pronto e segnalarmelo

## 11. Verifica finale

1. Con una partita reale in corso (Play Mode), i banner mostrano nome/punteggio veri, non segnaposto
2. Il banner del giocatore di turno ha il bagliore oro, e passa correttamente da un giocatore all'altro
3. Le scope fatte durante la partita appaiono davvero come carte dietro il banner giusto, aggiornandosi in tempo reale
4. Con più di 4 carte sul tavolo, la seconda riga si dispone correttamente senza sovrapporsi alla prima
5. Un accuso dichiarato mostra le carte alla dimensione corretta (dorso avversario), non a quella del tavolo o della mano locale
