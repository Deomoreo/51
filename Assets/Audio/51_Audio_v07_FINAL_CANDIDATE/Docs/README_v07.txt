51 — AUDIO v07 FINAL CANDIDATE
================================

Questo è il primo pacchetto pensato come SISTEMA SONORO unico, non come raccolta di prove.

COSA È NUOVO
- UI: nuova famiglia tattile derivata dall'ultima sorgente dedicata.
- Your Turn: click tattile + cue brillante molto corto.
- Notification: più morbida e meno importante del turno.
- Card Play: 5 take REALI dalla sorgente dedicata al feltro.
- Card Capture: 4 take di raccolta + piccolo layer di feltro.
- Deal 3 / Deal 6: costruiti con colpi REALI differenti e timing umano.
- Shuffle: 3 take utili inclusi in Extras.
- Accuso: 3 varianti con gesto fisico + firma musicale corta.
- Scopa: 3 varianti fisiche con un layer celebrativo molto discreto.
- Victory / Defeat: accorciati a stinger da partita, non mini-colonne sonore.

COSA HO TENUTO
- home_theme_loop.ogg
- match_start.wav
- reward_coin.wav
- reward_gem.wav
Questi sono stati mantenuti perché il materiale precedente era già utilizzabile.

PRIMA DI INTEGRARE
Ascoltare:
1. Previews/51_v07_FINAL_CANDIDATE_reel.wav
2. Previews/51_v07_SIMULATED_MATCH.wav

NOTA IMPORTANTE
Non ho modificato il progetto Unity. Questa è una candidate build audio da approvare.
Dopo approvazione conviene:
- estendere CardPlay a 5 varianti;
- CardCapture a 4 varianti;
- Scopa a 3 varianti;
- ridurre PitchJitter di CardPlay a ~0.015 e Capture a ~0.010;
- implementare MUSIC ducking, non Master ducking:
  Accuso/Scopa circa -4/-5 dB, ritorno ~0.8 s;
  Victory/Defeat circa -7/-9 dB durante lo stinger.
