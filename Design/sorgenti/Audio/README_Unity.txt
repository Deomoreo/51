51 AUDIO PACK v01

FILES
- home_theme_loop.ogg / .wav : Home music, loop-ready edit
- card_play_01..03.wav          : randomize when playing a card
- card_capture_01..03.wav       : randomize when collecting cards
- accuso_01..03.wav             : three punch variants to audition/randomize
- scopa.wav                     : Scopa reward
- victory_short.wav             : compact victory cue
- ui_click.wav                  : normal UI click
- ui_confirm_01..03.wav         : optional stronger confirmations

UNITY SUGGESTED IMPORT
MUSIC (home_theme_loop.ogg):
  Load Type: Streaming
  Compression Format: Vorbis
  Loop: enabled on AudioSource
  Start AudioSource volume around 0.25-0.35

SHORT SFX (.wav):
  Load Type: Decompress On Load
  Compression Format: PCM while developing (ADPCM later if desired)
  Force To Mono: optional; keep stereo for UI/reward sounds if preferred

MIX STARTING POINT
  UI click:      0.45
  Card play:     0.65
  Card capture:  0.70
  Scopa:         0.85
  Accuso:        0.90
  Victory:       0.90

These are starting points; final levels should be adjusted in-game against the Home music.
