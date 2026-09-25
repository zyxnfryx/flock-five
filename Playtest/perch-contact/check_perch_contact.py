#!/usr/bin/env python3
"""Perch contact check: every bird's toes must meet bare bark at every exact seat.

Reads the seat table from WorldBuilder.cs (SeatXPx, SeatYMain, SeatYGift, SeatToeRowPx),
the toe-row table from BranchView.cs, and the real textures, then re-derives where each
bird's claws land on branch.png / branch_gift.png exactly as the game places them.

Fails (exit 1) if any foot floats (> FLOAT_TOL px above bark), is buried
(> BURY_TOL px into bark), the toe-row table disagrees with the sprite, or any
plant pixel (moss/flower) sits under the claws. Run after any change to branch art,
bird sprites, BirdScale, RestLift or the seat table:

    python3 Playtest/perch-contact/check_perch_contact.py
"""
import re, sys
from pathlib import Path
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SPR = ROOT / "Assets/_Project/Art/Resources/Sprites"
WB = (ROOT / "Assets/_Project/Scripts/App/WorldBuilder.cs").read_text()
BV = (ROOT / "Assets/_Project/Scripts/View/BranchView.cs").read_text()
FLOAT_TOL, BURY_TOL = 2.0, 16.0          # branch-sprite px
PPU_BRANCH, PPU_BIRD = 140.0, 220.0
COLORS = ["ruby", "gold", "teal", "violet", "peach"]   # BirdColor order

def arr(src, name):
    m = re.search(name + r"\s*=\s*\{([^}]*)\}", src)
    return [float(v.strip().rstrip("f")) for v in m.group(1).split(",")]
def const(src, name):
    return float(re.search(name + r"\s*=\s*([0-9.]+)f", src).group(1))

SEAT_X = arr(WB, "SeatXPx"); SEAT_Y = {"branch": arr(WB, "SeatYMain"), "branch_gift": arr(WB, "SeatYGift")}
TOE_REF = const(WB, "SeatToeRowPx")
WOOD = {"branch": (const(WB, "WoodScaleX"), 0.50), "branch_gift": (const(WB, "GiftWoodScaleX"), 0.52)}
REST_LIFT = const(BV, "RestLift")
BIRD_SCALE = float(re.search(r"BirdScale = new Vector3\(([0-9.]+)f", BV).group(1))

def plant(rgb):
    r, g, b = [rgb[..., i].astype(int) for i in range(3)]
    moss = (g > r + 4) & (g > b + 12)
    pink = (r > 185) & (b > g + 15) & (r - g > 25)      # orchid pink is bluer than the pinkish bark rim
    yellow = (r > 185) & (g > 160) & (b < g - 65) & (g > r - 40)   # saturated petal, not lit tan bark
    return moss | pink | yellow

def bark_top(a, x):
    """first row that is opaque bark (not plant) and stays opaque for 5 rows"""
    col = a[:, x]; P = plant(col[None, :, :3])[0]
    ok = (col[:, 3] > 200) & ~P
    for y in range(a.shape[0] - 5):
        if ok[y:y + 5].all():
            return y
    return None

LEG_X0, LEG_X1 = 380, 650   # sprite columns where the legs live

def feet(bird_alpha):
    al = bird_alpha > 100
    # legs only: ignore the long tail feather, which may hang below the perch
    al = al.copy(); al[:, :LEG_X0] = False; al[:, LEG_X1:] = False
    rows = np.where(al.sum(1) >= 2)[0]; ymax = rows.max()
    # split the two feet at the widest empty column run between them (x 460-550);
    # a median over all columns drifts when a long tail crosses the leg window
    occ = al[ymax - 102:ymax + 1].any(0)
    best, run, fx = -1, 0, 505
    for x in range(460, 551):
        run = run + 1 if not occ[x] else 0
        if run > best: best, fx = run, x - run // 2
    out = []
    for sl in (slice(0, fx), slice(fx, al.shape[1])):
        sub = al[:, sl]; ym = int(np.where(sub.sum(1) >= 2)[0].max())
        xs = np.where(sub[ym - 6:ym + 1].any(0))[0] + (sl.start or 0)
        out.append((ym, int(xs.min()), int(xs.max())))
    return int(ymax), out

fails, lines = [], []
birds = []
for ci, c in enumerate(COLORS):
    for tag in ("", "_f", "_m"):
        p = SPR / f"bird_{c}{tag}.png"
        if not p.exists():
            continue
        a = np.array(Image.open(p).convert("RGBA"))[..., 3]
        tip, ft = feet(a)
        # Every frame a SEATED bird can show must end its toes on the one shared
        # seat row. _1/_2 are the spread-wing flight frames: BirdIdle only shows
        # them when airborne (Frozen coroutine flight, Lift > 0.05, or Flapping
        # outside a seated Flutter); seated flutter/ruffle stays on the rest
        # frame. _3.._5 would be seated-flap extras and must still meet the row.
        FLIGHT_ONLY = {"_1", "_2"}
        for fr in [""] + [f"_{i}" for i in range(1, 6)]:
            if fr in FLIGHT_ONLY:
                continue
            q = SPR / f"bird_{c}{tag}{fr}.png"
            if q.exists():
                qa = np.array(Image.open(q).convert("RGBA"))[..., 3]
                qt = int(np.where(qa[:, LEG_X0:LEG_X1].max(1) > 40)[0].max())
                if abs(qt - TOE_REF) > 1:
                    fails.append(f"toe row: bird_{c}{tag}{fr} lowest toe row {qt}, must be {TOE_REF:.0f}")
        birds.append((f"{c}{tag}", tip, ft))

for name, (wsx, wsy) in WOOD.items():
    br = np.array(Image.open(SPR / f"{name}.png").convert("RGBA"))
    kx = BIRD_SCALE / PPU_BIRD / (wsx / PPU_BRANCH)
    ky = BIRD_SCALE / PPU_BIRD / (wsy / PPU_BRANCH)
    for s, sx in enumerate(SEAT_X):
        seat_y = SEAT_Y[name][s]
        for (bname, toe_row, ft) in birds:
            cy_world = seat_y + REST_LIFT               # bird centre, branch-local
            cy_px = 360.0 - cy_world * PPU_BRANCH / wsy        # in branch-sprite rows
            res = []
            for (ym, x0, x1) in ft:
                tip_px = cy_px + (ym - 512) * ky
                bx = np.arange(int(round(sx + (x0 - 512) * kx)), int(round(sx + (x1 - 512) * kx)) + 1)
                tops = [bark_top(br, int(x)) for x in bx]
                tops = [t for t in tops if t is not None]
                top = float(np.median(tops))
                ov = tip_px - top                               # + = into bark, - = floating
                y0, y1 = int(round(tip_px)) - 3, int(round(tip_px)) + 1
                under = br[max(0, y0):y1, bx[0]:bx[-1] + 1]
                opaque = under[..., 3] > 200
                npl = int((plant(under[..., :3]) & opaque).sum())
                res.append(f"{ov:+.1f}" + (f" plant{npl}" if npl else ""))
                if ov < -FLOAT_TOL: fails.append(f"{name} s{s} {bname}: foot floats {-ov:.1f}px")
                if ov > BURY_TOL: fails.append(f"{name} s{s} {bname}: foot buried {ov:.1f}px")
                if npl: fails.append(f"{name} s{s} {bname}: {npl} plant px under claws")
            lines.append(f"{name:12s} s{s} {bname:10s} L {res[0]:>14s}  R {res[1]:>14s}")

print("\n".join(lines))
if fails:
    print("\nFAIL (%d):" % len(fails)); print("\n".join("  " + f for f in fails)); sys.exit(1)
print("\nPASS: every bird's toes meet bare bark at every seat")
