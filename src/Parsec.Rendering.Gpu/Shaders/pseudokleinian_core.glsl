// =============================================================================
// Parsec Pseudo-Kleinian 4D distance estimator - shared core
// =============================================================================
//
// Includable chunk (no #version, no main). Concatenated after a #version line
// and before raymarch_main.glsl's entry point, exactly like kifs_core.glsl and
// mandelbox_core.glsl.
//
// Approximates a KLEINIAN-GROUP LIMIT SET (the "alien half-space architecture":
// foamy nested-cell lattices, cathedral-tiling-to-the-horizon). Adapted from
// the Mandelbulber pseudo-kleinian formula (Knighty / pseudo-Kleinian lineage),
// stripped to the essential path. Unlike the Mandelbox FAMILY this measures
// distance to a near-space-filling limit set, not to a compact solid -- the
// identity is the slab-intersect-tube DE at the end, not |z|/dr.
//
// Each iteration (z is 4D; the 4th coordinate is a fixed slice w0):
//     (optional) sphere inversion   z *= gInv/(z.z);  dr *= gInv/(z.z)   // bounds the half-space tiling into a ball
//     box offset                    z.xyz -= cOff * sign(z.xyz)          // the Kleinian symmetry break
//     box fold                      z = |z+cSize| - |z-cSize| - z        // clamp-form Mandelbox box fold to +-cSize
//     one-sided spherical fold      k = max(sScale/(z.z), 1); z *= k; dr *= k+tweak
// then the DE:
//     d1 = ||(x,y,w)|| - tubeRadius            (a tube about the z-axis; mode 1 = quaternionic min form)
//     d2 = |z.z|                               (distance to the z=0 slab)
//     DE = 0.5 * (min(d1,d2) - deOffset) / dr
//
// A rotation (z.z is an isometry) leaves dr unchanged; the inversion and the
// spherical fold both multiply dr by their own factor, so dr stays an honest
// running derivative and DE = distance/dr is valid. Validated in Python
// (pk_proto.py) against the Mandelbulber reference before this port.
//
// PARAMETER REUSE: shares the std430 FoldParams buffer (binding 1). Slots:
//   iterations
//   mode        = DE form        (0 = tube sqrt(x^2+y^2+w^2); 1 = quaternionic min-of-four)
//   juliaMode   = bounding mode  (0 = raw, render within boundSphere; 1 = sphere-inversion ON)
//   boxParams   = (cSize.x, cSize.y, cSize.z, sphereFoldScale)   // box half-sizes; spherical fold scale1
//   surfParams  = (tubeRadius, deOffset, deTweak, sphereInversionScale)
//   juliaC      = (boxOffset.x, boxOffset.y, boxOffset.z, w0)    // Kleinian translation; 4D slice
//   rot         = (_, _, _, fudge)                               // rot.w = DE fudge (reserved)
//   boundSphere = (cx, cy, cz, r)

layout(std430, binding = 1) readonly buffer FoldParams {
    int   iterations;
    int   mode;             // 0 = tube DE, 1 = quaternionic min-of-four DE
    int   juliaMode;        // 0 = raw, 1 = sphere-inversion bounding ON
    int   pad0;

    vec4  boxParams;        // (cSizeX, cSizeY, cSizeZ, sphereFoldScale)
    vec4  surfParams;       // (tubeRadius, deOffset, deTweak, sphereInversionScale)
    vec4  juliaC;           // (boxOffX, boxOffY, boxOffZ, w0)
    vec4  rot;              // (_, _, _, fudge)
    vec4  boundSphere;      // (cx, cy, cz, r)
} fp;

// Orbit-trap accumulator, read by the shared shading after a hit-point estimate.
//   x = min |z|             (origin trap)
//   y = min |z.x|           (x=0 plane trap)
//   z = min length(z.xy)    (z-axis tube trap)
//   w = min |length(z)-1|   (unit-shell trap)
vec4 gTrap;

float estimate(vec3 p) {
    vec4  z      = vec4(p, fp.juliaC.w);          // 4D embed; w0 is the slice
    float dr     = 1.0;

    vec3  cOff   = fp.juliaC.xyz;                 // box offset (Kleinian asymmetry)
    vec4  cSize  = vec4(fp.boxParams.xyz, 1.0);   // box fold half-size (w fixed at 1)
    float sScale = fp.boxParams.w;                // one-sided spherical fold scale
    float tube   = fp.surfParams.x;               // DE tube radius
    float dOff   = fp.surfParams.y;               // DE offset
    float tweak  = fp.surfParams.z;               // DE derivative tweak
    float gInv   = fp.surfParams.w;               // sphere inversion scale
    bool  invert = (fp.juliaMode == 1);

    gTrap = vec4(1e20);

    for (int i = 0; i < fp.iterations; i++) {
        // optional sphere inversion -> conformally bounds the half-space tiling
        if (invert) {
            float rr = max(dot(z, z), 1e-9);
            float f  = gInv / rr;
            z *= f; dr *= f;
        }

        // box offset: sign-dependent translation (breaks the cell symmetry)
        z.xyz -= cOff * sign(z.xyz);

        // pseudo-kleinian box fold (clamp form of the Mandelbox box fold)
        z = abs(z + cSize) - abs(z - cSize) - z;

        // one-sided spherical fold: invert only inside the min-radius sphere
        float k = max(sScale / max(dot(z, z), 1e-9), 1.0);
        z *= k; dr *= k + tweak;

        // orbit traps on the evolving z
        float rz = length(z);
        gTrap.x = min(gTrap.x, rz);
        gTrap.y = min(gTrap.y, abs(z.x));
        gTrap.z = min(gTrap.z, length(z.xy));
        gTrap.w = min(gTrap.w, abs(rz - 1.0));
    }

    // slab-intersect-tube distance estimate (the pseudo-Kleinian identity)
    vec4 zz = z * z;
    float d1;
    if (fp.mode == 1)
        d1 = sqrt(min(min(min(zz.x + zz.y, zz.y + zz.z), zz.z + zz.w), zz.w + zz.x));
    else
        d1 = sqrt(zz.x + zz.y + zz.w);
    d1 -= tube;

    float d2 = abs(z.z);
    float de = min(d1, d2);
    de = 0.5 * (de - dOff) / max(dr, 1e-9);
    return de;
}

vec4 attractorBoundingSphere() { return fp.boundSphere; }
float deFudge()                { return fp.rot.w; }
