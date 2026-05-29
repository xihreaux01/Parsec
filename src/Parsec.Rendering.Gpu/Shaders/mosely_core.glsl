// =============================================================================
// Parsec Mosely-snowflake KIFS distance estimator - shared core
// =============================================================================
//
// Includable chunk (no #version, no main). Concatenated after a #version line
// and before raymarch_main.glsl, exactly like kifs_core.glsl / mandelbox_core.glsl.
//
// Unlike kifs_core.glsl (Knighty escape-time: dr*scale+1, sphere fold, DE =
// |z|/|dr|), this is a LINEAR / EXACT cube-IFS: the corner-cube recursion that
// builds the Mosely snowflake (keep the 8 corner subcubes of a 3x3x3 split;
// fractal dimension log8/log3 ~ 1.893). Every per-iteration op is an isometry
// (abs fold, [111]-frame rotation, wedge reflection) except one uniform scale,
// so the DE stays EXACT: DE = sdBox(z, body) / scale^iters. No log potential.
//
// Two deformation knobs, both isometries -> exactness preserved:
//   twist : rotation about the body diagonal [1,1,1]. Near-120 deg breaks the
//           snowflake's mirror symmetry into a CHIRAL pinwheel (keeps 3-fold).
//   wedge : kaleidoscope fold of the [111]-frame x'y' plane into a wedge. The
//           OFF value (2*pi) is exactly the pure snowflake; smaller -> mandala.
// A 'body' knob fattens the final box SDF: 1.0 = sparse dust, ~1.4 = fuller
// lace (the corner rule carves space at every scale, so it never fuses solid).
//
// Validated against a Python ground-truth prototype (mosely_groundtruth.py)
// before this GLSL port: the down-[111] silhouette reproduced the canonical
// 3-fold Koch outline, and the DE stayed crisp (no haloes) across the full
// twist x wedge sweep.
//
// PARAMETER REUSE: shares the std430 FoldParams buffer (binding 1) with the
// Mandelbox/KIFS cores, so it reuses GpuMandelboxRenderer's machinery wholesale
// ("swap the DE core, keep the boat"). The slots are reinterpreted:
//   boxParams  = (scale, body, _, _)          [default scale 3.0, body ~1.4]
//   surfParams = (twist, wedge, _, _)          [radians; wedge >= 2*pi == OFF]
//   juliaC     = unused (corner is the constant (1,1,1))
//   rot        = (_, _, _, fudge)              [only .w used, via deFudge()]
//   boundSphere= (cx, cy, cz, r)               [~ (0,0,0,2.0)]

layout(std430, binding = 1) readonly buffer FoldParams {
    int   iterations;
    int   mode;             // unused by Mosely (kept for buffer-layout parity)
    int   juliaMode;        // unused by Mosely
    int   pad0;

    vec4  boxParams;        // (scale, body, _, _)
    vec4  surfParams;       // (twist, wedge, _, _)
    vec4  juliaC;           // unused
    vec4  rot;              // (_, _, _, fudge)
    vec4  boundSphere;      // (cx, cy, cz, r)
} fp;

const float MOSELY_TWO_PI = 6.28318530718;

// [111] orthonormal frame (compile-time constant -> baked, nothing passed).
// rows U,V,W ; W == body diagonal. Frame:  zf = (dot(U,z), dot(V,z), dot(W,z)).
// Inverse (orthogonal): z = zf.x*U + zf.y*V + zf.z*W.
const vec3 MOSELY_U = vec3( 0.70710678118, -0.70710678118,  0.0          );
const vec3 MOSELY_V = vec3( 0.40824829046,  0.40824829046, -0.81649658093);
const vec3 MOSELY_W = vec3( 0.57735026919,  0.57735026919,  0.57735026919);

// Exact box SDF (main does NOT define one, so no symbol collision).
float sdBox(vec3 p, vec3 b) {
    vec3 q = abs(p) - b;
    return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0);
}

// Orbit-trap accumulator, written as a side effect of estimate() -- identical
// layout/semantics to kifs_core.glsl so the shared trapAlbedo() is reused as-is:
//   x = min |z|             y = min |z.x|
//   z = min length(z.xy)    w = min |length(z) - 1|
vec4 gTrap;

float estimate(vec3 p) {
    float scale = fp.boxParams.x;          // 3.0 for the true snowflake
    float body  = fp.boxParams.y;          // final-box half-extent (fatten)
    float twist = fp.surfParams.x;         // radians, about [111]
    float wedge = fp.surfParams.y;         // radians; >= 2*pi disables the fold

    bool  doWedge = wedge < (MOSELY_TWO_PI - 1e-4);
    float ct = cos(twist), st = sin(twist);
    // rotation about z' by 'twist', applied as R * v with R = [[ct,-st],[st,ct]].
    mat2 twistR = mat2(ct, st, -st, ct);

    vec3 z = p;
    float dz = 1.0;

    gTrap = vec4(1e20);

    for (int i = 0; i < fp.iterations; i++) {
        // 1) world-frame octahedral fold -> single-corner symmetry
        z = abs(z);

        // 2) into the [111] frame
        vec3 zf = vec3(dot(MOSELY_U, z), dot(MOSELY_V, z), dot(MOSELY_W, z));

        // 3) twist about the body diagonal
        zf.xy = twistR * zf.xy;

        // 4) kaleidoscope fold of x'y' into a wedge (rotate-wrap + reflect: isometry)
        if (doWedge) {
            float r = length(zf.xy);
            float a = atan(zf.y, zf.x);
            a = abs(a - wedge * round(a / wedge));   // wrap into [-w/2,w/2], reflect
            zf.xy = r * vec2(cos(a), sin(a));
        }

        // 5) back to world frame
        z = zf.x * MOSELY_U + zf.y * MOSELY_V + zf.z * MOSELY_W;

        // 6) scale toward the (1,1,1) corner: z = scale*z - (scale-1)*corner
        z = scale * z - (scale - 1.0);   // corner == vec3(1.0)
        dz *= scale;

        // orbit traps (after the full step, on the evolving z)
        float rz = length(z);
        gTrap.x = min(gTrap.x, rz);
        gTrap.y = min(gTrap.y, abs(z.x));
        gTrap.z = min(gTrap.z, length(z.xy));
        gTrap.w = min(gTrap.w, abs(rz - 1.0));
    }

    return sdBox(z, vec3(body)) / dz;
}

// Accessor used by raymarch_main: the bounding sphere for the fast-skip.
vec4 attractorBoundingSphere() {
    return fp.boundSphere;
}

// Accessor used by raymarch_main: DE fudge (step-shortening for safety).
float deFudge() {
    return fp.rot.w;
}
