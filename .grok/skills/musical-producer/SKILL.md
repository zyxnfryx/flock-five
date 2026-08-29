---
name: musical-producer
description: >
  Musical producer for Flock Five. Mixes background music, ambience, and
  gameplay SFX so a human player hears a garden, not a carnival. Use when
  adding or changing sounds, music, chirps, flaps, bees, feeders, mix,
  volume, ducking, or anything audio; when the user mentions clash, busy,
  loud, carnival, squeaky, sharp, harsh, or ensemble; or when running
  /musical-producer.
---

# Musical producer — Flock Five

You are the record producer. Every new sound must earn its seat in a three-layer mix. If it stacks on an occupied seat, duck it, delay it, or omit it.

## Layers (one occupant at a time)

| Layer | Seat | Occupants | Rule |
|---|---|---|---|
| **Lead** | Player action | hop, chirp, takeoff/land, whoosh, branch crunch, combo, deny, unveil, finale | Always heard. Marks a quiet window after. |
| **Mid** | Garden creatures | wingbeats, one bee hum, snooze | Play only if lead is not hot. Soften, never chorus. |
| **Bed** | Place | MixDesk BedBus: day / dusk / night place stems, combo fifth, quiet moon lift | Continuous and quiet. Duck under lead. Never a melody-loop. No chimes. |

Gameplay must never wait on music. Lead wins.

The ban on a "looping song" is carnival melody-loops (tunes, drum loops, chord pads that sing). It is not a ban on a reactive stem score. Multiple non-melodic stems may loop behind **one Bed seat** on MixDesk's BedBus. Place (day/dusk/night) is one exclusive occupant; the combo fifth sits on the same seat, ducked with it. "More than one looping bed" as a carnival fail means more than one looping **melody**, not place air plus combo warmth.

## Carnival test

Before shipping a sound, imagine the noisiest legal frame: 5 hovering birds + 4 bee shrouds + two feeders + a hop + a x2 combo. If it would sound like a fairground, the new sound fails.

Fail conditions (any one is enough):

- Two pitched melodies at once (celebrate arpeggio vs a moonrise chime).
- More than one looping melody. Non-melodic BedBus stems (place air) may layer.
- A scheduled decoration (idle ruffle, bee hum) during lead.
- Identical timbre stacked (five unsynced bee hums).
- Raw hash/white noise in any clip. Grain is a mix error; lowpass or use sines.

## Tone: warm garden, never squeaky

The player has flagged the mix as too squeaky. Treat that as a standing mix note.

- **Selection is real hummingbirds.** `Sfx.Chirp` plays a bank of recorded chips (`Resources/Audio/Select`, 12 unique). Do not replace them with sine chirps. Do not pitch them up. Master is highpass 500 / denoise / asetrate 0.84 / lowpass 5.2 kHz. Playback pitch stays ~1.00.
- Synthesized voices (deny, bed, bees) stay around 380–720 Hz, dove-like, not piccolo. Place stems sit much lower (day 52/78/104, dusk 41/62/82, night 36/54/72).
- No playback pitch above ~1.04 on any clip. Pitching a recording up is how a hummingbird turns into a whistle.
- Synthesized clips go through a gentle lowpass (~2 kHz). Real hummingbird chips already sit near 6–8 kHz — leave that band, only roll off above ~8 kHz so phone speakers don't hash.
- No gongs, bowls, or Pavlov bells. `Sfx.Celebrate` is a no-op. The flock payoff is the feeder whoosh plus the branch crunch.
- Sleep (`Sfx.Sleep`) is a 12-clip bank of brief fun snores (`Resources/Audio/Snooze`) as Lead, so the hop does not duck it. Fires when a full flock sits with no matching feeder. Idle `Snooze` may reuse the same bank at Mid, quietly.
- Zips, pips, and 1 kHz+ sweeps are forbidden on synthesized voices. Bee and scatter stay low buzz / whoosh. Do not put wind-chimes in the bed.
- `Sfx.Moonrise` is a MixDesk Bed swell on the night stem. Never a G–C–E (or any) arpeggio on Lead.

## Flock Five key

- **Selection chips:** real hummingbird bank of 12 unique chips (`Sfx.Chirp` / `Audio/Select` `hum_sel_00–11`). One shot per tap, never the same clip twice in a row. Do not synthesize replacements. Do not pitch them up.
- **Flock pickup:** no bell. Birds take off, feeder whooshes out, then the branch crunch lands. That crunch is the reward.
- **Combo:** two or more feeders paid in one move (`Sfx.Combo`). Extra whooshes and a harder camera punch — never stacked gongs, never a new melody. MixDesk `ComboWarm` fades in a quiet fifth (78+117 Hz, no tune), recedes ~4s after last collect, ≤ 0.04 × duck.
- **Unmatched nap:** 12 brief snores when a full flock sits without a matching feeder (`Sfx.Sleep` / `Audio/Snooze`). Lead, so it is heard after the hop. Never the same clip twice.
- **Branch break:** THE player reward (`Sfx.Break` / `Audio/Break`, 12 clips). Fresh celery snap — hollow tick, pith tear, then splinters flying outward. ~5ms attack, mid-band crack, almost no energy above 4 kHz. Never a gunshot. If anything else is speaking, the crunch still wins.
- **Feeder leave:** 12 satisfying receding whooshes (`Sfx.FeederDone` / `Audio/Whoosh`). Unhook + falling swoop + silk air. Motion of an object leaving, never coarse blowing wind, never a bell.
- **Bee/mid:** low buzz, no melody, no raw noise. Scatter is buzz/whoosh, never a bell or pip train.
- **Bed:** MixDesk BedBus place stems plus optional combo fifth. No wind-chime. No hash crackle. No song.

## Where the mix lives

Runtime mixer: `Assets/_Project/Scripts/App/MixDesk.cs` via `Sfx`.

- New one-shots go through `Sfx` with a layer (`Lead` / `Mid` / `Bed`).
- Do not add a second `AudioSource` loop from `Sfx`. All loops live on MixDesk's BedBus.
- Place stems (day / dusk / night) crossfade from `SkyCycle.Dusk` as one Bed occupant. Combined place ≤ 0.06. Combo stem is extra warmth on the same seat, ≤ 0.04 × duck, not a second place bed.
- Hard duck under lead: hop/chirp 0.18, whoosh 0.15, break 0.08 (`MixDesk.DuckChirp` / `DuckWhoosh` / `DuckBreak`).
- Bee presence: one garden-wide hum, not one per swarm.
- Idle ruffles: MixDesk may refuse the sample; the wing visual can still play.
- Do not generate wind-chimes. Feeder sway is visual only.

## When adding audio

1. Name the layer.
2. If Mid or Bed, add a refuse/duck path for a hot lead.
3. Keep the clip short. Beds loop; decorations do not. Gameplay never waits on a stem.
4. Volume: bed ≤ 0.06, mid ≤ 0.28, lead 0.5–0.85. Exception: branch break plays at 1.0 — it is the hit.
5. Play the noisiest frame in your head. If two pitched things speak, cut the decoration.

## Do not

- Add a looping song, drum loop, or chord pad that sings.
- Fire a decoration on every sway frame, every bee, or every idle bird at once.
- Raise volume to “make sure they hear it.” Carve space instead.
- Ship a squeaky, sharp, or harsh clip. Warmth over brilliance.
- Put moonrise, celebrate, or any arpeggio on Lead.
