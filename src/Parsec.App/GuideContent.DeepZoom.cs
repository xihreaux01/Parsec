using System.Collections.Generic;

namespace Parsec.App;

// Guide prose for the 3D Burning Ship and the four 2D deep-zoom formulas.
// WhatItIs / HowComputed / Math are written for a reader with zero fractal
// background; they build on the shared primer at the top of every guide
// (iteration/orbits, escape-time vs distance-estimated raymarching, complex
// numbers, folds, palettes). SettingNotes and BestResults are copied verbatim
// from the prior committed entries (a test asserts every label keeps its note).
public static partial class FractalGuide
{
    private static GuideContent BurningShip => new()
    {
        Title = "3D Burning Ship",
        WhatItIs = new[]
        {
            "This is a 3D solid fractal carved out of empty space, and it is a close cousin of the Mandelbulb you may have seen elsewhere in this app. The primer explained that for an escape-time fractal we treat each point as a starting number, apply the same arithmetic rule over and over, and ask whether the running value flies off to infinity (escapes) or stays trapped near the origin forever (stays bounded). The trapped points form the solid body of the fractal; the escaping points are the empty space around it. The Mandelbulb does this in 3D using a 'triplex' number (a made-up 3D analogue of a complex number) whose multiplication rule is: turn the point into a direction-and-distance description (its angles and its radius r), then raise r to a power and multiply the angles by that same power. That single rule, applied repeatedly, grows a smooth bulbous shell.",
            "The 3D Burning Ship keeps that exact triplex power rule but adds one extra step at the end of every iteration: it takes the absolute value of each of the three coordinates, which means any negative coordinate is flipped to its positive twin. Flipping the sign of a coordinate is a 'fold', because geometrically it mirrors everything on the negative side over onto the positive side, like creasing a sheet of paper along a plane and pressing the two halves together. Doing this fold on every iteration is what turns the Mandelbulb's smooth shell into the terraced, sharply mirror-symmetric, wind-swept stacks of plates that give the 'burning ship' its name.",
            "Its most distinctive look, the swept laminar sheets, appears at LOW power, roughly 2 to 3. This is the opposite of the Mandelbulb, where power 8 is the famous setting. At high power the r-raised-to-the-power growth dominates everything and the folds stop mattering, so the shape degenerates back toward a flat-bottomed Mandelbulb. Be aware this 3D object is a community recipe, not a textbook fractal: only the 2D Burning Ship has a single agreed-upon definition.",
        },
        HowComputed = new[]
        {
            "Like the Mandelbulb, this is rendered by raymarching with a distance estimator (DE), the technique the primer described: instead of testing every point in space, a ray is shot from the camera through each pixel, and at each step along the ray a function tells you the safe distance to the nearest part of the solid fractal, so the ray can leap forward by that much without risk of overshooting. When the safe distance shrinks to nearly zero, the ray has hit the surface. To produce that safe distance the renderer runs the escape-time iteration AND simultaneously tracks how fast nearby points are being pulled apart (the running derivative), because the spacing of escaping points near the surface is what the distance estimate is built from.",
            "Each iteration does the following. First it converts the current triplex value z into spherical coordinates in a y-up convention: the polar angle theta is measured down from the +Y axis as atan2(sqrt(x^2 + z^2), y), the azimuth is atan2(x, z), and the radius is r. Second it advances the running derivative in log space; because the abs fold is an isometry (it only mirrors, it never stretches or shrinks), the fold itself does not change the derivative magnitude, so the derivative only has to track the power step. Third it applies the power map proper: raise r to the chosen power, multiply both angles by that power, rebuild the x, y, z coordinates from the new angles and radius, and add the original sample point c (this is the '+ c' that anchors the iteration to where you are in space). Fourth it applies the burning-ship fold z = abs(z), flipping any negative component positive.",
            "The escape test is simply: if r grows past the bailout radius, the point has escaped, stop iterating. When iteration finishes, the final distance estimate uses the standard Mandelbulb log formula, roughly 0.5 * log(r) * r * exp(-running_derivative). One honest caveat: near the spherical-coordinate poles (straight up and straight down the Y axis) this distance estimate is not a strict lower bound, meaning it can occasionally claim more free space than really exists and let a ray nick the surface, producing torn creases. That is exactly why the default DE fudge is set below 1.0: it deliberately shrinks every marching step to a safe fraction of the estimate so those pole creases do not tear.",
        },
        Math = new[]
        {
            "The 2D Burning Ship is the canonical, published object (Michelitsch and Rossler, 1992). Writing a complex number as z = Re(z) + i*Im(z), where Re is the real part, Im is the imaginary part, and i is the unit with i*i = -1, the rule is z_next = (|Re(z)| + i*|Im(z)|)^2 + c. The bars |x| mean absolute value (drop the minus sign), so both parts are forced positive before squaring; escape is when |z| > 2. That forced-positive step before the squaring is the entire difference from the ordinary Mandelbrot, and it is what produces the ship.",
            "The 3D version here is NOT a standard published formula. It is a community / custom extension that borrows the Mandelbulb's spherical-coordinate power map (raise r to the power, scale the angles by the power) and bolts on a per-component abs() fold to imitate the 2D ship in three dimensions. Because it is a recipe rather than a defined mathematical object, there is no single canonical choice of power, escape radius, or distance estimate; the y-up angle convention and the scalar log-space DE used here are this implementation's own decisions. Treat the visuals as an exploration of one reasonable 3D recipe, not as 'the' 3D Burning Ship.",
        },
        BestResults = new[]
        {
            "Keep Power at 2.0 to 3.0 for the windblown laminar sheets; unlike the Mandelbulb where 8 is the money value, here the headline knob lives low. Treat Power as a smooth animation target. If surface creases tear near the poles, lower DE fudge toward 0.5 before raising iterations. Raise Iterations to 32 to 64 for tight zooms; the default 16 is an overview value.",
        },
        SettingNotes = new Dictionary<string, string>
        {
            ["Power"] = "The triplex exponent. The headline knob, and it lives LOW (2 to 3), the opposite of the Mandelbulb; high power degenerates toward a Mandelbulb look.",
            ["Iterations"] = "Escape-test iteration cap. 16 is an overview; raise to 32 to 64 for tight zooms.",
            ["Bailout"] = "Escape radius. 2 matches the 2D |z| > 2 escape; larger smooths coloring slightly.",
            ["DE fudge"] = "Step safety factor. Deliberately below 1.0 to suppress pole creases; lower if they tear.",
        },
    };

    // ===================================================================
    // DEEP ZOOM 2D formulas
    // ===================================================================

    private static GuideContent Mandelbrot2D => new()
    {
        Title = "Deep Zoom: Mandelbrot",
        WhatItIs = new[]
        {
            "This is the single most famous fractal of all, the Mandelbrot set, rendered here in Parsec's flat 2D deep-zoom mode. As the primer described escape-time fractals: every pixel on screen stands for one starting number, you feed that number through a fixed arithmetic rule again and again, and you watch the running value. For the Mandelbrot set the rule is 'square the current value and add the pixel's number'. If the value stays bounded forever, the pixel belongs to the set and is painted as solid interior; if it eventually flies off toward infinity, the pixel is outside, and we color it by HOW FAST it escaped (its escape speed), which is what produces the glowing filaments and bands around the black body.",
            "The black body sits in the 'parameter plane': the pixel's number is the c that gets added each step, while the starting value z always begins at zero. Instead of the 3D camera and sliders used elsewhere in Parsec, this mode gives you a flat pan-and-zoom 'camera' driven by the mouse, and it is built specifically to keep going far past where an ordinary zoom would dissolve into blurry numerical mush. It can reach zoom depths of roughly 1e-147 (that is the screen half-height measured in mathematical units, so a value like 1e-147 means the visible window is unimaginably tiny).",
            "Reaching that depth is the whole point of this mode, and it is only possible because of a technique called perturbation theory, explained next. Ordinary computer numbers (doubles) carry only about 15 to 16 significant digits; once you zoom so far that neighboring pixels agree in their first 16 digits and differ only in the 17th and beyond, plain doubles can no longer tell those pixels apart and the image collapses. Perturbation is the trick that gets around this wall.",
        },
        HowComputed = new[]
        {
            "The core idea of perturbation deep zoom is to do the expensive high-precision arithmetic exactly ONCE, not once per pixel. Parsec picks the point at the center of your current view and iterates its orbit in true high precision (many more digits than a double), recording the whole sequence of values. This recorded sequence is called the 'reference orbit'. Every pixel on screen is very close to that center, so instead of tracking each pixel's full value, Parsec tracks only the tiny DIFFERENCE between the pixel's orbit and the reference orbit. That difference is called the delta (written dz), and because it is tiny, all its meaningful digits fit comfortably inside a fast low-precision double. In short: one slow exact orbit at the center, plus a fast cheap delta for every pixel, gives you the depth of high precision at the speed of low precision.",
            "There is a bookkeeping subtlety. As iterations proceed, a pixel's own orbit can sometimes shrink until it is actually smaller than its delta from the reference, or the pixel can wander so far that the reference orbit is no longer a good stand-in (it 'runs off' the end of the stored reference). Either situation makes the delta math lose accuracy, producing visible glitches. The cure is 'rebasing': when it detects this, the renderer resets the reference index back to the start and re-expresses the delta against the freshly restarted reference, which keeps every digit meaningful and the picture glitch-free.",
            "Parsec chooses one of three depth paths automatically, based on how far you have zoomed (the view radius). For wide, shallow views (radius above about 1e-6) it skips perturbation entirely and iterates each pixel directly in plain double precision, which is exact and simple at that scale. From there down to about 1e-148 it switches to double-precision perturbation, the fast delta-plus-rebasing method just described. Below 1e-148 a problem appears: the delta gets so small that squaring it (the dz^2 term) underflows, meaning the result is smaller than the tiniest number a double can store and silently becomes zero. To survive that, the deepest path carries the delta as a 'floatexp' number (a normal double mantissa paired with a separate large integer exponent), which can represent absurdly small magnitudes. That floatexp path is correct but runs about 3 to 5 times slower per iteration, so it is reserved for only the deepest zooms.",
        },
        Math = new[]
        {
            "The rule is z_next = z^2 + c, where z starts at 0 and c is the pixel's position in the parameter plane (z^2 means complex multiplication of z by itself, which both rotates and scales as the primer described). A point escapes when |z|^2 > 4, that is when the squared distance of z from the origin passes 4 (equivalently |z| > 2). Squaring the distance instead of taking a square root each step is just a speed optimization; the boundary is identical.",
            "The perturbation step is the same rule rewritten for the delta. Let Zref be the reference orbit's value at this iteration, dz the pixel's delta, and dc the pixel's tiny offset in c from the center. Then dz_next = 2*Zref*dz + dz^2 + dc. This comes straight from expanding (Zref + dz)^2 + (Cref + dc) and subtracting the reference's own (Zref^2 + Cref): the cross term gives 2*Zref*dz, the square gives dz^2, and the leftover constant is dc. Because Zref is supplied at full precision while dz and dc stay tiny, this formula holds the depth without needing high precision per pixel. The home view is centered near (-0.5, 0), inside the main heart-shaped body of the set.",
        },
        BestResults = new[]
        {
            "Wide views (radius above about 1e-6) use exact double precision; from there down to about 1e-148 the fast double-precision perturbation runs; below that the floatexp path takes over (correct but 3 to 5x slower per iteration), so expect deep frames to render notably slower. Iterations auto-scale with depth, so if boundary filaments render as solid you are iteration-starved at that spot; zoom slightly or accept the heuristic. The kappa sliders are inert here; they only affect the Julia formula.",
        },
        SettingNotes = new Dictionary<string, string>
        {
            ["kappa re (Julia)"] = "Inert for the Mandelbrot formula. It only sets the Julia constant when the Julia formula is selected.",
            ["kappa im (Julia)"] = "Inert for the Mandelbrot formula. Only used by the Julia formula.",
        },
    };

    private static GuideContent Prospector2D => new()
    {
        Title = "Deep Zoom: Prospector",
        WhatItIs = new[]
        {
            "Prospector is a custom Parsec formula with no standard published definition. It is NOT a recognized named fractal: a web search turns up no published 'Prospector' fractal using this rule, so this guide deliberately describes only what the shader actually computes and makes no claim about a canonical mathematical object. Treat it as an exploration of one hand-picked iteration rule that happens to produce interesting structure, rather than a textbook set with established theory behind it.",
            "Mechanically it is still a pure escape-time fractal of exactly the kind the primer described: each pixel is a starting point, a fixed arithmetic rule is applied over and over, and the pixel is colored by whether the running value stays bounded or escapes to infinity, and by how fast it escapes. The difference from the Mandelbrot set is only the rule itself, which is given in the Math section. One practical consequence of that rule is that even the bounded, non-escaping orbits swing out to a fairly large magnitude before settling, so this formula uses a much larger escape radius than the complex formulas do (otherwise it would wrongly flag bounded points as escaped).",
        },
        HowComputed = new[]
        {
            "It uses the identical perturbation deep-zoom machinery as the other formulas in this mode, so the depth story is the same. Parsec iterates one high-precision 'reference orbit' at the center of the view exactly once, then for every pixel iterates only the tiny DIFFERENCE (the delta) from that reference using fast low-precision arithmetic. This keeps the effective precision enormous while the per-pixel work stays cheap, because the deep digits live in the shared reference and the delta only has to carry the small leftover. As before, whenever a pixel's orbit shrinks below its delta or runs off the end of the stored reference, the renderer 'rebases' (restarts the reference index and re-expresses the delta) to keep the math glitch-free.",
            "The same three automatic depth paths apply, selected by how far you have zoomed. Wide views (radius above about 1e-6) iterate each pixel directly in exact double precision. Middle depths (down to about 1e-148) use double-precision perturbation, the fast delta-plus-rebasing method. The deepest zooms (below 1e-148, where squaring the delta would underflow a double to zero) switch to floatexp perturbation, which represents the delta as a mantissa plus a separate large integer exponent so it can survive arbitrarily tiny magnitudes; that path is correct but roughly 3 to 5 times slower per iteration.",
        },
        Math = new[]
        {
            "This is a real-valued 2D map (it works on a plain coordinate pair (X, Y) rather than a single complex number). Writing the pixel position as (Cx, Cy), the rule iterated each step is: X_next = Cx + 0.25*X*Y, and Y_next = Cy - 3*X^2 + 0.25*Y^2, starting from X = Y = 0. Here X^2 means ordinary real squaring (X times X), and the constants 0.25 and 3 are simply the fixed coefficients baked into this particular map. Because its bounded orbits grow until |z|^2 (the squared magnitude X^2 + Y^2) reaches about 53, the escape test fires only at a much larger radius-squared of about 1e6, so genuinely bounded points are not mistaken for escaped ones. The home view is centered at (0, 0).",
            "To be explicit about the honesty flag: every coefficient and term above is just a transcription of the shader code, not a derivation from any published theory. There is no known name, no literature, and no canonical escape radius or interior structure documented for this map. It is a custom Parsec object, included because it renders attractive results, and the math here is a faithful description of the code and nothing more.",
        },
        BestResults = new[]
        {
            "Treat it as an exploration of a custom map rather than a textbook fractal. Wide views use exact double precision; deeper views move through the perturbation paths automatically, with the floatexp path (below about 1e-148) being correct but 3 to 5x slower. Iterations auto-scale with depth. The kappa sliders are inert here; they only affect the Julia formula.",
        },
        SettingNotes = new Dictionary<string, string>
        {
            ["kappa re (Julia)"] = "Inert for the Prospector formula. It only sets the Julia constant when the Julia formula is selected.",
            ["kappa im (Julia)"] = "Inert for the Prospector formula. Only used by the Julia formula.",
        },
    };

    private static GuideContent Julia2D => new()
    {
        Title = "Deep Zoom: Julia",
        WhatItIs = new[]
        {
            "A Julia set is the close partner of the Mandelbrot set, and it is a pure escape-time fractal in exactly the primer's sense: each pixel is a starting number, the rule 'square it and add a constant' is applied over and over, and the pixel is colored by whether the running value stays bounded or escapes to infinity and how quickly. The one structural change from the Mandelbrot set is which quantity is fixed and which one the pixel controls. In the Mandelbrot set the starting value is always 0 and the pixel supplies the added constant c. In a Julia set it is the reverse: the added constant is held FIXED for the whole image, and the pixel itself is the starting value. This is called the dynamical plane, as opposed to the Mandelbrot's parameter plane.",
            "Because the constant is fixed per image, changing it produces an entirely different Julia set. Parsec exposes that constant as kappa, split into a real part and an imaginary part, and these two kappa sliders are the only slider-exposed parameters in the whole deep-zoom mode. Crucially they are keyframeable, meaning you can set kappa to one value at the start of an animation and another at the end, and Parsec will smoothly interpolate between them, morphing the entire fractal from one shape into another over the course of the clip. Every point in the family is connected to a single point of the Mandelbrot set: the Mandelbrot set is, in effect, a map of which kappa values give connected Julia sets and which give scattered dust.",
            "The deep-zoom plumbing (perturbation theory) is identical to the Mandelbrot mode and lets you zoom to roughly 1e-147 without the image dissolving into numerical mush, which ordinary doubles (about 15 to 16 significant digits) cannot do on their own. The mechanics of that are covered next.",
        },
        HowComputed = new[]
        {
            "The perturbation deep-zoom idea is the same as for the Mandelbrot set: do the costly high-precision arithmetic only once. Parsec iterates a single high-precision 'reference orbit' at the center of the view, then for every pixel tracks only the tiny DIFFERENCE (the delta) from that reference using fast low-precision doubles, so you get high-precision depth at low-precision speed. There is one Julia-specific wrinkle in how that reference is set up. For the Mandelbrot set the reference orbit starts at 0; for a Julia set the starting value IS the position, so the center pixel's reference orbit starts at the view center itself, not at 0. To make the per-pixel deltas line up correctly against that shifted starting point, each pixel enters its offset from the center as its INITIAL delta, and the rebasing step carries the reference-start correction so the arithmetic stays exact.",
            "Apart from that initialization detail, everything matches the Mandelbrot pipeline. As iterations run, whenever a pixel's orbit shrinks below its delta or runs off the end of the stored reference, the renderer rebases (restarts the reference index and re-expresses the delta) to avoid glitches. And the same three automatic depth paths apply, chosen by zoom radius: exact double precision for wide views (radius above about 1e-6), fast double-precision perturbation for the middle range (down to about 1e-148), and the slower floatexp perturbation below that, where an ordinary double would underflow to zero when squaring the delta. The floatexp path stores the delta as a mantissa plus a separate large integer exponent so it can represent vanishingly small numbers; it is correct but roughly 3 to 5 times slower per iteration.",
        },
        Math = new[]
        {
            "The rule is z_next = z^2 + kappa, where z^2 is complex multiplication of z by itself (which rotates and scales, as the primer described) and kappa is the single fixed complex constant for the whole image. A point escapes when |z|^2 > 4 (squared distance from the origin passes 4, the same boundary as |z| > 2). The essential difference from the Mandelbrot rule z_next = z^2 + c is bookkeeping, not arithmetic: here the constant added each step (kappa) is the same for every pixel, and the pixel supplies the starting z, so this draws on the dynamical plane.",
            "kappa is entered as two numbers, its real part 'kappa re' and its imaginary part 'kappa im', together forming kappa = kappa_re + i*kappa_im. The home view is centered at (0, 0). The default kappa of (-0.8, 0.156) is the community-named 'Spiral Galaxy' Julia set; values that sit on or just inside the boundary of the Mandelbrot set give connected, intricate sets, while values far outside it give disconnected dust.",
        },
        BestResults = new[]
        {
            "Keep kappa near the Mandelbrot boundary for connected, detailed sets. Try named constants such as Douady Rabbit (-0.122, 0.745), Dendrite (0, 1.0), San Marco (-0.75, 0), Airplane (-1.7549, 0), or Siegel Disk (-0.391, -0.587). Sweep kappa as a keyframe to morph the set in an animation. Wide views are exact; deeper views move through the perturbation paths, with the floatexp path being correct but slower.",
        },
        SettingNotes = new Dictionary<string, string>
        {
            ["kappa re (Julia)"] = "Real part of the Julia constant. The default -0.8 (with 0.156) is the Spiral Galaxy set; keep near the Mandelbrot boundary for connected sets. Keyframeable.",
            ["kappa im (Julia)"] = "Imaginary part of the Julia constant. Pairs with kappa re; near the Mandelbrot boundary gives detailed sets. Keyframeable.",
        },
    };

    private static GuideContent BurningShip2D => new()
    {
        Title = "Deep Zoom: Burning Ship",
        WhatItIs = new[]
        {
            "This is the canonical 2D Burning Ship, rendered in the flat deep-zoom mode, and unlike its 3D cousin elsewhere in Parsec this one IS a single, agreed-upon, published fractal (Michelitsch and Rossler, 1992). It is a pure escape-time fractal in the primer's sense: each pixel is a starting number, a fixed rule is applied over and over, and the pixel is colored by whether the running value stays bounded or escapes to infinity and how fast. Like the Mandelbrot set it lives in the parameter plane (the pixel supplies the added constant c, and the starting value begins at 0), with mouse-driven pan and zoom.",
            "Its single defining twist is a fold, the same idea as in the 3D version. Each iteration forces part of the value to be positive (it takes an absolute value, dropping any minus sign) before continuing. Geometrically, forcing a coordinate positive mirrors the negative half of the plane onto the positive half, creasing the picture and breaking the smooth Mandelbrot symmetry. Repeated every step, this fold sculpts the characteristic result: a craggy hull with a tall thin mast and antenna rising above it, which is what earns the fractal its 'burning ship' name. Just like the Mandelbrot set, it can be zoomed extremely deep (to roughly 1e-147) using perturbation theory, described next.",
        },
        HowComputed = new[]
        {
            "The perturbation deep-zoom skeleton is shared with the other formulas: one high-precision 'reference orbit' is iterated at the center of the view, and every pixel then tracks only the tiny DIFFERENCE (the delta) from that reference in fast low-precision math, giving high-precision depth at low-precision cost, with 'rebasing' (restarting the reference index and re-expressing the delta) whenever a pixel's orbit shrinks below its delta or runs off the reference. The Burning Ship, however, has a genuine extra difficulty that the smooth Mandelbrot and Julia formulas do not: the fold (the absolute value) has a sharp corner where the value crosses zero, and perturbation across that corner is unstable when the delta is large. In plain terms, when two nearby pixels land on opposite sides of the fold, the simple delta arithmetic can break down.",
            "Parsec handles this in two ways. First, at wide views (radius above about 1e-6) it does not use perturbation at all; it runs the exact direct double-precision path, iterating each pixel's own orbit from scratch, because that direct path is the only fully reliable one for the Burning Ship when deltas are large. Second, when it does use perturbation at deeper zooms, it does not fold the delta naively; it uses a special primitive called diffabs, defined as diffabs(c, d) = |c + d| - |c|, which computes exactly how the absolute value changes when you nudge a reference value c by a delta d, correctly handling the case where the nudge pushes the value across zero. This matches the standard case analysis used by the well-known deep-zoom tools (Kalles Fraktaler and mathr), and Parsec's implementation was checked against a high-precision oracle to confirm it agrees.",
            "Below that, the same three automatic depth paths apply as in the other formulas. Wide views use the exact direct double-precision path. Middle depths (down to about 1e-148) use double-precision perturbation with the diffabs fold. The deepest zooms (below 1e-148, where squaring the delta would underflow a double to zero) switch to floatexp perturbation, which carries the delta as a mantissa plus a separate large integer exponent to survive vanishingly small magnitudes; it is correct but roughly 3 to 5 times slower per iteration.",
        },
        Math = new[]
        {
            "Treating the pixel as a coordinate pair, with X and Y the running value's two parts and (Cx, Cy) the pixel's position, the rule iterated each step is: X_next = X^2 - Y^2 + Cx, and Y_next = 2*|X*Y| + Cy, starting from X = Y = 0. Here X^2 and Y^2 are ordinary real squaring, X*Y is their product, and the bars |X*Y| mean absolute value (force it positive). The first equation, X^2 - Y^2 + Cx, has NO absolute value, and that is not an oversight: it is identical to the real part of an ordinary complex squaring, and squaring already makes |X|^2 equal to X^2, so folding X would change nothing. Only the cross term in the second equation, 2*|X*Y|, actually folds, and that single forced-positive cross term is the entire difference between this and the plain Mandelbrot map.",
            "A point escapes when |z|^2 > 4, that is when the squared distance X^2 + Y^2 passes 4 (equivalently |z| > 2), the same boundary used by the Mandelbrot and Julia formulas. The home view is centered near (-0.5, -0.5), down and to the left where the main hull of the ship sits. This is exactly the canonical Burning Ship of the published definition; the only thing this mode adds is the deep-zoom machinery on top.",
        },
        BestResults = new[]
        {
            "Use wide views (radius above about 1e-6) for the cleanest rendering, since the Burning Ship can glitch under perturbation at wide deltas and the exact direct path is most reliable there. Deeper views move through the perturbation and floatexp paths automatically. Iterations auto-scale with depth. The kappa sliders are inert here; they only affect the Julia formula.",
        },
        SettingNotes = new Dictionary<string, string>
        {
            ["kappa re (Julia)"] = "Inert for the Burning Ship formula. It only sets the Julia constant when the Julia formula is selected.",
            ["kappa im (Julia)"] = "Inert for the Burning Ship formula. Only used by the Julia formula.",
        },
    };
}
