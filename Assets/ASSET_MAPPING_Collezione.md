# Mappatura Asset — Collezione (letterale, zero interpretazione)

Questo non è un documento di layout (quello è `UI_SPEC_Collezione.md`, resta
valido per le posizioni). Questo è l'elenco esatto di **quale asset e quale
colore usare per ogni singolo elemento**, così com'è nel codice che ha
generato i mockup — riga per riga, nessuna sintesi.

**Regola generale valida per tutto il documento**: quando dico "FILL" intendo
`panel_fill_r24.png` o `panel_fill_r30.png` (allegati), `Image.type = Sliced`,
colore = `Image.color`. Quando dico "RING" intendo `panel_ring_r24.png` /
`panel_ring_r30.png`, stesso discorso, sovrapposto SOPRA il FILL, stessa
dimensione esatta. **Mai usare `btn_teal`/`btn_blue_long`/`btn_gold_long`/
`btn_gray_small`/`sq_blue`/`sq_gold` per uno sfondo di pannello/cella/riga.**
Quegli sprite si usano **solo** per i bottoni veri elencati nella sezione 5.

Colori in formato RGBA 0-255.

---

## 1. Elementi comuni a tutte e tre le schede

| Elemento | Asset | Colore |
|---|---|---|
| Sfondo pagina | — (gradiente già esistente, non toccare) | — |
| TopBar, NavBar | — (già corretti, non toccare) | — |
| **Tab MAZZI/EMOTICON/ACCUSI — attivo** | Sprite reale `btn_teal.png`, Sliced | nessuna tinta, colore nativo dello sprite |
| **Tab — inattivo** | Sprite reale `btn_gray_small.png`, Sliced | nessuna tinta, colore nativo dello sprite |
| Testo tab | TMP | attivo: `(255,250,235,255)` — inattivo: `(196,208,224,255)` |
| Etichetta sezione ("IN USO"/"COLLEZIONE") | TMP, nessuno sfondo | `(255,228,156,255)` (oro chiaro) |
| Contatore "N / M" | TMP, nessuno sfondo | `(255,228,156,255)` |
| Linea sottile accanto all'etichetta sezione | Image piatta 2px altezza, nessuno sprite | `(70,102,142,255)` |
| Barra di progresso — traccia (sfondo) | FILL r24 | `(14,26,44,255)` |
| Barra di progresso — bordo traccia | RING r24 (o Outline component) | `(56,84,120,255)` |
| Barra di progresso — riempimento | FILL r24 (larghezza = frazione) | `(232,178,74,255)` |
| Barra di progresso — riflesso sopra il riempimento (metà superiore) | FILL r24, più piccolo, solo sulla metà alta | `(255,232,170,150)` |

---

## 2. Scheda MAZZI

### 2.1 Riquadro "IN USO" (featured box grande)
| Elemento | Asset | Colore |
|---|---|---|
| Bagliore dietro al riquadro | RING r30 ingrandito e sfocato | `(255,196,70,70)` |
| Riempimento riquadro | FILL r30 | `(18,32,52,252)` |
| Bordo riquadro | RING r30 | `(232,178,74,255)` — pieno oro |
| Badge pillola "IN USO" | FILL r24 (forma pillola, solo riempimento, NESSUN bordo) | `(232,178,74,255)` |
| Testo dentro il badge "IN USO" | TMP | `(58,34,8,255)` (marrone scuro) |
| 3 dorsi carta nel ventaglio | Sprite reale `card_back_green.png` (o il dorso del mazzo attivo) | nessuna tinta |
| Nome mazzo ("Napoletano") | TMP | `(255,236,190,255)` |
| Sottotitolo ("Mazzo standard · 40 carte") | TMP | `(185,200,222,255)` |

### 2.2 Cella nella griglia — stato "IN USO" (es. Napoletano)
| Elemento | Asset | Colore |
|---|---|---|
| Bagliore dietro la cella | RING r24 ingrandito/sfocato | `(255,196,70,95)` |
| Riempimento cella | FILL r24 | `(26,44,68,252)` |
| Bordo cella | RING r24 | `(232,178,74,255)` |
| Dorso carta dentro | Sprite `card_back_green.png` | nessuna tinta |
| Nome mazzo | TMP | `(255,236,190,255)` |
| Testo "In uso" (NON è un bottone, solo testo) | TMP, nessuno sfondo | `(255,228,156,255)` |
| Check ✓ angolo alto-destra | Sprite reale `ic_check.png` | nessuna tinta |

### 2.3 Cella — stato "sbloccato, non in uso" (es. Classico)
| Elemento | Asset | Colore |
|---|---|---|
| Riempimento cella | FILL r24 | `(26,44,68,252)` |
| Bordo cella | RING r24 | `(70,102,142,255)` (LINE — NON oro) |
| Dorso carta | Sprite `card_back_green.png` | nessuna tinta |
| Nome mazzo | TMP | `(255,236,190,255)` |
| **Bottone "USA"** (bottone vero) | Sprite reale `btn_teal.png`, Sliced | nessuna tinta |
| Testo "USA" | TMP | `(255,250,235,255)` |

### 2.4 Cella — stato "bloccato" (es. Reale, Smeraldo, ecc.)
| Elemento | Asset | Colore |
|---|---|---|
| Riempimento cella | FILL r24 | `(14,24,40,246)` |
| Bordo cella | RING r24 | `(70,102,142,255)` |
| Dorso generico (scurito) | Sprite `card_frame_dark.png` + velo scuro sopra | velo `(6,14,26,150)` |
| Lucchetto sovrapposto | Sprite reale `ic_lock.png` | nessuna tinta |
| Nome mazzo | TMP | `(124,146,176,255)` |
| **Bottone prezzo** (es. "1.500", "250 gemme", "Evento") | Sprite reale `btn_blue_long.png`, Sliced | nessuna tinta |
| Testo prezzo | TMP | `(255,250,235,255)` |

---

## 3. Scheda EMOTICON

### 3.1 Slot equipaggiato (pieno)
| Elemento | Asset | Colore |
|---|---|---|
| Bagliore dietro | RING r24 ingrandito/sfocato | `(255,196,70,80)` |
| Riempimento slot | FILL r24 | `(26,44,68,252)` |
| Bordo slot | RING r24 | `(232,178,74,255)` |
| Immagine emoticon | PNG emoticon reale (es. `emo_risata.png`) | nessuna tinta |
| Nome emoticon | TMP | `(185,200,222,255)` |
| Bottone rimuovi (cerchio rosso con ×) | Ellisse piena, nessuno sprite esistente — da creare o disegnare con primitive | riempimento `(200,56,56,255)`, bordo `(255,235,235,255)` 3px, "×" bianco |

### 3.2 Slot vuoto
| Elemento | Asset | Colore |
|---|---|---|
| Riempimento slot | FILL r24 | `(14,24,40,220)` |
| Bordo slot | **TRATTEGGIATO**, non l'anello pieno — texture dedicata da creare se il motore non ha bordi tratteggiati nativi | `(70,100,140,255)` |
| Icona "+" al centro | Disegno vettoriale (cerchio + croce) o piccolo sprite dedicato | `(90,124,166,255)` |
| Testo "Slot libero" | TMP | `(120,146,178,255)` |

### 3.3 Cella griglia — posseduta ed equipaggiata
Identico a §2.2 ma: riempimento `(26,44,68,250)`, bordo oro larghezza **4** (non 5), immagine = emoticon reale invece del dorso carta.

### 3.4 Cella griglia — posseduta, non equipaggiata
Identico a §3.3 ma bordo = LINE `(70,102,142,255)`, nessun check.

### 3.5 Cella griglia — non posseduta
| Elemento | Asset | Colore |
|---|---|---|
| Riempimento | FILL r24 | `(14,24,40,240)` |
| Bordo | RING r24 | `(70,102,142,255)` |
| Lucchetto | Sprite `ic_lock.png` | nessuna tinta |
| Testo "Bloccata" | TMP | `(110,132,162,255)` |

---

## 4. Scheda ACCUSI

### 4.1 Riquadro "IN USO" (featured box)
Stessa struttura di §2.1 (FILL r30 colore `(20,36,58,252)`, RING r30 oro, bagliore `(255,196,70,70)`), tranne che al posto del ventaglio carte c'è:

| Elemento | Asset | Colore |
|---|---|---|
| Cerchio icona animazione | Ellisse piena, disegno vettoriale | "Pugno" = `(196,54,44,255)` |
| Bordo del cerchio | Ellisse contorno 6px | `(232,178,74,255)` |
| Glifo dentro il cerchio | Disegno dedicato per animazione — non un'icona del set esistente | pelle `(246,204,158,255)`, contorno `(132,88,52,255)` |
| **Bottone "ANTEPRIMA"** (bottone vero) | Sprite reale `btn_blue_long.png`, Sliced | nessuna tinta |

### 4.2 Riga — posseduta ed equipaggiata (es. Pugno sul tavolo)
| Elemento | Asset | Colore |
|---|---|---|
| Riempimento riga | FILL r24 | `(26,44,68,250)` |
| Bordo riga | RING r24 | `(232,178,74,255)` |
| Badge icona circolare | Ellisse piena, colore specifico riga | Pugno = `(196,54,44,255)` |
| Bordo badge | Ellisse contorno | `(232,178,74,255)` |
| Nome animazione | TMP | `(255,250,235,255)` |
| Descrizione | TMP | `(185,200,222,255)` |
| Testo "IN USO" (solo testo) | TMP | `(255,228,156,255)` |

### 4.3 Riga — bloccata (es. Tuono, Pioggia d'oro, Vortice)
| Elemento | Asset | Colore |
|---|---|---|
| Riempimento riga | FILL r24 | `(16,28,46,244)` |
| Bordo riga | RING r24 | `(70,102,142,255)` |
| Badge icona + velo scuro | Ellisse piena + overlay | velo `(10,18,30,150)` |
| Lucchetto sul badge | Sprite `ic_lock.png` | nessuna tinta |
| Nome animazione | TMP | `(140,162,192,255)` |
| Descrizione | TMP | `(110,132,162,255)` |
| **Bottone prezzo** | Sprite reale `btn_blue_long.png`, Sliced | nessuna tinta |

---

## 5. Riepilogo — GLI UNICI punti dove uno sprite bottone reale è corretto

Lista completa e chiusa di dove `btn_teal` / `btn_blue_long` / `btn_gray_small`
vanno usati in queste tre schermate — da nessun'altra parte:

1. I 3 tab MAZZI/EMOTICON/ACCUSI (`btn_teal` attivo, `btn_gray_small` inattivo)
2. Bottone "USA" su cella mazzo/emoticon sbloccata (`btn_teal`)
3. Bottone prezzo su cella/riga bloccata (`btn_blue_long`)
4. Bottone "ANTEPRIMA" scheda Accusi (`btn_blue_long`)

Tutto il resto — ogni sfondo di cella, riga, slot, riquadro featured — è
**FILL + RING neutri tinti**, mai uno sprite bottone.

---

## 6. File allegati necessari

- `panel_fill_r24.png` — bordo import 24/24/24/24, Sliced
- `panel_ring_r24.png` — bordo import 24/24/24/24, Sliced
- `panel_fill_r30.png` — bordo import 30/30/30/30, Sliced
- `panel_ring_r30.png` — bordo import 30/30/30/30, Sliced
- `import_manifest.json` allegato, già pronto per questi 4

Elementi ancora mancanti, da creare quando serve (non bloccanti per il resto):
- Bordo tratteggiato per lo slot emoticon vuoto (§3.2)
- Bottone rimuovi circolare rosso con "×" (§3.1) — può bastare una primitiva Unity (Image circolare + Text), non serve subito un PNG dedicato
- Glifi vettoriali per le icone animazione accuso (pugno fatto, tuono/pioggia/vortice no) — non bloccante, sono righe bloccate di esempio
