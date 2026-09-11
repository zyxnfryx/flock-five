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

Garden bed (`Audio/Bed/garden-theme.wav`) is the splash porch theme’s soft
inverse: same original notes, 16 bars @ 96 BPM (40s), guitar + quiet bass,
no kit. Peak ~0.52 so MixDesk PlaceCap (~0.24) sits it under play. C# dawn
flute remains fallback if the wav is missing. Feeder ching (`Audio/Ching/ching_00–11`)
is an original synthesized Flock Five bank. Runtime C# ports match the
signed-off D-major garden mixes when wavs are not in Resources.

Feeder rattle (`Audio/Rattle`, procedural `MakeFeederRattle`) is an original
Flock Five bank: soft glass/metal feeder shake plus wood bump for poke and
sparrow perch taps. Commercially clear. Distinct from Ching score payoff and
Coin/piggy clink — not a ding, gong, or alarm.

Sparrow yell (`Audio/Yell`, procedural `MakeSparrowYell`) is an original harsh
short squawk for tap-scare flee. Dove/yell family, not hummingbird select chips
(`hum_sel_*` / Chirp) and not pitched-up chirps.

Hawk cry (`Audio/Hawk`, procedural `MakeHawkCry`) is an original deeper raptor
kee/scream bank for the hawk pest — longer and lower than SparrowYell, not
hummingbird select chips (`hum_sel_*` / Chirp). Soft band-limited grit; commercially clear.

Pig oink (`Audio/Oink`, procedural `MakeOink`) is an original cute soft snort-oink
bank for splash pig poke. Low-mid body (~150–350 Hz) + soft nasal chiff; not coin
Clink, Ching, gong, alarm, SparrowYell, or HawkCry. Commercially clear.

Splash title: `splash-theme.wav` loops the band (12 bars @ 128 BPM, 22.5s)
once the splash page is on screen — no guitar/harp/crash one-shot. Original
notes — not a cover.
Guitar is a real steel-string acoustic, sampled (FreePats FSS Steel-String /
FlameStudios Seagull, GPL-3+ with FreePats composition exception); MIDI only
hits recorded pitches (E2–C6). Fluidsynth chorus/reverb off. Bass: sampled
Yamaha RBX (FreePats FingerBass YR, CC0). Drums: MuldjordKit (Lars Muldjord /
FreePats, CC-BY-4.0). Cowbell original. C# `MakeSplash` is fallback only.
Sole splash bed occupant. Chirps stay real hummingbirds.

Gate activate (`Sfx.GateGo`) is an original chevron-lock + watery vortex whoosh
for the splash flower. Inspired by the *gesture* of a Stargate kawoosh, not a
copy of any recording.
