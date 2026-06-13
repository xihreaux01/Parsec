# Per-Fractal Guide Research: BULB / JULIA Family

Research + schema audit for the Parsec Hyperdrive per-fractal guide feature. Ground truth is the Parsec source (cited `file:line`); math/parameter claims are web-verified with inline URLs. Claims that could not be independently verified are marked **unverified**.

Scope: Mandelbulb, Quaternion Julia, Bicomplex Julia, QJulia x Box (half-cut). Only fractal-specific schema parameters are audited; shared Palette / Reflection / Light / Camera groups are intentionally excluded.

`FractalType` enum confirmed at `src/Parsec.App/FractalView.cs:21` (values: `Mandelbulb`, `QuaternionJulia`, `Bicomplex`, `QJBox`).

---

## Mandelbulb (`FractalType.Mandelbulb`)

**What it is:** The canonical 3D analogue of the Mandelbrot set, discovered in 2009 by Daniel White and Paul Nylander. It raises a 3D point to the n-th power in spherical coordinates and iterates `z -> z^n + c`. At the canonical power n=8 it produces the iconic bulbous body wrapped in fine filigree detail at the poles. Power is the headline morphology knob and a smooth scalar, so it animates beautifully.

**How Parsec computes it:** `mandelbulb_core.glsl:40-84`. For each iteration it takes `r = length(z)`, polar angle `theta = acos(z.z/r)` (the acos-from-+Z convention that yields the classic pole filigree), and `phi = atan(z.y, z.x)` (`:54-55`). It raises to the power with `zr = pow(r, power)`, multiplies both angles by `power`, rebuilds the vector, and adds `c = p` (Mandelbrot mode, `c` is the sample point) (`:68-72`). The escape-radius distance estimate uses a running derivative `dr -> n*r^(n-1)*dr + 1` carried in **log space** to avoid fp32 overflow past ~iteration 43 (`:57-64`), then `DE = 0.5*log(r)*r/dr` (`:83`). The CPU mirror `MandelbulbDE.cs:11-32` implements the identical formula directly (used only for fly-camera speed scaling, not rendering). `Bailout` is the escape radius (`:50`); `BoundRadius = 1.3` is hardcoded in `MandelbulbState.cs:23`.

**Canonical formula / math background:** White/Nylander define the n-th power of `v=(x,y,z)` as `v^n = r^n (sin(n*theta)cos(n*phi), sin(n*theta)sin(n*phi), cos(n*theta))` with `r=|v|`, `theta=acos(z/r)`, `phi=atan(y/x)` ([Wikipedia: Mandelbulb](https://en.wikipedia.org/wiki/Mandelbulb)). n=8 is the standard render power because higher powers give more symmetric shapes ([Wikipedia](https://en.wikipedia.org/wiki/Mandelbulb)). The running-derivative DE `DE = 0.5*ln(r)*r/dr` with `dr = n*r^(n-1)*dr + 1` is the standard Hubbard-Douady / Boettcher-potential estimator popularized by Inigo Quilez and Mikael Hvidtfeldt Christensen ([Syntopia: Mandelbulb DE](http://blog.hvidtfeldts.net/index.php/2011/09/distance-estimated-3d-fractals-v-the-mandelbulb-different-de-approximations/), [IQ: Mandelbulb](https://iquilezles.org/articles/mandelbulb/)). Power 2 (the "IQ Bulb") is mathematically clean; powers 2-16 are all explorable, with even powers more symmetric and odd powers (3,5,7) reducing to rational polynomials ([Wikipedia](https://en.wikipedia.org/wiki/Mandelbulb), [Skytopia](https://www.skytopia.com/project/fractal/mandelbulb.html)).

**Settings audit:**

| Setting | Default | Min..Max (Step) | Research-recommended | Default OK? | Range OK? | Note |
|---|---|---|---|---|---|---|
| Power | 8.0 | 2..12 (auto) | 8 canonical; 2-16 explorable | Yes (canonical) | Mostly | Min=2 good (IQ Bulb). Max=12 truncates the 13-16 region the community explores; not severe |
| Iterations | 8 | 4..500 (1) | 20-100+ for fine detail | Marginal/low | Yes | Default 8 is low for crisp filigree; community uses higher for detail ([search summary](http://blog.hvidtfeldts.net/index.php/2011/09/distance-estimated-3d-fractals-v-the-mandelbulb-different-de-approximations/)) |
| Bailout | 4.0 | 1.5..20 (auto) | Larger radius = more accurate DE (10-100+) | Marginal | Yes | 4 is on the low side for DE accuracy; raising toward 8-20 sharpens the surface |
| DE fudge | 1.0 | 0.4..2.0 (auto) | ~1.0 (lower if surface noise) | Yes | Yes | Step-safety multiplier; 1.0 is neutral |

**Discrepancies & recommendations:**
- Default `Iterations = 8` (`MandelbulbState.cs:12,46`) is low. For a first-impression "good viewing" default, 16-30 gives noticeably more filigree at negligible cost on a GPU. Recommend raising the default to ~20.
- Default `Bailout = 4` (`:14,48`) is acceptable but conservative for DE accuracy; the community notes larger escape radii give sharper distance estimates. Consider default ~8.
- `Power` max of 12 (`:31`) slightly under-covers the 13-16 high-lobe region some explorers enjoy. Low priority; could extend Max to 16.
- `Power` Min=2 correctly reaches the clean power-2 bulb. Good.

**For best results:**
- Keep Power at 8 for the iconic shape; sweep Power 2 -> 8 as a smooth animation to "grow lobes."
- Bump Iterations to 20+ before zooming in; detail is iteration-limited near the surface.
- If the surface looks noisy/overstepped when zoomed, drop DE fudge toward 0.6-0.8.

**Sources:** [Wikipedia: Mandelbulb](https://en.wikipedia.org/wiki/Mandelbulb), [Inigo Quilez: Mandelbulb](https://iquilezles.org/articles/mandelbulb/), [Syntopia: Mandelbulb DE](http://blog.hvidtfeldts.net/index.php/2011/09/distance-estimated-3d-fractals-v-the-mandelbulb-different-de-approximations/), [Skytopia: Mandelbulb](https://www.skytopia.com/project/fractal/mandelbulb.html).

---

## Quaternion Julia (`FractalType.QuaternionJulia`)

**What it is:** A 4D Julia set: iterate `z -> z^2 + c` in the quaternions, where `c` is a fixed quaternion constant that defines the shape's identity. Because quaternions are a true division algebra, `z^2+c` is analytic and the distance estimate is exact and well-behaved. The 4D object is rendered as a 3D slice (fix the 4th coordinate to `wslice`), and Parsec's signature half-cut clips the solid with a plane to reveal the iconic nested, onion-like interior.

**How Parsec computes it:** `quaternion_julia_core.glsl:60-117`. The seed is `z = (p, wslice)` for a flat slice (`:84`), or an inverse-stereographic wrap of R^3 onto a 3-sphere of radius `R` (scale `k`) in stereographic mode (`:78-82`), with a conformal `deScale = 1/lambda` correction applied to the DE (`:107`). Iteration uses the clean quaternion square `qsq` (vector part = `2*scalar*vector`, `:55-58`) plus `c`, with running derivative `z' -> 2*z*z'` via the Hamilton product `qmul` (`:46-52`, `:94-95`). The DE is `0.5*log(r)*r / |z'|` (`:104`). The half-cut intersects the solid with a half-space via CSG `max(de, dot(p,n) - planeOff)` (`:110-114`); the plane is a true 3D distance and is not scaled by `deScale` (`:108-109`). `BoundRadius = 2.0` is hardcoded (`QuaternionJuliaState.cs:42`).

**Canonical formula / math background:** The standard quaternion Julia is `f(q) = q^2 + c` with `q,c` quaternions; the DE is the Hubbard-Douady / Boettcher-potential estimate `DE = 0.5*|z|*ln|z| / |z'|` ([Inigo Quilez: 3D Julia sets](https://iquilezles.org/articles/juliasets3d/), [Paul Bourke: Quaternion Julia](https://paulbourke.net/fractals/quatjulia/)). The 2D Julia set is a slice of the 4D quaternion object; different 3D slices give different shapes ([juliasets.dk](http://www.juliasets.dk/Quaternion.htm)). Bourke recommends escape radius ~4 and iterations often as low as ~50, and catalogs interesting `c` constants including `(-0.2, 0.8, 0, 0)` (Parsec's exact default), `(-1, 0.2, 0, 0)`, `(-0.2, 0.6, 0.2, 0.2)`, `(-0.2, 0.4, -0.4, -0.4)`, `(0.185, 0.478, 0.125, -0.392)`, and several richer asymmetric values ([Paul Bourke](https://paulbourke.net/fractals/quatjulia/)).

**Settings audit:**

| Setting | Default | Min..Max (Step) | Research-recommended | Default OK? | Range OK? | Note |
|---|---|---|---|---|---|---|
| c.x | -0.2 | -1..1 (auto) | Bourke set incl. -0.2, -1, 0.185 | Yes | Yes | Default = Bourke's classic `(-0.2,0.8,0,0)` |
| c.y | 0.8 | -1..1 (auto) | 0.8, 0.6, 0.4, 0.2 all good | Yes | Yes | Part of canonical constant |
| c.z | 0.0 | -1..1 (auto) | 0 or up to ~0.56 for richer forms | Yes | Yes | 0 gives a clean symmetric slice |
| c.w | 0.0 | -1..1 (auto) | 0, or asymmetric values | Yes | Yes | 0 keeps the slice simple |
| 4D slice (w) | 0.0 | -1..1 (auto) | sweep for morphs | Yes | Yes | 0 = central slice; great animation target |
| Cut (0/1) | 1 | 0..1 (1) | 1 to show interior | Yes | Yes | The killer feature; on by default |
| Cut axis (0X 1Y 2Z) | 0 | 0..2 (1) | any | Yes | Yes | |
| Cut plane offset | 0.0 | -1.2..1.2 (auto) | sweep to slice through | Yes | Yes | Range comfortably exceeds `BoundRadius=2` half-extent of interest |
| Stereographic (0/1) | 0 | 0..1 (1) | 0 default; 1 for curved cut | Yes | Yes | |
| Stereo scale k | 1.0 | 0.3..3.0 (auto) | ~1 frames the wrap | Yes | Yes | |
| Stereo radius R | 0.8 | 0.3..1.6 (auto) | ~0.7-0.9 = separated lobes | Yes | Yes | Default 0.8 sits in the recommended band (per code comment `:76`) |
| Iterations | 10 | 4..500 (1) | ~50 (Bourke); 10 fine for shape | Marginal | Yes | 10 reads the gross shape; raise for fine shell detail |
| DE fudge | 0.9 | 0.4..2.0 (auto) | <1 for safety on cut | Yes | Yes | 0.9 is a sensible conservative default |

**Discrepancies & recommendations:**
- Default `c = (-0.2, 0.8, 0, 0)` (`:19,98`) matches Paul Bourke's catalog exactly. Strong, well-chosen default.
- Default `Iterations = 10` (`:18,97`) is fine for the silhouette but low for crisp interior shell detail; Bourke uses ~50. Optionally raise the default to ~16-20.
- `c` range `[-1,1]` covers essentially all of Bourke's interesting constants (his most extreme component is ~0.63 in magnitude); range is well-sized. No change needed.
- `Reset()` does not restore `StereoK`/`StereoR` to their initializers (`:95-105` resets `Stereo=0` but leaves `StereoK`/`StereoR` at whatever the user last set). Minor state-hygiene nit, not a viewing-quality issue. (Code-level observation; out of audit scope to fix here.)

**For best results:**
- Leave Cut on (1) with offset 0 to reveal the nested interior, then sweep Cut plane offset to "scrub" through the solid.
- Animate `4D slice (w)` from -1 to 1 for a continuous morph of the whole object.
- For the separated-lobe stereographic look, set Stereographic=1 and keep Stereo radius R near 0.8.
- Try alternate Bourke constants, e.g. `(-0.2, 0.6, 0.2, 0.2)` or `(-1, 0.2, 0, 0)`, for different identities.

**Sources:** [Inigo Quilez: 3D Julia sets](https://iquilezles.org/articles/juliasets3d/), [Paul Bourke: Quaternion Julia](https://paulbourke.net/fractals/quatjulia/), [juliasets.dk: Quaternion](http://www.juliasets.dk/Quaternion.htm), [Paul Nylander: Quaternion Julia](https://nylander.wordpress.com/2004/05/08/quaternion-julia-set-fractal/).

---

## Bicomplex Julia (`FractalType.Bicomplex`)

**What it is:** A 4D Julia set built on the bicomplex (tessarine) algebra rather than the quaternions. Bicomplex numbers (`i^2=-1, j^2=+1, ij=k, k^2=+1`) multiply commutatively but are **not** a division algebra (they have zero divisors), so the boundary can show faceted, crystalline, discontinuous character unlike the smooth quaternion Julia. Parsec uses Fracmonk's fractalforums formula with artist-added per-component multipliers and a `W add` that symmetry-break the otherwise-clean bicomplex square for a richer exploration space.

**How Parsec computes it:** `bicomplex_core.glsl:55-120`. The per-iteration step (`bicomplexStep`, `:56-63`) is Fracmonk's formula with mul/add:
```
x' = Xmul*(x*x - y*y - 2*z*w) + Cx
y' = Ymul*(2*x*y + z*z - w*w) + Cy
z' = Zmul*(2*x*z - 2*y*w)     + Cz
w' = Wmul*(2*x*w + 2*y*z) + Wadd + Cw
```
The seed is `z = (p, wslice)` (`:77`). The DE is a Hubbard-Douady running-scalar-derivative `de = 0.5*r*log(r)/dz` (`:108`) with `dz -> 2*maxMul*r*dz` where `maxMul = max(|Xmul|,|Ymul|,|Zmul|,|Wmul|)` absorbs the worst-case stretch from non-unit muls (`:73-75,:90`); this conservatively under-estimates distance (never over). The code comment notes this replaced an earlier numerical-gradient DE that produced wispy filaments because the iteration is non-analytic (`:18-31`). Half-cut uses the same axis-flag CSG `max(de, pn - planeOff)` as qjbox (`:111-117`). `Bailout` is clamped to `>= 2` (`:68`); `BoundRadius = 4.0` (`BicomplexState.cs:44`).

**Canonical formula / math background:** A bicomplex/tessarine number is `t = w + xi + yj + zk` with `ij=ji=k`, `i^2=-1`, `j^2=+1` (Cockle 1848); the bicomplex Julia iterates `P_c(w) = w^2 + c` over bicomplex numbers ([scientificlib: Bicomplex number](https://www.scientificlib.com/en/Mathematics/LX/BicomplexNumber.html), [arXiv 2505.00957: Multicomplex Julia slices](https://arxiv.org/pdf/2505.00957)). Bicomplex numbers are the commutative complex Clifford algebra and have well-studied Fatou/Julia theory ([arXiv](https://arxiv.org/pdf/2505.00957)). The specific component expansion `(x*x - y*y - 2*z*w, 2*x*y + z*z - w*w, 2*x*z - 2*y*w, 2*x*w + 2*y*z)` is the bicomplex square; the per-component `mul`/`W add` augmentation is Fracmonk's artist variant from fractalforums and is **unverified** as a named canonical formula (the multipliers are a Parsec/Fracmonk exploration extension, not standard bicomplex dynamics). The general bicomplex Julia (all muls = 1) is the verified mathematical object.

**Settings audit:**

| Setting | Default | Min..Max (Step) | Research-recommended | Default OK? | Range OK? | Note |
|---|---|---|---|---|---|---|
| c.x | -0.5 | -1.5..1.5 (auto) | ~ -0.5 reasonable Julia c | Yes | Yes | Wider than QJ to allow `BoundRadius=4` forms |
| c.y | 0.0 | -1.5..1.5 (auto) | 0 clean | Yes | Yes | |
| c.z | 0.0 | -1.5..1.5 (auto) | 0 clean | Yes | Yes | |
| c.w | 0.0 | -1.5..1.5 (auto) | 0 clean | Yes | Yes | |
| 4D slice (w) | 0.0 | -1..1 (auto) | sweep for morphs | Yes | Yes | |
| X mul | 1.0 | 0.3..1.5 (auto) | 1.0 = clean square | Yes | Yes | Default 1 = true bicomplex; departing breaks symmetry |
| Y mul | 1.0 | 0.3..1.5 (auto) | 1.0 | Yes | Yes | |
| Z mul | 1.0 | 0.3..1.5 (auto) | 1.0 | Yes | Yes | |
| W mul | 1.0 | 0.3..1.5 (auto) | 1.0 | Yes | Yes | |
| W add | 0.0 | -0.5..0.5 (auto) | 0 clean; small for variety | Yes | Yes | |
| Cut (0/1) | 1 | 0..1 (1) | 1 to show interior | Yes | Yes | On by default |
| Cut axis (0X 1Y 2Z) | 0 | 0..2 (1) | any | Yes | Yes | |
| Cut plane offset | 0.0 | -1.5..1.5 (auto) | sweep | Yes | Yes | |
| Iterations | 12 | 4..500 (1) | 12-30 | Yes | Yes | 12 reads shape; raise for detail |
| Bailout | 4.0 | 2.0..8.0 (auto) | >=2 (clamped in shader) | Yes | Yes | Shader floors at 2 (`:68`) |
| DE fudge | 0.85 | 0.4..2.0 (auto) | <1 (non-analytic, be safe) | Yes | Yes | 0.85 sensibly conservative for the faceted boundary |

**Discrepancies & recommendations:**
- No schema/code mismatches. `Bailout` Min=2 in the schema aligns with the shader's `max(bailout, 2.0)` clamp (`:68`) - consistent.
- Defaults all sit at the "clean bicomplex square" baseline (muls=1, Wadd=0, c=(-0.5,0,0,0)), which is the correct, well-behaved starting point. Good default-as-viewing-value.
- The `mul`/`W add` symmetry-break params are the exploration value-add; their `[0.3,1.5]` and `[-0.5,0.5]` ranges are sensibly bounded so the DE's `maxMul` bound stays meaningful. No change.
- `DE fudge` default 0.85 is well-chosen for a non-analytic iteration where the DE is only an upper bound; leaving headroom avoids overstepping. Good.

**For best results:**
- Start from defaults (clean square), then nudge a single mul (e.g. Y mul to 0.8) to break symmetry and watch crystalline facets emerge.
- Keep Cut on and sweep Cut plane offset to expose the interior; the non-division-algebra structure looks distinct from the smooth quaternion Julia.
- If the boundary shows thin-filament noise, lower DE fudge toward 0.6 rather than raising iterations.

**Sources:** [scientificlib: Bicomplex number](https://www.scientificlib.com/en/Mathematics/LX/BicomplexNumber.html), [arXiv 2505.00957: Classification of 3D slices of Julia sets in multicomplex spaces](https://arxiv.org/pdf/2505.00957), [fractalforums: 3D Mandelbrot formula summary](http://www.fractalforums.com/theory/summary-of-3d-mandelbrot-set-formulas/), [Softology: Kalisets and hybrids](https://softologyblog.wordpress.com/2011/05/04/kalisets-and-hybrid-ducks/).

---

## QJulia x Box, half-cut (`FractalType.QJBox`)

**What it is:** A hybrid that runs both a Mandelbox fold and a quaternion-Julia square every iteration, with a compounding per-iteration rotation as the "magic" morph control and the inherited half-cut to reveal a genuinely intricate (not blobby) cross-section. It is the richest parameter set in the app: Mandelbox fold params, the quaternion constant `c`, `wslice`, three rotation angles, and the cut. The DE is heuristic (no proven lower bound for the hybrid), so a 0.5 safety factor is baked in.

**How Parsec computes it:** `qjbox_core.glsl:88-154`. Per iteration (variant B, both ops in sequence): (1) rotate the 3D part of `z` by a compounding Euler rotation `R = Rz*Ry*Rx` (`:99,:111-113,:72-86`); (2) Mandelbox half on the 3D part: box fold `clamp(z,-foldLim,foldLim)*2 - z`, sphere fold (linear inner scale when `r2 < minR2`, inversion when `r2 < fixedR2`), then `z3*scale + p`, with the derivative tracked as `zp*|scale| + (1,0,0,0)` (`:116-124`); (3) full 4D quaternion square `z -> z^2 + c` with derivative `zp -> 2*qmul(z,zp)` (`:127-129`). The seed `w` is `wslice` (`:102`). DE is `0.5 * 0.5 * log(r)*r / |zp|` (the extra 0.5 is the hybrid safety factor) (`:139`). Half-cut uses axis-flag CSG `max(de, pn - planeOff)` (`:143-151`). Bailout is hardcoded to 4 in the shader (`:96`); `BoundRadius = 4.0` (`QJBoxState.cs:51`). Rotation is entered in degrees and converted to radians via `Deg2Rad` (`QJBoxState.cs:40,48`).

**Canonical formula / math background:** This is a custom Parsec/Mandelbulber-style hybrid, not a single named canonical fractal, so the "canonical" anchors are its two halves. Mandelbox (Tom Lowe): `z -> scale * sphereFold(boxFold(z)) + c`, with standard `scale=2, r=0.5, f=1`; **negative scales** (around -1.5 to -2) are a well-known, especially rich variant ([Syntopia: Mandelbox](http://blog.hvidtfeldts.net/index.php/2011/11/distance-estimated-3d-fractals-vi-the-mandelbox/), [Mandelbox site: more negatives](https://sites.google.com/site/mandelbox/more-negatives), [Mandelbox: what is a Mandelbox](https://sites.google.com/site/mandelbox/what-is-a-mandelbox)). Quaternion Julia: `q -> q^2 + c` as above ([Paul Bourke](https://paulbourke.net/fractals/quatjulia/)). Parsec's default `c = (-0.2, 0.6, 0.1, 0.0)` is a near-match to Bourke's catalogued `(-0.2, 0.6, 0.2, 0.2)` ([Paul Bourke](https://paulbourke.net/fractals/quatjulia/)). The compounding-rotation hybrid behavior is **unverified** as a published formula (it is Parsec's own variant, validated only in its `qjbox_proto.py` per the shader comments `:17-18`).

**Settings audit:**

| Setting | Default | Min..Max (Step) | Research-recommended | Default OK? | Range OK? | Note |
|---|---|---|---|---|---|---|
| Rotate X | 5.7 deg | -30..30 (auto) | small nonzero = morph magic | Yes | Yes | ~0.10 rad; compounds across iters |
| Rotate Y | 4.0 deg | -30..30 (auto) | small nonzero | Yes | Yes | ~0.07 rad |
| Rotate Z | 2.3 deg | -30..30 (auto) | small nonzero | Yes | Yes | ~0.04 rad |
| Cut (0/1) | 1 | 0..1 (1) | 1 to show interior | Yes | Yes | The killer feature; on by default |
| Cut axis (0X 1Y 2Z) | 0 | 0..2 (1) | any | Yes | Yes | |
| Cut plane offset | 0.0 | -1.5..1.5 (auto) | sweep | Yes | Yes | |
| c.x | -0.2 | -1..1 (auto) | Bourke set | Yes | Yes | |
| c.y | 0.6 | -1..1 (auto) | 0.6 (Bourke) | Yes | Yes | |
| c.z | 0.1 | -1..1 (auto) | ~0.2 (Bourke) | Yes | Yes | Slightly under Bourke's 0.2 |
| c.w | 0.0 | -1..1 (auto) | 0 or 0.2 | Yes | Yes | |
| 4D slice (w) | 0.0 | -1..1 (auto) | sweep for morphs | Yes | Yes | |
| Scale | -1.8 | -2.5..-1.2 (auto) | negative-scale variant (-1.5 to -2 rich) | Yes | Yes | Range is negative-only by design |
| Min radius | 0.5 | 0.1..1.0 (auto) | ~0.5 standard | Yes | Yes | Standard Mandelbox r |
| Fixed radius | 1.0 | 0.5..2.0 (auto) | ~1.0 standard | Yes | Yes | Standard Mandelbox f |
| Fold limit | 1.0 | 0.5..2.0 (auto) | ~1.0 standard | Yes | Yes | Box-fold limit |
| Iterations | 8 | 4..500 (1) | 8-20 (hybrid converges fast) | Marginal | Yes | 8 reads shape; raise for detail |
| DE fudge | 0.6 | 0.3..2.0 (auto) | <1 (heuristic DE) | Yes | Yes | 0.6 is appropriately cautious for the unproven-DE hybrid |

**Discrepancies & recommendations:**
- Default `Scale = -1.8` (`QJBoxState.cs:19,123`) sits squarely in the rich negative-scale Mandelbox band the community favors; combined with the negative-only `[-2.5,-1.2]` schema range this is a deliberate, well-chosen design. Good.
- Default `c = (-0.2, 0.6, 0.1, 0.0)` is close to but not identical to Bourke's `(-0.2, 0.6, 0.2, 0.2)`. The chosen values are reasonable; for an even more "textbook" identity, `c.z=0.2, c.w=0.2` could be offered as a preset. Low priority.
- `Iterations = 8` (`:16,122`) is low even for a fast-converging hybrid; the half-cut interior detail benefits from ~12-16. Consider raising the default.
- `DE fudge` default 0.6 (`:38,135`) is correctly conservative given the heuristic DE with its 0.5 safety factor; raising it risks overstepping artifacts. Keep low.
- Bailout is fixed at 4 in the shader (`qjbox_core.glsl:96`) with no schema control - intentional (the hybrid does not expose it). No discrepancy, just noting it is not user-adjustable here.

**For best results:**
- The rotation angles are the morph stars: sweep Rotate X/Y/Z slightly (they compound per iteration, so small changes have large visual effect) for dramatic animations.
- Keep Cut on and scrub Cut plane offset to expose the intricate interior cross-section (the feature this hybrid was built around).
- Keep Scale in the -1.5 to -2.0 zone for the richest structure; pair with the standard fold params (min 0.5, fixed 1.0, limit 1.0).
- Leave DE fudge low (~0.6); if you see banding/overstep when zooming, lower it further before adding iterations.

**Sources:** [Syntopia: Mandelbox DE](http://blog.hvidtfeldts.net/index.php/2011/11/distance-estimated-3d-fractals-vi-the-mandelbox/), [Mandelbox: more negatives](https://sites.google.com/site/mandelbox/more-negatives), [Mandelbox: what is a Mandelbox](https://sites.google.com/site/mandelbox/what-is-a-mandelbox), [Paul Bourke: Quaternion Julia](https://paulbourke.net/fractals/quatjulia/).

---

## Summary of key schema findings

- **Mandelbulb:** Default `Iterations=8` and `Bailout=4` are both on the low side for crisp detail / DE accuracy (recommend ~20 iters, ~8 bailout). Power range tops out at 12, slightly under the 13-16 the community explores. Power default 8 and Min 2 are correct.
- **Quaternion Julia:** Default `c=(-0.2,0.8,0,0)` is exactly a Paul Bourke catalog constant - excellent. `Iterations=10` is fine for shape, low for shell detail. Stereo radius default 0.8 sits in the documented separated-lobe band. Minor: `Reset()` does not restore StereoK/StereoR.
- **Bicomplex:** Clean, consistent. Defaults sit at the true bicomplex baseline (muls=1). Schema Bailout Min=2 matches the shader clamp. DE fudge 0.85 well-chosen for the non-analytic iteration. The `mul`/`W add` augmentation is an unverified artist (Fracmonk) variant, not standard bicomplex math.
- **QJBox:** Default `Scale=-1.8` is in the rich negative-scale Mandelbox band; negative-only schema range is intentional. Default `c=(-0.2,0.6,0.1,0)` is near Bourke's `(-0.2,0.6,0.2,0.2)`. `Iterations=8` is low. DE fudge 0.6 correctly conservative for the heuristic (unproven-lower-bound) hybrid DE. Bailout is hardcoded (not exposed).

_No source code was modified. This document is the sole artifact._
