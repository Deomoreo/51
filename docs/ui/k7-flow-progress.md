# K7 — flusso veloce: stato

Completato il 23/09/2026, versione 2.07.

## Cosa fa

- **Ospite già entrato**: `GuestEntryPreferences` (chiave `Navigation_GuestEntered`) ricorda
  solo la scelta di navigazione. Al riavvio `StartScreenV2` salta la schermata iniziale
  passando per il normale caricamento di `AppLoadingView`, senza forzare un'identità ospite.
  Dopo il caricamento ricontrolla l'account: con un accesso reale o una registrazione la
  scorciatoia viene cancellata. Anche logout ed eliminazione dell'account la cancellano.
  Home → GIOCA = partita in 1 tocco.
- **Roulette del mazziere**: un solo giro, tempi limitati (partenza ≤ 0,15 s, passi
  0,07–0,20 s, attesa ≤ 0,8 s). Il mazziere scelto, la distribuzione e l'autorità online
  non cambiano. Online non c'è il pulsante Continua e i tempi non dipendono dalle
  preferenze.
- **Risultati di smazzata**: `RoundAdvanceCountdown` di 8 s, mostrato come
  "CONTINUA · Ns". Il round avanza una sola volta, solo offline o sull'host. Il conto si
  ferma quando l'app è in pausa o senza focus, durante il caricamento, con le impostazioni
  aperte o con il pannello non ancora visibile. Un host promosso riparte da 8 s. Il client
  che non è host vede "ATTENDI L'HOST". La rivincita finale resta manuale.

## Verifiche

- EditMode: `K7FlowTests` (scorciatoia ospite, avanzamento unico, pausa con modale,
  nuovo host, annullamento, roulette < 2 s con il mazziere corretto per i vincitori 0–3).
- Runtime GameScene offline, due smazzate: il conto è sceso da 8s a 1s in tempo reale,
  poi il round è avanzato una sola volta. Console senza errori.
- Runtime MainMenu con la preferenza impostata: la schermata iniziale viene saltata e la
  Home si apre direttamente.

## Residuo

- Non provato su dispositivo né con due client Photon reali (host e client). L'autorità
  è coperta dal codice e dai test.
