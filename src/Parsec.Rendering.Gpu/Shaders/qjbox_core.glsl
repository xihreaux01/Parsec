// =============================================================================
// Parsec quaternion-Julia x Mandelbox hybrid (with half-cut) - core
// =============================================================================
//
// Includable chunk. Concatenated after a #version line and before
// raymarch_main.glsl, like the other DE cores.
//
// Per iteration (variant B): BOTH operations every iteration, in sequence:
//   1. Rotate the 3D part of z (compounds across iters - the morph knobs)
//   2. Mandelbox half on the 3D part: box fold, sphere fold, scale + c
//   3. Quaternion-square: full 4D z -> z^2 + c (with quaternion constant c)
//
// z is a 4D quaternion (x, y, z, w). The 'w' is set from the wslice parameter
// at the start (the 3D-slice-of-4D choice, same as the pure quaternion Julia).
//
// The DE is HEURISTIC (no proven lower-bound for the hybrid). Safety factor 0.5
// applied. Validated as renderable and cross-sectioning meaningfully in Python
// (qjbox_proto.py).
//
// KILLER FEATURE inherited from the pure quaternion Julia: the half-cut CSG
// intersection at the end of estimate() reveals the interior cross-section --
// which Python confirmed shows intricate structure, not a smooth blob.
//
// PARAMETER REUSE (shared FoldParams buffer, binding 1):
//   iterations    = iteration count
//   boxParams     = (scale, minRadius, fixedRadius, foldLimit)
//   surfParams    = (rotX, rotY, rotZ, planeNormalAxis_packed)  -- see below
//   juliaC        = quaternion constant c
//   rot           = (wslice, planeOffset, cutEnabled, fudge)
//   boundSphere   = bounding sphere for the fast skip
//
// surfParams: the rotation goes in xyz (radians); w is unused (we use rot.x/y/z
// for the cut state, see below). The plane normal is determined by an axis
// selector encoded in juliaC.w if we needed it -- but to keep things clean,
// we encode the cut state into the rot vec4 since fold-family fudge fits there
// alongside the cut params.
//
// Actually cleaner layout (used below):
//   rot           = (wslice, planeOffset, cutFlagPlusAxis, fudge)
//      cutFlagPlusAxis: encodes both "cut enabled" and "which axis"
//         0  = no cut
//         1  = cut X      2 = cut Y      3 = cut Z

layout(std430, binding = 1) readonly buffer FoldParams {
    int   iterations;
    int   mode;             // unused
    int   juliaMode;        // unused
    int   pad0;

    vec4  boxParams;        // (scale, minRadius, fixedRadius, foldLimit)
    vec4  surfParams;       // (rotX, rotY, rotZ, _)
    vec4  juliaC;           // quaternion constant c
    vec4  rot;              // (wslice, planeOffset, cutAxisFlag, fudge)
    vec4  boundSphere;
} fp;

vec4 gTrap;

vec4 qmul(vec4 p, vec4 q) {
    return vec4(
        p.x*q.x - p.y*q.y - p.z*q.z - p.w*q.w,
        p.x*q.y + p.y*q.x + p.z*q.w - p.w*q.z,
        p.x*q.z - p.y*q.w + p.z*q.x + p.w*q.y,
        p.x*q.w + p.y*q.z - p.z*q.y + p.w*q.x);
}

vec4 qsq(vec4 q) {
    return vec4(q.x*q.x - q.y*q.y - q.z*q.z - q.w*q.w,
                2.0*q.x*q.y, 2.0*q.x*q.z, 2.0*q.x*q.w);
}

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
    float wslice  = fp.rot.x;
    float bailout = 4.0;
    vec4  c       = fp.juliaC;

    mat3 R = eulerRotation(fp.surfParams.x, fp.surfParams.y, fp.surfParams.z);

    // Initialize z as 4D quaternion (3D point + wslice).
    vec4 z  = vec4(p, wslice);
    vec4 zp = vec4(1.0, 0.0, 0.0, 0.0);   // running quaternion derivative
    float r = 0.0;
    gTrap = vec4(1e20);

    for (int i = 0; i < fp.iterations; i++) {
        r = length(z);
        if (r > bailout) break;

        // --- rotate the 3D part (compounds across iters) ---
        vec3 z3 = R * z.xyz;
        z = vec4(z3, z.w);

        // --- Mandelbox half on the 3D part; leave w alone ---
        z3 = clamp(z.xyz, -foldLim, foldLim) * 2.0 - z.xyz;
        float r2 = dot(z3, z3);
        if (r2 < minR2) {
            float f = fixedR2 / minR2; z3 *= f; zp *= f;
        } else if (r2 < fixedR2) {
            float f = fixedR2 / r2; z3 *= f; zp *= f;
        }
        z3 = z3 * scale + p;
        zp = zp * abs(scale) + vec4(1.0, 0.0, 0.0, 0.0);
        z  = vec4(z3, z.w);

        // --- quaternion-square (full 4D) ---
        zp = 2.0 * qmul(z, zp);
        z  = qsq(z) + c;

        gTrap.x = min(gTrap.x, length(z));
        gTrap.y = min(gTrap.y, abs(z.x));
        gTrap.z = min(gTrap.z, length(z.xy));
        gTrap.w = min(gTrap.w, abs(length(z) - 1.0));
    }

    r = length(z);
    float dz = length(zp);
    float de = (dz < 1e-12) ? 0.0 : 0.5 * 0.5 * log(max(r, 1e-12)) * r / dz;

    // Half-cut: CSG intersection with a half-space along an axis.
    // cutAxisFlag: 0 = off, 1 = X, 2 = Y, 3 = Z.
    int cutFlag = int(round(fp.rot.z));
    if (cutFlag > 0) {
        float pn = 0.0;
        if      (cutFlag == 1) pn = p.x;
        else if (cutFlag == 2) pn = p.y;
        else                   pn = p.z;
        float planeOff = fp.rot.y;
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
