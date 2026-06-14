using System.Collections.Generic;

namespace Parsec.App;

// Guide prose for the KIFS / Kleinian family: Amazing IFS, Pseudo-Kleinian,
// Pseudo-Kleinian 4D, and the Orbit Hybrid. Rewritten for a reader who knows
// nothing about fractals, building on the shared primer at the top of every
// guide (it already explains iteration/orbits, distance estimators and
// raymarching, the box fold and the sphere fold, and orbit-trap coloring). Here
// we only explain what is specific to THIS family. BestResults and SettingNotes
// are copied verbatim from the committed research and the prior Registry text.
public static partial class FractalGuide
{
    private static GuideContent Kifs => new()
    {
        Title = "Amazing IFS",
        WhatItIs = new[]
        {
            "This is the 'Kaleidoscopic IFS' shape, often called the Amazing Surface, invented by a fractal hobbyist known as Knighty. The name 'IFS' stands for Iterated Function System, which just means 'a small list of geometric moves that you apply over and over.' The word 'kaleidoscopic' is the key idea: a real kaleidoscope uses angled mirrors to reflect one small pattern into a symmetric, repeating flower. This shape does exactly that in 3D. Each step reflects space across mirror walls, spins it, shrinks it, and turns it inside out through a sphere, and because you repeat the whole list many times, those reflections multiply into the lacy, spiraling, shell-like surface you see.",
            "It is worth being precise about one confusing point. A classic 'IFS' converges by shrinking, but this shape is actually run as an escape-time system, the same machinery used for the Mandelbulb and Mandelbox described in the primer. You take a point in space, push it through the list of moves again and again, and watch how fast it flies away to infinity. Points that stay trapped near the center for many steps lie on or inside the surface; points that escape quickly are outside. The surface is the boundary between trapped and escaping points.",
            "When you turn the rotations off and pick the standard scale and pivot, the moves collapse into famous rigid shapes: the Sierpinski tetrahedron (a pyramid endlessly subdivided into four smaller pyramids) or the Menger sponge (a cube riddled with square holes at every scale). Turning the rotations back on is what bends those hard polyhedral edges into the smooth logarithmic spirals (a spiral whose arms widen by the same ratio each turn) and flaring bell and trumpet horns that give the 'amazing' family its name.",
        },
        HowComputed = new[]
        {
            "Start with the 3D point you are testing, call it z = (x, y, z). One iteration applies four moves in a fixed order, and the renderer repeats that iteration up to the Iterations count.",
            "Move 1, the pre-rotation: rotate z by the Pre-rot X, Y, Z angles. By default these are zero, so nothing happens, but a small angle here tilts space before it hits the mirror so the later reflection lands at a new angle. Move 2, the plane fold: replace every coordinate by its absolute value, z = abs(z) = (|x|, |y|, |z|). Geometrically a fold is a mirror reflection: |x| means 'if x is negative, flip it positive,' which reflects anything in the negative half of space across the x=0 wall into the positive half. Doing this on all three axes packs all of space into the positive corner (the positive octant), exactly like the angled mirrors of a kaleidoscope. Move 3, the post-rotation: rotate z again by the Post-rot X, Y, Z angles. This is the most expressive move, because it spins the freshly folded wedge before the next fold reflects it, and that mismatch between fold walls and rotation is what curls straight edges into spirals.",
            "Move 4 has two parts. First a sphere fold (the inversion described in the primer): if the point sits inside an inner ball of radius Min radius it is pushed outward by a fixed factor, and if it sits in the shell between Min radius and Fixed radius it is inverted through the sphere, meaning points near the center are flung far out and points far out are pulled in, which everts flat sheets into bell and trumpet flares. Then a scale-toward-pivot: z = scale*z - (scale-1)*pivot. Plainly, this multiplies the point's distance from the Pivot point by 'scale' (with scale near 2 it doubles the spread), and the term (scale-1)*pivot is just the bookkeeping that keeps the Pivot itself fixed while everything else spreads out from it. This repeated magnification is what reveals finer and finer copies of the structure.",
            "Alongside the point, the renderer carries a single number called the running derivative dr, updated each step by dr = dr*|scale| + 1. This tracks how much the moves have stretched space so far, so the final distance estimate can be read off as DE = length(z) / abs(dr): the point's distance from the origin, divided by the total stretch. That is the standard analytic distance estimator for the fold family, and it tells the raymarcher how far it can safely step before it might hit the surface.",
        },
        Math = new[]
        {
            "First, vocabulary used below. 'IFS' (Iterated Function System) means a fixed short list of geometric maps applied repeatedly. A 'fold' is a conditional reflection across a flat wall; abs(x) is the fold across the wall x=0, sending the negative side onto the positive side. A 'rotation' spins space rigidly about an axis without changing any distances. A 'sphere inversion' (the sphere fold) turns the region near a center inside out: a point at distance r from the center moves to distance R^2/r, so small r becomes large and large r becomes small, with R the sphere radius. These four kinds of map (scaling, translation, plane reflection, rotation) plus sphere inversion are the only shape-preserving moves Knighty uses, which is why the result stays clean and self-similar.",
            "The base escape-time iteration without rotation is: z = abs(z); then z = scale*z - offset*(scale-1). Here 'scale' is the per-step magnification (the canonical value is 2), 'offset' is the pivot point the scaling pushes away from, and the canonical Menger and Sierpinski value is offset near (1,1,1). With rotation the full step becomes: rotate (pre), fold z = abs(z), rotate (post), apply the sphere fold, then z = scale*z - (scale-1)*pivot, and update dr = dr*|scale| + 1. The distance estimate is DE = length(z) / abs(dr), equivalently length(z) * scale^(-i) after i steps, since each step multiplies the stretch by about |scale|. The pre and post rotations are the upgrade that turns the rigid polyhedral fractal into the smooth spiral 'amazing' forms.",
        },
        BestResults = new[]
        {
            "Start from the default, then sweep Post-rot Z first, it has the strongest effect on the scrollwork. Keep Scale near 2.0 and Pivot near (1,1,1) for clean recognizable structure; push Pivot off-axis for organic asymmetry. Keep Min radius below Fixed radius so the sphere-fold shell exists. Raise Iterations to 30 to 60 only when you zoom in; 16 is enough for the silhouette and keeps the frame fast.",
        },
        SettingNotes = new Dictionary<string, string>
        {
            ["Scale"] = "Per-iteration similarity scale. 2.0 is the Sierpinski/Menger base; 1.5 to 2.6 is most varied.",
            ["Min radius"] = "Inner sphere-fold radius. Keep below Fixed radius or the inversion shell vanishes.",
            ["Fixed radius"] = "Sphere-fold shell radius. 1.0 is standard.",
            ["Post-rot X"] = "Rotation after the fold. These are the curl generators, the most expressive knobs.",
            ["Post-rot Y"] = "Second post-fold rotation axis.",
            ["Post-rot Z"] = "Third post-fold rotation axis; sweep this first for the strongest scrollwork change.",
            ["Pre-rot X"] = "Rotation before the fold. Off by default; small values add asymmetry.",
            ["Pre-rot Y"] = "Second pre-fold rotation axis.",
            ["Pre-rot Z"] = "Third pre-fold rotation axis.",
            ["Pivot X"] = "X of the scale-contraction center. (1,1,1) is canonical Menger/Sierpinski.",
            ["Pivot Y"] = "Y of the contraction center.",
            ["Pivot Z"] = "Z of the contraction center; push off-axis for organic asymmetry.",
            ["Iterations"] = "Escape-test iteration cap. 16 for the silhouette; 30 to 60 for zoom detail.",
            ["DE fudge"] = "Step shortener. Lower is safer and slower; 0.6 to 0.8 is a safe band.",
        },
    };

    private static GuideContent Kleinian => new()
    {
        Title = "Pseudo-Kleinian",
        WhatItIs = new[]
        {
            "This shape approximates the 'limit set of a Kleinian group.' Unpacking that: a Kleinian group is a collection of geometric moves (here, reflections in spheres) where applying any move, or any combination of moves, lands you back in the same family. If you take one starting point and apply every possible combination of those moves forever, the cloud of places it can end up settles onto a fractal dust or foam called the limit set. The everyday version of this idea is the Apollonian gasket: start with a few mutually touching circles, keep drawing the largest circle that fits snugly in each remaining gap, and you get an endless nest of shrinking tangent circles. This is the 3D version, so instead of nested circles you get nested, packed spheres tiling all of space, which is why it reads as a foam.",
            "Mechanically each iteration does three things: a sphere inversion (the sphere fold from the primer, which turns the region near a center inside out), a box fold (the wall reflection from the primer that reflects anything past a wall back inside a cell), and then a scale-and-shift that moves the cell over and magnifies. Repeating that endlessly is what packs the spheres ever tighter.",
            "One honesty flag worth stating plainly: unlike the Mandelbox and the Amazing IFS, this family has no clean exact formula for how far away the surface is. So the renderer cannot use the fast analytic distance estimate. Instead it measures the distance to the surface numerically, by sampling the field at several nearby points and comparing them. That is more faithful but noticeably more expensive per step.",
        },
        HowComputed = new[]
        {
            "Each iteration runs in three stages on the running point z. Stage 1, sphere inversion with an inner linear zone: if z is very close to the center (inside Min radius) it is simply blown up by a fixed factor, the 'linear zone'; if it is in the shell out to Fixed radius it is inverted through that sphere, so points near the center are thrown outward and outer points are pulled in. Stage 2, the box fold written as z = clamp(z, -cell, cell)*2 - z: 'clamp' pins each coordinate inside the range from -Cell size to +Cell size, and the surrounding arithmetic reflects anything outside that cell back across the cell wall, which is what tiles space into a repeating lattice of identical cells. Stage 3, scale and offset, z = z*scale + c: multiply the point's spread by Scale and then shift it by the Offset vector c. That Offset is the single most important creative dial, because it is the generator that decides which Kleinian limit set, which particular foam, you are looking at.",
            "After the iteration loop the renderer does not have a tidy derivative to divide by, so it builds a 'potential': a smooth number V = log(length(z)) that is large where the point escaped fast and small where it stayed trapped, so the surface sits at a level set of V (a contour where V holds a constant value), like a single elevation line on a topographic map.",
            "To turn that potential into a distance it estimates the gradient of V (the direction and steepness of fastest increase) numerically, by re-running the potential at six neighboring points (plus or minus a tiny step on each of the three axes) and taking central differences, which is the slope read from how V changes left-to-right, up-to-down, and front-to-back. The distance estimate is then |V| / |grad V|: how far the point's value is from the surface level, divided by how steeply the field is changing, which converts 'value difference' into 'physical distance.' Because each distance call needs the center sample plus six neighbors, that is seven full potential evaluations per step, so raising Iterations here costs roughly seven times what it would on an analytic core. The renderer accumulates orbit-trap colors in a separate re-run of the loop.",
        },
        Math = new[]
        {
            "Vocabulary: an 'inversion' through a sphere of radius R about a center sends a point at distance r to distance R^2/r along the same ray, so it turns the inside of the sphere out and the outside in; a 'box fold' reflects any coordinate that pokes past a wall back inside the cell; a 'limit set' is the fractal cloud of all places you can reach by applying the group's moves forever. The pseudo-Kleinian is built from the two conformal folds of the Mandelbox: a ball fold (the sphere inversion, schematically: if r2 < minR2 then z *= fixedR2/minR2; else if r2 < fixedR2 then z *= fixedR2/r2) and a box fold (clamp(z,-1,1)*2 - z), followed by the similarity z = scale*z + c.",
            "The attractor is the limit set of the inversive group these folds generate, a Kleinian / Apollonian sphere packing. The offset c is the generator that determines which limit set you get. Because the iterated inversive map has no convergent analytic derivative, distance is taken as the distance-to-level-set form DE = |log|z|| / |grad log|z||, where |z| is the length of the final point, log|z| is the smooth potential, and grad is its numerically estimated gradient. In words: how far the potential is from the surface contour, divided by how fast the potential changes, which is a physical distance.",
        },
        BestResults = new[]
        {
            "Treat Offset as the main creative dial; sweep Offset Z between 0.8 and 1.6 to morph the cell stacking. Keep Iterations 8 to 12 for interactive use; only raise it for stills, since each step costs seven potential evaluations. Keep DE fudge modest (at or below about 0.8): a numerical-gradient distance estimate is noisier than an analytic one and a high fudge punches through thin foam walls. If the surface looks melted, lower DE fudge toward 0.5 and nudge Min radius down.",
        },
        SettingNotes = new Dictionary<string, string>
        {
            ["Scale"] = "Per-iteration similarity scale. 2.0 is typical; 1.5 to 2.0 gives dense foam.",
            ["Cell size"] = "The box-fold clamp bound; sets the lattice cell.",
            ["Min radius"] = "Inner linear-zone radius of the inversion. 0.5 is canonical.",
            ["Fixed radius"] = "Inversion shell radius. 1.0 is canonical.",
            ["Offset X"] = "X of the tiling generator. The strongest morphology knob; defines the limit set.",
            ["Offset Y"] = "Y of the tiling generator.",
            ["Offset Z"] = "Z of the tiling generator. Sweep 0.8 to 1.6 to morph the cell stacking.",
            ["Iterations"] = "Iteration cap. Low by design (each step is 7 potential evals); keep 8 to 12 interactively.",
            ["DE fudge"] = "Step safety factor. Keep at or below 0.8; the numerical gradient needs margin.",
        },
    };

    private static GuideContent PseudoKleinian4D => new()
    {
        Title = "Pseudo-Kleinian 4D",
        WhatItIs = new[]
        {
            "This is the same idea as the Pseudo-Kleinian foam above, an approximation of a Kleinian group's limit set (the fractal dust you reach by reflecting a point through spheres forever), but lifted into four dimensions. You cannot see four dimensions directly, so the renderer takes a flat 3D slice through the 4D shape, fixed by the W slice value, the same way a single photograph is a 2D slice through a 3D scene. Sliding W moves the cut to a different cross-section, which is why animating W gives a flythrough of a slowly transforming structure. The look it produces is best described as alien half-space architecture: foamy nested cells and cathedral-like tiling that marches off to the horizon.",
            "There is an important honesty flag and it is good news here. Unlike the plain Pseudo-Kleinian, which had to measure distance numerically with seven samples per step, this 4D variant keeps an honest running derivative dr (a single number tracking how much space has been stretched) and ends with a known exact distance formula. So it renders cheaply, with no expensive numerical gradient. An optional sphere inversion can be switched on to bound the otherwise endless space-filling tiling neatly inside a ball, which both tidies the framing and lets the raymarcher skip empty space faster.",
        },
        HowComputed = new[]
        {
            "The point being tested is promoted to 4D as z = (x, y, z, w0), where the first three are the sample location and the fourth, w0, is the fixed W slice that selects which cross-section you see. Each iteration applies a sequence of moves while updating the running derivative dr alongside the point.",
            "First, optionally, a sphere inversion: z *= gInv / dot(z, z), where dot(z, z) is the squared length, so this divides the point by its own squared distance from the center, the 4D version of turning the region near the origin inside out; dr is scaled by the same factor to stay honest. Next a box offset, z.xyz -= cOff * sign(z.xyz): sign() is +1 or -1 per coordinate, so this nudges the point by a fixed amount in whichever direction it already points. That asymmetric nudge is the deliberate 'symmetry break,' the move that turns a boringly regular tiling into the characteristic lopsided Kleinian architecture. Then a clamp-form box fold, z = abs(z + cSize) - abs(z - cSize) - z: this is the wall-reflection box fold from the primer, written so that any coordinate poking past the half-size cSize gets folded back into the cell. Finally a one-sided spherical fold: k = max(sScale / dot(z,z), 1); z *= k; dr *= k + tweak. The max(..., 1) means the inversion only ever pushes points outward and never pulls them in (that is the 'one-sided' part), which keeps the structure from collapsing.",
            "After the loop the distance is read from the canonical pseudo-Kleinian identity, which describes the surface as a tube around the z-axis intersected with a thin slab at z=0. Concretely d1 is the distance to a tube of radius Tube radius about the z-axis (or, when DE form is set to 1, a quaternionic four-way minimum instead), and d2 = |z.z| is the distance to the flat z=0 slab. The final estimate is DE = 0.5*(min(d1, d2) - DE offset) / dr: take whichever surface is nearer, adjust by the DE offset thickness term, and divide by the accumulated stretch dr to convert it back to a real-world step size. Because dr was tracked honestly throughout, this needs no extra samples.",
        },
        Math = new[]
        {
            "This matches Mandelbulber's pseudo_kleinian / jos_kleinian construction. Vocabulary as before: a 'box offset' is a sign-dependent translation, z -= cOff*sign(z), that shoves the point by a fixed step in the direction it already leans, deliberately breaking the cell symmetry; a 'box fold' reflects coordinates back inside a cell of half-size cSize; a 'sphere inversion' turns the region near the origin inside out; the 'one-sided spherical fold' uses k = max(sScale/dot(z,z), 1) so it only ever pushes outward. An optional bounding sphere inversion confines the half-space tiling inside a ball.",
            "The final distance is the tube-and-slab identity DE = 0.5*(min(d1, d2) - DEoffset)/dr, where d1 is the distance to a tube about the z-axis (radius set by Tube radius), or a quaternionic min-of-four form when DE form is 1; d2 = |z.z| is the distance to the z=0 slab; DEoffset inflates or deflates the surface; and dr is the honest running derivative. The z.z derivative tweak (Parsec's DE tweak) is documented in Mandelbulber as a small additive number, normal use about 1.06 expressed as an additive, and Parsec's default 0.05 is that additive form. The W slice is the animatable 4th-dimension cut.",
        },
        BestResults = new[]
        {
            "Keep Inversion on with Bound radius about 8 for the ball-bounded view; if you switch it off, raise Bound radius toward 12 to 16 so the raw tiling fits. Bump one Offset axis to 0.5 to 0.8 for the true asymmetric Kleinian architecture (the default 0,0,0 shows the symmetric base tiling). Animate the W slice for a flythrough of a shifting cathedral. Add a touch of Tube radius (0.05 to 0.15) to thicken thin struts before a hero still, and keep DE fudge modest (at or below about 0.8).",
        },
        SettingNotes = new Dictionary<string, string>
        {
            ["Box size X"] = "X half-size of the box fold; sets the lattice cell. 1.0 standard.",
            ["Box size Y"] = "Y half-size of the box fold.",
            ["Box size Z"] = "Z half-size of the box fold.",
            ["Sphere fold"] = "One-sided spherical-fold scale (inversion strength). About 1.0.",
            ["Offset X"] = "X of the symmetry-break box offset. Bump to 0.5 to 0.8 for the asymmetric Kleinian look.",
            ["Offset Y"] = "Y of the symmetry-break offset.",
            ["Offset Z"] = "Z of the symmetry-break offset.",
            ["W slice"] = "The fixed 4th-dimension cut. The cheapest dramatic motion; great for animation.",
            ["Tube radius"] = "Thickens the struts. 0 gives thin filaments; 0.05 to 0.15 reads more solid.",
            ["DE offset"] = "Inflates or deflates the surface. Small values add thickness.",
            ["DE tweak"] = "Small additive derivative fudge. Keep small (about 0.05).",
            ["Inversion scale"] = "Strength of the bounding sphere inversion; only used when Inversion is on.",
            ["Inversion (0/1)"] = "Bounds the tiling into a ball. Off needs a larger Bound radius.",
            ["DE form (0/1)"] = "Selects the distance identity: 0 = tube, 1 = quaternionic min.",
            ["Iterations"] = "Iteration cap. Honest running derivative, so cheaper than the analytic Kleinian; 10 to 16.",
            ["DE fudge"] = "Step safety factor. 0.6 is safe for the tube/slab estimate; keep at or below 0.8.",
            ["Bound radius"] = "Fast-skip sphere. 8 with inversion on; raise toward 12 to 16 with inversion off.",
        },
    };

    private static GuideContent OrbitHybrid => new()
    {
        Title = "Orbit Hybrid (KIFS + Mandelbox)",
        WhatItIs = new[]
        {
            "This is an experiment in mixing two different fractal recipes into one. There are two honest ways to combine shapes. One is to render each shape separately and then glue the finished surfaces together with union or intersection (this is called CSG, constructive solid geometry, and it just stacks two existing objects). This fractal does NOT do that. Instead it does function composition: it interleaves the two recipes inside a single iteration loop, so the same running point and the same running derivative pass through a KIFS step, then a Mandelbox step, then back again, blending the two transforms into one orbit. Because the moves are woven together rather than computed apart, the result is a genuinely new shape that neither recipe makes alone.",
            "The schedule decides the weave. You set how many KIFS steps and how many Mandelbox steps make up one cycle, for example one KIFS step then two Mandelbox steps, and that pattern repeats for the whole iteration count. The reason for the specific KIFS-plus-Mandelbox pairing is technical but important: when you compose recipes in one orbit, at least one of them must include a fold that caps how large the point can grow, otherwise the point runs off to infinity on every pixel and you get nothing. The Mandelbox's box fold (a clamp that reflects the point back whenever it tries to leave the cell) provides that cap. The KIFS step's abs() fold reflects but does not cap magnitude, so on its own it cannot keep the orbit bounded. That is why the Mandelbox half is the anchor and should run at least as often as the KIFS half.",
        },
        HowComputed = new[]
        {
            "Each iteration first reads the schedule to pick which recipe runs this step: phase = i % cyc, where cyc = KIFS steps + Mandelbox steps and i is the step number; if phase is below the KIFS count it runs the KIFS step, otherwise the Mandelbox step. Both share the same running point z and the same running derivative dr.",
            "The KIFS step does abs(z) (the plane fold that reflects all of space into the positive corner), then an optional post-rotation by the Curl angles (this is the curl that lays spirals over the structure), then the shared sphere fold (the inversion that turns the region near the center inside out), then z = scale*z using the KIFS scale, and updates dr = dr*|scale| + 1. Note there is no pivot translation here, unlike the standalone Amazing IFS, and the abs() does NOT cap how big z can get.",
            "The Mandelbox step does the box fold, z = clamp(z, -L, L)*2 - z, where L is the Box fold limit; this is the magnitude cap that keeps the whole hybrid orbit bounded, because clamp pins the point inside the cell and the arithmetic reflects anything outside back in. Then the shared sphere fold, then z = scale*z + c using the Mandelbox scale, where c = p is the original sample point added back (the same 'add the starting point each step' trick that defines the Mandelbrot and Mandelbox families). It updates the shared dr the same way.",
            "After the loop, or as soon as the point escapes a bailout radius, the distance estimate is the familiar DE = length(z) / max(|dr|, eps), the point's distance from the origin over the accumulated stretch. By construction the running dr over-estimates the true stretch (by roughly 1.7 times per the in-file note), which makes the distance estimate slightly smaller than the true distance. That is the safe direction to err: an under-estimated distance means the raymarcher takes conservative steps and never tunnels through a thin wall, so the surface comes out hole-free.",
        },
        Math = new[]
        {
            "There is no single canonical orbit hybrid in the literature; it is Parsec's prototype of the multi-formula sequencer idea, apply formula A for N iterations, then formula B for M, in a repeating loop, the same generalization that Mandelbulber and Mandelbulb3D offer with their 'hybrid' formula stacks. The two halves are individually canonical. KIFS is Knighty's kaleidoscopic escape-time system at scale about 2. The Mandelbox (by Tom Lowe) is built from a box fold clamp(z,-1,1)*2 - z, a ball fold (if r2 < 0.25 then z *= 4; else if r2 < 1 then z /= r2), and a similarity z = scale*z + c.",
            "The single most important Mandelbox knob is its scale, and its most distinctive sets live at NEGATIVE scale: -1.5 gives dense, tight, spiky detail (just small enough to avoid stray floating corner boxes), -2.0 gives the classic boxed look, and -2.5 opens into an organic, porous form. The hybrid inherits this, so Mbox scale default -1.5 is the canonical rich negative-Mandelbox value. Because the Mandelbox box fold is the only magnitude-capping move in the weave, keeping Mandelbox steps at or above the KIFS count is what keeps the composed orbit bounded.",
        },
        BestResults = new[]
        {
            "Leave Mbox scale at -1.5 first; it carries the structure. Then try -2.0 (boxy) and -2.5 (porous). Keep Mandelbox steps at or above KIFS steps so the bounding box fold dominates and the orbit stays bounded, the whole reason this pairing was chosen. Add small Curl angles (10 to 30 degrees) for KIFS scrollwork over the Mandelbox cage; large curls can destabilize the shared orbit.",
        },
        SettingNotes = new Dictionary<string, string>
        {
            ["KIFS steps"] = "KIFS steps per schedule cycle. 1 to 2 typical.",
            ["Mandelbox steps"] = "Mandelbox steps per cycle. Should be at least the KIFS count, since it carries the magnitude cap.",
            ["Iterations"] = "Total iteration cap across the schedule.",
            ["KIFS scale"] = "KIFS similarity scale. 1.5 to 2.0.",
            ["Curl X"] = "Post-fold rotation on the KIFS step. Small values (0 to 30 degrees) add scrollwork.",
            ["Curl Y"] = "Second curl axis.",
            ["Curl Z"] = "Third curl axis.",
            ["Mbox scale"] = "Mandelbox scale. Negative is canonical: -1.5 the workhorse, -2.0 boxy, -2.5 porous.",
            ["Box fold limit"] = "The clamp bound that bounds the hybrid orbit. 1.0 canonical.",
            ["Min radius"] = "Shared sphere-fold inner radius. 0.5 canonical.",
            ["Fixed radius"] = "Shared sphere-fold shell radius. 1.0 canonical.",
            ["Bailout"] = "Loop escape radius. 30 is safe.",
            ["DE fudge"] = "Step safety factor. The derivative over-estimates, so 0.7 to 1.0 is fine.",
            ["Bound radius"] = "Fast-skip sphere. The engine honors the live value even where the slider caps lower.",
        },
    };
}
