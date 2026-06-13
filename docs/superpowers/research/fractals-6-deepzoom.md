# Per-Fractal Guide Research: 3D Burning Ship + Deep Zoom 2D

Research + schema audit for the Parsec Hyperdrive "per-fractal guide" feature.
Ground truth is the source code (cited file:line); math background is web-researched
(inline URLs). No source code was modified.

Covers 5 sections: the 3D Burning Ship, and the DeepZoom mode with its four
selectable 2D formulas (Mandelbrot, Prospector, Julia, Burning Ship 2D).

---

## 3D Burning Ship (`FractalType.BurningShip`)

**What it is:** A 3D distance-estimated escape-time fractal that takes the same
triplex `z -> z^n + c` power map as the Mandelbulb but applies an `abs()` fold to
every component after adding `c`, in a y-up angular convention (polar angle from
+Y). The `abs()` folds break the smooth Mandelbulb shell into terraced,
vertically mirror-symmetric, windblown "burning ship" massing. Its characteristic
swept laminar sheets live at LOW power (~2-3); at high power the radial `r^n`
growth dominates and the form degenerates toward a flat-bottomed Mandelbulb.

**How Parsec computes it:** Scalar-derivative escape-time DE, raymarched. Per
iteration (`burningship_core.glsl:63-93`): convert `z` to spherical coords y-up
(`theta = atan2(sqrt(x^2+z^2), y)`, `zangle = atan2(x, z)`, lines 68-70), advance
the running derivative in log space (`ldr`, lines 75-76 — `abs()` is an isometry so
it does not change the derivative magnitude), raise to the power (`zr = pow(r,
power)`, multiply both angles by `power`, rebuild Cartesian, add `c = p`, lines
80-85), then apply the burning-ship fold `z = abs(z)` (line 86). Escape when
`r > bailout` (line 65). Final DE = `0.5*log(r)*r*exp(-ldr)` (line 96). State and
schema defaults live in `BurningShipState.cs:17-48`. Note the DE is NOT a strict
lower bound near the spherical-coordinate poles (same intrinsic Mandelbulb
limitation), so the default DE fudge is below 1.0 to suppress crease tearing
(`BurningShipState.cs:13`, `burningship_core.glsl:24-29`).

**Canonical formula / math background:** The 2D Burning Ship is canonical
(Michelitsch & Rössler, 1992): `z_{n+1} = (|Re(z_n)| + i|Im(z_n)|)^2 + c`, escape
radius 2 ([Wikipedia](https://en.wikipedia.org/wiki/Burning_Ship_fractal)). The
**3D** version is NOT a standard published formula. It is a community/custom
triplex extension analogous to the Mandelbulb's spherical-coordinate power map
([Mandelbulb spherical-coordinate construction, Syntopia / Hvidtfeldt](http://blog.hvidtfeldts.net/index.php/2011/08/distance-estimated-3d-fractals-iii-folding-space/)),
with a per-component `abs()` fold added. The Parsec source itself calls it "the
posted reference formula" (`burningship_core.glsl:11`), i.e. a forum/community
recipe, not a textbook object. There is no single canonical power, escape radius,
or DE for a 3D Burning Ship; the choices here (y-up convention, scalar log-space
DE) are this implementation's.

**Settings audit:**

| Setting | Default | Min..Max (Step) | Research-recommended | Default OK? | Range OK? | Note |
|---|---|---|---|---|---|---|
| Power | 2.0 | 1.4..12 (0.01, 2 dec) | 2-3 for the signature swept sheets | Yes | Yes | Default at the low-power sweet spot; high end is reachable but degenerates to a Mandelbulb look |
| Iterations | 16 | 4..500 (1) | 8-32 typical; raise on close zoom | Marginal | Yes | 16 is fine for an overview but low for surface detail at depth; not the usual 100s because escape-time DE on this object converges fast |
| Bailout | 2.0 | 1.5..4.0 (0.01) | 2 is canonical for the squared map | Yes | Yes | Matches the 2D `|z|>2` escape; larger smooths coloring slightly |
| DE fudge | 0.75 | 0.4..2.0 (0.01) | <1.0 to suppress pole creases | Yes | Yes | Deliberately below 1.0; lower if creases tear, never needs >~1.2 |

**Discrepancies & recommendations:**
- None blocking. The schema is internally consistent and the in-code rationale is
  sound. The only thing a guide should flag is that **Power is the headline knob
  and lives LOW (2-3)**, the opposite of the Mandelbulb where 8 is the money
  value. Surface the power tip prominently.
- Iterations default 16 is conservative; consider mentioning that close-up shots
  benefit from 32-64. Not a schema bug.

**For best results:**
- Keep Power at 2.0-3.0 for the windblown laminar sheets; treat Power as a smooth
  animation target (it is a continuous scalar).
- If surface creases tear near the poles, lower DE fudge toward 0.5 before raising
  iterations.
- Raise Iterations to 32-64 for tight zooms; the default 16 is an overview value.

**Sources:**
- https://en.wikipedia.org/wiki/Burning_Ship_fractal
- http://blog.hvidtfeldts.net/index.php/2011/08/distance-estimated-3d-fractals-iii-folding-space/
- http://blog.hvidtfeldts.net/index.php/2011/08/distance-estimated-3d-fractals-ii-lighting-and-coloring/
- https://icefractal.com/articles/secrets-of-the-burning-ship/

---

## Deep Zoom 2D (`FractalType.DeepZoom`)

**What it is:** A 2D escape-time mode that replaces the 3D camera with a
high-precision pan/zoom "camera" (arbitrary-precision center as decimal strings, a
double half-height `Radius` as the zoom level — `DeepZoomView.cs:13-23`). It
renders one of FOUR selectable formulas chosen by the FORMULA dropdown
(`MainWindow.axaml:53`, index 0 Mandelbrot, 1 Prospector, 2 Julia, 3 Burning
Ship — `MainWindow.axaml.cs:60`, `FractalView.cs:354-365`). Pan and zoom are
mouse-driven, not sliders (`DeepZoomView.cs:93-129`). It supports zoom depths to
~1e-147 via perturbation theory.

**How Parsec computes it — the perturbation pipeline:** A single high-precision
**reference orbit** is iterated once per view in binary fixed-point
(`ReferenceOrbit.cs:42-140`); each `Z_n` is cast to a `double` for the GPU. Every
pixel then iterates a low-precision **delta** `dz = z - Zref` against that
reference, which keeps depth in the delta's exponent range rather than in the
reference mantissa. **Rebasing** resets the reference index to 0 (carrying a
`-Zref[0]` correction so it stays correct for Julia, whose reference starts at the
seed, not 0) whenever the pixel orbit gets smaller than its delta or runs off the
end of the reference (`deepzoom_delta.glsl:157-162`,
`deepzoom_delta_fe.glsl:125-131`). This is the standard "rebasing avoids glitches"
technique (Zhuoran / mathr). Iteration cap scales with zoom depth
(`DeepZoomView.cs:59-63`, `1000 + 1000*decimal-e-foldings`), and required
fixed-point precision scales with depth (`DeepZoomView.cs:66-70`,
`ReferenceOrbit.cs:152-153`).

**Three render paths by depth** (`DeepZoomPipeline.cs:142-144`,
`DeepZoomView.cs:78-90`):
1. **Direct fp64** — `Radius > 1e-6` (`DirectRadius`). Each pixel iterates its own
   orbit in plain `double` from `center + offset` (`deepzoom_delta.glsl:89-140`).
   Exact at shallow zoom and the ONLY reliable path for the Burning Ship at wide
   views (perturbation on the `abs` map is unstable when the delta is large).
2. **fp64 perturbation** — `1e-148 <= Radius <= 1e-6`. Delta + rebasing in
   `double` (`deepzoom_delta.glsl:142-196`).
3. **floatexp perturbation** — `Radius < 1e-148` (`FloatExpRadius`,
   `DeepZoomPipeline.cs:76`), down to `MinRadius = 1e-147` (`DeepZoomView.cs:78`).
   The delta is carried as a floatexp (double mantissa + int exponent) to survive
   past the ~1.5e-154 fp64 `dz^2` underflow wall (`deepzoom_delta_fe.glsl`). 3-5x
   slower per iteration, so it is the deep-only fallback.

The whole pipeline was validated against an mpmath oracle (Julia exact to 1e-13 at
all depths; Burning Ship diffabs exact; fixtures in `ReferenceOrbit.cs:156-187`).

**Formulas (the four selectable maps):**

- **0 Mandelbrot** — `z' = z^2 + c`, seed 0, parameter plane. Canonical complex
  quadratic. Reference recurrence `ReferenceOrbit.cs:119-136`; direct
  `deepzoom_delta.glsl:127-137`; perturbation `deepzoom_delta.glsl:164-172`
  (`dz' = 2*Zr*dz + dz^2 + dc`). Escape `|z|^2 > 4`
  (`DeepZoomPipeline.cs:270`). Home center `(-0.5, 0)`, radius 1.5
  (`DeepZoomView.cs:49`). ([Wikipedia](https://en.wikipedia.org/wiki/Mandelbrot_set))

- **1 Prospector** — custom real 2D quadratic map, seed 0, parameter plane:
  `X' = Cx + 0.25*X*Y`, `Y' = Cy - 3*X^2 + 0.25*Y^2`
  (`deepzoom_delta.glsl:104-113`, `ReferenceOrbit.cs:55-73`). **This is NOT a
  recognized/standard named fractal** — web search found no published "Prospector"
  fractal with this formula; it is a niche/custom map (likely a fold-prospector
  discovery, cf. the "fold prospector" referenced in
  `GpuTriballRenderer.cs:11`). Its bounded orbits reach `|z|^2 ~ 53`, so it uses a
  much larger escape radius `escapeR2 = 1e6` (`DeepZoomPipeline.cs:268-270`,
  `ReferenceOrbit.cs:61`). Home center `(0, 0)`, radius 2.5
  (`DeepZoomView.cs:46`). Guide should describe only what the code does and label
  it custom.

- **2 Julia** — `z' = z^2 + kappa`, DYNAMICAL plane: kappa is fixed, the pixel is
  the seed, and the reference orbit is the seed (view-center) orbit so
  `Z[0] = center`, not 0 (`ReferenceOrbit.cs:75-98`, `:21-24`). Direct
  `deepzoom_delta.glsl:92-102`; perturbation shares the complex-`z^2` branch with
  Mandelbrot but enters the offset as the initial delta rather than per step
  (`deepzoom_delta.glsl:145-146, 164-172`). Escape `|z|^2 > 4`. Home center
  `(0, 0)`, radius 1.5 (`DeepZoomView.cs:48`). kappa is the only slider-exposed
  parameter (see audit below); it is keyframeable, so sweeping it morphs the set
  for animations. ([Wikipedia: Julia set](https://en.wikipedia.org/wiki/Julia_set))

- **3 Burning Ship (2D)** — `X' = X^2 - Y^2 + Cx`, `Y' = 2|X*Y| + Cy`, seed 0,
  parameter plane (`ReferenceOrbit.cs:100-117`, direct `deepzoom_delta.glsl:115-124`).
  The x-equation has no `abs` (since `|X|^2 == X^2`); only the cross term folds.
  Perturbation uses the `diffabs(c,d) = |c+d| - |c|` primitive
  (`deepzoom_delta.glsl:59-63`, `:182-192`), which matches the canonical Kalles
  Fraktaler / mathr diffabs case analysis exactly. Escape `|z|^2 > 4`. Home center
  `(-0.5, -0.5)`, radius 1.5 (`DeepZoomView.cs:48`). This is the same canonical
  Burning Ship as the Wikipedia definition above.

**Settings audit (slider-exposed parameters):**

The only slider-exposed numeric parameters are the Julia constant kappa (formula 2
only) plus the shared palette (`FractalView.cs:123-137`). Center/Radius/iterations
are mouse-driven or depth-scaled, not slider parameters.

| Setting | Default | Min..Max (Step) | Research-recommended | Default OK? | Range OK? | Note |
|---|---|---|---|---|---|---|
| kappa re (Julia) | -0.8 | -2.0..2.0 (0.0001, 4 dec) | Interesting Julia c clusters near the Mandelbrot boundary, Re in ~[-2, 0.5] | Yes (excellent) | Yes (slightly wide on the +Re side) | Default (-0.8, 0.156) is the well-known "Spiral Galaxy" Julia set |
| kappa im (Julia) | 0.156 | -2.0..2.0 (0.0001, 4 dec) | Interesting c has Im in ~[-1.2, 1.2] | Yes (excellent) | Yes (wide) | Pairs with kappa re for Spiral Galaxy |
| MaxIterations | 2000 (code) | iteration floor; scales with depth | — | Yes | Yes | Not a slider; floor of `IterationsForDepth()` (`DeepZoomView.cs:23,59-63`) |
| Radius (zoom) | 1.5 (per-formula home) | 1e-147..4.0 | — | Yes | Yes | Mouse-driven; bounded by `MinRadius`/`ZoomBy` clamp (`DeepZoomView.cs:78,94`) |
| Center | per-formula home string | arbitrary precision | — | Yes | Yes | Mouse-driven, full precision via `BinaryFixed` |

**Discrepancies & recommendations:**
- **kappa default is excellent** — (-0.8, 0.156) is exactly the community-named
  "Spiral Galaxy" Julia set, a strong, visually rich starting value. No change.
- **kappa range `[-2, 2]` is correct but slightly generous on the +Re side.**
  Connected/interesting quadratic Julia sets require `c` near the Mandelbrot set,
  i.e. roughly `Re(c)` in `[-2, 0.5]` and `|Im(c)| <~ 1.2`. The current
  `[-2, 2]` covers all the interesting variety (it spans the full Mandelbrot
  boundary plus margin); the only "wasted" region is `Re > 0.5` and `|Im| > 1.2`,
  where the Julia set is total dust. This is harmless for exploration; if you want
  every slider position to land on a non-trivial set, tightening to `Re in
  [-2, 0.6]`, `Im in [-1.3, 1.3]` would help, but it is optional.
- **kappa only matters for formula 2 (Julia)** but is always shown in the schema.
  The labels already say "(Julia)". A guide should note the slider is inert for
  the other three formulas. Not a bug.
- **No discrepancy in the formula math** — all four reference recurrences, direct
  paths, and perturbation paths match their canonical forms (verified vs the
  in-code mpmath fixtures and external sources). Burning Ship diffabs matches
  Kalles Fraktaler / mathr. Mandelbrot/Julia escape `|z|^2>4` and Prospector's
  `1e6` escape are both correct for their orbit magnitudes.
- **Prospector is custom/undocumented.** The guide must label it a niche custom
  map and describe only the code's behavior, not claim a canonical definition.

**For best results:**
- **Zoom depth and the three paths:** wide views (`Radius > 1e-6`) use exact
  direct fp64 — best for the Burning Ship, which can glitch under perturbation at
  wide deltas. From `1e-6` down to `1e-148` the fast fp64 perturbation path runs.
  Below `1e-148` (down to the `1e-147` floor) the floatexp path takes over: still
  correct but 3-5x slower per iteration, so expect deep frames to render notably
  slower. The mode caps at ~147 orders of zoom.
- **Iterations auto-scale with depth** (`1000 + 1000 * e-foldings`); if filaments
  near the boundary render as solid in-set, you are likely iteration-starved at a
  spot that wants more than the heuristic — there is no manual iteration slider, so
  zoom slightly or accept the heuristic.
- **Julia exploration:** keep kappa near the Mandelbrot boundary for connected,
  detailed sets. Try the named constants below; sweep kappa as a keyframe to morph
  the set in an animation.
- **Switching formula re-homes the view** (`ApplyFormulaHome`,
  `DeepZoomView.cs:42-51`) because each set lives in a different region; kappa is
  left untouched as the user's dial.

**Community-known good Julia constants (kappa):**
- Spiral Galaxy `(-0.8, 0.156)` — the current default
- Douady Rabbit `(-0.122, 0.745)`
- Dendrite `(0, 1.0)`
- San Marco `(-0.75, 0.0)`
- Airplane `(-1.7549, 0.0)`
- Siegel Disk `(-0.391, -0.587)`
- Lightning / Feather `(-0.745, 0.113)`
- Feather `(0.285, 0.01)`
(unverified for exact aesthetic naming, but these are widely circulated canonical
Julia c values; all sit on or near the Mandelbrot boundary.)

**Sources:**
- https://en.wikipedia.org/wiki/Burning_Ship_fractal
- https://en.wikipedia.org/wiki/Mandelbrot_set
- https://en.wikipedia.org/wiki/Julia_set
- https://mathr.co.uk/blog/2021-05-14_deep_zoom_theory_and_practice.html
- https://mathr.co.uk/blog/2022-02-21_deep_zoom_theory_and_practice_again.html
- https://en.wikibooks.org/wiki/Fractals/perturbation
- https://fractalwiki.org/wiki/Perturbation_theory
- https://mathr.co.uk/blog/2018-01-04_the_burning_ship.html (Burning Ship perturbation + diffabs)
- https://math.bu.edu/DYSYS/FRACGEOM/node6.html (Julia set constants)
- https://handwiki.org/wiki/Douady_rabbit
