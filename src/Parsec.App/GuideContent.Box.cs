using System.Collections.Generic;

namespace Parsec.App;

public static partial class FractalGuide
{
    private static GuideContent Mandelbox => new()
    {
        Title = "Mandelbox",
        WhatItIs = new[]
        {
            "The Mandelbox is a three-dimensional fractal invented by Tom Lowe in 2010. Its name is a nod to the Mandelbrot set: like that famous shape it is built by repeating one simple step over and over and asking which starting points stay trapped forever versus which ones fly off to infinity. The difference is that the Mandelbrot step is a single multiplication of complex numbers, while the Mandelbox step is a pair of geometric 'folds' of ordinary 3D space followed by a scale and a shift. Because every step is a fold rather than a smooth squashing, the boundary it carves out is full of hard edges, right angles, arches, ledges, and corridors, which is why people describe the classic Mandelbox as looking like an endless self-similar city built out of concrete blocks.",
            "A fold here means exactly what the primer described: a rule that reflects part of space across a wall, or turns a region near the center inside out, so that points which started far apart can be brought close together. The Mandelbox applies two of these in sequence. The first is the box fold, which reflects any coordinate that has wandered outside a cubic boundary back inward, as if the walls of a box bounced it. The second is the sphere fold, which leaves the outside of space alone but magnifies and inverts the region near the origin, pulling fine detail outward where you can see it. Run that pair thousands of times across a grid of starting points and the trapped region forms the Mandelbox surface.",
            "The Mandelbox is notable as the parent of an entire family. Every other fractal in this BOX section (AmazingBox, Rotated Mandelbox, Folded Menger) is a small change to this same fold-then-scale loop. Learning the Mandelbox loop in detail means you already understand most of the others. The single most important control is the scale number: at scale 2 you get the solid blocky cityscape, while negative scales near -1.5 and -2 hollow the object out into delicate organic shells that famously contain tiny copies of many other known fractals embedded on their surface.",
        },
        HowComputed = new[]
        {
            "The Mandelbox is drawn by the distance-estimated raymarching method from the primer: for each pixel a ray is shot into the scene, and at each point along it a distance estimator (DE) reports a safe distance to the nearest surface, so the ray can leap forward by that much without ever passing through the object. The Mandelbox DE works by running the fold loop on the sample point and watching how fast space is being stretched. Concretely Parsec carries two things through the loop: a 3D vector z (the point being folded, which starts at the sample location) and a single number dr called the running derivative, which keeps a running tally of how much the scaling steps have magnified space so far.",
            "Each iteration does four things in order. First the box fold: every component of z (its x, y, and z coordinates) that has crossed outside the folding limit L is reflected back, using the rule clamp(z, -L, L) * 2 - z. In plain terms, anything past the +L wall is mirrored to the same distance on the inside, and likewise past the -L wall, while points already inside the box are left untouched. Second, an optional Euler rotation is applied, but only if one of the rotation angles is non-zero; for the classic Mandelbox all three angles are zero and this step does nothing. Third the two-zone sphere fold: if z is very close to the origin (inside the min radius) it is blown up linearly by a fixed factor, magnifying the tiniest detail; if z is in the shell between the min radius and the fixed radius it is inverted through the sphere (divided by its squared length), which turns that ring of space inside out; points outside the fixed radius are left alone.",
            "Fourth and last, z is scaled and the original sample point is added back: z becomes scale * z + c, where c is the sample point that this whole orbit started from (this re-injection of c each step is exactly what makes it a Mandelbrot-style set rather than a single shrinking spiral). At the same moment the running derivative is updated as dr = dr * |scale| + 1, recording the new stretch. A bailout check stops the loop once z has grown past a large radius (when |z| squared exceeds 1000); this bailout matters, because without it the derivative inside the trapped region would blow up and the whole field would collapse into a featureless solid block.",
            "When the loop ends, the distance estimate is simply the length of z divided by the magnitude of the running derivative, DE = |z| / |dr|. Intuitively this says: the object is roughly as far away as the current point is from the origin, corrected by however much the folding has been magnifying space. Parsec runs this loop on the GPU for the image; an identical copy on the CPU is used only to keep the flying camera from crashing into the surface.",
        },
        Math = new[]
        {
            "Box fold, per component (treat each of x, y, z separately): if the component is greater than the folding limit L, replace it with 2L minus the component; if it is less than -L, replace it with -2L minus the component; otherwise leave it. Sphere fold, using the squared length of z: if z is inside the min radius, scale z up by a fixed factor; if z is inside the fixed radius, divide z by its squared length (an inversion); otherwise leave it. Scale-and-shift: z -> scale * z + c, and alongside it dr -> |scale| * dr + 1, where c is the constant sample point and dr is the running derivative used by the distance estimate.",
            "Every symbol from first principles: z is the moving 3D point, started at the sample location and folded each step. c is that same sample location held fixed and re-added every iteration. L (the folding limit) is the half-width of the box, so the box runs from -L to +L on each axis; the canonical value is 1. The min radius (canonical 0.5) and fixed radius (canonical 1) set the two zones of the sphere fold. scale is the per-iteration similarity factor; the standard set is scale 2, min radius 0.5, fixed radius 1, folding limit 1. Positive scales of 2 and 3 give solid cityscapes, while -1.5 and -2 give the famous hollow, organic forms.",
        },
        BestResults = new[]
        {
            "Start at the default scale 2 for the solid cityscape, push Scale up toward 3 for the brighter open positive-scale cityscape, then flip to -1.5 to explore the fractal-zoo surface (expect to zoom in for the embedded shapes) or -2 for the dense hollow look. Keep the rotations at 0 here: the distance estimate is exact, so the marcher can take near-full steps and you can raise DE fudge toward 1.0 for speed. Raise iterations only when zooming deep; 14 is fine for overview shots.",
        },
        SettingNotes = new Dictionary<string, string>
        {
            ["Scale"] = "The per-iteration similarity scale. 2 is the canonical cityscape; negatives near -1.5 and -2 give the hollow, organic forms.",
            ["Min radius"] = "Inner sphere-fold radius. Inside it the point is scaled up linearly. 0.5 is canonical.",
            ["Fixed radius"] = "Outer sphere-fold shell radius where inversion happens. 1.0 is canonical.",
            ["Folding limit"] = "Half-width of the box fold. Points outside the +/- limit are reflected back. 1.0 is canonical.",
            ["Rotate X"] = "Euler rotation applied each iteration before folding. Zero keeps the exact distance estimate; non-zero curls the structure.",
            ["Rotate Y"] = "Second rotation axis. Zero for the clean classic look.",
            ["Rotate Z"] = "Third rotation axis. Zero for the clean classic look.",
            ["Iterations"] = "Escape-test iteration cap. Raise only for deep zooms; the overview saturates early.",
            ["DE fudge"] = "Safety factor on the marching step. Raise toward 1.0 for speed when unrotated; lower if you see overstepping.",
        },
    };

    private static GuideContent AmazingBox => new()
    {
        Title = "AmazingBox",
        WhatItIs = new[]
        {
            "The AmazingBox is a close cousin of the Mandelbox that swaps in one different fold and, as a result, grows a completely different kind of surface. Where the plain Mandelbox box fold only reflects coordinates that have strayed outside the box, the AmazingBox first takes the absolute value of every coordinate before box folding. Taking the absolute value means flipping any negative coordinate to its positive twin, which mirrors all of space into a single corner (the positive octant, where x, y, and z are all positive). This extra mirror is the 'Amazing' fold, and it is what gives the family its name (in the wider fractal community these forms are called Amazing Surf or Amazing Box).",
            "The visible payoff of that extra mirror, when paired with a negative scale and a small rotation between folds, is a richly detailed curled surface rather than the Mandelbox's blocky city. The abs step folds the structure onto itself with eightfold symmetry, and the slight rotation each iteration twists that symmetry so it never sits perfectly flat, producing the rolling, frond-like 'surf' texture that dominates a great deal of fractal art. It is one of the most generative shapes in the whole app for that reason: tiny changes to the rotation angles morph the curl in dramatic ways.",
            "Under the hood the AmazingBox reuses every piece of the Mandelbox distance-estimate machinery; only the fold and the default rotation are different. That is worth stressing because it means the math you learned for the Mandelbox carries over almost unchanged. Note also that the scale here is deliberately restricted to negative values: with a positive scale the abs fold collapses the whole thing into a dull solid eightfold slab with no surface detail to explore, so Parsec only exposes the negative range where the interesting surf forms live.",
        },
        HowComputed = new[]
        {
            "The AmazingBox runs the exact same per-iteration loop as the Mandelbox, with one change at the very first step. Instead of box folding z directly, it first replaces z with abs(z), flipping every coordinate to positive, and only then applies the same box fold rule (clamp(z, -L, L) * 2 - z). Everything after that is identical to the Mandelbox: the optional rotation, the two-zone sphere fold (linear blow-up inside the min radius, sphere inversion out to the fixed radius), then z becomes scale * z + c with the running derivative updated as dr = dr * |scale| + 1.",
            "The abs step is free as far as the distance estimate is concerned. Taking an absolute value never stretches space (it is a pure reflection, so points that were one unit apart are at most one unit apart afterward), which means it leaves the running derivative dr completely untouched. Only the scaling steps move dr, exactly as in the Mandelbox, so the final distance estimate is computed the same way: DE = |z| / |dr|.",
            "The one practical difference you will notice immediately is that Parsec ships the AmazingBox with a non-zero rotation already turned on (a few degrees on each of the three axes). In the plain Mandelbox the rotation defaults to zero and does nothing; here the rotation branch is active out of the box, and that small inter-fold twist is precisely what generates the intended surf curl. As with the Mandelbox, the GPU runs this loop to draw the image and a matching CPU copy keeps the flying camera from colliding with the surface.",
        },
        Math = new[]
        {
            "The Amazing Surf family is the abs-fold Mandelbox. The update rule per iteration is: z -> abs(z) (reflect into the positive octant), then box fold each component (greater than L maps to 2L minus it, less than -L maps to -2L minus it), then the optional rotation, then the two-zone sphere fold, then z -> scale * z + c with dr -> |scale| * dr + 1. Here abs(z) means replace each of the x, y, z components by its non-negative magnitude; L is the folding limit (half-width of the box, canonical 1); the min radius (near 0.5) and fixed radius set the sphere-fold zones; c is the fixed sample point; and dr is the running derivative feeding DE = |z| / |dr|.",
            "Good community parameters use a negative scale around -1.5 with a small rotation to generate the curl, a min radius near 0.5 and a folding limit near 1. A positive scale instead fills space into a dull solid eightfold-symmetric slab, which is why the scale range here is negative-only.",
        },
        BestResults = new[]
        {
            "Leave the default rotation on, it is the point of AmazingBox. Nudge the Z angle (the largest one) a few degrees to morph the curl dramatically. Sweep Scale slowly from -1.3 to -2.0; the surf detail changes character across that band. Keep DE fudge at or below 0.8 while rotated, and raise iterations only for deep zooms. The three rotation sliders are the best morph targets for animation.",
        },
        SettingNotes = new Dictionary<string, string>
        {
            ["Scale"] = "Negative-only similarity scale. -1.5 is the famous abs-fold value; a positive scale would flatten it to a solid slab.",
            ["Min radius"] = "Inner sphere-fold radius. 0.5 is canonical.",
            ["Fixed radius"] = "Outer sphere-fold shell radius. 1.0 is canonical.",
            ["Folding limit"] = "Half-width of the box fold. 1.0 is canonical.",
            ["Rotate X"] = "Inter-fold rotation. The surf curl is what makes AmazingBox distinct from a plain negative Mandelbox.",
            ["Rotate Y"] = "Second rotation axis of the curl.",
            ["Rotate Z"] = "Third rotation axis; the largest default angle and the most dramatic morph knob.",
            ["Iterations"] = "Escape-test iteration cap. Raise for deep zooms only.",
            ["DE fudge"] = "Step safety factor. Keep at or below 0.8 because the rotated fold can overshoot.",
        },
    };

    private static GuideContent RotBox => new()
    {
        Title = "Rotated Mandelbox",
        WhatItIs = new[]
        {
            "The Rotated Mandelbox is a standard Mandelbox with one addition: a full 3D rotation is inserted before the folds on every single iteration. In the plain Mandelbox the box fold reflects coordinates across walls that are perfectly axis-aligned, meaning the folding planes are square to the x, y, and z axes. Spinning the point first, before it meets those walls, is equivalent to tilting the walls themselves, so the fold planes now slice through space at arbitrary angles. The blocky right-angle character of the Mandelbox gives way to forms that lean, twist, and shear.",
            "The crucial detail is that the rotation compounds. Because it happens at the start of every iteration, the second iteration rotates an already-rotated point, the third rotates that again, and so on, so a tiny angle accumulates into a large cumulative twist over the course of the orbit. This makes the three Euler angles (rotation about the X, Y, and Z axes) extraordinarily sensitive: a change of a fraction of a degree can cascade into a visibly different fractal. For that reason the Rotated Mandelbox is the most generative and animation-friendly shape in the box family, and its rotation sliders are given an unusually fine 0.01-degree precision so you can dial in those small but powerful changes.",
            "Like the AmazingBox, the Rotated Mandelbox reuses all of the Mandelbox distance-estimate machinery; the only real change is that the rotation is promoted from an optional, defaults-to-zero afterthought into the headline control. Note that it uses a plain box fold with no absolute-value step, which is what distinguishes it from the AmazingBox even though both rely on rotation between folds.",
        },
        HowComputed = new[]
        {
            "Each iteration begins by pre-rotating the vector: z becomes R * z, where R is the 3D rotation matrix built from the three Euler angles. Only after that rotation does the rest of the familiar Mandelbox loop run on the now-tilted point. The plain box fold is applied (clamp(z, -foldLimit, foldLimit) * 2 - z, with no abs step), then the two-zone sphere fold (linear blow-up inside the min radius, sphere inversion out to the fixed radius), and finally z becomes z * scale + c where c is the sample point, while the running derivative updates as dr = dr * |scale| + 1.",
            "When the loop finishes, the distance estimate is the same length-over-derivative formula as the rest of the family: DE = |z| / |dr|, the length of the folded vector divided by the magnitude of the accumulated derivative. One small implementation note: this fractal uses its own shader core, which packs the numbers slightly differently from the main Mandelbox core (the scale, min radius, fixed radius, and fold limit travel together, and the three Euler angles travel together in radians), but the actual computation is the standard rotated-fold Mandelbox. As elsewhere, a matching CPU copy runs only to keep the flying camera off the surface.",
        },
        Math = new[]
        {
            "This is the rotated-fold mechanism: a rotation applied to a plain box fold and made the headline control rather than a small default. The per-iteration update is z -> R * z (rotate by the Euler angles), then box fold each component (greater than the fold limit maps to 2*foldLimit minus it, less than its negative maps to -2*foldLimit minus it), then the two-zone sphere fold, then z -> scale * z + c with dr -> |scale| * dr + 1. R is the rotation matrix that compounds across iterations; c is the fixed sample point; scale is the similarity factor (negative here, canonically -2, also -1.5); and dr is the running derivative feeding DE = |z| / |dr|.",
            "Small angles (a few degrees up to about 15) keep the distance estimate a clean valid field while still morphing the shape strongly; large angles degrade it, because the cumulative twist makes the marcher overestimate how far it can safely step, which is why a DE fudge below 1 is used to take more cautious steps.",
        },
        BestResults = new[]
        {
            "Animate the three rotation angles, they are the standout morph targets in the whole app, and tiny changes (use the fine 0.01-degree precision) cascade into very different forms. Keep total rotation modest, under about 15 to 20 degrees per axis, to keep the surface crisp; if you push higher, drop DE fudge toward 0.5 to 0.6 to suppress overshoot. Pair scale -2 with the default angles for the signature rotated cityscape, and bump iterations to about 16 before deep zooms.",
        },
        SettingNotes = new Dictionary<string, string>
        {
            ["Rotate X"] = "Pre-fold rotation that compounds each iteration. The most sensitive morph knob; use the fine step.",
            ["Rotate Y"] = "Second compounding rotation axis.",
            ["Rotate Z"] = "Third compounding rotation axis.",
            ["Scale"] = "Negative-only similarity scale. -2 is canonical, -1.5 also rich.",
            ["Min radius"] = "Inner sphere-fold radius. 0.5 is canonical.",
            ["Fixed radius"] = "Outer sphere-fold shell radius. 1.0 is canonical.",
            ["Fold limit"] = "Half-width of the box fold. 1.0 is canonical.",
            ["Iterations"] = "Escape-test iteration cap. Bump to about 16 before deep zooms.",
            ["DE fudge"] = "Step safety factor. Lower toward 0.5 to 0.6 when rotation is large to suppress overshoot.",
        },
    };

    private static GuideContent Menger => new()
    {
        Title = "Folded Menger",
        WhatItIs = new[]
        {
            "The Folded Menger takes a different route to a fractal than the rest of this family, and it produces a different look: stacked, rectilinear, alien-temple architecture rather than organic shells. It is built on the classic Menger sponge, a shape made by an iterated function system (IFS). An IFS is the most literal kind of self-similarity: take a cube, replace it with several smaller copies of itself arranged in a pattern (for the Menger sponge, the cube is divided into a 3x3x3 grid and the center plus face-center pieces are removed), then do the same to each remaining small copy, forever. The limit of that process is a cube riddled with square holes at every scale, and that is the Menger sponge.",
            "Rather than literally building and removing cubes, Parsec renders this as a distance estimate using the folding trick from the primer. Each iteration reflects space into the positive octant with an absolute value, sorts the coordinates so the largest one is moved to a fixed axis, then shrinks everything toward a chosen corner. Repeating that fold-sort-shrink is mathematically equivalent to the cube-subdivision recipe, but it runs cheaply at every point in space, which is what lets the raymarcher draw it. A small rotation is also applied each iteration to break the strict axis-alignment, which is how you morph the plain sponge into twisted temple-like variants.",
            "The Folded Menger is notable as the family's non-organic member. Where the Mandelbox and its cousins give curved, eroded, biological-looking surfaces, the Menger gives hard lattices, columns, and right-angle voids that read as built rather than grown. The classic textbook sponge appears at scale 3 with the contraction corner set to (1,1,1); changing the scale and corner produces a range of pseudo-Menger architectural forms.",
        },
        HowComputed = new[]
        {
            "The Folded Menger is drawn by distance-estimated raymarching, but its per-iteration loop is an IFS contraction rather than a Mandelbox-style escape. Each iteration starts by pre-rotating the point (z becomes R * z) so the structure is not perfectly square to the axes. Then it reflects the point into the positive octant by taking abs(z), flipping any negative coordinate to positive. Then it sorts the components by magnitude using a few pairwise swaps, so that the largest-magnitude coordinate ends up on one chosen axis; this sort is the step that carves the square holes of the sponge.",
            "Next comes the shrink-and-translate that is the heart of the IFS: z becomes scale * z - offset * (scale - 1). This magnifies the point by the scale factor and then shifts it back toward a chosen corner of the cube (the offset), so that across iterations the point is repeatedly pulled toward one self-similar sub-copy. There is also a conditional re-centering on the z axis: if z has fallen too far below the offset corner on that axis, a correction is added back, which keeps the third axis folding cleanly. Throughout, the running derivative is updated as dr = dr * scale.",
            "That derivative step is the reason the scale must stay positive. The Menger core multiplies dr by the raw scale (it does not take an absolute value the way the Mandelbox does), so a negative scale would corrupt the derivative and the distance field; the schema enforces a minimum scale of 2 to keep this safe. When the loop ends, the distance estimate is a bounding-cube measure: it computes how far z sticks out past a unit cube, d = max(|z| - 1, 0) per component, and returns the length of that overshoot divided by the running derivative, length(d) / |dr|. A matching CPU copy runs only for camera collision.",
        },
        Math = new[]
        {
            "This is the standard folded-IFS Menger distance estimate (Hvidtfeldt, Folding Space). Per iteration: z -> R * z (rotate), then z -> abs(z) (reflect to the positive octant), then sort the components so the largest magnitude moves to a fixed axis, then z -> z * scale - offset * (scale - 1) (the IFS shrink toward the offset corner), with the conditional z-axis re-centering, and dr -> dr * scale (accumulating the derivative as scale^n over n iterations). Here R is the rotation matrix, abs(z) flips coordinates positive, scale is the IFS magnification, offset is the corner the contraction pulls toward, and dr is the running derivative used in the final bounding-cube DE.",
            "The classic Menger sponge is scale 3 with offset (1,1,1), which reproduces the canonical one-third self-similar sponge. Non-integer scales and varied offsets give the pseudo-Menger architectural variants; rotation morphs them. Because the derivative uses the raw (unsigned) scale, scale stays positive, with a minimum of 2.",
        },
        BestResults = new[]
        {
            "For the textbook Menger sponge, set Offset to (1,1,1) and Scale to exactly 3 with rotations at 0 (the default offset Z of 0 gives a flatter, slab-leaning variant). Then introduce a few degrees of rotation to morph the cubes into twisted temple architecture; small angles go a long way. Sweep Scale 2.5 to 3.5 for denser versus airier lattices. Keep iterations low (4 to 6); the IFS detail saturates fast.",
        },
        SettingNotes = new Dictionary<string, string>
        {
            ["Rotate X"] = "Pre-fold rotation. Smaller visual effect than RotBox (the abs fold reflects after rotation) but meaningful.",
            ["Rotate Y"] = "Second pre-fold rotation axis.",
            ["Rotate Z"] = "Third pre-fold rotation axis.",
            ["Scale"] = "Positive-only IFS scale. 3 is the canonical sponge; the derivative uses raw scale so it cannot go negative.",
            ["Offset X"] = "X component of the contraction corner. 1.0 for the canonical sponge.",
            ["Offset Y"] = "Y component of the contraction corner. 1.0 for the canonical sponge.",
            ["Offset Z"] = "Z component of the contraction corner. Set to 1.0 for the symmetric (1,1,1) sponge; the default 0 flattens the third axis.",
            ["Iterations"] = "IFS depth. Keep low (4 to 6); detail saturates quickly.",
            ["DE fudge"] = "Step safety factor. Conservative because of the sort-and-rotation distance estimate.",
        },
    };
}
