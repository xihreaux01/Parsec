# Per-Fractal Guide Research — BOX Family

Research + schema audit for the four "box-fold" fractals in Parsec Hyperdrive:
Mandelbox (classic), AmazingBox, Rotated Mandelbox, and Folded Menger. All
calculation claims are anchored to the actual Parsec source (file:line). Web
claims are cited inline; anything not verifiable is marked "unverified."

Shared mechanics (verified from `mandelbox_core.glsl`): every box-fold fractal
here runs a flat per-iteration loop of fold -> sphere-fold -> scale+translate,
tracking a scalar running derivative `dr` and returning the standard Mandelbox
distance estimate `DE = |z| / |dr|`. The sphere fold is the canonical two-zone
form (linear blow-up inside `minRadius`, sphere inversion in the shell out to
`fixedRadius`), matching Hvidtfeldt/Buddhi's reference DE.

---

## Mandelbox (`FractalType.Mandelbox`)

**What it is:** Tom Lowe's 2010 box-shaped escape-time fractal. Space is folded
back on itself by a box fold (reflect anything outside [-L, L]) and a sphere
fold (invert points inside a shell), then scaled and translated, repeated. The
classic look is a self-similar "arches and corridors" cityscape. It is the
parent of the whole fold family.

**How Parsec computes it:** `mandelbox_core.glsl` mode 0. Per iteration
(`mandelbox_core.glsl:116-139`): `boxFold` = `clamp(z,-L,L)*2 - z`
(`:54-56`); optional Euler rotation if any angle is nonzero (`:122`); two-zone
`sphereFold` modifying `z` and `dr` together (`:70-80`); then
`z = scale*z + offset` and `dr = dr*|scale| + 1` (`:128-129`). Offset is the
sample point `p` (Mandelbrot mode). Bailout at `|z|^2 > 1000` (`:138`) — this
matters: without it the interior `dr` blows up and the whole field collapses to
"solid" (`:108-114`). DE returned as `length(z)/abs(dr)` (`:141`). The CPU
mirror in `MandelboxDE.cs:31-55` is identical and is used only for flycam speed.

**Canonical formula / math background:** Wikipedia's pseudocode is exactly
Parsec's mode 0: for each component, if `>1` -> `2-c`, if `<-1` -> `-2-c` (box
fold with L=1); then if `|z|<0.5` -> `z*=4`, else if `|z|<1` -> `z/=|z|^2`
(sphere fold with minRadius=0.5, fixedRadius=1); then `z = scale*z + c`
([Wikipedia: Mandelbox](https://en.wikipedia.org/wiki/Mandelbox)). The standard
parameter set is **s=2, minRadius=0.5, fixedRadius=1, foldingLimit=1** (verified,
Wikipedia + the Fragmentarium reference frag, whose Default preset uses
Scale 2.04344, MinRad2 0.25 i.e. minRadius 0.5
([Fragmentarium Mandelbox.frag](https://github.com/Syntopia/Fragmentarium/blob/master/Fragmentarium-Source/Examples/Historical%203D%20Fractals/Mandelbox.frag))).
Known scale sweet spots: **2** and **3** (positive, solid-core cityscape forms,
solid core when 1<|s|<2), and the negatives **-1.5** and **-2** which give the
hollow/organic look. Scale **-1.5** is famous for containing approximations of
many other well-known fractals on its surface
([Wikipedia](https://en.wikipedia.org/wiki/Mandelbox);
[mandelbox.google.site / Negative 1.5](https://sites.google.com/site/mandelbox/negative-1-5-mandelbox)).

**Settings audit:**

| Setting | Default | Min..Max (Step) | Research-recommended | Default OK? | Range OK? | Note |
|---|---|---|---|---|---|---|
| Scale | 2.0 | -3.0..2.0 (—) | 2 default; sweet spots {-2, -1.5, 2, 3} | Yes | **Too narrow at top** — cuts off 3 | Max 2.0 excludes the canonical scale-3 cityscape and all positive 2<s<=3 forms. Recommend Max = 3.0. |
| Min radius | 0.5 | 0.05..1.0 (—) | 0.5 | Yes | Yes | Canonical. |
| Fixed radius | 1.0 | 0.5..2.0 (—) | 1.0 | Yes | Yes | Canonical. |
| Folding limit | 1.0 | 0.5..2.0 (—) | 1.0 (~1) | Yes | Yes | Canonical ~1. |
| Rotate X/Y/Z | 0 / 0 / 0 | -45..45 (1°) | 0 for classic | Yes | Yes | Zero keeps the clean exact DE; non-zero curls it. |
| Iterations | 14 | 4..500 (1) | 11-16 typical | Yes | Yes (wide) | Plenty. |
| DE fudge | 0.9 | 0.3..2.0 (—) | ~0.9-1.0 unrotated | Yes | Yes | Raise toward 1.0 for speed when unrotated. |

**Discrepancies & recommendations:**
- **Scale Max should be 3.0, not 2.0.** The classic positive-scale Mandelbox
  cityscapes at scale 2.5-3 are a canonical, well-known form and the slider
  currently can't reach them. The state comment even says "optimal classic
  cityscape" yet caps at 2. Widen to `Max = 3.0`.
- The negative end (-3..) correctly covers -1.5 and -2; good.
- Consider snapping/marking the {-2, -1.5, 2, 3} sweet spots in the guide text.

**For best results:**
- Start at the default scale 2 for the solid cityscape; push to 3 (after the
  range fix) for taller, more open architecture.
- Flip to **-1.5** to explore the "fractal zoo" surface; expect to zoom for the
  embedded shapes. Use **-2** for the dense hollow look.
- Keep rotations at 0 here — the DE is exact and the marcher can take near-full
  steps; raise DE fudge toward 1.0 for speed.
- Raise iterations only when zooming deep; 14 is fine for overview shots.

**Sources:**
- https://en.wikipedia.org/wiki/Mandelbox
- https://sites.google.com/site/mandelbox/negative-1-5-mandelbox
- https://sites.google.com/site/mandelbox/what-is-a-mandelbox
- https://github.com/Syntopia/Fragmentarium/blob/master/Fragmentarium-Source/Examples/Historical%203D%20Fractals/Mandelbox.frag

---

## AmazingBox (`FractalType.AmazingBox`)

**What it is:** A Mandelbox variant that replaces the plain box fold with the
"Amazing" fold: first reflect into the positive octant (`z = abs(z)`), then box
fold. With a negative scale and a small inter-fold rotation this yields the
richly detailed, curled "Amazing Surf / Amazing Box" forms that dominate a lot
of fractal art. Same DE machinery as the Mandelbox; only the fold and the
default rotation differ.

**How Parsec computes it:** `mandelbox_core.glsl` mode 1. Identical loop to the
Mandelbox except the fold is `amazingFold` = `abs(z)` then box fold
(`mandelbox_core.glsl:64-67`, selected at `:118`). `abs()` is Lipschitz-1 so it
leaves `dr` untouched (code comment `:59-63`). The Parsec default ships with a
non-zero rotation (X=13°, Y=9°, Z=-20°, `AmazingBoxState.cs:19-21`), so the
optional rotation branch (`:122`) is active out of the box, which is the
intended Amazing-Surf curl. CPU mirror: `MandelboxDE.cs:33-37`.

**Canonical formula / math background:** The Amazing Surf / AmazingBox family is
the abs-fold Mandelbox; in Mandelbulber it is literally an alias ("AmazingBox is
also named Mandelbox") with added per-fold rotation, sphere-fold offset, and
fold controls. Typical good parameters from the community use a **negative
scale** (canonically around -1.5; the well-known Mandelbox negative range is
-1.1..-2) with a **small rotation** to generate the surf curl, `Min R` ~0.5,
`Fold` ~1
([Mandelbulber / Amazing Surf docs](https://github.com/buddhi1980/mandelbulber2/releases);
[Amazing Surf "Roots" how-to](https://www.deviantart.com/lukasfractalizator/journal/An-Amazing-Surf-Roots-How-To-532210631)).
The positive-scale abs fold instead fills space into a solid 8-fold-symmetric
slab (Parsec code comment `:59-63`; consistent with the solid-core property for
1<|s|<2 on [Wikipedia](https://en.wikipedia.org/wiki/Mandelbox)).

**Settings audit:**

| Setting | Default | Min..Max (Step) | Research-recommended | Default OK? | Range OK? | Note |
|---|---|---|---|---|---|---|
| Scale | -1.5 | -3.0..-1.0 (—) | -1.5 (sweet spot) | Yes | Yes (intentionally negative-only) | -1.5 is the famous abs-fold value; range correctly negative-only so you can't flatten it to the solid slab. |
| Min radius | 0.5 | 0.05..1.0 (—) | 0.5 | Yes | Yes | Canonical. |
| Fixed radius | 1.0 | 0.5..2.0 (—) | 1.0 | Yes | Yes | Canonical. |
| Folding limit | 1.0 | 0.5..2.0 (—) | ~1 | Yes | Yes | Canonical. |
| Rotate X/Y/Z | 13° / 9° / -20° | -45..45 (1°) | small non-zero curl | Yes | Yes | Good "interesting by default" choice; the surf curl is what makes AmazingBox distinct from a plain negative Mandelbox. |
| Iterations | 14 | 4..500 (1) | 11-16 | Yes | Yes | Fine. |
| DE fudge | 0.8 | 0.3..2.0 (—) | <1 because rotated | Yes | Yes | Lower than Mandelbox's 0.9 correctly compensates for the rotated DE overshoot. |

**Discrepancies & recommendations:**
- None major. Defaults and ranges are well chosen and the negative-only Scale
  range is a deliberate, correct constraint (positive scale would collapse the
  abs fold to a dull solid slab — confirmed by the code comment).
- Minor: the abs fold can also be interesting nearer **-2**; range already
  covers it, so just call it out in the guide.

**For best results:**
- Leave the default rotation on — it is the point of AmazingBox. Nudge Z (the
  largest angle) a few degrees to morph the curl dramatically.
- Sweep Scale -1.3..-2.0 slowly; the surf detail changes character across that
  band.
- Keep DE fudge <=0.8 while rotated; raise iterations for deep zooms only.
- For animation, the three rotation sliders are the best morph targets.

**Sources:**
- https://en.wikipedia.org/wiki/Mandelbox
- https://github.com/buddhi1980/mandelbulber2/releases
- https://www.deviantart.com/lukasfractalizator/journal/An-Amazing-Surf-Roots-How-To-532210631
- https://fractal.batjorge.com/tag/amazingsurf/

---

## Rotated Mandelbox (`FractalType.RotBox`)

**What it is:** A standard Mandelbox with a full 3D rotation inserted *before*
the folds every iteration, so the fold planes cut space at arbitrary angles
instead of axis-aligned. The rotation compounds across iterations, making the
three Euler angles extremely sensitive morph knobs — the most generative,
animation-friendly controls in the box family.

**How Parsec computes it:** `rotbox_core.glsl`. Per iteration (`:71-91`):
`z = R*z` pre-rotate (`:72`); box fold `clamp(z,-foldLim,foldLim)*2 - z`
(`:73`); two-zone sphere fold (`:75-82`); then `z = z*scale + p` and
`dr = dr*|scale| + 1` (`:84-85`). DE = `length(z)/|dr|` (`:93`). Note the
parameter packing differs from the Mandelbox core: here
`boxParams = (scale, minRadius, fixedRadius, foldLimit)` and the Euler angles
live in `surfParams.xyz` in radians (`:32-33`). CPU mirror: `RotBoxDE.cs:20-39`.

**Canonical formula / math background:** This is the rotated-fold / Amazing-Surf
mechanism (rotation between folds is the standard "curl generator" cited for
AmazingSurf in Mandelbulber). Distinct from AmazingBox in that the rotation
here is applied to a *plain* box fold (no abs), and is the headline control
rather than a small default. Negative scale (canonically **-2**, also -1.5) is
standard for the rotated Mandelbox; small angles (a few to ~15°) keep the DE a
clean valid field while still morphing the shape strongly. Large angles degrade
the DE (overshoot), which is why a DE fudge <1 is used
([Wikipedia: Mandelbox](https://en.wikipedia.org/wiki/Mandelbox);
[Mandelbulber AmazingSurf rotation controls](https://github.com/buddhi1980/mandelbulber2/releases)).
(The "validated in Python rotbox_proto.py" note in the shader header is an
internal claim, unverified externally.)

**Settings audit:**

| Setting | Default | Min..Max (Step) | Research-recommended | Default OK? | Range OK? | Note |
|---|---|---|---|---|---|---|
| Rotate X | 8.6° | -45..45 (0.01) | small, a few..~15° | Yes | Yes | Fine starting curl. |
| Rotate Y | 5.7° | -45..45 (0.01) | small | Yes | Yes | — |
| Rotate Z | 2.9° | -45..45 (0.01) | small | Yes | Yes | High-precision step (0.01°) is right — these are sensitive. |
| Scale | -2.0 | -3.0..-1.2 (—) | -2 (canonical), -1.5 | Yes | Slightly narrow at top | Negative-only is correct; Max -1.2 just barely excludes -1.0..-1.2. Acceptable; could relax to -1.0 to reach the gentler negatives. |
| Min radius | 0.5 | 0.1..1.0 (—) | 0.5 | Yes | Yes | Canonical. |
| Fixed radius | 1.0 | 0.5..2.0 (—) | 1.0 | Yes | Yes | Canonical. |
| Fold limit | 1.0 | 0.5..2.0 (—) | ~1 | Yes | Yes | Canonical. |
| Iterations | 12 | 4..500 (1) | 10-16 | Yes | Yes | Fine. |
| DE fudge | 0.85 | 0.4..2.0 (—) | <1 (rotated) | Yes | Yes | Correctly conservative for the rotated DE. |

**Discrepancies & recommendations:**
- None critical. The schema is well tuned for this fractal's purpose (morph by
  rotation). Defaults are interesting out of the box and ranges are sensible.
- Optional: relax Scale Max from -1.2 to -1.0 to expose the softer negatives;
  and consider clarifying in-guide that angles past ~20-30° start to break the
  DE (artifacts), so DE fudge may need lowering there.

**For best results:**
- Animate the three rotation angles — they are the standout morph targets in the
  whole app. Tiny changes (use the 0.01° precision) cascade into very different
  forms.
- Keep total rotation modest (<~15-20° per axis) to keep the surface crisp; if
  you push higher, drop DE fudge toward 0.5-0.6 to suppress overshoot artifacts.
- Pair scale -2 with the default angles for the signature rotated cityscape.
- Bump iterations to ~16 before deep zooms.

**Sources:**
- https://en.wikipedia.org/wiki/Mandelbox
- https://github.com/buddhi1980/mandelbulber2/releases
- https://fractal.batjorge.com/tag/amazingsurf/

---

## Folded Menger (`FractalType.Menger`)

**What it is:** A Menger-sponge-style iterated function system (IFS) rendered as
a distance estimate, with an abs fold, a magnitude sort of the components, and a
pre-rotation each iteration to break strict axis-alignment. Produces stacked,
rectilinear, "alien temple / architectural" geometry — a different aesthetic
category from the organic fold fractals.

**How Parsec computes it:** `menger_core.glsl`. Per iteration (`:74-94`):
`z = R*z` pre-rotate (`:75`); `z = abs(z)` reflect to positive octant (`:76`);
pairwise sort so the largest magnitude component goes to z (`:79-81`); IFS
shrink-and-translate `z = z*scale - off*(scale-1)` (`:84`) with a conditional
z-axis re-centering `if z.z < -off.z*(scale-1)*0.5: z.z += off.z*(scale-1)`
(`:85-87`); `dr *= scale` (`:88`). Final DE is a bounding-cube estimate
`d = max(|z|-1, 0); return length(d)/|dr|` (`:97-98`). Note: `dr` here uses raw
`scale` (no abs), so scale must stay positive — the schema enforces Min 2.0.
Parameter packing: `boxParams = (scale, offX, offY, offZ)`, angles in
`surfParams.xyz` (`:40-41`). CPU mirror: `MengerDE.cs:16-38`.

**Canonical formula / math background:** This is the standard folded-IFS Menger
DE popularized by Hvidtfeldt ("Folding Space"): fold/sort then
`z = z*Scale - Offset*(Scale-1)` and accumulate `pow(Scale, -n)`, equivalently
`dr *= Scale`
([Syntopia: Folding Space](http://blog.hvidtfeldts.net/index.php/2011/08/distance-estimated-3d-fractals-iii-folding-space/);
[Menger DE snippet, Snipplr](https://snipplr.com/view/33781/)). The **classic
Menger sponge is Scale = 3 with Offset = (1,1,1)** — that's exactly the Parsec
default (`MengerState.cs:16-19`), reproducing the canonical 1/3-self-similar
sponge. Non-integer scales (e.g. 2..5) and varied offsets produce the
"pseudo-Menger" architectural variants; rotation morphs them
([Wikipedia: Menger sponge](https://en.wikipedia.org/wiki/Menger_sponge);
[MathWorld](https://mathworld.wolfram.com/MengerSponge.html)). The default
OffsetZ = 0 (vs X=Y=1) is a Parsec choice that flattens the z-fold; the
canonical sponge uses (1,1,1).

**Settings audit:**

| Setting | Default | Min..Max (Step) | Research-recommended | Default OK? | Range OK? | Note |
|---|---|---|---|---|---|---|
| Rotate X | 5.7° | -30..30 (0.01) | small | Yes | Yes | Smaller effect than RotBox (abs fold reflects after rotation) but meaningful. |
| Rotate Y | 4.0° | -30..30 (0.01) | small | Yes | Yes | — |
| Rotate Z | 2.3° | -30..30 (0.01) | small | Yes | Yes | -30..30 is a sensible tighter clamp than the box family's 45. |
| Scale | 3.0 | 2.0..5.0 (0.001) | **3 = classic sponge** | Yes | Yes | Default is exactly canonical Menger. Positive-only is required (dr uses raw scale). |
| Offset X | 1.0 | 0.0..2.0 (0.001) | 1.0 | Yes | Yes | Canonical. |
| Offset Y | 1.0 | 0.0..2.0 (0.001) | 1.0 | Yes | Yes | Canonical. |
| Offset Z | 0.0 | 0.0..2.0 (0.001) | 1.0 for the true sponge | **Borderline** — see below | Yes | Default 0 gives a slab-like z-flattened variant, not the symmetric (1,1,1) sponge. Defensible aesthetic choice but not the canonical look. |
| Iterations | 5 | 3..500 (1) | 4-6 (IFS converges fast) | Yes | Yes | Menger converges quickly; 5 is a good overview. |
| DE fudge | 0.8 | 0.4..2.0 (—) | <1 (rotated/sort DE) | Yes | Yes | Conservative for the sort+rotation DE. |

**Discrepancies & recommendations:**
- **Offset Z default = 0.0 is the one questionable default.** The canonical,
  most-recognizable Menger sponge is Offset (1,1,1). With Z=0 the IFS does not
  fold the third axis symmetrically, giving a flatter, slab-leaning form. If the
  guide's goal is "good out-of-the-box recognizable Menger," set the default to
  **1.0**. If the intent is a deliberately distinct architectural variant, keep
  0 but document it as non-canonical.
- Ranges are otherwise excellent; the high-precision (0.001) Scale/Offset and
  (0.01°) rotation steps are appropriate for these sensitive controls.
- Scale being positive-only (2..5) is correct given the DE's raw-`scale`
  derivative; do not extend below the dr-stability point or to negatives.

**For best results:**
- For the textbook Menger sponge, set Offset to (1,1,1) and Scale to exactly 3,
  rotations to 0.
- Then introduce a few degrees of rotation to morph the cubes into twisted
  "temple" architecture — small angles go a long way here.
- Sweep Scale 2.5..3.5 for denser vs. airier lattices; non-3 scales give the
  pseudo-Menger variants.
- Keep iterations low (4-6); the IFS detail saturates fast and higher counts
  mostly cost performance.

**Sources:**
- http://blog.hvidtfeldts.net/index.php/2011/08/distance-estimated-3d-fractals-iii-folding-space/
- https://snipplr.com/view/33781/
- https://en.wikipedia.org/wiki/Menger_sponge
- https://mathworld.wolfram.com/MengerSponge.html

---

## Summary of schema discrepancies (most important first)

1. **Mandelbox Scale Max = 2.0 is too low.** Excludes the canonical positive
   scale-3 cityscape (and all 2<s<=3). Recommend `Max = 3.0`. The state's own
   comment ("optimal classic cityscape") contradicts the 2.0 cap.
2. **Folded Menger Offset Z default = 0.0** yields a non-canonical, flattened
   sponge. The recognizable Menger sponge is Offset (1,1,1); recommend default
   `OffsetZ = 1.0` (or document the slab variant as intentional).
3. **RotBox Scale Max = -1.2** barely excludes the soft negatives -1.0..-1.2;
   minor, optional relax to -1.0.
4. Everything else (radii, folding limits, rotations, iterations, DE fudges)
   matches canonical/community values and the ranges are appropriate. AmazingBox
   in particular is well tuned (negative-only Scale + interesting default curl).
