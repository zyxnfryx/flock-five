# Birds sync v5 (slower halo, longer screen time), Flock Five studio card, build 83
Brandon 9:42 PM PT: "slow down the birds a bit and give them a bit more screen time." v4 kept as *-birds-v4.*. Audio and StudioSync.cs are unchanged. Card 3.000 s, pop 2.000 s, fade 2.7–3.0 s.

## Ring (unchanged from v4)
Center (540,812) px, A=470, B=225 (view elevation 28.6°), rotation -15°.
Point(θ): u=A cosθ, v=B sinθ; x=Cx+u·cos15°+v·sin15°; y=Cy−u·sin15°+v·cos15°. Depth d=sinθ (back/top <0 → under the logo, 30%·|d| lighter, 0.8·|d| px blur; front ≥0 → over the logo). Scale ×lerp(0.75,1.15,(d+1)/2).

## Motion (v5)
- Angular speed 2.75 rad/s (v4 3.93), about 30% slower. Each bird: θ(t) = 2.5π − 2.75·(t_breakout − t). No off-screen lead-in: the birds are already on the ring at frame 0, starting partway round (left side, θ≈π), with a 0.12 s staggered alpha fade-in from 0.00 s. They cover about 0.73 of a lap: left → over the top behind the Z → right → bottom front → focus point (598,1029).
- Follow-the-leader lag 0.04 s. The game's 6-pose wingbeat [3, 1, 4, 2, 4, 1] stays at 30 poses/s.
- Break-out from the focus point at 1.700–1.860 s (unchanged). Scatter on a quartic Bezier: P0=focus, P1=P0+v·0.12 s, then the controls below. Linear time over T≈1.1–1.2 s (v4 0.45–0.5 s). Scale grows ×1→1.3, front layer.
- Fan into the open lower and right canvas. Ruby now goes wide left and down-left, so nobody crosses the letters. Birds stay visible until 2.88–2.95 s, drifting off the edges while fading with the card (alpha ×(3.0−t)/0.3 after 2.7 s).

| Bird | Sprite | Base scale | θ at t=0 | Fade-in s | Break-out s (f60) | T s | Exit s | Bezier px |
|---|---|---|---|---|---|---|---|---|
| Ruby | bird_ruby | 0.080 | 3.179 rad (depth -0.04) | 0.00–0.12 | 1.700 (102) | 1.20 | 2.900 | (598,1029) → (448,1069) → (80,1150) → (140,1450) → (-70,1640) |
| Gold | bird_gold | 0.075 | 3.069 rad (depth +0.07) | 0.04–0.16 | 1.740 (104) | 1.20 | 2.940 | (598,1029) → (448,1069) → (260,1250) → (380,1600) → (360,1960) |
| Teal | bird_teal | 0.077 | 2.959 rad (depth +0.18) | 0.08–0.20 | 1.780 (107) | 1.18 | 2.960 | (598,1029) → (448,1069) → (520,1300) → (800,1600) → (880,1960) |
| Violet | bird_violet | 0.073 | 2.849 rad (depth +0.29) | 0.12–0.24 | 1.820 (109) | 1.15 | 2.970 | (598,1029) → (448,1069) → (680,1250) → (1000,1350) → (1150,1470) |
| Peach | bird_peach_f | 0.075 | 2.739 rad (depth +0.39) | 0.16–0.28 | 1.860 (112) | 1.12 | 2.980 | (598,1029) → (448,1069) → (720,1160) → (960,1150) → (1150,1190) |

Facing = sign(vx), crown/bow flips with the sprite; tilt = atan2(−vy,|vx|) clamped ±60°.

## Verification (60 fps per-frame, birds_check.py, 0–2.99 s)
- Front-layer birds on letters during the scatter: 0 px. Front-layer vs sparkle star (ray radius) before 2.0 s: min 73 px. Front-layer vs burst: min 182 px. Any bird vs burst at 1.95–2.30 s: min 350 px.
- Ring edge clipping: 0 frames.
- Flutter: gain ∝ (scale/0.08)^1.5, ×0.7 behind; pan follows x. Mix −15.29 LUFS, −1.5 dBTP.
