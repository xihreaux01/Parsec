// =============================================================================
// Parsec Mandalay Fold (darkbeam V2) distance estimator - core
// =============================================================================
//
// Includable chunk (no #version, no main). Concatenated after a #version line
// and before raymarch_main.glsl, like the other fold-fractal cores.
//
// The Mandalay fold itself (darkbeam) is a TRANSFORM, not a fractal: it folds
// space into the positive octant then runs a per-axis cascade of conditional
// coordinate swaps and min/max of offset planes -- SDF CSG (union = min,
// intersection = max) that folds space across a cross/beam base shape set by
// the offsets fo / g / h. Built from abs, swaps, and min/max of linear terms,
// so it is nearly distance-preserving (measured Lipschitz: mean ~0.98, max
// ~1.73 at the fold seams).
//
// To make a fractal we add the escape-time scaffold the transform lacks, the
// same one the Mandelbox/KIFS use:  z = scale * fold(z) + c , tracking the
// analytic running derivative  dr = |scale|*dr + 1 ,  DE = |z| / dr. Because
// the fold can expand by ~1.7x at seams the DE can overestimate there, so a DE
// fudge ~0.55 (rot.w) keeps the marcher from overshooting -- standard for this
// family. Validated in Python (mandalay_proto.py): fold Lipschitz measured,
// negative scale (~ -2) gives a rich bounded set (~0.45), DE finite.
//
// PARAMETER REUSE (shared FoldParams buffer, binding 1):
//   iterations
//   mode         = 0 -> parallel folds (each axis from z), 1 -> sequential (Sw)
//   juliaMode    = 0 -> Mandelbrot (c = position), 1 -> Julia (c = juliaC.xyz)
//   boxParams    = (scale, fo, g, h)     // scale + the three uniform fold offsets
//   surfParams   = (bailout, _, _, _)
//   juliaC       = (cx, cy, cz, _)
//   rot          = (_, _, _, fudge)
//   boundSphere  = (cx, cy, cz, r)

layout(std430, binding = 1) readonly buffer FoldParams {
    int   iterations;
    int   mode;             // 0 = parallel, 1 = sequential (Sw)
    int   juliaMode;        // 0 = Mandelbrot, 1 = Julia
    int   pad0;

    vec4  boxParams;        // (scale, fo, g, h)
    vec4  surfParams;       // (bailout, _, _, _)
    vec4  juliaC;           // (cx, cy, cz, _)
    vec4  rot;              // (_, _, _, fudge)
    vec4  boundSphere;      // (cx, cy, cz, r)
} fp;

vec4 gTrap;

// One axis of the Mandalay fold. `a` is abs(z); we conditionally swap the two
// off-axis coordinates, then build the SDF min/max for the main axis `m`.
//   m = main axis index, s2 = the "second" axis used in t2 / v1 (h applies to it),
//   the two swapped coords are (sa, sb), swapped if a[sa] > a[sb].
// Returns the folded value for axis `m`.
float mandalayAxis(vec3 a, int m, int sa, int sb, int s2, float fo, float g, float h) {
    vec3 pp = a;
    if (pp[sa] > pp[sb]) { float tmp = pp[sa]; pp[sa] = pp[sb]; pp[sb] = tmp; }
    float t1 = pp[m]  - 2.0 * fo;
    float t2 = pp[s2] - 4.0 * fo;
    float v  = max(abs(t1 + fo) - fo, t2);
    float v1 = max(t1 - g, pp[s2] - h);
    v1 = max(v1, -abs(pp[m]));
    v  = min(v, v1);
    return min(v, pp[m]);
}

float estimate(vec3 p) {
    float scale   = fp.boxParams.x;
    float fo      = fp.boxParams.y;
    float g       = fp.boxParams.z;
    float h       = fp.boxParams.w;
    float bailout = fp.surfParams.x;

    bool julia = (fp.juliaMode == 1);
    bool seq   = (fp.mode == 1);
    vec3 c = julia ? fp.juliaC.xyz : p;

    vec3 z = p;
    float dr = 1.0;
    float r = 0.0;
    gTrap = vec4(1e20);

    for (int i = 0; i < fp.iterations; i++) {
        r = length(z);
        if (r > bailout) break;

        // ---- Mandalay fold ----
        vec3 s = sign(z);
        vec3 a = abs(z);
        vec3 q = a;

        // x: swap(y,z) if a.z>a.y; second axis = y (h on y)
        q.x = mandalayAxis(a, 0, 2, 1, 1, fo, g, h);
        // y: swap(x,z) if a.x>a.z; second axis = z (h on z). Sequential feeds q.
        vec3 by = seq ? q : a;
        q.y = mandalayAxis(by, 1, 0, 2, 2, fo, g, h);
        // z: swap(x,y) if a.y>a.x; second axis = x (h on x). Sequential feeds q.
        vec3 bz = seq ? q : a;
        q.z = mandalayAxis(bz, 2, 1, 0, 0, fo, g, h);

        z = q * s;

        // ---- escape-time scaffold: scale toward/away + add c ----
        z = scale * z + c;
        dr = abs(scale) * dr + 1.0;

        float rz = length(z);
        gTrap.x = min(gTrap.x, rz);
        gTrap.y = min(gTrap.y, abs(z.x));
        gTrap.z = min(gTrap.z, length(z.xy));
        gTrap.w = min(gTrap.w, abs(rz - 1.0));
    }

    return length(z) / max(dr, 1e-12);
}

vec4 attractorBoundingSphere() { return fp.boundSphere; }
float deFudge()                { return fp.rot.w; }
