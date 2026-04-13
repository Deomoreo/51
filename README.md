# Project 51 (Cirulla) — Project Bible

Questa `README.md` è la **bibbia** del progetto. Prima di fare modifiche o proporre soluzioni, va letta e usata come riferimento principale.

## Contesto

- Gioco Unity di carte: **51 / Cirulla**.
- Stiamo costruendo l'app e la UX **stile Clash Royale** (home con HUD, banner profilo, tap-to-enter, modali, ecc.).

## Regole di lavoro (importantissime)

### Compilazione / Errori IDE

Se durante lo sviluppo compaiono errori di compilazione in Visual Studio / IntelliSense (anche se il codice è corretto):

- **Procedere comunque** con lo sviluppo.
- Il fix spesso è:
  1. Chiudere Visual Studio
  2. Riavviare/rigenerare i progetti da Unity (o riaprire Unity)
  3. Riaprire Visual Studio

In altre parole: **non rimuovere cambiamenti solo per “far compilare subito”**, perché potrebbe essere un problema di sync Unity/VS e non del codice.

## Linee guida generali progetto (sempre valide)

### UI / UX

- Preferire flussi UX chiari e prevedibili: niente auto-navigazioni “a sorpresa”.
- Separare sempre UI di sistema (loading/gate/auth) dalla UI di gioco (HUD/pagine).
- Evitare dipendenze rigide tra UI e sistemi core: usare eventi/callback o servizi centrali.

### Persistenza / Account

- Non salvare mai password in chiaro.
- Usare flag locali (es. PlayerPrefs) solo come cache/UX, non come fonte di verità server.
- Ogni schermata che implica identità/account deve avere un percorso chiaro per logout/switch account.

### Codice

- Preferire modifiche minime e incrementali.
- Quando possibile, centralizzare la logica (es. auth) in un servizio unico e far aggiornare le UI tramite eventi.

### Performance / Ottimizzazione

- Disattivare o nascondere UI non necessarie quando si entra nel gameplay.
- Evitare `FindObjectOfType` in loop; se necessario, farlo una volta e cache.
