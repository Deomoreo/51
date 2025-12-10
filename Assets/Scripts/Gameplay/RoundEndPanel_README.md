# RoundEndPanel - Guida alla Configurazione

## Problema Risolto
Il pannello di fine smazzata ora mostra **tutti i dettagli del punteggio** in modo pulito e leggibile:
- **Scope**: Mostra solo il numero (es. "2")
- **7 Bello**: Mostra "V" verde se preso
- **Denari**: Mostra solo il numero (es. "6") - verde se vinto, grigio se no
- **Carte**: Mostra solo il numero (es. "21") - verde se vinto, grigio se no
- **Primiera**: Mostra il punteggio (es. "85") - verde se vinto, grigio se no
- **Grande**: Mostra "+5" se fatto
- **Piccola**: Mostra i punti (es. "+3" o "+5")
- **Accusi**: Mostra i punti (es. "+10")

## Come Funziona

### Modalità Automatica (Fallback)
Se il prefab `PlayerScoreRow` non è configurato correttamente, il sistema usa un **fallback automatico**:
1. **Console Log** - SEMPRE attivo, mostra tutti i dettagli
2. **Campo "All Details Text"** - Se presente, mostra tutto in formato compatto
3. **Fallback generico** - Usa i primi TMP_Text trovati

### Modalità Ottimale (Con Campi Individuali) - RACCOMANDATA

Nel prefab PlayerScoreRow, crea una tabella con colonne:

```
[Nome Giocatore] [Punteggio Totale] | Scope | 7B | Denari | Carte | Primiera | Grande | Piccola | Accusi
```

#### Layout Consigliato:
```
PlayerScoreRow (GameObject)
??? Background (Image) ? "Player Highlight"
??? Header
?   ??? PlayerName (TMP_Text) ? "Player Name Text"
?   ??? TotalScore (TMP_Text) ? "Total Score Text"
??? ScoreBreakdown (HorizontalLayoutGroup)
    ??? ScopeText (TMP_Text) ? "Scope Text"
    ??? SetteBelloText (TMP_Text) ? "Sette Bello Text"
    ??? DenariText (TMP_Text) ? "Denari Text"
    ??? CarteText (TMP_Text) ? "Carte Text"
    ??? PrimieraText (TMP_Text) ? "Primiera Text"
    ??? GrandeText (TMP_Text) ? "Grande Text"
    ??? PiccolaText (TMP_Text) ? "Piccola Text"
    ??? AccusiText (TMP_Text) ? "Accusi Text"
```

## Cosa Mostra Ogni Campo

### Scope Text
- Mostra: `"2"` (solo il numero)
- Colore: Verde se > 0
- Nascosto se: 0 scope

### Sette Bello Text
- Mostra: `"V"` (checkmark)
- Colore: Verde
- Nascosto se: non preso

### Denari Text
- Mostra: `"6"` (solo il numero di denari presi)
- Colore: **Verde** se vinto (6+ denari), **Grigio** se no
- Nascosto se: 0 denari

### Carte Text
- Mostra: `"21"` (solo il numero di carte prese)
- Colore: **Verde** se vinto (21+ carte), **Grigio** se no
- Nascosto se: 0 carte

### Primiera Text
- Mostra: `"85"` (punteggio primiera)
- Colore: **Verde** se vinto, **Grigio** se no
- Nascosto se: 0 punti

### Grande Text
- Mostra: `"+5"`
- Colore: Verde
- Nascosto se: non fatto

### Piccola Text
- Mostra: `"+3"` o `"+5"` (base 3 + eventuali extra)
- Colore: Verde
- Nascosto se: non fatto

### Accusi Text
- Mostra: `"+10"` (punti accusi)
- Colore: Verde
- Nascosto se: 0 punti

## Esempio Visivo

```
GIOCATORE 1    15 punti  | 2 | V | 6 | 18 | 85 |   | +3 |    |
GIOCATORE 2    12 punti  | 1 |   | 4 | 22 | 72 |   |    | +10|
```

In questo esempio:
- Giocatore 1: 2 scope, ha il 7 bello, 6 denari (vinto-verde), 18 carte (non vinto-grigio), primiera 85 (vinto-verde), piccola +3
- Giocatore 2: 1 scopa, 4 denari (non vinto-grigio), 22 carte (vinto-verde), primiera 72 (non vinto-grigio), accusi +10

## Configurazione nel RoundEndPanel
Nel GameObject con `RoundEndPanel`:
- `Panel Root` - GameObject da mostrare/nascondere
- `Player Scores Container` - Transform per istanziare le righe
- `Player Score Row Prefab` - Il prefab configurato
- `Title Text` - Titolo "FINE SMAZZATA"
- `Winner Text` - Testo vincitore
- `Continue Button` - Pulsante continua
- `Main Menu Button` - Pulsante menu

## Console Log (Sempre Attivo)

Alla fine di ogni smazzata, nella console Unity vedrai:
```
========== FINE SMAZZATA - PUNTEGGI DETTAGLIATI ==========

Giocatore 0 (Tu): 15 punti totali
  Scope: 2
  7 Bello: SI
  Denari: 6 (VINTO +1)
  Carte: 18
  Primiera: 85 (VINTO +1)
  Piccola: +3

Giocatore 1 (Bot 2): 12 punti totali
  Scope: 1
  Denari: 4
  Carte: 22 (VINTO +1)
  Primiera: 72
  Accusi: +10
==========================================================
```

## Personalizzazione Colori

Nel componente PlayerScoreRow:
- `Won Item Color` - Verde (#30FF30) - Categorie vinte o presenti
- `Normal Item Color` - Grigio chiaro (#CCC) - Categorie non vinte ma con valori
- `Disabled Item Color` - Grigio scuro (#666) - Non usato
- `Local Player Color` - Blu (#33A5FF) - Background giocatore locale
- `Winner Color` - Oro (#FFD700) - Background vincitore
- `Normal Color` - Grigio (#808080) - Background altri giocatori

## Personalizzazione Font

Nel componente PlayerScoreRow:
- `Player Name Font Size` - Default: 28
- `Total Score Font Size` - Default: 32
- `Score Item Font Size` - Default: 18

Nel componente RoundEndPanel:
- `Title Font Size` - Default: 48
- `Winner Font Size` - Default: 36

## Troubleshooting

**Q: Non vedo i numeri nelle colonne**
A: Assicurati che i campi TMP_Text siano assegnati correttamente nel prefab PlayerScoreRow.

**Q: Tutti i campi mostrano "0"**
A: Controlla che il punteggio sia stato calcolato correttamente. I dettagli sono sempre visibili nella console.

**Q: Il 7 Bello mostra simboli strani**
A: Dovrebbe mostrare solo "V". Se vedi caratteri strani, controlla il font usato nel TMP_Text.

**Q: Come distinguo chi ha vinto Denari/Carte/Primiera?**
A: Il testo è **verde** se vinto, **grigio** se non vinto.

**Q: Voglio una tabella più compatta**
A: Riduci `Score Item Font Size` a 14-16 e usa un HorizontalLayoutGroup con spacing ridotto.

## Vantaggi Design Pulito

? Ogni campo mostra SOLO il suo dato specifico  
? Facile confronto tra giocatori (colonne allineate)  
? Colori indicano vittorie (verde) vs non vinte (grigio)  
? Nessun emoji o simbolo che Unity potrebbe non renderizzare  
? Layout tabellare chiaro e professionale  
? Informazioni complete ma concise
