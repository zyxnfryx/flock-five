# Flock Five

iPhone portrait puzzle: assort tropical hummingbirds on branches. Five of a color on a matching live feeder and they flock. The feeder whooshes out. The branch snaps.

**Unity 6.3 LTS** — editor `6000.3.22f1`. URP. Input System only.

## Play

1. Open this folder in Unity Hub with **6000.3.22f1**.
2. Press Play, or **Flock Five → Play Slice**.
3. Runtime bootstrap builds the garden (no baked scene required).

Tap a branch, tap a destination. Same-color tip runs hop. Undo / Restart sit on the HUD.

## Gameplay now

- Six gardens, one after another. Clear a garden and the next one loads. Restart stays on the current garden.
- Level 1 **Dawn Garden**: 8 branches, 5 seats, 4 colors (palette cap is 5 for future stages). Live Ruby + Gold.
- Level 2 **Bee Thicket**: shorter stacks, more bees. Live Gold + Teal.
- Level 3 **Noon Queue**: dawn's rows with the feeders flipped. Live Teal + Violet.
- Level 4 **Dusk Scatter**: mixed stacks, three empty perches. Live Ruby + Violet.
- Level 5 **Moonrise Nap**: pair stacks and a mixed row. Live Gold + Violet.
- Level 6 **Night Lattice**: six occupied limbs, heavier bees. Live Teal + Ruby.
- Two live feeders, rest in a queue. Completing five of a live color flocks them.
- Inner birds can be bee-shrouded (black silhouettes). Bees lift only when a shrouded bird is actually at the tip.
- A full flock with no matching feeder naps (snore). Completing a live color can hang their feeder and chain a **combo**.
- Combo = two or more feeders paid in one move: extra whoosh, harder camera punch.

## Layout

- `Assets/_Project/Scripts/` — puzzle (`Board`, `LevelData`), views, audio (`Sfx`, `MixDesk`).
- `Assets/_Project/Art/Resources/` — sprites and audio banks (Select / Whoosh / Break / Snooze).
- `.grok/skills/musical-producer/` — mix rules for anyone continuing audio.

Audio credits: `Assets/_Project/Art/Resources/Audio/CREDITS.md`.

Bundle `com.zfxgames.flockfive` · portrait · company zfxgames.
