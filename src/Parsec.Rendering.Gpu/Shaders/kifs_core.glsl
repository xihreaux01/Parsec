// =============================================================================
// Parsec Kaleidoscopic IFS (KIFS) distance estimator - shared core
// =============================================================================
//
// Includable chunk (no #version, no main). Concatenated after a #version line
// and before an entry-point main(), exactly like de_core.glsl and
// mandelbox_core.glsl.
//
// This is the "Amazing IFS" family (Knighty's kaleidoscopic construction, as
// used by the amazingIFS formulas in Mandelbulb3D / Mandelbulber). It is an
// ESCAPE-TIME system like the Mandelbox, NOT an affine IFS, despite the "IFS"
// name. Each iteration:
//
//     z = preRot  * z          // rotation BEFORE the fold
//     z = abs(z)               // plane fold into the positive octant
//     z = postRot * z          // rotation AFTER the fold (the curl generator)
//     sphereFold(z)            // inversion: everts sheets into trumpet flares
//     z = scale*z - (scale-1)*pivot   // scale toward/away from a pivot point
//
// tracking the running scalar derivative dr; the DE is |z|/|dr|.
//
// The rotation composed with the fold under repeated scaling is what winds
// straight folded edges into logarithmic spirals (Ionic-volute scrollwork),
// and the sphere-fold inversion everts folded sheets into the bell/trumpet
// flares characteristic of the family. Validated against a Python prototype
// (kifs_proto2.py / kifs_spherefold.py) before this GLSL port.
//
// PARAMETER REUSE: KIFS shares the exact std430 FoldParams buffer (binding 1)
// with the Mandelbox core, so it reuses GpuMandelboxRenderer's machinery
// wholesale ("swap the DE core, keep the boat"). The slots are reinterpreted:
//   boxParams  = (scale, _, minRadius, fixedRadius)   [foldingLimit unused]
//   rot        = (preRotX, preRotY, preRotZ, fudge)
//   surfParams = (postRotX, postRotY, postRotZ, _)
//   juliaC     = (pivotX, pivotY, pivotZ, _)
//   boundSphere= (cx, cy, cz, r)

layout(std430, binding = 1) readonly buffer FoldParams {
    int   iterations;
    int   mode;             // unused by KIFS (kept for buffer-layout parity)
    int   juliaMode;        // unused by KIFS
    int   pad0;

    vec4  boxParams;        // (scale, _, minRadius, fixedRadius)
    vec4  surfParams;       // (postRotX, postRotY, postRotZ, _)
    vec4  juliaC;           // (pivotX, pivotY, pivotZ, _) - scale pivot
    vec4  rot;              // (preRotX, preRotY, preRotZ, fudge)
    vec4  boundSphere;      // (cx, cy, cz, r)
} fp;

mat3 rotationFromEuler(vec3 r) {
    float cx = cos(r.x), sx = sin(r.x);
    float cy = cos(r.y), sy = sin(r.y);
    float cz = cos(r.z), sz = sin(r.z);
    mat3 rx = mat3(1,0,0,  0,cx,-sx,  0,sx,cx);
    mat3 ry = mat3(cy,0,sy,  0,1,0,  -sy,0,cy);
    mat3 rz = mat3(cz,-sz,0,  sz,cz,0,  0,0,1);
    return rz * ry * rx;
}

// Sphere fold (inversion) with inner linear zone. Modifies dr by the same
// factor it scales z. Identical to the Mandelbox sphere fold.
void sphereFold(inout vec3 z, inout float dr, float minR2, float fixedR2) {
    float r2 = dot(z, z);
    if (r2 < minR2) {
        float t = fixedR2 / minR2;   // inner: linear blow-up
        z *= t; dr *= t;
    } else if (r2 < fixedR2) {
        float t = fixedR2 / r2;      // shell: sphere inversion
        z *= t; dr *= t;
    }
}

// Orbit-trap accumulator, written as a side effect of estimate(). The shared
// raymarch shading reads this after a dedicated estimate() at the hit point to
// colour the surface. Components:
//   x = min |z|              (distance to origin trap)
//   y = min |z.x|            (distance to the x=0 plane trap)
//   z = min length(z.xy)     (distance to the z-axis trap)
//   w = min |length(z) - 1|  (unit-sphere shell trap)
vec4 gTrap;

float estimate(vec3 p) {
    float scale       = fp.boxParams.x;
    float minRadius   = fp.boxParams.z;
    float fixedRadius = fp.boxParams.w;

    float minR2 = minRadius * minRadius;
    float fixedR2 = fixedRadius * fixedRadius;

    mat3 preR  = rotationFromEuler(fp.rot.xyz);
    mat3 postR = rotationFromEuler(fp.surfParams.xyz);
    bool usePre  = (fp.rot.x != 0.0 || fp.rot.y != 0.0 || fp.rot.z != 0.0);
    bool usePost = (fp.surfParams.x != 0.0 || fp.surfParams.y != 0.0 || fp.surfParams.z != 0.0);

    vec3 pivot = fp.juliaC.xyz;

    vec3 z = p;
    float dr = 1.0;

    gTrap = vec4(1e20);

    const float BAILOUT2 = 1000.0;

    for (int i = 0; i < fp.iterations; i++) {
        if (usePre)  z = preR * z;       // rotate before fold
        z = abs(z);                      // plane fold into octant
        if (usePost) z = postR * z;      // rotate after fold (curl)

        sphereFold(z, dr, minR2, fixedR2);

        // Scale toward the pivot: z = scale*z - (scale-1)*pivot.
        z = scale * z - (scale - 1.0) * pivot;
        dr = dr * abs(scale) + 1.0;

        // Accumulate orbit traps (after the full step, on the evolving z).
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

// Accessor used by raymarch_main: DE fudge (step-shortening for safety).
float deFudge() {
    return fp.rot.w;
}
