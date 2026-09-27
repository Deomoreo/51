# 51 — Specifiche grafiche per l'implementazione in Unity

Questo pacchetto contiene la **nuova grafica definitiva** dell'app "51" (Cirulla con mazzo napoletano).
Sostituisce completamente la grafica attuale del progetto Unity. La logica di gioco esistente va mantenuta.

Contenuto:
- `mockups/` — una schermata per file (`*.dc.html`). Sono HTML con stili inline: **non servono per essere aperti nel browser** (manca il runtime), ma sono la fonte esatta di misure, colori, testi, stati e animazioni (vedi i blocchi `<style>` con `@keyframes` e la classe `Component` in fondo a ogni file, che contiene i dati e la logica degli stati).
- `assets/` — tutte le immagini usate dai mockup, con nomi leggibili. I file `source_*` sono i fogli originali (icone, avatar, mazzo napoletano completo). I file `emo_*_sheet_8frames.png` sono le emoticon animate (8 fotogrammi, 2 righe × 4).
- `SPEC.md` — questo file.

---

## 1. Impostazione generale

- **Risoluzione di riferimento: 390 × 844** (portrait). Tutti i numeri nei mockup sono in queste unità.
  Canvas Scaler: *Scale With Screen Size*, reference 390×844, match 0.5. Così i valori dei mockup (left/top/width/height) si copiano 1:1 nei RectTransform (attenzione: in HTML l'origine è in alto a sinistra, y verso il basso).
- **Safe area**: script SafeArea su un contenitore sotto il Canvas (notch, barre di sistema).
- **UI**: se il progetto usa già uGUI restare su uGUI; altrimenti usare **uGUI + TextMeshPro + DOTween** (effetti, particelle, shader si integrano meglio che con UI Toolkit).
- **Font** (Google Fonts, licenza OFL): **Cinzel** (600/700) per titoli, numeri, CTA, etichette maiuscole con letter-spacing; **Nunito** (400/600/700/800) per tutto il resto. Creare i Font Asset TMP (SDF) con il set di caratteri italiani (àèéìòù, ’, «», €).

## 2. Design token

Colori
| Nome | Valore | Uso |
|---|---|---|
| gold | `#F3C969` | accento principale, bordi attivi, etichette maiuscole |
| goldLight | `#FCE29A` | alto dei gradienti oro, numeri in evidenza |
| goldDark | `#C4922F` | basso dei gradienti oro |
| onGold | `#25160A` | testo sopra i bottoni oro |
| cream | `#F5E9D0` | testo principale |
| creamMuted | `rgba(245,233,208,0.55)` | testo secondario (0.45–0.75 a seconda del contesto) |
| panelTop / panelBottom | `rgba(10,22,44,0.82)` → `rgba(6,13,27,0.90)` | pannelli standard (gradiente verticale) |
| sheetTop / sheetBottom | `rgba(12,26,50,0.97)` → `rgba(6,13,27,0.99)` | pannelli dal basso, dialog, banner giocatore |
| borderGold | `rgba(243,201,105,0.25)` / `0.35` / `0.45` | bordi 1px dei pannelli (più alto = più in evidenza) |
| danger / dangerText | `#E5484D` / `#F08A8D` | elimina, abbandona, notifiche |
| success / successText | `#27B585` / `#7FE0B8` | online, riscattato |
| teamBlue | `#4F80E8` → `#1B3A7A` | cornici avversari |
| felt | `#1F8460` → `#146346` → `#0C4532` → `#093826` | panno del tavolo (radiale) |
| wood | `#7A4B29`, `#4E2D16`, `#6B4122` | bordo del tavolo |

Gradienti ricorrenti
- **Bottone oro**: verticale `#FCE29A 0%` → `#F3C969 55%` → `#C4922F 100%`, bordo 1px `rgba(255,255,255,0.5)`.
- **Cornice oro avatar**: conic `#FCE29A, #C4922F, #FFF1C4, #8A5A12, #FCE29A` (from 20deg).
- **Cornice blu (avversari)**: conic `#4F80E8, #F3C969, #1B3A7A, #FCE29A, #4F80E8`.

Raggi: pillola/banner 25 · pannello 16 · card/bottone grande 16–18 · pannello dal basso 24 (solo angoli alti) · chip 8–13 · bottone tondo 20 (40×40).
Ombre: pannelli `0 6–10 14–30 rgba(0,0,0,0.35–0.5)`. In Unity: sprite ombra morbido sotto il pannello o componente Shadow leggero.

Sfondo delle schermate "interne": `home_bg_base.png` sfocato (blur ~10px) e scurito (brightness ~0.42). Conviene pre-renderizzare una versione sfocata una volta sola invece di sfocare a runtime.

## 3. Componenti da creare come prefab (coprono ~80% dell'app)

1. **GoldButton** — h 54 (52 nei pannelli), raggio 16, gradiente oro, testo Cinzel 700 14–15, letter-spacing 2, colore onGold. Hover/press: leggero aumento di luminosità / scale 0.97.
2. **OutlineButton** — bordo 1px gold 0.45–0.5, testo gold Nunito 800. Variante rossa (danger) per "Abbandona"/"Elimina".
3. **RoundIconButton** — 40×40, fondo `rgba(11,29,58,0.6)`, bordo gold 0.45, icona crema 18–20.
4. **Panel** — gradiente pannello, bordo gold 0.25, raggio 16. Consigliato: sprite 9-slice.
5. **PlayerBanner** (pillola al tavolo) — 120×50 (avversari) / 186×50 (tu), raggio 25, avatar 36–38 con cornice, nome Nunito 800 11–12, livello 9–10, chip prese (mini dorso + numero). Lo sfondo è il **banner scelto dal giocatore** (vedi §7). Variante verticale 64×100 per i giocatori laterali nel 2v2/1v3.
6. **AvatarFrame** — cerchio con anello (oro / blu / cornice scelta), immagine avatar ritagliata al 132% con offset (-16%, -4%) per nascondere la cornice dipinta dell'immagine originale.
7. **SegmentedTabs** — contenitore raggio 12, fondo `rgba(255,255,255,0.04)`, bordo gold 0.18; tab attiva: fondo gold 0.16, bordo gold 0.7, testo gold.
8. **Toggle** — 46×26; on: fondo gold, pallino `#25160A`; off: fondo `rgba(255,255,255,0.08)`, bordo gold 0.4.
9. **BottomSheet** — larghezza piena, raggio 24 in alto, maniglia 40×4, titolo Cinzel 18 + ✕ tonda 36. Entra dal basso (vedi animazioni). Dietro: scrim scuro + sfocatura.
10. **Dialog** — card centrata con icona tonda 56, titolo Cinzel 19, testo 13, GoldButton + OutlineButton.
11. **Chip / Badge** — pillole 16–26px; pallino notifica 9–11px rosso con bordo scuro.
12. **ConnectionOverlay** — overlay universale sopra tutto (vedi §6).

## 4. Schermate (file in `mockups/`)

Accesso: `Main` (login) · `Registrazione` · `Termini` · `Privacy` · `Caricamento`.
Home: `Home` (+ `HomeOspite`) con pannelli `HomeModalita` / `HomeModalitaBot` / `HomeModalitaPrivata` (schede Online · Allenamento · Stanza privata) e `HomeMazzo`.
Social e meta: `Amici` (+ `AmiciRichieste`) · `Posta` (+ `PostaMessaggio`) · `Notizie` (+ `NotizieArticolo`) · `Premi` (+ `PremiRiscattato`, premi giornalieri a 7 giorni).
Collezione: `Collezione` (dorsi) · `CollezioneEmoticon` (max 3 in partita) · `CollezioneAccuso`.
`Negozio` ("presto in arrivo").
Profilo: `Profilo` (+ `ProfiloOspite`) con editor `ProfiloAvatar` / `ProfiloCornice` / `ProfiloBanner`; `Impostazioni` (+ `ImpostazioniOspite`, `ImpostazioniElimina`).
Partita: `Sorteggio` (ruota del mazziere, parte da sola) · `Partita` (1v1) · `Partita4` (2v2 e base per 1v3) e varianti (accuso, opzioni, scelta presa, abbandono, profilo rapido, mazziere, banner) · `FineSmazzata` (1v1 / 2v2 / 1v3) · `FinePartita` (vittoria / sconfitta / 2v2 / classifica 1v3).
Connessione: `Connessione` (componente) e gli esempi `Conn*`.

Bottom nav (Home, Collezione, Negozio, Profilo): altezza 72, fondo `rgba(5,11,23,0.96)`, bordo alto gold 0.3, icona 22 + etichetta 11; tab attivo: icona piena, etichetta gold, lineetta gold 24×2 in alto.

Ospite: niente XP/livello/ricompense/statistiche/trofei; banner "OSPITE"; invito a registrarsi; nelle impostazioni niente "Elimina account".

## 5. Tavolo da gioco (regole di layout)

- Sfondo: atmosfera notturna (home_bg sfocato e scuro) + **tavolo** con bordo di legno (padding 9) e panno verde radiale con rumore leggero; `sun_emblem.png` al centro, opacità ~0.08.
- **HUD**: in alto a sinistra "Abbandona" (tondo 40), a destra "Opzioni", al centro la pillola del punteggio (TU · A 51 · AVVERSARIO; NOI/LORO nel 2v2).
- **Mazzo** fisso in alto a sinistra su un **cuscino di velluto rosso** (72×64, raggio 20, bordo oro, borchie agli angoli), dorso inclinato −8°. Tocco sul mazzo: il mazzo si solleva e appare un medaglione oro con le carte rimaste che conta da 0 al valore.
- **Mazziere**: gettone oro "M" 24px accanto al banner di chi distribuisce (si sposta ad ogni smazzata).
- **Avversari (stessa regola per tutti)**: banner; le carte in mano spuntano **dietro al banner verso il tavolo**; le **scope** spuntano dietro al banner dal lato opposto (sopra per chi sta in alto; di lato, carte sdraiate, per i laterali), massimo 4 visibili poi badge "+N"; toccandole si aprono a ventaglio (vedi animazioni). Le prese normali sono solo un chip numerico dentro il banner.
- **1v1**: avversario in alto (banner orizzontale) con le sue 3 carte coperte ben visibili sotto.
- **2v2**: compagna in alto (banner orizzontale, cornice oro), avversari ai lati nella fascia medio-alta (banner verticali 64×100, cornice blu); carte in tavola nella fascia sotto, a tutta larghezza. **1v3**: stesso layout, tutti con cornice blu, punteggio a 4.
- **Carte in tavola**: griglia centrata a righe bilanciate (mai una carta sola sotto una riga piena). La dimensione si calcola: per c colonne da 1 a 8, r = ceil(n/c), w = min((AW−(c−1)·8)/c, (AH−(r−1)·8)/(r·1.55), wMax); si sceglie la c con w massima. Proporzione carta 1 : 1.55.
- **Tua mano**: 3 carte 92×143 (distanza 99). Primo tocco: la carta si alza di 24 e le carte prendibili si illuminano d'oro. Secondo tocco: si gioca. Più prese possibili → **vassoio dal basso** sopra la mano (non copre il tavolo) con le combinazioni (carte grandi, numero e colore ①oro ②turchese ③rosa ④viola, chip +carte / denari / SCOPA); le stesse combinazioni sono colorate sul tavolo e sono toccabili.
- **Tuo banner** in basso (186×50) con il tuo banner scelto; a sinistra il tasto emoticon (apre le tue 3 emoticon dentro il banner), a destra il medaglione ACCUSA che **entra solo quando puoi accusare**.
- **Accuso**: animazione al centro (vedi §6). Le carte dell'accusatore poi tornano "in mano" ma **scoperte** (bordino oro) e toccandole si rivedono in grande. Nell'1v1 si girano scoperte al loro posto.
- **Emoticon**: quelle degli altri si animano al posto del loro avatar; la tua sale dal tuo banner in un fumetto 76×76 e svanisce (2.6 s).
- **Profilo rapido**: tocco su un banner → card 300px con il **banner del giocatore come sfondo dell'intera card** (+ sfumatura scura verso il basso), avatar, livello, titolo, 3 statistiche, trofei, Aggiungi amico, Silenzia emoticon, Segnala.
- **Opzioni in partita**: Audio (Musica, Effetti, Vibrazione) e Grafica (qualità Bassa/Media/Alta, Animazioni rapide).
- **Abbandono**: dialog rosso con "RESTA AL TAVOLO" (primario) e "Abbandona".

## 6. Animazioni (tradurre con DOTween)

| Nome | Dove | Durata / curva | Descrizione |
|---|---|---|---|
| Pop | dialog, chip, badge | 0.25–0.3 s, ease-out (leggero overshoot) | scale 0.6–0.85 → 1, alpha 0 → 1 |
| SheetUp | pannelli dal basso | 0.28 s, cubic(.2,.8,.3,1) | translateY 100% → 0 |
| Pulse | tuo turno, accusa, giorno di oggi | 1.6–1.8 s loop | anello che si espande (0 → 8–10px) e svanisce |
| SlideIn accusa | medaglione ACCUSA | 0.6 s + pulse | entra da destra ruotando (90px, 30° → 0) con overshoot; riflesso diagonale ogni 2.4 s |
| Accuso (centro) | tavolo | tot. ~1.2 s | scurisce il centro (0.3 s); pugno cade da −120px scale 2.2 ruotato −18° → rimbalzo (0.6 s); 2 onde d'urto oro (scale 0.3 → 2.3, 0.9 s, la seconda +0.6 s); bagliore; il tavolo trema (0.35 s, ±5px); testo "ACCUSO · Nome +3" (0.35 s dopo 0.45 s); si chiude dopo ~2.6 s |
| Fan scope | viewer scope/carte accusate | 0.45 s, stagger 0.06–0.07 s | carte da translateY 90 scale 0.45 → ventaglio (rotazione ±9–10° per carta) |
| Flip | carte che si scoprono | 0.35 s | rotateY 90° → 0 |
| Deck lift + count | mazzo | 0.5 s + conteggio | mazzo sale 8px e si raddrizza; medaglione conta a step di 2 ogni 30 ms |
| Ruota mazziere | Sorteggio | 3.4 s, cubic(.12,.75,.18,1) | 6 giri + spicchio del vincitore; lancetta "ticchetta" (±14° ogni 0.12 s); parte da sola dopo ~1.2 s |
| Onda caricamento | Caricamento | 1.4 s loop, stagger 0.12 s | 5 dorsi in fila: translateY 8 → −14 con leggera rotazione ±4° |
| Emoticon | ovunque | 0.9 s loop ping-pong | sprite sheet 8 fotogrammi (steps) |
| Banner animati | banner | vedi §7 | |
| Riscatto premio | Premi | 1.1 s + 1.8 s | esplosione del bagliore (scale 0.2 → 1.4) + "+150" che sale e svanisce |
| Fine smazzata | righe punteggi | 0.35 s, stagger 0.18 s | righe entrano da sinistra; badge "+1" pop; barre "corsa al 51" si riempiono (1 s, dopo 1.9 s) |
| Fine partita | titolo, coriandoli | pop 0.5 s | nastro; raggi di luce che ruotano (18 s/giro); coriandoli (particelle) solo in vittoria; barra XP (1.3 s) |
| Riconnessione | overlay | loop | anello che gira 1.1 s; archi Wi-Fi che pulsano sfasati 0.2 s |

Suggerimento: una classe statica `UIAnim` con questi preset, da richiamare ovunque.

## 7. Banner del giocatore

Visibili: sul banner al tavolo, come sfondo intero della card del profilo rapido e della card del Profilo, nell'editor (scheda Banner).
| id | tipo | resa |
|---|---|---|
| notte | statico (default) | gradiente pannello scuro |
| smeraldo | statico | 135°: `#0E4F3A → #0A3A2B → #146A50` |
| porpora | statico (livello 10) | 135°: `#4A1420 → #2E0B14 → #5C1A2A` |
| aurora | animato (pass) | gradiente `#0B1D3A, #1B6B8F, #27B585, #6B3FA0` che scorre (7 s ease loop) |
| stellato | animato (negozio) | `#0A0F2A → #1B1440` con puntini luminosi che si muovono piano (5 s alternate) |
| oro | animato | `#5A3A10 → #8A5A12 → #3A2508`, bordo oro chiaro, riflesso diagonale ogni 3 s |

Implementazione consigliata: **un solo shader UI** (Shader Graph) con parametri: colori del gradiente, velocità di scorrimento, intensità/periodo del riflesso, texture stelle opzionale. I banner statici = shader con velocità 0 oppure semplici sprite.

## 8. Asset

- Tutte le immagini in `assets/` vanno importate come Sprite (2D and UI) e messe in Sprite Atlas per area (UI comune, carte, avatar, emoticon).
- **Carte napoletane**: nel pacchetto ci sono ritagli d'esempio (`card_*`, `c_*`); il mazzo completo è in `source_napoletane_svgz.png` (griglia 13 colonne × 4 righe + dorso) — usare il mazzo già presente nel progetto se c'è.
- **Avatar**: `avatar_1.png`, `av_2..8.png` hanno la cornice dorata dipinta; nel gioco sono ritagliati in cerchio (vedi AvatarFrame). Idealmente servono versioni senza cornice.
- **Emoticon**: `emo_*_sheet_8frames.png` → Sprite Editor, Grid by Cell Count 4×2.
- **Gradienti/pannelli/pillole**: sono disegnati in codice nei mockup. Opzioni: 9-slice PNG generati una volta oppure componente/shader "UIGradient + bordo arrotondato". Tenere un solo stile riusabile.
- Icone "cream" (`ic_*_cream.png`): icone di interfaccia; icone colorate in `g/…` (dal foglio `source_Icons.png`).
- Segnaposto da sostituire più avanti: cornici avatar (anelli disegnati in codice), banner, testi legali (`Termini`, `Privacy`), nomi di mazzi/dorsi/banner bloccati.

## 9. Sistemi di gioco citati dalla grafica (per contesto)

Livelli ed XP; collezione (dorsi, emoticon max 3, accuso); sblocchi da livelli, forzieri, negozio, missioni, eventi, battle pass; idea futura: **frammenti per rarità** (es. Comune 20 · Rara 50 · Epica 120 · Leggendaria 300) + polvere universale + garanzia. Premi giornalieri a 7 giorni con serie. Punteggio Cirulla fino a 51: carte, denari, settebello, primiera, grande, piccola, scope, accusi.
