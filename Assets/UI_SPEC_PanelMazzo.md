# UI Spec — Panel Mazzo

Coordinate ricavate DIRETTAMENTE dal codice Python che ha generato
`panel_mazzo2.png` — stesso principio della spec Modalità, valori esatti
non stimati. Canvas di riferimento: 1080 × 1920.

Stessa `PanelFrame`/`TitleRibbon`/`CloseButton` del pannello Modalità
(stesse dimensioni base, cambia solo l'altezza totale) — se avete già
un componente/prefab condiviso per la cornice, riusatelo.

---

## 1. Cornice e titolo

| Elemento | Box (X0,Y0 → X1,Y1) | Note |
|---|---|---|
| PanelFrame | **44, 210 → 1036, 1680** (992×1470) | stesso stile Modalità |
| TitleRibbon | centrato X, **Y=154**, W=520, H=188 | testo "MAZZO" |
| CloseButton | **X=932, Y=236**, 86×86 | icona `ic_x` |

---

## 2. Riquadro Anteprima (la parte NUOVA rispetto a Modalità — qui c'è la dimostrazione visiva)

- Box: **X 74 → 1006, Y 340 → 870** (932×530)
- Sfondo: pannello scuro `#101C2E`, bordo sottile 3px `#466E8E`
- Label "ANTEPRIMA": TMP 22, colore Gold, centrata, Y=**382**

### Ventaglio 3 carte (il mazzo attualmente selezionato)
- Centro ventaglio: X=540, Y=**640**
- 3 carte, altezza **300px** ciascuna, sovrapposte con offset X **-150 / 0 / +150** dal centro
- Rotazione: **+16° / 0° / -16°** (sinistra, centro, destra)
- Ombra propria sotto ogni carta (drop shadow leggero, offset +8/+12, blur ~9)
- Sprite carta: dorso del mazzo attualmente selezionato (dinamico — cambia quando l'utente seleziona un mazzo diverso nella griglia sotto)

### Didascalia
- Testo "{NomeMazzo} · 40 carte" (dinamico), TMP 26, colore `#FFECBE`, centrato, Y=**814**

---

## 3. Griglia mazzi (scrollabile)

- Viewport: **X 74 → 984, Y 904 → 1490** (910×586)
- Grid: **3 colonne**, cella **290×260**, gap orizzontale **20px**, gap verticale **26px**
- Content totale: 3 righe × 286px passo = **858px** (supera il viewport 586 → scroll verticale attivo)

### Coordinate celle (offset dalla cima del Content, colonna X locale)

| Riga | Y offset | Col 1 (X) | Col 2 (X) | Col 3 (X) |
|---|---|---|---|---|
| 1 | 0 | Napoletano — **0** | Classico — **310** | Reale — **620** |
| 2 | 286 | Smeraldo — 0 | Antico — 310 | Drago — 620 |
| 3 | 572 | Notturno — 0 | Oro — 310 | Rubino — 620 |

### Struttura di ogni cella (component riutilizzabile `DeckCell`)
```
DeckCell [290 × 260]
├── CardArt      [dorso mazzo se sbloccato, altrimenti card_frame_dark scurito ~60%]
├── LockIcon     [ic_lock, H=52, centrato — SOLO se bloccato]
├── NameText     [TMP 21, sotto la carta; Cream se sbloccato / TextMuted se bloccato]
└── CheckIcon    [ic_check, H=46, angolo alto-destra — SOLO se selezionato]
```
- Se **selezionato**: bagliore dorato dietro la cella (glow, non un riempimento pieno — feedback dato dal test precedente: "sembra una macchia, meglio un bordo luminoso")
- **Stato di default nel mockup**: Napoletano sbloccato + selezionato; tutti gli altri 8 bloccati (solo segnaposto — la logica reale di sblocco arriverà con Negozio/Progressione)

---

## 4. Pulsante conferma

- Box: **X 310 → 770, Y 1540 → 1648** (460×108)
- Sprite `btn_gold_long`, Sliced, stesso stile del PlayButton
- Testo "USA QUESTO", TMP 34 Bold, colore `#3A2208`, stroke scuro

---

## 5. Comportamento

- Toccare una cella **sbloccata** nella griglia: aggiorna ventaglio anteprima + didascalia in alto, sposta lo stato "selezionato" su quella cella
- Toccare una cella **bloccata**: nessun cambio di anteprima (per ora — in futuro potrebbe aprire il Negozio)
- "USA QUESTO" conferma la selezione e chiude il pannello, aggiornando il `ValueText` del `DeckSelector` nella Home (vedi UI_SPEC_Home.md §3) con il nome del mazzo scelto

## 6. Cosa NON fare in questo passaggio

- Non implementare la logica di sblocco reale (monete/gemme) — solo lo stato visivo bloccato/sbloccato come sopra
- Non collegare ancora "USA QUESTO" al `DeckSelector` della Home se quel collegamento richiede logica non ancora pronta — in quel caso lasciare il bottone visivamente pronto ma segnalarmelo

## 7. Verifica finale

1. Il ventaglio in anteprima mostra 3 carte con rotazione visibile, non piatte
2. Scrollando la griglia, le righe 2 e 3 diventano visibili senza sovrapporsi alla riga 1
3. Solo "Napoletano" appare sbloccato e selezionato di default
4. Il pulsante "USA QUESTO" non si sovrappone all'ultima riga della griglia scrollata fino in fondo
