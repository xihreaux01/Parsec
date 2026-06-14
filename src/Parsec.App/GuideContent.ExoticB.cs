using System.Collections.Generic;

namespace Parsec.App;

// EXOTIC group B guide prose. Five fractals that each push past the plain
// escape-time / distance-estimator recipe taught in the shared primer:
// a sphere-wrapped escape set, two Mandelbox-family fold variants, a
// box-plus-bulb composition, and a strange attractor that is a traced path
// rather than an escape test. Prose is rewritten for a reader who knows
// nothing about fractals; SettingNotes and BestResults are copied verbatim
// from the audited research (a test asserts every label keeps its note).
public static partial class FractalGuide
{
    private static GuideContent RiemannSphere => new()
    {
        Title = "Riemann Sphere",
        WhatItIs = new[]
        {
            "Most of the 3D fractals here are built by repeating a rule on a point in flat 3D space and asking whether that point's path stays bounded or shoots off to infinity. This one does the same kind of test, but it first wraps the flat plane around a ball. There is a classic trick in mathematics for turning the infinite flat 2D plane into a sphere: imagine a globe sitting on a tabletop, with the table being the plane. Draw a straight line from the north pole of the globe through any point on the table; that line pierces the globe at exactly one place. So every point on the infinite table corresponds to one point on the globe, and the single north pole stands in for 'infinity'. That correspondence is called stereographic projection, and the globe is called the Riemann sphere. This fractal runs its escape test on coordinates that have been wrapped onto that ball, so the resulting set drapes over a sphere instead of spreading across a plane.",
            "The recipe is adapted from Mandelbulber, a well-known fractal renderer, where it is called 'Msltoe Riemann Sphere V1' after the user who designed it. Inside each repetition it folds the wrapped coordinates with an abs(sin()) operation. 'abs' means absolute value (drop the minus sign so everything is positive) and 'sin' is the sine wave from trigonometry; together they bounce a coordinate back and forth in a smooth, repeating, mirror-like way. That fold is the generator of the organic, coral-like, cellular look this formula is known for.",
            "Two numbers, Fold offset A and Fold offset B, shift the phase of those two sine waves (where in the wave's cycle the bounce starts). They are the expressive, animatable knobs: nudging them continuously reshapes the surface. Setting both offsets to zero gives the symmetric, canonical version of the shape; pulling them apart breaks the symmetry for a livelier look.",
        },
        HowComputed = new[]
        {
            "The shared primer explains the general loop: keep a point called z, apply a rule to it over and over, and watch how fast it escapes. Here each pass through the loop has five stages. First, z is projected onto a sphere whose size is set by Scale, and stereographic projection turns that sphere position into a flat-plane coordinate written as the pair (s, t). Second, a variable exponent is computed from how far out that coordinate sits: pe = min(1 + s*s + t*t, pClamp). The 'min' just caps it at the Power clamp ceiling so it cannot grow without bound. Third, the abs(sin()) fold is applied to s and t using the two offsets. Fourth, a radial power map raises the point's distance-from-center to a high power (this is what creates self-similar detail at many scales). Fifth, the point is projected back off the sphere into 3D and the constant c is added, which anchors the fractal in space.",
            "Alongside z the loop carries a second running number called the derivative, dr. Intuitively dr tracks how much the rule is stretching space near z at each step; the shared primer describes why a distance estimator needs this. It is updated in the same Mandelbulb style used elsewhere in the app. When z finally escapes (its distance from the center exceeds the Bailout radius) the loop stops, and the distance estimate is formed from z and dr.",
            "That distance estimate is approximate, not exact. It reuses the standard Mandelbulb formula and deliberately ignores the angular twist contributed by the sine fold and the way the exponent pe changes from place to place. This is the same class of approximation that makes ordinary Mandelbulbs renderable at all, so it is expected and well behaved; if it shows as faceting, the cure is to lower DE fudge before adding iterations.",
            "Bailout is kept unusually low on purpose, around 2. The radial power map uses the exponent 2*pe, which can reach about 72 at the Power clamp ceiling of 36. Raising a number to the 72nd power overflows a 32-bit float (exceeds the largest value it can hold) once the point gets even modestly far from the center, so the escape test has to declare 'escaped' early to stay numerically safe. That is why both Bailout and Power clamp are capped where they are.",
        },
        Math = new[]
        {
            "The Riemann sphere is what mathematicians call the one-point compactification of the complex plane: take the flat plane of complex numbers and add a single point named 'infinity', and the result behaves exactly like the surface of a sphere. Stereographic projection is the explicit map between them. A plane coordinate written s + i*t (where i is the imaginary unit, the square root of -1, and s, t are ordinary real numbers) corresponds to the sphere point (2*s, 2*t, s*s + t*t - 1) / (1 + s*s + t*t), and the inverse runs the other way. Plotting an escape-time field on the sphere instead of the flat plane is a known visualization in the fractal community; the specific 3D 'Msltoe Riemann Sphere V1' rule used here is a Mandelbulber formula type.",
            "Two practical constants follow from the formula itself. The effective exponent in the radial map is 2*pe, and pe is clamped at Power clamp = 36, so the largest exponent in play is about 72; raising r (the point's radius) to that power is what overflows a float, which forces the low Bailout near 2. A published Mandelbulber example for this exact formula uses a higher bailout, but Parsec diverges deliberately for the overflow reason above; that divergence is a sound, documented choice rather than a community canonical value.",
        },
        BestResults = new[]
        {
            "Animate Fold offset A and B (sweep the full +/- pi) for a morphing cellular/coral surface, that is what the formula is built to show off. Keep Bailout near 2 and Power clamp at 36; raising either invites overflow artifacts rather than more detail. If the surface looks faceted, lower DE fudge toward 0.4 before raising Iterations.",
        },
        SettingNotes = new Dictionary<string, string>
        {
            ["Scale"] = "Sphere-projection radius. Small changes restructure a lot, so the range is intentionally narrow. About 1.0.",
            ["Fold offset A"] = "First sine-fold phase. The primary animation target; sweep the full +/- pi.",
            ["Fold offset B"] = "Second sine-fold phase. A different default from A breaks symmetry for a livelier look.",
            ["Julia (0/1)"] = "Mode toggle. 0 for the set view; 1 uses the fixed Julia constant.",
            ["Julia C x"] = "X of the Julia constant. Only meaningful when Julia is on; keep small.",
            ["Julia C y"] = "Y of the Julia constant.",
            ["Julia C z"] = "Z of the Julia constant.",
            ["Rot X"] = "Cosmetic pre-rotation. 0 default.",
            ["Rot Y"] = "Second cosmetic rotation axis.",
            ["Rot Z"] = "Third cosmetic rotation axis.",
            ["Power clamp"] = "Caps the variable exponent. Keep high (36); the ceiling is the overflow-safe maximum, do not raise.",
            ["Iterations"] = "Escape-test iteration cap. 12 to 40 typical.",
            ["Bailout"] = "Escape radius. Intentionally low (about 2) to avoid float overflow from the doubled exponent.",
            ["DE fudge"] = "Step safety factor. Lower toward 0.4 first if the surface looks faceted.",
            ["Bound radius"] = "Fast-skip sphere. The set lives near r <= 1, so about 3 is right.",
        },
    };

    private static GuideContent Mandalay => new()
    {
        Title = "Mandalay Fold",
        WhatItIs = new[]
        {
            "This fractal belongs to the Mandelbox family. A Mandelbox is built from 'folds': operations that take a point in 3D space and reflect or push it back whenever it strays past some boundary, the way you would fold a sheet of paper along a crease. Folding alone is not a fractal; it just rearranges space once. The fractal appears when you fold, then scale (shrink or flip the whole space), then add a fixed offset, and repeat that combination over and over. Each pass folds the already-folded result, and the endlessly repeated creasing carves out infinitely fine self-similar detail.",
            "The specific fold here is called 'Mandalay', a transform shared on the Fractal Forums community by a user known as darkbeam, in the Amazing-Box / Amazing-Surf branch of the Mandelbox family. What makes the Mandalay fold distinctive is its per-axis cascade: for each of the x, y, and z axes it first folds all of space into the positive octant (the corner where every coordinate is positive), then runs a short sequence of conditional coordinate swaps and min/max comparisons against offset planes. 'min' and 'max' here are the building blocks of constructive solid geometry in a distance field: taking the minimum of two shapes' distances unions them, taking the maximum intersects them. That swap-and-compare cascade is what carves the characteristic cross or beam base shape before the fractal repetition multiplies it.",
            "The escape-time scaffold wrapped around the fold is the standard Mandelbox / KIFS recurrence z = scale*fold(z) + c. A negative Scale near -2 is what produces the rich, bounded, detailed set; positive scales mostly fail to bound the shape and leave little to see.",
        },
        HowComputed = new[]
        {
            "Each pass through the loop starts by recording the sign of every component of z (whether each of x, y, z is positive or negative) and then taking the absolute value of z, which collapses the point into the all-positive octant. With everything positive, the Mandalay axis fold is applied to each of the three axes in turn: a conditional swap of the two off-axis coordinates, followed by a small chain of max() and min() operations against the offset planes set by Fold offset, Offset g, and Offset h. After folding, the recorded signs are reapplied so the point returns to its original octant.",
            "Then comes the fractal step proper: z = scale*z + c. Scale shrinks or flips space (negative values flip it, which is geometrically a 180-degree rotation) and c re-anchors it. In lockstep the loop updates the running derivative with dr = abs(scale)*dr + 1; as the shared primer explains, this derivative is what lets a distance estimator know how much the repeated rule has magnified space near the point.",
            "There are two ways to feed the three per-axis folds, chosen by the Sequential toggle. In parallel mode (0) each axis fold reads from the original point captured at the start of the pass, so the three folds are independent. In sequential mode (1) each axis fold reads from the running, already-partly-folded point, so they chain together for a different look.",
            "When z escapes past Bailout, the distance estimate is simply the length of z divided by the derivative: DE = length(z)/dr. This works because the Mandalay fold is nearly distance-preserving (it barely stretches space, measured at roughly 0.98 on average). The catch is the fold seams, where it can expand space by about 1.7 times; to keep the ray marcher from overstepping at those seams, DE fudge defaults low, around 0.55.",
        },
        Math = new[]
        {
            "'Mandalay' is a named fractal type introduced by darkbeam on the Fractal Forums, in the Mandelbox / conditional-fold family. The surrounding escape-time scaffold is the standard Mandelbox / KIFS recurrence z -> scale*fold(z) + c. For any rule of this scale-and-fold shape the analytic scalar distance estimate is DE = |z| / dr, where the derivative is carried forward as dr -> |scale|*dr + 1 (start dr at 1). The |scale| factor is why a marcher built on this estimate behaves predictably even when scale is negative: flipping space does not change how much it magnifies.",
            "For this fold family the Scale value sets the character of the result. Around -1.5 gives tight, spiky detail; around -2.0 gives the classic 'boxed' look; around -2.5 opens into a more organic, porous structure. The fo / g / h offset semantics of the Mandalay axis fold are specific to darkbeam's fold and Parsec's port of it, and are not separately documented as canonical community constants, so treat their exact values as exploratory rather than reference settings.",
        },
        BestResults = new[]
        {
            "Keep Scale negative (-1.5 to -2.5): -2.0 for the classic boxed look, toward -2.5 for organic/porous, toward -1.5 for tight spiky detail. Positive scales rarely give a bounded set for this fold, so the positive half of the range is mostly a dead zone. Sweep Fold offset as the primary shape morph; nudge g and h off zero to open the beam/cross structure. If you see sparkle along the folds, lower DE fudge before raising Iterations.",
        },
        SettingNotes = new Dictionary<string, string>
        {
            ["Scale"] = "Per-iteration scale. Stay negative (-1.5 to -2.5) for the rich bounded set; positive scales are mostly a dead zone.",
            ["Fold offset"] = "The main shape control. About 0.5 is the canonical cross.",
            ["Offset g"] = "Secondary fold offset. 0 is the canonical look; opens the beam/cross structure.",
            ["Offset h"] = "Secondary fold offset.",
            ["Sequential (0/1)"] = "Fold mode toggle. 0 (parallel) feeds each axis from the original point; 1 feeds the running one.",
            ["Julia (0/1)"] = "Mode toggle. 0 for the set.",
            ["Julia C x"] = "X of the Julia constant. Only meaningful when Julia is on; keep small.",
            ["Julia C y"] = "Y of the Julia constant.",
            ["Julia C z"] = "Z of the Julia constant.",
            ["Iterations"] = "Escape-test iteration cap. 8 to 20 typical.",
            ["Bailout"] = "Escape radius. About 6 to 10 for this fold family.",
            ["DE fudge"] = "Step safety factor. About 0.55; the fold expands at seams, so keep below 1. Lower if you see sparkle.",
            ["Bound radius"] = "Fast-skip sphere. About 6.",
        },
    };

    private static GuideContent Anisotropic => new()
    {
        Title = "Anisotropic Fold",
        WhatItIs = new[]
        {
            "This is another Mandelbox-family fold fractal, but it exists to demonstrate one specific problem and its fix. 'Anisotropic' means 'different in different directions' (the opposite of isotropic, which means the same everywhere). In the plain Mandelbox the scaling step multiplies the whole space by a single number, so space grows by the same factor in every direction, a so-called similarity transform. Here the scaling step is replaced by a linear map that stretches space by different amounts along different axes and also shears it (slants the axes so they no longer meet at right angles, like pushing a stack of cards sideways). That non-uniform, slanted stretch is the anisotropy.",
            "The reason that matters is distance estimation. The shared primer explains that most of these fractals carry a single running number, the scalar derivative, to track how much the rule magnifies space, and the ray marcher divides by it to know how big a safe step is. That trick silently assumes space is stretched equally in every direction. When the stretch is anisotropic, one direction is magnified more than another, and a single number cannot capture both. Using the scalar derivative anyway overestimates the safe distance along the most-stretched direction, so the marcher oversteps and punches holes in the surface.",
            "Parsec's fix, and the whole point of this chapter, is to estimate the distance numerically instead. Rather than trusting one carried number, it actually measures how the orbit moves when you nudge the starting point a tiny bit in each of the three directions, and assembles those measurements into a small grid of numbers (a Jacobian matrix) that captures the full directional stretch. The stretch and shear knobs are the entire reason this more expensive method, called delta-DE, is needed.",
        },
        HowComputed = new[]
        {
            "The per-iteration rule is z = M*boxFold(z) + c. The box fold is the standard Mandelbox fold with no sphere fold, written boxFold(z, L) = clamp(z, -L, L)*2 - z, where L is the Fold limit; 'clamp' pins each coordinate inside the band from -L to +L, and the formula reflects anything outside that band back in. M is the anisotropic linear map: it combines a per-axis stretch (Stretch X, Y, Z, each multiplied by the base Scale) with two shear rotations (Shear Z and Shear Y). Because the three stretch factors differ and the shears slant the axes, M magnifies space by different amounts in different directions, which is exactly the situation a single scalar derivative cannot describe.",
            "So instead of carrying a scalar derivative, Parsec runs the orbit several times. First it runs the base orbit from the actual point p and notes the iteration N at which it escapes. Then it runs three more orbits, each starting from p nudged by a tiny amount eps along one of the x, y, z axes, for exactly the same N iterations. By comparing where each nudged orbit ended up against the base orbit, it measures how a small change in the start spreads out by the end. Those three comparisons fill in a 3x3 grid of rates of change called the Jacobian, J = dz/dp, the proper multi-directional replacement for the scalar derivative.",
            "The distance estimate is then DE = length(z) / norm(J), the size of the escaped point divided by how much the Jacobian magnifies space. There are two ways to measure that magnification, chosen by the Norm toggle. The Frobenius norm (mode 0) is the square root of the sum of all the matrix entries squared; it is always at least as large as the true worst-case stretch, so dividing by it can only make the step too small, never too big. That guarantees no holes but is a little conservative. The largest singular value (mode 1) is the exact worst-case stretch, computed by a short iterative method; it gives crisper edges but can occasionally sparkle at seams. Either way, running four orbits per pixel instead of one makes this roughly four times the cost of a scalar core, which is why Iterations should stay modest.",
        },
        Math = new[]
        {
            "The base map is the standard Mandelbox box fold, clamp(z, -1, 1)*2 - z with no sphere fold, but under a general linear transform M instead of a single scalar scale. The per-axis stretch plus shear makes M a non-similarity transform: it does not magnify all directions equally, so the usual analytic scalar distance estimate does not apply. The general fallback in that situation is the numerical-Jacobian (delta-DE) technique: run the orbit at the point p and at slightly offset points p + eps, finite-difference the results into the Jacobian J, and use DE = |z| / ||J||. This is the standard estimator used whenever no closed-form distance estimate exists. Choosing the Frobenius norm as the divisor is a deliberately safe over-estimate, because ||J||_F is always greater than or equal to the true operator norm sigma_max, so the marcher can never overshoot.",
            "The specific anisotropic-fold parametrization here (the exact stretch and shear knobs) is Parsec-original, which is the named purpose of this chapter, so there are no community canonical stretch/shear values to match; treat the audited guidance as being about the delta-DE behavior, not a published reference image. What is verified is the technique and the norm inequality, not a particular 'correct' configuration.",
        },
        BestResults = new[]
        {
            "Keep the stretch values unequal and at least one shear non-zero; that is where the anisotropy (and the reason for delta-DE) shows. Equal stretch (1,1,1) with zero shear collapses this to an ordinary scalar-DE-able fold, which makes the delta-DE pointless. Leave Norm on Frobenius for guaranteed hole-free renders; switch to sigma_max only when you want crisper edges and can tolerate occasional seam sparkle. Keep Iterations modest (about 10) since each iteration runs four orbits.",
        },
        SettingNotes = new Dictionary<string, string>
        {
            ["Scale"] = "Base similarity scale. The box-fold (no sphere fold) is stable at positive scale, unlike Mandalay; about 2 works.",
            ["Fold limit"] = "The Mandelbox standard fold limit. 1.0 canonical.",
            ["Stretch X"] = "Per-axis stretch on X. Unequal X/Y/Z is the anisotropy; equal collapses to a scalar fold.",
            ["Stretch Y"] = "Per-axis stretch on Y.",
            ["Stretch Z"] = "Per-axis stretch on Z.",
            ["Shear Z"] = "Shear angle (stored in radians, shown in degrees). Non-zero gives the lean that needs delta-DE.",
            ["Shear Y"] = "Second shear angle. 0 default.",
            ["Norm: Frob/sigma (0/1)"] = "Jacobian norm choice. 0 = Frobenius (safe, hole-free); 1 = largest singular value (crisper, can sparkle).",
            ["Julia (0/1)"] = "Mode toggle. 0 for the set.",
            ["Julia C x"] = "X of the Julia constant. Only meaningful when Julia is on; keep small.",
            ["Julia C y"] = "Y of the Julia constant.",
            ["Julia C z"] = "Z of the Julia constant.",
            ["Iterations"] = "Escape-test iteration cap. Keep modest (about 10); each iteration runs four orbits.",
            ["Bailout"] = "Escape radius. About 8 to 15.",
            ["DE fudge"] = "Step safety factor. About 0.9 absorbs the finite-difference noise at seams.",
            ["Bound radius"] = "Fast-skip sphere. About 5.",
        },
    };

    private static GuideContent Hybrid => new()
    {
        Title = "Hybrid (box + bulb)",
        WhatItIs = new[]
        {
            "A hybrid fractal runs two different fractal rules back to back inside the same loop, so the final shape inherits features of both. This one combines the two most famous distance-estimated 3D fractals. The first is the Mandelbox, built from folds and scaling as described for the Mandalay entry above; here it uses the textbook ingredients, a rotated box fold, a sphere fold, and a scale step. The second is the Mandelbulb, which raises a 3D point to a power the way the 2D Mandelbrot squares a complex number, spinning the point's angles and stretching its radius to create smooth, many-lobed shells.",
            "Every pass through the loop does three things in order: a small rotation, then the Mandelbox half, then the Mandelbulb half. The rotation is tiny, but it is applied again every pass, so it compounds: after ten iterations the space has been twisted ten times over. That compounding rotation is the morph knob, and because all three rotation angles are smooth scalars it makes this fractal an especially good animation target. Fusing a box fold with a bulb power per iteration is a well-established practice in fractal renderers like Mandelbulb 3D and Mandelbulber.",
            "There is an honest caveat. The Mandelbox and the Mandelbulb each have their own proven distance estimate, but no one has proven a correct distance estimate for the two combined this way. Parsec uses a heuristic combination of the two component estimates with an extra safety factor of 0.5 to keep the ray marcher conservative. The combination is well behaved in most settings, but some parameter regions will show artifacts; that is expected and normal for hybrid formulas, not a bug.",
        },
        HowComputed = new[]
        {
            "Each iteration begins by rotating z with a compounding rotation built from the three Rotate angles; because the same rotation is reapplied every pass, its effect accumulates across iterations. Then the Mandelbox half runs: a box fold, clamp(z, -foldLim, foldLim)*2 - z, reflects coordinates back inside the Fold limit band; a sphere fold uses Min radius and Fixed radius to push points outward from a small inner sphere onto a fixed shell; then z = z*scale + p applies the Scale and re-anchors the point. The running derivative is updated as dr = dr*abs(scale) + 1, exactly as in the plain Mandelbox.",
            "Immediately afterward the Mandelbulb half runs on the same z: the spherical power map raises the point to the chosen Power, which scales its radius by r^power and multiplies its angles by power, then adds p back. The derivative is updated again with the Mandelbulb rule dr = power*r^(power-1)*dr + 1, so both halves contribute to the single carried derivative.",
            "After the loop escapes (Bailout is fixed at 8 internally), the distance estimate uses the Mandelbulb-style form, 0.5*log(r)*r/dr, multiplied by an additional 0.5 safety factor, so the marcher steps conservatively where the combined estimate cannot be trusted. Note that Bailout and Bound radius are hardcoded for this fractal and are not exposed as sliders, unlike the other four in this group; do not go looking for a Bailout control that is not there. The CPU fly-camera path and the GPU render path use matching constants, so navigation speed and the final image agree. If holes or noise appear, the right response is to lower DE fudge and reduce Iterations, not raise them.",
        },
        Math = new[]
        {
            "This is a literal composition of two well-known distance-estimated fractals. The Mandelbox (Tom Lowe, 2010) is box fold + sphere fold + scale, with the standard ingredients Fold limit = 1, Min radius = 0.5, Fixed radius = 1, and a Scale near -1.5 to -2 for the interesting bounded sets; the negative scale geometrically flips space by 180 degrees each pass. The Mandelbulb (Daniel White and Paul Nylander, 2009) is the spherical nth-power map z -> z^n + c, which scales the radius by r^n and multiplies all angles by n; power 8 is the iconic, heavily lobed value and power 2 is the simpler so-called IQ-bulb.",
            "The per-iteration order (rotate -> box -> bulb) and the extra 0.5 safety factor are Parsec's heuristic choices. There is no canonical, proven distance estimate for this exact box-plus-bulb combination; it is a documented heuristic, accurate enough to render cleanly across most of the parameter space and expected to show occasional artifacts elsewhere. That is the standard trade-off for hybrid formulas, which is why the safety factor is built in and why DE fudge should stay below 1 in tricky regimes.",
        },
        BestResults = new[]
        {
            "Set Power to 8 for the classic many-lobed Mandelbulb look fused with the box detail; drop toward 2 for a smoother, blobbier hybrid. Keep Scale in -1.5 to -2.0 and the box radii at the canonical 0.5 / 1.0 / 1.0. Animate the three Rotate knobs (small angles) for a continuous morph, this fractal's standout feature. If holes or noise appear, lower DE fudge toward 0.4 and reduce Iterations rather than increasing them.",
        },
        SettingNotes = new Dictionary<string, string>
        {
            ["Power"] = "Exponent of the Mandelbulb half. 2 is the simple IQ-bulb; 8 is the iconic many-lobed look.",
            ["Rotate X"] = "Compounding per-iteration rotation. Small angles are correct; the standout morph knob.",
            ["Rotate Y"] = "Second compounding rotation axis.",
            ["Rotate Z"] = "Third compounding rotation axis.",
            ["Scale"] = "Negative-only Mandelbox scale. -1.5 to -2.0 is the sweet spot.",
            ["Min radius"] = "Inner sphere-fold radius. 0.5 canonical.",
            ["Fixed radius"] = "Outer sphere-fold shell radius. 1.0 canonical.",
            ["Fold limit"] = "Half-width of the box fold. 1.0 canonical.",
            ["Iterations"] = "Escape-test iteration cap. 6 to 12; the hybrid estimate gets noisy at very high counts.",
            ["DE fudge"] = "Step safety factor. The hybrid estimate over-estimates, so keep below 1 (0.4 to 0.7) in tricky regimes.",
        },
    };

    private static GuideContent Attractor => new()
    {
        Title = "Thomas Attractor",
        WhatItIs = new[]
        {
            "This object is made differently from every other fractal in this group, exactly the kind of exception the shared primer warns about. The other four ask, for each point in space, whether a repeated rule makes that point escape, and they color the result accordingly. A strange attractor is not built from an escape test at all. It is a single path traced through space over time. You start one particle somewhere, push it along according to a rule of motion, and follow where it goes. For certain rules the particle never settles into a fixed spot or a simple repeating loop; instead it wanders forever along an intricate, never-repeating curve that fills out a definite, self-similar shape. That shape is the strange attractor, and it earns the word 'fractal' from the structure of the traced curve rather than from an escape boundary.",
            "The particular system here is the Thomas cyclically symmetric attractor, named after Rene Thomas. A useful physical picture is a tiny ball moving through a 3D egg-carton landscape of sinusoidal hills and valleys, with a bit of friction (damping) slowing it down. The sinusoidal forces keep nudging it, the friction keeps draining energy, and the balance between them is what produces the looping, chaotic wander. 'Cyclically symmetric' means the rule treats x, y, and z identically, just cycling x -> y -> z -> x, so the resulting shape looks the same viewed down any of the three axes.",
            "Because the shape is a traced path, Parsec computes it once and renders the result as a glowing tube following the curve. That one-time computation is expensive (it integrates hundreds of thousands of steps), so the controls split into two kinds: generation params that change the path itself and only take effect when you press Generate, and live params that only affect the tube's appearance and update immediately. For the same reason, animation is disabled for this fractal: every frame would have to regenerate the whole path from scratch, so it is regenerated on demand, not smoothly tweened.",
        },
        HowComputed = new[]
        {
            "The rule of motion is a system of three differential equations, the canonical Thomas derivative, giving the particle's velocity at any instant from its current position. With all the optional perturbations off, the velocity is (sin(y) - b*x, sin(z) - b*y, sin(x) - b*z): in each axis a sinusoidal push from the next axis around the cycle, minus a friction term b times the current coordinate. To turn velocity into an actual path, Parsec advances the particle in small time steps using RK4 (the fourth-order Runge-Kutta method, a standard, accurate recipe for stepping a differential equation forward in time). It runs a 1000-step burn-in first and throws those away, because a freshly placed particle takes a while to settle onto the attractor; only the settled trajectory is kept.",
            "Four optional perturbation channels can deform the orbit away from canonical Thomas, all defaulting to zero (off): parameter drift (slowly varies the friction b per axis), phase modulation (lets the particle's position bend the sinusoidal pushes), a multi-seed phase shift (traces several seed particles instead of one), and nonlinear coupling (adds small quadratic cross-terms between axes). At zero they all vanish cleanly and you get the pure Thomas attractor, the clean logo shape.",
            "Once the path is computed it is stored as a long polyline (a chain of tiny straight segments). To render it as a tube, the ray marcher needs a distance estimate: at any point in space, the distance to the surface of the tube is the distance to the nearest trajectory segment minus the Tube radius. Testing every one of hundreds of thousands of segments for every step would be far too slow, so Parsec builds a uniform spatial hash, a 3D grid of buckets that records which segments pass through which cells. For any query point it only tests the segments in the immediate 3x3x3 neighborhood of cells, with a safety clamp that never reports a distance larger than half that neighborhood so the marcher cannot skip past a segment that lives just outside the searched cells.",
        },
        Math = new[]
        {
            "Thomas' cyclically symmetric attractor is the system dx/dt = sin(y) - b*x, dy/dt = sin(z) - b*y, dz/dt = sin(x) - b*z, cyclic in x -> y -> z -> x. The notation dx/dt means 'the rate of change of x over time', that is, the x-component of the particle's velocity. The single parameter b is the dissipation, or friction, and it acts as the bifurcation parameter: changing b alone switches the system between completely different long-term behaviors. The verified cascade is, for large b above 1 the particle just sinks to the origin; at b = 1 two attracting fixed points split off; near b = 0.32899 a stable limit cycle forms (a periodic loop, structured but not yet chaotic); near b = 0.208186 a period-doubling cascade reaches the onset of chaos; below b = 0.208186 the motion is a genuine chaotic strange attractor whose fractal dimension climbs toward 3 as b drops; and at b = 0 friction is gone entirely and the particle wanders all of space (so-called Labyrinth Chaos).",
            "So the chaotic, strange-attractor regime is roughly 0 < b < 0.208186, and the band 0.208186 < b < 0.329 is a structured limit cycle rather than the full attractor. The community-classic chaotic value is b = 0.19, sitting comfortably inside the chaotic range, which is why it is the recommended setting for the unmistakable strange-attractor look. The fixed RK4 time step (0.05 internally) is not exposed as a control: it is not an artistic choice, just a numerically safe step size for this smooth system, so there is no slider for it.",
        },
        BestResults = new[]
        {
            "For the unmistakable strange-attractor look, set Damping b to about 0.19 (anywhere 0.10 to 0.20) before Generate; staying below about 0.208 keeps it chaotic, while higher values drift into a limit cycle. Lower b (toward 0.10) thickens and fills the attractor. Use 150k to 300k steps for a dense, continuous tube, and thin tubes (radius about 0.04) read the structure better than fat ones. Keep perturbations at 0 for the canonical logo; introduce them in small amounts for organic deformation. Remember to press Generate after changing any generation param.",
        },
        SettingNotes = new Dictionary<string, string>
        {
            ["Damping b"] = "Generation param: the bifurcation parameter. About 0.19 is solidly chaotic; above about 0.208 it becomes a non-chaotic limit cycle. Takes effect on Generate.",
            ["Steps (x1000)"] = "Generation param: trajectory length and tube density, in thousands of integration steps. 150 to 300 for a dense tube.",
            ["Param variation"] = "Generation param: parameter-drift perturbation strength. 0 is canonical Thomas.",
            ["Phase mod"] = "Generation param: position-driven phase-modulation strength. 0 is canonical.",
            ["Coupling"] = "Generation param: quadratic cross-term strength. 0 is canonical; tiny values deform organically.",
            ["Drift phase"] = "Generation param: a reproducible explore knob; sweep 0 to 2*pi and Generate to find variants.",
            ["Multi-seed (0/1)"] = "Generation param: integrate multiple seed orbits instead of one.",
            ["Seed count"] = "Generation param: how many seed orbits, used only when multi-seed is on.",
            ["Tube radius"] = "Live param: thickness of the rendered tube. Updates immediately; about 0.04 reads structure best.",
            ["DE fudge"] = "Live param: tube-marcher step safety. Updates immediately; about 0.45.",
        },
    };
}
