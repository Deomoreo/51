# K5 — tavolo, consegna e ripresa (23/09/2026)

Direzione approvata: panno con trama sottile e luce centrale, ombre morbide sotto le carte,
mano più tangibile al tocco, giocate con discesa decisa e assestamento breve. Conservare
interazioni e tempi di gioco. Prima consegna 2.03, correzione del volo nella 2.04;
modifiche K1–K4 preservate.

## Correzione 2.04: scatti segnalati nel volo

La traiettoria spezzata della 2.03 introduceva una quasi-fermata a metà e un arresto
anticipato all'80%. Ripristinato il DOPath CatmullRom precedente con easing unico,
scala/rotazione originali, ombra continua e audio alla fine degli 0,35s.
Il test nuovo campiona sia la zona intermedia sia il 90% del volo: è fallito prima della
correzione (0,00325 unità percorse intorno al raccordo su un tragitto di 3 unità).
Dopo il fix, campioni a 60 Hz mostrano decelerazione continua senza arresto intermedio.
255 EditMode e 3 runtime K5 passati; i controlli della 2.03 sulla sola posa finale non
erano sufficienti per verificare la continuità temporale. I 2 runtime K4 restano verificati
nella 2.03, non ripetuti per questa correzione.

## Implementazione

- `TableFeltRenderer`: trama generata senza consumare il Random del gioco; luce ellittica
  normalizzata, mipmap. Rebuild rilascia sprite/texture sostituiti; OnValidate segnala solo
  una rigenerazione da eseguire nel successivo aggiornamento normale dell'Editor.
- `CardDropShadow`: una texture e un materiale condivisi, una sola ombra per carta.
  Bind da `CardView.Initialize` e `TryCreateVisualCopy`, senza nuovi asset da configurare.
  Segue bounds/pivot, flip, rotazione, sorting layer, alpha, enabled e dissolve K4.
  Stesso sorting order della faccia e coda trasparente 2999: resta sopra il feltro (-1)
  e sotto la propria faccia senza cambiare ordini consecutivi e aloni esistenti.
- `CardView`: easing di sollevamento rapido con piccolo overshoot; hover, selezione e
  hint concorrenti interrotti quando cambia la selezione. Posa e sorting ripristinati
  al disable. Matta applica separatamente il fattore di larghezza in LateUpdate;
  il movimento iniziato durante il flip acquisisce la scala non schiacciata.
- `CardAnimationController`: traiettoria continua lungo tutti gli 0,35s (ripristinata
  nella 2.04). Audio all'arrivo, velocità delle preferenze preservata.
  Su kill ripristina posa iniziale, scala, rotazione e sorting senza completare la mossa.
  Al completamento mantiene esattamente la posa finale e notifica una sola volta.

Nessuna scena o prefab generato modificato manualmente; nessuna dipendenza aggiunta.
Nessun commit effettuato: il workspace contiene anche le consegne precedenti non committate.

## Verifica

- Suite completa EditMode: 254 passati, zero fallimenti, 7 runtime espliciti esclusi.
- Tre runtime K5 eseguiti in Play Mode con `K5TableTests.RunRuntimeChecks`: annullamento
  a metà volo, hover/selezione/matta e riuso, selezione verso la fine del flip.
- Due runtime carte K4 rieseguiti: materiale su disable e reset decorazione al riuso.
  Runtime K1 e blur K4 non ripetuti in questo giro.
- Test iniziali hanno mostrato ombre mancanti e pose non ripristinate. Il kill di DOTween
  non esegue callback in EditMode: la prova di interruzione è quindi runtime esplicita.
- Allenamento a quattro giocatori: tavolo, mano, carte avversarie, giocata con presa e
  ritorno al turno locale. Fine sequenza: 0 copie temporanee, 0 ombre di renderer nascosti.
  Console finale senza errori. Revisione indipendente dei percorsi modificati completata.
- Immagini: [presa in corso](k5-table.png), [tavolo dopo la presa](k5-table-settled.png).

## Limiti e prossima ripresa

- Android: build, costo GPU/draw call e resa sul telefono da misurare in G2/I7.
- Multiplayer reale con più client non provato; durata e callback del flusso preservati.
- Collaudo esteso di tutti i mazzi/aspect ratio e accusi nel giro I7.
- Prossimo task: K6, vibrazione configurabile e particelle sui momenti forti.
