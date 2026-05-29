// =============================================================================
// Parsec Mandelbox / AmazingSurf distance estimator - shared core
// =============================================================================
//
// Includable chunk (no #version, no main). Concatenated after a #version line
// and before an entry-point main(), exactly like de_core.glsl.
//
// This is the "fold" family of distance-estimated fractals (Tom Lowe's
// Mandelbox and its AmazingBox / AmazingSurf descendants), NOT an affine IFS.
// Each iteration applies a box fold, a sphere fold, then scale-and-translate,
// tracking a running scalar derivative dr. The distance estimate is |z|/|dr|.
//
// A flat loop, no stack: much simpler than the affine IFS DE. The richness
// comes from the conditional folds and optional per-iteration rotation.

// -----------------------------------------------------------------------------
// Parameters (binding 1) - mirrors the Query slot used by the affine core, but
// carries Mandelbox/AmazingSurf parameters instead.
// -----------------------------------------------------------------------------

layout(std430, binding = 1) readonly buffer FoldParams {
    int   iterations;       // fold iteration count
    int   mode;             // 0 = Mandelbox, 1 = AmazingSurf
    int   juliaMode;        // 0 = use position as offset (Mandelbrot), 1 = fixed c
    int   pad0;

    vec4  boxParams;        // (scale, foldingLimit, minRadius, fixedRadius)
    vec4  surfParams;       // (foldXY, scaleVary, _, _) - AmazingSurf extras
    vec4  juliaC;           // (cx, cy, cz, _) used when juliaMode == 1
    vec4  rot;              // (rotX, rotY, rotZ, fudge) per-iteration rotation (radians) + DE fudge
    vec4  boundSphere;      // (cx, cy, cz, r) - bounding sphere for the marcher's fast-skip
} fp;

// -----------------------------------------------------------------------------
// Per-iteration rotation (built once from rot.xyz, applied each fold step)
// -----------------------------------------------------------------------------

mat3 rotationFromEuler(vec3 r) {
    float cx = cos(r.x), sx = sin(r.x);
    float cy = cos(r.y), sy = sin(r.y);
    float cz = cos(r.z), sz = sin(r.z);
    mat3 rx = mat3(1,0,0,  0,cx,-sx,  0,sx,cx);
    mat3 ry = mat3(cy,0,sy,  0,1,0,  -sy,0,cy);
    mat3 rz = mat3(cz,-sz,0,  sz,cz,0,  0,0,1);
    return rz * ry * rx;
}

// -----------------------------------------------------------------------------
// The folds
// -----------------------------------------------------------------------------

// Box fold: reflect components outside [-L, L]. Lipschitz constant 1, so it
// does not affect the derivative. clamp(z,-L,L)*2 - z is the branchless form.
vec3 boxFold(vec3 z, float L) {
    return clamp(z, -L, L) * 2.0 - z;
}

// "Amazing" fold: reflect into the positive octant (z = abs(z)) and THEN box
// fold. This is the fold that names the AmazingBox / AmazingSurf family.
// abs() is also Lipschitz-1 (it's a reflection across each coordinate plane),
// so it leaves the derivative untouched. This produces a substantial,
// richly-detailed 3D attractor, unlike a naive xy-only "surf" fold which
// collapses to a near-2D sheet.
vec3 amazingFold(vec3 z, float L) {
    z = abs(z);
    return clamp(z, -L, L) * 2.0 - z;
}

// Sphere fold with inner linear zone. Modifies dr by the same factor it scales z.
void sphereFold(inout vec3 z, inout float dr, float minR2, float fixedR2) {
    float r2 = dot(z, z);
    if (r2 < minR2) {
        float t = fixedR2 / minR2;   // inner: linear blow-up
        z *= t; dr *= t;
    } else if (r2 < fixedR2) {
        float t = fixedR2 / r2;      // shell: sphere inversion
        z *= t; dr *= t;
    }
    // else: outside fixedRadius, untouched
}

// -----------------------------------------------------------------------------
// The DE
// -----------------------------------------------------------------------------

// Orbit-trap accumulator (see kifs_core.glsl for component meanings). Shared
// with the KIFS core so the same shading colours both fold families.
vec4 gTrap;

float estimate(vec3 p) {
    float scale       = fp.boxParams.x;
    float foldingLimit= fp.boxParams.y;
    float minRadius   = fp.boxParams.z;
    float fixedRadius = fp.boxParams.w;
    float foldXY      = fp.surfParams.x;

    float minR2 = minRadius * minRadius;
    float fixedR2 = fixedRadius * fixedRadius;

    mat3 R = rotationFromEuler(fp.rot.xyz);
    bool useRotation = (fp.rot.x != 0.0 || fp.rot.y != 0.0 || fp.rot.z != 0.0);

    vec3 offset = (fp.juliaMode == 1) ? fp.juliaC.xyz : p;
    vec3 z = p;
    float dr = 1.0;

    gTrap = vec4(1e20);

    // Bailout radius squared. The |z|/|dr| DE is only meaningful for points
    // that escape; without a bailout, interior points accumulate a huge dr
    // (dr grows like |scale|^iterations) and the ratio underflows to zero
    // everywhere, collapsing the whole field to "solid". Breaking on escape
    // keeps the ratio meaningful for exterior points.
    const float BAILOUT2 = 1000.0;

    for (int i = 0; i < fp.iterations; i++) {
        // Fold (box for Mandelbox, abs+box "Amazing" fold for mode 1).
        if (fp.mode == 1) z = amazingFold(z, foldingLimit);
        else              z = boxFold(z, foldingLimit);

        // Optional rotation between folds (the AmazingSurf "curl" generator).
        if (useRotation) z = R * z;

        // Sphere fold.
        sphereFold(z, dr, minR2, fixedR2);

        // Scale and translate.
        z = scale * z + offset;
        dr = dr * abs(scale) + 1.0;

        // Accumulate orbit traps on the evolving z.
        float rz = length(z);
        gTrap.x = min(gTrap.x, rz);
        gTrap.y = min(gTrap.y, abs(z.x));
        gTrap.z = min(gTrap.z, length(z.xy));
        gTrap.w = min(gTrap.w, abs(rz - 1.0));

        if (dot(z, z) > BAILOUT2) break;
    }

    return length(z) / abs(dr);
}

// Accessor used by raymarch_main: the bounding sphere for the fast-skip.
vec4 attractorBoundingSphere() {
    return fp.boundSphere;
}

// Accessor used by raymarch_main: DE fudge. Fold-fractal DEs overshoot when
// rotations/hybrids are involved, so we scale the step down for safety.
float deFudge() {
    return fp.rot.w;
}
