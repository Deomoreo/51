# UI Spec — Panel Modalità

Coordinate ricavate DIRETTAMENTE dal codice Python che ha generato il mockup
`panel_modalita2.png` — non stime, sono i valori esatti usati per disegnarlo.
Canvas di riferimento: 1080 × 1920 (stesso setup della Home).

Aggiunta rispetto alla spec Home: questo pannello **scorre** (ScrollRect),
quindi le coordinate del contenuto sono relative alla cima del Content,
non allo schermo — è così che va costruito in Unity comunque.

---

## 1. Struttura

```
ModalitaPanelOverlay (Canvas esistente, nuovo child)
├── DimBackground        [Image full-stretch, colore #040A14, alpha 170/255]
├── PanelFrame            [Image: panel_parchment o pannello scuro con bordo oro, vedi §2]
│   ├── TitleRibbon        [Image: ribbon_teal, Sliced] + Text "MODALITÀ"
│   ├── CloseButton        [Image: sq_blue] + Icon ic_x
│   └── ScrollView         [ScrollRect, Vertical Only]
│       ├── Viewport      [Mask]
│       │   └── Content    [VerticalLayoutGroup NON usato - posizioni manuali, vedi §4]
│       │       ├── Header_PartitaVeloce
│       │       ├── Row_1v1
│       │       ├── Row_2v2
│       │       ├── Row_1v3
│       │       ├── Header_Allenamento
│       │       ├── Row_1v1Bot (selected=true)
│       │       ├── Row_2v2Bot
│       │       ├── Row_1v3Bot
│       │       ├── DifficoltaLabel
│       │       ├── DifficoltaPills (3 pill orizzontali)
│       │       ├── Header_StanzaPrivata
│       │       └── Row_Crea / Row_Entra (affiancate)
│       └── FadeBottom     [Image gradiente, indica scroll disponibile]
```

---

## 2. Cornice pannello (PanelFrame)

- Anchor: center, Pos (0,0) — ma dato in coordinate assolute canvas:
- Box: **X 44 → 1036, Y 250 → 1640** (larghezza 992, altezza 1390)
- Riempimento: colore pieno `#182838` (RGB 24,40,64)
- Bordo doppio: 14px esterno colore `#785018` (GOLD_D), 8px interno sopra `#E8B24A` (GOLD) — se avete già un componente `ThemedPanel` con bordo dorato spesso, usate quello invece di ricrearlo
- Ombra: leggero drop shadow verso il basso (offset Y +14, blur ~22), opacità bassa — cosmetico, non bloccante se saltato

## 3. Titolo e chiusura

| Elemento | Sprite | Box (X0,Y0 → X1,Y1) | Note |
|---|---|---|---|
| TitleRibbon | `ribbon_teal`, Sliced | centrato X, top **Y=194**, W=520, H=**188** | Testo "MODALITÀ" TMP 40 Bold, colore Cream, stroke 5 scuro |
| CloseButton | `sq_blue`, Sliced | **X=932, Y=276**, W=86, H=86 | Icona `ic_x` centrata, H=46 |

## 4. ScrollView

- Viewport: **X=76, Y=390 → X=984, Y=1608** (W=908, H=1218)
- Content: larghezza 908, **altezza totale 1342** (più alto del viewport → scroll verticale attivo)
- Tutte le coordinate sotto sono **offset dal margine superiore del Content** (Y=0 = cima del Content)

### Righe modalità (component riutilizzabile `ModeRow`)
Struttura interna di ogni riga:
```
ModeRow [Image: btn_teal se selected, altrimenti btn_blue_long — Sliced]
├── Icon       [X + 30 dal bordo sinistro, H = 50% dell'altezza riga, centrata verticalmente]
├── TitleText  [TMP 28 Bold, Cream, a sinistra dopo l'icona +24px]
├── SubText    [TMP 19 Medium, TextMuted, sotto TitleText]
└── CheckIcon  [solo se selected: ic_check, H=44% altezza riga, allineato a destra -26px]
```
Tutte le righe hanno **larghezza piena (888px, cioè Content_W - 20)** tranne Crea/Entra che sono affiancate a metà ciascuna.

| Riga | Y offset (top) | Altezza | Larghezza | Selected |
|---|---|---|---|---|
| Header "PARTITA VELOCE" | 24 | (testo) | full | — |
| Row "1 vs 1" | 52 | 126 | 888 | no |
| Row "2 vs 2" | 194 | 126 | 888 | no |
| Row "1 vs 3" | 336 | 126 | 888 | no |
| Header "ALLENAMENTO (BOT)" | 520 | (testo) | full | — |
| Row "1 vs 1 BOT" | 548 | 126 | 888 | **sì** |
| Row "2 vs 2 BOT" | 690 | 126 | 888 | no |
| Row "1 vs 3 BOT" | 832 | 126 | 888 | no |
| Label "Difficoltà" | 1006 | (testo) | — | — |
| Pills difficoltà | 1032 | 76 | 3× 285, gap 16 | vedi sotto |
| Header "STANZA PRIVATA" | 1164 | (testo) | full | — |
| Row "Crea" | 1192 | 126 | 434 (metà sinistra) | no |
| Row "Entra" | 1192 | 126 | 434 (metà destra, gap 20 dalla sinistra) | no |

**Gap tra header e riga sotto**: 44px (già incluso negli offset sopra). **Gap tra righe consecutive dello stesso gruppo**: 16px (differenza tra fine riga e inizio successiva: es. Row 1vs1 finisce a 52+126=178, Row 2vs2 inizia a 194 → gap 16).

### Pills difficoltà (component `DifficultyPill`)
- 3 pill uguali, larghezza **285** ciascuna, gap **16** tra loro, altezza **76**
- X offset: Pill1 = 10, Pill2 = 311, Pill3 = 612
- Sprite: `btn_green_small` se attiva (Sliced), `btn_gray_small` se inattiva
- Testo centrato, TMP 25 Bold
- Stato di default nel mockup: **"Medio" attiva**, le altre due no

## 5. Comportamento scroll

- Alla fine del Content (dopo Row "Entra") c'è un margine di **24px** prima del bordo
- `FadeBottom`: overlay gradiente 90px di altezza, ancorato in basso al Viewport (non al Content — resta fisso mentre si scrolla), indica che c'è altro sotto
- Scrollbar verticale opzionale a X = Viewport_X1 + 16, sottile (10px), colore oro

---

## 6. Cosa NON fare in questo passaggio

- Non collegare la logica di selezione reale (quale modalità è effettivamente scelta) — solo lo stato visivo di default sopra
- Non implementare ancora l'azione dei pulsanti Crea/Entra
- Costruire in scena separata o come overlay disattivato di default in HomeScreen, non sovrascrivere nulla

## 7. Verifica finale

1. Aprendo il pannello, "1 vs 1 BOT" appare evidenziato in teal con il segno di spunta
2. "Medio" è la pillola attiva tra le tre difficoltà
3. Scrollando fino in fondo, "Crea" ed "Entra" sono completamente visibili e affiancate, non sovrapposte
4. Il pulsante di chiusura in alto a destra non si sovrappone al ribbon del titolo
