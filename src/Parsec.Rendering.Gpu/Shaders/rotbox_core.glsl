// =============================================================================
// Parsec rotation-augmented Mandelbox ("rotated fold") distance estimator - core
// =============================================================================
//
// Includable chunk (no #version, no main). Concatenated after a #version line
// and before raymarch_main.glsl, like the other DE cores.
//
// Standard Mandelbox (box fold, sphere fold, scale) with a 3D ROTATION inserted
// each iteration before the folds, so the fold planes cut space at arbitrary
// angles instead of axis-aligned. The rotation compounds across iterations,
// making the three Euler angles extremely sensitive, generative morph knobs --
// this is the "Amazing Surf"/rotated-Mandelbox family behind much of the most
// striking fractal art. Validated in Python (rotbox_proto.py): the DE stays a
// clean valid distance field at modest angles, and the angles morph the shape.
//
// Iteration: z = R*z; box-fold; sphere-fold; z = z*scale + c; dr = dr*|scale|+1.
// DE = |z| / dr (standard Mandelbox estimator).
//
// PARAMETER REUSE (shared FoldParams buffer, binding 1):
//   iterations    = iteration count
//   boxParams     = (scale, minRadius, fixedRadius, foldLimit)
//   surfParams    = (rotX, rotY, rotZ, _)   -- Euler angles in RADIANS
//   rot.w         = DE fudge
//   boundSphere   = bounding sphere for the fast skip

layout(std430, binding = 1) readonly buffer FoldParams {
    int   iterations;
    int   mode;             // unused
    int   juliaMode;        // unused
    int   pad0;

    vec4  boxParams;        // (scale, minRadius, fixedRadius, foldLimit)
    vec4  surfParams;       // (rotX, rotY, rotZ, _)
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

    mat3 R = eulerRotation(fp.surfParams.x, fp.surfParams.y, fp.surfParams.z);

    vec3 z = p;
    float dr = 1.0;
    gTrap = vec4(1e20);

    for (int i = 0; i < fp.iterations; i++) {
        z = R * z;                                   // pre-rotate
        z = clamp(z, -foldLim, foldLim) * 2.0 - z;   // box fold

        float r2 = dot(z, z);
        if (r2 < minR2) {
            float f = fixedR2 / minR2;
            z *= f; dr *= f;
        } else if (r2 < fixedR2) {
            float f = fixedR2 / r2;
            z *= f; dr *= f;
        }

        z = z * scale + p;
        dr = dr * abs(scale) + 1.0;

        gTrap.x = min(gTrap.x, length(z));
        gTrap.y = min(gTrap.y, abs(z.x));
        gTrap.z = min(gTrap.z, length(z.xy));
        gTrap.w = min(gTrap.w, abs(length(z) - 1.0));
    }

    return length(z) / abs(dr);
}

vec4 attractorBoundingSphere() {
    return fp.boundSphere;
}

float deFudge() {
    return fp.rot.w;
}
