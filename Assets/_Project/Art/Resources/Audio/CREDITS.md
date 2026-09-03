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

Feeder whooshes (`Audio/Whoosh/whoosh_00–11`) and branch crunches
(`Audio/Break/break_00–11`) are original synthesized banks: receding
swoops (object leaving), and celery-snap wood with outward splinters.

Garden beds (`Audio/Bed/dawn-garden`, `mid-climb`, `last-light`) and feeder ching
(`Audio/Ching/ching_00–11`) are original synthesized Flock Five banks,
commercially clear. Runtime C# ports match the signed-off D-major garden mixes
when wavs are not in Resources.
