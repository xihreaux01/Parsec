// =============================================================================
// Parsec bicomplex Julia (Fracmonk formula) - core
// =============================================================================
//
// Includable chunk. Concatenated after a #version line and before
// raymarch_main.glsl, like the other DE cores.
//
// Fracmonk's formula (fractalforums.com) with user mul/add parameters:
//   x' = X_mul * (x*x - y*y - 2*z*w) + Cx
//   y' = Y_mul * (2*x*y + z*z - w*w) + Cy
//   z' = Z_mul * (2*x*z - 2*y*w)     + Cz
//   w' = W_mul * (2*x*w + 2*y*z) + W_add + Cw
//
// This is the BICOMPLEX (tessarine) square -- i^2=-1, j^2=+1, ij=k, k^2=+1,
// commutative multiplication. NOT a division algebra (has zero divisors), so
// unlike the quaternion Julia it can have structural discontinuities and
// faceted/crystalline boundary character.
//
// DE: RUNNING-SCALAR-DERIVATIVE (Hubbard-Douady), same family as Mandelbulb
// and Phoenix in this gallery. This REPLACES an earlier numerical-gradient
// DE that produced wispy filamentary artifacts at high resolution -- the
// numerical-gradient technique works cleanly for analytic algebras (quaternion
// Julia) but picks up high-frequency noise in non-analytic iterations like
// this one with the user mul/add scalings.
//
// The Jacobian of bicomplex squaring z -> z^2 has Euclidean operator norm
// bounded by 2*|z|, same as quaternion or Mandelbulb-style squaring. The
// per-component mul scalings can warp this slightly; we absorb the worst-case
// via max(mul) when computing the scale factor. The DE remains an upper bound
// (conservative -- under-estimates distance, never over) regardless.
//
// PARAMETER REUSE (shared FoldParams buffer, binding 1):
//   iterations    = iteration count
//   boxParams     = (X_mul, Y_mul, Z_mul, W_mul)
//   surfParams    = (W_add, wslice, _, _)
//   juliaC        = quaternion constant c = (Cx, Cy, Cz, Cw)
//   rot           = (planeOffset, cutAxisFlag, bailout, fudge)
//   boundSphere   = bounding sphere for the fast skip

layout(std430, binding = 1) readonly buffer FoldParams {
    int   iterations;
    int   mode;             // unused
    int   juliaMode;        // unused
    int   pad0;

    vec4  boxParams;        // (X_mul, Y_mul, Z_mul, W_mul)
    vec4  surfParams;       // (W_add, wslice, _, _)
    vec4  juliaC;           // (Cx, Cy, Cz, Cw)
    vec4  rot;              // (planeOffset, cutAxisFlag, bailout, fudge)
    vec4  boundSphere;
} fp;

vec4 gTrap;

// Fracmonk's bicomplex square with mul/add scalings.
vec4 bicomplexStep(vec4 z, vec4 c) {
    float x = z.x, y = z.y, zc = z.z, w = z.w;
    float xn = fp.boxParams.x * (x*x - y*y - 2.0*zc*w);
    float yn = fp.boxParams.y * (2.0*x*y + zc*zc - w*w);
    float zn = fp.boxParams.z * (2.0*x*zc - 2.0*y*w);
    float wn = fp.boxParams.w * (2.0*x*w + 2.0*y*zc) + fp.surfParams.x;
    return vec4(xn, yn, zn, wn) + c;
}

float estimate(vec3 p) {
    vec4 c          = fp.juliaC;
    float wslice    = fp.surfParams.y;
    float bailout   = max(fp.rot.z, 2.0);

    // Scale factor for the derivative bound. With clean muls (=1) this is
    // just 2*|z|; the max-mul absorbs the worst-case stretch when the muls
    // depart from unity.
    float maxMul = max(max(abs(fp.boxParams.x), abs(fp.boxParams.y)),
                       max(abs(fp.boxParams.z), abs(fp.boxParams.w)));
    float dzScale = 2.0 * maxMul;

    vec4  z   = vec4(p, wslice);
    float dz  = 1.0;
    float bo2 = bailout * bailout;

    // Orbit-trap mins for the cosine palette (same pattern as QJ/Phoenix).
    gTrap = vec4(1e20);

    for (int i = 0; i < fp.iterations; i++) {
        float r2 = dot(z, z);
        if (r2 > bo2) break;
        float r = sqrt(r2);

        // Update derivative bound BEFORE the state update.
        dz = min(dzScale * r * dz, 1e30);

        z = bicomplexStep(z, c);

        // Orbit traps.
        gTrap.x = min(gTrap.x, length(z));
        gTrap.y = min(gTrap.y, abs(z.x));
        gTrap.z = min(gTrap.z, length(z.xy));
        gTrap.w = min(gTrap.w, abs(length(z) - 1.0));
    }

    float r = length(z);

    // Hubbard-Douady DE: same form as Mandelbulb and Phoenix.
    float de;
    if (dz < 1e-12) {
        de = 0.0;
    } else {
        de = 0.5 * r * log(max(r, 1.0001)) / dz;
    }

    // Half-cut: same axis-flag encoding as qjbox (0 = off, 1 = X, 2 = Y, 3 = Z).
    int cutFlag = int(round(fp.rot.y));
    if (cutFlag > 0) {
        float pn = (cutFlag == 1) ? p.x : (cutFlag == 2) ? p.y : p.z;
        float planeOff = fp.rot.x;
        de = max(de, pn - planeOff);
    }

    return de;
}

vec4 attractorBoundingSphere() {
    return fp.boundSphere;
}

float deFudge() {
    return fp.rot.w;
}
