// =============================================================================
// Parsec Mandelbulb distance estimator - core
// =============================================================================
//
// Includable chunk (no #version, no main). Concatenated after a #version line
// and before raymarch_main.glsl, like the other DE cores.
//
// Canonical White/Nylander Mandelbulb: z -> z^n + c, the n-th power taken in
// spherical coordinates with theta = acos(z.z/r) (the convention that gives the
// classic filigree-pole bulb). Analytic escape-radius DE with running
// derivative dr -> n*r^(n-1)*dr + 1, DE = 0.5*log(r)*r/dr. Both the theta
// convention and the dr formula were validated in Python (mandelbulb_proto.py)
// against the recognizable canonical shape before this port.
//
// Provides the standard core contract (estimate / gTrap / boundingSphere /
// fudge), so it reuses the shared raymarch shading and orbit-trap colour.
//
// PARAMETER REUSE (shared FoldParams buffer, binding 1):
//   iterations   = iteration count
//   boxParams.x  = power (n)
//   boxParams.y  = bailout radius
//   rot.w        = DE fudge
//   boundSphere  = bounding sphere for the fast skip

layout(std430, binding = 1) readonly buffer FoldParams {
    int   iterations;
    int   mode;             // unused
    int   juliaMode;        // unused
    int   pad0;

    vec4  boxParams;        // (power, bailout, _, _)
    vec4  surfParams;       // unused
    vec4  juliaC;           // unused
    vec4  rot;              // (_, _, _, fudge)
    vec4  boundSphere;      // (cx, cy, cz, r)
} fp;

vec4 gTrap;

float estimate(vec3 p) {
    float power   = fp.boxParams.x;
    float bailout = fp.boxParams.y;

    vec3 z = p;
    float ldr = 0.0;       // log of the running derivative (overflow-proof; dr = exp(ldr))
    float r = 0.0;
    gTrap = vec4(1e20);

    for (int i = 0; i < fp.iterations; i++) {
        r = length(z);
        if (r > bailout) break;

        // Spherical coords: theta polar from +Z (acos convention).
        float theta = acos(clamp(z.z / max(r, 1e-12), -1.0, 1.0));
        float phi   = atan(z.y, z.x);

        // Running derivative in LOG space: dr = n*r^(n-1)*dr + 1 becomes
        //   ldr += log(n) + (n-1)*log(r),  with the "+1" carried exactly via
        //   logaddexp(x,0) = x + log(1+exp(-x))  (exp(-x) is tiny -> no overflow).
        // Direct dr overflows fp32 at ~iter 43 (it grows like n^i); this never
        // does, so iterations can climb past 43 to keep the DE accurate as you
        // zoom toward the fp32 march wall (~1e-7). Validated == direct to 1e-14.
        float x = ldr + log(power) + (power - 1.0) * log(max(r, 1e-12));
        ldr = x + log(1.0 + exp(-x));

        // Raise to the power: scale radius, multiply angles, rebuild + add c (=p).
        float zr = pow(r, power);
        theta *= power;
        phi   *= power;
        z = zr * vec3(sin(theta) * cos(phi),
                      sin(theta) * sin(phi),
                      cos(theta)) + p;

        // Orbit traps for the shared palette.
        gTrap.x = min(gTrap.x, length(z));
        gTrap.y = min(gTrap.y, abs(z.x));
        gTrap.z = min(gTrap.z, length(z.xy));
        gTrap.w = min(gTrap.w, abs(length(z) - 1.0));
    }

    // DE = 0.5*log(r)*r / dr = 0.5*log(r)*r*exp(-ldr). For interior points ldr
    // grows large so exp(-ldr) -> 0 (DE -> 0 inside), matching the direct form.
    return 0.5 * log(max(r, 1e-12)) * r * exp(-ldr);
}

vec4 attractorBoundingSphere() {
    return fp.boundSphere;
}

float deFudge() {
    return fp.rot.w;
}
