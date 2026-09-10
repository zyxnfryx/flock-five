# Audio credits

Selection chips are real hummingbird recordings, sliced and mastered
(highpass 500 Hz, denoise, asetrate 0.84, lowpass 5.2 kHz, loudness match,
then a gentle high shelf −5 dB above ~4.5 kHz so phone speakers don't whistle).
Not synthesized. The bank is 12 unique chips (`hum_sel_00–11`).

Color voices (one real chip each; a named `Audio/Select/{ruby,gold,teal,violet,peach}`
or `Audio/Chirp/{name}` file overrides). Assigned from the same NPS bank:

| Color | Clip |
|---|---|
| Ruby | `Select/hum_sel_00` |
| Gold | `Select/hum_sel_01` |
| Teal | `Select/hum_sel_02` |
| Violet | `Select/hum_sel_03` |
| Peach | `Select/hum_sel_04` |

`hum_sel_05–11` stay in the folder unused. Selection is never synthesized.

| Clip | Source | License |
|---|---|---|
| `Select/hum_sel_00–01` | Ruby-throated Hummingbird call, U.S. National Park Service | Public domain |
| `Select/hum_sel_02` | Broad-tailed Hummingbird call, Cow Creek Trail, Rocky Mountain National Park, National Park Service (J. Job, 2016-05-25). Isolated call ~2.84 s. [Page](https://www.nps.gov/romo/learn/photosmultimedia/sounds-broadtailedhummingbird.htm), [mp3](https://www.nps.gov/nps-audiovideo/legacy/mp3/imr/avElement/romo-BTAHROMO5252016CowCreekTrail.mp3) | Public domain |
| `Select/hum_sel_03–05` | Broad-tailed Hummingbird call, Fern Lake Trail, Rocky Mountain National Park, National Park Service (J. Job, 2015-06-10). [Page](https://www.nps.gov/romo/learn/photosmultimedia/sounds-broadtailedhummingbird.htm), [mp3](https://www.nps.gov/nps-audiovideo/legacy/mp3/imr/avElement/romo-BTAHROMO6102015FernLakeTrail.mp3) | Public domain |
| `Select/hum_sel_06–11` | Broad-tailed Hummingbird call, Cow Creek Trail, Rocky Mountain National Park, National Park Service (J. Job, 2016-05-25). [Page](https://www.nps.gov/romo/learn/photosmultimedia/sounds-broadtailedhummingbird.htm), [mp3](https://www.nps.gov/nps-audiovideo/legacy/mp3/imr/avElement/romo-BTAHROMO5252016CowCreekTrail.mp3) | Public domain |

Processed for Flock Five.

Phone remaster (this pass): select bank high-shelf −5 dB above 4.5 kHz;
break bank mixed with a 1.5–3 kHz pith-tear so the celery snap reads on
iPhone (still not a gunshot); snooze bank pitched so the dove-range
180–320 Hz tone is audible on phone speakers.

Sleep snores (`Audio/Snooze/snore_00–11`) are an original synthesized bank
for Flock Five. Not third-party recordings.

Combo is the wood-pluck jingle only (`Sfx.Combo`). The spoken announcer
bank is unused.

Feeder whooshes (`Audio/Whoosh/whoosh_00–11`) and branch crunches
(`Audio/Break/break_00–11`) are original synthesized banks: receding
swoops (object leaving), and celery-snap wood with outward splinters.

Garden beds (`Audio/Bed/dawn-garden`, `mid-climb`, `last-light`) and feeder ching
(`Audio/Ching/ching_00–11`) are original synthesized Flock Five banks,
commercially clear. Runtime C# ports match the signed-off D-major garden mixes
when wavs are not in Resources.

Splash title (`Audio/Bed/splash-theme`) Phase 1 Genesis-FM warm revise: original soft-FM
(YM2612-approx) stem biased toward Shining Force II heart (softer EP/flute-op, less brass).
Same D-major motif DNA and intro→statement→bridge→payoff→cadence arc. Not a ROM rip.
LoadBed prefers the WAV; C# MakeSplash remains fallback until signed off. Sole bed
occupant on the splash; garden stems mute until play. Chirps stay real hummingbirds.

Bird Poker casino bed (`Audio/Bed/poker-casino`) is an original Genesis-FM soft-FM
loop (YM2612-approx) — third music identity for the Bird Poker page. C-major chip-leap
swagger (dotted G–E → C5 leap → walk down; C–E–G–C ladder response) at 108 BPM /
12 bars ≈ 26.7s. Not a ROM rip; not the splash A–B–A–F# title; not the garden
D–E–F#–A flute motif. LoadBed prefers the WAV; C# MakePoker is a short soft-FM/sine
fallback so the page is never silent. Sole bed occupant on poker (splash + garden +
rain mute while poker mix is up). Chirp banks unchanged.

Gate activate (`Sfx.GateGo`) is an original chevron-lock + watery vortex whoosh
for the splash flower. Inspired by the *gesture* of a Stargate kawoosh, not a
copy of any recording.
