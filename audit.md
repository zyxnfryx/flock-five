# Flock Five ladder audit (2026-10-18)
Base audited: `b98/integration` tip `2962d920f47de8baa84cca7e01e9e26726a06437` (merged PR #164, Daily 24h window). Code read via gh/sparse checkout; boards measured with the repo's own GardenSolve/TrapProbe in a C# harness (`/workspace/ff/ladder50/h`).

## What is merged vs open
- #164 Daily = 24h window from last claim: MERGED (in base).
- #168 (spring motion, big ostrich art, **garden 32 loses its ostrich**, no sparrows/hawks on ostrich gardens via PestSchedule.OstrichGarden, SparrowVisits/HawkVisits 0 on ostrich): **OPEN, not in base**. At base, garden 32 is still an ostrich stage (PeckSeed 832449) and pests are NOT gated.
- #139 (my leaf guard BranchView + LeafLockFullLimb helper + cycled test): **OPEN, not in base**. Rule A itself (`EnforceFullLeafLimbs` in Pack, LeafLockedLimbsAreFull, Build89NoPartialLeafTests) IS in base.
- Kit-aware matching (Bird.SameFlock color+kit), FlockKit.Assign, ≤3 sets/color (BoardValidator.CheckKitSets), Level.Solo/Kits/Feeders/Ostrich/PeckSeed/PecksPerMove, GardenGen, Ostrich.cs (SplitMix64, 2 pecks default), TrapProbe.cs: all in base.

## Authored boards (LevelData.All = 32 entries) — every one measured at base, flocks 5-birds, leaf rule OK, Winnable without gift
| # | id | kind | feeders | pests (code rule by DisplayNumber) | season | badger |
|---|---|---|---|---|---|---|
| 1-15 | dawn-garden … last-light | named makers (learning ramp, trap-free) | 2 | sparrow budget 0/1/2/3 by level, hawk from 11 | Summer 1-14, Desert 15 | no |
| 16-25 | garden-16..25 | makers + Kits maps (18-22,24,25; 23 run-order) | 2 | sparrow 3, hawk 1 | 16-24 Desert, 25 Winter | 16, 21 |
| 26 | garden-26 | Ruby single-color (Solo) | 2 | normal | Winter | 26 |
| 27 | garden-27 | **OSTRICH debut**, Solo spec, PeckSeed 686559 | **3** | none intended (gating only in #168) | Winter | no |
| 28-30 | garden-28..30 | Teal/Gold/Violet single-color (Solo) | 2 | normal | Winter | no |
| 31 | garden-31 | regular maker (bees) | 2 | normal | Winter | **31 badger** |
| 32 | garden-32 | at base: ostrich spec (PeckSeed 832449); after #168: same 3-feeder board, ostrich off | 3 | at base none gating; after #168 normal pests | **Spring starts at 32** (GardenSeason.SpringFrom = 32) | no |
| 38 | `Garden38Level` (not in All) | ostrich + bees + leaf-lock spec, PeckSeed 941734 | 3 | — | Spring | no |

Measured (harness, 25-40 random "sensible" playouts; Measure with outlook cap 2500): sensible tangle rate — G16 44%, 17 20%, 18 4%, 19 56%, 20 36%, 21 36%, 22 32%, 23 12%, 24 20%, 25 52%, 26 44%, 28 40%, 29 12%, 30 64%, 31 28%. Solver effort (nodes): 16:99, 17:62, 18:38, 19:76, 20:70, 21:103, 22:5356, 23:257, 24:1185, 25:90, 26:14, 27:50, 28-30:~10, 31:143, 32:105 (O). Raw: `audit_ladder.txt`, `runs/aud*.txt`. Note: eased 18-25 targets (30-60%) are NOT all met in this measure (18: 4%, 23: 12%, 24: 20%, 17: 20%) — my measure uses a different playout policy/runs than earlier Level-Design harness numbers, so treat as indicative; flag for Brandon.
**Garden 38's authored spec is defective**: its leaf-locked limb `RBGCGC?TB` has only 4 birds, so Rule A (Pack) silently drops the lock → 38 has no leaf-lock. Needs re-authoring.

## Where the ladder really ends
- `LevelData.Peek(i)` = `All[i % All.Length]`; `Open(i)` uses the same wrap. There is **no clamp and no end card**: after garden 32 the game **loops** to the garden 1 layout (Dawn Garden) while `DisplayNumber = Index+1` keeps counting (level 33 is Dawn Garden's board, 34 = Bee Thicket, …, 59 = garden 27 again incl. ostrich, 64 = garden 32).
- Because season, badger and pest budgets key off DisplayNumber, level 33+ gets spring/summer-rotation art, badger every 5, and full pest budgets (sparrow 3 + hawk 1) on top of the easy early boards. Tests assert `LevelData.Count == 32` (Build82LevelChecks, Build83TestsD).
- So Brandon's "boards 33-37 missing" is exact: levels 33+ currently replay boards 1+.

## Planned-but-unbuilt vs reality (gardens pipeline)
Spec files from earlier turns (pipeline.txt, ostrich-impl.txt, ease-18-25.txt, fresh-gardens-plan.md) are **no longer on this box** (workspace was reset; /workspace/ff/gardens-pipeline and /workspace/ff/ostrich do not exist). What exists in the repo: GardenGen (feeders/ostrich knobs + hash), TrapProbe (measure/proof finder), Ostrich.cs, GardenSolve, Build82LevelChecks proofs table. What does **not** exist in the repo: GardenData JSON storage/loader, GardenHash.Canonical (GardenGen.Hash covers feeders+ostrich only), LadderGateTests over a catalog, GardenBands, a generator tool, Daily Garden pool/"more gardens coming" end card, any garden 33+.
- My earlier targets "150 at launch / +60 per 2 weeks to 300" are **plans only; reality is 32 authored gardens + 1 authored-but-unwired (38), looping after 32**. Launch-readiness bar was set to level 32; Brandon now wants ≥50 planned.
- Season facts differ from earlier notes: code has Spring from **32** (not 31) and the 40+ rotation counts the next 8 **from where the change actually landed** (= my "Option B / shifted grid", NOT the approved Option A fixed grid). Code changes: 40 Summer, 48 Fall, 57 Winter, 65 Spring, 73 Summer, 82 Fall, 90 Winter, 98 Spring, 107, 115, 123, 132, 140, 148, 157, 165, 173, 182, 190, 198, 207, 215, 223, 232, 240, 248, 257, 265, 273, 282 (`seasons_code.txt`). Option A would give 40,48,57,64,72,80,88,97,… Open question for Brandon: keep code's B or switch to A.
- Badger levels (BadgerSchedule: FirstAfter 15, Interval 5): 16, 21, 26, 31, 36, 41, 46, 51, … i.e. every level ending in 1 or 6 from 16.
