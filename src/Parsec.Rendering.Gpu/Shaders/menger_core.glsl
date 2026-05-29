// =============================================================================
// Parsec rotated folded Menger-IFS distance estimator - core
// =============================================================================
//
// Includable chunk. Concatenated after a #version line and before
// raymarch_main.glsl, like the other DE cores.
//
// Architectural fractal: a Menger-sponge-style IFS with abs-fold and sort
// preserving rectilinear character, plus a pre-rotation each iteration to
// break the strict axis-alignment and morph the form. Produces stacked-cube
// architecture-like geometry -- a distinct aesthetic category in the
// collection (rectilinear/architectural vs the existing geological/biological/
// glassy/uncanny categories).
//
// Per iteration:
//   z = R * z                       -- rotate (compounds)
//   z = abs(z)                      -- reflect into positive octant
//   pairwise-sort components by magnitude
//   z = z*scale - off*(scale-1)     -- IFS shrink-and-translate to corner
//   if z.z low: z.z += off.z*(scale-1)
//   dr *= scale
// DE = boxBoundDistance(z) / dr
//
// Validated in Python (menger_proto.py): produces recognizable Menger-class
// rectilinear geometry, DE is stable, rotation has measurable effect.
//
// PARAMETER REUSE (shared FoldParams buffer, binding 1):
//   iterations   = iteration count
//   boxParams    = (scale, offX, offY, offZ)
//   surfParams   = (rotX, rotY, rotZ, _)  -- radians
//   rot.w        = DE fudge
//   boundSphere  = bounding sphere for the fast skip

layout(std430, binding = 1) readonly buffer FoldParams {
    int   iterations;
    int   mode;             // unused
    int   juliaMode;        // unused
    int   pad0;

    vec4  boxParams;        // (scale, offX, offY, offZ)
    vec4  surfParams;       // (rotX, rotY, rotZ, _)
    vec4  juliaC;           // unused
    vec4  rot;              // (_, _, _, fudge)
    vec4  boundSphere;
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
    float scale = fp.boxParams.x;
    vec3  off   = fp.boxParams.yzw;
    mat3  R     = eulerRotation(fp.surfParams.x, fp.surfParams.y, fp.surfParams.z);

    vec3  z  = p;
    float dr = 1.0;
    gTrap = vec4(1e20);

    for (int i = 0; i < fp.iterations; i++) {
        z = R * z;             // pre-rotate (compounds)
        z = abs(z);            // reflect into positive octant

        // Menger sort: largest component to z (pairwise swaps).
        if (z.x < z.y) z.xy = z.yx;
        if (z.x < z.z) z.xz = z.zx;
        if (z.y < z.z) z.yz = z.zy;

        // IFS step: shrink-and-translate to corner.
        z = z * scale - off * (scale - 1.0);
        if (z.z < -off.z * (scale - 1.0) * 0.5) {
            z.z += off.z * (scale - 1.0);
        }
        dr *= scale;

        gTrap.x = min(gTrap.x, length(z));
        gTrap.y = min(gTrap.y, abs(z.x));
        gTrap.z = min(gTrap.z, length(z.xy));
        gTrap.w = min(gTrap.w, abs(length(z) - 1.0));
    }

    // Bounding-cube distance estimator.
    vec3 d = max(abs(z) - 1.0, 0.0);
    return length(d) / abs(dr);
}

vec4 attractorBoundingSphere() {
    return fp.boundSphere;
}

float deFudge() {
    return fp.rot.w;
}
