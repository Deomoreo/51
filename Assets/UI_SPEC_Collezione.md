# UI Spec — Sezione Collezione (Mazzi / Emoticon / Accusi)

Ogni numero in questo documento è preso **letteralmente** dal codice Python
che ha generato i tre mockup — non è una lettura a occhio degli screenshot.
Dove il codice usa una formula (es. `W-76`), la formula è riportata così
com'è: `W` = 1080, `H` = 1920.

Le tre schermate condividono `TopBar` e `NavBar` (identiche), e le due
schede Emoticon/Accusi condividono anche la `TabBar`. Mazzi (pagina Carte)
oggi NON ha la TabBar nel mockup originale — vedi nota in fondo §0.

---

## 0. Nota sulla TabBar — leggere prima di tutto

Il mockup "pagina Carte" (`20_pagina_carte.png`) è stato disegnato **prima**
di introdurre il concetto MAZZI/EMOTICON/ACCUSI, quindi non ha la TabBar in
cima. I mockup Emoticon/Accusi ce l'hanno. Per coerenza, quando build i
tre insieme: **aggiungi la TabBar (§3) anche sopra la pagina Mazzi**,
spostando "IN USO" più in basso della stessa quantità con cui è spostato
negli altri due (confronta §4: "IN USO" a Y=348 in Emoticon/Accusi contro
Y=240 in Mazzi — la differenza, 108px, è lo spazio della TabBar+margine).

---

## 1. Palette (usata identica in tutte e tre)

| Nome | RGB | Uso |
|---|---|---|
| `GOLD` | 232,178,74 | bordi attivi, testo valore, accenti |
| `GOLD_L` | 255,236,190 | testo dorato chiaro (nomi mazzi/animazioni sbloccati) |
| `TXT` | 255,250,235 circa | testo primario su sfondi scuri |
| `TXT_DIM` | testo secondario attenuato | sottotitoli |
| `LINE` | ~70,102,142 | bordo pannelli/celle non attive |
| Sfondo cella sbloccata | 26,44,68,252 | celle/righe possedute |
| Sfondo cella bloccata | 14,24,40,240–246 | celle/righe bloccate |
| Sfondo riga scura (Accusi) | 16,28,46,244 | righe lista Accusi non possedute |

> Nota pratica: `GOLD`/`GOLD_L`/`TXT`/`TXT_DIM`/`LINE` sono costanti definite
> una volta in `ui_kit.py` e riusate ovunque. Se in Unity avete già un
> `UITheme` con questi stessi nomi (dal lavoro sulla Home), **riusate quelle**,
> non ridefinitele qui.

---

## 2. Sfondo, TopBar, NavBar (identici nelle tre schermate)

### Sfondo (`bg_gradient_app`)
Gradiente verticale da (24,42,68) in alto a (9,17,30) in basso, con un
bagliore radiale caldo centrato leggermente sopra il centro (offset Y +0.4
in coordinate normalizzate, colore aggiunto (46,36,12)). Se in Unity avete
già l'immagine di sfondo della Home, riusatela — è identica.

### TopBar (`app_topbar`)
| Elemento | Box (X,Y,W) | Contenuto |
|---|---|---|
| AvatarFrame | 24, 30, W=180 | sprite `avatar_frame` |
| NameText | centro X=114, Y=fondo avatar-25 | "Deomoreo" — dinamico, TMP 21 |
| LevelIcon | 16, 26, W=52 | sprite `ic_coin_clover` |
| LevelText | centro 42,52 | "1", TMP 24 |
| CoinBar | 232, 40, W=280 | sprite `bar_coin` |
| CoinValueText | X=316 | "1.000" — dinamico, TMP 25 |
| GemBar | 530, 40, W=214 | sprite `bar_energy` |
| GemValueText | X=610 | "50" — dinamico, TMP 24 |
| XpBar | 802, 40, W=218 | sprite `bar_empty` |
| XpValueText | X=911 | "0 / 100" — dinamico, TMP 21 |

*(Identica a UI_SPEC_Home.md — se già costruita lì, riusare lo stesso
prefab/componente TopBar invece di ricrearlo.)*

### NavBar (`app_navbar`)
- Y inizio: **1748**, altezza fino a 1920 (172px)
- Sfondo (13,22,38,255), linea superiore 4px (58,88,128,255)
- Tab attivo: sprite `tab_gold`, W=248, slot = W/4 = 270, Y=1736
- 4 slot: Gioca(`ic_gamepad`) / Carte(`ic_cards`) / Negozio(`ic_cart`) / Profilo(`ic_person`)
- Icona: H=70, Y=1774; Label: TMP 21, Y=1874
- Colore label/icona: attivo (255,228,156), inattivo (148,172,202)
- **Per questa sezione, il tab attivo è sempre "Carte" → `active=1`**

---

## 3. TabBar interna (MAZZI / EMOTICON / ACCUSI)

- Posizione Y: **210**
- 3 pulsanti uguali, larghezza `tw = (W-120-2*14)/3 = 284`, altezza **84**
- X del pulsante i: `60 + i*(tw+14)` → **Mazzi X=60 · Emoticon X=358 · Accusi X=656**
- Sprite: attivo = `btn_teal` (Sliced), inattivo = `btn_gray_small` (Sliced)
- Testo centrato: TMP 23 Bold, colore `TXT` se attivo / (196,208,224) se inattivo, stroke 3 (8,18,34)
- Label testo Y: centro verticale del pulsante (y+42 = 252)

---

## 4. Sezione "IN USO" — varia per scheda

### 4A. Mazzi — riquadro anteprima grande
- Con TabBar aggiunta: label "IN USO" a Y=**348**
- Riquadro: box **(50, 382 → 1030, 850)**
  - Sfondo (18,32,52,252), bordo oro 5px, bagliore dietro (blur 30, colore 255,196,70,70 alpha)
- Badge "IN USO": pillola oro W=180 H=48, centrata X, Y=`pv0+18`
- Ventaglio 3 dorsi: altezza carta **270px**, centro ventaglio Y=`pv0+230`, offset X tra le tre: **-140/0/+140**, rotazione **+15°/0°/-15°**
- Nome mazzo: TMP 34, Y=`pv1-92`
- Sottotitolo "Mazzo standard · 40 carte": TMP 21 Medium, Y=`pv1-46`

### 4B. Emoticon — 3 slot equipaggiabili
- Label "EQUIPAGGIATE · 3 max": Y=**348**
- Slot: Y=**382**, altezza **230**, larghezza `slot_w = (W-120-2*24)/3 = 304`
- X slot i: `60 + i*(slot_w+24)` → 0:X=60 · 1:X=388 · 2:X=716
- **Slot pieno**: sfondo (26,44,68,252), bordo oro 5px, bagliore dietro; emoticon 150×150 centrata Y=`sy+26`; nome sotto Y=`sy+200`; bottone rimuovi (cerchio rosso 48×48 con "×") in alto a destra dello slot
- **Slot vuoto**: bordo tratteggiato (segmenti 12px ogni 24px) colore (70,100,140), icona "+" stilizzata al centro (cerchio 76×76 + croce), testo "Slot libero" TMP 19 Medium (120,146,178)

### 4C. Accusi — card grande singola
- Label "IN USO": Y=**348**
- Riquadro: box **(50, 382 → 1030, 706)**, stesso stile bagliore+bordo oro di 4A
- Icona animazione: cerchio 168×168 (raggio 84), centro **(190, 500)**, colore riempimento dipende dall'animazione (rosso 196,54,44 per "Pugno"), bordo oro 6px
  - Raggi d'impatto: 8 raggi a 45° l'uno dall'altro, da raggio 58 a raggio 76 dal centro, spessore 6px, colore (255,214,120)
  - Glifo pugno: rettangolo arrotondato radius 16 + 4 nocche circolari + pollice — vedi script sorgente per la geometria esatta se va ridisegnato vettoriale
- Testo titolo: X=320 (allineato a sinistra, non centrato), Y=454, TMP 30 Bold
- Sottotitolo stato: Y=502, TMP 20 Medium
- Descrizione (2 righe): Y=556 e Y=592, TMP 21 Medium
- Bottone "ANTEPRIMA": box **(330, 630 → 640, 688)**, sprite `btn_blue_long`, TMP 22

---

## 5. Sezione "COLLEZIONE" — griglia o lista

### 5A. Mazzi — griglia 3 colonne
- Label + contatore "N / 9": Y=**808** (verificare offset coerente se si aggiunge la TabBar, vedi §0)
- Barra progresso: box **(60, 840 → 1020, 872)**
- Griglia: `gy=906`, 3 colonne, cella **W=305, H=300**, gap orizzontale 22, gap verticale 34
- Cella X: col0=60, col1=387, col2=714
- Cella Y: riga0=906, riga1=1240
- **Struttura cella**: sfondo (sbloccato 26,44,68,252 / bloccato 14,24,40,246), bordo (in uso: oro 5px+bagliore / altro: LINE 3px), arte dorso carta (H=164, scurita se bloccata + lucchetto `ic_lock` H=54 sovrapposto), nome (TMP 22), riga stato in basso (badge testo "In uso" dorato / bottone "USA" `btn_teal` / bottone costo `btn_blue_long`), check ✓ in alto a destra se in uso (`ic_check` H=48)
- Nota in fondo griglia: TMP 19 Medium (128,152,184), Y = `gy + 2*(300+34) + 30`

### 5B. Emoticon — griglia 4 colonne
- Label + contatore "N / 12": Y=**690**
- Barra progresso: box **(60, 722 → 1020, 754)**
- Griglia: `gy=790`, 4 colonne, cella **W=210, H=208**, gap orizzontale 20, gap verticale 22
- Cella X: 60, 290, 520, 750
- Cella Y: riga0=790, riga1=1020, riga2=1250
- **Struttura cella**: sfondo/bordo come sopra (equipaggiata = bordo oro 4px + check in alto a destra); emoticon 104×104 centrata Y=`y0+22` se posseduta, altrimenti `ic_lock` H=56 centrato Y=`y0+44`; nome/"Bloccata" sotto, TMP 17 Medium, Y=`y0+158`

### 5C. Accusi — lista verticale (non griglia)
- Label + contatore "N / 6": Y=**762**
- Barra progresso: box **(60, 794 → 1020, 826)**
- Righe: `ry=862`, altezza riga **128**, passo tra righe **146** (gap 18px)
- Riga i: Y = `862 + i*146`
- **Struttura riga**: sfondo pieno-larghezza (60→1020), badge icona circolare 84×84 (raggio 42) a X=130 relativo al centro riga, colore diverso per riga (personalizzabile), lucchetto sovrapposto se non posseduta; testo nome X=196 (TMP 25) + descrizione sotto (TMP 19 Medium); a destra: "IN USO" (TMP 20 dorato) oppure bottone "USA" (`btn_teal`) oppure bottone costo (`btn_blue_long`), box bottone W=166 H=54

---

## 6. Componenti riutilizzabili da creare

```
CollectionCell (per Mazzi e Emoticon — layout a griglia)
├── Background      [Image Sliced, colore/bordo secondo stato]
├── GlowOverlay      [solo se equipaggiato/in uso]
├── ContentArt      [Image: dorso carta / emoticon / icona — dipende dal tab]
├── LockIcon         [solo se bloccato]
├── NameLabel        [TMP]
├── StatusRow        [badge testo "In uso" / bottone azione]
└── CheckBadge       [solo se equipaggiato/in uso]

CollectionRow (per Accusi — layout a lista)
├── Background
├── IconBadge        [cerchio colorato + eventuale lucchetto]
├── NameLabel + DescLabel
└── ActionArea        [IN USO testo / bottone USA / bottone costo]
```

---

## 7. Cosa NON fare in questo passaggio

- Non collegare ancora la logica reale di sblocco/acquisto (monete/gemme che si scalano) — solo lo stato visivo
- Non implementare l'animazione vera del pugno dietro al bottone "ANTEPRIMA" — il bottone deve esistere ed essere pronto, l'aggancio all'animazione reale è un passaggio successivo
- I nomi delle animazioni Accusi diversi da "Pugno sul tavolo" (Tuono, Pioggia d'oro, Vortice) sono segnaposto — non creare asset per queste, sono solo righe bloccate di esempio

## 8. Verifica finale

1. Le tre schede si raggiungono dalla stessa pagina "Carte" della NavBar, cambiando solo la TabBar interna
2. Nella scheda Mazzi, il mazzo "in uso" nel riquadro grande corrisponde a quello con il check ✓ nella griglia sotto
3. Nella scheda Emoticon, non è possibile equipaggiarne una quarta se i 3 slot sono pieni — decidere il comportamento (blocco o sostituzione) prima di implementare
4. Nella scheda Accusi, solo una riga alla volta può mostrare "IN USO"
5. Su tutte e tre, il tab attivo nella TabBar interna resta evidenziato correttamente cambiando scheda
