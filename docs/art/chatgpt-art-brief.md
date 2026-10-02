# Brief artistico per ChatGPT — 51 (29 settembre 2026)

Come si usa: le sezioni 1-2 vanno incollate come primo messaggio di ogni chat
(insieme alle immagini di riferimento della sezione 3). Poi un messaggio per asset,
con il modello della sezione 4. I blocchi `PROMPT` sono in inglese di proposito:
i generatori di immagini seguono meglio l'inglese e sbagliano meno le scritte.

## 0. Metodo di lavoro (quello che ha già funzionato: fogli icone, kit tavolo, Giada)

1. **Una chat per famiglia** (mazzi / tavoli / accusi / banner / avatar-emoticon). Lo stile si stabilizza dopo 2-3 immagini.
2. **Un foglio per volta**, con tutti gli elementi in griglia, mai immagini singole: stile, scala e luce restano identici.
3. **Correzioni**: «cambia solo X, tutto il resto identico». Non rigenerare il foglio intero.
4. **Sfondo**: chiedi trasparente; se arriva bianco o a scacchi, rigenera su magenta piatto `#FF00FF` e toglilo in Photopea.
5. **Niente testo nelle immagini.** Nomi, numeri e prezzi li scrive Unity. Unica eccezione tollerata: il cartiglio dell'Asso.
6. Dopo la generazione: Upscayl x2, ritaglio in Photopea, poi passa i file a Claude: import con la ricetta del progetto (mazzo: PPU = altezza/1.8; tavolo: 9-slice; emoticon: fogli 4×2).
7. **Le carte 2-7 non le fa ChatGPT.** Si generano cornice, simboli e figure separati; Claude compone le 40 carte con uno script (disposizione dei simboli identica su tutte). È la pipeline della Corte di Giada.

## 1. Il gioco (da incollare)

```
"51" is a mobile card game (portrait, Unity) played with the traditional Italian
40-card deck: four suits — Denari (coins), Coppe (cups), Bastoni (clubs/batons),
Spade (swords) — ranks Ace to 7, then Fante (page), Cavallo (knight on horse), Re (king).
Scopa-style: players capture table cards, score "scope" (sweeps) and can announce an
"accuso" (a combination that earns bonus points); each accuso triggers a short table
animation. 2-4 players online with friends, private rooms, or against bots.
Audience: Italian players, teenagers to adults. Tone: warm, friendly, polished,
premium but clean. NOT a casino, NOT a slot machine.
Players customise: card decks, table + room, accuso animations, player banners
(the name plate seen by opponents), avatars, emoticons.
```

## 2. Bibbia dello stile (da incollare)

```
ART STYLE (match the attached references exactly): illustrated storybook / cel-shaded
cartoon. Clean thin dark-brown outlines, soft gradient shading, rich saturated colours,
warm gold highlights, light from the top-left, short soft drop shadow.

PALETTE: gold #E8B24A (highlight #FCE29A) · cream parchment #F3E6C8 / #FAF4E0 ·
deep navy #0B1426 → #1C2E52 · emerald felt #124032 · jade #0E4F3A → #146A50 ·
burgundy #4A1420 → #5C1A2A · mahogany wood with thin gold inlay.

HARD RULES: no text, letters, numbers, logos, signatures or watermarks in the artwork
(unless I ask). No photorealism. No real brands, celebrities, football clubs or existing
characters. No gambling iconography: no roulette, dice, slot bars, dollar signs, poker chips.
Every asset is a game-ready cut-out: centred, fully visible, generous margin, flat pure
magenta #FF00FF background (or transparent if possible), no cast shadow on the background,
no scenery unless asked. Same scale and lighting across a sheet.

METHOD: when I ask for a set, produce ONE sheet with all elements in a grid so the style
stays identical. When I ask for a fix, change only what I say and keep everything else
identical.
```

## 3. Riferimenti da allegare (percorsi nel progetto)

| Famiglia | File |
|---|---|
| Mazzi | `Assets/Art/Decks/51_CORTE_DI_GIADA_PNG_Unity/Anteprime/51_GIADA_Anteprima_COPPE.jpg` (+ una seconda anteprima) |
| Tavoli | `Assets/Art/Table/table_frame.png`, `table_felt.png`, `table_vignette.png` + lo sfondo della sala consegnato il 25/09 |
| Accusi | `Assets/UI/Sprites/DragonsHoard/sprites_unity/sprites_unity/PugnoIcon.png` |
| Banner | `Assets/Art/firstBanner 1.png`, `Assets/Art/SecondBanner.png` |
| Avatar / emoticon | `Assets/UI51/Art/Avatars/av_2.png`, `Assets/UI51/Art/Emoticons/emo_risata_sheet_8frames.png` |
| Negozio | `Assets/UI51/Art/Common/chest_purple.png`, `chest_green.png` |
| Atmosfera Home | `Assets/UI51/Art/Backgrounds/home_bg_base.png` |

## 4. Specifiche e modelli per famiglia

### 4.1 Mazzi (40 facce + dorso, PNG RGBA 963×1419, angoli trasparenti)

Rapporto 2:3. Genera a 1024×1536 e lascia a Claude il ridimensionamento a 963×1419.
Cinque fogli per un mazzo completo (gli Assi vanno con i simboli, ingranditi):

1. **Cornice + fondo vuoti** (1 immagine). Centro calmo, senza illustrazione; bordo ≤ 7% della larghezza.
2. **Simboli dei 4 semi** (foglio 4×2: riga 1 taglia "pip", riga 2 taglia Asso).
3. **Figure**: 12 (Fante, Cavallo, Re × 4 semi), foglio 4×3, a figura intera, stesse proporzioni.
4. **Dorso**.
5. (Facoltativo) **Cartigli** per l'Asso.

```
PROMPT (frame) — Empty playing-card face template, portrait 2:3, rounded corners,
{THEME} ornamental border: thin gold outer line, inner {COLOUR} band, {MOTIF} in the four
corners. The centre is an empty, calm {PAPER} texture with no illustration. Border no more
than 7% of the card width. Flat #FF00FF background, 3% margin.

PROMPT (suits) — One sheet, 4 columns × 2 rows, on flat #FF00FF. Columns: Denari (coin with
embossed {MOTIF}), Coppe (goblet), Bastoni (knotted wooden club), Spade (curved sword).
Row 1: small "pip" size. Row 2: the same four, large, for the Ace. Colour coding: {COLOURS}.
Same outline weight and lighting on all eight. No text.

PROMPT (figures) — One sheet, 4 columns (Denari, Coppe, Bastoni, Spade) × 3 rows (Fante = young
page, Cavallo = knight on horse, Re = king). Full-body, {THEME} costumes, each holding or
marked with the suit symbol of its column, same proportions and scale in every cell,
flat #FF00FF background, no text, no frames.

PROMPT (back) — Card back, portrait 2:3, symmetrical (180° rotation looks identical),
{THEME} pattern in {COLOURS}, thin gold border, centred medallion with a suit-neutral emblem
(no letters or numbers). Flat #FF00FF background.
```

### 4.2 Tavoli (kit di 3 pezzi + sfondo sala)

Regole dal codice (`UIV2FoundationBuilder.RoomTable.cs`):
- **Cornice**: usata a **9-slice con bordo 150 px** su 634×632. Ornamenti solo dentro il 24% esterno di ogni angolo; lati uniformi e ripetibili, **nessun ornamento al centro di un lato**; stesso spessore sui 4 lati; interno completamente vuoto.
- **Panno**: 506×481, viene stirato. Nessun bordo né ornamento vicino ai lati; centro un po' più chiaro; texture morbida (dietro le carte crema con contorno oro deve restare leggibile: niente motivi grandi).
- **Vignetta**: 579×554, ombra scura ai bordi, centro trasparente.
- **Sfondo sala**: verticale 9:16 (consegna precedente 1882×3344). Il tavolo copre il centro: dettagli verso i bordi, centro calmo e scuro.
- Panno: tonalità media-scura e satura, lontana da crema e oro (le carte devono staccare). Prova su carte vere prima di approvare.

```
PROMPT (kit) — One wide sheet, 3 items side by side on flat #FF00FF:
(1) a rounded-square table frame seen from above, {WOOD/MATERIAL} with thin gold inlay, equal thickness on
all four sides, {MOTIF} ornaments ONLY in the four corners, straight edges perfectly plain and
repeatable, completely empty transparent-looking hole in the middle;
(2) a square top-down swatch of {FELT/CLOTH} in {COLOUR}, soft radial light, no border;
(3) a soft dark radial vignette ring, empty centre.
Same lighting (top-left) on all three.

PROMPT (room) — Portrait 9:16 empty {THEME} game room seen from above at a slight angle,
no people, no text, no table in the centre (leave a calm, darker central area), rich details
only near the edges, warm light, deep navy and gold accents, illustrated storybook style.
```

### 4.3 Accusi (animazioni dell'accuso)

Oggi l'accuso base è il «Pugno sul tavolo» (il pugno batte due volte, le carte saltano; `AccusoImpactV2.cs`). Il movimento lo fa Unity (tween + particelle): **ChatGPT dà i pezzi, non l'animazione.**
Per ogni accuso servono:
1. **Medaglione** 512×512 (tondo, usato a ~85 e ~170 px nella Collezione, riconoscibile a 64 px).
2. **2-4 elementi eroe** trasparenti (il pugno, il fulmine, la moneta...).
3. Facoltativo: **foglio 4×2 da 8 fotogrammi** (1024×512, stesso formato delle emoticon), solo per loop semplici (fiamma, scintilla).

```
PROMPT (medallion + parts) — One sheet on flat #FF00FF. Left: a round badge 512×512 for the
"{NAME}" table effect: {DESCRIPTION OF THE ICON}, thick gold rim, {BACKGROUND COLOUR} disc,
readable at 64px, no text. Right: {N} separate hero elements for the animation
({ELEMENTS}), each isolated, same style, large and simple, no motion blur.
```

### 4.4 Banner del giocatore

Oggi i 6 banner (Notte, Smeraldo, Porpora, Aurora, Stellato, Oro) sono **gradienti dello shader**, senza illustrazione: qui l'arte fa più differenza. Si aggiunge uno **strato ornamento** trasparente sopra la pillola (raggio 25):
- Striscia 4:1: genera su tela 1536×1024 con la striscia centrata, poi ritaglio a 2048×512.
- **Zone riservate**: a sinistra ~22% per il cerchio dell'avatar; centro-destra per nome e punti. Gli ornamenti vanno agli estremi (terminale destro, filetti sopra/sotto) o sono a bassa opacità nella zona del testo.
- Lo sfondo colorato lo mette lo shader: l'ornamento deve funzionare su fondi scuri diversi.

```
PROMPT — One sheet on flat #FF00FF with {N} horizontal name-plate ornament overlays,
each a 4:1 strip stacked vertically. Each is a thin ornamental frame/filigree for a rounded
pill-shaped player banner: gold line work, {MOTIF} at the right end, small flourishes on the
top and bottom edges, the left 22% and the whole central band left clear and low-detail so
text stays readable. Transparent inside the pill. Themes: {LIST}. No text.
```

Banner promozionali (Negozio, Pass, novità): stesso stile, ma a parte. Prima chiarisci il formato con Claude.

### 4.5 Avatar, emoticon e negozio

- **Avatar**: busto 4:5 (i file attuali sono ~356×424), soggetto centrato, sfondo teal piatto. **Senza cornice**: l'app ritaglia (132% dell'interno) e aggiunge il proprio anello.
- **Emoticon**: foglio 1024×512 = 4×2 = 8 fotogrammi da 256×256, 10 fps; stesso personaggio, piccole variazioni di posa (loop). Una sola faccina per foglio.
- **Negozio**: pacchetti monete/gemme e scrigni (`chest_*` sono 186×158, da rifare più grandi se si aggiungono fasce). Niente scrigni che promettano ricompense casuali ("cassa misteriosa"): gli store richiedono di dichiarare le probabilità, meglio vendere gli oggetti direttamente.

## 5. Catalogo proposto (da approvare, niente è ancora deciso)

Temi con forte identità italiana, sicuri sul piano dei diritti:

| Tema | Mazzo | Tavolo | Accuso | Banner |
|---|---|---|---|---|
| Vendemmia e notte delle streghe (ottobre, subito stagionale) | grappoli, zucche, luna | tavola di legno scuro, pergolato | Pipistrelli | Porpora + ragnatele oro |
| Carnevale di Venezia | maschere, oro e borgogna | palco di teatro | Coriandoli e sipario | Oro con maschera |
| Natale napoletano | presepe, stelle | botteguccia del presepe | Stella cometa | Rosso e verde |
| Estate in Costiera | limoni, ceramica | terrazza sul mare | Onda | Aurora |
| Carretto siciliano | motivi dipinti a mano | carretto | Ruota | Colori vivi |
| Commedia dell'arte | maschere Arlecchino/Pulcinella | palcoscenico | Schiaffo di scena | Losanghe |

Accusi da aggiungere a Pugno / Tuono / Pioggia d'oro / Vortice: **Scopa che spazza** (una scopa spazza le carte: gioco di parole sul nome del gioco), Fuochi d'artificio, Sigillo di ceralacca, Sipario, Fanfara.

Unità di lavoro per un kit tematico completo: ~10-12 generazioni (mazzo 4, tavolo 2, accuso 1-2, banner 1, emoticon 2-4).
