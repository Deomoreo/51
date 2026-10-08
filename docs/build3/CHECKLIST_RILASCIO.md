# Checklist di rilascio Build 3 (prima versione pubblica)

Stato al 08/10 notte. **Prima di cominciare servono le tue scelte A-C.** Poi i passi vanno fatti **in quest'ordine**. Il riferimento per
le spiegazioni è `RAPPORTO_STABILIZZAZIONE.md`.

Pronto e verificato in locale:
- versione 1.0.0, iOS build **3**, Android versionCode **265**;
- protocollo Photon **`p3`** (TestFlight 2 = `1.0.0`): due build si incontrano solo con lo stesso protocollo;
- Roslyn solo nell'Editor;
- Unity: EditMode 381/388 passati (7 esclusi di proposito), 0 falliti; script del player compilati per Android e iOS senza errori e senza
  Roslyn;
- server: `test.js` e `concorrenza.js` passano;
- nessun riferimento ad asset mancanti; nessun segreto nei file da salvare.

## Scelte prima del commit

- **A. Pacchetti non usati.** Cinemachine, glTFast, ProBuilder e Visual Effect Graph sono stati aggiunti dopo la pulizia di Fase 10. Si
  portano dietro Burst, Collections e Splines e hanno modificato `VFXManager.asset` e `ProjectSettings/Packages/`. **Nessun file del gioco
  li usa**, ma entrano nell'app. Consiglio: toglierli prima del commit. Altrimenti si salvano così come sono.
- **B. Android per il Play Store** (non serve per l'APK di prova):
  - l'id del pacchetto è `com.project51.cirulla`, diverso da iOS (`com.deomoreo.51`) e con "Cirulla" dentro; una volta pubblicato non si
    cambia più;
  - manca la chiave di firma per il caricamento (oggi l'APK è firmato con la chiave di debug);
  - lo store vuole un AAB, non un APK.
- **C. `.codex/config.toml`** (approvazioni degli strumenti di Codex): salvarlo o lasciarlo fuori. Non riguarda la build.

## Passi

1. **Commit controllato** (lo fai tu, o io quando mi dai il via)
   - `git add -A`, poi `git status`. Devono esserci:
     - le cancellazioni dei mazzi vecchi (cartelle `51_*_PNG_Unity`, `51_DEFAULT_DECK_Unity`) e di `Resources/Cards`: **già in staging,
       volute**;
     - `home_flame_a.png` sostituito da `BackgroundHome/Flame/`;
     - i file nuovi: `51_Audio_v07_FINAL_CANDIDATE/` con i suoi `.meta` (51 file + 60 meta), `TutorialScript.cs`, `UI51WalletPills.cs`,
       `Plugins/Roslyn/` con i `.meta` "solo Editor", i test nuovi, `Server/QA/`, `concorrenza.js`, `docs/build3/`.
   - Non devono mai comparire: `segreto.txt`, `51.carica.js`, `Server/QA/qa.json` (sono in `.gitignore`).
   - Su un clone pulito: apri con Unity e controlla che compili e che il Mixer audio suoni. La prova dice che il progetto si ricostruisce
     da git.
2. **Deploy del CloudScript** (compatibile con TestFlight 2)
   - `node Server/CloudScript/carica.js` subito prima, poi Game Manager → Automation → CloudScript → Upload di `51.carica.js` e Deploy.
   - Subito dopo: `node Server/QA/qa.js stato Test51QA` per vedere che risponde. Torna indietro alla revisione 19 se qualcosa non va.
   - `Economia.partita.tettoNonVerificabili` non serve, perché vale già 100.
3. **PathLeave nel pannello Photon** (subito dopo il passo 2)
   - PathLeave = `RoomLeft<segreto>` (lo stampa `carica.js`); PathCreate, PathJoin e PathClose come stampati; PathBeforeJoin vuoto;
     accesso anonimo spento.
   - Aggregazione "Last" anche per `XPConcordato` (#102).
4. **Nuovo APK Android** (dal commit del passo 1, versionCode 265)
   - Development build spenta. In fondo alla schermata di accesso deve comparire "v1.0.0 p3".
   - Lo stesso APK su tutti i telefoni di prova.
5. **Prove fondamentali** (§5 del rapporto; con `qa.js stato` prima e dopo le prove sui premi)
   - Primo avvio → tutorial +200 una volta; registrazione, Esci, Accedi con un altro account, ospite, senza dati che passano.
   - Premio giornaliero e Posta con tocchi rapidi; saldo subito giusto.
   - Allenamento: 5 vittorie = 100 monete, poi "Tetto di monete di oggi raggiunto".
   - 1v1 fra due telefoni: confermata, abbandono, rete tolta a fine partita → incompleta → confermata.
   - Rientro: background, app chiusa, rete tolta nel proprio turno e in quello dell'altro; Wi-Fi ↔ rete mobile.
   - Tastiera nei campi principali; amici (richiesta, accettazione, invito).
   - **TestFlight 2 + APK nuovo**: non devono mai finire allo stesso tavolo (ricerca partita e codice stanza). TestFlight 2 da solo deve
     ancora ricevere i premi (evento `partita_senza_biglietto`, tipo `appVecchia`).
6. **Nuova build TestFlight** (iOS build 3, dallo stesso commit)
   - Lettere accentate, safe area su un iPhone piccolo e uno grande, vibrazione, ritorno dal background, cancellazione dell'account (solo
     per gli account veri).

Dopo il lancio:
- ogni settimana PlayStream (`partita_incongruenza`, `partita_senza_biglietto`) e `qa.js` sulle `Incongruenze`;
- `senzaBiglietto.attiva = false` solo quando `appVecchia` è quasi a zero;
- il protocollo (`PhotonAuthConnector.ProtocolVersion`) si alza **solo** quando due build non possono più giocare insieme.
