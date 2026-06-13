# Per-Fractal Guide Research — EXOTIC group B

Research + schema audit for five "exotic" fractals in Parsec Hyperdrive. Ground truth is the C# state files and GLSL/C# distance estimators (cited file:line); math background and community-known good values are web-researched with inline URLs. Items that could not be confirmed against a primary source are marked **unverified**.

Scope note: only FRACTAL-SPECIFIC parameters are audited. Shared Palette / Reflection / Light / Camera groups are intentionally omitted. Schema values below are read directly from each `BuildSchema()`.

---

## Riemann Sphere (`FractalType.RiemannSphere`)

**What it is:** A 3D escape-time fractal adapted from Mandelbulber's "Msltoe Riemann Sphere V1." Each iteration projects the orbit point onto a sphere, stereographically maps it to the complex plane, applies an `abs(sin())` fold to those plane coordinates (the organic, coral/cellular generator), runs a variable-exponent radial power map, then inverse-projects and adds c. The sine-fold offsets are the expressive, animatable knobs; zero offsets give the symmetric canonical form.

**How Parsec computes it:** `riemann_sphere_core.glsl:72-131`. Per iteration: project `z` onto a sphere of radius `scale` then stereographic-to-plane giving `(s,t)` (`riemann_sphere_core.glsl:97-102`); variable exponent `pe = min(1 + s^2 + t^2, pClamp)` (`:105`); sine-fold `s = abs(sin(PI*s + offA))`, `t = abs(sin(PI*t + offB))` (`:108-109`); radial map `|z| -> |z|^(2*pe) - 0.25` (`:112`); Mandelbulb-style running scalar derivative `dr = 2*pe*r^(2*pe-1)*dr + 1` (`:115`); inverse stereographic rebuild + c (`:119-121`); DE `0.5*log(r)*r/dr` (`:130`). The DE is APPROXIMATE (it ignores the sine-fold's angular derivative and the spatial variation of `pe`) — same approximation class that makes Mandelbulbs render (`:14-26`). Bailout is kept LOW on purpose: the exponent `2*pe` reaches ~72, so `r^(2*pe)` overflows a float past r~4 (`:28-30, 108-111`).

**Canonical formula / math background:** The Riemann sphere is the one-point compactification of the complex plane; stereographic projection maps a point on the unit sphere `(2s, 2t, s^2+t^2-1)/(1+s^2+t^2)` to/from the plane coordinate `s+it`. Plotting the Mandelbrot/escape-time field on the sphere instead of the plane is a known visualization (Christopher Olah, "Mandelbrot Set on the Riemann Sphere," https://christopherolah.wordpress.com/2011/03/05/mandelbrot-set-on-the-riemann-sphere/ ; Wikipedia "Riemann sphere," https://en.wikipedia.org/wiki/Riemann_sphere ). The specific 3D "Msltoe Riemann Sphere V1" formula is a Mandelbulber type by user MslToe; Mandelbulber ships four Riemann-sphere variants (https://mandelbulber.org/examples/riemann_sphere_msltoe_v1_001/ ; Mandelbulb wiki context https://en.wikipedia.org/wiki/Mandelbulb ). A published Mandelbulber example for this exact formula uses bailout 8 and DE_factor 0.3 (per the example page metadata; the full parameter table is JS-rendered and could not be fully scraped — **unverified** beyond bailout/DE factor). Parsec deliberately diverges on bailout (it uses ~2) for the float-overflow reason above, which is a sound and documented choice.

**Settings audit:**

| Setting | Default | Min..Max (Step) | Research-recommended | Default OK? | Range OK? | Note |
|---|---|---|---|---|---|---|
| Scale | 1.0 | 0.5..2.0 (—, 2dp) | ~1.0 canonical | Yes | Yes | Sphere-projection radius; small changes restructure a lot, so a narrow range is correct. |
| Fold offset A | 0.4 | -3.14..3.14 (2dp) | 0 = symmetric; sweep full ±π | Yes | Yes | Primary animation target; full ±π covers one period of the sine fold. |
| Fold offset B | 0.7 | -3.14..3.14 (2dp) | 0 = symmetric; sweep full ±π | Yes | Yes | Same; A≠B default breaks symmetry for a livelier default look. |
| Julia (0/1) | 0 | 0..1 (1) | 0 for the "set" view | Yes | Yes | Mode toggle. |
| Julia C x/y/z | 0,0,0 | -1.0..1.0 (2dp) | small values near 0 | Yes | Yes | Only meaningful when Julia=1. Set lives near r<=1 so a ±1 box is appropriate. |
| Rot X/Y/Z | 0 | -45..45 (deg, 0dp) | 0 default | Yes | Adequate | Cosmetic pre-rotation; full orientation freedom not needed. |
| Power clamp | 36 | 2..36 (1) | keep high (~36) | Yes | Yes | Caps `pe`; the docstring notes 2*pe~72 at the 36 ceiling is the overflow-safe max. Do not raise. |
| Iterations | 20 | 4..500 (1) | 12-40 typical | Yes | Yes | Detail vs cost. |
| Bailout | 2.0 | 1.2..3.0 (2dp) | keep ~2 (LOW by design) | Yes | Yes | Intentionally low to avoid 2p-exponent float overflow (`:28-30`). Range correctly capped at 3.0. |
| DE fudge | 0.6 | 0.3..2.0 (2dp) | 0.5-0.7 if faceted | Yes | Yes | Lower first if surface looks faceted (docstring guidance). |
| Bound radius | 3.0 | 1.0..8.0 (1dp) | ~3 | Yes | Yes | Fast-skip sphere; set lives near r<=1. |

**Discrepancies & recommendations:** None substantive. The schema is internally consistent and the unusual choices (low bailout cap, power-clamp ceiling at exactly 36) are deliberate and correctly bounded against the documented float-overflow failure mode. Optional polish: the Julia constant box (±1) is tighter than Mandalay/Anisotropic (±2); since this set lives near r<=1, ±1 is defensible, but if Julia exploration feels cramped, widening to ±1.5 is low-risk.

**For best results:**
- Animate Fold offset A and B (sweep ±π) for a morphing cellular/coral surface — that is what the formula is built to show off.
- Keep Bailout near 2 and Power clamp at 36; raising either invites overflow artifacts rather than more detail.
- If the surface looks faceted, lower DE fudge toward 0.4 before raising Iterations.

**Sources:** https://mandelbulber.org/examples/riemann_sphere_msltoe_v1_001/ , https://christopherolah.wordpress.com/2011/03/05/mandelbrot-set-on-the-riemann-sphere/ , https://en.wikipedia.org/wiki/Riemann_sphere , https://en.wikipedia.org/wiki/Mandelbulb

---

## Mandalay Fold (`FractalType.Mandalay`)

**What it is:** An escape-time fractal built on the "Mandalay" fold, a darkbeam transform from Fractal Forums (the Amazing-Box/Amazing-Surf family). The fold itself is not a fractal — it folds space into the positive octant, then runs a per-axis cascade of conditional coordinate swaps and min/max of offset planes (SDF CSG: union=min, intersection=max) that carves a cross/beam base shape. The escape-time scaffold `z = scale*fold(z) + c` (Mandelbox/KIFS-style) turns it into a fractal. Negative scale (~-2) gives the rich bounded set.

**How Parsec computes it:** `mandalay_core.glsl:66-114`. `mandalayAxis()` (`:54-64`) folds one axis: conditional swap of two off-axis coords, then `v = max(abs(t1+fo)-fo, t2)`, `v1 = max(t1-g, p[s2]-h)`, `v1 = max(v1, -abs(p[m]))`, `v = min(v, v1)`, return `min(v, p[m])` — pure abs/swap/min/max linear terms, nearly distance-preserving (measured mean Lipschitz ~0.98, max ~1.73 at fold seams, `:12-22`). Per iteration: take `sign(z)` and `abs(z)`, fold each of x/y/z via `mandalayAxis` (parallel mode feeds each from the original `a`; sequential mode feeds the running `q`, `:92-98`), reapply sign, then `z = scale*z + c` with `dr = abs(scale)*dr + 1` (`:100-104`). DE = `length(z)/dr` (`:113`). DE fudge defaults to 0.55 because the fold expands ~1.7x at seams (`:18-22`).

**Canonical formula / math background:** "Mandalay" is a named fractal type introduced by darkbeam on Fractal Forums, available in KIFS and non-KIFS versions in the Amazing-Box/Amazing-Surf variations subforum ("'New' fractal type; Mandalay," http://www.fractalforums.com/amazing-box-amazing-surf-and-variations/'new'-fractal-type-mandalay/ ). It belongs to the Mandelbox/conditional-fold family. The escape-time scaffold is the standard Mandelbox/KIFS recurrence `z -> scale*fold(z) + c`, whose analytic scalar DE is `|z|/dr` with `dr -> |scale|*dr + 1` (Mandelbox: Tom Lowe, 2010; Wikipedia "Mandelbox," https://en.wikipedia.org/wiki/Mandelbox ; standard fold parameters fold_limit=1, min_radius=0.5, fixed_radius=1, and negative scale = 180-degree rotation per the Mandelbox notes, https://sites.google.com/site/mandelbox/what-is-a-mandelbox ). For the Mandelbox family, scale ~-1.5 gives tight spiky detail and contains approximations of many other fractals, ~-2.0 the classic "boxed" look, ~-2.5 a more organic/porous opening (https://sites.google.com/site/mandelbox/negative-1-5-mandelbox ). The specific `fo`/`g`/`h` offset semantics of the Mandalay axis fold are Parsec/darkbeam-specific and not separately documented as canonical constants (**unverified** beyond the source forum thread).

**Settings audit:**

| Setting | Default | Min..Max (Step) | Research-recommended | Default OK? | Range OK? | Note |
|---|---|---|---|---|---|---|
| Scale | -2.0 | -3.0..3.0 (2dp) | negative ~-2 for the rich set; -1.5 to -2.5 sweet spot | Yes | Mostly — see note | Positive scales rarely give a bounded set for this fold; the positive half of the range is mostly dead. |
| Fold offset (fo) | 0.555 | 0.1..1.5 (3dp) | ~0.5 canonical; primary shape knob | Yes | Yes | The main shape control. |
| Offset g | 0.0 | -1.0..1.0 (2dp) | 0 canonical; opens beam/cross structure | Yes | Yes | Secondary; 0 = canonical look. |
| Offset h | 0.0 | -1.0..1.0 (2dp) | 0 canonical | Yes | Yes | Secondary. |
| Sequential (0/1) | 0 | 0..1 (1) | 0 (parallel) default | Yes | Yes | Sw mode toggle. |
| Julia (0/1) | 0 | 0..1 (1) | 0 for the set | Yes | Yes | Mode toggle. |
| Julia C x/y/z | 0,0,0 | -2.0..2.0 (2dp) | small values near 0 | Yes | Yes | Only meaningful when Julia=1. |
| Iterations | 12 | 4..500 (1) | 8-20 typical | Yes | Yes | Detail vs cost. |
| Bailout | 8.0 | 2..20 (1dp) | ~6-10 | Yes | Yes | Standard fold-family escape radius. |
| DE fudge | 0.55 | 0.2..2.0 (2dp) | ~0.55; lower if sparkle on folds | Yes | Yes | Fold expands ~1.7x at seams, so <1 is correct. |
| Bound radius | 6.0 | 2.0..12.0 (1dp) | ~6 | Yes | Yes | Fast-skip sphere. |

**Discrepancies & recommendations:**
- **Scale range half-wasted.** Min..Max is -3.0..3.0 but the docstring and Mandelbox-family behavior say the rich bounded set lives at negative scale (~-2). Positive scales generally fail to produce an interesting bounded object for this fold. Recommend documenting this in the per-fractal guide (the negative half is the working region; sweet spot -1.5 to -2.5), or optionally re-centering the slider toward negatives. Not a bug, but the default panel invites users into a dead zone.
- Fold offset default 0.555 is a fine non-symmetric starting point; for a "cleaner" canonical cross, ~0.5 with g=h=0 is the reference.

**For best results:**
- Keep Scale negative (-1.5 to -2.5). -2.0 for the classic boxed look, toward -2.5 for organic/porous, toward -1.5 for tight spiky detail.
- Sweep Fold offset (fo) as the primary shape morph; nudge g and h off zero to open the beam/cross structure.
- If you see sparkle or dropout along the folds, lower DE fudge before raising Iterations.

**Sources:** http://www.fractalforums.com/amazing-box-amazing-surf-and-variations/'new'-fractal-type-mandalay/ , https://en.wikipedia.org/wiki/Mandelbox , https://sites.google.com/site/mandelbox/what-is-a-mandelbox , https://sites.google.com/site/mandelbox/negative-1-5-mandelbox

---

## Anisotropic Fold (`FractalType.Anisotropic`)

**What it is:** An escape-time box-fold fractal under an ANISOTROPIC (non-uniform, sheared) linear step, and Parsec's first delta-DE chapter. The linear map stretches space by different amounts along different (sheared) axes, so a scalar running derivative is wrong — it overestimates distance along the most-stretched direction and tears holes in the surface. Parsec instead estimates distance numerically via a finite-difference Jacobian. The stretch and shear knobs are the whole reason this fractal needs delta-DE.

**How Parsec computes it:** `anisotropic_core.glsl:97-138`. The map is `z = M*boxFold(z, L) + c` where `boxFold(z,L) = clamp(z,-L,L)*2 - z` (`:71`) and `M = Rz(shearZ)*Ry(shearY)*diag(scale*rx, scale*ry, scale*rz)` (`buildM`, `:57-69`). DE is delta-DE: run the base orbit to find escape iteration N (`:104-118`), run three more orbits from `p + eps` along each axis for exactly N iterations (`runFixed`, `:75-82`, `:120-123`), finite-difference into the 3x3 Jacobian `J = dz/dp` (`:125-128`), then `DE = |z_base| / ||J||` (`:137`). `||J||` is selectable: Frobenius (mode 0, `>= sigma_max` so the DE only ever under-shoots — guaranteed hole-free but conservative) or largest singular value via power iteration on `J^T J` (mode 1, tight/crisper but can sparkle, `sigmaMax` `:85-95`). Cost ~4x a scalar core; validated against the exact matrix-DE to ~1e-10 (`:27-28`). DE fudge ~0.9 absorbs finite-difference noise at seams (`:18`).

**Canonical formula / math background:** The base map is the standard Mandelbox box-fold `clamp(z,-1,1)*2 - z` with no sphere fold, under a general linear transform instead of a scalar `scale` (Mandelbox: https://en.wikipedia.org/wiki/Mandelbox ; standard fold_limit=1). The anisotropy (per-axis stretch + shear) makes `M` a non-similarity transform; the numerical-Jacobian / delta-DE technique (run the orbit at `p` and `p + eps`, finite-difference) is the general distance estimator used when no analytic DE exists, described by Mikael Hvidtfeldt Christensen ("Distance Estimated 3D Fractals," http://blog.hvidtfeldts.net/index.php/2011/09/distance-estimated-3d-fractals-v-the-mandelbulb-different-de-approximations/ ). Using the Frobenius norm as a safe over-estimate of the operator norm `sigma_max` is a standard linear-algebra bound (`||J||_F >= sigma_max`). The specific anisotropic-fold parametrization is Parsec-original (the chapter's named purpose), so "community canonical values" do not exist for stretch/shear — **unverified** as canonical; the audited recommendation is about the DE behavior, not a published reference image.

**Settings audit:**

| Setting | Default | Min..Max (Step) | Research-recommended | Default OK? | Range OK? | Note |
|---|---|---|---|---|---|---|
| Scale | 2.0 | -3.0..3.0 (2dp) | ~2 (positive) works here; box-fold tolerates positive scale | Yes | Yes | Box-fold (no sphere fold) is stable at positive scale, unlike Mandalay. |
| Fold limit | 1.0 | 0.3..2.0 (2dp) | 1.0 canonical | Yes | Yes | The Mandelbox standard fold limit. |
| Stretch X | 1.2 | 0.3..2.0 (2dp) | unequal X/Y/Z = the anisotropy; equal collapses to scalar | Yes | Yes | Default 1.2/1.0/0.8 is a good, clearly-anisotropic starting triple. |
| Stretch Y | 1.0 | 0.3..2.0 (2dp) | — | Yes | Yes | — |
| Stretch Z | 0.8 | 0.3..2.0 (2dp) | — | Yes | Yes | — |
| Shear Z | 0.5 rad (~29deg) | -90..90 (deg, 0dp) | nonzero = the "lean" | Yes | Yes | Stored in radians, shown in degrees. Default ~29deg gives visible lean. |
| Shear Y | 0.0 | -90..90 (deg, 0dp) | 0 default | Yes | Yes | — |
| Norm Frob/sigma (0/1) | 0 | 0..1 (1) | 0 (Frobenius, safe) default; 1 for crisper | Yes | Yes | Frobenius is hole-free; sigma_max is faster/crisper but can sparkle. |
| Julia (0/1) | 0 | 0..1 (1) | 0 | Yes | Yes | — |
| Julia C x/y/z | 0,0,0 | -2.0..2.0 (2dp) | small near 0 | Yes | Yes | Only meaningful when Julia=1. |
| Iterations | 10 | 4..500 (1) | 8-16 (each iter is ~4x cost) | Yes | Yes | Note the 4-orbit cost; keep modest. |
| Bailout | 12.0 | 4..30 (1dp) | ~8-15 | Yes | Yes | — |
| DE fudge | 0.9 | 0.2..2.0 (2dp) | ~0.9 absorbs FD noise | Yes | Yes | — |
| Bound radius | 5.0 | 2.0..10.0 (1dp) | ~5 | Yes | Yes | — |

**Discrepancies & recommendations:** None substantive. The defaults form a deliberately anisotropic, sheared configuration (unequal stretch + ~29deg shear) that showcases the delta-DE — exactly what this chapter is meant to demonstrate. The Frobenius-default / sigma_max-option split is sound (safe by default, crisp on request). Minor guidance for the guide text: equal stretch values (1,1,1) plus zero shear collapse this to an ordinary scalar-DE-able fold, which makes the delta-DE pointless — that is the "boring" configuration to warn against, not a bug.

**For best results:**
- Keep the stretch values unequal and at least one shear nonzero; that is where the anisotropy (and the reason for delta-DE) shows.
- Leave Norm on Frobenius (0) for guaranteed hole-free renders; switch to sigma_max (1) only when you want crisper edges and can tolerate occasional seam sparkle.
- Keep Iterations modest (~10) since each iteration runs four orbits.

**Sources:** https://en.wikipedia.org/wiki/Mandelbox , http://blog.hvidtfeldts.net/index.php/2011/09/distance-estimated-3d-fractals-v-the-mandelbulb-different-de-approximations/

---

## Hybrid (box + bulb) (`FractalType.Hybrid`)

**What it is:** A hybrid escape-time fractal that runs BOTH a Mandelbox (rotated box fold + sphere fold + scale) and a Mandelbulb (spherical power) step in sequence every iteration. The small per-iteration rotation compounds across iterations (the "morph" knobs). The DE is a heuristic combination of the two component DEs (neither combination has a proven lower-bound property), with a 0.5 safety factor to keep the marcher conservative — some parameter regions will show artifacts, which is expected for hybrids.

**How Parsec computes it:** `hybrid_core.glsl:59-116` (GPU) mirrored by `HybridDE.cs:17-53` (CPU, fly-camera speed only). Per iteration: rotate `z = R*z` (compounding, `:78`); Mandelbox half — box fold `clamp(z,-foldLim,foldLim)*2 - z` (`:81`), sphere fold using `minRadius`/`fixedRadius` (`:82-87`), `z = z*scale + p`, `dr = dr*abs(scale) + 1` (`:88-89`); Mandelbulb half — spherical power `z -> z^power + p` with `dr = power*r^(power-1)*dr + 1` (`:91-103`); bailout 8 (`:67`). Final DE is bulb-style with a doubled safety factor: `0.5 * 0.5 * log(r)*r/dr` (`:115`). Note: the CPU mirror uses a single `0.25*` factor (`HybridDE.cs:52`) which equals the GPU `0.5*0.5`, so they match. Every knob is a smooth scalar, making this "a phenomenal animation target" (`HybridState.cs:11`).

**Canonical formula / math background:** This is a literal composition of two well-known DE fractals. Mandelbox: Tom Lowe (2010), box fold + sphere fold + scale, with standard fold_limit=1, min_radius=0.5, fixed_radius=1, and scale near -1.5 to -2 for the interesting bounded sets (https://en.wikipedia.org/wiki/Mandelbox ; https://sites.google.com/site/mandelbox/what-is-a-mandelbox ; scale -1.5 contains approximations of many fractals, https://sites.google.com/site/mandelbox/negative-1-5-mandelbox ). Mandelbulb: White and Nylander (2009), the spherical "nth power" `z -> z^n` scaling radius by `r^n` and multiplying angles by n; power 8 is the iconic value, power 2 is the simpler IQ-bulb (https://en.wikipedia.org/wiki/Mandelbulb ; http://blog.hvidtfeldts.net/index.php/2011/09/distance-estimated-3d-fractals-v-the-mandelbulb-different-de-approximations/ ). Hybridizing a box fold with a bulb power per iteration is a standard Mandelbulb-3D / Mandelbulber practice (DarkBeam-style hybrid formulas, https://www.deviantart.com/mandelbulb3d/journal/New-M3D-Formulas-from-dark-beam-403364013 ). The specific 0.5 safety factor and the per-iteration order (rotate -> box -> bulb) are Parsec's heuristic choice; there is no canonical DE for this exact combination (**unverified** as a published formula — it is a documented heuristic).

**Settings audit:**

| Setting | Default | Min..Max (Step) | Research-recommended | Default OK? | Range OK? | Note |
|---|---|---|---|---|---|---|
| Power (bulb half) | 2.0 | 1.5..8.0 (2dp) | 2 = IQ-bulb; 8 = iconic bulb; try 8 | Default is fine, but conservative | Yes | Power 8 is the famous Mandelbulb look; 2 is the simplest. Range covers the useful span. |
| Rotate X | 6.9deg | -30..30 (2dp) | small nonzero = morph; 0 = static | Yes | Yes | Compounds per iteration; small angles are correct. |
| Rotate Y | 4.6deg | -30..30 (2dp) | — | Yes | Yes | — |
| Rotate Z | 2.3deg | -30..30 (2dp) | — | Yes | Yes | Default ~7/4.6/2.3 deg gives a lively non-axis-aligned morph. |
| Scale (box half) | -1.8 | -2.5..-1.2 (2dp) | -1.5 to -2.0 sweet spot | Yes | Yes | Range correctly restricted to the negative Mandelbox regime. |
| Min radius | 0.5 | 0.1..1.0 (2dp) | 0.5 canonical | Yes | Yes | Mandelbox standard. |
| Fixed radius | 1.0 | 0.5..2.0 (2dp) | 1.0 canonical | Yes | Yes | Mandelbox standard. |
| Fold limit | 1.0 | 0.5..2.0 (2dp) | 1.0 canonical | Yes | Yes | Mandelbox standard. |
| Iterations | 8 | 4..500 (1) | 6-12 typical | Yes | Yes | Hybrid DE gets noisy with very high iters in some regimes. |
| DE fudge | 0.6 | 0.3..2.0 (2dp) | 0.4-0.7 if artifacts | Yes | Yes | Hybrid DE over-estimates; keep below 1 in tricky regimes. |

**Discrepancies & recommendations:**
- **Bailout and Bound radius are hardcoded, not exposed.** Bailout is fixed at 8.0 (`hybrid_core.glsl:67`) and BoundRadius at 4.0 (`HybridState.cs:41`). This is fine for a hybrid (stable defaults), but worth noting in the guide so users do not look for a missing Bailout slider that the other four fractals have.
- **Power default is the conservative choice.** Default 2.0 renders a relatively plain bulb half. For the showcase "wow" image, the per-fractal guide should point users to Power ~8 (the iconic Mandelbulb lobing) — the range already supports it. Not a bug, a discoverability note.
- The CPU/GPU DE constants agree (`0.25` == `0.5*0.5`), so fly-camera and render are consistent — no discrepancy there.

**For best results:**
- Set Power to 8 for the classic many-lobed Mandelbulb look fused with the box detail; drop toward 2 for a smoother, blobbier hybrid.
- Keep Scale in -1.5 to -2.0 and the box radii at the canonical 0.5 / 1.0 / 1.0.
- Animate the three Rotate knobs (small angles) for a continuous morph — that is this fractal's standout feature.
- If holes or noise appear, lower DE fudge toward 0.4 and reduce Iterations rather than increasing them.

**Sources:** https://en.wikipedia.org/wiki/Mandelbox , https://en.wikipedia.org/wiki/Mandelbulb , https://sites.google.com/site/mandelbox/what-is-a-mandelbox , https://sites.google.com/site/mandelbox/negative-1-5-mandelbox , http://blog.hvidtfeldts.net/index.php/2011/09/distance-estimated-3d-fractals-v-the-mandelbulb-different-de-approximations/ , https://www.deviantart.com/mandelbulb3d/journal/New-M3D-Formulas-from-dark-beam-403364013

---

## Thomas Attractor (`FractalType.Attractor`)

**What it is:** The Thomas cyclically symmetric strange attractor — a 3D chaotic ODE viewed as the trajectory of a frictionally damped particle in a 3D lattice of sinusoidal forces. Unlike the other four (closed-form distance fields), this is a TRAJECTORY: Parsec integrates the ODE into hundreds of thousands of points, builds a spatial hash, and renders the polyline as a glowing tube. Because that integration is expensive, the parameters split into GENERATION params (take effect on the "Generate" action) and LIVE params (tube look, update immediately). Animation is disabled for this fractal; the generation params are not live-animatable. The default is canonical Thomas (all perturbations off) — the clean logo.

**How Parsec computes it:** ODE in `ThomasAttractor.cs:118-152` — canonical derivative `(sin(y) - b*x, sin(z) - b*y, sin(x) - b*z)` when all perturbation flags are off (`:139-142`), integrated by RK4 (`Rk4Step` `:105-112`) at timestep `Dt` for `NumSteps` steps, after a 1000-step burn-in to skip the transient (`BurnIn` `:88-93`). Four optional perturbation channels (Meadow's recipe, ported from a Unity SDF): parameter drift (per-axis b driven by `sin(driftPhase + k)`, `:121-126`), phase modulation (position-driven amplitude, `:129-135`), seed phase shift (multi-seed orbits, `:137`, `:59-73`), and nonlinear coupling (quadratic cross terms, `:144-149`). The DE is "distance to nearest trajectory segment minus tube radius," accelerated by a uniform spatial hash that only tests segments in the 3x3x3 cell neighborhood (`attractor_core.glsl:74-139`), with a safety clamp to half the neighborhood radius (`:135-138`).

**Canonical formula / math background:** Thomas' cyclically symmetric attractor, René Thomas: `dx/dt = sin(y) - b*x`, `dy/dt = sin(z) - b*y`, `dz/dt = sin(x) - b*z`, cyclic in x->y->z->x (Wikipedia "Thomas' cyclically symmetric attractor," https://en.wikipedia.org/wiki/Thomas%27_cyclically_symmetric_attractor ). `b` is the dissipation / bifurcation parameter. Bifurcation cascade (Wikipedia, verified):

- `b > 1`: origin is the sole stable equilibrium.
- `b = 1`: pitchfork bifurcation, two attractive fixed points appear.
- `b ≈ 0.32899`: Hopf bifurcation, a stable LIMIT CYCLE forms (periodic, not chaotic).
- `b ≈ 0.208186`: period-doubling cascade reaches CHAOS ONSET.
- `b < 0.208186`: chaotic attractor (may split into multiple coexisting attractors); fractal dimension rises toward 3 as b decreases.
- `b = 0`: dissipation lost; trajectory ergodically wanders all space ("Labyrinth Chaos," deterministic fractional Brownian motion).

So the chaotic regime is roughly `0 < b < 0.208186`; for `0.208186 < b < 0.329`, the system is a (period-doubled) limit cycle, structured but not yet fully chaotic; `b ≈ 0.19` sits just inside the chaotic regime and is a community-cited classic value (https://en.wikipedia.org/wiki/Thomas%27_cyclically_symmetric_attractor ; HandWiki mirror https://handwiki.org/wiki/Thomas'_cyclically_symmetric_attractor ; Medium overview https://medium.com/@rh.h.rad/thomas-attractor-exploring-the-beauty-of-chaotic-dynamics-169208a7dcab ). RK4 is comfortably stable at `dt = 0.05` for this smooth system (general RK4 stability, https://www.physicsforums.com/threads/rk4-time-step-question.761787/ — **unverified** as a Thomas-specific published step size, but standard practice).

**Settings audit:** (Generation params take effect on Generate; Live params update immediately. `Dt = 0.05` exists in state but is NOT exposed in any schema.)

| Setting | Default | Min..Max (Step) | Research-recommended | Default OK? | Range OK? | Note |
|---|---|---|---|---|---|---|
| Damping b (Generate) | 0.208186 | 0.05..0.35 (3dp) | chaotic look: ~0.10-0.20; classic 0.19 | Borderline — see below | Mostly, but top end leaks past chaos | Default sits EXACTLY at the chaos-onset threshold; 0.05..0.35 includes the non-chaotic limit-cycle band 0.208-0.329. |
| Steps x1000 (Generate) | 200 (=200k) | 20..400 (10) | 100-300k for a full attractor | Yes | Yes | Trajectory length / tube density. |
| Param variation (Generate) | 0.0 | 0..0.3 (3dp) | 0 = canonical; small for drift | Yes | Yes | Perturbation strength, zeros out cleanly. |
| Phase mod (Generate) | 0.0 | 0..1.0 (2dp) | 0 = canonical | Yes | Yes | Perturbation. |
| Coupling (Generate) | 0.0 | 0..0.05 (3dp) | 0 = canonical; tiny values | Yes | Yes | Quadratic cross-term strength. |
| Drift phase (Generate) | 0.0 | 0..6.283 (2dp) | full 0..2π = "explore" knob | Yes | Yes | Reproducible replacement for the old Time.time randomizer. |
| Multi-seed (0/1) (Generate) | 0 | 0..1 (1) | 0 = single orbit | Yes | Yes | Structural toggle. |
| Seed count (Generate) | 5 | 1..10 (1) | 3-8 | Yes | Yes | Only used when multi-seed=1. |
| Tube radius (Live) | 0.06 | 0.01..0.2 (3dp) | 0.04-0.08 | Yes | Yes | Live; no regenerate. |
| DE fudge (Live) | 0.45 | 0.2..0.8 (2dp) | ~0.45 | Yes | Yes | Live; tube marcher safety. |

**Discrepancies & recommendations:**
- **`b` default sits exactly at the chaos-onset boundary (0.208186), and the slider range leaks into the non-chaotic regime.** This is the one substantive finding. Per the verified bifurcation table, true chaos requires `b < 0.208186`; the band `0.208186 < b < 0.32899` is a period-doubled limit cycle (structured loops, not the full strange attractor), and above ~0.329 it collapses toward a fixed point. The current default (0.208186) is therefore right at the edge — it renders the attractor but is the least-chaotic value that still does, and any upward nudge (the slider goes to 0.35) drops the user OUT of chaos into a limit cycle or toward collapse. Recommendations: (a) set the default to a solidly chaotic value such as **0.19** (the community-classic) or ~0.18 so the out-of-box image is unambiguously the strange attractor; (b) optionally cap the slider Max near ~0.30 (or document that >0.21 is the non-chaotic limit-cycle band) so users do not wander into the dead zone expecting more chaos. The low end (0.05) is good — more chaos / higher fractal dimension. Note: the code comment at `AttractorState.cs:24`/`ThomasAttractor.cs:23` frames 0.208186 as "canonical," which matches its status as the named chaos-onset constant, but for VIEWING, a value just below it is better.
- **`Dt` (0.05) is not exposed in any schema** (`AttractorState.cs:24-26`, set but absent from `BuildGenerateSchema`). Intentional (RK4 at 0.05 is stable and step size is not an artistic knob), worth a one-line note in the guide rather than a change.
- Perturbation defaults all zero out to canonical Thomas — correct and clean.

**For best results:**
- For the unmistakable strange-attractor look, set Damping b to ~0.19 (or anywhere 0.10-0.20) before Generate; stay below 0.208 to remain chaotic. Lower b (toward 0.10) thickens and fills the attractor (higher fractal dimension).
- Use 150-300k steps for a dense, continuous tube; thin tubes (radius ~0.04) read the structure better than fat ones.
- Treat Drift phase as the "explore the family" knob: sweep 0..2π and Generate to find new but reproducible variants.
- Keep perturbations at 0 for the canonical logo; introduce them in small amounts (param variation < 0.1, coupling < 0.02) for organic deformation without losing the shape.

**Sources:** https://en.wikipedia.org/wiki/Thomas%27_cyclically_symmetric_attractor , https://handwiki.org/wiki/Thomas'_cyclically_symmetric_attractor , https://medium.com/@rh.h.rad/thomas-attractor-exploring-the-beauty-of-chaotic-dynamics-169208a7dcab , https://www.physicsforums.com/threads/rk4-time-step-question.761787/

---

## Cross-cutting notes

- **Hybrid is the only fractal with no Bailout/Bound-radius sliders** (both hardcoded: bailout 8 in `hybrid_core.glsl:67`, bound 4.0 in `HybridState.cs:41`). The other four expose Bailout and Bound radius.
- **Thomas is the only one with a Generate/Live split** and the only one where a state field (`Dt`) is intentionally not exposed.
- **Negative-scale convention** matters for the Mandelbox-family members: Mandalay's interesting set lives at negative scale (its +scale half is largely dead), and Hybrid correctly restricts Scale to -2.5..-1.2; Anisotropic uses a box-fold-only map that is stable at positive scale (default +2.0), so its -3..3 range is genuinely two-sided there.
- Only one default actually warrants change for "best out-of-box view": **Thomas Damping b** (move from the 0.208186 chaos boundary down to ~0.19). Every other default is a defensible viewing value.
