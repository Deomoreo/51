# K4 — shader, piano e punto di ripresa (23/09/2026)

Obiettivo autorizzato: realizzare K4 del backlog preservando K1/K2 e i dati di gioco.
Unity 2022.3, Built-in, uGUI e SpriteRenderer; nessuna nuova dipendenza.
Gli effetti devono rispettare trasparenze, maschere, pooling e disattivazione.
Non esiste ancora una rarità dei mazzi: l'olografico sarà opt-in, senza inventare ricompense.

## Lavoro
- [x] Sfocatura GPU: CaptureScreenshotIntoRenderTexture, downsample e passate separabili;
      nessun ReadPixels/GetPixels nel percorso runtime. Rilascio risorse e fallback al velo.
- [x] Carte: shader sprite con riflesso, dissolvenza/bruciatura per trasformazione matta,
      olografico opt-in. Reset stato e materiali quando si interrompe la trasformazione.
- [x] UI: riflesso sui pulsanti primari, alone che usa la geometria/alpha del pulsante,
      animazione tenue del fondo Home. Builder ripetibile e installazione runtime.
- [x] Test mirati, compilazione shader, suite EditMode e controllo visivo se disponibile.
- [x] Versione, roadmap e backlog aggiornati con esiti e limiti reali.

## Verifica e rischi
Testare riapplicazione, materiale ripristinato, interruzione, trasparenza, maschere,
render texture rilasciate e riferimenti shader inclusi nel player tramite Resources.
La velocità su Android richiede una misura su dispositivo: non dichiararla misurata in Editor.
Unity inizialmente chiuso; verifiche da eseguire in batch con GPU, senza -nographics.

## Stato
Consegnato nella versione 2.02. Modifiche K1/K2 preesistenti preservate; nessun commit effettuato.

## Esiti e ripresa
- 249 EditMode superati; 4 espliciti esclusi (3 K4, 1 K1).
- I 3 runtime K4 eseguiti separatamente in Play Mode con le stesse asserzioni: passati.
  Test Runner MCP non conserva correttamente il job quando un test entra/esce da PlayMode;
  per questo i runtime restano espliciti, eseguibili dal runner con Play Mode già attivo.
- Avvio e Home controllati visivamente; partita allenamento e Impostazioni con blur GPU
  RenderTexture 135x240. Orientamento Direct3D inizialmente invertito, corretto e ricontrollato.
- Console finale: nessun errore. Android e confronto prestazioni CPU/GPU non eseguiti.
- Revisione: fix alpha agli estremi dissolve; halo conservato ai bordi RectMask2D senza
  disattivare il clipping dei pixel. Resta controllo visivo completo dello scorrimento in I7.
- Foto verifica blur: `Assets/Screenshots/screenshot-20260923-102958.png`.

## API e vincoli
- `UIV2ShaderKit.SetSurface(graphic, UIV2SurfaceEffect.Surface.Holographic)` per UI rara;
  `CardView.SetHolographic(true)` per carte. Nessun mazzo attuale viene marcato raro.
- Lo stato decorativo della carta viene azzerato al pooling e alla cancellazione del valore
  temporaneo: i futuri binder dei mazzi devono riapplicare la decorazione dopo il bind.
- Shader inclusi nel player in `Assets/Resources/K4`, caricati tramite Resources.
- Il builder modifica i prefab; scene completate al caricamento, senza salvataggi manuali.
- Prossimo lavoro: K5. Collegamenti rarità con F2/F5/J5; collaudo completo in I7/G2.
