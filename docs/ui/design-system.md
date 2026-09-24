# K2 — sistema di design

Decisione confermata il 23 settembre 2026. Fonte condivisa:
`Assets/UIV2/Art/UIV2Theme.asset`, configurata da **Tools/UIV2/Apply Design System**.

Aggiornamento di direzione del 23/09: confermati «Indietro» nelle schermate e
«Chiudi» nei popup al posto delle X di chiusura. La revisione delle icone senza
riquadro è descritta come proposta in [asset-refresh-brief.md](asset-refresh-brief.md).
La tabella sotto documenta ancora l'implementazione K2: la migrazione non è applicata.

## Regole

| Elemento | Stile |
| --- | --- |
| Azione primaria | Oro, Poppins ExtraBold panna `#FFFCF2`, contorno bruno `#945408` |
| Azione secondaria | Blu, testo chiaro |
| Azione piatta | Sfondo trasparente, area di tocco conservata |
| Azione icona | Riquadro blu, icona separata dalla superficie cliccabile |
| Modale | Fondo blu `#16283C`, cornice oro `#E8B24A`, nastro dove previsto |
| Contenuto | Fondo blu e bordo blu secondario `#2E4F6C`, senza nastro |
| Titolo / sottotitolo / testo / didascalia | Poppins ExtraBold / Bold / Medium / Medium; 40 / 32 / 24 / 20 unità UI |
| Colori testo | Crema `#FAF4E0`, secondario `#8FA6BC`; i colori di stato restano al componente che li gestisce |

Il materiale oro è unico: `Poppins-ExtraBold SDF Outline Gold Button.mat`.
Contorno 0,6 e dilatazione 0,3, nei limiti del padding dell'atlante.
Non creare materiali per ogni scritta. Il font e il materiale devono usare lo stesso atlante.

## Applicazione e compatibilità

- Il builder aggiorna i prefab UIV2 e crea `UIV2_FlatButton`, `UIV2_IconButton`,
  `UIV2_ContentPanel`. Il modale riusa `UIV2_ModalFrame`.
- `UIV2DesignCatalog` in Resources referenzia il tema originale: nessuna seconda copia.
- `UIV2DesignSystem` completa la UI delle scene al caricamento, inclusi i pannelli inattivi.
  Non salva scene: evita il drift dei layout ExecuteAlways già osservato in K1.
- Dopo un vecchio builder di schermata, eseguire **Apply Design System**; la stessa
  applicazione avviene comunque all'ingresso nella scena durante il gioco.
- La migrazione dei testi esistenti allinea la gerarchia locale alla scala comune:
  da 36 → titolo, da 28 → sottotitolo, da 23 → testo, il resto → didascalia.
  Per nuovo codice usare direttamente `UIV2Theme.ApplyTypography` con il ruolo desiderato.
- Rimangono calibrati GIOCA/GIOCA COME OSPITE, nastri, nomi nei banner, contatori,
  codici stanza, testi minuscoli e titoli molto grandi. Autosize conserva il rapporto
  minimo/massimo. Gli outline decorativi conservano il font compatibile.
- `UIV2Button.Apply` ripristina il materiale oro anche dopo riapertura. I valori
  serializzati Teal=2 e GreenSmall=3 restano compatibili per i controlli esistenti;
  selezioni, pillole e stati attivi continuano a usare il loro colore semantico.
- I pannelli sono riconosciuti tramite proprietari strutturali espliciti. Non trattare
  un generico `Fill` come pannello: barre progresso e miniature usano gli stessi sprite.
- Il testo runtime di scelta presa usa l'evento `MoveSelectionUI.TextCreated`, così
  l'assembly Gameplay non dipende dalla UI. Contatori prese e indicatore turno usano
  il tema senza cambiare colori di stato o numeri.

## Verifica

Otto test K2 coprono ripetibilità, oro dopo riapplicazione, contenuti e stato input,
pulsanti annidati, esclusione TMP nello spazio 3D, area cliccabile piatta,
geometria dei pannelli, riconoscimento modali e contrasto del progresso anche nei
prefab annidati di Profilo e Collezione.

Controllo visivo in Editor/Play Mode: avvio, accesso, impostazioni, Home, Collezione,
Profilo, anteprime layout fine smazzata e fine partita. Le anteprime risultati usano
solo i dati dei template in memoria, senza registrare punteggi o ricompense.
Nessuna build Android o verifica su dispositivo in questa consegna.
