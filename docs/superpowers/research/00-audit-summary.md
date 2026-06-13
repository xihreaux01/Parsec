# Parsec Fractal Research & Schema Audit — Summary

Consolidated findings from researching every selectable fractal (definition, how
Parsec computes it, canonical math, community-known good parameters) and auditing
each fractal's setting schema: are the **defaults** good out-of-the-box viewing
values, and do the slider **ranges** cover the proper viewing variety?

Per-family detail lives in the sibling files:

- `fractals-1-box.md` — Mandelbox, AmazingBox, Rotated Mandelbox, Folded Menger
- `fractals-2-bulb.md` — Mandelbulb, Quaternion Julia, Bicomplex Julia, QJulia x Box
- `fractals-3-kifs.md` — Amazing IFS (KIFS), Pseudo-Kleinian, Pseudo-Kleinian 4D, Orbit Hybrid
- `fractals-4-exoticA.md` — Apollonian Gasket, Phoenix, Biomorph, Mosely Snowflake
- `fractals-5-exoticB.md` — Riemann Sphere, Mandalay Fold, Anisotropic Fold, Hybrid (box+bulb), Thomas Attractor
- `fractals-6-deepzoom.md` — 3D Burning Ship, Deep Zoom 2D (Mandelbrot / Prospector / Julia / Burning Ship)

All "how Parsec computes it" claims are anchored to the actual shader/DE code
(file:line in the per-family files). Calculation facts come from the code;
optimal-parameter claims come from web research and are cited there. Items that
could not be verified against a published source are marked "unverified."

## Method

For each fractal: read its `*State.cs` (exact parameter Labels, default field
values / Reset values, slider Min/Max/Step/Decimals) and its `*_core.glsl` shader
(and `*DE.cs` CPU mirror where present) as ground truth; web-research the math and
community parameter values; then compare defaults and ranges against findings.
The five highest-impact items below were re-verified directly against source after
the research pass.

## Confirmed code defects (verified in source)

These are independent of the guide feature and are real correctness issues.

| # | Fractal | Issue | Evidence | Recommended fix |
|---|---------|-------|----------|-----------------|
| B1 | Orbit Hybrid | `Bound radius` default/Reset = **16.0** but slider clamps to **Max 10.0** — the default is unreachable; any slider drag snaps it to <=10, changing the DE bound | `OrbitHybridState.cs:41,112-113,129` | Raise slider Max to >=16 (e.g. 20), or lower the default to <=10 |
| B2 | Quaternion Julia | `Reset()` restores `Stereo` but omits `StereoK` (1.0) and `StereoR` (0.8) — Reset leaves stereographic params changed | `QuaternionJuliaState.cs:95-105` vs fields `:27-28` | Add `StereoK = 1.0f; StereoR = 0.8f;` to `Reset()` |
| B3 | Mosely Snowflake | `Reset()` omits `WedgeDeg` (360) and `Fudge` (0.9) — Reset does not restore the documented pure-snowflake state | `MoselyState.cs:70-72` vs fields `:24,26` | Add `WedgeDeg = 360f; Fudge = 0.9f;` to `Reset()` |

## Default / range coverage findings (tuning)

These affect whether the out-of-the-box look is good and whether the sliders span
the "proper viewing variety." Each is a candidate either for a schema change or for
explicit guidance in the guide text.

| # | Fractal | Finding | Recommendation |
|---|---------|---------|----------------|
| R1 | Mandelbox | `Scale` Max = 2.0 excludes the canonical positive scale-3 cityscape and all 2 < s <= 3 forms (`MandelboxState.cs:53`) | Raise Max to 3.0 |
| R2 | Thomas Attractor | `Damping b` default 0.208186 sits at the chaos-onset threshold; true chaos needs b < ~0.208, and the 0.208-0.329 band is a non-chaotic limit cycle (`AttractorState.cs:24,77,127`) | Default ~0.19; optionally cap Max ~0.30 |
| R3 | Mandalay Fold | `Scale` range -3..3 but the bounded set only exists at negative scale (sweet spot -1.5..-2.5); the positive half is a dead zone | Tighten to negative, or note in guide |
| R4 | KIFS (Amazing IFS) | Rotation sliders cap at +/-45 deg while sibling Orbit Hybrid uses +/-90 for the same curl role; the dramatic spirals live beyond 45 deg | Widen to +/-90 deg |
| R5 | Folded Menger | `Offset Z` default 0.0 is non-canonical; the recognizable sponge uses offset (1,1,1) | Default OffsetZ = 1.0, or document as a deliberate variant |
| R6 | Mandelbulb, QJulia, QJBox, Biomorph | Default iteration counts (8, 10, 8, 16) are low — fine for interactive framerate, soft for crisp stills | Guide tip: raise iterations for hero stills |
| R7 | Kleinian, Pseudo-Kleinian 4D | `DE fudge` Max = 2.0 is over-permissive for these noisier/conservative DEs (practical ceiling ~0.8) | Guide note; optional Max tighten |
| R8 | Apollonian Gasket | Outer-radius lower bound 0.85 can clip the gasket's outer lobes below ~0.92 | Guide note; safe sub-range 0.95-1.5 |

## Data-quality caveats (for honest guide text)

- **Prospector** (deep-zoom formula) — no standard "Prospector" fractal found in
  research; it is a custom/niche formula. Describe by code behavior only.
- **3D Burning Ship** — a community triplex extension, not a canonical published
  object; no standard power/escape/DE. Its quirk: Power lives low (2-3), opposite
  of the Mandelbulb.
- **Bicomplex Julia** mul/add params — an artist variant, not standard bicomplex
  math; baseline (muls = 1) recovers the true bicomplex set.
- **Anisotropic Fold** and the **Hybrid** 0.5 DE safety factor — Parsec-original
  parametrizations with no published canonical values; marked unverified.

## Shared groups (audited clean)

Palette (`PaletteState.cs`), Reflections (`ReflectionState.cs`), Light
(`LightState.cs`), and the injected Camera params (`FractalView.cs`) have sensible
defaults and ranges with no issues. They are documented well in their own
doc-comments and will share one common set of guide notes across all fractals.

## Confirmed-good anchors (defaults that match canon)

Worth keeping as-is: AmazingBox negative-scale + rotation default; Quaternion Julia
c = (-0.2, 0.8, 0, 0) (a Paul Bourke catalog constant); deep-zoom kappa =
(-0.8, 0.156) (the community "Spiral Galaxy" Julia); Apollonian tangency 1.0
(Coxeter packing); Phoenix p = -0.5; Biomorph bailout 10 (Pickover canonical);
Mosely Scale 3.0 / Twist 0 / Wedge 360 (exact snowflake).
