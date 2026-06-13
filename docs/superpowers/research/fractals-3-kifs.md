# Fractal Research 3: KIFS / Kleinian Family

Research and schema audit for the per-fractal guide feature. Covers the four
KIFS / Kleinian-family fractals in Parsec Hyperdrive. Ground truth is the GLSL
DE cores and the `*State.cs` schemas; web research verifies the canonical math
and community-known good parameter values.

Scope note: shared groups (Palette, Reflection, Light, Camera) are intentionally
omitted; only fractal-specific schema parameters are audited.

Date: 2026-06-13. Repo root: `/mnt/samsung4tb/projects/Parsec`.

---

## Amazing IFS (`FractalType.Kifs`)

**What it is:** The Kaleidoscopic IFS ("Amazing Surface" / amazingIFS) family,
introduced by Knighty on Fractal Forums. Despite the "IFS" name it is an
escape-time system, not an affine IFS: each iteration plane-folds space into the
positive octant, applies a rotation before and after the fold, inverts through a
sphere, then scales toward a pivot. The pre/post rotations composed with repeated
scaling are what wind folded edges into logarithmic-spiral scrollwork; the sphere
fold everts sheets into bell/trumpet flares. With rotations zeroed and the right
pivot/scale it degenerates to the Sierpinski tetrahedron or Menger sponge.

**How Parsec computes it:** `kifs_core.glsl:104-123` runs the iteration:
pre-rotate (`:105`), `abs(z)` plane fold (`:106`), post-rotate (`:107`),
`sphereFold` inversion (`:109`, defined `:62-71`), then scale toward the pivot
`z = scale*z - (scale-1)*pivot` (`:112`) with derivative `dr = dr*|scale|+1`
(`:113`). DE is `length(z)/abs(dr)` (`:125`) - the standard escape-time
fold-family analytic estimator. The CPU mirror used for fly-camera speed is
`KifsDE.cs:13-46`. Schema: `KifsState.cs:42-95`; defaults `:14-25` and
`Reset()` `:96-107`.

**Canonical formula / math background:** Knighty's kaleidoscopic construction
composes scalings, translations, plane reflections (conditional folds) and
rotations (the only conformal 3D maps besides sphere inversion). The base
escape-time iteration without rotation is
`z = abs(z); z = scale*z - offset*(scale-1)` with the canonical Menger/Sierpinski
offset near `(1,1,1)` and `scale = 2` (Sierpinski tetra uses conditional axis
folds plus `scale 2`, `offset 1`). The "kaleidoscopic" upgrade adds a 3D rotation
before and after the fold, turning the rigid polyhedral fractal into the smooth
spiral "amazing" forms. DE for the escape-time form is `length(z)/dr` (or
equivalently `length(z) * scale^-i`).
Sources: Syntopia "Folding Space II: Kaleidoscopic Fractals"
(http://blog.hvidtfeldts.net/index.php/2010/06/folding-space-ii-kaleidoscopic-fractals/),
Syntopia "Folding Space III"
(http://blog.hvidtfeldts.net/index.php/2011/08/distance-estimated-3d-fractals-iii-folding-space/),
Knighty's thread
(http://www.fractalforums.com/sierpinski-gasket/kaleidoscopic-(escape-time-ifs)/).

**Settings audit:**

| Setting | Default | Min..Max (Step) | Research-recommended | Default OK? | Range OK? | Note |
|---|---|---|---|---|---|---|
| Scale | 2.00 | 1.20..3.00 (0.01) | 2.0 canonical; 1.5-2.6 most varied | Yes | Yes | 2.0 is the Sierpinski/Menger base; range good |
| Min radius | 0.50 | 0.05..1.00 (0.01) | 0.5 (Mandelbox inner-fold value) | Yes | Yes | Must stay < Fixed radius for the inversion shell |
| Fixed radius | 1.00 | 0.50..2.00 (0.01) | 1.0 canonical | Yes | Yes | Sphere-fold shell radius; 1.0 is standard |
| Post-rot X/Y/Z | 20/15/10 | -45..45 (1) | small nonzero, e.g. 10-30 deg | Yes | Slightly narrow | These are the curl generators; the most expressive knobs |
| Pre-rot X/Y/Z | 0/0/0 | -45..45 (1) | 0 default; small values for asymmetry | Yes | Slightly narrow | Off by default; rotates before the fold |
| Pivot X/Y/Z | 1/1/1 | -2..2 (0.01) | (1,1,1) canonical Menger/Sierpinski | Yes | Yes | The scale contraction center; strong morphology |
| Iterations | 16 | 4..500 (1) | 12-20 for shape, more for detail | Yes | Yes | 16 is a good balance |
| DE fudge | 0.70 | 0.30..2.00 (0.01) | 0.6-0.8 safe | Yes | Yes | Step shortener; lower = safer/slower |

**Discrepancies & recommendations:**
- The default is a good, recognizable "amazing IFS" view: scale 2, pivot (1,1,1),
  modest curl rotations. No correctness issue.
- The rotation sliders cap at +/-45 degrees. The "amazing" family produces its
  most dramatic spiral/volute reorganization between 30 and 90 degrees of post-fold
  rotation. Consider widening Post-rot (and Pre-rot) to +/-60 or +/-90 to reach the
  full expressive range; the related OrbitHybrid schema already uses +/-90 for its
  "Curl" angles (`OrbitHybridState.cs:81-88`), so +/-45 here is the conservative
  outlier within the same codebase.
- Min radius max of 1.0 equals the Fixed radius default; at min=fixed the shell
  inversion zone vanishes (only the inner linear blow-up remains). This is benign
  but worth a UI note: keep Min radius below Fixed radius for the full sphere-fold
  effect.

**For best results:**
- Start from the default, then sweep Post-rot Z first - it has the strongest
  visual effect on the scrollwork.
- Keep Scale near 2.0 and Pivot near (1,1,1) for clean recognizable structure;
  push Pivot off-axis for organic asymmetry.
- Raise Iterations to 30-60 only when you zoom in; 16 is enough for the overall
  silhouette and keeps the frame fast.

**Sources:**
- http://blog.hvidtfeldts.net/index.php/2010/06/folding-space-ii-kaleidoscopic-fractals/
- http://blog.hvidtfeldts.net/index.php/2011/08/distance-estimated-3d-fractals-iii-folding-space/
- http://www.fractalforums.com/sierpinski-gasket/kaleidoscopic-(escape-time-ifs)/
- https://softologyblog.wordpress.com/2010/05/12/kaleidoscopic-ifs-fractals/

---

## Pseudo-Kleinian (`FractalType.Kleinian`)

**What it is:** The inversive-limit-set ("pseudo-Kleinian") family. It iterates a
sphere inversion plus a box fold (clamp form of the Mandelbox box fold) plus a
scaled translation, producing a 3D Kleinian/Apollonian foam: nested packed
spheres tiling space. Community lineage: Theli-at (2011) discovered it as a hybrid
of two fractals; Knighty reduced it to an unscaled BallFold-BoxFold transform
("AmazingBox + something"). Unlike the Mandelbox/KIFS, this family has no stable
analytic scalar-derivative DE, so the renderers (Mandelbulb3D, Mandelbulber, and
Parsec) measure distance to the level set numerically.

**How Parsec computes it:** Parsec implements this as a Hart/Quilez
distance-to-level-set estimate. `kleinian_core.glsl:50-71` defines
`kleinianPotential`: per iteration, sphere inversion with inner linear zone
(`:60-65`), box fold `z = clamp(z,-cell,cell)*2 - z` (`:67`), then scale+offset
`z = z*scale + c` (`:68`); it returns `log(length(z))` (`:70`). The DE is
`|V(p)| / |grad V(p)|` with the gradient by central differences over 7 potential
evaluations (`estimate`, `:76-114`; gradient `:80-87`; result `:113`). Orbit
traps are accumulated in a re-run loop (`:90-110`). CPU mirror for fly-camera
speed: `KleinianDE.cs:12-39`. Schema: `KleinianState.cs:33-68`; defaults `:13-19`;
`Reset()` `:69-77`.

Note: the shader file `kifs_core` doc header says this family "uses a numerical
gradient" and the in-file comment block (`kleinian_core.glsl:9-23`) confirms the
analytic `|z|/dr` collapses the field to solid, which is why the numerical
gradient is required. This matches the canonical practice.

**Canonical formula / math background:** The pseudo-Kleinian is built from the
two conformal folds of the Mandelbox: a ball fold (sphere inversion,
`if r2<minR2: z*=fixedR2/minR2; elif r2<fixedR2: z*=fixedR2/r2`) and a box fold
(`clamp(z,-1,1)*2 - z`), followed by `z = scale*z + c`. The attractor is the
limit set of the generated inversive group: a Kleinian/Apollonian sphere packing.
The offset `c` (the "tiling offset") is the generator that determines which limit
set you get. Because the iterated inversive map has no convergent analytic
derivative, distance is taken as `|log|z_end|| / |grad log|z_end||` (Hart's
distance-to-level-set / Quilez's potential method).
Sources: IMAGINARY "Hybrid Pseudo Kleinian"
(https://www.imaginary.org/gallery/hybrid-pseudo-kleinian), Syntopia "Folding
Space III" (http://blog.hvidtfeldts.net/index.php/2011/08/distance-estimated-3d-fractals-iii-folding-space/),
sbcode Kleinian inversion tutorial (https://sbcode.net/tsl/kleinian/),
Wikipedia Mandelbox (https://en.wikipedia.org/wiki/Mandelbox).

**Settings audit:**

| Setting | Default | Min..Max (Step) | Research-recommended | Default OK? | Range OK? | Note |
|---|---|---|---|---|---|---|
| Scale | 2.00 | 1.30..2.60 (0.01) | 2.0 typical; 1.5-2.0 dense foam | Yes | Yes | Mandelbox-like similarity scale per iteration |
| Cell size | 1.00 | 0.40..2.00 (0.01) | 1.0 (box fold half-size) | Yes | Yes | The box-fold clamp bound; sets the lattice cell |
| Min radius | 0.50 | 0.10..1.00 (0.01) | 0.5 canonical | Yes | Yes | Inner linear-zone radius of the inversion |
| Fixed radius | 1.00 | 0.50..2.00 (0.01) | 1.0 canonical | Yes | Yes | Inversion shell radius |
| Offset X/Y/Z | 0.5/0.5/1.2 | -2..2 (0.01) | the generator; 0.0-1.5 typical | Yes | Yes | Strongest morphology knob; defines the limit set |
| Iterations | 9 | 4..500 (1) | 8-12 (numerical DE cost is 7x) | Borderline | Yes | Low default is deliberate (7 potential evals/step) |
| DE fudge | 0.70 | 0.30..2.00 (0.01) | 0.5-0.7 (numerical DE is noisier) | Yes | Yes | Keep <=0.7; numerical gradient needs safety margin |

**Discrepancies & recommendations:**
- Default offset `(0.5, 0.5, 1.2)` is a good asymmetric generator that produces a
  recognizable foam; not the symmetric `(0,0,0)` (which gives a more regular
  packing). Reasonable choice. No correctness issue.
- Iterations default 9 is low compared to the other fractals (KIFS 16, PK4D 12),
  but this is intentional and correct: each step costs 7 `kleinianPotential`
  evaluations (1 center + 6 finite-difference offsets), so 9 iterations here is
  roughly 63 potential evals per DE call. Worth surfacing in the guide so users
  understand why raising Iterations is expensive here.
- The DE fudge max of 2.0 is risky for this family: a numerical-gradient DE is
  noisier than an analytic one, and fudge > 1.0 lets the raymarch overstep and
  punch through thin foam walls. Recommend documenting a practical ceiling of
  ~0.8 even though the slider allows 2.0.
- No pre/post rotation exposed (unlike KIFS). That is faithful to the canonical
  pseudo-Kleinian (rotation is optional and omitted in the essential form).

**For best results:**
- Treat Offset as the main creative dial; sweep Offset Z between 0.8 and 1.6 to
  morph the cell stacking.
- Keep Iterations 8-12 for interactive use; only raise it for stills.
- If the surface looks "melted"/solid, lower DE fudge toward 0.5 and nudge Min
  radius down so the inversion shell is wider than the inner zone.

**Sources:**
- https://www.imaginary.org/gallery/hybrid-pseudo-kleinian
- https://sbcode.net/tsl/kleinian/
- http://blog.hvidtfeldts.net/index.php/2011/08/distance-estimated-3d-fractals-iii-folding-space/
- https://en.wikipedia.org/wiki/Mandelbox

---

## Pseudo-Kleinian 4D (`FractalType.PseudoKleinian4D`)

**What it is:** A Kleinian-group limit-set approximation adapted from the
Mandelbulber pseudo-Kleinian formula (Knighty / pseudo-Kleinian lineage),
embedded in 4D with a fixed W slice. It produces the "alien half-space
architecture" look: foamy nested-cell lattices and cathedral-tiling-to-the-horizon
structures. Unlike the analytic Kleinian above, this variant keeps an honest
running derivative `dr` and uses the canonical pseudo-Kleinian tube/slab distance
estimator at the end, so it renders cheaply (no numerical gradient). An optional
sphere inversion conformally bounds the otherwise space-filling half-space tiling
into a ball.

**How Parsec computes it:** `pseudokleinian_core.glsl:61-115`. The 4D point is
`z = vec4(p, w0)` (`:62`); the 4th coordinate is the fixed slice `w0`
(`juliaC.w`). Per iteration (`:76-100`): optional sphere inversion
`z *= gInv/dot(z,z); dr *= same` (`:78-82`), box offset
`z.xyz -= cOff*sign(z.xyz)` (the Kleinian symmetry break, `:85`), clamp-form box
fold `z = abs(z+cSize) - abs(z-cSize) - z` (`:88`), and a one-sided spherical fold
`k = max(sScale/dot(z,z),1); z*=k; dr*=k+tweak` (`:91-92`). The DE
(`:103-114`) is the pseudo-Kleinian slab-intersect-tube identity:
`d1` = a tube about the z-axis (or the quaternionic min-of-four form when
`mode==1`) minus tube radius, `d2 = |z.z|` (distance to the z=0 slab),
`DE = 0.5*(min(d1,d2) - deOffset)/dr`. Schema: `PseudoKleinian4DState.cs:60-127`;
defaults `:21-41`; `Reset()` `:128-143`. There is no CPU DE mirror; fly-camera
speed for this type returns a constant 1.0 (`FractalView.cs:606`).

**Canonical formula / math background:** This matches Mandelbulber's
pseudo_kleinian / jos_kleinian construction: a box offset (sign-dependent
translation that breaks cell symmetry), a Mandelbox box fold, an optional sphere
inversion to conformally bound the half-space tiling, and a one-sided spherical
fold, with the final DE being the "tube about the z-axis intersected with the z=0
slab" form `DE = 0.5*(min(d1,d2) - DEoffset)/dr`. The `z.z` tweak scale
(Parsec's DE tweak / `deTweak`) is documented in Mandelbulber as a small number,
normal use ~1.06 expressed as a small additive (Parsec's default 0.05 is the
additive form). The W slice is the animatable 4th-dimension cut.
Sources: Mandelbulber jos_kleinian_v3 source
(https://github.com/buddhi1980/mandelbulber2/blob/master/mandelbulber2/formula/opencl/jos_kleinian_v3.cl),
IMAGINARY "Hybrid Pseudo Kleinian"
(https://www.imaginary.org/gallery/hybrid-pseudo-kleinian),
Mandelbulber docs on cSize / DEoffset / z.z tweak (https://mandelbulber.org/graemes-blog/).

**Settings audit:**

| Setting | Default | Min..Max (Step) | Research-recommended | Default OK? | Range OK? | Note |
|---|---|---|---|---|---|---|
| Box size X/Y/Z | 1.0/1.0/1.0 | 0.20..2.00 (0.01) | 1.0 (cSize cell) | Yes | Yes | Box-fold half-size; sets the lattice cell |
| Sphere fold | 1.00 | 0.20..2.00 (0.01) | ~1.0; inversion strength | Yes | Yes | One-sided spherical-fold scale |
| Offset X/Y/Z | 0.0/0.0/0.0 | -1.5..1.5 (0.01) | the symmetry break; 0.3-1.0 | Borderline | Yes | At 0 the tiling is symmetric/regular |
| W slice | 0.00 | -2.0..2.0 (0.01) | animatable; 0 is the base slice | Yes | Yes | 4th-dim cut; great for animation |
| Tube radius | 0.00 | 0.00..1.50 (0.01) | 0.0-0.3; thickens the struts | Borderline | Yes | 0 gives thin filaments |
| DE offset | 0.00 | -0.5..0.5 (0.01) | 0.0 default; small for thickness | Yes | Yes | Inflates/deflates the surface |
| DE tweak | 0.050 | 0.00..0.30 (0.001) | ~0.05 (Mandelbulber z.z tweak) | Yes | Yes | Derivative fudge; keep small |
| Inversion scale | 1.00 | 0.20..2.00 (0.01) | ~1.0 | Yes | Yes | gInv; only used when Inversion ON |
| Inversion (0/1) | 1 | 0..1 (1) | 1 = bound into a ball | Yes | Yes | Off needs a larger Bound radius |
| DE form (0/1) | 0 | 0..1 (1) | 0 = tube, 1 = quaternionic min | Yes | Yes | Two DE identities |
| Iterations | 12 | 4..500 (1) | 10-16 | Yes | Yes | Honest dr DE, so cheaper than Kleinian |
| DE fudge | 0.60 | 0.30..2.00 (0.01) | 0.5-0.7 | Yes | Yes | Tube/slab DE is conservative; 0.6 safe |
| Bound radius | 8.0 | 2.0..16.0 (0.1) | 8 with inversion ON; raise if OFF | Yes | Yes | Inversion mode bounds into a ball |

**Discrepancies & recommendations:**
- The default `Offset = (0,0,0)` renders the symmetric base tiling. The state's
  own doc comment (`PseudoKleinian4DState.cs:13-15`) says the offset is "the
  symmetry break... turn it up for the characteristic asymmetric Kleinian
  tiling." So the default deliberately shows the regular form; for a "best first
  impression" the guide should recommend bumping one offset axis to ~0.5-0.8 to
  reveal the signature asymmetric Kleinian look. Consider whether the default
  should ship at e.g. `(0.5, 0, 0)` so the reset state looks more characteristic;
  flagged as a UX choice, not a bug.
- Tube radius default 0.0 yields very thin filaments that can alias/flicker at
  grazing angles. A small default like 0.05-0.1 would give a more solid,
  screenshot-friendly surface out of the box. Minor.
- DE fudge max 2.0 again over-permissive (same caution as Kleinian); practical
  ceiling ~0.8 for the tube/slab DE.
- W slice range +/-2.0 is appropriate and ideal for keyframe animation (the W0
  cut sweeps the 4D structure). No issue.

**For best results:**
- Keep Inversion ON (1) with Bound radius ~8 for the ball-bounded view; if you
  switch it OFF, raise Bound radius toward 12-16 so the raw tiling fits.
- Bump one Offset axis to 0.5-0.8 for the true asymmetric Kleinian architecture.
- Animate W slice for a "flythrough a shifting cathedral" effect; it is the
  cheapest dramatic motion this fractal offers.
- Add a touch of Tube radius (0.05-0.15) to thicken thin struts before rendering
  a hero still.

**Sources:**
- https://github.com/buddhi1980/mandelbulber2/blob/master/mandelbulber2/formula/opencl/jos_kleinian_v3.cl
- https://www.imaginary.org/gallery/hybrid-pseudo-kleinian
- https://mandelbulber.org/graemes-blog/
- https://en.wikipedia.org/wiki/Mandelbox

---

## Orbit Hybrid (KIFS + Mandelbox) (`FractalType.OrbitHybrid`)

**What it is:** A prototype "orbit hybrid": two formulas (KIFS and Mandelbox)
composed into a single orbit, sharing one `z` and one running derivative `dr`,
with the active formula chosen each iteration by a repeating schedule (e.g. 1 KIFS
step then 2 Mandelbox steps, repeating). This is function composition (a genuinely
new shape), not CSG of two finished fields. The pairing was selected after a
parameter study: the originally-attempted Mandelbulb+KIFS pairing was degenerate
(no magnitude cap, every orbit diverges); the rule that fell out is that an orbit
hybrid needs at least one fold that caps `|z|`. Mandelbox's box fold (clamp) is
that cap; KIFS's `abs()` is not.

**How Parsec computes it:** `orbithybrid_core.glsl:100-142`. Each iteration picks
a phase from the schedule `phase = i % cyc` where `cyc = kifsCount + mboxCount`
(`:125`); if `phase < kifsCount` it runs `kifsStep`, else `mboxStep`.
- `kifsStep` (`:83-90`): `abs(z)`, optional post-rotation (the curl), sphere fold,
  `z = scale*z`, `dr = dr*|scale|+1`. Note: no pivot translation (unlike the
  standalone KIFS) and abs does NOT cap `|z|`.
- `mboxStep` (`:92-98`): box fold `z = clamp(z,-L,L)*2 - z` (the magnitude cap that
  bounds the hybrid), sphere fold, `z = scale*z + c` where `c = p` (Mandelbrot
  add), `dr = dr*|scale|+1`.
DE is `length(z)/max(|dr|,eps)` (`:141`). Per the in-file note (`:23-26`), `dr`
over-estimates the true derivative by ~1.7x, so the DE under-estimates distance
(conservative / hole-free). A bailout `length(z) > bailout` breaks the loop
(`:122`). Schema: `OrbitHybridState.cs:61-115`; defaults `:24-41`;
`Reset()` `:116-130`. Fly-camera speed returns constant 1.0 (`FractalView.cs:610`).

**Canonical formula / math background:** There is no single canonical "orbit
hybrid" in the literature; it is Parsec's prototype of the function-composition
hybrid idea that Mandelbulber/Mandelbulb3D generalize with their multi-formula
"hybrid" sequencers (apply formula A for N iterations, then formula B for M, in a
loop). The two component formulas are canonical: KIFS (Knighty's kaleidoscopic
escape-time system, scale ~2) and the Mandelbox (Tom Lowe), whose box fold is
`clamp(z,-1,1)*2 - z`, ball fold `if r2<0.25: z*=4; elif r2<1: z/=r2`, and
similarity `z = scale*z + c`. The Mandelbox's most distinctive sets are at
NEGATIVE scale: -1.5 gives dense, tight, spiky detail (just small enough to avoid
floating corner boxes), -2.0 the classic "boxed" look, -2.5 an organic porous
opening. The hybrid inherits this: `MboxScale` default -1.5 is the canonical rich
negative-Mandelbox value.
Sources: Mandelbox site "Negative 1.5 Mandelbox"
(https://sites.google.com/site/mandelbox/negative-1-5-mandelbox) and "More
negatives" (https://sites.google.com/site/mandelbox/more-negatives),
Wikipedia Mandelbox (https://en.wikipedia.org/wiki/Mandelbox),
IMAGINARY "Hybrid Pseudo Kleinian"
(https://www.imaginary.org/gallery/hybrid-pseudo-kleinian).

**Settings audit:**

| Setting | Default | Min..Max (Step) | Research-recommended | Default OK? | Range OK? | Note |
|---|---|---|---|---|---|---|
| KIFS steps | 1 | 0..6 (1) | 1-2 | Yes | Yes | Steps of KIFS per schedule cycle |
| Mandelbox steps | 2 | 0..6 (1) | 1-3 | Yes | Yes | Steps of Mandelbox per cycle; should dominate (it carries the cap) |
| Iterations | 16 | 4..500 (1) | 12-20 | Yes | Yes | Total cap across the schedule |
| KIFS scale | 1.60 | -3.0..3.0 (0.01) | 1.5-2.0 | Yes | Yes | KIFS similarity scale |
| Curl X/Y/Z | 11/6/0 | -90..90 (1) | small; 0-30 deg | Yes | Yes | Post-fold rotation; +/-90 range is generous (good) |
| Mbox scale | -1.50 | -3.0..3.0 (0.01) | -1.5 (rich), -2.0 classic, -2.5 porous | Yes | Yes | Negative is canonical; -1.5 is the workhorse default |
| Box fold limit | 1.00 | 0.30..2.00 (0.01) | 1.0 canonical | Yes | Yes | The clamp bound that bounds the hybrid orbit |
| Min radius | 0.50 | 0.00..1.50 (0.01) | 0.5 canonical | Yes | Yes | Shared sphere-fold inner radius |
| Fixed radius | 1.00 | 0.30..2.00 (0.01) | 1.0 canonical | Yes | Yes | Shared sphere-fold shell radius |
| Bailout | 30.0 | 6..60 (0.1) | 10-30 | Yes | Yes | Loop escape; 30 is safe |
| DE fudge | 1.00 | 0.30..1.50 (0.01) | 0.7-1.0 | Yes | Yes | dr over-estimates ~1.7x so DE is conservative; 1.0 ok |
| Bound radius | 16.0 | 2.0..10.0 (0.1) | -- | NO | NO | DEFAULT 16.0 IS OUTSIDE THE SLIDER MAX OF 10.0 |

**Discrepancies & recommendations:**
- BUG (schema): The `Bound radius` default and `Reset()` value is **16.0**
  (`OrbitHybridState.cs:41` and `:129`, and `ToParams` `BoundRadius`), but the
  schema slider clamps to `Min = 2.0, Max = 10.0` (`OrbitHybridState.cs:111-113`).
  The default sits 6 units above the slider maximum. On load the live value is 16
  while the slider can only represent up to 10, so the slider thumb pins to the max
  and any user interaction silently snaps Bound radius down from 16 to <=10. Also
  note `FractalView.cs:242` returns `16.0f` as this type's default bound radius,
  confirming 16 is the intended value. Recommend raising the slider `Max` to at
  least 16.0 (or 20.0 for headroom) so the default is reachable and not silently
  reduced. This is the one concrete schema defect in this fractal family.
- The default schedule (KIFS 1 / Mbox 2) correctly lets the Mandelbox (the only
  magnitude-capping fold) dominate, per the documented selection rule. Good.
- `Min radius` allows 0.0 (sphere fold becomes a pure inner-zone no-op below the
  shell); benign but worth noting it disables the ball fold's inner blow-up.
- Both component scales allow the full +/-3.0, which is correct: KIFS likes
  positive ~1.6-2.0, Mandelbox likes negative ~-1.5 to -2.5. Range is well chosen.

**For best results:**
- Leave Mbox scale at -1.5 first; it carries the structure. Then try -2.0 (boxy)
  and -2.5 (porous/organic).
- Keep Mandelbox steps >= KIFS steps so the bounding box fold dominates and the
  orbit stays bounded (the whole reason this pairing was chosen).
- Add small Curl angles (10-30 deg) for KIFS scrollwork laid over the Mandelbox
  cage; large curls can destabilize the shared orbit.
- If you raise Bound radius, be aware of the slider-max bug above; the engine
  honors values up to the live field even though the UI caps at 10.

**Sources:**
- https://sites.google.com/site/mandelbox/negative-1-5-mandelbox
- https://sites.google.com/site/mandelbox/more-negatives
- https://en.wikipedia.org/wiki/Mandelbox
- https://www.imaginary.org/gallery/hybrid-pseudo-kleinian

---

## Cross-fractal summary of schema findings

1. **OrbitHybrid Bound radius default (16.0) exceeds its slider max (10.0)** -
   the one hard schema bug in this family. `OrbitHybridState.cs:41,111-113,129`.
   Raise slider Max to >=16.

2. **KIFS rotation sliders cap at +/-45 deg** while the sibling OrbitHybrid uses
   +/-90 for the same kind of curl angle. The "amazing" family's most dramatic
   reorganization lives beyond 45 deg; widening to +/-90 would expose the full
   range. `KifsState.cs:57-75` vs `OrbitHybridState.cs:81-88`.

3. **DE fudge max of 2.0 on Kleinian and PseudoKleinian4D is over-permissive**
   for their noisier/conservative DEs; practical safe ceiling is ~0.8. Defaults
   (0.70 / 0.60) are fine. Documentation note rather than a code change.

4. **PseudoKleinian4D ships with Offset (0,0,0)** (symmetric base tiling) and
   **Tube radius 0.0** (thin filaments). The signature asymmetric Kleinian look
   needs a nonzero offset; a small offset + small tube radius would make the
   default view more characteristic and screenshot-friendly. UX choice, not a bug.

5. **Kleinian Iterations default 9** is low on purpose (its numerical-gradient DE
   costs 7 potential evals per step); worth surfacing in the guide so users
   understand the cost of raising it. `kleinian_core.glsl:76-114`.

All four DE cores faithfully implement their canonical formulas (KIFS escape-time
fold, pseudo-Kleinian inversion+box-fold with numerical-gradient or tube/slab DE,
Mandelbox box+ball fold in the hybrid). No math/correctness errors were found in
the shader cores; the only concrete defect is the OrbitHybrid Bound radius
slider-range mismatch.
