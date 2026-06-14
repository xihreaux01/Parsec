using System.Collections.Generic;

namespace Parsec.App;

// Guide prose for the BULB / JULIA family: Mandelbulb, Quaternion Julia,
// Bicomplex Julia, and QJulia x Box (half-cut). The WhatItIs / HowComputed /
// Math fields are written for a reader who has just finished the shared primer
// (what a fractal is; iteration and orbits; escape-time vs distance-estimated
// raymarching; complex numbers; quaternions and the triplex; folds; orbit-trap
// and palette and lighting) and knows nothing else. SettingNotes and
// BestResults are copied verbatim from the original registry entries.
public static partial class FractalGuide
{
    private static GuideContent Mandelbulb => new()
    {
        Title = "Mandelbulb",
        WhatItIs = new[]
        {
            "The Mandelbulb is the most famous attempt to take the flat 2D Mandelbrot set and turn it into a solid 3D object you can fly around. In 2D, the Mandelbrot set lives in the plane of complex numbers, where squaring a number both rotates it and scales it. The trick that makes the Mandelbrot shape so intricate is that simple squaring, repeated over and over. To get a 3D version we need a way to 'square' a point in space, meaning a rule that takes a 3D point and rotates and stretches it the same way complex squaring does in the plane. There is no natural multiplication for 3D points, so Daniel White and Paul Nylander invented one in 2009 by analogy: describe the point by how far it is from the center and by two angles (like latitude and longitude on a globe), then raise the distance to a power and multiply both angles by that same power. That invented rule is what this whole fractal is built on.",
            "When you use the power 8 (the canonical choice), the result is the iconic 'bulb': a heavy, rounded, cauliflower-like body with deep folds, surrounded near its poles by a halo of impossibly fine lace and filigree. The power is the single biggest shape control. Power 2 gives a softer, simpler blob; higher powers wrap the body in more lobes and more rotational symmetry. Because the power is just a number you can slide smoothly, animating it from 2 up to 8 looks like lobes literally growing out of the surface. Everything else about the Mandelbulb (the lighting, the color, the way the camera glides up to the surface) is the standard raymarching machinery from the primer; the only genuinely new idea here is that one made-up 3D squaring rule.",
        },
        HowComputed = new[]
        {
            "Parsec renders the Mandelbulb by distance-estimated raymarching, exactly as the primer described: a ray is fired from the camera through each pixel, and at every point along the ray we ask a single function 'how far is the nearest piece of the fractal from here?' We then step the ray forward by that safe distance and repeat until we either touch the surface or give up. The interesting part is how that distance function decides whether a given point in space is inside or outside the bulb, and how far away the surface is.",
            "For a candidate point in space, call it c, we start a running 3D vector v at the origin and iterate the made-up squaring rule. Each iteration does five concrete steps. First, measure r, the length of v (its distance from the center): r = sqrt(v.x*v.x + v.y*v.y + v.z*v.z). Second, find the two angles that describe v's direction: theta is the angle down from the vertical z axis, theta = acos(v.z / r), and phi is the angle around the equator, phi = atan2(v.y, v.x). Third, raise the radius to the power: the new radius is r^n (for n = 8 that is r multiplied by itself eight times). Fourth, multiply both angles by n, so theta becomes n*theta and phi becomes n*phi; this is the 3D echo of how complex squaring doubles an angle. Fifth, convert that new radius and those new angles back into an ordinary (x, y, z) vector and add the original point c. That finished vector is the next v, and we loop.",
            "Just like the Mandelbrot test, we watch whether v stays trapped near the center or flies off to infinity. If after many iterations the length of v stays below the bailout radius, the point c is treated as inside the solid; if v shoots past the bailout, c is outside, and how quickly it escaped tells us roughly how far outside. To turn that escape information into an actual distance, Parsec carries a second number alongside v called the derivative, dr, which tracks how fast nearby points are being pulled apart by the iteration. It updates as dr -> n * r^(n-1) * dr + 1. Because r^(n-1) can grow astronomically and overflow normal 32-bit math after about 43 iterations, Parsec keeps this derivative in log space so it never blows up. The final distance estimate is DE = 0.5 * log(r) * r / dr, the standard Hubbard-Douady formula. The ray uses that DE to take its next safe step, and the DE fudge setting just shrinks each step slightly for safety when the estimate is optimistic.",
        },
        Math = new[]
        {
            "The update rule is v -> v^n + c, where v and c are 3D points and 'v to the power n' uses White and Nylander's triplex power instead of any real multiplication. Writing v in spherical form with radius r = |v|, polar angle theta = acos(v.z / r), and azimuth phi = atan2(v.y, v.x), the power is defined as v^n = r^n * (sin(n*theta)*cos(n*phi), sin(n*theta)*sin(n*phi), cos(n*theta)). In words: the new length is the old length raised to the n-th power, and the two direction angles are simply multiplied by n, then that direction-and-length is written back as an ordinary (x, y, z) vector. Compare this to the primer's complex squaring, where squaring a 2D number squares its length and doubles its single angle; here we square (or n-th power) the length and scale both 3D angles by n, which is the direct analogy. The symbol n is the Power setting (n = 8 is canonical), c is the point in space being tested (it stays fixed for the whole orbit), and |v| means the length of the vector v. We declare the orbit escaped the moment |v| exceeds the Bailout radius; n = 8 is the standard, while other integer and fractional powers give different lobe counts and symmetries.",
        },
        BestResults = new[]
        {
            "Power 8 is the classic look; try 2-16 for different symmetries. Raise Iterations (default 10) toward 30-60 for crisp filigree in hero stills; lower it while flying for speed. A larger Bailout (default 8) sharpens the distance estimate and reduces mushy edges.",
        },
        SettingNotes = new Dictionary<string, string>
        {
            ["Power"] = "The exponent n in v -> v^n + c. 8 is canonical; lower powers give fewer lobes, higher powers more.",
            ["Iterations"] = "Escape-test iteration cap. Higher reveals finer filigree at the cost of framerate.",
            ["Bailout"] = "Escape radius. Larger values give a more accurate distance estimate and crisper edges.",
            ["DE fudge"] = "Safety factor on the marching step. Lower if you see overstepping artifacts; raise toward 1.0 for speed.",
        },
    };

    private static GuideContent QuaternionJulia => new()
    {
        Title = "Quaternion Julia",
        WhatItIs = new[]
        {
            "A Quaternion Julia set is the 4D cousin of the flat Julia sets from the primer. A 2D Julia set comes from iterating z -> z^2 + c where z is a complex number that moves and c is a complex number you pick once and hold fixed; the fixed c is the shape's DNA, and every different c gives a completely different Julia shape. Quaternions are the primer's 4D number system, which (unlike the Mandelbulb's invented triplex) is a real, well-behaved algebra where you can genuinely multiply, square, and even divide. So we run the exact same idea, z -> z^2 + c, but now z and c are quaternions, four-component numbers, and the resulting Julia set is a solid object living in four dimensions.",
            "We cannot see 4D directly, so Parsec renders a 3D slice: it fixes the fourth coordinate at a chosen value (the '4D slice' setting) and looks at the 3D cross-section that remains, the same way slicing a 3D apple gives you a flat 2D face. Sweeping that fourth coordinate slowly is like pushing the knife through the apple, morphing the whole object continuously. Because quaternions are a true division algebra, the squaring map is smooth and analytic, which means the distance estimate is mathematically exact and the surface comes out clean rather than noisy. Parsec's signature touch is the half-cut: it slices the solid open with a flat plane so you can see the nested, onion-like shells hiding inside, which is the feature that makes this fractal worth exploring.",
        },
        HowComputed = new[]
        {
            "Parsec raymarches a distance estimate, the same fire-a-ray-and-step-forward process from the primer. To test a point, it builds the starting quaternion z from the 3D sample position plus the fixed 4D slice value as the fourth coordinate. In the optional stereographic mode it first wraps ordinary 3D space onto the surface of a 4D sphere (a 3-sphere) before doing this, which curves and separates the lobes; a small conformal correction is applied to the distance afterward so the wrap does not distort the spacing.",
            "Each iteration performs the quaternion square and adds the constant. Squaring a quaternion is done with the standard Hamilton product, which for a square has a tidy form: the new scalar part is (scalar squared minus the length of the vector part squared), and the new vector part is just (2 * scalar * vector part). After squaring, we add the fixed constant c, giving z -> z^2 + c. Alongside z we carry a running quaternion derivative z', updated by z' -> 2 * z * z' (again via the Hamilton product), which measures how fast the iteration stretches space near this point.",
            "We iterate until the length of z exceeds the bound radius (escape) or we hit the iteration cap (treated as inside). The distance to the surface is then DE = 0.5 * log(r) * r / |z'|, where r is the final length of z and |z'| is the length of the derivative quaternion; this is the same Hubbard-Douady estimator used by the Mandelbulb, and because quaternion squaring is genuinely analytic it is exact here rather than approximate. Finally the half-cut is applied as a constructive-geometry intersection: the distance to the solid is combined with the signed distance to a flat plane by taking whichever is larger, so the ray can only land on the part of the solid that lies on the visible side of the plane. The plane is a true 3D distance and is deliberately left out of the stereographic scaling so the cut stays flat and crisp.",
        },
        Math = new[]
        {
            "The standard quaternion Julia is f(q) = q^2 + c, where q is the moving quaternion and c is the fixed quaternion constant that defines the shape. A quaternion has four parts, one real (scalar) and three imaginary (the i, j, k directions); squaring it follows the same rotate-and-scale spirit as complex squaring, just in 4D, using the Hamilton multiplication rule from the primer. The distance estimate is the Hubbard-Douady form DE = 0.5 * |z| * ln|z| / |z'|, where |z| is the length of the current quaternion, ln is the natural logarithm, and |z'| is the length of the running derivative quaternion. The familiar 2D Julia set is simply one slice of this 4D object, and choosing different 3D slices (different fixed fourth coordinates) reveals different shapes from the same c. Paul Bourke's published catalog of good-looking constants includes (-0.2, 0.8, 0, 0), which is the default used here.",
        },
        BestResults = new[]
        {
            "Leave Cut on with offset 0 to reveal the nested interior, then sweep Cut plane offset to scrub through the solid. Animate the 4D slice from -1 to 1 for a continuous morph of the whole object. For the separated-lobe stereographic look, set Stereographic on and keep the stereo radius near 0.8. Raise iterations toward 16 to 20 for crisp interior shell detail in stills; try alternate Bourke constants such as (-0.2, 0.6, 0.2, 0.2) for different identities.",
        },
        SettingNotes = new Dictionary<string, string>
        {
            ["c.x"] = "First component of the quaternion constant. The default (-0.2, 0.8, 0, 0) is a Paul Bourke catalog value.",
            ["c.y"] = "Second component of the quaternion constant.",
            ["c.z"] = "Third component. 0 keeps the slice clean and symmetric.",
            ["c.w"] = "Fourth component. 0 keeps the slice simple.",
            ["4D slice (w)"] = "The fixed 4th coordinate of the 3D slice. A great animation target; sweep -1 to 1 to morph.",
            ["Cut (0/1)"] = "Toggles the half-cut. On exposes the nested interior, the killer feature.",
            ["Cut axis (0X 1Y 2Z)"] = "Which axis the cut plane is perpendicular to.",
            ["Cut plane offset"] = "Slides the cut plane through the solid to scrub the interior cross-section.",
            ["Stereographic (0/1)"] = "Wraps space onto a 3-sphere for the separated-lobe curved-cut look.",
            ["Stereo scale k"] = "Scale of the stereographic wrap. About 1 frames it well.",
            ["Stereo radius R"] = "Radius of the wrap sphere. Near 0.8 separates the lobes.",
            ["Iterations"] = "Escape-test iteration cap. 10 reads the shape; raise for fine shell detail.",
            ["DE fudge"] = "Step safety factor. 0.9 is a sensible conservative value with the cut on.",
        },
    };

    private static GuideContent Bicomplex => new()
    {
        Title = "Bicomplex Julia",
        WhatItIs = new[]
        {
            "The Bicomplex Julia set is another 4D Julia set, z -> z^2 + c, but it swaps out the number system. Where the Quaternion Julia uses quaternions, this one uses bicomplex numbers (also called tessarines), a different four-component algebra. The crucial difference is in their multiplication rules. Quaternions have three imaginary units that all square to -1, and their multiplication does not commute (order matters). Bicomplex numbers have one unit i that squares to -1 like usual, a second unit j that squares to +1, and their product k = ij; because of that +1, bicomplex multiplication is commutative (order does not matter), but the algebra is not a division algebra: there exist nonzero numbers whose product is zero (zero divisors).",
            "That missing division property is exactly what gives this fractal its look. The quaternion set is smooth and rounded because its algebra is so well-behaved; the bicomplex set, lacking that safety net, can produce faceted, crystalline, even sharply discontinuous boundaries, like a cut gemstone instead of a smooth pebble. Parsec uses Fracmonk's formula from fractalforums, which adds two artist controls on top of the pure bicomplex square: a per-component multiplier on each of the four output terms, and an extra additive nudge on the w term ('W add'). These deliberately break the natural symmetry of the clean square to open up a much wider space of shapes to explore. Be aware this is an artist variant, not standard bicomplex dynamics: setting all four multipliers to 1 (and W add to 0) recovers the true, mathematically faithful bicomplex set, and everything else is creative embellishment.",
        },
        HowComputed = new[]
        {
            "Parsec raymarches this the same way as the other Julia sets, building the starting 4D point z from the 3D sample position plus the fixed 4D slice value, then iterating. The one genuinely different piece is the squaring step, because bicomplex multiplication expands into its own specific set of component formulas. Writing the four parts of z as (x, y, z, w), one bicomplex square computes: the new x is Xmul * (x*x - y*y - 2*z*w), the new y is Ymul * (2*x*y + z*z - w*w), the new z is Zmul * (2*x*z - 2*y*w), and the new w is Wmul * (2*x*w + 2*y*z) + Wadd. Then the fixed constant c is added component by component. Those formulas are the literal bicomplex multiplication rule (with j squaring to +1, which is why some terms add where the quaternion version would subtract); the Xmul..Wmul multipliers and the Wadd term are the artist knobs layered on top, and with all multipliers at 1 and Wadd at 0 the formulas reduce to the genuine bicomplex square.",
            "Carrying a clean analytic derivative is harder here because the iteration is not analytic once you bend it with non-unit multipliers, so Parsec uses a conservative scalar (single-number) derivative instead of a full quaternion one. It updates as dz -> 2 * maxMul * r * dz, where r is the current length and maxMul is the largest absolute value among the four multipliers. By always using the biggest multiplier, this deliberately over-estimates how fast space is stretching, which makes the resulting distance an under-estimate of the true distance. That is the safe direction to err: under-estimating distance means the ray takes slightly shorter, cautious steps and never punches through a thin feature, which is why an earlier numerical-gradient approach was abandoned for producing wispy filament noise. The distance estimate itself is the same Hubbard-Douady shape, de = 0.5 * r * log(r) / dz, and the half-cut plane intersection works identically to the Quaternion Julia.",
        },
        Math = new[]
        {
            "A bicomplex (tessarine) number is written w + x*i + y*j + z*k, with the multiplication rules i*j = j*i = k, i^2 = -1, and j^2 = +1. Notice i*j = j*i: the order does not matter, so unlike quaternions this multiplication is commutative. The bicomplex Julia iterates w -> w^2 + c, the same square-and-add-a-constant recipe as every Julia set, just carried out with these bicomplex rules so the four components mix according to the expansion shown above. The per-component multipliers (Xmul, Ymul, Zmul, Wmul) and the W add term are Fracmonk's artist variant from fractalforums and are not part of standard bicomplex dynamics; setting all the multipliers to 1 (and W add to 0) recovers the true bicomplex set. The +1 square of j is the key technical detail: it is why the boundary can turn faceted and crystalline where the quaternion set stays smooth.",
        },
        BestResults = new[]
        {
            "Start from the defaults (the clean square: multipliers 1, W add 0), then nudge a single multiplier (for example Y mul to 0.8) to break symmetry and watch crystalline facets emerge. Keep Cut on and sweep Cut plane offset to expose the interior, which looks distinct from the smooth quaternion Julia. If the boundary shows thin-filament noise, lower DE fudge toward 0.6 rather than raising iterations; raise iterations for crisp detail in stills.",
        },
        SettingNotes = new Dictionary<string, string>
        {
            ["c.x"] = "First component of the Julia constant.",
            ["c.y"] = "Second component of the Julia constant.",
            ["c.z"] = "Third component. 0 keeps it clean.",
            ["c.w"] = "Fourth component. 0 keeps it clean.",
            ["4D slice (w)"] = "The fixed 4th coordinate of the slice. Sweep for morphs.",
            ["X mul"] = "Per-component multiplier on the x term. 1.0 is the true bicomplex square; departing breaks symmetry.",
            ["Y mul"] = "Per-component multiplier on the y term. 1.0 is clean; the first one to nudge for facets.",
            ["Z mul"] = "Per-component multiplier on the z term. 1.0 is clean.",
            ["W mul"] = "Per-component multiplier on the w term. 1.0 is clean.",
            ["W add"] = "Additive symmetry-break on the w term. 0 is the clean square; small values add variety.",
            ["Cut (0/1)"] = "Toggles the half-cut to expose the faceted interior. On by default.",
            ["Cut axis (0X 1Y 2Z)"] = "Which axis the cut plane is perpendicular to.",
            ["Cut plane offset"] = "Slides the cut plane through the solid.",
            ["Iterations"] = "Escape-test iteration cap. 12 reads the shape; raise for detail.",
            ["Bailout"] = "Escape radius. The shader floors it at 2.",
            ["DE fudge"] = "Step safety factor. 0.85 is conservative for the non-analytic, faceted boundary.",
        },
    };

    private static GuideContent QJBox => new()
    {
        Title = "QJulia x Box (half-cut)",
        WhatItIs = new[]
        {
            "This fractal is a deliberate mash-up of two completely different fractal recipes run together inside the same loop. One recipe is the Quaternion Julia square, z -> z^2 + c in 4D, the smooth rounded object described above. The other is the Mandelbox fold, a fractal built not from multiplication but from origami: every iteration it reflects (folds) space back on itself with a box fold and a sphere fold, then scales it, which produces hard, boxy, architectural detail full of corridors and cells. Doing both every iteration, on the same running point, fuses the organic Julia body with the crystalline Mandelbox lattice into something neither could make alone.",
            "The control that makes it come alive is a small rotation applied to the 3D part of the point on every single iteration. Because the same rotation is applied again and again, its effect compounds: a tiny one-degree twist becomes an enormous reshaping after eight or twelve iterations, so the three Rotate angles are by far the most powerful morph knobs in the app. Combined with the inherited half-cut plane that slices the solid open, you get a genuinely intricate interior cross-section rather than a featureless blob. This is the richest parameter set in Parsec: the Mandelbox fold parameters, the quaternion constant c, the 4D slice, three rotation angles, and the cut all interact. The distance estimate for such a hybrid has no clean mathematical proof, so it is treated as a heuristic guess and a built-in 0.5 safety factor keeps the ray from overstepping.",
        },
        HowComputed = new[]
        {
            "Parsec raymarches a heuristic distance estimate. Each iteration runs three stages in sequence on the running 4D point z. Stage one is the compounding rotation: the 3D part of z (its x, y, z) is rotated by a fixed Euler rotation built from the three Rotate angles, R = Rz * Ry * Rx; because this same rotation is reapplied every iteration, the twist accumulates and small angles produce dramatic morphs.",
            "Stage two is the Mandelbox half, applied to that rotated 3D part. The box fold reflects any coordinate that has strayed past a fold limit back inward: clamp the vector to the range [-foldLimit, foldLimit], double it, and subtract the original. The sphere fold then pushes points around based on their distance from the center: points inside a small inner radius get scaled up linearly, points between the inner radius and a fixed outer radius get turned inside out by an inversion, and points outside are left alone. Finally the folded 3D vector is multiplied by the Mandelbox Scale (kept negative, around -1.8, for the richest structure) and the original sample point is added back. A derivative is tracked through these folds as derivative -> derivative * |Scale| + 1 so the distance estimate knows how much space was stretched.",
            "Stage three runs the full 4D quaternion square z -> z^2 + c on the result, with its derivative updated by the Hamilton product as before. After the iterations finish, the distance is DE = 0.5 * 0.5 * log(r) * r / |z'|, the familiar Hubbard-Douady estimator but with an extra 0.5 multiplied in as the hybrid safety factor, since no exact bound exists for this combination. The half-cut then intersects the solid with a plane (take the larger of the solid distance and the plane distance) to expose the interior, and the bailout escape radius is fixed inside the shader rather than exposed as a setting.",
        },
        Math = new[]
        {
            "This is a custom hybrid of two canonical halves, run one after the other each iteration. The Mandelbox half is z -> Scale * sphereFold(boxFold(z)) + c, where boxFold reflects out-of-range coordinates back inward, sphereFold scales or inverts points by their radius, and Scale is the overall stretch; negative scales near -1.5 to -2 are especially rich. The Quaternion Julia half is q -> q^2 + c, the smooth 4D square-and-add described earlier. The two are bridged by a compounding per-iteration rotation R = Rz * Ry * Rx applied to the 3D part before the fold, whose repetition is what makes tiny Rotate angles produce large visual change. The default constant c = (-0.2, 0.6, 0.1, 0) sits near Paul Bourke's catalogued (-0.2, 0.6, 0.2, 0.2). The compounding-rotation behavior is Parsec's own variant, not a published canonical formula, which is why its distance estimate carries the extra 0.5 safety factor.",
        },
        BestResults = new[]
        {
            "The rotation angles are the morph stars: sweep Rotate X/Y/Z slightly (they compound per iteration, so small changes have large visual effect) for dramatic animations. Keep Cut on and scrub Cut plane offset to expose the intricate interior cross-section. Keep Scale in the -1.5 to -2.0 zone with the standard fold params (min 0.5, fixed 1.0, limit 1.0). Leave DE fudge low (about 0.6); if you see banding when zooming, lower it further before adding iterations.",
        },
        SettingNotes = new Dictionary<string, string>
        {
            ["Rotate X"] = "Compounding per-iteration rotation. The morph star; small angles have large effect.",
            ["Rotate Y"] = "Second compounding rotation axis.",
            ["Rotate Z"] = "Third compounding rotation axis.",
            ["Cut (0/1)"] = "Toggles the half-cut to expose the interior cross-section. On by default.",
            ["Cut axis (0X 1Y 2Z)"] = "Which axis the cut plane is perpendicular to.",
            ["Cut plane offset"] = "Slides the cut plane through the solid.",
            ["c.x"] = "First component of the quaternion constant.",
            ["c.y"] = "Second component of the quaternion constant.",
            ["c.z"] = "Third component of the quaternion constant.",
            ["c.w"] = "Fourth component of the quaternion constant.",
            ["4D slice (w)"] = "The fixed 4th coordinate of the slice. Sweep for morphs.",
            ["Scale"] = "Negative-only Mandelbox scale. Keep in the -1.5 to -2.0 band for the richest structure.",
            ["Min radius"] = "Inner sphere-fold radius. 0.5 is standard.",
            ["Fixed radius"] = "Outer sphere-fold shell radius. 1.0 is standard.",
            ["Fold limit"] = "Half-width of the box fold. 1.0 is standard.",
            ["Iterations"] = "Escape-test iteration cap. 8 reads the shape; raise toward 12 to 16 for interior detail.",
            ["DE fudge"] = "Step safety factor. Keep low (about 0.6) for the heuristic hybrid distance estimate.",
        },
    };
}
