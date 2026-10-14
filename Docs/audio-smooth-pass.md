# Music smoothness pass (build 89)

Request: Brandon, relayed by Chief of Staff on Oct 13 at 11:47 PM PT. Android first impressions said the music sounds harsh at high volume.
Base: `b87/ipad-layout` @ 50c88b0. `main` (f36d2e3, Sep 10) is stale and has no Bed folder.

## Root causes (measured)
1. **Every authored bed was imported with Force To Mono = 1 and Normalize = 1.** Unity summed L+R to mono and peak-normalized each clip to 0 dBFS, so the beds reached the bus with no headroom (TP 0.0 dBTP).
2. **No master stage.** Bed + Lead sting + studio jingle could sum past full scale, and a phone's speaker amp then hard-clips.
3. **Jingles and stings were the hottest things in the game.** The studio jingle ran at 0.9 (-16.2 LUFS in game) and the combo stings at 0.76 (-14.5 LUFS). In the phone sim at max volume they clipped the speaker 10–21% of the time.
4. **Storm rain** (procedural) had 43% of its energy at 3–8 kHz, the brightest thing in the mix.

## Shared pieces
- `MasterSoftClip` (new, on the AudioListener): the one bus limiter for music, SFX and the studio jingle.
- `AudioPolish.Apply` (new): the same chain that mastered the authored assets offline (`Docs/audio-smooth/tools/smooth.py`). It runs on every procedural music clip. The C# and Python implementations match to -112 dB.

## Results
Phone sim: band-pass 300 Hz–8 kHz (2nd order), +6 dB peak at 3 kHz (Q 1.1), +14 dB max-volume drive, tanh speaker saturation.
"Before" is as Unity actually played it (mono-summed, normalized) at the old gain. "After" is the new asset at the new gain.

| Track | In-game gain | LUFS (asset) | True peak dBTP | Crest dB | 3–8 kHz % | Sibilance p95 dB | Harsh-res peak dB | Phone: harsh (2–6k vs 0.3–2k) dB | Phone: clip % | Phone: distortion dB | In-game LUFS |
|---|---|---|---|---|---|---|---|---|---|---|---|
| splash-theme | 0.52 → 0.760 | -12.7 → -17.0 | 0.0 → -5.3 | 14.9 → 13.8 | 2.07 → 1.17 | -13.1 → -17.0 | 3.9 → 3.9 | -5.9 → -7.5 | 1.77 → 0.75 | -15.5 → -17.5 | -18.4 → -19.4 |
| garden-theme | 0.24 → 0.300 | -14.0 → -17.0 | 0.0 → -4.0 | 16.4 → 15.2 | 1.53 → 0.84 | -14.4 → -18.0 | 4.1 → 4.0 | -6.7 → -8.5 | 0.00 → 0.00 | -25.0 → -27.8 | -26.4 → -27.4 |
| badger-battle | 0.40 → 0.478 | -14.5 → -17.0 | 0.0 → -3.0 | 16.9 → 16.4 | 0.81 → 0.31 | -17.0 → -21.1 | 1.6 → 1.7 | -8.1 → -10.3 | 0.46 → 0.14 | -16.9 → -19.1 | -22.4 → -23.4 |
| badger-entrance | 0.40 → 0.478 | -17.0 → -19.8 | 0.0 → -2.9 | 19.0 → 18.9 | 0.34 → 0.13 | -15.5 → -20.1 | 2.0 → 1.6 | -10.1 → -12.1 | 0.22 → 0.07 | -13.1 → -17.7 | -25.0 → -26.2 |
| badger-win | 0.40 → 0.542 | -12.4 → -16.0 | 0.0 → -5.3 | 15.8 → 13.7 | 0.96 → 0.32 | -23.4 → -27.6 | 3.4 → 3.7 | -10.8 → -13.4 | 2.76 → 1.13 | -14.8 → -17.2 | -20.4 → -21.3 |
| badger-lose | 0.40 → 0.542 | -11.3 → -16.0 | 0.0 → -5.8 | 14.0 → 13.0 | 0.49 → 0.15 | -14.1 → -19.1 | 1.9 → 1.9 | -10.8 → -13.1 | 2.51 → 0.29 | -15.7 → -19.4 | -19.3 → -21.3 |
| zfx-jingle v4.1 (shipped ogg) | 0.90 → 0.651 | -15.3 → -16.0 | -1.8 → -3.9 | 17.6 → 15.9 | 0.18 → 0.07 | -28.3 → -32.8 | 3.9 → 3.5 | -11.3 → -14.0 | 10.24 → 3.92 | -8.2 → -12.1 | -16.2 → -19.7 |
| proc dawn (fallback) | 0.24 → 0.300 | -17.2 → -20.0 | -7.9 → -12.4 | 19.7 → 18.8 | 0.04 → 0.01 | -17.6 → -21.9 | 2.9 → 2.8 | -21.3 → -23.7 | 0.00 → 0.00 | -33.7 → -38.6 | -29.6 → -30.5 |
| proc mid (fallback) | 0.24 → 0.300 | -17.4 → -20.2 | -6.3 → -11.4 | 16.8 → 15.2 | 0.04 → 0.02 | -19.8 → -23.3 | 2.3 → 2.2 | -21.5 → -23.2 | 0.00 → 0.00 | -35.2 → -39.8 | -29.8 → -30.7 |
| proc last-light (fallback) | 0.24 → 0.300 | -16.1 → -19.1 | -5.0 → -8.6 | 17.3 → 16.8 | 0.06 → 0.03 | -16.2 → -19.8 | 2.5 → 2.4 | -19.0 → -20.5 | 0.00 → 0.00 | -31.9 → -35.9 | -28.5 → -29.5 |
| proc splash (fallback) | 0.52 → 0.760 | -16.4 → -20.8 | -4.1 → -9.6 | 14.6 → 13.6 | 0.04 → 0.02 | -31.0 → -32.7 | 2.3 → 2.2 | -21.3 → -22.1 | 0.11 → 0.01 | -21.3 → -24.4 | -22.1 → -23.2 |
| proc storm rain | 0.17 → 0.170 | -13.6 → -14.4 | -5.1 → -5.8 | 14.0 → 13.5 | 42.64 → 22.74 | -3.8 → -6.4 | 0.6 → 1.0 | 5.3 → 2.0 | 0.00 → 0.00 | -37.0 → -37.9 | -29.0 → -29.8 |
| proc combo fifth | 0.04 → 0.040 | -28.6 → -29.5 | -10.8 → -14.4 | 20.0 → 17.3 | 0.88 → 0.38 | -17.1 → -21.3 | 3.0 → 3.0 | -6.9 → -8.4 | 0.00 → 0.00 | -75.3 → -79.7 | -56.5 → -57.5 |
| proc combo jingle x2 | 0.76 → 0.480 | -15.4 → -16.4 | -6.0 → -7.0 | 11.6 → 11.8 | 0.00 → 0.00 | -81.1 → -83.7 | -0.6 → -0.0 | -43.1 → -55.1 | 3.55 → 0.00 | -14.3 → -20.7 | -17.8 → -22.8 |
| proc combo jingle x3 | 0.76 → 0.480 | -14.5 → -15.4 | -5.2 → -6.1 | 11.7 → 11.9 | 0.00 → 0.00 | -76.5 → -77.6 | -0.4 → -0.2 | -31.8 → -42.0 | 9.55 → 0.85 | -12.1 → -17.5 | -16.9 → -21.8 |
| proc combo jingle x4 | 0.76 → 0.480 | -13.5 → -14.1 | -5.6 → -6.2 | 11.0 → 11.1 | 0.00 → 0.00 | -74.8 → -75.1 | -1.2 → -0.5 | -27.1 → -33.5 | 14.32 → 2.07 | -11.4 → -16.1 | -15.9 → -20.5 |
| proc combo jingle x5 | 0.76 → 0.480 | -13.4 → -14.1 | -4.2 → -5.1 | 12.1 → 11.9 | 0.00 → 0.00 | -67.9 → -68.1 | -1.4 → -0.3 | -20.2 → -25.1 | 17.45 → 2.76 | -11.0 → -15.7 | -15.8 → -20.4 |
| proc combo jingle x6 | 0.76 → 0.480 | -12.7 → -13.3 | -4.3 → -4.8 | 11.5 → 11.9 | 0.00 → 0.00 | -68.1 → -68.5 | -1.3 → -1.0 | -17.9 → -22.4 | 19.98 → 3.64 | -10.8 → -15.4 | -15.1 → -19.7 |
| proc combo jingle x7 | 0.76 → 0.480 | -12.1 → -12.7 | -4.0 → -4.4 | 11.5 → 11.7 | 0.00 → 0.00 | -68.2 → -68.6 | -1.3 → -0.8 | -17.9 → -22.4 | 21.35 → 4.49 | -10.6 → -15.0 | -14.5 → -19.1 |
| proc combo jingle x8 | 0.76 → 0.480 | -12.1 → -12.7 | -3.2 → -3.9 | 12.4 → 12.4 | 0.00 → 0.00 | -68.0 → -68.2 | -1.3 → -0.5 | -18.5 → -23.3 | 20.93 → 4.28 | -10.5 → -14.9 | -14.5 → -19.1 |
| proc celebrate | 0.80 → 0.500 | -12.8 → -13.3 | -4.5 → -5.9 | 11.1 → 10.4 | 0.00 → 0.00 | -76.6 → -76.7 | -1.1 → -0.8 | -29.5 → -37.0 | 14.83 → 1.26 | -12.3 → -17.9 | -14.7 → -19.3 |

## Rain balance (build 89, Brandon Oct 14 12:45 PM PT: "rain ... a bit overpowering")
`MixDesk.RainCap` 0.17 → 0.068 (-8.0 dB), one constant on stem 5. Measured in game, Wet = 1, no duck:

| | full-band vs garden bed | phone sim vs garden bed |
|---|---|---|
| rain before (0.17) | -2.4 dB | -0.1 dB |
| rain after (0.068) | -10.3 dB | -8.0 dB |

Thunder is unchanged: it was already about level with the bed full-band (+0.4 dB) and stays so. It is now ~10.7 dB over the rain (was ~2.7), which is what a strike over steady rain should be.
