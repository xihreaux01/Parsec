using System.Collections.Generic;

namespace Parsec.App;

/// <summary>Hand-authored guide prose for every fractal and deep-zoom formula.
/// The settings list itself is auto-derived from the live schema; this only
/// supplies the per-setting NOTE text (keyed by the exact ParamDescriptor.Label)
/// plus the surrounding prose. Anchored to the committed research in
/// docs/superpowers/research/.</summary>
public static partial class FractalGuide
{
    public static GuideContent Resolve(FractalType type, int deepFormula)
    {
        if (type == FractalType.DeepZoom)
            return DeepFormulas[System.Math.Clamp(deepFormula, 0, DeepFormulas.Count - 1)];
        return Registry[type];
    }

    private static readonly IReadOnlyList<GuideContent> DeepFormulas = new[]
    {
        Mandelbrot2D, Prospector2D, Julia2D, BurningShip2D,
    };

    private static readonly IReadOnlyDictionary<FractalType, GuideContent> Registry =
        new Dictionary<FractalType, GuideContent>
        {
            [FractalType.Mandelbox] = Mandelbox,
            [FractalType.AmazingBox] = AmazingBox,
            [FractalType.RotBox] = RotBox,
            [FractalType.Menger] = Menger,
            [FractalType.Mandelbulb] = Mandelbulb,
            [FractalType.QuaternionJulia] = QuaternionJulia,
            [FractalType.Bicomplex] = Bicomplex,
            [FractalType.QJBox] = QJBox,
            [FractalType.Kifs] = Kifs,
            [FractalType.Kleinian] = Kleinian,
            [FractalType.PseudoKleinian4D] = PseudoKleinian4D,
            [FractalType.OrbitHybrid] = OrbitHybrid,
            [FractalType.Apollonian] = Apollonian,
            [FractalType.Phoenix] = Phoenix,
            [FractalType.Biomorph] = Biomorph,
            [FractalType.Mosely] = Mosely,
            [FractalType.RiemannSphere] = RiemannSphere,
            [FractalType.Mandalay] = Mandalay,
            [FractalType.Anisotropic] = Anisotropic,
            [FractalType.Hybrid] = Hybrid,
            [FractalType.Attractor] = Attractor,
            [FractalType.BurningShip] = BurningShip,
        };

    // ===================================================================
    // BOX family
    // ===================================================================

    private static GuideContent Mandelbox => new()
    {
        Title = "Mandelbox",
        WhatItIs = new[]
        {
            "Tom Lowe's 2010 box-shaped escape-time fractal. Space is folded back on itself by a box fold (reflect anything outside the folding limit) and a sphere fold (invert points inside a shell), then scaled and translated, and the whole step repeats. The classic look is a self-similar arches-and-corridors cityscape, and it is the parent of the entire fold family.",
        },
        HowComputed = new[]
        {
            "Each iteration applies the box fold, an optional Euler rotation when any angle is non-zero, the two-zone sphere fold (linear blow-up inside the min radius, sphere inversion out to the fixed radius), then scale-and-translate by the sample point. A running scalar derivative is tracked alongside, and the distance estimate is the magnitude of the vector divided by that derivative.",
        },
        Math = new[]
        {
            "Per component: if greater than the limit map to 2L minus the value, if less than the negative limit map to -2L minus it (box fold); then if inside the min radius scale up, if inside the fixed radius invert (sphere fold); then z becomes scale*z + c. The standard set is scale 2, min radius 0.5, fixed radius 1, folding limit 1. Scale 2 and 3 give solid cityscapes; -1.5 and -2 give the famous hollow, organic forms.",
        },
        BestResults = new[]
        {
            "Start at the default scale 2 for the solid cityscape, then flip to -1.5 to explore the fractal-zoo surface (expect to zoom in for the embedded shapes) or -2 for the dense hollow look. Keep the rotations at 0 here: the distance estimate is exact, so the marcher can take near-full steps and you can raise DE fudge toward 1.0 for speed. Raise iterations only when zooming deep; 14 is fine for overview shots.",
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
            "A Mandelbox variant that replaces the plain box fold with the Amazing fold: first reflect into the positive octant with abs(z), then box fold. With a negative scale and a small inter-fold rotation this yields the richly detailed, curled Amazing Surf forms that dominate a lot of fractal art.",
            "It shares all of the Mandelbox distance-estimate machinery; only the fold and the default rotation differ.",
        },
        HowComputed = new[]
        {
            "The loop is identical to the Mandelbox except the fold first takes abs(z) and then box-folds. The abs step leaves the running derivative untouched. The Parsec default ships with a non-zero rotation, so the optional inter-fold rotation is active out of the box, which produces the intended surf curl.",
        },
        Math = new[]
        {
            "The Amazing Surf family is the abs-fold Mandelbox. Good community parameters use a negative scale around -1.5 with a small rotation to generate the curl, a min radius near 0.5 and a folding limit near 1. A positive scale instead fills space into a dull solid 8-fold-symmetric slab, which is why the scale range here is negative-only.",
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
            "A standard Mandelbox with a full 3D rotation inserted before the folds every iteration, so the fold planes cut space at arbitrary angles instead of axis-aligned. The rotation compounds across iterations, making the three Euler angles extremely sensitive morph knobs, the most generative and animation-friendly controls in the box family.",
        },
        HowComputed = new[]
        {
            "Each iteration pre-rotates the vector, applies the plain box fold (no abs), runs the two-zone sphere fold, then scales and translates by the sample point while tracking the running derivative. The distance estimate is the vector length over that derivative.",
        },
        Math = new[]
        {
            "This is the rotated-fold mechanism, with rotation applied to a plain box fold and made the headline control rather than a small default. A negative scale (canonically -2, also -1.5) is standard. Small angles (a few up to about 15 degrees) keep the distance estimate a clean valid field while still morphing the shape strongly; large angles degrade it, which is why a DE fudge below 1 is used.",
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
            "A Menger-sponge-style iterated function system rendered as a distance estimate, with an abs fold, a magnitude sort of the components, and a pre-rotation each iteration to break strict axis-alignment. It produces stacked, rectilinear, alien-temple architecture, a different aesthetic category from the organic fold fractals.",
        },
        HowComputed = new[]
        {
            "Each iteration pre-rotates, reflects to the positive octant with abs(z), sorts the components so the largest magnitude goes to one axis, then shrinks and translates toward an offset corner (z becomes scale*z minus offset*(scale-1)) with a conditional re-centering on the z axis. The running derivative is multiplied by scale, so the scale must stay positive; the schema enforces a minimum of 2. The final distance is a bounding-cube estimate.",
        },
        Math = new[]
        {
            "This is the standard folded-IFS Menger distance estimate (Hvidtfeldt, Folding Space): fold and sort, then z = z*scale - offset*(scale-1), accumulating the derivative as scale^n. The classic Menger sponge is scale 3 with offset (1,1,1). Non-integer scales and varied offsets give the pseudo-Menger architectural variants; rotation morphs them.",
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

    // ===================================================================
    // BULB / JULIA family
    // ===================================================================

    private static GuideContent Mandelbulb => new()
    {
        Title = "Mandelbulb",
        WhatItIs = new[]
        {
            "The Mandelbulb is a 3D analogue of the Mandelbrot set: it raises a point in spherical coordinates to a power and adds the start point, iterating until the orbit escapes. The power-8 version is the canonical 'bulb' with its cauliflower lobes and filigree.",
        },
        HowComputed = new[]
        {
            "Parsec raymarches a distance estimate. Each step converts the running vector to spherical coordinates (r, theta, phi), raises r to the power and multiplies the angles by the power, converts back, and adds the start point. A running derivative gives the distance estimate that lets the ray step safely.",
        },
        Math = new[]
        {
            "Iteration: v -> v^n + c in spherical form, with theta,phi scaled by n. Escape when |v| exceeds the bailout radius. Power n = 8 is the standard; other integer and fractional powers give different lobe counts.",
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
            "A 4D Julia set: iterate z -> z^2 + c in the quaternions, where c is a fixed quaternion constant that defines the shape's identity. Because quaternions are a true division algebra the map is analytic and the distance estimate is exact and well-behaved.",
            "The 4D object is rendered as a 3D slice (fix the 4th coordinate), and Parsec's signature half-cut clips the solid with a plane to reveal the iconic nested, onion-like interior.",
        },
        HowComputed = new[]
        {
            "The seed is the sample point plus the 4D slice value, optionally wrapped onto a 3-sphere in stereographic mode. Each iteration squares the quaternion and adds c, carrying a running quaternion derivative, and the distance estimate is 0.5*log(r)*r over the derivative magnitude. The half-cut intersects the solid with a half-space so the interior is exposed.",
        },
        Math = new[]
        {
            "The standard quaternion Julia is f(q) = q^2 + c with q and c quaternions; the distance estimate is the Hubbard-Douady form 0.5*|z|*ln|z| / |z'|. The 2D Julia set is a slice of the 4D object, and different 3D slices give different shapes. Paul Bourke's catalog includes the (-0.2, 0.8, 0, 0) used here as the default.",
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
            "A 4D Julia set built on the bicomplex (tessarine) algebra rather than the quaternions. Bicomplex numbers multiply commutatively but are not a division algebra (they have zero divisors), so the boundary can show faceted, crystalline, discontinuous character unlike the smooth quaternion Julia.",
            "Parsec uses Fracmonk's fractalforums formula with artist-added per-component multipliers and a W add that symmetry-break the otherwise-clean bicomplex square for a richer exploration space.",
        },
        HowComputed = new[]
        {
            "Each iteration applies the bicomplex square component-by-component (with the optional per-component multipliers and W add) and adds c. The distance estimate is a Hubbard-Douady running-scalar derivative whose growth absorbs the worst-case stretch from non-unit multipliers, so it under-estimates distance conservatively rather than punching holes. The half-cut works the same way as the quaternion Julia.",
        },
        Math = new[]
        {
            "A bicomplex number is w + xi + yj + zk with ij = ji = k, i^2 = -1, j^2 = +1; the bicomplex Julia iterates w -> w^2 + c. The per-component multipliers and W add are Fracmonk's artist variant from fractalforums, not standard bicomplex dynamics; setting all multipliers to 1 recovers the true bicomplex set.",
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
            "A hybrid that runs both a Mandelbox fold and a quaternion-Julia square every iteration, with a compounding per-iteration rotation as the morph control and the inherited half-cut to reveal a genuinely intricate (not blobby) cross-section.",
            "It is the richest parameter set in the app: Mandelbox fold params, the quaternion constant c, the 4D slice, three rotation angles, and the cut. The distance estimate is heuristic, so a 0.5 safety factor is baked in.",
        },
        HowComputed = new[]
        {
            "Each iteration rotates the 3D part by a compounding Euler rotation, runs the Mandelbox half on that 3D part (box fold, sphere fold, scale and translate), then runs the full 4D quaternion square and adds c, tracking a quaternion derivative. The distance estimate is the bulb-style form with an extra 0.5 hybrid safety factor, and the half-cut exposes the interior. Bailout is fixed in the shader.",
        },
        Math = new[]
        {
            "This is a custom hybrid of two canonical halves. Mandelbox: z -> scale*sphereFold(boxFold(z)) + c, with negative scales near -1.5 to -2 being especially rich. Quaternion Julia: q -> q^2 + c. The default c (-0.2, 0.6, 0.1, 0) is near Bourke's (-0.2, 0.6, 0.2, 0.2). The compounding-rotation behavior is Parsec's own variant.",
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

    // ===================================================================
    // KIFS / KLEINIAN family
    // ===================================================================

    private static GuideContent Kifs => new()
    {
        Title = "Amazing IFS",
        WhatItIs = new[]
        {
            "The Kaleidoscopic IFS (Amazing Surface) family, introduced by Knighty on Fractal Forums. Despite the IFS name it is an escape-time system: each iteration plane-folds space into the positive octant, applies a rotation before and after the fold, inverts through a sphere, then scales toward a pivot.",
            "The pre/post rotations composed with repeated scaling wind folded edges into logarithmic-spiral scrollwork; the sphere fold everts sheets into bell and trumpet flares. With rotations zeroed and the right pivot and scale it degenerates to the Sierpinski tetrahedron or Menger sponge.",
        },
        HowComputed = new[]
        {
            "Each iteration pre-rotates, takes abs(z) as a plane fold, post-rotates, applies a sphere-fold inversion, then scales toward the pivot (z = scale*z - (scale-1)*pivot) while tracking a running derivative. The distance estimate is the vector length over the derivative, the standard escape-time fold-family estimator.",
        },
        Math = new[]
        {
            "Knighty's construction composes scalings, translations, plane reflections (conditional folds) and rotations (the only conformal 3D maps besides sphere inversion). The base escape-time iteration without rotation is z = abs(z); z = scale*z - offset*(scale-1) with the canonical Menger/Sierpinski offset near (1,1,1) and scale 2. The pre/post rotation upgrade turns the rigid polyhedral fractal into the smooth spiral amazing forms.",
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
            "The inversive-limit-set (pseudo-Kleinian) family. It iterates a sphere inversion plus a box fold plus a scaled translation, producing a 3D Kleinian/Apollonian foam: nested packed spheres tiling space.",
            "Unlike the Mandelbox and KIFS, this family has no stable analytic scalar-derivative distance estimate, so the renderer measures distance to the level set numerically.",
        },
        HowComputed = new[]
        {
            "Each iteration does a sphere inversion with an inner linear zone, a box fold, then a scale-and-offset, and returns log(length(z)) as a potential. The distance estimate is the potential divided by the magnitude of its gradient, with the gradient taken by central differences over seven potential evaluations. That makes raising iterations here roughly seven times as expensive as an analytic core.",
        },
        Math = new[]
        {
            "The pseudo-Kleinian is built from the two conformal folds of the Mandelbox: a ball fold (sphere inversion) and a box fold, followed by z = scale*z + c. The attractor is the limit set of the generated inversive group, a Kleinian/Apollonian sphere packing. The offset c is the generator that determines which limit set you get. Because the iterated map has no convergent analytic derivative, distance is taken as |log|z|| over |grad log|z||.",
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
            "A Kleinian-group limit-set approximation adapted from the Mandelbulber pseudo-Kleinian formula, embedded in 4D with a fixed W slice. It produces the alien half-space architecture look: foamy nested-cell lattices and cathedral-tiling-to-the-horizon structures.",
            "Unlike the analytic Kleinian, this variant keeps an honest running derivative and uses the canonical pseudo-Kleinian tube/slab distance estimator, so it renders cheaply (no numerical gradient). An optional sphere inversion conformally bounds the otherwise space-filling tiling into a ball.",
        },
        HowComputed = new[]
        {
            "The 4D point is the sample point plus the fixed W slice. Each iteration optionally inverts through a sphere, applies a sign-dependent box offset (the Kleinian symmetry break), a clamp-form box fold, and a one-sided spherical fold, all while tracking the running derivative. The final distance estimate is the pseudo-Kleinian identity: a tube about the z-axis intersected with the z=0 slab.",
        },
        Math = new[]
        {
            "This matches Mandelbulber's pseudo-Kleinian construction: a box offset that breaks cell symmetry, a Mandelbox box fold, an optional sphere inversion to bound the half-space tiling, and a one-sided spherical fold, with the final distance being 0.5*(min(tube, slab) - offset) over the derivative. The W slice is the animatable 4th-dimension cut.",
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
            "A prototype orbit hybrid: two formulas (KIFS and Mandelbox) composed into a single orbit, sharing one running point and one derivative, with the active formula chosen each iteration by a repeating schedule (for example one KIFS step then two Mandelbox steps, repeating).",
            "This is function composition (a genuinely new shape), not CSG of two finished fields. The pairing was chosen because an orbit hybrid needs at least one fold that caps the magnitude: Mandelbox's box fold is that cap, KIFS's abs is not.",
        },
        HowComputed = new[]
        {
            "Each iteration picks a phase from the schedule. The KIFS step does abs(z), an optional post-rotation curl, a sphere fold and a scale. The Mandelbox step does the box fold (the magnitude cap), a sphere fold and a scale-and-add. Both update the shared derivative. The distance estimate is the vector length over the derivative; the derivative over-estimates the true value, so the estimate is conservative and hole-free.",
        },
        Math = new[]
        {
            "There is no single canonical orbit hybrid in the literature; it is Parsec's prototype of the multi-formula sequencer idea (apply formula A for N iterations, then B for M, in a loop). The two halves are canonical: KIFS (Knighty's kaleidoscopic system, scale about 2) and the Mandelbox (Tom Lowe), whose most distinctive sets are at negative scale: -1.5 dense and spiky, -2.0 the classic boxed look, -2.5 organic and porous.",
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

    // ===================================================================
    // EXOTIC group A
    // ===================================================================

    private static GuideContent Apollonian => new()
    {
        Title = "Apollonian Gasket",
        WhatItIs = new[]
        {
            "The 3D Apollonian gasket (Coxeter tetrahedral sphere packing) is the limit set of the Kleinian group generated by inversions through five mutually tangent spheres: four unit spheres at the vertices of a regular tetrahedron plus one bounding sphere. Repeated inversion packs ever-smaller tangent spheres into the gaps, producing a fractal surface.",
            "A planar cross-section of the 3D packing is the classic 2D Apollonian gasket.",
        },
        HowComputed = new[]
        {
            "The distance estimator renders the limit set itself, not the sphere surfaces. For each point it iterates inversions: if it lies inside an inner sphere it is inverted through that sphere and a log-scale accumulator grows; if it is outside the bounding sphere it is inverted back inside; if neither fires, the orbit has settled and the loop breaks. The distance is the envelope times exp(-logScale): far from the set the estimate is large, deep in the orbit it collapses to zero so the ray hits the gasket. The half-cut is a CSG intersection with a tilted half-space.",
        },
        Math = new[]
        {
            "Sphere inversion through a sphere of radius r centred at c maps q to c + (r^2 / |q-c|^2)(q-c). The four-unit-sphere tetrahedral configuration and its bounding sphere follow from the 3D Descartes/Soddy theorem. The inner-sphere centres are the regular-tetrahedron vertices and the bounding-sphere radius is the canonical Soddy value (sqrt(6)+2)/2.",
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
            "The Phoenix set is a Julia-type fractal with memory: the next iterate depends on both the current and the previous iterate, giving organic, curling flame and feather growth instead of crystalline Julia structure. It was introduced by Shigehiro Ushiki.",
            "The canonical 2D map is z_next = z^2 + c + p*z_prev, with the memory coefficient p controlling how strongly the previous term feeds back. Parsec lifts this to 3D using Mandelbulb-style power-2 trig.",
        },
        HowComputed = new[]
        {
            "Each iteration computes the Mandelbulb square of the current point, adds c and the memory term (p times the previous point), then rolls the memory (the old current becomes the new previous). The distance estimate is an analytic running-scalar derivative (deliberately not a numerical gradient, to avoid ghost-ring artifacts on the non-smooth trig map) whose bound includes the memory feedback. The same tilted-plane half-cut is available.",
        },
        Math = new[]
        {
            "Ushiki's original 2D Phoenix map is z_next = z^2 + c + p*z_prev, a second-order recurrence. The historically-cited constants are c around 0.5667 (real) and p = -0.5. Setting the memory p to 0 collapses it to a pure Mandelbulb-Julia; p = -0.5 is the canonical Phoenix character. These 2D values do not transfer one-to-one through the 3D trig lift, so treat them as direction-finding hints.",
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
            "Pickover biomorphs are Julia-type sets rendered with a componentwise (L-infinity) escape test instead of the usual |z| > B. Testing the axes separately makes orbits leak out along individual axes, producing limb-like, creature-resembling forms with arms, antennae, and bulbous bodies.",
            "The effect famously arose from Clifford Pickover swapping AND/OR in the escape condition.",
        },
        HowComputed = new[]
        {
            "Each iteration is the plain Mandelbulb-square Julia (z -> z^2 + c), but escape uses the componentwise test max(|x|,|y|,|z|) > bailout, which is the entire trick. The distance estimate is the Hubbard-Douady running-scalar derivative; since that standard estimate assumes a radial escape, for the L-infinity test it is approximate (off by at most a factor of sqrt(3)), compensated by the DE fudge. The standard tilted-plane half-cut applies.",
        },
        Math = new[]
        {
            "Biomorph escape: a point is interior unless a component exceeds the threshold, i.e. the L-infinity norm exceeds B. Pickover's canonical threshold is B = 10. Common biomorph constants include real values around 0.5 to 0.76; the (-0.5, 0.5) default used here is a community-style constant in the same family (not a single canonical citation).",
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
            "The Mosely snowflake is a cube-based Sierpinski-Menger family fractal named after Jeannine Mosely. It is built by a 3x3x3 cube subdivision that keeps a subset of subcubes and recurses. Parsec keeps the 8 corner subcubes (the corner rule); the silhouette down the body diagonal reproduces the 3-fold Koch snowflake outline.",
        },
        HowComputed = new[]
        {
            "This is an exact linear cube-IFS, not an escape-time fold. Each step folds with abs(z), rotates into the body-diagonal frame, twists about that diagonal, optionally folds the cross-section into a kaleidoscope wedge, returns to the world frame, then scales toward the (1,1,1) corner. Because every operation except the single uniform scale is an isometry, the distance estimate stays exact: a box SDF over the derivative, with no log-potential approximation.",
        },
        Math = new[]
        {
            "The Mosely snowflake is a member of the Menger-sponge family. Parsec keeps the 8 corners (an inverse / dual corner-cube IFS) with dimension log8/log3, giving the 3-fold Koch silhouette down the body diagonal. Scale 3 (the 3x3x3 subdivision) and a body around 1.4 are the snowflake defaults; twist near 120 degrees breaks mirror symmetry into a chiral pinwheel, and shrinking the wedge below 360 makes a radial mandala.",
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

    // ===================================================================
    // EXOTIC group B
    // ===================================================================

    private static GuideContent RiemannSphere => new()
    {
        Title = "Riemann Sphere",
        WhatItIs = new[]
        {
            "A 3D escape-time fractal adapted from Mandelbulber's Msltoe Riemann Sphere V1. Each iteration projects the orbit point onto a sphere, stereographically maps it to the complex plane, applies an abs(sin()) fold to those coordinates (the organic, coral/cellular generator), runs a variable-exponent radial power map, then inverse-projects and adds c.",
            "The sine-fold offsets are the expressive, animatable knobs; zero offsets give the symmetric canonical form.",
        },
        HowComputed = new[]
        {
            "Each iteration projects z onto a sphere of the given radius and stereographically maps to plane coordinates, computes a variable exponent from those coordinates (clamped by Power clamp), applies the abs(sin()) fold with the two offsets, runs a radial power map, tracks a Mandelbulb-style running derivative, then inverse-projects and adds c. The distance estimate is the Mandelbulb form and is approximate. Bailout is kept deliberately low because the high effective exponent overflows a float past a small radius.",
        },
        Math = new[]
        {
            "The Riemann sphere is the one-point compactification of the complex plane; stereographic projection maps a sphere point to and from a plane coordinate. Plotting an escape-time field on the sphere instead of the plane is a known visualization, and the specific 3D Msltoe Riemann Sphere V1 formula is a Mandelbulber type. Parsec deliberately uses a low bailout (about 2) to avoid float overflow from the doubled exponent.",
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
            "An escape-time fractal built on the Mandalay fold, a darkbeam transform from Fractal Forums in the Amazing-Box/Amazing-Surf family. The fold folds space into the positive octant, then runs a per-axis cascade of conditional coordinate swaps and min/max of offset planes that carves a cross/beam base shape.",
            "The escape-time scaffold z = scale*fold(z) + c (Mandelbox/KIFS-style) turns the fold into a fractal. A negative scale (about -2) gives the rich bounded set.",
        },
        HowComputed = new[]
        {
            "Each iteration takes the sign and abs of z, folds each of the x/y/z axes with the Mandalay axis fold (conditional swaps plus min/max of offset planes), reapplies the sign, then does z = scale*z + c while tracking the running derivative. The fold is nearly distance-preserving, so the distance estimate is the vector length over the derivative. DE fudge defaults low because the fold expands about 1.7x at the fold seams.",
        },
        Math = new[]
        {
            "Mandalay is a named fractal type introduced by darkbeam, in the Mandelbox/conditional-fold family. The escape-time scaffold is the standard Mandelbox/KIFS recurrence z -> scale*fold(z) + c, whose analytic scalar distance estimate is |z|/dr with dr -> |scale|*dr + 1. For this fold family, scale about -1.5 gives tight spiky detail, -2.0 the classic boxed look, -2.5 a more organic/porous opening.",
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
            "An escape-time box-fold fractal under an anisotropic (non-uniform, sheared) linear step, and Parsec's first delta-DE chapter. The linear map stretches space by different amounts along different sheared axes, so a scalar running derivative is wrong (it tears holes in the surface). Parsec instead estimates distance numerically via a finite-difference Jacobian.",
            "The stretch and shear knobs are the whole reason this fractal needs delta-DE.",
        },
        HowComputed = new[]
        {
            "The map is z = M*boxFold(z) + c, where M combines a per-axis stretch with two shear rotations. The distance estimate is delta-DE: run the base orbit to find the escape iteration, run three more orbits from slightly offset start points for the same number of steps, finite-difference into a 3x3 Jacobian, and divide the base vector length by the Jacobian norm. The norm is either Frobenius (safe, hole-free but conservative) or the largest singular value (tight and crisper but can sparkle). This costs about 4x a scalar core.",
        },
        Math = new[]
        {
            "The base map is the standard Mandelbox box fold (no sphere fold) under a general linear transform instead of a scalar scale. The anisotropy makes M a non-similarity transform, so the numerical-Jacobian / delta-DE technique (run the orbit at the point and at offset points, finite-difference) is the general estimator used when no analytic distance estimate exists. The Frobenius norm is a safe over-estimate of the operator norm. This parametrization is Parsec-original, so there are no community canonical stretch/shear values.",
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
            "A hybrid escape-time fractal that runs both a Mandelbox (rotated box fold + sphere fold + scale) and a Mandelbulb (spherical power) step in sequence every iteration. The small per-iteration rotation compounds across iterations and is the morph knob.",
            "The distance estimate is a heuristic combination of the two component estimates (neither has a proven lower bound), with a 0.5 safety factor to keep the marcher conservative. Some parameter regions will show artifacts, which is expected for hybrids.",
        },
        HowComputed = new[]
        {
            "Each iteration rotates z by a compounding rotation, runs the Mandelbox half (box fold, sphere fold, scale and add) updating the derivative, then runs the Mandelbulb half (spherical power) updating the derivative again. The final distance estimate is the bulb-style form with a doubled safety factor. Bailout and bound radius are hardcoded, not exposed. Every knob is a smooth scalar, which makes this a strong animation target.",
        },
        Math = new[]
        {
            "This is a literal composition of two well-known distance-estimated fractals. Mandelbox (Tom Lowe): box fold + sphere fold + scale, with scale near -1.5 to -2 for the interesting bounded sets. Mandelbulb (White and Nylander): the spherical nth-power map, power 8 iconic and power 2 the simpler IQ-bulb. The 0.5 safety factor and the rotate -> box -> bulb order are Parsec's heuristic choice; there is no canonical distance estimate for this exact combination.",
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
            "The Thomas cyclically symmetric strange attractor, a 3D chaotic ODE viewed as the trajectory of a frictionally damped particle in a lattice of sinusoidal forces. Unlike the closed-form distance fields, this is a trajectory: Parsec integrates the ODE into hundreds of thousands of points, builds a spatial hash, and renders the polyline as a glowing tube.",
            "Because that integration is expensive, the parameters split into generation params (take effect on Generate) and live params (tube look). Animation is disabled for this fractal: it is regenerated, not tweened.",
        },
        HowComputed = new[]
        {
            "The canonical Thomas derivative (sin(y) - b*x, cyclic in x->y->z) is integrated by RK4 at a fixed timestep, after a burn-in to skip the transient. Four optional perturbation channels (parameter drift, phase modulation, multi-seed phase shift, nonlinear coupling) can deform the orbit. The distance estimate is the distance to the nearest trajectory segment minus the tube radius, accelerated by a uniform spatial hash that only tests segments in the neighboring cells.",
        },
        Math = new[]
        {
            "Thomas' cyclically symmetric attractor: dx/dt = sin(y) - b*x, dy/dt = sin(z) - b*y, dz/dt = sin(x) - b*z. The damping b is the bifurcation parameter: above about 0.2082 the system is a limit cycle (structured loops), and below it the orbit becomes chaotic, filling out toward the full strange attractor as b drops. The classic chaotic value is about 0.19.",
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

    // ===================================================================
    // 3D Burning Ship
    // ===================================================================

    private static GuideContent BurningShip => new()
    {
        Title = "3D Burning Ship",
        WhatItIs = new[]
        {
            "A 3D distance-estimated escape-time fractal that takes the same triplex z -> z^n + c power map as the Mandelbulb but applies an abs() fold to every component after adding c, in a y-up angular convention. The abs folds break the smooth Mandelbulb shell into terraced, vertically mirror-symmetric, windblown massing.",
            "Its characteristic swept laminar sheets live at low power (about 2 to 3); at high power the radial growth dominates and the form degenerates toward a flat-bottomed Mandelbulb.",
        },
        HowComputed = new[]
        {
            "Each iteration converts z to spherical coordinates (y-up), advances the running derivative in log space (the abs is an isometry so it does not change the derivative magnitude), raises r to the power and multiplies the angles by the power, rebuilds Cartesian and adds the sample point, then applies the burning-ship fold abs(z). Escape is when r exceeds the bailout. The distance estimate is the Mandelbulb log form and is not a strict lower bound near the poles, so the default DE fudge is below 1.0 to suppress crease tearing.",
        },
        Math = new[]
        {
            "The 2D Burning Ship is canonical (Michelitsch and Rossler, 1992): z_next = (|Re z| + i|Im z|)^2 + c, escape radius 2. The 3D version is not a standard published formula; it is a community/custom triplex extension analogous to the Mandelbulb's spherical-coordinate power map with a per-component abs fold added. There is no single canonical power, escape radius, or distance estimate for a 3D Burning Ship.",
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
            "The canonical complex quadratic Mandelbrot set, rendered in the 2D deep-zoom mode. It iterates z -> z^2 + c from a zero seed over the parameter plane, with pan and zoom driven by the mouse rather than sliders, reaching depths to about 1e-147 via perturbation theory.",
        },
        HowComputed = new[]
        {
            "A single high-precision reference orbit is iterated once per view; each pixel then iterates a low-precision delta against that reference, which keeps depth in the delta's exponent range. Whenever a pixel orbit gets smaller than its delta or runs off the reference, it rebases to keep the math glitch-free. Three depth paths are used: exact double precision for wide views, double-precision perturbation in the middle, and a slower floatexp perturbation past the double underflow wall for the deepest zooms.",
        },
        Math = new[]
        {
            "z_next = z^2 + c, seed 0, parameter plane, escape when |z|^2 > 4. The perturbation step is dz_next = 2*Zref*dz + dz^2 + dc. The home view is centered near (-0.5, 0).",
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
            "A custom Parsec 2D quadratic map with no standard published definition. It is not a recognized named fractal; web search finds no published Prospector fractal with this formula, so this guide describes only what the shader does.",
        },
        HowComputed = new[]
        {
            "Like the other deep-zoom formulas, a high-precision reference orbit is iterated once per view and each pixel iterates a delta against it with rebasing, across the same three depth paths (exact double precision, double-precision perturbation, and floatexp for the deepest zooms). Because the map's bounded orbits reach a large magnitude, it uses a much larger escape radius than the complex formulas.",
        },
        Math = new[]
        {
            "The real 2D map is X_next = Cx + 0.25*X*Y, Y_next = Cy - 3*X^2 + 0.25*Y^2, seed 0, parameter plane. Its orbits reach about |z|^2 = 53, so it escapes at a radius-squared of about 1e6. The home view is centered at (0, 0). This is a custom map, not a canonical object.",
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
            "The canonical complex quadratic Julia set, rendered in the 2D deep-zoom mode. Here kappa (the constant c) is fixed and the pixel is the seed (the dynamical plane), so sweeping kappa morphs the whole set. The kappa sliders are the only slider-exposed parameters in deep-zoom mode, and they are keyframeable for animation.",
        },
        HowComputed = new[]
        {
            "A high-precision reference orbit (here the view-center orbit, which starts at the seed rather than 0) is iterated once per view; each pixel enters its offset as the initial delta and iterates against the reference with rebasing, which carries the reference-start correction so the math stays correct. The same three depth paths apply: exact double precision wide, double-precision perturbation in the middle, floatexp for the deepest zooms.",
        },
        Math = new[]
        {
            "z_next = z^2 + kappa, dynamical plane, escape when |z|^2 > 4. The pixel is the seed and kappa is fixed. The home view is centered at (0, 0). The default kappa (-0.8, 0.156) is the community-named Spiral Galaxy Julia set.",
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
            "The canonical 2D Burning Ship fractal, rendered in the deep-zoom mode. It folds the cross term with an abs to produce the characteristic ship-and-antenna structure, over the parameter plane with mouse-driven pan and zoom.",
        },
        HowComputed = new[]
        {
            "A high-precision reference orbit is iterated once per view and each pixel iterates a delta against it. Because perturbation on the abs map is unstable when the delta is large, the exact double-precision direct path is the only reliable one at wide views; the perturbation path uses the diffabs primitive (|c+d| - |c|) to stay correct, matching the standard Kalles Fraktaler / mathr case analysis. The floatexp path handles the deepest zooms.",
        },
        Math = new[]
        {
            "X_next = X^2 - Y^2 + Cx, Y_next = 2*|X*Y| + Cy, seed 0, parameter plane, escape when |z|^2 > 4. The x-equation has no abs (since |X|^2 = X^2); only the cross term folds. The home view is centered near (-0.5, -0.5).",
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
