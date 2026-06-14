using System.Collections.Generic;

namespace Parsec.App;

// EXOTIC group A guide content: Apollonian, Phoenix, Biomorph, Mosely.
// Prose rewritten for a reader who knows nothing about fractals, building on the
// shared primer at the top of every guide (iteration/orbits, escape-time vs
// distance-estimator raymarching, complex numbers, distance estimators, quaternions
// and triplex numbers, folds, and orbit-trap coloring). SettingNotes and BestResults
// are copied verbatim from the original committed entries; only WhatItIs, HowComputed,
// and Math were rewritten. Anchored to docs/superpowers/research/fractals-4-exoticA.md.
public static partial class FractalGuide
{
    private static GuideContent Apollonian => new()
    {
        Title = "Apollonian Gasket",
        WhatItIs = new[]
        {
            "Start with a simple picture. Take three coins that all touch each other, so the three of them trap a small curved triangle of empty space between them. There is exactly one circle that fits snugly into that gap, kissing all three coins at once. Drop it in. Now you have several new, smaller gaps, and each of those has its own perfect kissing circle. Keep filling every gap forever and you get the classic 2D Apollonian gasket: an infinitely intricate lace of circles inside circles, where every circle touches its neighbors but never overlaps them. The word for 'just touching at a single point' is 'tangent', so this whole object is a packing of mutually tangent circles.",
            "Parsec renders the 3D version of this idea. Instead of coins on a table you use spheres in space. The setup is five spheres that are all mutually tangent: four equal spheres sitting at the four corners of a regular tetrahedron (the simplest 3D pyramid, with four triangular faces), plus one big bounding sphere that wraps around them and touches them from outside. The empty pockets between these spheres get packed with ever-smaller tangent spheres, and those leave smaller pockets, and so on without end. The mathematical name for the resulting object is the 'limit set' of this packing: the dust of contact points and infinitely fine structure that the process homes in on. It is a genuine fractal, meaning it shows the same detail no matter how far you zoom in, and it has tetrahedral symmetry inherited from the four-corner arrangement. Its measured fractal dimension is about 2.474, a number between a 2D surface and a solid 3D volume that captures how densely the packing fills space.",
            "A flat slice through the 3D packing reproduces the original 2D Apollonian gasket (whose dimension is about 1.306), so cutting the 3D object open shows you the familiar circle lace on the cut face.",
        },
        HowComputed = new[]
        {
            "The primer explained raymarching with a distance estimator (DE): for any point in space the DE returns a safe distance you can step the ray forward without hitting the object, and when that distance collapses to near zero the ray has arrived at the surface. The trick here is that Parsec never builds the actual spheres. Instead it asks, for a sample point, how deeply that point is buried inside the infinite packing, and turns that depth into a distance.",
            "It does this with 'sphere inversion', the 3D cousin of reflecting in a mirror. Inverting a point through a sphere turns the sphere's inside out: points near the center get flung far away, far points get pulled close, and the sphere's own surface stays fixed. The estimator runs a small loop on the sample point q. First it checks the four inner tetrahedron spheres: if q sits inside one of them, it inverts q out through that sphere and records how much the local scale stretched by adding log(stretch) to a running accumulator called logScale. Next, if q has landed outside the big bounding sphere, it inverts q back inside so it cannot escape. If neither test fires, q is sitting in a genuine free pocket of the packing, the orbit has settled, and the loop stops.",
            "Each inversion that fires means q was wedged deeper into the fractal, and each one bumps logScale up. After the loop, the distance estimate is deEnvelope times exp(-logScale). Read that backwards: out in open space almost no inversions happen, logScale stays near zero, exp(-logScale) is near one, and the DE is large, so the ray sails through the empty outer shells. Deep inside the packing many inversions fire, logScale grows large, exp(-logScale) collapses toward zero, the DE shrinks to nothing, and the ray stops on the actual gasket. The deEnvelope factor is just an overall thickness multiplier on this result. The half-cut option intersects the whole shape with a tilted flat half-space, computed as de = max(de, plane); this slices the solid open so you can see the 2D gasket cross-section on the flat cut face at the same time as the 3D lobes.",
        },
        Math = new[]
        {
            "Sphere inversion through a sphere of radius r centered at point c sends a point q to c + (r^2 / |q-c|^2) * (q-c). Reading the pieces: q-c is the arrow from the center to the point, |q-c| is that arrow's length, so |q-c|^2 is the squared distance; dividing r^2 by it gives a factor bigger than one when q is close (pushing it outward) and smaller than one when q is far (pulling it inward), and multiplying the arrow by that factor and re-adding c lands the inverted point. The five mutually tangent spheres come from the 3D Descartes-Soddy theorem, the rule (Soddy, 1936) that relates the curvatures, meaning one-over-radius, of spheres that all touch. The four inner sphere centers are the regular-tetrahedron vertices, taken in Parsec as the points whose coordinates are the sign combinations of 1/sqrt(2) at distance sqrt(3/2) from the origin, and the bounding-sphere radius is the canonical Soddy value (sqrt(6)+2)/2, which is about 2.2247.",
        },
        BestResults = new[]
        {
            "Keep Tangency at 1.000 for the canonical connected gasket; nudge to about 0.9 for a denser tetrahedral foam or about 1.1 for Kleinian fractal dust. Keep Outer radius at or above 0.95 so the bounding sphere does not clip the outer lobes of the gasket. Turn Cut on with a tilted normal to get the 2D Apollonian cross-section and the 3D lobes at once. Raise Iterations to 40+ when zooming into the recursive sub-gaskets.",
        },
        SettingNotes = new Dictionary<string, string>
        {
            ["Tangency (morph star)"] = "The headline morph knob. 1.0 is the exact mutually-tangent packing; below it densifies, above it fragments into dust.",
            ["Outer radius x"] = "Bounding-sphere radius multiplier. 1.0 is the Soddy value; keep at or above 0.95 to avoid clipping the gasket.",
            ["DE envelope"] = "Render/thickness knob. Lower gives thinner shells and a slower march; higher is thicker and can blur fine packing.",
            ["Cut (0/1)"] = "Toggles the half-cut. On exposes the 2D gasket on the cut face.",
            ["Plane normal x"] = "X of the cut-plane normal. A tilted (non-axis) normal reveals cross-sections axis cuts miss.",
            ["Plane normal y"] = "Y of the cut-plane normal.",
            ["Plane normal z"] = "Z of the cut-plane normal.",
            ["Cut plane offset"] = "Slides the cut plane through the packing. Sweep across 0.",
            ["Iterations"] = "Number of inversions resolved. Deeper resolves finer packing; raise to 40+ for zooms.",
            ["DE fudge"] = "Step-shortening safety factor. Lower if you see overstep artifacts.",
        },
    };

    private static GuideContent Phoenix => new()
    {
        Title = "Phoenix",
        WhatItIs = new[]
        {
            "The primer covered the Julia set: you fix a constant c, then repeatedly apply a rule like z -> z*z + c to a starting point, and you color that point by whether the resulting sequence of values (its 'orbit') stays bounded or flies off to infinity. The Phoenix set changes one thing about that recipe, and the change is striking. Ordinary Julia iteration is memoryless: the next value depends only on the current value. Phoenix iteration has a memory. The next value depends on the current value and also on the value from one step earlier. That backward glance is what makes the difference.",
            "Why does memory matter? In a memoryless map each point's fate is decided by a tidy push-forward, and the boundary between 'stays bounded' and 'escapes' tends to come out crystalline and self-similar. Feeding the previous value back in couples each step to its own past, and the boundary starts to curl and braid. Instead of sharp crystalline filigree you get organic, flame-like and feather-like growth, which is exactly why it is named after the phoenix. The set was introduced by the mathematician Shigehiro Ushiki (in IEEE Transactions on Circuits and Systems; the discovery is most often dated 1988).",
            "Phoenix is naturally a 2D object on the complex plane. Parsec lifts it into 3D the same way it lifts the Mandelbulb: it replaces the flat complex squaring with the power-2 'triplex' squaring described in the primer, the operation that squares a 3D point's distance from the origin and doubles its two spherical angles. The headline knob is the memory strength, written p (sometimes k): turn it to zero and the memory vanishes, leaving an ordinary 3D Mandelbulb-style Julia; set it negative and the curling Phoenix character appears.",
        },
        HowComputed = new[]
        {
            "Each iteration carries two pieces of state: the current point z and the previous point zPrev. One step computes a new point by taking the triplex square of the current z (the Mandelbulb power-2 operation, in spherical coordinates (r, theta, phi) -> (r*r, 2*theta, 2*phi)), then adding the fixed constant c, then adding the memory term p times zPrev. After computing that new value the loop 'rolls' the memory: the point that was current becomes the new previous, and the freshly computed value becomes the new current. So the previous value is never discarded; it is recycled into the next step, which is the whole point of the construction.",
            "The escape test is the usual one from the primer: square the length of z and compare it to the square of the Bailout radius; once |z|^2 exceeds Bailout^2 the orbit has clearly escaped and the loop ends. To raymarch the surface, Parsec needs a distance estimate. It uses the analytic Hubbard-Douady running-scalar derivative described in the primer, which tracks how fast the map stretches space by carrying a derivative magnitude alongside z. Crucially this is computed by an exact formula rather than by sampling neighboring points numerically; the triplex trig map is not perfectly smooth, and a numerical gradient on it would create faint ghost-ring artifacts, so the analytic route is chosen deliberately. The derivative update folds in the memory feedback: the new derivative bound is 2*r*dz + |p|*dzPrev, started with dz = 1 and dzPrev = 0, where r is the current radius. The final distance is 0.5 * r * log(r) / dz, the standard escape-time DE. The same tilted-plane half-cut available on the other exotics applies here, slicing the body open to reveal interior cross-sections.",
        },
        Math = new[]
        {
            "Ushiki's original 2D Phoenix map is z_next = z*z + c + p*z_prev. Reading every symbol: z is the current complex value; z*z is ordinary complex squaring; c is a fixed complex constant chosen per image; z_prev is the value of z from one iteration earlier; and p is the memory coefficient that scales how much of that earlier value is fed back in. Because the rule reaches one step into the past it is called a second-order recurrence (an ordinary Julia map is first-order, reaching back only one step to the present value). The historically cited canonical constants are c about 0.5667 on the real axis and p = -0.5. Setting p = 0 deletes the memory term and collapses the whole thing to a plain Mandelbulb-Julia; p = -0.5 gives the canonical Phoenix character. One honest caveat from the research: those 2D numbers do not transfer one-to-one through Parsec's 3D triplex lift, so treat them as direction-finding hints rather than exact targets, and note that Parsec's default c.x of 0.4 is a 3D-friendly shape, not the canonical 0.5667.",
        },
        BestResults = new[]
        {
            "Start from p = -0.5 (canonical). Sweep p toward 0 to watch the curling memory growth relax into crystalline Mandelbulb-Julia form. Try the canonical c around 0.5667 on the real axis (c.y = c.z = 0) for the textbook Phoenix flame; add small c.z for 3D asymmetry. Keep Iterations modest (14 to 20) for interactive sweeps and bump them for stills.",
        },
        SettingNotes = new Dictionary<string, string>
        {
            ["c.x"] = "Real part of the Julia constant. The canonical 2D value is about 0.5667; the default 0.4 is a 3D-friendly shape.",
            ["c.y"] = "Second component of the constant. 0 keeps it on the real axis.",
            ["c.z"] = "Third component, a 3D-lift extra with no 2D analogue; nonzero adds asymmetry.",
            ["Memory p (morph star)"] = "Strength of the previous-iterate feedback. -0.5 is canonical Phoenix; 0 collapses to Mandelbulb-Julia.",
            ["Cut (0/1)"] = "Toggles the half-cut to reveal interior cross-sections.",
            ["Plane normal x"] = "X of the cut-plane normal (normalized in the shader).",
            ["Plane normal y"] = "Y of the cut-plane normal.",
            ["Plane normal z"] = "Z of the cut-plane normal.",
            ["Cut plane offset"] = "Slides the cut plane through the body. Sweep across 0.",
            ["Iterations"] = "Escape-test iteration cap. 14 to 20 interactive; raise for detail.",
            ["Bailout"] = "Escape radius. The shader clamps it to at least 2; 4 is conventional.",
            ["DE fudge"] = "Step safety factor. Slightly below 1 is safe because the memory estimate is a conservative bound.",
        },
    };

    private static GuideContent Biomorph => new()
    {
        Title = "Biomorph",
        WhatItIs = new[]
        {
            "Biomorphs are one of the most charming accidents in fractal history. In 1986 Clifford Pickover was rendering ordinary Julia-type sets and made a small mistake in the bit of code that decides when an orbit has escaped. The standard escape test asks one question: has the value of z grown longer than some bailout radius B, measured as ordinary distance from the origin. Pickover meant to combine two conditions but swapped an AND for an OR, which quietly changed the test so that it checks each coordinate axis on its own rather than the overall length. The pictures that came out looked startlingly like microscopic organisms: bodies with arms, antennae, and bulbous cells. He named them biomorphs, from 'bio' for life and 'morph' for shape, and called the radially symmetric ones radiolarians after the real single-celled sea creatures.",
            "Here is why the swap produces limbs. The usual length-based test treats every direction the same, so escape happens along a smooth round frontier and the set keeps a clean rounded silhouette. The componentwise test instead lets a point escape the moment any single coordinate gets large, even if the other coordinates are still small. That makes the escape boundary leak outward preferentially along the coordinate axes, and those leaks stretch into the thin protruding arms and antennae that give biomorphs their organic, creature-like look. The underlying iteration is still an ordinary Julia map; only the question 'has it escaped yet' has been rephrased.",
            "Mathematically, swapping to a per-axis test means measuring size with the L-infinity norm, which is just the largest single coordinate magnitude rather than the straight-line length. Parsec renders biomorphs as a 3D object, so the per-axis test runs over all three axes x, y, and z, and nonzero structure on the third axis adds genuine 3D limbs.",
        },
        HowComputed = new[]
        {
            "Each iteration is the plain triplex-square Julia step from the primer: take the Mandelbulb power-2 of the current point z and add the fixed constant c, written z -> bulbPow2(z) + c. Nothing exotic happens in the update itself; the entire biomorph effect lives in the escape test. Instead of the usual 'has |z| passed the bailout', Parsec asks whether the single largest coordinate has passed it: max(|z.x|, |z.y|, |z.z|) > Bailout. A source comment in the core literally calls this 'the entire trick'. Because a point can trip this test by growing along just one axis, the escaped regions reach out along the axes and carve the limbs and antennae.",
            "For raymarching, Parsec needs a distance to the surface, and it uses the same analytic Hubbard-Douady running-scalar derivative described in the primer: it carries a derivative magnitude dz updated as dz = 2*r*dz with dz starting at 1, where r is the current radius, and returns de = 0.5 * r * log(r) / dz. There is an honesty flag here worth stating plainly. That standard distance estimate was derived assuming the round, length-based escape test, not the per-axis L-infinity one. Used with the L-infinity test it is only approximate; the research notes it can be off by at most a factor of sqrt(3) (about 1.73), which is the worst-case mismatch between a box and the sphere that just contains it in 3D. Parsec compensates with the DE fudge slider, which shortens each ray step by a safety factor so the slightly-too-optimistic estimate never oversteps the surface. The standard tilted-plane half-cut applies, slicing the creature open to show its interior.",
        },
        Math = new[]
        {
            "Biomorph escape rule: a point is treated as still interior unless one of its coordinates exceeds the threshold, that is, unless max(|x|, |y|, |z|) > B. The expression max(|x|, |y|, |z|) is the L-infinity norm: take the absolute value of each coordinate, then keep the largest. Contrast it with the ordinary length sqrt(x*x + y*y + z*z), the L-2 norm, which blends all coordinates together; the L-infinity norm instead listens to whichever single axis is loudest, and that is precisely what makes orbits leak out along the axes into limbs. B is the bailout threshold, and Pickover's canonical value is B = 10. Common biomorph constants c have real parts around 0.5 to 0.76; an honesty flag from the research: the (-0.5, 0.5) default used here is a community-style constant in the same family, reasonable but not tied to a single canonical citation.",
        },
        BestResults = new[]
        {
            "Keep Bailout at 10 (Pickover canonical); drop toward 3 to 5 for tighter, denser bodies or push to 15 to 20 for long spindly antennae. Raise Iterations toward 50 to 100 for crisp limbs in final renders, since the L-infinity leak needs several iterations to express arms. Sweep c around (-0.5, 0.5) in small steps; the creature morphology is very sensitive.",
        },
        SettingNotes = new Dictionary<string, string>
        {
            ["c.x"] = "Real part of the Julia constant. -0.5 gives a strong radial creature; radiolarian reals run about 0.5 to 0.76.",
            ["c.y"] = "Imaginary part of the constant. 0.5 is in the typical biomorph band.",
            ["c.z"] = "Third axis, a 3D-lift extra; nonzero adds 3D limbs.",
            ["Bailout B (biomorph)"] = "Componentwise escape threshold. 10 is Pickover canonical; lower tightens limbs, higher extends antennae.",
            ["Cut (0/1)"] = "Toggles the half-cut to reveal the creature interior.",
            ["Plane normal x"] = "X of the cut-plane normal (normalized in the shader).",
            ["Plane normal y"] = "Y of the cut-plane normal.",
            ["Plane normal z"] = "Z of the cut-plane normal.",
            ["Cut plane offset"] = "Slides the cut plane through the body. Sweep across 0.",
            ["Iterations"] = "Escape-test iteration cap. 16 is fast; raise to 50 to 100 for crisp limbs.",
            ["DE fudge"] = "Step safety factor. Below 1 offsets the approximate L-infinity distance estimate.",
        },
    };

    private static GuideContent Mosely => new()
    {
        Title = "Mosely Snowflake",
        WhatItIs = new[]
        {
            "Most fractals in this app are escape-time sets, decided by iterating a number and watching whether it runs away. The Mosely snowflake is a different breed entirely: it is built by repeatedly cutting up a solid shape, the same way the famous Sierpinski triangle and Menger sponge are built. The recipe is dead simple to state. Take a cube and slice it into a 3 by 3 by 3 grid of 27 smaller cubes, like a Rubik's cube. Throw away most of them and keep only a chosen subset. Then do the exact same thing to each cube you kept, and to each of theirs, forever. Because the same operation repeats at every scale, the result is self-similar: zoom into any kept piece and it looks like a shrunken copy of the whole.",
            "The choice of which subcubes to keep is what defines the variant. Parsec keeps the 8 corner subcubes, called the corner rule. The object is named after Jeannine Mosely, an engineer and origami artist who discovered this snowflake-sponge variant around 2006 while exhibiting her enormous Menger sponge folded from business cards. Keeping only the 8 corners gives a relatively sparse, lacy structure with a fractal dimension of log8/log3, about 1.893, a number that says the object is wispier than a filled surface.",
            "The signature payoff is what you see looking straight down the body diagonal of the cube, the line from one corner through the center to the opposite corner, the direction along which the three coordinate axes look symmetric. From that viewpoint the silhouette of the 3D corner-packing collapses into the classic 3-fold Koch snowflake outline, the same crinkly six-pointed star curve people draw in 2D. So the Mosely snowflake is, in a real sense, a 3D solid whose shadow down its main diagonal is a 2D snowflake.",
        },
        HowComputed = new[]
        {
            "This is an exact linear cube-IFS (iterated function system), not an escape-time fold, and that distinction matters for how cleanly it renders. Rather than literally chopping a cube 27 ways, Parsec uses the standard raymarching trick from the primer: it folds space so that one carved corner cell stands in for the whole infinite structure, then measures distance to a single box. The estimator runs this sequence each iteration. First a world-frame fold, z = abs(z), which mirrors the point into one octant so the eight corners become one. Second, a rotation into an orthonormal frame aligned with the body diagonal, built from three perpendicular axes U, V, W so the [1,1,1] direction becomes a clean axis to work around. Third, a twist: a 2D rotation of the cross-section plane about that diagonal. Fourth, an optional kaleidoscope wedge fold that maps the cross-section into a single angular sector (skipped when the wedge is a full circle). Fifth, a rotation back to the world frame. Sixth, the scale step toward the corner: z = scale*z - (scale-1), with the derivative scaled by the same factor, dz *= scale.",
            "After folding through the chosen number of iterations, the distance is sdBox(z, vec3(body)) / dz: the signed distance to an axis-aligned cube of half-size 'body', divided by the accumulated derivative dz that records how much all the scalings shrank space. Here is the reason this fractal looks so clean. Every operation in the loop except the single uniform scale is an isometry, meaning it moves points around without stretching or distorting distances (folds, rotations, and reflections all preserve length). Only the one uniform scale changes size, and dz tracks it exactly. So unlike the escape-time fractals, which lean on the approximate log-potential distance estimate, this distance estimate is mathematically exact. That is why you can crank the iterations high and still get crisp edges with no soft haloes around the structure.",
        },
        Math = new[]
        {
            "The Mosely snowflake belongs to the Menger-sponge family of cube-removal fractals. The standard Menger sponge removes the center and the 6 face-centers, keeping 20 of the 27 subcubes, giving dimension log20/log3, about 2.727. Parsec instead keeps the 8 corners (a dual, inverse corner-cube rule), which has dimension log8/log3, about 1.893. The dimension formula reads: at each step the shape splits into N self-similar copies (here N = 8) each scaled down by a factor s (here s = 3, the 3 by 3 by 3 subdivision), and the fractal dimension is log(N)/log(s); plugging in log8/log3 gives roughly 1.893. This corner rule is what produces the 3-fold Koch silhouette down the body diagonal. The snowflake defaults are Scale = 3 (the exact 3 by 3 by 3 subdivision) and a body around 1.4. Twist near 120 degrees breaks the mirror symmetry into a chiral pinwheel, one that cannot be superimposed on its own mirror image, and shrinking the wedge below 360 degrees folds the cross-section into a repeating angular slice to make a radial mandala. One honest note from the research: pressing Reset does not currently restore the Wedge and Fudge sliders, only Iterations, Scale, Body, and Twist, so after changing Wedge or Fudge they keep their last value rather than snapping back to 360 and 0.9.",
        },
        BestResults = new[]
        {
            "Pure snowflake: Twist 0, Wedge 360, Scale 3.0, viewed down the body diagonal for the 3-fold Koch silhouette. Sweep Twist toward 118 to 120 degrees for a chiral pinwheel (animatable reveal). Bring Wedge down toward 120 for a radial mandala. Take Body from 1.0 to 1.5 to go from sparse dust to full lace; keep Iterations 12 to 20. Because the distance estimate is exact, high iterations stay crisp without haloes.",
        },
        SettingNotes = new Dictionary<string, string>
        {
            ["Scale"] = "Subdivision scale. 3.0 is the exact 3x3x3 snowflake; the narrow window lets you detune slightly for non-canonical lace.",
            ["Body"] = "SDF thickness. 1.0 is sparse dust, about 1.4 is fuller lace; it never fuses solid because the corner rule carves at every scale.",
            ["Twist"] = "Rotation about the body diagonal. 0 is the mirror-symmetric snowflake; about 120 degrees gives a chiral pinwheel.",
            ["Wedge (360=off)"] = "Kaleidoscope sector fold. 360 disables it (pure snowflake); about 120 gives a radial mandala.",
            ["Iterations"] = "IFS depth. Exact distance estimate, so more iterations add depth without haloes; 12 to 20 typical.",
            ["DE fudge"] = "Step safety factor. The distance estimate is exact, so it can sit near 1.0; 0.9 is conservative-safe.",
        },
    };
}
