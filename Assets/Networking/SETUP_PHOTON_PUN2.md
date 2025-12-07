# Setup Photon PUN 2 per Project51

## ?? Installazione Photon PUN 2

### Metodo 1: Unity Asset Store (Consigliato)
1. Apri Unity Asset Store (Window ? Asset Store)
2. Cerca "Photon PUN 2 FREE"
3. Scarica e importa il package
4. Accetta l'importazione di tutti i file

### Metodo 2: Package Manager con URL
1. Apri Package Manager (Window ? Package Manager)
2. Click su "+" ? "Add package from git URL"
3. Non disponibile per PUN 2 (usa Asset Store)

## ?? Configurazione Account Photon

1. Vai su https://www.photonengine.com/
2. Registrati/Login
3. Crea una nuova App:
   - Tipo: **Photon PUN**
   - Nome: "Project51_Cirulla" (o come preferisci)
4. Copia l'**App ID** generato

5. In Unity:
   - Window ? Photon Unity Networking ? PUN Wizard
   - Incolla l'App ID
   - Click "Setup Project"

## ?? Verifica Installazione

Dopo l'import, dovresti vedere:
- `Assets/Photon/` folder
- `Assets/PhotonServerSettings` file

## ?? Piano Tier Gratuito Photon
- **20 CCU** (Concurrent Users) gratuiti
- Sufficiente per iniziare
- Upgrade disponibile se necessario

## ?? Configurazioni Consigliate

In `PhotonServerSettings`:
- **App Id PUN**: [il tuo App ID]
- **Fixed Region**: "eu" (Europa per latenza migliore)
- **Run In Background**: ? (importante per mobile)
- **Start In Offline Mode**: ?
- **Pun Logging**: Full (durante sviluppo), poi Informational

## ?? Configurazione Mobile Specifica

1. **Android**: 
   - Build Settings ? Android
   - Internet Access: "Require"
   
2. **iOS**:
   - Build Settings ? iOS
   - Capability: Background Modes (se necessario)

## ?? Note Importanti
- Non committare il tuo App ID su repository pubblici
- Usa `.gitignore` per `PhotonServerSettings.asset` se pubblico
- Per questo progetto privato, va bene committarlo

## ?? Test Connessione

Dopo il setup, usa il prefab `PhotonServerSettings` test scene fornita da Photon per verificare la connessione.

---

## ?? Prossimi Passi
Dopo l'installazione, procederemo con:
1. NetworkManager
2. Lobby System
3. Room Creation/Join
4. GameState Sync
