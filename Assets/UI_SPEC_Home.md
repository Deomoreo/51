# UI Spec — Schermata Home (Gioca)

Documento di riferimento per costruire la schermata Home in Unity.
Mockup di riferimento: `home_B2.png` (video loop a tutto schermo).

---

## 0. Convenzioni

- **Risoluzione di progetto**: 1080 × 1920 (portrait)
- **CanvasScaler**: `Scale With Screen Size`, Reference Resolution `1080x1920`, `Match Width Or Height = 0.5`
- Tutte le coordinate sotto sono in **pixel della risoluzione di riferimento**
- Gli **ancoraggi** sono la parte critica: non usare posizioni assolute su elementi che devono restare attaccati a un bordo
- Testi: `TextMeshProUGUI`, font Poppins (Bold per titoli/valori, Medium per il resto)

---

## 1. Sprite — cartella e import

Percorso: `Assets/UI/Sprites/DragonsHoard/`

Per **ogni** PNG:
- `Texture Type: Sprite (2D and UI)`
- `Sprite Mode: Single`
- `Mesh Type: Full Rect`
- `Alpha Is Transparency: true`
- `Border L/R/T/B`: leggere da `import_manifest.json` nella stessa cartella

Nel manifest, `type: sliced` indica che l'Image che lo usa deve avere `Image.Type = Sliced`.
`type: simple` → `Image.Type = Simple` e `Preserve Aspect = true` (icone).

---

## 2. Gerarchia della scena

```
Canvas (Screen Space - Overlay)
└── VideoBackground          [RawImage, stretch totale]
└── SafeArea                 [SafeAreaFitter esistente]
    ├── TopBar
    │   ├── PlayerBadge
    │   │   ├── AvatarFrame  [Image: avatar_frame]
    │   │   ├── LevelBadge   [Image: ic_coin_clover]
    │   │   │   └── LevelText [TMP]
    │   │   └── NameText     [TMP]
    │   ├── CoinBar
    │   │   ├── Background   [Image: bar_coin, Sliced]
    │   │   └── ValueText    [TMP]
    │   ├── GemBar
    │   │   ├── Background   [Image: bar_energy, Sliced]
    │   │   └── ValueText    [TMP]
    │   └── XpBar
    │       ├── Background   [Image: bar_empty, Sliced]
    │       └── ValueText    [TMP]
    ├── RightRail
    │   ├── RewardButton     [Button + ThemedButton(IconTab)]
    │   ├── LeaderboardButton
    │   └── MailButton
    ├── BottomSection
    │   ├── ModeSelector     [Button + ThemedButton(Secondary)]
    │   ├── DeckSelector     [Button + ThemedButton(Secondary)]
    │   └── PlayButton       [Button + ThemedButton(Primary)]
    └── NavBar
        ├── ActiveTabHighlight [Image: tab_gold]
        └── TabGioca / TabCarte / TabNegozio / TabProfilo
```

---

## 3. Ancoraggi e misure

### VideoBackground
- Anchor: `stretch/stretch`, offset 0 su tutti i lati
- Fuori da SafeArea: deve coprire anche notch e bordi

### TopBar
- Anchor: `top/stretch`, Left 0, Right 0, Height **150**, PosY 0

| Elemento | Anchor | Pos/Size |
|---|---|---|
| PlayerBadge | top-left | X 24, Y -30, W 180, H 150 |
| AvatarFrame | stretch | riempie PlayerBadge |
| LevelBadge | top-left di PlayerBadge | X -8, Y 4, W 52, H 52 |
| NameText | bottom-center di PlayerBadge | Y 26, W 140, H 34, **Auto Size 15–24, Overflow: Ellipsis** |
| CoinBar | top-left | X 232, Y -40, W 280, H 70 |
| GemBar | top-left | X 530, Y -40, W 214, H 70 |
| XpBar | top-right | X -22, Y -40, W 218, H 70 |

> **NameText**: Auto Size + Ellipsis sono obbligatori, non opzionali — vedi test nomi lunghi.

### RightRail
- Anchor: `top-right`, X -24, Y -240, W 112
- `VerticalLayoutGroup`: spacing 34, childAlignment UpperCenter
- Ogni bottone: W 112, H 112, sprite `sq_blue` (Sliced)
- Icona figlia: H 66, centrata, `Preserve Aspect`
- Etichetta sotto: TMP 17, Y -16 rispetto al bottone
- Badge notifica (opzionale): cerchio rosso 32×32 ancorato top-right del bottone

### BottomSection
- Anchor: `bottom/stretch`, Left 0, Right 0, Height **420**, PosY 172 (sopra la NavBar)

| Elemento | Anchor | Pos/Size |
|---|---|---|
| ModeSelector | bottom-left | X 60, Y 214, W 468, H 132 |
| DeckSelector | bottom-right | X -60, Y 214, W 468, H 132 |
| PlayButton | bottom-center | Y 40, W 560, H 148 |

**Struttura interna dei selettori** (ModeSelector / DeckSelector):
```
Selector [Image: btn_blue_long (Mode) / btn_teal (Deck), Sliced]
├── Icon      [Image: ic_gamepad / ic_cards, H 68, left, X 26]
├── LabelText [TMP 20 Medium, colore TextMuted, left]  → "MODALITÀ" / "MAZZO"
└── ValueText [TMP 27 Bold, colore Cream, left]        → "Allenamento" / "Napoletano"
```
`ValueText` è quello che cambia a runtime in base alla selezione.

**PlayButton**: sprite `btn_gold_long` (Sliced), testo "GIOCA" TMP 54 Bold, colore `#3A2208`.
Aggiungere un `Image` figlio con `glow_soft` dietro (alpha ~0.4, tinta Gold), non interattivo.

### NavBar
- Anchor: `bottom/stretch`, Left 0, Right 0, Height **172**, PosY 0
- Sfondo: Image colore `#0D1626` opaco + linea superiore 4px `#3A5880`
- 4 tab con `HorizontalLayoutGroup`, childForceExpandWidth
- `ActiveTabHighlight`: Image `tab_gold`, W 248, H 96, si sposta sul tab attivo
- Ogni tab: icona H 70 + etichetta TMP 21
- Colore icona/testo: attivo `#FFE49C`, inattivo `#94ACCA`

---

## 4. Cosa NON fare in questo passaggio

- Non implementare il video loop: mettere un `RawImage` con un colore/immagine statica come segnaposto
- Non collegare la logica dei bottoni: solo la UI
- Non toccare `MainMenu.unity` esistente — costruire in una **scena nuova** `HomeScreen.unity` per confronto affiancato

---

## 5. Verifica finale

1. La scena si vede corretta a 1080×1920
2. Cambiando Game view a 1170×2532 (iPhone) e 1440×3120 (Android alto): la TopBar resta in alto, la NavBar in basso, il PlayButton non si sovrappone alla NavBar
3. Con nome giocatore `GiocatoreItalianoMoltoLungo` il testo si rimpicciolisce e poi tronca con `…`, senza uscire dalla targa
4. Nessun errore in Console
