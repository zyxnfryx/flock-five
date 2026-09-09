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
| **Bed** | Place | MixDesk BedBus: dawn-garden / mid-climb / last-light D mixes, combo fifth, quiet moon lift | Continuous garden score. Chirps do not duck it (frequency split). One looping melody family, not a carnival second tune. |

Gameplay must never wait on music. Lead wins.

The ban on a "looping song" is carnival: a second looping melody, gongs, or a 26s brass/choir wall. It is not a ban on MixDesk's three discrete garden beds. Dawn / mid / last-light are one exclusive occupant crossfaded from SkyCycle.Dusk. Each bed is a 16-bar 84 BPM loop that stays sparse, teases the flute at bar 5, and blooms at bar 11 (~2/3) — staggered like a chase cue, warm garden, not brass. The combo fifth sits on the same seat. No fourth place bed. No Last Light choir drone.

## Carnival test

Before shipping a sound, imagine the noisiest legal frame: 5 hovering birds + 4 bee shrouds + two feeders + a hop + a x2 combo. If it would sound like a fairground, the new sound fails.

Fail conditions (any one is enough):

- Two pitched melodies at once (celebrate arpeggio vs a moonrise chime).
- More than one looping melody. Non-melodic BedBus stems (place air) may layer.
- A scheduled decoration (idle ruffle, bee hum) during lead.
- Identical timbre stacked (five unsynced bee hums).
- Raw hash/white noise in any clip. Grain is a mix error. Hop flutters use gated feather rustle (band-limited, envelope-only), never an open noise floor.

## Tone: warm garden, never squeaky

The player has flagged the mix as too squeaky. Treat that as a standing mix note.

- **Selection is real hummingbirds.** One chip per color, forever: Ruby=`hum_sel_00`, Gold=`01`, Teal=`02`, Violet=`03`. `Sfx.Chirp(color)` only, and only on select. Hops are a soft cartoon fwip-fwip (`Sfx.FlockFlutter`): five round whooshes with falling pitch, no grit, staggered ~90ms. Never another chirp on a hop.
- Synthesized voices (deny, bees) stay around 380–720 Hz, dove-like, not piccolo. Garden beds are D-major 84 BPM flute over pizzicato strings only (no pad, kick, or choir). Dawn is walking pizz + flute at 2/3. Mid adds the full motif. Last-light is the complete piece: eighth pizz, chord plucks, flute cadence. Fundamentals under ~1.2 kHz.
- No playback pitch above ~1.04 on any clip. Pitching a recording up is how a hummingbird turns into a whistle.
- Synthesized clips go through a gentle lowpass (~2 kHz). Real hummingbird chips already sit near 6–8 kHz — leave that band, only roll off above ~8 kHz so phone speakers don't hash.
- No gongs, bowls, or Pavlov bells. `Sfx.Celebrate` is a no-op. Feeder SCORE is Ching; feeder LEAVE is Whoosh; the crunch is the reward.
- Sleep (`Sfx.Sleep`) is a 12-clip bank of brief fun snores (`Resources/Audio/Snooze`) as Lead, so the hop does not duck it. Fires when a full flock sits with no matching feeder. Idle `Snooze` may reuse the same bank at Mid, quietly.
- Zips, pips, and 1 kHz+ sweeps are forbidden on synthesized voices. Bee and scatter stay low buzz / whoosh. Do not put wind-chimes in the bed.
- `Sfx.Moonrise` is a MixDesk Bed swell on the night stem. Never a G–C–E (or any) arpeggio on Lead.

## Flock Five key

- **Selection chips:** one chip per color forever (`Sfx.Chirp(color)` / `Audio/Select` `hum_sel_00–03` for Ruby/Gold/Teal/Violet). Every red bird is the same chip. Moves are loud flutter, not chips. Do not synthesize replacements. Do not pitch them up.
- **Flock pickup:** no bell. Score plays Ching once (`Sfx.FeederDone` / `Audio/Ching`). Birds take off, feeder whooshes out (`Sfx.FeederLeave` / `Audio/Whoosh`), then the branch crunch lands. That crunch is the reward. Lead wins.
- **Combo:** two or more feeders paid in one move (`Sfx.Combo`). Extra leave whooshes (`FeederLeave`) and a harder camera punch — never stacked gongs, never a new melody. MixDesk `ComboWarm` fades in a quiet fifth (78+117 Hz, no tune), recedes ~4s after last collect, ≤ 0.04 × duck.
- **Unmatched nap:** 12 brief snores when a full flock sits without a matching feeder (`Sfx.Sleep` / `Audio/Snooze`). Lead, so it is heard after the hop. Never the same clip twice.
- **Branch break:** THE player reward (`Sfx.Break` / `Audio/Break`, 12 clips). Fresh celery snap — hollow tick, pith tear, then splinters flying outward. ~5ms attack, mid-band crack, almost no energy above 4 kHz. Never a gunshot. If anything else is speaking, the crunch still wins.
- **Feeder score:** 12 garden-till chings (`Sfx.FeederDone` / `Audio/Ching`). Wood body + muted brass + coin tap in D/A/F#/E. Not a bell, gong, or whoosh.
- **Feeder leave:** 12 satisfying receding whooshes (`Sfx.FeederLeave` / `Audio/Whoosh`). Unhook + falling swoop + silk air. Motion of an object leaving, never coarse blowing wind, never a bell. `PullAway` and combo extras use leave, not score.
- **Bee/mid:** low buzz, no melody, no raw noise. Scatter is buzz/whoosh, never a bell or pip train.
- **Bed:** flute + pizzicato only on stems 0/1/2. 16-bar loops, payoff at bar 11. Last-light is the complete arrangement (denser pizz, flute cadence). No pad, no kick, no choir, no second flute tune. Combo fifth on stem 3. PlaceCap ~0.24. Chirps do not duck the bed.

## Where the mix lives

Runtime mixer: `Assets/_Project/Scripts/App/MixDesk.cs` via `Sfx`.

- New one-shots go through `Sfx` with a layer (`Lead` / `Mid` / `Bed`).
- Do not add a second `AudioSource` loop from `Sfx`. All loops live on MixDesk's BedBus.
- Garden beds (dawn / mid / last-light) crossfade from `SkyCycle.Dusk` as one Bed occupant. Combined place ≤ 0.24 (moon ≤ 0.28). Combo stem is extra warmth on the same seat, ≤ 0.04, not a fourth place bed.
- Chirps do not duck the bed (`DuckChirp` = 1). Hummingbird chips sit 6–8 kHz; beds stay under ~1.2 kHz. Whoosh 0.88, break 0.78, both slewed. Mid decorations still wait while lead is hot.
- Bee presence: one garden-wide hum, not one per swarm.
- Idle ruffles: MixDesk may refuse the sample; the wing visual can still play.
- Do not generate wind-chimes. Feeder sway is visual only.

## When adding audio

1. Name the layer.
2. If Mid or Bed, add a refuse/duck path for a hot lead.
3. Keep the clip short. Beds loop; decorations do not. Gameplay never waits on a stem.
4. Volume: bed ~0.24 (chirps do not duck it), mid ≤ 0.28, lead 0.5–0.85. Exception: branch break plays at 1.0 — it is the hit.
5. Play the noisiest frame in your head. If two pitched things speak, cut the decoration.

## Do not

- Add a second looping melody, drum loop, gong, or a Last Light choir drone.
- Fire a decoration on every sway frame, every bee, or every idle bird at once.
- Raise volume to “make sure they hear it.” Carve space instead.
- Ship a squeaky, sharp, or harsh clip. Warmth over brilliance.
- Put moonrise, celebrate, or any arpeggio on Lead.
