51 — Audio Pack v02
=====================

Creato esclusivamente dagli audio già presenti nel progetto, senza nuove generazioni a crediti.

NUOVI FILE
- card_deal_3.wav       Distribuzione rapida di 3 carte
- card_deal_6.wav       Distribuzione più lunga / animata
- your_turn.wav         Avviso leggero: è il tuo turno
- ui_back.wav           Indietro / chiusura
- ui_error.wav          Azione non valida, volutamente non aggressiva
- ui_tab.wav            Cambio tab
- popup_open.wav        Apertura pannello / popup
- popup_close.wav       Chiusura pannello / popup
- notification_soft.wav Notifica generica discreta
- match_start_TEMP.wav  Inizio partita provvisorio; da sostituire solo se non convince in-game

ANCORA DA GENERARE QUANDO AVREMO CREDITI / ALTRO SERVIZIO
- defeat.wav
- reward_coin.wav
- reward_gem.wav
- match_start.wav definitivo (solo se il TEMP non funziona)

UNITY
Per questi SFX brevi:
Load Type: Decompress On Load
Compression Format: ADPCM (o PCM per click molto corti)
Preload Audio Data: On

Volumi iniziali indicativi:
card_deal: 0.60
your_turn: 0.55
ui_back/ui_tab: 0.50
ui_error: 0.55
popup: 0.50
notification_soft: 0.50
match_start_TEMP: 0.70

Consiglio: usa un AudioMixer con gruppi Music e SFX separati.
