// =============================================================================
// Parsec hybrid Mandelbox + Mandelbulb (rotated) distance estimator - core
// =============================================================================
//
// Includable chunk (no #version, no main). Concatenated after a #version line
// and before raymarch_main.glsl, like the other DE cores.
//
// Per iteration (variant B): BOTH operations in sequence, each iteration:
//   1. Rotate (compounds across iters -- the morph knobs)
//   2. Mandelbox half: box fold, sphere fold, scale + c
//   3. Mandelbulb half: spherical-power z -> z^n + c
//
// The DE is a HEURISTIC combination (no proven lower-bound property for the
// hybrid -- both component DEs are individually proven but their combination
// isn't). A safety factor of 0.5 on the final result is applied to mitigate
// over-estimation; this is standard practice in the fractal-art community for
// hybrids and was validated in Python (hybrid_proto.py) as producing a clean
// renderable surface. If visible holes/noise appear in some regimes the proper
// upgrade path is a numerical-gradient DE (the technique we used for Kleinian).
//
// PARAMETER REUSE (shared FoldParams buffer, binding 1):
//   iterations    = iteration count
//   boxParams     = (scale, minRadius, fixedRadius, foldLimit)
//   surfParams    = (rotX, rotY, rotZ, power)  -- angles in radians, then power
//   rot.w         = DE fudge
//   boundSphere   = bounding sphere for the fast skip

layout(std430, binding = 1) readonly buffer FoldParams {
    int   iterations;
    int   mode;             // unused
    int   juliaMode;        // unused
    int   pad0;

    vec4  boxParams;        // (scale, minRadius, fixedRadius, foldLimit)
    vec4  surfParams;       // (rotX, rotY, rotZ, power)
    vec4  juliaC;           // unused
    vec4  rot;              // (_, _, _, fudge)
    vec4  boundSphere;      // (cx, cy, cz, r)
} fp;

vec4 gTrap;

mat3 eulerRotation(float ax, float ay, float az) {
    float cx = cos(ax), sx = sin(ax);
    float cy = cos(ay), sy = sin(ay);
    float cz = cos(az), sz = sin(az);
    mat3 Rx = mat3(1.0, 0.0, 0.0,
                   0.0, cx, sx,
                   0.0, -sx, cx);
    mat3 Ry = mat3(cy, 0.0, -sy,
                   0.0, 1.0, 0.0,
                   sy, 0.0, cy);
    mat3 Rz = mat3(cz, sz, 0.0,
                   -sz, cz, 0.0,
                   0.0, 0.0, 1.0);
    return Rz * Ry * Rx;
}

float estimate(vec3 p) {
    float scale   = fp.boxParams.x;
    float minR    = fp.boxParams.y;
    float fixedR  = fp.boxParams.z;
    float foldLim = fp.boxParams.w;
    float minR2   = minR * minR;
    float fixedR2 = fixedR * fixedR;
    float power   = fp.surfParams.w;
    float bailout = 8.0;

    mat3 R = eulerRotation(fp.surfParams.x, fp.surfParams.y, fp.surfParams.z);

    vec3 z = p;
    float dr = 1.0;
    float r = 0.0;
    gTrap = vec4(1e20);

    for (int i = 0; i < fp.iterations; i++) {
        // --- rotate (compounds across iterations) ---
        z = R * z;

        // --- Mandelbox half ---
        z = clamp(z, -foldLim, foldLim) * 2.0 - z;     // box fold
        float r2 = dot(z, z);
        if (r2 < minR2) {
            float f = fixedR2 / minR2; z *= f; dr *= f;
        } else if (r2 < fixedR2) {
            float f = fixedR2 / r2; z *= f; dr *= f;
        }
        z = z * scale + p;
        dr = dr * abs(scale) + 1.0;

        // --- Mandelbulb half (power step on the folded z) ---
        r = length(z);
        if (r > bailout) break;
        if (r > 1e-12) {
            float theta = acos(clamp(z.z / r, -1.0, 1.0));
            float phi   = atan(z.y, z.x);
            dr = power * pow(r, power - 1.0) * dr + 1.0;
            float zr = pow(r, power);
            theta *= power; phi *= power;
            z = zr * vec3(sin(theta) * cos(phi),
                          sin(theta) * sin(phi),
                          cos(theta)) + p;
        }

        gTrap.x = min(gTrap.x, length(z));
        gTrap.y = min(gTrap.y, abs(z.x));
        gTrap.z = min(gTrap.z, length(z.xy));
        gTrap.w = min(gTrap.w, abs(length(z) - 1.0));
    }

    // Bulb-style DE (the final z came from the bulb power step) with safety
    // factor: the heuristic combined dr can over-estimate, so scale the result
    // down to keep the marcher conservative.
    r = length(z);
    return 0.5 * 0.5 * log(max(r, 1e-12)) * r / max(dr, 1e-12);
}

vec4 attractorBoundingSphere() {
    return fp.boundSphere;
}

float deFudge() {
    return fp.rot.w;
}
