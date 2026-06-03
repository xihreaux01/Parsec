// =============================================================================
// Parsec Riemann Sphere (Msltoe) distance estimator - core
// =============================================================================
//
// Includable chunk (no #version, no main). Concatenated after a #version line
// and before raymarch_main.glsl, like the other DE cores.
//
// Adapted from the Mandelbulber "Msltoe Riemann Sphere" formula. Each iteration
// projects z onto a sphere of radius `scale`, stereographically maps to the
// plane (s,t), sine-folds those coordinates (the periodic abs(sin(pi*.)) is the
// organic, coral/cellular generator), runs a VARIABLE-exponent radial power
// map, then inverse-projects and adds c.
//
// NO ANALYTIC DE: the source tracks no derivative; Mandelbulber renders it with
// generic numerical delta-DE. BUT the radial part collapses cleanly --
// |(2s,2t,s^2+t^2-1)| == 1+s^2+t^2 exactly, so the rebuild's magnitude cancels
// and each step is just  |z| -> |z|^(2p) - 0.25  with p = 1 + |stereo-proj|^2
// in [1, pClamp]. That is a high-power escape-time map (exponent 2p up to ~72),
// so a Mandelbulb-style scalar-derivative DE rides it:
//     dr -> 2p * r^(2p-1) * dr + 1,   DE = 0.5*log(r)*r/dr.
// This is APPROXIMATE (it ignores the sine-fold's angular derivative and how p
// varies spatially) -- same class of approximation that makes Mandelbulbs
// render. Validated in Python (riemann_proto.py): magnitude identity exact,
// radial recurrence exact, bounded fraction ~0.36, DE finite with near-surface
// zeros. If it proves artifact-y, swap ONLY this DE for a delta-DE; the chapter
// wiring is unaffected.
//
// NUMERICAL NOTE: bailout must stay LOW (~2). The exponent 2p reaches ~72, so
// r^(2p) at r=4 overflows a float; at the bailout=2 ceiling, 2^72 ~ 4.7e21 is
// safely finite. The set lives near r<=1 anyway, so a low bailout loses nothing.
//
// PARAMETER REUSE (shared FoldParams buffer, binding 1):
//   iterations
//   mode         = unused (reserved)
//   juliaMode    = 0 -> Mandelbrot (c = position), 1 -> Julia (c = juliaC.xyz)
//   boxParams    = (scale, offsetA0, offsetB0, bailout)
//   surfParams   = (powerClamp, _, _, _)        // p clamp, default 36
//   juliaC       = (cx, cy, cz, _)              // Julia constant
//   rot          = (rotX, rotY, rotZ, fudge)    // optional pre-rotation; rot.w = DE fudge
//   boundSphere  = (cx, cy, cz, r)

layout(std430, binding = 1) readonly buffer FoldParams {
    int   iterations;
    int   mode;             // unused
    int   juliaMode;        // 0 = Mandelbrot, 1 = Julia
    int   pad0;

    vec4  boxParams;        // (scale, offsetA0, offsetB0, bailout)
    vec4  surfParams;       // (powerClamp, _, _, _)
    vec4  juliaC;           // (cx, cy, cz, _)
    vec4  rot;              // (rotX, rotY, rotZ, fudge)
    vec4  boundSphere;      // (cx, cy, cz, r)
} fp;

const float PI = 3.14159265358979323846;

mat3 rotationFromEuler(vec3 r) {
    float cx = cos(r.x), sx = sin(r.x);
    float cy = cos(r.y), sy = sin(r.y);
    float cz = cos(r.z), sz = sin(r.z);
    mat3 rx = mat3(1,0,0,  0,cx,-sx,  0,sx,cx);
    mat3 ry = mat3(cy,0,sy,  0,1,0,  -sy,0,cy);
    mat3 rz = mat3(cz,-sz,0,  sz,cz,0,  0,0,1);
    return rz * ry * rx;
}

// Orbit-trap accumulator for the shared palette shading.
//   x = min |z|            y = min |z.x|
//   z = min length(z.xy)   w = min |length(z)-1|
vec4 gTrap;

float estimate(vec3 p) {
    float scale   = fp.boxParams.x;
    float offA    = fp.boxParams.y;
    float offB    = fp.boxParams.z;
    float bailout = fp.boxParams.w;
    float pClamp  = fp.surfParams.x;

    bool julia = (fp.juliaMode == 1);
    vec3 c = julia ? fp.juliaC.xyz : p;

    mat3 R = rotationFromEuler(fp.rot.xyz);
    bool useRot = (fp.rot.x != 0.0 || fp.rot.y != 0.0 || fp.rot.z != 0.0);

    vec3 z = p;
    float dr = 1.0;
    float r = length(z);
    gTrap = vec4(1e20);

    for (int i = 0; i < fp.iterations; i++) {
        r = length(z);
        if (r > bailout) break;

        if (useRot) z = R * z;

        // Project onto the sphere of radius `scale`, then stereographic to plane.
        vec3 zs = z * (scale / max(r, 1e-12));
        float dz = 1.0 - zs.z;
        if (abs(dz) < 1e-6) dz = (dz < 0.0) ? -1e-6 : 1e-6;   // guard the north-pole pole
        float q = 1.0 / dz;
        float s = zs.x * q;
        float t = zs.y * q;

        // Variable exponent from the projected radius (clamped: pow() guard).
        float pe = min(1.0 + s * s + t * t, pClamp);

        // Sine-fold the plane coordinates -> the organic cellular generator.
        s = abs(sin(PI * s + offA));
        t = abs(sin(PI * t + offB));

        // Radial power map: |z| -> |z|^(2*pe) - 0.25.  (r is the pre-step radius.)
        float rp = pow(r, 2.0 * pe) - 0.25;

        // Running scalar derivative (Mandelbulb-style radial-power approximation).
        dr = 2.0 * pe * pow(r, 2.0 * pe - 1.0) * dr + 1.0;

        // Inverse stereographic rebuild; magnitude of (2s,2t,s^2+t^2-1) is exactly
        // 1+s^2+t^2, so this gives |z| == rp before the +c.
        float denom = 1.0 + s * s + t * t;
        z = vec3(2.0 * s, 2.0 * t, -1.0 + s * s + t * t) * (rp / denom);
        z += c;

        float rz = length(z);
        gTrap.x = min(gTrap.x, rz);
        gTrap.y = min(gTrap.y, abs(z.x));
        gTrap.z = min(gTrap.z, length(z.xy));
        gTrap.w = min(gTrap.w, abs(rz - 1.0));
    }

    return 0.5 * log(max(r, 1e-12)) * r / max(dr, 1e-12);
}

vec4 attractorBoundingSphere() { return fp.boundSphere; }
float deFudge()                { return fp.rot.w; }
